/*
 * This file is part of NScumm.
 *
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;

namespace NScumm.Core
{
    [Flags]
    enum LightModes
    {
        /**
         * Lighting flag that indicates whether the normal palette, or the 'dark'
         * palette shall be used to draw actors.
         * Apparantly only used in very old games (so far only NESCostumeRenderer
         * checks it).
         */
        ActorUseBasePalette = 1,

        /**
         * Lighting flag that indicates whether the room is currently lit. Normally
         * always on. Used for rooms in which the light can be switched "off".
         */
        RoomLightsOn = 2,

        /**
         * Lighting flag that indicates whether a flashlight like device is active.
         * Used in Loom (flashlight follows the actor) and Indy 3 (flashlight
         * follows the mouse). Only has any effect if the room lights are off.
         */
        FlashlightOn = 4,

        /**
         * Lighting flag that indicates whether actors are to be drawn with their
         * own custom palette, or using a fixed 'dark' palette. This is the
         * modern successor of LIGHTMODE_actor_use_base_palette.
         * Note: It is tempting to 'merge' these two flags, but since flags can
         * check their values, this is probably not a good idea.
         */
        ActorUseColors = 8
    }

    enum OpCodeParameter : byte
    {
        Param1 = 0x80,
        Param2 = 0x40,
        Param3 = 0x20,
    }

    public partial class ScummEngine
    {
        #region Constants
        static readonly int[] ShakePositions={0,1*2,2*2,1*2,0*2,2*2,3*2,1*2};

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

        public const int VariableEgo = 0x01;
        const int VariableCameraPosX = 0x02;
        const int VariableHaveMessage = 0x03;
        const int VariableRoom = 0x04;
        const int VariableOverride = 0x05;
        const int VariableCurrentLights = 0x09;
        public const int VariableTimer1 = 0x0B;
        public const int VariableTimer2 = 0x0C;
        public const int VariableTimer3 = 0x0D;
        const int VariableCameraMinX = 0x11;
        const int VariableCameraMaxX = 0x12;
        public const int VariableTimerNext = 0x13;
        public const int VariableVirtualMouseX = 0x14;
        public const int VariableVirtualMouseY = 0x15;
        const int VariableRoomResource = 0x16;
        public const int VariableCutSceneExitKey = 0x18;
        const int VariableTalkActor = 0x19;
        const int VariableCameraFastX = 0x1A;
        const int VariableScrollScript = 0x1B;
        const int VariableEntryScript = 0x1C;
        const int VariableEntryScript2 = 0x1D;
        const int VariableExitScript = 0x1E;
        const int VariableVerbScript = 0x20;
        const int VariableSentenceScript = 0x21;
        const int VariableInventoryScript = 0x22;
        const int VariableCutSceneStartScript = 0x23;
        const int VariableCutSceneEndScript = 0x24;
        public const int VariableCharIncrement = 0x25;
        const int VariableWalkToObject = 0x26;
        const int VariableDebugMode = 0x27;
        const int VariableHeapSpace = 0x28;
        public const int VariableMouseX = 0x2C;
        public const int VariableMouseY = 0x2D;
        const int VariableTimer = 0x2E;
        const int VariableTimerTotal = 0x2F;
        const int VariableSoundcard = 0x30;
        const int VariableVideoMode = 0x31;
        const int VariableMainMenu = 0x32;
        const int VariableFixedDisk = 0x33;
        const int VariableCursorState = 0x34;
        const int VariableUserPut = 0x35;
        const int VariableTalkStringY = 0x36;

        static byte[] tableEGAPalette = new byte[]{
            0x00, 0x00, 0x00,   0x00, 0x00, 0xAA,   0x00, 0xAA, 0x00,   0x00, 0xAA, 0xAA,
            0xAA, 0x00, 0x00,   0xAA, 0x00, 0xAA,   0xAA, 0x55, 0x00,   0xAA, 0xAA, 0xAA,
            0x55, 0x55, 0x55,   0x55, 0x55, 0xFF,   0x55, 0xFF, 0x55,   0x55, 0xFF, 0xFF,
            0xFF, 0x55, 0x55,   0xFF, 0x55, 0xFF,   0xFF, 0xFF, 0x55,   0xFF, 0xFF, 0xFF
        };

        #endregion Constants

        #region Events

        public event EventHandler ShowMenuDialogRequested;

        #endregion Events

        #region Fields

        bool _shakeEnabled;
        int _shakeFrame;

        List<byte> _boxMatrix = new List<byte>();
        ScummIndex _scumm;
        string _directory;
        byte _currentRoom;
        int _actorToPrintStrFor;
        ushort[] _inventory = new ushort[NumInventory];

        BitArray _bitVars = new BitArray(4096);
        int[] _variables;
        Stack<int> _stack = new Stack<int>();
        int _resultVarIndex;

        Actor[] _actors = new Actor[13];
        ObjectData[] _objs = new ObjectData[200];
        HashSet<ObjectData> _drawingObjects = new HashSet<ObjectData>();

        sbyte _userPut;
        byte _roomResource;
        bool _egoPositioned;

        Cursor _cursor = new Cursor();
        Point _mousePos;

        Dictionary<byte, Action> _opCodes;
        byte[] _currentScriptData;
        int _currentPos;
        byte _opCode;
        byte _currentScript;
        int _numNestedScripts;
        NestedScript[] _nest;
        ScriptSlot[] _slots;

        CutScene cutScene = new CutScene();

        Room roomData;
        int _sentenceNum;
        TextSlot[] _string = new TextSlot[6];
        byte[] _charsetBuffer = new byte[512];

        byte[] _resourceMapper = new byte[128];
        byte[][] _strings;
        byte[][] _charsets;
        public byte[] CharsetColorMap = new byte[16];

        FlashLight _flashlight;
        Camera _camera = new Camera();
        ICostumeLoader _costumeLoader;
        ICostumeRenderer _costumeRenderer;

        bool _keepText;
        bool _useTalkAnims;
        byte _charsetColor;
        int _talkDelay;
        int _haveMsg;
        int _charsetBufPos;

        VerbSlot[] _verbs;
        byte cursorColor;
        int _currentCursor;

        int _screenStartStrip;
        int _screenEndStrip;
        public int ScreenTop;
        public int ScreenWidth = 320;
        public int ScreenHeight = 200;

        VirtScreen _mainVirtScreen;
        VirtScreen _textVirtScreen;
        VirtScreen _verbVirtScreen;
        VirtScreen _unkVirtScreen;
        Surface _textSurface;
        Surface _composite;

        bool _bgNeedsRedraw;
        bool _fullRedraw;
        internal Gdi Gdi;

        // Somewhat hackish stuff for 2 byte support (Chinese/Japanese/Korean)
        public byte NewLineCharacter;

        internal bool UseCjkMode;
        internal int _2byteWidth;

        static byte[] defaultCursorColors = new byte[] { 15, 15, 7, 8 };

        public byte[] RoomPalette = new byte[256];

        byte _newEffect = 129, _switchRoomEffect2, _switchRoomEffect;
        bool _disableFadeInEffect;
        bool _doEffect;
        bool _screenEffectFlag;

        ScaleSlot[] _scaleSlots;

        int _numGlobalObjects = 1000;
        bool _haveActorSpeechMsg;

        CharsetRenderer _charset;
        const byte CharsetMaskTransparency = 0xFD;
        GameInfo _game;
        Box[] _boxes;

        byte[] _gameMD5;
        int _numLocalScripts = 60;
        int _screenB;
        int _screenH;

        bool _completeScreenRedraw;
        IGraphicsManager _gfxManager;
        IInputManager _inputManager;
        int _palDirtyMin, _palDirtyMax;
        int _nextLeft, _nextTop;
        int _textSurfaceMultiplier = 1;

        byte[] _shadowPalette = new byte[256];

        Sentence[] _sentence = InitSentences();
        ObjectData[] _invData = new ObjectData[NumInventory];

        KeyCode mouseAndKeyboardStat;

        Dictionary<int, byte[]> _newNames = new Dictionary<int, byte[]>();

        #endregion Fields

        #region Properties

        internal uint[] ClassData
        {
            get { return _scumm.ClassData; }
        }

        internal int[] Variables
        {
            get { return _variables; }
        }

        internal Room CurrentRoomData
        {
            get { return roomData; }
        }

        internal byte CurrentRoom
        {
            get { return _currentRoom; }
        }

        internal ScummIndex Index { get { return _scumm; } }

        internal VirtScreen MainVirtScreen
        {
            get { return _mainVirtScreen; }
        }

        public GameInfo Game
        {
            get { return _game; }
        }

        public bool HastToQuit { get; set; }

        #endregion Properties

        #region Constructor

        public ScummEngine(GameInfo game, IGraphicsManager gfxManager, IInputManager inputManager)
        {
            _scumm = new ScummIndex();
            _scumm.LoadIndex(game.Path);

            _game = game;
            _gameMD5 = Encoding.Default.GetBytes(game.MD5);
            _gfxManager = gfxManager;
            _inputManager = inputManager;
            _directory = _scumm.Directory;
            _strings = new byte[NumArray][];
            _charsets = new byte[NumArray][];
            _currentScript = 0xFF;
            _gfxUsageBits = new uint[410 * 3];
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
            Gdi = new Gdi(this);
            _costumeLoader = new ClassicCostumeLoader(_scumm);
            _costumeRenderer = new ClassicCostumeRenderer(this);

            // Create the charset renderer
            _charset = new CharsetRendererClassic(this);

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
                    _currentPalette.Colors [i] = Color.FromRgb(tableEGAPalette [i * 3], tableEGAPalette [i * 3 + 1], tableEGAPalette [i * 3 + 2]);
                }
            }

            for (int i = 0; i < 256; i++)
                RoomPalette[i] = (byte)i;

            if (game.Features.HasFlag(GameFeatures.SixteenColors))
            {
                for (int i = 0; i < 256; i++)
                    _shadowPalette [i] = (byte)i;
            }

            InitializeVerbs();
            InitVariables();
        }

        void InitializeVerbs()
        {
            _verbs = new VerbSlot[100];
            for (int i = 0; i < 100; i++)
            {
                _verbs[i] = new VerbSlot();
                _verbs[i].CurRect.Right = ScreenWidth - 1;
                _verbs[i].OldRect.Left = -1;
                _verbs[i].Color = 2;
                _verbs[i].CharsetNr = 1;
            }
        }

        void InitActors()
        {
            for (byte i = 0; i < _actors.Length; i++)
            {
                _actors[i] = new Actor(this, i);
                _actors[i].Init(-1);
            }
        }

        void InitVariables()
        {
            _variables = new int[NumVariables];
            _variables[VariableVideoMode] = 19;
            _variables[VariableFixedDisk] = 1;
            _variables[VariableHeapSpace] = 1400;
            _variables[VariableCharIncrement] = 4;
            TalkingActor = 0;
#if DEBUG
            //_variables[VariableDebugMode] = 1;
#endif
            // MDT_ADLIB
            //_variables[VariableSoundcard] = 3;

            _variables[VariableTalkStringY] = -0x50;

            // Setup light
            _variables[VariableCurrentLights] = (int)(LightModes.ActorUseBasePalette | LightModes.ActorUseColors | LightModes.RoomLightsOn);

            if (_game.Id == "monkey")
            {
                _variables[74] = 1225;
            }
        }

        static Sentence[] InitSentences()
        {
            var sentences = new Sentence[6];
            for (int i = 0; i < sentences.Length; i++)
            {
                sentences[i] = new Sentence();
            }
            return sentences;
        }

        void InitScreens(int b, int h)
        {
            var format = PixelFormat.Indexed8;

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
            //Console.WriteLine("OpCode: {1:X2}, Name = {2}", _currentScript, _opCode, _opCodes.ContainsKey(_opCode) ? _opCodes[opCode].Method.Name : "Unknown");
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

        #region Variable Manipulation

        byte ReadByte()
        {
            return _currentScriptData[_currentPos++];
        }

        ushort ReadWord()
        {
            ushort word = (ushort)(_currentScriptData[_currentPos++] | (_currentScriptData[_currentPos++] << 8));
            return word;
        }

        void GetResult()
        {
            _resultVarIndex = ReadWord();
            if ((_resultVarIndex & 0x2000) == 0x2000)
            {
                int a = (int)ReadWord();
                if ((a & 0x2000) == 0x2000)
                {
                    _resultVarIndex += ReadVariable(a & ~0x2000);
                }
                else
                {
                    _resultVarIndex += (a & 0xFFF);
                }
                _resultVarIndex &= ~0x2000;
            }
        }

        int ReadVariable(int var)
        {
            if ((var & 0x2000) == 0x2000)
            {
                var a = ReadWord();
                if ((a & 0x2000) == 0x2000)
                    var += ReadVariable(a & ~0x2000);
                else
                    var += a & 0xFFF;
                var &= ~0x2000;
            }

            if ((var & 0xF000) == 0)
            {
                //Console.WriteLine("ReadVariable({0}) => {1}", var, _variables[var]);
                ScummHelper.AssertRange(0, var, NumVariables - 1, "variable (reading)");
                return _variables[var];
            }

            if ((var & 0x8000) == 0x8000)
            {
                var &= 0x7FFF;

                ScummHelper.AssertRange(0, _resultVarIndex, _bitVars.Length - 1, "variable (reading)");
                return _bitVars[var] ? 1 : 0;
            }

            if ((var & 0x4000) == 0x4000)
            {
                var &= 0xFFF;

                ScummHelper.AssertRange(0, var, 20, "local variable (reading)");
                return _slots[_currentScript].LocalVariables[var];
            }

            throw new NotSupportedException("Illegal varbits (r)");
        }

        int GetVarOrDirectWord(OpCodeParameter param)
        {
            if (((OpCodeParameter)_opCode).HasFlag(param))
                return GetVar();
            return ReadWordSigned();
        }

        int GetVarOrDirectByte(OpCodeParameter param)
        {
            if (((OpCodeParameter)_opCode).HasFlag(param))
                return GetVar();
            return ReadByte();
        }

        int GetVar()
        {
            return ReadVariable(ReadWord());
        }

        short ReadWordSigned()
        {
            return (short)ReadWord();
        }

        int[] GetWordVarArgs()
        {
            var args = new List<int>();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                args.Add(GetVarOrDirectWord(OpCodeParameter.Param1));
            }
            return args.ToArray();
        }

        void SetResult(int value)
        {
            if ((_resultVarIndex & 0xF000) == 0)
            {
                ScummHelper.AssertRange(0, _resultVarIndex, NumVariables - 1, "variable (writing)");
                //Console.WriteLine ("SetResult({0},{1})",_resultVarIndex,value);
                _variables[_resultVarIndex] = value;
                return;
            }

            if ((_resultVarIndex & 0x8000) != 0)
            {
                _resultVarIndex &= 0x7FFF;

                ScummHelper.AssertRange(0, _resultVarIndex, _bitVars.Length - 1, "bit variable (writing)");
                //Console.WriteLine ("SetResult({0},{1})",_resultVarIndex,value!=0);
                _bitVars[_resultVarIndex] = value != 0;
                return;
            }

            if ((_resultVarIndex & 0x4000) != 0)
            {
                _resultVarIndex &= 0xFFF;

                ScummHelper.AssertRange(0, _resultVarIndex, 20, "local variable (writing)");
                //Console.WriteLine ("SetLocalVariables(script={0},var={1},value={2})",_currentScript, _resultVarIndex,value);
                _slots[_currentScript].LocalVariables[_resultVarIndex] = value;
                return;
            }
        }

        #endregion Variable Manipulation

        #region OpCodes

        void InitOpCodes()
        {
            _opCodes = new Dictionary<byte, Action>();
            /* 00 */
            _opCodes[0x00] = StopObjectCode;
            _opCodes[0x01] = PutActor;
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
            _opCodes[0x21] = PutActor;
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
            _opCodes[0x30] = MatrixOperations;
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
            _opCodes[0xA3] = GetActorY;
            /* A4 */
            _opCodes[0xA4] = LoadRoomWithEgo;
            _opCodes[0xA5] = DrawObject;
            _opCodes[0xA6] = SetVarRange;
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
            _opCodes[0xB0] = MatrixOperations;
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

        void DebugOp()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            System.Diagnostics.Debug.WriteLine("Debug: {0}", a);
        }

        void GetActorCostume()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = _actors[act];
            SetResult(a.Costume);
        }

        void SetObjectName()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetObjectName(obj);
        }

        void GetActorMoving()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = _actors[act];
            SetResult((int)a.Moving);
        }

        void PutActorAtObject()
        {
            int obj;
            Point p;
            Actor a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                p = GetObjectXYPos(obj);
            }
            else
            {
                p = new Point(240,120);
            }
            a.PutActor(p);
        }

        void WalkActorToActor()
        {
            int x, y;
            int nr = GetVarOrDirectByte(OpCodeParameter.Param1);
            int nr2 = GetVarOrDirectByte(OpCodeParameter.Param2);
            int dist = ReadByte();

            var a = _actors[nr];
            if (!a.IsInCurrentRoom)
                return;

            var a2 = _actors[nr2];
            if (!a2.IsInCurrentRoom)
                return;

            if (dist == 0xFF)
            {
                dist = (int)(a.ScaleX * a.Width / 0xFF);
                dist += (int)(a2.ScaleX * a2.Width / 0xFF) / 2;
            }
            x = a2.Position.X;
            y = a2.Position.Y;
            if (x < a.Position.X)
                x += dist;
            else
                x -= dist;

            a.StartWalk(new Point((short)x, (short)y), -1);
        }

        void PanCameraTo()
        {
            PanCameraTo(GetVarOrDirectWord(OpCodeParameter.Param1));
        }

        void GetActorX()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(GetObjX(a));
        }

        void GetActorY()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(GetObjY(a));
        }

        void MatrixOperations()
        {
            int a, b;

            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxFlags(a, b);
                    break;

                case 2:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxScale(a, b);
                    break;

                case 3:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxScale(a, (b - 1) | 0x8000);
                    break;

                case 4:
                    CreateBoxMatrix();
                    break;
            }
        }

        void DelayVariable()
        {
            _slots[_currentScript].Delay = GetVar();
            _slots[_currentScript].Status = ScriptStatus.Paused;
            BreakHere();
        }

        void ActorFromPosition()
        {
            GetResult();
            var x = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            var y = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
            var actor = GetActorFromPos(new Point(x, y));
            SetResult(actor);
        }

        void ChainScript()
        {
            int script;
            int cur;

            script = GetVarOrDirectByte(OpCodeParameter.Param1);

            var vars = GetWordVarArgs();

            cur = _currentScript;

            _slots[cur].Number = 0;
            _slots[cur].Status = ScriptStatus.Dead;
            _currentScript = 0xFF;

            RunScript((byte)script, _slots[cur].FreezeResistant, _slots[cur].Recursive, vars);
        }

        void GetDistance()
        {
            GetResult();
            var o1 = GetVarOrDirectWord(OpCodeParameter.Param1);
            var o2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            var r = GetObjActToObjActDist(o1, o2);

            // TODO: WORKAROUND bug #795937 ?
            //if ((_game.id == GID_MONKEY_EGA || _game.id == GID_PASS) && o1 == 1 && o2 == 307 && vm.slot[_currentScript].number == 205 && r == 2)
            //    r = 3;

            SetResult(r);
        }

        void IfClassOfIs()
        {
            int obj, cls;
            bool cond = true;

            obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            while ((_opCode = ReadByte()) != 0xFF)
            {
                cls = GetVarOrDirectWord(OpCodeParameter.Param1);

                // WORKAROUND bug #1668393: Due to a script bug, the wrong opcode is
                // used to test and set the state of various objects (e.g. the inside
                // door (object 465) of the of the Hostel on Mars), when opening the
                // Hostel door from the outside.

                bool b = GetClass(obj, (ObjectClass)cls);
                if ((((cls & 0x80) != 0) && !b) || ((0 == (cls & 0x80)) && b))
                    cond = false;
            }
            JumpRelative(cond);
        }

        void IfState()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = GetVarOrDirectByte(OpCodeParameter.Param2);

            JumpRelative(GetState(a) == b);
        }

        void IfNotState()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = GetVarOrDirectByte(OpCodeParameter.Param2);

            JumpRelative(GetState(a) != b);
        }

        void GetActorWalkBox()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = _actors[act];
            SetResult(a.Walkbox);
        }

        void Wait()
        {
            var oldPos = _currentPos - 1;
            _opCode = ReadByte();

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
                    if (_variables[VariableHaveMessage] != 0)
                        break;
                    return;

                case 3:     // SO_WAIT_FOR_CAMERA
                    if (_camera.CurrentPosition.X / 8 != _camera.DestinationPosition.X / 8)
                        break;
                    return;

                case 4:     // SO_WAIT_FOR_SENTENCE
                    if (_sentenceNum != 0)
                    {
                        if (_sentence[_sentenceNum - 1].FreezeCount != 0 && !IsScriptInUse(_variables[VariableSentenceScript]))
                            return;
                    }
                    else if (!IsScriptInUse(_variables[VariableSentenceScript]))
                        return;
                    break;

                default:
                    throw new NotImplementedException("Wait: unknown subopcode" + (_opCode & 0x1F));
            }

            _currentPos = oldPos;
            BreakHere();
        }

        void WalkActorTo()
        {
            var a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            var x = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
            var y = (short)GetVarOrDirectWord(OpCodeParameter.Param3);
            a.StartWalk(new Point(x, y), -1);
        }

        void WalkActorToObject()
        {
            var a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                int dir;
                Point p;
                GetObjectXYPos(obj, out p, out dir);
                a.StartWalk(p, dir);
            }
        }

        void FaceActor()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            int obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            var a = _actors[act];
            a.FaceToObject(obj);
        }

        void PrintEgo()
        {
            _actorToPrintStrFor = (byte)_variables[VariableEgo];
            DecodeParseString();
        }

        void FindObject()
        {
            GetResult();
            int x = GetVarOrDirectByte(OpCodeParameter.Param1);
            int y = GetVarOrDirectByte(OpCodeParameter.Param2);
            SetResult(FindObject(x, y));
        }

        void SetOwnerOf()
        {
            int obj, owner;

            obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            owner = GetVarOrDirectByte(OpCodeParameter.Param2);

            SetOwnerOf(obj, owner);
        }

        void IsSoundRunning()
        {
            int snd;
            GetResult();
            snd = GetVarOrDirectByte(OpCodeParameter.Param1);
            if (snd != 0)
            {
                // TODO:
                //snd = _sound->isSoundRunning(snd);
                snd = 0;
            }
            SetResult(snd);
        }

        void LoadRoomWithEgo()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            int room = GetVarOrDirectByte(OpCodeParameter.Param2);

            Actor a = _actors[_variables[VariableEgo]];

            a.PutActor((byte)room);
            int oldDir = a.Facing;
            _egoPositioned = false;

            short x = ReadWordSigned();
            short y = ReadWordSigned();

            _variables[VariableWalkToObject] = obj;
            StartScene(a.Room);
            _variables[VariableWalkToObject] = 0;

            if (!_egoPositioned)
            {
                int dir;
                Point p;
                GetObjectXYPos(obj, out p, out dir);
                a.PutActor(p, _currentRoom);
                if (a.Facing == oldDir)
                    a.SetDirection(dir + 180);
            }
            a.Moving = 0;

            // This is based on disassembly
            _camera.CurrentPosition.X = _camera.DestinationPosition.X = a.Position.X;
            SetCameraFollows(a, false);

            _fullRedraw = true;

            if (x != -1)
            {
                a.StartWalk(new Point(x, y), -1);
            }
        }

        void GetInventoryCount()
        {
            GetResult();
            SetResult(GetInventoryCount(GetVarOrDirectByte(OpCodeParameter.Param1)));
        }

        void FindInventory()
        {
            GetResult();
            int x = GetVarOrDirectByte(OpCodeParameter.Param1);
            int y = GetVarOrDirectByte(OpCodeParameter.Param2);
            SetResult(FindInventory(x, y));
        }

        void SaveRestoreVerbs()
        {
            int a, b, c, slot, slot2;

            _opCode = ReadByte();

            a = GetVarOrDirectByte(OpCodeParameter.Param1);
            b = GetVarOrDirectByte(OpCodeParameter.Param2);
            c = GetVarOrDirectByte(OpCodeParameter.Param3);

            switch (_opCode)
            {
                case 1:     // SO_SAVE_VERBS
                    while (a <= b)
                    {
                        slot = GetVerbSlot(a, 0);
                        if (slot != 0 && _verbs[slot].SaveId == 0)
                        {
                            _verbs[slot].SaveId = (ushort)c;
                            DrawVerb(slot, 0);
                            VerbMouseOver(0);
                        }
                        a++;
                    }
                    break;

                case 2:     // SO_RESTORE_VERBS
                    while (a <= b)
                    {
                        slot = GetVerbSlot(a, c);
                        if (slot != 0)
                        {
                            slot2 = GetVerbSlot(a, 0);
                            if (slot2 != 0)
                                KillVerb(slot2);
                            slot = GetVerbSlot(a, c);
                            _verbs[slot].SaveId = 0;
                            DrawVerb(slot, 0);
                            VerbMouseOver(0);
                        }
                        a++;
                    }
                    break;

                case 3:     // SO_DELETE_VERBS
                    while (a <= b)
                    {
                        slot = GetVerbSlot(a, c);
                        if (slot != 0)
                            KillVerb(slot);
                        a++;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        void ActorFollowCamera()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var old = _camera.ActorToFollow;
            SetCameraFollows(_actors[actor], false);

            if (_camera.ActorToFollow != old)
                RunInventoryScript(0);

            _camera.MovingToActor = false;
        }

        void EndCutscene()
        {
            if (_slots[_currentScript].CutSceneOverride > 0)    // Only terminate if active
                _slots[_currentScript].CutSceneOverride--;

            var cutSceneData = cutScene.Data.Pop();
            var args = new int[] { cutSceneData.Data };

            _variables[VariableOverride] = 0;

            if (cutSceneData.Pointer != 0 && (_slots[_currentScript].CutSceneOverride > 0))   // Only terminate if active
                _slots[_currentScript].CutSceneOverride--;

            if (_variables[VariableCutSceneEndScript] != 0)
                RunScript((byte)_variables[VariableCutSceneEndScript], false, false, args);
        }

        void StopSound()
        {
            GetVarOrDirectByte(OpCodeParameter.Param1);
            //_sound->stopSound();
        }

        void PutActor()
        {
            Actor a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            short x = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
            short y = (short)GetVarOrDirectWord(OpCodeParameter.Param3);
            a.PutActor(new Point(x, y));
        }

        void AnimateActor()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            int anim = GetVarOrDirectByte(OpCodeParameter.Param2);

            Actor a = _actors[act];
            a.Animate(anim);
        }

        void ActorOps()
        {
            var convertTable = new byte[20] { 1, 0, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 20 };
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var a = _actors[act];
            int i, j;

            while ((_opCode = ReadByte()) != 0xFF)
            {
                _opCode = (byte)((_opCode & 0xE0) | convertTable[(_opCode & 0x1F) - 1]);
                switch (_opCode & 0x1F)
                {
                    case 0:                                     /* dummy case */
                        GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 1:         // SO_COSTUME
                        var cost = (ushort)GetVarOrDirectByte(OpCodeParameter.Param1);
                        a.SetActorCostume(cost);
                        break;

                    case 2:         // SO_STEP_DIST
                        i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        a.SetActorWalkSpeed((uint)i, (uint)j);
                        break;

                    case 3:         // SO_SOUND
                        a.Sound[0] = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 4:         // SO_WALK_ANIMATION
                        a.WalkFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 5:         // SO_TALK_ANIMATION
                        a.TalkStartFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        a.TalkStopFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);
                        break;

                    case 6:         // SO_STAND_ANIMATION
                        a.StandFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 7:         // SO_ANIMATION
                        GetVarOrDirectByte(OpCodeParameter.Param1);
                        GetVarOrDirectByte(OpCodeParameter.Param2);
                        GetVarOrDirectByte(OpCodeParameter.Param3);
                        break;

                    case 8:         // SO_DEFAULT
                        a.Init(0);
                        break;

                    case 9:         // SO_ELEVATION
                        a.Elevation = GetVarOrDirectWord(OpCodeParameter.Param1);
                        break;

                    case 10:        // SO_ANIMATION_DEFAULT
                        a.ResetFrames();
                        break;

                    case 11:        // SO_PALETTE
                        i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        ScummHelper.AssertRange(0, i, 31, "o5_actorOps: palette slot");
                        a.SetPalette(i, (ushort)j);
                        break;

                    case 12:        // SO_TALK_COLOR
                        a.TalkColor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 13:        // SO_ACTOR_NAME
                        a.Name = ReadCharacters();
                        break;

                    case 14:        // SO_INIT_ANIMATION
                        a.InitFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 16:        // SO_ACTOR_WIDTH
                        a.Width = (uint)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 17:        // SO_ACTOR_SCALE
                        i = j = GetVarOrDirectByte(OpCodeParameter.Param1);
                        a.BoxScale = (ushort)i;
                        a.SetScale(i, j);
                        break;

                    case 18:        // SO_NEVER_ZCLIP
                        a.ForceClip = false;
                        break;

                    case 19:        // SO_ALWAYS_ZCLIP
                        a.ForceClip = GetVarOrDirectByte(OpCodeParameter.Param1)>0;
                        break;

                    case 20:        // SO_IGNORE_BOXES
                    case 21:        // SO_FOLLOW_BOXES
                        a.IgnoreBoxes = (_opCode & 1) == 0;
                        a.ForceClip = false;
                        if (a.IsInCurrentRoom)
                            a.PutActor();
                        break;

                    case 22:        // SO_ANIMATION_SPEED
                        a.SetAnimSpeed((byte)GetVarOrDirectByte(OpCodeParameter.Param1));
                        break;

                    case 23:        // SO_SHADOW
                        a.ShadowMode = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        void GetActorFacing()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var a = _actors[act];
            SetResult(ScummHelper.NewDirToOldDir(a.Facing));
        }

        void GetActorElevation()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = _actors[act];
            SetResult(a.Elevation);
        }

        void SetClass()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            int cls;

            while ((_opCode = ReadByte()) != 0xFF)
            {
                cls = GetVarOrDirectWord(OpCodeParameter.Param1);

                // WORKAROUND bug #1668393: Due to a script bug, the wrong opcode is
                // used to test and set the state of various objects (e.g. the inside
                // door (object 465) of the of the Hostel on Mars), when opening the
                // Hostel door from the outside.
                if (cls == 0)
                {
                    // Class '0' means: clean all class data
                    ClassData[obj] = 0;
                    if (obj < _actors.Length)
                    {
                        var a = _actors[obj];
                        a.IgnoreBoxes = false;
                        a.ForceClip = false;
                    }
                }
                else
                {
                    PutClass(obj, cls, (cls & 0x80) != 0);
                }
            }
        }

        void GetActorRoom()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);

            Actor a = _actors[act];
            SetResult(a.Room);
        }

        void GetActorWidth()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = _actors[act];
            SetResult((int)a.Width);
        }

        void PutActorInRoom()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            byte room = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = _actors[act];

            if (a.IsVisible && _currentRoom != room && TalkingActor == a.Number)
            {
                StopTalk();
            }
            a.Room = room;
            if (room == 0)
                a.PutActor(new Point(), 0);
        }

        void BeginOverride()
        {
            if (ReadByte() != 0)
                BeginOverrideCore();
            else
                EndOverrideCore();
        }

        void BeginOverrideCore()
        {
            cutScene.Override.Pointer = _currentPos;
            cutScene.Override.Script = _currentScript;
            
            // Skip the jump instruction following the override instruction
            // (the jump is responsible for "skipping" cutscenes, and the reason
            // why we record the current script position in vm.cutScenePtr).
            ReadByte();
            ReadWord();
        }

        void EndOverrideCore()
        {
            cutScene.Override.Pointer = 0;
            cutScene.Override.Script = 0;

            _variables[VariableOverride] = 0;
        }

        void SetCameraAt()
        {
            short at = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            _camera.Mode = CameraMode.Normal;
            _camera.CurrentPosition.X = at;
            SetCameraAt(at, 0);
            _camera.MovingToActor = false;
        }

        void Lights()
        {
            int a, b, c;

            a = GetVarOrDirectByte(OpCodeParameter.Param1);
            b = ReadByte();
            c = ReadByte();

            if (c == 0)
                _variables[VariableCurrentLights] = a;
            else if (c == 1)
            {
                _flashlight.XStrips = (ushort)a;
                _flashlight.YStrips = (ushort)b;
            }
            _fullRedraw = true;
        }

        void FreezeScripts()
        {
            int scr = GetVarOrDirectByte(OpCodeParameter.Param1);

            if (scr != 0)
                FreezeScripts(scr);
            else
                UnfreezeScripts();
        }

        void UnfreezeScripts()
        {
            for (int i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].Frozen)
                {
                    if (--_slots[i].FreezeCount == 0)
                    {
                        _slots[i].Frozen = false;
                    }
                }
            }

            for (int i = 0; i < _sentence.Length; i++)
            {
                if (_sentence[i].FreezeCount > 0)
                    _sentence[i].FreezeCount--;
            }
        }

        void DoSentence()
        {
            var verb = GetVarOrDirectByte(OpCodeParameter.Param1);
            if (verb == 0xFE)
            {
                _sentenceNum = 0;
                StopScript(_variables[VariableSentenceScript]);
                //TODO: clearClickedStatus();
                return;
            }

            var objectA = GetVarOrDirectWord(OpCodeParameter.Param2);
            var objectB = GetVarOrDirectWord(OpCodeParameter.Param3);
            DoSentence((byte)verb, (ushort)objectA, (ushort)objectB);
        }

        void CutScene()
        {
            var args = GetWordVarArgs();
            BeginCutscene(args);
        }

        void IsScriptRunning()
        {
            GetResult();
            SetResult(IsScriptRunning(GetVarOrDirectByte(OpCodeParameter.Param1)) ? 1 : 0);
        }

        void LoadRoom()
        {
            var room = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
            if (room != _currentRoom)
            {
                StartScene(room);
            }
            _fullRedraw = true;
        }

        void RoomOps()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:     // SO_ROOM_SCROLL
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        if (a < (ScreenWidth / 2))
                            a = (ScreenWidth / 2);
                        if (b < (ScreenWidth / 2))
                            b = (ScreenWidth / 2);
                        if (a > roomData.Header.Width - (ScreenWidth / 2))
                            a = roomData.Header.Width - (ScreenWidth / 2);
                        if (b > roomData.Header.Width - (ScreenWidth / 2))
                            b = roomData.Header.Width - (ScreenWidth / 2);
                        _variables[VariableCameraMinX] = a;
                        _variables[VariableCameraMaxX] = b;
                    }
                    break;

                case 2:     // SO_ROOM_COLOR
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        RoomPalette[b] = (byte)a;
                        _fullRedraw = true;
                    }
                    break;

                case 3:     // SO_ROOM_SCREEN
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        InitScreens(a, b);
                    }
                    break;

                case 4:     // SO_ROOM_PALETTE
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var b = GetVarOrDirectWord(OpCodeParameter.Param2);

                        ScummHelper.AssertRange(0, a, 256, "RoomOps: 4: room color slot");
                        _shadowPalette[b] = (byte)a;
                        SetDirtyColors(b, b);
                    }
                    break;

                case 5:     // SO_ROOM_SHAKE_ON
                    SetShake(true);
                    break;

                case 6:     // SO_ROOM_SHAKE_OFF
                    SetShake(false);
                    break;

                case 7:     // SO_ROOM_SCALE
                    {
                        var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var c = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var d = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        SetScaleSlot(e - 1, 0, b, a, 0, d, c);
                    }
                    break;

                case 10:    // SO_ROOM_FADE
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        if (a != 0)
                        {
                            _switchRoomEffect = (byte)(a & 0xFF);
                            _switchRoomEffect2 = (byte)(a >> 8);
                        }
                        else
                        {
                            FadeIn(_newEffect);
                        }
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        void StartObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var script = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);

            var data = GetWordVarArgs();
            RunObjectScript(obj, script, false, false, data);
        }

        void StopObjectCode()
        {
            if (_slots[_currentScript].Where != WhereIsObject.Global && _slots[_currentScript].Where != WhereIsObject.Local)
            {
                StopObjectScript(_slots[_currentScript].Number);
            }
            else
            {
                _slots[_currentScript].Number = 0;
                _slots[_currentScript].Status = ScriptStatus.Dead;
            }
            _currentScript = 0xFF;
        }

        void StopObjectScript()
        {
            StopObjectScript((ushort)GetVarOrDirectWord(OpCodeParameter.Param1));
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

        void StartScript()
        {
            var op = _opCode;
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var data = GetWordVarArgs();

            // Copy protection was disabled in KIXX XL release (Amiga Disk) and
            // in LucasArts Classic Adventures (PC Disk)
            if (_game.Id == "monkey" && script == 0x98)
            {
                return;
            }

            RunScript((byte)script, (op & 0x20) != 0, (op & 0x40) != 0, data);
        }

        void Move()
        {
            GetResult();
            var result = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(result);
        }

        void IsEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(a == b);
        }

        void IsNotEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(a != b);
        }

        void Add()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = ReadVariable(_resultVarIndex);
            SetResult(a + b);
        }

        void Divide()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(ReadVariable(_resultVarIndex) / a);
        }

        void OldRoomEffect()
        {
            _opCode = ReadByte();
            if ((_opCode & 0x1F) == 3)
            {
                var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                if (a != 0)
                {
                    _switchRoomEffect = (byte)(a & 0xFF);
                    _switchRoomEffect2 = (byte)(a >> 8);
                }
                else
                {
                    FadeIn(_newEffect);
                }
            }
        }

        void PseudoRoom()
        {
            int i = ReadByte(), j;
            while ((j = ReadByte()) != 0)
            {
                if (j >= 0x80)
                {
                    _resourceMapper[j & 0x7F] = (byte)i;
                }
            }
        }

        void Subtract()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(ReadVariable(_resultVarIndex) - a);
        }

        void Increment()
        {
            GetResult();
            SetResult(ReadVariable(_resultVarIndex) + 1);
        }

        void Decrement()
        {
            GetResult();
            SetResult(ReadVariable(_resultVarIndex) - 1);
        }

        void StartSound()
        {
            GetVarOrDirectByte(OpCodeParameter.Param1);

            // TODDO:
            //VAR(VAR_MUSIC_TIMER) = 0;
            //_sound->addSoundToQueue(sound);
        }

        void DrawBox()
        {
            int x, y, x2, y2, color;

            x = GetVarOrDirectWord(OpCodeParameter.Param1);
            y = GetVarOrDirectWord(OpCodeParameter.Param2);

            _opCode = ReadByte();
            x2 = GetVarOrDirectWord(OpCodeParameter.Param1);
            y2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            color = GetVarOrDirectByte(OpCodeParameter.Param3);

            DrawBox(x, y, x2, y2, color);
        }

        void DrawObject()
        {
            byte state = 1;
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            int xpos = GetVarOrDirectWord(OpCodeParameter.Param2);
            int ypos = GetVarOrDirectWord(OpCodeParameter.Param3);

            int idx = GetObjectIndex(obj);
            if (idx == -1)
                return;

            if (xpos != 0xFF)
            {
                _objs[idx].WalkX += (short)((xpos * 8) - _objs[idx].Position.X);
                _objs[idx].WalkY += (short)((ypos * 8) - _objs[idx].Position.Y);
                _objs[idx].Position =new Point((short)(xpos * 8),(short)(ypos * 8));
            }

            AddObjectToDrawQue((byte)idx);

            var x = (ushort)_objs[idx].Position.X;
            var y = (ushort)_objs[idx].Position.Y;
            var w = _objs[idx].Width;
            var h = _objs[idx].Height;

            int i = _objs.Length - 1;
            do
            {
                if (_objs[i].Number != 0 && 
                    _objs[i].Position.X == x && _objs[i].Position.Y == y && 
                    _objs[i].Width == w && _objs[i].Height == h)
                    PutState(_objs[i].Number, 0);
            } while ((--i) != 0);

            PutState(obj, state);
        }

        void IsLess()
        {
            var varNum = ReadWord();
            short a = (short)ReadVariable(varNum);
            short b = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b < a);
        }

        void IsLessEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b <= a);
        }

        void IsGreater()
        {
            var a = ReadVariable(ReadWord());
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b > a);
        }

        void IsGreaterEqual()
        {
            var a = ReadVariable(ReadWord());
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b >= a);
        }

        void Multiply()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(_resultVarIndex);
            SetResult(a * b);
        }

        void Or()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(_resultVarIndex);
            SetResult(a | b);
        }

        void And()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(_resultVarIndex);
            SetResult(a & b);
        }

        void NotEqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);
            JumpRelative(a != 0);
        }

        void EqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);
            JumpRelative(a == 0);
        }

        void JumpRelative()
        {
            JumpRelative(false);
        }

        void StringOperations()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    {
                        // loadstring
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        _strings[id] = ReadCharacters();
                    }
                    break;

                case 2:
                    {
                        // copy string
                        var idA = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var idB = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _strings[idA] = new byte[_strings[idB].Length];
                        Array.Copy(_strings[idB], _strings[idA], _strings[idB].Length);
                    }
                    break;

                case 3:
                    {
                        // Write Character
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var index = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var character = GetVarOrDirectByte(OpCodeParameter.Param3);
                        _strings[id][index] = (byte)character;
                    }
                    break;

                case 4:
                    {
                        // Get string char
                        GetResult();
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var result = _strings[id][b];
                        SetResult(result);
                    }
                    break;

                case 5:
                    {
                        // New String
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var size = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _strings[id] = new byte[size];
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
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
                    // TODO:
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

                default:
                    throw new NotImplementedException();
            }
        }

        void CursorCommand()
        {
            var subOpCode = ReadByte();
            switch (subOpCode)
            {
                case 1:
                    // Cursor On
                    _cursor.State = 1;
                    VerbMouseOver(0);
                    break;

                case 2:
                    // Cursor Off
                    _cursor.State = 0;
                    VerbMouseOver(0);
                    break;

                case 3:
                    // User Input on
                    _userPut = 1;
                    break;

                case 4:
                    // User Input off
                    _userPut = 0;
                    break;

                case 5:
                    // SO_CURSOR_SOFT_ON
                    _cursor.State++;
                    VerbMouseOver(0);
                    break;

                case 6:
                    // SO_CURSOR_SOFT_OFF
                    _cursor.State--;
                    VerbMouseOver(0);
                    break;

                case 7:         // SO_USERPUT_SOFT_ON
                    _userPut++;
                    break;

                case 8:         // SO_USERPUT_SOFT_OFF
                    _userPut--;
                    break;

                case 10:
                    {
                        // SO_CURSOR_IMAGE
                        GetVarOrDirectByte(OpCodeParameter.Param1); // Cursor number
                        GetVarOrDirectByte(OpCodeParameter.Param2); // Charset letter to use
                        // TODO:
                        //redefineBuiltinCursorFromChar(i, j);
                    }
                    break;

                case 11:        // SO_CURSOR_HOTSPOT
                    {
                        GetVarOrDirectByte(OpCodeParameter.Param1);
                        GetVarOrDirectByte(OpCodeParameter.Param2);
                        GetVarOrDirectByte(OpCodeParameter.Param3);
                        // TODO:
                        //redefineBuiltinCursorHotspot(i, j, k);
                    }
                    break;

                case 12:
                    {
                        // SO_CURSOR_SET
                        var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        if (i >= 0 && i <= 3)
                        {
                            _currentCursor = i;
                        }
                        break;
                    }
                case 13:
                    {
                        InitCharset(GetVarOrDirectByte(OpCodeParameter.Param1));
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            _variables[VariableCursorState] = _cursor.State;
            _variables[VariableUserPut] = _userPut;
        }

        void Expression()
        {
            _stack.Clear();
            GetResult();
            int dst = _resultVarIndex;
            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0x1F)
                {
                    case 1:
                        // var
                        _stack.Push(GetVarOrDirectWord(OpCodeParameter.Param1));
                        break;

                    case 2:
                        // add
                        {
                            var i = _stack.Pop();
                            _stack.Push(i + _stack.Pop());
                        }
                        break;

                    case 3:
                        // sub
                        {
                            var i = _stack.Pop();
                            _stack.Push(_stack.Pop() - i);
                        }
                        break;

                    case 4:
                        // mul
                        {
                            var i = _stack.Pop();
                            _stack.Push(i * _stack.Pop());
                        }
                        break;

                    case 5:
                        // div
                        {
                            var i = _stack.Pop();
                            _stack.Push(_stack.Pop() / i);
                        }
                        break;

                    case 6:
                        // normal opcode
                        {
                            _opCode = ReadByte();
                            ExecuteOpCode(_opCode);
                            _stack.Push(_variables[0]);
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            _resultVarIndex = dst;
            SetResult(_stack.Pop());
        }

        void SetVarRange()
        {
            GetResult();
            var a = ReadByte();
            int b;
            do
            {
                if ((_opCode & 0x80) == 0x80)
                    b = ReadWordSigned();
                else
                    b = ReadByte();
                SetResult(b);
                _resultVarIndex++;
            } while ((--a) > 0);
        }

        void StopScript()
        {
            int script;

            script = GetVarOrDirectByte(OpCodeParameter.Param1);

            if (script == 0)
                StopObjectCode();
            else
                StopScript(script);
        }

        void VerbOps()
        {
            var verb = GetVarOrDirectByte(OpCodeParameter.Param1);
            var slot = GetVerbSlot(verb, 0);
            ScummHelper.AssertRange(0, slot, _verbs.Length - 1, "new verb slot");
            var vs = _verbs[slot];
            vs.VerbId = (ushort)verb;

            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0x1F)
                {
                    case 1:     // SO_VERB_IMAGE
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        if (slot != 0)
                        {
                            SetVerbObject(a, slot);
                            vs.Type = VerbType.Image;
                        }
                        break;

                    case 2:     // SO_VERB_NAME
                        vs.Text = ReadCharacters();
                        vs.Type = VerbType.Text;
                        vs.ImgIndex = 0;
                        break;

                    case 3:     // SO_VERB_COLOR
                        vs.Color = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 4:     // SO_VERB_HICOLOR
                        vs.HiColor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 5:     // SO_VERB_AT
                        var left = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var top = GetVarOrDirectWord(OpCodeParameter.Param2);
                        vs.CurRect.Left = left;
                        vs.CurRect.Top = top;
                        break;

                    case 6:
                        // SO_VERB_ON
                        vs.CurMode = 1;
                        break;

                    case 7:
                        // SO_VERB_OFF
                        vs.CurMode = 0;
                        break;

                    case 8:     // SO_VERB_DELETE
                        KillVerb(slot);
                        break;

                    case 9:
                        {
                            // SO_VERB_NEW
                            slot = GetVerbSlot(verb, 0);

                            if (slot == 0)
                            {
                                for (slot = 1; slot < _verbs.Length; slot++)
                                {
                                    if (_verbs[slot].VerbId == 0)
                                        break;
                                }
                            }
                            vs = _verbs[slot];
                            vs.VerbId = (ushort)verb;
                            vs.Color = 2;
                            vs.HiColor = 0;
                            vs.DimColor = 8;
                            vs.Type = VerbType.Text;
                            vs.CharsetNr = _string[0].Default.Charset;
                            vs.CurMode = 0;
                            vs.SaveId = 0;
                            vs.Key = 0;
                            vs.Center = false;
                            vs.ImgIndex = 0;
                            break;
                        }
                    case 16:    // SO_VERB_DIMCOLOR
                        vs.DimColor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 17:    // SO_VERB_DIM
                        vs.CurMode = 2;
                        break;

                    case 18:    // SO_VERB_KEY
                        vs.Key = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 19:    // SO_VERB_CENTER
                        vs.Center = true;
                        break;

                    case 20:    // SO_VERB_NAME_STR
                        var index = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var ptr = _strings[index];
                        if (ptr != null)
                        {
                            vs.Text = ptr;
                        }
                        //if (slot == 0)
                        //    _res->nukeResource(rtVerb, slot);
                        vs.Type = VerbType.Text;
                        vs.ImgIndex = 0;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            // Force redraw of the modified verb slot
            DrawVerb(slot, 0);
            VerbMouseOver(0);
        }

        void GetRandomNumber()
        {
            GetResult();
            var max = GetVarOrDirectByte(OpCodeParameter.Param1);
            var rnd = new Random();
            var value = rnd.Next(max + 1);
            SetResult(value);
        }

        void BreakHere()
        {
            _slots[_currentScript].Offset = (uint)_currentPos;
            _currentScript = 0xFF;
        }

        void SetState()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var state = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);
            PutState(obj, state);
            MarkObjectRectAsDirty(obj);
            if (_bgNeedsRedraw)
                ClearDrawObjectQueue();
        }

        void PickupObject()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            if (obj < 1)
            {
                string msg = string.Format("pickupObjectOld received invalid index %d (script %d)", obj, _slots[_currentScript].Number);
                throw new NotSupportedException(msg);
            }

            if (GetObjectIndex(obj) == -1)
                return;

            // Don't take an object twice
            if (GetWhereIsObject(obj) == WhereIsObject.Inventory)
                return;

            // debug(0, "adding %d from %d to inventoryOld", obj, _currentRoom);
            AddObjectToInventory(obj, _roomResource);
            MarkObjectRectAsDirty(obj);
            PutOwner(obj, (byte)_variables[VariableEgo]);
            PutClass(obj, (int)ObjectClass.Untouchable, true);
            PutState(obj, 1);
            ClearDrawObjectQueue();
            RunInventoryScript(1);
        }

        #endregion OpCodes

        #region Properties

        internal int ScreenStartStrip
        {
            get { return _screenStartStrip; }
        }

        internal int CharsetBufPos
        {
            get { return _charsetBufPos; }
            set { _charsetBufPos = value; }
        }

        internal int HaveMsg
        {
            get { return _haveMsg; }
            set { _haveMsg = value; }
        }

        internal bool EgoPositioned
        {
            get { return _egoPositioned; }
            set { _egoPositioned = value; }
        }

        internal int TalkDelay
        {
            get { return _talkDelay; }
            set { _talkDelay = value; }
        }

        internal ICostumeLoader CostumeLoader
        {
            get { return _costumeLoader; }
        }

        internal ICostumeRenderer CostumeRenderer { get { return _costumeRenderer; } }

        internal Surface TextSurface { get { return _textSurface; } }

        #endregion Properties

        #region Room Methods

        void ResetRoomObjects()
        {
            for (int i = 0; i < roomData.Objects.Count; i++)
            {
                _objs[i + 1].Position = roomData.Objects[i].Position;
                _objs[i + 1].Width = roomData.Objects[i].Width;
                _objs[i + 1].WalkX = roomData.Objects[i].WalkX;
                _objs[i + 1].WalkY = roomData.Objects[i].WalkY;
                _objs[i + 1].State = roomData.Objects[i].State;
                _objs[i + 1].Parent = roomData.Objects[i].Parent;
                _objs[i + 1].ParentState = roomData.Objects[i].ParentState;
                _objs[i + 1].Number = roomData.Objects[i].Number;
                _objs[i + 1].Height = roomData.Objects[i].Height;
                _objs[i + 1].Flags = roomData.Objects[i].Flags;
                _objs[i + 1].FlObjectIndex = roomData.Objects[i].FlObjectIndex;
                _objs[i + 1].ActorDir = roomData.Objects[i].ActorDir;
                _objs[i + 1].Image = roomData.Objects[i].Image;
                _objs[i + 1].Script.Offset = roomData.Objects[i].Script.Offset;
                _objs[i + 1].Script.Data = roomData.Objects[i].Script.Data;
                _objs[i + 1].ScriptOffsets.Clear();
                foreach (var scriptOffset in roomData.Objects[i].ScriptOffsets)
                {
                    _objs[i + 1].ScriptOffsets.Add(scriptOffset.Key, scriptOffset.Value);
                }
                _objs[i + 1].Name = roomData.Objects[i].Name;
            }
            for (int i = roomData.Objects.Count + 1; i < _objs.Length; i++)
            {
                _objs[i].Number = 0;
                _objs[i].Script.Offset = 0;
                _objs[i].ScriptOffsets.Clear();
                _objs[i].Script.Data = new byte[0];
            }
        }

        void ClearRoomObjects()
        {
            for (int i = 0; i < _objs.Length; i++)
            {
                _objs[i].Number = 0;
            }
        }

        void ResetRoomSubBlocks()
        {
            _boxMatrix.Clear();
            _boxMatrix.AddRange(roomData.BoxMatrix);

            for (int i = 0; i < _scaleSlots.Length; i++)
            {
                _scaleSlots [i] = new ScaleSlot();
            }

            if (roomData.Scales != null)
            {
                for (int i = 1; i <= roomData.Scales.Length; i++)
                {
                    var scale = roomData.Scales [i - 1];
                    if (scale.Scale1 != 0 || scale.Y1 != 0 || scale.Scale2 != 0 || scale.Y2 != 0)
                    {
                        SetScaleSlot(i, 0, scale.Y1, scale.Scale1, 0, scale.Y2, scale.Scale2);
                    }
                }
            }

            _boxes = new Box[roomData.Boxes.Count];
            for (int i = 0; i < roomData.Boxes.Count; i++)
            {
                var box = roomData.Boxes[i];
                _boxes[i] = new Box { Flags = box.Flags, Llx = box.Llx, Lly = box.Lly, Lrx = box.Lrx, Lry = box.Lry, Mask = box.Mask, Scale = box.Scale, Ulx = box.Ulx, Uly = box.Uly, Urx = box.Urx, Ury = box.Ury };
            }

            if (roomData.ColorCycle != null)
            {
                Array.Copy(roomData.ColorCycle, _colorCycle, 16);
            }
        }

        #endregion Room Methods

        #region Script Members

        TimeSpan GetTimeToWait()
        {
            int delta = _variables[VariableTimerNext];
            if (delta < 0)  // Ensure we don't get into an endless loop
                delta = 0;  // by not decreasing sleepers.
            var tsDelta = TimeSpan.FromSeconds(delta / 60.0);
            return tsDelta;
        }

        void StartScene(byte room)
        {
            StopTalk();

            FadeOut(_switchRoomEffect2);
            _newEffect = _switchRoomEffect;

            if (_currentScript != 0xFF)
            {
                if (_slots[_currentScript].Where == WhereIsObject.Room || _slots[_currentScript].Where == WhereIsObject.FLObject)
                {
                    //nukeArrays(_currentScript);
                    _currentScript = 0xFF;
                }
                else if (_slots[_currentScript].Where == WhereIsObject.Local)
                {
                    //if (slots[_currentScript].cutsceneOverride && _game.version >= 5)
                    //    error("Script %d stopped with active cutscene/override in exit", slots[_currentScript].number);

                    //nukeArrays(_currentScript);
                    _currentScript = 0xFF;
                }
            }

            RunExitScript();

            KillScriptsAndResources();

            StopCycle(0);

            for (int i = 0; i < _actors.Length; i++)
            {
                _actors[i].Hide();
            }

            for (int i = 0; i < 256; i++)
            {
                RoomPalette[i] = (byte)i;
                if (_shadowPalette != null)
                    _shadowPalette[i] = (byte)i;
            }

            SetDirtyColors(0, 255);

            _variables[VariableRoom] = room;
            _fullRedraw = true;

            _currentRoom = room;

            if (room >= 0x80)
                _roomResource = _resourceMapper[room & 0x7F];
            else
                _roomResource = room;

            _variables[VariableRoomResource] = _roomResource;

            ClearRoomObjects();

            roomData = _scumm.GetRoom(_roomResource);
            if (roomData != null && roomData.HasPalette)
            {
                Array.Copy(roomData.Palette.Colors, _currentPalette.Colors, roomData.Palette.Colors.Length);
            }

            if (_currentRoom == 0)
            {
                return;
            }

            Gdi.TransparentColor = roomData.TransparentColor;
            ResetRoomSubBlocks();
            ResetRoomObjects();
            _drawingObjects.Clear();

            _variables[VariableCameraMinX] = ScreenWidth / 2;
            _variables[VariableCameraMaxX] = roomData.Header.Width - (ScreenWidth / 2);

            _camera.Mode = CameraMode.Normal;
            _camera.CurrentPosition.X = _camera.DestinationPosition.X = (short)(ScreenWidth / 2);
            _camera.CurrentPosition.Y = _camera.DestinationPosition.Y = (short)(ScreenHeight / 2);

            if (_roomResource == 0)
                return;

            Array.Clear(_gfxUsageBits, 0, _gfxUsageBits.Length);

            ShowActors();

            _egoPositioned = false;

            RunEntryScript();

            _doEffect = true;
        }

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

        void RunInventoryScript(int i)
        {
            if (_variables[VariableInventoryScript] != 0)
            {
                RunScript((byte)_variables[VariableInventoryScript], false, false, new int[] { i });
            }
        }

        void RunInputScript(ClickArea clickArea, KeyCode code, int mode)
        {
            var verbScript = _variables[VariableVerbScript];

            if (verbScript != 0)
            {
                RunScript((byte)verbScript, false, false, new int[] { (int)clickArea, (int)code, mode });
            }
        }

        void UpdateVariables()
        {
            _variables[VariableCameraPosX] = _camera.CurrentPosition.X;
            _variables[VariableHaveMessage] = _haveMsg;
        }

        void RunEntryScript()
        {
            if (_variables[VariableEntryScript] != 0)
                RunScript((byte)_variables[VariableEntryScript], false, false, new int[] { });

            if (roomData != null && roomData.EntryScript.Data != null)
            {
                int slot = GetScriptSlotIndex();
                _slots[slot].Status = ScriptStatus.Running;
                _slots[slot].Number = 10002;
                _slots[slot].Where = WhereIsObject.Room;
                _slots[slot].Offset = 0;
                _slots[slot].FreezeResistant = false;
                _slots[slot].Recursive = false;
                _slots[slot].FreezeCount = 0;
                _currentScriptData = roomData.EntryScript.Data;
                _slots[slot].InitializeLocals(new int[0]);
                RunScriptNested((byte)slot);
            }

            if (_variables[VariableEntryScript2] != 0)
                RunScript((byte)_variables[VariableEntryScript2], false, false, new int[] { });
        }

        void RunExitScript()
        {
            if (_variables[VariableExitScript] != 0)
            {
                RunScript((byte)_variables[VariableExitScript], false, false, new int[] { });
            }

            if (roomData != null && roomData.ExitScript.Data != null)
            {
                int slot = GetScriptSlotIndex();
                _slots[slot].Status = ScriptStatus.Running;
                _slots[slot].Number = 10001;
                _slots[slot].Where = WhereIsObject.Room;
                _slots[slot].Offset = 0;
                _slots[slot].FreezeResistant = false;
                _slots[slot].Recursive = false;
                _slots[slot].FreezeCount = 0;
                _currentScriptData = roomData.ExitScript.Data;
                _slots[slot].InitializeLocals(new int[0]);
                RunScriptNested((byte)slot);
            }
        }

        public void RunBootScript(int bootParam = 0)
        {
            RunScript(1, false, false, new int[] { bootParam });
        }

        public void StopScript(int script)
        {
            int i;

            if (script == 0)
                return;

            for (i = 0; i < NumScriptSlot; i++)
            {
                if (script == _slots[i].Number && _slots[i].Status != ScriptStatus.Dead &&
                    (_slots[i].Where == WhereIsObject.Global || _slots[i].Where == WhereIsObject.Local))
                {
                    _slots[i].Number = 0;
                    _slots[i].Status = ScriptStatus.Dead;
                    //nukeArrays(i);
                    if (_currentScript == i)
                        _currentScript = 0xFF;
                }
            }

            for (i = 0; i < _numNestedScripts; ++i)
            {
                if (_nest[i].Number == script &&
                        (_nest[i].Where == WhereIsObject.Global || _nest[i].Where == WhereIsObject.Local))
                {
                    //nukeArrays(vm.nest[i].slot);
                    _nest[i].Number = 0xFF;
                    _nest[i].Slot = 0xFF;
                    _nest[i].Where = WhereIsObject.NotFound;
                }
            }
        }

        public void RunScript(byte scriptNum, bool freezeResistant, bool recursive, int[] data)
        {
            if (scriptNum == 0)
                return;

            if (!recursive)
                StopScript(scriptNum);

            WhereIsObject scriptType;
            if (scriptNum < NumGlobalScripts)
            {
                scriptType = WhereIsObject.Global;
            }
            else
            {
                scriptType = WhereIsObject.Local;
            }

            var slotIndex = GetScriptSlotIndex();
            _slots[slotIndex].Number = scriptNum;
            _slots[slotIndex].Offset = 0;
            _slots[slotIndex].Status = ScriptStatus.Running;
            _slots[slotIndex].FreezeResistant = freezeResistant;
            _slots[slotIndex].Recursive = recursive;
            _slots[slotIndex].Where = scriptType;
            _slots[slotIndex].FreezeCount = 0;

            UpdateScriptData(slotIndex);
            _slots[slotIndex].InitializeLocals(data);
            RunScriptNested(slotIndex);
        }

        void UpdateScriptData(ushort slotIndex)
        {
            var scriptNum = _slots[slotIndex].Number;
            if (_slots[slotIndex].Where == WhereIsObject.Inventory)
            {
                var data = (from o in _invData
                            where o.Number == scriptNum
                            select o.Script.Data).FirstOrDefault();
                _currentScriptData = data;
            }
            else if (scriptNum == 10002)
            {
                _currentScriptData = roomData.EntryScript.Data;
            }
            else if (scriptNum == 10001)
            {
                _currentScriptData = roomData.ExitScript.Data;
            }
            else if (_slots[slotIndex].Where == WhereIsObject.Room)
            {
                var data = (from o in roomData.Objects
                            where o.Number == scriptNum
                            let entry = (byte)_slots[slotIndex].InventoryEntry
                            where o.ScriptOffsets.ContainsKey(entry) || o.ScriptOffsets.ContainsKey(0xFF)
                            select o.Script.Data).FirstOrDefault();
                _currentScriptData = data;
            }
            else if (scriptNum < NumGlobalScripts)
            {
                var data = _scumm.GetScript((byte)scriptNum);
                _currentScriptData = data;
            }
            else if ((scriptNum - NumGlobalScripts) < roomData.LocalScripts.Length)
            {
                _currentScriptData = roomData.LocalScripts[scriptNum - NumGlobalScripts].Data;
            }
            else
            {
                var data = (from o in roomData.Objects
                            where o.Number == scriptNum
                            let entry = (byte)_slots[slotIndex].InventoryEntry
                            where o.ScriptOffsets.ContainsKey(entry) || o.ScriptOffsets.ContainsKey(0xFF)
                            select o.Script.Data).FirstOrDefault();
                _currentScriptData = data;
            }
        }

        void RunScriptNested(byte script)
        {
            if (_currentScript == 0xFF)
            {
                _nest[_numNestedScripts].Number = 0xFF;
                _nest[_numNestedScripts].Where = WhereIsObject.NotFound;
            }
            else
            {
                // Store information about the currently running script
                _slots[_currentScript].Offset = (uint)_currentPos;
                _nest[_numNestedScripts].Number = _slots[_currentScript].Number;
                _nest[_numNestedScripts].Where = _slots[_currentScript].Where;
                _nest[_numNestedScripts].Slot = _currentScript;
            }

            _numNestedScripts++;

            _currentScript = script;
            ResetScriptPointer();
            Run();

            if (_numNestedScripts > 0)
                _numNestedScripts--;

            var nest = _nest[_numNestedScripts];
            if (nest.Number != 0xFF)
            {
                // Try to resume the script which called us, if its status has not changed
                // since it invoked us. In particular, we only resume it if it hasn't been
                // stopped in the meantime, and if it did not already move on.
                var slot = _slots[nest.Slot];
                if (slot.Number == nest.Number && slot.Where == nest.Where &&
                    slot.Status != ScriptStatus.Dead && slot.FreezeCount == 0)
                {
                    _currentScript = nest.Slot;
                    UpdateScriptData(nest.Slot);
                    ResetScriptPointer();
                    return;
                }
            }
            _currentScript = 0xFF;
        }

        void ResetScriptPointer()
        {
            _currentPos = (int)_slots[_currentScript].Offset;
            if (_currentPos < 0)
                throw new NotSupportedException("Invalid offset in reset script pointer");
        }

        byte GetScriptSlotIndex()
        {
            for (byte i = 1; i < NumScriptSlot; i++)
            {
                if (_slots[i].Status == ScriptStatus.Dead)
                    return i;
            }
            return 0xFF;
        }

        void RunAllScripts()
        {
            for (int i = 0; i < NumScriptSlot; i++)
                _slots[i].IsExecuted = false;

            _currentScript = 0xFF;

            for (int i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].Status == ScriptStatus.Running && !_slots[i].IsExecuted)
                {
                    _currentScript = (byte)i;
                    UpdateScriptData((ushort)i);
                    ResetScriptPointer();
                    Run();
                }
            }
        }

        bool IsScriptInUse(int script)
        {
            for (int i = 0; i < NumScriptSlot; i++)
                if (_slots[i].Number == script)
                    return true;
            return false;
        }

        void CheckAndRunSentenceScript()
        {
            int i;
            int sentenceScript;

            sentenceScript = _variables[VariableSentenceScript];

            if (IsScriptInUse(sentenceScript))
            {
                for (i = 0; i < NumScriptSlot; i++)
                    if (_slots[i].Number == sentenceScript && _slots[i].Status != ScriptStatus.Dead && _slots[i].FreezeCount == 0)
                        return;
            }

            if (_sentenceNum == 0 || _sentence[_sentenceNum - 1].FreezeCount != 0)
                return;

            _sentenceNum--;
            Sentence st = _sentence[_sentenceNum];

            if (st.Preposition != 0 && st.ObjectB == st.ObjectA)
                return;

            _currentScript = 0xFF;
            if (sentenceScript != 0)
            {
                var data = new int[3] { st.Verb, st.ObjectA, st.ObjectB };
                RunScript((byte)sentenceScript, false, false, data);
            }
        }

        void RunObjectScript(int obj, byte entry, bool freezeResistant, bool recursive, int[] vars)
        {
            if (obj == 0)
                return;

            if (!recursive)
                StopObjectScript((ushort)obj);

            var where = GetWhereIsObject(obj);

            if (where == WhereIsObject.NotFound)
            {
                Console.Error.WriteLine("warning: Code for object {0} not in room {1}", obj, _roomResource);
                return;
            }

            // Find a free object slot, unless one was specified
            byte slot = GetScriptSlotIndex();

            ObjectData objFound = null;
            if (roomData != null)
            {
                objFound = (from o in roomData.Objects.Concat(_invData)
                            where o != null
                            where o.Number == obj
                            where o.ScriptOffsets.ContainsKey(entry) || o.ScriptOffsets.ContainsKey(0xFF)
                            select o).FirstOrDefault();
            }

            if (objFound == null)
                return;

            _slots[slot].Number = (ushort)obj;
            _slots[slot].InventoryEntry = entry;
            _slots[slot].Offset = (uint)((objFound.ScriptOffsets.ContainsKey(entry) ? objFound.ScriptOffsets[entry] : objFound.ScriptOffsets[0xFF]) - objFound.Script.Offset);
            _slots[slot].Status = ScriptStatus.Running;
            _slots[slot].Where = where;
            _slots[slot].FreezeResistant = freezeResistant;
            _slots[slot].Recursive = recursive;
            _slots[slot].FreezeCount = 0;

            _slots[slot].InitializeLocals(vars);

            // V0 Ensure we don't try and access objects via index inside the script
            //_v0ObjectIndex = false;
            UpdateScriptData(slot);
            RunScriptNested(slot);
        }

        void StopObjectScript(ushort script)
        {
            if (script == 0)
                return;

            for (int i = 0; i < NumScriptSlot; i++)
            {
                if (script == _slots[i].Number && _slots[i].Status != ScriptStatus.Dead &&
                    (_slots[i].Where == WhereIsObject.Room || _slots[i].Where == WhereIsObject.Inventory || _slots[i].Where == WhereIsObject.FLObject))
                {
                    _slots[i].Number = 0;
                    _slots[i].Status = ScriptStatus.Dead;
                    if (_currentScript == i)
                        _currentScript = 0xFF;
                }
            }

            for (int i = 0; i < _numNestedScripts; ++i)
            {
                if (_nest[i].Number == script &&
                    (_nest[i].Where == WhereIsObject.Room || _nest[i].Where == WhereIsObject.Inventory || _nest[i].Where == WhereIsObject.FLObject))
                {
                    _nest[i].Number = 0xFF;
                    _nest[i].Slot = 0xFF;
                    _nest[i].Where = WhereIsObject.NotFound;
                }
            }
        }

        void FreezeScripts(int flag)
        {
            for (int i = 0; i < NumScriptSlot; i++)
            {
                if (_currentScript != i && _slots[i].Status != ScriptStatus.Dead && (!_slots[i].FreezeResistant || flag >= 0x80))
                {
                    _slots[i].Frozen = true;
                    _slots[i].FreezeCount++;
                }
            }

            for (int i = 0; i < _sentence.Length; i++)
                _sentence[i].FreezeCount++;

            if (cutScene.CutSceneScriptIndex != 0xFF)
            {
                _slots[cutScene.CutSceneScriptIndex].Frozen = false;
                _slots[cutScene.CutSceneScriptIndex].FreezeCount = 0;
            }
        }

        bool IsScriptRunning(int script)
        {
            for (int i = 0; i < NumScriptSlot; i++)
            {
                var ss = _slots[i];
                if (ss.Number == script && (ss.Where == WhereIsObject.Global || ss.Where == WhereIsObject.Local) && ss.Status != ScriptStatus.Dead)
                    return true;
            }
            return false;
        }

        void BeginCutscene(int[] args)
        {
            int scr = _currentScript;
            _slots[scr].CutSceneOverride++;

            var cutSceneData = new CutSceneData {
                Data = args.Length > 0 ? args [0] : 0
            };
            cutScene.Data.Push(cutSceneData);

            if (cutScene.Data.Count >= MaxCutsceneNum)
                throw new NotSupportedException("Cutscene stack overflow");

            cutScene.CutSceneScriptIndex = scr;

            if (_variables[VariableCutSceneStartScript] != 0)
                RunScript((byte)_variables[VariableCutSceneStartScript], false, false, args);

            cutScene.CutSceneScriptIndex = 0xFF;
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

        void AbortCutscene()
        {
            if (cutScene.Data.Count == 0)
                return;

            var cutSceneData = cutScene.Data.Peek();

            var offs = cutSceneData.Pointer;
            if (offs != 0)
            {
                _slots[cutSceneData.Script].Offset = (uint)offs;
                _slots[cutSceneData.Script].Status = ScriptStatus.Running;
                _slots[cutSceneData.Script].FreezeCount = 0;

                if (_slots[cutSceneData.Script].CutSceneOverride > 0)
                    _slots[cutSceneData.Script].CutSceneOverride--;

                _variables[VariableOverride] = 1;
                cutSceneData.Pointer = 0;
            }
        }

        void DecreaseScriptDelay(int amount)
        {
            _talkDelay -= amount;
            if (_talkDelay < 0) _talkDelay = 0;
            int i;
            for (i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].Status == ScriptStatus.Paused)
                {
                    _slots[i].Delay -= amount;
                    if (_slots[i].Delay < 0)
                    {
                        _slots[i].Status = ScriptStatus.Running;
                        _slots[i].Delay = 0;
                    }
                }
            }
        }

        public TimeSpan Loop(TimeSpan tsDelta)
        {
            var delta = (int)tsDelta.TotalMilliseconds;
            if (delta == 0) delta = 4;
            _variables[VariableTimer1] += delta;
            _variables[VariableTimer2] += delta;
            _variables[VariableTimer3] += delta;

            if (delta > 15)
                delta = 15;

            DecreaseScriptDelay(delta);

            _talkDelay -= delta;
            if (_talkDelay < 0)
            {
                _talkDelay = 0;
            }

            ProcessInput();

            UpdateVariables();

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
            //if (_saveLoadFlag && _saveLoadFlag != 1) {
            //    goto load_game;
            //}

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
                //PlayActorSounds();
            }

            //HandleSound();

            _camera.LastPosition = _camera.CurrentPosition;

            //_res->increaseExpireCounter();

            AnimateCursor();

            // show or hide mouse
            _gfxManager.ShowCursor(_cursor.State > 0);

            _variables[VariableTimer] = (int)tsDelta.TotalSeconds * 60;
            _variables[VariableTimerTotal] += (int)tsDelta.TotalSeconds * 60;

            return GetTimeToWait();
        }

        #endregion Script Members

        #region Camera Members
        void PanCameraTo(int x)
        {
            _camera.DestinationPosition.X = (short)x;
            _camera.Mode = CameraMode.Panning;
            _camera.MovingToActor = false;
        }

        void SetCameraAt(short posX, short posY)
        {
            if (_camera.Mode != CameraMode.FollowActor || Math.Abs(posX - _camera.CurrentPosition.X) > (ScreenWidth / 2))
            {
                _camera.CurrentPosition.X = posX;
            }
            _camera.DestinationPosition.X = posX;

            if (_camera.CurrentPosition.X < _variables[VariableCameraMinX])
                _camera.CurrentPosition.X = (short)_variables[VariableCameraMinX];

            if (_camera.CurrentPosition.X > _variables[VariableCameraMaxX])
                _camera.CurrentPosition.X = (short)_variables[VariableCameraMaxX];

            if (_variables[VariableScrollScript] != 0)
            {
                _variables[VariableCameraPosX] = _camera.CurrentPosition.X;
                RunScript((byte)_variables[VariableScrollScript], false, false, new int[0]);
            }

            // If the camera moved and text is visible, remove it
            if (_camera.CurrentPosition.X != _camera.LastPosition.X && _charset.HasMask)
                StopTalk();
        }

        void SetCameraFollows(Actor actor, bool setCamera)
        {
            _camera.Mode = CameraMode.FollowActor;
            _camera.ActorToFollow = actor.Number;

            if (!actor.IsInCurrentRoom)
            {
                StartScene(actor.Room);
                _camera.Mode = CameraMode.FollowActor;
                _camera.CurrentPosition.X = actor.Position.X;
                SetCameraAt(_camera.CurrentPosition.X, 0);
            }

            int t = actor.Position.X / 8 - _screenStartStrip;

            if (t < _camera.LeftTrigger || t > _camera.RightTrigger || setCamera)
                SetCameraAt(actor.Position.X, 0);

            for (int i = 1; i < _actors.Length; i++)
            {
                if (_actors[i].IsInCurrentRoom)
                    _actors[i].NeedRedraw = true;
            }
            RunInventoryScript(0);
        }

        void MoveCamera()
        {
            int pos = _camera.CurrentPosition.X;
            int t;
            Actor a = null;
            bool snapToX = /*_snapScroll ||*/ _variables[VariableCameraFastX] != 0;

            _camera.CurrentPosition.X = (short)(_camera.CurrentPosition.X & 0xFFF8);

            if (_camera.CurrentPosition.X < _variables[VariableCameraMinX])
            {
                if (snapToX)
                    _camera.CurrentPosition.X = (short)_variables[VariableCameraMinX];
                else
                    _camera.CurrentPosition.X += 8;

                CameraMoved();
                return;
            }

            if (_camera.CurrentPosition.X > _variables[VariableCameraMaxX])
            {
                if (snapToX)
                    _camera.CurrentPosition.X = (short)_variables[VariableCameraMaxX];
                else
                    _camera.CurrentPosition.X -= 8;

                CameraMoved();
                return;
            }

            if (_camera.Mode == CameraMode.FollowActor)
            {
                a = _actors[_camera.ActorToFollow];

                int actorx = a.Position.X;
                t = actorx / 8 - _screenStartStrip;

                if (t < _camera.LeftTrigger || t > _camera.RightTrigger)
                {
                    if (snapToX)
                    {
                        if (t > 40 - 5)
                            _camera.DestinationPosition.X = (short)(actorx + 80);
                        if (t < 5)
                            _camera.DestinationPosition.X = (short)(actorx - 80);
                    }
                    else
                        _camera.MovingToActor = true;
                }
            }

            if (_camera.MovingToActor)
            {
                a = _actors[_camera.ActorToFollow];
                _camera.DestinationPosition.X = a.Position.X;
            }

            if (_camera.DestinationPosition.X < _variables[VariableCameraMinX])
                _camera.DestinationPosition.X = (short)_variables[VariableCameraMinX];

            if (_camera.DestinationPosition.X > _variables[VariableCameraMaxX])
                _camera.DestinationPosition.X = (short)_variables[VariableCameraMaxX];

            if (snapToX)
            {
                _camera.CurrentPosition.X = _camera.DestinationPosition.X;
            }
            else
            {
                if (_camera.CurrentPosition.X < _camera.DestinationPosition.X)
                    _camera.CurrentPosition.X += 8;
                if (_camera.CurrentPosition.X > _camera.DestinationPosition.X)
                    _camera.CurrentPosition.X -= 8;
            }

            /* Actor 'a' is set a bit above */
            if (_camera.MovingToActor && (_camera.CurrentPosition.X / 8) == (a.Position.X / 8))
            {
                _camera.MovingToActor = false;
            }

            CameraMoved();

            if (_variables[VariableScrollScript] != 0 && pos != _camera.CurrentPosition.X)
            {
                _variables[VariableCameraPosX] = _camera.CurrentPosition.X;
                RunScript((byte)_variables[VariableScrollScript], false, false, new int[0]);
            }
        }

        void CameraMoved()
        {
            int screenLeft;

            if (_camera.CurrentPosition.X < (ScreenWidth / 2))
            {
                _camera.CurrentPosition.X = (short)(ScreenWidth / 2);
            }
            else if (_camera.CurrentPosition.X > (CurrentRoomData.Header.Width - (ScreenWidth / 2)))
            {
                _camera.CurrentPosition.X = (short)(CurrentRoomData.Header.Width - (ScreenWidth / 2));
            }

            _screenStartStrip = _camera.CurrentPosition.X / 8 - Gdi.NumStrips / 2;
            _screenEndStrip = _screenStartStrip + Gdi.NumStrips - 1;

            ScreenTop = _camera.CurrentPosition.Y - (ScreenHeight / 2);
            screenLeft = _screenStartStrip * 8;

            _mainVirtScreen.XStart = (ushort)screenLeft;
        }
        #endregion

        #region Message Methods
        byte[] ReadCharacters()
        {
            var sb = new List<byte>();
            var character = ReadByte();
            while (character != 0)
            {
                sb.Add(character);
                if (character == 0xFF)
                {
                    character = ReadByte();
                    sb.Add(character);
                    if (character != 1 && character != 2 && character != 3 && character != 8)
                    {
                        character = ReadByte();
                        sb.Add(character);
                        character = ReadByte();
                        sb.Add(character);
                    }
                }
                character = ReadByte();
            }
            return sb.ToArray();
        }

        void Print()
        {
            _actorToPrintStrFor = GetVarOrDirectByte(OpCodeParameter.Param1);
            DecodeParseString();
        }

        void DecodeParseString()
        {
            int textSlot;
            switch (_actorToPrintStrFor)
            {
                case 252:
                    textSlot = 3;
                    break;

                    case 253:
                    textSlot = 2;
                    break;

                    case 254:
                    textSlot = 1;
                    break;

                    default:
                    textSlot = 0;
                    break;
            }

            _string[textSlot].LoadDefault();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0xF)
                {
                    case 0:     // SO_AT
                        _string[textSlot].XPos = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
                        _string[textSlot].YPos = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
                        _string[textSlot].Overhead = false;
                        break;

                        case 1:     // SO_COLOR
                        _string[textSlot].Color = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                        case 2:     // SO_CLIPPED
                        _string[textSlot].Right = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
                        break;

                        case 4:     // SO_CENTER
                        _string[textSlot].Center = true;
                        _string[textSlot].Overhead = false;
                        break;

                        case 6:     // SO_LEFT
                    {
                        _string[textSlot].Center = false;
                        _string[textSlot].Overhead = false;
                    }
                        break;

                        case 7:     // SO_OVERHEAD
                        _string[textSlot].Overhead = true;
                        break;

                        case 15:
                    {   // SO_TEXTSTRING
                        var tmp = ReadCharacters();
                        PrintString(textSlot, tmp);
                    }
                        return;

                        default:
                        throw new NotImplementedException();
                }
            }

            _string[textSlot].SaveDefault();
        }

        void PrintString(int textSlot, byte[] msg)
        {
            switch (textSlot)
            {
                case 0:
                    ActorTalk(msg);
                    break;

                    case 1:
                    DrawString(1, msg);
                    break;
                    //case 2:
                    //    debugMessage(msg);
                    //    break;
                    //case 3:
                    //    showMessageDialog(msg);
                    //    break;
                    default:
                    throw new NotImplementedException();
            }
        }

        static IList<char> ConvertMessage(IList<char> msg, int i, string text)
        {
            var src = msg.ToArray();
            var dst = new char[msg.Count - 3 + text.Length];
            Array.Copy(src, dst, i - 1);
            Array.Copy(text.ToArray(), 0, dst, i - 1, text.Length);
            Array.Copy(src, i + 2, dst, i - 1 + text.Length, src.Length - i - 3);
            msg = dst;
            return msg;
        }

        int ConvertMessageToString(byte[] src, byte[] dst, int dstPos)
        {
            uint num = 0;
            int val;
            byte chr;
            int dstPosBegin = dstPos;

            while (num < src.Length)
            {
                chr = src[num++];
                if (chr == 0)
                    break;

                if (chr == 0xFF)
                {
                    chr = src[num++];

                    if (chr == 1 || chr == 2 || chr == 3 || chr == 8)
                    {
                        // Simply copy these special codes
                        dst[dstPos++] = 0xFF;
                        dst[dstPos++] = chr;
                    }
                    else
                    {
                        val = src[num] | ((int)src[num + 1] << 8);
                        switch (chr)
                        {
                            case 4:
                                dstPos += ConvertIntMessage(dst, dstPos, val);
                                break;

                            case 5:
                                dstPos += ConvertVerbMessage(dst, dstPos, val);
                                break;

                            case 6:
                                dstPos += ConvertNameMessage(dst, dstPos, val);
                                break;

                            case 7:
                                dstPos += ConvertStringMessage(dst, dstPos, val);
                                break;

                            case 9:
                            case 10:
                            case 12:
                            case 13:
                            case 14:
                                // Simply copy these special codes
                                dst[dstPos++] = 0xFF;
                                dst[dstPos++] = chr;
                                dst[dstPos++] = src[num + 0];
                                dst[dstPos++] = src[num + 1];
                                break;

                            default:
                                throw new NotSupportedException(string.Format("convertMessageToString(): string escape sequence %d unknown", chr));
                        }
                        num += 2;
                    }
                }
                else
                {
                    if (chr != '@')
                    {
                        dst[dstPos++] = chr;
                    }
                }
            }

            dst[dstPos] = 0;

            return dstPos - dstPosBegin;
        }

        int ConvertNameMessage(byte[] dst, int dstPos, int var)
        {
            var num = ReadVariable(var);
            if (num != 0)
            {
                var ptr = GetObjectOrActorName(num);
                if (ptr != null)
                {
                    return ConvertMessageToString(ptr, dst, dstPos);
                }
            }
            return 0;
        }

        int ConvertVerbMessage(byte[] dst, int dstPos, int var)
        {
            var num = ReadVariable(var);
            if (num != 0)
            {
                for (int k = 1; k < _verbs.Length; k++)
                {
                    if (num == _verbs[k].VerbId && _verbs[k].Type == VerbType.Text && (_verbs[k].SaveId == 0))
                    {
                        return ConvertMessageToString(_verbs[k].Text, dst, dstPos);
                    }
                }
            }
            return 0;
        }

        int ConvertIntMessage(Array dst, int dstPos, int var)
        {
            var num = ReadVariable(var);
            var src = Encoding.ASCII.GetBytes(num.ToString());
            Array.Copy(src, 0, dst, dstPos, src.Length);
            return src.Length;
        }

        int ConvertStringMessage(byte[] dst, int dstPos, int var)
        {
            if (var != 0)
            {
                var ptr = _strings[var];
                if (ptr != null)
                {
                    return ConvertMessageToString(ptr, dst, dstPos);
                }
            }
            return 0;
        }

        #endregion Message Methods

        #region Box Members

        void CreateBoxMatrix()
        {
            // The total number of boxes
            int num = GetNumBoxes();

            // calculate shortest paths
            var itineraryMatrix = CalcItineraryMatrix(num);

            // "Compress" the distance matrix into the box matrix format used
            // by the engine. The format is like this:
            // For each box (from 0 to num) there is first a byte with value 0xFF,
            // followed by an arbitrary number of byte triples; the end is marked
            // again by the lead 0xFF for the next "row". The meaning of the
            // byte triples is as follows: the first two bytes define a range
            // of box numbers (e.g. 7-11), while the third byte defines an
            // itineray box. Assuming we are in the 5th "row" and encounter
            // the triplet 7,11,15: this means to get from box 5 to any of
            // the boxes 7,8,9,10,11 the shortest way is to go via box 15.
            // See also getNextBox.

            var boxMatrix = new List<byte>();

            for (byte i = 0; i < num; i++)
            {
                boxMatrix.Add(0xFF);
                for (byte j = 0; j < num; j++)
                {
                    byte itinerary = itineraryMatrix[i, j];
                    if (itinerary != Actor.InvalidBox)
                    {
                        boxMatrix.Add(j);
                        while (j < num - 1 && itinerary == itineraryMatrix[i, (j + 1)])
                            j++;
                        boxMatrix.Add(j);
                        boxMatrix.Add(itinerary);
                    }
                }
            }
            boxMatrix.Add(0xFF);

            _boxMatrix.Clear();
            _boxMatrix.AddRange(boxMatrix);
        }

        internal BoxFlags GetBoxFlags(byte boxNum)
        {
            var box = GetBoxBase(boxNum);
            if (box == null)
                return 0;
            return box.Flags;
        }

        internal byte GetBoxMask(byte boxNum)
        {
            Box box = GetBoxBase(boxNum);
            if (box == null)
                return 0;
            return box.Mask;
        }

        internal int GetNumBoxes()
        {
            return _boxes.Length;
        }

        internal BoxCoords GetBoxCoordinates(int boxnum)
        {
            var bp = GetBoxBase(boxnum);
            var box = new BoxCoords();

            box.Ul.X = bp.Ulx;
            box.Ul.Y = bp.Uly;
            box.Ur.X = bp.Urx;
            box.Ur.Y = bp.Ury;

            box.Ll.X = bp.Llx;
            box.Ll.Y = bp.Lly;
            box.Lr.X = bp.Lrx;
            box.Lr.Y = bp.Lry;

            return box;
        }

        Box GetBoxBase(int boxnum)
        {
            if (boxnum == 255)
                return null;

            // As a workaround, we simply use the last box if the last+1 box is requested.
            // Note that this may cause different behavior than the original game
            // engine exhibited! To faithfully reproduce the behavior of the original
            // engine, we would have to know the data coming *after* the walkbox table.
            if (_boxes.Length == boxnum)
                boxnum--;
            return _boxes[boxnum];
        }

        /// <summary>
        /// Compute if there is a way that connects box 'from' with box 'to'.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>
        /// The number of a box adjacent to 'from' that is the next on the
        /// way to 'to' (this can be 'to' itself or a third box).
        /// If there is no connection -1 is return.
        /// </returns>
        internal int GetNextBox(byte from, byte to)
        {
            byte i;
            int numOfBoxes = GetNumBoxes();
            int dest = -1;

            if (from == to)
                return to;

            if (to == Actor.InvalidBox)
                return -1;

            if (from == Actor.InvalidBox)
                return to;

            if (from >= numOfBoxes) throw new ArgumentOutOfRangeException("from");
            if (to >= numOfBoxes) throw new ArgumentOutOfRangeException("to");

            var boxm = _boxMatrix;

            // WORKAROUND #1: It seems that in some cases, the box matrix is corrupt
            // (more precisely, is too short) in the datafiles already. In
            // particular this seems to be the case in room 46 of Indy3 EGA (see
            // also bug #770690). This didn't cause problems in the original
            // engine, because there, the memory layout is different. After the
            // walkbox would follow the rest of the room file, thus the program
            // always behaved the same (and by chance, correct). Not so for us,
            // since random data may follow after the resource in ScummVM.
            //
            // As a workaround, we add a check for the end of the box matrix
            // resource, and abort the search once we reach the end.

            int boxmIndex = _boxMatrix[0] == 0xFF ? 1 : 0;
            // Skip up to the matrix data for box 'from'
            for (i = 0; i < from && boxmIndex < boxm.Count; i++)
            {
                while (boxmIndex < boxm.Count && boxm[boxmIndex] != 0xFF)
                    boxmIndex += 3;
                boxmIndex++;
            }

            // Now search for the entry for box 'to'
            while (boxmIndex < boxm.Count && boxm[boxmIndex] != 0xFF)
            {
                if (boxm[boxmIndex] <= to && to <= boxm[boxmIndex + 1])
                    dest = (sbyte)boxm[boxmIndex + 2];
                boxmIndex += 3;
            }

            //if (boxm >= boxm.Count)
            //    debug(0, "The box matrix apparently is truncated (room %d)", _roomResource);

            return dest;
        }

        internal bool CheckXYInBoxBounds(int boxnum, Point p)
        {
            // Since this method is called by many other methods that take params
            // from e.g. script opcodes, but do not validate the boxnum, we
            // make a check here to filter out invalid boxes.
            // See also bug #1599113.
            if (boxnum < 0 || boxnum == Actor.InvalidBox)
                return false;

            var box = GetBoxCoordinates(boxnum);
           
            // Quick check: If the x (resp. y) coordinate of the point is
            // strictly smaller (bigger) than the x (y) coordinates of all
            // corners of the quadrangle, then it certainly is *not* contained
            // inside the quadrangle.
            if (p.X < box.Ul.X && p.X < box.Ur.X && p.X < box.Lr.X && p.X < box.Ll.X)
                return false;

            if (p.X > box.Ul.X && p.X > box.Ur.X && p.X > box.Lr.X && p.X > box.Ll.X)
                return false;

            if (p.Y < box.Ul.Y && p.Y < box.Ur.Y && p.Y < box.Lr.Y && p.Y < box.Ll.Y)
                return false;

            if (p.Y > box.Ul.Y && p.Y > box.Ur.Y && p.Y > box.Lr.Y && p.Y > box.Ll.Y)
                return false;

            // Corner case: If the box is a simple line segment, we consider the
            // point to be contained "in" (or rather, lying on) the line if it
            // is very close to its projection to the line segment.
            if ((box.Ul == box.Ur && box.Lr == box.Ll) ||
                (box.Ul == box.Ll && box.Ur == box.Lr))
            {
                Point tmp;
                tmp = ScummMath.ClosestPtOnLine(box.Ul, box.Lr, p);
                if (p.SquareDistance(tmp) <= 4)
                    return true;
            }

            // Finally, fall back to the classic algorithm to compute containment
            // in a convex polygon: For each (oriented) side of the polygon
            // (quadrangle in this case), compute whether p is "left" or "right"
            // from it.

            if (!ScummMath.CompareSlope(box.Ul, box.Ur, p))
                return false;

            if (!ScummMath.CompareSlope(box.Ur, box.Lr, p))
                return false;

            if (!ScummMath.CompareSlope(box.Lr, box.Ll, p))
                return false;

            if (!ScummMath.CompareSlope(box.Ll, box.Ul, p))
                return false;

            return true;
        }

        byte[,] CalcItineraryMatrix(int num)
        {
            const byte boxSize = 64;

            // Allocate the adjacent & itinerary matrices
            var itineraryMatrix = new byte[boxSize, boxSize];
            var adjacentMatrix = new byte[boxSize, boxSize];

            // Initialize the adjacent matrix: each box has distance 0 to itself,
            // and distance 1 to its direct neighbors. Initially, it has distance
            // 255 (= infinity) to all other boxes.
            for (byte i = 0; i < num; i++)
            {
                for (byte j = 0; j < num; j++)
                {
                    if (i == j)
                    {
                        adjacentMatrix[i, j] = 0;
                        itineraryMatrix[i, j] = j;
                    }
                    else if (AreBoxesNeighbors(i, j))
                    {
                        adjacentMatrix[i, j] = 1;
                        itineraryMatrix[i, j] = j;
                    }
                    else
                    {
                        adjacentMatrix[i, j] = 255;
                        itineraryMatrix[i, j] = Actor.InvalidBox;
                    }
                }
            }

            // Compute the shortest routes between boxes via Kleene's algorithm.
            // The original code used some kind of mangled Dijkstra's algorithm;
            // while that might in theory be slightly faster, it was
            // a) extremly obfuscated
            // b) incorrect: it didn't always find the shortest paths
            // c) not any faster in reality for our sparse & small adjacent matrices
            for (byte k = 0; k < num; k++)
            {
                for (byte i = 0; i < num; i++)
                {
                    for (byte j = 0; j < num; j++)
                    {
                        if (i == j)
                            continue;
                        byte distIK = adjacentMatrix[i, k];
                        byte distKJ = adjacentMatrix[k, j];
                        if (adjacentMatrix[i, j] > distIK + distKJ)
                        {
                            adjacentMatrix[i, j] = (byte)(distIK + distKJ);
                            itineraryMatrix[i, j] = itineraryMatrix[i, k];
                        }
                    }
                }
            }

            return itineraryMatrix;
        }

        /// <summary>
        /// Check if two boxes are neighbors.
        /// </summary>
        /// <param name="box1nr"></param>
        /// <param name="box2nr"></param>
        /// <returns></returns>
        bool AreBoxesNeighbors(byte box1nr, byte box2nr)
        {
            Point tmp;

            if (GetBoxFlags(box1nr).HasFlag(BoxFlags.Invisible) || GetBoxFlags(box2nr).HasFlag(BoxFlags.Invisible))
                return false;

            //System.Diagnostics.Debug.Assert(_game.version >= 3);
            var box2 = GetBoxCoordinates(box1nr);
            var box = GetBoxCoordinates(box2nr);

            // Roughly, the idea of this algorithm is to search for sies of the given
            // boxes that touch each other.
            // In order to keep te code simple, we only match the upper sides;
            // then, we "rotate" the box coordinates four times each, for a total
            // of 16 comparisions.
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    // Are the "upper" sides of the boxes on a single vertical line
                    // (i.e. all share one x value) ?
                    if (box2.Ur.X == box2.Ul.X && box.Ul.X == box2.Ul.X && box.Ur.X == box2.Ul.X)
                    {
                        bool swappedBox2 = false, swappedBox1 = false;
                        if (box2.Ur.Y < box2.Ul.Y)
                        {
                            swappedBox2 = true;
                            ScummHelper.Swap(ref box2.Ur.Y, ref box2.Ul.Y);
                        }
                        if (box.Ur.Y < box.Ul.Y)
                        {
                            swappedBox1 = true;
                            ScummHelper.Swap(ref box.Ur.Y, ref box.Ul.Y);
                        }
                        if (box.Ur.Y < box2.Ul.Y ||
                            box.Ul.Y > box2.Ur.Y ||
                            ((box.Ul.Y == box2.Ur.Y ||
                          box.Ur.Y == box2.Ul.Y) && box2.Ur.Y != box2.Ul.Y && box.Ul.Y != box.Ur.Y))
                        {
                        }
                        else
                        {
                            return true;
                        }

                        // Swap back if necessary
                        if (swappedBox2)
                        {
                            ScummHelper.Swap(ref box2.Ur.Y, ref box2.Ul.Y);
                        }
                        if (swappedBox1)
                        {
                            ScummHelper.Swap(ref box.Ur.Y, ref box.Ul.Y);
                        }
                    }

                    // Are the "upper" sides of the boxes on a single horizontal line
                    // (i.e. all share one y value) ?
                    if (box2.Ur.Y == box2.Ul.Y && box.Ul.Y == box2.Ul.Y && box.Ur.Y == box2.Ul.Y)
                    {
                        var swappedBox2 = false;
                        var swappedBox1 = false;
                        if (box2.Ur.X < box2.Ul.X)
                        {
                            swappedBox2 = true;
                            ScummHelper.Swap(ref box2.Ur.X, ref box2.Ul.X);
                        }
                        if (box.Ur.X < box.Ul.X)
                        {
                            swappedBox1 = true;
                            ScummHelper.Swap(ref box.Ur.X, ref box.Ul.X);
                        }
                        if (box.Ur.X < box2.Ul.X ||
                            box.Ul.X > box2.Ur.X ||
                            ((box.Ul.X == box2.Ur.X ||
                          box.Ur.X == box2.Ul.X) && box2.Ur.X != box2.Ul.X && box.Ul.X != box.Ur.X))
                        {

                        }
                        else
                        {
                            return true;
                        }

                        // Swap back if necessary
                        if (swappedBox2)
                        {
                            ScummHelper.Swap(ref box2.Ur.X, ref box2.Ul.X);
                        }
                        if (swappedBox1)
                        {
                            ScummHelper.Swap(ref box.Ur.X, ref box.Ul.X);
                        }
                    }

                    // "Rotate" the box coordinates
                    tmp = box2.Ul;
                    box2.Ul = box2.Ur;
                    box2.Ur = box2.Lr;
                    box2.Lr = box2.Ll;
                    box2.Ll = tmp;
                }

                // "Rotate" the box coordinates
                tmp = box.Ul;
                box.Ul = box.Ur;
                box.Ur = box.Lr;
                box.Lr = box.Ll;
                box.Ll = tmp;
            }

            return false;
        }

        void SetBoxScale(int box, int scale)
        {
            var b = GetBoxBase(box);
            b.Scale = (ushort)scale;
        }

        void SetBoxFlags(int box, int val)
        {
            var b = GetBoxBase(box);
            if (b == null)
                return;
            b.Flags = (BoxFlags)val;
        }

        #endregion Box Members

        #region Cursor Members

        ushort[][] _cursorImages = new ushort[4][];
        byte[] _cursorHotspots = new byte[2 * 4];

        static readonly ushort[][] default_cursor_images = new ushort[4][] {
        /* cross-hair */
        new ushort[16]{
            0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0000, 0x7e3f,
            0x0000, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0000 },
        /* hourglass */
        new ushort[16]{
            0x0000, 0x7ffe, 0x6006, 0x300c, 0x1818, 0x0c30, 0x0660, 0x03c0,
            0x0660, 0x0c30, 0x1998, 0x33cc, 0x67e6, 0x7ffe, 0x0000, 0x0000 },
        /* arrow */
        new ushort[16]{
            0x0000, 0x4000, 0x6000, 0x7000, 0x7800, 0x7c00, 0x7e00, 0x7f00,
            0x7f80, 0x78c0, 0x7c00, 0x4600, 0x0600, 0x0300, 0x0300, 0x0180 },
        /* hand */
        new ushort[16]{
            0x1e00, 0x1200, 0x1200, 0x1200, 0x1200, 0x13ff, 0x1249, 0x1249,
            0xf249, 0x9001, 0x9001, 0x9001, 0x8001, 0x8001, 0x8001, 0xffff }
        };

        static readonly byte[] default_cursor_hotspots = new byte[] {
            8, 7,
            8, 7,
            1, 1,
            5, 0,
            8, 7, //zak256
        };

        void AnimateCursor()
        {
            if (_cursor.Animate)
            {
                if ((_cursor.AnimateIndex & 0x1) == 0)
                {
                    SetBuiltinCursor((_cursor.AnimateIndex >> 1) & 3);
                }
                _cursor.AnimateIndex++;
            }
        }

        void SetBuiltinCursor(int idx)
        {
            var src = _cursorImages[_currentCursor];
            cursorColor = defaultCursorColors[idx];

            _cursor.HotspotX = _cursorHotspots[2 * _currentCursor] * _textSurfaceMultiplier;
            _cursor.HotspotY = _cursorHotspots[2 * _currentCursor + 1] * _textSurfaceMultiplier;
            _cursor.Width = 16 * _textSurfaceMultiplier;
            _cursor.Height = 16 * _textSurfaceMultiplier;

            var pixels = new byte[_cursor.Width * _cursor.Height];

            int offset = 0;
            for (int w = 0; w < _cursor.Width; w++)
            {
                for (int h = 0; h < _cursor.Height; h++)
                {
                    if ((src[w] & (1 << h)) != 0)
                    {
                        pixels[offset] = cursorColor;
                    }
                    offset++;
                }
            }

            _gfxManager.SetCursor(pixels, _cursor.Width, _cursor.Height, _cursor.HotspotX, _cursor.HotspotY);
        }

        void ResetCursors()
        {
            for (int i = 0; i < 4; i++)
            {
                _cursorImages[i] = new ushort[16];
                Array.Copy(default_cursor_images[i], _cursorImages[i], 16);
            }
            Array.Copy(default_cursor_hotspots, _cursorHotspots, 8);
        }

        #endregion Cursor Members

        #region Scale Members

        public int GetBoxScale(byte boxNum)
        {
            var box = GetBoxBase(boxNum);
            if (box == null)
                return 255;
            return box.Scale;
        }

        public int GetScale(int boxNum, short x, short y)
        {
            var box = GetBoxBase(boxNum);
            if (box == null) return 255;

            int scale = (int)box.Scale;
            int slot = 0;
            if ((scale & 0x8000) != 0)
                slot = (scale & 0x7FFF) + 1;

            // Was a scale slot specified? If so, we compute the effective scale
            // from it, ignoring the box scale.
            if (slot != 0)
                scale = GetScaleFromSlot(slot, x, y);

            return scale;
        }

        public int GetScaleFromSlot(int slot, int x, int y)
        {
            int scale;
            int scaleX;
            int scaleY = 0;
            var s = _scaleSlots[slot - 1];

            //if (s.y1 == s.y2 && s.x1 == s.x2)
            //    throw new NotSupportedException(string.Format("Invalid scale slot {0}", slot));

            if (s.Y1 != s.Y2)
            {
                if (y < 0)
                    y = 0;

                scaleY = (s.Scale2 - s.Scale1) * (y - s.Y1) / (s.Y2 - s.Y1) + s.Scale1;
            }
            if (s.X1 == s.X2)
            {
                scale = scaleY;
            }
            else
            {
                scaleX = (s.Scale2 - s.Scale1) * (x - s.X1) / (s.X2 - s.X1) + s.Scale1;

                if (s.Y1 == s.Y2)
                {
                    scale = scaleX;
                }
                else
                {
                    scale = (scaleX + scaleY) / 2;
                }
            }

            // Clip the scale to range 1-255
            if (scale < 1)
                scale = 1;
            else if (scale > 255)
                scale = 255;

            return scale;
        }

        void SetScaleSlot(int slot, int x1, int y1, int scale1, int x2, int y2, int scale2)
        {
            if (slot < 1) throw new ArgumentOutOfRangeException("slot", slot, "Invalid scale slot");
            if (slot > _scaleSlots.Length) throw new ArgumentOutOfRangeException("slot", slot, "Invalid scale slot");
            _scaleSlots[slot - 1] = new ScaleSlot { X1 = x1, X2 = x2, Y1 = y1, Y2 = y2, Scale1 = scale1, Scale2 = scale2 };
        }

        #endregion Scale Members

        #region Effects Methods
        void UnkScreenEffect6()
        {
            DissolveEffect(8, 4);
        }

        void FadeIn(byte effect)
        {
            if (_disableFadeInEffect)
            {
                // fadeIn() calls can be disabled in TheDig after a SMUSH movie
                // has been played. Like the original interpreter, we introduce
                // an extra flag to handle 
                _disableFadeInEffect = false;
                _doEffect = false;
                _screenEffectFlag = true;
                return;
            }

            UpdatePalette();

            switch (effect)
            {
                case 0:
                    // seems to do nothing
                    break;

                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    // Some of the transition effects won't work properly unless
                    // the screen is marked as clean first. At first I thought I
                    // could safely do this every time fadeIn() was called, but
                    // that broke the FOA intro. Probably other things as well.
                    //
                    // Hopefully it's safe to do it at this point, at least.
                    MainVirtScreen.SetDirtyRange(0, 0);
                    //TransitionEffect(effect - 1);
                    //throw new NotImplementedException();
                    break;

                    case 128:
                    UnkScreenEffect6();
                    break;

                    case 129:
                    break;

                    case 130:
                    case 131:
                    case 132:
                    case 133:
                    //scrollEffect(133 - effect);
                    throw new NotImplementedException();
                    //break;

                    case 134:
                    DissolveEffect(1, 1);
                    break;

                    case 135:
                    DissolveEffect(1, MainVirtScreen.Height);
                    break;

                    default:
                    throw new NotImplementedException(string.Format("Unknown screen effect {0}", effect));
            }
            _screenEffectFlag = true;
        }

        /// <summary>
        /// Update width*height areas of the screen, in random order, until the whole
        /// screen has been updated. For instance:
        ///
        /// dissolveEffect(1, 1) produces a pixel-by-pixel dissolve
        /// dissolveEffect(8, 8) produces a square-by-square dissolve
        /// dissolveEffect(virtsrc[0].width, 1) produces a line-by-line dissolve
        /// </summary>
        /// <param name='width'>
        /// Width.
        /// </param>
        /// <param name='height'>
        /// Height.
        /// </param>
        void DissolveEffect(int width, int height)
        {
            var vs = MainVirtScreen;
            int[] offsets;
            int blits_before_refresh, blits;
            int x, y;
            int w, h;
            int i;
            var rnd = new Random();

            // There's probably some less memory-hungry way of doing  But
            // since we're only dealing with relatively small images, it shouldn't
            // be too bad.

            w = vs.Width / width;
            h = vs.Height / height;

            // When used correctly, vs->width % width and vs->height % height
            // should both be zero, but just to be safe...

            if ((vs.Width % width)!=0)
                w++;

            if ((vs.Height % height)!=0)
                h++;

            offsets = new int[w * h];

            // Create a permutation of offsets into the frame buffer

            if (width == 1 && height == 1)
            {
                // Optimized case for pixel-by-pixel dissolve

                for (i = 0; i < vs.Width * vs.Height; i++)
                    offsets [i] = i;

                for (i = 1; i < w * h; i++)
                {
                    int j;

                    j = rnd.Next(i);
                    offsets [i] = offsets [j];
                    offsets [j] = i;
                }
            } else
            {
                int[] offsets2;

                for (i = 0, x = 0; x < vs.Width; x += width)
                    for (y = 0; y < vs.Height; y += height)
                        offsets [i++] = y * vs.Pitch + x;

                offsets2 = new int[w * h];

                Array.Copy(offsets,offsets2,offsets.Length);

                for (i = 1; i < w * h; i++)
                {
                    int j;

                    j = rnd.Next(i);
                    offsets [i] = offsets [j];
                    offsets [j] = offsets2 [i];
                }
            }

            // Blit the image piece by piece to the screen. The idea here is that
            // the whole update should take about a quarter of a second, assuming
            // most of the time is spent in waitForTimer(). It looks good to me,
            // but might still need some tuning.

            blits = 0;
            blits_before_refresh = (3 * w * h) / 25;

            // Speed up the effect for CD Loom since it uses it so often. I don't
            // think the original had any delay at all, so on modern hardware it
            // wasn't even noticeable.
            if (_game.Id=="loom")
                blits_before_refresh *= 2;

            for (i = 0; i < w * h; i++)
            {
                x = offsets [i] % vs.Pitch;
                y = offsets [i] / vs.Pitch;

                _gfxManager.CopyRectToScreen(vs.Surfaces[0].Pixels, vs.Pitch, 
                                             x, y, width, height);


                if (++blits >= blits_before_refresh)
                {
                    blits = 0;
                    System.Threading.Thread.Sleep(30);
                }
            }

            if (blits != 0)
            {
                System.Threading.Thread.Sleep(30);
            }
        }

        void FadeOut(int effect)
        {
            _mainVirtScreen.SetDirtyRange(0, 0);

            _camera.LastPosition.X = _camera.CurrentPosition.X;

            if (_screenEffectFlag && effect != 0)
            {
                // Fill screen 0 with black
                var l_pixNav = new PixelNavigator(_mainVirtScreen.Surfaces[0]);
                l_pixNav.OffsetX(_mainVirtScreen.XStart);
                Gdi.Fill(l_pixNav, 0, _mainVirtScreen.Width, _mainVirtScreen.Height);

                // Fade to black with the specified effect, if any.
                switch (effect)
                {
                    case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        //case 6:
                        //    transitionEffect(effect - 1);
                        //    break;
                        case 128:
                        UnkScreenEffect6();
                        break;

                        case 129:
                        // Just blit screen 0 to the display (i.e. display will be black)
                        _mainVirtScreen.SetDirtyRange(0, _mainVirtScreen.Height);
                        UpdateDirtyScreen(_mainVirtScreen);
                        break;
                        //case 134:
                        //    dissolveEffect(1, 1);
                        //    break;
                        //case 135:
                        //    dissolveEffect(1, _virtscr[kMainVirtScreen].h);
                        //    break;
                        default:
                        throw new NotImplementedException(string.Format("fadeOut: case {0}", effect));
                }
            }

            // Update the palette at the end (once we faded to black) to avoid
            // some nasty effects when the palette is changed
            UpdatePalette();

            _screenEffectFlag = false;
        }

        void SetShake(bool enabled)
        {
            if(_shakeEnabled != enabled)
                _fullRedraw = true;

            _shakeEnabled = enabled;
            _shakeFrame = 0;
            _gfxManager.SetShakePos(0);
        }

        void StopCycle(int i)
        {
            ScummHelper.AssertRange(0, i, 16, "stopCycle: cycle");
            if (i != 0)
            {
                _colorCycle[i - 1].Delay = 0;
                return;
            }

            for (i = 0; i < 16; i++)
            {
                var cycl = _colorCycle[i];
                cycl.Delay = 0;
            }
        }

        #endregion Effects Methods

        #region Lights Methods

        internal LightModes GetCurrentLights()
        {
            //if (_game.version >= 6)
            //    return LIGHTMODE_room_lights_on | LIGHTMODE_actor_use_colors;
            //else
            return (LightModes)_variables[VariableCurrentLights];
        }

        internal bool IsLightOn()
        {
            return GetCurrentLights().HasFlag(LightModes.RoomLightsOn);
        }

        #endregion Lights Methods

        #region Input Methods
       
        void CheckExecVerbs()
        {
            if (_userPut <= 0 || mouseAndKeyboardStat == 0)
                return;

            if ((ScummMouseButtonState)mouseAndKeyboardStat < ScummMouseButtonState.MaxKey)
            {
                // Check keypresses
                var vs = (from verb in _verbs.Skip(1)
                          where verb.VerbId != 0 && verb.SaveId == 0 && verb.CurMode == 1
                          where verb.Key == (byte)mouseAndKeyboardStat
                          select verb).FirstOrDefault();
                if (vs != null)
                {
                    // Trigger verb as if the user clicked it
                    RunInputScript(ClickArea.Verb, (KeyCode)vs.VerbId, 1);
                    return;
                }

                // Generic keyboard input
                RunInputScript(ClickArea.Key, mouseAndKeyboardStat, 1);
            }
            else if ((((ScummMouseButtonState)mouseAndKeyboardStat) & ScummMouseButtonState.MouseMask) != 0)
            {
                var code = mouseAndKeyboardStat.HasFlag(ScummMouseButtonState.LeftClick) ? (byte)1 : (byte)2;
                var zone = FindVirtScreen(_mousePos.Y);

                if (zone == null)
                    return;

                var over = FindVerbAtPos(_mousePos.X, _mousePos.Y);

                if (over != 0)
                {
                    // Verb was clicked
                    RunInputScript(ClickArea.Verb, (KeyCode)_verbs[over].VerbId, code);
                }
                else
                {
                    // Scene was clicked
                    var area = zone == MainVirtScreen ? ClickArea.Scene : ClickArea.Verb;
                    RunInputScript(area, 0, code);
                }
            }
        }

        void ProcessInput()
        {
            mouseAndKeyboardStat = 0;

            bool mainmenuKeyEnabled = _variables[VariableMainMenu] != 0;

            if (_inputManager.IsKeyDown(KeyCode.Escape))
            {
                mouseAndKeyboardStat = (KeyCode)Variables[ScummEngine.VariableCutSceneExitKey];
                AbortCutscene();
            }

            if (mainmenuKeyEnabled && _inputManager.IsKeyDown(KeyCode.F5))
            {
                var eh = ShowMenuDialogRequested;
                if (eh != null)
                {
                    eh(this, EventArgs.Empty);
                }
            }

            for (var i = KeyCode.A; i <= KeyCode.Z; i++)
            {
                if (_inputManager.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = i;
                }
            }
            for (var i = KeyCode.F1; i <= KeyCode.F9; i++)
            {
                if (_inputManager.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = i;
                }
            }

            for (var i = KeyCode.F1; i <= KeyCode.F9; i++)
            {
                if (_inputManager.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = i;
                }
            }

            if (_inputManager.IsKeyDown(KeyCode.Return))
            {
                mouseAndKeyboardStat = KeyCode.Return;
            }
            if (_inputManager.IsKeyDown(KeyCode.Backspace))
            {
                mouseAndKeyboardStat = KeyCode.Backspace;
            }
            if (_inputManager.IsKeyDown(KeyCode.Tab))
            {
                mouseAndKeyboardStat = KeyCode.Tab;
            }
            if (_inputManager.IsKeyDown(KeyCode.Space))
            {
                mouseAndKeyboardStat = KeyCode.Space;
            }

            if (_inputManager.IsMouseLeftPressed())
            {
                mouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.LeftClick;
            }

            if (_inputManager.IsMouseRightPressed())
            {
                mouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.RightClick;
            }

            var numpad = new int[]{
                '0',
                335, 336, 337,
                331, 332, 333,
                327, 328, 329
            };

            for (var i = KeyCode.D0; i <= KeyCode.D9; i++)
            {
                if (_inputManager.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = (KeyCode)numpad[i - KeyCode.D0];
                }
            }

            _mousePos = _inputManager.GetMousePosition();
            if (_mousePos.X < 0)
                _mousePos.X = 0;
            if (_mousePos.X > ScreenWidth - 1)
                _mousePos.X = (short)(ScreenWidth - 1);
            if (_mousePos.Y < 0)
                _mousePos.Y = 0;
            if (_mousePos.Y > ScreenHeight - 1)
                _mousePos.Y = (short)(ScreenHeight - 1);

            var mouseX = (ScreenStartStrip * 8) + _mousePos.X;
            Variables[ScummEngine.VariableMouseX] = (int)_mousePos.X;
            Variables[ScummEngine.VariableMouseY] = (int)_mousePos.Y;
            Variables[ScummEngine.VariableVirtualMouseX] = (int)mouseX;
            Variables[ScummEngine.VariableVirtualMouseY] = (int)_mousePos.Y - MainVirtScreen.TopLine;
        }

        #endregion Input Methods

        #region Inventory
        int GetInventorySlot()
        {
            for (int i = 0; i < NumInventory; i++)
            {
                if (_inventory[i] == 0)
                    return i;
            }
            return -1;
        }

        void AddObjectToInventory(int obj, byte room)
        {
            var slot = GetInventorySlot();
            if (GetWhereIsObject(obj) == WhereIsObject.FLObject)
            {
                GetObjectIndex(obj);
                throw new NotImplementedException();
            }
            else
            {
                var objs = _scumm.GetRoom(room).Objects;
                var objFound = (from o in objs
                                where o.Number == obj
                                select o).FirstOrDefault();
                _invData[slot] = objFound;
            }
            _inventory[slot] = (ushort)obj;
        }

        int GetInventoryCount(int owner)
        {
            int i, obj;
            int count = 0;
            for (i = 0; i < NumInventory; i++)
            {
                obj = _inventory[i];
                if (obj != 0 && GetOwner(obj) == owner)
                    count++;
            }
            return count;
        }

        int FindInventory(int owner, int idx)
        {
            int count = 1, i, obj;
            for (i = 0; i < NumInventory; i++)
            {
                obj = _inventory[i];
                if (obj != 0 && GetOwner(obj) == owner && count++ == idx)
                    return obj;
            }
            return 0;
        }
        #endregion

        #region Save & Load

        const uint InfoSectionVersion = 2;
        const uint SaveInfoSectionSize = (4 + 4 + 4 + 4 + 4 + 4 + 2);
        const uint SaveCurrentVersion = 94;

        bool _hasToLoad;
        bool _hasToSave;
        string _savegame;

        public void Load(string savegame)
        {
            _hasToLoad = true;
            _savegame = savegame;
        }

        void SaveLoad()
        {
            // TODO: improve this
            if (_variables[VariableMainMenu] != 0)
            {
                if (_hasToLoad)
                {
                    _hasToLoad = false;
                    if (File.Exists(_savegame))
                    {
                        LoadState(_savegame);
                    }
                }
                else if (_hasToSave)
                {
                    _hasToSave = false;
                    SaveState(_savegame, _savegame);
                }
            }
        }

        void SaveState(string path, string name)
        {
            using (var file = File.OpenWrite(path))
            {
                var bw = new BinaryWriter(file);
                SaveHeader(name, bw);

                SaveInfos(bw);

                var serializer = Serializer.CreateWriter(bw, CurrentVersion);
                SaveOrLoad(serializer);
            }
        }

        bool LoadState(string path)
        {
            using (var file = File.OpenRead(path))
            {
                var br = new BinaryReader(file);
                var hdr = LoadSaveGameHeader(br);
                var serializer = Serializer.CreateReader(br, hdr.Version);

                // Since version 56 we save additional information about the creation of
                // the save game and the save time.
                if (hdr.Version >= 56)
                {
                    SaveStateMetaInfos infos = LoadInfos(br);
                    if (infos == null)
                    {
                        //warning("Info section could not be found");
                        //delete in;
                        return false;
                    }

                    //SetTotalPlayTime(infos.playtime * 1000);
                }
                //else
                //{
                    // start time counting
                    //setTotalPlayTime();
                //}

                // Due to a bug in scummvm up to and including 0.3.0, save games could be saved
                // in the V8/V9 format but were tagged with a V7 mark. Ouch. So we just pretend V7 == V8 here
                if (hdr.Version == 7)
                    hdr.Version = 8;

                //_saveLoadDescription = hdr.name;

                // Unless specifically requested with _saveSound, we do not save the iMUSE
                // state for temporary state saves - such as certain cutscenes in DOTT,
                // FOA, Sam and Max, etc.
                //
                // Thus, we should probably not stop music when restoring from one of
                // these saves. This change stops the Mole Man theme from going quiet in
                // Sam & Max when Doug tells you about the Ball of Twine, as mentioned in
                // patch #886058.
                //
                // If we don't have iMUSE at all we may as well stop the sounds. The previous
                // default behavior here was to stopAllSounds on all state restores.

                //if (!_imuse || _saveSound || !_saveTemporaryState)
                //    _sound->stopAllSounds();

                //            _sound->stopCD();

                //_sound->pauseSounds(true);

                //closeRoom();

                Array.Clear(_inventory, 0, _inventory.Length);
                Array.Clear(_invData, 0, _invData.Length);
                _newNames.Clear();

                // Because old savegames won't fill the entire gfxUsageBits[] array,
                // clear it here just to be sure it won't hold any unforseen garbage.
                Array.Clear(_gfxUsageBits, 0, _gfxUsageBits.Length);

                // Nuke all resources
                //for (ResType type = rtFirst; type <= rtLast; type = ResType(type + 1))
                //    if (type != rtTemp && type != rtBuffer && (type != rtSound || _saveSound || !compat))
                //        for (ResId idx = 0; idx < _res->_types[type].size(); idx++)
                //        {
                //            _res->nukeResource(type, idx);
                //        }

                InitVariables();

                //if (_game.features & GF_OLD_BUNDLE)
                //    loadCharset(0); // FIXME - HACK ?

                SaveOrLoad(serializer);

                var sb = _screenB;
                var sh = _screenH;
                _camera.LastPosition.X = _camera.CurrentPosition.X;

                // Restore the virtual screens and force a fade to black.
                InitScreens(0, ScreenHeight);

                Gdi.Fill(MainVirtScreen.Surfaces[0].Pixels, MainVirtScreen.Pitch, 0, MainVirtScreen.Width, MainVirtScreen.Height);
                MainVirtScreen.SetDirtyRange(0, MainVirtScreen.Height);
                UpdateDirtyScreen(MainVirtScreen);
                //UpdatePalette();
                _gfxManager.SetPalette(_currentPalette.Colors);
                InitScreens(sb, sh);

                _completeScreenRedraw = true;

                // Reset charset mask
                _charset.HasMask = false;
                ClearTextSurface();
                ClearDrawObjectQueue();
                _verbMouseOver = 0;

                CameraMoved();
            }

            return true;
        }

        void SaveOrLoad(Serializer serializer)
        {
            uint ENCD_offs = 0;
            uint EXCD_offs = 0;
            uint IM00_offs = 0;
            uint CLUT_offs = 0;
            uint EPAL_offs = 0;
            uint PALS_offs = 0;
            byte curPalIndex = 0;
            byte numObjectsInRoom = (byte)_objs.Length;

            #region MainEntries

            var mainEntries = new[]{
                    LoadAndSaveEntry.Create(reader => _gameMD5 = reader.ReadBytes(16), writer =>writer.Write(_gameMD5), 39),
                    LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.Write(roomData.Header.Width), 8,50),
                    LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.Write(roomData.Header.Height),8,50),
                    LoadAndSaveEntry.Create(reader => ENCD_offs = reader.ReadUInt32(), writer => writer.Write(ENCD_offs),8,50),
                    LoadAndSaveEntry.Create(reader => EXCD_offs = reader.ReadUInt32(), writer => writer.Write(EXCD_offs),8,50),
                    LoadAndSaveEntry.Create(reader => IM00_offs = reader.ReadUInt32(), writer => writer.Write(IM00_offs),8,50),
                    LoadAndSaveEntry.Create(reader => CLUT_offs = reader.ReadUInt32(), writer => writer.Write(CLUT_offs),8,50),
                    LoadAndSaveEntry.Create(reader => EPAL_offs = reader.ReadUInt32(), writer => writer.Write(EPAL_offs),8,9),
                    LoadAndSaveEntry.Create(reader => PALS_offs = reader.ReadUInt32(), writer => writer.Write(PALS_offs),8,50),
                    LoadAndSaveEntry.Create(reader => curPalIndex = reader.ReadByte(), writer => writer.Write(curPalIndex),8),
                    LoadAndSaveEntry.Create(reader => _currentRoom = reader.ReadByte(), (writer)=> writer.Write(_currentRoom),8),
                    LoadAndSaveEntry.Create(reader => _roomResource = reader.ReadByte(), (writer)=> writer.Write(_roomResource),8),
                    LoadAndSaveEntry.Create(reader => numObjectsInRoom = reader.ReadByte(), (writer)=> writer.Write(numObjectsInRoom),8),
                    LoadAndSaveEntry.Create(reader => _currentScript = reader.ReadByte(), (writer)=> writer.Write(_currentScript),8),
                    LoadAndSaveEntry.Create(reader =>  reader.ReadUInt32s(_numLocalScripts), (writer)=> writer.Write(new uint[_numLocalScripts],_numLocalScripts),8,50),
                    // vm.localvar grew from 25 to 40 script entries and then from
                    // 16 to 32 bit variables (but that wasn't reflect here)... and
                    // THEN from 16 to 25 variables.
                    LoadAndSaveEntry.Create(reader => {
                        for (int i = 0; i < 25; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadUInt16s(17));
                        }
                    },writer => {
                        for (int i = 0; i < 25; i++) 
                        {
                            writer.WriteUInt16s(_slots[i].LocalVariables.Cast<ushort>().ToArray(),17);
                        }
                    },8,8),
                    LoadAndSaveEntry.Create(reader => {
                        for (int i = 0; i < 40; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadUInt16s(17));
                        }
                    },writer => {
                        for (int i = 0; i < 40; i++)
                        {
                            writer.WriteUInt16s(_slots[i].LocalVariables.Cast<ushort>().ToArray(),17);
                        }
                    },9,14),
                    // We used to save 25 * 40 = 1000 blocks; but actually, each 'row consisted of 26 entry,
                    // i.e. 26 * 40 = 1040. Thus the last 40 blocks of localvar where not saved at all. To be
                    // able to load this screwed format, we use a trick: We load 26 * 38 = 988 blocks.
                    // Then, we mark the followin 12 blocks (24 bytes) as obsolete.
                    LoadAndSaveEntry.Create(reader => {
                        for (int i = 0; i < 38; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadUInt16s(26));
                        }
                    },writer => {
                        for (int i = 0; i < 38; i++)
                        {
                            writer.WriteUInt16s(_slots[i].LocalVariables.Cast<ushort>().ToArray(),26);
                        }
                    },15,17),
                    // TODO
                    //MK_OBSOLETE_ARRAY(ScummEngine, vm.localvar[39][0], sleUint16, 12, VER(15), VER(17)),
                    // This was the first proper multi dimensional version of the localvars, with 32 bit values
                    LoadAndSaveEntry.Create(reader => {
                        for (int i = 0; i < 40; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadInt32s(26));
                        }
                    }, writer => {
                        for (int i = 0; i < 40; i++)
                        {
                        writer.WriteInt32s(_slots[i].LocalVariables,26);
                        }
                    },18,19),

                    // Then we doubled the script slots again, from 40 to 80
                    LoadAndSaveEntry.Create(reader => {
                        for (int i = 0; i < NumScriptSlot; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadInt32s(26));
                        }
                    },writer => {
                    for (int i = 0; i < NumScriptSlot; i++)
                        {
                            writer.WriteInt32s(_slots[i].LocalVariables,26);
                        }
                    },20),

                    LoadAndSaveEntry.Create(reader => _resourceMapper = reader.ReadBytes(128),writer => writer.Write(_resourceMapper), 8),
                    LoadAndSaveEntry.Create(reader => CharsetColorMap = reader.ReadBytes(16),writer => writer.Write(CharsetColorMap), 8),

                    // _charsetData grew from 10*16, to 15*16, to 23*16 bytes
                    LoadAndSaveEntry.Create(reader => reader.ReadMatrixBytes(10,16), writer => writer.WriteMatrixBytes(new byte[16,10],10,16), 8,9),
                    LoadAndSaveEntry.Create(reader => reader.ReadMatrixBytes(15,16), writer => writer.WriteMatrixBytes(new byte[16,15],15,16), 10,66),
                    LoadAndSaveEntry.Create(reader => reader.ReadMatrixBytes(23,16), writer => writer.WriteMatrixBytes(new byte[16,23],23,16), 67),
                                                   
                    LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.WriteUInt16(0), 8,62),
                                                   
                    LoadAndSaveEntry.Create(reader => _camera.DestinationPosition.X = reader.ReadInt16(), writer => writer.WriteInt16(_camera.DestinationPosition.X), 8),
                    LoadAndSaveEntry.Create(reader => _camera.DestinationPosition.Y = reader.ReadInt16(), writer => writer.WriteInt16(_camera.DestinationPosition.Y), 8),
                    LoadAndSaveEntry.Create(reader => _camera.CurrentPosition.X = reader.ReadInt16(), writer => writer.WriteInt16(_camera.CurrentPosition.X), 8),
                    LoadAndSaveEntry.Create(reader => _camera.CurrentPosition.Y = reader.ReadInt16(), writer => writer.WriteInt16(_camera.CurrentPosition.Y), 8),
                    LoadAndSaveEntry.Create(reader => _camera.LastPosition.X = reader.ReadInt16(), writer => writer.WriteInt16(_camera.LastPosition.X), 8),
                    LoadAndSaveEntry.Create(reader => _camera.LastPosition.Y = reader.ReadInt16(), writer => writer.WriteInt16(_camera.LastPosition.Y), 8),
                    LoadAndSaveEntry.Create(reader => _camera.Accel.X = reader.ReadInt16(), writer => writer.WriteInt16(_camera.Accel.X), 8),
                    LoadAndSaveEntry.Create(reader => _camera.Accel.Y = reader.ReadInt16(), writer => writer.WriteInt16(_camera.Accel.Y), 8),
                    LoadAndSaveEntry.Create(reader => _screenStartStrip = reader.ReadInt16(), writer => writer.WriteInt16(_screenStartStrip), 8),
                    LoadAndSaveEntry.Create(reader => _screenEndStrip = reader.ReadInt16(), writer => writer.WriteInt16(_screenEndStrip), 8),
                    LoadAndSaveEntry.Create(reader => _camera.Mode = (CameraMode)reader.ReadByte(), writer => writer.Write((byte)_camera.Mode), 8),
                    LoadAndSaveEntry.Create(reader => _camera.ActorToFollow = reader.ReadByte(), writer => writer.Write(_camera.ActorToFollow), 8),
                    LoadAndSaveEntry.Create(reader => _camera.LeftTrigger = reader.ReadInt16(), writer => writer.WriteInt16(_camera.LeftTrigger), 8),
                    LoadAndSaveEntry.Create(reader => _camera.RightTrigger = reader.ReadInt16(), writer => writer.WriteInt16(_camera.RightTrigger), 8),
                    LoadAndSaveEntry.Create(reader => _camera.MovingToActor = reader.ReadUInt16()!=0, writer => writer.WriteUInt16(_camera.MovingToActor), 8),

                    LoadAndSaveEntry.Create(reader => _actorToPrintStrFor = reader.ReadByte(), writer => writer.WriteByte(_actorToPrintStrFor), 8),
                    LoadAndSaveEntry.Create(reader => _charsetColor = reader.ReadByte(), writer => writer.WriteByte(_charsetColor), 8),

                    // _charsetBufPos was changed from byte to int
                    LoadAndSaveEntry.Create(reader => _charsetBufPos = reader.ReadByte(), writer => writer.WriteByte(_charsetBufPos), 8,9),
                    LoadAndSaveEntry.Create(reader => _charsetBufPos = reader.ReadInt16(), writer => writer.WriteInt16(_charsetBufPos), 10),
                                                   
                    LoadAndSaveEntry.Create(reader => _haveMsg = reader.ReadByte(), writer => writer.WriteByte(_haveMsg), 8),
                    LoadAndSaveEntry.Create(reader => _haveActorSpeechMsg = reader.ReadByte()!=0, writer => writer.WriteByte(_haveActorSpeechMsg), 61),
                    LoadAndSaveEntry.Create(reader => _useTalkAnims = reader.ReadByte()!=0, writer => writer.WriteByte(_useTalkAnims), 8),
                                                   
                    LoadAndSaveEntry.Create(reader => _talkDelay = reader.ReadInt16(), writer => writer.WriteInt16(_talkDelay), 8),
                    LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 8),
                    LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 8,27),
                    LoadAndSaveEntry.Create(reader => _sentenceNum = reader.ReadByte(), writer => writer.WriteByte(_sentenceNum), 8),
                                                   
                    LoadAndSaveEntry.Create(reader => cutScene.SaveOrLoad(serializer), writer => cutScene.SaveOrLoad(serializer), 8),
                                                   
                    LoadAndSaveEntry.Create(reader => _numNestedScripts = reader.ReadByte(), writer => writer.WriteByte(_numNestedScripts), 8),
                    LoadAndSaveEntry.Create(reader => _userPut = (sbyte)reader.ReadByte(), writer => writer.WriteByte(_userPut), 8),
                    LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.WriteUInt16(0), 17),
                    LoadAndSaveEntry.Create(reader => _cursor.State = (sbyte)reader.ReadByte(), writer => writer.WriteByte(_cursor.State), 8),
                    LoadAndSaveEntry.Create(reader => reader.ReadByte(), writer => writer.WriteByte(0), 8,20),
                    LoadAndSaveEntry.Create(reader => _currentCursor = reader.ReadByte(), writer => writer.WriteByte(_currentCursor), 8),
                    LoadAndSaveEntry.Create(reader => reader.ReadBytes(8192), writer => writer.Write(new byte[8192]), 20),
                    LoadAndSaveEntry.Create(reader => _cursor.Width = reader.ReadInt16(), writer => writer.WriteInt16(_cursor.Width), 20),
                    LoadAndSaveEntry.Create(reader => _cursor.Height = reader.ReadInt16(), writer => writer.WriteInt16(_cursor.Height), 20),
                    LoadAndSaveEntry.Create(reader => _cursor.HotspotX = reader.ReadInt16(), writer => writer.WriteInt16(_cursor.HotspotX), 20),
                    LoadAndSaveEntry.Create(reader => _cursor.HotspotY = reader.ReadInt16(), writer => writer.WriteInt16(_cursor.HotspotY), 20),
                    LoadAndSaveEntry.Create(reader => _cursor.Animate = reader.ReadByte()!=0, writer => writer.WriteByte(_cursor.Animate), 20),
                    LoadAndSaveEntry.Create(reader => _cursor.AnimateIndex = reader.ReadByte(), writer => writer.WriteByte(_cursor.AnimateIndex), 20),
                    LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 20),
                    LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 20),
                                                   
                    LoadAndSaveEntry.Create(reader => reader.ReadBytes(256), writer => writer.Write(new byte[256]), 60),
                    LoadAndSaveEntry.Create(reader => _doEffect = reader.ReadByte()!=0, writer => writer.WriteByte(_doEffect), 8),
                    LoadAndSaveEntry.Create(reader => _switchRoomEffect = reader.ReadByte(), writer => writer.WriteByte(_switchRoomEffect), 8),
                    LoadAndSaveEntry.Create(reader => _newEffect = reader.ReadByte(), writer => writer.WriteByte(_newEffect), 8),
                    LoadAndSaveEntry.Create(reader => _switchRoomEffect2 = reader.ReadByte(), writer => writer.WriteByte(_switchRoomEffect2), 8),
                    LoadAndSaveEntry.Create(reader => _bgNeedsRedraw = reader.ReadByte()!=0, writer => writer.WriteByte(_bgNeedsRedraw), 8),

                    // The state of palManipulate is stored only since V10
                    LoadAndSaveEntry.Create((reader)=> reader.ReadByte(), writer => writer.WriteByte(0), 10),
                    LoadAndSaveEntry.Create((reader)=> reader.ReadByte(), writer => writer.WriteByte(0), 10),
                    LoadAndSaveEntry.Create((reader)=> reader.ReadUInt16(), writer => writer.WriteUInt16(0), 10),

                    // gfxUsageBits grew from 200 to 410 entries. Then 3 * 410 entries:
                    LoadAndSaveEntry.Create(reader => _gfxUsageBits = reader.ReadUInt32s(200), writer => writer.WriteUInt32s(_gfxUsageBits,200), 8,9),
                    LoadAndSaveEntry.Create(reader => _gfxUsageBits = reader.ReadUInt32s(410), writer => writer.WriteUInt32s(_gfxUsageBits,410), 10,13),
                    LoadAndSaveEntry.Create(reader => _gfxUsageBits = reader.ReadUInt32s(3*410), writer => writer.WriteUInt32s(_gfxUsageBits,3*410), 14),
                                                   
                    LoadAndSaveEntry.Create(reader => Gdi.TransparentColor = reader.ReadByte(), writer => writer.WriteByte(Gdi.TransparentColor), 8,50),
                    LoadAndSaveEntry.Create(reader => {
                        for (int i = 0; i < 256; i++)
                        {
                            _currentPalette.Colors[i] = Color.FromRgb(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                        }
                    }, writer => {
                        for (int i = 0; i < 256; i++)
                        {
                            var l_color = _currentPalette.Colors[i];
                            writer.Write(l_color.R);
                            writer.Write(l_color.G);
                            writer.Write(l_color.B);
                        }
                    }, 8),
                    LoadAndSaveEntry.Create((reader)=> reader.ReadBytes(768), writer => writer.Write(new byte[768]), 53),

                    // Sam & Max specific palette replaced by _shadowPalette now.
                    LoadAndSaveEntry.Create((reader)=> reader.ReadBytes(256), writer => writer.Write(new byte[256]), 8,33),

                    LoadAndSaveEntry.Create(reader => _charsetBuffer = reader.ReadBytes(256), writer => writer.WriteBytes(_charsetBuffer,256), 8),

                    LoadAndSaveEntry.Create(reader => _egoPositioned = reader.ReadByte()!=0, writer => writer.WriteByte(_egoPositioned), 8),

                    // _gdi->_imgBufOffs grew from 4 to 5 entries. Then one day we realized
                    // that we don't have to store it since initBGBuffers() recomputes it.
                    LoadAndSaveEntry.Create((reader)=> reader.ReadUInt16s(4), writer => writer.WriteUInt16s(new ushort[4],4), 8,9),
                    LoadAndSaveEntry.Create((reader)=> reader.ReadUInt16s(5), writer => writer.WriteUInt16s(new ushort[5],5), 10,26),

                    // See _imgBufOffs: _numZBuffer is recomputed by initBGBuffers().
                    LoadAndSaveEntry.Create((reader)=> Gdi.NumZBuffer = reader.ReadByte(), writer => writer.WriteByte(Gdi.NumZBuffer), 8,26),

                    LoadAndSaveEntry.Create((reader)=> _screenEffectFlag = reader.ReadByte()!=0, writer => writer.WriteByte(_screenEffectFlag), 8),

                    LoadAndSaveEntry.Create((reader)=> reader.ReadByte(), writer => writer.WriteByte(0), 8,9),
                    LoadAndSaveEntry.Create((reader)=> reader.ReadByte(), writer => writer.WriteByte(0), 8,9),

                    // Converted _shakeEnabled to boolean and added a _shakeFrame field.
                    LoadAndSaveEntry.Create((reader)=> _shakeEnabled = reader.ReadInt16()==1, writer => writer.WriteInt16(_shakeEnabled?1:0), 8,9),
                    LoadAndSaveEntry.Create((reader)=> _shakeEnabled = reader.ReadBoolean(), writer => writer.WriteByte(_shakeEnabled), 10),
                    LoadAndSaveEntry.Create((reader)=> _shakeFrame = (int)reader.ReadUInt32(), writer => writer.WriteUInt32((uint)_shakeFrame), 10),

                    LoadAndSaveEntry.Create((reader)=> _keepText = reader.ReadByte()!=0, writer => writer.WriteByte(_keepText), 8),

                    LoadAndSaveEntry.Create((reader)=> _screenB = reader.ReadUInt16(), writer => writer.WriteUInt16(_screenB), 8),
                    LoadAndSaveEntry.Create((reader)=> _screenH = reader.ReadUInt16(), writer => writer.WriteUInt16(_screenH), 8),

                    LoadAndSaveEntry.Create((reader)=> reader.ReadUInt16(), writer => writer.WriteUInt16(0), 47),

                    LoadAndSaveEntry.Create((reader)=> reader.ReadInt16(), writer => writer.WriteInt16(0), 9,9),
                    LoadAndSaveEntry.Create((reader)=> reader.ReadInt16(), writer => writer.WriteInt16(0), 9,9),
                    LoadAndSaveEntry.Create((reader)=> reader.ReadInt16(), writer => writer.WriteInt16(0), 9,9),
                    LoadAndSaveEntry.Create((reader)=> reader.ReadInt16(), writer => writer.WriteInt16(0), 9,9)
                };

            #endregion MainEntries

            var md5Backup = new byte[16];
            Array.Copy(_gameMD5, md5Backup, 16);

            //Array.ForEach(mainEntries, e => e.Execute(serializer));
            for (int i = 0; i < mainEntries.Length; i++)
            {
                mainEntries[i].Execute(serializer);
            }

            if (serializer.IsLoading)
            {
                roomData = _scumm.GetRoom(_roomResource);
            }
            //if (!Array.Equals(md5Backup, _gameMD5))
            //{
            //    //warning("Game was saved with different gamedata - you may encounter problems");
            //    //debug(1, "You have %s and save is %s.", md5str2, md5str1);
            //    return false;
            //}

            // Starting V14, we extended the usage bits, to be able to cope with games
            // that have more than 30 actors (up to 94 are supported now, in theory).
            // Since the format of the usage bits was changed by this, we have to
            // convert them when loading an older savegame.
            //if (hdr.ver < 14)
            //    UpgradeGfxUsageBits();

            // When loading, move the mouse to the saved mouse position.
            //if (serializer.Version >= 20)
            //{
            //    UpdateCursor();
            //    _system->warpMouse(_mouse.x, _mouse.y);
            //}

            // Before V61, we re-used the _haveMsg flag to handle "alternative" speech
            // sound files (see charset code 10).
            if (serializer.IsLoading && serializer.Version < 61)
            {
                if (_haveMsg == 0xFE)
                {
                    _haveActorSpeechMsg = false;
                    _haveMsg = 0xFF;
                }
                else
                {
                    _haveActorSpeechMsg = true;
                }
            }

            //
            // Save/load actors
            //
            for (int i = 0; i < _actors.Length; i++)
            {
                _actors[i].SaveOrLoad(serializer);
            }

            //
            // Save/load sound data
            //
            var soundEntries = new[]{
                    LoadAndSaveEntry.Create(reader => reader.ReadInt16(),writer => writer.WriteInt16(0),35),
                    LoadAndSaveEntry.Create(reader => reader.ReadInt16(),writer => writer.WriteInt16(0),35),
                };
            Array.ForEach(soundEntries, e => e.Execute(serializer));

            //
            // Save/load script data
            //
            if (serializer.Version < 9)
            {
                for (int i = 0; i < 25; i++)
                {
                    _slots[i].SaveOrLoad(serializer, roomData.LocalScripts);
                }
            }
            else if (serializer.Version < 20)
            {
                for (int i = 0; i < 40; i++)
                {
                    _slots[i].SaveOrLoad(serializer, roomData.LocalScripts);
                }
            }
            else
            {
                for (int i = 0; i < NumScriptSlot; i++)
                {
                    _slots[i].SaveOrLoad(serializer, roomData.LocalScripts);
                }
            }
            if (serializer.IsLoading)
            {
                Array.ForEach(_slots, slot =>
                {
                    if (slot.Where == WhereIsObject.Global)
                    {
                        slot.Offset -= (uint)_scumm.GetGlobalScriptOffset((byte)slot.Number);
                    }
                    else if (slot.Where == WhereIsObject.Local && slot.Number >= 0xC8 && roomData.LocalScripts[slot.Number - 0xC8] != null)
                    {
                        slot.Offset = (uint)(slot.Offset - roomData.LocalScripts[slot.Number - 0xC8].Offset);
                    }
                });

                ResetRoomObjects();
            }

            //
            // Save/load local objects
            //
            for (int i = 0; i < _objs.Length; i++)
            {
                _objs[i].SaveOrLoad(serializer);
            }

            //
            // Save/load misc stuff
            //
            for (int i = 0; i < _verbs.Length; i++)
            {
                _verbs[i].SaveOrLoad(serializer);
            }
            for (int i = 0; i < 16; i++)
            {
                _nest[i].SaveOrLoad(serializer);
            }
            for (int i = 0; i < 6; i++)
            {
                _sentence[i].SaveOrLoad(serializer);
            }
            for (int i = 0; i < 6; i++)
            {
                _string[i].SaveOrLoad(serializer);
            }
            for (int i = 0; i < 16; i++)
            {
                _colorCycle[i].SaveOrLoad(serializer);
            }
            if (serializer.Version >= 13)
            {
                for (int i = 0; i < 20; i++)
                {
                    if (serializer.IsLoading)
                    {
                        _scaleSlots[i] = new ScaleSlot();
                    }
                    if (_scaleSlots[i] != null)
                    {
                        _scaleSlots[i].SaveOrLoad(serializer);
                    }
                }
            }

            //
            // Save/load resources
            //
            SaveOrLoadResources(serializer);

            //
            // Save/load global object state
            //
            var l_objStatesEntries = new[]{
                LoadAndSaveEntry.Create(reader =>
                {
                    var objectOwnerTable = reader.ReadBytes(_numGlobalObjects);
                    Array.Copy(objectOwnerTable, _scumm.ObjectOwnerTable, _numGlobalObjects);
                },
                writer =>
                {
                    writer.WriteBytes(_scumm.ObjectOwnerTable,_numGlobalObjects);
                }),
                LoadAndSaveEntry.Create(reader =>
                {
                    var objectStateTable = reader.ReadBytes(_numGlobalObjects);
                    Array.Copy(objectStateTable, _scumm.ObjectStateTable, _numGlobalObjects);
                },
                writer =>
                {
                    writer.WriteBytes(_scumm.ObjectStateTable, _numGlobalObjects);
                })
            };
            Array.ForEach(l_objStatesEntries, e => e.Execute(serializer));

            //if (_objectRoomTable)
            //    s->saveLoadArrayOf(_objectRoomTable, _numGlobalObjects, sizeof(_objectRoomTable[0]), sleByte);

            //
            // Save/load palette data
            // Don't save 16 bit palette in FM-Towns and PCE games, since it gets regenerated afterwards anyway.
            //if (_16BitPalette && !(_game.platform == Common::kPlatformFMTowns && s->getVersion() < VER(82)) && !((_game.platform == Common::kPlatformFMTowns || _game.platform == Common::kPlatformPCEngine) && s->getVersion() > VER(87))) {
            //    s->saveLoadArrayOf(_16BitPalette, 512, sizeof(_16BitPalette[0]), sleUint16);
            //}

            var l_paletteEntries = new[]{
                LoadAndSaveEntry.Create(reader => {
                    _shadowPalette = reader.ReadBytes(_shadowPalette.Length);
                },
                writer => {
                    writer.WriteBytes(_shadowPalette, _shadowPalette.Length);
                }),
                // _roomPalette didn't show up until V21 save games
                // Note that we also save the room palette for Indy4 Amiga, since it
                // is used as palette map there too, but we do so slightly a bit
                // further down to group it with the other special palettes needed.
                LoadAndSaveEntry.Create(reader => {
                    RoomPalette = reader.ReadBytes(256);
                },
                writer => {
                    writer.WriteBytes(RoomPalette,256);
                },21),

                // PalManip data was not saved before V10 save games
                LoadAndSaveEntry.Create(reader => {
//                    if (palManipCounter!=0)
//                    {
//                        _palManipPalette = reader.ReadBytes(0x300);
//                        _palManipIntermediatePal = reader.ReadBytes(0x600);
//                    }
                },
                writer => {
//                    if (palManipCounter!=0)
//                    {
//                        writer.WriteBytes(_palManipPalette, 0x300);
//                        writer.WriteBytes(_palManipIntermediatePal, 0x600);
//                    }
                },10),


                // darkenPalette was not saved before V53
                LoadAndSaveEntry.Create(reader => {
                    // TODO?
                    //Array.Copy(currentPalette, darkenPalette, 768);
                },0, 53),

                // darkenPalette was not saved before V53
                LoadAndSaveEntry.Create(reader => {
//                    if (palManipCounter != 0)
//                    {
//                        _palManipPalette = reader.ReadBytes(0x300);
//                        _palManipIntermediatePal = reader.ReadBytes(0x600);
//                    }
                },
                writer => {
//                    if (palManipCounter != 0)
//                    {
//                        writer.WriteBytes(_palManipPalette,0x300);
//                        writer.WriteBytes(_palManipIntermediatePal,0x600);
//                    }
                },53)
            };
            Array.ForEach(l_paletteEntries, entry => entry.Execute(serializer));

            // _colorUsedByCycle was not saved before V60
            if (serializer.IsLoading)
            {
                if (serializer.Version < 60)
                {
                    //Array.Clear(_colorUsedByCycle, 0, _colorUsedByCycle.Length);
                }
            }

            // Indy4 Amiga specific palette tables were not saved before V85
            //if (_game.platform == Common::kPlatformAmiga && _game.id == GID_INDY4) {
            //    if (s->getVersion() >= 85) {
            //        s->saveLoadArrayOf(_roomPalette, 256, 1, sleByte);
            //        s->saveLoadArrayOf(_verbPalette, 256, 1, sleByte);
            //        s->saveLoadArrayOf(_amigaPalette, 3 * 64, 1, sleByte);

            //        // Starting from version 86 we also save the first used color in
            //        // the palette beyond the verb palette. For old versions we just
            //        // look for it again, which hopefully won't cause any troubles.
            //        if (s->getVersion() >= 86) {
            //            s->saveLoadArrayOf(&_amigaFirstUsedColor, 1, 2, sleUint16);
            //        } else {
            //            amigaPaletteFindFirstUsedColor();
            //        }
            //    } else {
            //        warning("Save with old Indiana Jones 4 Amiga palette handling detected");
            //        // We need to restore the internal state of the Amiga palette for Indy4
            //        // Amiga. This might lead to graphics glitches!
            //        setAmigaPaletteFromPtr(_currentPalette);
            //    }
            //}

            //
            // Save/load more global object state
            //
            var l_globalObjStatesEntries = new[]{
                LoadAndSaveEntry.Create(reader => {
                    Array.Copy(reader.ReadUInt32s(_numGlobalObjects), _scumm.ClassData, _numGlobalObjects);
                },
                writer => {
                    writer.WriteUInt32s(_scumm.ClassData, _numGlobalObjects);
                }),
            };
            Array.ForEach(l_globalObjStatesEntries, entry => entry.Execute(serializer));

            //
            // Save/load script variables
            //
            //var120Backup = _scummVars[120];
            //var98Backup = _scummVars[98];

            //if (serializer.Version > 37)
            //{
            //    s->saveLoadArrayOf(_roomVars, _numRoomVariables, sizeof(_roomVars[0]), sleInt32);
            //}

            // The variables grew from 16 to 32 bit.
            var l_variablesEntries = new[]{
                LoadAndSaveEntry.Create(reader => {
                    _variables = Array.ConvertAll(reader.ReadInt16s(_variables.Length), s => (int)s);
                },
                writer => {
                    writer.WriteInt16s(_variables, _variables.Length);
                },0,15),
                LoadAndSaveEntry.Create(
                    reader => _variables = reader.ReadInt32s(_variables.Length),
                    writer => writer.WriteInt32s(_variables, _variables.Length),15),
                //if (_game.id == GID_TENTACLE) // Maybe misplaced, but that's the main idea
                //    _scummVars[120] = var120Backup;
                //if (_game.id == GID_INDY4)
                //    _scummVars[98] = var98Backup;
                LoadAndSaveEntry.Create(
                    reader => _bitVars = new BitArray(reader.ReadBytes(_bitVars.Length/8)),
                    writer => {
                        var vars=new byte[_bitVars.Length/8];
                        _bitVars.CopyTo(vars,0);
                        writer.Write(vars);
                    }
                ),
            };
            Array.ForEach(l_variablesEntries, entry => entry.Execute(serializer));

            //
            // Save/load a list of the locked objects
            //
            var l_lockedObjEntries = new[]{
                LoadAndSaveEntry.Create(reader => {
                    while (((ResType)reader.ReadByte()) != (ResType)0xFF)
                    {
                        reader.ReadUInt16();
                        //_res->lock(type, idx);
                    }
                },
                writer => {
                    writer.Write((byte)0xFF);
                })
            };
            Array.ForEach(l_lockedObjEntries, entry => entry.Execute(serializer));

            //
            // Save/load the Audio CD status
            //
            //if (serializer.Version >= 24)
            //{
            //    AudioCDManager::Status info;
            //    if (s->isSaving())
            //        info = _system->getAudioCDManager()->getStatus();
            //    s->saveLoadArrayOf(&info, 1, sizeof(info), audioCDEntries);
            //     If we are loading, and the music being loaded was supposed to loop
            //     forever, then resume playing it. This helps a lot when the audio CD
            //     is used to provide ambient music (see bug #788195).
            //    if (s->isLoading() && info.playing && info.numLoops < 0)
            //      _system->getAudioCDManager()->play(info.track, info.numLoops, info.start, info.duration);
            //}

            //
            // Save/load the iMuse status
            //
            //if (_imuse && (_saveSound || !_saveTemporaryState))
            //{
            //    _imuse->save_or_load(s, this);
            //}

            //
            // Save/load music engine status
            //
            //if (_musicEngine)
            //{
            //    _musicEngine->saveLoadWithSerializer(s);
            //}

            //
            // Save/load the charset renderer state
            //
            //if (s->getVersion() >= VER(73))
            //{
            //    _charset->saveLoadWithSerializer(s);
            //}
            //else if (s->isLoading())
            //{
            //    if (s->getVersion() == VER(72))
            //    {
            //        _charset->setCurID(s->loadByte());
            //    }
            //    else
            //    {
            //        // Before V72, the charset id wasn't saved. This used to cause issues such
            //        // as the one described in the bug report #1722153. For these savegames,
            //        // we reinitialize the id using a, hopefully, sane value.
            //        _charset->setCurID(_string[0]._default.charset);
            //    }
            //}
        }

        void SaveOrLoadResources(Serializer serializer)
        {
            var l_entry = LoadAndSaveEntry.Create(reader =>
            {
                ResType type;
                ushort idx;
                while ((type = (ResType)reader.ReadUInt16()) != (ResType)0xFFFF)
                {
                    while ((idx = reader.ReadUInt16()) != 0xFFFF)
                    {
                        LoadResource(reader, type, idx);
                    }
                }
            },
                writer =>
                {
                    // inventory
                    writer.Write((ushort)ResType.Inventory);
                    for (int i = 0; i < _invData.Length; i++)
                    {
                        var data = _invData[i];
                        if (data == null) break;
                        // write index
                        writer.WriteUInt16(i);
                        // write size
                        var nameOffset = data.ScriptOffsets.Values.Min() - data.Name.Length - 1;
                        writer.WriteInt32(nameOffset + data.Name.Length + 1 + data.Script.Data.Length);
                        writer.Write(new byte[18]);
                        // write name offset
                        writer.WriteByte(nameOffset);
                        // write verb table
                        foreach (var scriptOffset in data.ScriptOffsets)
                        {
                            writer.WriteByte(scriptOffset.Key);
                            writer.WriteUInt16(scriptOffset.Value);
                        }
                        // write end of table
                        writer.WriteByte(0);
                        var diff = nameOffset - (19 + 3 * data.ScriptOffsets.Count + 1);
                        for (int c = 0; c < diff; c++)
                        {
                            writer.WriteByte(0);
                        }
                        var name = EncodeName(data.Name);
                        // write name
                        for (int c = 0; c < name.Length; c++)
                        {
                            writer.WriteByte(name[c]);
                        }
                        writer.WriteByte(0);
                        // write script
                        writer.Write(data.Script.Data);
                        // write index
                        writer.WriteUInt16(_inventory[i]);
                    }
                    writer.WriteUInt16(0xFFFF);

                    // actors name
                    writer.Write((ushort)ResType.ActorName);
                    for (int i = 0; i < _actors.Length; i++)
                    {
                        var actor = _actors[i];
                        if (actor.Name != null)
                        {
                            // write index
                            writer.WriteUInt16(i);
                            // write name
                            writer.WriteInt32(actor.Name.Length);
                            writer.WriteBytes(actor.Name, actor.Name.Length);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // objects name
                    writer.Write((ushort)ResType.ObjectName);
                    var objs = _invData.ToArray();
                    for (int i = 0; i < objs.Length; i++)
                    {
                        var obj = objs[i];
                        if (obj != null && obj.Name != null && _inventory.Any(inv => inv == obj.Number))
                        {
                            // write index
                            writer.WriteUInt16(i);
                            // write name
                            writer.WriteInt32(obj.Name.Length);
                            writer.WriteBytes(obj.Name, obj.Name.Length);
                            // writer obj number
                            writer.WriteUInt16(obj.Number);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // matrix
                    writer.Write((ushort)ResType.Matrix);
                    // write BoxMatrix
                    writer.WriteUInt16(1);
                    writer.WriteInt32(_boxMatrix.Count);
                    writer.WriteBytes(_boxMatrix.ToArray(), _boxMatrix.Count);
                    // write boxes
                    writer.WriteUInt16(2);
                    writer.WriteInt32(20 * _boxes.Length + 1);
                    writer.WriteByte(_boxes.Length);
                    for (int i = 0; i < _boxes.Length; i++)
                    {
                        Box box = _boxes[i];
                        writer.WriteInt16(box.Ulx);
                        writer.WriteInt16(box.Uly);
                        writer.WriteInt16(box.Urx);
                        writer.WriteInt16(box.Ury);
                        writer.WriteInt16(box.Lrx);
                        writer.WriteInt16(box.Lry);
                        writer.WriteInt16(box.Llx);
                        writer.WriteInt16(box.Lly);
                        writer.WriteByte(box.Mask);
                        writer.WriteByte((byte)box.Flags);
                        writer.WriteUInt16(box.Scale);
                    }
                    writer.WriteUInt16(0xFFFF);

                    // verbs
                    writer.Write((ushort)ResType.Verb);
                    for (int i = 0; i < _verbs.Length; i++)
                    {
                        var verb = _verbs[i];
                        if (verb.Text != null)
                        {
                            // write index
                            writer.WriteUInt16(i);
                            // write text
                            writer.WriteInt32(verb.Text.Length);
                            writer.WriteBytes(verb.Text, verb.Text.Length);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // write end of resources
                    writer.WriteUInt16(0xFFFF);

                });
            l_entry.Execute(serializer);
        }

        static byte[] EncodeName(byte[] name)
        {
            var encodedName = new List<byte>();
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == 255 && name[i + 1] == 4)
                {
                    encodedName.AddRange(new byte[] { 35, 35, 35, 35 });
                    i += 3;
                }
                else
                {
                    encodedName.Add(name[i]);
                }
            }
            return encodedName.ToArray();
        }

        void LoadResource(BinaryReader reader, ResType type, ushort idx)
        {
            bool dynamic = false;
            switch (type)
            {
                case ResType.Inventory:
                case ResType.String:
                case ResType.Verb:
                case ResType.ActorName:
                case ResType.ScaleTable:
                case ResType.Temp:
                case ResType.FlObject:
                case ResType.Matrix:
                case ResType.ObjectName:
                    dynamic = true;
                    break;
            }

            if (dynamic)
            {
                int size = reader.ReadInt32();
                var ptr = reader.ReadBytes(size);

                Console.WriteLine("Type: {0}, Index: {1}, Data: {2}", type, idx, size);

                switch (type)
                {
                    case ResType.Inventory:
                        {
                            var index = reader.ReadUInt16();
                            _inventory[idx] = index;
                            var br = new BinaryReader(new MemoryStream(ptr));
                            br.BaseStream.Seek(18, SeekOrigin.Begin);
                            var offset = br.ReadByte();
                            br.BaseStream.Seek(offset, SeekOrigin.Begin);
                            var name = new List<byte>();
                            var c = br.ReadByte();
                            while (c != 0)
                            {
                                name.Add(c);
                                c = br.ReadByte();
                            }
                            _invData[idx] = new ObjectData { Number = index, Name = name.ToArray() };
                            _invData[idx].Script.Offset = offset + name.Count + 1;
                            _invData[idx].Script.Data = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
                            br.BaseStream.Seek(19, SeekOrigin.Begin);
                            while (true)
                            {
                                var id = br.ReadByte();
                                var off = br.ReadUInt16();
                                if (id == 0)
                                    break;
                                _invData[idx].ScriptOffsets.Add(id, off);
                            }
                        }
                        break;

                    case ResType.ActorName:
                        {
                            _newNames[idx] = ptr;
                            _actors[idx].Name = ptr;
                        }
                        break;

                    case ResType.ObjectName:
                        {
                            var index = reader.ReadUInt16();
                            var obj = (from o in _invData
                                       where o != null && o.Number == index
                                       select o).FirstOrDefault();
                            obj.Name = ptr;
                        }
                        break;

                    case ResType.Matrix:
                        {
                            if (idx == 1)
                            {
                                // BOXM
                                _boxMatrix.Clear();
                                _boxMatrix.AddRange(ptr);
                            }
                            else if (idx == 2)
                            {
                                // BOXD
                                var br = new BinaryReader(new MemoryStream(ptr));

                                var numBoxes = br.ReadByte();
                                _boxes = new Box[numBoxes];
                                for (int i = 0; i < numBoxes; i++)
                                {
                                    var box = new Box();
                                    box.Ulx = br.ReadInt16();
                                    box.Uly = br.ReadInt16();
                                    box.Urx = br.ReadInt16();
                                    box.Ury = br.ReadInt16();
                                    box.Lrx = br.ReadInt16();
                                    box.Lry = br.ReadInt16();
                                    box.Llx = br.ReadInt16();
                                    box.Lly = br.ReadInt16();
                                    box.Mask = br.ReadByte();
                                    box.Flags = (BoxFlags)br.ReadByte();
                                    box.Scale = br.ReadUInt16();
                                    _boxes[i] = box;
                                }
                            }
                        }
                        break;

                    case ResType.Verb:
                        {
                            _verbs[idx].Text = ptr;
                        }
                        break;

                    case ResType.String:
                        {
                            Console.WriteLine("String: {0}", Encoding.ASCII.GetString(ptr));
                        }
                        break;
                }
            }
            else
            {
                Console.WriteLine("Type: {0}", type);
            }
        }

        enum ResType
        {
            Invalid = 0,
            First = 1,
            Room = 1,
            Script = 2,
            Costume = 3,
            Sound = 4,
            Inventory = 5,
            Charset = 6,
            String = 7,
            Verb = 8,
            ActorName = 9,
            Buffer = 10,
            ScaleTable = 11,
            Temp = 12,
            FlObject = 13,
            Matrix = 14,
            Box = 15,
            ObjectName = 16,
            RoomScripts = 17,
            RoomImage = 18,
            Image = 19,
            Talkie = 20,
            SpoolBuffer = 21,
            Last = 21
        }

        public void Save(string filename)
        {
            _hasToSave = true;
            _savegame = filename;
        }

        static bool SkipThumbnail(BinaryReader reader)
        {
            var position = reader.BaseStream.Position;
            var header = LoadHeader(reader);

            if (header == null)
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                return false;
            }

            reader.BaseStream.Seek(header.Size - (reader.BaseStream.Position - position), SeekOrigin.Current);
            return true;
        }

        static bool CheckThumbnailHeader(BinaryReader reader)
        {
            var position = reader.BaseStream.Position;
            var header = LoadHeader(reader);

            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return header != null;
        }

        const byte TumbnailVersion = 1;

        void SaveHeader(string name, BinaryWriter bw)
        {
            var hdr = new SaveGameHeader();
            hdr.Type = ScummHelper.MakeTag('S', 'C', 'V', 'M');
            hdr.Size = 0;
            hdr.Version = SaveCurrentVersion;

            bw.WriteUInt32BigEndian(hdr.Type);
            bw.Write(hdr.Size);
            bw.Write(hdr.Version);

            var data = Encoding.Default.GetBytes(Path.GetFileNameWithoutExtension(name));
            var data2 = new byte[32];
            int length = Math.Min(data.Length, 31);
            Array.Copy(data, data2, Math.Min(data.Length, 31));
            data2[length] = 0;
            bw.Write(data2);
        }

        SaveStateMetaInfos LoadInfos(BinaryReader reader)
        {
            var stuff = new SaveStateMetaInfos();
            var section = new SaveInfoSection();
            section.Type = ScummHelper.SwapBytes(reader.ReadUInt32());
            if (section.Type != ScummHelper.MakeTag('I', 'N', 'F', 'O'))
            {
                return null;
            }

            section.Version = ScummHelper.SwapBytes(reader.ReadUInt32());
            section.Size = ScummHelper.SwapBytes(reader.ReadUInt32());

            // If we ever extend this we should add a table containing the sizes corresponding to each
            // version, so that we are able to properly verify their correctness.
            if (section.Version == InfoSectionVersion && section.Size != SaveInfoSectionSize)
            {
                //warning("Info section is corrupt");
                reader.BaseStream.Seek(section.Size, SeekOrigin.Current);
                return null;
            }

            section.TimeTValue = ScummHelper.SwapBytes(reader.ReadUInt32());
            section.PlayTime = ScummHelper.SwapBytes(reader.ReadUInt32());

            // For header version 1, we load the data in with our old method
            if (section.Version == 1)
            {
                //time_t tmp = section.timeTValue;
                //tm *curTime = localtime(&tmp);
                //stuff->date = (curTime->tm_mday & 0xFF) << 24 | ((curTime->tm_mon + 1) & 0xFF) << 16 | (curTime->tm_year + 1900) & 0xFFFF;
                //stuff->time = (curTime->tm_hour & 0xFF) << 8 | (curTime->tm_min) & 0xFF;
                stuff.Date = 0;
                stuff.Time = 0;
            }

            if (section.Version >= 2)
            {
                section.Date = ScummHelper.SwapBytes(reader.ReadUInt32());
                section.Time = ScummHelper.SwapBytes(reader.ReadUInt16());

                stuff.Date = section.Date;
                stuff.Time = section.Time;
            }

            stuff.PlayTime = section.PlayTime;

            // Skip over the remaining (unsupported) data
            if (section.Size > SaveInfoSectionSize)
                reader.BaseStream.Seek(section.Size - SaveInfoSectionSize, SeekOrigin.Current);

            return stuff;
        }

        void SaveInfos(BinaryWriter writer)
        {
            var section = new SaveInfoSection();
            section.Type = ScummHelper.MakeTag('I', 'N', 'F', 'O');
            section.Version = InfoSectionVersion;
            section.Size = SaveInfoSectionSize;

            // TODO: still save old format for older versions
            section.TimeTValue = 0;
            section.PlayTime = 0;

            //TimeDate curTime;
            //_system->getTimeAndDate(curTime);

            //section.date = ((curTime.tm_mday & 0xFF) << 24) | (((curTime.tm_mon + 1) & 0xFF) << 16) | ((curTime.tm_year + 1900) & 0xFFFF);
            //section.time = ((curTime.tm_hour & 0xFF) << 8) | ((curTime.tm_min) & 0xFF);

            writer.WriteUInt32BigEndian(section.Type);
            writer.WriteUInt32BigEndian(section.Version);
            writer.WriteUInt32BigEndian(section.Size);
            writer.WriteUInt32BigEndian(section.TimeTValue);
            writer.WriteUInt32BigEndian(section.PlayTime);
            writer.WriteUInt32BigEndian(section.Date);
            writer.WriteUInt16(section.Time);
        }

        static SaveGameHeader LoadSaveGameHeader(BinaryReader reader)
        {
            var filename = ((FileStream)reader.BaseStream).Name;
            var hdr = new SaveGameHeader();
            hdr.Type = ScummHelper.SwapBytes(reader.ReadUInt32());
            if (hdr.Type != ScummHelper.MakeTag('S', 'C', 'V', 'M')) throw new NotSupportedException(string.Format("Invalid savegame '{0}'", filename));
            hdr.Size = reader.ReadUInt32();
            hdr.Version = reader.ReadUInt32();
            // In older versions of ScummVM, the header version was not endian safe.
            // We account for that by retrying once with swapped byte order in case
            // we see a version that is higher than anything we'd expect...
            if (hdr.Version > 0xFFFFFF)
                hdr.Version = ScummHelper.SwapBytes(hdr.Version);

            // Reject save games which are too old or too new. Note that
            // We do not really support V7 games, but still accept them here
            // to work around a bug from the stone age (see below for more
            // information).
            if (hdr.Version < 7 || hdr.Version > CurrentVersion)
            {
                throw new NotSupportedException(string.Format("Invalid version of '{0}'", filename));
            }

            hdr.Name = Encoding.Default.GetString(reader.ReadBytes(32));

            // Since version 52 a thumbnail is saved directly after the header.
            if (hdr.Version >= 52)
            {
                // Prior to version 75 we always required an thumbnail to be present
                if (hdr.Version <= 74)
                {
                    if (!CheckThumbnailHeader(reader))
                    {
                        throw new NotSupportedException("Cannot load thumbnail");
                    }
                }
                SkipThumbnail(reader);
            }

            return hdr;
        }

        static ThumbnailHeader LoadHeader(BinaryReader reader)
        {
            var header = new ThumbnailHeader();
            header.Type = ScummHelper.SwapBytes(reader.ReadUInt32());
            // We also accept the bad 'BMHT' header here, for the sake of compatibility
            // with some older savegames which were written incorrectly due to a bug in
            // ScummVM which wrote the thumb header type incorrectly on LE systems.
            if (header.Type != ScummHelper.MakeTag('T', 'H', 'M', 'B') && header.Type != ScummHelper.MakeTag('B', 'M', 'H', 'T'))
            {
                //if (outputWarnings)
                //    warning("couldn't find thumbnail header type");
                return null;
            }

            header.Size = ScummHelper.SwapBytes(reader.ReadUInt32());
            header.Version = reader.ReadByte();

            if (header.Version > TumbnailVersion)
            {
                //if (outputWarnings)
                //    warning("trying to load a newer thumbnail version: %d instead of %d", header.version, THMB_VERSION);
                return null;
            }

            header.Width = ScummHelper.SwapBytes(reader.ReadUInt16());
            header.Height = ScummHelper.SwapBytes(reader.ReadUInt16());
            header.Bpp = reader.ReadByte();

            return header;
        }

        #endregion Save & Load

        #region Actors

        void ActorTalk(byte[] msg)
        {
            ConvertMessageToString(msg, _charsetBuffer, 0);

            if (_actorToPrintStrFor == 0xFF)
            {
                if (!_keepText)
                {
                    StopTalk();
                }
                TalkingActor=0xFF;
            }
            else
            {
                int oldact;

                var a = _actors[_actorToPrintStrFor];
                if (!a.IsInCurrentRoom)
                {
                    oldact = 0xFF;
                }
                else
                {
                    if (!_keepText)
                    {
                        StopTalk();
                    }
                    TalkingActor = a.Number;

                    if (!_string[0].NoTalkAnim)
                    {
                        a.RunTalkScript(a.TalkStartFrame);
                        _useTalkAnims = true;
                    }
                    oldact = TalkingActor;
                }
                if (oldact >= 0x80)
                    return;
            }

            if (TalkingActor > 0x7F)
            {
                _charsetColor = _string [0].Color;
            }
            else
            {
                var a = _actors[TalkingActor];
                _charsetColor = a.TalkColor;
            }

            _charsetBufPos = 0;
            _talkDelay = 0;
            _haveMsg = 0xFF;
            _variables[VariableHaveMessage] = 0xFF;

            _haveActorSpeechMsg = true;
            Charset();
        }

        internal int TalkingActor
        {
            get{ return _variables[VariableTalkActor]; }
            set { _variables [VariableTalkActor] = value; }
        }

        internal void StopTalk()
        {
            //_sound->stopTalkSound();

            _haveMsg = 0;
            _talkDelay = 0;

            var act = TalkingActor;
            if (act != 0 && act < 0x80)
            {
                var a = _actors[act];
                if (a.IsInCurrentRoom && _useTalkAnims)
                {
                    a.RunTalkScript(a.TalkStopFrame);
                    _useTalkAnims = false;
                }
                TalkingActor = 0xFF;
            }

            _keepText = false;
            RestoreCharsetBg();
        }

        void ShowActors()
        {
            for (int i = 0; i < _actors.Length; i++)
            {
                if (_actors[i].IsInCurrentRoom)
                    _actors[i].Show();
            }
        }

        void WalkActors()
        {
            for (int i = 0; i < _actors.Length; i++)
            {
                if (_actors[i].IsInCurrentRoom)
                    _actors[i].Walk();
            }
        }

        void ProcessActors()
        {
            var actors = from actor in _actors
                where actor.IsInCurrentRoom
                    orderby actor.Position.Y
                    select actor;

            foreach (var actor in actors)
            {
                if (actor.Costume != 0)
                {
                    actor.DrawCostume();
                    actor.AnimateCostume();
                }
            }
        }

        void HandleActors()
        {
            SetActorRedrawFlags();
            ResetActorBgs();

            var mode = GetCurrentLights();
            if (!mode.HasFlag(LightModes.RoomLightsOn) && mode.HasFlag(LightModes.FlashlightOn))
            {
                // TODO:
                //drawFlashlight();
                SetActorRedrawFlags();
            }

            ProcessActors();
        }

        void ResetActorBgs()
        {
            for (int i = 0; i < Gdi.NumStrips; i++)
            {
                int strip = _screenStartStrip + i;
                ClearGfxUsageBit(strip, UsageBitDirty);
                ClearGfxUsageBit(strip, UsageBitRestored);
                for (int j = 0; j < _actors.Length; j++)
                {
                    if (TestGfxUsageBit(strip, j) &&
                        ((_actors[j].Top != 0x7fffffff && _actors[j].NeedRedraw) || _actors[j].NeedBackgroundReset))
                    {
                        ClearGfxUsageBit(strip, j);
                        if ((_actors[j].Bottom - _actors[j].Top) >= 0)
                            Gdi.ResetBackground(_actors[j].Top, _actors[j].Bottom, i);
                    }
                }
            }

            for (int i = 0; i < _actors.Length; i++)
            {
                _actors[i].NeedBackgroundReset = false;
            }
        }

        void SetActorRedrawFlags()
        {
            // Redraw all actors if a full redraw was requested.
            // Also redraw all actors in COMI (see bug #1066329 for details).
            if (_fullRedraw)
            {
                for (int j = 0; j < _actors.Length; j++)
                {
                    _actors[j].NeedRedraw = true;
                }
            }
            else
            {
                for (int i = 0; i < Gdi.NumStrips; i++)
                {
                    int strip = _screenStartStrip + i;
                    if (TestGfxAnyUsageBits(strip))
                    {
                        for (int j = 1; j < _actors.Length; j++)
                        {
                            if (TestGfxUsageBit(strip, j) && TestGfxOtherUsageBits(strip, j))
                            {
                                _actors[j].NeedRedraw = true;
                            }
                        }
                    }
                }
            }
        }

        int GetActorFromPos(Point p)
        {
            for (int i = 1; i < _actors.Length; i++)
            {
                if (!GetClass(i, ObjectClass.Untouchable) && p.Y >= _actors[i].Top &&
                    p.Y <= _actors[i].Bottom)
                {
                    return i;
                }
            }

            return 0;
        }

        int GetObjActToObjActDist(int a, int b)
        {
            Actor acta = null;
            Actor actb = null;

            if (a < _actors.Length)
                acta = _actors[a];

            if (b < _actors.Length)
                actb = _actors[b];

            if ((acta != null) && (actb != null) && (acta.Room == actb.Room) && (acta.Room != 0) && !acta.IsInCurrentRoom)
                return 0;

            Point pA;
            if (!GetObjectOrActorXY(a, out pA))
                return 0xFF;

            Point pB;
            if (!GetObjectOrActorXY(b, out pB))
                return 0xFF;

            // Perform adjustXYToBeInBox() *only* if the first item is an
            // actor and the second is an object. This used to not check
            // whether the second item is a non-actor, which caused bug
            // #853874).
            if (acta != null && actb == null)
            {
                var r = acta.AdjustXYToBeInBox(pB);
                pB = r.Position;
            }

            // Now compute the distance between the two points
            return ScummMath.GetDistance(pA, pB);
        }

        #endregion Actors

        #region Objects
        byte[] GetObjectOrActorName(int num)
        {
            byte[] name;
            if (num < _actors.Length)
            {
                name = _actors[num].Name;
            }
            else if (_newNames.ContainsKey(num))
            {
                name = _newNames[num];
            }
            else
            {
                var obj = (from o in _invData
                           where o != null && o.Number == num
                           select o).FirstOrDefault();

                if (obj == null)
                {
                    obj = (from o in _objs
                           where o.Number == num
                           select o).FirstOrDefault();
                }
                if (obj != null && obj.Name != null)
                {
                    name = obj.Name;
                }
                else
                {
                    name = new byte[0];
                }
            }
            return name;
        }

        void UpdateObjectStates()
        {
            for (int i = 1; i < _objs.Length; i++)
            {
                if (_objs[i].Number > 0)
                    _objs[i].State = GetState(_objs[i].Number);
            }
        }

        byte GetState(int obj)
        {
            ScummHelper.AssertRange(0, obj, _numGlobalObjects - 1, "object");
            return _scumm.ObjectStateTable[obj];
        }

        void GetObjectOwner()
        {
            GetResult();
            SetResult(GetOwner(GetVarOrDirectWord(OpCodeParameter.Param1)));
        }

        internal bool GetObjectOrActorXY(int obj, out Point p)
        {
            p = new Point();

            if (ObjIsActor(obj))
            {
                var act = _actors [ObjToActor(obj)];
                if (act != null && act.IsInCurrentRoom)
                {
                    p = act.Position;
                    return true;
                }
                return false;
            }

            switch (GetWhereIsObject(obj))
            {
                case WhereIsObject.NotFound:
                    return false;
                    case WhereIsObject.Inventory:
                    if (ObjIsActor(_scumm.ObjectOwnerTable[obj]))
                    {
                        var act = _actors[_scumm.ObjectOwnerTable[obj]];
                        if (act != null && act.IsInCurrentRoom)
                        {
                            p = act.Position;
                            return true;
                        }
                    }
                    return false;
            }

            int dir;
            GetObjectXYPos(obj, out p, out dir);
            return true;
        }

        int ObjToActor(int obj)
        {
            return obj;
        }

        bool ObjIsActor(int obj)
        {
            return obj < _actors.Length;
        }

        internal bool GetClass(int obj, ObjectClass cls)
        {
            cls &= (ObjectClass)0x7F;

            // Translate the new (V5) object classes to the old classes
            // (for those which differ).
            switch (cls)
            {
                case ObjectClass.Untouchable:
                    cls = (ObjectClass)24;
                    break;

                    case ObjectClass.Player:
                    cls = (ObjectClass)23;
                    break;

                    case ObjectClass.XFlip:
                    cls = (ObjectClass)19;
                    break;

                    case ObjectClass.YFlip:
                    cls = (ObjectClass)18;
                    break;
            }

            return (_scumm.ClassData[obj] & (1 << ((int)cls - 1))) != 0;
        }

        void SetObjectName(int obj)
        {
            if (obj < _actors.Length)
            {
                string msg = string.Format("Can't set actor {0} name with new name.", obj);
                throw new NotSupportedException(msg);
            }

            _newNames[obj] = ReadCharacters();
            RunInventoryScript(0);
        }

        int GetObjX(int obj)
        {
            if (obj < 1)
                return 0;                                   /* fix for indy4's map */

            if (obj < _actors.Length)
            {
                return _actors [obj].Position.X;
            }

            if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                return -1;

            Point p;
            GetObjectOrActorXY(obj, out p);
            return p.X;
        }

        int GetObjY(int obj)
        {
            if (obj < 1)
                return 0;                                   /* fix for indy4's map */

            if (obj < _actors.Length)
            {
                return _actors [obj].Position.Y;
            }
            if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                return -1;

            Point p;
            GetObjectOrActorXY(obj, out p);
            return p.Y;
        }

        WhereIsObject GetWhereIsObject(int obj)
        {
            int i;

            if (obj >= _numGlobalObjects)
                return WhereIsObject.NotFound;

            if (obj < 1)
                return WhereIsObject.NotFound;

            if (_scumm.ObjectOwnerTable[obj] != OwnerRoom)
            {
                for (i = 0; i < NumInventory; i++)
                    if (_inventory[i] == obj)
                        return WhereIsObject.Inventory;
                return WhereIsObject.NotFound;
            }

            for (i = (_objs.Length - 1); i > 0; i--)
                if (_objs[i].Number == obj)
            {
                if (_objs[i].FlObjectIndex != 0)
                    return WhereIsObject.FLObject;
                return WhereIsObject.Room;
            }

            return WhereIsObject.NotFound;
        }

        void PutState(int obj, byte state)
        {
            ScummHelper.AssertRange(0, obj, _numGlobalObjects - 1, "object");
            ScummHelper.AssertRange(0, state, 0xFF, "state");
            _scumm.ObjectStateTable[obj] = state;
        }

        int GetObjectIndex(int obj)
        {
            if (obj < 1)
                return -1;

            for(int i = (_objs.Length - 1); i > 0; i--)
            {
                if (_objs[i].Number == obj)
                    return i;
            }
            return -1;
        }

        void AddObjectToDrawQue(byte obj)
        {
            _drawingObjects.Add(_objs[obj]);
        }

        void ClearDrawObjectQueue()
        {
            _drawingObjects.Clear();
        }

        void PutOwner(int obj, byte owner)
        {
            _scumm.ObjectOwnerTable[obj] = owner;
        }

        void PutClass(int obj, int cls, bool set)
        {
            ScummHelper.AssertRange(0, obj, _numGlobalObjects - 1, "object");
            var cls2 = (ObjectClass)(cls & 0x7F);
            ScummHelper.AssertRange(1, (int)cls2, 32, "class");

            // Translate the new (V5) object classes to the old classes
            // (for those which differ).
            switch (cls2)
            {
                case ObjectClass.Untouchable:
                    cls2 = (ObjectClass)24;
                    break;

                    case ObjectClass.Player:
                    cls2 = (ObjectClass)23;
                    break;

                    case ObjectClass.XFlip:
                    cls2 = (ObjectClass)19;
                    break;

                    case ObjectClass.YFlip:
                    cls2 = (ObjectClass)18;
                    break;
            }

            if (set)
                ClassData[obj] |= (uint)(1 << ((int)cls2 - 1));
            else
                ClassData[obj] &= (uint)~(1 << ((int)cls2 - 1));

            if (obj >= 0 && obj < _actors.Length)
            {
                _actors[obj].ClassChanged(cls2, set);
            }
        }

        int GetOwner(int obj)
        {
            return _scumm.ObjectOwnerTable[obj];
        }

        int FindObject(int x, int y)
        {
            byte a;
            int mask = 0xF;

            for (int i = 1; i < _objs.Length; i++)
            {
                if ((_objs[i].Number < 1) || GetClass(_objs[i].Number, ObjectClass.Untouchable))
                    continue;

                var b = i;
                do
                {
                    a = _objs[b].ParentState;
                    b = _objs[b].Parent;
                    if (b == 0)
                    {
                        if (_objs[i].Position.X <= x && (_objs[i].Width + _objs[i].Position.X) > x &&
                            _objs[i].Position.Y <= y && (_objs[i].Height + _objs[i].Position.Y) > y)
                        {
                            return _objs[i].Number;
                        }
                        break;
                    }
                } while ((_objs[b].State & mask) == a);
            }

            return 0;
        }

        void GetObjectXYPos(int obj, out Point p, out int dir)
        {
            var idx = GetObjectIndex(obj);

            var od = _objs[idx];
            p = new Point(od.WalkX, od.WalkY);

            dir = ScummHelper.OldDirToNewDir(od.ActorDir & 3);
        }

        Point GetObjectXYPos(int obj)
        {
            int dir;
            Point p;
            GetObjectXYPos(obj, out p, out dir);
            return p;
        }

        void DrawRoomObjects(int argument)
        {
            const int mask = 0xF;
            for (int i = (_objs.Length - 1); i > 0; i--)
            {
                if (_objs[i].Number > 0 && ((_objs[i].State & mask) != 0))
                {
                    DrawRoomObject(i, argument);
                }
            }
        }

        void DrawRoomObject(int i, int argument)
        {
            ObjectData od;
            byte a;
            const int mask = 0xF;

            od = _objs[i];
            if ((i < 1) || (od.Number < 1) || od.State == 0)
            {
                return;
            }
            do
            {
                a = od.ParentState;
                if (od.Parent == 0)
                {
                    if (od.FlObjectIndex == 0)
                        DrawObject(i, argument);
                    break;
                }
                od = _objs[od.Parent];
            } while ((od.State & mask) == a);
        }

        void DrawObject(int obj, int arg)
        {
            ObjectData od = _objs[obj];
            int height, width;

            int x, a, numstrip;
            int tmp;

            if (_bgNeedsRedraw)
                arg = 0;

            if (od.Number == 0)
                return;

            ScummHelper.AssertRange(0, od.Number, _numGlobalObjects - 1, "object");

            int xpos = (od.Position.X / 8);
            int ypos = (int)od.Position.Y;

            width = od.Width / 8;
            height = (ushort)(od.Height &= 0xFFF8); // Mask out last 3 bits

            // Short circuit for objects which aren't visible at all.
            if (width == 0 || xpos > _screenEndStrip || xpos + width < _screenStartStrip)
                return;

            var ptr = (from o in roomData.Objects
                       where o.Number == od.Number
                       select o).First().Image;
            if (ptr == null || ptr.Length == 0)
                return;

            x = 0xFFFF;

            for (a = numstrip = 0; a < width; a++)
            {
                tmp = xpos + a;
                if (tmp < _screenStartStrip || _screenEndStrip < tmp)
                    continue;
                if (arg > 0 && _screenStartStrip + arg <= tmp)
                    continue;
                if (arg < 0 && tmp <= _screenEndStrip + arg)
                    continue;
                SetGfxUsageBit(tmp, UsageBitDirty);
                if (tmp < x)
                    x = tmp;
                numstrip++;
            }

            if (numstrip != 0)
            {
                DrawBitmaps flags = od.Flags | DrawBitmaps.ObjectMode;

                Gdi.DrawBitmap(ptr, _mainVirtScreen, x, ypos, width * 8, height, x - xpos, numstrip, flags);
            }
        }

        void ProcessDrawQueue()
        {
            foreach (var obj in _drawingObjects)
            {
                var index = Array.IndexOf(_objs, obj);
                DrawObject(index, 0);
            }
            ClearDrawObjectQueue();
        }

        void SetOwnerOf(int obj, int owner)
        {
            // In Sam & Max this is necessary, or you won't get your stuff back
            // from the Lost and Found tent after riding the Cone of Tragedy. But
            // it probably applies to all V6+ games. See bugs #493153 and #907113.
            // FT disassembly is checked, behavior is correct. [sev]
            int arg = 0;

            // WORKAROUND for bug #1917981: Game crash when finishing Indy3 demo.
            // Script 94 tries to empty the inventory but does so in a bogus way.
            // This causes it to try to remove object 0 from the inventory.
            if (owner == 0)
            {
                ClearOwnerOf(obj);

                if (_currentScript != 0xFF)
                {
                    var ss = _slots[_currentScript];
                    if (ss.Where == WhereIsObject.Inventory)
                    {
                        if (ss.Number < NumInventory && _inventory[ss.Number] == obj)
                        {
                            //throw new NotSupportedException("Odd setOwnerOf case #1: Please report to Fingolfin where you encountered this");
                            PutOwner(obj, 0);
                            RunInventoryScript(arg);
                            StopObjectCode();
                            return;
                        }
                        if (ss.Number == obj)
                            throw new NotSupportedException("Odd setOwnerOf case #2: Please report to Fingolfin where you encountered this");
                    }
                }
            }

            PutOwner(obj, (byte)owner);
            RunInventoryScript(arg);
        }

        void ClearOwnerOf(int obj)
        {
            int i;

            // Stop the associated object script code (else crashes might occurs)
            StopObjectScript((ushort)obj);

            // If the object is "owned" by a the current room, we scan the
            // object list and (only if it's a floating object) nuke it.
            if (GetOwner(obj) == OwnerRoom)
            {
                for (i = 0; i < _objs.Length; i++)
                {
                    if (_objs[i].Number == obj && _objs[i].FlObjectIndex != 0)
                    {
                        // Removing an flObject from a room means we can nuke it
                        _objs[i].Number = 0;
                        _objs[i].FlObjectIndex = 0;
                    }
                }
            }
            else
            {
                // Alternatively, scan the inventory to see if the object is in there...
                for (i = 0; i < NumInventory; i++)
                {
                    if (_inventory[i] == obj)
                    {
                        // Found the object! Nuke it from the inventory.
                        _inventory[i] = 0;

                        // Now fill up the gap removing the object from the inventory created.
                        for (i = 0; i < NumInventory - 1; i++)
                        {
                            if (_inventory[i] == 0 && _inventory[i + 1] != 0)
                            {
                                _inventory[i] = _inventory[i + 1];
                                _invData[i] = _invData[i + 1];
                                _inventory[i + 1] = 0;
                                // FIXME FIXME FIXME: This is incomplete, as we do not touch flags, status... BUG
                                // TODO:
                                //_res->_types[rtInventory][i]._address = _res->_types[rtInventory][i + 1]._address;
                                //_res->_types[rtInventory][i]._size = _res->_types[rtInventory][i + 1]._size;
                                //_res->_types[rtInventory][i + 1]._address = NULL;
                                //_res->_types[rtInventory][i + 1]._size = 0;
                            }
                        }
                        break;
                    }
                }
            }
        }

        #endregion Objects

        #region Charset

        void InitCharset(int charsetNum)
        {
            _string[0].Default.Charset = (byte)charsetNum;
            _string[1].Default.Charset = (byte)charsetNum;

            //if (_charsets[charsetNum] != null)
            //{
            //    Array.Copy(_charsets[charsetNum], _charsetColorMap, 16);
            //}
        }

        void LoadCharset(int resId)
        {
            var diskName = string.Format("{0}.lfl", 900 + resId);
            var path = Path.Combine(_directory, diskName);
            using (var br = new BinaryReader(File.OpenRead(path)))
            {
                var size = (int)br.ReadUInt32() + 11;
                _charsets[resId] = br.ReadBytes(size);
            }
        }

        void Charset()
        {
            if (_haveMsg == 0)
                return;

            // Do nothing while the camera is moving
            if ((_camera.DestinationPosition.X / 8) != (_camera.CurrentPosition.X / 8) || _camera.CurrentPosition.X != _camera.LastPosition.X)
                return;

            Actor a = null;
            if (TalkingActor != 0xFF)
                a = _actors[TalkingActor];

            if (a != null && _string[0].Overhead)
            {
                int s;

                _string[0].XPos = (short)(a.Position.X - MainVirtScreen.XStart);
                _string[0].YPos = (short)(a.Position.Y - a.Elevation - ScreenTop);

                if (_variables[VariableTalkStringY] < 0)
                {
                    s = (a.ScaleY * _variables[VariableTalkStringY]) / 0xFF;
                    _string[0].YPos += (short)(((_variables[VariableTalkStringY] - s) / 2) + s);
                }
                else
                {
                    _string[0].YPos = (short)_variables[VariableTalkStringY];
                }

                if (_string[0].YPos < 1)
                    _string[0].YPos = 1;

                if (_string[0].XPos < 80)
                    _string[0].XPos = 80;
                if (_string[0].XPos > ScreenWidth - 80)
                    _string[0].XPos = (short)(ScreenWidth - 80);
            }

            _charset.Top = _string[0].YPos + ScreenTop;
            _charset.StartLeft = _charset.Left = _string[0].XPos;
            _charset.Right = _string[0].Right;
            _charset.Center = _string[0].Center;
            _charset.SetColor(_charsetColor);

            if (a != null && a.Charset != 0)
                _charset.SetCurID(a.Charset);
            else
                _charset.SetCurID(_string[0].Charset);

            if (_talkDelay != 0)
                return;

            if (_haveMsg == 1)
            {
                // TODO:
                //if ((_sound->_sfxMode & 2) == 0)
                StopTalk();
                return;
            }

            if (a != null && !_string[0].NoTalkAnim)
            {
                a.RunTalkScript(a.TalkStartFrame);
                _useTalkAnims = true;
            }

            _talkDelay = 60;

            if (!_keepText)
            {
                RestoreCharsetBg();
            }

            int maxwidth = _charset.Right - _string[0].XPos - 1;
            if (_charset.Center)
            {
                if (maxwidth > _nextLeft)
                    maxwidth = _nextLeft;
                maxwidth *= 2;
            }

            _charset.AddLinebreaks(0, _charsetBuffer, _charsetBufPos, maxwidth);

            if (_charset.Center)
            {
                _nextLeft -= _charset.GetStringWidth(0, _charsetBuffer, _charsetBufPos) / 2;
                if (_nextLeft < 0)
                    _nextLeft = 0;
            }

            _charset.DisableOffsX = _charset.FirstChar = !_keepText;

            int c = 0;
            while (HandleNextCharsetCode(a, ref c))
            {
                if (c == 0)
                {
                    // End of text reached, set _haveMsg accordingly
                    _haveMsg = 1;
                    _keepText = false;
                    break;
                }

                if (c == 13)
                {
                    if (!NewLine())
                        break;
                    continue;
                }

                _charset.Left = _nextLeft;
                _charset.Top = _nextTop;

                _charset.PrintChar(c, false);
                _nextLeft = _charset.Left;
                _nextTop = _charset.Top;

                _talkDelay += _variables[VariableCharIncrement];
            }
        }

        bool NewLine()
        {
            _nextLeft = _string[0].XPos;
            if (_charset.Center)
            {
                _nextLeft -= _charset.GetStringWidth(0, _charsetBuffer, _charsetBufPos) / 2;
                if (_nextLeft < 0)
                    _nextLeft = 0;
            }

            if (_string[0].Height != 0)
            {
                _nextTop += _string[0].Height;
            }
            else
            {
                bool useCJK = UseCjkMode;
                _nextTop += _charset.GetFontHeight();
                UseCjkMode = useCJK;
            }

            // FIXME: is this really needed?
            _charset.DisableOffsX = true;

            return true;
        }

        bool HandleNextCharsetCode(Actor a, ref int code)
        {
            int color, frme, c = 0, oldy;
            bool endLoop = false;
            //byte* buffer = _charsetBuffer + _charsetBufPos;
            int bufferPos = _charsetBufPos;
            while (!endLoop)
            {
                c = _charsetBuffer[bufferPos++];
                if (!(c == 0xFF || (c == 0xFE)))
                {
                    break;
                }
                c = _charsetBuffer[bufferPos++];

                if (NewLineCharacter != 0 && c == NewLineCharacter)
                {
                    c = 13;
                    break;
                }

                switch (c)
                {
                    case 1:
                        c = 13; // new line
                        endLoop = true;
                        break;

                    case 2:
                        _haveMsg = 0;
                        _keepText = true;
                        endLoop = true;
                        break;

                    case 3:
                        _haveMsg = 0xFF;
                        _keepText = false;
                        endLoop = true;
                        break;

                    case 8:
                        // Ignore this code here. Occurs e.g. in MI2 when you
                        // talk to the carpenter on scabb island. It works like
                        // code 1 (=newline) in verb texts, but is ignored in
                        // spoken text (i.e. here). Used for very long verb
                        // sentences.
                        break;

                    case 9:
                        frme = _charsetBuffer[bufferPos] | (_charsetBuffer[bufferPos + 1] << 8);
                        bufferPos += 2;
                        if (a != null)
                            a.StartAnimActor((byte)frme);
                        break;

                    case 10:
                        // Note the similarity to the code in debugMessage()
                        //talk_sound_a = (uint)(_charsetBuffer[bufferPos] | (_charsetBuffer[bufferPos + 1] << 8) | (_charsetBuffer[bufferPos + 4] << 16) | (_charsetBuffer[bufferPos + 5] << 24));
                        //talk_sound_b = (uint)(_charsetBuffer[bufferPos + 8] | (_charsetBuffer[bufferPos + 9] << 8) | (_charsetBuffer[bufferPos + 12] << 16) | (_charsetBuffer[bufferPos + 13] << 24));
                        bufferPos += 14;
                        //_sound->talkSound(talk_sound_a, talk_sound_b, 2);
                        _haveActorSpeechMsg = false;
                        break;

                    case 12:
                        color = _charsetBuffer[bufferPos] | (_charsetBuffer[bufferPos + 1] << 8);
                        bufferPos += 2;
                        if (color == 0xFF)
                            _charset.SetColor(_charsetColor);
                        else
                            _charset.SetColor((byte)color);
                        break;

                    case 13:
                        //debug(0, "handleNextCharsetCode: Unknown opcode 13 %d", READ_LE_UINT16(buffer));
                        bufferPos += 2;
                        break;

                    case 14:
                        oldy = _charset.GetFontHeight();
                        _charset.SetCurID(_charsetBuffer[bufferPos++]);
                        bufferPos += 2;
                        // TODO:
                        //memcpy(_charsetColorMap, _charsetData[_charset.getCurID()], 4);
                        _nextTop -= _charset.GetFontHeight() - oldy;
                        break;

                    default:
                        throw new NotSupportedException(string.Format("handleNextCharsetCode: invalid code {0}", c));
                }
            }
            _charsetBufPos = bufferPos;
            code = c;
            return (c != 2 && c != 3);
        }

        void RestoreCharsetBg()
        {
            _nextLeft = _string[0].XPos;
            _nextTop = _string[0].YPos + ScreenTop;

            if (_charset.HasMask)
            {
                _charset.HasMask = false;
                _charset.Str.Left = -1;
                _charset.Left = -1;

                // Restore background on the whole text area. This code is based on
                // restoreBackground(), but was changed to only restore those parts which are
                // currently covered by the charset mask.

                var vs = _charset.TextScreen;
                if (vs.Height == 0)
                    return;

                MarkRectAsDirty(vs, 0, vs.Width, 0, vs.Height, UsageBitRestored);

                if (vs.HasTwoBuffers && _currentRoom != 0 && IsLightOn())
                {
                    if (vs != MainVirtScreen)
                    {
                        // Restore from back buffer
                        var screenBufNav = new PixelNavigator(vs.Surfaces[0]);
                        screenBufNav.OffsetX(vs.XStart);
                        var backNav = new PixelNavigator(vs.Surfaces[1]);
                        backNav.OffsetX(vs.XStart);
                        Gdi.Blit(screenBufNav, backNav, vs.Width, vs.Height);
                    }
                }
                else
                {
                    // Clear area
                    var screenBuf = vs.Surfaces[0].Pixels;
                    Array.Clear(screenBuf, 0, screenBuf.Length);
                }

                if (vs.HasTwoBuffers)
                {
                    // Clean out the charset mask
                    ClearTextSurface();
                }
            }
        }

        #endregion Charset

        #region Drawing Methods

        Palette _currentPalette = new Palette();

        void RestoreBackground(Rect rect, byte backColor)
        {
            VirtScreen vs;

            if (rect.Top < 0)
                rect.Top = 0;
            if (rect.Left >= rect.Right || rect.Top >= rect.Bottom)
                return;

            if ((vs = FindVirtScreen(rect.Top)) == null)
                return;

            if (rect.Left > vs.Width)
                return;

            // Convert 'rect' to local (virtual screen) coordinates
            rect.Top -= vs.TopLine;
            rect.Bottom -= vs.TopLine;

            rect.Clip(vs.Width, vs.Height);

            int height = rect.Height;
            int width = rect.Width;

            MarkRectAsDirty(vs, rect.Left, rect.Right, rect.Top, rect.Bottom, UsageBitRestored);

            var screenBuf = new PixelNavigator(vs.Surfaces[0]);
            screenBuf.GoTo(vs.XStart + rect.Left, rect.Top);

            if (height == 0)
                return;

            if (vs.HasTwoBuffers && _currentRoom != 0 && IsLightOn())
            {
                var back = new PixelNavigator(vs.Surfaces[1]);
                back.GoTo(vs.XStart + rect.Left, rect.Top);
                Gdi.Blit(screenBuf, back, width, height);
                if (vs == MainVirtScreen && _charset.HasMask)
                {
                    var mask = new PixelNavigator(_textSurface);
                    mask.GoTo(rect.Left, rect.Top - ScreenTop);
                    Gdi.Fill(mask, CharsetMaskTransparency, width * _textSurfaceMultiplier, height * _textSurfaceMultiplier);
                }
            }
            else
            {
                Gdi.Fill(screenBuf, backColor, width, height);
            }
        }

        void DrawVerbBitmap(int verb, int x, int y)
        {
            var vst=_verbs[verb];
            var vs=FindVirtScreen(y);

            if(vs==null) return;

            Gdi.IsZBufferEnabled=false;

            var hasTwoBufs = vs.HasTwoBuffers;
            //vs.HasTwoBuffers=false;

            int xStrip=x/8;
            int yDiff=y-vs.TopLine;

            for (int i = 0; i < vst.ImageWidth/8; i++)
            {
                Gdi.DrawBitmap(vst.Image,vs,xStrip+i,yDiff,
                               vst.ImageWidth,vst.ImageHeight,
                               i,1,DrawBitmaps.AllowMaskOr| DrawBitmaps.ObjectMode);
            }

            vst.CurRect.Right=vst.CurRect.Left+vst.ImageWidth;
            vst.CurRect.Bottom=vst.CurRect.Top+vst.ImageHeight;
            vst.OldRect=vst.CurRect;

            Gdi.IsZBufferEnabled=true;
            //vs.HasTwoBuffers=hasTwoBufs;
        }

        void DrawString(int a, byte[] msg)
        {
            var buf = new byte[270];
            int i, c;
            int fontHeight;
            uint color;

            ConvertMessageToString(msg, buf, 0);

            _charset.Top = _string[a].YPos + ScreenTop;
            _charset.StartLeft = _charset.Left = _string[a].XPos;
            _charset.Right = _string[a].Right;
            _charset.Center = _string[a].Center;
            _charset.SetColor(_string[a].Color);
            _charset.DisableOffsX = _charset.FirstChar = true;
            _charset.SetCurID(_string[a].Charset);

            fontHeight = _charset.GetFontHeight();

            // trim from the right
            int tmpPos = 0;
            int spacePos = 0;
            while (buf[tmpPos] != 0)
            {
                if (buf[tmpPos] == ' ')
                {
                    if (spacePos == 0)
                        spacePos = tmpPos;
                }
                else
                {
                    spacePos = 0;
                }
                tmpPos++;
            }
            if (spacePos != 0)
            {
                buf[spacePos] = 0;
            }

            if (_charset.Center)
            {
                _charset.Left -= _charset.GetStringWidth(a, buf, 0) / 2;
            }

            if (buf[0] == 0)
            {
                _charset.Str.Left = _charset.Left;
                _charset.Str.Top = _charset.Top;
                _charset.Str.Right = _charset.Left;
                _charset.Str.Bottom = _charset.Top;
            }

            for (i = 0; (c = buf[i++]) != 0; )
            {
                if (c == 0xFF || (c == 0xFE))
                {
                    c = buf[i++];
                    switch (c)
                    {
                        case 9:
                        case 10:
                        case 13:
                        case 14:
                            i += 2;
                        break;

                        case 1:
                        case 8:
                            if (_charset.Center)
                            {
                                _charset.Left = _charset.StartLeft - _charset.GetStringWidth(a, buf, i);
                            }   
                            else
                            {
                                _charset.Left = _charset.StartLeft;
                            }
                            if (_string[0].Height != 0)
                            {
                                _nextTop += _string[0].Height;
                            }
                            else
                            {
                                _charset.Top += fontHeight;
                            }
                            break;

                        case 12:
                            color = (uint)(buf[i] + (buf[i + 1] << 8));
                            i += 2;
                            if (color == 0xFF)
                                _charset.SetColor(_string[a].Color);
                            else
                                _charset.SetColor((byte)color);
                            break;
                    }
                }
                else
                {
                    //if ((c & 0x80) != 0 && _useCJKMode)
                    //{
                    //    if (checkSJISCode(c))
                    //        c += buf[i++] * 256;
                    //}
                    _charset.PrintChar(c, true);
                    _charset.BlitAlso = false;
                }
            }

            if (a == 0)
            {
                _nextLeft = _charset.Left;
                _nextTop = _charset.Top;
            }

            _string[a].XPos = (short)_charset.Str.Right;
        }

        void SetDirtyColors(int min, int max)
        {
            if (_palDirtyMin > min)
                _palDirtyMin = min;
            if (_palDirtyMax < max)
                _palDirtyMax = max;
        }

        void DrawBox(int x, int y, int x2, int y2, int color)
        {
            VirtScreen vs;

            if ((vs = FindVirtScreen(y)) == null)
                return;

            if (x > x2)
                ScummHelper.Swap(ref x, ref x2);

            if (y > y2)
                ScummHelper.Swap(ref y, ref y2);

            x2++;
            y2++;

            // Adjust for the topline of the VirtScreen
            y -= vs.TopLine;
            y2 -= vs.TopLine;

            // Clip the coordinates
            if (x < 0)
                x = 0;
            else if (x >= vs.Width)
                return;

            if (x2 < 0)
                return;
            if (x2 > vs.Width)
                x2 = vs.Width;

            if (y < 0)
                y = 0;
            else if (y > vs.Height)
                return;

            if (y2 < 0)
                return;

            if (y2 > vs.Height)
                y2 = vs.Height;

            int width = x2 - x;
            int height = y2 - y;

            // This will happen in the Sam & Max intro - see bug #1039162 - where
            // it would trigger an assertion in blit().

            if (width <= 0 || height <= 0)
                return;

            MarkRectAsDirty(vs, x, x2, y, y2);

            var backbuff = new PixelNavigator(vs.Surfaces[0]);
            backbuff.GoTo(vs.XStart + x, y);

            // A check for -1 might be wrong in all cases since o5_drawBox() in its current form
            // is definitely not capable of passing a parameter of -1 (color range is 0 - 255).
            // Just to make sure I don't break anything I restrict the code change to FM-Towns
            // version 5 games where this change is necessary to fix certain long standing bugs.
            if (color == -1)
            {
                if (vs != MainVirtScreen)
                    Console.Error.WriteLine("can only copy bg to main window");

                var bgbuff = new PixelNavigator(vs.Surfaces[1]);
                bgbuff.GoTo(vs.XStart + x, y);

                Gdi.Blit(backbuff, bgbuff, width, height);
                if (_charset.HasMask)
                {
                    var mask = new PixelNavigator(_textSurface);
                    mask.GoToIgnoreBytesByPixel(x * _textSurfaceMultiplier, (y - ScreenTop) * _textSurfaceMultiplier);
                    Gdi.Fill(mask, CharsetMaskTransparency, width * _textSurfaceMultiplier, height * _textSurfaceMultiplier);
                }
            }
            else
            {
                Gdi.Fill(backbuff, (byte)color, width, height);
            }
        }

        void UpdatePalette()
        {
            if (_palDirtyMax == -1)
                return;

            var colors = new Color[256];

            int first = _palDirtyMin;
            int num = _palDirtyMax - first + 1;

            for (int i = _palDirtyMin; i <= _palDirtyMax; i++)
            {
                var color = _currentPalette.Colors[_shadowPalette[i]];
                colors[i] = color;
            }

            _palDirtyMax = -1;
            _palDirtyMin = 256;

            _gfxManager.SetPalette(colors, first, num);
        }

        void HandleEffects()
        {
            CyclePalette();
            //PalManipulate();
            if (_doEffect)
            {
                _doEffect = false;
                FadeIn(_newEffect);
                // TODO:
                //clearClickedStatus();
            }
        }

        void HandleMouseOver()
        {
            if (_completeScreenRedraw)
            {
                VerbMouseOver(0);
            }
            else
            {
                if (_cursor.State > 0)
                {
                    var pos = _inputManager.GetMousePosition();
                    VerbMouseOver(FindVerbAtPos((int)pos.X, (int)pos.Y));
                }
            }
        }

        void ClearTextSurface()
        {
            Gdi.Fill(_textSurface.Pixels, _textSurface.Pitch, CharsetMaskTransparency, _textSurface.Width, _textSurface.Height);
        }

        void HandleDrawing()
        {
            if (_camera.CurrentPosition != _camera.LastPosition || _bgNeedsRedraw || _fullRedraw)
            {
                RedrawBGAreas();
            }

            ProcessDrawQueue();

            _fullRedraw = false;
        }

        /// <summary>
        /// Redraw background as needed, i.e. the left/right sides if scrolling took place etc.
        /// Note that this only updated the virtual screen, not the actual display.
        /// </summary>
        void RedrawBGAreas()
        {
            if (_game.Id != "pass" && _game.Version >= 4 && _game.Version <= 6)
            {
                // Starting with V4 games (with the exception of the PASS demo), text
                // is drawn over the game graphics (as  opposed to be drawn in a
                // separate region of the screen). So, when scrolling in one of these
                // games (pre-new camera system), if actor text is visible (as indicated
                // by the _hasMask flag), we first remove it before proceeding.
                if (_camera.CurrentPosition.X != _camera.LastPosition.X && _charset.HasMask)
                {
                    StopTalk();
                }
            }

            // Redraw parts of the background which are marked as dirty.
            if (!_fullRedraw && _bgNeedsRedraw)
            {
                for (int i = 0; i != Gdi.NumStrips; i++)
                {
                    if (TestGfxUsageBit(_screenStartStrip + i, UsageBitDirty))
                    {
                        RedrawBGStrip(i, 1);
                    }
                }
            }

            int val = 0;
            var diff = _camera.CurrentPosition.X - _camera.LastPosition.X;
            if (!_fullRedraw && diff == 8)
            {
                val = -1;
                RedrawBGStrip(Gdi.NumStrips - 1, 1);
            }
            else if (!_fullRedraw && diff == -8)
            {
                val = +1;
                RedrawBGStrip(0, 1);
            }
            else if (_fullRedraw || diff != 0)
            {
                // TODO: ClearFlashlight
                //ClearFlashlight();
                _bgNeedsRedraw = false;
                RedrawBGStrip(0, Gdi.NumStrips);
            }
            DrawRoomObjects(val);
            _bgNeedsRedraw = false;
        }

        void RedrawBGStrip(int start, int num)
        {
            int s = _screenStartStrip + start;

            for (int i = 0; i < num; i++)
                SetGfxUsageBit(s + i, UsageBitDirty);

            Gdi.DrawBitmap(roomData.Data, _mainVirtScreen, s, 0, roomData.Header.Width, _mainVirtScreen.Height, s, num, 0);
        }

        void HandleShaking()
        {
            if (_shakeEnabled)
            {
                _shakeFrame = (_shakeFrame + 1) % ShakePositions.Length;
                _gfxManager.SetShakePos(ShakePositions[_shakeFrame]);
            }
            else if (!_shakeEnabled && _shakeFrame != 0)
            {
                _shakeFrame = 0;
                _gfxManager.SetShakePos(0);
            }
        }

        void DrawDirtyScreenParts()
        {
            // Update verbs
            UpdateDirtyScreen(_verbVirtScreen);

            // Update the conversation area (at the top of the screen)
            UpdateDirtyScreen(_textVirtScreen);

            // Update game area ("stage")
            if (_camera.LastPosition.X != _camera.CurrentPosition.X)
            {
                // Camera moved: redraw everything
                DrawStripToScreen(_mainVirtScreen, 0, _mainVirtScreen.Width, 0, _mainVirtScreen.Height);
                _mainVirtScreen.SetDirtyRange(_mainVirtScreen.Height, 0);
            }
            else
            {
                UpdateDirtyScreen(_mainVirtScreen);
            }

            // Handle shaking
            HandleShaking();
        }

        void UpdateDirtyScreen(VirtScreen vs)
        {
            // Do nothing for unused virtual screens
            if (vs.Height == 0)
                return;

            int i;
            int w = 8;
            int start = 0;

            for (i = 0; i < Gdi.NumStrips; i++)
            {
                if (vs.BDirty[i] != 0)
                {
                    int top = vs.TDirty[i];
                    int bottom = vs.BDirty[i];
                    vs.TDirty[i] = vs.Height;
                    vs.BDirty[i] = 0;
                    if (i != (Gdi.NumStrips - 1) && vs.BDirty[i + 1] == bottom && vs.TDirty[i + 1] == top)
                    {
                        // Simple optimizations: if two or more neighboring strips
                        // form one bigger rectangle, coalesce them.
                        w += 8;
                        continue;
                    }
                    DrawStripToScreen(vs, start * 8, w, top, bottom);
                    w = 8;
                }
                start = i + 1;
            }
        }

        /// <summary>
        /// Blit the specified rectangle from the given virtual screen to the display.
        /// Note: t and b are in *virtual screen* coordinates, while x is relative to
        /// the *real screen*. This is due to the way tdirty/vdirty work: they are
        /// arrays which map 'strips' (sections of the real screen) to dirty areas as
        /// specified by top/bottom coordinate in the virtual screen.
        /// </summary>
        /// <param name="vs"></param>
        /// <param name="x"></param>
        /// <param name="width"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        void DrawStripToScreen(VirtScreen vs, int x, int width, int top, int bottom)
        {
            // Short-circuit if nothing has to be drawn
            if (bottom <= top || top >= vs.Height)
                return;

            // Perform some clipping
            if (width > vs.Width - x)
                width = vs.Width - x;
            if (top < ScreenTop)
                top = ScreenTop;
            if (bottom > ScreenTop + ScreenHeight)
                bottom = ScreenTop + ScreenHeight;

            // Convert the vertical coordinates to real screen coords
            int y = vs.TopLine + top - ScreenTop;
            int height = bottom - top;

            if (width <= 0 || height <= 0)
                return;

            var srcNav = new PixelNavigator(vs.Surfaces[0]);
            srcNav.GoTo(vs.XStart + x, top);

            var compNav = new PixelNavigator(_composite);
            var txtNav = new PixelNavigator(_textSurface);
            int m = _textSurfaceMultiplier;
            txtNav.GoTo(x * m, y * m);

            var vsPitch = vs.Pitch - width * vs.BytesPerPixel;
            var textPitch = _textSurface.Pitch - width * m;

            for (int h = height * m; h > 0; --h)
            {
                for (int w = width * m; w > 0; w--)
                {
                    var temp = txtNav.Read();
                    int mask = temp ^ CharsetMaskTransparency;
                    mask = (((mask & 0x7f) + 0x7f) | mask) & 0x80;
                    mask = ((mask >> 7) + 0x7f) ^ 0x80;

                    var dst = ((temp ^ srcNav.Read()) & mask) ^ temp;
                    compNav.Write((byte)dst);

                    srcNav.OffsetX(1);
                    txtNav.OffsetX(1);
                    compNav.OffsetX(1);
                }

                srcNav.OffsetX(vsPitch);
                txtNav.OffsetX(textPitch);
            }

            var src = _composite.Pixels;

            // Finally blit the whole thing to the screen
            _gfxManager.CopyRectToScreen(src, width * vs.BytesPerPixel, x, y, width, height);
        }

        void MarkObjectRectAsDirty(int obj)
        {
            for (int i = 1; i < _objs.Length; i++)
            {
                if (_objs[i].Number == obj)
                {
                    if (_objs[i].Width != 0)
                    {
                        int minStrip = Math.Max(_screenStartStrip, _objs[i].Position.X / 8);
                        int maxStrip = Math.Min(_screenEndStrip + 1, _objs[i].Position.X / 8 + _objs[i].Width / 8);
                        for (int strip = minStrip; strip < maxStrip; strip++)
                        {
                            SetGfxUsageBit(strip, UsageBitDirty);
                        }
                    }
                    _bgNeedsRedraw = true;
                    return;
                }
            }
        }

        internal void MarkRectAsDirty(VirtScreen vs, int left, int right, int top, int bottom, int dirtybit = 0)
        {
            int lp, rp;

            if (left > right || top > bottom)
                return;
            if (top > vs.Height || bottom < 0)
                return;

            if (top < 0)
                top = 0;
            if (bottom > vs.Height)
                bottom = vs.Height;

            if (vs == MainVirtScreen && dirtybit != 0)
            {
                lp = left / 8 + _screenStartStrip;
                if (lp < 0)
                    lp = 0;

                rp = (right + vs.XStart) / 8;

                if (rp >= 200)
                    rp = 200;

                for (; lp <= rp; lp++)
                    SetGfxUsageBit(lp, dirtybit);
            }

            // The following code used to be in the separate method setVirtscreenDirty
            lp = left / 8;
            rp = right / 8;

            if ((lp >= Gdi.NumStrips) || (rp < 0))
                return;
            if (lp < 0)
                lp = 0;
            if (rp >= Gdi.NumStrips)
                rp = Gdi.NumStrips - 1;

            while (lp <= rp)
            {
                if (top < vs.TDirty[lp])
                    vs.TDirty[lp] = top;
                if (bottom > vs.BDirty[lp])
                    vs.BDirty[lp] = bottom;
                lp++;
            }
        }

        #region GfxUsageBit Members

        const int UsageBitDirty = 96;
        const int UsageBitRestored = 95;

        /// <summary>
        /// For each of the 410 screen strips, gfxUsageBits contains a
        /// bitmask. The lower 80 bits each correspond to one actor and
        /// signify if any part of that actor is currently contained in
        /// that strip.
        ///
        /// If the leftmost bit is set, the strip (background) is dirty
        /// needs to be redrawn.
        ///
        /// The second leftmost bit is set by removeBlastObject() and
        /// restoreBackground(), but I'm not yet sure why.
        /// </summary>
        uint[] _gfxUsageBits;

        void SetGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3)) throw new ArgumentOutOfRangeException("strip");
            if (bit < 1 || bit > 96) throw new ArgumentOutOfRangeException("bit");
            bit--;
            _gfxUsageBits[3 * strip + bit / 32] |= (uint)((1 << bit % 32));
        }

        void ClearGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3)) throw new ArgumentOutOfRangeException("strip");
            if (bit < 1 || bit > 96) throw new ArgumentOutOfRangeException("bit");
            bit--;
            _gfxUsageBits[3 * strip + bit / 32] &= (uint)~(1 << (bit % 32));
        }

        bool TestGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3)) throw new ArgumentOutOfRangeException("strip");
            if (bit < 0 || bit > 96) throw new ArgumentOutOfRangeException("bit");
            bit--;
            return (_gfxUsageBits[3 * strip + bit / 32] & (1 << (bit % 32))) != 0;
        }

        bool TestGfxOtherUsageBits(int strip, int bit)
        {
            // Don't exclude the DIRTY and RESTORED bits from the test
            var bitmask = new uint[3] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };

            ScummHelper.AssertRange(1, bit, 96, "TestGfxOtherUsageBits");
            bit--;
            bitmask[bit / 32] &= (uint)(~(1 << (bit % 32)));

            for (int i = 0; i < 3; i++)
                if ((_gfxUsageBits[3 * strip + i] & bitmask[i]) != 0)
                    return true;

            return false;
        }

        bool TestGfxAnyUsageBits(int strip)
        {
            // Exclude the DIRTY and RESTORED bits from the test
            var bitmask = new uint[3] { 0xFFFFFFFF, 0xFFFFFFFF, 0x3FFFFFFF };

            ScummHelper.AssertRange(0, strip, _gfxUsageBits.Length / 3, "TestGfxOtherUsageBits");
            for (var i = 0; i < 3; i++)
                if ((_gfxUsageBits[3 * strip + i] & bitmask[i]) != 0)
                    return true;

            return false;
        }

        #endregion GfxUsageBit Members

        internal PixelNavigator GetMaskBuffer(int x, int y, int z)
        {
            return Gdi.GetMaskBuffer((x + _mainVirtScreen.XStart) / 8, y, z);
        }

        internal VirtScreen FindVirtScreen(int y)
        {
            if (VirtScreenContains(_mainVirtScreen, y)) return _mainVirtScreen;
            if (VirtScreenContains(_textVirtScreen, y)) return _textVirtScreen;
            if (VirtScreenContains(_verbVirtScreen, y)) return _verbVirtScreen;
            if (VirtScreenContains(_unkVirtScreen, y)) return _unkVirtScreen; 

            return null;
        }

        bool VirtScreenContains(VirtScreen vs, int y)
        {
            return (y >= vs.TopLine && y < vs.TopLine + vs.Height);
        }

        #endregion Drawing Methods

        #region Verb Members

        int _verbMouseOver;

        void SetVerbObject(int obj, int verb)
        {
            for (int i = NumLocalObjects-1; i>0; i--)
            {
                if (_objs [i].Number == obj)
                {
                    var o = _objs[i];
                    _verbs[verb].ImageWidth = o.Width;
                    _verbs[verb].ImageHeight = o.Height;
                    _verbs[verb].Image = new byte[_objs [i].Image.Length];
                    Array.Copy(o.Image, _verbs[verb].Image, o.Image.Length);
                }
            }
        }

        void VerbMouseOver(int verb)
        {
            if (_verbMouseOver != verb)
            {
                if (_verbs[_verbMouseOver].Type != VerbType.Image)
                {
                    DrawVerb(_verbMouseOver, 0);
                    _verbMouseOver = verb;
                }

                if (_verbs[verb].Type != VerbType.Image && _verbs[verb].HiColor != 0)
                {
                    DrawVerb(verb, 1);
                    _verbMouseOver = verb;
                }
            }
        }

        void GetVerbEntrypoint()
        {
            int a, b;
            GetResult();
            a = GetVarOrDirectWord(OpCodeParameter.Param1);
            b = GetVarOrDirectWord(OpCodeParameter.Param2);

            SetResult(GetVerbEntrypoint(a, b));
        }

        int GetVerbEntrypoint(int obj, int entry)
        {
            if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                return 0;

            ObjectData result = null;

            if (_scumm.ObjectOwnerTable[obj] != OwnerRoom)
            {
                for (int i = 0; i < NumInventory; i++)
                {
                    if (_inventory[i] == obj)
                        result = _invData[i];
                }
            }
            else
            {
                result = (from o in _objs
                          where o.Number == obj
                          select o).FirstOrDefault();
            }

            foreach (var key in result.ScriptOffsets.Keys)
            {
                if (key == entry || key == 0xFF)
                    return result.ScriptOffsets[key];
            }

            return 0;
        }

        VerbSlot GetVerb(int num)
        {
            var verbSlot = (from verb in _verbs
                            where num == verb.VerbId && verb.Type == 0 && verb.SaveId == 0
                            select verb).FirstOrDefault();
            return verbSlot;
        }

        void DrawVerb(int verb, int mode)
        {
            if (verb == 0)
                return;

            var vs = _verbs[verb];
            if (vs.SaveId == 0 && vs.CurMode != 0 && vs.VerbId != 0)
            {
                if (vs.Type == VerbType.Image)
                {
                    DrawVerbBitmap(verb, vs.CurRect.Left, vs.CurRect.Top);
                    return;
                }

                RestoreVerbBG(verb);

                _string[4].Charset = vs.CharsetNr;
                _string[4].XPos = (short)vs.CurRect.Left;
                _string[4].YPos = (short)vs.CurRect.Top;
                _string[4].Right = (short)(ScreenWidth - 1);
                _string[4].Center = vs.Center;

                if (vs.CurMode == 2)
                    _string[4].Color = vs.DimColor;
                else if (mode != 0 && vs.HiColor != 0)
                    _string[4].Color = vs.HiColor;
                else
                    _string[4].Color = vs.Color;

                // FIXME For the future: Indy3 and under inv scrolling
                /*
                   if (verb >= 31 && verb <= 36)
                   verb += _inventoryOffset;
                 */
                byte[] msg = _verbs[verb].Text;
                if (msg==null || msg.Length == 0)
                    return;

                var tmp = _charset.Center;
                DrawString(4, msg);
                _charset.Center = tmp;

                vs.CurRect.Right = _charset.Str.Right;
                vs.CurRect.Bottom = _charset.Str.Bottom;
                vs.OldRect = _charset.Str;
                _charset.Str.Left = _charset.Str.Right;
            }
            else
            {
                RestoreVerbBG(verb);
            }
        }

        void RestoreVerbBG(int verb)
        {
            VerbSlot vs = _verbs[verb];
            byte col = vs.BkColor;

            if (vs.OldRect.Left != -1)
            {
                RestoreBackground(vs.OldRect, col);
                vs.OldRect.Left = -1;
            }
        }

        void KillVerb(int slot)
        {
            if (slot == 0)
                return;

            VerbSlot vs = _verbs[slot];
            vs.VerbId = 0;
            vs.CurMode = 0;
            vs.Text = null;

            if (vs.SaveId == 0)
            {
                DrawVerb(slot, 0);
                VerbMouseOver(0);
            }
            vs.SaveId = 0;
        }

        int FindVerbAtPos(int x, int y)
        {
            for (int i = _verbs.Length - 1; i >= 0; i--)
            {
                var vs = _verbs[i];
                if (vs.CurMode != 1 || vs.VerbId == 0 || vs.SaveId != 0 || y < vs.CurRect.Top || y >= vs.CurRect.Bottom)
                    continue;
                if (vs.Center)
                {
                    if (x < -(vs.CurRect.Right - 2 * vs.CurRect.Left) || x >= vs.CurRect.Right)
                        continue;
                }
                else
                {
                    if (x < vs.CurRect.Left || x >= vs.CurRect.Right)
                        continue;
                }

                return i;
            }

            return 0;
        }

        int GetVerbSlot(int id, int mode)
        {
            for (int i = 1; i < _verbs.Length; i++)
            {
                if (_verbs[i].VerbId == id && _verbs[i].SaveId == mode)
                {
                    return i;
                }
            }
            return 0;
        }

        void DoSentence(byte verb, ushort objectA, ushort objectB)
        {
            var st = _sentence[_sentenceNum++];
            st.Verb = verb;
            st.ObjectA = objectA;
            st.ObjectB = objectB;
            st.Preposition = (byte)((objectB != 0) ? 1 : 0);
            st.FreezeCount = 0;
        }

        #endregion Verb Members
    }
}