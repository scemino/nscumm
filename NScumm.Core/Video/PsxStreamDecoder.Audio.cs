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
using System.IO;
using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    partial class PsxStreamDecoder
    {
        class PsxAudioTrack : AudioTrack
        {
            class ADPCMStatus
            {
                public short[] sample = new short[2];
            }

            // Ha! It's palindromic!
            private const int AUDIO_DATA_CHUNK_SIZE = 2304;
            private const int AUDIO_DATA_SAMPLE_COUNT = 4032;

            private static readonly int[,] _xaTable = new int[5, 2]
            {
               {   0,   0 },
               {  60,   0 },
               { 115, -52 },
               {  98, -55 },
               { 122, -60 }
            };

            private ADPCMStatus[] _adpcmStatus;
            private bool _endOfTrack;
            private QueuingAudioStream _audStream;
            private IMixer _mixer;
            private byte[] _dst;
            private byte[] _buf;

            public override IAudioStream AudioStream => _audStream;

            public PsxAudioTrack(IMixer mixer, Stream sector)
            {
                _mixer = mixer;
                _endOfTrack = false;
                _adpcmStatus = new ADPCMStatus[2];
                for (int i = 0; i < _adpcmStatus.Length; i++)
                {
                    _adpcmStatus[i] = new ADPCMStatus();
                }

                sector.Seek(19, SeekOrigin.Begin);
                byte format = (byte)sector.ReadByte();
                bool stereo = (format & (1 << 0)) != 0;
                int rate = ((format & (1 << 2)) != 0) ? 18900 : 37800;
                _audStream = new QueuingAudioStream(rate, stereo);
                _dst = new byte[AUDIO_DATA_SAMPLE_COUNT * 2];
                _buf = new byte[AUDIO_DATA_CHUNK_SIZE];
            }

            public void Dispose()
            {
                _audStream.Dispose();
            }

            public override bool EndOfTrack => base.EndOfTrack && _endOfTrack;

            public void SetEndOfTrack()
            {
                _endOfTrack = true;
            }

            public void QueueAudioFromSector(Stream sector)
            {
                sector.Seek(24, SeekOrigin.Begin);

                // This XA audio is different (yet similar) from normal XA audio! Watch out!
                // TODO: It's probably similar enough to normal XA that we can merge it somehow...
                // TODO: RTZ PSX needs the same audio code in a regular AudioStream class. Probably
                // will do something similar to QuickTime and creating a base class 'ISOMode2Parser'
                // or something similar.
                sector.Read(_buf, 0, AUDIO_DATA_CHUNK_SIZE);

                int channels = _audStream.IsStereo ? 2 : 1;
                var dst = _dst;
                var buf = _buf;
                Array.Clear(_dst, 0, _dst.Length);
                var leftChannel = 0;
                var rightChannel = 1;

                for (var src = 0; src < AUDIO_DATA_CHUNK_SIZE; src += 128)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int shift = 12 - (buf[src + 4 + i * 2] & 0xf);
                        int filter = buf[src + 4 + i * 2] >> 4;
                        int f0 = _xaTable[filter, 0];
                        int f1 = _xaTable[filter, 1];
                        int s_1 = _adpcmStatus[0].sample[0];
                        int s_2 = _adpcmStatus[0].sample[1];

                        for (int j = 0; j < 28; j++)
                        {
                            byte d = buf[src + 16 + i + j * 4];
                            int t = (sbyte)(d << 4) >> 4;
                            int s = (t << shift) + ((s_1 * f0 + s_2 * f1 + 32) >> 6);
                            s_2 = s_1;
                            s_1 = (short)ScummHelper.Clip(s, short.MinValue, short.MaxValue);
                            dst.WriteInt16(leftChannel * 2, (short)s_1);
                            leftChannel += channels;
                        }

                        if (channels == 2)
                        {
                            _adpcmStatus[0].sample[0] = (short)s_1;
                            _adpcmStatus[0].sample[1] = (short)s_2;
                            s_1 = _adpcmStatus[1].sample[0];
                            s_2 = _adpcmStatus[1].sample[1];
                        }

                        shift = 12 - (buf[src + 5 + i * 2] & 0xf);
                        filter = buf[src + 5 + i * 2] >> 4;
                        f0 = _xaTable[filter, 0];
                        f1 = _xaTable[filter, 1];

                        for (int j = 0; j < 28; j++)
                        {
                            var d = buf[src + 16 + i + j * 4];
                            int t = (sbyte)d >> 4;
                            int s = (t << shift) + ((s_1 * f0 + s_2 * f1 + 32) >> 6);
                            s_2 = s_1;
                            s_1 = (short)ScummHelper.Clip(s, short.MinValue, short.MaxValue);

                            if (channels == 2)
                            {
                                dst.WriteInt16(rightChannel * 2, (short)s_1);
                                rightChannel += 2;
                            }
                            else {
                                dst.WriteInt16(leftChannel * 2, (short)s_1);
                                leftChannel++;
                            }
                        }

                        if (channels == 2)
                        {
                            _adpcmStatus[1].sample[0] = (short)s_1;
                            _adpcmStatus[1].sample[1] = (short)s_2;
                        }
                        else {
                            _adpcmStatus[0].sample[0] = (short)s_1;
                            _adpcmStatus[0].sample[1] = (short)s_2;
                        }
                    }
                }

                var flags = AudioFlags.Is16Bits;

                if (_audStream.IsStereo)
                    flags |= AudioFlags.Stereo;

                if (BitConverter.IsLittleEndian)
                {
                    flags |= AudioFlags.LittleEndian;
                }

                _audStream.QueueBuffer(dst, AUDIO_DATA_SAMPLE_COUNT * 2, true, flags);
            }            
        }
    }
}
