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

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private const int _K_FILE_MODE_OPEN_OR_CREATE = 0;
        private const int _K_FILE_MODE_OPEN_OR_FAIL = 1;
        private const int _K_FILE_MODE_CREATE = 2;

        // We assume that scripts give us savegameId 0.99 for creating a new save slot
        //  and savegameId 100.199 for existing save slots. Refer to kfile.cpp
        private const int SAVEGAMEID_OFFICIALRANGE_START = 100;
        private const int SAVEGAMEID_OFFICIALRANGE_END = 199;


        private const int VIRTUALFILE_HANDLE = 200;

        /// <summary>
        /// Maximum number of savegames.
        /// </summary>
        private const int MAX_SAVEGAME_NR = 20;
        /// <summary>
        /// Maximum length of a savegame name (including terminator character).
        /// </summary>
        private const int SCI_MAX_SAVENAME_LENGTH = 0x24;


        enum DeviceInfo
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
                        throw new InvalidOperationException("kCheckFreeSpace: called with unknown sub-op {argv[1].ToUInt16()}");
                }
            }

            string path = s._segMan.GetString(argv[0]);

            Debug(3, $"kCheckFreeSpace({path}");

            // We simply always pretend that there is enough space. The alternative
            // would be to write a big test file, which is not nice on systems where
            // doing so is very slow.
            return Register.Make(0, 1);
        }

        private static Register kCheckSaveGame(EngineState s, int argc, StackPtr argv)
        {
            string game_id = s._segMan.GetString(argv[0]);
            ushort virtualId = argv[1].ToUInt16();

            Debug(3, $"kCheckSaveGame({game_id}, {virtualId})");

            var saves = File.ListSavegames();

            // we allow 0 (happens in QfG2 when trying to restore from an empty saved game list) and return false in that case
            if (virtualId == 0)
                return Register.NULL_REG;

            int savegameId = 0;
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

            int savegameNr = File.FindSavegame(saves, (short)savegameId);
            if (savegameNr == -1)
                return Register.NULL_REG;

            // Check for compatible savegame version
            int ver = saves[savegameNr].version;
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

            var mode = (DeviceInfo)argv[0].ToUInt16();

            switch (mode)
            {
                case DeviceInfo.GET_DEVICE:
                    {
                        string input_str = s._segMan.GetString(argv[1]);

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
                        string path1_s = s._segMan.GetString(argv[1]);
                        string path2_s = s._segMan.GetString(argv[2]);
                        Debug(3, $"DeviceInfo.PATHS_EQUAL({path1_s},{path2_s})");

                        Register.Make(0, string.Equals(path2_s, path1_s, StringComparison.Ordinal));
                    }
                    break;

                case DeviceInfo.IS_FLOPPY:
                    {
                        string input_str = s._segMan.GetString(argv[1]);
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
                        string game_prefix = s._segMan.GetString(argv[2]);
                        s._segMan.Strcpy(argv[1], "__throwaway");
                        Debug(3, $"DeviceInfo.GET_SAVECAT_NAME({game_prefix}) . __throwaway");
                    }

                    break;
                case DeviceInfo.GET_SAVEFILE_NAME:
                    {
                        string game_prefix = s._segMan.GetString(argv[2]);
                        int virtualId = argv[3].ToUInt16();
                        s._segMan.Strcpy(argv[1], "__throwaway");
                        Debug(3, $"DeviceInfo.GET_SAVEFILE_NAME({game_prefix},{virtualId}) . __throwaway");
                        if ((virtualId < SAVEGAMEID_OFFICIALRANGE_START) || (virtualId > SAVEGAMEID_OFFICIALRANGE_END))
                            throw new InvalidOperationException("kDeviceInfo(deleteSave): invalid savegame ID specified");
                        int savegameId = virtualId - SAVEGAMEID_OFFICIALRANGE_START;
                        var saves = File.ListSavegames();
                        if (File.FindSavegame(saves, (short)savegameId) != -1)
                        {
                            // Confirmed that this id still lives...
                            string filename = SciEngine.Instance.GetSavegameName(savegameId);
                            ISaveFileManager saveFileMan = SciEngine.Instance.SaveFileManager;
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
            string game_id = s._segMan.GetString(argv[0]);

            Debug(3, $"kGetSaveFiles({game_id})");

            // Scripts ask for current save files, we can assume that if afterwards they ask us to create a new slot they really
            //  mean new slot instead of overwriting the old one
            s._lastSaveVirtualId = SAVEGAMEID_OFFICIALRANGE_START;

            var saves = File.ListSavegames();
            uint totalSaves = (uint)Math.Min(saves.Count, MAX_SAVEGAME_NR);

            StackPtr? slot = s._segMan.DerefRegPtr(argv[2], (int)totalSaves);

            if (slot == null)
            {
                Warning($"kGetSaveFiles: {argv[2]} invalid or too small to hold slot data");
                totalSaves = 0;
            }
            var sl = slot.Value;

            uint bufSize = (totalSaves * SCI_MAX_SAVENAME_LENGTH) + 1;
            var saveNames = new byte[bufSize];
            var ptr = 0;

            for (int i = 0; i < totalSaves; i++)
            {
                sl[i] = Register.Make(0, (ushort)(saves[i].id + SAVEGAMEID_OFFICIALRANGE_START)); // Store the virtual savegame ID (see above)
                Array.Copy(saves[i].name.GetBytes(), 0, saveNames, ptr, saves[i].name.Length);
                ptr += SCI_MAX_SAVENAME_LENGTH;
            }

            saveNames[ptr] = 0; // Terminate list

            s._segMan.Memcpy(argv[1], new ByteAccess(saveNames), (int)bufSize);

            return Register.Make(0, (ushort)totalSaves);
        }

        private static Register kRestoreGame(EngineState s, int argc, StackPtr argv)
        {
            string game_id = !argv[0].IsNull ? s._segMan.GetString(argv[0]) : "";
            short savegameId = argv[1].ToInt16();
            bool pausedMusic = false;

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
                ISaveFileManager saveFileMan = SciEngine.Instance.SaveFileManager;
                string filename = SciEngine.Instance.GetSavegameName(savegameId);

                using (var @in = saveFileMan.OpenForLoading(filename))
                {
                    // found a savegame file
                    Savegame.gamestate_restore(s, @in);
                }

                switch (SciEngine.Instance.GameId)
                {
                    case SciGameId.MOTHERGOOSE:
                        // WORKAROUND: Mother Goose SCI0
                        //  Script 200 / rm200::newRoom will set global C5h directly right after creating a child to the
                        //   current number of children plus 1.
                        //  We can't trust that global, that's why we set the actual savedgame id right here directly after
                        //   restoring a saved game.
                        //  If we didn't, the game would always save to a new slot
                        s.variables[Vm.VAR_GLOBAL][0xC5] = Register.SetOffset(s.variables[Vm.VAR_GLOBAL][0xC5], (ushort)(SAVEGAMEID_OFFICIALRANGE_START + savegameId));
                        break;
                    case SciGameId.MOTHERGOOSE256:
                        // WORKAROUND: Mother Goose SCI1/SCI1.1 does some weird things for
                        //  saving a previously restored game.
                        // We set the current savedgame-id directly and remove the script
                        //  code concerning this via script patch.
                        s.variables[Vm.VAR_GLOBAL][0xB3] = Register.SetOffset(s.variables[Vm.VAR_GLOBAL][0xB3], (ushort)(SAVEGAMEID_OFFICIALRANGE_START + savegameId));
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
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(257 >> 8, 257 & 0xFF, Graphics.MenuAttribute.ENABLED, Register.TRUE_REG);    // Sierra . About Jones
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(258 >> 8, 258 & 0xFF, Graphics.MenuAttribute.ENABLED, Register.TRUE_REG);    // Sierra . Help
                                                                                                                                                    // The rest are normally enabled from room1::init
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(769 >> 8, 769 & 0xFF, Graphics.MenuAttribute.ENABLED, Register.TRUE_REG);    // Options . Delete current player
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(513 >> 8, 513 & 0xFF, Graphics.MenuAttribute.ENABLED, Register.TRUE_REG);    // Game . Save Game
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(515 >> 8, 515 & 0xFF, Graphics.MenuAttribute.ENABLED, Register.TRUE_REG);    // Game . Restore Game
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(1025 >> 8, 1025 & 0xFF, Graphics.MenuAttribute.ENABLED, Register.TRUE_REG);  // Status . Statistics
                        SciEngine.Instance._gfxMenu.KernelSetAttribute(1026 >> 8, 1026 & 0xFF, Graphics.MenuAttribute.ENABLED, Register.TRUE_REG);  // Status . Goals
                        break;
                    default:
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
            short virtualId = argv[1].ToInt16();
            short savegameId = -1;
            string game_description;
            string version = string.Empty;

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
                    savegameId = (short)(virtualId - SAVEGAMEID_OFFICIALRANGE_START);
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

            string filename = SciEngine.Instance.GetSavegameName(savegameId);
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

            // TODO: debugC(kDebugLevelFile, "kGetCWD() . %s", "/");

            return argv[0];
        }

        private static Register kFileIO(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            throw new InvalidOperationException("not supposed to call this");
        }

        private static Register kFileIOOpen(EngineState s, int argc, StackPtr argv)
        {
            string name = s._segMan.GetString(argv[0]);

            // SCI32 can call K_FILEIO_OPEN with only one argument. It seems to
            // just be checking if it exists.
            int mode = (argc < 2) ? (int)_K_FILE_MODE_OPEN_OR_FAIL : argv[1].ToUInt16();
            bool unwrapFilename = true;

            // SQ4 floppy prepends /\ to the filenames
            if (name.StartsWith("/\\"))
            {
                name = name.Remove(0, 2);
            }

            // SQ4 floppy attempts to update the savegame index file sq4sg.dir when
            // deleting saved games. We don't use an index file for saving or loading,
            // so just stop the game from modifying the file here in order to avoid
            // having it saved in the ScummVM save directory.
            if (name == "sq4sg.dir")
            {
                // TODO: debugC(kDebugLevelFile, "Not opening unused file sq4sg.dir");
                return Register.SIGNAL_REG;
            }

            if (string.IsNullOrEmpty(name))
            {
                // Happens many times during KQ1 (e.g. when typing something)
                // TODO: debugC(kDebugLevelFile, "Attempted to open a file with an empty filename");
                return Register.SIGNAL_REG;
            }
            // TODO: debugC(kDebugLevelFile, "kFileIO(open): %s, 0x%x", name.c_str(), mode);

#if ENABLE_SCI32
            if (name == PHANTASMAGORIA_SAVEGAME_INDEX)
            {
                if (s._virtualIndexFile)
                {
                    return make_reg(0, VIRTUALFILE_HANDLE);
                }
                else {
                    Common::String englishName = SciEngine.Instance.getSciLanguageString(name, K_LANG_ENGLISH);
                    Common::String wrappedName = SciEngine.Instance.wrapFilename(englishName);
                    if (!SciEngine.Instance.getSaveFileManager().listSavefiles(wrappedName).empty())
                    {
                        s._virtualIndexFile = new VirtualIndexFile(wrappedName);
                        return make_reg(0, VIRTUALFILE_HANDLE);
                    }
                }
            }

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
            if (SciEngine.Instance.getGameId() == SciGameId.SHIVERS && name.hasSuffix(".SG"))
            {
                if (mode == _K_FILE_MODE_OPEN_OR_CREATE || mode == _K_FILE_MODE_CREATE)
                {
                    // Game scripts are trying to create a file with the save
                    // description, stop them here
                    debugC(kDebugLevelFile, "Not creating unused file %s", name.c_str());
                    return SIGNAL_REG;
                }
                else if (mode == _K_FILE_MODE_OPEN_OR_FAIL)
                {
                    // Create a virtual file containing the save game description
                    // and slot number, as the game scripts expect.
                    int slotNumber;
                    sscanf(name.c_str(), "%d.SG", &slotNumber);

                    Common::Array<SavegameDesc> saves;
                    listSavegames(saves);
                    int savegameNr = findSavegame(saves, slotNumber - SAVEGAMEID_OFFICIALRANGE_START);

                    if (!s._virtualIndexFile)
                    {
                        // Make the virtual file buffer big enough to avoid having it grow dynamically.
                        // 50 bytes should be more than enough.
                        s._virtualIndexFile = new VirtualIndexFile(50);
                    }

                    s._virtualIndexFile.seek(0, SEEK_SET);
                    s._virtualIndexFile.write(saves[savegameNr].name, strlen(saves[savegameNr].name));
                    s._virtualIndexFile.write("\0", 1);
                    s._virtualIndexFile.write("\0", 1);   // Spot description (empty)
                    s._virtualIndexFile.seek(0, SEEK_SET);
                    return make_reg(0, VIRTUALFILE_HANDLE);
                }
            }
#endif

            // QFG import rooms get a virtual filelisting instead of an actual one
            if (SciEngine.Instance.InQfGImportRoom != 0)
            {
                // We need to find out what the user actually selected, "savedHeroes" is
                // already destroyed when we get here. That's why we need to remember
                // selection via kDrawControl.
                name = s._dirseeker.GetVirtualFilename(s._chosenQfGImportItem);
                unwrapFilename = false;
            }

            return file_open(s, name, mode, unwrapFilename);
        }

        private static Register file_open(EngineState s, string filename, int mode, bool unwrapFilename)
        {
            string englishName = SciEngine.Instance.GetSciLanguageString(filename, Language.ENGLISH).ToLower();

            string wrappedName = unwrapFilename ? SciEngine.Instance.WrapFilename(englishName) : englishName;
            Stream inFile = null;
            Stream outFile = null;
            ISaveFileManager saveFileMan = SciEngine.Instance.SaveFileManager;

            bool isCompressed = true;
            SciGameId gameId = SciEngine.Instance.GameId;
            if ((gameId == SciGameId.QFG1 || gameId == SciGameId.QFG1VGA || gameId == SciGameId.QFG2 || gameId == SciGameId.QFG3)
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

                // TODO: if (inFile == null)
                // TODO: debugC(kDebugLevelFile, "  . file_open(_K_FILE_MODE_OPEN_OR_FAIL): failed to open file '%s'", englishName.c_str());
            }
            else if (mode == _K_FILE_MODE_CREATE)
            {
                // Create the file, destroying any content it might have had
                outFile = saveFileMan.OpenForSaving(wrappedName, isCompressed);
                // TODO:
                //if (outFile==null)
                //    debugC(kDebugLevelFile, "  . file_open(_K_FILE_MODE_CREATE): failed to create file '%s'", englishName.c_str());
            }
            else if (mode == _K_FILE_MODE_OPEN_OR_CREATE)
            {
                // Try to open file, create it if it doesn't exist
                outFile = saveFileMan.OpenForSaving(wrappedName, isCompressed);
                // TODO:
                //if (outFile==null)
                //    debugC(kDebugLevelFile, "  . file_open(_K_FILE_MODE_CREATE): failed to create file '%s'", englishName.c_str());

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
            { // Failed
              // TODO: debugC(kDebugLevelFile, "  . file_open() failed");
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

            // TODO: debugC(kDebugLevelFile, "  . opened file '%s' with handle %d", englishName.c_str(), handle);
            return Register.Make(0, (ushort)handle);
        }

        private static Register kFileIOClose(EngineState s, int argc, StackPtr argv)
        {
            // TODO: debugC(kDebugLevelFile, "kFileIO(close): %d", argv[0].toUint16());

            if (argv[0] == Register.SIGNAL_REG)
                return s.r_acc;

            ushort handle = argv[0].ToUInt16();

#if ENABLE_SCI32
            if (handle == VIRTUALFILE_HANDLE)
            {
                s._virtualIndexFile.close();
                return SIGNAL_REG;
            }
#endif

            FileHandle f = GetFileFromHandle(s, handle);
            if (f != null)
            {
                f.Close();
                if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                    return s.r_acc;    // SCI0 semantics: no value returned
                return Register.SIGNAL_REG;
            }

            if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                return s.r_acc;    // SCI0 semantics: no value returned
            return Register.NULL_REG;
        }

        private static Register kFileIOReadRaw(EngineState s, int argc, StackPtr argv)
        {
            ushort handle = argv[0].ToUInt16();
            ushort size = argv[2].ToUInt16();
            int bytesRead = 0;
            byte[] buf = new byte[size];
            // TODO: debugC(kDebugLevelFile, "kFileIO(readRaw): %d, %d", handle, size);

#if ENABLE_SCI32
                        if (handle == VIRTUALFILE_HANDLE)
                        {
                            bytesRead = s._virtualIndexFile.read(buf, size);
                        }
                        else {
#endif
            FileHandle f = GetFileFromHandle(s, handle);
            if (f != null)
                bytesRead = f._in.Read(buf, 0, size);
#if ENABLE_SCI32
                        }
#endif

            // TODO: What happens if less bytes are read than what has
            // been requested? (i.e. if bytesRead is non-zero, but still
            // less than size)
            if (bytesRead > 0)
                s._segMan.Memcpy(argv[1], new ByteAccess(buf), size);

            return Register.Make(0, (ushort)bytesRead);
        }

        private static FileHandle GetFileFromHandle(EngineState s, uint handle)
        {
            if (handle == 0 || handle == VIRTUALFILE_HANDLE)
            {
                Error($"Attempt to use invalid file handle ({handle})");
                return null;
            }

            if ((handle >= s._fileHandles.Length) || !s._fileHandles[handle].IsOpen)
            {
                Warning($"Attempt to use invalid/unused file handle {handle}");
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
            // TODO: debugC(kDebugLevelFile, "kFileIO(writeRaw): %d, %d", handle, size);

#if ENABLE_SCI32
                        if (handle == VIRTUALFILE_HANDLE)
                        {
                            s._virtualIndexFile.write(buf, size);
                            success = true;
                        }
                        else {
#endif
            FileHandle f = GetFileFromHandle(s, handle);
            if (f != null)
            {
                f._out.Write(buf, 0, size);
                success = true;
            }
#if ENABLE_SCI32
                        }
#endif

            if (success)
                return Register.NULL_REG;
            return Register.Make(0, 6); // DOS - invalid handle
        }

        private static Register kFileIOUnlink(EngineState s, int argc, StackPtr argv)
        {
            var name = s._segMan.GetString(argv[0]);
            var saveFileMan = SciEngine.Instance.SaveFileManager;
            bool result = false;

            // SQ4 floppy prepends /\ to the filenames
            if (name.StartsWith("/\\"))
            {
                name = name.Remove(0, 2);
            }

            // Special case for SQ4 floppy: This game has hardcoded names for all of
            // its savegames, and they are all named "sq4sg.xxx", where xxx is the
            // slot. We just take the slot number here, and delete the appropriate
            // save game.
            if (name.StartsWith("sq4sg."))
            {
                // Special handling for SQ4... get the slot number and construct the
                // save game name.
                int slotNum = int.Parse(name.Substring(name.Length - 3, 3));
                var saves = File.ListSavegames();
                int savedir_nr = saves[slotNum].id;
                name = SciEngine.Instance.GetSavegameName(savedir_nr);

                try
                {
                    saveFileMan.RemoveSavefile(name);
                    result = true;
                }
                catch (Exception) { }

            }
            else if (ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                // The file name may be already wrapped, so check both cases
                try
                {
                    saveFileMan.RemoveSavefile(name);
                    result = true;
                }
                catch (Exception) { }

                if (!result)
                {
                    string wrappedName = SciEngine.Instance.WrapFilename(name);
                    try
                    {
                        saveFileMan.RemoveSavefile(wrappedName);
                        result = true;
                    }
                    catch (Exception) { }
                }

# if ENABLE_SCI32
                if (name == PHANTASMAGORIA_SAVEGAME_INDEX)
                {
                    s._virtualIndexFile = 0;
                }
#endif
            }
            else
            {
                string wrappedName = SciEngine.Instance.WrapFilename(name);
                try
                {
                    saveFileMan.RemoveSavefile(wrappedName);
                    result = true;
                }
                catch (Exception) { }
            }

            // TODO: debugC(kDebugLevelFile, "kFileIO(unlink): %s", name.c_str());
            if (result)
                return Register.NULL_REG;
            return Register.Make(0, 2); // DOS - file not found error code
        }

        private static Register kFileIOReadString(EngineState s, int argc, StackPtr argv)
        {
            ushort maxsize = argv[1].ToUInt16();
            var buf = new byte[maxsize];
            ushort handle = argv[2].ToUInt16();
            // TODO:      debugC(kDebugLevelFile, "kFileIO(readString): %d, %d", handle, maxsize);
            uint bytesRead;

#if ENABLE_SCI32
                        if (handle == VIRTUALFILE_HANDLE)
                            bytesRead = s._virtualIndexFile.readLine(buf, maxsize);
                        else
#endif
            bytesRead = (uint)fgets_wrapper(s, buf, maxsize, handle);

            s._segMan.Memcpy(argv[0], new ByteAccess(buf), maxsize);
            return bytesRead != 0 ? argv[0] : Register.NULL_REG;
        }

        private static int fgets_wrapper(EngineState s, byte[] dest, int maxsize, int handle)
        {
            FileHandle f = GetFileFromHandle(s, (uint)handle);
            if (f == null)
                return 0;

            if (f._in == null)
            {
                throw new InvalidOperationException($"fgets_wrapper: Trying to read from file '{f._name}' opened for writing");
            }

            var @in = new StreamReader(f._in);
            int readBytes = 0;
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
                var data = dst.ToCharArray().Select(c => (byte)c).ToArray();
                Array.Copy(data, dest, data.Length);
            }
            else
            {
                dest[0] = 0;
            }

            // TODO: debugC(kDebugLevelFile, "  . FGets'ed \"%s\"", dest);
            return readBytes;
        }


        private static Register kFileIOWriteString(EngineState s, int argc, StackPtr argv)
        {
            int handle = argv[0].ToUInt16();
            var str = s._segMan.GetString(argv[1]);
            // TODO: DebugC(kDebugLevelFile, "kFileIO(writeString): %d", handle);

            // Handle sciAudio calls in fanmade games here. sciAudio is an
            // external .NET library for playing MP3 files in fanmade games.
            // It runs in the background, and obtains sound commands from the
            // currently running game via text files (called "conductor files").
            // We skip creating these files, and instead handle the calls
            // directly. Since the sciAudio calls are only creating text files,
            // this is probably the most straightforward place to handle them.
            if (handle == 0xFFFF && str.StartsWith("(sciAudio"))
            {
                System.Collections.Generic.List<ExecStack> iter = s._executionStack;
                SciEngine.Instance._audio.HandleFanmadeSciAudio(iter[iter.Count - 2].sendp, s._segMan);
                return Register.NULL_REG;
            }

#if ENABLE_SCI32
                        if (handle == VIRTUALFILE_HANDLE)
                        {
                            s._virtualIndexFile.write(str.c_str(), str.size());
                            return NULL_REG;
                        }
#endif

            FileHandle f = GetFileFromHandle(s, (uint)handle);

            if (f != null)
            {
                var sw = new StreamWriter(f._out);
                sw.Write(str);
                sw.Flush();
                if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                    return s.r_acc;    // SCI0 semantics: no value returned
                return Register.NULL_REG;
            }

            if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                return s.r_acc;    // SCI0 semantics: no value returned
            return Register.Make(0, 6); // DOS - invalid handle
        }

        private static Register kFileIOSeek(EngineState s, int argc, StackPtr argv)
        {
            ushort handle = argv[0].ToUInt16();
            ushort offset = (ushort)Math.Abs(argv[1].ToInt16()); // can be negative
            SeekOrigin whence = (SeekOrigin)argv[2].ToUInt16();
            // TODO: debugC(kDebugLevelFile, "kFileIO(seek): %d, %d, %d", handle, offset, whence);

#if ENABLE_SCI32
                        if (handle == VIRTUALFILE_HANDLE)
                            return make_reg(0, s._virtualIndexFile.seek(offset, whence));
#endif

            FileHandle f = GetFileFromHandle(s, handle);

            if (f != null && f._in != null)
            {
                // Backward seeking isn't supported in zip file streams, thus adapt the
                // parameters accordingly if games ask for such a seek mode. A known
                // case where this is requested is the save file manager in Phantasmagoria
                if (whence == SeekOrigin.End)
                {
                    whence = SeekOrigin.Begin;
                    offset = (ushort)(f._in.Length - offset);
                }

                return Register.Make(0, (ushort)f._in.Seek(offset, whence));
            }
            else if (f != null && f._out != null)
            {
                Error("kFileIOSeek: Unsupported seek operation on a writeable stream (offset: %d, whence: %d)", offset, whence);
            }

            return Register.SIGNAL_REG;
        }

        private static Register kFileIOFindFirst(EngineState s, int argc, StackPtr argv)
        {
            string mask = s._segMan.GetString(argv[0]);
            Register buf = argv[1];
            int attr = argv[2].ToUInt16(); // We won't use this, Win32 might, though...
            // TODO: debugC(kDebugLevelFile, "kFileIO(findFirst): %s, 0x%x", mask.c_str(), attr);

            // We remove ".*". mask will get prefixed, so we will return all additional files for that gameid
            if (mask == "*.*")
                mask = "*";
            return s._dirseeker.FirstFile(mask, buf, s._segMan);
        }

        private static Register kFileIOFindNext(EngineState s, int argc, StackPtr argv)
        {
            // TODO: debugC(kDebugLevelFile, "kFileIO(findNext)");
            return s._dirseeker.NextFile(s._segMan);
        }

        private static Register kFileIOExists(EngineState s, int argc, StackPtr argv)
        {
            var name = s._segMan.GetString(argv[0]);

#if ENABLE_SCI32
                        // Cache the file existence result for the Phantasmagoria
                        // save index file, as the game scripts keep checking for
                        // its existence.
                        if (name == PHANTASMAGORIA_SAVEGAME_INDEX && s._virtualIndexFile)
                            return TRUE_REG;
#endif

            bool exists = false;

            // Check for regular file
            exists = ServiceLocator.FileStorage.FileExists(name);

            // Check for a savegame with the name
            var saveFileMan = SciEngine.Instance.SaveFileManager;
            if (!exists)
                exists = saveFileMan.ListSavefiles(name).Length > 0;

            // Try searching for the file prepending "target-"
            var wrappedName = SciEngine.Instance.WrapFilename(name);
            if (!exists)
            {
                exists = saveFileMan.ListSavefiles(wrappedName).Length > 0;
            }

            // SCI2+ debug mode
            // TODO: if (DebugMan.isDebugChannelEnabled(kDebugLevelDebugMode))
            //{
            //    if (!exists && name == "1.scr")     // PQ4
            //        exists = true;
            //    if (!exists && name == "18.scr")    // QFG4
            //        exists = true;
            //    if (!exists && name == "99.scr")    // GK1, KQ7
            //        exists = true;
            //    if (!exists && name == "classes")   // GK2, SQ6, LSL7
            //        exists = true;
            //}

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
                byte[] defaultContent = { 0xE9, 0xE9, 0xEB, 0xE1, 0x0D, 0x0A, 0x31, 0x30, 0x30, 0x30 };
                using (var outFile = saveFileMan.OpenForSaving(wrappedName))
                {
                    for (int i = 0; i < 10; i++)
                        outFile.WriteByte(defaultContent[i]);
                    exists = true;   // check whether we managed to create the file.
                }
            }

            // Special case for KQ6 Mac: The game checks for two video files to see
            // if they exist before it plays them. Since we support multiple naming
            // schemes for resource fork files, we also need to support that here in
            // case someone has a "HalfDome.bin" file, etc.
            if (!exists && SciEngine.Instance.GameId == SciGameId.KQ6 && SciEngine.Instance.Platform == Core.IO.Platform.Macintosh &&
                (name == "HalfDome" || name == "Kq6Movie"))
            {
                throw new NotImplementedException();
                //TODO: exists = Common::MacResManager::exists(name);
            }

            // TODO: debugC(kDebugLevelFile, "kFileIO(fileExists) %s . %d", name.c_str(), exists);
            return Register.Make(0, exists);
        }

        private static Register kFileIORename(EngineState s, int argc, StackPtr argv)
        {
            string oldName = s._segMan.GetString(argv[0]);
            string newName = s._segMan.GetString(argv[1]);

            // SCI1.1 returns 0 on success and a DOS error code on fail. SCI32
            // returns -1 on fail. We just return -1 for all versions.
            if (SciEngine.Instance.SaveFileManager.RenameSavefile(oldName, newName))
                return Register.NULL_REG;
            return Register.SIGNAL_REG;
        }

        private static Register kValidPath(EngineState s, int argc, StackPtr argv)
        {
            string path = s._segMan.GetString(argv[0]);

            Debug(3, "kValidPath({0}) . {1}", path, s.r_acc.Offset);

            // Always return true
            return Register.Make(0, 1);
        }

    }
}
