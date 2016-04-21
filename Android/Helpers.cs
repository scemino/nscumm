//
//  Helpers.cs
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

using Android.Content;
using Android.Runtime;
using Android.Views;

namespace NScumm.Mobile.Droid
{
	public static class Helpers
	{
		/// <summary>
		///   Will obtain an instance of a LayoutInflater for the specified Context.
		/// </summary>
		/// <param name="context"> </param>
		/// <returns> </returns>
		public static LayoutInflater GetLayoutInflater(this Context context)
		{
			return context.GetSystemService(Context.LayoutInflaterService).JavaCast<LayoutInflater>();
		}

		/// <summary>
		///   This method will tell us if the given FileSystemInfo instance is a directory.
		/// </summary>
		/// <param name="fsi"> </param>
		/// <returns> </returns>
		public static bool IsDirectory(this FileSystemInfo fsi)
		{
			if (fsi == null || !fsi.Exists)
			{
				return false;
			}

			return (fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
		}

		/// <summary>
		///   This method will tell us if the the given FileSystemInfo instance is a file.
		/// </summary>
		/// <param name="fsi"> </param>
		/// <returns> </returns>
		public static bool IsFile(this FileSystemInfo fsi)
		{
			if (fsi == null || !fsi.Exists)
			{
				return false;
			}
			return !IsDirectory(fsi);
		}

		public static bool IsVisible(this FileSystemInfo fsi)
		{
			if (fsi == null || !fsi.Exists)
			{
				return false;
			}

			var isHidden = (fsi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
			return !isHidden;
		}
	}
}
