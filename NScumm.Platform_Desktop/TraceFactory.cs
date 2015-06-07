//
//  TraceFatory.cs
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

using System;
using NScumm.Core;
using System.Collections.Generic;

namespace NScumm
{
    class Trace: ITrace
    {
        readonly IEnableTrace _source;
        readonly HashSet<string> _switches;

        public Trace(IEnableTrace trace, IEnumerable<string> switches)
        {
            _source = trace;
            _switches = new HashSet<string>(switches, StringComparer.OrdinalIgnoreCase);
        }

        public void Write(string traceName, string format, params object[] args)
        {
            if (_switches.Contains(traceName))
            {
                Console.WriteLine("{0,-16} {1}", _source.GetType().Name, string.Format(format, args));
            }
        }
    }

    public class TraceFactory: ITraceFactory
    {
        readonly IEnumerable<string> _switches;

        public TraceFactory(IEnumerable<string> switches)
        {
            _switches = switches;
        }

        public ITrace CreateTrace(IEnableTrace trace)
        {
            return new Trace(trace, _switches);
        }
    }
}
