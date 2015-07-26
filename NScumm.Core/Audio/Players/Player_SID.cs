//
//  Player_SID.cs
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
using NScumm.Core.IO;
using NScumm.Core.Audio.SoftSynth;
using System.Collections.Generic;

namespace NScumm.Core.Audio
{
    enum VideoStandard
    {
        Pal,
        Ntsc
    }

    class Player_SID: IMusicEngine, IAudioStream
    {
        public int Rate { get { return _sampleRate; } }

        public bool IsStereo { get { return false; } }

        public bool IsEndOfData { get { return false; } }

        bool IAudioStream.IsEndOfStream { get { return IsEndOfData; } }

        public Player_SID(ScummEngine scumm, IMixer mixer, ISid sid)
        {
            _sid = sid;
            for (int i = 0; i < 7; ++i)
            {
                _soundQueue[i] = -1;
            }

            _mixer = mixer;
            _sampleRate = _mixer.OutputRate;
            _vm = scumm;

            // sound speed is slightly different on NTSC and PAL machines
            // as the SID clock depends on the frame rate.
            // ScummVM does not distinguish between NTSC and PAL targets
            // so we use the NTSC timing here as the music was composed for
            // NTSC systems (music on PAL systems is slower).
            _videoSystem = VideoStandard.Ntsc;

            resStatus[1] = 0;
            resStatus[2] = 0;

            InitSID();
            ResetSID();

            _soundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        public void SetMusicVolume(int vol)
        {
        }

        public void StartSound(int nr)
        {
            var data = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);

            // WORKAROUND:
            // sound[4] contains either a song prio or a music channel usage byte.
            // As music channel usage is always 0x07 for all music files and
            // prio 7 is never used in any sound file use this byte for auto-detection.
            bool isMusic = (data[4] == 0x07);

            lock (_mutex)
            {
                if (isMusic)
                {
                    InitMusic(nr);
                }
                else
                {
                    StopSound_intern(nr);
                    InitSound(nr);
                }
            }
        }

        public void StopSound(int nr)
        {
            if (nr == -1)
                return;

            lock (_mutex)
            {
                StopSound_intern(nr);
            }
        }

        public void StopAllSounds()
        {
            lock (_mutex)
            {
                ResetPlayerState();
            }
        }

        public int GetSoundStatus(int nr)
        {
            int result = 0;

            //Common::StackLock lock(_mutex);

            if (resID_song == nr && isMusicPlaying)
            {
                result = 1;
            }

            for (int i = 0; (i < 4) && (result == 0); ++i)
            {
                if (nr == _soundQueue[i] || nr == channelMap[i])
                {
                    result = 1;
                }
            }

            return result;
        }

        public int GetMusicTimer()
        {
            int result = _music_timer;
            _music_timer = 0;
            return result;
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            int samplesLeft = count;
            int offset = 0;
            lock (_mutex)
            {
                while (samplesLeft > 0)
                {
                    // update SID status after each frame
                    if (_cpuCyclesLeft <= 0)
                    {
                        Update();
                        _cpuCyclesLeft = timingProps[(int)_videoSystem].CyclesPerFrame;
                    }
                    // fetch samples
                    int sampleCount = _sid.UpdateClock(ref _cpuCyclesLeft, buffer, samplesLeft, offset);
                    samplesLeft -= sampleCount;
                    offset += sampleCount;
                }
            }

            return count;
        }

        public void Dispose()
        {
            _mixer.StopHandle(_soundHandle);
        }

        void IMusicEngine.SaveOrLoad(Serializer serializer)
        {
        }

        void Update()
        { // $481B
            if (initializing)
                return;

            if (_soundInQueue)
            {
                for (int i = 6; i >= 0; --i)
                {
                    if (_soundQueue[i] != -1)
                        ProcessSongData(i);
                }
                _soundInQueue = false;
            }

            // no sound
            if (busyChannelBits == 0)
                return;

            for (int i = 6; i >= 0; --i)
            {
                if ((busyChannelBits & BITMASK[i]) != 0)
                {
                    UpdateFreq(i);
                }
            }

            // seems to be used for background (prio=1?) sounds.
            // If a bg sound cannot be played because all SID
            // voices are used by higher priority sounds, the
            // bg sound's state is updated here so it will be at
            // the correct state when a voice is available again.
            if (swapPrepared)
            {
                SwapVars(0, 0);
                swapVarLoaded = true;
                UpdateFreq(0);
                SwapVars(0, 0);
                if (pulseWidthSwapped)
                {
                    SwapVars(4, 1);
                    UpdateFreq(4);
                    SwapVars(4, 1);
                }
                swapVarLoaded = false;
            }

            for (int i = 6; i >= 0; --i)
            {
                if ((busyChannelBits & BITMASK[i]) != 0)
                    SetSIDWaveCtrlReg(i);
            }

            if (isMusicPlaying)
            {
                HandleMusicBuffer();
            }

            return;
        }

        // channel: 0..6
        void UpdateFreq(int channel)
        {
            isVoiceChannel = (channel < 3);

            --freqDeltaCounter[channel];
            if (freqDeltaCounter[channel] < 0)
            {
                ReadSongChunk(channel);
            }
            else
            {
                freqReg[channel] += freqDelta[channel];
            }
            SetSIDFreqAS(channel);
        }

