using System;
using System.Globalization;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    class SwordEngine : IEngine
    {
        // number of frames per second (max rate)
        public const int FRAME_RATE = 12;

        uint _features;
        private ResMan _resMan;
        private Logic _logic;
        private ushort _mouseState;
        private Mixer _mixer;
        private Screen _screen;
        private Sound _sound;
        private Mouse _mouse;
        private Menu _menu;
        private Point _mouseCoord;
        private ScummInputState _keyPressed;
        private Control _control;
        private ObjectMan _objectMan;
        private Music _music;
        public GameSettings Settings { get; }
        public IGraphicsManager GraphicsManager { get; }

        event EventHandler IEngine.ShowMenuDialogRequested
        {
            add { }
            remove { }
        }

        bool IEngine.HasToQuit
        {
            get { return ShouldQuit; }
            set { ShouldQuit = value; }
        }

        public IMixer Mixer => _mixer;
        
        public bool IsPaused { get; set; }

        public ISystem System { get; private set; }

        public static bool ShouldQuit { get; set; }


        public SwordEngine(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager,
            IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode)
        {
            Settings = settings;
            GraphicsManager = gfxManager;
            _mixer = new Mixer(44100);
            // HACK:
            _mixer.Read(new byte[0], 0);
            output.SetSampleProvider(_mixer);
            System = new SwordSystem(gfxManager, inputManager, saveFileManager);

            var gameId = ((SwordGameDescriptor)settings.Game).GameId;
            _features = gameId == SwordGameId.Sword1Demo || gameId == SwordGameId.Sword1MacDemo ||
                        gameId == SwordGameId.Sword1PsxDemo
                ? 1U
                : 0;

            // TODO: debug
            // _console = new SwordConsole(this);

            SystemVars.Platform = settings.Game.Platform;

            // TODO:
            // CheckCdFiles();

            // TODO: debug(5, "Starting resource manager");
            var directory = ServiceLocator.FileStorage.GetDirectoryName(settings.Game.Path);
            var path = ServiceLocator.FileStorage.Combine(directory, "swordres.rif");
            _resMan = new ResMan(directory, path, SystemVars.Platform == Platform.Macintosh);
            // TODO: debug(5, "Starting object manager");

            _objectMan = new ObjectMan(_resMan);
            _mouse = new Mouse(System, _resMan, _objectMan);
            _screen = new Screen(System, _resMan, _objectMan);
            _music = new Music(Mixer);
            _sound = new Sound(settings, _mixer, _resMan);
            _menu = new Menu(_screen, _mouse);
            _logic = new Logic(this, _objectMan, _resMan, _screen, _mouse, _sound, _music, _menu, Mixer);
            _mouse.UseLogicAndMenu(_logic, _menu);

            // TODO:
            //SyncSoundSettings();

            SystemVars.JustRestoredGame = 0;
            SystemVars.CurrentCd = 0;
            SystemVars.ControlPanelMode = ControlPanelMode.CP_NEWGAME;
            SystemVars.ForceRestart = false;
            SystemVars.WantFade = true;
            //_systemVars.realLanguage = Common::parseLanguage(ConfMan.get("language"));
            SystemVars.RealLanguage = new CultureInfo("en-GB");

            //switch (SystemVars.RealLanguage.TwoLetterISOLanguageName)
            //{
            //    case "de":
            //        SystemVars.Language = Language.BS1_GERMAN;
            //        break;
            //    case "fr":
            //        SystemVars.Language = Language.BS1_FRENCH;
            //        break;
            //    case "it":
            //        SystemVars.Language = Language.BS1_ITALIAN;
            //        break;
            //    case "es":
            //        SystemVars.Language = Language.BS1_SPANISH;
            //        break;
            //    case "pt":
            //        SystemVars.Language = Language.BS1_PORT;
            //        break;
            //    case "cz":
            //        SystemVars.Language = Language.BS1_CZECH;
            //        break;
            //    default:
            //        SystemVars.Language = Language.BS1_ENGLISH;
            //        break;
            //}

            // TODO:
            //_systemVars.showText = ConfMan.getBool("subtitles");

            SystemVars.PlaySpeech = 1;
            _mouseState = 0;

            // Some Mac versions use big endian for the speech files but not all of them.
            if (SystemVars.Platform == Platform.Macintosh)
                _sound.CheckSpeechFileEndianness();

            _logic.Initialize();
            _objectMan.Initialize();
            _mouse.Initialize();
            _control = new Control(saveFileManager, _resMan, _objectMan, System, _mouse, _sound, _music);
        }

        public void Run()
        {
            // TODO: run
            //_control.CheckForOldSaveGames();
            //SetTotalPlayTime(0);

            // TODO: configuration
            //uint16 startPos = ConfMan.getInt("boot_param");
            ushort startPos = 0;
            _control.ReadSavegameDescriptions();
            if (startPos != 0)
            {
                _logic.StartPositions(startPos);
            }
            else
            {
                // TODO: configuration
                //int saveSlot = ConfMan.getInt("save_slot");
                int saveSlot = -1;
                // Savegames are numbered starting from 1 in the dialog window,
                // but their filenames are numbered starting from 0.
                if (saveSlot >= 0 && _control.SavegamesExist() && _control.RestoreGameFromFile((byte)saveSlot))
                {
                    _control.DoRestore();
                }
                else if (_control.SavegamesExist())
                {
                    SystemVars.ControlPanelMode = ControlPanelMode.CP_NEWGAME;
                    if (_control.RunPanel() == Control.CONTROL_GAME_RESTORED)
                        _control.DoRestore();
                    else if (!ShouldQuit)
                        _logic.StartPositions(0);
                }
                else
                {
                    // no savegames, start new game.
                    _logic.StartPositions(0);
                }
            }
            SystemVars.ControlPanelMode = ControlPanelMode.CP_NORMAL;

            while (!ShouldQuit)
            {
                byte action = MainLoop();

                if (!ShouldQuit)
                {
                    // the mainloop was left, we have to reinitialize.
                    Reinitialize();
                    if (action == Control.CONTROL_GAME_RESTORED)
                        _control.DoRestore();
                    else if (action == Control.CONTROL_RESTART_GAME)
                        _logic.StartPositions(1);
                    SystemVars.ForceRestart = false;
                    SystemVars.ControlPanelMode = ControlPanelMode.CP_NORMAL;
                }
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


        private void Reinitialize()
        {
            _sound.QuitScreen();
            _resMan.Flush(); // free everything that's currently alloced and opened. (*evil*)

            _logic.Initialize();     // now reinitialize these objects as they (may) have locked
            _objectMan.Initialize(); // resources which have just been wiped.
            _mouse.Initialize();
            // TODO: _system.WwarpMouse(320, 240);
            SystemVars.WantFade = true;
        }

        private byte MainLoop()
        {
            byte retCode = 0;
            System.InputManager.ResetKeys();

            while ((retCode == 0) && !ShouldQuit)
            {
                // do we need the section45-hack from sword.c here?
                CheckCd();

                _screen.NewScreen(Logic.ScriptVars[(int)ScriptVariableNames.NEW_SCREEN]);
                _logic.NewScreen(Logic.ScriptVars[(int)ScriptVariableNames.NEW_SCREEN]);
                _sound.NewScreen(Logic.ScriptVars[(int)ScriptVariableNames.NEW_SCREEN]);
                Logic.ScriptVars[(int)ScriptVariableNames.SCREEN] = Logic.ScriptVars[(int)ScriptVariableNames.NEW_SCREEN];

                do
                {
                    int newTime;
                    bool scrollFrameShown = false;

                    int frameTime = Environment.TickCount;
                    _logic.Engine();
                    _logic.UpdateScreenParams(); // sets scrolling

                    _screen.Draw();
                    _mouse.Animate();
                    _sound.Engine();
                    _menu.Refresh(Menu.MENU_TOP);
                    _menu.Refresh(Menu.MENU_BOT);

                    newTime = Environment.TickCount;
                    if (newTime - frameTime < 1000 / FRAME_RATE)
                    {
                        scrollFrameShown = _screen.ShowScrollFrame();
                        Delay(1000 / (FRAME_RATE * 2) - (Environment.TickCount - frameTime));
                    }

                    newTime = Environment.TickCount;
                    if ((newTime - frameTime < 1000 / FRAME_RATE) || !scrollFrameShown)
                        _screen.UpdateScreen();
                    Delay(1000 / FRAME_RATE - (Environment.TickCount - frameTime));

                    _mouse.Engine((ushort)_mouseCoord.X, (ushort)_mouseCoord.Y, _mouseState);

                    if (SystemVars.ForceRestart)
                        retCode = Control.CONTROL_RESTART_GAME;

                    // The control panel is triggered by F5 or ESC.
                    else if (((_keyPressed.IsKeyDown(KeyCode.F5) || _keyPressed.IsKeyDown(KeyCode.Escape))
                             && (Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] & 1) != 0)
                             || (SystemVars.ControlPanelMode != 0))
                    {
                        retCode = _control.RunPanel();
                        if (retCode == Control.CONTROL_NOTHING_DONE)
                            _screen.FullRefresh();
                    }

                    // TODO: Check for Debugger Activation
                    //if (_keyPressed.hasFlags(Common::KBD_CTRL) && _keyPressed.keycode == Common::KEYCODE_d)
                    //{
                    //    this.getDebugger().attach();
                    //    this.getDebugger().onFrame();
                    //}

                    _mouseState = 0;
                    _keyPressed = new ScummInputState();
                } while ((Logic.ScriptVars[(int)ScriptVariableNames.SCREEN] == Logic.ScriptVars[(int)ScriptVariableNames.NEW_SCREEN]) && (retCode == 0) && !ShouldQuit);

                if ((retCode == 0) && (Logic.ScriptVars[(int)ScriptVariableNames.SCREEN] != 53) && SystemVars.WantFade && !ShouldQuit)
                {
                    _screen.FadeDownPalette();
                    int relDelay = Environment.TickCount;
                    while (_screen.StillFading())
                    {
                        relDelay += 1000 / FRAME_RATE;
                        _screen.UpdateScreen();
                        Delay(relDelay - Environment.TickCount);
                    }
                }

                _sound.QuitScreen();
                _screen.QuitScreen(); // close graphic resources
                _objectMan.CloseSection(Logic.ScriptVars[(int)ScriptVariableNames.SCREEN]); // close the section that PLAYER has just left, if it's empty now
            }
            return retCode;
        }

        private void Delay(int delayInMs)
        {

            int start = Environment.TickCount;

            do
            {
                var inputState = System.InputManager.GetState();
                _mouseCoord = System.InputManager.GetMousePosition();
                _keyPressed = inputState;
                _mouseState |= (ushort)(inputState.IsLeftButtonDown ? Mouse.BS1L_BUTTON_DOWN : Mouse.BS1L_BUTTON_UP);
                _mouseState |= (ushort)(inputState.IsRightButtonDown ? Mouse.BS1R_BUTTON_DOWN : Mouse.BS1R_BUTTON_UP);

                System.GraphicsManager.UpdateScreen();

                if (delayInMs > 0)
                    ServiceLocator.Platform.Sleep(10);

            } while (Environment.TickCount < start + delayInMs);
        }

        private void CheckCd()
        {
            byte needCd = _cdList[Logic.ScriptVars[(int)ScriptVariableNames.NEW_SCREEN]];
            if (SystemVars.RunningFromCd)
            { // are we running from cd?
                if (needCd == 0)
                { // needCd == 0 means we can use either CD1 or CD2.
                    if (SystemVars.CurrentCd == 0)
                    {
                        SystemVars.CurrentCd = 1; // if there is no CD currently inserted, ask for CD1.
                        _control.AskForCd();
                    } // else: there is already a cd inserted and we don't care if it's cd1 or cd2.
                }
                else if (needCd != SystemVars.CurrentCd)
                { // we need a different CD than the one in drive.
                    _music.StartMusic(0, 0); //
                    _sound.CloseCowSystem(); // close music and sound files before changing CDs
                    SystemVars.CurrentCd = needCd; // askForCd will ask the player to insert _systemVars.currentCd,
                    _control.AskForCd();           // so it has to be updated before calling it.
                }
            }
            else
            {        // we're running from HDD, we don't have to care about music files and Sound will take care of
                if (needCd != 0) // switching sound.clu files on Sound::newScreen by itself, so there's nothing to be done.
                    SystemVars.CurrentCd = needCd;
                else if (SystemVars.CurrentCd == 0)
                    SystemVars.CurrentCd = 1;
            }
        }

        static readonly byte[] _cdList = {
            0,		// 0		inventory

	        1,		// 1		PARIS 1
	        1,		// 2
	        1,		// 3
	        1,		// 4
	        1,		// 5
	        1,		// 6
	        1,		// 7
	        1,		// 8

	        1,		// 9		PARIS 2
	        1,		// 10
	        1,		// 11
	        1,		// 12
	        1,		// 13
	        1,		// 14
	        1,		// 15
	        1,		// 16
	        1,		// 17
	        1,		// 18

	        2,		// 19		IRELAND
	        2,		// 20
	        2,		// 21
	        2,		// 22
	        2,		// 23
	        2,		// 24
	        2,		// 25
	        2,		// 26

	        1,		// 27		PARIS 3
	        1,		// 28
	        1,		// 29
	        1,		// 30 - Heart Monitor
	        1,		// 31
	        1,		// 32
	        1,		// 33
	        1,		// 34
	        1,		// 35

	        1,		// 36		PARIS 4
	        1,		// 37
	        1,		// 38
	        1,		// 39
	        1,		// 40
	        1,		// 41
	        1,		// 42
	        1,		// 43
	        0,		// 44	<NOT USED>

	        2,		// 45		SYRIA
	        1,		// 46		PARIS 4
	        2,		// 47
	        1,		// 48		PARIS 4
	        2,		// 49
	        2,		// 50
	        0,		// 51 <NOT USED>
	        0,		// 52 <NOT USED>
	        2,		// 53
	        2,		// 54
	        2,		// 55

	        2,		// 56		SPAIN
	        2,		// 57
	        2,		// 58
	        2,		// 59
	        2,		// 60
	        2,		// 61
	        2,		// 62

	        2,		// 63		NIGHT TRAIN
	        0,		// 64 <NOT USED>
	        2,		// 65
	        2,		// 66
	        2,		// 67
	        0,		// 68 <NOT USED>
	        2,		// 69
	        0,		// 70 <NOT USED>

	        2,		// 71		SCOTLAND
	        2,		// 72
	        2,		// 73
	        2,		// 74		END SEQUENCE IN SECRET_CRYPT
	        2,		// 75
	        2,		// 76
	        2,		// 77
	        2,		// 78
	        2,		// 79

	        1,		// 80		PARIS MAP

	        1,		// 81	Full-screen for "Asstair" in Paris2

	        2,		// 82	Full-screen BRITMAP in sc55 (Syrian Cave)
	        0,		// 83 <NOT USED>
	        0,		// 84 <NOT USED>
	        0,		// 85 <NOT USED>

	        1,		// 86		EUROPE MAP
	        1,		// 87		fudged in for normal window (sc48)
	        1,		// 88		fudged in for filtered window (sc48)
	        0,		// 89 <NOT USED>

	        0,		// 90		PHONE SCREEN
	        0,		// 91		ENVELOPE SCREEN
	        1,		// 92		fudged in for George close-up surprised in sc17 wardrobe
	        1,		// 93		fudged in for George close-up inquisitive in sc17 wardrobe
	        1,		// 94		fudged in for George close-up in sc29 sarcophagus
	        1,		// 95		fudged in for George close-up in sc29 sarcophagus
	        1,		// 96		fudged in for chalice close-up from sc42
	        0,		// 97 <NOT USED>
	        0,		// 98 <NOT USED>
	        0,		// 99		MESSAGE SCREEN (BLANK)

	        0,		// 100
	        0,		// 101
	        0,		// 102
	        0,		// 103
	        0,		// 104
	        0,		// 105
	        0,		// 106
	        0,		// 107
	        0,		// 108
	        0,		// 109

	        0,		// 110
	        0,		// 111
	        0,		// 112
	        0,		// 113
	        0,		// 114
	        0,		// 115
	        0,		// 116
	        0,		// 117
	        0,		// 118
	        0,		// 119

	        0,		// 120
	        0,		// 121
	        0,		// 122
	        0,		// 123
	        0,		// 124
	        0,		// 125
	        0,		// 126
	        0,		// 127
	        0,		// 128  GEORGE'S GAME SECTION
	        0,		// 129	NICO'S TEXT		- on both CD's

	        0,		// 130
	        1,		// 131	BENOIR'S TEXT - on CD1
	        0,		// 132
	        1,		// 133	ROSSO'S TEXT	- on CD1
	        0,		// 134
	        0,		// 135
	        0,		// 136
	        0,		// 137
	        0,		// 138
	        0,		// 139

	        0,		// 140
	        0,		// 141
	        0,		// 142
	        0,		// 143
	        0,		// 144
	        1,		// 145	MOUE'S TEXT		- on CD1
	        1,		// 146	ALBERT'S TEXT	- on CD1
        };        
    }
}