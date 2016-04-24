//
//  GameService.cs
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

using System;
using NScumm.Core;
using NScumm.Services;
using NScumm.Core.IO;
using NScumm.Sky;
using NScumm.Sword1;
using System.Linq;
using System.IO;

namespace NScumm.Mobile.Services
{
	public class GameService: IGameService
	{
		public string GetDirectory ()
		{
			var directory = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			return directory;
		}

		public void StartGame (string path)
		{
			Initialize ();

			var gd = new GameDetector ();
			gd.Add (new SkyMetaEngine ());
			gd.Add (new Sword1MetaEngine ());

			var info = gd.DetectGame (path);
			if (info == null) {
				//Toast.MakeText (this, Resources.GetText (Resource.String.game_not_supported), ToastLength.Short).Show ();
				return;
			}

			((AudioManager)ServiceLocator.AudioManager).Directory = Path.GetDirectoryName (info.Game.Path);
			var settings = new GameSettings (info.Game, info.Engine) {
				AudioDevice = "adlib",
				CopyProtection = false
			};

			// Create our OpenGL view, and display it
			var game = new ScummGame (settings);
			game.Services.AddService<IMenuService> (new MenuService (game));
			game.Run ();
		}

		private static void Initialize ()
		{
			ServiceLocator.Platform = new Platform ();
			ServiceLocator.FileStorage = new FileStorage ();
			ServiceLocator.SaveFileManager = new SaveFileManager ();
			ServiceLocator.AudioManager = new AudioManager ();
			var switches = Enumerable.Empty<string> ();
			ServiceLocator.TraceFatory = new TraceFactory (switches);
		}
	}
}
