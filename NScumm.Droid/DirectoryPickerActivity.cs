using System;
using System.Linq;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace NScumm.Droid
{
	[Activity(Label = "DirectoryPickerActivity")]
	public class DirectoryPickerActivity : Activity
	{
		string[] _files;
		ListView _listView1;
		TextView _txtDirectory;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.directoryPicker);

			var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
			SetActionBar(toolbar);

			_files = new Java.IO.File("/storage/emulated", "0")
							 .ListFiles()
							 .Where(o => o.IsDirectory)
							 .Select(o => o.AbsolutePath)
							 .ToArray();

			_listView1 = FindViewById<ListView>(Resource.Id.listView1);
			_listView1.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, _files);
			_listView1.ItemClick += OnListItemClick;

			_txtDirectory = FindViewById<TextView>(Resource.Id.txtDirectory);
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			MenuInflater.Inflate(Resource.Menu.directoryPickerMenu, menu);
			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			if (item.ItemId == Resource.Id.menu_ok)
			{
				OnOk();
			}
			return base.OnOptionsItemSelected(item);
		}

		private void OnOk()
		{
			var intent = new Intent();
			intent.PutExtra("Path", _txtDirectory.Text);
			SetResult(Result.Ok,intent);
			Finish();
		}

		private void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			var file = _files[e.Position];
			_txtDirectory.Text = file;
		}
	}
}
