//
//  ImuseDigiSndMgr.cs
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
using System.Diagnostics;
using NScumm.Core.Audio.Decoders;
using NScumm.Core.IO;
using System.IO;

namespace NScumm.Core.Audio.IMuse
{
    class ImuseDigiSndMgr
    {
        public ImuseDigiSndMgr(ScummEngine scumm)
        {
            _vm = scumm;
            _disk = 0;
            _cacheBundleDir = new BundleDirCache();
            BundleCodecs.InitializeImcTables();
        }

        void CountElements(byte[] ptr, int posPtr, ref int numRegions, ref int numJumps, ref int numSyncs, ref int numMarkers)
        {
            string tag;
            int size = 0;
            int pos = posPtr;

            do
            {
                tag = ptr.ToText(pos);
                pos += 4;
                switch (tag)
                {
                    case "STOP":
                    case "FRMT":
                    case "DATA":
                        size = ptr.ToInt32BigEndian(pos);
                        pos += size + 4;
                        break;
                    case "TEXT":
                        if (string.Equals(ptr.ToText(pos + 8), "exit", StringComparison.OrdinalIgnoreCase))
                        {
                            numMarkers++;
                        }
                        size = ptr.ToInt32BigEndian(pos);
                        pos += size + 4;
                        break;
                    case "REGN":
                        numRegions++;
                        size = ptr.ToInt32BigEndian(pos);
                        pos += size + 4;
                        break;
                    case "JUMP":
                        numJumps++;
                        size = ptr.ToInt32BigEndian(pos);
                        pos += size + 4;
                        break;
                    case "SYNC":
                        numSyncs++;
                        size = ptr.ToInt32BigEndian(pos);
                        pos += size + 4;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("CountElements() Unknown sfx header '{0}'", tag));
                }
            } while (tag != "DATA");
        }

        void PrepareSoundFromRMAP(XorReader file, SoundDesc sound, int offset, int size)
        {
            // TODO: vs PrepareSoundFromRMAP
//            int l;
//        
//            file.Seek(offset, SeekOrigin.Begin);
//            uint tag = file.readUint32BE();
//            Debug.Assert(tag == MKTAG('R', 'M', 'A', 'P'));
//            int version = file.readUint32BE();
//            if (version != 3)
//            {
//                if (version == 2)
//                {
//                    warning("prepareSoundFromRMAP: Wrong version of compressed *.bun file, expected 3, but it's 2");
//                    warning("Suggested to recompress with latest tool from daily builds");
//                }
//                else
//                    error("prepareSoundFromRMAP: Wrong version number, expected 3, but it's: %d", version);
//            }
//            sound.bits = file.readUint32BE();
//            sound.freq = file.readUint32BE();
//            sound.channels = file.readUint32BE();
//            sound.numRegions = file.readUint32BE();
//            sound.numJumps = file.readUint32BE();
//            sound.numSyncs = file.readUint32BE();
//            if (version >= 3)
//                sound.numMarkers = file.readUint32BE();
//            else
//                sound.numMarkers = 0;
//        
//            sound.region = new Region[sound.numRegions];
//            Debug.Assert(sound.region);
//            sound.jump = new Jump[sound.numJumps];
//            Debug.Assert(sound.jump);
//            sound.sync = new Sync[sound.numSyncs];
//            Debug.Assert(sound.sync);
//            sound.marker = new Marker[sound.numMarkers];
//            Debug.Assert(sound.marker);
//        
//            for (l = 0; l < sound.numRegions; l++)
//            {
//                sound.region[l].offset = file.readUint32BE();
//                sound.region[l].length = file.readUint32BE();
//            }
//            for (l = 0; l < sound.numJumps; l++)
//            {
//                sound.jump[l].offset = file.readUint32BE();
//                sound.jump[l].dest = file.readUint32BE();
//                sound.jump[l].hookId = file.readUint32BE();
//                sound.jump[l].fadeDelay = file.readUint32BE();
//            }
//            for (l = 0; l < sound.numSyncs; l++)
//            {
//                sound.sync[l].size = file.readUint32BE();
//                sound.sync[l].ptr = new byte[sound.sync[l].size];
//                file.read(sound.sync[l].ptr, sound.sync[l].size);
//            }
//            if (version >= 3)
//            {
//                for (l = 0; l < sound.numMarkers; l++)
//                {
//                    sound.marker[l].pos = file.readUint32BE();
//                    sound.marker[l].length = file.readUint32BE();
//                    sound.marker[l].ptr = new char[sound.marker[l].length];
//                    file.read(sound.marker[l].ptr, sound.marker[l].length);
//                }
//            }
        }

