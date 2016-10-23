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
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Common;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Core.Video;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Video
{
    public class RobotDecoder : VideoDecoder
    {
        private class RobotHeader
        {
            // 6 bytes, identifier bytes
            public ushort version;
            public ushort audioChunkSize;
            public ushort audioSilenceSize;
            // 2 bytes, unknown
            public ushort frameCount;
            public ushort paletteDataSize;
            public ushort unkChunkDataSize;
            // 5 bytes, unknown
            public byte hasSound;
            // 34 bytes, unknown
        }

        private class RobotVideoTrack : FixedRateVideoTrack
        {
            private int _frameCount;
            private int _curFrame;
            private byte[] _palette = new byte[256 * 3];
            private bool _dirtyPalette;
            private Surface _surface;

            public override ushort Width => (ushort) _surface.Width;
            public override ushort Height => (ushort) _surface.Height;
            public override PixelFormat PixelFormat => _surface.PixelFormat;
            public override int CurrentFrame => _curFrame;
            public override int FrameCount => _frameCount;

            public override Surface DecodeNextFrame()
            {
                return _surface;
            }

            protected override Rational FrameRate => new Rational(60, 10);

            public override bool HasDirtyPalette()
            {
                return _dirtyPalette;
            }

            public override byte[] GetPalette()
            {
                _dirtyPalette = false;
                return _palette;
            }

            public RobotVideoTrack(int frameCount)
            {
                _frameCount = frameCount;
                _surface = null;
                _curFrame = -1;
                _dirtyPalette = false;
            }

            public void ReadPaletteChunk(IBinaryReader stream, ushort chunkSize)
            {
                var paletteData = new byte[chunkSize];
                stream.BaseStream.Read(paletteData, 0, chunkSize);

                // SCI1.1 palette
                byte palFormat = paletteData[32];
                ushort palColorStart = paletteData[25];
                ushort palColorCount = paletteData.ReadSci11EndianUInt16(29);

                int palOffset = 37;
                Array.Clear(_palette, 0, 256 * 3);

                for (ushort colorNo = palColorStart; colorNo < palColorStart + palColorCount; colorNo++)
                {
                    if (palFormat == kRobotPalVariable)
                        palOffset++;
                    _palette[colorNo * 3 + 0] = paletteData[palOffset++];
                    _palette[colorNo * 3 + 1] = paletteData[palOffset++];
                    _palette[colorNo * 3 + 2] = paletteData[palOffset++];
                }

                _dirtyPalette = true;
            }

            public void CalculateVideoDimensions(IBinaryReader stream, uint[] frameSizes)
            {
                // This is an O(n) operation, as each frame has a different size.
                // We need to know the actual frame size to have a constant video size.
                long pos = stream.BaseStream.Position;

                ushort width = 0, height = 0;

                for (int curFrame = 0; curFrame < _frameCount; curFrame++)
                {
                    stream.BaseStream.Seek(4, SeekOrigin.Current);
                    ushort frameWidth = stream.ReadUInt16();
                    ushort frameHeight = stream.ReadUInt16();
                    if (frameWidth > width)
                        width = frameWidth;
                    if (frameHeight > height)
                        height = frameHeight;
                    stream.BaseStream.Seek(frameSizes[curFrame] - 8, SeekOrigin.Current);
                }

                stream.BaseStream.Seek(pos, SeekOrigin.Begin);

                _surface = new Surface(width, height, PixelFormat.Indexed8);
            }
        }

        private class RobotAudioTrack : AudioTrack
        {
            private readonly QueuingAudioStream _audioStream;

            public RobotAudioTrack()
            {
                _audioStream = new QueuingAudioStream(11025, false);
            }

            public override IAudioStream AudioStream => _audioStream;
            public override SoundType SoundType => SoundType.Music;

            private void QueueBuffer(byte[] buffer, int size)
            {
                _audioStream.QueueBuffer(buffer, size, true, AudioFlags.Is16Bits | AudioFlags.LittleEndian);
            }
        }

        private const int kRobotPalVariable = 0;
        private const int kRobotPalConstant = 1;

        private RobotHeader _header;
        private int _frameCount;
        private int _curFrame;
        private byte[] _palette = new byte[256 * 3];
        private bool _dirtyPalette;
        private Surface _surface;

        private Point _pos;
        private bool _isBigEndian;
        private uint[] _frameTotalSize;

        private IBinaryReader _fileStream;

        public RobotDecoder(bool isBigEndian)
        {
            _isBigEndian = isBigEndian;
            _header = new RobotHeader();
        }

        public override bool LoadStream(Stream stream)
        {
            Close();

            _fileStream = new EndianBinaryReader(_isBigEndian, new SeekableSubReadStream(stream, 0, stream.Length, true));

            ReadHeaderChunk();

            // There are several versions of robot files, ranging from 3 to 6.
            // v3: no known examples
            // v4: PQ:SWAT demo
            // v5: SCI2.1 and SCI3 games
            // v6: SCI3 games
            if (_header.version < 4 || _header.version > 6)
                Error("Unknown robot version: {0}", _header.version);

            RobotVideoTrack videoTrack = new RobotVideoTrack(_header.frameCount);
            AddTrack(videoTrack);

            if (_header.hasSound != 0)
                AddTrack(new RobotAudioTrack());

            videoTrack.ReadPaletteChunk(_fileStream, _header.paletteDataSize);
            ReadFrameSizesChunk();
            videoTrack.CalculateVideoDimensions(_fileStream, _frameTotalSize);
            return true;
        }

        private void ReadHeaderChunk()
        {
            // Header (60 bytes)
            _fileStream.BaseStream.Seek(6, SeekOrigin.Current);
            _header.version = _fileStream.ReadUInt16();
            _header.audioChunkSize = _fileStream.ReadUInt16();
            _header.audioSilenceSize = _fileStream.ReadUInt16();
            _fileStream.BaseStream.Seek(2, SeekOrigin.Current);
            _header.frameCount = _fileStream.ReadUInt16();
            _header.paletteDataSize = _fileStream.ReadUInt16();
            _header.unkChunkDataSize = _fileStream.ReadUInt16();
            _fileStream.BaseStream.Seek(5, SeekOrigin.Current);
            _header.hasSound = _fileStream.ReadByte();
            _fileStream.BaseStream.Seek(34, SeekOrigin.Current);

            // Some videos (e.g. robot 1305 in Phantasmagoria and
            // robot 184 in Lighthouse) have an unknown chunk before
            // the palette chunk (probably used for sound preloading).
            // Skip it here.
            if (_header.unkChunkDataSize != 0)
                _fileStream.BaseStream.Seek(_header.unkChunkDataSize, SeekOrigin.Current);
        }

        private void ReadFrameSizesChunk()
        {
            // The robot video file contains 2 tables, with one entry for each frame:
            // - A table containing the size of the image in each video frame
            // - A table containing the total size of each video frame.
            // In v5 robots, the tables contain 16-bit integers, whereas in v6 robots,
            // they contain 32-bit integers.
            _frameTotalSize = new uint[_header.frameCount];

            // TODO: The table reading code can probably be removed once the
            // audio chunk size is figured out (check the TODO inside processNextFrame())
#if Undefined
// We don't need any of the two tables to play the video, so we ignore
// both of them.
            uint16 wordSize = _header.version == 6 ? 4 : 2;
            _fileStream.skip(_header.frameCount * wordSize * 2);
#else
            switch (_header.version)
            {
                case 4:
                case 5: // sizes are 16-bit integers
                    // Skip table with frame image sizes, as we don't need it
                    _fileStream.BaseStream.Seek(_header.frameCount * 2, SeekOrigin.Current);
                    for (int i = 0; i < _header.frameCount; ++i)
                        _frameTotalSize[i] = _fileStream.ReadUInt16();
                    break;
                case 6: // sizes are 32-bit integers
                    // Skip table with frame image sizes, as we don't need it
                    _fileStream.BaseStream.Seek(_header.frameCount * 4, SeekOrigin.Current);
                    for (int i = 0; i < _header.frameCount; ++i)
                        _frameTotalSize[i] = _fileStream.ReadUInt32();
                    break;
                default:
                    Error("Can't yet handle index table for robot version {0}", _header.version);
                    break;
            }
#endif

            // 2 more unknown tables
            _fileStream.BaseStream.Seek(1024 + 512, SeekOrigin.Current);

            // Pad to nearest 2 kilobytes
            var curPos = _fileStream.BaseStream.Position;
            if ((curPos & 0x7ff) != 0)
                _fileStream.BaseStream.Seek((curPos & ~0x7ff) + 2048, SeekOrigin.Begin);
        }

        public bool Load(int id)
        {
            // TODO: RAMA's robot 1003 cannot be played (shown at the menu screen) -
            // its drawn at odd coordinates. SV can't play it either (along with some
            // others), so it must be some new functionality added in RAMA's robot
            // videos. Skip it for now.
            if (SciEngine.Instance.GameId == SciGameId.RAMA && id == 1003)
                return false;

            // Robots for the options in the RAMA menu
            if (SciEngine.Instance.GameId == SciGameId.RAMA && (id >= 1004 && id <= 1009))
                return false;

            // TODO: The robot video in the Lighthouse demo gets stuck
            if (SciEngine.Instance.GameId == SciGameId.LIGHTHOUSE && id == 16)
                return false;

            string fileName = $"{id}.rbt";
            var stream = Core.Engine.OpenFileRead(fileName);

            if (stream == null)
            {
                Warning("Unable to open robot file {0}", fileName);
                return false;
            }

            using (stream)
            {
                return LoadStream(stream);
            }
        }
    }
}