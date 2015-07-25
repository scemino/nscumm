//
//  Tfmx.cs
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
using System.Diagnostics;
using System;
using System.IO;

namespace NScumm.Core.Audio
{
    public class Tfmx: Paula
    {
        public int Ticks { get { return _playerCtx.tickCount; } }

        public int SongIndex { get { return _playerCtx.song; } }

        public Tfmx(int rate, bool stereo)
            : base(stereo, rate)
        {
            _playerCtx.stopWithLastPattern = false;

            for (int i = 0; i < NumVoices; ++i)
                _channelCtx[i].paulaChannel = (byte)i;

            _playerCtx.volume = 0x40;
            _playerCtx.patternSkip = 6;
            StopSongImpl();

            TimerBase = PalCiaClock;
            InterruptFreqUnscaled = PalDefaultCiaVal;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            FreeResources();
        }

        public bool Load(Stream musicData, Stream sampleData, bool autoDelete)
        {
            var mdat = LoadMdatFile(musicData);
            if (mdat != null)
            {
                var sampleDat = LoadSampleFile(sampleData);
                if (sampleDat != null)
                {
                    SetModuleData(mdat, sampleDat, autoDelete);
                    return true;
                }
                mdat.mdatAlloc = null;
            }
            return false;
        }

        /// <summary>
        /// Stops a playing Song (but leaves macros running) and optionally also stops the player
        /// </summary>
        /// <param name="stopAudio">If set to <c>true</c> stops player and audio output.</param>
        public void StopSong(bool stopAudio = true)
        { 
            lock (_mutex)
            {
                StopSongImpl(stopAudio);
            }
        }

        public void SetModuleData(Tfmx otherPlayer)
        {
            SetModuleData(otherPlayer._resource, otherPlayer._resourceSample, false);
        }

        void SetModuleData(MdatResource resource, byte[] sampleData, bool autoDelete)
        {
            lock (_mutex)
            {
                StopSongImpl(true);
                FreeResourceDataImpl();
                _resource = resource;
                _resourceSample = sampleData;
                _deleteResource = autoDelete;
            }
        }

        public void StopMacroEffect(int channel)
        {
            Debug.Assert(0 <= channel && channel < NumVoices);
            lock (_mutex)
            {
                UnlockMacroChannel(_channelCtx[channel]);
                HaltMacroProgramm(_channelCtx[channel]);
                DisableChannel(_channelCtx[channel].paulaChannel);
            }
        }

        /// <summary>
        /// Stops currently playing Song (if any) and cues up a new one.
        /// if stopAudio is specified, the player gets reset before starting the new song
        /// </summary>
        /// <param name="songPos">index of Song to play.</param>
        /// <param name="stopAudio">If set to <c>true</c> stops player and audio output.</param>
        public void DoSong(int songPos, bool stopAudio = false)
        {
            Debug.Assert(0 <= songPos && songPos < NumSubsongs);
            lock (_mutex)
            {
                StopSongImpl(stopAudio);

                if (!HasResources())
                    return;

                _trackCtx.loopCount = -1;
                _trackCtx.startInd = _trackCtx.posInd = _resource.subsong[songPos].songstart;
                _trackCtx.stopInd = _resource.subsong[songPos].songend;
                _playerCtx.song = (sbyte)songPos;

                var palFlag = (_resource.headerFlags & 2) != 0;
                var tempo = _resource.subsong[songPos].tempo;
                ushort ciaIntervall;
                if (tempo >= 0x10)
                {
                    ciaIntervall = (ushort)(CiaBaseInterval / tempo);
                    _playerCtx.patternSkip = 0;
                }
                else
                {
                    ciaIntervall = palFlag ? (ushort)PalDefaultCiaVal : (ushort)NtscDefaultCiaVal;
                    _playerCtx.patternSkip = tempo;
                }
                InterruptFreqUnscaled = ciaIntervall;
                SetAudioFilter(true);

                _playerCtx.patternCount = 0;
                if (TrackRun())
                    StartPaula();
            }
        }

        /// <summary>
        /// Plays an effect from the sfx-table, does not start audio-playback.
        /// </summary>
        /// <returns>index of the channel which now queued up the effect.
        //  -1 in case the effect couldnt be queued up</returns>
        /// <param name="sfxIndex">index of effect to play.</param>
        /// <param name="unlockChannel">If set to <c>true</c> overwrite higher priority effects.</param>
        public int DoSfx(ushort sfxIndex, bool unlockChannel = false)
        {
            Debug.Assert(sfxIndex < 128);
            lock (_mutex)
            {

                if (!HasResources())
                    return -1;
                var sfxEntryOff = (int)(_resource.sfxTableOffset + sfxIndex * 8) - _resource.mdatOffset;
                if (_resource.mdatAlloc[sfxEntryOff] == 0xFB)
                {
                    Debug.WriteLine("Tfmx: custom patterns are not supported");
                    // custompattern
                    /* const uint8 patCmd = sfxEntry[2];
        const int8 patExp = (int8)sfxEntry[3]; */
                }
                else
                {
                    // custommacro
                    var channelNo = ((_playerCtx.song >= 0) ? _resource.mdatAlloc[sfxEntryOff + 2] : _resource.mdatAlloc[sfxEntryOff + 4]) & (NumVoices - 1);
                    var priority = _resource.mdatAlloc[sfxEntryOff + 5] & 0x7F;

                    var channel = _channelCtx[channelNo];
                    if (unlockChannel)
                        UnlockMacroChannel(channel);

                    var sfxLocktime = channel.sfxLockTime;
                    if (priority >= channel.customMacroPrio || sfxLocktime < 0)
                    {
                        if (sfxIndex != channel.customMacroIndex || sfxLocktime < 0 || (_resource.mdatAlloc[sfxEntryOff + 5] < 0x80))
                        {
                            channel.customMacro = BitConverter.ToUInt32(_resource.mdatAlloc, sfxEntryOff); // intentionally not "endian-correct"
                            channel.customMacroPrio = (byte)priority;
                            channel.customMacroIndex = (byte)sfxIndex;
                            Debug.WriteLine("Tfmx: running Macro {0:X8} on channel {1} - priority: {2:X2}",
                                ScummHelper.SwapBytes(channel.customMacro), channelNo, priority);
                            return channelNo;
                        }
                    }
                }
                return -1;
            }
        }

        public void SetSignalAction(Action<int,ushort> action)
        {
            _playerCtx.signal = action;
        }

