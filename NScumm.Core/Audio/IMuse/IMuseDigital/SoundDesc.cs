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

namespace NScumm.Core
{
    struct Region
    {
        public int offset;
        // offset of region
        public int length;
        // length of region
    }

    struct Jump
    {
        public int offset;
        // jump offset position
        public int dest;
        // jump to dest position
        public byte hookId;
        // id of hook
        public short fadeDelay;
        // fade delay in ms
    }

    struct Sync
    {
        public byte[] ptr;
    }

    struct Marker
    {
        /// <summary>
        /// Marker in sound data.
        /// </summary>
        public int pos;
        public string ptr;
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    class SoundDesc: ICloneable
    {
        internal string DebuggerDisplay
        {
            get
            { 
                return inUse ? string.Format("SoundId {0}", soundId) : string.Empty;
            }    
        }

        public ushort freq;
        // frequency
        public byte channels;
        // stereo or mono
        public byte bits;
        // 8, 12, 16

        public int numJumps;
        // number of Jumps
        public Region[] region;

        public int numRegions;
        // number of Regions
        public Jump[] jump;

        public int numSyncs;
        // number of Syncs
        public Sync[] sync;

        public int numMarkers;
        // number of Markers
        public Marker[] marker;

        public bool endFlag;
        public bool inUse;
        public byte[] allData;
        public int offsetData;
        public byte[] resPtr;
        public string name;
        public short soundId;
        public BundleMgr bundle;
        public int type;
        public int volGroupId;
        public int disk;
        public IAudioStream compressedStream;
        public bool compressed;
        public string lastFileName;

        public SoundDesc Clone()
        {
            SoundDesc desc = new SoundDesc();
            desc.freq = freq;
            desc.channels = channels;
            desc.bits = bits;
            desc.numJumps = numJumps;
            desc.region = new Region[region.Length];
            Array.Copy(region, desc.region, region.Length);
            desc.numRegions = numRegions;
            desc.jump = new Jump[jump.Length];
            Array.Copy(jump, desc.jump, jump.Length);
            desc.numSyncs = numSyncs;
            desc.sync = new Sync[sync.Length];
            Array.Copy(sync, desc.sync, sync.Length);
            desc.numMarkers = numMarkers;
            desc.marker = new Marker[marker.Length];
            Array.Copy(marker, desc.marker, marker.Length);
            desc.endFlag = endFlag;
            desc.inUse = inUse;
            if (allData != null)
            {
                desc.allData = new byte[allData.Length];
                Array.Copy(allData, desc.allData, allData.Length);
            }
            desc.offsetData = offsetData;
            if (resPtr != null)
            {
                desc.resPtr = new byte[resPtr.Length];
                Array.Copy(resPtr, desc.resPtr, resPtr.Length);
            }
            desc.name = name;
            desc.soundId = soundId;
            desc.bundle = bundle;
            desc.type = type;
            desc.volGroupId = volGroupId;
            desc.disk = disk;
            desc.compressedStream = compressedStream;
            desc.compressed = compressed;
            desc.lastFileName = lastFileName;
            return desc;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public void Clear()
        {
            freq = 0;
            channels = 0;
            bits = 0;
            numJumps = 0;
            region = null;
            numRegions = 0;
            jump = null;
            numSyncs = 0;
            sync = null;
            numMarkers = 0;
            marker = null;
            endFlag = false;
            inUse = false;
            allData = null;
            offsetData = 0;
            resPtr = null;
            name = null;
            soundId = 0;
            bundle = null;
            type = 0;
            volGroupId = 0;
            disk = 0;
            compressedStream = null;
            compressed = false;
            lastFileName = null;
        }
    }
}

