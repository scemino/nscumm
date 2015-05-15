//
//  Player_V2A.cs
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
using NScumm.Core.IO;

namespace NScumm.Core.Audio
{
    interface IV2A_Sound
    {
        void Start(IPlayerMod mod, int id, byte[] data);

        bool Update();

        void Stop();
    }

    class Player_V2A: IMusicEngine
    {
        const int V2A_MAXSLOTS = 8;

        public Player_V2A(ScummEngine scumm, IPlayerMod modPlayer)
        {
            _vm = scumm;

            _mod = modPlayer;
            _mod.SetUpdateProc(UpdateSound, 60);
        }

        void IMusicEngine.SaveOrLoad(Serializer serializer)
        {
        }

        public int GetMusicTimer()
        {
            return 0; // FIXME - need to keep track of playing music resources
        }

        public int GetSoundStatus(int nr)
        {
            for (var i = 0; i < V2A_MAXSLOTS; i++)
            {
                if (_slot[i].Id == nr)
                    return 1;
            }
            return 0;
        }

        public void SetMusicVolume(int vol)
        {
            _mod.MusicVolume = vol;
        }

        public void StopAllSounds()
        {
            for (var i = 0; i < V2A_MAXSLOTS; i++)
            {
                if (_slot[i].Id == 0)
                    continue;
                _slot[i].Sound.Stop();
                _slot[i].Sound = null;
                _slot[i].Id = 0;
            }
        }

        public void StartSound(int nr)
        {
            var data = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);
            var crc = GetCRC(data, 0x0A, data.ToUInt16BigEndian(0x08));
            var snd = FindSound(crc);
            if (snd == null)
            {
                Debug.WriteLine("player_v2a - sound {0} not recognized yet (crc {1:X8})", nr, crc);
                return;
            }
            StopSound(nr);
            int i = GetSoundSlot();
            if (i == -1)
            {
//                delete snd;
                return;
            }
            _slot[i].Id = nr;
            _slot[i].Sound = snd;
            _slot[i].Sound.Start(_mod, nr, data);
        }

        public void StopSound(int nr)
        {
            if (nr == 0)
                return;
            var i = GetSoundSlot(nr);
            if (i == -1)
                return;
            _slot[i].Sound.Stop();
//            delete _slot[i].sound;
            _slot[i].Sound = null;
            _slot[i].Id = 0;
        }

