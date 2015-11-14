//
//  ScummEngine6_Room.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        [OpCode(0x9c)]
        protected virtual void RoomOps()
        {
            var subOp = ReadByte();

            switch (subOp)
            {
                case 172:               // SO_ROOM_SCROLL
                    {
                        var b = Pop();
                        var a = Pop();
                        if (a < (ScreenWidth / 2))
                            a = (ScreenWidth / 2);
                        if (b < (ScreenWidth / 2))
                            b = (ScreenWidth / 2);
                        if (a > CurrentRoomData.Header.Width - (ScreenWidth / 2))
                            a = CurrentRoomData.Header.Width - (ScreenWidth / 2);
                        if (b > CurrentRoomData.Header.Width - (ScreenWidth / 2))
                            b = CurrentRoomData.Header.Width - (ScreenWidth / 2);
                        Variables[VariableCameraMinX.Value] = a;
                        Variables[VariableCameraMaxX.Value] = b;
                    }
                    break;

                case 174:               // SO_ROOM_SCREEN
                    {
                        var b = Pop();
                        var a = Pop();
                        InitScreens(a, b);
                    }
                    break;

                case 175:               // SO_ROOM_PALETTE
                    {
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        SetPalColor(d, a, b, c);
                    }
                    break;

                case 176:               // SO_ROOM_SHAKE_ON
                    SetShake(true);
                    break;

                case 177:               // SO_ROOM_SHAKE_OFF
                    SetShake(false);
                    break;

                case 179:               // SO_ROOM_INTENSITY
                    {
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        DarkenPalette(a, a, a, b, c);
                    }
                    break;

                case 180:               // SO_ROOM_SAVEGAME
                    _saveTemporaryState = true;
                    _saveLoadSlot = Pop();
                    _saveLoadFlag = Pop();
                    if (Game.GameId == Scumm.IO.GameId.Tentacle)
                        _saveSound = (_saveLoadSlot != 0);
                    break;
                case 181:               // SO_ROOM_FADE
                    {
                        var a = Pop();
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

                case 182:               // SO_RGB_ROOM_INTENSITY
                    {
                        var e = Pop();
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        DarkenPalette(a, b, c, d, e);
                    }
                    break;

                case 183:               // SO_ROOM_SHADOW
                    {
                        var e = Pop();
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        SetShadowPalette(a, b, c, d, e, 0, 256);
                    }
                    break;

                case 184:               // SO_SAVE_STRING
                    throw new NotImplementedException("save string not implemented");

                case 185:               // SO_LOAD_STRING
                    throw new NotImplementedException("load string not implemented");

                case 186:               // SO_ROOM_TRANSFORM
                    {
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        PalManipulateInit(a, b, c, d);
                    }
                    break;

                case 187:               // SO_CYCLE_SPEED
                    {
                        var b = Pop();
                        var a = Pop();
                        ScummHelper.AssertRange(1, a, 16, "RoomOps: 187: color cycle");
                        _colorCycle[a - 1].Delay = (ushort)((b != 0) ? 0x4000 / (b * 0x4C) : 0);
                    }
                    break;
                case 213:               // SO_ROOM_NEW_PALETTE
                    {
                        var a = Pop();

                        // This opcode is used when turning off noir mode in Sam & Max,
                        // but since our implementation of this feature doesn't change
                        // the original palette there's no need to reload it. Doing it
                        // this way, we avoid some graphics glitches that the original
                        // interpreter had.

                        if (Game.GameId == Scumm.IO.GameId.SamNMax && Slots[CurrentScript].Number == 64)
                            SetDirtyColors(0, 255);
                        else
                            SetCurrentPalette(a);
                    }
                    break;
                default:
                    throw new NotSupportedException(string.Format("RoomOps: default case {0}", subOp));
            }

        }

        [OpCode(0x7b)]
        protected virtual void LoadRoom(byte room)
        {
            StartScene(room);
            _fullRedraw = true;
        }

        [OpCode(0x85)]
        protected virtual void LoadRoomWithEgo(int x, int y)
        {
            int obj, room;
            PopRoomAndObj(out room, out obj);
            var a = Actors[Variables[VariableEgo.Value]];
            a.PutActor((byte)room);
            EgoPositioned = false;

            Variables[VariableWalkToObject.Value] = obj;
            StartScene(a.Room, a, obj);
            Variables[VariableWalkToObject.Value] = 0;

            if (Game.Version == 6)
            {
                Camera.CurrentPosition.X = Camera.DestinationPosition.X = a.Position.X;
                SetCameraFollows(a);
            }

            _fullRedraw = true;

            if (x != -1 && x != 0x7FFFFFFF)
            {
                a.StartWalk(new NScumm.Core.Graphics.Point(x, y), -1);
            }
        }

        [OpCode(0x8c)]
        protected virtual void GetActorRoom(int index)
        {
            if (index == 0)
            {
                // This case occurs at the very least in COMI. That's because in COMI's script 28,
                // there is a check which looks as follows:
                //   if (((VAR_TALK_ACTOR != 0) && (VAR_HAVE_MSG == 1)) &&
                //        (getActorRoom(VAR_TALK_ACTOR) == VAR_ROOM))
                // Due to the way this is represented in bytecode, the engine cannot
                // short circuit. Hence, even though this would be perfectly fine code
                // in C/C++, here it can (and does) lead to getActorRoom(0) being
                // invoked. We silently ignore this.
                Push(0);
                return;
            }

            if (index == 255)
            {
                // This case also occurs in COMI...
                Push(0);
                return;
            }

            var actor = Actors[index];
            Push(actor.Room);
        }

        [OpCode(0xa6)]
        protected virtual void DrawBox(int x, int y, int x2, int y2, int color)
        {
            DrawBoxCore(x, y, x2, y2, color);
        }

        [OpCode(0xe3)]
        protected void PickVarRandom(int[] args)
        {
            var value = ReadWord();

            if (ReadVariable(value) == 0)
            {
                DefineArray(value, ArrayType.IntArray, 0, args.Length);
                if (args.Length > 0)
                {
                    int counter = 0;
                    do
                    {
                        WriteArray(value, 0, counter + 1, args[counter]);
                    } while (++counter < args.Length);
                }

                ShuffleArray(value, 1, args.Length);
                WriteArray(value, 0, 0, 2);
                Push(ReadArray(value, 0, 1));
                return;
            }

            var num = ReadArray(value, 0, 0);

            var ah = GetArray(value);
            var dim1 = ah.Dim1 - 1;

            if (dim1 < num)
            {
                var var_2 = ReadArray(value, 0, num - 1);
                ShuffleArray(value, 1, dim1);
                if (ReadArray(value, 0, 1) == var_2)
                {
                    num = 2;
                }
                else
                {
                    num = 1;
                }
            }

            WriteArray(value, 0, 0, num + 1);
            Push(ReadArray(value, 0, num));
        }

        [OpCode(0xe4)]
        protected void SetBoxSet()
        {
            throw new NotImplementedException("TODO: SetBoxSet");
        }

    }
}

