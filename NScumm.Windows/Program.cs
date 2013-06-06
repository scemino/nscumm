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

#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using NScumm.Core;


#endregion

namespace NScumm.Windows
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
			// load game
			var cmd = Environment.CommandLine.Split(new[] { "\"" }, StringSplitOptions.RemoveEmptyEntries);
			var filename = cmd[2];
			var info = GameManager.GetInfo(filename);

			using (var game = new ScummGame(info)){
				game.Run();
			}
        }
    }
#endif
}
