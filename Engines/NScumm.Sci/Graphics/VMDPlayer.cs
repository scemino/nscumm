//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Video;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
#if ENABLE_SCI32
    /// <summary>
    /// VMDPlayer is used to play VMD videos.
    /// </summary>
    internal class VMDPlayer
    {
        [Flags]
        public enum OpenFlags
        {
            None = 0,
            Mute = 1
        }

        public enum IOStatus
        {
            Success = 0,
            Error = 0xFFFF
        }

        [Flags]
        public enum PlayFlags
        {
            None = 0,
            DoublePixels = 1,
            BlackLines = 4,
            Boost = 0x10,
            LeaveScreenBlack = 0x20,
            LeaveLastFrame = 0x40,
            BlackPalette = 0x80,
            StretchVertical = 0x100
        }

        [Flags]
        public enum EventFlags
        {
            None = 0,
            End = 1,
            EscapeKey = 2,
            MouseDown = 4,
            HotRectangle = 8,
            ToFrame = 0x10,
            YieldToVM = 0x20,
            Reverse = 0x80
        }

        /**
         * Whether or not a VMD stream has been opened with
         * `open`.
         */
        private bool _isOpen;

        /**
         * Whether or not a VMD player has been initialised
         * with `init`.
         */
        private bool _isInitialized;

        /**
         * For VMDs played with the `kEventFlagYieldToVM` flag,
         * the number of frames that should be rendered until
         * yielding back to the SCI VM.
         */
        private int _yieldInterval;

        /**
         * For VMDs played with the `kEventFlagYieldToVM` flag,
         * the last frame when control of the main thread was
         * yielded back to the SCI VM.
         */
        private int _lastYieldedFrameNo;

        /**
	 * The location of the VMD plane, in game script
	 * coordinates.
	 */
        private short _x, _y;

        /**
         * The plane where the VMD will be drawn.
         */
        private Plane _plane;

        /**
         * The screen item representing the VMD surface.
         */
        private ScreenItem _screenItem;

        // TODO: planeIsOwned and priority are used in SCI3+ only

        /**
         * If true, the plane for this VMD was set
         * externally and is not owned by this VMDPlayer.
         */
        private bool _planeIsOwned;

        /**
         * The screen priority of the video.
         * @see ScreenItem::_priority
         */
        private int _priority;

        /**
         * Whether or not the video should be pixel doubled.
         */
        private bool _doublePixels;

        /**
         * Whether or not the video should be pixel doubled
         * vertically only.
         */
        private bool _stretchVertical;

        /**
         * Whether or not black lines should be rendered
         * across the video.
         */
        private bool _blackLines;

        /**
         * Whether or not the playback area of the VMD
         * should be left black at the end of playback.
         */
        private bool _leaveScreenBlack;

        /**
         * Whether or not the area of the VMD should be left
         * displaying the final frame of the video.
         */
        private bool _leaveLastFrame;

        /**
	 * The dimensions of the blackout plane.
	 */
        private Rect _blackoutRect;

        /**
         * An optional plane that will be used to black out
         * areas of the screen outside of the VMD surface.
         */
        private Plane _blackoutPlane;

        /**
	 * The first color in the system palette that the VMD
	 * can write to.
	 */
        private byte _startColor;

        /**
         * The last color in the system palette that the VMD
         * can write to.
         */
        private byte _endColor;

        /**
         * If true, video frames are rendered after a blank
         * palette is submitted to the palette manager,
         * which is then restored after the video pixels
         * have already been rendered.
         */
        private bool _blackPalette;

        /**
	 * The amount of brightness boost for the video.
	 * Values above 100 increase brightness; values below
	 * 100 reduce it.
	 */
        private short _boostPercent;

        /**
         * The first color in the palette that should be
         * brightness boosted.
         */
        private byte _boostStartColor;

        /**
         * The last color in the palette that should be
         * brightness boosted.
         */
        private byte _boostEndColor;

        /**
	 * Whether or not the mouse cursor should be shown
	 * during playback.
	 */
        private bool _showCursor;

        private SegManager _segMan;
        private EventManager _eventMan;
        private readonly AdvancedVMDDecoder _decoder;

        public VMDPlayer(SegManager segMan, EventManager eventMan)
        {
            _segMan = segMan;
            _eventMan = eventMan;
            _decoder = new AdvancedVMDDecoder(SoundType.SFX);
            _planeIsOwned = true;
            _endColor = 255;
            _boostPercent = 100;
            _boostEndColor = 255;
        }

        public bool ShowCursor
        {
            get { return _showCursor; }
            set { _showCursor = value; }
        }

        public void RestrictPalette(byte startColor, byte endColor)
        {
            _startColor = startColor;
            _endColor = endColor;
        }

        public IOStatus Open(string fileName, OpenFlags flags)
        {
            if (_isOpen)
            {
                Error("Attempted to play {0}, but another VMD was loaded", fileName);
            }

            if (!_decoder.LoadFile(fileName)) return IOStatus.Error;

            if ((flags & OpenFlags.Mute) != 0)
            {
                _decoder.SetVolume(0);
            }
            _isOpen = true;
            return IOStatus.Success;
        }

        public void Init(short x, short y, PlayFlags flags, short boostPercent, short boostStartColor,
            short boostEndColor)
        {
            _x = (short) (ResourceManager.GetSciVersion() >= SciVersion.V3 ? x : x & ~1);
            _y = y;
            _doublePixels = (flags & PlayFlags.DoublePixels) != 0;
            _blackLines = ConfigManager.Instance.Get<bool>("enable_black_lined_video") &&
                          (flags & PlayFlags.BlackLines) != 0;
            _boostPercent = (short) (100 + ((flags & PlayFlags.Boost) != 0 ? boostPercent : 0));
            _boostStartColor = (byte) ScummHelper.Clip(boostStartColor, 0, 255);
            _boostEndColor = (byte) ScummHelper.Clip(boostEndColor, 0, 255);
            _leaveScreenBlack = (flags & PlayFlags.LeaveScreenBlack) != 0;
            _leaveLastFrame = (flags & PlayFlags.LeaveLastFrame) != 0;
            _blackPalette = (flags & PlayFlags.BlackPalette) != 0;
            _stretchVertical = (flags & PlayFlags.StretchVertical) != 0;
        }

        public IOStatus Close()
        {
            if (!_isOpen)
            {
                return IOStatus.Success;
            }

            _decoder.Close();
            _isOpen = false;
            _isInitialized = false;

            if (!_planeIsOwned && _screenItem != null)
            {
                SciEngine.Instance._gfxFrameout.DeleteScreenItem(_screenItem);
                _screenItem = null;
            }
            else if (_plane != null)
            {
                SciEngine.Instance._gfxFrameout.DeletePlane(_plane);
                _plane = null;
            }

            if (!_leaveLastFrame && _leaveScreenBlack)
            {
                // This call *actually* deletes the plane/screen item
                SciEngine.Instance._gfxFrameout.FrameOut(true);
            }

            if (_blackoutPlane != null)
            {
                SciEngine.Instance._gfxFrameout.DeletePlane(_blackoutPlane);
                _blackoutPlane = null;
            }

            if (!_leaveLastFrame && !_leaveScreenBlack)
            {
                // This call *actually* deletes the blackout plane
                SciEngine.Instance._gfxFrameout.FrameOut(true);
            }

            if (!_showCursor)
            {
                SciEngine.Instance._gfxCursor.KernelShow();
            }

            _lastYieldedFrameNo = 0;
            _planeIsOwned = true;
            _priority = 0;
            return IOStatus.Success;
        }

        public EventFlags KernelPlayUntilEvent(EventFlags flags, short lastFrameNo, short yieldInterval)
        {
            System.Diagnostics.Debug.Assert(lastFrameNo >= -1);

            var maxFrameNo = _decoder.GetFrameCount() - 1;

            if (((flags & EventFlags.ToFrame) != 0) && lastFrameNo > 0)
            {
                _decoder.SetEndFrame((uint) Math.Min(lastFrameNo, maxFrameNo));
            }
            else
            {
                _decoder.SetEndFrame((uint) maxFrameNo);
            }

            if ((flags & EventFlags.YieldToVM) != 0)
            {
                _yieldInterval = 3;
                if (yieldInterval == -1 && (flags & EventFlags.ToFrame) == 0)
                {
                    _yieldInterval = lastFrameNo;
                }
                else if (yieldInterval != -1)
                {
                    _yieldInterval = Math.Min(yieldInterval, maxFrameNo);
                }
            }
            else
            {
                _yieldInterval = maxFrameNo;
            }

            return PlayUntilEvent(flags);
        }

        private EventFlags PlayUntilEvent(EventFlags flags)
        {
            // Flushing all the keyboard and mouse events out of the event manager to
            // avoid letting any events queued from before the video started from
            // accidentally activating an event callback
            for (;;)
            {
                var @event = _eventMan.GetSciEvent(SciEvent.SCI_EVENT_KEYBOARD | SciEvent.SCI_EVENT_MOUSE_PRESS |
                                                   SciEvent.SCI_EVENT_MOUSE_RELEASE | SciEvent.SCI_EVENT_QUIT);
                if (@event.type == SciEvent.SCI_EVENT_NONE)
                {
                    break;
                }
                if (@event.type == SciEvent.SCI_EVENT_QUIT)
                {
                    return EventFlags.End;
                }
            }

            _decoder.PauseVideo(false);

            if ((flags & EventFlags.Reverse) != 0)
            {
                // NOTE: This flag may not work properly since SSCI does not care
                // if a video has audio, but the VMD decoder does.
                var success = _decoder.SetReverse(true);
                System.Diagnostics.Debug.Assert(success);
                _decoder.SetVolume(0);
            }

            if (!_isInitialized)
            {
                _isInitialized = true;

                if (!_showCursor)
                {
                    SciEngine.Instance._gfxCursor.KernelHide();
                }

                var vmdRect = new Rect(_x, _y, (short) (_x + _decoder.GetWidth()), (short) (_y + _decoder.GetHeight()));
                var vmdScaleInfo = new ScaleInfo();

                if (!_blackoutRect.IsEmpty && _planeIsOwned)
                {
                    _blackoutPlane = new Plane(_blackoutRect);
                    SciEngine.Instance._gfxFrameout.AddPlane(_blackoutPlane);
                }

                if (_doublePixels)
                {
                    vmdScaleInfo.x = 256;
                    vmdScaleInfo.y = 256;
                    vmdScaleInfo.signal = ScaleSignals32.kScaleSignalManual;
                    vmdRect.Right += vmdRect.Width;
                    vmdRect.Bottom += vmdRect.Height;
                }
                else if (_stretchVertical)
                {
                    vmdScaleInfo.y = 256;
                    vmdScaleInfo.signal = ScaleSignals32.kScaleSignalManual;
                    vmdRect.Bottom += vmdRect.Height;
                }

                var screenWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
                var screenHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;
                var scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
                var scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

                Register bitmapId = new Register();
                SciBitmap vmdBitmap = _segMan.AllocateBitmap(out bitmapId, vmdRect.Width, vmdRect.Height, 255, 0, 0,
                    screenWidth, screenHeight, 0, false, false);

                if (screenWidth != scriptWidth || screenHeight != scriptHeight)
                {
                    var r1 = new Rational(scriptWidth, screenWidth);
                    var r2 = new Rational(scriptHeight, screenHeight);
                    Helpers.Mulru(ref vmdRect, ref r1, ref r2, 1);
                }

                var vmdCelInfo = new CelInfo32();
                vmdCelInfo.bitmap = vmdBitmap.Object;
                _decoder.SetSurfaceMemory(vmdBitmap.Pixels, vmdBitmap.Width, vmdBitmap.Height, 1);

                if (_planeIsOwned)
                {
                    _x = 0;
                    _y = 0;
                    _plane = new Plane(vmdRect, PlanePictureCodes.kPlanePicColored);
                    if (_priority != 0)
                    {
                        _plane._priority = (short) _priority;
                    }
                    SciEngine.Instance._gfxFrameout.AddPlane(_plane);
                    _screenItem = new ScreenItem(_plane._object, vmdCelInfo, new Point(), vmdScaleInfo);
                }
                else
                {
                    _screenItem = new ScreenItem(_plane._object, vmdCelInfo, new Point(_x, _y), vmdScaleInfo);
                    if (_priority != 0)
                    {
                        _screenItem._priority = (short) _priority;
                    }
                }

                if (_blackLines)
                {
                    _screenItem._drawBlackLines = true;
                }

                // NOTE: There was code for positioning the screen item using insetRect
                // here, but none of the game scripts seem to use this functionality.

                SciEngine.Instance._gfxFrameout.AddScreenItem(_screenItem);

                _decoder.Start();
            }

            var stopFlag = EventFlags.None;
            while (!SciEngine.Instance.ShouldQuit)
            {
                if (_decoder.EndOfVideo)
                {
                    stopFlag = EventFlags.End;
                    break;
                }

                SciEngine.Instance.EngineState.SpeedThrottler((int) _decoder.GetTimeToNextFrame());
                SciEngine.Instance.EngineState._throttleTrigger = true;
                if (_decoder.NeedsUpdate)
                {
                    RenderFrame();
                }

                var currentFrameNo = _decoder.CurrentFrame;

                if (_yieldInterval > 0 &&
                    currentFrameNo != _lastYieldedFrameNo &&
                    currentFrameNo % _yieldInterval == 0
                )
                {
                    _lastYieldedFrameNo = currentFrameNo;
                    stopFlag = EventFlags.YieldToVM;
                    break;
                }

                var @event = _eventMan.GetSciEvent(SciEvent.SCI_EVENT_MOUSE_PRESS | SciEvent.SCI_EVENT_PEEK);
                if (((flags & EventFlags.MouseDown) != 0) && @event.type == SciEvent.SCI_EVENT_MOUSE_PRESS)
                {
                    stopFlag = EventFlags.MouseDown;
                    break;
                }

                @event = _eventMan.GetSciEvent(SciEvent.SCI_EVENT_KEYBOARD | SciEvent.SCI_EVENT_PEEK);
                if (((flags & EventFlags.EscapeKey) != 0) && @event.type == SciEvent.SCI_EVENT_KEYBOARD)
                {
                    var stop = false;
                    if (ResourceManager.GetSciVersion() < SciVersion.V3)
                    {
                        while ((@event = _eventMan.GetSciEvent(SciEvent.SCI_EVENT_KEYBOARD)) != null &&
                               @event.type != SciEvent.SCI_EVENT_NONE)
                        {
                            if (@event.character == SciEvent.SCI_KEY_ESC)
                            {
                                stop = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        stop = @event.character == SciEvent.SCI_KEY_ESC;
                    }

                    if (stop)
                    {
                        stopFlag = EventFlags.EscapeKey;
                        break;
                    }
                }

                // TODO: Hot rectangles
                if (((flags & EventFlags.HotRectangle) != 0) /* && event.type == SCI_EVENT_HOT_RECTANGLE */)
                {
                    Warning("Hot rectangles not implemented in VMD player");
                    stopFlag = EventFlags.HotRectangle;
                    break;
                }
            }

            _decoder.PauseVideo(true);
            return stopFlag;
        }

        private void RenderFrame()
        {
            // This writes directly to the CelObjMem we already created,
            // so no need to take its return value
            _decoder.DecodeNextFrame();

            // NOTE: Normally this would write a hunk palette at the end of the
            // video bitmap that CelObjMem would read out and submit, but instead
            // we are just submitting it directly here because the decoder exposes
            // this information a little bit differently than the one in SSCI
            bool dirtyPalette = _decoder.HasDirtyPalette;
            if (dirtyPalette)
            {
                var palette = new Palette();
                palette.timestamp = (int) SciEngine.Instance.TickCount;
                if (_blackPalette)
                {
                    for (var i = _startColor; i <= _endColor; ++i)
                    {
                        palette.colors[i].R = palette.colors[i].G = palette.colors[i].B = 0;
                        palette.colors[i].used = 1;
                    }
                }
                else
                {
                    FillPalette(palette);
                }

                SciEngine.Instance._gfxPalette32.Submit(palette);
                SciEngine.Instance._gfxFrameout.UpdateScreenItem(_screenItem);
                SciEngine.Instance._gfxFrameout.FrameOut(true);

                if (_blackPalette)
                {
                    FillPalette(palette);
                    SciEngine.Instance._gfxPalette32.Submit(palette);
                    SciEngine.Instance._gfxPalette32.UpdateForFrame();
                    SciEngine.Instance._gfxPalette32.UpdateHardware();
                }
            }
            else
            {
                SciEngine.Instance._gfxFrameout.UpdateScreenItem(_screenItem);
                //TODO: SciEngine.Instance.SciDebugger.OnFrame();
                SciEngine.Instance._gfxFrameout.FrameOut(true);
                SciEngine.Instance._gfxFrameout.Throttle();
            }
        }

        private void FillPalette(Palette palette)
        {
            var vmdPalette = new Ptr<byte>(_decoder.Palette, _startColor);
            for (var i = _startColor; i <= _endColor; ++i)
            {
                short r = vmdPalette.Value;
                vmdPalette.Offset++;
                short g = vmdPalette.Value;
                vmdPalette.Offset++;
                short b = vmdPalette.Value;
                vmdPalette.Offset++;

                if (_boostPercent != 100 && i >= _boostStartColor && i <= _boostEndColor)
                {
                    r = (short) ScummHelper.Clip(r * _boostPercent / 100, 0, 255);
                    g = (short) ScummHelper.Clip(g * _boostPercent / 100, 0, 255);
                    b = (short) ScummHelper.Clip(b * _boostPercent / 100, 0, 255);
                }

                palette.colors[i].R = (byte) r;
                palette.colors[i].G = (byte) g;
                palette.colors[i].B = (byte) b;
                palette.colors[i].used = 1;
            }
        }

        /// <summary>
        /// Sets the area of the screen that should be blacked out
        /// during VMD playback.
        /// </summary>
        /// <param name="rect"></param>
        public void SetBlackoutArea(Rect rect)
        {
            _blackoutRect = rect;
        }
    }
#endif
}