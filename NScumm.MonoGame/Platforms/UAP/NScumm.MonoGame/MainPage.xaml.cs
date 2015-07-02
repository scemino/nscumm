using NScumm.Core;
using NScumm.Core.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using System.Reactive.Windows.Foundation;

namespace NScumm.MonoGame
{
    public sealed partial class MainPage
    {
        static readonly HashSet<string> indexFiles = new HashSet<string>(new string[] { ".D64", ".DSK,", ".LFL", ".000", ".LA0" }, StringComparer.OrdinalIgnoreCase);

        public MainPage()
        {
            InitializeComponent();

            // get the nscumm.xml
            var gamesInfoFile = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "Content", "Nscumm.xml");

            var gmInfo = ServiceLocator.FileStorage.OpenFileRead(gamesInfoFile);
            GameManager = GameManager.Create(gmInfo);

            if (ApplicationData.Current.LocalSettings.Containers.ContainsKey("Games"))
            {
                var gamesContainer = ApplicationData.Current.LocalSettings.Containers["Games"];
                var games = (from gameContainer in gamesContainer.Containers.Values
                             let path = (string)gameContainer.Values["Path"]
                             let game = GameManager.GetInfo(path)
                             orderby game.Description
                             select game).ToList();
                GameListBox.ItemsSource = games;
                NoGameTextBlock.Visibility = games.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                Scan();
            }
        }

        public GameManager GameManager { get; private set; }

        private void Scan()
        {
            NoGameTextBlock.Visibility = Visibility.Collapsed;
            var container = ApplicationData.Current.LocalSettings.CreateContainer("Games", ApplicationDataCreateDisposition.Always);
            ProgressPanel.Visibility = Visibility.Visible;
            var games = new ObservableCollection<GameInfo>();
            GameListBox.ItemsSource = games;

            // scan for games
            var obsItems = GetFilesAsync(KnownFolders.DocumentsLibrary);
            var indexes = from item in obsItems
                          where indexFiles.Contains(Path.GetExtension(item.Path))
                          let g = GameManager.GetInfo(item.Path)
                          where g != null
                          select g;

            // add games every 200 ms
            indexes
                .Buffer(TimeSpan.FromMilliseconds(200))
                .ObserveOnDispatcher().Subscribe(items =>
            {
                foreach (var item in items)
                {
                    var gameContainer = container.CreateContainer(item.MD5, ApplicationDataCreateDisposition.Always);
                    gameContainer.Values["Path"] = item.Path;
                    games.Add(item);
                }
            },
            () =>
            {
                // hide progression when finished
                ProgressPanel.Visibility = Visibility.Collapsed;
                NoGameTextBlock.Visibility = games.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private IObservable<StorageFile> GetFilesAsync(StorageFolder folder)
        {
            var obsItems = folder.GetItemsAsync().ToObservable();
            var files = (from item in obsItems
                         from i in item
                         select i is StorageFile ? Observable.Return((StorageFile)i) : GetFilesAsync((StorageFolder)i))
                         .SelectMany(i => i);
            return files;
        }

        private void PickAFileButton_Click(object sender, RoutedEventArgs e)
        {
            Scan();
        }

        private void GameListBox_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // navigate to the game
            var info = GameListBox.SelectedItem as GameInfo;
            Frame.Navigate(typeof(GamePage), info);
        }
    }
}
