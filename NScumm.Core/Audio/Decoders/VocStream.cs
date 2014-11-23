//
//  VocStream.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System.Collections.Generic;
using System.Text;

namespace NScumm.Core.Audio.Decoders
{
    public class VocStream: IMixerAudioStream, IDisposable
    {
        /// <summary>
        /// How many samples we can buffer at once.
        /// </summary>
        const int SampleBufferLength = 2048;
        bool _isUnsigned;
        int _rate;
        int _curBlock;

        int _blockLeft;

        Timestamp _length;
        byte[] _buffer;
        Stream _stream;
        BinaryReader _br;
        List<Block> _blocks;

        public VocStream(Stream stream, bool isUnsigned)
        {
            _stream = stream;
            _br = new BinaryReader(_stream);
            _isUnsigned = isUnsigned;
            _buffer = new byte[SampleBufferLength];
            _blocks = new List<Block>();

            if (!CheckVOCHeader())
            {
                throw new NotSupportedException("Invalid VOC file.");
            }
            PreProcess();
        }

        #region IDisposable implementation

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        #endregion

        public virtual int ReadBuffer(short[] buffer)
        {
            int pos = 0;
            var samplesLeft = buffer.Length;
            while (samplesLeft > 0)
            {
                // Try to read up to "samplesLeft" samples.
                var len = FillBuffer(samplesLeft);

                // In case we were not able to read any samples
                // we will stop reading here.
                if (len == 0)
                    break;

                // Adjust the samples left to read.
                samplesLeft -= len;

                // Copy the data to the caller's buffer.
                var src = _buffer;
                for (var i = 0; i < len; i++)
                {
                    buffer[pos++] = (short)((src[i] << 8) ^ (_isUnsigned ? 0x8000 : 0));
                }
            }

            return buffer.Length - samplesLeft;
        }

        int FillBuffer(int maxSamples)
        {
            var bufferedSamples = 0;
            int dst = 0;

            // We can only read up to "kSampleBufferLength" samples
            // so we take this into consideration, when trying to
            // read up to maxSamples.
            maxSamples = Math.Min(SampleBufferLength, maxSamples);

            // We will only read up to maxSamples
            while (maxSamples > 0 && !IsEndOfData)
            {
                // Calculate how many samples we can safely read
                // from the current block.
                var len = Math.Min(maxSamples, _blockLeft);

                // Try to read all the sample data and update the
                // destination pointer.
                var buffer = _br.ReadBytes(len);
                Array.Copy(buffer, 0, _buffer, dst, buffer.Length);
                dst += buffer.Length;

                // Calculate how many samples we actually read.
                var samplesRead = buffer.Length;

                // Update all status variables
                bufferedSamples += samplesRead;
                maxSamples -= samplesRead;
                _blockLeft -= samplesRead;

                // Advance to the next block in case the current
                // one is already finished.
                UpdateBlockIfNeeded();
            }

            return bufferedSamples;
        }

        void UpdateBlockIfNeeded()
        {
            // Have we now finished this block? If so, read the next block
            if (_blockLeft == 0 && _curBlock != _blocks.Count)
            {
                // Find the next sample block
                while (true)
                {
                    // Next block
                    ++_curBlock;

                    // Check whether we reached the end of the stream
                    // yet.
                    if (_curBlock == _blocks.Count)
                        return;

                    // Skip all none sample blocks for now
                    if (_blocks[_curBlock].Code != 1 && _blocks[_curBlock].Code != 9)
                        continue;

                    _stream.Seek(_blocks[_curBlock].Offset, SeekOrigin.Current);

                    _blockLeft = _blocks[_curBlock].Samples;

                    return;
                }
            }
        }

        public virtual bool IsStereo { get { return false; } }

        public virtual int Rate { get { return _rate; } }

        public bool IsEndOfData
        {
            get{ return (_curBlock == _blocks.Count) && (_blockLeft == 0); }
        }

