//#define CLEAN_GAMELIST
//#define WP_COMI

using NScumm.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Windows.Foundation;
using System.Reactive.Linq;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using System.Reactive.Concurrency;
using System.Windows.Input;
using Microsoft.Xna.Framework.Input;
using NScumm.Core.IO;
using System.Reflection;

namespace NScumm.MonoGame.ViewModels
{
    public interface IGameLibraryViewModel
    {
        bool IsScanning { get; }
        bool ShowNoGameMessage { get; }
        IEnumerable<GameViewModel> Games { get; }

        ICommand AddCommand { get; }
    }

    class GameLibraryViewModel : ViewModel, IGameLibraryViewModel
    {
        static readonly HashSet<string> indexFiles = new HashSet<string>(new string[] { ".D64", ".DSK,", ".LFL", ".000", ".LA0", ".SM0", ".DNR" }, StringComparer.OrdinalIgnoreCase);
        private ObservableCollection<GameViewModel> _games;
        private HashSet<string> _gameSignatures;
        private HashSet<string> _folders;
        private bool _isScanning;
        private bool _showNoGameMessage;
        private DelegateCommand _addCommand;
        private ApplicationDataContainer _gamesContainer;
        private ApplicationDataContainer _foldersContainer;

        public IEnumerable<GameViewModel> Games { get { return _games; } }

        public bool IsScanning
        {
            get { return _isScanning; }
            private set { RaiseAndSetIfChanged(ref _isScanning, value); }
        }

        public bool ShowNoGameMessage
        {
            get { return _showNoGameMessage; }
            private set { RaiseAndSetIfChanged(ref _showNoGameMessage, value); }
        }

        public ICommand AddCommand
        {
            get { return _addCommand; }
        }

        public GameDetector GameDetector { get; private set; }

        public GameLibraryViewModel()
        {
            _showNoGameMessage = true;
            _gameSignatures = new HashSet<string>();
            _games = new ObservableCollection<GameViewModel>();
            _addCommand = new DelegateCommand(Scan);

            GameDetector = new GameDetector("$plugins");

            LoadGameLibrary();
        }

        private void LoadGameLibrary()
        {
            LoadGameFolders();
            LoadGames();
        }

        private void LoadGames()
        {
#if CLEAN_GAMELIST && DEBUG
            ApplicationData.Current.LocalSettings.DeleteContainer("Games");
#endif
            _gamesContainer = ApplicationData.Current.LocalSettings.CreateContainer("Games", ApplicationDataCreateDisposition.Always);
            var paths = (from gameContainer in _gamesContainer.Containers.Values
                         let path = (string)gameContainer.Values["Path"]
                         select path).ToList();
            var games = (from path in paths
                         where File.Exists(path)
                         let game = GameDetector.DetectGame(path)
                         where game != null
                         where !_gameSignatures.Contains(game.Game.Path)
                         orderby game.Game.Description
                         select game).ToObservable();
            games
                .Buffer(TimeSpan.FromMilliseconds(200))
                .SubscribeOn(Scheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(items =>
                {
                    foreach (var item in items)
                    {
                        _gameSignatures.Add(item.Game.Path);
                        _games.Add(new GameViewModel(item));
                    }
                },
                e => MessageBox.Show("nSCUMM", e.Message, new[] { "OK" }),
                () => UpdateShowNoGameMessage());
        }

        private void LoadGameFolders()
        {
#if CLEAN_GAMELIST && DEBUG
            ApplicationData.Current.LocalSettings.DeleteContainer("Folders");
#endif
            _foldersContainer = ApplicationData.Current.LocalSettings.CreateContainer("Folders", ApplicationDataCreateDisposition.Always);
            var folders = from folderContainer in _foldersContainer.Containers.Values
                          let path = (string)folderContainer.Values["Path"]
                          select path;
            _folders = new HashSet<string>(folders, StringComparer.OrdinalIgnoreCase);
        }

        private async void Scan()
        {
            var folder = await PickFolder();
            if (folder == null)
            {
                return;
            }

            IsScanning = true;

            UpdateShowNoGameMessage();

            // scan for games
            var obsItems = GetFilesAsync(folder);
            var indexes = from item in obsItems
                          where indexFiles.Contains(Path.GetExtension(item.Path))
                          let g = GameDetector.DetectGame(item.Path)
                          where g != null
                          where !_gameSignatures.Contains(g.Game.Path)
                          select g;

            // add games every 200 ms
            indexes
                .Buffer(TimeSpan.FromMilliseconds(200), Scheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(items =>
                {
                    foreach (var item in items)
                    {
                        var name = string.Format("Game{0}", _gamesContainer.Containers.Count + 1);
                        var gameContainer = _gamesContainer.CreateContainer(name, ApplicationDataCreateDisposition.Always);
                        gameContainer.Values["Path"] = item.Game.Path;
                        _games.Add(new GameViewModel(item));
                    }
                },
            () =>
            {
                // hide progression when finished
                IsScanning = false;
                UpdateShowNoGameMessage();
            });
        }

        private void UpdateShowNoGameMessage()
        {
            ShowNoGameMessage = !IsScanning && _games.Count == 0;
        }

        private void AddFolder(StorageFolder folder)
        {
            if (folder == null) return;

            // check if the folder already existin the folder list
            if (!_folders.Contains(folder.Path))
            {
                _folders.Add(folder.Path);

                var token = string.Format("Folder{0}", _folders.Count);

                // Application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, folder);

                var gameFolder = _foldersContainer.CreateContainer(token, ApplicationDataCreateDisposition.Always);
                gameFolder.Values["Path"] = folder.Path;
            }
        }

        private async System.Threading.Tasks.Task<StorageFolder> PickFolder()
        {
            StorageFolder folder = null;
#if WP_COMI
            try
            {
                folder = await StorageFolder.GetFolderFromPathAsync(@"d:\Downloads\comi");
                AddFolder(folder);
            }
            catch (UnauthorizedAccessException)
            {
            }
#else
            var folderPicker = new FolderPicker();
            indexFiles.ToList().ForEach(folderPicker.FileTypeFilter.Add);
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            try
            {
                folder = await folderPicker.PickSingleFolderAsync();
                AddFolder(folder);
            }
            catch (UnauthorizedAccessException)
            {
            }
#endif
            return folder;
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
    }
}
