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

using NScumm.Sci.Engine;
using System.Collections.Generic;
using System;
using System.Linq;
using NScumm.Core.Graphics;
using NScumm.Core;
using NScumm.Core.Common;
using NScumm.Sci.Parser;

namespace NScumm.Sci.Graphics
{
    enum MenuAttribute
    {
        SAID = 0x6d,
        TEXT = 0x6e,
        KEYPRESS = 0x6f,
        ENABLED = 0x70,
        TAG = 0x71
    }

    class GuiMenuEntry
    {
        public ushort id;
        public string text;
        public string textSplit;
        public short textWidth;

        public GuiMenuEntry(ushort curId)
        {
            id = curId;
            textWidth = 0;
        }
    }

    class GuiMenuItemEntry
    {
        public ushort menuId;
        public ushort id;
        public bool enabled;
        public ushort tag;
        public ushort keyPress;
        public ushort keyModifier;
        public bool separatorLine;
        public Register saidVmPtr;
        public string text;
        public string textSplit;
        public Register textVmPtr;
        public short textWidth;
        public string textRightAligned;
        public short textRightAlignedWidth;

        public GuiMenuItemEntry(ushort curMenuId, ushort curId)
        {
            menuId = curMenuId;
            id = curId;
            enabled = true;
            tag = 0; keyPress = 0;
            keyModifier = 0;
            separatorLine = false;
            textWidth = 0;
            textRightAlignedWidth = 0;
            saidVmPtr = Register.NULL_REG;
            textVmPtr = Register.NULL_REG;
            text = string.Empty;
            textSplit = string.Empty;
            textRightAligned = string.Empty;
        }
    }

    /// <summary>
    /// Menu class, handles game pulldown menu for SCI16 (SCI0-SCI1.1) games
    /// </summary>
    internal class GfxMenu
    {
        private const char SCI_MENU_REPLACE_ONCONTROL = '\x3';
        private const char SCI_MENU_REPLACE_ONALT = '\x2';
        private const char SCI_MENU_REPLACE_ONFUNCTION = 'F';

        private EventManager _event;
        private GfxCursor _cursor;
        private GfxPaint16 _paint16;
        private GfxPorts _ports;
        private GfxScreen _screen;
        private GfxText16 _text16;
        private SegManager _segMan;

        private Port _oldPort;
        private Register _barSaveHandle;
        private Register _menuSaveHandle;
        private Core.Graphics.Rect _menuRect;

        private bool _mouseOldState;

        private List<GuiMenuEntry> _list;
        private List<GuiMenuItemEntry> _itemList;

        private ushort _curMenuId;
        private ushort _curItemId;

        public GfxMenu(EventManager eventMan, SegManager segMan, GfxPorts ports, GfxPaint16 paint16, GfxText16 text16, GfxScreen screen, GfxCursor cursor)
        {
            _event = eventMan;
            _segMan = segMan;
            _ports = ports;
            _paint16 = paint16;
            _text16 = text16;
            _screen = screen;
            _cursor = cursor;
            _list = new List<GuiMenuEntry>();
            _itemList = new List<GuiMenuItemEntry>();

            _menuSaveHandle = Register.NULL_REG;
            _barSaveHandle = Register.NULL_REG;
            _oldPort = null;
            _mouseOldState = false;

            Reset();
        }

