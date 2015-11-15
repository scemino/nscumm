//
//  TownsAudioInterfaceInternal.cs
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
using System.Collections.Generic;
using System.Diagnostics;

namespace NScumm.Core.Audio.SoftSynth
{
    class TownsAudioInterfaceInternal : TownsPC98_FmSynth
    {
        TownsAudioInterfaceInternal(IMixer mixer, TownsAudioInterface owner, ITownsAudioInterfacePluginDriver driver, bool externalMutexHandling)
            : base(mixer, FmSynthEmuType.Towns, externalMutexHandling)
        {
            _baserate = 55125.0f / mixer.OutputRate;
            _drv = driver;
            _drvOwner = owner;
            _musicVolume = Mixer.MaxMixerVolume;
            _sfxVolume = Mixer.MaxMixerVolume;

            _timerBase = (uint)(_baserate * 1000000.0f);
            _tickLength = 2 * _timerBase;

            _intfOpcodes = new Dictionary<int, TownsAudioIntfCallback>
            { 
                { 0,intf_reset },
                { 1,intf_keyOn },
                { 2,intf_keyOff },
                { 3,intf_setPanPos },
                // 4
                { 4,intf_setInstrument },
                { 5,intf_loadInstrument },
                { 7,intf_setPitch },
                // 8
                { 8,intf_setLevel },
                { 9,intf_chanOff },
                // 12
                // 16
                { 17,intf_writeReg },
                { 19,intf_writeRegBuffer },
                // 20
                { 20,intf_readRegBuffer },
                { 21,intf_setTimerA },
                { 22,intf_setTimerB },
                { 23,intf_enableTimerA },
                // 24
                { 24,intf_enableTimerB },
                // 28
                // 32
                { 32,intf_loadSamples },
                { 33,intf_reserveEffectChannels },
                { 34,intf_loadWaveTable },
                { 35,intf_unloadWaveTable },
                // 36
                { 37,intf_pcmPlayEffect },
                { 39,intf_pcmChanOff },
                // 40
                { 40,intf_pcmEffectPlaying },
                // 44
                // 48
                { 50,intf_fmKeyOn },
                { 51,intf_fmKeyOff },
                // 52
                { 52,intf_fmSetPanPos },
                { 53,intf_fmSetInstrument },
                { 54,intf_fmLoadInstrument },
                // 56
                { 56,intf_fmSetPitch },
                { 57,intf_fmSetLevel },
                { 58,intf_fmReset },
                // 60
                // 64
                { 67,intf_setOutputVolume },
                // 68
                { 68,intf_resetOutputVolume },
                { 69,intf_getOutputVolume },
                { 70,intf_setOutputMute },
                // 72
                { 73,intf_cdaToggle },
                { 74,intf_getOutputVolume2 },
                { 75,intf_getOutputMute },
                // 76
                // 80
                { 80,intf_pcmUpdateEnvelopeGenerator }
            };
        }

        public static TownsAudioInterfaceInternal AddNewRef(IMixer mixer, TownsAudioInterface owner, ITownsAudioInterfacePluginDriver driver, bool externalMutexHandling)
        {
            _refCount++;
            if (_refCount == 1 && _refInstance == null)
                _refInstance = new TownsAudioInterfaceInternal(mixer, owner, driver, externalMutexHandling);
            else if (_refCount < 2 || _refInstance == null)
                throw new InvalidOperationException("TownsAudioInterfaceInternal::addNewRef(): Internal reference management failure");
            else if (!_refInstance.AssignPluginDriver(owner, driver, externalMutexHandling))
                throw new InvalidOperationException("TownsAudioInterfaceInternal::addNewRef(): Plugin driver conflict");

            return _refInstance;
        }

        public static void ReleaseRef(TownsAudioInterface owner)
        {
            if (_refCount == 0)
                return;

            _refCount--;

            if (_refCount != 0)
            {
                if (_refInstance != null)
                    _refInstance.RemovePluginDriver(owner);
            }
            else if (_refInstance != null)
            {
                _refInstance.Dispose();
                _refInstance = null;
            }
        }

        public override bool Init()
        {
            if (_ready)
                return true;

            if (!base.Init())
                return false;

            _fmSaveReg[0] = new byte[256];
            _fmSaveReg[1] = new byte[256];
            _fmInstruments = new byte[128 * 48];
            _pcmInstruments = new byte[32][];
            _waveTables = new TownsAudio_WaveTable[128];
            for (int i = 0; i < _waveTables.Length; i++)
            {
                _waveTables[i] = new TownsAudio_WaveTable();
            }
            _pcmChan = new TownsAudio_PcmChannel[8];
            for (int i = 0; i < _pcmChan.Length; i++)
            {
                _pcmChan[i] = new TownsAudio_PcmChannel();
            }

            _timer = 0;

            SetVolumeChannelMasks(-1, 0);

            _ready = true;
            Callback(0);

            return true;
        }

        public void SetSoundEffectChanMask(int mask)
        {
            _pcmSfxChanMask = mask >> 6;
            mask &= 0x3f;
            SetVolumeChannelMasks(~mask, mask);
        }

        public void SetMusicVolume(int volume)
        {
            _musicVolume = (ushort)ScummHelper.Clip(volume, 0, Mixer.MaxMixerVolume);
            SetVolumeIntern(_musicVolume, _sfxVolume);
        }

