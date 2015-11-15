//
//  SamAndMaxSysEx.cs
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
using NScumm.Core;

namespace NScumm.Scumm.Audio.IMuse
{
    class SamAndMaxSysEx: ISysEx
    {
        const int TicksPerBeat = 480;

        ScummSysEx scummSysEx = new ScummSysEx();

        #region ISysEx implementation

        public void Do(Player player, byte[] msg, ushort len)
        {
            IMuseInternal se = player._se;

            switch (msg[0])
            {
                case 0:
                    // Trigger Event
                    // Triggers are set by doCommand(ImSetTrigger).
                    // When a SysEx marker is encountered whose sound
                    // ID and marker ID match what was set by ImSetTrigger,
                    // something magical is supposed to happen....
                    for (var a = 0; a < se._snm_triggers.Length; ++a)
                    {
                        if (se._snm_triggers[a].Sound == player.Id &&
                            se._snm_triggers[a].Id == msg[1])
                        {
                            se._snm_triggers[a].Sound = se._snm_triggers[a].Id = 0;
                            se.DoCommand(8, se._snm_triggers[a].Command);
                            break;
                        }
                    }
                    break;

                case 1:
                    // maybe_jump.
                    if (player.Scanning)
                        break;
                    using (var br = new BinaryReader(new MemoryStream(msg)))
                    {
                        br.ReadByte();

                        player.MaybeJump(br.ReadByte(), (uint)(br.ReadByte() - 1), (uint)((br.ReadUInt16BigEndian() - 1) * 4 + br.ReadByte()),
                            (uint)((br.ReadByte() * TicksPerBeat) >> 2) + br.ReadByte());
                    }
                    break;

                default:
                    scummSysEx.Do(player, msg, len);
                    break;
            }
        }

        #endregion
    }
}
