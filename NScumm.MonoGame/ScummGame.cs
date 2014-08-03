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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NScumm.Core;

#endregion
namespace NScumm.MonoGame
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
		TimeSpan tsToWait;
		Vector2 cursorPos;
		SpriteFont defaultFont;
		OpenALDriver audioDriver;

		public ScummGame (GameInfo info)
		{
			IsMouseVisible = true;
			IsFixedTimeStep = false;
			Window.AllowUserResizing = true;

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
			Window.Title = string.Format ("NScumm - {0} [{1}]", info.Description, info.Culture.NativeName);

			base.Initialize ();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent ()
		{
			defaultFont = Content.Load<SpriteFont> ("spriteFont");
			spriteBatch = new SpriteBatch (GraphicsDevice);
			inputManager = new XnaInputManager (Window);
			gfx = new XnaGraphicsManager (GraphicsDevice);
			audioDriver = new OpenALDriver ();
			// init engines
			engine = new ScummEngine (info, gfx, inputManager, audioDriver);
			engine.ShowMenuDialogRequested += OnShowMenuDialogRequested;
			tsToWait = engine.RunBootScript ();
		}

		void OnShowMenuDialogRequested (object sender, EventArgs e)
		{
//			var dialog = new MenuDialog();
//			dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
//			dialog.Engine = this.engine;
//			dialog.ShowDialog();
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent ()
		{
			engine.ShowMenuDialogRequested -= OnShowMenuDialogRequested;
			gfx.Dispose ();
			audioDriver.Dispose ();
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update (GameTime gameTime)
		{
			UpdateKeyboardState ();
			UpdateMouseState ();
			UpdateGame ();            

			base.Update (gameTime);
		}

		void UpdateGame ()
		{
			engine.UpdateSound ();

			inputManager.UpdateStates ();
			System.Threading.Thread.Sleep (tsToWait);
			tsToWait = engine.Loop ();
			gfx.UpdateScreen ();
		}

		void UpdateKeyboardState ()
		{
			var keyboardState = Keyboard.GetState ();
			if (keyboardState.IsKeyDown (Keys.LeftAlt) && keyboardState.IsKeyDown (Keys.Enter)) {
				graphics.IsFullScreen = !graphics.IsFullScreen;
				graphics.ApplyChanges ();
			}
		}

		void UpdateMouseState ()
		{
			var mouseState = Mouse.GetState ();
			cursorPos = new Vector2 (mouseState.X, mouseState.Y);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw (GameTime gameTime)
		{
			GraphicsDevice.Clear (Color.Black);

			spriteBatch.Begin (SpriteSortMode.BackToFront, BlendState.AlphaBlend,
				SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
			gfx.DrawScreen (spriteBatch);
			gfx.DrawCursor (spriteBatch, cursorPos);
			spriteBatch.End ();

			base.Draw (gameTime);
		}
	}
}
