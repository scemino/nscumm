using MonoGame.Framework;
using NScumm.Core.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using NScumm.MonoGame.Services;

namespace NScumm.MonoGame
{
    public sealed partial class GamePage
    {
        internal static GameDetected Info;

        private ScummGame _game;
        private readonly MenuService _menuService;

        public GamePage()
        {
            InitializeComponent();
            _menuService = new MenuService(Dispatcher);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var info = e.Parameter as GameDetected;
            Info = info;
            _game = XamlGame<ScummGame>.Create("", Window.Current.CoreWindow, GamePanel);
            _menuService.Game = _game;
            _game.Services.AddService<IMenuService>(_menuService);
        }
    }
}
