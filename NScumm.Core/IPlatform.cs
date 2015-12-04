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