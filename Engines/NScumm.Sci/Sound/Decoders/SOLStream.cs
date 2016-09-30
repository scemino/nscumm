//
//  SOLStream.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using NScumm.Core.Common;

namespace NScumm.Sci.Sound.Decoders
{
    [Flags]
    internal enum SolFlags
    {
        Compressed = 1,
        Is16Bit = 4,
        Stereo = 16
    }

    internal class SolStream: ISeekableAudioStream
    {
        public bool IsStereo { get; }
        public int Rate => _sampleRate;
        public bool IsEndOfData => _stream.Value.Position >= _dataOffset + _rawDataSize;
        public bool IsEndOfStream => IsEndOfData;
        public Timestamp Length { get; }

        /// <summary>
        /// Read stream containing possibly-compressed SOL audio.
        /// </summary>
        private readonly DisposablePtr<Stream> _stream;
        /// <summary>
        /// Start offset of the audio data in the read stream.
        /// </summary>
        private readonly int _dataOffset;
        /// <summary>
        /// Sample rate of audio data.
        /// </summary>
        private readonly ushort _sampleRate;
        /// <summary>
        /// The raw (possibly-compressed) size of audio data in
        /// the stream.
        /// </summary>
        private readonly int _rawDataSize;

        /// <summary>
        /// The last sample from the previous DPCM decode.
        /// </summary>
        private short _dpcmCarry16;

        private byte _dpcmCarry8;

        private readonly bool _is16Bit;

        // Note that the 16-bit version is also used in coktelvideo.cpp
        private static readonly ushort[] TableDpcm16 = {
            0x0000, 0x0008, 0x0010, 0x0020, 0x0030, 0x0040, 0x0050, 0x0060, 0x0070, 0x0080,
            0x0090, 0x00A0, 0x00B0, 0x00C0, 0x00D0, 0x00E0, 0x00F0, 0x0100, 0x0110, 0x0120,
            0x0130, 0x0140, 0x0150, 0x0160, 0x0170, 0x0180, 0x0190, 0x01A0, 0x01B0, 0x01C0,
            0x01D0, 0x01E0, 0x01F0, 0x0200, 0x0208, 0x0210, 0x0218, 0x0220, 0x0228, 0x0230,
            0x0238, 0x0240, 0x0248, 0x0250, 0x0258, 0x0260, 0x0268, 0x0270, 0x0278, 0x0280,
            0x0288, 0x0290, 0x0298, 0x02A0, 0x02A8, 0x02B0, 0x02B8, 0x02C0, 0x02C8, 0x02D0,
            0x02D8, 0x02E0, 0x02E8, 0x02F0, 0x02F8, 0x0300, 0x0308, 0x0310, 0x0318, 0x0320,
            0x0328, 0x0330, 0x0338, 0x0340, 0x0348, 0x0350, 0x0358, 0x0360, 0x0368, 0x0370,
            0x0378, 0x0380, 0x0388, 0x0390, 0x0398, 0x03A0, 0x03A8, 0x03B0, 0x03B8, 0x03C0,
            0x03C8, 0x03D0, 0x03D8, 0x03E0, 0x03E8, 0x03F0, 0x03F8, 0x0400, 0x0440, 0x0480,
            0x04C0, 0x0500, 0x0540, 0x0580, 0x05C0, 0x0600, 0x0640, 0x0680, 0x06C0, 0x0700,
            0x0740, 0x0780, 0x07C0, 0x0800, 0x0900, 0x0A00, 0x0B00, 0x0C00, 0x0D00, 0x0E00,
            0x0F00, 0x1000, 0x1400, 0x1800, 0x1C00, 0x2000, 0x3000, 0x4000
        };

        private static readonly byte[] TableDpcm8 = { 0, 1, 2, 3, 6, 10, 15, 21 };

        public SolStream(bool isStereo, bool is16Bit, Stream stream, bool disposeAfterUse, int dataOffset, ushort sampleRate, int rawDataSize)
        {
            _stream = new DisposablePtr<Stream>(stream, disposeAfterUse);
            IsStereo = isStereo;
            _is16Bit = is16Bit;
            _dataOffset = dataOffset;
            _sampleRate = sampleRate;
            // SSCI aligns the size of SOL data to 32 bits
            _rawDataSize = rawDataSize & ~3;
            // TODO: This is not valid for stereo SOL files, which
            // have interleaved L/R compression so need to store the
            // carried values for each channel separately. See
            // 60900.aud from Lighthouse for an example stereo file
            if (is16Bit)
            {
                _dpcmCarry16 = 0;
            }
            else
            {
                _dpcmCarry8 = 0x80;
            }

            byte compressionRatio = 2;
            var numChannels = isStereo ? 2 : 1;
            var bytesPerSample = is16Bit ? 2 : 1;
            Length = new Timestamp(_rawDataSize * compressionRatio * 1000 / (_sampleRate * numChannels * bytesPerSample), 60);
        }

