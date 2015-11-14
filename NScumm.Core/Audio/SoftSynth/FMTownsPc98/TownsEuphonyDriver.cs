//
//  TownsEuphonyDriver.cs
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

namespace NScumm.Core.Audio.SoftSynth
{
    public class TownsEuphonyDriver: ITownsAudioInterfacePluginDriver
    {
        public TownsAudioInterface Interface { get { return _intf; } }

        public TownsEuphonyDriver(IMixer mixer)
        {
            _intf = new TownsAudioInterface(mixer, this);
            ResetTempo();
        }

        public void StopParser()
        {
            if (_playing)
            {
                _playing = false;
                _pulseCount = 0;
                _endOfTrack = false;
                FlushEventBuffer();
                ResetControl();
            }
        }

        public void SetMusicVolume(int volume)
        {
            _intf.SetMusicVolume(volume);
        }

        public bool SoundEffectIsPlaying(int chan)
        {
            return _intf.Callback(40, chan);
        }

        public void StopSoundEffect(int chan)
        {
            _intf.Callback(39, chan);
        }

        public bool Init()
        {
            if (!_intf.Init())
                return false;

            _activeChannels = new sbyte[16];
            _sustainChannels = new sbyte[16];
            _assignedChannels = new ActiveChannel[128];
            for (int i = 0; i < _assignedChannels.Length; i++)
            {
                _assignedChannels[i] = new ActiveChannel();
            }
            _eventBuffer = new DlEvent[64];

            _tEnable = new byte[32];
            _tMode = new byte[32];
            _tOrdr = new byte[32];
            _tLevel = new sbyte[32];
            _tTranspose = new sbyte[32];

            Reset();

            return true;
        }

        public void PlaySoundEffect(int chan, byte note, byte velo, byte[] data)
        {
            _intf.Callback(37, chan, note, velo, data);
        }

        public void ContinueParsing()
        {
            _suspendParsing = false;
        }

        public void SetMusicLoop(bool loop)
        {
            _loop = loop;
        }

        public int StartMusicTrack(byte[] data, int offset, int trackSize, int startTick)
        {
            if (_playing)
                return 2;

            _musicPos = _musicStart = offset;
            _musicData = data;
            _defaultBaseTickLen = _baseTickLen = (byte)startTick;
            _musicTrackSize = trackSize;
            _timeStampBase = _timeStampDest = 0;
            _tickCounter = 0;
            _playing = true;

            return 0;
        }

        public int SetMusicTempo(int tempo)
        {
            if (tempo > 250)
                return 3;
            _defaultTempo = (byte)tempo;
            _trackTempo = tempo;
            SetTempoIntern(tempo);
            return 0;
        }

        public void ChanVolume(int chan, int vol)
        {
            _intf.Callback(8, chan, vol);
        }

        public int ConfigChanEnable(int tableEntry, int val)
        {
            if (tableEntry > 31)
                return 3;
            _tEnable[tableEntry] = (byte)val;
            return 0;
        }

        public int ConfigChanSetMode(int tableEntry, int val)
        {
            if (tableEntry > 31)
                return 3;
            _tMode[tableEntry] = (byte)val;
            return 0;
        }

        public int ConfigChanRemap(int tableEntry, int val)
        {
            if (tableEntry > 31)
                return 3;
            if (val < 16)
                _tOrdr[tableEntry] = (byte)val;
            return 0;
        }

        public int ConfigChanAdjustVolume(int tableEntry, int val)
        {
            if (tableEntry > 31)
                return 3;
            if (val <= 40)
                _tLevel[tableEntry] = (sbyte)(val & 0xff);
            return 0;
        }

        public int AssignChannel(int chan, int tableEntry)
        {
            if (tableEntry > 15 || chan > 127 || chan < 0)
                return 3;

            ActiveChannel a = _assignedChannels[chan];
            if (a.chan == tableEntry)
                return 0;

            if (a.chan != -1)
            {
                var c = new Action<sbyte>(i => _activeChannels[a.chan] = i);
                sbyte b = _activeChannels[a.chan];
                while (b != chan)
                {
                    c = new Action<sbyte>(i => _assignedChannels[b].next = i);
                    b = _assignedChannels[b].next;
                    if (b == -1 && b != chan)
                        return 3;
                }

                c(a.next);

                if (a.note != 0)
                    _intf.Callback(2, chan);

                a.chan = a.next = -1;
                a.note = 0;
            }

            a.next = _activeChannels[tableEntry];
            _activeChannels[tableEntry] = (sbyte)chan;
            a.chan = (sbyte)tableEntry;
            a.note = a.sub = 0;

            return 0;
        }

