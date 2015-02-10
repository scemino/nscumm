//
//  ImuseTrack.cs
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
using System;

namespace NScumm.Core.Audio.IMuse
{
    class Track: ICloneable
    {
        public int trackId;
        // used to identify track by value (0-15)

        public sbyte pan;
        // panning value of sound
        public int vol;
        // volume level (values 0-127 * 1000)
        public int volFadeDest;
        // volume level which fading target (values 0-127 * 1000)
        public int volFadeStep;
        // delta of step while changing volume at each imuse callback
        public int volFadeDelay;
        // time in ms how long fading volume must be
        public bool volFadeUsed;
        // flag if fading is in progress

        public int soundId;
        // sound id used by scumm script
        public string soundName;
        // sound name but also filename of sound in bundle data
        public bool used;
        // flag mean that track is used
        public bool toBeRemoved;
        // flag mean that track need to be free
        public bool souStreamUsed;
        // flag mean that track use stream from sou file
        public bool sndDataExtComp;
        // flag mean that sound data is compressed by scummvm tools
        public int soundPriority;
        // priority level of played sound (0-127)
        public int regionOffset;
        // offset to sound data relative to begining of current region
        public int dataOffset;
        // offset to sound data relative to begining of 'DATA' chunk
        public int curRegion;
        // id of current used region
        public int curHookId;
        // id of current used hook id
        public int volGroupId;
        // id of volume group (IMUSE_VOLGRP_VOICE, IMUSE_VOLGRP_SFX, IMUSE_VOLGRP_MUSIC)
        public int soundType;
        // type of sound data (kSpeechSoundType, kSFXSoundType, kMusicSoundType)
        public int feedSize;
        // size of sound data needed to be filled at each callback iteration
        public int dataMod12Bit;
        // value used between all callback to align 12 bit source of data
        public AudioFlags mixerFlags;
        // flags for sound mixer's channel (kFlagStereo, kFlag16Bits, kFlagUnsigned)

        public SoundDesc soundDesc;
        // sound handle used by iMuse sound manager
        public SoundHandle mixChanHandle=new SoundHandle();
        // sound mixer's channel handle
        public QueuingAudioStream stream;
        // sound mixer's audio stream handle for *.la1 and *.bun

        public Track()
        {
            soundId = -1;
        }

        public Track Clone()
        {
            return (Track)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return (Track)Clone();
        }

        public void Clear()
        {
            trackId = 0;
            pan = 0;
            vol = 0;
            volFadeDest = 0;
            volFadeStep = 0;
            volFadeDelay = 0;
            volFadeUsed = false;
            soundId = 0;
            soundName = null;
            used = false;
            toBeRemoved = false;
            souStreamUsed = false;
            sndDataExtComp = false;
            soundPriority = 0;
            regionOffset = 0;
            dataOffset = 0;
            curRegion = 0;
            curHookId = 0;
            volGroupId = 0;
            soundType = 0;
            feedSize = 0;
            dataMod12Bit = 0;
            mixerFlags = AudioFlags.None;

            soundDesc = null;
            mixChanHandle = new SoundHandle();
            stream = null;
        }

        public int Pan  { get { return (pan != 64) ? 2 * pan - 127 : 0; } }

        public int Volume { get { return vol / 1000; } }

        public SoundType GetSoundType()
        {
            SoundType type;
            if (volGroupId == 1)
                type = SoundType.Speech;
            else if (volGroupId == 2)
                type = SoundType.SFX;
            else if (volGroupId == 3)
                type = SoundType.Music;
            else
                throw new InvalidOperationException("Track::GetType(): invalid sound type");
            return type;
        }
    }
}

