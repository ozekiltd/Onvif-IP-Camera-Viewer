using System;
using System.ComponentModel;
using System.Windows;
using Onvif_IP_Camera_Manager.Model;
using Onvif_IP_Camera_Manager.Model.Data;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for AddCameraWindow.xaml
    /// </summary>
    public partial class AddCameraWindow : Window, INotifyPropertyChanged
    {
        public AddCameraModel AddCameraModel { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<CameraAddedEventArgs> CameraAdded;

        public AddCameraWindow(ConnectModel connectModel)
        {
            InitializeComponent();

            InitializeResources(connectModel);
        }

        void InitializeResources(ConnectModel model)
        {
            AddCameraModel = new AddCameraModel(this)
            {
                ConnectModel = model
            };

            AddCameraModel.CameraAdded += AddCameraModel_CameraAdded;

            OnPropertyChanged("AddCameraModel");
        }

        void AddCameraModel_CameraAdded(object sender, CameraAddedEventArgs e)
        {
            OnCameraAdded(e);
        }

        void OnCameraAdded(CameraAddedEventArgs e)
        {
            var handler = CameraAdded;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
