//
//  ResourceManager.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace NScumm.Core.IO
{
    public class Script
    {
        public int Id
        {
            get;
            private set;
        }

        public byte[] Data
        {
            get;
            private set;
        }

        public Script(int id, byte[] data)
        {
            Id = id;
            Data = data;
        }
    }

    public abstract class ResourceManager : IEnableTrace
    {
        protected GameInfo Game { get; private set; }

        protected ResourceIndex Index { get; private set; }

        public byte[] ObjectOwnerTable { get { return Index.ObjectOwnerTable; } }

        public byte[] ObjectStateTable { get { return Index.ObjectStateTable; } }

        public uint[] ClassData { get { return Index.ClassData; } }

        public string[] AudioNames { get { return Index.AudioNames; } }

        public List<ArrayDefinition> ArrayDefinitions { get; private set; }

        public string Directory { get; private set; }

        public IEnumerable<Room> Rooms
        {
            get
            {
                var roomIndices = Game.Version == 0 ? Enumerable.Range(1, 56).Select(r => (byte)r) :
                    (from res in Enumerable.Range(1, Index.RoomResources.Count)
                     where Index.RoomResources[res].RoomNum != 0 && Index.RoomResources[res].Offset != 0xFFFFFFFF
                     select (byte)res).Distinct();
                Room room = null;
                foreach (var i in roomIndices)
                {
                    try
                    {
                        room = GetRoom(i);
                    }
                    catch (Exception)
                    {
                        //                        Console.ForegroundColor = ConsoleColor.Red;
                        //                        Console.WriteLine(e);
                        //                        Console.ResetColor();
                        room = null;
                    }
                    if (room != null)
                    {
                        yield return room;
                    }
                }
            }
        }

        public IEnumerable<Script> Scripts
        {
            get
            {
                for (var i = 1; i < Index.ScriptResources.Count; i++)
                {
                    if (Index.ScriptResources[i].RoomNum != 0 && Index.ScriptResources[i].Offset != 0xFFFFFFFF)
                    {
                        byte[] script = null;
                        try
                        {
                            script = GetScript(i);
                        }
                        catch (NotSupportedException)
                        {
                            // TODO: mmmh suspicious script error
                        }
                        if (script != null)
                        {
                            yield return new Script(i, script);
                        }
                    }
                }
            }
        }


        public IEnumerable<byte[]> GetSounds(Audio.MusicDriverTypes music)
        {
            for (byte i = 0; i < Index.SoundResources.Count; i++)
            {
                if (Index.SoundResources[i].RoomNum != 0)
                {
                    yield return GetSound(music, i);
                }
            }
        }

        public int NumVerbs { get { return Index.NumVerbs; } }

        public int NumVariables { get { return Index.NumVariables; } }

        public int NumBitVariables { get { return Index.NumBitVariables; } }

        public int NumInventory { get { return Index.NumInventory; } }

        public int NumLocalObjects { get { return Index.NumLocalObjects; } }

        public int NumArray { get { return Index.NumArray; } }

        public int NumGlobalScripts { get { return Index.NumGlobalScripts; } }

        public byte[] ObjectRoomTable { get { return Index.ObjectRoomTable; } }

        protected ResourceManager(GameInfo game)
        {
            Game = game;
            Index = ResourceIndex.Load(game);
            Directory = ServiceLocator.FileStorage.GetDirectoryName(game.Path);
            ArrayDefinitions = Index.ArrayDefinitions;
        }

        public void LoadRoom(int id)
        {
            if (id == 0 || _rooms.ContainsKey(id)) return;

            NukeResource(ResType.Room, id);

            Room room = null;
            var disk = OpenRoom((byte)id);
            if (disk != null)
            {
                var roomOffset = GetRoomOffset(disk, (byte)id);
                room = disk.ReadRoom(roomOffset);
                room.Number = id;
                room.Name = Index.RoomNames != null && Index.RoomNames.ContainsKey((byte)id) ? Index.RoomNames[(byte)id] : null;
            }

            ExpireResources(room.Size);
            _rooms[id] = room;
            _allocatedSize += room.Size;
            SetRoomCounter(id, 1);
        }

        public void LoadCostume(int id)
        {
            if (id == 0 || _costumes.ContainsKey(id)) return;

            NukeResource(ResType.Costume, id);

            byte[] data = null;
            var res = Index.CostumeResources[id];
            if (res.RoomNum != 0)
            {
                var disk = OpenRoom(res.RoomNum);
                if (disk != null)
                {
                    var roomOffset = GetRoomOffset(disk, res.RoomNum);
                    data = disk.ReadCostume(roomOffset + res.Offset);
                }
            }

            if (data != null)
            {
                ExpireResources(data.Length);
                _costumes[id] = data;
                _allocatedSize += data.Length;
                SetCostumeCounter(id, 1);
            }
        }

        public void LoadScript(int id)
        {
            if (id == 0 || _scripts.ContainsKey(id)) return;

            NukeResource(ResType.Script, id);

            byte[] data = null;
            var resource = Index.ScriptResources[id];
            var disk = OpenRoom(resource.RoomNum);
            if (disk != null)
            {
                var roomOffset = GetRoomOffset(disk, resource.RoomNum);
                data = disk.ReadScript(roomOffset + resource.Offset);
            }

            if (data != null)
            {
                ExpireResources(data.Length);
                _scripts[id] = data;
                _allocatedSize += data.Length;
                SetScriptCounter(id, 1);
            }
        }

        public void LoadSound(Audio.MusicDriverTypes music, int id)
        {
            if (id == 0 || _sounds.ContainsKey(id)) return;

            NukeResource(ResType.Sound, id);

            byte[] data = null;
            var resource = Index.SoundResources[id];
            if (resource.RoomNum != 0)
            {
                var disk = OpenRoom(resource.RoomNum);
                if (disk != null)
                {
                    var roomOffset = GetRoomOffset(disk, resource.RoomNum);
                    if (Game.IsOldBundle && Game.Version == 3 && Game.Platform == Platform.Amiga)
                    {
                        data = ((ResourceFile3_16)disk).ReadAmigaSound(roomOffset + resource.Offset);
                    }
                    else if (Game.Version == 3 && (Game.Platform == Platform.Amiga || Game.Platform == Platform.FMTowns))
                    {
                        data = ((ResourceFile3)disk).ReadAmigaSound(roomOffset + resource.Offset);
                    }
                    else if (Game.Version == 4 && Game.Platform == Platform.Amiga)
                    {
                        data = ((ResourceFile4)disk).ReadAmigaSound(roomOffset + resource.Offset);
                    }
                    else
                    {
                        data = disk.ReadSound(music, roomOffset + resource.Offset);
                        // For games using AD except Indy3 and Loom we are using our iMuse
                        // implementation. See output initialization in
                        // ScummEngine::setupMusic for more information.
                        if (data != null && Game.Version < 5 && Game.GameId != GameId.Indy3 && Game.GameId != GameId.Loom && music == Audio.MusicDriverTypes.AdLib)
                        {
                            data = ConvertADResource(data, id);
                        }
                    }
                }
            }

            if (data != null)
            {
                ExpireResources(data.Length);
                _sounds[id] = data;
                _allocatedSize += data.Length;
                SetSoundCounter(id, 1);
            }
        }

        public bool IsSoundLoaded(int sound)
        {
            return _sounds.ContainsKey(sound);
        }

        public void LockRoom(int resid)
        {
            _roomsLock.Add(resid);
        }

        public void UnlockRoom(int resid)
        {
            _roomsLock.Remove(resid);
        }

        public void LockCostume(int resid)
        {
            _costumesLock.Add(resid);
        }

        public void UnlockCostume(int resid)
        {
            _costumesLock.Remove(resid);
        }

        public void LockScript(int resid)
        {
            _scriptsLock.Add(resid);
        }

        public void UnlockScript(int resid)
        {
            _scriptsLock.Remove(resid);
        }

        public void LockSound(int resid)
        {
            _soundsLock.Add(resid);
        }

        public void UnlockSound(int resid)
        {
            _soundsLock.Remove(resid);
        }

        public void SetRoomCounter(int id, int counter)
        {
            _roomsCounter[id] = counter;
        }

        public int GetRoomCounter(int id)
        {
            return _roomsCounter[id];
        }

        public void SetCostumeCounter(int id, int counter)
        {
            _costumesCounter[id] = counter;
        }

        public int GetCostumeCounter(int id)
        {
            return _costumesCounter[id];
        }

        public void SetScriptCounter(int id, int counter)
        {
            _scriptsCounter[id] = counter;
        }

        public int GetScriptCounter(int id)
        {
            return _scriptsCounter[id];
        }

        public void SetSoundCounter(int id, int counter)
        {
            _soundsCounter[id] = counter;
        }

        public int GetSoundCounter(int id)
        {
            return _soundsCounter[id];
        }

        void ExpireResources(int size)
        {
            int best_counter;
            ResType best_type;
            int best_res = 0;
            int oldAllocatedSize;

            if (_expireCounter != 0xFF)
            {
                _expireCounter = 0xFF;
                IncreaseResourceCounters();
            }

            if (size + _allocatedSize < _maxHeapThreshold)
                return;

            oldAllocatedSize = _allocatedSize;

            do
            {
                best_type = ResType.Invalid;
                best_counter = 2;

                var types = new[] { ResType.Room, ResType.Script, ResType.Sound, ResType.Costume };
                foreach (ResType type in types)
                {
                    // Resources of this type can be reloaded from the data files,
                    // so we can potentially unload them to free memory.
                    var typeCounter = GetCounter(type);
                    var typeLock = GetLock(type);
                    foreach (var pair in typeCounter)
                    {
                        var counter = pair.Value;

                        if (!typeLock.Contains(pair.Key) && counter >= best_counter && (ScummEngine.Instance != null && !ScummEngine.Instance.IsResourceInUse(type, pair.Key)) /*&& !tmp.isOffHeap()*/)
                        {
                            best_counter = counter;
                            best_type = type;
                            best_res = pair.Key;
                        }
                    }
                }

                if (best_type == ResType.Invalid)
                    break;
                NukeResource(best_type, best_res);
            } while (size + _allocatedSize > _minHeapThreshold);

            IncreaseResourceCounters();

            this.Trace().Write("resource", "Expired resources, mem {0} -> {1}", oldAllocatedSize, _allocatedSize);
        }

        void NukeResource(ResType type, int idx)
        {
            Dictionary<int, byte[]> res;
            var resCounter = GetCounter(type);
            switch (type)
            {
                case ResType.Room:
                    if (_rooms.ContainsKey(idx))
                    {
                        var data = _rooms[idx];
                        _allocatedSize -= data.Size;
                        _rooms.Remove(idx);
                        resCounter.Remove(idx);
                    }
                    return;
                case ResType.Script:
                    res = _scripts;
                    break;
                case ResType.Costume:
                    res = _costumes;
                    break;
                case ResType.Sound:
                    res = _sounds;
                    break;
                default:
                    return;
            }
            if (res.ContainsKey(idx))
            {
                this.Trace().Write("Resource", "NukeResource({0},{1})", type, idx);
                var data = res[idx];
                _allocatedSize -= data.Length;
                res.Remove(idx);
                resCounter.Remove(idx);
            }
        }

        IDictionary<int, int> GetCounter(ResType type)
        {
            Dictionary<int, int> resCounter;
            switch (type)
            {
                case ResType.Room:
                    resCounter = _roomsCounter;
                    break;
                case ResType.Script:
                    resCounter = _scriptsCounter;
                    break;
                case ResType.Costume:
                    resCounter = _costumesCounter;
                    break;
                case ResType.Sound:
                    resCounter = _soundsCounter;
                    break;
                default:
                    return null;
            }
            return resCounter;
        }

        IDictionary<int, byte[]> GetResources(ResType type)
        {
            switch (type)
            {
                case ResType.Script:
                    return _scripts;
                case ResType.Costume:
                    return _costumes;
                case ResType.Sound:
                    return _sounds;
                default:
                    return null;
            }
        }

        HashSet<int> GetLock(ResType type)
        {
            HashSet<int> resLock;
            switch (type)
            {
                case ResType.Room:
                    resLock = _roomsLock;
                    break;
                case ResType.Script:
                    resLock = _scriptsLock;
                    break;
                case ResType.Costume:
                    resLock = _costumesLock;
                    break;
                case ResType.Sound:
                    resLock = _soundsLock;
                    break;
                default:
                    return null;
            }
            return resLock;
        }

        void IncreaseExpireCounter()
        {
            ++_expireCounter;
            if (_expireCounter == 0)
            {   // overflow?
                IncreaseResourceCounters();
            }
        }

        private void IncreaseResourceCounters()
        {
            IncreaseResourceCounter(_costumesCounter);
            IncreaseResourceCounter(_roomsCounter);
            IncreaseResourceCounter(_scriptsCounter);
            IncreaseResourceCounter(_soundsCounter);
        }

        private void IncreaseResourceCounter(IDictionary<int, int> counters)
        {
            foreach (var pair in counters.ToList())
            {
                var counter = pair.Value;
                if (counter != 0 && counter < ResourceUsageMax)
                {
                    counters[pair.Key]++;
                }
            }
        }

        public static ResourceManager Load(GameInfo game)
        {
            switch (game.Version)
            {
                case 0:
                    return new ResourceManager0(game);
                case 1:
                case 2:
                    return new ResourceManager2(game);
                case 3:
                    return new ResourceManager3(game);
                case 4:
                    return new ResourceManager4(game);
                case 5:
                    return new ResourceManager5(game);
                case 6:
                    return new ResourceManager6(game);
                case 7:
                    return new ResourceManager7(game);
                case 8:
                    return new ResourceManager8(game);
                default:
                    throw new NotSupportedException(string.Format("ResourceManager {0} is not supported", game.Version));
            }
        }

        static long GetRoomOffset(ResourceFile disk, byte roomNum)
        {
            var rOffset = disk.GetRoomOffset(roomNum);
            return rOffset;
        }

        public Room GetRoom(byte roomNum)
        {
            if (!_rooms.ContainsKey(roomNum))
            {
                LoadRoom(roomNum);
            }
            return _rooms.ContainsKey(roomNum) ? _rooms[roomNum] : null;
        }

        public byte[] GetCostumeData(int id)
        {
            return GetResource(_costumes, id, () => LoadCostume(id));
        }

        public byte[] GetCharsetData(byte id)
        {
            return GetResource(_charsets, id, () => _charsets[id] = ReadCharset(id));
        }

        public byte[] GetScript(int id)
        {
            return GetResource(_scripts, id, () => LoadScript(id));
        }

        public byte[] GetSound(Audio.MusicDriverTypes music, int id)
        {
            return GetResource(_sounds, id, () => LoadSound(music, id));
        }

        byte[] GetResource(Dictionary<int, byte[]> resources, int id, Action loadResource)
        {
            if (!resources.ContainsKey(id))
            {
                loadResource();
            }
            return resources.ContainsKey(id) ? resources[id] : null;
        }

        protected abstract ResourceFile OpenRoom(byte roomIndex);

        protected abstract byte[] ReadCharset(byte id);

        // AdLib MIDI-SYSEX to set MIDI instruments for small header games.
        static readonly byte[] ADLIB_INSTR_MIDI_HACK =
            {
                0x00, 0xf0, 0x14, 0x7d, 0x00,  // sysex 00: part on/off
                0x00, 0x00, 0x03,              // part/channel  (offset  5)
                0x00, 0x00, 0x07, 0x0f, 0x00, 0x00, 0x08, 0x00,
                0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0xf7,
                0x00, 0xf0, 0x41, 0x7d, 0x10,  // sysex 16: set instrument
                0x00, 0x01,                    // part/channel  (offset 28)
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xf7,
                0x00, 0xb0, 0x07, 0x64        // Controller 7 = 100 (offset 92)
            };

        static readonly byte[] map_param =
            {
                0, 2, 3, 4, 8, 9, 0,
            };

        static readonly ushort[] num_steps_table =
            {
                1, 2, 4, 5,
                6, 7, 8, 9,
                10, 12, 14, 16,
                18, 21, 24, 30,
                36, 50, 64, 82,
                100, 136, 160, 192,
                240, 276, 340, 460,
                600, 860, 1200, 1600
            };

        static readonly byte[] freq2note =
            {
            /*128*/
                        6, 6, 6, 6,
            /*132*/
                        7, 7, 7, 7, 7, 7, 7,
            /*139*/
                        8, 8, 8, 8, 8, 8, 8, 8, 8,
            /*148*/
                        9, 9, 9, 9, 9, 9, 9, 9, 9,
            /*157*/
                        10, 10, 10, 10, 10, 10, 10, 10, 10,
            /*166*/
                        11, 11, 11, 11, 11, 11, 11, 11, 11, 11,
            /*176*/
                        12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
            /*186*/
                        13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
            /*197*/
                        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
            /*209*/
                        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            /*222*/
                        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
            /*235*/
                        17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17,
            /*249*/
                        18, 18, 18, 18, 18, 18, 18
            };

        const int MIDIHeaderSize = 46;
        const int ResourceUsageMax = 0x7F;

        static void WriteMIDIHeader(byte[] input, string type, int ppqn, int totalSize)
        {
            int pos = 0;
            Array.Copy(System.Text.Encoding.UTF8.GetBytes(type), 0, input, pos, 4);
            pos += 4;
            Array.Copy(ScummHelper.GetBytesBigEndian((uint)totalSize), 0, input, pos, 4);
            pos += 4;
            Array.Copy(System.Text.Encoding.UTF8.GetBytes("MDhd"), 0, input, pos, 4);
            pos += 4;
            Array.Copy(new byte[] { 0, 0, 0, 8 }, 0, input, pos, 4);
            pos += 4;
            Array.Copy(new byte[8], 0, input, pos, 8);
            pos += 8;
            Array.Copy(System.Text.Encoding.UTF8.GetBytes("MThd"), 0, input, pos, 4);
            pos += 4;
            Array.Copy(new byte[] { 0, 0, 0, 6 }, 0, input, pos, 4);
            pos += 4;
            Array.Copy(new byte[] { 0, 0, 0, 1 }, 0, input, pos, 4);
            pos += 4; // MIDI format 0 with 1 track
            input[pos++] = (byte)(ppqn >> 8);
            input[pos++] = (byte)(ppqn & 0xFF);
            Array.Copy(System.Text.Encoding.UTF8.GetBytes("MTrk"), 0, input, pos, 4);
            pos += 4;
            Array.Copy(ScummHelper.GetBytesBigEndian((uint)totalSize), 0, input, pos, 4);
            pos += 4;
        }

        static int WriteVLQ(byte[] ptr, int outPos, int value)
        {
            if (value > 0x7f)
            {
                if (value > 0x3fff)
                {
                    ptr[outPos++] = (byte)((value >> 14) | 0x80);
                    value &= 0x3fff;
                }
                ptr[outPos++] = (byte)((value >> 7) | 0x80);
                value &= 0x7f;
            }
            ptr[outPos++] = (byte)value;
            return outPos;
        }

        static int ConvertExtraflags(byte[] ptr, int destIndex, byte[] srcPtr, int inPos)
        {
            int flags = srcPtr[inPos + 0];

            int t1, t2, t3, t4, time;
            int v1, v2, v3;

            if (0 == (flags & 0x80))
                return -1;

            t1 = (srcPtr[inPos + 1] & 0xf0) >> 3;
            t2 = (srcPtr[inPos + 2] & 0xf0) >> 3;
            t3 = (srcPtr[inPos + 3] & 0xf0) >> 3 | ((flags & 0x40) != 0 ? 0x80 : 0);
            t4 = (srcPtr[inPos + 3] & 0x0f) << 1;
            v1 = (srcPtr[inPos + 1] & 0x0f);
            v2 = (srcPtr[inPos + 2] & 0x0f);
            v3 = 31;
            if ((flags & 0x7) == 0)
            {
                v1 = v1 + 31 + 8;
                v2 = v2 + 31 + 8;
            }
            else
            {
                v1 = v1 * 2 + 31;
                v2 = v2 * 2 + 31;
            }

            /* flags a */
            if ((flags & 0x7) == 6)
                ptr[destIndex + 0] = 0;
            else
            {
                ptr[destIndex + 0] = (byte)((flags >> 4) & 0xb);
                ptr[destIndex + 1] = map_param[flags & 0x7];
            }

            /* extra a */
            ptr[destIndex + 2] = 0;
            ptr[destIndex + 3] = 0;
            ptr[destIndex + 4] = (byte)(t1 >> 4);
            ptr[destIndex + 5] = (byte)(t1 & 0xf);
            ptr[destIndex + 6] = (byte)(v1 >> 4);
            ptr[destIndex + 7] = (byte)(v1 & 0xf);
            ptr[destIndex + 8] = (byte)(t2 >> 4);
            ptr[destIndex + 9] = (byte)(t2 & 0xf);
            ptr[destIndex + 10] = (byte)(v2 >> 4);
            ptr[destIndex + 11] = (byte)(v2 & 0xf);
            ptr[destIndex + 12] = (byte)(t3 >> 4);
            ptr[destIndex + 13] = (byte)(t3 & 0xf);
            ptr[destIndex + 14] = (byte)(t4 >> 4);
            ptr[destIndex + 15] = (byte)(t4 & 0xf);
            ptr[destIndex + 16] = (byte)(v3 >> 4);
            ptr[destIndex + 17] = (byte)(v3 & 0xf);

            time = num_steps_table[t1] + num_steps_table[t2]
            + num_steps_table[t3 & 0x7f] + num_steps_table[t4];
            if ((flags & 0x20) != 0)
            {
                int playtime = ((srcPtr[inPos + 4] >> 4) & 0xf) * 118 +
                               (srcPtr[inPos + 4] & 0xf) * 8;
                if (playtime > time)
                    time = playtime;
            }
            /*
    time = ((src_ptr[4] >> 4) & 0xf) * 118 +
        (src_ptr[4] & 0xf) * 8;
    */
            return time;
        }

        byte[] ConvertADResource(byte[] input, int idx)
        {
            // We will ignore the PPQN in the original resource, because
            // it's invalid anyway. We use a constant PPQN of 480.
            const int ppqn = 480;
            int dw;
            int total_size = MIDIHeaderSize + 7 + 8 * ADLIB_INSTR_MIDI_HACK.Length + input.Length;
            total_size += 24;   // Up to 24 additional bytes are needed for the jump sysex

            var ptr = new byte[total_size];

            var size = input.Length - 2;
            int inputPos = 2;
            int outPos = 0;

            // 0x80 marks a music resource. Otherwise it's a SFX
            if (input[inputPos] == 0x80)
            {
                WriteMIDIHeader(ptr, "ADL ", ppqn, total_size);
                outPos += MIDIHeaderSize;

                // The "speed" of the song
                var ticks = input[inputPos + 1];

                // Flag that tells us whether we should loop the song (0) or play it only once (1)
                var play_once = input[inputPos + 2];

                // Number of instruments used
                var num_instr = input[inputPos + 8]; // Normally 8

                // copy the pointer to instrument data
                var channelPos = inputPos + 9;
                var instrPos = inputPos + 0x11;

                // skip over the rest of the header and copy the MIDI data into a buffer
                inputPos += 0x11 + 8 * 16;
                size -= 0x11 + 8 * 16;

                var trackPos = inputPos;

                // Convert the ticks into a MIDI tempo.
                // Unfortunate LOOM and INDY3 have different interpretation
                // of the ticks value.
                if (Game.GameId == GameId.Indy3)
                {
                    // Note: since we fix ppqn at 480, ppqn/473 is almost 1
                    dw = 500000 * 256 / 473 * ppqn / ticks;
                }
                else if (Game.GameId == GameId.Loom && Game.Version == 3)
                {
                    dw = 500000 * ppqn / 4 / ticks;
                }
                else
                {
                    dw = 500000 * 256 / ticks;
                }
                Debug.WriteLine("  ticks = {0}, speed = {1}", ticks, dw);

                // Write a tempo change Meta event
                Array.Copy(new byte[] { 0x00, 0xFF, 0x51, 0x03 }, 0, ptr, outPos, 4);
                outPos += 4;
                ptr[outPos++] = (byte)((dw >> 16) & 0xFF);
                ptr[outPos++] = (byte)((dw >> 8) & 0xFF);
                ptr[outPos++] = (byte)(dw & 0xFF);

                // Copy our hardcoded instrument table into it
                // Then, convert the instrument table as given in this song resource
                // And write it *over* the hardcoded table.
                // Note: we deliberately.

                /* now fill in the instruments */
                for (var i = 0; i < num_instr; i++)
                {
                    var ch = input[channelPos + i] - 1;
                    if (ch < 0 || ch > 15)
                        continue;

                    if (input[instrPos + i * 16 + 13] != 0)
                        Debug.WriteLine("Sound {0} instrument {1} uses percussion", idx, i);

                    Debug.WriteLine("Sound {0}: instrument {1} on channel {2}.", idx, i, ch);

                    Array.Copy(ADLIB_INSTR_MIDI_HACK, 0, ptr, outPos, ADLIB_INSTR_MIDI_HACK.Length);

                    ptr[outPos + 5] += (byte)ch;
                    ptr[outPos + 28] += (byte)ch;
                    ptr[outPos + 92] += (byte)ch;

                    /* mod_characteristics */
                    ptr[outPos + 30 + 0] = (byte)((input[instrPos + i * 16 + 3] >> 4) & 0xf);
                    ptr[outPos + 30 + 1] = (byte)(input[instrPos + i * 16 + 3] & 0xf);

                    /* mod_scalingOutputLevel */
                    ptr[outPos + 30 + 2] = (byte)((input[instrPos + i * 16 + 4] >> 4) & 0xf);
                    ptr[outPos + 30 + 3] = (byte)(input[instrPos + i * 16 + 4] & 0xf);

                    /* mod_attackDecay */
                    ptr[outPos + 30 + 4] = (byte)(((~input[instrPos + i * 16 + 5]) >> 4) & 0xf);
                    ptr[outPos + 30 + 5] = (byte)((~input[instrPos + i * 16 + 5]) & 0xf);

                    /* mod_sustainRelease */
                    ptr[outPos + 30 + 6] = (byte)(((~input[instrPos + i * 16 + 6]) >> 4) & 0xf);
                    ptr[outPos + 30 + 7] = (byte)((~input[instrPos + i * 16 + 6]) & 0xf);

                    /* mod_waveformSelect */
                    ptr[outPos + 30 + 8] = (byte)((input[instrPos + i * 16 + 7] >> 4) & 0xf);
                    ptr[outPos + 30 + 9] = (byte)(input[instrPos + i * 16 + 7] & 0xf);

                    /* car_characteristic */
                    ptr[outPos + 30 + 10] = (byte)((input[instrPos + i * 16 + 8] >> 4) & 0xf);
                    ptr[outPos + 30 + 11] = (byte)(input[instrPos + i * 16 + 8] & 0xf);

                    /* car_scalingOutputLevel */
                    ptr[outPos + 30 + 12] = (byte)((input[instrPos + i * 16 + 9] >> 4) & 0xf);
                    ptr[outPos + 30 + 13] = (byte)(input[instrPos + i * 16 + 9] & 0xf);

                    /* car_attackDecay */
                    ptr[outPos + 30 + 14] = (byte)(((~input[instrPos + i * 16 + 10]) >> 4) & 0xf);
                    ptr[outPos + 30 + 15] = (byte)((~input[instrPos + i * 16 + 10]) & 0xf);

                    /* car_sustainRelease */
                    ptr[outPos + 30 + 16] = (byte)(((~input[instrPos + i * 16 + 11]) >> 4) & 0xf);
                    ptr[outPos + 30 + 17] = (byte)((~input[instrPos + i * 16 + 11]) & 0xf);

                    /* car_waveFormSelect */
                    ptr[outPos + 30 + 18] = (byte)((input[instrPos + i * 16 + 12] >> 4) & 0xf);
                    ptr[outPos + 30 + 19] = (byte)(input[instrPos + i * 16 + 12] & 0xf);

                    /* feedback */
                    ptr[outPos + 30 + 20] = (byte)((input[instrPos + i * 16 + 2] >> 4) & 0xf);
                    ptr[outPos + 30 + 21] = (byte)(input[instrPos + i * 16 + 2] & 0xf);

                    outPos += ADLIB_INSTR_MIDI_HACK.Length;
                }

                // There is a constant delay of ppqn/3 before the music starts.
                if ((ppqn / 3) >= 128)
                    ptr[outPos++] = ((ppqn / 3) >> 7) | 0x80;
                ptr[outPos++] = ppqn / 3 & 0x7f;

                // Now copy the actual music data
                Array.Copy(input, trackPos, ptr, outPos, size);
                outPos += size;

                if (play_once == 0)
                {
                    // The song is meant to be looped. We achieve this by inserting just
                    // before the song end a jump to the song start. More precisely we abuse
                    // a S&M sysex, "maybe_jump" to achieve this effect. We could also
                    // use a set_loop sysex, but it's a bit longer, a little more complicated,
                    // and has no advantage either.

                    // First, find the track end
                    var endPos = outPos;
                    outPos -= size;
                    for (; outPos < endPos; outPos++)
                    {
                        if (ptr[outPos] == 0xff && ptr[outPos + 1] == 0x2f)
                            break;
                    }
                    Debug.Assert(outPos < endPos);

                    // Now insert the jump. The jump offset is measured in ticks.
                    // We have ppqn/3 ticks before the first note.

                    const int jump_offset = ppqn / 3;
                    // maybe_jump
                    Array.Copy(new byte[] { 0xf0, 0x13, 0x7d, 0x30, 0x00 }, 0, ptr, outPos, 5);
                    outPos += 5;
                    // cmd -> 0 means always jump
                    Array.Copy(new byte[] { 0x00, 0x00 }, 0, ptr, outPos, 2);
                    outPos += 2;
                    // track -> there is only one track, 0
                    Array.Copy(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0, ptr, outPos, 4);
                    outPos += 4;
                    // beat -> for now, 1 (first beat)
                    Array.Copy(new byte[] { 0x00, 0x00, 0x00, 0x01 }, 0, ptr, outPos, 4);
                    outPos += 4;

                    // Ticks
                    ptr[outPos++] = (jump_offset >> 12) & 0x0F;
                    ptr[outPos++] = (jump_offset >> 8) & 0x0F;
                    ptr[outPos++] = (jump_offset >> 4) & 0x0F;
                    ptr[outPos++] = jump_offset & 0x0F;

                    // sysex end marker
                    Array.Copy(new byte[] { 0x00, 0xf7 }, 0, ptr, outPos, 2);
                }
            }
            else
            {
                // This is a sfx resource.  First parse it quickly to find the parallel
                // tracks.
                WriteMIDIHeader(ptr, "ASFX", ppqn, total_size);
                outPos += MIDIHeaderSize;

                var current_instr = new byte[3][];
                var current_note = new int[3];
                var track_time = new int[3];
                var track_data = new int[3];

                int track_ctr = 0;
                byte chunk_type = 0;
                int delay, delay2, olddelay;

                // Write a tempo change Meta event
                // 473 / 4 Hz, convert to micro seconds.
                dw = 1000000 * ppqn * 4 / 473;
                Array.Copy(new byte[] { 0x00, 0xFF, 0x51, 0x03 }, 0, ptr, outPos, 4);
                outPos += 4;
                ptr[outPos++] = (byte)((dw >> 16) & 0xFF);
                ptr[outPos++] = (byte)((dw >> 8) & 0xFF);
                ptr[outPos++] = (byte)(dw & 0xFF);

                for (var i = 0; i < 3; i++)
                {
                    track_time[i] = -1;
                    current_note[i] = -1;
                }
                while (size > 0)
                {
                    Debug.Assert(track_ctr < 3);
                    track_data[track_ctr] = inputPos;
                    track_time[track_ctr] = 0;
                    track_ctr++;
                    while (size > 0)
                    {
                        chunk_type = input[inputPos];
                        if (chunk_type == 1)
                        {
                            inputPos += 15;
                            size -= 15;
                        }
                        else if (chunk_type == 2)
                        {
                            inputPos += 11;
                            size -= 11;
                        }
                        else if (chunk_type == 0x80)
                        {
                            inputPos++;
                            size--;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (chunk_type == 0xff)
                        break;
                    inputPos++;
                }

                int curtime = 0;
                for (;;)
                {
                    int mintime = -1;
                    var ch = -1;
                    for (var i = 0; i < 3; i++)
                    {
                        if (track_time[i] >= 0 &&
                            (mintime == -1 || mintime > track_time[i]))
                        {
                            mintime = track_time[i];
                            ch = i;
                        }
                    }
                    if (mintime < 0)
                        break;

                    inputPos = track_data[ch];
                    chunk_type = input[inputPos];

                    if (current_note[ch] >= 0)
                    {
                        delay = mintime - curtime;
                        curtime = mintime;
                        outPos = WriteVLQ(ptr, outPos, delay);
                        ptr[outPos++] = (byte)(0x80 + ch);// key off channel;
                        ptr[outPos++] = (byte)current_note[ch];
                        ptr[outPos++] = 0;
                        current_note[ch] = -1;
                    }

                    switch (chunk_type)
                    {
                        case 1:
                            /* Instrument definition */
                            current_instr[ch] = new byte[14];
                            Array.Copy(input, inputPos + 1, current_instr[ch], 0, 14);
                            inputPos += 15;
                            break;

                        case 2:
                            /* tone/parammodulation */
                            Array.Copy(ADLIB_INSTR_MIDI_HACK, 0, ptr, outPos, ADLIB_INSTR_MIDI_HACK.Length);

                            ptr[outPos + 5] += (byte)ch;
                            ptr[outPos + 28] += (byte)ch;
                            ptr[outPos + 92] += (byte)ch;

                            /* mod_characteristic */
                            ptr[outPos + 30 + 0] = (byte)((current_instr[ch][3] >> 4) & 0xf);
                            ptr[outPos + 30 + 1] = (byte)(current_instr[ch][3] & 0xf);

                            /* mod_scalingOutputLevel */
                            ptr[outPos + 30 + 2] = (byte)((current_instr[ch][4] >> 4) & 0xf);
                            ptr[outPos + 30 + 3] = (byte)(current_instr[ch][4] & 0xf);

                            /* mod_attackDecay */
                            ptr[outPos + 30 + 4] = (byte)(((~current_instr[ch][5]) >> 4) & 0xf);
                            ptr[outPos + 30 + 5] = (byte)((~current_instr[ch][5]) & 0xf);

                            /* mod_sustainRelease */
                            ptr[outPos + 30 + 6] = (byte)(((~current_instr[ch][6]) >> 4) & 0xf);
                            ptr[outPos + 30 + 7] = (byte)((~current_instr[ch][6]) & 0xf);

                            /* mod_waveformSelect */
                            ptr[outPos + 30 + 8] = (byte)((current_instr[ch][7] >> 4) & 0xf);
                            ptr[outPos + 30 + 9] = (byte)(current_instr[ch][7] & 0xf);

                            /* car_characteristic */
                            ptr[outPos + 30 + 10] = (byte)((current_instr[ch][8] >> 4) & 0xf);
                            ptr[outPos + 30 + 11] = (byte)(current_instr[ch][8] & 0xf);

                            /* car_scalingOutputLevel */
                            ptr[outPos + 30 + 12] = (byte)(((current_instr[ch][9]) >> 4) & 0xf);
                            ptr[outPos + 30 + 13] = (byte)((current_instr[ch][9]) & 0xf);

                            /* car_attackDecay */
                            ptr[outPos + 30 + 14] = (byte)(((~current_instr[ch][10]) >> 4) & 0xf);
                            ptr[outPos + 30 + 15] = (byte)((~current_instr[ch][10]) & 0xf);

                            /* car_sustainRelease */
                            ptr[outPos + 30 + 16] = (byte)(((~current_instr[ch][11]) >> 4) & 0xf);
                            ptr[outPos + 30 + 17] = (byte)((~current_instr[ch][11]) & 0xf);

                            /* car_waveFormSelect */
                            ptr[outPos + 30 + 18] = (byte)((current_instr[ch][12] >> 4) & 0xf);
                            ptr[outPos + 30 + 19] = (byte)(current_instr[ch][12] & 0xf);

                            /* feedback */
                            ptr[outPos + 30 + 20] = (byte)((current_instr[ch][2] >> 4) & 0xf);
                            ptr[outPos + 30 + 21] = (byte)(current_instr[ch][2] & 0xf);

                            delay = mintime - curtime;
                            curtime = mintime;

                            {
                                delay = ConvertExtraflags(ptr, outPos + 30 + 22, input, inputPos + 1);
                                delay2 = ConvertExtraflags(ptr, outPos + 30 + 40, input, inputPos + 6);
                                Debug.WriteLine("delays: {0} / {1}", delay, delay2);
                                if (delay2 >= 0 && delay2 < delay)
                                    delay = delay2;
                                if (delay == -1)
                                    delay = 0;
                            }

                            /* duration */
                            ptr[outPos + 30 + 58] = 0; // ((delay * 17 / 63) >> 4) & 0xf;
                            ptr[outPos + 30 + 59] = 0; // (delay * 17 / 63) & 0xf;

                            outPos += ADLIB_INSTR_MIDI_HACK.Length;

                            olddelay = mintime - curtime;
                            curtime = mintime;
                            outPos = WriteVLQ(ptr, outPos, olddelay);

                            {
                                int freq = ((current_instr[ch][1] & 3) << 8)
                                           | current_instr[ch][0];
                                if (freq == 0)
                                    freq = 0x80;
                                freq <<= (((current_instr[ch][1] >> 2) + 1) & 7);
                                int note = -11;
                                while (freq >= 0x100)
                                {
                                    note += 12;
                                    freq >>= 1;
                                }
                                Debug.WriteLine("Freq: {0} (0x{0:X}) Note: {1}", freq, note);
                                if (freq < 0x80)
                                    note = 0;
                                else
                                    note += freq2note[freq - 0x80];

                                Debug.WriteLine("Note: {0}", note);
                                if (note <= 0)
                                    note = 1;
                                else if (note > 127)
                                    note = 127;

                                // Insert a note on event
                                ptr[outPos++] = (byte)(0x90 + ch); // key on channel
                                ptr[outPos++] = (byte)note;
                                ptr[outPos++] = 63;
                                current_note[ch] = note;
                                track_time[ch] = curtime + delay;
                            }
                            inputPos += 11;
                            break;

                        case 0x80:
                            // FIXME: This is incorrect. The original uses 0x80 for
                            // looping a single channel. We currently interpret it as stop
                            // thus we won't get looping for sound effects. It should
                            // always jump to the start of the channel.
                            //
                            // Since we convert the data to MIDI and we cannot only loop a
                            // single channel via MIDI fixing this will require some more
                            // thought.
                            track_time[ch] = -1;
                            inputPos++;
                            break;

                        default:
                            track_time[ch] = -1;
                            break;
                    }
                    track_data[ch] = inputPos;
                }
            }

            // Insert end of song sysex
            Array.Copy(new byte[] { 0x00, 0xff, 0x2f, 0x00, 0x00 }, 0, ptr, outPos, 5);

            return ptr;
        }

        byte _expireCounter;
        int _allocatedSize;
        int _maxHeapThreshold, _minHeapThreshold;

        Dictionary<int, byte[]> _scripts = new Dictionary<int, byte[]>();
        Dictionary<int, byte[]> _costumes = new Dictionary<int, byte[]>();
        Dictionary<int, Room> _rooms = new Dictionary<int, Room>();
        Dictionary<int, byte[]> _charsets = new Dictionary<int, byte[]>();
        Dictionary<int, byte[]> _sounds = new Dictionary<int, byte[]>();

        HashSet<int> _roomsLock = new HashSet<int>();
        HashSet<int> _costumesLock = new HashSet<int>();
        HashSet<int> _scriptsLock = new HashSet<int>();
        HashSet<int> _soundsLock = new HashSet<int>();

        Dictionary<int, int> _roomsCounter = new Dictionary<int, int>();
        Dictionary<int, int> _costumesCounter = new Dictionary<int, int>();
        Dictionary<int, int> _scriptsCounter = new Dictionary<int, int>();
        Dictionary<int, int> _soundsCounter = new Dictionary<int, int>();
    }
}
