using System;

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
using System.Linq;
using NScumm.Sword1;

namespace NScumm.Mobile.Droid
{
	[Activity (Label = "nScumm", 
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
				Toast.MakeText (this, Resources.GetText (Resource.String.no_game_selected), ToastLength.Short).Show ();
				return;
			}

			var gd = new GameDetector ();
			gd.Add(new SkyMetaEngine());
			gd.Add(new Sword1MetaEngine());

			var info = gd.DetectGame (path);
			if (info == null) {
				Toast.MakeText (this, Resources.GetText (Resource.String.game_not_supported), ToastLength.Short).Show ();
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
			var switches = Enumerable.Empty<string> ();
			ServiceLocator.TraceFatory = new TraceFactory (switches);
		}
	}
}


