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
using System.IO;
using NScumm.Core;
using NScumm.Core.IO;
using NScumm.Mobile.Services;
//using NScumm.Queen;
//using NScumm.Scumm.IO;
//using NScumm.Sky;
//using NScumm.Sword1;

namespace NScumm.Mobile.Services
{
    public class GameService : IGameService
    {
        public string GetDirectory()
        {
#if __ANDROID__
            string directory;
            directory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            return directory;
#elif __IOS__
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return directory;
#endif
        }

#if __ANDROID__
        public void StartGame(string path)
        {
            //var intent = new Android.Content.Intent(Xamarin.Forms.Forms.Context, typeof(ScummActivity));
            //intent.PutExtra("Game", path);
            //Xamarin.Forms.Forms.Context.StartActivity(intent);
        }
#elif __IOS__
        public void StartGame(string path)
        {
            Initialize();

            var gd = new GameDetector();
            gd.Add(new SkyMetaEngine());
            gd.Add(new ScummMetaEngine());
            gd.Add(new Sword1MetaEngine());
            gd.Add(new QueenMetaEngine());

            try
            {
                var info = gd.DetectGame(path);
                if (info == null)
                {
                    //Toast.MakeText (this, Resources.GetText (Resource.String.game_not_supported), ToastLength.Short).Show ();
                    return;
                }

            ((AudioManager)ServiceLocator.AudioManager).Directory = Path.GetDirectoryName(info.Game.Path);
                var settings = new GameSettings(info.Game, info.Engine)
                {
                    AudioDevice = "adlib",
                    CopyProtection = false
                };

                // Create our OpenGL view, and display it
                var game = new ScummGame(settings);
                game.Services.AddService<IMenuService>(new MenuService(game));
                game.Run();
            }
            catch (Exception e)
            {
                int tmp = 42;
            }
        }

        private static void Initialize()
        {
            ServiceLocator.Platform = new Platform();
            ServiceLocator.FileStorage = new FileStorage();
            ServiceLocator.SaveFileManager = new SaveFileManager();
            ServiceLocator.AudioManager = new AudioManager();
            ServiceLocator.TraceFatory = new TraceFactory();
        }
#endif
    }
}

