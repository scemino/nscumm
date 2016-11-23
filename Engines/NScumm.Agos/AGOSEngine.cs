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

        protected TextLocation _textLocation1 = new TextLocation();
        protected TextLocation _textLocation2 = new TextLocation();
        protected TextLocation _textLocation3 = new TextLocation();
        protected TextLocation _textLocation4 = new TextLocation();

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

        protected int _numVideoOpcodes;
        protected int _numTextBoxes;

        protected byte _numMusic;
        protected byte _numSFX;
        protected ushort _numSpeech;
        protected ushort _numZone;

        protected byte _numBitArray1;
        protected byte _numBitArray2;
        private byte _numBitArray3;
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

        private BytePtr _mouseData;
        private bool _animatePointer;
        private byte _maxCursorWidth, _maxCursorHeight;
        private byte _mouseAnim, _mouseAnimMax, _mouseCursor;
        private byte _currentMouseAnim, _currentMouseCursor;
        private byte _oldMouseAnimMax, _oldMouseCursor;
        protected ushort _mouseHideCount;
        private bool _mouseToggle;

        private bool _leftButtonDown, _rightButtonDown;
        private byte _leftButton, _leftButtonCount, _leftButtonOld;
        private byte _oneClick;
        private bool _clickOnly;
        private bool _leftClick, _rightClick;
        private bool _noRightClick;
        private short[] _variableArray;
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
        private ushort _currentRoom, _superRoomNumber;
        private byte _wallOn;

        private BytePtr _planarBuf;
        private byte[] _videoBuf1 = new byte[32000];
        protected ushort[] _videoWindows = new ushort[128];

        private bool _copyProtection;
        protected Language _language;
        protected bool _speech;
        protected bool _subtitles;
        private Item[] _itemArrayPtr;
        private int _itemArraySize;
        private int _itemArrayInited;
        private BytePtr[] _stringTabPtr;
        private int _stringTabPos;
        private int _stringTabSize;
        private int _stringTabNum;
        private Item _currentPlayer;
        protected Item _dummyItem1 = new Item();
        protected Item _dummyItem2 = new Item();
        protected Item _dummyItem3 = new Item();
        private byte[] _strippedTxtMem;
        private int _numRoomStates;
        private RoomState[] _roomStates;
        private byte[] _roomsList;
        private byte[] _xtblList;
        private BytePtr _xtablesHeapPtrOrg;
        private int _xtablesHeapCurPosOrg;
        protected Subroutine _subroutineList;
        protected Subroutine _subroutineListOrg;
        protected Subroutine _xsubroutineListOrg;
        private uint _lastMinute; // Used in processSpecialKeys()
        private uint _lastTime;
        private uint _clockStopped, _gameStoppedClock;
        private uint _timeStore;

        private TimeEvent _firstTimeStruct, _pendingDeleteTimeEvent;
        private Stream _gameFile;
        protected readonly VgaPointersEntry[] _vgaBufferPointers = CreateArray<VgaPointersEntry>(450);
        private readonly VgaSprite[] _vgaSprites = CreateArray<VgaSprite>(200);
        protected short _scrollX;
        private short _scrollXMax;
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

        private readonly ushort[] _bitArray = new ushort[128];
        protected readonly ushort[] _bitArrayTwo = new ushort[16];
        private ushort[] _bitArrayThree = new ushort[16];
        private Item _findNextPtr;

        private short _scriptVerb, _scriptNoun1;
        protected short _scriptNoun2;
        private short _scriptAdj1;
        protected short _scriptAdj2;

        protected Item _subjectItem;
        protected Item _objectItem;
        private bool _mortalFlag;

        private WindowBlock _dummyWindow;
        private readonly WindowBlock[] _windowArray = new WindowBlock[80];

        private readonly byte[] _fcsData1 = new byte[8];
        private readonly bool[] _fcsData2 = new bool[8];
        private WindowBlock _textWindow;
        private ushort _curWindow;

        private short _printCharCurPos, _printCharMaxPos, _printCharPixelCount;
        private ushort _numLettersToPrint;
        private ushort _displayFlag;
        private readonly byte[] _lettersToPrintBuf = new byte[80];

        private readonly byte[] _hebrewCharWidths = new byte[32];
        private HitArea _lastHitArea;
        private Item _hitAreaSubjectItem;
        protected Item _hitAreaObjectItem;
        protected bool _nameLocked;
        private ushort _needHitAreaRecalc;
        private HitArea _lastHitArea3;
        private bool _dragAccept;
        protected ScummInputState _keyPressed;
        private bool _dragMode;
        private HitArea _lastClickRem;
        private bool _dragFlag;
        private bool _dragEnd;
        private byte _dragCount;
        private Point _mouse;
        private Point _mouseOld;
        private HitArea _currentBox, _currentVerbBox, _lastVerbOn;
        private ushort _defaultVerb;

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
        private bool _litBoxFlag;
        private bool _inCallBack;
        private DateTime _lastVgaTick;
        private ushort _syncCount;
        private bool _cepeFlag;
        protected RandomSource _rnd;
        private int _vgaTickCounter;
        private Ptr<VgaTimerEntry> _nextVgaTimerToProcess;
        protected BytePtr _curVgaFile1;
        private ushort _zoneNumber;
        private bool _scriptVar2;
        private bool _skipVgaWait;
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
        private MidiPlayer _midi;
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

        protected BytePtr BackGround => _backGroundBuf.Pixels;

        private BytePtr BackBuf => _backBuf.Pixels;

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

        protected virtual void DrawIcon(WindowBlock window, int icon, int x, int y)
        {
            _videoLockOut |= 0x8000;

            LocksScreen(screen =>
            {
                var dst = screen.GetBasePtr(x * 8, y);
                var src = new BytePtr(_iconFilePtr, icon * 146);

                if (icon == 0xFF)
                {
                    // Draw Blank Icon
                    for (int yp = 0; yp < 24; yp++)
                    {
                        Array.Clear(dst.Data, dst.Offset, 24);
                        dst += screen.Pitch;
                    }
                }
                else
                {
                    byte[] palette = new byte[4];
                    palette[0] = (byte) (src.Value >> 4);
                    palette[1] = (byte) (src.Value & 0xf);
                    src.Offset++;
                    palette[2] = (byte) (src.Value >> 4);
                    palette[3] = (byte) (src.Value & 0xf);
                    src.Offset++;
                    for (int yp = 0; yp < 24; ++yp, src += 6)
                    {
                        // Get bit-set representing the 24 pixels for the line
                        int v1 = (src.ToUInt16BigEndian() << 8) | src[4];
                        int v2 = (src.ToUInt16BigEndian(2) << 8) | src[5];
                        for (int xp = 0; xp < 24; ++xp, v1 >>= 1, v2 >>= 1)
                        {
                            dst[yp * screen.Pitch + (23 - xp)] = palette[((v1 & 1) << 1) | (v2 & 1)];
                        }
                    }
                }
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
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
            OSystem.GraphicsManager.CopyRectToScreen(screen.Pixels, 0, screen.Pitch, 0, 0, screen.Width,
                screen.Height);
        }

        protected void LoadVGABeardFile(int i)
        {
            throw new NotImplementedException();
        }

        protected int GetOffsetOfChild2Param(SubObject child, int prop)
        {
            int m = 1;
            int offset = 0;
            while (m != prop)
            {
                if ((child.objectFlags & (SubObjectFlags) m) != 0)
                    offset++;
                m *= 2;
            }
            return offset;
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

        private uint GetNextVarContents()
        {
            return (ushort) ReadVariable((ushort) GetVarWrapper());
        }

        protected uint GetVarWrapper()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                return GetVarOrWord();
            return GetVarOrByte();
        }

        protected uint GetVarOrByte()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                return GetVarOrWord();
            }
            uint a = _codePtr.Value;
            _codePtr.Offset++;
            if (a != 255)
                return a;
            var v = ReadVariable(_codePtr.Value);
            _codePtr.Offset++;
            return v;
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
            int hitarea_id = (int)GetVarOrWord();
            int x = (int)GetVarOrWord();
            int y = (int)GetVarOrWord();
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

        protected void SetTextColor(uint color)
        {
            WindowBlock window = _windowArray[_curWindow];

            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR) && color != 0)
            {
                if (window.fillColor == 17)
                    color = 25;
                else
                    color = 220;
            }

            window.textColor = (byte) color;
        }

        protected virtual int SetupIconHitArea(WindowBlock window, uint num, int x, int y, Item itemPtr)
        {
            var ha = FindEmptyHitArea();
            ha.Value.x = (ushort) ((x + window.x) * 8);
            ha.Value.y = (ushort) (y * 8 + window.y);
            ha.Value.itemPtr = itemPtr;
            ha.Value.width = 24;
            ha.Value.height = 24;
            ha.Value.flags = (ushort) (BoxFlags.kBFDragBox | BoxFlags.kBFBoxInUse | BoxFlags.kBFBoxItem);
            ha.Value.id = 0x7FFD;
            ha.Value.priority = 100;
            ha.Value.verb = 253;

            return Array.IndexOf(_hitAreas, ha);
        }

        protected virtual int ItemGetIconNumber(Item item)
        {
            return GetUserFlag(item, 7);
        }

        protected virtual bool HasIcon(Item item)
        {
            return GetUserFlag(item, 7) != 0;
        }

        protected virtual void AddArrows(WindowBlock window, uint num)
        {
            throw new NotImplementedException();
        }

        protected uint GetNextStringID()
        {
            return (ushort) GetNextWord();
        }

        protected void SetScriptCondition(bool cond)
        {
            _runScriptCondition[_recursionDepth] = cond;
        }

        protected int GetUserFlag1(Item haItemPtr, int a)
        {
            throw new NotImplementedException();
        }

        protected void StopAnimateSimon2(ushort a, ushort b)
        {
            byte[] items = new byte[4];
            items.WriteUInt16(0, To16Wrapper(a));
            items.WriteUInt16(2, To16Wrapper(b));

            _videoLockOut |= 0x8000;
            _vcPtr = items;
            vc60_stopAnimation();
            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected int GetNextWord()
        {
            short a = (short) _codePtr.ToUInt16BigEndian();
            _codePtr += 2;
            return a;
        }

        protected void Animate(ushort windowNum, ushort zoneNum, ushort vgaSpriteId, short x, short y,
            ushort palette, bool vgaScript = false)
        {
            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_PN &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_ELVIRA1)
            {
                if (IsSpriteLoaded(vgaSpriteId, zoneNum))
                    return;
            }

            Ptr<VgaSprite> vsp = _vgaSprites;
            while (vsp.Value.id != 0)
                vsp.Offset++;

            vsp.Value.windowNum = windowNum;
            vsp.Value.priority = 0;
            vsp.Value.flags = 0;

            vsp.Value.y = y;
            vsp.Value.x = x;
            vsp.Value.image = 0;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                vsp.Value.palette = 0;
            else
                vsp.Value.palette = palette;
            vsp.Value.id = vgaSpriteId;
            vsp.Value.zoneNum = zoneNum;

            for (;;)
            {
                var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, zoneNum);
                _curVgaFile1 = vpe.Value.vgaFile1;
                if (vgaScript)
                {
                    if (vpe.Value.vgaFile1 != BytePtr.Null)
                        break;
                    if (_zoneNumber != zoneNum)
                        _noOverWrite = _zoneNumber;

                    LoadZone(zoneNum);
                    _noOverWrite = 0xFFFF;
                }
                else
                {
                    _zoneNumber = zoneNum;
                    if (vpe.Value.vgaFile1 != BytePtr.Null)
                        break;
                    LoadZone(zoneNum);
                }
            }

            var pp = _curVgaFile1;
            BytePtr p;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                throw new NotImplementedException();
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                p = pp + pp.ToUInt16BigEndian(4);
                var header = new VgaFile1Header_Common(p);
                int count = ScummHelper.SwapBytes(header.animationCount);
                p = pp + ScummHelper.SwapBytes(header.animationTable);

                AnimationHeader_Simon animHeader;
                while (count-- != 0)
                {
                    animHeader = new AnimationHeader_Simon(p);
                    if (ScummHelper.SwapBytes(animHeader.id) == vgaSpriteId)
                        break;
                    p += AnimationHeader_Simon.Size;
                }

                animHeader = new AnimationHeader_Simon(p);
                System.Diagnostics.Debug.Assert(ScummHelper.SwapBytes(animHeader.id) == vgaSpriteId);
            }
            else
            {
                throw new NotImplementedException();
            }