        protected override void Interrupt()
        {
            Debug.Assert(!IsEndOfData);
            ++_playerCtx.tickCount;

            for (int i = 0; i < NumVoices; ++i)
            {
                if (_channelCtx[i].dmaIntCount != 0)
                {
                    // wait for DMA Interupts to happen
                    int doneDma = GetChannelDmaCount(i);
                    if (doneDma >= _channelCtx[i].dmaIntCount)
                    {
                        _channelCtx[i].dmaIntCount = 0;
                        _channelCtx[i].macroRun = true;
                    }
                }
            }

            for (int i = 0; i < NumVoices; ++i)
            {
                var channel = _channelCtx[i];

                if (channel.sfxLockTime >= 0)
                    --channel.sfxLockTime;
                else
                {
                    channel.sfxLocked = false;
                    channel.customMacroPrio = 0;
                }

                // externally queued macros
                if (channel.customMacro != 0)
                {
                    var noteCmd = BitConverter.GetBytes(channel.customMacro);
                    channel.sfxLocked = false;
                    NoteCommand(noteCmd[0], noteCmd[1], (byte)((noteCmd[2] & 0xF0) | i), noteCmd[3]);
                    channel.customMacro = 0;
                    channel.sfxLocked = (channel.customMacroPrio != 0);
                }

                // apply timebased effects on Parameters
                if (channel.macroSfxRun > 0)
                    Effects(channel);

                // see if we have to run the macro-program
                if (channel.macroRun)
                {
                    if (channel.macroWait == 0)
                        MacroRun(channel);
                    else
                        --channel.macroWait;
                }

                SetChannelPeriod(i, (short)channel.period);
                if (channel.macroSfxRun >= 0)
                    channel.macroSfxRun = 1;

                // TODO: handling pending DMAOff?
            }

            // Patterns are only processed each _playerCtx.timerCount + 1 tick
            if (_playerCtx.song >= 0 && _playerCtx.patternCount-- == 0)
            {
                _playerCtx.patternCount = _playerCtx.patternSkip;
                AdvancePatterns();
            }
        }

        bool HasResources()
        {
            return _resource != null && _resource.mdatLen != 0 && _resourceSample != null;
        }

        MdatResource LoadMdatFile(Stream musicData)
        {
            var br = new BinaryReader(musicData);
            bool hasHeader = false;
            var mdatSize = musicData.Length;
            if (mdatSize >= 0x200)
            {

                // 0x0000: 10 Bytes Header "TFMX-SONG "
                var buf = System.Text.Encoding.UTF8.GetString(br.ReadBytes(10));
                hasHeader = buf == "TFMX-SONG ";
            }

            if (!hasHeader)
            {
                Debug.WriteLine("Tfmx: File is not a Tfmx Module");
                return null;
            }

            var resource = new MdatResource();

            resource.mdatAlloc = null;
            resource.mdatOffset = 0;
            resource.mdatLen = 0;

            // 0x000A: int16 flags
            resource.headerFlags = br.ReadUInt16BigEndian();
            // 0x000C: int32 ?
            // 0x0010: 6*40 Textfield
            musicData.Seek(4 + 6 * 40, SeekOrigin.Current);

            /* 0x0100: Songstart x 32*/
            for (int i = 0; i < NumSubsongs; ++i)
                resource.subsong[i].songstart = br.ReadUInt16BigEndian();
            /* 0x0140: Songend x 32*/
            for (int i = 0; i < NumSubsongs; ++i)
                resource.subsong[i].songend = br.ReadUInt16BigEndian();
            /* 0x0180: Tempo x 32*/
            for (int i = 0; i < NumSubsongs; ++i)
                resource.subsong[i].tempo = br.ReadUInt16BigEndian();

            /* 0x01c0: unused ? */
            musicData.Seek(16, SeekOrigin.Current);

            /* 0x01d0: trackstep, pattern data p, macro data p */
            var offTrackstep = br.ReadUInt32BigEndian();
            uint offPatternP, offMacroP;

            // This is how MI`s TFMX-Player tests for unpacked Modules.
            if (offTrackstep == 0)
            { // unpacked File
                resource.trackstepOffset = 0x600 + 0x200;
                offPatternP = 0x200 + 0x200;
                offMacroP = 0x400 + 0x200;
            }
            else
            { // packed File
                resource.trackstepOffset = offTrackstep;
                offPatternP = br.ReadUInt32BigEndian();
                offMacroP = br.ReadUInt32BigEndian();
            }

            // TODO: if a File is packed it could have for Ex only 2 Patterns/Macros
            // the following loops could then read beyond EOF.
            // To correctly handle this it would be necessary to sort the pointers and
            // figure out the number of Macros/Patterns
            // We could also analyze pointers if they are correct offsets,
            // so that accesses can be unchecked later

            // Read in pattern starting offsets
            musicData.Seek(offPatternP, SeekOrigin.Begin);
            for (int i = 0; i < MaxPatternOffsets; ++i)
                resource.patternOffset[i] = br.ReadUInt32BigEndian();

            // use last PatternOffset (stored at 0x5FC in mdat) if unpacked File
            // or fixed offset 0x200 if packed
            resource.sfxTableOffset = offTrackstep != 0 ? 0x200 : resource.patternOffset[127];

            // Read in macro starting offsets
            musicData.Seek(offMacroP, SeekOrigin.Begin);
            for (int i = 0; i < MaxMacroOffsets; ++i)
                resource.macroOffset[i] = br.ReadUInt32BigEndian();

            // Read in mdat-file
            // TODO: we can skip everything thats already stored in the resource-structure.
            var mdatOffset = offTrackstep != 0 ? 0x200 : 0x600;  // 0x200 is very conservative
            var allocSize = mdatSize - mdatOffset;

            musicData.Seek(mdatOffset, SeekOrigin.Begin);
            var mdatAlloc = br.ReadBytes((int)allocSize);

            resource.mdatAlloc = mdatAlloc;
            resource.mdatOffset = mdatOffset;
            resource.mdatLen = (int)mdatSize;
            return resource;
        }

        byte[] LoadSampleFile(Stream sampleStream)
        {
            var br = new BinaryReader(sampleStream);

            var sampleSize = (int)sampleStream.Length;
            if (sampleSize < 4)
            {
                Debug.WriteLine("Tfmx: Cant load Samplefile");
                return null;
            }

            var sampleAlloc = br.ReadBytes(sampleSize);
            sampleAlloc[0] = sampleAlloc[1] = sampleAlloc[2] = sampleAlloc[3] = 0;
            return sampleAlloc;
        }

        void FreeResources()
        { 
            _deleteResource = true; 
            FreeResourceDataImpl(); 
        }

