//
//  AGOSEngine.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Common;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    abstract partial class AGOSEngine : Engine
    {
        private const int SUBROUTINE_LINE_SMALL_SIZE = 2;
        private const int SUBROUTINE_LINE_BIG_SIZE = 8;

        public readonly AgosGameDescriptor _gd;

        protected GameSpecificSettings gss;
        protected int _currentBoxNum;

        protected TextLocation _textLocation1 = new TextLocation();
        protected TextLocation _textLocation2 = new TextLocation();
        protected TextLocation _textLocation3 = new TextLocation();
        protected TextLocation _textLocation4 = new TextLocation();

        protected ushort _numSaveGameRows;
        protected ushort _saveLoadRowCurPos;
        protected bool _saveLoadEdit;
        protected bool _saveOrLoad;
        private int _screenWidth;
        private int _screenHeight;
        protected Surface _backGroundBuf;
        private Surface _backBuf;
        private Surface _scaleBuf;
        protected Surface _window4BackScn;
        private Surface _window6BackScn;
        private bool _musicPaused;

        protected BytePtr _vcPtr; /* video code ptr */
        private readonly byte[] _vcGetOutOfCode = new byte[2];

        protected Ptr<uint> _gameOffsetsPtr;

        protected bool _saveDialogFlag;
        protected int _numVideoOpcodes;
        protected int _numTextBoxes;

        protected byte _numMusic;
        protected byte _numSFX;
        protected ushort _numSpeech;
        protected ushort _numZone;

        protected byte _numBitArray1;
        protected byte _numBitArray2;
        protected byte _numBitArray3;
        protected byte _numItemStore;
        protected ushort _numVars;

        protected byte _vgaBaseDelay;
        protected byte _vgaPeriod;

        protected ushort _musicIndexBase;
        protected ushort _soundIndexBase;
        protected ushort _tableIndexBase;
        protected ushort _textIndexBase;

        protected int _itemMemSize;
        protected int _tableMemSize;
        protected int _vgaMemSize;
        protected ushort _vgaWaitFor, _lastVgaWaitFor;

        protected BytePtr _tblList;
        protected BytePtr _tablesHeapPtr, _tablesHeapPtrOrg, _tablesheapPtrNew;
        protected int _tablesHeapSize, _tablesHeapCurPos, _tablesHeapCurPosOrg;
        protected int _tablesHeapCurPosNew;

        private readonly Array<object> _itemHeap = new Array<object>(() => null);

        protected BytePtr _mouseData;
        private bool _animatePointer;
        private byte _maxCursorWidth, _maxCursorHeight;
        private byte _mouseAnim, _mouseAnimMax;
        protected byte _mouseCursor;
        private byte _currentMouseAnim, _currentMouseCursor;
        private byte _oldMouseAnimMax, _oldMouseCursor;
        protected ushort _mouseHideCount;
        private bool _mouseToggle;

        protected bool _leftButtonDown;
        protected bool _rightButtonDown;
        protected byte _leftButton;
        protected byte _leftButtonCount;
        protected byte _leftButtonOld;
        private byte _oneClick;
        private bool _clickOnly;
        private bool _leftClick, _rightClick;
        private bool _noRightClick;
        protected short[] _variableArray;
        private Ptr<short> _variableArrayPtr;
        private short[] _variableArray2;

        protected readonly Action[] _vga_opcode_table = new Action[100];
        private BytePtr _block, _blockEnd;
        protected BytePtr _vgaMemPtr;
        private BytePtr _vgaMemEnd;
        protected BytePtr _vgaMemBase;
        protected BytePtr _vgaFrozenBase;
        private BytePtr _vgaRealBase;
        private BytePtr _zoneBuffers;

        protected ushort _stringIdLocalMin, _stringIdLocalMax;

        private ushort _noOverWrite;
        private bool _rejectBlock;

        protected ushort _soundFileId;
        private short _lastMusicPlayed;
        private short _nextMusicToPlay;
        protected bool _showPreposition;
        private bool _showMessageFlag;

        private byte _agosMenu;
        private byte[] _textMenu = new byte[10];
        protected ushort _currentRoom;
        protected ushort _superRoomNumber;
        private byte _wallOn;

        private BytePtr _planarBuf;
        private byte[] _videoBuf1 = new byte[32000];
        protected ushort[] _videoWindows = new ushort[128];

        private bool _copyProtection;
        protected Language _language;
        protected bool _speech;
        protected bool _subtitles;
        protected Item[] _itemArrayPtr;
        private int _itemArraySize;
        protected int _itemArrayInited;
        private BytePtr[] _stringTabPtr;
        private int _stringTabPos;
        private int _stringTabSize;
        private int _stringTabNum;
        private Item _currentPlayer;
        protected Item _dummyItem1 = new Item();
        protected Item _dummyItem2 = new Item();
        protected Item _dummyItem3 = new Item();
        private byte[] _strippedTxtMem;
        protected int _numRoomStates;
        protected RoomState[] _roomStates;
        private byte[] _roomsList;
        private byte[] _xtblList;
        private BytePtr _xtablesHeapPtrOrg;
        private int _xtablesHeapCurPosOrg;
        protected Subroutine _subroutineList;
        protected Subroutine _subroutineListOrg;
        protected Subroutine _xsubroutineListOrg;
        private uint _lastMinute; // Used in processSpecialKeys()
        private uint _lastTime;
        protected uint _clockStopped;
        protected uint _gameStoppedClock;
        private uint _timeStore;

        protected TimeEvent _firstTimeStruct;
        private TimeEvent _pendingDeleteTimeEvent;
        protected Stream _gameFile;
        protected readonly VgaPointersEntry[] _vgaBufferPointers = CreateArray<VgaPointersEntry>(450);
        private readonly VgaSprite[] _vgaSprites = CreateArray<VgaSprite>(200);
        protected short _scrollX;
        protected short _scrollXMax;
        protected short _scrollY;
        private short _scrollYMax;
        private bool _newDirtyClip;

        protected byte[] _iconFilePtr;
        protected Subroutine _currentTable;
        private BytePtr _codePtr;
        private SubroutineLine _currentLine;
        private SubroutineLine _classLine;
        private short _classMask, _classMode1, _classMode2;
        private byte _recursionDepth;

        private bool _runScriptReturn1;
        private readonly bool[] _runScriptCondition = new bool[40];
        private readonly short[] _runScriptReturn = new short[40];

        protected readonly ushort[] _bitArray = new ushort[128];
        protected readonly ushort[] _bitArrayTwo = new ushort[16];
        protected ushort[] _bitArrayThree = new ushort[16];
        private Item _findNextPtr;

        private short _scriptVerb, _scriptNoun1;
        protected short _scriptNoun2;
        private short _scriptAdj1;
        protected short _scriptAdj2;

        protected Item _subjectItem;
        protected Item _objectItem;
        private bool _mortalFlag;

        private WindowBlock _dummyWindow;
        protected readonly WindowBlock[] _windowArray = new WindowBlock[80];

        private readonly byte[] _fcsData1 = new byte[8];
        private readonly bool[] _fcsData2 = new bool[8];
        private WindowBlock _textWindow;
        private ushort _curWindow;

        private short _printCharCurPos, _printCharMaxPos, _printCharPixelCount;
        private ushort _numLettersToPrint;
        private ushort _displayFlag;
        private readonly byte[] _lettersToPrintBuf = new byte[80];

        protected byte[] _saveBuf = new byte[200];
        protected byte _saveGameNameLen;
        protected readonly byte[] _hebrewCharWidths = new byte[32];
        protected HitArea _lastHitArea;
        private Item _hitAreaSubjectItem;
        protected Item _hitAreaObjectItem;
        protected bool _nameLocked;
        protected ushort _needHitAreaRecalc;
        protected HitArea _lastHitArea3;
        private bool _dragAccept;
        protected ScummInputState _keyPressed;
        private bool _dragMode;
        protected HitArea _lastClickRem;
        private bool _dragFlag;
        private bool _dragEnd;
        private byte _dragCount;
        protected Point _mouse;
        protected Point _mouseOld;
        protected HitArea _currentBox;
        private HitArea _currentVerbBox, _lastVerbOn;
        protected ushort _defaultVerb;

        protected HitArea[] _hitAreas = CreateArray<HitArea>(250);
        private bool _exitCutscene, _picture8600;

        protected ushort _numOpcodes, _opcode;
        private int _freeStringSlot;
        private readonly byte[][] _stringReturnBuffer = CreateStringReturnBuffer();
        private int _textCount;
        private int _awaitTwoByteToken;
        private readonly byte[] _textBuffer = new byte[180];
        private BytePtr[] _localStringtable;
        private readonly WindowBlock[] _windowList = CreateArray<WindowBlock>(16);
        private ushort _hyperLink, _newLines;
        private ushort _oracleMaxScrollY, _noOracleScroll;
        private ushort _interactY;
        protected bool _noParentNotify;
        protected ushort _scrollUpHitArea;
        protected ushort _scrollDownHitArea;
        protected bool _restoreWindow6;
        protected HitArea _lastNameOn;
        protected bool _litBoxFlag;
        private bool _inCallBack;
        private DateTime _lastVgaTick;
        private ushort _syncCount;
        private bool _cepeFlag;
        protected RandomSource _rnd;
        private int _vgaTickCounter;
        private Ptr<VgaTimerEntry> _nextVgaTimerToProcess;
        protected BytePtr _curVgaFile1;
        private ushort _zoneNumber;
        protected bool _scriptVar2;
        protected bool _skipVgaWait;
        protected ushort _vgaCurSpriteId;
        protected ushort _vgaCurZoneNum;
        protected ushort _windowNum;
        protected bool _backFlag;
        protected ushort _scrollWidth, _scrollHeight;
        protected short _scrollCount, _scrollFlag;
        protected byte _paletteFlag;
        protected byte _window3Flag;
        protected byte _window4Flag;
        private byte _window6Flag;
        protected BytePtr _curVgaFile2;
        private BytePtr _curSfxFile;
        private int _curSfxFileSize;
        private readonly VgaSleepStruct[] _onStopTable = CreateArray<VgaSleepStruct>(60);
        private readonly VgaSleepStruct[] _waitEndTable = CreateArray<VgaSleepStruct>(60);
        protected readonly VgaSleepStruct[] _waitSyncTable = CreateArray<VgaSleepStruct>(60);
        private readonly AnimTable[] _screenAnim1 = CreateArray<AnimTable>(90);
        private volatile ushort _fastFadeInFlag;
        protected readonly Color[] _currentPalette = new Color[256];
        protected Color[] _displayPalette = new Color[256];
        private ushort _moveXMin, _moveYMin;
        private ushort _moveXMax, _moveYMax;
        private ushort _fastFadeCount;
        private bool _fastFadeOutFlag;
        protected Sound _sound;
        protected MidiPlayer _midi;
        private bool _midiEnabled;

        protected ushort _frameCount;
        protected volatile ushort _videoLockOut;
        protected bool _beardLoaded;
        private byte _boxStarHeight;
        protected bool _wiped;
        protected ushort _copyScnFlag, _vgaSpriteChanged;
        protected bool _bottomPalette;
        private bool _syncFlag2;
        protected readonly VgaTimerEntry[] _vgaTimerList = CreateArray<VgaTimerEntry>(205);
        private ushort _verbHitArea;
        private int _textSize;
        private byte[] _textMem;
        private BytePtr _twoByteTokens;
        private BytePtr _twoByteTokenStrings;
        private BytePtr _secondTwoByteTokenStrings;
        private BytePtr _thirdTwoByteTokenStrings;
        private BytePtr _byteTokens;
        private BytePtr _byteTokenStrings;
        protected bool _vgaVar9;
        protected BytePtr _roomsListPtr;
        private bool _effectsPaused;

        protected AGOSEngine(ISystem system, GameSettings settings, AGOSGameDescription gd)
            : base(system, settings)
        {
            _gd = (AgosGameDescriptor) Settings.Game;
            _rnd = new RandomSource("agos");
            DebugManager.Instance.AddDebugChannel(DebugLevels.kDebugOpcode, "opcode", "Opcode debug level");
            DebugManager.Instance.AddDebugChannel(DebugLevels.kDebugVGAOpcode, "vga_opcode", "VGA Opcode debug level");
            DebugManager.Instance.AddDebugChannel(DebugLevels.kDebugSubroutine, "subroutine", "Subroutine debug level");
            DebugManager.Instance.AddDebugChannel(DebugLevels.kDebugVGAScript, "vga_script", "VGA Script debug level");
            //Image dumping command disabled as it doesn't work well
#if Undefined
    DebugMan.addDebugChannel(kDebugImageDump, "image_dump", "Enable dumping of images to files");
#endif
        }

        public override void Run()
        {
            Init();
            Go();
        }

        protected virtual void SetupGame()
        {
            AllocItemHeap();
            AllocTablesHeap();

            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_SIMON2)
                InitMouse();

            _variableArray = new short[_numVars];
            _variableArrayPtr = _variableArray;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                _variableArray2 = new short[_numVars];
            }

            SetupOpcodes();
            SetupVgaOpcodes();

            SetZoneBuffers();

            _currentMouseCursor = 255;
            _currentMouseAnim = 255;

            _lastMusicPlayed = -1;
            _nextMusicToPlay = -1;

            _noOverWrite = 0xFFFF;

            _stringIdLocalMin = 1;

            _agosMenu = 1;
            _superRoomNumber = 1;

            for (int i = 0; i < 20; i++)
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    _videoWindows[i] = initialVideoWindows_Simon[i];
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                {
                    _videoWindows[i] = initialVideoWindows_PN[i];
                }
                else
                {
                    _videoWindows[i] = initialVideoWindows_Common[i];
                }
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 &&
                _gd.Platform == Platform.AtariST)
            {
                _videoWindows[9] = 75;
            }
        }

        protected virtual void SetupOpcodes()
        {
            Error("setupOpcodes: Unknown game");
        }

        protected virtual void Go()
        {
#if ENABLE_AGOS2
            LoadArchives();
#endif

            LoadGamePcFile();

            AddTimeEvent(0, 1);

            if (GetFileName(GameFileTypes.GAME_GMEFILE) != null)
            {
                OpenGameFile();
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
            {
                LoadIconData();
            }
            else if (GetFileName(GameFileTypes.GAME_ICONFILE) != null)
            {
                LoadIconFile();
            }

            if (GetFileName(GameFileTypes.GAME_MENUFILE) != null)
            {
                LoadMenuFile();
            }

            vc34_setMouseOff();

            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_PP &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_FF)
            {
                ushort count = (ushort) (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ? 5 : _frameCount);
                AddVgaEvent(count, EventType.ANIMATE_INT, BytePtr.Null, 0, 0);
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                _gd.Platform == Platform.AtariST &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
            {
                SetWindowImage(3, 9900);
                while (!HasToQuit)
                    Delay(0);
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                _gd.Platform == Platform.Amiga &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
            {
                PlayMusic(0, 0);
            }

            RunSubroutine101();
            PermitInput();

            while (!HasToQuit)
            {
                WaitForInput();
                HandleVerbClicked(_verbHitArea);
                Delay(100);
            }
        }

        protected void LocksScreen(Action<Surface> action)
        {
            var screen = OSystem.GraphicsManager.Capture();
            action(screen);
            OSystem.GraphicsManager.CopyRectToScreen(screen.Pixels, screen.Pitch, 0, 0, screen.Width,
                screen.Height);
        }

        protected void o_loadZone()
        {
            // 97: load zone
            uint vga_res = GetVarOrWord();

            _videoLockOut |= 0x80;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
            {
                vc27_resetSprite();
                vc29_stopAllSounds();
            }

            LoadZone((ushort) vga_res);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
            {
                _copyScnFlag = 0;
                _vgaSpriteChanged = 0;
            }

            _videoLockOut = (ushort) (_videoLockOut & ~0x80);
        }

        protected void o_invalid()
        {
            Error("Invalid opcode {0}", _opcode);
        }

        protected void o_at()
        {
            // 1: ptrA parent is
            SetScriptCondition(Me().parent == GetNextItemID());
        }

        protected void o_place()
        {
            // 33: set item parent
            Item item = GetNextItemPtr();
            SetItemParent(item, GetNextItemPtr());
        }

        protected void o_defWindow()
        {
            // 101: define window
            uint num = GetVarOrByte();
            uint x = GetVarOrWord();
            uint y = GetVarOrWord();
            uint w = GetVarOrWord();
            uint h = GetVarOrWord();
            uint flags = GetVarOrWord();
            uint color = GetVarOrWord();

            uint fillColor, textColor;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
            {
                fillColor = color % 100;
                textColor = color / 100;
            }
            else
            {
                fillColor = color;
                textColor = 0;
            }

            num &= 7;

            if (_windowArray[num] != null)
                CloseWindow(num);

            _windowArray[num] = OpenWindow(x, y, w, h, flags, fillColor, textColor);

            if (num == _curWindow)
            {
                _textWindow = _windowArray[num];
                JustifyStart();
            }
        }

        protected void o_window()
        {
            // 102
            ChangeWindow(GetVarOrByte() & 7);
        }

        protected void o_addBox()
        {
            // 107: add item box
            BoxFlags flags = 0;
            uint id = GetVarOrWord();
            uint @params = id / 1000;

            id = id % 1000;

            if ((@params & 1) != 0)
                flags |= BoxFlags.kBFInvertTouch;
            if ((@params & 2) != 0)
                flags |= BoxFlags.kBFNoTouchName;
            if ((@params & 4) != 0)
                flags |= BoxFlags.kBFBoxItem;
            if ((@params & 8) != 0)
                flags |= BoxFlags.kBFTextBox;
            if ((@params & 16) != 0)
                flags |= BoxFlags.kBFDragBox;

            uint x = GetVarOrWord();
            uint y = GetVarOrWord();
            uint w = GetVarOrWord();
            uint h = GetVarOrWord();
            var item = GetNextItemPtrStrange();
            uint verb = GetVarOrWord();
            if (x >= 1000)
            {
                verb += 0x4000;
                x -= 1000;
            }
            DefineBox((int) id, (int) x, (int) y, (int) w, (int) h, (int) flags, (int) verb, item);
        }

        protected void o_enableBox()
        {
            // 109: enable box
            EnableBox((int) GetVarOrWord());
        }

        protected void o_disableBox()
        {
            // 110: set hitarea bit 0x40
            DisableBox((int) GetVarOrWord());
        }

        protected void o_moveBox()
        {
            // 111: set hitarea xy
            int hitarea_id = (int) GetVarOrWord();
            int x = (int) GetVarOrWord();
            int y = (int) GetVarOrWord();
            MoveBox(hitarea_id, x, y);
        }

        protected void o_process()
        {
            // 71: start subroutine
            var id = (ushort) GetVarOrWord();

            if (!_copyProtection && _gd.ADGameDescription.gameType == SIMONGameType.GType_WW &&
                id == 71)
            {
                // Copy protection was disabled in Good Old Games release
                return;
            }

            var sub = GetSubroutineByID(id);
            if (sub != null)
            {
#if __DS__
// HACK: Skip scene of Simon reading letter from Calypso
// due to speech segment been too large to fit into memory
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                    _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE) &&
                    _gd.Platform == Platform.Windows && sub.id == 2922)
                {
                    // set parent special
                    _noParentNotify = true;
                    SetItemParent(DerefItem(16), Me());
                    _noParentNotify = false;

                    // set parent special
                    _noParentNotify = true;
                    SetItemParent(DerefItem(14), Me());
                    _noParentNotify = false;

                    // set item parent
                    SetItemParent(DerefItem(12), Me());

                    return;
                }
#endif
                StartSubroutine(sub);
            }
        }

        protected string GetFileName(GameFileTypes type)
        {
// Required if the InstallShield cab is been used
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                if (type == GameFileTypes.GAME_BASEFILE)
                    return gss.base_filename;
            }

