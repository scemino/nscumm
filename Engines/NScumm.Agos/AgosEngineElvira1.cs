//
//  AGOSEngineElvira1.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Agos
{
    internal class AgosEngineElvira1 : AgosEngine
    {
        protected static readonly GameSpecificSettings Simon1Settings =
            new GameSpecificSettings
            {
                base_filename = string.Empty, // base_filename
                restore_filename = string.Empty, // restore_filename
                tbl_filename = string.Empty, // tbl_filename
                effects_filename = "EFFECTS", // effects_filename
                speech_filename = "SIMON" // speech_filename
            };

        public AgosEngineElvira1(ISystem system, GameSettings settings, AgosGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected void oe1_isNotAt()
        {
            // 9: parent is not
            Item item = GetNextItemPtr();
            SetScriptCondition(item.parent != GetNextItemID());
        }

        protected void oe1_sibling()
        {
            // 10: sibling
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(item1.parent == item2.parent);
        }

        protected void oe1_notSibling()
        {
            // 11: not sibling
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(item1.parent != item2.parent);
        }

        protected void oe1_isIn()
        {
            // 22: is in
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(Contains(item1, item2) != 0);
        }

        protected void oe1_isNotIn()
        {
            // 23: is not in
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(Contains(item1, item2) == 0);
        }

        protected void oe1_isPlayer()
        {
            // 30: is player
            SetScriptCondition(IsPlayer(GetNextItemPtr()));
        }

        protected void oe1_canPut()
        {
            // 37: can put
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(CanPlace(item1, item2) == 0);
        }

        protected void oe1_copyof()
        {
            // 54: copy of
            Item item = GetNextItemPtr();
            uint tmp = GetVarOrByte();
            WriteNextVarContents((ushort) GetUserFlag(item, (int) tmp));
        }

        protected void oe1_copyfo()
        {
            // 55: copy fo
            uint tmp = GetNextVarContents();
            Item item = GetNextItemPtr();
            SetUserFlag(item, (int) GetVarOrByte(), (int) tmp);
        }

        protected void oe1_whatO()
        {
            // 57: what o
            int a = (int) GetVarOrWord();

            if (a == 1)
                _subjectItem = FindMaster(_scriptAdj1, _scriptNoun1);
            else
                _objectItem = FindMaster(_scriptAdj2, _scriptNoun2);
        }

        protected void oe1_weigh()
        {
            // 59: weight
            Item item = GetNextItemPtr();
            WriteNextVarContents((ushort) WeighUp(item));
        }

        protected void oe1_pName()
        {
            // 114: print item name
            Item i = GetNextItemPtr();
            ShowMessageFormat("{0}", GetStringPtrById(i.itemName));
        }

        protected void oe1_pcName()
        {
            // 115: print item case (and change first letter to upper case)
            Item i = GetNextItemPtr();
            ShowMessageFormat("{0}", GetStringPtrById(i.itemName, true));
        }

        protected void oe1_rescan()
        {
            // 164: restart subroutine
            SetScriptReturn(-10);
        }

        protected void oe1_stopAnimate()
        {
            // 227: stop animate
            StopAnimate((ushort) GetVarOrWord());
        }

        protected void oe1_loadGame()
        {
            // 202: load restart state
            ushort stringId = (ushort) GetNextStringID();
            LoadGame(GetStringPtrById(stringId), true);
        }

        protected void oe1_findMaster()
        {
            // 219: find master
            short ad, no;
            short d = (short) GetVarOrByte();

            ad = (d == 1) ? _scriptAdj1 : _scriptAdj2;
            no = (d == 1) ? _scriptNoun1 : _scriptNoun2;

            d = (short) GetVarOrByte();
            if (d == 1)
                _subjectItem = FindMaster(ad, no);
            else
                _objectItem = FindMaster(ad, no);
        }

        protected void oe1_nextMaster()
        {
            // 220: next master
            short ad, no;
            Item item = GetNextItemPtr();
            short d = (short) GetVarOrByte();

            ad = (d == 1) ? _scriptAdj1 : _scriptAdj2;
            no = (d == 1) ? _scriptNoun1 : _scriptNoun2;

            d = (short) GetVarOrByte();
            if (d == 1)
                _subjectItem = NextMaster(item, ad, no);
            else
                _objectItem = NextMaster(item, ad, no);
        }

        protected void oe1_animate()
        {
            // 226: animate
            ushort vgaSpriteId = (ushort) GetVarOrWord();
            ushort windowNum = (ushort) GetVarOrByte();
            short x = (short) GetVarOrWord();
            short y = (short) GetVarOrWord();
            ushort palette = (ushort) GetVarOrWord();

            _videoLockOut |= 0x40;
            Animate(windowNum, (ushort) (vgaSpriteId / 100), vgaSpriteId, x, y, palette);
            _videoLockOut = (ushort) (_videoLockOut & ~0x40);
        }

        protected void oe1_setTime()
        {
            // 259: set time
            _timeStore = GetTime();
        }

        protected void oe1_ifTime()
        {
            // 260: if time
            uint a = GetVarOrWord();
            uint t = GetTime() - a;
            SetScriptCondition(t >= _timeStore);
        }

        protected void oe2_pauseGame()
        {
            // 135: pause game
            uint pauseTime = GetTime();
            HaltAnimation();

            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                while (!HasToQuit)
                {
                    if (ProcessSpecialKeys() || _lastHitArea3 != null)
                        break;
                    Delay(1);
                }

                var ha = _lastHitArea;

                if (ha == null)
                {
                }
                else if (ha.id == 201)
                {
                    break;
                }
            }

            RestartAnimation();
            _gameStoppedClock = GetTime() - pauseTime + _gameStoppedClock;
        }

        private bool IsPlayer(Item item)
        {
            return FindChildOfType(item, ChildType.kPlayerType) != null;
        }

        private int Contains(Item a, Item b)
        {
            while (DerefItem(b.parent) != null)
            {
                if (DerefItem(b.parent) == a)
                    return 1;
                b = DerefItem(b.parent);
            }

            return 0;
        }

        protected override void ExecuteOpcode(int opcode)
        {
            throw new System.NotImplementedException();
        }
    }
}