//            if (DebugMan.isDebugChannelEnabled(kDebugVGAScript)) {
//                if (_gd.ADGameDescription.gameType == GType_FF || _gd.ADGameDescription.gameType == GType_PP) {
//                    DumpVgaScript(_curVgaFile1 + READ_LE_UINT16(&((AnimationHeader_Feeble*)p).scriptOffs), zoneNum, vgaSpriteId);
//                } else if (_gd.ADGameDescription.gameType == GType_SIMON1 || _gd.ADGameDescription.gameType == GType_SIMON2) {
//                    DumpVgaScript(_curVgaFile1 + READ_BE_UINT16(&((AnimationHeader_Simon*)p).scriptOffs), zoneNum, vgaSpriteId);
//                } else {
//                    DumpVgaScript(_curVgaFile1 + READ_BE_UINT16(&((AnimationHeader_WW*)p).scriptOffs), zoneNum, vgaSpriteId);
//                }
//            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                throw new NotImplementedException();
                // AddVgaEvent(_vgaBaseDelay, ANIMATE_EVENT,_curVgaFile1 + READ_LE_UINT16(&((AnimationHeader_Feeble*) p).scriptOffs), vgaSpriteId, zoneNum);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                AddVgaEvent(_vgaBaseDelay, EventType.ANIMATE_EVENT,
                    _curVgaFile1 + ScummHelper.SwapBytes(new AnimationHeader_Simon(p).scriptOffs), vgaSpriteId, zoneNum);
            }
            else
            {
                throw new NotImplementedException();
                // AddVgaEvent(_vgaBaseDelay, ANIMATE_EVENT,_curVgaFile1 + READ_BE_UINT16(&((AnimationHeader_WW*) p).scriptOffs), vgaSpriteId, zoneNum);
            }
        }

        protected void vc60_stopAnimation()
        {
            ushort sprite, zoneNum;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                zoneNum = (ushort) VcReadNextWord();
                sprite = (ushort) VcReadVarOrWord();
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
            {
                zoneNum = (ushort) VcReadNextWord();
                sprite = (ushort) VcReadNextWord();
            }
            else
            {
                sprite = (ushort) VcReadNextWord();
                zoneNum = (ushort) (sprite / 100);
            }

            VcStopAnimation(zoneNum, sprite);
        }

        protected virtual void VcStopAnimation(ushort zone, ushort sprite)
        {
            ushort oldCurSpriteId, oldCurZoneNum;

            oldCurSpriteId = _vgaCurSpriteId;
            oldCurZoneNum = _vgaCurZoneNum;
            var vcPtrOrg = _vcPtr;

            _vgaCurZoneNum = zone;
            _vgaCurSpriteId = sprite;

            var vsp = FindCurSprite().Value;
            if (vsp.id != 0)
            {
                vc25_halt_sprite();

                Ptr<VgaTimerEntry> vte = _vgaTimerList;
                while (vte.Value.delay != 0)
                {
                    if (vte.Value.id == _vgaCurSpriteId && vte.Value.zoneNum == _vgaCurZoneNum)
                    {
                        DeleteVgaEvent(vte);
                        break;
                    }
                    vte.Offset++;
                }
            }

            _vgaCurZoneNum = oldCurZoneNum;
            _vgaCurSpriteId = oldCurSpriteId;
            _vcPtr = vcPtrOrg;
        }

        protected abstract void ExecuteOpcode(int opcode);

        protected uint GetVarOrWord()
        {
            uint a = _codePtr.ToUInt16BigEndian();
            _codePtr += 2;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                if (a >= 60000 && a < 62048)
                {
                    return ReadVariable((ushort) (a - 60000));
                }
            }
            else
            {
                if (a >= 30000 && a < 30512)
                {
                    return ReadVariable((ushort) (a - 30000));
                }
            }
            return a;
        }

        protected virtual bool LoadTablesIntoMem(ushort subrId)
        {
            if (_tblList == BytePtr.Null)
                return false;

            var p = 32;

            var minNum = _tblList.ToUInt16BigEndian(p);
            var maxNum = _tblList.ToUInt16BigEndian(p + 2);
            ushort fileNum = _tblList[p + 4];
            p += 6;

            while (minNum != 0)
            {
                if ((subrId >= minNum) && (subrId <= maxNum))
                {
                    _subroutineList = _subroutineListOrg;
                    _tablesHeapPtr = _tablesHeapPtrOrg;
                    _tablesHeapCurPos = _tablesHeapCurPosOrg;
                    _stringIdLocalMin = 1;
                    _stringIdLocalMax = 0;

                    var filename = $"TABLES%.{fileNum:D2}";
                    var @in = OpenTablesFile(filename);
                    ReadSubroutineBlock(@in);
                    CloseTablesFile(@in);

                    AlignTableMem();

                    _tablesheapPtrNew = _tablesHeapPtr;
                    _tablesHeapCurPosNew = _tablesHeapCurPos;

                    if (_tablesHeapCurPos > _tablesHeapSize)
                        Error("loadTablesIntoMem: Out of table memory");
                    return true;
                }

                minNum = _tblList.ToUInt16BigEndian(p);
                maxNum = _tblList.ToUInt16BigEndian(p + 2);
                fileNum = _tblList[p + 4];
                p += 6;
            }

            Debug(1, "loadTablesIntoMem: didn't find {0}", subrId);
            return false;
        }

        protected Stream OpenTablesFile(string filename)
        {
            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_OLD_BUNDLE))
                return OpenTablesFileSimon1(filename);
            return OpenTablesFileGme(filename);
        }

        protected void CloseTablesFile(Stream @in)
        {
            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_OLD_BUNDLE))
            {
                @in.Dispose();
            }
        }

        protected void SetMoveRect(ushort x, ushort y, ushort width, ushort height)
        {
            if (x < _moveXMin)
                _moveXMin = x;

            if (y < _moveYMin)
                _moveYMin = y;

            if (width > _moveXMax)
                _moveXMax = width;

            if (height > _moveYMax)
                _moveYMax = height;
        }

        protected void DrawBackGroundImage(VC10_state state)
        {
            state.width = (ushort) _screenWidth;
            if (_window3Flag == 1)
            {
                state.width = 0;
                state.x_skip = 0;
                state.y_skip = 0;
            }

            var src = state.srcPtr + state.width * state.y_skip + state.x_skip * 8;
            var dst = state.surf_addr;

            state.draw_width *= 2;

            int h = state.draw_height;
            int w = state.draw_width;
            var paletteMod = state.paletteMod;
            do
            {
                for (var i = 0; i != w; i += 2)
                {
                    dst[i] = (byte) (src[i] + paletteMod);
                    dst[i + 1] = (byte) (src[i + 1] + paletteMod);
                }
                dst += (int) state.surf_pitch;
                src += state.width;
            } while (--h != 0);
        }

        protected void Draw32ColorImage(VC10_state state)
        {
            if (state.flags.HasFlag(DrawFlags.kDFCompressed))
            {
                var dstPtr = state.surf_addr;
                var src = state.srcPtr;
                /* AAAAAAAA BBBBBBBB CCCCCCCC DDDDDDDD EEEEEEEE
                 * aaaaabbb bbcccccd ddddeeee efffffgg ggghhhhh
                 */

                do
                {
                    int count = state.draw_width / 4;

                    var dst = dstPtr;
                    do
                    {
                        int bits = (src[0] << 24) | (src[1] << 16) | (src[2] << 8) | (src[3]);

                        var color = (byte) ((bits >> (32 - 5)) & 31);
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                            dst[0] = color;
                        color = (byte) ((bits >> (32 - 10)) & 31);
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                            dst[1] = color;
                        color = (byte) ((bits >> (32 - 15)) & 31);
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                            dst[2] = color;
                        color = (byte) ((bits >> (32 - 20)) & 31);
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                            dst[3] = color;
                        color = (byte) ((bits >> (32 - 25)) & 31);
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                            dst[4] = color;
                        color = (byte) ((bits >> (32 - 30)) & 31);
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                            dst[5] = color;

                        bits = (bits << 8) | src[4];

                        color = (byte) ((bits >> (40 - 35)) & 31);
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                            dst[6] = color;
                        color = (byte) ((bits) & 31);
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                            dst[7] = color;

                        dst += 8;
                        src += 5;
                    } while (--count != 0);
                    dstPtr.Offset += (int) state.surf_pitch;
                } while (--state.draw_height != 0);
            }
            else
            {
                try
                {
                    var src = state.srcPtr + (state.width * state.y_skip * 16) + (state.x_skip * 8);
                    var dst = state.surf_addr;

                    state.draw_width *= 2;

                    int h = state.draw_height;
                    do
                    {
                        for (var i = 0; i != state.draw_width; i++)
                            if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || src[i] != 0)
                                dst[i] = (byte) (src[i] + state.paletteMod);
                        dst.Offset += (int) state.surf_pitch;
                        src.Offset += state.width * 16;
                    } while (--h != 0);
                }
                catch (Exception e)
                {
                    int tmp = 42;
                }
            }
        }

        protected void DrawVertImage(VC10_state state)
        {
            if (state.flags.HasFlag(DrawFlags.kDFCompressed))
            {
                DrawVertImageCompressed(state);
            }
            else
            {
                DrawVertImageUncompressed(state);
            }
        }

        protected bool DrawImageClip(VC10_state state)
        {
            var vlut = new Ptr<ushort>(_videoWindows, _windowNum * 4);

            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_FF &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_PP)
            {
                state.draw_width = (ushort) (state.width * 2);
            }

            int cur = state.x;
            if (cur < 0)
            {
                do
                {
                    if (--state.draw_width == 0)
                        return false;
                    state.x_skip++;
                } while (++cur != 0);
            }
            state.x = (short) cur;

            var maxWidth = _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                           _gd.ADGameDescription.gameType == SIMONGameType.GType_PP
                ? _screenWidth
                : vlut[2] * 2;
            cur += state.draw_width - maxWidth;
            if (cur > 0)
            {
                do
                {
                    if (--state.draw_width == 0)
                        return false;
                } while (--cur != 0);
            }

            cur = state.y;
            if (cur < 0)
            {
                do
                {
                    if (--state.draw_height == 0)
                        return false;
                    state.y_skip++;
                } while (++cur != 0);
            }
            state.y = (short) cur;

            var maxHeight = _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                            _gd.ADGameDescription.gameType == SIMONGameType.GType_PP
                ? _screenHeight
                : vlut[3];
            cur += state.draw_height - maxHeight;
            if (cur > 0)
            {
                do
                {
                    if (--state.draw_height == 0)
                        return false;
                } while (--cur != 0);
            }

            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_FF &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_PP)
            {
                state.draw_width *= 4;
            }

            return state.draw_width != 0 && state.draw_height != 0;
        }

        protected virtual void DrawImage(VC10_state state)
        {
            throw new NotImplementedException();
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

        protected void ReadSubroutineBlock(Stream @in)
        {
            var br = new BinaryReader(@in);
            while (br.ReadUInt16BigEndian() == 0)
            {
                ReadSubroutine(br, CreateSubroutine(br.ReadUInt16BigEndian()));
            }
        }

        protected void AlignTableMem()
        {
            if (!IS_ALIGNED(_tablesHeapPtr, 4))
            {
                _tablesHeapCurPos += 2;
                _tablesHeapCurPos += 2;
            }
        }

        protected virtual void ReadItemChildren(BinaryReader br, Item item, ChildType type)
        {
            if (type == ChildType.kRoomType)
            {
                var subRoom = AllocateChildBlock<SubRoom>(item, ChildType.kRoomType);
                subRoom.roomShort = (ushort) br.ReadUInt32BigEndian();
                subRoom.roomLong = (ushort) br.ReadUInt32BigEndian();
                subRoom.flags = br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kObjectType)
            {
                var subObject =
                    AllocateChildBlock<SubObject>(item, ChildType.kObjectType);
                br.ReadUInt32BigEndian();
                br.ReadUInt32BigEndian();
                br.ReadUInt32BigEndian();
                subObject.objectName = (ushort) br.ReadUInt32BigEndian();
                subObject.objectSize = br.ReadUInt16BigEndian();
                subObject.objectWeight = br.ReadUInt16BigEndian();
                subObject.objectFlags = (SubObjectFlags) br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kGenExitType)
            {
                var genExit = AllocateChildBlock<SubGenExit>(item, ChildType.kGenExitType);
                genExit.dest[0] = (ushort) FileReadItemID(br);
                genExit.dest[1] = (ushort) FileReadItemID(br);
                genExit.dest[2] = (ushort) FileReadItemID(br);
                genExit.dest[3] = (ushort) FileReadItemID(br);
                genExit.dest[4] = (ushort) FileReadItemID(br);
                genExit.dest[5] = (ushort) FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
            }
            else if (type == ChildType.kContainerType)
            {
                var container = AllocateChildBlock<SubContainer>(item, ChildType.kContainerType);
                container.volume = br.ReadUInt16BigEndian();
                container.flags = br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kChainType)
            {
                var chain = AllocateChildBlock<SubChain>(item, ChildType.kChainType);
                chain.chChained = (ushort) FileReadItemID(br);
            }
            else if (type == ChildType.kUserFlagType)
            {
                SetUserFlag(item, 0, br.ReadUInt16BigEndian());
                SetUserFlag(item, 1, br.ReadUInt16BigEndian());
                SetUserFlag(item, 2, br.ReadUInt16BigEndian());
                SetUserFlag(item, 3, br.ReadUInt16BigEndian());
                SetUserFlag(item, 4, br.ReadUInt16BigEndian());
                SetUserFlag(item, 5, br.ReadUInt16BigEndian());
                SetUserFlag(item, 6, br.ReadUInt16BigEndian());
                SetUserFlag(item, 7, br.ReadUInt16BigEndian());
                var subUserFlag = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
                subUserFlag.userItems[0] = (ushort) FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
            }
            else if (type == ChildType.kInheritType)
            {
                var inherit = AllocateChildBlock<SubInherit>(item, ChildType.kInheritType);
                inherit.inMaster = (ushort) FileReadItemID(br);
            }
            else
            {
                Error("readItemChildren: invalid type {0}", type);
            }
        }

        protected static uint FileReadItemID(BinaryReader br)
        {
            uint val = br.ReadUInt32BigEndian();
            if (val == 0xFFFFFFFF)
                return 0;
            return val + 2;
        }

        protected void SetUserFlag(Item item, int a, int b)
        {
            var subUserFlag = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
            if (subUserFlag == null)
            {
                subUserFlag =
                    AllocateChildBlock<SubUserFlag>(item, ChildType.kUserFlagType);
            }

            if (a < 0 || a > 7)
                return;

            subUserFlag.userFlags[a] = (ushort) b;
        }

        protected Child FindChildOfType(Item i, ChildType type)
        {
            Item b = null;
            var child = i.children;

            while (child != null)
            {
                if (child.type == type)
                    return child;
                if (child.type == (ChildType) 255)
                    b = DerefItem(((SubInherit) child).inMaster);
                child = child.next;
            }
            if (b == null) return null;

            child = b.children;
            while (child != null)
            {
                if (child.type == type)
                    return child;
                child = child.next;
            }

            return null;
        }

        protected T AllocateChildBlock<T>(Item i, ChildType type) where T : Child, new()
        {
            var child = AllocateItem<T>();
            child.next = i.children;
            i.children = child;
            child.type = type;
            return child;
        }

        protected void InitMouse()
        {
            _maxCursorWidth = 16;
            _maxCursorHeight = 16;
            _mouseData = new byte[_maxCursorWidth * _maxCursorHeight];

            _mouseData.Data.Set(_mouseData.Offset, 0xFF, _maxCursorWidth * _maxCursorHeight);

// CursorMan.replaceCursorPalette(mouseCursorPalette, 0, ARRAYSIZE(mouseCursorPalette) / 3);
        }


        private void FreezeBottom()
        {
            _vgaMemBase = _vgaMemPtr;
            _vgaFrozenBase = _vgaMemPtr;
        }

        protected void UnfreezeBottom()
        {
            _vgaMemPtr = _vgaRealBase;
            _vgaMemBase = _vgaRealBase;
            _vgaFrozenBase = _vgaRealBase;
        }

        private void SynchChain(Item i)
        {
            SubChain c = (SubChain) FindChildOfType(i, ChildType.kChainType);
            while (c != null)
            {
                SetItemState(DerefItem(c.chChained), i.state);
                c = (SubChain) NextSub(c, ChildType.kChainType);
            }
        }

        private Child NextSub(Child sub, ChildType key)
        {
            Child a = sub.next;
            while (a != null)
            {
                if (a.type == key)
                    return a;
                a = a.next;
            }
            return null;
        }

        private void SetItemState(Item item, int value)
        {
            item.state = (short) value;
        }

        private void UnlockScreen(Surface screen)
        {
            OSystem.GraphicsManager.CopyRectToScreen(screen.Pixels, 0, screen.Pitch, 0, 0, screen.Width,
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
                //if (_midiEnabled)
                //{
                //    _midi.Pause(_musicPaused);
                //}
                Mixer.SetVolumeForSoundType(SoundType.Music, 0);
            }

//            if (ConfigManager.Instance.HasKey("sfx_mute") &&
            //                ConfigManager.Instance.Get<bool>("sfx_mute"))
            //            {
            //                if (_gd.ADGameDescription.gameId == GameIds.GID_SIMON1DOS)
            //                    _midi._enable_sfx = !_midi._enable_sfx;
            //                else
            //                {
            //                    _effectsPaused = !_effectsPaused;
            //                    _sound.EffectsPause(_effectsPaused);
            //                }
            //            }

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


        private Item GetNextItemPtrStrange()
        {
            int a = GetNextWord();
            switch (a)
            {
                case -1:
                    return _subjectItem;
                case -3:
                    return _objectItem;
                case -5:
                    return _dummyItem2;
                case -7:
                    return null;
                case -9:
                    return _dummyItem3;
                default:
                    return DerefItem((uint) a);
            }
        }

        private void CloseWindow(uint a)
        {
            if (_windowArray[a] == null)
                return;
            RemoveIconArray((int) a);
            ResetWindow(_windowArray[a]);
            _windowArray[a] = null;
            if (_curWindow == a)
            {
                _textWindow = null;
                ChangeWindow(0);
            }
        }

        private void ResetWindow(WindowBlock window)
        {
            if ((window.flags & 8) != 0)
                RestoreWindow(window);
            window.mode = 0;
        }

        private void RestoreWindow(WindowBlock window)
        {
            _videoLockOut |= 0x8000;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                RestoreBlock((ushort) (window.y + window.height), (ushort) (window.x + window.width),
                    (ushort) window.y, (ushort) window.x);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                if (_restoreWindow6 && _windowArray[2] == window)
                {
                    window = _windowArray[6];
                    _restoreWindow6 = false;
                }

                RestoreBlock((ushort) (window.x * 8), (ushort) window.y,
                    (ushort) ((window.x + window.width) * 8),
                    (ushort) (window.y + window.height * 8));
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
            {
                RestoreBlock((ushort) (window.x * 8), (ushort) window.y,
                    (ushort) ((window.x + window.width) * 8),
                    (ushort) (window.y + window.height * 8 + (window == _windowArray[2] ? 1 : 0)));
            }
            else
            {
                ushort x = (ushort) window.x;
                ushort w = (ushort) window.width;

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                {
                    // Adjustments to remove inventory arrows
                    if ((x & 1) != 0)
                    {
                        x--;
                        w++;
                    }
                    if ((w & 1) != 0)
                    {
                        w++;
                    }
                }

                RestoreBlock((ushort) (x * 8), (ushort) window.y,
                    (ushort) ((x + w) * 8), (ushort) (window.y + window.height * 8));
            }

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        private void RestoreBlock(ushort x, ushort y, ushort w, ushort h)
        {
            LocksScreen(screen =>
            {
                var dst = screen.Pixels;
                var src = BackGround;

                dst += y * screen.Pitch;
                src += y * _backGroundBuf.Pitch;

                byte paletteMod = 0;
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                    !_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO) && y >= 133)
                    paletteMod = 16;

                while (y < h)
                {
                    for (var i = x; i < w; i++)
                        dst[i] = (byte) (src[i] + paletteMod);
                    y++;
                    dst += screen.Pitch;
                    src += _backGroundBuf.Pitch;
                }
            });
        }

        private void SetItemParent(Item item, Item parent)
        {
            Item old_parent = DerefItem(item.parent);

            if (item == parent)
                Error("setItemParent: Trying to set item as its own parent");

            // unlink it if it has a parent
            if (old_parent != null)
                UnlinkItem(item);
            ItemChildrenChanged(old_parent);
            LinkItem(item, parent);
            ItemChildrenChanged(parent);
        }

        private void LinkItem(Item item, Item parent)
        {
            uint id;
            // Don't allow that an item that is already linked is relinked
            if (item.parent != 0)
                return;

            id = (uint) ItemPtrToID(parent);
            item.parent = (ushort) id;

            if (parent != null)
            {
                item.next = parent.child;
                parent.child = (ushort) ItemPtrToID(item);
            }
            else
            {
                item.next = 0;
            }
        }

        private void UnlinkItem(Item item)
        {
            Item first, parent, next;

            // can't unlink item without parent
            if (item.parent == 0)
                return;

            // get parent and first child of parent
            parent = DerefItem(item.parent);
            first = DerefItem(parent.child);

            // the node to remove is first in the parent's children?
            if (first == item)
            {
                parent.child = item.next;
                item.parent = 0;
                item.next = 0;
                return;
            }

            for (;;)
            {
                if (first == null)
                    Error("unlinkItem: parent empty");
                if (first.next == 0)
                    Error("unlinkItem: parent does not contain child");

                next = DerefItem(first.next);
                if (next == item)
                {
                    first.next = next.next;
                    item.parent = 0;
                    item.next = 0;
                    return;
                }
                first = next;
            }
        }

        private void ItemChildrenChanged(Item item)
        {
            int i;

            if (_noParentNotify)
                return;

            MouseOff();

            for (i = 0; i != 8; i++)
            {
                var window = _windowArray[i];
                if (window?.iconPtr != null && window.iconPtr.itemRef == item)
                {
                    if (_fcsData1[i] != 0)
                    {
                        _fcsData2[i] = true;
                    }
                    else
                    {
                        _fcsData2[i] = false;
                        DrawIconArray(i, item, window.iconPtr.line, window.iconPtr.classMask);
                    }
                }
            }

            MouseOn();
        }

        private void MouseOn()
        {
            _videoLockOut |= 1;

            if (_mouseHideCount != 0)
                _mouseHideCount--;

            _videoLockOut = (ushort) (_videoLockOut & ~1);
        }

        private void DrawIconArray(int num, Item itemRef, int line, int classMask)
        {
            Item item_ptr_org = itemRef;
            uint width, height;
            uint k;
            bool item_again, showArrows;
            int iconSize = (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) ? 20 : 1;

            var window = _windowArray[num & 7];

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                width = 100;
                height = 40;
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
            {
                width = (uint) (window.width / 3);
                height = (uint) (window.height / 2);
            }
            else
            {
                width = (uint) (window.width / 3);
                height = (uint) (window.height / 3);
            }

            if (window == null)
                return;

            if (window.iconPtr != null)
                RemoveIconArray(num);

            window.iconPtr = new IconBlock();
            window.iconPtr.itemRef = itemRef;
            window.iconPtr.upArrow = uint.MaxValue;
            window.iconPtr.downArrow = -1;
            window.iconPtr.line = (short) line;
            window.iconPtr.classMask = (ushort) classMask;

            itemRef = DerefItem(itemRef.child);

            uint curWidth;
            while (itemRef != null && line-- != 0)
            {
                curWidth = 0;
                while (itemRef != null && width > curWidth)
                {
                    if ((classMask == 0 || (itemRef.classFlags & classMask) != 0) && HasIcon(itemRef))
                        curWidth = (uint) (curWidth + iconSize);
                    itemRef = DerefItem(itemRef.next);
                }
            }

            if (itemRef == null)
            {
                window.iconPtr.line = 0;
                itemRef = DerefItem(item_ptr_org.child);
            }

            var x_pos = 0;
            var y_pos = 0;
            k = 0;
            item_again = false;
            showArrows = false;

            while (itemRef != null)
            {
                if ((classMask == 0 || (itemRef.classFlags & classMask) != 0) && HasIcon(itemRef))
                {
                    if (item_again == false)
                    {
                        window.iconPtr.iconArray[k].item = itemRef;
                        if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                        {
                            DrawIcon(window, ItemGetIconNumber(itemRef), x_pos, y_pos);
                            window.iconPtr.iconArray[k].boxCode =
                                (ushort) SetupIconHitArea(window, 0, x_pos, y_pos, itemRef);
                        }
                        else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                                 _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                        {
                            DrawIcon(window, ItemGetIconNumber(itemRef), x_pos * 3, y_pos);
                            window.iconPtr.iconArray[k].boxCode =
                                (ushort) SetupIconHitArea(window, 0, x_pos * 3, y_pos, itemRef);
                        }
                        else
                        {
                            DrawIcon(window, ItemGetIconNumber(itemRef), x_pos * 3, y_pos * 3);
                            window.iconPtr.iconArray[k].boxCode =
                                (ushort) SetupIconHitArea(window, 0, x_pos * 3, y_pos * 3, itemRef);
                        }
                        k++;
                    }
                    else
                    {
                        window.iconPtr.iconArray[k].item = null;
                        showArrows = true;
                    }

                    x_pos = (x_pos + iconSize);
                    if (x_pos >= width)
                    {
                        x_pos = 0;
                        y_pos = (y_pos + iconSize);
                        if (y_pos >= height)
                            item_again = true;
                    }
                }
                itemRef = DerefItem(itemRef.next);
            }

            window.iconPtr.iconArray[k].item = null;

            if (showArrows || window.iconPtr.line != 0)
            {
                /* Plot arrows and add their boxes */
                AddArrows(window, (uint) num);
                window.iconPtr.upArrow = _scrollUpHitArea;
                window.iconPtr.downArrow = (short) _scrollDownHitArea;
            }
        }

        private int GetUserFlag(Item item, int a)
        {
            var subUserFlag = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
            if (subUserFlag == null)
                return 0;

            int max = _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ? 7 : 3;
            if (a < 0 || a > max)
                return 0;

            return subUserFlag.userFlags[a];
        }

        private void RemoveIconArray(int num)
        {
            ushort curWindow;
            ushort i;

            var window = _windowArray[num & 7];
            curWindow = _curWindow;

            if (window?.iconPtr == null)
                return;

            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_FF &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_PP)
            {
                ChangeWindow((uint) num);
                SendWindow(12);
                ChangeWindow(curWindow);
            }

            for (i = 0; window.iconPtr.iconArray[i].item != null; i++)
            {
                FreeBox(window.iconPtr.iconArray[i].boxCode);
            }

            if (window.iconPtr.upArrow != -1)
            {
                FreeBox(window.iconPtr.upArrow);
            }

            if (window.iconPtr.downArrow != -1)
            {
                FreeBox((uint) window.iconPtr.downArrow);
                RemoveArrows(window, num);
            }

            window.iconPtr = null;

            _fcsData1[num] = 0;
            _fcsData2[num] = false;
        }

        private void RemoveArrows(WindowBlock window, int num)
        {
            throw new NotImplementedException();
        }

        private void ChangeWindow(uint a)
        {
            a &= 7;

            if (_windowArray[a] == null || _curWindow == a)
                return;

            _curWindow = (ushort) a;
            JustifyOutPut(0);
            _textWindow = _windowArray[a];
            JustifyStart();
        }

        private Item Actor()
        {
            Error("actor: is this code ever used?");
            //if (_actorPlayer)
            //	return _actorPlayer;
            return _dummyItem1; // for compilers that don't support NORETURN
        }

        protected Item GetNextItemPtr()
        {
            int a = GetNextWord();
            switch (a)
            {
                case -1:
                    return _subjectItem;
                case -3:
                    return _objectItem;
                case -5:
                    return Me();
                case -7:
                    return Actor();
                case -9:
                    return DerefItem(Me().parent);
                default:
                    return DerefItem((uint) a);
            }
        }

        private WindowBlock OpenWindow(uint x, uint y, uint w, uint h, uint flags, uint fillColor, uint textColor)
        {
            var window = _windowList.FirstOrDefault(o => o.mode == 0);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && y >= 133)
                textColor += 16;

            window.mode = 2;
            window.x = (short) x;
            window.y = (short) y;
            window.width = (short) w;
            window.height = (short) h;
            window.flags = (byte) flags;
            window.fillColor = (byte) fillColor;
            window.textColor = (byte) textColor;
            window.textColumn = 0;
            window.textColumnOffset = 0;
            window.textRow = 0;
            window.scrollY = 0;

            // Characters are 6 pixels
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                window.textMaxLength = (ushort) ((window.width * 8 - 4) / 6);
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                window.textMaxLength = (ushort) (window.width * 8 / 6 + 1);
            else
                window.textMaxLength = (ushort) (window.width * 8 / 6);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                ClearWindow(window);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                _gd.Platform == Platform.Amiga && window.fillColor == 225)
                window.fillColor = (byte) (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR) ? 17 : 241);

            return window;
        }

        private int GetNextItemID()
        {
            int a = GetNextWord();
            switch (a)
            {
                case -1:
                    return ItemPtrToID(_subjectItem);
                case -3:
                    return ItemPtrToID(_objectItem);
                case -5:
                    return GetItem1ID();
                case -7:
                    return 0;
                case -9:
                    return Me().parent;
                default:
                    return a;
            }
        }

        protected Item Me()
        {
            if (_currentPlayer != null)
                return _currentPlayer;
            return _dummyItem1;
        }

        private int GetItem1ID()
        {
            return 1;
        }

        private int ItemPtrToID(Item id)
        {
            int i;
            for (i = 0; i != _itemArraySize; i++)
                if (_itemArrayPtr[i] == id)
                    return i;
            Error("itemPtrToID: not found");
            return 0; // for compilers that don't support NORETURN
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

        private void LightMenuStrip(int getUserFlag1)
        {
            throw new NotImplementedException();
        }

        private void DoMenuStrip(uint menuForWw)
        {
            throw new NotImplementedException();
        }

        private uint menuFor_e2(Item haItemPtr)
        {
            throw new NotImplementedException();
        }

        private uint menuFor_ww(Item haItemPtr, uint id)
        {
            throw new NotImplementedException();
        }

        private void WaitForSync(uint a)
        {
            int maxCount = _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ? 1000 : 2500;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                if (a != 200)
                {
                    ushort tmp = _lastVgaWaitFor;
                    _lastVgaWaitFor = 0;
                    if (tmp == a)
                        return;
                }
            }

            _vgaWaitFor = (ushort) a;
            _syncCount = 0;
            _exitCutscene = false;
            _rightButtonDown = false;

            while (_vgaWaitFor != 0 && !HasToQuit)
            {
                if (_rightButtonDown)
                {
                    if (_vgaWaitFor == 200 && (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                                               !GetBitFlag(14)))
                    {
                        SkipSpeech();
                        break;
                    }
                }
                if (_exitCutscene)
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                    {
                        if (_variableArray[105] == 0)
                        {
                            _variableArray[105] = 255;
                            break;
                        }
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                             _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    {
                        if (_vgaWaitFor == 51)
                        {
                            SetBitFlag(244, true);
                            break;
                        }
                    }
                    else
                    {
                        if (GetBitFlag(9))
                        {
                            EndCutscene();
                            break;
                        }
                    }
                }
                ProcessSpecialKeys();

                if (_syncCount >= maxCount)
                {
                    Warning("waitForSync: wait timed out");
                    break;
                }

                Delay(1);
            }
        }

        private void EndCutscene()
        {
            _sound.StopVoice();

            var sub = GetSubroutineByID(170);
            if (sub != null)
                StartSubroutineEx(sub);

            _runScriptReturn1 = true;
        }

        private void SkipSpeech()
        {
            _sound.StopVoice();
            if (!GetBitFlag(28))
            {
                SetBitFlag(14, true);
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                {
                    _variableArray[103] = 5;
                    Animate(4, 2, 13, 0, 0, 0);
                    WaitForSync(213);
                    StopAnimateSimon2(2, 1);
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    _variableArray[100] = 5;
                    Animate(4, 1, 30, 0, 0, 0);
                    WaitForSync(130);
                    StopAnimateSimon2(2, 1);
                }
                else
                {
                    _variableArray[100] = 15;
                    Animate(4, 1, 130, 0, 0, 0);
                    WaitForSync(130);
                    StopAnimate(1);
                }
            }
        }

        protected void StopAnimate(ushort a)
        {
            ushort b = To16Wrapper(a);
            _videoLockOut |= 0x8000;
            var data = new byte[2];
            data.WriteUInt16(0, b);
            _vcPtr = data;
            vc60_stopAnimation();
            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        private ushort To16Wrapper(uint value)
        {
            return ScummHelper.SwapBytes((ushort) value);
        }

        protected ushort ReadUint16Wrapper(BytePtr src)
        {
            return src.ToUInt16BigEndian();
        }

        private void UnlightMenuStrip()
        {
            throw new NotImplementedException();
        }

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

        private void DisplayBoxStars()
        {
            throw new NotImplementedException();
        }

        private void ClearMenuStrip()
        {
            throw new NotImplementedException();
        }

        private void WaitWindow(WindowBlock textWindow)
        {
            throw new NotImplementedException();
        }

        private int GetFeebleFontSize(byte chr)
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO) &&
                chr == 32)
            {
                return 4;
            }
            if (_language == Language.PL_POL)
            {
                if (GetExtra() == "4CD")
                    return polish4CD_feebleFontSize[chr - 32];
                return polish2CD_feebleFontSize[chr - 32];
            }
            return feebleFontSize[chr - 32];
        }

        private string GetExtra()
        {
            return _gd.ADGameDescription.extra;
        }

        private void SendWindow(uint a)
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ||
                _textWindow != _windowArray[0])
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                {
                    if ((_textWindow.flags & 1) == 0)
                    {
                        HaltAnimation();
                    }
                }

                WindowPutChar(_textWindow, (byte) a);
            }
        }

        private void WindowDrawChar(WindowBlock window, int p1, int p2, byte p3)
        {
            throw new NotImplementedException();
        }

        private void ClearWindow(WindowBlock window)
        {
            if ((window.flags & 0x10) != 0)
                RestoreWindow(window);
            else
                ColorWindow(window);

            window.textColumn = 0;
            window.textRow = 0;
            window.textColumnOffset = (ushort) ((GameType == SIMONGameType.GType_ELVIRA2) ? 4 : 0);
            window.textLength = 0;
            window.scrollY = 0;
        }

        private void ColorWindow(WindowBlock window)
        {
            ushort y = (ushort) window.y;
            ushort h = (ushort) (window.height * 8);

            if (GameType == SIMONGameType.GType_ELVIRA2 && window.y == 146)
            {
                if (window.fillColor == 1)
                {
                    _displayPalette[33] = Color.FromRgb(48 * 4, 40 * 4, 32 * 4);
                }
                else
                {
                    _displayPalette[33] = Color.FromRgb(56 * 4, 56 * 4, 40 * 4);
                }

                y--;
                h += 2;

                _paletteFlag = 1;
            }

            ColorBlock(window, (ushort) (window.x * 8), y, (ushort) (window.width * 8), h);
        }

        private void ColorBlock(WindowBlock window, ushort x, ushort y, ushort w, ushort h)
        {
            _videoLockOut |= 0x8000;

            LocksScreen(screen =>
            {
                var dst = screen.GetBasePtr(x, y);

                byte color = window.fillColor;
                if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
                    color = (byte) (color + dst[0] & 0xF0);

                do
                {
                    dst.Data.Set(dst.Offset, color, w);
                    dst += screen.Pitch;
                } while (--h != 0);
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }


        private void DisplayScreen()
        {
            if (_fastFadeInFlag == 0 && _paletteFlag == 1)
            {
                _paletteFlag = 0;
                if (!ScummHelper.ArrayEquals(_displayPalette, 0, _currentPalette, 0, _displayPalette.Length))
                {
                    Array.Copy(_displayPalette, _currentPalette, _displayPalette.Length);
                    OSystem.GraphicsManager.SetPalette(_displayPalette, 0, 256);
                }
            }

            LocksScreen(screen =>
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                {
                    BytePtr src = BackBuf;
                    var dst = screen.Pixels;
                    for (int i = 0; i < _screenHeight; i++)
                    {
                        Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, _screenWidth);
                        src += _backBuf.Pitch;
                        dst += screen.Pitch;
                    }
                    if (_gd.ADGameDescription.gameId != GameIds.GID_DIMP)
                        FillBackFromBackGround(_screenHeight, _screenWidth);
                }
                else
                {
                    if (_window4Flag == 2)
                    {
                        _window4Flag = 0;

                        ushort srcWidth, width, height;
                        var dst = screen.Pixels;

                        var src = _window4BackScn.Pixels;
                        if (_window3Flag == 1)
                        {
                            src = BackGround;
                        }

                        dst += (_moveYMin + _videoWindows[17]) * screen.Pitch;
                        dst += (_videoWindows[16] * 16) + _moveXMin;

                        src += (_videoWindows[18] * 16 * _moveYMin);
                        src += _moveXMin;

                        srcWidth = (ushort) (_videoWindows[18] * 16);

                        width = (ushort) (_moveXMax - _moveXMin);
                        height = (ushort) (_moveYMax - _moveYMin);

                        for (; height > 0; height--)
                        {
                            Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, width);
                            dst += screen.Pitch;
                            src += srcWidth;
                        }

                        _moveXMin = 0xFFFF;
                        _moveYMin = 0xFFFF;
                        _moveXMax = 0;
                        _moveYMax = 0;
                    }

                    if (_window6Flag == 2)
                    {
                        _window6Flag = 0;

                        var src = _window6BackScn.Pixels;
                        var dst = screen.GetBasePtr(0, 51);
                        for (int i = 0; i < 80; i++)
                        {
                            Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, _window6BackScn.Width);
                            dst += screen.Pitch;
                            src += _window6BackScn.Pitch;
                        }
                    }
                }
            });

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF && _scrollFlag != 0)
            {
                ScrollScreen();
            }

            if (_fastFadeInFlag != 0)
            {
                FastFadeIn();
            }
        }

        private void FastFadeIn()
        {
            if ((_fastFadeInFlag & 0x8000) != 0)
            {
                SlowFadeIn();
            }
            else
            {
                _paletteFlag = 0;
                Array.Copy(_displayPalette, _currentPalette, _displayPalette.Length);
                OSystem.GraphicsManager.SetPalette(_displayPalette, 0, _fastFadeInFlag);
                _fastFadeInFlag = 0;
            }
        }

        private void SlowFadeIn()
        {
            _fastFadeInFlag = (ushort) ((_fastFadeInFlag & ~0x8000) / 3);
            _paletteFlag = 0;

            Array.Clear(_currentPalette, 0, _currentPalette.Length);

            for (var c = 255; c >= 0; c -= 4)
            {
                Ptr<Color> src = _displayPalette;

                for (var p = 0; p < _fastFadeInFlag; p++)
                {
                    if (src.Value.R >= c)
                        _currentPalette[p].R += 4;
                    if (src.Value.G >= c)
                        _currentPalette[p].G += 4;
                    if (src.Value.B >= c)
                        _currentPalette[p].B += 4;
                    src.Offset++;
                }
                OSystem.GraphicsManager.SetPalette(_currentPalette, 0, _fastFadeCount);
                Delay(5);
            }
            _fastFadeInFlag = 0;
        }

        private void FillBackFromBackGround(int screenHeight, int screenWidth)
        {
            throw new NotImplementedException();
        }

        private int GetWindowNum(WindowBlock window)
        {
            int i;

            for (i = 0; i != _windowArray.Length; i++)
                if (_windowArray[i] == window)
                    return i;

            Error("getWindowNum: not found");
            return 0; // for compilers that don't support NORETURN
        }

        private void RunSubroutine101()
        {
            var sub = GetSubroutineByID(101);
            if (sub != null)
                StartSubroutineEx(sub);

            PermitInput();
        }

        private int StartSubroutineEx(Subroutine sub)
        {
            return StartSubroutine(sub);
        }

        protected int StartSubroutine(Subroutine sub)
        {
            int result = -1;
            var sl = new SubroutineLine(sub.Pointer + sub.first);

            var old_code_ptr = _codePtr;
            Subroutine old_currentTable = _currentTable;
            SubroutineLine old_currentLine = _currentLine;
            SubroutineLine old_classLine = _classLine;
            short old_classMask = _classMask;
            short old_classMode1 = _classMode1;
            short old_classMode2 = _classMode2;

            _classLine = null;
            _classMask = 0;
            _classMode1 = 0;
            _classMode2 = 0;

            //            if (DebugMan.isDebugChannelEnabled(kDebugSubroutine))
            //                DumpSubroutine(sub);

            if (++_recursionDepth > 40)
                Error("Recursion error");

            // WORKAROUND: If the game is saved, right after Simon is thrown in the dungeon of Sordid's Fortress of Doom,
            // the saved game fails to load correctly. When loading the saved game, the sequence of Simon waking is started,
            // before the scene is actually reloaded, due to a script bug. We manually add the extra script code from the
            // updated DOS CD release, which fixed this particular script bug.
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                sub.id == 12101)
            {
                const byte bit = 228;
                if ((_bitArrayTwo[bit / 16] & (1 << (bit & 15))) != 0 && (int) ReadVariable(34) == -1)
                {
                    _bitArrayTwo[228 / 16] = (ushort) (_bitArrayTwo[228 / 16] & ~(1 << (bit & 15)));
                    WriteVariable(34, 1);
                }
            }

            _currentTable = sub;
            restart:

            if (HasToQuit)
                return result;

            while (sl.Pointer != sub.Pointer)
            {
                _currentLine = sl;
                if (CheckIfToRunSubroutineLine(sl, sub))
                {
                    _codePtr = sl.Pointer;
                    if (sub.id != 0)
                        _codePtr += 2;
                    else
                        _codePtr += 8;

                    DebugC(DebugLevels.kDebugOpcode, "; {0}", sub.id);
                    result = RunScript();
                    if (result != 0)
                    {
                        break;
                    }
                }
                sl = new SubroutineLine(sub.Pointer + sl.next);
            }

            // WORKAROUND: Feeble walks in the incorrect direction, when looking at the Vent in the Research and Testing area of
            // the Company Central Command Compound. We manually add the extra script code from the updated English 2CD release,
            // which fixed this particular script bug.
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF &&
                _language == Language.EN_ANY)
            {
                if (sub.id == 39125 && ReadVariable(84) == 2)
                {
                    WriteVariable(1, 1136);
                    WriteVariable(2, 346);
                }
                if (sub.id == 39126 && ReadVariable(84) == 2)
                {
                    Subroutine tmpSub = GetSubroutineByID(80);
                    if (tmpSub != null)
                    {
                        StartSubroutine(tmpSub);
                    }
                }
            }

            if (_classMode1 != 0)
            {
                _subjectItem = NextInByClass(_subjectItem, _classMask);
                if (_subjectItem == null)
                {
                    _classMode1 = 0;
                }
                else
                {
                    Delay(0);
                    sl = _classLine; /* Rescanner */
                    goto restart;
                }
            }
            if (_classMode2 != 0)
            {
                _objectItem = NextInByClass(_objectItem, _classMask);
                if (_objectItem == null)
                {
                    _classMode2 = 0;
                }
                else
                {
                    Delay(0);
                    sl = _classLine; /* Rescanner */
                    goto restart;
                }
            }

            /* result -10 means restart subroutine */
            if (result == -10)
            {
                Delay(0);
                sl = new SubroutineLine(sub.Pointer + sub.first);
                goto restart;
            }

            _codePtr = old_code_ptr;
            _currentLine = old_currentLine;
            _currentTable = old_currentTable;
            _classLine = old_classLine;
            _classMask = old_classMask;
            _classMode1 = old_classMode2;
            _classMode2 = old_classMode1;
            _findNextPtr = null;

            _recursionDepth--;
            return result;
        }

        private int RunScript()
        {
            bool flag;

            if (HasToQuit)
                return 1;

            do
            {
//                if (DebugMan.isDebugChannelEnabled(kDebugOpcode))
//                    dumpOpcode(_codePtr);

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                {
                    _opcode = (ushort) GetVarOrWord();
                    if (_opcode == 10000)
                        return 0;
                }
                else
                {
                    _opcode = GetByte();
                    if (_opcode == 0xFF)
                        return 0;
                }

                if (_runScriptReturn1)
                    return 1;

                /* Invert condition? */
                flag = false;
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                {
                    if (_opcode == 203)
                    {
                        flag = true;
                        _opcode = (ushort) GetVarOrWord();
                        if (_opcode == 10000)
                            return 0;
                    }
                }
                else
                {
                    if (_opcode == 0)
                    {
                        flag = true;
                        _opcode = GetByte();
                        if (_opcode == 0xFF)
                            return 0;
                    }
                }

                SetScriptCondition(true);
                SetScriptReturn(0);

                if (_opcode > _numOpcodes)
                    Error("Invalid opcode '{0}' encountered", _opcode);

                ExecuteOpcode(_opcode);
            } while (GetScriptCondition() != flag && GetScriptReturn() == 0 && !HasToQuit);

            return HasToQuit ? 1 : GetScriptReturn();
        }

        private void SetScriptReturn(int ret)
        {
            _runScriptReturn[_recursionDepth] = (short) ret;
        }

        private int GetScriptReturn()
        {
            return _runScriptReturn[_recursionDepth];
        }

        private bool GetScriptCondition()
        {
            return _runScriptCondition[_recursionDepth];
        }

        private byte GetByte()
        {
            var value = _codePtr.Value;
            _codePtr.Offset++;
            return value;
        }

        private Item NextInByClass(Item i, short m)
        {
            i = _findNextPtr;
            while (i != null)
            {
                if ((i.classFlags & m) != 0)
                {
                    _findNextPtr = DerefItem(i.next);
                    return i;
                }
                if (m == 0)
                {
                    _findNextPtr = DerefItem(i.next);
                    return i;
                }
                i = DerefItem(i.next);
            }
            return null;
        }

        private bool CheckIfToRunSubroutineLine(SubroutineLine sl, Subroutine sub)
        {
            if (sub.id != 0)
                return true;

            if (sl.verb != -1 && sl.verb != _scriptVerb &&
                (sl.verb != -2 || _scriptVerb != -1))
                return false;

            if (sl.noun1 != -1 && sl.noun1 != _scriptNoun1 &&
                (sl.noun1 != -2 || _scriptNoun1 != -1))
                return false;

            if (sl.noun2 != -1 && sl.noun2 != _scriptNoun2 &&
                (sl.noun2 != -2 || _scriptNoun2 != -1))
                return false;

            return true;
        }

        protected void WriteVariable(ushort variable, ushort contents)
        {
            if (variable >= _numVars)
                Error("writeVariable: Variable {0} out of range", variable);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF && GetBitFlag(83))
                _variableArray2[variable] = (short) contents;
            else
                _variableArray[variable] = (short) contents;
        }

        protected uint ReadVariable(ushort variable)
        {
            if (variable >= _numVars)
                Error("readVariable: Variable {0} out of range", variable);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                return (ushort) _variableArray[variable];
            }
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
            {
                if (GetBitFlag(83))
                    return (ushort) _variableArray2[variable];
                return (ushort) _variableArray[variable];
            }
            return (uint) _variableArray[variable];
        }

        protected Subroutine GetSubroutineByID(uint subroutineId)
        {
            Subroutine cur;

            for (cur = _subroutineList; cur != null; cur = cur.next)
            {
                if (cur.id == subroutineId)
                    return cur;
            }

            if (LoadXTablesIntoMem((ushort) subroutineId))
            {
                for (cur = _subroutineList; cur != null; cur = cur.next)
                {
                    if (cur.id == subroutineId)
                        return cur;
                }
            }

            if (LoadTablesIntoMem((ushort) subroutineId))
            {
                for (cur = _subroutineList; cur != null; cur = cur.next)
                {
                    System.Diagnostics.Debug.WriteLine($"cur.id: {cur.id}");
                    if (cur.id == subroutineId)
                        return cur;
                }
            }

            Debug(0, "getSubroutineByID: subroutine {0} not found", subroutineId);
            return null;
        }

        private bool LoadXTablesIntoMem(ushort subrId)
        {
            int p = 0;
            char[] filename = new char[30];

            if (_xtblList == null)
                return false;

            while (_xtblList[p] != 0)
            {
                int i;
                for (i = 0; _xtblList[p] != 0; p++, i++)
                    filename[i] = (char) _xtblList[p];
                filename[i] = '\0';
                p++;

                for (;;)
                {
                    uint min_num = _xtblList.ToUInt16BigEndian(p);
                    p += 2;

                    if (min_num == 0)
                        break;

                    uint max_num = _xtblList.ToUInt16BigEndian(p);
                    p += 2;

                    if (subrId >= min_num && subrId <= max_num)
                    {
                        _subroutineList = _xsubroutineListOrg;
                        _tablesHeapPtr = _xtablesHeapPtrOrg;
                        _tablesHeapCurPos = _xtablesHeapCurPosOrg;
                        _stringIdLocalMin = 1;
                        _stringIdLocalMax = 0;

                        var @in = OpenTablesFile(new string(filename));
                        ReadSubroutineBlock(@in);
                        CloseTablesFile(@in);

                        AlignTableMem();

                        _subroutineListOrg = _subroutineList;
                        _tablesHeapPtrOrg = _tablesHeapPtr;
                        _tablesHeapCurPosOrg = _tablesHeapCurPos;
                        _tablesheapPtrNew = _tablesHeapPtr;
                        _tablesHeapCurPosNew = _tablesHeapCurPos;

                        return true;
                    }
                }
            }

            Debug(1, "loadXTablesIntoMem: didn't find {0}", subrId);
            return false;
        }

        private Stream OpenTablesFileSimon1(string filename)
        {
            var @in = OpenFileRead(filename);
            if (@in == null)
                Error("openTablesFile: Can't open '{0}'", filename);
            return @in;
        }

        private Stream OpenTablesFileGme(string filename)
        {
            var res = int.Parse(filename.Substring(0, 6)) + _tableIndexBase - 1;
            var offs = _gameOffsetsPtr[res];

            _gameFile.Seek(offs, SeekOrigin.Begin);
            return _gameFile;
        }

        private void PlayMusic(int i, int i1)
        {
            // TODO: vs: PlayMusic
        }

        private void AnimateSprites()
        {
            Ptr<VgaSprite> vsp;
            Ptr<VgaPointersEntry> vpe;

            if (_copyScnFlag != 0)
            {
                _copyScnFlag--;
                _vgaSpriteChanged++;
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
                byte var = (byte) (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ? 293 : 71);
                if (_wallOn != 0 && _variableArray[var] == 0)
                {
                    _wallOn--;

                    var state = new VC10_state();
                    state.srcPtr = BackGround + 3 * _backGroundBuf.Pitch + 3 * 16;
                    state.height = state.draw_height = 127;
                    state.width = state.draw_width = 14;
                    state.y = 0;
                    state.x = 0;
                    state.palette = 0;
                    state.paletteMod = 0;
                    state.flags = DrawFlags.kDFNonTrans;

                    _windowNum = 4;

                    _backFlag = true;
                    DrawImage(state);
                    _backFlag = false;

                    _vgaSpriteChanged++;
                }
            }

            if (_scrollFlag == 0 && _vgaSpriteChanged == 0)
            {
                return;
            }

            _vgaSpriteChanged = 0;

            if (_paletteFlag == 2)
                _paletteFlag = 1;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 && _scrollFlag != 0)
            {
                ScrollScreen();
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                DirtyClips();
            }

            RestoreBackGround();

            vsp = new Ptr<VgaSprite>(_vgaSprites);
            for (; vsp.Value.id != 0; vsp.Offset++)
            {
                if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                    (vsp.Value.windowNum & 0x8000) == 0)
                {
                    continue;
                }

                vsp.Value.windowNum = (ushort) (vsp.Value.windowNum & ~0x8000);

                vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, vsp.Value.zoneNum);
                _curVgaFile1 = vpe.Value.vgaFile1;
                _curVgaFile2 = vpe.Value.vgaFile2;
                _curSfxFile = vpe.Value.sfxFile;
                _windowNum = vsp.Value.windowNum;
                _vgaCurSpriteId = vsp.Value.id;

                SaveBackGround(vsp);

                DrawImageInit(vsp.Value.image, vsp.Value.palette, vsp.Value.x, vsp.Value.y, vsp.Value.flags);
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && _variableArray[293] != 0)
            {
                // Used by the Fire Wall and Ice Wall spells
                Debug(0, "Using special wall");

                byte color, h, len;
                var dst = _window4BackScn.Pixels;

                color = (byte) ((_variableArray[293] & 1) != 0 ? 13 : 15);
                _wallOn = 2;

                h = 127;
                while (h != 0)
                {
                    len = 112;
                    while (len-- != 0)
                    {
                        dst.Value = color;
                        dst.Offset += 2;
                    }

                    h--;
                    if (h == 0)
                        break;

                    len = 112;
                    while (len-- != 0)
                    {
                        dst.Offset++;
                        dst.Value = color;
                        dst.Offset++;
                    }
                    h--;
                }

                _window4Flag = 1;
                SetMoveRect(0, 0, 224, 127);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 &&
                     (_variableArray[71] & 2) != 0)
            {
                // Used by the Unholy Barrier spell
                byte color, h;
                var dst = _window4BackScn.Pixels;

                color = 1;
                _wallOn = 2;

                h = (byte) 43;
                while (h != 0)
                {
                    var len = (byte) 56;
                    while (len-- != 0)
                    {
                        dst.Value = color;
                        dst += 4;
                    }

                    h--;
                    if (h == 0)
                        break;

                    dst += 448;

                    len = 56;
                    while (len-- != 0)
                    {
                        dst += 2;
                        dst.Value = color;
                        dst += 2;
                    }
                    dst += 448;
                    h--;
                }

                _window4Flag = 1;
                SetMoveRect(0, 0, 224, 127);
            }

            if (_window6Flag == 1)
                _window6Flag++;

            if (_window4Flag == 1)
                _window4Flag++;

            _displayFlag++;
        }

        private uint ReadUint32Wrapper(BytePtr src)
        {
            return src.ToUInt32BigEndian();
        }

        private BytePtr ConvertImage(VC10_state state, bool hasFlag)
        {
            throw new NotImplementedException();
        }

        private void HorizontalScroll(VC10_state state)
        {
            throw new NotImplementedException();
        }

        private void VerticalScroll(VC10_state state)
        {
            throw new NotImplementedException();
        }

        private void SaveBackGround(Ptr<VgaSprite> vsp)
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && _gd.Platform == Platform.AtariST &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
            {
                return;
            }

            if (((vsp.Value.flags & (ushort) DrawFlags.kDFSkipStoreBG) != 0) || vsp.Value.image == 0)
                return;

            Ptr<AnimTable> animTable = _screenAnim1;

            while (animTable.Value.srcPtr != BytePtr.Null)
                animTable.Offset++;

            var ptr = _curVgaFile2 + vsp.Value.image * 8;
            short x = (short) (vsp.Value.x - _scrollX);
            short y = (short) (vsp.Value.y - _scrollY);

            if (_window3Flag == 1)
            {
                animTable.Value.srcPtr = _window4BackScn.Pixels;
            }
            else
            {
                int xoffs = (_videoWindows[vsp.Value.windowNum * 4 + 0] * 2 + x) * 8;
                int yoffs = (_videoWindows[vsp.Value.windowNum * 4 + 1] + y);
                animTable.Value.srcPtr = BackGround + yoffs * _backGroundBuf.Pitch + xoffs;
            }

            animTable.Value.x = x;
            animTable.Value.y = y;

            animTable.Value.width = (ushort) (ptr.ToUInt16BigEndian(6) / 16);
            if ((vsp.Value.flags & 0x40) != 0)
            {
                animTable.Value.width++;
            }

            animTable.Value.height = ptr[5];
            animTable.Value.windowNum = vsp.Value.windowNum;
            animTable.Value.id = vsp.Value.id;
            animTable.Value.zoneNum = vsp.Value.zoneNum;

            animTable.Offset++;
            animTable.Value.srcPtr = BytePtr.Null;
        }

        private void RestoreBackGround()
        {
            int images = 0;

            Ptr<AnimTable> animTable = _screenAnim1;
            while (animTable.Value.srcPtr != BytePtr.Null)
            {
                animTable.Offset++;
                images++;
            }

            while (images-- != 0)
            {
                animTable.Offset--;

                if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                    (animTable.Value.windowNum & 0x8000) == 0)
                {
                    continue;
                }

                _windowNum = (ushort) (animTable.Value.windowNum & ~0x8000);

                VC10_state state = new VC10_state();
                state.srcPtr = animTable.Value.srcPtr;
                state.height = state.draw_height = animTable.Value.height;
                state.width = state.draw_width = animTable.Value.width;
                state.y = animTable.Value.y;
                state.x = animTable.Value.x;
                state.palette = 0;
                state.paletteMod = 0;
                state.flags = DrawFlags.kDFNonTrans;

                _backFlag = true;
                DrawImage(state);

                if (_gd.ADGameDescription.gameType != SIMONGameType.GType_SIMON1 &&
                    _gd.ADGameDescription.gameType != SIMONGameType.GType_SIMON2)
                {
                    animTable.Value.srcPtr = BytePtr.Null;
                }
            }
            _backFlag = false;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                Ptr<AnimTable> animTableTmp;

                animTable = animTableTmp = _screenAnim1;
                while (animTable.Value.srcPtr != BytePtr.Null)
                {
                    if ((animTable.Value.windowNum & 0x8000) == 0)
                    {
                        animTableTmp[0] = new AnimTable(animTable[0]);
                        animTableTmp.Offset++;
                    }
                    animTable.Offset++;
                }
                animTableTmp.Value.srcPtr = BytePtr.Null;
            }
        }

        private void DirtyClips()
        {
            short x, y, w, h;
            restart:
            _newDirtyClip = false;

            Ptr<VgaSprite> vsp = _vgaSprites;
            while (vsp.Value.id != 0)
            {
                if ((vsp.Value.windowNum & 0x8000) != 0)
                {
                    x = vsp.Value.x;
                    y = vsp.Value.y;
                    w = 1;
                    h = 1;

                    if (vsp.Value.image != 0)
                    {
                        var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, vsp.Value.zoneNum);
                        BytePtr ptr = vpe.Value.vgaFile2 + vsp.Value.image * 8;
                        w = (short) (ptr.ToUInt16BigEndian(6) / 8);
                        h = ptr[5];
                    }

                    DirtyClipCheck(x, y, w, h);
                }
                vsp.Offset++;
            }

            Ptr<AnimTable> animTable = _screenAnim1;
            while (animTable.Value.srcPtr != BytePtr.Null)
            {
                if ((animTable.Value.windowNum & 0x8000) != 0)
                {
                    x = (short) (animTable.Value.x + _scrollX);
                    y = animTable.Value.y;
                    w = (short) (animTable.Value.width * 2);
                    h = (short) animTable.Value.height;

                    DirtyClipCheck(x, y, w, h);
                }
                animTable.Offset++;
            }

            if (_newDirtyClip)
                goto restart;
        }

        private void DirtyClipCheck(short x, short y, short w, short h)
        {
            short width, height, tmp;

            Ptr<VgaSprite> vsp = _vgaSprites;
            for (; vsp.Value.id != 0; vsp.Offset++)
            {
                if ((vsp.Value.windowNum & 0x8000) != 0)
                    continue;

                if (vsp.Value.image == 0)
                    continue;

                var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, vsp.Value.zoneNum);
                BytePtr ptr = vpe.Value.vgaFile2 + vsp.Value.image * 8;
                width = (short) (ptr.ToUInt32BigEndian(6) / 8);
                height = ptr[5];

                tmp = vsp.Value.x;
                if (tmp >= x)
                {
                    tmp -= w;
                    if (tmp >= x)
                        continue;
                }
                else
                {
                    tmp += width;
                    if (tmp < x)
                        continue;
                }

                tmp = vsp.Value.y;
                if (tmp >= y)
                {
                    tmp -= h;
                    if (tmp >= y)
                        continue;
                }
                else
                {
                    tmp += height;
                    if (tmp < y)
                        continue;
                }

                vsp.Value.windowNum |= 0x8000;
                _newDirtyClip = true;
            }

            Ptr<AnimTable> animTable = _screenAnim1;
            for (; animTable.Value.srcPtr != BytePtr.Null; animTable.Offset++)
            {
                if ((animTable.Value.windowNum & 0x8000) != 0)
                    continue;

                width = (short) (animTable.Value.width * 2);
                height = (short) animTable.Value.height;

                tmp = (short) (animTable.Value.x + _scrollX);
                if (tmp >= x)
                {
                    tmp -= w;
                    if (tmp >= x)
                        continue;
                }
                else
                {
                    tmp += width;
                    if (tmp < x)
                        continue;
                }

                tmp = animTable.Value.y;
                if (tmp >= y)
                {
                    tmp -= h;
                    if (tmp >= y)
                        continue;
                }
                else
                {
                    tmp += height;
                    if (tmp < y)
                        continue;
                }

                animTable.Value.windowNum |= 0x8000;
                _newDirtyClip = true;
            }
        }

        private void DrawVertImageCompressed(VC10_state state)
        {
            System.Diagnostics.Debug.Assert(state.flags.HasFlag(DrawFlags.kDFCompressed));

            state.x_skip *= 4; /* reached */

            state.dl = state.width;
            state.dh = state.height;

            vc10_skip_cols(state);

            var dstPtr = state.surf_addr;
            if (!state.flags.HasFlag(DrawFlags.kDFNonTrans) && ((ushort) (state.flags) & 0x40) != 0)
            {
                /* reached */
                dstPtr += (int) VcReadVar(252);
            }
            var w = 0;
            do
            {
                byte color;

                var src = vc10_depackColumn(state);
                var dst = dstPtr;

                var h = 0;
                if (state.flags.HasFlag(DrawFlags.kDFNonTrans))
                {
                    do
                    {
                        byte colors = src.Value;
                        color = (byte) (colors / 16);
                        dst[0] = (byte) (color | state.palette);
                        color = (byte) (colors & 15);
                        dst[1] = (byte) (color | state.palette);
                        dst += (int) state.surf_pitch;
                        src.Offset++;
                    } while (++h != state.draw_height);
                }
                else
                {
                    do
                    {
                        byte colors = src.Value;
                        color = (byte) (colors / 16);
                        if (color != 0)
                            dst[0] = (byte) (color | state.palette);
                        color = (byte) (colors & 15);
                        if (color != 0)
                            dst[1] = (byte) (color | state.palette);
                        dst += (int) state.surf_pitch;
                        src.Offset++;
                    } while (++h != state.draw_height);
                }
                dstPtr += 2;
            } while (++w != state.draw_width);
        }

        protected BytePtr vc10_depackColumn(VC10_state vs)
        {
            sbyte a = vs.depack_cont;
            var src = vs.srcPtr;
            var dst = new BytePtr(vs.depack_dest);
            ushort dh = vs.dh;

            if (a == -0x80)
            {
                a = (sbyte) src.Value;
                src.Offset++;
            }

            for (;;)
            {
                if (a >= 0)
                {
                    var color = src.Value;
                    src.Offset++;
                    do
                    {
                        dst.Value = color;
                        dst.Offset++;
                        if (--dh == 0)
                        {
                            if (--a < 0)
                                a = -0x80;
                            else
                                src.Offset--;
                            goto get_out;
                        }
                    } while (--a >= 0);
                }
                else
                {
                    do
                    {
                        dst.Value = src.Value;
                        src.Offset++;
                        dst.Offset++;
                        if (--dh == 0)
                        {
                            if (++a == 0)
                                a = -0x80;
                            goto get_out;
                        }
                    } while (++a != 0);
                }
                a = (sbyte) src.Value;
                src.Offset++;
            }

            get_out:
            vs.srcPtr = src;
            vs.depack_cont = a;
            return new BytePtr(vs.depack_dest, vs.y_skip);
        }

        protected void vc10_skip_cols(VC10_state vs)
        {
            while (vs.x_skip != 0)
            {
                vc10_depackColumn(vs);
                vs.x_skip--;
            }
        }

        private void DrawVertImageUncompressed(VC10_state state)
        {
            System.Diagnostics.Debug.Assert(!state.flags.HasFlag(DrawFlags.kDFCompressed));

            var src = state.srcPtr + (state.width * state.y_skip) * 8;
            var dst = state.surf_addr;
            state.x_skip *= 4;

            do
            {
                for (var count = 0; count != state.draw_width; count++)
                {
                    byte color;
                    color = (byte) (src[count + state.x_skip] / 16 + state.paletteMod);
                    if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                        dst[count * 2] = (byte) (color | state.palette);
                    color = (byte) ((src[count + state.x_skip] & 15) + state.paletteMod);
                    if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                        dst[count * 2 + 1] = (byte) (color | state.palette);
                }
                dst.Offset += (int) state.surf_pitch;
                src.Offset += state.width * 8;
            } while (--state.draw_height != 0);
        }

        private void ScrollScreen()
        {
            throw new NotImplementedException();
        }

        private void HandleMouseMoved()
        {
// TODO: vs: HandleMouseMoved()
        }

        private void SetWindowImage(ushort mode, ushort vgaSpriteId, bool specialCase = false)
        {
            ushort updateWindow;

            _windowNum = updateWindow = mode;
            _videoLockOut |= 0x20;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                vc27_resetSprite();
            }
            else if (!specialCase)
            {
                Ptr<VgaTimerEntry> vte = _vgaTimerList;
                while (vte.Value.type != EventType.ANIMATE_INT)
                    vte.Offset++;

                vte.Value.delay = 2;
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
            {
                Ptr<AnimTable> animTable = _screenAnim1;
                while (animTable.Value.srcPtr != BytePtr.Null)
                {
                    animTable.Value.srcPtr = BytePtr.Null;
                    animTable.Offset++;
                }
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
            {
                _scrollX = 0;
                _scrollY = 0;
                _scrollXMax = 0;
                _scrollYMax = 0;
                _scrollCount = 0;
                _scrollFlag = 0;
                _scrollHeight = 134;
                _variableArrayPtr = _variableArray;
                if (_variableArray[34] >= 0)
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                        _variableArray[250] = 0;
                    _variableArray[251] = 0;
                }
            }

            SetImage(vgaSpriteId, specialCase);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                FillBackGroundFromBack();
                _syncFlag2 = true;
            }
            else
            {
                _copyScnFlag = 2;
                _vgaSpriteChanged++;

                if (_window3Flag == 1)
                {
                    ClearVideoBackGround(3, 0);
                    _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                    return;
                }

                int xoffs = _videoWindows[updateWindow * 4 + 0] * 16;
                int yoffs = (int) (_videoWindows[updateWindow * 4 + 1]);
                uint width = (uint) (_videoWindows[updateWindow * 4 + 2] * 16);
                uint height = (uint) (_videoWindows[updateWindow * 4 + 3]);

                var screen = OSystem.GraphicsManager.Capture();
                var dst = _backGroundBuf.GetBasePtr(xoffs, yoffs);
                BytePtr src = BytePtr.Null;
                int srcWidth = 0;

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    src = _window4BackScn.GetBasePtr(xoffs, yoffs);
                    srcWidth = 320;
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1
                         && _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
                {
                    // The DOS Floppy demo was based off Waxworks engine
                    if (updateWindow == 4 || updateWindow >= 10)
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow == 3 || updateWindow == 9)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        UnlockScreen(screen);
                        _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                        return;
                    }
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                {
                    if (updateWindow == 4)
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow >= 10)
                    {
                        src = _window4BackScn.GetBasePtr(xoffs, yoffs);
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow == 0)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        UnlockScreen(screen);
                        _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                        return;
                    }
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                {
                    if (updateWindow == 4 || updateWindow >= 10)
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow == 3 || updateWindow == 9)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        UnlockScreen(screen);
                        _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                        return;
                    }
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (updateWindow == 4 || updateWindow >= 10)
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow == 3)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        UnlockScreen(screen);
                        _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                        return;
                    }
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                {
                    if (updateWindow == 6)
                    {
                        _window6Flag = 1;
                        src = _window6BackScn.Pixels;
                        srcWidth = 48;
                    }
                    else if (updateWindow == 2 || updateWindow == 3)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                }
                else
                {
                    src = screen.GetBasePtr(xoffs, yoffs);
                    srcWidth = screen.Pitch;
                }

                _boxStarHeight = (byte) height;

                for (; height > 0; height--)
                {
                    Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, (int) width);
                    dst.Offset += _backGroundBuf.Pitch;
                    src.Offset += srcWidth;
                }

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN &&
                    !_wiped && !specialCase)
                {
                    byte color = (byte) ((_gd.Platform == Platform.DOS) ? 7 : 15);
                    dst = screen.GetBasePtr(48, 0);
                    dst.Data.Set(dst.Offset, (byte) color, 224);

                    dst = screen.GetBasePtr(48, 132);
                    dst.Data.Set(dst.Offset, (byte) color, 224);
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                         updateWindow == 3 && _bottomPalette)
                {
                    dst = screen.GetBasePtr(0, 133);

                    for (int h = 0; h < 67; h++)
                    {
                        for (int w = 0; w < _screenWidth; w++)
                            dst[w] += 0x10;
                        dst += screen.Pitch;
                    }
                }

                UnlockScreen(screen);
            }

            _videoLockOut = (ushort) (_videoLockOut & ~0x20);
        }

        private void FillBackGroundFromBack()
        {
            var src = BackBuf;
            var dst = BackGround;
            for (int i = 0; i < _screenHeight; i++)
            {
                Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, _screenWidth);
                src.Offset += _backBuf.Pitch;
                dst.Offset += _backGroundBuf.Pitch;
            }
        }

        private void MouseOff()
        {
            _mouseHideCount++;
        }

        private void LoadMenuFile()
        {
            throw new NotImplementedException();
        }

        private void LoadIconFile()
        {
            var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_ICONFILE));
            int srcSize;

            if (@in == null)
                Error("Can't open icons file '{0}'", GetFileName(GameFileTypes.GAME_ICONFILE));

            srcSize = (int) @in.Length;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW &&
                _gd.Platform == Platform.Amiga)
            {
                var srcBuf = new byte[srcSize];
                @in.Read(srcBuf, 0, srcSize);

                int dstSize = srcBuf.ToInt32BigEndian(srcSize - 4);
                _iconFilePtr = new byte[dstSize];
                if (_iconFilePtr == null)
                    Error("Out of icon memory");

                DecrunchFile(srcBuf, _iconFilePtr, srcSize);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN &&
                     _gd.Platform == Platform.AtariST)
            {
// The icon data is hard coded in the program file.
                _iconFilePtr = new byte[15038];
                if (_iconFilePtr == null)
                    Error("Out of icon memory");

                @in.Seek(48414, SeekOrigin.Begin);
                @in.Read(_iconFilePtr, 0, 15038);
            }
            else
            {
                _iconFilePtr = new byte[srcSize];
                if (_iconFilePtr == null)
                    Error("Out of icon memory");

                @in.Read(_iconFilePtr, 0, srcSize);
            }
            @in.Dispose();
        }

        private void LoadIconData()
        {
            LoadZone(8);
            var vpe = _vgaBufferPointers[8];

            var src = new BytePtr(vpe.vgaFile2, vpe.vgaFile2.ToInt32(8));

            _iconFilePtr = new byte[43 * 336];
            if (_iconFilePtr == null)
                Error("Out of icon memory");

            Array.Copy(src.Data, src.Offset, _iconFilePtr, 0, 43 * 336);
            UnfreezeBottom();
        }

        private void CHECK_BOUNDS<T>(int x, T[] y)
        {
            System.Diagnostics.Debug.Assert(x < y.Length);
        }

        private void LoadZone(ushort zoneNum, bool useError = true)
        {
            Ptr<VgaPointersEntry> vpe;

            CHECK_BOUNDS(zoneNum, _vgaBufferPointers);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
            {
// Only a single zone is used in Personal Nightmare
                vpe = _vgaBufferPointers;
                vc27_resetSprite();
                _vgaMemPtr = _vgaMemBase;
            }
            else
            {
                vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, zoneNum);
                if (vpe.Value.vgaFile1 != BytePtr.Null)
                    return;
            }