        void FreeResourceDataImpl()
        {
            if (_deleteResource)
            {
                if (_resource != null)
                {
                    _resource.mdatAlloc = null;
                    _resource = null;
                }
                _resourceSample = null;
            }
            _resource = null;
            _resourceSample = null;
            _deleteResource = false;
        }

        void AdvancePatterns()
        {
            startPatterns:
            int runningPatterns = 0;

            for (int i = 0; i < NumChannels; ++i)
            {
                var pattern = _patternCtx[i];
                var pattCmd = pattern.command;
                if (pattCmd < 0x90)
                {   // execute Patternstep
                    ++runningPatterns;
                    if (pattern.wait == 0)
                    {
                        // issue all Steps for this tick
                        if (PatternRun(pattern))
                        {
                            // we load the next Trackstep Command and then process all Channels again
                            if (TrackRun(true))
                                goto startPatterns;
                            else
                                break;
                        }

                    }
                    else
                        --pattern.wait;

                }
                else if (pattCmd == 0xFE)
                {   // Stop voice in pattern.expose
                    pattern.command = 0xFF;
                    var channel = _channelCtx[pattern.expose & (NumVoices - 1)];
                    if (!channel.sfxLocked)
                    {
                        HaltMacroProgramm(channel);
                        DisableChannel(channel.paulaChannel);
                    }
                } // else this pattern-Channel is stopped
            }
            if (_playerCtx.stopWithLastPattern && runningPatterns == 0)
            {
                StopPaula();
            }
        }

        bool PatternRun(PatternContext pattern)
        {
            for (;;)
            {
                var patternPtrOff = (int)(pattern.offset + 4 * pattern.step - _resource.mdatOffset);
                ++pattern.step;
                byte pattCmd = _resource.mdatAlloc[patternPtrOff];

                if (pattCmd < 0xF0)
                { // Playnote
                    bool doWait = false;
                    byte noteCmd = (byte)(pattCmd + pattern.expose);
                    byte param3 = _resource.mdatAlloc[patternPtrOff + 3];
                    if (pattCmd < 0xC0)
                    {   // Note
                        if (pattCmd >= 0x80)
                        {  // Wait
                            pattern.wait = param3;
                            param3 = 0;
                            doWait = true;
                        }
                        noteCmd &= 0x3F;
                    }   // else Portamento
                    NoteCommand(noteCmd, _resource.mdatAlloc[patternPtrOff + 1], _resource.mdatAlloc[patternPtrOff + 2], param3);
                    if (doWait)
                        return false;

                }
                else
                {    // Patterncommand
                    switch (pattCmd & 0xF)
                    {
                        case 0:     // End Pattern + Next Trackstep
                            pattern.command = 0xFF;
                            --pattern.step;
                            return true;

                        case 1:     // Loop Pattern. Parameters: Loopcount, PatternStep(W)
                            if (pattern.loopCount != 0)
                            {
                                if (pattern.loopCount == 0xFF)
                                    pattern.loopCount = _resource.mdatAlloc[patternPtrOff + 1];
                                pattern.step = _resource.mdatAlloc.ToUInt16BigEndian(patternPtrOff + 2);
                            }
                            --pattern.loopCount;
                            continue;

                        case 2:     // Jump. Parameters: PatternIndex, PatternStep(W)
                            pattern.offset = _resource.patternOffset[_resource.mdatAlloc[patternPtrOff + 1] & (MaxPatternOffsets - 1)];
                            pattern.step = _resource.mdatAlloc.ToUInt16BigEndian(patternPtrOff + 2);
                            continue;

                        case 3:     // Wait. Paramters: ticks to wait
                            pattern.wait = _resource.mdatAlloc[patternPtrOff + 1];
                            return false;

                        case 14:    // Stop custompattern
                            // TODO apparently toggles on/off pattern channel 7
                            Debug.WriteLine("Tfmx: Encountered 'Stop custompattern' command");
                            // same as 4
                            pattern.command = 0xFF;
                            --pattern.step;
                            break;
                    // FT
                        case 4:     // Stop this pattern
                            pattern.command = 0xFF;
                            --pattern.step;
                            // TODO: try figuring out if this was the last Channel?
                            return false;

                        case 5:     // Key Up Signal. Paramters: channel
                            if (!_channelCtx[_resource.mdatAlloc[patternPtrOff + 2] & (NumVoices - 1)].sfxLocked)
                                _channelCtx[_resource.mdatAlloc[patternPtrOff + 2] & (NumVoices - 1)].keyUp = true;
                            continue;

                        case 6:     // Vibrato. Parameters: length, channel, rate
                        case 7:     // Envelope. Parameters: rate, tempo | channel, endVol
                            NoteCommand(pattCmd, _resource.mdatAlloc[patternPtrOff + 1], _resource.mdatAlloc[patternPtrOff + 2], _resource.mdatAlloc[patternPtrOff + 3]);
                            continue;

                        case 8:     // Subroutine. Parameters: pattern, patternstep(W)
                            pattern.savedOffset = pattern.offset;
                            pattern.savedStep = pattern.step;

                            pattern.offset = _resource.patternOffset[_resource.mdatAlloc[patternPtrOff + 1] & (MaxPatternOffsets - 1)];
                            pattern.step = _resource.mdatAlloc.ToUInt16BigEndian(patternPtrOff + 2);
                            continue;

                        case 9:     // Return from Subroutine
                            pattern.offset = pattern.savedOffset;
                            pattern.step = pattern.savedStep;
                            continue;

                        case 10:    // fade. Parameters: tempo, endVol
                            InitFadeCommand(_resource.mdatAlloc[patternPtrOff + 1], (sbyte)_resource.mdatAlloc[patternPtrOff + 3]);
                            continue;

                        case 11:    // play pattern. Parameters: patternCmd, channel, expose
                            InitPattern(_patternCtx[_resource.mdatAlloc[patternPtrOff + 2] & (NumChannels - 1)], 
                                _resource.mdatAlloc[patternPtrOff + 1], 
                                (sbyte)_resource.mdatAlloc[patternPtrOff + 3], 
                                _resource.patternOffset[_resource.mdatAlloc[patternPtrOff + 1] & (MaxPatternOffsets - 1)]);
                            continue;

                        case 12:    // Lock. Parameters: lockFlag, channel, lockTime
                            _channelCtx[_resource.mdatAlloc[patternPtrOff + 2] & (NumVoices - 1)].sfxLocked = (_resource.mdatAlloc[patternPtrOff + 1] != 0);
                            _channelCtx[_resource.mdatAlloc[patternPtrOff + 2] & (NumVoices - 1)].sfxLockTime = _resource.mdatAlloc[patternPtrOff + 3];
                            continue;

                        case 13:    // Cue. Parameters: signalnumber, value(W)
                            if (_playerCtx.signal != null)
                                _playerCtx.signal(_resource.mdatAlloc[patternPtrOff + 1], _resource.mdatAlloc.ToUInt16BigEndian(patternPtrOff + 2));
                            continue;

                        case 15:    // NOP
                            continue;
                    }
                }
            }
        }

