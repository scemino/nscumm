//
//  SmushFont.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

using System.Diagnostics;
using System.Text;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm.Smush
{
    class SmushFont: NutRenderer
    {
        public SmushFont(ScummEngine vm, string filename, bool use_original_colors, bool new_colors)
            : base(vm, filename)
        {
            _color = -1;
            _new_colors = new_colors;
            _original = use_original_colors;
        }

        public byte Color
        {
            get { return (byte)_color; }
            set { _color = value; }
        }

        public void DrawString(string str, byte[] buffer, int dst_width, int dst_height, int x, int y, bool center)
        {
//            Debug.WriteLine("SmushFont::drawString({0}, {1}, {2}, {3})", str, x, y, center);

            foreach (var line in str.Split('\n'))
            {
                DrawSubstring(line, buffer, dst_width, center ? (x - GetStringWidth(line) / 2) : x, y);
                y += GetStringHeight(line);
            }
        }

        public void DrawStringWrap(string  str, byte[] buffer, int dst_width, int dst_height, int x, int y, int left, int right, bool center)
        {
//            Debug.WriteLine("SmushFont::drawStringWrap({0}, {1}, {2}, {3}, {4}, {5})", str, x, y, left, right, center);

            var width = right - left;

            var words = str.Split(new char[]{ ' ', '\t', '\r', '\n' });
            Debug.Assert(words.Length < MaxWords);

            int i = 0, maxWidth = 0, height = 0, line_count = 0;

            var subStrings = new string[MaxWords];
            var subStrWidths = new int[MaxWords];
            var spaceWidth = GetCharWidth(' ');

            i = 0;
            while (i < words.Length)
            {
                var subStr = words[i++];
                var lineStr = new StringBuilder(subStr);
                var subStrWidth = GetStringWidth(subStr);

                while (i < words.Length)
                {
                    int wordWidth = GetStringWidth(words[i]);
                    if ((subStrWidth + spaceWidth + wordWidth) >= width)
                        break;
                    subStrWidth += wordWidth + spaceWidth;
                    lineStr.Append(' ').Append(words[i]);
                    i++;
                }

                subStrings[line_count] = lineStr.ToString();
                subStrWidths[line_count++] = subStrWidth;
                if (maxWidth < subStrWidth)
                    maxWidth = subStrWidth;
                height += GetStringHeight(subStr);
            }

            if (y > dst_height - height)
            {
                y = dst_height - height;
            }

            if (center)
            {
                maxWidth = (maxWidth + 1) / 2;
                x = left + width / 2;

                if (x < left + maxWidth)
                    x = left + maxWidth;
                if (x > right - maxWidth)
                    x = right - maxWidth;

                for (i = 0; i < line_count; i++)
                {
                    DrawSubstring(subStrings[i], buffer, dst_width, x - subStrWidths[i] / 2, y);
                    y += GetStringHeight(subStrings[i]);
                }
            }
            else
            {
                if (x > dst_width - maxWidth)
                    x = dst_width - maxWidth;

                for (i = 0; i < line_count; i++)
                {
                    DrawSubstring(subStrings[i], buffer, dst_width, x, y);
                    y += GetStringHeight(subStrings[i]);
                }
            }
        }

        void DrawSubstring(string str, byte[] buffer, int dst_width, int x, int y)
        {
            // This happens in the Full Throttle intro. I don't know if our
            // text-drawing functions are buggy, or if this function is supposed
            // to have to check for it.
            if (x < 0)
                x = 0;

            for (var i = 0; i < str.Length; i++)
            {
                x += DrawChar(buffer, dst_width, x, y, str[i]);
            }
        }

        int DrawChar(byte[] buffer, int dst_width, int x, int y, char chr)
        {
            chr = chr >= _chars.Length ? ' ' : chr;
            int w = _chars[chr].Width;
            int h = _chars[chr].Height;
            var src = UnpackChar(chr);
            var srcPos = 0;
            var dst = buffer;
            var dstPos = dst_width * y + x;

            Debug.Assert(dst_width == _vm.ScreenWidth);

            if (_original)
            {
                for (int j = 0; j < h; j++)
                {
                    for (int i = 0; i < w; i++)
                    {
                        byte value = src[srcPos++];
                        if (value != _chars[chr].Transparency)
                            dst[dstPos + i] = value;
                    }
                    dstPos += dst_width;
                }
            }
            else
            {
                var color = (_color != -1) ? (sbyte)_color : (sbyte)1;
                if (_new_colors)
                {
                    for (int j = 0; j < h; j++)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            sbyte value = (sbyte)src[srcPos++];
                            if (value == -color)
                            {
                                dst[dstPos + i] = 0xFF;
                            }
                            else if (value == -31)
                            {
                                dst[dstPos + i] = 0;
                            }
                            else if (value != _chars[chr].Transparency)
                            {
                                dst[dstPos + i] = (byte)value;
                            }
                        }
                        dstPos += dst_width;
                    }
                }
                else
                {
                    for (int j = 0; j < h; j++)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            sbyte value = (sbyte)src[srcPos++];
                            if (value == 1)
                            {
                                dst[dstPos + i] = (byte)color;
                            }
                            else if (value != _chars[chr].Transparency)
                            {
                                dst[dstPos + i] = 0;
                            }
                        }
                        dstPos += dst_width;
                    }
                }
            }
            return w;
        }

        int GetStringWidth(string str)
        {
            Debug.Assert(str != null);

            int width = 0;
            foreach (var c in str)
            {
                width += GetCharWidth(c);
            }
            return width;
        }

        int GetStringHeight(string str)
        {
            Debug.Assert(str != null);

            int height = 0;
            foreach (var c in str)
            {
                int charHeight = GetCharHeight(c);
                if (height < charHeight)
                    height = charHeight;
            }
            return height;
        }

        const int MaxWords = 60;

        short _color;
        bool _new_colors;
        bool _original;
    }
}

