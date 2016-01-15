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


using NScumm.Core.Common;
using System;

namespace NScumm.Sci.Graphics
{
    internal class GfxFont
    {
        public virtual int ResourceId { get { return 0; } }
        public virtual byte Height { get { return 0; } }

        public virtual bool IsDoubleByte(ushort chr) { return false; }
        public virtual byte GetCharWidth(ushort chr) { return 0; }
        public virtual void Draw(ushort chr, short top, short left, byte color, bool greyedOutput) { }
        public virtual void DrawToBuffer(ushort chr, short top, short left, byte color, bool greyedOutput, byte[] buffer, short width, short height) { }
    }

    /// <summary>
    /// Font class, handles loading of font resources and drawing characters to screen
    ///  every font resource has its own instance of this class
    /// </summary>
    internal class GfxFontFromResource : GfxFont
    {
        private ResourceManager _resMan;
        private GfxScreen _screen;

        private ResourceManager.ResourceSource.Resource _resource;
        private int _resourceId;
        private byte[] _resourceData;

        struct Charinfo
        {
            public byte width;
            public byte height;
            public short offset;
        }

        private byte _fontHeight;
        private ushort _numChars;
        private Charinfo[] _chars;

        public override int ResourceId
        {
            get
            {
                return _resourceId;
            }
        }

        public override byte Height
        {
            get
            {
                return _fontHeight;
            }
        }

        public GfxFontFromResource(ResourceManager resMan, GfxScreen screen, int resourceId)
        {
            _resMan = resMan;
            _screen = screen;
            _resourceId = resourceId;

            // Workaround: lsl1sci mixes its own internal fonts with the global
            // SCI ones, so we translate them here, by removing their extra bits
            if (resMan.TestResource(new ResourceId(ResourceType.Font, (ushort)resourceId)) == null)
                resourceId = resourceId & 0x7ff;

            _resource = resMan.FindResource(new ResourceId(ResourceType.Font, (ushort)resourceId), true);
            if (_resource == null)
            {
                throw new System.InvalidOperationException($"font resource {resourceId} not found");
            }
            _resourceData = _resource.data;

            _numChars = _resourceData.ReadSci32EndianUInt16(2);
            _fontHeight = (byte)_resourceData.ReadSci32EndianUInt16(4);
            _chars = new Charinfo[_numChars];
            // filling info for every char
            for (var i = 0; i < _numChars; i++)
            {
                _chars[i].offset = (short)_resourceData.ReadSci32EndianUInt16(6 + i * 2);
                _chars[i].width = _resourceData[_chars[i].offset];
                _chars[i].height = _resourceData[_chars[i].offset + 1];
            }
        }

        public override byte GetCharWidth(ushort chr)
        {
            return (byte)(chr < _numChars ? _chars[chr].width : 0);
        }

        public override void Draw(ushort chr, short top, short left, byte color, bool greyedOutput)
        {
            // Make sure we're comparing against the correct dimensions
            // If the font we're drawing is already upscaled, make sure we use the full screen width/height
            ushort screenWidth = _screen.FontIsUpscaled ? _screen.DisplayWidth : _screen.Width;
            ushort screenHeight = _screen.FontIsUpscaled ? _screen.DisplayHeight : _screen.Height;

            int charWidth = Math.Min(GetCharWidth(chr), screenWidth - left);
            int charHeight = Math.Min(GetCharHeight(chr), screenHeight - top);
            byte b = 0, mask = 0xFF;
            int y = 0;
            short greyedTop = top;

            var pIn = GetCharData(chr);
            for (int i = 0; i < charHeight; i++, y++)
            {
                if (greyedOutput)
                    mask = (byte)((((greyedTop++) % 2) != 0) ? 0xAA : 0x55);
                for (int done = 0; done < charWidth; done++)
                {
                    if ((done & 7) == 0) // fetching next data byte
                        b = (byte)(pIn.Increment() & mask);
                    if ((b & 0x80) != 0) // if MSB is set - paint it
                        _screen.PutFontPixel(top, (short)(left + done), (short)y, color);
                    b = (byte)(b << 1);
                }
            }
        }

        private byte GetCharHeight(ushort chr)
        {
            return (byte)(chr < _numChars ? _chars[chr].height : 0);
        }

        private ByteAccess GetCharData(ushort chr)
        {
            return chr < _numChars ? new ByteAccess(_resourceData, _chars[chr].offset + 2) : null;
        }
    }
}