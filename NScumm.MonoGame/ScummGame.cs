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
using NScumm.Core.IO;

#region Using Statements
using Microsoft.Xna.Framework;

#endregion
namespace NScumm.MonoGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    class ScummGame : Game
    {
        readonly GameSettings _settings;
        readonly ScreenManager _screenManager;

        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }

        public ScummGame(GameSettings settings)
        {
            IsMouseVisible = false;
            IsFixedTimeStep = false;
            Window.AllowUserResizing = true;

            Content.RootDirectory = "Content";
            _settings = settings;
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            GraphicsDeviceManager.PreferredBackBufferWidth = 800;
            GraphicsDeviceManager.PreferredBackBufferHeight = (int)(800.0 * settings.Game.Height / settings.Game.Width);

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
            Window.Title = string.Format("NScumm - {0} [{1}/{2}]", _settings.Game.Description, _settings.Game.Variant, _settings.Game.Culture.NativeName);
            _screenManager.AddScreen(new BackgroundScreen());
            _screenManager.AddScreen(new ScummScreen(this, _settings));

            base.Initialize();
        }
    }
}
