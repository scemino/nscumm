//
//  Rjp1.cs
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
using System.IO;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio
{
    class Rjp1Channel
    {
        public byte index;
        public ByteAccess waveData;
        public ByteAccess modulatePeriodData;
        public ByteAccess modulateVolumeData;
        public ByteAccess envelopeData;
        public ushort volumeScale;
        public short volume;
        public ushort modulatePeriodBase;
        public uint modulatePeriodLimit;
        public uint modulatePeriodIndex;
        public ushort modulateVolumeBase;
        public uint modulateVolumeLimit;
        public uint modulateVolumeIndex;
        public byte freqStep;
        public uint freqInc;
        public uint freqInit;
        public ByteAccess noteData;
        public ByteAccess sequenceOffsets;
        public ByteAccess sequenceData;
        public byte loopSeqCount;
        public byte loopSeqCur;
        public byte loopSeq2Count;
        public byte loopSeq2Cur;
        public bool active;
        public short modulatePeriodInit;
        public short modulatePeriodNext;
        public bool setupNewNote;
        public sbyte envelopeMode;
        public sbyte envelopeScale;
        public sbyte envelopeEnd1;
        public sbyte envelopeEnd2;
        public sbyte envelopeStart;
        public sbyte envelopeVolume;
        public byte currentInstrument;
        public ByteAccess data;
        public ushort pos;
        public ushort len;
        public ushort repeatPos;
        public ushort repeatLen;
        public bool isSfx;

        public Rjp1Channel(int index)
        {
            this.index = (byte)index;
        }
    }

    public class Rjp1 : Paula
    {
        class Vars
        {
            public byte[] instData;
            public byte[][] songData = new byte[7][];
            public byte activeChannelsMask;
            public byte currentChannel;
            public int subsongsCount;
            public int instrumentsCount;
        }

        private Vars _vars;
        private Rjp1Channel[] _channelsTable;
        private static readonly short[] _periodsTable = {
            0x01C5, 0x01E0, 0x01FC, 0x021A, 0x023A, 0x025C, 0x0280, 0x02A6, 0x02D0,
            0x02FA, 0x0328, 0x0358, 0x00E2, 0x00F0, 0x00FE, 0x010D, 0x011D, 0x012E,
            0x0140, 0x0153, 0x0168, 0x017D, 0x0194, 0x01AC, 0x0071, 0x0078, 0x007F,
            0x0087, 0x008F, 0x0097, 0x00A0, 0x00AA, 0x00B4, 0x00BE, 0x00CA, 0x00D6
        };

        public Rjp1(int rate, bool stereo)
            : base(stereo, rate, (uint)(rate / 50))
        {
            _vars = new Vars();
            _channelsTable = new Rjp1Channel[4];
            for (int i = 0; i < 4; i++)
            {
                _channelsTable[i] = new Rjp1Channel(i);
            }
        }

        private bool Load(Stream songData, Stream instrumentsData)
        {
            var brSong = new BinaryReader(songData);
            var brInstrument = new BinaryReader(instrumentsData);
            if (brSong.ReadUInt32BigEndian() == ScummHelper.MakeTag('R', 'J', 'P', '1') &&
                brSong.ReadUInt32BigEndian() == ScummHelper.MakeTag('S', 'M', 'O', 'D'))
            {
                for (int i = 0; i < 7; ++i)
                {
                    uint size = brSong.ReadUInt32BigEndian();
                    _vars.songData[i] = new byte[size];

                    songData.Read(_vars.songData[i], 0, (int)size);
                    switch (i)
                    {
                        case 0:
                            _vars.instrumentsCount = (int)(size / 32);
                            break;
                        case 1:
                            break;
                        case 2:
                            // sequence index to offsets, 1 per channel
                            _vars.subsongsCount = (int)(size / 4);
                            break;
                        case 3:
                        case 4:
                            // sequence offsets
                            break;
                        case 5:
                        case 6:
                            // sequence data
                            break;
                    }
                }

                if (brInstrument.ReadUInt32BigEndian() == ScummHelper.MakeTag('R', 'J', 'P', '1'))
                {
                    int size = (int)(instrumentsData.Length - 4);
                    _vars.instData = new byte[size];
                    instrumentsData.Read(_vars.instData, 0, size);

                }
            }

            D.Debug(5, $"Rjp1::load() _instrumentsCount = {_vars.instrumentsCount} _subsongsCount = {_vars.subsongsCount}");
            return true;
        }

        private void StartPattern(int ch, int pat)
        {
            Rjp1Channel channel = _channelsTable[ch];
            _vars.activeChannelsMask |= (byte)(1 << ch);
            channel.sequenceData = new ByteAccess(_vars.songData[6], _vars.songData[4].ToInt32BigEndian(pat * 4));
            channel.loopSeqCount = 6;
            channel.loopSeqCur = channel.loopSeq2Cur = 1;
            channel.active = true;
            channel.isSfx = true;
            // "start" Paula audiostream
            StartPaula();
        }

        private void StartSong(int song)
        {
            if (song == 0 || song >= _vars.subsongsCount)
            {
                D.Warning($"Invalid subsong number {song}, defaulting to 1");
                song = 1;
            }
            var p = new ByteAccess(_vars.songData[2], (song & 0x3F) * 4);
            for (int i = 0; i < 4; ++i)
            {
                byte seq = p.Value; p.Offset++;
                if (seq != 0)
                {
                    StartSequence((byte)i, seq);
                }
            }
            // "start" Paula audiostream
            StartPaula();
        }

        private void StartSequence(byte channelNum, byte seqNum)
        {
            Rjp1Channel channel = _channelsTable[channelNum];
            _vars.activeChannelsMask |= (byte)(1 << channelNum);
            if (seqNum != 0)
            {
                var p = new ByteAccess(_vars.songData[5], _vars.songData[3].ToInt32BigEndian(seqNum * 4));
                byte seq = p.Value; p.Offset++;
                channel.sequenceOffsets = p;
                channel.sequenceData = new ByteAccess(_vars.songData[6], _vars.songData[4].ToInt32BigEndian(seq * 4));
                channel.loopSeqCount = 6;
                channel.loopSeqCur = channel.loopSeq2Cur = 1;
                channel.active = true;
            }
            else
            {
                channel.active = false;
                TurnOffChannel(channel);
            }
        }

        private void TurnOffChannel(Rjp1Channel channel)
        {
            StopPaulaChannel(channel.index);
        }

        private void PlayChannel(Rjp1Channel channel)
        {
            if (channel.active)
            {
                TurnOnChannel(channel);
                if (channel.sequenceData != null)
                {
                    PlaySongSequence(channel);
                }
                ModulateVolume(channel);
                ModulatePeriod(channel);
            }
        }

        private void TurnOnChannel(Rjp1Channel channel)
        {
            if (channel.setupNewNote)
            {
                channel.setupNewNote = false;
                SetupPaulaChannel(channel.index, channel.data, channel.pos, channel.len, channel.repeatPos, channel.repeatLen);
            }
        }

        bool ExecuteSfxSequenceOp(Rjp1Channel channel, byte code, ByteAccess p)
        {
            bool loop = true;
            switch (code & 7)
            {
                case 0:
                    _vars.activeChannelsMask &= (byte)(~(1 << _vars.currentChannel));
                    loop = false;
                    StopPaula();
                    break;
                case 1:
                    SetRelease(channel);
                    loop = false;
                    break;
                case 2:
                    channel.loopSeqCount = p.Value; p.Offset++;
                    break;
                case 3:
                    channel.loopSeq2Count = p.Value; p.Offset++;
                    break;
                case 4:
                    code = p.Value; p.Offset++;
                    if (code != 0)
                    {
                        SetupInstrument(channel, code);
                    }
                    break;
                case 7:
                    loop = false;
                    break;
            }
            return loop;
        }

        private bool ExecuteSongSequenceOp(Rjp1Channel channel, byte code, ByteAccess p)
        {
            bool loop = true;
            ByteAccess offs;
            switch (code & 7)
            {
                case 0:
                    offs = channel.sequenceOffsets;
                    channel.loopSeq2Count = 1;
                    while (true)
                    {
                        code = offs.Value; offs.Offset++;
                        if (code != 0)
                        {
                            channel.sequenceOffsets = offs;
                            p = new ByteAccess(_vars.songData[6], _vars.songData[4].ToInt32BigEndian(code * 4));
                            break;
                        }
                        else
                        {
                            code = offs[0];
                            if (code == 0)
                            {
                                p = null;
                                channel.active = false;
                                _vars.activeChannelsMask &= (byte)(~(1 << _vars.currentChannel));
                                loop = false;
                                break;
                            }
                            else if ((code & 0x80) != 0)
                            {
                                code = offs[1];
                                offs = new ByteAccess(_vars.songData[5], _vars.songData[3].ToInt32BigEndian(code * 4));
                            }
                            else
                            {
                                offs.Offset -= code;
                            }
                        }
                    }
                    break;
                case 1:
                    SetRelease(channel);
                    loop = false;
                    break;
                case 2:
                    channel.loopSeqCount = p.Value; p.Offset++;
                    break;
                case 3:
                    channel.loopSeq2Count = p.Value; p.Offset++;
                    break;
                case 4:
                    code = p.Value; p.Offset++;
                    if (code != 0)
                    {
                        SetupInstrument(channel, code);
                    }
                    break;
                case 5:
                    channel.volumeScale = p.Value; p.Offset++;
                    break;
                case 6:
                    p.ToUInt16BigEndian();
                    channel.freqStep = p.Value; p.Offset++;
                    channel.freqInc = p.ToUInt32BigEndian(); p.Offset += 4;
                    channel.freqInit = 0;
                    break;
                case 7:
                    loop = false;
                    break;
            }
            return loop;
        }

        private void PlaySongSequence(Rjp1Channel channel)
        {
            var p = new ByteAccess(channel.sequenceData);
            --channel.loopSeqCur;
            if (channel.loopSeqCur == 0)
            {
                --channel.loopSeq2Cur;
                if (channel.loopSeq2Cur == 0)
                {
                    bool loop = true;
                    do
                    {
                        byte code = p.Value; p.Offset++;
                        if ((code & 0x80) != 0)
                        {
                            if (channel.isSfx)
                            {
                                loop = ExecuteSfxSequenceOp(channel, code, p);
                            }
                            else
                            {
                                loop = ExecuteSongSequenceOp(channel, code, p);
                            }
                        }
                        else
                        {
                            code >>= 1;
                            if (code < _periodsTable.Length)
                            {
                                SetupNote(channel, _periodsTable[code]);
                            }
                            loop = false;
                        }
                    } while (loop);
                    channel.sequenceData = p;
                    channel.loopSeq2Cur = channel.loopSeq2Count;
                }
                channel.loopSeqCur = channel.loopSeqCount;
            }
        }

        private void ModulateVolume(Rjp1Channel channel)
        {
            ModulateVolumeEnvelope(channel);
            ModulateVolumeWaveform(channel);
            SetVolume(channel);
        }

        private void ModulatePeriod(Rjp1Channel channel)
        {
            if (channel.modulatePeriodData != null)
            {
                uint per = channel.modulatePeriodIndex;
                int period = (channel.modulatePeriodData[(int)per] * channel.modulatePeriodInit) / 128;
                period = -period;
                if (period < 0)
                {
                    period /= 2;
                }
                channel.modulatePeriodNext = (short)(period + channel.modulatePeriodInit);
                ++per;
                if (per == channel.modulatePeriodLimit)
                {
                    per = (uint)(channel.modulatePeriodBase * 2);
                }
                channel.modulatePeriodIndex = per;
            }
            if (channel.freqStep != 0)
            {
                channel.freqInit += channel.freqInc;
                --channel.freqStep;
            }
            SetChannelPeriod(channel.index, (short)(channel.freqInit + channel.modulatePeriodNext));
        }

        private void SetupNote(Rjp1Channel channel, short period)
        {
            var note = channel.noteData;
            if (note != null)
            {
                channel.modulatePeriodInit = channel.modulatePeriodNext = period;
                channel.freqInit = 0;
                var e = new ByteAccess(_vars.songData[1], note.ToUInt16BigEndian(12));
                channel.envelopeData = e;
                channel.envelopeStart = (sbyte)e[1];
                channel.envelopeScale = (sbyte)(e[1] - e[0]);
                channel.envelopeEnd2 = (sbyte)e[2];
                channel.envelopeEnd1 = (sbyte)e[2];
                channel.envelopeMode = 4;
                channel.data = channel.waveData;
                channel.pos = note.ToUInt16BigEndian(16);
                channel.len = (ushort)(channel.pos + note.ToUInt16BigEndian(18));
                channel.setupNewNote = true;
            }
        }

        void SetupInstrument(Rjp1Channel channel, byte num)
        {
            if (channel.currentInstrument != num)
            {
                channel.currentInstrument = num;
                var p = new ByteAccess(_vars.songData[0], num * 32);
                channel.noteData = p;
                channel.repeatPos = p.ToUInt16BigEndian(20);
                channel.repeatLen = p.ToUInt16BigEndian(22);
                channel.volumeScale = p.ToUInt16BigEndian(14);
                channel.modulatePeriodBase = p.ToUInt16BigEndian(24);
                channel.modulatePeriodIndex = 0;
                channel.modulatePeriodLimit = (uint)(p.ToUInt16BigEndian(26) * 2);
                channel.modulateVolumeBase = p.ToUInt16BigEndian(28);
                channel.modulateVolumeIndex = 0;
                channel.modulateVolumeLimit = (uint)(p.ToUInt16BigEndian(30) * 2);
                channel.waveData = new ByteAccess(_vars.instData, p.ToInt32BigEndian());
                uint off = p.ToUInt32BigEndian(4);
                if (off != 0)
                {
                    channel.modulatePeriodData = new ByteAccess(_vars.instData, (int)off);
                }
                off = p.ToUInt32BigEndian(8);
                if (off != 0)
                {
                    channel.modulateVolumeData = new ByteAccess(_vars.instData, (int)off);
                }
            }
        }

        private void SetRelease(Rjp1Channel channel)
        {
            var e = new ByteAccess(channel.envelopeData);
            if (e != null)
            {
                channel.envelopeStart = 0;
                channel.envelopeScale = (sbyte)-channel.envelopeVolume;
                channel.envelopeEnd2 = (sbyte)e[5];
                channel.envelopeEnd1 = (sbyte)e[5];
                channel.envelopeMode = -1;
            }
        }

        private void ModulateVolumeEnvelope(Rjp1Channel channel)
        {
            if (channel.envelopeMode != 0)
            {
                short es = channel.envelopeScale;
                if (es != 0)
                {
                    sbyte m = channel.envelopeEnd1;
                    if (m == 0)
                    {
                        es = 0;
                    }
                    else
                    {
                        es *= m;
                        m = channel.envelopeEnd2;
                        if (m == 0)
                        {
                            es = 0;
                        }
                        else
                        {
                            es /= m;
                        }
                    }
                }
                channel.envelopeVolume = (sbyte)(channel.envelopeStart - es);
                --channel.envelopeEnd1;
                if (channel.envelopeEnd1 == -1)
                {
                    switch (channel.envelopeMode)
                    {
                        case 0:
                            break;
                        case 2:
                            SetSustain(channel);
                            break;
                        case 4:
                            SetDecay(channel);
                            break;
                        case -1:
                            SetSustain(channel);
                            break;
                        default:
                            throw new InvalidOperationException($"Unhandled envelope mode {channel.envelopeMode}");
                    }
                    return;
                }
            }
            channel.volume = channel.envelopeVolume;
        }

        void SetSustain(Rjp1Channel channel)
        {
            channel.envelopeMode = 0;
        }

        void SetDecay(Rjp1Channel channel)
        {
            if (channel.envelopeData != null)
            {
                var e = new ByteAccess(channel.envelopeData);
                channel.envelopeStart = (sbyte)e[3];
                channel.envelopeScale = (sbyte)(e[3] - e[1]);
                channel.envelopeEnd2 = (sbyte)e[4];
                channel.envelopeEnd1 = (sbyte)e[4];
                channel.envelopeMode = 2;
            }
        }

        private void ModulateVolumeWaveform(Rjp1Channel channel)
        {
            if (channel.modulateVolumeData != null)
            {
                uint i = channel.modulateVolumeIndex;
                channel.volume = (short)(channel.volume + channel.modulateVolumeData[(int)i] * channel.volume / 128);
                ++i;
                if (i == channel.modulateVolumeLimit)
                {
                    i = (uint)(channel.modulateVolumeBase * 2);
                }
                channel.modulateVolumeIndex = i;
            }
        }

        private void SetVolume(Rjp1Channel channel)
        {
            channel.volume = (short)((channel.volume * channel.volumeScale) / 64);
            channel.volume = (short)ScummHelper.Clip(channel.volume, 0, 64);
            SetChannelVolume(channel.index, (byte)channel.volume);
        }

        private void StopPaulaChannel(byte channel)
        {
            ClearVoice(channel);
        }

        private void SetupPaulaChannel(byte channel, ByteAccess waveData, ushort offset, ushort len, ushort repeatPos, ushort repeatLen)
        {
            if (waveData != null)
            {
                // TODO: SetChannelData(channel, waveData, waveData + repeatPos * 2, len * 2, repeatLen * 2, offset * 2);
            }
        }

        protected override void Interrupt()
        {
            for (int i = 0; i < 4; ++i)
            {
                _vars.currentChannel = (byte)i;
                PlayChannel(_channelsTable[i]);
            }
        }

        public static IAudioStream MakeRjp1Stream(Stream songData, Stream instrumentsData, int num, int rate = 44100, bool stereo = true)
        {
            using (Rjp1 stream = new Rjp1(rate, stereo))
            {
                if (stream.Load(songData, instrumentsData))
                {
                    if (num < 0)
                    {
                        stream.StartPattern(3, -num);
                    }
                    else
                    {
                        stream.StartSong(num);
                    }
                    return stream;
                }
            }
            return null;
        }
    }
}

