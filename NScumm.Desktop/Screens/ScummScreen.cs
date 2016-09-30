//
//  ScummScreen.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.IO;
using NScumm.Services;

namespace NScumm.Desktop.Screens
{
    public class ScummScreen : GameScreen
    {
        private readonly GameSettings _info;
        private SpriteBatch _spriteBatch;
        private IEngine _engine;
        private XnaGraphicsManager _gfx;
        private XnaInputManager _inputManager;
        private IAudioOutput _audioDriver;
        private readonly Game _game;
        private bool _contentLoaded;
        //		private SpriteFont font;

        public ScummScreen(Game game, GameSettings info)
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.0);
            TransitionOffTime = TimeSpan.FromSeconds(1.0);

            _game = game;
            _info = info;
        }

        public override void LoadContent()
        {
            if (_contentLoaded) return;

            _contentLoaded = true;
            _spriteBatch = new SpriteBatch(ScreenManager.GraphicsDevice);

            //				font = ScreenManager.Content.Load<SpriteFont> ("Fonts/MenuFont");
            _inputManager = new XnaInputManager(ScreenManager.Game, _info.Game);
            _gfx = new XnaGraphicsManager(_info.Game.Width, _info.Game.Height, _info.Game.PixelFormat, ScreenManager.GraphicsDevice);
            ScreenManager.Game.Services.AddService<Core.Graphics.IGraphicsManager>(_gfx);
            var saveFileManager = ServiceLocator.SaveFileManager;
#if WINDOWS_UWP
                audioDriver = new XAudio2Mixer();
#else
            _audioDriver = new XnaAudioDriver();
#endif
            _audioDriver.Play();

            // init engines
            _engine = _info.MetaEngine.Create(_info, new OSystem(_gfx, _inputManager, saveFileManager, _audioDriver));
            _engine.ShowMenuDialogRequested += OnShowMenuDialogRequested;

            foreach (var sw in _info.Switches.Split(','))
            {
                if (string.Equals(sw, "all", StringComparison.OrdinalIgnoreCase))
                    DebugManager.Instance.EnableAllDebugChannels();
                else if (!DebugManager.Instance.EnableDebugChannel(sw))
                    DebugHelper.Warning("Engine does not support debug level '{0}'", sw);
            }

            _game.Services.AddService(_engine);

            Task.Factory.StartNew(UpdateGame);
        }

        public override void EndRun()
        {
            _engine.HasToQuit = true;
            _audioDriver.Stop();
            base.EndRun();
        }

        public override void UnloadContent()
        {
            _gfx.Dispose();
            _audioDriver.Dispose();
        }

        public override void HandleInput(InputState input)
        {
            if (input.IsNewKeyPress(Keys.Enter) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                var gdm = ((ScummGame)_game).GraphicsDeviceManager;
                gdm.ToggleFullScreen();
                gdm.ApplyChanges();
            }
            else if (input.IsNewKeyPress(Keys.Space))
            {
                _engine.IsPaused = !_engine.IsPaused;
            }
            else {
                _inputManager.UpdateInput(input.CurrentKeyboardState);
                base.HandleInput(input);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();
            _gfx.DrawScreen(_spriteBatch);
            _gfx.DrawCursor(_spriteBatch);
            _spriteBatch.End();
        }

        private void UpdateGame()
        {
            try
            {
                _engine.Run();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e);
                Console.ResetColor();
            }
            ScreenManager.Game.Exit();
        }

        private void OnShowMenuDialogRequested(object sender, EventArgs e)
        {
            if (!_engine.IsPaused)
            {
                _engine.IsPaused = true;
                var page = _game.Services.GetService<IMenuService>();
                page.ShowMenu();
            }
        }

    }

}

