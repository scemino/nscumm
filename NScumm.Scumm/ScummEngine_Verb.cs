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

using System.Linq;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        internal VerbSlot[] Verbs { get; private set; }

        readonly Sentence[] _sentence = InitSentences();
        int _verbMouseOver;
        protected UserStates _userState;

        protected int SentenceNum { get; set; }

        internal Sentence[] Sentence { get { return _sentence; } }

        protected void SetVerbObject(byte room, int obj, int verb)
        {
            ObjectData o = null;
            if (Game.Version < 5)
            {
                for (var i = _resManager.NumLocalObjects - 1; i > 0; i--)
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
                Verbs[verb].ImageWidth = o.Width;
                Verbs[verb].ImageHeight = o.Height;
                Verbs[verb].ImageData = o.Images.Count > 0 ? o.Images[0] : null;
            }
        }

        protected void VerbMouseOver(int verb)
        {
            // Don't do anything unless verbs are active
            if (Game.Version <= 2 && !(_userState.HasFlag(UserStates.IFaceVerbs)))
                return;

            if (Game.GameId == GameId.FullThrottle)
                return;

            if (_verbMouseOver != verb)
            {
                if (Verbs[_verbMouseOver].Type != VerbType.Image)
                {
                    DrawVerb(_verbMouseOver, 0);
                    _verbMouseOver = verb;
                }

                if (Verbs[verb].Type != VerbType.Image && Verbs[verb].HiColor != 0)
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

            if ((Game.Version != 0 || OBJECT_V0_TYPE(obj) == 0) && GetOwnerCore(obj) != OwnerRoom)
            {
                for (int i = 0; i < _resManager.NumInventory; i++)
                {
                    if (_inventory[i] == obj)
                        result = _invData[i];
                }
            }
            else
            {
                result = _objs.FirstOrDefault(o => o.Number == obj);
            }

            var FallbackEntry = Game.Version == 8 ? 0xFFFFFFFF : 0xFF;
            foreach (var key in result.ScriptOffsets.Keys)
            {
                if (key == entry || key == FallbackEntry)
                    return result.ScriptOffsets[key];
            }

            return 0;
        }

        protected virtual void DrawVerb(int verb, int mode)
        {
            if (verb == 0)
                return;

            var vs = Verbs[verb];
            if (vs.SaveId == 0 && vs.CurMode != 0 && vs.VerbId != 0)
            {
                if (vs.Type == VerbType.Image)
                {
                    DrawVerbBitmap(verb, vs.CurRect.Left, vs.CurRect.Top);
                    return;
                }

                RestoreVerbBG(verb);

                _string[4].Charset = vs.CharsetNr;
                _string[4].Position = new Point(vs.CurRect.Left, vs.CurRect.Top);
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
                var msg = Verbs[verb].Text;
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
            else if (Game.GameId != GameId.FullThrottle)
            {
                RestoreVerbBG(verb);
            }
        }

        protected void KillVerb(int slot)
        {
            if (slot == 0)
                return;

            var vs = Verbs[slot];
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

        protected int FindVerbAtPos(Point p)
        {
            for (int i = Verbs.Length - 1; i >= 0; i--)
            {
                var vs = Verbs[i];
                if (vs.CurMode != 1 || vs.VerbId == 0 || vs.SaveId != 0 || p.Y < vs.CurRect.Top || p.Y >= vs.CurRect.Bottom)
                    continue;
                if (vs.Center)
                {
                    if (p.X < -(vs.CurRect.Right - 2 * vs.CurRect.Left) || p.X >= vs.CurRect.Right)
                        continue;
                }
                else
                {
                    if (p.X < vs.CurRect.Left || p.X >= vs.CurRect.Right)
                        continue;
                }

                return i;
            }

            return 0;
        }

        protected int GetVerbSlot(int id, int mode)
        {
            for (int i = 1; i < Verbs.Length; i++)
            {
                if (Verbs[i].VerbId == id && Verbs[i].SaveId == mode)
                {
                    return i;
                }
            }
            return 0;
        }

        protected void DoSentence(byte verb, ushort objectA, ushort objectB)
        {
            if (Game.Version >= 7)
            {

                if (objectA == objectB)
                    return;

                if (SentenceNum > 0)
                {
                    var st = _sentence[SentenceNum - 1];

                    // Check if this doSentence request is identical to the previous one;
                    // if yes, ignore this invocation.
                    if (SentenceNum != 0 && st.Verb == verb && st.ObjectA == objectA && st.ObjectB == objectB)
                        return;
                }

            }

            _sentence[SentenceNum++] = new Sentence(verb, objectA, objectB);
        }

        protected void RedrawVerbs()
        {
            if (Game.Version <= 2 && !(_userState.HasFlag(UserStates.IFaceVerbs))) // Don't draw verbs unless active
                return;

            int verb = 0;
            if (_cursor.State > 0)
                verb = FindVerbAtPos(_mousePos);

            // Iterate over all verbs.
            // Note: This is the correct order (at least for MI EGA, MI2, Full Throttle).
            // Do not change it! If you discover, based on disasm, that some game uses
            // another (e.g. the reverse) order here, you have to use an if/else construct
            // to add it as a special case!
            for (var i = 0; i < Verbs.Length; i++)
            {
                if (i == verb && Verbs[verb].HiColor != 0)
                    DrawVerb(i, 1);
                else
                    DrawVerb(i, 0);
            }
            _verbMouseOver = verb;
        }

        void InitializeVerbs()
        {
            Verbs = new VerbSlot[_resManager.NumVerbs];
            for (int i = 0; i < Verbs.Length; i++)
            {
                Verbs[i] = new VerbSlot();
                Verbs[i].CurRect.Right = ScreenWidth - 1;
                Verbs[i].OldRect.Left = -1;
                Verbs[i].Color = 2;
                Verbs[i].CharsetNr = 1;
            }
        }

        void RestoreVerbBG(int verb)
        {
            var vs = Verbs[verb];

            var col =
                ((Game.Platform == Platform.FMTowns) &&
                    (Game.GameId == GameId.Monkey2 || Game.GameId == GameId.Indy4) &&
                    (vs.BkColor == TownsOverrideShadowColor)) ? 0 : vs.BkColor;
            if (vs.OldRect.Left != -1)
            {
                RestoreBackground(vs.OldRect, (byte)col);
                vs.OldRect.Left = -1;
            }
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
    }
}

