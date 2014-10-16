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

        const int StringIdIqEpisode = 7;
        const int StringIdIqSeries = 9;
        const int StringIdSavename1 = 10;

        const uint CurrentVersion = 94;
        const int OwnerRoom = 0x0F;
        const int NumVariables = 800;
        const int NumLocalObjects = 200;
        const int NumArray = 50;
        const int NumScriptSlot = 80;
        const int NumGlobalScripts = 200;
        const int NumInventory = 80;
        const int MaxScriptNesting = 15;
        const int MaxCutsceneNum = 5;

        #endregion Constants

        #region Events

        public event EventHandler ShowMenuDialogRequested;

        #endregion Events

        #region Fields

        ResourceManager _resManager;
        protected byte _roomResource;
        byte[] _resourceMapper = new byte[128];

        bool _egoPositioned;

        protected Dictionary<byte, Action> _opCodes;
        int _currentPos;
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

        #endregion Properties

        #region Constructor

        public static ScummEngine Create(GameInfo game, IGraphicsManager gfxManager, IInputManager inputManager, IAudioDriver driver)
        {
            ScummEngine engine = null;
            if (game.Version == 3)
            {
                engine = new ScummEngine3(game, gfxManager, inputManager, driver);
            }
            else if (game.Version == 4)
            {
                engine = new ScummEngine4(game, gfxManager, inputManager, driver);
            }
            else if (game.Version == 5)
            {
                engine = new ScummEngine5(game, gfxManager, inputManager, driver);
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

        protected ScummEngine(GameInfo game, IGraphicsManager gfxManager, IInputManager inputManager, IAudioDriver driver)
        {
            _resManager = ResourceManager.Load(game);

            _game = game;
            _gameMD5 = ToMd5Bytes(game.MD5);
            _gfxManager = gfxManager;
            _inputManager = inputManager;
            _strings = new byte[NumArray][];
            _charsets = new byte[NumArray][];
            _currentScript = 0xFF;
            _sound = new Sound(this, driver);

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
        }

        void InitScreens(int b, int h)
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

        void ExecuteOpCode(byte opCode)
        {
            _opCode = opCode;
            _slots[_currentScript].IsExecuted = true;

            Console.WriteLine("Room = {1}, Script = {0}, Offset = {4}, Name = {2} [{3:X2}]", 
                _slots[_currentScript].Number, 
                _roomResource, 
                _opCodes.ContainsKey(_opCode) ? _opCodes[opCode].Method.Name : "Unknown", 
                _opCode,
                _currentPos - 1);
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

        #region OpCodes

        protected virtual void InitOpCodes()
        {
            _opCodes = new Dictionary<byte, Action>();
            /* 00 */
            _opCodes[0x00] = StopObjectCode;
            _opCodes[0x01] = PutActor;
            _opCodes[0x02] = StartMusic;
            _opCodes[0x03] = GetActorRoom;
            /* 04 */
            _opCodes[0x04] = IsGreaterEqual;
            _opCodes[0x05] = DrawObject;
            _opCodes[0x06] = GetActorElevation;
            _opCodes[0x07] = SetState;
            /* 08 */
            _opCodes[0x08] = IsNotEqual;
            _opCodes[0x09] = FaceActor;
            _opCodes[0x0A] = StartScript;
            _opCodes[0x0B] = GetVerbEntrypoint;
            /* 0C */
            _opCodes[0x0C] = ResourceRoutines;
            _opCodes[0x0D] = WalkActorToActor;
            _opCodes[0x0E] = PutActorAtObject;
            _opCodes[0x0F] = IfState;
            /* 10 */
            _opCodes[0x10] = GetObjectOwner;
            _opCodes[0x11] = AnimateActor;
            _opCodes[0x12] = PanCameraTo;
            _opCodes[0x13] = ActorOps;
            /* 14 */
            _opCodes[0x14] = Print;
            _opCodes[0x15] = ActorFromPosition;
            _opCodes[0x16] = GetRandomNumber;
            _opCodes[0x17] = And;
            /* 18 */
            _opCodes[0x18] = JumpRelative;
            _opCodes[0x19] = DoSentence;
            _opCodes[0x1A] = Move;
            _opCodes[0x1B] = Multiply;
            /* 1C */
            _opCodes[0x1C] = StartSound;
            _opCodes[0x1D] = IfClassOfIs;
            _opCodes[0x1E] = WalkActorTo;
            /* 20 */
            _opCodes[0x20] = StopMusic;
            _opCodes[0x21] = PutActor;
            _opCodes[0x22] = SaveLoadGame;
            _opCodes[0x23] = GetActorY;
            /* 24 */
            _opCodes[0x24] = LoadRoomWithEgo;
            _opCodes[0x25] = DrawObject;
            _opCodes[0x26] = SetVarRange;
            _opCodes[0x27] = StringOperations;
            /* 28 */
            _opCodes[0x28] = EqualZero;
            _opCodes[0x29] = SetOwnerOf;
            _opCodes[0x2A] = StartScript;
            _opCodes[0x2B] = DelayVariable;
            /* 2C */
            _opCodes[0x2C] = CursorCommand;
            _opCodes[0x2D] = PutActorInRoom;
            _opCodes[0x2E] = Delay;
            _opCodes[0x2F] = IfNotState;
            /* 30 */
            _opCodes[0x30] = SetBoxFlags;
            _opCodes[0x31] = GetInventoryCount;
            _opCodes[0x32] = SetCameraAt;
            _opCodes[0x33] = RoomOps;
            /* 34 */
            _opCodes[0x34] = GetDistance;
            _opCodes[0x35] = FindObject;
            _opCodes[0x36] = WalkActorToObject;
            _opCodes[0x37] = StartObject;
            /* 38 */
            _opCodes[0x38] = IsLessEqual;
            _opCodes[0x39] = DoSentence;
            _opCodes[0x3A] = Subtract;
            _opCodes[0x3B] = WaitForActor;
            /* 3C */
            _opCodes[0x3C] = StopSound;
            _opCodes[0x3D] = FindInventory;
            _opCodes[0x3E] = WalkActorTo;
            _opCodes[0x3F] = DrawBox;
            /* 40 */
            _opCodes[0x40] = CutScene;
            _opCodes[0x41] = PutActor;
            _opCodes[0x42] = ChainScript;
            _opCodes[0x43] = GetActorX;
            /* 44 */
            _opCodes[0x44] = IsLess;
            _opCodes[0x45] = DrawObject;
            _opCodes[0x46] = Increment;
            _opCodes[0x47] = SetState;
            /* 48 */
            _opCodes[0x48] = IsEqual;
            _opCodes[0x49] = FaceActor;
            _opCodes[0x4A] = StartScript;
            _opCodes[0x4B] = GetVerbEntrypoint;
            /* 4C */
            _opCodes[0x4C] = WaitForSentence;
            _opCodes[0x4D] = WalkActorToActor;
            _opCodes[0x4E] = PutActorAtObject;
            _opCodes[0x4F] = IfState;
            /* 50 */
            _opCodes[0x50] = PickupObject;
            _opCodes[0x51] = AnimateActor;
            _opCodes[0x52] = ActorFollowCamera;
            _opCodes[0x53] = ActorOps;
            /* 54 */
            _opCodes[0x54] = SetObjectName;
            _opCodes[0x55] = ActorFromPosition;
            _opCodes[0x56] = GetActorMoving;
            _opCodes[0x57] = Or;
            /* 58 */
            _opCodes[0x58] = BeginOverride;
            _opCodes[0x59] = DoSentence;
            _opCodes[0x5A] = Add;
            _opCodes[0x5B] = Divide;
            /* 5C */
            _opCodes[0x5C] = OldRoomEffect;
            _opCodes[0x5D] = SetClass;
            _opCodes[0x5E] = WalkActorTo;
            /* 60 */
            _opCodes[0x60] = FreezeScripts;
            _opCodes[0x61] = PutActor;
            _opCodes[0x62] = StopScript;
            _opCodes[0x63] = GetActorFacing;
            /* 64 */
            _opCodes[0x64] = LoadRoomWithEgo;
            _opCodes[0x65] = DrawObject;
            _opCodes[0x67] = GetStringWidth;
            /* 68 */
            _opCodes[0x68] = IsScriptRunning;
            _opCodes[0x69] = SetOwnerOf;
            _opCodes[0x6A] = StartScript;
            _opCodes[0x6B] = DebugOp;
            /* 6C */
            _opCodes[0x6C] = GetActorWidth;
            _opCodes[0x6D] = PutActorInRoom;
            _opCodes[0x6E] = StopObjectScript;
            _opCodes[0x6F] = IfNotState;
            /* 70 */
            _opCodes[0x70] = Lights;
            _opCodes[0x71] = GetActorCostume;
            _opCodes[0x72] = LoadRoom;
            _opCodes[0x73] = RoomOps;
            /* 74 */
            _opCodes[0x74] = GetDistance;
            _opCodes[0x75] = FindObject;
            _opCodes[0x76] = WalkActorToObject;
            _opCodes[0x77] = StartObject;
            /* 78 */
            _opCodes[0x78] = IsGreater;
            _opCodes[0x79] = DoSentence;
            _opCodes[0x7A] = VerbOps;
            _opCodes[0x7B] = GetActorWalkBox;
            /* 7C */
            _opCodes[0x7C] = IsSoundRunning;
            _opCodes[0x7D] = FindInventory;
            _opCodes[0x7E] = WalkActorTo;
            _opCodes[0x7F] = DrawBox;
            /* 80 */
            _opCodes[0x80] = BreakHere;
            _opCodes[0x81] = PutActor;
            _opCodes[0x82] = StartMusic;
            _opCodes[0x83] = GetActorRoom;
            /* 84 */
            _opCodes[0x84] = IsGreaterEqual;
            _opCodes[0x85] = DrawObject;
            _opCodes[0x86] = GetActorElevation;
            _opCodes[0x87] = SetState;
            /* 88 */
            _opCodes[0x88] = IsNotEqual;
            _opCodes[0x89] = FaceActor;
            _opCodes[0x8A] = StartScript;
            _opCodes[0x8B] = GetVerbEntrypoint;
            /* 8C */
            _opCodes[0x8C] = ResourceRoutines;
            _opCodes[0x8D] = WalkActorToActor;
            _opCodes[0x8E] = PutActorAtObject;
            _opCodes[0x8F] = IfState;
            /* 90 */
            _opCodes[0x90] = GetObjectOwner;
            _opCodes[0x91] = AnimateActor;
            _opCodes[0x92] = PanCameraTo;
            _opCodes[0x93] = ActorOps;
            /* 94 */
            _opCodes[0x94] = Print;
            _opCodes[0x95] = ActorFromPosition;
            _opCodes[0x96] = GetRandomNumber;
            _opCodes[0x97] = And;
            /* 98 */
            _opCodes[0x98] = SystemOps;
            _opCodes[0x99] = DoSentence;
            _opCodes[0x9A] = Move;
            _opCodes[0x9B] = Multiply;
            /* 9C */
            _opCodes[0x9C] = StartSound;
            _opCodes[0x9D] = IfClassOfIs;
            _opCodes[0x9E] = WalkActorTo;
            /* A0 */
            _opCodes[0xA0] = StopObjectCode;
            _opCodes[0xA1] = PutActor;
            _opCodes[0xA2] = SaveLoadGame;
            _opCodes[0xA3] = GetActorY;
            /* A4 */
            _opCodes[0xA4] = LoadRoomWithEgo;
            _opCodes[0xA5] = DrawObject;
            _opCodes[0xA6] = SetVarRange;
            _opCodes[0xA7] = SaveLoadVars;
            /* A8 */
            _opCodes[0xA8] = NotEqualZero;
            _opCodes[0xA9] = SetOwnerOf;
            _opCodes[0xAA] = StartScript;
            _opCodes[0xAB] = SaveRestoreVerbs;
            /* AC */
            _opCodes[0xAC] = Expression;
            _opCodes[0xAD] = PutActorInRoom;
            _opCodes[0xAE] = Wait;
            _opCodes[0xAF] = IfNotState;
            /* B0 */
            _opCodes[0xB0] = SetBoxFlags;
            _opCodes[0xB1] = GetInventoryCount;
            _opCodes[0xB2] = SetCameraAt;
            _opCodes[0xB3] = RoomOps;
            /* B4 */
            _opCodes[0xB4] = GetDistance;
            _opCodes[0xB5] = FindObject;
            _opCodes[0xB6] = WalkActorToObject;
            _opCodes[0xB7] = StartObject;
            /* B8 */
            _opCodes[0xB8] = IsLessEqual;
            _opCodes[0xB9] = DoSentence;
            _opCodes[0xBA] = Subtract;
            _opCodes[0xBB] = WaitForActor;
            /* BC */
            _opCodes[0xBC] = StopSound;
            _opCodes[0xBD] = FindInventory;
            _opCodes[0xBE] = WalkActorTo;
            _opCodes[0xBF] = DrawBox;
            /* C0 */
            _opCodes[0xC0] = EndCutscene;
            _opCodes[0xC1] = PutActor;
            _opCodes[0xC2] = ChainScript;
            _opCodes[0xC3] = GetActorX;
            /* C4 */
            _opCodes[0xC4] = IsLess;
            _opCodes[0xC5] = DrawObject;
            _opCodes[0xC6] = Decrement;
            _opCodes[0xC7] = SetState;
            /* C8 */
            _opCodes[0xC8] = IsEqual;
            _opCodes[0xC9] = FaceActor;
            _opCodes[0xCA] = StartScript;
            _opCodes[0xCB] = GetVerbEntrypoint;
            /* CC */
            _opCodes[0xCC] = PseudoRoom;
            _opCodes[0xCD] = WalkActorToActor;
            _opCodes[0xCE] = PutActorAtObject;
            _opCodes[0xCF] = IfState;
            /* D0 */
            _opCodes[0xD0] = PickupObject;
            _opCodes[0xD1] = AnimateActor;
            _opCodes[0xD2] = ActorFollowCamera;
            _opCodes[0xD3] = ActorOps;
            /* D4 */
            _opCodes[0xD4] = SetObjectName;
            _opCodes[0xD5] = ActorFromPosition;
            _opCodes[0xD6] = GetActorMoving;
            _opCodes[0xD7] = Or;
            /* D8 */
            _opCodes[0xD8] = PrintEgo;
            _opCodes[0xD9] = DoSentence;
            _opCodes[0xDA] = Add;
            _opCodes[0xDB] = Divide;
            /* DC */
            _opCodes[0xDC] = OldRoomEffect;
            _opCodes[0xDD] = SetClass;
            _opCodes[0xDE] = WalkActorTo;
            /* E0 */
            _opCodes[0xE0] = FreezeScripts;
            _opCodes[0xE1] = PutActor;
            _opCodes[0xE2] = StopScript;
            _opCodes[0xE3] = GetActorFacing;
            /* E4 */
            _opCodes[0xE4] = LoadRoomWithEgo;
            _opCodes[0xE5] = DrawObject;
            _opCodes[0xE7] = GetStringWidth;
            /* E8 */
            _opCodes[0xE8] = IsScriptRunning;
            _opCodes[0xE9] = SetOwnerOf;
            _opCodes[0xEA] = StartScript;
            _opCodes[0xEB] = DebugOp;
            /* EC */
            _opCodes[0xEC] = GetActorWidth;
            _opCodes[0xED] = PutActorInRoom;
            _opCodes[0xEF] = IfNotState;
            /* F0 */
            _opCodes[0xF0] = Lights;
            _opCodes[0xF1] = GetActorCostume;
            _opCodes[0xF2] = LoadRoom;
            _opCodes[0xF3] = RoomOps;
            /* F4 */
            _opCodes[0xF4] = GetDistance;
            _opCodes[0xF5] = FindObject;
            _opCodes[0xF6] = WalkActorToObject;
            _opCodes[0xF7] = StartObject;
            /* F8 */
            _opCodes[0xF8] = IsGreater;
            _opCodes[0xF9] = DoSentence;
            _opCodes[0xFA] = VerbOps;
            _opCodes[0xFB] = GetActorWalkBox;
            /* FC */
            _opCodes[0xFC] = IsSoundRunning;
            _opCodes[0xFD] = FindInventory;
            _opCodes[0xFE] = WalkActorTo;
            _opCodes[0xFF] = DrawBox;
        }

        void SetBoxFlags()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = ReadByte();
            SetBoxFlags(a, b);
        }

        void SystemOps()
        {
            byte subOp = ReadByte();
            switch (subOp)
            {
                case 1:		// SO_RESTART
				//restart();
                    break;
                case 2:		// SO_PAUSE
				//pauseGame();
                    break;
                case 3:		// SO_QUIT
				//quitGame();
                    break;
            }
        }

        void DebugOp()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            System.Diagnostics.Debug.WriteLine("Debug: {0}", a);
        }

        void DelayVariable()
        {
            _slots[_currentScript].Delay = GetVar();
            _slots[_currentScript].Status = ScriptStatus.Paused;
            BreakHere();
        }

        void Wait()
        {
            var oldPos = _currentPos - 1;
            if (Game.Id == "indy3")
            {
                _opCode = 2;
            }
            else
            {
                _opCode = ReadByte();
            }

            switch (_opCode & 0x1F)
            {
                case 1:     // SO_WAIT_FOR_ACTOR
                    {
                        var a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
                        if (a != null && a.Moving != 0)
                            break;
                        return;
                    }
                case 2:     // SO_WAIT_FOR_MESSAGE
                    if (_variables[VariableHaveMessage.Value] != 0)
                        break;
                    return;

                case 3:     // SO_WAIT_FOR_CAMERA
                    if (_camera.CurrentPosition.X / 8 != _camera.DestinationPosition.X / 8)
                        break;
                    return;

                case 4:     // SO_WAIT_FOR_SENTENCE
                    if (_sentenceNum != 0)
                    {
                        if (_sentence[_sentenceNum - 1].IsFrozen && !IsScriptInUse(_variables[VariableSentenceScript.Value]))
                            return;
                    }
                    else if (!IsScriptInUse(_variables[VariableSentenceScript.Value]))
                        return;
                    break;

                default:
                    throw new NotImplementedException("Wait: unknown subopcode" + (_opCode & 0x1F));
            }

            _currentPos = oldPos;
            BreakHere();
        }

        void Delay()
        {
            uint delay = ReadByte();
            delay |= (uint)(ReadByte() << 8);
            delay |= (uint)(ReadByte() << 16);
            _slots[_currentScript].Delay = (int)delay;
            _slots[_currentScript].Status = ScriptStatus.Paused;
            BreakHere();
        }

        void Move()
        {
            GetResult();
            var result = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(result);
        }

        void ResourceRoutines()
        {
            int resId = 0;

            _opCode = ReadByte();
            if (_opCode != 17)
            {
                resId = GetVarOrDirectByte(OpCodeParameter.Param1);
            }

            int op = _opCode & 0x3F;
            switch (op)
            {
                case 1: // load script
                case 2: // load sound
                case 3: // load costume
				// TODO: load room/sound/script
                    break;

                case 4: // load room
				// TODO: load room
                    break;

                case 5:         // SO_NUKE_SCRIPT
                case 6:         // SO_NUKE_SOUND
                case 7:         // SO_NUKE_COSTUME
                case 8:         // SO_NUKE_ROOM
                    break;

                case 9:         // SO_LOCK_SCRIPT
                    if (resId < NumGlobalScripts)
                    {
                        //_res.Sounds[resId].Lock = true;
                    }
                    break;

                case 10:
				// TODO: lock Sound
                    break;

                case 11:        // SO_LOCK_COSTUME
				//_res.Costumes[resId].Lock = true;
                    break;

                case 12:        // SO_LOCK_ROOM
				// TODO: lock room
				// if (resid > 0x7F)
				//    resid = _resourceMapper[resid & 0x7F];
				//_res->lock(rtRoom, resid);
                    break;

                case 13:        // SO_UNLOCK_SCRIPT
                    break;

                case 14:        // SO_UNLOCK_SOUND
                    break;

                case 15:        // SO_UNLOCK_COSTUME
                    break;

                case 16:        // SO_UNLOCK_ROOM
                    if (resId > 0x7F)
                        resId = _resourceMapper[resId & 0x7F];
				// TODO: unlock room
				//_res->unlock(rtRoom, resId);
                    break;

                case 17:
				// SO_CLEAR_HEAP
				//heapClear(0);
				//unkHeapProc2(0, 0);
                    break;

                case 18:
                    LoadCharset(resId);
                    break;

                case 20:        // SO_LOAD_OBJECT
                    LoadFlObject(GetVarOrDirectWord(OpCodeParameter.Param2), resId);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        List<ObjectData> flObjects = new List<ObjectData>();

        void LoadFlObject(int obj, int room)
        {
            var od = ResourceManager.GetRoom((byte)room).Objects.First(o => o.Number == obj);
            for (int i = 0; i < _objs.Length; i++)
            {
                if (_objs[i].Number == 0)
                {
                    _objs[i] = od;
                    od.FloatingObjectIndex = flObjects.Count;
                    flObjects.Add(od);
                    return;
                }
            }
        }

        void GetRandomNumber()
        {
            GetResult();
            var max = GetVarOrDirectByte(OpCodeParameter.Param1);
            var rnd = new Random();
            var value = rnd.Next(max + 1);
            SetResult(value);
        }

        #endregion OpCodes

        void KillScriptsAndResources()
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
                    var owner = GetOwner(obj);
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

        void JumpRelative(bool condition)
        {
            var offset = (short)ReadWord();
            if (!condition)
            {
                _currentPos += offset;
                if (_currentPos < 0)
                    throw new NotSupportedException("Invalid position in JumpRelative");
            }
        }

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

            _sound.ProcessSoundQueue();

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
    }
}