        void PrepareSound(byte[] ptr, SoundDesc sound)
        {
            if (ptr.ToText() == "Crea")
            {
                bool quit = false;
                int len;

                int offset = ptr.ToInt16(20);
                int code = ptr.ToInt16(24);

                sound.numRegions = 0;
                sound.region = new Region[70];

                sound.numJumps = 0;
                sound.jump = new Jump[1];

                sound.numSyncs = 0;

                sound.resPtr = ptr;
                sound.bits = 8;
                sound.channels = 1;

                while (!quit)
                {
                    len = ptr.ToInt32(offset);
                    code = len & 0xFF;
                    if ((code != 0) && (code != 1) && (code != 6) && (code != 7))
                    {
                        // try again with 2 bytes forward (workaround for some FT sounds (ex.362, 363)
                        offset += 2;
                        len = ptr.ToInt32(offset);
                        code = len & 0xFF;
                        if ((code != 0) && (code != 1) && (code != 6) && (code != 7))
                        {
                            throw new NotSupportedException(string.Format("Invalid code in VOC file : {0}", code));
                        }
                    }
                    offset += 4;
                    len >>= 8;
                    switch (code)
                    {
                        case 0:
                            quit = true;
                            break;
                        case 1:
                            {
                                int time_constant = ptr[offset];
                                offset += 2;
                                len -= 2;
                                sound.freq = (ushort)VocStream.GetSampleRateFromVOCRate(time_constant);
                                sound.region[sound.numRegions].offset = offset;
                                sound.region[sound.numRegions].length = len;
                                sound.numRegions++;
                            }
                            break;
                        case 6: // begin of loop
                            sound.jump[0].dest = offset + 8;
                            sound.jump[0].hookId = 0;
                            sound.jump[0].fadeDelay = 0;
                            break;
                        case 7: // end of loop
                            sound.jump[0].offset = offset - 4;
                            sound.numJumps++;
                            sound.region[sound.numRegions].offset = offset - 4;
                            sound.region[sound.numRegions].length = 0;
                            sound.numRegions++;
                            break;
                        default:
                            throw new InvalidOperationException(string.Format("Invalid code in VOC file : {0}", code));
//                            quit = true;
//                            break;
                    }
                    offset += len;
                }
            }
            else if (ptr.ToText() == "iMUS")
            {
                string tag;
                int size = 0;
                int s_ptr = 0;
                int posPtr = 16;

                int curIndexRegion = 0;
                int curIndexJump = 0;
                int curIndexSync = 0;
                int curIndexMarker = 0;

                sound.numRegions = 0;
                sound.numJumps = 0;
                sound.numSyncs = 0;
                sound.numMarkers = 0;
                CountElements(ptr, posPtr, ref sound.numRegions, ref sound.numJumps, ref sound.numSyncs, ref sound.numMarkers);
                sound.region = new Region[sound.numRegions];
                sound.jump = new Jump[sound.numJumps];
                sound.sync = new Sync[sound.numSyncs];
                sound.marker = new Marker[sound.numMarkers];

                do
                {
                    tag = ptr.ToText(posPtr);
                    posPtr += 4;
                    switch (tag)
                    {
                        case "FRMT":
                            posPtr += 12;
                            sound.bits = (byte)ptr.ToUInt32BigEndian(posPtr);
                            posPtr += 4;
                            sound.freq = (ushort)ptr.ToUInt32BigEndian(posPtr);
                            posPtr += 4;
                            sound.channels = (byte)ptr.ToUInt32BigEndian(posPtr);
                            posPtr += 4;
                            break;
                        case "TEXT":
                            if (string.Equals(ptr.ToText(posPtr + 8), "exit"))
                            {
                                sound.marker[curIndexMarker].pos = ptr.ToInt32BigEndian(posPtr + 4);
                                sound.marker[curIndexMarker].ptr = ptr.GetText(posPtr + 8);
                                curIndexMarker++;
                            }
                            size = ptr.ToInt32BigEndian(posPtr);
                            posPtr += size + 4;
                            break;
                        case "STOP":
                            size = ptr.ToInt32BigEndian(posPtr);
                            posPtr += size + 4;
                            break;
                        case "REGN":
                            posPtr += 4;
                            sound.region[curIndexRegion].offset = ptr.ToInt32BigEndian(posPtr);
                            posPtr += 4;
                            sound.region[curIndexRegion].length = ptr.ToInt32BigEndian(posPtr);
                            posPtr += 4;
                            curIndexRegion++;
                            break;
                        case "JUMP":
                            posPtr += 4;
                            sound.jump[curIndexJump].offset = ptr.ToInt32BigEndian(posPtr);
                            posPtr += 4;
                            sound.jump[curIndexJump].dest = ptr.ToInt32BigEndian(posPtr);
                            posPtr += 4;
                            sound.jump[curIndexJump].hookId = (byte)ptr.ToInt32BigEndian(posPtr);
                            posPtr += 4;
                            sound.jump[curIndexJump].fadeDelay = (short)ptr.ToInt32BigEndian(posPtr);
                            posPtr += 4;
                            curIndexJump++;
                            break;
                        case "SYNC":
                            size = ptr.ToInt32BigEndian(posPtr);
                            posPtr += 4;
                            sound.sync[curIndexSync].ptr = new byte[size];
                            Array.Copy(ptr, posPtr, sound.sync[curIndexSync].ptr, 0, size);
                            curIndexSync++;
                            posPtr += size;
                            break;
                        case "DATA":
                            posPtr += 4;
                            break;
                        default:
                            throw new InvalidOperationException(
                                string.Format("ImuseDigiSndMgr::prepareSound({0}/{1}) Unknown sfx header '{2}'", sound.soundId, sound.name, tag));
                    }
                } while (tag != "DATA");
                sound.offsetData = posPtr - s_ptr;
            }
            else
            {
                throw new InvalidOperationException("ImuseDigiSndMgr::prepareSound(): Unknown sound format");
            }
        }

