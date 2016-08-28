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
using System.Collections.Generic;

namespace NScumm.Core.IO
{
    /// <summary>
    /// Per-game extra GUI options structure.
    /// Currently, this can only be used for options with checkboxes.
    /// </summary>
    public class ExtraGuiOption
    {
        public string Label; // option label, e.g. "Fullscreen mode"
        public string Tooltip; // option tooltip (when the mouse hovers above it)
        public string ConfigOption; // confMan key, e.g. "fullscreen"
        public bool DefaultState; // the detault state of the checkbox (checked or not)

        public ExtraGuiOption(string label, string tooltip, string configOption, bool defaultState)
        {
            Label = label;
            Tooltip = tooltip;
            ConfigOption = configOption;
            DefaultState = defaultState;
        }

        public static GuiOptions ParseGameGuiOptions(string str)
        {
            var res = GuiOptions.NONE;

            foreach (var option in GameOptions)
                if (str.IndexOf(option.Key, StringComparison.Ordinal) != -1)
                    res |= option.Value;

            return res;
        }

        private static readonly Dictionary<string, GuiOptions> GameOptions = new Dictionary<string, GuiOptions>
        {
            {"sndNoSubs", GuiOptions.NOSUBTITLES},
            {"sndNoMusic", GuiOptions.NOMUSIC},
            {"sndNoSpeech", GuiOptions.NOSPEECH},
            {"sndNoSFX", GuiOptions.NOSFX},
            {"sndNoMIDI", GuiOptions.NOMIDI},
            {"launchNoLoad", GuiOptions.NOLAUNCHLOAD},
            {"midiPCSpk", GuiOptions.MIDIPCSPK},
            {"midiCMS", GuiOptions.MIDICMS},
            {"midiPCJr", GuiOptions.MIDIPCJR},
            {"midiAdLib", GuiOptions.MIDIADLIB},
            {"midiC64", GuiOptions.MIDIC64},
            {"midiAmiga", GuiOptions.MIDIAMIGA},
            {"midiAppleIIgs", GuiOptions.MIDIAPPLEIIGS},
            {"midiTowns", GuiOptions.MIDITOWNS},
            {"midiPC98", GuiOptions.MIDIPC98},
            {"midiMt32", GuiOptions.MIDIMT32},
            {"midiGM", GuiOptions.MIDIGM},
            {"noAspect", GuiOptions.NOASPECT},
            {"hercGreen", GuiOptions.RENDERHERCGREEN},
            {"hercAmber", GuiOptions.RENDERHERCAMBER},
            {"cga", GuiOptions.RENDERCGA},
            {"ega", GuiOptions.RENDEREGA},
            {"vga", GuiOptions.RENDERVGA},
            {"amiga", GuiOptions.RENDERAMIGA},
            {"fmtowns", GuiOptions.RENDERFMTOWNS},
            {"pc9821", GuiOptions.RENDERPC9821},
            {"pc9801", GuiOptions.RENDERPC9801},
            {"2gs", GuiOptions.RENDERAPPLE2GS},
            {"atari", GuiOptions.RENDERATARIST},
            {"macintosh", GuiOptions.RENDERMACINTOSH},
            {"gameOption1", GuiOptions.GAMEOPTIONS1},
            {"gameOption2", GuiOptions.GAMEOPTIONS2},
            {"gameOption3", GuiOptions.GAMEOPTIONS3},
            {"gameOption4", GuiOptions.GAMEOPTIONS3},
            {"gameOption5", GuiOptions.GAMEOPTIONS3},
            {"gameOption6", GuiOptions.GAMEOPTIONS3},
            {"gameOption7", GuiOptions.GAMEOPTIONS3},
            {"gameOption8", GuiOptions.GAMEOPTIONS3},
            {"gameOption9", GuiOptions.GAMEOPTIONS3},
        };
    }

    [Flags]
    public enum MetaEngineFeature
    {
        /// <summary>
        /// Listing all Save States for a given target is supported, i.e.,
        /// the listSaves() and getMaximumSaveSlot methods are implemented.
        /// Used for --list-saves support, as well as the GMM load dialog.
        /// </summary>
        SupportsListSaves,

        /// <summary>
        /// Loading from the Launcher / command line (-x).
        /// </summary>
        SupportsLoadingDuringStartup,

        /// <summary>
        /// Deleting Saves from the Launcher (i.e. implements the
        /// removeSaveState() method)
        /// </summary>
        SupportsDeleteSave,

