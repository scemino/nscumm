//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
using System.Text;

namespace NScumm.Sci.Engine
{
    // Savegame metadata
    internal class SavegameMetadata
    {
        public string name;
        public int version;
        public string gameVersion;
        public int saveDate;
        public int saveTime;
        public int playTime;
        public ushort gameObjectOffset;
        public ushort script0Size;
    }

    /*
     * Savegame format history:
     *
     * Version - new/changed feature
     * =============================
     *      33 - new overridePriority flag in MusicEntry
     *      32 - new playBed flag in MusicEntry
     *      31 - priority for sound effects/music is now a signed int16, instead of a byte
     *      30 - synonyms
     *      29 - system strings
     *      28 - heap
     *      27 - script created windows
     *      26 - play time
     *      25 - palette intensity
     *      24 - palvary
     *      23 - script buffer and heap size
     *      22 - game signature
     *      21 - script local variables
     *      20 - exports/synonyms
     *      19 - exportsAreWide
     *      18 - SCI32 arrays/strings
     *      17 - sound
     *
     */

    internal static class Savegame
    {
        public const int CURRENT_SAVEGAME_VERSION = 33;
        public const int MINIMUM_SAVEGAME_VERSION = 14;

        public static bool get_savegame_metadata(Stream stream, out SavegameMetadata meta)
        {
            //assert(stream);
            //assert(meta);

            meta = new SavegameMetadata();
            Serializer ser = new Serializer(stream, null);
            sync_SavegameMetadata(ser, ref meta);

            if ((meta.version < MINIMUM_SAVEGAME_VERSION) ||
                (meta.version > CURRENT_SAVEGAME_VERSION))
            {
                if (meta.version < MINIMUM_SAVEGAME_VERSION)
                {
                    // TODO: warning("Old savegame version detected- can't load");
                }
                else {
                    // TODO: warning("Savegame version is %d- maximum supported is %0d", meta.version, CURRENT_SAVEGAME_VERSION);
                }
                return false;
            }

            return true;
        }

        private static void sync_SavegameMetadata(Serializer s, ref SavegameMetadata obj)
        {
            var tmp = new StringBuilder();
            s.SyncString(tmp);
            obj.name = tmp.ToString();
            s.SyncVersion(CURRENT_SAVEGAME_VERSION);
            obj.version = s.Version;
            s.SyncString(tmp);
            obj.gameVersion = tmp.ToString();
            s.SyncAsInt32LE(ref obj.saveDate);
            s.SyncAsInt32LE(ref obj.saveTime);
            if (s.Version < 22)
            {
                obj.gameObjectOffset = 0;
                obj.script0Size = 0;
            }
            else {
                s.SyncAsUint16LE(ref obj.gameObjectOffset);
                s.SyncAsUint16LE(ref obj.script0Size);
            }

            // Playtime
            obj.playTime = 0;
            if (s.IsLoading)
            {
                if (s.Version >= 26)
                    s.SyncAsInt32LE(ref obj.playTime);
            }
            else {
                obj.playTime = SciEngine.Instance.TotalPlaytime / 1000;
                s.SyncAsInt32LE(ref obj.playTime);
            }
        }

        internal static bool gamestate_save(EngineState s, Stream @out, string game_description, string version)
        {
            throw new NotImplementedException();
        }

        internal static void gamestate_restore(EngineState s, Stream @in)
        {
            throw new NotImplementedException();
        }
    }

    public class Serializer
    {
        private const int LastVersion = int.MaxValue;

        protected BinaryReader _loadStream;
        protected BinaryWriter _saveStream;
        protected int _version;
        protected int _bytesSynced;

        public bool IsSaving { get { return (_saveStream != null); } }
        public bool IsLoading { get { return (_loadStream != null); } }

        /// <summary>
        /// Return the version of the savestate being serialized. Useful if the engine
        /// needs to perform additional adjustments when loading old savestates.
        /// </summary>
        public int Version { get { return _version; } }

        public Serializer(Stream @in, Stream @out)
        {
            _loadStream = @in != null ? new BinaryReader(@in) : null;
            _saveStream = @out != null ? new BinaryWriter(@out) : null;
        }

        /// <summary>
        /// Sync a C-string, by treating it as a zero-terminated byte sequence.
        /// @todo Replace this method with a special Syncer class for Common::String
        /// </summary>
        /// <param name="str"></param>
        /// <param name="minVersion"></param>
        /// <param name="maxVersion"></param>
        public void SyncString(StringBuilder str, int minVersion = 0, int maxVersion = LastVersion)
        {
            if (_version < minVersion || _version > maxVersion)
                return; // Ignore anything which is not supposed to be present in this save game version

            if (IsLoading)
            {
                char c;
                str.Clear();
                while ((c = _loadStream.ReadChar()) != 0)
                {
                    str.Append(c);
                    _bytesSynced++;
                }
                _bytesSynced++;
            }
            else {
                _saveStream.Write(str.ToString());
                _saveStream.Write((byte)0);
                _bytesSynced += str.Length + 1;
            }
        }

        /// <summary>
        /// Sync the "version" of the savegame we are loading/creating.
        /// </summary>
        /// <param name="currentVersion">current format version, used when writing a new file</param>
        /// <returns>true if the version of the savestate is not too new.</returns>
        public bool SyncVersion(int currentVersion)
        {
            _version = currentVersion;
            SyncAsInt32LE(ref _version);
            return _version <= currentVersion;
        }

        public void SyncAsUint32LE(ref uint val, int minVersion = 0, int maxVersion = LastVersion)
        {
            if (_version < minVersion || _version > maxVersion)
                return;
            if (IsLoading)
                val = _loadStream.ReadUInt32();
            else
            {
                _saveStream.Write(val);
            }
            _bytesSynced += 4;
        }

        public void SyncAsInt32LE(ref int val, int minVersion = 0, int maxVersion = LastVersion)
        {
            if (_version < minVersion || _version > maxVersion)
                return;
            if (IsLoading)
                val = _loadStream.ReadInt32();
            else
            {
                _saveStream.Write(val);
            }
            _bytesSynced += 4;
        }

        public void SyncAsUint16LE(ref ushort val, int minVersion = 0, int maxVersion = LastVersion)
        {
            if (_version < minVersion || _version > maxVersion)
                return;
            if (IsLoading)
                val = _loadStream.ReadUInt16();
            else
            {
                _saveStream.Write(val);
            }
            _bytesSynced += 4;
        }
    }
}
