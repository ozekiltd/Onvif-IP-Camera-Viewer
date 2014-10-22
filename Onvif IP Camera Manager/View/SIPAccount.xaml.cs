using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Onvif_IP_Camera_Manager.Model;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for SIPAccount.xaml
    /// </summary>
    public partial class SIPAccount : Window
    {
        public AccountModel Model { get; set; }

        public SIPAccount()
        {
            InitializeComponent();
        }

        public SIPAccount(AccountModel model)
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
