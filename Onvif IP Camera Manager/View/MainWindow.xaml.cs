using Microsoft.Win32;
using Onvif_IP_Camera_Manager.LOG;
using Onvif_IP_Camera_Manager.Model;
using Onvif_IP_Camera_Manager.Model.Data;
using Ozeki.Common;
using Ozeki.Media.IPCamera;
using Ozeki.Media.IPCamera.PTZ;
using Ozeki.Media.IPCamera.UserManagement;
using Ozeki.Media.MediaHandlers.IPCamera;
using Ozeki.Media.Video;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Onvif_IP_Camera_Manager.Service;
using Ozeki.MediaGateway.Config;
using Ozeki.Network;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        object _sync;

        Camera _currentModel;
        MyMediaGateway _mediaGateway;
        Ftp _ftp;
        Email _email;
        List<CameraViewerControl> _videoViewerList;
        List<ImageSetting> _imageSliders;
        object _currentTimeZone;
        string _userName;
        string _password;
        string _cameraAddress;

        bool _connecting;

        public bool IsStreamable
        {
            get
            {
                return CurrentModel != null && CurrentModel.CameraState.ToUpper().Contains("STREAMING");
            }
            set { }
        }

        public bool IsIPCamera
        {
            get
            {
                return !(CurrentModel == null
                    || CurrentModel.UriType == null
                    || CurrentModel.UriType == CameraUriType.RTSP
                    || String.IsNullOrEmpty(CurrentModel.CameraState)
                    || (!String.IsNullOrEmpty(CurrentModel.CameraState) && !CurrentModel.CameraState.ToUpper().Contains("STREAMING")));
            }
        }

        public bool IsImagingSupported
        {
            get
            {
                return !(CurrentModel == null
                    || CurrentModel.CameraImage == null
                    || String.IsNullOrEmpty(CurrentModel.CameraState)
                    || (!String.IsNullOrEmpty(CurrentModel.CameraState) && !CurrentModel.CameraState.ToUpper().Contains("STREAMING")));
            }
        }

        public MyServer Server { get; private set; }
        public ConnectModel ConnectModel { get; set; }
        public CameraUser CameraUser { get; set; }
        public string Password2 { get; set; }
        public CameraUserLevel Role { get; set; }
        public SoftphoneEngine SoftPhone { get; set; }
        public AccountModel AccountModel { get; set; }
        public ObservableList<Camera> ModelList { get; private set; }
        public string DialAdd { get; set; }
        public string CapturePath { get; set; }
        public bool UseUTC { get; set; }
        public DateTime SelectedDate { get; set; }
        public string SelectedTime { get; set; }

        public string CameraAddress
        {
            get { return _cameraAddress; }
            set
            {
                _cameraAddress = value;
                ConnectModel.Url = _cameraAddress;
                OnPropertyChanged("CameraAddress");
            }
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                InvokeGuiThread(() =>
                {
                    _userName = value;
                    ConnectModel.User = _userName;
                    OnPropertyChanged("UserName");
                });
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                InvokeGuiThread(() =>
                {
                    _password = value;
                    ConnectModel.Password = _password;
                    OnPropertyChanged("Password");
                });
            }
        }

        public Camera CurrentModel
        {
            get { return _currentModel; }
            set
            {
                try
                {
                    _currentModel = value;

                    StartViewer(value);
                    SetMotionSensitivity();
                    if (value != null)
                        SoftPhone.SetModel(value.AudioSender, value.VideoSender);

                    try
                    {
                        if (_imageSliders != null)
                            foreach (var slider in _imageSliders)
                                slider.Model = value;
                    }
                    catch
                    {
                    }

                    OnPropertyChanged("CurrentModel");
                }
                catch (Exception ex)
                {

                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            try
            {
                ModelList = new ObservableList<Camera>();

                AccountModel = new AccountModel
                {
                    RegistrationRequired = true
                };

                try
                {
                    SoftPhone = new SoftphoneEngine();
                    SoftPhone.Softphone.IncomingCall += Softphone_IncomingCall;
                }
                catch (Exception ex)
                {
                }

                Server = new MyServer();
                Server.ClientCountChange += _server_ClientCountChange;

                ConnectModel = new ConnectModel();
                ConnectModel.DeviceAdded += ConnectModel_DeviceAdded;

                InitializeComponent();

                ConnectModel.DiscoverDevices();

                _imageSliders = new List<ImageSetting>
                {
                    BrightnessSlider,
                    ContrastSlider,
                    SaturationSlider, 
                    SharpnessSlider, 
                    BackLightSlider, 
                    WhiteBalanceCbSlider, 
                    WhiteBalanceCrSlider, 
                    FrameRateSlider
                };

                _videoViewerList = new List<CameraViewerControl>
                {
                    Viewer1,
                    Viewer2,
                    Viewer3,
                    Viewer4
                };

                UseUTC = true;
                OnPropertyChanged("UseUTC");
                SelectedDate = DateTime.Now;
                OnPropertyChanged("SelectedDate");
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error occurred: {0}, {1}", e.GetType(), e.Message));
                Environment.Exit(-1);
            }
        }

        void ConnectModel_DeviceAdded(object sender, DeviceAddedEventArgs e)
        {
            // add webcam
            if (e.Info.WebCameraInfo != null)
            {
                var createdCamera = new WebCameraEngine(e.Info.WebCameraInfo);
                AddCamera(createdCamera);
                //CurrentModel = createdCamera;
            }
            else if (e.Info.IpCameraInfo != null)
            {
                // add IP camera
                if (e.Info.IpCameraInfo.Uri == null)
                    return;

                var ipCamera = new IpCameraEngine(e.Info.IpCameraInfo.Uri.ToString(), UserName, Password);

                if (CurrentModel != null && CurrentModel.CameraInfo.Equals(ipCamera.CameraInfo))
                    CurrentModel = null;

                AddCamera(ipCamera);
                // CurrentModel = ipCamera;
            }
        }

        void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectModel.DiscoverDevices();
            }
            catch (Exception ex)
            {
            }
        }

        void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddCameraWindow(ConnectModel);
            addWindow.CameraAdded += addWindow_CameraAdded;
            var success = addWindow.ShowDialog();

            if (success == null || success != true)
                return;
        }

        void addWindow_CameraAdded(object sender, CameraAddedEventArgs e)
        {
            ModelList.Add(e.Camera);
        }

        void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ConnectModel == null || _connecting)
                    return;

                if (CurrentModel == null)
                    CurrentModel = GetCurrentModel((CameraDeviceInfo)CameraDevicesCombo.SelectedItem);

                if (CurrentModel == null ||
                    (!String.IsNullOrEmpty(CurrentModel.CameraState) && !CurrentModel.CameraState.Equals("Disconnected")))
                    return;

                if (CurrentModel is IpCameraEngine)
                {
                    if (!IsSavedCamera(CurrentModel))
                        CurrentModel = new IpCameraEngine(CameraAddress, UserName, Password);
                    //else
                    //    CurrentModel = GetSavedCamera(CurrentModel).Camera;
                }

                AddCamera(CurrentModel);
                WireUpCameraEvents(CurrentModel);

                if (CurrentModel.CameraError != null && CurrentModel.CameraError.ToUpper().Contains("LOST"))
                    CurrentModel.CameraError = String.Empty;

                CurrentModel.Connect();
                _connecting = true;

                StartViewer(CurrentModel);

                Server.Model = CurrentModel;

                if (CurrentModel.CameraStreams == null)
                    ModelList.Add(CurrentModel);
            }
            catch
            {
            }
        }

        void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        void Disconnect()
        {
            if (CurrentModel == null)
                return;

            CurrentModel.Disconnect();

            var succ = ConnectModel.RemoveCamera(CurrentModel);

            WireDownCameraEvents(CurrentModel);

            StopViewer();
        }

        void AddCamera(Camera camera)
        {
            ConnectModel.CameraList.Add(camera);

            ControlSizeToTextboxes();
        }

        void Model_CameraStateChanged(object sender, CameraStateEventArgs e)
        {
            if (!(e.State == IPCameraState.Streaming || e.State == IPCameraState.Disconnected))
                return;

            _connecting = false;

            switch (e.State)
            {
                case IPCameraState.Streaming:
                    InitMediaGateway();
                    var camToAdd = (Camera)sender;

                    if (!CameraListContains(ModelList, camToAdd))
                        ModelList.Add(camToAdd);

                    if (!IsSavedCamera(camToAdd))
                        ConnectModel.SavedCameras.Add(new SavedCamera { Camera = camToAdd, UserName = UserName, Password = Password });

                    if (CurrentModel.CustomInfos == null)
                        return;

                    InvokeGuiThread(() =>
                    {
                        foreach (var item in CurrentModel.CustomInfos)
                        {
                            if (item.Name == "ImageSettingName")
                                CurrentModel.CameraName = item.Value;
                            if (item.Name == "Location")
                                CurrentModel.CameraLocation = item.Value;
                        }

                        DiscoverAbleCheckbox.IsChecked = CurrentModel.IsDiscoverable;

                        try
                        {
                            foreach (var slider in _imageSliders)
                                slider.Model = CurrentModel;
                        }
                        catch (Exception ex)
                        {
                        }
                    });
                    break;

                case IPCameraState.Disconnected:
                    break;
            }

            OnPropertyChanged("IsStreamable");
            OnPropertyChanged("IsIPCamera");
            OnPropertyChanged("IsImagingSupported");
            OnPropertyChanged("CurrentModel");
        }

        void WireUpCameraEvents(Camera camera)
        {
            camera.CameraStateChanged += Model_CameraStateChanged;
            camera.GetFilePath += Model_GetFilePath;
            camera.AlarmEvent += Model_AlarmEvent;
        }

        void WireDownCameraEvents(Camera camera)
        {
            camera.CameraStateChanged -= Model_CameraStateChanged;
            camera.GetFilePath -= Model_GetFilePath;
            camera.AlarmEvent -= Model_AlarmEvent;
        }

        void _server_ClientCountChange(object sender, EventArgs e)
        {
            OnPropertyChanged("Server");
        }

        void Softphone_IncomingCall(object sender, Ozeki.VoIP.VoIPEventArgs<Ozeki.VoIP.IPhoneCall> e)
        {
            if (_currentModel == null)
                return;

            SoftPhone.SetModel(_currentModel.AudioSender, _currentModel.VideoSender);
        }

        void InitMediaGateway()
        {
            if (_mediaGateway != null)
                return;

            var config = new MediaGatewayConfig();
            config.AddConfigElement(new FlashConfig { ServiceName = "IPCameraServer" });

            _mediaGateway = new MyMediaGateway(config, _currentModel);
            _mediaGateway.Start();
        }

        void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DirectionComboBox.ItemsSource = Enum.GetValues(typeof(PatrolDirection));
            CurrentTimeZone = TimeZoneInfo.Local;
        }

        void StartButton(object sender, RoutedEventArgs e)
        {
            Server.Start();
            Server.SetListenAddress(Server.IpAddress, Server.Port);
            OnPropertyChanged("Server");
        }

        void StopButton(object sender, RoutedEventArgs e)
        {
            Server.Stop();
            OnPropertyChanged("Server");
        }

        void ControlSizeToTextboxes()
        {
            InvokeGuiThread(() =>
            {
                HeightTextBox.Text = ((int)videoViewer.RenderSize.Height).ToString(CultureInfo.InvariantCulture);
                HeightTextBox.Text = ((int)videoViewer.RenderSize.Height).ToString(CultureInfo.InvariantCulture);
                WidthTextBox.Text = ((int)videoViewer.RenderSize.Width).ToString(CultureInfo.InvariantCulture);
            });
        }

        void StartViewer(Camera camera)
        {
            if (videoViewer == null)
                return;
            if (camera == null)
            {
                StopViewer();
                return;
            }

            videoViewer.SetImageProvider(camera.BitmapSourceProvider);
            videoViewer.Start();
        }

        void StopViewer()
        {
            if (videoViewer == null) return;
            videoViewer.Stop();
            videoViewer.Dispose();
        }

        void Model_AlarmEvent(object sender, EventArgs e)
        {
            if (_currentModel != null)
                SoftPhone.SetModel(_currentModel.AudioSender, _currentModel.VideoSender);

            InvokeGuiThread(() =>
            {
                if (AlarmCallCheckBox.IsChecked != null && (bool)AlarmCallCheckBox.IsChecked)
                {
                    if (SoftPhone != null)
                    {
                        Task.Factory.StartNew(SoftPhone.StartAlarmCalls);
                    }
                }
            });
        }

        void Model_GetFilePath(object sender, Ozeki.VoIP.VoIPEventArgs<string> e)
        {
            InvokeGuiThread(() =>
            {
                if (SendCapturedFileEmailCheckBox.IsChecked != null && (bool)SendCapturedFileEmailCheckBox.IsChecked)
                {
                    Task.Factory.StartNew(SendEmailThread(e.Item));
                }

                if (UploadCapturedFileFtpCheckBox.IsChecked != null && (bool)UploadCapturedFileFtpCheckBox.IsChecked)
                {
                    Task.Factory.StartNew(UploadFtpThread(e.Item));
                }

                MessageBox.Show("Snapshot saved to " + e.Item, "Snapshot");
            });
        }

        Action UploadFtpThread(string file)
        {
            return () =>
            {
                if (_ftp == null) return;
                Log.Write("FTP uploading...");
                var words = file.Split('\\');

                _ftp.Upload(file, "etc/" + words[words.Count() - 1]);
            };
        }

        Action SendEmailThread(string file)
        {
            return () =>
            {
                if (_email == null) return;
                Log.Write("Email sending...");
                _email.SendEmail(file);
            };
        }

        void InvokeGuiThread(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }

        bool CameraListContains(ObservableList<Camera> list, Camera cam)
        {
            foreach (var camera in list)
            {
                if (camera.CameraInfo.Equals(cam.CameraInfo) || String.Format("{0}/", camera.CameraInfo).Equals(cam.CameraInfo))
                    return true;
            }

            return false;
        }

        #region About

        void btnAboutOzeki_Click(object sender, RoutedEventArgs e)
        {
            var box = new AboutWindow(this);
            box.ShowDialog();
        }

        void btnProjects_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var myDocuments =
                Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ozeki"),
                    "Ozeki SDK\\Examples");

                OpenUrl(myDocuments);
            }
            catch (Exception)
            {
                OpenUrl("http://www.camera-sdk.com/p_14-online-manual-onvif.html");
            }

        }

        void btnWebstream_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("http://" + NetworkAddressHelper.GetLocalIP() + "/bin-debug/Main.html");
        }

        void btnWebsite_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("http://www.camera-sdk.com");
        }

        void btnPurchase_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("http://www.camera-sdk.com/p_11-product-onvif.html");
        }

        void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpPath = (GetInstallDir() + "\\Documentation\\index.html");
                OpenUrl(helpPath);
            }
            catch (Exception ex)
            {
                OpenUrl("http://www.camera-sdk.com");
            }

        }

        void OpenUrl(string url)
        {
            Process.Start(url);
        }

        string GetInstallDir()
        {
            try
            {
                string keyname;
                if (IntPtr.Size == 4)
                {
                    keyname = "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Ozeki SDK";
                }
                else
                {
                    keyname = "HKEY_LOCAL_MACHINE\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Ozeki SDK";
                }
                var path = (String)Registry.GetValue(keyname, "InstallDir", String.Empty);
                return path;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public object GetTimeZones
        {
            get { return TimeZoneInfo.GetSystemTimeZones(); }
        }

        public object CurrentTimeZone
        {
            get { return _currentTimeZone; }
            set
            {
                _currentTimeZone = value;
                OnPropertyChanged("CurrentTimeZone");
            }
        }

        public object GetCameraUsersLevel
        {
            get { return Enum.GetValues(typeof(CameraUserLevel)); }
        }

        #endregion

        void StreamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StreamComboBox.SelectedIndex == -1 || CurrentModel == null || CurrentModel.CameraState == IPCameraState.Disconnected.ToString())
                return;

            DeviceAudioInfo.Clear();
            DeviceVideoInfo.Clear();

            var currentStream = CurrentModel.CameraStreams[StreamComboBox.SelectedIndex];

            if (currentStream == null)
                return;

            Log.Write("Camera changed stream to " + currentStream.Name);

            CurrentModel.Start(currentStream);

            OnPropertyChanged("CurrentModel");
        }

        #region IMAGE SETTING

        void FlipCheck(object sender, RoutedEventArgs routedEventArgs)
        {
            var flippedX = (bool)HorizontalCheckBox.IsChecked;
            var flippedY = (bool)VerticalCheckBox.IsChecked;

            if (flippedX && flippedY)
            {
                videoViewer.FlipMode = FlipMode.FlipXY;
                return;
            }

            if (flippedX)
            {
                videoViewer.FlipMode = FlipMode.FlipX;
                return;
            }

            if (flippedY)
            {
                videoViewer.FlipMode = FlipMode.FlipY;
                return;
            }

            videoViewer.FlipMode = FlipMode.None;
        }
        #endregion

        void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (SoftPhone != null)
            {
                SoftPhone.CloseCall();
                SoftPhone.CloseAlarmCalls();
            }

            foreach (var camera in ModelList)
            {
                camera.Disconnect();
                camera.Close();
            }

            foreach (var viewer in _videoViewerList.Where(viewer => viewer.Model != null))
            {
                viewer.Model.Disconnect();
                viewer.Model.Close();
            }
        }

        void ApplySizeButton_Click(object sender, RoutedEventArgs e)
        {
            var minWidth = 100;
            var minHeight = 100;
            var maxWidth = (int)CameraBox.ActualWidth - 30;
            var maxHeight = (int)CameraBox.ActualHeight - 30;

            try
            {

                videoViewer.Width = MathHelper.Clamp(Int32.Parse(WidthTextBox.Text), minWidth, maxWidth);
                WidthTextBox.Text = videoViewer.Width.ToString(CultureInfo.InvariantCulture);

                videoViewer.Height = MathHelper.Clamp(Int32.Parse(HeightTextBox.Text), minHeight, maxHeight);
                HeightTextBox.Text = videoViewer.Height.ToString(CultureInfo.InvariantCulture);

            }
            catch (Exception exception)
            {
                Log.Write(exception.Message);
            }
        }

        void ControlMovement(object sender, MouseButtonEventArgs e)
        {
            if (CurrentModel == null)
                return;

            var button = sender as Button;
            if (button != null) CurrentModel.Move(button.Content.ToString());
        }

        void ControlStop(object sender, MouseButtonEventArgs e)
        {
            if (CurrentModel == null)
                return;

            CurrentModel.StopMove();
        }

        void HomeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            CurrentModel.GoToHome();
        }

        void SetHomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            CurrentModel.SetHome();
        }

        void AddPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            CurrentModel.AddPreset();
            OnPropertyChanged("CurrentModel");
        }

        void MoveToPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            var preset = (IPCameraPreset)PresetsComboBox.SelectedItem;
            if (PresetsComboBox.SelectedItem == null)
                return;

            CurrentModel.MoveToPreset(preset.Name);
        }

        void DeletePresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            var preset = (IPCameraPreset)PresetsComboBox.SelectedItem;
            if (preset == null)
                return;

            CurrentModel.RemovePreset(preset.Name);
            OnPropertyChanged("CurrentModel");
        }

        void ScanStartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentModel == null)
                    return;

                if (String.IsNullOrEmpty(DurationTextBox.Text))
                    return;

                var patrol = (PatrolDirection)DirectionComboBox.SelectedItem;
                var duration = double.Parse(DurationTextBox.Text);
                CurrentModel.Patrol(patrol, duration);
            }
            catch (Exception exception)
            {
                Log.Write(exception.Message);
            }
        }

        void ScanStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            CurrentModel.StopMove();
        }

        void SpeedPropertyChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CurrentModel != null)
            {
                var slider = sender as Slider;
                switch (slider.Name)
                {
                    case "PanSlider":
                        CurrentModel.SetCameraSpeeds(Camera.CameraSpeed.Pan, (float)slider.Value);
                        break;

                    case "TiltSlider":
                        CurrentModel.SetCameraSpeeds(Camera.CameraSpeed.Tilt, (float)slider.Value);
                        break;

                    case "ZoomSlider":
                        CurrentModel.SetCameraSpeeds(Camera.CameraSpeed.Zoom, (float)slider.Value);
                        break;
                }
            }
        }

        void FtpSettingButton_Click(object sender, RoutedEventArgs e)
        {
            var ftpsetting = new FTPWindow(_ftp);

            bool? ok = ftpsetting.ShowDialog();
            if (ok != null && ok == true)
            {
                _ftp = ftpsetting.Ftp;
            }
        }

        void EmailSettingButton_Click(object sender, RoutedEventArgs e)
        {
            var emailSetting = new EmailWindow(_email);

            bool? ok = emailSetting.ShowDialog();
            if (ok != null && ok == true)
            {
                _email = emailSetting.Email;
            }
        }

        void CaptureImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            CurrentModel.TakeSnapshot(CapturePath);
        }

        void StartCaptureVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            CurrentModel.StartCaptionVideo(CapturePath);
        }

        void StopCaptureVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;
            CurrentModel.StopCaptionVideo();
        }

        void FilePathButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new FolderBrowserDialog { SelectedPath = "C:\\" };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CapturePath = folderDialog.SelectedPath;
                OnPropertyChanged("CapturePath");
            }
        }

        void SetMotionSensitivity()
        {
            double amount = 0;
            double intensity = 0;

            if (CurrentModel == null)
                return;

            CurrentModel.GetMotionValues(ref amount, ref intensity);

            InvokeGuiThread(() =>
            {
                PixelIntensitySlider.Value = intensity;
                PixelAmountSlider.Value = amount * 100;
            });
        }

        void MotionPropertyValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            if (slider == null)
                return;

            switch (slider.Name)
            {
                case "PixelIntensitySlider":
                    CurrentModel.ChangePixelSensitivity((int)slider.Value);
                    break;

                case "PixelAmountSlider":
                    CurrentModel.ChangeAmountSensitivity(slider.Value / 100);
                    break;
            }
        }

        void Enable_Motion_Detection(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null) return;
            var checkbox = sender as CheckBox;
            OnPropertyChanged("CurrentModel");
            switch (checkbox.IsChecked)
            {
                case true:
                    CurrentModel.StartMotionDetection(CapturePath);
                    break;

                case false:
                    CurrentModel.StopMotionDetection();
                    break;
            }
        }

        void Enable_Sound_Detection(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null) return;
            var checkbox = sender as CheckBox;
            OnPropertyChanged("CurrentModel");
            switch (checkbox.IsChecked)
            {
                case true:
                    CurrentModel.StartVoiceDetection(CapturePath);
                    break;

                case false:
                    CurrentModel.StopVoiceDetection();
                    break;
            }
        }

        void SipRegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountModel == null)
                return;

            SoftPhone.RegisterSipAccount(AccountModel);
            OnPropertyChanged("SoftPhone");
        }

        void SipUnregistrationButton_Click(object sender, RoutedEventArgs e)
        {
            SoftPhone.UnregisterPhoneLine();
        }

        void CallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null)
                return;

            SoftPhone.SetModel(_currentModel.AudioSender, _currentModel.VideoSender);
            SoftPhone.StartCall(SoftPhone.SelectedDial);
        }

        void CallHangUpButton_Click(object sender, RoutedEventArgs e)
        {
            SoftPhone.CloseCall();
        }

        void SetPropertyButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            CurrentModel.SetCameraCustomInfo(new[]
            {
                new CustomInfo("ImageSettingName", CurrentModel.CameraName), new CustomInfo("Location", CurrentModel.CameraLocation)
            });

            CurrentModel.SetDiscoverable(DiscoverAbleCheckbox.IsChecked != null && (bool)DiscoverAbleCheckbox.IsChecked);
        }

        void UserListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            if (CurrentModel.GetCameraUsers == null)
                return;

            var index = UserListComboBox.SelectedIndex;

            if (index == -1)
                return;

            CameraUser = (CameraUser)CurrentModel.GetCameraUsers[index];
            OnPropertyChanged("CameraUser");
        }

        void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (CameraUser == null)
                return;

            if (String.IsNullOrEmpty(CameraUser.UserName) || String.IsNullOrEmpty(CameraUser.Password))
                return;

            if (!CameraUser.Password.Equals(Password2))
                return;

            var newUser = new CameraUser(Role) { UserName = CameraUser.UserName, Password = CameraUser.Password };
            CurrentModel.UserManager.AddCameraUser(newUser);
        }

        void ModifyUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (CameraUser == null)
                return;

            if (String.IsNullOrEmpty(CameraUser.UserName) || String.IsNullOrEmpty(CameraUser.Password))
                return;

            if (!CameraUser.Password.Equals(Password2))
                return;

            if (CameraUser == null)
                return;

            var index = UserListComboBox.SelectedIndex;

            var oldUser = (CameraUser)CurrentModel.GetCameraUsers[index];

            var newUser = new CameraUser(Role) { UserName = CameraUser.UserName, Password = CameraUser.Password };
            CurrentModel.UserManager.ModifyCameraUser(oldUser.UserName, newUser);
        }

        void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (CameraUser == null)
                return;

            if (String.IsNullOrEmpty(CameraUser.UserName) || String.IsNullOrEmpty(CameraUser.Password))
                return;


            if (!CameraUser.Password.Equals(Password2))
                return;

            if (CameraUser == null)
                return;

            CurrentModel.UserManager.RemoveCameraUser(CameraUser.UserName);
        }

        void SetTimeSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentModel == null)
                    return;

                if (UseUTC)
                    CurrentModel.SetTime((TimeZoneInfo)CurrentTimeZone);
                else
                {
                    var date = SelectedDate;

                    var split = SelectedTime.Split(':');

                    var time = new DateTime(date.Year, date.Month, date.Day, int.Parse(split[0]), int.Parse(split[1]),
                        int.Parse(split[2]));

                    CurrentModel.SetTimeManually(time);
                }
            }
            catch (Exception exception)
            {
                Log.Write("Invalid time format: " + exception.Message);
                System.Windows.MessageBox.Show("Invalid time format (HH:MM:SS)");
            }
        }

        void SaveNetworkButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentModel == null)
                return;

            if (CurrentModel.Network.DefaultConfig.UseDHCP == false)
                CurrentModel.Network.ApplyConfig();
        }

        void AddNumber_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(DialAdd))
                SoftPhone.AlarmList.Add(DialAdd);

            DialAdd = "";
        }

        void RemoveNumber_Click(object sender, RoutedEventArgs e)
        {
            var selected = SoftPhone.SelectedDial;
            SoftPhone.AlarmList.Remove(selected);
        }

        void CameraDevicesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cameraInfo = CameraDevicesCombo.SelectedItem as CameraDeviceInfo;
            if (cameraInfo == null)
                return;

            ConnectModel.SelectedDevice = cameraInfo;

            if (cameraInfo.IpCameraInfo != null)
                ConnectModel.Url = string.Format("{0}:{1}", cameraInfo.IpCameraInfo.Host, cameraInfo.IpCameraInfo.Port);
            else if (cameraInfo.WebCameraInfo != null)
                ConnectModel.Url = cameraInfo.WebCameraInfo.Name;

            if (cameraInfo.IpCameraInfo != null)
                CameraAddress = cameraInfo.IpCameraInfo.Uri.ToString();
            else
                CameraAddress = String.Empty;

            SavedCamera savedCamera = null;

            //if (IsSavedCamera(CameraAddress))
            //{
            savedCamera = GetSavedCamera(CameraAddress, CameraDevicesCombo.SelectedIndex);

            if (savedCamera != null)
                CurrentModel = savedCamera.Camera;
            else
                CurrentModel = GetCurrentModel(cameraInfo);

            if (savedCamera == null)
            {
                UserName = String.Empty;
                Password = String.Empty;
            }
            else
            {
                UserName = savedCamera.UserName;
                Password = savedCamera.Password;
            }

            if (CurrentModel == null ||
                CurrentModel.CameraState == null ||
                (String.IsNullOrEmpty(CurrentModel.CameraState) || !CurrentModel.CameraState.Trim().ToUpper().Equals("STREAMING")))
                StopViewer();

            OnPropertyChanged("IsStreamable");
        }

        Camera GetCurrentModel(CameraDeviceInfo cameraInfo)
        {
            if (cameraInfo.IpCameraInfo != null)
            {
                foreach (var camInfo in ConnectModel.CameraList)
                {
                    if (camInfo.CameraInfo != null)
                        if (camInfo.CameraInfo.Equals(cameraInfo.IpCameraInfo.Uri.ToString()) ||
                            String.Format("{0}/", camInfo.CameraInfo).Equals(cameraInfo.IpCameraInfo.Uri.ToString()) ||
                            String.Format("http://{0}/", camInfo.CameraInfo).Equals(cameraInfo.IpCameraInfo.Uri.ToString()))
                            return camInfo;
                }
            }
            else if (cameraInfo.WebCameraInfo != null)
            {
                foreach (var camInfo in ConnectModel.CameraList)
                {
                    if (camInfo.DeviceInfo != null)
                        if (camInfo.CameraInfo.Equals(cameraInfo.WebCameraInfo.Name))
                            return camInfo;
                }
            }

            return null;
        }

        bool IsSavedCamera(Camera camera)
        {
            if (camera == null || ConnectModel.SavedCameras.Count <= 0)
                return false;

            var saved = false;

            foreach (var savedCamera in ConnectModel.SavedCameras)
            {
                saved = ContainedSavedCamera(savedCamera, camera.CameraInfo);

                if (saved)
                    return saved;
            }

            return saved;
        }

        bool IsSavedCamera(string cameraInfo)
        {
            if (String.IsNullOrEmpty(cameraInfo) || ConnectModel.SavedCameras.Count <= 0)
                return false;

            var saved = false;

            foreach (var savedCamera in ConnectModel.SavedCameras)
            {
                saved = ContainedSavedCamera(savedCamera, cameraInfo);

                if (saved)
                    return saved;
            }

            return saved;
        }

        SavedCamera GetSavedCamera(Camera camera, int selected)
        {
            if (camera == null)
                return null;

            var index = GetListIndex(selected);

            if (index == 0)
                return null;

            foreach (var savedCamera in ConnectModel.SavedCameras)
            {
                if (!ContainedSavedCamera(savedCamera, camera.CameraInfo))
                    continue;

                index--;

                if (index == 0)
                    return savedCamera;
            }

            return null;
        }

        SavedCamera GetSavedCamera(string cameraInfo, int selected)
        {
            if (String.IsNullOrEmpty(cameraInfo))
                return null;

            var index = GetListIndex(selected);

            if (index == 0)
                return null;

            foreach (var savedCamera in ConnectModel.SavedCameras)
            {
                //if (!ContainsSavedCamera(savedCamera, cameraInfo))
                //    continue;

                if (savedCamera.Camera == null || savedCamera.Camera.CameraInfo == null)
                    continue;

                if (!savedCamera.Camera.CameraInfo.Equals(cameraInfo))
                    continue;

                index--;

                if (index == 0)
                    return savedCamera;
            }

            return null;
        }

        bool ContainedSavedCamera(SavedCamera savedCamera, string cameraInfo)
        {
            if (String.IsNullOrEmpty(savedCamera.UserName))
                savedCamera.UserName = String.Empty;

            if (String.IsNullOrEmpty(UserName))
                UserName = String.Empty;

            if (String.IsNullOrEmpty(savedCamera.Password))
                savedCamera.Password = String.Empty;

            if (String.IsNullOrEmpty(Password))
                Password = String.Empty;

            if (!savedCamera.UserName.Equals(UserName))
                return false;

            if (String.IsNullOrEmpty(savedCamera.Password) && !String.IsNullOrEmpty(Password))
                return false;

            if (!savedCamera.Password.Equals(Password))
                return false;

            if (!savedCamera.Camera.CameraInfo.Equals(cameraInfo))
                return false;

            return true;
        }

        int GetListIndex(int index)
        {
            var count = 0;

            for (var i = 0; i <= index; i++)
            {
                if (ConnectModel.DeviceList[i].IpCameraInfo == null || ConnectModel.DeviceList[i].IpCameraInfo.Host != null)
                    continue;

                if (ConnectModel.DeviceList[i].IpCameraInfo.Uri.ToString().Equals(CameraAddress))
                    count++;
            }

            return count;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}


