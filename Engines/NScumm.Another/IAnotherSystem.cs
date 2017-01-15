//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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

using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Another
{
    delegate void AudioCallback(object param, BytePtr stream, int len);

    delegate uint TimerCallback(int delay, object param);

    internal interface IAnotherSystem
    {
        PlayerInput Input { get; }
        int OutputSampleRate { get; }
        BytePtr OffScreenFramebuffer { get; }

        void SetPalette(byte s, byte n, Ptr<Color> buf);
        void CopyRect(ushort x, ushort y, ushort w, ushort h, BytePtr buf);

        void ProcessEvents();
        void Sleep(int duration);
        uint GetTimeStamp();

        void StartAudio(AudioCallback callback, object param);
        void StopAudio();

        object AddTimer(int delay, TimerCallback callback, object param);
        void RemoveTimer(object timerId);
    }
}