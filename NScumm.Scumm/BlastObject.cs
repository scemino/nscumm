//
//  BlastObject.cs
//
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

using NScumm.Core.Graphics;

namespace NScumm.Scumm
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    class BlastObject
    {
        public int Number { get; set; }

        public Rect Rect { get; set; }

        public int ScaleX { get; set; }

        public int ScaleY { get; set; }

        public int Image { get; set; }

        public int Mode { get; set; }

        internal string DebuggerDisplay
        {
            get
            { 
                return string.Format("Id={0}, Rec={1}]", Number, Rect);
            }
        }
    }
    
}
