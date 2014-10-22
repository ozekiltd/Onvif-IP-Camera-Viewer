using System;
using Onvif_IP_Camera_Manager.Model.Data;

namespace Onvif_IP_Camera_Manager.LOG
{
    class Log
    {
        public static ObservableList<string> LogList { get; set; }
        public static ObservableList<string> MotionLogList { get; set; }

        static object sync = new object();

        static Log()
        {
            LogList = new ObservableList<string>();
            MotionLogList = new ObservableList<string>();
        }

        private static int Counter;
        public static void Write(string logMessage)
        {
            string logMsg;
            lock (sync)
            {
                Counter++;
                logMsg = "(" + Counter + ") " + DateTime.Now + " | " + logMessage;
                LogList.Add(logMsg);
            }
        }

        private static int MotionCounter;
        public static void Motion(string logMessage)
        {
            string logMsg;

            lock (sync)
            {
                Write(logMessage);

                MotionCounter++;
                logMsg = "(" + MotionCounter + ") " + DateTime.Now + " | " + logMessage;
                MotionLogList.Add(logMsg);
            }
        }
    }
}
