using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Windows;
using Onvif_IP_Camera_Manager.Model.Data;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for EmailWindow.xaml
    /// </summary>
    public partial class EmailWindow : Window, INotifyPropertyChanged
    {
        private string _fromAddress;
        public string FromAddress
        {
            get { return _fromAddress; }
            private set
            {
                _fromAddress = value;
                OnPropertyChanged("FromAddress");
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            private set
            {
                _password = value;
                OnPropertyChanged("Password");
            }
        }

        private string _to;
        public string To
        {
            get { return _to; }
            private set
            {
                _to = value;
                OnPropertyChanged("To");
            }
        }

        private string _subject;
        public string Subject
        {
            get { return _subject; }
            private set
            {
                _subject = value;
                OnPropertyChanged("Subject");
            }
        }

        private string _body;
        public string Body
        {
            get { return _body; }
            private set
            {
                _body = value;
                OnPropertyChanged("Body");
            }
        }

        private string _smtp;
        public string Smtp
        {
            get { return _smtp; }
            private set
            {
                _smtp = value;
                OnPropertyChanged("Smtp");
            }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            private set
            {
                _port = value;
                OnPropertyChanged("Port");
            }
        }

        public Email Email { get; private set; }

        public EmailWindow(Email email)
        {
            InitializeComponent();
            this.Email = email;
            if (Email == null) return;

            FromAddress = email.FromAddress.Address;
            Password = email.FromPassword;
            To = email.ToAddress.Address;
            Subject = email.Subject;
            Body = email.Body;
            Smtp = email.Smtpclient.Host;
            Port = email.PortNum;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            if (IsValidEmail(FromAddress) && IsValidEmail(To))
            {
                Email = new Email(FromAddress, Password, To,
                    Subject, Body, Port);
                Email.CreateSmtpClient(Smtp);
                Close();
            }
            else
                MessageBox.Show("Invalid e-mail format");  
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static bool IsValidEmail(string emailaddress)
        {
            try
            {
                new MailAddress(emailaddress);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
