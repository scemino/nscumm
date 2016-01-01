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


namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private static Register kGetSaveDir(EngineState s, int argc, StackPtr argv)
        {
# if ENABLE_SCI32
            // SCI32 uses a parameter here. It is used to modify a string, stored in a
            // global variable, so that game scripts store the save directory. We
            // don't really set a save game directory, thus not setting the string to
            // anything is the correct thing to do here.
            //if (argc > 0)
            //	warning("kGetSaveDir called with %d parameter(s): %04x:%04x", argc, PRINT_REG(argv[0]));
#endif
            return s._segMan.SaveDirPtr;
        }
    }
}