        public Register KernelSelect(Register eventObject, bool pauseSound)
        {
            short eventType = (short)SciEngine.ReadSelectorValue(_segMan, eventObject, o => o.type);
            short keyPress, keyModifier;
            GuiMenuItemEntry itemEntry = null;
            int i;
            bool forceClaimed = false;

            switch (eventType)
            {
                case SciEvent.SCI_EVENT_KEYBOARD:
                    keyPress = (short)SciEngine.ReadSelectorValue(_segMan, eventObject, o => o.message);
                    keyModifier = (short)SciEngine.ReadSelectorValue(_segMan, eventObject, o => o.modifiers);
                    // If tab got pressed, handle it here as if it was Ctrl-I - at least
                    // sci0 also did it that way
                    if (keyPress == SciEvent.SCI_KEY_TAB)
                    {
                        keyModifier = SciEvent.SCI_KEYMOD_CTRL;
                        keyPress = (short)'i';
                    }
                    switch (keyPress)
                    {
                        case 0:
                            break;
                        case SciEvent.SCI_KEY_ESC:
                            InteractiveStart(pauseSound);
                            itemEntry = InteractiveWithKeyboard();
                            InteractiveEnd(pauseSound);
                            forceClaimed = true;
                            break;
                        default:
                            for (i = 0; i < _itemList.Count; i++)
                            {
                                itemEntry = _itemList[i];
                                if (itemEntry.keyPress == keyPress &&
                                    itemEntry.keyModifier == keyModifier &&
                                    itemEntry.enabled)
                                    break;
                            }
                            if (i == _itemList.Count)
                                itemEntry = null;
                            break;
                    }
                    break;

                case SciEvent.SCI_EVENT_SAID:
                    for (i = 0; i < _itemList.Count; i++)
                    {
                        itemEntry = _itemList[i];

                        if (!itemEntry.saidVmPtr.IsNull)
                        {
                            var saidSpec = _segMan.DerefBulkPtr(itemEntry.saidVmPtr, 0);

                            if (saidSpec == null)
                            {
                                // TODO: warning("Could not dereference saidSpec");
                                continue;
                            }

                            if (Said(saidSpec, 0) != Vocabulary.SAID_NO_MATCH)
                                break;
                        }
                    }
                    if (i == _itemList.Count)
                        itemEntry = null;
                    break;

                case SciEvent.SCI_EVENT_MOUSE_PRESS:
                    {
                        Point mousePosition = new Point();
                        mousePosition.X = (int)SciEngine.ReadSelectorValue(_segMan, eventObject, o => o.x);
                        mousePosition.Y = (int)SciEngine.ReadSelectorValue(_segMan, eventObject, o => o.y);
                        if (mousePosition.Y < 10)
                        {
                            InteractiveStart(pauseSound);
                            itemEntry = InteractiveWithMouse();
                            InteractiveEnd(pauseSound);
                            forceClaimed = true;
                        }
                    }
                    break;
            }

            if (!_menuSaveHandle.IsNull)
            {
                _paint16.BitsRestore(_menuSaveHandle);
                // Display line inbetween menubar and actual menu
                Rect menuLine = _menuRect;
                menuLine.Bottom = menuLine.Top + 1;
                _paint16.BitsShow(menuLine);
                _paint16.KernelGraphRedrawBox(_menuRect);
                _menuSaveHandle = Register.NULL_REG;
            }
            if (!_barSaveHandle.IsNull)
            {
                _paint16.BitsRestore(_barSaveHandle);
                _paint16.BitsShow(_ports._menuRect);
                _barSaveHandle = Register.NULL_REG;
            }
            if (_oldPort != null)
            {
                _ports.SetPort(_oldPort);
                _oldPort = null;
            }

            if ((itemEntry != null) || (forceClaimed))
                SciEngine.WriteSelector(_segMan, eventObject, o => o.claimed, Register.Make(0, 1));
            if (null != itemEntry)
                return Register.Make(0, (ushort)((itemEntry.menuId << 8) | (itemEntry.id)));
            return Register.NULL_REG;
        }

        private int Said(ByteAccess saidSpec, int v)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Mouse button is currently pressed - we are now interpreting mouse coordinates
        /// till mouse button is released. The menu item that is selected at that time is
        /// chosen. If no menu item is selected we cancel. No keyboard interaction is
        /// allowed, cause that wouldnt make any sense at all.
        /// </summary>
        /// <returns></returns>
        private GuiMenuItemEntry InteractiveWithMouse()
        {
            SciEvent curEvent;
            ushort newMenuId = 0, newItemId = 0;
            ushort curMenuId = 0, curItemId = 0;
            bool firstMenuChange = true;
            GuiMenuItemEntry curItemEntry = null;

            _oldPort = _ports.SetPort(_ports._menuPort);
            CalculateMenuAndItemWidth();
            _barSaveHandle = _paint16.BitsSave(_ports._menuRect, GfxScreenMasks.VISUAL);

            _ports.PenColor(0);
            _ports.BackColor(_screen.ColorWhite);

            DrawBar();
            _paint16.BitsShow(_ports._menuRect);

            while (true)
            {
                curEvent = _event.GetSciEvent(SciEvent.SCI_EVENT_ANY);

                switch (curEvent.type)
                {
                    case SciEvent.SCI_EVENT_MOUSE_RELEASE:
                        if ((curMenuId == 0) || (curItemId == 0))
                            return null;
                        if ((!curItemEntry.enabled) || (curItemEntry.separatorLine))
                            return null;
                        return curItemEntry;

                    case SciEvent.SCI_EVENT_NONE:
                        SciEngine.Instance.Sleep(2500 / 1000);
                        break;
                }

                // Find out where mouse is currently pointing to
                Point mousePosition = curEvent.mousePos;
                if (mousePosition.Y < 10)
                {
                    // Somewhere on the menubar
                    newMenuId = MouseFindMenuSelection(mousePosition);
                    newItemId = 0;
                }
                else {
                    // Somewhere below menubar
                    newItemId = MouseFindMenuItemSelection(mousePosition, newMenuId);
                    curItemEntry = InteractiveGetItem(curMenuId, newItemId, false);
                }

                if (newMenuId != curMenuId)
                {
                    // Menu changed, remove cur menu and paint new menu
                    DrawMenu(curMenuId, newMenuId);
                    if (firstMenuChange)
                    {
                        _paint16.BitsShow(_ports._menuBarRect);
                        firstMenuChange = false;
                    }
                    curMenuId = newMenuId;
                }
                else {
                    if (newItemId != curItemId)
                    {
                        // Item changed
                        InvertMenuSelection(curItemId);
                        InvertMenuSelection(newItemId);
                        curItemId = newItemId;
                    }
                }

            }
            return null;
        }

