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

using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using NScumm.Core.Audio;

namespace NScumm.Core
{
    public enum OpCodeParameter : byte
    {
        Param1 = 0x80,
        Param2 = 0x40,
        Param3 = 0x20,
    }

    public abstract partial class ScummEngine
    {
        #region Constants

        protected const int StringIdIqEpisode = 7;
        protected const int StringIdIqSeries = 9;
        protected const int StringIdSavename1 = 10;

        const uint CurrentVersion = 94;
        protected const int OwnerRoom = 0x0F;

        protected const int NumArray = 50;
        protected const int NumScriptSlot = 80;
        protected const int NumGlobalScripts = 200;

        protected const int MaxScriptNesting = 15;
        protected const int MaxCutsceneNum = 5;

        #endregion Constants

        #region Events

        public event EventHandler ShowMenuDialogRequested;

        #endregion Events

        #region Fields

        protected ResourceManager _resManager;
        protected byte _roomResource;
        protected byte[] _resourceMapper = new byte[128];

        protected bool _egoPositioned;

        protected Dictionary<byte, Action> _opCodes;
        protected byte _opCode;

        ICostumeLoader _costumeLoader;
        ICostumeRenderer _costumeRenderer;
        bool _keepText;
        int _talkDelay;
        int _haveMsg;

        public int ScreenTop;
        public int ScreenWidth = 320;
        public int ScreenHeight = 200;
        int _screenB;
        int _screenH;

        GameInfo _game;
        byte[] _gameMD5;
        int deltaTicks;
        TimeSpan timeToWait;

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

        public bool HastToQuit { get; set; }

        internal int ScreenStartStrip
        {
            get { return _screenStartStrip; }
        }

        internal bool EgoPositioned
        {
            get { return _egoPositioned; }
            set { _egoPositioned = value; }
        }

        internal ICostumeLoader CostumeLoader
        {
            get { return _costumeLoader; }
        }

        internal ICostumeRenderer CostumeRenderer { get { return _costumeRenderer; } }

        public int TalkDelay{ get { return _talkDelay; } }

        #endregion Properties

        #region Constructor