// Loading order is important due to resource management

            if (_gd.Platform == Platform.Amiga &&
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW &&
                zoneTable[zoneNum] == 3)
            {
                byte num = (byte) ((zoneNum >= 85) ? 94 : 18);
                LoadVGAVideoFile(num, 2, useError);
            }
            else
            {
                LoadVGAVideoFile(zoneNum, 2, useError);
            }
            vpe.Value.vgaFile2 = _block;
            vpe.Value.vgaFile2End = _blockEnd;

            LoadVGAVideoFile(zoneNum, 1, useError);
            vpe.Value.vgaFile1 = _block;
            vpe.Value.vgaFile1End = _blockEnd;

            vpe.Value.sfxFile = BytePtr.Null;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
// A singe sound file is used for Amiga and AtariST versions
                if (LoadVGASoundFile(1, 3))
                {
                    vpe.Value.sfxFile = _block;
                    vpe.Value.sfxFileEnd = _blockEnd;
                }
            }
            else if (!_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_ZLIBCOMP))
            {
                if (LoadVGASoundFile(zoneNum, 3))
                {
                    vpe.Value.sfxFile = _block;
                    vpe.Value.sfxFileEnd = _blockEnd;
                }
            }
        }

        private bool LoadVGASoundFile(ushort id, byte type)
        {
            Stream @in;
            string filename;
            BytePtr dst;
            int srcSize, dstSize;

            if (_gd.Platform == Platform.Amiga || _gd.Platform == Platform.AtariST)
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                    _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO) &&
                    _gd.Platform == Platform.Amiga)
                {
                    filename = $"{(char) 48 + id}{type}.out";
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                         _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                {
                    filename = $"{id:D2}{type}.out";
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                {
                    filename = "{(char)id + 48}{type}.in";
                }
                else
                {
                    filename = $"{id:D3}{type}.out";
                }
            }
            else
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                {
                    if (elvira1_soundTable[id] == 0)
                        return false;

                    filename = $"{elvira1_soundTable[id]:D2}.SND";
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                         _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                {
                    filename = $"{id:D2}{type}.VGA";
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                {
                    filename = $"{(char) 48 + id}{type}.out";
                }
                else
                {
                    filename = $"{id:D3}{type}.VGA";
                }
            }

            @in = OpenFileRead(filename);
            if (@in == null || @in.Length == 0)
            {
                return false;
            }

            dstSize = srcSize = (int) @in.Length;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_CRUNCHED))
            {
                Stack<uint> data = new Stack<uint>();
                BytePtr dataOut;
                int dataOutSize = 0;

                var br = new BinaryReader(@in);
                for (var i = 0; i < srcSize / 4; ++i)
                    data.Push(br.ReadUInt32BigEndian());

                DecompressPN(data, out dataOut, ref dataOutSize);
                dst = AllocBlock(dataOutSize);
                Array.Copy(dataOut.Data, dataOut.Offset, dst.Data, dst.Offset, dataOutSize);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                     _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
            {
                var srcBuffer = new byte[srcSize];
                if (@in.Read(srcBuffer, 0, srcSize) != srcSize)
                    Error("loadVGASoundFile: Read failed");

                dstSize = srcBuffer.ToInt32BigEndian(srcSize - 4);
                dst = AllocBlock(dstSize);
                DecrunchFile(srcBuffer, dst, srcSize);
            }
            else
            {
                dst = AllocBlock(dstSize);
                if (@in.Read(dst.Data, dst.Offset, dstSize) != dstSize)
                    Error("loadVGASoundFile: Read failed");
            }
            @in.Dispose();

            return true;
        }

        private void LoadVGAVideoFile(ushort id, byte type, bool useError)
        {
            Stream @in;
            BytePtr dst;
            string filename;
            int file, offs, srcSize, dstSize;
            int extraBuffer = 0;

            if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                 _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                id == 2 && type == 2)
            {
// WORKAROUND: For the extra long strings in foreign languages
// Allocate more space for text to cope with foreign languages that use
// up more space than English. I hope 6400 bytes are enough. This number
// is base on: 2 (lines) * 320 (screen width) * 10 (textheight) -- olki
                extraBuffer += 6400;
            }

            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_ZLIBCOMP))
            {
                LoadOffsets(GetFileName(GameFileTypes.GAME_GFXIDXFILE), id * 3 + type, out file, out offs,
                    out srcSize, out dstSize);

                if (_gd.Platform == Platform.Amiga)
                    filename = $"GFX{file}.VGA";
                else
                    filename = "graphics.vga";

                dst = AllocBlock(dstSize + extraBuffer);
                DecompressData(filename, dst, offs, srcSize, dstSize);
            }
            else if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_OLD_BUNDLE))
            {
                if (_gd.Platform == Platform.Acorn)
                {
                    filename = $"{id:D3}{type}.DAT";
                }
                else if (_gd.Platform == Platform.Amiga || _gd.Platform == Platform.AtariST)
                {
                    if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
                    {
                        filename = $"{id:D3}{type}.out";
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                             _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
                    {
                        if (_gd.Platform == Platform.AtariST)
                            filename = $"{id:D2}{type}.out";
                        else
                            filename = $"{(char) (48 + id)}{type}.out";
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                             _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                    {
                        filename = $"{id:D2}{type}.pkd";
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                    {
                        filename = $"{(char) (48 + id)}{type}.in";
                    }
                    else
                    {
                        filename = $"{id:D3}{type}.pkd";
                    }
                }
                else
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                        _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                        _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    {
                        filename = $"{id:D2}{type}.VGA";
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                    {
                        filename = $"{(char) (48 + id)}{type}.out";
                    }
                    else
                    {
                        filename = $"{id:D3}{type}.VGA";
                    }
                }

                @in = OpenFileRead(filename);
                if (@in == null)
                {
                    if (useError)
                        Error("loadVGAVideoFile: Can't load {0}", filename);

                    _block = _blockEnd = BytePtr.Null;
                    return;
                }

                dstSize = srcSize = (int) @in.Length;
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN && _gd.Platform == Platform.DOS && id == 17 &&
                    type == 2)
                {
// The A2.out file isn't compressed in PC version of Personal Nightmare
                    dst = AllocBlock(dstSize + extraBuffer);
                    if (@in.Read(dst.Data, dst.Offset, dstSize) != dstSize)
                        Error("loadVGAVideoFile: Read failed");
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN &&
                         _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_CRUNCHED))
                {
                    var data = new Stack<uint>();
                    BytePtr dataOut;
                    int dataOutSize = 0;

                    var br = new BinaryReader(@in);
                    for (int i = 0; i < srcSize / 4; ++i)
                    {
                        uint dataVal = br.ReadUInt32BigEndian();
// Correct incorrect byte, in corrupt 72.out file, included in some PC versions.
                        if (dataVal == 168042714)
                            data.Push(168050906);
                        else
                            data.Push(dataVal);
                    }

                    DecompressPN(data, out dataOut, ref dataOutSize);
                    dst = AllocBlock(dataOutSize + extraBuffer);
                    Array.Copy(dataOut.Data, dataOut.Offset, dst.Data, dst.Offset, dataOutSize);
                }
                else if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_CRUNCHED))
                {
                    var srcBuffer = new byte[srcSize];
                    if (@in.Read(srcBuffer, 0, srcSize) != srcSize)
                        Error("loadVGAVideoFile: Read failed");

                    dstSize = srcBuffer.ToInt32BigEndian(srcSize - 4);
                    dst = AllocBlock(dstSize + extraBuffer);
                    DecrunchFile(srcBuffer, dst, srcSize);
                }
                else
                {
                    dst = AllocBlock(dstSize + extraBuffer);
                    if (@in.Read(dst.Data, dst.Offset, dstSize) != dstSize)
                        Error("loadVGAVideoFile: Read failed");
                }
            }
            else
            {
                id = (ushort) (id * 2 + (type - 1));
                offs = (int) _gameOffsetsPtr[id];
                dstSize = (int) (_gameOffsetsPtr[id + 1] - offs);

                if (dstSize == 0)
                {
                    if (useError)
                        Error("loadVGAVideoFile: Can't load id {0} type {1}", id, type);

                    _block = _blockEnd = BytePtr.Null;
                    return;
                }

                dst = AllocBlock(dstSize + extraBuffer);
                ReadGameFile(dst, offs, dstSize);
            }
        }

        private void ReadGameFile(BytePtr dst, int offs, int size)
        {
            _gameFile.Seek(offs, SeekOrigin.Begin);
            if (_gameFile.Read(dst.Data, dst.Offset, size) != size)
                Error("readGameFile: Read failed ({0},{1})", offs, size);
        }

        private void DecompressPN(Stack<uint> data, out BytePtr dataOut, ref int dataOutSize)
        {
            throw new NotImplementedException();
        }

        private void DecompressData(string filename, BytePtr dst, int offs, int srcSize, int dstSize)
        {
            throw new NotImplementedException();
        }

        private void LoadOffsets(string filename, int number, out int file,
            out int offset, out int srcSize, out int dstSize)
        {
            int offsSize = _gd.Platform == Platform.Amiga ? 16 : 12;

/* read offsets from index */
            var stream = OpenFileRead(filename);
            if (stream == null)
            {
                Error("loadOffsets: Can't load index file '{0}'", filename);
                file = 0;
                offset = 0;
                srcSize = 0;
                dstSize = 0;
                return;
            }

            using (var br = new BinaryReader(stream))
            {
                stream.Seek(number * offsSize, SeekOrigin.Begin);
                offset = br.ReadInt32();
                dstSize = br.ReadInt32();
                srcSize = br.ReadInt32();
                file = br.ReadInt32();
            }
        }

        private BytePtr AllocBlock(int size)
        {
            while (true)
            {
                _block = _vgaMemPtr;
                _blockEnd = _block + size;

                if (_blockEnd >= _vgaMemEnd)
                {
                    _vgaMemPtr = _vgaMemBase;
                }
                else
                {
                    _rejectBlock = false;
                    CheckNoOverWrite();
                    if (_rejectBlock)
                        continue;
                    CheckRunningAnims();
                    if (_rejectBlock)
                        continue;
                    CheckZonePtrs();
                    _vgaMemPtr = _blockEnd;
                    return _block;
                }
            }
        }

        private void CheckZonePtrs()
        {
            foreach (var vpe in _vgaBufferPointers)
            {
                if (((vpe.vgaFile1 < _blockEnd) && (vpe.vgaFile1End > _block)) ||
                    ((vpe.vgaFile2 < _blockEnd) && (vpe.vgaFile2End > _block)) ||
                    ((vpe.sfxFile < _blockEnd) && (vpe.sfxFileEnd > _block)))
                {
                    vpe.vgaFile1 = BytePtr.Null;
                    vpe.vgaFile1End = BytePtr.Null;
                    vpe.vgaFile2 = BytePtr.Null;
                    vpe.vgaFile2End = BytePtr.Null;
                    vpe.sfxFile = BytePtr.Null;
                    vpe.sfxFileEnd = BytePtr.Null;
                }
            }
        }

        private void CheckRunningAnims()
        {
            if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                 _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                (_videoLockOut & 0x20) != 0)
            {
                return;
            }

            for (var vsp = 0; _vgaSprites[vsp].id != 0; vsp++)
            {
                CheckAnims(_vgaSprites[vsp].zoneNum);
                if (_rejectBlock)
                    return;
            }
        }

        private void CheckAnims(uint a)
        {
            var vpe = _vgaBufferPointers[a];

            if (vpe.vgaFile1 < _blockEnd && vpe.vgaFile1End > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.vgaFile1End;
            }
            else if (vpe.vgaFile2 < _blockEnd && vpe.vgaFile2End > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.vgaFile2End;
            }
            else if (vpe.sfxFile != BytePtr.Null && vpe.sfxFile < _blockEnd && vpe.sfxFileEnd > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.sfxFileEnd;
            }
            else
            {
                _rejectBlock = false;
            }
        }

        private void CheckNoOverWrite()
        {
            if (_noOverWrite == 0xFFFF)
                return;

            var vpe = _vgaBufferPointers[_noOverWrite];

            if (vpe.vgaFile1 < _blockEnd && vpe.vgaFile1End > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.vgaFile1End;
            }
            else if (vpe.vgaFile2 < _blockEnd && vpe.vgaFile2End > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.vgaFile2End;
            }
            else if (vpe.sfxFile != BytePtr.Null && vpe.sfxFile < _blockEnd && vpe.sfxFileEnd > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.sfxFileEnd;
            }
            else
            {
                _rejectBlock = false;
            }
        }

        private void OpenGameFile()
        {
            _gameFile = OpenFileRead(GetFileName(GameFileTypes.GAME_GMEFILE));

            if (_gameFile == null)
                Error("openGameFile: Can't load game file '{0}'",
                    GetFileName(GameFileTypes.GAME_GMEFILE));

            var br = new BinaryReader(_gameFile);
            int size = br.ReadInt32();

            _gameOffsetsPtr = new Ptr<uint>(new uint[size / 4]);
            _gameFile.Seek(0, SeekOrigin.Begin);

            for (int r = 0; r < size / 4; r++)
                _gameOffsetsPtr[r] = br.ReadUInt32();
        }

        private uint GetTime()
        {
            return (uint) (DateTime.Now.Ticks / 1000);
        }

        private void LoadGamePcFile()
        {
            int fileSize;

            if (GetFileName(GameFileTypes.GAME_BASEFILE) != null)
            {
/* Read main gamexx file */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_BASEFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load gamexx file '{0}'",
                        GetFileName(GameFileTypes.GAME_BASEFILE));
                }

                if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_CRUNCHED_GAMEPC))
                {
                    var srcSize = (int) @in.Length;
                    var srcBuf = new byte[srcSize];
                    @in.Read(srcBuf, 0, srcSize);

                    int dstSize = srcBuf.ToInt32BigEndian(srcSize - 4);
                    var dstBuf = new byte[dstSize];
                    DecrunchFile(srcBuf, dstBuf, srcSize);

                    using (var stream = new MemoryStream(dstBuf, 0, dstSize))
                    {
                        ReadGamePcFile(stream);
                    }
                }
                else
                {
                    ReadGamePcFile(@in);
                }
            }

            if (GetFileName(GameFileTypes.GAME_TBLFILE) != null)
            {
/* Read list of TABLE resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_TBLFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load table resources file '{0}'",
                        GetFileName(GameFileTypes.GAME_TBLFILE));
                }

                fileSize = (int) @in.Length;

                _tblList = new byte[fileSize];
                @in.Read(_tblList.Data, _tblList.Offset, fileSize);

/* Remember the current state */
                _subroutineListOrg = _subroutineList;
                _tablesHeapPtrOrg = _tablesHeapPtr;
                _tablesHeapCurPosOrg = _tablesHeapCurPos;
            }

            if (GetFileName(GameFileTypes.GAME_STRFILE) != null)
            {
/* Read list of TEXT resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_STRFILE));
                if (@in == null)
                    Error("loadGamePcFile: Can't load text resources file '{0}'",
                        GetFileName(GameFileTypes.GAME_STRFILE));

                fileSize = (int) @in.Length;
                _strippedTxtMem = new byte[fileSize];
                if (_strippedTxtMem == null)
                    Error("loadGamePcFile: Out of memory for strip text list");
                @in.Read(_strippedTxtMem, 0, fileSize);
            }

            if (GetFileName(GameFileTypes.GAME_STATFILE) != null)
            {
/* Read list of ROOM STATE resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_STATFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load state resources file '{0}'",
                        GetFileName(GameFileTypes.GAME_STATFILE));
                }

                _numRoomStates = (int) (@in.Length / 8);

                _roomStates = new RoomState[_numRoomStates];

                var br = new BinaryReader(@in);
                for (uint s = 0; s < _numRoomStates; s++)
                {
                    ushort num = (ushort) (br.ReadUInt16BigEndian() - (_itemArrayInited - 2));

                    _roomStates[num].state = br.ReadUInt16BigEndian();
                    _roomStates[num].classFlags = br.ReadUInt16BigEndian();
                    _roomStates[num].roomExitStates = br.ReadUInt16BigEndian();
                }
            }

            if (GetFileName(GameFileTypes.GAME_RMSLFILE) != null)
            {
/* Read list of ROOM ITEMS resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_RMSLFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load room resources file '0'",
                        GetFileName(GameFileTypes.GAME_RMSLFILE));
                }

                fileSize = (int) @in.Length;

                _roomsList = new byte[fileSize];
                if (_roomsList == null)
                    Error("loadGamePcFile: Out of memory for room items list");
                @in.Read(_roomsList, 0, fileSize);
            }

            if (GetFileName(GameFileTypes.GAME_XTBLFILE) != null)
            {
/* Read list of XTABLE resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_XTBLFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load xtable resources file '{0}'",
                        GetFileName(GameFileTypes.GAME_XTBLFILE));
                }

                fileSize = (int) @in.Length;

                _xtblList = new byte[fileSize];
                if (_xtblList == null)
                    Error("loadGamePcFile: Out of memory for strip xtable list");
                @in.Read(_xtblList, 0, fileSize);

/* Remember the current state */
                _xsubroutineListOrg = _subroutineList;
                _xtablesHeapPtrOrg = _tablesHeapPtr;
                _xtablesHeapCurPosOrg = _tablesHeapCurPos;
            }
        }

        private void ReadGamePcFile(Stream @in)
        {
            int i;
            var numInitedObjects = AllocGamePcVars(@in);

            CreatePlayer();
            ReadGamePcText(@in);

            for (i = 2; i < numInitedObjects; i++)
            {
                ReadItemFromGamePc(@in, _itemArrayPtr[i]);
            }

            ReadSubroutineBlock(@in);
        }

        private void ReadSubroutine(BinaryReader br, Subroutine sub)
        {
            while (br.ReadUInt16BigEndian() == 0)
            {
                ReadSubroutineLine(br, CreateSubroutineLine(sub, 0xFFFF), sub);
            }
        }

        private void ReadSubroutineLine(BinaryReader br, SubroutineLine sl, Subroutine sub)
        {
            var lineBuffer = new byte[2048];
            BytePtr q = lineBuffer;
            int size;

            if (sub.id == 0)
            {
                sl.verb = br.ReadInt16BigEndian();
                sl.noun1 = br.ReadInt16BigEndian();
                sl.noun2 = br.ReadInt16BigEndian();
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                br.ReadUInt16BigEndian();
                br.ReadUInt16BigEndian();
                br.ReadUInt16BigEndian();
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                short tmp = br.ReadInt16BigEndian();
                q.WriteInt16BigEndian(0, tmp);
                while (tmp != 10000)
                {
                    if (q.ToUInt16BigEndian() == 198)
                    {
                        br.ReadUInt16BigEndian();
                    }
                    else
                    {
                        q = ReadSingleOpcode(br, q);
                    }

                    tmp = br.ReadInt16BigEndian();
                    q.WriteInt16BigEndian(0, tmp);
                }
            }
            else
            {
                while ((q.Value = br.ReadByte()) != 0xFF)
                {
                    if (q.Value == 87)
                    {
                        br.ReadUInt16BigEndian();
                    }
                    else
                    {
                        q = ReadSingleOpcode(br, q);
                    }
                }
            }

            size = q.Offset + 2;
            var data = AllocateTable(size);
            Array.Copy(lineBuffer, 0, data.Data, data.Offset, size);
        }

        private BytePtr ReadSingleOpcode(BinaryReader br, BytePtr ptr)
        {
            int i;
            ushort opcode;
            string[] table;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                table = opcodeArgTable_puzzlepack;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                table = opcodeArgTable_feeblefiles;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                     _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
                table = opcodeArgTable_simon2talkie;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                table = opcodeArgTable_simon2dos;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                     _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
                table = opcodeArgTable_simon1talkie;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                table = opcodeArgTable_simon1dos;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                table = opcodeArgTable_waxworks;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                table = opcodeArgTable_elvira2;
            else
                table = opcodeArgTable_elvira1;

            i = 0;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                opcode = ptr.ToUInt16BigEndian();
                ptr += 2;
            }
            else
            {
                opcode = ptr.Value;
                ptr.Offset++;
            }

            var stringPtr = table[opcode];
            if (stringPtr == null)
                Error("Unable to locate opcode table. Perhaps you are using the wrong game target?");

            for (;;)
            {
                if (stringPtr[i] == ' ')
                    return ptr;

                int l = stringPtr[i++];

                ushort val;
                switch (l)
                {
                    case 'F':
                    case 'N':
                    case 'S':
                    case 'a':
                    case 'n':
                    case 'p':
                    case 'v':
                    case '3':
                        val = br.ReadUInt16BigEndian();
                        ptr.WriteUInt16BigEndian(0, val);
                        ptr.Offset += 2;
                        break;

                    case 'B':
                        if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                        {
                            val = br.ReadUInt16BigEndian();
                            ptr.WriteUInt16BigEndian(0, val);
                            ptr.Offset += 2;
                        }
                        else
                        {
                            ptr.Value = br.ReadByte();
                            ptr.Offset++;
                            if (ptr[-1] == 0xFF)
                            {
                                ptr.Value = br.ReadByte();
                                ptr.Offset++;
                            }
                        }
                        break;

                    case 'I':
                        val = br.ReadUInt16BigEndian();
                        switch (val)
                        {
                            case 1:
                                val = 0xFFFF;
                                break;
                            case 3:
                                val = 0xFFFD;
                                break;
                            case 5:
                                val = 0xFFFB;
                                break;
                            case 7:
                                val = 0xFFF9;
                                break;
                            case 9:
                                val = 0xFFF7;
                                break;
                            default:
                                val = (ushort) FileReadItemID(br);
                                break;
                        }
                        ptr.WriteUInt16BigEndian(0, val);
                        ptr.Offset += 2;
                        break;

                    case 'T':
                        val = br.ReadUInt16BigEndian();
                        switch (val)
                        {
                            case 0:
                                val = 0xFFFF;
                                break;
                            case 3:
                                val = 0xFFFD;
                                break;
                            default:
                                val = (ushort) br.ReadUInt32BigEndian();
                                break;
                        }
                        ptr.WriteUInt16BigEndian(0, val);
                        ptr.Offset += 2;
                        break;
                    default:
                        Error("readSingleOpcode: Bad cmd table entry {0}", l);
                        break;
                }
            }
        }

        private SubroutineLine CreateSubroutineLine(Subroutine sub, int where)
        {
            SubroutineLine curSl = null, lastSl = null;

            var size = sub.id == 0 ? SUBROUTINE_LINE_BIG_SIZE : SUBROUTINE_LINE_SMALL_SIZE;
            var sl = new SubroutineLine(AllocateTable(size));

// where is what offset to insert the line at, locate the proper beginning line
            if (sub.first != 0)
            {
                curSl = new SubroutineLine(sub.Pointer + sub.first);
                while (where != 0)
                {
                    lastSl = curSl;
                    curSl = new SubroutineLine(sub.Pointer + curSl.next);
                    if (curSl.Pointer == sub.Pointer)
                        break;
                    where--;
                }
            }

            if (lastSl != null)
            {
// Insert the subroutine line in the middle of the link
                lastSl.next = (ushort) (sl.Pointer.Offset - sub.Pointer.Offset);
                sl.next = (ushort) (curSl.Pointer.Offset - sub.Pointer.Offset);
            }
            else
            {
// Insert the subroutine line at the head of the link
                sl.next = sub.first;
                sub.first = (ushort) (sl.Pointer.Offset - sub.Pointer.Offset);
            }

            return sl;
        }

        private Subroutine CreateSubroutine(ushort id)
        {
            System.Diagnostics.Debug.WriteLine($"CreateSubroutine {id}");
            AlignTableMem();

            var sub = new Subroutine(AllocateTable(Subroutine.Size));
            sub.id = id;
            sub.first = 0;
            sub.next = _subroutineList;
            _subroutineList = sub;
            return sub;
        }

        private static bool IS_ALIGNED(BytePtr value, int alignment)
        {
            return (value.Value & (alignment - 1)) == 0;
        }

        private void ReadItemFromGamePc(Stream @in, Item item)
        {
            uint type;

            var br = new BinaryReader(@in);
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                item.itemName = (ushort) br.ReadUInt32BigEndian();
                item.adjective = br.ReadInt16BigEndian();
                item.noun = br.ReadInt16BigEndian();
                item.state = br.ReadInt16BigEndian();
                br.ReadUInt16BigEndian();
                item.next = (ushort) FileReadItemID(br);
                item.child = (ushort) FileReadItemID(br);
                item.parent = (ushort) FileReadItemID(br);
                br.ReadUInt16BigEndian();
                br.ReadUInt16BigEndian();
                br.ReadUInt16BigEndian();
                item.classFlags = br.ReadUInt16BigEndian();
                item.children = null;
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
                item.itemName = (ushort) br.ReadUInt32BigEndian();
                item.adjective = br.ReadInt16BigEndian();
                item.noun = br.ReadInt16BigEndian();
                item.state = br.ReadInt16BigEndian();
                item.next = (ushort) FileReadItemID(br);
                item.child = (ushort) FileReadItemID(br);
                item.parent = (ushort) FileReadItemID(br);
                br.ReadUInt16BigEndian();
                item.classFlags = br.ReadUInt16BigEndian();
                item.children = null;
            }
            else
            {
                item.adjective = br.ReadInt16BigEndian();
                item.noun = br.ReadInt16BigEndian();
                item.state = br.ReadInt16BigEndian();
                item.next = (ushort) FileReadItemID(br);
                item.child = (ushort) FileReadItemID(br);
                item.parent = (ushort) FileReadItemID(br);
                br.ReadUInt16BigEndian();
                item.classFlags = br.ReadUInt16BigEndian();
                item.children = null;
            }


            type = br.ReadUInt32BigEndian();
            while (type != 0)
            {
                type = br.ReadUInt16BigEndian();
                if (type != 0)
                    ReadItemChildren(br, item, (ChildType) type);
            }
        }

        private void ReadGamePcText(Stream @in)
        {
            var br = new BinaryReader(@in);
            _textSize = br.ReadInt32BigEndian();
            _textMem = new byte[_textSize];
            if (_textMem == null)
                Error("readGamePcText: Out of text memory");

            @in.Read(_textMem, 0, _textSize);

            SetupStringTable(_textMem, _stringTabNum);
        }

        private void CreatePlayer()
        {
            _currentPlayer = _itemArrayPtr[1];
            _currentPlayer.adjective = -1;
            _currentPlayer.noun = 10000;

            var p = AllocateChildBlock<SubPlayer>(_currentPlayer, ChildType.kPlayerType);
            if (p == null)
                Error("createPlayer: player create failure");

            p.size = 0;
            p.weight = 0;
            p.strength = 6000;
            p.flags = 1; // Male
            p.level = 1;
            p.score = 0;

            SetUserFlag(_currentPlayer, 0, 0);
        }

        protected Item DerefItem(uint item)
        {
            if (item >= _itemArraySize)
                Error("derefItem: invalid item {0}", item);
            return _itemArrayPtr[item];
        }

        private int AllocGamePcVars(Stream @in)
        {
            int itemArraySize, itemArrayInited, stringTableNum;
            uint version;
            int i;

            var br = new BinaryReader(@in);
            itemArraySize = br.ReadInt32BigEndian();
            version = br.ReadUInt32BigEndian();
            itemArrayInited = br.ReadInt32BigEndian();
            stringTableNum = br.ReadInt32BigEndian();

// First two items are predefined
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
                itemArraySize += 2;
                itemArrayInited = itemArraySize;
            }
            else
            {
                itemArrayInited += 2;
                itemArraySize += 2;
            }

            if (version != 0x80)
                Error("allocGamePcVars: Not a runtime database");

            _itemArrayPtr = new Item[itemArraySize];
            _itemArraySize = itemArraySize;
            _itemArrayInited = itemArrayInited;
            for (i = 1; i < itemArrayInited; i++)
            {
                var item = AllocateItem<Item>();
                _itemArrayPtr[i] = item;
            }
