using System;

namespace Onvif_IP_Camera_Manager.Model.Data
{
    public class CameraAddedEventArgs : EventArgs
    {
        public Camera Camera { get; private set; }

        internal CameraAddedEventArgs(Camera camera)
        {
            Camera = camera;
        }

    }
}
