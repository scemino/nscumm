//
//  GameLibraryViewModel.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

using ReactiveUI;
using Splat;
using System;
using System.Linq;
using NScumm.Core.IO;
using NScumm.Sky;
using System.Reactive.Linq;
using System.Reactive;
using System.Runtime.Serialization;
using NScumm.Sword1;
using System.IO;
using NScumm.Mobile.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using NScumm.Scumm.IO;
using NScumm.Mobile.Resx;

namespace NScumm.Mobile.ViewModels
{
	[DataContract]
	public class GameLibraryViewModel : ReactiveObject, IRoutableViewModel
	{
		[IgnoreDataMember]
		ReactiveList<GameViewModel> _games;

		[IgnoreDataMember]
		IGameService _gameService;

		[IgnoreDataMember]
		public string UrlPathSegment {
			get { return AppResources.GameLibrary_Title; }
		}

		[IgnoreDataMember]
		public IScreen HostScreen { get; protected set; }

		[IgnoreDataMember]
		public IReactiveCommand<IList<GameViewModel>> Scan { get; private set; }

		[IgnoreDataMember]
		public IReactiveCommand<object> Delete { get; private set; }

		[IgnoreDataMember]
		public IReactiveCommand LaunchGame { get; private set; }

		[DataMember]
		public IReactiveList<GameViewModel> Games {
			get { return _games; }
		}

		public GameLibraryViewModel (IScreen hostScreen = null)
		{
			_gameService = new GameService ();
			HostScreen = hostScreen ?? Locator.Current.GetService<IScreen> ();
			_games = new ReactiveList<GameViewModel> ();

			LoadGamesAsync ()
                .Select (o => o.Games.Select (g => new GameViewModel (g.Description, g.Path)))
                .ObserveOn (RxApp.MainThreadScheduler)
                .Subscribe (games => _games.AddRange (games));

			// create commands
			Delete = ReactiveCommand.Create ();
			Delete.Subscribe (DeleteImpl);

			Scan = ReactiveCommand.CreateAsyncObservable (ScanImpl);
			Scan.ThrownExceptions.ObserveOn (RxApp.MainThreadScheduler).Subscribe (e => {
				this.Log ().ErrorException ("Scan error", e);
			});

			LaunchGame = ReactiveCommand.CreateAsyncObservable (LaunchGameImpl);

			Scan.ObserveOn (RxApp.MainThreadScheduler)
                .Subscribe (games => {
				using (_games.SuppressChangeNotifications ()) {
					_games.Clear ();
					_games.AddRange (games);
				}
			});

			// auto save
			this.AutoPersist (x => {
				var library = new GameLibrary {
					Games = x.Games.Select (g => new Game { Description = g.Description, Path = g.Path }).ToArray ()
				};
				return SaveGamesAsync (library);
			}, _games.Changed);
		}

		private IObservable<Unit> LaunchGameImpl (object parameter)
		{
			var game = (GameViewModel)parameter;
			_gameService.StartGame (game.Path);
			return Observable.Return (Unit.Default);
		}

		private void DeleteImpl (object parameter)
		{
			var game = (GameViewModel)parameter;
			var gameToRemove = _games.FirstOrDefault (g => g.Path == game.Path);
			if (gameToRemove == null)
				return;
			_games.Remove (gameToRemove);
		}

		private IObservable<IList<GameViewModel>> ScanImpl (object parameter)
		{
			var directory = _gameService.GetDirectory ();
			var gd = CreateGameDetector ();
			var games = new List<GameViewModel> ();
			return Observable.Create<IList<GameViewModel>> (observer => {

				return GetFilesAsync (directory)
					.Select (file => gd.DetectGame (file))
	                        .Where (g => g != null)
					.Select (g => CreateGameViewModel (g.Game))
					.Subscribe (games.Add,
					() => {
						observer.OnNext (games);
						observer.OnCompleted ();
					});
			});
		}

		private IObservable<GameLibrary> LoadGamesAsync ()
		{
			var path = GetGameLibraryPath ();
			if (!File.Exists (path))
				return Observable.Return (CreateEmptyLibrary ());

			return Observable.Start (() => {
				using (var fs = File.OpenRead (path)) {
					var reader = new StreamReader (fs);
					var json = reader.ReadToEnd ();
					var gameLibrary = JsonConvert.DeserializeObject<GameLibrary> (json);
					return gameLibrary ?? CreateEmptyLibrary ();
				}
			});
		}

		private IObservable<Unit> SaveGamesAsync (GameLibrary library)
		{
			var path = GetGameLibraryPath ();
			var serializer = new JsonSerializer ();
			using (var fs = new StreamWriter (path))
			using (var writer = new JsonTextWriter (fs)) {
				serializer.Serialize (writer, library);
			}
			return Observable.Return (Unit.Default);
		}

		private IObservable<string> GetFilesAsync (string directory)
		{
			return Observable.Create<string> (observer => {
				return Observable.Start (() => {
					ScanDirectory (directory, observer);
					observer.OnCompleted ();
				}, RxApp.TaskpoolScheduler).Subscribe();
			});
		}

		private void ScanDirectory (string directory, IObserver<string> files)
		{
			try {
				this.Log ().Info ($"Scan Directory {directory}");
				var entries = Directory.EnumerateFileSystemEntries (directory, "*", SearchOption.TopDirectoryOnly);
				foreach (var entry in entries) {
					if (Directory.Exists (entry)) {
						ScanDirectory (entry, files);
					} else {
						this.Log ().Info ($"Scan {entry}");
						files.OnNext (entry);
					}
				}
			} catch (UnauthorizedAccessException) {
				// ignore exception
			}
		}

		private static GameDetector CreateGameDetector ()
		{
			var gd = new GameDetector ();
			gd.Add (new SkyMetaEngine ());
			gd.Add (new ScummMetaEngine ());
			gd.Add (new Sword1MetaEngine ());
			return gd;
		}

		private static GameViewModel CreateGameViewModel (IGameDescriptor game)
		{
			return new GameViewModel (game.Description, game.Path);
		}

		private static string GetGameLibraryPath ()
		{
			var documents = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			var path = Path.Combine (documents, "games.json");
			return path;
		}

		private static GameLibrary CreateEmptyLibrary ()
		{
			return new GameLibrary { Games = new Game[0] };
		}
	}
}

