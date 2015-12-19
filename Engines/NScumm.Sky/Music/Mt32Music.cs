using System;
using NScumm.Core;
using NScumm.Core.Audio;

namespace NScumm.Sky.Music
{
    internal class Mt32Music : MusicBase
    {
        private readonly MidiDriver _midiDrv;
        private int _timerCount;
        private ByteAccess _sysExSequence;

        public Mt32Music(MidiDriver midiDrv, Mixer mixer, Disk disk) : base(mixer, disk)
        {
            _driverFileBase = 60200;
            _midiDrv = midiDrv;
            var midiRes = _midiDrv.Open();
            if (midiRes != 0)
                throw new InvalidOperationException(string.Format("Can't open midi device. Errorcode: {0}", midiRes));
            _timerCount = 0;
            _midiDrv.SetTimerCallback(this, TimerCall);
            _midiDrv.SendMt32Reset();
        }

        private void TimerCall(object param)
        {
            _timerCount += (int)_midiDrv.BaseTempo;
            if (_timerCount > 1000000 / 50)
            {
                // call pollMusic() 50 times per second
                _timerCount -= 1000000 / 50;
                if (_musicData != null)
                    PollMusic();
            }
        }

        protected override void SetupPointers()
        {
            _musicDataLoc = _musicData.ToUInt16(0x7DC);
            _sysExSequence = new ByteAccess(_musicData, _musicData.ToUInt16(0x7E0));
        }

        protected override void SetupChannels(byte[] channelData, int offset)
        {
            _numberOfChannels = channelData[offset];
            offset++;
            for (byte cnt = 0; cnt < _numberOfChannels; cnt++)
            {
                ushort chDataStart = (ushort)(channelData.ToUInt16(offset + cnt) + _musicDataLoc);
                _channels[cnt] = new GmChannel(_musicData, chDataStart, _midiDrv, null, null);
                _channels[cnt].UpdateVolume(_musicVolume);
            }
        }

        protected override void StartDriver()
        {
            // setup timbres and patches using SysEx data
            var sysExData = _sysExSequence;
            byte timbreNum = sysExData[0];
            byte cnt, crc;
            sysExData.Offset++;
            byte[] sendBuf = new byte[256];
            byte len;
            sendBuf[0] = 0x41;
            sendBuf[1] = 0x10;
            sendBuf[2] = 0x16;
            sendBuf[3] = 0x12;
            for (cnt = 0; cnt < timbreNum; cnt++)
            {
                len = 7;
                crc = 0;
                // Timbre address
                sendBuf[4] = (byte)(0x8 | (sysExData[0] >> 6));
                sendBuf[5] = (byte)((sysExData[0] & 0x3F) << 1);
                sendBuf[6] = 0xA;
                sysExData.Offset++;
                crc -= (byte)(sendBuf[4] + sendBuf[5] + sendBuf[6]);
                byte dataLen = sysExData[0];
                sysExData.Offset++;
                // Timbre data:
                do
                {
                    byte rlVal = 1;
                    byte codeVal = sysExData[0];
                    sysExData.Offset++;

                    if ((codeVal & 0x80) != 0)
                    {
                        codeVal &= 0x7F;
                        rlVal = sysExData[0];
                        sysExData.Offset++;
                        dataLen--;
                    }
                    for (byte cnt2 = 0; cnt2 < rlVal; cnt2++)
                    {
                        sendBuf[len] = codeVal;
                        len++;
                        crc -= codeVal;
                    }
                    dataLen--;
                } while (dataLen > 0);
                sendBuf[len] = (byte)(crc & 0x7F);
                len++;
                _midiDrv.SysEx(sendBuf, len);
                // We delay the time it takes to send the sysEx plus an
                // additional 40ms, which is required for MT-32 rev00,
                // to assure no buffer overflow or missing bytes
                ServiceLocator.Platform.Sleep((len + 2) * 1000 / 3125 + 40);
            }

            while (ProcessPatchSysEx(sysExData))
                sysExData.Offset += 5;
        }

        protected override void SetVolume(ushort volume)
        {
            byte[] sysEx = { 0x41, 0x10, 0x16, 0x12, 0x10, 0x00, 0x16, 0x00, 0x00 };
            _musicVolume = volume;
            sysEx[7] = (byte)(volume > 100 ? 100 : volume);
            sysEx[8] = 0x00;
            for (byte cnt = 4; cnt < 8; cnt++)
                sysEx[8] -= sysEx[cnt];
            sysEx[8] &= 0x7F;
            _midiDrv.SysEx(sysEx, 9);
        }

        private bool ProcessPatchSysEx(ByteAccess sysExData)
        {
            byte[] sysExBuf = new byte[15];
            byte crc = 0;
            if ((sysExData[0] & 0x80) != 0)
                return false;

            // decompress data from stream
            sysExBuf[0] = 0x41;
            sysExBuf[1] = 0x10;
            sysExBuf[2] = 0x16;
            sysExBuf[3] = 0x12;
            sysExBuf[4] = 0x5;
            sysExBuf[5] = (byte)(sysExData[0] >> 4);            // patch offset part 1
            sysExBuf[6] = (byte)((sysExData[0] & 0xF) << 3);    // patch offset part 2
            sysExBuf[7] = (byte)(sysExData[1] >> 6);            // timbre group
            sysExBuf[8] = (byte)(sysExData[1] & 0x3F);          // timbre num
            sysExBuf[9] = (byte)(sysExData[2] & 0x3F);          // key shift
            sysExBuf[10] = (byte)(sysExData[3] & 0x7F);         // fine tune
            sysExBuf[11] = (byte)(sysExData[4] & 0x7F);         // bender range
            sysExBuf[12] = (byte)(sysExData[2] >> 6);           // assign mode
            sysExBuf[13] = (byte)(sysExData[3] >> 7);           // reverb switch
            for (byte cnt = 4; cnt < 14; cnt++)
                crc -= sysExBuf[cnt];
            sysExBuf[14] = (byte)(crc & 0x7F);                  // crc
            _midiDrv.SysEx(sysExBuf, 15);
            // We delay the time it takes to send the sysEx plus an
            // additional 40ms, which is required for MT-32 rev00,
            // to assure no buffer overflow or missing bytes
            ServiceLocator.Platform.Sleep(17 * 1000 / 3125 + 40);
            return true;
        }
    }
}
