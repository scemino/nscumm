//
//  MidiParser.cs
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
using System.IO;
using System;
using System.Linq;
using NScumm.Core.Audio.Midi;
using System.Diagnostics;

namespace NScumm.Core
{
    /// <summary>
    /// Configuration options for MidiParser
    /// The following options can be set to modify MidiParser's
    /// behavior.
    /// </summary>
    public enum MidiParserProperty
    {
        /// <summary>
        /// Events containing a pitch bend command should be treated as
        /// single-byte padding before the  real event. This allows the
        /// MidiParser to work with some malformed SMF files from Simon 1/2.
        /// </summary>
        MalformedPitchBends = 1,

        /// <summary>
        /// Sets auto-looping, which can be used by lightweight clients
        /// that don't provide their own flow control.
        /// </summary>
        AutoLoop = 2,

        /// <summary>
        /// Sets smart jumping, which intelligently expires notes that are
        /// active when a jump is made, rather than just cutting them off.
        /// </summary>
        SmartJump = 3,

        /// <summary>
        /// Center the pitch wheels when unloading music in preparation
        /// for the next piece of music.
        /// </summary>
        CenterPitchWheelOnUnload = 4,

        /// <summary>
        /// Sends a sustain off event when a notes off event is triggered.
        /// Stops hanging notes.
        /// </summary>
        SendSustainOffOnNotesOff = 5
    }

    public class Track
    {
        public long Position { get; set; }
    }

    public class EventInfo
    {
        /// <summary>
        /// Position in the MIDI stream where the event starts.
        /// For delta-based MIDI streams (e.g. SMF and XMIDI), this points to the delta.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// The number of ticks after the previous event that this event should occur.
        /// </summary>
        public long Delta { get; set; }

        /// <summary>
        /// Gets or sets the event.
        /// </summary>
        /// <value>The event.</value>
        /// <remarks>
        /// Upper 4 bits are the command code, lower 4 bits are the MIDI channel.
        /// For META, event == 0xFF. For SysEx, event == 0xF0.
        /// </remarks>
        public int Event { get; set; }

        /// <summary>
        /// Gets or sets the first parameter in a simple MIDI message.
        /// </summary>
        /// <value>The param1.</value>
        public int Param1 { get; set; }

        /// <summary>
        /// Gets or sets the second parameter in a simple MIDI message.
        /// </summary>
        /// <value>The param2.</value>
        public int Param2 { get; set; }

        /// <summary>
        /// Gets or sets the the META type.
        /// </summary>
        /// <value>The the META type.</value>
        public int MetaType { get; set; }

        /// <summary>
        /// Gets or sets the start of the data.
        /// </summary>
        /// <value>The data.</value>
        /// <remarks>For META and SysEx events, this points to the start of the data.</remarks>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets the the MIDI channel.
        /// </summary>
        /// <value>The MIDI channel.</value>
        public int Channel { get { return Event & 0x0F; } }

        /// <summary>
        /// Gets the the command code.
        /// </summary>
        /// <value>The command.</value>
        public int Command { get { return Event >> 4; } }

        public EventInfo()
        {
        }

        public EventInfo(EventInfo info)
        {
            Start = info.Start;
            Delta = info.Delta;
            Event = info.Event;
            Param1 = info.Param1;
            Param2 = info.Param2;
            MetaType = info.MetaType;
            Data = info.Data;
        }
    }

    public abstract class MidiParser
    {
        public static MidiParser CreateRO()
        {
            throw new NotImplementedException();
        }

        public static MidiParser CreateXMidiParser()
        {
            throw new NotImplementedException();
        }

        public static MidiParser CreateSmfParser()
        {
            return new SmfMidiParser();
        }

        /// <summary>
        /// Gets or sets the midi driver, the device to which all events will be transmitted..
        /// </summary>
        /// <value>The midi driver.</value>
        public IMidiDriver MidiDriver { get; set; }

        /// <summary>
        /// Gets or sets the pulses per quarter note.
        /// </summary>
        /// <value>The pulses per quarter note.</value>
        /// <remarks>We refer to "pulses" as "ticks".</remarks>
        public int PulsesPerQuarterNote { get; set; }

        public abstract void LoadMusic(byte[] data);