        /// <summary>
        /// Features meta infos for savestates (i.e. implements the
        /// querySaveMetaInfos method properly).
        ///
        /// Engines implementing meta infos always have to provide
        /// the following entries in the save state descriptor queried
        /// by querySaveMetaInfos:
        /// - 'is_deletable', which indicates if a given save is
        ///                   safe for deletion
        /// - 'is_write_protected', which indicates if a given save
        ///                         can be overwritten by the user.
        ///                         (note: of course you do not have to
        ///                         set this, since it defaults to 'false')
        /// </summary>
        SavesSupportMetaInfo,

        /// <summary>
        /// Features a thumbnail in savegames (i.e. includes a thumbnail
        /// in savestates returned via querySaveMetaInfo).
        /// This flag may only be set when 'kSavesSupportMetaInfo' is set.
        /// </summary>
        SavesSupportThumbnail,

        /// <summary>
        /// Features 'save_date' and 'save_time' entries in the
        /// savestate returned by querySaveMetaInfo. Those values
        /// indicate the date/time the savegame was created.
        /// This flag may only be set when 'kSavesSupportMetaInfo' is set.
        /// </summary>
        SavesSupportCreationDate,

        /// <summary>
        /// Features 'play_time' entry in the savestate returned by
        /// querySaveMetaInfo. It indicates how long the user played
        /// the game till the save.
        /// This flag may only be set when 'kSavesSupportMetaInfo' is set.
        /// </summary>
        SavesSupportPlayTime
    }

    public interface IMetaEngine
    {
        /// <summary>
        /// Gets some copyright information about the original engine.
        /// </summary>
        /// <value>The copyright information about the original engine.</value>
        string OriginalCopyright { get; }

        GameDetected DetectGame(string path);

        IEngine Create(GameSettings settings, ISystem system);

        IList<SaveStateDescriptor> ListSaves(string target);

        void RemoveSaveState(string target, int slot);

        /// <summary>
        /// Determine whether the engine supports the specified MetaEngine feature.
        /// Used by e.g. the launcher to determine whether to enable the "Load" button.
        /// </summary>
        /// <returns>The feature.</returns>
        /// <param name="f">Feature.</param>
        bool HasFeature(MetaEngineFeature f);

        List<ExtraGuiOption> GetExtraGuiOptions(string target);
    }

    public abstract class MetaEngine : IMetaEngine
    {
        public abstract string OriginalCopyright { get; }

        public abstract GameDetected DetectGame(string path);

        public abstract IEngine Create(GameSettings settings, ISystem system);

        /// <summary>
        /// Return a list of all save states associated with the given target.
        /// The caller has to ensure that this (Meta)Engine is responsible
        /// for the specified target (by using findGame on it respectively
        /// on the associated gameid from the relevant ConfMan entry, if present).
        /// The default implementation returns an empty list.
        /// </summary>
        /// <remarks>
        /// MetaEngines must indicate that this function has been implemented
        /// via the kSupportsListSaves feature flag.
        /// </remarks>
        /// <returns>A list of save state descriptors.</returns>
        /// <param name="target">Name of a config manager target.</param>
        public virtual IList<SaveStateDescriptor> ListSaves(string target)
        {
            return new SaveStateDescriptor[0];
        }

        /// <summary>
        /// Remove the specified save state.
        /// For most engines this just amounts to calling _saveFileMan->removeSaveFile().
        /// Engines which keep an index file will also update it accordingly.
        /// </summary>
        /// <returns>The save state.</returns>
        /// <param name="target">Name of a config manager target.</param>
        /// <param name="slot">Slot number of the save state to be removed.</param>
        public virtual void RemoveSaveState(string target, int slot)
        {
        }

        /// <summary>
        /// Determine whether the engine supports the specified MetaEngine feature.
        /// Used by e.g. the launcher to determine whether to enable the "Load" button.
        /// </summary>
        /// <returns>The feature.</returns>
        /// <param name="f">F.</param>
        public virtual bool HasFeature(MetaEngineFeature f)
        {
            return false;
        }

        /// <summary>
        /// Return a list of extra GUI options for the specified target.
        /// If no target is specified, all of the available custom GUI options are
        /// Returned for the plugin (used to set default values).
        /// Currently, this only supports options with checkboxes.
        /// The default implementation returns an empty list.
        /// </summary>
        /// <returns>A list of extra GUI options for an engine plugin and target.</returns>
        /// <param name="target">name of a config manager target.</param>
        public virtual List<ExtraGuiOption> GetExtraGuiOptions(string target)
        {
            return new List<ExtraGuiOption>();
        }
    }
}