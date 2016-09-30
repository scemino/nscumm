//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
using System.Collections.Generic;
using System.IO;
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using NScumm.Core.Common;
using NScumm.Core.Graphics;
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Video
{
    public class VMDDecoder : CoktelDecoder
    {
        enum PartType
        {
            Separator = 0,
            Audio = 1,
            Video = 2,
            File = 3,
            P4 = 4,
            Subtitle = 5
        }

        enum AudioFormat
        {
            Format8BitRaw = 0,
            Format16BitDpcm = 1,
            Format16BitAdpcm = 2
        }

        class File
        {
            public string name;

            public int offset;
            public int size;
            public int realSize;
        }

        class Part
        {
            public PartType type;
            public byte field_1;
            public byte field_E;
            public int size;
            public short left;
            public short top;
            public short right;
            public short bottom;
            public ushort id;
            public byte flags;
        }

        class Frame
        {
            public int offset;
            public Part[] parts;
        }

        Stream _stream;

        byte _version;
        uint _flags;

        uint _frameInfoOffset;
        ushort _partsPerFrame;
        Frame[] _frames;

        List<File> _files;

        // Sound properties
        ushort _soundFlags;
        short _soundFreq;
        short _soundSliceSize;
        short _soundSlicesCount;
        byte _soundBytesPerSample;
        byte _soundStereo; // (0: mono, 1: old-style stereo, 2: new-style stereo)
        int _soundHeaderSize;
        int _soundDataSize;
        uint _soundLastFilledFrame;
        AudioFormat _audioFormat;
        bool _autoStartSound;

        // Video properties
        bool _hasVideo;
        uint _videoCodec;
        byte _blitMode;
        byte _bytesPerPixel;

        uint _firstFramePos;

        ///< Position of the first frame's data within the stream.
        int _videoBufferSize;

        ///< Size of the video buffers.
        BytePtr[] _videoBuffer = new BytePtr[3];

        ///< Video buffers.
        int[] _videoBufferLen = new int[3];

        ///< Size of the video buffers filled.
        Surface[] _8bppSurface = new Surface[3];

        ///< Fake 8bpp surfaces over the video buffers.
        bool _externalCodec;

        Image.Codecs.Codec _codec;

        int _subtitle;

        bool _isPaletted;

        public VMDDecoder(IMixer mixer, SoundType soundType)
            : base(mixer, soundType)
        {
            //_audioFormat
            _subtitle = -1;
            _isPaletted = true;
            _autoStartSound = true;
        }

        public override PixelFormat PixelFormat
        {
            get
            {
                if (_externalCodec)
                {
                    if (_codec != null)
                        return _codec.PixelFormat;

                    // If we don't have the needed codec, just assume it's in the
                    // current screen format
                    return Engine.Instance.OSystem.GraphicsManager.PixelFormat;
                }

                if (_blitMode > 0)
                    return Engine.Instance.OSystem.GraphicsManager.PixelFormat;

                return PixelFormat.Indexed8;
            }
        }

        public override bool LoadStream(Stream stream)
        {
            Close();

            _stream = stream;
            var br = new BinaryReader(_stream);
            _stream.Seek(0, SeekOrigin.Begin);

            ushort headerLength;
            ushort handle;

            headerLength = br.ReadUInt16();
            handle = br.ReadUInt16();
            _version = (byte) br.ReadUInt16();

            // Version checking
            if (headerLength == 50)
            {
                // Newer version, used in Addy 5 upwards
                Warning("VMDDecoder::loadStream(): TODO: Addy 5 videos");
            }
            else if (headerLength == 814)
            {
                // Old version
                _features |= Features.Palette;
            }
            else
            {
                Warning("VMDDecoder::loadStream(): Version incorrect ({0}, {1}, {2})", headerLength, handle, _version);
                Close();
                return false;
            }

            _frameCount = br.ReadUInt16();

            _defaultX = br.ReadUInt16();
            _defaultY = br.ReadUInt16();
            _width = br.ReadUInt16();
            _height = br.ReadUInt16();

            _x = _defaultX;
            _y = _defaultY;

            if ((_width != 0) && (_height != 0))
            {
                _hasVideo = true;
                _features |= Features.Video;
            }
            else
                _hasVideo = false;

            _bytesPerPixel = 1;
            if ((_version & 4) != 0)
                _bytesPerPixel = (byte) (handle + 1);

            if (_bytesPerPixel > 3)
            {
                Warning("VMDDecoder::loadStream(): Requested %d bytes per pixel (%d, %d, %d)",
                    _bytesPerPixel, headerLength, handle, _version);
                Close();
                return false;
            }

            _flags = br.ReadUInt16();

            _partsPerFrame = br.ReadUInt16();
            _firstFramePos = br.ReadUInt32();

            _videoCodec = br.ReadUInt32BigEndian();

            if ((_features & Features.Palette) != 0)
            {
                for (int i = 0; i < 768; i++)
                    _palette[i] = (byte) (br.ReadByte() << 2);

                _paletteDirty = true;
            }

            uint videoBufferSize1 = br.ReadUInt32();
            uint videoBufferSize2 = br.ReadUInt32();

            _videoBufferSize = (int) Math.Max(videoBufferSize1, videoBufferSize2);

            if (_hasVideo)
            {
                if (!AssessVideoProperties())
                {
                    Close();
                    return false;
                }
            }

            _soundFreq = br.ReadInt16();
            _soundSliceSize = br.ReadInt16();
            _soundSlicesCount = br.ReadInt16();
            _soundFlags = br.ReadUInt16();

            _hasSound = _soundFreq != 0;

            if (_hasSound)
            {
                if (!AssessAudioProperties())
                {
                    Close();
                    return false;
                }
            }
            else
                _frameRate = new Rational(12);

            _frameInfoOffset = br.ReadUInt32();

            int numFiles;
            if (!ReadFrameTable(out numFiles))
            {
                Close();
                return false;
            }

            _stream.Seek(_firstFramePos, SeekOrigin.Begin);

            if (numFiles == 0)
                return true;

            _files = new List<File>(numFiles);
            if (!ReadFiles())
            {
                Close();
                return false;
            }

            _stream.Seek(_firstFramePos, SeekOrigin.Begin);
            return true;
        }

        private bool ReadFiles()
        {
            var ssize = _stream.Length;
            for (ushort i = 0; i < _frameCount; i++)
            {
                var br = new BinaryReader(_stream);
                _stream.Seek(_frames[i].offset, SeekOrigin.Begin);

                for (ushort j = 0; j < _partsPerFrame; j++)
                {
                    if (_frames[i].parts[j].type == PartType.Separator)
                        break;

                    if (_frames[i].parts[j].type == PartType.File)
                    {
                        File file = new File();

                        file.offset = (int) (_stream.Position + 20);
                        file.size = _frames[i].parts[j].size;
                        file.realSize = br.ReadInt32();

                        byte[] name = new byte[16];

                        _stream.Read(name, 0, 16);
                        name[15] = 0;

                        file.name = name.GetRawText();

                        _stream.Seek(_frames[i].parts[j].size - 20, SeekOrigin.Current);

                        if ((file.realSize >= ssize) || (file.name == ""))
                            continue;

                        _files.Add(file);
                    }
                    else
                        _stream.Seek(_frames[i].parts[j].size, SeekOrigin.Current);
                }
            }

            return true;
        }


        private bool ReadFrameTable(out int numFiles)
        {
            numFiles = 0;
            var br = new BinaryReader(_stream);
            _stream.Seek(_frameInfoOffset, SeekOrigin.Begin);
            _frames = new Frame[_frameCount];
            for (ushort i = 0; i < _frameCount; i++)
            {
                _frames[i].parts = new Part[_partsPerFrame];
                _stream.Seek(2, SeekOrigin.Current); // Unknown
                _frames[i].offset = br.ReadInt32();
            }

            _soundLastFilledFrame = 0;
            for (ushort i = 0; i < _frameCount; i++)
            {
                bool separator = false;

                for (ushort j = 0; j < _partsPerFrame; j++)
                {
                    _frames[i].parts[j].type = (PartType) br.ReadByte();
                    _frames[i].parts[j].field_1 = br.ReadByte();
                    _frames[i].parts[j].size = br.ReadInt32();

                    if (_frames[i].parts[j].type == PartType.Audio)
                    {
                        _frames[i].parts[j].flags = br.ReadByte();
                        _stream.Seek(9, SeekOrigin.Current); // Unknown

                        if (_frames[i].parts[j].flags != 3)
                            _soundLastFilledFrame = i;
                    }
                    else if (_frames[i].parts[j].type == PartType.Video)
                    {
                        _frames[i].parts[j].left = br.ReadInt16();
                        _frames[i].parts[j].top = br.ReadInt16();
                        _frames[i].parts[j].right = br.ReadInt16();
                        _frames[i].parts[j].bottom = br.ReadInt16();
                        _frames[i].parts[j].field_E = br.ReadByte();
                        _frames[i].parts[j].flags = br.ReadByte();
                    }
                    else if (_frames[i].parts[j].type == PartType.Subtitle)
                    {
                        _frames[i].parts[j].id = br.ReadUInt16();
                        // Speech text file name
                        _stream.Seek(8, SeekOrigin.Current);
                    }
                    else if (_frames[i].parts[j].type == PartType.File)
                    {
                        if (!separator)
                            numFiles++;
                        _stream.Seek(10, SeekOrigin.Current);
                    }
                    else if (_frames[i].parts[j].type == PartType.Separator)
                    {
                        separator = true;
                        _stream.Seek(10, SeekOrigin.Current);
                    }
                    else
                    {
                        // Unknown type
                        _stream.Seek(10, SeekOrigin.Current);
                    }
                }
            }

            return true;
        }


        private bool AssessAudioProperties()
        {
            bool supportedFormat = true;

            _features |= Features.Sound;

            _soundStereo = (byte) ((_soundFlags & 0x8000) != 0 ? 1 : ((_soundFlags & 0x200) != 0 ? 2 : 0));

            if (_soundSliceSize < 0)
            {
                _soundBytesPerSample = 2;
                _soundSliceSize = (short) -_soundSliceSize;

                if ((_soundFlags & 0x10) != 0)
                {
                    _audioFormat = AudioFormat.Format16BitAdpcm;
                    _soundHeaderSize = 3;
                    _soundDataSize = _soundSliceSize >> 1;

                    if (_soundStereo > 0)
                        supportedFormat = false;
                }
                else
                {
                    _audioFormat = AudioFormat.Format16BitDpcm;
                    _soundHeaderSize = 1;
                    _soundDataSize = _soundSliceSize;

                    if (_soundStereo == 1)
                    {
                        supportedFormat = false;
                    }
                    else if (_soundStereo == 2)
                    {
                        _soundDataSize = 2 * _soundDataSize + 2;
                        _soundHeaderSize = 4;
                    }
                }
            }
            else
            {
                _soundBytesPerSample = 1;
                _audioFormat = AudioFormat.Format8BitRaw;
                _soundHeaderSize = 0;
                _soundDataSize = _soundSliceSize;

                if (_soundStereo > 0)
                    supportedFormat = false;
            }

            if (!supportedFormat)
            {
                Warning(
                    "VMDDecoder::assessAudioProperties(): Unsupported audio format: {0} bits, encoding {1}, stereo {2}",
                    _soundBytesPerSample * 8, _audioFormat, _soundStereo);
                return false;
            }

            _frameRate = new Rational(_soundFreq, _soundSliceSize);

            _hasSound = true;
            _soundEnabled = true;
            _soundStage = SoundStage.Loaded;

            _audioStream = new QueuingAudioStream(_soundFreq, _soundStereo != 0);

            return true;
        }


        private bool AssessVideoProperties()
        {
            _isPaletted = true;

            if ((_version & 2) != 0 && (_version & 8) == 0)
            {
                _externalCodec = true;
                _videoBufferSize = 0;
            }
            else
                _externalCodec = false;

            if (!OpenExternalCodec())
                return false;

            if (_externalCodec)
                _blitMode = 0;
            else if (_bytesPerPixel == 1)
                _blitMode = 0;
            else if ((_bytesPerPixel == 2) || (_bytesPerPixel == 3))
            {
                int n = (_flags & 0x80) != 0 ? 2 : 3;

                _blitMode = (byte) (_bytesPerPixel - 1);
                _bytesPerPixel = (byte) n;

                _isPaletted = false;
            }

            if (_blitMode == 1)
            {
                _width /= _bytesPerPixel;
                _defaultX /= _bytesPerPixel;
                _x /= _bytesPerPixel;
            }

            if (_hasVideo)
            {
                int suggestedVideoBufferSize = _videoBufferSize;

                _videoBufferSize = _width * _height * _bytesPerPixel + 1000;

                if ((suggestedVideoBufferSize > _videoBufferSize) && (suggestedVideoBufferSize < 2097152))
                {
                    Warning("Suggested video buffer size greater than what should be needed (%d, %d, %dx%d",
                        suggestedVideoBufferSize, _videoBufferSize, _width, _height);

                    _videoBufferSize = suggestedVideoBufferSize;
                }

                for (int i = 0; i < 3; i++)
                {
                    _videoBuffer[i] = new byte[_videoBufferSize];

                    _8bppSurface[i] = new Surface((ushort)(_width * _bytesPerPixel), _height, _videoBuffer[i],
                        PixelFormat.Indexed8);
                }
            }

            return true;
        }

        private bool OpenExternalCodec()
        {
            throw new NotImplementedException();
//            _codec = null;
//
//            if (_externalCodec)
//            {
//                if (_videoCodec == kVideoCodecIndeo3)
//                {
//                    _isPaletted = false;
//
//                    _codec = new Image.Indeo3Decoder(_width, _height);
//
//                }
//                else
//                {
//                    Warning("VMDDecoder::openExternalCodec(): Unknown video codec FourCC \"{0}\"",
//                        Tag2str(_videoCodec));
//                    return false;
//                }
//            }
//
//            return true;
        }


        public override bool ReloadStream(Stream stream)
        {
            if (_stream == null)
                return false;

            stream.Seek(_stream.Position, SeekOrigin.Begin);
            _stream.Dispose();
            _stream = stream;

            return true;
        }

        public override bool Seek(int frame, SeekOrigin whence = SeekOrigin.Begin, bool restart = false)
        {
            if (!EvaluateSeekFrame(ref frame, whence))
                return false;

            if (frame == _curFrame)
                // Nothing to do
                return true;

            // Restart sound
            if (_hasSound && (frame == -1) &&
                ((_soundStage == SoundStage.None) || (_soundStage == SoundStage.Finished)))
            {
                _audioStream.Dispose();

                _soundStage = SoundStage.Loaded;
                _audioStream = new QueuingAudioStream(_soundFreq, _soundStereo != 0);
            }

            _subtitle = -1;

            if ((_blitMode > 0) && (_flags & 0x4000) != 0)
            {
                if (_curFrame > frame)
                {
                    _stream.Seek(_frames[0].offset, SeekOrigin.Begin);
                    _curFrame = -1;
                }

                while (frame > _curFrame)
                    DecodeNextFrame();

                return true;
            }

            // Seek
            _stream.Seek(_frames[frame + 1].offset, SeekOrigin.Begin);
            _curFrame = frame;
            _startTime = ServiceLocator.Platform.GetMilliseconds() - (frame + 2) * StaticTimeToNextFrame;

            return true;
        }

        public override Surface DecodeNextFrame()
        {
            if (!IsVideoLoaded || EndOfVideo)
                return null;

            CreateSurface();

            ProcessFrame();

            if (_curFrame == 0)
                _startTime = ServiceLocator.Platform.GetMilliseconds();

            return _surface;
        }

        private void ProcessFrame()
        {
            _curFrame++;
            _dirtyRects.Clear();
            _subtitle = -1;
            bool startSound = false;
            var br = new BinaryReader(_stream);
            for (ushort i = 0; i < _partsPerFrame; i++)
            {
                int pos = (int) _stream.Position;

                Part part = _frames[_curFrame].parts[i];

                if (part.type == PartType.Audio)
                {
                    if (part.flags == 1)
                    {
                        // Next sound slice data

                        if (_soundEnabled)
                        {
                            FilledSoundSlice(part.size);

                            if (_soundStage == SoundStage.Loaded)
                                startSound = true;
                        }
                        else
                            _stream.Seek(part.size, SeekOrigin.Current);
                    }
                    else if (part.flags == 2)
                    {
                        // Initial sound data (all slices)

                        if (_soundEnabled)
                        {
                            int mask = br.ReadInt32();
                            FilledSoundSlices(part.size - 4, mask);

                            if (_soundStage == SoundStage.Loaded)
                                startSound = true;
                        }
                        else
                            _stream.Seek(part.size, SeekOrigin.Current);
                    }
                    else if (part.flags == 3)
                    {
                        // Empty sound slice

                        if (_soundEnabled)
                        {
                            if ((uint) _curFrame < _soundLastFilledFrame)
                                EmptySoundSlice(_soundDataSize * _soundBytesPerSample);

                            if (_soundStage == SoundStage.Loaded)
                                startSound = true;
                        }

                        _stream.Seek(part.size, SeekOrigin.Current);
                    }
                    else if (part.flags == 4)
                    {
                        Warning("VMDDecoder::processFrame(): TODO: Addy 5 sound type 4 (%d)", part.size);
                        DisableSound();
                        _stream.Seek(part.size, SeekOrigin.Current);
                    }
                    else
                    {
                        Warning("VMDDecoder::processFrame(): Unknown sound type %d", part.flags);
                        _stream.Seek(part.size, SeekOrigin.Current);
                    }

                    _stream.Seek(pos + part.size, SeekOrigin.Begin);
                }
                else if ((part.type == PartType.Video) && !_hasVideo)
                {
                    Warning("VMDDecoder::processFrame(): Header claims there's no video, but video found (%d)",
                        part.size);
                    _stream.Seek(part.size, SeekOrigin.Current);
                }
                else if ((part.type == PartType.Video) && _hasVideo)
                {
                    int size = part.size;

                    // New palette
                    if ((part.flags & 2) != 0)
                    {
                        byte index = br.ReadByte();
                        byte count = br.ReadByte();

                        for (int j = 0; j < (count + 1) * 3; j++)
                            _palette[index * 3 + j] = (byte) (br.ReadByte() << 2);

                        _stream.Seek((255 - count) * 3, SeekOrigin.Current);

                        _paletteDirty = true;

                        size -= 768 + 2;
                    }

                    _stream.Read(_videoBuffer[0].Data, _videoBuffer[0].Offset, size);
                    _videoBufferLen[0] = size;

                    Rect rect = new Rect(part.left, part.top, (short)(part.right + 1), (short)(part.bottom + 1));
                    if (RenderFrame(ref rect))
                        _dirtyRects.Add(rect);
                }
                else if (part.type == PartType.Separator)
                {
                    // Ignore
                }
                else if (part.type == PartType.File)
                {
                    // Ignore
                    _stream.Seek(part.size, SeekOrigin.Current);
                }
                else if (part.type == PartType.P4)
                {
                    // Unknown, ignore
                    _stream.Seek(part.size, SeekOrigin.Current);
                }
                else if (part.type == PartType.Subtitle)
                {
                    _subtitle = part.id;
                    _stream.Seek(part.size, SeekOrigin.Current);
                }
                else
                {
                    Warning("VMDDecoder::processFrame(): Unknown frame part type %d, size %d (%d of %d)",
                        part.type, part.size, i + 1, _partsPerFrame);
                }
            }

            if (startSound && _soundEnabled)
            {
                if (_hasSound && _audioStream != null)
                {
                    if (_autoStartSound)
                        _audioHandle = _mixer.PlayStream(_soundType, _audioStream, -1, Mixer.MaxChannelVolume, 0, false);
                    _soundStage = SoundStage.Playing;
                }
                else
                    _soundStage = SoundStage.None;
            }

            if (((uint) _curFrame == _frameCount - 1) && (_soundStage == (SoundStage) 2))
            {
                _audioStream.Finish();
                _soundStage = SoundStage.Finished;
            }
        }

        public void SetAutoStartSound(bool autoStartSound)
        {
            _autoStartSound = autoStartSound;
        }

        private bool RenderFrame(ref Rect rect)
        {
            Rect realRect, fakeRect;
            if (!GetRenderRects(ref rect, out realRect, out fakeRect))
                return false;

            if (_externalCodec)
            {
                if (_codec == null)
                    return false;

                Stream frameStream = new MemoryStream(_videoBuffer[0].Data, _videoBuffer[0].Offset, _videoBufferLen[0]);
                Surface codecSurf = _codec.DecodeFrame(frameStream);
                if (codecSurf == null)
                    return false;

                rect = new Rect((short) _x, (short) _y, (short) (_x + codecSurf.Width), (short) (_y + codecSurf.Height));
                rect.Clip(new Rect((short) _x, (short) _y, (short) (_x + _width), (short) (_y + _height)));

                RenderBlockWhole(_surface, codecSurf.Pixels, ref rect);
                return true;
            }

            byte srcBuffer = 0;
            var dataPtr = _videoBuffer[srcBuffer];
            int dataSize = _videoBufferLen[srcBuffer] - 1;

            byte type = dataPtr.Value;
            dataPtr.Offset++;

            if ((type & 0x80) != 0)
            {
                // Frame data is compressed

                type &= 0x7F;

                if ((type == 2) && (rect.Width == _surface.Width) && (_x == 0) && (_blitMode == 0))
                {
                    // Directly uncompress onto the video surface
                    int offsetX = rect.Left * _surface.BytesPerPixel;
                    int offsetY = (_y + rect.Top) * _surface.Pitch;
                    int offset = offsetX + offsetY;

                    if (DeLZ77(new BytePtr(_surface.Pixels, offset), dataPtr, dataSize,
                            _surface.Width * _surface.Height * _surface.BytesPerPixel - offset) != 0)
                        return true;
                }

                srcBuffer = 1;
                _videoBufferLen[srcBuffer] =
                    DeLZ77(_videoBuffer[srcBuffer], dataPtr, dataSize, _videoBufferSize);

                dataPtr = _videoBuffer[srcBuffer];
                dataSize = _videoBufferLen[srcBuffer];
            }

            Rect blockRect = fakeRect;
            Surface surface = _surface;
            if (_blitMode == 0)
            {
                blockRect = new Rect((short) (blockRect.Left + _x), (short) (blockRect.Top + _y),
                    (short) (blockRect.Right + _x), (short) (blockRect.Bottom + _y));
            }
            else
            {
                surface = _8bppSurface[2];
            }

            // Evaluate the block type
            if (type == 0x01)
                RenderBlockSparse(surface, dataPtr, ref blockRect);
            else if (type == 0x02)
                RenderBlockWhole(surface, dataPtr, ref blockRect);
            else if (type == 0x03)
                RenderBlockRLE(surface, dataPtr, ref blockRect);
            else if (type == 0x42)
                RenderBlockWhole4X(surface, dataPtr, ref blockRect);
            else if ((type & 0x0F) == 0x02)
                RenderBlockWhole2Y(surface, dataPtr, ref blockRect);
            else
                RenderBlockSparse2Y(surface, dataPtr, ref blockRect);

            if (_blitMode > 0)
            {
                if (_bytesPerPixel == 2)
                    Blit16(surface, ref blockRect);
                else if (_bytesPerPixel == 3)
                    Blit24(surface, ref blockRect);

                blockRect = new Rect((short) (blockRect.Left + _x), (short) (blockRect.Top + _y),
                    (short) (blockRect.Right + _x), (short) (blockRect.Bottom + _y));
            }

            rect = blockRect;
            return true;
        }

        private bool GetRenderRects(ref Rect rect, out Rect realRect, out Rect fakeRect)
        {
            realRect = rect;
            fakeRect = rect;

            if (_blitMode == 0)
            {
                realRect = new Rect((short) (realRect.Left - _x), (short) (realRect.Top - _y),
                    (short) (realRect.Right - _x), (short) (realRect.Bottom - _y));

                fakeRect = new Rect((short) (fakeRect.Left - _x), (short) (fakeRect.Top - _y),
                    (short) (fakeRect.Right - _x), (short) (fakeRect.Bottom - _y));
            }
            else if (_blitMode == 1)
            {
                realRect = new Rect((short) (rect.Left / _bytesPerPixel), rect.Top,
                    (short) (rect.Right / _bytesPerPixel), rect.Bottom);

                realRect = new Rect((short) (realRect.Left - _x), (short) (realRect.Top - _y),
                    (short) (realRect.Right - _x), (short) (realRect.Bottom - _y));

                fakeRect = new Rect((short) (fakeRect.Left - _x * _bytesPerPixel), (short) (fakeRect.Top - _y),
                    (short) (fakeRect.Right - _x * _bytesPerPixel), (short) (fakeRect.Bottom - _y));
            }
            else if (_blitMode == 2)
            {
                fakeRect = new Rect((short) (rect.Left * _bytesPerPixel), rect.Top,
                    (short) (rect.Right * _bytesPerPixel), rect.Bottom);

                realRect = new Rect((short) (realRect.Left - _x), (short) (realRect.Top - _y),
                    (short) (realRect.Right - _x), (short) (realRect.Bottom - _y));

                fakeRect = new Rect((short) (fakeRect.Left - _x * _bytesPerPixel), (short) (fakeRect.Top - _y),
                    (short) (fakeRect.Right - _x * _bytesPerPixel), (short) (fakeRect.Bottom - _y));
            }

            realRect.Clip(new Rect((short) _surface.Width, (short) _surface.Height));
            fakeRect.Clip(new Rect((short) (_surface.Width * _bytesPerPixel), (short) _surface.Height));

            if (!realRect.IsValidRect || realRect.IsEmpty)
                return false;
            if (!fakeRect.IsValidRect || realRect.IsEmpty)
                return false;

            return true;
        }


        private void Blit16(Surface srcSurf, ref Rect rect)
        {
            rect = new Rect((short) (rect.Left / 2), rect.Top, (short) (rect.Right / 2), rect.Bottom);

            Rect srcRect = rect;

            rect.Clip((short) _surface.Width, (short) _surface.Height);

            PixelFormat pixelFormat = PixelFormat;

            // We cannot use getBasePtr here because srcSurf.format.bytesPerPixel is
            // different from _bytesPerPixel.
            BytePtr src = new BytePtr(srcSurf.Pixels,
                srcRect.Top * srcSurf.Pitch + srcRect.Left * _bytesPerPixel);
            BytePtr dst = _surface.GetBasePtr(_x + rect.Left, _y + rect.Top);

            for (int i = 0; i < rect.Height; i++)
            {
                var srcRow = src;
                var dstRow = dst;

                for (int j = 0; j < rect.Width; j++, srcRow.Offset += 2, dstRow.Offset += _surface.BytesPerPixel)
                {
                    ushort data = srcRow.ToUInt16();

                    byte r = (byte) (((data & 0x7C00) >> 10) << 3);
                    byte g = (byte) (((data & 0x03E0) >> 5) << 3);
                    byte b = (byte) (((data & 0x001F) >> 0) << 3);

                    int c = pixelFormat.RGBToColor(r, g, b);
                    if ((r == 0) && (g == 0) && (b == 0))
                        c = 0;

                    if (_surface.BytesPerPixel == 2)
                        dstRow.WriteUInt16(0, (ushort) c);
                    else if (_surface.BytesPerPixel == 4)
                        dstRow.WriteInt32(0, c);
                }

                src.Offset += srcSurf.Pitch;
                dst.Offset += _surface.Pitch;
            }
        }

        private void Blit24(Surface srcSurf, ref Rect rect)
        {
            rect = new Rect((short) (rect.Left / 3), rect.Top, (short) (rect.Right / 3), rect.Bottom);

            Rect srcRect = rect;

            rect.Clip((short) _surface.Width, (short) _surface.Height);

            PixelFormat pixelFormat = PixelFormat;

            // We cannot use getBasePtr here because srcSurf.format.bytesPerPixel is
            // different from _bytesPerPixel.
            BytePtr src = new BytePtr(srcSurf.Pixels,
                srcRect.Top * srcSurf.Pitch + srcRect.Left * _bytesPerPixel);
            BytePtr dst = _surface.GetBasePtr(_x + rect.Left, _y + rect.Top);

            for (int i = 0; i < rect.Height; i++)
            {
                BytePtr srcRow = src;
                BytePtr dstRow = dst;

                for (int j = 0; j < rect.Width; j++, srcRow.Offset += 3, dstRow.Offset += _surface.BytesPerPixel)
                {
                    byte r = srcRow[2];
                    byte g = srcRow[1];
                    byte b = srcRow[0];

                    int c = pixelFormat.RGBToColor(r, g, b);
                    if ((r == 0) && (g == 0) && (b == 0))
                        c = 0;

                    if (_surface.BytesPerPixel == 2)
                        dstRow.WriteUInt16(0, (ushort) c);
                    else if (_surface.BytesPerPixel == 4)
                        dstRow.WriteInt32(0, c);
                }

                src.Offset += srcSurf.Pitch;
                dst.Offset += _surface.Pitch;
            }
        }

        private void FilledSoundSlice(int size)
        {
            if (_audioStream == null)
            {
                _stream.Seek(size, SeekOrigin.Current);
                return;
            }

            var data = _stream.ReadStream(size);
            IAudioStream sliceStream = null;

            if (_audioFormat == AudioFormat.Format8BitRaw)
                sliceStream = Create8bitRaw(data);
            else if (_audioFormat == AudioFormat.Format16BitDpcm)
                sliceStream = Create16bitDPCM(data);
            else if (_audioFormat == AudioFormat.Format16BitAdpcm)
                sliceStream = Create16bitADPCM(data);

            if (sliceStream != null)
                _audioStream.QueueAudioStream(sliceStream);
        }

        private void FilledSoundSlices(int size, int mask)
        {
            bool[] fillInfo = new bool[32];

            byte max;
            byte n = EvaluateMask(mask, fillInfo, out max);

            int extraSize = size - n * _soundDataSize;

            if (_soundSlicesCount > 32)
                extraSize -= (_soundSlicesCount - 32) * _soundDataSize;

            if (n > 0)
                extraSize /= n;

            for (byte i = 0; i < max; i++)
                if (fillInfo[i])
                    FilledSoundSlice(_soundDataSize + extraSize);
                else
                    EmptySoundSlice(_soundDataSize * _soundBytesPerSample);

            if (_soundSlicesCount > 32)
                FilledSoundSlice((_soundSlicesCount - 32) * _soundDataSize + _soundHeaderSize);
        }

        private void EmptySoundSlice(int size)
        {
            var soundBuf = new byte[size];
            AudioFlags flags = 0;
            flags |= _soundBytesPerSample == 2 ? AudioFlags.Is16Bits : 0;
            flags |= _soundStereo > 0 ? AudioFlags.Stereo : 0;

            _audioStream.QueueBuffer(soundBuf, size, true, flags);
        }

        private byte EvaluateMask(int mask, bool[] fillInfo, out byte max)
        {
            max = (byte) Math.Min(_soundSlicesCount - 1, 31);

            byte n = 0;
            for (var i = 0; i < max; i++)
            {
                if ((mask & 1) == 0)
                {
                    n++;
                    fillInfo[i] = true;
                }
                else
                    fillInfo[i] = false;

                mask >>= 1;
            }

            return n;
        }


        private IAudioStream Create8bitRaw(Stream stream)
        {
            AudioFlags flags = AudioFlags.Unsigned;

            if (_soundStereo != 0)
                flags |= AudioFlags.Stereo;

            return new RawStream(flags, _soundFreq, true, stream);
        }

        private IAudioStream Create16bitDPCM(Stream stream)
        {
            throw new NotImplementedException();
            //return new DPCMStream(stream, _soundFreq, _soundStereo == 0 ? 1 : 2);
        }

        private IAudioStream Create16bitADPCM(Stream stream)
        {
            throw new NotImplementedException();
            //return new VMD_ADPCMStream(stream, true, _soundFreq, _soundStereo == 0 ? 1 : 2);
        }
    }
}