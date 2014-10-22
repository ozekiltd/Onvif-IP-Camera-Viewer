using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Timers;
using Onvif_IP_Camera_Manager.LOG;
using Ozeki.Media.IPCamera;
using Ozeki.Media.IPCamera.Imaging;
using Ozeki.Media.IPCamera.Media;
using Ozeki.Media.IPCamera.Network;
using Ozeki.Media.IPCamera.PTZ;
using Ozeki.Media.IPCamera.UserManagement;
using Ozeki.Media.MediaHandlers;
using Ozeki.Media.MediaHandlers.IPCamera;
using Ozeki.Media.MediaHandlers.Video;
using Ozeki.VoIP;

namespace Onvif_IP_Camera_Manager.Model
{
    public class Camera : INotifyPropertyChanged
    {
        string _cameraState;
        string _cameraError;

        public int Duration { get; set; }
        public BitmapSourceProvider BitmapSourceProvider { get; private set; }

        public event EventHandler<CameraStateEventArgs> CameraStateChanged;
        public event EventHandler<CameraErrorEventArgs> CameraErrorOccurred;
        public event EventHandler<VoIPEventArgs<string>> GetFilePath;

        public virtual List<ICustomInfo> CustomInfos { get; set; }
        public virtual string AudioStreamInfo { get; set; }
        public virtual string VideoStreamInfo { get; set; }
        public virtual string DeviceInfo { get; set; }
        public virtual IPCameraStream[] CameraStreams { get; set; }
        public virtual IPCameraStream CurrentStream { get; set; }
        public virtual object CameraObject { get; set; }
        public virtual IVideoSender VideoSender { get; set; }
        public virtual IAudioSender AudioSender { get; set; }
        public virtual CameraUriType? UriType { get; set; }
        public virtual IPCameraPreset[] CameraPresets { get; set; }
        public virtual ICameraImaging CameraImage { get; set; }
        public virtual ICameraNetworkManager Network { get; set; }
        public virtual IUserManager UserManager { get; set; }
        public virtual string CameraInfo { get; set; }
        public virtual List<ICameraUser> GetCameraUsers { get; set; }
        public virtual bool IsDiscoverable { get; set; }
        public virtual string CameraName { get; set; }
        public virtual string CameraLocation { get; set; }

        public virtual event EventHandler<EventArgs> AlarmEvent;

        public virtual void SetCameraImaging(CameraImaging config) { }
        public virtual void SetVideoEncoding(IPCameraVideoEncoding encoding) { }
        public virtual void SetCameraCustomInfo(CustomInfo[] customInfos) { }
        public virtual void Move(string direction) { }
        public virtual void StopMove() { }
        public virtual void GoToHome() { }
        public virtual void SetHome() { }
        public virtual void AddPreset() { }
        public virtual void RemovePreset(string preset) { }
        public virtual void MoveToPreset(string preset) { }
        public virtual void Patrol(PatrolDirection direction, double time) { }
        public virtual void SetCameraSpeeds(CameraSpeed speed, float value) { }
        public virtual void StartVoiceDetection(string path) { }
        public virtual void StopVoiceDetection() { }
        public virtual void SetTime(TimeZoneInfo zone) { }
        public virtual void SetTimeManually(DateTime time) { }
        public virtual void SetDiscoverable(bool isDiscoverAble) { }
        public virtual void Start(IPCameraStream stream) { }
        public virtual void StartCaptionVideo(string path) { }
        public virtual void StopCaptionVideo() { }

        protected string SoundFilePath { get; set; }
        protected string MotionDirectoryPath { get; set; }
        protected string MotionFilePath { get; set; }
        protected MPEG4Recorder Mpeg4Recorder { get; set; }
        protected WaveStreamRecorder WaveStreamRecorder { get; set; }
        protected SnapshotHandler Snapshot { get; private set; }
        protected Timer MotionTimer { get; set; }
        protected Timer SoundTimer { get; set; }
        protected MotionDetector Detector { get; private set; }
        protected VADFilter VadFilter { get; private set; }
        protected MediaConnector Connector { get; private set; }
        protected bool IsVideoCaptured { get; set; }
        protected bool IsSoundDetected { get; set; }

        protected virtual void VadFilterVoiceDetected(object sender, EventArgs e) { }
        protected virtual void ElapsedVoice(object sender, EventArgs eventArgs) { }
        protected virtual void Detector_MotionDetection(object sender, MotionDetectionEvent e) { }

