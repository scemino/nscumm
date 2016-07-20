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

using System;
using System.IO;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci
{
    internal class Decompressor
    {
        /// <summary>
        /// bits buffer
        /// </summary>
        protected uint _dwBits;
        /// <summary>
        /// number of unread bits in _dwBits
        /// </summary>
        protected byte _nBits;
        /// <summary>
        /// size of the compressed data
        /// </summary>
        protected int _szPacked;
        /// <summary>
        /// size of the decompressed data
        /// </summary>
        protected int _szUnpacked;
        /// <summary>
        /// number of bytes read from _src
        /// </summary>
        protected int _dwRead;
        /// <summary>
        /// number of bytes written to _dest
        /// </summary>
        protected int _dwWrote;
        protected Stream _src;
        protected byte[] _dest;

        /// <summary>
        /// Gets a value indicationg whether or not all expected data has been unpacked to _dest
        /// and there is no more data in _src.
        /// </summary>
        protected bool IsFinished
        {
            get
            {
                return (_dwWrote == _szUnpacked) && (_dwRead >= _szPacked);
            }
        }

        public virtual ResourceErrorCodes Unpack(Stream src, byte[] dest, int nPacked, int nUnpacked)
        {
            int chunk;
            int offset = 0;
            while (nPacked != 0 && src.Position < src.Length)
            {
                chunk = Math.Min(1024, nPacked);
                src.Read(dest, offset, chunk);
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
        protected virtual void Init(Stream src, byte[] dest, int nPacked, int nUnpacked)
        {
            _src = src;
            _dest = dest;
            _szPacked = nPacked;
            _szUnpacked = nUnpacked;
            _nBits = 0;
            _dwRead = _dwWrote = 0;
            _dwBits = 0;
        }

        protected void FetchBitsLSB()
        {
            while (_nBits <= 24)
            {
                _dwBits |= (uint)(_src.ReadByte() << _nBits);
                _nBits += 8;
                _dwRead++;
            }
        }

        protected uint GetBitsLSB(int n)
        {
            // fetching more data to buffer if needed
            if (_nBits < n)
                FetchBitsLSB();
            uint ret = (uint)(_dwBits & ~((~0) << n));
            _dwBits >>= n;
            _nBits = (byte)(_nBits - n);
            return ret;
        }

        /// <summary>
        /// Write one byte into _dest stream.
        /// </summary>
        /// <param name="b">byte to put</param>
        protected virtual void PutByte(byte b)
        {
            _dest[_dwWrote++] = b;
        }

        protected uint GetBitsMSB(int n)
        {
            // fetching more data to buffer if needed
            if (_nBits < n)
                FetchBitsMSB();
            uint ret = _dwBits >> (32 - n);
            _dwBits <<= n;
            _nBits = (byte)(_nBits - n);
            return ret;
        }

        protected byte GetByteMSB()
        {
            return (byte)GetBitsMSB(8);
        }

        private void FetchBitsMSB()
        {
            while (_nBits <= 24)
            {
                _dwBits |= ((uint)_src.ReadByte()) << (24 - _nBits);
                _nBits += 8;
                _dwRead++;
            }
        }
    }

    /// <summary>
    /// LZW-like decompressor for SCI01/SCI1.
    /// </summary>
    /// <remarks>TODO: Needs clean-up of post-processing fncs</remarks>
    internal class DecompressorLZW : Decompressor
    {
        ushort _numbits;
        ushort _curtoken, _endtoken;
        ResourceCompression _compression;

        public DecompressorLZW(ResourceCompression nCompression)
        {
            _compression = nCompression;
        }

        public override ResourceErrorCodes Unpack(Stream src, byte[] dest, int nPacked, int nUnpacked)
        {
            byte[] buffer = null;

            switch (_compression)
            {
                case ResourceCompression.LZW:  // SCI0 LZW compression
                    return UnpackLZW(src, dest, nPacked, nUnpacked);
                case ResourceCompression.LZW1: // SCI01/1 LZW compression
                    return UnpackLZW1(src, dest, nPacked, nUnpacked);
                case ResourceCompression.LZW1View:
                    buffer = new byte[nUnpacked];
                    UnpackLZW1(src, buffer, nPacked, nUnpacked);
                    ReorderView(buffer, dest);
                    break;
                case ResourceCompression.LZW1Pic:
                    buffer = new byte[nUnpacked];
                    UnpackLZW1(src, buffer, nPacked, nUnpacked);
                    ReorderPic(buffer, dest, nUnpacked);
                    break;
            }

            return ResourceErrorCodes.NONE;
        }

        private void ReorderPic(byte[] buffer, byte[] dest, int nUnpacked)
        {
            throw new NotImplementedException();
        }

        private void ReorderView(byte[] buffer, byte[] dest)
        {
            throw new NotImplementedException();
        }

        private ResourceErrorCodes UnpackLZW1(Stream src, byte[] dest, int nPacked, int nUnpacked)
        {
            throw new NotImplementedException();
        }

        private ResourceErrorCodes UnpackLZW(Stream src, byte[] dest, int nPacked, int nUnpacked)
        {
            Init(src, dest, nPacked, nUnpacked);

            ushort token; // The last received value
            ushort tokenlastlength = 0;

            var tokenlist = new ushort[4096]; // pointers to dest[]
            var tokenlengthlist = new ushort[4096]; // char length of each token

            while (!IsFinished)
            {
                token = (ushort)GetBitsLSB(_numbits);

                if (token == 0x101)
                {
                    return 0; // terminator
                }

                if (token == 0x100)
                { // reset command
                    _numbits = 9;
                    _endtoken = 0x1FF;
                    _curtoken = 0x0102;
                }
                else {
                    if (token > 0xff)
                    {
                        if (token >= _curtoken)
                        {
                            Warning($"unpackLZW: Bad token {token:X}");
                            return ResourceErrorCodes.DECOMPRESSION_ERROR;
                        }
                        tokenlastlength = (ushort)(tokenlengthlist[token] + 1);
                        if (_dwWrote + tokenlastlength > _szUnpacked)
                        {
                            // For me this seems a normal situation, It's necessary to handle it
                            Warning($"unpackLZW: Trying to write beyond the end of array(len={_szUnpacked}, destctr={_dwWrote}, tok_len={tokenlastlength})");
                            for (int i = 0; _dwWrote < _szUnpacked; i++)
                                PutByte(dest[tokenlist[token] + i]);
                        }
                        else
                            for (int i = 0; i < tokenlastlength; i++)
                                PutByte(dest[tokenlist[token] + i]);
                    }
                    else {
                        tokenlastlength = 1;
                        if (_dwWrote >= _szUnpacked)
                        {
                            Warning("unpackLZW: Try to write single byte beyond end of array");
                        }
                        else
                            PutByte((byte)token);
                    }
                    if (_curtoken > _endtoken && _numbits < 12)
                    {
                        _numbits++;
                        _endtoken = (ushort)((_endtoken << 1) + 1);
                    }
                    if (_curtoken <= _endtoken)
                    {
                        tokenlist[_curtoken] = (ushort)(_dwWrote - tokenlastlength);
                        tokenlengthlist[_curtoken] = tokenlastlength;
                        _curtoken++;
                    }

                }
            }

            return _dwWrote == _szUnpacked ? 0 : ResourceErrorCodes.DECOMPRESSION_ERROR;
        }

        protected override void Init(Stream src, byte[] dest, int nPacked, int nUnpacked)
        {
            base.Init(src, dest, nPacked, nUnpacked);

            _numbits = 9;
            _curtoken = 0x102;
            _endtoken = 0x1ff;
        }
    }

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

            numnodes = (byte)_src.ReadByte();
            terminator = (ushort)(_src.ReadByte() | 0x100);
            _nodes = new byte[numnodes << 1];
            _src.Read(_nodes, 0, numnodes << 1);

            while ((c = Getc2()) != terminator && (c >= 0) && !IsFinished)
                PutByte((byte)c);

            _nodes = null;
            return _dwWrote == _szUnpacked ? ResourceErrorCodes.NONE : ResourceErrorCodes.IO_ERROR;
        }

        private short Getc2()
        {
            var node = new ByteAccess(_nodes);
            short next;
            while (node[1] != 0)
            {
                if (GetBitsMSB(1) != 0)
                {
                    next = (short)(node[1] & 0x0F); // use lower 4 bits
                    if (next == 0)
                        return (short)(GetByteMSB() | 0x100);
                }
                else
                    next = (short)(node[1] >> 4); // use higher 4 bits
                node.Offset += next << 1;
            }
            return (short)(node.Value | (node[1] << 8));
        }
    }
}