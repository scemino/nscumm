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
using System.IO;
using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    class SmackerAudioTrack : AudioTrack
    {
        private AudioInfo _audioInfo;
        private QueuingAudioStream _audioStream;
        public override IAudioStream AudioStream => _audioStream;

        public override SoundType SoundType { get; }
        public override bool IsRewindable => true;

        public SmackerAudioTrack(AudioInfo audioInfo, SoundType soundType)
        {
            _audioInfo = audioInfo;
            SoundType = soundType;
            _audioStream = new QueuingAudioStream((int)_audioInfo.sampleRate, _audioInfo.isStereo);
        }

        public override bool Rewind()
        {
            _audioStream.Dispose();
            _audioStream = new QueuingAudioStream((int)_audioInfo.sampleRate, _audioInfo.isStereo);
            return true;
        }

        public void QueueCompressedBuffer(byte[] buffer, int bufferSize, int unpackedSize)
        {
            using (var ms = new MemoryStream(buffer, 0, bufferSize))
            {
                var audioBS = BitStream.Create8Lsb(ms);

                bool dataPresent = audioBS.GetBit() != 0;

                if (!dataPresent)
                    return;

                bool isStereo = audioBS.GetBit() != 0;
                Debug.Assert(isStereo == _audioInfo.isStereo);
                bool is16Bits = audioBS.GetBit() != 0;
                Debug.Assert(is16Bits == _audioInfo.is16Bits);

                int numBytes = 1 * (isStereo ? 2 : 1) * (is16Bits ? 2 : 1);

                byte[] unpackedBuffer = new byte[unpackedSize];
                var curPointer = 0;
                var curPos = 0;

                SmallHuffmanTree[] audioTrees = new SmallHuffmanTree[4];
                for (int k = 0; k < numBytes; k++)
                    audioTrees[k] = new SmallHuffmanTree(audioBS);

                // Base values, stored as big endian

                int[] bases = new int[2];

                if (isStereo)
                {
                    if (is16Bits)
                    {
                        bases[1] = ScummHelper.SwapBytes((ushort)audioBS.GetBits(16));
                    }
                    else
                    {
                        bases[1] = (int)audioBS.GetBits(8);
                    }
                }

                if (is16Bits)
                {
                    bases[0] = ScummHelper.SwapBytes((ushort)audioBS.GetBits(16));
                }
                else
                {
                    bases[0] = (int)audioBS.GetBits(8);
                }

                // The bases are the first samples, too
                for (int i = 0;
                    i < (isStereo ? 2 : 1);
                    i++, curPointer += (is16Bits ? 2 : 1), curPos += (is16Bits ? 2 : 1))
                {
                    if (is16Bits)
                        unpackedBuffer.WriteUInt16BigEndian(curPointer, (ushort)bases[i]);
                    else
                        unpackedBuffer[curPointer] = (byte)((bases[i] & 0xFF) ^ 0x80);
                }

                // Next follow the deltas, which are added to the corresponding base values and
                // are stored as little endian
                // We store the unpacked bytes in big endian format

                while (curPos < unpackedSize)
                {
                    // If the sample is stereo, the data is stored for the left and right channel, respectively
                    // (the exact opposite to the base values)
                    if (!is16Bits)
                    {
                        for (int k = 0; k < (isStereo ? 2 : 1); k++)
                        {
                            sbyte delta = (sbyte)((short)audioTrees[k].GetCode(audioBS));
                            bases[k] = (bases[k] + delta) & 0xFF;
                            unpackedBuffer[curPointer++] = (byte)(bases[k] ^ 0x80);
                            curPos++;
                        }
                    }
                    else
                    {
                        for (int k = 0; k < (isStereo ? 2 : 1); k++)
                        {
                            byte lo = (byte)audioTrees[k * 2].GetCode(audioBS);
                            byte hi = (byte)audioTrees[k * 2 + 1].GetCode(audioBS);
                            bases[k] += (short)(lo | (hi << 8));

                            unpackedBuffer.WriteUInt16BigEndian(curPointer, (ushort)bases[k]);
                            curPointer += 2;
                            curPos += 2;
                        }
                    }

                }

                QueuePCM(unpackedBuffer, unpackedSize);
            }
        }

        public void QueuePCM(byte[] buffer, int bufferSize)
        {
            AudioFlags flags = AudioFlags.None;
            if (_audioInfo.is16Bits)
                flags |= AudioFlags.Is16Bits;
            if (_audioInfo.isStereo)
                flags |= AudioFlags.Stereo;

            _audioStream.QueueBuffer(buffer, bufferSize, true, flags);
        }
    }
}