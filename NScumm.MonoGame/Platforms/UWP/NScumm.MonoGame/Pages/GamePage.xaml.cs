using MonoGame.Framework;
using NScumm.Core.IO;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace NScumm.MonoGame
{
    public sealed partial class GamePage
    {
        private ScummGame _game;

        internal static GameInfo _info;
        private SystemNavigationManager _view;

        public GamePage()
        {
            InitializeComponent();

            _view = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            _view.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
            _view.BackRequested += View_BackRequested;
        }

        private void View_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                _view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                Frame.GoBack();
                e.Handled = true;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var info = e.Parameter as GameInfo;
            _info = info;
            _game = XamlGame<ScummGame>.Create("", Window.Current.CoreWindow, GamePanel);
        }
    }
}
