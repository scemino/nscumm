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
using NScumm.Core;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm.IO
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
                                        //bool found = false;
                                        while ((size--) != 0)
                                        {
                                            if (BitConverter.ToUInt32(akst, akstPtr) == 0)
                                            {
//                                                a.Cost.HeCondMaskTable[i] = BitConverter.ToUInt32(akst, akstPtr + 4);
                                                //found = true;
                                                break;
                                            }
                                            akstPtr += 8;
                                        }
//                                        if (!found)
//                                        {
//                                            Console.Error.WriteLine("Sequence not found in actor {0} costume {1}", a.Number, a.Costume);
//                                        }
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
                                        //bool found = false;
                                        while ((size--) != 0)
                                        {
                                            if (BitConverter.ToUInt16(aksf, aksfPtr) == start)
                                            {
//                                                a.Cost.HeJumpOffsetTable[i] = BitConverter.ToUInt16(aksf, aksfPtr + 2);
//                                                a.Cost.HeJumpCountTable[i] = BitConverter.ToUInt16(aksf, aksfPtr + 4);
                                                //found = true;
                                                break;
                                            }
                                            aksfPtr += 6;
                                        }
//                                        if (!found)
//                                        {
//                                            Console.Error.WriteLine("Sequence not found in actor {0} costume {1}", a.Number, a.Costume);
//                                        }
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
                                        //bool found = false;
                                        while ((size--) != 0)
                                        {
                                            if (BitConverter.ToUInt32(akst, akstPtr) == start)
                                            {
//                                                a.Cost.heCondMaskTable[i] = READ_LE_UINT32(akst + 4);
                                                //found = true;
                                                break;
                                            }
                                            akstPtr += 8;
                                        }
