//
//  IMuseSysEx.cs
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
using System.IO;
using NScumm.Core.Audio.IMuse;
using NScumm.Core.Audio.Midi;

namespace NScumm.Core
{
    public class IMuseSysEx: ISysEx
    {
        readonly IIMuse imuse;

        public IMuseSysEx(IIMuse imuse)
        {
            this.imuse = imuse;
        }

        #region ISysEx implementation

        public void Do(IMidiPlayer midi, Stream input)
        {
            var br = new BinaryReader(input);
            var code = br.ReadByte();
            switch (code)
            {
                case 2: // Start of song. Ignore for now.
                    break;

                case 16:
                    // channel index
                    var c = br.ReadByte() & 0xF;
                    // Skip hardware type
                    br.ReadByte();

                    var data = ReadSysExBytes(input, input.Length - 2);
                    midi.Channels[c].ins[0] = data[0];
                    midi.Channels[c].ins[2] = (byte)(0xff - data[1] & 0x3f);
                    midi.Channels[c].ins[4] = (byte)(0xff - data[2]);
                    midi.Channels[c].ins[6] = (byte)(0xff - data[3]);
                    midi.Channels[c].ins[8] = data[4];

                    midi.Channels[c].ins[1] = data[5];
                    midi.Channels[c].ins[3] = (byte)(0xff - data[6] & 0x3f);
                    midi.Channels[c].ins[5] = (byte)(0xff - data[7]);
                    midi.Channels[c].ins[7] = (byte)(0xff - data[8]);
                    midi.Channels[c].ins[9] = data[9];

                    midi.Channels[c].ins[10] = data[10];

                    if ((data[10] & 1) == 1)
                        midi.Channels[c].ins[10] = 1;

                    break;

                case 64: // Marker
                    input.ReadByte();
                    var player = midi as IPlayer;
                    if (player != null)
                    {
                        while (input.Position < input.Length - 1)
                        {
                            imuse.HandleMarker(player.Id, input.ReadByte());
                        }
                    }
                    break;
                default:
                    System.Console.WriteLine("IMUSE code {0} not implemented.", code);
                    break;
            }
        }

        static byte[] ReadSysExBytes(Stream input, long length)
        {
            var data = new byte[length / 2];
            using (var ms = new MemoryStream(data))
            {
                while (length > 0)
                {
                    var read = ((input.ReadByte() << 4) & 0xFF) | (input.ReadByte() & 0xF);
                    ms.WriteByte((byte)read);
                    length -= 2;
                }
            }
            return data;
        }

        #endregion
    }
}

