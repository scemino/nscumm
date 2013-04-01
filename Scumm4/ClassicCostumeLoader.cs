/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class ClassicCostumeLoader : ICostumeLoader
    {
        public int _id;
        public long _baseptr;
        public long _animCmds;
        public long _dataOffsets;
        public byte[] _palette;
        public long _frameOffsets;
        public byte _numColors;
        public byte _numAnim;
        public byte _format;
        public bool _mirror;
        public XorReader _costumeReader;

        private ScummIndex _vm;

        public ClassicCostumeLoader(ScummIndex vm)
        {
            _vm = vm;
            _id = -1;
        }

        public void LoadCostume(int id)
        {
            _costumeReader = _vm.GetCostumeReader((byte)id);

            _baseptr = _costumeReader.BaseStream.Position - 6;
            _numAnim = _costumeReader.ReadByte();
            _format = (byte)(_costumeReader.PeekByte() & 0x7F);
            _mirror = (_costumeReader.ReadByte() & 0x80) != 0;

            switch (_format)
            {
                case 0x57:				// Only used in V1 games
                    _numColors = 0;
                    break;
                case 0x58:
                    _numColors = 16;
                    break;
                case 0x59:
                    _numColors = 32;
                    break;
                case 0x60:				// New since version 6
                    _numColors = 16;
                    break;
                case 0x61:				// New since version 6
                    _numColors = 32;
                    break;
                default:
                    throw new NotSupportedException(string.Format("Costume {0} with format 0x{1:X2} is invalid", id, _format));
            }

            _palette = _costumeReader.ReadBytes(_numColors);
            _frameOffsets = _costumeReader.BaseStream.Position + 2;
            if (_format == 0x57)
            {
                _dataOffsets = _costumeReader.BaseStream.Position + 18;
                _baseptr += 4;
            }
            else
            {
                _dataOffsets = _costumeReader.BaseStream.Position + 34;
            }
            _animCmds = _baseptr + _costumeReader.ReadUInt16();
        }

        public void CostumeDecodeData(Actor a, int frame, uint usemask)
        {
            long r;
            long baseptr;
            uint mask, j;
            int i;
            byte extra, cmd;
            int anim;

            LoadCostume(a._costume);

            anim = ScummHelper.NewDirToOldDir(a.GetFacing()) + frame * 4;

            if (anim > _numAnim)
            {
                return;
            }

            baseptr = _baseptr;

            _costumeReader.BaseStream.Seek(_dataOffsets + anim * 2, System.IO.SeekOrigin.Begin);
            r = baseptr + _costumeReader.ReadUInt16();
            if (r == baseptr)
            {
                return;
            }

            _costumeReader.BaseStream.Seek(r, System.IO.SeekOrigin.Begin);
            mask = _costumeReader.ReadUInt16();

            i = 0;
            do
            {
                if ((mask & 0x8000) != 0)
                {
                    j = _costumeReader.ReadUInt16();

                    if ((usemask & 0x8000) != 0)
                    {
                        if (j == 0xFFFF)
                        {
                            a._cost.curpos[i] = 0xFFFF;
                            a._cost.start[i] = 0;
                            a._cost.frame[i] = (ushort)frame;
                        }
                        else
                        {
                            extra = _costumeReader.ReadByte();
                            cmd = ReadAnimCommand((int)j);
                            if (cmd == 0x7A)
                            {
                                a._cost.stopped &= (ushort)(~(1 << i));
                            }
                            else if (cmd == 0x79)
                            {
                                a._cost.stopped |= (ushort)(1 << i);
                            }
                            else
                            {
                                a._cost.curpos[i] = a._cost.start[i] = (ushort)j;
                                a._cost.end[i] = (ushort)(j + (extra & 0x7F));
                                if ((extra & 0x80) > 0)
                                    a._cost.curpos[i] |= 0x8000;
                                a._cost.frame[i] = (ushort)frame;
                            }
                        }
                    }
                    else
                    {
                        if (j != 0xFFFF)
                        {
                            _costumeReader.ReadByte();
                        }
                    }
                }
                i++;
                usemask <<= 1;
                mask <<= 1;
            } while ((mask & 0xFFFF) > 0);
        }

        private byte ReadAnimCommand(int j)
        {
            byte cmd;
            long r = _costumeReader.BaseStream.Position;
            _costumeReader.BaseStream.Seek(_animCmds + j, System.IO.SeekOrigin.Begin);
            cmd = _costumeReader.ReadByte();
            _costumeReader.BaseStream.Seek(r, System.IO.SeekOrigin.Begin);
            return cmd;
        }

        public int IncreaseAnims(Actor a)
        {
            int r = 0;

            for (int i = 0; i != 16; i++)
            {
                if (a._cost.curpos[i] != 0xFFFF)
                    r += IncreaseAnim(a, i) ? 1 : 0;
            }
            return r;
        }

        protected bool IncreaseAnim(Actor a, int slot)
        {
            int highflag;
            int i, end;
            byte code, nc;

            if (a._cost.curpos[slot] == 0xFFFF)
                return false;

            highflag = a._cost.curpos[slot] & 0x8000;
            i = a._cost.curpos[slot] & 0x7FFF;
            end = a._cost.end[slot];
            code = (byte)(ReadAnimCommand(i) & 0x7F);

            do
            {
                if (highflag == 0)
                {
                    if (i++ >= end)
                        i = a._cost.start[slot];
                }
                else
                {
                    if (i != end)
                        i++;
                }
                nc = ReadAnimCommand(i);

                if (nc == 0x7C)
                {
                    a._cost.animCounter++;
                    if (a._cost.start[slot] != end)
                        continue;
                }
                else
                {

                    if (nc == 0x78)
                    {
                        a._cost.soundCounter++;
                        if (a._cost.start[slot] != end)
                            continue;
                    }
                }

                a._cost.curpos[slot] = (ushort)(i | highflag);
                return (ReadAnimCommand(i) & 0x7F) != code;
            } while (true);
        }
    }
}
