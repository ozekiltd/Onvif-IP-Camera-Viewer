using System;
using System.ComponentModel;
using System.Windows;
using Onvif_IP_Camera_Manager.Service;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for OnlineStream.xaml
    /// </summary>
    public partial class OnlineStream : Window, INotifyPropertyChanged
    {
        private MyMediaGateway mediaGateway;
        public MyMediaGateway MediaGateway
        {
            get { return mediaGateway; }
            set
            {
                mediaGateway = value;
                OnPropertyChanged("MediaGateway");
            }
        }

        public OnlineStream(MyMediaGateway mgw)
        {
            MediaGateway = mgw;

            InitializeComponent();

            MediaGateway.ClientCountChange += MediaGateway_ClientCountChange;
        }

        void MediaGateway_ClientCountChange(object sender, EventArgs e)
        {
            OnPropertyChanged("MediaGateway");
        }

        public event PropertyChangedEventHandler PropertyChanged;


        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CloseButton(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
