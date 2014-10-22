using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow(Window owner)
        {
            Owner = owner;
            InitializeComponent();
            TextBlockVersion.Text = String.Format("Version {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("http://www.camera-sdk.com/");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        private void email_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("mailto:info@camera-sdk.com")); e.Handled = true;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }

        }
    }
}
