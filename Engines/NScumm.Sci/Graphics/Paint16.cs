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
        private const int SCI_DISPLAY_MOVEPEN = 100;
        private const int SCI_DISPLAY_SETALIGNMENT = 101;
        private const int SCI_DISPLAY_SETPENCOLOR = 102;
        private const int SCI_DISPLAY_SETBACKGROUNDCOLOR = 103;
        private const int SCI_DISPLAY_SETGREYEDOUTPUT = 104;
        private const int SCI_DISPLAY_SETFONT = 105;
        private const int SCI_DISPLAY_WIDTH = 106;
        private const int SCI_DISPLAY_SAVEUNDER = 107;
        private const int SCI_DISPLAY_RESTOREUNDER = 108;
        private const int SCI_DISPLAY_DUMMY1 = 114;
        private const int SCI_DISPLAY_DUMMY2 = 115;
        private const int SCI_DISPLAY_DUMMY3 = 117;
        private const int SCI_DISPLAY_DONTSHOWBITS = 121;

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
                _ports.OffsetRect(ref workerRect);
            }

            // now actually ask _screen how much space it will need for saving
            size = _screen.BitsGetDataSize(workerRect, screenMask);

            memoryId = _segMan.AllocateHunkEntry("SaveBits()", size);
            memoryPtr = _segMan.GetHunkPointer(memoryId);
            if (memoryPtr != null)
                _screen.BitsSave(workerRect, screenMask, memoryPtr);
            return memoryId;
        }

        public void InvertRect(Rect rect)
        {
            short oldpenmode = _ports._curPort.penMode;
            _ports._curPort.penMode = 2;
            FillRect(rect, GfxScreenMasks.VISUAL, (byte)_ports._curPort.penClr, (byte)_ports._curPort.backClr);
            _ports._curPort.penMode = oldpenmode;
        }

        // used in SCI0early exclusively
        public void InvertRectViaXOR(Rect rect)
        {
            Rect r = rect;
            short x, y;
            byte curVisual;

            r.Clip(_ports._curPort.rect);
            if (r.IsEmpty) // nothing to invert
                return;

            _ports.OffsetRect(ref r);
            for (y = (short)r.Top; y < r.Bottom; y++)
            {
                for (x = (short)r.Left; x < r.Right; x++)
                {
                    curVisual = _screen.GetVisual(x, y);
                    _screen.PutPixel(x, y, GfxScreenMasks.VISUAL, (byte)(curVisual ^ 0x0f), 0, 0);
                }
            }
        }

        public Register KernelGraphSaveBox(Rect rect, ushort screenMask)
        {
            return BitsSave(rect, (GfxScreenMasks)screenMask);
        }

        public void KernelGraphRestoreBox(Register handle)
        {
            BitsRestore(handle);
        }

        public void KernelGraphDrawLine(Point startPoint, Point endPoint, short color, short priority, short control)
        {
            _ports.ClipLine(ref startPoint, ref endPoint);
            _ports.OffsetLine(ref startPoint, ref endPoint);
            _screen.DrawLine((short)startPoint.X, (short)startPoint.Y, (short)endPoint.X, (short)endPoint.Y, (byte)color, (byte)priority, (byte)control);
        }

        public void KernelGraphFillBoxBackground(Rect rect)
        {
            EraseRect(rect);
        }

        public void KernelGraphFillBoxForeground(Rect rect)
        {
            PaintRect(rect);
        }

        public void FillRect(Rect rect, GfxScreenMasks drawFlags, byte color, byte priority = 0, byte control = 0)
        {
            Rect r = rect;
            r.Clip(_ports._curPort.rect);
            if (r.IsEmpty) // nothing to fill
                return;

            short oldPenMode = _ports._curPort.penMode;
            _ports.OffsetRect(ref r);
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

        public void KernelDrawCel(int viewId, short loopNo, short celNo, ushort leftPos, ushort topPos, short priority, ushort paletteNo, ushort scaleX, ushort scaleY, bool hiresMode, Register upscaledHiresHandle)
        {
            // some calls are hiresMode even under kq6 DOS, that's why we check for
            // upscaled hires here
            if ((!hiresMode) || (_screen.UpscaledHires == GfxScreenUpscaledMode.DISABLED))
            {
                DrawCelAndShow(viewId, loopNo, celNo, leftPos, topPos, priority, paletteNo, scaleX, scaleY);
            }
            else {
                DrawHiresCelAndShow(viewId, loopNo, celNo, leftPos, topPos, priority, paletteNo, upscaledHiresHandle);
            }
        }

        private void DrawHiresCelAndShow(int viewId, short loopNo, short celNo, ushort leftPos, ushort topPos, short priority, ushort paletteNo, Register upscaledHiresHandle)
        {
            GfxView view = _cache.GetView(viewId);
            Rect celRect, curPortRect, clipRect, clipRectTranslated;
            Point curPortPos;
            bool upscaledHiresHack = false;

            if (view != null)
            {
                if ((leftPos == 0) && (topPos == 0))
                {
                    // HACK: in kq6, we get leftPos&topPos == 0 SOMETIMES, that's why we
                    // need to get coordinates from upscaledHiresHandle. I'm not sure if
                    // this is what we are supposed to do or if there is some other bug
                    // that actually makes coordinates to be 0 in the first place.
                    var memoryPtr = _segMan.GetHunkPointer(upscaledHiresHandle);
                    if (memoryPtr != null)
                    {
                        Rect upscaledHiresRect = _screen.BitsGetRect(memoryPtr);
                        leftPos = (ushort)upscaledHiresRect.Left;
                        topPos = (ushort)upscaledHiresRect.Top;
                        upscaledHiresHack = true;
                    }
                }

                celRect.Left = leftPos;
                celRect.Top = topPos;
                celRect.Right = celRect.Left + view.GetWidth(loopNo, celNo);
                celRect.Bottom = celRect.Top + view.GetHeight(loopNo, celNo);
                // adjust curPort to upscaled hires
                clipRect = celRect;
                curPortRect = _ports._curPort.rect;
				view.AdjustToUpscaledCoordinates(ref curPortRect.Top, ref curPortRect.Left);
				view.AdjustToUpscaledCoordinates(ref curPortRect.Bottom, ref curPortRect.Right);
                curPortRect.Bottom++;
                curPortRect.Right++;
                clipRect.Clip(curPortRect);
                if (clipRect.IsEmpty) // nothing to draw
                    return;

                clipRectTranslated = clipRect;
                if (!upscaledHiresHack)
                {
                    curPortPos.X = _ports._curPort.left; curPortPos.Y = _ports._curPort.top;
					view.AdjustToUpscaledCoordinates(ref curPortPos.Y, ref curPortPos.X);
                    clipRectTranslated.Top += curPortPos.Y; clipRectTranslated.Bottom += curPortPos.Y;
                    clipRectTranslated.Left += curPortPos.X; clipRectTranslated.Right += curPortPos.X;
                }

                view.Draw(celRect, clipRect, clipRectTranslated, loopNo, celNo, (byte)priority, paletteNo, true);
                if (_screen._picNotValidSci11 == 0)
                {
                    _screen.CopyDisplayRectToScreen(clipRectTranslated);
                }
            }
        }

        // This one is the only one that updates screen!
        public void DrawCelAndShow(int viewId, short loopNo, short celNo, ushort leftPos, ushort topPos, short priority, ushort paletteNo, ushort scaleX = 128, ushort scaleY = 128)
        {
            GfxView view = _cache.GetView(viewId);
            Rect celRect;

            if (view != null)
            {
                celRect.Left = leftPos;
                celRect.Top = topPos;
                celRect.Right = celRect.Left + view.GetWidth(loopNo, celNo);
                celRect.Bottom = celRect.Top + view.GetHeight(loopNo, celNo);

                DrawCel(view, loopNo, celNo, celRect, (byte)priority, paletteNo, scaleX, scaleY);

                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                {
                    if (_screen._picNotValidSci11 == 0)
                    {
                        BitsShow(celRect);
                    }
                }
                else {
                    if (_screen._picNotValid == 0)
                        BitsShow(celRect);
                }
            }
        }

        public Register KernelDisplay(string text, ushort languageSplitter, int argc, StackPtr? argv)
        {
            Register displayArg;
            short alignment = GfxText16.SCI_TEXT16_ALIGNMENT_LEFT;
            short colorPen = -1, colorBack = -1, width = -1, bRedraw = 1;
            bool doSaveUnder = false;
            Rect rect = new Rect();
            Register result = Register.NULL_REG;

            // Make a "backup" of the port settings (required for some SCI0LATE and
            // SCI01+ only)
            Port oldPort = _ports.Port;

            // setting defaults
            _ports.PenMode(0);
            _ports.PenColor(0);
            _ports.TextGreyedOutput(false);
            // processing codes in argv
            while (argc > 0)
            {
                displayArg = argv.Value[0];
                if (displayArg.Segment != 0)
                    displayArg= Register.SetOffset(displayArg, 0xFFFF);
                argc--; argv++;
                switch (displayArg.Offset)
                {
                    case SCI_DISPLAY_MOVEPEN:
                        _ports.MoveTo(argv.Value[0].ToInt16(), argv.Value[1].ToInt16());
                        argc -= 2; argv += 2;
                        break;
                    case SCI_DISPLAY_SETALIGNMENT:
                        alignment = argv.Value[0].ToInt16();
                        argc--; argv++;
                        break;
                    case SCI_DISPLAY_SETPENCOLOR:
                        colorPen = argv.Value[0].ToInt16();
                        _ports.PenColor(colorPen);
                        argc--; argv++;
                        break;
                    case SCI_DISPLAY_SETBACKGROUNDCOLOR:
                        colorBack = argv.Value[0].ToInt16();
                        argc--; argv++;
                        break;
                    case SCI_DISPLAY_SETGREYEDOUTPUT:
                        _ports.TextGreyedOutput(!argv.Value[0].IsNull);
                        argc--; argv++;
                        break;
                    case SCI_DISPLAY_SETFONT:
                        _text16.SetFont(argv.Value[0].ToUInt16());
                        argc--; argv++;
                        break;
                    case SCI_DISPLAY_WIDTH:
                        width = argv.Value[0].ToInt16();
                        argc--; argv++;
                        break;
                    case SCI_DISPLAY_SAVEUNDER:
                        doSaveUnder = true;
                        break;
                    case SCI_DISPLAY_RESTOREUNDER:
                        rect = BitsGetRect(argv.Value[0]);
                        rect.Translate(-_ports.Port.left, -_ports.Port.top);
                        BitsRestore(argv.Value[0]);
                        KernelGraphRedrawBox(rect);
                        // finishing loop
                        argc = 0;
                        break;
                    case SCI_DISPLAY_DONTSHOWBITS:
                        bRedraw = 0;
                        break;

                    // The following three dummy calls are not supported by the Sierra SCI
                    // interpreter, but are erroneously called in some game scripts.
                    case SCI_DISPLAY_DUMMY1:    // Longbow demo (all rooms) and QFG1 EGA demo (room 11)
                    case SCI_DISPLAY_DUMMY2:    // Longbow demo (all rooms)
                    case SCI_DISPLAY_DUMMY3:    // QFG1 EGA demo (room 11) and PQ2 (room 23)
                        if (!(SciEngine.Instance.GameId == SciGameId.LONGBOW && SciEngine.Instance.IsDemo) &&
                            !(SciEngine.Instance.GameId == SciGameId.QFG1 && SciEngine.Instance.IsDemo) &&
                            !(SciEngine.Instance.GameId == SciGameId.PQ2))
                            throw new InvalidOperationException($"Unknown kDisplay argument {displayArg.Offset}");

                        if (displayArg.Offset == SCI_DISPLAY_DUMMY2)
                        {
                            if (argc == 0)
                                throw new InvalidOperationException("No parameter left for kDisplay(115)");
                            argc--; argv++;
                        }
                        break;
                    default:
                        SciTrackOriginReply originReply;
                        SciWorkaroundSolution solution = Workarounds.TrackOriginAndFindWorkaround(0, Workarounds.kDisplay_workarounds, out originReply);
                        if (solution.type == SciWorkaroundType.NONE)
                            throw new InvalidOperationException($"Unknown kDisplay argument ({displayArg}) from method {originReply.objectName}::{originReply.methodName} (script {originReply.scriptNr}, localCall {originReply.localCallOffset:X})");
                        //assert(solution.type == WORKAROUND_IGNORE);
                        break;
                }
            }

            // now drawing the text
            _text16.Size(out rect, text, languageSplitter, -1, width);
            rect.MoveTo(_ports.Port.curLeft, _ports.Port.curTop);
            // Note: This code has been found in SCI1 middle and newer games. It was
            // previously only for SCI1 late and newer, but the LSL1 interpreter contains
            // this code.
            if (ResourceManager.GetSciVersion() >= SciVersion.V1_MIDDLE)
            {
                short leftPos = (short)(rect.Right <= _screen.Width ? 0 : _screen.Width - rect.Right);
                short topPos = (short)(rect.Bottom <= _screen.Height ? 0 : _screen.Height - rect.Bottom);
                _ports.Move(leftPos, topPos);
                rect.MoveTo(_ports.Port.curLeft, _ports.Port.curTop);
            }

            if (doSaveUnder)
                result = BitsSave(rect, GfxScreenMasks.VISUAL);
            if (colorBack != -1)
                FillRect(rect, GfxScreenMasks.VISUAL, (byte)colorBack, 0, 0);
            _text16.Box(text, languageSplitter, false, rect, alignment, -1);
            if (_screen._picNotValid == 0 && bRedraw != 0)
                BitsShow(rect);
            // restoring port and cursor pos
            Port currport = _ports.Port;
            ushort tTop = (ushort)currport.curTop;
            ushort tLeft = (ushort)currport.curLeft;
            if (!SciEngine.Instance.Features.UsesOldGfxFunctions())
            {
                // Restore port settings for some SCI0LATE and SCI01+ only.
                //
                // The change actually happened inbetween .530 (hoyle1) and .566 (heros
                // quest). We don't have any detection for that currently, so we are
                // using oldGfxFunctions (.502). The only games that could get
                // regressions because of this are hoyle1, kq4 and funseeker. If there
                // are regressions, we should use interpreter version (which would
                // require exe version detection).
                //
                // If we restore the port for whole SCI0LATE, at least sq3old will get
                // an issue - font 0 will get used when scanning for planets instead of
                // font 600 - a setfont parameter is missing in one of the kDisplay
                // calls in script 19. I assume this is a script bug, because it was
                // added in sq3new.
                currport = oldPort;
            }
            currport.curTop = (short)tTop;
            currport.curLeft = (short)tLeft;
            return result;
        }

        public void EraseRect(Rect rect)
        {
            FillRect(rect, GfxScreenMasks.VISUAL, (byte)_ports._curPort.backClr);
        }

        private Rect BitsGetRect(Register memoryHandle)
        {
            Rect destRect = new Rect();
            byte[] memoryPtr = null;

            if (!memoryHandle.IsNull)
            {
                memoryPtr = _segMan.GetHunkPointer(memoryHandle);

                if (memoryPtr != null)
                {
                    destRect = _screen.BitsGetRect(memoryPtr);
                }
            }
            return destRect;
        }

        public void FrameRect(Rect rect)
        {
            Rect r = rect;
            // left
            r.Right = rect.Left + 1;
            PaintRect(r);
            // right
            r.Right = rect.Right;
            r.Left = rect.Right - 1;
            PaintRect(r);
            //top
            r.Left = rect.Left;
            r.Bottom = rect.Top + 1;
            PaintRect(r);
            //bottom
            r.Bottom = rect.Bottom;
            r.Top = rect.Bottom - 1;
            PaintRect(r);
        }

        public void PaintRect(Rect rect)
        {
            FillRect(rect, GfxScreenMasks.VISUAL, (byte)_ports._curPort.penClr);
        }

        public void BitsShow(Rect rect)
        {
            Rect workerRect = new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            workerRect.Clip(_ports._curPort.rect);
            if (workerRect.IsEmpty) // nothing to show
                return;

            _ports.OffsetRect(ref workerRect);

            // We adjust the left/right coordinates to even coordinates
            workerRect.Left &= 0xFFFE; // round down
            workerRect.Right = (workerRect.Right + 1) & 0xFFFE; // round up

            _screen.CopyRectToScreen(workerRect);
        }

        public void KernelGraphFillBox(Rect rect, short colorMask, short color, short priority, short control)
        {
            FillRect(rect, (GfxScreenMasks)colorMask, (byte)color, (byte)priority, (byte)control);
        }

        public Register KernelGraphSaveUpscaledHiresBox(Rect rect)
        {
            return BitsSave(rect, GfxScreenMasks.DISPLAY);
        }

        public void KernelGraphRedrawBox(Rect rect)
        {
            var left = (short)rect.Left;
            var top = (short)rect.Top;
            var right = (short)rect.Right;
            var bottom = (short)rect.Bottom;
            _coordAdjuster.KernelLocalToGlobal(ref left, ref top);
            _coordAdjuster.KernelLocalToGlobal(ref right, ref bottom);
            Port oldPort = _ports.SetPort(_ports._picWind);
            _coordAdjuster.KernelGlobalToLocal(ref left, ref top);
            _coordAdjuster.KernelGlobalToLocal(ref right, ref bottom);

            rect = new Rect(left, top, right, bottom);
            _animate.ReAnimate(rect);

            _ports.SetPort(oldPort);
        }

        public void KernelGraphUpdateBox(Rect rect, bool hiresMode)
        {
            // some calls are hiresMode even under kq6 DOS, that's why we check for
            // upscaled hires here
            if ((!hiresMode) || (_screen.UpscaledHires == GfxScreenUpscaledMode.DISABLED))
                BitsShow(rect);
            else
                BitsShowHires(rect);
        }

        private void BitsShowHires(Rect rect)
        {
            _screen.CopyDisplayRectToScreen(rect);
        }

        public void BitsRestore(Register memoryHandle)
        {
            byte[] memoryPtr = null;

            if (!memoryHandle.IsNull)
            {
                memoryPtr = _segMan.GetHunkPointer(memoryHandle);

                if (memoryPtr != null)
                {
                    _screen.BitsRestore(memoryPtr);
                    BitsFree(memoryHandle);
                }
            }
        }

        public void KernelDrawPicture(int pictureId, short animationNr, bool animationBlackoutFlag, bool mirroredFlag, bool addToFlag, short EGApaletteNo)
        {
            Port oldPort = _ports.SetPort(_ports._picWind);

            if (_ports.IsFrontWindow(_ports._picWind))
            {
                _screen._picNotValid = 1;
                DrawPicture(pictureId, animationNr, mirroredFlag, addToFlag, EGApaletteNo);
                _transitions.Setup(animationNr, animationBlackoutFlag);
            }
            else {
                // We need to set it for SCI1EARLY+ (sierra sci also did so), otherwise we get at least the following issues:
                //  LSL5 (english) - last wakeup (taj mahal flute dream)
                //  SQ5 (english v1.03) - during the scene following the scrubbing
                //   in both situations a window is shown when kDrawPic is called, which would result otherwise in
                //   no showpic getting called from kAnimate and we would get graphic corruption
                // XMAS1990 EGA did not set it in this case, VGA did
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_EARLY)
                    _screen._picNotValid = 1;
                _ports.BeginUpdate(_ports._picWind);
                DrawPicture(pictureId, animationNr, mirroredFlag, addToFlag, EGApaletteNo);
                _ports.EndUpdate(_ports._picWind);
            }
            _ports.SetPort(oldPort);
        }

        public void BitsFree(Register memoryHandle)
        {
            if (!memoryHandle.IsNull) // happens in KQ5CD
                _segMan.FreeHunkEntry(memoryHandle);
        }

        private void DrawPicture(int pictureId, short animationNr, bool mirroredFlag, bool addToFlag, int paletteId)
        {
            GfxPicture picture = new GfxPicture(_resMan, _coordAdjuster, _ports, _screen, _palette, pictureId, _EGAdrawingVisualize);

            // do we add to a picture? if not . clear screen with white
            if (!addToFlag)
                ClearScreen(_screen.ColorWhite);

            picture.Draw(animationNr, mirroredFlag, addToFlag, (short)paletteId);

            // We make a call to SciPalette here, for increasing sys timestamp and also loading targetpalette, if palvary active
            //  (SCI1.1 only)
            if (ResourceManager.GetSciVersion() == SciVersion.V1_1)
                _palette.DrewPicture(pictureId);
        }

        // This version of drawCel is not supposed to call BitsShow()!
        public void DrawCel(int viewId, short loopNo, short celNo, Rect celRect, byte priority, ushort paletteNo, ushort scaleX, ushort scaleY)
        {
            DrawCel(_cache.GetView(viewId), loopNo, celNo, celRect, priority, paletteNo, scaleX, scaleY);
        }

        // This version of drawCel is not supposed to call BitsShow()!
        public void DrawCel(GfxView view, short loopNo, short celNo, Rect celRect, byte priority, ushort paletteNo, ushort scaleX = 128, ushort scaleY = 128)
        {
            Rect clipRect = celRect;
            clipRect.Clip(_ports._curPort.rect);
            if (clipRect.IsEmpty) // nothing to draw
                return;

            Rect clipRectTranslated = clipRect;
            _ports.OffsetRect(ref clipRectTranslated);
            if (scaleX == 128 && scaleY == 128)
                view.Draw(celRect, clipRect, clipRectTranslated, loopNo, celNo, priority, paletteNo, false);
            else
                view.DrawScaled(celRect, clipRect, clipRectTranslated, loopNo, celNo, priority, scaleX, scaleY);
        }

        private void ClearScreen(byte color)
        {
            FillRect(_ports._curPort.rect, GfxScreenMasks.ALL, color, 0, 0);
        }
    }
}
