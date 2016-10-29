//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015
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
using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;
using NScumm.Sci.Graphics;
using NScumm.Sci.Engine;
using NScumm.Core.Common;

namespace NScumm.Sci.Video
{
    /// <summary>
    /// The playback status of the robot.
    /// </summary>
    enum RobotStatus
    {
        kRobotStatusUninitialized = 0,
        kRobotStatusPlaying = 1,
        kRobotStatusEnd = 2,
        kRobotStatusPaused = 3
    }

    /// <summary>
    /// RobotDecoder implements the logic required
    /// for Robot animations.
    ///
    /// @note A paused or finished RobotDecoder was
    /// classified as serializable in SCI3, but the
    /// save/load code would attempt to use uninitialised
    /// values, so it seems that robots were not ever
    /// actually able to be saved.
    /// </summary>
    class RobotDecoder
    {
        /// <summary>
        /// The number of ticks that should elapse
        /// between each AV sync check.
        /// </summary>
        private const int AudioSyncCheckInterval = 5 * 60; /* 5 seconds */

        enum CompressionType
        {
            kCompressionLZS = 0,
            kCompressionNone = 2
        }

        /// <summary>
        /// Describes the state of a Robot video cel.
        /// </summary>
        class CelHandleInfo
        {
            /// <summary>
            /// The persistence level of Robot cels.
            /// </summary>
            public enum CelHandleLifetime
            {
                kNoCel = 0,
                kFrameLifetime = 1,
                kRobotLifetime = 2
            }

            /// <summary>
            ///  A reg_t pointer to an in-memory
            /// bitmap containing the cel.
            /// </summary>
            public Register bitmapId;

            /// <summary>
            /// The lifetime of the cel, either just
            /// for this frame or for the entire
            /// duration of the robot playback.
            /// </summary>
            public CelHandleLifetime status;

            /// <summary>
            /// The size, in pixels, of the decompressed
            /// cel.
            /// </summary>
            public int area;

            public CelHandleInfo()
            {
                bitmapId = Register.NULL_REG;
                status = CelHandleLifetime.kNoCel;
            }
        }

        /// <summary>
        /// Special high value used to represent
        /// parameters that should be left unchanged
        /// when calling `showFrame`
        /// </summary>
        public const int kUnspecified = 50000;

        /// <summary>
        /// Maximum number of on-screen screen items.
        /// </summary>
        private const int kScreenItemListSize = 10;

        /// <summary>
        /// Maximum number of queued audio blocks.
        /// </summary>
        private const int kAudioListSize = 10;

        /// <summary>
        /// Maximum number of samples used for frame timing.
        /// </summary>
        private const int kDelayListSize = 10;

        /// <summary>
        /// Maximum number of cues.
        /// </summary>
        private const int kCueListSize = 256;

        /// <summary>
        /// Maximum number of 'fixed' cels that never
        /// change for the duration of a robot.
        /// </summary>
        private const int kFixedCelListSize = 4;

        /// <summary>
        /// The size of a hunk palette in the Robot stream.
        /// </summary>
        private const int kRawPaletteSize = 1200;

        /// <summary>
        /// The size of a frame of Robot data. This
        /// value was used to align the first block of
        /// data after the main Robot header to the next
        /// CD sector.
        /// </summary>
        private const int kRobotFrameSize = 2048;

        /// <summary>
        /// The size of a block of zero-compressed
        /// audio. Used to fill audio when the size of
        /// an audio packet does not match the expected
        /// packet size.
        /// </summary>
        private const int kRobotZeroCompressSize = 2048;

        /// <summary>
        /// The size of the audio block header, in bytes.
        /// The audio block header consists of the
        /// compressed size of the audio in the record,
        /// plus the position of the audio in the
        /// compressed data stream.
        /// </summary>
        private const int kAudioBlockHeaderSize = 8;

        /// <summary>
        /// The size of a Robot cel header, in bytes.
        /// </summary>
        private const int kCelHeaderSize = 22;

        /// <summary>
        /// The maximum amount that the frame rate is
        /// allowed to drift from the nominal frame rate
        /// in order to correct for AV drift or slow
        /// playback.
        /// </summary>
        private const int kMaxFrameRateDrift = 1;

        /// <summary>
        /// The current status of the player.
        /// </summary>
        private RobotStatus _status;

        /// <summary>
        /// A map of frame numbers to byte offsets within `_stream`.
        /// </summary>
        private Array<int> _recordPositions = new Array<int>();

        /// <summary>
        /// The offset of the Robot file within a
        /// resource bundle.
        /// </summary>
        private int _fileOffset;

        /// <summary>
        /// A list of cue times that is updated to
        /// prevent earlier cue values from being
        /// given to the game more than once.
        /// </summary>
        private int[] _cueTimes = new int[kCueListSize];

        /// <summary>
        /// The original list of cue times from the
        /// raw Robot data.
        /// </summary>
        private int[] _masterCueTimes = new int[kCueListSize];

        /// <summary>
        /// The list of values to provide to a game
        /// when a cue value is requested.
        /// </summary>
        private int[] _cueValues = new int[kCueListSize];

        /// <summary>
        /// The current playback frame rate.
        /// </summary>
        private short _frameRate;

        /// <summary>
        /// The nominal playback frame rate.
        /// </summary>
        private short _normalFrameRate;

        /// <summary>
        /// The minimal playback frame rate. Used to
        /// correct for AV sync drift when the video
        /// is more than one frame ahead of the audio.
        /// </summary>
        private short _minFrameRate;

        /// <summary>
        /// The maximum playback frame rate. Used to
        /// correct for AV sync drift when the video
        /// is more than one frame behind the audio.
        /// </summary>
        private short _maxFrameRate;

        /// <summary>
        /// The maximum number of record blocks that
        /// can be skipped without causing audio to
        /// drop out.
        /// </summary>
        private short _maxSkippablePackets;

        /// <summary>
        /// The currently displayed frame number.
        /// </summary>
        private int _currentFrameNo;

        /// <summary>
        /// The last displayed frame number.
        /// </summary>
        private int _previousFrameNo;

        /// <summary>
        /// The time, in ticks, when the robot was
        /// last started or resumed.
        /// </summary>
        private int _startTime;

        /// <summary>
        /// The first frame displayed when the
        /// robot was resumed.
        /// </summary>
        private int _startFrameNo;

        /// <summary>
        /// The last frame displayed when the robot
        /// was resumed.
        /// </summary>
        private int _startingFrameNo;

        private IBinaryReader _fileStream;

        /// <summary>
        /// If true, the robot just started playing and
        /// is awaiting output for the first frame.
        /// </summary>
        private bool _syncFrame;

        /// <summary>
        /// Scratch memory used to store the compressed robot
        /// video data for the current frame.
        /// </summary>
        private Array<byte> _doVersion5Scratch = new Array<byte>();

        /// <summary>
        /// When set to a non-negative value, forces the next
        /// call to doRobot to render the given frame number
        /// instead of whatever frame would have normally been
        /// rendered.
        /// </summary>
        private int _cueForceShowFrame;

        /// <summary>
        /// The plane where the robot animation will be drawn.
        /// </summary>
        private Plane _plane;

        /// <summary>
        /// A list of pointers to ScreenItems used by the robot.
        /// </summary>
        private Array<ScreenItem> _screenItemList = new Array<ScreenItem>();

        /// <summary>
        /// The positions of the various screen items in this
        /// robot, in screen coordinates.
        /// </summary>
        private Array<short> _screenItemX = new Array<short>(), _screenItemY = new Array<short>();

        /// <summary>
        /// The raw position values from the cel header for
        /// each screen item currently on-screen.
        /// </summary>
        private Array<short> _originalScreenItemX = new Array<short>(), _originalScreenItemY = new Array<short>();

        /// <summary>
        /// The duration of the current robot, in frames.
        /// </summary>
        private ushort _numFramesTotal;

