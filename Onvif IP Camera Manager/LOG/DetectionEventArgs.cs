using System;

namespace Onvif_IP_Camera_Manager.LOG
{
    public class DetectionEventArgs : EventArgs
    {
        public string Path;

        public DetectionEventArgs(string path)
        {
            Path = path;
        }
    }
}