        public GuiMenuItemEntry InteractiveWithKeyboard()
        {
            SciEvent curEvent;
            ushort newMenuId = _curMenuId;
            ushort newItemId = _curItemId;
            GuiMenuItemEntry curItemEntry = FindItem(_curMenuId, _curItemId);
            GuiMenuItemEntry newItemEntry = curItemEntry;

            // We don't 100% follow Sierra here: we select last item instead of
            // selecting first item of first menu every time. Also sierra sci didn't
            // allow mouse interaction, when menu was activated via keyboard.

            _oldPort = _ports.SetPort(_ports._menuPort);
            CalculateMenuAndItemWidth();
            _barSaveHandle = _paint16.BitsSave(_ports._menuRect, GfxScreenMasks.VISUAL);

            _ports.PenColor(0);
            _ports.BackColor(_screen.ColorWhite);

            DrawBar();
            DrawMenu(0, curItemEntry.menuId);
            InvertMenuSelection(curItemEntry.id);
            _paint16.BitsShow(_ports._menuRect);
            _paint16.BitsShow(_menuRect);

            while (true)
            {
                curEvent = _event.GetSciEvent(SciEvent.SCI_EVENT_ANY);

                switch (curEvent.type)
                {
                    case SciEvent.SCI_EVENT_KEYBOARD:
                        // We don't 100% follow sierra here:
                        // - sierra didn't wrap around when changing item id
                        // - sierra allowed item id to be 0, which didn't make any sense
                        do
                        {
                            switch (curEvent.data)
                            {
                                case SciEvent.SCI_KEY_ESC:
                                    _curMenuId = curItemEntry.menuId; _curItemId = curItemEntry.id;
                                    return null;
                                case SciEvent.SCI_KEY_ENTER:
                                    if (curItemEntry.enabled)
                                    {
                                        _curMenuId = curItemEntry.menuId; _curItemId = curItemEntry.id;
                                        return curItemEntry;
                                    }
                                    break;
                                case SciEvent.SCI_KEY_LEFT:
                                    newMenuId--; newItemId = 1;
                                    break;
                                case SciEvent.SCI_KEY_RIGHT:
                                    newMenuId++; newItemId = 1;
                                    break;
                                case SciEvent.SCI_KEY_UP:
                                    newItemId--;
                                    break;
                                case SciEvent.SCI_KEY_DOWN:
                                    newItemId++;
                                    break;
                            }
                            if ((newMenuId != curItemEntry.menuId) || (newItemId != curItemEntry.id))
                            {
                                // Selection changed, fix up new selection if required
                                newItemEntry = InteractiveGetItem(newMenuId, newItemId, newMenuId != curItemEntry.menuId);
                                newMenuId = newItemEntry.menuId; newItemId = newItemEntry.id;

                                // if we do this step again because of a separator line . don't repeat left/right, but go down
                                switch (curEvent.data)
                                {
                                    case SciEvent.SCI_KEY_LEFT:
                                    case SciEvent.SCI_KEY_RIGHT:
                                        curEvent.data = SciEvent.SCI_KEY_DOWN;
                                        break;
                                }
                            }
                        } while (newItemEntry.separatorLine);
                        if ((newMenuId != curItemEntry.menuId) || (newItemId != curItemEntry.id))
                        {
                            // paint old and new
                            if (newMenuId != curItemEntry.menuId)
                            {
                                // Menu changed, remove cur menu and paint new menu
                                DrawMenu(curItemEntry.menuId, newMenuId);
                            }
                            else {
                                InvertMenuSelection(curItemEntry.id);
                            }
                            InvertMenuSelection(newItemId);

                            curItemEntry = newItemEntry;
                        }
                        break;

                    case SciEvent.SCI_EVENT_MOUSE_PRESS:
                        {
                            Point mousePosition = curEvent.mousePos;
                            if (mousePosition.Y < 10)
                            {
                                // Somewhere on the menubar
                                newMenuId = MouseFindMenuSelection(mousePosition);
                                if (newMenuId != 0)
                                {
                                    newItemId = 1;
                                    newItemEntry = InteractiveGetItem(newMenuId, newItemId, newMenuId != curItemEntry.menuId);
                                    if (newMenuId != curItemEntry.menuId)
                                    {
                                        DrawMenu(curItemEntry.menuId, newMenuId);
                                    }
                                    else {
                                        InvertMenuSelection(curItemEntry.id);
                                    }
                                    InvertMenuSelection(newItemId);
                                    curItemEntry = newItemEntry;
                                }
                                else {
                                    newMenuId = curItemEntry.menuId;
                                }
                            }
                            else {
                                // Somewhere below menubar
                                newItemId = MouseFindMenuItemSelection(mousePosition, newMenuId);
                                if (newItemId != 0)
                                {
                                    newItemEntry = InteractiveGetItem(newMenuId, newItemId, false);
                                    if ((newItemEntry.enabled) && (!newItemEntry.separatorLine))
                                    {
                                        _curMenuId = newItemEntry.menuId; _curItemId = newItemEntry.id;
                                        return newItemEntry;
                                    }
                                    newItemEntry = curItemEntry;
                                }
                                newItemId = curItemEntry.id;
                            }
                        }
                        break;

                    case SciEvent.SCI_EVENT_NONE:
                        ServiceLocator.Platform.Sleep(2500 / 1000);
                        break;
                }
            }
        }

