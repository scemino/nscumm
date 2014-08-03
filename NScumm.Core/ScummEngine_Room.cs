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
		public byte[] RoomPalette = new byte[256];

		internal Room CurrentRoomData {
			get { return roomData; }
		}

		internal byte CurrentRoom {
			get { return _currentRoom; }
		}

		void LoadRoom ()
		{
			var room = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
			if (room != _currentRoom) {
				StartScene (room);
			}
			_fullRedraw = true;
		}

		void LoadRoomWithEgo ()
		{
			int obj = GetVarOrDirectWord (OpCodeParameter.Param1);
			int room = GetVarOrDirectByte (OpCodeParameter.Param2);

			var a = _actors [_variables [VariableEgo]];

			a.PutActor ((byte)room);
			int oldDir = a.Facing;
			_egoPositioned = false;

			short x = ReadWordSigned ();
			short y = ReadWordSigned ();

			_variables [VariableWalkToObject] = obj;
			StartScene (a.Room);
			_variables [VariableWalkToObject] = 0;

			if (!_egoPositioned) {
				int dir;
				Point p;
				GetObjectXYPos (obj, out p, out dir);
				a.PutActor (p, _currentRoom);
				if (a.Facing == oldDir)
					a.SetDirection (dir + 180);
			}
			a.Moving = 0;

			// This is based on disassembly
			_camera.CurrentPosition.X = _camera.DestinationPosition.X = a.Position.X;
			SetCameraFollows (a, false);

			_fullRedraw = true;

			if (x != -1) {
				a.StartWalk (new Point (x, y), -1);
			}
		}

		void RoomOps ()
		{
			_opCode = ReadByte ();
			switch (_opCode & 0x1F) {
			case 1:     // SO_ROOM_SCROLL
				{
					var a = GetVarOrDirectWord (OpCodeParameter.Param1);
					var b = GetVarOrDirectWord (OpCodeParameter.Param2);
					if (a < (ScreenWidth / 2))
						a = (ScreenWidth / 2);
					if (b < (ScreenWidth / 2))
						b = (ScreenWidth / 2);
					if (a > roomData.Header.Width - (ScreenWidth / 2))
						a = roomData.Header.Width - (ScreenWidth / 2);
					if (b > roomData.Header.Width - (ScreenWidth / 2))
						b = roomData.Header.Width - (ScreenWidth / 2);
					_variables [VariableCameraMinX] = a;
					_variables [VariableCameraMaxX] = b;
				}
				break;

			case 2:     // SO_ROOM_COLOR
				{
					var a = GetVarOrDirectWord (OpCodeParameter.Param1);
					var b = GetVarOrDirectWord (OpCodeParameter.Param2);
					RoomPalette [b] = (byte)a;
					_fullRedraw = true;
				}
				break;

			case 3:     // SO_ROOM_SCREEN
				{
					var a = GetVarOrDirectWord (OpCodeParameter.Param1);
					var b = GetVarOrDirectWord (OpCodeParameter.Param2);
					InitScreens (a, b);
				}
				break;

			case 4:     // SO_ROOM_PALETTE
				{
					var a = GetVarOrDirectWord (OpCodeParameter.Param1);
					var b = GetVarOrDirectWord (OpCodeParameter.Param2);

					ScummHelper.AssertRange (0, a, 256, "RoomOps: 4: room color slot");
					_shadowPalette [b] = (byte)a;
					SetDirtyColors (b, b);
				}
				break;

			case 5:     // SO_ROOM_SHAKE_ON
				SetShake (true);
				break;

			case 6:     // SO_ROOM_SHAKE_OFF
				SetShake (false);
				break;

			case 7:     // SO_ROOM_SCALE
				{
					var a = GetVarOrDirectByte (OpCodeParameter.Param1);
					var b = GetVarOrDirectByte (OpCodeParameter.Param2);
					_opCode = ReadByte ();
					var c = GetVarOrDirectByte (OpCodeParameter.Param1);
					var d = GetVarOrDirectByte (OpCodeParameter.Param2);
					_opCode = ReadByte ();
					var e = GetVarOrDirectByte (OpCodeParameter.Param2);
					SetScaleSlot (e - 1, 0, b, a, 0, d, c);
				}
				break;

			case 10:    // SO_ROOM_FADE
				{
					var a = GetVarOrDirectWord (OpCodeParameter.Param1);
					if (a != 0) {
						_switchRoomEffect = (byte)(a & 0xFF);
						_switchRoomEffect2 = (byte)(a >> 8);
					} else {
						FadeIn (_newEffect);
					}
				}
				break;

			default:
				throw new NotImplementedException ();
			}
		}

		void OldRoomEffect ()
		{
			_opCode = ReadByte ();
			if ((_opCode & 0x1F) == 3) {
				var a = GetVarOrDirectWord (OpCodeParameter.Param1);
				if (a != 0) {
					_switchRoomEffect = (byte)(a & 0xFF);
					_switchRoomEffect2 = (byte)(a >> 8);
				} else {
					FadeIn (_newEffect);
				}
			}
		}

		void PseudoRoom ()
		{
			int i = ReadByte (), j;
			while ((j = ReadByte ()) != 0) {
				if (j >= 0x80) {
					_resourceMapper [j & 0x7F] = (byte)i;
				}
			}
		}

		void ResetRoomObjects ()
		{
			for (int i = 0; i < roomData.Objects.Count; i++) {
				_objs [i + 1].Position = roomData.Objects [i].Position;
				_objs [i + 1].Width = roomData.Objects [i].Width;
				_objs [i + 1].Walk = roomData.Objects [i].Walk;
				_objs [i + 1].State = roomData.Objects [i].State;
				_objs [i + 1].Parent = roomData.Objects [i].Parent;
				_objs [i + 1].ParentState = roomData.Objects [i].ParentState;
				_objs [i + 1].Number = roomData.Objects [i].Number;
				_objs [i + 1].Height = roomData.Objects [i].Height;
				_objs [i + 1].Flags = roomData.Objects [i].Flags;
				_objs [i + 1].ActorDir = roomData.Objects [i].ActorDir;
				_objs [i + 1].Image = roomData.Objects [i].Image;
				_objs [i + 1].Script.Offset = roomData.Objects [i].Script.Offset;
				_objs [i + 1].Script.Data = roomData.Objects [i].Script.Data;
				_objs [i + 1].ScriptOffsets.Clear ();
				foreach (var scriptOffset in roomData.Objects[i].ScriptOffsets) {
					_objs [i + 1].ScriptOffsets.Add (scriptOffset.Key, scriptOffset.Value);
				}
				_objs [i + 1].Name = roomData.Objects [i].Name;
			}
			for (int i = roomData.Objects.Count + 1; i < _objs.Length; i++) {
				_objs [i].Number = 0;
				_objs [i].Script.Offset = 0;
				_objs [i].ScriptOffsets.Clear ();
				_objs [i].Script.Data = new byte[0];
			}
		}

		void ClearRoomObjects ()
		{
			for (int i = 0; i < _objs.Length; i++) {
				_objs [i].Number = 0;
			}
		}

		void ResetRoomSubBlocks ()
		{
			_boxMatrix.Clear ();
			_boxMatrix.AddRange (roomData.BoxMatrix);

			for (int i = 0; i < _scaleSlots.Length; i++) {
				_scaleSlots [i] = new ScaleSlot ();
			}

			for (int i = 1; i <= roomData.Scales.Length; i++) {
				var scale = roomData.Scales [i - 1];
				if (scale.Scale1 != 0 || scale.Y1 != 0 || scale.Scale2 != 0 || scale.Y2 != 0) {
					SetScaleSlot (i, 0, scale.Y1, scale.Scale1, 0, scale.Y2, scale.Scale2);
				}
			}

			_boxes = new Box[roomData.Boxes.Count];
			for (int i = 0; i < roomData.Boxes.Count; i++) {
				var box = roomData.Boxes [i];
				_boxes [i] = new Box {
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

			Array.Copy (roomData.ColorCycle, _colorCycle, 16);
		}
	}
}

