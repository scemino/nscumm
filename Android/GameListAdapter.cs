//
//  GameListAdapter.cs
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
using Android.Views;
using Android.Widget;
using NScumm.Core.IO;

namespace NScumm.Mobile.Droid
{
	public class GameListAdapter : BaseAdapter<GameDetected>
	{
		GameDetected[] items;
		Activity context;

		public GameListAdapter (Activity context, GameDetected[] items)
		{
			this.context = context;
			this.items = items;
		}

		public override long GetItemId (int position)
		{
			return position;
		}

		public override GameDetected this [int position] {  
			get { return items [position]; }
		}

		public override int Count {
			get { return items.Length; }
		}

		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			View view = convertView; // re-use an existing view, if one is available
			if (view == null) // otherwise create a new one
				view = context.LayoutInflater.Inflate (Android.Resource.Layout.SimpleListItem1, null);
			view.FindViewById<TextView> (Android.Resource.Id.Text1).Text = items [position].Game.Description;
			return view;
		}
	}
	
}