        public override void TimerCallbackA()
        {
            if (_drv != null && _ready)
                _drv.TimerCallback(0);
        }

        public override void TimerCallbackB()
        {
            if (_ready)
            {
                if (_drv != null)
                    _drv.TimerCallback(1);
                Callback(80);
            }
        }

        public void SetSoundEffectVolume(int volume)
        {
            _sfxVolume = (ushort)ScummHelper.Clip(volume, 0, Mixer.MaxMixerVolume);
            SetVolumeIntern(_musicVolume, _sfxVolume);
        }

        public int ProcessCommand(int command, object[] args)
        {
            if (!_ready)
                return 1;

            if (command < 0 || command > 81 || !_intfOpcodes.ContainsKey(command))
                return 4;

            int res;
            lock (_mutex)
            {
                res = _intfOpcodes[command](args);
            }

            return res;
        }

        protected override void NextTickEx(int[] buffer, int offset, int bufferSize)
        {
            if (!_ready)
                return;

            if (_updateOutputVol)
                UpdateOutputVolumeInternal();

            for (var i = 0; i < bufferSize; i++)
            {
                _timer += _tickLength;
                while (_timer > 0x514767)
                {
                    _timer -= 0x514767;

                    for (int ii = 0; ii < 8; ii++)
                        _pcmChan[ii].UpdateOutput();
                }

                int finOutL = 0;
                int finOutR = 0;

                for (int ii = 0; ii < 8; ii++)
                {
                    if (_pcmChan[ii]._activeOutput)
                    {
                        int oL = _pcmChan[ii].CurrentSampleLeft();
                        int oR = _pcmChan[ii].CurrentSampleRight();
                        if (((1 << ii) & (~_pcmSfxChanMask)) != 0)
                        {
                            oL = (oR * _musicVolume) / Mixer.MaxMixerVolume;
                            oR = (oR * _musicVolume) / Mixer.MaxMixerVolume;
                        }
                        if (((1 << ii) & _pcmSfxChanMask) != 0)
                        {
                            oL = (oL * _sfxVolume) / Mixer.MaxMixerVolume;
                            oR = (oR * _sfxVolume) / Mixer.MaxMixerVolume;
                        }
                        finOutL += oL;
                        finOutR += oR;

                        if (!(_pcmChan[ii]._activeKey || _pcmChan[ii]._activeEffect))
                            _pcmChan[ii]._activeOutput = false;
                    }
                }

                buffer[offset + (i << 1)] += finOutL;
                buffer[offset + (i << 1) + 1] += finOutR;
            }
        }

        bool AssignPluginDriver(TownsAudioInterface owner, ITownsAudioInterfacePluginDriver driver, bool externalMutexHandling)
        {
            if (_refCount <= 1)
                return true;

            if (_drv != null)
            {
                if (driver != null && driver != _drv)
                    return false;
            }
            else
            {
                lock (_mutex)
                {
                    _drv = driver;
                    _drvOwner = owner;
                    _externalMutex = externalMutexHandling;
                }
            }

            return true;
        }

        void RemovePluginDriver(TownsAudioInterface owner)
        {
            if (_drvOwner == owner)
            {
                lock (_mutex)
                {
                    _drv = null;
                }
            }
        }

        void UpdateOutputVolume()
        {
            // Avoid calls to g_system->getAudioCDManager() functions from the main thread
            // since this can cause mutex lockups.
            _updateOutputVol = true;
        }

        void UpdateOutputVolumeInternal()
        {
            if (!_ready)
                return;

            // FM Towns seems to support volumes of 0 - 63 for each channel.
            // We recalculate sane values for our 0 to 255 volume range and
            // balance values for our -128 to 127 volume range

            // CD-AUDIO
            uint maxVol = (uint)Math.Max(_outputLevel[12] * (_outputMute[12] ^ 1), _outputLevel[13] * (_outputMute[13] ^ 1));

            int volume = (int)(((float)(maxVol * 255) / 63.0f));
            int balance = maxVol != 0 ? (int)((((int)_outputLevel[13] * (_outputMute[13] ^ 1) - _outputLevel[12] * (_outputMute[12] ^ 1)) * 127) / (float)maxVol) : 0;

            // TODO: audio cd
            //ScummEngine.Instance.AudioCDManager.Volume = volume;
            //ScummEngine.Instance.AudioCDManager.Balance = balance;

            _updateOutputVol = false;
        }

        void SetVolumeChannelMasks(int channelMaskA, int channelMaskB)
        {
            lock (_mutex)
            {
                _volMaskA = channelMaskA;
                _volMaskB = channelMaskB;
                if (_ssg != null)
                    _ssg.SetVolumeChannelMasks(_volMaskA >> _numChan, _volMaskB >> _numChan);
                #if !DISABLE_PC98_RHYTHM_CHANNEL
                if (_prc != null)
                    _prc.SetVolumeChannelMasks(_volMaskA >> (_numChan + _numSSG), _volMaskB >> (_numChan + _numSSG));
                #endif
            }
        }

        int Callback(int command, params object[] args)
        {
            if (!_ready)
                return 1;

            int res = ProcessCommand(command, args);
            return res;
        }

