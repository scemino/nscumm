//
//  AgosEngine.Icons.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System.IO;
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AGOSEngine
    {
        private void LoadIconFile()
        {
            var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_ICONFILE));
            int srcSize;

            if (@in == null)
                Error("Can't open icons file '{0}'", GetFileName(GameFileTypes.GAME_ICONFILE));

            srcSize = (int) @in.Length;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW &&
                _gd.Platform == Platform.Amiga)
            {
                var srcBuf = new byte[srcSize];
                @in.Read(srcBuf, 0, srcSize);

                int dstSize = srcBuf.ToInt32BigEndian(srcSize - 4);
                _iconFilePtr = new byte[dstSize];
                if (_iconFilePtr == null)
                    Error("Out of icon memory");

                DecrunchFile(srcBuf, _iconFilePtr, srcSize);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN &&
                     _gd.Platform == Platform.AtariST)
            {
// The icon data is hard coded in the program file.
                _iconFilePtr = new byte[15038];
                if (_iconFilePtr == null)
                    Error("Out of icon memory");

                @in.Seek(48414, SeekOrigin.Begin);
                @in.Read(_iconFilePtr, 0, 15038);
            }
            else
            {
                _iconFilePtr = new byte[srcSize];
                if (_iconFilePtr == null)
                    Error("Out of icon memory");

                @in.Read(_iconFilePtr, 0, srcSize);
            }
            @in.Dispose();
        }

        private void LoadIconData()
        {
            LoadZone(8);
            var vpe = _vgaBufferPointers[8];

            var src = new BytePtr(vpe.vgaFile2, vpe.vgaFile2.ToInt32(8));

            _iconFilePtr = new byte[43 * 336];
            if (_iconFilePtr == null)
                Error("Out of icon memory");

            Array.Copy(src.Data, src.Offset, _iconFilePtr, 0, 43 * 336);
            UnfreezeBottom();
        }

        // Thanks to Stuart Caie for providing the original
        // C conversion upon which this function is based.
        protected void DecompressIconPlanar(BytePtr dst, BytePtr src, int i, int i1, byte color, int screenPitch)
        {
            throw new NotImplementedException();
        }

        protected void DecompressIcon(BytePtr dst, BytePtr src, int width, int height, byte @base, int pitch)
        {
            BytePtr dstOrg = dst;
            int h = height;

            for (;;)
            {
                var reps = (sbyte) src.Value;
                src.Offset++;
                byte color1;
                byte color2;
                if (reps < 0)
                {
                    reps--;
                    color1 = (byte) (src.Value >> 4);
                    if (color1 != 0)
                        color1 |= @base;
                    color2 = (byte) (src.Value & 0xF);
                    src.Offset++;
                    if (color2 != 0)
                        color2 |= @base;

                    do
                    {
                        if (color1 != 0)
                            dst.Value = color1;
                        dst += pitch;
                        if (color2 != 0)
                            dst.Value = color2;
                        dst += pitch;

                        // reached bottom?
                        if (--h == 0)
                        {
                            // reached right edge?
                            if (--width == 0)
                                return;
                            dstOrg.Offset++;
                            dst = dstOrg;
                            h = height;
                        }
                    } while (++reps != 0);
                }
                else
                {
                    do
                    {
                        color1 = (byte) (src.Value >> 4);
                        if (color1 != 0)
                            dst.Value = (byte) (color1 | @base);
                        dst += pitch;

                        color2 = (byte) (src.Value & 0xF);
                        src.Offset++;
                        if (color2 != 0)
                            dst.Value = (byte) (color2 | @base);
                        dst += pitch;

                        // reached bottom?
                        if (--h == 0)
                        {
                            // reached right edge?
                            if (--width == 0)
                                return;
                            dstOrg.Offset++;
                            dst = dstOrg;
                            h = height;
                        }
                    } while (--reps >= 0);
                }
            }
        }

        protected virtual void DrawIcon(WindowBlock window, int icon, int x, int y)
        {
            _videoLockOut |= 0x8000;

            LocksScreen(screen =>
            {
                var dst = screen.GetBasePtr(x * 8, y);
                var src = new BytePtr(_iconFilePtr, icon * 146);

                if (icon == 0xFF)
                {
                    // Draw Blank Icon
                    for (int yp = 0; yp < 24; yp++)
                    {
                        Array.Clear(dst.Data, dst.Offset, 24);
                        dst += screen.Pitch;
                    }
                }
                else
                {
                    byte[] palette = new byte[4];
                    palette[0] = (byte) (src.Value >> 4);
                    palette[1] = (byte) (src.Value & 0xf);
                    src.Offset++;
                    palette[2] = (byte) (src.Value >> 4);
                    palette[3] = (byte) (src.Value & 0xf);
                    src.Offset++;
                    for (int yp = 0; yp < 24; ++yp, src += 6)
                    {
                        // Get bit-set representing the 24 pixels for the line
                        int v1 = (src.ToUInt16BigEndian() << 8) | src[4];
                        int v2 = (src.ToUInt16BigEndian(2) << 8) | src[5];
                        for (int xp = 0; xp < 24; ++xp, v1 >>= 1, v2 >>= 1)
                        {
                            dst[yp * screen.Pitch + (23 - xp)] = palette[((v1 & 1) << 1) | (v2 & 1)];
                        }
                    }
                }
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        private void DrawIconArray(int num, Item itemRef, int line, int classMask)
        {
            Item itemPtrOrg = itemRef;
            uint width, height;
            uint k;
            bool item_again, showArrows;
            int iconSize = (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) ? 20 : 1;

            var window = _windowArray[num & 7];

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                width = 100;
                height = 40;
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
            {
                width = (uint) (window.width / 3);
                height = (uint) (window.height / 2);
            }
            else
            {
                width = (uint) (window.width / 3);
                height = (uint) (window.height / 3);
            }

            if (window == null)
                return;

            if (window.iconPtr != null)
                RemoveIconArray(num);

            window.iconPtr = new IconBlock();
            window.iconPtr.itemRef = itemRef;
            window.iconPtr.upArrow = -1;
            window.iconPtr.downArrow = -1;
            window.iconPtr.line = (short) line;
            window.iconPtr.classMask = (ushort) classMask;

            itemRef = DerefItem(itemRef.child);

            while (itemRef != null && line-- != 0)
            {
                uint curWidth = 0;
                while (itemRef != null && width > curWidth)
                {
                    if ((classMask == 0 || (itemRef.classFlags & classMask) != 0) && HasIcon(itemRef))
                        curWidth = (uint) (curWidth + iconSize);
                    itemRef = DerefItem(itemRef.next);
                }
            }

            if (itemRef == null)
            {
                window.iconPtr.line = 0;
                itemRef = DerefItem(itemPtrOrg.child);
            }

            var xPos = 0;
            var yPos = 0;
            k = 0;
            item_again = false;
            showArrows = false;

            while (itemRef != null)
            {
                if ((classMask == 0 || (itemRef.classFlags & classMask) != 0) && HasIcon(itemRef))
                {
                    if (item_again == false)
                    {
                        window.iconPtr.iconArray[k].item = itemRef;
                        if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                        {
                            DrawIcon(window, ItemGetIconNumber(itemRef), xPos, yPos);
                            window.iconPtr.iconArray[k].boxCode =
                                (ushort) SetupIconHitArea(window, 0, xPos, yPos, itemRef);
                        }
                        else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                                 _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                        {
                            DrawIcon(window, ItemGetIconNumber(itemRef), xPos * 3, yPos);
                            window.iconPtr.iconArray[k].boxCode =
                                (ushort) SetupIconHitArea(window, 0, xPos * 3, yPos, itemRef);
                        }
                        else
                        {
                            DrawIcon(window, ItemGetIconNumber(itemRef), xPos * 3, yPos * 3);
                            window.iconPtr.iconArray[k].boxCode =
                                (ushort) SetupIconHitArea(window, 0, xPos * 3, yPos * 3, itemRef);
                        }
                        k++;
                    }
                    else
                    {
                        window.iconPtr.iconArray[k].item = null;
                        showArrows = true;
                    }

                    xPos = (xPos + iconSize);
                    if (xPos >= width)
                    {
                        xPos = 0;
                        yPos = (yPos + iconSize);
                        if (yPos >= height)
                            item_again = true;
                    }
                }
                itemRef = DerefItem(itemRef.next);
            }

            window.iconPtr.iconArray[k].item = null;

            if (showArrows || window.iconPtr.line != 0)
            {
                /* Plot arrows and add their boxes */
                AddArrows(window, (uint) num);
                window.iconPtr.upArrow = (short) _scrollUpHitArea;
                window.iconPtr.downArrow = (short) _scrollDownHitArea;
            }
        }

        protected virtual int SetupIconHitArea(WindowBlock window, uint num, int x, int y, Item itemPtr)
        {
            var ha = FindEmptyHitArea();
            ha.Value.x = (ushort) ((x + window.x) * 8);
            ha.Value.y = (ushort) (y * 8 + window.y);
            ha.Value.itemPtr = itemPtr;
            ha.Value.width = 24;
            ha.Value.height = 24;
            ha.Value.flags = (BoxFlags.kBFDragBox | BoxFlags.kBFBoxInUse | BoxFlags.kBFBoxItem);
            ha.Value.id = 0x7FFD;
            ha.Value.priority = 100;
            ha.Value.verb = 253;

            return Array.IndexOf(_hitAreas, ha.Value);
        }

        protected virtual void AddArrows(WindowBlock window, uint num)
        {
            throw new NotImplementedException();
        }

        protected virtual void RemoveArrows(WindowBlock window, int num)
        {
            throw new NotImplementedException();
        }

        private void RemoveIconArray(int num)
        {
            ushort curWindow;
            ushort i;

            var window = _windowArray[num & 7];
            curWindow = _curWindow;

            if (window?.iconPtr == null)
                return;

            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_FF &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_PP)
            {
                ChangeWindow((uint) num);
                SendWindow(12);
                ChangeWindow(curWindow);
            }

            for (i = 0; window.iconPtr.iconArray[i].item != null; i++)
            {
                FreeBox(window.iconPtr.iconArray[i].boxCode);
            }

            if (window.iconPtr.upArrow != -1)
            {
                FreeBox(window.iconPtr.upArrow);
            }

            if (window.iconPtr.downArrow != -1)
            {
                FreeBox(window.iconPtr.downArrow);
                RemoveArrows(window, num);
            }

            window.iconPtr = null;

            _fcsData1[num] = 0;
            _fcsData2[num] = false;
        }
    }
}