        public bool IsEndOfStream
        {
            get{ return IsEndOfData; }
        }

        public virtual bool Seek(Timestamp where)
        {
            // Invalidate stream
            _blockLeft = 0;
            _curBlock = _blocks.Count;

            if (where > _length)
                return false;

            // Search for the block containing the requested sample
            int seekSample = ConvertTimeToStreamPos(where, Rate, IsStereo).TotalNumberOfFrames;
            int curSample = 0;

            for (_curBlock = 0; _curBlock != _blocks.Count; ++_curBlock)
            {
                // Skip all none sample blocks for now
                if (_blocks[_curBlock].Code != 1 && _blocks[_curBlock].Code != 9)
                    continue;

                var nextBlockSample = curSample + _blocks[_curBlock].Samples;

                if (nextBlockSample > seekSample)
                    break;

                curSample = nextBlockSample;
            }

            if (_curBlock == _blocks.Count)
            {
                return ((seekSample - curSample) == 0);
            }
            else
            {
                var offset = seekSample - curSample;

                _stream.Seek(_blocks[_curBlock].Offset + offset, SeekOrigin.Begin);

                _blockLeft = _blocks[_curBlock].Samples - offset;

                return true;
            }

        }

        public bool Seek(int where)
        {
            return Seek(new Timestamp(where, Rate));
        }

        public bool Rewind()
        {
            return Seek(0);
        }

        public virtual Timestamp Length  { get { return _length; } }

        struct Block
        {
            public byte Code;
            public int Length;

            public int Offset;
            public int Rate;
            public int Samples;
            public int Count;
         
        }

        protected static Timestamp ConvertTimeToStreamPos(Timestamp where, int rate, bool isStereo)
        {
            var result = new Timestamp(where.ConvertToFramerate(rate * (isStereo ? 2 : 1)));

            // When the Stream is a stereo stream, we have to assure
            // that the sample position is an even number.
            if (isStereo && (result.TotalNumberOfFrames & 1) != 0)
                result = result.AddFrames(-1); // We cut off one sample here.

            // Since Timestamp allows sub-frame-precision it might lead to odd behaviors
            // when we would just return result.
            //
            // An example is when converting the timestamp 500ms to a 11025 Hz based
            // stream. It would have an internal frame counter of 5512.5. Now when
            // doing calculations at frame precision, this might lead to unexpected
            // results: The frame difference between a timestamp 1000ms and the above
            // mentioned timestamp (both with 11025 as framerate) would be 5512,
            // instead of 5513, which is what a frame-precision based code would expect.
            //
            // By creating a new Timestamp with the given parameters, we create a
            // Timestamp with frame-precision, which just drops a sub-frame-precision
            // information (i.e. rounds down).
            return new Timestamp(result.Seconds, result.NumberOfFrames, result.Framerate);
        }

        bool IsAtEndOfStream
        {
            get
            {
                return _stream.Position == _stream.Length;
            }
        }