        void BufferedWriteReg(byte part, byte regAddress, byte value)
        {
            _fmSaveReg[part][regAddress] = value;
            WriteReg(part, regAddress, value);
        }


        int intf_reset(params object[] args)
        {
            FmReset();
            PcmReset();
            Callback(68);
            return 0;
        }

        int intf_keyOn(object[] args)
        {
            int chan = (int)args[0];
            int note = (int)args[1];
            int velo = (int)args[2];
            return ((chan & 0x40) != 0) ? PcmKeyOn(chan, note, velo) : FmKeyOn(chan, note, velo);
        }

        int intf_keyOff(object[] args)
        {
            int chan = (int)args[0];
            return ((chan & 0x40) != 0) ? PcmKeyOff(chan) : FmKeyOff(chan);
        }

        int intf_setPanPos(object[] args)
        {
            int chan = (int)args[0];
            int mode = (int)args[1];
            return ((chan & 0x40) != 0) ? PcmSetPanPos(chan, mode) : FmSetPanPos(chan, mode);
        }

        int intf_setInstrument(object[] args)
        {
            int chan = (int)args[0];
            int instrId = (int)args[1];
            return ((chan & 0x40) != 0) ? PcmSetInstrument(chan, instrId) : FmSetInstrument(chan, instrId);
        }

        int intf_loadInstrument(object[] args)
        {
            int chanType = (int)args[0];
            int instrId = (int)args[1];
            var instrData = (byte[])args[2];
            var instrDataOffset = (int)args[3];
            return ((chanType & 0x40) != 0) ? PcmLoadInstrument(instrId, instrData, instrDataOffset) : FmLoadInstrument(instrId, instrData, instrDataOffset);
        }

        int intf_setPitch(object[] args)
        {
            int chan = (int)args[0];
            short pitch = (short)((int)args[1] & 0xffff);
            return ((chan & 0x40) != 0) ? PcmSetPitch(chan, pitch) : FmSetPitch(chan, pitch);
        }

        int intf_setLevel(object[] args)
        {
            int chan = (int)args[0];
            int lvl = (int)args[1];
            return ((chan & 0x40) != 0) ? PcmSetLevel(chan, lvl) : FmSetLevel(chan, lvl);
        }

        int intf_chanOff(object[] args)
        {
            int chan = (int)args[0];
            return ((chan & 0x40) != 0) ? PcmChanOff(chan) : FmChanOff(chan);
        }

        int intf_writeReg(object[] args)
        {
            int part = ((int)args[0]) != 0 ? 1 : 0;
            int reg = (int)args[1];
            int val = (int)args[2];
            if ((part == 0 && reg < 0x20) || (part != 0 && reg < 0x30) || (reg > 0xb6))
                return 3;

            BufferedWriteReg((byte)part, (byte)reg, (byte)val);
            return 0;
        }

        int intf_writeRegBuffer(object[] args)
        {
            int part = ((int)args[0]) != 0 ? 1 : 0;
            int reg = (int)args[1];
            int val = (int)args[2];

            if ((part == 0 && reg < 0x20) || (part != 0 && reg < 0x30) || (reg > 0xef))
                return 3;

            _fmSaveReg[part][reg] = (byte)val;
            return 0;
        }

        int intf_readRegBuffer(object[] args)
        {
            int part = ((int)args[0]) != 0 ? 1 : 0;
            int reg = (int)args[1];
            var dst = (byte[])args[2];
            dst[0] = 0;

            if ((part == 0 && reg < 0x20) || (part != 0 && reg < 0x30) || (reg > 0xef))
                return 3;

            dst[0] = _fmSaveReg[part][reg];
            return 0;
        }

        int intf_setTimerA(object[] args)
        {
            int enable = (int)args[0];
            int tempo = (int)args[1];

            if (enable != 0)
            {
                BufferedWriteReg(0, 0x25, (byte)(tempo & 3));
                BufferedWriteReg(0, 0x24, (byte)((tempo >> 2) & 0xff));
                BufferedWriteReg(0, 0x27, (byte)(_fmSaveReg[0][0x27] | 0x05));
            }
            else
            {
                BufferedWriteReg(0, 0x27, (byte)((_fmSaveReg[0][0x27] & 0xfa) | 0x10));
            }

            return 0;
        }

        int intf_setTimerB(object[] args)
        {
            int enable = (int)args[0];
            int tempo = (int)args[1];

            if (enable != 0)
            {
                BufferedWriteReg(0, 0x26, (byte)(tempo & 0xff));
                BufferedWriteReg(0, 0x27, (byte)(_fmSaveReg[0][0x27] | 0x0A));
            }
            else
            {
                BufferedWriteReg(0, 0x27, (byte)((_fmSaveReg[0][0x27] & 0xf5) | 0x20));
            }

            return 0;
        }

        int intf_enableTimerA(object[] args)
        {
            BufferedWriteReg(0, 0x27, (byte)(_fmSaveReg[0][0x27] | 0x15));
            return 0;
        }

        int intf_enableTimerB(object[] args)
        {
            BufferedWriteReg(0, 0x27, (byte)(_fmSaveReg[0][0x27] | 0x2a));
            return 0;
        }

