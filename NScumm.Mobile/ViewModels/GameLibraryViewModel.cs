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
using System.Reactive.Disposables;
using NScumm.Mobile.Services;
using Newtonsoft.Json;

namespace NScumm.Mobile.ViewModels
{
	[DataContract]
	public class GameLibraryViewModel:ReactiveObject, IRoutableViewModel
	{
		ReactiveList<GameViewModel> _games;
		IGameService _gameService;

		public string UrlPathSegment {
			get { return "GameLibrary"; }
		}

		public IScreen HostScreen { get; protected set; }

		public IReactiveCommand<string> Scan { get; private set; }
		public IReactiveCommand<Unit> Delete { get; private set; }

		public IReactiveCommand LaunchGame { get; private set; }

		public IReactiveList<GameViewModel> Games { 
			get { return _games; } 
		}

		public GameLibraryViewModel (IScreen hostScreen = null)
		{
			_gameService = new GameService ();
			HostScreen = hostScreen ?? Locator.Current.GetService<IScreen> ();

			var gamesLibrary = LoadGamesAsync ().Wait ();
			var games = gamesLibrary.Games.Select (g => new GameViewModel (g.Description, g.Path));
			_games = new ReactiveList<GameViewModel> (games);

			// create commands
			Delete = ReactiveCommand.CreateAsyncObservable(DeleteImpl);
			Scan = ReactiveCommand.CreateAsyncObservable <string> (ScanImpl);
			Scan.ThrownExceptions.Subscribe (e => this.Log ().ErrorException ("Scan", e));
				
			LaunchGame = ReactiveCommand.CreateAsyncObservable (LaunchGameImpl);

			var gd = new GameDetector ();
			gd.Add (new SkyMetaEngine ());
//			gd.Add (new ScummMetaEngine ());
			gd.Add (new Sword1MetaEngine ());
			Scan.ObserveOn (RxApp.MainThreadScheduler)
				.Select (f => gd.DetectGame (f))
				.Where (g => g != null)
				.Select (g => CreateGameViewModel (g.Game))
				.Subscribe (_games.Add);
			
			// auto save
			this.AutoPersist (x => {
				var library = new GameLibrary {
					Games = x.Games.Select (g => new Game{ Description = g.Description, Path = g.Path }).ToArray ()
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

		private static GameViewModel CreateGameViewModel (IGameDescriptor game)
		{
			return new GameViewModel (game.Description, game.Path);
		}

		private IObservable<Unit> DeleteImpl(object parameter)
		{
			var game = (GameViewModel)parameter;
			_games.Remove (game);
			return Observable.Return (Unit.Default);
		}

		private IObservable<string> ScanImpl (object parameter)
		{
			_games.Clear ();
			var directory = _gameService.GetDirectory ();
			return GetFilesAsync (directory);
		}

		private static string GetGameLibraryPath ()
		{
			var documents = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			var path = Path.Combine (documents, "games.json");
			return path;
		}

		private IObservable<GameLibrary> LoadGamesAsync ()
		{
			var path = GetGameLibraryPath ();
			if (!File.Exists (path))
				return Observable.Return (CreateEmptyLibrary ());

			using (var fs = File.OpenRead (path)) {
				var reader = new StreamReader (fs);
				var json = reader.ReadToEnd ();
				var gameLibrary = JsonConvert.DeserializeObject<GameLibrary> (json);
				return Observable.Return (gameLibrary ?? CreateEmptyLibrary ());
			}
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
				Observable.Start (() => {
					var files = Directory.EnumerateFiles (directory, "*", SearchOption.AllDirectories);
					foreach (var f in files) {
						observer.OnNext (f);
					}
					observer.OnCompleted ();
				}, RxApp.TaskpoolScheduler);
				return Disposable.Empty;
			});
		}

		private static GameLibrary CreateEmptyLibrary ()
		{
			return new GameLibrary { Games = new Game [0] };
		}
	}
}