        public void LoadInstrument(int chanType, int id, byte[] data, int offset)
        {
            _intf.Callback(5, chanType, id, data, offset);
        }

        public void TimerCallback(int timerId)
        {
            switch (timerId)
            {
                case 0:
                    UpdatePulseCount();
                    while (_pulseCount > 0)
                    {
                        --_pulseCount;
                        UpdateTimeStampBase();
                        if (!_playing)
                            continue;
                        UpdateEventBuffer();
                        UpdateParser();
                        UpdateCheckEot();
                    }
                    break;
            }
        }

        public int ConfigChanSetTranspose(int tableEntry, int val)
        {
            if (tableEntry > 31)
                return 3;
            if (val <= 40)
                _tTranspose[tableEntry] = (sbyte)(val & 0xff);
            return 0;
        }

        public void ReserveSoundEffectChannels(int num)
        {
            _intf.Callback(33, num);
            int volMask = 0;

            if (num > 8)
                return;

            for (var v = 1 << 13; num != 0; num--)
            {
                volMask |= v;
                v >>= 1;
            }

            _intf.SetSoundEffectChanMask(volMask);
        }

        void UpdateEventBuffer()
        {
            var ei = 0;
            for (int i = _bufferedEventsCount; i != 0; ei++)
            {
                DlEvent e = _eventBuffer[ei];
                if (e.evt == 0)
                    continue;
                if ((--e.len) != 0)
                {
                    --i;
                    continue;
                }
                ProcessBufferNote(e.mode, e.evt, e.note, e.velo);
                e.evt = 0;
                --i;
                --_bufferedEventsCount;
            }
        }

        void UpdatePulseCount()
        {
            int tc = _extraTimingControl + _extraTimingControlRemainder;
            _extraTimingControlRemainder = tc & 0x0f;
            tc >>= 4;
            _tempoDiff -= (sbyte)tc;

            while (_tempoDiff < 0)
            {
                _elapsedEvents++;
                _tempoDiff += 4;
            }

            if (_playing && !_suspendParsing)
                _pulseCount += (uint)tc;
        }

        void UpdateTimeStampBase()
        {
            ushort[] table = { 0x180, 0xC0, 0x80, 0x60, 0x40, 0x30, 0x20, 0x18 };
            if ((uint)(table[_baseTickLen >> 4] * ((_baseTickLen & 0x0f) + 1)) > ++_tickCounter)
                return;
            ++_timeStampDest;
            _tickCounter = 0;
        }

        void UpdateParser()
        {
            for (bool loop = true; loop;)
            {
                byte cmd = _musicData[_musicPos];

                if (cmd == 0xff || cmd == 0xf7)
                {
                    JumpNextLoop();

                }
                else if (cmd < 0x90)
                {
                    _endOfTrack = true;
                    FlushEventBuffer();
                    loop = false;

                }
                else if (_timeStampBase > _timeStampDest)
                {
                    loop = false;

                }
                else
                {
                    if (_timeStampBase == _timeStampDest)
                    {
                        ushort timeStamp = _musicData.ToUInt16(_musicPos + 2);
                        byte l = (byte)((timeStamp & 0xff) + (timeStamp & 0xff));
                        timeStamp = (ushort)(((timeStamp & 0xff00) | l) >> 1);
                        if (timeStamp > _tickCounter)
                            loop = false;
                    }

                    if (loop)
                    {
                        if (ParseNext())
                            loop = false;
                    }
                }
            }
        }

        delegate bool EuphonyOpcode();

        bool evtNotImpl()
        {
            return false;
        }

        bool ParseNext()
        {

            EuphonyOpcode[] opcodes =
                {
                    evtNotImpl,
                    evtSetupNote,
                    evtPolyphonicAftertouch,
                    evtControlPitch,
                    evtInstrumentChanAftertouch,
                    evtInstrumentChanAftertouch,
                    evtControlPitch
                };

            uint cmd = _musicData[_musicPos];
            if (cmd != 0xfe && cmd != 0xfd)
            {
                if (cmd >= 0xf0)
                {
                    cmd &= 0x0f;
                    if (cmd == 0)
                        evtLoadInstrument();
                    else if (cmd == 2)
                        evtAdvanceTimestampOffset();
                    else if (cmd == 8)
                        evtTempo();
                    else if (cmd == 12)
                        evtModeOrdrChange();
                    JumpNextLoop();
                    return false;

                }
                else if (!(opcodes[(cmd - 0x80) >> 4])())
                {
                    JumpNextLoop();
                    return false;
                }
            }

            if (cmd == 0xfd)
            {
                _suspendParsing = true;
                return true;
            }

            if (!_loop)
            {
                _endOfTrack = true;
                return true;
            }

            _endOfTrack = false;
            _musicPos = _musicStart;
            _timeStampBase = _timeStampDest = _tickCounter = 0;
            _baseTickLen = _defaultBaseTickLen;

            return false;
        }

