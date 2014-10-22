using System;
using System.Windows.Data;

namespace Onvif_IP_Camera_Manager.View.Converters
{
    class DeviceLabelConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int index = (int)value;
            if (index == 0)
                return "Device name:";

            return "URL:";
            //var cameraInfo = value as CameraDeviceInfo;
            //if (cameraInfo != null)
            //{
            //    if (cameraInfo.WebCameraInfo != null)
            //        return "Device name:";
            //}

            //return "URL:";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
