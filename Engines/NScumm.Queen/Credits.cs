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
using System.Collections.Generic;
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Queen
{
    class Credits
    {
        QueenEngine _vm;
        List<string> _credits;

        //! true if end of credits description hasn't been reached
        bool _running;

        //! number of elements in _list array
        int _count;

        //! pause counts for next room
        int _pause;

        //! current text justification mode
        int _justify;

        //! current font size (unused ?)
        int _fontSize;

        //! current text color
        int _color;

        //! current text position
        int _zone;

        int _lineNum;

        struct Line
        {
            public short x, y, color, fontSize;
            public string text;
        }

        //! contains the formatted lines of texts to display
        Line[] _list = new Line[19];

        public Credits(QueenEngine vm, string filename)
        {
            _vm = vm;
            _running = true;
            _credits = _vm.Resource.LoadTextFile(filename);
        }

        public void NextRoom()
        {
            if (-1 == _pause)
            {
                _pause = 0;
                _vm.Display.ClearTexts(0, 199);
            }
        }

        public void Update()
        {
            if (!_running)
                return;

            if (_pause > 0)
            {
                _pause--;
                if (_pause == 0)
                    _vm.Display.ClearTexts(0, 199);
                return;
            }

            /* wait until next room */
            if (-1 == _pause)
                return;

            while (_lineNum < _credits.Count)
            {
                string line = _credits[_lineNum];
                ++_lineNum;

                if (0 == string.Compare(line, 0, "EN", 0, 2))
                {
                    _running = false;
                    return;
                }

                if (line.Length > 0 && '.' == line[0])
                {
                    int i;

                    switch (char.ToLower(line[1]))
                    {
                        case 'l':
                            _justify = 0;
                            break;
                        case 'c':
                            _justify = 1;
                            break;
                        case 'r':
                            _justify = 2;
                            break;
                        case 's':
                            _fontSize = 0;
                            break;
                        case 'b':
                            _fontSize = 1;
                            break;
                        case 'p':
                            _pause = int.Parse(line.Substring(3));
                            _pause *= 10;
                            /* wait until next room */
                            if (0 == _pause)
                                _pause = -1;
                            for (i = 0; i < _count; i++)
                            {
                                _vm.Display.TextCurrentColor((byte)_list[i].color);
                                _vm.Display.SetText((ushort)_list[i].x, (ushort)_list[i].y, _list[i].text);
                            }
                            _count = 0;
                            return;
                        case 'i':
                            _color = int.Parse(line.Substring(3));
                            if (_vm.Resource.Platform == Platform.Amiga)
                            {
                                _color &= 31;
                            }
                            break;
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            _zone = line[1] - '1';
                            break;
                    }
                }
                else
                {
                    Debug.Assert(_count < _list.Length);
                    _list[_count].text = line;
                    _list[_count].color = (short)_color;
                    _list[_count].fontSize = (short)_fontSize;
                    switch (_justify)
                    {
                        case 0:
                            _list[_count].x = (short)((_zone % 3) * (320 / 3) + 8);
                            break;
                        case 1:
                            _list[_count].x = (short)((_zone % 3) * (320 / 3) + 54 - _vm.Display.TextWidth(line) / 2);
                            if (_list[_count].x < 8)
                                _list[_count].x = 8;
                            break;
                        case 2:
                            _list[_count].x = (short)((_zone % 3) * (320 / 3) + 100 - _vm.Display.TextWidth(line));
                            break;
                    }
                    _list[_count].y = (short)((_zone / 3) * (200 / 3) + (_count * 10));
                    _count++;
                }
            }
            _running = false;
        }
    }
}

