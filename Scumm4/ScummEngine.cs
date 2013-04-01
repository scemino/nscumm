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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Scumm4;
using Scumm4.Graphics;
using System.Windows.Input;

namespace Scumm4
{
    [Flags]
    public enum LightModes
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

    [Flags]
    public enum DrawBitmapFlags
    {
        AllowMaskOr = 1 << 0,
        DrawMaskOnAll = 1 << 1,
        ObjectMode = 2 << 2
    }

    public enum OpCodeParameter : byte
    {
        Param1 = 0x80,
        Param2 = 0x40,
        Param3 = 0x20,
    }

    public class ScummEngine : System.Windows.Threading.DispatcherObject
    {
        #region Constants
        private const uint CurrentVersion = 94;
        const int OF_OWNER_ROOM = 0x0F;

        private const int NumVariables = 800;
        private const int NumActors = 13;
        private const int NumLocalObjects = 200;
        private const int NumArray = 50;
        private const int NumScriptSlot = 80;
        private const int NumGlobalScripts = 200;
        private const int NumInventory = 80;
        private const int MaxScriptNesting = 15;
        private const int MaxCutsceneNum = 5;

        public const int VariableEgo = 0x01;
        private const int VariableCameraPosX = 0x02;
        private const int VariableHaveMessage = 0x03;
        private const int VariableRoom = 0x04;
        private const int VariableOverride = 0x05;
        private const int VariableCurrentLights = 0x09;
        public const int VariableTimer1 = 0x0B;
        public const int VariableTimer2 = 0x0C;
        public const int VariableTimer3 = 0x0D;
        private const int VariableCameraMinX = 0x11;
        private const int VariableCameraMaxX = 0x12;
        public const int VariableTimerNext = 0x13;
        public const int VariableVirtualMouseX = 0x14;
        public const int VariableVirtualMouseY = 0x15;
        private const int VariableRoomResource = 0x16;
        public const int VariableCutSceneExitKey = 0x18;
        private const int VariableTalkActor = 0x19;
        private const int VariableCameraFastX = 0x1A;
        private const int VariableScrollScript = 0x1B;
        private const int VariableEntryScript = 0x1C;
        private const int VariableEntryScript2 = 0x1D;
        private const int VariableExitScript = 0x1E;
        private const int VariableVerbScript = 0x20;
        private const int VariableSentenceScript = 0x21;
        private const int VariableInventoryScript = 0x22;
        private const int VariableCutSceneStartScript = 0x23;
        private const int VariableCutSceneEndScript = 0x24;
        public const int VariableCharIncrement = 0x25;
        private const int VariableWalkToObject = 0x26;
        private const int VariableDebugMode = 0x27;
        private const int VariableHeapSpace = 0x28;
        public const int VariableMouseX = 0x2C;
        public const int VariableMouseY = 0x2D;
        private const int VariableTimer = 0x2E;
        private const int VariableTimerTotal = 0x2F;
        private const int VariableSoundcard = 0x30;
        private const int VariableVideoMode = 0x31;
        private const int VariableMainMenu = 0x32;
        private const int VariableFixedDisk = 0x33;
        private const int VariableCursorState = 0x34;
        private const int VariableUserPut = 0x35;
        private const int VariableTalkStringY = 0x36;
        #endregion

        #region Events
        public event EventHandler ShowMenuDialogRequested;
        #endregion

        #region Fields
        private ScummIndex _scumm;
        private string _directory;
        private Actor[] _actors = new Actor[NumActors];
        private byte _currentRoom;
        private int _actorToPrintStrFor;
        public int _screenWidth = 320;
        private int _screenHeight = 200;
        private ushort[] _inventory = new ushort[NumInventory];

        private ScriptSlot[] _slots;
        private int[][] _localVariables = new int[NumScriptSlot][];
        private byte[] _bitVars = new byte[4096 / 8];
        private Dictionary<byte, Action> _opCodes;
        private byte[] _currentScriptData;
        private int _currentPos;
        private int[] _variables;
        private HashSet<ObjectData> _drawingObjects = new HashSet<ObjectData>();

        private sbyte _userPut;
        private byte _roomResource;
        private bool _egoPositioned;

        private Cursor _cursor = new Cursor();
        private int _resultVarIndex;
        private byte _opCode;
        private Stack<int> _stack = new Stack<int>();
        private byte _currentScript;
        private int _numNestedScripts;
        private NestedScript[] _nest;
        private uint[] cutScenePtr = new uint[MaxCutsceneNum];
        private byte[] cutSceneScript = new byte[MaxCutsceneNum];
        private int[] cutSceneData = new int[MaxCutsceneNum];
        private Room roomData;
        private int _sentenceNum;
        private int cutSceneStackPointer;
        private TextSlot[] _string = new TextSlot[6];
        private byte[] _charsetBuffer = new byte[512];

        private byte[] _resourceMapper = new byte[128];
        private byte[][] _strings;
        private byte[][] _charsets;
        public byte[] _charsetColorMap = new byte[16];

        private int _cutSceneScriptIndex;
        private FlashLight _flashlight = new FlashLight();
        private Camera _camera = new Camera();
        private ICostumeLoader _costumeLoader;
        private ICostumeRenderer _costumeRenderer;

        private bool _keepText;
        private bool _useTalkAnims;
        private byte _charsetColor;
        private int _talkDelay;
        private int _haveMsg;
        private int _charsetBufPos;
        private int _screenStartStrip;
        private int _screenEndStrip;
        public int _screenTop;
        private VerbSlot[] _verbs;
        private byte cursor_color;
        private int _currentCursor;
        private VirtScreen _mainVirtScreen;
        private VirtScreen _textVirtScreen;
        private VirtScreen _verbVirtScreen;
        private bool _bgNeedsRedraw;
        private bool _fullRedraw;
        public Gdi _gdi;

        // Somewhat hackish stuff for 2 byte support (Chinese/Japanese/Korean)
        public byte _newLineCharacter;
        public bool _useCJKMode;
        public int _2byteWidth;

        static byte[] default_cursor_colors = new byte[] { 15, 15, 7, 8 };

        public byte[] _roomPalette = new byte[256];

        private byte _newEffect = 129, _switchRoomEffect2, _switchRoomEffect;
        private bool _disableFadeInEffect;
        private bool _doEffect;
        private bool _screenEffectFlag;

        private ObjectData[] _objs = new ObjectData[200];
        private ColorCycle[] _colorCycle;
        private ScaleSlot[] _scaleSlots;

        private int _numGlobalObjects = 1000;
        private int _numLocalObjects = 200;
        private bool _haveActorSpeechMsg;
        private Surface _composite;

        private CharsetRenderer _charset;
        private const byte CharsetMaskTransparency = 0xFD;
        #endregion

        #region Properties
        public Camera Camera
        {
            get { return _camera; }
        }

        public Actor[] Actors
        {
            get { return _actors; }
        }

        public uint[] ClassData
        {
            get { return _scumm.ClassData; }
        }

        public int[] Variables
        {
            get { return _variables; }
        }

        public TextSlot[] TextSlot
        {
            get { return _string; }
        }

        public Room CurrentRoomData
        {
            get { return roomData; }
        }

        public byte CurrentRoom
        {
            get { return _currentRoom; }
        }

        public HashSet<ObjectData> DrawingObjects
        {
            get { return _drawingObjects; }
        }

        public ScummIndex Index { get { return _scumm; } }

        public VirtScreen MainVirtScreen
        {
            get { return _mainVirtScreen; }
        }
        #endregion

        #region Constructor
        public ScummEngine(ScummIndex index, IGraphicsManager gfxManager)
        {
            _scumm = index;
            _gfxManager = gfxManager;
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
            _scaleSlots = new ScaleSlot[20];
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
            _gdi = new Gdi(this);
            _costumeLoader = new ClassicCostumeLoader(index);
            _costumeRenderer = new ClassicCostumeRenderer(this);

            // Create the charset renderer
            _charset = new CharsetRendererClassic(this);

            ResetCursors();

            // Create the text surface
            _textSurface = new Surface(_screenWidth * _textSurfaceMultiplier, _screenHeight * _textSurfaceMultiplier, PixelFormat.Indexed8, false);
            ClearTextSurface();

            InitScreens(16, 144);
            _composite = new Surface(_screenWidth, _screenHeight, PixelFormat.Indexed8, false);
            InitActors();
            InitOpCodes();
            for (int i = 0; i < 256; i++)
                _roomPalette[i] = (byte)i;
            InitializeVerbs();
            InitVariables();
        }

        private void InitializeVerbs()
        {
            _verbs = new VerbSlot[100];
            for (int i = 0; i < 100; i++)
            {
                _verbs[i] = new VerbSlot();
                _verbs[i].verbid = 0;
                _verbs[i].curRect.right = _screenWidth - 1;
                _verbs[i].oldRect.left = -1;
                _verbs[i].type = 0;
                _verbs[i].color = 2;
                _verbs[i].hicolor = 0;
                _verbs[i].charset_nr = 1;
                _verbs[i].curmode = 0;
                _verbs[i].saveid = 0;
                _verbs[i].center = false;
                _verbs[i].key = 0;
            }
        }

        private void InitActors()
        {
            for (byte i = 0; i < NumActors; i++)
            {
                _actors[i] = new Actor(this, i);
                _actors[i].InitActor(-1);
            }
        }

        private void InitVariables()
        {
            for (int i = 0; i < NumScriptSlot; i++)
            {
                _localVariables[i] = new int[26];
            }
            _variables = new int[NumVariables];
            _variables[VariableVideoMode] = 19;
            _variables[VariableFixedDisk] = 1;
            _variables[VariableHeapSpace] = 1400;
            _variables[VariableCharIncrement] = 4;
            SetTalkingActor(0);
#if DEBUG
            //_variables[VariableDebugMode] = 1;
#endif
            // MDT_ADLIB
            //_variables[VariableSoundcard] = 3;

            _variables[VariableTalkStringY] = -0x50;

            // Setup light
            _variables[VariableCurrentLights] = (int)(LightModes.ActorUseBasePalette | LightModes.ActorUseColors | LightModes.RoomLightsOn);

            _variables[74] = 1225;
        }

        #endregion

        #region Execution
        public void Step()
        {
            var opCode = _currentScriptData[_currentPos++];
            // execute the code
            ExecuteOpCode(opCode);
        }

        private void ExecuteOpCode(byte opCode)
        {
            _opCode = opCode;
            _slots[_currentScript].didexec = true;
            //Console.WriteLine("OpCode: {0:X2}, Name = {1}", _opCode, _opCodes.ContainsKey(_opCode) ? _opCodes[opCode].Method.Name : "Unknown");
            _opCodes[opCode]();
        }

        private void Run()
        {
            while (_currentScript != 0xFF)
            {
                this.Step();
            }
        }
        #endregion

        #region Variable Manipulation
        private byte ReadByte()
        {
            return _currentScriptData[_currentPos++];
        }

        private ushort ReadWord()
        {
            ushort word = (ushort)(_currentScriptData[_currentPos++] | (ushort)(_currentScriptData[_currentPos++] << 8));
            return word;
        }

        private void GetResult()
        {
            _resultVarIndex = ReadWord();
            if ((_resultVarIndex & 0x2000) != 0)
            {
                int a = (int)ReadWord();
                if ((a & 0x2000) != 0)
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

        private int ReadVariable(int var)
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
                return _variables[var];
            }

            if ((var & 0x8000) == 0x8000)
            {
                var &= 0x7FFF;

                return ((_bitVars[var >> 3] & (1 << (var & 7))) != 0) ? 1 : 0;
            }

            if ((var & 0x4000) == 0x4000 && _currentScript != 0xFF)
            {
                var &= 0xFFF;

                //assertRange(0, var, 20, "local variable (reading)");
                return this._localVariables[_currentScript][var];
            }
            return -1;
        }

        private List<int> GetWordVarArgs()
        {
            List<int> args = new List<int>();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                args.Add(GetVarOrDirectWord(OpCodeParameter.Param1));
            }
            return args;
        }

        private void SetResult(int value)
        {
            if ((_resultVarIndex & 0xF000) == 0)
            {
                _variables[_resultVarIndex] = value;
                return;
            }

            if ((_resultVarIndex & 0x8000) != 0)
            {
                _resultVarIndex &= 0x7FFF;

                if (value != 0)
                    _bitVars[_resultVarIndex >> 3] |= (byte)(1 << (_resultVarIndex & 7));
                else
                    _bitVars[_resultVarIndex >> 3] &= (byte)(~(1 << (_resultVarIndex & 7)));
                return;
            }

            if ((_resultVarIndex & 0x4000) != 0)
            {
                _resultVarIndex &= 0xFFF;
                _localVariables[_currentScript][_resultVarIndex] = value;
                return;
            }
        }
        #endregion

        #region OpCodes

        private void InitOpCodes()
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
            /* 6C */
            _opCodes[0x6D] = PutActorInRoom;
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
            /* EC */
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

