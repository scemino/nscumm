//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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
using Microsoft.Xna.Framework;
using System.IO;
using NScumm.Core;
using System.Linq;

namespace NScumm.MonoGame
{
    public class MainMenuScreen : MenuScreen
    {
        private ScummScreen _screen;

        enum MenuState
        {
            Main,
            Load,
            Save
        }

        MenuState State
        {
            get;
            set;
        }

        enum MainMenuEntry
        {
            LoadGame,
            SaveGame,
            ResumeGame,
            Exit
        }

        /// <summary>
        /// Constructs a new MainMenu object.
        /// </summary>
        public MainMenuScreen(ScummScreen screen)
        {
            this._screen = screen;
            // set the transition times
            TransitionOnTime = TimeSpan.FromSeconds(1.0);
            TransitionOffTime = TimeSpan.FromSeconds(1.0);
            UpdateMenus();
        }

        /// <summary>
        /// Updates the screen. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                    bool coveredByOtherScreen)
        {

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }


        /// <summary>
        /// Responds to user menu selections.
        /// </summary>
        protected override void OnSelectEntry(int entryIndex)
        {
            switch (State)
            {
                case MenuState.Main:
                    switch ((MainMenuEntry)entryIndex)
                    {
                        case MainMenuEntry.LoadGame:
                            State = MenuState.Load;
                            UpdateMenus();
                            break;

                        case MainMenuEntry.SaveGame:
                            State = MenuState.Save;
                            UpdateMenus();
                            break;
                        case MainMenuEntry.ResumeGame:
                            OnCancel();
                            break;
                        case MainMenuEntry.Exit:
                            OnExitGame();
                            break;
                    }
                    break;
                case MenuState.Load:
                    if (entryIndex != MenuEntries.Count - 1)
                    {
                        LoadGame(entryIndex);
                        ExitScreen();
                    }
                    else
                    {
                        OnCancel();
                    }
                    break;
                case MenuState.Save:
                    if (entryIndex != MenuEntries.Count - 1)
                    {
                        SaveGame(entryIndex);
                        ExitScreen();
                    }
                    else
                    {
                        OnCancel();
                    }
                    break;
            }
        }

        private void UpdateMenus()
        {
            switch (State)
            {
                case MenuState.Main:
                    MenuEntries.Clear();
                    MenuEntries.Add("Load Game");
                    MenuEntries.Add("Save Game");
                    MenuEntries.Add("Resume Game");
                    MenuEntries.Add("Exit");
                    break;
                case MenuState.Load:
                    MenuEntries.Clear();
                    foreach (var game in GetSaveGames())
                    {
                        MenuEntries.Add(Path.GetFileNameWithoutExtension(game));
                    }
                    MenuEntries.Add("Back");
                    break;
                case MenuState.Save:
                    MenuEntries.Clear();
                    foreach (var game in GetSaveGames())
                    {
                        MenuEntries.Add(Path.GetFileNameWithoutExtension(game));
                    }
                    MenuEntries.Add("New Slot");
                    MenuEntries.Add("Back");
                    break;
            }

        }

        public override void Draw(GameTime gameTime)
        {
            var viewport = ScreenManager.GraphicsDevice.Viewport;
            var rectangle = new Rectangle(0, 0, viewport.Width, viewport.Height);
            ScreenManager.DrawRectangle(rectangle, new Color(Color.Black, 0.7f), Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend);
            base.Draw(gameTime);
        }
        
        /// <summary>
        /// When the user cancels the main menu, ask if they want to exit the sample.
        /// </summary>
        protected override void OnCancel()
        {
            if (State == MenuState.Main)
            {
                ExitScreen();
            }
            else
            {
                State = MenuState.Main;
                UpdateMenus();
            }
        }

        public override void ExitScreen()
        {
            var engine = GetEngine();
            engine.IsPaused = false;
            base.ExitScreen();
        }


        private void OnExitGame()
        {
            ScreenManager.Game.Exit();
        }

        private string[] GetSaveGames()
        {
            var game = GetGame();
            var dir = Path.GetDirectoryName(game.Path);
            return ServiceLocator.FileStorage.EnumerateFiles(dir, "*.sav").ToArray();
        }

        private Core.IO.IGameDescriptor GetGame()
        {
            return ((ScummGame)ScreenManager.Game).Settings.Game;
        }

        private IEngine GetEngine()
        {
            return ScreenManager.Game.Services.GetService<IEngine>();
        }

        private void LoadGame(int index)
        {
            var engine = GetEngine();
            var filename = GetSaveGamePath(index);
            engine.Load(filename);
        }

        private void SaveGame(int index)
        {
            var engine = GetEngine();
            var filename = GetSaveGamePath(index);
            engine.Save(filename);
        }

        private string GetSaveGamePath(int index)
        {
            var game = GetGame();
            var dir = Path.GetDirectoryName(game.Path);
            var filename = Path.Combine(dir, string.Format("{0}{1}.sav", game.Id, (index + 1)));
            return filename;
        }
    }
}