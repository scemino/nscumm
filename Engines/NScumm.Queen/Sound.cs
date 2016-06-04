//
//  Sound.cs
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
using System;
using System.IO;
using NScumm.Core.Audio;
using NScumm.Core.IO;

namespace NScumm.Queen
{
    class SilentSound : PCSound
    {
        public SilentSound(IMixer mixer, QueenEngine vm)
            : base(mixer, vm)
        {
        }

        protected override void PlaySoundData(Stream f, uint size, ref SoundHandle soundHandle)
        {
        }
    }

    class SBSound : PCSound
    {
        public SBSound(IMixer mixer, QueenEngine vm) : base(mixer, vm) { }

        protected override void PlaySoundData(Stream f, uint size, ref SoundHandle soundHandle)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class Sound
    {
        protected IMixer _mixer;
        protected QueenEngine _vm;
        bool _sfxToggle;
        bool _musicToggle;
        protected bool _speechSfxExists;

        public bool SpeechOn { get; set; }
        public bool SpeechSfxExists { get { return _speechSfxExists; } }
        public virtual bool IsSpeechActive { get { return false; } }

        protected Sound(IMixer mixer, QueenEngine vm)
        {
            _mixer = mixer;
            _vm = vm;
            _sfxToggle = true;
            SpeechOn = true;
            _musicToggle = true;
        }

        public static Sound MakeSoundInstance(IMixer mixer, QueenEngine vm, byte compression)
        {
            if (vm.Resource.Platform == Platform.Amiga)
                throw new NotImplementedException();

            switch (compression)
            {
                case Defines.COMPRESSION_NONE:
                    return new SBSound(mixer, vm);
                case Defines.COMPRESSION_MP3:
#if !USE_MAD
                    // TODO: warning("Using MP3 compressed datafile, but MP3 support not compiled in");
                    return new SilentSound(mixer, vm);
#else
                    return new MP3Sound(mixer, vm);
#endif
                case Defines.COMPRESSION_OGG:
#if !USE_VORBIS
                    // TODO: warning("Using OGG compressed datafile, but OGG support not compiled in");
                    return new SilentSound(mixer, vm);
#else
                    return new OGGSound(mixer, vm);
#endif
                case Defines.COMPRESSION_FLAC:
#if !USE_FLAC
                    // TODO: warning("Using FLAC compressed datafile, but FLAC support not compiled in");
                    return new SilentSound(mixer, vm);
#else
                    return new FLACSound(mixer, vm);
#endif
                default:
                    // TODO: warning("Unknown compression type");
                    return new SilentSound(mixer, vm);
            }
        }

        public virtual void PlaySpeech(string @base) {}
        public virtual void StopSpeech() {}
        public virtual void PlaySfx(ushort sfx) { }
    }
}

