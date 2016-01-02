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

    struct GuiMenuEntry
    {
        public ushort id;
        public string text;
        public string textSplit;
        public short textWidth;

        public GuiMenuEntry(ushort curId)
            : this()
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

        private EventManager _eventMan;
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
            _eventMan = eventMan;
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
    }
}
