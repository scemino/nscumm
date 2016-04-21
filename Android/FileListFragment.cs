//
//  FileListFragment.cs
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Content;

namespace NScumm.Mobile.Droid
{
	/// <summary>
	///   A ListFragment that will show the files and subdirectories of a given directory.
	/// </summary>
	/// <remarks>
	///   <para> This was placed into a ListFragment to make this easier to share this functionality with with tablets. </para>
	///   <para> Note that this is a incomplete example. It lacks things such as the ability to go back up the directory tree, or any special handling of a file when it is selected. </para>
	/// </remarks>
	public class FileListFragment : ListFragment
	{
		public static readonly string DefaultInitialDirectory = "/";
		private FileListAdapter _adapter;
		private DirectoryInfo _directory;

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			_adapter = new FileListAdapter(Activity, new FileSystemInfo[0]);
			ListAdapter = _adapter;
		}

		public override void OnListItemClick(ListView l, View v, int position, long id)
		{
			var fileSystemInfo = _adapter.GetItem(position);

			if (fileSystemInfo.IsFile())
			{
				// Do something with the file.  In this case we just pop some toast.
				Log.Verbose("FileListFragment", "The file {0} was clicked.", fileSystemInfo.FullName);
				//Toast.MakeText(Activity, "You selected file " + fileSystemInfo.FullName, ToastLength.Short).Show();
				var activity = new Intent (Activity, typeof(GameLibraryActivity));
				activity.PutExtra ("Directory", fileSystemInfo.FullName);
				StartActivity (activity);
			}
			else
			{
				// Dig into this directory, and display it's contents
				RefreshFilesList(fileSystemInfo.FullName);
			}

			base.OnListItemClick(l, v, position, id);
		}

		public override void OnResume()
		{
			base.OnResume();
			RefreshFilesList(DefaultInitialDirectory);
		}

		public void RefreshFilesList(string directory)
		{
			IList<FileSystemInfo> visibleThings = new List<FileSystemInfo>();
			var dir = new DirectoryInfo(directory);

			try
			{
				foreach (var item in dir.GetFileSystemInfos().Where(item => item.IsVisible()))
				{
					visibleThings.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Error("FileListFragment", "Couldn't access the directory " + _directory.FullName + "; " + ex);
				Toast.MakeText(Activity, "Problem retrieving contents of " + directory, ToastLength.Long).Show();
				return;
			}

			_directory = dir;

			_adapter.AddDirectoryContents(visibleThings);

			// If we don't do this, then the ListView will not update itself when then data set 
			// in the adapter changes. It will appear to the user that nothing has happened.
			ListView.RefreshDrawableState();

			Log.Verbose("FileListFragment", "Displaying the contents of directory {0}.", directory);
		}
	}
}
