//
//  Player_V2CMS.cs
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
using NScumm.Core.Audio.SoftSynth;
using System.Diagnostics;

namespace NScumm.Core.Audio
{
    /// <summary>
    /// Scumm V2 CMS/Gameblaster MIDI driver.
    /// </summary>
    class Player_V2CMS: Player_V2Base
    {
        public Player_V2CMS(ScummEngine scumm, IMixer mixer)
            : base(scumm, mixer, true)
        {
            SetMusicVolume(255);

            for (var i = 0; i < _sfxFreq.Length; i++)
            {
                _sfxFreq[i] = 0xFF;
            }
            Array.Clear(_sfxAmpl, 0, _sfxAmpl.Length);
            for (var i = 0; i < _sfxOctave.Length; i++)
            {
                _sfxOctave[i] = 0x66;
            }

            _cmsVoices[0].amplitudeOutput = value => _cmsChips[0].ampl[0] = value;
            _cmsVoices[0].freqOutput = value => _cmsChips[0].freq[0] = value;
            _cmsVoices[0].octaveOutput = value => _cmsChips[0].octave[0] = value;
            _cmsVoices[0].octaveInput = () => _cmsChips[0].octave[0];
            _cmsVoices[1].amplitudeOutput = value => _cmsChips[0].ampl[1] = value;
            _cmsVoices[1].freqOutput = value => _cmsChips[0].freq[1] = value;
            _cmsVoices[1].octaveOutput = value => _cmsChips[0].octave[0] = value;
            _cmsVoices[1].octaveInput = () => _cmsChips[0].octave[0];
            _cmsVoices[2].amplitudeOutput = value => _cmsChips[0].ampl[2] = value;
            _cmsVoices[2].freqOutput = value => _cmsChips[0].freq[2] = value;
            _cmsVoices[2].octaveOutput = value => _cmsChips[0].octave[1] = value;
            _cmsVoices[2].octaveInput = () => _cmsChips[0].octave[1];
            _cmsVoices[3].amplitudeOutput = value => _cmsChips[0].ampl[3] = value;
            _cmsVoices[3].freqOutput = value => _cmsChips[0].freq[3] = value;
            _cmsVoices[3].octaveOutput = value => _cmsChips[0].octave[1] = value;
            _cmsVoices[3].octaveInput = () => _cmsChips[0].octave[1];
            _cmsVoices[4].amplitudeOutput = value => _cmsChips[1].ampl[0] = value;
            _cmsVoices[4].freqOutput = value => _cmsChips[1].freq[0] = value;
            _cmsVoices[4].octaveOutput = value => _cmsChips[1].octave[0] = value;
            _cmsVoices[4].octaveInput = () => _cmsChips[1].octave[0];
            _cmsVoices[5].amplitudeOutput = value => _cmsChips[1].ampl[1] = value;
            _cmsVoices[5].freqOutput = value => _cmsChips[1].freq[1] = value;
            _cmsVoices[5].octaveOutput = value => _cmsChips[1].octave[0] = value;
            _cmsVoices[5].octaveInput = () => _cmsChips[1].octave[0];
            _cmsVoices[6].amplitudeOutput = value => _cmsChips[1].ampl[2] = value;
            _cmsVoices[6].freqOutput = value => _cmsChips[1].freq[2] = value;
            _cmsVoices[6].octaveOutput = value => _cmsChips[1].octave[1] = value;
            _cmsVoices[6].octaveInput = () => _cmsChips[1].octave[1];
            _cmsVoices[7].amplitudeOutput = value => _cmsChips[1].ampl[3] = value;
            _cmsVoices[7].freqOutput = value => _cmsChips[1].freq[3] = value;
            _cmsVoices[7].octaveOutput = value => _cmsChips[1].octave[1] = value;
            _cmsVoices[7].octaveInput = () => _cmsChips[1].octave[1];

            // inits the CMS Emulator like in the original
            _cmsEmu = new CMSEmulator(_sampleRate);
            for (int i = 0, cmsPort = 0x220; i < 2; cmsPort += 2, ++i)
            {
                for (int off = 0; off < 13; ++off)
                {
                    _cmsEmu.PortWrite(cmsPort + 1, _cmsInitData[off * 2]);
                    _cmsEmu.PortWrite(cmsPort, _cmsInitData[off * 2 + 1]);
                }
            }

            _soundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        public override void SetMusicVolume(int vol)
        {
        }

        public override int GetMusicTimer()
        {
            return _midiData != null ? _musicTimer : base.GetMusicTimer();
        }

        public override void StartSound(int nr)
        {
            lock (_mutex)
            {
                var data = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);
                Debug.Assert(data != null);

                if (data[6] == 0x80)
                {
                    _musicTimer = _musicTimerTicks = 0;
                    LoadMidiData(data, nr);
                }
                else
                {
                    int cprio = _current_data != null ? _current_data[_header_len] : 0;
                    int prio = data[_header_len];
                    int nprio = _next_data != null ? _next_data[_header_len] : 0;

                    int restartable = data[_header_len + 1];

                    if (_current_nr == 0 || cprio <= prio)
                    {
                        int tnr = _current_nr;
                        int tprio = cprio;
                        var tdata = _current_data;

                        ChainSound(nr, data);
                        nr = tnr;
                        prio = tprio;
                        data = tdata;
                        restartable = data != null ? data[_header_len + 1] : 0;
                    }

                    if (_current_nr == 0)
                    {
                        nr = 0;
                        _next_nr = 0;
                        _next_data = null;
                    }

                    if (nr != _current_nr
                        && restartable != 0
                        && (_next_nr == 0
                        || nprio <= prio))
                    {

                        _next_nr = nr;
                        _next_data = data;
                    }
                }
            }
        }

