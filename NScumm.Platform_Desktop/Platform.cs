//
//  Platform.cs
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
using System.Threading;
using NScumm.Core;
using System.Runtime.InteropServices;
using System.Reflection;

namespace NScumm
{
    public class Platform: IPlatform
    {
        public Assembly LoadAssembly(string dll)
        {
            return Assembly.LoadFile(dll);
        }

        public void Sleep(int timeInMs)
        {
            Thread.Sleep(timeInMs);
        }

        public object ToStructure(byte[] data, int offset, Type type)
        {
            object obj;
            var size = Marshal.SizeOf(type);
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, offset, ptr, size);
                obj = Marshal.PtrToStructure(ptr, type);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return obj;
        }
    }
}

