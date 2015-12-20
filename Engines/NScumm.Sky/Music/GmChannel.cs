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

namespace NScumm.Sky.Music
{
    internal struct MidiChannelType
    {
        public ushort EventDataPtr;
        public int NextEventTime;
        public ushort LoopPoint;
        public byte MidiChannelNumber;
        public byte Note;
        public bool ChannelActive;
    }

    internal class GmChannel : IChannelBase
    {
        private readonly byte[] _musicData;
        private readonly MidiDriver _midiDrv;
        private readonly byte[] _instMap;
        private readonly byte[] _veloTab;
        private ushort _musicVolume;
        private byte _currentChannelVolume;
        private MidiChannelType _channelData;

        public GmChannel(byte[] musicData, ushort startOfData, MidiDriver midiDrv, byte[] instMap, byte[] veloTab)
        {
            _musicData = musicData;
            _midiDrv = midiDrv;
            _channelData.MidiChannelNumber = 0;
            _channelData.LoopPoint = startOfData;
            _channelData.EventDataPtr = startOfData;
            _channelData.ChannelActive = true;
            _channelData.NextEventTime = GetNextEventTime();
            _instMap = instMap;
            _veloTab = veloTab;

            _musicVolume = 0x7F;
            _currentChannelVolume = 0x7F;
        }

        public void Dispose()
        {
            StopNote();
        }

        private void StopNote()
        {
            // All Notes Off
            _midiDrv.Send((0xB0 | _channelData.MidiChannelNumber) | 0x7B00 | 0 | 0x79000000);
            // Reset the Pitch Wheel. See bug #1016556.
            _midiDrv.Send((0xE0 | _channelData.MidiChannelNumber) | 0x400000);
        }

        public byte Process(ushort aktTime)
        {
            if (!_channelData.ChannelActive)
                return 0;

            byte returnVal = 0;

            _channelData.NextEventTime -= aktTime;

            while ((_channelData.NextEventTime < 0) && (_channelData.ChannelActive))
            {
                var opcode = _musicData[_channelData.EventDataPtr];
                _channelData.EventDataPtr++;
                if ((opcode & 0x80) != 0)
                {
                    if (opcode == 0xFF)
                    {
                        // dummy opcode
                    }
                    else if (opcode >= 0x90)
                    {
                        switch (opcode & 0xF)
                        {
                            case 0: com90_caseNoteOff(); break;
                            case 1: com90_stopChannel(); break;
                            case 2: com90_setupInstrument(); break;
                            case 3:
                                returnVal = com90_updateTempo();
                                break;
                            case 5: com90_getPitch(); break;
                            case 6: com90_getChannelVolume(); break;
                            case 8: com90_loopMusic(); break;
                            case 9: com90_keyOff(); break;
                            case 11: com90_getChannelPanValue(); break;
                            case 12: com90_setLoopPoint(); break;
                            case 13: com90_getChannelControl(); break;

                            default:
                                throw new InvalidOperationException(string.Format("GmChannel: Unknown music opcode 0x{0:X2}", opcode));
                        }
                    }
                    else
                    {
                        // new midi channel assignment
                        _channelData.MidiChannelNumber = (byte)(opcode & 0xF);
                    }
                }
                else
                {
                    _channelData.Note = opcode;
                    byte velocity = _musicData[_channelData.EventDataPtr];
                    if (_veloTab != null)
                        velocity = _veloTab[velocity];
                    _channelData.EventDataPtr++;
                    _midiDrv.Send((0x90 | _channelData.MidiChannelNumber) | (opcode << 8) | (velocity << 16));
                }
                if (_channelData.ChannelActive)
                    _channelData.NextEventTime += GetNextEventTime();
            }
            return returnVal;
        }

        public void UpdateVolume(ushort pVolume)
        {
            _musicVolume = pVolume;
            if (_musicVolume > 0)
                _musicVolume = (ushort)((_musicVolume * 2) / 3 + 43);

            byte newVol = (byte) ((_currentChannelVolume * _musicVolume) >> 7);
            _midiDrv.Send(0xB0 | _channelData.MidiChannelNumber | 0x700 | (newVol << 16));
        }

        public bool IsActive { get { return _channelData.ChannelActive; } }

        private int GetNextEventTime()
        {
            int retV = 0;
            byte cnt, lVal = 0;
            for (cnt = 0; cnt < 4; cnt++)
            {
                lVal = _musicData[_channelData.EventDataPtr];
                _channelData.EventDataPtr++;
                retV = (retV << 7) | (lVal & 0x7F);
                if ((lVal & 0x80) == 0)
                    break;
            }
            if ((lVal & 0x80) != 0)
            { // should never happen
                return -1;
            }
            return retV;
        }

        private void com90_caseNoteOff()
        {
            _midiDrv.Send((0x90 | _channelData.MidiChannelNumber) | (_musicData[_channelData.EventDataPtr] << 8));
            _channelData.EventDataPtr++;
        }

        private void com90_stopChannel()
        {
            StopNote();
            _channelData.ChannelActive = false;
        }

        private void com90_setupInstrument()
        {
            byte instrument = _musicData[_channelData.EventDataPtr];
            if (_instMap != null)
                instrument = _instMap[instrument];
            _midiDrv.Send((0xC0 | _channelData.MidiChannelNumber) | (instrument << 8));
            _channelData.EventDataPtr++;
        }

        private byte com90_updateTempo()
        {
            return _musicData[_channelData.EventDataPtr++];
        }

        private void com90_getPitch()
        {
            _midiDrv.Send((0xE0 | _channelData.MidiChannelNumber) | 0 | (_musicData[_channelData.EventDataPtr] << 16));
            _channelData.EventDataPtr++;
        }

        private void com90_getChannelVolume()
        {
            _currentChannelVolume = _musicData[_channelData.EventDataPtr++];
            byte newVol = (byte)((_currentChannelVolume * _musicVolume) >> 7);
            _midiDrv.Send((0xB0 | _channelData.MidiChannelNumber) | 0x700 | (newVol << 16));
        }

        private void com90_loopMusic()
        {
            _channelData.EventDataPtr = _channelData.LoopPoint;
        }

        private void com90_keyOff()
        {
            _midiDrv.Send((0x90 | _channelData.MidiChannelNumber) | (_channelData.Note << 8) | 0);
        }

        private void com90_setLoopPoint()
        {
            _channelData.LoopPoint = _channelData.EventDataPtr;
        }

        private void com90_getChannelPanValue()
        {
            _midiDrv.Send((0xB0 | _channelData.MidiChannelNumber) | 0x0A00 | (_musicData[_channelData.EventDataPtr] << 16));
            _channelData.EventDataPtr++;
        }

        private void com90_getChannelControl()
        {
            byte conNum = _musicData[_channelData.EventDataPtr++];
            byte conDat = _musicData[_channelData.EventDataPtr++];
            _midiDrv.Send((0xB0 | _channelData.MidiChannelNumber) | (conNum << 8) | (conDat << 16));
        }
    }
}
