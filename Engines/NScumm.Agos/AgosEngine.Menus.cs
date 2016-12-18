//
//  AgosEngine.Menus.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private byte[] _menuBase;

        private void LoadMenuFile()
        {
            var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_MENUFILE));
            if (@in == null)
            {
                Error("loadMenuFile: Can't load menus file '{0}'", GetFileName(GameFileTypes.GAME_MENUFILE));
            }

            int fileSize = (int) @in.Length;
            _menuBase = new byte[fileSize];
            @in.Read(_menuBase, 0, fileSize);
            @in.Dispose();
        }

        private void LightMenuStrip(int a)
        {
            MouseOff();
            UnlightMenuStrip();

            for (int i = 0; i != 10; i++)
            {
                if ((a & (1 << i)) != 0)
                {
                    EnableBox(120 + i);
                    LightMenuBox(120 + i);
                }
            }

            MouseOn();
        }

        private void LightMenuBox(int hitarea)
        {
            var ha = FindBox(hitarea);
            BytePtr src;
            int w, h, i;

            MouseOff();

            LockScreen(screen =>
            {
                src = screen.GetBasePtr(ha.x, ha.y);
                w = ha.width;
                h = ha.height;

                do
                {
                    for (i = 0; i != w; ++i)
                    {
                        if (src[i] == 14)
                            src[i] = 15;
                    }
                    src += screen.Pitch;
                } while (--h != 0);
            });

            MouseOn();
        }

        private void UnlightMenuStrip()
        {
            MouseOff();

            LockScreen(screen =>
            {
                var src = screen.GetBasePtr(272, 8);
                var w = 48;
                var h = 82;

                do
                {
                    for (var i = 0; i != w; ++i)
                    {
                        if (src[i] != 0)
                            src[i] = 14;
                    }
                    src += screen.Pitch;
                } while (--h != 0);

                for (var i = 120; i != 130; i++)
                    DisableBox(i);
            });

            MouseOn();
        }

        // Elvira 2 specific
        private uint menuFor_e2(Item item)
        {
            if (item == null || item == _dummyItem2 || item == _dummyItem3)
                return 0xFFFF;

            var subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFMenu))
            {
                int offs = GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFMenu);
                return (uint) subObject.objectFlagValue[offs];
            }

            return _agosMenu;
        }

        // Waxworks specific
        private uint menuFor_ww(Item item, uint id)
        {
            if (id != 0xFFFF && id < 10 && _textMenu[id] != 0)
                return _textMenu[id];

            if (item == null || item == _dummyItem2 || item == _dummyItem3)
                return _agosMenu;

            var subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFMenu))
            {
                int offs = GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFMenu);
                return (uint) subObject.objectFlagValue[offs];
            }

            return _agosMenu;
        }

        private void ClearMenuStrip()
        {
            for (var i = 111; i != 115; i++)
                DisableBox(i);

            if (GameType == SIMONGameType.GType_WW)
            {
                SetWindowImageEx(2, 101);
            }
            else
            {
                SetWindowImageEx(2, 102);
            }
        }

        // Elvira 2 and Waxworks specific
        private void DoMenuStrip(uint menuNum)
        {
            uint var = (uint) ((GameType == SIMONGameType.GType_WW) ? 11 : 1);

            for (var i = 111; i != 115; i++)
                DisableBox(i);

            for (var i = var; i != (var + 5); i++)
                _variableArray[i] = 0;

            BytePtr srcPtr = _menuBase;
            while (menuNum-- != 0)
            {
                while (srcPtr.ToUInt16BigEndian() != 0)
                    srcPtr += 2;
                srcPtr += 2;
            }

            uint id = 111;
            uint v = var;

            while (srcPtr.ToUInt16BigEndian() != 0)
            {
                uint verb = srcPtr.ToUInt16BigEndian();
                _variableArray[v] = (short) verb;

                HitArea ha = FindBox((int) id);
                if (ha != null)
                {
                    ha.flags &= ~BoxFlags.kBFBoxDead;
                    ha.verb = (ushort) verb;
                }

                id++;
                srcPtr += 2;
                v++;
            }

            _variableArray[var + 4] = (short) (id - 111);
            if (GameType == SIMONGameType.GType_WW)
            {
                SetWindowImageEx(2, 102);
            }
            else
            {
                SetWindowImageEx(2, 103);
            }
        }
    }
}