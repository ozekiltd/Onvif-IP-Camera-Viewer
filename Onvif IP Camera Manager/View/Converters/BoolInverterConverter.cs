using System;
using System.Windows.Data;

namespace Onvif_IP_Camera_Manager.View.Converters
{
    class BoolInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
       System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            return value;
        }
    }
}