        SoundDesc AllocSlot()
        {
            for (int l = 0; l < IMuseDigital.MAX_IMUSE_SOUNDS; l++)
            {
                if (!_sounds[l].inUse)
                {
                    _sounds[l].inUse = true;
                    return _sounds[l];
                }
            }

            return null;
        }

        bool OpenMusicBundle(SoundDesc sound, ref int disk)
        {
            bool result = false;

            sound.bundle = new BundleMgr(_cacheBundleDir);

            if (_vm.Game.GameId == GameId.CurseOfMonkeyIsland)
            {
                if (_vm.Game.Features.HasFlag(GameFeatures.Demo))
                {
                    result = sound.bundle.Open("music.bun", ref sound.compressed);
                }
                else
                {
                    if (disk == -1)
                        disk = _vm.Variables[_vm.VariableCurrentDisk.Value];
                    var musicfile = string.Format("musdisk{0}.bun", disk);
//                    if (_disk != _vm.Variables[_vm.VariableCurrentDisk.Value])
//                    {
//                        _vm.IMuseDigital.ParseScriptCmds(0x1000, 0, 0, 0, 0, 0, 0, 0);
//                        _vm.IMuseDigital.ParseScriptCmds(0x2000, 0, 0, 0, 0, 0, 0, 0);
//                        _vm.IMuseDigital.StopAllSounds();
//                        sound.bundle.CloseFile();
//                    }E

                    result = sound.bundle.Open(musicfile, ref sound.compressed, true);

                    // FIXME: Shouldn't we only set _disk if result == true?
                    _disk = (byte)_vm.Variables[_vm.VariableCurrentDisk.Value];
                }
            }
            else if (_vm.Game.GameId == GameId.Dig)
                result = sound.bundle.Open("digmusic.bun", ref sound.compressed, true);
            else
                throw new InvalidOperationException("openMusicBundle() Don't know which bundle file to load");

            _vm.Variables[_vm.VariableMusicBundleLoaded.Value] = result ? 1 : 0;

            return result;
        }

