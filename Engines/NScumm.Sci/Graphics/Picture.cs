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
using NScumm.Core.Common;
using NScumm.Core.Graphics;
using System;
using System.Collections.Generic;

namespace NScumm.Sci.Graphics
{
    enum PictureType
    {
        REGULAR = 0,
        SCI11 = 1,
        SCI32 = 2
    }

    enum PictureOperation
    {
        SET_COLOR = 0xf0,
        DISABLE_VISUAL = 0xf1,
        SET_PRIORITY = 0xf2,
        DISABLE_PRIORITY = 0xf3,
        SHORT_PATTERNS = 0xf4,
        MEDIUM_LINES = 0xf5,
        LONG_LINES = 0xf6,
        SHORT_LINES = 0xf7,
        FILL = 0xf8,
        SET_PATTERN = 0xf9,
        ABSOLUTE_PATTERN = 0xfa,
        SET_CONTROL = 0xfb,
        DISABLE_CONTROL = 0xfc,
        MEDIUM_PATTERNS = 0xfd,
        OPX = 0xfe,
        TERMINATE = 0xff
    }

    enum PictureOperationEx
    {
        EGA_SET_PALETTE_ENTRIES = 0,
        EGA_SET_PALETTE = 1,
        EGA_MONO0 = 2,
        EGA_MONO1 = 3,
        EGA_MONO2 = 4,
        EGA_MONO3 = 5,
        EGA_MONO4 = 6,
        EGA_EMBEDDED_VIEW = 7,
        EGA_SET_PRIORITY_TABLE = 8,
        VGA_SET_PALETTE_ENTRIES = 0,
        VGA_EMBEDDED_VIEW = 1,
        VGA_SET_PALETTE = 2,
        VGA_PRIORITY_TABLE_EQDIST = 3,
        VGA_PRIORITY_TABLE_EXPLICIT = 4
    }

    /// <summary>
    /// Picture class, handles loading and displaying of picture resources
    ///  every picture resource has its own instance of this class
    /// </summary>
    internal class GfxPicture
    {
        private const int PIC_EGAPALETTE_COUNT = 4;
        private const int PIC_EGAPALETTE_SIZE = 40;
        private const int PIC_EGAPALETTE_TOTALSIZE = PIC_EGAPALETTE_COUNT * PIC_EGAPALETTE_SIZE;
        private const int PIC_EGAPRIORITY_SIZE = PIC_EGAPALETTE_SIZE;

        private const int SCI_PATTERN_CODE_RECTANGLE = 0x10;
        private const int SCI_PATTERN_CODE_USE_TEXTURE = 0x20;
        private const int SCI_PATTERN_CODE_PENSIZE = 0x07;


        private const PictureOperation PIC_OP_FIRST = PictureOperation.SET_COLOR;

        private ResourceManager _resMan;
        private GfxCoordAdjuster _coordAdjuster;
        private GfxPorts _ports;
        private GfxScreen _screen;
        private GfxPalette _palette;

        private short _resourceId;
        private ResourceManager.ResourceSource.Resource _resource;
        private PictureType _resourceType;

        private short _animationNr;
        private bool _mirroredFlag;
        private bool _addToFlag;
        private short _EGApaletteNo;
        private byte _priority;

        // If true, we will show the whole EGA drawing process...
        private bool _EGAdrawingVisualize;

        static readonly byte[] vector_defaultEGApalette = {
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0x88,
            0x88, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x88,
            0x88, 0xf9, 0xfa, 0xfb, 0xfc, 0xfd, 0xfe, 0xff,
            0x08, 0x91, 0x2a, 0x3b, 0x4c, 0x5d, 0x6e, 0x88
        };

        static readonly byte[] vector_defaultEGApriority = {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07
        };

        // This table is bitwise upwards (from bit0 to bit7), sierras original table went down the bits (bit7 to bit0)
        //  this was done to simplify things, so we can just run through the table w/o worrying too much about clipping
        static readonly bool[] vectorPatternTextures = {
            false, false,  true, false, false, false, false, false, // 0x04
	         true, false, false,  true, false,  true, false, false, // 0x29
	        false, false, false, false, false, false,  true, false, // 0x40
	        false, false,  true, false, false,  true, false, false, // 0x24
	         true, false, false,  true, false, false, false, false, // 0x09
	         true, false, false, false, false, false,  true, false, // 0x41
	         true, false,  true, false, false,  true, false, false, // 0x25
	         true, false,  true, false, false, false,  true, false, // 0x45
	         true, false, false, false, false, false,  true, false, // 0x41
	        false, false, false, false,  true, false, false,  true, // 0x90
	        false, false, false, false,  true, false,  true, false, // 0x50
	        false, false,  true, false, false, false,  true, false, // 0x44
	        false, false, false,  true, false, false,  true, false, // 0x48
	        false, false, false,  true, false, false, false, false, // 0x08
	        false,  true, false, false, false, false,  true, false, // 0x42
	        false, false, false,  true, false,  true, false, false, // 0x28
	         true, false, false,  true, false, false, false,  true, // 0x89
	        false,  true, false, false,  true, false,  true, false, // 0x52
	         true, false, false,  true, false, false, false,  true, // 0x89
	        false, false, false,  true, false, false, false,  true, // 0x88
	        false, false, false, false,  true, false, false, false, // 0x10
	        false, false, false,  true, false, false,  true, false, // 0x48
	        false, false,  true, false, false,  true, false,  true, // 0xA4
	        false, false, false,  true, false, false, false, false, // 0x08
	        false, false,  true, false, false, false,  true, false, // 0x44
	         true, false,  true, false,  true, false, false, false, // 0x15
	        false, false, false,  true, false,  true, false, false, // 0x28
	        false, false,  true, false, false,  true, false, false, // 0x24
	        false, false, false, false, false, false, false, false, // 0x00
	        false,  true, false,  true, false, false, false, false, // 0x0A
	        false, false,  true, false, false,  true, false, false, // 0x24
	        false, false, false, false, false,  true, false,        // 0x20 (last bit is not mentioned cause original interpreter also ignores that bit)
	        // Now the table is actually duplicated, so we won't need to wrap around
	        false, false,  true, false, false, false, false, false, // 0x04
	         true, false, false,  true, false,  true, false, false, // 0x29
	        false, false, false, false, false, false,  true, false, // 0x40
	        false, false,  true, false, false,  true, false, false, // 0x24
	         true, false, false,  true, false, false, false, false, // 0x09
	         true, false, false, false, false, false,  true, false, // 0x41
	         true, false,  true, false, false,  true, false, false, // 0x25
	         true, false,  true, false, false, false,  true, false, // 0x45
	         true, false, false, false, false, false,  true, false, // 0x41
	        false, false, false, false,  true, false, false,  true, // 0x90
	        false, false, false, false,  true, false,  true, false, // 0x50
	        false, false,  true, false, false, false,  true, false, // 0x44
	        false, false, false,  true, false, false,  true, false, // 0x48
	        false, false, false,  true, false, false, false, false, // 0x08
	        false,  true, false, false, false, false,  true, false, // 0x42
	        false, false, false,  true, false,  true, false, false, // 0x28
	         true, false, false,  true, false, false, false,  true, // 0x89
	        false,  true, false, false,  true, false,  true, false, // 0x52
	         true, false, false,  true, false, false, false,  true, // 0x89
	        false, false, false,  true, false, false, false,  true, // 0x88
	        false, false, false, false,  true, false, false, false, // 0x10
	        false, false, false,  true, false, false,  true, false, // 0x48
	        false, false,  true, false, false,  true, false,  true, // 0xA4
	        false, false, false,  true, false, false, false, false, // 0x08
	        false, false,  true, false, false, false,  true, false, // 0x44
	         true, false,  true, false,  true, false, false, false, // 0x15
	        false, false, false,  true, false,  true, false, false, // 0x28
	        false, false,  true, false, false,  true, false, false, // 0x24
	        false, false, false, false, false, false, false, false, // 0x00
	        false,  true, false,  true, false, false, false, false, // 0x0A
	        false, false,  true, false, false,  true, false, false, // 0x24
	        false, false, false, false, false,  true, false,        // 0x20 (last bit is not mentioned cause original interpreter also ignores that bit)
        };

