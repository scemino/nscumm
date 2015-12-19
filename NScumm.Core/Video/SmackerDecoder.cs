using System;
using System.IO;
using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    enum AudioCompression
    {
        None,
        Dpcm,
        Rdft,
        Dct
    }

    struct AudioInfo
    {
        public AudioCompression compression;
        public bool hasAudio;
        public bool is16Bits;
        public bool isStereo;
        public uint sampleRate;
    }

    public class SmackerDecoder : VideoDecoder
    {
        class Header
        {
            public uint signature;
            public uint flags;
            public uint[] audioSize;
            public uint treesSize;
            public uint mMapSize;
            public uint mClrSize;
            public uint fullSize;
            public uint typeSize;
            public AudioInfo[] audioInfo;
            public uint dummy;

            public Header()
            {
                audioSize = new uint[7];
                audioInfo = new AudioInfo[7];
            }
        }

        Header _header;

        private SoundType _soundType;
        private Stream _fileStream;
        private BinaryReader _br;
        private uint[] _frameSizes;
        private byte[] _frameTypes;
        private uint _firstFrameStart;
        private IMixer _mixer;

        public SmackerDecoder(IMixer mixer, SoundType soundType = SoundType.SFX)
        {
            _mixer = mixer;
            _soundType = soundType;
        }

        public override bool LoadStream(Stream stream)
        {
            Close();

            _fileStream = stream;
            _br = new BinaryReader(stream);

            // Read in the Smacker header
            _header = new Header();
            _header.signature = _br.ReadUInt32BigEndian();

            if (_header.signature != ScummHelper.MakeTag('S', 'M', 'K', '2') && _header.signature != ScummHelper.MakeTag('S', 'M', 'K', '4'))
                return false;

            uint width = _br.ReadUInt32();
            uint height = _br.ReadUInt32();
            uint frameCount = _br.ReadUInt32();
            int frameDelay = _br.ReadInt32();

            // frame rate contains 2 digits after the comma, so 1497 is actually 14.97 fps
            Rational frameRate;
            if (frameDelay > 0)
                frameRate = new Rational(1000, frameDelay);
            else if (frameDelay < 0)
                frameRate = new Rational(100000, -frameDelay);
            else
                frameRate = new Rational(1000);

            // Flags are determined by which bit is set, which can be one of the following:
            // 0 - set to 1 if file contains a ring frame.
            // 1 - set to 1 if file is Y-interlaced
            // 2 - set to 1 if file is Y-doubled
            // If bits 1 or 2 are set, the frame should be scaled to twice its height
            // before it is displayed.
            _header.flags = _br.ReadUInt32();

            var videoTrack = CreateVideoTrack(width, height, frameCount, frameRate, _header.flags, _header.signature);
            AddTrack(videoTrack);

            // TODO: should we do any extra processing for Smacker files with ring frames?

            // TODO: should we do any extra processing for Y-doubled videos? Are they the
            // same as Y-interlaced videos?

            uint i;
            for (i = 0; i < 7; ++i)
                _header.audioSize[i] = _br.ReadUInt32();

            _header.treesSize = _br.ReadUInt32();
            _header.mMapSize = _br.ReadUInt32();
            _header.mClrSize = _br.ReadUInt32();
            _header.fullSize = _br.ReadUInt32();
            _header.typeSize = _br.ReadUInt32();

            for (i = 0; i < 7; ++i)
            {
                // AudioRate - Frequency and format information for each sound track, up to 7 audio tracks.
                // The 32 constituent bits have the following meaning:
                // * bit 31 - indicates Huffman + Dpcm compression
                // * bit 30 - indicates that audio data is present for this track
                // * bit 29 - 1 = 16-bit audio; 0 = 8-bit audio
                // * bit 28 - 1 = stereo audio; 0 = mono audio
                // * bit 27 - indicates Bink Rdft compression
                // * bit 26 - indicates Bink Dct compression
                // * bits 25-24 - unused
                // * bits 23-0 - audio sample rate
                uint audioInfo = _br.ReadUInt32();
                _header.audioInfo[i].hasAudio = (audioInfo & 0x40000000) != 0;
                _header.audioInfo[i].is16Bits = (audioInfo & 0x20000000) != 0;
                _header.audioInfo[i].isStereo = (audioInfo & 0x10000000) != 0;
                _header.audioInfo[i].sampleRate = audioInfo & 0xFFFFFF;

                if ((audioInfo & 0x8000000) != 0)
                    _header.audioInfo[i].compression = AudioCompression.Rdft;
                else if ((audioInfo & 0x4000000) != 0)
                    _header.audioInfo[i].compression = AudioCompression.Dct;
                else if ((audioInfo & 0x80000000) != 0)
                    _header.audioInfo[i].compression = AudioCompression.Dpcm;
                else
                    _header.audioInfo[i].compression = AudioCompression.None;

                if (_header.audioInfo[i].hasAudio)
                {
                    if (_header.audioInfo[i].compression == AudioCompression.Rdft || _header.audioInfo[i].compression == AudioCompression.Dct)
                        throw new InvalidOperationException("Unhandled Smacker v2 audio compression");

                    AddTrack(new SmackerAudioTrack(_mixer, _header.audioInfo[i], _soundType));
                }
            }

            _header.dummy = _br.ReadUInt32();

            _frameSizes = new uint[frameCount];
            for (i = 0; i < frameCount; ++i)
                _frameSizes[i] = _br.ReadUInt32();

            _frameTypes = new byte[frameCount];
            for (i = 0; i < frameCount; ++i)
                _frameTypes[i] = _br.ReadByte();

            var huffmanTrees = _br.ReadBytes((int)_header.treesSize);
            using (var ms = new MemoryStream(huffmanTrees))
            {
                var bs = BitStream.Create8Lsb(ms);
                videoTrack.ReadTrees(bs, (int)_header.mMapSize, (int)_header.mClrSize, (int)_header.fullSize, (int)_header.typeSize);
            }

            _firstFrameStart = (uint)_fileStream.Position;

            return true;
        }

        public override bool Rewind()
        {
            // Call the parent method to rewind the tracks first
            if (!base.Rewind())
                return false;

            // And seek back to where the first frame begins
            _fileStream.Seek(_firstFrameStart, SeekOrigin.Begin);
            return true;
        }

        public override void Close()
        {
            base.Close();

            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }

            _frameTypes = null;
            _frameSizes = null;
        }


        protected override void ReadNextPacket()
        {
            var videoTrack = (SmackerVideoTrack)GetTrack(0);

            if (videoTrack.EndOfTrack)
                return;

            videoTrack.IncreaseCurrentFrame();

            int i;
            uint chunkSize = 0;
            uint dataSizeUnpacked = 0;

            uint startPos = (uint)_fileStream.Position;

            // Check if we got a frame with palette data, and
            // call back the virtual setPalette function to set
            // the current palette
            if ((_frameTypes[videoTrack.CurrentFrame] & 1) != 0)
                videoTrack.UnpackPalette(_fileStream);

            // Load audio tracks
            for (i = 0; i < 7; ++i)
            {
                if ((_frameTypes[videoTrack.CurrentFrame] & (2 << i)) == 0)
                    continue;

                chunkSize = _br.ReadUInt32();
                chunkSize -= 4;    // subtract the first 4 bytes (chunk size)

                if (_header.audioInfo[i].compression == AudioCompression.None)
                {
                    dataSizeUnpacked = chunkSize;
                }
                else
                {
                    dataSizeUnpacked = _br.ReadUInt32();
                    chunkSize -= 4;    // subtract the next 4 bytes (unpacked data size)
                }

                HandleAudioTrack((byte)i, (int)chunkSize, (int) dataSizeUnpacked);
            }

            uint frameSize = (uint)(_frameSizes[videoTrack.CurrentFrame] & ~3);
            //	uint32 remainder =  _frameSizes[videoTrack.getCurFrame()] & 3;

            if (_fileStream.Position - startPos > frameSize)
                throw new InvalidOperationException("Smacker actual frame size exceeds recorded frame size");

            uint frameDataSize = (uint)(frameSize - (_fileStream.Position - startPos));

            var frameData = new byte[frameDataSize + 1];
            // Padding to keep the BigHuffmanTrees from reading past the data end
            frameData[frameDataSize] = 0x00;

            _fileStream.Read(frameData, 0, (int)frameDataSize);

            using (var ms = new MemoryStream(frameData, 0, (int)(frameDataSize + 1)))
            {
                var bs = BitStream.Create8Lsb(ms);
                videoTrack.DecodeFrame(bs);
            }

            _fileStream.Seek(startPos + frameSize, SeekOrigin.Begin);
        }

        private void HandleAudioTrack(byte track, int chunkSize, int unpackedSize)
        {
            if (chunkSize == 0)
                return;

            if (_header.audioInfo[track].hasAudio)
            {
                // Get the audio track, which start at offset 1 (first track is video)
                SmackerAudioTrack audioTrack = (SmackerAudioTrack)GetTrack(track + 1);

                // If it's track 0, play the audio data
                byte[] soundBuffer = new byte[chunkSize + 1];
                // Padding to keep the SmallHuffmanTrees from reading past the data end
                soundBuffer[chunkSize] = 0x00;

                _fileStream.Read(soundBuffer, 0, chunkSize);

                if (_header.audioInfo[track].compression == AudioCompression.Rdft || _header.audioInfo[track].compression == AudioCompression.Dct)
                {
                    // TODO: Compressed audio (Bink RDFT/DCT encoded)
                    throw new NotImplementedException("Compressed audio (Bink RDFT/DCT encoded) not implemented");
                }
                else if (_header.audioInfo[track].compression == AudioCompression.Dpcm)
                {
                    // Compressed audio (Huffman DPCM encoded)
                    audioTrack.QueueCompressedBuffer(soundBuffer, chunkSize + 1, unpackedSize);
                }
                else
                {
                    // Uncompressed audio (PCM)
                    audioTrack.QueuePCM(soundBuffer, chunkSize);
                }
            }
            else
            {
                // Ignore possibly unused data
                _fileStream.Seek(chunkSize, SeekOrigin.Current);
            }
        }

        private SmackerVideoTrack CreateVideoTrack(uint width, uint height, uint frameCount, Rational frameRate, uint flags, uint signature)
        {
            return new SmackerVideoTrack(width, height, frameCount, frameRate, flags, signature);
        }
    }
}