        bool OpenVoiceBundle(SoundDesc sound, ref int disk)
        {
            bool result = false;

            sound.bundle = new BundleMgr(_cacheBundleDir);

            if (_vm.Game.GameId == GameId.CurseOfMonkeyIsland)
            {
                if (_vm.Game.Features.HasFlag(GameFeatures.Demo))
                {
                    result = sound.bundle.Open("voice.bun", ref sound.compressed);
                }
                else
                {

                    if (disk == -1)
                        disk = _vm.Variables[_vm.VariableCurrentDisk.Value];
                    var voxfile = string.Format("voxdisk{0}.bun", disk);
                    //          if (_disk != _vm.Variables[_vm.VariableCurrentDisk]) {
                    //              _vm._imuseDigital.parseScriptCmds(0x1000, 0, 0, 0, 0, 0, 0, 0);
                    //              _vm._imuseDigital.parseScriptCmds(0x2000, 0, 0, 0, 0, 0, 0, 0);
                    //              _vm._imuseDigital.stopAllSounds();
                    //              sound.bundle.closeFile();
                    //          }

                    result = sound.bundle.Open(voxfile, ref sound.compressed);

                    // FIXME: Shouldn't we only set _disk if result == true?
                    _disk = (byte)_vm.Variables[_vm.VariableCurrentDisk.Value];
                }
            }
            else if (_vm.Game.GameId == GameId.Dig)
                result = sound.bundle.Open("digvoice.bun", ref sound.compressed);
            else
                throw new InvalidOperationException("openVoiceBundle() Don't know which bundle file to load");

            _vm.Variables[_vm.VariableMusicBundleLoaded.Value] = result ? 1 : 0;

            return result;
        }

        public SoundDesc OpenSound(int soundId, string soundName, int soundType, int volGroupId, int disk)
        {
            Debug.Assert(soundId >= 0);
            Debug.Assert(soundType != 0);

            var sound = AllocSlot();
            if (sound == null)
            {
                throw new InvalidOperationException("openSound() can't alloc free sound slot");
            }

            bool header_outside = ((_vm.Game.GameId == GameId.CurseOfMonkeyIsland) && !(_vm.Game.Features.HasFlag(GameFeatures.Demo)));
            bool result = false;
            byte[] ptr = null;

            switch (soundType)
            {
                case IMuseDigital.IMUSE_RESOURCE:
                    Debug.Assert(string.IsNullOrEmpty(soundName));  // Paranoia check

//                    _vm.ensureResourceLoaded(rtSound, soundId);
                    // TODO: lock
//                    _vm._res.lock(rtSound, soundId);
                    ptr = _vm.ResourceManager.GetSound(soundId);
                    if (ptr == null)
                    {
                        CloseSound(sound);
                        return null;
                    }
                    sound.resPtr = ptr;
                    break;
                case IMuseDigital.IMUSE_BUNDLE:
                    if (volGroupId == IMuseDigital.IMUSE_VOLGRP_VOICE)
                        result = OpenVoiceBundle(sound, ref disk);
                    else if (volGroupId == IMuseDigital.IMUSE_VOLGRP_MUSIC)
                        result = OpenMusicBundle(sound, ref disk);
                    else
                        Console.Error.WriteLine("openSound() Don't know how load sound: {0}", soundId);
                    if (!result)
                    {
                        CloseSound(sound);
                        return null;
                    }
                    if (sound.compressed)
                    {
                        int offset = 0, size = 0;
                        var fileName = string.Format("{0}.map", soundName);
                        var rmapFile = sound.bundle.GetFile(fileName, ref offset, ref size);
                        if (rmapFile == null)
                        {
                            CloseSound(sound);
                            return null;
                        }
                        PrepareSoundFromRMAP(rmapFile, sound, offset, size);
                        sound.name = soundName;
                        sound.soundId = (short)soundId;
                        sound.type = soundType;
                        sound.volGroupId = volGroupId;
                        sound.disk = disk;
                        return sound;
                    }
                    else if (soundName[0] == 0)
                    {
                        if (sound.bundle.DecompressSampleByIndex(soundId, 0, 0x2000, out ptr, 0, header_outside) == 0 || ptr == null)
                        {
                            CloseSound(sound);
                            return null;
                        }
                    }
                    else
                    {
                        if (sound.bundle.DecompressSampleByName(soundName, 0, 0x2000, out ptr, header_outside) == 0 || ptr == null)
                        {
                            CloseSound(sound);
                            return null;
                        }
                    }
                    sound.resPtr = null;
                    break;
                default:
                    Console.Error.WriteLine("openSound() Unknown soundType {0} (trying to load sound {1})", soundType, soundId);
                    break;
            }

            sound.name = soundName;
            sound.soundId = (short)soundId;
            sound.type = soundType;
            sound.volGroupId = volGroupId;
            sound.disk = _disk;
            PrepareSound(ptr, sound);

            return sound;
        }