        int InitSound(int soundResID)
        { // $4D0A
            initializing = true;

            if (isMusicPlaying && (statusBits1A & 0x07) == 0x07)
            {
                initializing = false;
                return -2;
            }

            var songFilePtr = GetResource(soundResID);
            if (songFilePtr == null)
            {
                initializing = false;
                return 1;
            }

            var soundPrio = songFilePtr[4];
            // for (mostly but not always looped) background sounds
            if (soundPrio == 1)
            {
                bgSoundResID = (byte)soundResID;
                bgSoundActive = true;
            }

            var requestedChannels = 0;
            if ((songFilePtr[5] & 0x40) == 0)
            {
                ++requestedChannels;
                if ((songFilePtr[5] & 0x02) != 0)
                    ++requestedChannels;
                if ((songFilePtr[5] & 0x08) != 0)
                    ++requestedChannels;
            }

            bool filterNeeded = (songFilePtr[5] & 0x20) != 0;
            bool filterBlocked = (filterUsed && filterNeeded);
            if (filterBlocked || (freeChannelCount < requestedChannels))
            {
                FindLessPrioChannels(soundPrio);

                if ((freeChannelCount + chansWithLowerPrioCount < requestedChannels) ||
                    (filterBlocked && !actFilterHasLowerPrio))
                {
                    initializing = false;
                    return -1;
                }

                if (filterBlocked)
                {
                    if (soundPrio < chanPrio[3])
                    {
                        initializing = false;
                        return -1;
                    }

                    var l_resID = channelMap[3];
                    ReleaseResourceBySound(l_resID);
                }

                while ((freeChannelCount < requestedChannels) || (filterNeeded && filterUsed))
                {
                    FindLessPrioChannels(soundPrio);
                    if (minChanPrio >= soundPrio)
                    {
                        initializing = false;
                        return -1;
                    }

                    var l_resID = channelMap[minChanPrioIndex];
                    ReleaseResourceBySound(l_resID);
                }
            }

            int x;
            var soundByte5 = songFilePtr[5];
            if ((soundByte5 & 0x40) != 0)
                x = ReserveSoundFilter(soundPrio, (byte)soundResID);
            else
                x = ReserveSoundVoice(soundPrio, (byte)soundResID);

            var var4CF3 = x;
            int y = 6;
            if ((soundByte5 & 0x01) != 0)
            {
                x += 4;
                ReadVec6Data(x, ref y, songFilePtr, soundResID);
            }
            if ((soundByte5 & 0x02) != 0)
            {
                x = ReserveSoundVoice(soundPrio, (byte)soundResID);
                ReadVec6Data(x, ref y, songFilePtr, soundResID);
            }
            if ((soundByte5 & 0x04) != 0)
            {
                x += 4;
                ReadVec6Data(x, ref y, songFilePtr, soundResID);
            }
            if ((soundByte5 & 0x08) != 0)
            {
                x = ReserveSoundVoice(soundPrio, (byte)soundResID);
                ReadVec6Data(x, ref y, songFilePtr, soundResID);
            }
            if ((soundByte5 & 0x10) != 0)
            {
                x += 4;
                ReadVec6Data(x, ref y, songFilePtr, soundResID);
            }
            if ((soundByte5 & 0x20) != 0)
            {
                x = ReserveSoundFilter(soundPrio, (byte)soundResID);
                ReadVec6Data(x, ref y, songFilePtr, soundResID);
            }

            //vec5[var4CF3] = songFilePtr;
            vec6[var4CF3] = (ushort)y;
            _soundQueue[var4CF3] = soundResID;

            initializing = false;
            _soundInQueue = true;

            return soundResID;
        }

        int ReserveSoundVoice(byte value, byte chanResIndex)
        { // $4EB8
            for (int i = 2; i >= 0; --i)
            {
                if ((usedChannelBits & BITMASK[i]) == 0)
                {
                    ReserveChannel(i, value, chanResIndex);
                    return i;
                }
            }
            return 0;
        }

        void ReleaseResourceBySound(int resID)
        { // $5088
            var481A = 1;
            ReleaseResource(resID);
        }

        void ReadVec6Data(int x, ref int offset, byte[] songFilePtr, int chanResID)
        { // $4E99
            //vec5[x] = songFilePtr;
            vec6[x] = songFilePtr[offset];
            offset += 2;
            _soundQueue[x] = chanResID;
        }

        int ReserveSoundFilter(byte value, byte chanResIndex)
        { // $4ED0
            int channel = 3;
            ReserveChannel(channel, value, chanResIndex);
            return channel;
        }

        void FindLessPrioChannels(byte soundPrio)
        { // $4ED8
            minChanPrio = 127;

            chansWithLowerPrioCount = 0;
            for (int i = 2; i >= 0; --i)
            {
                if ((usedChannelBits & BITMASK[i]) != 0)
                {
                    if (chanPrio[i] < soundPrio)
                        ++chansWithLowerPrioCount;
                    if (chanPrio[i] < minChanPrio)
                    {
                        minChanPrio = chanPrio[i];
                        minChanPrioIndex = (byte)i;
                    }
                }
            }

            if (chansWithLowerPrioCount == 0)
                return;

            if (soundPrio >= chanPrio[3])
            {
                actFilterHasLowerPrio = true;
            }
            else
            {
                /* TODO: is this really a no-op?
        if (minChanPrioIndex < chanPrio[3])
            minChanPrioIndex = minChanPrioIndex;
        */

                actFilterHasLowerPrio = false;
            }
        }

        void StopSound_intern(int soundResID)
        { // $5093
            for (int i = 0; i < 7; ++i)
            {
                if (soundResID == _soundQueue[i])
                {
                    _soundQueue[i] = -1;
                }
            }
            var481A = -1;
            ReleaseResource(soundResID);
        }

        void InitMusic(int songResIndex)
        { // $7de6
            UnlockResource(resID_song);

            resID_song = songResIndex;
            _music = GetResource(resID_song);
            if (_music == null)
            {
                return;
            }

            // song base address
            var songFileDataPtr = _music;
            actSongFileData = _music;

            initializing = true;
            _soundInQueue = false;
            isMusicPlaying = false;

            UnlockCodeLocation();
            ResetPlayerState();

            LockResource(resID_song);
            BuildStepTbl(songFileDataPtr[5]);

            // fetch sound
            songChannelBits = songFileDataPtr[4];
            for (int i = 2; i >= 0; --i)
            {
                if ((songChannelBits & BITMASK[i]) != 0)
                {
                    Func_7eae(i, songFileDataPtr);
                }
            }

            isMusicPlaying = true;
            LockCodeLocation();

            SIDReg23 &= 0xf0;
            SID_Write(23, SIDReg23);

            HandleMusicBuffer();

            initializing = false;
            _soundInQueue = true;
        }

