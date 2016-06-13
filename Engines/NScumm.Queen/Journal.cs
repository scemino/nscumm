//
//  QueenEngine.cs
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
using System.Diagnostics;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    struct TextField
    {
        public bool enabled;
        public int posCursor;
        public uint textCharsCount;
        public string text;
        public int x, y;
        public int w, h;

        public void Reset()
        {
            enabled = false;
            posCursor = 0;
            textCharsCount = 0;
            text = null;
            x = 0;
            y = 0;
            w = 0;
            w = 0;
        }
    }

    struct Zone
    {
        public int num;
        public short x1, y1, x2, y2;

        public Zone(int num, short x1, short y1, short x2, short y2)
        {
            this.num = num;
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }
    }

    enum PanelMode
    {
        NORMAL,
        INFO_BOX,
        YES_NO
    }

    enum QuitMode
    {
        LOOP,
        RESTORE,
        CONTINUE
    }

    public class Journal
    {
        const int NUM_SAVES_PER_PAGE = 10;
        const int MAX_PANEL_TEXTS = 8;
        const int MAX_ZONES = 31;

        const int ZN_REVIEW_ENTRY = 1;
        const int ZN_MAKE_ENTRY = 2;
        const int ZN_YES = ZN_MAKE_ENTRY;
        const int ZN_CLOSE = 3;
        const int ZN_NO = ZN_CLOSE;
        const int ZN_GIVEUP = 4;
        const int ZN_TEXT_SPEED = 5;
        const int ZN_SFX_TOGGLE = 6;
        const int ZN_MUSIC_VOLUME = 7;
        const int ZN_DESC_1 = 8;
        const int ZN_DESC_2 = 9;
        const int ZN_DESC_3 = 10;
        const int ZN_DESC_4 = 11;
        const int ZN_DESC_5 = 12;
        const int ZN_DESC_6 = 13;
        const int ZN_DESC_7 = 14;
        const int ZN_DESC_8 = 15;
        const int ZN_DESC_9 = 16;
        const int ZN_DESC_10 = 17;
        const int ZN_PAGE_A = 18;
        const int ZN_PAGE_B = 19;
        const int ZN_PAGE_C = 20;
        const int ZN_PAGE_D = 21;
        const int ZN_PAGE_E = 22;
        const int ZN_PAGE_F = 23;
        const int ZN_PAGE_G = 24;
        const int ZN_PAGE_H = 25;
        const int ZN_PAGE_I = 26;
        const int ZN_PAGE_J = 27;
        const int ZN_INFO_BOX = 28;
        const int ZN_MUSIC_TOGGLE = 29;
        const int ZN_VOICE_TOGGLE = 30;
        const int ZN_TEXT_TOGGLE = 31;

        const int FRAME_BLUE_1 = 1;
        const int FRAME_BLUE_2 = 2;
        const int FRAME_ORANGE = 3;
        const int FRAME_GREY = 5;
        const int FRAME_CHECK_BOX = 16;
        const int FRAME_BLUE_PIN = 18;
        const int FRAME_GREEN_PIN = 19;
        const int FRAME_INFO_BOX = 20;

        const int TXT_CLOSE = 30;
        const int TXT_GIVE_UP = 31;
        const int TXT_MAKE_ENTRY = 32;
        const int TXT_REVIEW_ENTRY = 33;
        const int TXT_YES = 34;
        const int TXT_NO = 35;

        const int JOURNAL_BANK = 8;
        const int JOURNAL_FRAMES = 40;

        const int BOB_LEFT_RECT_1 = 1;
        const int BOB_LEFT_RECT_2 = 2;
        const int BOB_LEFT_RECT_3 = 3;
        const int BOB_LEFT_RECT_4 = 4;
        const int BOB_TALK_SPEED = 5;
        const int BOB_SFX_TOGGLE = 6;
        const int BOB_MUSIC_VOLUME = 7;
        const int BOB_SAVE_DESC = 8;
        const int BOB_SAVE_PAGE = 9;
        const int BOB_SPEECH_TOGGLE = 10;
        const int BOB_TEXT_TOGGLE = 11;
        const int BOB_MUSIC_TOGGLE = 12;
        const int BOB_INFO_BOX = 13;

        static readonly int[] _frames = { FRAME_BLUE_1, FRAME_BLUE_2, FRAME_BLUE_1, FRAME_ORANGE };
        static readonly int[] _titles = { TXT_REVIEW_ENTRY, TXT_MAKE_ENTRY, TXT_CLOSE, TXT_GIVE_UP };
        static readonly int[] DrawYesNoPanelFrames = { FRAME_GREY, FRAME_BLUE_1, FRAME_BLUE_2 };

        static readonly Zone[] _zones = {
            new Zone(ZN_REVIEW_ENTRY,  32,   8,  96,  40),
            new Zone(ZN_MAKE_ENTRY,    32,  56,  96,  88 ), // == ZN_YES
            new Zone(ZN_CLOSE,         32, 104,  96, 136 ), // == ZN_NO
            new Zone(ZN_GIVEUP,        32, 152,  96, 184 ),
            new Zone(ZN_TEXT_SPEED,   136, 169, 265, 176 ),
            new Zone(ZN_SFX_TOGGLE,   197, 155, 231, 164 ),
            new Zone(ZN_MUSIC_VOLUME, 136, 182, 265, 189 ),
            new Zone(ZN_DESC_1,       131,   7, 290,  18 ),
            new Zone(ZN_DESC_2,       131,  20, 290,  31 ),
            new Zone(ZN_DESC_3,       131,  33, 290,  44 ),
            new Zone(ZN_DESC_4,       131,  46, 290,  57 ),
            new Zone(ZN_DESC_5,       131,  59, 290,  70 ),
            new Zone(ZN_DESC_6,       131,  72, 290,  83 ),
            new Zone(ZN_DESC_7,       131,  85, 290,  96 ),
            new Zone(ZN_DESC_8,       131,  98, 290, 109 ),
            new Zone(ZN_DESC_9,       131, 111, 290, 122 ),
            new Zone(ZN_DESC_10,      131, 124, 290, 135 ),
            new Zone(ZN_PAGE_A,       300,   4, 319,  17 ),
            new Zone(ZN_PAGE_B,       300,  19, 319,  32 ),
            new Zone(ZN_PAGE_C,       300,  34, 319,  47 ),
            new Zone(ZN_PAGE_D,       300,  49, 319,  62 ),
            new Zone(ZN_PAGE_E,       300,  64, 319,  77 ),
            new Zone(ZN_PAGE_F,       300,  79, 319,  92 ),
            new Zone(ZN_PAGE_G,       300,  94, 319, 107 ),
            new Zone(ZN_PAGE_H,       300, 109, 319, 122 ),
            new Zone(ZN_PAGE_I,       300, 124, 319, 137 ),
            new Zone(ZN_PAGE_J,       300, 139, 319, 152 ),
            new Zone(ZN_INFO_BOX,     273, 146, 295, 189 ),
            new Zone(ZN_MUSIC_TOGGLE, 109, 181, 135, 190 ),
            new Zone(ZN_VOICE_TOGGLE, 134, 155, 168, 164 ),
            new Zone(ZN_TEXT_TOGGLE,  109, 168, 135, 177 )
        };

        QueenEngine _vm;
        PanelMode _panelMode;
        QuitMode _quitMode;

        int _currentSavePage;
        int _currentSaveSlot;

        int _prevJoeX, _prevJoeY;

        int _panelTextCount;
        int[] _panelTextY = new int[MAX_PANEL_TEXTS];
        TextField _textField;
        ushort _prevZoneNum;
        string[] _saveDescriptions = new string[100];

        ISystem _system;

        public Journal(QueenEngine vm)
        {
            _vm = vm;
        }

        public void Use()
        {
            BobSlot joe = _vm.Graphics.Bobs[0];
            _prevJoeX = joe.x;
            _prevJoeY = joe.y;

            _panelMode = PanelMode.NORMAL;
            _system = _vm.System;

            _panelTextCount = 0;
            Array.Clear(_panelTextY, 0, _panelTextY.Length);
            _textField.Reset();

            Array.Clear(_saveDescriptions, 0, _saveDescriptions.Length);
            _vm.FindGameStateDescriptions(_saveDescriptions);

            Setup();
            Redraw();
            Update();
            _vm.Display.PalFadeIn(Defines.ROOM_JOURNAL);

            _quitMode = QuitMode.LOOP;
            while (_quitMode == QuitMode.LOOP)
            {
                var state = _system.InputManager.GetState();
                var keys = state.GetKeys();
                var mousePos = _system.InputManager.GetMousePosition();
                if (keys.Count > 0)
                {
                    HandleKeyDown(keys.First());
                }
                if (state.IsLeftButtonDown)
                {
                    HandleMouseDown(mousePos.X, mousePos.Y);
                }
                _system.InputManager.ResetKeys();
                ServiceLocator.Platform.Sleep(20);
                _system.GraphicsManager.UpdateScreen();
            }

            _vm.WriteOptionSettings();

            _vm.Display.ClearTexts(0, Defines.GAME_SCREEN_HEIGHT - 1);
            _vm.Graphics.PutCameraOnBob(0);
            if (_quitMode == QuitMode.CONTINUE)
            {
                ContinueGame();
            }
        }

        private void HandleMouseDown(int x, int y)
        {
            int val;
            short zoneNum = (short)_vm.Grid.FindZoneForPos(GridScreen.ROOM, (ushort)x, (ushort)y);
            switch (_panelMode)
            {
                case PanelMode.INFO_BOX:
                    ExitInfoPanelMode();
                    break;
                case PanelMode.YES_NO:
                    if (zoneNum == ZN_YES)
                    {
                        _panelMode = PanelMode.NORMAL;
                        int currentSlot = _currentSavePage * 10 + _currentSaveSlot;
                        switch (_prevZoneNum)
                        {
                            case ZN_REVIEW_ENTRY:
                                if (_saveDescriptions[currentSlot].Length > 0)
                                {
                                    _vm.Graphics.ClearBobs();
                                    _vm.Display.PalFadeOut(Defines.ROOM_JOURNAL);
                                    _vm.Sound.StopSong();
                                    _vm.LoadGameState(currentSlot);
                                    _vm.Display.ClearTexts(0, Defines.GAME_SCREEN_HEIGHT - 1);
                                    _quitMode = QuitMode.RESTORE;
                                }
                                else
                                {
                                    ExitYesNoPanelMode();
                                }
                                break;
                            case ZN_MAKE_ENTRY:
                                if (_textField.text.Length > 0)
                                {
                                    CloseTextField();
                                    _vm.SaveGameState(currentSlot, _textField.text);
                                    _quitMode = QuitMode.CONTINUE;
                                }
                                else
                                {
                                    ExitYesNoPanelMode();
                                }
                                break;
                            case ZN_GIVEUP:
                                _quitMode = QuitMode.CONTINUE;
                                _vm.QuitGame();
                                break;
                        }
                    }
                    else if (zoneNum == ZN_NO)
                    {
                        ExitYesNoPanelMode();
                    }
                    break;
                case PanelMode.NORMAL:
                    switch (zoneNum)
                    {
                        case ZN_REVIEW_ENTRY:
                            EnterYesNoPanelMode(zoneNum, TXT_REVIEW_ENTRY);
                            break;
                        case ZN_MAKE_ENTRY:
                            InitTextField(_saveDescriptions[_currentSavePage * 10 + _currentSaveSlot]);
                            EnterYesNoPanelMode(zoneNum, TXT_MAKE_ENTRY);
                            break;
                        case ZN_CLOSE:
                            _quitMode = QuitMode.CONTINUE;
                            break;
                        case ZN_GIVEUP:
                            EnterYesNoPanelMode(zoneNum, TXT_GIVE_UP);
                            break;
                        case ZN_TEXT_SPEED:
                            val = (x - 136) * QueenEngine.MAX_TEXT_SPEED / (266 - 136);
                            _vm.TalkSpeed = val;
                            DrawConfigPanel();
                            break;
                        case ZN_SFX_TOGGLE:
                            _vm.Sound.ToggleSfx();
                            DrawConfigPanel();
                            break;
                        case ZN_MUSIC_VOLUME:
                            val = (x - 136) * Mixer.MaxMixerVolume / (266 - 136);
                            _vm.Sound.Volume = val;
                            DrawConfigPanel();
                            break;
                        case ZN_DESC_1:
                        case ZN_DESC_2:
                        case ZN_DESC_3:
                        case ZN_DESC_4:
                        case ZN_DESC_5:
                        case ZN_DESC_6:
                        case ZN_DESC_7:
                        case ZN_DESC_8:
                        case ZN_DESC_9:
                        case ZN_DESC_10:
                            _currentSaveSlot = zoneNum - ZN_DESC_1;
                            DrawSaveSlot();
                            break;
                        case ZN_PAGE_A:
                        case ZN_PAGE_B:
                        case ZN_PAGE_C:
                        case ZN_PAGE_D:
                        case ZN_PAGE_E:
                        case ZN_PAGE_F:
                        case ZN_PAGE_G:
                        case ZN_PAGE_H:
                        case ZN_PAGE_I:
                        case ZN_PAGE_J:
                            _currentSavePage = zoneNum - ZN_PAGE_A;
                            DrawSaveDescriptions();
                            break;
                        case ZN_INFO_BOX:
                            EnterInfoPanelMode();
                            break;
                        case ZN_MUSIC_TOGGLE:
                            _vm.Sound.ToggleMusic();
                            if (_vm.Sound.MusicOn)
                            {
                                _vm.Sound.PlayLastSong();
                            }
                            else
                            {
                                _vm.Sound.StopSong();
                            }
                            DrawConfigPanel();
                            break;
                        case ZN_VOICE_TOGGLE:
                            _vm.Sound.ToggleSpeech();
                            DrawConfigPanel();
                            break;
                        case ZN_TEXT_TOGGLE:
                            _vm.Subtitles = !_vm.Subtitles;
                            DrawConfigPanel();
                            break;
                    }
                    break;
            }
            Update();
        }

        private void EnterInfoPanelMode()
        {
            _panelMode = PanelMode.INFO_BOX;
            _vm.Display.ClearTexts(0, Defines.GAME_SCREEN_HEIGHT - 1);
            DrawInfoPanel();
        }

        private void DrawInfoPanel()
        {
            ShowBob(BOB_INFO_BOX, 72, 221, FRAME_INFO_BOX);
            string ver = _vm.Resource.JASVersion;
            switch (ver[0])
            {
                case 'P':
                    _vm.Display.SetTextCentered(132, "PC Hard Drive", false);
                    break;
                case 'C':
                    _vm.Display.SetTextCentered(132, "PC CD-ROM", false);
                    break;
                case 'a':
                    _vm.Display.SetTextCentered(132, "Amiga A500/600", false);
                    break;
            }
            switch (ver[1])
            {
                case 'E':
                    _vm.Display.SetTextCentered(144, "English", false);
                    break;
                case 'F':
                    _vm.Display.SetTextCentered(144, "Français", false);
                    break;
                case 'G':
                    _vm.Display.SetTextCentered(144, "Deutsch", false);
                    break;
                case 'H':
                    _vm.Display.SetTextCentered(144, "Hebrew", false);
                    break;
                case 'I':
                    _vm.Display.SetTextCentered(144, "Italiano", false);
                    break;
                case 'S':
                    _vm.Display.SetTextCentered(144, "Espa\xA4ol", false);
                    break;
            }
            string versionId = $"Version {ver[2]}.{ver[3]}{ver[4]}";
            _vm.Display.SetTextCentered(156, versionId, false);
        }

        private void InitTextField(string desc)
        {
            desc = desc ?? string.Empty;
            _system.InputManager.ShowVirtualKeyboard();
            _textField.enabled = true;
            _textField.posCursor = _vm.Display.TextWidth(desc);
            _textField.textCharsCount = (uint)desc.Length;
            _textField.text = desc;
        }

        private void EnterYesNoPanelMode(short prevZoneNum, int titleNum)
        {
            _panelMode = PanelMode.YES_NO;
            _prevZoneNum = (ushort)prevZoneNum;
            DrawYesNoPanel(titleNum);
        }

        private void DrawYesNoPanel(int titleNum)
        {
            int[] titles = { titleNum, TXT_YES, TXT_NO };
            DrawPanel(DrawYesNoPanelFrames, titles, 3);

            HideBob(BOB_LEFT_RECT_4);
            HideBob(BOB_TALK_SPEED);
            HideBob(BOB_SFX_TOGGLE);
            HideBob(BOB_MUSIC_VOLUME);
            HideBob(BOB_SPEECH_TOGGLE);
            HideBob(BOB_TEXT_TOGGLE);
            HideBob(BOB_MUSIC_TOGGLE);
        }

        private void ExitInfoPanelMode()
        {
            _vm.Display.ClearTexts(0, Defines.GAME_SCREEN_HEIGHT - 1);
            HideBob(BOB_INFO_BOX);
            Redraw();
            _panelMode = PanelMode.NORMAL;
        }

        private void HandleKeyDown(KeyCode keycode)
        {
            switch (_panelMode)
            {
                case PanelMode.INFO_BOX:
                    break;
                case PanelMode.YES_NO:
                    if (keycode == KeyCode.Escape)
                    {
                        ExitYesNoPanelMode();
                    }
                    else if (_textField.enabled)
                    {
                        UpdateTextField(keycode);
                    }
                    break;
                case PanelMode.NORMAL:
                    if (keycode == KeyCode.Escape)
                    {
                        _quitMode = QuitMode.CONTINUE;
                    }
                    break;
            }
        }

        private void UpdateTextField(KeyCode keycode)
        {
            bool dirty = false;
            switch (keycode)
            {
                case KeyCode.Backspace:
                    if (_textField.textCharsCount > 0)
                    {
                        --_textField.textCharsCount;
                        _textField.text = _textField.text.Substring(0, (int)_textField.textCharsCount);
                        dirty = true;
                    }
                    break;
                case KeyCode.Return:
                    //case KeyCode.Enter:
                    if (_textField.text.Length > 0)
                    {
                        CloseTextField();
                        int currentSlot = _currentSavePage * 10 + _currentSaveSlot;
                        _vm.SaveGameState(currentSlot, _textField.text);
                        _quitMode = QuitMode.CONTINUE;
                    }
                    break;
                default:
                    char ascii = Input.ToChar(keycode);
                    if (char.IsLetterOrDigit(ascii) &&
                        _vm.Display.TextWidth(_textField.text) < _textField.w)
                    {
                        _textField.text = _textField.text + ascii;
                        ++_textField.textCharsCount;
                        dirty = true;
                    }
                    break;
            }
            if (dirty)
            {
                _vm.Display.SetText((ushort)_textField.x, (ushort)(_textField.y + _currentSaveSlot * _textField.h), _textField.text, false);
                _textField.posCursor = _vm.Display.TextWidth(_textField.text);
                Update();
            }
        }

        private void ExitYesNoPanelMode()
        {
            _panelMode = PanelMode.NORMAL;
            if (_prevZoneNum == ZN_MAKE_ENTRY)
            {
                CloseTextField();
            }
            Redraw();
        }

        private void CloseTextField()
        {
            _system.InputManager.HideVirtualKeyboard();
            _textField.enabled = false;
        }

        private void ContinueGame()
        {
            _vm.Display.Fullscreen = false;
            _vm.Display.ForceFullRefresh();

            _vm.Logic.JoePos((ushort)_prevJoeX, (ushort)_prevJoeY);
            _vm.Logic.JoeCutFacing = _vm.Logic.JoeFacing;

            _vm.Logic.OldRoom = _vm.Logic.CurrentRoom;
            _vm.Logic.DisplayRoom(_vm.Logic.CurrentRoom, RoomDisplayMode.RDM_FADE_JOE, 0, 0, false);
        }

        private void Update()
        {
            _vm.Graphics.SortBobs();
            _vm.Display.PrepareUpdate();
            _vm.Graphics.DrawBobs();
            if (_textField.enabled)
            {
                short x = (short)(_textField.x + _textField.posCursor);
                short y = (short)(_textField.y + _currentSaveSlot * _textField.h + 8);
                _vm.Display.DrawBox(x, y, (short)(x + 6), y, _vm.Display.GetInkColor(InkColor.INK_JOURNAL));
            }
            _vm.Display.ForceFullRefresh();
            _vm.Display.Update();
            _system.GraphicsManager.UpdateScreen();
        }

        private void Redraw()
        {
            DrawNormalPanel();
            DrawConfigPanel();
            DrawSaveDescriptions();
            DrawSaveSlot();
        }

        private void DrawSaveSlot()
        {
            ShowBob(BOB_SAVE_DESC, 130, (short)(6 + _currentSaveSlot * 13), 17);
        }

        private void DrawSaveDescriptions()
        {
            for (int i = 0; i < NUM_SAVES_PER_PAGE; ++i)
            {
                int n = _currentSavePage * 10 + i;
                string nb = $"{n + 1}";
                int y = _textField.y + i * _textField.h;
                _vm.Display.SetText((ushort)_textField.x, (ushort)y, _saveDescriptions[n], false);
                _vm.Display.SetText((ushort)(_textField.x - 27), (ushort)(y + 1), nb, false);
            }
            // highlight current page
            ShowBob(BOB_SAVE_PAGE, 300, (short)(3 + _currentSavePage * 15), 6 + _currentSavePage);
        }

        private void DrawConfigPanel()
        {
            _vm.CheckOptionSettings();

            DrawSlideBar(_vm.TalkSpeed, QueenEngine.MAX_TEXT_SPEED, BOB_TALK_SPEED, 164, FRAME_BLUE_PIN);
            DrawSlideBar(_vm.Sound.Volume, Mixer.MaxMixerVolume, BOB_MUSIC_VOLUME, 177, FRAME_GREEN_PIN);

            DrawCheckBox(_vm.Sound.SfxOn, BOB_SFX_TOGGLE, 221, 155, FRAME_CHECK_BOX);
            DrawCheckBox(_vm.Sound.SpeechOn, BOB_SPEECH_TOGGLE, 158, 155, FRAME_CHECK_BOX);
            DrawCheckBox(_vm.Subtitles, BOB_TEXT_TOGGLE, 125, 167, FRAME_CHECK_BOX);
            DrawCheckBox(_vm.Sound.MusicOn, BOB_MUSIC_TOGGLE, 125, 181, FRAME_CHECK_BOX);
        }

        private void DrawCheckBox(bool active, int bobNum, short x, short y, int frameNum)
        {
            if (active)
            {
                ShowBob(bobNum, x, y, frameNum);
            }
            else
            {
                HideBob(bobNum);
            }
        }

        private void HideBob(int bobNum)
        {
            _vm.Graphics.Bobs[bobNum].active = false;
        }

        private void DrawSlideBar(int value, int maxValue, int bobNum, short y, int frameNum)
        {
            ShowBob(bobNum, (short)(136 + value * (266 - 136) / maxValue), y, frameNum);
        }

        private void DrawNormalPanel()
        {
            DrawPanel(_frames, _titles, 4);
        }

        private void DrawPanel(int[] frames, int[] titles, int n)
        {
            for (int i = 0; i < _panelTextCount; ++i)
            {
                _vm.Display.ClearTexts((ushort)_panelTextY[i], (ushort)_panelTextY[i]);
            }
            _panelTextCount = 0;
            int bobNum = 1;
            int y = 8;
            int j = 0;
            while ((n--) != 0)
            {
                ShowBob(bobNum++, 32, (short)y, frames[j]);
                DrawPanelText(y + 12, _vm.Logic.JoeResponse(titles[j]));
                j++;
                y += 48;
            }
        }

        private void ShowBob(int bobNum, short x, short y, int frameNum)
        {
            BobSlot bob = _vm.Graphics.Bobs[bobNum];
            bob.CurPos(x, y);
            bob.frameNum = (ushort)(JOURNAL_FRAMES + frameNum);
        }

        private void DrawPanelText(int y, string text, int offset = 0, int length = -1)
        {
            D.Debug(7, $"Journal::drawPanelText({y}, '{text}')");

            if (length == -1)
            {
                length = text.Length;
            }

            string s = text.Substring(offset, length).Trim(); // necessary for spanish version

            // draw the substrings
            int p = s.IndexOf(' ');
            if (p == -1)
            {
                int x = (128 - _vm.Display.TextWidth(s)) / 2;
                _vm.Display.SetText((ushort)x, (ushort)y, s, false);
                Debug.Assert(_panelTextCount < MAX_PANEL_TEXTS);
                _panelTextY[_panelTextCount++] = y;
            }
            else
            {
                //* p++ = '\0';
                if (_vm.Resource.Language == Language.HE_ISR)
                {
                    DrawPanelText(y - 5, s, p + 1, s.Length - p - 1);
                    DrawPanelText(y + 5, s, 0, p);
                }
                else
                {
                    DrawPanelText(y - 5, s, 0, p);
                    DrawPanelText(y + 5, s, p + 1, s.Length - p - 1);
                }
            }
        }

        private void Setup()
        {
            _vm.Display.PalFadeOut(_vm.Logic.CurrentRoom);
            _vm.Display.HorizontalScroll = 0;
            _vm.Display.Fullscreen = true;
            _vm.Graphics.ClearBobs();
            _vm.Display.ClearTexts(0, Defines.GAME_SCREEN_HEIGHT - 1);
            _vm.BankMan.EraseFrames(false);
            _vm.Display.TextCurrentColor(_vm.Display.GetInkColor(InkColor.INK_JOURNAL));

            _vm.Grid.Clear(GridScreen.ROOM);
            for (int i = 0; i < MAX_ZONES; ++i)
            {
                Zone zn = _zones[i];
                _vm.Grid.SetZone(GridScreen.ROOM, (short)zn.num, zn.x1, zn.y1, zn.x2, zn.y2);
            }

            _vm.Display.SetupNewRoom("journal", Defines.ROOM_JOURNAL);
            _vm.BankMan.Load("journal.BBK", JOURNAL_BANK);
            for (int f = 1; f <= 20; ++f)
            {
                int frameNum = JOURNAL_FRAMES + f;
                _vm.BankMan.Unpack((uint)f, (uint)frameNum, JOURNAL_BANK);
                BobFrame bf = _vm.BankMan.FetchFrame((uint)frameNum);
                bf.xhotspot = 0;
                bf.yhotspot = 0;
                if (f == FRAME_INFO_BOX)
                { // adjust info box hot spot to put it always on top
                    bf.yhotspot = 200;
                }
            }
            _vm.BankMan.Close(JOURNAL_BANK);

            _textField.x = 136;
            _textField.y = 9;
            _textField.w = 146;
            _textField.h = 13;
        }
    }
}

