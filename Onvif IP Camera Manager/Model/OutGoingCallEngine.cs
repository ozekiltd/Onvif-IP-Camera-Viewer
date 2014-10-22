using System;
using Ozeki.Media.MediaHandlers;
using Ozeki.VoIP;
using Ozeki.VoIP.SDK;

namespace Onvif_IP_Camera_Manager.Model
{
    public class OutGoingCallEngine
    {
        public IPhoneCall Call { get; set; }
        private PhoneCallAudioSender _audioSender;
        private PhoneCallVideoSender _videoSender;
        private MediaConnector _connector;

        public event EventHandler<CallStateChangedArgs> CallStateChanged;

        public OutGoingCallEngine(string dialedNumber, ISoftPhone softPhone, IPhoneLine phoneLine)
        {
            _connector = new MediaConnector();
            _videoSender = new PhoneCallVideoSender();
            _audioSender = new PhoneCallAudioSender();

            var dial = new DialParameters(dialedNumber) { CallType = CallType.AudioVideo };
            Call = softPhone.CreateCallObject(phoneLine, dial);
            Call.CallStateChanged += _call_CallStateChanged;        

            _videoSender.AttachToCall(Call);
            _audioSender.AttachToCall(Call);
        }


        private void _call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            var handler = CallStateChanged;
            if (handler != null)
                handler(sender, e);

            if (e.State.IsCallEnded())
            {
                if (Call != null)
                {
                    Call.CallStateChanged -= _call_CallStateChanged;
                    _videoSender.Detach();
                    _audioSender.Detach();
                    _connector.Dispose();
                    Call = null;
                }
            }
        }

        public void Connect(IAudioSender audioSender, IVideoSender videoSender)
        {
            _connector.Connect(audioSender, _audioSender);
            _connector.Connect(videoSender, _videoSender);
        }

        public void Close()
        {
            if(Call !=null)
                Call.HangUp();
        }

        public void StartCall()
        {
            Call.Start(); 
        }
    }
}