        private ushort MouseFindMenuSelection(Point mousePosition)
        {
            GuiMenuEntry listEntry;
            ushort curXstart = 8;

            for (int i = 0; i < _list.Count; i++)
            {
                listEntry = _list[i];
                if (mousePosition.X >= curXstart && mousePosition.X < curXstart + listEntry.textWidth)
                {
                    return listEntry.id;
                }
                curXstart += (ushort)listEntry.textWidth;
            }
            return 0;
        }

        private ushort MouseFindMenuItemSelection(Point mousePosition, ushort menuId)
        {
            GuiMenuItemEntry listItemEntry;
            ushort curYstart = 10;
            ushort itemId = 0;

            if (menuId == 0)
                return 0;

            if ((mousePosition.X < _menuRect.Left) || (mousePosition.X >= _menuRect.Right))
                return 0;

            for (int i = 0; i < _itemList.Count; i++)
            {
                listItemEntry = _itemList[i];
                if (listItemEntry.menuId == menuId)
                {
                    curYstart += (ushort)_ports._curPort.fontHeight;
                    // Found it
                    if ((itemId == 0) && (curYstart > mousePosition.Y))
                        itemId = listItemEntry.id;
                }
            }
            return itemId;
        }

        private void InvertMenuSelection(ushort itemId)
        {
            Rect itemRect = _menuRect;

            if (itemId == 0)
                return;

            itemRect.Top += (itemId - 1) * _ports._curPort.fontHeight + 1;
            itemRect.Bottom = itemRect.Top + _ports._curPort.fontHeight;
            itemRect.Left++; itemRect.Right--;

            _paint16.InvertRect(itemRect);
            _paint16.BitsShow(itemRect);
        }