        void HandleMusicBuffer()
        { // $33cd
            int channel = 2;
            while (channel >= 0)
            {
                if ((statusBits1A & BITMASK[channel]) == 0 ||
                    (busyChannelBits & BITMASK[channel]) != 0)
                {
                    --channel;
                    continue;
                }

                if (SetupSongFileData() == 1)
                    return;

                var l_chanFileDataPtr = chanFileData[channel];
                var l_chanFileDataOffset = chanFileDataPtr[channel];

                var l_freq = 0;
                bool l_keepFreq = false;

                int y = 0;
                var curByte = l_chanFileDataPtr[l_chanFileDataOffset + y++];

                // freq or 0/0xFF
                if (curByte == 0)
                {
                    Func_3674(channel);
                    if (!isMusicPlaying)
                        return;
                    continue;
                }
                else if (curByte == 0xFF)
                {
                    l_keepFreq = true;
                }
                else
                {
                    l_freq = FREQ_TBL[curByte];
                }

                var local1 = 0;
                curByte = l_chanFileDataPtr[l_chanFileDataOffset + y++];
                bool isLastCmdByte = (curByte & 0x80) != 0;
                var curStepSum = stepTbl[curByte & 0x7f];

                for (int i = 0; !isLastCmdByte && (i < 2); ++i)
                {
                    curByte = l_chanFileDataPtr[l_chanFileDataOffset + y++];
                    isLastCmdByte = (curByte & 0x80) != 0;
                    if ((curByte & 0x40) != 0)
                    {
                        // note: bit used in zak theme (95) only (not used/handled in MM)
                        _music_timer = curByte & 0x3f;
                    }
                    else
                    {
                        local1 = curByte & 0x3f;
                    }
                }

                chanFileDataPtr[channel] += y;
                chanDataOffset[channel] += (ushort)y;

                var l_chanBuf = GetResource(RES_ID_CHANNEL[channel]);

                if (local1 != 0)
                {
                    // TODO: signed or unsigned?
                    var offset = BitConverter.ToUInt16(actSongFileData, local1 * 2 + 12);
                    l_chanFileDataPtr = actSongFileData;
                    l_chanFileDataOffset = offset;

                    // next five bytes: freqDelta, attack, sustain and phase bit
                    for (int i = 0; i < 5; ++i)
                    {
                        l_chanBuf[15 + i] = l_chanFileDataPtr[l_chanFileDataOffset + i];
                    }
                    phaseBit[channel] = l_chanFileDataPtr[l_chanFileDataOffset + 4];

                    for (int i = 0; i < 17; ++i)
                    {
                        l_chanBuf[25 + i] = l_chanFileDataPtr[l_chanFileDataOffset + 5 + i];
                    }
                }

                if (l_keepFreq)
                {
                    if (!releasePhase[channel])
                    {
                        l_chanBuf[10] &= 0xfe; // release phase
                    }
                    releasePhase[channel] = true;
                }
                else
                {
                    if (releasePhase[channel])
                    {
                        l_chanBuf[19] = phaseBit[channel];
                        l_chanBuf[10] |= 0x01; // attack phase
                    }
                    l_chanBuf[11] = (byte)LowByte(l_freq);
                    l_chanBuf[12] = (byte)HiByte(l_freq);
                    releasePhase[channel] = false;
                }

                // set counter value for frequency update (freqDeltaCounter)
                l_chanBuf[13] = (byte)LowByte(curStepSum);
                l_chanBuf[14] = (byte)HiByte(curStepSum);

                _soundQueue[channel] = RES_ID_CHANNEL[channel];
                ProcessSongData(channel);
                _soundQueue[channel + 4] = RES_ID_CHANNEL[channel];
                ProcessSongData(channel + 4);
                --channel;
            }
        }

        // channel: 0..6
        void ProcessSongData(int channel)
        { // $4939
            // always: _soundQueue[channel] != -1
            // -> channelMap[channel] != -1
            channelMap[channel] = _soundQueue[channel];
            _soundQueue[channel] = -1;
            songPosUpdateCounter[channel] = 0;

            isVoiceChannel = (channel < 3);

            songFileOrChanBufOffset[channel] = vec6[channel];

            SetupSongPtr(channel);

            //vec5[channel] = songFileOrChanBufData; // not used

            if (songFileOrChanBufData == null)
            { // chanBuf (4C1C)
                /*
        // TODO: do we need this?
                LOBYTE_(vec20[channel]) = 0;
                LOBYTE_(songPosPtr[channel]) = LOBYTE_(songFileOrChanBufOffset[channel]);
                */
                ReleaseResourceUnk(channel);
                return;
            }

            vec20[channel] = songFileOrChanBufData; // chanBuf (4C1C)
            songPosPtr[channel] = songFileOrChanBufOffset[channel]; // chanBuf (4C1C)
            var ptr1 = songPosPtr[channel];

            int y = -1;
            if (channel < 4)
            {
                ++y;
                if (channel == 3)
                {
                    ReadSetSIDFilterAndProps(ref y, songFileOrChanBufData, ptr1);
                }
                else if ((statusBits1A & BITMASK[channel]) != 0)
                {
                    ++y;
                }
                else
                { // channel = 0/1/2
                    waveCtrlReg[channel] = songFileOrChanBufData[ptr1 + y];

                    ++y;
                    if ((songFileOrChanBufData[ptr1 + y] & 0x0f) != 0)
                    {
                        // filter on for voice channel
                        SIDReg23 |= BITMASK[channel];
                    }
                    else
                    {
                        // filter off for voice channel
                        SIDReg23 &= BITMASK_INV[channel];
                    }
                    SID_Write(23, SIDReg23);
                }
            }

            SaveSongPos(y, channel);
            busyChannelBits |= BITMASK[channel];
            ReadSongChunk(channel);
        }