        bool evtSetupNote()
        {
            if (_musicData[_musicPos + 1] > 31)
                return false;
            if (_tEnable[_musicData[_musicPos + 1]] == 0)
            {
                JumpNextLoop();
                return (_musicData[_musicPos + 0] == 0xfe || _musicData[_musicPos + 0] == 0xfd);
            }
            byte evt = AppendEvent(_musicData[_musicPos + 0], _musicData[_musicPos + 1]);
            byte mode = _tMode[_musicData[_musicPos + 1]];
            byte note = _musicData[_musicPos + 4];
            byte velo = _musicData[_musicPos + 5];

            SendEvent(mode, evt);
            SendEvent(mode, ApplyTranspose(note));
            SendEvent(mode, ApplyVolumeAdjust(velo));

            JumpNextLoop();
            if (_musicData[_musicPos + 0] == 0xfe || _musicData[_musicPos + 0] == 0xfd)
                return true;

            velo = _musicData[_musicPos + 5];
            ushort len = (ushort)(((((_musicData[_musicPos + 1] << 4) | (_musicData[_musicPos + 2] << 8)) >> 4) & 0xff) | ((((_musicData[_musicPos + 3] << 4) | (_musicData[_musicPos + 4] << 8)) >> 4) << 8));

            int i = 0;
            for (; i < 64; i++)
            {
                if (_eventBuffer[i].evt == 0)
                    break;
            }

            if (i == 64)
            {
                ProcessBufferNote(mode, evt, note, velo);
            }
            else
            {
                _eventBuffer[i].evt = evt;
                _eventBuffer[i].mode = mode;
                _eventBuffer[i].note = note;
                _eventBuffer[i].velo = velo;
                _eventBuffer[i].len = (ushort)(len != 0 ? len : 1);
                _bufferedEventsCount++;
            }

            return false;
        }

        byte AppendEvent(byte evt, byte chan)
        {
            if (evt >= 0x80 && evt < 0xf0 && _tOrdr[chan] < 16)
                return (byte)((evt & 0xf0) | _tOrdr[chan]);
            return evt;
        }

        byte ApplyTranspose(byte @in)
        {
            int @out = _tTranspose[_musicData[_musicPos + 1]];
            if (@out == 0)
                return @in;
            @out += (@in & 0x7f);

            if (@out > 127)
                @out -= 12;

            if (@out < 0)
                @out += 12;

            return (byte)(@out & 0xff);
        }

        byte ApplyVolumeAdjust(byte @in)
        {
            int @out = _tLevel[_musicData[_musicPos + 1]];
            @out += (@in & 0x7f);
            @out = ScummHelper.Clip(@out, 1, 127);

            return (byte)(@out & 0xff);
        }

        bool evtPolyphonicAftertouch()
        {
            if (_musicData[_musicPos + 1] > 31)
                return false;
            if (_tEnable[_musicData[_musicPos + 1]] == 0)
                return false;

            byte evt = AppendEvent(_musicData[_musicPos + 0], _musicData[_musicPos + 1]);
            byte mode = _tMode[_musicData[_musicPos + 1]];

            SendEvent(mode, evt);
            SendEvent(mode, ApplyTranspose(_musicData[_musicPos + 4]));
            SendEvent(mode, _musicData[_musicPos + 5]);

            return false;
        }

        bool evtControlPitch()
        {
            if (_musicData[_musicPos + 1] > 31)
                return false;
            if (_tEnable[_musicData[_musicPos + 1]] == 0)
                return false;

            byte evt = AppendEvent(_musicData[_musicPos + 0], _musicData[_musicPos + 1]);
            byte mode = _tMode[_musicData[_musicPos + 1]];

            SendEvent(mode, evt);
            SendEvent(mode, _musicData[_musicPos + 4]);
            SendEvent(mode, _musicData[_musicPos + 5]);

            return false;
        }

