using NScumm.Core.Graphics;
using NScumm.Core.Input;
using System;
using NScumm.Core;

namespace NScumm.Sky
{
    [Flags]
    enum SystemFlags
    {
        Timer = 1 << 0,   // set if timer interrupt redirected
        Graphics = 1 << 1,    // set if screen is in graphics mode
        Mouse = 1 << 2,   // set if mouse handler installed
        Keyboard = 1 << 3,    // set if keyboard interrupt redirected
        MusicBoard = 1 << 4, // set if a music board detected
        Roland = 1 << 5,  // set if roland board present
        Adlib = 1 << 6,   // set if adlib board present
        Sblaster = 1 << 7,    // set if sblaster present
        Tandy = 1 << 8,   // set if tandy present
        MusicBin = 1 << 9,   // set if music driver is loaded
        PlusFx = 1 << 10,    // set if extra fx module needed
        FxOff = 1 << 11, // set if fx disabled
        MusOff = 1 << 12,    // set if music disabled
        TimerTick = 1 << 13, // set every timer interupt

        //Status flags
        Choosing = 1 << 14,   // set when choosing text
        NoScroll = 1 << 15,  // when set don't scroll
        Speed = 1 << 16,  // when set allow speed options
        GameRestored = 1 << 17,  // set when game restored or restarted
        ReplayRst = 1 << 18, // set when loading restart data (used to stop rewriting of replay file)
        SpeechFile = 1 << 19,    // set when loading speech file
        VocPlaying = 1 << 20,    // set when a voc file is playing
        PlayVocs = 1 << 21,  // set when we want speech instead of text
        CritErr = 1 << 22,   // set when critical error routine trapped
        AllowSpeech = 1 << 23,   // speech allowes on cd sblaster version
        AllowText = 1 << 24, // text allowed on cd sblaster version
        AllowQuick = 1 << 25,    // when set allow speed playing
        TestDisk = 1 << 26,  // set when loading files
        MouseLocked = 1 << 27	// set if coordinates are locked
    }

    class SystemVars
    {
        public SystemFlags SystemFlags;
        public SkyGameVersion GameVersion;
        public ushort Language;
        public uint CurrentPalette;
        public ushort GameSpeed;
        public ushort CurrentMusic;
        public bool PastIntro;
        public bool Paused;

        private static SystemVars _instance;

        public static SystemVars Instance { get { return _instance ?? (_instance = new SystemVars()); } }
    }

    interface ISystem
    {
        IGraphicsManager GraphicsManager { get; }
        IInputManager InputManager { get; }
        ISaveFileManager SaveFileManager { get; }
    }

    class SkySystem : ISystem
    {
        public IGraphicsManager GraphicsManager { get; private set; }
        public IInputManager InputManager { get; private set; }
        public ISaveFileManager SaveFileManager { get; private set; }

        public SkySystem(IGraphicsManager graphicsManager, IInputManager inputManager, ISaveFileManager saveFileManager)
        {
            GraphicsManager = graphicsManager;
            InputManager = inputManager;
            SaveFileManager = saveFileManager;
        }
    }
}
