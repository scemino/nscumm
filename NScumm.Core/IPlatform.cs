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

namespace NScumm.Core
{
    public interface IPlatform
    {
        void Sleep(int timeInMs);

        int SizeOf(Type type);

        byte[] FromStructure(object obj);
        object ToStructure(byte[] data, int offset, Type type);
        IWrappedObject WriteStructure(byte[] data, int offset, Type type);
    }

    public static class PlatformExtension
    {
        public static int SizeOf<T>(this IPlatform platform)
        {
            return platform.SizeOf(typeof(T));
        }

        public static T ToStructure<T>(this IPlatform platform, byte[] data, int offset)
        {
            return (T)platform.ToStructure(data, offset, typeof(T));
        }

        public static IWrappedObject<T> WriteStructure<T>(this IPlatform platform, byte[] data, int offset)
        {
            return new WrappedObject<T>(platform.WriteStructure(data, offset, typeof(T)));
        }

        public static void WriteStructure<T>(this IPlatform platform, byte[] data, int offset, Action<T> action)
        {
            using (var obj = new WrappedObject<T>(platform.WriteStructure(data, offset, typeof(T))))
            {
                action(obj.Object);
            }
        }
    }

    public interface IWrappedObject : IDisposable
    {
        object Object { get; }
    }

    public interface IWrappedObject<T> : IDisposable
    {
        T Object { get; }
    }

    public class WrappedObject<T> : IWrappedObject<T>
    {
        private IWrappedObject _wrap;

        public WrappedObject(IWrappedObject wrap)
        {
            _wrap = wrap;
        }

        public T Object
        {
            get
            {
                return (T)_wrap.Object;
            }
        }

        public void Dispose()
        {
            _wrap.Dispose();
        }
    }
}