using MonoGame.Framework;
using NScumm.Core.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace NScumm.MonoGame
{
    public sealed partial class GamePage
    {
        private ScummGame _game;

        internal static GameDetected _info;

        public GamePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var info = e.Parameter as GameDetected;
            _info = info;
            _game = XamlGame<ScummGame>.Create("", Window.Current.CoreWindow, GamePanel);
        }
    }
}