// The rest is cleared automatically by calloc
            AllocateStringTable(stringTableNum + 10);
            _stringTabNum = stringTableNum;

            return itemArrayInited;
        }

        private BytePtr AllocateTable(int size)
        {
            var org = _tablesHeapPtr;
            size = (size + 1) & ~1;

            _tablesHeapPtr += size;
            _tablesHeapCurPos += size;

            if (_tablesHeapCurPos > _tablesHeapSize)
                Error("Tablesheap overflow");

            return org;
        }

        private T AllocateItem<T>() where T : new()
        {
            var ptr = new T();
            _itemHeap.PushBack(ptr);
            return ptr;
        }

        private void DecrunchFile(BytePtr srcBuf, BytePtr dstBuf, int srcSize)
        {
            throw new NotImplementedException();
        }

        private void SetZoneBuffers()
        {
            _zoneBuffers = new byte[_vgaMemSize];

            _vgaMemPtr = _zoneBuffers;
            _vgaMemBase = _zoneBuffers;
            _vgaFrozenBase = _zoneBuffers;
            _vgaRealBase = _zoneBuffers;
            _vgaMemEnd = _zoneBuffers + _vgaMemSize;
        }

        protected void PaletteFadeOut(Ptr<Color> palPtr, int num, int size)
        {
            for (int i = 0; i < num; i++)
            {
                palPtr[i] = Color.FromRgb(
                    palPtr[i].R >= size ? palPtr[i].R - size : 0,
                    palPtr[i].G >= size ? palPtr[i].G - size : 0,
                    palPtr[i].B >= size ? palPtr[i].B - size : 0);
            }
        }

        private void ClearSurfaces()
        {
            OSystem.GraphicsManager.FillScreen(0);

            if (_backBuf != null)
            {
                BackBuf.Data.Set(BackBuf.Offset, 0, _backBuf.Height * _backBuf.Pitch);
            }
        }

        private void LoadMusic(short nextMusicToPlay)
        {
            throw new NotImplementedException();
        }

        private void SetImage(ushort vgaSpriteId, bool vgaScript)
        {
            BytePtr b;

            uint zoneNum = (uint) (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ? 0 : vgaSpriteId / 100);

            for (;;)
            {
                var vpe = _vgaBufferPointers[zoneNum];
                _curVgaFile1 = vpe.vgaFile1;
                _curVgaFile2 = vpe.vgaFile2;

                if (vgaScript)
                {
                    if (vpe.vgaFile1 != BytePtr.Null)
                        break;
                    if (_zoneNumber != zoneNum)
                        _noOverWrite = _zoneNumber;

                    LoadZone((ushort) zoneNum);
                    _noOverWrite = 0xFFFF;
                }
                else
                {
                    _curSfxFile = vpe.sfxFile;
                    _curSfxFileSize = vpe.sfxFileEnd.Offset - vpe.sfxFile.Offset;
                    _zoneNumber = (ushort) zoneNum;

                    if (vpe.vgaFile1 != BytePtr.Null)
                        break;

                    LoadZone((ushort) zoneNum);
                }
            }

            var bb = _curVgaFile1;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                throw new NotImplementedException();
//                b = bb + READ_LE_UINT16(bb + 2);
//                count = READ_LE_UINT16(&((VgaFile1Header_Feeble *) b).imageCount);
//                b = bb + READ_LE_UINT16(&((VgaFile1Header_Feeble *) b).imageTable);
//
//                while (count--) {
//                    if (READ_LE_UINT16(&((ImageHeader_Feeble *) b).id) == vgaSpriteId)
//                        break;
//                    b += sizeof(ImageHeader_Feeble);
//                }
//                assert(READ_LE_UINT16(&((ImageHeader_Feeble *) b).id) == vgaSpriteId);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                b = bb + bb.ToUInt16BigEndian(4);
                var header = new VgaFile1Header_Common(b);
                var count = ScummHelper.SwapBytes(header.imageCount);
                b = bb + ScummHelper.SwapBytes(header.imageTable);

                while (count-- != 0)
                {
                    if (ScummHelper.SwapBytes(new ImageHeader_Simon(b).id) == vgaSpriteId)
                        break;
                    b += ImageHeader_Simon.Size;
                }
                System.Diagnostics.Debug.Assert(ScummHelper.SwapBytes(new ImageHeader_Simon(b).id) == vgaSpriteId);

                if (!vgaScript)
                    ClearVideoWindow(_windowNum, ScummHelper.SwapBytes(new ImageHeader_Simon(b).color));
            }
            else
            {
                throw new NotImplementedException();
//                b = bb + READ_BE_UINT16(bb + 10);
//                b += 20;
//
//                count = READ_BE_UINT16(&((VgaFile1Header_Common *) b).imageCount);
//                b = bb + READ_BE_UINT16(&((VgaFile1Header_Common *) b).imageTable);
//
//                while (count--!=0) {
//                    if (READ_BE_UINT16(&((ImageHeader_WW *) b).id) == vgaSpriteId)
//                        break;
//                    b += sizeof(ImageHeader_WW);
//                }
//                assert(READ_BE_UINT16(&((ImageHeader_WW *) b).id) == vgaSpriteId);
//
//                if (!vgaScript) {
//                    ushort color = READ_BE_UINT16(&((ImageHeader_WW *) b).color);
//                    if (_gd.ADGameDescription.gameType == GType_PN) {
//                        if (color & 0x80)
//                            _wiped = true;
//                        else if (_wiped == true)
//                            RestoreMenu();
//                        color &= 0xFF7F;
//                    }
//                    ClearVideoWindow(_windowNum, color);
//                }
            }