        /// <summary>
        /// The screen priority of the video.
        /// @see ScreenItem::_priority
        /// </summary>
        private short _priority;

        /// <summary>
        /// The amount of visual vertical compression applied
        /// to the current cel. A value of 100 means no
        /// compression; a value above 100 indicates how much
        /// the cel needs to be scaled along the y-axis to
        /// return to its original dimensions.
        /// </summary>
        private byte _verticalScaleFactor;

        /// <summary>
        /// Whether or not this robot animation has
        /// an audio track.
        /// </summary>
        private bool _hasAudio;

        /// <summary>
        /// The audio list for the current robot.
        /// </summary>
        private AudioList _audioList = new AudioList();

        /// <summary>
        /// The size, in bytes, of a block of audio data,
        /// excluding the audio block header.
        /// </summary>
        private ushort _audioBlockSize;

        /// <summary>
        /// The expected size of a block of audio data,
        /// in bytes, excluding the audio block header.
        /// </summary>
        private short _expectedAudioBlockSize;

        /// <summary>
        /// The number of compressed audio bytes that are
        /// needed per frame to fill the audio buffer
        /// without causing audio to drop out.
        /// </summary>
        private short _audioRecordInterval;

        /// <summary>
        /// If true, primer audio buffers should be filled
        /// with silence instead of trying to read buffers
        /// from the Robot data.
        /// </summary>
        private ushort _primerZeroCompressFlag;

        /// <summary>
        /// The size, in bytes, of the primer audio in the
        /// Robot, including any extra alignment padding.
        /// </summary>
        private ushort _primerReservedSize;

        /// <summary>
        /// The combined size, in bytes, of the even and odd
        /// primer channels.
        /// </summary>
        private int _totalPrimerSize;

        /// <summary>
        /// The absolute offset of the primer audio data in
        /// the robot data stream.
        /// </summary>
        private int _primerPosition;

        /// <summary>
        /// The size, in bytes, of the even primer.
        /// </summary>
        private int _evenPrimerSize;

        /// <summary>
        /// The size, in bytes, of the odd primer.
        /// </summary>
        private int _oddPrimerSize;

        /// <summary>
        /// The absolute position in the audio stream of
        /// the first audio packet.
        /// </summary>
        private int _firstAudioRecordPosition;

        public ushort GetFrameSize(ref Rect outRect)
        {
            outRect.Clip(0, 0);
            for (var i = 0; i < _screenItemList.Size; ++i)
            {
                ScreenItem screenItem = _screenItemList[i];
                outRect.Extend(screenItem.GetNowSeenRect(_plane));
            }

            return _numFramesTotal;
        }

        /// <summary>
        /// A temporary buffer used to hold one frame of
        /// raw(DPCM-compressed) audio when reading audio
        /// records from the robot stream.
        /// </summary>
        private byte[] _audioBuffer;

        /// <summary>
        /// The next tick count when AV sync should be
        /// checked and framerate adjustments made, if
        /// necessary.
        /// </summary>
        private uint _checkAudioSyncTime;

        /// <summary>
        /// The performance timer for the robot.
        /// </summary>
        private DelayTime _delayTime;
        private SegManager _segMan;

        /// <summary>
        /// The maximum areas, in pixels, for each of
        /// the fixed cels in the robot.Used for
        /// preallocation of cel memory.
        /// </summary>
        private Array<uint> _maxCelArea = new Array<uint>();

        /// <summary>
        /// The hunk palette to use when rendering the
        /// current frame, if the `usePalette` flag was set
        /// in the robot header.
        /// </summary>
        private byte[] _rawPalette;

        /// <summary>
        /// A list of the raw video data sizes, in bytes,
        /// for each frame of the robot.
        /// </summary>
        private Array<int> _videoSizes = new Array<int>();

        /// <summary>
        /// A list of cels that will be present for the
        /// entire duration of the robot animation.
        /// </summary>
        private Array<Register> _fixedCels = new Array<Register>();

        /// <summary>
        /// The decompressor for LZS-compressed cels.
        /// </summary>
        private DecompressorLZS _decompressor = new DecompressorLZS();

        /// <summary>
        ///  The origin of the robot animation, in screen
        /// coordinates.
        /// </summary>
        private Point _position;

        /// <summary>
        /// Global scaling applied to the robot.
        /// </summary>
        private ScaleInfo _scaleInfo = new ScaleInfo();

        /// <summary>
        /// The native resolution of the robot.
        /// </summary>
        private short _xResolution, _yResolution;

        /// <summary>
        /// A list of handles for each cel in the current
        /// frame.
        /// </summary>
        private Array<CelHandleInfo> _celHandles = new Array<CelHandleInfo>();

        /// <summary>
        /// Scratch memory used to temporarily store
        /// decompressed cel data for vertically squashed
        /// cels.
        /// </summary>
        private Array<byte> _celDecompressionBuffer = new Array<byte>();

        /// <summary>
        /// The size, in bytes, of the squashed cel
        /// decompression buffer.
        /// </summary>
        private int _celDecompressionArea;

        /// <summary>
        /// The version number for the currently loaded
        /// robot.
        ///
        /// There are several known versions of robot:
        ///
        /// v2: before Nov 1994; no known examples
        /// v3: before Nov 1994; no known examples
        /// v4: Jan 1995; PQ:SWAT demo
        /// v5: Mar 1995; SCI2.1 and SCI3 games
        /// v6: SCI3 games
        /// </summary>
        private ushort _version;

        /// <summary>
        /// Whether or not the coordinates read from robot
        /// data are high resolution.
        /// </summary>
        private bool _isHiRes;

        /// <summary>
        ///  The maximum number of cels that will be rendered
        /// on any given frame in this robot.Used for
        /// preallocation of cel memory.
        /// </summary>
        private short _maxCelsPerFrame;

        /// <summary>
        /// Gets the playback status of the player.
        /// </summary>
        public RobotStatus Status => _status;

        /// <summary>
        /// Gets the current game time, in ticks.
        /// </summary>
        public uint TickCount => SciEngine.Instance.TickCount;

        /// <summary>
        /// Gets the currently displayed frame.
        /// </summary>
        public short FrameNo
        {
            get
            {
                if (_status == RobotStatus.kRobotStatusUninitialized)
                {
                    return 0;
                }

                return (short)_currentFrameNo;
            }
        }

        public RobotDecoder(SegManager segMan)
        {
            _delayTime = new DelayTime(this);
            _segMan = segMan;
            _status = RobotStatus.kRobotStatusUninitialized;
            _rawPalette = new byte[kRawPaletteSize];
        }

        /// <summary>
        /// Resumes a paused robot.
        /// </summary>
        public void Resume()
        {
            if (_status != RobotStatus.kRobotStatusPaused)
            {
                return;
            }

            _startingFrameNo = _currentFrameNo;
            _status = RobotStatus.kRobotStatusPlaying;
            if (_hasAudio)
            {
                PrimeAudio((uint)(_currentFrameNo * 60 / _frameRate));
                _syncFrame = true;
            }

            SetRobotTime(_currentFrameNo);
            for (int i = 0; i < kCueListSize; ++i)
            {
                if (_masterCueTimes[i] != -1 && _masterCueTimes[i] < _currentFrameNo)
                {
                    _cueTimes[i] = -1;
                }
                else
                {
                    _cueTimes[i] = _masterCueTimes[i];
                }
            }
        }

