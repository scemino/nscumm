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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NScumm.Core;
using NScumm.Core.Input;
using NScumm.Sky.Music;

namespace NScumm.Sky
{
	internal class Control
	{
		private const int MaxSaveGames = 999;
		private const int MaxTextLen = 80;

		private const int PanLineWidth = 184;

		private const int SaveFileRevision = 6;
		private const int OldSaveGameType = 5;
		private const int PanCharHeight = 12;

		private const int Mainpanel = 0;
		private const int Savepanel = 1;

		private const bool NoMask = false;
		private const bool WithMask = true;

		private const SystemFlags TextFlagMask = SystemFlags.AllowSpeech | SystemFlags.AllowText;

		private const int GameNameX = SpnlX + 18;
		// x coordinate of game names
		private const int GameNameY = SpnlY + SpTopGap;
		// start y coord of game names
		private const int MaxOnScreen = (SpHeight - SpTopGap - SpBotGap) / PanCharHeight;
		// no of save games on screen
		private const int CpPanel = 60400;
		// main panel sprite


		private const int SpeedMultiply = 12;

		// resource's onClick routines
		private const int DoNothing = 0;
		private const int RestGamePanel = 1;
		private const int SaveGamePanel = 2;
		private const int SaveAGame = 3;
		private const int RestoreAGame = 4;
		private const int SpCancel = 5;
		private const int ShiftDownFast = 6;
		private const int ShiftDownSlow = 7;
		private const int ShiftUpFast = 8;
		private const int ShiftUpSlow = 9;
		private const int SpeedSlide = 10;
		private const int MusicSlide = 11;
		private const int ToggleFx = 12;
		private const int ToggleMs = 13;
		private const int ToggleText = 14;
		private const int Exit = 15;
		private const int Restart = 16;
		private const int QuitToDos = 17;
		private const int RestoreAuto = 18;

		// onClick return codes
		private const int CancelPressed = 100;
		private const int NameTooShort = 101;
		private const int GameSaved = 102;
		private const int Shifted = 103;
		private const int Toggled = 104;
		private const int Restarted = 105;
		public const int GameRestored = 106;
		private const int RestoreFailed = 107;
		private const int NoDiskSpace = 108;
		private const int SpeedChanged = 109;
		private const int QuitPanel = 110;

		private const int Slow = 0;
		private const int Fast = 1;

		private const int MpnlX = 60;
		// Main Panel
		private const int MpnlY = 10;

		private const int SpnlX = 20;
		// Save Panel
		private const int SpnlY = 20;
		private const int SpHeight = 149;
		private const int SpTopGap = 12;
		private const int SpBotGap = 27;
		private const int CrossSzX = 27;
		private const int CrossSzY = 22;


		private static readonly string[] QuitTexts = {
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

        private static readonly byte[] CrossImg =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0x09, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0B, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0x4F, 0x4D, 0x61,
            0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x08, 0x4E, 0x53, 0x50, 0x4F, 0x0C, 0x4D, 0x4E, 0x51, 0x58, 0x58, 0x54, 0x4E, 0x08, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x4E, 0x54, 0x58, 0x50, 0x4E, 0xFF,
            0xFF, 0xFF, 0xFF, 0x50, 0x4E, 0x54, 0x58, 0x58, 0x54, 0x4E, 0x0C, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x61, 0x53, 0x58, 0x54, 0x4E, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x50, 0x4E, 0x55, 0x58, 0x58, 0x53, 0x4E, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x05, 0x51, 0x58, 0x58,
            0x51, 0x50, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x4F, 0x51, 0x58,
            0x59, 0x58, 0x51, 0x61, 0xFF, 0xFF, 0x61, 0x54, 0x58, 0x58, 0x4F, 0x52, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x4E, 0x55, 0x58, 0x58, 0x57, 0x4E,
            0x4F, 0x56, 0x58, 0x57, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x4F, 0x51, 0x58, 0x58, 0x58, 0x58, 0x58, 0x54, 0x4E, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0x6A, 0x4F, 0x58, 0x58, 0x58, 0x58, 0x52, 0x06, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x04, 0x54, 0x58,
            0x58, 0x58, 0x58, 0x57, 0x53, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x04, 0x09, 0x58, 0x58, 0x58, 0x57, 0x56, 0x58, 0x58, 0x58,
            0x57, 0x4F, 0x0A, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x61, 0x55, 0x58, 0x58, 0x58, 0x58, 0x4E, 0x64, 0x57, 0x58, 0x58, 0x58, 0x58, 0x53, 0x61, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x61, 0x57, 0x58, 0x58, 0x58, 0x58,
            0x50, 0xFF, 0xFF, 0x4E, 0x57, 0x58, 0x58, 0x58, 0x58, 0x56, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0x61, 0x58, 0x58, 0x58, 0x58, 0x58, 0x53, 0x09, 0xFF, 0xFF, 0xFF, 0x4E,
            0x57, 0x58, 0x58, 0x58, 0x58, 0x58, 0x0B, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x61, 0x57,
            0x58, 0x58, 0x58, 0x58, 0x56, 0x4E, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x61, 0x58, 0x58, 0x58, 0x58,
            0x58, 0x57, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x04, 0x55, 0x58, 0x58, 0x58, 0x58, 0x58, 0x4E,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x4F, 0x58, 0x58, 0x58, 0x58, 0x4E, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x06, 0x58, 0x58, 0x58, 0x58, 0x58, 0x52, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0x0C, 0x52, 0x58, 0x58, 0x51, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x61, 0x56, 0x58,
            0x58, 0x58, 0x58, 0x56, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x61, 0x56,
            0x58, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x4D, 0x4D, 0x51, 0x56, 0x58, 0x58, 0x50, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x4F, 0x54, 0x09, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x4E, 0x50, 0x54, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x06, 0x50, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0x61, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x61, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x61, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF
        };


        private readonly SkyCompact _skyCompact;
        private readonly Disk _skyDisk;
        private readonly Logic _skyLogic;
        private readonly Mouse _skyMouse;
        private readonly MusicBase _skyMusic;
        private readonly Screen _skyScreen;
        private readonly Sound _skySound;
        private readonly Text _skyText;
        private readonly ISystem _system;
        private ConResource _autoSaveButton;
        private ConResource _bodge;
        private ConResource _controlPanel;
        private ConResource[] _controlPanLookList;
        private int _curButtonText;
        private ConResource _dosPanButton;
        private ConResource _downFastButton;
        private ConResource _downSlowButton;

        private ConResource _exitButton;
        private ushort _firstText;
        private ConResource _fxPanButton;
        private ScummInputState _keyPressed;
        private bool _mouseClicked;
        private ConResource _musicPanButton;
        private ConResource _quitButton;
        private ConResource _restartPanButton;
        private ConResource _restoreButton;
        private ConResource _restorePanButton;
        private ConResource[] _restorePanLookList;
        private ConResource _saveButton;
        private uint _savedCharSet;

        private ushort _savedMouse;
        private ConResource _savePanButton;
        private ConResource _savePanel;
        private ConResource[] _savePanLookList;
        private byte[] _screenBuf;
        private ConResource _slide;
        private ConResource _slide2;
        private ConResource _slode;
        private Sprites _sprites;
        private ControlStatus _statusBar;
        private TextResource _text;
		private DataFileHeader _textSprite;
        private ConResource _upFastButton;
        private ConResource _upSlowButton;
        private ConResource _yesNo;
        private int _mouseWheel;
        private ushort _selectedGame;
        private ushort _enteredTextWidth;
        private ISaveFileManager _saveFileMan;

        public Control(Screen screen, Disk disk, Mouse mouse, Text text, MusicBase music, Logic logic, Sound sound,
            SkyCompact skyCompact, ISystem system)
        {
            _saveFileMan = system.SaveFileManager;

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

            var resetData = _skyCompact.CreateResetData((ushort)SystemVars.Instance.GameVersion.Version.Minor);
            ParseSaveData(resetData);

            _skyScreen.ForceRefresh();

            Array.Clear(_skyScreen.Current, 0, Screen.GameScreenWidth * Screen.FullScreenHeight);
            _skyScreen.ShowScreen(_skyScreen.Current);
            _skyScreen.SetPaletteEndian(_skyCompact.FetchCptRaw((ushort)SystemVars.Instance.CurrentPalette));
            _skyMouse.SpriteMouse(_savedMouse, 0, 0);
            SystemVars.Instance.PastIntro = true;
        }

