//
//  AkosHeader.cs
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

using NScumm.Core;

namespace NScumm.Scumm.IO
{
	struct AkosHeader
	{
		public ushort unk_1 {
			get { return Data.ToUInt16 (Offset); }
			set { Data.WriteUInt16 (Offset, value); }
		}

		public byte flags {
			get { return Data [Offset + 2]; }
			set { Data [Offset + 2] = value; }
		}

		public byte unk_2 {
			get { return Data [Offset + 3]; }
			set { Data [Offset + 3] = value; }
		}

		public ushort num_anims {
			get { return Data.ToUInt16 (Offset + 4); }
			set { Data.WriteUInt16 (Offset + 4, value); }
		}

		public ushort unk_3 {
			get { return Data.ToUInt16 (Offset + 6); }
			set { Data.WriteUInt16 (Offset + 6, value); }
		}

		public ushort codec {
			get { return Data.ToUInt16 (Offset + 8); }
			set { Data.WriteUInt16 (Offset + 8, value); }
		}

		public byte[] Data { get; }

		public int Offset { get; }

		public AkosHeader (byte[] data, int offset = 0)
		{
			Data = data;
			Offset = offset;
		}
	}

	struct AkosOffset
	{
		/// <summary>
		/// Gets or sets the offset into the akcd data.
		/// </summary>
		/// <value>The offset into the akcd data.</value>
		public uint akcd {
			get { return Data.ToUInt32 (Offset); }
			set { Data.WriteUInt32 (Offset, value); }
		}

		/// <summary>
		/// Gets or sets the offset into the akci data.
		/// </summary>
		/// <value>The offset into the akci data.</value>
		public ushort akci {
			get { return Data.ToUInt16 (Offset + 4); }
			set { Data.WriteUInt16 (Offset + 4, value); }
		}

		public byte[] Data { get; }

		public int Offset { get; }

		public AkosOffset (byte[] data, int offset)
		{
			Data = data;
			Offset = offset;
		}
	}
}

