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
using NScumm.Sci.Sound.Drivers;
using NScumm.Core;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound
{
    enum SciMidiCommands
    {
        SetSignalLoop = 0x7F,
        EndOfTrack = 0xFC,
        SetReverb = 0x50,
        MidiHold = 0x52,
        UpdateCue = 0x60,
        ResetOnPause = 0x4C
    }

    /// <summary>
    /// An extended standard MIDI (SMF) parser. Sierra used an extra channel
    /// with special commands for extended functionality and animation syncing.
    /// Refer to MidiParser_SMF() in /sound/midiparser_smf.cpp for the standard
    /// MIDI (SMF) parser functionality that the SCI MIDI parser is based on
    /// </summary>
    internal class MidiParser_SCI : MidiParser
    {
        // this is set, when main thread calls us . we send commands to queue instead to driver
        private bool _mainThreadCalled;

        private SciVersion _soundVersion;
        private byte[] _mixedData;
        private SoundResource.Track _track;
        private MusicEntry _pSnd;
        private int _loopTick;
        private byte _masterVolume; // the overall master volume (same for all tracks)
        private byte _volume; // the global volume of the current track

        private bool _resetOnPause;

        private bool[] _channelUsed = new bool[16];
        private short[] _channelRemap = new short[16];
        private bool[] _channelMuted = new bool[16];
        private byte[] _channelVolume = new byte[16];

        private class ChannelState
        {
            public sbyte _modWheel;
            public sbyte _pan;
            public sbyte _patch;
            public sbyte _note;
            public bool _sustain;
            public short _pitchWheel;
            public sbyte _voices;
        }

        private ChannelState[] _channelState;

        private static readonly int[] nMidiParams = { 2, 2, 2, 2, 1, 1, 2, 0 };
        private SciMusic _music;

        public byte SongReverb
        {
            get
            {
                //assert(_track);

                if (_soundVersion >= SciVersion.V1_EARLY)
                {
                    for (int i = 0; i < _track.channelCount; i++)
                    {
                        SoundResource.Channel channel = _track.channels[i];
                        // Peek ahead in the control channel to get the default reverb setting
                        if (channel.number == 15 && channel.size >= 7)
                            return channel.data[6];
                    }
                }

                return 127;
            }
        }


        public MidiParser_SCI(SciVersion soundVersion, SciMusic music)
        {
            _soundVersion = soundVersion;
            _music = music;

            // mididata contains delta in 1/60th second
            // values of ppqn and tempo are found experimentally and may be wrong
            PulsesPerQuarterNote = 1;
            Tempo = 16667;

            _masterVolume = 15;
            _volume = 127;

            _channelState = new ChannelState[16];
            for (int i = 0; i < _channelState.Length; i++)
            {
                _channelState[i] = new ChannelState();
            }

            ResetStateTracking();
        }

        /// <summary>
        /// this is used for scripts sending midi commands to us. we verify in that case that the channel is actually
        /// used, so that channel remapping will work as well and then send them on
        /// </summary>
        /// <param name="midi">Midi.</param>
        public void SendFromScriptToDriver(uint midi)
        {
            byte midiChannel = (byte)(midi & 0xf);

            if (!_channelUsed[midiChannel])
            {
                // trying to send to an unused channel
                //  this happens for cmdSendMidi at least in sq1vga right at the start, it's a script issue
                return;
            }
            SendToDriver((int)midi);
        }

        public void RemapChannel(int channel, int devChannel)
        {
            if (_channelRemap[channel] == devChannel)
                return;

            _channelRemap[channel] = (short)devChannel;

            if (devChannel == -1)
                return;

            //  debug("  restoring state: channel %d on devChannel %d", channel, devChannel);

            // restore state
            ChannelState s = _channelState[channel];

            int channelVolume = _channelVolume[channel];
            channelVolume = (channelVolume * _volume / 127) & 0xFF;
            byte pitch1 = (byte)(s._pitchWheel & 0x7F);
            byte pitch2 = (byte)((s._pitchWheel >> 7) & 0x7F);

            SendToDriver_raw(0x0040B0 | devChannel); // sustain off
            SendToDriver_raw(0x004BB0 | devChannel | (s._voices << 16));
            SendToDriver_raw(0x0000C0 | devChannel | (s._patch << 8));
            SendToDriver_raw(0x0007B0 | devChannel | (channelVolume << 16));
            SendToDriver_raw(0x000AB0 | devChannel | (s._pan << 16));
            SendToDriver_raw(0x0001B0 | devChannel | (s._modWheel << 16));
            SendToDriver_raw(0x0040B0 | devChannel | (s._sustain ? 0x7F0000 : 0));
            SendToDriver_raw(0x0000E0 | devChannel | (pitch1 << 8) | (pitch2 << 16));

            // CHECKME: Some SSCI version send a control change 0x4E with s._note as
            // parameter.
            // We need to investigate how (and if) drivers should act on this.
            // Related: controller 0x4E is used for 'mute' in the midiparser.
            // This could be a bug in SSCI that went unnoticed because few (or no?)
            // drivers implement controller 0x4E

            // NB: The line below is _not_ valid since s._note can be 0xFF.
            // SSCI handles this out of band in the driver interface.
            // sendToDriver_raw(0x004EB0 | devChannel | (s._note << 16);
        }

        public void SetMasterVolume(byte masterVolume)
        {
            //assert(masterVolume <= MUSIC_MASTERVOLUME_MAX);
            _masterVolume = masterVolume;
            switch (_soundVersion)
            {
                case SciVersion.V0_EARLY:
                case SciVersion.V0_LATE:
                    // update driver master volume
                    SetVolume(_volume);
                    break;

                case SciVersion.V1_EARLY:
                case SciVersion.V1_LATE:
                case SciVersion.V2_1:
                    // directly set master volume (global volume is merged with channel volumes)
                    ((MidiPlayer)MidiDriver).Volume = masterVolume;
                    break;

                default:
                    throw new InvalidOperationException("MidiParser_SCI::setVolume: Unsupported soundVersion");
            }
        }

        public void SetVolume(byte volume)
        {
            // assert(volume <= MUSIC_VOLUME_MAX);
            _volume = volume;

            switch (_soundVersion)
            {
                case SciVersion.V0_EARLY:
                case SciVersion.V0_LATE:
                    {
                        // SCI0 adlib driver doesn't support channel volumes, so we need to go this way
                        short globalVolume = (short)(_volume * _masterVolume / SoundCommandParser.MUSIC_VOLUME_MAX);
                        ((MidiPlayer)MidiDriver).Volume = (byte)globalVolume;
                        break;
                    }

                case SciVersion.V1_EARLY:
                case SciVersion.V1_LATE:
                case SciVersion.V2_1:
                    // Send previous channel volumes again to actually update the volume
                    for (int i = 0; i < 15; i++)
                        if (_channelRemap[i] != -1)
                            SendToDriver(0xB0 + i, 7, _channelVolume[i]);
                    break;

                default:
                    Error("MidiParser_SCI::setVolume: Unsupported soundVersion");
                    break;
            }
        }

        public void Stop()
        {
            AbortParse = true;
            AllNotesOff();
        }

        public void Pause()
        {
            AllNotesOff();
            if (_resetOnPause)
                JumpToTick(0);
        }

        public void MainThreadBegin()
        {
            System.Diagnostics.Debug.Assert(!_mainThreadCalled);
            _mainThreadCalled = true;
        }

        public bool LoadMusic(SoundResource.Track track, MusicEntry pSnd, int channelFilterMask, SciVersion soundVersion)
        {
            UnloadMusic();
            _track = track;
            _pSnd = pSnd;
            _soundVersion = soundVersion;

            for (int i = 0; i < 16; i++)
            {
                _channelUsed[i] = false;
                _channelMuted[i] = false;
                _channelVolume[i] = 127;

                if (_soundVersion <= SciVersion.V0_LATE)
                    _channelRemap[i] = (short)i;
                else
                    _channelRemap[i] = -1;
            }

            // FIXME: SSCI does not always start playing a track at the first byte.
            // By default it skips 10 (or 13?) bytes containing prio/voices, patch,
            // volume, pan commands in fixed locations, and possibly a signal
            // in channel 15. We should initialize state tracking to those values
            // so that they automatically get set up properly when the channels get
            // mapped. See also the related FIXME in MidiParser_SCI::processEvent.

            if (channelFilterMask != 0)
            {
                // SCI0 only has 1 data stream, but we need to filter out channels depending on music hardware selection
                MidiFilterChannels(channelFilterMask);
            }
            else
            {
                MidiMixChannels();
            }

            NumTracks = 1;
            Tracks[0] = new BytePtr(_mixedData);
            if (_pSnd != null)
            {
                ActiveTrack = 0;
            }
            _loopTick = 0;

            return true;
        }

        public override void UnloadMusic()
        {
            if (_pSnd != null)
            {
                ResetTracking();
                AllNotesOff();
            }
            NumTracks = 0;
            ActiveTrack = 255;
            _resetOnPause = false;

            _mixedData = null;
        }

        public void MainThreadEnd()
        {
            System.Diagnostics.Debug.Assert(_mainThreadCalled);
            _mainThreadCalled = false;
        }

        public void SendInitCommands()
        {
            ResetStateTracking();

            // reset our "global" volume
            _volume = 127;

            // Set initial voice count
            if (_pSnd != null)
            {
                if (_soundVersion <= SciVersion.V0_LATE)
                {
                    for (int i = 0; i < 15; ++i)
                    {
                        byte voiceCount = 0;
                        if (_channelUsed[i])
                        {
                            voiceCount = _pSnd.soundRes.GetInitialVoiceCount(i);
                            SendToDriver(0xB0 | i, 0x4B, voiceCount);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _track.channelCount; ++i)
                    {
                        byte voiceCount = _track.channels[i].poly;
                        byte num = _track.channels[i].number;
                        // TODO: Should we skip the control channel?
                        SendToDriver(0xB0 | num, 0x4B, voiceCount);
                    }
                }
            }

            // Reset all the parameters of the channels used by this song
            for (int i = 0; i < 16; ++i)
            {
                if (_channelUsed[i])
                {
                    SendToDriver(0xB0 | i, 0x07, 127);  // Reset volume to maximum
                    SendToDriver(0xB0 | i, 0x0A, 64);   // Reset panning to center
                    SendToDriver(0xB0 | i, 0x40, 0);    // Reset hold pedal to none
                    SendToDriver(0xB0 | i, 0x4E, 0);    // Reset velocity to none
                    SendToDriver(0xE0 | i, 0, 64);  // Reset pitch wheel to center
                }
            }
        }


        protected override void ParseNextEvent(EventInfo info)
        {
            info.Start = Position.PlayPos;
            info.Delta = 0;
            while (Position.PlayPos.Value == 0xF8)
            {
                info.Delta += 240;
                Position.PlayPos.Offset++;
            }
            info.Delta += Position.PlayPos[0]; Position.PlayPos.Offset++;

            // Process the next info.
            if ((Position.PlayPos[0] & 0xF0) >= 0x80)
            {
                info.Event = Position.PlayPos[0]; Position.PlayPos.Offset++;
            }
            else
                info.Event = (byte)Position.RunningStatus;
            if (info.Event < 0x80)
                return;

            Position.RunningStatus = info.Event;
            switch (info.Command)
            {
                case 0xC:
                    info.Param1 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                    info.Param2 = 0;
                    break;
                case 0xD:
                    info.Param1 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                    info.Param2 = 0;
                    break;

                case 0xB:
                    info.Param1 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                    info.Param2 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                    info.Length = 0;
                    break;

                case 0x8:
                case 0x9:
                case 0xA:
                case 0xE:
                    info.Param1 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                    info.Param2 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                    if (info.Command == 0x9 && info.Param2 == 0)
                    {
                        // NoteOn with param2==0 is a NoteOff
                        info.Event = (byte)(info.Channel | 0x80);
                    }
                    info.Length = 0;
                    break;

                case 0xF: // System Common, Meta or SysEx event
                    switch (info.Event & 0x0F)
                    {
                        case 0x2: // Song Position Pointer
                            info.Param1 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                            info.Param2 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                            break;

                        case 0x3: // Song Select
                            info.Param1 = Position.PlayPos[0]; Position.PlayPos.Offset++;
                            info.Param2 = 0;
                            break;

                        case 0x6:
                        case 0x8:
                        case 0xA:
                        case 0xB:
                        case 0xC:
                        case 0xE:
                            info.Param1 = info.Param2 = 0;
                            break;

                        case 0x0: // SysEx
                            info.Length = ReadVLQ(ref Position.PlayPos);
                            info.Data = Position.PlayPos;
                            Position.PlayPos.Offset += info.Length;
                            info.Length = 0;
                            break;

                        case 0xF: // META event
                            info.MetaType = Position.PlayPos[0]; Position.PlayPos.Offset++;
                            info.Length = ReadVLQ(ref Position.PlayPos);
                            info.Data = Position.PlayPos;
                            Position.PlayPos.Offset += info.Length;
                            break;
                        default:
                            Warning("MidiParser_SCI::parseNextEvent: Unsupported event code {0:X}", info.Event);
                            break;
                    } // // System Common, Meta or SysEx event
                    break;
            }// switch (info.command())
        }

        protected override bool ProcessEvent(EventInfo info, bool fireEvents = true)
        {
            if (!fireEvents)
            {
                // We don't do any processing that should be done while skipping events
                return base.ProcessEvent(info, fireEvents);
            }

            switch (info.Command)
            {
                case 0xC:
                    if (info.Channel == 0xF)
                    {// SCI special case
                        if (info.Param1 != (int)SciMidiCommands.SetSignalLoop)
                        {
                            // At least in kq5/french&mac the first scene in the intro has
                            // a song that sets signal to 4 immediately on tick 0. Signal
                            // isn't set at that point by sierra sci and it would cause the
                            // castle daventry text to get immediately removed, so we
                            // currently filter it. Sierra SCI ignores them as well at that
                            // time. However, this filtering should only be performed for
                            // SCI1 and newer games. Signalling is done differently in SCI0
                            // though, so ignoring these signals in SCI0 games will result
                            // in glitches (e.g. the intro of LB1 Amiga gets stuck - bug
                            // #3297883). Refer to MusicEntry::setSignal() in sound/music.cpp.
                            // FIXME: SSCI doesn't start playing at the very beginning
                            // of the stream, but at a fixed location a few commands later.
                            // That is probably why this signal isn't triggered
                            // immediately there.
                            bool skipSignal = false;
                            if (_soundVersion >= SciVersion.V1_EARLY)
                            {
                                if (Position.PlayTick == 0)
                                {
                                    skipSignal = true;
                                    switch (SciEngine.Instance.GameId)
                                    {
                                        case SciGameId.ECOQUEST2:
                                            // In Eco Quest 2 room 530 - gonzales is supposed to dance
                                            // WORKAROUND: we need to signal in this case on tick 0
                                            // this whole issue is complicated and can only be properly fixed by
                                            // changing the whole parser to a per-channel parser. SSCI seems to
                                            // start each channel at offset 13 (may be 10 for us) and only
                                            // starting at offset 0 when the music loops to the initial position.
                                            if (SciEngine.Instance.EngineState.CurrentRoomNumber == 530)
                                                skipSignal = false;
                                            break;
                                    }
                                }
                            }
                            if (!skipSignal)
                            {
                                if (!_jumpingToTick)
                                {
                                    _pSnd.SetSignal(info.Param1);
                                    // TODO: debugC(4, kDebugLevelSound, "signal %04x", info.basic.param1);
                                }
                            }
                        }
                        else
                        {
                            _loopTick = Position.PlayTick;
                        }

                        // Done with this event.
                        return true;
                    }

                    // Break to let parent handle the rest.
                    break;
                case 0xB:
                    // Reference for some events:
                    // http://wiki.scummvm.org/index.php/SCI/Specifications/Sound/SCI0_Resource_Format#Status_Reference
                    // Handle common special events
                    switch (info.Param1)
                    {
                        case (int)SciMidiCommands.SetReverb:
                            if (info.Param2 == 127)       // Set global reverb instead
                                _pSnd.reverb = _music.GlobalReverb;
                            else
                                _pSnd.reverb = (sbyte)info.Param2;

                            ((MidiPlayer)MidiDriver).Reverb = _pSnd.reverb;
                            break;
                    }

                    // Handle events sent to the SCI special channel (15)
                    if (info.Channel == 0xF)
                    {
                        switch (info.Param1)
                        {
                            case (int)SciMidiCommands.SetReverb:
                                // Already handled above
                                return true;
                            case (int)SciMidiCommands.MidiHold:
                                // Check if the hold ID marker is the same as the hold ID
                                // marker set for that song by cmdSetSoundHold.
                                // If it is, loop back, but don't stop notes when jumping.
                                if (info.Param2 == _pSnd.hold)
                                {
                                    JumpToTick((uint)_loopTick, false, false);
                                    // Done with this event.
                                    return true;
                                }
                                return true;
                            case (int)SciMidiCommands.UpdateCue:
                                if (!_jumpingToTick)
                                {
                                    int inc;
                                    switch (_soundVersion)
                                    {
                                        case SciVersion.V0_EARLY:
                                        case SciVersion.V0_LATE:
                                            inc = info.Param2;
                                            break;
                                        case SciVersion.V1_EARLY:
                                        case SciVersion.V1_LATE:
                                        case SciVersion.V2_1:
                                            inc = 1;
                                            break;
                                        default:
                                            throw new InvalidOperationException("unsupported _soundVersion");
                                    }
                                    _pSnd.dataInc += inc;
                                    // TODO: debugC(4, kDebugLevelSound, "datainc %04x", inc);

                                }
                                return true;
                            case (int)SciMidiCommands.ResetOnPause:
                                _resetOnPause = info.Param2 != 0;
                                return true;
                            // Unhandled SCI commands
                            case 0x46: // LSL3 - binoculars
                            case 0x61: // Iceman (AdLib?)
                            case 0x73: // Hoyle
                            case 0xD1: // KQ4, when riding the unicorn
                                       // Obscure SCI commands - ignored
                                return true;
                            // Standard MIDI commands
                            case 0x01:  // mod wheel
                            case 0x04:  // foot controller
                            case 0x07:  // channel volume
                            case 0x0A:  // pan
                            case 0x0B:  // expression
                            case 0x40:  // sustain
                            case 0x79:  // reset all
                            case 0x7B:  // notes off
                                        // These are all handled by the music driver, so ignore them
                                break;
                            case 0x4B:  // voice mapping
                                        // TODO: is any support for this needed at the MIDI parser level?
                                Warning("Unhanded SCI MIDI command 0x{0:X} - voice mapping (parameter {1})", info.Param1, info.Param2);
                                return true;
                            default:
                                Warning("Unhandled SCI MIDI command 0x{0:X} (parameter {1})", info.Param1, info.Param2);
                                return true;
                        }

                    }

                    // Break to let parent handle the rest.
                    break;
                case 0xF: // META event
                    if (info.MetaType == 0x2F)
                    {// end of track reached
                        if (_pSnd.loop != 0)
                            _pSnd.loop--;
                        // QFG3 abuses the hold flag. Its scripts call kDoSoundSetHold,
                        // but sometimes there's no hold marker in the associated songs
                        // (e.g. song 110, during the intro). The original interpreter
                        // treats this case as an infinite loop (bug #3311911).
                        if (_pSnd.loop != 0 || _pSnd.hold > 0)
                        {
                            JumpToTick((uint)_loopTick);

                            // Done with this event.
                            return true;

                        }
                        else
                        {
                            _pSnd.status = SoundStatus.Stopped;
                            _pSnd.SetSignal(Register.SIGNAL_OFFSET);

                            // TODO: debugC(4, kDebugLevelSound, "signal EOT");
                        }
                    }

                    // Break to let parent handle the rest.
                    break;
            }


            // Let parent handle the rest
            return base.ProcessEvent(info, fireEvents);
        }

        protected override void SendToDriver(int midi)
        {
            // State tracking
            TrackState(midi);

            if ((midi & 0xFFF0) == 0x4EB0 && _soundVersion >= SciVersion.V1_EARLY)
            {
                // Mute. Handled in trackState().
                // CHECKME: Should we send this on to the driver?
                return;
            }

            if ((midi & 0xFFF0) == 0x07B0)
            {
                // someone trying to set channel volume?
                int channelVolume = (midi >> 16) & 0xFF;
                // Adjust volume accordingly to current local volume
                channelVolume = channelVolume * _volume / 127;
                midi = (midi & 0xFFFF) | ((channelVolume & 0xFF) << 16);
            }


            // Channel remapping
            byte midiChannel = (byte)(midi & 0xf);
            short realChannel = _channelRemap[midiChannel];
            if (realChannel == -1)
                return;

            midi = (int)((midi & 0xFFFFFFF0) | (ushort)realChannel);
            SendToDriver_raw(midi);
        }

        protected override void AllNotesOff()
        {
            if (MidiDriver == null)
                return;

            int i, j;

            // Turn off all active notes
            for (i = 0; i < 128; ++i)
            {
                for (j = 0; j < 16; ++j)
                {
                    if ((ActiveNotes[i] & (1 << j)) != 0 && (_channelRemap[j] != -1))
                    {
                        SendToDriver(0x80 | j, i, 0);
                    }
                }
            }

            // Turn off all hanging notes
            for (i = 0; i < HangingNotes.Length; i++)
            {
                byte midiChannel = (byte)HangingNotes[i].Channel;
                if ((HangingNotes[i].TimeLeft != 0) && (_channelRemap[midiChannel] != -1))
                {
                    SendToDriver(0x80 | midiChannel, HangingNotes[i].Note, 0);
                    HangingNotes[i].TimeLeft = 0;
                }
            }
            HangingNotesCount = 0;

            // To be sure, send an "All Note Off" event (but not all MIDI devices
            // support this...).

            for (i = 0; i < 16; ++i)
            {
                if (_channelRemap[i] != -1)
                {
                    SendToDriver(0xB0 | i, 0x7b, 0); // All notes off
                    SendToDriver(0xB0 | i, 0x40, 0); // Also send a sustain off event (bug #3116608)
                }
            }

            Array.Clear(ActiveNotes, 0, ActiveNotes.Length);
        }


        private byte MidiGetNextChannel(long ticker)
        {
            byte curr = 0xFF;
            long closest = ticker + 1000000, next = 0;

            for (int i = 0; i < _track.channelCount; i++)
            {
                if (_track.channels[i].time == -1) // channel ended
                    continue;
                var curChannel = _track.channels[i];
                if (curChannel.curPos >= curChannel.size)
                    continue;
                next = curChannel.data[curChannel.curPos]; // when the next event should occur
                if (next == 0xF8) // 0xF8 means 240 ticks delay
                    next = 240;
                next += _track.channels[i].time;
                if (next < closest)
                {
                    curr = (byte)i;
                    closest = next;
                }
            }

            return curr;
        }

        private BytePtr MidiMixChannels()
        {
            int totalSize = 0;
            int i = 0;

            for (i = 0; i < _track.channelCount; i++)
            {
                _track.channels[i].time = 0;
                _track.channels[i].prev = 0;
                _track.channels[i].curPos = 0;
                totalSize += _track.channels[i].size;
            }

            var outData = new byte[totalSize * 2]; // FIXME: creates overhead and still may be not enough to hold all data
            _mixedData = outData;

            i = 0;
            long ticker = 0;
            byte channelNr, curDelta;
            byte midiCommand = 0, midiParam, globalPrev = 0;
            long newDelta;
            SoundResource.Channel channel;

            while ((channelNr = MidiGetNextChannel(ticker)) != 0xFF)
            { // there is still an active channel
                channel = _track.channels[channelNr];
                curDelta = channel.data[channel.curPos++];
                channel.time += (curDelta == 0xF8 ? 240 : curDelta); // when the command is supposed to occur
                if (curDelta == 0xF8)
                    continue;
                newDelta = channel.time - ticker;
                ticker += newDelta;

                midiCommand = channel.data[channel.curPos++];
                if (midiCommand != (byte)SciMidiCommands.EndOfTrack)
                {
                    // Write delta
                    while (newDelta > 240)
                    {
                        outData[i++] = 0xF8;
                        newDelta -= 240;
                    }
                    outData[i++] = (byte)newDelta;
                }
                // Write command
                switch (midiCommand)
                {
                    case 0xF0: // sysEx
                        outData[i++] = midiCommand;
                        do
                        {
                            midiParam = channel.data[channel.curPos++];
                            outData[i++] = midiParam;
                        } while (midiParam != 0xF7);
                        break;
                    case (byte)SciMidiCommands.EndOfTrack: // end of channel
                        channel.time = -1;
                        break;
                    default: // MIDI command
                        if ((midiCommand & 0x80) != 0)
                        {
                            midiParam = channel.data[channel.curPos++];
                        }
                        else
                        {// running status
                            midiParam = midiCommand;
                            midiCommand = channel.prev;
                        }

                        // remember which channel got used for channel remapping
                        byte midiChannel = (byte)(midiCommand & 0xF);
                        _channelUsed[midiChannel] = true;

                        if (midiCommand != globalPrev)
                            outData[i++] = midiCommand;
                        outData[i++] = midiParam;
                        if (nMidiParams[(midiCommand >> 4) - 8] == 2)
                            outData[i++] = channel.data[channel.curPos++];
                        channel.prev = midiCommand;
                        globalPrev = midiCommand;
                        break;
                }
            }

            // Insert stop event
            outData[i++] = 0;    // Delta
            outData[i++] = 0xFF; // Meta event
            outData[i++] = 0x2F; // End of track (EOT)
            outData[i++] = 0x00;
            outData[i++] = 0x00;
            return _mixedData;
        }

        /// <summary>
        /// This is used for SCI0 sound-data. SCI0 only has one stream that may
        /// contain several channels and according to output device we remove
        /// certain channels from that data.
        /// </summary>
        /// <param name="channelMask"></param>
        private byte[] MidiFilterChannels(int channelMask)
        {
            SoundResource.Channel channel = _track.channels[0];
            BytePtr channelData = new BytePtr(channel.data);
            BytePtr outData = new BytePtr(new byte[channel.size + 5]);
            byte curChannel = 15, curByte, curDelta;
            byte command = 0, lastCommand = 0;
            int delta = 0;
            int midiParamCount = 0;
            bool containsMidiData = false;

            _mixedData = outData.Data;

            while (channelData.Offset < channel.size)
            {
                curDelta = channelData.Value; channelData.Offset++;
                if (curDelta == 0xF8)
                {
                    delta += 240;
                    continue;
                }
                delta += curDelta;
                curByte = channelData.Value; channelData.Offset++;

                switch (curByte)
                {
                    case 0xF0: // sysEx
                    case (byte)SciMidiCommands.EndOfTrack: // end of channel
                        command = curByte;
                        curChannel = 15;
                        break;
                    default:
                        if ((curByte & 0x80) != 0)
                        {
                            command = curByte;
                            curChannel = (byte)(command & 0x0F);
                            midiParamCount = nMidiParams[(command >> 4) - 8];
                        }
                        break;
                }
                if (((1 << curChannel) & channelMask) != 0)
                {
                    if (curChannel != 0xF)
                        containsMidiData = true;

                    // Write delta
                    while (delta > 240)
                    {
                        outData.Value = 0xF8;
                        outData.Offset++;
                        delta -= 240;
                    }
                    outData.Value = (byte)delta;
                    outData.Offset++;
                    delta = 0;

                    // Write command
                    switch (command)
                    {
                        case 0xF0: // sysEx
                            outData.Value = command;
                            outData.Offset++;
                            do
                            {
                                curByte = channelData.Value; channelData.Offset++;
                                outData.Value = curByte; // out
                                outData.Offset++;
                            } while (curByte != 0xF7);
                            lastCommand = command;
                            break;

                        case (byte)SciMidiCommands.EndOfTrack: // end of channel
                            break;

                        default: // MIDI command
                                 // remember which channel got used for channel remapping
                            byte midiChannel = (byte)(command & 0xF);
                            _channelUsed[midiChannel] = true;

                            if (lastCommand != command)
                            {
                                outData.Value = command;
                                outData.Offset++;
                                lastCommand = command;
                            }
                            if (midiParamCount > 0)
                            {
                                if ((curByte & 0x80) != 0)
                                {
                                    outData.Value = channelData.Value; channelData.Offset++;
                                    outData.Offset++;
                                }
                                else
                                {
                                    outData.Value = curByte;
                                    outData.Offset++;
                                }
                            }
                            if (midiParamCount > 1)
                            {
                                outData.Value = channelData.Value; channelData.Offset++;
                                outData.Offset++;
                            }
                            break;
                    }
                }
                else
                {
                    if ((curByte & 0x80) != 0)
                        channelData.Offset += midiParamCount;
                    else
                        channelData.Offset += midiParamCount - 1;
                }
            }

            // Insert stop event
            // (Delta is already output above)
            outData.Value = 0xFF; // Meta event
            outData.Offset++;
            outData.Value = 0x2F; // End of track (EOT)
            outData.Offset++;
            outData.Value = 0x00;
            outData.Offset++;
            outData.Value = 0x00;
            outData.Offset++;

            // This occurs in the music tracks of LB1 Amiga, when using the MT-32
            // driver (bug #3297881)
            if (!containsMidiData)
                Warning("MIDI parser: the requested SCI0 sound has no MIDI note data for the currently selected sound driver");

            return _mixedData;
        }

        private void ResetStateTracking()
        {
            for (int i = 0; i < 16; ++i)
            {
                ChannelState s = _channelState[i];
                s._modWheel = 0;
                s._pan = 64;
                s._patch = 0; // TODO: Initialize properly (from data in LoadMusic?)
                s._note = -1;
                s._sustain = false;
                s._pitchWheel = 0x2000;
                s._voices = 0;

                _channelVolume[i] = 127;
            }
        }

        private void TrackState(int b)
        {
            // We keep track of most of the state of a midi channel, so we can
            // at any time reset the device to the current state, even if the
            // channel has been temporarily disabled due to remapping.

            byte command = (byte)(b & 0xf0);
            byte channel = (byte)(b & 0xf);
            byte op1 = (byte)((b >> 8) & 0x7f);
            byte op2 = (byte)((b >> 16) & 0x7f);

            ChannelState s = _channelState[channel];

            switch (command)
            {
                case 0x90:
                case 0x80:
                    if (command == 0x90 && op2 != 0)
                    {
                        // note on
                        s._note = (sbyte)op1;
                        break;
                    }
                    // note off
                    if (s._note == op1)
                        s._note = -1;
                    break;
                case 0xB0:
                    // control change
                    switch (op1)
                    {
                        case 0x01: // mod wheel
                            s._modWheel = (sbyte)op2;
                            break;
                        case 0x07: // channel volume
                            _channelVolume[channel] = op2;
                            break;
                        case 0x0A: // pan
                            s._pan = (sbyte)op2;
                            break;
                        case 0x40: // sustain
                            s._sustain = (op2 != 0);
                            break;
                        case 0x4B: // voices
                            if (s._voices != op2)
                            {
                                // CHECKME: Should we directly call remapChannels() if _mainThreadCalled?
                                // TODO: debugC(2, kDebugLevelSound, "Dynamic voice change (%d to %d)", s._voices, op2);
                                _music.NeedsRemap();
                            }
                            s._voices = (sbyte)op2;
                            _pSnd._chan[channel]._voices = (sbyte)op2; // Also sync our MusicEntry
                            break;
                        case 0x4E: // mute
                                   // This is channel mute only for sci1.
                                   // (It's velocity control for sci0, but we don't need state in sci0)
                            if (_soundVersion > SciVersion.V1_EARLY)
                            {
                                // FIXME: mute is a level, not a bool, in some SCI versions
                                bool m = op2 != 0;
                                if (_pSnd._chan[channel]._mute != m)
                                {
                                    _pSnd._chan[channel]._mute = m;
                                    // CHECKME: Should we directly call remapChannels() if _mainThreadCalled?
                                    _music.NeedsRemap();
                                    // TODO: debugC(2, kDebugLevelSound, "Dynamic mute change (arg = %d, mainThread = %d)", m, _mainThreadCalled);
                                }
                            }
                            break;
                    }
                    break;
                case 0xC0:
                    // program change
                    s._patch = (sbyte)op1;
                    break;
                case 0xE0:
                    // pitchwheel
                    s._pitchWheel = (short)((op2 << 7) | op1);
                    break;
            }
        }

        private void SendToDriver_raw(int midi)
        {
            if (_mainThreadCalled)
                _music.PutMidiCommandInQueue(midi);
            else
                MidiDriver.Send(midi);
        }

        public override void LoadMusic(byte[] data, int offset, int length)
        {
        }
    }
}
