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

        public static bool IsDemo
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

        public static bool IsCDVersion
        {
            get { return SystemVars.Instance.GameVersion.Type.HasFlag(SkyGameType.Cd); }
        }

        public static bool ShouldQuit { get; set; }

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

            // TODO: music
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

            if (IsCDVersion)
            {
                // TODO: configuration
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
            _skyMouse = new Mouse(_system, _skyDisk, _skyCompact);
            _skyScreen = new Screen(_system, _skyDisk, _skyCompact);

            InitVirgin();
            InitItemList();
            LoadFixedItems();
            _skyLogic = new Logic(_skyCompact, _skyScreen, _skyDisk, _skyText, _skyMusic, _skyMouse, _skySound);
            _skyMouse.Logic = _skyLogic;
            _skyScreen.Logic = _skyLogic;
            _skySound.Logic = _skyLogic;
            _skyText.Logic = _skyLogic;

            _skyControl = new Control(/*_saveFileMan,*/ _skyScreen, _skyDisk, _skyMouse, _skyText, _skyMusic, _skyLogic, _skySound, _skyCompact, _system);
            _skyLogic.Control = _skyControl;

            // TODO: language

            // TODO: Setup mixer
            //SyncSoundSettings();

            // TODO: debugger
            //_debugger = new Debugger(_skyLogic, _skyMouse, _skyScreen, _skyCompact);
        }

        ~SkyEngine()
        {
            Dispose();
        }

        public static void QuitGame()
        {
            throw new NotImplementedException();
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
            _keyPressed = new ScummInputState();

            ushort result = 0;
            // TODO: configuration
            //if (ConfMan.hasKey("save_slot"))
            //{
            //    var saveSlot = (int)ConfigurationManager["save_slot"];
            //    if (saveSlot >= 0 && saveSlot <= 999)
            //        result = _skyControl.QuickXRestore((int)ConfigurationManager["save_slot"]);
            //}

            if (result != Control.GameRestored)
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

                if (!HasToQuit)
                {
                    // restartGame() takes us to the first scene, without showing the
                    // initial animation where Foster is being chased. initScreen0()
                    // shows the first scene together with that animation. We can't
                    // call both, as they both load the same scene.
                    if (introSkipped)
                        _skyControl.RestartGame();
                    else
                        _skyLogic.InitScreen0();
                }
            }

            _lastSaveTime = Environment.TickCount;

            var delayCount = Environment.TickCount;
            while (!HasToQuit)
            {
                // TODO: _debugger.onFrame();

                // TODO: autosave
                //if (shouldPerformAutoSave(_lastSaveTime))
                //{
                //    if (_skyControl.loadSaveAllowed())
                //    {
                //        _lastSaveTime = _system.getMillis();
                //        _skyControl.doAutoSave();
                //    }
                //    else
                //        _lastSaveTime += 30 * 1000; // try again in 30 secs
                //}
                _skySound.CheckFxQueue();
                _skyMouse.MouseEngine();
                HandleKey();
                if (SystemVars.Instance.Paused)
                {
                    do
                    {
                        _system.GraphicsManager.UpdateScreen();
                        Delay(50);
                        HandleKey();
                    } while (SystemVars.Instance.Paused);
                    delayCount = Environment.TickCount;
                }

                _skyLogic.Engine();
                _skyScreen.ProcessSequence();
                _skyScreen.Recreate();
                _skyScreen.SpriteEngine();

                // TODO: debugger
                //if (_debugger.showGrid())
                //{
                //    uint8* grid = _skyLogic._skyGrid.giveGrid(Logic::_scriptVariables[SCREEN]);
                //    if (grid)
                //    {
                //        _skyScreen.showGrid(grid);
                //        _skyScreen.forceRefresh();
                //    }
                //}
                _skyScreen.Flip();

                if ((_fastMode & 2) != 0)
                    Delay(0);
                else if ((_fastMode & 1) != 0)
                    Delay(10);
                else
                {
                    delayCount += SystemVars.Instance.GameSpeed;
                    int needDelay = delayCount - Environment.TickCount;
                    if ((needDelay < 0) || (needDelay > SystemVars.Instance.GameSpeed))
                    {
                        needDelay = 0;
                        delayCount = Environment.TickCount;
                    }
                    Delay(needDelay);
                }
            }

            _skyControl.ShowGameQuitMsg();
            _skyMusic.StopMusic();
            // TODO: configuration
            //ConfMan.flushToDisk();
            Delay(1500);
        }

        private void Delay(int amount)
        {
            int start = Environment.TickCount;

            if (amount < 0)
                amount = 0;

            do
            {
                _keyPressed = _system.InputManager.GetState();
                var mousePos = _system.InputManager.GetMousePosition();
                if (!SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.MOUSE_LOCKED))
                    _skyMouse.MouseMoved((ushort)mousePos.X, (ushort)mousePos.Y);

                if (_keyPressed.IsLeftButtonDown)
                    _skyMouse.ButtonPressed(2);

                if (_keyPressed.IsRightButtonDown)
                    _skyMouse.ButtonPressed(1);

                _system.GraphicsManager.UpdateScreen();

                if (amount > 0)
                    ServiceLocator.Platform.Sleep(amount > 10 ? 10 : amount);

            } while (Environment.TickCount < start + amount);
        }

        private void HandleKey()
        {
            if (_keyPressed.GetKeys().Count > 0 && SystemVars.Instance.Paused)
            {
                _skySound.FnUnPauseFx();
                SystemVars.Instance.Paused = false;
                _skyScreen.SetPaletteEndian(_skyCompact.FetchCptRaw((ushort)SystemVars.Instance.CurrentPalette));
            }
            else if (_keyPressed.IsKeyDown(KeyCode.LeftControl))
            {
                if (_keyPressed.IsKeyDown(KeyCode.F))
                    _fastMode ^= 1;
                else if (_keyPressed.IsKeyDown(KeyCode.G))
                    _fastMode ^= 2;
                // TODO: debugger
                //else if (_keyPressed.keycode == Common::KEYCODE_d)
                //    _debugger.attach();
            }
            else
            {

                // TODO: debugger
                //case Common::KEYCODE_BACKQUOTE:
                //case Common::KEYCODE_HASH:
                //    _debugger.attach();
                //    break;
                if (_keyPressed.IsKeyDown(KeyCode.F5))
                {
                    _skyControl.DoControlPanel();
                }

                if (_keyPressed.IsKeyDown(KeyCode.Escape))
                {
                    if (!SystemVars.Instance.PastIntro)
                        _skyControl.RestartGame();
                }
                if (_keyPressed.IsKeyDown(KeyCode.OemPeriod))
                {
                    _skyMouse.LogicClick();
                }
                if (_keyPressed.IsKeyDown(KeyCode.P))
                {
                    _skyScreen.HalvePalette();
                    _skySound.FnPauseFx();
                    SystemVars.Instance.Paused = true;
                }
            }
            _keyPressed = new ScummInputState();
            _system.InputManager.ResetKeys();
        }

        public void Load(string filename)
        {
            // TODO: remove this
            throw new NotImplementedException("Load game not implemented");
        }

        public void Save(string filename)
        {
            // TODO: remove this
            throw new NotImplementedException("Save game not implemented");
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
        private readonly SkyCompact _skyCompact;
        private readonly Screen _skyScreen;
        private readonly SkySystem _system;
        private readonly Text _skyText;
        private MusicBase _skyMusic;
        private readonly Mixer _mixer;
        private readonly Sound _skySound;
        private readonly Control _skyControl;
        private readonly Logic _skyLogic;

        static byte[][] _itemList = new byte[300][];
        private readonly Mouse _skyMouse;
        private int _lastSaveTime;
        private ScummInputState _keyPressed;
        private int _fastMode;
    }
}
