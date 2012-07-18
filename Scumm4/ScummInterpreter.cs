using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Scumm4;

namespace Scumm4
{
    public class Cursor
    {
        public byte State { get; set; }

        public bool animate { get; set; }

        public int animateIndex { get; set; }

        public Cursor()
        {
            animate = true;
        }
    }

    public class ScummInterpreter
    {
        #region Constants
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
        private const int VariableSoundcard = 0x30;
        private const int VariableVideoMode = 0x31;
        private const int VariableFixedDisk = 0x33;
        private const int VariableCursorState = 0x34;
        private const int VariableUserPut = 0x35;
        #endregion



        #region Fields
        private ScummIndex _scumm;
        private string _directory;
        private ResourceManager _res = new ResourceManager();
        private Actor[] _actors = new Actor[NumActors];
        private byte _currentRoom;
        private int _actorToPrintStrFor;
        private int _screenWidth = 320;
        private int _screenHeight = 200;
        private ushort[] _inventory = new ushort[NumInventory];

        private ScriptSlot[] slots = new ScriptSlot[NumScriptSlot];
        private int[][] _localVariables = new int[NumScriptSlot][];
        private byte[] _bitVars = new byte[4096 / 8];
        private Dictionary<byte, Action> _opCodes;
        private byte[] _currentScriptData;
        private int _currentPos;
        private int[] _variables;
        private HashSet<ObjectData> _drawingObjects = new HashSet<ObjectData>();



        private byte _userPut;
        private IList<char> _msg;
        private byte _roomResource;
        private bool _egoPositioned;

        private Cursor _cursor = new Cursor();
        private bool _userInput;
        private ushort _resultVarIndex;
        private byte _opCode;
        private Stack<int> _stack = new Stack<int>();
        private byte _currentScript;
        private int _numNestedScripts;
        private NestedScript[] _nest = new NestedScript[MaxScriptNesting];
        private uint[] cutScenePtr = new uint[MaxCutsceneNum];
        private byte[] cutSceneScript = new byte[MaxCutsceneNum];
        private int[] cutSceneData = new int[MaxCutsceneNum];
        private Room roomData;
        private int _sentenceNum;
        private int cutSceneStackPointer;
        private TextSlot[] _string = new TextSlot[6];

        private byte[] _resourceMapper = new byte[128];
        private char[][] _strings;
        private byte[][] _charsets;
        byte[] _charsetColorMap = new byte[16];

        private int _cutSceneScriptIndex;
        FlashLight _flashlight = new FlashLight();
        private Camera _camera = new Camera();
        private byte[] _pixels;

        private bool _keepText;
        private bool _useTalkAnims;
        private byte _charsetColor;
        private bool _haveActorSpeechMsg;
        private int _talkDelay;
        private int _haveMsg;
        private int _charsetBufPos;
        private int _screenStartStrip;
        private int _screenEndStrip;
        private int _screenTop;
        private VerbSlot[] _verbs = InitializeVerbs();
        private byte cursor_color;
        private int _currentCursor;

