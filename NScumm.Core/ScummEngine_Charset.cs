//
//  ScummEngine_Charset.cs
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
using System.Linq;
using System.IO;
using NScumm.Core.Graphics;

namespace NScumm.Core
{
	partial class ScummEngine
	{
		const byte CharsetMaskTransparency = 0xFD;
		byte[] _charsetBuffer = new byte[512];
		byte[][] _charsets;
		public byte[] CharsetColorMap = new byte[16];
		int _charsetBufPos;
		readonly CharsetRenderer _charset;
		// Somewhat hackish stuff for 2 byte support (Chinese/Japanese/Korean)
		public byte NewLineCharacter;
		internal bool UseCjkMode;
		internal int _2byteWidth;
		byte _charsetColor;
		int _nextLeft, _nextTop;

		void InitCharset (int charsetNum)
		{
			_string [0].Default.Charset = (byte)charsetNum;
			_string [1].Default.Charset = (byte)charsetNum;

			//if (_charsets[charsetNum] != null)
			//{
			//    Array.Copy(_charsets[charsetNum], _charsetColorMap, 16);
			//}
		}

		void LoadCharset (int resId)
		{
			var diskName = string.Format ("{0}.lfl", 900 + resId);
			var path = (from p in Directory.GetFiles (_directory)
				let file = Path.GetFileName (p)
				where StringComparer.OrdinalIgnoreCase.Equals (file, diskName)
				select p).First ();
			using (var br = new BinaryReader (File.OpenRead (path))) {
				var size = (int)br.ReadUInt32 () + 11;
				_charsets [resId] = br.ReadBytes (size);
			}
		}

		void Charset ()
		{
			if (_haveMsg == 0)
				return;

            if (Game.Version >= 4)
            {
                // Do nothing while the camera is moving
                if ((_camera.DestinationPosition.X / 8) != (_camera.CurrentPosition.X / 8) || _camera.CurrentPosition.X != _camera.LastPosition.X)
                    return;
            }

			Actor a = null;
			if (TalkingActor != 0xFF)
				a = _actors [TalkingActor];

			if (a != null && _string [0].Overhead) {
				int s;

				_string [0].Position = new Point (
					(short)(a.Position.X - MainVirtScreen.XStart),
					(short)(a.Position.Y - a.Elevation - ScreenTop));

				if (_variables [VariableTalkStringY] < 0) {
					s = (a.ScaleY * _variables [VariableTalkStringY]) / 0xFF;
					_string [0].Position = _string [0].Position.Offset (0, (short)(((_variables [VariableTalkStringY] - s) / 2) + s));
				} else {
					_string [0].Position = new Point (_string [0].Position.X, (short)_variables [VariableTalkStringY]);
				}

				if (_string [0].Position.Y < 1)
					_string [0].Position = new Point (_string [0].Position.X, 1);

				if (_string [0].Position.X < 80)
					_string [0].Position = new Point (80, _string [0].Position.Y);
				if (_string [0].Position.X > ScreenWidth - 80)
					_string [0].Position = new Point ((short)(ScreenWidth - 80), _string [0].Position.Y);
			}

			_charset.Top = _string [0].Position.Y + ScreenTop;
			_charset.StartLeft = _charset.Left = _string [0].Position.X;
			_charset.Right = _string [0].Right;
			_charset.Center = _string [0].Center;
			_charset.SetColor (_charsetColor);

			if (a != null && a.Charset != 0)
				_charset.SetCurID (a.Charset);
			else
				_charset.SetCurID (_string [0].Charset);

			if (_talkDelay != 0)
				return;

			if (_haveMsg == 1) {
				// TODO:
				//if ((_sound->_sfxMode & 2) == 0)
				StopTalk ();
				return;
			}

			if (a != null && !_string [0].NoTalkAnim) {
				a.RunTalkScript (a.TalkStartFrame);
				_useTalkAnims = true;
			}

			_talkDelay = 60;

			if (!_keepText) {
				RestoreCharsetBg ();
			}

			int maxWidth = _charset.Right - _string [0].Position.X - 1;
			if (_charset.Center) {
				if (maxWidth > _nextLeft)
					maxWidth = _nextLeft;
				maxWidth *= 2;
			}

			_charset.AddLinebreaks (0, _charsetBuffer, _charsetBufPos, maxWidth);

			if (_charset.Center) {
				_nextLeft -= _charset.GetStringWidth (0, _charsetBuffer, _charsetBufPos) / 2;
				if (_nextLeft < 0)
					_nextLeft = 0;
			}

			_charset.DisableOffsX = _charset.FirstChar = !_keepText;

			int c = 0;
			while (HandleNextCharsetCode (a, ref c)) {
				if (c == 0) {
					// End of text reached, set _haveMsg accordingly
					_haveMsg = 1;
					_keepText = false;
					break;
				}

				if (c == 13) {
					if (!NewLine ())
						break;
					continue;
				}

				_charset.Left = _nextLeft;
				_charset.Top = _nextTop;

				_charset.PrintChar (c, false);
				_nextLeft = _charset.Left;
				_nextTop = _charset.Top;

				_talkDelay += _variables [VariableCharIncrement];
			}
		}

