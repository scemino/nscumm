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

using NScumm.Core;
using System;
using System.IO;

namespace NScumm.Sci
{
    internal class Decompressor
    {
        public const int PicOpxEmbeddedView = 1;
        public const int PicOpxSetPalette = 2;
        public const int PicOpOpx = 0xfe;

        /// <summary>
        /// bits buffer
        /// </summary>
        protected uint DwBits;
        /// <summary>
        /// number of unread bits in _dwBits
        /// </summary>
        protected byte NBits;
        /// <summary>
        /// size of the compressed data
        /// </summary>
        protected int SzPacked;
        /// <summary>
        /// size of the decompressed data
        /// </summary>
        protected int SzUnpacked;
        /// <summary>
        /// number of bytes read from _src
        /// </summary>
        protected int DwRead;
        /// <summary>
        /// number of bytes written to _dest
        /// </summary>
        protected int DwWrote;
        protected Stream Src;
        protected BytePtr Dest;

        /// <summary>
        /// Gets a value indicationg whether or not all expected data has been unpacked to _dest
        /// and there is no more data in _src.
        /// </summary>
        protected bool IsFinished => (DwWrote == SzUnpacked) && (DwRead >= SzPacked);

        public virtual ResourceErrorCodes Unpack(Stream src, BytePtr dest, int nPacked, int nUnpacked)
        {
            var offset = 0;
            while (nPacked != 0 && src.Position < src.Length)
            {
                var chunk = Math.Min(1024, nPacked);
                src.Read(dest.Data, dest.Offset + offset, chunk);
                nPacked -= chunk;
                offset += chunk;
            }
            return ResourceErrorCodes.NONE;
        }


        /// <summary>
        /// Initialize decompressor.
        /// </summary>
        /// <param name="src">source stream to read from</param>
        /// <param name="dest">destination stream to write to</param>
        /// <param name="nPacked">size of packed data</param>
        /// <param name="nUnpacked">size of unpacked data</param>
        protected virtual void Init(Stream src, BytePtr dest, int nPacked, int nUnpacked)
        {
            Src = src;
            Dest = dest;
            SzPacked = nPacked;
            SzUnpacked = nUnpacked;
            NBits = 0;
            DwRead = DwWrote = 0;
            DwBits = 0;
        }

        protected void FetchBitsLsb()
        {
            while (NBits <= 24)
            {
                DwBits |= (uint)(Src.ReadByte() << NBits);
                NBits += 8;
                DwRead++;
            }
        }

        protected uint GetBitsLsb(int n)
        {
            // fetching more data to buffer if needed
            if (NBits < n)
                FetchBitsLsb();
            uint ret = (uint)(DwBits & ~(~0 << n));
            DwBits >>= n;
            NBits = (byte)(NBits - n);
            return ret;
        }

        /// <summary>
        /// Write one byte into _dest stream.
        /// </summary>
        /// <param name="b">byte to put</param>
        protected virtual void PutByte(byte b)
        {
            Dest[DwWrote++] = b;
        }

        protected uint GetBitsMsb(int n)
        {
            // fetching more data to buffer if needed
            if (NBits < n)
                FetchBitsMsb();
            uint ret = DwBits >> (32 - n);
            DwBits <<= n;
            NBits = (byte)(NBits - n);
            return ret;
        }

        protected byte GetByteMsb()
        {
            return (byte)GetBitsMsb(8);
        }

        private void FetchBitsMsb()
        {
            while (NBits <= 24)
            {
                var src = (uint)Src.ReadByte();
                if (src == uint.MaxValue)
                {
                    src = 0;
                }
                DwBits |= src << (24 - NBits);
                NBits += 8;
                DwRead++;
            }
        }
    }
}