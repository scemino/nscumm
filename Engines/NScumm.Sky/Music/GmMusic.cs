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
using NScumm.Core;
using NScumm.Core.Audio;

namespace NScumm.Sky.Music
{
    class GmMusic : MusicBase
    {
        private readonly MidiDriver _midiDrv;
        private int _timerCount;
        private ByteAccess _sysExSequence;

        public GmMusic(MidiDriver midiDrv, Mixer mixer, Disk disk) : base(mixer, disk)
        {
            _driverFileBase = 60200;
            _midiDrv = midiDrv;
            var midiRes = midiDrv.Open();
            if (midiRes != 0)
                throw new InvalidOperationException(string.Format("Can't open midi device. Errorcode: {0}", midiRes));
            _timerCount = 0;
            _midiDrv.SetTimerCallback(this, TimerCall);
            _midiDrv.SendGmReset();
        }

        public override void Dispose()
        {
            _midiDrv.SetTimerCallback(null, null);
            if (_currentMusic!=0)
                StopMusic();
            // Send All Sound Off and All Notes Off (for external synths)
            for (int i = 0; i < 16; i++)
            {
                _midiDrv.Send((120 << 8) | 0xB0 | i);
                _midiDrv.Send((123 << 8) | 0xB0 | i);
            }
            _midiDrv.Dispose();

            base.Dispose();
        }

        protected override void SetupPointers()
        {
            if (SystemVars.Instance.GameVersion.Version.Minor == 109)
            {
                _musicDataLoc = _musicData.ToUInt16(0x79B);
                _sysExSequence = new ByteAccess(_musicData, 0x1EF2);
            }
            else
            {
                _musicDataLoc = _musicData.ToUInt16(0x7DC);
                _sysExSequence = new ByteAccess(_musicData, _musicData.ToUInt16(0x7E0));
            }
        }

        protected override void SetupChannels(byte[] channelData, int offset)
        {
            _numberOfChannels = channelData[offset];
            offset++;
            for (byte cnt = 0; cnt < _numberOfChannels; cnt++)
            {
                ushort chDataStart = (ushort)(channelData.ToUInt16(offset + cnt) + _musicDataLoc);
                _channels[cnt] = new GmChannel(_musicData, chDataStart, _midiDrv, MidiDriver.Mt32ToGm, _veloTab);
                _channels[cnt].UpdateVolume(_musicVolume);
            }
        }

        protected override void StartDriver()
        {
            // Send GM System On to reset channel parameters etc.
            byte[] sysEx = { 0x7e, 0x7f, 0x09, 0x01 };
            _midiDrv.SysEx(new NScumm.Core.Common.ByteAccess(sysEx), (ushort)sysEx.Length);
            //_midiDrv->send(0xFF);  //ALSA can't handle this.
            // skip all sysEx as it can't be handled anyways.
        }

        protected override void SetVolume(ushort param)
        {
            _musicVolume = param;
            for (byte cnt = 0; cnt < _numberOfChannels; cnt++)
                _channels[cnt].UpdateVolume(_musicVolume);
        }

        private void TimerCall(object param)
        {
            _timerCount += (int)_midiDrv.BaseTempo;
            if (_timerCount > (1000 * 1000 / 50))
            {
                // call pollMusic() 50 times per second
                _timerCount -= 1000 * 1000 / 50;
                if (_musicData != null)
                    PollMusic();
            }
        }

        static readonly byte[] _veloTab = {
            0x00, 0x40, 0x41, 0x41, 0x42, 0x42, 0x43, 0x43, 0x44, 0x44,
            0x45, 0x45, 0x46, 0x46, 0x47, 0x47, 0x48, 0x48, 0x49, 0x49,
            0x4A, 0x4A, 0x4B, 0x4B, 0x4C, 0x4C, 0x4D, 0x4D, 0x4E, 0x4E,
            0x4F, 0x4F, 0x50, 0x50, 0x51, 0x51, 0x52, 0x52, 0x53, 0x53,
            0x54, 0x54, 0x55, 0x55, 0x56, 0x56, 0x57, 0x57, 0x58, 0x58,
            0x59, 0x59, 0x5A, 0x5A, 0x5B, 0x5B, 0x5C, 0x5C, 0x5D, 0x5D,
            0x5E, 0x5E, 0x5F, 0x5F, 0x60, 0x60, 0x61, 0x61, 0x62, 0x62,
            0x63, 0x63, 0x64, 0x64, 0x65, 0x65, 0x66, 0x66, 0x67, 0x67,
            0x68, 0x68, 0x69, 0x69, 0x6A, 0x6A, 0x6B, 0x6B, 0x6C, 0x6C,
            0x6D, 0x6D, 0x6E, 0x6E, 0x6F, 0x6F, 0x70, 0x70, 0x71, 0x71,
            0x72, 0x72, 0x73, 0x73, 0x74, 0x74, 0x75, 0x75, 0x76, 0x76,
            0x77, 0x77, 0x78, 0x78, 0x79, 0x79, 0x7A, 0x7A, 0x7B, 0x7B,
            0x7C, 0x7C, 0x7D, 0x7D, 0x7E, 0x7E, 0x7F, 0x7F
        };
    }
}
