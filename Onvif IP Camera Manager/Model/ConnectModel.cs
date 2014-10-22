using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Onvif_IP_Camera_Manager.Model.Data;
using Ozeki.Media.IPCamera;
using Ozeki.Media.IPCamera.Discovery;
using Ozeki.Media.MediaHandlers.Video;

namespace Onvif_IP_Camera_Manager.Model
{
    public class ConnectModel : INotifyPropertyChanged
    {
        string _url;
        public string User { get; set; }
        public string Password { get; set; }

        public ObservableList<CameraDeviceInfo> DeviceList { get; private set; }
        public CameraDeviceInfo SelectedDevice { get; set; }
        public List<SavedCamera> SavedCameras { get; private set; }
        public List<Camera> CameraList { get; private set; }

        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                OnPropertyChanged("URL");
            }
        }

        public ConnectModel()
        {
            Url = String.Empty;
            User = String.Empty;
            Password = String.Empty;
            DeviceList = new ObservableList<CameraDeviceInfo>();
            SavedCameras = new List<SavedCamera>();
            CameraList = new List<Camera>();

            IPCameraFactory.DeviceDiscovered += IPCamera_DeviceDiscovered;
        }

        internal void Close()
        {
            IPCameraFactory.DeviceDiscovered -= IPCamera_DeviceDiscovered;
        }

        void DiscoverUsbDevices()
        {
            var webCameras = WebCamera.GetDevices();
            foreach (var camera in webCameras)
            {
                var deviceInfo = new CameraDeviceInfo { WebCameraInfo = camera };
                AddDeviceToList(deviceInfo);
            }
        }

        internal void DiscoverDevices()
        {
            DeviceList.Clear();
            CameraList.Clear();

            foreach (var userCamera in SavedCameras)
            {
                if (userCamera.DeviceInfo != null)
                {
                    try
                    {
                        DeviceList.Add(userCamera.DeviceInfo);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                if (userCamera.Camera != null)
                {
                    try
                    {
                        CameraList.Add(userCamera.Camera);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            DiscoverUsbDevices();
            IPCameraFactory.DiscoverDevices();
        }

        void IPCamera_DeviceDiscovered(object sender, DiscoveryEventArgs e)
        {
            var deviceInfo = new CameraDeviceInfo { IpCameraInfo = new IPVideoDeviceInfo(e.Device) };
            AddDeviceToList(deviceInfo);
        }

        void AddDeviceToList(CameraDeviceInfo info)
        {
            if (SelectedDevice == null)
            {
                SelectedDevice = info;
                OnPropertyChanged("SelectedDevice");
            }

            if (info.IpCameraInfo != null)
            {
                foreach (var cameraDeviceInfo in SavedCameras)
                {
                    if (String.IsNullOrEmpty(cameraDeviceInfo.Camera.DeviceInfo))
                        continue;

                    if (cameraDeviceInfo.DeviceInfo == null || cameraDeviceInfo.DeviceInfo.IpCameraInfo == null || info.IpCameraInfo.Uri == null)
                        continue;

                    if (cameraDeviceInfo.DeviceInfo.IpCameraInfo.Uri.ToString().Equals(info.IpCameraInfo.Uri.ToString()))
                        return;
                }
            }
            else
            {
                foreach (var cameraDeviceInfo in SavedCameras)
                {
                    if (String.IsNullOrEmpty(cameraDeviceInfo.Camera.DeviceInfo))
                        continue;

                    if (cameraDeviceInfo.DeviceInfo == null || cameraDeviceInfo.DeviceInfo.WebCameraInfo == null)
                        continue;

                    if (cameraDeviceInfo.DeviceInfo.WebCameraInfo.Name.Equals(info.WebCameraInfo.Name))
                        return;
                }
            }

            DeviceList.Add(info);

            try
            {
                OnDeviceAdded(info);
            }
            catch (Exception ex)
            {
            }

            OnPropertyChanged("DeviceList");
        }

        public SavedCamera GetSavedCamera(string address)
        {
            if (String.IsNullOrEmpty(address))
                return null;

            foreach (var savedCamera in SavedCameras)
            {
                if (savedCamera.Camera != null
                    && savedCamera.Camera.CameraInfo.Equals(address))
                    return savedCamera;

                if (savedCamera.DeviceInfo != null
                    && savedCamera.DeviceInfo.IpCameraInfo != null
                    && savedCamera.DeviceInfo.IpCameraInfo.Uri.ToString().Equals(address))
                    return savedCamera;
            }

            return null;
        }

        public bool RemoveCamera(Camera camera)
        {
            for (var i = CameraList.Count - 1; i >= 0; i--)
            {
                if (CameraList[i].CameraInfo != camera.CameraInfo)
                    continue;

                return CameraList.Remove(CameraList.FirstOrDefault(c=> c.CameraInfo.Equals(camera.CameraInfo)));
            }

            return false;
        }

        void OnDeviceAdded(CameraDeviceInfo info)
        {
            var handler = DeviceAdded;
            if (handler != null)
                handler(this, new DeviceAddedEventArgs(info));
        }

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