        void InitPattern(PatternContext pattern, byte cmd, sbyte expose, uint offset)
        {
            pattern.command = cmd;
            pattern.offset = offset;
            pattern.expose = expose;
            pattern.step = 0;
            pattern.wait = 0;
            pattern.loopCount = 0xFF;

            pattern.savedOffset = 0;
            pattern.savedStep = 0;
        }

        bool TrackRun(bool incStep = false)
        {
            Debug.Assert(_playerCtx.song >= 0);
            if (incStep)
            {
                // TODO Optionally disable looping
                if (_trackCtx.posInd == _trackCtx.stopInd)
                    _trackCtx.posInd = _trackCtx.startInd;
                else
                    ++_trackCtx.posInd;
            }
            for (;;)
            {
                var trackDataOff = (int)(_resource.trackstepOffset + 16 * _trackCtx.posInd - _resource.mdatOffset);

                if (BitConverter.ToUInt16(_resource.mdatAlloc, trackDataOff) != ScummHelper.SwapBytes(0xEFFE))
                {
                    // 8 commands for Patterns
                    for (int i = 0; i < 8; ++i)
                    {
                        var patCmdOff = trackDataOff + i * 2;
                        // First byte is pattern number
                        var patNum = _resource.mdatAlloc[patCmdOff];
                        // if highest bit is set then keep previous pattern
                        if (patNum < 0x80)
                        {
                            InitPattern(_patternCtx[i], patNum, (sbyte)_resource.mdatAlloc[patCmdOff + 1], _resource.patternOffset[patNum]);
                        }
                        else
                        {
                            _patternCtx[i].command = patNum;
                            _patternCtx[i].expose = (sbyte)_resource.mdatAlloc[patCmdOff + 1];
                        }
                    }
                    return true;

                }
                else
                {
                    // 16 byte Trackstep Command
                    switch (_resource.mdatAlloc.ToUInt16BigEndian(trackDataOff + 2 * 1))
                    {
                        case 0: // Stop Player. No Parameters
                            StopPaula();
                            return false;

                        case 1: // Branch/Loop section of tracksteps. Parameters: branch target, loopcount
                            if (_trackCtx.loopCount != 0)
                            {
                                if (_trackCtx.loopCount < 0)
                                    _trackCtx.loopCount = _resource.mdatAlloc.ToInt16BigEndian(trackDataOff + 2 * 3);
                                _trackCtx.posInd = _resource.mdatAlloc.ToUInt16BigEndian(trackDataOff + 2 * 2);
                                continue;
                            }
                            --_trackCtx.loopCount;
                            break;

                        case 2:
                            { // Set Tempo. Parameters: tempo, divisor
                                _playerCtx.patternCount = _playerCtx.patternSkip = _resource.mdatAlloc.ToUInt16BigEndian(trackDataOff + 2 * 2); // tempo
                                var temp = _resource.mdatAlloc.ToUInt16BigEndian(trackDataOff + 2 * 3); // divisor

                                if (((temp & 0x8000) == 0) && ((temp & 0x1FF) != 0))
                                    InterruptFreqUnscaled = (uint)(temp & 0x1FF);
                                break;
                            }
                        case 4: // Fade. Parameters: tempo, endVol
                            // load the LSB of the 16bit words
                            InitFadeCommand(_resource.mdatAlloc[trackDataOff + 2 * 2 + 1], (sbyte)_resource.mdatAlloc[trackDataOff + 2 * 3 + 1]);
                            break;

                        case 3: // Unknown, stops player aswell
                        default:
                            Debug.WriteLine("Tfmx: Unknown Trackstep Command: {0:X2}", _resource.mdatAlloc.ToUInt16BigEndian(trackDataOff + 2 * 1));
                            break;
                    // MI-Player handles this by stopping the player, we just continue
                    }
                }

                if (_trackCtx.posInd == _trackCtx.stopInd)
                {
                    Debug.WriteLine("Tfmx: Reached invalid Song-Position");
                    return false;
                }
                ++_trackCtx.posInd;
            }
        }