        public override void StopAllSounds()
        {
            lock (_mutex)
            {

                for (int i = 0; i < 4; i++)
                {
                    ClearChannel(i);
                }
                _next_nr = _current_nr = 0;
                _next_data = _current_data = null;
                _midiData = null;
                _midiSongBegin = 0;
                _midiDelay = 0;
                _musicTimer = _musicTimerTicks = 0;
                OffAllChannels();
            }
        }

        public override void StopSound(int nr)
        {
            lock (_mutex)
            {
                if (_next_nr == nr)
                {
                    _next_nr = 0;
                    _next_data = null;
                }
                if (_current_nr == nr)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        ClearChannel(i);
                    }
                    _current_nr = 0;
                    _current_data = null;
                    ChainNextSound();
                }
                if (_loadedMidiSong == nr)
                {
                    _midiData = null;
                    _midiSongBegin = 0;
                    _midiDelay = 0;
                    OffAllChannels();
                }
            }
        }

        public override int GetSoundStatus(int nr)
        {
            return (_current_nr == nr || _next_nr == nr || _loadedMidiSong == nr) ? 1 : 0;
        }

        public override int ReadBuffer(short[] buffer)
        {
            lock (_mutex)
            {
                int step = 1;
                var offset = 0;
                var numSamples = buffer.Length;
                int len = numSamples / 2;

                // maybe this needs a complete rewrite
                do
                {
                    if ((_next_tick >> FIXP_SHIFT) == 0)
                    {
                        if (_midiData != null)
                        {
                            --_voiceTimer;
                            if ((_voiceTimer & 0x01) == 0)
                                PlayVoice();

                            int newTempoSum = _tempo + _tempoSum;
                            _tempoSum = (byte)(newTempoSum & 0xFF);
                            if (newTempoSum > 0xFF)
                                ProcessMidiData();
                        }
                        else
                        {
                            NextTick();
                            Play();
                        }
                        _next_tick += _tick_len;
                    }

                    step = len;
                    if (step > (_next_tick >> FIXP_SHIFT))
                        step = (int)(_next_tick >> FIXP_SHIFT);
                    var tmp = new short[step * 2];
                    _cmsEmu.ReadBuffer(tmp);
                    Array.Copy(tmp, 0, buffer, offset, tmp.Length);
                    offset += 2 * step;
                    _next_tick -= (uint)(step << FIXP_SHIFT);
                } while ((len -= step) != 0);

                return numSamples;
            }
        }

        void Play()
        {
            _octaveMask = 0xF0;
            var chan = _channels[0].d;

            byte noiseGen = 3;

            for (int i = 1; i <= 4; ++i)
            {
                if (chan.time_left != 0)
                {
                    ushort freq = chan.freq;

                    if (i == 4)
                    {
                        if (((freq >> 8) & 0x40) != 0)
                        {
                            noiseGen = (byte)(freq & 0xFF);
                        }
                        else
                        {
                            noiseGen = 3;
                            _sfxFreq[0] = _sfxFreq[3];
                            _sfxOctave[0] = (byte)((_sfxOctave[0] & 0xF0) | ((_sfxOctave[1] & 0xF0) >> 4));
                        }
                    }
                    else
                    {
                        if (freq == 0)
                        {
                            freq = 0xFFC0;
                        }

                        int cmsOct = 2;
                        int freqOct = 0x8000;

                        while (true)
                        {
                            if (freq >= freqOct)
                            {
                                break;
                            }
                            freqOct >>= 1;
                            ++cmsOct;
                            if (cmsOct == 8)
                            {
                                --cmsOct;
                                freq = 1024;
                                break;
                            }
                        }
                        byte oct = (byte)(cmsOct << 4);
                        oct |= (byte)cmsOct;

                        oct &= _octaveMask;
                        oct |= (byte)((~_octaveMask) & _sfxOctave[(i & 3) >> 1]);
                        _sfxOctave[(i & 3) >> 1] = oct;

                        freq >>= -(cmsOct - 9);
                        _sfxFreq[i & 3] = (byte)((-(freq - 511)) & 0xFF);
                    }
                    _sfxAmpl[i & 3] = _volumeTable[chan.volume >> 12];
                }
                else
                {
                    _sfxAmpl[i & 3] = 0;
                }

                chan = _channels[i].d;
                _octaveMask ^= 0xFF;
            }

            // with the high nibble of the volumeReg value
            // the right channels amplitude is set
            // with the low value the left channels amplitude
            _cmsEmu.PortWrite(0x221, 0);
            _cmsEmu.PortWrite(0x220, _sfxAmpl[0]);
            _cmsEmu.PortWrite(0x221, 1);
            _cmsEmu.PortWrite(0x220, _sfxAmpl[1]);
            _cmsEmu.PortWrite(0x221, 2);
            _cmsEmu.PortWrite(0x220, _sfxAmpl[2]);
            _cmsEmu.PortWrite(0x221, 3);
            _cmsEmu.PortWrite(0x220, _sfxAmpl[3]);
            _cmsEmu.PortWrite(0x221, 8);
            _cmsEmu.PortWrite(0x220, _sfxFreq[0]);
            _cmsEmu.PortWrite(0x221, 9);
            _cmsEmu.PortWrite(0x220, _sfxFreq[1]);
            _cmsEmu.PortWrite(0x221, 10);
            _cmsEmu.PortWrite(0x220, _sfxFreq[2]);
            _cmsEmu.PortWrite(0x221, 11);
            _cmsEmu.PortWrite(0x220, _sfxFreq[3]);
            _cmsEmu.PortWrite(0x221, 0x10);
            _cmsEmu.PortWrite(0x220, _sfxOctave[0]);
            _cmsEmu.PortWrite(0x221, 0x11);
            _cmsEmu.PortWrite(0x220, _sfxOctave[1]);
            _cmsEmu.PortWrite(0x221, 0x14);
            _cmsEmu.PortWrite(0x220, 0x3E);
            _cmsEmu.PortWrite(0x221, 0x15);
            _cmsEmu.PortWrite(0x220, 0x01);
            _cmsEmu.PortWrite(0x221, 0x16);
            _cmsEmu.PortWrite(0x220, noiseGen);
        }

        void PlayVoice()
        {
            if (_outputTableReady != 0)
            {
                PlayMusicChips();
                _outputTableReady = 0;
            }

            _octaveMask = 0xF0;
            Voice2 voice = null;
            for (int i = 0; i < 8; ++i)
            {
                voice = _cmsVoices[i];
                _octaveMask = (byte)~_octaveMask;

                if (voice.chanNumber != 0xFF)
                {
                    ProcessChannel(voice);
                }
                else
                {
                    if (voice.curVolume == 0)
                    {
                        voice.amplitudeOutput(0);
                    }

                    int volume = voice.curVolume - voice.releaseRate;
                    if (volume < 0)
                        volume = 0;

                    voice.curVolume = (byte)volume;
                    voice.amplitudeOutput((byte)(((volume >> 4) | (volume & 0xF0)) & voice.channel));
                    ++_outputTableReady;
                }
            }
        }

        void PlayMusicChips()
        {
            int cmsPort = 0x21E;
            int i = 0;
            do
            {
                var table = _cmsChips[i];
                cmsPort += 2;
                _cmsEmu.PortWrite(cmsPort + 1, 0);
                _cmsEmu.PortWrite(cmsPort, table.ampl[0]);
                _cmsEmu.PortWrite(cmsPort + 1, 1);
                _cmsEmu.PortWrite(cmsPort, table.ampl[1]);
                _cmsEmu.PortWrite(cmsPort + 1, 2);
                _cmsEmu.PortWrite(cmsPort, table.ampl[2]);
                _cmsEmu.PortWrite(cmsPort + 1, 3);
                _cmsEmu.PortWrite(cmsPort, table.ampl[3]);
                _cmsEmu.PortWrite(cmsPort + 1, 8);
                _cmsEmu.PortWrite(cmsPort, table.freq[0]);
                _cmsEmu.PortWrite(cmsPort + 1, 9);
                _cmsEmu.PortWrite(cmsPort, table.freq[1]);
                _cmsEmu.PortWrite(cmsPort + 1, 10);
                _cmsEmu.PortWrite(cmsPort, table.freq[2]);
                _cmsEmu.PortWrite(cmsPort + 1, 11);
                _cmsEmu.PortWrite(cmsPort, table.freq[3]);
                _cmsEmu.PortWrite(cmsPort + 1, 0x10);
                _cmsEmu.PortWrite(cmsPort, table.octave[0]);
                _cmsEmu.PortWrite(cmsPort + 1, 0x11);
                _cmsEmu.PortWrite(cmsPort, table.octave[1]);
                _cmsEmu.PortWrite(cmsPort + 1, 0x14);
                _cmsEmu.PortWrite(cmsPort, 0x3F);
                _cmsEmu.PortWrite(cmsPort + 1, 0x15);
                _cmsEmu.PortWrite(cmsPort, 0x00);
                ++i;
            } while ((cmsPort & 2) == 0);
        }

        void ProcessChannel(Voice2 channel)
        {
            ++_outputTableReady;
            switch (channel.nextProcessState)
            {
                case EnvelopeState.Attack:
                    ProcessAttack(channel);
                    break;

                case EnvelopeState.Decay:
                    ProcessDecay(channel);
                    break;

                case EnvelopeState.Sustain:
                    ProcessSustain(channel);
                    break;

                case EnvelopeState.Release:
                    ProcessRelease(channel);
                    break;
            }
        }

        void ProcessRelease(Voice2 channel)
        {
            int newVolume = channel.curVolume - channel.releaseRate;
            if (newVolume < 0)
                newVolume = 0;

            channel.curVolume = (byte)newVolume;
            ProcessVibrato(channel);
        }

        void ProcessAttack(Voice2 channel)
        {
            int newVolume = channel.curVolume + channel.attackRate;
            if (newVolume > channel.maxAmpl)
            {
                channel.curVolume = channel.maxAmpl;
                channel.nextProcessState = EnvelopeState.Decay;
            }
            else
            {
                channel.curVolume = (byte)newVolume;
            }

            ProcessVibrato(channel);
        }

        void ProcessDecay(Voice2 channel)
        {
            int newVolume = channel.curVolume - channel.decayRate;
            if (newVolume <= channel.sustainRate)
            {
                channel.curVolume = channel.sustainRate;
                channel.nextProcessState = EnvelopeState.Sustain;
            }
            else
            {
                channel.curVolume = (byte)newVolume;
            }

            ProcessVibrato(channel);
        }

        void ProcessSustain(Voice2 channel)
        {
            if (channel.unkVibratoRate != 0)
            {
                short volume = (short)(channel.curVolume + channel.unkRate);
                if ((volume & 0xFF00) != 0)
                {
                    volume = (sbyte)(volume >> 8);
                    volume = (short)-volume;
                }

                channel.curVolume = (byte)volume;
                --channel.unkCount;
                if (channel.unkCount == 0)
                {
                    channel.unkRate = (sbyte)-channel.unkRate;
                    channel.unkCount = (sbyte)((channel.unkVibratoDepth & 0x0F) << 1);
                }
            }
            ProcessVibrato(channel);
        }

        void ProcessVibrato(Voice2 channel)
        {
            if (channel.vibratoRate != 0)
            {
                short temp = (short)(channel.curFreq + channel.curVibratoRate);
                channel.curOctave += (byte)((temp & 0xFF00) >> 8);
                channel.curFreq = (byte)(temp & 0xFF);

                --channel.curVibratoUnk;
                if (channel.curVibratoUnk == 0)
                {
                    channel.curVibratoRate = (sbyte)(-channel.curVibratoRate);
                    channel.curVibratoUnk = (sbyte)((channel.vibratoDepth & 0x0F) << 1);
                }
            }

            var output = channel.amplitudeOutput;
            output((byte)(((channel.curVolume >> 4) | (channel.curVolume & 0xF0)) & channel.channel));
            output = channel.freqOutput;
            output(channel.curFreq);
            output = channel.octaveOutput;
            output((byte)((((channel.curOctave << 4) | (channel.curOctave & 0x0F)) & _octaveMask) | ((~_octaveMask) & channel.octaveInput())));
        }

        void LoadMidiData(byte[] data, int sound)
        {
            Array.Clear(_midiChannelUse, 0, _midiChannelUse.Length);
            Array.Clear(_midiChannel, 0, _midiChannel.Length);

            _tempo = data[7];
            _looping = data[8];

            byte channels = data[14];
            byte curChannel = 0;
            int voice2Off = 23;

            for (; channels != 0; ++curChannel, --channels, voice2Off += 16)
            {
                if (data[15 + curChannel] != 0)
                {
                    byte channel = (byte)(data[15 + curChannel] - 1);
                    _midiChannelUse[channel] = 1;

                    var voiceDef = _cmsVoicesBase[channel];

                    byte attackDecay = data[voice2Off + 10];
                    voiceDef.attack = _attackRate[attackDecay >> 4];
                    voiceDef.decay = _decayRate[attackDecay & 0x0F];
                    byte sustainRelease = data[voice2Off + 11];
                    voiceDef.sustain = _sustainRate[sustainRelease >> 4];
                    voiceDef.release = _releaseRate[sustainRelease & 0x0F];

                    if ((data[voice2Off + 3] & 0x40) != 0)
                    {
                        voiceDef.vibrato = 0x0301;
                        if ((data[voice2Off + 13] & 0x40) != 0)
                        {
                            voiceDef.vibrato = 0x0601;
                        }
                    }
                    else
                    {
                        voiceDef.vibrato = 0;
                    }

                    if ((data[voice2Off + 8] & 0x80) != 0)
                    {
                        voiceDef.vibrato2 = 0x0506;
                        if ((data[voice2Off + 13] & 0x80) != 0)
                        {
                            voiceDef.vibrato2 = 0x050C;
                        }
                    }
                    else
                    {
                        voiceDef.vibrato2 = 0;
                    }

                    if ((data[voice2Off + 8] & 0x0F) > 1)
                    {
                        voiceDef.octadd = 0x01;
                    }
                    else
                    {
                        voiceDef.octadd = 0x00;
                    }
                }
            }

            for (int i = 0; i < 8; ++i)
            {
                _cmsVoices[i].chanNumber = 0xFF;
                _cmsVoices[i].curVolume = 0;
                _cmsVoices[i].nextVoice = null;
            }

            _midiDelay = 0;
            _cmsChips = CreateMusicChips();
            _midiData = data;
            _midiDataOffset = 151;
            _midiSongBegin = _midiDataOffset + data[9];

            _loadedMidiSong = sound;
        }

        void ProcessMidiData()
        {
            var currentData = _midiData;
            var currentDataOffset = _midiDataOffset;
            byte command = 0x00;
            short temp = 0;

            ++_musicTimerTicks;
            if (_musicTimerTicks > 60)
            {
                _musicTimerTicks = 0;
                ++_musicTimer;
            }

            if (_midiDelay == 0)
            {
                while (true)
                {
                    if ((command = currentData[currentDataOffset++]) == 0xFF)
                    {
                        if ((command = currentData[currentDataOffset++]) == 0x2F)
                        {
                            if (_looping == 0)
                            {
                                currentDataOffset = _midiDataOffset = _midiSongBegin;
                                continue;
                            }
                            _midiData = null;
                            _midiSongBegin = 0;
                            _midiDelay = 0;
                            _loadedMidiSong = 0;
                            OffAllChannels();
                            return;
                        }
                        else
                        {
                            if (command == 0x58)
                            {
                                currentDataOffset += 6;
                            }
                        }
                    }
                    else
                    {
                        _lastMidiCommand = command;
                        if (command < 0x90)
                        {
                            ClearNote(currentData, ref currentDataOffset);
                        }
                        else
                        {
                            PlayNote(currentData, ref currentDataOffset);
                        }
                    }

                    temp = command = currentData[currentDataOffset++];
                    if ((command & 0x80) != 0)
                    {
                        temp = (short)((command & 0x7F) << 8);
                        command = currentData[currentDataOffset++];
                        temp |= (short)(command << 1);
                        temp >>= 1;
                    }
                    temp >>= 1;
                    int lastBit = temp & 1;
                    temp >>= 1;
                    temp += (short)lastBit;

                    if (temp != 0)
                        break;
                }
                _midiData = currentData;
                _midiDataOffset = currentDataOffset;
                _midiDelay = temp;
            }

            --_midiDelay;
            if (_midiDelay < 0)
                _midiDelay = 0;

            return;
        }

        void OffAllChannels()
        {
            for (int cmsPort = 0x220, i = 0; i < 2; cmsPort += 2, ++i)
            {
                for (int off = 1; off <= 10; ++off)
                {
                    _cmsEmu.PortWrite(cmsPort + 1, _cmsInitData[off * 2]);
                    _cmsEmu.PortWrite(cmsPort, _cmsInitData[off * 2 + 1]);
                }
            }
        }

        void ClearNote(byte[] data, ref int offset)
        {
            var voice = GetPlayVoice(data[offset]);
            if (voice != null)
            {
                voice.chanNumber = 0xFF;
                voice.nextVoice = null;
                voice.nextProcessState = EnvelopeState.Release;
            }
            offset += 2;
        }

        Voice2 GetPlayVoice(byte param)
        {
            byte channelNum = (byte)(_lastMidiCommand & 0x0F);
            var curVoice = _midiChannel[channelNum];

            if (curVoice != null)
            {
                Voice2 prevVoice = null;
                while (true)
                {
                    if (curVoice.playingNote == param)
                        break;

                    prevVoice = curVoice;
                    curVoice = curVoice.nextVoice;
                    if (curVoice == null)
                        return null;
                }

                if (prevVoice != null)
                    prevVoice.nextVoice = curVoice.nextVoice;
                else
                    _midiChannel[channelNum] = curVoice.nextVoice;
            }

            return curVoice;
        }

        void PlayNote(byte[] data, ref int offset)
        {
            byte channel = (byte)(_lastMidiCommand & 0x0F);
            if (_midiChannelUse[channel] != 0)
            {
                var freeVoice = GetFreeVoice();
                if (freeVoice != null)
                {
                    var voice = _cmsVoicesBase[freeVoice.chanNumber];
                    freeVoice.attackRate = voice.attack;
                    freeVoice.decayRate = voice.decay;
                    freeVoice.sustainRate = voice.sustain;
                    freeVoice.releaseRate = voice.release;
                    freeVoice.octaveAdd = (sbyte)voice.octadd;
                    freeVoice.vibratoRate = freeVoice.curVibratoRate = (sbyte)(voice.vibrato & 0xFF);
                    freeVoice.vibratoDepth = freeVoice.curVibratoUnk = (sbyte)(voice.vibrato >> 8);
                    freeVoice.unkVibratoRate = freeVoice.unkRate = (sbyte)(voice.vibrato2 & 0xFF);
                    freeVoice.unkVibratoDepth = freeVoice.unkCount = (sbyte)(voice.vibrato2 >> 8);
                    freeVoice.maxAmpl = 0xFF;

                    byte rate = freeVoice.attackRate;
                    byte volume = (byte)(freeVoice.curVolume >> 1);

                    if (rate < volume)
                        rate = volume;

                    rate -= freeVoice.attackRate;
                    freeVoice.curVolume = rate;
                    freeVoice.playingNote = (sbyte)data[offset];

                    int effectiveNote = freeVoice.playingNote + 3;
                    if (effectiveNote < 0 || effectiveNote >= _midiNotes.Length)
                    {
                        Debug.WriteLine("Player_V2CMS::playNote: Note {0} out of bounds", effectiveNote);
                        effectiveNote = ScummHelper.Clip(effectiveNote, 0, _midiNotes.Length - 1);
                    }

                    int octave = _midiNotes[effectiveNote].baseOctave + freeVoice.octaveAdd - 3;
                    if (octave < 0)
                        octave = 0;
                    if (octave > 7)
                        octave = 7;
                    if (octave == 0)
                        ++octave;
                    freeVoice.curOctave = (byte)octave;
                    freeVoice.curFreq = _midiNotes[effectiveNote].frequency;
                    freeVoice.curVolume = 0;
                    freeVoice.nextProcessState = EnvelopeState.Attack;
                    if ((_lastMidiCommand & 1) == 0)
                        freeVoice.channel = 0xF0;
                    else
                        freeVoice.channel = 0x0F;
                }
            }
            offset += 2;
        }

        Voice2 GetFreeVoice()
        {
            Voice2 curVoice = null;
            Voice2 selected = null;
            byte volume = 0xFF;

            for (int i = 0; i < 8; ++i)
            {
                curVoice = _cmsVoices[i];

                if (curVoice.chanNumber == 0xFF)
                {
                    if (curVoice.curVolume == 0)
                    {
                        selected = curVoice;
                        break;
                    }

                    if (curVoice.curVolume < volume)
                    {
                        selected = curVoice;
                        volume = selected.curVolume;
                    }
                }
            }

            if (selected != null)
            {
                selected.chanNumber = (byte)(_lastMidiCommand & 0x0F);

                byte channel = selected.chanNumber;
                var oldChannel = _midiChannel[channel];
                _midiChannel[channel] = selected;
                selected.nextVoice = oldChannel;
            }

            return selected;
        }

        class Voice
        {
            public byte attack;
            public byte decay;
            public byte sustain;
            public byte release;
            public byte octadd;
            public short vibrato;
            public short vibrato2;
            public short noise;
        }

        enum EnvelopeState
        {
            Attack,
            Decay,
            Sustain,
            Release
        }

        class Voice2
        {
            public Action<byte> amplitudeOutput;
            public Action<byte> freqOutput;
            public Action<byte> octaveOutput;
            public Func<byte> octaveInput;

            public byte channel;
            public sbyte sustainLevel;
            public byte attackRate;
            public byte maxAmpl;
            public byte decayRate;
            public byte sustainRate;
            public byte releaseRate;
            public byte releaseTime;
            public sbyte vibratoRate;
            public sbyte vibratoDepth;

            public sbyte curVibratoRate;
            public sbyte curVibratoUnk;

            public sbyte unkVibratoRate;
            public sbyte unkVibratoDepth;

            public sbyte unkRate;
            public sbyte unkCount;

            public EnvelopeState nextProcessState;
            public byte curVolume;
            public byte curOctave;
            public byte curFreq;

            public sbyte octaveAdd;

            public sbyte playingNote;
            public Voice2 nextVoice;

            public byte chanNumber;
        }

        class MusicChip
        {
            public byte[] ampl = new byte[4];
            public byte[] freq = new byte[4];
            public byte[] octave = new byte[2];
        }

        struct MidiNote
        {
            public byte frequency;
            public byte baseOctave;

            public MidiNote(byte frequency, byte baseOctave)
            {
                this.frequency = frequency;
                this.baseOctave = baseOctave;
            }

            public static explicit operator MidiNote(byte[] fahr)
            {
                return new MidiNote(fahr[0], fahr[1]);
            }
        }

        Voice[] _cmsVoicesBase = CreateVoicesBase();
        Voice2[] _cmsVoices = CreateVoices();
        MusicChip[] _cmsChips;

        static MusicChip[] CreateMusicChips()
        {
            var chips = new MusicChip[2];
            for (int i = 0; i < chips.Length; i++)
            {
                chips[i] = new MusicChip();
            }
            return chips;
        }

        static Voice2[] CreateVoices()
        {
            var voices = new Voice2[8];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = new Voice2();
            }
            return voices;
        }

        static Voice[] CreateVoicesBase()
        {
            var voices = new Voice[16];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = new Voice();
            }
            return voices;
        }

        byte _tempo;
        byte _tempoSum;
        byte _looping;
        byte _octaveMask;
        short _midiDelay;
        Voice2[] _midiChannel = new Voice2[16];
        byte[] _midiChannelUse = new byte[16];
        byte[] _midiData;
        int _midiDataOffset;
        int _midiSongBegin;

        int _loadedMidiSong;

        byte[] _sfxFreq = new byte[4];
        byte[] _sfxAmpl = new byte[4];
        byte[] _sfxOctave = new byte[2];

        byte _lastMidiCommand;
        uint _outputTableReady;
        byte _voiceTimer;

        int _musicTimer, _musicTimerTicks;

        CMSEmulator _cmsEmu;

        static readonly MidiNote[] _midiNotes =
            {
                new MidiNote(3, 0), new MidiNote(31, 0), new MidiNote(58, 0), new MidiNote(83, 0),
                new MidiNote(107, 0), new MidiNote(130, 0), new MidiNote(151, 0), new MidiNote(172, 0),
                new MidiNote(191, 0), new MidiNote(209, 0), new MidiNote(226, 0), new MidiNote(242, 0),
                new MidiNote(3, 1), new MidiNote(31, 1), new MidiNote(58, 1), new MidiNote(83, 1),
                new MidiNote(107, 1), new MidiNote(130, 1), new MidiNote(151, 1), new MidiNote(172, 1),
                new MidiNote(191, 1), new MidiNote(209, 1), new MidiNote(226, 1), new MidiNote(242, 1),
                new MidiNote(3, 2), new MidiNote(31, 2), new MidiNote(58, 2), new MidiNote(83, 2),
                new MidiNote(107, 2), new MidiNote(130, 2), new MidiNote(151, 2), new MidiNote(172, 2),
                new MidiNote(191, 2), new MidiNote(209, 2), new MidiNote(226, 2), new MidiNote(242, 2),
                new MidiNote(3, 3), new MidiNote(31, 3), new MidiNote(58, 3), new MidiNote(83, 3),
                new MidiNote(107, 3), new MidiNote(130, 3), new MidiNote(151, 3), new MidiNote(172, 3),
                new MidiNote(191, 3), new MidiNote(209, 3), new MidiNote(226, 3), new MidiNote(242, 3),
                new MidiNote(3, 4), new MidiNote(31, 4), new MidiNote(58, 4), new MidiNote(83, 4),
                new MidiNote(107, 4), new MidiNote(130, 4), new MidiNote(151, 4), new MidiNote(172, 4),
                new MidiNote(191, 4), new MidiNote(209, 4), new MidiNote(226, 4), new MidiNote(242, 4),
                new MidiNote(3, 5), new MidiNote(31, 5), new MidiNote(58, 5), new MidiNote(83, 5),
                new MidiNote(107, 5), new MidiNote(130, 5), new MidiNote(151, 5), new MidiNote(172, 5),
                new MidiNote(191, 5), new MidiNote(209, 5), new MidiNote(226, 5), new MidiNote(242, 5),
                new MidiNote(3, 6), new MidiNote(31, 6), new MidiNote(58, 6), new MidiNote(83, 6),
                new MidiNote(107, 6), new MidiNote(130, 6), new MidiNote(151, 6), new MidiNote(172, 6),
                new MidiNote(191, 6), new MidiNote(209, 6), new MidiNote(226, 6), new MidiNote(242, 6),
                new MidiNote(3, 7), new MidiNote(31, 7), new MidiNote(58, 7), new MidiNote(83, 7),
                new MidiNote(107, 7), new MidiNote(130, 7), new MidiNote(151, 7), new MidiNote(172, 7),
                new MidiNote(191, 7), new MidiNote(209, 7), new MidiNote(226, 7), new MidiNote(242, 7),
                new MidiNote(3, 8), new MidiNote(31, 8), new MidiNote(58, 8), new MidiNote(83, 8),
                new MidiNote(107, 8), new MidiNote(130, 8), new MidiNote(151, 8), new MidiNote(172, 8),
                new MidiNote(191, 8), new MidiNote(209, 8), new MidiNote(226, 8), new MidiNote(242, 8),
                new MidiNote(3, 9), new MidiNote(31, 9), new MidiNote(58, 9), new MidiNote(83, 9),
                new MidiNote(107, 9), new MidiNote(130, 9), new MidiNote(151, 9), new MidiNote(172, 9),
                new MidiNote(191, 9), new MidiNote(209, 9), new MidiNote(226, 9), new MidiNote(242, 9),
                new MidiNote(3, 10), new MidiNote(31, 10), new MidiNote(58, 10), new MidiNote(83, 10),
                new MidiNote(107, 10), new MidiNote(130, 10), new MidiNote(151, 10), new MidiNote(172, 10),
                new MidiNote(191, 10), new MidiNote(209, 10), new MidiNote(226, 10), new MidiNote(242, 10)
            };

        static readonly byte[] _attackRate =
            {
                0,   2,   4,   7,  14,  26,  48,  82,
                128, 144, 160, 176, 192, 208, 224, 255
            };

        static readonly byte[] _decayRate =
            {
                0,   1,   2,   3,   4,   6,  12,  24,
                48,  96, 192, 215, 255, 255, 255, 255
            };

        static readonly byte[] _sustainRate =
            {
                255, 180, 128,  96,  80,  64,  56,  48,
                42,  36,  32,  28,  24,  20,  16,   0
            };

        static readonly byte[] _releaseRate =
            {
                0,   1,   2,   4,   6,   9,  14,  22,
                36,  56,  80, 100, 120, 140, 160, 255
            };

        static readonly byte[] _volumeTable =
            {
                0x00, 0x10, 0x10, 0x11, 0x11, 0x21, 0x22, 0x22,
                0x33, 0x44, 0x55, 0x66, 0x88, 0xAA, 0xCC, 0xFF
            };

        static readonly byte[] _cmsInitData =
            {
                0x1C, 0x02,
                0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05, 0x00,
                0x14, 0x3F, 0x15, 0x00, 0x16, 0x00, 0x18, 0x00, 0x19, 0x00, 0x1C, 0x01
            };
    }
}

