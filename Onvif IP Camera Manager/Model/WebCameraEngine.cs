using System;
using System.Text;
using System.Timers;
using Onvif_IP_Camera_Manager.LOG;
using Ozeki.Media.IPCamera;
using Ozeki.Media.MediaHandlers;
using Ozeki.Media.MediaHandlers.Video;
using Ozeki.Media.Video;
using Ozeki.VoIP;

namespace Onvif_IP_Camera_Manager.Model
{
    class WebCameraEngine : Camera
    {
        private Microphone _microphone;
        private WebCamera _camera;

        public override IAudioSender AudioSender { get { return _microphone; } }

        public override IVideoSender VideoSender { get { return _camera; } }

        public override CameraUriType? UriType { get { return null; } }

        public override string CameraInfo { get { return _camera.DeviceName; } }

        public override event EventHandler<EventArgs> AlarmEvent;

        public WebCameraEngine(VideoDeviceInfo device)
        {
            _camera = WebCamera.GetDevice(device);
            _microphone = Microphone.GetDefaultDevice();
        }

        public override bool Connect()
        {
            //try
            //{
            if (_camera != null)
            {
                _camera.Start();
                Connector.Connect(_camera, Detector);
                Connector.Connect(Detector, BitmapSourceProvider);
                Connector.Connect(_camera, Snapshot);

                CameraState = "Streaming";
                Log.Write("USB camera connected");
                return true;
            }

            CameraState = "Disconnected";
            Log.Write("Cannot access USB camera");
            return false;
            //}
            //catch (Exception ex)
            //{
            //    CameraState = "Disconnected";
            //    return false;
            //}
        }

        public override void Disconnect()
        {
            if (_camera != null)
            {
                _camera.Stop();

                Connector.Disconnect(_camera, Detector);
                Connector.Disconnect(Detector, BitmapSourceProvider);
                Connector.Disconnect(_camera, Snapshot);

                _camera.Stop();

                CameraState = "Disconnected";

                Log.Write("USB camera disconnected");
                base.Disconnect();
            }
        }

        public override string DeviceInfo
        {
            get
            {
                if (_camera == null) return String.Empty;
                var info = new StringBuilder();
                info.AppendLine("Device name: " + _camera.DeviceName);
                info.AppendLine("Camera ID: " + _camera.ID);
                info.AppendLine("Capabilities: ");
                foreach (var item in _camera.Capabilities)
                {
                    info.AppendLine("\t Max Frame rate - " + item.MaxFrameRate);
                    info.AppendLine("\t Resolution - " + item.Resolution);
                }
                info.AppendLine("Capturing: " + _camera.Capturing);
                info.AppendLine("Current frame rate: " + _camera.CurrentFrameRate);
                info.AppendLine("Desired frame rate: " + _camera.DesiredFrameRate);
                info.AppendLine("Resolution: " + _camera.Resolution);

                return info.ToString();
            }
            set { }
        }

        public override void StartCaptionVideo(string path)
        {
            if (_camera == null) return;

            var date = DateTime.Now.Year + "y-" + DateTime.Now.Month + "m-" + DateTime.Now.Day + "d-" +
                       DateTime.Now.Hour + "h-" + DateTime.Now.Minute + "m-" + DateTime.Now.Second + "s";

            string currentpath;
            if (String.IsNullOrEmpty(path))
                currentpath = AppDomain.CurrentDomain.BaseDirectory + date + ".mp4";
            else
                currentpath = path + "\\" + date + ".mp4";

            Mpeg4Recorder = new MPEG4Recorder(currentpath);
            MotionFilePath = currentpath;

            _microphone.Start();
            Connector.Connect(_microphone, Mpeg4Recorder.AudioRecorder);
            Connector.Connect(_camera, Mpeg4Recorder.VideoRecorder);

            Log.Motion("Video capture has been started");
            Log.Write("The captured video will be saved at the location: " + currentpath);
        }

        void Mpeg4RecorderMultiplexFinished(object sender, VoIPEventArgs<bool> e)
        {
            Mpeg4Recorder.Dispose();
            _microphone.Dispose();

            OnGetFilePath(new VoIPEventArgs<string>(MotionFilePath));
            MotionFilePath = String.Empty;

            Mpeg4Recorder.MultiplexFinished -= Mpeg4RecorderMultiplexFinished;
        }

        public override void StopCaptionVideo()
        {
            if (_camera == null || Mpeg4Recorder == null) return;

            Connector.Disconnect(_microphone, Mpeg4Recorder.AudioRecorder);
            Connector.Disconnect(_camera, Mpeg4Recorder.VideoRecorder);

            Mpeg4Recorder.MultiplexFinished += Mpeg4RecorderMultiplexFinished;
            Mpeg4Recorder.Multiplex();

            Log.Motion("Video capture has been stopped");
        }

        protected override void Detector_MotionDetection(object sender, MotionDetectionEvent e)
        {
            switch (e.Detection)
            {
                case true:
                    if (IsVideoCaptured) return;

                    var handler = AlarmEvent;
                    if (handler != null)
                        handler(this, new EventArgs());

                    Log.Motion("Motion detected");

                    StartCaptionVideo(MotionDirectoryPath);
                    IsVideoCaptured = true;

                    var timer = new Timer();
                    timer.Elapsed += ElapsedMotion;
                    timer.Interval = Duration * 1000;
                    timer.AutoReset = false;
                    timer.Start();

                    break;

                case false:
                    Log.Motion("Motion stopped");
                    break;
            }
        }

        protected override void VadFilterVoiceDetected(object sender, EventArgs e)
        {
            if (IsSoundDetected) return;

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

            Connector.Connect(_microphone, WaveStreamRecorder); // connects the voices to the recorder

            SoundTimer = new Timer();
            SoundTimer.Elapsed += ElapsedVoice;
            SoundTimer.Interval = Duration * 1000;
            SoundTimer.AutoReset = false;

            _microphone.Start();
            SoundTimer.Start();
            WaveStreamRecorder.Start();
            IsSoundDetected = true;
            Log.Motion("Sound detected");
        }

        protected override void ElapsedVoice(object sender, EventArgs eventArgs)
        {
            if (SoundTimer != null)
            {
                SoundTimer.Stop();
                SoundTimer.Elapsed -= ElapsedVoice;
                SoundTimer.Dispose();
            }
            Connector.Disconnect(_microphone, WaveStreamRecorder);
            _microphone.Stop();
            _microphone.Dispose();

            WaveStreamRecorder.Stop();

            WaveStreamRecorder.Dispose();

            VadFilter.Enabled = true;
            IsSoundDetected = false;
            Log.Motion("Sound recording has stopped");

            OnGetFilePath(new VoIPEventArgs<string>(SoundFilePath));
            SoundFilePath = String.Empty;
        }

        public override void StartVoiceDetection(string path)
        {
            VadFilter.Enabled = true;
            Log.Motion("Sound detection has been started");

            Connector.Connect(_microphone, VadFilter);

            MotionDirectoryPath = path;
        }

        public override void StopVoiceDetection()
        {
            VadFilter.Enabled = false;
            Connector.Disconnect(_microphone, VadFilter);
            Log.Motion("Sound detection has been stopped");
        }
    }
}