        private void DrawMenu(int oldMenuId, ushort newMenuId)
        {
            GuiMenuEntry listEntry;
            GuiMenuItemEntry listItemEntry;
            Rect menuTextRect;
            ushort listNr = 0;
            short maxTextWidth = 0, maxTextRightAlignedWidth = 0;
            short topPos;
            Point pixelPos;

            // Remove menu, if one is displayed
            if (!_menuSaveHandle.IsNull)
            {
                _paint16.BitsRestore(_menuSaveHandle);
                // Display line inbetween menubar and actual menu
                Rect menuLine = _menuRect;
                menuLine.Bottom = menuLine.Top + 1;
                _paint16.BitsShow(menuLine);
                _paint16.KernelGraphRedrawBox(_menuRect);
            }

            // First calculate rect of menu and also invert old and new menu text
            _menuRect.Top = _ports._menuBarRect.Bottom;
            menuTextRect.Top = _ports._menuBarRect.Top;
            menuTextRect.Bottom = _ports._menuBarRect.Bottom;
            menuTextRect.Left = menuTextRect.Right = 7;
            for (int i = 0; i < _list.Count; i++)
            {
                listEntry = _list[i];
                listNr++;
                menuTextRect.Left = menuTextRect.Right;
                menuTextRect.Right += listEntry.textWidth;
                if (listNr == newMenuId)
                    _menuRect.Left = menuTextRect.Left;
                if ((listNr == newMenuId) || (listNr == oldMenuId))
                {
                    menuTextRect.Translate(1, 0);
                    _paint16.InvertRect(menuTextRect);
                    menuTextRect.Translate(-1, 0);
                }
            }
            _paint16.BitsShow(_ports._menuBarRect);

            _menuRect.Bottom = _menuRect.Top + 2;
            for (int i = 0; i < _itemList.Count; i++)
            {
                listItemEntry = _itemList[i];
                if (listItemEntry.menuId == newMenuId)
                {
                    _menuRect.Bottom += _ports._curPort.fontHeight;
                    maxTextWidth = Math.Max(maxTextWidth, listItemEntry.textWidth);
                    maxTextRightAlignedWidth = Math.Max(maxTextRightAlignedWidth, listItemEntry.textRightAlignedWidth);
                }
            }
            _menuRect.Right = _menuRect.Left + 16 + 4 + 2;
            _menuRect.Right += maxTextWidth + maxTextRightAlignedWidth;
            if (maxTextRightAlignedWidth == 0)
                _menuRect.Right -= 5;

            // If part of menu window is outside the screen, move it into the screen
            // (this happens in multilingual sq3 and lsl3).
            if (_menuRect.Right > _screen.Width)
            {
                _menuRect.Translate(-(_menuRect.Right - _screen.Width), 0);
            }

            // Save background
            _menuSaveHandle = _paint16.BitsSave(_menuRect, GfxScreenMasks.VISUAL);

            // Do the drawing
            _paint16.FillRect(_menuRect, GfxScreenMasks.VISUAL, 0);
            _menuRect.Left++; _menuRect.Right--; _menuRect.Bottom--;
            _paint16.FillRect(_menuRect, GfxScreenMasks.VISUAL, _screen.ColorWhite);

            _menuRect.Left += 8;
            topPos = (short)(_menuRect.Top + 1);
            for (int i = 0; i < _itemList.Count; i++)
            {
                listItemEntry = _itemList[i];
                if (listItemEntry.menuId == newMenuId)
                {
                    if (!listItemEntry.separatorLine)
                    {
                        _ports.TextGreyedOutput(!listItemEntry.enabled);
                        _ports.MoveTo((short)_menuRect.Left, topPos);
                        _text16.DrawString(listItemEntry.textSplit);
                        _ports.MoveTo((short)(_menuRect.Right - listItemEntry.textRightAlignedWidth - 5), topPos);
                        _text16.DrawString(listItemEntry.textRightAligned);
                    }
                    else {
                        // We dont 100% follow sierra here, we draw the line from left to right. Looks better
                        // BTW. SCI1.1 seems to put 2 pixels and then skip one, we don't do this at all (lsl6)
                        pixelPos.Y = topPos + (_ports._curPort.fontHeight >> 1) - 1;
                        pixelPos.X = _menuRect.Left - 7;
                        while (pixelPos.X < (_menuRect.Right - 1))
                        {
                            _screen.PutPixel((short)pixelPos.X, (short)pixelPos.Y, GfxScreenMasks.VISUAL, 0, 0, 0);
                            pixelPos.X += 2;
                        }
                    }
                    topPos += _ports._curPort.fontHeight;
                }
            }
            _ports.TextGreyedOutput(false);

            // Draw the black line again
            _paint16.FillRect(_ports._menuLine, GfxScreenMasks.VISUAL, 0);

            _menuRect.Left -= 8;
            _menuRect.Left--; _menuRect.Right++; _menuRect.Bottom++;
            _paint16.BitsShow(_menuRect);
        }

        private void CalculateMenuAndItemWidth()
        {
            GuiMenuItemEntry itemEntry;
            short dummyHeight;

            CalculateMenuWidth();

            for (int i = 0; i < _itemList.Count; i++)
            {
                itemEntry = _itemList[i];
                // Split the text now for multilingual SCI01 games
                itemEntry.textSplit = SciEngine.Instance.StrSplit(itemEntry.text, null);
                _text16.StringWidth(itemEntry.textSplit, 0, out itemEntry.textWidth, out dummyHeight);
                _text16.StringWidth(itemEntry.textRightAligned, 0, out itemEntry.textRightAlignedWidth, out dummyHeight);
            }
        }

        private GuiMenuItemEntry InteractiveGetItem(ushort menuId, ushort itemId, bool menuChanged)
        {
            GuiMenuItemEntry itemEntry;
            GuiMenuItemEntry firstItemEntry = null;
            GuiMenuItemEntry lastItemEntry = null;

            // Fixup menuId if needed
            if (menuId > _list.Count)
                menuId = 1;
            if (menuId == 0)
                menuId = (ushort)_list.Count;
            for (int i = 0; i < _itemList.Count; i++)
            {
                itemEntry = _itemList[i];
                if (itemEntry.menuId == menuId)
                {
                    if (itemEntry.id == itemId)
                        return itemEntry;
                    if (firstItemEntry == null)
                        firstItemEntry = itemEntry;
                    if ((lastItemEntry == null) || (itemEntry.id > lastItemEntry.id))
                        lastItemEntry = itemEntry;
                }
            }
            if ((itemId == 0) || (menuChanged))
                return lastItemEntry;
            return firstItemEntry;
        }