        int intf_loadSamples(object[] args)
        {
            uint dest = (uint)args[0];
            int size = (int)args[1];
            var src = (byte[])args[2];
            var srcOffset = (int)args[3];

            return intf_loadSamples(dest, size, src, srcOffset);
        }

        int intf_loadSamples(uint dest, int size, byte[] src, int offset)
        {
            if (dest >= 65536 || size == 0 || size > 65536)
                return 3;
            if (size + dest > 65536)
                return 5;

            int dwIndex = _numWaveTables - 1;
            for (uint t = (uint)_waveTablesTotalDataSize; dwIndex != 0 && (dest < t); dwIndex--)
                t -= (uint)_waveTables[dwIndex].size;

            TownsAudio_WaveTable s = _waveTables[dwIndex];
            _waveTablesTotalDataSize -= s.size;
            s.size = size;
            s.ReadData(src, offset);
            _waveTablesTotalDataSize += s.size;

            return 0;
        }

        int intf_reserveEffectChannels(object[] args)
        {
            int numChan = (int)args[0];
            if (numChan > 8)
                return 3;
            if ((numChan << 13) + _waveTablesTotalDataSize > 65536)
                return 5;

            if (numChan == _numReservedChannels)
                return 0;

            if (numChan < _numReservedChannels)
            {
                int c = 8 - _numReservedChannels;
                for (int i = numChan; i != 0; i--)
                    _pcmChan[c--]._activeEffect = false;
            }
            else
            {
                int c = 7 - _numReservedChannels;
                for (int i = numChan - _numReservedChannels; i != 0; i--)
                {
                    _pcmChan[c]._keyPressed = false;
                    _pcmChan[c--]._activeKey = false;
                }
            }

            _numReservedChannels = (byte)numChan;
            for (int i = 0; i < 8; i++)
                _pcmChan[i]._reserved = i >= (8 - _numReservedChannels) ? true : false;

            return 0;
        }

        int intf_loadWaveTable(object[] args)
        {
            var data = (byte[])args[0];
            if (_numWaveTables > 127)
                return 3;

            TownsAudio_WaveTable w = new TownsAudio_WaveTable();
            w.ReadHeader(data, 0);
            if (w.size == 0)
                return 6;

            if (_waveTablesTotalDataSize + w.size > 65504)
                return 5;

            for (int i = 0; i < _numWaveTables; i++)
            {
                if (_waveTables[i].id == w.id)
                    return 10;
            }

            var s = _waveTables[_numWaveTables++];
            s.ReadHeader(data, 0);

            _waveTablesTotalDataSize += s.size;
            Callback(32, _waveTablesTotalDataSize, s.size, data, 32);

            return 0;
        }

        int intf_unloadWaveTable(object[] args)
        {
            int id = (int)args[0];

            if (id == -1)
            {
                for (int i = 0; i < 128; i++)
                    _waveTables[i].Clear();
                _numWaveTables = 0;
                _waveTablesTotalDataSize = 0;
            }
            else
            {
                if (_waveTables != null)
                {
                    for (int i = 0; i < _numWaveTables;)
                    {
                        if (_waveTables[i].id == id)
                        {
                            _numWaveTables--;
                            _waveTablesTotalDataSize -= _waveTables[i].size;
                            _waveTables[i].Clear();
                            for (; i < _numWaveTables; i++)
                            {
                                _waveTables[i] = _waveTables[i + 1];
                            }
                            return 0;
                        }
                        return 9;
                    }
                }
            }

            return 0;
        }

        int intf_pcmPlayEffect(object[] args)
        {
            int chan = (int)args[0];
            int note = (int)args[1];
            int velo = (int)args[2];
            var data = (byte[])args[3];
            var dataOffset = (int)args[4];

            if (chan < 0x40 || chan > 0x47)
                return 1;

            if ((note & 0x80) != 0 || ((velo & 0x80) != 0))
                return 3;

            chan -= 0x40;

            if (!_pcmChan[chan]._reserved)
                return 7;

            if (_pcmChan[chan]._activeEffect)
                return 2;

            var w = new TownsAudio_WaveTable();
            w.ReadHeader(data, dataOffset);

            if (w.size < (w.loopStart + w.loopLen))
                return 13;

            if (w.size == 0)
                return 6;

            var p = _pcmChan[chan];

            p.LoadData(data, dataOffset + 32, w.size);
            p.KeyOn((byte)note, (byte)velo, w);

            return 0;
        }

        int intf_pcmChanOff(object[] args)
        {
            int chan = (int)args[0];
            PcmChanOff(chan);
            return 0;
        }

        int intf_pcmEffectPlaying(object[] args)
        {
            int chan = (int)args[0];
            if (chan < 0x40 || chan > 0x47)
                return 1;
            chan -= 0x40;
            return _pcmChan[chan]._activeEffect ? 1 : 0;
        }

        int intf_fmKeyOn(object[] args)
        {
            int chan = (int)args[0];
            int note = (int)args[1];
            int velo = (int)args[2];
            return FmKeyOn(chan, note, velo);
        }

        int intf_fmKeyOff(object[] args)
        {
            int chan = (int)args[0];
            return FmKeyOff(chan);
        }

        int intf_fmSetPanPos(object[] args)
        {
            int chan = (int)args[0];
            int mode = (int)args[1];
            return FmSetPanPos(chan, mode);
        }