        /// <summary>
        /// Closes the currently open robot file.
        /// </summary>
        public void Close()
        {
            if (_status == RobotStatus.kRobotStatusUninitialized)
            {
                return;
            }

            DebugC(DebugLevels.Video, "Closing robot");

            _status = RobotStatus.kRobotStatusUninitialized;
            _videoSizes.Clear();
            _recordPositions.Clear();
            _celDecompressionBuffer.Clear();
            _doVersion5Scratch.Clear();
            _fileStream.Dispose();
            _fileStream = null;

            for (var i = 0; i < _celHandles.Size; ++i)
            {
                if (_celHandles[i].status == CelHandleInfo.CelHandleLifetime.kFrameLifetime)
                {
                    _segMan.FreeBitmap(_celHandles[i].bitmapId);
                }
            }
            _celHandles.Clear();

            for (var i = 0; i < _fixedCels.Size; ++i)
            {
                _segMan.FreeBitmap(_fixedCels[i]);
            }
            _fixedCels.Clear();

            if (SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(_plane._object) != null)
            {
                for (var i = 0; i < _screenItemList.Size; ++i)
                {
                    if (_screenItemList[i] != null)
                    {
                        SciEngine.Instance._gfxFrameout.DeleteScreenItem(_screenItemList[i]);
                    }
                }
            }
            _screenItemList.Clear();

            if (_hasAudio)
            {
                _audioList.Reset();
            }
        }

        /// <summary>
        /// Sets the visual priority of the robot.
        /// @see Plane::_priority
        /// </summary>
        /// <param name="newPriority"></param>
        public void SetPriority(short newPriority)
        {
            _priority = newPriority;
        }

        /// <summary>
        /// Retrieves the value associated with the
        /// current cue point.
        /// </summary>
        /// <returns></returns>
        public short GetCue()
        {
            if (_status == RobotStatus.kRobotStatusUninitialized || _status == RobotStatus.kRobotStatusPaused ||
                _syncFrame)
            {
                return 0;
            }

            if (_status == RobotStatus.kRobotStatusEnd)
            {
                return -1;
            }

            ushort estimatedNextFrameNo = Math.Min(CalculateNextFrameNo((uint)_delayTime.PredictedTicks()), _numFramesTotal);

            for (int i = 0; i < kCueListSize; ++i)
            {
                if (_cueTimes[i] != -1 && _cueTimes[i] <= estimatedNextFrameNo)
                {
                    if (_cueTimes[i] >= _previousFrameNo)
                    {
                        _cueForceShowFrame = _cueTimes[i] + 1;
                    }

                    _cueTimes[i] = -1;
                    return (short)_cueValues[i];
                }
            }

            return 0;
        }

        /// <summary>
        /// Pauses the robot. Once paused, the audio for a robot
        /// is disabled until the end of playback.
        /// </summary>
        public void Pause()
        {
            if (_status != RobotStatus.kRobotStatusPlaying)
            {
                return;
            }

            if (_hasAudio)
            {
                _audioList.StopAudioNow();
            }

            _status = RobotStatus.kRobotStatusPaused;
            _frameRate = _normalFrameRate;
        }

        /// <summary>
        /// Opens a robot file for playback.
        /// Newly opened robots are paused by default.
        /// </summary>
        /// <param name="robotId"></param>
        /// <param name="plane"></param>
        /// <param name="priority"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        public void Open(int robotId, Register plane, short priority, short x, short y, short scale)
        {
            if (_status != RobotStatus.kRobotStatusUninitialized)
            {
                Close();
            }

            InitStream(robotId);

            _version = _fileStream.ReadUInt16();

            // TODO: Version 4 for PQ:SWAT demo?
            if (_version < 5 || _version > 6)
            {
                Error("Unsupported version {0} of Robot resource", _version);
            }

            DebugC(DebugLevels.Video, "Opening version {0} robot {1}", _version, robotId);

            InitPlayback();

            _audioBlockSize = _fileStream.ReadUInt16();
            _primerZeroCompressFlag = _fileStream.ReadUInt16();
            _fileStream.BaseStream.Seek(2, SeekOrigin.Current); // unused
            _numFramesTotal = _fileStream.ReadUInt16();
            ushort paletteSize = _fileStream.ReadUInt16();
            _primerReservedSize = _fileStream.ReadUInt16();
            _xResolution = _fileStream.ReadInt16();
            _yResolution = _fileStream.ReadInt16();
            bool hasPalette = _fileStream.ReadByte() != 0;
            _hasAudio = _fileStream.ReadByte() != 0;
            _fileStream.BaseStream.Seek(2, SeekOrigin.Current); // unused
            _frameRate = _normalFrameRate = _fileStream.ReadInt16();
            _isHiRes = _fileStream.ReadInt16() != 0;
            _maxSkippablePackets = _fileStream.ReadInt16();
            _maxCelsPerFrame = _fileStream.ReadInt16();

            // used for memory preallocation of fixed cels
            _maxCelArea.PushBack(_fileStream.ReadUInt32());
            _maxCelArea.PushBack(_fileStream.ReadUInt32());
            _maxCelArea.PushBack(_fileStream.ReadUInt32());
            _maxCelArea.PushBack(_fileStream.ReadUInt32());
            _fileStream.BaseStream.Seek(8, SeekOrigin.Current); // reserved

            if (_hasAudio)
            {
                InitAudio();
            }
            else
            {
                _fileStream.BaseStream.Seek(_primerReservedSize, SeekOrigin.Current);
            }

            _priority = priority;
            InitVideo(x, y, scale, plane, hasPalette, paletteSize);
            InitRecordAndCuePositions();
        }

        /// <summary>
        /// Moves robot to the specified frame and pauses playback.
        /// 
        /// @note Called DisplayFrame in SSCI.
        /// </summary>
        /// <param name="frameNo"></param>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        /// <param name="kUnspecified"></param>
        public void ShowFrame(ushort frameNo, ushort newX, ushort newY, ushort newPriority)
        {
            DebugC(DebugLevels.Video, "Show frame {0} ({1} {2} {3})", frameNo, newX, newY, newPriority);

            if (newX != kUnspecified)
            {
                _position.X = (short)newX;
            }

            if (newY != kUnspecified)
            {
                _position.Y = (short)newY;
            }

            if (newPriority != kUnspecified)
            {
                _priority = (short)newPriority;
            }

            _currentFrameNo = frameNo;
            Pause();

            if (frameNo != _previousFrameNo)
            {
                SeekToFrame(frameNo);
                DoVersion5(false);
            }
            else
            {
                for (var i = 0; i < _screenItemList.Size; ++i)
                {
                    if (_isHiRes)
                    {
                        SciBitmap bitmap = _segMan.LookupBitmap(_celHandles[i].bitmapId);

                        short scriptWidth = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
                        short scriptHeight = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;
                        short screenWidth = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
                        short screenHeight = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;

                        if (scriptWidth == LowRes.X && scriptHeight == LowRes.Y)
                        {
                            Rational lowResToScreenX = new Rational(screenWidth, LowRes.X);
                            Rational lowResToScreenY = new Rational(screenHeight, LowRes.Y);
                            Rational screenToLowResX = new Rational(LowRes.X, screenWidth);
                            Rational screenToLowResY = new Rational(LowRes.Y, screenHeight);

                            short scaledX = (short)(_originalScreenItemX[i] + (_position.X * lowResToScreenX));
                            short scaledY1 = (short)(_originalScreenItemY[i] + (_position.Y * lowResToScreenY));
                            short scaledY2 = (short)(scaledY1 + bitmap.Height - 1);

                            short lowResX = (short)(scaledX * screenToLowResX);
                            short lowResY = (short)(scaledY2 * screenToLowResY);

                            bitmap.Origin = new Point(
                                (short)((scaledX - (lowResX * lowResToScreenX)) * -1),
                                (short)((lowResY * lowResToScreenY) - scaledY1)
                            );

                            _screenItemX[i] = lowResX;
                            _screenItemY[i] = lowResY;
                        }
                        else
                        {
                            short scaledX = (short)(_originalScreenItemX[i] + _position.X);
                            short scaledY = (short)(_originalScreenItemY[i] + _position.Y + bitmap.Height - 1);
                            bitmap.Origin = new Point(0, (short)(bitmap.Height - 1));
                            _screenItemX[i] = scaledX;
                            _screenItemY[i] = scaledY;
                        }
                    }
                    else
                    {
                        _screenItemX[i] = (short)(_originalScreenItemX[i] + _position.X);
                        _screenItemY[i] = (short)(_originalScreenItemY[i] + _position.Y);
                    }

                    if (_screenItemList[i] == null)
                    {
                        CelInfo32 celInfo = new CelInfo32();
                        celInfo.type = CelType.Mem;
                        celInfo.bitmap = _celHandles[i].bitmapId;
                        ScreenItem screenItem = new ScreenItem(_plane._object, celInfo);
                        _screenItemList[i] = screenItem;
                        screenItem._position = new Point(_screenItemX[i], _screenItemY[i]);
                        if (_priority == -1)
                        {
                            screenItem._fixedPriority = false;
                        }
                        else
                        {
                            screenItem._priority = _priority;
                            screenItem._fixedPriority = true;
                        }
                        SciEngine.Instance._gfxFrameout.AddScreenItem(screenItem);
                    }
                    else
                    {
                        ScreenItem screenItem = _screenItemList[i];
                        screenItem._celInfo.bitmap = _celHandles[i].bitmapId;
                        screenItem._position = new Point(_screenItemX[i], _screenItemY[i]);
                        if (_priority == -1)
                        {
                            screenItem._fixedPriority = false;
                        }
                        else
                        {
                            screenItem._priority = _priority;
                            screenItem._fixedPriority = true;
                        }
                        SciEngine.Instance._gfxFrameout.UpdateScreenItem(screenItem);
                    }
                }
            }

            _previousFrameNo = frameNo;
        }

