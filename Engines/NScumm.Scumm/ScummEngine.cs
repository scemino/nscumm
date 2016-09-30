//
//  ScummEngine.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.SoftSynth;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.Audio;
using NScumm.Scumm.Audio.IMuse;
using NScumm.Scumm.Audio.Players;
using NScumm.Scumm.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    public enum OpCodeParameter : byte
    {
        Param1 = 0x80,
        Param2 = 0x40,
        Param3 = 0x20,
    }

    public abstract partial class ScummEngine : Engine, IEnableTrace
    {
        #region Constants

        protected const int StringIdIqEpisode = 7;
        protected const int StringIdIqSeries = 9;
        protected const int StringIdSavename1 = 10;

        public const uint CurrentVersion = 94;

        protected int OwnerRoom { get; private set; }

        protected const int NumScriptSlot = 80;

        protected const int MaxScriptNesting = 15;
        protected const int MaxCutsceneNum = 5;

        #endregion

        #region Fields

        protected ResourceManager _resManager;
        protected byte _roomResource;
        protected byte[] _resourceMapper = new byte[128];


        protected Dictionary<byte, Action> _opCodes;
        protected byte _opCode;

        ICostumeLoader _costumeLoader;
        ICostumeRenderer _costumeRenderer;
        protected bool _keepText;
        protected int _talkDelay;
        protected int _defaultTalkDelay = 3;
        protected int _haveMsg;

        public int ScreenTop { get; private set; }

        public int ScreenWidth { get; private set; }

        public int ScreenHeight { get; private set; }

        public bool DebugMode { get; private set; }

        int _screenB;
        int _screenH;

        GameInfo _game;
        byte[] _gameMD5;
        int deltaTicks;
        TimeSpan timeToWait;

        protected TownsScreen _townsScreen;

        public byte TownsOverrideShadowColor { get; protected set; }

        protected byte[] _textPalette = new byte[48];
        protected byte _townsClearLayerFlag = 1;
        protected byte _townsActiveLayerFlags = 3;

        #endregion

        #region Properties

        internal ResourceManager ResourceManager { get { return _resManager; } }

        internal VirtScreen MainVirtScreen
        {
            get { return _mainVirtScreen; }
        }

        public GameInfo Game
        {
            get { return _game; }
        }

        public byte InvalidBox { get; private set; }

        internal int ScreenStartStrip
        {
            get { return _screenStartStrip; }
        }

        internal bool EgoPositioned
        {
            get;
            set;
        }

        internal ICostumeLoader CostumeLoader
        {
            get { return _costumeLoader; }
        }

        internal ICostumeRenderer CostumeRenderer { get { return _costumeRenderer; } }

        public int TalkDelay { get { return _talkDelay; } }

        internal IIMuse IMuse { get; private set; }

        public IMusicEngine MusicEngine { get; protected set; }

        internal Player_Towns TownsPlayer { get; private set; }

        public GameSettings Settings { get; private set; }

        public IAudioCDManager AudioCDManager
        {
            get;
            private set;
        }
        #endregion

        #region Constructor

        public static ScummEngine Create(GameSettings settings, ISystem system)
        {
            ScummEngine engine = null;
            var game = (GameInfo)settings.Game;
            var mixer = new Mixer(44100);
            system.AudioOutput.SetSampleProvider(mixer);

            if (game.Version == 0)
            {
                engine = new ScummEngine0(settings, system, mixer);
            }
            else if ((game.Version == 1) || (game.Version == 2))
            {
                engine = new ScummEngine2(settings, system, mixer);
            }
            else if (game.Version == 3)
            {
                engine = new ScummEngine3(settings, system, mixer);
            }
            else if (game.Version == 4)
            {
                engine = new ScummEngine4(settings, system, mixer);
            }
            else if (game.Version == 5)
            {
                engine = new ScummEngine5(settings, system, mixer);
            }
            else if (game.Version == 6)
            {
                engine = new ScummEngine6(settings, system, mixer);
            }
            else if (game.Version == 7)
            {
                engine = new ScummEngine7(settings, system, mixer);
            }
            else if (game.Version == 8)
            {
                engine = new ScummEngine8(settings, system, mixer);
            }
            Instance = engine;
            //engine.DebugMode = debugMode;
            engine.InitOpCodes();
            engine.SetupVars();
            engine.ResetScummVars();
            return engine;
        }

        byte[] ToMd5Bytes(string md5)
        {
            byte[] sig = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                var b = int.Parse(md5.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                sig[i] = (byte)b;
            }
            return sig;
        }

        protected ScummEngine(GameSettings settings, ISystem system, IMixer mixer)
            : base(system, settings)
        {
            Settings = settings;
            var game = (GameInfo)settings.Game;
            _resManager = ResourceManager.Load(game);

            _game = game;
            InvalidBox = _game.Version < 5 ? (byte)255 : (byte)0;
            _gameMD5 = ToMd5Bytes(game.MD5);
            _gfxManager = system.GraphicsManager;
            _inputManager = system.InputManager;
            _inputState = system.InputManager.GetState();
            _strings = new byte[_resManager.NumArray][];
            _inventory = new ushort[_resManager.NumInventory];
            _invData = new ObjectData[_resManager.NumInventory];
            CurrentScript = 0xFF;
            Mixer = mixer;
            ScreenWidth = Game.Width;
            ScreenHeight = Game.Height;

            AudioCDManager = new DefaultAudioCDManager(this, mixer);
            Sound = new Sound(this, mixer);

            SetupMusic();

            _variables = new int[_resManager.NumVariables];
            _bitVars = new BitArray(_resManager.NumBitVariables);
            _slots = new ScriptSlot[NumScriptSlot];
            for (int i = 0; i < NumScriptSlot; i++)
            {
                _slots[i] = new ScriptSlot();
            }
            for (int i = 0; i < 200; i++)
            {
                _objs[i] = new ObjectData();
            }
            for (int i = 0; i < 6; i++)
            {
                _string[i] = new TextSlot();
                if (game.Version != 3)
                {
                    _string[i].Default.Position = new Point(2, 5);
                }
            }
            _colorCycle = new ColorCycle[16];
            for (int i = 0; i < _colorCycle.Length; i++)
            {
                _colorCycle[i] = new ColorCycle();
            }
            _nest = new NestedScript[MaxScriptNesting + 1];
            for (int i = 0; i < _nest.Length; i++)
            {
                _nest[i] = new NestedScript();
            }
            _scaleSlots = new ScaleSlot[20];
            for (int i = 0; i < _scaleSlots.Length; i++)
            {
                _scaleSlots[i] = new ScaleSlot();
            }

            Gdi = Gdi.Create(this, game);
            switch (game.Version)
            {
                case 0:
                    _costumeLoader = new CostumeLoader0(this);
                    _costumeRenderer = new CostumeRenderer0(this);
                    break;
                case 7:
                case 8:
                    _costumeLoader = new AkosCostumeLoader(this);
                    _costumeRenderer = new AkosRenderer(this);
                    break;
                default:
                    _costumeLoader = new ClassicCostumeLoader(this);
                    _costumeRenderer = new ClassicCostumeRenderer(this);
                    break;
            }

            CreateCharset();
            ResetCursors();

            // Create the text surface
            var pixelFormat = _game.Features.HasFlag(GameFeatures.Is16BitColor) ? PixelFormat.Rgb16 : PixelFormat.Indexed8;
            _textSurface = new Surface((ushort) (ScreenWidth * _textSurfaceMultiplier), (ushort) (ScreenHeight * _textSurfaceMultiplier), PixelFormat.Indexed8, false);
            ClearTextSurface();

            if (Game.Platform == Platform.FMTowns)
            {
                _townsScreen = new TownsScreen(_gfxManager, ScreenWidth * _textSurfaceMultiplier, ScreenHeight * _textSurfaceMultiplier, PixelFormat.Rgb16);
                _townsScreen.SetupLayer(0, ScreenWidth, ScreenHeight, 32767);
                _townsScreen.SetupLayer(1, ScreenWidth * _textSurfaceMultiplier, ScreenHeight * _textSurfaceMultiplier, 16, _textPalette);
            }

            if (Game.Version == 0)
            {
                InitScreens(8, 144);
            }
            else if ((Game.GameId == GameId.Maniac) && (_game.Version <= 1) && _game.Platform != Platform.NES)
            {
                InitScreens(16, 152);
            }
            else if (Game.Version >= 7)
            {
                InitScreens(0, ScreenHeight);
            }
            else
            {
                InitScreens(16, 144);
            }
            // Allocate gfx compositing buffer (not needed for V7/V8 games).
            if (Game.Version < 7)
            {
                _composite = new Surface((ushort) ScreenWidth, (ushort) ScreenHeight, pixelFormat, false);
            }
            InitActors();
            OwnerRoom = Game.Version >= 7 ? 0x0FF : 0x0F;

            if (Game.Version < 7)
            {
                Camera.LeftTrigger = 10;
                Camera.RightTrigger = 30;
            }

            InitPalettes();
            InitializeVerbs();

            // WORKAROUND for bug in boot script of Loom (CD)
            // The boot script sets the characters of string 21,
            // before creating the string.resource.
            if (_game.GameId == GameId.Loom)
            {
                _strings[21] = new byte[13];
            }
        }

        void SetupMusic()
        {
            var selectedDevice = Settings.AudioDevice;
            var deviceHandle = MidiDriver.DetectDevice(Game.Music, selectedDevice);

            switch (MidiDriver.GetMusicType(deviceHandle))
            {
                case MusicType.Null:
                    Sound.MusicType = MusicDriverTypes.None;
                    break;
                case MusicType.PCSpeaker:
                    Sound.MusicType = MusicDriverTypes.PCSpeaker;
                    break;
                case MusicType.PCjr:
                    Sound.MusicType = MusicDriverTypes.PCjr;
                    break;
                case MusicType.CMS:
                    Sound.MusicType = MusicDriverTypes.CMS;
                    break;
                case MusicType.FMTowns:
                    Sound.MusicType = MusicDriverTypes.FMTowns;
                    break;
                case MusicType.AdLib:
                    Sound.MusicType = MusicDriverTypes.AdLib;
                    break;
                case MusicType.C64:
                    Sound.MusicType = MusicDriverTypes.C64;
                    break;
                case MusicType.AppleIIGS:
                    Sound.MusicType = MusicDriverTypes.AppleIIGS;
                    break;
                default:
                    Sound.MusicType = MusicDriverTypes.Midi;
                    break;
            }

            // Init iMuse
            if (Game.Version >= 7)
            {
                // Setup for digital iMuse is performed in another place

                // HACK: don't know why I have to keep this to work
                var adlibMidiDriver = (MidiDriver)MidiDriver.CreateMidi(Mixer, MidiDriver.DetectDevice(MusicDriverTypes.AdLib, "adlib"));
                // Setup for digital iMuse is performed in another place
                Audio.IMuse.IMuse.Create(null, adlibMidiDriver);
            }
            else if (Game.Platform == Platform.Apple2GS && Game.Version == 0)
            {
                MusicEngine = new Player_AppleII(this, Mixer);
            }
            else if (Game.Platform == Platform.C64 && Game.Version <= 1)
            {
                var sid = new SID();
                MusicEngine = new Player_SID(this, Mixer, sid);
            }
            else if (_game.Platform == Platform.Amiga && Game.Version == 2)
            {
                var modPlayer = new Player_MOD(Mixer);
                MusicEngine = new Player_V2A(this, modPlayer);
            }
            else if (Game.Platform == Platform.Amiga && Game.Version == 3)
            {
                var modPlayer = new Player_MOD(Mixer);
                MusicEngine = new Player_V3A(this, modPlayer);
            }
            else if (Game.Platform == Platform.Amiga && Game.Version <= 4)
            {
                MusicEngine = new Player_V4A(this, Mixer);
            }
            else if (Game.Platform == Platform.Macintosh && Game.GameId == GameId.Loom)
            {
                MusicEngine = new Player_V3M(this, Mixer);
            }
            else if (Game.Platform == Platform.Macintosh && Game.GameId == GameId.Monkey1)
            {
                MusicEngine = new Player_V5M(this, Mixer);
            }
            else if (Game.GameId == GameId.Maniac && Game.Version == 1)
            {
                MusicEngine = new Player_V1(this, Mixer, Sound.MusicType == MusicDriverTypes.PCjr);
            }
            else if ((Sound.MusicType == MusicDriverTypes.PCSpeaker || Sound.MusicType == MusicDriverTypes.PCjr) && (Game.Version >= 2 && Game.Version <= 4))
            {
                MusicEngine = new Player_V2(this, Mixer, Sound.MusicType == MusicDriverTypes.PCjr);
            }
            else if (Sound.MusicType == MusicDriverTypes.CMS)
            {
                MusicEngine = new Player_V2CMS(this, Mixer);
            }
            else if (Game.Platform == Platform.FMTowns && (Game.Version == 3 || Game.GameId == GameId.Monkey1))
            {
                MusicEngine = TownsPlayer = new Player_Towns_v1(this, Mixer);
                if (!TownsPlayer.Init())
                    Debug.WriteLine("Failed to initialize FM-Towns audio driver");
            }
            else if (Game.GameId == GameId.Loom || Game.GameId == GameId.Indy3)
            {
                MusicEngine = new Player_AD(this, Mixer);
            }
            else
            {
                MidiDriver nativeMidiDriver = null;
                MidiDriver adlibMidiDriver = null;
                if (Sound.MusicType == MusicDriverTypes.AdLib || Sound.MusicType == MusicDriverTypes.FMTowns)
                {
                    adlibMidiDriver = (MidiDriver)MidiDriver.CreateMidi(Mixer,
                        MidiDriver.DetectDevice(Sound.MusicType == MusicDriverTypes.FMTowns
                            ? MusicDriverTypes.FMTowns : MusicDriverTypes.AdLib, selectedDevice));
                    adlibMidiDriver.Property(AdlibMidiDriver.PropertyOldAdLib, (Game.Version < 5) ? 1 : 0);
                    adlibMidiDriver.Property(AdlibMidiDriver.PropertyScummOPL3, (Game.GameId == GameId.SamNMax) ? 1 : 0);
                }
                else if (Sound.MusicType == MusicDriverTypes.PCSpeaker)
                {
                    adlibMidiDriver = new PCSpeakerDriver(Mixer);
                }

                IMuse = Audio.IMuse.IMuse.Create(nativeMidiDriver, adlibMidiDriver);

                if (Game.Platform == Platform.FMTowns)
                {
                    MusicEngine = TownsPlayer = new Player_Towns_v2(this, Mixer, IMuse, true);
                    if (!TownsPlayer.Init())
                        throw new InvalidOperationException("ScummEngine::setupMusic(): Failed to initialize FM-Towns audio driver");
                }
                else
                {
                    MusicEngine = IMuse;
                }

                if (IMuse != null)
                {
                    IMuse.AddSysexHandler(0x7D, Game.GameId == GameId.SamNMax ? new SysExFunc(new SamAndMaxSysEx().Do) : new SysExFunc(new ScummSysEx().Do));
                    IMuse.Property(ImuseProperty.GameId, (uint)Game.GameId);
                    //                    IMuse.Property(ImuseProperty.NativeMt32, _native_mt32);
                    //                    if (MidiDriver.GetMusicType(deviceHandle) != MusicType.MT32) // MT-32 Emulation shouldn't be GM/GS initialized
                    //                        IMuse.Property(ImuseProperty.Gs, _enable_gs);
                    if (Sound.MusicType == MusicDriverTypes.PCSpeaker)
                        IMuse.Property(ImuseProperty.PcSpeaker, 1);
                }
            }

            if (MusicEngine != null)
            {
                MusicEngine.SetMusicVolume(192);
            }
        }

        protected abstract void SetupVars();

        protected abstract void ResetScummVars();

        protected void InitScreens(int b, int h)
        {
            if (_townsScreen != null)
            {
                if (_townsClearLayerFlag == 0 && MainVirtScreen != null && ((h - b) != MainVirtScreen.Height))
                    _townsScreen.ClearLayer(0);

                if (Game.GameId != GameId.Monkey1)
                {
                    Gdi.Fill(TextSurface,
                        new Rect(0, 0, (short) (_textSurface.Width * _textSurfaceMultiplier), (short) (_textSurface.Height * _textSurfaceMultiplier)), 0);
                    _townsScreen.ClearLayer(1);
                }
            }

            var format = Game.Features.HasFlag(GameFeatures.Is16BitColor) ? PixelFormat.Rgb16 : PixelFormat.Indexed8;

            _mainVirtScreen = new VirtScreen(b, ScreenWidth, h - b, format, 2, true);
            _textVirtScreen = new VirtScreen(0, ScreenWidth, b, format, 1);
            _verbVirtScreen = new VirtScreen(h, ScreenWidth, ScreenHeight - h, format, 1);

            // Since the size of screen 3 is fixed, there is no need to reallocate
            // it if its size changed.
            // Not sure what it is good for, though. I think it may have been used
            // in pre-V7 for the games messages (like 'Pause', Yes/No dialogs,
            // version display, etc.). I don't know about V7, maybe the same is the
            // case there. If so, we could probably just remove it completely.
            if (_game.Version >= 7)
            {
                _unkVirtScreen = new VirtScreen((ScreenHeight / 2) - 10, ScreenWidth, 13, format, 1);
            }
            else
            {
                _unkVirtScreen = new VirtScreen(80, ScreenWidth, 13, format, 1);
            }

            _screenB = b;
            _screenH = h;

            Gdi.Init();
        }

        #endregion

        #region Execution

        void Step()
        {
            var opCode = _currentScriptData[CurrentPos++];
            // execute the code
            ExecuteOpCode(opCode);
        }

        protected void ExecuteOpCode(byte opCode)
        {
            _opCode = opCode;
            if (_game.Version > 2) // V0-V2 games didn't use the didexec flag
                _slots[CurrentScript].IsExecuted = true;

            if (Game.Version < 6)
            {
                this.Trace().Write(TraceSwitches.OpCodes, "Room = {1}, Script = {0}, Offset = {4}, Name = {2} [{3:X2}]",
                    _slots[CurrentScript].Number,
                    _roomResource,
                    _opCodes.ContainsKey(_opCode) ? _opCodes[opCode].Method.Name : "Unknown",
                    _opCode,
                    CurrentPos - 1);
            }
            if (_opCodes.ContainsKey(opCode))
            {
                _opCodes[opCode]();
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid opcode 0x{0:X2}.", opCode));
            }
        }

        void RunCurrentScript()
        {
            while (CurrentScript != 0xFF)
            {
                Step();
            }
        }

        #endregion

        public override void Run()
        {
            var tsToWait = RunBootScript(Settings.BootParam);
            while (!HasToQuit)
            {
                if (!IsPaused)
                {
                    // Wait...
                    WaitForTimer((int)tsToWait.TotalMilliseconds);
                    tsToWait = Loop();
                    _gfxManager.UpdateScreen();
                }
            }
        }

        public override void LoadGameState(int slot)
        {
            Load(GetSaveGamePath(slot));
        }

        public override void SaveGameState(int slot, string desc)
        {
            Save(GetSaveGamePath(slot));
        }

        private string GetSaveGamePath(int index)
        {
            var game = Settings.Game;
            var dir = ServiceLocator.FileStorage.GetDirectoryName(game.Path);
            var filename = ServiceLocator.FileStorage.Combine(dir, string.Format("{0}{1}.sav", game.Id, (index + 1)));
            return filename;
        }

        protected abstract void InitOpCodes();

        private TimeSpan GetTimeToWaitBeforeLoop(TimeSpan lastTimeLoop)
        {
            var numTicks = ScummHelper.ToTicks(timeToWait);

            // Notify the script about how much time has passed, in ticks (60 ticks per second)
            if (VariableTimer.HasValue)
                _variables[VariableTimer.Value] = numTicks;
            if (VariableTimerTotal.HasValue)
                _variables[VariableTimerTotal.Value] += numTicks;

            // Determine how long to wait before the next loop iteration should start
            deltaTicks = VariableTimerNext.HasValue ? _variables[VariableTimerNext.Value] : 4;
            if (deltaTicks < 1) // Ensure we don't get into an endless loop
                deltaTicks = 1; // by not decreasing sleepers.

            timeToWait = ScummHelper.ToTimeSpan(deltaTicks);
            if (timeToWait > lastTimeLoop)
            {
                timeToWait -= lastTimeLoop;
            }
            else
            {
                timeToWait = TimeSpan.Zero;
            }
            return timeToWait;
        }

        protected virtual TimeSpan Loop()
        {
            var t = DateTime.Now;
            int delta = deltaTicks;

            if (Game.Version >= 3)
            {
                _variables[VariableTimer1.Value] += delta;
                _variables[VariableTimer2.Value] += delta;
                _variables[VariableTimer3.Value] += delta;

                if (Game.GameId == GameId.Indy3)
                {
                    _variables[39] += delta;
                    _variables[40] += delta;
                    _variables[41] += delta;
                }
            }

            if (delta > 15)
                delta = 15;

            DecreaseScriptDelay(delta);

            UpdateTalkDelay(delta);

            // Record the current ego actor before any scripts (including input scripts)
            // get a chance to run.
            var oldEgo = VariableEgo.HasValue ? Variables[VariableEgo.Value] : 0;

            // In V1-V3 games, CHARSET_1 is called much earlier than in newer games.
            // See also bug #770042 for a case were this makes a difference.
            if (Game.Version <= 3)
                Charset();

            ProcessInput();

            UpdateVariables();

            if (_game.Features.HasFlag(GameFeatures.AudioTracks))
            {
                // Covered automatically by the Sound class
            }
            else if (VariableMusicTimer.HasValue)
            {
                if (MusicEngine != null)
                {
                    // The music engine generates the timer data for us.
                    _variables[VariableMusicTimer.Value] = MusicEngine.GetMusicTimer();
                }
            }

            if (VariableGameLoaded.HasValue)
                _variables[VariableGameLoaded.Value] = 0;

            load_game:
            SaveLoad();

            if (_completeScreenRedraw)
            {
                _charset.HasMask = false;

                if (Game.Version > 3)
                {
                    for (int i = 0; i < Verbs.Length; i++)
                    {
                        DrawVerb(i, 0);
                    }
                }
                else
                {
                    RedrawVerbs();
                }

                HandleMouseOver(false);

                _completeScreenRedraw = false;
                _fullRedraw = true;
            }

            RunAllScripts();
            CheckExecVerbs();
            CheckAndRunSentenceScript();

            if (HasToQuit)
                return TimeSpan.Zero;

            // HACK: If a load was requested, immediately perform it. This avoids
            // drawing the current room right after the load is request but before
            // it is performed. That was annoying esp. if you loaded while a SMUSH
            // cutscene was playing.
            if (_saveLoadFlag != 0 && _saveLoadFlag != 1)
            {
                goto load_game;
            }

            TownsProcessPalCycleField();

            if (_currentRoom == 0)
            {
                Charset();
                DrawDirtyScreenParts();
            }
            else
            {
                WalkActors();
                MoveCamera();
                UpdateObjectStates();
                Charset();

                HandleDrawing();

                HandleActors();

                _fullRedraw = false;

                HandleEffects();

                //if (VAR_MAIN_SCRIPT != 0xFF && VAR(VAR_MAIN_SCRIPT) != 0) {
                //    runScript(VAR(VAR_MAIN_SCRIPT), 0, 0, 0);
                //}

                // Handle mouse over effects (for verbs).
                HandleMouseOver(oldEgo != Variables[VariableEgo.Value]);

                // Render everything to the screen.
                UpdatePalette();
                DrawDirtyScreenParts();

                // FIXME / TODO: Try to move the following to HandleSound or
                // scummLoop_handleActors (but watch out for regressions!)
                if (Game.Version <= 5)
                {
                    PlayActorSounds();
                }
            }

            HandleSound();

            _camera.LastPosition = _camera.CurrentPosition;

            //_res.increaseExpireCounter();

            AnimateCursor();

            // show or hide mouse
            _gfxManager.IsCursorVisible = _cursor.State > 0;

            return GetTimeToWaitBeforeLoop(DateTime.Now - t);
        }

        protected internal virtual void HandleSound()
        {
            Sound.ProcessSound();
        }

        private void UpdateTalkDelay(int delta)
        {
            _talkDelay -= delta;
            if (_talkDelay < 0)
            {
                _talkDelay = 0;
            }
        }

        protected void KillScriptsAndResources()
        {
            for (int i = 0; i < NumScriptSlot; i++)
            {
                var ss = _slots[i];
                if (ss.Where == WhereIsObject.Room || ss.Where == WhereIsObject.FLObject)
                {
                    if (ss.CutSceneOverride != 0)
                    {
                        //                        if (Game.Version >= 5)
                        //                            Console.Error.WriteLine("Object {0} stopped with active cutscene/override in exit", ss.Number);
                        ss.CutSceneOverride = 0;
                    }
                    //nukeArrays(i);
                    ss.Status = ScriptStatus.Dead;
                }
                else if (ss.Where == WhereIsObject.Local)
                {
                    if (ss.CutSceneOverride != 0)
                    {
                        //                        if (Game.Version >= 5)
                        //                            Console.Error.WriteLine("Script {0} stopped with active cutscene/override in exit", ss.Number);
                        ss.CutSceneOverride = 0;
                    }
                    //nukeArrays(i);
                    ss.Status = ScriptStatus.Dead;
                }
            }

            /* Nuke local object names */
            if (_newNames != null)
            {
                foreach (var obj in _newNames.Keys.ToArray())
                {
                    var owner = GetOwnerCore(obj);
                    // We can delete custom name resources if either the object is
                    // no longer in use (i.e. not owned by anyone anymore); or if
                    // it is an object which is owned by a room.
                    if (owner == 0 || (owner == OwnerRoom))
                    {
                        // WORKAROUND for a problem mentioned in bug report #941275:
                        // In FOA in the sentry room, in the chest plate of the statue,
                        // the pegs may be renamed to mouth: this custom name is lost
                        // when leaving the room; this hack prevents this).
                        //if (owner == OF_OWNER_ROOM && _game.id == GID_INDY4 && 336 <= obj && obj <= 340)
                        //    continue;

                        _newNames[obj] = new byte[0];
                    }
                }
            }
        }

        protected void JumpRelative(bool condition)
        {
            // We explicitly call ScummEngine::fetchScriptWord()
            // to make this method work also in v0, which overloads
            // fetchScriptWord to only read bytes (which is the right thing
            // to do for most opcodes, but not for jump offsets).
            var offset = (short)FetchScriptWord();
            if (!condition)
            {
                CurrentPos += offset;
                if (CurrentPos < 0)
                    throw new NotSupportedException("Invalid position in JumpRelative");
            }
        }

        public bool IsResourceInUse(ResType type, int idx)
        {
            //if (!ResourceManager.ValidateResource("isResourceInUse", type, idx))
            //    return false;
            switch (type)
            {
                case ResType.Room:
                    return _roomResource == (byte)idx;
                case ResType.RoomImage:
                    return _roomResource == (byte)idx;
                case ResType.RoomScripts:
                    return _roomResource == (byte)idx;
                case ResType.Script:
                    return IsScriptInUse(idx);
                case ResType.Costume:
                    return IsCostumeInUse(idx);
                case ResType.Sound:
                    // Sound resource 1 is used for queued speech
                    //if (_game.heversion >= 60 && idx == 1)
                    //    return true;
                    //else
                        return Sound.IsSoundInUse(idx);
                case ResType.Charset:
                    return _charset.GetCurId() == idx;
                /*case ResType.Image:
                    return ResourceManager.IsModified(type, idx) != 0;*/
                case ResType.SpoolBuffer:
                    return Sound.IsSoundRunning(10000 + idx);
                default:
                    return false;
            }
        }

        private bool IsCostumeInUse(int cost)
        {
            int i;
            Actor a;

            if (_roomResource != 0)
                for (i = 1; i < Actors.Length; i++)
                {
                    a = Actors[i];
                    if (a.IsInCurrentRoom && a.Costume == cost)
                        return true;
                }

            return false;
        }
    }
}
