//
//  ScummEngine3_Verb.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine3
    {
        void SaveRestoreVerbs()
        {
            int a, b, c, slot, slot2;

            _opCode = ReadByte();

            a = GetVarOrDirectByte(OpCodeParameter.Param1);
            b = GetVarOrDirectByte(OpCodeParameter.Param2);
            c = GetVarOrDirectByte(OpCodeParameter.Param3);

            switch (_opCode)
            {
                case 1:     // SO_SAVE_VERBS
                    while (a <= b)
                    {
                        slot = GetVerbSlot(a, 0);
                        if (slot != 0 && Verbs[slot].SaveId == 0)
                        {
                            Verbs[slot].SaveId = (ushort)c;
                            DrawVerb(slot, 0);
                            VerbMouseOver(0);
                        }
                        a++;
                    }
                    break;

                case 2:     // SO_RESTORE_VERBS
                    while (a <= b)
                    {
                        slot = GetVerbSlot(a, c);
                        if (slot != 0)
                        {
                            slot2 = GetVerbSlot(a, 0);
                            if (slot2 != 0)
                                KillVerb(slot2);
                            slot = GetVerbSlot(a, c);
                            Verbs[slot].SaveId = 0;
                            DrawVerb(slot, 0);
                            VerbMouseOver(0);
                        }
                        a++;
                    }
                    break;

                case 3:     // SO_DELETE_VERBS
                    while (a <= b)
                    {
                        slot = GetVerbSlot(a, c);
                        if (slot != 0)
                            KillVerb(slot);
                        a++;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        void VerbOps()
        {
            var verb = GetVarOrDirectByte(OpCodeParameter.Param1);
            var slot = GetVerbSlot(verb, 0);
            ScummHelper.AssertRange(0, slot, Verbs.Length - 1, "new verb slot");
            var vs = Verbs[slot];
            vs.VerbId = (ushort)verb;

            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0x1F)
                {
                    case 1:     // SO_VERB_IMAGE
                        var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        if (slot != 0)
                        {
                            SetVerbObject(_roomResource, a, slot);
                            vs.Type = VerbType.Image;
                        }
                        break;

                    case 2:     // SO_VERB_NAME
                        vs.Text = ReadCharacters();
                        vs.Type = VerbType.Text;
                        vs.ImgIndex = 0;
                        break;

                    case 3:     // SO_VERB_COLOR
                        vs.Color = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 4:     // SO_VERB_HICOLOR
                        vs.HiColor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 5:     // SO_VERB_AT
                        var left = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var top = GetVarOrDirectWord(OpCodeParameter.Param2);
                        vs.CurRect.Left = left;
                        vs.CurRect.Top = top;
                        if (Game.GameId == Scumm.IO.GameId.Loom && Game.Version == 4)
                        {
                            // FIXME: hack loom notes into right spot
                            if ((verb >= 90) && (verb <= 97))
                            {	// Notes
                                switch (verb)
                                {
                                    case 90:
                                    case 91:
                                        vs.CurRect.Top -= 7;
                                        break;
                                    case 92:
                                        vs.CurRect.Top -= 6;
                                        break;
                                    case 93:
                                        vs.CurRect.Top -= 4;
                                        break;
                                    case 94:
                                        vs.CurRect.Top -= 3;
                                        break;
                                    case 95:
                                        vs.CurRect.Top -= 1;
                                        break;
                                    case 97:
                                        vs.CurRect.Top -= 5;
                                        break;
                                }
                            }
                        }
                        break;

                    case 6:
					// SO_VERB_ON
                        vs.CurMode = 1;
                        break;

                    case 7:
					// SO_VERB_OFF
                        vs.CurMode = 0;
                        break;

                    case 8:     // SO_VERB_DELETE
                        KillVerb(slot);
                        break;

                    case 9:
                        {
                            // SO_VERB_NEW
                            slot = GetVerbSlot(verb, 0);

                            if (Game.Platform == Platform.FMTowns && Game.Version == 3 && slot != 0)
                                continue;

                            if (slot == 0)
                            {
                                for (slot = 1; slot < Verbs.Length; slot++)
                                {
                                    if (Verbs[slot].VerbId == 0)
                                        break;
                                }
                            }
                            vs = Verbs[slot];
                            vs.VerbId = (ushort)verb;
                            vs.Color = 2;
                            vs.HiColor = (Game.Version == 3) ? (byte)14 : (byte)0;
                            vs.DimColor = 8;
                            vs.Type = VerbType.Text;
                            vs.CharsetNr = String[0].Default.Charset;
                            vs.CurMode = 0;
                            vs.SaveId = 0;
                            vs.Key = 0;
                            vs.Center = false;
                            vs.ImgIndex = 0;
                            break;
                        }
                    case 16:    // SO_VERB_DIMCOLOR
                        vs.DimColor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 17:    // SO_VERB_DIM
                        vs.CurMode = 2;
                        break;

                    case 18:    // SO_VERB_KEY
                        vs.Key = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 19:    // SO_VERB_CENTER
                        vs.Center = true;
                        break;

                    case 20:    // SO_VERB_NAME_STR
                        var index = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var ptr = _strings[index];
                        if (ptr != null)
                        {
                            vs.Text = ptr;
                        }
					//if (slot == 0)
					//    _res->nukeResource(rtVerb, slot);
                        vs.Type = VerbType.Text;
                        vs.ImgIndex = 0;
                        break;
                    case 22:    // assign object
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                            if (slot != 0 && vs.ImgIndex != a)
                            {
                                SetVerbObject((byte)b, a, slot);
                                vs.Type = VerbType.Image;
                                vs.ImgIndex = (ushort)a;
                            }
                        }
                        break;
                    case 23:                                        /* set back color */
                        vs.BkColor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            // Force redraw of the modified verb slot
            DrawVerb(slot, 0);
            VerbMouseOver(0);
        }

        void GetVerbEntrypoint()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = GetVarOrDirectWord(OpCodeParameter.Param2);

            SetResult(GetVerbEntrypointCore(a, b));
        }

        void WaitForSentence()
        {
            if (SentenceNum != 0)
            {
                if (Sentence[SentenceNum - 1].IsFrozen && !IsScriptInUse(Variables[VariableSentenceScript.Value]))
                    return;
            }
            else if (!IsScriptInUse(Variables[VariableSentenceScript.Value]))
                return;
            CurrentPos--;
            BreakHere();
        }
    }
}

