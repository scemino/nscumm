using System;
using Microsoft.Xna.Framework;
using System.IO;

namespace NScumm.MonoGame
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    public class MainMenuScreen : MenuScreen
    {
        ScummScreen screen;

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
            this.screen = screen;
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
                        screen.LoadGame(entryIndex);
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
                        screen.SaveGame(entryIndex);
                        ExitScreen();
                    }
                    else
                    {
                        OnCancel();
                    }
                    break;
            }
        }

        void UpdateMenus()
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
                    foreach (var game in screen.GetSaveGames())
                    {
                        MenuEntries.Add(Path.GetFileNameWithoutExtension(game));
                    }
                    MenuEntries.Add("Back");
                    break;
                case MenuState.Save:
                    MenuEntries.Clear();
                    foreach (var game in screen.GetSaveGames())
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
            base.ExitScreen();
        }

        void OnExitGame()
        {
            ScreenManager.Game.Exit();
        }
    }
}