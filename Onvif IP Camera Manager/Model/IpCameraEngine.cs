using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Onvif_IP_Camera_Manager.LOG;
using Ozeki.Media.IPCamera;
using Ozeki.Media.IPCamera.DateAndTime;
using Ozeki.Media.IPCamera.Imaging;
using Ozeki.Media.IPCamera.Media;
using Ozeki.Media.IPCamera.Network;
using Ozeki.Media.IPCamera.PTZ;
using Ozeki.Media.IPCamera.UserManagement;
using Ozeki.Media.MediaHandlers;
using Ozeki.Media.MediaHandlers.IPCamera;
using Ozeki.Media.MediaHandlers.Video;
using Ozeki.Media.Video;
using Ozeki.VoIP;
using Timer = System.Timers.Timer;

namespace Onvif_IP_Camera_Manager.Model
{
    class IpCameraEngine : Camera
    {
        private IIPCamera _camera;

        public override object CameraObject { get { return _camera; } }
        public override string CameraInfo { get { return _camera.CameraAddress; } }
        public override IAudioSender AudioSender { get { return _camera.AudioChannel; } }
        public override IVideoSender VideoSender { get { return _camera.VideoChannel; } }

        public override IPCameraPreset[] CameraPresets
        {
            get
            {
                try
                {
                    return _camera.CameraMovement.GetPresets();
                }
                catch
                {
                    return null;
                }
            }
        }

        public override List<ICameraUser> GetCameraUsers
        {
            get
            {
                try
                {
                    return _camera.UserManager.GetUsersList();
                }
                catch
                {
                    return null;
                }
            }
        }

        public override IUserManager UserManager { get { return _camera.UserManager; } }
        public override ICameraNetworkManager Network { get { return _camera.NetworkManager; } }
        public override ICameraImaging CameraImage { get { return _camera.ImagingSettings; } }
        public override IPCameraStream[] CameraStreams { get { return _camera.AvailableStreams; } }
        public override List<ICustomInfo> CustomInfos { get { return _camera.CameraInfo.CustomInfos; } }
        public override CameraUriType? UriType { get { return _camera.UriType; } }

        public override IPCameraStream CurrentStream
        {
            get { return _camera.CurrentStream; }
            set { }
        }

        public override bool IsDiscoverable
        {
            get { return _camera.CameraInfo.Discoverable; }
            set { }
        }

        public IpCameraEngine(string ipAddress, string username, string password)
        {
            _camera = IPCameraFactory.GetCamera(ipAddress, username, password);
            _camera.CameraStateChanged += Camera_CameraStateChanged;
            _camera.CameraErrorOccurred += Camera_CameraErrorOccurred;
        }

        public override void SetDiscoverable(bool isDiscoverable)
        {
            _camera.CameraInfo.SetAttributes(isDiscoverable);
            _camera.CameraInfo.RefreshProperties();
        }

        public override bool Connect()
        {
            // connect camera to image viewer
            Connector.Connect(_camera.VideoChannel, Detector);
            Connector.Connect(Detector, BitmapSourceProvider);
            Connector.Connect(_camera.VideoChannel, Snapshot);

            // connect to IP camera
            _camera.Start();

            return true;
        }

        public override void Disconnect()
        {
            if (_camera == null)
                return;

            Connector.Disconnect(_camera.VideoChannel, Detector);
            Connector.Disconnect(Detector, BitmapSourceProvider);
            Connector.Disconnect(_camera.VideoChannel, Snapshot);

            _camera.Disconnect();

            //_camera.CameraStateChanged -= Camera_CameraStateChanged;
            //_camera.CameraErrorOccurred -= Camera_CameraErrorOccurred;

            base.Disconnect();
        }

        public override void Start(IPCameraStream stream)
        {
            _camera.Start(stream);
        }

        void Camera_CameraErrorOccurred(object sender, CameraErrorEventArgs e)
        {
            OnCameraErrorOccurred(e);
        }

        void Camera_CameraStateChanged(object sender, CameraStateEventArgs e)
        {
            OnCameraStateChanged(e);
        }