        // Bit offsets into pattern_textures
        static readonly byte[] vectorPatternTextureOffset = {
            0x00, 0x18, 0x30, 0xc4, 0xdc, 0x65, 0xeb, 0x48,
            0x60, 0xbd, 0x89, 0x05, 0x0a, 0xf4, 0x7d, 0x7d,
            0x85, 0xb0, 0x8e, 0x95, 0x1f, 0x22, 0x0d, 0xdf,
            0x2a, 0x78, 0xd5, 0x73, 0x1c, 0xb4, 0x40, 0xa1,
            0xb9, 0x3c, 0xca, 0x58, 0x92, 0x34, 0xcc, 0xce,
            0xd7, 0x42, 0x90, 0x0f, 0x8b, 0x7f, 0x32, 0xed,
            0x5c, 0x9d, 0xc8, 0x99, 0xad, 0x4e, 0x56, 0xa6,
            0xf7, 0x68, 0xb7, 0x25, 0x82, 0x37, 0x3a, 0x51,
            0x69, 0x26, 0x38, 0x52, 0x9e, 0x9a, 0x4f, 0xa7,
            0x43, 0x10, 0x80, 0xee, 0x3d, 0x59, 0x35, 0xcf,
            0x79, 0x74, 0xb5, 0xa2, 0xb1, 0x96, 0x23, 0xe0,
            0xbe, 0x05, 0xf5, 0x6e, 0x19, 0xc5, 0x66, 0x49,
            0xf0, 0xd1, 0x54, 0xa9, 0x70, 0x4b, 0xa4, 0xe2,
            0xe6, 0xe5, 0xab, 0xe4, 0xd2, 0xaa, 0x4c, 0xe3,
            0x06, 0x6f, 0xc6, 0x4a, 0xa4, 0x75, 0x97, 0xe1
        };

        // Bitmap for drawing sierra circles
        static readonly byte[][] vectorPatternCircles = {
            new byte[]{ 0x01 },
            new byte[]{ 0x72, 0x02 },
            new byte[]{ 0xCE, 0xF7, 0x7D, 0x0E },
            new byte[]{ 0x1C, 0x3E, 0x7F, 0x7F, 0x7F, 0x3E, 0x1C, 0x00 },
            new byte[]{ 0x38, 0xF8, 0xF3, 0xDF, 0x7F, 0xFF, 0xFD, 0xF7, 0x9F, 0x3F, 0x38 },
            new byte[]{ 0x70, 0xC0, 0x1F, 0xFE, 0xE3, 0x3F, 0xFF, 0xF7, 0x7F, 0xFF, 0xE7, 0x3F, 0xFE, 0xC3, 0x1F, 0xF8, 0x00 },
            new byte[]{ 0xF0, 0x01, 0xFF, 0xE1, 0xFF, 0xF8, 0x3F, 0xFF, 0xDF, 0xFF, 0xF7, 0xFF, 0xFD, 0x7F, 0xFF, 0x9F, 0xFF,
                        0xE3, 0xFF, 0xF0, 0x1F, 0xF0, 0x01 },
            new byte[]{ 0xE0, 0x03, 0xF8, 0x0F, 0xFC, 0x1F, 0xFE, 0x3F, 0xFE, 0x3F, 0xFF, 0x7F, 0xFF, 0x7F, 0xFF, 0x7F, 0xFF,
                0x7F, 0xFF, 0x7F, 0xFE, 0x3F, 0xFE, 0x3F, 0xFC, 0x1F, 0xF8, 0x0F, 0xE0, 0x03 }
        //  { 0x01 };
        //	{ 0x03, 0x03, 0x03 },
        //	{ 0x02, 0x07, 0x07, 0x07, 0x02 },
        //	{ 0x06, 0x06, 0x0F, 0x0F, 0x0F, 0x06, 0x06 },
        //	{ 0x04, 0x0E, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x0E, 0x04 },
        //	{ 0x0C, 0x1E, 0x1E, 0x1E, 0x3F, 0x3F, 0x3F, 0x1E, 0x1E, 0x1E, 0x0C },
        //	{ 0x1C, 0x3E, 0x3E, 0x3E, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x3E, 0x3E, 0x3E, 0x1C },
        //	{ 0x18, 0x3C, 0x7E, 0x7E, 0x7E, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7E, 0x7E, 0x7E, 0x3C, 0x18 }
        };

        public GfxPicture(ResourceManager resMan, GfxCoordAdjuster coordAdjuster, GfxPorts ports, GfxScreen screen, GfxPalette palette, int resourceId, bool EGAdrawingVisualize = false)
        {
            _resMan = resMan;
            _coordAdjuster = coordAdjuster;
            _ports = ports;
            _screen = screen;
            _palette = palette;
            _resourceId = (short)resourceId;
            _EGAdrawingVisualize = EGAdrawingVisualize;

            InitData(resourceId);
        }

        private void InitData(int resourceId)
        {
            _resource = _resMan.FindResource(new ResourceId(ResourceType.Pic, (ushort)resourceId), true);
            if (_resource == null)
            {
                throw new InvalidOperationException($"picture resource {resourceId} not found");
            }
        }

