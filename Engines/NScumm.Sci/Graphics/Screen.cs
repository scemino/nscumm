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

using NScumm.Core.IO;
using System;
using NScumm.Core.Graphics;
using NScumm.Core;
using NScumm.Core.Common;

namespace NScumm.Sci.Graphics
{
    enum GfxScreenUpscaledMode
    {
        DISABLED = 0,
        S480x300 = 1,
        S640x400 = 2,
        S640x440 = 3,
        S640x480 = 4
    }

    [Flags]
    enum GfxScreenMasks
    {
        VISUAL = 1,
        PRIORITY = 2,
        CONTROL = 4,
        DISPLAY = 8, // not official sierra sci, only used internally
        ALL = VISUAL | PRIORITY | CONTROL
    }

    class GfxScreen
    {
        public interface IVectorAdjustCoordinate
        {
            void Execute(ref short x, ref short y);
        }

        class VectorAdjustCoordinateNOP : IVectorAdjustCoordinate
        {
            public void Execute(ref short x, ref short y)
            {
            }
        }

        class VectorAdjustCoordinate480x300Mac : IVectorAdjustCoordinate
        {
            private GfxScreen _screen;

            public VectorAdjustCoordinate480x300Mac(GfxScreen screen)
            {
                _screen = screen;
            }

            public void Execute(ref short x, ref short y)
            {
                x = _screen._upscaledWidthMapping[x];
                y = _screen._upscaledHeightMapping[y];
            }
        }

        public interface IVectorAdjustLineCoordinates
        {
            void Execute(ref short left, ref short top, ref short right, ref short bottom, GfxScreenMasks drawMask, byte color, byte priority, byte control);
        }

        class VectorAdjustLineCoordinatesNOP : IVectorAdjustLineCoordinates
        {
            public void Execute(ref short left, ref short top, ref short right, ref short bottom, GfxScreenMasks drawMask, byte color, byte priority, byte control)
            {
            }
        }

        class VectorAdjustLineCoordinates480x300Mac : IVectorAdjustLineCoordinates
        {
            private GfxScreen _screen;

            public VectorAdjustLineCoordinates480x300Mac(GfxScreen screen)
            {
                _screen = screen;
            }

            public void Execute(ref short left, ref short top, ref short right, ref short bottom, GfxScreenMasks drawMask, byte color, byte priority, byte control)
            {
                short displayLeft = _screen._upscaledWidthMapping[left];
                short displayRight = _screen._upscaledWidthMapping[right];
                short displayTop = _screen._upscaledHeightMapping[top];
                short displayBottom = _screen._upscaledHeightMapping[bottom];

                if (displayLeft < displayRight)
                {
                    // one more pixel to the left, one more pixel to the right
                    if (displayLeft > 0)
                    {
                        _screen.VectorPutLinePixel((short)(displayLeft - 1), displayTop, drawMask, color, priority, control);
                    }
                    _screen.VectorPutLinePixel((short)(displayRight + 1), displayBottom, drawMask, color, priority, control);
                }
                else if (displayLeft > displayRight)
                {
                    if (displayRight > 0)
                    {
                        _screen.VectorPutLinePixel((short)(displayRight - 1), displayBottom, drawMask, color, priority, control);
                    }
                    _screen.VectorPutLinePixel((short)(displayLeft + 1), displayTop, drawMask, color, priority, control);
                }
                left = displayLeft;
                top = displayTop;
                right = displayRight;
                bottom = displayBottom;
            }
        }

        struct UpScaledAdjust
        {
            public GfxScreenUpscaledMode gameHiresMode;
            public Sci32ViewNativeResolution viewNativeRes;
            public int numerator;
            public int denominator;
        }

        public const int SCI_SCREEN_UPSCALEDMAXHEIGHT = 200;
        public const int SCI_SCREEN_UPSCALEDMAXWIDTH = 320;

        public const int DITHERED_BG_COLORS_SIZE = 256;

        private ResourceManager _resMan;
        private GfxScreenUpscaledMode _upscaledHires;