        private void GetActorCostume()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = this.Actors[act];
            SetResult(a._costume);
        }

        private void SetObjectName()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetObjectName(obj);
        }

        private void GetActorMoving()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = _actors[act];
            SetResult((int)a._moving);
        }

        private void PutActorAtObject()
        {
            int obj, x, y;
            Actor a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
                GetObjectXYPos(obj, out x, out y);
            else
            {
                x = 240;
                y = 120;
            }
            a.PutActor((short)x, (short)y);
        }

        private void WalkActorToActor()
        {
            int x, y;
            int nr = GetVarOrDirectByte(OpCodeParameter.Param1);
            int nr2 = GetVarOrDirectByte(OpCodeParameter.Param2);
            int dist = ReadByte();

            var a = _actors[nr];
            if (!a.IsInCurrentRoom())
                return;

            var a2 = _actors[nr2];
            if (!a2.IsInCurrentRoom())
                return;

            if (dist == 0xFF)
            {
                dist = (int)(a._scalex * a._width / 0xFF);
                dist += (int)(a2._scalex * a2._width / 0xFF) / 2;
            }
            x = a2.GetPos().X;
            y = a2.GetPos().Y;
            if (x < a.GetPos().X)
                x += dist;
            else
                x -= dist;

            a.StartWalkActor(x, y, -1);
        }

        private void PanCameraTo()
        {
            PanCameraTo(GetVarOrDirectWord(OpCodeParameter.Param1));
        }

        private void GetActorX()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(GetObjX(a));
        }

        private void GetActorY()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(GetObjY(a));
        }

        private void MatrixOperations()
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

        private void CreateBoxMatrix()
        {
            // TODO
            throw new NotImplementedException("CreateBoxMatrix");
        }

        private void SetBoxScale(int box, int scale)
        {
            Box b = GetBoxBase(box);
            b.scale = (ushort)scale;
        }

        private void SetBoxFlags(int box, int val)
        {
            Box b = GetBoxBase(box);
            if (b == null)
                return;
            b.flags = (BoxFlags)val;
        }

        private void DelayVariable()
        {
            _slots[_currentScript].delay = GetVar();
            _slots[_currentScript].status = ScriptStatus.Paused;
            BreakHere();
        }

        private void ActorFromPosition()
        {
            int x, y;
            GetResult();
            x = GetVarOrDirectWord(OpCodeParameter.Param1);
            y = GetVarOrDirectWord(OpCodeParameter.Param2);
            SetResult(GetActorFromPos(x, y));
        }

        private int GetActorFromPos(int x, int y)
        {
            int i;

            for (i = 1; i < _actors.Length; i++)
            {
                if (!GetClass(i, ObjectClass.Untouchable) && y >= _actors[i]._top && y <= _actors[i]._bottom)
                {
                    return i;
                }
            }

            return 0;
        }

        private void ChainScript()
        {
            int script;
            int cur;

            script = GetVarOrDirectByte(OpCodeParameter.Param1);

            var vars = GetWordVarArgs();

            cur = _currentScript;

            _slots[cur].number = 0;
            _slots[cur].status = ScriptStatus.Dead;
            _currentScript = 0xFF;

            RunScript((byte)script, _slots[cur].freezeResistant, _slots[cur].recursive, vars);
        }

        private void GetDistance()
        {
            int o1, o2;
            int r;
            GetResult();
            o1 = GetVarOrDirectWord(OpCodeParameter.Param1);
            o2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            r = GetObjActToObjActDist(o1, o2);

            // TODO: WORKAROUND bug #795937 ?
            //if ((_game.id == GID_MONKEY_EGA || _game.id == GID_PASS) && o1 == 1 && o2 == 307 && vm.slot[_currentScript].number == 205 && r == 2)
            //    r = 3;

            SetResult(r);
        }

        private int GetObjActToObjActDist(int a, int b)
        {
            int x, y, x2, y2;
            Actor acta = null;
            Actor actb = null;

            if (a < _actors.Length)
                acta = _actors[a];

            if (b < _actors.Length)
                actb = _actors[b];

            if ((acta != null) && (actb != null) && (acta.GetRoom() == actb.GetRoom()) && (acta.GetRoom() != 0) && !acta.IsInCurrentRoom())
                return 0;

            if (GetObjectOrActorXY(a, out x, out y) == false)
                return 0xFF;

            if (GetObjectOrActorXY(b, out x2, out y2) == false)
                return 0xFF;

            // Perform adjustXYToBeInBox() *only* if the first item is an
            // actor and the second is an object. This used to not check
            // whether the second item is a non-actor, which caused bug
            // #853874).
            if (acta != null && actb == null)
            {
                AdjustBoxResult r = acta.AdjustXYToBeInBox((short)x2, (short)y2);
                x2 = r.x;
                y2 = r.y;
            }


            // Now compute the distance between the two points
            return GetDistance(x, y, x2, y2);
        }

        private int GetDistance(int x, int y, int x2, int y2)
        {
            int a = Math.Abs(y - y2);
            int b = Math.Abs(x - x2);
            return Math.Max(a, b);
        }

        private void IfClassOfIs()
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

        private void IfState()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = GetVarOrDirectByte(OpCodeParameter.Param2);

            JumpRelative(GetState(a) == b);
        }

        private void IfNotState()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = GetVarOrDirectByte(OpCodeParameter.Param2);

            JumpRelative(GetState(a) != b);
        }

        private void GetActorWalkBox()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = _actors[act];
            SetResult(a._walkbox);
        }

        private void Wait()
        {
            var oldPos = _currentPos - 1;
            _opCode = ReadByte();

            switch (_opCode & 0x1F)
            {
                case 1:		// SO_WAIT_FOR_ACTOR
                    {
                        Actor a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
                        if (a != null && a._moving != 0)
                            break;
                        return;
                    }
                case 2:		// SO_WAIT_FOR_MESSAGE
                    if (_variables[VariableHaveMessage] != 0)
                        break;
                    return;
                case 3:		// SO_WAIT_FOR_CAMERA
                    if (_camera._cur.X / 8 != _camera._dest.X / 8)
                        break;
                    return;
                case 4:		// SO_WAIT_FOR_SENTENCE
                    if (_sentenceNum != 0)
                    {
                        if (_sentence[_sentenceNum - 1].freezeCount != 0 && !IsScriptInUse(_variables[VariableSentenceScript]))
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

        private void WalkActorTo()
        {
            int x, y;
            Actor a;

            a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            x = GetVarOrDirectWord(OpCodeParameter.Param2);
            y = GetVarOrDirectWord(OpCodeParameter.Param3);
            a.StartWalkActor(x, y, -1);
        }

        private void WalkActorToObject()
        {
            Actor a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                int x, y, dir;
                GetObjectXYPos(obj, out x, out y, out dir);
                a.StartWalkActor(x, y, dir);
            }
        }

        private void FaceActor()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            int obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            Actor a = _actors[act];
            a.FaceToObject(obj);
        }

        private void PrintEgo()
        {
            _actorToPrintStrFor = (byte)_variables[VariableEgo];
            DecodeParseString();
        }

        private void FindObject()
        {
            GetResult();
            int x = GetVarOrDirectByte(OpCodeParameter.Param1);
            int y = GetVarOrDirectByte(OpCodeParameter.Param2);
            SetResult(FindObject(x, y));
        }

        private int FindObject(int x, int y)
        {
            int i, b;
            byte a;
            int mask = 0xF;

            for (i = 1; i < _numLocalObjects; i++)
            {
                if ((Objects[i].obj_nr < 1) || GetClass(Objects[i].obj_nr, ObjectClass.Untouchable))
                    continue;

                b = i;
                do
                {
                    a = Objects[b].parentstate;
                    b = Objects[b].parent;
                    if (b == 0)
                    {
                        if (Objects[i].x_pos <= x && (Objects[i].width + Objects[i].x_pos) > x &&
                            Objects[i].y_pos <= y && (Objects[i].height + Objects[i].y_pos) > y)
                        {
                            return Objects[i].obj_nr;
                        }
                        break;
                    }
                } while ((Objects[b].state & mask) == a);
            }

            return 0;
        }

        private void SetOwnerOf()
        {
            int obj, owner;

            obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            owner = GetVarOrDirectByte(OpCodeParameter.Param2);

            SetOwnerOf(obj, owner);
        }

        private void SetOwnerOf(int obj, int owner)
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

                var ss = _slots[_currentScript];
                if (ss.where == WhereIsObject.Inventory)
                {
                    if (ss.number < NumInventory && _inventory[ss.number] == obj)
                    {
                        //throw new NotSupportedException("Odd setOwnerOf case #1: Please report to Fingolfin where you encountered this");
                        PutOwner(obj, 0);
                        RunInventoryScript(arg);
                        StopObjectCode();
                        return;
                    }
                    if (ss.number == obj)
                        throw new NotSupportedException("Odd setOwnerOf case #2: Please report to Fingolfin where you encountered this");
                }
            }

            PutOwner(obj, (byte)owner);
            RunInventoryScript(arg);
        }

        private void ClearOwnerOf(int obj)
        {
            int i;

            // Stop the associated object script code (else crashes might occurs)
            StopObjectScript((ushort)obj);

            // If the object is "owned" by a the current room, we scan the
            // object list and (only if it's a floating object) nuke it.
            if (GetOwner(obj) == OF_OWNER_ROOM)
            {
                for (i = 0; i < _numLocalObjects; i++)
                {
                    if (Objects[i].obj_nr == obj && Objects[i].fl_object_index != 0)
                    {
                        // Removing an flObject from a room means we can nuke it
                        Objects[i].obj_nr = 0;
                        Objects[i].fl_object_index = 0;
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

        private void IsSoundRunning()
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

        private void LoadRoomWithEgo()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            int room = GetVarOrDirectByte(OpCodeParameter.Param2);

            Actor a = _actors[_variables[VariableEgo]];

            a.PutActor((byte)room);
            int oldDir = a.GetFacing();
            _egoPositioned = false;

            short x = ReadWordSigned();
            short y = ReadWordSigned();

            _variables[VariableWalkToObject] = obj;

            StartScene(a._room);
            _variables[VariableWalkToObject] = 0;

            if (!_egoPositioned)
            {
                int x2, y2, dir;
                GetObjectXYPos(obj, out x2, out y2, out dir);
                a.PutActor((short)x2, (short)y2, _currentRoom);
                if (a.GetFacing() == oldDir)
                    a.SetDirection(dir + 180);
            }
            a._moving = 0;


            // This is based on disassembly
            _camera._cur.X = _camera._dest.X = a.GetPos().X;
            SetCameraFollows(a, false);

            _fullRedraw = true;

            if (x != -1)
            {
                a.StartWalkActor(x, y, -1);
            }
        }

        private void GetObjectXYPos(int obj, out int x, out int y, out int dir)
        {
            int idx = GetObjectIndex(obj);

            ObjectData od = Objects[idx];
            x = od.walk_x;
            y = od.walk_y;

            dir = ScummHelper.OldDirToNewDir(od.actordir & 3);
        }

        private void GetObjectXYPos(int obj, out int x, out int y)
        {
            int dir;
            GetObjectXYPos(obj, out x, out y, out dir);
        }

        private void GetInventoryCount()
        {
            GetResult();
            SetResult(GetInventoryCount(GetVarOrDirectByte(OpCodeParameter.Param1)));
        }

        private int GetInventoryCount(int owner)
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

        private void FindInventory()
        {
            GetResult();
            int x = GetVarOrDirectByte(OpCodeParameter.Param1);
            int y = GetVarOrDirectByte(OpCodeParameter.Param2);
            SetResult(FindInventory(x, y));
        }

        private int FindInventory(int owner, int idx)
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

        private void SaveRestoreVerbs()
        {
            int a, b, c, slot, slot2;

            _opCode = ReadByte();

            a = GetVarOrDirectByte(OpCodeParameter.Param1);
            b = GetVarOrDirectByte(OpCodeParameter.Param2);
            c = GetVarOrDirectByte(OpCodeParameter.Param3);

            switch (_opCode)
            {
                case 1:		// SO_SAVE_VERBS
                    while (a <= b)
                    {
                        slot = GetVerbSlot(a, 0);
                        if (slot != 0 && _verbs[slot].saveid == 0)
                        {
                            _verbs[slot].saveid = (ushort)c;
                            DrawVerb(slot, 0);
                            VerbMouseOver(0);
                        }
                        a++;
                    }
                    break;
                case 2:		// SO_RESTORE_VERBS
                    while (a <= b)
                    {
                        slot = GetVerbSlot(a, c);
                        if (slot != 0)
                        {
                            slot2 = GetVerbSlot(a, 0);
                            if (slot2 != 0)
                                KillVerb(slot2);
                            slot = GetVerbSlot(a, c);
                            _verbs[slot].saveid = 0;
                            DrawVerb(slot, 0);
                            VerbMouseOver(0);
                        }
                        a++;
                    }
                    break;
                case 3:		// SO_DELETE_VERBS
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

        private int GetOwner(int obj)
        {
            return _scumm.ObjectOwnerTable[obj];
        }

        private void ActorFollowCamera()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var old = _camera._follows;
            SetCameraFollows(_actors[actor], false);

            if (_camera._follows != old)
                RunInventoryScript(0);

            _camera._movingToActor = false;
        }

        private void SetCameraFollows(Actor actor, bool setCamera)
        {
            int t, i;

            _camera._mode = CameraMode.FollowActor;
            _camera._follows = actor._number;

            if (!actor.IsInCurrentRoom())
            {
                // TODO: check this
                StartScene((byte)actor.GetRoom());
                _camera._mode = CameraMode.FollowActor;
                _camera._cur.X = actor.GetPos().X;
                SetCameraAt(_camera._cur.X, 0);
            }

            t = actor.GetPos().X / 8 - _screenStartStrip;

            if (t < _camera._leftTrigger || t > _camera._rightTrigger || setCamera == true)
                SetCameraAt(actor.GetPos().X, 0);

            for (i = 1; i < _actors.Length; i++)
            {
                if (_actors[i].IsInCurrentRoom())
                    _actors[i].NeedRedraw = true;
            }
            RunInventoryScript(0);
        }

        private void RunInventoryScript(int i)
        {
            if (_variables[VariableInventoryScript] != 0)
            {
                RunScript((byte)_variables[VariableInventoryScript], false, false, new int[] { i });
            }
        }

        private void EndCutscene()
        {
            if (_slots[_currentScript].cutsceneOverride > 0)	// Only terminate if active
                _slots[_currentScript].cutsceneOverride--;

            var args = new int[] { cutSceneData[cutSceneStackPointer] };

            _variables[VariableOverride] = 0;

            if (cutScenePtr[cutSceneStackPointer] != 0 && (_slots[_currentScript].cutsceneOverride > 0))	// Only terminate if active
                _slots[_currentScript].cutsceneOverride--;

            cutSceneScript[cutSceneStackPointer] = 0;
            cutScenePtr[cutSceneStackPointer] = 0;

            cutSceneStackPointer--;

            if (_variables[VariableCutSceneEndScript] != 0)
                RunScript((byte)_variables[VariableCutSceneEndScript], false, false, args);
        }

        private void StopSound()
        {
            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
            //_sound->stopSound();
        }

        private void PutActor()
        {
            Actor a = _actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            short x = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
            short y = (short)GetVarOrDirectWord(OpCodeParameter.Param3);
            a.PutActor(x, y);
        }

        private void AnimateActor()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            int anim = GetVarOrDirectByte(OpCodeParameter.Param2);

            Actor a = _actors[act];
            a.AnimateActor(anim);
        }

        private void ActorOps()
        {
            byte[] convertTable = new byte[20] { 1, 0, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 20 };
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = _actors[act];
            int i, j;

            while ((_opCode = ReadByte()) != 0xFF)
            {
                _opCode = (byte)((_opCode & 0xE0) | convertTable[(_opCode & 0x1F) - 1]);

                switch (_opCode & 0x1F)
                {
                    case 0:										/* dummy case */
                        GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 1:			// SO_COSTUME
                        a.SetActorCostume((ushort)GetVarOrDirectByte(OpCodeParameter.Param1));
                        break;
                    case 2:			// SO_STEP_DIST
                        i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        a.SetActorWalkSpeed((uint)i, (uint)j);
                        break;
                    case 3:			// SO_SOUND
                        a._sound[0] = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 4:			// SO_WALK_ANIMATION
                        a._walkFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 5:			// SO_TALK_ANIMATION
                        a._talkStartFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        a._talkStopFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);
                        break;
                    case 6:			// SO_STAND_ANIMATION
                        a._standFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 7:			// SO_ANIMATION
                        GetVarOrDirectByte(OpCodeParameter.Param1);
                        GetVarOrDirectByte(OpCodeParameter.Param2);
                        GetVarOrDirectByte(OpCodeParameter.Param3);
                        break;
                    case 8:			// SO_DEFAULT
                        a.InitActor(0);
                        break;
                    case 9:			// SO_ELEVATION
                        a.SetElevation(GetVarOrDirectWord(OpCodeParameter.Param1));
                        break;
                    case 10:		// SO_ANIMATION_DEFAULT
                        a._initFrame = 1;
                        a._walkFrame = 2;
                        a._standFrame = 3;
                        a._talkStartFrame = 4;
                        a._talkStopFrame = 5;
                        break;
                    case 11:		// SO_PALETTE
                        i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        //assertRange(0, i, 31, "o5_actorOps: palette slot");
                        a.SetPalette(i, (ushort)j);
                        break;
                    case 12:		// SO_TALK_COLOR
                        a._talkColor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 13:		// SO_ACTOR_NAME
                        var name = ReadCharacters();
                        a.Name = name;
                        break;
                    case 14:		// SO_INIT_ANIMATION
                        a._initFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 16:		// SO_ACTOR_WIDTH
                        a._width = (uint)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 17:		// SO_ACTOR_SCALE
                        i = j = GetVarOrDirectByte(OpCodeParameter.Param1);
                        a._boxscale = (ushort)i;
                        a.SetScale(i, j);
                        break;
                    case 18:		// SO_NEVER_ZCLIP
                        a._forceClip = 0;
                        break;
                    case 19:		// SO_ALWAYS_ZCLIP
                        a._forceClip = GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 20:		// SO_IGNORE_BOXES
                    case 21:		// SO_FOLLOW_BOXES
                        a._ignoreBoxes = (_opCode & 1) == 0;
                        a._forceClip = 0;
                        if (a.IsInCurrentRoom())
                            a.PutActor();
                        break;
                    case 22:		// SO_ANIMATION_SPEED
                        a.SetAnimSpeed((byte)GetVarOrDirectByte(OpCodeParameter.Param1));
                        break;
                    case 23:		// SO_SHADOW
                        a._shadowMode = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    default:
                        throw new NotImplementedException();
                    //error("o5_actorOps: default case %d", _opCode & 0x1F);
                }
            }
        }

        private void GetActorFacing()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var a = this.Actors[act];
            SetResult(ScummHelper.NewDirToOldDir(a.GetFacing()));
        }

        private void GetActorElevation()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = this.Actors[act];
            SetResult(a.GetElevation());
        }

        private void SetClass()
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
                    this.ClassData[obj] = 0;
                    if (obj <= NumActors)
                    {
                        Actor a = _actors[obj];
                        a._ignoreBoxes = false;
                        a._forceClip = 0;
                    }
                }
                else
                {
                    PutClass(obj, cls, (cls & 0x80) != 0);
                }
            }
        }

        private void PutClass(int obj, int cls, bool set)
        {
            ScummHelper.AssertRange(0, obj, _numGlobalObjects - 1, "object");
            ObjectClass cls2 = (ObjectClass)(cls & 0x7F);
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
                this.ClassData[obj] |= (uint)(1 << ((int)cls2 - 1));
            else
                this.ClassData[obj] &= (uint)~(1 << ((int)cls2 - 1));

            if (obj >= 1 && obj < NumActors)
            {
                _actors[obj].ClassChanged((ObjectClass)cls2, set);
            }
        }

        private void GetActorRoom()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);

            Actor a = _actors[act];
            SetResult(a._room);
        }

        private void PutActorInRoom()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            byte room = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = _actors[act];

            if (a._visible && _currentRoom != room && GetTalkingActor() == a._number)
            {
                StopTalk();
            }
            a._room = room;
            if (room == 0)
                a.PutActor(0, 0, 0);
        }

        private void BeginOverride()
        {
            if (ReadByte() != 0)
                BeginOverrideCore();
            else
                EndOverrideCore();
        }

        private void EndOverrideCore()
        {
            var idx = cutSceneStackPointer;
            cutScenePtr[idx] = 0;
            cutSceneScript[idx] = 0;

            _variables[VariableOverride] = 0;
        }

        private void BeginOverrideCore()
        {
            int idx = cutSceneStackPointer;

            cutScenePtr[idx] = (uint)_currentPos;
            cutSceneScript[idx] = _currentScript;

            // Skip the jump instruction following the override instruction
            // (the jump is responsible for "skipping" cutscenes, and the reason
            // why we record the current script position in vm.cutScenePtr).
            ReadByte();
            ReadWord();
        }

        private void SetCameraAt()
        {
            short at = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            _camera._mode = CameraMode.Normal;
            _camera._cur.X = at;
            SetCameraAt(at, 0);
            _camera._movingToActor = false;
        }

        private void SetCameraAt(short pos_x, short pos_y)
        {
            if (_camera._mode != CameraMode.FollowActor || Math.Abs(pos_x - _camera._cur.X) > (_screenWidth / 2))
            {
                _camera._cur.X = pos_x;
            }
            _camera._dest.X = pos_x;

            if (_camera._cur.X < _variables[VariableCameraMinX])
                _camera._cur.X = (short)_variables[VariableCameraMinX];

            if (_camera._cur.X > _variables[VariableCameraMaxX])
                _camera._cur.X = (short)_variables[VariableCameraMaxX];

            if (_variables[VariableScrollScript] != 0)
            {
                _variables[VariableCameraPosX] = _camera._cur.X;
                RunScript((byte)_variables[VariableScrollScript], false, false, new int[0]);
            }

            // If the camera moved and text is visible, remove it
            if (_camera._cur.X != _camera._last.X && _charset._hasMask)
                StopTalk();
        }

        private void Lights()
        {
            int a, b, c;

            a = GetVarOrDirectByte(OpCodeParameter.Param1);
            b = ReadByte();
            c = ReadByte();

            if (c == 0)
                _variables[VariableCurrentLights] = a;
            else if (c == 1)
            {
                _flashlight.xStrips = (ushort)a;
                _flashlight.yStrips = (ushort)b;
            }
            _fullRedraw = true;
        }

        private void FreezeScripts()
        {
            int scr = GetVarOrDirectByte(OpCodeParameter.Param1);

            if (scr != 0)
                FreezeScripts(scr);
            else
                UnfreezeScripts();
        }

        private void UnfreezeScripts()
        {
            for (int i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].Frozen)
                {
                    if (--_slots[i].freezeCount == 0)
                    {
                        _slots[i].Frozen = false;
                    }
                }
            }

            for (int i = 0; i < _sentence.Length; i++)
            {
                if (_sentence[i].freezeCount > 0)
                    _sentence[i].freezeCount--;
            }
        }

        private void FreezeScripts(int flag)
        {
            int i;

            for (i = 0; i < NumScriptSlot; i++)
            {
                if (_currentScript != i && _slots[i].status != ScriptStatus.Dead && (!_slots[i].freezeResistant || flag >= 0x80))
                {
                    _slots[i].Frozen = true;
                    _slots[i].freezeCount++;
                }
            }

            for (i = 0; i < _sentence.Length; i++)
                _sentence[i].freezeCount++;

            if (_cutSceneScriptIndex != 0xFF)
            {
                _slots[_cutSceneScriptIndex].Frozen = false;
                _slots[_cutSceneScriptIndex].freezeCount = 0;
            }
        }

        private void DoSentence()
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

        private Sentence[] _sentence = InitSentences();

        private static Sentence[] InitSentences()
        {
            var sentences = new Sentence[6];
            for (int i = 0; i < sentences.Length; i++)
            {
                sentences[i] = new Sentence();
            }
            return sentences;
        }

        private void DoSentence(byte verb, ushort objectA, ushort objectB)
        {
            Sentence st;

            st = _sentence[_sentenceNum++];

            st.verb = verb;
            st.objectA = objectA;
            st.objectB = objectB;
            st.preposition = (byte)((objectB != 0) ? 1 : 0);
            st.freezeCount = 0;
        }

        private void BeginCutscene(IList<int> args)
        {
            int scr = _currentScript;
            _slots[scr].cutsceneOverride++;

            ++cutSceneStackPointer;
            if (cutSceneStackPointer >= MaxCutsceneNum)
                throw new NotSupportedException("Cutscene stack overflow");

            cutSceneData[cutSceneStackPointer] = args.Count > 0 ? args[0] : 0;
            cutSceneScript[cutSceneStackPointer] = 0;
            cutScenePtr[cutSceneStackPointer] = 0;

            _cutSceneScriptIndex = scr;
            if (_variables[VariableCutSceneStartScript] != 0)
                RunScript((byte)_variables[VariableCutSceneStartScript], false, false, args);
            _cutSceneScriptIndex = 0xFF;
        }

        private void CutScene()
        {
            var args = GetWordVarArgs();
            BeginCutscene(args);
        }

        private void IsScriptRunning()
        {
            GetResult();
            SetResult(IsScriptRunning(GetVarOrDirectByte(OpCodeParameter.Param1)) ? 1 : 0);
        }

        private bool IsScriptRunning(int script)
        {
            for (int i = 0; i < NumScriptSlot; i++)
            {
                var ss = _slots[i];
                if (ss.number == script && (ss.where == WhereIsObject.Global || ss.where == WhereIsObject.Local) && ss.status != ScriptStatus.Dead)
                    return true;
            }
            return false;
        }

        private void LoadRoom()
        {
            var room = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
            if (room != _currentRoom)
            {
                StartScene(room);
            }
            _fullRedraw = true;
        }

        private void RoomOps()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:		// SO_ROOM_SCROLL
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        if (a < (_screenWidth / 2))
                            a = (_screenWidth / 2);
                        if (b < (_screenWidth / 2))
                            b = (_screenWidth / 2);
                        if (a > roomData.Header.Width - (_screenWidth / 2))
                            a = roomData.Header.Width - (_screenWidth / 2);
                        if (b > roomData.Header.Width - (_screenWidth / 2))
                            b = roomData.Header.Width - (_screenWidth / 2);
                        _variables[VariableCameraMinX] = a;
                        _variables[VariableCameraMaxX] = b;
                    }
                    break;
                case 2:		// SO_ROOM_COLOR
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        _roomPalette[b] = (byte)a;
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
                case 4:		// SO_ROOM_PALETTE
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var b = GetVarOrDirectWord(OpCodeParameter.Param2);

                        // TODO: _shadowPalette
                        //_shadowPalette[b] = a;
                        //setDirtyColors(b, b);
                    }
                    break;
                case 5:		// SO_ROOM_SHAKE_ON
                    // TODO:
                    //setShake(1);
                    break;
                case 6:		// SO_ROOM_SHAKE_OFF
                    // TODO:
                    //    setShake(0);
                    break;
                case 7:		// SO_ROOM_SCALE
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
                case 10:	// SO_ROOM_FADE
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

        private void InitScreens(int b, int h)
        {
            _mainVirtScreen = new VirtScreen(b, _screenWidth, h - b, PixelFormat.Indexed8, 2, true) { TopLine = b };
            _textVirtScreen = new VirtScreen(0, _screenWidth, b, PixelFormat.Indexed8, 1) { TopLine = 0 };
            _verbVirtScreen = new VirtScreen(h, _screenWidth, _screenHeight - h, PixelFormat.Indexed8, 1) { TopLine = h };
            _gdi.Init();
        }

        private void StartObject()
        {
            int obj, script;

            obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            script = GetVarOrDirectByte(OpCodeParameter.Param2);

            var data = GetWordVarArgs();
            RunObjectScript(obj, script, false, false, data);
        }

        private void RunObjectScript(int obj, int entry, bool freezeResistant, bool recursive, IList<int> vars)
        {
            if (obj == 0)
                return;

            if (!recursive)
                StopObjectScript((ushort)obj);

            WhereIsObject where = GetWhereIsObject(obj);

            if (where == WhereIsObject.NotFound)
            {
                //warning("Code for object %d not in room %d", obj, _roomResource);
                return;
            }

            // Find a free object slot, unless one was specified
            byte slot = GetScriptSlotIndex();

            var e = (byte)entry;
            var data = (from o in roomData.Objects
                        where o.obj_nr == obj
                        where o.Scripts.ContainsKey(e) || o.Scripts.ContainsKey(0xFF)
                        select o.Scripts.ContainsKey(e) ? o.Scripts[e].Data : o.Scripts[0xFF].Data).FirstOrDefault();

            if (data == null)
            {
                data = (from o in _invData
                        where o != null
                        where o.obj_nr == obj
                        where o.Scripts.ContainsKey(e) || o.Scripts.ContainsKey(0xFF)
                        select o.Scripts.ContainsKey(e) ? o.Scripts[e].Data : o.Scripts[0xFF].Data).FirstOrDefault();
            }

            if (data == null)
                return;

            var count = (from o in roomData.Objects
                         where o.obj_nr == obj
                         select o.Scripts.Count).Concat(
                         from o in _invData
                         where o != null
                         where o.obj_nr == obj
                         select o.Scripts.Count).First();
            if (count == 0)
                return;

            //if (cycle == 0)
            //    cycle = (_game.heversion >= 90) ? VAR(VAR_SCRIPT_CYCLE) : 1;

            _slots[slot].number = (ushort)obj;
            _slots[slot].InventoryEntry = entry;
            _slots[slot].offs = 0;
            _slots[slot].status = ScriptStatus.Running;
            _slots[slot].where = where;
            _slots[slot].freezeResistant = freezeResistant;
            _slots[slot].recursive = recursive;
            _slots[slot].freezeCount = 0;
            _slots[slot].delayFrameCount = 0;
            //slots[slot].cycle = cycle;

            InitializeLocals(slot, vars);

            // V0 Ensure we don't try and access objects via index inside the script
            //_v0ObjectIndex = false;
            UpdateScriptData(slot);
            RunScriptNested(slot);
        }

        private void StopObjectCode()
        {
            if (_slots[_currentScript].where != WhereIsObject.Global && _slots[_currentScript].where != WhereIsObject.Local)
            {
                StopObjectScript(_slots[_currentScript].number);
            }
            else
            {
                _slots[_currentScript].number = 0;
                _slots[_currentScript].status = ScriptStatus.Dead;
            }
            _currentScript = 0xFF;
        }

        private void StopObjectScript(ushort script)
        {
            int i;

            if (script == 0)
                return;

            for (i = 0; i < NumScriptSlot; i++)
            {
                if (script == _slots[i].number && _slots[i].status != ScriptStatus.Dead &&
                    (_slots[i].where == WhereIsObject.Room || _slots[i].where == WhereIsObject.Inventory || _slots[i].where == WhereIsObject.FLObject))
                {
                    _slots[i].number = 0;
                    _slots[i].status = ScriptStatus.Dead;
                    if (_currentScript == i)
                        _currentScript = 0xFF;
                }
            }

            for (i = 0; i < _numNestedScripts; ++i)
            {
                if (_nest[i].number == script &&
                        (_nest[i].where == WhereIsObject.Room || _nest[i].where == WhereIsObject.Inventory || _nest[i].where == WhereIsObject.FLObject))
                {
                    _nest[i].number = 0xFF;
                    _nest[i].slot = 0xFF;
                    _nest[i].where = WhereIsObject.NotFound;
                }
            }
        }

        private void Delay()
        {
            uint delay = ReadByte();
            delay |= (uint)(ReadByte() << 8);
            delay |= (uint)(ReadByte() << 16);
            _slots[_currentScript].delay = (int)delay;
            _slots[_currentScript].status = ScriptStatus.Paused;
            BreakHere();
        }

        private void StartScript()
        {
            var op = _opCode;
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var data = GetWordVarArgs();

            // Copy protection was disabled in KIXX XL release (Amiga Disk) and
            // in LucasArts Classic Adventures (PC Disk)
            if (script != 0x98)
            {
                RunScript((byte)script, (op & 0x20) != 0, (op & 0x40) != 0, data);
            }
        }

        private void Move()
        {
            GetResult();
            var result = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(result);
        }

        private void JumpRelative(bool condition)
        {
            var offset = (short)ReadWord();
            if (!condition)
            {
                _currentPos += offset;
            }
        }

        private void IsEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(a == b);
        }

        private void IsNotEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(a != b);
        }

        private void Add()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = ReadVariable(_resultVarIndex);
            SetResult(a + b);
        }

        private void Divide()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(ReadVariable(_resultVarIndex) / a);
        }

        private void OldRoomEffect()
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

        private void FadeIn(byte effect)
        {
            if (_disableFadeInEffect)
            {
                // fadeIn() calls can be disabled in TheDig after a SMUSH movie
                // has been played. Like the original interpreter, we introduce
                // an extra flag to handle this.
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
                    throw new NotImplementedException();
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
                    break;
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

        private void UnkScreenEffect6()
        {
            DissolveEffect(8, 4);
        }

        private void DissolveEffect(int width, int height)
        {
            // TODO:
        }

        private void FadeOut(int effect)
        {
            _mainVirtScreen.SetDirtyRange(0, 0);

            Camera._last.X = Camera._cur.X;

            if (_screenEffectFlag && effect != 0)
            {
                // Fill screen 0 with black
                var l_pixNav = new PixelNavigator(_mainVirtScreen.Surfaces[0]);
                l_pixNav.OffsetX(_mainVirtScreen.XStart);
                Fill(l_pixNav, _mainVirtScreen.Pitch, 0, _mainVirtScreen.Width, _mainVirtScreen.Height);

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
                    //case 128:
                    //    unkScreenEffect6();
                    //    break;
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

        private void PseudoRoom()
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

        private void Subtract()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(ReadVariable(_resultVarIndex) - a);
        }

        private void Increment()
        {
            GetResult();
            SetResult(ReadVariable(_resultVarIndex) + 1);
        }

        private void Decrement()
        {
            GetResult();
            SetResult(ReadVariable(_resultVarIndex) - 1);
        }

        private void StartSound()
        {
            int sound = GetVarOrDirectByte(OpCodeParameter.Param1);

            // TODDO:
            //VAR(VAR_MUSIC_TIMER) = 0;
            //_sound->addSoundToQueue(sound);
        }

        private void AddObjectToDrawQue(byte obj)
        {
            _drawingObjects.Add(Objects[obj]);
        }

        private void DrawBox()
        {
            int x, y, x2, y2, color;

            x = GetVarOrDirectWord(OpCodeParameter.Param1);
            y = GetVarOrDirectWord(OpCodeParameter.Param2);

            _opCode = ReadByte();
            x2 = GetVarOrDirectWord(OpCodeParameter.Param1);
            y2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            color = GetVarOrDirectByte(OpCodeParameter.Param3);

            // TODO:
            //drawBox(x, y, x2, y2, color);
        }

        private void DrawObject()
        {
            byte state;
            int obj, idx, i;
            ushort x, y, w, h;
            int xpos, ypos;

            state = 1;
            xpos = ypos = 255;
            obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            xpos = GetVarOrDirectWord(OpCodeParameter.Param2);
            ypos = GetVarOrDirectWord(OpCodeParameter.Param3);

            idx = GetObjectIndex(obj);
            if (idx == -1)
                return;

            if (xpos != 0xFF)
            {
                Objects[idx].walk_x += (short)((xpos * 8) - Objects[idx].x_pos);
                Objects[idx].x_pos = (short)(xpos * 8);
                Objects[idx].walk_y += (short)((ypos * 8) - Objects[idx].y_pos);
                Objects[idx].y_pos = (short)(ypos * 8);
            }

            AddObjectToDrawQue((byte)idx);

            x = (ushort)Objects[idx].x_pos;
            y = (ushort)Objects[idx].y_pos;
            w = Objects[idx].width;
            h = Objects[idx].height;

            i = _numLocalObjects - 1;
            do
            {
                if (Objects[i].obj_nr != 0 && Objects[i].x_pos == x && Objects[i].y_pos == y && Objects[i].width == w && Objects[i].height == h)
                    PutState(Objects[i].obj_nr, 0);
            } while ((--i) != 0);

            PutState(obj, state);
        }

        private void PutState(int obj, byte state)
        {
            ScummHelper.AssertRange(0, obj, _numGlobalObjects - 1, "object");
            ScummHelper.AssertRange(0, state, 0xFF, "state");
            _scumm.ObjectStateTable[obj] = state;
        }

        private int GetObjectIndex(int obj)
        {
            int i;

            if (obj < 1)
                return -1;

            for (i = (_numLocalObjects - 1); i > 0; i--)
            {
                if (Objects[i].obj_nr == obj)
                    return i;
            }
            return -1;
        }

        private int GetVarOrDirectWord(OpCodeParameter param)
        {
            if ((_opCode & (byte)param) == (byte)param)
                return GetVar();
            return ReadWordSigned();
        }

        private int GetVarOrDirectByte(OpCodeParameter param)
        {
            if ((_opCode & (byte)param) == (byte)param)
                return GetVar();
            return ReadByte();
        }

        private int GetVar()
        {
            return ReadVariable(ReadWord());
        }

        private short ReadWordSigned()
        {
            return (short)ReadWord();
        }

        private void IsLess()
        {
            var varNum = ReadWord();
            short a = (short)ReadVariable(varNum);
            short b = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b < a);
        }

        private void IsLessEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b <= a);
        }

        private void IsGreater()
        {
            var a = ReadVariable(ReadWord());
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b > a);
        }

        private void IsGreaterEqual()
        {
            var a = ReadVariable(ReadWord());
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b >= a);
        }

        private void Multiply()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(_resultVarIndex);
            SetResult(a * b);
        }

        private void Or()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(_resultVarIndex);
            SetResult(a | b);
        }

        private void And()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(_resultVarIndex);
            SetResult(a & b);
        }

        private void NotEqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);
            JumpRelative(a != 0);
        }

        private void EqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);
            JumpRelative(a == 0);
        }

        private void JumpRelative()
        {
            JumpRelative(false);
        }

        private void StringOperations()
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
                        _strings[idA] = _strings[idB].ToArray();
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

        private byte[] ReadCharacters()
        {
            byte character;
            List<byte> sb = new List<byte>();
            character = ReadByte();
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

        private void ResourceRoutines()
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
                    // TODO:
                    break;
                case 4: // load room
                    // TODO:
                    break;
                case 9:			// SO_LOCK_SCRIPT
                    if (resId < NumGlobalScripts)
                    {
                        //_res.Sounds[resId].Lock = true;
                    }
                    break;
                case 10:
                    // TODO: lock Sound
                    break;
                case 11:		// SO_LOCK_COSTUME
                    //_res.Costumes[resId].Lock = true;
                    break;
                case 13:		// SO_UNLOCK_SCRIPT
                    break;
                case 14:		// SO_UNLOCK_SOUND
                    break;
                case 15:		// SO_UNLOCK_COSTUME
                    // TODO:
                    break;
                case 16:		// SO_UNLOCK_ROOM
                    if (resId > 0x7F)
                        resId = _resourceMapper[resId & 0x7F];
                    // TODO:
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

        private void LoadCharset(int resId)
        {
            var diskName = string.Format("{0}.lfl", 900 + resId);
            var path = System.IO.Path.Combine(_directory, diskName);
            using (var br = new BinaryReader(File.OpenRead(path)))
            {
                var size = (int)br.ReadUInt32() + 11;
                _charsets[resId] = br.ReadBytes(size);
            }
        }

        private void CursorCommand()
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
                case 7:			// SO_USERPUT_SOFT_ON
                    _userPut++;
                    break;
                case 8:			// SO_USERPUT_SOFT_OFF
                    _userPut--;
                    break;
                case 10:
                    {
                        // SO_CURSOR_IMAGE
                        var i = GetVarOrDirectByte(OpCodeParameter.Param1);	// Cursor number
                        var j = GetVarOrDirectByte(OpCodeParameter.Param2);	// Charset letter to use
                        // TODO:
                        //redefineBuiltinCursorFromChar(i, j);
                    }
                    break;
                case 11:		// SO_CURSOR_HOTSPOT
                    {
                        var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var k = GetVarOrDirectByte(OpCodeParameter.Param3);
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

        private void InitCharset(int charsetNum)
        {
            _string[0].Default.charset = (byte)charsetNum;
            _string[1].Default.charset = (byte)charsetNum;

            //if (_charsets[charsetNum] != null)
            //{
            //    Array.Copy(_charsets[charsetNum], _charsetColorMap, 16);
            //}
        }

        private void Expression()
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

        private void SetVarRange()
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

        private void StopScript()
        {
            int script;

            script = GetVarOrDirectByte(OpCodeParameter.Param1);

            if (script == 0)
                StopObjectCode();
            else
                StopScript(script);
        }

        private void VerbOps()
        {
            var verb = GetVarOrDirectByte(OpCodeParameter.Param1);
            var slot = GetVerbSlot(verb, 0);
            ScummHelper.AssertRange(0, slot, _verbs.Length - 1, "new verb slot");
            var vs = _verbs[slot];
            vs.verbid = (ushort)verb;

            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0x1F)
                {
                    case 1:		// SO_VERB_IMAGE
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        if (slot != 0)
                        {
                            //setVerbObject(_roomResource, a, slot);
                            vs.type = VerbType.Image;
                        }
                        break;
                    case 2:		// SO_VERB_NAME
                        vs.Text = ReadCharacters();
                        vs.type = VerbType.Text;
                        vs.imgindex = 0;
                        break;
                    case 3:		// SO_VERB_COLOR
                        vs.color = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 4:		// SO_VERB_HICOLOR
                        vs.hicolor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 5:		// SO_VERB_AT
                        var left = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var top = GetVarOrDirectWord(OpCodeParameter.Param2);
                        vs.curRect.left = left;
                        vs.curRect.top = top;
                        break;
                    case 6:
                        // SO_VERB_ON
                        vs.curmode = 1;
                        break;
                    case 7:
                        // SO_VERB_OFF
                        vs.curmode = 0;
                        break;
                    case 9:
                        {
                            // SO_VERB_NEW
                            slot = GetVerbSlot(verb, 0);

                            if (slot == 0)
                            {
                                for (slot = 1; slot < _verbs.Length; slot++)
                                {
                                    if (_verbs[slot].verbid == 0)
                                        break;
                                }
                            }
                            vs = _verbs[slot];
                            vs.verbid = (ushort)verb;
                            vs.color = 2;
                            vs.hicolor = 0;
                            vs.dimcolor = 8;
                            vs.type = VerbType.Text;
                            vs.charset_nr = _string[0].Default.charset;
                            vs.curmode = 0;
                            vs.saveid = 0;
                            vs.key = 0;
                            vs.center = false;
                            vs.imgindex = 0;
                            break;
                        }
                    case 16:	// SO_VERB_DIMCOLOR
                        vs.dimcolor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 17:	// SO_VERB_DIM
                        vs.curmode = 2;
                        break;
                    case 18:	// SO_VERB_KEY
                        vs.key = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 19:	// SO_VERB_CENTER
                        vs.center = true;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            // Force redraw of the modified verb slot
            DrawVerb(slot, 0);
            VerbMouseOver(0);
        }

        private void GetRandomNumber()
        {
            GetResult();
            var max = GetVarOrDirectByte(OpCodeParameter.Param1);
            Random rnd = new Random();
            SetResult(rnd.Next(max));
        }

        private void BreakHere()
        {
            _slots[_currentScript].offs = (uint)_currentPos;
            _currentScript = 0xFF;
        }

        private void SetState()
        {
            int obj;
            byte state;
            obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            state = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);
            PutState(obj, state);
            MarkObjectRectAsDirty(obj);
            if (_bgNeedsRedraw)
                ClearDrawObjectQueue();
        }

        private void MarkObjectRectAsDirty(int obj)
        {
            int i, strip;

            for (i = 1; i < this._numLocalObjects; i++)
            {
                if (this.Objects[i].obj_nr == (ushort)obj)
                {
                    if (this.Objects[i].width != 0)
                    {
                        int minStrip = Math.Max(_screenStartStrip, this.Objects[i].x_pos / 8);
                        int maxStrip = Math.Min(_screenEndStrip + 1, this.Objects[i].x_pos / 8 + this.Objects[i].width / 8);
                        for (strip = minStrip; strip < maxStrip; strip++)
                        {
                            SetGfxUsageBit(strip, UsageBitDirty);
                        }
                    }
                    _bgNeedsRedraw = true;
                    return;
                }
            }
        }

        private void ClearDrawObjectQueue()
        {
            _drawingObjects.Clear();
        }

        private void PickupObject()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            if (obj < 1)
            {
                string msg = string.Format("pickupObjectOld received invalid index %d (script %d)", obj, _slots[_currentScript].number);
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

        private void AddObjectToInventory(int obj, byte room)
        {
            var slot = GetInventorySlot();
            if (GetWhereIsObject(obj) == WhereIsObject.FLObject)
            {
                var idx = GetObjectIndex(obj);
                throw new NotImplementedException();
            }
            else
            {
                var objs = _scumm.GetRoom(room).Objects;
                var objFound = (from o in objs
                                where o.obj_nr == obj
                                select o).FirstOrDefault();
                _invData[slot] = objFound;
            }
            _inventory[slot] = (ushort)obj;

        }

        private int GetInventorySlot()
        {
            int i;
            for (i = 0; i < NumInventory; i++)
            {
                if (_inventory[i] == 0)
                    return i;
            }
            return -1;
        }

        private void PutOwner(int obj, byte owner)
        {
            _scumm.ObjectOwnerTable[obj] = owner;
        }

        ObjectData[] _invData = new ObjectData[NumInventory];


        #endregion

        #region Properties
        public IList<ObjectData> Objects
        {
            get
            {
                return _objs;
            }
        }

        public int ScreenStartStrip
        {
            get { return _screenStartStrip; }
        }

        public int CharsetBufPos
        {
            get { return _charsetBufPos; }
            set { _charsetBufPos = value; }
        }

        public int HaveMsg
        {
            get { return _haveMsg; }
            set { _haveMsg = value; }
        }

        public bool EgoPositioned
        {
            get { return _egoPositioned; }
            set { _egoPositioned = value; }
        }

        public int TalkDelay
        {
            get { return _talkDelay; }
            set { _talkDelay = value; }
        }

        public ICostumeLoader CostumeLoader
        {
            get { return _costumeLoader; }
        }

        public ICostumeRenderer CostumeRenderer { get { return _costumeRenderer; } }

        public Surface TextSurface { get { return _textSurface; } }
        #endregion

        #region Misc Methods
        private Dictionary<int, byte[]> _newNames = new Dictionary<int, byte[]>();

        private void SetObjectName(int obj)
        {
            if (obj < _actors.Length)
            {
                string msg = string.Format("Can't set actor {0} name with new name.", obj);
                throw new NotSupportedException(msg);
            }

            _newNames[obj] = ReadCharacters();
            RunInventoryScript(0);
        }

        private void PanCameraTo(int x)
        {
            _camera._dest.X = (short)x;
            _camera._mode = CameraMode.Panning;
            _camera._movingToActor = false;
        }

        private int GetObjX(int obj)
        {
            if (obj < 1)
                return 0;									/* fix for indy4's map */

            if (obj < _actors.Length)
            {
                return _actors[obj].GetRealPos().X;
            }
            else
            {
                if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                    return -1;
                int x, y;
                GetObjectOrActorXY(obj, out x, out y);
                return x;
            }
        }

        private int GetObjY(int obj)
        {
            if (obj < 1)
                return 0;									/* fix for indy4's map */

            if (obj < _actors.Length)
            {
                return _actors[obj].GetRealPos().Y;
            }
            else
            {
                if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                    return -1;
                int x, y;
                GetObjectOrActorXY(obj, out x, out y);
                return y;
            }
        }

        private WhereIsObject GetWhereIsObject(int obj)
        {
            int i;

            if (obj >= _numGlobalObjects)
                return WhereIsObject.NotFound;

            if (obj < 1)
                return WhereIsObject.NotFound;

            if (_scumm.ObjectOwnerTable[obj] != OF_OWNER_ROOM)
            {
                for (i = 0; i < NumInventory; i++)
                    if (_inventory[i] == obj)
                        return WhereIsObject.Inventory;
                return WhereIsObject.NotFound;
            }

            for (i = (_numLocalObjects - 1); i > 0; i--)
                if (Objects[i].obj_nr == obj)
                {
                    if (Objects[i].fl_object_index != 0)
                        return WhereIsObject.FLObject;
                    return WhereIsObject.Room;
                }

            return WhereIsObject.NotFound;
        }

        private void KillScriptsAndResources()
        {
            int i;

            for (i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].where == WhereIsObject.Room || _slots[i].where == WhereIsObject.FLObject)
                {
                    if (_slots[i].cutsceneOverride != 0)
                    {
                        _slots[i].cutsceneOverride = 0;
                    }
                    //nukeArrays(i);
                    _slots[i].status = ScriptStatus.Dead;
                }
                else if (_slots[i].where == WhereIsObject.Local)
                {
                    if (_slots[i].cutsceneOverride != 0)
                    {
                        _slots[i].cutsceneOverride = 0;
                    }
                    //nukeArrays(i);
                    _slots[i].status = ScriptStatus.Dead;
                }
            }
        }

        public void WalkActors()
        {
            for (int i = 1; i < NumActors; ++i)
            {
                if (_actors[i].IsInCurrentRoom())
                    _actors[i].WalkActor();
            }
        }

        public void ProcessActors()
        {
            var actors = from actor in this.Actors
                         where actor.IsInCurrentRoom()
                         orderby actor.GetPos().Y
                         select actor;

            foreach (var actor in actors)
            {
                if (actor._costume != 0)
                {
                    actor.DrawActorCostume();
                    actor.AnimateCostume();
                }
            }
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
        public int GetNextBox(byte from, byte to)
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

            //assert(from < numOfBoxes);
            //assert(to < numOfBoxes);

            var boxm = CurrentRoomData.BoxMatrix;

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

            int boxmIndex = 0;
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

        private void StartScene(byte room)
        {
            StopTalk();

            FadeOut(_switchRoomEffect2);
            _newEffect = _switchRoomEffect;

            if (_currentScript != 0xFF)
            {
                if (_slots[_currentScript].where == WhereIsObject.Room || _slots[_currentScript].where == WhereIsObject.FLObject)
                {
                    //if (slots[_currentScript].cutsceneOverride && _game.version >= 5)
                    //    error("Object %d stopped with active cutscene/override in exit", slots[_currentScript].number);

                    //nukeArrays(_currentScript);
                    _currentScript = 0xFF;
                }
                else if (_slots[_currentScript].where == WhereIsObject.Local)
                {
                    //if (slots[_currentScript].cutsceneOverride && _game.version >= 5)
                    //    error("Script %d stopped with active cutscene/override in exit", slots[_currentScript].number);

                    //nukeArrays(_currentScript);
                    _currentScript = 0xFF;
                }
            }

            RunExitScript();

            KillScriptsAndResources();

            for (int i = 1; i < NumActors; i++)
            {
                _actors[i].HideActor();
            }

            for (int i = 0; i < 256; i++)
            {
                _roomPalette[i] = (byte)i;
                //if (_shadowPalette)
                //    _shadowPalette[i] = i;
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
            if (roomData != null)
            {
                _gfxManager.SetPalette(roomData.Palette.Colors.ToArray());
            }

            if (_currentRoom == 0)
            {
                return;
            }

            _gdi.TransparentColor = roomData.TransparentColor;
            ResetRoomSubBlocks();
            ResetRoomObjects();
            this.DrawingObjects.Clear();

            _variables[VariableCameraMinX] = _screenWidth / 2;
            _variables[VariableCameraMaxX] = roomData.Header.Width - (_screenWidth / 2);

            _camera._mode = CameraMode.Normal;
            _camera._cur.X = _camera._dest.X = (short)(_screenWidth / 2);
            _camera._cur.Y = _camera._dest.Y = (short)(_screenHeight / 2);

            if (_roomResource == 0)
                return;

            Array.Clear(_gfxUsageBits, 0, _gfxUsageBits.Length);

            ShowActors();

            _egoPositioned = false;

            RunEntryScript();

            _doEffect = true;
        }

        private void ResetRoomObjects()
        {
            if (roomData == null) return;
            for (int i = 0; i < roomData.Objects.Count; i++)
            {
                _objs[i + 1].x_pos = roomData.Objects[i].x_pos;
                _objs[i + 1].y_pos = roomData.Objects[i].y_pos;
                _objs[i + 1].width = roomData.Objects[i].width;
                _objs[i + 1].walk_x = roomData.Objects[i].walk_x;
                _objs[i + 1].walk_y = roomData.Objects[i].walk_y;
                _objs[i + 1].state = roomData.Objects[i].state;
                _objs[i + 1].parent = roomData.Objects[i].parent;
                _objs[i + 1].parentstate = roomData.Objects[i].parentstate;
                _objs[i + 1].obj_nr = roomData.Objects[i].obj_nr;
                _objs[i + 1].height = roomData.Objects[i].height;
                _objs[i + 1].flags = roomData.Objects[i].flags;
                _objs[i + 1].fl_object_index = roomData.Objects[i].fl_object_index;
                _objs[i + 1].actordir = roomData.Objects[i].actordir;
                _objs[i + 1].Scripts.Clear();
                _objs[i + 1].Image = roomData.Objects[i].Image;
                foreach (var script in roomData.Objects[i].Scripts)
                {
                    _objs[i + 1].Scripts.Add(script.Key, script.Value);
                }
                _objs[i + 1].ScriptOffsets.Clear();
                foreach (var scriptOffset in roomData.Objects[i].ScriptOffsets)
                {
                    _objs[i + 1].ScriptOffsets.Add(scriptOffset.Key, scriptOffset.Value);
                }
                _objs[i + 1].Name = roomData.Objects[i].Name;
            }
            for (int i = roomData.Objects.Count + 1; i < _objs.Length; i++)
            {
                _objs[i].obj_nr = 0;
                _objs[i].Scripts.Clear();
            }
        }

        private void ClearRoomObjects()
        {
            for (int i = 0; i < _numLocalObjects; i++)
            {
                this.Objects[i].obj_nr = 0;
            }
        }

        private void ResetRoomSubBlocks()
        {
            if (roomData != null)
            {
                if (roomData.Scales != null)
                {
                    for (int i = 1; i < roomData.Scales.Length; i++)
                    {
                        var scale = roomData.Scales[i - 1];
                        if (scale.scale1 != 0 || scale.y1 != 0 || scale.scale2 != 0 || scale.y2 != 0)
                        {
                            SetScaleSlot(i, 0, scale.y1, scale.scale1, 0, scale.y2, scale.scale2);
                        }
                    }
                }
                else
                {
                    Array.Clear(_scaleSlots, 0, _scaleSlots.Length);
                }

                _boxes = new Box[roomData.Boxes.Count];
                for (int i = 0; i < roomData.Boxes.Count; i++)
                {
                    var box = roomData.Boxes[i];
                    _boxes[i] = new Box { flags = box.flags, llx = box.llx, lly = box.lly, lrx = box.lrx, lry = box.lry, mask = box.mask, scale = box.scale, ulx = box.ulx, uly = box.uly, urx = box.urx, ury = box.ury };
                }
            }
        }

        private void SetDirtyColors(int min, int max)
        {
            if (_palDirtyMin > min)
                _palDirtyMin = min;
            if (_palDirtyMax < max)
                _palDirtyMax = max;
        }

        private void ShowActors()
        {
            int i;

            for (i = 1; i < NumActors; i++)
            {
                if (_actors[i].IsInCurrentRoom())
                    _actors[i].ShowActor();
            }
        }

        private void Print()
        {
            _actorToPrintStrFor = GetVarOrDirectByte(OpCodeParameter.Param1);
            DecodeParseString();
        }

        private void DecodeParseString()
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
                    case 0:		// SO_AT
                        _string[textSlot].xpos = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
                        _string[textSlot].ypos = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
                        _string[textSlot].overhead = false;
                        break;
                    case 1:		// SO_COLOR
                        _string[textSlot].color = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 2:		// SO_CLIPPED
                        _string[textSlot].right = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
                        break;
                    case 4:		// SO_CENTER
                        _string[textSlot].center = true;
                        _string[textSlot].overhead = false;
                        break;
                    case 6:		// SO_LEFT
                        {
                            _string[textSlot].center = false;
                            _string[textSlot].overhead = false;
                        }
                        break;
                    case 7:		// SO_OVERHEAD
                        _string[textSlot].overhead = true;
                        break;
                    case 15:
                        {	// SO_TEXTSTRING
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

        private void PrintString(int textSlot, byte[] msg)
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

        private void ActorTalk(byte[] msg)
        {
            Actor a;

            ConvertMessageToString(msg, _charsetBuffer, 0);

            if (_actorToPrintStrFor == 0xFF)
            {
                if (!_keepText)
                {
                    StopTalk();
                }
                SetTalkingActor(0xFF);
            }
            else
            {
                int oldact;

                a = _actors[_actorToPrintStrFor];
                if (!a.IsInCurrentRoom())
                {
                    oldact = 0xFF;
                }
                else
                {
                    if (!_keepText)
                    {
                        StopTalk();
                    }
                    SetTalkingActor(a._number);

                    if (!_string[0].no_talk_anim)
                    {
                        a.RunActorTalkScript(a._talkStartFrame);
                        _useTalkAnims = true;
                    }
                    oldact = GetTalkingActor();
                }
                if (oldact >= 0x80)
                    return;
            }

            if (GetTalkingActor() > 0x7F)
            {
                _charsetColor = (byte)_string[0].color;
            }
            else
            {
                a = _actors[GetTalkingActor()];
                _charsetColor = a._talkColor;
            }

            _charsetBufPos = 0;
            _talkDelay = 0;
            _haveMsg = 0xFF;
            _variables[VariableHaveMessage] = 0xFF;

            _haveActorSpeechMsg = true;
            CHARSET_1();
        }

        public int GetTalkingActor()
        {
            return _variables[VariableTalkActor];
        }

        private void SetTalkingActor(int actor)
        {
            //        if (i == 255) {
            //    _system->clearFocusRectangle();
            //} else {
            //    // Work out the screen co-ordinates of the actor
            //    int x = _actors[i]->getPos().x - (camera._cur.x - (_screenWidth >> 1));
            //    int y = _actors[i]->_top - (camera._cur.y - (_screenHeight >> 1));

            //    // Set the focus area to the calculated position
            //    // TODO: Make the size adjust depending on what it's focusing on.
            //    _system->setFocusRectangle(Common::Rect::center(x, y, 192, 128));
            //}

            _variables[VariableTalkActor] = actor;
        }

        private void RunEntryScript()
        {
            if (_variables[VariableEntryScript] != 0)
                RunScript((byte)_variables[VariableEntryScript], false, false, new int[] { });

            if (roomData != null && roomData.EntryScript.Data != null)
            {
                int slot = GetScriptSlotIndex();
                _slots[slot].status = ScriptStatus.Running;
                _slots[slot].number = 10002;
                _slots[slot].where = WhereIsObject.Room;
                _slots[slot].offs = 0;
                _slots[slot].freezeResistant = false;
                _slots[slot].recursive = false;
                _slots[slot].freezeCount = 0;
                _slots[slot].delayFrameCount = 0;
                _currentScriptData = roomData.EntryScript.Data;
                InitializeLocals((byte)slot, new int[] { });
                RunScriptNested((byte)slot);
            }

            if (_variables[VariableEntryScript2] != 0)
                RunScript((byte)_variables[VariableEntryScript2], false, false, new int[] { });
        }

        private void RunExitScript()
        {
            if (_variables[VariableExitScript] != 0)
            {
                RunScript((byte)_variables[VariableExitScript], false, false, new int[] { });
            }

            if (roomData != null && roomData.ExitScript.Data != null)
            {
                int slot = GetScriptSlotIndex();
                _slots[slot].status = ScriptStatus.Running;
                _slots[slot].number = 10001;
                _slots[slot].where = WhereIsObject.Room;
                _slots[slot].offs = 0;
                _slots[slot].freezeResistant = false;
                _slots[slot].recursive = false;
                _slots[slot].freezeCount = 0;
                _slots[slot].delayFrameCount = 0;
                _currentScriptData = roomData.ExitScript.Data;
                InitializeLocals((byte)slot, new int[] { });
                RunScriptNested((byte)slot);
            }
        }

        public void RunBootScript()
        {
            RunScript(1, false, false, new int[] { });
        }

        public void StopScript(int script)
        {
            int i;

            if (script == 0)
                return;

            for (i = 0; i < NumScriptSlot; i++)
            {
                if (script == this._slots[i].number && this._slots[i].status != ScriptStatus.Dead &&
                    (this._slots[i].where == WhereIsObject.Global || this._slots[i].where == WhereIsObject.Local))
                {
                    this._slots[i].number = 0;
                    this._slots[i].status = ScriptStatus.Dead;
                    //nukeArrays(i);
                    if (_currentScript == i)
                        _currentScript = 0xFF;
                }
            }

            for (i = 0; i < _numNestedScripts; ++i)
            {
                if (_nest[i].number == script &&
                        (_nest[i].where == WhereIsObject.Global || _nest[i].where == WhereIsObject.Local))
                {
                    //nukeArrays(vm.nest[i].slot);
                    _nest[i].number = 0xFF;
                    _nest[i].slot = 0xFF;
                    _nest[i].where = WhereIsObject.NotFound;
                }
            }
        }

        public void RunScript(byte scriptNum, bool freezeResistant, bool recursive, IList<int> data)
        {
            WhereIsObject scriptType;

            if (scriptNum == 0)
                return;

            if (!recursive)
                StopScript(scriptNum);

            if (scriptNum < NumGlobalScripts)
            {
                scriptType = WhereIsObject.Global;
            }
            else
            {
                scriptType = WhereIsObject.Local;
            }

            var slotIndex = GetScriptSlotIndex();
            _slots[slotIndex].number = scriptNum;
            _slots[slotIndex].offs = 0;
            _slots[slotIndex].status = ScriptStatus.Running;
            _slots[slotIndex].freezeResistant = freezeResistant;
            _slots[slotIndex].recursive = recursive;
            _slots[slotIndex].where = scriptType;
            _slots[slotIndex].freezeCount = 0;
            _slots[slotIndex].delayFrameCount = 0;

            this.UpdateScriptData(slotIndex);
            this.InitializeLocals(slotIndex, data);
            this.RunScriptNested(slotIndex);
        }

        private void InitializeLocals(byte slotIndex, IList<int> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                this._localVariables[slotIndex][i] = data[i];
            }
            for (int i = data.Count; i < 25; i++)
            {
                this._localVariables[slotIndex][i] = 0;
            }
        }

        private void UpdateScriptData(ushort slotIndex)
        {
            var scriptNum = _slots[slotIndex].number;
            if (roomData != null && (_slots[slotIndex].where == WhereIsObject.Inventory))
            {
                var data = (from o in roomData.Objects.Concat(_invData)
                            where o.obj_nr == scriptNum
                            select o.Scripts.ContainsKey((byte)_slots[slotIndex].InventoryEntry) ?
                            o.Scripts[(byte)_slots[slotIndex].InventoryEntry].Data : o.Scripts[0xFF].Data).FirstOrDefault();
                _currentScriptData = data;
            }
            else if (scriptNum < NumGlobalScripts)
            {
                var data = _scumm.GetScript((byte)scriptNum);
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
            else if (((scriptNum - NumGlobalScripts) < this.roomData.LocalScripts.Length) && (this.roomData.LocalScripts[scriptNum - NumGlobalScripts] != null))
            {
                _currentScriptData = this.roomData.LocalScripts[scriptNum - NumGlobalScripts].Data;
            }
            else
            {
                var data = (from o in roomData.Objects
                            where o.obj_nr == scriptNum
                            let entry = (byte)_slots[slotIndex].InventoryEntry
                            where o.Scripts.ContainsKey(entry) || o.Scripts.ContainsKey(0xFF)
                            select o.Scripts.ContainsKey(entry) ? o.Scripts[entry].Data : o.Scripts[0xFF].Data).FirstOrDefault();
                _currentScriptData = data;
            }
        }

        private void RunScriptNested(byte script)
        {
            if (_currentScript == 0xFF)
            {
                _nest[_numNestedScripts].number = 0xFF;
                _nest[_numNestedScripts].where = WhereIsObject.NotFound;
            }
            else
            {
                // Store information about the currently running script
                _slots[_currentScript].offs = (uint)_currentPos;
                _nest[_numNestedScripts].number = _slots[_currentScript].number;
                _nest[_numNestedScripts].where = _slots[_currentScript].where;
                _nest[_numNestedScripts].slot = _currentScript;
            }

            _numNestedScripts++;

            _currentScript = script;
            ResetScriptPointer();
            Run();

            if (_numNestedScripts != 0)
                _numNestedScripts--;

            var nest = _nest[_numNestedScripts];
            if (nest.number != 0xFF)
            {
                // Try to resume the script which called us, if its status has not changed
                // since it invoked us. In particular, we only resume it if it hasn't been
                // stopped in the meantime, and if it did not already move on.
                var slot = _slots[nest.slot];
                if (slot.number == nest.number && slot.where == nest.where &&
                        slot.status != ScriptStatus.Dead && slot.freezeCount == 0)
                {
                    _currentScript = nest.slot;
                    UpdateScriptData(nest.slot);
                    ResetScriptPointer();
                    return;
                }
            }
            _currentScript = 0xFF;
        }

        private void ResetScriptPointer()
        {
            _currentPos = (int)_slots[_currentScript].offs;
        }

        private byte GetScriptSlotIndex()
        {
            for (byte i = 1; i < NumScriptSlot; i++)
            {
                if (_slots[i].status == ScriptStatus.Dead)
                    return i;
            }
            return 0xFF;
        }

        public void RunAllScripts()
        {
            for (int i = 0; i < NumScriptSlot; i++)
                _slots[i].didexec = false;

            _currentScript = 0xFF;

            for (int i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].status == ScriptStatus.Running && !_slots[i].didexec)
                {
                    _currentScript = (byte)i;
                    UpdateScriptData((ushort)i);
                    ResetScriptPointer();
                    this.Run();
                }
            }
        }

        public BoxFlags GetBoxFlags(byte boxNum)
        {
            Box box = GetBoxBase(boxNum);
            if (box == null)
                return 0;
            return box.flags;
        }

        public byte GetBoxMask(byte boxNum)
        {
            Box box = GetBoxBase(boxNum);
            if (box == null)
                return 0;
            return box.mask;
        }

        Box[] _boxes;
        private Box GetBoxBase(byte boxNum)
        {
            Box box = null;
            if (boxNum != 0xFF)
            {
                box = _boxes[boxNum];
            }
            return box;
        }

        public int GetNumBoxes()
        {
            return _boxes.Length;
        }

        public BoxCoords GetBoxCoordinates(int boxnum)
        {
            Box bp = GetBoxBase(boxnum);
            BoxCoords box = new BoxCoords();

            box.ul.X = bp.ulx;
            box.ul.Y = bp.uly;
            box.ur.X = bp.urx;
            box.ur.Y = bp.ury;

            box.ll.X = bp.llx;
            box.ll.Y = bp.lly;
            box.lr.X = bp.lrx;
            box.lr.Y = bp.lry;

            return box;
        }

        private Box GetBoxBase(int boxnum)
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

        public bool CheckXYInBoxBounds(int boxnum, short x, short y)
        {
            // Since this method is called by many other methods that take params
            // from e.g. script opcodes, but do not validate the boxnum, we
            // make a check here to filter out invalid boxes.
            // See also bug #1599113.
            if (boxnum < 0 || boxnum == Actor.InvalidBox)
                return false;

            BoxCoords box = GetBoxCoordinates(boxnum);
            Point p = new Point(x, y);

            // Quick check: If the x (resp. y) coordinate of the point is
            // strictly smaller (bigger) than the x (y) coordinates of all
            // corners of the quadrangle, then it certainly is *not* contained
            // inside the quadrangle.
            if (x < box.ul.X && x < box.ur.X && x < box.lr.X && x < box.ll.X)
                return false;

            if (x > box.ul.X && x > box.ur.X && x > box.lr.X && x > box.ll.X)
                return false;

            if (y < box.ul.Y && y < box.ur.Y && y < box.lr.Y && y < box.ll.Y)
                return false;

            if (y > box.ul.Y && y > box.ur.Y && y > box.lr.Y && y > box.ll.Y)
                return false;

            // Corner case: If the box is a simple line segment, we consider the
            // point to be contained "in" (or rather, lying on) the line if it
            // is very close to its projection to the line segment.
            if ((box.ul == box.ur && box.lr == box.ll) ||
                (box.ul == box.ll && box.ur == box.lr))
            {

                Point tmp;
                tmp = ClosestPtOnLine(box.ul, box.lr, p);
                if (p.SquareDistance(tmp) <= 4)
                    return true;
            }

            // Finally, fall back to the classic algorithm to compute containment
            // in a convex polygon: For each (oriented) side of the polygon
            // (quadrangle in this case), compute whether p is "left" or "right"
            // from it.

            if (!CompareSlope(box.ul, box.ur, p))
                return false;

            if (!CompareSlope(box.ur, box.lr, p))
                return false;

            if (!CompareSlope(box.lr, box.ll, p))
                return false;

            if (!CompareSlope(box.ll, box.ul, p))
                return false;

            return true;
        }

        private static bool CompareSlope(Point p1, Point p2, Point p3)
        {
            return (p2.Y - p1.Y) * (p3.X - p1.X) <= (p3.Y - p1.Y) * (p2.X - p1.X);
        }

        private static Point ClosestPtOnLine(Point lineStart, Point lineEnd, Point p)
        {
            Point result;

            int lxdiff = lineEnd.X - lineStart.X;
            int lydiff = lineEnd.Y - lineStart.Y;

            if (lineEnd.X == lineStart.X)
            {	// Vertical line?
                result.X = lineStart.X;
                result.Y = p.Y;
            }
            else if (lineEnd.Y == lineStart.Y)
            {	// Horizontal line?
                result.X = p.X;
                result.Y = lineStart.Y;
            }
            else
            {
                int dist = lxdiff * lxdiff + lydiff * lydiff;
                int a, b, c;
                if (Math.Abs(lxdiff) > Math.Abs(lydiff))
                {
                    a = lineStart.X * lydiff / lxdiff;
                    b = p.X * lxdiff / lydiff;

                    c = (a + b - lineStart.Y + p.Y) * lydiff * lxdiff / dist;

                    result.X = (short)c;
                    result.Y = (short)(c * lydiff / lxdiff - a + lineStart.Y);
                }
                else
                {
                    a = lineStart.Y * lxdiff / lydiff;
                    b = p.Y * lydiff / lxdiff;

                    c = (a + b - lineStart.X + p.X) * lydiff * lxdiff / dist;

                    result.X = (short)(c * lxdiff / lydiff - a + lineStart.X);
                    result.Y = (short)c;
                }
            }

            if (Math.Abs(lydiff) < Math.Abs(lxdiff))
            {
                if (lxdiff > 0)
                {
                    if (result.X < lineStart.X)
                        result = lineStart;
                    else if (result.X > lineEnd.X)
                        result = lineEnd;
                }
                else
                {
                    if (result.X > lineStart.X)
                        result = lineStart;
                    else if (result.X < lineEnd.X)
                        result = lineEnd;
                }
            }
            else
            {
                if (lydiff > 0)
                {
                    if (result.Y < lineStart.Y)
                        result = lineStart;
                    else if (result.Y > lineEnd.Y)
                        result = lineEnd;
                }
                else
                {
                    if (result.Y > lineStart.Y)
                        result = lineStart;
                    else if (result.Y < lineEnd.Y)
                        result = lineEnd;
                }
            }

            return result;
        }

        public void DecreaseScriptDelay(int amount)
        {
            _talkDelay -= amount;
            if (_talkDelay < 0) _talkDelay = 0;
            int i;
            for (i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].status == ScriptStatus.Paused)
                {
                    _slots[i].delay -= amount;
                    if (_slots[i].delay < 0)
                    {
                        _slots[i].status = ScriptStatus.Running;
                        _slots[i].delay = 0;
                    }
                }
            }
        }

        public void AbortCutscene()
        {
            int idx = cutSceneStackPointer;

            var offs = cutScenePtr[idx];
            if (offs != 0)
            {
                _slots[cutSceneScript[idx]].offs = offs;
                _slots[cutSceneScript[idx]].status = ScriptStatus.Running;
                _slots[cutSceneScript[idx]].freezeCount = 0;

                if (_slots[cutSceneScript[idx]].cutsceneOverride > 0)
                    _slots[cutSceneScript[idx]].cutsceneOverride--;

                _variables[VariableOverride] = 1;
                cutScenePtr[idx] = 0;
            }
        }

        public void StopTalk()
        {
            //_sound->stopTalkSound();

            _haveMsg = 0;
            _talkDelay = 0;

            var act = GetTalkingActor();
            if (act != 0 && act < 0x80)
            {
                Actor a = _actors[act];
                if (a.IsInCurrentRoom() && _useTalkAnims)
                {
                    a.RunActorTalkScript(a._talkStopFrame);
                    _useTalkAnims = false;
                }
                SetTalkingActor(0xFF);
            }

            _keepText = false;
            RestoreCharsetBg();
        }

        public bool GetClass(int obj, ObjectClass cls)
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

        public KeyCode MouseAndKeyboardStat { get; set; }

        public void CheckExecVerbs()
        {
            if (_userPut <= 0 || MouseAndKeyboardStat == 0)
                return;

            if ((ScummMouseButtonState)MouseAndKeyboardStat < ScummMouseButtonState.MaxKey)
            {
                // Check keypresses
                var vs = (from verb in _verbs.Skip(1)
                          where verb.verbid != 0 && verb.saveid == 0 && verb.curmode == 1
                          where verb.key == (byte)MouseAndKeyboardStat
                          select verb).FirstOrDefault();
                if (vs != null)
                {
                    // Trigger verb as if the user clicked it
                    RunInputScript(ClickArea.Verb, (KeyCode)vs.verbid, 1);
                    return;
                }

                // Generic keyboard input
                RunInputScript(ClickArea.Key, MouseAndKeyboardStat, 1);
            }
            else if ((((ScummMouseButtonState)MouseAndKeyboardStat) & ScummMouseButtonState.MouseMask) != 0)
            {
                byte code = ((((ScummMouseButtonState)MouseAndKeyboardStat) & ScummMouseButtonState.LeftClick) != 0) ? (byte)1 : (byte)2;

                int mouseX = _variables[VariableMouseX];
                int mouseY = _variables[VariableMouseY];
                var over = FindVerbAtPos(mouseX, mouseY);

                if (over != 0)
                {
                    // Verb was clicked
                    RunInputScript(ClickArea.Verb, (KeyCode)_verbs[over].verbid, code);
                }
                else
                {
                    // Scene was clicked
                    var area = mouseY < roomData.Header.Height ? ClickArea.Scene : ClickArea.Verb;
                    RunInputScript(area, 0, code);
                }
            }

        }

        private void RunInputScript(ClickArea clickArea, KeyCode code, int mode)
        {
            var verbScript = _variables[VariableVerbScript];

            if (verbScript != 0)
            {
                RunScript((byte)verbScript, false, false, new int[] { (int)clickArea, (int)code, mode });
            }
        }

        private void UpdateObjectStates()
        {
            for (int i = 1; i < _numLocalObjects; i++)
            {
                if (Objects[i].obj_nr > 0)
                    Objects[i].state = GetState(Objects[i].obj_nr);
            }
        }

        private byte GetState(int obj)
        {
            ScummHelper.AssertRange(0, obj, _numGlobalObjects - 1, "object");
            return _scumm.ObjectStateTable[obj];
        }

        private void GetObjectOwner()
        {
            GetResult();
            SetResult(GetOwner(GetVarOrDirectWord(OpCodeParameter.Param1)));
        }

        public bool GetObjectOrActorXY(int obj, out int x, out int y)
        {
            Actor act;
            x = 0;
            y = 0;
            if (ObjIsActor(obj))
            {
                act = _actors[ObjToActor(obj)];
                if (act != null && act.IsInCurrentRoom())
                {
                    x = act.GetRealPos().X;
                    y = act.GetRealPos().Y;
                    return true;
                }
                else
                    return false;
            }

            switch (GetWhereIsObject(obj))
            {
                case WhereIsObject.NotFound:
                    return false;
                case WhereIsObject.Inventory:
                    if (ObjIsActor(_scumm.ObjectOwnerTable[obj]))
                    {
                        act = _actors[_scumm.ObjectOwnerTable[obj]];
                        if (act != null && act.IsInCurrentRoom())
                        {
                            x = act.GetRealPos().X;
                            y = act.GetRealPos().Y;
                            return true;
                        }
                    }
                    return false;
            }

            int dir;
            GetObjectXYPos(obj, out x, out y, out dir);
            return true;
        }

        private int ObjToActor(int obj)
        {
            return obj;
        }

        private bool ObjIsActor(int obj)
        {
            return obj < NumActors;
        }

        public void UpdateVariables()
        {
            _variables[VariableCameraPosX] = _camera._cur.X;
            _variables[VariableHaveMessage] = _haveMsg;
        }

        public void MoveCamera()
        {
            int pos = _camera._cur.X;
            int t;
            Actor a = null;
            bool snapToX = /*_snapScroll ||*/ _variables[VariableCameraFastX] != 0;

            _camera._cur.X = (short)(_camera._cur.X & 0xFFF8);

            if (_camera._cur.X < _variables[VariableCameraMinX])
            {
                if (snapToX)
                    _camera._cur.X = (short)_variables[VariableCameraMinX];
                else
                    _camera._cur.X += 8;

                CameraMoved();
                return;
            }

            if (_camera._cur.X > _variables[VariableCameraMaxX])
            {
                if (snapToX)
                    _camera._cur.X = (short)_variables[VariableCameraMaxX];
                else
                    _camera._cur.X -= 8;

                CameraMoved();
                return;
            }

            if (_camera._mode == CameraMode.FollowActor)
            {
                a = _actors[_camera._follows];

                int actorx = a.GetPos().X;
                t = actorx / 8 - _screenStartStrip;

                if (t < _camera._leftTrigger || t > _camera._rightTrigger)
                {
                    if (snapToX)
                    {
                        if (t > 40 - 5)
                            _camera._dest.X = (short)(actorx + 80);
                        if (t < 5)
                            _camera._dest.X = (short)(actorx - 80);
                    }
                    else
                        _camera._movingToActor = true;
                }
            }

            if (_camera._movingToActor)
            {
                a = _actors[_camera._follows];
                _camera._dest.X = a.GetPos().X;
            }

            if (_camera._dest.X < _variables[VariableCameraMinX])
                _camera._dest.X = (short)_variables[VariableCameraMinX];

            if (_camera._dest.X > _variables[VariableCameraMaxX])
                _camera._dest.X = (short)_variables[VariableCameraMaxX];

            if (snapToX)
            {
                _camera._cur.X = _camera._dest.X;
            }
            else
            {
                if (_camera._cur.X < _camera._dest.X)
                    _camera._cur.X += 8;
                if (_camera._cur.X > _camera._dest.X)
                    _camera._cur.X -= 8;
            }

            /* Actor 'a' is set a bit above */
            if (_camera._movingToActor && (_camera._cur.X / 8) == (a.GetPos().X / 8))
            {
                _camera._movingToActor = false;
            }

            CameraMoved();

            if (_variables[VariableScrollScript] != 0 && pos != _camera._cur.X)
            {
                _variables[VariableCameraPosX] = _camera._cur.X;
                RunScript((byte)_variables[VariableScrollScript], false, false, new int[0]);
            }
        }

        private void CameraMoved()
        {
            int screenLeft;

            if (_camera._cur.X < (_screenWidth / 2))
            {
                _camera._cur.X = (short)(_screenWidth / 2);
            }
            else if (_camera._cur.X > (CurrentRoomData.Header.Width - (_screenWidth / 2)))
            {
                _camera._cur.X = (short)(CurrentRoomData.Header.Width - (_screenWidth / 2));
            }

            _screenStartStrip = _camera._cur.X / 8 - _gdi._numStrips / 2;
            _screenEndStrip = _screenStartStrip + _gdi._numStrips - 1;

            _screenTop = _camera._cur.Y - (_screenHeight / 2);
            screenLeft = _screenStartStrip * 8;

            _mainVirtScreen.XStart = (ushort)screenLeft;
        }

        private byte[] GetObjectOrActorName(int num)
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
                           where o != null && o.obj_nr == num
                           select o).FirstOrDefault();

                if (obj == null)
                {
                    obj = (from o in Objects
                           where o.obj_nr == num
                           select o).FirstOrDefault();
                }
                if (obj != null && obj.Name != null)
                {
                    name = obj.Name;
                }
                else
                {
                    name = new byte[] { (byte)'F', (byte)'o', (byte)'o' };
                }
            }
            return name;
        }

        private static IList<char> ConvertMessage(IList<char> msg, int i, string text)
        {
            var src = msg.ToArray();
            var dst = new char[msg.Count - 3 + text.Length];
            Array.Copy(src, dst, i - 1);
            Array.Copy(text.ToArray(), 0, dst, i - 1, text.Length);
            Array.Copy(src, i + 2, dst, i - 1 + text.Length, src.Length - i - 3);
            msg = dst;
            return msg;
        }

        private void RestoreBackground(Rect rect, byte backColor)
        {
            VirtScreen vs;

            if (rect.top < 0)
                rect.top = 0;
            if (rect.left >= rect.right || rect.top >= rect.bottom)
                return;

            if ((vs = FindVirtScreen(rect.top)) == null)
                return;

            if (rect.left > vs.Width)
                return;

            // Convert 'rect' to local (virtual screen) coordinates
            rect.top -= vs.TopLine;
            rect.bottom -= vs.TopLine;

            rect.Clip(vs.Width, vs.Height);

            int height = rect.height();
            int width = rect.width();

            MarkRectAsDirty(vs, rect.left, rect.right, rect.top, rect.bottom, UsageBitRestored);

            PixelNavigator screenBuf = new PixelNavigator(vs.Surfaces[0]);
            screenBuf.GoTo(vs.XStart + rect.left, rect.top);

            if (height == 0)
                return;

            if (vs.HasTwoBuffers && _currentRoom != 0 && IsLightOn())
            {
                var back = new PixelNavigator(vs.Surfaces[1]);
                back.GoTo(vs.XStart + rect.left, rect.top);
                Blit(screenBuf, vs.Pitch, back, vs.Pitch, width, height, vs.BytesPerPixel);
                if (vs == MainVirtScreen && _charset._hasMask)
                {
                    var mask = new PixelNavigator(_textSurface);
                    mask.GoTo(rect.left, rect.top - _screenTop);
                    Fill(mask, _textSurface.Pitch, CharsetMaskTransparency, width * _textSurfaceMultiplier, height * _textSurfaceMultiplier);
                }
            }
            else
            {
                Fill(screenBuf, vs.Pitch, backColor, width, height);
            }
        }

        private void DrawString(int a, byte[] msg)
        {
            byte[] buf = new byte[270];
            //byte *space;
            int i, c;
            int fontHeight = 0;
            uint color;

            ConvertMessageToString(msg, buf, 0);

            _charset._top = _string[a].ypos + _screenTop;
            _charset._startLeft = _charset._left = _string[a].xpos;
            _charset._right = _string[a].right;
            _charset._center = _string[a].center;
            _charset.SetColor(_string[a].color);
            _charset._disableOffsX = _charset._firstChar = true;
            _charset.SetCurID(_string[a].charset);

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

            if (_charset._center)
            {
                _charset._left -= _charset.GetStringWidth(a, buf, 0) / 2;
            }

            if (buf[0] == 0)
            {
                _charset._str.left = _charset._left;
                _charset._str.top = _charset._top;
                _charset._str.right = _charset._left;
                _charset._str.bottom = _charset._top;
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
                            if (_charset._center)
                            {
                                _charset._left = _charset._startLeft - _charset.GetStringWidth(a, buf, i);
                            }
                            else
                            {
                                _charset._left = _charset._startLeft;
                            }
                            if (_string[0].height != 0)
                            {
                                _nextTop += _string[0].height;
                            }
                            else
                            {
                                _charset._top += fontHeight;
                            }
                            break;
                        case 12:
                            color = (uint)(buf[i] + (buf[i + 1] << 8));
                            i += 2;
                            if (color == 0xFF)
                                _charset.SetColor(_string[a].color);
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
                    _charset._blitAlso = false;
                }
            }

            if (a == 0)
            {
                _nextLeft = _charset._left;
                _nextTop = _charset._top;
            }

            _string[a].xpos = (short)_charset._str.right;
        }

        private int ConvertMessageToString(byte[] src, byte[] dst, int dstPos)
        {
            uint num = 0;
            int val;
            byte chr;
            byte lastChr = 0;
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
                    lastChr = chr;
                }
            }

            dst[dstPos] = 0;

            return dstPos - dstPosBegin;
        }

        private int ConvertNameMessage(byte[] dst, int dstPos, int var)
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

        private int ConvertVerbMessage(byte[] dst, int dstPos, int var)
        {
            var num = ReadVariable(var);
            if (num != 0)
            {
                for (int k = 1; k < _verbs.Length; k++)
                {
                    if (num == _verbs[k].verbid && _verbs[k].type == VerbType.Text && (_verbs[k].saveid == 0))
                    {
                        return ConvertMessageToString(_verbs[k].Text, dst, dstPos);
                    }
                }
            }
            return 0;
        }

        private int ConvertIntMessage(byte[] dst, int dstPos, int var)
        {
            var num = ReadVariable(var);
            var src = Encoding.ASCII.GetBytes(num.ToString());
            Array.Copy(src, 0, dst, dstPos, src.Length);
            return src.Length;
        }

        private int ConvertStringMessage(byte[] dst, int dstPos, int var)
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

        private void DrawVerbBitmap(int verb, double p, double p_2)
        {
            throw new NotImplementedException();
        }

        private bool IsScriptInUse(int script)
        {
            for (int i = 0; i < NumScriptSlot; i++)
                if (_slots[i].number == script)
                    return true;
            return false;
        }

        public void CheckAndRunSentenceScript()
        {
            int i;
            int sentenceScript;

            sentenceScript = _variables[VariableSentenceScript];

            if (IsScriptInUse(sentenceScript))
            {
                for (i = 0; i < NumScriptSlot; i++)
                    if (_slots[i].number == sentenceScript && _slots[i].status != ScriptStatus.Dead && _slots[i].freezeCount == 0)
                        return;
            }

            if (_sentenceNum == 0 || _sentence[_sentenceNum - 1].freezeCount != 0)
                return;

            _sentenceNum--;
            Sentence st = _sentence[_sentenceNum];

            if (st.preposition != 0 && st.objectB == st.objectA)
                return;

            int[] data = new int[3] { st.verb, st.objectA, st.objectB };

            _currentScript = 0xFF;
            if (sentenceScript != 0)
            {
                RunScript((byte)sentenceScript, false, false, data);
            }
        }

        public LightModes GetCurrentLights()
        {
            //if (_game.version >= 6)
            //    return LIGHTMODE_room_lights_on | LIGHTMODE_actor_use_colors;
            //else
            return (LightModes)_variables[VariableCurrentLights];
        }
        #endregion

        #region Cursor Members
        ushort[][] _cursorImages = new ushort[4][];
        byte[] _cursorHotspots = new byte[2 * 4];
        private static readonly ushort[][] default_cursor_images = new ushort[4][] {
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

        private static readonly byte[] default_cursor_hotspots = new byte[] {
            8, 7,   
            8, 7,   
            1, 1,  
            5, 0,
            8, 7, //zak256
        };

        private void AnimateCursor()
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

        private void SetBuiltinCursor(int idx)
        {
            var src = _cursorImages[_currentCursor];
            cursor_color = default_cursor_colors[idx];

            _cursor.HotspotX = _cursorHotspots[2 * _currentCursor] * _textSurfaceMultiplier;
            _cursor.HotspotY = _cursorHotspots[2 * _currentCursor + 1] * _textSurfaceMultiplier;
            _cursor.Width = 16 * _textSurfaceMultiplier;
            _cursor.Height = 16 * _textSurfaceMultiplier;

            byte[] pixels = new byte[_cursor.Width * _cursor.Height];

            int offset = 0;
            var color = (byte)(16 * cursor_color);
            for (int w = 0; w < 16; w++)
            {
                for (int h = 0; h < 16; h++)
                {
                    if ((src[w] & (1 << h)) != 0)
                    {
                        pixels[offset] = color;
                    }
                    offset++;
                }
            }

            _gfxManager.SetCursor(pixels, _cursor.Width, _cursor.Height, _cursor.HotspotX, _cursor.HotspotY);
        }

        private void ResetCursors()
        {
            for (int i = 0; i < 4; i++)
            {
                _cursorImages[i] = new ushort[16];
                Array.Copy(default_cursor_images[i], _cursorImages[i], 16);
            }
            Array.Copy(default_cursor_hotspots, _cursorHotspots, 8);
        }
        #endregion

        #region Scale Members
        public int GetBoxScale(byte boxNum)
        {
            var box = this.GetBoxBase(boxNum);
            if (box == null) return 255;
            else return box.scale;
        }

        public int GetScale(int boxNum, short x, short y)
        {
            var box = this.GetBoxBase(boxNum);
            if (box == null) return 255;

            int scale = (int)box.scale;
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
            int scaleX = 0, scaleY = 0;
            var s = _scaleSlots[slot - 1];

            if (s.y1 == s.y2 && s.x1 == s.x2)
                throw new NotSupportedException(string.Format("Invalid scale slot {0}", slot));

            if (s.y1 != s.y2)
            {
                if (y < 0)
                    y = 0;

                scaleY = (s.scale2 - s.scale1) * (y - s.y1) / (s.y2 - s.y1) + s.scale1;
            }
            if (s.x1 == s.x2)
            {
                scale = scaleY;
            }
            else
            {
                scaleX = (s.scale2 - s.scale1) * (x - s.x1) / (s.x2 - s.x1) + s.scale1;

                if (s.y1 == s.y2)
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

        private void SetScaleSlot(int slot, int x1, int y1, int scale1, int x2, int y2, int scale2)
        {
            if (slot < 1) throw new ArgumentOutOfRangeException("slot", slot, "Invalid scale slot");
            if (slot > _scaleSlots.Length) throw new ArgumentOutOfRangeException("slot", slot, "Invalid scale slot");
            _scaleSlots[slot - 1] = new ScaleSlot { x1 = x1, y1 = y1, y2 = y2, scale1 = scale1, scale2 = scale2 };
        }
        #endregion

        public bool IsLightOn()
        {
            return GetCurrentLights().HasFlag(LightModes.RoomLightsOn);
        }

        private void ProcessInput()
        {
            MouseAndKeyboardStat = 0;
            this.Dispatcher.Invoke(new Action(() =>
            {
                bool mainmenuKeyEnabled = _variables[VariableMainMenu] != 0;

                if (Keyboard.IsKeyDown(Key.Escape))
                {
                    MouseAndKeyboardStat = (KeyCode)Variables[ScummEngine.VariableCutSceneExitKey];
                    AbortCutscene();
                }

                if (mainmenuKeyEnabled && Keyboard.IsKeyDown(Key.F5))
                {
                    var eh = ShowMenuDialogRequested;
                    if (eh != null)
                    {
                        eh(this, EventArgs.Empty);
                    }
                }

                for (Key i = Key.A; i <= Key.Z; i++)
                {
                    if (Keyboard.IsKeyDown(i))
                    {
                        MouseAndKeyboardStat = (KeyCode)(i - Key.A + KeyCode.A);
                    }
                }

                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    MouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.LeftClick;
                }

                if (Mouse.RightButton == MouseButtonState.Pressed)
                {
                    MouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.RightClick;
                }

                var w = this._gfxManager.Width;
                var h = this._gfxManager.Height;
                var pos = this._gfxManager.GetMousePosition();
                if (pos.X > 0 && pos.Y > 0)
                {
                    var mouseX1 = (pos.X * 320.0) / w;
                    var mouseX = (ScreenStartStrip * 8) + mouseX1;
                    var mouseY = pos.Y * 200.0 / h;
                    Variables[ScummEngine.VariableMouseX] = (int)mouseX1;
                    Variables[ScummEngine.VariableMouseY] = (int)mouseY;
                    Variables[ScummEngine.VariableVirtualMouseX] = (int)mouseX;
                    Variables[ScummEngine.VariableVirtualMouseY] = (int)mouseY;
                }
            }));
        }

        public bool Go()
        {
            //SetTotalPlayTime();

            // If requested, load a save game instead of running the boot script
            //if (_saveLoadFlag != 2 || !loadState(_saveLoadSlot, _saveTemporaryState)) {
            //    _saveLoadFlag = 0;
            RunBootScript();
            //} else {
            //    _saveLoadFlag = 0;
            //}

            int diff = 0;	// Duration of one loop iteration

            while (!ShouldQuit())
            {

                //_debugger->onFrame();

                // Notify the script about how much time has passed, in ticks (60 ticks per second)
                _variables[VariableTimer] = diff * 60 / 1000;
                _variables[VariableTimerTotal] += diff * 60 / 1000;

                // Determine how long to wait before the next loop iteration should start
                int delta = _variables[VariableTimerNext];
                if (delta < 1)	// Ensure we don't get into an endless loop
                    delta = 1;  // by not decreasing sleepers.

                // Wait...
                WaitForTimer(delta * 1000 / 60 - diff);

                // Start the stop watch!
                var dt = DateTime.Now;

                // Run the main loop
                Loop(delta);

                // Halt the stop watch and compute how much time this iteration took.
                diff = (int)((DateTime.Now - dt).TotalMilliseconds);

                if (ShouldQuit())
                {
                    // TODO: Maybe perform an autosave on exit?
                }
            }

            return true;
        }

        private void WaitForTimer(int msec_delay)
        {
            var delay = TimeSpan.FromMilliseconds(msec_delay);
            DateTime start_time;

            start_time = DateTime.Now;

            while (!ShouldQuit())
            {
                //_sound->updateCD(); // Loop CD Audio if needed
                //ParseEvents();

                _gfxManager.UpdateScreen();
                if (DateTime.Now - start_time >= delay)
                {
                    break;
                }
                System.Threading.Thread.Sleep(10);
            }
        }

        public bool HastToQuit { get; set; }

        private bool ShouldQuit()
        {
            return HastToQuit;
        }

        private void Loop(int delta)
        {
            _variables[VariableTimer1] += delta;
            _variables[VariableTimer2] += delta;
            _variables[VariableTimer2] += delta;

            if (delta > 15)
                delta = 15;

            DecreaseScriptDelay(delta);

            _talkDelay -= delta;
            if (_talkDelay < 0)
            {
                _talkDelay = 0;
            }

            // Record the current ego actor before any scripts (including input scripts)
            // get a chance to run.
            int oldEgo = _variables[VariableEgo];

            ProcessInput();

            UpdateVariables();

            SaveLoad();

            if (_completeScreenRedraw)
            {
                _charset._hasMask = false;

                for (int i = 0; i < _verbs.Length; i++)
                {
                    DrawVerb(i, 0);
                }

                HandleMouseOver(false);

                _completeScreenRedraw = false;
                _fullRedraw = true;
            }

            RunAllScripts();
            CheckExecVerbs();
            CheckAndRunSentenceScript();

            if (ShouldQuit())
                return;

            // HACK: If a load was requested, immediately perform it. This avoids
            // drawing the current room right after the load is request but before
            // it is performed. That was annoying esp. if you loaded while a SMUSH
            // cutscene was playing.
            //if (_saveLoadFlag && _saveLoadFlag != 1) {
            //    goto load_game;
            //}

            if (_currentRoom == 0)
            {
                CHARSET_1();
                DrawDirtyScreenParts();
            }
            else
            {
                WalkActors();
                MoveCamera();
                UpdateObjectStates();
                CHARSET_1();

                HandleDrawing();

                HandleActors();

                _fullRedraw = false;

                HandleEffects();

                //if (VAR_MAIN_SCRIPT != 0xFF && VAR(VAR_MAIN_SCRIPT) != 0) {
                //    runScript(VAR(VAR_MAIN_SCRIPT), 0, 0, 0);
                //}

                // Handle mouse over effects (for verbs).
                HandleMouseOver(oldEgo != _variables[VariableEgo]);

                // Render everything to the screen.
                UpdatePalette();
                DrawDirtyScreenParts();

                // FIXME / TODO: Try to move the following to scummLoop_handleSound or
                // scummLoop_handleActors (but watch out for regressions!)
                //PlayActorSounds();
            }

            //HandleSound();

            Camera._last = Camera._cur;

            //_res->increaseExpireCounter();

            AnimateCursor();

            /* show or hide mouse */
            //CursorMan.showMouse(_cursor.state > 0);
        }

        #region Save & Load
        List<LoadAndSaveEntry> _loadAndSaveActions = new List<LoadAndSaveEntry>();

        private const uint InfosectionVersion = 2;
        private const uint SaveInfoSectionSize = (4 + 4 + 4 + 4 + 4 + 4 + 2);
        private bool _hasToLoad = false;
        private string _savegame;

        public void Load(string savegame)
        {
            _hasToLoad = true;
            _savegame = savegame;
        }

        private void SaveLoad()
        {
            // TODO: improve this
            if (_variables[VariableMainMenu] != 0 && _hasToLoad)
            {
                _hasToLoad = false;
                if (File.Exists(_savegame))
                {
                    LoadState(_savegame);
                }
            }
        }

        private bool LoadState(string path)
        {
            using (var file = File.OpenRead(path))
            {
                var reader = new BinaryReader(file);
                var hdr = LoadSaveGameHeader(reader);

                // Since version 56 we save additional information about the creation of
                // the save game and the save time.
                if (hdr.ver >= 56)
                {
                    SaveStateMetaInfos infos = LoadInfos(reader);
                    if (infos == null)
                    {
                        //warning("Info section could not be found");
                        //delete in;
                        return false;
                    }

                    //SetTotalPlayTime(infos.playtime * 1000);
                }
                else
                {
                    // start time counting
                    //setTotalPlayTime();
                }

                // Due to a bug in scummvm up to and including 0.3.0, save games could be saved
                // in the V8/V9 format but were tagged with a V7 mark. Ouch. So we just pretend V7 == V8 here
                if (hdr.ver == 7)
                    hdr.ver = 8;

                _saveLoadDescription = hdr.name;

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

                ushort roomWidth;
                ushort roomHeight;
                uint ENCD_offs;
                uint EXCD_offs;
                uint IM00_offs;
                uint CLUT_offs;
                uint EPAL_offs;
                uint PALS_offs;
                byte curPalIndex;
                byte numObjectsInRoom;
                uint[] localScriptOffsets;
                byte[][] charsetData;
                ushort curExecScript;
                short defaultTalkDelay;
                short numInMsgStack;
                ushort userState;
                byte gdiCursorActive;
                byte[] grabbedCursor;
                short mouseX, mouseY;
                byte[] colorUsedByCycle;
                byte palManipStart, palManipEnd;
                ushort palManipCounter = 0;
                byte[] currentPalette = null;
                byte[] darkenPalette = null;
                ushort[] gdiImgBufOffs;
                byte[] proc_special_palette;
                byte randSeed1;
                byte randSeed2;
                short shakeEnabled;
                uint shakeFrame;
                ushort screenB = 0;
                ushort screenH = 0;
                ushort NESCostumeSet;
                short cd_track, cd_loops, cd_frame, cd_end;

                #region MainEntries
                var mainEntries = new[]{
                    LoadAndSaveEntry.Create(()=> _gameMD5 = reader.ReadBytes(16),39),
                    LoadAndSaveEntry.Create(()=>roomWidth = reader.ReadUInt16(),8,50),
                    LoadAndSaveEntry.Create(()=>roomHeight = reader.ReadUInt16(),8,50),
                    LoadAndSaveEntry.Create(()=>ENCD_offs = reader.ReadUInt32(),8,50),
                    LoadAndSaveEntry.Create(()=>EXCD_offs = reader.ReadUInt32(),8,50),
                    LoadAndSaveEntry.Create(()=>IM00_offs = reader.ReadUInt32(),8,50),
                    LoadAndSaveEntry.Create(()=>CLUT_offs = reader.ReadUInt32(),8,50),
                    LoadAndSaveEntry.Create(()=>EPAL_offs = reader.ReadUInt32(),8,9),
                    LoadAndSaveEntry.Create(()=>PALS_offs = reader.ReadUInt32(),8,50),
                    LoadAndSaveEntry.Create(()=>curPalIndex = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=>_currentRoom = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=>_roomResource = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=>numObjectsInRoom = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=>_currentScript = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> localScriptOffsets = reader.ReadUInt32s(_numLocalScripts),8,50),
                    // vm.localvar grew from 25 to 40 script entries and then from
		            // 16 to 32 bit variables (but that wasn't reflect here)... and
		            // THEN from 16 to 25 variables.
                    LoadAndSaveEntry.Create(()=> _localVariables = reader.ReadMatrixUInt16(17,25),8,8),
                    LoadAndSaveEntry.Create(()=> _localVariables = reader.ReadMatrixUInt16(17,40),9,14),
                    // We used to save 25 * 40 = 1000 blocks; but actually, each 'row consisted of 26 entry,
		            // i.e. 26 * 40 = 1040. Thus the last 40 blocks of localvar where not saved at all. To be
		            // able to load this screwed format, we use a trick: We load 26 * 38 = 988 blocks.
		            // Then, we mark the followin 12 blocks (24 bytes) as obsolete.
                    LoadAndSaveEntry.Create(()=> _localVariables = reader.ReadMatrixUInt16(26,38),15,17),
                    // TODO
                    //MK_OBSOLETE_ARRAY(ScummEngine, vm.localvar[39][0], sleUint16, 12, VER(15), VER(17)),
                    // This was the first proper multi dimensional version of the localvars, with 32 bit values
                    LoadAndSaveEntry.Create(()=> _localVariables = reader.ReadMatrixInt32(26,40),18,19),

                    // Then we doubled the script slots again, from 40 to 80
		            LoadAndSaveEntry.Create(()=> _localVariables = reader.ReadMatrixInt32(26,NumScriptSlot),20),

		            LoadAndSaveEntry.Create(()=> _resourceMapper = reader.ReadBytes(128), 8),
		            LoadAndSaveEntry.Create(()=> _charsetColorMap = reader.ReadBytes(16), 8),

                    // _charsetData grew from 10*16, to 15*16, to 23*16 bytes
                    LoadAndSaveEntry.Create(()=> charsetData = reader.ReadMatrixBytes(10,16),8,9),
                    LoadAndSaveEntry.Create(()=> charsetData = reader.ReadMatrixBytes(15,16),10,66),
                    LoadAndSaveEntry.Create(()=> charsetData = reader.ReadMatrixBytes(23,16),67),

                    LoadAndSaveEntry.Create(()=> curExecScript = reader.ReadUInt16(), 8,62),

                    LoadAndSaveEntry.Create(()=> Camera._dest.X = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._dest.Y = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._cur.X = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._cur.Y = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._last.X = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._last.Y = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._accel.X = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._accel.Y = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> _screenStartStrip = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> _screenEndStrip = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._mode = (CameraMode)reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._follows = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._leftTrigger = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._rightTrigger = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> Camera._movingToActor = reader.ReadUInt16()!=0, 8),

                    LoadAndSaveEntry.Create(()=> _actorToPrintStrFor = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> _charsetColor = reader.ReadByte(), 8),

                    // _charsetBufPos was changed from byte to int
                    LoadAndSaveEntry.Create(()=> _charsetBufPos = reader.ReadByte(), 8,9),
                    LoadAndSaveEntry.Create(()=> _charsetBufPos = reader.ReadInt16(), 10),

                    LoadAndSaveEntry.Create(()=> _haveMsg = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> _haveActorSpeechMsg = reader.ReadByte()!=0, 61),
                    LoadAndSaveEntry.Create(()=> _useTalkAnims = reader.ReadByte()!=0, 8),

                    LoadAndSaveEntry.Create(()=> _talkDelay = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> defaultTalkDelay = reader.ReadInt16(), 8),
                    LoadAndSaveEntry.Create(()=> numInMsgStack = reader.ReadInt16(), 8,27),
                    LoadAndSaveEntry.Create(()=> _sentenceNum = reader.ReadByte(), 8),

                    LoadAndSaveEntry.Create(()=> cutSceneStackPointer = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> cutScenePtr = reader.ReadUInt32s(5), 8),
                    LoadAndSaveEntry.Create(()=> cutSceneScript = reader.ReadBytes(5), 8),
                    LoadAndSaveEntry.Create(()=> cutSceneData = Array.ConvertAll(reader.ReadInt16s(5), n => (int)n), 8),
                    LoadAndSaveEntry.Create(()=> _cutSceneScriptIndex = reader.ReadInt16(), 8),

                    LoadAndSaveEntry.Create(()=> _numNestedScripts = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> _userPut = (sbyte)reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> userState = reader.ReadUInt16(), 17),
                    LoadAndSaveEntry.Create(()=> _cursor.State = (sbyte)reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> gdiCursorActive = reader.ReadByte(), 8,20),
                    LoadAndSaveEntry.Create(()=> _currentCursor = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> grabbedCursor = reader.ReadBytes(8192), 20),
                    LoadAndSaveEntry.Create(()=> _cursor.Width = reader.ReadInt16(), 20),
                    LoadAndSaveEntry.Create(()=> _cursor.Height = reader.ReadInt16(), 20),
                    LoadAndSaveEntry.Create(()=> _cursor.HotspotX = reader.ReadInt16(), 20),
                    LoadAndSaveEntry.Create(()=> _cursor.HotspotY = reader.ReadInt16(), 20),
                    LoadAndSaveEntry.Create(()=> _cursor.Animate = reader.ReadByte()!=0, 20),
                    LoadAndSaveEntry.Create(()=> _cursor.AnimateIndex = reader.ReadByte(), 20),
                    LoadAndSaveEntry.Create(()=> mouseX = reader.ReadInt16(), 20),
                    LoadAndSaveEntry.Create(()=> mouseY = reader.ReadInt16(), 20),

                    LoadAndSaveEntry.Create(()=> colorUsedByCycle = reader.ReadBytes(256), 60),
                    LoadAndSaveEntry.Create(()=> _doEffect = reader.ReadByte()!=0, 8),
                    LoadAndSaveEntry.Create(()=> _switchRoomEffect = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> _newEffect = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> _switchRoomEffect2 = reader.ReadByte(), 8),
                    LoadAndSaveEntry.Create(()=> _bgNeedsRedraw = reader.ReadByte()!=0, 8),

                    // The state of palManipulate is stored only since V10
                    LoadAndSaveEntry.Create(()=> palManipStart = reader.ReadByte(), 10),
                    LoadAndSaveEntry.Create(()=> palManipEnd = reader.ReadByte(), 10),
                    LoadAndSaveEntry.Create(()=> palManipCounter = reader.ReadUInt16(), 10),

                    // gfxUsageBits grew from 200 to 410 entries. Then 3 * 410 entries:
                    LoadAndSaveEntry.Create(()=> _gfxUsageBits = reader.ReadUInt32s(200), 8,9),
                    LoadAndSaveEntry.Create(()=> _gfxUsageBits = reader.ReadUInt32s(410), 10,13),
                    LoadAndSaveEntry.Create(()=> _gfxUsageBits = reader.ReadUInt32s(3*410), 14),

                    LoadAndSaveEntry.Create(()=> _gdi.TransparentColor = reader.ReadByte(), 8,50),
                    LoadAndSaveEntry.Create(()=> currentPalette = reader.ReadBytes(768), 8),
                    LoadAndSaveEntry.Create(()=> darkenPalette = reader.ReadBytes(768), 53),

                    // Sam & Max specific palette replaced by _shadowPalette now.
                    LoadAndSaveEntry.Create(()=> proc_special_palette = reader.ReadBytes(256), 8,33),
                    
                    LoadAndSaveEntry.Create(()=> _charsetBuffer = reader.ReadBytes(256), 8),

                    LoadAndSaveEntry.Create(()=> _egoPositioned = reader.ReadByte()!=0, 8),

                    // _gdi->_imgBufOffs grew from 4 to 5 entries. Then one day we realized
                    // that we don't have to store it since initBGBuffers() recomputes it.
                    LoadAndSaveEntry.Create(()=> gdiImgBufOffs = reader.ReadUInt16s(4), 8,9),
                    LoadAndSaveEntry.Create(()=> gdiImgBufOffs = reader.ReadUInt16s(5), 10,26),

                    // See _imgBufOffs: _numZBuffer is recomputed by initBGBuffers().
                    LoadAndSaveEntry.Create(()=> _gdi._numZBuffer = reader.ReadByte(), 8,26),

                    LoadAndSaveEntry.Create(()=> _screenEffectFlag = reader.ReadByte()!=0, 8),

                    LoadAndSaveEntry.Create(()=> randSeed1 = reader.ReadByte(), 8,9),
                    LoadAndSaveEntry.Create(()=> randSeed2 = reader.ReadByte(), 8,9),

                    // Converted _shakeEnabled to boolean and added a _shakeFrame field.
                    LoadAndSaveEntry.Create(()=> shakeEnabled = reader.ReadInt16(), 8,9),
                    LoadAndSaveEntry.Create(()=> shakeEnabled = reader.ReadByte(), 10),
                    LoadAndSaveEntry.Create(()=> shakeFrame = reader.ReadUInt32(), 10),

                    LoadAndSaveEntry.Create(()=> _keepText = reader.ReadByte()!=0, 8),

                    LoadAndSaveEntry.Create(()=> screenB = reader.ReadUInt16(), 8),
                    LoadAndSaveEntry.Create(()=> screenH = reader.ReadUInt16(), 8),

                    LoadAndSaveEntry.Create(()=> NESCostumeSet = reader.ReadUInt16(), 47),

                    LoadAndSaveEntry.Create(()=> cd_track = reader.ReadInt16(), 9,9),
                    LoadAndSaveEntry.Create(()=> cd_loops = reader.ReadInt16(), 9,9),
                    LoadAndSaveEntry.Create(()=> cd_frame = reader.ReadInt16(), 9,9),
                    LoadAndSaveEntry.Create(()=> cd_end = reader.ReadInt16(), 9,9)
                };
                #endregion

                //var md5Backup = new byte[16];
                //Array.Copy(_gameMD5, md5Backup, 16);

                Array.ForEach(mainEntries, e => e.Execute(hdr.ver));
                roomData = _scumm.GetRoom(_currentRoom);

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
                if (hdr.ver >= 20)
                {
                    //UpdateCursor();
                    //_system->warpMouse(_mouse.x, _mouse.y);
                }

                // Before V61, we re-used the _haveMsg flag to handle "alternative" speech
                // sound files (see charset code 10).
                if (hdr.ver < 61)
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
                for (int i = 0; i < this.Actors.Length; i++)
                {
                    _actors[i].Load(reader, hdr.ver);
                }

                //
                // Save/load sound data
                //
                short currentCDSound;
                short currentMusic;
                var soundEntries = new[]{
                    LoadAndSaveEntry.Create(()=> currentCDSound = reader.ReadInt16(),35),
                    LoadAndSaveEntry.Create(()=> currentMusic = reader.ReadInt16(),35),
                };
                Array.ForEach(soundEntries, e => e.Execute(hdr.ver));

                //
                // Save/load script data
                //
                if (hdr.ver < 9)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        _slots[i].Load(reader, hdr.ver);
                    }
                }
                else if (hdr.ver < 20)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        _slots[i].Load(reader, hdr.ver);
                    }
                }
                else
                {
                    for (int i = 0; i < NumScriptSlot; i++)
                    {
                        _slots[i].Load(reader, hdr.ver);
                    }
                }
                Array.ForEach(_slots, slot =>
                {
                    if (slot.where == WhereIsObject.Global)
                    {
                        slot.offs -= (uint)_scumm.GetGlobalScriptOffset((byte)slot.number);
                    }
                    else if (slot.where == WhereIsObject.Local && slot.number >= 0xC8)
                    {
                        slot.offs = (uint)(slot.offs - roomData.LocalScripts[slot.number - 0xC8].Offset);
                    }
                });

                ResetRoomObjects();
                //
                // Save/load local objects
                //
                for (int i = 0; i < _numLocalObjects; i++)
                {
                    _objs[i].Load(reader, hdr.ver);
                }
                if (hdr.ver < 13)
                {
                    // Since roughly v13 of the save games, the objs storage has changed a bit
                    for (int i = _objs.Length; i < _numLocalObjects; i++)
                    {
                        _objs[i].obj_nr = 0;
                    }
                }

                //
                // Save/load misc stuff
                //
                for (int i = 0; i < _verbs.Length; i++)
                {
                    _verbs[i].Load(reader, hdr.ver);
                }
                for (int i = 0; i < 16; i++)
                {
                    _nest[i].Load(reader, hdr.ver);
                }
                for (int i = 0; i < 6; i++)
                {
                    _sentence[i].Load(reader, hdr.ver);
                }
                for (int i = 0; i < 6; i++)
                {
                    _string[i].Load(reader, hdr.ver);
                }
                for (int i = 0; i < 16; i++)
                {
                    _colorCycle[i].Load(reader, hdr.ver);
                }
                if (hdr.ver >= 13)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        _scaleSlots[i] = new ScaleSlot();
                        _scaleSlots[i].Load(reader, hdr.ver);
                    }
                }

                //
                // Save/load resources
                //
                ResType type;
                ushort idx;
                while ((type = (ResType)reader.ReadUInt16()) != (ResType)0xFFFF)
                {
                    while ((idx = reader.ReadUInt16()) != 0xFFFF)
                    {
                        LoadResource(reader, type, idx);
                    }
                }

                //
                // Save/load global object state
                //
                var objectOwnerTable = reader.ReadBytes(_numGlobalObjects);
                Array.Copy(objectOwnerTable, this._scumm.ObjectOwnerTable, _numGlobalObjects);
                var objectStateTable = reader.ReadBytes(_numGlobalObjects);
                Array.Copy(objectStateTable, this._scumm.ObjectStateTable, _numGlobalObjects);

                //if (_objectRoomTable)
                //    s->saveLoadArrayOf(_objectRoomTable, _numGlobalObjects, sizeof(_objectRoomTable[0]), sleByte);

                //
                // Save/load palette data
                // Don't save 16 bit palette in FM-Towns and PCE games, since it gets regenerated afterwards anyway.
                //if (_16BitPalette && !(_game.platform == Common::kPlatformFMTowns && s->getVersion() < VER(82)) && !((_game.platform == Common::kPlatformFMTowns || _game.platform == Common::kPlatformPCEngine) && s->getVersion() > VER(87))) {
                //    s->saveLoadArrayOf(_16BitPalette, 512, sizeof(_16BitPalette[0]), sleUint16);
                //}

                if (_shadowPaletteSize != 0)
                {
                    _shadowPalette = reader.ReadBytes(_shadowPaletteSize);
                    // _roomPalette didn't show up until V21 save games
                    // Note that we also save the room palette for Indy4 Amiga, since it
                    // is used as palette map there too, but we do so slightly a bit
                    // further down to group it with the other special palettes needed.
                    if (hdr.ver >= 21)
                    {
                        _roomPalette = reader.ReadBytes(256);
                    }
                }

                // PalManip data was not saved before V10 save games
                if (hdr.ver < 10)
                {
                    palManipCounter = 0;
                }
                if (palManipCounter != 0)
                {
                    _palManipPalette = reader.ReadBytes(0x300);
                    _palManipIntermediatePal = reader.ReadBytes(0x600);
                }

                // darkenPalette was not saved before V53
                if (hdr.ver < 53)
                {
                    Array.Copy(currentPalette, darkenPalette, 768);
                }

                // _colorUsedByCycle was not saved before V60
                if (hdr.ver < 60)
                {
                    //memset(_colorUsedByCycle, 0, sizeof(_colorUsedByCycle));
                    Array.Clear(_colorUsedByCycle, 0, _colorUsedByCycle.Length);
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
                Array.Copy(reader.ReadUInt32s(_numGlobalObjects), this._scumm.ClassData, _numGlobalObjects);

                //
                // Save/load script variables
                //
                //var120Backup = _scummVars[120];
                //var98Backup = _scummVars[98];

                if (hdr.ver > 37)
                {
                    //s->saveLoadArrayOf(_roomVars, _numRoomVariables, sizeof(_roomVars[0]), sleInt32);
                }

                // The variables grew from 16 to 32 bit.
                if (hdr.ver < 15)
                {
                    _variables = Array.ConvertAll(reader.ReadInt16s(_variables.Length), s => (int)s);
                }
                else
                {
                    _variables = Array.ConvertAll(reader.ReadInt32s(_variables.Length), s => (int)s);
                }

                //if (_game.id == GID_TENTACLE)	// Maybe misplaced, but that's the main idea
                //    _scummVars[120] = var120Backup;
                //if (_game.id == GID_INDY4)
                //    _scummVars[98] = var98Backup;

                _bitVars = reader.ReadBytes(_bitVars.Length);

                //
                // Save/load a list of the locked objects
                //
                while ((type = (ResType)reader.ReadByte()) != (ResType)0xFF)
                {
                    idx = reader.ReadUInt16();
                    //_res->lock(type, idx);
                }

                //
                // Save/load the Audio CD status
                //
                if (hdr.ver >= 24)
                {
                    //AudioCDManager::Status info;
                    //if (s->isSaving())
                    //    info = _system->getAudioCDManager()->getStatus();
                    //s->saveLoadArrayOf(&info, 1, sizeof(info), audioCDEntries);
                    // If we are loading, and the music being loaded was supposed to loop
                    // forever, then resume playing it. This helps a lot when the audio CD
                    // is used to provide ambient music (see bug #788195).
                    //if (s->isLoading() && info.playing && info.numLoops < 0)
                    //  _system->getAudioCDManager()->play(info.track, info.numLoops, info.start, info.duration);
                }

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

                var sb = screenB;
                var sh = screenH;
                Camera._last.X = Camera._cur.X;

                // Restore the virtual screens and force a fade to black.
                InitScreens(0, _screenHeight);

                Fill(MainVirtScreen.Surfaces[0].Pixels, MainVirtScreen.Pitch, 0, MainVirtScreen.Width, MainVirtScreen.Height);
                MainVirtScreen.SetDirtyRange(0, MainVirtScreen.Height);
                UpdateDirtyScreen(MainVirtScreen);
                //UpdatePalette();
                var colors = new System.Windows.Media.Color[256];
                for (int i = 0; i < 256; i++)
                {
                    colors[i] = System.Windows.Media.Color.FromRgb(currentPalette[i * 3], currentPalette[i * 3 + 1], currentPalette[i * 3 + 2]);
                }
                this._gfxManager.SetPalette(colors);
                InitScreens(sb, sh);

                _completeScreenRedraw = true;

                // Reset charset mask
                _charset._hasMask = false;
                ClearTextSurface();
                ClearDrawObjectQueue();
                _verbMouseOver = 0;

                CameraMoved();
            }

            return true;
        }

        int _shadowPaletteSize = 256;
        byte[] _shadowPalette;
        byte[] _palManipPalette, _palManipIntermediatePal;
        byte[] _colorUsedByCycle = new byte[256];

        private void LoadResource(BinaryReader reader, ResType type, ushort idx)
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
                            BinaryReader br = new BinaryReader(new MemoryStream(ptr));
                            br.BaseStream.Seek(18, SeekOrigin.Begin);
                            var offset = br.ReadByte();
                            br.BaseStream.Seek(offset, SeekOrigin.Begin);
                            List<byte> name = new List<byte>();
                            var c = br.ReadByte();
                            while (c != 0)
                            {
                                name.Add(c);
                                c = br.ReadByte();
                            }
                            _invData[idx] = new ObjectData() { obj_nr = index, Name = name.ToArray() };
                            br.BaseStream.Seek(19, SeekOrigin.Begin);
                            while (true)
                            {
                                var id = br.ReadByte();
                                var off = br.ReadUInt16();
                                if (id == 0)
                                    break;
                                _invData[idx].ScriptOffsets.Add(id, off);
                            }
                            foreach (var entry in _invData[idx].ScriptOffsets)
                            {
                                br.BaseStream.Seek(entry.Value, SeekOrigin.Begin);
                                _invData[idx].Scripts.Add(entry.Key, new ScriptData { Data = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position)) });
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
                            var obj = (from o in this.Objects.Concat(this._invData)
                                       where o != null && o.obj_nr == index
                                       select o).FirstOrDefault();
                            obj.Name = ptr;
                        }
                        break;
                    case ResType.Matrix:
                        {
                            if (idx == 1)
                            {
                                // BOXM
                                roomData.BoxMatrix.Clear();
                                roomData.BoxMatrix.AddRange(ptr);
                            }
                            else if (idx == 2)
                            {
                                // BOXD
                                BinaryReader br = new BinaryReader(new MemoryStream(ptr));

                                var numBoxes = br.ReadByte();
                                _boxes = new Box[numBoxes];
                                for (int i = 0; i < numBoxes; i++)
                                {
                                    Box box = new Box();
                                    box.ulx = br.ReadInt16();
                                    box.uly = br.ReadInt16();
                                    box.urx = br.ReadInt16();
                                    box.ury = br.ReadInt16();
                                    box.lrx = br.ReadInt16();
                                    box.lry = br.ReadInt16();
                                    box.llx = br.ReadInt16();
                                    box.lly = br.ReadInt16();
                                    box.mask = br.ReadByte();
                                    box.flags = (BoxFlags)br.ReadByte();
                                    box.scale = br.ReadUInt16();
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
                            Console.WriteLine("String: {0}", System.Text.Encoding.ASCII.GetString(ptr));
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

        private SaveStateMetaInfos LoadInfos(BinaryReader reader)
        {
            SaveStateMetaInfos stuff = new SaveStateMetaInfos();
            SaveInfoSection section = new SaveInfoSection();
            section.type = ScummHelper.SwapBytes(reader.ReadUInt32());
            if (section.type != ScummHelper.MakeTag('I', 'N', 'F', 'O'))
            {
                return null;
            }

            section.version = ScummHelper.SwapBytes(reader.ReadUInt32());
            section.size = ScummHelper.SwapBytes(reader.ReadUInt32());

            // If we ever extend this we should add a table containing the sizes corresponding to each
            // version, so that we are able to properly verify their correctness.
            if (section.version == InfosectionVersion && section.size != SaveInfoSectionSize)
            {
                //warning("Info section is corrupt");
                reader.BaseStream.Seek(section.size, SeekOrigin.Current);
                return null;
            }

            section.timeTValue = ScummHelper.SwapBytes(reader.ReadUInt32());
            section.playtime = ScummHelper.SwapBytes(reader.ReadUInt32());

            // For header version 1, we load the data in with our old method
            if (section.version == 1)
            {
                //time_t tmp = section.timeTValue;
                //tm *curTime = localtime(&tmp);
                //stuff->date = (curTime->tm_mday & 0xFF) << 24 | ((curTime->tm_mon + 1) & 0xFF) << 16 | (curTime->tm_year + 1900) & 0xFFFF;
                //stuff->time = (curTime->tm_hour & 0xFF) << 8 | (curTime->tm_min) & 0xFF;
                stuff.date = 0;
                stuff.time = 0;
            }

            if (section.version >= 2)
            {
                section.date = ScummHelper.SwapBytes(reader.ReadUInt32());
                section.time = ScummHelper.SwapBytes(reader.ReadUInt16());

                stuff.date = section.date;
                stuff.time = section.time;
            }

            stuff.playtime = section.playtime;

            // Skip over the remaining (unsupported) data
            if (section.size > SaveInfoSectionSize)
                reader.BaseStream.Seek(section.size - SaveInfoSectionSize, SeekOrigin.Current);

            return stuff;
        }

        private static SaveGameHeader LoadSaveGameHeader(BinaryReader reader)
        {
            var filename = ((FileStream)reader.BaseStream).Name;
            SaveGameHeader hdr = new SaveGameHeader();
            hdr.type = ScummHelper.SwapBytes(reader.ReadUInt32());
            if (hdr.type != ScummHelper.MakeTag('S', 'C', 'V', 'M')) throw new NotSupportedException(string.Format("Invalid savegame '{0}'", filename));
            hdr.size = reader.ReadUInt32();
            hdr.ver = reader.ReadUInt32();
            // In older versions of ScummVM, the header version was not endian safe.
            // We account for that by retrying once with swapped byte order in case
            // we see a version that is higher than anything we'd expect...
            if (hdr.ver > 0xFFFFFF)
                hdr.ver = ScummHelper.SwapBytes(hdr.ver);

            // Reject save games which are too old or too new. Note that
            // We do not really support V7 games, but still accept them here
            // to work around a bug from the stone age (see below for more
            // information).
            if (hdr.ver < 7 || hdr.ver > CurrentVersion)
            {
                throw new NotSupportedException(string.Format("Invalid version of '{0}'", filename));
            }

            hdr.name = Encoding.Default.GetString(reader.ReadBytes(32));

            // Since version 52 a thumbnail is saved directly after the header.
            if (hdr.ver >= 52)
            {
                // Prior to version 75 we always required an thumbnail to be present
                if (hdr.ver <= 74)
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

        private static bool SkipThumbnail(BinaryReader reader)
        {
            var position = reader.BaseStream.Position;
            ThumbnailHeader header = LoadHeader(reader, false);

            if (header == null)
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                return false;
            }

            reader.BaseStream.Seek(header.size - (reader.BaseStream.Position - position), SeekOrigin.Current);
            return true;
        }

        private static bool CheckThumbnailHeader(BinaryReader reader)
        {
            var position = reader.BaseStream.Position;
            ThumbnailHeader header = LoadHeader(reader, false);

            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return header != null;
        }

        private const byte TumbnailVersion = 1;

        private static ThumbnailHeader LoadHeader(BinaryReader reader, bool outputWarnings)
        {
            ThumbnailHeader header = new ThumbnailHeader();
            header.type = ScummHelper.SwapBytes(reader.ReadUInt32());
            // We also accept the bad 'BMHT' header here, for the sake of compatibility
            // with some older savegames which were written incorrectly due to a bug in
            // ScummVM which wrote the thumb header type incorrectly on LE systems.
            if (header.type != ScummHelper.MakeTag('T', 'H', 'M', 'B') && header.type != ScummHelper.MakeTag('B', 'M', 'H', 'T'))
            {
                //if (outputWarnings)
                //    warning("couldn't find thumbnail header type");
                return null;
            }

            header.size = ScummHelper.SwapBytes(reader.ReadUInt32());
            header.version = reader.ReadByte();

            if (header.version > TumbnailVersion)
            {
                //if (outputWarnings)
                //    warning("trying to load a newer thumbnail version: %d instead of %d", header.version, THMB_VERSION);
                return null;
            }

            header.width = ScummHelper.SwapBytes(reader.ReadUInt16());
            header.height = ScummHelper.SwapBytes(reader.ReadUInt16());
            header.bpp = reader.ReadByte();

            return header;
        }
        #endregion

        #region Actors
        private void PlayActorSounds()
        {
            throw new NotImplementedException();
        }

        private void HandleActors()
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

        private void ResetActorBgs()
        {
            int i, j;

            for (i = 0; i < _gdi._numStrips; i++)
            {
                int strip = _screenStartStrip + i;
                ClearGfxUsageBit(strip, UsageBitDirty);
                ClearGfxUsageBit(strip, UsageBitRestored);
                for (j = 1; j < Actors.Length; j++)
                {
                    if (TestGfxUsageBit(strip, j) &&
                        ((_actors[j]._top != 0x7fffffff && _actors[j].NeedRedraw) || _actors[j]._needBgReset))
                    {
                        ClearGfxUsageBit(strip, j);
                        if ((_actors[j]._bottom - _actors[j]._top) >= 0)
                            _gdi.ResetBackground(_actors[j]._top, _actors[j]._bottom, i);
                    }
                }
            }

            for (i = 1; i < Actors.Length; i++)
            {
                _actors[i]._needBgReset = false;
            }
        }

        private void SetActorRedrawFlags()
        {
            int i, j;

            // Redraw all actors if a full redraw was requested.
            // Also redraw all actors in COMI (see bug #1066329 for details).
            if (_fullRedraw)
            {
                for (j = 1; j < Actors.Length; j++)
                {
                    _actors[j].NeedRedraw = true;
                }
            }
            else
            {
                for (i = 0; i < _gdi._numStrips; i++)
                {
                    int strip = _screenStartStrip + i;
                    if (TestGfxAnyUsageBits(strip))
                    {
                        for (j = 1; j < Actors.Length; j++)
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
        #endregion

        #region Objects
        private void DrawRoomObjects(int argument)
        {
            const int mask = 0xF;
            for (int i = (_numLocalObjects - 1); i > 0; i--)
            {
                if (this.Objects[i].obj_nr > 0 && ((this.Objects[i].state & mask) != 0))
                {
                    DrawRoomObject(i, argument);
                }
            }
        }

        private void DrawRoomObject(int i, int argument)
        {
            ObjectData od;
            byte a;
            const int mask = 0xF;

            od = this.Objects[i];
            if ((i < 1) || (od.obj_nr < 1) || od.state == 0)
            {
                return;
            }
            do
            {
                a = od.parentstate;
                if (od.parent == 0)
                {
                    if (od.fl_object_index == 0)
                        DrawObject(i, argument);
                    break;
                }
                od = this.Objects[od.parent];
            } while ((od.state & mask) == a);
        }

        private void DrawObject(int obj, int arg)
        {
            ObjectData od = this.Objects[obj];
            int height, width;

            int x, a, numstrip;
            int tmp;

            if (_bgNeedsRedraw)
                arg = 0;

            if (od.obj_nr == 0)
                return;

            ScummHelper.AssertRange(0, od.obj_nr, _numGlobalObjects - 1, "object");

            int xpos = (int)(od.x_pos / 8);
            int ypos = (int)od.y_pos;

            width = od.width / 8;
            height = (ushort)(od.height &= 0xFFF8);	// Mask out last 3 bits

            // Short circuit for objects which aren't visible at all.
            if (width == 0 || xpos > _screenEndStrip || xpos + width < _screenStartStrip)
                return;

            var ptr = (from o in roomData.Objects
                       where o.obj_nr == od.obj_nr
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
                DrawBitmapFlags flags = od.flags | DrawBitmapFlags.ObjectMode;

                _gdi.DrawBitmap(ptr, this._mainVirtScreen, x, ypos, width * 8, height, x - xpos, numstrip, flags);
            }
        }

        private void ProcessDrawQueue()
        {
            foreach (var obj in this.DrawingObjects)
            {
                var index = this.Objects.IndexOf(obj);
                DrawObject(index, 0);
            }
            ClearDrawObjectQueue();
        }
        #endregion

        #region Charset
        private void CHARSET_1()
        {
            if (_haveMsg == 0)
                return;

            // Do nothing while the camera is moving
            if ((this.Camera._dest.X / 8) != (this.Camera._cur.X / 8) || this.Camera._cur.X != this.Camera._last.X)
                return;

            Actor a = null;
            if (GetTalkingActor() != 0xFF)
                a = this.Actors[GetTalkingActor()];

            if (a != null && _string[0].overhead)
            {
                int s;

                _string[0].xpos = (short)(a.GetPos().X - this.MainVirtScreen.XStart);
                _string[0].ypos = (short)(a.GetPos().Y - a.GetElevation() - _screenTop);

                if (_variables[VariableTalkStringY] < 0)
                {
                    s = (a._scaley * (int)_variables[VariableTalkStringY]) / 0xFF;
                    _string[0].ypos += (short)(((_variables[VariableTalkStringY] - s) / 2) + s);
                }
                else
                {
                    _string[0].ypos = (short)_variables[VariableTalkStringY];
                }


                if (_string[0].ypos < 1)
                    _string[0].ypos = 1;

                if (_string[0].xpos < 80)
                    _string[0].xpos = 80;
                if (_string[0].xpos > _screenWidth - 80)
                    _string[0].xpos = (short)(_screenWidth - 80);
            }

            _charset._top = _string[0].ypos + _screenTop;
            _charset._startLeft = _charset._left = _string[0].xpos;
            _charset._right = _string[0].right;
            _charset._center = _string[0].center;
            _charset.SetColor(_charsetColor);

            if (a != null && a._charset != 0)
                _charset.SetCurID(a._charset);
            else
                _charset.SetCurID(_string[0].charset);

            if (_talkDelay != 0)
                return;

            if (_haveMsg == 1)
            {
                // TODO:
                //if ((_sound->_sfxMode & 2) == 0)
                StopTalk();
                return;
            }

            if (a != null && !_string[0].no_talk_anim)
            {
                a.RunActorTalkScript(a._talkStartFrame);
                _useTalkAnims = true;
            }

            _talkDelay = 60;

            if (!_keepText)
            {
                RestoreCharsetBg();
            }


            int maxwidth = _charset._right - _string[0].xpos - 1;
            if (_charset._center)
            {
                if (maxwidth > _nextLeft)
                    maxwidth = _nextLeft;
                maxwidth *= 2;
            }

            _charset.AddLinebreaks(0, _charsetBuffer, _charsetBufPos, maxwidth);

            if (_charset._center)
            {
                _nextLeft -= _charset.GetStringWidth(0, _charsetBuffer, _charsetBufPos) / 2;
                if (_nextLeft < 0)
                    _nextLeft = 0;
            }

            _charset._disableOffsX = _charset._firstChar = !_keepText;

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

                _charset._left = _nextLeft;
                _charset._top = _nextTop;

                _charset.PrintChar(c, false);
                _nextLeft = _charset._left;
                _nextTop = _charset._top;

                _talkDelay += (int)_variables[VariableCharIncrement];
            }

        }

        private bool NewLine()
        {
            _nextLeft = _string[0].xpos;
            if (_charset._center)
            {
                _nextLeft -= _charset.GetStringWidth(0, _charsetBuffer, _charsetBufPos) / 2;
                if (_nextLeft < 0)
                    _nextLeft = 0;
            }

            if (_string[0].height != 0)
            {
                _nextTop += _string[0].height;
            }
            else
            {
                bool useCJK = _useCJKMode;
                _nextTop += _charset.GetFontHeight();
                _useCJKMode = useCJK;
            }

            // FIXME: is this really needed?
            _charset._disableOffsX = true;

            return true;
        }

        private bool HandleNextCharsetCode(Actor a, ref int code)
        {
            uint talk_sound_a = 0;
            uint talk_sound_b = 0;
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

                if (_newLineCharacter != 0 && c == _newLineCharacter)
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
                        talk_sound_a = (uint)(_charsetBuffer[bufferPos] | (_charsetBuffer[bufferPos + 1] << 8) | (_charsetBuffer[bufferPos + 4] << 16) | (_charsetBuffer[bufferPos + 5] << 24));
                        talk_sound_b = (uint)(_charsetBuffer[bufferPos + 8] | (_charsetBuffer[bufferPos + 9] << 8) | (_charsetBuffer[bufferPos + 12] << 16) | (_charsetBuffer[bufferPos + 13] << 24));
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

        private void RestoreCharsetBg()
        {
            _nextLeft = _string[0].xpos;
            _nextTop = _string[0].ypos + _screenTop;

            if (_charset._hasMask)
            {
                _charset._hasMask = false;
                _charset._str.left = -1;
                _charset._left = -1;

                // Restore background on the whole text area. This code is based on
                // restoreBackground(), but was changed to only restore those parts which are
                // currently covered by the charset mask.

                VirtScreen vs = _charset._textScreen;
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
                        Blit(screenBufNav, vs.Pitch, backNav, vs.Pitch, vs.Width, vs.Height, vs.BytesPerPixel);
                    }
                }
                else
                {
                    // Clear area
                    byte[] screenBuf = vs.Surfaces[0].Pixels;
                    Array.Clear(screenBuf, 0, screenBuf.Length);
                }

                if (vs.HasTwoBuffers)
                {
                    // Clean out the charset mask
                    ClearTextSurface();
                }
            }
        }
        #endregion

        private void UpdatePalette()
        {
            //throw new NotImplementedException();
        }

        private void HandleEffects()
        {
            // TODO:
            //CyclePalette();
            //PalManipulate();
            if (_doEffect)
            {
                _doEffect = false;
                FadeIn(_newEffect);
                //clearClickedStatus();
            }
        }

        private void HandleMouseOver(bool updateInventory)
        {
            if (_completeScreenRedraw)
            {
                VerbMouseOver(0);
            }
            else
            {
                if (_cursor.State > 0)
                {
                    var pos = _gfxManager.GetMousePosition();
                    var mouseX = (pos.X * 320.0) / _gfxManager.Width;
                    var mouseY = pos.Y * 200.0 / _gfxManager.Height;
                    VerbMouseOver(FindVerbAtPos((int)mouseX, (int)mouseY));
                }
            }
        }

        private void ClearTextSurface()
        {
            Fill(_textSurface.Pixels, _textSurface.Pitch, CharsetMaskTransparency, _textSurface.Width, _textSurface.Height);
        }

        private static void Fill(PixelNavigator dst, int dstPitch, byte color, int width, int height)
        {
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    dst.Write(color);
                    dst.OffsetX(1);
                }
                dst.Offset(-width, 1);
            }
        }

        private static void Fill(byte[] dst, int dstPitch, byte color, int w, int h)
        {

            if (w == dstPitch)
            {
                for (int i = 0; i < dst.Length; i++)
                {
                    dst[i] = color;
                }
            }
            else
            {
                int offset = 0;
                do
                {
                    for (int i = 0; i < w; i++)
                    {
                        dst[offset + i] = color;
                    }
                    offset += dstPitch;
                } while ((--h) != 0);
            }
        }

        private static void Blit(PixelNavigator dst, int dstPitch, PixelNavigator src, int srcPitch, int width, int height, int bitDepth)
        {
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    dst.Write(src.Read());
                    src.OffsetX(1);
                    dst.OffsetX(1);
                }
                src.Offset(-width, 1);
                dst.Offset(-width, 1);
            }
        }

        private void HandleDrawing()
        {
            if (_camera._cur != _camera._last || _bgNeedsRedraw || _fullRedraw)
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
        private void RedrawBGAreas()
        {
            // Starting with V4 games (with the exception of the PASS demo), text
            // is drawn over the game graphics (as  opposed to be drawn in a
            // separate region of the screen). So, when scrolling in one of these
            // games (pre-new camera system), if actor text is visible (as indicated
            // by the _hasMask flag), we first remove it before proceeding.
            if (_camera._cur.X != _camera._last.X && _charset._hasMask)
            {
                StopTalk();
            }

            // Redraw parts of the background which are marked as dirty.
            if (!_fullRedraw && _bgNeedsRedraw)
            {
                for (int i = 0; i != _gdi._numStrips; i++)
                {
                    if (TestGfxUsageBit(_screenStartStrip + i, UsageBitDirty))
                    {
                        RedrawBGStrip(i, 1);
                    }
                }
            }

            int val = 0;
            var diff = _camera._cur.X - _camera._last.X;
            if (!_fullRedraw && diff == 8)
            {
                val = -1;
                RedrawBGStrip(_gdi._numStrips - 1, 1);
            }
            else if (!_fullRedraw && diff == -8)
            {
                val = +1;
                RedrawBGStrip(0, 1);
            }
            else if (_fullRedraw || diff != 0)
            {
                // TODO:
                //ClearFlashlight();
                _bgNeedsRedraw = false;
                RedrawBGStrip(0, _gdi._numStrips);
            }
            DrawRoomObjects(val);
            _bgNeedsRedraw = false;
        }

        private void RedrawBGStrip(int start, int num)
        {
            int s = _screenStartStrip + start;

            for (int i = 0; i < num; i++)
                SetGfxUsageBit(s + i, UsageBitDirty);

            //var room = _scumm.GetRoom(_roomResource);
            _gdi.DrawBitmap(roomData.Data, _mainVirtScreen, s, 0, this.roomData.Header.Width, _mainVirtScreen.Height, s, num, 0);
        }

        private void DrawDirtyScreenParts()
        {
            // Update verbs
            UpdateDirtyScreen(_verbVirtScreen);

            // Update the conversation area (at the top of the screen)
            //UpdateDirtyScreen(_textVirtScreen);

            // Update game area ("stage")
            if (_camera._last.X != _camera._cur.X)
            {
                // Camera moved: redraw everything
                DrawStripToScreen(this._mainVirtScreen, 0, this._mainVirtScreen.Width, 0, this._mainVirtScreen.Height);
                this._mainVirtScreen.SetDirtyRange(this._mainVirtScreen.Height, 0);
            }
            else
            {
                UpdateDirtyScreen(this._mainVirtScreen);
            }

            // TODO: Handle shaking
            //if (_shakeEnabled)
            //{
            //    _shakeFrame = (_shakeFrame + 1) % NUM_SHAKE_POSITIONS;
            //    _system->setShakePos(shake_positions[_shakeFrame]);
            //}
            //else if (!_shakeEnabled && _shakeFrame != 0)
            //{
            //    _shakeFrame = 0;
            //    _system->setShakePos(0);
            //}
        }

        private void UpdateDirtyScreen(VirtScreen vs)
        {
            // Do nothing for unused virtual screens
            if (vs.Height == 0)
                return;

            int i;
            int w = 8;
            int start = 0;

            for (i = 0; i < _gdi._numStrips; i++)
            {
                if (vs.BDirty[i] != 0)
                {
                    int top = vs.TDirty[i];
                    int bottom = vs.BDirty[i];
                    vs.TDirty[i] = vs.Height;
                    vs.BDirty[i] = 0;
                    if (i != (_gdi._numStrips - 1) && vs.BDirty[i + 1] == bottom && vs.TDirty[i + 1] == top)
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
        private void DrawStripToScreen(VirtScreen vs, int x, int width, int top, int bottom)
        {
            // Short-circuit if nothing has to be drawn
            if (bottom <= top || top >= vs.Height)
                return;

            // Perform some clipping
            if (width > vs.Width - x)
                width = vs.Width - x;
            if (top < _screenTop)
                top = _screenTop;
            if (bottom > _screenTop + _screenHeight)
                bottom = _screenTop + _screenHeight;

            // Convert the vertical coordinates to real screen coords
            int y = vs.TopLine + top - _screenTop;
            int height = bottom - top;

            if (width <= 0 || height <= 0)
                return;

            var compNav = new PixelNavigator(_composite);
            if (vs == _verbVirtScreen)
            {
                compNav.GoTo(x, y);
            }
            else
            {
                compNav.GoTo(x, top);
            }
            var vsNav = new PixelNavigator(vs.Surfaces[0]);
            vsNav.GoTo(vs.XStart + x, top);
            var txtNav = new PixelNavigator(_textSurface);
            int m = _textSurfaceMultiplier;
            txtNav.GoTo(x * m, y * m);
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    var txtPixel = txtNav.Read();
                    byte pixel;
                    if (txtPixel != 0xFD)
                    {
                        pixel = txtPixel;
                    }
                    else
                    {
                        pixel = vsNav.Read();
                    }
                    compNav.Write(pixel);
                    compNav.OffsetX(1);
                    txtNav.OffsetX(1);
                    vsNav.OffsetX(1);
                }
                compNav.Offset(-width, 1);
                txtNav.Offset(-width, 1);
                vsNav.Offset(-width, 1);
            }

            var src = _composite.Pixels;

            // Finally blit the whole thing to the screen
            _gfxManager.CopyRectToScreen(src, vs.Pitch, x, y, width, height);
        }

        public void MarkRectAsDirty(VirtScreen vs, int left, int right, int top, int bottom, int dirtybit = 0)
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

            if ((lp >= _gdi._numStrips) || (rp < 0))
                return;
            if (lp < 0)
                lp = 0;
            if (rp >= _gdi._numStrips)
                rp = _gdi._numStrips - 1;

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
        private uint[] _gfxUsageBits;
        private bool _completeScreenRedraw;
        private IGraphicsManager _gfxManager;
        private int _palDirtyMin, _palDirtyMax;
        private int _nextLeft, _nextTop;
        private int _textSurfaceMultiplier = 1;
        private Surface _textSurface;

        private void SetGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3)) throw new ArgumentOutOfRangeException("strip");
            if (bit < 1 || bit > 96) throw new ArgumentOutOfRangeException("bit");
            bit--;
            _gfxUsageBits[3 * strip + bit / 32] |= (uint)((1 << bit % 32));
        }

        private void ClearGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3)) throw new ArgumentOutOfRangeException("strip");
            if (bit < 1 || bit > 96) throw new ArgumentOutOfRangeException("bit");
            bit--;
            _gfxUsageBits[3 * strip + bit / 32] &= (uint)~(1 << (bit % 32));
        }

        private bool TestGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3)) throw new ArgumentOutOfRangeException("strip");
            if (bit < 1 || bit > 96) throw new ArgumentOutOfRangeException("bit");
            bit--;
            return (_gfxUsageBits[3 * strip + bit / 32] & (1 << (bit % 32))) != 0;
        }

        private bool TestGfxOtherUsageBits(int strip, int bit)
        {
            // Don't exclude the DIRTY and RESTORED bits from the test
            uint[] bitmask = new uint[3] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
            int i;

            //assert(strip >= 0 && strip < ARRAYSIZE(gfxUsageBits) / 3);
            //assert(1 <= bit && bit <= 96);
            bit--;
            bitmask[bit / 32] &= (uint)(~(1 << (bit % 32)));

            for (i = 0; i < 3; i++)
                if ((_gfxUsageBits[3 * strip + i] & bitmask[i]) != 0)
                    return true;

            return false;
        }

        private bool TestGfxAnyUsageBits(int strip)
        {
            // Exclude the DIRTY and RESTORED bits from the test
            uint[] bitmask = new uint[3] { 0xFFFFFFFF, 0xFFFFFFFF, 0x3FFFFFFF };
            int i;

            //assert(strip >= 0 && strip < ARRAYSIZE(gfxUsageBits) / 3);
            for (i = 0; i < 3; i++)
                if ((_gfxUsageBits[3 * strip + i] & bitmask[i]) != 0)
                    return true;

            return false;
        }
        #endregion

        public PixelNavigator GetMaskBuffer(int x, int y, int z)
        {
            return _gdi.GetMaskBuffer((x + _mainVirtScreen.XStart) / 8, y, z);
        }

        public VirtScreen FindVirtScreen(int y)
        {
            if (VirtScreenContains(_mainVirtScreen, y)) return _mainVirtScreen;
            if (VirtScreenContains(_textVirtScreen, y)) return _textVirtScreen;
            if (VirtScreenContains(_verbVirtScreen, y)) return _verbVirtScreen;

            return null;
        }

        private bool VirtScreenContains(VirtScreen vs, int y)
        {
            return (y >= vs.TopLine && y < vs.TopLine + vs.Height);
        }

        #region Verb Members
        int _verbMouseOver;
        private string _saveLoadDescription;
        private byte[] _gameMD5;
        private int _numLocalScripts = 60;

        private void VerbMouseOver(int verb)
        {
            if (_verbMouseOver != verb)
            {
                if (_verbs[_verbMouseOver].type != VerbType.Image)
                {
                    DrawVerb(_verbMouseOver, 0);
                    _verbMouseOver = verb;
                }

                if (_verbs[verb].type != VerbType.Image && _verbs[verb].hicolor != 0)
                {
                    DrawVerb(verb, 1);
                    _verbMouseOver = verb;
                }
            }
        }

        private void GetVerbEntrypoint()
        {
            int a, b;
            GetResult();
            a = GetVarOrDirectWord(OpCodeParameter.Param1);
            b = GetVarOrDirectWord(OpCodeParameter.Param2);

            SetResult(GetVerbEntrypoint(a, b));
        }

        private int GetVerbEntrypoint(int obj, int entry)
        {
            if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                return 0;

            ObjectData result = null;

            if (this._scumm.ObjectOwnerTable[obj] != OF_OWNER_ROOM)
            {
                for (int i = 0; i < NumInventory; i++)
                {
                    if (_inventory[i] == obj)
                        result = _invData[i];
                }
            }
            else
            {
                result = (from o in this.Objects
                          where o.obj_nr == obj
                          select o).FirstOrDefault();
            }
            {
                foreach (var key in result.ScriptOffsets.Keys)
                {
                    if (key == entry || key == 0xFF)
                        return result.ScriptOffsets[key];
                }
            }
            return 0;
        }

        private VerbSlot GetVerb(int num)
        {
            var verbSlot = (from verb in _verbs
                            where num == verb.verbid && verb.type == 0 && verb.saveid == 0
                            select verb).FirstOrDefault();
            return verbSlot;
        }

        private void DrawVerb(int verb, int mode)
        {
            VerbSlot vs;
            bool tmp;

            if (verb == 0)
                return;

            vs = _verbs[verb];

            if (vs.saveid == 0 && vs.curmode != 0 && vs.verbid != 0)
            {
                if (vs.type == VerbType.Image)
                {
                    DrawVerbBitmap(verb, vs.curRect.left, vs.curRect.top);
                    return;
                }

                RestoreVerbBG(verb);

                _string[4].charset = vs.charset_nr;
                _string[4].xpos = (short)vs.curRect.left;
                _string[4].ypos = (short)vs.curRect.top;
                _string[4].right = (short)(_screenWidth - 1);
                _string[4].center = vs.center;

                if (vs.curmode == 2)
                    _string[4].color = vs.dimcolor;
                else if (mode != 0 && vs.hicolor != 0)
                    _string[4].color = vs.hicolor;
                else
                    _string[4].color = vs.color;

                // FIXME For the future: Indy3 and under inv scrolling
                /*
                   if (verb >= 31 && verb <= 36)
                   verb += _inventoryOffset;
                 */
                byte[] msg = _verbs[verb].Text;
                if (msg.Length == 0)
                    return;

                tmp = _charset._center;
                DrawString(4, msg);
                _charset._center = tmp;

                vs.curRect.right = _charset._str.right;
                vs.curRect.bottom = _charset._str.bottom;
                vs.oldRect = _charset._str;
                _charset._str.left = _charset._str.right;
            }
            else
            {
                RestoreVerbBG(verb);
            }
        }

        private void RestoreVerbBG(int verb)
        {
            VerbSlot vs = _verbs[verb];
            byte col = vs.bkcolor;

            if (vs.oldRect.left != -1)
            {
                RestoreBackground(vs.oldRect, col);
                vs.oldRect.left = -1;
            }
        }

        private void KillVerb(int slot)
        {
            if (slot == 0)
                return;

            VerbSlot vs = _verbs[slot];
            vs.verbid = 0;
            vs.curmode = 0;
            vs.Text = null;

            if (vs.saveid == 0)
            {
                DrawVerb(slot, 0);
                VerbMouseOver(0);
            }
            vs.saveid = 0;
        }

        private int FindVerbAtPos(int x, int y)
        {
            for (int i = _verbs.Length - 1; i >= 0; i--)
            {
                var vs = _verbs[i];
                if (vs.curmode != 1 || vs.verbid == 0 || vs.saveid != 0 || y < vs.curRect.top || y >= vs.curRect.bottom)
                    continue;
                if (vs.center)
                {
                    if (x < -(vs.curRect.right - 2 * vs.curRect.left) || x >= vs.curRect.right)
                        continue;
                }
                else
                {
                    if (x < vs.curRect.left || x >= vs.curRect.right)
                        continue;
                }

                return i;
            }

            return 0;
        }

        private int GetVerbSlot(int id, int mode)
        {
            int i;
            for (i = 1; i < _verbs.Length; i++)
            {
                if (_verbs[i].verbid == id && _verbs[i].saveid == mode)
                {
                    return i;
                }
            }
            return 0;
        }

        //public void RedrawVerbs()
        //{
        //    int i, verb = 0;
        //    int mouseX = _variables[VariableMouseX];
        //    int mouseY = _variables[VariableMouseY];

        //    if (_cursor.State > 0)
        //        verb = FindVerbAtPos(mouseX, mouseY);

        //    // Iterate over all verbs.
        //    // Note: This is the correct order (at least for MI EGA, MI2, Full Throttle).
        //    // Do not change it! If you discover, based on disasm, that some game uses
        //    // another (e.g. the reverse) order here, you have to use an if/else construct
        //    // to add it as a special case!
        //    for (i = 0; i < _verbs.Length; i++)
        //    {
        //        if (i == verb && _verbs[verb].hicolor != 0)
        //            DrawVerb(i, 1);
        //        else
        //            DrawVerb(i, 0);
        //    }

        //    _verbMouseOver = verb;
        //}
        #endregion
    }
}