        int intf_fmSetInstrument(object[] args)
        {
            int chan = (int)args[0];
            int instrId = (int)args[1];
            return FmSetInstrument(chan, instrId);
        }

        int intf_fmLoadInstrument(object[] args)
        {
            int instrId = (int)args[0];
            var instrData = (byte[])args[1];
            return FmLoadInstrument(instrId, instrData, 0);
        }

        int intf_fmSetPitch(object[] args)
        {
            int chan = (int)args[0];
            ushort freq = (ushort)(((int)args[1]) & 0xffff);
            return FmSetPitch(chan, freq);
        }

        int intf_fmSetLevel(object[] args)
        {
            int chan = (int)args[0];
            int lvl = (int)args[1];
            return FmSetLevel(chan, lvl);
        }

        int intf_fmReset(object[] args)
        {
            FmReset();
            return 0;
        }

        int intf_setOutputVolume(object[] args)
        {
            int chanType = (int)args[0];
            int left = (int)args[1];
            int right = (int)args[2];

            if (((left & 0xff80) != 0) || ((right & 0xff80) != 0))
                return 3;

            byte[] flags = { 0x0C, 0x30, 0x40, 0x80 };

            byte chan = ((chanType & 0x40) != 0) ? (byte)8 : (byte)12;

            chanType &= 3;
            left = (left & 0x7e) >> 1;
            right = (right & 0x7e) >> 1;

            if (chan == 12)
                _outputVolumeFlags |= flags[chanType];
            else
                _outputVolumeFlags = (byte)(_outputVolumeFlags & ~flags[chanType]);

            if (chanType > 1)
            {
                _outputLevel[chan + chanType] = (byte)left;
                _outputMute[chan + chanType] = 0;
            }
            else
            {
                if (chanType == 0)
                    chan -= 8;
                _outputLevel[chan] = (byte)left;
                _outputLevel[chan + 1] = (byte)right;
                _outputMute[chan] = _outputMute[chan + 1] = 0;
            }

            UpdateOutputVolume();

            return 0;
        }

        int intf_resetOutputVolume(object[] args)
        {
            Array.Clear(_outputLevel, 0, _outputLevel.Length);
            _outputVolumeFlags = 0;
            UpdateOutputVolume();
            return 0;
        }

        int intf_getOutputVolume(object[] args)
        {
            int chanType = (int)args[0];
            var left = (int[])args[1];
            var right = (int[])args[2];

            byte chan = ((chanType & 0x40) != 0) ? (byte)8 : (byte)12;
            chanType &= 3;

            if (chanType > 1)
            {
                left[0] = _outputLevel[chan + chanType] & 0x3f;
            }
            else
            {
                if (chanType == 0)
                    chan -= 8;
                left[0] = _outputLevel[chan] & 0x3f;
                right[0] = _outputLevel[chan + 1] & 0x3f;
            }

            return 0;
        }

        int intf_setOutputMute(object[] args)
        {
            int flags = (int)args[0];
            _outputVolumeFlags = (byte)flags;
            byte mute = (byte)(flags & 3);
            byte f = (byte)(flags & 0xff);

            _outputMute.Set(0, 1, 8);
            if ((mute & 2) != 0)
                _outputMute.Set(12, 1, 4);
            if ((mute & 1) != 0)
                _outputMute.Set(8, 1, 4);

            _outputMute[(f < 0x80) ? 11 : 15] = 0;
            f += f;
            _outputMute[(f < 0x80) ? 10 : 14] = 0;
            f += f;
            _outputMute[(f < 0x80) ? 8 : 12] = 0;
            f += f;
            _outputMute[(f < 0x80) ? 9 : 13] = 0;
            f += f;
            _outputMute[(f < 0x80) ? 0 : 4] = 0;
            f += f;
            _outputMute[(f < 0x80) ? 1 : 5] = 0;
            f += f;

            UpdateOutputVolume();
            return 0;
        }

        int intf_cdaToggle(object[] args)
        {
            //int mode = va_arg(args, int);
            //_unkMask = mode ? 0x7f : 0x3f;
            return 0;
        }

        int intf_getOutputVolume2(object[] args)
        {
            return 0;
        }

        int intf_getOutputMute(object[] args)
        {
            return 0;
        }

        int intf_pcmUpdateEnvelopeGenerator(object[] args)
        {
            for (int i = 0; i < 8; i++)
                _pcmChan[i].UpdateEnvelopeGenerator();
            return 0;
        }

        int intf_notImpl(object[] args)
        {
            return 4;
        }


        void PcmReset()
        {
            _numReservedChannels = 0;

            for (int i = 0; i < 8; i++)
                _pcmChan[i].Clear();

            for (int i = 0; i < 32; i++)
            {
                _pcmInstruments[i] = new byte[128];
                Array.Copy(name, _pcmInstruments[i], name.Length);
            }

            for (int i = 0; i < 128; i++)
                _waveTables[i].Clear();
            _numWaveTables = 0;
            _waveTablesTotalDataSize = 0;

            for (int i = 0x40; i < 0x48; i++)
            {
                PcmSetInstrument(i, 0);
                PcmSetLevel(i, 127);
            }
        }

        int PcmSetInstrument(int chan, int instrId)
        {
            if (chan > 0x47)
                return 1;
            if (instrId > 31)
                return 3;
            chan -= 0x40;
            _pcmChan[chan].SetInstrument(_pcmInstruments[instrId]);

            return 0;
        }

