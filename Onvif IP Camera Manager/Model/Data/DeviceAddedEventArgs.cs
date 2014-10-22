using System;

namespace Onvif_IP_Camera_Manager.Model.Data
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public CameraDeviceInfo Info { get; private set; }

        internal DeviceAddedEventArgs(CameraDeviceInfo info)
        {
            Info = info;
        }
    }
}
