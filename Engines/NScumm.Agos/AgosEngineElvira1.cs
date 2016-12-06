//
//  AGOSEngine_Elvira1.cs
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
using NScumm.Core.IO;

namespace NScumm.Agos
{
    internal abstract class AgosEngineElvira1 : AgosEngine
    {
        public AgosEngineElvira1(ISystem system, GameSettings settings, AgosGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected void oe1_rescan()
        {
            // 164: restart subroutine
            SetScriptReturn(-10);
        }

        protected void oe1_stopAnimate()
        {
            // 227: stop animate
            StopAnimate((ushort) GetVarOrWord());
        }
    }
}