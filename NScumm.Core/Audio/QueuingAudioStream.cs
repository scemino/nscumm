//
//  QueuingAudioStream.cs
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
using NScumm.Core.Audio;
using System.Diagnostics;
using System.Collections.Generic;
using NScumm.Core.Audio.Decoders;
using System.IO;

namespace NScumm.Core
{
    interface IQueuingAudioStream : IAudioStream
    {

        /// <summary>
        /// Queue an audio stream for playback. This stream plays all queued
        /// streams, in the order they were queued. If disposeAfterUse is set to
        /// DisposeAfterUse::YES, then the queued stream is deleted after all data
        /// contained in it has been played.
        /// </summary>
        void QueueAudioStream(IAudioStream audStream, bool disposeAfterUse = true);

        /// <summary>
        /// Queue a block of raw audio data for playback. This stream plays all
        /// queued block, in the order they were queued. If disposeAfterUse is set
        /// to DisposeAfterUse::YES, then the queued block is released using free()
        /// after all data contained in it has been played.
        ///
        /// @note Make sure to allocate the data block with malloc(), not with new[].
        ///
        /// @param data             pointer to the audio data block
        /// @param size             length of the audio data block
        /// @param disposeAfterUse  if equal to DisposeAfterUse::YES, the block is released using free() after use.
        /// @param flags            a bit-ORed combination of RawFlags describing the audio data format
        /// </summary>
        void QueueBuffer(byte[] data, int size, bool disposeAfterUse, AudioFlags flags);


        /// <summary>
        /// Mark this stream as finished. That is, signal that no further data
        /// will be queued to it. Only after this has been done can this
        /// stream ever 'end'.
        /// </summary>
        void Finish();

        /// <summary>
        /// Return the number of streams still queued for playback (including
        /// the currently playing stream).
        /// </summary>
        int NumQueuedStreams();
    }

    public class QueuingAudioStream : IQueuingAudioStream
    {
        struct StreamHolder
        {
            public IAudioStream Stream { get; private set; }

            public bool DisposeAfterUse { get; private set; }

            public StreamHolder(IAudioStream stream, bool disposeAfterUse)
                : this()
            {
                Stream = stream;
                DisposeAfterUse = disposeAfterUse;
            }
        }

        public QueuingAudioStream(int rate, bool stereo)
        {
            _rate = rate;
            _stereo = stereo;
        }

        public void QueueAudioStream(IAudioStream stream, bool disposeAfterUse)
        {
            Debug.Assert(!_finished);
            if ((stream.Rate != Rate) || (stream.IsStereo != IsStereo))
                throw new NotSupportedException("QueuingAudioStreamImpl::queueAudioStream: stream has mismatched parameters");

            lock (_mutex)
            {
                _queue.Enqueue(new StreamHolder(stream, disposeAfterUse));
            }
        }

        public void QueueBuffer(byte[] data, int size, bool disposeAfterUse, AudioFlags flags)
        {
            var stream = new RawStream(flags, Rate, disposeAfterUse, new MemoryStream(data, 0, size));
            QueueAudioStream(stream, true);
        }

        public void Finish()
        {
            _finished = true;
        }

        public int NumQueuedStreams()
        {
            return _queue.Count;
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            int samplesDecoded = 0;
            lock (_mutex)
            {
                while (samplesDecoded < buffer.Length && _queue.Count != 0)
                {
                    var stream = _queue.Peek().Stream;
                    var buf = new short[count - samplesDecoded];
                    var read = stream.ReadBuffer(buf, count - samplesDecoded);
                    Array.Copy(buf, 0, buffer, samplesDecoded, read);
                    samplesDecoded += read;

                    if (stream.IsEndOfData)
                    {
                        var tmp = _queue.Dequeue();
                        if (tmp.DisposeAfterUse)
                            stream.Dispose();
                    }
                }
            }
            return samplesDecoded;
        }

        public bool IsStereo
        {
            get { return _stereo; }
        }

        public int Rate
        {
            get
            {
                return _rate;
            }
        }

        public bool IsEndOfData
        {
            get
            {
                return _queue.Count == 0;
            }
        }

        public bool IsEndOfStream
        {
            get { return _finished && _queue.Count == 0; }
        }

        public void Dispose()
        {
            while (_queue.Count > 0)
            {
                var tmp = _queue.Dequeue();
                if (tmp.DisposeAfterUse)
                    tmp.Stream.Dispose();
            }
        }

        int _rate;
        bool _stereo;
        bool _finished;
        object _mutex = new object();
        Queue<StreamHolder> _queue = new Queue<StreamHolder>();
    }
}
