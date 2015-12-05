using System;
using NScumm.Core;
using Windows.UI.Core;

namespace NScumm.MonoGame.Services
{
    class MenuService : IMenuService
    {
        private readonly CoreDispatcher _dispatcher;

        public ScummGame Game { get; set; }

        private IEngine Engine
        {
            get
            {
                var engine = Game.Services.GetService<IEngine>();
                return engine;
            }
        }

        public MenuService(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async void ShowMenu()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
             {
                 if (Engine.IsPaused)
                 {
                     Game.IsMouseVisible = true;
                     var menu = new Pages.MainMenu();
                     menu.Game = Game;
                     var result = await menu.ShowAsync();
                     Game.IsMouseVisible = false;
                     Engine.IsPaused = false;
                 }
             });
        }
    }
}
