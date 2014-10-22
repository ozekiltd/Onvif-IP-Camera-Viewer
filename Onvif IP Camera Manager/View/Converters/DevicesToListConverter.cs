using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Ozeki.Media.IPCamera.Discovery;
using Ozeki.Media.Video;

namespace Onvif_IP_Camera_Manager.View.Converters
{
    class DevicesToListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var deviceList = value as ObservableCollection<object>;
            if (deviceList == null)
                return value;

            var temp = new List<string>();
            try
            {
                var deviceL = deviceList.ToList();

                foreach (var item in deviceL)
                {
                    if (item is DiscoveredDeviceInfo) // IP cam
                    {
                        var info = item as DiscoveredDeviceInfo;
                        temp.Add("Host - " + info.Host);
                    }
                    if (item is VideoDeviceInfo) // webcam
                    {
                        var info = item as VideoDeviceInfo;
                        temp.Add("ImageSettingName - " + info.Name);
                    }
                }
            }
            catch (Exception)
            {

            }
            return temp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