        // channel: 0..6
        int SetupSongPtr(int channel)
        { // $4C1C
            //resID:5,4,3,songid
            int resID = channelMap[channel];

            // TODO: when does this happen, only if resID == 0?
            if (GetResource(resID) == null)
            {
                ReleaseResourceUnk(resID);
                if (resID == bgSoundResID)
                {
                    bgSoundResID = 0;
                    bgSoundActive = false;
                    swapPrepared = false;
                    pulseWidthSwapped = false;
                }
                return 1;
            }

            songFileOrChanBufData = GetResource(resID); // chanBuf (4C1C)
            if (songFileOrChanBufData == vec20[channel])
            {
                return 0;
            }
            else
            {
                vec20[channel] = songFileOrChanBufData;
                songPosPtr[channel] = songFileOrChanBufOffset[channel];
                return -1;
            }
        }

        void ReleaseResourceUnk(int resIndex)
        { // $50A4
            var481A = -1;
            ReleaseResource(resIndex);
        }

        void ReleaseResource(int resIndex)
        { // $5031
            ReleaseResChannels(resIndex);
            if (resIndex == bgSoundResID && var481A == -1)
            {
                SafeUnlockResource(resIndex);

                bgSoundResID = 0;
                bgSoundActive = false;
                swapPrepared = false;
                pulseWidthSwapped = false;

                ResetSwapVars();
            }
        }

        void ReleaseResChannels(int resIndex)
        { // $5070
            for (int i = 3; i >= 0; --i)
            {
                if (resIndex == channelMap[i])
                {
                    ReleaseChannel(i);
                }
            }
        }

        void ReadSongChunk(int channel)
        { // $4a6b
            while (true)
            {
                if (SetupSongPtr(channel) == 1)
                {
                    // do something with code resource
                    ReleaseResourceUnk(1);
                    return;
                }

                var ptr1 = songPosPtr[channel];

                //curChannelActive = true;

                var l_cmdByte = songFileOrChanBufData[ptr1];
                if (l_cmdByte == 0)
                {
                    //curChannelActive = false;
                    songPosUpdateCounter[channel] = 0;

                    var481A = -1;
                    ReleaseChannel(channel);
                    return;
                }

                //vec19[channel] = l_cmdByte;

                // attack (1) / release (0) phase
                if (isVoiceChannel)
                {
                    if (GetBit(l_cmdByte, 0) != 0)
                        waveCtrlReg[channel] |= 0x01; // start attack phase
                    else
                        waveCtrlReg[channel] &= 0xfe; // start release phase
                }

                // channel finished bit
                if (GetBit(l_cmdByte, 1) != 0)
                {
                    var481A = -1;
                    ReleaseChannel(channel);
                    return;
                }

                int y = 0;

                // frequency
                if (GetBit(l_cmdByte, 2) != 0)
                {
                    y += 2;
                    freqReg[channel] = BitConverter.ToUInt16(songFileOrChanBufData, ptr1 + y - 1);
                    if (GetBit(l_cmdByte, 6) == 0)
                    {
                        y += 2;
                        freqDeltaCounter[channel] = BitConverter.ToUInt16(songFileOrChanBufData, ptr1 + y - 1);
                        y += 2;
                        freqDelta[channel] = BitConverter.ToUInt16(songFileOrChanBufData, ptr1 + y - 1);
                    }
                    else
                    {
                        ResetFreqDelta(channel);
                    }
                }
                else
                {
                    ResetFreqDelta(channel);
                }

                // attack / release
                if (isVoiceChannel && (GetBit(l_cmdByte, 3) != 0))
                {
                    // start release phase
                    waveCtrlReg[channel] &= 0xfe;
                    SetSIDWaveCtrlReg(channel);

                    ++y;
                    attackReg[channel] = songFileOrChanBufData[ptr1 + y];
                    ++y;
                    sustainReg[channel] = songFileOrChanBufData[ptr1 + y];

                    // set attack (1) or release (0) phase
                    waveCtrlReg[channel] |= (byte)(l_cmdByte & 0x01);
                }

                if (GetBit(l_cmdByte, 4) != 0)
                {
                    ++y;
                    var curByte = songFileOrChanBufData[ptr1 + y];

                    // pulse width
                    if (isVoiceChannel && (GetBit(curByte, 0) != 0))
                    {
                        int reg = SID_REG_OFFSET[channel + 4];

                        y += 2;
                        SID_Write(reg, songFileOrChanBufData[ptr1 + y - 1]);
                        SID_Write(reg + 1, songFileOrChanBufData[ptr1 + y]);
                    }

                    if (GetBit(curByte, 1) != 0)
                    {
                        ++y;
                        ReadSetSIDFilterAndProps(ref y, songFileOrChanBufData, ptr1);

                        y += 2;
                        SID_Write(21, songFileOrChanBufData[ptr1 + y - 1]);
                        SID_Write(22, songFileOrChanBufData[ptr1 + y]);
                    }

                    if (GetBit(curByte, 2) != 0)
                    {
                        ResetFreqDelta(channel);

                        y += 2;
                        freqDeltaCounter[channel] = BitConverter.ToUInt16(songFileOrChanBufData, ptr1 + y - 1);
                    }
                }

                // set waveform (?)
                if (GetBit(l_cmdByte, 5) != 0)
                {
                    ++y;
                    waveCtrlReg[channel] = (byte)((waveCtrlReg[channel] & 0x0f) | songFileOrChanBufData[ptr1 + y]);
                }

                // song position
                if (GetBit(l_cmdByte, 7) != 0)
                {
                    if (songPosUpdateCounter[channel] == 1)
                    {
                        y += 2;
                        --songPosUpdateCounter[channel];
                        SaveSongPos(y, channel);
                    }
                    else
                    {
                        // looping / skipping / ...
                        ++y;
                        songPosPtr[channel] -= songFileOrChanBufData[ptr1 + y];
                        songFileOrChanBufOffset[channel] -= songFileOrChanBufData[ptr1 + y];

                        ++y;
                        if (songPosUpdateCounter[channel] == 0)
                        {
                            songPosUpdateCounter[channel] = songFileOrChanBufData[ptr1 + y];
                        }
                        else
                        {
                            --songPosUpdateCounter[channel];
                        }
                    }
                }
                else
                {
                    SaveSongPos(y, channel);
                    return;
                }
            }
        }

