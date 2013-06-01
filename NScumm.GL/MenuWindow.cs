using System;
using Gtk;

namespace NScumm.GL
{
	public partial class MenuWindow : Gtk.Window
	{
		public event EventHandler<SaveGameEventArgs> LoadRequested;
		public event EventHandler<SaveGameEventArgs> SaveRequested;
		public event EventHandler QuitRequested;

		public MenuWindow () : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			btnLoad.Clicked += OnLoad;
			btnSave.Clicked += OnSave;
			btnQuit.Clicked += OnQuit;
		}

		private void OnQuit (object sender, EventArgs e)
		{
			var eh = QuitRequested;
				if (eh != null) {
					eh (this, EventArgs.Empty);
				}
		}

		private void OnSave (object sender, EventArgs e)
		{
			var dlg = new FileChooserDialog (
				"Save game", this, FileChooserAction.Save,
			    "Cancel", ResponseType.Cancel,
		        "Save", ResponseType.Accept);

			if (dlg.Run () == (int)ResponseType.Accept) {
				var eh = SaveRequested;
				if (eh != null) {
					eh (this, new SaveGameEventArgs (dlg.Filename));
				}
			}
			dlg.Destroy ();
		}

		private void OnLoad (object sender, EventArgs e)
		{
			var dlg = new FileChooserDialog (
				"Open savegame", this, FileChooserAction.Open,
			    "Cancel", ResponseType.Cancel,
		        "Open", ResponseType.Accept);

			if (dlg.Run () == (int)ResponseType.Accept) {
				var eh = LoadRequested;
				if (eh != null) {
					eh (this, new SaveGameEventArgs (dlg.Filename));
				}
			}
			dlg.Destroy ();
		}
	}

	public class SaveGameEventArgs: EventArgs
	{
		public string Filename { get; private set; }

		public SaveGameEventArgs (string filename)
		{
			this.Filename = filename;
		}
	}
}


