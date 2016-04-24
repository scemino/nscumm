//
//  AppDelegate.cs
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

using Foundation;
using UIKit;
using ReactiveUI;
using Xamarin.Forms;
using NScumm.Mobile.ViewModels;

namespace NScumm.Mobile.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		UIWindow window;
		AutoSuspendHelper suspendHelper;

		public AppDelegate()
		{
			RxApp.SuspensionHost.CreateNewAppState = () => new AppBootstrapper();
		}

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			Forms.Init ();
			RxApp.SuspensionHost.SetupDefaultSuspendResume();

			suspendHelper = new AutoSuspendHelper(this);
			suspendHelper.FinishedLaunching(app, options);

			window = new UIWindow (UIScreen.MainScreen.Bounds);
			var vc = RxApp.SuspensionHost.GetAppState<AppBootstrapper>().CreateMainView().CreateViewController();

			window.RootViewController = vc;
			window.MakeKeyAndVisible ();

			return true;
		}

		public override void DidEnterBackground(UIApplication application)
		{
			suspendHelper.DidEnterBackground(application);
		}

		public override void OnActivated(UIApplication application)
		{
			suspendHelper.OnActivated(application);
		}
	}
}

