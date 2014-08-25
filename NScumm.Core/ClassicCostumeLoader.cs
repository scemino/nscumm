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

using NScumm.Core.IO;
using System;

namespace NScumm.Core
{
	class ClassicCostumeLoader : ICostumeLoader
	{
		public int Id;
		public long BasePtr;
		public long AnimCmds;
		public long DataOffsets;
		public byte[] Palette;
		public long FrameOffsets;
		public byte NumColors;
		public byte NumAnim;
		public byte Format;
		public bool Mirror;
		public XorReader CostumeReader;

		ScummEngine _vm;

		public ClassicCostumeLoader (ScummEngine vm)
		{
			_vm = vm;
			Id = -1;
		}

		public void LoadCostume (int id)
		{
			CostumeReader = _vm.ResourceManager.GetCostumeReader ((byte)id);

			BasePtr = CostumeReader.BaseStream.Position - 6;
			NumAnim = CostumeReader.ReadByte ();
			Format = (byte)(CostumeReader.PeekByte () & 0x7F);
			Mirror = (CostumeReader.ReadByte () & 0x80) != 0;

			switch (Format) {
			case 0x57:				// Only used in V1 games
				NumColors = 0;
				break;
			case 0x58:
				NumColors = 16;
				break;
			case 0x59:
				NumColors = 32;
				break;
			case 0x60:				// New since version 6
				NumColors = 16;
				break;
			case 0x61:				// New since version 6
				NumColors = 32;
				break;
			default:
				throw new NotSupportedException (string.Format ("Costume {0} with format 0x{1:X2} is invalid", id, Format));
			}

			Palette = CostumeReader.ReadBytes (NumColors);
			FrameOffsets = CostumeReader.BaseStream.Position + 2;
			if (Format == 0x57) {
				DataOffsets = CostumeReader.BaseStream.Position + 18;
				BasePtr += 4;
			} else {
				DataOffsets = CostumeReader.BaseStream.Position + 34;
			}
			AnimCmds = BasePtr + CostumeReader.ReadUInt16 ();
		}

		public void CostumeDecodeData (Actor a, int frame, uint usemask)
		{
			long r;
			long baseptr;
			uint mask, j;
			int i;
			byte extra, cmd;
			int anim;

			LoadCostume (a.Costume);

			anim = ScummHelper.NewDirToOldDir (a.Facing) + frame * 4;

			if (anim > NumAnim) {
				return;
			}

			baseptr = BasePtr;

			CostumeReader.BaseStream.Seek (DataOffsets + anim * 2, System.IO.SeekOrigin.Begin);
			r = baseptr + CostumeReader.ReadUInt16 ();
			if (r == baseptr) {
				return;
			}

			CostumeReader.BaseStream.Seek (r, System.IO.SeekOrigin.Begin);
			mask = CostumeReader.ReadUInt16 ();

			i = 0;
			do {
				if ((mask & 0x8000) != 0) {
					if (_vm.Game.Version <= 3) {
						j = CostumeReader.ReadByte ();

						if (j == 0xFF)
							j = 0xFFFF;
					} else {
						j = CostumeReader.ReadUInt16 ();
					}

					if ((usemask & 0x8000) != 0) {
						if (j == 0xFFFF) {
							a.Cost.Curpos [i] = 0xFFFF;
							a.Cost.Start [i] = 0;
							a.Cost.Frame [i] = (ushort)frame;
						} else {
							extra = CostumeReader.ReadByte ();
							cmd = ReadAnimCommand ((int)j);
							if (cmd == 0x7A) {
								a.Cost.Stopped &= (ushort)(~(1 << i));
							} else if (cmd == 0x79) {
								a.Cost.Stopped |= (ushort)(1 << i);
							} else {
								a.Cost.Curpos [i] = a.Cost.Start [i] = (ushort)j;
								a.Cost.End [i] = (ushort)(j + (extra & 0x7F));
								if ((extra & 0x80) > 0)
									a.Cost.Curpos [i] |= 0x8000;
								a.Cost.Frame [i] = (ushort)frame;
							}
						}
					} else {
						if (j != 0xFFFF) {
							CostumeReader.ReadByte ();
						}
					}
				}
				i++;
				usemask <<= 1;
				mask <<= 1;
			} while ((mask & 0xFFFF) > 0);
		}

		byte ReadAnimCommand (int j)
		{
			byte cmd;
			long r = CostumeReader.BaseStream.Position;
			CostumeReader.BaseStream.Seek (AnimCmds + j, System.IO.SeekOrigin.Begin);
			cmd = CostumeReader.ReadByte ();
			CostumeReader.BaseStream.Seek (r, System.IO.SeekOrigin.Begin);
			return cmd;
		}

		public int IncreaseAnims (Actor a)
		{
			int r = 0;

			for (int i = 0; i != 16; i++) {
				if (a.Cost.Curpos [i] != 0xFFFF)
					r += IncreaseAnim (a, i) ? 1 : 0;
			}
			return r;
		}

		bool IncreaseAnim (Actor a, int slot)
		{
			int highflag;
			int i, end;
			byte code, nc;

			if (a.Cost.Curpos [slot] == 0xFFFF)
				return false;

			highflag = a.Cost.Curpos [slot] & 0x8000;
			i = a.Cost.Curpos [slot] & 0x7FFF;
			end = a.Cost.End [slot];
			code = (byte)(ReadAnimCommand (i) & 0x7F);

			if (_vm.Game.Version <= 3) {
				if ((ReadAnimCommand (i) & 0x80) > 0) {
                    a.Cost.SoundCounter++;
				}
			}

			do {
				if (highflag == 0) {
					if (i++ >= end)
						i = a.Cost.Start [slot];
				} else {
					if (i != end)
						i++;
				}
				nc = ReadAnimCommand (i);

				if (nc == 0x7C) {
					a.Cost.AnimCounter++;
					if (a.Cost.Start [slot] != end)
						continue;
				} else {

					if (nc == 0x78) {
						a.Cost.SoundCounter++;
						if (a.Cost.Start [slot] != end)
							continue;
					}
				}

				a.Cost.Curpos [slot] = (ushort)(i | highflag);
				return (ReadAnimCommand (i) & 0x7F) != code;
			} while (true);
		}
	}
}
