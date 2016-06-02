//
//  QueenEngine.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System;
using NScumm.Core;
using NScumm.Core.IO;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;

namespace NScumm.Queen
{
	enum BamFlags
	{
		F_STOP = 0,
		F_PLAY = 1,
		F_REQ_STOP = 2
	}

	class BamDataObj
	{
		public short x, y;
		public short frame;

		public BamDataObj (short x, short y, short frame)
		{
			this.x = x;
			this.y = y;
			this.frame = frame;
		}
	}

	class BamDataBlock
	{
		public BamDataObj obj1;
		// truck / Frank
		public BamDataObj obj2;
		// Rico  / robot
		public BamDataObj fx;
		public short sfx;

		public BamDataBlock (short o1x, short o1y, short o1f, short o2x, short o2y, short o2f, short fxx, short fxy, short fxf, short sfx)
		{
			obj1 = new BamDataObj (o1x, o1y, o1f);
			obj2 = new BamDataObj (o2x, o2y, o2f);
			fx = new BamDataObj (fxx, fxy, fxf);
			this.sfx = sfx;
		}

		public BamDataBlock (BamDataObj obj1, BamDataObj obj2, BamDataObj fx, short sfx)
		{
			this.obj1 = obj1;
			this.obj2 = obj2;
			this.fx = fx;
			this.sfx = sfx;
		}
	}

	public class BamScene
	{
		private BamFlags _flag;
		private ushort _index;
		private QueenEngine _vm;
		private BamDataBlock[] _fightData;

		private static readonly BamDataBlock[] _fight1Data = {
			new BamDataBlock (75, 96, 1, 187, 96, -23, 58, 37, 46, 0),
			new BamDataBlock (75, 96, 2, 187, 96, -23, 58, 37, 46, 0),
			new BamDataBlock (75, 96, 3, 187, 96, -23, 58, 37, 46, 0),
			new BamDataBlock (75, 96, 4, 187, 96, -23, 58, 37, 46, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 58, 37, 46, 0),
			new BamDataBlock (75, 96, 2, 187, 96, -23, 58, 37, 46, 0),
			new BamDataBlock (75, 96, 3, 187, 96, -23, 58, 37, 46, 0),
			new BamDataBlock (75, 96, 4, 187, 96, -24, 58, 37, 46, 0),
			new BamDataBlock (79, 96, 1, 187, 96, -24, 58, 37, 46, 0),
			new BamDataBlock (85, 96, 2, 187, 96, -24, 58, 37, 46, 0),
			new BamDataBlock (94, 96, 3, 187, 96, -24, 58, 37, 46, 0),
			new BamDataBlock (100, 96, 4, 187, 96, -24, 58, 37, 46, 0),
			new BamDataBlock (113, 96, 1, 187, 96, -25, 58, 37, 46, 0),
			new BamDataBlock (121, 96, 1, 187, 96, -25, 58, 37, 46, 0),
			new BamDataBlock (136, 96, 16, 187, 96, -26, 58, 37, 46, 0),
			new BamDataBlock (151, 93, 6, 187, 96, -27, 58, 37, 46, 0),
			new BamDataBlock (159, 83, 16, 187, 96, -28, 58, 37, 46, 0),
			new BamDataBlock (170, 73, 16, 187, 96, -29, 182, 96, 48, 3),
			new BamDataBlock (176, 69, 13, 187, 96, -31, 182, 94, 49, 1),
			new BamDataBlock (168, 66, 13, 187, 98, -32, 182, 92, 50, 0),
			new BamDataBlock (155, 75, 13, 187, 96, -32, 182, 88, 51, 3),
			new BamDataBlock (145, 86, 13, 187, 98, -32, 182, 85, 52, 0),
			new BamDataBlock (127, 104, 13, 187, 98, -32, 182, 25, 52, 1),
			new BamDataBlock (122, 108, 13, 187, 98, -32, 182, 25, 52, 1),
			new BamDataBlock (120, 104, 14, 187, 96, -34, 107, 145, 42, 2),
			new BamDataBlock (111, 103, 13, 187, 96, -23, 107, 144, 43, 0),
			new BamDataBlock (102, 105, 13, 187, 96, -23, 107, 142, 43, 0),
			new BamDataBlock (97, 107, 13, 187, 96, -23, 107, 139, 44, 0),
			new BamDataBlock (92, 101, 14, 187, 96, -23, 107, 34, 47, 3),
			new BamDataBlock (90, 105, 14, 187, 96, -23, 107, 34, 47, 0),
			new BamDataBlock (88, 104, 14, 187, 96, -23, 107, 34, 47, 0),
			new BamDataBlock (87, 105, 14, 187, 96, -23, 107, 34, 47, 0),
			new BamDataBlock (86, 105, 14, 187, 96, -23, 107, 34, 47, 0),
			new BamDataBlock (86, 105, 14, 187, 96, -23, 107, 34, 47, 0),
			new BamDataBlock (86, 105, 15, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (85, 98, 16, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (92, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (92, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (89, 96, 4, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (86, 96, 3, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (83, 96, 2, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (81, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (78, 96, 4, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 3, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 99)
		};

		private static readonly BamDataBlock[] _fight4Data = {
			new BamDataBlock (75, 96, 1, 187, 96, -23, 150, 45, 35, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 150, 45, 35, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 150, 45, 35, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -23, 150, 45, 35, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -23, 150, 45, 35, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -24, 150, 45, 35, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -24, 150, 45, 35, 0),
			new BamDataBlock (75, 96, 7, 187, 96, -24, 150, 45, 35, 0),
			new BamDataBlock (75, 96, 8, 187, 96, -25, 79, 101, 59, 0),
			new BamDataBlock (75, 96, 9, 187, 96, -25, 95, 104, 66, 0),
			new BamDataBlock (75, 96, 10, 187, 96, -25, 129, 104, 65, 0),
			new BamDataBlock (75, 96, 10, 187, 96, -25, 160, 104, 64, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -25, 179, 104, 63, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -23, 188, 104, 62, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -29, 191, 104, 36, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -29, 195, 104, 37, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -31, 202, 104, 38, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -32, 210, 104, 39, 0),
			new BamDataBlock (75, 96, 5, 187, 98, -32, 216, 104, 40, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -32, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 5, 187, 98, -32, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 5, 187, 97, -33, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -34, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -23, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -23, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -23, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -24, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -24, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -25, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -25, 223, 104, 42, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -26, 175, 98, 36, 0),
			new BamDataBlock (75, 96, 5, 187, 96, -26, 152, 98, 36, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -27, 124, 98, 37, 0),
			new BamDataBlock (75, 96, 6, 187, 96, -28, 105, 98, 38, 0),
			new BamDataBlock (75, 96, 11, 187, 96, -23, 77, 98, 39, 0),
			new BamDataBlock (75, 96, 13, 187, 96, -23, 63, 98, 40, 0),
			new BamDataBlock (75, 96, 14, 187, 96, -23, 51, 98, 41, 0),
			new BamDataBlock (75, 98, 14, 187, 96, -23, 51, 98, 42, 0),
			new BamDataBlock (75, 94, 14, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 98, 14, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 15, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
			new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 99)
		};

		public BamScene (QueenEngine vm)
		{
			_flag = BamFlags.F_STOP;
			_vm = vm;
			if (_vm.Resource.Platform == Platform.Amiga) {
				_fightData = _fight4Data;
			} else {
				_fightData = _fight1Data;
			}
		}

		public void UpdateCarAnimation ()
		{
			throw new NotImplementedException ();
		}


		public void UpdateFightAnimation ()
		{
			throw new NotImplementedException ();
		}
	}

}

