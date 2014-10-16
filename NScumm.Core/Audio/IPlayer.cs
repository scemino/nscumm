//
//  IPlayer.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Core
{
    public interface IPlayer
    {
        bool IsNativeMT32 { get; }

        bool IsMidi { get; }

        int Priority { get; set; }

        int EffectiveVolume { get; }

        int Transpose { get; }

        int Pan { get; set; }

        int Detune { get; set; }

        int Id { get; }

        bool IsActive { get; }

        bool IsFadingOut { get; }

        int OffsetNote { get; set; }

        float GetMusicTimer();

        bool StartSound(int sound);

        bool Update();

        void Clear();

        int GetParam(int param, int chan);

        int SetHook(int cls, int value, int chan);
    }

    public interface ISoundRepository
    {
        byte[] GetSound(int id);
    }
}

