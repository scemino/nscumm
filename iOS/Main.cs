//
//  Main.cs
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
#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;

#elif __IOS__ || __TVOS__
using Foundation;
using UIKit;
#endif
#endregion


namespace NScumm.Mobile.iOS
{
	#if __IOS__ || __TVOS__
	[Register("AppDelegate")]
	class Program : UIApplicationDelegate
	
#else
	static class Program
	#endif
    {
		private static Game1 game;

		internal static void RunGame ()
		{
			game = new Game1 ();
			game.Run ();
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		#if !MONOMAC && !__IOS__ &&  !__TVOS__		 
        [STAThread]
		#endif
		static void Main (string[] args)
		{
			#if MONOMAC
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate();
				NSApplication.Main(args);
			}
			#elif __IOS__ || __TVOS__
			UIApplication.Main(args, null, "AppDelegate");
			#else
			RunGame ();
			#endif
		}

		#if __IOS__ || __TVOS__
		public override void FinishedLaunching(UIApplication app)
		{
			RunGame();
		}
		#endif
	}

	#if MONOMAC
	class AppDelegate : NSApplicationDelegate
	{
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs a) =>  {
				if (a.Name.StartsWith("MonoMac")) {
					return typeof(MonoMac.AppKit.AppKitFramework).Assembly;
				}
				return null;
			};
			Program.RunGame();
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}  
	#endif
}

