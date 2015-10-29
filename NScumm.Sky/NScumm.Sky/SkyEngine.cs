using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using System;

namespace NScumm.Sky
{
    [Flags]
    enum SystemFlags
    {
        TIMER = (1 << 0),   // set if timer interrupt redirected
        GRAPHICS = (1 << 1),    // set if screen is in graphics mode
        MOUSE = (1 << 2),   // set if mouse handler installed
        KEYBOARD = (1 << 3),    // set if keyboard interrupt redirected
        MUSIC_BOARD = (1 << 4), // set if a music board detected
        ROLAND = (1 << 5),  // set if roland board present
        ADLIB = (1 << 6),   // set if adlib board present
        SBLASTER = (1 << 7),    // set if sblaster present
        TANDY = (1 << 8),   // set if tandy present
        MUSIC_BIN = (1 << 9),   // set if music driver is loaded
        PLUS_FX = (1 << 10),    // set if extra fx module needed
        FX_OFF = (1 << 11), // set if fx disabled
        MUS_OFF = (1 << 12),    // set if music disabled
        TIMER_TICK = (1 << 13), // set every timer interupt

        //Status flags
        CHOOSING = (1 << 14),   // set when choosing text
        NO_SCROLL = (1 << 15),  // when set don't scroll
        SPEED = (1 << 16),  // when set allow speed options
        GAME_RESTORED = (1 << 17),  // set when game restored or restarted
        REPLAY_RST = (1 << 18), // set when loading restart data (used to stop rewriting of replay file)
        SPEECH_FILE = (1 << 19),    // set when loading speech file
        VOC_PLAYING = (1 << 20),    // set when a voc file is playing
        PlayVocs = (1 << 21),  // set when we want speech instead of text
        CRIT_ERR = (1 << 22),   // set when critical error routine trapped
        ALLOW_SPEECH = (1 << 23),   // speech allowes on cd sblaster version
        AllowText = (1 << 24), // text allowed on cd sblaster version
        ALLOW_QUICK = (1 << 25),    // when set allow speed playing
        TEST_DISK = (1 << 26),  // set when loading files
        MOUSE_LOCKED = (1 << 27)	// set if coordinates are locked
    }

    class SystemVars
    {
        public SystemFlags SystemFlags;
        public SkyGameVersion GameVersion;
        public uint MouseFlag;
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
    }

    class SkySystem : ISystem
    {
        public IGraphicsManager GraphicsManager { get; private set; }
        public IInputManager InputManager { get; private set; }

        public SkySystem(IGraphicsManager graphicsManager, IInputManager inputManager)
        {
            GraphicsManager = graphicsManager;
            InputManager = inputManager;
        }
    }

    class SkyEngine : IEngine
    {
        private Disk _skyDisk;
        private SkyCompact _skyCompact;
        private Screen _skyScreen;
        private SkySystem _system;

        public bool HasToQuit
        {
            get;
            set;
        }

        public bool IsPaused
        {
            get;
            set;
        }

        public event EventHandler ShowMenuDialogRequested;

        public SkyEngine(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, bool debugMode = false)
        {
            _system = new SkySystem(gfxManager, inputManager);
            var mixer = new Mixer(44100);
            output.SetSampleProvider(mixer);

            var directory = ServiceLocator.FileStorage.GetDirectoryName(settings.Game.Path);
            _skyDisk = new Disk(directory);
            //_skySound = new Sound(_mixer, _skyDisk, Mixer.MaxChannelVolume);

            SystemVars.Instance.GameVersion = _skyDisk.DetermineGameVersion();

            //MidiDriver::DeviceHandle dev = MidiDriver::detectDevice(MDT_ADLIB | MDT_MIDI | MDT_PREFER_MT32);
            //if (MidiDriver::getMusicType(dev) == MT_ADLIB)
            //{
            //    _systemVars.systemFlags |= SF_SBLASTER;
            //    _skyMusic = new AdLibMusic(_mixer, _skyDisk);
            //}
            //else
            //{
            //    _systemVars.systemFlags |= SF_ROLAND;
            //    if ((MidiDriver::getMusicType(dev) == MT_MT32) || ConfMan.getBool("native_mt32"))
            //        _skyMusic = new MT32Music(MidiDriver::createMidi(dev), _mixer, _skyDisk);
            //    else
            //        _skyMusic = new GmMusic(MidiDriver::createMidi(dev), _mixer, _skyDisk);
            //}

            if (SystemVars.Instance.GameVersion.Type.HasFlag(SkyGameType.Cd))
            {
                //if (ConfMan.hasKey("nosubtitles"))
                //{
                //    warning("Configuration key 'nosubtitles' is deprecated. Use 'subtitles' instead");
                //    if (!ConfMan.getBool("nosubtitles"))
                //        _systemVars.systemFlags |= SF_ALLOW_TEXT;
                //}

                //if (ConfMan.getBool("subtitles"))
                //    _systemVars.systemFlags |= SF_ALLOW_TEXT;

                //if (!ConfMan.getBool("speech_mute"))
                //    _systemVars.systemFlags |= SF_ALLOW_SPEECH;

            }
            else
                SystemVars.Instance.SystemFlags |= SystemFlags.AllowText;

            SystemVars.Instance.SystemFlags |= SystemFlags.PlayVocs;
            SystemVars.Instance.GameSpeed = 80;

            _skyCompact = new SkyCompact();
            //_skyText = new Text(_skyDisk, _skyCompact);
            //_skyMouse = new Mouse(_system, _skyDisk, _skyCompact);
            _skyScreen = new Screen(_system, _skyDisk, _skyCompact);

            InitVirgin();
        }

        public void Run()
        {
            while (true)
            {
                // TODO:
            }
        }

        public void Load(string filename)
        {
            throw new NotImplementedException();
        }

        public void Save(string filename)
        {
            throw new NotImplementedException();
        }

        private void InitVirgin()
        {
            _skyScreen.SetPalette(60111);
            _skyScreen.ShowScreen(60110);
        }
    }
}
