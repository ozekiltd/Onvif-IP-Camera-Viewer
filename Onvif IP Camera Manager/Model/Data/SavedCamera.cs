namespace Onvif_IP_Camera_Manager.Model.Data
{
    public class SavedCamera
    {
        public CameraDeviceInfo DeviceInfo { get; set; }
        public Camera Camera { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
