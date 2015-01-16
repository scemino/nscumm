//
//  Player_AD.cs
//
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
using NScumm.Core.Audio;
using NScumm.Core.Audio.OPL;
using System.Diagnostics;
using NScumm.Core.IO;
using NScumm.Core.Audio.OPL.DosBox;

namespace NScumm.Core
{
    public class Player_AD: IAudioStream, IMusicEngine
    {
        public Player_AD(ScummEngine scumm, IMixer mixer)
        {
            _vm = scumm;
            _mixer = mixer;
            _rate = mixer.OutputRate;
            // TODO: vs OPL
            //        _opl2 = OPL::Config::create();
            _opl2 = new DosBoxOPL(OplType.Opl2);
            _opl2.Init(_rate);

            _samplesPerCallback = _rate / AD_CALLBACK_FREQUENCY;
            _samplesPerCallbackRemainder = _rate % AD_CALLBACK_FREQUENCY;
            _samplesTillCallback = 0;
            _samplesTillCallbackRemainder = 0;

            WriteReg(0x01, 0x00);
            WriteReg(0xBD, 0x00);
            WriteReg(0x08, 0x00);
            WriteReg(0x01, 0x20);

            _soundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);

            _engineMusicTimer = 0;
            _soundPlaying = -1;

            _curOffset = 0;

            _sfxTimer = 4;
            _rndSeed = 1;

            for (int i = 0; i < _sfx.Length; ++i)
            {
                _sfx[i] = new SfxSlot();
                _sfx[i].Resource = -1;
                for (int j = 0; j < _sfx[i].Channels.Length; ++j)
                {
                    _sfx[i].Channels[j].HardwareChannel = -1;
                }
            }

            _numHWChannels = _hwChannels.Length;

            _musicVolume = _sfxVolume = 255;
            _isSeeking = false;
        }

        public void Dispose()
        {
            _mixer.StopHandle(_soundHandle);

            StopAllSounds();
            lock (_mutex)
            {
                _opl2 = null;
            }
        }

        public void StartSound(int sound)
        {
            lock (_mutex)
            {

                // Setup the sound volume
                SetupVolume();

                // Query the sound resource
                var res = _vm.ResourceManager.GetSound(sound);

                if (res[2] == 0x80)
                {
                    // Stop the current sounds
                    StopMusic();

                    // TODO: Lock the new music resource
                    _soundPlaying = sound;
//				_vm._res.lock(rtSound, _soundPlaying);

                    // Start the new music resource
                    _musicData = res;
                    StartMusic();
                }
                else
                {
                    var priority = res[0];
                    // The original specified the channel to use in the sound
                    // resource. However, since we play as much as possible we sill
                    // ignore it and simply use the priority value to determine
                    // whether the sfx can be played or not.
                    //const byte channel  = res[1];

                    // Try to allocate a sfx slot for playback.
                    var sfx = AllocateSfxSlot(priority);
				
                    // Try to start sfx playback
                    sfx.Resource = sound;
                    sfx.Priority = priority;
                    if (StartSfx(sfx, res))
                    {
                        // TODO: Lock the new resource
//					_vm._res.lock(rtSound, sound);
                    }
                    else
                    {
                        // When starting the sfx failed we need to reset the slot.
                        sfx.Resource = -1;

                        for (int i = 0; i < sfx.Channels.Length; ++i)
                        {
                            sfx.Channels[i].State = ChannelState.Off;

                            if (sfx.Channels[i].HardwareChannel != -1)
                            {
                                FreeHWChannel(sfx.Channels[i].HardwareChannel);
                                sfx.Channels[i].HardwareChannel = -1;
                            }
                        }
                    }
                }
            }
        }

        public int ReadBuffer(short[] buffer)
        {
            lock (_mutex)
            {

                int len = buffer.Length;
                int pos = 0;

                while (len > 0)
                {
                    if (_samplesTillCallback == 0)
                    {
                        if (_curOffset != 0)
                        {
                            UpdateMusic();
                        }

                        UpdateSfx();

                        _samplesTillCallback = _samplesPerCallback;
                        _samplesTillCallbackRemainder += _samplesPerCallbackRemainder;
                        if (_samplesTillCallbackRemainder >= AD_CALLBACK_FREQUENCY)
                        {
                            ++_samplesTillCallback;
                            _samplesTillCallbackRemainder -= AD_CALLBACK_FREQUENCY;
                        }
                    }

                    int samplesToRead = Math.Min(len, _samplesTillCallback);
                    _opl2.ReadBuffer(buffer, pos, samplesToRead);

                    pos += samplesToRead;
                    len -= samplesToRead;
                    _samplesTillCallback -= samplesToRead;
                }

                return buffer.Length;
            }
        }

        public bool IsStereo { get { return false; } }

        public bool IsEndOfData { get { return false; } }

        public bool IsEndOfStream { get { return IsEndOfData; } }

        public int Rate { get { return _rate; } }

        public void StopSound(int sound)
        {
            lock (_mutex)
            {

                if (sound == _soundPlaying)
                {
                    StopMusic();
                }
                else
                {
                    for (int i = 0; i < _sfx.Length; ++i)
                    {
                        if (_sfx[i].Resource == sound)
                        {
                            StopSfx(_sfx[i]);
                        }
                    }
                }
            }
        }

        public void StopAllSounds()
        {
            lock (_mutex)
            {

                // Stop the music
                StopMusic();

                // Stop all the sfx playback
                for (int i = 0; i < _sfx.Length; ++i)
                {
                    StopSfx(_sfx[i]);
                }
            }
        }

