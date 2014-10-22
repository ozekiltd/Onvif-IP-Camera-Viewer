using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Onvif_IP_Camera_Manager.LOG;
using Ozeki.Media.MediaHandlers;
using Ozeki.VoIP;
using Ozeki.VoIP.SDK;

namespace Onvif_IP_Camera_Manager.Model
{
	public class SoftphoneEngine : INotifyPropertyChanged
	{
	    public ISoftPhone Softphone { get; private set; }
	    public IPhoneLine PhoneLine { get; set; }

        // single call objects
		public IPhoneCall Call { get; set; }
        private CallState? currentCallState;
	    private MediaConnector _connector;
        private PhoneCallVideoSender _videoSender;
        private PhoneCallAudioSender _audioSender;
        private IVideoSender _cameraVideo;
        private IAudioSender _cameraAudio;

        public CallState? CurrentCallState
        {
            get { return currentCallState; }
            set { currentCallState = value; OnPropertyChanged("CurrentCallState"); }
        }
        
        // alarm call objects
        public string SelectedDial { get; set; }
        public ObservableCollection<string> AlarmList { get; set; }
	    private List<OutGoingCallEngine> alarmCalls;

		public SoftphoneEngine()
		{
            AlarmList = new ObservableCollection<string>();
            alarmCalls = new List<OutGoingCallEngine>();

            _connector = new MediaConnector();
            _videoSender = new PhoneCallVideoSender();
            _audioSender = new PhoneCallAudioSender();

			Softphone = SoftPhoneFactory.CreateSoftPhone(5000, 10000);
			Softphone.IncomingCall += softphone_IncomingCall;
		}

	    public void SetModel(IAudioSender audioSource, IVideoSender videoSource)
	    {
	        DisconnectFromCall();

	        _cameraAudio = audioSource;
            _cameraVideo = videoSource;

            ConnectToCall(Call);
	    }

		public void RegisterSipAccount(AccountModel model)
		{
		    try
		    {
                if (model == null)
                    return;

		        if (PhoneLine != null)
		        {
                    PhoneLine.RegistrationStateChanged -= phoneLine_RegistrationStateChanged;
		        }

		        var account = new SIPAccount(model.RegistrationRequired, model.DisplayName, model.UserName, model.RegisterName, model.Password, model.Domain, model.OutboundProxy);
                PhoneLine = Softphone.CreatePhoneLine(account);
                PhoneLine.RegistrationStateChanged += phoneLine_RegistrationStateChanged;
                Softphone.RegisterPhoneLine(PhoneLine);
                Log.Write(string.Format("Registering phone line " + model.UserName));
		    }
		    catch (Exception exception)
		    {
                Log.Write("Error during SIP registration: " + exception.Message);
                MessageBox.Show("Error during SIP registration: " + exception.Message);
		    }
		}

		public void UnregisterPhoneLine()
		{
			if (PhoneLine == null)
				return;

			Softphone.UnregisterPhoneLine(PhoneLine);
		}

		private void phoneLine_RegistrationStateChanged(object sender, RegistrationStateChangedArgs e)
		{
			Log.Write("Phone line state changed to " + e.State);
		}

        #region Alarm calls

        public void StartAlarmCalls()
		{
            if (PhoneLine == null)
                return;

            if (Softphone == null)
                return;

            if (!PhoneLine.RegistrationInfo.IsRegistered)
                return;

            foreach (var number in AlarmList)
            {
                var callEngine = new OutGoingCallEngine(number, Softphone, PhoneLine);
                callEngine.Connect(_cameraAudio, _cameraVideo);
                callEngine.CallStateChanged += AlarmCall_CallStateChanged;
                alarmCalls.Add(callEngine);
                callEngine.StartCall();
            }
		}

        private void AlarmCall_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            var call = sender as ICall;

            Log.Write("Call state changed to " + e.State);

            if (e.State.IsCallEnded())
            {
                call.CallStateChanged -= AlarmCall_CallStateChanged;
            }
        }

        public void CloseAlarmCalls()
        {
            foreach (var item in alarmCalls)
                item.Close();
        }

        #endregion

        #region Single call

        private void softphone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            var incomingCall = e.Item;

            if (Call != null)
            {
                incomingCall.Reject();
                return;
            }

            incomingCall.CallStateChanged += SingleCall_CallStateChanged;
            ConnectToCall(incomingCall);
            incomingCall.Answer();

            Call = incomingCall;
        }

        public void StartCall(string dial)
        {
            if (Call != null)
                return;

            var dialParams = new DialParameters(dial) { CallType = CallType.AudioVideo };
            var call = Softphone.CreateCallObject(PhoneLine, dialParams);
            call.CallStateChanged += SingleCall_CallStateChanged;
            ConnectToCall(call);
            call.Start();

            Call = call;
        }

        private void SingleCall_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            var call = sender as ICall;
            if (call == null)
                return;

            Log.Write("Call state changed to " + e.State);

            CurrentCallState = call.CallState;

            if (call.CallState.IsCallEnded())
            {
                DisconnectFromCall();
                call.CallStateChanged -= SingleCall_CallStateChanged;
                Call = null;
            }
        }

	    private void ConnectToCall(ICall call)
		{
            if (call == null)
                return;

            _videoSender.AttachToCall(call);
            _audioSender.AttachToCall(call);

            if (_videoSender == null || _audioSender == null)
                return;

			_connector.Connect(_cameraVideo, _videoSender);
			_connector.Connect(_cameraAudio, _audioSender);
		}

	    private void DisconnectFromCall()
	    {
            _videoSender.Detach();
            _audioSender.Detach();

	        if (_videoSender == null || _audioSender == null)
                return;
            
            _connector.Disconnect(_cameraVideo, _videoSender);
            _connector.Disconnect(_cameraAudio, _audioSender);
	    }

		public void CloseCall()
		{
		    if (Call != null)
		        Call.HangUp();
		}

        #endregion

	    public event PropertyChangedEventHandler PropertyChanged;
	    protected void OnPropertyChanged(string propertyName)
	    {
	        var handler = PropertyChanged;
	        if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
	    }
	}
}
