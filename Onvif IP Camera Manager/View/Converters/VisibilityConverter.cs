using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Onvif_IP_Camera_Manager.View.Converters
{
    class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visible = value as String;

            if (String.IsNullOrEmpty(visible))
                return Visibility.Hidden;

            if (visible.Trim().ToUpper().Contains("STREAMING"))
                return Visibility.Visible;

            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
