//
//  SubLoopingAudioStream.cs
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

namespace NScumm.Core.Audio.Decoders
{
    /// <summary>
    /// A looping audio stream, which features looping of a nested part of the
    /// stream.
    /// </summary>
    /// <remarks>
    /// Currently this implementation stops after the nested loop finished
    /// playback.
    /// IMPORTANT:
    /// This might be merged with SubSeekableAudioStream for playback purposes.
    /// (After extending it to accept a start time).
    /// </remarks>
    public class SubLoopingAudioStream: IAudioStream
    {
        public SubLoopingAudioStream(ISeekableAudioStream stream, uint loops,
                                     Timestamp loopStart,
                                     Timestamp loopEnd,
                                     bool disposeAfterUse = true)
        {
            _parent = stream;
            _loops = loops;
            _pos = new Timestamp(0, Rate * (IsStereo ? 2 : 1));
            _loopStart = AudioStreamHelper.ConvertTimeToStreamPos(loopStart, Rate, IsStereo);
            _loopEnd = AudioStreamHelper.ConvertTimeToStreamPos(loopEnd, Rate, IsStereo);
            _done = false;
            Debug.Assert(loopStart < loopEnd);

            if (!_parent.Rewind())
                _done = true;
        }

        ~SubLoopingAudioStream()
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
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            if (_done)
                return 0;

            var numSamples = count;
            int framesLeft = Math.Min(_loopEnd.FrameDiff(_pos), numSamples);
            var tmp = new short[framesLeft];
            int framesRead = _parent.ReadBuffer(tmp, framesLeft);
            Array.Copy(tmp, 0, buffer, 0, tmp.Length);
            _pos = _pos.AddFrames(framesRead);

            if (framesRead < framesLeft && _parent.IsEndOfStream)
            {
                // TODO: Proper error indication.
                _done = true;
                return framesRead;
            }
            else if (_pos == _loopEnd)
            {
                if (_loops != 0)
                {
                    --_loops;
                    if (_loops == 0)
                    {
                        _done = true;
                        return framesRead;
                    }
                }

                if (!_parent.Seek(_loopStart))
                {
                    // TODO: Proper error indication.
                    _done = true;
                    return framesRead;
                }

                _pos = _loopStart;
                framesLeft = numSamples - framesLeft;
                tmp = new short[framesLeft];
                var numRead = ReadBuffer(tmp, framesLeft);
                Array.Copy(tmp, 0, buffer, framesRead, tmp.Length);
                return framesRead + numRead;
            }
            else
            {
                return framesRead;
            }
        }

        // We're out of data if this stream is finished or the parent
        // has run out of data for now.
        public bool IsEndOfData { get { return _done || _parent.IsEndOfData; } }

        // The end of the stream has been reached only when we've gone
        // through all the iterations.
        public bool IsEndOfStream { get { return _done; } }

        public bool IsStereo { get { return _parent.IsStereo; } }

        public int Rate { get { return _parent.Rate; } }

        ISeekableAudioStream _parent;

        uint _loops;
        Timestamp _pos;
        Timestamp _loopStart, _loopEnd;

        bool _done;
    }
}

