//
//  Wave.cs
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
using System.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio.Decoders
{
    public static class Wave
    {
        public static ISeekableAudioStream MakeWAVStream(Stream stream, bool disposeAfterUse)
        {
            int size, rate;
            AudioFlags flags;
            ushort type;
            int blockAlign;

            if (!LoadWAVFromStream(stream, out size, out rate, out flags, out type, out blockAlign))
            {
                if (disposeAfterUse)
                    stream.Dispose();
                return null;
            }

            if (type == 17) // MS IMA ADPCM
                throw new NotImplementedException("MS IMA ADPCM not implemented");
            //return makeADPCMStream(stream, disposeAfterUse, size, Audio::kADPCMMSIma, rate, flags.HasFlag(AudioFlags.Stereo) ? 2 : 1, blockAlign);
            if (type == 2) // MS ADPCM
                throw new NotImplementedException("MS ADPCM not implemented");
            //return makeADPCMStream(stream, disposeAfterUse, size, Audio::kADPCMMS, rate, flags.HasFlag(AudioFlags.Stereo) ? 2 : 1, blockAlign);

            // Raw PCM, make sure the last packet is complete
            int sampleSize = (flags.HasFlag(AudioFlags.Is16Bits) ? 2 : 1) * (flags.HasFlag(AudioFlags.Stereo) ? 2 : 1);
            if (size % sampleSize != 0)
            {
                Warning("makeWAVStream: Trying to play a WAVE file with an incomplete PCM packet");
                size &= ~(sampleSize - 1);
            }

            // Raw PCM. Just read everything at once.
            // TODO: More elegant would be to wrap the stream.
            byte[] data = new byte[size];
            stream.Read(data, 0, size);

            if (disposeAfterUse)
                stream.Dispose();

            return new RawStream(flags, rate, true, new MemoryStream(data, 0, size));
        }


        /// <summary>
        /// Try to load a WAVE from the given seekable stream. Returns true if
        /// successful.In that case, the stream's seek position will be set to the
        /// start of the audio data, and size, rate and flags contain information
        /// necessary for playback.Currently this function supports uncompressed
        /// raw PCM data, MS IMA ADPCM and MS ADPCM (uses makeADPCMStream internally).
        /// </summary>
        /// <returns><c>true</c>, if WAVF rom stream was loaded, <c>false</c> otherwise.</returns>
        /// <param name="stream">Stream.</param>
        /// <param name="size">Size.</param>
        /// <param name="rate">Rate.</param>
        /// <param name="flags">Flags.</param>
        /// <param name="wavType">Wav type.</param>
        /// <param name="blockAlign">Block align.</param>
        public static bool LoadWAVFromStream(Stream stream, out int size, out int rate, out AudioFlags flags, out ushort wavType, out int blockAlign)
        {
            var br = new BinaryReader(stream);
            var initialPos = stream.Position;
            byte[] buf = new byte[4 + 1];

            size = 0;
            rate = 0;
            flags = 0;
            wavType = 0;
            blockAlign = 0;

            stream.Read(buf, 0, 4);
            if (buf.GetRawText() != "RIFF")
            {
                Warning("getWavInfo: No 'RIFF' header");
                return false;
            }

            int wavLength = br.ReadInt32();

            stream.Read(buf, 0, 4);
            if (buf.GetRawText() != "WAVE")
            {
                Warning("getWavInfo: No 'WAVE' header");
                return false;
            }

            stream.Read(buf, 0, 4);
            if (buf.GetRawText() != "fmt ")
            {
                Warning("getWavInfo: No 'fmt' header");
                return false;
            }

            uint fmtLength = br.ReadUInt32();
            if (fmtLength < 16)
            {
                // A valid fmt chunk always contains at least 16 bytes
                Warning("getWavInfo: 'fmt' header is too short");
                return false;
            }

            // Next comes the "type" field of the fmt header. Some typical
            // values for it:
            // 1  -> uncompressed PCM
            // 17 -> IMA ADPCM compressed WAVE
            // See <http://www.saettler.com/RIFFNEW/RIFFNEW.htm> for a more complete
            // list of common WAVE compression formats...
            ushort type = br.ReadUInt16();    // == 1 for PCM data
            ushort numChannels = br.ReadUInt16(); // 1 for mono, 2 for stereo
            uint samplesPerSec = br.ReadUInt32();   // in Hz
            uint avgBytesPerSec = br.ReadUInt32();  // == SampleRate * NumChannels * BitsPerSample/8

            blockAlign = br.ReadUInt16();  // == NumChannels * BitsPerSample/8
            ushort bitsPerSample = br.ReadUInt16();   // 8, 16 ...
                                                      // 8 bit data is unsigned, 16 bit data signed

            wavType = type;

#if UNDEFINED
            Debug("WAVE information:");
            Debug("  total size: {0}", wavLength);
            Debug("  fmt size: {0}", fmtLength);
            Debug("  type: {0}", type);
            Debug("  numChannels: {0}", numChannels);
            Debug("  samplesPerSec: {0}", samplesPerSec);
            Debug("  avgBytesPerSec: {0}", avgBytesPerSec);
            Debug("  blockAlign: {0}", blockAlign);
            Debug("  bitsPerSample: {0}", bitsPerSample);
#endif

            if (type != 1 && type != 2 && type != 17)
            {
                Warning("getWavInfo: only PCM, MS ADPCM or IMA ADPCM data is supported (type {0})", type);
                return false;
            }

            if (blockAlign != numChannels * bitsPerSample / 8 && type != 2)
            {
                Debug(0, "getWavInfo: blockAlign is invalid");
            }

            if (avgBytesPerSec != samplesPerSec * blockAlign && type != 2)
            {
                Debug(0, "getWavInfo: avgBytesPerSec is invalid");
            }

            // Prepare the return values.
            rate = (int)samplesPerSec;

            flags = 0;
            if (bitsPerSample == 8)     // 8 bit data is unsigned
                flags |= AudioFlags.Unsigned;
            else if (bitsPerSample == 16)   // 16 bit data is signed little endian
                flags |= (AudioFlags.Is16Bits | AudioFlags.LittleEndian);
            else if (bitsPerSample == 4 && (type == 2 || type == 17))
                flags |= AudioFlags.Is16Bits;
            else {
                Warning("getWavInfo: unsupported bitsPerSample {0}", bitsPerSample);
                return false;
            }

            if (numChannels == 2)
                flags |= AudioFlags.Stereo;
            else if (numChannels != 1)
            {
                Warning("getWavInfo: unsupported number of channels {0}", numChannels);
                return false;
            }

            // It's almost certainly a WAV file, but we still need to find its
            // 'data' chunk.

            // Skip over the rest of the fmt chunk.
            int offset = (int)(fmtLength - 16);

            do
            {
                stream.Seek(offset, SeekOrigin.Current);
                if (stream.Position >= initialPos + wavLength + 8)
                {
                    Warning("getWavInfo: Can't find 'data' chunk");
                    return false;
                }
                stream.Read(buf, 0, 4);
                offset = br.ReadInt32();

#if UNDEFINED
        Debug("  found a '%s' tag of size %d", buf, offset);
#endif
            } while (buf.GetRawText() != "data");

            // Stream now points at 'offset' bytes of sample data...
            size = offset;

            return true;
        }

        public static bool LoadWAVFromStream(Stream stream, out int size, out int rate, out AudioFlags flags)
        {
            ushort wavType;
            int blockAlign;
            return LoadWAVFromStream(stream, out size, out rate, out flags, out wavType, out blockAlign);
        }
    }
}
