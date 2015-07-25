//
//  Player_Mac.cs
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
using System.Diagnostics;
using System.IO;
using NScumm.Core.IO;

namespace NScumm.Core.Audio
{
    /// <summary>
    /// Scumm Macintosh music driver, base class.
    /// </summary>
    class Player_Mac: IMusicEngine, IAudioStream
    {
        public bool IsStereo { get { return false; } }

        public bool IsEndOfData { get { return false; } }

        bool IAudioStream.IsEndOfStream { get { return IsEndOfData; } }

        public int Rate { get { return _sampleRate; } }

        public Player_Mac(ScummEngine scumm, IMixer mixer, int numberOfChannels, int channelMask, bool fadeNoteEnds)
        {
            _vm = scumm;
            _mixer = mixer;
            _sampleRate = _mixer.OutputRate;
            _soundPlaying = -1;
            _numberOfChannels = numberOfChannels;
            _channelMask = channelMask;
            _fadeNoteEnds = fadeNoteEnds;
            Debug.Assert(scumm != null);
            Debug.Assert(mixer != null);

            Init();
        }

        public virtual void Dispose()
        {
            lock (_mutex)
            {
                _mixer.StopHandle(_soundHandle);
                StopAllSounds_Internal();
            }
        }

        public virtual int GetSoundStatus(int nr)
        {
            return (_soundPlaying == nr) ? 1 : 0;
        }

        public virtual int GetMusicTimer()
        {
            return 0;
        }

        public virtual void SetMusicVolume(int vol)
        {
            Debug.WriteLine("Player_Mac::setMusicVolume({0})", vol);
        }

        public virtual void StartSound(int nr)
        {
            lock (_mutex)
            {
                Debug.WriteLine("Player_Mac::startSound({0})", nr);

                StopAllSounds_Internal();

                var ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);
                Debug.Assert(ptr != null);

                if (!LoadMusic(ptr))
                {
                    return;
                }

//            _vm._res.lock(rtSound, nr);
                _soundPlaying = nr;
            }
        }

        public void StopSound(int nr)
        {
            lock (_mutex)
            {
                Debug.WriteLine("Player_Mac::stopSound({0})", nr);

                if (nr == _soundPlaying)
                {
                    StopAllSounds();
                }
            }
        }

        public void StopAllSounds()
        {
            lock (_mutex)
            {
                Debug.WriteLine("Player_Mac::stopAllSounds()");
                StopAllSounds_Internal();
            }
        }

        public int ReadBuffer(short[] data, int count)
        {
            lock (_mutex)
            {
                Array.Clear(data, 0, count);
                if (_soundPlaying == -1)
                {
                    return count;
                }

                bool notesLeft = false;

                for (int i = 0; i < _numberOfChannels; i++)
                {
                    if ((_channelMask & (1 << i)) == 0)
                    {
                        continue;
                    }

                    uint samplesLeft = (uint)count;
                    var ptr = data;
                    var offset = 0;

                    while (samplesLeft > 0)
                    {
                        int generated;
                        if (_channel[i]._remaining == 0)
                        {
                            uint samples;
                            int pitchModifier;
                            byte velocity;
                            if (GetNextNote(i, out samples, out pitchModifier, out velocity))
                            {
                                _channel[i]._remaining = samples;
                                _channel[i]._pitchModifier = pitchModifier;
                                _channel[i]._velocity = velocity;

                            }
                            else
                            {
                                _channel[i]._pitchModifier = 0;
                                _channel[i]._velocity = 0;
                                _channel[i]._remaining = samplesLeft;
                            }
                        }
                        generated = (int)Math.Min(_channel[i]._remaining, samplesLeft);
                        if (_channel[i]._velocity != 0)
                        {
                            _channel[i]._instrument.GenerateSamples(ptr, offset, _channel[i]._pitchModifier, _channel[i]._velocity, generated, (int)_channel[i]._remaining, _fadeNoteEnds);
                        }
                        offset += generated;
                        samplesLeft -= (uint)generated;
                        _channel[i]._remaining -= (uint)generated;
                    }

                    if (_channel[i]._notesLeft)
                    {
                        notesLeft = true;
                    }
                }

                if (!notesLeft)
                {
                    StopAllSounds_Internal();
                }

                return count;
            }
        }

