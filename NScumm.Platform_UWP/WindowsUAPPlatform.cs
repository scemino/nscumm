using System;
using NScumm.Core;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
using NScumm.Sky;

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

    public class WindowsUAPPlatform : IPlatform
    {
        public IWrappedObject WriteStructure(byte[] data, int offset, Type type)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            return new WrappedObject(handle, Marshal.PtrToStructure(handle.AddrOfPinnedObject() + offset, type));
        }

        public Assembly LoadAssembly(string dll)
        {
            var asmName = typeof(SkyMetaEngine).AssemblyQualifiedName.Split(new[] { ',' }, 2)[1].Remove(0, 1);
            return Assembly.Load(new AssemblyName(asmName));
        }

        public async void Sleep(int timeInMs)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(timeInMs));
        }

        public int SizeOf(Type type)
        {
            return Marshal.SizeOf(type);
        }

        public byte[] FromStructure(object obj)
        {
            IntPtr ptr = IntPtr.Zero;
            var size = Marshal.SizeOf(obj.GetType());
            byte[] data = new byte[size];
            try
            {

                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, true);
                Marshal.Copy(ptr, data, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return data;
        }

        public object ToStructure(byte[] data, int offset, Type type)
        {
            object obj;
            var size = Math.Max(Marshal.SizeOf(type), data.Length);
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, offset, ptr, data.Length - offset);
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