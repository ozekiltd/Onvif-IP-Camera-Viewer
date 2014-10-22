using System;
using Ozeki.Media.Video;

namespace Onvif_IP_Camera_Manager.Model.Data
{
    public class CameraDeviceInfo
    {
        public VideoDeviceInfo WebCameraInfo { get; set; }
        //public DiscoveredDeviceInfo IpCameraInfo { get; set; }
        public IPVideoDeviceInfo IpCameraInfo { get; set; }

        public override string ToString()
        {
            if (WebCameraInfo != null)
                return string.Format("[USB] {0}", WebCameraInfo.Name);

            if (IpCameraInfo != null)
            {
                if (!String.IsNullOrEmpty(IpCameraInfo.Host))
                    return string.Format("[ONVIF] {0}:{1}", IpCameraInfo.Host, IpCameraInfo.Port);

                if (IpCameraInfo.Uri != null && IpCameraInfo.Uri.ToString().Trim().ToUpper().StartsWith("RTSP://"))
                    return String.Format("[RTSP] {0}", IpCameraInfo.Uri);

                return String.Format("[CUSTOM] {0}", IpCameraInfo.Uri);
            }

            return "Unknown";
        }
    }
}