        private ushort _width;
        private ushort _height;
        private uint _pixels;
        private ushort _scriptWidth;
        private ushort _scriptHeight;
        private ushort _displayWidth;
        private ushort _displayHeight;
        private uint _displayPixels;

        private byte _colorWhite;
        private byte _colorDefaultVectorData;

        /// <summary>
        /// This here holds a translation for vertical+horizontal coordinates between native
        /// (visual) and actual (display) screen.
        /// </summary>
        private short[] _upscaledHeightMapping = new short[SCI_SCREEN_UPSCALEDMAXHEIGHT + 1];
        private short[] _upscaledWidthMapping = new short[SCI_SCREEN_UPSCALEDMAXWIDTH + 1];

        // These screens have the real resolution of the game engine (320x200 for
        // SCI0/SCI1/SCI11 games, 640x480 for SCI2 games). SCI0 games will be
        // dithered in here at any time.
        private byte[] _visualScreen;
        private byte[] _priorityScreen;
        private byte[] _controlScreen;

        /// <summary>
        /// This screen is the one that is actually displayed to the user. It may be
        /// 640x400 for japanese SCI1 games. SCI0 games may be undithered in here.
        /// Only read from this buffer for Save/ShowBits usage.
        /// </summary>
        private byte[] _displayScreen;

        /// <summary>
        /// Pointer to the currently active screen (changing it only required for
        /// debug purposes).
        /// </summary>
        private byte[] _activeScreen;

        /// <summary>
        /// If this flag is true, undithering is enabled, otherwise disabled.
        /// </summary>
        private bool _unditheringEnabled;
        private short[] _ditheredPicColors = new short[DITHERED_BG_COLORS_SIZE];

        public int _picNotValid; // possible values 0, 1 and 2
        private int _picNotValidSci11; // another variable that is used by kPicNotValid in sci1.1

        /// <summary>
        /// This defines whether or not the font we're drawing is already scaled
        /// to the screen size (and we therefore should not upscale it ourselves).
        /// </summary>
        private bool _fontIsUpscaled;
        private IVectorAdjustCoordinate _vectorAdjustCoordinatePtr;
        private Func<short, short, GfxScreenMasks, byte, byte, byte, bool, GfxScreenMasks> _vectorIsFillMatchPtr;
        private IVectorAdjustLineCoordinates _vectorAdjustLineCoordinatesPtr;
        private Action<short, short, GfxScreenMasks, byte, byte, byte> _vectorPutPixelPtr;
        private Action<short, short, GfxScreenMasks, byte, byte, byte> _vectorPutLinePixelPtr;
        private Action<short, short, GfxScreenMasks, byte, byte, byte> _putPixelPtr;
        private Func<byte[], short, short, byte> _vectorGetPixelPtr;
        private Func<byte[], short, short, byte> _getPixelPtr;
        private ISystem _system;

        private static readonly UpScaledAdjust[] s_upscaledAdjustTable = {
            new UpScaledAdjust { gameHiresMode = GfxScreenUpscaledMode.S640x480, viewNativeRes = Sci32ViewNativeResolution.R640x400, numerator = 5, denominator = 6 }
        };

        public GfxScreenUpscaledMode UpscaledHires { get { return _upscaledHires; } }

        public ushort ScriptWidth { get { return _scriptWidth; } }

        public ushort ScriptHeight { get { return _scriptHeight; } }

        public int DisplayHeight { get { return _displayHeight; } }

        public int DisplayWidth { get { return _displayWidth; } }

        public int Height { get { return _height; } }

        public byte ColorWhite { get { return _colorWhite; } }

        public bool IsUnditheringEnabled { get { return _unditheringEnabled; } }

        public byte ColorDefaultVectorData { get { return _colorDefaultVectorData; } }

