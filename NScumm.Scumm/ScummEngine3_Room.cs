//
//  ScummEngine3_Room.cs
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
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine3
    {
        void LoadRoomWithEgo()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            int room = GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = Actors[Variables[VariableEgo.Value]];

            a.PutActor((byte)room);
            int oldDir = a.Facing;
            EgoPositioned = false;

            var x = ReadWordSigned();
            var y = ReadWordSigned();

            Variables[VariableWalkToObject.Value] = obj;
            StartScene(a.Room, a, obj);
            Variables[VariableWalkToObject.Value] = 0;

            if (Game.Version <= 4)
            {
                if (!EgoPositioned)
                {
                    int dir;
                    Point p;
                    GetObjectXYPos(obj, out p, out dir);
                    a.PutActor(p, CurrentRoom);
                    if (a.Facing == oldDir)
                        a.SetDirection(dir + 180);
                }
                a.Moving = 0;
            }

            // This is based on disassembly
            Camera.CurrentPosition.X = Camera.DestinationPosition.X = a.Position.X;
            if ((Game.GameId == GameId.Zak || Game.GameId == GameId.Loom) && (Game.Platform == Platform.FMTowns))
            {
                SetCameraAt(a.Position);
            }
            SetCameraFollows(a, false);

            _fullRedraw = true;

            if (x != -1)
            {
                a.StartWalk(new Point(x, y), -1);
            }
        }

        void RoomOps()
        {
            bool paramsBeforeOpcode = (Game.Version == 3);
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
                        Variables[VariableCameraMinX.Value] = a;
                        Variables[VariableCameraMaxX.Value] = b;
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
                            if (Game.Platform == Platform.FMTowns)
                            {
                                switch (a)
                                {
                                    case 8:
                                        TownsDrawStripToScreen(MainVirtScreen, 0, MainVirtScreen.TopLine, 0, 0, MainVirtScreen.Width, MainVirtScreen.TopLine + MainVirtScreen.Height);
                                        _townsScreen.Update();
                                        return;
                                    case 9:
                                        _townsActiveLayerFlags = 2;
                                        _townsScreen.ToggleLayers(_townsActiveLayerFlags);
                                        return;
                                    case 10:
                                        _townsActiveLayerFlags = 3;
                                        _townsScreen.ToggleLayers(_townsActiveLayerFlags);
                                        return;
                                    case 11:
                                        _townsScreen.ClearLayer(1);
                                        return;
                                    case 12:
                                        _townsActiveLayerFlags = 0;
                                        _townsScreen.ToggleLayers(_townsActiveLayerFlags);
                                        return;
                                    case 13:
                                        _townsActiveLayerFlags = 1;
                                        _townsScreen.ToggleLayers(_townsActiveLayerFlags);
                                        return;
                                    case 16: // enable clearing of layer 2 buffer in drawBitmap()
                                        TownsPaletteFlags |= 2;
                                        return;
                                    case 17: // disable clearing of layer 2 buffer in drawBitmap()
                                        TownsPaletteFlags &= ~2;
                                        return;
                                    case 18: // clear kMainVirtScreen layer 2 buffer
                                        Gdi.Fill(TextSurface,
                                            new Rect(0, MainVirtScreen.TopLine * TextSurfaceMultiplier, 
                                                TextSurface.Pitch, (MainVirtScreen.TopLine + MainVirtScreen.Height) * TextSurfaceMultiplier), 0);
                                        TownsPaletteFlags |= 1;
                                        return;
                                    case 19: // enable palette operations (palManipulate(), cyclePalette() etc.)
                                        TownsPaletteFlags |= 1;
                                        return;
                                    case 20: // disable palette operations
                                        TownsPaletteFlags &= ~1;
                                        return;
                                    case 21: // disable clearing of layer 0 in initScreens()
                                        _townsClearLayerFlag = 1;
                                        return;
                                    case 22: // enable clearing of layer 0 in initScreens()
                                        _townsClearLayerFlag = 0;
                                        return;
                                    case 30:
                                        TownsOverrideShadowColor = 3;
                                        return;
                                }
                            }
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
                        var filename = System.Text.Encoding.UTF8.GetString(ReadCharacters());
                        filename = GetIqFilename(filename);

                        using (var file = ServiceLocator.FileStorage.OpenFileWrite(filename))
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
                        var filename = System.Text.Encoding.UTF8.GetString(ReadCharacters());
                        filename = GetIqFilename(filename);
                        if (ServiceLocator.FileStorage.FileExists(filename))
                        {
                            _strings[index] = ServiceLocator.FileStorage.ReadAllBytes(filename);
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

                if (Game.Platform == Platform.FMTowns && Game.Version == 3)
                {
                    if (a == 4)
                    {
                        Gdi.Fill(TextSurface, new Rect(0, 0, TextSurface.Width * TextSurfaceMultiplier, TextSurface.Height * TextSurfaceMultiplier), 0);
                        if (_townsScreen != null)
                            _townsScreen.ClearLayer(1);
                        return;
                    }
                }

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
    }
}

