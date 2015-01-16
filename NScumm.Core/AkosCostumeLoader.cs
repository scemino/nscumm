//
//  AkosCostumeLoader.cs
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
using System.Diagnostics;
using NScumm.Core.IO;
using System.IO;
using System.Runtime.InteropServices;

namespace NScumm.Core
{
    class AkosCostumeLoader: ICostumeLoader
    {
        public AkosCostumeLoader(ScummEngine vm)
        {
            this.vm = vm;
        }

        public void LoadCostume(int id)
        {
            _akos = vm.ResourceManager.GetCostumeData(id);
            Debug.Assert(_akos != null);
        }

        public int IncreaseAnims(Actor a)
        {
            throw new NotImplementedException();
        }

        public void CostumeDecodeData(Actor a, int frame, uint usemask)
        {
            if (a.Costume == 0)
                return;

            LoadCostume(a.Costume);

            int anim;
            if (vm.Game.Version >= 7 && HasManyDirections())
                anim = ScummMath.ToSimpleDir(true, a.Facing) + frame * 8;
            else
                anim = ScummHelper.NewDirToOldDir(a.Facing) + frame * 4;

            var akhd = ResourceFile7.ReadData<AkosHeader>(_akos, "AKHD");

            if (anim >= akhd.num_anims)
                return;

            var akch = ResourceFile7.ReadData(_akos, "AKCH");
            Debug.Assert(akch != null);

            var offs = BitConverter.ToUInt16(akch, anim * 2);
            if (offs == 0)
                return;

            var akst = ResourceFile7.ReadData(_akos, "AKST");
            var aksf = ResourceFile7.ReadData(_akos, "AKSF");

            var i = 0;
            var mask = BitConverter.ToUInt16(akch, offs);
            offs += 2;

            byte code;
            ushort start, len;
            do
            {
                if ((mask & 0x8000) != 0)
                {
                    var akstPtr = 0;
                    var aksfPtr = 0;

                    code = akch[offs++];
                    if ((usemask & 0x8000) != 0)
                    {
                        switch (code)
                        {
                            case 1:
                                a.Cost.Active[i] = 0;
                                a.Cost.Frame[i] = (ushort)frame;
                                a.Cost.End[i] = 0;
                                a.Cost.Start[i] = 0;
                                a.Cost.Curpos[i] = 0;
//                                a.Cost.HeCondMaskTable[i] = 0;

                                if (akst != null)
                                {
                                    int size = akst.Length / 8;
                                    if (size > 0)
                                    {
                                        bool found = false;
                                        while ((size--) != 0)
                                        {
                                            if (BitConverter.ToUInt32(akst, akstPtr) == 0)
                                            {
//                                                a.Cost.HeCondMaskTable[i] = BitConverter.ToUInt32(akst, akstPtr + 4);
                                                found = true;
                                                break;
                                            }
                                            akstPtr += 8;
                                        }
                                        if (!found)
                                        {
                                            Console.Error.WriteLine("Sequence not found in actor {0} costume {1}", a.Number, a.Costume);
                                        }
                                    }
                                }
                                break;
                            case 4:
                                a.Cost.Stopped |= (ushort)(1 << i);
                                break;
                            case 5:
                                a.Cost.Stopped &= (ushort)(~(1 << i));
                                break;
                            default:
                                start = BitConverter.ToUInt16(akch, offs);
                                offs += 2;
                                len = BitConverter.ToUInt16(akch, offs);
                                offs += 2;

//                                a.Cost.heJumpOffsetTable[i] = 0;
//                                a.Cost.heJumpCountTable[i] = 0;
                                if (aksf != null)
                                {
                                    int size = aksf.Length / 6;
                                    if (size > 0)
                                    {
                                        bool found = false;
                                        while ((size--) != 0)
                                        {
                                            if (BitConverter.ToUInt16(aksf, aksfPtr) == start)
                                            {
//                                                a.Cost.HeJumpOffsetTable[i] = BitConverter.ToUInt16(aksf, aksfPtr + 2);
//                                                a.Cost.HeJumpCountTable[i] = BitConverter.ToUInt16(aksf, aksfPtr + 4);
                                                found = true;
                                                break;
                                            }
                                            aksfPtr += 6;
                                        }
                                        if (!found)
                                        {
                                            Console.Error.WriteLine("Sequence not found in actor {0} costume {1}", a.Number, a.Costume);
                                        }
                                    }
                                }

                                a.Cost.Active[i] = code;
                                a.Cost.Frame[i] = (ushort)frame;
                                a.Cost.End[i] = (ushort)(start + len);
                                a.Cost.Start[i] = start;
                                a.Cost.Curpos[i] = start;
//                                a.Cost.HeCondMaskTable[i] = 0;
                                if (akst != null)
                                {
                                    int size = akst.Length / 8;
                                    if (size > 0)
                                    {
                                        bool found = false;
                                        while ((size--) != 0)
                                        {
                                            if (BitConverter.ToUInt32(akst, akstPtr) == start)
                                            {
//                                                a.Cost.heCondMaskTable[i] = READ_LE_UINT32(akst + 4);
                                                found = true;
                                                break;
                                            }
                                            akstPtr += 8;
                                        }
                                        if (!found)
                                        {
                                            Console.Error.WriteLine("Sequence not found in actor {0} costume {1}", a.Number, a.Costume);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (code != 1 && code != 4 && code != 5)
                            offs += 2 * 2;
                    }
                }
                i++;
                mask <<= 1;
                usemask <<= 1;
            } while (mask!=0);
        }

        public bool HasManyDirections(int id)
        {
            LoadCostume(id);
            return HasManyDirections();
        }

        protected bool HasManyDirections()
        {
            var akhd = ResourceFile7.ReadData<AkosHeader>(_akos, "AKHD");
            return (akhd.flags & 2) != 0;
        }





        protected byte[] _akos;
        private ScummEngine vm;
    }
}