        bool evtInstrumentChanAftertouch()
        {
            if (_musicData[_musicPos + 1] > 31)
                return false;
            if (_tEnable[_musicData[_musicPos + 1]] == 0)
                return false;

            byte evt = AppendEvent(_musicData[_musicPos + 0], _musicData[_musicPos + 1]);
            byte mode = _tMode[_musicData[_musicPos + 1]];

            SendEvent(mode, evt);
            SendEvent(mode, _musicData[_musicPos + 4]);

            return false;
        }

        bool evtLoadInstrument()
        {
            return false;
        }

        bool evtAdvanceTimestampOffset()
        {
            ++_timeStampBase;
            _baseTickLen = _musicData[_musicPos + 1];
            return false;
        }

        bool evtTempo()
        {
            byte l = (byte)(_musicData[_musicPos + 4] << 1);
            _trackTempo = (l | (_musicData[_musicPos + 5] << 8)) >> 1;
            SetTempoIntern(_trackTempo);
            return false;
        }

        bool evtModeOrdrChange()
        {
            if (_musicData[_musicPos + 1] > 31)
                return false;
            if (_tEnable[_musicData[_musicPos + 1]] == 0)
                return false;

            if (_musicData[_musicPos + 4] == 1)
                _tMode[_musicData[_musicPos + 1]] = _musicData[_musicPos + 5];
            else if (_musicData[_musicPos + 4] == 2)
                _tOrdr[_musicData[_musicPos + 1]] = _musicData[_musicPos + 5];

            return false;
        }

        void FlushEventBuffer()
        {
            int ei = 0;
            for (int i = _bufferedEventsCount; i != 0; ei++)
            {
                DlEvent e = _eventBuffer[ei];
                if (e.evt == 0)
                    continue;
                ProcessBufferNote(e.mode, e.evt, e.note, e.velo);
                e.evt = 0;
                --i;
                --_bufferedEventsCount;
            }
        }

        void ProcessBufferNote(int mode, int evt, int note, int velo)
        {
            if (velo == 0)
                evt &= 0x8f;
            SendEvent(mode, evt);
            SendEvent(mode, note);
            SendEvent(mode, velo);
        }

        void JumpNextLoop()
        {
            _musicPos += 6;
            if (_musicPos >= _musicStart + _musicTrackSize)
                _musicPos = _musicStart;
        }

        void UpdateCheckEot()
        {
            if (!_endOfTrack || _bufferedEventsCount != 0)
                return;
            StopParser();
        }

        void ResetTempo()
        {
            _defaultBaseTickLen = _baseTickLen = 0x33;
            _pulseCount = 0;
            _extraTimingControlRemainder = 0;
            _extraTimingControl = 16;
            _tempoModifier = 0;
            _timeStampDest = 0;
            _tickCounter = 0;
            _defaultTempo = 90;
            _trackTempo = 90;
        }

        void Reset()
        {
            _intf.Callback(0);

            _intf.Callback(74);
            _intf.Callback(70, 0);
            _intf.Callback(75, 3);

            SetTimerA(true, 1);
            SetTimerA(false, 1);
            SetTimerB(true, 221);

            _paraCount = _command = _para[0] = _para[1] = 0;
            Array.Clear(_sustainChannels, 0, 16);
            for (int i = 0; i < 16; i++)
            {
                _activeChannels[i] = -1;
            }
            for (int i = 0; i < 128; i++)
            {
                _assignedChannels[i].chan = _assignedChannels[i].next = -1;
                _assignedChannels[i].note = _assignedChannels[i].sub = 0;
            }

            int e = 0;
            for (int i = 0; i < 6; i++)
                AssignChannel(i, e++);
            for (int i = 0x40; i < 0x48; i++)
                AssignChannel(i, e++);

            ResetTables();

            for (int i = 0; i < 64; i++)
            {
                _eventBuffer[i] = new DlEvent();
            }
            _bufferedEventsCount = 0;

            _playing = _endOfTrack = _suspendParsing = _loop = false;
            _elapsedEvents = 0;
            _tempoDiff = 0;

            ResetTempo();

            SetTempoIntern(_defaultTempo);

            ResetControl();
        }

        void ResetControl()
        {
            for (int i = 0; i < 32; i++)
            {
                if (_tOrdr[i] > 15)
                {
                    for (int ii = 0; ii < 16; ii++)
                        ResetControlIntern(_tMode[i], ii);
                }
                else
                {
                    ResetControlIntern(_tMode[i], _tOrdr[i]);
                }
            }
        }

