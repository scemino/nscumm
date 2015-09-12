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

namespace NScumm.MonoGame
{
    public class ScummScreen : GameScreen
    {
        readonly GameSettings info;
        SpriteBatch spriteBatch;
        ScummEngine engine;
        XnaGraphicsManager gfx;
        XnaInputManager inputManager;
        TimeSpan tsToWait;
        Vector2 cursorPos;
#if WINDOWS_UWP
        NullMixer audioDriver;
#else
        XnaAudioDriver audioDriver;
#endif
        Game game;
        bool contentLoaded;
        private SpriteFont font;

        public bool IsPaused
        {
            get;
            set;
        }

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
                gfx = new XnaGraphicsManager(info.Game.Width, info.Game.Height, game.Window, ScreenManager.GraphicsDevice);
#if WINDOWS_UWP
                audioDriver = new NullMixer();
#else
                audioDriver = new XnaAudioDriver();
#endif

                // init engines
                engine = ScummEngine.Create(info, gfx, inputManager, audioDriver);
                //engine = ScummEngine.Create(info, gfx, inputManager, null);
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
                IsPaused = true;
                ScreenManager.AddScreen(new MainMenuScreen(this));
            }
            else
            {
                IsPaused = false;
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
                IsPaused = !IsPaused;
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
            tsToWait = engine.RunBootScript(info.BootParam);
            while (!engine.HasToQuit)
            {
                if (!IsPaused)
                {
                    // Wait...
                    engine.WaitForTimer((int)tsToWait.TotalMilliseconds);
                    tsToWait = engine.Loop();
                    gfx.UpdateScreen();
                }
            }
            base.ScreenManager.Game.Exit();
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullNone);
            gfx.DrawScreen(spriteBatch);
            gfx.DrawCursor(spriteBatch, cursorPos);
            spriteBatch.DrawString(font, string.Format("x: {0:F2} y: {1:F2}", cursorPos.X, cursorPos.Y), new Vector2(10, 20), Color.White);
            spriteBatch.DrawString(font, ScreenManager.Game.Window.CurrentOrientation.ToString(), new Vector2(10, 40), Color.White);
            spriteBatch.DrawString(font, ScreenManager.Game.Window.ClientBounds.ToString(), new Vector2(10, 60), Color.White);
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

