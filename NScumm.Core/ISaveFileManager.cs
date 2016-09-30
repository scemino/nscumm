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

using System.IO;

namespace NScumm.Core
{
    public interface ISaveFileManager
    {
        /// <summary>
        /// Open the file with the specified name in the given directory for loading.
        /// </summary>
        /// <returns>A stream, or null if an error occurred.</returns>
        /// <param name="fileName">The name of the savefile.</param>
        Stream OpenForLoading(string fileName);

        /// <summary>
        /// Open the savefile with the specified name in the given directory for
        /// saving.
        /// </summary>
        /// <returns>The for saving.</returns>
        /// <param name="fileName">The name of the savefile.</param>
        /// <param name="compress">toggles whether to compress the resulting save file 
        /// (default) or not.</param>
        Stream OpenForSaving(string fileName, bool compress = true);

        /// <summary>
        /// Request a list of available savegames with a given DOS-style pattern,
        /// also known as "glob" in the POSIX world. Refer to the Common::matchString()
        /// function to learn about the precise pattern format.
        /// </summary>
        /// <returns>List of strings for all present file names.</returns>
        /// <param name="pattern">Pattern to match. Wildcards like * or ? are available.</param>
        string[] ListSavefiles(string pattern);

        /// <summary>
        /// Removes the given savefile from the system.
        /// </summary>
        /// <param name="name">Name the name of the savefile to be removed..</param>
        bool RemoveSavefile(string name);

        bool RenameSavefile(string oldName, string newName);
    }
}
