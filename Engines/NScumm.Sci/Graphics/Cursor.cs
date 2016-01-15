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
using NScumm.Core.Graphics;
using NScumm.Core;
using NScumm.Core.Common;
using NScumm.Sci.Engine;
using System.Collections.Generic;

namespace NScumm.Sci.Graphics
{
    internal class GfxCursor
    {
        private const int SCI_CURSOR_SCI0_HEIGHTWIDTH = 16;
        private const int SCI_CURSOR_SCI0_RESOURCESIZE = 68;
        private const int SCI_CURSOR_SCI0_TRANSPARENCYCOLOR = 1;

        struct SciCursorSetPositionWorkarounds
        {
            public SciGameId gameId;
            public short newPositionY;
            public short newPositionX;
            public short rectTop;
            public short rectLeft;
            public short rectBottom;
            public short rectRight;
        }

        private ResourceManager _resMan;
        private GfxScreen _screen;
        private GfxPalette _palette;
        private GfxCoordAdjuster _coordAdjuster;
        private EventManager _event;

        private GfxScreenUpscaledMode _upscaledHires;

        private bool _moveZoneActive;
        private Rect _moveZone; // Rectangle in which the pointer can move

        private bool _zoomZoneActive;
        private Rect _zoomZone;
        private GfxView _zoomCursorView;
        private byte _zoomCursorLoop;
        private byte _zoomCursorCel;
        private GfxView _zoomPicView;
        private byte _zoomColor;
        private byte _zoomMultiplier;
        private byte[] _cursorSurface;
        //private CursorCache _cachedCursors;
        private bool _isVisible;

        // KQ6 Windows has different black and white cursors. If this is true (set
        // from the windows_cursors ini setting), then we use these and don't scale
        // them by 2x like the rest of the graphics, like SSCI did. These look very
        // ugly, which is why they aren't enabled by default.
        private bool _useOriginalKQ6WinCursors;

        // The CD version of SQ4 contains a complete set of silver mouse cursors.
        // If this is true (set from the silver_cursors ini setting), then we use
        // these instead and replace the game's gold cursors with their silver
        // equivalents.
        private bool _useSilverSQ4CDCursors;

        // Mac versions of games use a remap list to remap their cursors
        private List<ushort> _macCursorRemap;

        // This list contains all mandatory set cursor changes, that need special handling
        // Refer to GfxCursor::setPosition() below
        //    Game,            newPosition,  validRect
        private static readonly SciCursorSetPositionWorkarounds[] setPositionWorkarounds = {
            new SciCursorSetPositionWorkarounds { gameId = SciGameId.ISLANDBRAIN, newPositionY =  84, newPositionX = 109, rectTop = 46, rectLeft =  76, rectBottom = 174, rectRight = 243 },  // Island of Dr. Brain, game menu
	        new SciCursorSetPositionWorkarounds { gameId = SciGameId.ISLANDBRAIN, newPositionY = 143, newPositionX = 135, rectTop = 57, rectLeft = 102, rectBottom = 163, rectRight = 218 },  // Island of Dr. Brain, pause menu within copy protection
	        new SciCursorSetPositionWorkarounds { gameId = SciGameId.LSL5,        newPositionY =  23, newPositionX = 171, rectTop =  0, rectLeft =   0, rectBottom =  26, rectRight = 320 },  // Larry 5, skip forward helper pop-up
	        new SciCursorSetPositionWorkarounds { gameId = SciGameId.QFG1VGA,     newPositionY =  64, newPositionX = 174, rectTop = 40, rectLeft =  37, rectBottom =  74, rectRight = 284 },  // Quest For Glory 1 VGA, run/walk/sleep sub-menu
	        new SciCursorSetPositionWorkarounds { gameId = SciGameId.QFG3,        newPositionY =  70, newPositionX = 170, rectTop = 40, rectLeft =  61, rectBottom =  81, rectRight = 258 },  // Quest For Glory 3, run/walk/sleep sub-menu
        };

        public bool IsVisible { get { return _isVisible; } }

        public Point Position
        {
            get
            {
                Point mousePos = SciEngine.Instance.System.InputManager.GetMousePosition();

                if (_upscaledHires != GfxScreenUpscaledMode.DISABLED)
                    _screen.AdjustBackUpscaledCoordinates(ref mousePos.Y, ref mousePos.X);

                return mousePos;
            }
        }

