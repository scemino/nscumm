//
//  SBSound.cs
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

using System.IO;
using NScumm.Core.Audio;
using D = NScumm.Core.DebugHelper;
using NScumm.Core.Audio.Decoders;

namespace NScumm.Queen
{
    class SBSound : PCSound
    {
        const int SB_HEADER_SIZE_V104 = 110;
        const int SB_HEADER_SIZE_V110 = 122;

        public SBSound(IMixer mixer, QueenEngine vm) : base(mixer, vm) { }

        protected override void PlaySoundData(Stream f, ref SoundHandle soundHandle)
        {
            // In order to simplify the code, we don't parse the .sb header but hard-code the
            // values. Refer to tracker item #1876741 for details on the format/fields.
            int headerSize;
            f.Seek(2, SeekOrigin.Current);
            var br = new BinaryReader(f);
            ushort version = br.ReadUInt16();
            switch (version)
            {
                case 104:
                    headerSize = SB_HEADER_SIZE_V104;
                    break;
                case 110:
                    headerSize = SB_HEADER_SIZE_V110;
                    break;
                default:
                    D.Warning("Unhandled SB file version %d, defaulting to 104", version);
                    headerSize = SB_HEADER_SIZE_V104;
                    break;
            }
            f.Seek(headerSize - 4, SeekOrigin.Current);
            var size = f.Length - headerSize;
            byte[] sound = new byte[size];
            f.Read(sound, 0, (int)size);
            var type = (soundHandle == _speechHandle) ? SoundType.Speech : SoundType.SFX;

            var stream = new RawStream(AudioFlags.Unsigned, 11840, true, new MemoryStream(sound, 0, (int)size));
            soundHandle = _mixer.PlayStream(type, stream);
        }
    }

}
