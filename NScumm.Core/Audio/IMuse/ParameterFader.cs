//
//  ParameterFader.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

using NScumm.Core.Audio.IMuse;

namespace NScumm.Core.Audio.IMuse
{
    enum ParameterFaderType
    {
        None = 0,
        Volume = 1,
        Transpose = 3,
        Speed = 4,
        ClearAll = 127
    }

    class ParameterFader
    {
        public ParameterFaderType Param { get; set; }

        public int Start { get; set; }

        public int End { get; set; }

        public uint TotalTime { get; set; }

        public uint CurrentTime { get; set; }

        public void Init()
        {
            Param = ParameterFaderType.None;
        }
    }
    
}
