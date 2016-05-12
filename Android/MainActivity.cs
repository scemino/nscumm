//
//  MainActivity.cs
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
using Android.OS;
using ReactiveUI;
using Xamarin.Forms.Platform.Android;
using Android.Content.PM;
using NScumm.Mobile.ViewModels;
using NScumm.Mobile.Resx;

namespace NScumm.Mobile.Droid
{
    [Activity(Label = "@string/app_name", Icon = "@drawable/icon", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AndroidActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Xamarin.Forms.Forms.Init(this, bundle);

            var view = RxApp.SuspensionHost.GetAppState<AppBootstrapper>().CreateMainView();
            SetPage(view);

            // This is a common enough error that we should warn about it
            // explicitly.
            if (!Environment.ExternalStorageDirectory.CanRead())
            {
                new AlertDialog.Builder(this)
                    .SetTitle(AppResources.Error_NoSdcardTitle)
                    .SetIcon(Android.Resource.Drawable.IcDialogAlert)
                    .SetMessage(AppResources.Error_NoSdcard)
                    .SetNegativeButton(AppResources.Quit, (o, e) =>
                    {
                        Finish();
                    }).Show();
            }
        }
    }
}
