using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using NScumm.Agos;
using NScumm.Core;
using NScumm.Core.IO;
using NScumm.Mobile.Services;
using Android.Views;
using System.Linq;
using Android.Support.V4.Content;
using Android.Content.PM;
using System.Collections.Generic;
using System;

namespace NScumm.Droid
{
	[Activity(Label = "nScumm", MainLauncher = true,
			  Icon = "@drawable/Plus_48")]
	public class GameLibraryActivity : Activity
	{
		GameDetectorService _gd;
		ListView _listView1;
		List<string> _pathes;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			SetContentView(Resource.Layout.libraryView);

			var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
			SetActionBar(toolbar);

			Initialize();

			_gd = new GameDetectorService();

			_listView1 = FindViewById<ListView>(Resource.Id.listView1);
			_listView1.Adapter = new GameListAdapter(this);
			_listView1.ItemClick += OnListItemClick;
		}

		protected override void OnPause()
		{
			base.OnPause();

			var preferences = GetPreferences(FileCreationMode.Private);
			var editor = preferences.Edit();
			editor.PutStringSet("Pathes", ((GameListAdapter)_listView1.Adapter).Select(o => o.Path).ToList());
			editor.Commit();
		}

		protected override void OnResume()
		{
			var preferences = GetPreferences(FileCreationMode.Private);
			var pathes = preferences.GetStringSet("Pathes", null);
			if (pathes == null)
			{
				base.OnResume();
				return;
			}

			_pathes = pathes.ToList();
			TryGetReadLibrary();

			base.OnResume();
		}

		private void TryGetReadLibrary()
		{
			if ((int)Build.VERSION.SdkInt < 23)
			{
				GetReadLibrary();
				return;
			}

			GetReadLibraryPermission();
		}

		private void GetReadLibrary()
		{
			((GameListAdapter)_listView1.Adapter).AddAll(_pathes.Select(o => GetGame(o).Game).ToList());
		}

		private void GetReadLibraryPermission()
		{
			if (CheckSelfPermission(Android.Manifest.Permission.ReadExternalStorage) == (int)Permission.Granted)
			{
				GetReadLibrary();
				return;
			}

			// Need to request permission
			if (ShouldShowRequestPermissionRationale(Android.Manifest.Permission.ReadExternalStorage))
			{
				//Explain to the user why we need to read the contacts
				new AlertDialog.Builder(this)
						.SetMessage("Read access is required to show game library.")
							   .SetPositiveButton("OK", (o, e) => RequestPermissions(new string[] { Android.Manifest.Permission.ReadExternalStorage }, 1))
						.Show();
				return;
			}

			RequestPermissions(new string[] { Android.Manifest.Permission.ReadExternalStorage }, 1);
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
		{
			if (requestCode == 1 && grantResults[0] == Permission.Granted)
			{
				// Permission granted
				GetReadLibrary();
				return;
			}
			OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			MenuInflater.Inflate(Resource.Menu.top_menus, menu);
			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			if (item.ItemId == Resource.Id.menu_edit)
			{
				AddGame();
			}
			return base.OnOptionsItemSelected(item);
		}

		private void AddGame()
		{
			var intent = new Intent(this, typeof(DirectoryPickerActivity));
			StartActivityForResult(intent, 1);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			// Check which request we're responding to
			if (requestCode != 1) return;

			// Make sure the request was successful
			if (resultCode != Result.Ok) return;

			// Try to detect game
			var path = data.GetStringExtra("Path");
			var game = GetGame(path);
			if (game == null)
			{
				// No game, show message
				new AlertDialog.Builder(this).SetMessage("No Game detected").Create().Show();
				return;
			}

			// Add game in the library
			((GameListAdapter)_listView1.Adapter).Add(game.Game);
		}

		private GameDetected GetGame(string path)
		{
			var game = _gd.DetectGame(path);
			return game;
		}

		private void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			var game = ((GameListAdapter)_listView1.Adapter).GetItem(e.Position);
			if (game != null)
			{
				var intent = new Intent(this, typeof(ScummGameActivity));
				intent.PutExtra("Game", game.Path);
				StartActivity(intent);
			}
		}

		private void Initialize()
		{
			ServiceLocator.Platform = new Mobile.Services.Platform();
			ServiceLocator.FileStorage = new FileStorage();
			ServiceLocator.SaveFileManager = new SaveFileManager();
			ServiceLocator.AudioManager = new Mobile.Services.AudioManager();
			ServiceLocator.TraceFatory = new TraceFactory();
		}
	}
}
