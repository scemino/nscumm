//
//  Cutaway.cs
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

namespace NScumm.Queen
{
	public class Cutaway
	{
		readonly QueenEngine _vm;

		public static void Run (string filename, string nextFilename, QueenEngine vm)
		{
			var cutaway = new Cutaway(filename, vm);
			cutaway.Run(nextFilename);
		}

		private Cutaway (string filename, QueenEngine vm)
		{
			_vm = vm;
			_vm.Input.CutawayQuitReset();
			Load(filename);
		}

		private void Run (string nextFilename)
		{
			throw new NotImplementedException ();
		}

		private void Load (string filename)
		{
			throw new NotImplementedException ();
		}
	}
}

