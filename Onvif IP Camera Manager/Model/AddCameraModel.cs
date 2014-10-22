using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Onvif_IP_Camera_Manager.Model.Data;
using Onvif_IP_Camera_Manager.Model.Helpers;

namespace Onvif_IP_Camera_Manager.Model
{
    public class AddCameraModel : INotifyPropertyChanged
    {
        string _cameraUrl;
        string _userName;
        string _password;

        Window _addCameraWindow;

        public string CameraUrl
        {
            get { return _cameraUrl; }
            set
            {
                _cameraUrl = value;
                OnPropertyChanged("CameraUrl");
            }
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged("UserName");
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                OnPropertyChanged("Password");
            }
        }

        public string Example1 { get; private set; }
        public string Example2 { get; private set; }
        public string Example3 { get; private set; }

        public ConnectModel ConnectModel { get; set; }

        public ICommand AddCameraCommand { get; private set; }
        public ICommand ClearCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<CameraAddedEventArgs> CameraAdded;

        public AddCameraModel(Window addCameraWindow)
        {
            _addCameraWindow = addCameraWindow;

            SetContents();
            SetCommands();
        }

        void SetContents()
        {
            //ClearFields();

            InvokeGui(() =>
            {
                if (ConnectModel.SelectedDevice.IpCameraInfo != null)
                    CameraUrl = ConnectModel.SelectedDevice.IpCameraInfo.Uri.ToString();
                else
                    CameraUrl = String.Empty;

                Example1 = "192.168.115.98:8080";
                OnPropertyChanged("Example1");

                Example2 = "http://192.168.115.98:8080";
                OnPropertyChanged("Example2");

                Example3 = "rtsp://192.168.115.98:554/test.sdp";
                OnPropertyChanged("Example3");
            });
        }

        void ClearFields()
        {
            InvokeGui(() =>
            {
                CameraUrl = String.Empty;
                UserName = String.Empty;
                Password = String.Empty;
            });
        }

        void SetCommands()
        {
            AddCameraCommand = new RelayCommand(() =>
            {
                AddCamera();
                _addCameraWindow.Close();
            });

            ClearCommand = new RelayCommand(ClearFields);
        }

        void AddCamera()
        {
            if (String.IsNullOrEmpty(CameraUrl))
                return;

            var ipCamera = new IpCameraEngine(CameraUrl, UserName, Password);

            if (!(CameraUrl.Trim().ToUpper().StartsWith("HTTP://") || CameraUrl.Trim().ToUpper().StartsWith("RTSP://")))
                CameraUrl = String.Format("http://{0}", CameraUrl);

            var ipCameraInfo = new IPVideoDeviceInfo
            {
                Uri = new Uri(CameraUrl)
            };

            var savedCamera = new SavedCamera
            {
                DeviceInfo = new CameraDeviceInfo
                {
                    IpCameraInfo = ipCameraInfo
                },
                Camera = ipCamera,
                UserName = UserName,
                Password = Password
            };

            var deviceInfo = new CameraDeviceInfo
            {
                IpCameraInfo = ipCameraInfo
            };

            ConnectModel.CameraList.Add(ipCamera);
            ConnectModel.SavedCameras.Add(savedCamera);
            ConnectModel.DeviceList.Add(deviceInfo);

            OnCameraAdded(ipCamera);
        }

        void InvokeGui(Action action)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(action);
        }

        void OnCameraAdded(Camera camera)
        {
            var handler = CameraAdded;
            if (handler != null)
                handler(this, new CameraAddedEventArgs(camera));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
