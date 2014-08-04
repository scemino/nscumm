//
//  ScummEngine_String.cs
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
using System.Collections.Generic;
using NScumm.Core.Graphics;
using System.Linq;
using System.Text;

namespace NScumm.Core
{
	partial class ScummEngine
	{
		byte[][] _strings;
		TextSlot[] _string = new TextSlot[6];

		void StringOperations ()
		{
			_opCode = ReadByte ();
			switch (_opCode & 0x1F) {
			case 1:
				{
					// loadstring
					var id = GetVarOrDirectByte (OpCodeParameter.Param1);
					_strings [id] = ReadCharacters ();
				}
				break;

			case 2:
				{
					// copy string
					var idA = GetVarOrDirectByte (OpCodeParameter.Param1);
					var idB = GetVarOrDirectByte (OpCodeParameter.Param2);
					_strings [idA] = new byte[_strings [idB].Length];
					Array.Copy (_strings [idB], _strings [idA], _strings [idB].Length);
				}
				break;

			case 3:
				{
					// Write Character
					var id = GetVarOrDirectByte (OpCodeParameter.Param1);
					var index = GetVarOrDirectByte (OpCodeParameter.Param2);
					var character = GetVarOrDirectByte (OpCodeParameter.Param3);
					_strings [id] [index] = (byte)character;
				}
				break;

			case 4:
				{
					// Get string char
					GetResult ();
					var id = GetVarOrDirectByte (OpCodeParameter.Param1);
					var b = GetVarOrDirectByte (OpCodeParameter.Param2);
					var result = _strings [id] [b];
					SetResult (result);
				}
				break;

			case 5:
				{
					// New String
					var id = GetVarOrDirectByte (OpCodeParameter.Param1);
					var size = GetVarOrDirectByte (OpCodeParameter.Param2);
					_strings [id] = new byte[size];
				}
				break;

			default:
				throw new NotImplementedException ();
			}
		}

		byte[] ReadCharacters ()
		{
			var sb = new List<byte> ();
			var character = ReadByte ();
			while (character != 0) {
				sb.Add (character);
				if (character == 0xFF) {
					character = ReadByte ();
					sb.Add (character);
					if (character != 1 && character != 2 && character != 3 && character != 8) {
						character = ReadByte ();
						sb.Add (character);
						character = ReadByte ();
						sb.Add (character);
					}
				}
				character = ReadByte ();
			}
			return sb.ToArray ();
		}

		void Print ()
		{
			_actorToPrintStrFor = GetVarOrDirectByte (OpCodeParameter.Param1);
			DecodeParseString ();
		}

		void DecodeParseString ()
		{
			int textSlot;
			switch (_actorToPrintStrFor) {
			case 252:
				textSlot = 3;
				break;

			case 253:
				textSlot = 2;
				break;

			case 254:
				textSlot = 1;
				break;

			default:
				textSlot = 0;
				break;
			}

			_string [textSlot].LoadDefault ();
			while ((_opCode = ReadByte ()) != 0xFF) {
				switch (_opCode & 0xF) {
				case 0:     // SO_AT
					_string [textSlot].Position = new Point (
						(short)GetVarOrDirectWord (OpCodeParameter.Param1),
						(short)GetVarOrDirectWord (OpCodeParameter.Param2));
					_string [textSlot].Overhead = false;
					break;

				case 1:     // SO_COLOR
					_string [textSlot].Color = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				case 2:     // SO_CLIPPED
					_string [textSlot].Right = (short)GetVarOrDirectWord (OpCodeParameter.Param1);
					break;

				case 4:     // SO_CENTER
					_string [textSlot].Center = true;
					_string [textSlot].Overhead = false;
					break;

				case 6:     // SO_LEFT
					{
						if (_game.Version == 3) {
							_string [textSlot].Height = GetVarOrDirectWord (OpCodeParameter.Param1);
						} else {
							_string [textSlot].Center = false;
							_string [textSlot].Overhead = false;
						}
					}
					break;

				case 7:     // SO_OVERHEAD
					_string [textSlot].Overhead = true;
					break;

				case 8:
					{	// SO_SAY_VOICE
						var offset = (ushort)GetVarOrDirectWord (OpCodeParameter.Param1);
						var delay = (ushort)GetVarOrDirectWord (OpCodeParameter.Param2);

						if (_game.Id == "loom" && _game.Version == 4) {
							if (offset == 0 && delay == 0) {
								_variables [VariableMusicTimer] = 0;
								_sound.StopCD ();
							} else {
								// Loom specified the offset from the start of the CD;
								// thus we have to subtract the length of the first track
								// (22500 frames) plus the 2 second = 150 frame leadin.
								// I.e. in total 22650 frames.
								offset = (ushort)(offset * 7.5 - 22500 - 2 * 75);

								// Slightly increase the delay (5 frames = 1/25 of a second).
								// This noticably improves the experience in Loom CD.
								delay = (ushort)(delay * 7.5 + 5);

								_sound.PlayCDTrack (1, 0, offset, delay);
							}
						} else {
							Console.Error.WriteLine ("ScummEngine: decodeParseString: Unhandled case 8");
						}
					}
					break;

				case 15:
					{   // SO_TEXTSTRING
						var tmp = ReadCharacters ();
						PrintString (textSlot, tmp);
					}
					return;

				default:
					throw new NotImplementedException ();
				}
			}

			// In SCUMM V1-V3, there were no 'default' values for the text slot
			// values. Hence to achieve correct behavior, we have to keep the
			// 'default' values in sync with the active values.
			//
			// Note: This is needed for Indy3 (Grail Diary). It's also needed
			// for Loom, or the lines Bobbin speaks during the intro are put
			// at position 0,0.
			//
			// Note: We can't use saveDefault() here because we only want to
			// save the position and color. In particular, we do not want to
			// save the 'center' flag. See bug #933168.
			if (_game.Version <= 3) {
				_string [textSlot].Default.Position = _string [textSlot].Position;
				_string [textSlot].Default.Height = _string [textSlot].Height;
				_string [textSlot].Default.Color = _string [textSlot].Color;
			}

			_string [textSlot].SaveDefault ();
		}

