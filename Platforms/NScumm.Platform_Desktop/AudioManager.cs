using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.MonoGame;
using System.IO;
using System.Linq;
using System;
using FlacBox;

namespace NScumm.Platform_Desktop
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