        /// <summary>
        /// differentiation between various picture formats can NOT get done using sci-version checks.
        ///  Games like PQ1 use the "old" vector data picture format, but are actually SCI1.1
        ///  We should leave this that way to decide the format on-the-fly instead of hardcoding it in any way
        /// </summary>
        /// <param name="animationNr"></param>
        /// <param name="mirroredFlag"></param>
        /// <param name="addToFlag"></param>
        /// <param name="paletteId"></param>
        public void Draw(short animationNr, bool mirroredFlag, bool addToFlag, short EGApaletteNo)
        {
            ushort headerSize;

            _animationNr = animationNr;
            _mirroredFlag = mirroredFlag;
            _addToFlag = addToFlag;
            _EGApaletteNo = EGApaletteNo;
            _priority = 0;

            headerSize = _resource.data.ToUInt16();
            switch (headerSize)
            {
                case 0x26: // SCI 1.1 VGA picture
                    _resourceType = PictureType.SCI11;
                    DrawSci11Vga();
                    break;
# if ENABLE_SCI32
                case 0x0e: // SCI32 VGA picture
                    _resourceType = SCI_PICTURE_TYPE_SCI32;
                    drawSci32Vga(0, 0, 0, 0, 0, false);
                    break;
#endif
                default:
                    // VGA, EGA or Amiga vector data
                    _resourceType = PictureType.REGULAR;
                    DrawVectorData(new ByteAccess(_resource.data), _resource.size);
                    break;
            }
        }

        private void DrawSci11Vga()
        {
            ByteAccess inbuffer = new ByteAccess(_resource.data);
            int size = _resource.size;
            int priorityBandsCount = inbuffer[3];
            int has_cel = inbuffer[4];
            int vector_dataPos = inbuffer.ToInt32(16);
            int vector_size = size - vector_dataPos;
            int palette_data_ptr = inbuffer.ToInt32(28);
            int cel_headerPos = inbuffer.ToInt32(32);
            int cel_RlePos = inbuffer.ToInt32(cel_headerPos + 24);
            int cel_LiteralPos = inbuffer.ToInt32(cel_headerPos + 28);

            // Header
            // [headerSize:WORD] [unknown:BYTE] [priorityBandCount:BYTE] [hasCel:BYTE] [unknown:BYTE]
            // [unknown:WORD] [unknown:WORD] [unknown:WORD] [unknown:WORD] [unknown:WORD]
            // Offset 16
            // [vectorDataOffset:DWORD] [unknown:DWORD] [unknown:DWORD] [paletteDataOffset:DWORD]
            // Offset 32
            // [celHeaderOffset:DWORD] [unknown:DWORD]
            // [priorityBandData:WORD] * priorityBandCount
            // [priority:BYTE] [unknown:BYTE]

            // priority bands are supposed to be 14 for sci1.1 pictures
            //assert(priorityBandsCount == 14);

            if (_addToFlag)
            {
                _priority = (byte)(inbuffer[40 + priorityBandsCount * 2] & 0xF);
            }

            // display Cel-data
            if (has_cel != 0)
            {
                // Create palette and set it
                var palette = _palette.CreateFromData(new ByteAccess(inbuffer, palette_data_ptr), size - palette_data_ptr);
                _palette.Set(palette, true);

                DrawCelData(inbuffer, size, cel_headerPos, cel_RlePos, cel_LiteralPos, 0, 0, 0, 0, false);
            }

            // process vector data
            DrawVectorData(new ByteAccess(inbuffer, vector_dataPos), vector_size);

            // Set priority band information
            _ports.PriorityBandsInitSci11(new ByteAccess(inbuffer, 40));
        }

