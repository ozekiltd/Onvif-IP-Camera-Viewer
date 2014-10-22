using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Ozeki.Media.IPCamera;

namespace Onvif_IP_Camera_Manager.View.Converters
{
    class CameraClientConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var client = value as List<IIPCameraClient>;
            if (client == null) return String.Empty;
            var temp = new List<string>();

            foreach (var item in client)
            {
                temp.Add("End point: " + item.TransportInfo.RemoteEndPoint);
            }
            return temp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
