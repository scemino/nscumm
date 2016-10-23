//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 3 of the License; or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful;
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not; see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    internal class GfxCursor32
    {
        private class DrawRegion
        {
            public Rect rect;
            public BytePtr data;
            public byte skipColor;

            public DrawRegion()
            {
            }
        }

        /**
	 * Information about the current cursor.
	 * Used to restore cursor when loading a
	 * savegame.
	 */
        CelInfo32 _cursorInfo = new CelInfo32();

        /// <summary>
        /// Content behind the cursor? TODO
        /// </summary>
        DrawRegion _cursorBack = new DrawRegion();

        /// <summary>
        /// Scratch buffer.
        /// </summary>
        DrawRegion _drawBuff1 = new DrawRegion();

        /// <summary>
        /// Scratch buffer 2.
        /// </summary>
        DrawRegion _drawBuff2 = new DrawRegion();

        /**
         * A draw region representing the current
         * output buffer.
         */
        DrawRegion _vmapRegion = new DrawRegion();

        /// <summary>
        /// The content behind the cursor in the
        /// output buffer.
        /// </summary>
        DrawRegion _savedVmapRegion = new DrawRegion();

        /// <summary>
        /// The cursor bitmap.
        /// </summary>
        DrawRegion _cursor = new DrawRegion();

        /**
         * The width and height of the cursor,
         * in screen coordinates.
         */
        short _width, _height;

        /**
         * The output buffer where the cursor is
         * rendered.
         */
        Buffer _vmap;

        /**
         * The number of times the cursor has been
         * hidden.
         */
        int _hideCount;

        /**
         * The rendered position of the cursor, in
         * screen coordinates.
         */
        Point _position;

        /**
         * The position of the cursor hot spot, relative
         * to the cursor origin, in screen pixels.
         */
        Point _hotSpot;

        /**
         * The area within which the cursor is allowed
         * to move, in screen pixels.
         */
        Rect _restrictedArea;

        /**
         * Indicates whether or not the cursor needs to
         * be repainted on the output buffer due to a
         * change of graphics in the area underneath the
         * cursor.
         */
        bool _writeToVMAP;

        // Mac versions of games use a remap list to remap their cursors
        private List<ushort> _macCursorRemap = new List<ushort>();

        /**
	 * Initialises the cursor system with the given
	 * buffer to use as the output buffer for
	 * rendering the cursor.
	 */

        public void Init(Buffer vmap)
        {
            _vmap = vmap;
            _vmapRegion.rect = new Rect((short) _vmap.ScreenWidth, (short) _vmap.ScreenHeight);
            _vmapRegion.data = _vmap.Pixels;
            _restrictedArea = _vmapRegion.rect;
        }

        /// <summary>
        /// Called when the hardware mouse moves.
        /// </summary>
        /// <param name="position"></param>
        public void DeviceMoved(Point position)
        {
            if (position.X < _restrictedArea.Left)
            {
                position.X = _restrictedArea.Left;
            }
            if (position.X >= _restrictedArea.Right)
            {
                position.X = (short) (_restrictedArea.Right - 1);
            }
            if (position.Y < _restrictedArea.Top)
            {
                position.Y = _restrictedArea.Top;
            }
            if (position.Y >= _restrictedArea.Bottom)
            {
                position.Y = (short) (_restrictedArea.Bottom - 1);
            }

            _position = position;

            // TODO: vs warp mouse
            //SciEngine.Instance.System.InputManager.WarpMouse(position.X, position.Y);
            Move();
        }

        /**
         * Called by GfxFrameout once for each show
         * rectangle that is going to be drawn to
         * hardware.
         */

        public void GonnaPaint(Rect paintRect)
        {
            if (_hideCount == 0 && !_writeToVMAP && !_cursorBack.rect.IsEmpty)
            {
                paintRect.Left &= ~3;
                paintRect.Right |= 3;
                if (_cursorBack.rect.Intersects(paintRect))
                {
                    _writeToVMAP = true;
                }
            }
        }

        /// <summary>
        /// Called by GfxFrameout when the rendering to
        /// hardware begins.
        /// </summary>
        public void PaintStarting()
        {
            if (_writeToVMAP)
            {
                _savedVmapRegion.rect = _cursor.rect;
                Copy(_savedVmapRegion, _vmapRegion);
                Paint(_vmapRegion, _cursor);
            }
        }

        /// <summary>
        /// Called by GfxFrameout when the output buffer
        /// has finished rendering to hardware.
        /// </summary>
        public void DonePainting()
        {
            if (_writeToVMAP)
            {
                Copy(_vmapRegion, _savedVmapRegion);
                _savedVmapRegion.rect = new Rect();
                _writeToVMAP = false;
            }

            if (_hideCount == 0 && !_cursorBack.rect.IsEmpty)
            {
                Copy(_cursorBack, _vmapRegion);
            }
        }

        /**
         * Hides the cursor. Each call to `hide` will
         * increment a hide counter, which must be
         * returned to 0 before the cursor will be
         * shown again.
         */

        public void Hide()
        {
            if (_hideCount++ != 0)
            {
                return;
            }

            if (!_cursorBack.rect.IsEmpty)
            {
                DrawToHardware(_cursorBack);
            }
        }

        /// <summary>
        /// Shows the cursor, if the hide counter is
        /// returned to 0.
        /// </summary>
        public void Unhide()
        {
            if (_hideCount == 0 || --_hideCount != 0)
            {
                return;
            }

            _cursor.rect.MoveTo((short) (_position.X - _hotSpot.X), (short) (_position.Y - _hotSpot.Y));
            RevealCursor();
        }

        /// <summary>
        /// Shows the cursor regardless of the state of
        /// the hide counter.
        /// </summary>
        public void Show()
        {
            if (_hideCount != 0)
            {
                _hideCount = 0;
                _cursor.rect.MoveTo((short) (_position.X - _hotSpot.X), (short) (_position.Y - _hotSpot.Y));
                RevealCursor();
            }
        }

        /// <summary>
        /// Removes restrictions on mouse movement.
        /// </summary>
        public void ClearRestrictedArea()
        {
            _restrictedArea = _vmapRegion.rect;
        }

        /// <summary>
        /// Explicitly sets the position of the cursor,
        /// in game script coordinates.
        /// </summary>
        /// <param name="position"></param>
        public void SetPosition(Point position)
        {
            short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;
            short screenWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
            short screenHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;

            _position.X = (short) (position.X * new Rational(screenWidth, scriptWidth));
            _position.Y = (short) (position.Y * new Rational(screenHeight, scriptHeight));

            // TODO: vs
            //SciEngine.Instance.System.InputManager.WarpMouse(_position.X, _position.Y);
        }

        public void SetRestrictedArea(Rect rect)
        {
            _restrictedArea = rect;

            short screenWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
            short screenHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;
            short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            var r1 = new Rational(screenWidth, scriptWidth);
            var r2 = new Rational(screenHeight, scriptHeight);
            Helpers.Mulru(ref _restrictedArea, ref r1, ref r2, 0);

            if (_position.X < rect.Left)
            {
                _position.X = rect.Left;
            }
            if (_position.X >= rect.Right)
            {
                _position.X = (short) (rect.Right - 1);
            }
            if (_position.Y < rect.Top)
            {
                _position.Y = rect.Top;
            }
            if (_position.Y >= rect.Bottom)
            {
                _position.Y = (short) (rect.Bottom - 1);
            }

            // TODO: vs
            // TODO: g_system.warpMouse(_position.X, _position.Y);
        }


        /// <summary>
        /// Sets the view used to render the cursor.
        /// </summary>
        public void SetView(int viewId, short loopNo, short celNo)
        {
            Hide();

            _cursorInfo.resourceId = viewId;
            _cursorInfo.loopNo = loopNo;
            _cursorInfo.celNo = celNo;

            if (_macCursorRemap.Count == 0 && viewId != -1)
            {
                CelObjView view = CelObjView.Create(viewId, loopNo, celNo);

                _hotSpot = view._origin;
                _width = (short) view._width;
                _height = (short) view._height;

                // SSCI never increased the size of cursors, but some of the cursors
                // in early SCI32 games were designed for low-resolution display mode
                // and so are kind of hard to pick out when running in high-resolution
                // mode.
                // To address this, we make some slight adjustments to cursor display
                // in these early games:
                // GK1: All the cursors are increased in size since they all appear to
                //      be designed for low-res display.
                // PQ4: We only make the cursors bigger if they are above a set
                //      threshold size because inventory items usually have a
                //      high-resolution cursor representation.
                bool pixelDouble = false;
                if (SciEngine.Instance._gfxFrameout._isHiRes &&
                    (SciEngine.Instance.GameId == SciGameId.GK1 ||
                     (SciEngine.Instance.GameId == SciGameId.PQ4 && _width <= 22 && _height <= 22)))
                {
                    _width *= 2;
                    _height *= 2;
                    _hotSpot.X *= 2;
                    _hotSpot.Y *= 2;
                    pixelDouble = true;
                }

                _cursor.data.Realloc(_width * _height);
                _cursor.rect = new Rect(_width, _height);
                _cursor.data.Data.Set(_cursor.data.Offset, 255, _width * _height);
                _cursor.skipColor = 255;

                Buffer target = new Buffer((ushort) _width, (ushort) _height, _cursor.data);
                if (pixelDouble)
                {
                    var p = new Point(0, 0);
                    view.Draw(target, ref _cursor.rect, ref p, false, new Rational(2), new Rational(2));
                }
                else
                {
                    var p = new Point(0, 0);
                    view.Draw(target, ref _cursor.rect, ref p, false);
                }
            }
            else if (_macCursorRemap.Count != 0 && viewId != -1)
            {
                // Mac cursor handling
                int viewNum = viewId;

                // Remap cursor view based on what the scripts have given us.
                for (var i = 0; i < _macCursorRemap.Count; i++)
                {
                    if (viewNum == _macCursorRemap[i])
                    {
                        viewNum = (i + 1) * 0x100 + loopNo * 0x10 + celNo;
                        break;
                    }

                    if (i == _macCursorRemap.Count)
                        Error("Unmatched Mac cursor {0}", viewNum);
                }

                _cursorInfo.resourceId = viewNum;

                var resource =
                    SciEngine.Instance.ResMan.FindResource(new ResourceId(ResourceType.Cursor, (ushort) viewNum),
                        false);

                if (resource == null)
                {
                    // The cursor resources often don't exist, this is normal behavior
                    Debug(0, "Mac cursor %d not found", viewNum);
                    return;
                }
                Stream resStream = new MemoryStream(resource.data, 0, resource.size);
                // TODO : vs MacCursor
//                MacCursor macCursor = new MacCursor();
//
//                if (!macCursor.readFromStream(resStream))
//                {
//                    Warning("Failed to load Mac cursor {0}", viewNum);
//                    //delete macCursor;
//                    return;
//                }
//
//                _hotSpot = new Point(macCursor.getHotspotX(), macCursor.getHotspotY());
//                _width = macCursor.getWidth();
//                _height = macCursor.getHeight();
//
//                _cursor.data.Realloc(_width * _height);
//                memcpy(_cursor.data, macCursor.Surface, _width * _height);
//                _cursor.rect = new Rect(_width, _height);
//                _cursor.skipColor = macCursor.KeyColor;

                // The cursor will be drawn on next refresh
                //delete macCursor;
            }
            else
            {
                _hotSpot = new Point(0, 0);
                _width = _height = 1;
                _cursor.data.Realloc(_width * _height);
                _cursor.rect = new Rect(_width, _height);
                _cursor.data.Value = _cursor.skipColor;
                _cursorBack.rect = _cursor.rect;
                _cursorBack.rect.Clip(_vmapRegion.rect);
                if (!_cursorBack.rect.IsEmpty)
                {
                    ReadVideo(_cursorBack);
                }
            }

            _cursorBack.data.Realloc(_width * _height);
            _drawBuff1.data.Realloc(_width * _height);
            _drawBuff2.data.Realloc(_width * _height * 4);
            _savedVmapRegion.data.Realloc(_width * _height);

            Unhide();
        }

        private void Move()
        {
            if (_hideCount != 0)
            {
                return;
            }

            // Cursor moved onto the screen after being offscreen
            _cursor.rect.MoveTo((short) (_position.X - _hotSpot.X), (short) (_position.Y - _hotSpot.Y));
            if (_cursorBack.rect.IsEmpty)
            {
                RevealCursor();
                return;
            }

            // Cursor moved offscreen
            if (!_cursor.rect.Intersects(_vmapRegion.rect))
            {
                DrawToHardware(_cursorBack);
                return;
            }

            if (!_cursor.rect.Intersects(_cursorBack.rect))
            {
                // Cursor moved to a completely different part of the screen
                _drawBuff1.rect = _cursor.rect;
                _drawBuff1.rect.Clip(_vmapRegion.rect);
                ReadVideo(_drawBuff1);

                _drawBuff2.rect = _drawBuff1.rect;
                Copy(_drawBuff2, _drawBuff1);

                Paint(_drawBuff1, _cursor);
                DrawToHardware(_drawBuff1);

                DrawToHardware(_cursorBack);

                _cursorBack.rect = _cursor.rect;
                _cursorBack.rect.Clip(_vmapRegion.rect);
                Copy(_cursorBack, _drawBuff2);
            }
            else
            {
                // Cursor moved, but still overlaps the previous cursor location
                Rect mergedRect = new Rect(_cursorBack.rect);
                mergedRect.Extend(_cursor.rect);
                mergedRect.Clip(_vmapRegion.rect);

                _drawBuff2.rect = mergedRect;
                ReadVideo(_drawBuff2);

                Copy(_drawBuff2, _cursorBack);

                _cursorBack.rect = _cursor.rect;
                _cursorBack.rect.Clip(_vmapRegion.rect);
                Copy(_cursorBack, _drawBuff2);

                Paint(_drawBuff2, _cursor);
                DrawToHardware(_drawBuff2);
            }
        }

        private void RevealCursor()
        {
            _cursorBack.rect = _cursor.rect;
            _cursorBack.rect.Clip(_vmapRegion.rect);
            if (_cursorBack.rect.IsEmpty)
            {
                return;
            }

            ReadVideo(_cursorBack);
            _drawBuff1.rect = _cursor.rect;
            Copy(_drawBuff1, _cursorBack);
            Paint(_drawBuff1, _cursor);
            DrawToHardware(_drawBuff1);
        }

        private void ReadVideo(DrawRegion target)
        {
            if (SciEngine.Instance._gfxFrameout._frameNowVisible)
            {
                Copy(target, _vmapRegion);
            }
            else
            {
                // NOTE: SSCI would read the background for the cursor directly out of
                // video memory here, but as far as can be determined, this does not
                // seem to actually be necessary for proper cursor rendering
            }
        }

        void Copy(DrawRegion target, DrawRegion source)
        {
            if (source.rect.IsEmpty)
            {
                return;
            }

            Rect drawRect = new Rect(source.rect);
            drawRect.Clip(target.rect);
            if (drawRect.IsEmpty)
            {
                return;
            }

            short sourceXOffset = (short) (drawRect.Left - source.rect.Left);
            short sourceYOffset = (short) (drawRect.Top - source.rect.Top);
            short drawWidth = drawRect.Width;
            short drawHeight = drawRect.Height;

            BytePtr targetPixel = target.data + ((drawRect.Top - target.rect.Top) * target.rect.Width)
                                  + (drawRect.Left - target.rect.Left);
            BytePtr sourcePixel = source.data + (sourceYOffset * source.rect.Width) + sourceXOffset;

            short sourceStride = source.rect.Width;
            short targetStride = target.rect.Width;

            for (int y = 0; y < drawHeight; ++y)
            {
                Array.Copy(sourcePixel.Data, sourcePixel.Offset, targetPixel.Data, targetPixel.Offset, drawWidth);
                targetPixel += targetStride;
                sourcePixel += sourceStride;
            }
        }

        private void DrawToHardware(DrawRegion source)
        {
            Rect drawRect = new Rect(source.rect);
            drawRect.Clip(_vmapRegion.rect);
            short sourceXOffset = (short) (drawRect.Left - source.rect.Left);
            short sourceYOffset = (short) (drawRect.Top - source.rect.Top);
            BytePtr sourcePixel = source.data + (sourceYOffset * source.rect.Width) + sourceXOffset;

            SciEngine.Instance.System.GraphicsManager.CopyRectToScreen(sourcePixel, source.rect.Width, drawRect.Left,
                drawRect.Top, drawRect.Width, drawRect.Height);
            SciEngine.Instance.System.GraphicsManager.UpdateScreen();
        }

        private void Paint(DrawRegion target, DrawRegion source)
        {
            if (source.rect.IsEmpty)
            {
                return;
            }

            Rect drawRect = new Rect(source.rect);
            drawRect.Clip(target.rect);
            if (drawRect.IsEmpty)
            {
                return;
            }

            short sourceXOffset = (short) (drawRect.Left - source.rect.Left);
            short sourceYOffset = (short) (drawRect.Top - source.rect.Top);
            short drawRectWidth = drawRect.Width;
            short drawRectHeight = drawRect.Height;

            BytePtr targetPixel = target.data + ((drawRect.Top - target.rect.Top) * target.rect.Width)
                                  + (drawRect.Left - target.rect.Left);
            BytePtr sourcePixel = source.data + (sourceYOffset * source.rect.Width) + sourceXOffset;
            byte skipColor = source.skipColor;

            short sourceStride = (short) (source.rect.Width - drawRectWidth);
            short targetStride = (short) (target.rect.Width - drawRectWidth);

            for (var y = 0; y < drawRectHeight; ++y)
            {
                for (var x = 0; x < drawRectWidth; ++x)
                {
                    if (sourcePixel.Value != skipColor)
                    {
                        targetPixel.Value = sourcePixel.Value;
                    }
                    ++targetPixel.Offset;
                    ++sourcePixel.Offset;
                }
                sourcePixel += sourceStride;
                targetPixel += targetStride;
            }
        }
    }
}