        public void SetMusicVolume(int vol)
        {
            // HACK: We ignore the parameter and set up the volume specified in the
            // config manager. This allows us to differentiate between music and sfx
            // volume changes.
            SetupVolume();
        }

        public int GetSoundStatus(int sound)
        {
            return (sound == _soundPlaying) ? 1 : 0;
        }

        public int GetMusicTimer()
        {
            return _engineMusicTimer;
        }

        public void SaveOrLoad(Serializer ser)
        {
            lock (_mutex)
            {

//            if (ser.Version < 95) {
//                IMuse *dummyImuse = IMuse::create(_vm._system, null, null);
//                dummyImuse.save_or_load(ser, _vm, false);
//                delete dummyImuse;
//                return;
//            }

                if (ser.Version >= 96)
                {

                    int[] res = null;
                    // The first thing we save is a list of sound resources being played
                    // at the moment.
                    LoadAndSaveEntry.Create(r => res = r.ReadInt32s(4), w => w.WriteInt32s(new int[]{ _soundPlaying, _sfx[0].Resource, _sfx[1].Resource, _sfx[2].Resource }, 4), 96).Execute(ser);

                    // If we are loading start the music again at this point.
                    if (ser.IsLoading)
                    {
                        if (res[0] != -1)
                        {
                            StartSound(res[0]);
                        }
                    }

                    uint musicOffset = _curOffset;

                    var musicData = new []
                    {
                        LoadAndSaveEntry.Create(r => _engineMusicTimer = r.ReadInt32(), w => w.WriteInt32(_engineMusicTimer), 96),
                        LoadAndSaveEntry.Create(r => _musicTimer = r.ReadInt32(), w => w.WriteInt32(_musicTimer), 96),
                        LoadAndSaveEntry.Create(r => _internalMusicTimer = r.ReadUInt32(), w => w.WriteUInt32(_internalMusicTimer), 96),
                        LoadAndSaveEntry.Create(r => _curOffset = r.ReadUInt32(), w => w.WriteUInt32(_curOffset), 96),
                        LoadAndSaveEntry.Create(r => _nextEventTimer = r.ReadUInt32(), w => w.WriteUInt32(_nextEventTimer), 96)
                    };

                    Array.ForEach(musicData, e => e.Execute(ser));

                    // We seek back to the old music position.
                    if (ser.IsLoading)
                    {
                        ScummHelper.Swap(ref musicOffset, ref _curOffset);
                        MusicSeekTo(musicOffset);
                    }

                    // Finally start up the SFX. This makes sure that they are not
                    // accidently stopped while seeking to the old music position.
                    if (ser.IsLoading)
                    {
                        for (int i = 1; i < res.Length; ++i)
                        {
                            if (res[i] != -1)
                            {
                                StartSound(res[i]);
                            }
                        }
                    }
                }
            }
        }

        void MusicSeekTo(uint position)
        {
            // This method is actually dangerous to use and should only be used for
            // loading save games because it does not set up anything like the engine
            // music timer or similar.
            _isSeeking = true;

            // Seek until the given position.
            while (_curOffset != position)
            {
                if (ParseCommand())
                {
                    // We encountered an EOT command. This should not happen unless
                    // we try to seek to an illegal position. In this case just abort
                    // seeking.
                    Debug.WriteLine("AD illegal seek to {0}", position);
                    break;
                }
                ParseVLQ();
            }

            _isSeeking = false;

            // Turn on all notes.
            for (int i = 0; i < _voiceChannels.Length; ++i)
            {
                if (_voiceChannels[i].lastEvent != 0)
                {
                    int reg = 0xB0 + i;
                    WriteReg(reg, ReadReg(reg));
                }
            }
        }

        bool StartSfx(SfxSlot sfx, byte[] resource)
        {
            WriteReg(0xBD, 0x00);

            // Clear the channels.
            sfx.Channels[0].State = ChannelState.Off;
            sfx.Channels[1].State = ChannelState.Off;
            sfx.Channels[2].State = ChannelState.Off;

            // Set up the first channel to pick up playback.
            // Try to allocate a hardware channel.
            sfx.Channels[0].HardwareChannel = AllocateHWChannel(sfx.Priority, sfx);
            if (sfx.Channels[0].HardwareChannel == -1)
            {
                Debug.WriteLine("AD No hardware channel available");
                return false;
            }
            sfx.Channels[0].MusicData = resource;
            sfx.Channels[0].CurrentOffset = sfx.Channels[0].StartOffset = 2;
            sfx.Channels[0].State = ChannelState.Parse;

            // Scan for the start of the other channels and set them up if required.
            int curChannel = 1;
            var bufferPosition = 2;
            byte command = 0;
            while ((command = resource[bufferPosition]) != 0xFF)
            {
                switch (command)
                {
                    case 1:
					// INSTRUMENT DEFINITION
                        bufferPosition += 15;
                        break;

                    case 2:
					// NOTE DEFINITION
                        bufferPosition += 11;
                        break;

                    case 0x80:
					// LOOP
                        bufferPosition += 1;
                        break;

                    default:
					// START OF CHANNEL
                        bufferPosition += 1;
                        if (curChannel >= 3)
                        {
                            throw new InvalidOperationException(string.Format("AD SFX resource {0} uses more than 3 channels", sfx.Resource));
                        }
                        sfx.Channels[curChannel].HardwareChannel = AllocateHWChannel(sfx.Priority, sfx);
                        if (sfx.Channels[curChannel].HardwareChannel == -1)
                        {
                            Debug.WriteLine("AD No hardware channel available");
                            return false;
                        }
                        sfx.Channels[curChannel].CurrentOffset = bufferPosition;
                        sfx.Channels[curChannel].StartOffset = bufferPosition;
                        sfx.Channels[curChannel].State = ChannelState.Parse;
                        ++curChannel;
                        break;
                }
            }

            return true;
        }

