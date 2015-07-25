//
//  LoopingAudioStream.cs
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
using System.Diagnostics;

namespace NScumm.Core.Audio
{
    public class LoopingAudioStream: IAudioStream
    {
        public bool IsStereo
        {
            get{ return _parent.IsStereo; }
        }

        public int Rate
        {
            get{ return _parent.Rate; }
        }

        public bool IsEndOfData
        {
            get
            {
                return (_loops != 0 && _completeIterations == _loops) || _parent.IsEndOfData;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                return _loops != 0 && _completeIterations == _loops;
            }
        }

        public static LoopingAudioStream Create(ISeekableAudioStream stream, Timestamp start, Timestamp end, int loops)
        {
            if (start.TotalNumberOfFrames == 0 && (end.TotalNumberOfFrames == 0 || end == stream.Length))
            {
                return new LoopingAudioStream(stream, loops);
            }
            else
            {
                if (end.TotalNumberOfFrames == 0)
                    end = stream.Length;

                if (start >= end)
                {
                    Debug.WriteLine("makeLoopingAudioStream: start ({0}) >= end ({1})", start.Milliseconds, end.Milliseconds);
                    stream.Dispose();
                    return null;
                }

                return new LoopingAudioStream(new SubSeekableAudioStream(stream, start, end), loops);
            }
        }

        public LoopingAudioStream(IRewindableAudioStream stream, int loops, bool disposeAfterUse = true)
        {
            _parent = stream;
            _loops = loops;
            _disposeAfterUse = disposeAfterUse;

            if (!stream.Rewind())
            {
                // TODO: Properly indicate error
                _loops = _completeIterations = 1;
            }
            if (stream.IsEndOfStream)
            {
                // Apparently this is an empty stream
                _loops = _completeIterations = 1;
            }
        }

        ~LoopingAudioStream()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposeAfterUse && _parent != null)
            {
                _parent.Dispose();
                _parent = null;
            }
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            if ((_loops != 0 && _completeIterations == _loops) || count == 0)
                return 0;

            int samplesRead = _parent.ReadBuffer(buffer, count);

            if (_parent.IsEndOfStream)
            {
                ++_completeIterations;
                if (_completeIterations == _loops)
                    return samplesRead;

                int remainingSamples = count - samplesRead;

                if (!_parent.Rewind())
                {
                    // TODO: Properly indicate error
                    _loops = _completeIterations = 1;
                    return samplesRead;
                }
                if (_parent.IsEndOfStream)
                {
                    // Apparently this is an empty stream
                    _loops = _completeIterations = 1;
                }

                var tmp = new short[remainingSamples];
                var read = ReadBuffer(tmp, remainingSamples);
                Array.Copy(tmp, 0, buffer, samplesRead, remainingSamples);
                return samplesRead + read;
            }

            return samplesRead;
        }

        IRewindableAudioStream _parent;
        bool _disposeAfterUse;
        int _loops;
        int _completeIterations;
    }
}

