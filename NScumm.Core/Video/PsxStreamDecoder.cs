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
using System.Linq;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using System.Diagnostics;

namespace NScumm.Core.Video
{
    // CD speed in sectors/second
    // Calling code should use these enum values instead of the constants
    public enum CDSpeed
    {
        CD1x = 75,
        CD2x = 150
    }

    /// <summary>
    /// Decoder for PSX stream videos.
    /// This currently implements the most basic PSX stream format that is
    /// used by most games on the system. Special variants are not supported
    /// at this time.
    /// </summary>
    public partial class PsxStreamDecoder : VideoDecoder
    {
        private const int RAW_CD_SECTOR_SIZE = 2352;
        private const int CDXA_TYPE_MASK = 0x0E;
        private const int CDXA_TYPE_DATA = 0x08;
        private const int CDXA_TYPE_AUDIO = 0x04;
        private const int CDXA_TYPE_VIDEO = 0x02;

        private const int VIDEO_DATA_CHUNK_SIZE = 2016;
        private const int VIDEO_DATA_HEADER_SIZE = 56;

        private CDSpeed _speed;
        private uint _frameCount;
        private Stream _stream;
        private PsxVideoTrack _videoTrack;
        private PsxAudioTrack _audioTrack;
        private PixelFormat _screenFormat;
        private IMixer _mixer;

        private static readonly byte[] s_syncHeader = { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00 };


        public override bool UseAudioSync
        {
            get { return false; }
        }

        public PsxStreamDecoder(IMixer mixer, CDSpeed speed, PixelFormat screenFormat, uint frameCount = 0)
        {
            _mixer = mixer;
            _speed = speed;
            _screenFormat = screenFormat;
            _frameCount = frameCount;
        }

        public override bool LoadStream(Stream stream)
        {
            Close();

            _stream = stream;
            ReadNextPacket();

            return true;
        }

        public override void Close()
        {
            base.Close();
            _audioTrack = null;
            _videoTrack = null;
            _frameCount = 0;

            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        protected override void ReadNextPacket()
        {
            Stream sector = null;
            BinaryReader br = null;
            byte[] partialFrame = null;
            int sectorsRead = 0;

            while (_stream.Position < _stream.Length)
            {
                sector = ReadSector();
                sectorsRead++;

                if (sector == null)
                    throw new InvalidOperationException("Corrupt PSX stream sector");
                br = new BinaryReader(sector);

                sector.Seek(0x11, SeekOrigin.Begin);
                byte track = (byte)sector.ReadByte();
                if (track >= 32)
                    throw new InvalidOperationException("Bad PSX stream track");

                byte sectorType = (byte)(br.ReadByte() & CDXA_TYPE_MASK);

                switch (sectorType)
                {
                    case CDXA_TYPE_DATA:
                    case CDXA_TYPE_VIDEO:
                        if (track == 1)
                        {
                            if (_videoTrack == null)
                            {
                                _videoTrack = new PsxVideoTrack(sector, _speed, _frameCount, _screenFormat);
                                AddTrack(_videoTrack);
                            }

                            sector.Seek(28, SeekOrigin.Begin);
                            ushort curSector = br.ReadUInt16();
                            ushort sectorCount = br.ReadUInt16();
                            br.ReadUInt32();
                            ushort frameSize = (ushort)br.ReadUInt32();

                            if (curSector >= sectorCount)
                                throw new InvalidOperationException("Bad sector");

                            if (partialFrame == null)
                                partialFrame = new byte[sectorCount * VIDEO_DATA_CHUNK_SIZE];

                            sector.Seek(VIDEO_DATA_HEADER_SIZE, SeekOrigin.Begin);
                            sector.Read(partialFrame, curSector * VIDEO_DATA_CHUNK_SIZE, VIDEO_DATA_CHUNK_SIZE);

                            if (curSector == sectorCount - 1)
                            {
                                // Done assembling the frame
                                using (var frame = new MemoryStream(partialFrame, 0, frameSize))
                                {
                                    _videoTrack.DecodeFrame(frame, sectorsRead);
                                }
                                sector.Dispose();
                                return;
                            }
                        }
                        else
                            throw new InvalidOperationException("Unhandled multi-track video");
                        break;
                    case CDXA_TYPE_AUDIO:
                        // We only handle one audio channel so far
                        if (track == 1)
                        {
                            if (_audioTrack == null)
                            {
                                _audioTrack = new PsxAudioTrack(_mixer, sector);
                                AddTrack(_audioTrack);
                            }

                            _audioTrack.QueueAudioFromSector(sector);
                        }
                        else {
                            // TODO: warning("Unhandled multi-track audio");
                        }
                        break;
                    default:
                        // This shows up way too often, but the other sectors
                        // are safe to ignore
                        //TODO: warning("Unknown PSX sector type 0x%x", sectorType);
                        break;
                }

                sector.Dispose();
                sector = null;
            }

            if (_stream.Position >= _stream.Length)
            {
                if (_videoTrack != null)
                    _videoTrack.SetEndOfTrack();

                if (_audioTrack != null)
                    _audioTrack.SetEndOfTrack();
            }
        }

        private Stream ReadSector()
        {
            Debug.Assert(_stream != null);

            var br = new BinaryReader(_stream);
            var stream = new MemoryStream(br.ReadBytes(RAW_CD_SECTOR_SIZE));

            var syncHeader = new byte[12];
            stream.Read(syncHeader, 0, 12);
            if (s_syncHeader.SequenceEqual(syncHeader))
                return stream;

            return null;
        }
    }
}