        private void InteractiveStart(bool pauseSound)
        {
            _mouseOldState = _cursor.IsVisible;
            _cursor.KernelShow();
            if (pauseSound)
                SciEngine.Instance._soundCmd.PauseAll(true);
        }

        private void InteractiveEnd(bool pauseSound)
        {
            if (pauseSound)
                SciEngine.Instance._soundCmd.PauseAll(false);
            if (!_mouseOldState)
                _cursor.KernelHide();
        }

        public void Reset()
        {
            _list.Clear();
            _itemList.Clear();

            // We actually set active item in here and remember last selection of the
            // user. Sierra SCI always defaulted to first item every time menu was
            // called via ESC, we don't follow that logic.
            _curMenuId = 1;
            _curItemId = 1;
        }

        public void KernelAddEntry(string title, string content, Register contentVmPtr)
        {
            GuiMenuEntry menuEntry;
            ushort itemCount = 0;
            GuiMenuItemEntry itemEntry;
            int contentSize = content.Length;
            int separatorCount;
            int curPos, beginPos, endPos, tempPos;
            int tagPos, rightAlignedPos, functionPos, altPos, controlPos;
            int tempPtr;

            // Sierra SCI starts with id 1, so we do so as well
            menuEntry = new GuiMenuEntry((ushort)(_list.Count + 1));
            menuEntry.text = title;
            _list.Add(menuEntry);

            curPos = 0;
            ushort listSize = (ushort)_list.Count;

            do
            {
                itemCount++;
                itemEntry = new GuiMenuItemEntry(listSize, itemCount);

                beginPos = curPos;

                // Now go through the content till we find end-marker and collect data about it.
                // ':' is an end-marker for each item.
                tagPos = 0; rightAlignedPos = 0;
                controlPos = 0; altPos = 0; functionPos = 0;
                while ((curPos < contentSize) && (content[curPos] != ':'))
                {
                    switch (content[curPos])
                    {
                        case '=': // Set tag
                                  // Special case for normal animation speed - they use right
                                  // aligned "=" for that one, so we ignore it as being recognized
                                  // as tag marker.
                            if (rightAlignedPos == curPos - 1)
                                break;
                            if (tagPos != 0)
                                throw new InvalidOperationException("multiple tag markers within one menu-item");
                            tagPos = curPos;
                            break;
                        case '`': // Right-aligned
                            if (rightAlignedPos != 0)
                                throw new InvalidOperationException("multiple right-aligned markers within one menu-item");
                            rightAlignedPos = curPos;
                            break;
                        case '^': // Ctrl-prefix
                            if (controlPos != 0)
                                throw new InvalidOperationException("multiple control markers within one menu-item");
                            controlPos = curPos;
                            break;
                        case '@': // Alt-prefix
                            if (altPos != 0)
                                throw new InvalidOperationException("multiple alt markers within one menu-item");
                            altPos = curPos;
                            break;
                        case '#': // Function-prefix
                            if (functionPos != 0)
                                throw new InvalidOperationException("multiple function markers within one menu-item");
                            functionPos = curPos;
                            break;
                    }
                    curPos++;
                }
                endPos = curPos;

                // Control/Alt/Function key mapping...
                if (controlPos != 0)
                {
                    content = Replace(content, controlPos, SCI_MENU_REPLACE_ONCONTROL);
                    itemEntry.keyModifier = SciEvent.SCI_KEYMOD_CTRL;
                    tempPos = controlPos + 1;
                    if (tempPos >= contentSize)
                        throw new InvalidOperationException("control marker at end of item");
                    itemEntry.keyPress = char.ToLower(content[tempPos]);
                    content = Replace(content, tempPos, char.ToUpper(content[tempPos]));
                }
                if (altPos != 0)
                {
                    content = Replace(content, altPos, SCI_MENU_REPLACE_ONALT);
                    itemEntry.keyModifier = SciEvent.SCI_KEYMOD_ALT;
                    tempPos = altPos + 1;
                    if (tempPos >= contentSize)
                        throw new InvalidOperationException("alt marker at end of item");
                    itemEntry.keyPress = char.ToLower(content[tempPos]);
                    content = Replace(content, tempPos, char.ToUpper(content[tempPos]));
                }
                if (functionPos != 0)
                {
                    content = Replace(content, functionPos, SCI_MENU_REPLACE_ONFUNCTION);
                    tempPos = functionPos + 1;
                    if (tempPos >= contentSize)
                        throw new InvalidOperationException("function marker at end of item");
                    itemEntry.keyPress = content[tempPos];
                    switch (content[functionPos + 1])
                    {
                        case '1': itemEntry.keyPress = SciEvent.SCI_KEY_F1; break;
                        case '2': itemEntry.keyPress = SciEvent.SCI_KEY_F2; break;
                        case '3': itemEntry.keyPress = SciEvent.SCI_KEY_F3; break;
                        case '4': itemEntry.keyPress = SciEvent.SCI_KEY_F4; break;
                        case '5': itemEntry.keyPress = SciEvent.SCI_KEY_F5; break;
                        case '6': itemEntry.keyPress = SciEvent.SCI_KEY_F6; break;
                        case '7': itemEntry.keyPress = SciEvent.SCI_KEY_F7; break;
                        case '8': itemEntry.keyPress = SciEvent.SCI_KEY_F8; break;
                        case '9': itemEntry.keyPress = SciEvent.SCI_KEY_F9; break;
                        case '0': itemEntry.keyPress = SciEvent.SCI_KEY_F10; break;
                        default:
                            throw new InvalidOperationException("illegal function key specified");
                    }
                }

                // Now get all strings
                tempPos = endPos;
                if (rightAlignedPos != 0)
                {
                    tempPos = rightAlignedPos;
                }
                else if (tagPos != 0)
                {
                    tempPos = tagPos;
                }
                curPos = beginPos;
                separatorCount = 0;
                while (curPos < tempPos)
                {
                    switch (content[curPos])
                    {
                        case '!':
                        case '-':
                        case ' ':
                            separatorCount++;
                            break;
                        case '%':
                            // Some multilingual sci01 games use e.g. '--!%G--!' (which doesn't really make sense)
                            separatorCount += 2;
                            curPos++;
                            break;
                    }
                    curPos++;
                }
                if (separatorCount == tempPos - beginPos)
                {
                    itemEntry.separatorLine = true;
                }
                else {
                    // We don't strSplit here, because multilingual SCI01 support
                    // language switching on the fly, so we have to do this everytime
                    // the menu is called.
                    itemEntry.text = content.Substring(beginPos, tempPos - beginPos);

                    // LSL6 uses "Ctrl-" prefix string instead of ^ like all the other games do
                    tempPtr = itemEntry.text.IndexOf("Ctrl-");
                    if (tempPtr != -1)
                    {
                        itemEntry.keyModifier = SciEvent.SCI_KEYMOD_CTRL;
                        itemEntry.keyPress = char.ToLower(itemEntry.text[tempPtr + 5]);
                    }
                }
                itemEntry.textVmPtr = contentVmPtr;
                itemEntry.textVmPtr.IncOffset((short)beginPos);

                if (rightAlignedPos != 0)
                {
                    rightAlignedPos++;
                    tempPos = endPos;
                    // some games have tagPos in front of right rightAlignedPos
                    // some have it the other way... (qfg1ega)
                    if (tagPos != 0 && tagPos >= rightAlignedPos)
                        tempPos = tagPos;
                    itemEntry.textRightAligned = content.Substring(rightAlignedPos, tempPos - rightAlignedPos);
                    // Remove ending space, if there is one. Strangely sometimes there
                    // are lone spaces at the end in some games
                    if (itemEntry.textRightAligned.EndsWith(" "))
                        itemEntry.textRightAligned.Remove(itemEntry.textRightAligned.Length - 1, 1);
                    // - and + are used sometimes for volume control/animation speed,
                    // = sometimes for animation speed
                    if (itemEntry.textRightAligned == "-")
                    {
                        itemEntry.keyPress = '-';
                    }
                    else if (itemEntry.textRightAligned == "+")
                    {
                        itemEntry.keyPress = '+';
                    }
                    else if (itemEntry.textRightAligned == "=")
                    {
                        itemEntry.keyPress = '=';
                    }
                }

                if (tagPos != 0)
                {
                    tempPos = functionPos + 1;
                    if (tempPos >= contentSize)
                        throw new InvalidOperationException("tag marker at end of item");
                    int i;
                    for (i = tempPos; i < content.Length; i++)
                    {
                        if (!char.IsDigit(content[i]))
                            break;
                    }
                    itemEntry.tag = ushort.Parse(content.Substring(tempPos, i - tempPos));
                }

                curPos = endPos + 1;

                _itemList.Add(itemEntry);
            } while (curPos < contentSize);
        }