        /// <summary>
        /// Pumps the robot player for the next frame of video.
        /// This is the main rendering function.
        /// </summary>
        public void DoRobot()
        {
            if (_status != RobotStatus.kRobotStatusPlaying)
            {
                return;
            }

            if (!_syncFrame)
            {
                if (_cueForceShowFrame != -1)
                {
                    _currentFrameNo = _cueForceShowFrame;
                    _cueForceShowFrame = -1;
                }
                else
                {
                    int nextFrameNo = CalculateNextFrameNo((uint)_delayTime.PredictedTicks());
                    if (nextFrameNo < _currentFrameNo)
                    {
                        return;
                    }
                    _currentFrameNo = nextFrameNo;
                }
            }

            if (_currentFrameNo >= _numFramesTotal)
            {
                int finalFrameNo = _numFramesTotal - 1;
                if (_previousFrameNo == finalFrameNo)
                {
                    _status = RobotStatus.kRobotStatusEnd;
                    if (_hasAudio)
                    {
                        _audioList.StopAudio();
                        _frameRate = _normalFrameRate;
                        _hasAudio = false;
                    }
                    return;
                }
                else
                {
                    _currentFrameNo = finalFrameNo;
                }
            }

            if (_currentFrameNo == _previousFrameNo)
            {
                _audioList.SubmitDriverMax();
                return;
            }

            if (_hasAudio)
            {
                for (int candidateFrameNo = _previousFrameNo + _maxSkippablePackets + 1; candidateFrameNo < _currentFrameNo; candidateFrameNo += _maxSkippablePackets + 1)
                {

                    _audioList.SubmitDriverMax();

                    int audioPosition, audioSize;
                    if (ReadAudioDataFromRecord(candidateFrameNo, _audioBuffer, out audioPosition, out audioSize))
                    {
                        _audioList.AddBlock(audioPosition, audioSize, _audioBuffer);
                    }
                }
                _audioList.SubmitDriverMax();
            }

            _delayTime.StartTiming();
            SeekToFrame(_currentFrameNo);
            DoVersion5();
            if (_hasAudio)
            {
                _audioList.SubmitDriverMax();
            }
        }

