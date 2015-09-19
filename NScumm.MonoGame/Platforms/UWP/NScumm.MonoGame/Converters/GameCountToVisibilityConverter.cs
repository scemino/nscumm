using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NScumm.MonoGame.Converters
{
    class ShowNoGameMessageToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var showNoGameMessage = (bool)value;
            return showNoGameMessage ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
