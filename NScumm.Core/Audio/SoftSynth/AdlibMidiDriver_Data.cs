//
//  AdLibMidiDriver_Data.cs
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

namespace NScumm.Core.Audio.SoftSynth
{
    public partial class AdlibMidiDriver
    {
        static readonly byte[] gmPercussionInstrumentMap =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C,
            0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0xFF, 0xFF, 0x17, 0x18, 0x19, 0x1A,
            0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x21, 0x22, 0x23, 0xFF, 0xFF,
            0x24, 0x25, 0xFF, 0xFF, 0xFF, 0x26, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        static readonly AdLibInstrument[] gmPercussionInstruments =
        {
            new AdLibInstrument(new byte[]{ 0x1A, 0x3F, 0x15, 0x05, 0x7C, 0x02, 0x21, 0x2B, 0xE4, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x06),
            new AdLibInstrument(new byte[]{ 0x11, 0x12, 0x04, 0x07, 0x7C, 0x02, 0x23, 0x0B, 0xE5, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
            new AdLibInstrument(new byte[]{ 0x0A, 0x3F, 0x0B, 0x01, 0x7C, 0x1F, 0x1C, 0x46, 0xD0, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x01),
            new AdLibInstrument(new byte[]{ 0x00, 0x3F, 0x0F, 0x00, 0x7C, 0x10, 0x12, 0x07, 0x00, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0x0F, 0x3F, 0x0B, 0x00, 0x7C, 0x1F, 0x0F, 0x19, 0xD0, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0x00, 0x3F, 0x1F, 0x00, 0x7E, 0x1F, 0x16, 0x07, 0x00, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0x12, 0x3F, 0x05, 0x06, 0x7C, 0x03, 0x1F, 0x4A, 0xD9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0xCF, 0x7F, 0x08, 0xFF, 0x7E, 0x00, 0xC7, 0x2D, 0xF7, 0x73, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0x12, 0x3F, 0x05, 0x06, 0x7C, 0x43, 0x21, 0x0C, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0xCF, 0x7F, 0x08, 0xCF, 0x7E, 0x00, 0x45, 0x2A, 0xF8, 0x4B, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0C),
            new AdLibInstrument(new byte[]{ 0x12, 0x3F, 0x06, 0x17, 0x7C, 0x03, 0x27, 0x0B, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0xCF, 0x7F, 0x08, 0xCD, 0x7E, 0x00, 0x40, 0x1A, 0x69, 0x63, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0C),
            new AdLibInstrument(new byte[]{ 0x13, 0x3F, 0x05, 0x06, 0x7C, 0x03, 0x17, 0x0A, 0xD9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0x15, 0x3F, 0x05, 0x06, 0x7C, 0x03, 0x21, 0x0C, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x2B, 0xFB, 0x7E, 0xC0, 0x1E, 0x1A, 0xCA, 0x7F, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x10),
            new AdLibInstrument(new byte[]{ 0x17, 0x3F, 0x04, 0x09, 0x7C, 0x03, 0x22, 0x0D, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x0F, 0x5E, 0x7C, 0xC6, 0x13, 0x00, 0xCA, 0x7F, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x7E, 0x9D, 0x7C, 0xC8, 0xC0, 0x0A, 0xBA, 0x74, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x06),
            new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x4D, 0x9F, 0x7C, 0xC6, 0x00, 0x08, 0xDA, 0x5B, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
            new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x5D, 0xAA, 0x7A, 0xC0, 0xA4, 0x67, 0x99, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x4A, 0xFD, 0x7C, 0xCF, 0x00, 0x59, 0xEA, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0x0F, 0x18, 0x0A, 0xFA, 0x57, 0x06, 0x07, 0x06, 0x39, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x2B, 0xFC, 0x7C, 0xCC, 0xC6, 0x0B, 0xEA, 0x7F, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x10),
            new AdLibInstrument(new byte[]{ 0x05, 0x1A, 0x04, 0x00, 0x7C, 0x12, 0x10, 0x0C, 0xEA, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x07),
            new AdLibInstrument(new byte[]{ 0x04, 0x19, 0x04, 0x00, 0x7C, 0x12, 0x10, 0x2C, 0xEA, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
            new AdLibInstrument(new byte[]{ 0x04, 0x0A, 0x04, 0x00, 0x6C, 0x01, 0x07, 0x0D, 0xFA, 0x74, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x07),
            new AdLibInstrument(new byte[]{ 0x15, 0x14, 0x05, 0x00, 0x7D, 0x01, 0x07, 0x5C, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
            new AdLibInstrument(new byte[]{ 0x10, 0x10, 0x05, 0x08, 0x7C, 0x01, 0x08, 0x0D, 0xEA, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
            new AdLibInstrument(new byte[]{ 0x11, 0x00, 0x06, 0x87, 0x7F, 0x02, 0x40, 0x09, 0x59, 0x68, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x08),
            new AdLibInstrument(new byte[]{ 0x13, 0x26, 0x04, 0x6A, 0x7F, 0x01, 0x00, 0x08, 0x5A, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x08),
            new AdLibInstrument(new byte[]{ 0xCF, 0x4E, 0x0C, 0xAA, 0x50, 0xC4, 0x00, 0x18, 0xF9, 0x54, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0xCF, 0x4E, 0x0C, 0xAA, 0x50, 0xC3, 0x00, 0x18, 0xF8, 0x54, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0xCB, 0x3F, 0x8F, 0x00, 0x7E, 0xC5, 0x00, 0x98, 0xD6, 0x5F, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0D),
            new AdLibInstrument(new byte[]{ 0x0C, 0x18, 0x87, 0xB3, 0x7F, 0x19, 0x10, 0x55, 0x75, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0x05, 0x11, 0x15, 0x00, 0x64, 0x02, 0x08, 0x08, 0x00, 0x5C, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0x04, 0x08, 0x15, 0x00, 0x48, 0x01, 0x08, 0x08, 0x00, 0x60, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
            new AdLibInstrument(new byte[]{ 0xDA, 0x00, 0x53, 0x30, 0x68, 0x07, 0x1E, 0x49, 0xC4, 0x7E, 0x03, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
            new AdLibInstrument(new byte[]{ 0x1C, 0x00, 0x07, 0xBC, 0x6C, 0x0C, 0x14, 0x0B, 0x6A, 0x7E, 0x0B, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
            new AdLibInstrument(new byte[]{ 0x0A, 0x0E, 0x7F, 0x00, 0x7D, 0x13, 0x20, 0x28, 0x03, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00)
        };

        static readonly AdLibInstrument[] gmInstruments =
            {
                // 0x00
                new AdLibInstrument(new byte []{ 0xC2, 0xC5, 0x2B, 0x99, 0x58, 0xC2, 0x1F, 0x1E, 0xC8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x23),
                new AdLibInstrument(new byte []{ 0x22, 0x53, 0x0E, 0x8A, 0x30, 0x14, 0x06, 0x1D, 0x7A, 0x5C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x06, 0x00, 0x1C, 0x79, 0x40, 0x02, 0x00, 0x4B, 0x79, 0x58, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC2, 0x89, 0x2A, 0x89, 0x49, 0xC2, 0x16, 0x1C, 0xB8, 0x7C, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x23),
                new AdLibInstrument(new byte []{ 0xC2, 0x17, 0x3D, 0x6A, 0x00, 0xC4, 0x2E, 0x2D, 0xC9, 0x20, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x06, 0x1E, 0x1C, 0x99, 0x00, 0x02, 0x3A, 0x4C, 0x79, 0x00, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x84, 0x40, 0x3B, 0x5A, 0x6F, 0x81, 0x0E, 0x3B, 0x5A, 0x7F, 0x0B, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x84, 0x40, 0x3B, 0x5A, 0x63, 0x81, 0x00, 0x3B, 0x5A, 0x7F, 0x01, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x8C, 0x80, 0x05, 0xEA, 0x59, 0x82, 0x0A, 0x3C, 0xAA, 0x64, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x85, 0x40, 0x0D, 0xEC, 0x71, 0x84, 0x58, 0x3E, 0xCB, 0x7C, 0x01, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x8A, 0xC0, 0x0C, 0xDC, 0x50, 0x88, 0x58, 0x3D, 0xDA, 0x7C, 0x01, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC9, 0x40, 0x2B, 0x78, 0x42, 0xC2, 0x04, 0x4C, 0x8A, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x1A),
                new AdLibInstrument(new byte []{ 0x2A, 0x0E, 0x17, 0x89, 0x28, 0x22, 0x0C, 0x1B, 0x09, 0x70, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE7, 0x9B, 0x08, 0x08, 0x26, 0xE2, 0x06, 0x0A, 0x08, 0x70, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC5, 0x05, 0x00, 0xFC, 0x40, 0x84, 0x00, 0x00, 0xDC, 0x50, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x86, 0x40, 0x5D, 0x5A, 0x41, 0x81, 0x00, 0x0B, 0x5A, 0x7F, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x10
                new AdLibInstrument(new byte []{ 0xED, 0x00, 0x7B, 0xC8, 0x40, 0xE1, 0x99, 0x4A, 0xE9, 0x7E, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE8, 0x4F, 0x3A, 0xD7, 0x7C, 0xE2, 0x97, 0x49, 0xF9, 0x7D, 0x05, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x10, 0x2F, 0xF7, 0x7D, 0xF3, 0x45, 0x8F, 0xC7, 0x62, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x01, 0x8C, 0x9F, 0xDA, 0x70, 0xE4, 0x50, 0x9F, 0xDA, 0x6A, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x08, 0xD5, 0x9D, 0xA5, 0x45, 0xE2, 0x3F, 0x9F, 0xD6, 0x49, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE5, 0x0F, 0x7D, 0xB8, 0x2E, 0xA2, 0x0F, 0x7C, 0xC7, 0x61, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x2A, 0x9F, 0xDB, 0x01, 0xE1, 0x04, 0x8F, 0xD7, 0x62, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x88, 0x9C, 0x50, 0x64, 0xE2, 0x18, 0x70, 0xC4, 0x7C, 0x0B, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x02, 0xA3, 0x0D, 0xDA, 0x01, 0xC2, 0x35, 0x5D, 0x58, 0x00, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x18),
                new AdLibInstrument(new byte []{ 0x42, 0x55, 0x3E, 0xEB, 0x24, 0xD4, 0x08, 0x0D, 0xA9, 0x71, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x18),
                new AdLibInstrument(new byte []{ 0xC2, 0x00, 0x2B, 0x17, 0x51, 0xC2, 0x1E, 0x4D, 0x97, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x19),
                new AdLibInstrument(new byte []{ 0xC6, 0x01, 0x2D, 0xA7, 0x44, 0xC2, 0x06, 0x0E, 0xA7, 0x79, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC2, 0x0C, 0x06, 0x06, 0x55, 0xC2, 0x3F, 0x09, 0x86, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0A),
                new AdLibInstrument(new byte []{ 0xC2, 0x2E, 0x4F, 0x77, 0x00, 0xC4, 0x08, 0x0E, 0x98, 0x59, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC2, 0x30, 0x4F, 0xCA, 0x01, 0xC4, 0x0D, 0x0E, 0xB8, 0x7F, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC4, 0x29, 0x4F, 0xCA, 0x03, 0xC8, 0x0D, 0x0C, 0xB7, 0x7D, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0B),
                // 0x20
                new AdLibInstrument(new byte []{ 0xC2, 0x40, 0x3C, 0x96, 0x58, 0xC4, 0xDE, 0x0E, 0xC7, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x20),
                new AdLibInstrument(new byte []{ 0x31, 0x13, 0x2D, 0xD7, 0x3C, 0xE2, 0x18, 0x2E, 0xB8, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x22, 0x86, 0x0D, 0xD7, 0x50, 0xE4, 0x18, 0x5E, 0xB8, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x28),
                new AdLibInstrument(new byte []{ 0xF2, 0x0A, 0x0D, 0xD7, 0x40, 0xE4, 0x1F, 0x5E, 0xB8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x09, 0x4B, 0xD6, 0x48, 0xE4, 0x1F, 0x1C, 0xB8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x28),
                new AdLibInstrument(new byte []{ 0x62, 0x11, 0x0C, 0xE6, 0x3C, 0xE4, 0x1F, 0x0C, 0xC8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x12, 0x3D, 0xE6, 0x34, 0xE4, 0x1F, 0x7D, 0xB8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x13, 0x3D, 0xE6, 0x34, 0xE4, 0x1F, 0x5D, 0xB8, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xA2, 0x40, 0x5D, 0xBA, 0x3F, 0xE2, 0x00, 0x8F, 0xD8, 0x79, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x40, 0x3D, 0xDA, 0x3B, 0xE1, 0x00, 0x7E, 0xD8, 0x7A, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x00, 0x6D, 0xFA, 0x5D, 0xE2, 0x00, 0x8F, 0xC8, 0x79, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0x4E, 0xDB, 0x4A, 0xE3, 0x18, 0x6F, 0xE9, 0x7E, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0x4E, 0xDB, 0x66, 0xE2, 0x00, 0x7F, 0xE9, 0x7E, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x02, 0x0F, 0x66, 0xAA, 0x51, 0x02, 0x64, 0x29, 0xF9, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte []{ 0x16, 0x4A, 0x04, 0xBA, 0x39, 0xC2, 0x58, 0x2D, 0xCA, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte []{ 0x02, 0x00, 0x01, 0x7A, 0x79, 0x02, 0x3F, 0x28, 0xEA, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                // 0x30
                new AdLibInstrument(new byte []{ 0x62, 0x53, 0x9C, 0xBA, 0x31, 0x62, 0x5B, 0xAD, 0xC9, 0x55, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x40, 0x6E, 0xDA, 0x49, 0xE2, 0x13, 0x8F, 0xF9, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x40, 0x8F, 0xFA, 0x50, 0xF2, 0x04, 0x7F, 0xFA, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0xA0, 0xCE, 0x5B, 0x02, 0xE2, 0x32, 0x7F, 0xFB, 0x3D, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE6, 0x80, 0x9C, 0x99, 0x42, 0xE2, 0x04, 0x7D, 0x78, 0x60, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xEA, 0xA0, 0xAC, 0x67, 0x02, 0xE2, 0x00, 0x7C, 0x7A, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE7, 0x94, 0xAD, 0xB7, 0x03, 0xE2, 0x00, 0x7C, 0xBA, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC3, 0x3F, 0x4B, 0xE9, 0x7E, 0xC1, 0x3F, 0x9B, 0xF9, 0x7F, 0x0B, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x06),
                new AdLibInstrument(new byte []{ 0xB2, 0x20, 0xAD, 0xE9, 0x00, 0x62, 0x05, 0x8F, 0xC8, 0x68, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x00, 0x8F, 0xFB, 0x50, 0xF6, 0x47, 0x8F, 0xE9, 0x68, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x00, 0xAF, 0x88, 0x58, 0xF2, 0x54, 0x6E, 0xC9, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x2A, 0x9F, 0x98, 0x01, 0xE2, 0x84, 0x4E, 0x78, 0x6C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x02, 0x9F, 0xB8, 0x48, 0x22, 0x89, 0x9F, 0xE8, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x2A, 0x7F, 0xB8, 0x01, 0xE4, 0x00, 0x0D, 0xC5, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x28, 0x8E, 0xE8, 0x01, 0xF2, 0x00, 0x4D, 0xD6, 0x7D, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x23, 0x8F, 0xEA, 0x00, 0xF2, 0x00, 0x5E, 0xD9, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x40
                new AdLibInstrument(new byte []{ 0xB4, 0x26, 0x6E, 0x98, 0x01, 0x62, 0x00, 0x7D, 0xC8, 0x7D, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x2E, 0x20, 0xD9, 0x01, 0xF2, 0x0F, 0x90, 0xF8, 0x78, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x28, 0x7E, 0xF8, 0x01, 0xE2, 0x23, 0x8E, 0xE8, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xB8, 0x28, 0x9E, 0x98, 0x01, 0x62, 0x00, 0x3D, 0xC8, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x00, 0x8E, 0xC9, 0x3D, 0xE6, 0x00, 0x7E, 0xD8, 0x68, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x00, 0x5F, 0xF9, 0x48, 0xE6, 0x98, 0x8F, 0xF8, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x0C, 0x6E, 0xD8, 0x3D, 0x2A, 0x06, 0x7D, 0xD8, 0x58, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0x7E, 0x89, 0x38, 0xE6, 0x84, 0x80, 0xF8, 0x68, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x80, 0x6C, 0xD9, 0x30, 0xE2, 0x00, 0x8D, 0xC8, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x80, 0x88, 0x48, 0x40, 0xE2, 0x0A, 0x7D, 0xA8, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0x77, 0xC5, 0x54, 0xE2, 0x00, 0x9E, 0xD7, 0x70, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x80, 0x86, 0xB9, 0x64, 0xE2, 0x05, 0x9F, 0xD7, 0x78, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x00, 0x68, 0x68, 0x56, 0xE2, 0x08, 0x9B, 0xB3, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0xA6, 0x87, 0x41, 0xE2, 0x0A, 0x7E, 0xC9, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x80, 0x9A, 0xB8, 0x48, 0xE2, 0x00, 0x9E, 0xF9, 0x60, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x80, 0x8E, 0x64, 0x68, 0xE2, 0x28, 0x6F, 0x73, 0x7C, 0x01, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x50
                new AdLibInstrument(new byte []{ 0xE8, 0x00, 0x7D, 0x99, 0x54, 0xE6, 0x80, 0x80, 0xF8, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE6, 0x00, 0x9F, 0xB9, 0x6D, 0xE1, 0x00, 0x8F, 0xC8, 0x7D, 0x02, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0x09, 0x68, 0x4A, 0xE2, 0x2B, 0x9E, 0xF3, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC4, 0x00, 0x99, 0xE8, 0x3B, 0xE2, 0x25, 0x6F, 0x93, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE6, 0x00, 0x6F, 0xDA, 0x69, 0xE2, 0x05, 0x2F, 0xD8, 0x6A, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xEC, 0x60, 0x9D, 0xC7, 0x00, 0xE2, 0x21, 0x7F, 0xC9, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE3, 0x00, 0x0F, 0xF7, 0x7D, 0xE1, 0x3F, 0x0F, 0xA7, 0x01, 0x0D, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0xA9, 0x0F, 0xA8, 0x02, 0xE2, 0x3C, 0x5F, 0xDA, 0x3C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE8, 0x40, 0x0D, 0x89, 0x7D, 0xE2, 0x17, 0x7E, 0xD9, 0x7C, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0xDF, 0x8A, 0x56, 0xE2, 0x5E, 0xCF, 0xBA, 0x7E, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x00, 0x0B, 0x68, 0x60, 0xE2, 0x01, 0x9E, 0xB8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xEA, 0x00, 0xAE, 0xAB, 0x49, 0xE2, 0x00, 0xAE, 0xBA, 0x6C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xEB, 0x80, 0x8C, 0xCB, 0x3A, 0xE2, 0x86, 0xAF, 0xCA, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE5, 0x40, 0xDB, 0x3B, 0x3C, 0xE2, 0x80, 0xBE, 0xCA, 0x71, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0x9E, 0xAA, 0x3D, 0xE1, 0x43, 0x0F, 0xBA, 0x7E, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE7, 0x40, 0xEC, 0xCA, 0x44, 0xE2, 0x03, 0xBF, 0xBA, 0x66, 0x02, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x60
                new AdLibInstrument(new byte []{ 0xEA, 0x00, 0x68, 0xB8, 0x48, 0xE2, 0x0A, 0x8E, 0xB8, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x61, 0x00, 0xBE, 0x99, 0x7E, 0xE3, 0x40, 0xCF, 0xCA, 0x7D, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xCD, 0x00, 0x0B, 0x00, 0x48, 0xC2, 0x58, 0x0C, 0x00, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x1C),
                new AdLibInstrument(new byte []{ 0xE2, 0x00, 0x0E, 0x00, 0x52, 0xE2, 0x58, 0x5F, 0xD0, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xCC, 0x00, 0x7D, 0xDA, 0x40, 0xC2, 0x00, 0x5E, 0x9B, 0x58, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE9, 0xC0, 0xEE, 0xD8, 0x43, 0xE2, 0x05, 0xDD, 0xAA, 0x70, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xDA, 0x00, 0x8F, 0xAC, 0x4A, 0x22, 0x05, 0x8D, 0x8A, 0x75, 0x02, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x8A, 0xCB, 0x7A, 0x74, 0xE6, 0x56, 0xAF, 0xDB, 0x70, 0x02, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC2, 0x41, 0xAC, 0x5B, 0x5B, 0xC2, 0x80, 0x0D, 0xCB, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x12),
                new AdLibInstrument(new byte []{ 0x75, 0x00, 0x0E, 0xCB, 0x5A, 0xE2, 0x1E, 0x0A, 0xC9, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x10),
                new AdLibInstrument(new byte []{ 0x41, 0x00, 0x0E, 0xEA, 0x53, 0xC2, 0x00, 0x08, 0xCA, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x07),
                new AdLibInstrument(new byte []{ 0xC1, 0x40, 0x0C, 0x59, 0x6A, 0xC2, 0x80, 0x3C, 0xAB, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0D),
                new AdLibInstrument(new byte []{ 0x4B, 0x00, 0x0A, 0xF5, 0x61, 0xC2, 0x19, 0x0C, 0xE9, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x07),
                new AdLibInstrument(new byte []{ 0x62, 0x00, 0x7F, 0xD8, 0x54, 0xEA, 0x00, 0x8F, 0xD8, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0x7F, 0xD9, 0x56, 0xE1, 0x00, 0x8F, 0xD8, 0x7E, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0x7F, 0xD9, 0x56, 0xE1, 0x00, 0x8F, 0xD8, 0x7E, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x70
                new AdLibInstrument(new byte []{ 0xCF, 0x40, 0x09, 0xEA, 0x54, 0xC4, 0x00, 0x0C, 0xDB, 0x64, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte []{ 0xCF, 0x40, 0x0C, 0xAA, 0x54, 0xC4, 0x00, 0x18, 0xF9, 0x64, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte []{ 0xC9, 0x0E, 0x88, 0xD9, 0x3E, 0xC2, 0x08, 0x1A, 0xEA, 0x6C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte []{ 0x03, 0x00, 0x15, 0x00, 0x64, 0x02, 0x00, 0x08, 0x00, 0x7C, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte []{ 0x01, 0x00, 0x47, 0xD7, 0x6C, 0x01, 0x3F, 0x0C, 0xFB, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte []{ 0x00, 0x00, 0x36, 0x67, 0x7C, 0x01, 0x3F, 0x0E, 0xFA, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte []{ 0x02, 0x00, 0x36, 0x68, 0x7C, 0x01, 0x3F, 0x0E, 0xFA, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte []{ 0xCB, 0x00, 0xAF, 0x00, 0x7E, 0xC0, 0x00, 0xC0, 0x06, 0x7F, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0F),
                new AdLibInstrument(new byte []{ 0x05, 0x0D, 0x80, 0xA6, 0x7F, 0x0B, 0x38, 0xA9, 0xD8, 0x00, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte []{ 0x0F, 0x00, 0x90, 0xFA, 0x68, 0x06, 0x00, 0xA7, 0x39, 0x54, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x06),
                new AdLibInstrument(new byte []{ 0xC9, 0x15, 0xDD, 0xFF, 0x7C, 0x00, 0x00, 0xE7, 0xFC, 0x6C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x38),
                new AdLibInstrument(new byte []{ 0x48, 0x3C, 0x30, 0xF6, 0x03, 0x0A, 0x38, 0x97, 0xE8, 0x00, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte []{ 0x07, 0x80, 0x0B, 0xC8, 0x65, 0x02, 0x3F, 0x0C, 0xEA, 0x7C, 0x0F, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte []{ 0x00, 0x21, 0x66, 0x40, 0x03, 0x00, 0x3F, 0x47, 0x00, 0x00, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte []{ 0x08, 0x00, 0x0B, 0x3C, 0x7C, 0x08, 0x3F, 0x06, 0xF3, 0x00, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02)
            };

        static readonly byte[] noteFrequencies =
            {
                90, 91, 92, 92, 93, 94, 94, 95,
                96, 96, 97, 98, 98, 99, 100, 101,
                101, 102, 103, 104, 104, 105, 106, 107,
                107, 108, 109, 110, 111, 111, 112, 113,
                114, 115, 115, 116, 117, 118, 119, 120,
                121, 121, 122, 123, 124, 125, 126, 127,
                128, 129, 130, 131, 132, 132, 133, 134,
                135, 136, 137, 138, 139, 140, 141, 142,
                143, 145, 146, 147, 148, 149, 150, 151,
                152, 153, 154, 155, 157, 158, 159, 160,
                161, 162, 163, 165, 166, 167, 168, 169,
                171, 172, 173, 174, 176, 177, 178, 180,
                181, 182, 184, 185, 186, 188, 189, 190,
                192, 193, 194, 196, 197, 199, 200, 202,
                203, 205, 206, 208, 209, 211, 212, 214,
                215, 217, 218, 220, 222, 223, 225, 226,
                228, 230, 231, 233, 235, 236, 238, 240,
                242, 243, 245, 247, 249, 251, 252, 254
            };

        static readonly ushort[] numStepsTable =
            {
                1, 2, 4, 5,
                6, 7, 8, 9,
                10, 12, 14, 16,
                18, 21, 24, 30,
                36, 50, 64, 82,
                100, 136, 160, 192,
                240, 276, 340, 460,
                600, 860, 1200, 1600
            };
        static readonly byte[] paramTable1 =
            {
                29, 28, 27, 0,
                3, 4, 7, 8,
                13, 16, 17, 20,
                21, 30, 31, 0
            };

        static readonly ushort[] maxValTable =
            {    
                0x2FF, 0x1F, 0x7, 0x3F,
                0x0F, 0x0F, 0x0F, 0x3,
                0x3F, 0x0F, 0x0F, 0x0F,
                0x3, 0x3E, 0x1F, 0
            };

        static readonly byte[] volumeTable =
            {    
                0, 4, 7, 11,
                13, 16, 18, 20,
                22, 24, 26, 27,
                29, 30, 31, 33,
                34, 35, 36, 37,
                38, 39, 40, 41,
                42, 43, 44, 44,
                45, 46, 47, 47,
                48, 49, 49, 50,
                51, 51, 52, 53,
                53, 54, 54, 55,
                55, 56, 56, 57,
                57, 58, 58, 59,
                59, 60, 60, 60,
                61, 61, 62, 62,
                62, 63, 63, 63
            };

        static readonly byte[] operator1Offsets =
            {
                0, 1, 2, 8,
                9, 10, 16, 17,
                18
            };

        static readonly byte[] operator2Offsets =
            {
                3, 4, 5, 11,
                12, 13, 19, 20,
                21
            };

        static readonly AdLibSetParams[] setParamTable =
            {
                new AdLibSetParams(new byte[]{ 0x40, 0, 63, 63 }),  // level
                new AdLibSetParams(new byte[]{ 0xE0, 2, 0, 0 }),    // unused
                new AdLibSetParams(new byte[]{ 0x40, 6, 192, 0 }),  // level key scaling
                new AdLibSetParams(new byte[]{ 0x20, 0, 15, 0 }),   // modulator frequency multiple
                new AdLibSetParams(new byte[]{ 0x60, 4, 240, 15 }), // attack rate
                new AdLibSetParams(new byte[]{ 0x60, 0, 15, 15 }),  // decay rate
                new AdLibSetParams(new byte[]{ 0x80, 4, 240, 15 }), // sustain level
                new AdLibSetParams(new byte[]{ 0x80, 0, 15, 15 }),  // release rate
                new AdLibSetParams(new byte[]{ 0xE0, 0, 3, 0 }),    // waveformSelect select
                new AdLibSetParams(new byte[]{ 0x20, 7, 128, 0 }),  // amp mod
                new AdLibSetParams(new byte[]{ 0x20, 6, 64, 0 }),   // vib
                new AdLibSetParams(new byte[]{ 0x20, 5, 32, 0 }),   // eg typ
                new AdLibSetParams(new byte[]{ 0x20, 4, 16, 0 }),   // ksr
                new AdLibSetParams(new byte[]{ 0xC0, 0, 1, 0 }),    // decay alg
                new AdLibSetParams(new byte[]{ 0xC0, 1, 14, 0 })    // feedback
            };
    }
}

