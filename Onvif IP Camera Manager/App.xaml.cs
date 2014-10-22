using System;
using System.Windows;
using System.Windows.Threading;

namespace Onvif_IP_Camera_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            base.OnStartup(e);
        }

        void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(string.Format("Error occurred: {0}, {1}, {2}", e.Exception.GetType(), e.Exception.Message, e.Exception.StackTrace), "Error");
            e.Handled = true;
        }
    }
}
