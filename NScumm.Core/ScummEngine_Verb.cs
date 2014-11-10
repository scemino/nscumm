//
//  ScummEngine_Verb.cs
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
using System.Linq;
using NScumm.Core.Graphics;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        protected VerbSlot[] _verbs;
        readonly Sentence[] _sentence = InitSentences();
        int _sentenceNum;

        protected int SentenceNum { get { return _sentenceNum; } set { _sentenceNum = value; } }

        protected Sentence[] Sentence{ get { return _sentence; } }

        void InitializeVerbs()
        {
            _verbs = new VerbSlot[100];
            for (int i = 0; i < 100; i++)
            {
                _verbs[i] = new VerbSlot();
                _verbs[i].CurRect.Right = ScreenWidth - 1;
                _verbs[i].OldRect.Left = -1;
                _verbs[i].Color = 2;
                _verbs[i].CharsetNr = 1;
            }
        }

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
                        if (slot != 0 && _verbs[slot].SaveId == 0)
                        {
                            _verbs[slot].SaveId = (ushort)c;
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
                            _verbs[slot].SaveId = 0;
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
            ScummHelper.AssertRange(0, slot, _verbs.Length - 1, "new verb slot");
            var vs = _verbs[slot];
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
                        if (_game.Id == "loom" && _game.Version == 4)
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

                            if (slot == 0)
                            {
                                for (slot = 1; slot < _verbs.Length; slot++)
                                {
                                    if (_verbs[slot].VerbId == 0)
                                        break;
                                }
                            }
                            vs = _verbs[slot];
                            vs.VerbId = (ushort)verb;
                            vs.Color = 2;
                            vs.HiColor = (_game.Version == 3) ? (byte)14 : (byte)0;
                            vs.DimColor = 8;
                            vs.Type = VerbType.Text;
                            vs.CharsetNr = _string[0].Default.Charset;
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

        int _verbMouseOver;

        protected void SetVerbObject(byte room, int obj, int verb)
        {
            ObjectData o = null;
            if (Game.Version < 5)
            {
                for (var i = NumLocalObjects - 1; i > 0; i--)
                {
                    if (_objs[i].Number == obj)
                    {
                        o = _objs[i];
                    }
                }
            }
            else
            {
                var roomD = room == _roomResource ? roomData : _resManager.GetRoom(room);
                o = roomD.Objects.FirstOrDefault(ro => ro.Number == obj);
            }

            if (o != null)
            {
                _verbs[verb].ImageWidth = o.Width;
                _verbs[verb].ImageHeight = o.Height;
                _verbs[verb].ImageData = o.Images.Count > 0 ? o.Images[0] : null;
            }
        }

        protected void VerbMouseOver(int verb)
        {
            if (_verbMouseOver != verb)
            {
                if (_verbs[_verbMouseOver].Type != VerbType.Image)
                {
                    DrawVerb(_verbMouseOver, 0);
                    _verbMouseOver = verb;
                }

                if (_verbs[verb].Type != VerbType.Image && _verbs[verb].HiColor != 0)
                {
                    DrawVerb(verb, 1);
                    _verbMouseOver = verb;
                }
            }
        }

        protected int GetVerbEntrypointCore(int obj, int entry)
        {
            if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                return 0;

            ObjectData result = null;

            if (_resManager.ObjectOwnerTable[obj] != OwnerRoom)
            {
                for (int i = 0; i < NumInventory; i++)
                {
                    if (_inventory[i] == obj)
                        result = _invData[i];
                }
            }
            else
            {
                result = (from o in _objs
                                      where o.Number == obj
                                      select o).FirstOrDefault();
            }

            foreach (var key in result.ScriptOffsets.Keys)
            {
                if (key == entry || key == 0xFF)
                    return result.ScriptOffsets[key];
            }

            return 0;
        }

        VerbSlot GetVerb(int num)
        {
            var verbSlot = (from verb in _verbs
                                     where num == verb.VerbId && verb.Type == 0 && verb.SaveId == 0
                                     select verb).FirstOrDefault();
            return verbSlot;
        }

        protected void DrawVerb(int verb, int mode)
        {
            if (verb == 0)
                return;

            var vs = _verbs[verb];
            if (vs.SaveId == 0 && vs.CurMode != 0 && vs.VerbId != 0)
            {
                if (vs.Type == VerbType.Image)
                {
                    DrawVerbBitmap(verb, vs.CurRect.Left, vs.CurRect.Top);
                    return;
                }

                RestoreVerbBG(verb);

                _string[4].Charset = vs.CharsetNr;
                _string[4].Position = new Point((short)vs.CurRect.Left, (short)vs.CurRect.Top);
                _string[4].Right = (short)(ScreenWidth - 1);
                _string[4].Center = vs.Center;

                if (vs.CurMode == 2)
                    _string[4].Color = vs.DimColor;
                else if (mode != 0 && vs.HiColor != 0)
                    _string[4].Color = vs.HiColor;
                else
                    _string[4].Color = vs.Color;

                // FIXME For the future: Indy3 and under inv scrolling
                /*
                   if (verb >= 31 && verb <= 36)
                   verb += _inventoryOffset;
                 */
                var msg = _verbs[verb].Text;
                if (msg == null || msg.Length == 0)
                    return;

                var tmp = _charset.Center;
                DrawString(4, msg);
                _charset.Center = tmp;

                vs.CurRect.Right = _charset.Str.Right;
                vs.CurRect.Bottom = _charset.Str.Bottom;
                vs.OldRect = _charset.Str;
                _charset.Str.Left = _charset.Str.Right;
            }
            else
            {
                RestoreVerbBG(verb);
            }
        }

        void RestoreVerbBG(int verb)
        {
            VerbSlot vs = _verbs[verb];
            byte col = vs.BkColor;

            if (vs.OldRect.Left != -1)
            {
                RestoreBackground(vs.OldRect, col);
                vs.OldRect.Left = -1;
            }
        }

        protected void KillVerb(int slot)
        {
            if (slot == 0)
                return;

            VerbSlot vs = _verbs[slot];
            vs.VerbId = 0;
            vs.CurMode = 0;
            vs.Text = null;

            if (vs.SaveId == 0)
            {
                DrawVerb(slot, 0);
                VerbMouseOver(0);
            }
            vs.SaveId = 0;
        }

        protected int FindVerbAtPos(int x, int y)
        {
            for (int i = _verbs.Length - 1; i >= 0; i--)
            {
                var vs = _verbs[i];
                if (vs.CurMode != 1 || vs.VerbId == 0 || vs.SaveId != 0 || y < vs.CurRect.Top || y >= vs.CurRect.Bottom)
                    continue;
                if (vs.Center)
                {
                    if (x < -(vs.CurRect.Right - 2 * vs.CurRect.Left) || x >= vs.CurRect.Right)
                        continue;
                }
                else
                {
                    if (x < vs.CurRect.Left || x >= vs.CurRect.Right)
                        continue;
                }

                return i;
            }

            return 0;
        }

        protected int GetVerbSlot(int id, int mode)
        {
            for (int i = 1; i < _verbs.Length; i++)
            {
                if (_verbs[i].VerbId == id && _verbs[i].SaveId == mode)
                {
                    return i;
                }
            }
            return 0;
        }

        static Sentence[] InitSentences()
        {
            var sentences = new Sentence[6];
            for (int i = 0; i < sentences.Length; i++)
            {
                sentences[i] = new Sentence();
            }
            return sentences;
        }

        protected void DoSentence(byte verb, ushort objectA, ushort objectB)
        {
            _sentence[_sentenceNum++] = new Sentence(verb, objectA, objectB);
        }

        void WaitForSentence()
        {
            if (_sentenceNum != 0)
            {
                if (_sentence[_sentenceNum - 1].IsFrozen && !IsScriptInUse(Variables[VariableSentenceScript.Value]))
                    return;
            }
            else if (!IsScriptInUse(Variables[VariableSentenceScript.Value]))
                return;
            _currentPos--;
            BreakHereCore();
        }
    }
}

