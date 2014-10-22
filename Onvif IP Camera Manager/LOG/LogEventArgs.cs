using System;

namespace Onvif_IP_Camera_Manager.LOG
{
    class LogEventArgs : EventArgs
    {
        public string LogMessage;

        public LogEventArgs(string log)
        {
            LogMessage = log;
        }
    }
}
