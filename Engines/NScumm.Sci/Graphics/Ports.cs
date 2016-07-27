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
using NScumm.Sci.Engine;
using NScumm.Core.Graphics;
using System.Collections.Generic;
using NScumm.Core;
using System.Linq;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    // window styles
    [Flags]
    enum WindowManagerStyle
    {
        TRANSPARENT = (1 << 0),
        NOFRAME = (1 << 1),
        TITLE = (1 << 2),
        TOPMOST = (1 << 3),
        USER = (1 << 7)
    }

    /// <summary>
    /// Ports class, includes all port managment for SCI0.SCI1.1 games. Ports are some sort of windows in SCI
    ///  this class also handles adjusting coordinates to a specific port
    /// </summary>
    internal class GfxPorts
    {
        private const int PORTS_FIRSTWINDOWID = 2;
        private const int PORTS_FIRSTSCRIPTWINDOWID = 3;

        public Port _wmgrPort;
        public Window _picWind;

        public Port _menuPort;
        public Rect _menuBarRect;
        public Rect _menuRect;
        public Rect _menuLine;
        public Port _curPort;

        private GfxScreen _screen;
        private SegManager _segMan;
        /// <summary>
        /// The list of open 'windows' (and ports), in visual order.
        /// </summary>
        private List<Port> _windowList;

        /// <summary>
        /// The list of all open 'windows' (and ports), ordered by their id.
        /// </summary>
        private List<Port> _windowsById;

        private GfxPaint16 _paint16;
        private GfxText16 _text16;

        private bool _usesOldGfxFunctions;

        private WindowManagerStyle _styleUser;

        // counts windows that got disposed but are not freed yet
        private ushort _freeCounter;

        private Rect _bounds;

        // Priority Bands related variables
        private short _priorityTop, _priorityBottom, _priorityBandCount;
        private byte[] _priorityBands = new byte[200];

        public Port Port { get { return _curPort; } }

        public short PointSize { get { return _curPort.fontHeight; } }

        public GfxPorts(SegManager segMan, GfxScreen screen)
        {
            _segMan = segMan;
            _screen = screen;

            _windowList = new List<Port>();
        }

        public void SaveLoadWithSerializer(Serializer ser)
        {
            throw new NotImplementedException();
        }

        public void BackColor(short color)
        {
            _curPort.backClr = color;
        }

        public void Init(bool usesOldGfxFunctions, GfxPaint16 paint16, GfxText16 text16)
        {
            short offTop = 10;

            _usesOldGfxFunctions = usesOldGfxFunctions;
            _paint16 = paint16;
            _text16 = text16;

            _freeCounter = 0;

            // _menuPort has actually hardcoded id 0xFFFF. Its not meant to be known to windowmanager according to sierra sci
            _menuPort = new Port(0xFFFF);
            OpenPort(_menuPort);
            SetPort(_menuPort);
            _text16.SetFont(0);
            _menuPort.rect = new Rect(0, 0, _screen.ScriptWidth, _screen.ScriptHeight);
            _menuBarRect = new Rect(0, 0, _screen.ScriptWidth, 9);
            _menuRect = new Rect(0, 0, _screen.ScriptWidth, 10);
            _menuLine = new Rect(0, 9, _screen.ScriptWidth, 10);

            _wmgrPort = new Port(1);
            _windowsById = new List<Port>() {
                _wmgrPort, // wmgrPort is supposed to be accessible via id 0
                _wmgrPort //  but wmgrPort may not actually have id 0, so we assign id 1 (as well)
                                             // Background: sierra sci replies with the offset of curPort on kGetPort calls. If we reply with 0 there most games
                                             //				will work, but some scripts seem to check for 0 and initialize the variable again in that case
                                             //				resulting in problems.
            };

            if (ResourceManager.GetSciVersion() >= SciVersion.V1_LATE)
                _styleUser = WindowManagerStyle.USER;
            else
                _styleUser = WindowManagerStyle.USER | WindowManagerStyle.TRANSPARENT;

            // Jones, Slater, Hoyle 3&4 and Crazy Nicks Laura Bow/Kings Quest were
            // called with parameter -Nw 0 0 200 320.
            // Mother Goose (SCI1) uses -Nw 0 0 159 262. The game will later use
            // SetPort so we don't need to set the other fields.
            // This actually meant not skipping the first 10 pixellines in windowMgrPort
            switch (SciEngine.Instance.GameId)
            {
                case SciGameId.JONES:
                case SciGameId.SLATER:
                case SciGameId.HOYLE3:
                case SciGameId.HOYLE4:
                case SciGameId.CNICK_LAURABOW:
                case SciGameId.CNICK_KQ:
                    offTop = 0;
                    break;
                case SciGameId.MOTHERGOOSE256:
                    // only the SCI1 and SCI1.1 (VGA) versions need this
                    offTop = 0;
                    break;
                case SciGameId.FAIRYTALES:
                    // Mixed-Up Fairy Tales (& its demo) uses -w 26 0 200 320. If we don't
                    // also do this we will get not-fully-removed windows everywhere.
                    offTop = 26;
                    break;
                default:
                    // For Mac games running with a height of 190, we do not have a menu bar
                    // so the top offset should be 0.
                    if (_screen.Height == 190)
                        offTop = 0;
                    break;
            }

            OpenPort(_wmgrPort);
            SetPort(_wmgrPort);
            // SCI0 games till kq4 (.502 - not including) did not adjust against _wmgrPort in kNewWindow
            //  We leave _wmgrPort top at 0, so the adjustment wont get done
            if (!_usesOldGfxFunctions)
            {
                SetOrigin(0, offTop);
                _wmgrPort.rect.Bottom = _screen.Height - offTop;
            }
            else
            {
                _wmgrPort.rect.Bottom = _screen.Height;
            }
            _wmgrPort.rect.Right = _screen.ScriptWidth;
            _wmgrPort.rect.MoveTo(0, 0);
            _wmgrPort.curTop = 0;
            _wmgrPort.curLeft = 0;
            _windowList.Add(_wmgrPort);

            _picWind = AddWindow(new Rect(0, offTop, _screen.ScriptWidth, _screen.ScriptHeight), null, null, WindowManagerStyle.TRANSPARENT | WindowManagerStyle.NOFRAME, 0, true);
            // For SCI0 games till kq4 (.502 - not including) we set _picWind top to offTop instead
            //  Because of the menu/status bar
            if (_usesOldGfxFunctions)
                _picWind.top = offTop;

            KernelInitPriorityBands();
        }

        public void KernelDisposeWindow(ushort windowId, bool reanimate)
        {
            Window wnd = (Window)GetPortById(windowId);
            if (wnd != null)
            {
                if (wnd.counterTillFree == 0)
                {
                    RemoveWindow(wnd, reanimate);
                }
                else
                {
                    throw new InvalidOperationException($"kDisposeWindow: used already disposed window id {windowId}");
                }
            }
            else
            {
                throw new InvalidOperationException($"kDisposeWindow: used unknown window id {windowId}");
            }
        }

        private void RemoveWindow(Window pWnd, bool reanimate)
        {
            SetPort(_wmgrPort);
            _paint16.BitsRestore(pWnd.hSaved1);
            pWnd.hSaved1 = Register.NULL_REG;
            _paint16.BitsRestore(pWnd.hSaved2);
            pWnd.hSaved2 = Register.NULL_REG;
            if (!reanimate)
                _paint16.BitsShow(pWnd.restoreRect);
            else
                _paint16.KernelGraphRedrawBox(pWnd.restoreRect);
            _windowList.Remove(pWnd);
            SetPort(_windowList.Last());
            // We will actually free this window after 15 kSetPort-calls
            // Sierra sci freed the pointer immediately, but pointer to that port
            //  still worked till the memory got overwritten. Some games depend
            //  on this (dispose a window and then kSetPort to it again for once)
            //  Those are actually script bugs, but patching all of those out
            //  would be quite a hassle and this just keeps compatibility
            //  (examples: hoyle 4 game menu and sq4cd inventory)
            //  sq4cd gum wrapper requires more than 10
            pWnd.counterTillFree = 15;
            _freeCounter++;
        }

        public void ClipLine(ref Point start, ref Point end)
        {
            start.Y = ScummHelper.Clip(start.Y, _curPort.rect.Top, _curPort.rect.Bottom - 1);
            start.X = ScummHelper.Clip(start.X, _curPort.rect.Left, _curPort.rect.Right - 1);
            end.Y = ScummHelper.Clip(end.Y, _curPort.rect.Top, _curPort.rect.Bottom - 1);
            end.X = ScummHelper.Clip(end.X, _curPort.rect.Left, _curPort.rect.Right - 1);
        }

        internal void PriorityBandsInitSci11(ByteAccess byteAccess)
        {
            throw new NotImplementedException();
        }

        public void BeginUpdate(Window wnd)
        {
            Port oldPort = SetPort(_wmgrPort);
            var index = _windowList.IndexOf(wnd);
            for (var i = _windowList.Count - 1; i != index; i--)
            {
                var port = _windowList[i];
                // We also store Port objects in the window list, but they
                // shouldn't be encountered during this iteration.
                System.Diagnostics.Debug.Assert(port.IsWindow);

                UpdateWindow((Window)port);
            }
            SetPort(oldPort);
        }

        public void EndUpdate(Window wnd)
        {
            Port oldPort = SetPort(_wmgrPort);
            var index = _windowList.IndexOf(wnd);

            // wnd has to be in _windowList
            System.Diagnostics.Debug.Assert(index!=-1);

            for (var i = _windowList.Count - 1; i != index; i--)
            {
                var port = _windowList[i];
                // We also store Port objects in the window list, but they
                // shouldn't be encountered during this iteration.
                System.Diagnostics.Debug.Assert(port.IsWindow);

                UpdateWindow((Window)port);
            }

            if (ResourceManager.GetSciVersion() < SciVersion.V1_EGA_ONLY)
                SciEngine.Instance._gfxPaint16.KernelGraphRedrawBox(_curPort.rect);

            SetPort(oldPort);
        }

        public Register KernelGetActive()
        {
            return Register.Make(0, Port.id);
        }

        public void KernelGraphAdjustPriority(int top, int bottom)
        {
            if (_usesOldGfxFunctions)
            {
                PriorityBandsInit(15, (short)top, (short)bottom);
            }
            else
            {
                PriorityBandsInit(14, (short)top, (short)bottom);
            }
        }

        public void ProcessEngineHunkList(WorklistManager wm)
        {
            foreach (var wnd in _windowList.OfType<Window>())
            {
                wm.Push(wnd.hSaved1);
                wm.Push(wnd.hSaved2);
            }
        }

        public void PenMode(short mode)
        {
            _curPort.penMode = mode;
        }

        public void TextGreyedOutput(bool state)
        {
            _curPort.greyedOutput = state;
        }

        public void OffsetRect(ref Rect r)
        {
            r.Top += _curPort.top;
            r.Bottom += _curPort.top;
            r.Left += _curPort.left;
            r.Right += _curPort.left;
        }

        private void KernelInitPriorityBands()
        {
            if (_usesOldGfxFunctions)
            {
                PriorityBandsInit(15, 42, 200);
            }
            else
            {
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                    PriorityBandsInit(14, 0, 190);
                else
                    PriorityBandsInit(14, 42, 190);
            }
        }

        public void PriorityBandsInit(short bandCount, short top, short bottom)
        {
            short y;
            int bandSize;

            if (bandCount != -1)
                _priorityBandCount = bandCount;

            _priorityTop = top;
            _priorityBottom = bottom;

            // Do NOT modify this algo or optimize it anyhow, sierra sci used int32 for calculating the
            //  priority bands and by using double or anything rounding WILL destroy the result
            bandSize = ((_priorityBottom - _priorityTop) * 2000) / _priorityBandCount;

            Array.Clear(_priorityBands, 0, _priorityTop);
            for (y = _priorityTop; y < _priorityBottom; y++)
                _priorityBands[y] = (byte)(1 + (((y - _priorityTop) * 2000) / bandSize));
            if (_priorityBandCount == 15)
            {
                // When having 15 priority bands, we actually replace band 15 with band 14, cause the original sci interpreter also
                //  does it that way as well
                y = _priorityBottom;
                while (_priorityBands[--y] == _priorityBandCount)
                    _priorityBands[y]--;
            }
            // We fill space that is left over with the highest band (hardcoded 200 limit, because this algo isnt meant to be used on hires)
            for (y = _priorityBottom; y < 200; y++)
                _priorityBands[y] = (byte)_priorityBandCount;

            // adjust, if bottom is 200 (one over the actual screen range) - we could otherwise go possible out of bounds
            //  sierra sci also adjust accordingly
            if (_priorityBottom == 200)
                _priorityBottom--;
        }

        public short KernelCoordinateToPriority(short y)
        {
            if (y < _priorityTop)
                return _priorityBands[_priorityTop];
            if (y > _priorityBottom)
                return _priorityBands[_priorityBottom];
            return _priorityBands[y];
        }

        public void PriorityBandsInit(ByteAccess data)
        {
            int i = 0, inx;
            byte priority = 0;

            for (inx = 0; inx < 14; inx++)
            {
                priority = data.Increment();
                while (i < priority)
                    _priorityBands[i++] = (byte)inx;
            }
            while (i < 200)
                _priorityBands[i++] = (byte)inx;
        }

        public bool IsFrontWindow(Window pWnd)
        {
            return _windowList.LastOrDefault() == pWnd;
        }

        private Window AddWindow(Rect dims, Rect? restoreRect, string title, WindowManagerStyle style, int priority, bool draw)
        {
            // Find an unused window/port id
            int id = PORTS_FIRSTWINDOWID;
            while (id < _windowsById.Count && _windowsById[id] != null)
            {
                if (_windowsById[id].counterTillFree != 0)
                {
                    // port that is already disposed, but not freed yet
                    FreeWindow((Window)_windowsById[id]);
                    _freeCounter--;
                    break; // reuse the handle
                           // we do this especially for sq4cd. it creates and disposes the
                           //  inventory window all the time, but reuses old handles as well
                           //  this worked somewhat under the original interpreter, because
                           //  it put the new window where the old was.
                }
                ++id;
            }
            if (id == _windowsById.Count)
                _windowsById.Add(null);
            //assert(0 < id && id < 0xFFFF);

            Window pwnd = new Window((ushort)id);
            Rect r;

            _windowsById[id] = pwnd;

            // KQ1sci, KQ4, iceman, QfG2 always add windows to the back of the list.
            // KQ5CD checks style.
            // Hoyle3-demo also always adds to the back (#3036763).
            bool forceToBack = (ResourceManager.GetSciVersion() <= SciVersion.V1_EGA_ONLY) ||
                               (SciEngine.Instance.GameId == SciGameId.HOYLE3 && SciEngine.Instance.IsDemo);

            if (!forceToBack && style.HasFlag(WindowManagerStyle.TOPMOST))
                _windowList.Insert(0, pwnd);
            else
                _windowList.Add(pwnd);
            OpenPort(pwnd);

            r = dims;
            // This looks fishy, but it's exactly what Sierra did. They removed last
            // bit of the left dimension in their interpreter. It seems Sierra did it
            // for EGA byte alignment (EGA uses 1 byte for 2 pixels) and left it in
            // their interpreter even in the newer VGA games.
            r.Left = r.Left & 0xFFFE;

            if (r.Width > _screen.ScriptWidth)
            {
                // We get invalid dimensions at least at the end of sq3 (script bug!).
                // Same happens very often in lsl5, sierra sci didnt fix it but it looked awful.
                // Also happens frequently in the demo of GK1.
                Warning($"Fixing too large window, left: {dims.Left}, right: {dims.Right}");
                r.Left = 0;
                r.Right = _screen.ScriptWidth - 1;
                if ((style != _styleUser) && !style.HasFlag(WindowManagerStyle.NOFRAME))
                    r.Right--;
            }
            pwnd.rect = r;
            if (restoreRect.HasValue)
                pwnd.restoreRect = restoreRect.Value;

            pwnd.wndStyle = style;
            pwnd.hSaved1 = pwnd.hSaved2 = Register.NULL_REG;
            pwnd.bDrawn = false;
            if (!style.HasFlag(WindowManagerStyle.TRANSPARENT))
                pwnd.saveScreenMask = (priority == -1 ? GfxScreenMasks.VISUAL : GfxScreenMasks.VISUAL | GfxScreenMasks.PRIORITY);

            if (title != null && style.HasFlag(WindowManagerStyle.TITLE))
            {
                pwnd.title = title;
            }

            r = pwnd.rect;
            if ((style != _styleUser) && !style.HasFlag(WindowManagerStyle.NOFRAME))
            {
                r.Grow(1);
                if (style.HasFlag(WindowManagerStyle.TITLE))
                {
                    r.Top -= 10;
                    r.Bottom++;
                }
            }

            pwnd.dims = r;

            // Clip window, if needed
            Rect wmprect = _wmgrPort.rect;
            // Handle a special case for Dr. Brain 1 Mac. When hovering the mouse cursor
            // over the status line on top, the game scripts try to draw the game's icon
            // bar above the current port, by specifying a negative window top, so that
            // the end result will be drawn 10 pixels above the current port. This is a
            // hack by Sierra, and is only limited to user style windows. Normally, we
            // should not clip, same as what Sierra does. However, this will result in
            // having invalid rectangles with negative coordinates. For this reason, we
            // adjust the containing rectangle instead.
            if (pwnd.dims.Top < 0 && SciEngine.Instance.Platform == Core.IO.Platform.Macintosh &&
                style.HasFlag(WindowManagerStyle.USER) && _wmgrPort.top + pwnd.dims.Top >= 0)
            {
                // Offset the final rect top by the requested pixels
                wmprect.Top += pwnd.dims.Top;
            }

            short oldtop = (short)pwnd.dims.Top;
            short oldleft = (short)pwnd.dims.Left;

            // WORKAROUND: We also adjust the restore rect when adjusting the window
            // rect.
            // SSCI does not do this. It wasn't necessary in the original interpreter,
            // but it is needed for Freddy Pharkas CD. This version does not normally
            // have text, but we allow this by modifying the text/speech setting
            // according to what is set in the ScummVM GUI (refer to syncIngameAudioOptions()
            // in sci.cpp). Since the text used in Freddy Pharkas CD is quite large in
            // some cases, it ends up being offset in order to fit inside the screen,
            // but the associated restore rect isn't adjusted accordingly, leading to
            // artifacts being left on screen when some text boxes are removed. The
            // fact that the restore rect wasn't ever adjusted doesn't make sense, and
            // adjusting it shouldn't have any negative side-effects (it *should* be
            // adjusted, normally, but SCI doesn't do it). The big text boxes are still
            // odd-looking, because the text rect is drawn outside the text window rect,
            // but at least there aren't any leftover textbox artifacts left when the
            // boxes are removed. Adjusting the text window rect would require more
            // invasive changes than this one, thus it's not really worth the effort
            // for a feature that was not present in the original game, and its
            // implementation is buggy in the first place.
            // Adjusting the restore rect properly fixes bug #3575276.

            if (wmprect.Top > pwnd.dims.Top)
            {
                pwnd.dims.MoveTo(pwnd.dims.Left, wmprect.Top);
                if (restoreRect.HasValue)
                    pwnd.restoreRect.MoveTo(pwnd.restoreRect.Left, wmprect.Top);
            }

            if (wmprect.Bottom < pwnd.dims.Bottom)
            {
                pwnd.dims.MoveTo(pwnd.dims.Left, wmprect.Bottom - pwnd.dims.Bottom + pwnd.dims.Top);
                if (restoreRect.HasValue)
                    pwnd.restoreRect.MoveTo(pwnd.restoreRect.Left, wmprect.Bottom - pwnd.restoreRect.Bottom + pwnd.restoreRect.Top);
            }

            if (wmprect.Right < pwnd.dims.Right)
            {
                pwnd.dims.MoveTo(wmprect.Right + pwnd.dims.Left - pwnd.dims.Right, pwnd.dims.Top);
                if (restoreRect.HasValue)
                    pwnd.restoreRect.MoveTo(wmprect.Right + pwnd.restoreRect.Left - pwnd.restoreRect.Right, pwnd.restoreRect.Top);
            }

            if (wmprect.Left > pwnd.dims.Left)
            {
                pwnd.dims.MoveTo(wmprect.Left, pwnd.dims.Top);
                if (restoreRect.HasValue)
                    pwnd.restoreRect.MoveTo(wmprect.Left, pwnd.restoreRect.Top);
            }

            pwnd.rect.MoveTo(pwnd.rect.Left + pwnd.dims.Left - oldleft, pwnd.rect.Top + pwnd.dims.Top - oldtop);

            if (!restoreRect.HasValue)
                pwnd.restoreRect = pwnd.dims;

            if (pwnd.restoreRect.Top < 0 && SciEngine.Instance.Platform == Core.IO.Platform.Macintosh &&
                style.HasFlag(WindowManagerStyle.USER) && _wmgrPort.top + pwnd.restoreRect.Top >= 0)
            {
                // Special case for Dr. Brain 1 Mac (check above), applied to the
                // restore rectangle.
                pwnd.restoreRect.MoveTo(pwnd.restoreRect.Left, wmprect.Top);
            }

            if (draw)
                DrawWindow(pwnd);
            SetPort(pwnd);

            // All SCI0 games till kq4 .502 (not including) did not adjust against _wmgrPort, we set _wmgrPort.top to 0 in that case
            SetOrigin((short)pwnd.rect.Left, (short)(pwnd.rect.Top + _wmgrPort.top));
            pwnd.rect.MoveTo(0, 0);
            return pwnd;
        }

        public Register KernelNewWindow(Rect dims, Rect restoreRect, ushort style, short priority, short colorPen, short colorBack, string title)
        {
            Window wnd = null;

            if (restoreRect.Bottom != 0 && restoreRect.Right != 0)
                wnd = AddWindow(dims, restoreRect, title, (WindowManagerStyle)style, priority, false);
            else
                wnd = AddWindow(dims, null, title, (WindowManagerStyle)style, priority, false);
            wnd.penClr = colorPen;
            wnd.backClr = colorBack;
            DrawWindow(wnd);

            return Register.Make(0, wnd.id);
        }

        public void KernelSetPicWindow(Rect rect, short picTop, short picLeft, bool initPriorityBandsFlag)
        {
            _picWind.rect = rect;
            _picWind.top = picTop;
            _picWind.left = picLeft;
            if (initPriorityBandsFlag)
                KernelInitPriorityBands();
        }

        public void KernelSetActive(ushort portId)
        {
            if (_freeCounter != 0)
            {
                // Windows waiting to get freed
                for (var id = PORTS_FIRSTSCRIPTWINDOWID; id < _windowsById.Count; id++)
                {
                    Window window = (Window)_windowsById[id];
                    if (window != null)
                    {
                        if (window.counterTillFree != 0)
                        {
                            window.counterTillFree--;
                            if (window.counterTillFree == 0)
                            {
                                FreeWindow(window);
                                _freeCounter--;
                            }
                        }
                    }
                }
            }

            switch (portId)
            {
                case 0:
                    SetPort(_wmgrPort);
                    break;
                case 0xFFFF:
                    SetPort(_menuPort);
                    break;
                default:
                    {
                        Port newPort = GetPortById(portId);
                        if (newPort != null)
                            SetPort(newPort);
                        else
                            throw new InvalidOperationException($"GfxPorts::kernelSetActive was requested to set invalid port id {portId}");
                    }
                    break;
            }
        }

        private Port GetPortById(ushort id)
        {
            return (id < _windowsById.Count) ? _windowsById[id] : null;
        }

        public void Move(short left, short top)
        {
            _curPort.curTop += top;
            _curPort.curLeft += left;
        }

        public short KernelPriorityToCoordinate(byte priority)
        {
            short y;
            if (priority <= _priorityBandCount)
            {
                for (y = 0; y <= _priorityBottom; y++)
                    if (_priorityBands[y] == priority)
                        return y;
            }
            return _priorityBottom;
        }

        public void OffsetLine(ref Point start, ref Point end)
        {
            start.X += _curPort.left;
            start.Y += _curPort.top;
            end.X += _curPort.left;
            end.Y += _curPort.top;
        }

        public void MoveTo(short left, short top)
        {
            _curPort.curTop = top;
            _curPort.curLeft = left;
        }

        private void DrawWindow(Window pWnd)
        {
            if (pWnd.bDrawn)
                return;
            var wndStyle = pWnd.wndStyle;

            pWnd.bDrawn = true;
            Port oldport = SetPort(_wmgrPort);
            PenColor(0);
            if (!wndStyle.HasFlag(WindowManagerStyle.TRANSPARENT))
            {
                pWnd.hSaved1 = _paint16.BitsSave(pWnd.restoreRect, GfxScreenMasks.VISUAL);
                if (pWnd.saveScreenMask.HasFlag(GfxScreenMasks.PRIORITY))
                {
                    pWnd.hSaved2 = _paint16.BitsSave(pWnd.restoreRect, GfxScreenMasks.PRIORITY);
                    if (!wndStyle.HasFlag(WindowManagerStyle.USER))
                        _paint16.FillRect(pWnd.restoreRect, GfxScreenMasks.PRIORITY, 0, 15);
                }
            }

            // drawing frame,shadow and title
            if ((ResourceManager.GetSciVersion() >= SciVersion.V1_LATE) ? !(wndStyle.HasFlag(_styleUser)) : wndStyle != _styleUser)
            {
                Rect r = pWnd.dims;

                if (!wndStyle.HasFlag(WindowManagerStyle.NOFRAME))
                {
                    r.Top++;
                    r.Left++;
                    _paint16.FrameRect(r);// draw shadow
                    r.Translate(-1, -1);
                    _paint16.FrameRect(r);// draw actual window frame

                    if (wndStyle.HasFlag(WindowManagerStyle.TITLE))
                    {
                        if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                        {
                            // draw a black line between titlebar and actual window content for SCI0
                            r.Bottom = r.Top + 10;
                            _paint16.FrameRect(r);
                        }
                        r.Grow(-1);
                        if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                            _paint16.FillRect(r, GfxScreenMasks.VISUAL, 8); // grey titlebar for SCI0
                        else
                            _paint16.FillRect(r, GfxScreenMasks.VISUAL, 0); // black titlebar for SCI01+
                        if (!string.IsNullOrEmpty(pWnd.title))
                        {
                            short oldcolor = Port.penClr;
                            PenColor(_screen.ColorWhite);
                            _text16.Box(pWnd.title, true, r, GfxText16.SCI_TEXT16_ALIGNMENT_CENTER, 0);
                            PenColor(oldcolor);
                        }

                        r.Grow(+1);
                        r.Bottom = pWnd.dims.Bottom - 1;
                        r.Top += 9;
                    }

                    r.Grow(-1);
                }

                if (!wndStyle.HasFlag(WindowManagerStyle.TRANSPARENT))
                    _paint16.FillRect(r, GfxScreenMasks.VISUAL, (byte)pWnd.backClr);

                _paint16.BitsShow(pWnd.dims);
            }
            SetPort(oldport);
        }

        public void PenColor(short color)
        {
            _curPort.penClr = color;
        }

        private void FreeWindow(Window pWnd)
        {
            if (!pWnd.hSaved1.IsNull)
                _segMan.FreeHunkEntry(pWnd.hSaved1);
            if (!pWnd.hSaved2.IsNull)
                _segMan.FreeHunkEntry(pWnd.hSaved2);
            _windowsById[pWnd.id] = null;
        }

        private void SetOrigin(short left, short top)
        {
            _curPort.left = left;
            _curPort.top = top;
        }

        public Port SetPort(Port newPort)
        {
            Port oldPort = _curPort;
            _curPort = newPort;
            return oldPort;
        }

        private void UpdateWindow(Window wnd)
        {
            Register handle;

            if (wnd.saveScreenMask != 0 && wnd.bDrawn)
            {
                handle = _paint16.BitsSave(wnd.restoreRect, GfxScreenMasks.VISUAL);
                _paint16.BitsRestore(wnd.hSaved1);
                wnd.hSaved1 = handle;
                if (wnd.saveScreenMask.HasFlag(GfxScreenMasks.PRIORITY))
                {
                    handle = _paint16.BitsSave(wnd.restoreRect, GfxScreenMasks.PRIORITY);
                    _paint16.BitsRestore(wnd.hSaved2);
                    wnd.hSaved2 = handle;
                }
            }
        }

        private void OpenPort(Port port)
        {
            port.fontId = 0;
            port.fontHeight = 8;

            Port tmp = _curPort;
            _curPort = port;
            _text16.SetFont(port.fontId);
            _curPort = tmp;

            port.top = 0;
            port.left = 0;
            port.greyedOutput = false;
            port.penClr = 0;
            port.backClr = _screen.ColorWhite;
            port.penMode = 0;
            port.rect = _bounds;
        }
    }
}
