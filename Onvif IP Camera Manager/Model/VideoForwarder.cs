using Ozeki.Media.MediaHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Onvif_IP_Camera_Manager.Model
{
    class VideoForwarder : VideoHandler
    {
        public override void OnDataReceived(object sender, Ozeki.Media.VideoData data)
        {
            SendData(data);
        }
    }
}
