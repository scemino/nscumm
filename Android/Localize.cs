using System;
using NScumm.Mobile.Resx;
using Xamarin.Forms;

[assembly: Dependency(typeof(NScumm.Mobile.Droid.Localize))]

namespace NScumm.Mobile.Droid
{
    public class Localize : ILocalize
    {
        public System.Globalization.CultureInfo GetCurrentCultureInfo()
        {
            var androidLocale = Java.Util.Locale.Default;
            var netLanguage = androidLocale.ToString().Replace("_", "-"); // turns pt_BR into pt-BR
            return new System.Globalization.CultureInfo(netLanguage);
        }
    }
}