// Required if the InstallShield cab is been used
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF &&
                _gd.Platform == Platform.Windows)
            {
                if (type == GameFileTypes.GAME_BASEFILE)
                    return gss.base_filename;
                if (type == GameFileTypes.GAME_RESTFILE)
                    return gss.restore_filename;
                if (type == GameFileTypes.GAME_TBLFILE)
                    return gss.tbl_filename;
            }

            for (int i = 0; i < _gd.ADGameDescription.filesDescriptions.Length; i++)
            {
                if (_gd.ADGameDescription.filesDescriptions[i].fileType == (ushort) type)
                    return _gd.ADGameDescription.filesDescriptions[i].fileName;
            }
            return null;
        }

        private void UnlockScreen(Surface screen)
        {
            OSystem.GraphicsManager.CopyRectToScreen(screen.Pixels, screen.Pitch, 0, 0, screen.Width,
                screen.Height);
        }

        public static T[] CreateArray<T>(int length) where T : new()
        {
            var objs = new T[length];
            for (var i = 0; i < objs.Length; i++)
            {
                objs[i] = new T();
            }
            return objs;
        }

        private static byte[][] CreateStringReturnBuffer()
        {
            var stringReturnBuffer = new byte[2][];
            for (int i = 0; i < 2; i++)
            {
                stringReturnBuffer[i] = new byte[180];
            }
            return stringReturnBuffer;
        }

        private void Init()
        {
            if (_gd.ADGameDescription.gameId == GameIds.GID_DIMP)
            {
                _screenWidth = 496;
                _screenHeight = 400;
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                _screenWidth = 640;
                _screenHeight = 480;
            }
            else
            {
                _screenWidth = 320;
                _screenHeight = 200;
            }

            //InitGraphics(_screenWidth, _screenHeight,
            //    _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
            //    _gd.ADGameDescription.gameType == SIMONGameType.GType_PP);

            _midi = new MidiPlayer();

            if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 && _gd.Platform == Platform.Windows) ||
                (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 && _gd.Platform == Platform.Windows) ||
                (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE) && _gd.Platform == Platform.Acorn) ||
                (_gd.Platform == Platform.DOS))
            {
                bool isDemo = _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO);

                var ret = _midi.Open(_gd.ADGameDescription.gameType, isDemo);
                if (ret != 0)
                    Warning("MIDI Player init failed: \"{0}\"", ret);

                _midi.SetVolume(ConfigManager.Instance.Get<int>("music_volume"),
                    ConfigManager.Instance.Get<int>("sfx_volume"));

                _midiEnabled = true;
            }

            // Setup mixer
            SyncSoundSettings();

            // allocate buffers
            _backGroundBuf = new Surface((ushort) _screenWidth, (ushort) _screenHeight, PixelFormat.Indexed8);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                _backBuf = new Surface((ushort) _screenWidth, (ushort) _screenHeight, PixelFormat.Indexed8);
                _scaleBuf = new Surface((ushort) _screenWidth, (ushort) _screenHeight, PixelFormat.Indexed8);
            }
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                _window4BackScn = new Surface((ushort) _screenWidth, (ushort) _screenHeight, PixelFormat.Indexed8);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
            {
                _window4BackScn = new Surface((ushort) _screenWidth, 134, PixelFormat.Indexed8);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
                _window4BackScn = new Surface(224, 127, PixelFormat.Indexed8);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                if (_gd.Platform == Platform.Amiga && _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
                {
                    _window4BackScn = new Surface(224, 196, PixelFormat.Indexed8);
                }
                else
                {
                    _window4BackScn = new Surface(224, 144, PixelFormat.Indexed8);
                }
                _window6BackScn = new Surface(48, 80, PixelFormat.Indexed8);
            }
            SetupGame();

            //_debugger = new Debugger(this);
            _sound = new Sound(this, gss, Mixer);

            if (ConfigManager.Instance.HasKey("music_mute") && ConfigManager.Instance.Get<bool>("music_mute"))
            {
                _musicPaused = true;
                if (_midiEnabled)
                {
                    _midi.Pause(_musicPaused);
                }
                Mixer.SetVolumeForSoundType(SoundType.Music, 0);
            }

            if (ConfigManager.Instance.HasKey("sfx_mute") &&
                ConfigManager.Instance.Get<bool>("sfx_mute"))
            {
                if (_gd.ADGameDescription.gameId == GameIds.GID_SIMON1DOS)
                    _midi._enable_sfx = !_midi._enable_sfx;
                else
                {
                    _effectsPaused = !_effectsPaused;
                    _sound.EffectsPause(_effectsPaused);
                }
            }

            _copyProtection = ConfigManager.Instance.Get<bool>("copy_protection");
            _language = LanguageHelper.ParseLanguage(ConfigManager.Instance.Get<string>("language"));

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                _speech = true;
                _subtitles = false;
            }
            else if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                _speech = !ConfigManager.Instance.Get<bool>("speech_mute");
                _subtitles = ConfigManager.Instance.Get<bool>("subtitles");

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                {
                    // English and German versions don't have full subtitles
                    if (_language == Language.EN_ANY || _language == Language.DE_DEU)
                        _subtitles = false;
                    // Other versions require speech to be enabled
                    else
                        _speech = true;
                }

                // Default to speech only, if both speech and subtitles disabled
                if (!_speech && !_subtitles)
                    _speech = true;
            }
            else
            {
                _speech = false;
                _subtitles = true;
            }
        }

        private int GetItem1ID()
        {
            return 1;
        }