        void ResetFreqDelta(int channel)
        {
            freqDeltaCounter[channel] = 0;
            freqDelta[channel] = 0;
        }

        void ReadSetSIDFilterAndProps(ref int offset, byte[] dataPtr, int offsetData)
        {  // $49e7
            SIDReg23 |= dataPtr[offset + offsetData];
            SID_Write(23, SIDReg23);
            ++offset;
            SIDReg24 = dataPtr[offset + offsetData];
            SID_Write(24, SIDReg24);
        }

        void SaveSongPos(int y, int channel)
        {
            ++y;
            songPosPtr[channel] += y;
            songFileOrChanBufOffset[channel] += (ushort)y;
        }

        int SetupSongFileData()
        { // $36cb
            // no song playing
            // TODO: remove (never NULL)
            if (_music == null)
            {
                for (int i = 2; i >= 0; --i)
                {
                    if ((songChannelBits & BITMASK[i]) != 0)
                    {
                        Func_3674(i);
                    }
                }
                return 1;
            }

            // no new song
            songFileOrChanBufData = _music;
            if (_music == actSongFileData)
            {
                return 0;
            }

            // new song selected
            actSongFileData = _music;
            for (int i = 0; i < 3; ++i)
            {
                chanFileData[i] = _music;
                chanFileDataPtr[i] = chanDataOffset[i];
            }

            return -1;
        }

        //x:0..2
        void Func_3674(int channel)
        { // $3674
            statusBits1B &= BITMASK_INV[channel];
            if (statusBits1B == 0)
            {
                isMusicPlaying = false;
                UnlockCodeLocation();
                SafeUnlockResource(resID_song);
                for (int i = 0; i < 3; ++i)
                {
                    SafeUnlockResource(RES_ID_CHANNEL[i]);
                }
            }

            chanPrio[channel] = 2;

            statusBits1A &= BITMASK_INV[channel];
            phaseBit[channel] = 0;

            Func_4F45(channel);
        }

        // ignore: no effect
        void LockCodeLocation()
        { // $514f
            resStatus[1] |= 0x01;
            resStatus[2] |= 0x01;
        }

        // params:
        //   channel: channel 0..2
        void Func_7eae(int channel, byte[] songFileDataPtr)
        {
            int pos = SONG_CHANNEL_OFFSET[channel];
            chanDataOffset[channel] = BitConverter.ToUInt16(songFileDataPtr, pos);
            chanFileData[channel] = songFileDataPtr;
            chanFileDataPtr[channel] = chanDataOffset[channel];

            //vec5[channel+4] = vec5[channel] = CHANNEL_BUFFER_ADDR[RES_ID_CHANNEL[channel]]; // not used
            vec6[channel + 4] = 0x0019;
            vec6[channel] = 0x0008;

            Func_819b(channel);

            waveCtrlReg[channel] = 0;
        }

        void Func_819b(int channel)
        {
            ReserveChannel(channel, 127, RES_ID_CHANNEL[channel]);

            statusBits1B |= BITMASK[channel];
            statusBits1A |= BITMASK[channel];
        }

        void ReserveChannel(int channel, byte prioValue, int chanResIndex)
        { // $4ffe
            if (channel == 3)
            {
                filterUsed = true;
            }
            else if (channel < 3)
            {
                usedChannelBits |= BITMASK[channel];
                CountFreeChannels();
            }

            chanPrio[channel] = prioValue;
            LockResource(chanResIndex);
        }

        void BuildStepTbl(int step)
        { // $82B4
            stepTbl[0] = 0;
            stepTbl[1] = (ushort)(step - 2);
            for (int i = 2; i < 33; ++i)
            {
                stepTbl[i] = (ushort)(stepTbl[i - 1] + step);
            }
        }

        // ignore: no effect
        // resIndex: 3,4,5 or 58
        void LockResource(int resIndex)
        { // $4ff4
            if (!isMusicPlaying)
            {
                if (!resStatus.ContainsKey(resIndex))
                    resStatus[resIndex] = 0;

                ++resStatus[resIndex];
            }
        }

        byte[] GetResource(int resID)
        {
            switch (resID)
            {
                case 0:
                    return null;
                case 3:
                case 4:
                case 5:
                    return chanBuffer[resID - 3];
                default:
                    return _vm.ResourceManager.GetSound(_vm.Sound.MusicType, resID);
            }
        }

        void InitSID()
        {
            _sid.SetSamplingParameters(
                timingProps[(int)_videoSystem].ClockFreq,
                _sampleRate);
            _sid.EnableFilter = true;

            _sid.Reset();
            // Synchronize the waveform generators (must occur after reset)
            _sid.Write(4, 0x08);
            _sid.Write(11, 0x08);
            _sid.Write(18, 0x08);
            _sid.Write(4, 0x00);
            _sid.Write(11, 0x00);
            _sid.Write(18, 0x00);
        }

