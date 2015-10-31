using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using NScumm.Sky.Music;
using System;

namespace NScumm.Sky
{
    class SkyEngine : IEngine, IDisposable
    {
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

        private bool IsDemo
        {
            get
            {
                return SystemVars.Instance.GameVersion.Type.HasFlag(SkyGameType.Demo);
            }
        }

        public static byte[][] ItemList
        {
            get { return _itemList; }
            private set { _itemList = value; }
        }

        public event EventHandler ShowMenuDialogRequested;


        public SkyEngine(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, bool debugMode = false)
        {
            _system = new SkySystem(gfxManager, inputManager);
            _mixer = new Mixer(44100);
            _mixer.Read(new byte[0], 0);
            output.SetSampleProvider(_mixer);

            var directory = ServiceLocator.FileStorage.GetDirectoryName(settings.Game.Path);
            _skyDisk = new Disk(directory);
            _skySound = new Sound(_mixer, _skyDisk, Mixer.MaxChannelVolume);

            SystemVars.Instance.GameVersion = _skyDisk.DetermineGameVersion();

            //MidiDriver::DeviceHandle dev = MidiDriver::detectDevice(MDT_ADLIB | MDT_MIDI | MDT_PREFER_MT32);
            //if (MidiDriver::getMusicType(dev) == MT_ADLIB)
            //{
            //    _systemVars.systemFlags |= SF_SBLASTER;
                _skyMusic = new AdLibMusic(_mixer, _skyDisk);
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
            _skyText = new Text(_skyDisk, _skyCompact);
            //_skyMouse = new Mouse(_system, _skyDisk, _skyCompact);
            _skyScreen = new Screen(_system, _skyDisk, _skyCompact);

            InitVirgin();
            InitItemList();
            LoadFixedItems();
        }

        ~SkyEngine()
        {
            Dispose();
        }


        public void Dispose()
        {
            if (_skyMusic != null)
            {
                _skyMusic.Dispose();
                _skyMusic = null;
            }
            if (_skyDisk != null)
            {
                _skyDisk.Dispose();
                _skyDisk = null;
            }
        }

        public void Run()
        {
            //_keyPressed.Reset();

            //ushort result = 0;
            //if (ConfMan.hasKey("save_slot"))
            //{
            //    var saveSlot = (int)ConfigurationManager["save_slot"];
            //    if (saveSlot >= 0 && saveSlot <= 999)
            //        result = _skyControl.QuickXRestore((int)ConfigurationManager["save_slot"]);
            //}

            //if (result != GAME_RESTORED)
            {
                bool introSkipped = false;
                if (SystemVars.Instance.GameVersion.Version.Minor > 272)
                {
                    // don't do intro for floppydemos
                    using (var skyIntro = new Intro(_skyDisk, _skyScreen, _skyMusic, _skySound, _skyText, _mixer, _system))
                    {
                        //var floppyIntro = (bool)ConfigurationManager["alt_intro"];
                        var floppyIntro = false;
                        introSkipped = !skyIntro.DoIntro(floppyIntro);
                    }
                }
            }
            while (!HasToQuit)
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

        private void InitItemList()
        {
            //See List.asm for (cryptic) item # descriptions

            for (int i = 0; i < 300; i++)
                ItemList[i] = null;
        }

        private void LoadFixedItems()
        {
            ItemList[49] = _skyDisk.LoadFile(49);
            ItemList[50] = _skyDisk.LoadFile(50);
            ItemList[73] = _skyDisk.LoadFile(73);
            ItemList[262] = _skyDisk.LoadFile(262);

            if (!IsDemo)
            {
                ItemList[36] = _skyDisk.LoadFile(36);
                ItemList[263] = _skyDisk.LoadFile(263);
                ItemList[264] = _skyDisk.LoadFile(264);
                ItemList[265] = _skyDisk.LoadFile(265);
                ItemList[266] = _skyDisk.LoadFile(266);
                ItemList[267] = _skyDisk.LoadFile(267);
                ItemList[269] = _skyDisk.LoadFile(269);
                ItemList[271] = _skyDisk.LoadFile(271);
                ItemList[272] = _skyDisk.LoadFile(272);
            }
        }


        private Disk _skyDisk;
        private SkyCompact _skyCompact;
        private Screen _skyScreen;
        private SkySystem _system;
        private Text _skyText;
        private MusicBase _skyMusic;
        private Mixer _mixer;
        private Sound _skySound;

        static byte[][] _itemList = new byte[300][];
    }
}