		bool NewLine ()
		{
			_nextLeft = _string [0].Position.X;
			if (_charset.Center) {
				_nextLeft -= _charset.GetStringWidth (0, _charsetBuffer, _charsetBufPos) / 2;
				if (_nextLeft < 0)
					_nextLeft = 0;
			}

			bool useCJK = UseCjkMode;
			_nextTop += _charset.GetFontHeight ();
			UseCjkMode = useCJK;

			// FIXME: is this really needed?
			_charset.DisableOffsX = true;

			return true;
		}

		bool HandleNextCharsetCode (Actor a, ref int code)
		{
			int color, frme, c = 0, oldy;
			bool endLoop = false;
			//byte* buffer = _charsetBuffer + _charsetBufPos;
			int bufferPos = _charsetBufPos;
			while (!endLoop) {
				c = _charsetBuffer [bufferPos++];
				if (!(c == 0xFF || (c == 0xFE))) {
					break;
				}
				c = _charsetBuffer [bufferPos++];

				if (NewLineCharacter != 0 && c == NewLineCharacter) {
					c = 13;
					break;
				}

				switch (c) {
				case 1:
					c = 13; // new line
					endLoop = true;
					break;

				case 2:
					_haveMsg = 0;
					_keepText = true;
					endLoop = true;
					break;

				case 3:
					_haveMsg = 0xFF;
					_keepText = false;
					endLoop = true;
					break;

				case 8:
					// Ignore this code here. Occurs e.g. in MI2 when you
					// talk to the carpenter on scabb island. It works like
					// code 1 (=newline) in verb texts, but is ignored in
					// spoken text (i.e. here). Used for very long verb
					// sentences.
					break;

				case 9:
					frme = _charsetBuffer [bufferPos] | (_charsetBuffer [bufferPos + 1] << 8);
					bufferPos += 2;
					if (a != null)
						a.StartAnimActor ((byte)frme);
					break;

				case 10:
					// Note the similarity to the code in debugMessage()
					//talk_sound_a = (uint)(_charsetBuffer[bufferPos] | (_charsetBuffer[bufferPos + 1] << 8) | (_charsetBuffer[bufferPos + 4] << 16) | (_charsetBuffer[bufferPos + 5] << 24));
					//talk_sound_b = (uint)(_charsetBuffer[bufferPos + 8] | (_charsetBuffer[bufferPos + 9] << 8) | (_charsetBuffer[bufferPos + 12] << 16) | (_charsetBuffer[bufferPos + 13] << 24));
					bufferPos += 14;
					//_sound->talkSound(talk_sound_a, talk_sound_b, 2);
					_haveActorSpeechMsg = false;
					break;

				case 12:
					color = _charsetBuffer [bufferPos] | (_charsetBuffer [bufferPos + 1] << 8);
					bufferPos += 2;
					if (color == 0xFF)
						_charset.SetColor (_charsetColor);
					else
						_charset.SetColor ((byte)color);
					break;

				case 13:
					//debug(0, "handleNextCharsetCode: Unknown opcode 13 %d", READ_LE_UINT16(buffer));
					bufferPos += 2;
					break;

				case 14:
					oldy = _charset.GetFontHeight ();
					_charset.SetCurID (_charsetBuffer [bufferPos++]);
					bufferPos += 2;
					// TODO:
					//memcpy(_charsetColorMap, _charsetData[_charset.getCurID()], 4);
					_nextTop -= _charset.GetFontHeight () - oldy;
					break;

				default:
					throw new NotSupportedException (string.Format ("handleNextCharsetCode: invalid code {0}", c));
				}
			}
			_charsetBufPos = bufferPos;
			code = c;
			return (c != 2 && c != 3);
		}

		void RestoreCharsetBg ()
		{
			_nextLeft = _string [0].Position.X;
			_nextTop = _string [0].Position.Y + ScreenTop;

			if (_charset.HasMask) {
				_charset.HasMask = false;
				_charset.Str.Left = -1;
				_charset.Left = -1;

				// Restore background on the whole text area. This code is based on
				// restoreBackground(), but was changed to only restore those parts which are
				// currently covered by the charset mask.

				var vs = _charset.TextScreen;
				if (vs.Height == 0)
					return;

				MarkRectAsDirty (vs, 0, vs.Width, 0, vs.Height, Gdi.UsageBitRestored);

				if (vs.HasTwoBuffers && _currentRoom != 0 && IsLightOn ()) {
					if (vs != MainVirtScreen) {
						// Restore from back buffer
						var screenBufNav = new PixelNavigator (vs.Surfaces [0]);
						screenBufNav.OffsetX (vs.XStart);
						var backNav = new PixelNavigator (vs.Surfaces [1]);
						backNav.OffsetX (vs.XStart);
						Gdi.Blit (screenBufNav, backNav, vs.Width, vs.Height);
					}
				} else {
					// Clear area
					var screenBuf = vs.Surfaces [0].Pixels;
					Array.Clear (screenBuf, 0, screenBuf.Length);
				}

				if (vs.HasTwoBuffers) {
					// Clean out the charset mask
					ClearTextSurface ();
				}
			}
		}


	}
}

