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

namespace NScumm.Sword1
{
    struct GlobalEvent
    {
        public int eventNumber;
        public int delay;
    }

    internal class EventManager
    {
        const int TOTAL_EVENT_SLOTS = 20;

        readonly GlobalEvent[] _eventPendingList = new GlobalEvent[TOTAL_EVENT_SLOTS];

        public void CheckForEvent(SwordObject compact)
        {
            for (var objCnt = 0; objCnt < SwordObject.O_TOTAL_EVENTS; objCnt++)
            {
                if (compact.event_list[objCnt].o_event != 0)
                    for (var globCnt = 0; globCnt < TOTAL_EVENT_SLOTS; globCnt++)
                    {
                        if (_eventPendingList[globCnt].delay != 0 &&
                                (_eventPendingList[globCnt].eventNumber == compact.event_list[objCnt].o_event))
                        {
                            compact.logic = Logic.LOGIC_script;      //force into script mode
                            _eventPendingList[globCnt].delay = 0; //started, so remove from queue
                            compact.tree.script_level++;
                            compact.tree.script_id[compact.tree.script_level] =
                                compact.event_list[objCnt].o_event_script;
                            compact.tree.script_pc[compact.tree.script_level] =
                                compact.event_list[objCnt].o_event_script;
                        }
                    }
            }
        }

        public void ServiceGlobalEventList()
        {
            for (var slot = 0; slot < TOTAL_EVENT_SLOTS; slot++)
                if (_eventPendingList[slot].delay != 0)
                    _eventPendingList[slot].delay--;
        }

        public void FnIssueEvent(SwordObject cpt, int id, int evt, int delay)
        {
            var evSlot = 0;
            while (_eventPendingList[evSlot].delay != 0)
                evSlot++;
            if (evSlot >= TOTAL_EVENT_SLOTS)
                throw new InvalidOperationException("EventManager ran out of event slots");
            _eventPendingList[evSlot].delay = delay;
            _eventPendingList[evSlot].eventNumber = evt;
        }

        public int FnCheckForEvent(SwordObject cpt, int id, int pause)
        {
            if (pause != 0)
            {
                cpt.pause = pause;
                cpt.logic = Logic.LOGIC_pause_for_event;
                return Logic.SCRIPT_STOP;
            }

            for (var objCnt = 0; objCnt < SwordObject.O_TOTAL_EVENTS; objCnt++)
            {
                if (cpt.event_list[objCnt].o_event != 0)
                    for (var globCnt = 0; globCnt < TOTAL_EVENT_SLOTS; globCnt++)
                    {
                        if (_eventPendingList[globCnt].delay!=0 && (_eventPendingList[globCnt].eventNumber == cpt.event_list[objCnt].o_event))
                        {
                            cpt.logic = Logic.LOGIC_script;      //force into script mode
                            _eventPendingList[globCnt].delay = 0; //started, so remove from queue
                            cpt.tree.script_level++;
                            cpt.tree.script_id[cpt.tree.script_level] =
                                cpt.event_list[objCnt].o_event_script;
                            cpt.tree.script_pc[cpt.tree.script_level] =
                                cpt.event_list[objCnt].o_event_script;
                            return Logic.SCRIPT_STOP;
                        }
                    }
            }
            return Logic.SCRIPT_CONT;
        }

        public bool EventValid(int evt)
        {
            for (var slot = 0; slot < TOTAL_EVENT_SLOTS; slot++)
                if ((_eventPendingList[slot].eventNumber == evt) && (_eventPendingList[slot].delay != 0))
                    return true;
            return false;
        }
    }
}