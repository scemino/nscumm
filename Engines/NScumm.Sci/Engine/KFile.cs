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

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private const int _K_FILE_MODE_OPEN_OR_CREATE = 0;
        private const int _K_FILE_MODE_OPEN_OR_FAIL = 1;
        private const int _K_FILE_MODE_CREATE = 2;

        private const int VIRTUALFILE_HANDLE = 200;

        private static Register kGetSaveDir(EngineState s, int argc, StackPtr? argv)
        {
# if ENABLE_SCI32
            // SCI32 uses a parameter here. It is used to modify a string, stored in a
            // global variable, so that game scripts store the save directory. We
            // don't really set a save game directory, thus not setting the string to
            // anything is the correct thing to do here.
            //if (argc > 0)
            //	warning("kGetSaveDir called with %d parameter(s): %04x:%04x", argc, PRINT_REG(argv[0]));
#endif
            return s._segMan.SaveDirPtr;
        }

        /// <summary>
        /// Writes the cwd to the supplied address and returns the address in acc.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        private static Register kGetCWD(EngineState s, int argc, StackPtr? argv)
        {
            // We do not let the scripts see the file system, instead pretending
            // we are always in the same directory.
            // TODO/FIXME: Is "/" a good value? Maybe "" or "." or "C:\" are better?
            s._segMan.Strcpy(argv.Value[0], "/");

            // TODO: debugC(kDebugLevelFile, "kGetCWD() . %s", "/");

            return argv.Value[0];
        }

        private static Register kFileIO(EngineState s, int argc, StackPtr? argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            throw new InvalidOperationException("not supposed to call this");
        }

        private static Register kFileIOOpen(EngineState s, int argc, StackPtr? argv)
        {
            string name = s._segMan.GetString(argv.Value[0]);

            // SCI32 can call K_FILEIO_OPEN with only one argument. It seems to
            // just be checking if it exists.
            int mode = (argc < 2) ? (int)_K_FILE_MODE_OPEN_OR_FAIL : argv.Value[1].ToUInt16();
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

# if ENABLE_SCI32
            if (name == PHANTASMAGORIA_SAVEGAME_INDEX)
            {
                if (s._virtualIndexFile)
                {
                    return make_reg(0, VIRTUALFILE_HANDLE);
                }
                else {
                    Common::String englishName = g_sci.getSciLanguageString(name, K_LANG_ENGLISH);
                    Common::String wrappedName = g_sci.wrapFilename(englishName);
                    if (!g_sci.getSaveFileManager().listSavefiles(wrappedName).empty())
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
            if (g_sci.getGameId() == SciGameId.SHIVERS && name.hasSuffix(".SG"))
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
                    var path = ScummHelper.LocatePath(SciEngine.Instance.Directory, englishName);
                    inFile = ServiceLocator.FileStorage.OpenFileRead(path);
                }

                // TODO:
                //if (inFile == null)
                //    debugC(kDebugLevelFile, "  . file_open(_K_FILE_MODE_OPEN_OR_FAIL): failed to open file '%s'", englishName.c_str());
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
            else {
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

        private static Register kFileIOClose(EngineState s, int argc, StackPtr? argv)
        {
            // TODO: debugC(kDebugLevelFile, "kFileIO(close): %d", argv[0].toUint16());

            if (argv.Value[0] == Register.SIGNAL_REG)
                return s.r_acc;

            ushort handle = argv.Value[0].ToUInt16();

#if ENABLE_SCI32
                        if (handle == VIRTUALFILE_HANDLE)
                        {
                            s._virtualIndexFile.close();
                            return SIGNAL_REG;
                        }
#endif

            FileHandle f = GetFileFromHandle(s, handle);
            if (f!=null)
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

        private static Register kFileIOReadRaw(EngineState s, int argc, StackPtr? argv)
        {
            ushort handle = argv.Value[0].ToUInt16();
            ushort size = argv.Value[2].ToUInt16();
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
                s._segMan.Memcpy(argv.Value[1], buf, size);

            return Register.Make(0, (ushort)bytesRead);
        }

        private static FileHandle GetFileFromHandle(EngineState s, uint handle)
        {
            if (handle == 0 || handle == VIRTUALFILE_HANDLE)
            {
                throw new NotImplementedException($"Attempt to use invalid file handle ({handle})");
            }

            if ((handle >= s._fileHandles.Length) || !s._fileHandles[handle].IsOpen)
            {
                // TODO: warning("Attempt to use invalid/unused file handle %d", handle);
                return null;
            }

            return s._fileHandles[handle];
        }

        private static Register kFileIOWriteRaw(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
            //            uint16 handle = argv[0].toUint16();
            //            uint16 size = argv[2].toUint16();
            //            char* buf = new char[size];
            //            bool success = false;
            //            s._segMan.memcpy((byte*)buf, argv[1], size);
            //            debugC(kDebugLevelFile, "kFileIO(writeRaw): %d, %d", handle, size);

            //#if ENABLE_SCI32
            //            if (handle == VIRTUALFILE_HANDLE)
            //            {
            //                s._virtualIndexFile.write(buf, size);
            //                success = true;
            //            }
            //            else {
            //#endif
            //            FileHandle* f = getFileFromHandle(s, handle);
            //                if (f)
            //                {
            //                    f._out.write(buf, size);
            //                    success = true;
            //                }
            //# if ENABLE_SCI32
            //            }
            //#endif

            //            delete[] buf;
            //            if (success)
            //                return NULL_REG;
            //            return make_reg(0, 6); // DOS - invalid handle
        }

        private static Register kFileIOUnlink(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
            //            Common::String name = s._segMan.getString(argv[0]);
            //            Common::SaveFileManager* saveFileMan = g_sci.getSaveFileManager();
            //            bool result;

            //            // SQ4 floppy prepends /\ to the filenames
            //            if (name.hasPrefix("/\\"))
            //            {
            //                name.deleteChar(0);
            //                name.deleteChar(0);
            //            }

            //            // Special case for SQ4 floppy: This game has hardcoded names for all of
            //            // its savegames, and they are all named "sq4sg.xxx", where xxx is the
            //            // slot. We just take the slot number here, and delete the appropriate
            //            // save game.
            //            if (name.hasPrefix("sq4sg."))
            //            {
            //                // Special handling for SQ4... get the slot number and construct the
            //                // save game name.
            //                int slotNum = atoi(name.c_str() + name.size() - 3);
            //                Common::Array<SavegameDesc> saves;
            //                listSavegames(saves);
            //                int savedir_nr = saves[slotNum].id;
            //                name = g_sci.getSavegameName(savedir_nr);
            //                result = saveFileMan.removeSavefile(name);
            //            }
            //            else if (getSciVersion() >= SCI_VERSION_2)
            //            {
            //                // The file name may be already wrapped, so check both cases
            //                result = saveFileMan.removeSavefile(name);
            //                if (!result)
            //                {
            //                    const Common::String wrappedName = g_sci.wrapFilename(name);
            //                    result = saveFileMan.removeSavefile(wrappedName);
            //                }

            //# ifdef ENABLE_SCI32
            //                if (name == PHANTASMAGORIA_SAVEGAME_INDEX)
            //                {
            //                    delete s._virtualIndexFile;
            //                    s._virtualIndexFile = 0;
            //                }
            //#endif
            //            }
            //            else {
            //                const Common::String wrappedName = g_sci.wrapFilename(name);
            //                result = saveFileMan.removeSavefile(wrappedName);
            //            }

            //            debugC(kDebugLevelFile, "kFileIO(unlink): %s", name.c_str());
            //            if (result)
            //                return NULL_REG;
            //            return make_reg(0, 2); // DOS - file not found error code
        }

        private static Register kFileIOReadString(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
            //            uint16 maxsize = argv[1].toUint16();
            //            char* buf = new char[maxsize];
            //            uint16 handle = argv[2].toUint16();
            //            debugC(kDebugLevelFile, "kFileIO(readString): %d, %d", handle, maxsize);
            //            uint32 bytesRead;

            //# if ENABLE_SCI32
            //            if (handle == VIRTUALFILE_HANDLE)
            //                bytesRead = s._virtualIndexFile.readLine(buf, maxsize);
            //            else
            //#endif
            //                bytesRead = fgets_wrapper(s, buf, maxsize, handle);

            //            s._segMan.memcpy(argv[0], (const byte*)buf, maxsize);
            //            delete[] buf;
            //            return bytesRead ? argv[0] : NULL_REG;
        }

        private static Register kFileIOWriteString(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
            //            int handle = argv[0].toUint16();
            //            Common::String str = s._segMan.getString(argv[1]);
            //            debugC(kDebugLevelFile, "kFileIO(writeString): %d", handle);

            //            // Handle sciAudio calls in fanmade games here. sciAudio is an
            //            // external .NET library for playing MP3 files in fanmade games.
            //            // It runs in the background, and obtains sound commands from the
            //            // currently running game via text files (called "conductor files").
            //            // We skip creating these files, and instead handle the calls
            //            // directly. Since the sciAudio calls are only creating text files,
            //            // this is probably the most straightforward place to handle them.
            //            if (handle == 0xFFFF && str.hasPrefix("(sciAudio"))
            //            {
            //                Common::List<ExecStack>::const_iterator iter = s._executionStack.reverse_begin();
            //                iter--; // sciAudio
            //                iter--; // sciAudio child
            //                g_sci._audio.handleFanmadeSciAudio(iter.sendp, s._segMan);
            //                return NULL_REG;
            //            }

            //# if ENABLE_SCI32
            //            if (handle == VIRTUALFILE_HANDLE)
            //            {
            //                s._virtualIndexFile.write(str.c_str(), str.size());
            //                return NULL_REG;
            //            }
            //#endif

            //            FileHandle* f = getFileFromHandle(s, handle);

            //            if (f)
            //            {
            //                f._out.write(str.c_str(), str.size());
            //                if (getSciVersion() <= SCI_VERSION_0_LATE)
            //                    return s.r_acc;    // SCI0 semantics: no value returned
            //                return NULL_REG;
            //            }

            //            if (getSciVersion() <= SCI_VERSION_0_LATE)
            //                return s.r_acc;    // SCI0 semantics: no value returned
            //            return make_reg(0, 6); // DOS - invalid handle
        }

        private static Register kFileIOSeek(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
            //            uint16 handle = argv[0].toUint16();
            //            uint16 offset = ABS<int16>(argv[1].toSint16()); // can be negative
            //            uint16 whence = argv[2].toUint16();
            //            debugC(kDebugLevelFile, "kFileIO(seek): %d, %d, %d", handle, offset, whence);

            //# if ENABLE_SCI32
            //            if (handle == VIRTUALFILE_HANDLE)
            //                return make_reg(0, s._virtualIndexFile.seek(offset, whence));
            //#endif

            //            FileHandle* f = getFileFromHandle(s, handle);

            //            if (f && f._in)
            //            {
            //                // Backward seeking isn't supported in zip file streams, thus adapt the
            //                // parameters accordingly if games ask for such a seek mode. A known
            //                // case where this is requested is the save file manager in Phantasmagoria
            //                if (whence == SEEK_END)
            //                {
            //                    whence = SEEK_SET;
            //                    offset = f._in.size() - offset;
            //                }

            //                return make_reg(0, f._in.seek(offset, whence));
            //            }
            //            else if (f && f._out)
            //            {
            //                error("kFileIOSeek: Unsupported seek operation on a writeable stream (offset: %d, whence: %d)", offset, whence);
            //            }

            //            return SIGNAL_REG;
        }

        private static Register kFileIOFindFirst(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
            //Common::String mask = s._segMan.getString(argv[0]);
            //reg_t buf = argv[1];
            //int attr = argv[2].toUint16(); // We won't use this, Win32 might, though...
            //debugC(kDebugLevelFile, "kFileIO(findFirst): %s, 0x%x", mask.c_str(), attr);

            //// We remove ".*". mask will get prefixed, so we will return all additional files for that gameid
            //if (mask == "*.*")
            //    mask = "*";
            //return s._dirseeker.firstFile(mask, buf, s._segMan);
        }

        private static Register kFileIOFindNext(EngineState s, int argc, StackPtr? argv)
        {
            // TODO: debugC(kDebugLevelFile, "kFileIO(findNext)");
            return s._dirseeker.NextFile(s._segMan);
        }

        private static Register kFileIOExists(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
            //            Common::String name = s._segMan.getString(argv[0]);

            //# if ENABLE_SCI32
            //            // Cache the file existence result for the Phantasmagoria
            //            // save index file, as the game scripts keep checking for
            //            // its existence.
            //            if (name == PHANTASMAGORIA_SAVEGAME_INDEX && s._virtualIndexFile)
            //                return TRUE_REG;
            //#endif

            //            bool exists = false;

            //            // Check for regular file
            //            exists = Common::File::exists(name);

            //            // Check for a savegame with the name
            //            Common::SaveFileManager* saveFileMan = g_sci.getSaveFileManager();
            //            if (!exists)
            //                exists = !saveFileMan.listSavefiles(name).empty();

            //            // Try searching for the file prepending "target-"
            //            const Common::String wrappedName = g_sci.wrapFilename(name);
            //            if (!exists)
            //            {
            //                exists = !saveFileMan.listSavefiles(wrappedName).empty();
            //            }

            //            // SCI2+ debug mode
            //            if (DebugMan.isDebugChannelEnabled(kDebugLevelDebugMode))
            //            {
            //                if (!exists && name == "1.scr")     // PQ4
            //                    exists = true;
            //                if (!exists && name == "18.scr")    // QFG4
            //                    exists = true;
            //                if (!exists && name == "99.scr")    // GK1, KQ7
            //                    exists = true;
            //                if (!exists && name == "classes")   // GK2, SQ6, LSL7
            //                    exists = true;
            //            }

            //            // Special case for non-English versions of LSL5: The English version of
            //            // LSL5 calls kFileIO(), case K_FILEIO_OPEN for reading to check if
            //            // memory.drv exists (which is where the game's password is stored). If
            //            // it's not found, it calls kFileIO() again, case K_FILEIO_OPEN for
            //            // writing and creates a new file. Non-English versions call kFileIO(),
            //            // case K_FILEIO_FILE_EXISTS instead, and fail if memory.drv can't be
            //            // found. We create a default memory.drv file with no password, so that
            //            // the game can continue.
            //            if (!exists && name == "memory.drv")
            //            {
            //                // Create a new file, and write the bytes for the empty password
            //                // string inside
            //                byte defaultContent[] = { 0xE9, 0xE9, 0xEB, 0xE1, 0x0D, 0x0A, 0x31, 0x30, 0x30, 0x30 };
            //                Common::WriteStream* outFile = saveFileMan.openForSaving(wrappedName);
            //                for (int i = 0; i < 10; i++)
            //                    outFile.writeByte(defaultContent[i]);
            //                outFile.finalize();
            //                exists = !outFile.err();   // check whether we managed to create the file.
            //                delete outFile;
            //            }

            //            // Special case for KQ6 Mac: The game checks for two video files to see
            //            // if they exist before it plays them. Since we support multiple naming
            //            // schemes for resource fork files, we also need to support that here in
            //            // case someone has a "HalfDome.bin" file, etc.
            //            if (!exists && g_sci.getGameId() == SciGameId.KQ6 && g_sci.getPlatform() == Common::kPlatformMacintosh &&
            //                    (name == "HalfDome" || name == "Kq6Movie"))
            //                exists = Common::MacResManager::exists(name);

            //            debugC(kDebugLevelFile, "kFileIO(fileExists) %s . %d", name.c_str(), exists);
            //            return make_reg(0, exists);
        }

        private static Register kFileIORename(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
            //string oldName = s._segMan.GetString(argv[0]);
            //string newName = s._segMan.GetString(argv[1]);

            //// SCI1.1 returns 0 on success and a DOS error code on fail. SCI32
            //// returns -1 on fail. We just return -1 for all versions.
            //if (SciEngine.Instance.SaveFileManager.RenameSavefile(oldName, newName))
            //    return Register.NULL_REG;
            //else
            //    return Register.SIGNAL_REG;
        }
    }
}
