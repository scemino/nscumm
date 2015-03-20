﻿//
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

        void LoadRoom()
        {
            var room = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);

            // For small header games, we only call startScene if the room
            // actually changed. This avoid unwanted (wrong) fades in Zak256
            // and others. OTOH, it seems to cause a problem in newer games.
            if ((_game.Version >= 5) || room != _currentRoom)
            {
                StartScene(room);
            }
            _fullRedraw = true;
        }

        void LoadRoomWithEgo()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            int room = GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = Actors[_variables[VariableEgo.Value]];

            a.PutActor((byte)room);
            int oldDir = a.Facing;
            _egoPositioned = false;

            var x = ReadWordSigned();
            var y = ReadWordSigned();

            _variables[VariableWalkToObject.Value] = obj;
            StartScene(a.Room, a, obj);
            _variables[VariableWalkToObject.Value] = 0;

            if (Game.Version <= 4)
            {
                if (!_egoPositioned)
                {
                    int dir;
                    Point p;
                    GetObjectXYPos(obj, out p, out dir);
                    a.PutActor(p, _currentRoom);
                    if (a.Facing == oldDir)
                        a.SetDirection(dir + 180);
                }
                a.Moving = 0;
            }

            // This is based on disassembly
            _camera.CurrentPosition.X = _camera.DestinationPosition.X = a.Position.X;
            SetCameraFollows(a, false);

            _fullRedraw = true;

            if (x != -1)
            {
                a.StartWalk(new Point(x, y), -1);
            }
        }

        protected string GetIqFilename(string filename)
        {
            var targetName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Game.Path), Game.Id);
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

        void RoomOps()
        {
            bool paramsBeforeOpcode = (_game.Version == 3);
            int a = 0;
            int b = 0;
            if (paramsBeforeOpcode)
            {
                a = GetVarOrDirectWord(OpCodeParameter.Param1);
                b = GetVarOrDirectWord(OpCodeParameter.Param2);
            }

            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:     // SO_ROOM_SCROLL
                    {
                        if (!paramsBeforeOpcode)
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        }
                        if (a < (ScreenWidth / 2))
                            a = (ScreenWidth / 2);
                        if (b < (ScreenWidth / 2))
                            b = (ScreenWidth / 2);
                        if (a > roomData.Header.Width - (ScreenWidth / 2))
                            a = roomData.Header.Width - (ScreenWidth / 2);
                        if (b > roomData.Header.Width - (ScreenWidth / 2))
                            b = roomData.Header.Width - (ScreenWidth / 2);
                        _variables[VariableCameraMinX.Value] = a;
                        _variables[VariableCameraMaxX.Value] = b;
                    }
                    break;

                case 2:     // SO_ROOM_COLOR
                    {
                        if (Game.Version < 5)
                        {
                            if (!paramsBeforeOpcode)
                            {
                                a = GetVarOrDirectWord(OpCodeParameter.Param1);
                                b = GetVarOrDirectWord(OpCodeParameter.Param2);
                            }
                            ScummHelper.AssertRange(0, a, 256, "RoomOps: 2: room color slot");
                            Gdi.RoomPalette[b] = (byte)a;
                            _fullRedraw = true;
                        }
                        else
                        {
                            throw new NotSupportedException("room-color is no longer a valid command");
                        }
                    }
                    break;

                case 3:     // SO_ROOM_SCREEN
                    {
                        if (!paramsBeforeOpcode)
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        }
                        InitScreens(a, b);
                    }
                    break;

                case 4:     // SO_ROOM_PALETTE
                    {
                        if (Game.Version < 5)
                        {
                            if (!paramsBeforeOpcode)
                            {
                                a = GetVarOrDirectWord(OpCodeParameter.Param1);
                                b = GetVarOrDirectWord(OpCodeParameter.Param2);
                            }
                            ScummHelper.AssertRange(0, a, 256, "RoomOps: 4: room color slot");
                            _shadowPalette[b] = (byte)a;
                            SetDirtyColors(b, b);
                        }
                        else
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            b = GetVarOrDirectWord(OpCodeParameter.Param2);
                            var c = GetVarOrDirectWord(OpCodeParameter.Param3);
                            _opCode = ReadByte();
                            var d = GetVarOrDirectByte(OpCodeParameter.Param1);
                            SetPalColor(d, a, b, c);        /* index, r, g, b */
                        }
                    }
                    break;

                case 5:     // SO_ROOM_SHAKE_ON
                    SetShake(true);
                    break;

                case 6:     // SO_ROOM_SHAKE_OFF
                    SetShake(false);
                    break;

                case 7:     // SO_ROOM_SCALE
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var c = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var d = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        SetScaleSlot(e - 1, 0, b, a, 0, d, c);
                    }
                    break;
                case 8:     // SO_ROOM_INTENSITY
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var c = GetVarOrDirectByte(OpCodeParameter.Param3);
                        DarkenPalette(a, a, a, b, c);
                    }
                    break;
                case 9:         // SO_ROOM_SAVEGAME
                    {
                        _saveLoadFlag = GetVarOrDirectByte(OpCodeParameter.Param1);
                        _saveLoadSlot = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _saveLoadSlot = 99;                                     /* use this slot */
                        _saveTemporaryState = true;
                    }
                    break;

                case 10:    // SO_ROOM_FADE
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        if (a != 0)
                        {
                            _switchRoomEffect = (byte)(a & 0xFF);
                            _switchRoomEffect2 = (byte)(a >> 8);
                        }
                        else
                        {
                            FadeIn(_newEffect);
                        }
                    }
                    break;
                case 11:    // SO_RGB_ROOM_INTENSITY
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        var c = GetVarOrDirectWord(OpCodeParameter.Param3);
                        _opCode = ReadByte();
                        var d = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        DarkenPalette(a, b, c, d, e);
                    }
                    break;
                case 12:        // SO_ROOM_SHADOW
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        var c = GetVarOrDirectWord(OpCodeParameter.Param3);
                        _opCode = ReadByte();
                        var d = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        SetShadowPalette(a, b, c, d, e, 0, 256);
                    }
                    break;
                case 13:    // SO_SAVE_STRING
                    {
                        // This subopcode is used in Indy 4 to save the IQ points
                        // data. No other LucasArts game uses it. We use this fact
                        // to substitute a filename based on the targetname
                        // ("TARGET.iq").
                        //
                        // This way, the iq data of each Indy 4 variant stays
                        // separate. Moreover, the filename now clearly reflects to
                        // which target it belongs (as it should).
                        //
                        // In addition, the Monkey Island fan patch (which adds
                        // speech support and more things to MI 1 and 2) uses
                        // this opcode to generate a "monkey.cfg" file containing.
                        // some user controllable settings.
                        // Once more we use a custom filename ("TARGET.cfg").
                        var index = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var filename = System.Text.Encoding.ASCII.GetString(ReadCharacters());
                        filename = GetIqFilename(filename);

                        using (var file = System.IO.File.OpenWrite(filename))
                        {
                            var str = _strings[index];
                            file.Write(str, 0, str.Length);
                            Variables[VariableSoundResult.Value] = 0;
                        }
                        break;
                    }
                case 14:    // SO_LOAD_STRING
                    {
                        // This subopcode is used in Indy 4 to load the IQ points data.
                        // See SO_SAVE_STRING for details
                        var index = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var filename = System.Text.Encoding.ASCII.GetString(ReadCharacters());
                        filename = GetIqFilename(filename);
                        if (System.IO.File.Exists(filename))
                        {
                            _strings[index] = System.IO.File.ReadAllBytes(filename);
                        }
                    }
                    break;
                case 15:        // SO_ROOM_TRANSFORM
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        _opCode = ReadByte();
                        b = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var c = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var d = GetVarOrDirectByte(OpCodeParameter.Param1);
                        PalManipulateInit(a, b, c, d);
                    }
                    break;

                case 16:	// SO_CYCLE_SPEED
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        ScummHelper.AssertRange(1, a, 16, "o5_roomOps: 16: color cycle");
                        _colorCycle[a - 1].Delay = (ushort)((b != 0) ? 0x4000 / (b * 0x4C) : 0);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        void OldRoomEffect()
        {
            _opCode = ReadByte();
            if ((_opCode & 0x1F) == 3)
            {
                var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                if (a != 0)
                {
                    _switchRoomEffect = (byte)(a & 0xFF);
                    _switchRoomEffect2 = (byte)(a >> 8);
                }
                else
                {
                    FadeIn(_newEffect);
                }
            }
        }

        void PseudoRoom()
        {
            int i = ReadByte(), j;
            while ((j = ReadByte()) != 0)
            {
                if (j >= 0x80)
                {
                    _resourceMapper[j & 0x7F] = (byte)i;
                }
            }
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
        }
    }
}