        int PcmLoadInstrument(int instrId, byte[] data, int offset)
        {
            if (instrId > 31)
                return 3;
            Debug.Assert(data != null);
            Array.Copy(data, offset, _pcmInstruments[instrId], 0, 128);
            return 0;
        }

        int PcmSetPitch(int chan, int pitch)
        {
            if (chan > 0x47)
                return 1;

            if (pitch < -8192 || pitch > 8191)
                return 3;

            chan -= 0x40;
            TownsAudio_PcmChannel p = _pcmChan[chan];

            uint pts = 0x4000;

            if (pitch < 0)
                pts = (uint)((0x20000000 / (-pitch + 0x2001)) >> 2);
            else if (pitch > 0)
                pts = (uint)((((pitch + 0x2001) << 16) / 0x2000) >> 2);

            p.SetPitch(pts);

            return 0;
        }

        int PcmSetLevel(int chan, int lvl)
        {
            if (chan > 0x47)
                return 1;

            if ((lvl & 0x80) != 0)
                return 3;

            chan -= 0x40;
            _pcmChan[chan].SetLevel(lvl);

            return 0;
        }

        int PcmChanOff(int chan)
        {
            if (chan < 0x40 || chan > 0x47)
                return 1;

            chan -= 0x40;
            _pcmChan[chan]._keyPressed = _pcmChan[chan]._activeEffect = _pcmChan[chan]._activeKey = _pcmChan[chan]._activeOutput = false;

            return 0;
        }

        int PcmKeyOn(int chan, int note, int velo)
        {
            if (chan < 0x40 || chan > 0x47)
                return 1;

            if (((note & 0x80) != 0) || ((velo & 0x80) != 0))
                return 3;

            chan -= 0x40;
            byte noteT = (byte)note;
            TownsAudio_PcmChannel p = _pcmChan[chan];

            if (p._reserved || p._keyPressed)
                return 2;

            TownsAudio_WaveTable w;
            int res = p.InitInstrument(ref noteT, _waveTables, _numWaveTables, out w);
            if (res != 0)
                return res;

            p.LoadData(w);
            p.KeyOn(noteT, (byte)velo, w);

            return 0;
        }

        int PcmKeyOff(int chan)
        {
            if (chan < 0x40 || chan > 0x47)
                return 1;

            chan -= 0x40;
            _pcmChan[chan].KeyOff();
            return 0;
        }

        int PcmSetPanPos(int chan, int mode)
        {
            if (chan > 0x47)
                return 1;
            if ((mode & 0x80) != 0)
                return 3;

            chan -= 0x40;
            byte blc = 0x77;

            if (mode > 64)
            {
                mode -= 64;
                blc = (byte)(((blc ^ (mode >> 3)) + (mode << 4)) & 0xff);
            }
            else if (mode < 64)
            {
                mode = (mode >> 3) ^ 7;
                blc = (byte)(((119 + mode) ^ (mode << 4)) & 0xff);
            }

            _pcmChan[chan].SetBalance(blc);

            return 0;
        }


        void FmReset()
        {
            Reset();

            _fmChanPlaying = 0;
            Array.Clear(_fmChanNote, 0, _fmChanNote.Length);
            Array.Clear(_fmChanPitch, 0, _fmChanPitch.Length);

            _fmSaveReg[0].Set(0, 0, 240);
            _fmSaveReg[0].Set(240, 0x7f, 16);
            _fmSaveReg[1].Set(0, 0, 256);
            _fmSaveReg[1].Set(240, 0x7f, 16);
            _fmSaveReg[0][243] = _fmSaveReg[0][247] = _fmSaveReg[0][251] = _fmSaveReg[0][255] = _fmSaveReg[1][243] = _fmSaveReg[1][247] = _fmSaveReg[1][251] = _fmSaveReg[1][255] = 0xff;

            for (int i = 0; i < 128; i++)
                FmLoadInstrument(i, _fmDefaultInstrument, 0);

            BufferedWriteReg(0, 0x21, 0);
            BufferedWriteReg(0, 0x2C, 0x80);
            BufferedWriteReg(0, 0x2B, 0);
            BufferedWriteReg(0, 0x27, 0x30);

            for (int i = 0; i < 6; i++)
            {
                FmKeyOff(i);
                FmSetInstrument(i, 0);
                FmSetLevel(i, 127);
            }
        }

        int FmSetLevel(int chan, int lvl)
        {
            if (chan > 5)
                return 1;
            if (lvl > 127)
                return 3;

            byte part = chan > 2 ? (byte)1 : (byte)0;
            if (chan > 2)
                chan -= 3;

            ushort c = _carrier[_fmSaveReg[part][0xb0 + chan] & 7];
            _fmSaveReg[part][0xd0 + chan] = (byte)lvl;

            for (var reg = 0x40 + chan; reg < 0x50; reg += 4)
            {
                c += c;
                if ((c & 0x100) != 0)
                {
                    c &= 0xff;
                    BufferedWriteReg(part, (byte)reg, (byte)((((((((_fmSaveReg[part][0x80 + reg] ^ 0x7f) * lvl) >> 7) + 1) * _fmSaveReg[part][0xe0 + chan]) >> 7) + 1) ^ 0x7f));
                }
            }
            return 0;
        }