		void PrintString (int textSlot, byte[] msg)
		{
			switch (textSlot) {
			case 0:
				ActorTalk (msg);
				break;

			case 1:
				DrawString (1, msg);
				break;

			case 2:
				// TODO:
				//debugMessage(msg);
				break;

			case 3:
				// TODO:
				//    showMessageDialog(msg);
				break;

			default:
				throw new NotImplementedException ();
			}
		}

		static IList<char> ConvertMessage (IList<char> msg, int i, string text)
		{
			var src = msg.ToArray ();
			var dst = new char[msg.Count - 3 + text.Length];
			Array.Copy (src, dst, i - 1);
			Array.Copy (text.ToArray (), 0, dst, i - 1, text.Length);
			Array.Copy (src, i + 2, dst, i - 1 + text.Length, src.Length - i - 3);
			msg = dst;
			return msg;
		}

		int ConvertMessageToString (byte[] src, byte[] dst, int dstPos)
		{
			uint num = 0;
			int val;
			byte chr;
			int dstPosBegin = dstPos;

			while (num < src.Length) {
				chr = src [num++];
				if (chr == 0)
					break;

				if (chr == 0xFF) {
					chr = src [num++];

					if (chr == 1 || chr == 2 || chr == 3 || chr == 8) {
						// Simply copy these special codes
						dst [dstPos++] = 0xFF;
						dst [dstPos++] = chr;
					} else {
						val = src [num] | ((int)src [num + 1] << 8);
						switch (chr) {
						case 4:
							dstPos += ConvertIntMessage (dst, dstPos, val);
							break;

						case 5:
							dstPos += ConvertVerbMessage (dst, dstPos, val);
							break;

						case 6:
							dstPos += ConvertNameMessage (dst, dstPos, val);
							break;

						case 7:
							dstPos += ConvertStringMessage (dst, dstPos, val);
							break;

						case 9:
						case 10:
						case 12:
						case 13:
						case 14:
							// Simply copy these special codes
							dst [dstPos++] = 0xFF;
							dst [dstPos++] = chr;
							dst [dstPos++] = src [num + 0];
							dst [dstPos++] = src [num + 1];
							break;

						default:
							throw new NotSupportedException (string.Format ("convertMessageToString(): string escape sequence {0} unknown", chr));
						}
						num += 2;
					}
				} else {
					if (chr != '@') {
						dst [dstPos++] = chr;
					}
				}
			}

			dst [dstPos] = 0;

			return dstPos - dstPosBegin;
		}

		int ConvertNameMessage (byte[] dst, int dstPos, int var)
		{
			var num = ReadVariable (var);
			if (num != 0) {
				var ptr = GetObjectOrActorName (num);
				if (ptr != null) {
					return ConvertMessageToString (ptr, dst, dstPos);
				}
			}
			return 0;
		}

		int ConvertVerbMessage (byte[] dst, int dstPos, int var)
		{
			var num = ReadVariable (var);
			if (num != 0) {
				for (int k = 1; k < _verbs.Length; k++) {
					if (num == _verbs [k].VerbId && _verbs [k].Type == VerbType.Text && (_verbs [k].SaveId == 0)) {
						return ConvertMessageToString (_verbs [k].Text, dst, dstPos);
					}
				}
			}
			return 0;
		}

		int ConvertIntMessage (Array dst, int dstPos, int var)
		{
			var num = ReadVariable (var);
			var src = Encoding.ASCII.GetBytes (num.ToString ());
			Array.Copy (src, 0, dst, dstPos, src.Length);
			return src.Length;
		}

		int ConvertStringMessage (byte[] dst, int dstPos, int var)
		{
			if (var != 0) {
				var ptr = _strings [var];
				if (ptr != null) {
					return ConvertMessageToString (ptr, dst, dstPos);
				}
			}
			return 0;
		}

		void PrintEgo ()
		{
			_actorToPrintStrFor = _variables [VariableEgo];
			DecodeParseString ();
		}
	}
}

