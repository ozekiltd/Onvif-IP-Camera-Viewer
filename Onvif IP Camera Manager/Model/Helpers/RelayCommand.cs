using System;
using System.Windows.Input;

namespace Onvif_IP_Camera_Manager.Model.Helpers
{
    class RelayCommand : ICommand
    {
        Action<object> _action;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action action)
        {
            _action = i => action();
        }

        public RelayCommand(Action<object> action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

    }
}