        public void CloseSound(SoundDesc soundDesc)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));

            if (soundDesc.resPtr != null)
            {
                bool found = false;
                for (int l = 0; l < IMuseDigital.MAX_IMUSE_SOUNDS; l++)
                {
                    if ((_sounds[l].soundId == soundDesc.soundId) && (_sounds[l] != soundDesc))
                        found = true;
                }
                // TODO: unlock
//                if (!found)
//                    _vm._res.unlock(rtSound, soundDesc.soundId);
            }

            if (soundDesc.compressedStream != null)
            {
                soundDesc.compressedStream.Dispose();
            }
            soundDesc.bundle = null;

            for (int r = 0; r < soundDesc.numSyncs; r++)
                soundDesc.sync[r].ptr = null;
            for (int r = 0; r < soundDesc.numMarkers; r++)
                soundDesc.marker[r].ptr = null;
            soundDesc.region = null;
            soundDesc.jump = null;
            soundDesc.sync = null;
            soundDesc.marker = null;
            soundDesc.Clear();
        }

        public SoundDesc CloneSound(SoundDesc soundDesc)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));

            var desc = OpenSound(soundDesc.soundId, soundDesc.name, soundDesc.type, soundDesc.volGroupId, soundDesc.disk);
            if (desc == null)
                desc = OpenSound(soundDesc.soundId, soundDesc.name, soundDesc.type, soundDesc.volGroupId, 1);
            if (desc == null)
                desc = OpenSound(soundDesc.soundId, soundDesc.name, soundDesc.type, soundDesc.volGroupId, 2);
            return desc;
        }

        bool CheckForProperHandle(SoundDesc soundDesc)
        {
            if (soundDesc == null)
                return false;
            for (int l = 0; l < IMuseDigital.MAX_IMUSE_SOUNDS; l++)
            {
                if (soundDesc == _sounds[l])
                    return true;
            }
            return false;
        }

        public bool IsSndDataExtComp(SoundDesc soundDesc)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));
            return soundDesc.compressed;
        }

        public int GetFreq(SoundDesc soundDesc)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));
            return soundDesc.freq;
        }

        public int GetBits(SoundDesc soundDesc)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));
            return soundDesc.bits;
        }

        public int GetChannels(SoundDesc soundDesc)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));
            return soundDesc.channels;
        }

        public bool IsEndOfRegion(SoundDesc soundDesc, int region)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(region >= 0 && region < soundDesc.numRegions);
            return soundDesc.endFlag;
        }

        public int GetNumRegions(SoundDesc soundDesc)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));
            return soundDesc.numRegions;
        }

        int GetNumJumps(SoundDesc soundDesc)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));
            return soundDesc.numJumps;
        }

        public int GetRegionOffset(SoundDesc soundDesc, int region)
        {
            Debug.WriteLine("getRegionOffset() region:{0}", region);
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(region >= 0 && region < soundDesc.numRegions);
            return soundDesc.region[region].offset;
        }

        public int GetJumpIdByRegionAndHookId(SoundDesc soundDesc, int region, int hookId)
        {
            Debug.WriteLine("getJumpIdByRegionAndHookId() region:{0}, hookId:{1}", region, hookId);
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(region >= 0 && region < soundDesc.numRegions);
            var offset = soundDesc.region[region].offset;
            for (var l = 0; l < soundDesc.numJumps; l++)
            {
                if (offset == soundDesc.jump[l].offset)
                {
                    if (soundDesc.jump[l].hookId == hookId)
                        return l;
                }
            }

            return -1;
        }

        public bool CheckForTriggerByRegionAndMarker(SoundDesc soundDesc, int region, string marker)
        {
            Debug.WriteLine("checkForTriggerByRegionAndMarker() region:{0}, marker:{1}", region, marker);
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(region >= 0 && region < soundDesc.numRegions);

            var offset = soundDesc.region[region].offset;
            for (int l = 0; l < soundDesc.numMarkers; l++)
            {
                if (offset == soundDesc.marker[l].pos)
                {
                    if (soundDesc.marker[l].ptr == marker)
                        return true;
                }
            }

            return false;
        }

        public void GetSyncSizeAndPtrById(SoundDesc soundDesc, int number, out int sync_size, out byte[] sync_ptr)
        {
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(number >= 0);
            if (number < soundDesc.numSyncs)
            {
                sync_size = soundDesc.sync[number].ptr.Length;
                sync_ptr = soundDesc.sync[number].ptr;
            }
            else
            {
                sync_size = 0;
                sync_ptr = null;
            }
        }

        public int GetRegionIdByJumpId(SoundDesc soundDesc, int jumpId)
        {
            Debug.WriteLine("getRegionIdByJumpId() jumpId:{0}", jumpId);
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(jumpId >= 0 && jumpId < soundDesc.numJumps);

            int dest = soundDesc.jump[jumpId].dest;
            for (int l = 0; l < soundDesc.numRegions; l++)
            {
                if (dest == soundDesc.region[l].offset)
                {
                    return l;
                }
            }

            return -1;
        }

        public int GetJumpHookId(SoundDesc soundDesc, int number)
        {
            Debug.WriteLine("getJumpHookId() number:{0}", number);
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(number >= 0 && number < soundDesc.numJumps);
            return soundDesc.jump[number].hookId;
        }

        public int GetJumpFade(SoundDesc soundDesc, int number)
        {
            Debug.WriteLine("getJumpFade() number:{0}", number);
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(number >= 0 && number < soundDesc.numJumps);
            return soundDesc.jump[number].fadeDelay;
        }

        public int GetDataFromRegion(SoundDesc soundDesc, int region, out byte[] buf, int offset, int size)
        {
            Debug.WriteLine("getDataFromRegion() region:{0}, offset:{1}, size:{2}, numRegions:{3}", region, offset, size, soundDesc.numRegions);
            Debug.Assert(CheckForProperHandle(soundDesc));
            Debug.Assert(offset >= 0 && size >= 0);
            Debug.Assert(region >= 0 && region < soundDesc.numRegions);

            buf = null;
            int region_offset = soundDesc.region[region].offset;
            int region_length = soundDesc.region[region].length;
            int offset_data = soundDesc.offsetData;
            int start = region_offset - offset_data;

            if (offset + size + offset_data > region_length)
            {
                size = region_length - offset;
                soundDesc.endFlag = true;
            }
            else
            {
                soundDesc.endFlag = false;
            }

            int header_size = soundDesc.offsetData;
            bool header_outside = ((_vm.Game.GameId == GameId.CurseOfMonkeyIsland) && !(_vm.Game.Features.HasFlag(GameFeatures.Demo)));
            if ((soundDesc.bundle != null) && (!soundDesc.compressed))
            {
                size = soundDesc.bundle.DecompressSampleByCurIndex(start + offset, size, out buf, header_size, header_outside);
            }
            else if (soundDesc.resPtr != null)
            {
                buf = new byte[size];
                Array.Copy(soundDesc.resPtr, start + offset + header_size, buf, 0, size);
            }
            else if ((soundDesc.bundle != null) && (soundDesc.compressed))
            {
                buf = new byte[size];

                int offsetMs = (((offset * 8 * 10) / soundDesc.bits) / (soundDesc.channels * soundDesc.freq)) * 100;
                var fileName = string.Format("{0}_reg{1:D3}", soundDesc.name, region);
                if (fileName != soundDesc.lastFileName)
                {
                    int offs = 0, len = 0;

#if USE_FLAC || USE_VORBIS || USE_MAD
            uint8 soundMode = 0;
#endif

                    fileName = string.Format("{0}_reg{1:D3}.fla", soundDesc.name, region);
                    var cmpFile = soundDesc.bundle.GetFile(fileName, ref offs, ref len);
                    if (len != 0)
                    {
#if !USE_FLAC
                        Console.Error.WriteLine("FLAC library compiled support needed");
#else
                soundMode = 3;
#endif
                    }
                    if (len == 0)
                    {
                        fileName = string.Format("{0}_reg{1:D3}.ogg", soundDesc.name, region);
                        cmpFile = soundDesc.bundle.GetFile(fileName, ref offs, ref len);
                        if (len != 0)
                        {
#if !USE_VORBIS
                            Console.Error.WriteLine("Vorbis library compiled support needed");
#else
                    soundMode = 2;
#endif
                        }
                    }
                    if (len == 0)
                    {
                        fileName = string.Format("{0}_reg{1:D3}.mp3", soundDesc.name, region);
                        cmpFile = soundDesc.bundle.GetFile(fileName, ref offs, ref len);
                        if (len != 0)
                        {
#if !USE_MAD
                            Console.Error.WriteLine("Mad library compiled support needed");
#else
                    soundMode = 1;
#endif
                        }
                    }
                    Debug.Assert(len != 0);

                    if (soundDesc.compressedStream == null)
                    {
//                        var tmp = cmpFile.ReadStream(len);
//                        Debug.Assert(tmp);
#if USE_FLAC
                if (soundMode == 3)
                    soundDesc.compressedStream = Audio::makeFLACStream(tmp, DisposeAfterUse::YES);
#endif
#if USE_VORBIS
                if (soundMode == 2)
                    soundDesc.compressedStream = Audio::makeVorbisStream(tmp, DisposeAfterUse::YES);
#endif
#if USE_MAD
                if (soundMode == 1)
                    soundDesc.compressedStream = Audio::makeMP3Stream(tmp, DisposeAfterUse::YES);
#endif
//                        Debug.Assert(soundDesc.compressedStream!=null);
//                        soundDesc.compressedStream.Seek(offsetMs);
                    }
                    soundDesc.lastFileName = fileName;
                }
                var tmpBuf = new short[size / 2];
                size = soundDesc.compressedStream.ReadBuffer(tmpBuf) * 2;
                // TODO: vs check this
                for (int i = 0; i < size / 2; i++)
                {
                    buf[i * 2] = (byte)(tmpBuf[i] & 0xFF);
                    buf[i * 2 + 1] = (byte)((tmpBuf[i] & 0xFF00) >> 8);
                }
                if (soundDesc.compressedStream.IsEndOfData || soundDesc.endFlag)
                {
                    soundDesc.compressedStream.Dispose();
                    soundDesc.compressedStream = null;
                    soundDesc.lastFileName = null;
                    soundDesc.endFlag = true;
                }
            }

            return size;
        }

        static SoundDesc[] CreateSounds()
        {
            var sounds = new SoundDesc[IMuseDigital.MAX_IMUSE_SOUNDS];
            for (int i = 0; i < sounds.Length; i++)
            {
                sounds[i] = new SoundDesc();
            }
            return sounds;
        }

        SoundDesc[] _sounds = CreateSounds();

        ScummEngine _vm;
        byte _disk;
        BundleDirCache _cacheBundleDir;
    }
}

