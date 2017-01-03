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

using Microsoft.Xna.Framework;
using NScumm.Core.IO;
using NScumm.Core;
using System.Threading.Tasks;
using NScumm.Mobile.Services;
using System;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace NScumm 
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	class ScummGame : Game
	{
		XnaInputManager inputManager;
		XnaGraphicsManager gfx;
		XnaAudioDriver audioDriver;
		IEngine engine;
		SpriteBatch spriteBatch;
		InputState input = new InputState();
		SpriteFont font;

		public GameSettings Settings { get; private set; }

		public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

		public ScummGame()
			: this(null)
		{
		}

		public ScummGame(GameSettings settings)
		{
			IsMouseVisible = false;
			IsFixedTimeStep = false;
			Window.AllowUserResizing = true;

			Content.RootDirectory = "Content";

			GraphicsDeviceManager = new GraphicsDeviceManager(this);
#if !WINDOWS_UWP
			Settings = settings;
			//			GraphicsDeviceManager.PreferredBackBufferWidth = 800;
			//			GraphicsDeviceManager.PreferredBackBufferHeight = (int)(800.0 * Settings.Game.Height / Settings.Game.Width);
#else
            Settings = new GameSettings(GamePage.Info.Game, GamePage.Info.Engine);
            GraphicsDeviceManager.PreferredBackBufferWidth = Settings.Game.Width;
            GraphicsDeviceManager.PreferredBackBufferHeight = Settings.Game.Height;
#endif
			GraphicsDeviceManager.IsFullScreen = true;
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
            Window.Title = string.Format("nSCUMM - {0} [{1}]", Settings.Game.Description, Settings.Game.Language);
			base.Initialize();

			//font = Content.Load<SpriteFont>("Fonts/MenuFont");
			spriteBatch = new SpriteBatch(GraphicsDevice);
			inputManager = new XnaInputManager(this, Settings.Game);
			gfx = new XnaGraphicsManager(Settings.Game.Width, Settings.Game.Height, Settings.Game.PixelFormat, GraphicsDevice);
			Services.AddService<Core.Graphics.IGraphicsManager>(gfx);
			var saveFileManager = ServiceLocator.SaveFileManager;

#if WINDOWS_UWP
			audioDriver = new XAudio2Mixer();
#else
			audioDriver = new XnaAudioDriver();
#endif
			audioDriver.Play();

			// init engines
            engine = Settings.MetaEngine.Create(Settings, new OSystem(gfx, inputManager, saveFileManager, audioDriver));
			engine.ShowMenuDialogRequested += OnShowMenuDialogRequested;
			Services.AddService(engine);

#if WINDOWS_UWP
			Task.Factory.StartNew (() => {
				UpdateGame ();
			});
#else
			var thread = new System.Threading.Thread(new System.Threading.ThreadStart(UpdateGame));
			thread.IsBackground = true;
			thread.Start();
#endif
		}

		protected override void UnloadContent()
		{
			gfx.Dispose();
			audioDriver.Dispose();
			base.UnloadContent();
		}

		private void UpdateGame()
		{
			engine.Run();
#if __ANDROID__
			Exit();
#endif
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.Black);

			spriteBatch.Begin();
			gfx.DrawScreen(spriteBatch);
#if DEBUG
			var state = inputManager.GetState();
			//spriteBatch.DrawString(font,
			//	string.Format("Left: {0:3}, Right {1:3}",
			//		state.IsLeftButtonDown ? "on" : "off",
			//		state.IsRightButtonDown ? "on" : "off"),
			//	new Vector2(10, 10),
			//	Color.White);
#endif
			gfx.DrawCursor(spriteBatch, inputManager.RealPosition);
			spriteBatch.End();

			base.Draw(gameTime);
		}

		protected override void Update(GameTime gameTime)
		{
			// Read the keyboard and gamepad.
			input.Update();

			if (input.IsNewKeyPress(Keys.Enter) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
			{
				var gdm = GraphicsDeviceManager;
				gdm.ToggleFullScreen();
				gdm.ApplyChanges();
			}
			else if (input.IsNewKeyPress(Keys.Space))
			{
				engine.IsPaused = !engine.IsPaused;
			}
			else {
				inputManager.UpdateInput(input.CurrentKeyboardState);
			}
			base.Update(gameTime);
		}

		private void OnShowMenuDialogRequested(object sender, EventArgs e)
		{
			if (!engine.IsPaused)
			{
				engine.IsPaused = true;
				var page = Services.GetService<IMenuService>();
				page.ShowMenu();
			}
		}
	}
}
