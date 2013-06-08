/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using NScumm.Core;
using NScumm.Core.Graphics;
#endregion

namespace NScumm.Windows
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class ScummGame : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private ScummEngine engine;
        private ScummIndex index;
        private GameInfo info;
        private XnaGraphicsManager gfx;
        private XnaInputManager inputManager;
        private TimeSpan tsDelta;
        private Microsoft.Xna.Framework.Vector2 cursorPos;

        public ScummGame(GameInfo info)
            : base()
        {
            base.IsMouseVisible = true;
            base.IsFixedTimeStep = false;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.info = info;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            index = new ScummIndex();
            index.LoadIndex(info.Path);

            // update title
            base.Window.Title = string.Format("NSucmm - {0} [{1}]", info.Description, info.Culture.NativeName);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            inputManager = new XnaInputManager(Window);
            gfx = new XnaGraphicsManager(GraphicsDevice);

            // init engines
            engine = new ScummEngine(index, info, gfx, inputManager);
            engine.ShowMenuDialogRequested += OnShowMenuDialogRequested;
            engine.RunBootScript();
        }

        private void OnShowMenuDialogRequested(object sender, EventArgs e)
        {
            var dialog = new MenuDialog();
            dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dialog.Engine = this.engine;
            dialog.ShowDialog();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            gfx.Dispose();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // toggle fullscreen
            var keyboardState = Keyboard.GetState(PlayerIndex.One);
            if (keyboardState.IsKeyDown(Keys.LeftAlt) && keyboardState.IsKeyDown(Keys.Enter))
            {
                graphics.IsFullScreen = !graphics.IsFullScreen;
                graphics.ApplyChanges();
            }

            // update mouse
            var mouseState = Mouse.GetState();
            cursorPos = new Vector2(mouseState.X, mouseState.Y);

            var dt = DateTime.Now;
            tsDelta = engine.Loop(tsDelta);
            System.Threading.Thread.Sleep(tsDelta);

            gfx.UpdateScreen();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            spriteBatch.Begin();
            gfx.DrawScreen(spriteBatch);
            gfx.DrawCursor(spriteBatch, cursorPos);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
