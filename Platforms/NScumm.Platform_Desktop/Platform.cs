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
    class WrappedObject : IWrappedObject
    {
        private GCHandle _handle;

        public WrappedObject(GCHandle handle, object obj)
        {
            _handle = handle;
            Object = obj;
        }

        public object Object
        {
            get; private set;
        }

        public void Dispose()
        {
            Marshal.StructureToPtr(Object, _handle.AddrOfPinnedObject(), false);
            _handle.Free();
        }
    }

    public class Platform : IPlatform
    {
        public Assembly LoadAssembly(string dll)
        {
            return Assembly.LoadFile(dll);
        }

        public void Sleep(int timeInMs)
        {
            Thread.Sleep(timeInMs);
        }

        public int SizeOf(Type type)
        {
            return Marshal.SizeOf(type);
        }

        public byte[] FromStructure(object obj)
        {
            var size = Marshal.SizeOf(obj);
            var data = new byte[size];
            var handle = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(obj, handle, true);
            }
            finally
            {
                Marshal.Copy(handle, data, 0, size);
                Marshal.FreeHGlobal(handle);
            }
            return data;
        }

        public object ToStructure(byte[] data, int offset, Type type)
        {
            object obj;
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                obj = Marshal.PtrToStructure(handle.AddrOfPinnedObject() + offset, type);
            }
            finally
            {
                handle.Free();
            }
            return obj;
        }

        public IWrappedObject WriteStructure(byte[] data, int offset, Type type)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            return new WrappedObject(handle, Marshal.PtrToStructure(handle.AddrOfPinnedObject() + offset, type));
        }

        public void Debug(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}

