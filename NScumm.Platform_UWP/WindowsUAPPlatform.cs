using System;
using NScumm.Core;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;

namespace NScumm
{
    public class WindowsUAPPlatform : IPlatform
    {
        public Assembly LoadAssembly(string dll)
        {
            var asmName = typeof(NScumm.Sky.SkyMetaEngine).AssemblyQualifiedName.Split(new[] { ',' }, 2)[1].Remove(0, 1);
            return Assembly.Load(new AssemblyName(asmName));
        }

        public async void Sleep(int timeInMs)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(timeInMs));
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