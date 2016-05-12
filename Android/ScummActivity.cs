//
//  ScummActivity.cs
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

using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;

using Microsoft.Xna.Framework;
using System.IO;
using NScumm.Core.IO;
using NScumm.Sky;
using NScumm.Core;
using NScumm.Services;
using NScumm.Sword1;
using NScumm.Scumm.IO;
using NScumm.Mobile.Resx;

namespace NScumm.Mobile.Droid
{
	[Activity (Label = "@string/app_name", 
		Icon = "@drawable/icon",
		Theme = "@style/Theme.Splash",
		AlwaysRetainTaskState = true,
		LaunchMode = LaunchMode.SingleInstance,
		ConfigurationChanges = ConfigChanges.Orientation |
		ConfigChanges.KeyboardHidden |
		ConfigChanges.Keyboard)]
	public class ScummActivity : AndroidGameActivity
	{
		ScummGame game;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Initialize ();

			var path = Intent.GetStringExtra ("Game");
			if (path == null) {
                Toast.MakeText (this, AppResources.Error_NoGameSelected, ToastLength.Short).Show ();
				return;
			}

			var gd = new GameDetector ();
            gd.Add(new SkyMetaEngine());
            gd.Add(new ScummMetaEngine());
			gd.Add(new Sword1MetaEngine());

			var info = gd.DetectGame (path);
			if (info == null) {
                Toast.MakeText (this, AppResources.Error_GameNotSupported, ToastLength.Short).Show ();
				return;
			}

			((AudioManager)ServiceLocator.AudioManager).Directory = Path.GetDirectoryName (info.Game.Path);
			var settings = new GameSettings (info.Game, info.Engine) {
				AudioDevice = "adlib",
				CopyProtection = false
			};

			// Create our OpenGL view, and display it
			game = new ScummGame (settings);
			game.Services.AddService<IMenuService> (new MenuService (game));
			SetContentView (game.Services.GetService<View> ());
			game.Run ();

		}

		private void Initialize ()
		{
			ServiceLocator.Platform = new Platform ();
			ServiceLocator.FileStorage = new FileStorage (Assets);
			ServiceLocator.SaveFileManager = new SaveFileManager ();
			ServiceLocator.AudioManager = new AudioManager ();
			ServiceLocator.TraceFatory = new TraceFactory ();
		}
	}
}