        public void UnloadMusic()
        {
            ResetTracking();
            AllNotesOff();
            NumTracks = 0;
            activeTrack = 255;
            AbortParse = true;

            if (CenterPitchWheelOnUnload)
            {
                // Center the pitch wheels in preparation for the next piece of
                // music. It's not safe to do this from within allNotesOff(),
                // and might not even be safe here, so we only do it if the
                // client has explicitly asked for it.

                if (MidiDriver != null)
                {
                    for (int i = 0; i < 16; ++i)
                    {
                        SendToDriver(0xE0 | i, 0, 0x40);
                    }
                }
            }
        }

        /// <summary>
        /// Pulses Per Quarter Note. (We refer to "pulses" as "ticks".)
        /// </summary>
        uint _ppqn;
        /// <summary>
        /// Microseconds per quarter note.
        /// </summary>
        int tempo;
        /// <summary>
        /// Microseconds per tick (_tempo / _ppqn). default = 500000 / 96
        /// </summary>
        int _psecPerTick = 5208;
        /// <summary>
        /// For lightweight clients that don't provide their own flow control.
        /// </summary>
        bool _autoLoop;
        /// <summary>
        /// Support smart expiration of hanging notes when jumping.
        /// </summary>
        bool _smartJump;
        /// <summary>
        /// The next event to transmit. Events are preparsed
        /// so each event is parsed only once; this permits
        /// simulated events in certain formats.
        /// </summary>
        EventInfo _nextEvent;
        /// <summary>
        /// True if currently inside jumpToTick.
        /// </summary>
        bool _jumpingToTick;

        public int Tempo
        {
            get{ return tempo; }
            set
            {
                tempo = value;
                if (PulsesPerQuarterNote != 0)
                {
                    _psecPerTick = (tempo + (PulsesPerQuarterNote >> 2)) / PulsesPerQuarterNote;
                }
            }
        }

        public uint TimerRate
        {
            get;
            set;
        }

        public bool IsPlaying { get { return (Position.PlayPos != 0); } }

        public uint PPQN { get { return _ppqn; } }

        public virtual int Tick { get { return Position.PlayTick; } }

        int activeTrack;

        /// <summary>
        /// Gets or sets the currently active track, in multi-track formats.
        /// </summary>
        /// <value>The active track.</value>
        public int ActiveTrack
        {
            get{ return activeTrack; }
            set
            {

                if (value < 0 || value >= NumTracks)
                    return;

                // We allow restarting the track via setTrack when
                // it isn't playing anymore. This allows us to reuse
                // a MidiParser when a track has finished and will
                // be restarted via setTrack by the client again.
                // This isn't exactly how setTrack behaved before though,
                // the old MidiParser code did not allow setTrack to be
                // used to restart a track, which was already finished.
                //
                // TODO: Check if any engine has problem with this
                // handling, if so we need to find a better way to handle
                // track restarts. (KYRA relies on this working)
                if (value == ActiveTrack && IsPlaying)
                    return;

                if (SmartJump)
                    HangAllActiveNotes();
                else
                    AllNotesOff();

                ResetTracking();
                Array.Clear(ActiveNotes, 0, ActiveNotes.Length);
                activeTrack = value;
                Position.PlayPos = Tracks[value].Position;
                ParseNextEvent(NextEvent);
            }
        }

        public bool JumpToTick(uint tick, bool fireEvents = false, bool stopNotes = true, bool dontSendNoteOn = false)
        {
            if (ActiveTrack >= NumTracks)
                return false;

            Debug.Assert(!_jumpingToTick); // This function is not re-entrant
            _jumpingToTick = true;

            Tracker currentPos = new Tracker(Position);
            var currentEvent = new EventInfo(_nextEvent);

            ResetTracking();
            Position.PlayPos = Tracks[ActiveTrack].Position;
            ParseNextEvent(_nextEvent);
            if (tick > 0)
            {
                while (true)
                {
                    EventInfo info = _nextEvent;
                    if (Position.LastEventTick + info.Delta >= tick)
                    {
                        Position.PlayTime += (int)(tick - Position.LastEventTick) * _psecPerTick;
                        Position.PlayTick = (int)tick;
                        break;
                    }

                    Position.LastEventTick += info.Delta;
                    Position.LastEventTime += (int)info.Delta * _psecPerTick;
                    Position.PlayTick = (int)Position.LastEventTick;
                    Position.PlayTime = Position.LastEventTime;

                    // Some special processing for the fast-forward case
                    if (info.Command == 0x9 && dontSendNoteOn)
                    {
                        // Don't send note on; doing so creates a "warble" with
                        // some instruments on the MT-32. Refer to patch #3117577
                    }
                    else if (info.Event == 0xFF && info.MetaType == 0x2F)
                    {
                        // End of track
                        // This means that we failed to find the right tick.
                        Position = currentPos;
                        _nextEvent = currentEvent;
                        _jumpingToTick = false;
                        return false;
                    }
                    else
                    {
                        ProcessEvent(info, fireEvents);
                    }

                    ParseNextEvent(_nextEvent);
                }
            }

            if (stopNotes)
            {
                if (_smartJump || currentPos.PlayPos == 0)
                {
                    AllNotesOff();
                }
                else
                {
                    var targetEvent = new EventInfo(_nextEvent);
                    var targetPosition = new Tracker(Position);

                    Position = currentPos;
                    _nextEvent = currentEvent;
                    HangAllActiveNotes();

                    _nextEvent = targetEvent;
                    Position = targetPosition;
                }
            }

            AbortParse = true;
            _jumpingToTick = false;
            return true;
        }

