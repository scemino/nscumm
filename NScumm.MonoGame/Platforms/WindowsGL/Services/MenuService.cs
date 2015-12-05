using System;
using System.Linq;

namespace NScumm.MonoGame.Services
{
    class MenuService : IMenuService
    {
        private ScummGame _game;

        public MenuService(ScummGame game)
        {
            _game = game;
        }

        public void ShowMenu()
        {
            var scummScreen = _game.ScreenManager.GetScreens().OfType<ScummScreen>().FirstOrDefault();
            _game.ScreenManager.AddScreen(new MainMenuScreen(scummScreen));
        }
    }
}
