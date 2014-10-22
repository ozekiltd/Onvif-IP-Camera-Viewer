using System.Windows;
using Onvif_IP_Camera_Manager.Model;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for SIPAccountWindow.xaml
    /// </summary>
    public partial class SipAccountWindow : Window
    {
        public AccountModel Model { get; set; }

        public SipAccountWindow()
        {
            InitializeComponent();
        }

        public SipAccountWindow(AccountModel model)
        {
            Model = model;
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate("User name", Model.UserName))
                return;

            if (!Validate("Register name", Model.RegisterName))
                return;

            if (!Validate("Domain", Model.Domain))
                return;
            
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool Validate(string propertyName, string value)
        {
            if (value == null || string.IsNullOrEmpty(value.Trim()))
            {
                MessageBox.Show(string.Format("{0} cannot be empty!", propertyName));
                return false;
            }
            return true;
        }

       
    }
}
