//
//  IEnableTrace.cs
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

namespace NScumm.Core
{
    public interface IEnableTrace
    {
    }

    public interface ITrace
    {
        void Write(string traceName, string format, params object[] args);
    }

    class NullTrace: ITrace
    {
        public void Write(string traceName, string format, params object[] args)
        {
        }
    }

    public static class EnableTraceExtension
    {
        public static ITrace Trace(this IEnableTrace trace)
        {
            if (ServiceLocator.TraceFatory != null)
                return ServiceLocator.TraceFatory.CreateTrace(trace) ?? new NullTrace();
            return new NullTrace();
        }
    }
}

