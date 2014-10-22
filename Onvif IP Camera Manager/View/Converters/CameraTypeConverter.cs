using System;
using System.Windows.Data;
using Onvif_IP_Camera_Manager.Model.Data;

namespace Onvif_IP_Camera_Manager.View.Converters
{
    class CameraTypeConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var info = value as CameraDeviceInfo;
            if (info == null)
                return 1;

            if (info.WebCameraInfo != null)
                return 0;

            return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