        void ResetControlIntern(int mode, int chan)
        {
            SendEvent(mode, 0xb0 | chan);
            SendEvent(mode, 0x40);
            SendEvent(mode, 0);
            SendEvent(mode, 0xb0 | chan);
            SendEvent(mode, 0x7b);
            SendEvent(mode, 0);
            SendEvent(mode, 0xb0 | chan);
            SendEvent(mode, 0x79);
            SendEvent(mode, 0x40);
        }

        void SendEvent(int mode, int command)
        {
            if (mode == 0)
            {
                // warning("TownsEuphonyDriver: Mode 0 not implemented");

            }
            else if (mode == 0x10)
            {
                Debug.WriteLine("TownsEuphonyDriver: Mode 0x10 not implemented");

            }
            else if (mode == 0xff)
            {
                if (command >= 0xf0)
                {
                    _paraCount = 1;
                    _command = 0;
                }
                else if (command >= 0x80)
                {
                    _paraCount = 1;
                    _command = (byte)command;
                }
                else if (_command >= 0x80)
                {
                    switch ((_command - 0x80) >> 4)
                    {
                        case 0:
                            if (_paraCount < 2)
                            {
                                _paraCount++;
                                _para[0] = (byte)command;
                            }
                            else
                            {
                                _paraCount = 1;
                                _para[1] = (byte)command;
                                SendNoteOff();
                            }
                            break;

                        case 1:
                            if (_paraCount < 2)
                            {
                                _paraCount++;
                                _para[0] = (byte)command;
                            }
                            else
                            {
                                _paraCount = 1;
                                _para[1] = (byte)command;
                                if (command != 0)
                                    SendNoteOn();
                                else
                                    SendNoteOff();
                            }
                            break;

                        case 2:
                            if (_paraCount < 2)
                            {
                                _paraCount++;
                                _para[0] = (byte)command;
                            }
                            else
                            {
                                _paraCount = 1;
                            }
                            break;

                        case 3:
                            if (_paraCount < 2)
                            {
                                _paraCount++;
                                _para[0] = (byte)command;
                            }
                            else
                            {
                                _paraCount = 1;
                                _para[1] = (byte)command;

                                if (_para[0] == 7)
                                    SendChanVolume();
                                else if (_para[0] == 10)
                                    SendPanPosition();
                                else if (_para[0] == 64)
                                    SendAllNotesOff();
                            }
                            break;

                        case 4:
                            _paraCount = 1;
                            _para[0] = (byte)command;
                            SendSetInstrument();
                            break;

                        case 5:
                            _paraCount = 1;
                            _para[0] = (byte)command;
                            break;

                        case 6:
                            if (_paraCount < 2)
                            {
                                _paraCount++;
                                _para[0] = (byte)command;
                            }
                            else
                            {
                                _paraCount = 1;
                                _para[1] = (byte)command;
                                SendPitch();
                            }
                            break;
                    }
                }
            }
        }

        void SendNoteOff()
        {
            var chan = _activeChannels[_command & 0x0f];
            if (chan == -1)
                return;

            while (_assignedChannels[chan].note != _para[0])
            {
                chan = _assignedChannels[chan].next;
                if (chan == -1)
                    return;
            }

            if (_sustainChannels[_command & 0x0f] != 0)
            {
                _assignedChannels[chan].note |= 0x80;
            }
            else
            {
                _assignedChannels[chan].note = 0;
                _intf.Callback(2, chan);
            }
        }

        void SendNoteOn()
        {
            if (_para[0] == 0)
                return;
            var chan = _activeChannels[_command & 0x0f];
            if (chan == -1)
                return;

            do
            {
                _assignedChannels[chan].sub++;
                chan = _assignedChannels[chan].next;
            } while (chan != -1);

            chan = _activeChannels[_command & 0x0f];

            int d = 0;
            int c = 0;
            bool found = false;

            do
            {
                if (_assignedChannels[chan].note == 0)
                {
                    found = true;
                    break;
                }
                if (d <= _assignedChannels[chan].sub)
                {
                    c = chan;
                    d = _assignedChannels[chan].sub;
                }
                chan = _assignedChannels[chan].next;
            } while (chan != -1);

            if (found)
                c = chan;
            else
                _intf.Callback(2, c);

            _assignedChannels[c].note = _para[0];
            _assignedChannels[c].sub = 0;
            _intf.Callback(1, c, _para[0], _para[1]);
        }

