//
//  GameLibraryActivity.cs
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

using System.Linq;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using NScumm.Core.IO;
using NScumm.Sky;
using System.Collections.Generic;
using NScumm.Core;
using Android.Content;

namespace NScumm.Mobile.Droid
{
	[Activity (Label = "nSCUMM", MainLauncher = true)]			
	public class GameLibraryActivity : ListActivity
	{
		GameDetected[] data;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			ServiceLocator.FileStorage = new FileStorage (Assets);

			var gd = new GameDetector ();
			gd.Add (new SkyMetaEngine ());

			var directory = Intent.GetStringExtra ("Directory");
			if (directory == null) {
				StartActivity (typeof(FilePickerActivity));
				return;
			} else {
				var dirFile = new Java.IO.File (directory);
				data = GetRecursiveFiles (dirFile)
				.Select (f => {
					System.Console.WriteLine (f.Path);
					return f;	
				})
				.Select (f => gd.DetectGame (f.Path))
				.Where (g => g != null)
				.Select (g => g).ToArray ();

				var adapter = new GameListAdapter (this, data);
				ListAdapter = adapter;
			}
		}

		public override void OnBackPressed ()
		{
			Toast.MakeText (this, "Back", ToastLength.Long);
			base.OnBackPressed ();
		}

		private IEnumerable<Java.IO.File> GetRecursiveFiles (Java.IO.File file)
		{
			if (file.IsDirectory) {
				foreach (var f in file.ListFiles()) {
					foreach (var sf in GetRecursiveFiles(f)) {
						yield return sf;
					}
				}
			} else {
				yield return file;
			}
		}

		protected override void OnListItemClick (ListView l, View v, int position, long id)
		{
			base.OnListItemClick (l, v, position, id);
			var activity2 = new Intent (this, typeof(ScummActivity));
			activity2.PutExtra ("Game", data [position].Game.Path);
			StartActivity (activity2);
		}
	}
}