        bool ProcessEvent(EventInfo info, bool fireEvents = true)
        {
            if (info.Event == 0xF0)
            {
                // SysEx event
                // Check for trailing 0xF7 -- if present, remove it.
                if (fireEvents)
                {
                    if (info.Data[info.Data.Length - 1] == 0xF7)
                        MidiDriver.SysEx(info.Data, (ushort)(info.Data.Length - 1));
                    else
                        MidiDriver.SysEx(info.Data, (ushort)info.Data.Length);
                }
            }
            else if (info.Event == 0xFF)
            {
                // META event
                if (info.MetaType == 0x2F)
                {
                    // End of Track must be processed by us,
                    // as well as sending it to the output device.
                    if (_autoLoop)
                    {
                        JumpToTick(0);
                        ParseNextEvent(_nextEvent);
                    }
                    else
                    {
                        StopPlaying();
                        if (fireEvents)
                            MidiDriver.MetaEvent((byte)info.MetaType, info.Data, (ushort)info.Data.Length);
                    }
                    return false;
                }
                else if (info.MetaType == 0x51)
                {
                    if (info.Data.Length >= 3)
                    {
                        Tempo = (info.Data[0] << 16 | info.Data[1] << 8 | info.Data[2]);
                    }
                }
                if (fireEvents)
                    MidiDriver.MetaEvent((byte)info.MetaType, info.Data, (ushort)info.Data.Length);
            }
            else
            {
                if (fireEvents)
                    SendToDriver(info.Event, info.Param1, info.Param2);
            }

            return true;
        }

        public void StopPlaying()
        {
            AllNotesOff();
            ResetTracking();
        }

        /// <summary>
        /// Gets or sets the next event to transmit.
        /// </summary>
        /// <value>The next event.</value>
        /// <remarks>>
        /// Events are preparsed so each event is parsed only once; this permits
        /// simulated events in certain formats.
        /// </remarks>
        protected EventInfo NextEvent { get { return _nextEvent; } set { _nextEvent = value; } }

        protected abstract void ParseNextEvent(EventInfo info);

        protected void HangAllActiveNotes()
        {
            // Search for note off events until we have
            // accounted for every active note.
            var tempActive = new ushort[128];
            Array.Copy(ActiveNotes, tempActive, ActiveNotes.Length);

            var advanceTick = Position.LastEventTick;
            while (true)
            {
                int i;
                for (i = 0; i < 128; ++i)
                    if (tempActive[i] != 0)
                        break;
                if (i == 128)
                    break;
                ParseNextEvent(NextEvent);
                advanceTick += NextEvent.Delta;
                if (NextEvent.Command == 0x8)
                {
                    if ((tempActive[NextEvent.Param1] & (1 << NextEvent.Channel)) != 0)
                    {
                        HangingNote(NextEvent.Channel, NextEvent.Param1, (int)(advanceTick - Position.LastEventTick) * _psecPerTick, false);
                        tempActive[NextEvent.Param1] &= (ushort)~(1 << NextEvent.Channel);
                    }
                }
                else if (NextEvent.Event == 0xFF && NextEvent.MetaType == 0x2F)
                {
                    // warning("MidiParser::hangAllActiveNotes(): Hit End of Track with active notes left");
                    for (i = 0; i < 128; ++i)
                    {
                        for (int j = 0; j < 16; ++j)
                        {
                            if ((tempActive[i] & (1 << j)) != 0)
                            {
                                ActiveNote(j, i, false);
                                SendToDriver(0x80 | j, i, 0);
                            }
                        }
                    }
                    break;
                }
            }
        }

