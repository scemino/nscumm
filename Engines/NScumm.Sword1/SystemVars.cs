//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

using System.Globalization;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    enum ControlPanelMode
    {
        CP_NORMAL = 0,
        CP_DEATHSCREEN,
        CP_THEEND,
        CP_NEWGAME
    }

    enum Language
    {
        BS1_ENGLISH = 0,
        BS1_FRENCH,
        BS1_GERMAN,
        BS1_ITALIAN,
        BS1_SPANISH,
        BS1_CZECH,
        BS1_PORT
    }

    static class SystemVars
    {
        public static bool RunningFromCd;
        public static uint CurrentCd;          // starts at zero, then either 1 or 2 depending on section being played
        public static uint JustRestoredGame;   // see main() in sword.c & New_screen() in gtm_core.c

        public static ControlPanelMode ControlPanelMode;   // 1 death screen version of the control panel, 2 = successful end of game, 3 = force restart
        public static bool ForceRestart;
        public static bool WantFade;           // when true => fade during scene change, else cut.
        public static byte PlaySpeech;
        public static byte ShowText;
        public static Language Language;
        public static bool IsDemo;
        public static Platform Platform;
        public static CultureInfo RealLanguage;
    }
}