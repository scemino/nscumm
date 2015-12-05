using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NScumm.MonoGame.ViewModels
{
    internal abstract class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eh = PropertyChanged;
            eh?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }
    }
}