        static IV2A_Sound FindSound(long crc)
        {
            switch (crc)
            {
                case 0x8FAB08C4:
                    return new V2A_Sound_SingleLooped(0x006C, 0x2B58, 0x016E, 0x3F); // Maniac 17
                case 0xB673160A:
                    return new V2A_Sound_SingleLooped(0x006C, 0x1E78, 0x01C2, 0x1E); // Maniac 38
                case 0x4DB1D0B2:
                    return new V2A_Sound_MultiLooped(0x0072, 0x1BC8, 0x023D, 0x3F, 0x0224, 0x3F); // Maniac 20
                case 0x754D75EF:
                    return new V2A_Sound_Single(0x0076, 0x0738, 0x01FC, 0x3F); // Maniac 10
                case 0x6E3454AF:
                    return new V2A_Sound_Single(0x0076, 0x050A, 0x017C, 0x3F); // Maniac 12
                case 0x92F0BBB6:
                    return new V2A_Sound_Single(0x0076, 0x3288, 0x012E, 0x3F); // Maniac 41
                case 0xE1B13982:
                    return new V2A_Sound_MultiLoopedDuration(0x0078, 0x0040, 0x007C, 0x3F, 0x007B, 0x3F, 0x001E); // Maniac 21
                case 0x288B16CF:
                    return new V2A_Sound_MultiLoopedDuration(0x007A, 0x0040, 0x007C, 0x3F, 0x007B, 0x3F, 0x000A); // Maniac 11
                case 0xA7565268:
                    return new V2A_Sound_MultiLoopedDuration(0x007A, 0x0040, 0x00F8, 0x3F, 0x00F7, 0x3F, 0x000A); // Maniac 19
                case 0x7D419BFC:
                    return new V2A_Sound_MultiLoopedDuration(0x007E, 0x0040, 0x012C, 0x3F, 0x0149, 0x3F, 0x001E); // Maniac 22
                case 0x1B52280C:
                    return new V2A_Sound_Single(0x0098, 0x0A58, 0x007F, 0x32); // Maniac 6
                case 0x38D4A810:
                    return new V2A_Sound_Single(0x0098, 0x2F3C, 0x0258, 0x32); // Maniac 7
                case 0x09F98FC2:
                    return new V2A_Sound_Single(0x0098, 0x0A56, 0x012C, 0x32); // Maniac 16
                case 0x90440A65:
                    return new V2A_Sound_Single(0x0098, 0x0208, 0x0078, 0x28); // Maniac 28
                case 0x985C76EF:
                    return new V2A_Sound_Single(0x0098, 0x0D6E, 0x00C8, 0x32); // Maniac 30
                case 0x76156137:
                    return new V2A_Sound_Single(0x0098, 0x2610, 0x017C, 0x39); // Maniac 39
                case 0x5D95F88C:
                    return new V2A_Sound_Single(0x0098, 0x0A58, 0x007F, 0x1E); // Maniac 65
                case 0x92D704EA:
                    return new V2A_Sound_SingleLooped(0x009C, 0x29BC, 0x012C, 0x3F, 0x1BD4, 0x0DE8); // Maniac 15
                case 0x92F5513C:
                    return new V2A_Sound_Single(0x009E, 0x0DD4, 0x01F4, 0x3F); // Maniac 13
                case 0xCC2F3B5A:
                    return new V2A_Sound_Single(0x009E, 0x00DE, 0x01AC, 0x3F); // Maniac 43
                case 0x153207D3:
                    return new V2A_Sound_Single(0x009E, 0x0E06, 0x02A8, 0x3F); // Maniac 67
                case 0xC4F370CE:
                    return new V2A_Sound_Single(0x00AE, 0x0330, 0x01AC, 0x3F); // Maniac 8
                case 0x928C4BAC:
                    return new V2A_Sound_Single(0x00AE, 0x08D6, 0x01AC, 0x3F); // Maniac 9
                case 0x62D5B11F:
                    return new V2A_Sound_Single(0x00AE, 0x165C, 0x01CB, 0x3F); // Maniac 27
                case 0x3AB22CB5:
                    return new V2A_Sound_Single(0x00AE, 0x294E, 0x012A, 0x3F); // Maniac 62
                case 0x2D70BBE9:
                    return new V2A_Sound_SingleLoopedPitchbend(0x00B4, 0x1702, 0x03E8, 0x0190, 0x3F, 5); // Maniac 64
                case 0xFA4C1B1C:
                    return new V2A_Sound_Special_Maniac69(0x00B2, 0x1702, 0x0190, 0x3F); // Maniac 69
                case 0x19D50D67:
                    return new V2A_Sound_Special_ManiacDing(0x00B6, 0x0020, 0x00C8, 16, 2); // Maniac 14
                case 0x3E6FBE15:
                    return new V2A_Sound_Special_ManiacTentacle(0x00B2, 0x0010, 0x007C, 0x016D, 1); // Maniac 25
                case 0x5305753C:
                    return new V2A_Sound_Special_ManiacTentacle(0x00B2, 0x0010, 0x007C, 0x016D, 7); // Maniac 36
                case 0x28895106:
                    return new V2A_Sound_Special_Maniac59(0x00C0, 0x00FE, 0x00E9, 0x0111, 4, 0x0A); // Maniac 59
                case 0xB641ACF6:
                    return new V2A_Sound_Special_Maniac61(0x00C8, 0x0100, 0x00C8, 0x01C2); // Maniac 61
                case 0xE1A91583:
                    return new V2A_Sound_Special_ManiacPhone(0x00D0, 0x0040, 0x007C, 0x3F, 0x007B, 0x3F, 0x3C, 5, 6); // Maniac 23
                case 0x64816ED5:
                    return new V2A_Sound_Special_ManiacPhone(0x00D0, 0x0040, 0x00BE, 0x37, 0x00BD, 0x37, 0x3C, 5, 6); // Maniac 24
                case 0x639D72C2:
                    return new V2A_Sound_Special_Maniac46(0x00D0, 0x10A4, 0x0080, 0x3F, 0x28, 3); // Maniac 46
                case 0xE8826D92:
                    return new V2A_Sound_Special_ManiacTypewriter(0x00EC, 0x025A, 0x023C, 0x3F, 8, new byte[]{ 0x20, 0x41, 0x04, 0x21, 0x08, 0x10, 0x13, 0x07 }, true); // Maniac 45
                case 0xEDFF3D41:
                    return new V2A_Sound_Single(0x00F8, 0x2ADE, 0x01F8, 0x3F); // Maniac 42 (this should echo, but it's barely noticeable and I don't feel like doing it)
                case 0x15606D06:
                    return new V2A_Sound_Special_Maniac32(0x0148, 0x0020, 0x0168, 0x0020, 0x3F); // Maniac 32
                case 0x753EAFE3:
                    return new V2A_Sound_Special_Maniac44(0x017C, 0x0010, 0x018C, 0x0020, 0x00C8, 0x0080, 0x3F); // Maniac 44
                case 0xB1AB065C:
                    return new V2A_Sound_Music(0x0032, 0x00B2, 0x08B2, 0x1222, 0x1A52, 0x23C2, 0x3074, false); // Maniac 50
                case 0x091F5D9C:
                    return new V2A_Sound_Music(0x0032, 0x0132, 0x0932, 0x1802, 0x23D2, 0x3EA2, 0x4F04, false); // Maniac 58

            }
            return null;
        }

