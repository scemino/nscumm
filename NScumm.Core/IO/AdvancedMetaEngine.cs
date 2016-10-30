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
    [Flags]
    public enum GuiOptions
    {
        NONE,
        NOSUBTITLES,
        NOMUSIC,
        NOSPEECH,
        NOSFX,
        NOMIDI,
        NOLAUNCHLOAD,

        MIDIPCSPK,
        MIDICMS,
        MIDIPCJR,
        MIDIADLIB,
        MIDIC64,
        MIDIAMIGA,
        MIDIAPPLEIIGS,
        MIDITOWNS,
        MIDIPC98,
        MIDIMT32,
        MIDIGM,

        NOASPECT,

        RENDERHERCGREEN,
        RENDERHERCAMBER,
        RENDERCGA,
        RENDEREGA,
        RENDERVGA,
        RENDERAMIGA,
        RENDERFMTOWNS,
        RENDERPC9821,
        RENDERPC9801,
        RENDERAPPLE2GS,
        RENDERATARIST,
        RENDERMACINTOSH,

        // Special GUIO flags for the AdvancedDetector's caching of game specific
        // options.
        GAMEOPTIONS1,
        GAMEOPTIONS2,
        GAMEOPTIONS3,
        GAMEOPTIONS4,
        GAMEOPTIONS5,
        GAMEOPTIONS6,
        GAMEOPTIONS7,
        GAMEOPTIONS8,
        GAMEOPTIONS9,
    }

    [Flags]
    public enum ADGameFlags
    {
        NO_FLAGS = 0,

        /// <summary>
        /// flag to designate not yet officially-supported games that are not fit for public testin
        /// </summary>
        UNSTABLE = 1 << 21,

        /// <summary>
        /// flag to designate not yet officially-supported games that are fit for public testing
        /// </summary>
        TESTING = 1 << 22,

        /// <summary>
        /// flag to designate well known pirated versions with cracks
        /// </summary>
        PIRATED = 1 << 23,

        /// <summary>
        /// always add English as language option
        /// </summary>
        ADDENGLISH = 1 << 24,

        /// <summary>
        /// the md5 for this entry will be calculated from the resource fork
        /// </summary>
        MACRESFORK = 1 << 25,

        /// <summary>
        /// Extra field value will be used as main game title, not gameid
        /// </summary>
        USEEXTRAASTITLE = 1 << 26,

        /// <summary>
        /// don't add language to gameid
        /// </summary>
        DROPLANGUAGE = 1 << 27,

        /// <summary>
        /// don't add platform to gameid
        /// </summary>
        DROPPLATFORM = 1 << 28,

        /// <summary>
        /// add "-cd" to gameid
        /// </summary>
        CD = 1 << 29,

        /// <summary>
        /// add "-demo" to gameid
        /// </summary>
        DEMO = 1 << 30
    }

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

        public ADGameFileDescription(string fileName, ushort fileType = 0, string md5 = null, int fileSize = -1)
        {
            this.fileName = fileName;
            this.md5 = md5;
            this.fileSize = fileSize;
            this.fileType = fileType;
        }
    }

    public class ADGameDescription
    {
        public readonly string gameid;
        public readonly string extra;
        public readonly ADGameFileDescription[] filesDescriptions;
        public readonly Language language;
        public readonly Platform platform;

        /// <summary>
        /// A bitmask of extra flags. The top 16 bits are reserved for generic flags
        /// defined in the ADGameFlags. This leaves 16 bits to be used by client
        /// code.
        /// </summary>
        public readonly ADGameFlags flags;

        public GuiOptions guioptions;

        public ADGameDescription(string gameid, string extra = null, ADGameFileDescription[] filesDescriptions = null,
            Language language = Language.EN_ANY, Platform platform = Platform.DOS,
            ADGameFlags flags = ADGameFlags.NO_FLAGS, GuiOptions guiOptions = GuiOptions.NONE)
        {
            this.gameid = gameid;
            this.extra = extra;
            this.filesDescriptions = filesDescriptions;
            this.language = language;
            this.platform = platform;
            this.flags = flags;
            guioptions = guiOptions;
        }
    }

    public abstract class AdvancedMetaEngine : MetaEngine
    {
        private readonly ADGameDescription[] _gameDescriptors;
        private readonly IDictionary<GuiOptions, ExtraGuiOption> _extraGuiOptions;

        protected AdvancedMetaEngine(ADGameDescription[] descs,
            IDictionary<GuiOptions, ExtraGuiOption> extraGuiOptions = null)
        {
            if (descs == null) throw new ArgumentNullException(nameof(descs));

            _gameDescriptors = descs;
            _extraGuiOptions = extraGuiOptions ?? new Dictionary<GuiOptions, ExtraGuiOption>();
        }

        protected abstract IGameDescriptor CreateGameDescriptor(string path, ADGameDescription desc);

        public override GameDetected DetectGame(string path)
        {
            var dir = ServiceLocator.FileStorage.GetDirectoryName(path);
            D.Debug(3, $"Starting detection in dir '{path}'");

            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var allFiles = ServiceLocator.FileStorage.EnumerateFiles(dir, option: SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                files[ServiceLocator.FileStorage.GetFileName(file)] = file;
            }

            var curFilesMatched = 0;

            var matched = new List<IGameDescriptor>();
            var maxFilesMatched = 0;
            var gotAnyMatchesWithAllFiles = false;

            // MD5 based matching
            foreach (var g in _gameDescriptors)
            {
                var fileMissing = false;
                var allFilesPresent = true;

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

                        matched.Clear(); // Remove any prior, lower ranked matches.
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

            // We didn't find a match
            if (matched.Count == 0)
            {
                if (files.Count != 0 && gotAnyMatchesWithAllFiles)
                {
                    ReportUnknown(path, files);
                }

                // TODO:Filename based fallback
                var desc = FallbackDetect(dir, files);
                if (desc == null) return null;
                var gd = CreateGameDescriptor(path, desc);
                return new GameDetected(gd, this);
            }

            return new GameDetected(matched.First(), this);
        }

        /**
         * An (optional) generic fallback detect function which is invoked
         * if the regular MD5 based detection failed to detect anything.
         */
        protected virtual ADGameDescription FallbackDetect(string directory, Dictionary<string, string> allFiles)
        {
            return null;
        }

        public override List<ExtraGuiOption> GetExtraGuiOptions(string target)
        {
            var options = new List<ExtraGuiOption>();

            // If there isn't any target specified, return all available GUI options.
            // Only used when an engine starts in order to set option defaults.
            if (string.IsNullOrEmpty(target))
            {
                options.AddRange(_extraGuiOptions.Values);
                return options;
            }

            // Query the GUI options
            var guiOptionsString = ConfigManager.Instance.Get<string>("guioptions", target);
            var guiOptions = ExtraGuiOption.ParseGameGuiOptions(guiOptionsString);

            // Add all the applying extra GUI options.
            foreach (var entry in _extraGuiOptions)
            {
                if (guiOptions.HasFlag(entry.Key))
                    options.Add(entry.Value);
            }

            return options;
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