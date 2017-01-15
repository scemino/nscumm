//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal class UnpackContext
    {
        public ushort Size;
        public uint Crc;
        public uint Chk;
        public int Datasize;
    }

    internal class Bank
    {
        private BytePtr _iBuf, _oBuf, _startBuf;
        private readonly UnpackContext _unpCtx;

        public Bank()
        {
            _unpCtx = new UnpackContext();
        }

        public bool Read(MemEntry me, BytePtr buf)
        {
            var bankName = $"bank{me.BankId:X2}";
            var f = Engine.OpenFileRead(bankName);

            if (f == null)
                Error("Bank::read() unable to open '{0}'", bankName);

            bool ret;
            f.Seek(me.BankOffset, SeekOrigin.Begin);

            // Depending if the resource is packed or not we
            // can read directly or unpack it.
            if (me.PackedSize == me.Size)
            {
                f.Read(buf.Data, buf.Offset, me.PackedSize);
                ret = true;
            }
            else
            {
                f.Read(buf.Data, buf.Offset, me.PackedSize);
                _startBuf = buf;
                _iBuf = buf + me.PackedSize - 4;
                ret = Unpack();
            }
            f.Dispose();

            return ret;
        }

        private bool NextChunk()
        {
            bool cf = Rcr(false);
            if (_unpCtx.Chk != 0) return cf;
            //assert(_iBuf >= _startBuf);
            _unpCtx.Chk = _iBuf.ToUInt32BigEndian();
            _iBuf -= 4;
            _unpCtx.Crc ^= _unpCtx.Chk;
            cf = Rcr(true);
            return cf;
        }

        private bool Rcr(bool cf)
        {
            bool rCf = (_unpCtx.Chk & 1) != 0;
            _unpCtx.Chk >>= 1;
            if (cf) _unpCtx.Chk |= 0x80000000;
            return rCf;
        }

        private void DecUnk1(byte numChunks, byte addCount)
        {
            ushort count = (ushort) (GetCode(numChunks) + addCount + 1);
            //Debug(DebugLevels.DBG_BANK, "Bank::decUnk1({0}, {1}) count={2}", numChunks, addCount, count);
            _unpCtx.Datasize -= count;
            while (count-- != 0)
            {
                //assert(_oBuf >= _iBuf && _oBuf >= _startBuf);
                _oBuf.Value = (byte) GetCode(8);
                --_oBuf.Offset;
            }
        }

        private void DecUnk2(byte numChunks)
        {
            ushort i = GetCode(numChunks);
            ushort count = (ushort) (_unpCtx.Size + 1);
            //Debug(DebugLevels.DBG_BANK, "Bank::decUnk2({0}) i={1} count={2}", numChunks, i, count);
            _unpCtx.Datasize -= count;
            while (count-- != 0)
            {
                //assert(_oBuf >= _iBuf && _oBuf >= _startBuf);
                _oBuf.Value = (_oBuf + i).Value;
                --_oBuf.Offset;
            }
        }

        private ushort GetCode(byte numChunks)
        {
            ushort c = 0;
            while (numChunks-- != 0)
            {
                c <<= 1;
                if (NextChunk())
                {
                    c |= 1;
                }
            }
            return c;
        }

        /// <summary>
        /// Most resource in the banks are compacted.
        /// </summary>
        /// <returns></returns>
        private bool Unpack()
        {
            _unpCtx.Size = 0;
            _unpCtx.Datasize = _iBuf.ToInt32BigEndian();
            _iBuf -= 4;
            _oBuf = _startBuf + _unpCtx.Datasize - 1;
            _unpCtx.Crc = _iBuf.ToUInt32BigEndian();
            _iBuf -= 4;
            _unpCtx.Chk = _iBuf.ToUInt32BigEndian();
            _iBuf -= 4;
            _unpCtx.Crc ^= _unpCtx.Chk;
            do
            {
                if (!NextChunk())
                {
                    _unpCtx.Size = 1;
                    if (!NextChunk())
                    {
                        DecUnk1(3, 0);
                    }
                    else
                    {
                        DecUnk2(8);
                    }
                }
                else
                {
                    ushort c = GetCode(2);
                    if (c == 3)
                    {
                        DecUnk1(8, 8);
                    }
                    else
                    {
                        if (c < 2)
                        {
                            _unpCtx.Size = (ushort) (c + 2);
                            DecUnk2((byte) (c + 9));
                        }
                        else
                        {
                            _unpCtx.Size = GetCode(8);
                            DecUnk2(12);
                        }
                    }
                }
            } while (_unpCtx.Datasize > 0);
            return _unpCtx.Crc == 0;
        }
    }
}