        public override string DeviceInfo
        {
            get
            {
                if (_camera.CameraInfo.DeviceInfo == null)
                    return String.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("Firmware: " + _camera.CameraInfo.DeviceInfo.Firmware);
                sb.AppendLine("Hardware ID: " + _camera.CameraInfo.DeviceInfo.HardwareId);
                sb.AppendLine("Manufacture: " + _camera.CameraInfo.DeviceInfo.Manufacturer);
                sb.AppendLine("CurrentModel: " + _camera.CameraInfo.DeviceInfo.Model);
                sb.AppendLine("Serial number: " + _camera.CameraInfo.DeviceInfo.SerialNumber);

                return sb.ToString();
            }
            set { }
        }

        public override string AudioStreamInfo
        {
            get
            {
                if (_camera.CurrentStream.AudioEncoding == null || _camera.CurrentStream.AudioSource == null)
                    return String.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("- Audio Encoding");
                sb.AppendLine("\t Bitrate: " + _camera.CurrentStream.AudioEncoding.Bitrate);
                sb.AppendLine("\t Encoding: " + _camera.CurrentStream.AudioEncoding.Encoding);
                sb.AppendLine("\t Samplerate: " + _camera.CurrentStream.AudioEncoding.SampleRate);
                //sb.AppendLine("\t Session time out: " + _camera.CurrentStream.AudioEncoding.SessionTimeOut);
                sb.AppendLine("\t Use count: " + _camera.CurrentStream.AudioEncoding.UseCount);

                sb.AppendLine("\n - Audio Source");
                sb.AppendLine("\t Channels: " + _camera.CurrentStream.AudioSource.Channels);
                sb.AppendLine("\t Use count: " + _camera.CurrentStream.AudioSource.UseCount);

                return sb.ToString();
            }
            set { }
        }

        public override string VideoStreamInfo
        {
            get
            {
                if (_camera.CurrentStream.VideoEncoding == null || _camera.CurrentStream.VideoSource == null)
                    return String.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("- Video Encoding");
                sb.AppendLine("\t Bitrate: " + _camera.CurrentStream.VideoEncoding.BitRate);
                sb.AppendLine("\t Encoding: " + _camera.CurrentStream.VideoEncoding.Encoding);
                sb.AppendLine("\t Encoding interval: " + _camera.CurrentStream.VideoEncoding.EncodingInterval);
                sb.AppendLine("\t Framerate: " + _camera.CurrentStream.VideoEncoding.FrameRate);
                sb.AppendLine("\t Quality: " + _camera.CurrentStream.VideoEncoding.Quality);
                sb.AppendLine("\t Resolution: " + _camera.CurrentStream.VideoEncoding.Resolution);
                //sb.AppendLine("\t Session timeout: " + _camera.CurrentStream.VideoEncoding.SessionTimeout);
                sb.AppendLine("\t Use count: " + _camera.CurrentStream.VideoEncoding.UseCount);

                sb.AppendLine("\n- Video Source");
                sb.AppendLine("\t Bounds: " + _camera.CurrentStream.VideoSource.Bounds);
                sb.AppendLine("\t Use count: " + _camera.CurrentStream.VideoSource.UseCount);

                return sb.ToString();
            }
            set { }
        }

        public override void SetCameraImaging(CameraImaging config)
        {
            _camera.ImagingSettings.SetAttributes(config);
            _camera.ImagingSettings.RefreshProperties();
        }

        public override void SetVideoEncoding(IPCameraVideoEncoding encoding)
        {
            _camera.CurrentStream.VideoEncoding.SetAttributes(encoding);
            _camera.CurrentStream.VideoEncoding.RefreshProperties();
        }

        public override void SetCameraCustomInfo(CustomInfo[] customInfos)
        {
            _camera.CameraInfo.SetAttributes(customInfos);
            _camera.CameraInfo.RefreshProperties();
        }

