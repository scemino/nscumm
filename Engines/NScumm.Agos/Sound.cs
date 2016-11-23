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
using NScumm.Core;
using NScumm.Core.Audio;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    [Flags]
    internal enum SoundTypeFlags
    {
        AMBIENT = 1 << 0,
        SFX = 1 << 1,
        SFX5 = 1 << 2
    }

    internal class Sound
    {
        AGOSEngine _vm;

        IMixer _mixer;

        BaseSound _voice;
        BaseSound _effects;

        bool _effectsPaused;
        bool _ambientPaused;
        bool _sfx5Paused;

        //uint16 *_filenums;
        //uint32 *_offsets;
        ushort _lastVoiceFile;

        SoundHandle _voiceHandle;
        SoundHandle _effectsHandle;
        SoundHandle _ambientHandle;
        SoundHandle _sfx5Handle;

        bool _hasEffectsFile;
        bool _hasVoiceFile;
        ushort _ambientPlaying;

        // Personal Nightmare specfic
        BytePtr _soundQueuePtr;
        ushort _soundQueueNum;
        uint _soundQueueSize;
        ushort _soundQueueFreq;

        public Sound(AGOSEngine vm, GameSpecificSettings gss, IMixer mixer)
        {
            _vm = vm;
            _mixer = mixer;

            if (_vm._gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                LoadVoiceFile(gss);

                if (_vm._gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                    LoadSfxFile(gss);
            }
        }

        public void StopVoice()
        {
            _mixer.StopHandle(_voiceHandle);
        }

        private void LoadSfxFile(GameSpecificSettings gss)
        {
            if (_hasEffectsFile)
                return;

            _effects = MakeSound(_mixer, gss.effects_filename);
            _hasEffectsFile = _effects != null;

            if (_hasEffectsFile)
                return;

            const bool dataIsUnsigned = true;

            if (Engine.FileExists(gss.effects_filename))
            {
                _hasEffectsFile = true;
                throw new NotImplementedException();
                //_effects = new VocSound(_mixer, gss.effects_filename, dataIsUnsigned);
            }
        }

        private static BaseSound MakeSound(IMixer mixer, string basename)
        {
#if USE_FLAC
            if (Engine.FileExists(basename + ".fla"))
                return new FLACSound(mixer, basename + ".fla");
#endif
#if USE_VORBIS
            if (Engine.FileExists(basename + ".ogg"))
                return new VorbisSound(mixer, basename + ".ogg");
#endif
#if USE_MAD
            if (Common::File::exists(basename + ".mp3"))
                return new MP3Sound(mixer, basename + ".mp3");
#endif
            if (Engine.FileExists(basename + ".wav"))
                return new WavSound(mixer, basename + ".wav");
            if (Engine.FileExists(basename + ".voc"))
                throw new NotImplementedException();
            //return new VocSound(mixer, basename + ".voc", true);
            return null;
        }

        private void LoadVoiceFile(GameSpecificSettings gss)
        {
            throw new NotImplementedException();
        }


        // This method is only used by Simon1 Amiga CD32 & Windows
        public void ReadSfxFile(string filename)
        {
            if (_hasEffectsFile)
                return;

            _mixer.StopHandle(_effectsHandle);

            if (!Engine.FileExists(filename))
            {
                Error("readSfxFile: Can't load sfx file {0}", filename);
            }

            bool dataIsUnsigned = _vm._gd.ADGameDescription.gameId != GameIds.GID_SIMON1CD32;

            if (_vm._gd.ADGameDescription.gameId == GameIds.GID_SIMON1CD32)
            {
                throw new NotImplementedException();
                //_effects = new VocSound(_mixer, filename, dataIsUnsigned, 0, SOUND_BIG_ENDIAN);
            }
            else
            {
                _effects = new WavSound(_mixer, filename);
            }
        }

        public void PlayAmbientData(BytePtr dstPtr, ushort sound, short pan, short vol)
        {
            throw new NotImplementedException();
        }

        public void PlaySfxData(BytePtr dstPtr, ushort sound, short pan, short vol)
        {
            throw new NotImplementedException();
        }

        public void PlaySfx5Data(BytePtr dstPtr, ushort sound, short pan, short vol)
        {
            throw new NotImplementedException();
        }

        public void PlayAmbient(ushort sound)
        {
            throw new NotImplementedException();
        }

        public void PlayEffects(ushort sound)
        {
            throw new NotImplementedException();
        }
    }
}