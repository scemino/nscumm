//
//  SoundDesc.cs
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
using NScumm.Core.Audio;

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
{
    struct Region
    {
        public int Offset;
        // offset of region
        public int Length;
        // length of region
    }

    struct Jump
    {
        public int Offset;
        // jump offset position
        public int Dest;
        // jump to dest position
        public byte HookId;
        // id of hook
        public short FadeDelay;
        // fade delay in ms
    }

    struct Sync
    {
        public byte[] Ptr;
    }

    struct Marker
    {
        /// <summary>
        /// Marker in sound data.
        /// </summary>
        public int Pos;
        public string Ptr;
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    class SoundDesc
    {
        internal string DebuggerDisplay
        {
            get
            { 
                return InUse ? string.Format("SoundId {0}", SoundId) : string.Empty;
            }    
        }

        public ushort Freq;
        // frequency
        public byte Channels;
        // stereo or mono
        public byte Bits;
        // 8, 12, 16

        public int NumJumps;
        // number of Jumps
        public Region[] Region;

        public int NumRegions;
        // number of Regions
        public Jump[] Jump;

        public int NumSyncs;
        // number of Syncs
        public Sync[] Sync;

        public int NumMarkers;
        // number of Markers
        public Marker[] Marker;

        public bool EndFlag;
        public bool InUse;
        public byte[] AllData;
        public int OffsetData;
        public byte[] ResPtr;
        public string Name;
        public short SoundId;
        public BundleMgr Bundle;
        public int Type;
        public int VolGroupId;
        public int Disk;
        public IAudioStream CompressedStream;
        public bool Compressed;
        public string LastFileName;

        public SoundDesc Clone()
        {
            var desc = new SoundDesc();
            desc.Freq = Freq;
            desc.Channels = Channels;
            desc.Bits = Bits;
            desc.NumJumps = NumJumps;
            desc.Region = new Region[Region.Length];
            Array.Copy(Region, desc.Region, Region.Length);
            desc.NumRegions = NumRegions;
            desc.Jump = new Jump[Jump.Length];
            Array.Copy(Jump, desc.Jump, Jump.Length);
            desc.NumSyncs = NumSyncs;
            desc.Sync = new Sync[Sync.Length];
            Array.Copy(Sync, desc.Sync, Sync.Length);
            desc.NumMarkers = NumMarkers;
            desc.Marker = new Marker[Marker.Length];
            Array.Copy(Marker, desc.Marker, Marker.Length);
            desc.EndFlag = EndFlag;
            desc.InUse = InUse;
            if (AllData != null)
            {
                desc.AllData = new byte[AllData.Length];
                Array.Copy(AllData, desc.AllData, AllData.Length);
            }
            desc.OffsetData = OffsetData;
            if (ResPtr != null)
            {
                desc.ResPtr = new byte[ResPtr.Length];
                Array.Copy(ResPtr, desc.ResPtr, ResPtr.Length);
            }
            desc.Name = Name;
            desc.SoundId = SoundId;
            desc.Bundle = Bundle;
            desc.Type = Type;
            desc.VolGroupId = VolGroupId;
            desc.Disk = Disk;
            desc.CompressedStream = CompressedStream;
            desc.Compressed = Compressed;
            desc.LastFileName = LastFileName;
            return desc;
        }

        public void Clear()
        {
            Freq = 0;
            Channels = 0;
            Bits = 0;
            NumJumps = 0;
            Region = null;
            NumRegions = 0;
            Jump = null;
            NumSyncs = 0;
            Sync = null;
            NumMarkers = 0;
            Marker = null;
            EndFlag = false;
            InUse = false;
            AllData = null;
            OffsetData = 0;
            ResPtr = null;
            Name = null;
            SoundId = 0;
            Bundle = null;
            Type = 0;
            VolGroupId = 0;
            Disk = 0;
            CompressedStream = null;
            Compressed = false;
            LastFileName = null;
        }
    }
}