        /// <summary>
        /// Evaluates frame drift and makes modifications to
        /// the player in order to ensure that future frames
        /// will arrive on time.
        /// </summary>
        public void FrameNowVisible()
        {
            if (_status != RobotStatus.kRobotStatusPlaying)
            {
                return;
            }

            if (_syncFrame)
            {
                _syncFrame = false;
                if (_hasAudio)
                {
                    _audioList.StartAudioNow();
                    _checkAudioSyncTime = (uint)(_startTime + AudioSyncCheckInterval);
                }

                SetRobotTime(_currentFrameNo);
            }

            if (_delayTime.TimingInProgress)
            {
                _delayTime.EndTiming();
            }

            if (_hasAudio)
            {
                _audioList.SubmitDriverMax();
            }

            if (_previousFrameNo != _currentFrameNo)
            {
                _previousFrameNo = _currentFrameNo;
            }

            if (!_syncFrame && _hasAudio && TickCount >= _checkAudioSyncTime)
            {
                RobotAudioStream.StreamState status;
                bool success = SciEngine.Instance._audio32.QueryRobotAudio(out status);
                if (!success)
                {
                    return;
                }

                int bytesPerFrame = status.rate / _normalFrameRate * (status.bits == 16 ? 2 : 1);
                // check again in 1/3rd second
                _checkAudioSyncTime = TickCount + 60 / 3;

                int currentVideoFrameNo = CalculateNextFrameNo() - _startingFrameNo;
                int currentAudioFrameNo = status.bytesPlaying / bytesPerFrame;
                DebugC(DebugLevels.Video, "Video frame {0} {1} audio frame {3}", currentVideoFrameNo, currentVideoFrameNo == currentAudioFrameNo ? "=" : currentVideoFrameNo < currentAudioFrameNo ? "<" : ">", currentAudioFrameNo);
                if (currentVideoFrameNo < _numFramesTotal &&
                    currentAudioFrameNo < _numFramesTotal)
                {

                    bool shouldResetRobotTime = false;

                    if (currentAudioFrameNo < currentVideoFrameNo - 1 && _frameRate != _minFrameRate)
                    {
                        DebugC(DebugLevels.Video, "[v] Reducing frame rate");
                        _frameRate = _minFrameRate;
                        shouldResetRobotTime = true;
                    }
                    else if (currentAudioFrameNo > currentVideoFrameNo + 1 && _frameRate != _maxFrameRate)
                    {
                        DebugC(DebugLevels.Video, "[^] Increasing frame rate");
                        _frameRate = _maxFrameRate;
                        shouldResetRobotTime = true;
                    }
                    else if (_frameRate != _normalFrameRate)
                    {
                        DebugC(DebugLevels.Video, "[=] Setting to normal frame rate");
                        _frameRate = _normalFrameRate;
                        shouldResetRobotTime = true;
                    }

                    if (shouldResetRobotTime)
                    {
                        if (currentAudioFrameNo < _currentFrameNo)
                        {
                            SetRobotTime(_currentFrameNo);
                        }
                        else
                        {
                            SetRobotTime(currentAudioFrameNo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Submits any outstanding audio blocks that should
        /// be added to the queue before the robot frame
        /// becomes visible.
        /// </summary>
        public void FrameAlmostVisible()
        {
            if (_status == RobotStatus.kRobotStatusPlaying && !_syncFrame)
            {
                if (_previousFrameNo != _currentFrameNo)
                {
                    while (CalculateNextFrameNo() < _currentFrameNo)
                    {
                        _audioList.SubmitDriverMax();
                    }
                }
            }
        }

        /// <summary>
        /// Sets the start time and frame of the robot
        /// when the robot is started or resumed.
        /// </summary>
        /// <param name="frameNo"></param>
        private void SetRobotTime(int frameNo)
        {
            _startTime = (int)TickCount;
            _startFrameNo = frameNo;
        }

        /// <summary>
        /// Primes the audio buffer with the first frame
        /// of audio data.
        /// 
        /// @note `primeAudio` was `InitAudio` in SSCI
        /// </summary>
        /// <param name="startTick"></param>
        /// <returns></returns>
        private bool PrimeAudio(uint startTick)
        {

            bool success = true;
            _audioList.Reset();

            if (startTick == 0)
            {
                _audioList.PrepareForPrimer();
                byte[] evenPrimerBuff = new byte[_evenPrimerSize];
                byte[] oddPrimerBuff = new byte[_oddPrimerSize];

                success = ReadPrimerData(evenPrimerBuff, oddPrimerBuff);
                if (success)
                {
                    if (_evenPrimerSize != 0)
                    {
                        _audioList.AddBlock(0, _evenPrimerSize, evenPrimerBuff);
                    }
                    if (_oddPrimerSize != 0)
                    {
                        _audioList.AddBlock(1, _oddPrimerSize, oddPrimerBuff);
                    }
                }
            }
            else
            {

                System.Diagnostics.Debug.Assert(_evenPrimerSize * 2 >= _audioRecordInterval || _oddPrimerSize * 2 >= _audioRecordInterval);

                int audioStartFrame = 0;
                int videoStartFrame = (int)(startTick * _frameRate / 60);

                System.Diagnostics.Debug.Assert(videoStartFrame < _numFramesTotal);

                int audioStartPosition = (int)(startTick * RobotAudioStream.kRobotSampleRate) / 60;
                if ((audioStartPosition & 1) != 0)
                {
                    audioStartPosition--;
                }
                _audioList.SetAudioOffset(audioStartPosition);
                _audioList.PrepareForPrimer();

                if (audioStartPosition < _evenPrimerSize * 2 ||

                    audioStartPosition + 1 < _oddPrimerSize * 2)
                {

                    byte[] evenPrimerBuffer = new byte[_evenPrimerSize];
                    byte[] oddPrimerBuffer = new byte[_oddPrimerSize];
                    success = ReadPrimerData(evenPrimerBuffer, oddPrimerBuffer);
                    if (success)
                    {
                        int halfAudioStartPosition = audioStartPosition / 2;
                        if (audioStartPosition < _evenPrimerSize * 2)
                        {
                            _audioList.AddBlock(audioStartPosition, _evenPrimerSize - halfAudioStartPosition, new BytePtr(evenPrimerBuffer, halfAudioStartPosition));
                        }

                        if (audioStartPosition + 1 < _oddPrimerSize * 2)
                        {
                            _audioList.AddBlock(audioStartPosition + 1, _oddPrimerSize - halfAudioStartPosition, new BytePtr(oddPrimerBuffer, halfAudioStartPosition));
                        }
                    }
                }

                if (audioStartPosition >= _firstAudioRecordPosition)
                {
                    int audioRecordSize = _expectedAudioBlockSize;

                    System.Diagnostics.Debug.Assert(audioRecordSize > 0);

                    System.Diagnostics.Debug.Assert(_audioRecordInterval > 0);

                    System.Diagnostics.Debug.Assert(_firstAudioRecordPosition >= 0);

                    audioStartFrame = (audioStartPosition - _firstAudioRecordPosition) / _audioRecordInterval;

                    System.Diagnostics.Debug.Assert(audioStartFrame < videoStartFrame);

                    int oddRemainder;
                    int audioRecordStart;
                    int audioRecordEnd;
                    if (audioStartFrame > 0)
                    {
                        int lastAudioFrame = audioStartFrame - 1;
                        oddRemainder = lastAudioFrame & 1;
                        audioRecordStart = (lastAudioFrame * _audioRecordInterval) + oddRemainder + _firstAudioRecordPosition;
                        audioRecordEnd = (audioRecordStart + ((audioRecordSize - 1) * 2)) + oddRemainder + _firstAudioRecordPosition;

                        if (audioStartPosition >= audioRecordStart && audioStartPosition <= audioRecordEnd)
                        {
                            --audioStartFrame;
                        }
                    }


                    System.Diagnostics.Debug.Assert((audioStartPosition & 1) == 0);
                    if ((audioStartFrame & 1) != 0)
                    {
                        ++audioStartPosition;
                    }

                    if (!ReadPartialAudioRecordAndSubmit(audioStartFrame, audioStartPosition))
                    {
                        return false;
                    }

                    ++audioStartFrame;

                    System.Diagnostics.Debug.Assert(audioStartFrame < videoStartFrame);

                    oddRemainder = audioStartFrame & 1;
                    audioRecordStart = (audioStartFrame * _audioRecordInterval) + oddRemainder + _firstAudioRecordPosition;
                    audioRecordEnd = (audioRecordStart + ((audioRecordSize - 1) * 2)) + oddRemainder + _firstAudioRecordPosition;

                    if (audioStartPosition >= audioRecordStart && audioStartPosition <= audioRecordEnd)
                    {
                        if (!ReadPartialAudioRecordAndSubmit(audioStartFrame, audioStartPosition + 1))
                        {
                            return false;
                        }

                        ++audioStartFrame;
                    }
                }

                int audioPosition, audioSize;
                for (int i = audioStartFrame; i < videoStartFrame; i++)
                {
                    if (!ReadAudioDataFromRecord(i, _audioBuffer, out audioPosition, out audioSize))
                    {
                        break;
                    }

                    _audioList.AddBlock(audioPosition, audioSize, _audioBuffer);
                }
            }

            return success;
        }

        /// <summary>
        /// Submits part of the audio packet of the given
        /// frame to the audio list, starting `startPosition`
        /// bytes into the audio.
        /// </summary>
        /// <param name="audioStartFrame"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool ReadPartialAudioRecordAndSubmit(int startFrame, int startPosition)
        {
            int audioPosition, audioSize;
            bool success = ReadAudioDataFromRecord(startFrame, _audioBuffer,out audioPosition, out audioSize);
            if (success)
            {
                int relativeStartOffset = (startPosition - audioPosition) / 2;
                _audioList.AddBlock(startPosition, audioSize - relativeStartOffset,new BytePtr(_audioBuffer, relativeStartOffset));
            }

            return success;
        }

        /// <summary>
        /// Reads primer data from the robot data stream
        /// and puts it into the given buffers.
        /// </summary>
        /// <param name="outEvenBuffer"></param>
        /// <param name="outOddBuffer"></param>
        /// <returns></returns>
        private bool ReadPrimerData(byte[] outEvenBuffer, byte[] outOddBuffer)
        {
            if (_primerReservedSize != 0)
            {
                if (_totalPrimerSize != 0)
                {
                    _fileStream.BaseStream.Seek(_primerPosition, SeekOrigin.Begin);
                    if (_evenPrimerSize > 0)
                    {
                        _fileStream.BaseStream.Read(outEvenBuffer, 0, _evenPrimerSize);
                    }

                    if (_oddPrimerSize > 0)
                    {
                        _fileStream.BaseStream.Read(outOddBuffer, 0, _oddPrimerSize);
                    }
                }
            }
            else if (_primerZeroCompressFlag != 0)
            {
                Array.Clear(outEvenBuffer, 0, _evenPrimerSize);
                Array.Clear(outOddBuffer, 0, _oddPrimerSize);
            }
            else
            {
                Error("ReadPrimerData - Flags corrupt");
            }

            return true;
        }

        /// <summary>
        /// Sets up the robot's data record and cue positions.
        /// </summary>
        private void InitRecordAndCuePositions()
        {
            _videoSizes.Reserve(_numFramesTotal);
            _recordPositions.Reserve(_numFramesTotal);
            var recordSizes = new Array<int>();
            recordSizes.Reserve(_numFramesTotal);

            switch (_version)
            {
                case 5: // 16-bit sizes and positions
                    for (int i = 0; i < _numFramesTotal; ++i)
                    {
                        _videoSizes.PushBack(_fileStream.ReadUInt16());
                    }
                    for (int i = 0; i < _numFramesTotal; ++i)
                    {
                        recordSizes.PushBack(_fileStream.ReadUInt16());
                    }
                    break;
                case 6: // 32-bit sizes and positions
                    for (int i = 0; i < _numFramesTotal; ++i)
                    {
                        _videoSizes.PushBack(_fileStream.ReadInt32());
                    }
                    for (int i = 0; i < _numFramesTotal; ++i)
                    {
                        recordSizes.PushBack(_fileStream.ReadInt32());
                    }
                    break;
                default:
                    Error("Unknown Robot version {0}", _version);
                    break;
            }

            for (int i = 0; i < kCueListSize; ++i)
            {
                _cueTimes[i] = _fileStream.ReadInt32();
            }

            for (int i = 0; i < kCueListSize; ++i)
            {
                _cueValues[i] = _fileStream.ReadUInt16();
            }

            Array.Copy(_cueTimes, _masterCueTimes, kCueListSize);

            int bytesRemaining = (int)(_fileStream.BaseStream.Position - _fileOffset) % kRobotFrameSize;
            if (bytesRemaining != 0)
            {
                _fileStream.BaseStream.Seek(kRobotFrameSize - bytesRemaining, SeekOrigin.Current);
            }

            int position = (int)_fileStream.BaseStream.Position;
            _recordPositions.PushBack(position);
            for (int i = 0; i < _numFramesTotal - 1; ++i)
            {
                position += recordSizes[i];
                _recordPositions.PushBack(position);
            }
        }

        /// <summary>
        /// Sets up the initial values for video rendering.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <param name="plane"></param>
        /// <param name="hasPalette"></param>
        /// <param name="paletteSize"></param>
        private void InitVideo(short x, short y, short scale, Register plane, bool hasPalette, ushort paletteSize)
        {
            _position = new Point(x, y);

            if (scale != 128)
            {
                _scaleInfo.x = scale;
                _scaleInfo.y = scale;
                _scaleInfo.signal = ScaleSignals32.kScaleSignalManual;
            }

            _plane = SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(plane);
            if (_plane == null)
            {
                Error("Invalid plane {0} passed to RobotDecoder::open", plane);
            }

            _minFrameRate = (short)(_frameRate - kMaxFrameRateDrift);
            _maxFrameRate = (short)(_frameRate + kMaxFrameRateDrift);

            if (_xResolution == 0 || _yResolution == 0)
            {
                // TODO: Default values were taken from RESOURCE.CFG hires property
                // if it exists, so need to check games' configuration files for those
                _xResolution = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
                _yResolution = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;
            }

            if (hasPalette)
            {
                _fileStream.BaseStream.Read(_rawPalette, 0, paletteSize);
            }
            else
            {
                _fileStream.BaseStream.Seek(paletteSize, SeekOrigin.Current);
            }

            _screenItemList.Reserve(kScreenItemListSize);

            // Fixed cel buffers are for version 5 and newer
            _fixedCels.Reserve(Math.Min(_maxCelsPerFrame, (short)kFixedCelListSize));
            _celDecompressionBuffer.Reserve((int)(_maxCelArea[0] + SciBitmap.BitmapHeaderSize + kRawPaletteSize));
            _celDecompressionArea = (int)_maxCelArea[0];
        }

        /// <summary>
        /// Sets up the initial values for audio decoding.
        /// </summary>
        private void InitAudio()
        {
            _syncFrame = true;

            _audioRecordInterval = (short)(RobotAudioStream.kRobotSampleRate / _frameRate);

            // TODO: Might actually be for all games newer than Lighthouse; check to
            // see which games have this condition.
            if (SciEngine.Instance.GameId != SciGameId.LIGHTHOUSE && (_audioRecordInterval & 1) == 0)
            {
                ++_audioRecordInterval;
            }

            _expectedAudioBlockSize = (short)(_audioBlockSize - kAudioBlockHeaderSize);
            Array.Resize(ref _audioBuffer, kRobotZeroCompressSize + _expectedAudioBlockSize);

            if (_primerReservedSize != 0)
            {
                int primerHeaderPosition = (int)_fileStream.BaseStream.Position;
                _totalPrimerSize = _fileStream.ReadInt32();
                short compressionType = _fileStream.ReadInt16();
                _evenPrimerSize = _fileStream.ReadInt32();
                _oddPrimerSize = _fileStream.ReadInt32();
                _primerPosition = (int)_fileStream.BaseStream.Position;

                if (compressionType != 0)
                {
                    Error("Unknown audio header compression type {0}", compressionType);
                }

                if (_evenPrimerSize + _oddPrimerSize != _primerReservedSize)
                {
                    _fileStream.BaseStream.Seek(primerHeaderPosition + _primerReservedSize, SeekOrigin.Begin);
                }
            }
            else if (_primerZeroCompressFlag != 0)
            {
                _evenPrimerSize = 19922;
                _oddPrimerSize = 21024;
            }

            _firstAudioRecordPosition = _evenPrimerSize * 2;

            int usedEachFrame = (RobotAudioStream.kRobotSampleRate / 2) / _frameRate;
            _maxSkippablePackets = (short)Math.Max((ushort)0, (ushort)(_audioBlockSize / usedEachFrame - 1));
        }

        /// <summary>
        /// Sets up the initial values for playback control.
        /// </summary>
        private void InitPlayback()
        {
            _startFrameNo = 0;
            _startTime = -1;
            _startingFrameNo = -1;
            _cueForceShowFrame = -1;
            _previousFrameNo = -1;
            _currentFrameNo = 0;
            _status = RobotStatus.kRobotStatusPaused;
        }

        /// <summary>
        /// Sets up the read stream for the robot.
        /// </summary>
        /// <param name="robotId"></param>
        private void InitStream(int robotId)
        {
            var fileName = $"{robotId}.rbt";
            var stream = SciEngine.OpenFileRead(fileName);
            _fileOffset = 0;

            if (stream == null)
            {
                Error("Unable to open robot file {0}", fileName);
            }

            var br = new BinaryReader(stream);
            ushort id = br.ReadUInt16();
            if (id != 0x16)
            {
                Error("Invalid robot file {0}", fileName);
            }

            // TODO: Mac version not tested, so this could be totally wrong
            _fileStream = new EndianBinaryReader(SciEngine.Instance.Platform == Platform.Macintosh, stream);
            stream.Seek(2, SeekOrigin.Begin);
            if (br.ReadUInt32BigEndian() != ScummHelper.MakeTag('S', 'O', 'L', '\0'))
            {
                Error("Resource {0} is not Robot type!", fileName);
            }
        }

        /// <summary>
        /// Calculates the next frame number that needs
        /// to be rendered, using the timing data
        /// collected by DelayTime.
        /// </summary>
        /// <param name="extraTicks"></param>
        /// <returns></returns>
        private ushort CalculateNextFrameNo(uint extraTicks = 0)
        {
            return (ushort)(TicksToFrames((uint)(TickCount + extraTicks - _startTime)) + _startFrameNo);
        }

        /// <summary>
        /// Calculates and returns the number of frames
        /// that should be rendered in `ticks` time,
        /// according to the current target frame rate
        /// of the robot.
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        private uint TicksToFrames(uint ticks)
        {
            return (uint)((ticks * _frameRate) / 60);
        }

        /// <summary>
        /// Seeks the raw data stream to the record for
        /// the given frame number.
        /// </summary>
        /// <param name="frameNo"></param>
        private void SeekToFrame(int frameNo)
        {
            _fileStream.BaseStream.Seek(_recordPositions[frameNo], SeekOrigin.Begin);
        }

        /// <summary>
        /// Renders a version 5/6 robot frame.
        /// </summary>
        /// <param name="shouldSubmitAudio"></param>
        private void DoVersion5(bool shouldSubmitAudio = true)
        {
            var oldScreenItemCount = _screenItemList.Size;
            int videoSize = _videoSizes[_currentFrameNo];
            _doVersion5Scratch.Resize(videoSize);

            byte[] videoFrameData = _doVersion5Scratch.Storage;

            _fileStream.BaseStream.Read(videoFrameData, 0, videoSize);

            var screenItemCount = videoFrameData.ReadSci11EndianUInt16();

            if (screenItemCount > kScreenItemListSize)
            {
                return;
            }

            if (_hasAudio && (ResourceManager.GetSciVersion() < SciVersion.V3 || shouldSubmitAudio))
            {
                int audioPosition, audioSize;
                if (ReadAudioDataFromRecord(_currentFrameNo, _audioBuffer, out audioPosition, out audioSize))
                {
                    _audioList.AddBlock(audioPosition, audioSize, _audioBuffer);
                }
            }

            if (screenItemCount > oldScreenItemCount)
            {
                _screenItemList.Reserve(screenItemCount);
                _screenItemX.Reserve(screenItemCount);
                _screenItemY.Reserve(screenItemCount);
                _originalScreenItemX.Reserve(screenItemCount);
                _originalScreenItemY.Reserve(screenItemCount);
            }


            CreateCels5(new BytePtr(videoFrameData, 2), (short)screenItemCount, true);
            for (var i = 0; i < screenItemCount; ++i)
            {
                Point position = new Point(_screenItemX[i], _screenItemY[i]);

                // TODO: Version 6 robot?
                //		int scaleXRemainder;
                if (_scaleInfo.signal == ScaleSignals32.kScaleSignalManual)
                {
                    position.X = (short)((position.X * _scaleInfo.x) / 128);
                    // TODO: Version 6 robot?
                    //			scaleXRemainder = (position.x * _scaleInfo.x) % 128;
                    position.Y = (short)((position.Y * _scaleInfo.y) / 128);
                }

                if (_screenItemList[i] == null)
                {
                    CelInfo32 celInfo = new CelInfo32();
                    celInfo.bitmap = _celHandles[i].bitmapId;
                    ScreenItem screenItem = new ScreenItem(_plane._object, celInfo, position, _scaleInfo);
                    _screenItemList[i] = screenItem;
                    // TODO: Version 6 robot?
                    // screenItem._field_30 = scaleXRemainder;

                    if (_priority == -1)
                    {
                        screenItem._fixedPriority = false;
                    }
                    else
                    {
                        screenItem._fixedPriority = true;
                        screenItem._priority = _priority;
                    }
                    SciEngine.Instance._gfxFrameout.AddScreenItem(screenItem);
                }
                else
                {
                    ScreenItem screenItem = _screenItemList[i];
                    screenItem._celInfo.bitmap = _celHandles[i].bitmapId;
                    screenItem._position = position;
                    // TODO: Version 6 robot?
                    // screenItem._field_30 = scaleXRemainder;

                    if (_priority == -1)
                    {
                        screenItem._fixedPriority = false;
                    }
                    else
                    {
                        screenItem._fixedPriority = true;
                        screenItem._priority = _priority;
                    }
                    SciEngine.Instance._gfxFrameout.UpdateScreenItem(screenItem);
                }
            }

            for (var i = screenItemCount; i < oldScreenItemCount; ++i)
            {
                if (_screenItemList[i] != null)
                {
                    SciEngine.Instance._gfxFrameout.DeleteScreenItem(_screenItemList[i]);
                    _screenItemList[i] = null;
                }
            }
        }

        /// <summary>
        /// Creates screen items for a version 5/6 robot.
        /// </summary>
        /// <param name="rawVideoData"></param>
        /// <param name="numCels"></param>
        /// <param name="usePalette"></param>
        private void CreateCels5(BytePtr rawVideoData, short numCels, bool usePalette)
        {
            PreallocateCelMemory(rawVideoData, numCels);
            for (short i = 0; i < numCels; ++i)
            {
                rawVideoData.Offset += CreateCel5(rawVideoData, i, usePalette);
            }
        }

        /// <summary>
        /// Creates a single screen item for a cel in a
        /// version 5/6 robot.
        /// 
        /// Returns the size, in bytes, of the raw cel data.
        /// </summary>
        /// <param name="rawVideoData"></param>
        /// <param name="screenItemIndex"></param>
        /// <param name="usePalette"></param>
        /// <returns></returns>
        private int CreateCel5(BytePtr rawVideoData, short screenItemIndex, bool usePalette)
        {
            _verticalScaleFactor = rawVideoData[1];
            short celWidth = (short)rawVideoData.ReadSci11EndianUInt16(2);
            short celHeight = (short)rawVideoData.ReadSci11EndianUInt16(4);
            Point celPosition = new Point((short)rawVideoData.ReadSci11EndianUInt16(10),
                                            (short)rawVideoData.ReadSci11EndianUInt16(12));
            ushort dataSize = rawVideoData.ReadSci11EndianUInt16(14);
            short numDataChunks = (short)rawVideoData.ReadSci11EndianUInt16(16);

            rawVideoData += kCelHeaderSize;

            short scriptWidth = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;
            short screenWidth = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
            short screenHeight = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;

            Point origin;
            if (scriptWidth == LowRes.X && scriptHeight == LowRes.Y)
            {
                Rational lowResToScreenX = new Rational(screenWidth, LowRes.X);
                Rational lowResToScreenY = new Rational(screenHeight, LowRes.Y);
                Rational screenToLowResX = new Rational(LowRes.X, screenWidth);
                Rational screenToLowResY = new Rational(LowRes.Y, screenHeight);

                short scaledX = (short)(celPosition.X + (_position.X * lowResToScreenX));
                short scaledY1 = (short)(celPosition.Y + (_position.Y * lowResToScreenY));
                short scaledY2 = (short)(scaledY1 + celHeight - 1);

                short lowResX = (short)(scaledX * screenToLowResX);
                short lowResY = (short)(scaledY2 * screenToLowResY);

                origin.X = (short)((scaledX - (lowResX * lowResToScreenX)) * -1);
                origin.Y = (short)((lowResY * lowResToScreenY) - scaledY1);
                _screenItemX[screenItemIndex] = lowResX;
                _screenItemY[screenItemIndex] = lowResY;


                DebugC(DebugLevels.Video, "Low resolution position c: %d %d l: %d/%d %d/%d d: %d %d s: %d/%d %d/%d x: %d y: %d",
                    celPosition.X, celPosition.Y, lowResX, scriptWidth, lowResY, scriptHeight, origin.X, origin.Y, scaledX, screenWidth, scaledY2, screenHeight, scaledX - origin.X, scaledY2 - origin.Y);
            }
            else
            {
                short highResX = (short)(celPosition.X + _position.X);
                short highResY = (short)(celPosition.Y + _position.Y + celHeight - 1);

                origin.X = 0;
                origin.Y = (short)(celHeight - 1);
                _screenItemX[screenItemIndex] = highResX;
                _screenItemY[screenItemIndex] = highResY;


                DebugC(DebugLevels.Video, "High resolution position c: %d %d s: %d %d d: %d %d",
                    celPosition.X, celPosition.Y, highResX, highResY, origin.X, origin.Y);
            }

            _originalScreenItemX[screenItemIndex] = celPosition.X;
            _originalScreenItemY[screenItemIndex] = celPosition.Y;


            System.Diagnostics.Debug.Assert(_celHandles[screenItemIndex].area >= celWidth * celHeight);

            SciBitmap bitmap = _segMan.LookupBitmap(_celHandles[screenItemIndex].bitmapId);

            System.Diagnostics.Debug.Assert(bitmap.Width == celWidth && bitmap.Height == celHeight);

            System.Diagnostics.Debug.Assert(bitmap.XResolution == _xResolution && bitmap.YResolution == _yResolution);

            System.Diagnostics.Debug.Assert(bitmap.HunkPaletteOffset == (uint)bitmap.Width * bitmap.Height + SciBitmap.BitmapHeaderSize);
            bitmap.Origin = origin;

            BytePtr targetBuffer = BytePtr.Null;
            if (_verticalScaleFactor == 100)
            {
                // direct copy to bitmap
                targetBuffer = bitmap.Pixels;
            }
            else
            {
                // go through squashed cel decompressor
                _celDecompressionBuffer.Reserve(_celDecompressionArea >= celWidth ? (celHeight * _verticalScaleFactor / 100) : 0);
                targetBuffer = new BytePtr(_celDecompressionBuffer.Storage);
            }

            for (int i = 0; i < numDataChunks; ++i)
            {
                uint compressedSize = rawVideoData.ReadSci11EndianUInt32();
                uint decompressedSize = rawVideoData.ReadSci11EndianUInt32(4);
                CompressionType compressionType = (CompressionType)rawVideoData.ReadSci11EndianUInt16(8);
                rawVideoData += 10;

                switch (compressionType)
                {
                    case CompressionType.kCompressionLZS:
                        {
                            MemoryStream videoDataStream = new MemoryStream(rawVideoData.Data, rawVideoData.Offset, (int)compressedSize);
                            _decompressor.Unpack(videoDataStream, targetBuffer, (int)compressedSize, (int)decompressedSize);
                            break;
                        }
                    case CompressionType.kCompressionNone:
                        Array.Copy(rawVideoData.Data, rawVideoData.Offset, targetBuffer.Data, targetBuffer.Offset, (int)decompressedSize);
                        break;
                    default:
                        Error("Unknown compression type {0}!", compressionType);
                        break;
                }

                rawVideoData.Offset += (int)compressedSize;
                targetBuffer.Offset += (int)decompressedSize;
            }

            if (_verticalScaleFactor != 100)
            {
                ExpandCel(bitmap.Pixels, _celDecompressionBuffer, celWidth, celHeight);
            }

            if (usePalette)
            {
                Array.Copy(_rawPalette, 0, bitmap.HunkPalette.Data, bitmap.HunkPalette.Offset, (int)kRawPaletteSize);
            }

            return kCelHeaderSize + dataSize;
        }

        /// <summary>
        /// Scales a vertically compressed cel to its original
        /// uncompressed dimensions.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="celWidth"></param>
        /// <param name="celHeight"></param>
        private void ExpandCel(BytePtr target, Array<byte> source, short celWidth, short celHeight)
        {
            System.Diagnostics.Debug.Assert(source != null && target != BytePtr.Null);

            int s = 0;
            int sourceHeight = (celHeight * _verticalScaleFactor) / 100;
            System.Diagnostics.Debug.Assert(sourceHeight > 0);

            short numerator = celHeight;
            short denominator = (short)sourceHeight;
            int remainder = 0;
            for (short y = (short)(sourceHeight - 1); y >= 0; --y)
            {
                remainder += numerator;
                short linesToDraw = (short)(remainder / denominator);
                remainder %= denominator;

                while ((linesToDraw--) != 0)
                {
                    for (int i = 0; i < celWidth; i++)
                    {
                        target[i] = source[s + i];
                    }
                    target += celWidth;
                }

                s += celWidth;
            }
        }

        /// <summary>
        /// Preallocates memory for the next `numCels` cels
        /// in the robot data stream.
        /// </summary>
        /// <param name="rawVideoData"></param>
        /// <param name="numCels"></param>
        private void PreallocateCelMemory(BytePtr rawVideoData, short numCels)
        {
            for (var i = 0; i < _celHandles.Size; ++i)
            {
                CelHandleInfo celHandle = _celHandles[i];

                if (celHandle.status == CelHandleInfo.CelHandleLifetime.kFrameLifetime)
                {
                    _segMan.FreeBitmap(celHandle.bitmapId);
                    celHandle.bitmapId = Register.NULL_REG;
                    celHandle.status = CelHandleInfo.CelHandleLifetime.kNoCel;
                    celHandle.area = 0;
                }
            }
            _celHandles.Resize(numCels);

            int numFixedCels = Math.Min(numCels, (short)kFixedCelListSize);
            for (int i = 0; i < numFixedCels; ++i)
            {
                CelHandleInfo celHandle = _celHandles[i];

                // NOTE: There was a check to see if the cel handle was not allocated
                // here, for some reason, which would mean that nothing was ever
                // allocated from fixed cels, because the _celHandles array just got
                // deleted and recreated...
                if (celHandle.bitmapId == Register.NULL_REG)
                {
                    break;
                }

                celHandle.bitmapId = _fixedCels[i];
                celHandle.status = CelHandleInfo.CelHandleLifetime.kRobotLifetime;
                celHandle.area = (int)_maxCelArea[i];
            }

            int maxFrameArea = 0;
            for (int i = 0; i < numCels; ++i)
            {
                short celWidth = (short)rawVideoData.ReadSci11EndianUInt16(2);
                short celHeight = (short)rawVideoData.ReadSci11EndianUInt16(4);
                ushort dataSize = rawVideoData.ReadSci11EndianUInt16(14);
                int area = celWidth * celHeight;

                if (area > maxFrameArea)
                {
                    maxFrameArea = area;
                }

                CelHandleInfo celHandle = _celHandles[i];
                if (celHandle.status == CelHandleInfo.CelHandleLifetime.kRobotLifetime)
                {
                    if (_maxCelArea[i] < area)
                    {
                        _segMan.FreeBitmap(celHandle.bitmapId);
                        _segMan.AllocateBitmap(out celHandle.bitmapId, celWidth, celHeight, 255, 0, 0, _xResolution, _yResolution, kRawPaletteSize, false, false);
                        celHandle.area = area;
                        celHandle.status = CelHandleInfo.CelHandleLifetime.kFrameLifetime;
                    }
                }
                else if (celHandle.status == CelHandleInfo.CelHandleLifetime.kNoCel)
                {
                    _segMan.AllocateBitmap(out celHandle.bitmapId, celWidth, celHeight, 255, 0, 0, _xResolution, _yResolution, kRawPaletteSize, false, false);
                    celHandle.area = area;
                    celHandle.status = CelHandleInfo.CelHandleLifetime.kFrameLifetime;
                }
                else
                {

                    Error("Cel Handle has bad status");
                }

                rawVideoData += kCelHeaderSize + dataSize;
            }

            if (maxFrameArea > _celDecompressionBuffer.Size)
            {
                _celDecompressionBuffer.Reserve(maxFrameArea);
            }
        }

        /// <summary>
        /// Reads audio data for the given frame number
        /// into the given buffer.
        /// </summary>
        /// <param name="frameNo"></param>
        /// <param name="outBuffer"></param>
        /// <param name="outAudioPosition">The position of the
        /// audio, in compressed bytes, in the data stream.</param>
        /// <param name="outAudioSize">The size of the audio data,
        /// in compressed bytes.</param>
        /// <returns></returns>
        private bool ReadAudioDataFromRecord(int frameNo, byte[] outBuffer, out int outAudioPosition, out int outAudioSize)
        {
            _fileStream.BaseStream.Seek(_recordPositions[frameNo] + _videoSizes[frameNo], SeekOrigin.Begin);
            _audioList.SubmitDriverMax();

            // Compressed absolute position of the audio block in the audio stream
            int position = _fileStream.ReadInt32();

            // Size of the block of audio, excluding the audio block header
            int size = _fileStream.ReadInt32();

            System.Diagnostics.Debug.Assert(size <= _expectedAudioBlockSize);

            if (position == 0)
            {
                outAudioPosition = 0;
                outAudioSize = 0;
                return false;
            }

            if (size != _expectedAudioBlockSize)
            {
                Array.Clear(outBuffer, 0, kRobotZeroCompressSize);
                _fileStream.BaseStream.Read(outBuffer, kRobotZeroCompressSize, size);
                size += kRobotZeroCompressSize;
            }
            else
            {
                _fileStream.BaseStream.Read(outBuffer, 0, size);
            }

            outAudioPosition = position;
            outAudioSize = size;
            return true;
        }
    }
}