        void ResetSID()
        { 
            // $48D8
            SIDReg24 = 0x0f;

            SID_Write(4, 0);
            SID_Write(11, 0);
            SID_Write(18, 0);
            SID_Write(23, 0);
            SID_Write(21, 0);
            SID_Write(22, 0);
            SID_Write(24, SIDReg24);

            ResetPlayerState();
        }

        void ResetPlayerState()
        { // $48f7
            for (int i = 6; i >= 0; --i)
                ReleaseChannel(i);

            isMusicPlaying = false;
            UnlockCodeLocation(); // does nothing
            statusBits1B = 0;
            statusBits1A = 0;
            freeChannelCount = 3;
            swapPrepared = false;
            filterSwapped = false;
            pulseWidthSwapped = false;
            //var5163 = 0;
        }

        // ignore: no effect
        void UnlockCodeLocation()
        { // $513e
            resStatus[1] &= 0x80;
            resStatus[2] &= 0x80;
        }

        // a: 0..6
        void ReleaseChannel(int channel)
        {
            StopChannel(channel);
            if (channel >= 4)
            {
                return;
            }
            if (channel < 3)
            {
                SIDReg23Stuff = SIDReg23;
                ClearSIDWaveform(channel);
            }
            Func_4F45(channel);
            if (channel >= 3)
            {
                return;
            }
            if ((SIDReg23 != SIDReg23Stuff) &&
                (SIDReg23 & 0x07) == 0)
            {
                if (filterUsed)
                {
                    Func_4F45(3);
                    StopChannel(3);
                }
            }

            StopChannel(channel + 4);
        }

        void Func_4F45(int channel)
        { // $4F45
            if (swapVarLoaded)
            {
                if (channel == 0)
                {
                    swapPrepared = false;
                    ResetSwapVars();
                }
                pulseWidthSwapped = false;
            }
            else
            {
                if (channel == 3)
                {
                    filterUsed = false;
                }

                if (chanPrio[channel] == 1)
                {
                    if (var481A == 1)
                        PrepareSwapVars(channel);
                    else if (channel < 3)
                        ClearSIDWaveform(channel);
                }
                else if (channel < 3 && bgSoundActive && swapPrepared &&
                         !(filterSwapped && filterUsed))
                {
                    busyChannelBits |= BITMASK[channel];
                    UseSwapVars(channel);
                    waveCtrlReg[channel] |= 0x01;
                    SetSIDWaveCtrlReg(channel);

                    SafeUnlockResource(channelMap[channel]);
                    return;
                }

                chanPrio[channel] = 0;
                usedChannelBits &= BITMASK_INV[channel];
                CountFreeChannels();
            }

            int resIndex = channelMap[channel];
            channelMap[channel] = 0;
            SafeUnlockResource(resIndex);
        }

        void CountFreeChannels()
        { // $4f26
            freeChannelCount = 0;
            for (int i = 0; i < 3; ++i)
            {
                if (GetBit(usedChannelBits, i) == 0)
                    ++freeChannelCount;
            }
        }

        // chanResIndex: 3,4,5 or 58
        void SafeUnlockResource(int resIndex)
        { // $4FEA
            if (!isMusicPlaying)
            {
                UnlockResource(resIndex);
            }
        }

        // ignore: no effect
        // chanResIndex: 3,4,5 or 58
        void UnlockResource(int chanResIndex)
        { // $4CDA
            if (resStatus.ContainsKey(chanResIndex) && (resStatus[chanResIndex] & 0x7F) != 0)
                --resStatus[chanResIndex];
        }

        void UseSwapVars(int channel)
        { // $5342
            if (channel >= 3)
                return;

            SwapVars(channel, 0);
            SetSIDFreqAS(channel);
            if (pulseWidthSwapped)
            {
                SwapVars(channel + 4, 1);
                SetSIDFreqAS(channel + 4);
            }
            if (filterSwapped)
            {
                SwapVars(3, 2);

                // resonating filter freq. or voice-to-filter mapping?
                SIDReg23 = (byte)((SIDReg23Stuff & 0xf0) | BITMASK[channel]);
                SID_Write(23, SIDReg23);

                // filter props
                SIDReg24 = (byte)((SIDReg24 & 0x0f) | SIDReg24_HiNibble);
                SID_Write(24, SIDReg24);

                // filter freq.
                SID_Write(21, (byte)LowByte(freqReg[3]));
                SID_Write(22, (byte)HiByte(freqReg[3]));
            }
            else
            {
                SIDReg23 = (byte)(SIDReg23Stuff & BITMASK_INV[channel]);
                SID_Write(23, SIDReg23);
            }

            swapPrepared = false;
            pulseWidthSwapped = false;
            keepSwapVars = false;
            SIDReg24_HiNibble = 0;
            filterSwapped = false;
        }

        /// <summary>
        /// Sets frequency, attack and sustain register.
        /// </summary>
        /// <param name="channel">Channel.</param>
        void SetSIDFreqAS(int channel)
        { // $4be6
            if (swapVarLoaded)
                return;
            int reg = SID_REG_OFFSET[channel];
            SID_Write(reg, (byte)LowByte(freqReg[channel]));   // freq/pulseWidth voice 1/2/3
            SID_Write(reg + 1, (byte)HiByte(freqReg[channel]));
            if (channel < 3)
            {
                SID_Write(reg + 5, attackReg[channel]); // attack
                SID_Write(reg + 6, sustainReg[channel]); // sustain
            }
        }

        void PrepareSwapVars(int channel)
        { // $52E5
            if (channel >= 4)
                return;

            if (channel < 3)
            {
                if (!keepSwapVars)
                {
                    ResetSwapVars();
                }
                SwapVars(channel, 0);
                if ((busyChannelBits & BITMASK[channel + 4]) != 0)
                {
                    SwapVars(channel + 4, 1);
                    pulseWidthSwapped = true;
                }
            }
            else if (channel == 3)
            {
                SIDReg24_HiNibble = (byte)(SIDReg24 & 0x70);
                ResetSwapVars();
                keepSwapVars = true;
                SwapVars(3, 2);
                filterSwapped = true;
            }
            swapPrepared = true;
        }

