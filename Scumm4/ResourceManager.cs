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
    public class ResourceManager
    {
        public Resource[] Rooms { get; private set; }
        public Resource[] Sounds { get; private set; }
        public Resource[] Scripts { get; private set; }
        public Resource[] Charsets { get; private set; }
        public Resource[] Costumes { get; private set; }
        public Resource[] Strings { get; private set; }

        public ResourceManager()
        {
            this.Rooms = new Resource[99];
            this.Sounds = new Resource[0xC7];
            this.Scripts = new Resource[0xC7];
            this.Charsets = new Resource[9];
            this.Costumes = new Resource[0xC7];
            this.Strings = new Resource[0x32];
        }
    }
}
