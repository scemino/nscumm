//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

namespace NScumm.Sci.Engine
{
    internal struct ClassReference
    {
        public int script;
        public string className;
        public string selectorName;
        public SelectorType selectorType;
        public uint selectorOffset;

        public ClassReference(int script, string className, string selectorName, SelectorType selectorType,
            uint selectorOffset)
        {
            this.script = script;
            this.className = className;
            this.selectorName = selectorName;
            this.selectorType = selectorType;
            this.selectorOffset = selectorOffset;
        }
    }
}