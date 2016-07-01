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

using NScumm.Core;
using NScumm.Core.Audio;
using System.IO;
using System.Linq;
using System;
using FlacBox;

namespace NScumm.Mobile.Services
{
    public class AudioManager : IAudioManager
    {
        public string Directory { get; set; }

        public IRewindableAudioStream MakeStream(string filename)
        {
            IRewindableAudioStream stream = null;

            var path = LocatePath(filename + ".*");
            if (path == null) return null;

            if (string.Equals(Path.GetExtension(path), ".flac", StringComparison.OrdinalIgnoreCase) || string.Equals(Path.GetExtension(path), ".fla", StringComparison.OrdinalIgnoreCase))
            {
                stream = (IRewindableAudioStream)MakeFlacStream(File.OpenRead(path));
            }
            return stream;
        }

        public IAudioStream MakeFlacStream(Stream stream)
        {
			return new WaveAudioStream(new WaveOverFlacStream(stream, WaveOverFlacStreamMode.Decode));
        }

        public IAudioStream MakeVorbisStream(Stream stream)
        {
            return null;
        }

        public IAudioStream MakeMp3Stream(Stream stream)
        {
            return null;
        }

        private string LocatePath(string pattern)
        {
            return ServiceLocator.FileStorage.EnumerateFiles(Directory, pattern, Core.SearchOption.AllDirectories).FirstOrDefault();
        }
    }
}
