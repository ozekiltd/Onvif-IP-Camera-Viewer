using System;
using System.Collections.Generic;
using System.Threading;
using Onvif_IP_Camera_Manager.LOG;
using Onvif_IP_Camera_Manager.Model;
using Onvif_IP_Camera_Manager.View;
using Ozeki.MediaGateway;
using Ozeki.MediaGateway.Config;
using Ozeki.MediaGateway.Service;
using Ozeki.Network;

namespace Onvif_IP_Camera_Manager.Service
{
    public class MyMediaGateway : MediaGateway
    {
        private Dictionary<IClient, MyClient> clients;
        private IStreamService _streamService;
        private Camera _currentCam;

        public event EventHandler<EventArgs> ClientCountChange;

        public MyMediaGateway(MediaGatewayConfig config, Camera currentCamera)
            : base(config)
        {
            _currentCam = currentCamera;
            clients = new Dictionary<IClient, MyClient>();
        }

        #region MediaGateway methods
        public override void OnStart()
        {
            base.OnStart();

            _streamService = GetService<IStreamService>();

            Log.Write("MediaGateway started on address: " +NetworkAddressHelper.GetLocalIP());
        }

        public override void OnClientConnect(IClient client, object[] parameters)
        {
            Log.Write(client.RemoteAddress + " client connected to the server with " + client.ClientType);
            if (clients.ContainsKey(client)) return;
            clients.Add(client, new MyClient(client, _streamService, _currentCam));
            ClientChange();
        }

        private void ClientChange()
        {
            var handler = ClientCountChange;
            if (handler != null)
                ClientCountChange(this, new EventArgs());
        }

        public void Connect(IClient client)
        {
            var myClient = GetClient(client);

            if (myClient == null)
                return;

           myClient.ConnectCamera();
        }

        #endregion

        MyClient GetClient(IClient client)
        {
            return !clients.ContainsKey(client) ? null : clients[client];
        }
    }
}