        public static ScummEngine Create(GameInfo game, IGraphicsManager gfxManager, IInputManager inputManager, IMixer mixer)
        {
            ScummEngine engine = null;
            if (game.Version == 3)
            {
                engine = new ScummEngine3(game, gfxManager, inputManager, mixer);
            }
            else if (game.Version == 4)
            {
                engine = new ScummEngine4(game, gfxManager, inputManager, mixer);
            }
            else if (game.Version == 5)
            {
                engine = new ScummEngine5(game, gfxManager, inputManager, mixer);
            }
            else if (game.Version == 6)
            {
                engine = new ScummEngine6(game, gfxManager, inputManager, mixer);
            }
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

        protected ScummEngine(GameInfo game, IGraphicsManager gfxManager, IInputManager inputManager, IMixer mixer)
        {
            _resManager = ResourceManager.Load(game);

            _game = game;
            InvalidBox = _game.Version < 5 ? (byte)255 : (byte)0;
            _gameMD5 = ToMd5Bytes(game.MD5);
            _gfxManager = gfxManager;
            _inputManager = inputManager;
            _strings = new byte[NumArray][];
            _charsets = new byte[NumArray][];
            _inventory = new ushort[_resManager.NumInventory];
            _invData = new ObjectData[_resManager.NumInventory];
            _currentScript = 0xFF;
            _sound = new Sound(this, mixer);

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
            Gdi = new Gdi(this, game);
            _costumeLoader = new ClassicCostumeLoader(this);
            _costumeRenderer = new ClassicCostumeRenderer(this);

            // Create the charset renderer
            _charset = game.Version == 3 ? (CharsetRenderer)new CharsetRenderer3(this) : new CharsetRendererClassic(this);

            ResetCursors();

            // Create the text surface
            _textSurface = new Surface(ScreenWidth * _textSurfaceMultiplier, ScreenHeight * _textSurfaceMultiplier, PixelFormat.Indexed8, false);
            ClearTextSurface();

            InitScreens(16, 144);
            _composite = new Surface(ScreenWidth, ScreenHeight, PixelFormat.Indexed8, false);
            InitActors();
            InitOpCodes();

            if (_game.Features.HasFlag(GameFeatures.SixteenColors))
            {
                for (int i = 0; i < 16; i++)
                {
                    _currentPalette.Colors[i] = Color.FromRgb(tableEGAPalette[i * 3], tableEGAPalette[i * 3 + 1], tableEGAPalette[i * 3 + 2]);
                }
            }

            for (int i = 0; i < 256; i++)
                Gdi.RoomPalette[i] = (byte)i;

            if (game.Features.HasFlag(GameFeatures.SixteenColors))
            {
                for (int i = 0; i < 256; i++)
                    _shadowPalette[i] = (byte)i;
            }

            InitializeVerbs();
            InitVariables();

            // WORKAROUND for bug in boot script of Loom (CD)
            // The boot script sets the characters of string 21,
            // before creating the string.resource.
            if (_game.Id == "loom")
            {
                _strings[21] = new byte[13];
            }

            if (_game.Version >= 5 && _game.Version <= 7)
                _sound.SetupSound();
        }

        protected void InitScreens(int b, int h)
        {
            const PixelFormat format = PixelFormat.Indexed8;

            _mainVirtScreen = new VirtScreen(b, ScreenWidth, h - b, format, 2, true);
            _textVirtScreen = new VirtScreen(0, ScreenWidth, b, format, 1);
            _verbVirtScreen = new VirtScreen(h, ScreenWidth, ScreenHeight - h, format, 1);
            _unkVirtScreen = new VirtScreen(80, ScreenWidth, 13, format, 1);

            _screenB = b;
            _screenH = h;

            Gdi.Init();
        }

        #endregion Constructor

        #region Execution

        void Step()
        {
            var opCode = _currentScriptData[_currentPos++];
            // execute the code
            ExecuteOpCode(opCode);
        }

        protected void ExecuteOpCode(byte opCode)
        {
            _opCode = opCode;
            _slots[_currentScript].IsExecuted = true;

            if (Game.Version < 6)
            {
                Console.WriteLine("Room = {1}, Script = {0}, Offset = {4}, Name = {2} [{3:X2}]", 
                    _slots[_currentScript].Number, 
                    _roomResource, 
                    _opCodes.ContainsKey(_opCode) ? _opCodes[opCode].Method.Name : "Unknown", 
                    _opCode,
                    _currentPos - 1);
            }
            _opCodes[opCode]();
        }

        void Run()
        {
            while (_currentScript != 0xFF)
            {
                Step();
            }
        }

        #endregion Execution

        protected abstract void InitOpCodes();

        TimeSpan GetTimeToWaitBeforeLoop(TimeSpan lastTimeLoop)
        {
            var numTicks = ScummHelper.ToTicks(timeToWait);
            _variables[VariableTimer.Value] = numTicks;
            _variables[VariableTimerTotal.Value] += numTicks;

            deltaTicks = _variables[VariableTimerNext.Value];
            if (deltaTicks < 1)
                deltaTicks = 1;

            timeToWait = ScummHelper.ToTimeSpan(deltaTicks);
            if (timeToWait > lastTimeLoop)
            {
                timeToWait -= lastTimeLoop;
            }
            return timeToWait;
        }

        public TimeSpan Loop()
        {
            var t = DateTime.Now;
            int delta = deltaTicks;

            _variables[VariableTimer1.Value] += delta;
            _variables[VariableTimer2.Value] += delta;
            _variables[VariableTimer3.Value] += delta;

            if (Game.Id == "indy3")
            {
                _variables[39] += delta;
                _variables[40] += delta;
                _variables[41] += delta;
            }

            if (delta > 15)
                delta = 15;

            DecreaseScriptDelay(delta);

            UpdateTalkDelay(delta);

            ProcessInput();

            UpdateVariables();

            // The music engine generates the timer data for us.
            _variables[VariableMusicTimer.Value] = _sound.GetMusicTimer();

            load_game:
            SaveLoad();

            if (_completeScreenRedraw)
            {
                _charset.HasMask = false;

                for (int i = 0; i < _verbs.Length; i++)
                {
                    DrawVerb(i, 0);
                }

                HandleMouseOver();

                _completeScreenRedraw = false;
                _fullRedraw = true;
            }

            RunAllScripts();
            CheckExecVerbs();
            CheckAndRunSentenceScript();

            if (HastToQuit)
                return TimeSpan.Zero;

            // HACK: If a load was requested, immediately perform it. This avoids
            // drawing the current room right after the load is request but before
            // it is performed. That was annoying esp. if you loaded while a SMUSH
            // cutscene was playing.
            if (_saveLoadFlag != 0 && _saveLoadFlag != 1)
            {
                goto load_game;
            }

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
                HandleMouseOver();

                // Render everything to the screen.
                UpdatePalette();
                DrawDirtyScreenParts();

                // FIXME / TODO: Try to move the following to scummLoop_handleSound or
                // scummLoop_handleActors (but watch out for regressions!)
                if (Game.Version <= 5)
                {
                    PlayActorSounds();
                }
            }

            _sound.ProcessSound();

            _camera.LastPosition = _camera.CurrentPosition;

            //_res->increaseExpireCounter();

            AnimateCursor();

            // show or hide mouse
            _gfxManager.ShowCursor(_cursor.State > 0);

            return GetTimeToWaitBeforeLoop(DateTime.Now - t);
        }

        void UpdateTalkDelay(int delta)
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
                        //if (_game.version >= 5)
                        //    warning("Object %d stopped with active cutscene/override in exit", ss->number);
                        ss.CutSceneOverride = 0;
                    }
                    //nukeArrays(i);
                    ss.Status = ScriptStatus.Dead;
                }
                else if (ss.Where == WhereIsObject.Local)
                {
                    if (ss.CutSceneOverride != 0)
                    {
                        //if (_game.version >= 5)
                        //    warning("Script %d stopped with active cutscene/override in exit", ss->number);
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
            var offset = (short)ReadWord();
            if (!condition)
            {
                _currentPos += offset;
                if (_currentPos < 0)
                    throw new NotSupportedException("Invalid position in JumpRelative");
            }
        }
    }
}