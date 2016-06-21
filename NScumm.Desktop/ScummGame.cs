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

namespace NScumm
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    class ScummGame : Game
    {
        readonly ScreenManager _screenManager;

        public GameSettings Settings { get; private set; }

        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        public ScreenManager ScreenManager { get { return _screenManager; } }

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
            GraphicsDeviceManager.PreferredBackBufferWidth = 800;
            GraphicsDeviceManager.PreferredBackBufferHeight = (int)(800.0 * Settings.Game.Height / Settings.Game.Width);
#else
            Settings = new GameSettings(GamePage.Info.Game, GamePage.Info.Engine);
            GraphicsDeviceManager.PreferredBackBufferWidth = Settings.Game.Width;
            GraphicsDeviceManager.PreferredBackBufferHeight = Settings.Game.Height;
#endif
            _screenManager = new ScreenManager(this);
            Components.Add(_screenManager);
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
            _screenManager.AddScreen(new BackgroundScreen());
            _screenManager.AddScreen(new ScummScreen(this, Settings));

            base.Initialize();
        }

        protected override void EndRun()
        {
            _screenManager.EndRun();
            base.EndRun();
        }
    }
}