        public GfxCursor(ResourceManager resMan, GfxPalette palette, GfxScreen screen)
        {
            _resMan = resMan;
            _palette = palette;
            _screen = screen;
            _macCursorRemap = new List<ushort>();

            _upscaledHires = _screen.UpscaledHires;
            _isVisible = true;

            // center mouse cursor
            SetPosition(new Point(_screen.ScriptWidth / 2, _screen.ScriptHeight / 2));
            _moveZoneActive = false;

            _zoomZoneActive = false;
            _zoomZone = new Rect();
            _zoomCursorView = null;
            _zoomCursorLoop = 0;
            _zoomCursorCel = 0;
            _zoomPicView = null;
            _zoomColor = 0;
            _zoomMultiplier = 0;
            _cursorSurface = null;

            if (SciEngine.Instance != null && SciEngine.Instance.GameId == SciGameId.KQ6 && SciEngine.Instance.Platform == Core.IO.Platform.Windows)
            {
                // TODO: _useOriginalKQ6WinCursors = ConfMan.getBool("windows_cursors");
                _useOriginalKQ6WinCursors = true;
            }
            else {
                _useOriginalKQ6WinCursors = false;
            }

            if (SciEngine.Instance != null && SciEngine.Instance.GameId == SciGameId.SQ4 && ResourceManager.GetSciVersion() == SciVersion.V1_1)
            {
                // TODO: _useSilverSQ4CDCursors = ConfMan.getBool("silver_cursors");
                _useSilverSQ4CDCursors = true;
            }
            else {
                _useSilverSQ4CDCursors = false;
            }

            // _coordAdjuster and _event will be initialized later on
            _coordAdjuster = null;
            _event = null;
        }

        public void KernelSetPos(Point pos)
        {
            _coordAdjuster.SetCursorPos(ref pos);
            KernelMoveCursor(pos);
        }

        private void KernelMoveCursor(Point pos)
        {
            _coordAdjuster.MoveCursor(ref pos);
            if (pos.X > _screen.ScriptWidth || pos.Y > _screen.ScriptHeight)
            {
                // TODO: warning("attempt to place cursor at invalid coordinates (%d, %d)", pos.y, pos.x);
                return;
            }

            SetPosition(pos);

            // Trigger event reading to make sure the mouse coordinates will
            // actually have changed the next time we read them.
            _event.GetSciEvent(SciEvent.SCI_EVENT_PEEK);
        }