        void SendChanVolume()
        {
            var chan = _activeChannels[_command & 0x0f];
            while (chan != -1)
            {
                _intf.Callback(8, chan, _para[1] & 0x7f);
                chan = _assignedChannels[chan].next;
            }
        }

        void SendPanPosition()
        {
            var chan = _activeChannels[_command & 0x0f];
            while (chan != -1)
            {
                _intf.Callback(3, chan, _para[1] & 0x7f);
                chan = _assignedChannels[chan].next;
            }
        }

        void SetTempoIntern(int tempo)
        {
            tempo = ScummHelper.Clip(tempo + _tempoModifier, 0, 500);
            _timerSetting = 34750 / (tempo + 30);
            _extraTimingControl = 16;

            while (_timerSetting < 126)
            {
                _timerSetting <<= 1;
                _extraTimingControl <<= 1;
            }

            while (_timerSetting > 383)
            {
                _timerSetting >>= 1;
                _extraTimingControl >>= 1;
            }

            SetTimerA(true, -(_timerSetting - 2));
        }

        void SendAllNotesOff()
        {
            if (_para[1] > 63)
            {
                _sustainChannels[_command & 0x0f] = -1;
                return;
            }

            _sustainChannels[_command & 0x0f] = 0;
            var chan = _activeChannels[_command & 0x0f];
            while (chan != -1)
            {
                if ((_assignedChannels[chan].note & 0x80) != 0)
                {
                    _assignedChannels[chan].note = 0;
                    _intf.Callback(2, chan);
                }
                chan = _assignedChannels[chan].next;
            }
        }

        void SendSetInstrument()
        {
            var chan = _activeChannels[_command & 0x0f];
            while (chan != -1)
            {
                _intf.Callback(4, chan, _para[0]);
                _intf.Callback(7, chan, 0);
                chan = _assignedChannels[chan].next;
            }
        }

        void SendPitch()
        {
            var chan = _activeChannels[_command & 0x0f];
            while (chan != -1)
            {
                _para[0] += _para[0];
                short pitch = (short)(((_para.ToUInt16() >> 1) & 0x3fff) - 0x2000);
                _intf.Callback(7, chan, pitch);
                chan = _assignedChannels[chan].next;
            }
        }

        void ResetTables()
        {
            _tEnable.Set(0, 0xff, 32);
            _tMode.Set(0, 0xff, 16);
            _tMode.Set(16, 0, 16);
            for (int i = 0; i < 32; i++)
                _tOrdr[i] = (byte)(i & 0x0f);
            _tLevel.Set(0, (sbyte)0, 32);
            _tTranspose.Set(0, (sbyte)0, 32);
        }

        void SetTimerA(bool enable, int tempo)
        {
            _intf.Callback(21, enable ? 255 : 0, tempo);
        }

        void SetTimerB(bool enable, int tempo)
        {
            _intf.Callback(22, enable ? 255 : 0, tempo);
        }

        sbyte[] _activeChannels;
        sbyte[] _sustainChannels;

        class ActiveChannel
        {
            public sbyte chan;
            public sbyte next;
            public byte note;
            public byte sub;
        }

        ActiveChannel[] _assignedChannels;

        byte[] _tEnable;
        byte[] _tMode;
        byte[] _tOrdr;
        sbyte[] _tLevel;
        sbyte[] _tTranspose;

        class DlEvent
        {
            public byte evt;
            public byte mode;
            public byte note;
            public byte velo;
            public ushort len;
        }

        DlEvent[] _eventBuffer;
        int _bufferedEventsCount;

        byte[] _para = new byte[2];
        byte _paraCount;
        byte _command;

        byte _defaultBaseTickLen;
        byte _baseTickLen;
        uint _pulseCount;
        int _extraTimingControlRemainder;
        int _extraTimingControl;
        int _timerSetting;
        sbyte _tempoDiff;
        int _tempoModifier;
        uint _timeStampDest;
        uint _timeStampBase;
        sbyte _elapsedEvents;
        uint _tickCounter;
        byte _defaultTempo;
        int _trackTempo;

        bool _loop;
        bool _playing;
        bool _endOfTrack;
        bool _suspendParsing;

        byte[] _musicData;
        int _musicPos;
        int _musicStart;
        int _musicTrackSize;

        readonly TownsAudioInterface _intf;
    }
}