        void PreProcess()
        {
            var block = new Block();

            // Scan through the file and collect all blocks
            while (true)
            {
                block.Code = _br.ReadByte();
                block.Length = 0;

                // If we hit EOS here we found the end of the VOC file.
                // According to http://wiki.multimedia.cx/index.php?title=Creative_Voice
                // there is no need for an "Terminator" block to be present.
                // In case we hit a "Terminator" block we also break here.
                if (IsAtEndOfStream || block.Code == 0)
                    break;
                // We will allow invalid block numbers as terminators. This is needed,
                // since some games ship broken VOC files. The following occasions are
                // known:
                // - 128 is used as terminator in Simon 1 Amiga CD32
                // - Full Throttle contains a VOC file with an incorrect block length
                //   resulting in a sample (127) to be read as block code.
                if (block.Code > 9)
                {
                    Console.Error.WriteLine("VocStream::preProcess: Caught {0} as terminator", block.Code);
                    break;
                }

                block.Length = _stream.ReadByte();
                block.Length |= _stream.ReadByte() << 8;
                block.Length |= _stream.ReadByte() << 16;

                // Premature end of stream => error!
                if (IsAtEndOfStream)
                {
                    Console.Error.WriteLine("VocStream::preProcess: Reading failed");
                    return;
                }

                int skip = 0;

                switch (block.Code)
                {
                // Sound data
                    case 1:
                            // Sound data (New format)
                    case 9:
                        if (block.Code == 1)
                        {
                            if (block.Length < 2)
                            {
                                Console.Error.WriteLine("Invalid sound data block length {0} in VOC file", block.Length);
                                return;
                            }

                            // Read header data
                            int freqDiv = _stream.ReadByte();
                            // Prevent division through 0
                            if (freqDiv == 256)
                            {
                                Console.Error.WriteLine("Invalid frequency divisor 256 in VOC file");
                                return;
                            }
                            block.Rate = GetSampleRateFromVOCRate(freqDiv);

                            int codec = _stream.ReadByte();
                            // We only support 8bit PCM
                            if (codec != 0)
                            {
                                Console.Error.WriteLine("Unhandled codec {0} in VOC file", codec);
                                return;
                            }

                            block.Samples = skip = block.Length - 2;
                            block.Offset = (int)_stream.Position;

                            // Check the last block if there is any
                            if (_blocks.Count > 0)
                            {
                                int lastBlock = _blocks.Count;
                                --lastBlock;
                                // When we have found a block 8 as predecessor
                                // we need to use its settings
                                if (_blocks[lastBlock].Code == 8)
                                {
                                    block.Rate = _blocks[lastBlock].Rate;
                                    // Remove the block since we don't need it anymore
                                    _blocks.RemoveAt(lastBlock);
                                }
                            }
                        }
                        else
                        {
                            if (block.Length < 12)
                            {
                                Console.Error.WriteLine("Invalid sound data (wew format) block length {0} in VOC file", block.Length);
                                return;
                            }

                            block.Rate = _br.ReadInt32();
                            int bitsPerSample = _stream.ReadByte();
                            // We only support 8bit PCM
                            if (bitsPerSample != 8)
                            {
                                Console.Error.WriteLine("Unhandled bits per sample {0} in VOC file", bitsPerSample);
                                return;
                            }
                            int channels = _stream.ReadByte();
                            // We only support mono
                            if (channels != 1)
                            {
                                Console.Error.WriteLine("Unhandled channel count {0} in VOC file", channels);
                                return;
                            }
                            int codec = _br.ReadInt16();
                            // We only support 8bit PCM
                            if (codec != 0)
                            {
                                Console.Error.WriteLine("Unhandled codec {0} in VOC file", codec);
                                return;
                            }
                            /*uint32 reserved = */
                            _br.ReadInt32();
                            block.Offset = (int)_stream.Position;
                            block.Samples = skip = block.Length - 12;
                        }

                            // Check whether we found a new highest rate
                        if (_rate < block.Rate)
                            _rate = block.Rate;
                        break;

                // Silence
                    case 3:
                        {
                            if (block.Length != 3)
                            {
                                Console.Error.WriteLine("Invalid silence block length {0} in VOC file", block.Length);
                                return;
                            }

                            block.Offset = 0;

                            block.Samples = _br.ReadInt16() + 1;
                            int freqDiv = _stream.ReadByte();
                            // Prevent division through 0
                            if (freqDiv == 256)
                            {
                                Console.Error.WriteLine("Invalid frequency divisor 256 in VOC file");
                                return;
                            }
                            block.Rate = GetSampleRateFromVOCRate(freqDiv);
                        }
                        break;

                // Repeat start
                    case 6:
                        if (block.Length != 2)
                        {
                            Console.Error.WriteLine("Invalid repeat start block length {0} in VOC file", block.Length);
                            return;
                        }

                        block.Count = _br.ReadInt16() + 1;
                        break;

                // Repeat end
                    case 7:
                        break;

                // Extra info
                    case 8:
                        {
                            if (block.Length != 4)
                                return;

                            int freqDiv = _br.ReadInt16();
                            // Prevent division through 0
                            if (freqDiv == 65536)
                            {
                                Console.Error.WriteLine("Invalid frequency divisor 65536 in VOC file");
                                return;
                            }

                            int codec = _stream.ReadByte();
                            // We only support RAW 8bit PCM.
                            if (codec != 0)
                            {
                                Console.Error.WriteLine("Unhandled codec {0} in VOC file", codec);
                                return;
                            }

                            int channels = _stream.ReadByte() + 1;
                            // We only support mono sound right now
                            if (channels != 1)
                            {
                                Console.Error.WriteLine("Unhandled channel count {0} in VOC file", channels);
                                return;
                            }

                            block.Offset = 0;
                            block.Samples = 0;
                            block.Rate = (int)(256000000L / (65536L - freqDiv));
                        }
                        break;

                    default:
                        Console.Error.WriteLine("Unhandled code {0} in VOC file (len {1})", block.Code, block.Length);
                            // Skip the whole block and try to use the next one.
                        skip = block.Length;
                        break;
                }

                // Premature end of stream => error!
                if (IsAtEndOfStream)
                {
                    Console.Error.WriteLine("VocStream::preProcess: Reading failed");
                    return;
                }

                // Skip the rest of the block
                if (skip != 0)
                    _stream.Seek(skip, SeekOrigin.Current);

                _blocks.Add(block);
            }

            // Since we determined the sample rate we need for playback now, we will
            // initialize the play length.
            _length = new Timestamp(0, _rate);

            // Calculate the total play time and do some more sanity checks
            foreach (var i in _blocks)
            {
                // Check whether we found a block 8 which survived, this is not
                // allowed to happen!
                if (i.Code == 8)
                {
                    Console.Error.WriteLine("VOC file contains unused block 8");
                    return;
                }
                // For now only use blocks with actual samples
                if (i.Code != 1 && i.Code != 9)
                    continue;

                // Check the sample rate
                if (i.Rate != _rate)
                {
                    Console.Error.WriteLine("VOC file contains chunks with different sample rates ({0} != {1})", _rate, i.Rate);
                    return;
                }

                _length = _length.AddFrames(i.Samples);
            }

            // Set the current block to the first block in the stream
            Rewind();
        }

