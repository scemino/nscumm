//
//  ScummEngine6_Verb.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        int _curVerbSlot;
        int _curVerb;

        [OpCode(0x9e)]
        void VerbOps()
        {
            var subOp = ReadByte();
            if (subOp == 196)
            {
                _curVerbSlot = Pop();
                _curVerbSlot = GetVerbSlot(_curVerb, 0);
                ScummHelper.AssertRange(0, _curVerbSlot, _verbs.Length - 1, "new verb slot");
                return;
            }
            var vs = _verbs[_curVerbSlot];
            var slot = _curVerbSlot;
            switch (subOp)
            {
                case 124:               // SO_VERB_IMAGE
                    {
                        var a = Pop();
                        if (_curVerbSlot != 0)
                        {
                            SetVerbObject(_roomResource, a, slot);
                            vs.Type = VerbType.Image;
                        }
                    }
                    break;
                case 125:               // SO_VERB_NAME
                    _verbs[slot].Text = ReadCharacters();
                    vs.Type = VerbType.Text;
                    vs.ImgIndex = 0;
                    break;
                case 126:               // SO_VERB_COLOR
                    vs.Color = (byte)Pop();
                    break;
                case 127:               // SO_VERB_HICOLOR
                    vs.HiColor = (byte)Pop();
                    break;
                case 128:               // SO_VERB_AT
                    vs.CurRect.Top = Pop();
                    vs.CurRect.Left = Pop();
                    break;
                case 129:               // SO_VERB_ON
                    vs.CurMode = 1;
                    break;
                case 130:               // SO_VERB_OFF
                    vs.CurMode = 0;
                    break;
                case 131:               // SO_VERB_DELETE
                    KillVerb(slot);
                    break;
                case 132:               // SO_VERB_NEW
                    slot = GetVerbSlot(_curVerb, 0);
                    if (slot == 0)
                    {
                        for (slot = 1; slot < _verbs.Length; slot++)
                        {
                            if (_verbs[slot].VerbId == 0)
                                break;
                        }
                        _curVerbSlot = slot;
                    }
                    vs = _verbs[slot];
                    vs.VerbId = (ushort)_curVerb;
                    vs.Color = 2;
                    vs.HiColor = 0;
                    vs.DimColor = 8;
                    vs.Type = VerbType.Text;
                    vs.CharsetNr = _string[0].Default.Charset;
                    vs.CurMode = 0;
                    vs.SaveId = 0;
                    vs.Key = 0;
                    vs.Center = false;
                    vs.ImgIndex = 0;
                    break;
                case 133:               // SO_VERB_DIMCOLOR
                    vs.DimColor = (byte)Pop();
                    break;
                case 134:               // SO_VERB_DIM
                    vs.CurMode = 2;
                    break;
                case 135:               // SO_VERB_KEY
                    vs.Key = (byte)Pop();
                    break;
                case 136:               // SO_VERB_CENTER
                    vs.Center = true;
                    break;
                case 137:               // SO_VERB_NAME_STR
                    {
                        var a = Pop();
                        if (a == 0)
                        {
                            _verbs[slot].Text = new byte[0];
                        }
                        else
                        {
                            _verbs[slot].Text = _strings[a];
                        }
                        vs.Type = VerbType.Text;
                        vs.ImgIndex = 0;
                    }
                    break;
                case 139:               // SO_VERB_IMAGE_IN_ROOM
                    {
                        var b = (byte)Pop();
                        var a = (ushort)Pop();

                        if (slot != 0 && a != vs.ImgIndex)
                        {
                            SetVerbObject(b, a, slot);
                            vs.Type = VerbType.Image;
                            vs.ImgIndex = a;
                        }
                    }
                    break;
                case 140:               // SO_VERB_BAKCOLOR
                    vs.BkColor = (byte)Pop();
                    break;
                case 255:
                    DrawVerb(slot, 0);
                    VerbMouseOver(0);
                    break;
                default:
                    throw new NotSupportedException(string.Format("Verbops: default case {0}", subOp));
            }
        }
    }
}
