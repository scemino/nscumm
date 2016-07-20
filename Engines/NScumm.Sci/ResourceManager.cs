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
    struct ResourceIndex
    {
        public ushort wOffset;
        public ushort wSize;
    }

    enum ResourceCompression
    {
        Unknown = -1,
        None = 0,
        LZW,
        Huffman,
        LZW1,          // LZW-like compression used in SCI01 and SCI1
        LZW1View,      // Comp3 + view Post-processing
        LZW1Pic,       // Comp3 + pic Post-processing
#if ENABLE_SCI32
        STACpack,  // ? Used in SCI32
#endif
        DCL
    }

    /// <summary>
    /// SCI versions
    /// For more information, check here:
    /// http://wiki.scummvm.org/index.php/Sierra_Game_Versions#SCI_Games
    /// </summary>
    enum SciVersion
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
        V2_1, // GK2, KQ7, LSL6 hires, MUMG Deluxe, Phantasmagoria 1, PQ4CD, PQ:SWAT, QFG4CD, Shivers 1, SQ6, Torin
        V3 // LSL7, Lighthouse, RAMA, Phantasmagoria 2
    }

    enum ResVersion
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
    enum ViewType
    {
        Unknown,   // uninitialized, or non-SCI
        Ega,       // EGA SCI0/SCI1 and Amiga SCI0/SCI1 ECS 16 colors
        Amiga,     // Amiga SCI1 ECS 32 colors
        Amiga64,   // Amiga SCI1 AGA 64 colors (i.e. Longbow)
        Vga,       // VGA SCI1 256 colors
        Vga11      // VGA SCI1.1 and newer 256 colors
    }

    enum ResourceType
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
        CdAudio,
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
        MacPict,        // PICT resources (inventory)

        Rave,  // KQ6 hires RAVE (special sync) resources

        Invalid
    }

    /** Resource error codes. Should be in sync with s_errorDescriptions */
    enum ResourceErrorCodes
    {
        NONE = 0,
        IO_ERROR = 1,
        EMPTY_RESOURCE = 2,
        RESMAP_INVALID_ENTRY = 3, /**< Invalid resource.map entry */
        RESMAP_NOT_FOUND = 4,
        NO_RESOURCE_FILES_FOUND = 5,  /**< No resource at all was found */
        UNKNOWN_COMPRESSION = 6,
        DECOMPRESSION_ERROR = 7,  /**< sanity checks failed during decompression */
        RESOURCE_TOO_BIG = 8  /**< Resource size exceeds SCI_MAX_RESOURCE_SIZE */
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
        private string _directory;
        private List<ResourceSource> _sources;
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
        private static SciVersion s_sciVersion = SciVersion.NONE;   // FIXME: Move this inside a suitable class, e.g. SciEngine
        /// <summary>
        /// List of opened volume files
        /// </summary>
        private List<Stream> _volumeFiles;

        // Resource type suffixes. Note that the
        // suffix of SCI3 scripts has been changed from
        // scr to csc
        private static readonly string[] s_resourceTypeSuffixes =
        {
            "v56", "p56", "scr", "tex", "snd",
               "", "voc", "fon", "cur", "pat",
            "bit", "pal", "cda", "aud", "syn",
            "msg", "map", "hep",    "",    "",
            "trn", "rbt", "vmd", "chk",    "",
            "etc", "duk", "clu", "tga", "zzz",
               "",    "",    "", ""
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

        private static readonly ResourceType[] s_resTypeMapSci0 = {
            ResourceType.View, ResourceType.Pic, ResourceType.Script, ResourceType.Text,          // 0x00-0x03
	        ResourceType.Sound, ResourceType.Memory, ResourceType.Vocab, ResourceType.Font,       // 0x04-0x07
	        ResourceType.Cursor, ResourceType.Patch, ResourceType.Bitmap, ResourceType.Palette,   // 0x08-0x0B
	        ResourceType.CdAudio, ResourceType.Audio, ResourceType.Sync, ResourceType.Message,    // 0x0C-0x0F
	        ResourceType.Map, ResourceType.Heap, ResourceType.Audio36, ResourceType.Sync36,       // 0x10-0x13
	        ResourceType.Translation, ResourceType.Rave                                           // 0x14
        };

        // TODO: 12 should be "Wave", but SCI seems to just store it in Audio resources
        private static readonly ResourceType[] s_resTypeMapSci21 = {
            ResourceType.View, ResourceType.Pic, ResourceType.Script, ResourceType.Animation,     // 0x00-0x03
	        ResourceType.Sound, ResourceType.Etc, ResourceType.Vocab, ResourceType.Font,          // 0x04-0x07
	        ResourceType.Cursor, ResourceType.Patch, ResourceType.Bitmap, ResourceType.Palette,   // 0x08-0x0B
	        ResourceType.Invalid, ResourceType.Audio, ResourceType.Sync, ResourceType.Message,    // 0x0C-0x0F
	        ResourceType.Map, ResourceType.Heap, ResourceType.Chunk, ResourceType.Audio36,        // 0x10-0x13
	        ResourceType.Sync36, ResourceType.Translation, ResourceType.Robot, ResourceType.VMD,  // 0x14-0x17
	        ResourceType.Duck, ResourceType.Clut, ResourceType.TGA, ResourceType.ZZZ              // 0x18-0x1B
        };

        public bool IsSci11Mac { get { return _volVersion == ResVersion.Sci11Mac; } }

        public ViewType ViewType { get { return _viewType; } }

        public ResourceManager(string directory)
        {
            _directory = directory;
            _sources = new List<ResourceSource>();
            _LRU = new List<ResourceSource.Resource>();
            _resMap = new ResourceMap();
            _volumeFiles = new List<Stream>();
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
                if ((SciEngine.Instance.GameId == SciGameId.LAURABOW2) && (!SciEngine.Instance.IsCD) && (!SciEngine.Instance.IsDemo))
                    return true;
                return false;
            }
            return false;
        }

        public int AddAppropriateSources()
        {
            var path = ScummHelper.LocatePath(_directory, "resource.map");
            if (path != null)
            {
                // SCI0-SCI2 file naming scheme
                ResourceSource map = AddExternalMap("resource.map");

                var files = ServiceLocator.FileStorage.EnumerateFiles(_directory, "resource.0??");
                foreach (var file in files)
                {
                    var name = ServiceLocator.FileStorage.GetFileName(file);
                    var number = int.Parse(name.Split('.')[1]);
                    AddSource(new VolumeResourceSource(name, map, number));
                }
#if ENABLE_SCI32
                // GK1CD hires content
                if (Common::File::exists("alt.map") && Common::File::exists("resource.alt"))
                    AddSource(new VolumeResourceSource("resource.alt", addExternalMap("alt.map", 10), 10));
#endif

            }
            else
            {
                throw new NotImplementedException();
            }

            AddPatchDir(".");

            path = ScummHelper.LocatePath(_directory, "message.map");
            if (path != null)
                AddSource(new VolumeResourceSource("resource.msg", AddExternalMap("message.map"), 0));

            path = ScummHelper.LocatePath(_directory, "altres.map");
            if (path != null)
                AddSource(new VolumeResourceSource("altres.000", AddExternalMap("altres.map"), 0));

            return 1;
        }

        public void Init()
        {
            _memoryLocked = 0;
            _memoryLRU = 0;
            _LRU.Clear();
            _resMap.Clear();
            _audioMapSCI1 = null;

            // FIXME: put this in an Init() function, so that we can error out if detection fails completely

            _mapVersion = DetectMapVersion();
            _volVersion = DetectVolVersion();

            // TODO/FIXME: Remove once SCI3 resource detection is finished
            if ((_mapVersion == ResVersion.Sci3 || _volVersion == ResVersion.Sci3) && (_mapVersion != _volVersion))
            {
                // warning("FIXME: Incomplete SCI3 detection: setting map and volume version to SCI3");
                _mapVersion = _volVersion = ResVersion.Sci3;
            }

            if ((_volVersion == ResVersion.Unknown) && (_mapVersion != ResVersion.Unknown))
            {
                // warning("Volume version not detected, but map version has been detected. Setting volume version to map version");
                _volVersion = _mapVersion;
            }

            if ((_mapVersion == ResVersion.Unknown) && (_volVersion != ResVersion.Unknown))
            {
                // warning("Map version not detected, but volume version has been detected. Setting map version to volume version");
                _mapVersion = _volVersion;
            }

            //debugC(1, kDebugLevelResMan, "resMan: Detected resource map version %d: %s", _mapVersion, versionDescription(_mapVersion));
            //debugC(1, kDebugLevelResMan, "resMan: Detected volume version %d: %s", _volVersion, versionDescription(_volVersion));

            if ((_mapVersion == ResVersion.Unknown) && (_volVersion == ResVersion.Unknown))
            {
                // warning("Volume and map version not detected, assuming that this is not a SCI game");
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

            // TODO: debug
            // debugC(1, kDebugLevelResMan, "resMan: Detected %s", getSciVersionDesc(getSciVersion()));

            switch (_viewType)
            {
                case ViewType.Ega:
                    //debugC(1, kDebugLevelResMan, "resMan: Detected EGA graphic resources");
                    break;
                case ViewType.Amiga:
                    //debugC(1, kDebugLevelResMan, "resMan: Detected Amiga ECS graphic resources");
                    break;
                case ViewType.Amiga64:
                    //debugC(1, kDebugLevelResMan, "resMan: Detected Amiga AGA graphic resources");
                    break;
                case ViewType.Vga:
                    //debugC(1, kDebugLevelResMan, "resMan: Detected VGA graphic resources");
                    break;
                case ViewType.Vga11:
                    //debugC(1, kDebugLevelResMan, "resMan: Detected SCI1.1 VGA graphic resources");
                    break;
                default:
#if ENABLE_SCI32
                    error("resMan: Couldn't determine view type");
#else
                    if (GetSciVersion() >= SciVersion.V2)
                    {
                        // SCI support isn't built in, thus the view type won't be determined for
                        // SCI2+ games. This will be handled further up, so throw no error here
                    }
                    else
                    {
                        throw new InvalidOperationException("resMan: Couldn't determine view type");
                    }
                    break;
#endif
            }
        }

        public int GetAudioLanguage()
        {
            return (_audioMapSCI1 != null ? _audioMapSCI1._volumeNumber : 0);
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
                default:
                    break;
            }

            if (gmPatchFile != null && ScummHelper.LocatePath(_directory, gmPatchFile) != null)
            {
                throw new NotImplementedException();
                //ResourceSource psrcPatch = new PatchResourceSource(gmPatchFile);
                //ProcessPatch(psrcPatch, ResourceType.Patch, 4);
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
                var bufOffset = (GetSciVersion() == SciVersion.V0_EARLY) ? 2 : 0;

                // Check if the first block is the exports block (in most cases, it is)
                bool exportsIsFirst = (buf.ToUInt16(bufOffset + 4) == 7);
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

                ushort offset = !IsSci11Mac ? buf.ToUInt16(offsetPtr) : buf.ToUInt16BigEndian(offsetPtr);
                return Register.Make(1, offset);
            }
            else if (GetSciVersion() >= SciVersion.V1_1 && GetSciVersion() <= SciVersion.V2_1)
            {
                var buf = script.data;
                offsetPtr = 4 + 2 + 2;

                // In SCI1.1 - SCI2.1, the heap is appended at the end of the script,
                // so adjust the offset accordingly if requested
                ushort offset = !IsSci11Mac ? buf.ToUInt16(offsetPtr) : buf.ToUInt16BigEndian(offsetPtr);
                if (addSci11ScriptOffset)
                {
                    offset = (ushort)(offset + script.size);

                    // Ensure that the start of the heap is word-aligned - same as in Script::init()
                    if ((script.size & 2) != 0)
                        offset++;
                }

                return Register.Make(1, offset);
            }
            else
            {
                return Register.Make(1, (ushort)RelocateOffsetSci3(script.data, 22));
            }
        }

        public ResourceSource.Resource FindResource(ResourceId id, bool locked)
        {
            var retval = TestResource(id);

            if (retval == null)
                return null;

            if (retval._status == ResourceStatus.NoMalloc)
                LoadResource(retval);
            else if (retval._status == ResourceStatus.Enqueued)
                RemoveFromLRU(retval);
            // Unless an error occurred, the resource is now either
            // locked or allocated, but never queued or freed.

            FreeOldResources();

            if (locked)
            {
                if (retval._status == ResourceStatus.Allocated)
                {
                    retval._status = ResourceStatus.Locked;
                    retval._lockers = 0;
                    _memoryLocked += (int)retval.size;
                }
                retval._lockers++;
            }
            else if (retval._status != ResourceStatus.Locked)
            { // Don't lock it
                if (retval._status == ResourceStatus.Allocated)
                    AddToLRU(retval);
            }

            if (retval.data != null)
                return retval;
            else
            {
                Warning($"resMan: Failed to read {retval._id}");
                return null;
            }
        }

        /// <summary>
        /// Unlocks a previously locked resource.
        /// </summary>
        /// <param name="res">The resource to free</param>
        public void UnlockResource(ResourceSource.Resource res)
        {
            if (res._status != ResourceStatus.Locked)
            {
                // TODO: debugC(kDebugLevelResMan, 2, "[resMan] Attempt to unlock unlocked resource %s", res._id.toString().c_str());
                return;
            }

            if (--res._lockers == 0)
            { // No more lockers?
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
            for (int i = 0; i < 32768; i++)
            {
                Resource* res = findResource(ResourceId(kResourceTypePic, i), 0);

                if (res)
                {
                    if (READ_SCI11ENDIAN_UINT16(res.data) == 0x0e)
                    {
                        // SCI32 picture
                        uint16 width = READ_SCI11ENDIAN_UINT16(res.data + 10);
                        uint16 height = READ_SCI11ENDIAN_UINT16(res.data + 12);
                        // Surely lowres (e.g. QFG4CD)
                        if ((width == 320) && ((height == 190) || (height == 200)))
                            return false;
                        // Surely hires
                        if ((width >= 600) || (height >= 400))
                            return true;
                    }
                }
            }

            // We haven't been able to find hires content

            return false;
#else
            throw new NotImplementedException("no sci32 support");
#endif
        }

        public bool DetectFontExtended()
        {
            var res = FindResource(new ResourceId(ResourceType.Font, 0), false);
            if (res != null)
            {
                if (res.size >= 4)
                {
                    var numChars = res.data.ToUInt16(2);
                    if (numChars > 0x80)
                        return true;
                }
            }
            return false;
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
            bool oldScriptHeader = (GetSciVersion() == SciVersion.V0_EARLY);

            if (oldScriptHeader)
                buf += 2;

            do
            {
                int seekerType = buffer.ToUInt16(buf);

                if (seekerType == 0)
                    break;
                if (seekerType == 7)    // exports
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
            int relocStart = buf.ToInt32(8);
            int relocCount = buf.ToUInt16(18);
            var seeker = relocStart;

            for (int i = 0; i < relocCount; ++i)
            {
                if (buf.ReadSci11EndianUInt32(seeker) == offset)
                {
                    // TODO: Find out what UINT16 at (seeker + 8) means
                    return (int)(buf.ReadSci11EndianUInt16(offset) + buf.ReadSci11EndianUInt32(seeker + 4));
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

            List<string> files = new List<string>();

            for (var i = (int)ResourceType.View; i < (int)ResourceType.Invalid; ++i)
            {
                // Ignore the types that can't be patched (and Robot/VMD is handled externally for now)
                if (s_resourceTypeSuffixes[i] != null || (i >= (int)ResourceType.Robot && i != (int)ResourceType.Chunk))
                    continue;

                files.Clear();
                var szResType = GetResourceTypeName((ResourceType)i);
                // SCI0 naming - type.nnn
                var mask = szResType;
                mask += ".???";
                files.AddRange(ServiceLocator.FileStorage.EnumerateFiles(mask));
                // SCI1 and later naming - nnn.typ
                mask = "*.";
                mask += s_resourceTypeSuffixes[i];
                files.AddRange(ServiceLocator.FileStorage.EnumerateFiles(mask));

                if (i == (int)ResourceType.View)
                {
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.v16"));  // EGA SCI1 view patches
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.v32"));  // Amiga SCI1 view patches
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.v64"));  // Amiga AGA SCI1 (i.e. Longbow) view patches
                }
                else if (i == (int)ResourceType.Pic)
                {
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.p16"));  // EGA SCI1 picture patches
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.p32"));  // Amiga SCI1 picture patches
                    files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.p64"));  // Amiga AGA SCI1 (i.e. Longbow) picture patches
                }
                else if (i == (int)ResourceType.Script)
                {
                    if (files.Count == 0)
                        // SCI3 (we can't use getSciVersion() at this point)
                        files.AddRange(ServiceLocator.FileStorage.EnumerateFiles("*.csc"));
                }

                foreach (var x in files)
                {
                    bool bAdd = false;
                    var name = ServiceLocator.FileStorage.GetFileName(x);

                    throw new NotImplementedException();
                    //// SCI1 scheme
                    //if (char.IsDigit(name[0]))
                    //{
                    //    char* end = 0;
                    //    resourceNr = strtol(name.c_str(), &end, 10);
                    //    bAdd = (*end == '.'); // Ensure the next character is the period
                    //}
                    //else {
                    //    // SCI0 scheme
                    //    int resname_len = strlen(szResType);
                    //    if (scumm_strnicmp(name.c_str(), szResType, resname_len) == 0
                    //        && !Common::isAlpha(name[resname_len + 1]))
                    //    {
                    //        resourceNr = atoi(name.c_str() + resname_len + 1);
                    //        bAdd = true;
                    //    }
                    //}

                    //if (bAdd)
                    //{
                    //    psrcPatch = new PatchResourceSource(name);
                    //    processPatch(psrcPatch, (ResourceType)i, resourceNr);
                    //}
                }
            }
        }

        private string GetResourceTypeName(ResourceType restype)
        {
            if (restype != ResourceType.Invalid)
                return s_resourceTypeNames[(int)restype];
            else
                return "invalid";
        }

        private void ReadWaveAudioPatches()
        {
            // Here we do check for SCI1.1+ so we can patch wav files in as audio resources
            List<string> files = ServiceLocator.FileStorage.EnumerateFiles(_directory, "*.wav").ToList();

            foreach (var x in files)
            {
                var name = ServiceLocator.FileStorage.GetFileName(x);

                if (char.IsDigit(name[0]))
                    ProcessWavePatch(new ResourceId(ResourceType.Audio, ushort.Parse(name)), name);
            }
        }

        private void ProcessWavePatch(ResourceId resourceId, string name)
        {
            throw new NotImplementedException();
            //ResourceSource resSrc = new WaveResourceSource(name);
            //Common::File file;
            //file.open(name);

            //updateResource(resourceId, resSrc, file.size());
            //_sources.push_back(resSrc);

            //debugC(1, kDebugLevelResMan, "Patching %s - OK", name.c_str());
        }

        private void ReadResourcePatchesBase36()
        {
            // TODO: throw new NotImplementedException();
        }

        private ResourceErrorCodes ReadAudioMapSCI11(IntMapResourceSource map)
        {
#if !ENABLE_SCI32
            // SCI32 support is not built in. Check if this is a SCI32 game
            // and if it is abort here.
            if (_volVersion >= ResVersion.Sci2)
                return ResourceErrorCodes.RESMAP_NOT_FOUND;
#endif

            throw new InvalidOperationException();

            //uint offset = 0;
            //ResourceSource.Resource mapRes = FindResource(new ResourceId( ResourceType.Map, map._volumeNumber), false);

            //if (!mapRes)
            //{
            //    Warning($"Failed to open {map._volumeNumber}.MAP");
            //    return ResourceErrorCodes.RESMAP_NOT_FOUND;
            //}

            //ResourceSource src = FindVolume(map, 0);

            //if (src==null)
            //    return ResourceErrorCodes.NO_RESOURCE_FILES_FOUND;

            //byte[] ptr = mapRes.Data;

            //// Heuristic to detect entry size
            //uint entrySize = 0;
            //for (int i = mapRes.size - 1; i >= 0; --i)
            //{
            //    if (ptr[i] == 0xff)
            //        entrySize++;
            //    else
            //        break;
            //}

            //if (map._volumeNumber == 65535)
            //{
            //    while (ptr < mapRes.data + mapRes.size)
            //    {
            //        ushort n = READ_LE_UINT16(ptr);
            //        ptr += 2;

            //        if (n == 0xffff)
            //            break;

            //        if (entrySize == 6)
            //        {
            //            offset = READ_LE_UINT32(ptr);
            //            ptr += 4;
            //        }
            //        else {
            //            offset += READ_LE_UINT24(ptr);
            //            ptr += 3;
            //        }

            //        addResource(ResourceId(kResourceTypeAudio, n), src, offset);
            //    }
            //}
            //else if (map._volumeNumber == 0 && entrySize == 10 && ptr[3] == 0)
            //{
            //    // QFG3 demo format
            //    // ptr[3] would be 'seq' in the normal format and cannot possibly be 0
            //    while (ptr < mapRes.data + mapRes.size)
            //    {
            //        uint16 n = READ_BE_UINT16(ptr);
            //        ptr += 2;

            //        if (n == 0xffff)
            //            break;

            //        offset = READ_LE_UINT32(ptr);
            //        ptr += 4;
            //        uint32 size = READ_LE_UINT32(ptr);
            //        ptr += 4;

            //        addResource(ResourceId(kResourceTypeAudio, n), src, offset, size);
            //    }
            //}
            //else if (map._volumeNumber == 0 && entrySize == 8 && READ_LE_UINT16(ptr + 2) == 0xffff)
            //{
            //    // LB2 Floppy/Mother Goose SCI1.1 format
            //    Common::SeekableReadStream* stream = getVolumeFile(src);

            //    while (ptr < mapRes.data + mapRes.size)
            //    {
            //        uint16 n = READ_LE_UINT16(ptr);
            //        ptr += 4;

            //        if (n == 0xffff)
            //            break;

            //        offset = READ_LE_UINT32(ptr);
            //        ptr += 4;

            //        // The size is not stored in the map and the entries have no order.
            //        // We need to dig into the audio resource in the volume to get the size.
            //        stream.seek(offset + 1);
            //        byte headerSize = stream.readByte();
            //        assert(headerSize == 11 || headerSize == 12);

            //        stream.skip(5);
            //        uint32 size = stream.readUint32LE() + headerSize + 2;

            //        addResource(ResourceId(kResourceTypeAudio, n), src, offset, size);
            //    }
            //}
            //else {
            //    bool isEarly = (entrySize != 11);

            //    if (!isEarly)
            //    {
            //        offset = READ_LE_UINT32(ptr);
            //        ptr += 4;
            //    }

            //    while (ptr < mapRes.data + mapRes.size)
            //    {
            //        uint32 n = READ_BE_UINT32(ptr);
            //        int syncSize = 0;
            //        ptr += 4;

            //        if (n == 0xffffffff)
            //            break;

            //        if (isEarly)
            //        {
            //            offset = READ_LE_UINT32(ptr);
            //            ptr += 4;
            //        }
            //        else {
            //            offset += READ_LE_UINT24(ptr);
            //            ptr += 3;
            //        }

            //        if (isEarly || (n & 0x80))
            //        {
            //            syncSize = READ_LE_UINT16(ptr);
            //            ptr += 2;

            //            // FIXME: The sync36 resource seems to be two bytes too big in KQ6CD
            //            // (bytes taken from the RAVE resource right after it)
            //            if (syncSize > 0)
            //                addResource(ResourceId(kResourceTypeSync36, map._volumeNumber, n & 0xffffff3f), src, offset, syncSize);
            //        }

            //        if (n & 0x40)
            //        {
            //            // This seems to define the size of raw lipsync data (at least
            //            // in KQ6 CD Windows).
            //            int kq6HiresSyncSize = READ_LE_UINT16(ptr);
            //            ptr += 2;

            //            if (kq6HiresSyncSize > 0)
            //            {
            //                addResource(ResourceId(kResourceTypeRave, map._volumeNumber, n & 0xffffff3f), src, offset + syncSize, kq6HiresSyncSize);
            //                syncSize += kq6HiresSyncSize;
            //            }
            //        }

            //        addResource(ResourceId(kResourceTypeAudio36, map._volumeNumber, n & 0xffffff3f), src, offset + syncSize);
            //    }
            //}

            return 0;
        }

        private ResourceErrorCodes ReadResourceMapSCI1(ResourceSource map)
        {
            Stream fileStream = null;

            if (map._resourceFile != null)
            {
                throw new NotImplementedException();
                //fileStream = map._resourceFile.createReadStream();
                //if (fileStream == null)
                //    return ResourceErrorCodes.RESMAP_NOT_FOUND;
            }
            else
            {
                fileStream = Core.Engine.OpenFileRead(map.LocationName);
                if (fileStream == null)
                    return ResourceErrorCodes.RESMAP_NOT_FOUND;
            }

            var br = new BinaryReader(fileStream);
            ResourceIndex[] resMap=new ResourceIndex[32];
            byte type = 0, prevtype = 0;
            int nEntrySize = _mapVersion == ResVersion.Sci11 ? SCI11_RESMAP_ENTRIES_SIZE : SCI1_RESMAP_ENTRIES_SIZE;
            ResourceId resId;

            // Read resource type and offsets to resource offsets block from .MAP file
            // The last entry has type=0xFF (0x1F) and offset equals to map file length
            do
            {
                type = (byte)(br.ReadByte() & 0x1F);
                resMap[type].wOffset = br.ReadUInt16();
                if (fileStream.Position == fileStream.Length)
                    return ResourceErrorCodes.RESMAP_NOT_FOUND;

                resMap[prevtype].wSize = (ushort)((resMap[type].wOffset
                                          - resMap[prevtype].wOffset) / nEntrySize);
                prevtype = type;
            } while (type != 0x1F); // the last entry is FF

            // reading each type's offsets
            uint fileOffset = 0;
            for (type = 0; type < 32; type++)
            {
                if (resMap[type].wOffset == 0) // this resource does not exist in map
                    continue;
                fileStream.Seek(resMap[type].wOffset, SeekOrigin.Begin);
                for (int i = 0; i < resMap[type].wSize; i++)
                {
                    ushort number = br.ReadUInt16();
                    int volume_nr = 0;
                    if (_mapVersion == ResVersion.Sci11)
                    {
                        // offset stored in 3 bytes
                        fileOffset = br.ReadUInt16();
                        fileOffset = (uint)(fileOffset | br.ReadByte() << 16);
                        fileOffset <<= 1;
                    }
                    else
                    {
                        // offset/volume stored in 4 bytes
                        fileOffset = br.ReadUInt32();
                        if (_mapVersion < ResVersion.Sci11)
                        {
                            volume_nr = (int)(fileOffset >> 28); // most significant 4 bits
                            fileOffset &= 0x0FFFFFFF;     // least significant 28 bits
                        }
                        else
                        {
                            // in SCI32 it's a plain offset
                        }
                    }
                    resId = new ResourceId(ConvertResType(type), number);
                    // NOTE: We add the map's volume number here to the specified volume number
                    // for SCI2.1 and SCI3 maps that are not resmap.000. The resmap.* files' numbers
                    // need to be used in concurrence with the volume specified in the map to get
                    // the actual resource file.
                    int mapVolumeNr = volume_nr + map._volumeNumber;
                    ResourceSource source = FindVolume(map, mapVolumeNr);

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
                            resource._source = source;
                            resource._fileOffset = (int)fileOffset;
                            resource.size = 0;
                        }
                    }
                }
            }

            fileStream.Dispose();
            return 0;
        }

        private ResourceErrorCodes ReadResourceMapSCI0(ExtMapResourceSource map)
        {
            Stream fileStream = null;
            ResourceType type = ResourceType.Invalid;   // to silence a false positive in MSVC
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

            var br = new BinaryReader(fileStream);
            fileStream.Seek(0, SeekOrigin.Begin);

            byte bMask = (_mapVersion >= ResVersion.Sci1Middle) ? (byte)0xF0 : (byte)0xFC;
            byte bShift = (_mapVersion >= ResVersion.Sci1Middle) ? (byte)28 : (byte)26;

            do
            {
                // King's Quest 5 FM-Towns uses a 7 byte version of the SCI1 Middle map,
                // splitting the type from the id.
                if (_mapVersion == ResVersion.KQ5FMT)
                    type = ConvertResType(fileStream.ReadByte());

                id = br.ReadUInt16();
                offset = br.ReadUInt32();

                if (offset == 0xFFFFFFFF)
                    break;

                if (fileStream.Position == fileStream.Length)
                {
                    fileStream.Dispose();
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
                    number = (ushort)(id & 0x7FF);
                }

                var resId = new ResourceId(type, number);
                // adding a new resource
                if (_resMap.ContainsKey(resId) == false)
                {
                    var source = FindVolume(map, (int)(offset >> bShift));
                    if (source == null)
                    {
                        Warning($"Could not get volume for resource {id}, VolumeID {offset >> bShift}");
                        if (_mapVersion != _volVersion)
                        {
                            Warning($"Retrying with the detected volume version instead");
                            Warning($"Map version was: {_mapVersion}, retrying with: {_volVersion}");
                            _mapVersion = _volVersion;
                            bMask = (_mapVersion == ResVersion.Sci1Middle) ? (byte)0xF0 : (byte)0xFC;
                            bShift = (_mapVersion == ResVersion.Sci1Middle) ? (byte)28 : (byte)26;
                            source = FindVolume(map, (int)(offset >> bShift));
                        }
                    }

                    AddResource(resId, source, (uint)(offset & (((~bMask) << 24) | 0xFFFFFF)));
                }
            } while (fileStream.Position < fileStream.Length);

            fileStream.Dispose();
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
                res._fileOffset = (int)offset;
                res.size = size;
            }
        }

        /// <summary>
        /// Converts a map resource type to our type
        /// </summary>
        /// <param name="v">The type from the map/patch</param>
        /// <returns>The ResourceType</returns>
        public ResourceType ConvertResType(int type)
        {
            type &= 0x7f;

            bool forceSci0 = false;

            // LSL6 hires doesn't have the chunk resource type, to match
            // the resource types of the lowres version, thus we use the
            // older resource types here.
            // PQ4 CD and QFG4 CD are SCI2.1, but use the resource types of the
            // corresponding SCI2 floppy disk versions.
            if (SciEngine.Instance != null && (SciEngine.Instance.GameId == SciGameId.LSL6HIRES ||
                    SciEngine.Instance.GameId == SciGameId.QFG4 || SciEngine.Instance.GameId == SciGameId.PQ4))
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
            int viewsTested = 0;

            // Test 10 views to see if any are compressed
            for (int i = 0; i < 1000; i++)
            {
                Stream fileStream = null;
                var res = TestResource(new ResourceId(ResourceType.View, (ushort)i));

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

                if (res.ReadResourceInfo(_volVersion, fileStream, out szPacked, out compression) != ResourceErrorCodes.NONE)
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
                throw new NotImplementedException();
                //return source_resourceFile.CreateReadStream();
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

            Stream newFile = null;
            // adding a new file
            path = ScummHelper.LocatePath(_directory, filename);
            if (path != null)
            {
                newFile = ServiceLocator.FileStorage.OpenFileRead(path);
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
            bool oldDecompressors = true;

            ResourceCompression viewCompression;
#if ENABLE_SCI32
            viewCompression = getViewCompression();
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
                _viewType = detectViewType();
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
                    s_sciVersion = SciVersion.V2_1;
                else
                    s_sciVersion = SciVersion.V1_1;
                return;
            }

            // Handle SCI32 versions here
            if (_volVersion >= ResVersion.Sci2)
            {
                List<ResourceId> heaps = ListResources(ResourceType.Heap);
                bool hasHeapResources = heaps.Count != 0;

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
                    s_sciVersion = SciVersion.V2_1;
                    return;
                }
                else
                {
                    s_sciVersion = SciVersion.V3;
                    return;
                }
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

                        // TODO: error("Failed to accurately determine SCI version");
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
                    // TODO: error("detectSciVersion(): Unable to detect the game's SCI version");
                    break;
            }
        }

        private bool HasSci1Voc900()
        {
            throw new NotImplementedException();
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

            ushort count = res.data.ToUInt16();

            // Make sure there's enough room for the pointers
            if (res.size < (uint)count * 2)
                return false;

            // Iterate over all pointers
            for (uint i = 0; i < count; i++)
            {
                // Offset to string
                ushort offset = res.data.ToUInt16(2 + count * 2);

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
                // TODO: error("resMan: Failed to find script.000");
                return false;
            }

            int offset = 2;
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
            for (int i = 0; i < 1000; i++)
            {
                var res = FindResource(new ResourceId(ResourceType.View, (ushort)i), false);
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
                            ushort offset = res.data.ToUInt16(8);

                            if (offset + 6U >= res.size)
                                return ViewType.Unknown;

                            // Read offset of first cel
                            offset = res.data.ToUInt16(offset + 4);

                            if (offset + 4U >= res.size)
                                return ViewType.Unknown;

                            // Check palette offset, amiga views have no palette
                            if (res.data.ToUInt16(6) != 0)
                                return ViewType.Ega;

                            ushort width = res.data.ToUInt16(offset);
                            offset += 2;
                            ushort height = res.data.ToUInt16(offset);
                            offset += 6;

                            // To improve the heuristic, we skip very small views
                            if (height < 10)
                                continue;

                            // Check that the RLE data stays within bounds
                            int y;
                            for (y = 0; y < height; y++)
                            {
                                int x = 0;

                                while ((x < width) && (offset < res.size))
                                {
                                    byte op = res.data[offset++];
                                    x += ((op & 0x07) != 0) ? op & 0x07 : op >> 3;
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
            _memoryLRU += (int)res.size;
#if SCI_VERBOSE_RESMAN
	debug("Adding %s.%03d (%d bytes) to lru control: %d bytes total",
	      getResourceTypeName(res.type), res.number, res.size,
	      mgr._memoryLRU);
#endif
            res._status = ResourceStatus.Enqueued;
        }

        private void FreeOldResources()
        {
            while (ResourceSource.Resource.MAX_MEMORY < _memoryLRU)
            {
                //Debug.Assert(_LRU.Count > 0);
                var goner = _LRU.Last();
                RemoveFromLRU(goner);
                goner.Unalloc();
#if SCI_VERBOSE_RESMAN
                debug("resMan-debug: LRU: Freeing %s.%03d (%d bytes)", getResourceTypeName(goner.type), goner.number, goner.size);
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
            _LRU.Remove(res);
            _memoryLRU -= (int)res.size;
            res._status = ResourceStatus.Allocated;
        }

        private void AddScriptChunkSources()
        {
#if ENABLE_SCI32
            if (_mapVersion >= kResVersionSci2)
            {
                // If we have no scripts, but chunk 0 is present, open up the chunk
                // to try to get to any scripts in there. The Lighthouse SCI2.1 demo
                // does exactly this.

                Common::List<ResourceId> resources = listResources(kResourceTypeScript);

                if (resources.empty() && testResource(ResourceId(kResourceTypeChunk, 0)))
                    addResourcesFromChunk(0);
            }
#endif
        }

        public static SciVersion GetSciVersion()
        {
            //Debug.Assert(s_sciVersion != SciVersion.NONE);
            return s_sciVersion;
        }

        private bool AddAudioSources()
        {
            var resources = ListResources(ResourceType.Map);
            foreach (var itr in resources)
            {
                var src = AddSource(new IntMapResourceSource("MAP", itr.Number));

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
            foreach (var source in _sources)
            {
                if (!source._scanned)
                {
                    source._scanned = true;
                    source.ScanSource(this);
                }
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

        private ResVersion DetectMapVersion()
        {
            Stream fileStream = null;
            byte[] buff = new byte[6];
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
                        var path = ScummHelper.LocatePath(_directory, source.LocationName);
                        var file = ServiceLocator.FileStorage.OpenFileRead(path);
                        fileStream = file;
                    }
                    break;
                }
                else if (source.SourceType == ResSourceType.MacResourceFork)
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
            uint uEnd = br.ReadUInt32();
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
            byte directoryType = 0;
            ushort directoryOffset = 0;
            ushort lastDirectoryOffset = 0;
            ushort directorySize = 0;
            ResVersion mapDetected = ResVersion.Unknown;
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
                    directorySize = (ushort)(directoryOffset - lastDirectoryOffset);
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
            ResourceSource rsrc;

            foreach (var it in _sources)
            {
                rsrc = it;
                if (rsrc.SourceType == ResSourceType.Volume)
                {
                    if (rsrc._resourceFile != null)
                    {
                        throw new NotImplementedException();
                        //fileStream = rsrc._resourceFile.CreateReadStream();
                    }
                    else
                    {
                        var path = ScummHelper.LocatePath(_directory, rsrc.LocationName);
                        if (path != null)
                        {
                            fileStream = ServiceLocator.FileStorage.OpenFileRead(path);
                        }
                    }
                    break;
                }
                else if (rsrc.SourceType == ResSourceType.MacResourceFork)
                    return ResVersion.Sci11Mac;
            }

            if (fileStream == null)
            {
                //warning("Failed to open volume file - if you got resource.p01/resource.p02/etc. files, merge them together into resource.000");
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
            ResVersion curVersion = ResVersion.Sci0Sci1Early;
            bool failed = false;
            bool sci11Align = false;

            // Check for SCI0, SCI1, SCI1.1, SCI32 v2 (Gabriel Knight 1 CD) and SCI32 v3 (LSL7) formats
            while (fileStream.Position < fileStream.Length && fileStream.Position < 0x100000)
            {
                if (curVersion > ResVersion.Sci0Sci1Early)
                    fileStream.ReadByte();
                fileStream.Seek(2, SeekOrigin.Current);    // resId
                dwPacked = (curVersion < ResVersion.Sci2) ? br.ReadUInt16() : br.ReadUInt32();
                dwUnpacked = (curVersion < ResVersion.Sci2) ? br.ReadUInt16() : br.ReadUInt32();

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

                int offs = curVersion < ResVersion.Sci11 ? 4 : 0;
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
                    fileStream.Seek((sci11Align && ((9 + dwPacked) % 2) != 0) ? dwPacked + 1 : dwPacked, SeekOrigin.Current);
                else if (curVersion >= ResVersion.Sci2)
                    fileStream.Seek(dwPacked, SeekOrigin.Current);
            }

            if (!failed)
                return curVersion;

            // Failed to detect volume version
            return ResVersion.Unknown;
        }

        private ResourceSource FindVolume(ResourceSource map, int volume_nr)
        {
            foreach (var it in _sources)
            {
                ResourceSource src = it.FindVolume(map, volume_nr);
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
            _sources.Add(newsrc);
            return newsrc;
        }

        /// <summary>
        /// Add an external (i.e., separate file) map resource to the resource
        /// manager's list of sources.
        /// </summary>
        /// <param name="filename">The name of the volume to add</param>
        /// <param name="volume_nr">The volume number the map starts at, 0 for &lt;SCI2.1</param>
        /// <returns>Added source structure, or null if an error occurred.</returns>
        private ResourceSource AddExternalMap(string filename, int volume_nr = 0)
        {
            ResourceSource newsrc = new ExtMapResourceSource(filename, volume_nr);
            _sources.Add(newsrc);
            return newsrc;
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
            public ByteAccess data;
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

        private SciVersion _soundVersion;
        private int _trackCount;
        private Track[] _tracks;
        private ResourceManager.ResourceSource.Resource _innerResource;
        private ResourceManager _resMan;
        private byte _soundPriority;

        public Track DigitalTrack
        {
            get
            {
                for (int trackNr = 0; trackNr < _trackCount; trackNr++)
                {
                    if (_tracks[trackNr].digitalChannelNr != -1)
                        return _tracks[trackNr];
                }
                return null;
            }
        }

        public byte SoundPriority { get { return _soundPriority; } }

        public SoundResource(uint resourceNr, ResourceManager resMan, SciVersion soundVersion)
        {
            _resMan = resMan;
            _soundVersion = soundVersion;

            var resource = _resMan.FindResource(new ResourceId(ResourceType.Sound, (ushort)resourceNr), true);
            int trackNr, channelNr;
            if (resource == null)
                return;

            _innerResource = resource;
            _soundPriority = 0xFF;

            ByteAccess data;
            ByteAccess data2;
            ByteAccess dataEnd;
            Channel channel, sampleChannel;

            switch (_soundVersion)
            {
                case SciVersion.V0_EARLY:
                case SciVersion.V0_LATE:
                    // SCI0 only has a header of 0x11/0x21 byte length and the actual midi track follows afterwards
                    _trackCount = 1;
                    _tracks = new Track[_trackCount];
                    for (int i = 0; i < _tracks.Length; i++)
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
                    for (int i = 0; i < _tracks[0].channels.Length; i++)
                    {
                        _tracks[0].channels[i] = new Channel();
                    }
                    channel = _tracks[0].channels[0];
                    channel.flags |= 2; // don't remap (SCI0 doesn't have remapping)
                    if (_soundVersion == SciVersion.V0_EARLY)
                    {
                        channel.data = new ByteAccess(resource.data, 0x11);
                        channel.size = (ushort)(resource.size - 0x11);
                    }
                    else
                    {
                        channel.data = new ByteAccess(resource.data, 0x21);
                        channel.size = (ushort)(resource.size - 0x21);
                    }
                    if (_tracks[0].channelCount == 2)
                    {
                        // Digital sample data included
                        _tracks[0].digitalChannelNr = 1;
                        sampleChannel = _tracks[0].channels[1];
                        // we need to find 0xFC (channel terminator) within the data
                        data = new ByteAccess(channel.data);
                        dataEnd = new ByteAccess(channel.data, channel.size);
                        while ((data.Offset < dataEnd.Offset) && (data[0] != 0xfc))
                            data.Offset++;
                        // Skip any following 0xFCs as well
                        while ((data.Offset < dataEnd.Offset) && (data[0] == 0xfc))
                            data.Offset++;
                        // Now adjust channels accordingly
                        sampleChannel.data = data;
                        sampleChannel.size = (ushort)(channel.size - (data.Offset - channel.data.Offset));
                        channel.size = (ushort)(data.Offset - channel.data.Offset);
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
                case SciVersion.V2_1:
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
                    data = new ByteAccess(resource.data);

                    byte channelCount;

                    for (trackNr = 0; trackNr < _trackCount; trackNr++)
                    {
                        // Track info starts with track type:BYTE
                        // Then the channel information gets appended Unknown:WORD, ChannelOffset:WORD, ChannelSize:WORD
                        // 0xFF:BYTE as terminator to end that track and begin with another track type
                        // Track type 0xFF is the marker signifying the end of the tracks

                        _tracks[trackNr].type = data.Increment();
                        // Counting # of channels used
                        data2 = new ByteAccess(data);
                        channelCount = 0;
                        while (data2.Value != 0xFF)
                        {
                            data2.Offset += 6;
                            channelCount++;
                            _tracks[trackNr].channelCount++;
                        }
                        _tracks[trackNr].channels = new Channel[channelCount];
                        _tracks[trackNr].channelCount = 0;
                        _tracks[trackNr].digitalChannelNr = -1; // No digital sound associated
                        _tracks[trackNr].digitalSampleRate = 0;
                        _tracks[trackNr].digitalSampleSize = 0;
                        _tracks[trackNr].digitalSampleStart = 0;
                        _tracks[trackNr].digitalSampleEnd = 0;
                        if (_tracks[trackNr].type != 0xF0)
                        { // Digital track marker - not supported currently
                            channelNr = 0;
                            while ((channelCount--) != 0)
                            {
                                channel = _tracks[trackNr].channels[channelNr];
                                uint dataOffset = data.ToUInt16(2);

                                if (dataOffset >= resource.size)
                                {
                                    Warning($"Invalid offset inside sound resource {resourceNr}: track {trackNr}, channel {channelNr}");
                                    data.Offset += 6;
                                    continue;
                                }

                                channel.data = new ByteAccess(resource.data, (int)dataOffset);
                                channel.size = data.ToUInt16(4);
                                channel.curPos = 0;
                                channel.number = channel.data[0];

                                channel.poly = (byte)(channel.data[1] & 0x0F);
                                channel.prio = (ushort)(channel.data[1] >> 4);
                                channel.time = channel.prev = 0;
                                channel.data.Offset += 2; // skip over header
                                channel.size -= 2; // remove header size
                                if (channel.number == 0xFE)
                                { // Digital channel
                                    _tracks[trackNr].digitalChannelNr = (short)channelNr;
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
                                    channel.flags = (byte)(channel.number >> 4);
                                    channel.number = (byte)(channel.number & 0x0F);

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
                            _soundPriority = data.Value;

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

            for (int trackNr = 0; trackNr < _tracks.Length; trackNr++)
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
            int channelMask = 0;

            if (_soundVersion > SciVersion.V0_LATE)
                return 0;

            data.Offset++; // Skip over digital sample flag

            for (int channelNr = 0; channelNr < 16; channelNr++)
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
            ByteAccess data = new ByteAccess(_innerResource.data);

            if (_soundVersion > SciVersion.V0_LATE)
                return 0; // TODO

            data.Offset++; // Skip over digital sample flag

            if (_soundVersion == SciVersion.V0_EARLY)
                return (byte)(data[channel] >> 4);
            else
                return data[channel * 2];
        }
    }
}