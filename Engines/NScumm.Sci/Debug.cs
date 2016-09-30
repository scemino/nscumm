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

using NScumm.Sci.Engine;
using System;
using System.Collections.Generic;

namespace NScumm.Sci
{
    // These types are used both as identifiers and as elements of bitfields
    [Flags]
    internal enum BreakpointType
    {
        /// <summary>
        /// Break when a selector is executed. Data contains (char *) selector name
        /// (in the format Object::Method)
        /// </summary>
        BREAK_SELECTOREXEC = 1 << 0, // break when a function selector is executed
        BREAK_SELECTORREAD = 1 << 1, // break when a variable selector is read
        BREAK_SELECTORWRITE = 1 << 2, // break when a variable selector is written

        /// <summary>
        /// Break when an exported function is called. Data contains
        /// script_no &lt;&lt; 16 | export_no.
        /// </summary>
        BREAK_EXPORT = 1 << 3
    }

    internal struct Breakpoint
    {
        public BreakpointType type;
        /// <summary>
        /// Breakpoints on exports
        /// </summary>
        public uint address;
        /// <summary>
        /// Breakpoints on selector names
        /// </summary>
        public string name;
    }

    internal enum DebugSeeking
    {
        Nothing = 0,
        Callk = 1,        // Step forward until callk is found
        LevelRet = 2,     // Step forward until returned from this level
        SpecialCallk = 3, // Step forward until a /special/ callk is found
        Global = 4,       // Step forward until one specified global variable is modified
        StepOver = 5      // Step forward until we reach same stack-level again
    }

    internal class DebugState
    {
        public bool debugging;
        public bool breakpointWasHit;
        public bool stopOnEvent;
        public DebugSeeking seeking;       // Stepping forward until some special condition is met
        public int runningStep;            // Set to > 0 to allow multiple stepping
        public int seekLevel;              // Used for seekers that want to check their exec stack depth
        public int seekSpecial;            // Used for special seeks
        public int old_pc_offset;
        public StackPtr old_sp;
        public List<Breakpoint> _breakpoints;   //< List of breakpoints
        public int _activeBreakpointTypes;  //< Bit mask specifying which types of breakpoints are active
    }
}
