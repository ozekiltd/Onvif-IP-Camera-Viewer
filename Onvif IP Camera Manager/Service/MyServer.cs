using System;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Onvif_IP_Camera_Manager.LOG;
using Ozeki.Media.IPCamera;
using Ozeki.Media.IPCamera.Rtsp;
using Ozeki.Media.IPCamera.Server;
using Ozeki.Media.MediaHandlers;
using Ozeki.VoIP.PBX.Authentication;

namespace Onvif_IP_Camera_Manager.Service
{
    public class MyServer : IPCameraServer, INotifyPropertyChanged
    {
        MediaConnector _connector;

        IIPCameraClient _client;

        string _cameraUrl;
        string _defaultIP;

        public Model.Camera Model { get; set; }

        public string CameraUrl
        {
            get { return _cameraUrl; }
            private set
            {
                _cameraUrl = value;
                OnPropertyChanged("CameraUrl");
            }
        }

        public string IpAddress { get; set; }
        public int Port { get; set; }

        public event EventHandler<EventArgs> ClientCountChange;

        public string State { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MyServer()
        {
            _defaultIP = "127.0.0.1";

            _connector = new MediaConnector();
            Port = 554;

            _defaultIP = GetDefaultIP();

            CameraUrl = String.Format("rtsp://{0}:{1}", _defaultIP, Port);
        }

        protected override void OnClientConnected(IIPCameraClient client)
        {
            _client = client;

            _connector.Connect(Model.AudioSender, _client.AudioChannel);
            _connector.Connect(Model.VideoSender, _client.VideoChannel);

            var handler = ClientCountChange;
            if (handler != null)
                handler(null, new EventArgs());

            base.OnClientConnected(_client);

            Log.Write("Client Connected: " + _client.TransportInfo.RemoteEndPoint + " Local: " + _client.TransportInfo.LocalEndPoint);
        }

        protected override void OnClientDisconnected(IIPCameraClient client)
        {
            _client = client;

            _connector.Disconnect(Model.AudioSender, _client.AudioChannel);
            _connector.Disconnect(Model.VideoSender, _client.VideoChannel);
            _connector.Dispose();

            var handler = ClientCountChange;
            if (handler != null)
                handler(null, new EventArgs());

            base.OnClientDisconnected(_client);

            Log.Write("Client Disconnected: " + _client.TransportInfo.RemoteEndPoint + " Local: " + _client.TransportInfo.LocalEndPoint);
        }

        protected override bool OnAuthenticationRequired(IIPCameraClient client, RtspMethod method)
        {
            //if (method != RtspMethod.DESCRIBE)
            //    return false;

            Log.Write("Authentication required " + method);

            return base.OnAuthenticationRequired(client, method);
        }

        protected override bool OnAuthenticationRequested(BaseAuthenticationInfo info, IIPCameraClient client)
        {
            Log.Write("Authentication requested.");

            if (info.AuthName != "admin")
                return false;

            var success = CheckPassword(info, "admin");

            return success;
        }

        protected override void OnStarted()
        {
            Log.Write("IPCameraServer started");
            State = "Started";
            base.OnStarted();
        }

        protected override void OnStopped()
        {
            Log.Write("IPCameraServer stopped");
            State = "Stopped";
            base.OnStopped();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        string GetDefaultIP()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_IP4RouteTable");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    var interfaceIndex = queryObj["InterfaceIndex"].ToString();
                    var interfaceIPAddress = GetInterfaceAddress(interfaceIndex);

                    var fullpath = queryObj.Path.ToString();
                    //minta: "\\\\TOSHIBASZURKE\\root\\CIMV2:Win32_IP4RouteTable.Destination=\"0.0.0.0\",InterfaceIndex=3,Mask=\"0.0.0.0\",NextHop=\"192.168.112.1\""

                    if (fullpath.Contains("Win32_IP4RouteTable.Destination=\"0.0.0.0\""))
                        _defaultIP = interfaceIPAddress;
                }
            }
            catch (Exception ex)
            {
                _defaultIP = "127.0.0.1";
            }

            return _defaultIP;
        }

        string GetInterfaceAddress(string interfaceIndex)
        {
            UnicastIPAddressInformation result = null;
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    var ipProperties = networkInterface.GetIPProperties();
                    var prop = ipProperties.GetIPv4Properties();

                    if (prop.Index.ToString() == interfaceIndex)
                    {
                        result = ipProperties.UnicastAddresses.FirstOrDefault(item => item.Address.AddressFamily == AddressFamily.InterNetwork);
                        break;
                    }
                }
                catch (Exception)
                {

                }
            }

            if (result == null)
                return "127.0.0.1";

            return result.Address.ToString();
        }

    }
}
