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


using System.Collections.Generic;

namespace NScumm.Sci.Engine
{
    internal struct MessageTuple
    {
        public byte noun;
        public byte verb;
        public byte cond;
        public byte seq;

        public MessageTuple(byte noun_ = 0, byte verb_ = 0, byte cond_ = 0, byte seq_ = 1)
        {
            noun = noun_;
            verb = verb_;
            cond = cond_;
            seq = seq_;
        }
    }

    internal class CursorStack : Stack<MessageTuple>
    {
        public void Init(int module, MessageTuple t)
        {
            Clear();
            Push(t);
            _module = module;
        }

        public int Module
        {
            get { return _module; }
        }

        private int _module;
    }

    internal class CursorStackStack : Stack<CursorStack>
    {
    }

    internal class MessageState
    {
        private SegManager _segMan;
        private CursorStack _cursorStack;
        private CursorStackStack _cursorStackStack;
        private MessageTuple _lastReturned;
        private int _lastReturnedModule;

        public MessageState(SegManager _segMan)
        {
            this._segMan = _segMan;
        }
    }
}