        private void DrawCelData(BytePtr inbuffer, int size, int headerPos, int rlePos, int literalPos, int drawX, int drawY, int pictureX, int pictureY, bool isEGA)
        {
            byte[] celBitmap = null;
            var headerPtr = new ByteAccess(inbuffer, headerPos);
            var rlePtr = new ByteAccess(inbuffer, rlePos);
            // displaceX, displaceY fields are ignored, and may contain garbage
            // (e.g. pic 261 in Dr. Brain 1 Spanish - bug #3614914)
            //int16 displaceX, displaceY;
            byte priority = _priority;
            byte clearColor;
            bool compression = true;
            byte curByte;
            short y, lastY, x, leftX, rightX;
            int pixelCount;
            ushort width, height;

            // if the picture is not an overlay and we are also not in EGA mode, use priority 0
            if (!isEGA && !_addToFlag)
                priority = 0;

#if ENABLE_SCI32
            if (_resourceType != SCI_PICTURE_TYPE_SCI32)
            {
#endif
            // Width/height here are always LE, even in Mac versions
            width = headerPtr.ToUInt16(0);
            height = headerPtr.ToUInt16(2);
            //displaceX = (signed char)headerPtr[4];
            //displaceY = (unsigned char)headerPtr[5];
            if (_resourceType == PictureType.SCI11)
                // SCI1.1 uses hardcoded clearcolor for pictures, even if cel header specifies otherwise
                clearColor = _screen.ColorWhite;
            else
                clearColor = headerPtr[6];
# if ENABLE_SCI32
            }
            else
            {
                width = READ_SCI11ENDIAN_UINT16(headerPtr + 0);
                height = READ_SCI11ENDIAN_UINT16(headerPtr + 2);
                //displaceX = READ_SCI11ENDIAN_UINT16(headerPtr + 4); // probably signed?!?
                //displaceY = READ_SCI11ENDIAN_UINT16(headerPtr + 6); // probably signed?!?
                clearColor = headerPtr[8];
                if (headerPtr[9] == 0)
                    compression = false;
            }
#endif

            //if (displaceX || displaceY)
            //  error("unsupported embedded cel-data in picture");

            // We will unpack cel-data into a temporary buffer and then plot it to screen
            //  That needs to be done cause a mirrored picture may be requested
            pixelCount = width * height;
            celBitmap = new byte[pixelCount];

            if (SciEngine.Instance.Platform == Core.IO.Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                // See GfxView::unpackCel() for why this black/white swap is done
                // This picture swap is only needed in SCI32, not SCI1.1
                if (clearColor == 0)
                    clearColor = 0xff;
                else if (clearColor == 0xff)
                    clearColor = 0;
            }

            if (compression)
            {
                GfxView.UnpackCelData(inbuffer.Data, inbuffer.Offset, celBitmap, clearColor, pixelCount, rlePos, literalPos, _resMan.ViewType, width, false);
            }
            else
            {
                // No compression (some SCI32 pictures)
                rlePtr.CopyTo(celBitmap, 0, pixelCount);
            }

            if (SciEngine.Instance.Platform == Core.IO.Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                // See GfxView::unpackCel() for why this black/white swap is done
                // This picture swap is only needed in SCI32, not SCI1.1
                for (int i = 0; i < pixelCount; i++)
                {
                    if (celBitmap[i] == 0)
                        celBitmap[i] = 0xff;
                    else if (celBitmap[i] == 0xff)
                        celBitmap[i] = 0;
                }
            }

            var displayArea = _coordAdjuster.PictureGetDisplayArea();

            // Horizontal clipping
            ushort skipCelBitmapPixels = 0;
            short displayWidth = (short)width;
            if (pictureX != 0)
            {
                // horizontal scroll position for picture active, we need to adjust drawX accordingly
                drawX -= pictureX;
                if (drawX < 0)
                {
                    skipCelBitmapPixels = (ushort)-drawX;
                    displayWidth = (short)(displayWidth - skipCelBitmapPixels);
                    drawX = 0;
                }
            }

            // Vertical clipping
            ushort skipCelBitmapLines = 0;
            short displayHeight = (short)height;
            if (pictureY != 0)
            {
                // vertical scroll position for picture active, we need to adjust drawY accordingly
                // TODO: Finish this
                /*drawY -= pictureY;
                if (drawY < 0) {
                    skipCelBitmapLines = -drawY;
                    displayHeight -= skipCelBitmapLines;
                    drawY = 0;
                }*/
            }

            if (displayWidth > 0 && displayHeight > 0)
            {
                y = (short)(displayArea.Top + drawY);
                lastY = (short)Math.Min(height + y, displayArea.Bottom);
                leftX = (short)(displayArea.Left + drawX);
                rightX = (short)Math.Min(displayWidth + leftX, displayArea.Right);

                ushort sourcePixelSkipPerRow = 0;
                if (width > rightX - leftX)
                    sourcePixelSkipPerRow = (ushort)(width - (rightX - leftX));

                // Change clearcolor to white, if we dont add to an existing picture. That way we will paint everything on screen
                // but white and that won't matter because the screen is supposed to be already white. It seems that most (if not all)
                // SCI1.1 games use color 0 as transparency and SCI1 games use color 255 as transparency. Sierra SCI seems to paint
                // the whole data to screen and wont skip over transparent pixels. So this will actually make it work like Sierra.
                // SCI32 doesn't use _addToFlag at all.
                if (!_addToFlag && _resourceType != PictureType.SCI32)
                    clearColor = _screen.ColorWhite;

                GfxScreenMasks drawMask = priority > 15 ? GfxScreenMasks.VISUAL : GfxScreenMasks.VISUAL | GfxScreenMasks.PRIORITY;

                var ptr = new ByteAccess(celBitmap);
                ptr.Offset += skipCelBitmapPixels;
                ptr.Offset += skipCelBitmapLines * width;

                if ((!isEGA) || (priority < 16))
                {
                    // VGA + EGA, EGA only checks priority, when given priority is below 16
                    if (!_mirroredFlag)
                    {
                        // Draw bitmap to screen
                        x = leftX;
                        while (y < lastY)
                        {
                            curByte = ptr.Increment();
                            if ((curByte != clearColor) && (priority >= _screen.GetPriority(x, y)))
                                _screen.PutPixel(x, y, drawMask, curByte, priority, 0);

                            x++;

                            if (x >= rightX)
                            {
                                ptr.Offset += sourcePixelSkipPerRow;
                                x = leftX;
                                y++;
                            }
                        }
                    }
                    else
                    {
                        // Draw bitmap to screen (mirrored)
                        x = (short)(rightX - 1);
                        while (y < lastY)
                        {
                            curByte = ptr.Increment();
                            if ((curByte != clearColor) && (priority >= _screen.GetPriority(x, y)))
                                _screen.PutPixel(x, y, drawMask, curByte, priority, 0);

                            if (x == leftX)
                            {
                                ptr.Offset += sourcePixelSkipPerRow;
                                x = rightX;
                                y++;
                            }

                            x--;
                        }
                    }
                }
                else
                {
                    // EGA, when priority is above 15
                    //  we don't check priority and also won't set priority at all
                    //  fixes picture 48 of kq5 (island overview). Bug #5182
                    if (!_mirroredFlag)
                    {
                        // EGA+priority>15: Draw bitmap to screen
                        x = leftX;
                        while (y < lastY)
                        {
                            curByte = ptr.Increment();
                            if (curByte != clearColor)
                                _screen.PutPixel(x, y, GfxScreenMasks.VISUAL, curByte, 0, 0);

                            x++;

                            if (x >= rightX)
                            {
                                ptr.Offset += sourcePixelSkipPerRow;
                                x = leftX;
                                y++;
                            }
                        }
                    }
                    else
                    {
                        // EGA+priority>15: Draw bitmap to screen (mirrored)
                        x = (short)(rightX - 1);
                        while (y < lastY)
                        {
                            curByte = ptr.Increment();
                            if (curByte != clearColor)
                                _screen.PutPixel(x, y, GfxScreenMasks.VISUAL, curByte, 0, 0);

                            if (x == leftX)
                            {
                                ptr.Offset += sourcePixelSkipPerRow;
                                x = rightX;
                                y++;
                            }

                            x--;
                        }
                    }
                }
            }
        }

