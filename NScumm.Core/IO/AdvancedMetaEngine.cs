//
//  AdvancedMetaEngine.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Core.IO
{
    public struct ADGameFileDescription
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
        /// Size of the described file. Set to -1 to ignore.
        /// </summary>
        public int fileSize;

        public ADGameFileDescription(string fileName, string md5 = null, int fileSize = -1, ushort fileType = 0)
        {
            this.fileName = fileName;
            this.md5 = md5;
            this.fileSize = fileSize;
            this.fileType = fileType;
        }
    }

    public class ADGameDescription
    {
        public string gameid;
        public string extra;
        public ADGameFileDescription[] filesDescriptions;
        public Language language;
        public Platform platform;

        /**
         * A bitmask of extra flags. The top 16 bits are reserved for generic flags
         * defined in the ADGameFlags. This leaves 16 bits to be used by client
         * code.
         */
        public uint flags;

        public string guioptions;

        public ADGameDescription(string gameid, string extra = null, ADGameFileDescription[] filesDescriptions = null,
                                 Language language = Language.EN_ANY, Platform platform = Platform.DOS)
        {
            this.gameid = gameid;
            this.extra = extra;
            this.filesDescriptions = filesDescriptions;
            this.language = language;
            this.platform = platform;
        }
    }

        public abstract class AdvancedMetaEngine : MetaEngine
    {
        ADGameDescription[] _gameDescriptors;

        protected AdvancedMetaEngine(ADGameDescription[] descs)
        {
            _gameDescriptors = descs;
        }

        protected abstract IGameDescriptor CreateGameDescriptor(string path, ADGameDescription desc);

        public override GameDetected DetectGame(string path)
        {
            var dir = ServiceLocator.FileStorage.GetDirectoryName(path);
            D.Debug(3, $"Starting detection in dir '{path}'");

            var files = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            var allFiles = ServiceLocator.FileStorage.EnumerateFiles(dir, option: SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                files[ServiceLocator.FileStorage.GetFileName(file)] = file;
            }

            int curFilesMatched = 0;

            List<IGameDescriptor> matched = new List<IGameDescriptor>();
            int maxFilesMatched = 0;
            bool gotAnyMatchesWithAllFiles = false;

            // MD5 based matching
            foreach (var g in _gameDescriptors)
            {
                bool fileMissing = false;
                bool allFilesPresent = true;

                foreach (var desc in g.filesDescriptions)
                {
                    if (!files.ContainsKey(desc.fileName))
                    {
                        fileMissing = true;
                        allFilesPresent = false;
                        break;
                    }

                    var filePath = files[desc.fileName];
                    if (desc.md5 != null)
                    {
                        var md5 = ServiceLocator.FileStorage.GetSignature(filePath, 5000);
                        if (desc.md5 != md5)
                        {
                            D.Debug(3, $"MD5 Mismatch. Skipping ({desc.md5}) ({md5})");
                            fileMissing = true;
                            break;
                        }
                    }

                    if (desc.fileSize != -1 && desc.fileSize != ServiceLocator.FileStorage.GetSize(filePath))
                    {
                        D.Debug(3, "Size Mismatch. Skipping");
                        fileMissing = true;
                        break;
                    }

                    // We found at least one entry with all required files present.
                    // That means that we got new variant of the game.
                    //
                    // Without this check we would have erroneous checksum display
                    // where only located files will be enlisted.
                    //
                    // Potentially this could rule out variants where some particular file
                    // is really missing, but the developers should better know about such
                    // cases.
                    if (allFilesPresent)
                        gotAnyMatchesWithAllFiles = true;

                    if (!fileMissing)
                    {
                        // TODO: D.Debug(2, "Found game: %s (%s %s/%s) (%d)", desc.gameid, desc.extra,
                        //getPlatformDescription(g->platform), getLanguageDescription(g->language), i);

                        if (curFilesMatched > maxFilesMatched)
                        {
                            D.Debug(2, " ... new best match, removing all previous candidates");
                            maxFilesMatched = curFilesMatched;

                            matched.Clear();    // Remove any prior, lower ranked matches.
                            matched.Add(CreateGameDescriptor(path, g));
                        }
                        else if (curFilesMatched == maxFilesMatched)
                        {
                            matched.Add(CreateGameDescriptor(path, g));
                        }
                        else
                        {
                            D.Debug(2, " ... skipped");
                        }

                    }
                    else
                    {
                        D.Debug(5, $"Skipping game: {g.gameid} ({g.extra} {g.platform}/{g.language})");
                    }
                }
            }

            // We didn't find a match
            if (matched.Count == 0)
            {
                if (files.Count != 0 && gotAnyMatchesWithAllFiles)
                {
                    ReportUnknown(path, files);
                }

                // TODO:Filename based fallback
                return null;
            }

            return new GameDetected(matched.First(), this);
        }

        private void ReportUnknown(string path, Dictionary<string, string> files)
        {
            // TODO: This message should be cleaned up / made more specific.
            // For example, we should specify at least which engine triggered this.
            //
            // Might also be helpful to display the full path (for when this is used
            // from the mass detector).
            var report = new StringBuilder($"The game in '{path}' seems to be unknown.\n");
            report.AppendLine("Please, report the following data to the ScummVM team along with name");
            report.AppendLine("of the game you tried to add and its version/language/etc.:");

            foreach (var file in files)
            {
                var md5 = ServiceLocator.FileStorage.GetSignature(file.Value, 5000);
                var size = ServiceLocator.FileStorage.GetSize(file.Value);
                report.AppendLine($"  \"{file.Key}\", 0, \"{md5}\", {size}");
            }

            report.AppendLine();

            ServiceLocator.Platform.LogMessage(LogMessageType.Info, report.ToString());
        }
    }
}

