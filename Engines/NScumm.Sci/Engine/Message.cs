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


using NScumm.Core;
using System;
using System.Collections.Generic;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal class MessageTuple
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

    internal class MessageRecord
    {
        public MessageTuple tuple;
        public MessageTuple refTuple;
        public string @string;
        public byte talker;
    }

    internal abstract class MessageReader
    {
        protected MessageReader(byte[] data, int size, int headerSize, int recordSize)
        {
            _data = data;
            _size = size;
            _headerSize = headerSize;
            _recordSize = recordSize;
        }

        public bool Init()
        {
            if (_headerSize > _size)
                return false;

            // Read message count from last word in header
            _messageCount = _data.ReadSci11EndianUInt16(_headerSize - 2);

            if (_messageCount * _recordSize + _headerSize > _size)
                return false;

            return true;
        }

        public abstract bool FindRecord(MessageTuple tuple, out MessageRecord record);

        protected byte[] _data;
        protected int _size;
        protected int _headerSize;
        protected int _recordSize;
        protected int _messageCount;
    }

    internal class MessageReaderV2 : MessageReader
    {
        public MessageReaderV2(byte[] data, int size)
            : base(data, size, 6, 4)
        {
        }

        public override bool FindRecord(MessageTuple tuple, out MessageRecord record)
        {
            record = new MessageRecord();
            var recordPtr = new ByteAccess(_data, _headerSize);
            for (uint i = 0; i < _messageCount; i++)
            {
                if ((recordPtr[0] == tuple.noun) && (recordPtr[1] == tuple.verb))
                {
                    record.tuple = tuple;
                    record.refTuple = new MessageTuple();
                    record.talker = 0;
                    record.@string = ScummHelper.GetText(_data, recordPtr.ToUInt16(2));
                    return true;
                }
                recordPtr.Offset += _recordSize;
            }

            return false;
        }
    }

    internal class MessageReaderV3 : MessageReader
    {
        public MessageReaderV3(byte[] data, int size)
            : base(data, size, 8, 10)
        {
        }

        public override bool FindRecord(MessageTuple tuple, out MessageRecord record)
        {
            record = new MessageRecord();
            var recordPtr = new ByteAccess(_data, _headerSize);
            for (uint i = 0; i < _messageCount; i++)
            {
                if ((recordPtr[0] == tuple.noun) && (recordPtr[1] == tuple.verb)
                && (recordPtr[2] == tuple.cond) && (recordPtr[3] == tuple.seq))
                {
                    record.tuple = tuple;
                    record.refTuple = new MessageTuple();
                    record.talker = recordPtr[4];
                    record.@string = ScummHelper.GetText(_data, recordPtr.ToUInt16(5));
                    return true;
                }
                recordPtr.Offset += _recordSize;
            }

            return false;
        }
    }

    internal class MessageReaderV4 : MessageReader
    {
        public MessageReaderV4(byte[] data, int size)
            : base(data, size, 10, 11)
        {
        }

        public override bool FindRecord(MessageTuple tuple, out MessageRecord record)
        {
            record = new MessageRecord();
            var recordPtr = new ByteAccess(_data, _headerSize);
            for (uint i = 0; i < _messageCount; i++)
            {
                if ((recordPtr[0] == tuple.noun) && (recordPtr[1] == tuple.verb)
                && (recordPtr[2] == tuple.cond) && (recordPtr[3] == tuple.seq))
                {
                    record.tuple = tuple;
                    record.refTuple = new MessageTuple(recordPtr[7], recordPtr[8], recordPtr[9]);
                    record.talker = recordPtr[4];
                    record.@string = ScummHelper.GetText(_data, recordPtr.Data.ReadSci11EndianUInt16(recordPtr.Offset + 5));
                    return true;
                }
                recordPtr.Offset += _recordSize;
            }

            return false;
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

        public int GetMessage(int module, MessageTuple t, Register buf)
        {
            _cursorStack.Init(module, t);
            return NextMessage(buf);
        }

        private void OutputString(Register buf, string v)
        {
            throw new NotImplementedException();
        }

        private string ProcessString(string s)
        {
            throw new NotImplementedException();
        }

        private MessageRecord GetRecord(CursorStack stack, bool recurse)
        {
            MessageRecord record = new MessageRecord();
            var res = SciEngine.Instance.ResMan.FindResource(new ResourceId(ResourceType.Message, (ushort)stack.Module), false);

            if (res == null)
            {
                Warning($"Failed to open message resource {stack.Module}");
                return null;
            }

            MessageReader reader;
            int version = (int)(res.data.ReadSci11EndianUInt32(0) / 1000);

            switch (version)
            {
                case 2:
                    reader = new MessageReaderV2(res.data, res.size);
                    break;
                case 3:
                    reader = new MessageReaderV3(res.data, res.size);
                    break;
                case 4:
#if ENABLE_SCI32
                case 5: // v5 seems to be compatible with v4
                        // SCI32 Mac is different than SCI32 DOS/Win here
                    if (g_sci.getPlatform() == Common::kPlatformMacintosh && getSciVersion() >= SCI_VERSION_2_1)
                        reader = new MessageReaderV4_MacSCI32(res.data, res.size);
                    else
#endif
                    reader = new MessageReaderV4(res.data, res.size);
                    break;
                default:
                    throw new InvalidOperationException($"Message: unsupported resource version {version}");
            }

            if (!reader.Init())
            {
                Warning("Message: failed to read resource header");
                return null;
            }

            while (true)
            {
                MessageTuple t = stack.Peek();

                // Fix known incorrect message tuples
                if (SciEngine.Instance.GameId == SciGameId.QFG1VGA && stack.Module == 322 &&
                    t.noun == 14 && t.verb == 1 && t.cond == 19 && t.seq == 1)
                {
                    // Talking to Kaspar the shopkeeper - bug #3604944
                    t.verb = 2;
                }

                if (SciEngine.Instance.GameId == SciGameId.PQ1 && stack.Module == 38 &&
                    t.noun == 10 && t.verb == 4 && t.cond == 8 && t.seq == 1)
                {
                    // Using the hand icon on Keith in the Blue Room - bug #3605654
                    t.cond = 9;
                }

                if (SciEngine.Instance.GameId == SciGameId.PQ1 && stack.Module == 38 &&
                    t.noun == 10 && t.verb == 1 && t.cond == 0 && t.seq == 1)
                {
                    // Using the eye icon on Keith in the Blue Room - bug #3605654
                    t.cond = 13;
                }

                // Fill in known missing message tuples
                if (SciEngine.Instance.GameId == SciGameId.SQ4 && stack.Module == 16 &&
                    t.noun == 7 && t.verb == 0 && t.cond == 3 && t.seq == 1)
                {
                    // This fixes the error message shown when speech and subtitles are
                    // enabled simultaneously in SQ4 - the (very) long dialog when Roger
                    // is talking with the aliens is missing - bug #3538416.
                    record.tuple = t;
                    record.refTuple = new MessageTuple();
                    record.talker = 7;  // Roger
                                        // The missing text is just too big to fit in one speech bubble, and
                                        // if it's added here manually and drawn on screen, it's painted over
                                        // the entrance in the back where the Sequel Police enters, so it
                                        // looks very ugly. Perhaps this is why this particular text is missing,
                                        // as the text shown in this screen is very short (one-liners).
                                        // Just output an empty string here instead of showing an error.
                    record.@string = string.Empty;
                    return record;
                }

                if (!reader.FindRecord(t, out record))
                {
                    // Tuple not found
                    if (recurse && (stack.Count > 1))
                    {
                        stack.Pop();
                        continue;
                    }
                    return null;
                }

                if (recurse)
                {
                    MessageTuple @ref = record.refTuple;

                    if (@ref.noun != 0 || @ref.verb != 0 || @ref.cond != 0)
                    {
                        t.seq++;
                        stack.Push(@ref);
                        continue;
                    }
                }
                return record;
            }
        }

        public ushort NextMessage(Register buf)
        {
            MessageRecord record;

            if (!buf.IsNull)
            {
                if ((record = GetRecord(_cursorStack, true)) != null)
                {
                    OutputString(buf, ProcessString(record.@string));
                    _lastReturned = record.tuple;
                    _lastReturnedModule = _cursorStack.Module;
                    _cursorStack.Peek().seq++;
                    return record.talker;
                }
                else {
                    MessageTuple t = _cursorStack.Peek();
                    OutputString(buf, $"Msg {_cursorStack.Module}: {t.noun} {t.verb} {t.cond} {t.seq} not found");
                    return 0;
                }
            }
            else {
                CursorStack stack = _cursorStack;

                if ((record = GetRecord(stack, true)) != null)
                    return record.talker;
                else
                    return 0;
            }
        }

        public ushort MessageSize(ushort module, MessageTuple t)
        {
            CursorStack stack = new CursorStack();
            MessageRecord record;

            stack.Init(module, t);
            if ((record = GetRecord(stack, true)) != null)
                return (ushort)(record.@string.Length + 1);
            else
                return 0;
        }

        public bool MessageRef(ushort module, MessageTuple t, out MessageTuple @ref)
        {
            CursorStack stack = new CursorStack();
            MessageRecord record;

            stack.Init(module, t);
            if ((record = GetRecord(stack, false)) != null)
            {
                @ref = record.refTuple;
                return true;
            }
            @ref = null;
            return false;
        }

        public void LastQuery(out int lastModule, out MessageTuple msg)
        {
            lastModule = _lastReturnedModule;
            msg = _lastReturned;
        }

        public void PushCursorStack()
        {
            _cursorStackStack.Push(_cursorStack);
        }

        public void PopCursorStack()
        {
            if (_cursorStackStack.Count!=0)
                _cursorStack = _cursorStackStack.Pop();
            else
                throw new InvalidOperationException("Message: attempt to pop from empty stack");
        }
    }
}
