using System;
using System.Collections.Generic;
using System.IO;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Scumm.IO
{
    public class ScummMetaEngine : MetaEngine
    {
        public override string OriginalCopyright => "LucasArts SCUMM Games (C) LucasArts\n"
                                                    + "Humongous SCUMM Games (C) Humongous";

        private readonly GameManager _gm;

        public ScummMetaEngine()
            : this(ServiceLocator.FileStorage.OpenContent("Nscumm.xml"))
        {
        }

        public ScummMetaEngine(Stream stream)
        {
            _gm = GameManager.Create(stream);
        }

        public override IEngine Create(GameSettings settings, ISystem system)
        {
            return ScummEngine.Create(settings, system);
        }

        public override GameDetected DetectGame(string path)
        {
            return new GameDetected(_gm.GetInfo(path), this);
        }

        public override IList<SaveStateDescriptor> ListSaves(string target)
        {
            var saveFileMan = ServiceLocator.SaveFileManager;
            var pattern = target + ".s??";
            string saveDesc;

            var filenames = saveFileMan.ListSavefiles(pattern);
            Array.Sort(filenames);   // Sort (hopefully ensuring we are sorted numerically..)

            var saveList = new List<SaveStateDescriptor>();
            foreach (var file in filenames)
            {
                // Obtain the last 2 digits of the filename, since they correspond to the save slot
                int slotNum = int.Parse(file.Substring(file.Length - 3, 2));

                if (slotNum >= 0 && slotNum <= 99)
                {
                    using (var @in = saveFileMan.OpenForLoading(file))
                    {
                        GetSavegameName(@in, out saveDesc, 0);    // FIXME: heversion?!?
                        saveList.Add(new SaveStateDescriptor(slotNum, saveDesc));
                    }
                }
            }
            return saveList;
        }

        public override bool HasFeature(MetaEngineFeature f)
        {
            return (f == MetaEngineFeature.SupportsListSaves) ||
                    (f == MetaEngineFeature.SupportsLoadingDuringStartup) ||
                    (f == MetaEngineFeature.SupportsDeleteSave) ||
                    (f == MetaEngineFeature.SavesSupportMetaInfo) ||
                    (f == MetaEngineFeature.SavesSupportThumbnail) ||
                    (f == MetaEngineFeature.SavesSupportCreationDate) ||
                    (f == MetaEngineFeature.SavesSupportPlayTime);
        }

        private bool GetSavegameName(Stream @in, out string desc, int heversion)
        {
            SaveGameHeader hdr;

            if (!LoadAndCheckSaveGameHeader(@in, heversion, out hdr, out desc))
            {
                return false;
            }

            desc = hdr.Name;
            return true;
        }

        public override void RemoveSaveState(string target, int slot)
        {
            var filename = MakeSavegameName(target, slot, false);
            ServiceLocator.SaveFileManager.RemoveSavefile(filename);
        }

        private string MakeSavegameName(string target, int slot, bool temporary)
        {
            var tmp = temporary ? 'c' : 's';
            return $"{target}.{tmp}{slot:D2}";
        }

        private bool LoadAndCheckSaveGameHeader(Stream @in, int heversion, out SaveGameHeader hdr, out string error)
        {
            error = null;
            if (!LoadSaveGameHeader(@in, out hdr))
            {
                error = "Invalid savegame";
                return false;
            }

            if (hdr.Version < 7 || hdr.Version > ScummEngine.CurrentVersion)
            {
                error = "Invalid version";
                return false;
            }

            // We (deliberately) broke HE savegame compatibility at some point.
            if (hdr.Version < 57 && heversion >= 60)
            {
                error = "Unsupported version";
                return false;
            }

            return true;
        }

        private static bool LoadSaveGameHeader(Stream @in, out SaveGameHeader hdr)
        {
            var br = new BinaryReader(@in);
            hdr = new SaveGameHeader();
            hdr.Type = br.ReadUInt32BigEndian();
            hdr.Size = br.ReadUInt32();
            hdr.Version = br.ReadUInt32();
            hdr.Name = br.ReadBytes(32).GetRawText();
            return hdr.Type == ScummHelper.MakeTag('S', 'C', 'V', 'M');
        }

    }
}