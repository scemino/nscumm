/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class ObjectData
    {
        public uint OBIMoffset;
        public uint OBCDoffset;
        public short walk_x, walk_y;
        public ushort obj_nr;
        public short x_pos;
        public short y_pos;
        public ushort width;
        public ushort height;
        public byte actordir;
        public byte parent;
        public byte parentstate;
        public byte state;
        public byte fl_object_index;
        public DrawBitmapFlags flags;

        public Dictionary<byte, ushort> ScriptOffsets { get; private set; }
        public Dictionary<byte, ScriptData> Scripts { get; private set; }
        public string Name { get; set; }
        public List<Strip> Strips { get; private set; }

        public ObjectData()
        {
            this.ScriptOffsets = new Dictionary<byte, ushort>();
            this.Scripts = new Dictionary<byte, ScriptData>();
            this.Strips = new List<Strip>();
        }
    }
}
