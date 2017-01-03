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
using System;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NScumm.Sky
{
    class SkyGameDescriptor : IGameDescriptor
    {
        private readonly Language _language;
        private string _path;

        public string Description
        {
            get
            {
                return "Beneath a Steel Sky";
            }
        }

        public string Id
        {
            get
            {
                return "sky";
            }
        }

        public Language Language
        {
            get
            {
                return _language;
            }
        }

        public Platform Platform
        {
            get
            {
                return Platform.Unknown;
            }
        }

        public int Width
        {
            get
            {
                return 320;
            }
        }

        public int Height
        {
            get
            {
                return 200;
            }
        }

        public PixelFormat PixelFormat
        {
            get
            {
                return PixelFormat.Indexed8;
            }
        }

        public string Path
        {
            get
            {
                return _path;
            }
        }

        public SkyGameDescriptor(string path)
        {
            _path = path;
            // The game detector uses US English by default. We want British
            // English to match the recorded voices better.
            _language = Language.UNK_LANG;
        }
    }

    public class SkyMetaEngine : MetaEngine
    {
        public override string OriginalCopyright
        {
            get
            {
                return "Beneath a Steel Sky (C) Revolution";
            }
        }

        public override IEngine Create(GameSettings settings, ISystem system)
        {
            return new SkyEngine(settings, system);
        }

        public override GameDetected DetectGame(string path)
        {
			if (ServiceLocator.FileStorage.DirectoryExists(path))
			{
				var skyFile = ServiceLocator.FileStorage.Combine(path, "sky.dnr");
				if (ServiceLocator.FileStorage.FileExists(skyFile))
					path = skyFile;
			}
            var fileName = ServiceLocator.FileStorage.GetFileName(path);
            if (string.Equals(fileName, "sky.dnr", StringComparison.OrdinalIgnoreCase))
            {
                var directory = ServiceLocator.FileStorage.GetDirectoryName(path);
                using (var disk = new Disk(directory))
                {
                    var version = disk.DetermineGameVersion();
                    return new GameDetected(new SkyGameDescriptor(path), this);
                }
            }
            return null;
        }

        public override IList<SaveStateDescriptor> ListSaves(string target)
        {
            var saveFileMan = ServiceLocator.SaveFileManager;
            var saveList = new List<SaveStateDescriptor>();

            // Load the descriptions
            var savenames = new string[Control.MaxSaveGames];
            using (var inf = saveFileMan.OpenForLoading("SKY-VM.SAV"))
            {
                var br = new BinaryReader(inf);
                for (int i = 0; i < Control.MaxSaveGames; ++i)
                {
                    savenames[i] = br.ReadBytes(Control.MaxTextLen).GetRawText();
                }
            }

            // Find all saves
            var filenames = saveFileMan.ListSavefiles("SKY-VM.???");
            Array.Sort(filenames);   // Sort (hopefully ensuring we are sorted numerically..)

            // Slot 0 is the autosave, if it exists.
            // TODO: Check for the existence of the autosave -- but this require us
            // to know which SKY variant we are looking at.
            saveList.Insert(0, new SaveStateDescriptor(0, "*AUTOSAVE*"));

            // Prepare the list of savestates by looping over all matching savefiles
            foreach (var file in filenames)
            {
                // Extract the extension
                var ext = file.Substring(file.Length - 4, 3);
                if (char.IsDigit(ext[0]) && char.IsDigit(ext[1]) && char.IsDigit(ext[2]))
                {
                    int slotNum = int.Parse(ext);
                    using (var @in = saveFileMan.OpenForLoading(file))
                    {
                        saveList.Add(new SaveStateDescriptor(slotNum + 1, savenames[slotNum]));
                    }
                }
            }

            return saveList;
        }

        public override void RemoveSaveState(string target, int slot)
        {
            if (slot == 0)  // do not delete the auto save
                return;

            var saveFileMan = ServiceLocator.SaveFileManager;
            var fName = $"SKY-VM.{slot - 1}";
            saveFileMan.RemoveSavefile(fName);

            // Load current save game descriptions
            var savenames = new string[Control.MaxSaveGames];
            using (var inf = saveFileMan.OpenForLoading("SKY-VM.SAV"))
            {
                var br = new BinaryReader(inf);
                for (int i = 0; i < Control.MaxSaveGames; ++i)
                {
                    savenames[i] = br.ReadBytes(Control.MaxTextLen).GetRawText();
                }
            }
            // Update the save game description at the given slot
            savenames[slot - 1] = string.Empty;

            // Save the updated descriptions
            using (var outf = saveFileMan.OpenForSaving("SKY-VM.SAV"))
            {
                var bw = new BinaryWriter(outf);
                for (ushort cnt = 0; cnt < Control.MaxSaveGames; cnt++)
                {
                    var tmp = savenames[cnt].ToCharArray().Select(c => (byte)c).ToArray();
                    bw.WriteBytes(tmp, tmp.Length);
                    var len = Control.MaxTextLen + 1 - tmp.Length;
                    if (len > 0)
                    {
                        bw.WriteBytes(new byte[len], len);
                    }
                }
            }
        }

        public override bool HasFeature(MetaEngineFeature f)
        {
            return
                (f == MetaEngineFeature.SupportsListSaves) ||
                (f == MetaEngineFeature.SupportsLoadingDuringStartup) ||
                (f == MetaEngineFeature.SupportsDeleteSave);
        }
    }
}