        static int GetSampleRateFromVOCRate(int vocSR)
        {
            if (vocSR == 0xa5 || vocSR == 0xa6)
            {
                return 11025;
            }
            else if (vocSR == 0xd2 || vocSR == 0xd3)
            {
                return 22050;
            }
            else
            {
                int sr = (int)(1000000L / (256L - vocSR));
                // inexact sampling rates occur e.g. in the kitchen in Monkey Island,
                // very easy to reach right from the start of the game.
                //warning("inexact sample rate used: %i (0x%x)", sr, vocSR);
                return sr;
            }
        }

        bool CheckVOCHeader()
        {
            var desc = _br.ReadBytes(20);
            if ((Encoding.ASCII.GetString(desc, 0, 4) != "VTLK") &&
                (Encoding.ASCII.GetString(desc, 0, 8) != "Creative") &&
                (Encoding.ASCII.GetString(desc, 0, 19) != "Creative Voice File"))
                return false;
            //if (fileHeader.desc[19] != 0x1A)
            //      debug(3, "checkVOCHeader: Partially invalid header");

            int offset = _br.ReadInt16();
            int version = _br.ReadInt16();
            int code = _br.ReadInt16();

            if (offset != 26)
                return false;

            // 0x100 is an invalid VOC version used by German version of DOTT (Disk) and
            // French version of Simon the Sorcerer 2 (CD)
            if (version != 0x010A && version != 0x0114 && version != 0x0100)
                return false;

            return code == ~version + 0x1234;
        }
    }
}

