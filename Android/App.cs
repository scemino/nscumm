//
//  App.cs
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
using NScumm.Core;
using ReactiveUI;
using System;
using Android.Runtime;
using NScumm.Mobile.ViewModels;

namespace NScumm.Mobile.Droid
{
    [Application(Label = "@string/app_name")]
    public class App : Application
    {
        App(IntPtr handle, JniHandleOwnership owner) : base(handle, owner)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            ServiceLocator.FileStorage = new FileStorage(Assets);

            var suspendHelper = new AutoSuspendHelper(this);

            RxApp.SuspensionHost.CreateNewAppState = () =>
            {
                Console.WriteLine("Creating app state");
                return new AppBootstrapper();
            };

            RxApp.SuspensionHost.SetupDefaultSuspendResume();
        }
    }
}