        void MacroRun(ChannelContext channel)
        {
            bool deferWait = channel.deferWait;
            for (;;)
            {
                var macroPtrOff = (int)(channel.macroOffset + 4 * channel.macroStep - _resource.mdatOffset);
                ++channel.macroStep;

                switch (_resource.mdatAlloc[macroPtrOff])
                {
                    case 0x00:  // Reset + DMA Off. Parameters: deferWait, addset, vol
                        ClearEffects(channel);
                        // same as 0x13
                        // TODO: implement PArameters
                        DisableChannel(channel.paulaChannel);
                        channel.deferWait = deferWait = (_resource.mdatAlloc[macroPtrOff + 1] != 0);
                        if (deferWait)
                        {
                            // if set, then we expect a DMA On in the same tick.
                            channel.period = 4;
                            //Paula::setChannelPeriod(channel.paulaChannel, channel.period);
                            SetChannelSampleLen(channel.paulaChannel, 1);
                            // in this state we then need to allow some commands that normally
                            // would halt the macroprogamm to continue instead.
                            // those commands are: Wait, WaitDMA, AddPrevNote, AddNote, SetNote, <unknown Cmd>
                            // DMA On is affected aswell
                            // TODO remember time disabled, remember pending dmaoff?.
                        }

                        if (_resource.mdatAlloc[macroPtrOff + 2] != 0 || _resource.mdatAlloc[macroPtrOff + 3] != 0)
                        {
                            channel.volume = (sbyte)((_resource.mdatAlloc[macroPtrOff + 2] != 0 ? 0 : channel.relVol * 3) + _resource.mdatAlloc[macroPtrOff + 3]);
                            SetChannelVolume(channel.paulaChannel, (byte)channel.volume);
                        }
                        continue;
                // FT
                    case 0x13:  // DMA Off. Parameters:  deferWait, addset, vol
                        // TODO: implement PArameters
                        DisableChannel(channel.paulaChannel);
                        channel.deferWait = deferWait = (_resource.mdatAlloc[macroPtrOff + 1] != 0);
                        if (deferWait)
                        {
                            // if set, then we expect a DMA On in the same tick.
                            channel.period = 4;
                            //Paula::setChannelPeriod(channel.paulaChannel, channel.period);
                            SetChannelSampleLen(channel.paulaChannel, 1);
                            // in this state we then need to allow some commands that normally
                            // would halt the macroprogamm to continue instead.
                            // those commands are: Wait, WaitDMA, AddPrevNote, AddNote, SetNote, <unknown Cmd>
                            // DMA On is affected aswell
                            // TODO remember time disabled, remember pending dmaoff?.
                        }

                        if (_resource.mdatAlloc[macroPtrOff + 2] != 0 || _resource.mdatAlloc[macroPtrOff + 3] != 0)
                        {
                            channel.volume = (sbyte)((_resource.mdatAlloc[macroPtrOff + 2] != 0 ? 0 : channel.relVol * 3) + _resource.mdatAlloc[macroPtrOff + 3]);
                            SetChannelVolume(channel.paulaChannel, (byte)channel.volume);
                        }
                        continue;

                    case 0x01:  // DMA On
                        // TODO: Parameter macroPtr[1] - en-/disable effects
                        channel.dmaIntCount = 0;
                        if (deferWait)
                        {
                            // TODO
                            // there is actually a small delay in the player, but I think that
                            // only allows to clear DMA-State on real Hardware
                        }
                        SetChannelPeriod(channel.paulaChannel, (short)channel.period);
                        EnableChannel(channel.paulaChannel);
                        channel.deferWait = deferWait = false;
                        continue;

                    case 0x02:  // Set Beginn. Parameters: SampleOffset(L)
                        channel.addBeginLength = 0;
                        channel.sampleStart = _resource.mdatAlloc.ToInt32BigEndian(macroPtrOff) & 0xFFFFFF;
                        SetChannelSampleStart(channel.paulaChannel, GetSamplePtr(channel.sampleStart));
                        continue;

                    case 0x03:  // SetLength. Parameters: SampleLength(W)
                        channel.sampleLen = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        SetChannelSampleLen(channel.paulaChannel, channel.sampleLen);
                        continue;

                    case 0x04:  // Wait. Parameters: Ticks to wait(W).
                        // TODO: some unknown Parameter? (macroPtr[1] & 1)
                        channel.macroWait = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        break;

                    case 0x10:  // Loop Key Up. Parameters: Loopcount, MacroStep(W)
                        if (channel.keyUp)
                            continue;
                        // same as 0x05
                        if (channel.macroLoopCount != 0)
                        {
                            if (channel.macroLoopCount == 0xFF)
                                channel.macroLoopCount = _resource.mdatAlloc[macroPtrOff + 1];
                            channel.macroStep = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        }
                        --channel.macroLoopCount;
                        continue;
                // FT
                    case 0x05:  // Loop. Parameters: Loopcount, MacroStep(W)
                        if (channel.macroLoopCount != 0)
                        {
                            if (channel.macroLoopCount == 0xFF)
                                channel.macroLoopCount = _resource.mdatAlloc[macroPtrOff + 1];
                            channel.macroStep = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        }
                        --channel.macroLoopCount;
                        continue;

                    case 0x06:  // Jump. Parameters: MacroIndex, MacroStep(W)
                        // channel.macroIndex = macroPtr[1] & (kMaxMacroOffsets - 1);
                        channel.macroOffset = _resource.macroOffset[_resource.mdatAlloc[macroPtrOff + 1] & (MaxMacroOffsets - 1)];
                        channel.macroStep = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        channel.macroLoopCount = 0xFF;
                        continue;

                    case 0x07:  // Stop Macro
                        channel.macroRun = false;
                        --channel.macroStep;
                        return;

                    case 0x08:  // AddNote. Parameters: Note, Finetune(W)
                        SetNoteMacro(channel, channel.note + _resource.mdatAlloc[macroPtrOff + 1], _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2));
                        break;

                    case 0x09:  // SetNote. Parameters: Note, Finetune(W)
                        SetNoteMacro(channel, _resource.mdatAlloc[macroPtrOff + 1], _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2));
                        break;

                    case 0x0A:  // Clear Effects
                        ClearEffects(channel);
                        continue;

                    case 0x0B:  // Portamento. Parameters: count, speed
                        channel.portaSkip = _resource.mdatAlloc[macroPtrOff + 1];
                        channel.portaCount = 1;
                        // if porta is already running, then keep using old value
                        if (channel.portaDelta == 0)
                            channel.portaValue = channel.refPeriod;
                        channel.portaDelta = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        continue;

                    case 0x0C:  // Vibrato. Parameters: Speed, intensity
                        channel.vibLength = _resource.mdatAlloc[macroPtrOff + 1];
                        channel.vibCount = (byte)(_resource.mdatAlloc[macroPtrOff + 1] / 2);
                        channel.vibDelta = (sbyte)_resource.mdatAlloc[macroPtrOff + 3];
                        // TODO: Perhaps a bug, vibValue could be left uninitialized
                        if (channel.portaDelta == 0)
                        {
                            channel.period = channel.refPeriod;
                            channel.vibValue = 0;
                        }
                        continue;

                    case 0x0D:  // Add Volume. Parameters: note, addNoteFlag, volume
                        if (_resource.mdatAlloc[macroPtrOff + 2] == 0xFE)
                        {
                            SetNoteMacro(channel, channel.note + _resource.mdatAlloc[macroPtrOff + 1], 0);
                        }
                        channel.volume = (sbyte)(channel.relVol * 3 + _resource.mdatAlloc[macroPtrOff + 3]);
                        continue;

                    case 0x0E:  // Set Volume. Parameters: note, addNoteFlag, volume
                        if (_resource.mdatAlloc[macroPtrOff + 2] == 0xFE)
                        {
                            SetNoteMacro(channel, channel.note + _resource.mdatAlloc[macroPtrOff + 1], 0);
                        }
                        channel.volume = (sbyte)_resource.mdatAlloc[macroPtrOff + 3];
                        continue;

                    case 0x0F:  // Envelope. Parameters: speed, count, endvol
                        channel.envDelta = _resource.mdatAlloc[macroPtrOff + 1];
                        channel.envCount = channel.envSkip = _resource.mdatAlloc[macroPtrOff + 2];
                        channel.envEndVolume = (sbyte)_resource.mdatAlloc[macroPtrOff + 3];
                        continue;

                    case 0x11:  // Add Beginn. Parameters: times, Offset(W)
                        channel.addBeginLength = channel.addBeginCount = _resource.mdatAlloc[macroPtrOff + 1];
                        channel.addBeginDelta = _resource.mdatAlloc.ToInt16BigEndian(macroPtrOff + 2);
                        channel.sampleStart += channel.addBeginDelta;
                        SetChannelSampleStart(channel.paulaChannel, GetSamplePtr(channel.sampleStart));
                        continue;

                    case 0x12:  // Add Length. Parameters: added Length(W)
                        channel.sampleLen = (ushort)(channel.sampleLen + _resource.mdatAlloc.ToInt16BigEndian(macroPtrOff + 2));
                        SetChannelSampleLen(channel.paulaChannel, channel.sampleLen);
                        continue;

                    case 0x14:  // Wait key up. Parameters: wait cycles
                        if (channel.keyUp || channel.macroLoopCount == 0)
                        {
                            channel.macroLoopCount = 0xFF;
                            continue;
                        }
                        else if (channel.macroLoopCount == 0xFF)
                            channel.macroLoopCount = _resource.mdatAlloc[macroPtrOff + 3];
                        --channel.macroLoopCount;
                        --channel.macroStep;
                        return;

                    case 0x15:  // Subroutine. Parameters: MacroIndex, Macrostep(W)
                        channel.macroReturnOffset = channel.macroOffset;
                        channel.macroReturnStep = channel.macroStep;

                        channel.macroOffset = (_resource.macroOffset[_resource.mdatAlloc[macroPtrOff + 1] & (MaxMacroOffsets - 1)]);
                        channel.macroStep = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        // TODO: MI does some weird stuff there. Figure out which varioables need to be set
                        continue;

                    case 0x16:  // Return from Sub.
                        channel.macroOffset = channel.macroReturnOffset;
                        channel.macroStep = channel.macroReturnStep;
                        continue;

                    case 0x17:  // Set Period. Parameters: Period(W)
                        channel.refPeriod = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        if (channel.portaDelta == 0)
                        {
                            channel.period = channel.refPeriod;
                            //Paula::setChannelPeriod(channel.paulaChannel, channel.period);
                        }
                        continue;

                    case 0x18:
                        {    // Sampleloop. Parameters: Offset from Samplestart(W)
                            // TODO: MI loads 24 bit, but thats useless?
                            ushort temp = /* ((int8)macroPtr[1] << 16) | */ _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                            if (_resource.mdatAlloc[macroPtrOff + 1] != 0 || ((temp & 1) != 0))
                                Debug.WriteLine("Tfmx: Problematic value for sampleloop: {0:X6}", (_resource.mdatAlloc[macroPtrOff + 1] << 16) | temp);
                            channel.sampleStart += temp & 0xFFFE;
                            channel.sampleLen -= (ushort)(temp / 2) /* & 0x7FFF */;
                            SetChannelSampleStart(channel.paulaChannel, GetSamplePtr(channel.sampleStart));
                            SetChannelSampleLen(channel.paulaChannel, channel.sampleLen);
                            continue;
                        }
                    case 0x19:  // Set One-Shot Sample
                        channel.addBeginLength = 0;
                        channel.sampleStart = 0;
                        channel.sampleLen = 1;
                        SetChannelSampleStart(channel.paulaChannel, GetSamplePtr(0));
                        SetChannelSampleLen(channel.paulaChannel, 1);
                        continue;

                    case 0x1A:  // Wait on DMA. Parameters: Cycles-1(W) to wait
                        channel.dmaIntCount = (ushort)(_resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2) + 1);
                        channel.macroRun = false;
                        SetChannelDmaCount(channel.paulaChannel);
                        break;

