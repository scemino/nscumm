//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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

namespace NScumm.Another
{
    /// <summary>
    /// This is a directory entry. When the game starts, it loads memlist.bin and
    /// populate and array of MemEntry
    /// </summary>
    internal class MemEntry
    {
        public const int StateEndOfMemlist = 0xFF;
        public const int NotNeeded = 0;
        public const int Loaded = 1;
        public const int LoadMe = 2;

        public byte State; // 0x0
        public ResType Type; // 0x1, Resource::ResType
        public BytePtr BufPtr; // 0x2
        public ushort Unk4; // 0x4, unused
        public byte RankNum; // 0x6
        public byte BankId; // 0x7
        public uint BankOffset; // 0x8 0xA
        public ushort UnkC; // 0xC, unused

        public ushort PackedSize; // 0xE
        // All ressources are packed (for a gain of 28% according to Chahi)

        public ushort Unk10; // 0x10, unused
        public ushort Size; // 0x12
    }
}