        void IDisposable.Dispose()
        {
        }

        public int ReadBuffer(short[] buffer, int numSamples)
        {
            // Reading an odd number of 8-bit samples will result in a loss of samples
            // since one byte represents two samples and we do not store the second
            // nibble in this case; it should never happen in reality
           Debug.Assert(_is16Bit || numSamples % 2 == 0);

            int samplesPerByte = _is16Bit ? 1 : 2;

            int bytesToRead = numSamples / samplesPerByte;
            if (_stream.Value.Position + bytesToRead > _rawDataSize)
            {
                bytesToRead = (int) (_rawDataSize - _stream.Value.Position);
            }

            if (_is16Bit)
            {
                DeDpcm16(buffer, _stream.Value, bytesToRead, ref _dpcmCarry16);
            }
            else
            {
                DeDpcm8(buffer, _stream.Value, bytesToRead, ref _dpcmCarry8);
            }

            int samplesRead = bytesToRead * samplesPerByte;
            return samplesRead;
        }

        public bool Rewind()
        {
            return Seek(new Timestamp(0));
        }

        public bool Seek(Timestamp @where)
        {
            if (@where != new Timestamp(0))
            {
                // In order to seek in compressed SOL files, all
                // previous bytes must be known since it uses
                // differential compression. Therefore, only seeking
                // to the beginning is supported now (SSCI does not
                // offer seeking anyway)
                return false;
            }

            if (_is16Bit)
            {
                _dpcmCarry16 = 0;
            }
            else
            {
                _dpcmCarry8 = 0x80;
            }

            _stream.Value.Seek(_dataOffset, SeekOrigin.Begin);
            return true;
        }

        /// <summary>
        /// Decompresses 8-bit DPCM compressed audio. Each byte read
        /// outputs two samples into the decompression buffer.
        /// </summary>
        /// <param name="out"></param>
        /// <param name="audioStream"></param>
        /// <param name="numBytes"></param>
        /// <param name="sample"></param>
        private static void DeDpcm8(Ptr<short> @out, Stream audioStream, int numBytes, ref byte sample)
        {
            for (var i = 0; i < numBytes; ++i)
            {
                byte delta = (byte) audioStream.ReadByte();
                DeDpcm8Nibble(@out, ref sample, (byte) (delta >> 4));
                @out.Offset++;
                DeDpcm8Nibble(@out, ref sample, (byte) (delta & 0xf));
                @out.Offset++;
            }
        }

        /// <summary>
        /// Decompresses one half of an 8-bit DPCM compressed audio
        /// byte.
        /// </summary>
        /// <param name="out"></param>
        /// <param name="sample"></param>
        /// <param name="delta"></param>
        private static void DeDpcm8Nibble(Ptr<short> @out, ref byte sample, byte delta)
        {
            byte lastSample = sample;
            if ((delta & 8) != 0)
            {
                sample -= TableDpcm8[delta & 7];
            }
            else
            {
                sample += TableDpcm8[delta & 7];
            }
            sample = (byte) ScummHelper.Clip(sample, 0, 255);
            @out.Value = (short) (((lastSample + sample) << 7) ^ 0x8000);
        }

        private static void DeDpcm16(Ptr<short> @out, Stream audioStream, int numBytes, ref short sample)
        {
            for (var i = 0; i < numBytes; ++i)
            {
                byte delta = (byte) audioStream.ReadByte();
                if ((delta & 0x80) != 0)
                {
                    sample = (short) (sample - TableDpcm16[delta & 0x7f]);
                }
                else
                {
                    sample = (short) (sample + TableDpcm16[delta]);
                }
                sample = (short) ScummHelper.Clip(sample, -32768, 32767);
                @out.Value = sample;
                @out.Offset++;
            }
        }
    }

