//
//  GameLibraryPage.xaml.cs
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

using Xamarin.Forms;
using ReactiveUI;
using NScumm.Mobile.ViewModels;
using System;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using NScumm.Mobile.Resx;

namespace NScumm.Mobile.Views
{
	public partial class GameLibraryView : ContentPage, IViewFor<GameLibraryViewModel>
	{
		public GameLibraryView ()
		{
			InitializeComponent ();

			this.OneWayBind (ViewModel, vm => vm.Games, v => v.FileLisView.ItemsSource);
			this.WhenAnyObservable (v => v.ViewModel.Scan.IsExecuting)
				.BindTo (this, v => v.FileLisView.IsRefreshing);

			this.BindCommand (ViewModel, vm => vm.Scan, v => v.ScanToolbarItem);
			this.BindCommand (ViewModel, vm => vm.Scan, v => v.FileLisView.RefreshCommand);

			FileLisView.ItemTapped += OnItemTapped;
		}

		private void OnDelete (object sender, EventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var game = (GameViewModel)menuItem.CommandParameter;
            string msg = string.Format(AppResources.RemoveGame_Message, game.Description);
            DisplayAlert (AppResources.RemoveGame_Title, msg, AppResources.DialogBox_OK, AppResources.DialogBox_Cancel)
				.ToObservable ()
				.Where (o => o)
				.Subscribe (result => {
					ViewModel.Delete.Execute (menuItem.CommandParameter);
			});
		}

		private void OnItemTapped (object sender, ItemTappedEventArgs e)
		{
			ViewModel.LaunchGame.Execute (e.Item);
		}

		public GameLibraryViewModel ViewModel {
			get { return (GameLibraryViewModel)GetValue (ViewModelProperty); }
			set { SetValue (ViewModelProperty, value); }
		}

		public static readonly BindableProperty ViewModelProperty =
			BindableProperty.Create<GameLibraryView, GameLibraryViewModel> (x => x.ViewModel, default(GameLibraryViewModel));

		object IViewFor.ViewModel {
			get { return ViewModel; }
			set { ViewModel = (GameLibraryViewModel)value; }
		}
	}
}

