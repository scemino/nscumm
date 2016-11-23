//
//  MusicInfo.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core.Audio;

namespace NScumm.Agos
{
    class MusicInfo
    {
        public MidiParser parser;
        public BytePtr data;
        public byte num_songs; // For Type 1 SMF resources
        public BytePtr[] songs = new BytePtr[16]; // For Type 1 SMF resources
        public int[] song_sizes = new int[16]; // For Type 1 SMF resources

        public MidiChannel[] channel = new MidiChannel[16]; // Dynamic remapping of channels to resolve conflicts
        public byte[] volume = new byte[16]; // Current channel volume

        public MusicInfo()
        {
            Clear();
        }

        public void Clear()
        {
            parser = null;
            data = BytePtr.Null;
            num_songs = 0;
            songs.Set(0, BytePtr.Null, songs.Length);
            song_sizes.Set(0, 0, song_sizes.Length);
            channel.Set(0, null, channel.Length);
        }
    }
}