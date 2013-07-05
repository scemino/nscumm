//
//  ScummGame.cs
//
//  Author:
//       Valéry Sablonnière <scemino74@gmail.com>
//
//  Copyright (c) 2013 
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


#region Using Statements

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NScumm.Core;

#endregion

namespace NScumm.GL
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class ScummGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        ScummEngine engine;
        GameInfo info;
        XnaGraphicsManager gfx;
        XnaInputManager inputManager;

        TimeSpan tsDelta;
        Vector2 cursorPos;

        bool quitRequested;
        KeyboardState oldKeyboardState;
        bool pause;

        public ScummGame (GameInfo info)
        {
            if (info == null)
                throw new ArgumentNullException ("info");

            base.IsMouseVisible = true;
            //base.IsFixedTimeStep = true;
            base.Window.AllowUserResizing = true;

            graphics = new GraphicsDeviceManager (this);
            Content.RootDirectory = "Content";
            this.info = info;            
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize ()
        {
            // update title
            base.Window.Title = string.Format ("NScumm - {0} [{1}]", info.Description, info.Culture.NativeName);

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch (GraphicsDevice);
            inputManager = new XnaInputManager (Window);
            gfx = new XnaGraphicsManager (GraphicsDevice);

            // init engines
            engine = new ScummEngine (info, gfx, inputManager);
            engine.ShowMenuDialogRequested += OnShowMenuDialogRequested;

            base.Initialize ();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent ()
        {
            engine.RunBootScript ();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent ()
        {
            gfx.Dispose ();
        }

        void OnShowMenuDialogRequested (object sender, EventArgs e)
        {
            Gtk.Application.Init ();
            var win = new MenuWindow ();
            win.LoadRequested += (o1,e1) => {
                engine.Load (e1.Filename);
                win.Destroy();
                Gtk.Application.Quit ();
            };
            win.SaveRequested += (o1,e1) => {
                engine.Save (e1.Filename);
                win.Destroy();
                Gtk.Application.Quit ();
            };
            win.QuitRequested += (o1,e1) => {
                quitRequested = true;
                win.Destroy();
                Gtk.Application.Quit ();
            };
            win.DeleteEvent+=(o,e1)=> {
                Gtk.Application.Quit ();
                e1.RetVal=true;
            };
            win.ShowAll ();
            Gtk.Application.Run ();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update (GameTime gameTime)
        {
            if (quitRequested) {
                Exit ();
            }

            // toggle fullscreen ?
            var keyboardState = Keyboard.GetState (PlayerIndex.One);
            if (oldKeyboardState.IsKeyDown (Keys.LeftAlt) && 
                oldKeyboardState.IsKeyDown (Keys.Enter) && 
                keyboardState.IsKeyDown (Keys.LeftAlt) && 
                keyboardState.IsKeyUp (Keys.Enter)) {
                graphics.IsFullScreen = !graphics.IsFullScreen;
                base.IsMouseVisible = !graphics.IsFullScreen;
                graphics.ApplyChanges ();
            }
            if (oldKeyboardState.IsKeyDown (Keys.Space) && keyboardState.IsKeyUp (Keys.Space)) {
                pause=!pause;
            }
            oldKeyboardState = keyboardState;

            // update mouse position
            var mouseState = Mouse.GetState ();
            cursorPos = new Vector2 (mouseState.X, mouseState.Y);

            if (tsDelta == TimeSpan.Zero) {
                tsDelta = engine.Loop (tsDelta);
                gfx.UpdateScreen ();
            } else {
                if (gameTime.ElapsedGameTime > tsDelta) {
                    tsDelta = TimeSpan.Zero;
                } else if (!pause) {
                    tsDelta -= gameTime.ElapsedGameTime;
                }
            }

            base.Update (gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw (GameTime gameTime)
        {
            GraphicsDevice.Clear (Color.Black);

            spriteBatch.Begin (SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            gfx.DrawScreen (spriteBatch);
            gfx.DrawCursor (spriteBatch, cursorPos);
            spriteBatch.End ();

            base.Draw (gameTime);
        }
    }
}
