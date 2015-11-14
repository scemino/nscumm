//
//  V0CostumeLoader.cs
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

namespace NScumm.Scumm.IO
{
    class CostumeLoader0: ICostumeLoader
    {
        protected ScummEngine _vm;

        public int Id { get; private set; }

        public byte Format { get; private set; }

        public bool Mirror { get; private set; }

        public byte[] Palette { get; private set; }

        public long FrameOffsets { get; private set; }

        public long AnimCmds { get; private set; }

        public byte[] Data { get; private set; }

        public CostumeLoader0(ScummEngine vm)
        {            
            _vm = vm;
            Id = -1;
        }

        public void LoadCostume(int id)
        {
            _vm.ResourceManager.LoadCostume(id);
            Data = _vm.ResourceManager.GetCostumeData(id);

            Id = id;

            Format = 0x57;
            Mirror = false;
//            Palette = &actorV0Colors[id];
            Palette = new byte[actorV0Colors.Length - id];
            Array.Copy(actorV0Colors, id, Palette, 0, Palette.Length);

            FrameOffsets = 9 + BitConverter.ToUInt16(Data, 5);
            AnimCmds = 9 + BitConverter.ToUInt16(Data, 7);
        }

        bool ICostumeLoader.HasManyDirections(int id)
        {
            return false;
        }

        public void CostumeDecodeData(Actor a, int frame, uint usemask)
        {
            var a0 = (Actor0)a;

            if (a.Costume == 0)
                return;

            LoadCostume(a.Costume);

            // Invalid costume command?
            if (a0.CostCommandNew == 0xFF || (a0.CostCommand == a0.CostCommandNew))
                return;

            a0.CostCommand = a0.CostCommandNew;

            int cmd = a0.CostCommand;
            byte limbFrameNumber;

            // Each costume-command has 8 limbs  (0x2622)
            cmd <<= 3;

            for (int limb = 0; limb < 8; ++limb)
            {
                // get the frame number for the beginning of the costume command
                limbFrameNumber = Data[AnimCmds + cmd + limb];

                // Is this limb flipped?
                if ((limbFrameNumber & 0x80) != 0)
                {
                    // Invalid frame?
                    if (limbFrameNumber == 0xFF)
                        continue;

                    // Store the limb frame number (clear the flipped status)
                    a.Cost.Frame[limb] = (ushort)(limbFrameNumber & 0x7f);

                    if (!a0.LimbFlipped[limb])
                        a.Cost.Start[limb] = 0xFFFF;

                    a0.LimbFlipped[limb] = true;

                }
                else
                {
                    //Store the limb frame number
                    a.Cost.Frame[limb] = limbFrameNumber;

                    if (a0.LimbFlipped[limb])
                        a.Cost.Start[limb] = 0xFFFF;

                    a0.LimbFlipped[limb] = false;
                }

                // Set the repeat value
                a0.LimbFrameRepeatNew[limb] = a0.AnimFrameRepeat;
            }
        }

        public byte GetFrame(Actor a, int limb)
        {
            LoadCostume(a.Costume);

            // Get the frame number for the current limb / Command
            return Data[FrameOffsets+ Data[FrameOffsets+limb] + a.Cost.Start[limb]];
        }

        public int IncreaseAnims(Actor a)
        {
            var a0 = (Actor0)a;
            int r = 0;

            for (var i = 0; i != 8; i++)
            {
                a0.LimbFrameCheck(i);
                r += IncreaseAnim(a, i) ? 1 : 0;
            }
            return r;
        }

        bool IncreaseAnim(Actor a, int limb) {
            var a0 = (Actor0)a;
            var limbPrevious = a.Cost.Curpos[limb]++;

            LoadCostume(a.Costume);

            // 0x2543
            byte frame = Data[FrameOffsets + a.Cost.Curpos[limb] + a.Cost.Active[limb]];

            // Is this frame invalid?
            if (frame == 0xFF) {

                // Repeat timer has reached 0?
                if (a0.LimbFrameRepeat[limb] == 0) {

                    // Use the previous frame
                    --a0.Cost.Curpos[limb];

                    // Reset the comstume command
                    a0.CostCommandNew = 0xFF;
                    a0.CostCommand = 0xFF;

                    // Set the frame/start to invalid
                    a0.Cost.Frame[limb] = 0xFFFF;
                    a0.Cost.Start[limb] = 0xFFFF;

                } else {

                    // Repeat timer enabled?
                    if (a0.LimbFrameRepeat[limb] != -1)
                        --a0.LimbFrameRepeat[limb];

                    // No, restart at frame 0
                    a.Cost.Curpos[limb] = 0;
                }
            }

            // Limb frame has changed?
            if (limbPrevious == a.Cost.Curpos[limb])
                return false;

            return true;
        }

        public static readonly byte[] actorV0Colors =
            {
                0, 7, 2, 6, 9, 1, 3, 7, 7, 1, 1, 9, 1, 4, 5, 5, 4, 1, 0, 5, 4, 2, 2, 7, 7
            };
    }
    
}
