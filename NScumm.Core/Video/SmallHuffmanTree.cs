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

using System;
using System.Diagnostics;

namespace NScumm.Core.Video
{
    internal class SmallHuffmanTree
    {
        private const int SMK_NODE = 0x8000;

        private BitStream _bs;
        ushort _treeSize;
        ushort[] _tree = new ushort[511];
        ushort[] _prefixtree = new ushort[256];
        byte[] _prefixlength = new byte[256];

        public SmallHuffmanTree(BitStream bs)
        {
            _bs = bs;
            uint bit = _bs.GetBit();
            Debug.Assert(bit != 0);

            for (ushort i = 0; i < 256; ++i)
                _prefixtree[i] = _prefixlength[i] = 0;

            DecodeTree(0, 0);

            bit = _bs.GetBit();
            Debug.Assert(bit == 0);
        }

        public ushort GetCode(BitStream bs)
        {
            byte peek = (byte)bs.PeekBits(Math.Min(bs.Size - bs.Position, 8));
            int p = _prefixtree[peek];
            bs.Skip(_prefixlength[peek]);

            while ((_tree[p] & SMK_NODE) != 0)
            {
                if (bs.GetBit() != 0)
                    p += (_tree[p] & ~SMK_NODE);
                p++;
            }

            return _tree[p];
        }

        private ushort DecodeTree(uint prefix, int length)
        {
            if (_bs.GetBit() == 0)
            { // Leaf
                _tree[_treeSize] = (ushort)_bs.GetBits(8);

                if (length <= 8)
                {
                    for (int i = 0; i < 256; i += (1 << length))
                    {
                        _prefixtree[prefix | i] = _treeSize;
                        _prefixlength[prefix | i] = (byte)length;
                    }
                }
                ++_treeSize;

                return 1;
            }

            ushort t = _treeSize++;

            if (length == 8)
            {
                _prefixtree[prefix] = t;
                _prefixlength[prefix] = 8;
            }

            ushort r1 = DecodeTree(prefix, length + 1);

            _tree[t] = (ushort)(SMK_NODE | r1);

            ushort r2 = DecodeTree((uint)(prefix | (1 << length)), length + 1);

            return (ushort)(r1 + r2 + 1);
        }
    }
}