        private static string Replace(string content, int pos, char text)
        {
            return content.Remove(pos, 1).Insert(pos, text.ToString());
        }

        public void KernelSetAttribute(ushort menuId, ushort itemId, MenuAttribute attributeId, Register value)
        {
            GuiMenuItemEntry itemEntry = FindItem(menuId, itemId);

            if (itemEntry == null)
            {
                // PQ2 demo calls this, for example, but has no menus (bug report #3034507). Some SCI
                // fan games (Al Pond 2, Aquarius) call this too on non-existent menu items. The
                // original interpreter ignored these as well.
                // TODO: debugC(kDebugLevelGraphics, "Tried to setAttribute() on non-existent menu-item %d:%d", menuId, itemId);
                return;
            }

            switch (attributeId)
            {
                case MenuAttribute.ENABLED:
                    itemEntry.enabled = !value.IsNull;
                    break;
                case MenuAttribute.SAID:
                    itemEntry.saidVmPtr = value;
                    break;
                case MenuAttribute.TEXT:
                    itemEntry.text = _segMan.GetString(value);
                    itemEntry.textVmPtr = value;
                    // We assume here that no script ever creates a separatorLine dynamically
                    break;
                case MenuAttribute.KEYPRESS:
                    itemEntry.keyPress = char.ToLower((char)value.Offset);
                    itemEntry.keyModifier = 0;
                    // TODO: Find out how modifier is handled
                    // TODO: debug("setAttr keypress %X %X", value.Segment, value.Offset);
                    break;
                case MenuAttribute.TAG:
                    itemEntry.tag = (ushort)value.Offset;
                    break;
                default:
                    // Happens when loading a game in LSL3 - attribute 1A
                    // TODO: warning("setAttribute() called with unsupported attributeId %X", attributeId);
                    break;
            }
        }