    internal static class Sol
    {
        public static ISeekableAudioStream MakeSOLStream(Stream stream, bool disposeAfterUse)
        {
            var br=new BinaryReader(stream);
            // TODO: Might not be necessary? Makes seeking work, but
            // not sure if audio is ever actually seeked in SSCI.
            var initialPosition = stream.Position;

            byte[] header =new byte[6];
            if (stream.Read(header, 0, 6) != 6)
            {
                return null;
            }

            if (header[0] != 0x8d || header.ToUInt32BigEndian(2) != ScummHelper.MakeTag('S', 'O', 'L', '\0'))
            {
                return null;
            }

            byte headerSize = header[1];
            ushort sampleRate = br.ReadUInt16();
            SolFlags flags = (SolFlags)br.ReadByte();
            int dataSize = br.ReadInt32();

            if ((flags & SolFlags.Compressed) != 0)
            {
                if ((flags & SolFlags.Stereo) != 0 && (flags & SolFlags.Is16Bit) != 0)
                {
                    return new SolStream(true,true,
                        new SeekableSubReadStream(stream, initialPosition, initialPosition + dataSize, disposeAfterUse),
                        disposeAfterUse,headerSize,sampleRate,dataSize);
                }
                if ((flags & SolFlags.Stereo) != 0)
                {
                    return new SolStream(true,false,
                        new SeekableSubReadStream(stream, initialPosition, initialPosition + dataSize, disposeAfterUse),
                        disposeAfterUse,headerSize,sampleRate,dataSize);
                }
                if ((flags & SolFlags.Is16Bit) != 0)
                {
                    return new SolStream(false,true,
                        new SeekableSubReadStream(stream, initialPosition, initialPosition + dataSize, disposeAfterUse),
                        disposeAfterUse,headerSize,sampleRate,dataSize);
                }
                    return new SolStream(false,false,
                            new SeekableSubReadStream(stream, initialPosition, initialPosition + dataSize, disposeAfterUse),
                            disposeAfterUse, headerSize, sampleRate, dataSize);
            }

            AudioFlags rawFlags = AudioFlags.LittleEndian;
            if ((flags & SolFlags.Is16Bit) != 0)
            {
                rawFlags |= AudioFlags.Is16Bits;
            }
            else
            {
                rawFlags |= AudioFlags.Unsigned;
            }

            if ((flags & SolFlags.Stereo) != 0)
            {
                rawFlags |= AudioFlags.Stereo;
            }

            return new RawStream(rawFlags, sampleRate, disposeAfterUse,
                new SeekableSubReadStream(stream, initialPosition + headerSize, initialPosition + headerSize + dataSize, disposeAfterUse));
        }

        // TODO: This needs to be removed when resource manager is fixed
        // to not split audio into two parts
        public static ISeekableAudioStream MakeSOLStream(Stream headerStream,Stream dataStream, bool disposeAfterUse)
        {
            var br=new BinaryReader(headerStream);
            if (br.ReadUInt32BigEndian() != ScummHelper.MakeTag('S', 'O', 'L', '\0'))
            {
                return null;
            }

            ushort sampleRate = br.ReadUInt16();
            SolFlags flags = (SolFlags)br.ReadByte();
            int dataSize = br.ReadInt32();

            if ((flags & SolFlags.Compressed) != 0)
            {
                if ((flags & SolFlags.Stereo) != 0 && (flags & SolFlags.Is16Bit) != 0)
                {
                    return new SolStream(true,true, dataStream, disposeAfterUse,
                    0,
                    sampleRate,
                    dataSize)
                    ;
                }
                if ((flags & SolFlags.Stereo) != 0)
                {
                    return new SolStream(true,false,dataStream,
                    disposeAfterUse,
                    0,
                    sampleRate,
                    dataSize)
                    ;
                }
                if ((flags & SolFlags.Is16Bit) != 0)
                {
                    return new SolStream(
                    false,
                    true ,dataStream,
                    disposeAfterUse,
                    0,
                    sampleRate,
                    dataSize)
                    ;
                }
                return new SolStream(
                        false,
                        false, dataStream,
                        disposeAfterUse,
                        0,
                        sampleRate,
                        dataSize)
                    ;
            }

            AudioFlags rawFlags = AudioFlags.LittleEndian;
            if ((flags & SolFlags.Is16Bit) != 0)
            {
                rawFlags |= AudioFlags.Is16Bits;
            }
            else
            {
                rawFlags |= AudioFlags.Unsigned;
            }

            if ((flags & SolFlags.Stereo) != 0)
            {
                rawFlags |= AudioFlags.Stereo;
            }

            return new RawStream(rawFlags, sampleRate, disposeAfterUse, dataStream);
        }
    }
}