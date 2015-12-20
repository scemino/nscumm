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

namespace NScumm.Core
{
    [Flags]
    public enum MouseButtonStatus
    {
        None = 0,
        Down = 1,
        Clicked = 2
    }

    public enum KeyCode
    {
        None = 0,

        LeftControl = 1,

        Backspace = 8,
        Tab = 9,
        Return = 13,
        Escape = 27,
        Space = 32,
        OemPeriod = 46,

        F1 = 315,
        F2 = 316,
        F3 = 317,
        F4 = 318,
        F5 = 319,
        F6 = 320,
        F7 = 321,
        F8 = 322,
        F9 = 323,
        F10 = 324,
        F11 = 325,
        F12 = 326,

        A = 97,
        B = 98,
        C = 99,
        D = 100,
        E = 101,
        F = 102,
        G = 103,
        H = 104,
        I = 105,
        J = 106,
        K = 107,
        L = 108,
        M = 109,
        N = 110,
        O = 111,
        P = 112,
        Q = 113,
        R = 114,
        S = 115,
        T = 116,
        U = 117,
        V = 118,
        W = 119,
        X = 120,
        Y = 121,
        Z = 122,

        D0 = 50,
        D1 = 51,
        D2 = 52,
        D3 = 53,
        D4 = 54,
        D5 = 55,
        D6 = 56,
        D7 = 57,
        D8 = 58,
        D9 = 59,

        // Numeric keypad
        NumPad0 = 256,
        NumPad1 = 257,
        NumPad2 = 258,
        NumPad3 = 259,
        NumPad4 = 260,
        NumPad5 = 261,
        NumPad6 = 262,
        NumPad7 = 263,
        NumPad8 = 264,
        NumPad9 = 265,

        // Arrows + Home/End pad
        Up = 273,
        Down = 274,
        Right = 275,
        Left = 276,
        Insert = 277,
        Home = 278,
        End = 279,
        PageUp = 280,
        PageDown = 281,

        LeftShift = 304,
    }
}