        public override void Move(string direction)
        {
            if (_camera == null) return;
            switch (direction)
            {
                case "Up Left":
                    _camera.CameraMovement.ContinuousMove(MoveDirection.LeftUp);
                    break;
                case "Up":
                    _camera.CameraMovement.ContinuousMove(MoveDirection.Up);
                    break;
                case "Up Right":
                    _camera.CameraMovement.ContinuousMove(MoveDirection.RightUp);
                    break;
                case "Left":
                    _camera.CameraMovement.ContinuousMove(MoveDirection.Left);
                    break;
                case "Right":
                    _camera.CameraMovement.ContinuousMove(MoveDirection.Right);
                    break;
                case "Down Left":
                    _camera.CameraMovement.ContinuousMove(MoveDirection.LeftDown);
                    break;
                case "Down":
                    _camera.CameraMovement.ContinuousMove(MoveDirection.Down);
                    break;
                case "Down Right":
                    _camera.CameraMovement.ContinuousMove(MoveDirection.RightDown);
                    break;
                case "Set home":
                    _camera.CameraMovement.SetHome();
                    break;
                case "In":
                    _camera.CameraMovement.Zoom(ZoomDirection.In);
                    break;
                case "Out":
                    _camera.CameraMovement.Zoom(ZoomDirection.Out);
                    break;
            }
        }

        public override void StopMove()
        {
            if (_camera == null) return;
            _camera.CameraMovement.StopMovement();
        }

        public override void GoToHome()
        {
            if (_camera == null) return;
            _camera.CameraMovement.GoToHome();
        }

        public override void SetHome()
        {
            if (_camera == null) return;
            _camera.CameraMovement.SetHome();
        }

        public override void AddPreset()
        {
            if (_camera == null) return;
            _camera.CameraMovement.AddPreset();
        }

        public override void RemovePreset(string preset)
        {
            if (_camera.CameraMovement.GetPresets().Count() == 1)
                _camera.CameraMovement.ClearPresets();
            else
                _camera.CameraMovement.RemovePreset(preset);
        }

        public override void MoveToPreset(string preset)
        {
            _camera.CameraMovement.MoveToPreset(preset);
        }

        public override void Patrol(PatrolDirection direction, double time)
        {
            _camera.CameraMovement.Patrol(direction, time);
        }

        public override void SetCameraSpeeds(CameraSpeed speed, float value)
        {
            switch (speed)
            {
                case CameraSpeed.Pan:
                    _camera.CameraMovement.PanSpeed = value;
                    break;
                case CameraSpeed.Tilt:
                    _camera.CameraMovement.TiltSpeed = value;
                    break;
                case CameraSpeed.Zoom:
                    _camera.CameraMovement.ZoomSpeed = value;
                    break;
            }
        }

        public override void StartCaptionVideo(string path)
        {
            var date = DateTime.Now.Year + "y-" + DateTime.Now.Month + "m-" + DateTime.Now.Day + "d-" +
                       DateTime.Now.Hour + "h-" + DateTime.Now.Minute + "m-" + DateTime.Now.Second + "s";

            string currentpath;
            if (String.IsNullOrEmpty(path))
                currentpath = AppDomain.CurrentDomain.BaseDirectory + date + ".mp4";
            else
                currentpath = path + "\\" + date + ".mp4";

            MotionDirectoryPath = path;

            MotionFilePath = currentpath;

            Mpeg4Recorder = new MPEG4Recorder(currentpath);
            Mpeg4Recorder.MultiplexFinished += Mpeg4RecorderMultiplexFinished;

            Connector.Connect(_camera.AudioChannel, Mpeg4Recorder.AudioRecorder);
            Connector.Connect(_camera.VideoChannel, Mpeg4Recorder.VideoRecorder);

            Log.Motion("Video capture has been started");
            Log.Write("The captured video will be saved at the location: " + currentpath);
        }

        void Mpeg4RecorderMultiplexFinished(object sender, VoIPEventArgs<bool> e)
        {
            Mpeg4Recorder.Dispose();
            Mpeg4Recorder.MultiplexFinished -= Mpeg4RecorderMultiplexFinished;

            OnGetFilePath(new VoIPEventArgs<string>(MotionFilePath));

            Log.Write("The captured video has been saved");
        }

        public override void StopCaptionVideo()
        {
            if (Mpeg4Recorder == null)
                return;

            Connector.Disconnect(_camera.AudioChannel, Mpeg4Recorder.AudioRecorder);
            Connector.Disconnect(_camera.VideoChannel, Mpeg4Recorder.VideoRecorder);

            Mpeg4Recorder.Multiplex();

            Log.Motion("Video capture has been stopped");
        }

