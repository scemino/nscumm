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
using NScumm.Sci.Engine;
using NScumm.Sci.Sound;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// Paint16 class, handles painting/drawing for SCI16 (SCI0-SCI1.1) games
    /// </summary>
    internal class GfxPaint16 : GfxPaint
    {
        private ResourceManager _resMan;
        private SegManager _segMan;
        private AudioPlayer _audio;
        private GfxAnimate _animate;
        private GfxCache _cache;
        private GfxPorts _ports;
        private GfxCoordAdjuster _coordAdjuster;
        private GfxScreen _screen;
        private GfxPalette _palette;
        private GfxText16 _text16;
        private GfxTransitions _transitions;

        // true means make EGA picture drawing visible
        private bool _EGAdrawingVisualize;

        public GfxPaint16(ResourceManager resMan, SegManager segMan, GfxCache cache, GfxPorts ports, GfxCoordAdjuster coordAdjuster, GfxScreen screen, GfxPalette palette, GfxTransitions transitions, AudioPlayer player)
        {
            _resMan = resMan;
            _segMan = segMan;
            _cache = cache;
            _ports = ports;
            _coordAdjuster = coordAdjuster;
            _screen = screen;
            _palette = palette;
            _transitions = transitions;
            _audio = player;
        }

        public void Init(GfxAnimate animate, GfxText16 text16)
        {
            _animate = animate;
            _text16 = text16;
        }

        public Register BitsSave(Rect rect, GfxScreenMasks screenMask)
        {
            Register memoryId;
            byte[] memoryPtr;
            int size;

            Rect workerRect = new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            workerRect.Clip(_ports._curPort.rect);
            if (workerRect.IsEmpty) // nothing to save
                return Register.NULL_REG;

            if (screenMask == GfxScreenMasks.DISPLAY)
            {
                // The coordinates we are given are actually up-to-including right/bottom - we extend accordingly
                workerRect.Bottom++;
                workerRect.Right++;
                // Adjust rect to upscaled hires, but dont adjust according to port
                _screen.AdjustToUpscaledCoordinates(ref workerRect.Top, ref workerRect.Left);
                _screen.AdjustToUpscaledCoordinates(ref workerRect.Bottom, ref workerRect.Right);
            }
            else {
                _ports.OffsetRect(workerRect);
            }

            // now actually ask _screen how much space it will need for saving
            size = _screen.BitsGetDataSize(workerRect, screenMask);

            memoryId = _segMan.AllocateHunkEntry("SaveBits()", size);
            memoryPtr = _segMan.GetHunkPointer(memoryId);
            if (memoryPtr != null)
                _screen.BitsSave(workerRect, screenMask, memoryPtr);
            return memoryId;
        }

        internal Register KernelGraphSaveBox(Rect rect, ushort screenMask)
        {
            throw new NotImplementedException();
        }

        internal void KernelGraphRestoreBox(Register register)
        {
            throw new NotImplementedException();
        }

        internal void KernelGraphDrawLine(Point point1, Point point2, short color, short priority, short control)
        {
            throw new NotImplementedException();
        }

        internal void KernelGraphFillBoxBackground(Rect rect)
        {
            throw new NotImplementedException();
        }

        internal void KernelGraphFillBoxForeground(Rect rect)
        {
            throw new NotImplementedException();
        }

        public void FillRect(Rect rect, GfxScreenMasks drawFlags, byte color, byte priority = 0, byte control = 0)
        {
            Rect r = rect;
            r.Clip(_ports._curPort.rect);
            if (r.IsEmpty) // nothing to fill
                return;

            short oldPenMode = _ports._curPort.penMode;
            _ports.OffsetRect(r);
            short x, y;
            byte curVisual;

            // Doing visual first
            if (drawFlags.HasFlag(GfxScreenMasks.VISUAL))
            {
                if (oldPenMode == 2)
                { // invert mode
                    for (y = (short)r.Top; y < r.Bottom; y++)
                    {
                        for (x = (short)r.Left; x < r.Right; x++)
                        {
                            curVisual = _screen.GetVisual(x, y);
                            if (curVisual == color)
                            {
                                _screen.PutPixel(x, y, GfxScreenMasks.VISUAL, priority, 0, 0);
                            }
                            else if (curVisual == priority)
                            {
                                _screen.PutPixel(x, y, GfxScreenMasks.VISUAL, color, 0, 0);
                            }
                        }
                    }
                }
                else { // just fill rect with color
                    for (y = (short)r.Top; y < r.Bottom; y++)
                    {
                        for (x = (short)r.Left; x < r.Right; x++)
                        {
                            _screen.PutPixel(x, y, GfxScreenMasks.VISUAL, color, 0, 0);
                        }
                    }
                }
            }

            if (drawFlags < GfxScreenMasks.PRIORITY)
                return;
            drawFlags &= GfxScreenMasks.PRIORITY | GfxScreenMasks.CONTROL;

            // we need to isolate the bits, sierra sci saved priority and control inside one byte, we don't
            priority &= 0x0f;
            control &= 0x0f;

            if (oldPenMode != 2)
            {
                for (y = (short)r.Top; y < r.Bottom; y++)
                {
                    for (x = (short)r.Left; x < r.Right; x++)
                    {
                        _screen.PutPixel(x, y, drawFlags, 0, priority, control);
                    }
                }
            }
            else {
                for (y = (short)r.Top; y < r.Bottom; y++)
                {
                    for (x = (short)r.Left; x < r.Right; x++)
                    {
                        // TODO: check this
                        _screen.PutPixel(x, y, drawFlags, 0, (byte)(_screen.GetPriority(x, y) == 0 ? 1 : 0), (byte)(_screen.GetControl(x, y) == 0 ? 1 : 0));
                    }
                }
            }
        }

        public void FrameRect(Rect r)
        {
            throw new NotImplementedException();
        }

        public void BitsShow(Rect rect)
        {
            Rect workerRect = new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            workerRect.Clip(_ports._curPort.rect);
            if (workerRect.IsEmpty) // nothing to show
                return;

            _ports.OffsetRect(workerRect);

            // We adjust the left/right coordinates to even coordinates
            workerRect.Left &= 0xFFFE; // round down
            workerRect.Right = (workerRect.Right + 1) & 0xFFFE; // round up

            _screen.CopyRectToScreen(workerRect);
        }

        internal void KernelGraphFillBox(Rect rect, short colorMask, short color, short priority, short control)
        {
            throw new NotImplementedException();
        }

        internal Register KernelGraphSaveUpscaledHiresBox(Rect rect)
        {
            throw new NotImplementedException();
        }

        internal void KernelGraphRedrawBox(Rect rect)
        {
            throw new NotImplementedException();
        }

        internal void KernelGraphUpdateBox(Rect rect, bool hiresMode)
        {
            throw new NotImplementedException();
        }

        internal void BitsRestore(Register register)
        {
            throw new NotImplementedException();
        }
    }
}
