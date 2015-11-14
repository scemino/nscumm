//
//  IPlayerMod.cs
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

namespace NScumm.Scumm.Audio.Amiga
{
    public delegate void ModUpdateProc();

    interface IPlayerMod
    {
        int MusicVolume { get; set; }

        void SetChannelVol(int id, int vol);

        void StartChannel(int id, byte[] data, int size, int rate, int vol, int loopStart = 0, int loopEnd = 0, int pan = 0);

        void StopChannel(int id);

        void SetChannelFreq(int id, int freq);

        void SetUpdateProc(ModUpdateProc proc, int freq);
    }
    
}
