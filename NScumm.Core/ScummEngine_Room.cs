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
                _objs[j].Position = roomData.Objects[i].Position;
                _objs[j].Width = roomData.Objects[i].Width;
                _objs[j].Walk = roomData.Objects[i].Walk;
                _objs[j].State = roomData.Objects[i].State;
                _objs[j].Parent = roomData.Objects[i].Parent;
                _objs[j].ParentState = roomData.Objects[i].ParentState;
                _objs[j].Number = roomData.Objects[i].Number;
                _objs[j].Height = roomData.Objects[i].Height;
                // HACK: This is done since an angle doesn't fit into a byte (360 > 256)
                _objs[j].ActorDir = Game.Version == 8 ? (byte)ScummMath.ToSimpleDir(true, roomData.Objects[i].ActorDir) : roomData.Objects[i].ActorDir;
                _objs[j].Flags = Game.Version == 8 ? ((((int)roomData.Objects[i].Flags & 16) != 0) ? DrawBitmaps.AllowMaskOr : 0) : roomData.Objects[i].Flags;
                _objs[j].Script.Offset = roomData.Objects[i].Script.Offset;
                _objs[j].Script.Data = roomData.Objects[i].Script.Data;
                _objs[j].ScriptOffsets.Clear();
                foreach (var scriptOffset in roomData.Objects[i].ScriptOffsets)
                {
                    _objs[j].ScriptOffsets.Add(scriptOffset.Key, scriptOffset.Value);
                }
                _objs[j].Name = roomData.Objects[i].Name;
                _objs[j].Images.Clear();
                _objs[j].Images.AddRange(roomData.Objects[i].Images);
                _objs[j].Hotspots.Clear();
                _objs[j].Hotspots.AddRange(roomData.Objects[i].Hotspots);
                _objs[j].IsLocked = _objs[i].IsLocked;
                j++;
            }
            for (int i = j; i < _objs.Length; i++)
            {
                if (_objs[i].FloatingObjectIndex == 0)
                {
                    _objs[i].Number = 0;
                    _objs[i].Script.Offset = 0;
                    _objs[i].ScriptOffsets.Clear();
                    _objs[i].Script.Data = new byte[0];
                }
            }
        }

        void ClearRoomObjects()
        {
            if (Game.Version < 5)
            {
                for (var i = 0; i < _objs.Length; i++)
                {
                    _objs[i].Number = 0;
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
                        _objs[i].Number = 0;
                    }
                    else
                    {
                        // Nuke all unlocked flObjects
                        if (!_objs[i].IsLocked)
                        {
//                            _res->nukeResource(rtFlObject, _objs[i].fl_object_index);
                            _objs[i].Number = 0;
                            _objs[i].FloatingObjectIndex = 0;
                        }
                    }
                }
            }
        }

        void ResetRoomSubBlocks()
        {
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

            Gdi.RoomChanged();
        }
    }
}