        private GuiMenuItemEntry FindItem(ushort menuId, ushort itemId)
        {
            return _itemList.FirstOrDefault(m => m.menuId == menuId && m.id == itemId);
        }

        public void KernelDrawMenuBar(bool clear)
        {
            if (!clear)
            {
                Port oldPort = _ports.SetPort(_ports._menuPort);
                CalculateMenuWidth();
                DrawBar();
                _paint16.BitsShow(_ports._menuBarRect);
                _ports.SetPort(oldPort);
            }
            else {
                KernelDrawStatus("", 0, 0);
            }
        }

        public void KernelDrawStatus(string text, short colorPen, short colorBack)
        {
            Port oldPort = _ports.SetPort(_ports._menuPort);

            _paint16.FillRect(_ports._menuBarRect, GfxScreenMasks.VISUAL, (byte)colorBack);
            _ports.PenColor(colorPen);
            _ports.MoveTo(0, 1);
            _text16.DrawStatus(text);
            _paint16.BitsShow(_ports._menuBarRect);
            // Also draw the line under the status bar. Normally, this is never drawn,
            // but we need it to be drawn because Dr. Brain 1 Mac draws over it when
            // it displays the icon bar. SSCI used negative rectangles to erase the
            // area after drawing the icon bar, but this is a much cleaner way of
            // achieving the same effect.
            _paint16.FillRect(_ports._menuLine, GfxScreenMasks.VISUAL, 0);
            _paint16.BitsShow(_ports._menuLine);
            _ports.SetPort(oldPort);
        }

        private void DrawBar()
        {
            // Hardcoded black on white and a black line afterwards
            _paint16.FillRect(_ports._menuBarRect, GfxScreenMasks.VISUAL, _screen.ColorWhite);
            _paint16.FillRect(_ports._menuLine, GfxScreenMasks.VISUAL, 0);
            _ports.PenColor(0);
            _ports.MoveTo(8, 1);

            for (int i = 0; i < _list.Count; i++)
            {
                var listEntry = _list[i];
                _text16.DrawString(listEntry.textSplit);
            }
        }

        /// <summary>
        /// This helper calculates all text widths for all menus (only)
        /// </summary>
        private void CalculateMenuWidth()
        {
            short dummyHeight;

            for (int i = 0; i < _list.Count; i++)
            {
                var menuEntry = _list[i];
                menuEntry.textSplit = SciEngine.Instance.StrSplit(menuEntry.text, null);
                _text16.StringWidth(menuEntry.textSplit, 0, out menuEntry.textWidth, out dummyHeight);
            }
        }

        public Register KernelGetAttribute(ushort menuId, ushort itemId, MenuAttribute attributeId)
        {
            GuiMenuItemEntry itemEntry = FindItem(menuId, itemId);
            if (itemEntry == null)
                throw new InvalidOperationException($"Tried to getAttribute() on non-existent menu-item {menuId}:{itemId}");
            switch (attributeId)
            {
                case MenuAttribute.ENABLED:
                    if (itemEntry.enabled)
                        return Register.Make(0, 1);
                    break;
                case MenuAttribute.SAID:
                    return itemEntry.saidVmPtr;
                case MenuAttribute.TEXT:
                    return itemEntry.textVmPtr;
                case MenuAttribute.KEYPRESS:
                    // TODO: Find out how modifier is handled
                    return Register.Make(0, itemEntry.keyPress);
                case MenuAttribute.TAG:
                    return Register.Make(0, itemEntry.tag);
                default:
                    throw new InvalidOperationException($"getAttribute() called with unsupported attributeId {attributeId:X}");
            }
            return Register.NULL_REG;
        }
    }
}
