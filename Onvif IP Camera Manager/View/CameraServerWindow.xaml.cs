using System.ComponentModel;
using System.Windows;
using Onvif_IP_Camera_Manager.Service;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for CameraServer.xaml
    /// </summary>
    public partial class CameraServer : Window, INotifyPropertyChanged
    {
        public MyServer Server { get; set; }

        public CameraServer(Window main, MyServer server)
        {
            Server = server;
            Server.ClientCountChange += Server_ClientCountChange;
            
            Owner = main;
            
            InitializeComponent();
        }

        void Server_ClientCountChange(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Server");
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StartButton(object sender, RoutedEventArgs e)
        {
            Server.Start();
            Server.SetListenAddress(Server.IpAddress, Server.Port);
            OnPropertyChanged("Server");
        }

        private void StopButton(object sender, RoutedEventArgs e)
        {
            Server.Stop();
            OnPropertyChanged("Server");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
