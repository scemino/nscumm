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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NScumm.Sci.Sound.Drivers;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Common;

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
        private SciMusic sciMusic;

        // this is set, when main thread calls us . we send commands to queue instead to driver
        private bool _mainThreadCalled;

        private SciVersion _soundVersion;
        private byte[] _mixedData;
        private SoundResource.Track _track;
        private MusicEntry _pSnd;
        private uint _loopTick;
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

        private ChannelState[] _channelState = new ChannelState[16];
        private int _numTracks;

        private static readonly int[] nMidiParams = { 2, 2, 2, 2, 1, 1, 2, 0 };

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

        public MidiParser_SCI(SciVersion _soundVersion, SciMusic sciMusic)
        {
            this._soundVersion = _soundVersion;
            this.sciMusic = sciMusic;
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
                    throw new InvalidOperationException("MidiParser_SCI::setVolume: Unsupported soundVersion");
            }
        }

        public override void LoadMusic(byte[] data)
        {
            throw new NotImplementedException();
        }

        protected override void ParseNextEvent(EventInfo info)
        {
            throw new NotImplementedException();
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

        internal void SetMasterVolume(object _masterVolume)
        {
            throw new NotImplementedException();
        }

        public void MainThreadBegin()
        {
            //assert(!_mainThreadCalled);
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
            else {
                MidiMixChannels();
            }

            _numTracks = 1;
            Tracks[0] = new Track { Position = 0 };
            if (_pSnd != null)
            {
                ActiveTrack = 0;
            }
            _loopTick = 0;

            return true;
        }

        private void MidiMixChannels()
        {
            throw new NotImplementedException();
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
            ByteAccess channelData = new ByteAccess(channel.data);
            ByteAccess channelDataEnd = new ByteAccess(channel.data, channel.size);
            ByteAccess outData = new ByteAccess(new byte[channel.size + 5]);
            byte curChannel = 15, curByte, curDelta;
            byte command = 0, lastCommand = 0;
            int delta = 0;
            int midiParamCount = 0;
            bool containsMidiData = false;

            _mixedData = outData.Data;

            while (channelData.Offset < channelDataEnd.Offset)
            {
                curDelta = channelData.Increment();
                if (curDelta == 0xF8)
                {
                    delta += 240;
                    continue;
                }
                delta += curDelta;
                curByte = channelData.Increment();

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
                                curByte = channelData.Increment();
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
                                    outData.Value = channelData.Increment();
                                    outData.Offset++;
                                }
                                else {
                                    outData.Value = curByte;
                                    outData.Offset++;
                                }
                            }
                            if (midiParamCount > 1)
                            {
                                outData.Value = channelData.Increment();
                                outData.Offset++;
                            }
                            break;
                    }
                }
                else {
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
            //if (!containsMidiData)
            //    warning("MIDI parser: the requested SCI0 sound has no MIDI note data for the currently selected sound driver");

            return _mixedData;
        }

        public void MainThreadEnd()
        {
            //assert(_mainThreadCalled);
            _mainThreadCalled = false;
        }
    }
}
