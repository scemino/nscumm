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
        Room roomData;

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
            if (room != _currentRoom)
            {
                StartScene(room);
            }
            _fullRedraw = true;
        }

        void LoadRoomWithEgo()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            int room = GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = _actors[_variables[VariableEgo.Value]];

            a.PutActor((byte)room);
            int oldDir = a.Facing;
            _egoPositioned = false;

            short x = ReadWordSigned();
            short y = ReadWordSigned();

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

        string GetIqFilename(string filename)
        {
            var targetName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Game.Path), Game.Id);
            if (_game.Id == "atlantis")
            {
                filename = targetName + ".iq";
            }
            else if (_game.Id == "monkey" || _game.Id == "monkey2")
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
            for (int i = 0; i < roomData.Objects.Count; i++)
            {
                _objs[i + 1].Position = roomData.Objects[i].Position;
                _objs[i + 1].Width = roomData.Objects[i].Width;
                _objs[i + 1].Walk = roomData.Objects[i].Walk;
                _objs[i + 1].State = roomData.Objects[i].State;
                _objs[i + 1].Parent = roomData.Objects[i].Parent;
                _objs[i + 1].ParentState = roomData.Objects[i].ParentState;
                _objs[i + 1].Number = roomData.Objects[i].Number;
                _objs[i + 1].Height = roomData.Objects[i].Height;
                _objs[i + 1].Flags = roomData.Objects[i].Flags;
                _objs[i + 1].ActorDir = roomData.Objects[i].ActorDir;
                _objs[i + 1].Script.Offset = roomData.Objects[i].Script.Offset;
                _objs[i + 1].Script.Data = roomData.Objects[i].Script.Data;
                _objs[i + 1].ScriptOffsets.Clear();
                foreach (var scriptOffset in roomData.Objects[i].ScriptOffsets)
                {
                    _objs[i + 1].ScriptOffsets.Add(scriptOffset.Key, scriptOffset.Value);
                }
                _objs[i + 1].Name = roomData.Objects[i].Name;
            }
            for (int i = roomData.Objects.Count + 1; i < _objs.Length; i++)
            {
                _objs[i].Number = 0;
                _objs[i].Script.Offset = 0;
                _objs[i].ScriptOffsets.Clear();
                _objs[i].Script.Data = new byte[0];
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
                if (scale.Scale1 != 0 || scale.Y1 != 0 || scale.Scale2 != 0 || scale.Y2 != 0)
                {
                    SetScaleSlot(i, 0, scale.Y1, scale.Scale1, 0, scale.Y2, scale.Scale2);
                }
            }

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

