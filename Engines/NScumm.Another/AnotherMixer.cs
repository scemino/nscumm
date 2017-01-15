//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal class MixerChunk
    {
        public BytePtr Data;
        public ushort Len;
        public ushort LoopPos;
        public ushort LoopLen;
    }

    internal class MixerChannel
    {
        public bool Active;
        public byte Volume;
        public MixerChunk Chunk;
        public int ChunkPos;
        public int ChunkInc;

        public void SaveOrLoad(Serializer ser)
        {
            Entry[] entries =
            {
                Entry.Create(this, o => o.Active, 2),
                Entry.Create(this, o => o.Volume, 2),
                Entry.Create(this, o => o.ChunkPos, 2),
                Entry.Create(this, o => o.ChunkInc, 2),
                Entry.Create(Chunk, o => o.Data, 2),
                Entry.Create(Chunk, o => o.Len, 2),
                Entry.Create(Chunk, o => o.LoopPos, 2),
                Entry.Create(Chunk, o => o.LoopLen, 2),
            };
            ser.SaveOrLoadEntries(entries);
        }
    }

    internal class AnotherMixer : IDisposable
    {
        private const int AudioNumChannels = 4;
        private readonly IAnotherSystem _sys;
        private readonly object _mutex;

        // Since the virtual machine and the sound are running simultaneously in two different threads
        // any read or write to an elements of the sound channels MUST be synchronized with a
        // mutex.
        private readonly MixerChannel[] _channels = ScummHelper.CreateArray<MixerChannel>(AudioNumChannels);

        public AnotherMixer(IAnotherSystem sys)
        {
            _sys = sys;
            _mutex = new object();
        }

        public void Init()
        {
            _sys.StartAudio(MixCallback, this);
        }

        public void StopAll()
        {
            Debug(DebugLevels.DbgSnd, "Mixer::stopAll()");
            lock (_mutex)
            {
                for (var i = 0; i < AudioNumChannels; ++i)
                {
                    _channels[i].Active = false;
                }
            }
        }

        public void SetChannelVolume(byte channel, byte volume)
        {
            Debug(DebugLevels.DbgSnd, "Mixer::setChannelVolume({0}, {1})", channel, volume);
            //assert(channel < AUDIO_NUM_CHANNELS);
            lock (_mutex)
            {
                _channels[channel].Volume = volume;
            }
        }

        public void StopChannel(byte channel)
        {
            Debug(DebugLevels.DbgSnd, "Mixer::stopChannel({0})", channel);
            //assert(channel < AUDIO_NUM_CHANNELS);
            lock (_mutex)
            {
                _channels[channel].Active = false;
            }
        }

        public void PlayChannel(byte channel, MixerChunk mc, ushort freq, byte volume)
        {
            Debug(DebugLevels.DbgSnd, "Mixer::playChannel({0}, {1}, {2})", channel, freq, volume);
            //assert(channel < AUDIO_NUM_CHANNELS);

            // The mutex is acquired in the constructor
            lock (_mutex)
            {
                var ch = _channels[channel];
                ch.Active = true;
                ch.Volume = volume;
                ch.Chunk = mc;
                ch.ChunkPos = 0;
                ch.ChunkInc = (freq << 8) / _sys.OutputSampleRate;
            }
            //At the end of the scope the MutexStack destructor is called and the mutex is released.
        }

        private void Mix(BytePtr buf, int len)
        {
            lock (_mutex)
            {
                //Clear the buffer since nothing garanty we are receiving clean memory.
                Array.Clear(buf.Data, buf.Offset, len);

                for (var i = 0; i < AudioNumChannels; ++i)
                {
                    var ch = _channels[i];
                    if (!ch.Active)
                        continue;

                    SBytePtr pBuf = buf;
                    for (var j = 0; j < len; ++j, ++pBuf.Offset)
                    {
                        ushort p1, p2;
                        var ilc = (ushort) (ch.ChunkPos & 0xFF);
                        p1 = (ushort) (ch.ChunkPos >> 8);
                        ch.ChunkPos += ch.ChunkInc;

                        if (ch.Chunk.LoopLen != 0)
                        {
                            if (p1 == ch.Chunk.LoopPos + ch.Chunk.LoopLen - 1)
                            {
                                Debug(DebugLevels.DbgSnd, "Looping sample on channel {0}", i);
                                ch.ChunkPos = p2 = ch.Chunk.LoopPos;
                            }
                            else
                            {
                                p2 = (ushort) (p1 + 1);
                            }
                        }
                        else
                        {
                            if (p1 == ch.Chunk.Len - 1)
                            {
                                Debug(DebugLevels.DbgSnd, "Stopping sample on channel {0}", i);
                                ch.Active = false;
                                break;
                            }
                            p2 = (ushort) (p1 + 1);
                        }
                        // interpolate
                        var b1 = (sbyte) ch.Chunk.Data[p1];
                        var b2 = (sbyte) ch.Chunk.Data[p2];
                        var b = (sbyte) ((b1 * (0xFF - ilc) + b2 * ilc) >> 8);

                        // set volume and clamp
                        pBuf.Value = Addclamp(pBuf.Value, b * ch.Volume / 0x40); //0x40=64
                    }
                }
            }
        }

        private static void MixCallback(object param, BytePtr buf, int len)
        {
            ((AnotherMixer) param).Mix(buf, len);
        }

        private static sbyte Addclamp(int a, int b)
        {
            var add = a + b;
            if (add < -128)
            {
                add = -128;
            }
            else if (add > 127)
            {
                add = 127;
            }
            return (sbyte) add;
        }

        public void SaveOrLoad(Serializer ser)
        {
            lock (_mutex)
            {
                for (int i = 0; i < AudioNumChannels; ++i)
                {
                    var ch = _channels[i];
                    ch.SaveOrLoad(ser);
                }
            }
        }

        public void Dispose()
        {
            StopAll();
            _sys.StopAudio();
        }
    }
}