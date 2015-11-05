using System;
using System.Diagnostics;
using System.IO;
using NScumm.Core;
using NScumm.Sky.Music;

namespace NScumm.Sky
{
    internal class Control
    {
        private const int SaveFileRevision = 6;
        private const int OldSaveGameType = 5;
        private const int PanCharHeight = 12;

        public const int GameRestored = 106;
        private const int RestoreFailed = 107;

        private static readonly string[] QuitTexts =
        {
            "Game over player one",
            "BE VIGILANT",
            "Das Spiel ist aus.",
            "SEI WACHSAM",
            "Game over joueur 1",
            "SOYEZ VIGILANTS",
            "Game over player one",
            "BE VIGILANT",
            "SPELET \x2Ar SLUT, Agent 1.",
            "VAR VAKSAM",
            "Game over giocatore 1",
            "SIATE VIGILANTI",
            "Fim de jogo para o jogador um",
            "BE VIGILANT",
            "Game over player one",
            "BE VIGILANT"
        };

        private ushort _savedMouse;

        private readonly SkyCompact _skyCompact;
        private readonly Disk _skyDisk;
        private readonly Logic _skyLogic;
        private readonly Mouse _skyMouse;
        private readonly MusicBase _skyMusic;
        private readonly Screen _skyScreen;
        private readonly Sound _skySound;
        private readonly Text _skyText;
        private ISystem _system;

        public Control( /*SaveFileManager saveFileMan,*/
            Screen screen, Disk disk, Mouse mouse, Text text, MusicBase music, Logic logic, Sound sound,
            SkyCompact skyCompact, ISystem system)
        {
            //_saveFileMan = saveFileMan;

            _skyScreen = screen;
            _skyDisk = disk;
            _skyMouse = mouse;
            _skyText = text;
            _skyMusic = music;
            _skyLogic = logic;
            _skySound = sound;
            _skyCompact = skyCompact;
            _system = system;
        }

        public void RestartGame()
        {
            if (SystemVars.Instance.GameVersion.Version.Minor <= 267)
                return; // no restart for floppy demo

            var resetData = _skyCompact.CreateResetData((ushort) SystemVars.Instance.GameVersion.Version.Minor);
            ParseSaveData(resetData);

            _skyScreen.ForceRefresh();

            Array.Clear(_skyScreen.Current, 0, Screen.GameScreenWidth*Screen.FullScreenHeight);
            _skyScreen.ShowScreen(_skyScreen.Current);
            _skyScreen.SetPaletteEndian(_skyCompact.FetchCptRaw((ushort) SystemVars.Instance.CurrentPalette));
            _skyMouse.SpriteMouse(_savedMouse, 0, 0);
            SystemVars.Instance.PastIntro = true;
        }

        public void ShowGameQuitMsg()
        {
            _skyText.FnSetFont(0);
            var sizeofDataFileHeader = ServiceLocator.Platform.SizeOf<DataFileHeader>();
            var textBuf1 = new byte[Screen.GameScreenWidth*14 + sizeofDataFileHeader];
            var textBuf2 = new byte[Screen.GameScreenWidth*14 + sizeofDataFileHeader];
            if (_skyScreen.SequenceRunning())
                _skyScreen.StopSequence();

            var screenData = _skyScreen.Current;

            _skyText.DisplayText(QuitTexts[SystemVars.Instance.Language*2 + 0], textBuf1, true, 320, 255);
            _skyText.DisplayText(QuitTexts[SystemVars.Instance.Language*2 + 1], textBuf2, true, 320, 255);
            var curLine1 = sizeofDataFileHeader;
            var curLine2 = sizeofDataFileHeader;
            var targetLine = Screen.GameScreenWidth*80;
            for (byte cnty = 0; cnty < PanCharHeight; cnty++)
            {
                for (ushort cntx = 0; cntx < Screen.GameScreenWidth; cntx++)
                {
                    if (textBuf1[curLine1 + cntx] != 0)
                        screenData[targetLine + cntx] = textBuf1[curLine1 + cntx];
                    if (textBuf2[curLine2 + cntx] != 0)
                        screenData[targetLine + 24*Screen.GameScreenWidth + cntx] = textBuf2[curLine2 + cntx];
                }
                curLine1 += Screen.GameScreenWidth;
                curLine2 += Screen.GameScreenWidth;
                targetLine += Screen.GameScreenWidth;
            }
            _skyScreen.HalvePalette();
            _skyScreen.ShowScreen(screenData);
        }

