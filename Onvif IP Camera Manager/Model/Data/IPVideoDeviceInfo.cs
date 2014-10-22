using System;
using Ozeki.Media.IPCamera.Discovery;

namespace Onvif_IP_Camera_Manager.Model.Data
{
    public class IPVideoDeviceInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public Uri Uri { get; set; }

        public IPVideoDeviceInfo()
        {
        }

        public IPVideoDeviceInfo(DiscoveredDeviceInfo info)
            : this()
        {
            Host = info.Host;
            Port = info.Port;
            Uri = info.Uri;
        }

        public IPVideoDeviceInfo(string host, int port, Uri uri)
            : this()
        {
            Host = host;
            Port = port;
            Uri = uri;
        }

    }
}
