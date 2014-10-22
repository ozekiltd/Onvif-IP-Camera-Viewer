using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using Onvif_IP_Camera_Manager.Model.Data;
using Ozeki.Media.MediaHandlers;
using Ozeki.Media.MediaHandlers.Video;
using UserControl = System.Windows.Controls.UserControl;
using Onvif_IP_Camera_Manager.Model;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for CameraViewerControl.xaml
    /// </summary>
    public partial class CameraViewerControl : UserControl, INotifyPropertyChanged
    {
        private MediaConnector connector;
        private BitmapSourceProvider bitmapSourceProvider;
        private Camera model;

        public ObservableCollection<Camera> ModelList
        {
            get { return (ObservableCollection<Camera>)GetValue(ModelListProperty); }
            set { SetValue(ModelListProperty, value); }
        }

         //Using a DependencyProperty as the backing store for ModelList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelListProperty =
            DependencyProperty.Register("ModelList", typeof(ObservableCollection<Camera>), typeof(CameraViewerControl), new PropertyMetadata(new ObservableCollection<Camera>()));

        public Camera Model
        {
            get { return model; }
            set
            {
                Disconnect();
                model = value;
                OnPropertyChanged("Model");
                Start();
            }
        }

        public string FilePath { get; set; }

        public CameraViewerControl()
        {
            connector = new MediaConnector();
            bitmapSourceProvider = new BitmapSourceProvider();

            InitializeComponent();
            Viewer.SetImageProvider(bitmapSourceProvider);
        }

        private void Start()
        {
            if (Model == null)
                return;

            connector.Connect(Model.VideoSender, bitmapSourceProvider);
            Viewer.Start();
        }

        private void Disconnect()
        {
            if (Model == null)
                return;

            connector.Disconnect(Model.VideoSender, bitmapSourceProvider);
            Viewer.Stop();
        }

        private void FilePathButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new FolderBrowserDialog { SelectedPath = "C:\\" };

            var result = folderDialog.ShowDialog();
            if (result.ToString() != "OK") return;
            FilePath = folderDialog.SelectedPath;
            OnPropertyChanged("FilePath");
        }

        private void SnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model == null)
                return;

            Model.TakeSnapshot(FilePath);
        }

        private void StartVideoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Model == null)
                return;

            Model.StartCaptionVideo(FilePath);
        }

        private void StopVideoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Model == null)
                return;

            Model.StopCaptionVideo();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
