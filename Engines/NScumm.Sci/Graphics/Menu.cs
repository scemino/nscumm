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

namespace NScumm.Sci.Graphics
{
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

    struct GuiMenuItemEntry
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
            : this()
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
    }
}