        int FmChanOff(int chan)
        {
            if (chan > 5)
                return 1;
            _fmChanPlaying = (byte)(_fmChanPlaying & ~_chanFlags[chan]);

            byte part = (byte)(chan > 2 ? 1 : 0);
            if (chan > 2)
                chan -= 3;

            for (var reg = 0x80 + chan; reg < 0x90; reg += 4)
                WriteReg(part, (byte)reg, (_fmSaveReg[part][reg] | 0x0f));

            if (part != 0)
                chan += 4;
            WriteReg(0, 0x28, chan);
            return 0;
        }

        int FmLoadInstrument(int instrId, byte[] data, int offset)
        {
            if (instrId > 127)
                return 3;
            Debug.Assert(data != null);
            Array.Copy(data, offset, _fmInstruments, instrId * 48, 48);
            return 0;
        }

        int FmSetInstrument(int chan, int instrId)
        {
            if (chan > 5)
                return 1;
            if (instrId > 127)
                return 3;

            byte part = chan > 2 ? (byte)1 : (byte)0;
            if (chan > 2)
                chan -= 3;

            var src = _fmInstruments;
            var srcOffset = instrId * 48 + 8;

            ushort c = _carrier[src[srcOffset + 24] & 7];
            byte reg = (byte)(0x30 + chan);

            for (; reg < 0x40; reg += 4)
                BufferedWriteReg(part, reg, src[srcOffset++]);

            byte v;
            for (; reg < 0x50; reg += 4)
            {
                v = src[srcOffset++];
                _fmSaveReg[part][0x80 + reg] = _fmSaveReg[part][reg] = v;
                c += c;
                if ((c & 0x100) != 0)
                {
                    c &= 0xff;
                    v = 127;
                }
                WriteReg(part, reg, v);
            }

            for (; reg < 0x90; reg += 4)
                BufferedWriteReg(part, reg, src[srcOffset++]);

            reg += 0x20;
            BufferedWriteReg(part, reg, src[srcOffset++]);

            v = src[srcOffset++];
            reg += 4;
            if (v < 64)
                v |= ((byte)(_fmSaveReg[part][reg] & 0xc0));
            BufferedWriteReg(part, reg, v);

            return 0;
        }

        int FmKeyOn(int chan, int note, int velo)
        {
            if (chan > 5)
                return 1;
            if (note < 12 || note > 107 || ((velo & 0x80) != 0))
                return 3;
            if (_fmChanPlaying != 0 & _chanFlags[chan] != 0)
                return 2;

            _fmChanPlaying |= _chanFlags[chan];
            note -= 12;

            _fmChanNote[chan] = (byte)note;
            short pitch = _fmChanPitch[chan];

            byte part = chan > 2 ? (byte)1 : (byte)0;
            if (chan > 2)
                chan -= 3;

            int frq = 0;
            byte bl = 0;

            if (note != 0)
            {
                frq = _frequency[(note - 1) % 12];
                bl = (byte)((note - 1) / 12);
            }
            else
            {
                frq = 616;
            }

            frq += pitch;

            if (frq < 616)
            {
                if (bl == 0)
                {
                    frq = 616;
                }
                else
                {
                    frq += 616;
                    --bl;
                }
            }
            else if (frq > 1232)
            {
                if (bl == 7)
                {
                    frq = 15500;
                }
                else
                {
                    frq -= 616;
                    ++bl;
                }
            }

            frq |= (bl << 11);

            BufferedWriteReg(part, (byte)(chan + 0xa4), (byte)((frq >> 8) & 0xff));
            BufferedWriteReg(part, (byte)(chan + 0xa0), (byte)(frq & 0xff));

            velo = (velo >> 2) + 96;
            ushort c = _carrier[_fmSaveReg[part][0xb0 + chan] & 7];
            _fmSaveReg[part][0xe0 + chan] = (byte)velo;

            for (var reg = 0x40 + chan; reg < 0x50; reg += 4)
            {
                c += c;
                if ((c & 0x100) != 0)
                {
                    c &= 0xff;
                    BufferedWriteReg(part, (byte)reg, (byte)((((((((_fmSaveReg[part][0x80 + reg] ^ 0x7f) * velo) >> 7) + 1) * _fmSaveReg[part][0xd0 + chan]) >> 7) + 1) ^ 0x7f));
                }
            }

            byte v = (byte)chan;
            if (part != 0)
                v |= 4;

            for (var reg = 0x80 + chan; reg < 0x90; reg += 4)
                WriteReg(part, (byte)reg, (_fmSaveReg[part][reg] | 0x0f));

            WriteReg(0, 0x28, v);

            for (var reg = 0x80 + chan; reg < 0x90; reg += 4)
                WriteReg(part, (byte)reg, _fmSaveReg[part][reg]);

            BufferedWriteReg(0, 0x28, (byte)(v | 0xf0));

            return 0;
        }

        int FmKeyOff(int chan)
        {
            if (chan > 5)
                return 1;
            _fmChanPlaying = (byte)(_fmChanPlaying & ~_chanFlags[chan]);
            if (chan > 2)
                chan++;
            BufferedWriteReg(0, 0x28, (byte)chan);
            return 0;
        }

