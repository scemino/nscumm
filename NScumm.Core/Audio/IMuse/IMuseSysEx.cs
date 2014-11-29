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
using System;

namespace NScumm.Core.Audio.IMuse
{
    class IMuseSysEx: ISysEx
    {
        #region ISysEx implementation

        public void Do(Player player, byte[] msg, ushort len)
        {
            IMuseInternal se = player._se;
            int p = 0;

            byte code;
            switch (code = msg[p++])
            {
                case 0:
                    {
                        // Allocate new part.
                        // There are 8 bytes (after decoding!) of useful information here.
                        // Here is what we know about them so far:
                        //   BYTE 0: Channel #
                        //   BYTE 1: BIT 01(0x01): Part on?(1 = yes)
                        //            BIT 02(0x02): Reverb? (1 = yes) [bug #1088045]
                        //   BYTE 2: Priority adjustment
                        //   BYTE 3: Volume [guessing]
                        //   BYTE 4: Pan [bug #1088045]
                        //   BYTE 5: BIT 8(0x80): Percussion?(1 = yes) [guessed?]
                        //   BYTE 5: Transpose, if set to 0x80(=-1) it means no transpose
                        //   BYTE 6: Detune
                        //   BYTE 7: Pitchbend factor [bug #1088045]
                        //   BYTE 8: Program

                        var part = player.GetPart((byte)(msg[p] & 0x0F));
                        var buf = DecodeSysExBytes(msg, p + 1, len - 1);
                        if (part != null)
                        {
                            part.SetOnOff((buf[0] & 0x01) != 0);
                            part.EffectLevel((byte)(((buf[0] & 0x02) != 0) ? 127 : 0));
                            part.Priority = buf[1];
                            part.Volume = buf[2];
                            part.Pan = buf[3];
                            part.Percussion = player.SupportsPercussion && ((buf[4] & 0x80) > 0);
                            part.SetTranspose((sbyte)buf[4]);
                            part.Detune = buf[5];
                            part.PitchBendFactor(buf[6]);
                            if (part.Percussion)
                            {
                                if (part.MidiChannel != null)
                                {
                                    part.Off();
                                    se.ReallocateMidiChannels(player.MidiDriver);
                                }
                            }
                            else
                            {
                                if (player.IsMIDI)
                                {
                                    // Even in cases where a program does not seem to be specified,
                                    // i.e. bytes 15 and 16 are 0, we send a program change because
                                    // 0 is a valid program number. MI2 tests show that in such
                                    // cases, a regular program change message always seems to follow
                                    // anyway.
                                    part.Instrument.Program(buf[7], player.IsMT32);
                                }
                                else
                                {
                                    // Like the original we set up the instrument data of the
                                    // specified program here too. In case the global
                                    // instrument data is not loaded already, this will take
                                    // care of setting a default instrument too.
                                    se.CopyGlobalInstrument(buf[7], part.Instrument);
                                }
                                part.SendAll();
                            }
                        }
                    }
                    break;

                case 1:
                        // Shut down a part. [Bug 1088045, comments]
                    {
                        var part = player.GetPart(msg[p]);
                        if (part != null)
                            part.Uninit();
                    }
                    break;

                case 2: // Start of song. Ignore for now.
                    break;

                case 16: // AdLib instrument definition(Part)
                    {
                        var a = (byte)(msg[p++] & 0x0F);
                        ++p; // Skip hardware type
                        var part = player.GetPart(a);
                        if (part != null)
                        {
                            if (len == 62 || len == 48)
                            {
                                var buf = DecodeSysExBytes(msg, p, len - 2);
                                part.SetInstrument(buf);
                            }
                            else
                            {
                                part.ProgramChange(254); // Must be invalid, but not 255 (which is reserved)
                            }
                        }
                    }
                    break;

//                case 17: // AdLib instrument definition(Global)
//                    p += 2; // Skip hardware type and... whatever came right before it
//                    a = *p++;
//                    player.decode_sysex_bytes(p, buf, len - 3);
//                    if (len == 63 || len == 49)
//                        se.setGlobalInstrument(a, buf);
//                    break;
//
//                case 33: // Parameter adjust
//                    a = *p++ & 0x0F;
//                    ++p; // Skip hardware type
//                    player.decode_sysex_bytes(p, buf, len - 2);
//                    part = player.GetPart(a);
//                    if (part)
//                        part.set_param(READ_BE_UINT16(buf), READ_BE_UINT16(buf + 2));
//                    break;
//
                case 48: // Hook - jump
                    {
                        if (player.Scanning)
                            break;
                        var buf = DecodeSysExBytes(msg, p + 1, len - 1);
                        using (var br = new BinaryReader(new MemoryStream(buf)))
                        {
                            player.MaybeJump(br.ReadByte(), br.ReadUInt16BigEndian(), br.ReadUInt16BigEndian(), br.ReadUInt16BigEndian());
                        }
                    }
                    break;
//
//                case 49: // Hook - global transpose
//                    player.decode_sysex_bytes(p + 1, buf, len - 1);
//                    player.maybe_set_transpose(buf);
//                    break;
//
//                case 50: // Hook - part on/off
//                    buf[0] = *p++ & 0x0F;
//                    player.decode_sysex_bytes(p, buf + 1, len - 1);
//                    player.maybe_part_onoff(buf);
//                    break;
//
//                case 51: // Hook - set volume
//                    buf[0] = *p++ & 0x0F;
//                    player.decode_sysex_bytes(p, buf + 1, len - 1);
//                    player.maybe_set_volume(buf);
//                    break;
//
//                case 52: // Hook - set program
//                    buf[0] = *p++ & 0x0F;
//                    player.decode_sysex_bytes(p, buf + 1, len - 1);
//                    player.maybe_set_program(buf);
//                    break;
//
//                case 53: // Hook - set transpose
//                    buf[0] = *p++ & 0x0F;
//                    player.decode_sysex_bytes(p, buf + 1, len - 1);
//                    player.maybe_set_transpose_part(buf);
//                    break;
//
                case 64: // Marker
                    p++;
                    len--;
                    while (len-- != 0)
                    {
                        se.HandleMarker(player.Id, msg[p++]);
                    }
                    break;
//
//                case 80: // Loop
//                    player.decode_sysex_bytes(p + 1, buf, len - 1);
//                    player.setLoop(READ_BE_UINT16(buf), READ_BE_UINT16(buf + 2),
//                        READ_BE_UINT16(buf + 4), READ_BE_UINT16(buf + 6),
//                        READ_BE_UINT16(buf + 8));
//                    break;
//
//                case 81: // End loop
//                    player.clearLoop();
//                    break;
//
//                case 96: // Set instrument
//                    part = player.GetPart(p[0] & 0x0F);
//                    a = (p[1] & 0x0F) << 12 | (p[2] & 0x0F) << 8 | (p[3] & 0x0F) << 4 | (p[4] & 0x0F);
//                    if (part)
//                        part.set_instrument(a);
//                    break;

                default:
                    Console.Error.WriteLine("Unknown SysEx command {0}", (int)code);
                    break;
            }
        }

        static byte[] DecodeSysExBytes(byte[] input, int pos, long length)
        {
            var data = new byte[length / 2];
            using (var ms = new MemoryStream(data))
            {
                while (length > 0)
                {
                    var read = ((input[pos++] << 4) & 0xFF) | (input[pos++] & 0xF);
                    ms.WriteByte((byte)read);
                    length -= 2;
                }
            }
            return data;
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