//            if (DebugMan.isDebugChannelEnabled(kDebugVGAScript)) {
//                if (_gd.ADGameDescription.gameType == GType_FF || _gd.ADGameDescription.gameType == GType_PP) {
//                    DumpVgaScript(_curVgaFile1 + READ_LE_UINT16(&((ImageHeader_Feeble*)b).scriptOffs), zoneNum, vgaSpriteId);
//                } else if (_gd.ADGameDescription.gameType == GType_SIMON1 || _gd.ADGameDescription.gameType == GType_SIMON2) {
//                    DumpVgaScript(_curVgaFile1 + READ_BE_UINT16(&((ImageHeader_Simon*)b).scriptOffs), zoneNum, vgaSpriteId);
//                } else {
//                    DumpVgaScript(_curVgaFile1 + READ_BE_UINT16(&((ImageHeader_WW*)b).scriptOffs), zoneNum, vgaSpriteId);
//                }
//            }

            var vc_ptr_org = _vcPtr;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                throw new NotImplementedException();
//                _vcPtr = _curVgaFile1 + READ_LE_UINT16(&((ImageHeader_Feeble*) b).scriptOffs);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                _vcPtr = _curVgaFile1 + ScummHelper.SwapBytes(new ImageHeader_Simon(b).scriptOffs);
            }
            else
            {
                throw new NotImplementedException();
//                _vcPtr = _curVgaFile1 + READ_BE_UINT16(&((ImageHeader_WW*) b).scriptOffs);
            }

            RunVgaScript();
            _vcPtr = vc_ptr_org;
        }

        private void AllocItemHeap()
        {
            _itemHeap.Clear();
        }

        private void AllocTablesHeap()
        {
            _tablesHeapSize = _tableMemSize;
            _tablesHeapCurPos = 0;
            _tablesHeapPtr = new byte[_tableMemSize];
        }

        private bool SaveGame(int slot, string caption)
        {
            throw new NotImplementedException();
        }

        private string GenSaveName(int i)
        {
            throw new NotImplementedException();
        }

        private bool LoadGame(string genSaveName)
        {
            throw new NotImplementedException();
        }

        private void UserGame(bool b)
        {
            throw new NotImplementedException();
        }

        private static readonly byte[] zoneTable =
        {
            0, 0, 2, 2, 2, 2, 0, 2, 2, 2,
            3, 0, 0, 0, 0, 0, 0, 0, 1, 0,
            3, 3, 3, 1, 3, 0, 0, 0, 1, 0,
            2, 0, 3, 0, 3, 3, 0, 1, 1, 0,
            1, 2, 2, 2, 0, 2, 2, 2, 0, 2,
            1, 2, 2, 2, 0, 2, 2, 2, 2, 2,
            2, 2, 2, 1, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 0, 2, 0, 3, 2, 2, 2, 3,
            2, 3, 3, 3, 1, 3, 3, 1, 1, 0,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 0, 0, 2, 2, 0,
            0, 2, 0, 2, 2, 2, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 2, 2, 2, 2, 2,
            2, 0, 2, 0, 0, 2, 2, 0, 2, 2,
            2, 2, 2, 2, 2, 0, 0, 0, 0, 0,
        };

        private static readonly string[] opcodeArgTable_puzzlepack =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "N ", "N ", "NN ", "NN ",
            "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBN ", "NIB ", "NN ", "B ", "BI ", "IN ", "N ", "N ", "NN ",
            "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "N ", "T ", "T ", "NNNNNB ", "BTNN ", "BTS ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NNBNNN ", "NN ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NNB ", "N ", "N ", "Ban ", " ", " ", " ", " ", " ", "IN ", "B ", " ", "II ", " ", "BI ",
            "N ", "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "N ", "N ", "N ",
            "N ", "IBN ", "IBN ", "IN ", "B ", "BNNN ", "BBTS ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ",
            "T ", "N ", " ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", "T ", " ", "N ", "N ",
            " ", " ", "BT ", " ", "B ", " ", "BBBB ", " ", " ", "BBBB ", "B ", "B ", "B ", "B "
        };

        private static readonly string[] opcodeArgTable_feeblefiles =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BTS ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NNBNNN ", "NN ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NNB ", "N ", "N ", "Ban ", " ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ",
            "N ", "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ",
            "B ", "IBB ", "IBN ", "IB ", "B ", "BNNN ", "BBTS ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ",
            "T ", "N ", " ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", "T ", " ", "N ", "N ",
            " ", " ", "BT ", " ", "B ", " ", "BBBB ", " ", " ", "BBBB ", "B ", "B ", "B ", "B "
        };

        private static readonly string[] opcodeArgTable_simon2talkie =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BTS ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NNBNNN ", "NN ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NNB ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ",
            "N ", "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ",
            "B ", "IBB ", "IBN ", "IB ", "B ", "BNBN ", "BBTS ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ",
            "T ", "T ", "B ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", " ", " ", "N ", "N ",
            " ", " ", "BT ", " ", "B "
        };

        private static readonly string[] opcodeArgTable_simon2dos =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BT ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NNBNNN ", "NN ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NNB ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ",
            "N ", "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ",
            "B ", "IBB ", "IBN ", "IB ", "B ", "BNBN ", "BBT ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ",
            "T ", "T ", "B ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", " ", " ", "N ", "N ",
            " ", " ", "BT ", " ", "B "
        };

        private static readonly string[] opcodeArgTable_simon1talkie =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BTS ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NBNNN ", "N ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NN ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ", "N ",
            "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ", "B ",
            "IBB ", "IBN ", "IB ", "B ", "BNBN ", "BBTS ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ", "T ",
            "T ", "B ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", " ", " ", "N ", "N ", " ",
            " ",
        };

        private static readonly string[] opcodeArgTable_simon1dos =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BT ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NBNNN ", "N ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NN ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ", "N ",
            "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ", "B ",
            "IBB ", "IBN ", "IB ", "B ", "BNBN ", "BBT ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ", "T ",
            "T ", "B ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", " ", " ", "N ", "N ", " ",
            " ",
        };

        public static readonly string[] opcodeArgTable_waxworks =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BT ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", "T ", "T ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NBNNN ", "N ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NN ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ", "N ",
            "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ", "B ",
            "IBB ", "IBN ", "IB ", "B ", " ", "TB ", "TB ", "I ", "N ", "B ", "INB ", "INB ", "INB ", "INB ",
            "INB ", "INB ", "INB ", "N ", " ", "INBB ", "B ", "B ", "Ian ", "B ", "B ", "B ", "B ", "T ",
            "T ", "B ", " ", "I ", " ", " "
        };

        private static readonly string[] opcodeArgTable_elvira1 =
        {
            "I ", "I ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "F ", "F ", "FN ",
            "FN ", "FN ", "FN ", "FF ", "FF ", "FF ", "FF ", "II ", "II ", "a ", "a ", "n ", "n ", "p ",
            "N ", "I ", "I ", "I ", "I ", "IN ", "IB ", "IB ", "II ", "IB ", "N ", " ", " ", " ", "I ",
            "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "IBF ", "FIB ", "FF ", "N ", "NI ",
            "IF ", "F ", "F ", "IB ", "IB ", "FN ", "FN ", "FN ", "FF ", "FF ", "FN ", "FN ", "FF ", "FF ",
            "FN ", "FF ", "FN ", "F ", "I ", "IN ", "IN ", "IB ", "IB ", "IB ", "IB ", "II ", "I ", "I ",
            "IN ", "T ", "F ", " ", "T ", "T ", "I ", "I ", " ", " ", "T ", " ", " ", " ", " ", " ", "T ",
            " ", "N ", "INN ", "II ", "II ", "ITN ", "ITIN ", "ITIN ", "I3 ", "IN ", "I ", "I ", "Ivnn ",
            "vnn ", "Ivnn ", "NN ", "IT ", "INN ", " ", "N ", "N ", "N ", "T ", "v ", " ", " ", " ", " ",
            "FN ", "I ", "TN ", "IT ", "II ", "I ", " ", "N ", "I ", " ", "I ", "NI ", "I ", "I ", "T ",
            "I ", "I ", "N ", "N ", " ", "N ", "IF ", "IF ", "IF ", "IF ", "IF ", "IF ", "T ", "IB ",
            "IB ", "IB ", "I ", " ", "vnnN ", "Ivnn ", "T ", "T ", "T ", "IF ", " ", " ", " ", "Ivnn ",
            "IF ", "INI ", "INN ", "IN ", "II ", "IFF ", "IIF ", "I ", "II ", "I ", "I ", "IN ", "IN ",
            "II ", "II ", "II ", "II ", "IIN ", "IIN ", "IN ", "II ", "IN ", "IN ", "T ", "vanpan ",
            "vIpI ", "T ", "T ", " ", " ", "IN ", "IN ", "IN ", "IN ", "N ", "INTTT ", "ITTT ",
            "ITTT ", "I ", "I ", "IN ", "I ", " ", "F ", "NN ", "INN ", "INN ", "INNN ", "TF ", "NN ",
            "N ", "NNNNN ", "N ", " ", "NNNNNNN ", "N ", " ", "N ", "NN ", "N ", "NNNNNIN ", "N ", "N ",
            "N ", "NNN ", "NNNN ", "INNN ", "IN ", "IN ", "TT ", "I ", "I ", "I ", "TTT ", "IN ", "IN ",
            "FN ", "FN ", "FN ", "N ", "N ", "N ", "NI ", " ", " ", "N ", "I ", "INN ", "NN ", "N ",
            "N ", "Nan ", "NN ", " ", " ", " ", " ", " ", " ", " ", "IF ", "N ", " ", " ", " ", "II ",
            " ", "NI ", "N ",
        };

        private static readonly string[] opcodeArgTable_elvira2 =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "I  ", "I ", " ", "T ", " ", " ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", "T ", "T ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NBNNN ", "N ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "N ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NN ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ", "N ",
            "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ", "B ",
            "IBB ", "IBN ", "IB ", "B ", " ", "TB ", "TB ", "I ", "N ", "B ", "INB ", "INB ", "INB ", "INB ",
            "INB ", "INB ", "INB ", "N ", " ", "INBB ", "B ", "B ", "Ian ", "B ", "B ", "B ", "B ", "T ",
            "T ", "B ", " ", "I ", " ", " "
        };

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
    }
}