        private void DrawVectorData(BytePtr data, int dataSize)
        {
            byte pic_color = _screen.ColorDefaultVectorData;
            byte pic_priority = 255, pic_control = 255;
            short x = 0, y = 0, oldx, oldy;
            byte[] EGApalettes = new byte[PIC_EGAPALETTE_TOTALSIZE];
            var EGApalette = new ByteAccess(EGApalettes, _EGApaletteNo * PIC_EGAPALETTE_SIZE);
            byte[] EGApriority = new byte[PIC_EGAPRIORITY_SIZE];
            bool isEGA = false;
            int curPos = 0;
            ushort size;
            byte pixel;
            int i;
            Palette palette = new Palette();
            short pattern_Code = 0, pattern_Texture = 0;
            bool icemanDrawFix = false;
            bool ignoreBrokenPriority = false;

            if (_EGApaletteNo >= PIC_EGAPALETTE_COUNT)
                _EGApaletteNo = 0;

            if (_resMan.ViewType == ViewType.Ega)
            {
                isEGA = true;
                // setup default mapping tables
                for (i = 0; i < PIC_EGAPALETTE_TOTALSIZE; i += PIC_EGAPALETTE_SIZE)
                {
                    Array.Copy(vector_defaultEGApalette, 0, EGApalettes, i, vector_defaultEGApalette.Length);
                }
                Array.Copy(vector_defaultEGApriority, EGApriority, vector_defaultEGApriority.Length);

                if (SciEngine.Instance.GameId == SciGameId.ICEMAN)
                {
                    // WORKAROUND: we remove certain visual&priority lines in underwater
                    // rooms of iceman, when not dithering the picture. Normally those
                    // lines aren't shown, because they share the same color as the
                    // dithered fill color combination. When not dithering, those lines
                    // would appear and get distracting.
                    if ((_screen.IsUnditheringEnabled) && ((_resourceId >= 53 && _resourceId <= 58) || (_resourceId == 61)))
                        icemanDrawFix = true;
                }
            }

            // Drawing
            while (curPos < dataSize)
            {
# if DEBUG_PICTURE_DRAW
                debug("Picture op: %X (%s) at %d", data[curPos], picOpcodeNames[data[curPos] - 0xF0], curPos);
                _screen.copyToScreen();
                g_system.updateScreen();
                g_system.delayMillis(400);
#endif
                PictureOperation pic_op;
                switch (pic_op = (PictureOperation)data[curPos++])
                {
                    case PictureOperation.SET_COLOR:
                        pic_color = data[curPos++];
                        if (isEGA)
                        {
                            pic_color = EGApalette[pic_color];
                            pic_color ^= (byte)(pic_color << 4);
                        }
                        break;
                    case PictureOperation.DISABLE_VISUAL:
                        pic_color = 0xFF;
                        break;

                    case PictureOperation.SET_PRIORITY:
                        pic_priority = (byte)(data[curPos++] & 0x0F);
                        if (isEGA)
                            pic_priority = EGApriority[pic_priority];
                        if (ignoreBrokenPriority)
                            pic_priority = 255;
                        break;
                    case PictureOperation.DISABLE_PRIORITY:
                        pic_priority = 255;
                        break;

                    case PictureOperation.SET_CONTROL:
                        pic_control = (byte)(data[curPos++] & 0x0F);
                        break;
                    case PictureOperation.DISABLE_CONTROL:
                        pic_control = 255;
                        break;

                    case PictureOperation.SHORT_LINES: // short line
                        VectorGetAbsCoords(data, ref curPos, out x, out y);
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            oldx = x; oldy = y;
                            VectorGetRelCoords(data, ref curPos, ref x, ref y);
                            Point startPoint = new Point(oldx, oldy);
                            Point endPoint = new Point(x, y);
                            _ports.OffsetLine(ref startPoint, ref endPoint);
                            _screen.DrawLine(startPoint, endPoint, pic_color, pic_priority, pic_control);
                        }
                        break;
                    case PictureOperation.MEDIUM_LINES: // medium line
                        VectorGetAbsCoords(data, ref curPos, out x, out y);
                        if (icemanDrawFix)
                        {
                            // WORKAROUND: remove certain lines in iceman - see above
                            if ((pic_color == 1) && (pic_priority == 14))
                            {
                                if ((y < 100) || ((y & 1) == 0))
                                {
                                    pic_color = 255;
                                    pic_priority = 255;
                                }
                            }
                        }
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            oldx = x; oldy = y;
                            VectorGetRelCoordsMed(data, ref curPos, ref x, ref y);
                            Point startPoint = new Point(oldx, oldy);
                            Point endPoint = new Point(x, y);
                            _ports.OffsetLine(ref startPoint, ref endPoint);
                            _screen.DrawLine(startPoint, endPoint, pic_color, pic_priority, pic_control);
                        }
                        break;
                    case PictureOperation.LONG_LINES: // long line
                        VectorGetAbsCoords(data, ref curPos, out x, out y);
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            oldx = x; oldy = y;
                            VectorGetAbsCoords(data, ref curPos, out x, out y);
                            Point startPoint = new Point(oldx, oldy);
                            Point endPoint = new Point(x, y);
                            _ports.OffsetLine(ref startPoint, ref endPoint);
                            _screen.DrawLine(startPoint, endPoint, pic_color, pic_priority, pic_control);
                        }
                        break;

                    case PictureOperation.FILL: //fill
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            VectorGetAbsCoords(data, ref curPos, out x, out y);
                            VectorFloodFill(x, y, pic_color, pic_priority, pic_control);
                        }
                        break;