        SfxSlot AllocateSfxSlot(int priority)
        {
            // We always reaLlocate the slot with the lowest priority in case none is
            // free.
            SfxSlot sfx = null;
            int minPrio = priority;

            for (int i = 0; i < _sfx.Length; ++i)
            {
                if (_sfx[i].Resource == -1)
                {
                    return _sfx[i];
                }
                else if (_sfx[i].Priority <= minPrio)
                {
                    minPrio = _sfx[i].Priority;
                    sfx = _sfx[i];
                }
            }

            // In case we reallocate a slot stop the old one.
            if (sfx != null)
            {
                StopSfx(sfx);
            }

            return sfx;
        }

        void StartMusic()
        {
            Array.Clear(_instrumentOffset, 0, _instrumentOffset.Length);

            bool hasRhythmData = false;
            uint instruments = _musicData[10];
            for (int i = 0; i < instruments; ++i)
            {
                int instrIndex = _musicData[11 + i] - 1;
                if (0 <= instrIndex && instrIndex < 16)
                {
                    _instrumentOffset[instrIndex] = i * 16 + 16 + 3;
                    hasRhythmData |= (_musicData[_instrumentOffset[instrIndex] + 13] != 0);
                }
            }

            if (hasRhythmData)
            {
                _mdvdrState = 0x20;
                LimitHWChannels(6);
            }
            else
            {
                _mdvdrState = 0;
                LimitHWChannels(9);
            }

            _curOffset = 0x93;
            // TODO: is this the same for Loom?
            _nextEventTimer = 40;
            _engineMusicTimer = 0;
            _internalMusicTimer = 0;
            _musicTimer = 0;

            WriteReg(0xBD, _mdvdrState);

            var isLoom = (_vm.Game.GameId == GameId.Loom);
            _timerLimit = isLoom ? 473 : 256;
            _musicTicks = _musicData[3] * (isLoom ? 2 : 1);
            _loopFlag = (_musicData[4] == 0);
            _musicLoopStart = _curOffset + BitConverter.ToUInt16(_musicData, 5);
        }

        void SetupVolume()
        {
            // Setup the correct volume
//			_musicVolume = CLIP<int>(ConfMan.getInt("music_volume"), 0, Audio::Mixer::kMaxChannelVolume);
//			_sfxVolume = CLIP<int>(ConfMan.getInt("sfx_volume"), 0, Audio::Mixer::kMaxChannelVolume);
//			_musicVolume = Mixer.MaxChannelVolume;
//			_sfxVolume = Mixer.MaxChannelVolume;
            _musicVolume = 192;
            _sfxVolume = 192;

//			if (ConfMan.hasKey("mute")) {
//				if (ConfMan.getBool("mute")) {
//					_musicVolume = 0;
//					_sfxVolume = 0;
//				}
//			}

            // Update current output levels
            for (int i = 0; i < _operatorOffsetTable.Length; ++i)
            {
                var reg = 0x40 + _operatorOffsetTable[i];
                WriteReg(reg, ReadReg(reg));
            }

            // Reset note on status
            for (int i = 0; i < _hwChannels.Length; ++i)
            {
                var reg = 0xB0 + i;
                WriteReg(reg, ReadReg(reg));
            }
        }

        void WriteReg(int r, int v)
        {
            if (r >= 0 && r < _registerBackUpTable.Length)
            {
                _registerBackUpTable[r] = (byte)v;
            }

            // Handle volume scaling depending on the sound type.
            if (r >= 0x40 && r <= 0x55)
            {
                int operatorOffset = r - 0x40;
                int channel = _operatorOffsetToChannel[operatorOffset];
                if (channel != -1)
                {
                    bool twoOPOutput = (ReadReg(0xC0 + channel) & 0x01) != 0;

                    int scale = Mixer.MaxChannelVolume;
                    // We only scale the volume of operator 2 unless both operators
                    // are set to directly produce sound.
                    if (twoOPOutput || operatorOffset == _operatorOffsetTable[channel * 2 + 1])
                    {
                        if (_hwChannels[channel].SfxOwner != null)
                        {
                            scale = _sfxVolume;
                        }
                        else
                        {
                            scale = _musicVolume;
                        }
                    }

                    int vol = 0x3F - (v & 0x3F);
                    vol = vol * scale / Mixer.MaxChannelVolume;
                    v &= 0xC0;
                    v |= (0x3F - vol);
                }
            }

            // Since AdLib's lowest volume level does not imply that the sound is
            // completely silent we ignore key on in such a case.
            // We also ignore key on for music whenever we do seeking.
            if (r >= 0xB0 && r <= 0xB8)
            {
                int channel = r - 0xB0;
                bool mute = false;
                if (_hwChannels[channel].SfxOwner != null)
                {
                    if (_sfxVolume == 0)
                    {
                        mute = true;
                    }
                }
                else
                {
                    if (_musicVolume == 0)
                    {
                        mute = true;
                    }
                    else
                    {
                        mute = _isSeeking;
                    }
                }

                if (mute)
                {
                    v &= ~0x20;
                }
            }

            _opl2.WriteReg(r, v);
        }

