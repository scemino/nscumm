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

namespace NScumm.Mobile.Services
{
	public class GameService: IGameService
	{
		public string GetDirectory ()
		{
			string directory;
			directory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
			return directory;
		}

		public void StartGame (string path)
		{
			var intent = new Android.Content.Intent (Xamarin.Forms.Forms.Context, typeof(Droid.ScummActivity));
			intent.PutExtra ("Game", path);
			Xamarin.Forms.Forms.Context.StartActivity (intent);
		}
	}
}
