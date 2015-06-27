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
using System.Threading;
using System.Threading.Tasks;

namespace NScumm.MonoGame
{
    public class ScummScreen: GameScreen
    {
        readonly GameSettings info;
        SpriteBatch spriteBatch;
        ScummEngine engine;
        XnaGraphicsManager gfx;
        XnaInputManager inputManager;
        TimeSpan tsToWait;
        Vector2 cursorPos;
#if WINDOWS_UAP
        NullMixer audioDriver;
#else
        XnaAudioDriver audioDriver;
#endif
        Game game;
        bool contentLoaded;

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

                inputManager = new XnaInputManager(game.Window, info.Game.Width, info.Game.Height);
                gfx = new XnaGraphicsManager(info.Game.Width, info.Game.Height, game.Window, ScreenManager.GraphicsDevice);
#if WINDOWS_UAP
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
            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt) && input.IsNewKeyPress(Keys.Enter))
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
                UpdateMouseState();
                inputManager.UpdateInput(Mouse.GetState(), input.CurrentKeyboardState);
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

        void UpdateMouseState()
        {
            var state = Mouse.GetState();
            var x = state.X;
            var y = state.Y;
            cursorPos = new Vector2(x, y);
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullNone);
            gfx.DrawScreen(spriteBatch);
            gfx.DrawCursor(spriteBatch, cursorPos);
            spriteBatch.End();
        }

        public string[] GetSaveGames()
        {
            var dir = Path.GetDirectoryName(info.Game.Path);
            return ServiceLocator.FileStorage.EnumerateFiles(dir, "*.sav").ToArray();
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
            var dir = Path.GetDirectoryName(info.Game.Path);
            var filename = Path.Combine(dir, string.Format("{0}{1}.sav", info.Game.Id, (index + 1)));
            return filename;
        }
    }
}

