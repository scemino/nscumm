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

        private IOStatus Open(string fileName, OpenFlags flags)
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

        private void Init(short x, short y, PlayFlags flags, short boostPercent, short boostStartColor,
            short boostEndColor)
        {
            _x = (short) (ResourceManager.GetSciVersion() >= SciVersion.V3 ? x : (x & ~1));
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

        private IOStatus Close()
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

    }
#endif
}