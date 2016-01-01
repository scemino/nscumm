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

            _numChars = _resourceData.ReadSci11EndianUInt16(2);
            _fontHeight = (byte)_resourceData.ReadSci11EndianUInt16(4);
            _chars = new Charinfo[_numChars];
            // filling info for every char
            for (var i = 0; i < _numChars; i++)
            {
                _chars[i].offset = (short)_resourceData.ReadSci11EndianUInt16(6 + i * 2);
                _chars[i].width = _resourceData[_chars[i].offset];
                _chars[i].height = _resourceData[_chars[i].offset + 1];
            }
        }
    }
}