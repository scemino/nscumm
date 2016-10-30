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
using System.IO;
using static NScumm.Core.DebugHelper;
using System.Linq;
using System.Text;
using NScumm.Core.IO;

namespace NScumm.Sci.Engine
{
    internal partial class Kernel
    {
        private const int _K_FILE_MODE_OPEN_OR_CREATE = 0;
        private const int _K_FILE_MODE_OPEN_OR_FAIL = 1;
        private const int _K_FILE_MODE_CREATE = 2;

        // We assume that scripts give us savegameId 0.99 for creating a new save slot
        //  and savegameId 100.199 for existing save slots. Refer to kfile.cpp
        private const int SAVEGAMEID_OFFICIALRANGE_START = 100;
        private const int SAVEGAMEID_OFFICIALRANGE_END = 199;


        private const int VIRTUALFILE_HANDLE_START = 32000;
        private const int VIRTUALFILE_HANDLE_SCI32SAVE = 32100;
        private const int VIRTUALFILE_HANDLE_SCIAUDIO = 32300;
        private const int VIRTUALFILE_HANDLE_END = 32300;

        /// <summary>
        /// Maximum number of savegames.
        /// </summary>
        private const int MAX_SAVEGAME_NR = 20;

        /// <summary>
        /// Maximum length of a savegame name (including terminator character).
        /// </summary>
        private const int SCI_MAX_SAVENAME_LENGTH = 0x24;

#if ENABLE_SCI32
        /// <summary>
        /// The save game slot number for autosaves
        /// </summary>
        private const int AutoSaveId = 0;

        /// <summary>
        /// The save game slot number for a "new game" save
        /// </summary>
        private const int NewGameId = 999;

        // SCI engine expects game IDs to start at 0, but slot 0 in ScummVM is
        // reserved for autosave, so non-autosave games get their IDs shifted up
        // when saving or restoring, and shifted down when enumerating save games
        private const int SaveIdShift = 1;
#endif

        private enum DeviceInfo
        {
            GET_DEVICE = 0,
            GET_CURRENT_DEVICE = 1,
            PATHS_EQUAL = 2,
            IS_FLOPPY = 3,
            GET_CONFIG_PATH = 5,
            GET_SAVECAT_NAME = 7,
            GET_SAVEFILE_NAME = 8
        }

        private static Register kCheckFreeSpace(EngineState s, int argc, StackPtr argv)
        {
            if (argc > 1)
            {
                // SCI1.1/SCI32
                // TODO: don't know if those are right for SCI32 as well
                // Please note that sierra sci supported both calls either w/ or w/o opcode in SCI1.1
                switch (argv[1].ToUInt16())
                {
                    case 0: // return saved game size
                        return Register.Make(0, 0); // we return 0

                    case 1: // return free harddisc space (shifted right somehow)
                        return Register.Make(0, 0x7fff); // we return maximum

                    case 2: // same as call w/o opcode
                        break;

                    default:
                        throw new InvalidOperationException(
                            "kCheckFreeSpace: called with unknown sub-op {argv[1].ToUInt16()}");
                }
            }

            var path = s._segMan.GetString(argv[0]);

            Debug(3, $"kCheckFreeSpace({path}");

            // We simply always pretend that there is enough space. The alternative
            // would be to write a big test file, which is not nice on systems where
            // doing so is very slow.
            return Register.Make(0, 1);
        }

        private static Register kCheckSaveGame(EngineState s, int argc, StackPtr argv)
        {
            var game_id = s._segMan.GetString(argv[0]);
            var virtualId = argv[1].ToUInt16();

            Debug(3, $"kCheckSaveGame({game_id}, {virtualId})");

            var saves = File.ListSavegames();

            // we allow 0 (happens in QfG2 when trying to restore from an empty saved game list) and return false in that case
            if (virtualId == 0)
                return Register.NULL_REG;

            var savegameId = 0;
            if (SciEngine.Instance.GameId == SciGameId.JONES)
            {
                // Jones has one save slot only
            }
            else
            {
                // Find saved game
                if ((virtualId < SAVEGAMEID_OFFICIALRANGE_START) || (virtualId > SAVEGAMEID_OFFICIALRANGE_END))
                    throw new InvalidOperationException($"kCheckSaveGame: called with invalid savegame ID ({virtualId})");
                savegameId = virtualId - SAVEGAMEID_OFFICIALRANGE_START;
            }

            var savegameNr = File.FindSavegame(saves, (short) savegameId);
            if (savegameNr == -1)
                return Register.NULL_REG;

            // Check for compatible savegame version
            var ver = saves[savegameNr].version;
            if (ver < Savegame.MINIMUM_SAVEGAME_VERSION || ver > Savegame.CURRENT_SAVEGAME_VERSION)
                return Register.NULL_REG;

            // Otherwise we assume the savegame is OK
            return Register.TRUE_REG;
        }