                /*      case 0x1B:  // Random play. Parameters: macro/speed/mode
            warnMacroUnimplemented(macroPtr, 0);
            continue;*/

                    case 0x1C:  // Branch on Note. Parameters: note/macrostep(W)
                        if (channel.note > _resource.mdatAlloc[macroPtrOff + 1])
                            channel.macroStep = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        continue;

                    case 0x1D:  // Branch on Volume. Parameters: volume/macrostep(W)
                        if (channel.volume > _resource.mdatAlloc[macroPtrOff + 1])
                            channel.macroStep = _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2);
                        continue;

                /*      case 0x1E:  // Addvol+note. Parameters: note/CONST./volume
            warnMacroUnimplemented(macroPtr, 0);
            continue;*/

                    case 0x1F:  // AddPrevNote. Parameters: Note, Finetune(W)
                        SetNoteMacro(channel, channel.prevNote + _resource.mdatAlloc[macroPtrOff + 1], _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2));
                        break;

                    case 0x20:  // Signal. Parameters: signalnumber, value(W)
                        if (_playerCtx.signal != null)
                            _playerCtx.signal(_resource.mdatAlloc[macroPtrOff + 1], _resource.mdatAlloc.ToUInt16BigEndian(macroPtrOff + 2));
                        continue;

                    case 0x21:  // Play macro. Parameters: macro, chan, detune
                        NoteCommand(channel.note, _resource.mdatAlloc[macroPtrOff + 1], 
                            (byte)((channel.relVol << 4) | _resource.mdatAlloc[macroPtrOff + 2]), 
                            _resource.mdatAlloc[macroPtrOff + 3]);
                        continue;

                // 0x22 - 0x29 are used by Gem`X
                // 0x30 - 0x34 are used by Carribean Disaster

                    default:
                        Debug.WriteLine("Tfmx: Macro {0:XX} not supported", _resource.mdatAlloc[macroPtrOff]);
                        break;
                }
                if (!deferWait)
                    return;
            }
        }

        byte[] GetSamplePtr(int offset)
        {
            var tmp = new byte[_resourceSample.Length - offset];
            Array.Copy(_resourceSample, offset, tmp, 0, tmp.Length);
            return tmp;
        }

        void SetNoteMacro(ChannelContext channel, int note, int fineTune)
        {
            var noteInt = noteIntervalls[note & 0x3F];
            var finetune = (ushort)(fineTune + channel.fineTune + (1 << 8));
            channel.refPeriod = (ushort)((uint)noteInt * finetune >> 8);
            if (channel.portaDelta == 0)
                channel.period = channel.refPeriod;
        }

        void Effects(ChannelContext channel)
        {
            // addBegin
            if (channel.addBeginLength != 0)
            {
                channel.sampleStart += channel.addBeginDelta;
                SetChannelSampleStart(channel.paulaChannel, GetSamplePtr(channel.sampleStart));
                if ((--channel.addBeginCount) == 0)
                {
                    channel.addBeginCount = channel.addBeginLength;
                    channel.addBeginDelta = -channel.addBeginDelta;
                }
            }

            // vibrato
            if (channel.vibLength != 0)
            {
                channel.vibValue += channel.vibDelta;
                if (--channel.vibCount == 0)
                {
                    channel.vibCount = channel.vibLength;
                    channel.vibDelta = (sbyte)-channel.vibDelta;
                }
                if (channel.portaDelta == 0)
                {
                    // 16x16 bit multiplication, casts needed for the right results
                    channel.period = (ushort)(((uint)channel.refPeriod * (ushort)((1 << 11) + channel.vibValue)) >> 11);
                }
            }

            // portamento
            if (channel.portaDelta != 0 && (--channel.portaCount) == 0)
            {
                channel.portaCount = channel.portaSkip;

                bool resetPorta = true;
                ushort period = channel.refPeriod;
                ushort portaVal = channel.portaValue;

                if (period > portaVal)
                {
                    portaVal = (ushort)(((uint)portaVal * (ushort)((1 << 8) + channel.portaDelta)) >> 8);
                    resetPorta = (period <= portaVal);

                }
                else if (period < portaVal)
                {
                    portaVal = (ushort)(((uint)portaVal * (ushort)((1 << 8) - channel.portaDelta)) >> 8);
                    resetPorta = (period >= portaVal);
                }

                if (resetPorta)
                {
                    channel.portaDelta = 0;
                    channel.portaValue = (ushort)(period & 0x7FF);
                }
                else
                    channel.period = channel.portaValue = (ushort)(portaVal & 0x7FF);
            }

            // envelope
            if (channel.envSkip != 0 && channel.envCount-- == 0)
            {
                channel.envCount = channel.envSkip;

                sbyte endVol = channel.envEndVolume;
                sbyte volume = channel.volume;
                bool resetEnv;

                if (endVol > volume)
                {
                    volume += (sbyte)channel.envDelta;
                    resetEnv = endVol <= volume;
                }
                else
                {
                    volume -= (sbyte)channel.envDelta;
                    resetEnv = volume <= 0 || endVol >= volume;
                }

                if (resetEnv)
                {
                    channel.envSkip = 0;
                    volume = endVol;
                }
                channel.volume = volume;
            }

            // Fade
            if (_playerCtx.fadeDelta != 0 && (--_playerCtx.fadeCount) == 0)
            {
                _playerCtx.fadeCount = _playerCtx.fadeSkip;

                _playerCtx.volume += _playerCtx.fadeDelta;
                if (_playerCtx.volume == _playerCtx.fadeEndVolume)
                    _playerCtx.fadeDelta = 0;
            }

            // Volume
            var finVol = (byte)(_playerCtx.volume * channel.volume >> 6);
            SetChannelVolume(channel.paulaChannel, finVol);
        }

        void InitFadeCommand(byte fadeTempo, sbyte endVol)
        {
            _playerCtx.fadeCount = _playerCtx.fadeSkip = fadeTempo;
            _playerCtx.fadeEndVolume = endVol;

            if (fadeTempo != 0)
            {
                int diff = _playerCtx.fadeEndVolume - _playerCtx.volume;
                _playerCtx.fadeDelta = (sbyte)((diff != 0) ? ((diff > 0) ? 1 : -1) : 0);
            }
            else
            {
                _playerCtx.volume = endVol;
                _playerCtx.fadeDelta = 0;
            }
        }

        void NoteCommand(byte note, byte param1, byte param2, byte param3)
        {
            var channel = _channelCtx[param2 & (NumVoices - 1)];

            if (note == 0xFC)
            { // Lock command
                channel.sfxLocked = (param1 != 0);
                channel.sfxLockTime = param3; // only 1 byte read!

            }
            else if (channel.sfxLocked)
            { // Channel still locked, do nothing

            }
            else if (note < 0xC0)
            {   // Play Note - Parameters: note, macro, relVol | channel, finetune

                channel.prevNote = channel.note;
                channel.note = note;
                // channel.macroIndex = param1 & (MaxMacroOffsets - 1);
                channel.macroOffset = (_resource.macroOffset[param1 & (MaxMacroOffsets - 1)]);
                channel.relVol = (byte)(param2 >> 4);
                channel.fineTune = (sbyte)param3;

                // TODO: the point where the channel gets initialized varies with the games, needs more research.
                InitMacroProgramm(channel);
                channel.keyUp = false; // key down = playing a Note

            }
            else if (note < 0xF0)
            {   // Portamento - Parameters: note, tempo, channel, rate
                channel.portaSkip = param1;
                channel.portaCount = 1;
                if (channel.portaDelta == 0)
                    channel.portaValue = channel.refPeriod;
                channel.portaDelta = param3;

                channel.note = (byte)(note & 0x3F);
                channel.refPeriod = noteIntervalls[channel.note];

            }
            else
                switch (note)
                {  // Command

                    case 0xF5:  // Key Up Signal
                        channel.keyUp = true;
                        break;

                    case 0xF6:  // Vibratio - Parameters: length, channel, rate
                        channel.vibLength = (byte)(param1 & 0xFE);
                        channel.vibCount = (byte)(param1 / 2);
                        channel.vibDelta = (sbyte)param3;
                        channel.vibValue = 0;
                        break;

                    case 0xF7:  // Envelope - Parameters: rate, tempo | channel, endVol
                        channel.envDelta = param1;
                        channel.envCount = channel.envSkip = (byte)((param2 >> 4) + 1);
                        channel.envEndVolume = (sbyte)param3;
                        break;
                }
        }

        void StopSongImpl(bool stopAudio = true)
        {
            _playerCtx.song = -1;
            for (int i = 0; i < NumChannels; ++i)
            {
                _patternCtx[i].command = 0xFF;
                _patternCtx[i].expose = 0;
            }
            if (stopAudio)
            {
                StopPaula();
                for (int i = 0; i < NumVoices; ++i)
                {
                    ClearEffects(_channelCtx[i]);
                    UnlockMacroChannel(_channelCtx[i]);
                    HaltMacroProgramm(_channelCtx[i]);
                    _channelCtx[i].note = 0;
                    _channelCtx[i].volume = 0;
                    _channelCtx[i].macroSfxRun = -1;
                    _channelCtx[i].vibValue = 0;

                    _channelCtx[i].sampleStart = 0;
                    _channelCtx[i].sampleLen = 2;
                    _channelCtx[i].refPeriod = 4;
                    _channelCtx[i].period = 4;
                    DisableChannel(i);
                }
            }
        }

        static void InitMacroProgramm(ChannelContext channel)
        {
            channel.macroStep = 0;
            channel.macroWait = 0;
            channel.macroRun = true;
            channel.macroSfxRun = 0;
            channel.macroLoopCount = 0xFF;
            channel.dmaIntCount = 0;
            channel.deferWait = false;

            channel.macroReturnOffset = 0;
            channel.macroReturnStep = 0;
        }

        static void ClearEffects(ChannelContext channel)
        {
            channel.addBeginLength = 0;
            channel.envSkip = 0;
            channel.vibLength = 0;
            channel.portaDelta = 0;
        }

        static void HaltMacroProgramm(ChannelContext channel)
        {
            channel.macroRun = false;
            channel.dmaIntCount = 0;
        }

        static void UnlockMacroChannel(ChannelContext channel)
        {
            channel.customMacro = 0;
            channel.customMacroIndex = 0;
            channel.customMacroPrio = 0;
            channel.sfxLocked = false;
            channel.sfxLockTime = -1;
        }

        const int PalDefaultCiaVal = 11822;
        const int NtscDefaultCiaVal = 14320;
        const int CiaBaseInterval = 0x1B51F8;
        const int NumVoices = 4;
        const int NumChannels = 8;
        const int NumSubsongs = 32;
        const int MaxPatternOffsets = 128;
        const int MaxMacroOffsets = 128;

        byte[] _resourceSample;

        bool _deleteResource;

        class MdatResource
        {
            /// <summary>
            /// allocated Block of Memory
            /// </summary>
            public byte[] mdatAlloc;

            /// <summary>
            /// Start of mdat-File, might point before mdatAlloc to correct Offset
            /// </summary>
            public int mdatOffset;

            public int mdatLen;

            public ushort headerFlags;
            //      uint32 headerUnknown;
            //      char textField[6 * 40];

            public class Subsong
            {
                public ushort songstart;
                ///< Index in Trackstep-Table
                public ushort songend;
                ///< Last index in Trackstep-Table
                public ushort tempo;
            }

            public Subsong[] subsong;

            public uint trackstepOffset;
            ///< Offset in mdat
            public uint sfxTableOffset;

            public uint[] patternOffset = new uint[MaxPatternOffsets];
            ///< Offset in mdat
            public uint[] macroOffset = new uint[MaxMacroOffsets];

            ///< Offset in mdat

            public MdatResource()
            {
                subsong = new Subsong[NumSubsongs];
                for (int i = 0; i < subsong.Length; i++)
                {
                    subsong[i] = new Subsong();
                }
            }
        }

        MdatResource _resource;

        class PatternContext
        {
            /// <summary>
            /// patternStart, Offset from mdat
            /// </summary>
            public uint offset;
            /// <summary>
            /// for subroutine calls
            /// </summary>
            public uint savedOffset;
            /// <summary>
            /// distance from patternStart
            /// </summary>
            public ushort step;
            public ushort savedStep;

            public byte command;
            public sbyte expose;
            public byte loopCount;
            /// <summary>
            /// how many ticks to wait before next Command.
            /// </summary>
            public byte wait;
        }

        PatternContext[] _patternCtx = CreatePatternContext();

        static PatternContext[] CreatePatternContext()
        {
            var patternCtx = new PatternContext[NumChannels];
            for (int i = 0; i < patternCtx.Length; i++)
            {
                patternCtx[i] = new PatternContext();
            }
            return patternCtx;
        }

        class ChannelContext
        {
            public byte paulaChannel;

            //      byte    macroIndex;
            public ushort macroWait;
            public uint macroOffset;
            public uint macroReturnOffset;
            public ushort macroStep;
            public ushort macroReturnStep;
            public byte macroLoopCount;
            public bool macroRun;
            /// <summary>
            /// values are the folowing: -1 macro disabled, 0 macro init, 1 macro running
            /// </summary>
            public sbyte macroSfxRun;


            public uint customMacro;
            public byte customMacroIndex;
            public byte customMacroPrio;

            public bool sfxLocked;
            public short sfxLockTime;
            public bool keyUp;

            public bool deferWait;
            public ushort dmaIntCount;

            public int sampleStart;
            public ushort sampleLen;
            public ushort refPeriod;
            public ushort period;

            public sbyte volume;
            public byte relVol;
            public byte note;
            public byte prevNote;
            /// <summary>
            /// always a signextended byte
            /// </summary>
            public short fineTune;

            public byte portaSkip;
            public byte portaCount;
            public ushort portaDelta;
            public ushort portaValue;

            public byte envSkip;
            public byte envCount;
            public byte envDelta;
            public sbyte envEndVolume;

            public byte vibLength;
            public byte vibCount;
            public short vibValue;
            public sbyte vibDelta;

            public byte addBeginLength;
            public byte addBeginCount;
            public int addBeginDelta;
        }

        ChannelContext[] _channelCtx = CreateChannelContexts();

        static ChannelContext[] CreateChannelContexts()
        {
            var channelCtx = new ChannelContext[NumVoices];
            for (int i = 0; i < channelCtx.Length; i++)
            {
                channelCtx[i] = new ChannelContext();
            }
            return channelCtx;
        }

        class TrackStepContext
        {
            public ushort startInd;
            public ushort stopInd;
            public ushort posInd;
            public short loopCount;
        }

        TrackStepContext _trackCtx = new TrackStepContext();

        class PlayerContext
        {
            /// <summary>
            /// <>= 0 if Song is running (means process Patterns)
            /// </summary>
            public sbyte song;

            public ushort patternCount;
            /// <summary>
            /// skip that amount of CIA-Interrupts
            /// </summary>
            public ushort patternSkip;

            /// <summary>
            /// Master Volume
            /// </summary>
            public sbyte volume;

            public byte fadeSkip;
            public byte fadeCount;
            public sbyte fadeEndVolume;
            public sbyte fadeDelta;

            public int tickCount;

            public Action<int,ushort> signal;

            /// <summary>
            /// hack to automatically stop the whole player if no Pattern is running
            /// </summary>
            public bool stopWithLastPattern;
        }

        PlayerContext _playerCtx = new PlayerContext();

        static readonly ushort[] noteIntervalls =
            {
                1710, 1614, 1524, 1438, 1357, 1281, 1209, 1141, 1077, 1017,  960,  908,
                856,  810,  764,  720,  680,  642,  606,  571,  539,  509,  480,  454,
                428,  404,  381,  360,  340,  320,  303,  286,  270,  254,  240,  227,
                214,  202,  191,  180,  170,  160,  151,  143,  135,  127,  120,  113,
                214,  202,  191,  180,  170,  160,  151,  143,  135,  127,  120,  113,
                214,  202,  191,  180
            };
    }
}

