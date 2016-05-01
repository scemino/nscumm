//
//  AppBootstrapper.cs
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

using ReactiveUI;
using Splat;
using Xamarin.Forms;
using NScumm.Mobile.Views;

namespace NScumm.Mobile.ViewModels
{
	public class AppBootstrapper : ReactiveObject, IScreen
	{
		public RoutingState Router { get; protected set; }

		public AppBootstrapper ()
		{
			Router = new RoutingState ();

			Locator.CurrentMutable.RegisterConstant (this, typeof(IScreen));
			Locator.CurrentMutable.Register (() => new GameLibraryView (), typeof(IViewFor<GameLibraryViewModel>));

			Router.Navigate.Execute (new GameLibraryViewModel (this));
		}

		public Page CreateMainView ()
		{
			return new ReactiveUI.XamForms.RoutedViewHost ();
		}
	}
}