        public GfxScreen(ISystem system, ResourceManager resMan)
        {
            _system = system;
            _resMan = resMan;

            // Scale the screen, if needed
            _upscaledHires = GfxScreenUpscaledMode.DISABLED;

            // we default to scripts running at 320x200
            _scriptWidth = 320;
            _scriptHeight = 200;
            _width = 0;
            _height = 0;
            _displayWidth = 0;
            _displayHeight = 0;

            // King's Quest 6 and Gabriel Knight 1 have hires content, gk1/cd was able
            // to provide that under DOS as well, but as gk1/floppy does support
            // upscaled hires scriptswise, but doesn't actually have the hires content
            // we need to limit it to platform windows.
            if (SciEngine.Instance.Platform == Platform.Windows)
            {
                if (SciEngine.Instance.GameId == SciGameId.KQ6)
                    _upscaledHires = GfxScreenUpscaledMode.S640x440;
#if ENABLE_SCI32
                if (SciEngine.Instance.getGameId() == GID_GK1)
                    _upscaledHires = GFX_SCREEN_UPSCALED_640x480;
#endif
            }

            // Japanese versions of games use hi-res font on upscaled version of the game.
            if ((SciEngine.Instance.Language == Core.Common.Language.JA_JPN) && (ResourceManager.GetSciVersion() <= SciVersion.V1_1))
                _upscaledHires = GfxScreenUpscaledMode.S640x400;

            // Macintosh SCI0 games used 480x300, while the scripts were running at 320x200
            if (SciEngine.Instance.Platform == Platform.Macintosh)
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V01)
                {
                    _upscaledHires = GfxScreenUpscaledMode.S480x300;
                    _width = 480;
                    _height = 300; // regular visual, priority and control map are 480x300 (this is different than other upscaled SCI games)
                }

                // Some Mac SCI1/1.1 games only take up 190 rows and do not
                // have the menu bar.
                // TODO: Verify that LSL1 and LSL5 use height 190
                switch (SciEngine.Instance.GameId)
                {
                    case SciGameId.FREDDYPHARKAS:
                    case SciGameId.KQ5:
                    case SciGameId.KQ6:
                    case SciGameId.LSL1:
                    case SciGameId.LSL5:
                    case SciGameId.SQ1:
                        _scriptHeight = 190;
                        break;
                    default:
                        break;
                }
            }

#if ENABLE_SCI32
            // GK1 Mac uses a 640x480 resolution too
            if (SciEngine.Instance.getPlatform() == Common::kPlatformMacintosh)
            {
                if (SciEngine.Instance.getGameId() == GID_GK1)
                    _upscaledHires = GFX_SCREEN_UPSCALED_640x480;
            }
#endif

            if (_resMan.DetectHires())
            {
                _scriptWidth = 640;
                _scriptHeight = 480;
            }

#if ENABLE_SCI32
            // Phantasmagoria 1 effectively outputs 630x450
            //  Coordinate translation has to use this resolution as well
            if (SciEngine.Instance.getGameId() == GID_PHANTASMAGORIA)
            {
                _width = 630;
                _height = 450;
            }
#endif

            // if not yet set, set those to script-width/height
            if (_width == 0)
                _width = _scriptWidth;
            if (_height == 0)
                _height = _scriptHeight;

            _pixels = (uint)(_width * _height);

            switch (_upscaledHires)
            {
                case GfxScreenUpscaledMode.S480x300:
                    // Space Quest 3, Hoyle 1+2 on MAC use this one
                    _displayWidth = 480;
                    _displayHeight = 300;
                    for (int i = 0; i <= _scriptHeight; i++)
                        _upscaledHeightMapping[i] = (short)((i * 3) >> 1);
                    for (int i = 0; i <= _scriptWidth; i++)
                        _upscaledWidthMapping[i] = (short)((i * 3) >> 1);
                    break;
                case GfxScreenUpscaledMode.S640x400:
                    // Police Quest 2 and Quest For Glory on PC9801 (Japanese)
                    _displayWidth = 640;
                    _displayHeight = 400;
                    for (int i = 0; i <= _scriptHeight; i++)
                        _upscaledHeightMapping[i] = (short)(i * 2);
                    for (int i = 0; i <= _scriptWidth; i++)
                        _upscaledWidthMapping[i] = (short)(i * 2);
                    break;
                case GfxScreenUpscaledMode.S640x440:
                    // used by King's Quest 6 on Windows
                    _displayWidth = 640;
                    _displayHeight = 440;
                    for (int i = 0; i <= _scriptHeight; i++)
                        _upscaledHeightMapping[i] = (short)((i * 11) / 5);
                    for (int i = 0; i <= _scriptWidth; i++)
                        _upscaledWidthMapping[i] = (short)(i * 2);
                    break;
                case GfxScreenUpscaledMode.S640x480:
                    // Gabriel Knight 1 (VESA, Mac)
                    _displayWidth = 640;
                    _displayHeight = 480;
                    for (int i = 0; i <= _scriptHeight; i++)
                        _upscaledHeightMapping[i] = (short)((i * 12) / 5);
                    for (int i = 0; i <= _scriptWidth; i++)
                        _upscaledWidthMapping[i] = (short)(i * 2);
                    break;
                default:
                    if (_displayWidth == 0)
                        _displayWidth = _width;
                    if (_displayHeight == 0)
                        _displayHeight = _height;
                    Array.Clear(_upscaledHeightMapping, 0, _upscaledHeightMapping.Length);
                    Array.Clear(_upscaledWidthMapping, 0, _upscaledWidthMapping.Length);
                    break;
            }

