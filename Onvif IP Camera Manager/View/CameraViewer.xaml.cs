using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using Onvif_IP_Camera_Manager.Annotations;
using Ozeki.Media.MediaHandlers;
using Ozeki.Media.MediaHandlers.Video;
using UserControl = System.Windows.Controls.UserControl;
using Onvif_IP_Camera_Manager.Model;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for CameraViewer.xaml
    /// </summary>
    public partial class CameraViewer : UserControl, INotifyPropertyChanged
    {
       
        private MediaConnector Connector;
        private BitmapSourceProvider bitmapSourceProvider;

        public string Title
        {
            get { return (string)GetValue(TestProperty); }
            set { this.SetValue(TestProperty, value); }
        }

        public static readonly DependencyProperty TestProperty =
            DependencyProperty.Register("Title",
            typeof(string),
            typeof(CameraViewer));
        public Camera Model { get; private set; }

        public string FilePath { get; set; }

     
        public CameraViewer()
        {
            InitializeComponent();
        }

        public void Start(Camera model)
        {
            if(Model != null)
                Connector.Disconnect(Model.VideoSender, bitmapSourceProvider);

            bitmapSourceProvider = new BitmapSourceProvider();

            Viewer.SetImageProvider(bitmapSourceProvider);

            Connector = new MediaConnector();

            Model = model;
            Connector.Connect(model.VideoSender, bitmapSourceProvider);

            Viewer.Start();
        }

        public void Disconnect()
        {
            if (Model == null) return;
            Viewer.Stop();
            Viewer.Dispose();

            Connector.Disconnect(Model.VideoSender, bitmapSourceProvider);
            Connector.Dispose();

            bitmapSourceProvider.Dispose();
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
            Model.CreateSnapshot(FilePath);
        }

        private void StartVideoButton_OnClick(object sender, RoutedEventArgs e)
        {
            Model.StartCaptionVideo(FilePath);
        }

        private void StopVideoButton_OnClick(object sender, RoutedEventArgs e)
        {
            Model.StopCaptionVideo();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

       
    }
}
