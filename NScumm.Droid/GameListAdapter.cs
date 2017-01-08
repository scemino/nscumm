using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using NScumm.Core.IO;

namespace NScumm.Droid
{
	public class GameListAdapter : ArrayAdapter<IGameDescriptor>, IEnumerable<IGameDescriptor>
	{
		Context _context;

		public GameListAdapter(Context context)
			: base(context, Android.Resource.Layout.SimpleListItem2)
		{
			_context = context;
		}

		public IEnumerator<IGameDescriptor> GetEnumerator()
		{
			return Enumerable.Range(0, Count).Select(index => GetItem(index)).ToList().GetEnumerator();
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			var layoutInflater = _context.GetSystemService(Context.LayoutInflaterService).JavaCast<LayoutInflater>();
			var view = convertView ?? layoutInflater.Inflate(
				Android.Resource.Layout.SimpleListItem2, parent, false);
			var game = GetItem(position);
			var title = view.FindViewById<TextView>(Android.Resource.Id.Text1);
			var subTitle = view.FindViewById<TextView>(Android.Resource.Id.Text2);
			title.Text = game.Description;
			subTitle.Text = $"{game.Language} [{game.Platform}]";
			return view;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