        public void SaveOrLoad(Serializer ser)
        {
            lock (_mutex)
            {
                if (ser.Version < 94)
                {
                    if (_vm.Game.GameId == GameId.Monkey1 && ser.IsLoading)
                    {
                        var dummyImuse = NScumm.Core.Audio.IMuse.IMuse.Create(null, null);
                        dummyImuse.SaveOrLoad(ser);
                    }
                }
                else
                {
                    var musicEntries = new []
                    {
                        LoadAndSaveEntry.Create(r => _sampleRate = r.ReadInt32(), w => w.WriteInt32(_sampleRate), 94),
                        LoadAndSaveEntry.Create(r => _soundPlaying = r.ReadInt16(), w => w.WriteInt16(_soundPlaying), 94),
                    };

                    int mixerSampleRate = _sampleRate;
                    int i;

                    musicEntries.ForEach(e => e.Execute(ser));

                    if (ser.IsLoading && _soundPlaying != -1)
                    {
                        var ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, _soundPlaying);
                        Debug.Assert(ptr != null);
                        LoadMusic(ptr);
                    }

                    _channel.ForEach(c => c.SaveOrLoad(ser));
                    _channel.ForEach(c => c._instrument.SaveOrLoad(ser));

                    if (ser.IsLoading)
                    {
                        // If necessary, adjust the channel data to fit the
                        // current sample rate.
                        if (_soundPlaying != -1 && _sampleRate != mixerSampleRate)
                        {
                            double mult = (double)_sampleRate / (double)mixerSampleRate;
                            for (i = 0; i < _numberOfChannels; i++)
                            {
                                _channel[i]._pitchModifier = (int)((double)_channel[i]._pitchModifier * mult);
                                _channel[i]._remaining = (uint)((double)_channel[i]._remaining / mult);
                            }
                        }
                        _sampleRate = mixerSampleRate;
                    }
                }
            }
        }

        protected uint DurationToSamples(ushort duration)
        {
            // The correct formula should be:
            //
            // (duration * 473 * _sampleRate) / (4 * 480 * 480)
            //
            // But that's likely to cause integer overflow, so we do it in two
            // steps using bitwise operations to perform
            // ((duration * 473 * _sampleRate) / 4096) without overflowing,
            // then divide this by 225
            // (note that 4 * 480 * 480 == 225 * 4096 == 225 << 12)
            //
            // The original code is a bit unclear on if it should be 473 or 437,
            // but since the comments indicated 473 I'm assuming 437 was a typo.
            uint samples = (uint)(duration * _sampleRate);
            samples = (samples >> 12) * 473 + (((samples & 4095) * 473) >> 12);
            samples = samples / 225;
            return samples;
        }

        protected int NoteToPitchModifier(byte note, Instrument instrument)
        {
            if (note > 0)
            {
                var pitchIdx = note + 60 - instrument._baseFreq;
                // I don't want to use floating-point arithmetics here, but I
                // ran into overflow problems with the church music in Monkey
                // Island. It's only once per note, so it should be ok.
                double mult = (double)instrument._rate / (double)_sampleRate;
                return (int)(mult * _pitchTable[pitchIdx]);
            }
            else
            {
                return 0;
            }
        }

        protected virtual bool CheckMusicAvailable()
        {
            return false;
        }

        protected virtual bool LoadMusic(byte[] ptr)
        {
            return false;
        }

        protected virtual bool GetNextNote(int ch, out uint samples, out int pitchModifier, out byte velocity)
        {
            samples = 0;
            pitchModifier = 0;
            velocity = 0;
            return false;
        }

        public static readonly uint RES_SND = GetResSnd();

        static uint GetResSnd()
        {
            return ScummHelper.SwapBytes(BitConverter.ToUInt32(
                System.Text.Encoding.UTF8.GetBytes("snd "), 0));
        }

        void StopAllSounds_Internal()
        {
//            if (_soundPlaying != -1) {
//                _vm._res.unlock(rtSound, _soundPlaying);
//            }
            _soundPlaying = -1;
            for (int i = 0; i < _numberOfChannels; i++)
            {
                // The channel data is managed by the resource manager, so
                // don't delete that.
//                delete[] _channel[i]._instrument._data;
                _channel[i]._instrument._data = null;

                _channel[i]._remaining = 0;
                _channel[i]._notesLeft = false;
            }
        }

        void Init()
        {
            _channel = new Channel[_numberOfChannels];

            for (var i = 0; i < _numberOfChannels; i++)
            {
                _channel[i] = new Channel();
                _channel[i]._looped = false;
                _channel[i]._length = 0;
                _channel[i]._data = null;
                _channel[i]._dataOffset = 0;
                _channel[i]._pos = 0;
                _channel[i]._pitchModifier = 0;
                _channel[i]._velocity = 0;
                _channel[i]._remaining = 0;
                _channel[i]._notesLeft = false;
                _channel[i]._instrument._data = null;
                _channel[i]._instrument._size = 0;
                _channel[i]._instrument._rate = 0;
                _channel[i]._instrument._loopStart = 0;
                _channel[i]._instrument._loopEnd = 0;
                _channel[i]._instrument._baseFreq = 0;
                _channel[i]._instrument._pos = 0;
                _channel[i]._instrument._subPos = 0;
            }

            _pitchTable[116] = 1664510;
            _pitchTable[117] = 1763487;
            _pitchTable[118] = 1868350;
            _pitchTable[119] = 1979447;
            _pitchTable[120] = 2097152;
            _pitchTable[121] = 2221855;
            _pitchTable[122] = 2353973;
            _pitchTable[123] = 2493948;
            _pitchTable[124] = 2642246;
            _pitchTable[125] = 2799362;
            _pitchTable[126] = 2965820;
            _pitchTable[127] = 3142177;
            for (var i = 115; i >= 0; --i)
            {
                _pitchTable[i] = _pitchTable[i + 12] / 2;
            }

            SetMusicVolume(255);

            if (!CheckMusicAvailable())
            {
                return;
            }

            _soundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        object _mutex = new object();
        readonly IMixer _mixer;
        SoundHandle _soundHandle;
        int _sampleRate;
        int _soundPlaying;

        protected class Instrument
        {
            public byte[] _data;
            public uint _size;
            public uint _rate;
            public uint _loopStart;
            public uint _loopEnd;
            public byte _baseFreq;

            public uint _pos;
            public uint _subPos;

            public void NewNote()
            {
                _pos = 0;
                _subPos = 0;
            }

            public void SaveOrLoad(Serializer ser)
            {
                var instrumentEntries = new []
                {
                    LoadAndSaveEntry.Create(r => _pos = r.ReadUInt32(), w => w.WriteUInt32(_pos), 94),
                    LoadAndSaveEntry.Create(r => _subPos = r.ReadUInt32(), w => w.WriteUInt32(_subPos), 94),
                };

                instrumentEntries.ForEach(e => e.Execute(ser));
            }

            public void GenerateSamples(short[] data, int offset, int pitchModifier, int volume, int numSamples, int remainingSamplesOnNote, bool fadeNoteEnds)
            {
                int samplesLeft = numSamples;
                while (samplesLeft != 0)
                {
                    _subPos += (uint)pitchModifier;
                    while (_subPos >= 0x10000)
                    {
                        _subPos -= 0x10000;
                        _pos++;
                        if (_pos >= _loopEnd)
                        {
                            _pos = _loopStart;
                        }
                    }

                    int newSample = (((short)((_data[_pos] << 8) ^ 0x8000)) * volume) / 255;

                    if (fadeNoteEnds)
                    {
                        // Fade out the last 100 samples on each note. Even at
                        // low output sample rates this is just a fraction of a
                        // second, but it gets rid of distracting "pops" at the
                        // end when the sample would otherwise go abruptly from
                        // something to nothing. This was particularly
                        // noticeable on the distaff notes in Loom.
                        //
                        // The reason it's conditional is that Monkey Island
                        // appears to have a "hold current note" command, and
                        // if we fade out the current note in that case we
                        // will actually introduce new "pops".

                        remainingSamplesOnNote--;
                        if (remainingSamplesOnNote < 100)
                        {
                            newSample = (newSample * remainingSamplesOnNote) / 100;
                        }
                    }

                    int sample = data[offset] + newSample;
                    if (sample > 32767)
                    {
                        sample = 32767;
                    }
                    else if (sample < -32768)
                    {
                        sample = -32768;
                    }

                    data[offset++] = (short)sample;
                    samplesLeft--;
                }
            }
        }

        protected class Channel
        {
            public Instrument _instrument;
            public bool _looped;
            public uint _length;
            public byte[] _data;
            public int _dataOffset;

            public uint _pos;
            public int _pitchModifier;
            public byte _velocity;
            public uint _remaining;

            public bool _notesLeft;

            public Channel()
            {
                _instrument = new Instrument();
            }

            public void SaveOrLoad(Serializer ser)
            {
                var channelEntries = new []
                {
                    LoadAndSaveEntry.Create(r => _pos = r.ReadUInt16(), w => w.WriteUInt16(_pos), 94),
                    LoadAndSaveEntry.Create(r => _pitchModifier = r.ReadInt32(), w => w.WriteInt32(_pitchModifier), 94),
                    LoadAndSaveEntry.Create(r => _velocity = r.ReadByte(), w => w.WriteByte(_velocity), 94),
                    LoadAndSaveEntry.Create(r => _remaining = r.ReadUInt32(), w => w.WriteUInt32(_remaining), 94),
                    LoadAndSaveEntry.Create(r => _notesLeft = r.ReadBoolean(), w => w.WriteByte(_notesLeft), 94),
                };

                channelEntries.ForEach(e => e.Execute(ser));
            }

            public bool LoadInstrument(Stream stream)
            {
                var br = new BinaryReader(stream);
                ushort soundType = br.ReadUInt16BigEndian();
                if (soundType != 1)
                {
                    Debug.WriteLine("Player_Mac::loadInstrument: Unsupported sound type {0}", soundType);
                    return false;
                }
                var typeCount = br.ReadUInt16BigEndian();
                if (typeCount != 1)
                {
                    Debug.WriteLine("Player_Mac::loadInstrument: Unsupported data type count %d", typeCount);
                    return false;
                }
                var dataType = br.ReadUInt16BigEndian();
                if (dataType != 5)
                {
                    Debug.WriteLine("Player_Mac::loadInstrument: Unsupported data type %d", dataType);
                    return false;
                }

                br.ReadUInt32BigEndian(); // initialization option

                var cmdCount = br.ReadUInt16BigEndian();
                if (cmdCount != 1)
                {
                    Debug.WriteLine("Player_Mac::loadInstrument: Unsupported command count %d", cmdCount);
                    return false;
                }
                var command = br.ReadUInt16BigEndian();
                if (command != 0x8050 && command != 0x8051)
                {
                    Debug.WriteLine("Player_Mac::loadInstrument: Unsupported command 0x%04X", command);
                    return false;
                }

                br.ReadUInt16BigEndian(); // 0
                var soundHeaderOffset = br.ReadUInt32BigEndian();

                stream.Seek(soundHeaderOffset, SeekOrigin.Begin);

                var soundDataOffset = br.ReadUInt32BigEndian();
                var size = br.ReadUInt32BigEndian();
                var rate = br.ReadUInt32BigEndian() >> 16;
                var loopStart = br.ReadUInt32BigEndian();
                var loopEnd = br.ReadUInt32BigEndian();
                byte encoding = br.ReadByte();
                byte baseFreq = br.ReadByte();

                if (encoding != 0)
                {
                    Debug.WriteLine("Player_Mac::loadInstrument: Unsupported encoding %d", encoding);
                    return false;
                }

                stream.Seek(soundDataOffset, SeekOrigin.Current);

                var data = br.ReadBytes((int)size);
                _instrument._data = data;
                _instrument._size = size;
                _instrument._rate = rate;
                _instrument._loopStart = loopStart;
                _instrument._loopEnd = loopEnd;
                _instrument._baseFreq = baseFreq;

                return true;
            }
        }

        readonly int[] _pitchTable = new int[128];
        readonly int _numberOfChannels;
        int _channelMask;
        bool _fadeNoteEnds;

        protected ScummEngine _vm;
        protected Channel[] _channel;
    }

}