        private static Register kDeviceInfo(EngineState s, int argc, StackPtr argv)
        {
            if (SciEngine.Instance.GameId == SciGameId.FANMADE && argc == 1)
            {
                // WORKAROUND: The fan game script library calls kDeviceInfo with one parameter.
                // According to the scripts, it wants to call CurDevice. However, it fails to
                // provide the subop to the function.
                s._segMan.Strcpy(argv[0], "/");
                return s.r_acc;
            }

            var mode = (DeviceInfo) argv[0].ToUInt16();

            switch (mode)
            {
                case DeviceInfo.GET_DEVICE:
                {
                    var input_str = s._segMan.GetString(argv[1]);

                    s._segMan.Strcpy(argv[2], "/");
                    Debug(3, $"DeviceInfo.GET_DEVICE({input_str}) . /");
                    break;
                }
                case DeviceInfo.GET_CURRENT_DEVICE:
                    s._segMan.Strcpy(argv[1], "/");
                    Debug(3, "DeviceInfo.GET_CURRENT_DEVICE() . /");
                    break;

                case DeviceInfo.PATHS_EQUAL:
                {
                    var path1_s = s._segMan.GetString(argv[1]);
                    var path2_s = s._segMan.GetString(argv[2]);
                    Debug(3, $"DeviceInfo.PATHS_EQUAL({path1_s},{path2_s})");

                    Register.Make(0, string.Equals(path2_s, path1_s, StringComparison.Ordinal));
                }
                    break;

                case DeviceInfo.IS_FLOPPY:
                {
                    var input_str = s._segMan.GetString(argv[1]);
                    Debug(3, $"DeviceInfo.IS_FLOPPY({input_str})");
                    return Register.NULL_REG; /* Never */
                }
                case DeviceInfo.GET_CONFIG_PATH:
                {
                    // Early versions return drive letter, later versions a path string
                    // FIXME: Implement if needed, for now return NULL_REG
                    return Register.NULL_REG;
                }
                /* SCI uses these in a less-than-portable way to delete savegames.
                    ** Read http://www-plan.cs.colorado.edu/creichen/freesci-logs/2005.10/log20051019.html
                    ** for more information on our workaround for this.
                    */
                case DeviceInfo.GET_SAVECAT_NAME:
                {
                    var game_prefix = s._segMan.GetString(argv[2]);
                    s._segMan.Strcpy(argv[1], "__throwaway");
                    Debug(3, $"DeviceInfo.GET_SAVECAT_NAME({game_prefix}) . __throwaway");
                }

                    break;
                case DeviceInfo.GET_SAVEFILE_NAME:
                {
                    var game_prefix = s._segMan.GetString(argv[2]);
                    int virtualId = argv[3].ToUInt16();
                    s._segMan.Strcpy(argv[1], "__throwaway");
                    Debug(3, $"DeviceInfo.GET_SAVEFILE_NAME({game_prefix},{virtualId}) . __throwaway");
                    if ((virtualId < SAVEGAMEID_OFFICIALRANGE_START) || (virtualId > SAVEGAMEID_OFFICIALRANGE_END))
                        throw new InvalidOperationException("kDeviceInfo(deleteSave): invalid savegame ID specified");
                    var savegameId = virtualId - SAVEGAMEID_OFFICIALRANGE_START;
                    var saves = File.ListSavegames();
                    if (File.FindSavegame(saves, (short) savegameId) != -1)
                    {
                        // Confirmed that this id still lives...
                        var filename = SciEngine.Instance.GetSavegameName(savegameId);
                        var saveFileMan = SciEngine.Instance.SaveFileManager;
                        saveFileMan.RemoveSavefile(filename);
                    }
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unknown DeviceInfo() sub-command: {mode}");
            }

            return s.r_acc;
        }

        private static Register kGetSaveDir(EngineState s, int argc, StackPtr argv)
        {
#if ENABLE_SCI32
            // SCI32 uses a parameter here. It is used to modify a string, stored in a
            // global variable, so that game scripts store the save directory. We
            // don't really set a save game directory, thus not setting the string to
            // anything is the correct thing to do here.
            //if (argc > 0)
            //	warning("kGetSaveDir called with %d parameter(s): %04x:%04x", argc, PRINT_REG(argv[0]));
#endif
            return s._segMan.SaveDirPtr;
        }

        private static Register kGetSaveFiles(EngineState s, int argc, StackPtr argv)
        {
            var game_id = s._segMan.GetString(argv[0]);

            Debug(3, $"kGetSaveFiles({game_id})");

            // Scripts ask for current save files, we can assume that if afterwards they ask us to create a new slot they really
            //  mean new slot instead of overwriting the old one
            s._lastSaveVirtualId = SAVEGAMEID_OFFICIALRANGE_START;

            var saves = File.ListSavegames();
            var totalSaves = (uint) Math.Min(saves.Count, MAX_SAVEGAME_NR);

            var slot = s._segMan.DerefRegPtr(argv[2], (int) totalSaves);

            if (slot == null)
            {
                Warning($"kGetSaveFiles: {argv[2]} invalid or too small to hold slot data");
                totalSaves = 0;
            }
            var sl = slot.Value;

            var bufSize = (totalSaves * SCI_MAX_SAVENAME_LENGTH) + 1;
            var saveNames = new byte[bufSize];
            var ptr = 0;

            for (var i = 0; i < totalSaves; i++)
            {
                sl[i] = Register.Make(0, (ushort) (saves[i].id + SAVEGAMEID_OFFICIALRANGE_START));
                // Store the virtual savegame ID (see above)
                Array.Copy(saves[i].name.GetBytes(), 0, saveNames, ptr, saves[i].name.Length);
                ptr += SCI_MAX_SAVENAME_LENGTH;
            }

            saveNames[ptr] = 0; // Terminate list

            s._segMan.Memcpy(argv[1], new ByteAccess(saveNames), (int) bufSize);

            return Register.Make(0, (ushort) totalSaves);
        }

        private static Register kGetSaveFiles32(EngineState s, int argc, StackPtr argv)
        {
            // argv[0] is gameName, used in SSCI as the name of the save game catalogue
            // but unused here since ScummVM does not support multiple catalogues
            SciArray descriptions = s._segMan.LookupArray(argv[1]);
            SciArray saveIds = s._segMan.LookupArray(argv[2]);

            var saves = File.ListSavegames();

            // Normally SSCI limits to 20 games per directory, but ScummVM allows more
            // than that with games that use the standard save-load dialogue
            descriptions.Resize((ushort) (SCI_MAX_SAVENAME_LENGTH * saves.Count + 1), true);
            saveIds.Resize((ushort) (saves.Count + 1), true);

            BytePtr ptr;
            for (var i = 0; i < saves.Count; ++i)
            {
                SavegameDesc save = saves[i];
                var target = descriptions.ByteAt((ushort) (SCI_MAX_SAVENAME_LENGTH * i));
                Array.Copy(save.name.GetBytes(), 0, target.Data, target.Offset, SCI_MAX_SAVENAME_LENGTH);
                ptr = saveIds.ByteAt(0);
                ptr.Data.WriteInt16(i * 2, (short) (save.id - SaveIdShift));
            }

            descriptions[(ushort) (SCI_MAX_SAVENAME_LENGTH * saves.Count)] = 0;
            ptr = saveIds.ByteAt(0);
            ptr.Data.WriteInt16(saves.Count * 2, 0);

            return Register.Make(0, (ushort) saves.Count);
        }

        public static Register kRestoreGame(EngineState s, int argc, StackPtr argv)
        {
            var game_id = !argv[0].IsNull ? s._segMan.GetString(argv[0]) : "";
            var savegameId = argv[1].ToInt16();
            var pausedMusic = false;

            Debug(3, "kRestoreGame({0},{1})", game_id, savegameId);

            if (argv[0].IsNull)
            {
                // Direct call, either from launcher or from a patched Game::restore
                if (savegameId == -1)
                {
                    // we are supposed to show a dialog for the user and let him choose a saved game
                    SciEngine.Instance._soundCmd.PauseAll(true); // pause music
                    throw new NotImplementedException("SaveLoadChooser not implemented.");
                    //using (var dialog = new GUI::SaveLoadChooser(_("Restore game:"), _("Restore"), false))
                    //{
                    //    savegameId = dialog.runModalWithCurrentTarget();
                    //}
                    if (savegameId < 0)
                    {
                        SciEngine.Instance._soundCmd.PauseAll(false); // unpause music
                        return s.r_acc;
                    }
                    pausedMusic = true;
                }
                // don't adjust ID of the saved game, it's already correct
            }
            else
            {
                if (SciEngine.Instance.GameId == SciGameId.JONES)
                {
                    // Jones has one save slot only
                    savegameId = 0;
                }
                else
                {
                    // Real call from script, we need to adjust ID
                    if ((savegameId < SAVEGAMEID_OFFICIALRANGE_START) || (savegameId > SAVEGAMEID_OFFICIALRANGE_END))
                    {
                        Warning($"Savegame ID {savegameId} is not allowed");
                        return Register.TRUE_REG;
                    }
                    savegameId -= SAVEGAMEID_OFFICIALRANGE_START;
                }
            }

            s.r_acc = Register.NULL_REG; // signals success

            var saves = File.ListSavegames();
            if (File.FindSavegame(saves, savegameId) == -1)
            {
                s.r_acc = Register.TRUE_REG;
                Warning($"Savegame ID {savegameId} not found");
            }
            else
            {
                var saveFileMan = SciEngine.Instance.SaveFileManager;
                var filename = SciEngine.Instance.GetSavegameName(savegameId);

                using (var @in = saveFileMan.OpenForLoading(filename))
                {
                    // found a savegame file
                    Savegame.gamestate_restore(s, @in);
                }

                switch (SciEngine.Instance.GameId)
                {
                    case SciGameId.MOTHERGOOSE:
                    {
                        // WORKAROUND: Mother Goose SCI0
                        //  Script 200 / rm200::newRoom will set global C5h directly right after creating a child to the
                        //   current number of children plus 1.
                        //  We can't trust that global, that's why we set the actual savedgame id right here directly after
                        //   restoring a saved game.
                        //  If we didn't, the game would always save to a new slot
                        s.variables[Vm.VAR_GLOBAL].SetOffset(0xC5,
                            (ushort) (SAVEGAMEID_OFFICIALRANGE_START + savegameId));
                    }
                        break;
                    case SciGameId.MOTHERGOOSE256:
                    {
                        // WORKAROUND: Mother Goose SCI1/SCI1.1 does some weird things for
                        //  saving a previously restored game.
                        // We set the current savedgame-id directly and remove the script
                        //  code concerning this via script patch.
                        s.variables[Vm.VAR_GLOBAL].SetOffset(0xB3,
                            (ushort) (SAVEGAMEID_OFFICIALRANGE_START + savegameId));
                    }
                        break;
                    case SciGameId.JONES:
                        // HACK: The code that enables certain menu items isn't called when a game is restored from the
                        // launcher, or the "Restore game" option in the game's main menu - bugs #6537 and #6723.
                        // These menu entries are disabled when the game is launched, and are enabled when a new game is
                        // started. The code for enabling these entries is is all in script 1, room1::init, but that code
                        // path is never followed in these two cases (restoring game from the menu, or restoring a game
                        // from the ScummVM launcher). Thus, we perform the calls to enable the menus ourselves here.
                        // These two are needed when restoring from the launcher
                        // FIXME: The original interpreter saves and restores the menu state, so these attributes
                        // are automatically reset there. We may want to do the same.
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(257 >> 8, 257 & 0xFF,
                            Graphics.MenuAttribute.ENABLED, Register.TRUE_REG); // Sierra . About Jones
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(258 >> 8, 258 & 0xFF,
                            Graphics.MenuAttribute.ENABLED, Register.TRUE_REG); // Sierra . Help
                        // The rest are normally enabled from room1::init
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(769 >> 8, 769 & 0xFF,
                            Graphics.MenuAttribute.ENABLED, Register.TRUE_REG); // Options . Delete current player
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(513 >> 8, 513 & 0xFF,
                            Graphics.MenuAttribute.ENABLED, Register.TRUE_REG); // Game . Save Game
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(515 >> 8, 515 & 0xFF,
                            Graphics.MenuAttribute.ENABLED, Register.TRUE_REG); // Game . Restore Game
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(1025 >> 8, 1025 & 0xFF,
                            Graphics.MenuAttribute.ENABLED, Register.TRUE_REG); // Status . Statistics
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(1026 >> 8, 1026 & 0xFF,
                            Graphics.MenuAttribute.ENABLED, Register.TRUE_REG); // Status . Goals
                        break;
                }
            }

            if (!s.r_acc.IsNull)
            {
                // no success?
                if (pausedMusic)
                    SciEngine.Instance._soundCmd.PauseAll(false); // unpause music
            }

            return s.r_acc;
        }

        private static Register kSaveGame(EngineState s, int argc, StackPtr argv)
        {
            string game_id;
            var virtualId = argv[1].ToInt16();
            short savegameId = -1;
            string game_description;
            var version = string.Empty;

            if (argc > 3)
                version = s._segMan.GetString(argv[3]);

            // We check here, we don't want to delete a users save in case we are within a kernel function
            if (s.executionStackBase != 0)
            {
                Warning("kSaveGame - won't save from within kernel function");
                return Register.NULL_REG;
            }

            if (argv[0].IsNull)
            {
                // Direct call, from a patched Game::save
                if ((argv[1] != Register.SIGNAL_REG) || (!argv[2].IsNull))
                    throw new InvalidOperationException("kSaveGame: assumed patched call isn't accurate");

                // we are supposed to show a dialog for the user and let him choose where to save
                SciEngine.Instance._soundCmd.PauseAll(true); // pause music
                throw new NotImplementedException("SaveLoadChooser not implemented.");
                //using (var dialog = new GUI::SaveLoadChooser(_("Save game:"), _("Save"), true))
                //{
                //    savegameId = dialog.runModalWithCurrentTarget();
                //    game_description = dialog.getResultString();
                //    if (string.IsNullOrEmpty(game_description))
                //    {
                //        // create our own description for the saved game, the user didn't enter it
                //        game_description = dialog.createDefaultSaveDescription(savegameId);
                //    }
                //}
                SciEngine.Instance._soundCmd.PauseAll(false); // unpause music (we can't have it paused during save)
                if (savegameId < 0)
                    return Register.NULL_REG;
            }
            else
            {
                // Real call from script
                game_id = s._segMan.GetString(argv[0]);
                if (argv[2].IsNull)
                    throw new InvalidOperationException("kSaveGame: called with description being NULL");
                game_description = s._segMan.GetString(argv[2]);

                Debug(3, $"kSaveGame({game_id},{virtualId},{game_description},{version})");

                var saves = File.ListSavegames();

                if ((virtualId >= SAVEGAMEID_OFFICIALRANGE_START) && (virtualId <= SAVEGAMEID_OFFICIALRANGE_END))
                {
                    // savegameId is an actual Id, so search for it just to make sure
                    savegameId = (short) (virtualId - SAVEGAMEID_OFFICIALRANGE_START);
                    if (File.FindSavegame(saves, savegameId) == -1)
                        return Register.NULL_REG;
                }
                else if (virtualId < SAVEGAMEID_OFFICIALRANGE_START)
                {
                    // virtualId is low, we assume that scripts expect us to create new slot
                    if (SciEngine.Instance.GameId == SciGameId.JONES)
                    {
                        // Jones has one save slot only
                        savegameId = 0;
                    }
                    else if (virtualId == s._lastSaveVirtualId)
                    {
                        // if last virtual id is the same as this one, we assume that caller wants to overwrite last save
                        savegameId = s._lastSaveNewId;
                    }
                    else
                    {
                        int savegameNr;
                        // savegameId is in lower range, scripts expect us to create a new slot
                        for (savegameId = 0; savegameId < SAVEGAMEID_OFFICIALRANGE_START; savegameId++)
                        {
                            for (savegameNr = 0; savegameNr < saves.Count; savegameNr++)
                            {
                                if (savegameId == saves[savegameNr].id)
                                    break;
                            }
                            if (savegameNr == saves.Count)
                                break;
                        }
                        if (savegameId == SAVEGAMEID_OFFICIALRANGE_START)
                            throw new InvalidOperationException("kSavegame: no more savegame slots available");
                    }
                }
                else
                {
                    throw new InvalidOperationException("kSaveGame: invalid savegameId used");
                }

                // Save in case caller wants to overwrite last newly created save
                s._lastSaveVirtualId = virtualId;
                s._lastSaveNewId = savegameId;
            }

            s.r_acc = Register.NULL_REG;

            var filename = SciEngine.Instance.GetSavegameName(savegameId);
            var saveFileMan = SciEngine.Instance.SaveFileManager;

            using (var @out = saveFileMan.OpenForSaving(filename))
            {
                if (!Savegame.gamestate_save(s, @out, game_description, version))
                {
                    Warning("Saving the game failed");
                }
                else
                {
                    s.r_acc = Register.TRUE_REG; // save successful
                }
            }

            return s.r_acc;
        }

        /// <summary>
        /// Writes the cwd to the supplied address and returns the address in acc.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        private static Register kGetCWD(EngineState s, int argc, StackPtr argv)
        {
            // We do not let the scripts see the file system, instead pretending
            // we are always in the same directory.
            // TODO/FIXME: Is "/" a good value? Maybe "" or "." or "C:\" are better?
            s._segMan.Strcpy(argv[0], "/");

            DebugC(DebugLevels.File, "kGetCWD() . {0}", "/");

            return argv[0];
        }

        private static Register kFileIO(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            throw new InvalidOperationException("not supposed to call this");
        }

        private static Register kFileIOOpen(EngineState s, int argc, StackPtr argv)
        {
            var name = new StringBuilder(s._segMan.GetString(argv[0]));

            // SCI32 can call K_FILEIO_OPEN with only one argument. It seems to
            // just be checking if it exists.
            var mode = (argc < 2) ? _K_FILE_MODE_OPEN_OR_FAIL : argv[1].ToUInt16();
            var unwrapFilename = true;

            // SQ4 floppy prepends /\ to the filenames
            if (name.ToString().StartsWith("/\\"))
            {
                name.Remove(0, 2);
            }

            // SQ4 floppy attempts to update the savegame index file sq4sg.dir when
            // deleting saved games. We don't use an index file for saving or loading,
            // so just stop the game from modifying the file here in order to avoid
            // having it saved in the ScummVM save directory.
            if (name.ToString() == "sq4sg.dir")
            {
                DebugC(DebugLevels.File, "Not opening unused file sq4sg.dir");
                return Register.SIGNAL_REG;
            }

            if (name.Length == 0)
            {
                // Happens many times during KQ1 (e.g. when typing something)
                DebugC(DebugLevels.File, "Attempted to open a file with an empty filename");
                return Register.SIGNAL_REG;
            }
            DebugC(DebugLevels.File, "kFileIO(open): {0}, 0x{1:X}", name, mode);

            if (name.ToString().StartsWith("sciAudio\\"))
            {
                // fan-made sciAudio extension, don't create those files and instead return a virtual handle
                return Register.Make(0, VIRTUALFILE_HANDLE_SCIAUDIO);
            }

#if ENABLE_SCI32
            // Shivers is trying to store savegame descriptions and current spots in
            // separate .SG files, which are hardcoded in the scripts.
            // Essentially, there is a normal save file, created by the executable
            // and an extra hardcoded save file, created by the game scripts, probably
            // because they didn't want to modify the save/load code to add the extra
            // information.
            // Each slot in the book then has two strings, the save description and a
            // description of the current spot that the player is at. Currently, the
            // spot strings are always empty (probably related to the unimplemented
            // kString subop 14, which gets called right before this call).
            // For now, we don't allow the creation of these files, which means that
            // all the spot descriptions next to each slot description will be empty
            // (they are empty anyway). Until a viable solution is found to handle these
            // extra files and until the spot description strings are initialized
            // correctly, we resort to virtual files in order to make the load screen
            // useable. Without this code it is unusable, as the extra information is
            // always saved to 0.SG for some reason, but on restore the correct file is
            // used. Perhaps the virtual ID is not taken into account when saving.
            //
            // Future TODO: maintain spot descriptions and show them too, ideally without
            // having to return to this logic of extra hardcoded files.
            if (SciEngine.Instance.GameId == SciGameId.SHIVERS && name.ToString().EndsWith(".SG"))
            {
                switch (mode)
                {
                    case _K_FILE_MODE_OPEN_OR_CREATE:
                    case _K_FILE_MODE_CREATE:
                        // Game scripts are trying to create a file with the save
                        // description, stop them here
                        DebugC(DebugLevels.File, "Not creating unused file {0}", name);
                        return Register.SIGNAL_REG;
                    case _K_FILE_MODE_OPEN_OR_FAIL:
                        // Create a virtual file containing the save game description
                        // and slot number, as the game scripts expect.
                        var slotNumber = int.Parse(
                            ServiceLocator.FileStorage.GetFileNameWithoutExtension(name.ToString()));

                        var saves = File.ListSavegames();
                        int savegameNr = File.FindSavegame(saves, (short) (slotNumber - SAVEGAMEID_OFFICIALRANGE_START));

                        var size = saves[savegameNr].name.Length + 2;
                        var buf = new byte[size];
                        var savename = saves[savegameNr].name.GetBytes();
                        Array.Copy(savename, buf, name.Length);
                        buf[size - 1] = 0; // Spot description (empty)

                        uint handle = FindFreeFileHandle(s);

                        s._fileHandles[handle]._in = new MemoryStream(buf, 0, size);
                        s._fileHandles[handle]._out = null;
                        s._fileHandles[handle]._name = "";

                        return Register.Make(0, (ushort) handle);
                }
            }
#endif

            // QFG import rooms get a virtual filelisting instead of an actual one
            if (SciEngine.Instance.InQfGImportRoom != 0)
            {
                // We need to find out what the user actually selected, "savedHeroes" is
                // already destroyed when we get here. That's why we need to remember
                // selection via kDrawControl.
                name = new StringBuilder(s._dirseeker.GetVirtualFilename(s._chosenQfGImportItem));
                unwrapFilename = false;
            }

            return file_open(s, name.ToString(), mode, unwrapFilename);
        }

        private static uint FindFreeFileHandle(EngineState s)
        {
            // Find a free file handle
            uint handle = 1; // Ignore _fileHandles[0]
            while ((handle < s._fileHandles.Length) && s._fileHandles[handle].IsOpen)
                handle++;

            if (handle == s._fileHandles.Length)
            {
                // Hit size limit => Allocate more space
                Array.Resize(ref s._fileHandles, s._fileHandles.Length + 1);
            }

            return handle;
        }

        private static Register file_open(EngineState s, string filename, int mode, bool unwrapFilename)
        {
            var englishName = SciEngine.Instance.GetSciLanguageString(filename, Language.English).ToLower();

            var wrappedName = unwrapFilename ? SciEngine.Instance.WrapFilename(englishName) : englishName;
            Stream inFile = null;
            Stream outFile = null;
            var saveFileMan = SciEngine.Instance.SaveFileManager;

            var isCompressed = true;
            var gameId = SciEngine.Instance.GameId;
            if ((gameId == SciGameId.QFG1 || gameId == SciGameId.QFG1VGA || gameId == SciGameId.QFG2 ||
                 gameId == SciGameId.QFG3)
                && englishName.EndsWith(".sav"))
            {
                // QFG Characters are saved via the CharSave object.
                // We leave them uncompressed so that they can be imported in later QFG
                // games.
                // Rooms/Scripts: QFG1: 601, QFG2: 840, QFG3/4: 52
                isCompressed = false;
            }

            if (mode == _K_FILE_MODE_OPEN_OR_FAIL)
            {
                // Try to open file, abort if not possible
                inFile = saveFileMan.OpenForLoading(wrappedName);
                // If no matching savestate exists: fall back to reading from a regular
                // file
                if (inFile == null)
                {
                    inFile = Core.Engine.OpenFileRead(englishName);
                }

                if (inFile == null)
                    DebugC(DebugLevels.File, "  . file_open(_K_FILE_MODE_OPEN_OR_FAIL): failed to open file '{0}'",
                        englishName);
            }
            else if (mode == _K_FILE_MODE_CREATE)
            {
                // Create the file, destroying any content it might have had
                outFile = saveFileMan.OpenForSaving(wrappedName, isCompressed);
                if (outFile == null)
                    DebugC(DebugLevels.File, "  . file_open(_K_FILE_MODE_CREATE): failed to create file '{0}'",
                        englishName);
            }
            else if (mode == _K_FILE_MODE_OPEN_OR_CREATE)
            {
                // Try to open file, create it if it doesn't exist
                outFile = saveFileMan.OpenForSaving(wrappedName, isCompressed);
                if (outFile == null)
                    DebugC(DebugLevels.File, "  . file_open(_K_FILE_MODE_CREATE): failed to create file '{0}'",
                        englishName);

                // QfG1 opens the character export file with _K_FILE_MODE_CREATE first,
                // closes it immediately and opens it again with this here. Perhaps
                // other games use this for read access as well. I guess changing this
                // whole code into using virtual files and writing them after close
                // would be more appropriate.
            }
            else
            {
                throw new InvalidOperationException($"file_open: unsupported mode {mode} (filename '{englishName}')");
            }

            if (inFile == null && outFile == null)
            {
                // Failed
                DebugC(DebugLevels.File, "  . file_open() failed");
                return Register.SIGNAL_REG;
            }

            // Find a free file handle
            uint handle = 1; // Ignore _fileHandles[0]
            while ((handle < s._fileHandles.Length) && s._fileHandles[handle].IsOpen)
                handle++;

            if (handle == s._fileHandles.Length)
            {
                // Hit size limit => Allocate more space
                Array.Resize(ref s._fileHandles, s._fileHandles.Length + 1);
            }

            s._fileHandles[handle]._in = inFile;
            s._fileHandles[handle]._out = outFile;
            s._fileHandles[handle]._name = englishName;

            DebugC(DebugLevels.File, "  . opened file '{0}' with handle {1}", englishName, handle);
            return Register.Make(0, (ushort) handle);
        }

        private static Register kFileIOClose(EngineState s, int argc, StackPtr argv)
        {
            DebugC(DebugLevels.File, "kFileIO(close): {0}", argv[0].ToUInt16());

            if (argv[0] == Register.SIGNAL_REG)
                return s.r_acc;

            var handle = argv[0].ToUInt16();

            if (handle >= VIRTUALFILE_HANDLE_START)
            {
                // it's a virtual handle? ignore it
                return Register.SIGNAL_REG;
            }

            var f = GetFileFromHandle(s, handle);
            if (f != null)
            {
                f.Close();
                if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                    return s.r_acc; // SCI0 semantics: no value returned
                return Register.SIGNAL_REG;
            }

            if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                return s.r_acc; // SCI0 semantics: no value returned
            return Register.NULL_REG;
        }

        private static Register kFileIOReadRaw(EngineState s, int argc, StackPtr argv)
        {
            ushort handle = argv[0].ToUInt16();
            ushort size = argv[2].ToUInt16();
            int bytesRead = 0;
            var buf = new byte[size];
            DebugC(DebugLevels.File, "kFileIO(readRaw): {0}, {1}", handle, size);
            Debug("kFileIO(readRaw): {0}, {1}", handle, size);

            FileHandle f = GetFileFromHandle(s, handle);
            if (f != null)
                bytesRead = f._in.Read(buf, 0, size);
            Debug("kFileIO(readRaw): {0}", buf);

            // TODO: What happens if less bytes are read than what has
            // been requested? (i.e. if bytesRead is non-zero, but still
            // less than size)
            if (bytesRead > 0)
                s._segMan.Memcpy(argv[1], new ByteAccess(buf), size);

            return Register.Make(0, (ushort) bytesRead);
        }

        private static FileHandle GetFileFromHandle(EngineState s, uint handle)
        {
            if ((handle == 0) || ((handle >= VIRTUALFILE_HANDLE_START) && (handle <= VIRTUALFILE_HANDLE_END)))
            {
                Error("Attempt to use invalid file handle ({0})", handle);
                return null;
            }

            if ((handle >= s._fileHandles.Length) || !s._fileHandles[handle].IsOpen)
            {
                Warning("Attempt to use invalid/unused file handle {0}", handle);
                return null;
            }

            return s._fileHandles[handle];
        }

        private static Register kFileIOWriteRaw(EngineState s, int argc, StackPtr argv)
        {
            ushort handle = argv[0].ToUInt16();
            ushort size = argv[2].ToUInt16();
            var buf = new byte[size];
            bool success = false;
            s._segMan.Memcpy(new ByteAccess(buf), argv[1], size);
            DebugC(DebugLevels.File, "kFileIO(writeRaw): {0}, {1}", handle, size);

            FileHandle f = GetFileFromHandle(s, handle);
            if (f != null)
            {
                f._out.Write(buf, 0, size);
                success = true;
            }

            if (success)
                return Register.NULL_REG;
            return Register.Make(0, 6); // DOS - invalid handle
        }

        private static Register kFileIOUnlink(EngineState s, int argc, StackPtr argv)
        {
            string name = s._segMan.GetString(argv[0]);
            var saveFileMan = SciEngine.Instance.System.SaveFileManager;
            bool result;

            // SQ4 floppy prepends /\ to the filenames
            if (name.StartsWith("/\\"))
            {
                name = name.Substring(2);
            }

            // Special case for SQ4 floppy: This game has hardcoded names for all of
            // its savegames, and they are all named "sq4sg.xxx", where xxx is the
            // slot. We just take the slot number here, and delete the appropriate
            // save game.
            if (name.StartsWith("sq4sg."))
            {
                // Special handling for SQ4... get the slot number and construct the
                // save game name.
                int slotNum = int.Parse(name.Substring(0, name.Length - 3));
                var saves = File.ListSavegames();
                int savedir_nr = saves[slotNum].id;
                name = SciEngine.Instance.GetSavegameName(savedir_nr);
                result = saveFileMan.RemoveSavefile(name);
            }
            else if (ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                // The file name may be already wrapped, so check both cases
                result = saveFileMan.RemoveSavefile(name);
                if (!result)
                {
                    string wrappedName = SciEngine.Instance.WrapFilename(name);
                    result = saveFileMan.RemoveSavefile(wrappedName);
                }
            }
            else
            {
                string wrappedName = SciEngine.Instance.WrapFilename(name);
                result = saveFileMan.RemoveSavefile(wrappedName);
            }

            DebugC(DebugLevels.File, "kFileIO(unlink): {0}", name);
            if (result)
                return Register.NULL_REG;
            return Register.Make(0, 2); // DOS - file not found error code
        }

        private static Register kFileIOReadString(EngineState s, int argc, StackPtr argv)
        {
            ushort maxsize = argv[1].ToUInt16();
            var buf = new byte[maxsize];
            ushort handle = argv[2].ToUInt16();
            DebugC(DebugLevels.File, "kFileIO(readString): {0}, {1}", handle, maxsize);
            int bytesRead = fgets_wrapper(s, buf, maxsize, handle);

            s._segMan.Memcpy(argv[0], new ByteAccess(buf), maxsize);
            return bytesRead != 0 ? argv[0] : Register.NULL_REG;
        }

        private static int fgets_wrapper(EngineState s, byte[] dest, int maxsize, int handle)
        {
            var f = GetFileFromHandle(s, (uint) handle);
            if (f == null)
                return 0;

            if (f._in == null)
            {
                throw new InvalidOperationException(
                    $"fgets_wrapper: Trying to read from file '{f._name}' opened for writing");
            }

            var @in = new StreamReader(f._in);
            var readBytes = 0;
            if (maxsize > 1)
            {
                var dst = @in.ReadLine();
                if (dst == null) return 0;

                readBytes = dst.Length; // FIXME: sierra sci returned byte count and didn't react on NUL characters
                // The returned string must not have an ending LF
                if (readBytes > 0)
                {
                    dst = dst + "\xA";
                }
                var data = dst.ToCharArray().Select(c => (byte) c).ToArray();
                Array.Copy(data, dest, data.Length);
            }
            else
            {
                dest[0] = 0;
            }

            DebugC(DebugLevels.File, "  . FGets'ed \"{0}\"", dest);
            return readBytes;
        }


        private static Register kFileIOWriteString(EngineState s, int argc, StackPtr argv)
        {
            int handle = argv[0].ToUInt16();
            var str = s._segMan.GetString(argv[1]).GetBytes();
            DebugC(DebugLevels.File, "kFileIO(writeString): {0}", handle);

            // Handle sciAudio calls in fanmade games here. sciAudio is an
            // external .NET library for playing MP3 files in fanmade games.
            // It runs in the background, and obtains sound commands from the
            // currently running game via text files (called "conductor files").
            // We skip creating these files, and instead handle the calls
            // directly. Since the sciAudio calls are only creating text files,
            // this is probably the most straightforward place to handle them.
            if (handle == VIRTUALFILE_HANDLE_SCIAUDIO)
            {
                var iter = s._executionStack[s._executionStack.Count - 2];
                SciEngine.Instance._audio.HandleFanmadeSciAudio(iter.sendp, s._segMan);
                return Register.NULL_REG;
            }

            FileHandle f = GetFileFromHandle(s, (uint) handle);

            if (f != null)
            {
                f._out.Write(str, 0, str.Length);
                if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                    return s.r_acc; // SCI0 semantics: no value returned
                return Register.NULL_REG;
            }

            if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                return s.r_acc; // SCI0 semantics: no value returned
            return Register.Make(0, 6); // DOS - invalid handle
        }

        private static Register kFileIOSeek(EngineState s, int argc, StackPtr argv)
        {
            ushort handle = argv[0].ToUInt16();
            ushort offset = (ushort) Math.Abs(argv[1].ToInt16()); // can be negative
            var whence = (SeekOrigin) argv[2].ToUInt16();
            DebugC(DebugLevels.File, "kFileIO(seek): {0}, {1}, {2}", handle, offset, whence);

            var f = GetFileFromHandle(s, handle);

            if (f?._in != null)
            {
                // Backward seeking isn't supported in zip file streams, thus adapt the
                // parameters accordingly if games ask for such a seek mode. A known
                // case where this is requested is the save file manager in Phantasmagoria
                if (whence != SeekOrigin.End)
                    return Register.Make(0, (ushort) f._in.Seek(offset, whence));

                whence = SeekOrigin.Begin;
                offset = (ushort) (f._in.Length - offset);

                return Register.Make(0, (ushort) f._in.Seek(offset, whence));
            }

            if (f?._out != null)
            {
                Error("kFileIOSeek: Unsupported seek operation on a writeable stream (offset: {0}, whence: {1})", offset,
                    whence);
            }

            return Register.SIGNAL_REG;
        }

        private static Register kFileIOFindFirst(EngineState s, int argc, StackPtr argv)
        {
            var mask = s._segMan.GetString(argv[0]);
            var buf = argv[1];
            int attr = argv[2].ToUInt16(); // We won't use this, Win32 might, though...
            DebugC(DebugLevels.File, "kFileIO(findFirst): {0}, 0x{1}", mask, attr);

            // We remove ".*". mask will get prefixed, so we will return all additional files for that gameid
            if (mask == "*.*")
                mask = "*";
            return s._dirseeker.FirstFile(mask, buf, s._segMan);
        }

        private static Register kFileIOFindNext(EngineState s, int argc, StackPtr argv)
        {
            DebugC(DebugLevels.File, "kFileIO(findNext)");
            return s._dirseeker.NextFile(s._segMan);
        }

        private static Register kFileIOExists(EngineState s, int argc, StackPtr argv)
        {
            string name = s._segMan.GetString(argv[0]);

            bool exists = false;

            if (SciEngine.Instance.GameId == SciGameId.PEPPER)
            {
                // HACK: Special case for Pepper's Adventure in Time
                // The game checks like crazy for the file CDAUDIO when entering the game menu.
                // On at least Windows that makes the engine slow down to a crawl and takes at least 1 second.
                // Should get solved properly by changing the code below. This here is basically for 1.8.0 release.
                // TODO: Fix this properly.
                if (name == "CDAUDIO")
                    return Register.NULL_REG;
            }

            // TODO: It may apparently be worth caching the existence of
            // phantsg.dir, and possibly even keeping it open persistently

            // Check for regular file
            exists = ServiceLocator.FileStorage.FileExists(name);

            // Check for a savegame with the name
            var saveFileMan = SciEngine.Instance.System.SaveFileManager;
            if (!exists)
                exists = saveFileMan.ListSavefiles(name).Length != 0;

            // Try searching for the file prepending "target-"
            var wrappedName = SciEngine.Instance.WrapFilename(name);
            if (!exists)
            {
                exists = saveFileMan.ListSavefiles(wrappedName).Length != 0;
            }

            // SCI2+ debug mode
            // TODO: if (DebugMan.isDebugChannelEnabled(kDebugLevelDebugMode)) {
//                if (!exists && name == "1.scr")		// PQ4
//                    exists = true;
//                if (!exists && name == "18.scr")	// QFG4
//                    exists = true;
//                if (!exists && name == "99.scr")	// GK1, KQ7
//                    exists = true;
//                if (!exists && name == "classes")	// GK2, SQ6, LSL7
//                    exists = true;
//            }

            // Special case for non-English versions of LSL5: The English version of
            // LSL5 calls kFileIO(), case K_FILEIO_OPEN for reading to check if
            // memory.drv exists (which is where the game's password is stored). If
            // it's not found, it calls kFileIO() again, case K_FILEIO_OPEN for
            // writing and creates a new file. Non-English versions call kFileIO(),
            // case K_FILEIO_FILE_EXISTS instead, and fail if memory.drv can't be
            // found. We create a default memory.drv file with no password, so that
            // the game can continue.
            if (!exists && name == "memory.drv")
            {
                // Create a new file, and write the bytes for the empty password
                // string inside
                byte[] defaultContent = {0xE9, 0xE9, 0xEB, 0xE1, 0x0D, 0x0A, 0x31, 0x30, 0x30, 0x30};
                using (var outFile = saveFileMan.OpenForSaving(wrappedName))
                {
                    for (int i = 0; i < 10; i++)
                        outFile.WriteByte(defaultContent[i]);
                    exists = true; // check whether we managed to create the file.
                }
            }

            // Special case for KQ6 Mac: The game checks for two video files to see
            // if they exist before it plays them. Since we support multiple naming
            // schemes for resource fork files, we also need to support that here in
            // case someone has a "HalfDome.bin" file, etc.
            if (!exists && SciEngine.Instance.GameId == SciGameId.KQ6 &&
                SciEngine.Instance.Platform == Platform.Macintosh &&
                (name == "HalfDome" || name == "Kq6Movie"))
                exists = MacResManager.Exists(name);

            DebugC(DebugLevels.File, "kFileIO(fileExists) {0} . {1}", name, exists);
            return Register.Make(0, exists);
        }

        private static Register kFileIORename(EngineState s, int argc, StackPtr argv)
        {
            var oldName = s._segMan.GetString(argv[0]);
            var newName = s._segMan.GetString(argv[1]);

            // SCI1.1 returns 0 on success and a DOS error code on fail. SCI32
            // returns -1 on fail. We just return -1 for all versions.
            if (SciEngine.Instance.SaveFileManager.RenameSavefile(oldName, newName))
                return Register.NULL_REG;
            return Register.SIGNAL_REG;
        }

#if ENABLE_SCI32
        private static Register kFileIOReadByte(EngineState s, int argc, StackPtr argv)
        {
            // Read the byte into the low byte of the accumulator
            var f = GetFileFromHandle(s, argv[0].ToUInt16());
            if (f == null)
                return Register.NULL_REG;
            return Register.Make(0, (ushort) ((s.r_acc.ToUInt16() & 0xff00) | f._in.ReadByte()));
        }

        private static Register kFileIOWriteByte(EngineState s, int argc, StackPtr argv)
        {
            var f = GetFileFromHandle(s, argv[0].ToUInt16());
            if (f != null)
                f._out.WriteByte((byte) (argv[1].ToUInt16() & 0xff));
            return s.r_acc; // FIXME: does this really not return anything?
        }

        private static Register kFileIOReadWord(EngineState s, int argc, StackPtr argv)
        {
            var f = GetFileFromHandle(s, argv[0].ToUInt16());
            if (f == null)
                return Register.NULL_REG;
            var br = new BinaryReader(f._in);
            return Register.Make(0, br.ReadUInt16());
        }

        private static Register kFileIOWriteWord(EngineState s, int argc, StackPtr argv)
        {
            var f = GetFileFromHandle(s, argv[0].ToUInt16());
            if (f != null)
            {
                var bw = new BinaryWriter(f._out);
                bw.WriteUInt16(argv[1].ToUInt16());
            }
            return s.r_acc; // FIXME: does this really not return anything?
        }

        private static Register kFileIOIsValidDirectory(EngineState s, int argc, StackPtr argv)
        {
            // Used in Torin's Passage and LSL7 to determine if the directory passed as
            // a parameter (usually the save directory) is valid. We always return true
            // here.
            return Register.TRUE_REG;
        }

        private static Register kFileIOChangeDirectory(EngineState s, int argc, StackPtr argv)
        {
            return kEmpty(s, argc, argv);
        }

        private static Register kMakeSaveCatName(EngineState s, int argc, StackPtr argv)
        {
            // Normally, this creates the name of the save catalogue/directory to save into.
            // First parameter is the string to save the result into. Second is a string
            // with game parameters. We don't have a use for this at all, as we have our own
            // savegame directory management, thus we always return an empty string.
            return argv[0];
        }

        private static Register kMakeSaveFileName(EngineState s, int argc, StackPtr argv)
        {
            SciArray outFileName = s._segMan.LookupArray(argv[0]);
            // argv[1] is the game name, which is not used by ScummVM
            short saveNo = argv[2].ToInt16();
            outFileName.FromString(SciEngine.Instance.GetSavegameName(saveNo + SaveIdShift));
            return argv[0];
        }

        private static Register kCD(EngineState s, int argc, StackPtr argv)
        {
            // TODO: Stub
            switch (argv[0].ToUInt16())
            {
                case 0:
                    if (argc == 1)
                    {
                        // Check if a disc is in the drive
                        return Register.TRUE_REG;
                    }
                    // Check if the specified disc is in the drive
                    // and return the current disc number. We just
                    // return the requested disc number.
                    return argv[1];
                case 1:
                    // Return the current CD number
                    return Register.Make(0, 1);
                default:
                    Warning("CD({0})", argv[0].ToUInt16());
                    break;
            }

            return Register.NULL_REG;
        }

        private static Register kSave(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kAutoSave(EngineState s, int argc, StackPtr argv)
        {
            // TODO
            // This is a timer callback, with 1 parameter: the timer object
            // (e.g. "timers").
            // It's used for auto-saving (i.e. save every X minutes, by checking
            // the elapsed time from the timer object)

            // This function has to return something other than 0 to proceed
            return Register.TRUE_REG;
        }
#endif

        private static Register kValidPath(EngineState s, int argc, StackPtr argv)
        {
            var path = s._segMan.GetString(argv[0]);

            Debug(3, "kValidPath({0}) . {1}", path, s.r_acc.Offset);

            // Always return true
            return Register.Make(0, 1);
        }

        private static Register kCheckSaveGame32(EngineState s, int argc, StackPtr argv)
        {
            string gameName = s._segMan.GetString(argv[0]);
            short saveNo = argv[1].ToInt16();
            string gameVersion = argv[2].IsNull ? string.Empty : s._segMan.GetString(argv[2]);

            var saves = File.ListSavegames();
            if (gameName == "Autosave" || gameName == "Autosv")
            {
                if (saveNo == 1)
                {
                    saveNo = NewGameId;
                }
            }
            else
            {
                saveNo += SaveIdShift;
            }

            SavegameDesc save;
            if (!File.FillSavegameDesc(SciEngine.Instance.GetSavegameName(saveNo), out save))
            {
                return Register.NULL_REG;
            }

            if (save.version < Savegame.MINIMUM_SAVEGAME_VERSION ||
                save.version > Savegame.CURRENT_SAVEGAME_VERSION ||
                save.gameVersion != gameVersion)
            {
                return Register.NULL_REG;
            }

            return Register.TRUE_REG;
        }

        private static Register kRestoreGame32(EngineState s, int argc, StackPtr argv)
        {
            bool isScummVMRestore = argv[0].IsNull;
            string gameName = "";
            short saveNo = argv[1].ToInt16();
            string gameVersion = argv[2].IsNull ? string.Empty : s._segMan.GetString(argv[2]);

            if (isScummVMRestore && saveNo == -1)
            {
                // ScummVM call, either from lancher or a patched Game::restore
                SciEngine.Instance._soundCmd.PauseAll(true);
                // TODO: vs SaveLoadChooser
                //GUI::SaveLoadChooser dialog(_("Restore game:"), _("Restore"), false);
                //saveNo = dialog.runModalWithCurrentTarget();
                SciEngine.Instance._soundCmd.PauseAll(false);

                if (saveNo < 0)
                {
                    // User cancelled restore
                    return s.r_acc;
                }
            }
            else
            {
                gameName = s._segMan.GetString(argv[0]);
            }

            if (gameName == "Autosave" || gameName == "Autosv")
            {
                if (saveNo == 0)
                {
                    // Autosave slot 0 is the autosave
                }
                else
                {
                    // Autosave slot 1 is a "new game" save
                    saveNo = NewGameId;
                }
            }
            else if (!isScummVMRestore)
            {
                // ScummVM save screen will give a pre-corrected save number, but native
                // save-load will not
                saveNo += SaveIdShift;
            }

            var saveFileMan = SciEngine.Instance.SaveFileManager;
            string filename = SciEngine.Instance.GetSavegameName(saveNo);
            var saveStream = saveFileMan.OpenForLoading(filename);

            if (saveStream == null)
            {
                Warning("Savegame #{0} not found", saveNo);
                return Register.NULL_REG;
            }
            using (saveStream)
            {
                Savegame.gamestate_restore(s, saveStream);
            }

            Savegame.gamestate_afterRestoreFixUp(s, saveNo);
            return Register.TRUE_REG;
        }

        private static Register kSaveGame32(EngineState s, int argc, StackPtr argv)
        {
            throw new NotImplementedException();
        }
    }
}