        byte ReadReg(int r)
        {
            if (r >= 0 && r < _registerBackUpTable.Length)
            {
                return _registerBackUpTable[r];
            }
            else
            {
                return 0;
            }
        }

        void UpdateMusic()
        {
            _musicTimer += _musicTicks;
            if (_musicTimer < _timerLimit)
            {
                return;
            }
            _musicTimer -= _timerLimit;

            ++_internalMusicTimer;
            if (_internalMusicTimer > 120)
            {
                _internalMusicTimer = 0;
                ++_engineMusicTimer;
            }

            --_nextEventTimer;
            if (_nextEventTimer != 0)
            {
                return;
            }

            while (true)
            {
                if (ParseCommand())
                {
                    // We received an EOT command. In case there's no music playing
                    // we know there was no looping enabled. Thus, we stop further
                    // handling. Otherwise we will just continue parsing. It is
                    // important to note that we need to parse a command directly
                    // at the new position, i.e. there is no time value we need to
                    // parse.
                    if (_soundPlaying == -1)
                    {
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }

                // In case there is a delay till the next event stop handling.
                if (_musicData[_curOffset] != 0)
                {
                    break;
                }
                ++_curOffset;
            }

            _nextEventTimer = ParseVLQ();
            _nextEventTimer >>= (_vm.Game.GameId == NScumm.Core.IO.GameId.Loom) ? 2 : 1;
            if (_nextEventTimer == 0)
            {
                _nextEventTimer = 1;
            }
        }

        uint ParseVLQ()
        {
            uint vlq = _musicData[_curOffset++];
            if ((vlq & 0x80) != 0)
            {
                vlq -= 0x80;
                vlq <<= 7;
                vlq |= _musicData[_curOffset++];
            }
            return vlq;
        }

        bool ParseCommand()
        {
            uint command = _musicData[_curOffset++];
            if (command == 0xFF)
            {
                // META EVENT
                // Get the command number.
                command = _musicData[_curOffset++];
                if (command == 47)
                {
                    // End of track
                    if (_loopFlag)
                    {
                        // In case the track is looping jump to the start.
                        _curOffset = _musicLoopStart;
                        _nextEventTimer = 0;
                    }
                    else
                    {
                        // Otherwise completely stop playback.
                        StopMusic();
                    }
                    return true;
                }
                else if (command == 88)
                {
                    // This is proposedly a debug information insertion. The CMS
                    // player code handles this differently, but is still using
                    // the same resources...
                    _curOffset += 5;
                }
                else if (command == 81)
                {
                    // Change tempo. This is used exclusively in Loom.
                    int timing = _musicData[_curOffset + 2] | (_musicData[_curOffset + 1] << 8);
                    _musicTicks = 0x73000 / timing;
                    command = _musicData[_curOffset++];
                    _curOffset += command;
                }
                else
                {
                    // In case an unknown meta event occurs just skip over the
                    // data by using the length supplied.
                    command = _musicData[_curOffset++];
                    _curOffset += command;
                }
            }
            else
            {
                if (command >= 0x90)
                {
                    // NOTE ON
                    // Extract the channel number and save it in command.
                    command -= 0x90;

                    int instrOffset = _instrumentOffset[command];
                    if (instrOffset != 0)
                    {
                        if (_musicData[instrOffset + 13] != 0)
                        {
                            SetupRhythm(_musicData[instrOffset + 13], instrOffset);
                        }
                        else
                        {
                            // Priority 256 makes sure we always prefer music
                            // channels over SFX channels.
                            int channel = AllocateHWChannel(256);
                            if (channel != -1)
                            {
                                SetupChannel(channel, _musicData, instrOffset);
                                _voiceChannels[channel].lastEvent = command + 0x90;
                                _voiceChannels[channel].frequency = _musicData[_curOffset];
                                SetupFrequency(channel, (sbyte)_musicData[_curOffset]);
                            }
                        }
                    }
                }
                else
                {
                    // NOTE OFF
                    uint note = _musicData[_curOffset];
                    command += 0x10;

                    // Find the output channel which plays the note.
                    int channel = 0xFF;
                    for (int i = 0; i < _voiceChannels.Length; ++i)
                    {
                        if (_voiceChannels[i].frequency == note && _voiceChannels[i].lastEvent == command)
                        {
                            channel = i;
                            break;
                        }
                    }

                    if (channel != 0xFF)
                    {
                        // In case a output channel playing the note was found,
                        // stop it.
                        NoteOff(channel);
                    }
                    else
                    {
                        // In case there is no such note this will disable the
                        // rhythm instrument played on the channel.
                        command -= 0x90;
                        int instrOffset = _instrumentOffset[command];
                        if (instrOffset != 0 && _musicData[instrOffset + 13] != 0)
                        {
                            int rhythmInstr = _musicData[instrOffset + 13];
                            if (rhythmInstr < 6)
                            {
                                _mdvdrState &= _mdvdrTable[rhythmInstr] ^ 0xFF;
                                WriteReg(0xBD, _mdvdrState);
                            }
                        }
                    }
                }

                _curOffset += 2;
            }

            return false;
        }

        void ParseSlot(Channel channel)
        {
            while (true)
            {
                var curOffset = channel.CurrentOffset;

                switch (channel.MusicData[curOffset])
                {
                    case 1:
					// INSTRUMENT DEFINITION
                        ++curOffset;
                        channel.InstrumentData[0] = channel.MusicData[curOffset + 0];
                        channel.InstrumentData[1] = channel.MusicData[curOffset + 2];
                        channel.InstrumentData[2] = channel.MusicData[curOffset + 9];
                        channel.InstrumentData[3] = channel.MusicData[curOffset + 8];
                        channel.InstrumentData[4] = channel.MusicData[curOffset + 4];
                        channel.InstrumentData[5] = channel.MusicData[curOffset + 3];
                        channel.InstrumentData[6] = 0;

                        SetupChannel(channel.HardwareChannel, channel.MusicData, curOffset);

                        WriteReg(0xA0 + channel.HardwareChannel, channel.MusicData[curOffset + 0]);
                        WriteReg(0xB0 + channel.HardwareChannel, channel.MusicData[curOffset + 1] & 0xDF);

                        channel.CurrentOffset += 15;
                        break;

                    case 2:
					// NOTE DEFINITION
                        ++curOffset;
                        channel.State = ChannelState.Play;
                        NoteOffOn(channel.HardwareChannel);
                        ParseNote(channel.Notes[0], channel, curOffset + 0);
                        ParseNote(channel.Notes[1], channel, curOffset + 5);
                        return;

                    case 0x80:
					// LOOP
                        channel.CurrentOffset = channel.StartOffset;
                        break;

                    default:
					// START OF CHANNEL
					// When we encounter a start of another channel while playback
					// it means that the current channel is finished. Thus, we will
					// stop it.
                        ClearChannel(channel);
                        channel.State = ChannelState.Off;
                        return;
                }
            }
        }

        void ParseNote(Note note, Channel channel, int offset)
        {
            if ((channel.MusicData[offset] & 0x80) != 0)
            {
                note.State = NoteState.PreInit;
                ProcessNote(note, channel, offset);
                note.PlayTime = 0;

                if ((channel.MusicData[offset] & 0x20) != 0)
                {
                    note.PlayTime = (channel.MusicData[offset + 4] >> 4) * 118;
                    note.PlayTime += (channel.MusicData[offset + 4] & 0x0F) * 8;
                }
            }
        }

        bool ProcessNote(Note note, Channel channel, int offset)
        {
            if (++note.State == NoteState.Off)
            {
                return true;
            }

            int instrumentDataOffset = channel.MusicData[offset] & 0x07;
            note.Bias = _noteBiasTable[instrumentDataOffset];

            byte instrumentDataValue = 0;
            if (note.State == NoteState.Attack)
            {
                instrumentDataValue = channel.InstrumentData[instrumentDataOffset];
            }

            byte noteInstrumentValue = ReadRegisterSpecial(channel.HardwareChannel, instrumentDataValue, instrumentDataOffset);
            if (note.Bias != 0)
            {
                noteInstrumentValue = (byte)(note.Bias - noteInstrumentValue);
            }
            note.InstrumentValue = noteInstrumentValue;

            if (note.State == NoteState.Sustain)
            {
                note.SustainTimer = _numStepsTable[channel.MusicData[offset + 3] >> 4];

                if ((channel.MusicData[offset] & 0x40) != 0)
                {
                    note.SustainTimer = (((GetRnd() << 8) * note.SustainTimer) >> 16) + 1;
                }
            }
            else
            {
                int timer1, timer2;
                if (note.State == NoteState.Release)
                {
                    timer1 = channel.MusicData[offset + 3] & 0x0F;
                    timer2 = 0;
                }
                else
                {
                    timer1 = channel.MusicData[offset + (int)note.State + 1] >> 4;
                    timer2 = channel.MusicData[offset + (int)note.State + 1] & 0x0F;
                }

                int adjustValue = ((_noteAdjustTable[timer2] * _noteAdjustScaleTable[instrumentDataOffset]) >> 16) - noteInstrumentValue;
                SetupNoteEnvelopeState(note, _numStepsTable[timer1], adjustValue);
            }

            return false;
        }

        byte GetRnd()
        {
            if ((_rndSeed & 1) != 0)
            {
                _rndSeed >>= 1;
                _rndSeed ^= 0xB8;
            }
            else
            {
                _rndSeed >>= 1;
            }

            return _rndSeed;
        }

        void SetupNoteEnvelopeState(Note note, int steps, int adjust)
        {
            note.PreIncrease = 0;
            if (Math.Abs(adjust) > steps)
            {
                note.PreIncrease = 1;
                note.Adjust = adjust / steps;
                note.Envelope.StepIncrease = Math.Abs(adjust % steps);
            }
            else
            {
                note.Adjust = adjust;
                note.Envelope.StepIncrease = Math.Abs(adjust);
            }

            note.Envelope.Step = steps;
            note.Envelope.StepCounter = 0;
            note.Envelope.Timer = steps;
        }

        byte ReadRegisterSpecial(int channel, byte defaultValue, int offset)
        {
            if (offset == 6)
            {
                return 0;
            }

            int regNum;
            if (_useOperatorTable[offset])
            {
                regNum = _operatorOffsetTable[_channelOperatorOffsetTable[offset] + channel * 2];
            }
            else
            {
                regNum = _channelOffsetTable[channel];
            }

            regNum += _baseRegisterTable[offset];

            byte regValue;
            if (defaultValue != 0)
            {
                regValue = defaultValue;
            }
            else
            {
                regValue = ReadReg(regNum);
            }

            regValue &= (byte)_registerMaskTable[offset];
            regValue >>= _registerShiftTable[offset];

            return regValue;
        }

        void NoteOffOn(int channel)
        {
            var regValue = ReadReg(0xB0 | channel);
            WriteReg(0xB0 | channel, regValue & 0xDF);
            WriteReg(0xB0 | channel, regValue | 0x20);
        }

        void UpdateSlot(Channel channel)
        {
            var curOffset = channel.CurrentOffset + 1;

            for (int num = 0; num <= 1; ++num, curOffset += 5)
            {
                if ((channel.MusicData[curOffset] & 0x80) == 0)
                {
                    continue;
                }

                var note = channel.Notes[num];
                bool updateNote = false;

                if (note.State == NoteState.Sustain)
                {
                    if ((--note.SustainTimer) == 0)
                    {
                        updateNote = true;
                    }
                }
                else
                {
                    updateNote = ProcessNoteEnvelope(note);

                    if (note.Bias != 0)
                    {
                        WriteRegisterSpecial(channel.HardwareChannel, (byte)(note.Bias - note.InstrumentValue), channel.MusicData[curOffset] & 0x07);
                    }
                    else
                    {
                        WriteRegisterSpecial(channel.HardwareChannel, (byte)note.InstrumentValue, channel.MusicData[curOffset] & 0x07);
                    }
                }

                if (updateNote)
                {
                    if (ProcessNote(note, channel, curOffset))
                    {
                        if (0 == (channel.MusicData[curOffset] & 0x08))
                        {
                            channel.CurrentOffset += 11;
                            channel.State = ChannelState.Parse;
                            continue;
                        }
                        else if ((channel.MusicData[curOffset] & 0x10) != 0)
                        {
                            NoteOffOn(channel.HardwareChannel);
                        }

                        note.State = NoteState.PreInit;
                        ProcessNote(note, channel, curOffset);
                    }
                }

                if (((channel.MusicData[curOffset] & 0x20) != 0) && (--note.PlayTime == 0))
                {
                    channel.CurrentOffset += 11;
                    channel.State = ChannelState.Parse;
                }
            }
        }

        void WriteRegisterSpecial(int channel, byte value, int offset)
        {
            if (offset == 6)
            {
                return;
            }

            int regNum;
            if (_useOperatorTable[offset])
            {
                regNum = _operatorOffsetTable[_channelOperatorOffsetTable[offset] + channel * 2];
            }
            else
            {
                regNum = _channelOffsetTable[channel];
            }

            regNum += _baseRegisterTable[offset];

            byte regValue = (byte)(ReadReg(regNum) & (~_registerMaskTable[offset]));
            regValue |= (byte)(value << _registerShiftTable[offset]);

            WriteReg(regNum, regValue);
        }

        bool ProcessNoteEnvelope(Note note)
        {
            if (note.PreIncrease != 0)
            {
                note.InstrumentValue += note.Adjust;
            }

            note.Envelope.StepCounter += note.Envelope.StepIncrease;
            if (note.Envelope.StepCounter >= note.Envelope.Step)
            {
                note.Envelope.StepCounter -= note.Envelope.Step;

                if (note.Adjust < 0)
                {
                    --note.InstrumentValue;
                }
                else
                {
                    ++note.InstrumentValue;
                }
            }

            if ((--note.Envelope.Timer) != 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        void StopMusic()
        {
            if (_soundPlaying == -1)
            {
                return;
            }

            // TODO: Unlock the music resource if present
//			_vm._res.unlock(rtSound, _soundPlaying);
            _soundPlaying = -1;

            // Stop the music playback
            _curOffset = 0;

            // Stop all music voice channels
            for (int i = 0; i < _voiceChannels.Length; ++i)
            {
                if (_voiceChannels[i].lastEvent != 0)
                {
                    NoteOff(i);
                }
            }

            // Reset rhythm state
            WriteReg(0xBD, 0x00);
            LimitHWChannels(9);
        }

        void NoteOff(int channel)
        {
            WriteReg(0xB0 + channel, _voiceChannels[channel].b0Reg & 0xDF);
            FreeVoiceChannel(channel);
        }

        void FreeHWChannel(int channel)
        {
            Debug.Assert(_hwChannels[channel].Allocated);
            _hwChannels[channel].Allocated = false;
            _hwChannels[channel].SfxOwner = null;
        }

        void FreeVoiceChannel(int channel)
        {
            var vChannel = _voiceChannels[channel];
            Debug.Assert(vChannel.lastEvent != 0);

            FreeHWChannel(channel);
            vChannel.lastEvent = 0;
            vChannel.b0Reg = 0;
            vChannel.frequency = 0;
        }

        void LimitHWChannels(int newCount)
        {
            for (int i = newCount; i < _hwChannels.Length; ++i)
            {
                if (_hwChannels[i].Allocated)
                {
                    FreeHWChannel(i);
                }
            }
            _numHWChannels = newCount;
        }

        int AllocateHWChannel(int priority, SfxSlot owner = null)
        {
            // We always reaLlocate the channel with the lowest priority in case none
            // is free.
            int channel = -1;
            int minPrio = priority;

            for (int i = 0; i < _numHWChannels; ++i)
            {
                if (!_hwChannels[i].Allocated)
                {
                    channel = i;
                    break;
                }

                // We don't allow SFX to reallocate their own channels. Otherwise we
                // would call stopSfx in the midst of startSfx and that can lead to
                // horrible states...
                // We also prevent the music from reallocating its own channels like
                // in the original.
                if (_hwChannels[i].Priority <= minPrio && _hwChannels[i].SfxOwner != owner)
                {
                    minPrio = _hwChannels[i].Priority;
                    channel = i;
                }
            }

            if (channel != -1)
            {
                // In case the HW channel belongs to a SFX we will completely
                // stop playback of that SFX.
                // TODO: Maybe be more fine grained in the future and allow
                // detachment of individual channels of a SFX?
                if (_hwChannels[channel].Allocated && _hwChannels[channel].SfxOwner != null)
                {
                    StopSfx(_hwChannels[channel].SfxOwner);
                }

                _hwChannels[channel].Allocated = true;
                _hwChannels[channel].Priority = priority;
                _hwChannels[channel].SfxOwner = owner;
            }

            return channel;
        }

        void SetupFrequency(int channel, sbyte frequency)
        {
            frequency -= 31;
            if (frequency < 0)
            {
                frequency = 0;
            }

            int octave = 0;
            while (frequency >= 12)
            {
                frequency -= 12;
                ++octave;
            }

            int noteFrequency = _noteFrequencies[frequency];
            octave <<= 2;
            octave |= noteFrequency >> 8;
            octave |= 0x20;
            WriteReg(0xA0 + channel, noteFrequency & 0xFF);
            _voiceChannels[channel].b0Reg = octave;
            WriteReg(0xB0 + channel, octave);
        }

        void SetupChannel(int channel, byte[] musicData, int instrOffset)
        {
            instrOffset += 2;
            WriteReg(0xC0 + channel, musicData[instrOffset++]);
            SetupOperator(_operatorOffsetTable[channel * 2 + 0], musicData, ref instrOffset);
            SetupOperator(_operatorOffsetTable[channel * 2 + 1], musicData, ref instrOffset);
        }

        void SetupOperator(int opr, byte[] musicData, ref int instrOffset)
        {
            WriteReg(0x20 + opr, musicData[instrOffset++]);
            WriteReg(0x40 + opr, musicData[instrOffset++]);
            WriteReg(0x60 + opr, musicData[instrOffset++]);
            WriteReg(0x80 + opr, musicData[instrOffset++]);
            WriteReg(0xE0 + opr, musicData[instrOffset++]);
        }

        void SetupRhythm(uint rhythmInstr, int instrOffset)
        {
            if (rhythmInstr == 1)
            {
                SetupChannel(6, _musicData, instrOffset);
                WriteReg(0xA6, _musicData[instrOffset++]);
                WriteReg(0xB6, _musicData[instrOffset] & 0xDF);
                _mdvdrState |= 0x10;
                WriteReg(0xBD, _mdvdrState);
            }
            else if (rhythmInstr < 6)
            {
                var secondOperatorOffset = instrOffset + 8;
                SetupOperator(_rhythmOperatorTable[rhythmInstr], _musicData, ref secondOperatorOffset);
                WriteReg(0xA0 + _rhythmChannelTable[rhythmInstr], _musicData[instrOffset++]);
                WriteReg(0xB0 + _rhythmChannelTable[rhythmInstr], _musicData[instrOffset++] & 0xDF);
                WriteReg(0xC0 + _rhythmChannelTable[rhythmInstr], _musicData[instrOffset]);
                _mdvdrState |= _mdvdrTable[rhythmInstr];
                WriteReg(0xBD, _mdvdrState);
            }
        }

        void UpdateSfx()
        {
            if ((--_sfxTimer) != 0)
            {
                return;
            }
            _sfxTimer = 4;

            for (int i = 0; i < _sfx.Length; ++i)
            {
                if (_sfx[i].Resource == -1)
                {
                    continue;
                }

                bool hasActiveChannel = false;
                for (int j = 0; j < _sfx[i].Channels.Length; ++j)
                {
                    if (_sfx[i].Channels[j].State != ChannelState.Off)
                    {
                        hasActiveChannel = true;
                        UpdateChannel(_sfx[i].Channels[j]);
                    }
                }

                // In case no channel is active we will stop the sfx.
                if (!hasActiveChannel)
                {
                    StopSfx(_sfx[i]);
                }
            }
        }

        void UpdateChannel(Channel channel)
        {
            if (channel.State == ChannelState.Parse)
            {
                ParseSlot(channel);
            }
            else
            {
                UpdateSlot(channel);
            }
        }

        void StopSfx(SfxSlot sfx)
        {
            if (sfx.Resource == -1)
            {
                return;
            }

            // 1. step: Clear all the channels.
            for (int i = 0; i < sfx.Channels.Length; ++i)
            {
                if (sfx.Channels[i].State != ChannelState.Off)
                {
                    ClearChannel(sfx.Channels[i]);
                    sfx.Channels[i].State = ChannelState.Off;
                }

                if (sfx.Channels[i].HardwareChannel != -1)
                {
                    FreeHWChannel(sfx.Channels[i].HardwareChannel);
                    sfx.Channels[i].HardwareChannel = -1;
                }
            }

            // TODO: 2. step: Unlock the resource.
//			_vm._res.unlock(rtSound, sfx.resource);
            sfx.Resource = -1;
        }

        void ClearChannel(Channel channel)
        {
            WriteReg(0xA0 + channel.HardwareChannel, 0x00);
            WriteReg(0xB0 + channel.HardwareChannel, 0x00);
        }

        const int AD_CALLBACK_FREQUENCY = 472;

        readonly ScummEngine _vm;
        object _mutex = new object();
        readonly IMixer _mixer;
        readonly int _rate;
        SoundHandle _soundHandle;

        IOpl _opl2;

        int _samplesPerCallback;
        int _samplesPerCallbackRemainder;
        int _samplesTillCallback;
        int _samplesTillCallbackRemainder;

        int _soundPlaying;
        int _engineMusicTimer;

        // AdLib register utilities
        byte[] _registerBackUpTable = new byte[256];

        uint _curOffset;
        uint _nextEventTimer;

        int _sfxTimer;

        byte _rndSeed;

        struct Envelope
        {
            public int StepIncrease;
            public int Step;
            public int StepCounter;
            public int Timer;
        }

        enum NoteState
        {
            PreInit = -1,
            Attack = 0,
            Decay = 1,
            Sustain = 2,
            Release = 3,
            Off = 4
        }

        class Note
        {
            public NoteState State;
            public int PlayTime;
            public int SustainTimer;
            public int InstrumentValue;
            public int Bias;
            public int PreIncrease;
            public int Adjust;
            public Envelope Envelope;
        }

        enum ChannelState
        {
            Off = 0,
            Parse = 1,
            Play = 2
        }

        class Channel
        {
            public byte[] MusicData
            {
                get;
                set;
            }

            public ChannelState State
            {
                get;
                set;
            }

            public int CurrentOffset
            {
                get;
                set;
            }

            public int StartOffset
            {
                get;
                set;
            }

            public byte[] InstrumentData
            {
                get;
                private set;
            }

            public Note[] Notes
            {
                get;
                private set;
            }

            public int HardwareChannel
            {
                get;
                set;
            }

            public Channel()
            {
                InstrumentData = new byte[7];
                Notes = new Note[2];
                for (int i = 0; i < Notes.Length; i++)
                {
                    Notes[i] = new Note();
                }
            }
        }

        class SfxSlot
        {
            public int Resource { get; set; }

            public int Priority { get; set; }

            public Channel[] Channels { get; private set; }

            public SfxSlot()
            {
                Channels = new Channel[3];
                for (int i = 0; i < Channels.Length; i++)
                {
                    Channels[i] = new Channel();
                }
            }
        }

        SfxSlot[] _sfx = new SfxSlot[3];

        struct HardwareChannel
        {
            public bool Allocated;
            public int Priority;
            public SfxSlot SfxOwner;
        }

        HardwareChannel[] _hwChannels = new HardwareChannel[9];
        int _numHWChannels;

        struct VoiceChannel
        {
            public uint lastEvent;
            public uint frequency;
            public int b0Reg;
        }

        VoiceChannel[] _voiceChannels = new VoiceChannel[9];

        int _musicVolume;
        int _sfxVolume;

        bool _isSeeking;

        byte[] _musicData;
        int _timerLimit;
        int _musicTicks;
        int _musicTimer;
        uint _internalMusicTimer;
        bool _loopFlag;
        uint _musicLoopStart;

        int[] _instrumentOffset = new int[16];

        int _mdvdrState;

        static readonly int[] _operatorOffsetToChannel =
            {
                0,  1,  2,  0,  1,  2, -1, -1,
                3,  4,  5,  3,  4,  5, -1, -1,
                6,  7,  8,  6,  7,  8
            };

        static readonly int[] _operatorOffsetTable =
            {
			0,  3,  1,  4,
			2,  5,  8, 11,
			9, 12, 10, 13,
			16, 19, 17, 20,
			18, 21
		};

		static readonly int[] _noteFrequencies =
			{
				0x200, 0x21E, 0x23F, 0x261,
				0x285, 0x2AB, 0x2D4, 0x300,
				0x32E, 0x35E, 0x390, 0x3C7
			};

		static readonly int[] _mdvdrTable =
			{
				0x00, 0x10, 0x08, 0x04, 0x02, 0x01
			};

		static readonly int[] _rhythmOperatorTable =
			{
			0x00, 0x00, 0x14, 0x12, 0x15, 0x11
		};

		static readonly int[] _rhythmChannelTable =
		{
			0x00, 0x00, 0x07, 0x08, 0x08, 0x07
		};

		static readonly int[] _noteBiasTable =
		{
			0x00, 0x00, 0x3F, 0x00, 0x3F, 0x00, 0x00
		};

		static readonly bool[] _useOperatorTable =
		{
			false, false, true, true, true, true, false
		};

		static readonly int[] _numStepsTable =
		{
			1,    4,    6,    8,
			10,   14,   18,   24,
			36,   64,  100,  160,
			240,  340,  600, 1200
		};

		static readonly int[] _noteAdjustTable =
		{
			0,  4369,  8738, 13107,
			17476, 21845, 26214, 30583,
			34952, 39321, 43690, 48059,
			52428, 56797, 61166, 65535
		};

		static readonly int[] _noteAdjustScaleTable =
		{
			255,   7,  63,  15,  63,  15,  63
		};

		static readonly uint[] _channelOperatorOffsetTable =
		{
			0, 0, 1, 1, 0, 0, 0
		};

		static readonly int[] _channelOffsetTable =
		{
			0,  1,  2,  3,
			4,  5,  6,  7,
			8,  8,  7
		};

		static readonly int[] _baseRegisterTable =
		{
			0xA0, 0xC0, 0x40, 0x20, 0x40, 0x20, 0x00
		};

		static readonly int[] _registerMaskTable =
		{
			0xFF, 0x0E, 0x3F, 0x0F, 0x3F, 0x0F, 0x00
		};

		static readonly int[] _registerShiftTable =
		{
			0, 1, 0, 0, 0, 0, 0
		};
	}
}