//void Add_Camera_Click(object sender, RoutedEventArgs e)
//{
//if (String.IsNullOrEmpty(CameraAddress))
//    return;

//var ipCamera = new IpCameraEngine(CameraAddress, UserName, Password);
//AddCamera(ipCamera);

//var savedCam = new SavedCamera
//{
//    DeviceInfo = new CameraDeviceInfo
//    {
//        IpCameraInfo = new IPVideoDeviceInfo
//            {
//                Uri = new Uri(CameraAddress)
//            }
//    },

//    Camera = ipCamera,
//    UserName = UserName,
//    Password = Password
//};

//ConnectModel.SavedCameras.Add(savedCam);
////ModelList.Add(ipCamera);

//var deviceInfo = new CameraDeviceInfo { IpCameraInfo = new IPVideoDeviceInfo { Uri = new Uri(CameraAddress) } };
//ConnectModel.DeviceList.Add(deviceInfo);
//}


//void RemoveButton_Click(object sender, RoutedEventArgs e)
//{
//    if (ConnectModel.SelectedDevice.IpCameraInfo != null)
//    {
//        var cam = ConnectModel.GetSavedCamera(ConnectModel.SelectedDevice.IpCameraInfo.Uri.ToString());
//        ConnectModel.SavedCameras.Remove(cam);
//    }

//    ConnectModel.DeviceList.Remove(ConnectModel.SelectedDevice);

//    if (CurrentModel == null)
//        return;

//    StopViewer();
//    CurrentModel.Close();

//    if (CurrentModel == null)
//        return;

//    ModelList.Remove(CurrentModel);
//}

//void AddButton_Click(object sender, RoutedEventArgs e)
//{
//    var addWindow = new AddCameraWindow(ConnectModel);
//    var success = addWindow.ShowDialog();

//    if (success == null || success != true)
//        return;

// show Connect to camera window
//var connectWindow = new AddCameraWindow(this);
//var ok = connectWindow.ShowDialog();

//if (ok == null || ok != true)
//{
//    // cancel pressed
//    return;
//}

//var windowData = connectWindow.Model;
//var selectedDevice = windowData.SelectedDevice;

//// add USB camera
//if (selectedDevice != null)
//{
//    if (selectedDevice.WebCameraInfo != null)
//    {
//        var createdCamera = new WebCameraEngine(selectedDevice.WebCameraInfo);
//        AddCamera(createdCamera);
//        return;
//    }
//}
//// add IP camera
//var ipCamera = new IpCameraEngine(windowData.Url, windowData.User, windowData.Password);
//AddCamera(ipCamera);
//}
