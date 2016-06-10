//
//  QueenEngine.cs
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
using NScumm.Core;
using NScumm.Core.IO;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    class PackedBank
    {
        private const int MAX_BANK_SIZE = 110;

        public int[] indexes = new int[MAX_BANK_SIZE];
        public byte[] data;
        public string name;

        public void Reset()
        {
            Array.Clear(indexes, 0, indexes.Length);
            if (data != null)
                Array.Clear(data, 0, data.Length);
            name = null;
        }
    }

    public class BankManager
    {
        private const int MAX_BANKS_NUMBER = 18;
        private const int MAX_FRAMES_NUMBER = 256;

        Resource _res;
        PackedBank[] _banks;
        BobFrame[] _frames;

        public BankManager(Resource resource)
        {
            _res = resource;
            _banks = new PackedBank[MAX_BANKS_NUMBER];
            for (int i = 0; i < _banks.Length; i++)
            {
                _banks[i] = new PackedBank();
            }
            _frames = new BobFrame[MAX_FRAMES_NUMBER];
            for (int i = 0; i < _frames.Length; i++)
            {
                _frames[i] = new BobFrame();
            }
        }

        public void EraseFrame(ushort index)
        {
            D.Debug(9, $"BankManager::eraseFrame({index})");
            // TODO: assert(index < MAX_FRAMES_NUMBER);
            BobFrame bf = _frames[index];
            bf.Reset();
        }

        public void EraseFrames(bool joe)
        {
            for (var i = joe ? 0 : Defines.FRAMES_JOE; i < MAX_FRAMES_NUMBER; ++i)
            {
                EraseFrame(i);
            }
        }

        private void EraseFrame(int index)
        {
            D.Debug(9, $"BankManager::eraseFrame({index})");
            // TODO: assert(index < MAX_FRAMES_NUMBER);
            BobFrame bf = _frames[index];
            bf.Reset();
        }

        public void Load(string bankname, uint bankslot)
        {
            D.Debug(9, $"BankManager::load({bankname}, {bankslot})");

            // assert(bankslot < MAX_BANKS_NUMBER);
            PackedBank bank = _banks[bankslot];

            if (string.Equals(bankname, bank.name, StringComparison.OrdinalIgnoreCase))
            {
                D.Debug(9, $"BankManager::load() bank '{bankname}' already loaded", bankname);
                return;
            }

            Close(bankslot);

            if (_res.Platform == Platform.Amiga && !_res.FileExists(bankname))
            {
                D.Debug(9, $"BankManager::load() bank '{bankname}' doesn't exist");
                return;
            }

            bank.data = _res.LoadFile(bankname);

            if (_res.Platform == Platform.Amiga)
            {
                ushort entries = bank.data.ToUInt16BigEndian(4);

                D.Debug(9, $"BankManager::load() entries = {entries}");
                // TODO: assert(entries < MAX_BANK_SIZE);
                int offset = 6;
                _banks[bankslot].indexes[0] = offset;
                for (ushort i = 1; i <= entries; ++i)
                {
                    _banks[bankslot].indexes[i] = offset;
                    ushort dataSize = bank.data.ToUInt16BigEndian(offset + 10);
                    offset += dataSize + 12;
                }
            }
            else
            {
                ushort entries = bank.data.ToUInt16();
                D.Debug(9, $"BankManager::load() entries = {entries}");
                // TODO: assert(entries < MAX_BANK_SIZE);
                int offset = 2;
                _banks[bankslot].indexes[0] = offset;
                for (ushort i = 1; i <= entries; ++i)
                {
                    _banks[bankslot].indexes[i] = offset;
                    ushort w = bank.data.ToUInt16(offset + 0);
                    ushort h = bank.data.ToUInt16(offset + 2);
                    offset += w * h + 8;
                }
            }

            // mark this bank as loaded
            bank.name = bankname;
        }

        public void Close(int bankslot)
        {
            D.Debug(9, $"BankManager::close({bankslot})");
            // TODO: assert(bankslot < MAX_BANKS_NUMBER);
            var bank = _banks[bankslot];
            bank.Reset();
        }

        public void Unpack(uint srcframe, uint dstframe, uint bankslot)
        {
            D.Debug(9, $"BankManager::unpack({srcframe}, {dstframe}, {bankslot})");

            // assert(bankslot < MAX_BANKS_NUMBER);
            PackedBank bank = _banks[bankslot];
            // assert(bank.data != NULL);

            // assert(dstframe < MAX_FRAMES_NUMBER);
            BobFrame bf = _frames[dstframe];
            bf.data = null;

            var p = bank.indexes[srcframe];

            if (_res.Platform == Platform.Amiga)
            {
                ushort w = bank.data.ToUInt16BigEndian(p + 0);
                ushort h = bank.data.ToUInt16BigEndian(p + 2);
                ushort plane = bank.data.ToUInt16BigEndian(p + 4);
                bf.xhotspot = bank.data.ToUInt16BigEndian(p + 6);
                bf.yhotspot = bank.data.ToUInt16BigEndian(p + 8);
                bf.width = (ushort)(w * 16);
                bf.height = h;

                uint size = (uint)(bf.width * bf.height);
                if (size != 0)
                {
                    bf.data = new byte[size];
                    ConvertPlanarBitmap(bf.data, bf.width, bank.data, p + 12, w, h, plane);
                }
            }
            else
            {
                bf.width = bank.data.ToUInt16(p + 0);
                bf.height = bank.data.ToUInt16(p + 2);
                bf.xhotspot = bank.data.ToUInt16(p + 4);
                bf.yhotspot = bank.data.ToUInt16(p + 6);

                int size = bf.width * bf.height;
                if (size != 0)
                {
                    bf.data = new byte[size];
                    Array.Copy(bank.data, p + 8, bf.data, 0, size);
                }
            }
        }

        public BobFrame FetchFrame(uint index)
        {
            D.Debug(9, $"BankManager::fetchFrame({index})");
            // TODO: assert(index < MAX_FRAMES_NUMBER);
            BobFrame bf = _frames[index];
            // TODO: assert((bf.width == 0 && bf.height == 0) || bf.data != 0);
            return bf;
        }

        private static void ConvertPlanarBitmap(byte[] dst, int dstPitch, byte[] src, int srcPos, int w, int h, int plane)
        {
            // assert(w != 0 && h != 0);
            int planarSize = plane * h * w * 2;
            byte[] planarBuf = new byte[planarSize];
            var dstPlanar = 0;
            while (planarSize > 0)
            {
                if (src[srcPos] == 0)
                {
                    int count = src[srcPos + 1];
                    Array.Clear(planarBuf, dstPlanar, count);
                    dstPlanar += count;
                    srcPos += 2;
                    planarSize -= count;
                }
                else
                {
                    planarBuf[dstPlanar++] = src[srcPos++];
                    --planarSize;
                }
            }

            src = planarBuf;
            srcPos = 0;
            var dstPos = 0;
            int i = 0;
            int planeSize = h * w * 2;
            while ((h--) != 0)
            {
                for (int x = 0; x < w * 2; ++x)
                {
                    for (int b = 0; b < 8; ++b)
                    {
                        byte mask = (byte)(1 << (7 - b));
                        byte color = 0;
                        for (int p = 0; p < plane; ++p)
                        {
                            if ((src[planeSize * p + i] & mask) != 0)
                            {
                                color |= (byte)(1 << p);
                            }
                        }
                        dst[dstPos + 8 * x + b] = color;
                    }
                    ++i;
                }
                dstPos += dstPitch;
            }
        }

        private void Close(uint bankslot)
        {
            D.Debug(9, $"BankManager::close({bankslot})");
            // assert(bankslot < MAX_BANKS_NUMBER);
            var bank = _banks[bankslot];
            bank.Reset();
        }

        public void Overpack(uint srcframe, uint dstframe, uint bankslot)
        {
            D.Debug(9, $"BankManager::overpack({srcframe}, {dstframe}, {bankslot})");

            PackedBank bank = _banks[bankslot];
            BobFrame bf = _frames[dstframe];

            var ptr = bank.data;
            int p = bank.indexes[srcframe];

            if (_res.Platform == Platform.Amiga)
            {
                ushort w = ptr.ToUInt16BigEndian(p + 0); p += 2;
                ushort h = ptr.ToUInt16BigEndian(p + 2); p += 2;
                ushort plane = ptr.ToUInt16BigEndian(p + 4); p += 2;
                ushort src_w = (ushort)(w * 16);
                ushort src_h = h;

                if (bf.width < src_w || bf.height < src_h)
                {
                    Unpack(srcframe, dstframe, bankslot);
                }
                else
                {
                    ConvertPlanarBitmap(bf.data, bf.width, ptr, p + 12, w, h, plane);
                }
            }
            else
            {
                ushort src_w = ptr.ToUInt16(p + 0); p += 2;
                ushort src_h = ptr.ToUInt16(p + 2); p += 2;

                // unpack if destination frame is smaller than source
                if (bf.width < src_w || bf.height < src_h)
                {
                    Unpack(srcframe, dstframe, bankslot);
                }
                else
                {
                    // copy data 'over' destination frame (without updating frame header)
                    Array.Copy(ptr, p + 8, bf.data, 0, src_w * src_h);
                }
            }
        }

    }
}


