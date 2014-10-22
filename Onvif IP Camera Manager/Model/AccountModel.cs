
namespace Onvif_IP_Camera_Manager.Model
{
    public class AccountModel
    {
        public string DisplayName { get; set; }
        public string UserName { get; set; }
        public string RegisterName { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
        public string OutboundProxy { get; set; }
        public bool RegistrationRequired { get; set; }
    }
}