                    // Pattern opcodes are handled in sierra sci1.1+ as actual NOPs and
                    // normally they definitely should not occur inside picture data for
                    // such games.
                    case PictureOperation.SET_PATTERN:
                        if (_resourceType >= PictureType.SCI11)
                        {
                            if (SciEngine.Instance.GameId == SciGameId.SQ4)
                            {
                                // WORKAROUND: For SQ4 / for some pictures handle this like
                                // a terminator. This picture includes garbage data, first a
                                // set pattern w/o parameter and then short pattern. I guess
                                // that garbage is a left over from the sq4-floppy (sci1) to
                                // sq4-cd (sci1.1) conversion.
                                switch (_resourceId)
                                {
                                    case 35:
                                    case 381:
                                    case 376:
                                        //case 390:	// in the blacklisted NRS patch 1.2 (bug #3615060)
                                        return;
                                    default:
                                        break;
                                }
                            }
                            throw new InvalidOperationException("pic-operation set pattern inside sci1.1+ vector data");
                        }
                        pattern_Code = data[curPos++];
                        break;
                    case PictureOperation.SHORT_PATTERNS:
                        if (_resourceType >= PictureType.SCI11)
                            throw new InvalidOperationException("pic-operation short pattern inside sci1.1+ vector data");
                        VectorGetPatternTexture(data, ref curPos, pattern_Code, ref pattern_Texture);
                        VectorGetAbsCoords(data, ref curPos, out x, out y);
                        VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            VectorGetPatternTexture(data, ref curPos, pattern_Code, ref pattern_Texture);
                            VectorGetRelCoords(data, ref curPos, ref x, ref y);
                            VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        }
                        break;
                    case PictureOperation.MEDIUM_PATTERNS:
                        if (_resourceType >= PictureType.SCI11)
                            throw new InvalidOperationException("pic-operation medium pattern inside sci1.1+ vector data");
                        VectorGetPatternTexture(data, ref curPos, pattern_Code, ref pattern_Texture);
                        VectorGetAbsCoords(data, ref curPos, out x, out y);
                        VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            VectorGetPatternTexture(data, ref curPos, pattern_Code, ref pattern_Texture);
                            VectorGetRelCoordsMed(data, ref curPos, ref x, ref y);
                            VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        }
                        break;
                    case PictureOperation.ABSOLUTE_PATTERN:
                        if (_resourceType >= PictureType.SCI11)
                            throw new InvalidOperationException("pic-operation absolute pattern inside sci1.1+ vector data");
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            VectorGetPatternTexture(data, ref curPos, pattern_Code, ref pattern_Texture);
                            VectorGetAbsCoords(data, ref curPos, out x, out y);
                            VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        }
                        break;

                    case PictureOperation.OPX: // Extended functions
                        if (isEGA)
                        {
# if DEBUG_PICTURE_DRAW
                            debug("* Picture ex op: %X (%s) at %d", data[curPos], picExOpcodeNamesEGA[data[curPos]], curPos);
#endif
                            PictureOperationEx pic_op2;
                            switch (pic_op2 = (PictureOperationEx)data[curPos++])
                            {
                                case PictureOperationEx.EGA_SET_PALETTE_ENTRIES:
                                    while (VectorIsNonOpcode(data[curPos]))
                                    {
                                        pixel = data[curPos++];
                                        if (pixel >= PIC_EGAPALETTE_TOTALSIZE)
                                        {
                                            throw new InvalidOperationException("picture trying to write to invalid EGA-palette");
                                        }
                                        EGApalettes[pixel] = data[curPos++];
                                    }
                                    break;
                                case PictureOperationEx.EGA_SET_PALETTE:
                                    pixel = data[curPos++];
                                    if (pixel >= PIC_EGAPALETTE_COUNT)
                                    {
                                        throw new InvalidOperationException($"picture trying to write to invalid palette {pixel}");
                                    }
                                    pixel *= PIC_EGAPALETTE_SIZE;
                                    for (i = 0; i < PIC_EGAPALETTE_SIZE; i++)
                                    {
                                        EGApalettes[pixel + i] = data[curPos++];
                                    }
                                    break;
                                case PictureOperationEx.EGA_MONO0:
                                    curPos += 41;
                                    break;
                                case PictureOperationEx.EGA_MONO1:
                                case PictureOperationEx.EGA_MONO3:
                                    curPos++;
                                    break;
                                case PictureOperationEx.EGA_MONO2:
                                case PictureOperationEx.EGA_MONO4:
                                    break;
                                case PictureOperationEx.EGA_EMBEDDED_VIEW:
                                    VectorGetAbsCoordsNoMirror(data, ref curPos, ref x, ref y);
                                    size = data.ToUInt16(curPos); curPos += 2;
                                    // hardcoded in SSCI, 16 for SCI1early excluding Space Quest 4, 0 for anything else
                                    //  fixes sq4 pictures 546+547 (Vohaul's head and Roger Jr trapped). Bug #5250
                                    //  fixes sq4 picture 631 (SQ1 view from cockpit). Bug 5249
                                    if ((ResourceManager.GetSciVersion() <= SciVersion.V1_EARLY) && (SciEngine.Instance.GameId != SciGameId.SQ4))
                                    {
                                        _priority = 16;
                                    }
                                    else
                                    {
                                        _priority = 0;
                                    }
                                    DrawCelData(data, _resource.size, curPos, curPos + 8, 0, x, y, 0, 0, true);
                                    curPos += size;
                                    break;
                                case PictureOperationEx.EGA_SET_PRIORITY_TABLE:
                                    _ports.PriorityBandsInit(new ByteAccess(data, curPos));
                                    curPos += 14;
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unsupported sci1 extended pic-operation {pic_op2}");
                            }
                        }
                        else
                        {
# if DEBUG_PICTURE_DRAW
                            debug("* Picture ex op: %X (%s) at %d", data[curPos], picExOpcodeNamesVGA[data[curPos]], curPos);
#endif
                            PictureOperationEx pic_op2;
                            switch (pic_op2 = (PictureOperationEx)data[curPos++])
                            {
                                case PictureOperationEx.VGA_SET_PALETTE_ENTRIES:
                                    while (VectorIsNonOpcode(data[curPos]))
                                    {
                                        curPos++; // skip commands
                                    }
                                    break;
                                case PictureOperationEx.VGA_SET_PALETTE:
                                    if (_resMan.ViewType == ViewType.Amiga || _resMan.ViewType == ViewType.Amiga64)
                                    {
                                        if ((data[curPos] == 0x00) && (data[curPos + 1] == 0x01) && ((data[curPos + 32] & 0xF0) != 0xF0))
                                        {
                                            // Left-Over VGA palette, we simply ignore it
                                            curPos += 256 + 4 + 1024;
                                        }
                                        else
                                        {
                                            // Setting half of the Amiga palette
                                            _palette.ModifyAmigaPalette(new ByteAccess(data, curPos));
                                            curPos += 32;
                                        }
                                    }
                                    else
                                    {
                                        curPos += 256 + 4; // Skip over mapping and timestamp
                                        for (i = 0; i < 256; i++)
                                        {
                                            palette.colors[i].used = data[curPos++];
                                            palette.colors[i].r = data[curPos++]; palette.colors[i].g = data[curPos++]; palette.colors[i].b = data[curPos++];
                                        }
                                        _palette.Set(palette, true);
                                    }
                                    break;
                                case PictureOperationEx.VGA_EMBEDDED_VIEW: // draw cel
                                    VectorGetAbsCoordsNoMirror(data, ref curPos, ref x, ref y);
                                    size = data.ToUInt16(curPos); curPos += 2;
                                    if (ResourceManager.GetSciVersion() <= SciVersion.V1_EARLY)
                                    {
                                        // During SCI1Early sierra always used 0 as priority for cels inside picture resources
                                        //  fixes Space Quest 4 orange ship lifting off (bug #6446)
                                        _priority = 0;
                                    }
                                    else
                                    {
                                        _priority = pic_priority; // set global priority so the cel gets drawn using current priority as well
                                    }
                                    DrawCelData(data, _resource.size, curPos, curPos + 8, 0, x, y, 0, 0, false);
                                    curPos += size;
                                    break;
                                case PictureOperationEx.VGA_PRIORITY_TABLE_EQDIST:
                                    _ports.PriorityBandsInit(-1, data.ToInt16(curPos), data.ToInt16(curPos + 2));
                                    curPos += 4;
                                    break;
                                case PictureOperationEx.VGA_PRIORITY_TABLE_EXPLICIT:
                                    _ports.PriorityBandsInit(new ByteAccess(data, curPos));
                                    curPos += 14;
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unsupported sci1 extended pic-operation {pic_op2:X}");
                            }
                        }
                        break;
                    case PictureOperation.TERMINATE:
                        _priority = pic_priority;
                        // Dithering EGA pictures
                        if (isEGA)
                        {
                            _screen.Dither(_addToFlag);
                            switch (SciEngine.Instance.GameId)
                            {
                                case SciGameId.SQ3:
                                    switch (_resourceId)
                                    {
                                        case 154: // SQ3: intro, ship gets sucked in
                                            _screen.DitherForceDitheredColor(0xD0);
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        return;
                    default:
                        throw new InvalidOperationException("Unsupported pic-operation {pic_op:X}");
                }
                if ((_EGAdrawingVisualize) && (isEGA))
                {
                    _screen.CopyToScreen();
                    SciEngine.Instance.System.GraphicsManager.UpdateScreen();
                    ServiceLocator.Platform.Sleep(10);
                }
            }
            throw new InvalidOperationException("picture vector data without terminator");
        }

        private void VectorGetAbsCoordsNoMirror(ByteAccess data, ref int curPos, ref short x, ref short y)
        {
            byte pixel = data[curPos++];
            x = (short)(data[curPos++] + ((pixel & 0xF0) << 4));
            y = (short)(data[curPos++] + ((pixel & 0x0F) << 8));
        }

        private void VectorPattern(short x, short y, byte color, byte priority, byte control, short code, short texture)
        {
            byte size = (byte)(code & SCI_PATTERN_CODE_PENSIZE);

            // We need to adjust the given coordinates, because the ones given us do not define upper left but somewhat middle
            y -= size; if (y < 0) y = 0;
            x -= size; if (x < 0) x = 0;

            var rect = new Rect();
            rect.Top = y; rect.Left = x;
            rect.Height = (size * 2) + 1; rect.Width = (size * 2) + 2;
            _ports.OffsetRect(ref rect);
            rect.Clip(_screen.ScriptWidth, _screen.ScriptHeight);

            _screen.VectorAdjustCoordinate(ref rect.Left, ref rect.Top);
            _screen.VectorAdjustCoordinate(ref rect.Right, ref rect.Bottom);

            if ((code & SCI_PATTERN_CODE_RECTANGLE) != 0)
            {
                // Rectangle
                if ((code & SCI_PATTERN_CODE_USE_TEXTURE) != 0)
                {
                    VectorPatternTexturedBox(rect, color, priority, control, texture);
                }
                else
                {
                    VectorPatternBox(rect, color, priority, control);
                }

            }
            else
            {
                // Circle
                if ((code & SCI_PATTERN_CODE_USE_TEXTURE) != 0)
                {
                    VectorPatternTexturedCircle(rect, size, color, priority, control, texture);
                }
                else
                {
                    VectorPatternCircle(rect, size, color, priority, control);
                }
            }
        }

        private void VectorPatternCircle(Rect box, byte size, byte color, byte prio, byte control)
        {
            var flag = _screen.GetDrawingMask(color, prio, control);
            var circleData = new ByteAccess(vectorPatternCircles[size]);
            var bitmap = circleData.Value;
            byte bitNo = 0;
            int y, x;

            for (y = box.Top; y < box.Bottom; y++)
            {
                for (x = box.Left; x < box.Right; x++)
                {
                    if ((bitmap & 1) != 0)
                    {
                        _screen.VectorPutPixel((short)x, (short)y, flag, color, prio, control);
                    }
                    bitNo++;
                    if (bitNo == 8)
                    {
                        circleData.Offset++; bitmap = circleData.Value; bitNo = 0;
                    }
                    else
                    {
                        bitmap = (byte)(bitmap >> 1);
                    }
                }
            }
        }

        private void VectorPatternTexturedCircle(Rect box, byte size, byte color, byte prio, byte control, short texture)
        {
            var flag = _screen.GetDrawingMask(color, prio, control);
            var circleData = new ByteAccess(vectorPatternCircles[size]);
            byte bitmap = circleData.Value;
            byte bitNo = 0;
            var offset = vectorPatternTextureOffset[texture];
            int y, x;

            for (y = box.Top; y < box.Bottom; y++)
            {
                for (x = box.Left; x < box.Right; x++)
                {
                    if ((bitmap & 1) != 0)
                    {
                        if (vectorPatternTextures[offset])
                        {
                            _screen.VectorPutPixel((short)x, (short)y, flag, color, prio, control);
                        }
                        offset++;
                    }
                    bitNo++;
                    if (bitNo == 8)
                    {
                        circleData.Offset++; bitmap = circleData.Value; bitNo = 0;
                    }
                    else
                    {
                        bitmap = (byte)(bitmap >> 1);
                    }
                }
            }
        }

        private void VectorPatternBox(Rect box, byte color, byte prio, byte control)
        {
            var flag = _screen.GetDrawingMask(color, prio, control);
            int y, x;

            for (y = box.Top; y < box.Bottom; y++)
            {
                for (x = box.Left; x < box.Right; x++)
                {
                    _screen.VectorPutPixel((short)x, (short)y, flag, color, prio, control);
                }
            }
        }

        private void VectorPatternTexturedBox(Rect box, byte color, byte prio, byte control, short texture)
        {
            var flag = _screen.GetDrawingMask(color, prio, control);
            var offset = vectorPatternTextureOffset[texture];
            int y, x;

            for (y = box.Top; y < box.Bottom; y++)
            {
                for (x = box.Left; x < box.Right; x++)
                {
                    if (vectorPatternTextures[offset])
                    {
                        _screen.VectorPutPixel((short)x, (short)y, flag, color, prio, control);
                    }
                    offset++;
                }
            }
        }

        private void VectorGetPatternTexture(ByteAccess data, ref int curPos, short pattern_Code, ref short pattern_Texture)
        {
            if ((pattern_Code & SCI_PATTERN_CODE_USE_TEXTURE) != 0)
            {
                pattern_Texture = (short)((data[curPos++] >> 1) & 0x7f);
            }
        }

        // WARNING: Do not replace the following code with something else, like generic
        // code. This algo really needs to behave exactly as the one from sierra.
        private void VectorFloodFill(short x, short y, byte color, byte priority, byte control)
        {
            Port curPort = _ports.Port;
            Stack<Point> stack = new Stack<Point>();
            Point p, p1;
            GfxScreenMasks screenMask = _screen.GetDrawingMask(color, priority, control);
            GfxScreenMasks matchedMask, matchMask;

            bool isEGA = (_resMan.ViewType == ViewType.Ega);

            p.X = x + curPort.left;
            p.Y = y + curPort.top;

            _screen.VectorAdjustCoordinate(ref p.X, ref p.Y);

            byte searchColor = _screen.VectorGetVisual((short)p.X, (short)p.Y);
            byte searchPriority = _screen.VectorGetPriority((short)p.X, (short)p.Y);
            byte searchControl = _screen.VectorGetControl((short)p.X, (short)p.Y);

            if (isEGA)
            {
                // In EGA games a pixel in the framebuffer is only 4 bits. We store
                // a full byte per pixel to allow undithering, but when comparing
                // pixels for flood-fill purposes, we should only compare the
                // visible color of a pixel.

                if (((x ^ y) & 1) != 0)
                    searchColor = (byte)((searchColor ^ (searchColor >> 4)) & 0x0F);
                else
                    searchColor = (byte)(searchColor & 0x0F);
            }

            // This logic was taken directly from sierra sci, floodfill will get aborted on various occations
            if (screenMask.HasFlag(GfxScreenMasks.VISUAL))
            {
                if ((color == _screen.ColorWhite) || (searchColor != _screen.ColorWhite))
                    return;
            }
            else if (screenMask.HasFlag(GfxScreenMasks.PRIORITY))
            {
                if ((priority == 0) || (searchPriority != 0))
                    return;
            }
            else if (screenMask.HasFlag(GfxScreenMasks.CONTROL))
            {
                if ((control == 0) || (searchControl != 0))
                    return;
            }

            // Now remove screens, that already got the right color/priority/control
            if ((screenMask.HasFlag(GfxScreenMasks.VISUAL)) && (searchColor == color))
                screenMask &= ~GfxScreenMasks.VISUAL;
            if ((screenMask.HasFlag(GfxScreenMasks.PRIORITY)) && (searchPriority == priority))
                screenMask &= ~GfxScreenMasks.PRIORITY;
            if ((screenMask.HasFlag(GfxScreenMasks.CONTROL)) && (searchControl == control))
                screenMask &= ~GfxScreenMasks.CONTROL;

            // Exit, if no screens left
            if (screenMask == 0)
                return;

            if (screenMask.HasFlag(GfxScreenMasks.VISUAL))
            {
                matchMask = GfxScreenMasks.VISUAL;
            }
            else if (screenMask.HasFlag(GfxScreenMasks.PRIORITY))
            {
                matchMask = GfxScreenMasks.PRIORITY;
            }
            else
            {
                matchMask = GfxScreenMasks.CONTROL;
            }

            // hard borders for filling
            short borderLeft = (short)(curPort.rect.Left + curPort.left);
            short borderTop = (short)(curPort.rect.Top + curPort.top);
            short borderRight = (short)(curPort.rect.Right + curPort.left - 1);
            short borderBottom = (short)(curPort.rect.Bottom + curPort.top - 1);
            short curToLeft, curToRight, a_set, b_set;

            // Translate coordinates, if required (needed for Macintosh 480x300)
            _screen.VectorAdjustCoordinate(ref borderLeft, ref borderTop);
            _screen.VectorAdjustCoordinate(ref borderRight, ref borderBottom);
            //return;

            stack.Push(p);

            while (stack.Count != 0)
            {
                p = stack.Pop();
                if ((matchedMask = _screen.VectorIsFillMatch((short)p.X, (short)p.Y, matchMask, searchColor, searchPriority, searchControl, isEGA)) == 0) // already filled
                    continue;
                _screen.VectorPutPixel((short)p.X, (short)p.Y, screenMask, color, priority, control);
                curToLeft = (short)p.X;
                curToRight = (short)p.X;
                // moving west and east pointers as long as there is a matching color to fill
                while (curToLeft > borderLeft && (matchedMask = _screen.VectorIsFillMatch((short)(curToLeft - 1), (short)p.Y, matchMask, searchColor, searchPriority, searchControl, isEGA)) != 0)
                    _screen.VectorPutPixel(--curToLeft, (short)p.Y, screenMask, color, priority, control);
                while (curToRight < borderRight && (matchedMask = _screen.VectorIsFillMatch((short)(curToRight + 1), (short)p.Y, matchMask, searchColor, searchPriority, searchControl, isEGA)) != 0)
                    _screen.VectorPutPixel(++curToRight, (short)p.Y, screenMask, color, priority, control);
#if false
		// debug code for floodfill
		_screen.copyToScreen();
		g_system.updateScreen();
		g_system.delayMillis(100);
#endif
                // checking lines above and below for possible flood targets
                a_set = b_set = 0;
                while (curToLeft <= curToRight)
                {
                    if (p.Y > borderTop && (matchedMask = _screen.VectorIsFillMatch(curToLeft, (short)(p.Y - 1), matchMask, searchColor, searchPriority, searchControl, isEGA)) != 0)
                    { // one line above
                        if (a_set == 0)
                        {
                            p1.X = curToLeft;
                            p1.Y = p.Y - 1;
                            stack.Push(p1);
                            a_set = 1;
                        }
                    }
                    else
                        a_set = 0;

                    if (p.Y < borderBottom && (matchedMask = _screen.VectorIsFillMatch(curToLeft, (short)(p.Y + 1), matchMask, searchColor, searchPriority, searchControl, isEGA)) != 0)
                    { // one line below
                        if (b_set == 0)
                        {
                            p1.X = curToLeft;
                            p1.Y = p.Y + 1;
                            stack.Push(p1);
                            b_set = 1;
                        }
                    }
                    else
                        b_set = 0;
                    curToLeft++;
                }
            }
        }

        private void VectorGetRelCoordsMed(ByteAccess data, ref int curPos, ref short x, ref short y)
        {
            byte pixel = data[curPos++];
            if ((pixel & 0x80) != 0)
            {
                y -= (short)(pixel & 0x7F);
            }
            else
            {
                y += pixel;
            }
            pixel = data[curPos++];
            if ((pixel & 0x80) != 0)
            {
                x -= (short)((128 - (pixel & 0x7F)) * (_mirroredFlag ? -1 : 1));
            }
            else
            {
                x += (short)(pixel * (_mirroredFlag ? -1 : 1));
            }
        }

        private void VectorGetRelCoords(ByteAccess data, ref int curPos, ref short x, ref short y)
        {
            byte pixel = data[curPos++];
            if ((pixel & 0x80) != 0)
            {
                x -= (short)(((pixel >> 4) & 7) * (_mirroredFlag ? -1 : 1));
            }
            else
            {
                x += (short)((pixel >> 4) * (_mirroredFlag ? -1 : 1));
            }
            if ((pixel & 0x08) != 0)
            {
                y -= (short)(pixel & 7);
            }
            else
            {
                y += (short)(pixel & 7);
            }
        }

        private bool VectorIsNonOpcode(byte pixel)
        {
            if (pixel >= (byte)PIC_OP_FIRST)
                return false;
            return true;
        }

        private void VectorGetAbsCoords(ByteAccess data, ref int curPos, out short x, out short y)
        {
            byte pixel = data[curPos++];
            x = (short)(data[curPos++] + ((pixel & 0xF0) << 4));
            y = (short)(data[curPos++] + ((pixel & 0x0F) << 8));
            if (_mirroredFlag) x = (short)(319 - x);
        }
    }
}