        // channel: 0..6, swapIndex: 0..2
        void SwapVars(int channel, int swapIndex)
        { // $51a5
            if (channel < 3)
            {
                ScummHelper.Swap(ref attackReg[channel], ref swapAttack[swapIndex]);
                ScummHelper.Swap(ref sustainReg[channel], ref swapSustain[swapIndex]);
            }
            //SWAP(vec5[channel],  swapVec5[swapIndex]);  // not used
            //SWAP(vec19[channel], swapVec19[swapIndex]); // not used

            ScummHelper.Swap(ref chanPrio[channel], ref swapSongPrio[swapIndex]);
            ScummHelper.Swap(ref channelMap[channel], ref swapVec479C[swapIndex]);
            ScummHelper.Swap(ref songPosUpdateCounter[channel], ref swapSongPosUpdateCounter[swapIndex]);
            ScummHelper.Swap(ref waveCtrlReg[channel], ref swapWaveCtrlReg[swapIndex]);
            ScummHelper.Swap(ref songPosPtr[channel], ref swapSongPosPtr[swapIndex]);
            ScummHelper.Swap(ref freqReg[channel], ref swapFreqReg[swapIndex]);
            ScummHelper.Swap(ref freqDeltaCounter[channel], ref swapVec11[swapIndex]);
            ScummHelper.Swap(ref freqDelta[channel], ref swapVec10[swapIndex]);
            ScummHelper.Swap(ref vec20[channel], ref swapVec20[swapIndex]);
            ScummHelper.Swap(ref songFileOrChanBufOffset[channel], ref swapVec8[swapIndex]);
        }

        void ResetSwapVars()
        { // $52d0
            for (int i = 0; i < 2; ++i)
            {
                swapAttack[i] = 0;
                swapSustain[i] = 0;
            }
            for (int i = 0; i < 3; ++i)
            {
//                swapVec5[i] = 0;
                swapSongPrio[i] = 0;
                swapVec479C[i] = 0;
                swapVec19[i] = 0;
                swapSongPosUpdateCounter[i] = 0;
                swapWaveCtrlReg[i] = 0;
                swapSongPosPtr[i] = 0;
                swapFreqReg[i] = 0;
                swapVec11[i] = 0;
                swapVec10[i] = 0;
                swapVec20[i] = null;
                swapVec8[i] = 0;
            }
        }

        void ClearSIDWaveform(int channel)
        {
            if (!isMusicPlaying && var481A == -1)
            {
                waveCtrlReg[channel] &= 0x0e;
                SetSIDWaveCtrlReg(channel);
            }
        }

        void SetSIDWaveCtrlReg(int channel)
        { // $4C0D
            if (channel < 3)
            {
                int reg = SID_REG_OFFSET[channel];
                SID_Write(reg + 4, waveCtrlReg[channel]);
            }
        }

        void StopChannel(int channel)
        {
            songPosUpdateCounter[channel] = 0;
            // clear "channel" bit
            busyChannelBits &= BITMASK_INV[channel];
            if (channel >= 4)
            {
                // pulsewidth = 0
                channelMap[channel] = 0;
            }
        }

        void SID_Write(int reg, byte data)
        {
            _sid.Write(reg, data);
        }

        static int GetBit(int var, int pos)
        {
            return ((var) & (1 << (pos)));
        }

        static int LowByte(int a)
        {
            return ((a) & 0xFF);
        }

        static int HiByte(int a)
        {
            return (((a) >> 8) & 0xFF);
        }

        // number of cpu cycles until next frame update
        int _cpuCyclesLeft;
        ISid _sid;
        ScummEngine _vm;
        IMixer _mixer;
        SoundHandle _soundHandle;
        readonly int _sampleRate;
        object _mutex = new object();
        VideoStandard _videoSystem;

        int _music_timer;
        byte[] _music;

        int resID_song;

        // statusBits1A/1B are always equal
        byte statusBits1A;
        byte statusBits1B;

        byte busyChannelBits;

        byte SIDReg23;
        byte SIDReg23Stuff;
        byte SIDReg24;

        byte[][] chanFileData = new byte[3][];
        int[] chanFileDataPtr = new int[3];
        ushort[] chanDataOffset = new ushort[3];
        int[] songPosPtr = new int[7];

        // 0..2: freq value voice1/2/3
        // 3:    filter freq
        // 4..6: pulse width
        ushort[] freqReg = new ushort[7];

        // start offset[i] for songFileOrChanBufData to obtain songPosPtr[i]
        //  vec6[0..2] = 0x0008;
        //  vec6[4..6] = 0x0019;
        ushort[] vec6 = new ushort[7];

        // current offset[i] for songFileOrChanBufData to obtain songPosPtr[i] (starts with vec6[i], increased later)
        ushort[] songFileOrChanBufOffset = new ushort[7];

        ushort[] freqDelta = new ushort[7];
        int[] freqDeltaCounter = new int[7];
        int[] swapSongPosPtr = new int[3];

        ushort[] swapVec8 = new ushort[3];
        ushort[] swapVec10 = new ushort[3];
        ushort[] swapFreqReg = new ushort[3];
        int[] swapVec11 = new int[3];

        byte[][] vec20 = new byte[7][];

        byte[][] swapVec20 = new byte[3][];

        // resource status (never read)
        // bit7: some flag
        // bit6..0: counter (use-count?), maybe just bit0 as flag (used/unused?)
        Dictionary<int,byte> resStatus = new Dictionary<int, byte>();

        byte[] songFileOrChanBufData;
        byte[] actSongFileData;

        ushort[] stepTbl = new ushort[33];

        bool initializing;
        bool _soundInQueue;
        bool isVoiceChannel;