            _displayPixels = (uint)(_displayWidth * _displayHeight);

            // Allocate visual, priority, control and display screen
            _visualScreen = new byte[(int)_pixels];
            _priorityScreen = new byte[(int)_pixels];
            _controlScreen = new byte[(int)_pixels];
            _displayScreen = new byte[(int)_displayPixels];

            Array.Clear(_ditheredPicColors, 0, _ditheredPicColors.Length);

            // Sets display screen to be actually displayed
            _activeScreen = _displayScreen;

            _picNotValid = 0;
            _picNotValidSci11 = 0;
            _unditheringEnabled = true;
            _fontIsUpscaled = false;

            if (_resMan.ViewType != ViewType.Ega)
            {
                // It is not 100% accurate to set white to be 255 for Amiga 32-color
                // games. But 255 is defined as white in our SCI at all times, so it
                // doesn't matter.
                _colorWhite = 255;
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                    _colorDefaultVectorData = 255;
                else
                    _colorDefaultVectorData = 0;
            }
            else {
                _colorWhite = 15;
                _colorDefaultVectorData = 0;
            }

            // Initialize the actual screen

            if (SciEngine.Instance.HasMacIconBar)
            {
                // For SCI1.1 Mac games with the custom icon bar, we need to expand the screen
                // to accommodate for the icon bar. Of course, both KQ6 and QFG1 VGA differ in size.
                // We add 2 to the height of the icon bar to add a buffer between the screen and the
                // icon bar (as did the original interpreter).
                if (SciEngine.Instance.GameId == SciGameId.KQ6)
                    InitGraphics(_displayWidth, _displayHeight + 26 + 2, _displayWidth > 320);
                else if (SciEngine.Instance.GameId == SciGameId.FREDDYPHARKAS)
                    InitGraphics(_displayWidth, _displayHeight + 28 + 2, _displayWidth > 320);
                else
                    throw new InvalidOperationException("Unknown SCI1.1 Mac game");
            }
            else
                InitGraphics(_displayWidth, _displayHeight, _displayWidth > 320);

            // Initialize code pointers
            _vectorAdjustCoordinatePtr = new VectorAdjustCoordinateNOP();
            _vectorAdjustLineCoordinatesPtr = new VectorAdjustLineCoordinatesNOP();
            _vectorIsFillMatchPtr = VectorIsFillMatchNormal;
            _vectorPutPixelPtr = PutPixelNormal;
            _vectorPutLinePixelPtr = PutPixel;
            _vectorGetPixelPtr = GetPixelNormal;
            _putPixelPtr = PutPixelNormal;
            _getPixelPtr = GetPixelNormal;

