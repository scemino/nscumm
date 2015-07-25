//
//  SubSeekableAudioStream.cs
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
using NScumm.Core.Audio.Decoders;
using System.Diagnostics;

namespace NScumm.Core.Audio
{
    public class SubSeekableAudioStream: ISeekableAudioStream
    {
        public Timestamp Length
        {
            get
            {
                return _length;
            }
        }

        public bool IsStereo
        {
            get
            {
                return _parent.IsStereo;
            }
        }

        public int Rate
        {
            get
            {
                return _parent.Rate;
            }
        }

        public bool IsEndOfData
        {
            get
            {
                return (_pos >= _length) || _parent.IsEndOfData;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                return (_pos >= _length) || _parent.IsEndOfStream;
            }
        }


        public SubSeekableAudioStream(ISeekableAudioStream parent, Timestamp start, Timestamp end, bool disposeAfterUse = true)
        {
            _parent = parent;
            _disposeAfterUse = disposeAfterUse;
            _start = AudioStreamHelper.ConvertTimeToStreamPos(start, Rate, IsStereo);
            _pos = new Timestamp(0, Rate * (IsStereo ? 2 : 1));
            _length = AudioStreamHelper.ConvertTimeToStreamPos(end, Rate, IsStereo) - _start;


            Debug.Assert(_length.TotalNumberOfFrames % (IsStereo ? 2 : 1) == 0);
            _parent.Seek(_start);
        }

        ~SubSeekableAudioStream()
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

        public bool Rewind()
        {
            return Seek(new Timestamp(0, Rate));
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            int framesLeft = Math.Min(_length.FrameDiff(_pos), count);
            var tmp = new short[framesLeft];
            int framesRead = _parent.ReadBuffer(tmp, framesLeft);
            Array.Copy(tmp, buffer, framesLeft);
            _pos = _pos.AddFrames(framesRead);
            return framesRead;
        }

        public bool Seek(Timestamp where)
        {
            _pos = AudioStreamHelper.ConvertTimeToStreamPos(where, Rate, IsStereo);
            if (_pos > _length)
            {
                _pos = _length;
                return false;
            }

            if (_parent.Seek(_pos + _start))
            {
                return true;
            }
            else
            {
                _pos = _length;
                return false;
            }
        }

        ISeekableAudioStream _parent;
        bool _disposeAfterUse;
        readonly Timestamp _start;
        readonly Timestamp _length;
        Timestamp _pos;
    }
}