        public void KernelSetShape(int resourceId)
        {
            ResourceManager.ResourceSource.Resource resource;
            ByteAccess resourceData;
            Point hotspot = new Point(0, 0);
            byte[] colorMapping = new byte[4];
            short x, y;
            byte color;
            short maskA, maskB;
            ByteAccess pOut;
            byte[] rawBitmap = new byte[SCI_CURSOR_SCI0_HEIGHTWIDTH * SCI_CURSOR_SCI0_HEIGHTWIDTH];
            short heightWidth;

            if (resourceId == -1)
            {
                // no resourceId given, so we actually hide the cursor
                KernelHide();
                return;
            }

            // Load cursor resource...
            resource = _resMan.FindResource(new ResourceId(ResourceType.Cursor, (ushort)resourceId), false);
            if (resource == null)
                throw new InvalidOperationException($"cursor resource {resourceId} not found");
            if (resource.size != SCI_CURSOR_SCI0_RESOURCESIZE)
                throw new InvalidOperationException($"cursor resource {resourceId} has invalid size");

            resourceData = new ByteAccess(resource.data);

            if (ResourceManager.GetSciVersion() <= SciVersion.V01)
            {
                // SCI0 cursors contain hotspot flags, not actual hotspot coordinates.
                // If bit 0 of resourceData[3] is set, the hotspot should be centered,
                // otherwise it's in the top left of the mouse cursor.
                hotspot.X = hotspot.Y = resourceData[3] != 0 ? SCI_CURSOR_SCI0_HEIGHTWIDTH / 2 : 0;
            }
            else {
                // Cursors in newer SCI versions contain actual hotspot coordinates.
                hotspot.X = resourceData.ReadUInt16();
                hotspot.Y = resourceData.ReadUInt16(2);
            }

            // Now find out what colors we are supposed to use
            colorMapping[0] = 0; // Black is hardcoded
            colorMapping[1] = _screen.ColorWhite; // White is also hardcoded
            colorMapping[2] = SCI_CURSOR_SCI0_TRANSPARENCYCOLOR;
            colorMapping[3] = (byte)(_palette.MatchColor(170, 170, 170) & GfxPalette.SCI_PALETTE_MATCH_COLORMASK); // Grey
                                                                                                                   // TODO: Figure out if the grey color is hardcoded
                                                                                                                   // HACK for the magnifier cursor in LB1, fixes its color (bug #3487092)
            if (SciEngine.Instance.GameId == SciGameId.LAURABOW && resourceId == 1)
                colorMapping[3] = _screen.ColorWhite;
            // HACK for Longbow cursors, fixes the shade of grey they're using (bug #3489101)
            if (SciEngine.Instance.GameId == SciGameId.LONGBOW)
                colorMapping[3] = (byte)(_palette.MatchColor(223, 223, 223) & GfxPalette.SCI_PALETTE_MATCH_COLORMASK); // Light Grey

            // Seek to actual data
            resourceData.Offset += 4;

            pOut = new ByteAccess(rawBitmap);
            for (y = 0; y < SCI_CURSOR_SCI0_HEIGHTWIDTH; y++)
            {
                maskA = resourceData.ReadInt16(y << 1);
                maskB = resourceData.ReadInt16(32 + (y << 1));

                for (x = 0; x < SCI_CURSOR_SCI0_HEIGHTWIDTH; x++)
                {
                    color = (byte)((((maskA << x) & 0x8000) | (((maskB << x) >> 1) & 0x4000)) >> 14);
                    pOut[0] = colorMapping[color];
                    pOut.Offset++;
                }
            }

            heightWidth = SCI_CURSOR_SCI0_HEIGHTWIDTH;

            if (_upscaledHires != GfxScreenUpscaledMode.DISABLED)
            {
                // Scale cursor by 2x - note: sierra didn't do this, but it looks much better
                heightWidth *= 2;
                hotspot.X *= 2;
                hotspot.Y *= 2;
                byte[] upscaledBitmap = new byte[heightWidth * heightWidth];
                throw new NotImplementedException();
                //_screen.Scale2x(rawBitmap, upscaledBitmap, SCI_CURSOR_SCI0_HEIGHTWIDTH, SCI_CURSOR_SCI0_HEIGHTWIDTH);
                //rawBitmap = upscaledBitmap;
            }

            if (hotspot.X >= heightWidth || hotspot.Y >= heightWidth)
            {
                throw new InvalidOperationException($"cursor {resourceId}'s hotspot ({hotspot.X}, {hotspot.Y}) is out of range of the cursor's dimensions ({heightWidth}x{heightWidth})");
            }

            SciEngine.Instance.System.GraphicsManager.SetCursor(rawBitmap, 0, heightWidth, heightWidth, hotspot, SCI_CURSOR_SCI0_TRANSPARENCYCOLOR);
            KernelShow();
        }

        public void RefreshPosition()
        {
            Point mousePoint = Position;

            if (_moveZoneActive)
            {
                bool clipped = false;

                if (mousePoint.X < _moveZone.Left)
                {
                    mousePoint.X = _moveZone.Left;
                    clipped = true;
                }
                else if (mousePoint.X >= _moveZone.Right)
                {
                    mousePoint.X = _moveZone.Right - 1;
                    clipped = true;
                }

                if (mousePoint.Y < _moveZone.Top)
                {
                    mousePoint.Y = _moveZone.Top;
                    clipped = true;
                }
                else if (mousePoint.Y >= _moveZone.Bottom)
                {
                    mousePoint.Y = _moveZone.Bottom - 1;
                    clipped = true;
                }

                // FIXME: Do this only when mouse is grabbed?
                if (clipped)
                    SetPosition(mousePoint);
            }

            if (_zoomZoneActive)
            {
                // Cursor
                CelInfo cursorCelInfo = _zoomCursorView.GetCelInfo(_zoomCursorLoop, _zoomCursorCel);
                var cursorBitmap = _zoomCursorView.GetBitmap(_zoomCursorLoop, _zoomCursorCel);
                // Pic
                CelInfo picCelInfo = _zoomPicView.GetCelInfo(0, 0);
                var rawPicBitmap = _zoomPicView.GetBitmap(0, 0);

                // Compute hotspot of cursor
                Point cursorHotspot = new Point((cursorCelInfo.width >> 1) - cursorCelInfo.displaceX, cursorCelInfo.height - cursorCelInfo.displaceY - 1);

                short targetX = (short)(((mousePoint.X - _moveZone.Left) * _zoomMultiplier));
                short targetY = (short)(((mousePoint.Y - _moveZone.Top) * _zoomMultiplier));
                if (targetX < 0)
                    targetX = 0;
                if (targetY < 0)
                    targetY = 0;

                targetX -= (short)cursorHotspot.X;
                targetY -= (short)cursorHotspot.Y;

                // Sierra SCI actually drew only within zoom area, thus removing the need to fill any other pixels with upmost/left
                //  color of the picture cel. This also made the cursor not appear on top of everything. They actually drew the
                //  cursor manually within kAnimate processing and used a hidden cursor for moving.
                //  TODO: we should also do this

                // Replace the special magnifier color with the associated magnified pixels
                for (int x = 0; x < cursorCelInfo.width; x++)
                {
                    for (int y = 0; y < cursorCelInfo.height; y++)
                    {
                        int curPos = cursorCelInfo.width * y + x;
                        if (cursorBitmap[curPos] == _zoomColor)
                        {
                            short rawY = (short)(targetY + y);
                            short rawX = (short)(targetX + x);
                            if ((rawY >= 0) && (rawY < picCelInfo.height) && (rawX >= 0) && (rawX < picCelInfo.width))
                            {
                                int rawPos = picCelInfo.width * rawY + rawX;
                                _cursorSurface[curPos] = rawPicBitmap[rawPos];
                            }
                            else {
                                _cursorSurface[curPos] = rawPicBitmap[0]; // use left and upmost pixel color
                            }
                        }
                    }
                }

                SciEngine.Instance.System.GraphicsManager.SetCursor(_cursorSurface, 0, cursorCelInfo.width, cursorCelInfo.height, cursorHotspot, cursorCelInfo.clearKey);
            }
        }

