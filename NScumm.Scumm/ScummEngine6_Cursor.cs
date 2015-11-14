//
//  ScummEngine6_Cursor.cs
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
using System.Linq;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        static readonly byte[] default_v6_cursor =
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
                0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0xFF,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            };

        [OpCode(0x6b)]
        protected virtual void CursorCommand()
        {
            var subOp = ReadByte();
            switch (subOp)
            {
                case 0x90:              // SO_CURSOR_ON Turn cursor on
                    _cursor.State = 1;
                    VerbMouseOver(0);
                    break;
                case 0x91:              // SO_CURSOR_OFF Turn cursor off
                    _cursor.State = 0;
                    VerbMouseOver(0);
                    break;
                case 0x92:              // SO_USERPUT_ON
                    _userPut = 1;
                    break;
                case 0x93:              // SO_USERPUT_OFF
                    _userPut = 0;
                    break;
                case 0x94:              // SO_CURSOR_SOFT_ON Turn soft cursor on
                    _cursor.State++;
                    if (_cursor.State > 1)
                        throw new NotSupportedException("Cursor state greater than 1 in script");
                    VerbMouseOver(0);
                    break;
                case 0x95:              // SO_CURSOR_SOFT_OFF Turn soft cursor off
                    _cursor.State--;
                    VerbMouseOver(0);
                    break;
                case 0x96:              // SO_USERPUT_SOFT_ON
                    _userPut++;
                    break;
                case 0x97:              // SO_USERPUT_SOFT_OFF
                    _userPut--;
                    break;
                case 0x99:              // SO_CURSOR_IMAGE Set cursor image
                    {
                        int room, obj;
                        PopRoomAndObj(out room, out obj);
                        SetCursorFromImg(obj, room, 1);
                        break;
                    }
                case 0x9A:              // SO_CURSOR_HOTSPOT Set cursor hotspot
                    {
                        var y = (short)Pop();
                        var x = (short)Pop();
                        SetCursorHotspot(new Point(x, y));
                        UpdateCursor();
                    }
                    break;
                case 0x9C:              // SO_CHARSET_SET
                    InitCharset(Pop());
                    break;
                case 0x9D:              // SO_CHARSET_COLOR
                    var args = GetStackList(16);
                    for (var i = 0; i < args.Length; i++)
                        CharsetColorMap[i] = _charsetData[String[1].Default.Charset][i] = (byte)args[i];
                    break;
                case 0xD6:              // SO_CURSOR_TRANSPARENT Set cursor transparent color
                    SetCursorTransparency(Pop());
                    break;
                default:
                    throw new NotSupportedException(string.Format("CursorCommand: default case {0:X2}", subOp));
            }

            Variables[VariableCursorState.Value] = _cursor.State;
            Variables[VariableUserPut.Value] = _userPut;

        }

        protected void SetCursorFromImg(int obj, int room, int index)
        {
            if (room == - 1)
                room = GetObjectRoom(obj);

            var roomD = (CurrentRoom != room) ? _resManager.GetRoom((byte)room) : roomData;
            var ob = roomD.Objects.First(o => o.Number == obj);
            var img = ob.Images[index - 1];

            SetCursorHotspot(ob.Hotspots[index - 1]);
            var pixels = img.IsBomp ? BompDrawData.DecompressBomp(img.Data, ob.Width, ob.Height) : GrabPixels(img, ob.Width, ob.Height);
            SetCursorFromBuffer(pixels, ob.Width, ob.Height);
        }

        byte[] GrabPixels(ImageData im, int w, int h)
        {
            // Backup the screen content
            var backup = (byte[])MainVirtScreen.Surfaces[0].Pixels.Clone();

            // Do some drawing
            DrawBox(0, 0, w - 1, h - 1, 0xFF);

            MainVirtScreen.HasTwoBuffers = false;
            Gdi.IsZBufferEnabled = false;
            Gdi.DrawBitmap(im, MainVirtScreen, _screenStartStrip, 0, w, h, 0, w / 8, MainVirtScreen.Width, DrawBitmaps.None);
            MainVirtScreen.HasTwoBuffers = true;
            Gdi.IsZBufferEnabled = true;

            // Grab the data we just drew and setup the cursor with it
            var pixels = Capture(MainVirtScreen, MainVirtScreen.XStart, 0, w, h);

            // Restore the screen content
            Array.Copy(backup, MainVirtScreen.Surfaces[0].Pixels, backup.Length);

            return pixels;
        }

        protected override void SetDefaultCursor()
        {
            _cursor.Animate = false;
            SetCursorHotspot(new Point(7, 6));
            SetCursorFromBuffer(default_v6_cursor, 16, 13);
        }

        protected void SetCursorHotspot(Point pos)
        {
            _cursor.Hotspot = pos;
        }

        void SetCursorFromBuffer(byte[] data, int width, int height)
        {
            _cursor.Width = width;
            _cursor.Height = height;
            _cursor.Animate = false;

            _cursorData = data;
            _gfxManager.SetCursor(data, _cursor.Width, _cursor.Height, _cursor.Hotspot);
        }

        protected void SetCursorTransparency(int a)
        {
            for (var i = 0; i < _cursorData.Length; i++)
                if (_cursorData[i] == a)
                    _cursorData[i] = 0xFF;

            UpdateCursor();
        }

        void UpdateCursor()
        {
            _gfxManager.SetCursor(_cursorData, _cursor.Width, _cursor.Height, _cursor.Hotspot);
        }

        static byte[] Capture(VirtScreen screen, int x, int y, int w, int h)
        {
            var pixels = new byte[w * h];
            var nav = new PixelNavigator(screen.Surfaces[0]);
            nav.GoTo(x, y);
            for (int height = 0; height < h; height++)
            {
                for (int width = 0; width < w; width++)
                {
                    pixels[height * w + width] = nav.Read();
                    nav.OffsetX(1);
                }
                nav.Offset(-w, 1);
            }
            return pixels;
        }

        protected void GrabCursor(int x, int y, int w, int h)
        {
            var vs = FindVirtScreen(y);
            var pixels = Capture(vs, x, y - vs.TopLine, w, h);
            SetCursorFromBuffer(pixels, w, h);
        }
    }
}

