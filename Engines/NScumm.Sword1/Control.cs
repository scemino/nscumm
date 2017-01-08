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
using System.IO;
using System.Linq;
using System.Text;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sword1
{
    enum LangStrings
    {
        STR_PAUSED = 0,
        STR_INSERT_CD_A,
        STR_INSERT_CD_B,
        STR_INCORRECT_CD,
        STR_SAVE,
        STR_RESTORE,
        STR_RESTART,
        STR_START,
        STR_QUIT,
        STR_SPEED,
        STR_VOLUME,
        STR_TEXT,
        STR_DONE,
        STR_OK,
        STR_CANCEL,
        STR_MUSIC,
        STR_SPEECH,
        STR_FX,
        STR_THE_END,
        STR_DRIVE_FULL
    }

    enum ButtonIds
    {
        BUTTON_DONE = 1,
        BUTTON_MAIN_PANEL,
        BUTTON_SAVE_PANEL,
        BUTTON_RESTORE_PANEL,
        BUTTON_RESTART,
        BUTTON_QUIT,
        BUTTON_SPEED,
        BUTTON_VOLUME_PANEL,
        BUTTON_TEXT,
        BUTTON_CONFIRM,
        //-
        BUTTON_SCROLL_UP_FAST,
        BUTTON_SCROLL_UP_SLOW,
        BUTTON_SCROLL_DOWN_SLOW,
        BUTTON_SCROLL_DOWN_FAST,
        BUTTON_SAVE_SELECT1,
        BUTTON_SAVE_SELECT2,
        BUTTON_SAVE_SELECT3,
        BUTTON_SAVE_SELECT4,
        BUTTON_SAVE_SELECT5,
        BUTTON_SAVE_SELECT6,
        BUTTON_SAVE_SELECT7,
        BUTTON_SAVE_SELECT8,
        BUTTON_SAVE_RESTORE_OKAY,
        BUTTON_SAVE_CANCEL,
        //-
        CONFIRM_OKAY,
        CONFIRM_CANCEL
    }

    [Flags]
    enum TextModes
    {
        TEXT_LEFT_ALIGN = 0,
        TEXT_CENTER,
        TEXT_RIGHT_ALIGN,
        TEXT_RED_FONT = 128
    }

    class Control
    {
        const int TumbnailVersion = 1;

        public const int CONTROL_NOTHING_DONE = 0;
        public const int CONTROL_GAME_RESTORED = 1;
        public const int CONTROL_RESTART_GAME = 2;
        private const int MAX_BUTTONS = 16;

        private const int kButtonOk = 1;
        private const int kButtonCancel = 2;

        private static readonly int SAVEGAME_HEADER = (int)ScummHelper.MakeTag('B', 'S', '_', '1');
        private const int SAVEGAME_VERSION = 2;


        private ISaveFileManager _saveFileMan;
        private ResMan _resMan;
        private ObjectMan _objMan;
        private ISystem _system;
        private Mouse _mouse;
        private Music _music;
        private Sound _sound;
        private string[] _lStrings;

        byte[] _restoreBuf;
        byte _saveFiles;
        byte _numSaves;
        byte _saveScrollPos;
        byte _selectedSavegame;
        List<string> _saveNames = new List<string>();
        string _oldName;
        byte _cursorTick;
        bool _cursorVisible;
        bool _panelShown;
        byte _numButtons;
        private byte _selectedButton;
        ControlButton[] _buttons = new ControlButton[MAX_BUTTONS];

        ScummInputState _keyPressed;
        Point _mouseCoord;
        ushort _mouseState;
        bool _mouseDown;
        private byte[] _screenBuf;


        public Control(ISaveFileManager saveFileMan, ResMan resMan, ObjectMan objMan, ISystem system, Mouse mouse, Sound sound, Music music)
        {
            _saveFileMan = saveFileMan;
            _resMan = resMan;
            _objMan = objMan;
            _system = system;
            _mouse = mouse;
            _music = music;
            _sound = sound;
            _lStrings = _languageStrings[(int)SystemVars.Language];
            _selectedButton = 255;
        }

        public byte RunPanel()
        {
            _panelShown = true;
            _mouseDown = false;
            _restoreBuf = null;
            _numButtons = 0;
            _screenBuf = new byte[640 * 480];
            _system.GraphicsManager.CopyRectToScreen(_screenBuf, 640, 0, 0, 640, 480);
            _sound.QuitScreen();

            uint fontId = SwordRes.SR_FONT, redFontId = SwordRes.SR_REDFONT;
            if (SystemVars.Language == Language.BS1_CZECH)
            {
                fontId = SwordRes.CZECH_SR_FONT;
                redFontId = SwordRes.CZECH_SR_REDFONT;
            }
            _font = _resMan.OpenFetchRes(fontId);
            _redFont = _resMan.OpenFetchRes(redFontId);

            var pal = _resMan.OpenFetchRes(SwordRes.SR_PALETTE);
            var palOut = new Color[256];
            for (ushort cnt = 1; cnt < 256; cnt++)
            {
                palOut[cnt] = Color.FromRgb(pal[cnt * 3 + 0] << 2, pal[cnt * 3 + 1] << 2, pal[cnt * 3 + 2] << 2);
            }
            palOut[0] = Color.FromRgb(0, 0, 0);
            _resMan.ResClose(SwordRes.SR_PALETTE);
            _system.GraphicsManager.SetPalette(palOut, 0, 256);
            ButtonIds mode = 0;
            ButtonIds newMode = ButtonIds.BUTTON_MAIN_PANEL;
            bool fullRefresh = false;
            _mouse.ControlPanel(true);
            byte retVal = CONTROL_NOTHING_DONE;
            _music.StartMusic(61, 1);

            do
            {
                if (newMode != 0)
                {
                    mode = newMode;
                    fullRefresh = true;
                    DestroyButtons();
                    Array.Clear(_screenBuf, 0, 640 * 480);
                    if (mode != ButtonIds.BUTTON_SAVE_PANEL)
                        _cursorVisible = false;
                }
                switch (mode)
                {
                    case ButtonIds.BUTTON_MAIN_PANEL:
                        if (fullRefresh)
                            SetupMainPanel();
                        break;
                    case ButtonIds.BUTTON_SAVE_PANEL:
                        if (fullRefresh)
                        {
                            SetupSaveRestorePanel(true);
                        }
                        if (_selectedSavegame < 255)
                        {
                            _system.InputManager.ShowVirtualKeyboard();
                            bool visible = _cursorVisible;
                            _cursorTick++;
                            if (_cursorTick == 7)
                                _cursorVisible = true;
                            else if (_cursorTick == 14)
                            {
                                _cursorVisible = false;
                                _cursorTick = 0;
                            }
                            if (_keyPressed.GetKeys().Count > 0)
                            {
                                HandleSaveKey(_keyPressed);
                                _keyPressed = new ScummInputState();
                                _system.InputManager.ResetKeys();
                            }
                            else if (_cursorVisible != visible)
                                ShowSavegameNames();
                        }
                        else
                        {
                            _system.InputManager.HideVirtualKeyboard();
                        }
                        break;
                    case ButtonIds.BUTTON_RESTORE_PANEL:
                        if (fullRefresh)
                            SetupSaveRestorePanel(false);
                        break;
                    case ButtonIds.BUTTON_VOLUME_PANEL:
                        if (fullRefresh)
                            SetupVolumePanel();
                        break;
                    default:
                        break;
                }
                if (fullRefresh)
                {
                    fullRefresh = false;
                    _system.GraphicsManager.CopyRectToScreen(_screenBuf, Screen.SCREEN_WIDTH, 0, 0, Screen.SCREEN_WIDTH, 480);
                }
                Delay(1000 / 12);
                newMode = GetClicks(mode, out retVal);
            } while ((newMode != ButtonIds.BUTTON_DONE) && (retVal == 0) && (!Engine.Instance.HasToQuit));

            if (SystemVars.ControlPanelMode == ControlPanelMode.CP_NORMAL)
            {
                byte volL, volR;
                _music.GiveVolume(out volL, out volR);
                ConfigManager.Instance.Set<int>("music_volume", (volR + volL) / 2);
                ConfigManager.Instance.Set<int>("music_balance", VolToBalance(volL, volR));

                _sound.GiveSpeechVol(out volL, out volR);
                ConfigManager.Instance.Set<int>("speech_volume", (volR + volL) / 2);
                ConfigManager.Instance.Set<int>("speech_balance", VolToBalance(volL, volR));

                _sound.GiveSfxVol(out volL, out volR);
                ConfigManager.Instance.Set<int>("sfx_volume", ((volR + volL) / 2));
                ConfigManager.Instance.Set<int>("sfx_balance", VolToBalance(volL, volR));

                ConfigManager.Instance.Set<bool>("subtitles", SystemVars.ShowText == 1);
                // TODO: conf
                //ConfMan.flushToDisk();
            }

            DestroyButtons();
            _resMan.ResClose(fontId);
            _resMan.ResClose(redFontId);
            Array.Clear(_screenBuf, 0, 640 * 480);
            _system.GraphicsManager.CopyRectToScreen(_screenBuf, 640, 0, 0, 640, 480);
            _screenBuf = null;
            _mouse.ControlPanel(false);
            // Can also be used to end the control panel music.
            _music.StartMusic((int)Logic.ScriptVars[(int)ScriptVariableNames.CURRENT_MUSIC], 1);
            _sound.NewScreen(Logic.ScriptVars[(int)ScriptVariableNames.SCREEN]);
            _panelShown = false;
            return retVal;
        }

        public void AskForCd()
        {
            _screenBuf = new byte[640 * 480];
            uint fontId = SwordRes.SR_FONT;
            if (SystemVars.Language == Language.BS1_CZECH)
                fontId = SwordRes.CZECH_SR_FONT;
            _font = _resMan.OpenFetchRes(fontId);
            var pal = _resMan.OpenFetchRes(SwordRes.SR_PALETTE);
            var palOut = new Color[256];
            for (var cnt = 1; cnt < 256; cnt++)
            {
                palOut[cnt] = Color.FromRgb(pal[cnt * 3 + 0] << 2, pal[cnt * 3 + 1] << 2, pal[cnt * 3 + 2] << 2);
            }
            palOut[0] = Color.FromRgb(0, 0, 0);
            _resMan.ResClose(SwordRes.SR_PALETTE);
            _system.GraphicsManager.SetPalette(palOut, 0, 256);

            var fName = $"cd{SystemVars.CurrentCd}.id";
            var textA = $"{_lStrings[(int)LangStrings.STR_INSERT_CD_A]}{SystemVars.CurrentCd}";
            bool notAccepted = true;
            bool refreshText = true;
            do
            {
                if (refreshText)
                {
                    Array.Clear(_screenBuf, 0, 640 * 480);
                    RenderText(textA, 320, 220, TextModes.TEXT_CENTER);
                    RenderText(_lStrings[(int)LangStrings.STR_INSERT_CD_B], 320, 240, TextModes.TEXT_CENTER);
                    _system.GraphicsManager.CopyRectToScreen(_screenBuf, 640, 0, 0, 640, 480);
                }
                Delay(300);
                if (_keyPressed.GetKeys().Count > 0)
                {
                    if (!ServiceLocator.FileStorage.FileExists(fName))
                    {
                        Array.Clear(_screenBuf, 0, 640 * 480);
                        RenderText(_lStrings[(int)LangStrings.STR_INCORRECT_CD], 320, 230, TextModes.TEXT_CENTER);
                        _system.GraphicsManager.CopyRectToScreen(_screenBuf, 640, 0, 0, 640, 480);
                        Delay(2000);
                        refreshText = true;
                    }
                    else
                    {
                        notAccepted = false;
                    }
                }
            } while (notAccepted && (!Engine.Instance.HasToQuit));

            _resMan.ResClose(fontId);
            _screenBuf = null;
        }

        static int VolToBalance(int volL, int volR)
        {
            if (volL + volR == 0)
            {
                return 50;
            }
            return (100 * volL / (volL + volR));
        }

        private ButtonIds GetClicks(ButtonIds mode, out byte retVal)
        {
            retVal = 0;
            byte checkButtons = _numButtons;
            if (mode == ButtonIds.BUTTON_VOLUME_PANEL)
            {
                HandleVolumeClicks();
                checkButtons = 1;
            }

            byte flag = 0;
            if (_keyPressed.IsKeyDown(KeyCode.Escape))
                flag = kButtonCancel;
            else if (_keyPressed.IsKeyDown(KeyCode.Return) /*|| _keyPressed.IsKeyDown(KeyCode.Enter)*/)
                flag = kButtonOk;

            if (flag != 0)
            {
                for (var cnt = 0; cnt < checkButtons; cnt++)
                    if (_buttons[cnt]._flag == flag)
                        return HandleButtonClick(_buttons[cnt]._id, mode, out retVal);
            }

            if (_mouseState == 0)
                return 0;
            if ((_mouseState & Mouse.BS1L_BUTTON_DOWN) != 0)
                for (var cnt = 0; cnt < checkButtons; cnt++)
                    if (_buttons[cnt].WasClicked((ushort)_mouseCoord.X, (ushort)_mouseCoord.Y))
                    {
                        _selectedButton = (byte)cnt;
                        _buttons[cnt].SetSelected(1);
                        if (_buttons[cnt].IsSaveslot())
                            ShowSavegameNames();
                    }
            if ((_mouseState & Mouse.BS1L_BUTTON_UP) != 0)
            {
                for (var cnt = 0; cnt < checkButtons; cnt++)
                    if (_buttons[cnt].WasClicked((ushort)_mouseCoord.X, (ushort)_mouseCoord.Y))
                        if (_selectedButton == cnt)
                        {
                            // saveslots stay selected after clicking
                            if (!_buttons[cnt].IsSaveslot())
                                _buttons[cnt].SetSelected(0);
                            _selectedButton = 255;
                            return HandleButtonClick(_buttons[cnt]._id, mode, out retVal);
                        }
                if (_selectedButton < checkButtons)
                {
                    _buttons[_selectedButton].SetSelected(0);
                    if (_buttons[_selectedButton].IsSaveslot())
                        ShowSavegameNames();
                }
                _selectedButton = 255;
            }
            if ((_mouseState & Mouse.BS1_WHEEL_UP) != 0)
            {
                for (var cnt = 0; cnt < checkButtons; cnt++)
                    if (_buttons[cnt]._id == ButtonIds.BUTTON_SCROLL_UP_SLOW)
                        return HandleButtonClick(_buttons[cnt]._id, mode, out retVal);
            }
            if ((_mouseState & Mouse.BS1_WHEEL_DOWN) != 0)
            {
                for (var cnt = 0; cnt < checkButtons; cnt++)
                    if (_buttons[cnt]._id == ButtonIds.BUTTON_SCROLL_DOWN_SLOW)
                        return HandleButtonClick(_buttons[cnt]._id, mode, out retVal);
            }
            return 0;
        }

        private void HandleVolumeClicks()
        {
            if (_mouseDown)
            {
                byte clickedId = 0;
                for (byte cnt = 1; cnt < 4; cnt++)
                    if (_buttons[cnt].WasClicked((ushort)_mouseCoord.X, (ushort)_mouseCoord.Y))
                        clickedId = cnt;
                if (clickedId != 0)
                { // these are circle shaped, so check again if it was clicked.
                    byte clickDest = 0;
                    short mouseDiffX = (short)(_mouseCoord.X - (_volumeButtons[clickedId].x + 48));
                    short mouseDiffY = (short)(_mouseCoord.Y - (_volumeButtons[clickedId].y + 48));
                    short mouseOffs = (short)Math.Sqrt((mouseDiffX * mouseDiffX + mouseDiffY * mouseDiffY));
                    // check if the player really hit the button (but not the center).
                    if ((mouseOffs <= 42) && (mouseOffs >= 8))
                    {
                        if (mouseDiffX > 8)
                        { // right part
                            if (mouseDiffY < -8) // upper right
                                clickDest = 2;
                            else if (Math.Abs(mouseDiffY) <= 8) // right
                                clickDest = 3;
                            else                 // lower right
                                clickDest = 4;
                        }
                        else if (mouseDiffX < -8)
                        { // left part
                            if (mouseDiffY < -8) // upper left
                                clickDest = 8;
                            else if (Math.Abs(mouseDiffY) <= 8) // left
                                clickDest = 7;
                            else                 // lower left
                                clickDest = 6;
                        }
                        else
                        { // middle
                            if (mouseDiffY < -8)
                                clickDest = 1; // upper
                            else if (mouseDiffY > 8)
                                clickDest = 5; // lower
                        }
                    }
                    _buttons[clickedId].SetSelected(clickDest);
                    ChangeVolume(clickedId, clickDest);
                }
            }
            else if ((_mouseState & Mouse.BS1L_BUTTON_UP) != 0)
            {
                _buttons[1].SetSelected(0);
                _buttons[2].SetSelected(0);
                _buttons[3].SetSelected(0);
            }
        }

        private void ChangeVolume(byte id, byte action)
        {
            // ids: 1 = music, 2 = speech, 3 = sfx
            byte volL = 0, volR = 0;
            if (id == 1)
                _music.GiveVolume(out volL, out volR);
            else if (id == 2)
                _sound.GiveSpeechVol(out volL, out volR);
            else if (id == 3)
                _sound.GiveSfxVol(out volL, out volR);

            sbyte direction = 0;
            if ((action >= 4) && (action <= 6)) // lower part of the button => decrease volume
                direction = -1;
            else if ((action == 8) || (action == 1) || (action == 2)) // upper part => increase volume
                direction = 1;
            else if ((action == 3) || (action == 7)) // middle part => pan volume
                direction = 1;
            sbyte factorL = 8, factorR = 8;
            if ((action >= 6) && (action <= 8))
            { // left part => left pan
                factorL = 8;
                factorR = (sbyte)((action == 7) ? -8 : 0);
            }
            else if ((action >= 2) && (action <= 4))
            { // right part
                factorR = 8;
                factorL = (sbyte)((action == 3) ? -8 : 0);
            }
            short resVolL = (short)(volL + direction * factorL);
            short resVolR = (short)(volR + direction * factorR);

            volL = (byte)Math.Max((short)0, Math.Min(resVolL, (short)255));
            volR = (byte)Math.Max((short)0, Math.Min(resVolR, (short)255));

            if (id == 1)
                _music.SetVolume(volL, volR);
            else if (id == 2)
                _sound.SetSpeechVol(volL, volR);
            else if (id == 3)
                _sound.SetSfxVol(volL, volR);

            RenderVolumeBar(id, volL, volR);
        }

        private void RenderVolumeBar(byte id, byte volL, byte volR)
        {
            ushort destX = (ushort)(_volumeButtons[id].x + 20);
            ushort destY = (ushort)(_volumeButtons[id].y + 116);

            for (var chCnt = 0; chCnt < 2; chCnt++)
            {
                byte vol = (chCnt == 0) ? volL : volR;
                FrameHeader frHead = new FrameHeader(_resMan.FetchFrame(_resMan.OpenFetchRes(SwordRes.SR_VLIGHT), (uint)((vol + 15) >> 4)));
                var destMem = new ByteAccess(_screenBuf, destY * Screen.SCREEN_WIDTH + destX);
                var srcMem = new ByteAccess(frHead.Data.Data, frHead.Data.Offset + FrameHeader.Size);
                ushort barHeight = _resMan.ReadUInt16(frHead.height);
                byte[] psxVolBuf = null;

                if (SystemVars.Platform == Platform.PSX)
                {
                    psxVolBuf = new byte[_resMan.ReadUInt16(frHead.height) / 2 * _resMan.ReadUInt16(frHead.width)];
                    Screen.DecompressHIF(srcMem.Data, srcMem.Offset, psxVolBuf);
                    srcMem = new ByteAccess(psxVolBuf);
                    barHeight /= 2;
                }

                for (ushort cnty = 0; cnty < barHeight; cnty++)
                {
                    Array.Copy(srcMem.Data, srcMem.Offset, destMem.Data, destMem.Offset, _resMan.ReadUInt16(frHead.width));

                    if (SystemVars.Platform == Platform.PSX)
                    {
                        //linedoubling
                        destMem.Offset += Screen.SCREEN_WIDTH;
                        Array.ConstrainedCopy(srcMem.Data, srcMem.Offset, destMem.Data, destMem.Offset, _resMan.ReadUInt16(frHead.width));
                    }

                    srcMem.Offset += _resMan.ReadUInt16(frHead.width);
                    destMem.Offset += Screen.SCREEN_WIDTH;
                }

                _system.GraphicsManager.CopyRectToScreen(new BytePtr(_screenBuf, destY * Screen.SCREEN_WIDTH + destX), Screen.SCREEN_WIDTH, destX, destY, _resMan.ReadUInt16(frHead.width), _resMan.ReadUInt16(frHead.height));
                _resMan.ResClose(SwordRes.SR_VLIGHT);
                destX += 32;
            }
        }

        private ButtonIds HandleButtonClick(ButtonIds id, ButtonIds mode, out byte retVal)
        {
            retVal = 0;
            switch (mode)
            {
                case ButtonIds.BUTTON_MAIN_PANEL:
                    if (id == ButtonIds.BUTTON_RESTART)
                    {
                        if (SystemVars.ControlPanelMode != ControlPanelMode.CP_NORMAL) // if player is dead or has just started, don't ask for confirmation
                            retVal |= CONTROL_RESTART_GAME;
                        else if (GetConfirm(_lStrings[(int)LangStrings.STR_RESTART]))
                            retVal |= CONTROL_RESTART_GAME;
                        else
                            return mode;
                    }
                    else if ((id == ButtonIds.BUTTON_RESTORE_PANEL) || (id == ButtonIds.BUTTON_SAVE_PANEL) ||
                             (id == ButtonIds.BUTTON_DONE) || (id == ButtonIds.BUTTON_VOLUME_PANEL))
                        return id;
                    else if (id == ButtonIds.BUTTON_TEXT)
                    {
                        SystemVars.ShowText ^= 1;
                        _buttons[5].SetSelected(SystemVars.ShowText);
                    }
                    else if (id == ButtonIds.BUTTON_QUIT)
                    {
                        if (GetConfirm(_lStrings[(int)LangStrings.STR_QUIT]))
                            Engine.Instance.HasToQuit = true;
                        return mode;
                    }
                    break;
                case ButtonIds.BUTTON_SAVE_PANEL:
                case ButtonIds.BUTTON_RESTORE_PANEL:
                    if ((id >= ButtonIds.BUTTON_SCROLL_UP_FAST) && (id <= ButtonIds.BUTTON_SCROLL_DOWN_FAST))
                        SaveNameScroll(id, mode == ButtonIds.BUTTON_SAVE_PANEL);
                    else if ((id >= ButtonIds.BUTTON_SAVE_SELECT1) && (id <= ButtonIds.BUTTON_SAVE_SELECT8))
                        SaveNameSelect(id, mode == ButtonIds.BUTTON_SAVE_PANEL);
                    else if (id == ButtonIds.BUTTON_SAVE_RESTORE_OKAY)
                    {
                        if (mode == ButtonIds.BUTTON_SAVE_PANEL)
                        {
                            _system.InputManager.HideVirtualKeyboard();
                            if (SaveToFile()) // don't go back to main panel if save fails.
                                return ButtonIds.BUTTON_DONE;
                        }
                        else
                        {
                            if (RestoreFromFile())
                            { // don't go back to main panel if restore fails.
                                retVal |= CONTROL_GAME_RESTORED;
                                return ButtonIds.BUTTON_MAIN_PANEL;
                            }
                        }
                    }
                    else if (id == ButtonIds.BUTTON_SAVE_CANCEL)
                    {
                        _system.InputManager.HideVirtualKeyboard();
                        return ButtonIds.BUTTON_MAIN_PANEL; // mode down to main panel
                    }
                    break;
                case ButtonIds.BUTTON_VOLUME_PANEL:
                    return id;
            }
            return 0;
        }

        private bool RestoreFromFile()
        {
            if (_selectedSavegame < 255)
            {
                return RestoreGameFromFile(_selectedSavegame);
            }
            return false;
        }

        public bool RestoreGameFromFile(byte slot)
        {
            ushort cnt;
            var fName = $"sword1.{slot:D3}";
            using (var inf = new BinaryReader(_saveFileMan.OpenForLoading(fName)))
            {
                // TODO:
                //if (inf==null)
                //{
                //    // Display an error message, and do nothing
                //    // TODO: DisplayMessage(0, "Can't open file '%s'. (%s)", fName, _saveFileMan.popErrorDesc().c_str());
                //    return false;
                //}

                uint saveHeader = inf.ReadUInt32();
                if (saveHeader != SAVEGAME_HEADER)
                {
                    // Display an error message, and do nothing
                    // TODO: DisplayMessage(0, "Save game '%s' is corrupt", fName);
                    return false;
                }

                inf.BaseStream.Seek(40, SeekOrigin.Current); // skip description
                byte saveVersion = inf.ReadByte();

                if (saveVersion > SAVEGAME_VERSION)
                {
                    Warning("Different save game version");
                    return false;
                }

                if (saveVersion < 2) // These older version of the savegames used a flag to signal presence of thumbnail
                    inf.BaseStream.Seek(1, SeekOrigin.Current);

                SkipThumbnail(inf);

                inf.ReadUInt32BigEndian(); // save date
                inf.ReadUInt16BigEndian(); // save time

                if (saveVersion < 2)
                {
                    // Before version 2 we didn't had play time feature
                    Engine.Instance.TotalPlayTime = 0;
                }
                else
                {
                    var time = inf.ReadUInt32BigEndian();
                    Engine.Instance.TotalPlayTime = ((int)(time * 1000));
                }

                _restoreBuf = new byte[
                    ObjectMan.TOTAL_SECTIONS * 2 +
                    Logic.NUM_SCRIPT_VARS * 4 +
                    (SwordObject.Size - 12000)];

                var liveBuf = new UShortAccess(_restoreBuf);
                var scriptBuf = new UIntAccess(_restoreBuf, 2 * ObjectMan.TOTAL_SECTIONS);
                var playerBuf = new UIntAccess(_restoreBuf, 2 * ObjectMan.TOTAL_SECTIONS + 4 * Logic.NUM_SCRIPT_VARS);

                for (cnt = 0; cnt < ObjectMan.TOTAL_SECTIONS; cnt++)
                    liveBuf[cnt] = inf.ReadUInt16();

                for (cnt = 0; cnt < Logic.NUM_SCRIPT_VARS; cnt++)
                    scriptBuf[cnt] = inf.ReadUInt32();

                uint playerSize = (SwordObject.Size - 12000) / 4;
                for (var cnt2 = 0; cnt2 < playerSize; cnt2++)
                    playerBuf[cnt2] = inf.ReadUInt32();

                // TODO: error
                //if (inf.err() || inf.eos())
                //{
                //    displayMessage(0, "Can't read from file '%s'. (%s)", fName, _saveFileMan.popErrorDesc().c_str());
                //    delete inf;
                //    free(_restoreBuf);
                //    _restoreBuf = NULL;
                //    return false;
                //}
            }

            return true;
        }

        private bool SkipThumbnail(BinaryReader reader)
        {
            var position = reader.BaseStream.Position;
            var header = LoadHeader(reader);

            if (header == null)
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                return false;
            }

            reader.BaseStream.Seek(header.Size - (reader.BaseStream.Position - position), SeekOrigin.Current);
            return true;
        }

        private static ThumbnailHeader LoadHeader(BinaryReader reader)
        {
            var header = new ThumbnailHeader();
            header.Type = ScummHelper.SwapBytes(reader.ReadUInt32());
            // We also accept the bad 'BMHT' header here, for the sake of compatibility
            // with some older savegames which were written incorrectly due to a bug in
            // ScummVM which wrote the thumb header type incorrectly on LE systems.
            if (header.Type != ScummHelper.MakeTag('T', 'H', 'M', 'B') && header.Type != ScummHelper.MakeTag('B', 'M', 'H', 'T'))
            {
                //if (outputWarnings)
                //    warning("couldn't find thumbnail header type");
                return null;
            }

            header.Size = ScummHelper.SwapBytes(reader.ReadUInt32());
            header.Version = reader.ReadByte();

            if (header.Version > TumbnailVersion)
            {
                //if (outputWarnings)
                //    warning("trying to load a newer thumbnail version: %d instead of %d", header.version, THMB_VERSION);
                return null;
            }

            header.Width = ScummHelper.SwapBytes(reader.ReadUInt16());
            header.Height = ScummHelper.SwapBytes(reader.ReadUInt16());
            header.Bpp = reader.ReadByte();

            return header;
        }

        private bool SaveToFile()
        {
            if ((_selectedSavegame == 255) || string.IsNullOrEmpty(_saveNames[_selectedSavegame]))
                return false; // no saveslot selected or no name entered
            SaveGameToFile(_selectedSavegame);
            return true;
        }

        private byte[] StrToBytes(string text, int length)
        {
            var name = new byte[length];
            var saveName = text.ToCharArray().Select(c => (byte)c).ToArray();
            Array.Copy(saveName, name, saveName.Length);
            return name;
        }

        private bool IsPanelShown()
        {
            return _panelShown;
        }

        // TODO: share these methods
        void SaveThumbnail(BinaryWriter output)
        {
            Surface thumb = CreateThumbnailFromScreen();
            SaveThumbnail(output, thumb);
        }

        const int ThumbnailHeaderSize = (4 + 4 + 1 + 2 + 2 + (1 + 4 + 4));
        const int THMB_VERSION = 2;

        static void SaveThumbnail(BinaryWriter output, Surface thumb)
        {
            var bpp = Surface.GetBytesPerPixel(thumb.PixelFormat);
            if (bpp != 2 && bpp != 4)
            {
                Warning($"trying to save thumbnail with bpp {bpp}");
                return;
            }

            ThumbnailHeader header = new ThumbnailHeader();
            header.Type = ScummHelper.MakeTag('T', 'H', 'M', 'B');
            header.Size = (uint)(ThumbnailHeaderSize + thumb.Width * thumb.Height * bpp);
            header.Version = THMB_VERSION;
            header.Width = (ushort)thumb.Width;
            header.Height = (ushort)thumb.Height;

            output.WriteUInt32BigEndian(header.Type);
            output.WriteUInt32BigEndian(header.Size);
            output.WriteByte(header.Version);
            output.WriteUInt16BigEndian(header.Width);
            output.WriteUInt16BigEndian(header.Height);

            // Serialize the PixelFormat
            output.WriteByte(bpp);
            output.WriteByte(3);
            output.WriteByte(2);
            output.WriteByte(3);
            output.WriteByte(8);
            output.WriteByte(11);
            output.WriteByte(5);
            output.WriteByte(0);
            output.WriteByte(0);

            // Serialize the pixel data
            for (uint y = 0; y < thumb.Height; ++y)
            {
                switch (bpp)
                {
                    case 2:
                        {
                            var pixels = new UShortAccess(thumb.Pixels, (int)(y * thumb.Width * 2));
                            for (uint x = 0; x < thumb.Width; ++x)
                            {
                                output.WriteUInt16BigEndian(pixels[0]);
                                pixels.Data.Offset += 2;
                            }
                        }
                        break;

                    case 4:
                        {
                            var pixels = new UIntAccess(thumb.Pixels, (int)(y * thumb.Width * 4));
                            for (var x = 0; x < thumb.Width; ++x)
                            {
                                output.WriteUInt32BigEndian(pixels[0]);
                                pixels.Offset += 4;
                            }
                        }
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        Surface CreateThumbnailFromScreen()
        {
            Surface screen = GrabScreen565();
            return CreateThumbnail(screen);
        }

        Surface GrabScreen565()
        {
            _system.GraphicsManager.Capture(ref _screen);

            var bpp = Surface.GetBytesPerPixel(_screen.PixelFormat);
            System.Diagnostics.Debug.Assert(bpp == 1 || bpp == 2);
            System.Diagnostics.Debug.Assert(_screen.Pixels != BytePtr.Null);

            PixelFormat screenFormat = _system.GraphicsManager.PixelFormat;
            var screenBpp = Surface.GetBytesPerPixel(screenFormat);

            var surf = new Surface(_screen.Width, _screen.Height, PixelFormat.Rgb16, false);

            Color[] palette = null;
            if (screenBpp == 1)
            {
                palette = _system.GraphicsManager.GetPalette();
            }

            for (int y = 0; y < _screen.Height; ++y)
            {
                for (int x = 0; x < _screen.Width; ++x)
                {
                    Color c = new Color();
                    if (screenBpp == 1)
                    {
                        var pixel = _screen.Pixels[x + y * _screen.Width];
                        c = palette[pixel];
                    }
                    else if (screenBpp == 2)
                    {
                        ushort col = _screen.Pixels.ToUInt16(x * 2 + y * 2 * _screen.Width);
                        byte r, g, b;
                        ColorHelper.ColorToRGB(col, out r, out g, out b);
                        c = Color.FromRgb(r, g, b);
                    }

                    var colDst = ColorHelper.RGBToColor((byte)c.R, (byte)c.G, (byte)c.B);
                    surf.Pixels.WriteUInt16(x * 2 + y * 2 * _screen.Width, colDst);
                }
            }

            return surf;
        }

        // creates a 160x100 thumbnail for 320x200 games
        // and 160x120 thumbnail for 320x240 and 640x480 games
        // only 565 mode
        enum ThumbnailSize
        {
            ThumbnailWidth = 160,
            ThumbnailHeight1 = 100,
            ThumbnailHeight2 = 120
        }

        static Surface CreateThumbnail(Surface input)
        {
            int height;
            if ((input.Width == 320 && input.Height == 200) || (input.Width == 640 && input.Height == 400))
            {
                height = (int)ThumbnailSize.ThumbnailHeight1;
            }
            else
            {
                height = (int)ThumbnailSize.ThumbnailHeight2;
            }

            var output = new Surface((int)ThumbnailSize.ThumbnailWidth, (ushort) height, PixelFormat.Rgb16, false);
            ScaleThumbnail(input, output);

            return output;
        }

        static void ScaleThumbnail(Surface input, Surface output)
        {
            // TODO:
            //while (input.Width / output.Width >= 4 || input.Height / output.Height >= 4)
            //{
            //    createThumbnail_4 < 565 > ((const uint8*)input.getPixels(), input.pitch, (uint8*)input.getPixels(), input.pitch, input.w, input.h);
            //    input.w /= 4;
            //    input.h /= 4;
            //}

            //while (input.w / output.w >= 2 || input.h / output.h >= 2)
            //{
            //    createThumbnail_2 < 565 > ((const uint8*)input.getPixels(), input.pitch, (uint8*)input.getPixels(), input.pitch, input.w, input.h);
            //    input.w /= 2;
            //    input.h /= 2;
            //}

            //if ((input.w == output.w && input.h < output.h) || (input.w < output.w && input.h == output.h))
            //{
            //    // In this case we simply center the input surface in the output
            //    uint8* dst = (uint8*)output.getBasePtr((output.w - input.w) / 2, (output.h - input.h) / 2);
            //    const uint8* src = (const uint8*)input.getPixels();

            //    for (int y = 0; y < input.h; ++y)
            //    {
            //        memcpy(dst, src, input.w * input.format.bytesPerPixel);
            //        src += input.pitch;
            //        dst += output.pitch;
            //    }
            //}
            //else
            //{
            //    // Assure the aspect of the scaled image still matches the original.
            //    int targetWidth = output.w, targetHeight = output.h;

            //    const float inputAspect = (float)input.w / input.h;
            //    const float outputAspect = (float)output.w / output.h;

            //    if (inputAspect > outputAspect)
            //    {
            //        targetHeight = int(targetWidth / inputAspect);
            //    }
            //    else if (inputAspect < outputAspect)
            //    {
            //        targetWidth = int(targetHeight * inputAspect);
            //    }

            //    // Make sure we are still in the bounds of the output
            //    assert(targetWidth <= output.w);
            //    assert(targetHeight <= output.h);

            //    // Center the image on the output surface
            //    byte* dst = (byte*)output.getBasePtr((output.w - targetWidth) / 2, (output.h - targetHeight) / 2);
            //    const uint dstLineIncrease = output.pitch - targetWidth * output.format.bytesPerPixel;

            //    const float scaleFactorX = (float)targetWidth / input.w;
            //    const float scaleFactorY = (float)targetHeight / input.h;

            //    for (int y = 0; y < targetHeight; ++y)
            //    {
            //        const float yFrac = (y / scaleFactorY);
            //        const int y1 = (int)yFrac;
            //        const int y2 = (y1 + 1 < input.h) ? (y1 + 1) : (input.h - 1);

            //        for (int x = 0; x < targetWidth; ++x)
            //        {
            //            const float xFrac = (x / scaleFactorX);
            //            const int x1 = (int)xFrac;
            //            const int x2 = (x1 + 1 < input.w) ? (x1 + 1) : (input.w - 1);

            //            // Look up colors at the points
            //            uint8 p1R, p1G, p1B;
            //            Graphics::colorToRGB < Graphics::ColorMasks < 565 > > (READ_UINT16(input.getBasePtr(x1, y1)), p1R, p1G, p1B);
            //    uint8 p2R, p2G, p2B;
            //    Graphics::colorToRGB < Graphics::ColorMasks < 565 > > (READ_UINT16(input.getBasePtr(x2, y1)), p2R, p2G, p2B);
            //    uint8 p3R, p3G, p3B;
            //    Graphics::colorToRGB < Graphics::ColorMasks < 565 > > (READ_UINT16(input.getBasePtr(x1, y2)), p3R, p3G, p3B);
            //    uint8 p4R, p4G, p4B;
            //    Graphics::colorToRGB < Graphics::ColorMasks < 565 > > (READ_UINT16(input.getBasePtr(x2, y2)), p4R, p4G, p4B);

            //    const float xDiff = xFrac - x1;
            //    const float yDiff = yFrac - y1;

            //    uint8 pR = (uint8)((1 - yDiff) * ((1 - xDiff) * p1R + xDiff * p2R) + yDiff * ((1 - xDiff) * p3R + xDiff * p4R));
            //    uint8 pG = (uint8)((1 - yDiff) * ((1 - xDiff) * p1G + xDiff * p2G) + yDiff * ((1 - xDiff) * p3G + xDiff * p4G));
            //    uint8 pB = (uint8)((1 - yDiff) * ((1 - xDiff) * p1B + xDiff * p2B) + yDiff * ((1 - xDiff) * p3B + xDiff * p4B));

            //    WRITE_UINT16(dst, Graphics::RGBToColor < Graphics::ColorMasks < 565 > > (pR, pG, pB));
            //    dst += 2;
            //}

            //// Move to the next line
            //dst = (byte*)dst + dstLineIncrease;
        }

        private void SaveGameToFile(byte slot)
        {
            ushort cnt;
            var fName = $"sword1.{slot:D3}";
            ushort[] liveBuf = new ushort[ObjectMan.TOTAL_SECTIONS];

            using (var stream = _saveFileMan.OpenForSaving(fName))
            using (var outf = new BinaryWriter(stream))
            {
                //if (!outf)
                //{
                //    // Display an error message and do nothing
                //    displayMessage(0, "Unable to create file '%s'. (%s)", fName, _saveFileMan.popErrorDesc().c_str());
                //    return;
                //}

                outf.WriteUInt32((uint)SAVEGAME_HEADER);

                outf.Write(StrToBytes(_saveNames[slot], 40));
                outf.WriteByte(SAVEGAME_VERSION);

                if (!IsPanelShown()) // Generate a thumbnail only if we are outside of game menu
                    SaveThumbnail(outf);

                // Date / time
                DateTime curTime = DateTime.Now;

                uint saveDate =
                    (uint)
                        ((curTime.Day & 0xFF) << 24 | ((curTime.Month + 1) & 0xFF) << 16 |
                         ((curTime.Year + 1900) & 0xFFFF));
                ushort saveTime = (ushort)((curTime.Hour & 0xFF) << 8 | ((curTime.Minute) & 0xFF));

                outf.WriteUInt32BigEndian(saveDate);
                outf.WriteUInt16BigEndian(saveTime);

                outf.WriteUInt32BigEndian((uint)(Engine.Instance.TotalPlayTime / 1000));

                _objMan.SaveLiveList(liveBuf);
                for (cnt = 0; cnt < ObjectMan.TOTAL_SECTIONS; cnt++)
                    outf.WriteUInt16(liveBuf[cnt]);

                var cpt = _objMan.FetchObject(Logic.PLAYER);
                Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_DIR] = (uint)cpt.dir;
                Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_X] = (uint)cpt.xcoord;
                Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_Y] = (uint)cpt.ycoord;
                Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_STANCE] = StaticRes.STAND;
                Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_PLACE] = (uint)cpt.place;

                for (cnt = 0; cnt < Logic.NUM_SCRIPT_VARS; cnt++)
                    outf.WriteUInt32(Logic.ScriptVars[cnt]);

                uint playerSize = (SwordObject.Size - 12000) / 4;
                var playerRaw = new UIntAccess(cpt.Data, cpt.Offset);
                for (var cnt2 = 0; cnt2 < playerSize; cnt2++)
                    outf.WriteUInt32(playerRaw[cnt2]);
            }
            // TODO: error
            //if (outf.err())
            //    displayMessage(0, "Couldn't write to file '%s'. Device full? (%s)", fName, _saveFileMan.popErrorDesc().c_str());
            //delete outf;
        }

        private void SaveNameSelect(ButtonIds id, bool saving)
        {
            DeselectSaveslots();
            _buttons[id - ButtonIds.BUTTON_SAVE_SELECT1].SetSelected(1);
            byte num = (byte)((id - ButtonIds.BUTTON_SAVE_SELECT1) + _saveScrollPos);
            if (saving && (_selectedSavegame != 255)) // the player may have entered something, clear it again
                _saveNames[_selectedSavegame] = _oldName;
            if (num < _saveFiles)
            {
                _selectedSavegame = num;
                _oldName = _saveNames[num]; // save for later
            }
            else
            {
                if (!saving)
                    _buttons[id - ButtonIds.BUTTON_SAVE_SELECT1].SetSelected(0); // no save in slot, deselect it
                else
                {
                    if (_saveFiles <= num)
                        _saveFiles = (byte)(num + 1);
                    _selectedSavegame = num;
                    _oldName = string.Empty;
                }
            }
            if (_selectedSavegame < 255)
                _cursorTick = 0;
            ShowSavegameNames();
        }

        private void DeselectSaveslots()
        {
            for (var cnt = 0; cnt < 8; cnt++)
                _buttons[cnt].SetSelected(0);
        }

        private void SaveNameScroll(ButtonIds scroll, bool saving)
        {
            ushort maxScroll;
            if (saving)
                maxScroll = 64;
            else
                maxScroll = _saveFiles; // for loading, we can only scroll as far as there are savegames
            if (scroll == ButtonIds.BUTTON_SCROLL_UP_FAST)
            {
                if (_saveScrollPos >= 8)
                    _saveScrollPos -= 8;
                else
                    _saveScrollPos = 0;
            }
            else if (scroll == ButtonIds.BUTTON_SCROLL_UP_SLOW)
            {
                if (_saveScrollPos >= 1)
                    _saveScrollPos--;
            }
            else if (scroll == ButtonIds.BUTTON_SCROLL_DOWN_SLOW)
            {
                if (_saveScrollPos + 8 < maxScroll)
                    _saveScrollPos++;
            }
            else if (scroll == ButtonIds.BUTTON_SCROLL_DOWN_FAST)
            {
                if (_saveScrollPos + 16 < maxScroll)
                    _saveScrollPos += 8;
                else
                {
                    if (maxScroll >= 8)
                        _saveScrollPos = (byte)(maxScroll - 8);
                    else
                        _saveScrollPos = 0;
                }
            }
            _selectedSavegame = 255; // deselect savegame
            DeselectSaveslots();
            ShowSavegameNames();
        }

        private bool GetConfirm(string title)
        {
            var panel = new ControlButton(0, 0, SwordRes.SR_CONFIRM, 0, 0, _resMan, _screenBuf, _system);
            panel.Draw();

            RenderText(title, 320, 160, TextModes.TEXT_CENTER);
            ControlButton[] buttons = new ControlButton[2];
            buttons[0] = new ControlButton(260, 192 + 40, SwordRes.SR_BUTTON, 0, 0, _resMan, _screenBuf, _system);
            RenderText(_lStrings[(int)LangStrings.STR_OK], 640 - 260, 192 + 40, TextModes.TEXT_RIGHT_ALIGN);
            buttons[1] = new ControlButton(260, 256 + 40, SwordRes.SR_BUTTON, 0, 0, _resMan, _screenBuf, _system);
            RenderText(_lStrings[(int)LangStrings.STR_CANCEL], 640 - 260, 256 + 40, TextModes.TEXT_RIGHT_ALIGN);
            byte retVal = 0;
            byte clickVal = 0;
            do
            {
                buttons[0].Draw();
                buttons[1].Draw();
                Delay(1000 / 12);
                if (_keyPressed.IsKeyDown(KeyCode.Escape))
                    retVal = 2;
                else if (_keyPressed.IsKeyDown(KeyCode.Return) /*|| _keyPressed.IsKeyDown(KeyCode.Enter)*/)
                    retVal = 1;
                if ((_mouseState & Mouse.BS1L_BUTTON_DOWN) != 0)
                {
                    if (buttons[0].WasClicked((ushort)_mouseCoord.X, (ushort)_mouseCoord.Y))
                        clickVal = 1;
                    else if (buttons[1].WasClicked((ushort)_mouseCoord.X, (ushort)_mouseCoord.Y))
                        clickVal = 2;
                    else
                        clickVal = 0;
                    if (clickVal != 0)
                        buttons[clickVal - 1].SetSelected(1);
                }
                if ((_mouseState & Mouse.BS1L_BUTTON_UP) != 0 && (clickVal != 0))
                {
                    if (buttons[clickVal - 1].WasClicked((ushort)_mouseCoord.X, (ushort)_mouseCoord.Y))
                        retVal = clickVal;
                    else
                        buttons[clickVal - 1].SetSelected(0);
                    clickVal = 0;
                }
            } while (retVal == 0);
            return retVal == 1;
        }

        private void RenderText(string str, int x, int y, TextModes mode)
        {
            int i = 0;
            var font = _font;
            if (mode.HasFlag(TextModes.TEXT_RED_FONT))
            {
                mode = (mode & ~TextModes.TEXT_RED_FONT);
                font = _redFont;
            }

            if (mode == TextModes.TEXT_RIGHT_ALIGN) // negative x coordinate means right-aligned.
                x -= GetTextWidth(str);
            else if (mode == TextModes.TEXT_CENTER)
                x -= GetTextWidth(str) / 2;

            ushort destX = (ushort)x;
            while (i < str.Length)
            {
                var dst = new ByteAccess(_screenBuf, +y * Screen.SCREEN_WIDTH + destX);

                FrameHeader chSpr = new FrameHeader(_resMan.FetchFrame(font, (uint)(str[i] - 32)));
                var sprData = new ByteAccess(chSpr.Data.Data, chSpr.Data.Offset + FrameHeader.Size);

                if (SystemVars.Platform == Platform.PSX)
                {
                    //Text fonts are compressed in psx version
                    var HIFbuf = new byte[_resMan.ReadUInt16(chSpr.height) * _resMan.ReadUInt16(chSpr.width)];
                    Screen.DecompressHIF(sprData.Data, sprData.Offset, HIFbuf);
                    sprData = new ByteAccess(HIFbuf);
                }

                for (ushort cnty = 0; cnty < _resMan.ReadUInt16(chSpr.height); cnty++)
                {
                    for (ushort cntx = 0; cntx < _resMan.ReadUInt16(chSpr.width); cntx++)
                    {
                        if (sprData[cntx] != 0)
                            dst[cntx] = sprData[cntx];
                    }

                    if (SystemVars.Platform == Platform.PSX)
                    { //On PSX version we need to double horizontal lines
                        dst.Offset += Screen.SCREEN_WIDTH;
                        for (ushort cntx = 0; cntx < _resMan.ReadUInt16(chSpr.width); cntx++)
                            if (sprData[cntx] != 0)
                                dst[cntx] = sprData[cntx];
                    }

                    sprData.Offset += _resMan.ReadUInt16(chSpr.width);
                    dst.Offset += Screen.SCREEN_WIDTH;
                }
                destX = (ushort)(destX + _resMan.ReadUInt16(chSpr.width) - 3);
                i++;
            }

            _system.GraphicsManager.CopyRectToScreen(new BytePtr(_screenBuf, y * Screen.SCREEN_WIDTH + x), Screen.SCREEN_WIDTH, x, y, (destX - x) + 3, 28);
        }

        private ushort GetTextWidth(string str)
        {
            var i = 0;
            ushort width = 0;
            while (i < str.Length)
            {
                width = (ushort)(width + _resMan.ReadUInt16(new FrameHeader(_resMan.FetchFrame(_font, (uint)(str[i] - 32))).width) - 3);
                i++;
            }
            return width;
        }

        private void Delay(int msecs)
        {
            var now = Environment.TickCount;
            var endTime = now + msecs;
            _keyPressed = new ScummInputState();
            _mouseState = 0;

            do
            {
                _keyPressed = _system.InputManager.GetState();
                if (_keyPressed.GetKeys().Count > 0)
                {
                    // we skip the rest of the delay and return immediately
                    // to handle keyboard input
                    return;
                }

                _mouseCoord = _system.InputManager.GetMousePosition();
                _mouseDown = _keyPressed.IsLeftButtonDown;
                _mouseState |= (ushort)(_mouseDown ? Mouse.BS1L_BUTTON_DOWN : Mouse.BS1L_BUTTON_UP);

                _system.GraphicsManager.UpdateScreen();
                ServiceLocator.Platform.Sleep(10);
            } while (Environment.TickCount < endTime);
        }

        private void SetupVolumePanel()
        {
            ControlButton panel = new ControlButton(0, 0, SwordRes.SR_VOLUME, 0, 0, _resMan, _screenBuf, _system);
            panel.Draw();

            RenderText(_lStrings[(int)LangStrings.STR_MUSIC], 149, 39 + 40, TextModes.TEXT_LEFT_ALIGN);
            RenderText(_lStrings[(int)LangStrings.STR_SPEECH], 320, 39 + 40, TextModes.TEXT_CENTER);
            RenderText(_lStrings[(int)LangStrings.STR_FX], 438, 39 + 40, TextModes.TEXT_LEFT_ALIGN);

            CreateButtons(_volumeButtons, 4);
            RenderText(_lStrings[(int)LangStrings.STR_DONE], _volumeButtons[0].x - 10, _volumeButtons[0].y, TextModes.TEXT_RIGHT_ALIGN);

            byte volL, volR;
            _music.GiveVolume(out volL, out volR);
            RenderVolumeBar(1, volL, volR);
            _sound.GiveSpeechVol(out volL, out volR);
            RenderVolumeBar(2, volL, volR);
            _sound.GiveSfxVol(out volL, out volR);
            RenderVolumeBar(3, volL, volR);
        }

        private void CreateButtons(ButtonInfo[] buttons, byte num)
        {
            for (var cnt = 0; cnt < num; cnt++)
            {
                _buttons[cnt] = new ControlButton(buttons[cnt].x, buttons[cnt].y, buttons[cnt].resId, (ButtonIds)buttons[cnt].id, buttons[cnt].flag, _resMan, _screenBuf, _system);
                _buttons[cnt].Draw();
            }
            _numButtons = num;
        }

        private void ShowSavegameNames()
        {
            for (var cnt = 0; cnt < 8; cnt++)
            {
                _buttons[cnt].Draw();
                TextModes textMode = TextModes.TEXT_LEFT_ALIGN;
                ushort ycoord = (ushort)(_saveButtons[cnt].y + 2);
                var str = $"{cnt + _saveScrollPos + 1}. {_saveNames[cnt + _saveScrollPos]}";
                if (cnt + _saveScrollPos == _selectedSavegame)
                {
                    textMode |= TextModes.TEXT_RED_FONT;
                    ycoord += 2;
                    if (_cursorVisible)
                        str += "_";
                }
                RenderText(str, _saveButtons[cnt].x + 6, ycoord, textMode);
            }
        }

        private void HandleSaveKey(ScummInputState kbd)
        {
            if (_selectedSavegame < 255)
            {
                var saveName = new StringBuilder(_saveNames[_selectedSavegame]);
                byte len = (byte)_saveNames[_selectedSavegame].Length;
                if (kbd.IsKeyDown(KeyCode.Backspace) && len != 0)  // backspace
                    saveName.Remove(saveName.Length - 1, 1);
                else
                {
                    var key = kbd.GetKeys().Select(k => (char?)k).LastOrDefault();
                    if (key.HasValue && keyAccepted(key.Value) && (len < 31))
                    {
                        saveName.Append(key.Value);
                    }
                }
                _saveNames[_selectedSavegame] = saveName.ToString();
                ShowSavegameNames();
            }
        }

        static readonly char[] allowedSpecials = { ',', '.', ':', '-', '(', ')', '?', '!', '\"', '\'' };

        bool keyAccepted(char ascii)
        {
            if (((ascii >= 'A') && (ascii <= 'Z')) ||
                    ((ascii >= 'a') && (ascii <= 'z')) ||
                    ((ascii >= '0') && (ascii <= '9')) ||
                    allowedSpecials.Contains(ascii))
                return true;
            return false;
        }

        private void SetupSaveRestorePanel(bool saving)
        {
            ReadSavegameDescriptions();

            FrameHeader savePanel = new FrameHeader(_resMan.FetchFrame(_resMan.OpenFetchRes(SwordRes.SR_WINDOW), 0));
            short panelX = (short)((640 - _resMan.ReadUInt16(savePanel.width)) / 2);
            short panelY = (short)((480 - _resMan.ReadUInt16(savePanel.height)) / 2);
            ControlButton panel = new ControlButton((ushort)panelX, (ushort)panelY, SwordRes.SR_WINDOW, 0, 0, _resMan, _screenBuf, _system);
            panel.Draw();
            _resMan.ResClose(SwordRes.SR_WINDOW);
            CreateButtons(_saveButtons, 14);
            RenderText(_lStrings[(int)LangStrings.STR_CANCEL], _saveButtons[13].x - 10, _saveButtons[13].y, TextModes.TEXT_RIGHT_ALIGN);
            if (saving)
            {
                RenderText(_lStrings[(int)LangStrings.STR_SAVE], _saveButtons[12].x + 30, _saveButtons[13].y, TextModes.TEXT_LEFT_ALIGN);
            }
            else
            {
                RenderText(_lStrings[(int)LangStrings.STR_RESTORE], _saveButtons[12].x + 30, _saveButtons[13].y, TextModes.TEXT_LEFT_ALIGN);
            }
            ReadSavegameDescriptions();
            _selectedSavegame = 255;
            ShowSavegameNames();
        }

        public void ReadSavegameDescriptions()
        {
            var pattern = "sword1.???";
            var filenames = _saveFileMan.ListSavefiles(pattern);
            Array.Sort(filenames);// Sort (hopefully ensuring we are sorted numerically..)

            _saveNames.Clear();

            int num = 0;
            int slotNum = 0;
            foreach (var file in filenames)
            {
                // Obtain the last 3 digits of the filename, since they correspond to the save slot
                slotNum = int.Parse(file.Substring(file.Length - 3));

                while (num < slotNum)
                {
                    _saveNames.Add(string.Empty);
                    num++;
                }

                if (slotNum >= 0 && slotNum <= 999)
                {
                    num++;
                    using (var input = _saveFileMan.OpenForLoading(file))
                    {
                        if (input != null)
                        {
                            var br = new BinaryReader(input);
                            br.ReadUInt32(); // header
                            var saveName = br.ReadChars(40).TakeWhile(c => c != 0).ToArray();
                            _saveNames.Add(new string(saveName));
                        }
                    }
                }
            }

            for (int i = _saveNames.Count; i < 1000; i++)
                _saveNames.Add(string.Empty);

            _saveScrollPos = 0;
            _selectedSavegame = 255;
            _saveFiles = _numSaves = (byte)_saveNames.Count;
        }

        private void SetupMainPanel()
        {
            uint panelId;

            if (SystemVars.ControlPanelMode == ControlPanelMode.CP_DEATHSCREEN)
                panelId = SwordRes.SR_DEATHPANEL;
            else
            {
                if (SystemVars.RealLanguage == Core.Language.EN_USA)
                    panelId = SwordRes.SR_PANEL_AMERICAN;
                else if (SystemVars.Language <= Language.BS1_SPANISH)
                    panelId = (uint)(SwordRes.SR_PANEL_ENGLISH + SystemVars.Language);
                else
                    panelId = SwordRes.SR_PANEL_ENGLISH;
            }

            ControlButton panel = new ControlButton(0, 0, panelId, 0, 0, _resMan, _screenBuf, _system);
            panel.Draw();

            if (SystemVars.ControlPanelMode != ControlPanelMode.CP_NORMAL)
                CreateButtons(_deathButtons, 3);
            else
            {
                CreateButtons(_panelButtons, 7);
                _buttons[5].SetSelected(SystemVars.ShowText);
            }

            if (SystemVars.ControlPanelMode == ControlPanelMode.CP_THEEND) // end of game
                RenderText(_lStrings[(int)LangStrings.STR_THE_END], 480, 188 + 40, TextModes.TEXT_RIGHT_ALIGN);

            if (SystemVars.ControlPanelMode == ControlPanelMode.CP_NORMAL)
            { // normal panel
                RenderText(_lStrings[(int)LangStrings.STR_SAVE], 180, 188 + 40, TextModes.TEXT_LEFT_ALIGN);
                RenderText(_lStrings[(int)LangStrings.STR_DONE], 460, 332 + 40, TextModes.TEXT_RIGHT_ALIGN);
                RenderText(_lStrings[(int)LangStrings.STR_RESTORE], 180, 224 + 40, TextModes.TEXT_LEFT_ALIGN);
                RenderText(_lStrings[(int)LangStrings.STR_RESTART], 180, 260 + 40, TextModes.TEXT_LEFT_ALIGN);
                RenderText(_lStrings[(int)LangStrings.STR_QUIT], 180, 296 + 40, TextModes.TEXT_LEFT_ALIGN);

                RenderText(_lStrings[(int)LangStrings.STR_VOLUME], 460, 188 + 40, TextModes.TEXT_RIGHT_ALIGN);
                RenderText(_lStrings[(int)LangStrings.STR_TEXT], 460, 224 + 40, TextModes.TEXT_RIGHT_ALIGN);
            }
            else
            {
                RenderText(_lStrings[(int)LangStrings.STR_RESTORE], 285, 224 + 40, TextModes.TEXT_LEFT_ALIGN);
                if (SystemVars.ControlPanelMode == ControlPanelMode.CP_NEWGAME) // just started game
                    RenderText(_lStrings[(int)LangStrings.STR_START], 285, 260 + 40, TextModes.TEXT_LEFT_ALIGN);
                else
                    RenderText(_lStrings[(int)LangStrings.STR_RESTART], 285, 260 + 40, TextModes.TEXT_LEFT_ALIGN);
                RenderText(_lStrings[(int)LangStrings.STR_QUIT], 285, 296 + 40, TextModes.TEXT_LEFT_ALIGN);
            }
        }

        private void DestroyButtons()
        {
            for (var cnt = 0; cnt < _numButtons; cnt++)
                _buttons[cnt] = null;
            _numButtons = 0;
        }

        static readonly string[][] _languageStrings = {
           new [] {
	        // BS1_ENGLISH:
	        "PAUSED",
            "PLEASE INSERT CD-",
            "THEN PRESS A KEY",
            "INCORRECT CD",
            "Save",
            "Restore",
            "Restart",
            "Start",
            "Quit",
            "Speed",
            "Volume",
            "Text",
            "Done",
            "OK",
            "Cancel",
            "Music",
            "Speech",
            "Fx",
            "The End",
            "DRIVE FULL!",
           },
           new [] {
        // BS1_FRENCH:
	        "PAUSE",
            "INS\xC9REZ LE CD-",
            "ET APPUYES SUR UNE TOUCHE",
            "CD INCORRECT",
            "Sauvegarder",
            "Recharger",
            "Recommencer",
            "Commencer",
            "Quitter",
            "Vitesse",
            "Volume",
            "Texte",
            "Termin\xE9",
            "OK",
            "Annuler",
            "Musique",
            "Voix",
            "Fx",
            "Fin",
            "DISQUE PLEIN!",
            },
           new [] {
        //BS1_GERMAN:
	        "PAUSE",
            "BITTE LEGEN SIE CD-",
            "EIN UND DR\xDCCKEN SIE EINE BELIEBIGE TASTE",
            "FALSCHE CD",
            "Speichern",
            "Laden",
            "Neues Spiel",
            "Start",
            "Beenden",
            "Geschwindigkeit",
            "Lautst\xE4rke",
            "Text",
            "Fertig",
            "OK",
            "Abbrechen",
            "Musik",
            "Sprache",
            "Fx",
            "Ende",
            "DRIVE FULL!",
            },
           new [] {
        //BS1_ITALIAN:
	        "PAUSA",
            "INSERITE IL CD-",
            "E PREMETE UN TASTO",
            "CD ERRATO",
            "Salva",
            "Ripristina",
            "Ricomincia",
            "Inizio",
            "Esci",
            "Velocit\xE0",
            "Volume",
            "Testo",
            "Fatto",
            "OK",
            "Annulla",
            "Musica",
            "Parlato",
            "Fx",
            "Fine",
            "DISCO PIENO!",
            },
           new [] {
        //BS1_SPANISH:
	        "PAUSA",
            "POR FAVOR INTRODUCE EL CD-",
            "Y PULSA UNA TECLA",
            "CD INCORRECTO",
            "Guardar",
            "Recuperar",
            "Reiniciar",
            "Empezar",
            "Abandonar",
            "Velocidad",
            "Volumen",
            "Texto",
            "Hecho",
            "OK",
            "Cancelar",
            "M\xFAsica",
            "Di\xE1logo",
            "Fx",
            "Fin",
            "DISCO LLENO",
            },
           new [] {
        // BS1_CZECH:
	        "\xAC\x41S SE ZASTAVIL",
            "VLO\xA6TE DO MECHANIKY CD DISK",
            "PAK STISKN\xB7TE LIBOVOLNOU KL\xB5VESU",
            "TO NEBUDE TO SPR\xB5VN\x90 CD",
            "Ulo\xA7it pozici",
            "Nahr\xA0t pozici",
            "Za\x9F\xA1t znovu",
            "Start",
            "Ukon\x9Fit hru",
            "Rychlost",
            "Hlasitost",
            "Titulky",
            "Souhlas\xA1m",
            "Ano",
            "Ne",
            "Hudba",
            "Mluven, slovo",
            "Zvuky",
            "Konec",
            "Disk pln\xEC",
           },
           new [] {
        //BS1_PORTUGESE:
	        "PAUSA",
            "FAVOR INSERIR CD",
            "E DIGITAR UMA TECLA",
            "CD INCORRETO",
            "Salvar",
            "Restaurar",
            "Reiniciar",
            "Iniciar",
            "Sair",
            "Velocidade",
            "Volume",
            "Texto",
            "Feito",
            "OK",
            "Cancelar",
            "M\xFAsica",
            "Voz",
            "Efeitos",
            "Fim",
            "UNIDADE CHEIA!"
            }
        };

        struct ButtonInfo
        {
            public ushort x, y;
            public uint resId, id;
            public byte flag;
        }

        static readonly ButtonInfo[] _deathButtons = {
            new ButtonInfo {x=250, y=224 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_RESTORE_PANEL, flag=0 },
            new ButtonInfo {x=250, y=260 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_RESTART, flag=kButtonOk },
            new ButtonInfo {x=250, y=296 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_QUIT, flag=kButtonCancel }
        };

        static readonly ButtonInfo[] _panelButtons = {
            new ButtonInfo {x=145, y=188 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_SAVE_PANEL, flag=0 },
            new ButtonInfo {x=145, y=224 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_RESTORE_PANEL, flag=0 },
            new ButtonInfo {x=145, y=260 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_RESTART, flag=0 },
            new ButtonInfo {x=145, y=296 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_QUIT, flag=kButtonCancel },
            new ButtonInfo {x=475, y=188 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_VOLUME_PANEL, flag=0 },
            new ButtonInfo {x=475, y=224 + 40, resId = SwordRes.SR_TEXT_BUTTON, id=(uint) ButtonIds.BUTTON_TEXT, flag=0 },
            new ButtonInfo {x=475, y=332 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_DONE, flag=kButtonOk }
        };

        static readonly ButtonInfo[] _volumeButtons = {
            new ButtonInfo { x=478, y=338 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_MAIN_PANEL, flag=kButtonOk},
            new ButtonInfo { x=138, y=135, resId = SwordRes.SR_VKNOB, id=0, flag=0},
            new ButtonInfo { x=273, y=135, resId = SwordRes.SR_VKNOB, id=0, flag=0},
            new ButtonInfo { x=404, y=135, resId = SwordRes.SR_VKNOB, id=0, flag=0},
        };

        static readonly ButtonInfo[] _saveButtons = {
            new ButtonInfo {x=114, y= 32 + 40, resId = SwordRes.SR_SLAB1, id=(uint) ButtonIds.BUTTON_SAVE_SELECT1, flag=0 },
            new ButtonInfo {x=114, y= 68 + 40, resId = SwordRes.SR_SLAB2, id=(uint) ButtonIds.BUTTON_SAVE_SELECT2, flag=0 },
            new ButtonInfo {x=114, y=104 + 40, resId = SwordRes.SR_SLAB3, id=(uint) ButtonIds.BUTTON_SAVE_SELECT3, flag=0 },
            new ButtonInfo {x=114, y=140 + 40, resId = SwordRes.SR_SLAB4, id=(uint) ButtonIds.BUTTON_SAVE_SELECT4, flag=0 },
            new ButtonInfo {x=114, y=176 + 40, resId = SwordRes.SR_SLAB1, id=(uint) ButtonIds.BUTTON_SAVE_SELECT5, flag=0 },
            new ButtonInfo {x=114, y=212 + 40, resId = SwordRes.SR_SLAB2, id=(uint) ButtonIds.BUTTON_SAVE_SELECT6, flag=0 },
            new ButtonInfo {x=114, y=248 + 40, resId = SwordRes.SR_SLAB3, id=(uint) ButtonIds.BUTTON_SAVE_SELECT7, flag=0 },
            new ButtonInfo {x=114, y=284 + 40, resId = SwordRes.SR_SLAB4, id=(uint) ButtonIds.BUTTON_SAVE_SELECT8, flag=0 },

            new ButtonInfo {x=516,y=  25 + 40, resId = SwordRes.SR_BUTUF, id=(uint) ButtonIds.BUTTON_SCROLL_UP_FAST, flag=0 },
            new ButtonInfo {x=516,y=  45 + 40, resId = SwordRes.SR_BUTUS, id=(uint) ButtonIds.BUTTON_SCROLL_UP_SLOW, flag=0 },
            new ButtonInfo {x=516,y= 289 + 40, resId = SwordRes.SR_BUTDS, id=(uint) ButtonIds.BUTTON_SCROLL_DOWN_SLOW, flag=0 },
            new ButtonInfo {x=516,y= 310 + 40, resId = SwordRes.SR_BUTDF, id=(uint) ButtonIds.BUTTON_SCROLL_DOWN_FAST, flag=0 },

            new ButtonInfo {x=125, y=338 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_SAVE_RESTORE_OKAY, flag=kButtonOk},
            new ButtonInfo {x=462, y=338 + 40, resId = SwordRes.SR_BUTTON, id=(uint) ButtonIds.BUTTON_SAVE_CANCEL, flag=kButtonCancel }
        };

        private byte[] _font;
        private byte[] _redFont;
        private Surface _screen;

        public void DoRestore()
        {
            var bufPos = new ByteAccess(_restoreBuf);
            _objMan.LoadLiveList(new UShortAccess(_restoreBuf));
            bufPos.Offset += ObjectMan.TOTAL_SECTIONS * 2;
            for (var cnt = 0; cnt < Logic.NUM_SCRIPT_VARS; cnt++)
            {
                Logic.ScriptVars[cnt] = bufPos.Data.ToUInt32(bufPos.Offset);
                bufPos.Offset += 4;
            }
            uint playerSize = (SwordObject.Size - 12000) / 4;
            var cpt = _objMan.FetchObject(Logic.PLAYER);
            var playerRaw = new UIntAccess(cpt.Data, cpt.Offset);

            for (var cnt2 = 0; cnt2 < playerSize; cnt2++)
            {
                playerRaw[0] = bufPos.Data.ToUInt32(bufPos.Offset);
                playerRaw.Offset += 4;
                bufPos.Offset += 4;
            }

            Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_DIR] = (uint)cpt.dir;
            Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_X] = (uint)cpt.xcoord;
            Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_Y] = (uint)cpt.ycoord;
            Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_STANCE] = StaticRes.STAND;
            Logic.ScriptVars[(int)ScriptVariableNames.CHANGE_PLACE] = (uint)cpt.place;
            SystemVars.JustRestoredGame = 1;
            if (SystemVars.IsDemo)
                Logic.ScriptVars[(int)ScriptVariableNames.PLAYINGDEMO] = 1;
        }

        public bool SavegamesExist()
        {
            var pattern = "sword1.???";
            var saveNames = _saveFileMan.ListSavefiles(pattern);
            return saveNames.Length > 0;
        }
    }
}
