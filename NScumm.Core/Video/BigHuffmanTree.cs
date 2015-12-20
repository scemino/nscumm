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
    internal class BigHuffmanTree
    {
        const uint SMK_NODE = 0x80000000;
        private BitStream _bs;
        private uint[] _tree;
        private uint[] _last = new uint[3];
        private uint[] _prefixtree = new uint[256];
        private byte[] _prefixlength = new byte[256];
        private uint _treeSize;
        private uint[] _markers = new uint[3];
        private SmallHuffmanTree _loBytes;
        private SmallHuffmanTree _hiBytes;

        public BigHuffmanTree(BitStream bs, int allocSize)
        {
            _bs = bs;
            uint bit = _bs.GetBit();
            if (bit == 0)
            {
                _tree = new uint[1];
                _tree[0] = 0;
                _last[0] = _last[1] = _last[2] = 0;
                return;
            }

            for (uint i = 0; i < 256; ++i)
                _prefixtree[i] = _prefixlength[i] = 0;

            _loBytes = new SmallHuffmanTree(_bs);
            _hiBytes = new SmallHuffmanTree(_bs);

            _markers[0] = _bs.GetBits(16);
            _markers[1] = _bs.GetBits(16);
            _markers[2] = _bs.GetBits(16);

            _last[0] = _last[1] = _last[2] = 0xffffffff;

            _treeSize = 0;
            _tree = new uint[allocSize / 4];
            DecodeTree(0, 0);
            bit = _bs.GetBit();
            Debug.Assert(bit == 0);

            for (uint i = 0; i < 3; ++i)
            {
                if (_last[i] == 0xffffffff)
                {
                    _last[i] = _treeSize;
                    _tree[_treeSize++] = 0;
                }
            }
        }

        private uint DecodeTree(uint prefix, int length)
        {
            uint bit = _bs.GetBit();

            if (bit == 0)
            { // Leaf
                uint lo = _loBytes.GetCode(_bs);
                uint hi = _hiBytes.GetCode(_bs);

                uint v = (hi << 8) | lo;

                _tree[_treeSize] = v;

                if (length <= 8)
                {
                    for (uint i = 0; i < 256; i += (uint)(1 << length))
                    {
                        _prefixtree[prefix | i] = _treeSize;
                        _prefixlength[prefix | i] = (byte)length;
                    }
                }

                for (int i = 0; i < 3; ++i)
                {
                    if (_markers[i] == v)
                    {
                        _last[i] = _treeSize;
                        _tree[_treeSize] = 0;
                    }
                }
                ++_treeSize;

                return 1;
            }

            uint t = _treeSize++;

            if (length == 8)
            {
                _prefixtree[prefix] = t;
                _prefixlength[prefix] = 8;
            }

            uint r1 = DecodeTree(prefix, length + 1);

            _tree[t] = SMK_NODE | r1;

            uint r2 = DecodeTree((uint)(prefix | (1 << length)), length + 1);
            return r1 + r2 + 1;
        }

        public void Reset()
        {
            _tree[_last[0]] = _tree[_last[1]] = _tree[_last[2]] = 0;
        }

        public uint GetCode(BitStream bs)
        {
            byte peek = (byte)bs.PeekBits(Math.Min(bs.Size - bs.Position, 8));
            var p = _prefixtree[peek];
            bs.Skip(_prefixlength[peek]);

            while ((_tree[p] & SMK_NODE) != 0)
            {
                if (bs.GetBit() != 0)
                    p += _tree[p] & ~SMK_NODE;
                p++;
            }

            uint v = _tree[p];
            if (v != _tree[_last[0]])
            {
                _tree[_last[2]] = _tree[_last[1]];
                _tree[_last[1]] = _tree[_last[0]];
                _tree[_last[0]] = v;
            }

            return v;
        }
    }
}