#if ENABLE_AGOS2
        private void LoadArchives()
        {
            if (!_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_PACKED)) return;

            foreach (var ag in _gd.ADGameDescription.filesDescriptions)
            {
//                if (!SearchMan.hasArchive(ag.fileName))
//                {
//                    var stream = SearchMan.createReadStreamForMember(ag.fileName);
//
//                    if (stream)
//                        SearchMan.add(ag.fileName, Common::makeInstallShieldArchive(stream, DisposeAfterUse::YES),
//                            ag.fileType);
//                }
            }
        }
#endif

        private void Pause()
        {
            IsPaused = true;

            while (IsPaused && !HasToQuit)
            {
                Delay(1);
                if (_keyPressed.IsKeyDown(KeyCode.Pause))
                {
                    IsPaused = false;
                    OSystem.InputManager.ResetKeys();
                }
            }
        }

        private string GetExtra()
        {
            return _gd.ADGameDescription.extra;
        }

        private void MouseOff()
        {
            _mouseHideCount++;
        }

        private void CHECK_BOUNDS<T>(int x, T[] y)
        {
            System.Diagnostics.Debug.Assert(x < y.Length);
        }

        protected uint GetTime()
        {
            return (uint) ((DateTime.Now.Ticks / 10000) / 1000);
        }

        private static bool IS_ALIGNED(BytePtr value, int alignment)
        {
            return (value.Value & (alignment - 1)) == 0;
        }

        // Waxworks specific
        protected void LoadRoomItems(ushort currentRoom)
        {
            throw new NotImplementedException();
        }

        private static readonly byte[] polish4CD_feebleFontSize =
        {
            8, 2, 5, 8, 8, 8, 8, 2, 4, 4, 8, 8, 3, 8, 2, 9,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 2, 3, 8, 8, 8, 8,
            7, 8, 8, 8, 8, 8, 8, 8, 8, 4, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 3, 9,
            4, 8, 8, 8, 8, 8, 7, 8, 8, 4, 5, 7, 3, 8, 8, 8,
            8, 8, 8, 7, 7, 8, 8, 8, 8, 8, 8, 5, 2, 5, 8, 8,
            8, 8,
        };

        private static readonly byte[] polish2CD_feebleFontSize =
        {
            4, 2, 8, 8, 8, 8, 8, 2, 4, 4, 8, 8, 3, 8, 2, 9,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 2, 8, 8, 8, 8, 8,
            7, 8, 8, 8, 8, 8, 8, 8, 8, 4, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 4, 8,
            8, 8, 8, 8, 8, 8, 7, 8, 8, 4, 5, 7, 3, 8, 8, 8,
            8, 8, 8, 7, 7, 8, 8, 8, 8, 8, 8, 5, 2, 5, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 4, 4, 4, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 4, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 2, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
        };

        private static readonly byte[] feebleFontSize =
        {
            8, 2, 5, 7, 8, 8, 8, 2, 4, 4, 8, 8, 3, 8, 2, 9,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 2, 3, 5, 8, 5, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 4, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 4, 9, 4, 4, 9,
            4, 8, 8, 8, 8, 8, 7, 8, 8, 4, 5, 7, 3, 8, 8, 8,
            8, 8, 8, 7, 7, 8, 8, 8, 8, 8, 8, 5, 2, 5, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 4, 4, 4, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 4, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 2, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
        };

        private static readonly byte[] elvira1_soundTable =
        {
            0, 2, 0, 1, 0, 0, 0, 0, 0, 3,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 6, 4, 0, 0, 9, 0,
            0, 2, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 8, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 0, 5, 0, 6, 6, 0, 0,
            0, 5, 0, 0, 6, 0, 0, 0, 0, 8,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        };

        private static readonly ushort[] initialVideoWindows_Simon =
        {
            0, 0, 20, 200,
            0, 0, 3, 136,
            17, 0, 3, 136,
            0, 0, 20, 200,
            0, 0, 20, 134
        };

        private static readonly ushort[] initialVideoWindows_Common =
        {
            3, 0, 14, 136,
            0, 0, 3, 136,
            17, 0, 3, 136,
            0, 0, 20, 200,
            3, 3, 14, 127,
        };

        private static readonly ushort[] initialVideoWindows_PN =
        {
            3, 0, 14, 136,
            0, 0, 3, 136,
            17, 0, 3, 136,
            0, 0, 20, 200,
            3, 2, 14, 129,
        };

        protected static readonly byte[] hebrewKeyTable =
        {
            32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 90, 45, 85, 47, 48, 49, 50,
            51, 52, 53, 54, 55, 56, 57, 83, 83, 90, 61, 85, 63, 35, 89, 80, 65, 66, 87,
            75, 82, 73, 79, 71, 76, 74, 86, 78, 77, 84, 47, 88, 67, 64, 69, 68, 44, 81,
            72, 70, 91, 92, 93, 94, 95, 96, 89, 80, 65, 66, 87, 75, 82, 73, 79, 71, 76,
            74, 86, 78, 77, 84, 47, 88, 67, 64, 69, 68, 44, 81, 72, 70,
            123, 124, 125, 126, 127,
        };
    }
}