        bool isMusicPlaying;
        bool swapVarLoaded;
        bool bgSoundActive;
        bool filterUsed;

        byte bgSoundResID;
        byte freeChannelCount;

        // seems to be used for managing the three voices
        // bit[0..2]: 0 -> unused, 1 -> already in use
        byte usedChannelBits;
        byte[] attackReg = new byte[3];
        byte[] sustainReg = new byte[3];

        // -1/0/1
        int var481A;

        // bit-array: 00000cba
        // a/b/c: channel1/2/3
        byte songChannelBits;

        bool pulseWidthSwapped;
        bool swapPrepared;

        // never read
        //uint8 var5163;

        bool filterSwapped;
        byte SIDReg24_HiNibble;
        bool keepSwapVars;

        byte[] phaseBit = new byte[3];
        bool[] releasePhase = new bool[3];

        // values: a resID or -1
        // resIDs: 3, 4, 5 or song-number
        int[] _soundQueue = new int[7];

        // values: a resID or 0
        // resIDs: 3, 4, 5 or song-number
        int[] channelMap = new int[7];

        byte[] songPosUpdateCounter = new byte[7];

        // priortity of channel contents
        // MM:  1: lowest .. 120: highest (1,2,A,64,6E,73,78)
        // Zak: -???: lowest .. 120: highest (5,32,64,65,66,6E,78, A5,A6,AF,D7)
        byte[] chanPrio = new byte[7];

        // only [0..2] used?
        byte[] waveCtrlReg = new byte[7];

        byte[] swapAttack = new byte[2];
        byte[] swapSustain = new byte[2];
        byte[] swapSongPrio = new byte[3];
        int[] swapVec479C = new int[3];
        byte[] swapVec19 = new byte[3];
        byte[] swapSongPosUpdateCounter = new byte[3];
        byte[] swapWaveCtrlReg = new byte[3];

        bool actFilterHasLowerPrio;
        byte chansWithLowerPrioCount;
        byte minChanPrio;
        byte minChanPrioIndex;

        struct TimingProps
        {
            public double ClockFreq;
            public int CyclesPerFrame;
        }

        static readonly TimingProps[] timingProps =
            {
                new TimingProps { ClockFreq = 17734472.0 / 18, CyclesPerFrame = 312 * 63 }, // PAL:  312*63 cycles/frame @  985248 Hz (~50Hz)
                new TimingProps { ClockFreq = 14318180.0 / 14, CyclesPerFrame = 263 * 65 }  // NTSC: 263*65 cycles/frame @ 1022727 Hz (~60Hz)
            };

        byte[][] chanBuffer = new byte[3][]
        {
            new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x7f, 0x01, 0x19, 0x00,
                0x00, 0x00, 0x2d, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xf0, 0x40, 0x10, 0x04, 0x00, 0x00,
                0x00, 0x04, 0x27, 0x03, 0xff, 0xff, 0x01, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00
            },
            new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x7f, 0x01, 0x19, 0x00,
                0x00, 0x00, 0x2d, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xf0, 0x20, 0x10, 0x04, 0x00, 0x00,
                0x00, 0x04, 0x27, 0x03, 0xff, 0xff, 0x02, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00
            },
            new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x7f, 0x01, 0x19, 0x00,
                0x00, 0x00, 0x2d, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xf0, 0x20, 0x10, 0x04, 0x00, 0x00,
                0x00, 0x04, 0x27, 0x03, 0xff, 0xff, 0x02, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00
            }
        };

        static readonly byte[] BITMASK =
            {
                0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40
            };
        static readonly byte[] BITMASK_INV =
            {
                0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xDF, 0xBF
            };

        static readonly int[] SID_REG_OFFSET =
            {
                0, 7, 14, 21, 2, 9, 16
            };

        // NTSC frequency table (also used for PAL versions).
        // FREQ_TBL[i] = tone_freq[i] * 2^24 / clockFreq
        static readonly ushort[] FREQ_TBL =
            {
                0x0000, 0x010C, 0x011C, 0x012D, 0x013E, 0x0151, 0x0166, 0x017B,
                0x0191, 0x01A9, 0x01C3, 0x01DD, 0x01FA, 0x0218, 0x0238, 0x025A,
                0x027D, 0x02A3, 0x02CC, 0x02F6, 0x0323, 0x0353, 0x0386, 0x03BB,
                0x03F4, 0x0430, 0x0470, 0x04B4, 0x04FB, 0x0547, 0x0598, 0x05ED,
                0x0647, 0x06A7, 0x070C, 0x0777, 0x07E9, 0x0861, 0x08E1, 0x0968,
                0x09F7, 0x0A8F, 0x0B30, 0x0BDA, 0x0C8F, 0x0D4E, 0x0E18, 0x0EEF,
                0x0FD2, 0x10C3, 0x11C3, 0x12D1, 0x13EF, 0x151F, 0x1660, 0x17B5,
                0x191E, 0x1A9C, 0x1C31, 0x1DDF, 0x1FA5, 0x2187, 0x2386, 0x25A2,
                0x27DF, 0x2A3E, 0x2CC1, 0x2F6B, 0x323C, 0x3539, 0x3863, 0x3BBE,
                0x3F4B, 0x430F, 0x470C, 0x4B45, 0x4FBF, 0x547D, 0x5983, 0x5ED6,
                0x6479, 0x6A73, 0x70C7, 0x777C, 0x7E97, 0x861E, 0x8E18, 0x968B,
                0x9F7E, 0xA8FA, 0xB306, 0xBDAC, 0xC8F3, 0xD4E6, 0xE18F, 0xEEF8,
                0xFD2E
            };

        static readonly int[] SONG_CHANNEL_OFFSET = { 6, 8, 10 };
        static readonly int[] RES_ID_CHANNEL = { 3, 4, 5 };
    }
}

