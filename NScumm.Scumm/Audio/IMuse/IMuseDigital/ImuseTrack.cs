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
using NScumm.Core;
using NScumm.Core.Audio;

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Track
    {
        public int TrackId;
        // used to identify track by value (0-15)

        public sbyte pan;
        // panning value of sound
        public int vol;
        // volume level (values 0-127 * 1000)
        public int VolFadeDest;
        // volume level which fading target (values 0-127 * 1000)
        public int VolFadeStep;
        // delta of step while changing volume at each imuse callback
        public int VolFadeDelay;
        // time in ms how long fading volume must be
        public bool VolFadeUsed;
        // flag if fading is in progress

        public int SoundId;
        // sound id used by scumm script
        public string SoundName;
        // sound name but also filename of sound in bundle data
        public bool Used;
        // flag mean that track is used
        public bool ToBeRemoved;
        // flag mean that track need to be free
        public bool SouStreamUsed;
        // flag mean that track use stream from sou file
        public bool SndDataExtComp;
        // flag mean that sound data is compressed by scummvm tools
        public int SoundPriority;
        // priority level of played sound (0-127)
        public int RegionOffset;
        // offset to sound data relative to begining of current region
        public int DataOffset;
        // offset to sound data relative to begining of 'DATA' chunk
        public int CurRegion;
        // id of current used region
        public int CurHookId;
        // id of current used hook id
        public int VolGroupId;
        // id of volume group (IMUSE_VOLGRP_VOICE, IMUSE_VOLGRP_SFX, IMUSE_VOLGRP_MUSIC)
        public int SoundType;
        // type of sound data (kSpeechSoundType, kSFXSoundType, kMusicSoundType)
        public int FeedSize;
        // size of sound data needed to be filled at each callback iteration
        public int DataMod12Bit;
        // value used between all callback to align 12 bit source of data
        public AudioFlags MixerFlags;
        // flags for sound mixer's channel (kFlagStereo, kFlag16Bits, kFlagUnsigned)

        public SoundDesc SoundDesc;
        // sound handle used by iMuse sound manager
        public SoundHandle MixChanHandle = new SoundHandle();
        // sound mixer's channel handle
        public QueuingAudioStream Stream;
        // sound mixer's audio stream handle for *.la1 and *.bun

        public Track()
        {
            SoundId = -1;
        }

        public Track Clone()
        {
            var track = (Track)MemberwiseClone();
            return track;
        }

        public void Clear()
        {
            TrackId = 0;
            pan = 0;
            vol = 0;
            VolFadeDest = 0;
            VolFadeStep = 0;
            VolFadeDelay = 0;
            VolFadeUsed = false;
//            SoundId = 0;
            SoundName = null;
            Used = false;
            ToBeRemoved = false;
            SouStreamUsed = false;
            SndDataExtComp = false;
            SoundPriority = 0;
            RegionOffset = 0;
            DataOffset = 0;
            CurRegion = 0;
            CurHookId = 0;
            VolGroupId = 0;
            SoundType = 0;
            FeedSize = 0;
            DataMod12Bit = 0;
            MixerFlags = AudioFlags.None;

            SoundDesc = null;
            MixChanHandle = new SoundHandle();
            Stream = null;
        }

        public int Pan  { get { return (pan != 64) ? 2 * pan - 127 : 0; } }

        public int Volume { get { return vol / 1000; } }

        public SoundType GetSoundType()
        {
            SoundType type;
            if (VolGroupId == 1)
                type = Core.Audio.SoundType.Speech;
            else if (VolGroupId == 2)
                type = Core.Audio.SoundType.SFX;
            else if (VolGroupId == 3)
                type = Core.Audio.SoundType.Music;
            else
                throw new InvalidOperationException("Track::GetType(): invalid sound type");
            return type;
        }

        internal string DebuggerDisplay
        {
            get
            { 
                return SoundId != -1 ? string.Format("SoundId {0}", SoundId) : string.Empty;
            }    
        }
    }
}

