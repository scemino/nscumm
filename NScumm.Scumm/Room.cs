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

using System.Collections.Generic;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm
{
    public class RoomHeader
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int NumObjects { get; set; }

        public int NumZBuffer { get; set; }

        public int Transparency { get; set; }
    }


    public class Room
    {
        public int Size { get; set; }

        public RoomHeader Header { get; set; }

        public Palette Palette { get { return Palettes[0]; } }

        public List<Palette> Palettes { get; private set; }

        public ScriptData[] LocalScripts { get; private set; }

        public ScriptData EntryScript { get; private set; }

        public ScriptData ExitScript { get; private set; }

        public List<ObjectData> Objects { get; private set; }

        public List<Box> Boxes { get; private set; }

        public List<byte> BoxMatrix { get; private set; }

        public string Name { get; set; }

        public int Number { get; set; }

        public ImageData Image { get; set; }

        public int NumZBuffer { get; set; }

        public ScaleSlot[] Scales { get; set; }

        public byte TransparentColor { get; set; }

        public bool HasPalette { get; set; }

        public ColorCycle[] ColorCycle { get; set; }

        public Room()
        {
            Boxes = new List<Box>();
            Objects = new List<ObjectData>();
            BoxMatrix = new List<byte>();
            EntryScript = new ScriptData();
            ExitScript = new ScriptData();
            LocalScripts = new ScriptData[1024];
            TransparentColor = 255;
            Scales = new ScaleSlot[0];
            ColorCycle = new ColorCycle[16];
            for (int i = 0; i < ColorCycle.Length; i++)
            {
                ColorCycle[i] = new ColorCycle();
            }
            Image = new ImageData();
            Palettes = new List<Palette>();
            Palettes.Add(new Palette());
        }
    }
}