        protected override void Detector_MotionDetection(object sender, MotionDetectionEvent e)
        {
            switch (e.Detection)
            {
                case true:
                    if (IsVideoCaptured)
                        return;

                    OnAlarmEvent();

                    Log.Motion("Motion detected");

                    StartCaptionVideo(MotionDirectoryPath);
                    IsVideoCaptured = true;

                    MotionTimer = new Timer();
                    MotionTimer.Elapsed += ElapsedMotion;
                    MotionTimer.Interval = Duration * 1000;
                    MotionTimer.AutoReset = false;
                    MotionTimer.Start();

                    break;

                case false:
                    Log.Motion("Motion stopped");
                    break;
            }
        }

        protected override void VadFilterVoiceDetected(object sender, EventArgs e)
        {
            if (IsSoundDetected)
                return;

            VadFilter.Enabled = false;
            var date = DateTime.Now.Year + "y-" + DateTime.Now.Month + "m-" + DateTime.Now.Day + "d-" +
                      DateTime.Now.Hour + "h-" + DateTime.Now.Minute + "m-" + DateTime.Now.Second + "s";

            string currentpath;
            if (String.IsNullOrEmpty(MotionDirectoryPath))
                currentpath = AppDomain.CurrentDomain.BaseDirectory + date + ".wav";
            else
                currentpath = MotionDirectoryPath + "\\" + date + ".wav";
            SoundFilePath = currentpath;
            WaveStreamRecorder = new WaveStreamRecorder(currentpath);

            Connector.Connect(_camera.AudioChannel, WaveStreamRecorder); // connects the voices to the recorder

            SoundTimer = new Timer();
            SoundTimer.Elapsed += ElapsedVoice;
            SoundTimer.Interval = Duration * 1000;
            SoundTimer.AutoReset = false;

            SoundTimer.Start();
            WaveStreamRecorder.Start();
            IsSoundDetected = true;
            Log.Motion("Sound detected");
            Log.Write("The detected sound will be saved at the location: " + currentpath);
        }

        protected override void ElapsedVoice(object sender, EventArgs eventArgs)
        {
            if (SoundTimer != null)
            {
                SoundTimer.Stop();
                SoundTimer.Elapsed -= ElapsedVoice;
                SoundTimer.Dispose();
            }
            Connector.Disconnect(_camera.AudioChannel, WaveStreamRecorder);

            WaveStreamRecorder.Stop();

            WaveStreamRecorder.Dispose();

            VadFilter.Enabled = true;
            IsSoundDetected = false;
            Log.Motion("Sound recording has stopped");

            OnGetFilePath(new VoIPEventArgs<string>(SoundFilePath));

        }

        public override void StartVoiceDetection(string path)
        {
            VadFilter.Enabled = true;
            Log.Motion("Sound detection has been started");

            Connector.Connect(_camera.AudioChannel, VadFilter);

            MotionDirectoryPath = path;
        }

        public override void StopVoiceDetection()
        {
            VadFilter.Enabled = false;
            Connector.Disconnect(_camera.AudioChannel, VadFilter);

            Log.Motion("Sound detection has been stopped");
        }

        public override void SetTime(TimeZoneInfo zone)
        {
            var time = DateTime.UtcNow;
            var config = new CameraDateTimeSetter(true)
            {
                TimeZoneInfo = zone,
                UTCDate = new IPCameraDate(time.Year, time.Month, time.Day),
                UTCTime = new IPCameraTime(time.Hour, time.Minute, time.Second)
            };
            _camera.DateAndTime.SetAttributes(config);
            _camera.DateAndTime.RefreshProperties();
            Log.Write("Camera time changed: " + _camera.DateAndTime.ToString());
        }

        public override void SetTimeManually(DateTime time)
        {
            var config = new CameraDateTimeSetter(true)
            {
                TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("UTC"),
                UTCDate = new IPCameraDate(time.Year, time.Month, time.Day),
                UTCTime = new IPCameraTime(time.Hour, time.Minute, time.Second)
            };
            _camera.DateAndTime.SetAttributes(config);
            _camera.DateAndTime.RefreshProperties();
            Log.Write("Camera time changed: " + _camera.DateAndTime.ToString());
        }

        public override void Close()
        {
            Disconnect();

            _camera.CameraStateChanged -= Camera_CameraStateChanged;
            _camera.CameraErrorOccurred -= Camera_CameraErrorOccurred;

            _camera.Disconnect();
            _camera.Dispose();

            base.Close();
        }
    }
}
