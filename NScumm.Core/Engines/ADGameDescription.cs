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
using NScumm.Core.Common;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace NScumm.Core.Engines
{
    /// <summary>
    /// A record describing a file to be matched for detecting a specific game
    /// variant. A list of such records is used inside every ADGameDescription to
    /// enable detection.
    /// </summary>
    public class ADGameFileDescription
    {
        /// <summary>
        /// Name of described file.
        /// </summary>
        public string fileName;
        /// <summary>
        /// Optional. Not used during detection, only by engines.
        /// </summary>
        public ushort fileType;
        /// <summary>
        /// MD5 of (the beginning of) the described file. Optional. Set to NULL to ignore.
        /// </summary>
        public string md5;
        /// <summary>
        /// Size of the described file.Set to -1 to ignore.
        /// </summary>
        public int fileSize;
    }

    public class ADGameDescription
    {
        public string gameid;
        public string extra;
        public ADGameFileDescription[] filesDescriptions = new ADGameFileDescription[14];
        public Language language;
        public Platform platform;

        /// <summary>
        /// A bitmask of extra flags. The top 16 bits are reserved for generic flags
        /// defined in the ADGameFlags. This leaves 16 bits to be used by client
        /// code.
        /// </summary>
        public uint flags;
    }

    class FileProperties
    {
        public string Md5 { get; private set; }
        public string Path { get; private set; }
        public string FileName { get; private set; }
        public int Size { get; private set; }

        public FileProperties(string path)
        {
            Path = path;
            FileName = ServiceLocator.FileStorage.GetFileName(path);
            Md5 = ServiceLocator.FileStorage.GetSignature(path, 5000);
            Size = ServiceLocator.FileStorage.GetSize(path);
        }
    }

    public abstract class AdvancedMetaEngine : IMetaEngine
    {
        private ADGameDescription[] _descs;

        protected AdvancedMetaEngine(ADGameDescription[] descs)
        {
            _descs = descs;
        }

        public abstract IEngine Create(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode = false);

        public GameDetected DetectGame(string path)
        {
            var directory = ServiceLocator.FileStorage.GetDirectoryName(path);
            var fileProperties = ServiceLocator.FileStorage.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Select(p => new FileProperties(p))
                .ToList();
            foreach (var desc in _descs)
            {
                var gameDetected = DetectGame(path, fileProperties, desc);
                if (gameDetected != null)
                {
                    return gameDetected;
                }
            }
            return null;
        }

        private GameDetected DetectGame(string path, List<FileProperties> fileProperties, ADGameDescription desc)
        {
            foreach (var fileDesc in desc.filesDescriptions)
            {
                var file = fileProperties.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.FileName, fileDesc.fileName));
                if (file == null)
                    return null;
                if (fileDesc.fileSize != -1 && file.Size != fileDesc.fileSize)
                    return null;
                if (fileDesc.md5 != null && file.Md5 != fileDesc.md5)
                    return null;
            }
            return CreateGameDetected(path, desc);
        }

        protected abstract GameDetected CreateGameDetected(string path, ADGameDescription desc);

        protected CultureInfo ToCulture(Language language)
        {
            switch (language)
            {
                case Language.ZH_CNA:
                    break;
                case Language.ZH_TWN:
                    break;
                case Language.HR_HRV:
                    break;
                case Language.CZ_CZE:
                    break;
                case Language.NL_NLD:
                    break;
                case Language.EN_ANY:
                    return new CultureInfo("en");
                case Language.EN_GRB:
                    return new CultureInfo("en-GB");
                case Language.EN_USA:
                    return new CultureInfo("en-US");
                case Language.FR_FRA:
                    break;
                case Language.DE_DEU:
                    break;
                case Language.GR_GRE:
                    break;
                case Language.HE_ISR:
                    break;
                case Language.HU_HUN:
                    break;
                case Language.IT_ITA:
                    break;
                case Language.JA_JPN:
                    break;
                case Language.KO_KOR:
                    break;
                case Language.LV_LAT:
                    break;
                case Language.NB_NOR:
                    break;
                case Language.PL_POL:
                    break;
                case Language.PT_BRA:
                    break;
                case Language.RU_RUS:
                    break;
                case Language.ES_ESP:
                    break;
                case Language.SE_SWE:
                    break;
                case Language.UNK_LANG:
                    return new CultureInfo("inv");
                default:
                    break;
            }
            return new CultureInfo("inv");
        }
    }
}