//                                        if (!found)
//                                        {
//                                            Console.Error.WriteLine("Sequence not found in actor {0} costume {1}", a.Number, a.Costume);
//                                        }
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
            } while (mask != 0);
        }

        public bool HasManyDirections(int id)
        {
            LoadCostume(id);
            return HasManyDirections();
        }

        public int IncreaseAnims(Actor a)
        {
            var aksq = ResourceFile7.ReadData(_akos, "AKSQ");
            var akfo = ResourceFile7.ReadData(_akos, "AKFO");

            var size = akfo == null ? 0 : akfo.Length / 2;

            var result = false;
            for (var i = 0; i < 16; i++)
            {
                if (a.Cost.Active[i] != 0)
                    result |= IncreaseAnim(a, i, aksq, akfo, size, vm);
            }
            return result ? 1 : 0;
        }

        protected bool HasManyDirections()
        {
            var akhd = ResourceFile7.ReadData<AkosHeader>(_akos, "AKHD");
            return (akhd.flags & 2) != 0;
        }

        public static bool IncreaseAnim(Actor a, int chan, byte[] aksq, byte[] akfo, int numakfo, ScummEngine vm)
        {
            byte active;
            int old_curpos, end;
            int curpos;
            bool flag_value, needRedraw;
            int tmp, tmp2;

            active = a.Cost.Active[chan];
            end = a.Cost.End[chan];
            old_curpos = curpos = a.Cost.Curpos[chan];
            flag_value = false;
            needRedraw = false;

            do
            {

                var code = (AkosOpcode)aksq[curpos];
                if (((ushort)code & 0x80) != 0)
                    code = (AkosOpcode)ScummHelper.SwapBytes(BitConverter.ToUInt16(aksq, curpos));

                switch (active)
                {
                    case 6:
                    case 8:
                        switch (code)
                        {
                            case AkosOpcode.JumpIfSet:
                            case AkosOpcode.AddVar:
                            case AkosOpcode.SetVar:
                            case AkosOpcode.SkipGE:
                            case AkosOpcode.SkipG:
                            case AkosOpcode.SkipLE:
                            case AkosOpcode.SkipL:

                            case AkosOpcode.SkipNE:
                            case AkosOpcode.SkipE:
                            case AkosOpcode.C016:
                            case AkosOpcode.C017:
                            case AkosOpcode.C018:
                            case AkosOpcode.C019:
                                curpos += 5;
                                break;
                            case AkosOpcode.JumpTable:
                            case AkosOpcode.SetActorClip:
                            case AkosOpcode.Ignore3:
                            case AkosOpcode.Ignore2:
                            case AkosOpcode.Ignore:
                            case AkosOpcode.StartAnim:
                            case AkosOpcode.StartVarAnim:
                            case AkosOpcode.CmdQue3:
                            case AkosOpcode.C042:
                            case AkosOpcode.C044:
                            case AkosOpcode.C0A3:
                                curpos += 3;
                                break;
                            case AkosOpcode.SoundStuff:
//                                if (Game.Heversion >= 61)
//                                    curpos += 6;
//                                else
                                curpos += 8;
                                break;
                            case AkosOpcode.Cmd3:
                            case AkosOpcode.SetVarInActor:
                            case AkosOpcode.SetDrawOffs:
                                curpos += 6;
                                break;
                            case AkosOpcode.ClearFlag:
                            case AkosOpcode.HideActor:
                            case AkosOpcode.IncVar:
                            case AkosOpcode.CmdQue3Quick:
                            case AkosOpcode.Return:
                            case AkosOpcode.EndSeq:
                                curpos += 2;
                                break;
                            case AkosOpcode.JumpGE:
                            case AkosOpcode.JumpG:
                            case AkosOpcode.JumpLE:
                            case AkosOpcode.JumpL:
                            case AkosOpcode.JumpNE:
                            case AkosOpcode.JumpE:
                            case AkosOpcode.Random:
                                curpos += 7;
                                break;
                            case AkosOpcode.Flip:
                            case AkosOpcode.Jump:
                            case AkosOpcode.StartAnimInActor:
                            case AkosOpcode.C0A0:
                            case AkosOpcode.C0A1:
                            case AkosOpcode.C0A2:
                                curpos += 4;
                                break;
                            case AkosOpcode.ComplexChan2:
                                curpos += 4;
                                curpos += 3;
                                tmp = aksq[curpos - 1];
                                while (--tmp >= 0)
                                {
                                    curpos += 4;
                                    curpos += ((aksq[curpos] & 0x80) != 0) ? 2 : 1;
                                }
                                break;
                        // Fall through
                            case AkosOpcode.ComplexChan:
                                curpos += 3;
                                tmp = aksq[curpos - 1];
                                while (--tmp >= 0)
                                {
                                    curpos += 4;
                                    curpos += ((aksq[curpos] & 0x80) != 0) ? 2 : 1;
                                }
                                break;
                            case AkosOpcode.C021:
                            case AkosOpcode.C022:
                            case AkosOpcode.C045:
                            case AkosOpcode.C046:
                            case AkosOpcode.C047:
                            case AkosOpcode.C048:
                                needRedraw = true;
                                curpos += aksq[curpos + 2];
                                break;
                            case AkosOpcode.C08E:
                                akos_queCommand(7, a, GW(aksq, curpos, 2), 0, vm);
                                curpos += 4;
                                break;
                            default:
                                curpos += (((short)code & 0x8000) != 0) ? 2 : 1;
                                break;
                        }
                        break;
                    case 2:
                        curpos += (((short)code & 0x8000) != 0) ? 2 : 1;
                        if (curpos > end)
                            curpos = a.Cost.Start[chan];
                        break;
                    case 3:
                        if (curpos != end)
                            curpos += (((short)code & 0x8000) != 0) ? 2 : 1;
                        break;
                }

                code = (AkosOpcode)aksq[curpos];
                if (((short)code & 0x80) != 0)
                    code = (AkosOpcode)ScummHelper.SwapBytes(BitConverter.ToUInt16(aksq, curpos));

                if (flag_value && code != AkosOpcode.ClearFlag)
                    continue;

                switch (code)
                {
                    case AkosOpcode.StartAnimInActor:
                        akos_queCommand(4, vm.Actors[a.GetAnimVar(GB(aksq, curpos, 2))], a.GetAnimVar(GB(aksq, curpos, 3)), 0, vm);
                        continue;

                    case AkosOpcode.Random:
                        a.SetAnimVar(GB(aksq, curpos, 6), new Random().Next(GW(aksq, curpos, 2), GW(aksq, curpos, 4)));
                        continue;
                    case AkosOpcode.JumpGE:
                    case AkosOpcode.JumpG:
                    case AkosOpcode.JumpLE:
                    case AkosOpcode.JumpL:
                    case AkosOpcode.JumpNE:
                    case AkosOpcode.JumpE:
                        if (akos_compare(a.GetAnimVar(GB(aksq, curpos, 4)), GW(aksq, curpos, 5), (byte)(code - AkosOpcode.JumpStart)))
                        {
                            curpos = GUW(aksq, curpos, 2);
                            break;
                        }
                        continue;
                    case AkosOpcode.IncVar:
                        a.SetAnimVar(0, a.GetAnimVar(0) + 1);
                        continue;
                    case AkosOpcode.SetVar:
                        a.SetAnimVar(GB(aksq, curpos, 4), GW(aksq, curpos, 2));
                        continue;
                    case AkosOpcode.AddVar:
                        a.SetAnimVar(GB(aksq, curpos, 4), a.GetAnimVar(GB(aksq, curpos, 4)) + GW(aksq, curpos, 2));
                        continue;
                    case AkosOpcode.Flip:
                        a.Flip = GW(aksq, curpos, 2) != 0;
                        continue;
                    case AkosOpcode.CmdQue3:
//                        if (Game.Heversion >= 61)
                        //                            tmp = GB(aksq, curpos,2);
//                        else
                        tmp = GB(aksq, curpos, 2) - 1;
                        if ((uint)tmp < 24)
                            akos_queCommand(3, a, a.Sounds[tmp], 0, vm);
                        continue;
                    case AkosOpcode.CmdQue3Quick:
                        akos_queCommand(3, a, a.Sounds[0], 0, vm);
                        continue;
                    case AkosOpcode.StartAnim:
                        akos_queCommand(4, a, GB(aksq, curpos, 2), 0, vm);
                        continue;
                    case AkosOpcode.StartVarAnim:
                        akos_queCommand(4, a, a.GetAnimVar(GB(aksq, curpos, 2)), 0, vm);
                        continue;
                    case AkosOpcode.SetVarInActor:
                        vm.Actors[a.GetAnimVar(GB(aksq, curpos, 2))].SetAnimVar(GB(aksq, curpos, 3), GW(aksq, curpos, 4));
                        continue;
                    case AkosOpcode.HideActor:
                        akos_queCommand(1, a, 0, 0, vm);
                        continue;
                    case AkosOpcode.SetActorClip:
                        akos_queCommand(5, a, GB(aksq, curpos, 2), 0, vm);
                        continue;
                    case AkosOpcode.SoundStuff:
//                        if (_game.heversion >= 61)
//                            continue;
                        tmp = GB(aksq, curpos, 2) - 1;
                        if (tmp >= 8)
                            continue;
                        tmp2 = GB(aksq, curpos, 4);
                        if (tmp2 < 1 || tmp2 > 3)
                            throw new InvalidOperationException(string.Format("akos_increaseAnim:8 invalid code {0}", tmp2));
                        akos_queCommand((byte)(tmp2 + 6), a, a.Sounds[tmp], GB(aksq, curpos, 6), vm);
                        continue;
                    case AkosOpcode.SetDrawOffs:
                        akos_queCommand(6, a, GW(aksq, curpos, 2), GW(aksq, curpos, 4), vm);
                        continue;
                    case AkosOpcode.JumpTable:
                        if (akfo == null)
                            throw new InvalidOperationException("akos_increaseAnim: no AKFO table");
                        tmp = a.GetAnimVar(GB(aksq, curpos, 2)) - 1;
//                        if (_game.heversion >= 80)
//                        {
//                            if (tmp < 0 || tmp > a.Cost.heJumpCountTable[chan] - 1)
//                                error("akos_increaseAnim: invalid jump value %d", tmp);
//                            curpos = READ_LE_UINT16(akfo + a.Cost.heJumpOffsetTable[chan] + tmp * 2);
//                        }
//                        else
                        {
                            if (tmp < 0 || tmp > numakfo - 1)
                                throw new InvalidOperationException(string.Format("akos_increaseAnim: invalid jump value {0}", tmp));
                            curpos = BitConverter.ToUInt16(akfo, tmp);
                        }
                        break;
                    case AkosOpcode.JumpIfSet:
                        if (a.GetAnimVar(GB(aksq, curpos, 4)) == 0)
                            continue;
                        a.SetAnimVar(GB(aksq, curpos, 4), 0);
                        curpos = GUW(aksq, curpos, 2);
                        break;

                    case AkosOpcode.ClearFlag:
                        flag_value = false;
                        continue;

                    case AkosOpcode.Jump:
                        curpos = GUW(aksq, curpos, 2);
                        break;

                    case AkosOpcode.Return:
                    case AkosOpcode.EndSeq:
                    case AkosOpcode.ComplexChan:
                    case AkosOpcode.C08E:
                    case AkosOpcode.ComplexChan2:
                        break;

                    case AkosOpcode.C021:
                    case AkosOpcode.C022:
                        needRedraw = true;
                        break;

                    case AkosOpcode.Cmd3:
                    case AkosOpcode.Ignore:
                    case AkosOpcode.Ignore3:
                        continue;

                    case AkosOpcode.Ignore2:
//                        if (_game.heversion >= 71)
                        //                            akos_queCommand(3, a, a._sound[a.GetAnimVar(GB(aksq, curpos,2))], 0);
                        continue;

                    case AkosOpcode.SkipE:
                    case AkosOpcode.SkipNE:
                    case AkosOpcode.SkipL:
                    case AkosOpcode.SkipLE:
                    case AkosOpcode.SkipG:
                    case AkosOpcode.SkipGE:
                        if (!akos_compare(a.GetAnimVar(GB(aksq, curpos, 4)), GW(aksq, curpos, 2), (byte)(code - AkosOpcode.SkipStart)))
                            flag_value = true;
                        continue;
                    case AkosOpcode.C016:
                        if (vm.Sound.IsSoundRunning(a.Sounds[a.GetAnimVar(GB(aksq, curpos, 4))]))
                        {
                            curpos = GUW(aksq, curpos, 2);
                            break;
                        }
                        continue;
                    case AkosOpcode.C017:
                        if (!vm.Sound.IsSoundRunning(a.Sounds[a.GetAnimVar(GB(aksq, curpos, 4))]))
                        {
                            curpos = GUW(aksq, curpos, 2);
                            break;
                        }
                        continue;
                    case AkosOpcode.C018:
                        if (vm.Sound.IsSoundRunning(a.Sounds[GB(aksq, curpos, 4)]))
                        {
                            curpos = GUW(aksq, curpos, 2);
                            break;
                        }
                        continue;
                    case AkosOpcode.C019:
                        if (!vm.Sound.IsSoundRunning(a.Sounds[GB(aksq, curpos, 4)]))
                        {
                            curpos = GUW(aksq, curpos, 2);
                            break;
                        }
                        continue;
                    case AkosOpcode.C042:
                        akos_queCommand(9, a, a.Sounds[GB(aksq, curpos, 2)], 0, vm);
                        continue;
                    case AkosOpcode.C044:
                        akos_queCommand(9, a, a.Sounds[a.GetAnimVar(GB(aksq, curpos, 2))], 0, vm);
                        continue;
//                    case AkosOpcode.C045:
//                        ((ActorHE*)a).SetUserCondition(GB(aksq, curpos, 3), a.GetAnimVar(GB(aksq, curpos, 4)));
//                        continue;
//                    case AkosOpcode.C046:
//                        a.SetAnimVar(GB(aksq, curpos, 4), ((ActorHE*)a).isUserConditionSet(GB(aksq, curpos, 3)));
//                        continue;
//                    case AkosOpcode.C047:
//                        ((ActorHE*)a).setTalkCondition(GB(aksq, curpos, 3));
//                        continue;
//                    case AkosOpcode.C048:
//                        a.setAnimVar(GB(aksq, curpos, 4), ((ActorHE*)a).isTalkConditionSet(GB(aksq, curpos, 3)));
//                        continue;
                    case AkosOpcode.C0A0:
                        akos_queCommand(8, a, GB(aksq, curpos, 2), 0, vm);
                        continue;
//                    case AkosOpcode.C0A1:
//                        if (((ActorHE*)a)._heTalking != 0)
//                        {
//                            curpos = GUW(aksq, curpos, 2);
//                            break;
//                        }
//                        continue;
//                    case AkosOpcode.C0A2:
//                        if (((ActorHE*)a)._heTalking == 0)
//                        {
//                            curpos = GUW(aksq, curpos, 2);
//                            break;
//                        }
//                        continue;
                    case AkosOpcode.C0A3:
                        akos_queCommand(8, a, a.GetAnimVar(GB(aksq, curpos, 2)), 0, vm);
                        continue;
                    case AkosOpcode.C0A4:
                        if (vm.Variables[vm.VariableTalkActor.Value] != 0)
                        {
                            curpos = GUW(aksq, curpos, 2);
                            break;
                        }
                        continue;
                    case AkosOpcode.C0A5:
                        if (vm.Variables[vm.VariableTalkActor.Value] == 0)
                        {
                            curpos = GUW(aksq, curpos, 2);
                            break;
                        }
                        continue;
                    default:
                        if (((short)code & 0xC000) == 0xC000)
                            throw new InvalidOperationException(string.Format("Undefined uSweat token {0:X}", code));
                        break;
                }
                break;
            } while (true);

            int code2 = aksq[curpos];
            if ((code2 & 0x80) != 0)
                code2 = ScummHelper.SwapBytes(BitConverter.ToUInt16(aksq, curpos));

            if (((code2 & 0xC000) == 0xC000) && code2 != (int)AkosOpcode.ComplexChan &&
                code2 != (int)AkosOpcode.Return && code2 != (int)AkosOpcode.EndSeq &&
                code2 != (int)AkosOpcode.C08E && code2 != (int)AkosOpcode.ComplexChan2 &&
                code2 != (int)AkosOpcode.C021 && code2 != (int)AkosOpcode.C022)
                throw new InvalidOperationException(string.Format("Ending with undefined uSweat token {0:X}", code2));

            a.Cost.Curpos[chan] = (ushort)curpos;

            if (needRedraw)
                return true;
            else
                return curpos != old_curpos;
        }

        static short GW(byte[] aksq, int curpos, int o)
        {
            return BitConverter.ToInt16(aksq, curpos + o);
        }

        static ushort GUW(byte[] aksq, int curpos, int o)
        {
            return BitConverter.ToUInt16(aksq, curpos + o);
        }

        static byte GB(byte[] aksq, int curpos, int o)
        {
            return aksq[curpos + o];
        }

        static void akos_queCommand(byte cmd, Actor a, int param_1, int param_2, ScummEngine vm)
        {
            var v = (ScummEngine6)vm;
            v._akosQueuePos++;
            ScummHelper.AssertRange(0, v._akosQueuePos, 31, "akos_queCommand: _akosQueuePos");

            v._akosQueue[v._akosQueuePos].cmd = cmd;
            v._akosQueue[v._akosQueuePos].actor = a.Number;
            v._akosQueue[v._akosQueuePos].param1 = (short)param_1;
            v._akosQueue[v._akosQueuePos].param2 = (short)param_2;
        }

        static bool akos_compare(int a, int b, byte cmd)
        {
            switch (cmd)
            {
                case 0:
                    return a == b;
                case 1:
                    return a != b;
                case 2:
                    return a < b;
                case 3:
                    return a <= b;
                case 4:
                    return a > b;
                default:
                    return a >= b;
            }
        }

        protected byte[] _akos;
        private ScummEngine vm;
    }
}