        int FmSetPanPos(int chan, int value)
        {
            if (chan > 5)
                return 1;

            byte part = chan > 2 ? (byte)1 : (byte)0;
            if (chan > 2)
                chan -= 3;

            if (value > 0x40)
                value = 0x40;
            else if (value < 0x40)
                value = 0x80;
            else
                value = 0xC0;

            BufferedWriteReg(part, (byte)(0xb4 + chan), (byte)((_fmSaveReg[part][0xb4 + chan] & 0x3f) | value));
            return 0;
        }

        int FmSetPitch(int chan, int pitch)
        {
            if (chan > 5)
                return 1;

            int bl = _fmChanNote[chan];
            int frq = 0;

            if (pitch < 0)
            {
                if (bl != 0)
                {
                    if (pitch < -8008)
                        pitch = -8008;
                    pitch *= -1;
                    pitch /= 13;
                    frq = _frequency[(bl - 1) % 12] - pitch;
                    bl = ((bl - 1) / 12);
                    _fmChanPitch[chan] = (short)-pitch;

                    if (frq < 616)
                    {
                        if (bl != 0)
                        {
                            frq += 616;
                            bl--;
                        }
                        else
                        {
                            frq = 616;
                            bl = 0;
                        }
                    }
                }
                else
                {
                    frq = 616;
                    bl = 0;
                }

            }
            else if (pitch > 0)
            {
                if (bl < 96)
                {
                    if (pitch > 8008)
                        pitch = 8008;
                    pitch /= 13;

                    if (bl != 0)
                    {
                        frq = _frequency[(bl - 1) % 12] + pitch;
                        bl = ((bl - 1) / 12);
                    }
                    else
                    {
                        frq = 616;
                        bl = 0;
                    }

                    _fmChanPitch[chan] = (short)pitch;

                    if (frq > 1232)
                    {
                        if (bl < 7)
                        {
                            frq -= 616;
                            bl++;
                        }
                        else
                        {
                            frq = 1164;
                            bl = 7;
                        }
                    }
                    else
                    {
                        if (bl >= 7 && frq > 1164)
                            frq = 1164;
                    }

                }
                else
                {
                    frq = 1164;
                    bl = 7;
                }
            }
            else
            {
                _fmChanPitch[chan] = 0;
                if (bl != 0)
                {
                    frq = _frequency[(bl - 1) % 12];
                    bl = ((bl - 1) / 12);
                }
                else
                {
                    frq = 616;
                    bl = 0;
                }
            }

            byte part = chan > 2 ? (byte)1 : (byte)0;
            if (chan > 2)
                chan -= 3;

            frq |= (bl << 11);

            BufferedWriteReg(part, (byte)(chan + 0xa4), (byte)(frq >> 8));
            BufferedWriteReg(part, (byte)(chan + 0xa0), (byte)(frq & 0xff));

            return 0;
        }


        static int _refCount;
        static TownsAudioInterfaceInternal _refInstance;
        object _mutex = new object();
        ITownsAudioInterfacePluginDriver _drv;

        byte _fmChanPlaying;
        byte[] _fmChanNote = new byte[6];
        short[] _fmChanPitch = new short[6];
        byte[][] _fmSaveReg = new byte[2][];
        byte[] _fmInstruments;

        delegate int TownsAudioIntfCallback(params object[] args);

        Dictionary<int,TownsAudioIntfCallback> _intfOpcodes;

        readonly float _baserate;
        uint _timerBase;
        uint _tickLength;
        uint _timer;

        ushort _musicVolume;
        ushort _sfxVolume;
        int _pcmSfxChanMask;

        TownsAudioInterface _drvOwner;
        bool _ready;

        TownsAudio_PcmChannel[] _pcmChan;

        byte _numReservedChannels;
        byte[][] _pcmInstruments = new byte[32][];

        TownsAudio_WaveTable[] _waveTables;
        byte _numWaveTables;
        int _waveTablesTotalDataSize;

        byte _outputVolumeFlags;
        byte[] _outputLevel = new byte[16];
        byte[] _outputMute = new byte[16];
        bool _updateOutputVol;

        readonly static byte[] name = { 0x4E, 0x6F, 0x20, 0x44, 0x61, 0x74, 0x61, 0x21 };

        static readonly byte[] _chanFlags =
            {
                0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80
            };

        static readonly byte[] _carrier =
            {
                0x10, 0x10, 0x10, 0x10, 0x30, 0x70, 0x70, 0xF0
            };

        static readonly byte[] _fmDefaultInstrument =
            {
                0x45, 0x4C, 0x45, 0x50, 0x49, 0x41, 0x4E, 0x4F, 0x01, 0x0A, 0x02, 0x01,
                0x1E, 0x32, 0x05, 0x00, 0x9C, 0xDC, 0x9C, 0xDC, 0x07, 0x03, 0x14, 0x08,
                0x00, 0x03, 0x05, 0x05, 0x55, 0x45, 0x27, 0xA7, 0x04, 0xC0, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

        static readonly ushort[] _frequency =
            {
                0x028C, 0x02B4, 0x02DC, 0x030A, 0x0338, 0x0368, 0x039C, 0x03D4, 0x040E, 0x044A, 0x048C, 0x04D0
            };

    }
}