        protected void HangingNote(int channel, int note, int timeLeft, bool recycle = true)
        {
            NoteTimer best = null;

            if (HangingNotesCount >= HangingNotes.Length)
            {
//                Console.Error.WriteLine("MidiParser::hangingNote(): Exceeded polyphony");
                return;
            }

            foreach (var hangingNote in HangingNotes.Reverse())
            {
                if (hangingNote.Channel == channel && hangingNote.Note == note)
                {
                    if (hangingNote.TimeLeft != 0 && hangingNote.TimeLeft < timeLeft && recycle)
                        return;
                    best = hangingNote;
                    if (hangingNote.TimeLeft != 0)
                    {
                        if (recycle)
                            SendToDriver(0x80 | channel, note, 0);
                        --HangingNotesCount;
                    }
                    break;
                }
                else if (best == null && hangingNote.TimeLeft == 0)
                {
                    best = hangingNote;
                }
            }

            // Occassionally we might get a zero or negative note
            // length, if the note should be turned on and off in
            // the same iteration. For now just set it to 1 and
            // we'll turn it off in the next cycle.
            if (timeLeft == 0 || ((timeLeft & 0x80000000) != 0))
                timeLeft = 1;

            if (best != null)
            {
                best.Channel = channel;
                best.Note = note;
                best.TimeLeft = timeLeft;
                ++HangingNotesCount;
            }
            else
            {
                // We checked this up top. We should never get here!
//                Console.Error.WriteLine("MidiParser::hangingNote(): Internal error");
            }
        }

