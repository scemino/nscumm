//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// HunkPalette represents a raw palette resource
    /// </summary>
    internal class HunkPalette
    {
        /**
		 * The size of the HunkPalette header.
		 */
        private const int kHunkPaletteHeaderSize = 13;

        /**
         * The size of a palette entry header.
         */
        private const int kEntryHeaderSize = 22;

        /**
         * The offset of the hunk palette version
         * within the palette entry header.
         */
        private const int kEntryVersionOffset = 18;

        /**
         * The header for a palette inside the
         * HunkPalette.
    	 */

        private class EntryHeader
        {
            /**
             * The start color.
             */
            public byte startColor;

            /**
             * The number of palette colors in this
             * entry.
             */
            public ushort numColors;

            /**
             * The default `used` flag.
             */
            public byte used;

            /**
             * Whether or not all palette entries
             * share the same `used` value in
             * `defaultFlag`.
             */
            public bool sharedUsed;

            /**
             * The palette version.
             */
            public uint version;
        }

        /**
         * The version number from the last time this
         * palette was submitted to GfxPalette32.
         */
        private uint _version;

        /**
         * The number of palettes stored in the hunk
         * palette. In SCI32 games this is always 1.
         */
        private readonly byte _numPalettes;

        /**
         * The raw palette data for this hunk palette.
         */
        private BytePtr _data;


        public HunkPalette(BytePtr rawPalette)
        {
            // NOTE: The header size in palettes is garbage. In at least KQ7 2.00b and
            // Phant1, the 999.pal sets this value to 0. In most other palettes it is
            // set to 14, but the *actual* size of the header structure used in SSCI is
            // 13, which is reflected by `kHunkPaletteHeaderSize`.
            // _headerSize(rawPalette[0]),
            _numPalettes = rawPalette[10];
            System.Diagnostics.Debug.Assert(_numPalettes == 0 || _numPalettes == 1);
            if (_numPalettes != 0)
            {
                _data = rawPalette;
                _version = GetEntryHeader().version;
            }
        }

        /**
         * Gets the version of the palette.
         */

        public uint GetVersion()
        {
            return _version;
        }

        public void SetVersion(uint version)
        {
            if (_numPalettes != _data[10])
            {
                DebugHelper.Error("Invalid HunkPalette");
            }

            if (_numPalettes != 0)
            {
                EntryHeader header = GetEntryHeader();
                if (header.version != _version)
                {
                    DebugHelper.Error("Invalid HunkPalette");
                }

                var ptr = GetPalPointer();
                ptr.Data.WriteSci11EndianUInt32(ptr.Offset + kEntryVersionOffset, version);
                _version = version;
            }
        }

        public Palette ToPalette()
        {
            Palette outPalette = new Palette();

            for (var i = 0; i < outPalette.colors.Length; ++i)
            {
                outPalette.colors[i].used = 0;
                outPalette.colors[i].r = 0;
                outPalette.colors[i].g = 0;
                outPalette.colors[i].b = 0;
            }

            if (_numPalettes != 0)
            {
                EntryHeader header = GetEntryHeader();
                var data = new BytePtr(GetPalPointer(), kEntryHeaderSize);

                short end = (short) (header.startColor + header.numColors);
                System.Diagnostics.Debug.Assert(end <= 256);

                if (header.sharedUsed)
                {
                    for (short i = header.startColor; i < end; ++i)
                    {
                        outPalette.colors[i].used = header.used;
                        outPalette.colors[i].r = data.Value;
                        data.Offset++;
                        outPalette.colors[i].g = data.Value;
                        data.Offset++;
                        outPalette.colors[i].b = data.Value;
                        data.Offset++;
                    }
                }
                else
                {
                    for (short i = header.startColor; i < end; ++i)
                    {
                        outPalette.colors[i].used = data.Value;
                        data.Offset++;
                        outPalette.colors[i].r = data.Value;
                        data.Offset++;
                        outPalette.colors[i].g = data.Value;
                        data.Offset++;
                        outPalette.colors[i].b = data.Value;
                        data.Offset++;
                    }
                }
            }

            return outPalette;
        }

        private EntryHeader GetEntryHeader()
        {
            var data = GetPalPointer();
            var header = new EntryHeader
            {
                startColor = data[10],
                numColors = data.Data.ReadSci11EndianUInt16(data.Offset + 14),
                used = data[16],
                sharedUsed = data[17] != 0,
                version = data.Data.ReadSci11EndianUInt32(data.Offset + kEntryVersionOffset)
            };

            return header;
        }

        /**
         * Returns a pointer to the palette data within
         * the hunk palette.
         */

        private BytePtr GetPalPointer()
        {
            return new BytePtr(_data, kHunkPaletteHeaderSize + 2 * _numPalettes);
        }
    }
}