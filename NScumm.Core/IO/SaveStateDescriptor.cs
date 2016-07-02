//
//  SaveStateDescriptor.cs
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
using NScumm.Core.Graphics;

namespace NScumm.Core.IO
{
    /// <summary>
    /// Object describing a save state.
    /// This at least includes the save slot number and a human readable
    /// description of the save state.
    /// Further possibilites are a thumbnail, play time, creation date,
    /// creation time, delete protected, write protection.
    /// Saves are writable and deletable by default.
    /// </summary>
    public class SaveStateDescriptor
    {
        public int SaveSlot { get; set; }
        public string Description { get; set; }
        public bool IsDeletable { get; set; }
        public bool IsWriteProtected { get; set; }
        public Surface Thumbnail { get; set; }
        public DateTime SaveDate { get; set; }
        public TimeSpan PlayTime { get; set; }

        public SaveStateDescriptor()
        {
            SaveSlot = -1;
            IsDeletable = true;
        }

        public SaveStateDescriptor(int slot, string desc)
        {
            SaveSlot = slot;
            Description = desc;
        }
    }
    
}
