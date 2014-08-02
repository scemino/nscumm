//
//  ScummEngine_Cursor.cs
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
		Cursor _cursor = new Cursor ();
		Point _mousePos;
		byte cursorColor;
		int _currentCursor;
		sbyte _userPut;
		
		static byte[] defaultCursorColors = new byte[] { 15, 15, 7, 8 };
		ushort[][] _cursorImages = new ushort[4][];
		readonly byte[] _cursorHotspots = new byte[2 * 4];
		static readonly ushort[][] default_cursor_images = {
			/* cross-hair */
			new ushort[] {
				0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0000, 0x7e3f,
				0x0000, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0000
			},
			/* hourglass */
			new ushort[] {
				0x0000, 0x7ffe, 0x6006, 0x300c, 0x1818, 0x0c30, 0x0660, 0x03c0,
				0x0660, 0x0c30, 0x1998, 0x33cc, 0x67e6, 0x7ffe, 0x0000, 0x0000
			},
			/* arrow */
			new ushort[] {
				0x0000, 0x4000, 0x6000, 0x7000, 0x7800, 0x7c00, 0x7e00, 0x7f00,
				0x7f80, 0x78c0, 0x7c00, 0x4600, 0x0600, 0x0300, 0x0300, 0x0180
			},
			/* hand */
			new ushort[] {
				0x1e00, 0x1200, 0x1200, 0x1200, 0x1200, 0x13ff, 0x1249, 0x1249,
				0xf249, 0x9001, 0x9001, 0x9001, 0x8001, 0x8001, 0x8001, 0xffff
			}
		};
		static readonly byte[] default_cursor_hotspots = {
			8, 7,
			8, 7,
			1, 1,
			5, 0,
			8, 7, //zak256
		};

		void AnimateCursor ()
		{
			if (_cursor.Animate) {
				if ((_cursor.AnimateIndex & 0x1) == 0) {
					SetBuiltinCursor ((_cursor.AnimateIndex >> 1) & 3);
				}
				_cursor.AnimateIndex++;
			}
		}

		void SetBuiltinCursor (int idx)
		{
			var src = _cursorImages [_currentCursor];
			cursorColor = defaultCursorColors [idx];

			_cursor.Hotspot = new Point (
				(short)(_cursorHotspots [2 * _currentCursor] * _textSurfaceMultiplier),
				(short)(_cursorHotspots [2 * _currentCursor + 1] * _textSurfaceMultiplier));
			_cursor.Width = 16 * _textSurfaceMultiplier;
			_cursor.Height = 16 * _textSurfaceMultiplier;

			var pixels = new byte[_cursor.Width * _cursor.Height];

			int offset = 0;
			for (int w = 0; w < _cursor.Width; w++) {
				for (int h = 0; h < _cursor.Height; h++) {
					if ((src [w] & (1 << h)) != 0) {
						pixels [offset] = cursorColor;
					}
					offset++;
				}
			}

			_gfxManager.SetCursor (pixels, _cursor.Width, _cursor.Height, _cursor.Hotspot);
		}

		void ResetCursors ()
		{
			for (int i = 0; i < 4; i++) {
				_cursorImages [i] = new ushort[16];
				Array.Copy (default_cursor_images [i], _cursorImages [i], 16);
			}
			Array.Copy (default_cursor_hotspots, _cursorHotspots, 8);
		}

		void RedefineBuiltinCursorFromChar (int index, int chr)
		{
			// Cursor image in both Loom versions are based on images from charset.
			// This function is *only* supported for Loom!
			if (_game.Id != "loom")
				throw new NotSupportedException ("RedefineBuiltinCursorFromChar is *only* supported for Loom!");
			if (index < 0 || index >= 4)
				throw new ArgumentException ("index");

			//	const int oldID = _charset->getCurID();

			var ptr = _cursorImages [index];

			_charset.SetCurID (1);

			var s = new Surface (_charset.GetCharWidth (chr), _charset.GetFontHeight (), PixelFormat.Indexed8, false);
			var p = new PixelNavigator (s);
			Gdi.Fill (new PixelNavigator (s), 123, s.Width, s.Height);

			_charset.DrawChar (chr, s, 0, 0);

			Array.Clear (ptr, 0, ptr.Length);
			for (int h = 0; h < s.Height; h++) {
				for (int w = 0; w < s.Width; w++) {
					p.GoTo (w, h);
					if (p.Read () != 123) {
						ptr [h] |= (ushort)(1 << (15 - w));
					}
				}
			}

			//	_charset->setCurID(oldID);
		}

		void RedefineBuiltinCursorHotspot (int index, int x, int y)
		{
			// Cursor image in both Looms are based on images from charset.
			// This function is *only* supported for Loom!
			if (_game.Id != "loom")
				throw new NotSupportedException ("RedefineBuiltinCursorHotspot is *only* supported for Loom!");
			if (index < 0 || index >= 4)
				throw new ArgumentException ("index");

			_cursorHotspots [index * 2] = (byte)x;
			_cursorHotspots [index * 2 + 1] = (byte)y;
		}

		void CursorCommand ()
		{
			_opCode = ReadByte ();
			switch (_opCode) {
			case 1:
				// Cursor On
				_cursor.State = 1;
				VerbMouseOver (0);
				break;

			case 2:
				// Cursor Off
				_cursor.State = 0;
				VerbMouseOver (0);
				break;

			case 3:
				// User Input on
				_userPut = 1;
				break;

			case 4:
				// User Input off
				_userPut = 0;
				break;

			case 5:
				// SO_CURSOR_SOFT_ON
				_cursor.State++;
				VerbMouseOver (0);
				break;

			case 6:
				// SO_CURSOR_SOFT_OFF
				_cursor.State--;
				VerbMouseOver (0);
				break;

			case 7:         // SO_USERPUT_SOFT_ON
				_userPut++;
				break;

			case 8:         // SO_USERPUT_SOFT_OFF
				_userPut--;
				break;

			case 10:
				{
					// SO_CURSOR_IMAGE
					var i = GetVarOrDirectByte (OpCodeParameter.Param1); // Cursor number
					var j = GetVarOrDirectByte (OpCodeParameter.Param2); // Charset letter to use
					RedefineBuiltinCursorFromChar (i, j);
				}
				break;

			case 11:        // SO_CURSOR_HOTSPOT
				{
					var i = GetVarOrDirectByte (OpCodeParameter.Param1);
					var j = GetVarOrDirectByte (OpCodeParameter.Param2);
					var k = GetVarOrDirectByte (OpCodeParameter.Param3);
					RedefineBuiltinCursorHotspot (i, j, k);
				}
				break;

			case 12:
				{
					// SO_CURSOR_SET
					var i = GetVarOrDirectByte (OpCodeParameter.Param1);
					if (i >= 0 && i <= 3) {
						_currentCursor = i;
					} else {
						Console.Error.WriteLine ("CURSOR_SET: unsupported cursor id {0}", i);
					}
					break;
				}
			case 13:
				InitCharset (GetVarOrDirectByte (OpCodeParameter.Param1));
				break;

			default:
				throw new NotImplementedException ();
			}

			_variables [VariableCursorState] = _cursor.State;
			_variables [VariableUserPut] = _userPut;
		}
	}
}

