//
//  ScummEngine_Room.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using NScumm.Core.Graphics;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        byte _currentRoom;
        protected Room roomData;

        internal Room CurrentRoomData
        {
            get { return roomData; }
        }

        internal byte CurrentRoom
        {
            get { return _currentRoom; }
        }

        protected string GetIqFilename(string filename)
        {
            var targetName = ServiceLocator.FileStorage.Combine(ServiceLocator.FileStorage.GetDirectoryName(Game.Path), Game.Id);
            if (_game.GameId == NScumm.Core.IO.GameId.Indy4)
            {
                filename = targetName + ".iq";
            }
            else if (_game.GameId == NScumm.Core.IO.GameId.Monkey1 || _game.GameId == NScumm.Core.IO.GameId.Monkey2)
            {
                filename = targetName + ".cfg";
            }
            else
            {
                throw new NotSupportedException(string.Format("SO_SAVE_STRING: Unsupported filename {0}", filename));
            }
            return filename;
        }

        void ResetRoomObjects()
        {
            int j = 1;
            for (int i = 0; i < roomData.Objects.Count; i++)
            {
                for (; j < _objs.Length; j++)
                {
                    if (_objs[j].FloatingObjectIndex == 0)
                        break;
                }
                _objs[j] = roomData.Objects[i];
                j++;
            }
            for (int i = j; i < _objs.Length; i++)
            {
                if (_objs[i].FloatingObjectIndex == 0)
                {
                    _objs[i] = new ObjectData();
                }
            }
        }

        void ClearRoomObjects()
        {
            if (Game.Version < 5)
            {
                for (var i = 0; i < _objs.Length; i++)
                {
                    _objs[i] = new ObjectData();
                }
            }
            else
            {
                for (var i = 0; i < _objs.Length; i++)
                {
                    if (_objs[i].Number < 1)    // Optimise codepath
                                continue;

                    // Nuke all non-flObjects (flObjects are nuked in script.cpp)
                    if (_objs[i].FloatingObjectIndex == 0)
                    {
                        _objs[i] = new ObjectData();
                    }
                    else
                    {
                        // Nuke all unlocked flObjects
                        if (!_objs[i].IsLocked)
                        {
//                            _res->nukeResource(rtFlObject, _objs[i].fl_object_index);
                            _objs[i] = new ObjectData();
                        }
                    }
                }
            }
        }

        void ResetRoomSubBlocks()
        {
            // Reset room color for V1 zak
            if (Game.Version <= 1)
                Gdi.RoomPalette[0] = 0;

            _boxMatrix.Clear();
            _boxMatrix.AddRange(roomData.BoxMatrix);

            for (int i = 0; i < _scaleSlots.Length; i++)
            {
                _scaleSlots[i] = new ScaleSlot();
            }

            for (int i = 1; i <= roomData.Scales.Length; i++)
            {
                var scale = roomData.Scales[i - 1];
                if (Game.Version == 8 || scale.Scale1 != 0 || scale.Y1 != 0 || scale.Scale2 != 0 || scale.Y2 != 0)
                {
                    SetScaleSlot(i, 0, scale.Y1, scale.Scale1, 0, scale.Y2, scale.Scale2);
                }
            }

            Array.Clear(_extraBoxFlags, 0, _extraBoxFlags.Length);

            _boxes = new Box[roomData.Boxes.Count];
            for (int i = 0; i < roomData.Boxes.Count; i++)
            {
                var box = roomData.Boxes[i];
                _boxes[i] = new Box
                {
                    Flags = box.Flags,
                    Llx = box.Llx,
                    Lly = box.Lly,
                    Lrx = box.Lrx,
                    Lry = box.Lry,
                    Mask = box.Mask,
                    Scale = box.Scale,
                    ScaleSlot = box.ScaleSlot,
                    Ulx = box.Ulx,
                    Uly = box.Uly,
                    Urx = box.Urx,
                    Ury = box.Ury
                };
            }

            Array.Copy(roomData.ColorCycle, _colorCycle, roomData.ColorCycle.Length);

            Gdi.RoomChanged(CurrentRoomData);
        }
    }
}

