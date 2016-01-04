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
            int vector_dataPos = inbuffer.ReadInt32(16);
            int vector_size = size - vector_dataPos;
            int palette_data_ptr = inbuffer.ReadInt32(28);
            int cel_headerPos = inbuffer.ReadInt32(32);
            int cel_RlePos = inbuffer.ReadInt32(cel_headerPos + 24);
            int cel_LiteralPos = inbuffer.ReadInt32(cel_headerPos + 28);

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

        private void DrawCelData(ByteAccess inbuffer, int size, int cel_headerPos, int cel_RlePos, int cel_LiteralPos, int v1, int v2, int v3, int v4, bool v5)
        {
            throw new NotImplementedException();
        }

        private void DrawVectorData(ByteAccess data, int dataSize)
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
                            VectorGetRelCoords(data, curPos, x, y);
                            Point startPoint = new Point(oldx, oldy);
                            Point endPoint = new Point(x, y);
                            _ports.OffsetLine(startPoint, endPoint);
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
                            VectorGetRelCoordsMed(data, curPos, x, y);
                            Point startPoint = new Point(oldx, oldy);
                            Point endPoint = new Point(x, y);
                            _ports.OffsetLine(startPoint, endPoint);
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
                            _ports.OffsetLine(startPoint, endPoint);
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
                        VectorGetPatternTexture(data, curPos, pattern_Code, pattern_Texture);
                        VectorGetAbsCoords(data, ref curPos, out x, out y);
                        VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            VectorGetPatternTexture(data, curPos, pattern_Code, pattern_Texture);
                            VectorGetRelCoords(data, curPos, x, y);
                            VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        }
                        break;
                    case PictureOperation.MEDIUM_PATTERNS:
                        if (_resourceType >= PictureType.SCI11)
                            throw new InvalidOperationException("pic-operation medium pattern inside sci1.1+ vector data");
                        VectorGetPatternTexture(data, curPos, pattern_Code, pattern_Texture);
                        VectorGetAbsCoords(data, ref curPos, out x, out y);
                        VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            VectorGetPatternTexture(data, curPos, pattern_Code, pattern_Texture);
                            VectorGetRelCoordsMed(data, curPos, x, y);
                            VectorPattern(x, y, pic_color, pic_priority, pic_control, pattern_Code, pattern_Texture);
                        }
                        break;
                    case PictureOperation.ABSOLUTE_PATTERN:
                        if (_resourceType >= PictureType.SCI11)
                            throw new InvalidOperationException("pic-operation absolute pattern inside sci1.1+ vector data");
                        while (VectorIsNonOpcode(data[curPos]))
                        {
                            VectorGetPatternTexture(data, curPos, pattern_Code, pattern_Texture);
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
                                    VectorGetAbsCoordsNoMirror(data, curPos, x, y);
                                    size = data.ReadUInt16(curPos); curPos += 2;
                                    // hardcoded in SSCI, 16 for SCI1early excluding Space Quest 4, 0 for anything else
                                    //  fixes sq4 pictures 546+547 (Vohaul's head and Roger Jr trapped). Bug #5250
                                    //  fixes sq4 picture 631 (SQ1 view from cockpit). Bug 5249
                                    if ((ResourceManager.GetSciVersion() <= SciVersion.V1_EARLY) && (SciEngine.Instance.GameId != SciGameId.SQ4))
                                    {
                                        _priority = 16;
                                    }
                                    else {
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
                        else {
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
                                        else {
                                            // Setting half of the Amiga palette
                                            _palette.ModifyAmigaPalette(new ByteAccess(data, curPos));
                                            curPos += 32;
                                        }
                                    }
                                    else {
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
                                    VectorGetAbsCoordsNoMirror(data, curPos, x, y);
                                    size = data.ReadUInt16(curPos); curPos += 2;
                                    if (ResourceManager.GetSciVersion() <= SciVersion.V1_EARLY)
                                    {
                                        // During SCI1Early sierra always used 0 as priority for cels inside picture resources
                                        //  fixes Space Quest 4 orange ship lifting off (bug #6446)
                                        _priority = 0;
                                    }
                                    else {
                                        _priority = pic_priority; // set global priority so the cel gets drawn using current priority as well
                                    }
                                    DrawCelData(data, _resource.size, curPos, curPos + 8, 0, x, y, 0, 0, false);
                                    curPos += size;
                                    break;
                                case PictureOperationEx.VGA_PRIORITY_TABLE_EQDIST:
                                    _ports.PriorityBandsInit(-1, data.ReadInt16(curPos), data.ReadInt16(curPos + 2));
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

        private void VectorGetAbsCoordsNoMirror(ByteAccess data, int curPos, short x, short y)
        {
            throw new NotImplementedException();
        }

        private void VectorPattern(short x, short y, byte pic_color, byte pic_priority, byte pic_control, short pattern_Code, short pattern_Texture)
        {
            throw new NotImplementedException();
        }

        private void VectorGetPatternTexture(ByteAccess data, int curPos, short pattern_Code, short pattern_Texture)
        {
            throw new NotImplementedException();
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
            else {
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

        private void VectorGetRelCoordsMed(ByteAccess data, int curPos, short x, short y)
        {
            throw new NotImplementedException();
        }

        private void VectorGetRelCoords(ByteAccess data, int curPos, short x, short y)
        {
            throw new NotImplementedException();
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
