using NScumm.Core.Audio;
using System.IO;
using System;
using NScumm.Core;

namespace NScumm.MonoGame
{
    class WaveAudioStream : IRewindableAudioStream
    {
        private byte[] _buffer;
        private Stream _stream;

        public WaveAudioStream(Stream stream)
        {
            _stream = stream;
            var br = new BinaryReader(_stream);
            br.ReadBytes(22);
            var channels = br.ReadInt16();
            IsStereo = channels == 2;
            Rate = br.ReadInt32();
            var averageBytesPerSecond = br.ReadInt32();
            var blockAlign = br.ReadInt16();
            var bitsPerSample = br.ReadInt16();
            br.ReadBytes(10);
            _buffer = new byte[4096];
        }

        public bool IsEndOfData
        {
            get
            {
                return _stream.Position >= _stream.Length;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                return IsEndOfData;
            }
        }

        public bool IsStereo
        {
            get; private set;
        }

        public int Rate
        {
            get; private set;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public int ReadBuffer(short[] buffer, int numSamples)
        {
            int numRead = 0;
            int read;
            do
            {
                var len = Math.Min(_buffer.Length, numSamples * 2);
                read = _stream.Read(_buffer, 0, len);
                var offs = 0;
                for (int i = 0; i < read; i += 2)
                {
                    buffer[offs++] = _buffer.ToInt16(i);
                }
                numSamples -= (read / 2);
                numRead += (read / 2);
            } while (read != 0 && numSamples != 0);
            return numRead;
        }

        public bool Rewind()
        {
            _stream.Position = 0;
            return true;
        }
    }

}