        public void ShowGameQuitMsg()
        {
            _skyText.FnSetFont(0);
			var textBuf1 = new byte[Screen.GameScreenWidth * 14 + DataFileHeader.Size];
			var textBuf2 = new byte[Screen.GameScreenWidth * 14 + DataFileHeader.Size];
            if (_skyScreen.SequenceRunning())
                _skyScreen.StopSequence();

            var screenData = _skyScreen.Current;

            _skyText.DisplayText(QuitTexts[SystemVars.Instance.Language * 2 + 0], textBuf1, true, 320, 255);
            _skyText.DisplayText(QuitTexts[SystemVars.Instance.Language * 2 + 1], textBuf2, true, 320, 255);
			var curLine1 = DataFileHeader.Size;
			var curLine2 = DataFileHeader.Size;
            var targetLine = Screen.GameScreenWidth * 80;
            for (byte cnty = 0; cnty < PanCharHeight; cnty++)
            {
                for (ushort cntx = 0; cntx < Screen.GameScreenWidth; cntx++)
                {
                    if (textBuf1[curLine1 + cntx] != 0)
                        screenData[targetLine + cntx] = textBuf1[curLine1 + cntx];
                    if (textBuf2[curLine2 + cntx] != 0)
                        screenData[targetLine + 24 * Screen.GameScreenWidth + cntx] = textBuf2[curLine2 + cntx];
                }
                curLine1 += Screen.GameScreenWidth;
                curLine2 += Screen.GameScreenWidth;
                targetLine += Screen.GameScreenWidth;
            }
            _skyScreen.HalvePalette();
            _skyScreen.ShowScreen(screenData);
        }

        public void DoControlPanel()
        {
            if (SkyEngine.IsDemo)
            {
                return;
            }

            InitPanel();

            _savedCharSet = _skyText.CurrentCharSet;
            _skyText.FnSetFont(2);

            _skyScreen.ClearScreen();
            if (SystemVars.Instance.GameVersion.Version.Minor < 331)
                _skyScreen.SetPalette(60509);
            else
                _skyScreen.SetPalette(60510);

            // Set initial button lights
            _fxPanButton.CurSprite = (uint)(SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.FxOff) ? 0 : 2);

            // music button only available in floppy version
            if (!SkyEngine.IsCDVersion)
            {
                _musicPanButton.CurSprite =
                    (uint)(SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.MusOff) ? 0 : 2);
            }

            DrawMainPanel();

            _savedMouse = _skyMouse.CurrentMouseType;

            _skyMouse.SpriteMouse(Logic.MOUSE_NORMAL, 0, 0);
            var quitPanel = false;
            _curButtonText = 0;