        private ushort ParseSaveData(byte[] src)
        {
            using (var reader = new BinaryReader(new MemoryStream(src)))
            {
                var size = reader.ReadUInt32();
                var saveRev = reader.ReadUInt32();
                if (saveRev > SaveFileRevision)
                {
                    // TODO: displayMessage(0, "Unknown save file revision (%d)", saveRev);
                    return RestoreFailed;
                }
                if (saveRev < OldSaveGameType)
                {
                    // TODO: displayMessage(0, "This savegame version is unsupported.");
                    return RestoreFailed;
                }
                var gameVersion = reader.ReadUInt32();
                if (gameVersion != SystemVars.Instance.GameVersion.Version.Minor)
                {
                    if (!SystemVars.Instance.GameVersion.Type.HasFlag(SkyGameType.Cd) || (gameVersion < 365))
                    {
                        // cd versions are compatible
                        // TODO: displayMessage(NULL, "This savegame was created by\n"
                        //    "Beneath a Steel Sky v0.0%03d\n"
                        //    "It cannot be loaded by this version (v0.0%3d)",
                        //gameVersion, SkyEngine::_systemVars.gameVersion);
                        return RestoreFailed;
                    }
                }
                SystemVars.Instance.SystemFlags |= SystemFlags.GAME_RESTORED;

                _skySound.SaveSounds[0] = reader.ReadUInt16();
                _skySound.SaveSounds[1] = reader.ReadUInt16();
                _skySound.RestoreSfx();

                var music = reader.ReadUInt32();
                var _savedCharSet = reader.ReadUInt32();
                var mouseType = reader.ReadUInt32();
                var palette = reader.ReadUInt32();

                _skyLogic.ParseSaveData(reader.ReadBytes(Logic.NumSkyScriptVars*4));

                var reloadList = reader.ReadUInt32s(60);

                if (saveRev == SaveFileRevision)
                {
                    for (var cnt = 0; cnt < _skyCompact.SaveIds.Length; cnt++)
                    {
                        var cptEntry = _skyCompact.FetchCptEntry(_skyCompact.SaveIds[cnt]);
                        cptEntry.Patch(reader.ReadBytes(cptEntry.Size));
                    }
                }
                else
                {
                    // import old savegame revision
                    for (var cnt = 0; cnt < _skyCompact.SaveIds.Length - 2; cnt++)
                    {
                        var cptEntry = _skyCompact.FetchCptEntry(_skyCompact.SaveIds[cnt]);
                        if (cptEntry.Type == CptTypeId.Compact)
                        {
                            ImportOldCompact(cptEntry, reader);
                        }
                        else if (cptEntry.Type == CptTypeId.RouteBuf)
                        {
                            Debug.Assert(cptEntry.Size == 32*2);
                            cptEntry.Patch(reader.ReadBytes(cptEntry.Size));
                        }
                    }
                    {
                        var cptEntry = _skyCompact.FetchCptEntry(0xBF);
                        cptEntry.Patch(reader.ReadBytes(3*2));
                        cptEntry = _skyCompact.FetchCptEntry(0xC2);
                        cptEntry.Patch(reader.ReadBytes(13*2));
                    }
                }

                // make sure all text compacts are off
                for (var cnt = CptIds.Text1; cnt <= CptIds.Text11; cnt++)
                {
                    var c = _skyCompact.FetchCpt((ushort) cnt);
                    c.Core.status = 0;
                }

                if (reader.BaseStream.Position != size)
                    throw new InvalidOperationException(
                        string.Format("Restore failed! Savegame data = {0} bytes. Expected size: {1}",
                            reader.BaseStream.Position, size));

                _skyDisk.RefreshFilesList(reloadList);
                SystemVars.Instance.CurrentMusic = (ushort) music;
                if (!SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.MUS_OFF))
                    _skyMusic.StartMusic((ushort) music);
                _savedMouse = (ushort) mouseType;
                SystemVars.Instance.CurrentPalette = palette; // will be set when doControlPanel ends

                return GameRestored;
            }
        }

        private void ImportOldCompact(CompactEntry cptEntry, BinaryReader reader)
        {
            // TODO: ImportOldCompact
            throw new NotImplementedException();
        }

        public void DoLoadSavePanel()
        {
            throw new NotImplementedException();
            //if (SkyEngine.IsDemo)
            //    return; // I don't think this can even happen
            //InitPanel();
            //_skyScreen.ClearScreen();
            //if (SystemVars.Instance.GameVersion.Version.Minor < 331)
            //    _skyScreen.SetPalette(60509);
            //else
            //    _skyScreen.SetPalette(60510);

            //_savedMouse = _skyMouse.CurrentMouseType;
            //_savedCharSet = _skyText.CurrentCharSet;
            //_skyText.FnSetFont(2);
            //_skyMouse.SpriteMouse(MOUSE_NORMAL, 0, 0);
            //_lastButton = -1;
            //_curButtonText = 0;

            //SaveRestorePanel(false);

            //memset(_screenBuf, 0, GAME_SCREEN_WIDTH * FULL_SCREEN_HEIGHT);
            //_system.GraphicsManager.CopyRectToScreen(_screenBuf, GAME_SCREEN_WIDTH, 0, 0, GAME_SCREEN_WIDTH, FULL_SCREEN_HEIGHT);
            //_system.GraphicsManager.UpdateScreen();
            //_skyScreen.ForceRefresh();
            //_skyScreen.SetPaletteEndian(_skyCompact.FetchCptRaw((ushort)SystemVars.Instance.CurrentPalette));
            //RemovePanel();
            //_skyMouse.SpriteMouse(_savedMouse, 0, 0);
            //_skyText.FnSetFont(_savedCharSet);
        }

        private void InitPanel()
        {
            throw new NotImplementedException();
        }
    }
}