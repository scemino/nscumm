using NScumm.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using System;

namespace NScumm.MonoGame.Pages
{
    public sealed partial class MainMenu : ContentDialog
    {
        private ResourceLoader _loader;

        internal ScummGame Game { get; set; }

        private IEngine Engine
        {
            get
            {
                var engine = Game.Services.GetService<IEngine>();
                return engine;
            }
        }

        public MainMenu()
        {
            InitializeComponent();

            _loader = ResourceLoader.GetForViewIndependentUse();
        }

        private void OnResume(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Hide();
        }

        private void OnBack(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Title = _loader.GetString("MainMenu_Title");
            MainStackPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
            LoadStackPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            SaveStackPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void OnLoad(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Title = _loader.GetString("MainMenu_LoadTitle");
            LoadGameList.Items.Clear();
            var savegames = GetSavegames();
            savegames.ForEach(LoadGameList.Items.Add);

            MainStackPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            LoadStackPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void OnSave(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Title = _loader.GetString("MainMenu_SaveTitle");
            SaveGameList.Items.Clear();
            var savegames = GetSavegames();
            savegames.ForEach(SaveGameList.Items.Add);
            SaveGameList.Items.Add(_loader.GetString("New Entry"));

            MainStackPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            SaveStackPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void OnExit(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Hide();
            Engine.IsPaused = false;
            Engine.HasToQuit = true;
        }

        private void OnLoadGame(object sender, ItemClickEventArgs e)
        {
            var index = ((ListView)sender).Items.IndexOf(e.ClickedItem);
            LoadGame(index);
            Hide();
        }

        private void OnSaveGame(object sender, ItemClickEventArgs e)
        {
            var index = ((ListView)sender).Items.IndexOf(e.ClickedItem);
            SaveGame(index);
            Hide();
        }


        private List<string> GetSavegames()
        {
            var dir = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
            var pattern = string.Format("{0}*.sav", Game.Settings.Game.Id);
            return ServiceLocator.FileStorage.EnumerateFiles(dir, pattern).Select(Path.GetFileNameWithoutExtension).ToList();
        }

        private string GetSaveGamePath(int index)
        {
            var dir = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
            var filename = Path.Combine(dir, string.Format("{0}{1}.sav", Game.Settings.Game.Id, (index + 1)));
            return filename;
        }

        private void LoadGame(int index)
        {
            var filename = GetSaveGamePath(index);
            Engine.Load(filename);
        }

        private void SaveGame(int index)
        {
            var filename = GetSaveGamePath(index);
            Engine.Save(filename);
        }
    }
}