            while (!quitPanel && !SkyEngine.ShouldQuit)
            {
                _text.DrawToScreen(WithMask);
                _system.GraphicsManager.UpdateScreen();
                _mouseClicked = false;
                Delay(50);
                if (_controlPanel == null)
                    return;
                if (_keyPressed.IsKeyDown(KeyCode.Escape))
                {
                    // escape pressed
                    _mouseClicked = false;
                    quitPanel = true;
                }
                var haveButton = false;
                var mouse = _system.InputManager.GetMousePosition();
                for (byte lookCnt = 0; lookCnt < 9; lookCnt++)
                {
                    if (_controlPanLookList[lookCnt].IsMouseOver((uint)mouse.X, (uint)mouse.Y))
                    {
                        haveButton = true;
                        ButtonControl(_controlPanLookList[lookCnt]);
                        if (_mouseClicked && _controlPanLookList[lookCnt].OnClick != 0)
                        {
                            var clickRes = HandleClick(_controlPanLookList[lookCnt]);
                            if (_controlPanel == null) //game state was destroyed
                                return;
                            _text.FlushForRedraw();
                            DrawMainPanel();
                            _text.DrawToScreen(WithMask);
                            if ((clickRes == QuitPanel) || (clickRes == GameSaved) ||
                                (clickRes == GameRestored))
                                quitPanel = true;
                        }
                        _mouseClicked = false;
                    }
                }
                if (!haveButton)
                    ButtonControl(null);
            }
            Array.Clear(_screenBuf, 0, Screen.GameScreenWidth * Screen.FullScreenHeight);
            _system.GraphicsManager.CopyRectToScreen(_screenBuf, Screen.GameScreenWidth, 0, 0, Screen.GameScreenWidth,
                Screen.FullScreenHeight);
            if (!SkyEngine.ShouldQuit)
                _system.GraphicsManager.UpdateScreen();
            _skyScreen.ForceRefresh();
            _skyScreen.SetPaletteEndian(_skyCompact.FetchCptRaw((ushort)SystemVars.Instance.CurrentPalette));
            RemovePanel();
            _skyMouse.SpriteMouse(_savedMouse, 0, 0);
            _skyText.FnSetFont(_savedCharSet);
        }

        public bool LoadSaveAllowed()
        {
            if (SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.Choosing))
                return false; // texts get lost during load/save, so don't allow it during choosing
            if (_skyLogic.ScriptVariables[Logic.SCREEN] >= 101)
                return false; // same problem with LINC terminals
            if ((_skyLogic.ScriptVariables[Logic.SCREEN] >= 82) &&
                (_skyLogic.ScriptVariables[Logic.SCREEN] != 85) &&
                (_skyLogic.ScriptVariables[Logic.SCREEN] < 90))
                return false; // don't allow saving in final rooms

            return true;
        }

        public void DoAutoSave()
        {
            string fName;
            if (SkyEngine.IsCDVersion)
                fName = "SKY-VM-CD.ASD";
            else
                fName = string.Format("SKY-VM{0:D3}.ASD", SystemVars.Instance.GameVersion.Version.Minor);

            ushort res = SaveGameToFile(false, fName);
            if (res != GameSaved)
            {
                // TODO: DisplayMessage(0, "Unable to perform autosave to '%s'. (%s)", fName, _saveFileMan->popErrorDesc().c_str());
            }
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
                    if (!SkyEngine.IsCDVersion || (gameVersion < 365))
                    {
                        // cd versions are compatible
                        // TODO: displayMessage(NULL, "This savegame was created by\n"
                        //    "Beneath a Steel Sky v0.0%03d\n"
                        //    "It cannot be loaded by this version (v0.0%3d)",
                        //gameVersion, SkyEngine::_systemVars.gameVersion);
                        return RestoreFailed;
                    }
                }
                SystemVars.Instance.SystemFlags |= SystemFlags.GameRestored;

                _skySound.SaveSounds[0] = reader.ReadUInt16();
                _skySound.SaveSounds[1] = reader.ReadUInt16();
                _skySound.RestoreSfx();

                var music = reader.ReadUInt32();
                _savedCharSet = reader.ReadUInt32();
                var mouseType = reader.ReadUInt32();
                var palette = reader.ReadUInt32();

                _skyLogic.ParseSaveData(reader.ReadBytes(Logic.NumSkyScriptVars * 4));

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
                            System.Diagnostics.Debug.Assert(cptEntry.Size == 32 * 2);
                            cptEntry.Patch(reader.ReadBytes(cptEntry.Size));
                        }
                    }
                    {
                        var cptEntry = _skyCompact.FetchCptEntry(0xBF);
                        cptEntry.Patch(reader.ReadBytes(3 * 2));
                        cptEntry = _skyCompact.FetchCptEntry(0xC2);
                        cptEntry.Patch(reader.ReadBytes(13 * 2));
                    }
                }

                // make sure all text compacts are off
                for (var cnt = CptIds.Text1; cnt <= CptIds.Text11; cnt++)
                {
                    var c = _skyCompact.FetchCpt((ushort)cnt);
                    c.Core.status = 0;
                }

                if (reader.BaseStream.Position != size)
                    throw new InvalidOperationException(
                        string.Format("Restore failed! Savegame data = {0} bytes. Expected size: {1}",
                            reader.BaseStream.Position, size));

                _skyDisk.RefreshFilesList(reloadList);
                SystemVars.Instance.CurrentMusic = (ushort)music;
                if (!SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.MusOff))
                    _skyMusic.StartMusic((ushort)music);
                _savedMouse = (ushort)mouseType;
                SystemVars.Instance.CurrentPalette = palette; // will be set when doControlPanel ends

                return GameRestored;
            }
        }

        private void ImportOldCompact(CompactEntry cptEntry, BinaryReader reader)
        {
            // TODO: ImportOldCompact
            throw new NotImplementedException();
        }

        private void Delay(int amount)
        {
            var start = Environment.TickCount;
            var cur = start;
            do
            {
                _keyPressed = _system.InputManager.GetState();
                var mousePos = _system.InputManager.GetMousePosition();

                if (!SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.MouseLocked))
                    _skyMouse.MouseMoved((ushort)mousePos.X, (ushort)mousePos.Y);

                _mouseClicked = _keyPressed.IsLeftButtonDown;

                // TODO: mouse wheel ?
                //case Common::EVENT_WHEELUP:
                //	_mouseWheel = -1;
                //	break;
                //case Common::EVENT_WHEELDOWN:
                //	_mouseWheel = 1;
                //	break;

                var thisDelay = 20; // 1?
#if _WIN32_WCE
    this_delay = 10;
#endif
                if (thisDelay > amount)
                    thisDelay = amount;

                if (thisDelay > 0) ServiceLocator.Platform.Sleep(thisDelay);

                cur = Environment.TickCount;
            } while (cur < start + amount);
        }

        private ushort HandleClick(ConResource pButton)
        {
            switch (pButton.OnClick)
            {
                case DoNothing:
                    return 0;
                case RestGamePanel:
                    if (!LoadSaveAllowed())
                        return CancelPressed; // can't save/restore while choosing
                    AnimClick(pButton);
                    return SaveRestorePanel(false); // texts can't be edited
                case SaveGamePanel:
                    if (!LoadSaveAllowed())
                        return CancelPressed; // can't save/restore while choosing
                    AnimClick(pButton);
                    return SaveRestorePanel(true); // texts can be edited
                case SaveAGame:
                    AnimClick(pButton);
                    return SaveGameToFile(true);
                case RestoreAGame:
                    AnimClick(pButton);
                    return RestoreGameFromFile(false);
                case RestoreAuto:
                    AnimClick(pButton);
                    return RestoreGameFromFile(true);
                case SpCancel:
                    AnimClick(pButton);
                    return CancelPressed;
                case ShiftDownFast:
                    AnimClick(pButton);
                    return ShiftDown(Fast);
                case ShiftDownSlow:
                    AnimClick(pButton);
                    return ShiftDown(Slow);
                case ShiftUpFast:
                    AnimClick(pButton);
                    return ShiftUp(Fast);
                case ShiftUpSlow:
                    AnimClick(pButton);
                    return ShiftUp(Slow);
                case SpeedSlide:
                    _mouseClicked = true;
                    return DoSpeedSlide();
                case MusicSlide:
                    _mouseClicked = true;
                    return DoMusicSlide();
                case ToggleFx:
                    DoToggleFx(pButton);
                    return Toggled;
                case ToggleMs:
                    ToggleMusic(pButton);
                    return Toggled;
                case ToggleText:
                    AnimClick(pButton);
                    return DoToggleText();
                case Exit:
                    AnimClick(pButton);
                    return QuitPanel;
                case Restart:
                    AnimClick(pButton);
                    if (GetYesNo("Restart?"))
                    {
                        RestartGame();
                        return GameRestored;
                    }
                    return 0;
                case QuitToDos:
                    AnimClick(pButton);
                    if (GetYesNo("Quit to DOS?"))
                        SkyEngine.QuitGame();
                    return 0;
                default:
                    throw new InvalidOperationException(string.Format("Control::handleClick: unknown routine: {0:X2}",
                        pButton.OnClick));
            }
        }

        private ushort DoToggleText()
        {
            var flags = SystemVars.Instance.SystemFlags & TextFlagMask;
            SystemVars.Instance.SystemFlags &= ~TextFlagMask;

            if (flags == SystemFlags.AllowText)
            {
                flags = SystemFlags.AllowSpeech;
                _statusBar.SetToText(0x7000 + 21); // speech only
            }
            else if (flags == SystemFlags.AllowSpeech)
            {
                flags = SystemFlags.AllowSpeech | SystemFlags.AllowText;
                _statusBar.SetToText(0x7000 + 52); // text and speech
            }
            else
            {
                flags = SystemFlags.AllowText;
                _statusBar.SetToText(0x7000 + 35); // text only
            }

            // TODO: configuration
            //ConfMan.setBool("subtitles", (flags & SystemFlags.AllowText) != 0);
            //ConfMan.setBool("speech_mute", (flags & SystemFlags.ALLOW_SPEECH) == 0);

            SystemVars.Instance.SystemFlags |= flags;

            DrawTextCross(flags);

            _system.GraphicsManager.UpdateScreen();
            return Toggled;
        }

        private void DoToggleFx(ConResource pButton)
        {
            SystemVars.Instance.SystemFlags ^= SystemFlags.FxOff;
            if (SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.FxOff))
            {
                pButton.CurSprite = 0;
                _statusBar.SetToText(0x7000 + 87);
            }
            else
            {
                pButton.CurSprite = 2;
                _statusBar.SetToText(0x7000 + 86);
            }

            // TODO: configuration
            //ConfMan.setBool("sfx_mute", SystemVars.SystemFlags.HasFlag(SystemFlags.FX_OFF) != 0);

            pButton.DrawToScreen(WithMask);
            _system.GraphicsManager.UpdateScreen();
        }

        private void ToggleMusic(ConResource pButton)
        {
            SystemVars.Instance.SystemFlags ^= SystemFlags.MusOff;
            if (SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.MusOff))
            {
                _skyMusic.StartMusic(0);
                pButton.CurSprite = 0;
                _statusBar.SetToText(0x7000 + 89);
            }
            else
            {
                _skyMusic.StartMusic(SystemVars.Instance.CurrentMusic);
                pButton.CurSprite = 2;
                _statusBar.SetToText(0x7000 + 88);
            }

            // TODO: configuration 
            // ConfMan.setBool("music_mute", (SkyEngine::_systemVars.systemFlags & SF_MUS_OFF) != 0);

            pButton.DrawToScreen(WithMask);
            _system.GraphicsManager.UpdateScreen();
        }

        private ushort DoMusicSlide()
        {
            var mouse = _system.InputManager.GetMousePosition();
            var ofsY = _slide2.Y - mouse.Y;
            while (_mouseClicked)
            {
                Delay(50);
                if (_controlPanel == null)
                    return 0;
                mouse = _system.InputManager.GetMousePosition();
                var newY = ofsY + mouse.Y;
                if (newY < 59) newY = 59;
                if (newY > 91) newY = 91;
                if (newY != _slide2.Y)
                {
                    _slode.DrawToScreen(NoMask);
                    _slide2.SetXy(_slide2.X, (ushort)newY);
                    _slide2.DrawToScreen(WithMask);
                    _slide.DrawToScreen(WithMask);
                    var volume = (byte)((newY - 59) * 4);
                    if (volume >= 128) volume = 0;
                    else volume = (byte)(127 - volume);
                    _skyMusic.Volume = volume;
                }
                ButtonControl(_slide2);
                _text.DrawToScreen(WithMask);
                _system.GraphicsManager.UpdateScreen();
            }
            return 0;
        }

        private ushort DoSpeedSlide()
        {
            var mouse = _system.InputManager.GetMousePosition();
            var ofsY = _slide.Y - mouse.Y;
            var speedDelay = (ushort)(_slide.Y - (MpnlY + 93));
            speedDelay *= SpeedMultiply;
            speedDelay += 2;
            while (_mouseClicked)
            {
                Delay(50);
                if (_controlPanel == null)
                    return SpeedChanged;
                mouse = _system.InputManager.GetMousePosition();
                var newY = ofsY + mouse.Y;
                if (newY < MpnlY + 93) newY = MpnlY + 93;
                if (newY > MpnlY + 104) newY = MpnlY + 104;
                if ((newY == 110) || (newY == 108)) newY = 109;
                if (newY != _slide.Y)
                {
                    _slode.DrawToScreen(NoMask);
                    _slide.SetXy(_slide.X, (ushort)newY);
                    _slide.DrawToScreen(WithMask);
                    _slide2.DrawToScreen(WithMask);
                    speedDelay = (ushort)(newY - (MpnlY + 93));
                    speedDelay *= SpeedMultiply;
                    speedDelay += 2;
                }
                ButtonControl(_slide);
                _text.DrawToScreen(WithMask);
                _system.GraphicsManager.UpdateScreen();
            }
            SystemVars.Instance.GameSpeed = speedDelay;
            return SpeedChanged;
        }

        private ushort ShiftUp(byte speed)
        {
            if (speed == Slow)
            {
                if (_firstText > 0)
                    _firstText--;
                else
                    return 0;
            }
            else
            {
                if (_firstText >= MaxOnScreen)
                    _firstText -= MaxOnScreen;
                else if (_firstText > 0)
                    _firstText = 0;
                else
                    return 0;
            }
            return Shifted;
        }

        private ushort ShiftDown(byte speed)
        {
            if (speed == Slow)
            {
                if (_firstText >= MaxSaveGames - MaxOnScreen)
                    return 0;
                _firstText++;
            }
            else
            {
                if (_firstText <= MaxSaveGames - 2 * MaxOnScreen)
                    _firstText += MaxOnScreen;
                else if (_firstText < MaxSaveGames - MaxOnScreen)
                    _firstText = MaxSaveGames - MaxOnScreen;
                else
                    return 0;
            }

            return Shifted;
        }

        private ushort RestoreGameFromFile(bool autoSave)
        {
            string fName;
            if (autoSave)
            {
                if (SkyEngine.IsCDVersion)
                    fName = "SKY-VM-CD.ASD";
                else
                    fName = string.Format("SKY-VM{0:D3}.ASD", SystemVars.Instance.GameVersion.Version);
            }
            else
                fName = string.Format("SKY-VM.{0:D3}", _selectedGame);

            try
            {
                using (var inf = (Stream)_saveFileMan.OpenForLoading(fName))
                {
                    var br = new BinaryReader(inf);
                    int infSize = br.ReadInt32();
                    if (infSize < 4) infSize = 4;
                    var saveData = new byte[infSize];
                    saveData.WriteUInt32(0, (uint)infSize);

                    if (inf.Read(saveData, 4, infSize - 4) != infSize - 4)
                    {
                        // TODO: DisplayMessage(null, "Can't read from file '%s'", fName);
                        return RestoreFailed;
                    }

                    ushort res = ParseSaveData(saveData);
                    SystemVars.Instance.PastIntro = true;
                    return res;
                }
            }
            catch (Exception)
            {
                return RestoreFailed;
            }
        }

        private ushort SaveGameToFile(bool fromControlPanel, string filename = null)
        {
            if (filename == null)
            {
                filename = string.Format("SKY-VM.{0:D3}", _selectedGame);
            }

            ushort writeOk;
            using (var outf = _saveFileMan.OpenForSaving(filename))
            {
                if (outf == null)
                    return NoDiskSpace;

                if (!fromControlPanel)
                {
                    // These variables are usually set when entering the control panel,
                    // but not when using the GMM.
                    _savedCharSet = _skyText.CurrentCharSet;
                    _savedMouse = _skyMouse.CurrentMouseType;
                }

                var saveData = new byte[0x20000];
                int fSize = PrepareSaveData(saveData);
                try
                {
                    outf.Write(saveData, 0, fSize);
                    writeOk = GameSaved;
                }
                catch (Exception)
                {
                    writeOk = NoDiskSpace;
                }
            }

            return writeOk;
        }

        private int PrepareSaveData(byte[] destBuf)
        {
            using (var stream = new MemoryStream(destBuf))
            {
                using (var bw = new BinaryWriter(stream))
                {
                    bw.BaseStream.Seek(4, SeekOrigin.Begin);
                    bw.WriteUInt32(SaveFileRevision);
                    bw.WriteUInt32((uint) SystemVars.Instance.GameVersion.Version.Minor);

                    bw.WriteUInt16(_skySound.SaveSounds[0]);
                    bw.WriteUInt16(_skySound.SaveSounds[1]);

                    bw.WriteUInt32(_skyMusic.CurrentMusic);
                    bw.WriteUInt32(_savedCharSet);
                    bw.WriteUInt32(_savedMouse);
                    bw.WriteUInt32(SystemVars.Instance.CurrentPalette);
                    for (var cnt = 0; cnt < Logic.NumSkyScriptVars; cnt++)
                        bw.WriteUInt32(_skyLogic.ScriptVariables[cnt]);
                    var loadedFilesList = _skyDisk.LoadedFilesList;

                    for (var cnt = 0; cnt < 60; cnt++)
                        bw.WriteUInt32(loadedFilesList[cnt]);

                    for (var cnt = 0; cnt < _skyCompact.SaveIds.Length; cnt++)
                    {
                        var rawCpt = _skyCompact.FetchCptRaw(_skyCompact.SaveIds[cnt]);
                        bw.WriteBytes(rawCpt, rawCpt.Length);
                    }

                    var length = bw.BaseStream.Position;
                    bw.BaseStream.Seek(0, SeekOrigin.Begin);
                    bw.WriteUInt32((uint) length);
                    return (int) length;
                }
            }
        }

        private bool GetYesNo(string text)
        {
            var retVal = false;
            var quitPanel = false;
            byte mouseType = Logic.MOUSE_NORMAL;
            byte wantMouse = Logic.MOUSE_NORMAL;
            ushort textY = MpnlY;
			DataFileHeader dlgTextDat;

            _yesNo.DrawToScreen(WithMask);
            if (text != null)
            {
                var dlgLtm = _skyText.DisplayText(text, null, true,(ushort)(_yesNo.SpriteData.s_width - 8), 37);
				dlgTextDat = dlgLtm.TextData;
                textY =
                    (ushort)
                        (MpnlY + 44 +
                         (28 - dlgTextDat.s_height) / 2);
            }
            else
                dlgTextDat = null;

			var dlgText = new TextResource(dlgTextDat.Data, 1, 0, MpnlX + 2, textY, 0, DoNothing, _system, _screenBuf);
            dlgText.DrawToScreen(WithMask);

            while (!quitPanel)
            {
                if (mouseType != wantMouse)
                {
                    mouseType = wantMouse;
                    _skyMouse.SpriteMouse(mouseType, 0, 0);
                }
                _system.GraphicsManager.UpdateScreen();
                Delay(50);
                if (_controlPanel == null)
                {
                    return false;
                }
                var mouse = _system.InputManager.GetMousePosition();
                if ((mouse.Y >= 83) && (mouse.Y <= 110))
                {
                    if ((mouse.X >= 77) && (mouse.X <= 114))
                    {
                        // over 'yes'
                        wantMouse = Logic.MOUSE_CROSS;
                        if (_mouseClicked)
                        {
                            quitPanel = true;
                            retVal = true;
                        }
                    }
                    else if ((mouse.X >= 156) && (mouse.X <= 193))
                    {
                        // over 'no'
                        wantMouse = Logic.MOUSE_CROSS;
                        if (_mouseClicked)
                        {
                            quitPanel = true;
                        }
                    }
                    else
                        wantMouse = Logic.MOUSE_NORMAL;
                }
                else
                    wantMouse = Logic.MOUSE_NORMAL;
            }
            _mouseClicked = false;
            _skyMouse.SpriteMouse(Logic.MOUSE_NORMAL, 0, 0);
            return retVal;
        }

        private void AnimClick(ConResource pButton)
        {
            if (pButton.CurSprite != pButton.NumSprites - 1)
            {
                pButton.CurSprite++;
                _text.FlushForRedraw();
                pButton.DrawToScreen(NoMask);
                _text.DrawToScreen(WithMask);
                _system.GraphicsManager.UpdateScreen();
                Delay(150);
                if (_controlPanel == null)
                    return;
                pButton.CurSprite--;
                _text.FlushForRedraw();
                pButton.DrawToScreen(NoMask);
                _text.DrawToScreen(WithMask);
                _system.GraphicsManager.UpdateScreen();
            }
        }

        private void DrawMainPanel()
        {
            Array.Clear(_screenBuf, 0, Screen.GameScreenWidth * Screen.FullScreenHeight);
            _system.GraphicsManager.CopyRectToScreen(_screenBuf, Screen.GameScreenWidth, 0, 0, Screen.GameScreenWidth,
                Screen.FullScreenHeight);
            _controlPanel.DrawToScreen(NoMask);
            _exitButton.DrawToScreen(NoMask);
            _savePanButton.DrawToScreen(NoMask);
            _restorePanButton.DrawToScreen(NoMask);
            _dosPanButton.DrawToScreen(NoMask);
            _restartPanButton.DrawToScreen(NoMask);
            _fxPanButton.DrawToScreen(NoMask);
            _musicPanButton.DrawToScreen(NoMask);
            _slode.DrawToScreen(WithMask);
            _slide.DrawToScreen(WithMask);
            _slide2.DrawToScreen(WithMask);
            _bodge.DrawToScreen(WithMask);
            if (SkyEngine.IsCDVersion)
                DrawTextCross(SystemVars.Instance.SystemFlags & TextFlagMask);
            _statusBar.DrawToScreen();
        }

        private void DrawTextCross(SystemFlags flags)
        {
            _bodge.DrawToScreen(NoMask);
            if (!flags.HasFlag(SystemFlags.AllowSpeech))
                DrawCross(151, 124);
            if (!flags.HasFlag(SystemFlags.AllowText))
                DrawCross(173, 124);
        }

        private void DrawCross(ushort x, ushort y)
        {
            _text.FlushForRedraw();
            var bufPos = y * Screen.GameScreenWidth + x;
            var crossPos = 0;
            for (ushort cnty = 0; cnty < CrossSzY; cnty++)
            {
                for (ushort cntx = 0; cntx < CrossSzX; cntx++)
                    if (CrossImg[crossPos + cntx] != 0xFF)
                        _screenBuf[bufPos + cntx] = CrossImg[crossPos + cntx];
                bufPos += Screen.GameScreenWidth;
                crossPos += CrossSzX;
            }
            bufPos = y * Screen.GameScreenWidth + x;
            _system.GraphicsManager.CopyRectToScreen(_screenBuf, bufPos, Screen.GameScreenWidth, x, y, CrossSzX,
                CrossSzY);
            _text.DrawToScreen(WithMask);
        }

        private void ButtonControl(ConResource pButton)
        {
            if (pButton == null)
            {
                _textSprite = null;
                _curButtonText = 0;
                _text.SetSprite(null);
                return;
            }
            if (_curButtonText != pButton.Text)
            {
                _textSprite = null;
                _curButtonText = (int)pButton.Text;
                if (pButton.Text != 0)
                {
                    DisplayedText textRes;
                    if (pButton.Text == 0xFFFF) // text for autosave button
                        textRes = _skyText.DisplayText("Restore Autosave", null, false, PanLineWidth, 255);
                    else
                        textRes = _skyText.DisplayText((ushort)pButton.Text, null, false, PanLineWidth, 255);
                    _textSprite = textRes.TextData;
                    _text.SetSprite(_textSprite);
                }
                else
                    _text.SetSprite(null);
            }
            var mouse = _system.InputManager.GetMousePosition();
            var destY = mouse.X - 16 >= 0 ? mouse.Y - 16 : 0;
            _text.SetXy((ushort)(mouse.X + 12), (ushort)destY);
        }

        public void DoLoadSavePanel()
        {
            if (SkyEngine.IsDemo)
                return; // I don't think this can even happen
            InitPanel();
            _skyScreen.ClearScreen();
            if (SystemVars.Instance.GameVersion.Version.Minor < 331)
                _skyScreen.SetPalette(60509);
            else
                _skyScreen.SetPalette(60510);

            _savedMouse = _skyMouse.CurrentMouseType;
            _savedCharSet = _skyText.CurrentCharSet;
            _skyText.FnSetFont(2);
            _skyMouse.SpriteMouse(Logic.MOUSE_NORMAL, 0, 0);
            _curButtonText = 0;

            SaveRestorePanel(false);

            Array.Clear(_screenBuf, 0, Screen.GameScreenWidth * Screen.FullScreenHeight);
            _system.GraphicsManager.CopyRectToScreen(_screenBuf, Screen.GameScreenWidth, 0, 0, Screen.GameScreenWidth,
                Screen.FullScreenHeight);
            _system.GraphicsManager.UpdateScreen();
            _skyScreen.ForceRefresh();
            _skyScreen.SetPaletteEndian(_skyCompact.FetchCptRaw((ushort)SystemVars.Instance.CurrentPalette));
            RemovePanel();
            _skyMouse.SpriteMouse(_savedMouse, 0, 0);
            _skyText.FnSetFont(_savedCharSet);
        }

        private ushort SaveRestorePanel(bool allowSave)
        {
            _keyPressed = new ScummInputState();
            _system.InputManager.ResetKeys();
            _mouseWheel = 0;
            ButtonControl(null);
            _text.DrawToScreen(WithMask); // flush text restore buffer

            ConResource[] lookList;
            ushort cnt;
            byte lookListLen;
            if (allowSave)
            {
                lookList = _savePanLookList;
                lookListLen = 6;
                _system.InputManager.ShowVirtualKeyboard();
            }
            else
            {
                lookList = _restorePanLookList;
                if (AutoSaveExists())
                    lookListLen = 7;
                else
                    lookListLen = 6;
            }
            bool withAutoSave = lookListLen == 7;

			var textSprites = new DataFileHeader[MaxOnScreen + 1];
            _firstText = 0;

            var saveGameTexts = LoadDescriptions().Select(s => new StringBuilder(s)).ToArray();
            _selectedGame = 0;

            bool quitPanel = false;
            bool refreshNames = true;
            bool refreshAll = true;
            ushort clickRes = 0;
            while (!quitPanel && !SkyEngine.ShouldQuit)
            {
                clickRes = 0;
                if (refreshNames || refreshAll)
                {
                    if (refreshAll)
                    {
                        _text.FlushForRedraw();
                        _savePanel.DrawToScreen(NoMask);
                        _quitButton.DrawToScreen(NoMask);
                        if (withAutoSave)
                            _autoSaveButton.DrawToScreen(NoMask);
                        refreshAll = false;
                    }
                    for (cnt = 0; cnt < MaxOnScreen; cnt++)
                        if (textSprites[cnt] != null)
                            textSprites[cnt] = null;
                    SetUpGameSprites(saveGameTexts, textSprites, _firstText, _selectedGame);
                    ShowSprites(textSprites, allowSave);
                    refreshNames = false;
                }

                _text.DrawToScreen(WithMask);
                _system.GraphicsManager.UpdateScreen();
                _mouseClicked = false;
                Delay(50);
                if (_controlPanel == null)
                    return clickRes;
                if (_keyPressed.IsKeyDown(KeyCode.Escape))
                { // escape pressed
                    _mouseClicked = false;
                    clickRes = CancelPressed;
                    quitPanel = true;
                }
                else if (_keyPressed.IsKeyDown(KeyCode.Return)) // TODO: || _keyPressed.IsKeyDown(KeyCode.Enter)
                {
                    clickRes = HandleClick(lookList[0]);
                    if (_controlPanel == null) //game state was destroyed
                        return clickRes;
                    if (clickRes == GameSaved)
                        SaveDescriptions(saveGameTexts);
                    else if (clickRes == NoDiskSpace)
                    {
                        // TODO: DisplayMessage(0, "Could not save the game. (%s)", _saveFileMan.popErrorDesc().c_str());
                    }
                    quitPanel = true;
                    _mouseClicked = false;
                    _keyPressed = new ScummInputState();
                    _system.InputManager.ResetKeys();
                }
                if (allowSave && _keyPressed.GetKeys().Count > 0)
                {
                    HandleKeyPress(_keyPressed, saveGameTexts[_selectedGame]);
                    refreshNames = true;
                    _keyPressed = new ScummInputState();
                    _system.InputManager.ResetKeys();
                }

                if (_mouseWheel != 0)
                {
                    if (_mouseWheel < 0)
                        clickRes = ShiftUp(Slow);
                    else if (_mouseWheel > 0)
                        clickRes = ShiftDown(Slow);
                    _mouseWheel = 0;
                    if (clickRes == Shifted)
                    {
                        _selectedGame = _firstText;
                        refreshNames = true;
                    }
                }

                bool haveButton = false;
                var mouse = _system.InputManager.GetMousePosition();
                for (cnt = 0; cnt < lookListLen; cnt++)
                    if (lookList[cnt].IsMouseOver((uint)mouse.X, (uint)mouse.Y))
                    {
                        ButtonControl(lookList[cnt]);
                        haveButton = true;

                        if (_mouseClicked && lookList[cnt].OnClick != 0)
                        {
                            _mouseClicked = false;

                            clickRes = HandleClick(lookList[cnt]);
                            if (_controlPanel == null) //game state was destroyed
                                return clickRes;

                            if (clickRes == Shifted)
                            {
                                _selectedGame = _firstText;
                                refreshNames = true;
                            }
                            if (clickRes == NoDiskSpace)
                            {
                                // TODO: DisplayMessage(0, "Could not save the game. (%s)", _saveFileMan.popErrorDesc().c_str());
                                quitPanel = true;
                            }
                            if ((clickRes == CancelPressed) || (clickRes == GameRestored))
                                quitPanel = true;

                            if (clickRes == GameSaved)
                            {
                                SaveDescriptions(saveGameTexts);
                                quitPanel = true;
                            }
                            if (clickRes == RestoreFailed)
                                refreshAll = true;
                        }
                    }

                if (_mouseClicked)
                {
                    if ((mouse.X >= GameNameX) && (mouse.X <= GameNameX + PanLineWidth) &&
                        (mouse.Y >= GameNameY) && (mouse.Y <= GameNameY + PanCharHeight * MaxOnScreen))
                    {

                        _selectedGame = (ushort)((mouse.Y - GameNameY) / PanCharHeight + _firstText);
                        refreshNames = true;
                    }
                }
                if (!haveButton)
                    ButtonControl(null);
            }

            for (cnt = 0; cnt < MaxOnScreen + 1; cnt++)
                textSprites[cnt] = null;

            if (allowSave)
            {
                _system.InputManager.HideVirtualKeyboard();
            }

            return clickRes;
        }

        private void HandleKeyPress(ScummInputState kbd, StringBuilder textBuf)
        {
            if (kbd.IsKeyDown(KeyCode.Backspace))
            { // backspace
                if (textBuf.Length > 0)
                    textBuf.Remove(textBuf.Length - 1, 1);
            }
            else
            {
                // TODO: do this in ScummInputState ?
                var key = (char?)kbd.GetKeys().Where(k => k >= (KeyCode)32 && k <= (KeyCode)126).Select(k => (KeyCode?)k).FirstOrDefault();
                if (!key.HasValue)
                    return;

                // Cannot enter text wider than the save/load panel
                if (_enteredTextWidth >= PanLineWidth - 10)
                    return;

                // Cannot enter text longer than MaxTextLen-1 chars, since
                // the storage is only so big. Note: The code used to incorrectly
                // allow up to MaxTextLen, which caused an out of bounds access,
                // overwriting the next entry in the list of savegames partially.
                // This could be triggered by e.g. entering lots of periods ".".
                if (textBuf.Length >= MaxTextLen - 1)
                    return;

                // Allow the key only if is a letter, a digit, or one of a selected
                // list of extra characters

                if (char.IsLetterOrDigit(key.Value) || " ,().='-&+!?\"".Contains(key.Value.ToString()))
                {
                    textBuf.Append(key);
                }
            }
        }

        private void SaveDescriptions(StringBuilder[] list)
        {
            try
            {
                using (var outf = _saveFileMan.OpenForSaving("SKY-VM.SAV"))
                {
                    for (ushort cnt = 0; cnt < MaxSaveGames; cnt++)
                    {
                        if (list[cnt].Length != 0)
                        {
                            byte[] data = list[cnt].ToString().ToCharArray().Select(c => (byte)c).ToArray();
                            outf.Write(data, 0, data.Length);
                        }
                        outf.WriteByte(0);
                    }
                }
            }
            catch (Exception)
            {
                // TODO: DisplayMessage(null, "Unable to store Savegame names to file SKY-VM.SAV. (%s)", _saveFileMan.PopErrorDesc());
            }
        }

		private void ShowSprites(DataFileHeader[] nameSprites, bool allowSave)
        {
            var drawResource = new ConResource(null, 1, 0, 0, 0, 0, 0, _system, _screenBuf);
            for (ushort cnt = 0; cnt < MaxOnScreen; cnt++)
            {
                drawResource.SetSprite(nameSprites[cnt]);
                drawResource.SetXy(GameNameX, (ushort)(GameNameY + cnt * PanCharHeight));
                if (nameSprites[cnt].flag != 0)
                { // name is highlighted
                    for (ushort cnty = (ushort)(GameNameY + cnt * PanCharHeight); cnty < GameNameY + (cnt + 1) * PanCharHeight - 1; cnty++)
                    {
                        _screenBuf.Set(cnty * Screen.GameScreenWidth + GameNameX, 37, PanLineWidth);
                    }
                    drawResource.DrawToScreen(WithMask);
                    if (allowSave)
                    {
                        drawResource.SetSprite(nameSprites[MaxOnScreen]);
                        drawResource.SetXy((ushort)(GameNameX + _enteredTextWidth + 1), (ushort)(GameNameY + cnt * PanCharHeight + 4));
                        drawResource.DrawToScreen(WithMask);
                    }
                    _system.GraphicsManager.CopyRectToScreen(_screenBuf, (GameNameY + cnt * PanCharHeight) * Screen.GameScreenWidth + GameNameX, Screen.GameScreenWidth, GameNameX, GameNameY + cnt * PanCharHeight, PanLineWidth, PanCharHeight);
                }
                else
                    drawResource.DrawToScreen(NoMask);
            }
        }

		private void SetUpGameSprites(StringBuilder[] saveGameNames, DataFileHeader[] nameSprites, ushort firstNum, ushort selectedGame)
        {
            DisplayedText textSpr;
            if (nameSprites[MaxOnScreen] == null)
            {
                textSpr = _skyText.DisplayText("-", null, false, 15, 0);
                nameSprites[MaxOnScreen] = textSpr.TextData;
            }
            for (ushort cnt = 0; cnt < MaxOnScreen; cnt++)
            {
                var nameBuf = string.Format("{0,3}: {1}", firstNum + cnt + 1, saveGameNames[firstNum + cnt]);

                if (firstNum + cnt == selectedGame)
                    textSpr = _skyText.DisplayText(nameBuf, null, false, PanLineWidth, 0);
                else
                    textSpr = _skyText.DisplayText(nameBuf, null, false, PanLineWidth, 37);
                nameSprites[cnt] = textSpr.TextData;
                if (firstNum + cnt == selectedGame)
                {
					nameSprites[cnt].flag = 1;
                    _enteredTextWidth = (ushort)textSpr.TextWidth;
                }
                else
                {
                    nameSprites[cnt].flag = 0;
                }
            }
        }

        private string[] LoadDescriptions()
        {
            var savenames = new string[MaxSaveGames];
            try
            {
                using (var inf = _saveFileMan.OpenForLoading("SKY-VM.SAV"))
                {
                    var br = new BinaryReader(inf);
                    for (int i = 0; i < MaxSaveGames; ++i)
                    {
                        savenames[i] = ReadName(br);
                    }
                }
            }
            catch (Exception)
            {
            }
            return savenames;
        }

        private string ReadName(BinaryReader reader)
        {
            var name = new List<byte>();
            byte c;
            while ((c = reader.ReadByte()) != 0)
            {
                name.Add(c);
            }
            return Encoding.UTF8.GetString(name.ToArray());
        }

        private bool AutoSaveExists()
        {
            string fName;
            bool test = false;
            if (SkyEngine.IsCDVersion)
                fName = "SKY-VM-CD.ASD";
            else
                fName = string.Format("SKY-VM{0:D3}.ASD", SystemVars.Instance.GameVersion.Version.Minor);

            try
            {
                using (var f = _saveFileMan.OpenForLoading(fName))
                {
                    test = f != null;
                }
            }
            catch (Exception) { }
            return test;
        }

        private void InitPanel()
        {
            _screenBuf = new byte[Screen.GameScreenWidth * Screen.FullScreenHeight];

            var volY = (ushort)((127 - _skyMusic.Volume) / 4 + 59 - MpnlY); // volume slider's Y coordinate
            var spdY = (ushort)((SystemVars.Instance.GameSpeed - 2) / SpeedMultiply);
            spdY += MpnlY + 83; // speed slider's initial position

            _sprites.ControlPanel = _skyDisk.LoadFile(60500);
            _sprites.Button = _skyDisk.LoadFile(60501);
            _sprites.ButtonDown = _skyDisk.LoadFile(60502);
            _sprites.SavePanel = _skyDisk.LoadFile(60503);
            _sprites.YesNo = _skyDisk.LoadFile(60504);
            _sprites.Slide = _skyDisk.LoadFile(60505);
            _sprites.Slode = _skyDisk.LoadFile(60506);
            _sprites.Slode2 = _skyDisk.LoadFile(60507);
            _sprites.Slide2 = _skyDisk.LoadFile(60508);
            if (SystemVars.Instance.GameVersion.Version.Minor < 368)
                _sprites.MusicBodge = null;
            else
                _sprites.MusicBodge = _skyDisk.LoadFile(60509);

            //Main control panel:                                            X    Y Text       OnClick
            _controlPanel = CreateResource(_sprites.ControlPanel, 1, 0, 0, 0, 0, DoNothing, Mainpanel);
            _exitButton = CreateResource(_sprites.Button, 3, 0, 16, 125, 50, Exit, Mainpanel);
            _slide = CreateResource(_sprites.Slide2, 1, 0, 19, (short)spdY, 95, SpeedSlide, Mainpanel);
            _slide2 = CreateResource(_sprites.Slide2, 1, 0, 19, (short)volY, 14, MusicSlide, Mainpanel);
            _slode = CreateResource(_sprites.Slode2, 1, 0, 9, 49, 0, DoNothing, Mainpanel);
            _restorePanButton = CreateResource(_sprites.Button, 3, 0, 58, 19, 51, RestGamePanel, Mainpanel);
            _savePanButton = CreateResource(_sprites.Button, 3, 0, 58, 39, 48, SaveGamePanel, Mainpanel);
            _dosPanButton = CreateResource(_sprites.Button, 3, 0, 58, 59, 93, QuitToDos, Mainpanel);
            _restartPanButton = CreateResource(_sprites.Button, 3, 0, 58, 79, 94, Restart, Mainpanel);
            _fxPanButton = CreateResource(_sprites.Button, 3, 0, 58, 99, 90, ToggleFx, Mainpanel);

            if (SkyEngine.IsCDVersion)
            {
                // CD Version: Toggle text/speech
                _musicPanButton = CreateResource(_sprites.Button, 3, 0, 58, 119, 52, ToggleText, Mainpanel);
            }
            else
            {
                // disk version: toggle music on/off
                _musicPanButton = CreateResource(_sprites.Button, 3, 0, 58, 119, 91, ToggleMs, Mainpanel);
            }
            _bodge = CreateResource(_sprites.MusicBodge, 2, 1, 98, 115, 0, DoNothing, Mainpanel);
            _yesNo = CreateResource(_sprites.YesNo, 1, 0, -2, 40, 0, DoNothing, Mainpanel);

            _text = new TextResource(null, 1, 0, 15, 137, 0, DoNothing, _system, _screenBuf);
            _controlPanLookList = new[]
            {
                _exitButton,
                _restorePanButton,
                _savePanButton,
                _dosPanButton,
                _restartPanButton,
                _fxPanButton,
                _musicPanButton,
                _slide,
                _slide2
            };


            // save/restore panel
            _savePanel = CreateResource(_sprites.SavePanel, 1, 0, 0, 0, 0, DoNothing, Savepanel);
            _saveButton = CreateResource(_sprites.Button, 3, 0, 29, 129, 48, SaveAGame, Savepanel);
            _downFastButton = CreateResource(_sprites.ButtonDown, 1, 0, 212, 114, 0, ShiftDownFast, Savepanel);
            _downSlowButton = CreateResource(_sprites.ButtonDown, 1, 0, 212, 104, 0, ShiftDownSlow, Savepanel);
            _upFastButton = CreateResource(_sprites.ButtonDown, 1, 0, 212, 10, 0, ShiftUpFast, Savepanel);
            _upSlowButton = CreateResource(_sprites.ButtonDown, 1, 0, 212, 21, 0, ShiftUpSlow, Savepanel);
            _quitButton = CreateResource(_sprites.Button, 3, 0, 72, 129, 49, SpCancel, Savepanel);
            _restoreButton = CreateResource(_sprites.Button, 3, 0, 29, 129, 51, RestoreAGame, Savepanel);
            _autoSaveButton = CreateResource(_sprites.Button, 3, 0, 115, 129, 0x8FFF, RestoreAuto, Savepanel);

            _savePanLookList = new[]
            {
                _saveButton,
                _downSlowButton,
                _downFastButton,
                _upFastButton,
                _upSlowButton,
                _quitButton
            };
            _restorePanLookList = new[]
            {
                _restoreButton,
                _downSlowButton,
                _downFastButton,
                _upFastButton,
                _upSlowButton,
                _quitButton,
                _autoSaveButton
            };

            _statusBar = new ControlStatus(_skyText, _system, _screenBuf);

            _textSprite = null;
        }

        private void RemovePanel()
        {
            _screenBuf = null;
            _sprites.ControlPanel = null;
            _sprites.Button = null;
            _sprites.ButtonDown = null;
            _sprites.SavePanel = null;
            _sprites.YesNo = null;
            _sprites.Slide = null;
            _sprites.Slide2 = null;
            _sprites.Slode = null;
            _sprites.Slode2 = null;
            _sprites.MusicBodge = null;
            _controlPanel = null;
            _exitButton = null;
            _controlPanel = null;
            _slide = null;
            _slide2 = null;
            _slode = null;
            _restorePanButton = null;
            _savePanel = null;
            _saveButton = null;
            _downFastButton = null;
            _downSlowButton = null;
            _upFastButton = null;
            _upSlowButton = null;
            _quitButton = null;
            _autoSaveButton = null;
            _savePanButton = null;
            _dosPanButton = null;
            _restartPanButton = null;
            _fxPanButton = null;
            _musicPanButton = null;
            _bodge = null;
            _yesNo = null;
            _text = null;
            _statusBar = null;
            _restoreButton = null;
            _textSprite = null;
        }

        private ConResource CreateResource(byte[] pSpData, uint pNSprites, uint pCurSprite, short pX, short pY,
            uint pText, byte pOnClick, byte panelType)
        {
            if (pText != 0) pText += 0x7000;
            if (panelType == Mainpanel)
            {
                pX += MpnlX;
                pY += MpnlY;
            }
            else
            {
                pX += SpnlX;
                pY += SpnlY;
            }
            return new ConResource(pSpData, pNSprites, pCurSprite, (ushort)pX, (ushort)pY, pText, pOnClick, _system,
                _screenBuf);
        }

        private struct Sprites
        {
            public byte[] ControlPanel;
            public byte[] Button;
            public byte[] ButtonDown;
            public byte[] SavePanel;
            public byte[] YesNo;
            public byte[] Slide;
            public byte[] Slode;
            public byte[] Slode2;
            public byte[] Slide2;
            public byte[] MusicBodge;
        }

        private class ConResource
        {
            public readonly uint NumSprites;
            public readonly byte OnClick;
            public readonly byte[] Screen;
            public readonly ISystem System;
            public uint CurSprite;

			public DataFileHeader SpriteData;
            public uint Text;
            public ushort X, Y;

            public ConResource(byte[] pSpData, uint pNSprites, uint pCurSprite, ushort pX, ushort pY, uint pText,
                byte pOnClick, ISystem system, byte[] screen)
            {
				SpriteData = new DataFileHeader(pSpData);
                NumSprites = pNSprites;
                CurSprite = pCurSprite;
                X = pX;
                Y = pY;
                Text = pText;
                OnClick = pOnClick;
                System = system;
                Screen = screen;
            }

			public void SetSprite(DataFileHeader pSpData)
            {
				SpriteData = pSpData;
            }

            public void SetText(uint pText)
            {
                if (pText != 0) Text = pText + 0x7000;
                else Text = 0;
            }

            public void SetXy(ushort x, ushort y)
            {
                X = x;
                Y = y;
            }

            public bool IsMouseOver(uint mouseX, uint mouseY)
            {
                return (mouseX >= X) && (mouseY >= Y) && ((ushort)mouseX <= X + SpriteData.s_width) &&
					((ushort)mouseY <= Y + SpriteData.s_height);
            }

            public virtual void DrawToScreen(bool doMask)
            {
                var screenPos = Y * Sky.Screen.GameScreenWidth + X;
                var updatePos = screenPos;

                if (SpriteData == null)
                    return;

				var spriteDataPos = DataFileHeader.Size;
				spriteDataPos += (int)(SpriteData.s_sp_size * CurSprite);
                if (doMask)
                {
					for (ushort cnty = 0; cnty < SpriteData.s_height; cnty++)
                    {
						for (ushort cntx = 0; cntx < SpriteData.s_width; cntx++)
                        {
							if (SpriteData.Data[spriteDataPos + cntx] != 0)
								Screen[screenPos + cntx] = SpriteData.Data[spriteDataPos + cntx];
                        }
                        screenPos += Sky.Screen.GameScreenWidth;
						spriteDataPos += SpriteData.s_width;
                    }
                }
                else
                {
					for (ushort cnty = 0; cnty < SpriteData.s_height; cnty++)
                    {
						Array.Copy(SpriteData.Data, spriteDataPos, Screen, screenPos, SpriteData.s_width);
                        screenPos += Sky.Screen.GameScreenWidth;
						spriteDataPos += SpriteData.s_width;
                    }
                }
                System.GraphicsManager.CopyRectToScreen(Screen, updatePos, Sky.Screen.GameScreenWidth, X, Y,
					SpriteData.s_width, SpriteData.s_height);
            }
        }

        private class TextResource : ConResource
        {
            private const int PanLineWidth = 184;
            private const int PAN_CHAR_HEIGHT = 12;
            private readonly byte[] _oldScreen;

            private ushort _oldX, _oldY;

            public TextResource(byte[] pSpData, uint pNSprites, uint pCurSprite, ushort pX, ushort pY, uint pText,
                byte pOnClick, ISystem system, byte[] screen)
                : base(pSpData, pNSprites, pCurSprite, pX, pY, pText, pOnClick, system, screen)
            {
                _oldScreen = new byte[PAN_CHAR_HEIGHT * 3 * PanLineWidth];
                _oldY = 0;
                _oldX = Sky.Screen.GameScreenWidth;
            }

            public override void DrawToScreen(bool doMask)
            {
                ushort cnty;
                ushort cpWidth, cpHeight;
                if ((_oldX == X) && (_oldY == Y) && SpriteData != null)
                    return;
                var spriteData = SpriteData;
                if (_oldX < Sky.Screen.GameScreenWidth)
                {
                    cpWidth =
                        (ushort)
                            (PanLineWidth > Sky.Screen.GameScreenWidth - _oldX
                                ? Sky.Screen.GameScreenWidth - _oldX
                                : PanLineWidth);
                    if (spriteData != null && (cpWidth > spriteData.s_width))
                        cpWidth = spriteData.s_width;
                    if (spriteData != null)
                        cpHeight =
                            (ushort)
                                (spriteData.s_height > Sky.Screen.GameScreenHeight - _oldY
                                    ? Sky.Screen.GameScreenHeight - _oldY
                                    : spriteData.s_height);
                    else
                        cpHeight = PAN_CHAR_HEIGHT;
                    for (cnty = 0; cnty < cpHeight; cnty++)
                    {
                        Array.Copy(_oldScreen, cnty * PanLineWidth, Screen,
                            (cnty + _oldY) * Sky.Screen.GameScreenWidth + _oldX, cpWidth);
                    }
                    System.GraphicsManager.CopyRectToScreen(Screen, _oldY * Sky.Screen.GameScreenWidth + _oldX,
                        Sky.Screen.GameScreenWidth, _oldX, _oldY, cpWidth, PAN_CHAR_HEIGHT);
                }
                if (spriteData == null)
                {
                    _oldX = Sky.Screen.GameScreenWidth;
                    return;
                }
                _oldX = X;
                _oldY = Y;
                cpWidth =
                    (ushort)
                        (PanLineWidth > Sky.Screen.GameScreenWidth - X ? Sky.Screen.GameScreenWidth - X : PanLineWidth);
                if (cpWidth > spriteData.s_width)
                    cpWidth = spriteData.s_width;
                cpHeight =
                    (ushort)
                        (spriteData.s_height > Sky.Screen.GameScreenHeight - Y
                            ? Sky.Screen.GameScreenHeight - Y
                            : spriteData.s_height);

                var screenPos = Y * Sky.Screen.GameScreenWidth + X;
                var copyDest = 0;
				var copySrc = DataFileHeader.Size;
                for (cnty = 0; cnty < cpHeight; cnty++)
                {
                    Array.Copy(Screen, screenPos, _oldScreen, copyDest, cpWidth);
                    for (ushort cntx = 0; cntx < cpWidth; cntx++)
                    {
						if (SpriteData.Data[copySrc + cntx] != 0)
                        {
							Screen[screenPos + cntx] = SpriteData.Data[copySrc + cntx];
                        }
                    }
                    copySrc += spriteData.s_width;
                    copyDest += PanLineWidth;
                    screenPos += Sky.Screen.GameScreenWidth;
                }
                System.GraphicsManager.CopyRectToScreen(Screen, Y * Sky.Screen.GameScreenWidth + X,
                    Sky.Screen.GameScreenWidth,
                    X, Y, cpWidth, cpHeight);
            }

            public void FlushForRedraw()
            {
                if (_oldX < Sky.Screen.GameScreenWidth)
                {
                    var cpWidth =
                        (ushort)
                            (PanLineWidth > Sky.Screen.GameScreenWidth - _oldX
                                ? Sky.Screen.GameScreenWidth - _oldX
                                : PanLineWidth);
                    for (byte cnty = 0; cnty < PAN_CHAR_HEIGHT; cnty++)
                    {
                        Array.Copy(_oldScreen, cnty * PanLineWidth, Screen,
                            (cnty + _oldY) * Sky.Screen.GameScreenWidth + _oldX, cpWidth);
                    }
                }
                _oldX = Sky.Screen.GameScreenWidth;
            }
        }

        private class ControlStatus
        {
            const int StatusWidth = 146;

            private readonly byte[] _screenBuf;
            private readonly Text _skyText;

            private readonly TextResource _statusText;
            private readonly ISystem _system;
			private DataFileHeader _textData;

            public ControlStatus(Text skyText, ISystem system, byte[] scrBuf)
            {
                _skyText = skyText;
                _system = system;
                _screenBuf = scrBuf;
                _textData = null;
                _statusText = new TextResource(null, 2, 1, 64, 163, 0, DoNothing, _system, _screenBuf);
            }

            public void SetToText(string newText)
            {
                if (_textData != null)
                {
                    _statusText.FlushForRedraw();
                    _textData = null;
                }
                DisplayedText disText = _skyText.DisplayText(newText, null, true, StatusWidth, 255);
                _textData = disText.TextData;
                _statusText.SetSprite(_textData);
                _statusText.DrawToScreen(WithMask);
            }

            public void SetToText(ushort textNum)
            {
                _textData = null;
                DisplayedText disText = _skyText.DisplayText(textNum, null, true, StatusWidth, 255);
                _textData = disText.TextData;
                _statusText.SetSprite(_textData);
                _statusText.DrawToScreen(WithMask);
            }

            public void DrawToScreen()
            {
                _statusText.FlushForRedraw();
                _statusText.DrawToScreen(WithMask);
            }
        }
    }
}