        static ushort[][] default_cursor_images = new ushort[4][] {
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

        static byte[] default_cursor_colors = new byte[] { 15, 15, 7, 8 };
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
        #endregion

        #region Constructor
        public ScummInterpreter(ScummIndex index, byte[] pixels)
        {
            _scumm = index;
            _pixels = pixels;
            _debugWriter = new StreamWriter(_debugFile);
            _directory = _scumm.Directory;
            _strings = new char[NumArray][];
            _charsets = new byte[NumArray][];
            _currentScript = 0xFF;
            for (int i = 0; i < 6; i++)
            {
                _string[i] = new TextSlot();
            }
            InitActors();
            InitOpCodes();
            InitVariables();
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
        }

        #endregion

        #region Execution
        public void Step()
        {
            var opCode = _currentScriptData[_currentPos++];
            // execute the code
            ExecuteOpCode(opCode);
        }

        FileStream _debugFile = File.OpenWrite(@"c:\temp\scumm2.txt");
        StreamWriter _debugWriter;
        private void ExecuteOpCode(byte opCode)
        {
            _opCode = opCode;
            slots[_currentScript].didexec = true;
            _debugWriter.WriteLine("{0:X2}", _opCode);
            _debugWriter.Flush();
            Console.WriteLine("OpCode: {0:X2}, Name = {1}", _opCode, _opCodes.ContainsKey(_opCode) ? _opCodes[opCode].Method.Name : "Unknown");
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
                ushort a = ReadWord();
                if ((a & 0x2000) != 0)
                {
                    _resultVarIndex += (ushort)ReadVariable(a & 0xDFFF);
                }
                else
                {
                    _resultVarIndex += (ushort)(a & 0xFFF);
                }
                _resultVarIndex &= 0xDFFF;
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

            if ((var & 0x4000) == 0x4000)
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
                    _bitVars[_resultVarIndex >> 3] |= (byte)(1 << (value & 7));
                else
                    _bitVars[_resultVarIndex >> 3] &= (byte)~(1 << (value & 7));
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
            _opCodes[0x72] = LoadRoom;
            _opCodes[0x73] = RoomOps;
            _opCodes[0x74] = GetDistance;
            /* 74 */
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
            x = a2.GetPos().x;
            y = a2.GetPos().y;
            if (x < a.GetPos().x)
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
            /* SCUMM7+ stuff */
            if ((val & 0xC000) != 0)
            {
                // TODO:
                throw new NotImplementedException("SetBoxFlags");
                //_extraBoxFlags[box] = val;
            }
            else
            {
                Box b = GetBoxBase(box);
                if (b == null)
                    return;
                b.flags = (BoxFlags)val;
            }
        }

        private void DelayVariable()
        {
            slots[_currentScript].delay = GetVar();
            slots[_currentScript].status = ScriptStatus.Paused;
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

            slots[cur].number = 0;
            slots[cur].status = ScriptStatus.Dead;
            _currentScript = 0xFF;

            RunScript((byte)script, slots[cur].freezeResistant, slots[cur].recursive, vars);
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
                    if (_camera._cur.x / 8 != _camera._dest.x / 8)
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

            for (i = 0; i < Objects.Count; i++)
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

                var ss = slots[_currentScript];
                if (ss.where == WhereIsObject.Inventory)
                {
                    if (ss.number < NumInventory && _inventory[ss.number] == obj)
                    {
                        throw new NotSupportedException("Odd setOwnerOf case #1: Please report to Fingolfin where you encountered this");
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

        public IList<ObjectData> Objects
        {
            get
            {
                if (this.roomData == null)
                {
                    return new ObjectData[] { };
                }
                return this.roomData.Objects;
            }
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
                for (i = 0; i < NumLocalObjects; i++)
                {
                    if (Objects[i].obj_nr == obj && Objects[i].fl_object_index != 0)
                    {
                        // Removing an flObject from a room means we can nuke it
                        //TODO: _res->nukeResource(rtFlObject, _objs[i].fl_object_index);
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
                        //TODO: _res->nukeResource(rtInventory, i);
                        _inventory[i] = 0;

                        // Now fill up the gap removing the object from the inventory created.
                        for (i = 0; i < NumInventory - 1; i++)
                        {
                            if (_inventory[i] == 0 && _inventory[i + 1] != 0)
                            {
                                _inventory[i] = _inventory[i + 1];
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
            _camera._cur.x = _camera._dest.x = a.GetPos().x;
            SetCameraFollows(a, false);

            // TODO ?
            //_fullRedraw = true;

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

            dir = OldDirToNewDir(od.actordir & 3);
        }

        private void GetObjectXYPos(int obj, out int x, out int y)
        {
            int dir;
            GetObjectXYPos(obj, out x, out y, out dir);
        }

        private static int OldDirToNewDir(int dir)
        {
            //assert(0 <= dir && dir <= 3);
            int[] new_dir_table = new int[4] { 270, 90, 180, 0 };
            return new_dir_table[dir];
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
                            // TODO
                            //verbMouseOver(0);
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
                            // TODO:
                            //verbMouseOver(0);
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

        private void KillVerb(int slot)
        {
            if (slot == 0)
                return;

            VerbSlot vs = _verbs[slot];
            vs.verbid = 0;
            vs.curmode = 0;

            //_res->nukeResource(rtVerb, slot);

            if (vs.saveid == 0)
            {
                DrawVerb(slot, 0);
                // TODO:
                //verbMouseOver(0);
            }
            vs.saveid = 0;
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
                _camera._cur.x = actor.GetPos().x;
                SetCameraAt(_camera._cur.x, 0);
            }

            var screenStartStrip = _camera._cur.x / 8 - 20;
            t = actor.GetPos().x / 8 - screenStartStrip;

            if (t < _camera._leftTrigger || t > _camera._rightTrigger || setCamera == true)
                SetCameraAt(actor.GetPos().x, 0);

            for (i = 1; i < _actors.Length; i++)
            {
                if (_actors[i].IsInCurrentRoom())
                    _actors[i]._needRedraw = true;
            }
            RunInventoryScript(0);
        }

        private void RunInventoryScript(int i)
        {
            if (_variables[VariableInventoryScript] != 0)
            {
                _debugWriter.WriteLine("RunInventoryScript {0:X2}", _variables[VariableInventoryScript]);
                RunScript((byte)_variables[VariableInventoryScript], false, false, new int[] { i });
            }
        }

        private void EndCutscene()
        {
            if (slots[_currentScript].cutsceneOverride > 0)	// Only terminate if active
                slots[_currentScript].cutsceneOverride--;

            var args = new int[] { cutSceneData[cutSceneStackPointer] };

            _variables[VariableOverride] = 0;

            if (cutScenePtr[cutSceneStackPointer] != 0 && (slots[_currentScript].cutsceneOverride > 0))	// Only terminate if active
                slots[_currentScript].cutsceneOverride--;

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
                        a.SetActorCostume((byte)GetVarOrDirectByte(OpCodeParameter.Param1));
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
                        //loadPtrToResource(rtActorName, a._number, NULL);
                        // TODO:
                        var name = ReadCharacters();
                        break;
                    case 14:		// SO_INIT_ANIMATION
                        a._initFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 16:		// SO_ACTOR_WIDTH
                        a._width = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 17:		// SO_ACTOR_SCALE
                        i = j = GetVarOrDirectByte(OpCodeParameter.Param1);
                        a._boxscale = (ushort)i;
                        a.SetScale((byte)i, (byte)j);
                        break;
                    case 18:		// SO_NEVER_ZCLIP
                        a._forceClip = 0;
                        break;
                    case 19:		// SO_ALWAYS_ZCLIP
                        a._forceClip = GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    case 20:		// SO_IGNORE_BOXES
                    case 21:		// SO_FOLLOW_BOXES
                        a._ignoreBoxes = !((_opCode & 1) != 0);
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
            ObjectClass cls2 = (ObjectClass)(cls & 0x7F);

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
                this.ClassData[obj] |= (uint)(1 << (cls - 1));
            else
                this.ClassData[obj] &= (uint)~(1 << (cls - 1));

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
            _camera._cur.x = at;
            SetCameraAt(at, 0);
            _camera._movingToActor = false;
        }

        private void SetCameraAt(short pos_x, short pos_y)
        {
            if (_camera._mode != CameraMode.FollowActor || Math.Abs(pos_x - _camera._cur.x) > (_screenWidth / 2))
            {
                _camera._cur.x = pos_x;
            }
            _camera._dest.x = pos_x;

            if (_camera._cur.x < _variables[VariableCameraMinX])
                _camera._cur.x = (short)_variables[VariableCameraMinX];

            if (_camera._cur.x > _variables[VariableCameraMaxX])
                _camera._cur.x = (short)_variables[VariableCameraMaxX];

            if (_variables[VariableScrollScript] != 0)
            {
                _variables[VariableCameraPosX] = _camera._cur.x;
                RunScript((byte)_variables[VariableScrollScript], false, false, new int[] { });
            }

            //// If the camera moved and text is visible, remove it
            //if (_camera._cur.x != _camera._last.x && _charset->_hasMask && _game.version > 3)
            //    stopTalk();
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
            //_fullRedraw = true;
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
                if (slots[i].Frozen)
                {
                    if (--slots[i].freezeCount == 0)
                    {
                        slots[i].Frozen = false;
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
                if (_currentScript != i && slots[i].status != ScriptStatus.Dead && (!slots[i].freezeResistant || flag >= 0x80))
                {
                    slots[i].Frozen = true;
                    slots[i].freezeCount++;
                }
            }

            for (i = 0; i < _sentence.Length; i++)
                _sentence[i].freezeCount++;

            if (_cutSceneScriptIndex != 0xFF)
            {
                slots[_cutSceneScriptIndex].Frozen = false;
                slots[_cutSceneScriptIndex].freezeCount = 0;
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
            slots[scr].cutsceneOverride++;

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
                var ss = slots[i];
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
                        // TODO: _roomPalette[b] = a;
                    }
                    break;
                case 3:
                    {
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        // TODO: initScreens(a, b);
                    }
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
                        // TODO:
                        //setScaleSlot(e - 1, 0, b, a, 0, d, c);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
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

            _debugWriter.WriteLine("RunObjectScript obj={0:X4}, entry={1:X4}", obj, entry);

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
                return;

            var count = (from o in roomData.Objects
                         where o.obj_nr == obj
                         select o.Scripts.Count).First();
            if (count == 0)
                return;

            //if (cycle == 0)
            //    cycle = (_game.heversion >= 90) ? VAR(VAR_SCRIPT_CYCLE) : 1;

            slots[slot].number = (ushort)obj;
            slots[slot].InventoryEntry = entry;
            slots[slot].offs = 0;
            slots[slot].status = ScriptStatus.Running;
            slots[slot].where = where;
            slots[slot].freezeResistant = freezeResistant;
            slots[slot].recursive = recursive;
            slots[slot].freezeCount = 0;
            slots[slot].delayFrameCount = 0;
            //slots[slot].cycle = cycle;

            InitializeLocals(slot, vars);

            // V0 Ensure we don't try and access objects via index inside the script
            //_v0ObjectIndex = false;
            UpdateScriptData(slot);
            RunScriptNested(slot);
        }

        private void StopObjectCode()
        {
            if (slots[_currentScript].where != WhereIsObject.Global && slots[_currentScript].where != WhereIsObject.Local)
            {
                StopObjectScript(slots[_currentScript].number);
            }
            else
            {
                _debugWriter.WriteLine("StopObjectCode {0:X4}", _currentScript);
                slots[_currentScript].number = 0;
                slots[_currentScript].status = ScriptStatus.Dead;
            }
            _currentScript = 0xFF;
        }

        private void StopObjectScript(ushort script)
        {
            int i;

            if (script == 0)
                return;

            _debugWriter.WriteLine("StopObjectScript {0:X4}", script);

            for (i = 0; i < NumScriptSlot; i++)
            {
                if (script == slots[i].number && slots[i].status != ScriptStatus.Dead &&
                    (slots[i].where == WhereIsObject.Room || slots[i].where == WhereIsObject.Inventory || slots[i].where == WhereIsObject.FLObject))
                {
                    slots[i].number = 0;
                    slots[i].status = ScriptStatus.Dead;
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
            slots[_currentScript].delay = (int)delay;
            slots[_currentScript].status = ScriptStatus.Paused;
            BreakHere();
        }

        private void StartScript()
        {
            var op = _opCode;
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var data = GetWordVarArgs();

            _debugWriter.WriteLine("StartScript({0:X2})", script);
            if (script != 0x98)
            {
                RunScript((byte)script, (op & 0x20) != 0, (op & 0x40) != 0, data);
            }
        }

        private void Move()
        {
            GetResult();
            var result = GetVarOrDirectWord(OpCodeParameter.Param1);
            _debugWriter.WriteLine("Move [{1:X4}]={0:X4}", result, _resultVarIndex);
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
            _debugWriter.WriteLine("IsEqual {0}({1:X4})=={2} ?", a, varNum, b);
            JumpRelative(a == b);
        }

        private void IsNotEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            _debugWriter.WriteLine("IsNotEqual {0}({1:X4})!={2} ?", a, varNum, b);
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
                    var switchRoomEffect = (byte)(a & 0xFF);
                    var switchRoomEffect2 = (byte)(a >> 8);
                }
                else
                {
                    // TODO:
                    //fadeIn(_newEffect);
                }
            }
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

            i = Objects.Count - 1;
            do
            {
                if (Objects[i].obj_nr != 0 && Objects[i].x_pos == x && Objects[i].y_pos == y && Objects[i].width == w && Objects[i].height == h)
                    PutState(Objects[i].obj_nr, 0);
            } while ((--i) != 0);

            PutState(obj, state);
        }

        private void PutState(int obj, byte state)
        {
            _scumm.ObjectStateTable[obj] = state;
            var foundObj = (from o in this.Objects
                            where o.obj_nr == obj
                            select o).FirstOrDefault();
            if (foundObj != null)
            {
                foundObj.state = state;
            }
        }

        private int GetObjectIndex(int obj)
        {
            int i;

            if (obj < 1)
                return -1;

            for (i = (Objects.Count - 1); i >= 0; i--)
            {
                if (Objects[i].obj_nr == obj)
                    return i;
            }
            return -1;
        }

        public enum OpCodeParameter : byte
        {
            Param1 = 0x80,
            Param2 = 0x40,
            Param3 = 0x20,
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
            _debugWriter.WriteLine("IsLess {0}({1:X4})<{2} ?", a, varNum, b);
            JumpRelative(b < a);
        }

        private void IsLessEqual()
        {
            var a = ReadVariable(ReadWord());
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
            var b = ReadVariable(ReadWord());
            SetResult(a * b);
        }

        private void Or()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(ReadWord());
            SetResult(a | b);
        }

        private void And()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(ReadWord());
            SetResult(a & b);
        }

        private void NotEqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);
            _debugWriter.WriteLine("NotEqualZero {0}({1:X4})!=0 ?", a, var);
            JumpRelative(a != 0);
        }

        private void EqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);
            _debugWriter.WriteLine("EqualZero {0}({1:X4})==0 ?", a, var);
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
                        List<char> sb = ReadCharacters();
                        _strings[id] = sb.ToArray();
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
                        _strings[id][index] = (char)character;
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
                        _strings[id] = new char[size];
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private List<char> ReadCharacters()
        {
            byte character;
            List<char> sb = new List<char>();
            character = ReadByte();
            while (character != 0)
            {
                sb.Add((char)character);
                if (character == 0xFF)
                {
                    character = ReadByte();
                    sb.Add((char)character);
                    if (character != 1 && character != 2 && character != 3 && character != 8)
                    {
                        character = ReadByte();
                        sb.Add((char)character);
                        character = ReadByte();
                        sb.Add((char)character);
                    }
                }
                character = ReadByte();
            }
            return sb;
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
                        _res.Sounds[resId].Lock = true;
                    }
                    break;
                case 10:
                    // TODO: lock Sound
                    break;
                case 11:		// SO_LOCK_COSTUME
                    _res.Costumes[resId].Lock = true;
                    break;
                case 13:		// SO_UNLOCK_SCRIPT
                    break;
                case 14:		// SO_UNLOCK_SOUND
                    break;
                case 15:		// SO_UNLOCK_COSTUME
                    // TODO:
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
                    // TODO: verbMouseOver(0);
                    break;
                case 2:
                    // Cursor Off
                    _cursor.State = 0;
                    // TODO: verbMouseOver(0);
                    break;
                case 3:
                    // User Input on
                    _userInput = true;
                    break;
                case 4:
                    // User Input off
                    _userInput = false;
                    break;
                case 5:
                    // SO_CURSOR_SOFT_ON
                    _cursor.State++;
                    // TODO:
                    //verbMouseOver(0);
                    break;
                case 6:
                    // SO_CURSOR_SOFT_OFF
                    _cursor.State--;
                    //TODO: verbMouseOver(0);
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

            if (_charsets[charsetNum] != null)
            {
                Array.Copy(_charsets[charsetNum], _charsetColorMap, 16);
            }
        }

        private void Expression()
        {
            _stack.Clear();
            GetResult();
            ushort dst = _resultVarIndex;
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

        private int GetVerbSlot(int id, int mode)
        {
            int i;
            for (i = 1; i < 100; i++)
            {
                if (_verbs[i].verbid == id && _verbs[i].saveid == mode)
                {
                    return i;
                }
            }
            return 0;
        }

        private void VerbOps()
        {
            var verb = (ushort)GetVarOrDirectByte(OpCodeParameter.Param1);
            var slot = GetVerbSlot(verb, 0);
            var vs = _verbs[slot];
            vs.verbid = verb;

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
                        List<char> sb = ReadCharacters();
                        _verbs[slot].Text = new string(sb.ToArray());
                        //loadPtrToResource(rtVerb, slot, NULL);
                        //if (slot == 0)
                        //    _res->nukeResource(rtVerb, slot);
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
                        vs.curRect = new System.Windows.Rect(left, top, vs.curRect.Width, vs.curRect.Height);
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
                            vs.verbid = verb;
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
            slots[_currentScript].offs = (uint)_currentPos;
            _currentScript = 0xFF;
        }

        private void SetState()
        {
            int obj;
            byte state;
            obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            state = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);
            PutState(obj, state);
            // TODO:
            //markObjectRectAsDirty(obj);
            //if (_bgNeedsRedraw)
            //    clearDrawObjectQueue();
        }

        private void PickupObject()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            if (GetObjectIndex(obj) == -1)
                return;

            if (GetWhereIsObject(obj) == WhereIsObject.Inventory)	// Don't take an object twice
                return;

            // debug(0, "adding %d from %d to inventoryOld", obj, _currentRoom);
            // TODO:
            AddObjectToInventory(obj, _roomResource);
            //markObjectRectAsDirty(obj);
            PutOwner(obj, (byte)_variables[VariableEgo]);
            PutClass(obj, (int)ObjectClass.Untouchable, true);
            PutState(obj, 1);
            // TODO:
            //clearDrawObjectQueue();
            RunInventoryScript(1);
        }

        private void AddObjectToInventory(int obj, byte room)
        {
            var slot = GetInventorySlot();
            if (GetWhereIsObject(obj) == WhereIsObject.FLObject)
            {
                var idx = GetObjectIndex(obj);
            }
            else
            {
                var objs = _scumm.GetRoom(room).Objects;
                var objFound = (from o in objs
                                where o.obj_nr == obj
                                select o).FirstOrDefault();
                // TODO:
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

        private void GetVerbEntrypoint()
        {
            int a, b;
            GetResult();
            a = GetVarOrDirectWord(OpCodeParameter.Param1);
            b = GetVarOrDirectWord(OpCodeParameter.Param2);

            SetResult(GetVerbEntrypoint(a, b));
        }

        ObjectData[] _invData = new ObjectData[NumInventory];

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
        #endregion

        #region Properties
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
        #endregion

        #region Misc Methods
        private void PanCameraTo(int x)
        {
            _camera._dest.x = (short)x;
            _camera._mode = CameraMode.Panning;
            _camera._movingToActor = false;
        }

        private int GetObjX(int obj)
        {
            if (obj < _actors.Length)
            {
                if (obj < 1)
                    return 0;									/* fix for indy4's map */
                return _actors[obj].GetRealPos().x;
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
            if (obj < _actors.Length)
            {
                if (obj < 1)
                    return 0;									/* fix for indy4's map */
                return _actors[obj].GetRealPos().y;
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

            if (obj >= _scumm.ObjectOwnerTable.Length)
                return WhereIsObject.NotFound;

            if (obj < 1)
                return WhereIsObject.NotFound;

            if ((_scumm.ObjectOwnerTable[obj] != OF_OWNER_ROOM))
            {
                for (i = 0; i < NumInventory; i++)
                    if (_inventory[i] == obj)
                        return WhereIsObject.Inventory;
                return WhereIsObject.NotFound;
            }

            for (i = (Objects.Count - 1); i >= 0; i--)
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
                if (slots[i].where == WhereIsObject.Room || slots[i].where == WhereIsObject.FLObject)
                {
                    if (slots[i].cutsceneOverride != 0)
                    {
                        slots[i].cutsceneOverride = 0;
                    }
                    //nukeArrays(i);
                    slots[i].status = ScriptStatus.Dead;
                }
                else if (slots[i].where == WhereIsObject.Local)
                {
                    if (slots[i].cutsceneOverride != 0)
                    {
                        slots[i].cutsceneOverride = 0;
                    }
                    //nukeArrays(i);
                    slots[i].status = ScriptStatus.Dead;
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
            if (_currentScript != 0xFF)
            {
                if (slots[_currentScript].where == WhereIsObject.Room || slots[_currentScript].where == WhereIsObject.FLObject)
                {
                    //if (slots[_currentScript].cutsceneOverride && _game.version >= 5)
                    //    error("Object %d stopped with active cutscene/override in exit", slots[_currentScript].number);

                    //nukeArrays(_currentScript);
                    _currentScript = 0xFF;
                }
                else if (slots[_currentScript].where == WhereIsObject.Local)
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

            _variables[VariableRoom] = room;
            _currentRoom = room;

            if (room >= 0x80)
                _roomResource = _resourceMapper[room & 0x7F];
            else
                _roomResource = room;

            _variables[VariableRoomResource] = _roomResource;

            roomData = _scumm.GetRoom((byte)room);
            this.DrawingObjects.Clear();

            if (room != 0)
            {
                _variables[VariableCameraMinX] = _screenWidth / 2;
                _variables[VariableCameraMaxX] = roomData.Header.Width - (_screenWidth / 2);

                _camera._mode = CameraMode.Normal;
                _camera._cur.x = _camera._dest.x = (short)(_screenWidth / 2);
                _camera._cur.y = _camera._dest.y = (short)(_screenHeight / 2);

                ShowActors();

                _egoPositioned = false;

                RunEntryScript();
            }
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
                            PrintText(textSlot, tmp);
                        }
                        return;
                    default:
                        throw new NotImplementedException();
                }
            }

            _string[textSlot].SaveDefault();
        }

        private void PrintText(int textSlot, IList<char> msg)
        {
            switch (textSlot)
            {
                case 0:
                    ActorTalk(msg);
                    break;
                //case 1:
                //    drawString(1, msg);
                //    break;
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

        private void ActorTalk(IList<char> msg)
        {
            Actor a;

            // TODO:
            //convertMessageToString(msg, _charsetBuffer, sizeof(_charsetBuffer));

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
            _talkDelay = 60;
            _haveMsg = 0xFF;
            _variables[VariableHaveMessage] = 0xFF;

            _haveActorSpeechMsg = true;

            _msg = msg;

            //update charset color
            var tmp = this.TextSlot[0].charset;
            this._scumm.GetCharset(tmp).ColorMap[1] = _charsetColor;
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
                slots[slot].status = ScriptStatus.Running;
                slots[slot].number = 10002;
                slots[slot].where = WhereIsObject.Room;
                slots[slot].offs = 0;
                slots[slot].freezeResistant = false;
                slots[slot].recursive = false;
                slots[slot].freezeCount = 0;
                slots[slot].delayFrameCount = 0;
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
                slots[slot].status = ScriptStatus.Running;
                slots[slot].number = 10001;
                slots[slot].where = WhereIsObject.Room;
                slots[slot].offs = 0;
                slots[slot].freezeResistant = false;
                slots[slot].recursive = false;
                slots[slot].freezeCount = 0;
                slots[slot].delayFrameCount = 0;
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

            _debugWriter.WriteLine("StopScript {0:X4}", script);

            if (script == 0)
                return;

            for (i = 0; i < NumScriptSlot; i++)
            {
                if (script == this.slots[i].number && this.slots[i].status != ScriptStatus.Dead &&
                    (this.slots[i].where == WhereIsObject.Global || this.slots[i].where == WhereIsObject.Local))
                {
                    this.slots[i].number = 0;
                    this.slots[i].status = ScriptStatus.Dead;
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
            slots[slotIndex].number = scriptNum;
            slots[slotIndex].offs = 0;
            slots[slotIndex].status = ScriptStatus.Running;
            slots[slotIndex].freezeResistant = freezeResistant;
            slots[slotIndex].recursive = recursive;
            slots[slotIndex].where = scriptType;
            slots[slotIndex].freezeCount = 0;
            slots[slotIndex].delayFrameCount = 0;

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
            var scriptNum = slots[slotIndex].number;
            if (roomData != null && (slots[slotIndex].where == WhereIsObject.Inventory))
            {
                var data = (from o in roomData.Objects
                            where o.obj_nr == scriptNum
                            select o.Scripts.ContainsKey((byte)slots[slotIndex].InventoryEntry) ?
                            o.Scripts[(byte)slots[slotIndex].InventoryEntry].Data : o.Scripts[0xFF].Data).FirstOrDefault();
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
                            let entry = (byte)slots[slotIndex].InventoryEntry
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
                slots[_currentScript].offs = (uint)_currentPos;
                _nest[_numNestedScripts].number = slots[_currentScript].number;
                _nest[_numNestedScripts].where = slots[_currentScript].where;
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
                var slot = slots[nest.slot];
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
            _currentPos = (int)slots[_currentScript].offs;
            _debugWriter.WriteLine("resetScriptPointer, #{0:X2}, script = {1:X}", slots[_currentScript].number, _currentScript/*, _currentPos*/);
        }

        private byte GetScriptSlotIndex()
        {
            for (byte i = 1; i < NumScriptSlot; i++)
            {
                if (slots[i].status == ScriptStatus.Dead)
                    return i;
            }
            return 0xFF;
        }

        public void RunAllScripts()
        {
            for (int i = 0; i < NumScriptSlot; i++)
                slots[i].didexec = false;

            _currentScript = 0xFF;

            for (int i = 0; i < NumScriptSlot; i++)
            {
                if (slots[i].status == ScriptStatus.Running && !slots[i].Frozen && !slots[i].didexec)
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

        private Box GetBoxBase(byte boxNum)
        {
            Box box = null;
            if (boxNum != 0xFF)
            {
                box = roomData.Boxes[boxNum];
            }
            return box;
        }

        public int GetNumBoxes()
        {
            return roomData.Boxes.Count;
        }

        public BoxCoords GetBoxCoordinates(int boxnum)
        {
            Box bp = GetBoxBase(boxnum);
            BoxCoords box = new BoxCoords();

            box.ul.x = bp.ulx;
            box.ul.y = bp.uly;
            box.ur.x = bp.urx;
            box.ur.y = bp.ury;

            box.ll.x = bp.llx;
            box.ll.y = bp.lly;
            box.lr.x = bp.lrx;
            box.lr.y = bp.lry;

            return box;
        }

        private Box GetBoxBase(int boxnum)
        {
            // As a workaround, we simply use the last box if the last+1 box is requested.
            // Note that this may cause different behavior than the original game
            // engine exhibited! To faithfully reproduce the behavior of the original
            // engine, we would have to know the data coming *after* the walkbox table.
            if (roomData.Boxes.Count == boxnum)
                boxnum--;

            return roomData.Boxes[boxnum];
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
            if (x < box.ul.x && x < box.ur.x && x < box.lr.x && x < box.ll.x)
                return false;

            if (x > box.ul.x && x > box.ur.x && x > box.lr.x && x > box.ll.x)
                return false;

            if (y < box.ul.y && y < box.ur.y && y < box.lr.y && y < box.ll.y)
                return false;

            if (y > box.ul.y && y > box.ur.y && y > box.lr.y && y > box.ll.y)
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
            return (p2.y - p1.y) * (p3.x - p1.x) <= (p3.y - p1.y) * (p2.x - p1.x);
        }

        private static Point ClosestPtOnLine(Point lineStart, Point lineEnd, Point p)
        {
            Point result;

            int lxdiff = lineEnd.x - lineStart.x;
            int lydiff = lineEnd.y - lineStart.y;

            if (lineEnd.x == lineStart.x)
            {	// Vertical line?
                result.x = lineStart.x;
                result.y = p.y;
            }
            else if (lineEnd.y == lineStart.y)
            {	// Horizontal line?
                result.x = p.x;
                result.y = lineStart.y;
            }
            else
            {
                int dist = lxdiff * lxdiff + lydiff * lydiff;
                int a, b, c;
                if (Math.Abs(lxdiff) > Math.Abs(lydiff))
                {
                    a = lineStart.x * lydiff / lxdiff;
                    b = p.x * lxdiff / lydiff;

                    c = (a + b - lineStart.y + p.y) * lydiff * lxdiff / dist;

                    result.x = (short)c;
                    result.y = (short)(c * lydiff / lxdiff - a + lineStart.y);
                }
                else
                {
                    a = lineStart.y * lxdiff / lydiff;
                    b = p.y * lydiff / lxdiff;

                    c = (a + b - lineStart.x + p.x) * lydiff * lxdiff / dist;

                    result.x = (short)(c * lxdiff / lydiff - a + lineStart.x);
                    result.y = (short)c;
                }
            }

            if (Math.Abs(lydiff) < Math.Abs(lxdiff))
            {
                if (lxdiff > 0)
                {
                    if (result.x < lineStart.x)
                        result = lineStart;
                    else if (result.x > lineEnd.x)
                        result = lineEnd;
                }
                else
                {
                    if (result.x > lineStart.x)
                        result = lineStart;
                    else if (result.x < lineEnd.x)
                        result = lineEnd;
                }
            }
            else
            {
                if (lydiff > 0)
                {
                    if (result.y < lineStart.y)
                        result = lineStart;
                    else if (result.y > lineEnd.y)
                        result = lineEnd;
                }
                else
                {
                    if (result.y > lineStart.y)
                        result = lineStart;
                    else if (result.y < lineEnd.y)
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
                if (slots[i].status == ScriptStatus.Paused)
                {
                    slots[i].delay -= amount;
                    if (slots[i].delay < 0)
                    {
                        slots[i].status = ScriptStatus.Running;
                        slots[i].delay = 0;
                    }
                }
            }
        }

        public void AbortCutscene()
        {
            _debugWriter.WriteLine("AbortCutscene");

            int idx = cutSceneStackPointer;

            var offs = cutScenePtr[idx];
            if (offs != 0)
            {
                slots[cutSceneScript[idx]].offs = offs;
                slots[cutSceneScript[idx]].status = ScriptStatus.Running;
                slots[cutSceneScript[idx]].freezeCount = 0;

                if (slots[cutSceneScript[idx]].cutsceneOverride > 0)
                    slots[cutSceneScript[idx]].cutsceneOverride--;

                _variables[VariableOverride] = 1;
                cutScenePtr[idx] = 0;
            }
        }

        public IList<char> GetMessage()
        {
            return _msg;
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
            //if (_userPut <= 0 || MouseAndKeyboardStat == 0)
            //    return;

            if (MouseAndKeyboardStat == 0)
                return;

            if ((ScummMouseButtonState)MouseAndKeyboardStat < ScummMouseButtonState.MBS_MAX_KEY)
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
            else if ((((ScummMouseButtonState)MouseAndKeyboardStat) & ScummMouseButtonState.MBS_MOUSE_MASK) != 0)
            {
                byte code = ((((ScummMouseButtonState)MouseAndKeyboardStat) & ScummMouseButtonState.MBS_LEFT_CLICK) != 0) ? (byte)1 : (byte)2;

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

        public void UpdateObjectStates()
        {
            int i;
            int obIndex = 0;
            ObjectData od = Objects.FirstOrDefault();
            for (i = 0; i < Objects.Count; i++, obIndex++)
            {
                if (Objects[obIndex].obj_nr > 0)
                    od.state = GetState(od.obj_nr);
            }
        }

        private byte GetState(int obj)
        {
            return _scumm.ObjectStateTable[obj];
        }

        private void GetObjectOwner()
        {
            GetResult();
            SetResult(GetOwner(GetVarOrDirectWord(OpCodeParameter.Param1)));
        }

        //private void SetObjectName()
        //{
        //    int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
        //    setObjectName(obj);
        //}

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
                    x = act.GetRealPos().x;
                    y = act.GetRealPos().y;
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
                            x = act.GetRealPos().x;
                            y = act.GetRealPos().y;
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
            _variables[VariableCameraPosX] = _camera._cur.x;
            _variables[VariableHaveMessage] = _haveMsg;
        }

        public void MoveCamera()
        {
            int pos = _camera._cur.x;
            int t;
            Actor a = null;
            bool snapToX = /*_snapScroll ||*/ _variables[VariableCameraFastX] != 0;

            _camera._cur.x = (short)(_camera._cur.x & 0xFFF8);

            if (_camera._cur.x < _variables[VariableCameraMinX])
            {
                if (snapToX)
                    _camera._cur.x = (short)_variables[VariableCameraMinX];
                else
                    _camera._cur.x += 8;

                CameraMoved();
                return;
            }

            if (_camera._cur.x > _variables[VariableCameraMaxX])
            {
                if (snapToX)
                    _camera._cur.x = (short)_variables[VariableCameraMaxX];
                else
                    _camera._cur.x -= 8;

                CameraMoved();
                return;
            }

            if (_camera._mode == CameraMode.FollowActor)
            {
                a = _actors[_camera._follows];

                int actorx = a.GetPos().x;
                t = actorx / 8 - /*_screenStartStrip 20?*/ 20;

                if (t < _camera._leftTrigger || t > _camera._rightTrigger)
                {
                    if (snapToX)
                    {
                        if (t > 40 - 5)
                            _camera._dest.x = (short)(actorx + 80);
                        if (t < 5)
                            _camera._dest.x = (short)(actorx - 80);
                    }
                    else
                        _camera._movingToActor = true;
                }
            }

            if (_camera._movingToActor)
            {
                a = _actors[_camera._follows];
                _camera._dest.x = a.GetPos().x;
            }

            if (_camera._dest.x < _variables[VariableCameraMinX])
                _camera._dest.x = (short)_variables[VariableCameraMinX];

            if (_camera._dest.x > _variables[VariableCameraMaxX])
                _camera._dest.x = (short)_variables[VariableCameraMaxX];

            if (snapToX)
            {
                _camera._cur.x = _camera._dest.x;
            }
            else
            {
                if (_camera._cur.x < _camera._dest.x)
                    _camera._cur.x += 8;
                if (_camera._cur.x > _camera._dest.x)
                    _camera._cur.x -= 8;
            }

            /* Actor 'a' is set a bit above */
            if (_camera._movingToActor && (_camera._cur.x / 8) == (a.GetPos().x / 8))
            {
                _camera._movingToActor = false;
            }

            CameraMoved();

            if (_variables[VariableScrollScript] != 0 && pos != _camera._cur.x)
            {
                _variables[VariableCameraPosX] = _camera._cur.x;
                RunScript((byte)_variables[VariableScrollScript], false, false, new int[0]);
            }
        }

        private void CameraMoved()
        {
            int screenLeft;

            if (_camera._cur.x < (_screenWidth / 2))
            {
                _camera._cur.x = (short)(_screenWidth / 2);
            }
            else if (_camera._cur.x > CurrentRoomData.Header.Width - (_screenWidth / 2))
            {
                _camera._cur.x = (short)(CurrentRoomData.Header.Width - (_screenWidth / 2));
            }


            _screenStartStrip = _camera._cur.x / 8 - 40 / 2;
            _screenEndStrip = _screenStartStrip + 40 - 1;

            _screenTop = _camera._cur.y - (_screenHeight / 2);
            screenLeft = _screenStartStrip * 8;
        }

        private static VerbSlot[] InitializeVerbs()
        {
            var verbs = new VerbSlot[100];
            for (int i = 0; i < 100; i++)
            {
                verbs[i] = new VerbSlot();
            }
            return verbs;
        }

        public void DrawCharset()
        {
            if (this.TalkDelay <= 0 && this.HaveMsg == 1)
            {
                this.StopTalk();
                return;
            }
            if (this.HaveMsg == 0) return;
            var msg = this.GetMessage();
            DrawCharset(0, msg);
        }

        private void DrawCharset(int index, IList<char> msg)
        {
            if (msg == null || this.CurrentRoomData == null) return;
            var palette = this.CurrentRoomData.Palette;
            var tmp = this.TextSlot[index].charset;
            var charset = this._scumm.GetCharset(tmp);
            int lx = index == 0 ? this.TextSlot[index].xpos - this.Camera._cur.x : this.TextSlot[index].xpos;
            int ly = this.TextSlot[index].ypos;
            for (int i = this.CharsetBufPos; i < msg.Count; i++)
            {
                if (msg[i] == 0xFF)
                {
                    i++;
                    if (i < msg.Count)
                    {
                        switch ((byte)msg[i])
                        {
                            case 1:
                                ly += charset.Height;
                                lx = 0;
                                break;
                            case 2:
                                if (this.TalkDelay <= 0)
                                {
                                    this.HaveMsg = 0;
                                    this.CharsetBufPos = i + 1;
                                }
                                return;
                            case 3:
                                if (this.TalkDelay <= 0)
                                {
                                    this.HaveMsg = 0xFF;
                                    this.TalkDelay = 60;
                                    this.CharsetBufPos = i + 1;
                                }
                                return;
                            case 5:
                                {
                                    ushort val = (ushort)(msg[i + 1] | msg[i + 2] << 16);
                                    var num = ReadVariable((int)val);
                                    if (num > 0)
                                    {
                                        var verbSlot = GetVerb(num);
                                        if (verbSlot != null)
                                        {
                                            msg = ConvertMessage(msg, i, verbSlot.Text);
                                        }
                                        else
                                        {
                                            msg = ConvertMessage(msg, i, string.Empty);
                                        }
                                    }
                                    else
                                    {
                                        msg = ConvertMessage(msg, i, string.Empty);
                                    }
                                    i = i - 2;
                                }
                                break;
                            case 6:
                                {
                                    ushort val = (ushort)(msg[i + 1] | msg[i + 2] << 16);
                                    var num = ReadVariable((int)val);
                                    if (num > 0)
                                    {
                                        var name = GetObjectOrActorName(num);
                                        msg = ConvertMessage(msg, i, name);
                                    }
                                    else
                                    {
                                        msg = ConvertMessage(msg, i, string.Empty);
                                    }
                                    i = i - 2;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                else if (msg[i] == 13)
                {
                    ly += charset.Height;
                    lx = 0;
                }
                else
                {
                    //this.TalkDelay += (int)this.Variables[ScummInterpreter.VariableCharIncrement];
                    if (charset.Characters.ContainsKey((byte)msg[i]) == false) continue;
                    var info = charset.Characters[(byte)msg[i]];
                    lx += info.X;
                    for (int y = 0; y < info.Height; y++)
                    {
                        if ((ly + info.Y + y) < 200)
                        {
                            for (int x = 0; x < info.Width; x++)
                            {
                                if ((lx + x) < 320)
                                {
                                    var colIndex = info.Pixels[x + y * info.Stride];
                                    if (colIndex != 0)
                                    {
                                        colIndex = charset.ColorMap[colIndex];
                                        var color = palette.Colors[colIndex];
                                        var offset = (lx + x) * 3 + (320 * 3 * (ly + info.Y + y));
                                        _pixels[offset++] = color.R;
                                        _pixels[offset++] = color.G;
                                        _pixels[offset] = color.B;
                                    }
                                }
                            }
                        }
                    }
                    lx += info.Width;
                }
            }

            if (this.TalkDelay <= 0)
            {
                this.HaveMsg = 1;
            }
        }

        private string GetObjectOrActorName(int num)
        {
            string name;
            if (num < _actors.Length)
            {
                throw new NotImplementedException();
            }
            else
            {
                var obj = (from o in Objects
                           where o.obj_nr == num
                           select o).FirstOrDefault();
                name = obj.Name;
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

            if (verb == 0)
                return;

            vs = _verbs[verb];

            if (vs.saveid == 0 && vs.curmode != 0 && vs.verbid != 0)
            {
                if (vs.type == VerbType.Image)
                {
                    DrawVerbBitmap(verb, vs.curRect.Left, vs.curRect.Top);
                    return;
                }

                //restoreVerbBG(verb);

                _string[4].charset = vs.charset_nr;
                _string[4].xpos = (short)vs.curRect.Left;
                _string[4].ypos = (short)vs.curRect.Top;
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

                var msg = vs.Text;
                if (string.IsNullOrEmpty(msg))
                    return;

                //tmp = _charset._center;
                DrawString(4, msg);

                // TODO ?
                //_charset._center = tmp;

                vs.curRect = new System.Windows.Rect(vs.curRect.Location, new System.Windows.Size(msg.Length * 8, 8));
                //vs.oldRect = _charset._str;
                //_charset._str.left = _charset._str.right;
            }
            else
            {
                //restoreVerbBG(verb);
            }
        }

        private void DrawString(int index, string msg)
        {
            this._scumm.GetCharset(_string[4].charset).ColorMap[1] = _string[4].color;
            DrawCharset(index, msg.ToArray());
        }

        private void DrawVerbBitmap(int verb, double p, double p_2)
        {
            throw new NotImplementedException();
        }

        private int FindVerbAtPos(int x, int y)
        {
            for (int i = _verbs.Length - 1; i > 0; i--)
            {
                var vs = _verbs[i];
                if (vs.curmode != 1 || vs.verbid == 0 || vs.saveid != 0 || y < vs.curRect.Top || y >= vs.curRect.Bottom)
                    continue;
                if (vs.center)
                {
                    if (x < -(vs.curRect.Right - 2 * vs.curRect.Left) || x >= vs.curRect.Right)
                        continue;
                }
                else
                {
                    if (x < vs.curRect.Left || x >= vs.curRect.Right)
                        continue;
                }

                return i;
            }

            return 0;
        }

        public void RedrawVerbs()
        {
            int i, verb = 0;
            int mouseX = _variables[VariableMouseX];
            int mouseY = _variables[VariableMouseY];

            if (_cursor.State > 0)
                verb = FindVerbAtPos(mouseX, mouseY);

            // Iterate over all verbs.
            // Note: This is the correct order (at least for MI EGA, MI2, Full Throttle).
            // Do not change it! If you discover, based on disasm, that some game uses
            // another (e.g. the reverse) order here, you have to use an if/else construct
            // to add it as a special case!
            for (i = 0; i < _verbs.Length; i++)
            {
                if (i == verb && _verbs[verb].hicolor != 0)
                    DrawVerb(i, 1);
                else
                    DrawVerb(i, 0);
            }

            //_verbMouseOver = verb;
        }

        private bool IsScriptInUse(int script)
        {
            for (int i = 0; i < NumScriptSlot; i++)
                if (slots[i].number == script)
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
                    if (slots[i].number == sentenceScript && slots[i].status != ScriptStatus.Dead && slots[i].freezeCount == 0)
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
                _debugWriter.WriteLine("RunSentenceScript: {0:X2}", sentenceScript);
                RunScript((byte)sentenceScript, false, false, data);
            }
        }

        #endregion

        #region Cursor Methods
        public void AnimateCursor()
        {
            if (_cursor.animate)
            {
                if ((_cursor.animateIndex & 0x1) == 0)
                {
                    SetBuiltinCursor((_cursor.animateIndex >> 1) & 3);
                }
                _cursor.animateIndex++;
            }
        }

        private void SetBuiltinCursor(int idx)
        {
            cursor_color = default_cursor_colors[idx];
        }

        public void DrawCursor()
        {
            if (_cursor.State <= 0) return;

            var x = Variables[ScummInterpreter.VariableMouseX];
            var y = Variables[ScummInterpreter.VariableMouseY];
            x = x - 8;
            y = y - 8;
            var data = default_cursor_images[_currentCursor];
            var color = (byte)(16 * cursor_color);
            for (int w = 0; w < 16; w++)
            {
                for (int h = 0; h < 16; h++)
                {
                    if (((x + w) >= 0 && (x + w) < 320) && ((y + h) >= 0 && (y + h) < 200))
                    {
                        if ((data[w] & (1 << h)) != 0)
                        {
                            var offset = (x + w) * 3 + (320 * 3 * (h + y));
                            _pixels[offset++] = color;
                            _pixels[offset++] = color;
                            _pixels[offset] = color;
                        }
                    }
                }

            }
        }
        #endregion

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

            var scale = (int)box.scale;
            //int slot = 0;
            //if ((scale & 0x8000) != 0)
            //    slot = (scale & 0x7FFF) + 1;

            // Was a scale slot specified? If so, we compute the effective scale
            // from it, ignoring the box scale.
            // TODO:
            //if (slot != 0)
            //    scale = GetScaleFromSlot(slot, x, y);

            return scale;
        }

        public class ScaleSlot
        {
            public int x1, y1, scale1;
            public int x2, y2, scale2;
        }

        private ScaleSlot[] _scaleSlots = new ScaleSlot[20];

        private int GetScaleFromSlot(int slot, short x, short y)
        {
            int scale;
            int scaleX = 0, scaleY = 0;
            var s = _scaleSlots[slot - 1];

            if (s.y1 == s.y2 && s.x1 == s.x2)
                throw new NotSupportedException(string.Format("Invalid scale slot %d", slot));

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
    }
}
