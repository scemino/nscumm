//
//  AgosEngineFeebleDemo.cs
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

using NScumm.Core;
using NScumm.Core.Input;
using NScumm.Core.IO;

namespace NScumm.Agos
{
    internal class AgosEngineFeebleDemo : AgosEngineFeeble
    {
        private bool _filmMenuUsed;

        public AgosEngineFeebleDemo(ISystem system, GameSettings settings, AgosGameDescription gd) : base(system,
            settings, gd)
        {
        }

        protected override void InitMouse()
        {
            // TODO: Add larger cursor
            InitMouseSimon1();
        }

        protected override void DrawMousePointer()
        {
        }

        protected override void Go()
        {
            // Main menu
            DefineBox(1, 80, 75, 81, 117, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(2, 267, 21, 105, 97, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(3, 456, 89, 125, 103, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(4, 151, 225, 345, 41, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(5, 169, 319, 109, 113, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(6, 404, 308, 62, 117, (int) BoxFlags.kBFBoxDead, 0, null);

            // Film menu
            DefineBox(11, 28, 81, 123, 93, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(12, 182, 81, 123, 93, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(13, 335, 81, 123, 93, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(14, 488, 81, 123, 93, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(15, 28, 201, 123, 93, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(16, 182, 201, 123, 93, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(17, 335, 201, 123, 93, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(18, 488, 201, 123, 93, (int) BoxFlags.kBFBoxDead, 0, null);
            DefineBox(19, 255, 357, 135, 45, (int) BoxFlags.kBFBoxDead, 0, null);

            // Exit Menu
            DefineBox(21, 548, 421, 42, 21, (int) BoxFlags.kBFBoxDead, 0, null);

            // Text Window used by Feeble Files Data section
            if (_language == Language.DE_DEU)
            {
                _textWindow = OpenWindow(322, 457, 196, 15, 1, 0, 255);
            }
            else
            {
                _textWindow = OpenWindow(444, 452, 196, 15, 1, 0, 255);
            }

            PlayVideo("winasoft.smk");
            PlayVideo("fbigtalk.smk");

            while (!HasToQuit)
                MainMenu();
        }

        private void MainMenu()
        {
            for (int i = 1; i <= 6; i++)
                EnableBox(i);

            for (int i = 11; i <= 19; i++)
                DisableBox(i);

            PlayVideo("mmfadein.smk", true);

            StartInteractiveVideo("mainmenu.smk");

            HitArea ha = null;
            do
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                while (_lastHitArea3 == null)
                {
                    if (HasToQuit)
                        return;
                    HandleText();
                    Delay(1);
                }

                ha = _lastHitArea;
            } while (ha == null || !(ha.id >= 1 && ha.id <= 6));

            if (HasToQuit)
                return;

            StopInteractiveVideo();

            if (ha.id == 1)
            {
                // Feeble Files Data
                PlayVideo("ffade5.smk");
                PlayVideo("ftext0.smk");
                PlayVideo("ftext1.smk", true);
                WaitForSpace();
                PlayVideo("ftext2.smk", true);
                WaitForSpace();
                PlayVideo("ftext3.smk", true);
                WaitForSpace();
                PlayVideo("ftext4.smk", true);
                WaitForSpace();
                PlayVideo("ftext5.smk", true);
                WaitForSpace();
            }
            else if (ha.id == 2)
            {
                // Opening Sequence
                PlayVideo("ffade1.smk");
                PlayVideo("musosp1.smk");
                PlayVideo("newcred.smk");
                PlayVideo("fasall.smk");
                PlayVideo("mus5p2.smk");
                PlayVideo("coach.smk");
                PlayVideo("outmin.smk");
            }
            else if (ha.id == 3)
            {
                // Technical Information
                PlayVideo("ffade3.smk");
                PlayVideo("idfx4a.smk");
                PlayVideo("idfx4b.smk");
                PlayVideo("idfx4c.smk");
                PlayVideo("idfx4d.smk");
                PlayVideo("idfx4e.smk");
                PlayVideo("idfx4f.smk");
                PlayVideo("idfx4g.smk");
            }
            else if (ha.id == 4)
            {
                // About AdventureSoft
                PlayVideo("ffade2.smk");
                PlayVideo("fscene3b.smk");
                PlayVideo("fscene3a.smk");
                PlayVideo("fscene3c.smk");
                PlayVideo("fscene3g.smk");
            }
            else if (ha.id == 5)
            {
                // Video Clips
                PlayVideo("ffade4.smk");
                FilmMenu();
            }
            else if (ha.id == 6)
            {
                // Exit InfoDisk
                PlayVideo("ffade6.smk");
                ExitMenu();
            }
        }

        private void StartInteractiveVideo(string filename)
        {
            SetBitFlag(40, true);
            _interactiveVideo = VideoFlags.TYPE_LOOPING;
            _moviePlayer = MakeMoviePlayer(this, filename);
            _moviePlayer.Load();
            _moviePlayer.Play();
            SetBitFlag(40, false);
        }

        private void WaitForSpace()
        {
            string message;

            if (_language == Language.DE_DEU)
            {
                message = "Dr\x81cken Sie die <Leertaste>, um fortzufahren...";
            }
            else
            {
                message = "Press <SPACE> to continue...";
            }

            WindowPutChar(_textWindow, 12);
            foreach (var c in message)
                WindowPutChar(_textWindow, (byte) c);

            MouseOff();
            do
            {
                Delay(1);
            } while (!HasToQuit && !_keyPressed.IsKeyDown(KeyCode.Space));
            _keyPressed = new ScummInputState();
            MouseOn();
        }

        private void ExitMenu()
        {
            for (int i = 1; i <= 20; i++)
                DisableBox(i);

            EnableBox(21);

            PlayVideo("fhypno.smk");
            PlayVideo("fbye1.smk", true);

            HitArea ha;
            do
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                while (!HasToQuit && _lastHitArea3 == null)
                {
                    Delay(1);
                }

                ha = _lastHitArea;
            } while (!HasToQuit && !(ha != null && ha.id == 21));

            PlayVideo("fbye2.smk");
            QuitGame();
            Delay(0);
        }

        private void FilmMenu()
        {
            for (int i = 1; i <= 6; i++)
                DisableBox(i);

            for (int i = 11; i <= 19; i++)
                EnableBox(i);

            if (!_filmMenuUsed)
            {
                PlayVideo("fclipsin.smk", true);
            }
            else
            {
                PlayVideo("fclipin2.smk", true);
            }

            _filmMenuUsed = true;

            HitArea ha;
            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                while (!HasToQuit && _lastHitArea3 == null)
                {
                    HandleWobble();
                    Delay(1);
                }

                ha = _lastHitArea;

                if (ha == null)
                    continue;

                StopInteractiveVideo();

                if (ha.id == 11)
                {
                    PlayVideo("fgo1.smk");
                    PlayVideo("maze.smk");
                }
                else if (ha.id == 12)
                {
                    PlayVideo("fgo2.smk");
                    PlayVideo("radioin.smk");
                }
                else if (ha.id == 13)
                {
                    PlayVideo("fgo3.smk");
                    PlayVideo("pad.smk");
                }
                else if (ha.id == 14)
                {
                    PlayVideo("fgo4.smk");
                    PlayVideo("bridge.smk");
                }
                else if (ha.id == 15)
                {
                    PlayVideo("fgo5.smk");
                    PlayVideo("pilldie.smk");
                }
                else if (ha.id == 16)
                {
                    PlayVideo("fgo6.smk");
                    PlayVideo("bikebust.smk");
                }
                else if (ha.id == 17)
                {
                    PlayVideo("fgo7.smk");
                    PlayVideo("statue.smk");
                }
                else if (ha.id == 18)
                {
                    PlayVideo("fgo8.smk");
                    PlayVideo("junkout.smk");
                }
                else if (ha.id == 19)
                {
                    PlayVideo("fgo9.smk");
                    break;
                }

                PlayVideo("fclipin2.smk", true);
            }
        }

        private void HandleWobble()
        {
            if (_lastClickRem == _currentBox)
                return;

            StopInteractiveVideo();

            if (_currentBox != null && (_currentBox.id >= 11 && _currentBox.id <= 19))
            {
                var filename = $"wobble{_currentBox.id - 10}.smk";

                StartInteractiveVideo(filename);
            }

            _lastClickRem = _currentBox;
        }

        private void HandleText()
        {
            if (_lastClickRem == _currentBox)
                return;

            if (_currentBox != null && _currentBox.id >= 1 && _currentBox.id <= 6)
            {
                // TODO: Add the subtitles for menu options
            }

            _lastClickRem = _currentBox;
        }
    }
}