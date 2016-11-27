using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using NScumm.Core.Common;

namespace NScumm.Agos
{
    internal class RawSound: BaseSound
    {
        private readonly AudioFlags _flags;

        public RawSound(IMixer mixer, string filename, bool isUnsigned)
            : base(mixer, filename, 0, Sound.SOUND_BIG_ENDIAN)
        {
            _flags = isUnsigned ? AudioFlags.Unsigned : 0;
        }

        public override IAudioStream MakeAudioStream(uint sound)
        {
            if (_offsets == null)
                return null;

            var file = Engine.OpenFileRead(_filename);
            if (file==null) {
                DebugHelper.Warning("RawSound::makeAudioStream: Could not open file \"{0}\"", _filename);
                return null;
            }

            var br = new BinaryReader(file);
            file.Seek(_offsets[sound], SeekOrigin.Begin);
            int size = br.ReadInt32BigEndian();
            return new RawStream(_flags,
                22050, true, new SeekableSubReadStream(file, _offsets[sound] + 4, _offsets[sound] + 4 + size, true));
        }
    }
}