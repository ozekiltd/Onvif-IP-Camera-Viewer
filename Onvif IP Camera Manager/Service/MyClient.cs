using System;
using System.Threading;
using System.Windows.Forms;
using Onvif_IP_Camera_Manager.LOG;
using Onvif_IP_Camera_Manager.Model;
using Onvif_IP_Camera_Manager.View;
using Ozeki.Media.IPCamera;
using Ozeki.Media.MediaHandlers;
using Ozeki.MediaGateway;
using Ozeki.MediaGateway.Service;

namespace Onvif_IP_Camera_Manager.Service
{
    class MyClient
    {
        private IClient client;
        private IStreamService streamService;
        private IMediaStream _mediaStream;
        private MediaGatewayVideoReceiver _videoReceiver;
        private MediaConnector Connector;
        private Camera _camera;
        private VideoForwarder _forwarder;

        public MyClient(IClient client, IStreamService streamService, Camera currentCamera)
        {
            this.client = client;
            this.streamService = streamService;
            Connector = new MediaConnector();
            _forwarder = new VideoForwarder();
            _camera = currentCamera;
        }

        internal void ConnectCamera()
        {
            NotifyCameraStateChanged(IPCameraState.Connected);
            // start stream
            var playStreamName = Guid.NewGuid().ToString();
            _mediaStream = streamService.CreateStream(playStreamName);
            _videoReceiver = new MediaGatewayVideoReceiver(_mediaStream);

            Connector.Connect(_camera.VideoSender, _forwarder);
            Connector.Connect(_forwarder, _videoReceiver);

            // notify to client the stream name
            OnPlayRemoteStream(playStreamName);
        }

        void OnPlayRemoteStream(string streamName)
        {
            try
            {
                client.InvokeMethod("OnPlayRemoteStream", streamName);
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
            }
        }

        private void NotifyCameraStateChanged(IPCameraState state)
        {
            try
            {
                client.InvokeMethod("OnCameraStateChanged", state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
