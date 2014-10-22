using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Ozeki.MediaGateway;

namespace Onvif_IP_Camera_Manager.View.Converters
{
    class ClientConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var client = value as IEnumerable<IClient>;
            if (client == null) return String.Empty;
            return client.Select(item => "Address: " + item.RemoteAddress + " Type: " + item.ClientType).ToList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