            switch (_upscaledHires)
            {
                case GfxScreenUpscaledMode.S480x300:
                    _vectorAdjustCoordinatePtr = new VectorAdjustCoordinate480x300Mac(this);
                    _vectorAdjustLineCoordinatesPtr = new VectorAdjustLineCoordinates480x300Mac(this);
                    // vectorPutPixel . we already adjust coordinates for vector code, that's why we can set pixels directly
                    // vectorGetPixel . see vectorPutPixel
                    _vectorPutLinePixelPtr = VectorPutLinePixel480x300Mac;
                    _putPixelPtr = PutPixelAllUpscaled;
                    _getPixelPtr = GetPixelUpscaled;
                    break;
                case GfxScreenUpscaledMode.S640x400:
                case GfxScreenUpscaledMode.S640x440:
                case GfxScreenUpscaledMode.S640x480:
                    _vectorPutPixelPtr = PutPixelDisplayUpscaled;
                    _putPixelPtr = PutPixelDisplayUpscaled;
                    break;
                case GfxScreenUpscaledMode.DISABLED:
                    break;
            }
        }

        internal void DrawLine(Point startPoint, Point endPoint, byte pic_color, byte pic_priority, byte pic_control)
        {
            throw new NotImplementedException();
        }

        public byte GetVisual(short x, short y)
        {
            return _getPixelPtr(_visualScreen, x, y);
        }

        public byte GetControl(short x, short y)
        {
            return _getPixelPtr(_controlScreen, x, y);
        }

        public byte GetPriority(short x, short y)
        {
            return _getPixelPtr(_priorityScreen, x, y);
        }

        public void BitsSave(Rect rect, GfxScreenMasks mask, byte[] memoryPtr)
        {
            throw new NotImplementedException();
            //memcpy(memoryPtr, (void*)&rect, sizeof(rect)); memoryPtr += sizeof(rect);
            //memcpy(memoryPtr, (void*)&mask, sizeof(mask)); memoryPtr += sizeof(mask);

            //if (mask.HasFlag(GfxScreenMasks.VISUAL))
            //{
            //    BitsSaveScreen(rect, _visualScreen, _width, memoryPtr);
            //    BitsSaveDisplayScreen(rect, memoryPtr);
            //}
            //if (mask.HasFlag(GfxScreenMasks.PRIORITY))
            //{
            //    BitsSaveScreen(rect, _priorityScreen, _width, memoryPtr);
            //}
            //if (mask.HasFlag(GfxScreenMasks.CONTROL))
            //{
            //    BitsSaveScreen(rect, _controlScreen, _width, memoryPtr);
            //}
            //if (mask.HasFlag(GfxScreenMasks.DISPLAY))
            //{
            //    if (_upscaledHires == GfxScreenUpscaledMode.DISABLED)
            //        throw new InvalidOperationException("bitsSave() called w/o being in upscaled hires mode");
            //    BitsSaveScreen(rect, _displayScreen, _displayWidth, memoryPtr);
            //}
        }

        public void CopyRectToScreen(Rect rect)
        {
            if (_upscaledHires == GfxScreenUpscaledMode.DISABLED)
            {
                _system.GraphicsManager.CopyRectToScreen(_activeScreen, rect.Top * _displayWidth + rect.Left, _displayWidth, rect.Left, rect.Top, rect.Width, rect.Height);
            }
            else {
                int rectHeight = _upscaledHeightMapping[rect.Bottom] - _upscaledHeightMapping[rect.Top];
                int rectWidth = _upscaledWidthMapping[rect.Right] - _upscaledWidthMapping[rect.Left];
                _system.GraphicsManager.CopyRectToScreen(_activeScreen, _upscaledHeightMapping[rect.Top] * _displayWidth + _upscaledWidthMapping[rect.Left], _displayWidth, _upscaledWidthMapping[rect.Left], _upscaledHeightMapping[rect.Top], rectWidth, rectHeight);
            }
        }

        public int BitsGetDataSize(Rect rect, GfxScreenMasks mask)
        {
            int byteCount = ServiceLocator.Platform.SizeOf<Rect>() + ServiceLocator.Platform.SizeOf<GfxScreenMasks>();
            int pixels = rect.Width * rect.Height;
            if (mask.HasFlag(GfxScreenMasks.VISUAL))
            {
                byteCount += pixels; // _visualScreen
                if (_upscaledHires == GfxScreenUpscaledMode.DISABLED)
                {
                    byteCount += pixels; // _displayScreen
                }
                else {
                    int rectHeight = _upscaledHeightMapping[rect.Bottom] - _upscaledHeightMapping[rect.Top];
                    int rectWidth = _upscaledWidthMapping[rect.Right] - _upscaledWidthMapping[rect.Left];
                    byteCount += rectHeight * rect.Width * rectWidth; // _displayScreen (upscaled hires)
                }
            }
            if (mask.HasFlag(GfxScreenMasks.PRIORITY))
            {
                byteCount += pixels; // _priorityScreen
            }
            if (mask.HasFlag(GfxScreenMasks.CONTROL))
            {
                byteCount += pixels; // _controlScreen
            }
            if (mask.HasFlag(GfxScreenMasks.DISPLAY))
            {
                if (_upscaledHires == GfxScreenUpscaledMode.DISABLED)
                    throw new InvalidOperationException("bitsGetDataSize() called w/o being in upscaled hires mode");
                byteCount += pixels; // _displayScreen (coordinates actually are given to us for hires displayScreen)
            }

            return byteCount;
        }

        public void AdjustToUpscaledCoordinates(ref int y, ref int x, Sci32ViewNativeResolution viewNativeRes = Sci32ViewNativeResolution.NONE)
        {
            x = _upscaledWidthMapping[x];
            y = _upscaledHeightMapping[y];

            for (int i = 0; i < s_upscaledAdjustTable.Length; i++)
            {
                if (s_upscaledAdjustTable[i].gameHiresMode == _upscaledHires &&
                        s_upscaledAdjustTable[i].viewNativeRes == viewNativeRes)
                {
                    y = (y * s_upscaledAdjustTable[i].numerator) / s_upscaledAdjustTable[i].denominator;
                    break;
                }
            }
        }

        private void InitGraphics(int width, int height, bool defaultTo1xScaler)
        {
            // TODO: ? InitGraphics(width, height, defaultTo1xScaler, PixelFormat.Indexed8);
        }

        private byte GetPixelNormal(byte[] screen, short x, short y)
        {
            return screen[y * _width + x];
        }

        private byte GetPixelUpscaled(byte[] screen, short x, short y)
        {
            short mappedX = _upscaledWidthMapping[x];
            short mappedY = _upscaledHeightMapping[y];
            return screen[mappedY * _width + mappedX];
        }

        public void PutPixel(short x, short y, GfxScreenMasks drawMask, byte color, byte priority, byte control)
        {
            _putPixelPtr(x, y, drawMask, color, priority, control);
        }

        private void PutPixelNormal(short x, short y, GfxScreenMasks drawMask, byte color, byte priority, byte control)
        {
            int offset = y * _width + x;

            if (drawMask.HasFlag(GfxScreenMasks.VISUAL))
            {
                _visualScreen[offset] = color;
                _displayScreen[offset] = color;
            }
            if (drawMask.HasFlag(GfxScreenMasks.PRIORITY))
                _priorityScreen[offset] = priority;
            if (drawMask.HasFlag(GfxScreenMasks.CONTROL))
                _controlScreen[offset] = control;
        }

        // Directly sets a pixel on various screens, display IS upscaled
        private void PutPixelDisplayUpscaled(short x, short y, GfxScreenMasks drawMask, byte color, byte priority, byte control)
        {
            int offset = y * _width + x;

            if (drawMask.HasFlag(GfxScreenMasks.VISUAL))
            {
                _visualScreen[offset] = color;
                PutScaledPixelOnScreen(_displayScreen, x, y, color);
            }
            if (drawMask.HasFlag(GfxScreenMasks.PRIORITY))
                _priorityScreen[offset] = priority;
            if (drawMask.HasFlag(GfxScreenMasks.CONTROL))
                _controlScreen[offset] = control;

        }
        private void VectorPutLinePixel(short x, short y, GfxScreenMasks drawMask, byte color, byte priority, byte control)
        {
            _vectorPutLinePixelPtr(x, y, drawMask, color, priority, control);
        }

        // Special 480x300 Mac putPixel for vector line drawing, also draws an additional pixel below the actual one
        private void VectorPutLinePixel480x300Mac(short x, short y, GfxScreenMasks drawMask, byte color, byte priority, byte control)
        {
            int offset = y * _width + x;

            if (drawMask.HasFlag(GfxScreenMasks.VISUAL))
            {
                _visualScreen[offset] = color;
                _visualScreen[offset + _width] = color;
                _displayScreen[offset] = color;
                // also set pixel below actual pixel
                _displayScreen[offset + _displayWidth] = color;
            }
            if (drawMask.HasFlag(GfxScreenMasks.PRIORITY))
            {
                _priorityScreen[offset] = priority;
                _priorityScreen[offset + _width] = priority;
            }
            if (drawMask.HasFlag(GfxScreenMasks.CONTROL))
            {
                _controlScreen[offset] = control;
                _controlScreen[offset + _width] = control;
            }
        }

        // Directly sets a pixel on various screens, ALL screens ARE upscaled
        private void PutPixelAllUpscaled(short x, short y, GfxScreenMasks drawMask, byte color, byte priority, byte control)
        {
            if (drawMask.HasFlag(GfxScreenMasks.VISUAL))
            {
                PutScaledPixelOnScreen(_visualScreen, x, y, color);
                PutScaledPixelOnScreen(_displayScreen, x, y, color);
            }
            if (drawMask.HasFlag(GfxScreenMasks.PRIORITY))
                PutScaledPixelOnScreen(_priorityScreen, x, y, priority);
            if (drawMask.HasFlag(GfxScreenMasks.CONTROL))
                PutScaledPixelOnScreen(_controlScreen, x, y, control);
        }

        private void PutScaledPixelOnScreen(byte[] screen, short x, short y, byte data)
        {
            int displayOffset = _upscaledHeightMapping[y] * _displayWidth + _upscaledWidthMapping[x];
            int heightOffsetBreak = (_upscaledHeightMapping[y + 1] - _upscaledHeightMapping[y]) * _displayWidth;
            int heightOffset = 0;
            int widthOffsetBreak = _upscaledWidthMapping[x + 1] - _upscaledWidthMapping[x];
            do
            {
                int widthOffset = 0;
                do
                {
                    screen[displayOffset + heightOffset + widthOffset] = data;
                    widthOffset++;
                } while (widthOffset != widthOffsetBreak);
                heightOffset += _displayWidth;
            } while (heightOffset != heightOffsetBreak);
        }

        public void Dither(bool addToFlag)
        {
            int y, x;
            byte color, ditheredColor;
            var visualPtr = new ByteAccess(_visualScreen);
            var displayPtr = new ByteAccess(_displayScreen);

            if (!_unditheringEnabled)
            {
                // Do dithering on visual and display-screen
                for (y = 0; y < _height; y++)
                {
                    for (x = 0; x < _width; x++)
                    {
                        color = visualPtr[0];
                        if ((color & 0xF0) != 0)
                        {
                            color ^= (byte)(color << 4);
                            color = ((x ^ y) & 1) != 0 ? (byte)(color >> 4) : (byte)(color & 0x0F);
                            switch (_upscaledHires)
                            {
                                case GfxScreenUpscaledMode.DISABLED:
                                case GfxScreenUpscaledMode.S480x300:
                                    displayPtr[0] = color;
                                    break;
                                default:
                                    PutScaledPixelOnScreen(_displayScreen, (short)x, (short)y, color);
                                    break;
                            }
                            visualPtr[0] = color;
                        }
                        visualPtr.Offset++; displayPtr.Offset++;
                    }
                }
            }
            else {
                if (!addToFlag)
                {
                    Array.Clear(_ditheredPicColors, 0, _ditheredPicColors.Length);
                }
                // Do dithering on visual screen and put decoded but undithered byte onto display-screen
                for (y = 0; y < _height; y++)
                {
                    for (x = 0; x < _width; x++)
                    {
                        color = visualPtr[0];
                        if ((color & 0xF0) != 0)
                        {
                            color ^= (byte)(color << 4);
                            // remember dither combination for cel-undithering
                            _ditheredPicColors[color]++;
                            // if decoded color wants do dither with black on left side, we turn it around
                            //  otherwise the normal ega color would get used for display
                            if ((color & 0xF0) != 0)
                            {
                                ditheredColor = color;
                            }
                            else {
                                ditheredColor = (byte)(color << 4);
                            }
                            switch (_upscaledHires)
                            {
                                case GfxScreenUpscaledMode.DISABLED:
                                case GfxScreenUpscaledMode.S480x300:
                                    displayPtr[0] = ditheredColor;
                                    break;
                                default:
                                    PutScaledPixelOnScreen(_displayScreen, (short)x, (short)y, ditheredColor);
                                    break;
                            }
                            color = ((x ^ y) & 1) != 0 ? (byte)(color >> 4) : (byte)(color & 0x0F);
                            visualPtr[0] = color;
                        }
                        visualPtr.Offset++; displayPtr.Offset++;
                    }
                }
            }
        }

        internal void DitherForceDitheredColor(int v)
        {
            throw new NotImplementedException();
        }

        private GfxScreenMasks VectorIsFillMatchNormal(short x, short y, GfxScreenMasks screenMask, byte checkForColor, byte checkForPriority, byte checkForControl, bool isEGA)
        {
            int offset = y * _width + x;
            GfxScreenMasks match = 0;

            if (screenMask.HasFlag(GfxScreenMasks.VISUAL))
            {
                if (!isEGA)
                {
                    if (_visualScreen[offset] == checkForColor)
                        match |= GfxScreenMasks.VISUAL;
                }
                else {
                    // In EGA games a pixel in the framebuffer is only 4 bits. We store
                    // a full byte per pixel to allow undithering, but when comparing
                    // pixels for flood-fill purposes, we should only compare the
                    // visible color of a pixel.

                    byte EGAcolor = _visualScreen[offset];
                    if (((x ^ y) & 1) != 0)
                        EGAcolor = (byte)((EGAcolor ^ (EGAcolor >> 4)) & 0x0F);
                    else
                        EGAcolor = (byte)(EGAcolor & 0x0F);
                    if (EGAcolor == checkForColor)
                        match |= GfxScreenMasks.VISUAL;
                }
            }
            if (screenMask.HasFlag(GfxScreenMasks.PRIORITY) && _priorityScreen[offset] == checkForPriority)
                match |= GfxScreenMasks.PRIORITY;
            if (screenMask.HasFlag(GfxScreenMasks.CONTROL) && _controlScreen[offset] == checkForControl)
                match |= GfxScreenMasks.CONTROL;
            return match;
        }

        internal void CopyToScreen()
        {
            throw new NotImplementedException();
        }

        public GfxScreenMasks GetDrawingMask(byte color, byte priority, byte control)
        {
            GfxScreenMasks flag = 0;
            if (color != 255)
                flag |= GfxScreenMasks.VISUAL;
            if (priority != 255)
                flag |= GfxScreenMasks.PRIORITY;
            if (control != 255)
                flag |= GfxScreenMasks.CONTROL;
            return flag;
        }

        public void VectorAdjustCoordinate(ref short x, ref short y)
        {
            _vectorAdjustCoordinatePtr.Execute(ref x, ref y);
        }

        public byte VectorGetVisual(short x, short y)
        {
            return _vectorGetPixelPtr(_visualScreen, x, y);
        }

        public byte VectorGetControl(short x, short y)
        {
            return _vectorGetPixelPtr(_controlScreen, x, y);
        }

        public byte VectorGetPriority(short x, short y)
        {
            return _vectorGetPixelPtr(_priorityScreen, x, y);
        }

        public void VectorAdjustCoordinate(ref int x, ref int y)
        {
            short tmpx = (short)x;
            short tmpy = (short)y;
            _vectorAdjustCoordinatePtr.Execute(ref tmpx, ref tmpy);
            x = tmpx;
            y = tmpy;
        }

        public GfxScreenMasks VectorIsFillMatch(short x, short y, GfxScreenMasks screenMask, byte color, byte priority, byte control, bool isEGA)
        {
            return _vectorIsFillMatchPtr(x, y, screenMask, color, priority, control, isEGA);
        }

        public void VectorPutPixel(short x, short y, GfxScreenMasks drawMask, byte color, byte priority, byte control)
        {
            _vectorPutPixelPtr(x, y, drawMask, color, priority, control);
        }
    }
}
