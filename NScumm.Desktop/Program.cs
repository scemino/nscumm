//
//  Program.cs
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
using System.IO;
using System.Linq;
using NScumm.Core.IO;
using NScumm.Services;

namespace NScumm
{
	/// <summary>
	/// The main class.
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main (string[] args)
		{
			var options = new ScummOptionSet ();
			var extras = options.Parse (args);

			if (extras.Count != 1) {
				return 1;
			}

			Initialize (options.Switches);
			var path = ScummHelper.NormalizePath (extras [0]);
			if (!File.Exists (path)) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Error.WriteLine ("The file {0} does not exist.", path);
				Console.ResetColor ();
				return 1;
			}

			var pluginsdDirectory = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "plugins");
			var gd = new GameDetector ();
			gd.AddPluginsFromDirectory (pluginsdDirectory);

			var info = gd.DetectGame (path);
			if (info == null) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Error.WriteLine ("This game is not supported, sorry please contact me if you want to support this game.");
				Console.ResetColor ();
				return 1;
			}

			((AudioManager)ServiceLocator.AudioManager).Directory = Path.GetDirectoryName (info.Game.Path);
			var settings = new GameSettings (info.Game, info.Engine) {
				AudioDevice = options.MusicDriver,
				CopyProtection = options.CopyProtection,
				BootParam = options.BootParam
			};
			var game = new ScummGame (settings);
			game.Services.AddService<IMenuService> (new MenuService (game));
			game.Run ();
			return 0;
		}

		private static void Initialize (string sw)
		{
			ServiceLocator.Platform = new Platform ();
			ServiceLocator.FileStorage = new FileStorage ();
			ServiceLocator.SaveFileManager = new SaveFileManager (ServiceLocator.FileStorage);
			ServiceLocator.AudioManager = new AudioManager ();
			var switches = string.IsNullOrEmpty (sw) ? Enumerable.Empty<string> () : sw.Split (',');
			ServiceLocator.TraceFatory = new TraceFactory (switches);
		}
	}
}