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
using NScumm.Core.IO;
using NScumm.Core;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NScumm.Core.Audio;

namespace NScumm.MonoGame
{
    public class ScummScreen : GameScreen
    {
        readonly GameSettings info;
        SpriteBatch spriteBatch;
        IEngine engine;
        XnaGraphicsManager gfx;
        XnaInputManager inputManager;
        Vector2 cursorPos;
        IAudioOutput audioDriver;
        Game game;
        bool contentLoaded;
        private SpriteFont font;

        public ScummScreen(Game game, GameSettings info)
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.0);
            TransitionOffTime = TimeSpan.FromSeconds(1.0);

            this.game = game;
            this.info = info;
        }

        public override void LoadContent()
        {
            if (!contentLoaded)
            {
                contentLoaded = true;
                spriteBatch = new SpriteBatch(ScreenManager.GraphicsDevice);

                font = ScreenManager.Content.Load<SpriteFont>("Fonts/MenuFont");
                inputManager = new XnaInputManager(game.Window, info.Game.Width, info.Game.Height);
                gfx = new XnaGraphicsManager(info.Game.Width, info.Game.Height, info.Game.PixelFormat, game.Window, ScreenManager.GraphicsDevice);
#if WINDOWS_UWP
                audioDriver = new XAudio2Mixer();
#else
                audioDriver = new XnaAudioDriver();
#endif
                audioDriver.Play();

                // init engines
                engine = info.MetaEngine.Create(info, gfx, inputManager, audioDriver);
                engine.ShowMenuDialogRequested += OnShowMenuDialogRequested;

                Task.Factory.StartNew(() =>
                {
                    UpdateGame();
                });
            }
        }

        public override void EndRun()
        {
            engine.HasToQuit = true;
            audioDriver.Stop();
            base.EndRun();
        }

        void OnShowMenuDialogRequested(object sender, EventArgs e)
        {
            var isMenuActive = IsMenuActive();
            if (!isMenuActive)
            {
                engine.IsPaused = true;
                ScreenManager.AddScreen(new MainMenuScreen(this));
            }
            else
            {
                engine.IsPaused = false;
            }
        }

        bool IsMenuActive()
        {
            var isMenuActive = Array.Exists(ScreenManager.GetScreens(), screen => screen is MainMenuScreen);
            return isMenuActive;
        }

        public override void UnloadContent()
        {
            gfx.Dispose();
            audioDriver.Dispose();
        }

        public override void HandleInput(InputState input)
        {
            if (input.IsNewKeyPress(Keys.F))
            {
                var gdm = ((ScummGame)game).GraphicsDeviceManager;
                gdm.ToggleFullScreen();
                gdm.ApplyChanges();
            }
            else if (input.IsNewKeyPress(Keys.Space))
            {
                engine.IsPaused = !engine.IsPaused;
            }
            else
            {
                inputManager.UpdateInput(input.CurrentKeyboardState);
                cursorPos = inputManager.RealPosition;
                base.HandleInput(input);
            }
        }

        void UpdateGame()
        {
            engine.Run();
            base.ScreenManager.Game.Exit();
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            gfx.DrawScreen(spriteBatch);
            gfx.DrawCursor(spriteBatch, cursorPos);
            spriteBatch.End();
        }

        public string[] GetSaveGames()
        {
#if WINDOWS_UWP
            var dir = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
            var pattern = string.Format("{0}*.sav", info.Game.Id);
            return ServiceLocator.FileStorage.EnumerateFiles(dir, pattern).ToArray();
#else
            var dir = Path.GetDirectoryName(info.Game.Path);
            return ServiceLocator.FileStorage.EnumerateFiles(dir, "*.sav").ToArray();
#endif
        }

        public void LoadGame(int index)
        {
            var filename = GetSaveGamePath(index);
            engine.Load(filename);
        }

        public void SaveGame(int index)
        {
            var filename = GetSaveGamePath(index);
            engine.Save(filename);
        }

        string GetSaveGamePath(int index)
        {
#if WINDOWS_UWP
            var dir = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
            var filename = Path.Combine(dir, string.Format("{0}{1}.sav", info.Game.Id, (index + 1)));
            return filename;
#else
            var dir = Path.GetDirectoryName(info.Game.Path);
            var filename = Path.Combine(dir, string.Format("{0}{1}.sav", info.Game.Id, (index + 1)));
            return filename;
#endif
        }
    }
}