        protected void ActiveNote(int channel, int note, bool active)
        {
            if (note >= 128 || channel >= 16)
                return;

            if (active)
                ActiveNotes[note] |= (ushort)(1 << channel);
            else
                ActiveNotes[note] &= (ushort)~(1 << channel);

            // See if there are hanging notes that we can cancel
            foreach (var hangingNote in HangingNotes.Reverse())
            {
                if (hangingNote.Channel == channel && hangingNote.Note != 0 && hangingNote.TimeLeft != 0)
                {
                    hangingNote.TimeLeft = 0;
                    --HangingNotesCount;
                    break;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="NScumm.Core.MidiParser"/> supports smart expiration of hanging notes when jumping.
        /// </summary>
        /// <value><c>true</c> if smart jump; otherwise, <c>false</c>.</value>
        protected bool SmartJump { get; set; }

        protected virtual void SendToDriver(int data)
        {
            MidiDriver.Send(data);
        }

        protected void SendToDriver(int status, int firstOp, int secondOp)
        {
            SendToDriver(status | (firstOp << 8) | (secondOp << 16));
        }

        /// <summary>
        /// Gets or sets the number of total tracks for multi-track MIDI formats. 1 for single-track formats.
        /// </summary>
        /// <value>The number tracks.</value>
        protected int NumTracks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="NScumm.Core.MidiParser"/> abort parse.
        /// </summary>
        /// <value><c>true</c> if abort parse; otherwise, <c>false</c>.</value>
        protected bool AbortParse { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether center the pitch wheels when unloading a song or not.
        /// </summary>
        /// <value><c>true</c> if center pitch wheel on unload; otherwise, <c>false</c>.</value>
        protected bool CenterPitchWheelOnUnload { get; set; }

        internal Tracker Position { get; set; }

        /// <summary>
        /// Each uint16 is a bit mask for channels that have that note on.
        /// </summary>
        protected ushort[] ActiveNotes { get; private set; }

        internal NoteTimer[] HangingNotes { get; private set; }

        /// <summary>
        /// Gets or sets the count of hanging notes, used to optimize expiration.
        /// </summary>
        /// <value>The hanging notes count.</value>
        protected int HangingNotesCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Send a sustain off on a notes off event, stopping hanging notes or not.
        /// </summary>
        /// <value><c>true</c> if send sustain off on notes off; otherwise, <c>false</c>.</value>
        protected bool SendSustainOffOnNotesOff { get; set; }

        protected Track[] Tracks { get; private set; }

        protected MidiParser()
        {
            ActiveNotes = new ushort[128];
            HangingNotes = new NoteTimer[32];
            for (int i = 0; i < HangingNotes.Length; i++)
            {
                HangingNotes[i] = new NoteTimer();
            }
            Tracks = new Track[120];

            TimerRate = 0x4A0000;
            _ppqn = 96;
            tempo = 500000;
            _psecPerTick = 5208;// 500000 / 96
            _nextEvent = new EventInfo();
            _nextEvent.Start = 0;
            _nextEvent.Delta = 0;
            _nextEvent.Event = 0;
            Position = new Tracker();
        }

        public void Property(MidiParserProperty prop, int value)
        {
            switch (prop)
            {
                case MidiParserProperty.AutoLoop:
                    _autoLoop = (value != 0);
                    break;
                case MidiParserProperty.SmartJump:
                    _smartJump = (value != 0);
                    break;
                case MidiParserProperty.CenterPitchWheelOnUnload:
                    CenterPitchWheelOnUnload = (value != 0);
                    break;
                case MidiParserProperty.SendSustainOffOnNotesOff:
                    SendSustainOffOnNotesOff = (value != 0);
                    break;
            }
        }

        protected virtual void ResetTracking()
        {
            Position.Clear();
        }

        protected virtual void AllNotesOff()
        {
            if (MidiDriver == null)
                return;

            // Turn off all active notes
            for (var i = 0; i < 128; ++i)
            {
                for (var j = 0; j < 16; ++j)
                {
                    if ((ActiveNotes[i] & (1 << j)) != 0)
                    {
                        SendToDriver(0x80 | j, i, 0);
                    }
                }
            }

            // Turn off all hanging notes
            for (var i = 0; i < HangingNotes.Length; i++)
            {
                if (HangingNotes[i].TimeLeft > 0)
                {
                    SendToDriver(0x80 | HangingNotes[i].Channel, HangingNotes[i].Note, 0);
                    HangingNotes[i].TimeLeft = 0;
                }
            }
            HangingNotesCount = 0;

            // To be sure, send an "All Note Off" event (but not all MIDI devices
            // support this...).
            for (var i = 0; i < 16; ++i)
            {
                SendToDriver(0xB0 | i, 0x7b, 0); // All notes off
                if (SendSustainOffOnNotesOff)
                    SendToDriver(0xB0 | i, 0x40, 0); // Also send a sustain off event (bug #3116608)
            }

            Array.Clear(ActiveNotes, 0, ActiveNotes.Length);
        }

        protected static int ReadVLQ(Stream input)
        {
            int value = 0;

            for (var i = 0; i < 4; ++i)
            {
                var str = input.ReadByte();
                value = (value << 7) | (str & 0x7F);
                if ((str & 0x80) == 0)
                    break;
            }
            return value;
        }

        public void OnTimer()
        {
            uint endTime;
            uint eventTime;

            if (Position.PlayPos == 0 || MidiDriver == null)
                return;

            AbortParse = false;
            endTime = (uint)Position.PlayTime + TimerRate;

            // Scan our hanging notes for any
            // that should be turned off.
            if (HangingNotesCount != 0)
            {
                foreach (var ptr in HangingNotes)
                {
                    if (ptr.TimeLeft != 0)
                    {
                        if (ptr.TimeLeft <= TimerRate)
                        {
                            SendToDriver(0x80 | ptr.Channel, ptr.Note, 0);
                            ptr.TimeLeft = 0;
                            --HangingNotesCount;
                        }
                        else
                        {
                            ptr.TimeLeft -= (int)TimerRate;
                        }
                    }
                }
            }

            while (!AbortParse)
            {
                var info = _nextEvent;

                eventTime = (uint)(Position.LastEventTime + info.Delta * _psecPerTick);
                if (eventTime > endTime)
                    break;

                // Process the next info.
                Position.LastEventTick += info.Delta;
                if (info.Event < 0x80)
                {
//                    Console.Error.WriteLine("Bad command or running status {0:X2}", info.Event);
                    Position.PlayPos = 0;
                    return;
                }

                if (info.Command == 0x8)
                {
                    ActiveNote(info.Channel, info.Param1, false);
                }
                else if (info.Command == 0x9)
                {
                    if (info.Data.Length > 0)
                        HangingNote(info.Channel, info.Param1, (int)(info.Data.Length * _psecPerTick - (endTime - eventTime)));
                    else
                        ActiveNote(info.Channel, info.Param1, true);
                }

                // Player::metaEvent() in SCUMM will delete the parser object,
                // so return immediately if that might have happened.
                bool ret = ProcessEvent(info);
                if (!ret)
                    return;

                if (!AbortParse)
                {
                    Position.LastEventTime = (int)eventTime;
                    ParseNextEvent(_nextEvent);
                }
            }

            if (!AbortParse)
            {
                Position.PlayTime = (int)endTime;
                Position.PlayTick = (int)((Position.PlayTime - Position.LastEventTime) / _psecPerTick + Position.LastEventTick);
            }
        }
    }
}

