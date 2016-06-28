//
//  QueenEngine.cs
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
using System.Diagnostics;
using System.IO;
using NScumm.Core;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    class ZoneSlot
    {
        public bool valid;
        public Box box;
    }

    public enum GridScreen
    {
        ROOM = 0,
        PANEL = 1,
        COUNT = 2
    }

    public class Grid
    {
        const int MAX_ZONES_NUMBER = 32;
        const int MAX_AREAS_NUMBER = 11;

        private static readonly Verb[] pv = {
            Verb.NONE,
            Verb.OPEN,
            Verb.CLOSE,
            Verb.MOVE,
            Verb.GIVE,
            Verb.LOOK_AT,
            Verb.PICK_UP,
            Verb.TALK_TO,
            Verb.USE,
            Verb.SCROLL_UP,
            Verb.SCROLL_DOWN,
            Verb.INV_1,
            Verb.INV_2,
            Verb.INV_3,
            Verb.INV_4,
        };

        QueenEngine _vm;
        ushort _numRoomAreas;
        short[] _objMax;
        short[] _areaMax;
        Area[][] _area;

        Box[] _objectBox;
        ZoneSlot[,] _zones;

        public Area[][] Areas
        {
            get { return _area; }
        }

        public short[] AreaMax
        {
            get { return _areaMax; }
        }

        public short[] ObjMax
        {
            get { return _objMax; }
        }

        public Grid(QueenEngine vm)
        {
            _vm = vm;
            _zones = new ZoneSlot[(int)GridScreen.COUNT, MAX_ZONES_NUMBER];
            for (int i = 0; i < (int)GridScreen.COUNT; i++)
            {
                for (int j = 0; j < MAX_ZONES_NUMBER; j++)
                {
                    _zones[i, j] = new ZoneSlot();
                }
            }
        }

        public Box Zone(GridScreen screen, ushort index)
        {
            var zone = _zones[(int)screen, index];
            Debug.Assert(zone.valid);
            return zone.box;
        }

        public ushort FindAreaForPos(GridScreen screen, ushort x, ushort y)
        {
            ushort room = _vm.Logic.CurrentRoom;
            ushort zoneNum = FindZoneForPos(screen, x, y);
            if (zoneNum <= _objMax[room])
            {
                zoneNum = 0;
            }
            else
            {
                zoneNum = (ushort)(zoneNum - _objMax[room]);
            }
            return zoneNum;
        }

        public ushort FindScale(ushort x, ushort y)
        {
            ushort room = _vm.Logic.CurrentRoom;
            ushort scale = 100;
            ushort areaNum = FindAreaForPos(GridScreen.ROOM, x, y);
            if (areaNum != 0)
            {
                scale = _area[room][areaNum].CalcScale((short)y);
            }
            return scale;
        }

        public ushort FindZoneForPos(GridScreen screen, ushort x, ushort y)
        {
            D.Debug(9, $"Logic::findZoneForPos({screen}, ({x},{y}))");
            int i;
            if (screen == GridScreen.PANEL)
            {
                y -= Defines.ROOM_ZONE_HEIGHT;
            }
            for (i = 1; i < MAX_ZONES_NUMBER; ++i)
            {
                ZoneSlot pzs = _zones[(int)screen, i];
                if (pzs.valid && pzs.box.Contains((short)x, (short)y))
                {
                    return (ushort)i;
                }
            }
            return 0;
        }

        public void SetupNewRoom(ushort room, ushort firstRoomObjNum)
        {
            D.Debug(9, "Grid::setupNewRoom()");
            Clear(GridScreen.ROOM);

            // setup objects zones
            var maxObjRoom = _objMax[room];
            short zoneNum = 1;
            for (var i = firstRoomObjNum + 1; i <= firstRoomObjNum + maxObjRoom; ++i)
            {
                if (_vm.Logic.ObjectData[i].name != 0)
                {
                    if (room == 41 && i == 303)
                    {

                        // WORKAROUND bug #1599009: In the room 41, the bounding box of the
                        // stairs (object 303) doesn't match with the room picture. With the
                        // original box dimensions, Joe could walk "above" the stairs, giving
                        // the impression of floating in the air.
                        // To fix this, the bounding box is set relative to the position of
                        // the cabinet (object 295).

                        short y1 = (short)(_objectBox[295].y2 + 1);
                        SetZone(GridScreen.ROOM, zoneNum, _objectBox[i].x1, y1, _objectBox[i].x2, _objectBox[i].y2);
                    }
                    else
                    {
                        SetZone(GridScreen.ROOM, zoneNum, _objectBox[i]);
                    }
                }
                ++zoneNum;
            }

            // setup room zones (areas)
            short maxAreaRoom = _areaMax[room];
            for (zoneNum = 1; zoneNum <= maxAreaRoom; ++zoneNum)
            {
                SetZone(GridScreen.ROOM, (short)(maxObjRoom + zoneNum), _area[room][zoneNum].box);
            }
        }

        public void Clear(GridScreen screen)
        {
            D.Debug(9, $"Grid::clear({screen})");
            for (int i = 1; i < MAX_ZONES_NUMBER; ++i)
            {
                _zones[(int)screen, i].valid = false;
            }
        }

        public void SetupPanel()
        {
            for (int i = 0; i <= 7; ++i)
            {
                var x = i * 20;
                SetZone(GridScreen.PANEL, (short)(i + 1), (short)x, 10, (short)(x + 19), 49);
            }

            // inventory scrolls
            SetZone(GridScreen.PANEL, 9, 160, 10, 179, 29);
            SetZone(GridScreen.PANEL, 10, 160, 30, 179, 49);

            // inventory items
            SetZone(GridScreen.PANEL, 11, 180, 10, 213, 49);
            SetZone(GridScreen.PANEL, 12, 214, 10, 249, 49);
            SetZone(GridScreen.PANEL, 13, 250, 10, 284, 49);
            SetZone(GridScreen.PANEL, 14, 285, 10, 320, 49);
        }

        public void ReadDataFrom(ushort numObjects, ushort numRooms, byte[] data, ref int ptr)
        {
            ushort i, j;

            _numRoomAreas = numRooms;

            _objMax = new short[_numRoomAreas + 1];
            _areaMax = new short[_numRoomAreas + 1];
            _area = new Area[_numRoomAreas + 1][];

            for (i = 1; i <= _numRoomAreas; i++)
            {
                _area[i] = new Area[MAX_AREAS_NUMBER];
                _objMax[i] = data.ToInt16BigEndian(ptr); ptr += 2;
                _areaMax[i] = data.ToInt16BigEndian(ptr); ptr += 2;
                for (j = 1; j <= _areaMax[i]; j++)
                {
                    Debug.Assert(j < MAX_AREAS_NUMBER);
                    _area[i][j] = new Area();
                    _area[i][j].ReadFromBE(data, ref ptr);
                }
            }

            _objectBox = new Box[numObjects + 1];
            for (i = 1; i <= numObjects; i++)
            {
                _objectBox[i] = new Box();
                _objectBox[i].ReadFromBE(data, ref ptr);
            }
        }

        public void SetZone(GridScreen screen, short zoneNum, short x1, short y1, short x2, short y2)
        {
            D.Debug(9, $"Grid::setZone({screen}, {zoneNum}, ({x1},{y1}), ({x2},{y2}))");
            Debug.Assert(zoneNum < MAX_ZONES_NUMBER);
            var pzs = _zones[(int)screen, zoneNum] = new ZoneSlot();
            pzs.valid = true;
            pzs.box = new Box(x1, y1, x2, y2);
        }

        private void SetZone(GridScreen screen, short zoneNum, Box box)
        {
            D.Debug(9, $"Grid::setZone({screen}, {zoneNum}, ({box.x1},{box.y1}), ({box.x2},{box.y2}))");
            Debug.Assert(zoneNum < MAX_ZONES_NUMBER);
            var pzs = _zones[(int)screen, zoneNum];
            pzs.valid = true;
            pzs.box = box;
        }

        public ushort FindObjectUnderCursor(short cursorx, short cursory)
        {
            ushort roomObj = 0;
            if (cursory < Defines.ROOM_ZONE_HEIGHT)
            {
                short x = (short)(cursorx + _vm.Display.HorizontalScroll);
                roomObj = FindZoneForPos(GridScreen.ROOM, (ushort)x, (ushort)cursory);
            }
            return roomObj;
        }

        public ushort FindObjectNumber(ushort zoneNum)
        {
            // l.316-327 select.c
            ushort room = _vm.Logic.CurrentRoom;
            ushort obj = zoneNum;
            ushort objectMax = (ushort)_objMax[room];
            D.Debug(9, $"Grid::findObjectNumber({zoneNum:X}, {objectMax:X})");
            if (zoneNum > objectMax)
            {
                // this is an area box, check for associated object
                obj = _area[room][zoneNum - objectMax].@object;
                if (obj != 0)
                {
                    // there is an object, get its number
                    obj -= _vm.Logic.CurrentRoomData;
                }
            }
            return obj;
        }

        public Verb FindVerbUnderCursor(short cursorx, short cursory)
        {
            return pv[FindZoneForPos(GridScreen.PANEL, (ushort)cursorx, (ushort)cursory)];
        }

        public void LoadState(uint version, byte[] data, ref int ptr)
        {
            for (var i = 1; i <= _numRoomAreas; ++i)
            {
                for (var j = 1; j <= _areaMax[i]; ++j)
                {
                    _area[i][j].ReadFromBE(data, ref ptr);
                }
            }
        }

        public void SaveState(byte[] data, ref int ptr)
        {
            short i, j;
            for (i = 1; i <= _numRoomAreas; ++i)
            {
                for (j = 1; j <= _areaMax[i]; ++j)
                {
                    _area[i][j].WriteToBE(data, ref ptr);
                }
            }
        }
    }
}

