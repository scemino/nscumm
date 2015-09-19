using System;
using System.Windows.Input;

namespace NScumm.MonoGame.ViewModels
{
    public class DelegateCommand : ICommand
    {
        private Action _execute;
        private Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action action, Func<bool> canExecute = null)
        {
            _execute = action;
            _canExecute = canExecute ?? new Func<bool>(() => true);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            var eh = CanExecuteChanged;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }
    }
}