        public Camera()
        {
            Detector = new MotionDetector();
            BitmapSourceProvider = new BitmapSourceProvider();
            Connector = new MediaConnector();
            VadFilter = new VADFilter { Enabled = false, ActivationLevel = 40 };
            Snapshot = new SnapshotHandler();

            VadFilter.VoiceDetected += VadFilterVoiceDetected;
            Duration = 10;
        }

        public virtual string CameraState
        {
            get { return _cameraState; }
            set
            {
                _cameraState = value;
                OnPropertyChanged("CameraState");
            }
        }

        public virtual string CameraError
        {
            get { return _cameraError; }
            set
            {
                _cameraError = value;
                OnPropertyChanged("CameraError");
            }
        }
        
        public virtual bool Connect()
        {
            return false;
        }

        public virtual void Disconnect()
        {
            Connector.Dispose();
        }

        public virtual void Close()
        {
            BitmapSourceProvider.Dispose();

            if (Mpeg4Recorder != null)
                Mpeg4Recorder.Dispose();

            Detector.MotionDetection -= Detector_MotionDetection;
            Detector.Dispose();

            VadFilter.VoiceDetected -= VadFilterVoiceDetected;
            VadFilter.Dispose();

            if (SoundTimer != null)
            {
                SoundTimer.Elapsed -= ElapsedVoice;
                SoundTimer.Stop();
                SoundTimer.Dispose();
            }

            if (MotionTimer != null)
            {
                MotionTimer.Elapsed -= ElapsedVoice;
                MotionTimer.Stop();
                MotionTimer.Dispose();
            }
        }

        public virtual void TakeSnapshot(string path)
        {
            var snapshot = Snapshot.TakeSnapshot();
            if (snapshot == null)
                return;

            if (path == null)
                path = "";

            var date = DateTime.Now.ToString("u").Replace(':', '-');
            string filename = string.Format("{0}.jpg", date);
            path = Path.Combine(path, filename);

            var image = snapshot.ToImage();
            if (image != null)
            {
                image.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                Log.Write("Picture saved at the location: " + path);

                OnGetFilePath(new VoIPEventArgs<string>(path));
            }
        }

        public void ChangePixelSensitivity(int value)
        {
            Detector.PixelIntensitySensitivy = value;
        }

        public void ChangeAmountSensitivity(double value)
        {
            Detector.PixelAmountSensitivy = value;
        }

        public void StartMotionDetection(string path)
        {
            Detector.HighlightMotion = HighlightMotion.Highlight;
            Detector.MotionColor = MotionColor.Red;
            Detector.MotionDetection += Detector_MotionDetection;
            Detector.Start();

            MotionDirectoryPath = path;


            Log.Motion("Motion detection has been started");
        }

        public void StopMotionDetection()
        {
            Detector.Stop();
            Detector.MotionDetection -= Detector_MotionDetection;
            Detector.Dispose();

            Log.Motion("Motion detection has been stopped");
        }

        public void GetMotionValues(ref double amount, ref double intensity)
        {
            amount = Detector.PixelAmountSensitivy;
            intensity = Detector.PixelIntensitySensitivy;
        }

        protected virtual void OnGetFilePath(VoIPEventArgs<string> e)
        {
            var handler = GetFilePath;
            if (handler != null) handler(this, e);
        }

        protected void ElapsedMotion(object sender, EventArgs eventArgs)
        {
            var timer = sender as Timer;
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            StopCaptionVideo();
            IsVideoCaptured = false;
        }

        protected void OnCameraErrorOccurred(CameraErrorEventArgs e)
        {
            if (e.Details != null)
            {
                CameraError = e.Details;
                Log.Write("Camera error: " + e.Details);
            }
            else
            {
                CameraError = e.Error.ToString();
                Log.Write("Camera error: " + CameraError);
            }

            var handler = CameraErrorOccurred;
            if (handler != null)
                handler(this, e);
        }

        protected void OnCameraStateChanged(CameraStateEventArgs e)
        {
            CameraState = e.State.ToString();
            Log.Write("Camera state: " + CameraState);

            var handler = CameraStateChanged;
            if (handler != null)
                handler(this, e);
        }

        protected void OnAlarmEvent()
        {
            var handler = AlarmEvent;
            if (handler != null)
                handler(this, new EventArgs());
        }

        public enum CameraSpeed
        {
            Pan,
            Tilt,
            Zoom
        };

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
