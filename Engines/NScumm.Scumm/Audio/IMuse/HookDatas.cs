//
//  HookDatas.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
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

using NScumm.Core;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse
{
    class HookDatas
    {
        public byte[] Jump { get; private set; }

        public byte Transpose { get; set; }

        public byte[] PartOnOff { get; private set; }

        public byte[] PartVolume { get; private set; }

        public byte[] PartProgram { get; private set; }

        public byte[] PartTranspose { get; private set; }

        public int QueryParam(int param, int chan)
        {
            switch (param)
            {
                case 18:
                    return Jump[0];
                case 19:
                    return Transpose;
                case 20:
                    return PartOnOff[chan];
                case 21:
                    return PartVolume[chan];
                case 22:
                    return PartProgram[chan];
                case 23:
                    return PartTranspose[chan];
                default:
                    return -1;
            }
        }

        public int Set(int cls, int value, int chan)
        {
            switch (cls)
            {
                case 0:
                    if (value != Jump[0])
                    {
                        Jump[1] = Jump[0];
                        Jump[0] = (byte)value;
                    }
                    break;
                case 1:
                    Transpose = (byte)value;
                    break;
                case 2:
                    if (chan < 16)
                        PartOnOff[chan] = (byte)value;
                    else if (chan == 16)
                        Set(PartOnOff, (byte)value);
                    break;
                case 3:
                    if (chan < 16)
                        PartVolume[chan] = (byte)value;
                    else if (chan == 16)
                        Set(PartVolume, (byte)value);
                    break;
                case 4:
                    if (chan < 16)
                        PartProgram[chan] = (byte)value;
                    else if (chan == 16)
                        Set(PartProgram, (byte)value);
                    break;
                case 5:
                    if (chan < 16)
                        PartTranspose[chan] = (byte)value;
                    else if (chan == 16)
                        Set(PartTranspose, (byte)value);
                    break;
                default:
                    return -1;
            }
            return 0;
        }

        public HookDatas()
        { 
            Jump = new byte[2];
            PartOnOff = new byte[16];
            PartVolume = new byte[16];
            PartProgram = new byte[16];
            PartTranspose = new byte[16];
        }

        public void SaveOrLoad(Serializer ser)
        {
            var hookEntries = new []
            {
                LoadAndSaveEntry.Create(r => Jump[0] = r.ReadByte(), w => w.WriteByte(Jump[0]), 8),
                LoadAndSaveEntry.Create(r => Jump[0] = r.ReadByte(), w => w.WriteByte(Jump[0]), 8),
                LoadAndSaveEntry.Create(r => Transpose = r.ReadByte(), w => w.WriteByte(Transpose), 8),
                LoadAndSaveEntry.Create(r => PartOnOff = r.ReadBytes(16), w => w.WriteBytes(PartOnOff, 16), 8),
                LoadAndSaveEntry.Create(r => PartVolume = r.ReadBytes(16), w => w.WriteBytes(PartVolume, 16), 8),
                LoadAndSaveEntry.Create(r => PartProgram = r.ReadBytes(16), w => w.WriteBytes(PartProgram, 16), 8),
                LoadAndSaveEntry.Create(r => PartTranspose = r.ReadBytes(16), w => w.WriteBytes(PartTranspose, 16), 8)
            };

            hookEntries.ForEach(e => e.Execute(ser));
        }

        static void Set<T>(T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }
    }
}
