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