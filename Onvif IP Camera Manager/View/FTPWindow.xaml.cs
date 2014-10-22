using System.ComponentModel;
using System.Windows;
using Onvif_IP_Camera_Manager.Model.Data;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for FTPWindow.xaml
    /// </summary>
    public partial class FTPWindow : Window, INotifyPropertyChanged
    {
        private string url;
        public string URL
        {
            get { return url; }
            set
            {
                url = value;
                OnPropertyChanged("URL");
            }
        }

        private string username;
        public string Username
        {
            get { return username; }
            set
            {
                username = value;
                OnPropertyChanged("Username");
            }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                OnPropertyChanged("Password");
            }
        }

    
        public Ftp Ftp { get; set; }

        public FTPWindow(Ftp ftp)
        {
            InitializeComponent();

            if(ftp==null) return;
            
            this.Ftp = ftp;
            URL = ftp.Host;
            Username = ftp.User;
            Password = ftp.Pass;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Ftp = new Ftp(URL, Username, Password);
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