        int GetSoundSlot(int id = 0)
        {
            int i;
            for (i = 0; i < V2A_MAXSLOTS; i++)
            {
                if (_slot[i].Id == id)
                    break;
            }
            if (i == V2A_MAXSLOTS)
            {
                if (id == 0)
                {
                    Debug.WriteLine("player_v2a - out of sound slots");
                }
                return -1;
            }
            return i;
        }

        void UpdateSound()
        {
            for (var i = 0; i < V2A_MAXSLOTS; i++)
            {
                if ((_slot[i].Id != 0) && (!_slot[i].Sound.Update()))
                {
                    _slot[i].Sound.Stop();
//                    delete _slot[i].sound;
                    _slot[i].Sound = null;
                    _slot[i].Id = 0;
                }
            }
        }

        static uint[] InitCRC()
        {
            var crcTable = new uint[256];
            const uint poly = 0xEDB88320;
            uint n;

            for (var i = 0; i < 256; i++)
            {
                n = (uint)i;
                for (var j = 0; j < 8; j++)
                    n = ((n & 1) != 0) ? ((n >> 1) ^ poly) : (n >> 1);
                crcTable[i] = n;
            }
            return crcTable;
        }

        static uint GetCRC(byte[] data, int offset, int len)
        {
            uint CRC = 0xFFFFFFFF;
            for (var i = 0; i < len; i++)
                CRC = (CRC >> 8) ^ CRCtable[(CRC ^ data[offset + i]) & 0xFF];
            return CRC ^ 0xFFFFFFFF;
        }

        static readonly uint[] CRCtable = InitCRC();

        struct soundSlot
        {
            public int Id;
            public IV2A_Sound Sound;
        }

        ScummEngine _vm;
        IPlayerMod _mod;
        soundSlot[] _slot = new soundSlot[V2A_MAXSLOTS];
    }
}

