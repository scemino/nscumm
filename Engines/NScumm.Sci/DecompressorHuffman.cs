//
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

using System.IO;
using NScumm.Core;

namespace NScumm.Sci
{
    /// <summary>
    /// Huffman decompressor
    /// </summary>
    internal class DecompressorHuffman : Decompressor
    {
        private byte[] _nodes;

        public override ResourceErrorCodes Unpack(Stream src, byte[] dest, int nPacked, int nUnpacked)
        {
            Init(src, dest, nPacked, nUnpacked);
            byte numnodes;
            short c;
            ushort terminator;

            numnodes = (byte)Src.ReadByte();
            terminator = (ushort)(Src.ReadByte() | 0x100);
            _nodes = new byte[numnodes << 1];
            Src.Read(_nodes, 0, numnodes << 1);

            while ((c = Getc2()) != terminator && (c >= 0) && !IsFinished)
                PutByte((byte)c);

            _nodes = null;
            return DwWrote == SzUnpacked ? ResourceErrorCodes.NONE : ResourceErrorCodes.IO_ERROR;
        }

        private short Getc2()
        {
            var node = new ByteAccess(_nodes);
            while (node[1] != 0)
            {
                short next;
                if (GetBitsMsb(1) != 0)
                {
                    next = (short)(node[1] & 0x0F); // use lower 4 bits
                    if (next == 0)
                        return (short)(GetByteMsb() | 0x100);
                }
                else
                    next = (short)(node[1] >> 4); // use higher 4 bits
                node.Offset += next << 1;
            }
            return (short)(node.Value | (node[1] << 8));
        }
    }
}