        public void KernelShow()
        {
            SciEngine.Instance.System.GraphicsManager.IsCursorVisible = true;
            _isVisible = true;
        }

        public void KernelHide()
        {
            SciEngine.Instance.System.GraphicsManager.IsCursorVisible = false;
            _isVisible = false;
        }

        public void Init(GfxCoordAdjuster coordAdjuster, EventManager eventMan)
        {
            _coordAdjuster = coordAdjuster;
            _event = eventMan;
        }

        private void SetPosition(Point pos)
        {
            // Don't set position, when cursor is not visible.
            // This fixes eco quest 1 (floppy) right at the start, which is setting
            // mouse cursor to (0,0) all the time during the intro. It's escapeable
            // (now) by moving to the left or top, but it's getting on your nerves. This
            // could theoretically break some things, although sierra normally sets
            // position only when showing the cursor.
            if (!_isVisible)
                return;

            if (_upscaledHires == GfxScreenUpscaledMode.DISABLED)
            {
                // TODO: _system.GraphicsManager.WarpMouse(pos.x, pos.y);
            }
            else {
                _screen.AdjustToUpscaledCoordinates(ref pos.Y, ref pos.X);
                // TODO: g_system.warpMouse(pos.x, pos.y);
            }

            // WORKAROUNDS for games with windows that are hidden when the mouse cursor
            // is moved outside them - also check setPositionWorkarounds above.
            //
            // Some games display a new menu, set mouse position somewhere within and
            // expect it to be in there. This is fine for a real mouse, but on platforms
            // without a mouse, such as a Wii with a Wii Remote, or touch interfaces,
            // this won't work. In these platforms, the affected menus will close
            // immediately, because the mouse cursor's position won't be what the game
            // scripts expect.
            // We identify these cases via the cursor position set. If the mouse position
            // is outside the expected rectangle, we report back to the game scripts that
            // it's actually inside it, the first time that the mouse position is polled,
            // as the scripts expect. In subsequent mouse position poll attempts, we
            // return back the actual mouse coordinates.
            // Currently this code is enabled for all platforms, as we can't differentiate
            // between ones that have normal mouse input, and platforms that have
            // alternative mouse input methods, like a touch screen. Platforms that have
            // a normal mouse for input won't be affected by this workaround.
            SciGameId gameId = SciEngine.Instance.GameId;
            foreach (var workaround in setPositionWorkarounds)
            {
                if (workaround.gameId == gameId
                    && ((workaround.newPositionX == pos.X) && (workaround.newPositionY == pos.Y)))
                {
                    var s = SciEngine.Instance.EngineState;
                    s._cursorWorkaroundActive = true;
                    s._cursorWorkaroundPoint = pos;
                    s._cursorWorkaroundRect = new Rect(workaround.rectLeft, workaround.rectTop, workaround.rectRight, workaround.rectBottom);
                    return;
                }
            }
        }

        public void SetMacCursorRemapList(int cursorCount, StackPtr cursors)
        {
            for (int i = 0; i < cursorCount; i++)
                _macCursorRemap.Add(cursors[i].ToUInt16());
        }
    }
}
