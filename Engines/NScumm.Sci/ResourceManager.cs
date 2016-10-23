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

using NScumm.Core;
using System.Collections.Generic;
using System;
using System.IO;
using NScumm.Core.IO;
using System.Linq;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci
{
    internal struct ResourceIndex
    {
        public ushort wOffset;
        public ushort wSize;
    }

    internal enum ResourceCompression
    {
        Unknown = -1,
        None = 0,
        LZW,
        Huffman,
        LZW1, // LZW-like compression used in SCI01 and SCI1
        LZW1View, // Comp3 + view Post-processing
        LZW1Pic, // Comp3 + pic Post-processing
#if ENABLE_SCI32
        STACpack, // ? Used in SCI32
#endif
        DCL
    }

    /// <summary>
    /// SCI versions
    /// For more information, check here:
    /// http://wiki.scummvm.org/index.php/Sierra_Game_Versions#SCI_Games
    /// </summary>
    internal enum SciVersion
    {
        NONE,
        V0_EARLY, // KQ4 early, LSL2 early, XMAS card 1988
        V0_LATE, // KQ4, LSL2, LSL3, SQ3 etc
        V01, // KQ1 and multilingual games (S.old.*)
        V1_EGA_ONLY, // SCI 1 EGA with parser (i.e. QFG2 only)
        V1_EARLY, // KQ5 floppy, SQ4 floppy, XMAS card 1990, Fairy tales, Jones floppy
        V1_MIDDLE, // LSL1, Jones CD
        V1_LATE, // Dr. Brain 1, EcoQuest 1, Longbow, PQ3, SQ1, LSL5, KQ5 CD
        V1_1, // Dr. Brain 2, EcoQuest 1 CD, EcoQuest 2, KQ6, QFG3, SQ4CD, XMAS 1992 and many more
        V2, // GK1, PQ4 floppy, QFG4 floppy
        V2_1_EARLY, // GK2 demo, KQ7 1.4/1.51, LSL6 hires, PQ4CD, QFG4 floppy
        V2_1_MIDDLE, // GK2, KQ7 2.00b, MUMG Deluxe, Phantasmagoria 1, PQ:SWAT, QFG4CD, Shivers 1, SQ6, Torin
        V2_1_LATE, // demos of LSL7, Lighthouse, RAMA
        V3 // LSL7, Lighthouse, RAMA, Phantasmagoria 2
    }

    internal enum ResVersion
    {
        Unknown,
        Sci0Sci1Early,
        Sci1Middle,
        KQ5FMT,
        Sci1Late,
        Sci11,
        Sci11Mac,
        Sci2,
        Sci3
    }

    // Game view types, sorted by the number of colors
    internal enum ViewType
    {
        Unknown, // uninitialized, or non-SCI
        Ega, // EGA SCI0/SCI1 and Amiga SCI0/SCI1 ECS 16 colors
        Amiga, // Amiga SCI1 ECS 32 colors
        Amiga64, // Amiga SCI1 AGA 64 colors (i.e. Longbow)
        Vga, // VGA SCI1 256 colors
        Vga11 // VGA SCI1.1 and newer 256 colors
    }

    internal enum ResourceType
    {
        View = 0,
        Pic,
        Script,
        Text,
        Sound,
        Memory,
        Vocab,
        Font,
        Cursor,
        Patch,
        Bitmap,
        Palette,
        CdAudio = 12,
#if ENABLE_SCI32
        Wave = 12,
#endif
        Audio,
        Sync,
        Message,
        Map,
        Heap,
        Audio36,
        Sync36,
        Translation, // Currently unsupported

        // SCI2.1+ Resources
        Robot,
        VMD,
        Chunk,
        Animation,

        // SCI3 Resources
        Etc,
        Duck,
        Clut,
        TGA,
        ZZZ,

        // Mac-only resources
        MacIconBarPictN, // IBIN resources (icon bar, not selected)
        MacIconBarPictS, // IBIS resources (icon bar, selected)
        MacPict, // PICT resources (inventory)

        Rave, // KQ6 hires RAVE (special sync) resources

        Invalid
    }

    /** Resource error codes. Should be in sync with s_errorDescriptions */

    internal enum ResourceErrorCodes
    {
        NONE = 0,
        IO_ERROR = 1,
        EMPTY_RESOURCE = 2,
        RESMAP_INVALID_ENTRY = 3, /**< Invalid resource.map entry */
        RESMAP_NOT_FOUND = 4,
        NO_RESOURCE_FILES_FOUND = 5, /**< No resource at all was found */
        UNKNOWN_COMPRESSION = 6,
        DECOMPRESSION_ERROR = 7, /**< sanity checks failed during decompression */
        RESOURCE_TOO_BIG = 8 /**< Resource size exceeds SCI_MAX_RESOURCE_SIZE */
    }

    internal partial class ResourceManager
    {
        /// <summary>
        /// Max number of simultaneously opened volumes.
        /// </summary>
        private const int MAX_OPENED_VOLUMES = 5;

        private const int SCI0_RESMAP_ENTRIES_SIZE = 6;
        private const int SCI1_RESMAP_ENTRIES_SIZE = 6;
        private const int KQ5FMT_RESMAP_ENTRIES_SIZE = 7;
        private const int SCI11_RESMAP_ENTRIES_SIZE = 5;
        private readonly string _directory;
        private readonly List<ResourceSource> _sources;

        /// <summary>
        /// Amount of resource bytes in locked memory
        /// </summary>
        protected int _memoryLocked;

        /// <summary>
        /// Amount of resource bytes under LRU control
        /// </summary>
        protected int _memoryLRU;

        /// <summary>
        /// Last Resource Used list
        /// </summary>
        protected List<ResourceSource.Resource> _LRU;

        protected ResourceMap _resMap;

        /// <summary>
        /// Currently loaded audio map for SCI1
        /// </summary>
        protected ResourceSource _audioMapSCI1;

        /// <summary>
        /// resource.0xx version
        /// </summary>
        protected ResVersion _volVersion;

        /// <summary>
        /// resource.map version
        /// </summary>
        protected ResVersion _mapVersion;

        /// <summary>
        /// Used to determine if the game has EGA or VGA graphics
        /// </summary>
        private ViewType _viewType;

        private static SciVersion s_sciVersion = SciVersion.NONE;
        // FIXME: Move this inside a suitable class, e.g. SciEngine

        /// <summary>
        /// List of opened volume files
        /// </summary>
        private readonly List<Stream> _volumeFiles;

#if ENABLE_SCI32
        /// <summary>
        /// If true, the game has multiple audio volumes that contain different
        /// audio files for each disc.
        /// </summary>
        private bool _multiDiscAudio;
#endif

        // Resource type suffixes. Note that the
        // suffix of SCI3 scripts has been changed from
        // scr to csc
        private static readonly string[] s_resourceTypeSuffixes =
        {
            "v56", "p56", "scr", "tex", "snd",
            "", "voc", "fon", "cur", "pat",
            "bit", "pal", "cda", "aud", "syn",
            "msg", "map", "hep", "", "",
            "trn", "rbt", "vmd", "chk", "",
            "etc", "duk", "clu", "tga", "zzz",
            "", "", "", ""
        };

        private static readonly string[] s_resourceTypeNames =
        {
            "view", "pic", "script", "text", "sound",
            "memory", "vocab", "font", "cursor",
            "patch", "bitmap", "palette", "cdaudio",
            "audio", "sync", "message", "map", "heap",
            "audio36", "sync36", "xlate", "robot", "vmd",
            "chunk", "animation", "etc", "duck", "clut",
            "tga", "zzz", "macibin", "macibis", "macpict",
            "rave"
        };

        private static readonly ResourceType[] s_resTypeMapSci0 =
        {
            ResourceType.View, ResourceType.Pic, ResourceType.Script, ResourceType.Text, // 0x00-0x03
            ResourceType.Sound, ResourceType.Memory, ResourceType.Vocab, ResourceType.Font, // 0x04-0x07
            ResourceType.Cursor, ResourceType.Patch, ResourceType.Bitmap, ResourceType.Palette, // 0x08-0x0B
            ResourceType.CdAudio, ResourceType.Audio, ResourceType.Sync, ResourceType.Message, // 0x0C-0x0F
            ResourceType.Map, ResourceType.Heap, ResourceType.Audio36, ResourceType.Sync36, // 0x10-0x13
            ResourceType.Translation, ResourceType.Rave // 0x14
        };

        // TODO: 12 should be "Wave", but SCI seems to just store it in Audio resources
        private static readonly ResourceType[] s_resTypeMapSci21 =
        {
            ResourceType.View, ResourceType.Pic, ResourceType.Script, ResourceType.Animation, // 0x00-0x03
            ResourceType.Sound, ResourceType.Etc, ResourceType.Vocab, ResourceType.Font, // 0x04-0x07
            ResourceType.Cursor, ResourceType.Patch, ResourceType.Bitmap, ResourceType.Palette, // 0x08-0x0B
            ResourceType.Invalid, ResourceType.Audio, ResourceType.Sync, ResourceType.Message, // 0x0C-0x0F
            ResourceType.Map, ResourceType.Heap, ResourceType.Chunk, ResourceType.Audio36, // 0x10-0x13
            ResourceType.Sync36, ResourceType.Translation, ResourceType.Robot, ResourceType.VMD, // 0x14-0x17
            ResourceType.Duck, ResourceType.Clut, ResourceType.TGA, ResourceType.ZZZ // 0x18-0x1B
        };

        // to detect new kString calling to detect SCI2.1 Late
        private static byte[] detectSci21NewStringSignature =
        {
            8, // size of signature
            0x78, // push1
            0x78, // push1
            0x39, 0x09, // pushi 09
            0x59, 0x01, // rest 01
            0x43, 0x5c, // callk String
        };

        // to detect selector "wordFail" in LE vocab resource
        private static byte[] detectSci21EarlySignature =
        {
            10, // size of signature
            0x08, 0x00, (byte) 'w', (byte) 'o', (byte) 'r', (byte) 'd', (byte) 'F', (byte) 'a', (byte) 'i', (byte) 'l'
        };

        // to detect selector "wordFail" in BE vocab resource (SCI2.1 Early)
        private static byte[] detectSci21EarlyBESignature =
        {
            10, // size of signature
            0x00, 0x08, (byte) 'w', (byte) 'o', (byte) 'r', (byte) 'd', (byte) 'F', (byte) 'a', (byte) 'i', (byte) 'l'
        };

        // Maximum number of bytes to allow being allocated for resources
        // Note: maxMemory will not be interpreted as a hard limit, only as a restriction
        // for resources which are not explicitly locked. However, a warning will be
        // issued whenever this limit is exceeded.
        protected int _maxMemoryLRU;
        private short _currentDiscNo;

        public bool IsSci11Mac => _volVersion == ResVersion.Sci11Mac;

        public ViewType ViewType => _viewType;

        public string Directory => _directory;

        public bool IsGMTrackIncluded()
        {
            // This check only makes sense for SCI1 and newer games
            if (GetSciVersion() < SciVersion.V1_EARLY)
                return false;

            // SCI2 and newer games always have GM tracks
            if (GetSciVersion() >= SciVersion.V2)
                return true;

            // For the leftover games, we can safely use SCI_VERSION_1_EARLY for the soundVersion
            var soundVersion = SciVersion.V1_EARLY;

            // Read the first song and check if it has a GM track
            var result = false;
            var resources = ListResources(ResourceType.Sound);
            resources.Sort();
            var itr = resources.First();
            int firstSongId = itr.Number;

            var song1 = new SoundResource((uint) firstSongId, this, soundVersion);

            var gmTrack = song1.GetTrackByType(0x07);
            if (gmTrack != null)
                result = true;

            return result;
        }

        public ResourceManager(string directory)
        {
            _directory = directory;
            _sources = new List<ResourceSource>();
            _LRU = new List<ResourceSource.Resource>();
            _resMap = new ResourceMap();
            _volumeFiles = new List<Stream>();
        }

        public static string GetSciVersionDesc(SciVersion version)
        {
            switch (version)
            {
                case SciVersion.NONE:
                    return "Invalid SCI version";
                case SciVersion.V0_EARLY:
                    return "Early SCI0";
                case SciVersion.V0_LATE:
                    return "Late SCI0";
                case SciVersion.V01:
                    return "SCI01";
                case SciVersion.V1_EGA_ONLY:
                    return "SCI1 EGA";
                case SciVersion.V1_EARLY:
                    return "Early SCI1";
                case SciVersion.V1_MIDDLE:
                    return "Middle SCI1";
                case SciVersion.V1_LATE:
                    return "Late SCI1";
                case SciVersion.V1_1:
                    return "SCI1.1";
                case SciVersion.V2:
                    return "SCI2";
                case SciVersion.V2_1_EARLY:
                    return "Early SCI2.1";
                case SciVersion.V2_1_MIDDLE:
                    return "Middle SCI2.1";
                case SciVersion.V2_1_LATE:
                    return "Late SCI2.1";
                case SciVersion.V3:
                    return "SCI3";
                default:
                    return "Unknown";
            }
        }


        /// <summary>
        /// Detects, if SCI1.1 game uses palette merging or copying - this is supposed to only get used on SCI1.1 games
        /// </summary>
        /// <returns></returns>
        public bool DetectPaletteMergingSci11()
        {
            // Load palette 999 (default palette)
            var res = FindResource(new ResourceId(ResourceType.Palette, 999), false);

            if ((res != null) && (res.size > 30))
            {
                var data = res.data;
                // Old palette format used in palette resource? . it's merging
                if ((data[0] == 0 && data[1] == 1) || (data[0] == 0 && data[1] == 0 && data.ToUInt16(29) == 0))
                    return true;
                // Hardcoded: Laura Bow 2 floppy uses new palette resource, but still palette merging + 16 bit color matching
                if ((SciEngine.Instance.GameId == SciGameId.LAURABOW2) && !SciEngine.Instance.IsCd &&
                    !SciEngine.Instance.IsDemo)
                    return true;
                return false;
            }
            return false;
        }

        public int AddAppropriateSources()
        {
#if ENABLE_SCI32
            _multiDiscAudio = false;
#endif

            var path = ServiceLocator.FileStorage.Combine(_directory, "resource.map");
            if (ServiceLocator.FileStorage.FileExists(path))
            {
                // SCI0-SCI2 file naming scheme
                var map = AddExternalMap(path);

                var files = ServiceLocator.FileStorage.EnumerateFiles(_directory, "resource.0??",
                    SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var name = ServiceLocator.FileStorage.GetFileName(file);
                    var number = int.Parse(name.Split('.')[1]);
                    AddSource(new VolumeResourceSource(name, map, number));
                }
#if ENABLE_SCI32
                // GK1CD hires content
                if (Core.Engine.OpenFileRead("alt.map") != null && Core.Engine.OpenFileRead("resource.alt") != null)
                    AddSource(new VolumeResourceSource("resource.alt", AddExternalMap("alt.map", 10), 10));
#endif
            }
            else if (MacResManager.Exists("Data1"))
            {
                // Mac SCI1.1+ file naming scheme
                var files = MacResManager.ListFiles("Data?");

                foreach (var x in files)
                {
                    AddSource(new MacResourceForkResourceSource(x, int.Parse(x.Substring(4))));
                }

#if ENABLE_SCI32
                // There can also be a "Patches" resource fork with patches
                if (MacResManager.Exists("Patches"))
                    AddSource(new MacResourceForkResourceSource("Patches", 100));
            }
            else
            {
                // SCI2.1-SCI3 file naming scheme
                var mapFiles = ServiceLocator.FileStorage.EnumerateFiles(_directory, "resmap.0??").ToList();
                var files = ServiceLocator.FileStorage.EnumerateFiles(_directory, "ressci.0??").ToList();

                // We need to have the same number of maps as resource archives
                if (mapFiles.Count == 0 || files.Count == 0 || mapFiles.Count != files.Count)
                    return 0;

                if (ScummHelper.LocatePath(_directory, "resaud.001") != null)
                {
                    _multiDiscAudio = true;
                }

                foreach (var mapName in mapFiles)
                {
                    var mapNumber = int.Parse(ServiceLocator.FileStorage.GetExtension(mapName).Remove(0, 1));

                    foreach (var resName in files)
                    {
                        var resNumber = int.Parse(ServiceLocator.FileStorage.GetExtension(resName).Remove(0, 1));

                        if (mapNumber == resNumber)
                        {
                            AddSource(new VolumeResourceSource(resName, AddExternalMap(mapName, mapNumber), mapNumber));
                            break;
                        }
                    }
                }

                // SCI2.1 resource patches
                if (Core.Engine.OpenFileRead("resmap.pat") != null && Core.Engine.OpenFileRead("ressci.pat") != null)
                {
                    // We add this resource with a map which surely won't exist
                    AddSource(new VolumeResourceSource("ressci.pat", AddExternalMap("resmap.pat", 100), 100));
                }
            }
#else
            }
            else
                return 0;
#endif

            AddPatchDir(".");

            path = ScummHelper.LocatePath(_directory, "message.map");
            if (path != null)
                AddSource(new VolumeResourceSource("resource.msg", AddExternalMap(path), 0));

            path = ScummHelper.LocatePath(_directory, "altres.map");
            if (path != null)
                AddSource(new VolumeResourceSource("altres.000", AddExternalMap(path), 0));

            return 1;
        }

        public int AddAppropriateSourcesForDetection(IList<string> fslist)
        {
            ResourceSource map = null;
            var sci21Maps = new Dictionary<int, ResourceSource>();

#if ENABLE_SCI32
            ResourceSource sci21PatchMap = null;
            string sci21PatchRes = null;
            _multiDiscAudio = false;
#endif

            // First, find resource.map
            foreach (var file in fslist)
            {
                if (!ServiceLocator.FileStorage.FileExists(file))
                    continue;

                string filename = ServiceLocator.FileStorage.GetFileName(file);
                filename = filename.ToLowerInvariant();

                if (filename == "resource.map")
                    map = AddExternalMap(file);

                if (filename.StartsWith("resmap.0"))
                {
                    var ext = ServiceLocator.FileStorage.GetExtension(filename).Remove(0, 1);
                    int number = int.Parse(ext);

                    // We need to store each of these maps for use later on
                    sci21Maps[number] = AddExternalMap(file, number);
                }

#if ENABLE_SCI32
                // SCI2.1 resource patches
                if (filename == "resmap.pat")
                    sci21PatchMap = AddExternalMap(file, 100);

                if (filename == "ressci.pat")
                    sci21PatchRes = file;
#endif
            }

            if (map == null && sci21Maps.Count == 0)
                return 0;

#if ENABLE_SCI32
            if (sci21PatchMap != null && sci21PatchRes != null)
                AddSource(new VolumeResourceSource(ServiceLocator.FileStorage.GetFileName(sci21PatchRes),
                    sci21PatchMap, 100, sci21PatchRes));
#endif

            // Now find all the resource.0?? files
            foreach (var file in fslist)
            {
                if (!ServiceLocator.FileStorage.FileExists(file))
                    continue;

                string filename = ServiceLocator.FileStorage.GetFileName(file);
                filename = filename.ToLowerInvariant();

                if (filename.StartsWith("resource.0"))
                {
                    var ext = ServiceLocator.FileStorage.GetExtension(filename).Remove(0, 1);
                    int number = int.Parse(ext);

                    AddSource(new VolumeResourceSource(ServiceLocator.FileStorage.GetFileName(file), map, number, file));
                }
                else if (filename.StartsWith("ressci.0"))
                {
                    var ext = ServiceLocator.FileStorage.GetExtension(filename).Remove(0, 1);
                    int number = int.Parse(ext);

                    // Match this volume to its own map
                    AddSource(new VolumeResourceSource(ServiceLocator.FileStorage.GetFileName(file), sci21Maps[number],
                        number, file));
                }
            }

            // This function is only called by the advanced detector, and we don't really need
            // to add a patch directory or message.map here

            return 1;
        }

        public void Init()
        {
            _maxMemoryLRU = 256 * 1024; // 256KiB
            _memoryLocked = 0;
            _memoryLRU = 0;
            _LRU.Clear();
            _resMap.Clear();
            _audioMapSCI1 = null;
#if ENABLE_SCI32
            _currentDiscNo = 1;
#endif

            // FIXME: put this in an Init() function, so that we can error out if detection fails completely

            _mapVersion = DetectMapVersion();
            _volVersion = DetectVolVersion();

            // TODO/FIXME: Remove once SCI3 resource detection is finished
            if ((_mapVersion == ResVersion.Sci3 || _volVersion == ResVersion.Sci3) && (_mapVersion != _volVersion))
            {
                Warning("FIXME: Incomplete SCI3 detection: setting map and volume version to SCI3");
                _mapVersion = _volVersion = ResVersion.Sci3;
            }

            if ((_volVersion == ResVersion.Unknown) && (_mapVersion != ResVersion.Unknown))
            {
                Warning("Volume version not detected, but map version has been detected. Setting volume version to map version");
                _volVersion = _mapVersion;
            }

            if ((_mapVersion == ResVersion.Unknown) && (_volVersion != ResVersion.Unknown))
            {
                Warning("Map version not detected, but volume version has been detected. Setting map version to volume version");
                _mapVersion = _volVersion;
            }

            DebugC(1, DebugLevels.ResMan, "resMan: Detected resource map version {0}: {1}", _mapVersion,
                GetVersionDescription(_mapVersion));
            DebugC(1, DebugLevels.ResMan, "resMan: Detected volume version {0}: {1}", _volVersion,
                GetVersionDescription(_volVersion));

            if ((_mapVersion == ResVersion.Unknown) && (_volVersion == ResVersion.Unknown))
            {
                Warning("Volume and map version not detected, assuming that this is not a SCI game");
                _viewType = ViewType.Unknown;
                return;
            }

            ScanNewSources();

            if (!AddAudioSources())
            {
                // FIXME: This error message is not always correct.
                // OTOH, it is nice to be able to detect missing files/sources
                // So we should definitely fix addAudioSources so this error
                // only pops up when necessary. Disabling for now.
                //error("Somehow I can't seem to find the sound files I need (RESOURCE.AUD/RESOURCE.SFX), aborting");
            }

            AddScriptChunkSources();
            ScanNewSources();

            DetectSciVersion();

            DebugC(1, DebugLevels.ResMan, "resMan: Detected {0}", GetSciVersionDesc(GetSciVersion()));

            // Resources in SCI32 games are significantly larger than SCI16
            // games and can cause immediate exhaustion of the LRU resource
            // cache, leading to constant decompression of picture resources
            // and making the renderer very slow.
            if (GetSciVersion() >= SciVersion.V2)
            {
                _maxMemoryLRU = 2048 * 1024; // 2MiB
            }

            switch (_viewType)
            {
                case ViewType.Ega:
                    DebugC(1, DebugLevels.ResMan, "resMan: Detected EGA graphic resources");
                    break;
                case ViewType.Amiga:
                    DebugC(1, DebugLevels.ResMan, "resMan: Detected Amiga ECS graphic resources");
                    break;
                case ViewType.Amiga64:
                    DebugC(1, DebugLevels.ResMan, "resMan: Detected Amiga AGA graphic resources");
                    break;
                case ViewType.Vga:
                    DebugC(1, DebugLevels.ResMan, "resMan: Detected VGA graphic resources");
                    break;
                case ViewType.Vga11:
                    DebugC(1, DebugLevels.ResMan, "resMan: Detected SCI1.1 VGA graphic resources");
                    break;
                default:
                    // Throw a warning, but do not error out here, because this is called from the
                    // fallback detector, and the user could be pointing to a folder with a non-SCI
                    // game, but with SCI-like file names (e.g. Pinball Creep)
                    Warning("resMan: Couldn't determine view type");
                    break;
            }
        }

        private string GetVersionDescription(ResVersion version)
        {
            switch (version)
            {
                case ResVersion.Unknown:
                    return "Unknown";
                case ResVersion.Sci0Sci1Early:
                    return "SCI0 / Early SCI1";
                case ResVersion.Sci1Middle:
                    return "Middle SCI1";
                case ResVersion.KQ5FMT:
                    return "KQ5 FM Towns";
                case ResVersion.Sci1Late:
                    return "Late SCI1";
                case ResVersion.Sci11:
                    return "SCI1.1";
                case ResVersion.Sci11Mac:
                    return "Mac SCI1.1+";
                case ResVersion.Sci2:
                    return "SCI2/2.1";
                case ResVersion.Sci3:
                    return "SCI3";
            }

            return "Version not valid";
        }

        public int GetAudioLanguage()
        {
            return _audioMapSCI1?._volumeNumber ?? 0;
        }

        /// <summary>
        /// Adds the appropriate GM patch from the Sierra MIDI utility as 4.pat, without
        /// requiring the user to rename the file to 4.pat. Thus, the original Sierra
        /// archive can be extracted in the extras directory, and the GM patches can be
        /// applied per game, if applicable.
        /// </summary>
        /// <param name="gameId"></param>
        public void AddNewGMPatch(SciGameId gameId)
        {
            string gmPatchFile = null;

            switch (gameId)
            {
                case SciGameId.ECOQUEST:
                    gmPatchFile = "ECO1GM.PAT";
                    break;
                case SciGameId.HOYLE3:
                    gmPatchFile = "HOY3GM.PAT";
                    break;
                case SciGameId.LSL1:
                    gmPatchFile = "LL1_GM.PAT";
                    break;
                case SciGameId.LSL5:
                    gmPatchFile = "LL5_GM.PAT";
                    break;
                case SciGameId.LONGBOW:
                    gmPatchFile = "ROBNGM.PAT";
                    break;
                case SciGameId.SQ1:
                    gmPatchFile = "SQ1_GM.PAT";
                    break;
                case SciGameId.SQ4:
                    gmPatchFile = "SQ4_GM.PAT";
                    break;
                case SciGameId.FAIRYTALES:
                    gmPatchFile = "TALEGM.PAT";
                    break;
            }

            if (gmPatchFile != null && ScummHelper.LocatePath(_directory, gmPatchFile) != null)
            {
                ResourceSource psrcPatch = new PatchResourceSource(gmPatchFile);
                ProcessPatch(psrcPatch, ResourceType.Patch, 4);
            }
        }

        /// <summary>
        /// Finds the location of the game object from script 0.
        /// </summary>
        /// <param name="addSci11ScriptOffset">
        /// Adjust the return value for SCI1.1 and newer
        /// games.Needs to be false when the heap is accessed directly inside
        /// findSierraGameId().
        /// </param>
        /// <returns></returns>
        public Register FindGameObject(bool addSci11ScriptOffset = true)
        {
            var script = FindResource(new ResourceId(ResourceType.Script, 0), false);

            if (script == null)
                return Register.NULL_REG;

            int offsetPtr;

            if (GetSciVersion() <= SciVersion.V1_LATE)
            {
                var buf = script.data;
                var bufOffset = GetSciVersion() == SciVersion.V0_EARLY ? 2 : 0;

                // Check if the first block is the exports block (in most cases, it is)
                var exportsIsFirst = (buf.ToUInt16(bufOffset + 4) == 7);
                if (exportsIsFirst)
                {
                    offsetPtr = bufOffset + 4 + 2;
                }
                else
                {
                    offsetPtr = FindSci0ExportsBlock(script.data);
                    if (offsetPtr == -1)
                    {
                        throw new InvalidOperationException("Unable to find exports block from script 0");
                    }
                    offsetPtr += 4 + 2;
                }

                var offset = !IsSci11Mac ? buf.ToUInt16(offsetPtr) : buf.ToUInt16BigEndian(offsetPtr);
                return Register.Make(1, offset);
            }
            if (GetSciVersion() >= SciVersion.V1_1 && GetSciVersion() <= SciVersion.V2_1_LATE)
            {
                var buf = script.data;
                offsetPtr = 4 + 2 + 2;

                // In SCI1.1 - SCI2.1, the heap is appended at the end of the script,
                // so adjust the offset accordingly if requested
                var offset = !IsSci11Mac ? buf.ToUInt16(offsetPtr) : buf.ToUInt16BigEndian(offsetPtr);
                if (addSci11ScriptOffset)
                {
                    offset = (ushort) (offset + script.size);

                    // Ensure that the start of the heap is word-aligned - same as in Script::init()
                    if ((script.size & 2) != 0)
                        offset++;
                }

                return Register.Make(1, offset);
            }
            return Register.Make(1, (ushort) RelocateOffsetSci3(script.data, 22));
        }

        public ResourceSource.Resource FindResource(ResourceId id, bool locked)
        {
            var retval = TestResource(id);

            if (retval == null)
                return null;

            if (retval._status == ResourceStatus.NoMalloc)
            {
                LoadResource(retval);
            }
            else if (retval._status == ResourceStatus.Enqueued)
            {
                // The resource is removed from its current position
                // in the LRU list because it has been requested
                // again. Below, it will either be locked, or it
                // will be added back to the LRU list at the 'most
                // recent' position.
                RemoveFromLRU(retval);
            }
            // Unless an error occurred, the resource is now either
            // locked or allocated, but never queued or freed.

            FreeOldResources();

            if (locked)
            {
                if (retval._status == ResourceStatus.Allocated)
                {
                    retval._status = ResourceStatus.Locked;
                    retval._lockers = 0;
                    _memoryLocked += retval.size;
                }
                retval._lockers++;
            }
            else if (retval._status != ResourceStatus.Locked)
            {
                // Don't lock it
                if (retval._status == ResourceStatus.Allocated)
                    AddToLRU(retval);
            }

            if (retval.data != null)
                return retval;

            Warning($"resMan: Failed to read {retval._id}");
            return null;
        }

        /// <summary>
        /// Unlocks a previously locked resource.
        /// </summary>
        /// <param name="res">The resource to free</param>
        public void UnlockResource(ResourceSource.Resource res)
        {
            if (res._status != ResourceStatus.Locked)
            {
                DebugC(DebugLevels.ResMan, 2, "[resMan] Attempt to unlock unlocked resource {0}", res._id);
                return;
            }

            if (--res._lockers == 0)
            {
                // No more lockers?
                res._status = ResourceStatus.Allocated;
                _memoryLocked -= res.size;
                AddToLRU(res);
            }

            FreeOldResources();
        }

        public bool DetectHires()
        {
            // SCI 1.1 and prior is never hires
            if (GetSciVersion() <= SciVersion.V1_1)
                return false;

# if ENABLE_SCI32
            for (var i = 0; i < 32768; i++)
            {
                var res = FindResource(new ResourceId(ResourceType.Pic, (ushort) i), false);

                if (res?.data.ReadSci11EndianUInt16() == 0x0e)
                {
                    // SCI32 picture
                    var width = res.data.ReadSci11EndianUInt16(10);
                    var height = res.data.ReadSci11EndianUInt16(12);
                    // Surely lowres (e.g. QFG4CD)
                    if ((width == 320) && ((height == 190) || (height == 200)))
                        return false;
                    // Surely hires
                    if ((width >= 600) || (height >= 400))
                        return true;
                }
            }

            // We haven't been able to find hires content

            return false;
#else
            Error("no sci32 support");
            return false;
#endif
        }

        public bool DetectFontExtended()
        {
            var res = FindResource(new ResourceId(ResourceType.Font, 0), false);
            if (!(res?.size >= 4)) return false;

            var numChars = res.data.ToUInt16(2);
            return numChars > 0x80;
        }

        /// <summary>
        /// Same function as Script::findBlockSCI0(). Slight code
        /// duplication here, but this has been done to keep the resource
        /// manager independent from the rest of the engine
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static int FindSci0ExportsBlock(byte[] buffer)
        {
            var buf = 0;
            var oldScriptHeader = (GetSciVersion() == SciVersion.V0_EARLY);

            if (oldScriptHeader)
                buf += 2;

            do
            {
                int seekerType = buffer.ToUInt16(buf);

                if (seekerType == 0)
                    break;
                if (seekerType == 7) // exports
                    return buf;

                int seekerSize = buffer.ToUInt16(buf + 2);
                buf += seekerSize;
            } while (true);

            return -1;
        }

        // This code duplicates Script::relocateOffsetSci3, but we can't use
        // that here since we can't instantiate scripts at this point.
        private static int RelocateOffsetSci3(byte[] buf, int offset)
        {
            var relocStart = buf.ToInt32(8);
            int relocCount = buf.ToUInt16(18);
            var seeker = relocStart;

            for (var i = 0; i < relocCount; ++i)
            {
                if (buf.ReadSci11EndianUInt32(seeker) == offset)
                {
                    // TODO: Find out what UINT16 at (seeker + 8) means
                    return (int) (buf.ReadSci11EndianUInt16(offset) + buf.ReadSci11EndianUInt32(seeker + 4));
                }
                seeker += 10;
            }

            return -1;
        }

        private void ReadResourcePatches()
        {
            // Note: since some SCI1 games(KQ5 floppy, SQ4) might use SCI0 naming scheme for patch files
            // this function tries to read patch file with any supported naming scheme,
            // regardless of s_sciVersion value

            var files = new List<string>();
            ushort resourceNr = 0;

            for (var i = (int) ResourceType.View; i < (int) ResourceType.Invalid; ++i)
            {
                // Ignore the types that can't be patched (and Robot/VMD is handled externally for now)
                if (s_resourceTypeSuffixes[i] != null ||
                    (i >= (int) ResourceType.Robot && i != (int) ResourceType.Chunk))
                    continue;

                files.Clear();
                var szResType = GetResourceTypeName((ResourceType) i);
                // SCI0 naming - type.nnn
                var mask = szResType;
                mask += ".???";
                files.AddRange(ServiceLocator.FileStorage.EnumerateFiles(mask));
                // SCI1 and later naming - nnn.typ
                mask = "*.";
                mask += s_resourceTypeSuffixes[i];
                files.AddRange(ServiceLocator.FileStorage.EnumerateFiles(mask));

                if (i == (int) ResourceType.View)
                {
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.v16")); // EGA SCI1 view patches
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.v32")); // Amiga SCI1 view patches
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.v64"));
                    // Amiga AGA SCI1 (i.e. Longbow) view patches
                }
                else if (i == (int) ResourceType.Pic)
                {
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.p16")); // EGA SCI1 picture patches
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.p32")); // Amiga SCI1 picture patches
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.p64"));
                    // Amiga AGA SCI1 (i.e. Longbow) picture patches
                }
                else if (i == (int) ResourceType.Script)
                {
                    if (files.Count == 0)
                        // SCI3 (we can't use getSciVersion() at this point)
                        files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.csc"));
                }

                foreach (var x in files)
                {
                    var bAdd = false;
                    var name = ServiceLocator.FileStorage.GetFileName(x);

                    // SCI1 scheme
                    if (name.Length > 0 && char.IsDigit(name[0]))
                    {
                        var end = name.IndexOf('.');
                        if (end != -1)
                        {
                            // Ensure the next character is the perio
                            resourceNr = ushort.Parse(name.Substring(0, end));
                            bAdd = true;
                        }
                    }
                    else
                    {
                        // SCI0 scheme
                        var resnameLen = szResType.Length;
                        if (string.Compare(name, 0, szResType, 0, resnameLen, StringComparison.OrdinalIgnoreCase) == 0
                            && !char.IsLetter(name[resnameLen + 1]))
                        {
                            resourceNr = ushort.Parse(name.Substring(resnameLen + 1));
                            bAdd = true;
                        }
                    }

                    if (bAdd)
                    {
                        var psrcPatch = new PatchResourceSource(name);
                        ProcessPatch(psrcPatch, (ResourceType) i, resourceNr);
                    }
                }
            }
        }

        private static string GetResourceTypeName(ResourceType restype)
        {
            if (restype != ResourceType.Invalid)
                return s_resourceTypeNames[(int) restype];
            return "invalid";
        }

        private void ReadWaveAudioPatches()
        {
            // Here we do check for SCI1.1+ so we can patch wav files in as audio resources
            var files = ServiceLocator.FileStorage.EnumerateFiles(_directory, "*.wav").ToList();

            foreach (var x in files)
            {
                var name = ServiceLocator.FileStorage.GetFileName(x);

                if (char.IsDigit(name[0]))
                    ProcessWavePatch(new ResourceId(ResourceType.Audio, ushort.Parse(name)), name);
            }
        }

        private void ProcessWavePatch(ResourceId resourceId, string name)
        {
            ResourceSource resSrc = new WaveResourceSource(name);
            using (var file = Core.Engine.OpenFileRead(name))
            {
                UpdateResource(resourceId, resSrc, (int) file.Length);
            }
            _sources.Add(resSrc);

            DebugC(1, DebugLevels.ResMan, "Patching {0} - OK", name);
        }

        private void ReadResourcePatchesBase36()
        {
            // The base36 encoded audio36 and sync36 resources use a different naming scheme, because they
            // cannot be described with a single resource number, but are a result of a
            // <number, noun, verb, cond, seq> tuple. Please don't be confused with the normal audio patches
            // (*.aud) and normal sync patches (*.syn). audio36 patches can be seen for example in the AUD
            // folder of GK1CD, and are like this file: @0CS0M00.0X1. GK1CD is the first game where these
            // have been observed. The actual audio36 and sync36 resources exist in SCI1.1 as well, but the
            // first game where external patch files for them have been found is GK1CD. The names of these
            // files are base36 encoded, and we handle their decoding here. audio36 files start with a '@',
            // whereas sync36 start with a '#'. Mac versions begin with 'A' (probably meaning AIFF). Torin
            // has several that begin with 'B'.

            var files = new List<string>();

            for (var i = ResourceType.Audio36; i <= ResourceType.Sync36; ++i)
            {
                files.Clear();

                // audio36 resources start with a @, A, or B
                // sync36 resources start with a #, S, or T
                if (i == ResourceType.Audio36)
                {
                    files.AddRange(Core.Engine.EnumerateFiles("@???????.???"));
                    files.AddRange(Core.Engine.EnumerateFiles("A???????.???"));
                    files.AddRange(Core.Engine.EnumerateFiles("B???????.???"));
                }
                else
                {
                    files.AddRange(Core.Engine.EnumerateFiles("#???????.???"));
#if ENABLE_SCI32
                    files.AddRange(Core.Engine.EnumerateFiles("S???????.???"));
                    files.AddRange(Core.Engine.EnumerateFiles("T???????.???"));
#endif
                }

                foreach (var name in files)
                {
                    // The S/T prefixes often conflict with non-patch files and generate
                    // spurious warnings about invalid patches
                    var ext = ServiceLocator.FileStorage.GetExtension(name);
                    if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".DLL") ||
                        StringComparer.OrdinalIgnoreCase.Equals(ext, ".EXE") ||
                        StringComparer.OrdinalIgnoreCase.Equals(ext, ".TXT"))
                    {
                        continue;
                    }

                    var resource36 = ConvertPatchNameBase36(i, name);

                    /*
                    if (i == kResourceTypeAudio36)
                        debug("audio36 patch: %s => %s. tuple:%d, %s\n", name.c_str(), inputName.c_str(), resource36.tuple, resource36.toString().c_str());
                    else
                        debug("sync36 patch: %s => %s. tuple:%d, %s\n", name.c_str(), inputName.c_str(), resource36.tuple, resource36.toString().c_str());
                    */

                    // Make sure that the audio patch is a valid resource
                    if (i == ResourceType.Audio36)
                    {
                        var stream = ServiceLocator.FileStorage.OpenFileRead(name);
                        var br = new BinaryReader(stream);
                        var tag = br.ReadUInt32();

                        if (tag == ScummHelper.MakeTag('R', 'I', 'F', 'F') ||
                            tag == ScummHelper.MakeTag('F', 'O', 'R', 'M'))
                        {
                            stream.Dispose();
                            ProcessWavePatch(resource36, name);
                            continue;
                        }

                        // Check for SOL as well
                        tag = (tag << 16) | br.ReadUInt16BigEndian();

                        if (tag != ScummHelper.MakeTag('S', 'O', 'L', '\0'))
                        {
                            stream.Dispose();
                            continue;
                        }

                        stream.Dispose();
                    }

                    ResourceSource psrcPatch = new PatchResourceSource(name);
                    ProcessPatch(psrcPatch, i, resource36.Number, resource36.Tuple);
                }
            }
        }

        private static ResourceId ConvertPatchNameBase36(ResourceType type, string filename)
        {
            // The base36 encoded resource contains the following:
            // uint16 resourceId, byte noun, byte verb, byte cond, byte seq

            // Skip patch type character
            try
            {
                var resourceNr = Convert.ToInt32(filename.Substring(1, 3), 36); // 3 characters
                var noun = Convert.ToInt32(filename.Substring(4, 2), 36); // 2 characters
                var verb = Convert.ToInt32(filename.Substring(6, 2), 36); // 2 characters
                // Skip '.'
                var cond = Convert.ToInt32(filename.Substring(9, 2), 36); // 2 characters
                var seq = Convert.ToInt32(filename.Substring(11, 1), 36); // 1 character

                return new ResourceId(type, (ushort) resourceNr, (byte) noun, (byte) verb, (byte) cond, (byte) seq);
            }
            catch (ArgumentException)
            {
                return new ResourceId(type, 0, 0, 0, 0, 0);
            }
        }

        // version-agnostic patch application
        private void ProcessPatch(ResourceSource source, ResourceType resourceType, ushort resourceNr, uint tuple = 0)
        {
            Stream fileStream;
            var resId = new ResourceId(resourceType, resourceNr, tuple);
            var checkForType = resourceType;

            // base36 encoded patches (i.e. audio36 and sync36) have the same type as their non-base36 encoded counterparts
            if (checkForType == ResourceType.Audio36)
                checkForType = ResourceType.Audio;
            else if (checkForType == ResourceType.Sync36)
                checkForType = ResourceType.Sync;

            if (source._resourceFile != null)
            {
                throw new NotImplementedException();
                // TODO: fileStream = source._resourceFile.CreateReadStream();
            }
            else
            {
                if (!ServiceLocator.FileStorage.FileExists(source.LocationName))
                {
                    Warning($"ResourceManager::processPatch(): failed to open {source.LocationName}");
                    //source.Dispose();
                    return;
                }
                fileStream = ServiceLocator.FileStorage.OpenFileRead(source.LocationName);
            }

            var br = new BinaryReader(fileStream);
            var fsize = fileStream.Length;
            if (fsize < 3)
            {
                Debug($"Patching {source.LocationName} failed - file too small");
                //delete source;
                return;
            }

            var patchType = ConvertResType(br.ReadByte());
            var patchDataOffset = br.ReadByte();

            fileStream.Dispose();

            if (patchType != checkForType)
            {
                Debug("Patching {0} failed - resource type mismatch", source.LocationName);
                //delete source;
                return;
            }

            // Fixes SQ5/German, patch file special case logic taken from SCI View disassembly
            if ((patchDataOffset & 0x80) != 0)
            {
                switch (patchDataOffset & 0x7F)
                {
                    case 0:
                        patchDataOffset = 24;
                        break;
                    case 1:
                        patchDataOffset = 2;
                        break;
                    case 4:
                        patchDataOffset = 8;
                        break;
                    default:
                        Error("Resource patch unsupported special case {0:X}", patchDataOffset & 0x7F);
                        return;
                }
            }

            if (patchDataOffset + 2 >= fsize)
            {
                Debug("Patching {0} failed - patch starting at offset {1} can't be in file of size {2}",
                    source.LocationName, patchDataOffset + 2, fsize);
                //delete source;
                return;
            }

            // Overwrite everything, because we're patching
            var newrsc = UpdateResource(resId, source, (int) (fsize - patchDataOffset - 2));
            newrsc._headerSize = patchDataOffset;
            newrsc._fileOffset = 0;


            DebugC(1, DebugLevels.ResMan, "Patching {0} - OK", source.LocationName);
        }

        public ResourceSource.Resource UpdateResource(ResourceId resId, ResourceSource src, int size)
        {
            // Update a patched resource, whether it exists or not
            ResourceSource.Resource res;

            if (_resMap.ContainsKey(resId))
            {
                _resMap.TryGetValue(resId, out res);
            }
            else
            {
                res = new ResourceSource.Resource(this, resId);
                _resMap[resId] = res;
            }

            res._status = ResourceStatus.NoMalloc;
            res._source = src;
            res._headerSize = 0;
            res.size = size;

            return res;
        }

        private ResourceErrorCodes ReadAudioMapSCI11(IntMapResourceSource map)
        {
#if !ENABLE_SCI32
// SCI32 support is not built in. Check if this is a SCI32 game
// and if it is abort here.
            if (_volVersion >= ResVersion.Sci2)
                return ResourceErrorCodes.RESMAP_NOT_FOUND;
#endif

            uint offset = 0;
            var mapRes = FindResource(new ResourceId(ResourceType.Map, map._mapNumber), false);

            if (mapRes == null)
            {
                Warning($"Failed to open {map._mapNumber}.MAP");
                return ResourceErrorCodes.RESMAP_NOT_FOUND;
            }

            var src = FindVolume(map, map._volumeNumber);

            if (src == null)
            {
                Warning("Failed to find volume for {0}.MAP", map._mapNumber);
                return ResourceErrorCodes.NO_RESOURCE_FILES_FOUND;
            }

            var ptr = new ByteAccess(mapRes.data);

            // Heuristic to detect entry size
            uint entrySize = 0;
            for (var i = mapRes.size - 1; i >= 0; --i)
            {
                if (ptr[i] == 0xff)
                    entrySize++;
                else
                    break;
            }

            if (map._mapNumber == 65535)
            {
                var ba = new ByteAccess(mapRes.data, mapRes.size);
                while (ptr.Offset < ba.Offset)
                {
                    var n = ptr.ToUInt16();
                    ptr.Offset += 2;

                    if (n == 0xffff)
                        break;

                    if (entrySize == 6)
                    {
                        offset = ptr.ToUInt32();
                        ptr.Offset += 4;
                    }
                    else
                    {
                        offset += ptr.ToUInt24();
                        ptr.Offset += 3;
                    }

                    AddResource(new ResourceId(ResourceType.Audio, n), src, offset);
                }
            }
            else if (map._mapNumber == 0 && entrySize == 10 && ptr[3] == 0)
            {
                // QFG3 demo format
                // ptr[3] would be 'seq' in the normal format and cannot possibly be 0
                var ba = new ByteAccess(mapRes.data, mapRes.size);
                while (ptr.Offset < ba.Offset)
                {
                    var n = ptr.ToUInt16BigEndian();
                    ptr.Offset += 2;

                    if (n == 0xffff)
                        break;

                    offset = ptr.ToUInt32();
                    ptr.Offset += 4;
                    var size = ptr.ToUInt32();
                    ptr.Offset += 4;

                    AddResource(new ResourceId(ResourceType.Audio, n), src, offset, (int) size);
                }
            }
            else if (map._mapNumber == 0 && entrySize == 8 && ptr.ToUInt16(2) == 0xffff)
            {
                // LB2 Floppy/Mother Goose SCI1.1 format
                var stream = GetVolumeFile(src);
                var br = new BinaryReader(stream);
                var ba = new ByteAccess(mapRes.data, mapRes.size);
                while (ptr.Offset < ba.Offset)
                {
                    var n = ptr.ToUInt16();
                    ptr.Offset += 4;

                    if (n == 0xffff)
                        break;

                    offset = ptr.ToUInt32();
                    ptr.Offset += 4;

                    // The size is not stored in the map and the entries have no order.
                    // We need to dig into the audio resource in the volume to get the size.
                    stream.Seek(offset + 1, SeekOrigin.Begin);
                    var headerSize = br.ReadByte();
                    System.Diagnostics.Debug.Assert(headerSize == 11 || headerSize == 12);

                    stream.Seek(5, SeekOrigin.Begin);
                    var size = br.ReadUInt32() + headerSize + 2;

                    AddResource(new ResourceId(ResourceType.Audio, n), src, offset, (int) size);
                }
            }
            else
            {
                var isEarly = (entrySize != 11);

                if (!isEarly)
                {
                    offset = ptr.ToUInt32();
                    ptr.Offset += 4;
                }

                var ba = new ByteAccess(mapRes.data, mapRes.size);
                while (ptr.Offset < ba.Offset)
                {
                    var n = ptr.ToUInt32();
                    var syncSize = 0;
                    ptr.Offset += 4;

                    if (n == 0xffffffff)
                        break;

                    if (isEarly)
                    {
                        offset = ptr.ToUInt32();
                        ptr.Offset += 4;
                    }
                    else
                    {
                        offset += ptr.ToUInt24();
                        ptr.Offset += 3;
                    }

                    if (isEarly || ((n & 0x80) != 0))
                    {
                        syncSize = ptr.ToUInt16();
                        ptr.Offset += 2;

                        // FIXME: The sync36 resource seems to be two bytes too big in KQ6CD
                        // (bytes taken from the RAVE resource right after it)
                        if (syncSize > 0)
                            AddResource(
                                new ResourceId(ResourceType.Sync36, map._mapNumber, n & 0xffffff3f), src,
                                offset, syncSize);
                    }

                    if ((n & 0x40) != 0)
                    {
                        // This seems to define the size of raw lipsync data (at least
                        // in KQ6 CD Windows).
                        int kq6HiresSyncSize = ptr.ToUInt16();
                        ptr.Offset += 2;

                        if (kq6HiresSyncSize > 0)
                        {
                            AddResource(new ResourceId(ResourceType.Rave, map._mapNumber, n & 0xffffff3f),
                                src, (uint) (offset + syncSize), kq6HiresSyncSize);
                            syncSize += kq6HiresSyncSize;
                        }
                    }

                    AddResource(new ResourceId(ResourceType.Audio36, map._mapNumber, n & 0xffffff3f), src,
                        (uint) (offset + syncSize));
                }
            }

            return 0;
        }

        private ResourceErrorCodes ReadResourceMapSCI1(ResourceSource map)
        {
            var path = map._resourceFile ?? map.LocationName;
            var fileStream = ServiceLocator.FileStorage.OpenFileRead(path);
            if (fileStream == null)
                return ResourceErrorCodes.RESMAP_NOT_FOUND;

            Debug("ReadResourceMapSCI1 {0}", path);

            using (fileStream)
            using (var br = new BinaryReader(fileStream))
            {
                var resMap = new ResourceIndex[32];
                byte type, prevtype = 0;
                var nEntrySize = _mapVersion == ResVersion.Sci11 ? SCI11_RESMAP_ENTRIES_SIZE : SCI1_RESMAP_ENTRIES_SIZE;

                // Read resource type and offsets to resource offsets block from .MAP file
                // The last entry has type=0xFF (0x1F) and offset equals to map file length
                do
                {
                    type = (byte) (br.ReadByte() & 0x1F);
                    resMap[type].wOffset = br.ReadUInt16();
                    Debug("type {0}, off: {1}", type, resMap[type].wOffset);
                    if (br.BaseStream.Position == br.BaseStream.Length)
                        return ResourceErrorCodes.RESMAP_NOT_FOUND;

                    resMap[prevtype].wSize = (ushort) ((resMap[type].wOffset - resMap[prevtype].wOffset) / nEntrySize);
                    Debug("type {0}, size: {1}", type, resMap[prevtype].wSize);
                    prevtype = type;
                } while (type != 0x1F); // the last entry is FF

                // reading each type's offsets
                for (type = 0; type < 32; type++)
                {
                    if (resMap[type].wOffset == 0) // this resource does not exist in map
                        continue;
                    br.BaseStream.Seek(resMap[type].wOffset, SeekOrigin.Begin);
                    for (var i = 0; i < resMap[type].wSize; i++)
                    {
                        var number = br.ReadUInt16();
                        var volumeNr = 0;
                        uint fileOffset;

                        if (_mapVersion == ResVersion.Sci11)
                        {
                            // offset stored in 3 bytes
                            fileOffset = br.ReadUInt16();
                            fileOffset = (uint) (fileOffset | br.ReadByte() << 16);
                            fileOffset <<= 1;
                        }
                        else
                        {
                            // offset/volume stored in 4 bytes
                            fileOffset = br.ReadUInt32();
                            if (_mapVersion < ResVersion.Sci11)
                            {
                                volumeNr = (int) (fileOffset >> 28); // most significant 4 bits
                                fileOffset &= 0x0FFFFFFF; // least significant 28 bits
                            }
                            // in SCI32 it's a plain offset
                        }

                        var resId = new ResourceId(ConvertResType(type), number);
                        // NOTE: We add the map's volume number here to the specified volume number
                        // for SCI2.1 and SCI3 maps that are not resmap.000. The resmap.* files' numbers
                        // need to be used in concurrence with the volume specified in the map to get
                        // the actual resource file.
                        var mapVolumeNr = volumeNr + map._volumeNumber;
                        var source = FindVolume(map, mapVolumeNr);

                        System.Diagnostics.Debug.Assert(source != null);

                        ResourceSource.Resource resource;
                        if (!_resMap.TryGetValue(resId, out resource))
                        {
                            AddResource(resId, source, fileOffset);
                        }
                        else
                        {
                            // If the resource is already present in a volume, change it to
                            // the new content (but only in a volume, so as not to overwrite
                            // external patches - refer to bug #3366295).
                            // This is needed at least for the German version of Pharkas.
                            // That version contains several duplicate resources INSIDE the
                            // resource data files like fonts, views, scripts, etc. Thus,
                            // if we use the first entries in the resource file, half of the
                            // game will be English and umlauts will also be missing :P
                            if (resource._source.SourceType == ResSourceType.Volume)
                            {
                                // Maps are read during the scanning process (below), so
                                // need to be treated as unallocated in order for the new
                                // data from this volume to be picked up and used
                                if (resId.Type == ResourceType.Map)
                                {
                                    resource._status = ResourceStatus.NoMalloc;
                                }
                                resource._source = source;
                                resource._fileOffset = (int) fileOffset;
                                resource.size = 0;
                            }
                        }

#if ENABLE_SCI32
                        // Different CDs may have different audio maps on each disc. The
                        // ResourceManager does not know how to deal with this; it expects
                        // each resource ID to be unique across an entire game. To work
                        // around this problem, all audio maps from this disc must be
                        // processed immediately, since they will be replaced by the audio
                        // map from the next disc on the next call to readResourceMapSCI1
                        if (_multiDiscAudio && resId.Type == ResourceType.Map)
                        {
                            var audioMap =
                                (IntMapResourceSource) AddSource(
                                    new IntMapResourceSource("MAP", mapVolumeNr, resId.Number));
                            var volumeName = resId.Number == 65535
                                ? $"RESSFX.{mapVolumeNr:D3}"
                                : $"RESAUD.{mapVolumeNr:D3}";

                            var audioVolume =
                                AddSource(new AudioVolumeResourceSource(this, volumeName, audioMap, mapVolumeNr));
                            if (!audioMap._scanned)
                            {
                                audioVolume._scanned = true;
                                audioMap._scanned = true;
                                audioMap.ScanSource(this);
                            }
                        }
#endif
                    }
                }
            }

            return 0;
        }

        private ResourceErrorCodes ReadResourceMapSCI0(ExtMapResourceSource map)
        {
            Stream fileStream;
            var type = ResourceType.Invalid; // to silence a false positive in MSVC
            ushort number, id;
            uint offset;

            if (map._resourceFile != null)
            {
                throw new NotImplementedException();
                //fileStream = map._resourceFile.CreateReadStream();
                //if (fileStream == null)
                //    return ResourceErrorCodes.RESMAP_NOT_FOUND;
            }
            else
            {
                fileStream = Core.Engine.OpenFileRead(map.LocationName);
                if (fileStream == null)
                    return ResourceErrorCodes.RESMAP_NOT_FOUND;
            }

            using (var br = new BinaryReader(fileStream))
            {
                br.BaseStream.Seek(0, SeekOrigin.Begin);

                var bMask = (_mapVersion >= ResVersion.Sci1Middle) ? (byte) 0xF0 : (byte) 0xFC;
                var bShift = (_mapVersion >= ResVersion.Sci1Middle) ? (byte) 28 : (byte) 26;

                do
                {
                    // King's Quest 5 FM-Towns uses a 7 byte version of the SCI1 Middle map,
                    // splitting the type from the id.
                    if (_mapVersion == ResVersion.KQ5FMT)
                        type = ConvertResType(br.ReadByte());

                    id = br.ReadUInt16();
                    offset = br.ReadUInt32();

                    if (offset == 0xFFFFFFFF)
                        break;

                    if (br.BaseStream.Position == br.BaseStream.Length)
                    {
                        br.BaseStream.Dispose();
                        Warning($"Error while reading {map.LocationName}");
                        return ResourceErrorCodes.RESMAP_NOT_FOUND;
                    }

                    if (_mapVersion == ResVersion.KQ5FMT)
                    {
                        number = id;
                    }
                    else
                    {
                        type = ConvertResType(id >> 11);
                        number = (ushort) (id & 0x7FF);
                    }

                    var resId = new ResourceId(type, number);
                    // adding a new resource
                    if (_resMap.ContainsKey(resId) == false)
                    {
                        var source = FindVolume(map, (int) (offset >> bShift));
                        if (source == null)
                        {
                            Warning($"Could not get volume for resource {id}, VolumeID {offset >> bShift}");
                            if (_mapVersion != _volVersion)
                            {
                                Warning($"Retrying with the detected volume version instead");
                                Warning($"Map version was: {_mapVersion}, retrying with: {_volVersion}");
                                _mapVersion = _volVersion;
                                bMask = (_mapVersion == ResVersion.Sci1Middle) ? (byte) 0xF0 : (byte) 0xFC;
                                bShift = (_mapVersion == ResVersion.Sci1Middle) ? (byte) 28 : (byte) 26;
                                source = FindVolume(map, (int) (offset >> bShift));
                            }
                        }

                        AddResource(resId, source, (uint) (offset & (((~bMask) << 24) | 0xFFFFFF)));
                    }
                } while (br.BaseStream.Position < br.BaseStream.Length);
            }

            return 0;
        }

        protected void AddResource(ResourceId resId, ResourceSource src, uint offset, int size = 0)
        {
            // Adding new resource only if it does not exist
            if (!_resMap.ContainsKey(resId))
            {
                var res = new ResourceSource.Resource(this, resId);
                _resMap[resId] = res;
                res._source = src;
                res._fileOffset = (int) offset;
                res.size = size;
            }
        }

        /// <summary>
        /// Converts a map resource type to our type
        /// </summary>
        /// <param name="type">The type from the map/patch</param>
        /// <returns>The ResourceType</returns>
        public ResourceType ConvertResType(int type)
        {
            type &= 0x7f;

            var forceSci0 = false;

            // LSL6 hires doesn't have the chunk resource type, to match
            // the resource types of the lowres version, thus we use the
            // older resource types here.
            // PQ4 CD and QFG4 CD are SCI2.1, but use the resource types of the
            // corresponding SCI2 floppy disk versions.
            if (SciEngine.Instance != null && (SciEngine.Instance.GameId == SciGameId.LSL6HIRES ||
                                               SciEngine.Instance.GameId == SciGameId.QFG4 ||
                                               SciEngine.Instance.GameId == SciGameId.PQ4))
                forceSci0 = true;

            if (_mapVersion < ResVersion.Sci2 || forceSci0)
            {
                // SCI0 - SCI2
                if (type < s_resTypeMapSci0.Length)
                    return s_resTypeMapSci0[type];
            }
            else
            {
                if (type < s_resTypeMapSci21.Length)
                    return s_resTypeMapSci21[type];
            }

            return ResourceType.Invalid;
        }

        private ResourceCompression GetViewCompression()
        {
            var viewsTested = 0;

            // Test 10 views to see if any are compressed
            for (var i = 0; i < 1000; i++)
            {
                Stream fileStream;
                var res = TestResource(new ResourceId(ResourceType.View, (ushort) i));

                if (res == null)
                    continue;

                if (res._source.SourceType != ResSourceType.Volume)
                    continue;

                fileStream = GetVolumeFile(res._source);

                if (fileStream == null)
                    continue;
                fileStream.Seek(res._fileOffset, SeekOrigin.Begin);

                int szPacked;
                ResourceCompression compression;

                if (res.ReadResourceInfo(_volVersion, fileStream, out szPacked, out compression) !=
                    ResourceErrorCodes.NONE)
                {
                    if (res._source._resourceFile != null)
                        fileStream.Dispose();
                    continue;
                }

                if (res._source._resourceFile != null)
                    fileStream.Dispose();

                if (compression != ResourceCompression.None)
                    return compression;

                if (++viewsTested == 10)
                    break;
            }

            return ResourceCompression.None;
        }

        private Stream GetVolumeFile(ResourceSource source)
        {
            string path;
            if (source._resourceFile != null)
            {
                return ServiceLocator.FileStorage.OpenFileRead(source._resourceFile);
            }

            var filename = source.LocationName;

            // check if file is already opened
            foreach (var file in _volumeFiles)
            {
                path = ServiceLocator.FileStorage.GetPath(file);
                if (string.Equals(path, filename, StringComparison.OrdinalIgnoreCase))
                {
                    // move file to top
                    if (file != _volumeFiles.First())
                    {
                        _volumeFiles.Remove(file);
                        _volumeFiles.Insert(0, file);
                    }
                    return file;
                }
            }

            // adding a new file
            path = filename;
            if (!ServiceLocator.FileStorage.FileExists(filename))
            {
                path = ScummHelper.LocatePath(_directory, filename);
            }
            if (path != null)
            {
                var newFile = ServiceLocator.FileStorage.OpenFileRead(path);
                if (_volumeFiles.Count == MAX_OPENED_VOLUMES)
                {
                    _volumeFiles.RemoveAt(_volumeFiles.Count - 1);
                }
                _volumeFiles.Insert(0, newFile);
                return newFile;
            }
            // failed
            return null;
        }

        private void DetectSciVersion()
        {
            // We use the view compression to set a preliminary s_sciVersion for the sake of getResourceInfo
            // Pretend we have a SCI0 game
            s_sciVersion = SciVersion.V0_EARLY;
            var oldDecompressors = true;

            ResourceCompression viewCompression;
#if ENABLE_SCI32
            viewCompression = GetViewCompression();
#else
            if (_volVersion >= ResVersion.Sci2)
            {
                // SCI32 support isn't built in, thus view detection will fail
                viewCompression = ResourceCompression.Unknown;
            }
            else
            {
                viewCompression = GetViewCompression();
            }
#endif

            if (viewCompression != ResourceCompression.LZW)
            {
                // If it's a different compression type from kCompLZW, the game is probably
                // SciVersion.V1_EGA_ONLY or later. If the views are uncompressed, it is
                // likely not an early disk game.
                s_sciVersion = SciVersion.V1_EGA_ONLY;
                oldDecompressors = false;
            }

            // Set view type
            if (viewCompression == ResourceCompression.DCL
                || _volVersion == ResVersion.Sci11 // pq4demo
                || _volVersion == ResVersion.Sci11Mac
#if ENABLE_SCI32
                || viewCompression == ResourceCompression.STACpack
                || _volVersion == ResVersion.Sci2 // kq7
#endif
            )
            {
                // SCI1.1 VGA views
                _viewType = ViewType.Vga11;
            }
            else
            {
#if ENABLE_SCI32
// Otherwise we detect it from a view
                _viewType = DetectViewType();
#else
                if (_volVersion == ResVersion.Sci2 && viewCompression == ResourceCompression.Unknown)
                {
                    // A SCI32 game, but SCI32 support is disabled. Force the view type
                    // to kViewVga11, as we can't read from the game's resource files
                    _viewType = ViewType.Vga11;
                }
                else
                {
                    _viewType = DetectViewType();
                }
#endif
            }

            if (_volVersion == ResVersion.Sci11Mac)
            {
                var res = TestResource(new ResourceId(ResourceType.Script, 64920));
                // Distinguish between SCI1.1 and SCI32 games here. SCI32 games will
                // always include script 64920 (the Array class). Note that there are
                // no Mac SCI2 games. Yes, that means that GK1 Mac is SCI2.1 and not SCI2.

                // TODO: Decide between SCI2.1 and SCI3
                if (res != null)
                    s_sciVersion = SciVersion.V2_1_EARLY;
                else
                    s_sciVersion = SciVersion.V1_1;
                return;
            }

            // Handle SCI32 versions here
            if (s_sciVersion != SciVersion.V2_1_EARLY)
            {
                if (_volVersion >= ResVersion.Sci2)
                {
                    List<ResourceId> heaps = ListResources(ResourceType.Heap);
                    bool hasHeapResources = heaps.Count > 0;

                    // SCI2.1/3 and SCI1 Late resource maps are the same, except that
                    // SCI1 Late resource maps have the resource types or'd with
                    // 0x80. We differentiate between SCI2 and SCI2.1/3 based on that.
                    if (_mapVersion == ResVersion.Sci1Late)
                    {
                        s_sciVersion = SciVersion.V2;
                        return;
                    }
                    else if (hasHeapResources)
                    {
                        s_sciVersion = SciVersion.V2_1_EARLY; // exact SCI2.1 version is checked a bit later
                    }
                    else
                    {
                        s_sciVersion = SciVersion.V3;
                        return;
                    }
                }
            }

            if (s_sciVersion == SciVersion.V2_1_EARLY)
            {
                // we only know that it's SCI2.1, not which exact version it is

                // check, if selector "wordFail" inside vocab 997 exists, if it does it's SCI2.1 Early
                if (
                (CheckResourceForSignatures(ResourceType.Vocab, 997, detectSci21EarlySignature,
                    detectSci21EarlyBESignature)))
                {
                    // found . it is SCI2.1 early
                    return;
                }

                s_sciVersion = SciVersion.V2_1_MIDDLE;
                if (CheckResourceForSignatures(ResourceType.Script, 64918, detectSci21NewStringSignature, null))
                {
                    // new kString call detected, it's SCI2.1 late
                    // TODO: this call seems to be different on Mac
                    s_sciVersion = SciVersion.V2_1_LATE;
                    return;
                }
                return;
            }


            // Check for transitive SCI1/SCI1.1 games, like PQ1 here
            // If the game has any heap file (here we check for heap file 0), then
            // it definitely uses a SCI1.1 kernel
            if (TestResource(new ResourceId(ResourceType.Heap, 0)) != null)
            {
                s_sciVersion = SciVersion.V1_1;
                return;
            }

            switch (_mapVersion)
            {
                case ResVersion.Sci0Sci1Early:
                    if (_viewType == ViewType.Vga)
                    {
                        // VGA
                        s_sciVersion = SciVersion.V1_EARLY;
                        return;
                    }

                    // EGA
                    if (HasOldScriptHeader())
                    {
                        s_sciVersion = SciVersion.V0_EARLY;
                        return;
                    }

                    if (HasSci0Voc999())
                    {
                        s_sciVersion = SciVersion.V0_LATE;
                        return;
                    }

                    if (oldDecompressors)
                    {
                        // It's either SciVersion.V0_LATE or SciVersion.V01

                        // We first check for SCI1 vocab.999
                        if (TestResource(new ResourceId(ResourceType.Vocab, 999)) != null)
                        {
                            s_sciVersion = SciVersion.V01;
                            return;
                        }

                        // If vocab.999 is missing, we try vocab.900
                        if (TestResource(new ResourceId(ResourceType.Vocab, 900)) != null)
                        {
                            if (HasSci1Voc900())
                            {
                                s_sciVersion = SciVersion.V01;
                                return;
                            }
                            else
                            {
                                s_sciVersion = SciVersion.V0_LATE;
                                return;
                            }
                        }

                        Error("Failed to accurately determine SCI version");
                        // No parser, we assume SciVersion.V01.
                        s_sciVersion = SciVersion.V01;
                        return;
                    }

                    // New decompressors. It's either SciVersion.V1_EGA_ONLY or SciVersion.V1_EARLY.
                    if (HasSci1Voc900())
                    {
                        s_sciVersion = SciVersion.V1_EGA_ONLY;
                        return;
                    }

                    // SciVersion.V1_EARLY EGA versions lack the parser vocab
                    s_sciVersion = SciVersion.V1_EARLY;
                    return;
                case ResVersion.Sci1Middle:
                case ResVersion.KQ5FMT:
                    s_sciVersion = SciVersion.V1_MIDDLE;
                    // Amiga SCI1 middle games are actually SCI1 late
                    if (_viewType == ViewType.Amiga || _viewType == ViewType.Amiga64)
                        s_sciVersion = SciVersion.V1_LATE;
                    // Same goes for Mac SCI1 middle games
                    if (SciEngine.Instance != null && SciEngine.Instance.Platform == Platform.Macintosh)
                        s_sciVersion = SciVersion.V1_LATE;
                    return;
                case ResVersion.Sci1Late:
                    if (_volVersion == ResVersion.Sci11)
                    {
                        s_sciVersion = SciVersion.V1_1;
                        return;
                    }
                    s_sciVersion = SciVersion.V1_LATE;
                    return;
                case ResVersion.Sci11:
                    s_sciVersion = SciVersion.V1_1;
                    return;
                default:
                    s_sciVersion = SciVersion.NONE;
                    Error("detectSciVersion(): Unable to detect the game's SCI version");
                    break;
            }
        }

        private static bool CheckResourceDataForSignature(ResourceSource.Resource resource, BytePtr signature)
        {
            byte signatureSize = signature.Value;
            BytePtr resourceData = resource.data;

            signature.Offset++; // skip over size byte
            if (signatureSize < 4)
                Error("resource signature is too small, internal error");
            if (signatureSize > resource.size)
                return false;

            uint signatureDWord = signature.ToUInt32();
            signature += 4;
            signatureSize -= 4;

            int searchLimit = resource.size - signatureSize + 1;
            int dwordOffset = 0;
            while (dwordOffset < searchLimit)
            {
                if (signatureDWord == resourceData.ToUInt32(dwordOffset))
                {
                    // magic DWORD found, check if the rest matches as well
                    int offset = dwordOffset + 4;
                    int signaturePos = 0;
                    while (signaturePos < signatureSize)
                    {
                        if (resourceData[offset] != signature[signaturePos])
                            break;
                        offset++;
                        signaturePos++;
                    }
                    if (signaturePos >= signatureSize)
                        return true; // signature found
                }
                dwordOffset++;
            }
            return false;
        }

        private bool CheckResourceForSignatures(ResourceType resourceType, ushort resourceNr, byte[] signature1,
            byte[] signature2)
        {
            var resource = FindResource(new ResourceId(resourceType, resourceNr), false);

            if (resource != null)
            {
                // resource found and loaded, check for signatures
                if (signature1 != null)
                {
                    if (CheckResourceDataForSignature(resource, signature1))
                        return true;
                }
                if (signature2 != null)
                {
                    if (CheckResourceDataForSignature(resource, signature2))
                        return true;
                }
            }
            return false;
        }

        private bool HasSci1Voc900()
        {
            var res = FindResource(new ResourceId(ResourceType.Vocab, 900), false);

            if (res == null)
                return false;

            if (res.size < 0x1fe)
                return false;

            ushort offset = 0x1fe;

            while (offset < res.size)
            {
                offset++;
                do
                {
                    if (offset >= res.size)
                    {
                        // Out of bounds;
                        return false;
                    }
                } while (res.data[offset++] != 0);
                offset += 3;
            }

            return offset == res.size;
        }

        private bool HasSci0Voc999()
        {
            var res = FindResource(new ResourceId(ResourceType.Vocab, 999), false);

            if (res == null)
            {
                // No vocab present, possibly a demo version
                return false;
            }

            if (res.size < 2)
                return false;

            var count = res.data.ToUInt16();

            // Make sure there's enough room for the pointers
            if (res.size < (uint) count * 2)
                return false;

            // Iterate over all pointers
            for (uint i = 0; i < count; i++)
            {
                // Offset to string
                var offset = res.data.ToUInt16(2 + count * 2);

                // Look for end of string
                do
                {
                    if (offset >= res.size)
                    {
                        // Out of bounds
                        return false;
                    }
                } while (res.data[offset++] != 0);
            }

            return true;
        }

        private bool HasOldScriptHeader()
        {
            var res = FindResource(new ResourceId(ResourceType.Script, 0), false);

            if (res == null)
            {
                Error("resMan: Failed to find script.000");
                return false;
            }

            var offset = 2;
            const int objTypes = 17;

            while (offset < res.size)
            {
                var objType = res.data.ToUInt16(offset);

                if (objType == 0)
                {
                    offset += 2;
                    // We should be at the end of the resource now
                    return offset == res.size;
                }

                if (objType >= objTypes)
                {
                    // Invalid objType
                    return false;
                }

                int skip = res.data.ToUInt16(offset + 2);

                if (skip < 2)
                {
                    // Invalid size
                    return false;
                }

                offset += skip;
            }

            return false;
        }

        internal ResourceSource.Resource TestResource(ResourceId id)
        {
            return _resMap.ContainsKey(id) ? _resMap[id] : null;
        }

        private ViewType DetectViewType()
        {
            for (var i = 0; i < 1000; i++)
            {
                var res = FindResource(new ResourceId(ResourceType.View, (ushort) i), false);
                if (res != null)
                {
                    // Skip views coming from patch files
                    if (res._source.SourceType == ResSourceType.Patch)
                        continue;

                    switch (res.data[1])
                    {
                        case 128:
                            // If the 2nd byte is 128, it's a VGA game.
                            // However, Longbow Amiga (AGA, 64 colors), also sets this byte
                            // to 128, but it's a mixed VGA/Amiga format. Detect this from
                            // the platform here.
                            if (SciEngine.Instance != null && SciEngine.Instance.Platform == Platform.Amiga)
                                return ViewType.Amiga64;

                            return ViewType.Vga;
                        case 0:
                            // EGA or Amiga, try to read as Amiga view

                            if (res.size < 10)
                                return ViewType.Unknown;

                            // Read offset of first loop
                            var offset = res.data.ToUInt16(8);

                            if (offset + 6U >= res.size)
                                return ViewType.Unknown;

                            // Read offset of first cel
                            offset = res.data.ToUInt16(offset + 4);

                            if (offset + 4U >= res.size)
                                return ViewType.Unknown;

                            // Check palette offset, amiga views have no palette
                            if (res.data.ToUInt16(6) != 0)
                                return ViewType.Ega;

                            var width = res.data.ToUInt16(offset);
                            offset += 2;
                            var height = res.data.ToUInt16(offset);
                            offset += 6;

                            // To improve the heuristic, we skip very small views
                            if (height < 10)
                                continue;

                            // Check that the RLE data stays within bounds
                            int y;
                            for (y = 0; y < height; y++)
                            {
                                var x = 0;

                                while ((x < width) && (offset < res.size))
                                {
                                    var op = res.data[offset++];
                                    x += (op & 0x07) != 0 ? op & 0x07 : op >> 3;
                                }

                                // Make sure we got exactly the right number of pixels for this row
                                if (x != width)
                                    return ViewType.Ega;
                            }

                            return ViewType.Amiga;
                    }
                }
            }

            // this may happen if there are serious system issues (or trying to add a broken game)
            Warning("resMan: Couldn't find any views");
            return ViewType.Unknown;
        }

        private void AddToLRU(ResourceSource.Resource res)
        {
            if (res._status != ResourceStatus.Allocated)
            {
                Warning($"resMan: trying to enqueue resource with state {res._status}");
                return;
            }
            _LRU.Insert(0, res);
            _memoryLRU += res.size;
#if SCI_VERBOSE_RESMAN
            Debug("Adding {0}.{1:D3} ({2} bytes) to lru control: {3} bytes total",
                GetResourceTypeName(res.Type), res.Number, res.size,
                mgr._memoryLRU);
#endif
            res._status = ResourceStatus.Enqueued;
        }

        private void FreeOldResources()
        {
            while (ResourceSource.Resource.MAX_MEMORY < _memoryLRU)
            {
                var goner = _LRU.Last();
                RemoveFromLRU(goner);
                goner.Unalloc();
#if SCI_VERBOSE_RESMAN
                Debug("resMan-debug: LRU: Freeing {0}.{1:D3} ({2} bytes)", GetResourceTypeName(goner.Type),
                    goner.Number, goner.size);
#endif
            }
        }

        private void LoadResource(ResourceSource.Resource res)
        {
            res._source.LoadResource(this, res);
        }

        private void RemoveFromLRU(ResourceSource.Resource res)
        {
            if (res._status != ResourceStatus.Enqueued)
            {
                Warning("resMan: trying to remove resource that isn't enqueued");
                return;
            }
            Debug("removeFromLRU {0}, size: {1}", res.Number, res.size);
            _LRU.Remove(res);
            _memoryLRU -= res.size;
            res._status = ResourceStatus.Allocated;
        }

        private void AddScriptChunkSources()
        {
#if ENABLE_SCI32
            if (_mapVersion < ResVersion.Sci2) return;

            // If we have no scripts, but chunk 0 is present, open up the chunk
            // to try to get to any scripts in there. The Lighthouse SCI2.1 demo
            // does exactly this.

            var resources = ListResources(ResourceType.Script);

            if (resources.Count == 0 && TestResource(new ResourceId(ResourceType.Chunk, 0)) != null)
                AddResourcesFromChunk(0);
#endif
        }

        public static SciVersion GetSciVersion()
        {
            System.Diagnostics.Debug.Assert(s_sciVersion != SciVersion.NONE);
            return s_sciVersion;
        }

        private void AddResourcesFromChunk(ushort id)
        {
            AddSource(new ChunkResourceSource($"Chunk {id}", id));
            ScanNewSources();
        }

        private bool AddAudioSources()
        {
#if ENABLE_SCI32
            // Multi-disc audio is added during addAppropriateSources for those titles
            // that require it
            if (_multiDiscAudio)
            {
                return true;
            }
#endif
            var resources = ListResources(ResourceType.Map);
            foreach (var itr in resources)
            {
                var src = AddSource(new IntMapResourceSource("MAP", 0, itr.Number));

                if ((itr.Number == 65535) && ScummHelper.LocatePath(_directory, "RESOURCE.SFX") != null)
                    AddSource(new AudioVolumeResourceSource(this, "RESOURCE.SFX", src, 0));
                else if (ScummHelper.LocatePath(_directory, "RESOURCE.AUD") != null)
                    AddSource(new AudioVolumeResourceSource(this, "RESOURCE.AUD", src, 0));
                else
                    return false;
            }

            return true;
        }

        private void ScanNewSources()
        {
            Debug("num sources: {0}",_sources.Count);
            foreach (var source in _sources.ToList())
            {
                if (source._scanned) continue;
                source._scanned = true;
                source.ScanSource(this);
            }
        }

        /// <summary>
        /// Lists all resources of the specified type.
        /// </summary>
        /// <param name="type">The resource type to look for</param>
        /// <param name="mapNumber">For audio36 and sync36, limit search to this map</param>
        /// <returns>The resource list</returns>
        public List<ResourceId> ListResources(ResourceType type, int mapNumber = -1)
        {
            var resources = new List<ResourceId>();
            foreach (var itr in _resMap)
            {
                if ((itr.Value.ResourceType == type) && ((mapNumber == -1) || (itr.Value.Number == mapNumber)))
                    resources.Add(itr.Value._id);
            }
            return resources;
        }

        public void ChangeAudioDirectory(string path)
        {
            // TODO: This implementation is broken.
            return;

#if Undefined
// Remove all of the audio map resource sources, as well as the audio resource sources
            for (var i = 0; i < _sources.Count;)
            {
                var source = _sources[i];
                var sourceType = source.SourceType;

                // Remove the resource source, if it's an audio map or an audio file
                if (sourceType == ResSourceType.IntMap || sourceType == ResSourceType.AudioVolume)
                {
                    // Don't remove 65535.map (the SFX map) or resource.sfx
                    if (source._volumeNumber == 65535 || source.LocationName == "RESOURCE.SFX")
                    {
                        ++i;
                        continue;
                    }

                    // erase() will move the iterator to the next element
                    _sources.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }

            // Now, readd the audio resource sources
            var mapName = "MAP";
            var audioResourceName = "RESOURCE.AUD";
            if (path.Length != 0)
            {
                mapName = $"{path}/MAP";
                audioResourceName = $"{path}/RESOURCE.AUD";
            }

            var resources = ListResources(ResourceType.Map);
            foreach (var it in resources)
            {
                // Don't readd 65535.map or resource.sfx
                if ((it.Number == 65535))
                    continue;

                var src = AddSource(new IntMapResourceSource(mapName, it.Number));
                AddSource(new AudioVolumeResourceSource(this, audioResourceName, src, 0));
            }

            // Rescan the newly added resources
            ScanNewSources();
#endif
        }

        private ResVersion DetectMapVersion()
        {
            Stream fileStream = null;
            var buff = new byte[6];
            ResourceSource rsrc = null;

            // TODO: Add SCI3 support

            foreach (var source in _sources)
            {
                rsrc = source;
                if (source.SourceType == ResSourceType.ExtMap)
                {
                    if (source._resourceFile != null)
                    {
                        throw new NotImplementedException();
                        //fileStream = source._resourceFile.CreateReadStream();
                    }
                    else
                    {
                        var file = ServiceLocator.FileStorage.OpenFileRead(source.LocationName);
                        fileStream = file;
                    }
                    break;
                }
                if (source.SourceType == ResSourceType.MacResourceFork)
                {
                    return ResVersion.Sci11Mac;
                }
            }

            if (fileStream == null)
            {
                throw new InvalidOperationException("Failed to open resource map file");
            }

            // detection
            // SCI0 and SCI01 maps have last 6 bytes set to FF
            fileStream.Seek(-4, SeekOrigin.End);
            var br = new BinaryReader(fileStream);
            var uEnd = br.ReadUInt32();
            if (uEnd == 0xFFFFFFFF)
            {
                // check if the last 7 bytes are all ff, indicating a KQ5 FM-Towns map
                fileStream.Seek(-7, SeekOrigin.End);
                fileStream.Read(buff, 0, 3);
                if (buff[0] == 0xff && buff[1] == 0xff && buff[2] == 0xff)
                {
                    fileStream.Dispose();
                    return ResVersion.KQ5FMT;
                }

                // check if 0 or 01 - try to read resources in SCI0 format and see if exists
                fileStream.Seek(0, SeekOrigin.Begin);
                while (fileStream.Read(buff, 0, 6) == 6 && !(buff[0] == 0xFF && buff[1] == 0xFF && buff[2] == 0xFF))
                {
                    if (FindVolume(rsrc, (buff[5] & 0xFC) >> 2) == null)
                    {
                        fileStream.Dispose();
                        return ResVersion.Sci1Middle;
                    }
                }
                fileStream.Dispose();
                return ResVersion.Sci0Sci1Early;
            }

            // SCI1 and SCI1.1 maps consist of a fixed 3-byte header, a directory list (3-bytes each) that has one entry
            // of id FFh and points to EOF. The actual entries have 6-bytes on SCI1 and 5-bytes on SCI1.1
            byte directoryType;
            ushort directoryOffset;
            ushort lastDirectoryOffset = 0;
            ushort directorySize;
            var mapDetected = ResVersion.Unknown;
            fileStream.Seek(0, SeekOrigin.Begin);

            while (fileStream.Position <= fileStream.Length)
            {
                directoryType = br.ReadByte();
                directoryOffset = br.ReadUInt16();

                // Only SCI32 has directory type < 0x80
                if (directoryType < 0x80 && (mapDetected == ResVersion.Unknown || mapDetected == ResVersion.Sci2))
                    mapDetected = ResVersion.Sci2;
                else if (directoryType < 0x80 || ((directoryType & 0x7f) > 0x20 && directoryType != 0xFF))
                    break;

                // Offset is above file size? . definitely not SCI1/SCI1.1
                if (directoryOffset > fileStream.Length)
                    break;

                if (lastDirectoryOffset != 0 && mapDetected == ResVersion.Unknown)
                {
                    directorySize = (ushort) (directoryOffset - lastDirectoryOffset);
                    if ((directorySize % 5) != 0 && (directorySize % 6 == 0))
                        mapDetected = ResVersion.Sci1Late;
                    if ((directorySize % 5 == 0) && (directorySize % 6) != 0)
                        mapDetected = ResVersion.Sci11;
                }

                if (directoryType == 0xFF)
                {
                    // FFh entry needs to point to EOF
                    if (directoryOffset != fileStream.Length)
                        break;

                    if (mapDetected != ResVersion.Unknown)
                        return mapDetected;
                    return ResVersion.Sci1Late;
                }

                lastDirectoryOffset = directoryOffset;
            }

            return ResVersion.Unknown;
        }

        private ResVersion DetectVolVersion()
        {
            Stream fileStream = null;

            foreach (var it in _sources)
            {
                var rsrc = it;
                if (rsrc.SourceType == ResSourceType.Volume)
                {
                    if (rsrc._resourceFile != null)
                    {
                        fileStream = ServiceLocator.FileStorage.OpenFileRead(rsrc._resourceFile);
                    }
                    else
                    {
                        var path = rsrc.LocationName;
                        if (path != null)
                        {
                            fileStream = ServiceLocator.FileStorage.OpenFileRead(path);
                        }
                    }
                    break;
                }
                if (rsrc.SourceType == ResSourceType.MacResourceFork)
                    return ResVersion.Sci11Mac;
            }

            if (fileStream == null)
            {
                Warning(
                    "Failed to open volume file - if you got resource.p01/resource.p02/etc. files, merge them together into resource.000");
                // resource.p01/resource.p02/etc. may be there when directly copying the files from the original floppies
                // the sierra installer would merge those together (perhaps we could do this as well?)
                // possible TODO
                // example for such game: Laura Bow 2
                return ResVersion.Unknown;
            }

            var br = new BinaryReader(fileStream);

            // SCI0 volume format:  {wResId wPacked+4 wUnpacked wCompression} = 8 bytes
            // SCI1 volume format:  {bResType wResNumber wPacked+4 wUnpacked wCompression} = 9 bytes
            // SCI1.1 volume format:  {bResType wResNumber wPacked wUnpacked wCompression} = 9 bytes
            // SCI32 volume format:   {bResType wResNumber dwPacked dwUnpacked wCompression} = 13 bytes
            // Try to parse volume with SCI0 scheme to see if it make sense
            // Checking 1MB of data should be enough to determine the version
            ushort wCompression;
            uint dwPacked, dwUnpacked;
            var curVersion = ResVersion.Sci0Sci1Early;
            var failed = false;
            var sci11Align = false;

            // Check for SCI0, SCI1, SCI1.1, SCI32 v2 (Gabriel Knight 1 CD) and SCI32 v3 (LSL7) formats
            while (fileStream.Position < fileStream.Length && fileStream.Position < 0x100000)
            {
                if (curVersion > ResVersion.Sci0Sci1Early)
                    fileStream.ReadByte();
                fileStream.Seek(2, SeekOrigin.Current); // resId
                dwPacked = curVersion < ResVersion.Sci2 ? br.ReadUInt16() : br.ReadUInt32();
                dwUnpacked = curVersion < ResVersion.Sci2 ? br.ReadUInt16() : br.ReadUInt32();

                // The compression field is present, but bogus when
                // loading SCI3 volumes, the format is otherwise
                // identical to SCI2. We therefore get the compression
                // indicator here, but disregard it in the following
                // code.
                wCompression = br.ReadUInt16();

                if (fileStream.Position == fileStream.Length)
                {
                    fileStream.Dispose();
                    return curVersion;
                }

                int chk;

                if (curVersion == ResVersion.Sci0Sci1Early)
                    chk = 4;
                else if (curVersion < ResVersion.Sci2)
                    chk = 20;
                else
                    chk = 32; // We don't need this, but include it for completeness

                var offs = curVersion < ResVersion.Sci11 ? 4 : 0;
                if ((curVersion < ResVersion.Sci2 && wCompression > chk)
                    || (curVersion == ResVersion.Sci2 && wCompression != 0 && wCompression != 32)
                    || (wCompression == 0 && dwPacked != dwUnpacked + offs)
                    || (dwUnpacked < dwPacked - offs))
                {
                    // Retry with a newer SCI version
                    if (curVersion == ResVersion.Sci0Sci1Early)
                    {
                        curVersion = ResVersion.Sci1Late;
                    }
                    else if (curVersion == ResVersion.Sci1Late)
                    {
                        curVersion = ResVersion.Sci11;
                    }
                    else if (curVersion == ResVersion.Sci11 && !sci11Align)
                    {
                        // Later versions (e.g. QFG1VGA) have resources word-aligned
                        sci11Align = true;
                    }
                    else if (curVersion == ResVersion.Sci11)
                    {
                        curVersion = ResVersion.Sci2;
                    }
                    else if (curVersion == ResVersion.Sci2)
                    {
                        curVersion = ResVersion.Sci3;
                    }
                    else
                    {
                        // All version checks failed, exit loop
                        failed = true;
                        break;
                    }

                    fileStream.Seek(0, SeekOrigin.Begin);
                    continue;
                }

                if (curVersion < ResVersion.Sci11)
                    fileStream.Seek(dwPacked - 4, SeekOrigin.Current);
                else if (curVersion == ResVersion.Sci11)
                    fileStream.Seek((sci11Align && ((9 + dwPacked) % 2) != 0) ? dwPacked + 1 : dwPacked,
                        SeekOrigin.Current);
                else if (curVersion >= ResVersion.Sci2)
                    fileStream.Seek(dwPacked, SeekOrigin.Current);
            }

            if (!failed)
                return curVersion;

            // Failed to detect volume version
            return ResVersion.Unknown;
        }

        private ResourceSource FindVolume(ResourceSource map, int volumeNr)
        {
            foreach (var it in _sources)
            {
                var src = it.FindVolume(map, volumeNr);
                if (src != null)
                    return src;
            }

            return null;
        }

        private ResourceSource AddPatchDir(string dirname)
        {
            ResourceSource newsrc = new DirectoryResourceSource(dirname);
            _sources.Add(newsrc);
            return null;
        }

        private ResourceSource AddSource(ResourceSource newsrc)
        {
            System.Diagnostics.Debug.Assert(newsrc != null);
            _sources.Add(newsrc);
            return newsrc;
        }

        /// <summary>
        /// Add an external (i.e., separate file) map resource to the resource
        /// manager's list of sources.
        /// </summary>
        /// <param name="filename">The name of the volume to add</param>
        /// <param name="volumeNr">The volume number the map starts at, 0 for &lt;SCI2.1</param>
        /// <returns>Added source structure, or null if an error occurred.</returns>
        private ResourceSource AddExternalMap(string filename, int volumeNr = 0)
        {
            ResourceSource newsrc = new ExtMapResourceSource(filename, volumeNr);
            _sources.Add(newsrc);
            return newsrc;
        }

        public class ResourceSource
        {
            /// <summary>
            /// Class for storing resources in memory.
            /// </summary>
            public class Resource
            {
                internal const int MAX_MEMORY = 256 * 1024; // 256KB

                internal ResourceId _id; // TODO: _id could almost be made const, only readResourceInfo() modifies it...

                /// <summary>
                /// Offset in file
                /// </summary>
                internal int _fileOffset;

                internal ResourceSource _source;
                protected ResourceManager _resMan;
                internal ResourceStatus _status;

                /// <summary>
                /// Number of places where this resource was locked
                /// </summary>
                internal ushort _lockers;

                // NOTE : Currently most member variables lack the underscore prefix and have
                // public visibility to let the rest of the engine compile without changes.
                public int size;
                public byte[] data;
                public int _headerSize;
                public byte[] _header;

                public uint AudioCompressionType => _source.AudioCompressionType;

                public ResourceType ResourceType => _id.Type;

                public ushort Number => _id.Number;

                public bool IsLocked => _status == ResourceStatus.Locked;

                public string ResourceLocation => _source.LocationName;

                public Resource(ResourceManager resMan, ResourceId id)
                {
                    _resMan = resMan;
                    _id = id;
                }

                public ResourceErrorCodes ReadResourceInfo(ResVersion volVersion, Stream file,
                    out int szPacked, out ResourceCompression compression)
                {
                    szPacked = 0;
                    compression = ResourceCompression.None;

                    // SCI0 volume format:  {wResId wPacked+4 wUnpacked wCompression} = 8 bytes
                    // SCI1 volume format:  {bResType wResNumber wPacked+4 wUnpacked wCompression} = 9 bytes
                    // SCI1.1 volume format:  {bResType wResNumber wPacked wUnpacked wCompression} = 9 bytes
                    // SCI32 volume format :  {bResType wResNumber dwPacked dwUnpacked wCompression} = 13 bytes
                    ushort w, number;
                    int wCompression, szUnpacked;
                    ResourceType type;

                    if (file.Length == 0)
                        return ResourceErrorCodes.EMPTY_RESOURCE;

                    var br = new BinaryReader(file);
                    switch (volVersion)
                    {
                        case ResVersion.Sci0Sci1Early:
                        case ResVersion.Sci1Middle:
                            w = br.ReadUInt16();
                            type = _resMan.ConvertResType(w >> 11);
                            number = (ushort) (w & 0x7FF);
                            szPacked = br.ReadUInt16() - 4;
                            szUnpacked = br.ReadUInt16();
                            wCompression = br.ReadUInt16();
                            break;
                        case ResVersion.Sci1Late:
                            type = _resMan.ConvertResType(file.ReadByte());
                            number = br.ReadUInt16();
                            szPacked = br.ReadUInt16() - 4;
                            szUnpacked = br.ReadUInt16();
                            wCompression = br.ReadUInt16();
                            break;
                        case ResVersion.Sci11:
                            type = _resMan.ConvertResType(br.ReadByte());
                            number = br.ReadUInt16();
                            szPacked = br.ReadUInt16();
                            szUnpacked = br.ReadUInt16();
                            wCompression = br.ReadUInt16();
                            break;
#if ENABLE_SCI32
                        case ResVersion.Sci2:
                        case ResVersion.Sci3:
                            type = _resMan.ConvertResType(br.ReadByte());
                            number = br.ReadUInt16();
                            szPacked = br.ReadInt32();
                            szUnpacked = br.ReadInt32();

                            // The same comment applies here as in
                            // detectVolVersion regarding SCI3. We ignore the
                            // compression field for SCI3 games, but must presume
                            // it exists in the file.
                            wCompression = br.ReadUInt16();

                            if (volVersion == ResVersion.Sci3)
                                wCompression = szPacked != szUnpacked ? 32 : 0;

                            break;
#endif
                        default:
                            return ResourceErrorCodes.RESMAP_INVALID_ENTRY;
                    }

                    // check if there were errors while reading
                    if (file.Position >= file.Length)
                        return ResourceErrorCodes.IO_ERROR;

                    _id = new ResourceId(type, number);
                    size = szUnpacked;

                    // checking compression method
                    switch (wCompression)
                    {
                        case 0:
                            compression = ResourceCompression.None;
                            break;
                        case 1:
                            compression = (GetSciVersion() <= SciVersion.V01)
                                ? ResourceCompression.LZW
                                : ResourceCompression.Huffman;
                            break;
                        case 2:
                            compression = (GetSciVersion() <= SciVersion.V01)
                                ? ResourceCompression.Huffman
                                : ResourceCompression.LZW1;
                            break;
                        case 3:
                            compression = ResourceCompression.LZW1View;
                            break;
                        case 4:
                            compression = ResourceCompression.LZW1Pic;
                            break;
                        case 18:
                        case 19:
                        case 20:
                            compression = ResourceCompression.DCL;
                            break;
#if ENABLE_SCI32
                        case 32:
                            compression = ResourceCompression.STACpack;
                            break;
#endif
                        default:
                            compression = ResourceCompression.Unknown;
                            break;
                    }

                    return (compression == ResourceCompression.Unknown)
                        ? ResourceErrorCodes.UNKNOWN_COMPRESSION
                        : ResourceErrorCodes.NONE;
                }

                public void Unalloc()
                {
                    data = null;
                    _status = ResourceStatus.NoMalloc;
                }

                public bool LoadFromAudioVolumeSCI1(Stream stream)
                {
                    data = new byte[size];

                    int really_read = stream.Read(data, 0, size);
                    if (really_read != size)
                        Warning("Read {0} bytes from {1} but expected {2}", really_read, _id, size);

                    _status = ResourceStatus.Allocated;
                    return true;
                }

                public bool LoadFromAudioVolumeSCI11(Stream stream)
                {
                    // Check for WAVE files here
                    var br = new BinaryReader(stream);
                    uint riffTag = br.ReadUInt32BigEndian();
                    if (riffTag == ScummHelper.MakeTag('R', 'I', 'F', 'F'))
                    {
                        _headerSize = 0;
                        size = br.ReadInt32() + 8;
                        stream.Seek(-8, SeekOrigin.Current);
                        return LoadFromWaveFile(stream);
                    }
                    stream.Seek(-4, SeekOrigin.Current);

                    // Rave-resources (King's Quest 6) don't have any header at all
                    if (ResourceType != ResourceType.Rave)
                    {
                        var type = _resMan.ConvertResType(br.ReadByte());
                        if (((ResourceType == ResourceType.Audio || ResourceType == ResourceType.Audio36) &&
                             (type != ResourceType.Audio))
                            ||
                            ((ResourceType == ResourceType.Sync || ResourceType == ResourceType.Sync36) &&
                             (type != ResourceType.Sync)))
                        {
                            Warning("Resource type mismatch loading {0}", _id);
                            Unalloc();
                            return false;
                        }

                        _headerSize = br.ReadByte();

                        if (type == ResourceType.Audio)
                        {
                            if (_headerSize != 7 && _headerSize != 11 && _headerSize != 12)
                            {
                                Warning("Unsupported audio header");
                                Unalloc();
                                return false;
                            }

                            if (_headerSize != 7)
                            {
                                // Size is defined already from the map
                                // Load sample size
                                stream.Seek(7, SeekOrigin.Current);
                                size = br.ReadInt32();
                                // Adjust offset to point at the header data again
                                stream.Seek(-11, SeekOrigin.Current);
                            }
                        }
                    }
                    return LoadPatch(stream);
                }

                public bool LoadFromWaveFile(Stream stream)
                {
                    data = new byte[size];

                    int really_read = stream.Read(data, 0, size);
                    if (really_read != size)
                        Error("Read {0} bytes from {1} but expected {2}", really_read, _id, size);

                    _status = ResourceStatus.Allocated;
                    return true;
                }

                public bool LoadFromPatchFile()
                {
                    string filename = _source.LocationName;
                    var file = Core.Engine.OpenFileRead(filename);
                    if (file == null)
                    {
                        Warning($"Failed to open patch file {filename}");
                        Unalloc();
                        return false;
                    }
                    using (file)
                    {
                        // Skip resourceid and header size byte
                        file.Seek(2, SeekOrigin.Begin);
                        return LoadPatch(file);
                    }
                }

                // Resource manager constructors and operations

                private bool LoadPatch(Stream file)
                {
                    Resource res = this;

                    // We assume that the resource type matches res.type
                    //  We also assume that the current file position is right at the actual data (behind resourceid/headersize byte)

                    res.data = new byte[res.size];

                    if (res._headerSize > 0)
                        res._header = new byte[res._headerSize];

                    if ((res.data == null) || ((res._headerSize > 0) && (res._header == null)))
                    {
                        Error($"Can't allocate {res.size + res._headerSize} bytes needed for loading {res._id}");
                    }

                    int really_read;
                    if (res._headerSize > 0)
                    {
                        really_read = file.Read(res._header, 0, res._headerSize);
                        if (really_read != res._headerSize)
                            Error($"Read {really_read} bytes from {res._id} but expected {res._headerSize}");
                    }

                    really_read = file.Read(res.data, 0, res.size);
                    if (really_read != res.size)
                        Error($"Read {really_read} bytes from {res._id} but expected {res.size}");

                    res._status = ResourceStatus.Allocated;
                    return true;
                }


                internal ResourceErrorCodes Decompress(ResVersion volVersion, Stream file)
                {
                    int szPacked;
                    ResourceCompression compression;

                    // fill resource info
                    var errorNum = ReadResourceInfo(volVersion, file, out szPacked, out compression);
                    if (errorNum != ResourceErrorCodes.NONE)
                        return errorNum;

                    // getting a decompressor
                    Decompressor dec;
                    switch (compression)
                    {
                        case ResourceCompression.None:
                            dec = new Decompressor();
                            break;
                        case ResourceCompression.Huffman:
                            dec = new DecompressorHuffman();
                            break;
                        case ResourceCompression.LZW:
                        case ResourceCompression.LZW1:
                        case ResourceCompression.LZW1View:
                        case ResourceCompression.LZW1Pic:
                            dec = new DecompressorLzw(compression);
                            break;
                        case ResourceCompression.DCL:
                            dec = new DecompressorDcl();
                            break;
#if ENABLE_SCI32
                        case ResourceCompression.STACpack:
                            dec = new DecompressorLzs();
                            break;
#endif
                        default:
                            Error($"Resource {_id}: Compression method {compression} not supported");
                            return ResourceErrorCodes.UNKNOWN_COMPRESSION;
                    }

                    data = new byte[size];
                    _status = ResourceStatus.Allocated;
                    errorNum = dec.Unpack(file, data, szPacked, size);
                    if (errorNum != ResourceErrorCodes.NONE)
                        Unalloc();

                    return errorNum;
                }

#if ENABLE_SCI32
                public Stream MakeStream()
                {
                    return new MemoryStream(data, 0, size);
                }
#endif

                public override string ToString()
                {
                    return $"{_id}: {size}";
                }
            }

            private static readonly string[] ErrorDescriptions =
            {
                "No error",
                "I/O error",
                "Resource is empty (size 0)",
                "resource.map entry is invalid",
                "resource.map file not found",
                "No resource files found",
                "Unknown compression method",
                "Decompression failed: Sanity check failed",
                "Decompression failed: Resource too big"
            };

            protected readonly ResSourceType _sourceType;
            protected readonly string _name;
            public readonly string _resourceFile;
            public readonly int _volumeNumber;
            internal bool _scanned;

            public ResSourceType SourceType => _sourceType;

            public string LocationName => _name;

            // FIXME: This audio specific method is a hack. After all, why should a
            // ResourceSource or a Resource (which uses this method) have audio
            // specific methods? But for now we keep this, as it eases transition.
            public virtual uint AudioCompressionType => 0;

            public ResourceSource(ResSourceType type, string name, int volNum = 0, string resFile = null)
            {
                _sourceType = type;
                _name = name;
                _volumeNumber = volNum;
                _resourceFile = resFile;
            }

            public virtual void ScanSource(ResourceManager resMan)
            {
            }

            public virtual ResourceSource FindVolume(ResourceSource map, int volNum)
            {
                return null;
            }

            /// <summary>
            /// Auxiliary method, used by loadResource implementations.
            /// </summary>
            /// <param name="resMan"></param>
            /// <param name="res"></param>
            /// <returns></returns>
            public Stream GetVolumeFile(ResourceManager resMan, Resource res)
            {
                var fileStream = resMan.GetVolumeFile(this);
                if (fileStream == null)
                {
                    Warning($"Failed to open {LocationName}");
                    res?.Unalloc();
                }

                return fileStream;
            }

            /// <summary>
            /// Load a resource.
            /// </summary>
            /// <param name="resMan"></param>
            /// <param name="res"></param>
            public virtual void LoadResource(ResourceManager resMan, Resource res)
            {
                var fileStream = GetVolumeFile(resMan, res);
                if (fileStream == null)
                    return;

                fileStream.Seek(res._fileOffset, SeekOrigin.Begin);

                Debug("LoadResource {0}, offset: {1}", res.Number, res._fileOffset);
                var error = res.Decompress(resMan._volVersion, fileStream);
                if (error != ResourceErrorCodes.NONE)
                {
                    Warning($"Error {error} occurred while reading {res._id} from resource file " +
                            $"{res.ResourceLocation}: {ErrorDescriptions[(int) error]}");
                    res.Unalloc();
                }

                if (_resourceFile != null)
                    fileStream.Dispose();
            }
        }

        private class VolumeResourceSource : ResourceSource
        {
            private readonly ResourceSource _associatedMap;

            public VolumeResourceSource(string name, ResourceSource map, int volNum,
                ResSourceType type = ResSourceType.Volume)
                : base(type, name, volNum)
            {
                _associatedMap = map;
            }

            public VolumeResourceSource(string name, ResourceSource map, int volNum, string resFile)
                : base(ResSourceType.Volume, name, volNum, resFile)
            {
                _associatedMap = map;
            }

            public override ResourceSource FindVolume(ResourceSource map, int volNum)
            {
                if (_associatedMap == map && _volumeNumber == volNum)
                    return this;
                return null;
            }
        }

        public string FindSierraGameId()
        {
            // In SCI0-SCI1, the heap is embedded in the script. In SCI1.1 - SCI2.1,
            // it's in a separate heap resource
            ResourceSource.Resource heap = null;
            int nameSelector = 3;

            if (GetSciVersion() < SciVersion.V1_1)
            {
                heap = FindResource(new ResourceId(ResourceType.Script, 0), false);
            }
            else if (GetSciVersion() >= SciVersion.V1_1 && GetSciVersion() <= SciVersion.V2_1_LATE)
            {
                heap = FindResource(new ResourceId(ResourceType.Heap, 0), false);
                nameSelector += 5;
            }
            else if (GetSciVersion() == SciVersion.V3)
            {
                Warning("TODO: findSierraGameId(): SCI3 equivalent");
            }

            if (heap == null)
                return "";

            short gameObjectOffset = (short) FindGameObject(false).Offset;

            if (gameObjectOffset == 0)
                return "";

            // Seek to the name selector of the first export
            BytePtr offsetPtr = new BytePtr(heap.data, gameObjectOffset + nameSelector * 2);
            ushort offset = !IsSci11Mac ? offsetPtr.ToUInt16() : offsetPtr.ToUInt16BigEndian();
            BytePtr seeker = new BytePtr(heap.data, offset);
            string sierraId = seeker.GetRawText();

            return sierraId;
        }
    }

    internal class SoundResource
    {
        public class Channel
        {
            public byte number;
            public byte flags;
            public byte poly;
            public ushort prio;
            public ushort size;
            public BytePtr data;
            public ushort curPos;
            public long time;
            public byte prev;
        }

        public class Track
        {
            public byte type;
            public byte channelCount;
            public Channel[] channels;
            public short digitalChannelNr;
            public ushort digitalSampleRate;
            public ushort digitalSampleSize;
            public ushort digitalSampleStart;
            public ushort digitalSampleEnd;
        }

        private readonly SciVersion _soundVersion;
        private readonly int _trackCount;
        private readonly Track[] _tracks;
        private readonly ResourceManager.ResourceSource.Resource _innerResource;
        private readonly ResourceManager _resMan;

        public Track DigitalTrack
        {
            get
            {
                for (var trackNr = 0; trackNr < _trackCount; trackNr++)
                {
                    if (_tracks[trackNr].digitalChannelNr != -1)
                        return _tracks[trackNr];
                }
                return null;
            }
        }

        public byte SoundPriority { get; }

        public SoundResource(uint resourceNr, ResourceManager resMan, SciVersion soundVersion)
        {
            _resMan = resMan;
            _soundVersion = soundVersion;

            var resource = _resMan.FindResource(new ResourceId(ResourceType.Sound, (ushort) resourceNr), true);
            if (resource == null)
                return;

            _innerResource = resource;
            SoundPriority = 0xFF;

            ByteAccess data;
            Channel channel;

            switch (_soundVersion)
            {
                case SciVersion.V0_EARLY:
                case SciVersion.V0_LATE:
                    // SCI0 only has a header of 0x11/0x21 byte length and the actual midi track follows afterwards
                    _trackCount = 1;
                    _tracks = new Track[_trackCount];
                    for (var i = 0; i < _tracks.Length; i++)
                    {
                        _tracks[i] = new Track();
                    }
                    _tracks[0].digitalChannelNr = -1;
                    _tracks[0].type = 0; // Not used for SCI0
                    _tracks[0].channelCount = 1;
                    // Digital sample data included? . Add an additional channel
                    if (resource.data[0] == 2)
                        _tracks[0].channelCount++;
                    _tracks[0].channels = new Channel[_tracks[0].channelCount];
                    for (var i = 0; i < _tracks[0].channels.Length; i++)
                    {
                        _tracks[0].channels[i] = new Channel();
                    }
                    channel = _tracks[0].channels[0];
                    channel.flags |= 2; // don't remap (SCI0 doesn't have remapping)
                    if (_soundVersion == SciVersion.V0_EARLY)
                    {
                        channel.data = new ByteAccess(resource.data, 0x11);
                        channel.size = (ushort) (resource.size - 0x11);
                    }
                    else
                    {
                        channel.data = new ByteAccess(resource.data, 0x21);
                        channel.size = (ushort) (resource.size - 0x21);
                    }
                    if (_tracks[0].channelCount == 2)
                    {
                        // Digital sample data included
                        _tracks[0].digitalChannelNr = 1;
                        var sampleChannel = _tracks[0].channels[1];
                        // we need to find 0xFC (channel terminator) within the data
                        data = new ByteAccess(channel.data);
                        var dataEnd = new ByteAccess(channel.data, channel.size);
                        while ((data.Offset < dataEnd.Offset) && (data[0] != 0xfc))
                            data.Offset++;
                        // Skip any following 0xFCs as well
                        while ((data.Offset < dataEnd.Offset) && (data[0] == 0xfc))
                            data.Offset++;
                        // Now adjust channels accordingly
                        sampleChannel.data = data;
                        sampleChannel.size = (ushort) (channel.size - (data.Offset - channel.data.Offset));
                        channel.size = (ushort) (data.Offset - channel.data.Offset);
                        // Read sample header information
                        //Offset 14 in the header contains the frequency as a short integer. Offset 32 contains the sample length, also as a short integer.
                        _tracks[0].digitalSampleRate = sampleChannel.data.ToUInt16(14);
                        _tracks[0].digitalSampleSize = sampleChannel.data.ToUInt16(32);
                        _tracks[0].digitalSampleStart = 0;
                        _tracks[0].digitalSampleEnd = 0;
                        sampleChannel.data.Offset += 44; // Skip over header
                        sampleChannel.size -= 44;
                    }
                    break;

                case SciVersion.V1_EARLY:
                case SciVersion.V1_LATE:
                case SciVersion.V2_1_EARLY:
                    data = new ByteAccess(resource.data);
                    // Count # of tracks
                    _trackCount = 0;
                    while ((data.Increment()) != 0xFF)
                    {
                        _trackCount++;
                        while (data.Value != 0xFF)
                            data.Offset += 6;
                        data.Offset++;
                    }
                    _tracks = new Track[_trackCount];
                    for (var i = 0; i < _tracks.Length; i++)
                    {
                        _tracks[i] = new Track();
                    }
                    data = new ByteAccess(resource.data);

                    int trackNr;
                    for (trackNr = 0; trackNr < _trackCount; trackNr++)
                    {
                        // Track info starts with track type:BYTE
                        // Then the channel information gets appended Unknown:WORD, ChannelOffset:WORD, ChannelSize:WORD
                        // 0xFF:BYTE as terminator to end that track and begin with another track type
                        // Track type 0xFF is the marker signifying the end of the tracks

                        _tracks[trackNr].type = data.Increment();
                        // Counting # of channels used
                        var data2 = new ByteAccess(data);
                        byte channelCount = 0;
                        while (data2.Value != 0xFF)
                        {
                            data2.Offset += 6;
                            channelCount++;
                            _tracks[trackNr].channelCount++;
                        }
                        _tracks[trackNr].channels = new Channel[channelCount];
                        for (var i = 0; i < _tracks[trackNr].channels.Length; i++)
                        {
                            _tracks[trackNr].channels[i] = new Channel();
                        }
                        _tracks[trackNr].channelCount = 0;
                        _tracks[trackNr].digitalChannelNr = -1; // No digital sound associated
                        _tracks[trackNr].digitalSampleRate = 0;
                        _tracks[trackNr].digitalSampleSize = 0;
                        _tracks[trackNr].digitalSampleStart = 0;
                        _tracks[trackNr].digitalSampleEnd = 0;
                        if (_tracks[trackNr].type != 0xF0)
                        {
                            // Digital track marker - not supported currently
                            var channelNr = 0;
                            while ((channelCount--) != 0)
                            {
                                channel = _tracks[trackNr].channels[channelNr];
                                uint dataOffset = data.ToUInt16(2);

                                if (dataOffset >= resource.size)
                                {
                                    Warning(
                                        $"Invalid offset inside sound resource {resourceNr}: track {trackNr}, channel {channelNr}");
                                    data.Offset += 6;
                                    continue;
                                }

                                channel.data = new ByteAccess(resource.data, (int) dataOffset);
                                channel.size = data.ToUInt16(4);
                                channel.curPos = 0;
                                channel.number = channel.data[0];

                                channel.poly = (byte) (channel.data[1] & 0x0F);
                                channel.prio = (ushort) (channel.data[1] >> 4);
                                channel.time = channel.prev = 0;
                                channel.data.Offset += 2; // skip over header
                                channel.size -= 2; // remove header size
                                if (channel.number == 0xFE)
                                {
                                    // Digital channel
                                    _tracks[trackNr].digitalChannelNr = (short) channelNr;
                                    _tracks[trackNr].digitalSampleRate = channel.data.ToUInt16();
                                    _tracks[trackNr].digitalSampleSize = channel.data.ToUInt16(2);
                                    _tracks[trackNr].digitalSampleStart = channel.data.ToUInt16(4);
                                    _tracks[trackNr].digitalSampleEnd = channel.data.ToUInt16(6);
                                    channel.data.Offset += 8; // Skip over header
                                    channel.size -= 8;
                                    channel.flags = 0;
                                }
                                else
                                {
                                    channel.flags = (byte) (channel.number >> 4);
                                    channel.number = (byte) (channel.number & 0x0F);

                                    // 0x20 is set on rhythm channels to prevent remapping
                                    // CHECKME: Which SCI versions need that set manually?
                                    if (channel.number == 9)
                                        channel.flags |= 2;
                                    // Note: flag 1: channel start offset is 0 instead of 10
                                    //               (currently: everything 0)
                                    //               also: don't map the channel to device
                                    //       flag 2: don't remap
                                    //       flag 4: start muted
                                    // QfG2 lacks flags 2 and 4, and uses (flags >= 1) as
                                    // the condition for starting offset 0, without the "don't map"
                                }
                                _tracks[trackNr].channelCount++;
                                channelNr++;
                                data.Offset += 6;
                            }
                        }
                        else
                        {
                            // The first byte of the 0xF0 track's channel list is priority
                            SoundPriority = data.Value;

                            // Skip over digital track
                            data.Offset += 6;
                        }
                        data.Offset++; // Skipping 0xFF that closes channels list
                    }
                    break;

                default:
                    throw new InvalidOperationException("SoundResource: SCI version {_soundVersion} is unsupported");
            }
        }

        public Track GetTrackByType(byte type)
        {
            if (_soundVersion <= SciVersion.V0_LATE)
                return _tracks[0];

            for (var trackNr = 0; trackNr < _tracks.Length; trackNr++)
            {
                if (_tracks[trackNr].type == type)
                    return _tracks[trackNr];
            }
            return null;
        }

        // Gets the filter mask for SCI0 sound resources
        public int GetChannelFilterMask(int hardwareMask, bool wantsRhythm)
        {
            var data = new ByteAccess(_innerResource.data);
            var channelMask = 0;

            if (_soundVersion > SciVersion.V0_LATE)
                return 0;

            data.Offset++; // Skip over digital sample flag

            for (var channelNr = 0; channelNr < 16; channelNr++)
            {
                channelMask = channelMask >> 1;

                byte flags;

                if (_soundVersion == SciVersion.V0_EARLY)
                {
                    // Each channel is specified by a single byte
                    // Upper 4 bits of the byte is a voices count
                    // Lower 4 bits . bit 0 set: use for AdLib
                    //				   bit 1 set: use for PCjr
                    //				   bit 2 set: use for PC speaker
                    //				   bit 3 set and bit 0 clear: control channel (15)
                    //				   bit 3 set and bit 0 set: rhythm channel (9)
                    // Note: control channel is dynamically assigned inside the drivers,
                    // but seems to be fixed at 15 in the song data.
                    flags = data.Increment();

                    // Get device bits
                    flags &= 0x7;
                }
                else
                {
                    // Each channel is specified by 2 bytes
                    // 1st byte is voices count
                    // 2nd byte is play mask, which specifies if the channel is supposed to be played
                    // by the corresponding hardware

                    // Skip voice count
                    data.Offset++;

                    flags = data.Increment();
                }

                bool play;
                switch (channelNr)
                {
                    case 15:
                        // Always play control channel
                        play = true;
                        break;
                    case 9:
                        // Play rhythm channel when requested
                        play = wantsRhythm;
                        break;
                    default:
                        // Otherwise check for flag
                        play = (flags & hardwareMask) != 0;
                        break;
                }

                if (play)
                {
                    // This Channel is supposed to be played by the hardware
                    channelMask |= 0x8000;
                }
            }

            return channelMask;
        }

        public byte GetInitialVoiceCount(int channel)
        {
            var data = new ByteAccess(_innerResource.data);

            if (_soundVersion > SciVersion.V0_LATE)
                return 0; // TODO

            data.Offset++; // Skip over digital sample flag

            if (_soundVersion == SciVersion.V0_EARLY)
                return (byte) (data[channel] >> 4);
            return data[channel * 2];
        }
    }
}