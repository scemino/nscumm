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


using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using NScumm.Core;
using NScumm.Core.IO;
using NScumm.Droid;
using NScumm.Droid.Services;
using NScumm.Queen;
using NScumm.Sky;

namespace NScumm.Mobile.Droid
{
    [Activity(Label = "nScumm - Game Library", MainLauncher = true)]
    public class GameLibraryActivity : ListActivity
    {
        GameDetector _gameDetector;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ServiceLocator.FileStorage = new DroidFileStorage();

            var directory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

            _gameDetector = new GameDetector();
            _gameDetector.Add(new SkyMetaEngine());
            //gd.Add(new ScummMetaEngine());
            //gd.Add(new Sword1MetaEngine());
            //_gameDetector.Add(new QueenMetaEngine());

            var games = GetGames(directory);
            ListAdapter = new GameDescriptorAdapter(this, games);
        }

        protected override void OnListItemClick(ListView l, View v, int position, long id)
        {
            var intent = new Android.Content.Intent(this, typeof(MainActivity));
            var game = ((GameDescriptorAdapter)ListAdapter)[position];
            intent.PutExtra("Game", game.Path);
            StartActivity(intent);
        }

        private IList<IGameDescriptor> GetGames(string directory)
        {
            var files = new List<IGameDescriptor>();
            ScanDirectory(directory, files);
            return files;
        }

        private void ScanDirectory(string directory, List<IGameDescriptor> files)
        {
            try
            {
                Android.Util.Log.Info(MainActivity.LogTag, $"Scan Directory {directory}");
                var entries = Directory.EnumerateFileSystemEntries(directory, "*", System.IO.SearchOption.AllDirectories);
                foreach (var entry in entries)
                {
                    if (!Directory.Exists(entry))
                    {
                        Android.Util.Log.Info("nSCUMM", $"Scan {entry}");
                        var game = _gameDetector.DetectGame(entry);
                        if (game != null)
                        {
                            files.Add(game.Game);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // ignore exception
            }
        }
    }

    public class GameDescriptorAdapter : BaseAdapter<IGameDescriptor>
    {
        IList<IGameDescriptor> _items;
        Activity _context;

        public GameDescriptorAdapter(Activity context, IList<IGameDescriptor> items)
        {
            this._context = context;
            this._items = items;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override IGameDescriptor this[int position]
        {
            get { return _items[position]; }
        }

        public override int Count
        {
            get { return _items.Count; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
                view = _context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = _items[position].Description;
            return view;
        }
    }
}

