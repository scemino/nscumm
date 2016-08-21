//
//  MidiEventQueue.cs
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

using System;

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    /// <summary>
    /// Used to safely store timestamped MIDI events in a local queue.
    /// </summary>
    class MidiEvent
    {
        public int shortMessageData;
        public BytePtr sysexData;
        public int sysexLength;
        public int timestamp;

        public void SetShortMessage(int useShortMessageData, int useTimestamp)
        {
            sysexData = BytePtr.Null;
            shortMessageData = useShortMessageData;
            timestamp = useTimestamp;
            sysexLength = 0;
        }

        public void SetSysex(BytePtr useSysexData, int useSysexLength, int useTimestamp)
        {
            sysexData = BytePtr.Null;
            shortMessageData = 0;
            timestamp = useTimestamp;
            sysexLength = useSysexLength;
            var dstSysexData = new byte[sysexLength];
            sysexData = dstSysexData;
            Array.Copy(useSysexData.Data, useSysexData.Offset, dstSysexData, 0, sysexLength);
        }
    }

    /// <summary>
    ///  Simple queue implementation using a ring buffer to store incoming MIDI event before the synth actually processes it.
    ///  It is intended to:
    ///  - get rid of prerenderer while retaining graceful partial abortion
    ///  - add fair emulation of the MIDI interface delays
    ///  - extend the synth interface with the default implementation of a typical rendering loop.
    ///  THREAD SAFETY:
    ///  It is safe to use either in a single thread environment or when there are only two threads - one performs only reading
    ///  and one performs only writing.More complicated usage requires external synchronisation.
    /// </summary>
    class MidiEventQueue
    {
        readonly MidiEvent[] ringBuffer;
        readonly int ringBufferMask;
        volatile int startPosition;
        volatile int endPosition;

        public MidiEventQueue(int useRingBufferSize = Mt32Emu.DEFAULT_MIDI_EVENT_QUEUE_SIZE) // Must be a power of 2
        {
            ringBuffer = new MidiEvent[useRingBufferSize];
            for (int i = 0; i < ringBuffer.Length; i++)
            {
                ringBuffer[i] = new MidiEvent();
            }
            ringBufferMask = useRingBufferSize - 1;
        }

        public void Reset()
        {
            startPosition = 0;
            endPosition = 0;
        }

        public bool PushShortMessage(int shortMessageData, int timestamp)
        {
            int newEndPosition = (endPosition + 1) & ringBufferMask;
            // Is ring buffer full?
            if (startPosition == newEndPosition) return false;
            ringBuffer[endPosition].SetShortMessage(shortMessageData, timestamp);
            endPosition = newEndPosition;
            return true;
        }

        public bool PushSysex(BytePtr sysexData, int sysexLength, int timestamp)
        {
            int newEndPosition = (endPosition + 1) & ringBufferMask;
            // Is ring buffer full?
            if (startPosition == newEndPosition) return false;
            ringBuffer[endPosition].SetSysex(sysexData, sysexLength, timestamp);
            endPosition = newEndPosition;
            return true;
        }

        public MidiEvent PeekMidiEvent()
        {
            return (startPosition == endPosition) ? null : ringBuffer[startPosition];
        }

        public void DropMidiEvent()
        {
            // Is ring buffer empty?
            if (startPosition != endPosition)
            {
                startPosition = (startPosition + 1) & ringBufferMask;
            }
        }
   }
}