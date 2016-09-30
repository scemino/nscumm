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
using NScumm.Core.Graphics;
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Video
{
    public abstract class CoktelDecoder
    {
        private class State
        {
            /** Set accordingly to what was done. */
            public uint flags;
            /** The id of the spoken words. */
            public ushort speechId;
        }

        protected enum SoundStage
        {
            None = 0,

            ///< No sound.
            Loaded = 1,

            ///< Sound loaded.
            Playing = 2,

            ///< Sound is playing.
            Finished = 3 ///< No more new sound data.
        }

        [Flags]
        protected enum Features
        {
            None = 0x0000,
            Palette = 0x0008,

            ///< Has an own palette.
            DataSize = 0x0020,

            ///< Suggests a data size.
            Sound = 0x0040,

            ///< Has sound.
            FrameCoords = 0x0080,

            ///< Has specific frame coordinates.
            StdCoords = 0x0100,

            ///< Has general standard coordinates.
            FramePos = 0x0200,

            ///< Has a frame positions table.
            Video = 0x0400 ///< Has video.
        }

        protected IMixer _mixer;
        protected SoundType _soundType;

        protected ushort _width;
        protected ushort _height;

        protected ushort _x;
        protected ushort _y;

        protected ushort _defaultX;
        protected ushort _defaultY;

        protected Features _features;

        protected int _curFrame;
        protected int _frameCount;

        protected int _startTime;

        protected byte[] _palette = new byte[768];
        protected bool _paletteDirty;

        protected bool _ownSurface;
        protected Surface _surface;

        protected List<Rect> _dirtyRects;

        protected Rational _frameRate;

        // Current sound state
        protected bool _hasSound;
        protected bool _soundEnabled;
        protected SoundStage _soundStage;

        protected IQueuingAudioStream _audioStream;
        protected SoundHandle _audioHandle;

        private int _pauseStartTime;
        private bool _isPaused;

        Stream _stream;

        // Buffer for processed frame data
        byte[] _videoBuffer;
        uint _videoBufferSize;

        public bool IsVideoLoaded => _stream != null;

        public bool IsSoundEnabled => _soundEnabled;

        public bool IsSoundPlaying => _audioStream != null && _mixer.IsSoundHandleActive(_audioHandle);

        public bool HasVideo => true;

        public bool HasPalette => (_features & Features.Palette) != 0;

        public bool HasSound => _hasSound;

        public abstract PixelFormat PixelFormat { get; }

        public bool EndOfVideo => !IsVideoLoaded || (CurFrame >= FrameCount - 1);

        public Surface Surface => !IsVideoLoaded ? null : _surface;

        protected bool HasSurface => _surface.Pixels != null;

        /** Return the current subtitle index. */
        public int SubtitleIndex => -1;

        /** Is the video paletted or true color? */
        public bool IsPaletted => true;

        /**
         * Get the current frame
         * @see VideoDecoder::getCurFrame()
         */
        public int CurFrame => _curFrame;

        /** Get the Mixer SoundType audio is being played with. */
        public SoundType SoundType => _soundType;

        /** Get the AudioStream for the audio. */
        public IAudioStream AudioStream => _audioStream;

        public ushort Width => _width;

        public ushort Height => _height;

        public int FrameCount => _frameCount;

        public byte[] Palette
        {
            get
            {
                _paletteDirty = false;
                return _palette;
            }
        }

        public bool HasDirtyPalette => (_features & Features.Palette) != 0 && _paletteDirty;

        public Rational FrameRate => _frameRate;

        public int StaticTimeToNextFrame => 1000 / _frameRate;


        public CoktelDecoder(IMixer mixer, SoundType soundType)
        {
            _ownSurface = true;
            _frameRate = new Rational(12);
        }

        /** Replace the current video stream with this identical one. */
        public abstract bool ReloadStream(Stream stream);

        public abstract bool Seek(int frame, SeekOrigin whence = SeekOrigin.Begin, bool restart = false);

        /** Draw directly onto the specified video memory. */

        public void SetSurfaceMemory(byte[] mem, ushort width, ushort height, byte bpp)
        {
            FreeSurface();

            if (!HasVideo)
                return;

            // Sanity checks
            System.Diagnostics.Debug.Assert((width > 0) && (height > 0));
            System.Diagnostics.Debug.Assert(bpp == Surface.GetBytesPerPixel(PixelFormat));

            // Create a surface over this memory
            // TODO: Check whether it is fine to assume we want the setup PixelFormat.
            _surface = new Surface(width, height, mem, PixelFormat);

            _ownSurface = false;
        }

        /** Reset the video memory. */

        private void SetSurfaceMemory()
        {
            FreeSurface();
            CreateSurface();

            _ownSurface = true;
        }

        /** Draw the video starting at this position within the video memory. */

        public void SetXY(ushort x, ushort y)
        {
            _x = x;
            _y = y;
        }

        /** Draw the video at the default position. */

        public void SetXY()
        {
            SetXY(_defaultX, _defaultY);
        }

        /** Override the video's frame rate. */

        public void SetFrameRate(Rational frameRate)
        {
            _frameRate = frameRate;
        }

        /** Get the video's frame rate. */

        public Rational GetFrameRate()
        {
            return _frameRate;
        }

        public ushort GetDefaultX()
        {
            return _defaultX;
        }

        public ushort GetDefaultY()
        {
            return _defaultY;
        }

        public List<Rect> GetDirtyRects()
        {
            return _dirtyRects;
        }

        public void EnableSound()
        {
            if (!HasSound || IsSoundEnabled)
                return;

            // Sanity check
            if (_mixer.OutputRate == 0)
                return;

            // Only possible on the first frame
            if (_curFrame > -1)
                return;

            _soundEnabled = true;
        }

        public void DisableSound()
        {
            if (_audioStream != null)
            {
                if ((_soundStage == SoundStage.Playing) || (_soundStage == SoundStage.Finished))
                {
                    _audioStream.Finish();
                    _mixer.StopHandle(_audioHandle);
                }

                _audioStream.Dispose();
            }

            _soundEnabled = false;
            _soundStage = SoundStage.None;

            _audioStream = null;
        }

        public void FinishSound()
        {
            if (_audioStream == null)
                return;

            _audioStream.Finish();
            _soundStage = SoundStage.Finished;
        }

        public virtual void ColorModeChanged()
        {
        }

        public virtual bool GetFrameCoords(short frame, ref short x, ref short y, ref short width, ref short height)
        {
            return false;
        }

        public virtual bool HasEmbeddedFiles => false;

        public virtual bool HasEmbeddedFile(string fileName)
        {
            return false;
        }

        public virtual Stream GetEmbeddedFile(string fileName)
        {
            return null;
        }

        /**
         * Decode the next frame
         * @see VideoDecoder::decodeNextFrame()
         */
        public abstract Surface DecodeNextFrame();

        /**
         * Load a video from a stream
         * @see VideoDecoder::loadStream()
         */
        public abstract bool LoadStream(Stream stream);

        /** Close the video. */

        public void Close()
        {
            DisableSound();
            FreeSurface();

            _x = 0;
            _y = 0;

            _defaultX = 0;
            _defaultY = 0;

            _features = 0;

            _curFrame = -1;
            _frameCount = 0;

            _startTime = 0;

            _hasSound = false;

            _isPaused = false;
        }

        public void PauseVideo(bool pause)
        {
            if (_isPaused != pause)
            {
                if (_isPaused)
                {
                    // Add the time we were paused to the initial starting time
                    _startTime += ServiceLocator.Platform.GetMilliseconds() - _pauseStartTime;
                }
                else
                {
                    // Store the time we paused for use later
                    _pauseStartTime = ServiceLocator.Platform.GetMilliseconds();
                }

                _isPaused = pause;
            }
        }

        // A whole, completely filled block
        protected void RenderBlockWhole(Surface dstSurf, BytePtr src, ref Rect rect)
        {
            var srcRect = rect;

            rect.Clip((short) dstSurf.Width, (short) dstSurf.Height);
            var dst = dstSurf.GetBasePtr(rect.Left, rect.Top);
            for (var i = 0; i < rect.Height; i++)
            {
                Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, rect.Width * dstSurf.BytesPerPixel);

                src.Offset += srcRect.Width * dstSurf.BytesPerPixel;
                dst.Offset += dstSurf.Pitch;
            }
        }

        protected void RenderBlockRLE(Surface dstSurf, BytePtr src, ref Rect rect)
        {
            Rect srcRect = rect;

            rect.Clip((short) dstSurf.Width, (short) dstSurf.Height);

            BytePtr dst = dstSurf.GetBasePtr(rect.Left, rect.Top);
            for (int i = 0; i < rect.Height; i++)
            {
                BytePtr dstRow = dst;
                short pixWritten = 0;

                while (pixWritten < srcRect.Width)
                {
                    short pixCount = src.Value;
                    src.Offset++;

                    if ((pixCount & 0x80) != 0)
                    {
                        pixCount = (short) Math.Min((pixCount & 0x7F) + 1, srcRect.Width - pixWritten);
                        short copyCount = (short) ScummHelper.Clip(rect.Width - pixWritten, 0, pixCount);

                        if (src.Value != 0xFF)
                        {
                            // Normal copy

                            Array.Copy(src.Data, src.Offset, dstRow.Data, dstRow.Offset, copyCount);
                            dstRow.Offset += copyCount;
                            src.Offset += pixCount;
                        }
                        else
                            DeRLE(ref dstRow, ref src, copyCount, pixCount);

                        pixWritten += pixCount;
                    }
                    else
                    {
                        // "Hole"
                        short copyCount = (short) ScummHelper.Clip(rect.Width - pixWritten, 0, pixCount + 1);

                        dstRow.Offset += copyCount;
                        pixWritten = (short) (pixWritten + pixCount + 1);
                    }
                }

                dst.Offset += dstSurf.Pitch;
            }
        }


        // A quarter-wide whole, completely filled block
        protected void RenderBlockWhole4X(Surface dstSurf, BytePtr src, ref Rect rect)
        {
            var srcRect = rect;

            rect.Clip((short) dstSurf.Width, (short) dstSurf.Height);

            var dst = dstSurf.GetBasePtr(rect.Left, rect.Top);
            for (var i = 0; i < rect.Height; i++)
            {
                var dstRow = dst;
                var srcRow = new BytePtr(src);

                var count = rect.Width;
                while (count >= 0)
                {
                    dstRow.Data.Set(srcRow.Offset, srcRow.Value, Math.Min((int)count, 4));

                    count -= 4;
                    dstRow.Offset += 4;
                    srcRow.Offset += 1;
                }

                src.Offset += srcRect.Width / 4;
                dst.Offset += dstSurf.Pitch;
            }
        }

        // A half-high whole, completely filled block
        protected void RenderBlockWhole2Y(Surface dstSurf, BytePtr src, ref Rect rect)
        {
            var srcRect = rect;

            rect.Clip((short) dstSurf.Width, (short) dstSurf.Height);

            var height = rect.Height;

            var dst = dstSurf.GetBasePtr(rect.Left, rect.Top);
            while (height > 1)
            {
                Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, rect.Width);
                Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset + dstSurf.Pitch, rect.Width);

                height -= 2;
                src.Offset += srcRect.Width;
                dst.Offset += 2 * dstSurf.Pitch;
            }

            if (height == 1)
                Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, rect.Width);
        }

        // A sparse block
        protected void RenderBlockSparse(Surface dstSurf, BytePtr src, ref Rect rect)
        {
            var srcRect = rect;

            rect.Clip((short) dstSurf.Width, (short) dstSurf.Height);

            var dst = dstSurf.GetBasePtr(rect.Left, rect.Top);
            for (var i = 0; i < rect.Height; i++)
            {
                var dstRow = new BytePtr(dst);
                var pixWritten = 0;

                while (pixWritten < srcRect.Width)
                {
                    short pixCount = src.Value;
                    src.Offset++;

                    if ((pixCount & 0x80) != 0)
                    {
                        // Data
                        short copyCount;

                        pixCount = (short) Math.Min((pixCount & 0x7F) + 1, srcRect.Width - pixWritten);
                        copyCount = (short) ScummHelper.Clip(rect.Width - pixWritten, 0, pixCount);
                        Array.Copy(src.Data, src.Offset, dstRow.Data, dstRow.Offset, copyCount);

                        pixWritten += pixCount;
                        dstRow.Offset += pixCount;
                        src.Offset += pixCount;
                    }
                    else
                    {
                        // "Hole"
                        pixWritten += pixCount + 1;
                        dstRow.Offset += pixCount + 1;
                    }
                }

                dst.Offset += dstSurf.Pitch;
            }
        }

        // A half-high sparse block
        protected void RenderBlockSparse2Y(Surface dstSurf, BytePtr src, ref Rect rect)
        {
            Warning("renderBlockSparse2Y");

            var srcRect = rect;

            rect.Clip((short) dstSurf.Width, (short) dstSurf.Height);

            var dst = dstSurf.GetBasePtr(rect.Left, rect.Top);
            for (var i = 0; i < rect.Height; i += 2)
            {
                var dstRow = new BytePtr(dst);
                var pixWritten = 0;

                while (pixWritten < srcRect.Width)
                {
                    short pixCount = src.Value;
                    src.Offset++;

                    if ((pixCount & 0x80) != 0)
                    {
                        // Data
                        pixCount = (short) Math.Min((pixCount & 0x7F) + 1, srcRect.Width - pixWritten);
                        Array.Copy(src.Data, src.Offset, dstRow.Data, dstRow.Offset, pixCount);
                        Array.Copy(src.Data, src.Offset, dstRow.Data, dstRow.Offset + dstSurf.Pitch, pixCount);

                        pixWritten += pixCount;
                        dstRow.Offset += pixCount;
                        src.Offset += pixCount;
                    }
                    else
                    {
                        // "Hole"
                        pixWritten += pixCount + 1;
                        dstRow.Offset += pixCount + 1;
                    }
                }

                dst.Offset += dstSurf.Pitch;
            }
        }

        public int GetTimeToNextFrame()
        {
            if (EndOfVideo || _curFrame < 0)
                return 0;

            var elapsedTime = ServiceLocator.Platform.GetMilliseconds() - _startTime;
            var nextFrameStartTime = new Rational((_curFrame + 1) * 1000) / FrameRate;

            if (nextFrameStartTime <= elapsedTime)
                return 0;

            return nextFrameStartTime - elapsedTime;
        }

        protected void CreateSurface()
        {
            if (HasSurface)
                return;

            if (!HasVideo)
                return;

            if ((_width > 0) && (_height > 0))
                _surface = new Surface(_width, _height, PixelFormat);

            _ownSurface = true;
        }

        protected bool EvaluateSeekFrame(ref int frame, SeekOrigin whence)
        {
            if (!IsVideoLoaded)
                // Nothing to do
                return false;

            // Find the frame to which to seek
            if (whence == SeekOrigin.Current)
                frame += _curFrame;
            else if (whence == SeekOrigin.End)
                frame = (int) (_frameCount - frame - 1);
            else if (whence == SeekOrigin.Begin)
                frame--;
            else
                return false;

            if ((frame < -1) || (frame >= (int) _frameCount))
                // Out of range
                return false;

            return true;
        }

        protected void FreeSurface()
        {
            if (!_ownSurface)
            {
                _surface = new Surface(0, 0, PixelFormat.Indexed8);
            }
            else
                _surface = null;

            _ownSurface = true;
        }

        // Decompression
        protected int DeLZ77(BytePtr dest, BytePtr src, int srcSize, int destSize)
        {
            var frameLength = src.ToInt32();
            if (frameLength > destSize)
            {
                Warning("CoktelDecoder::deLZ77(): Uncompressed size bigger than buffer size (%d > %d)", frameLength,
                    destSize);
                return 0;
            }

            System.Diagnostics.Debug.Assert(srcSize >= 4);

            var realSize = frameLength;
            src.Offset += 4;
            srcSize -= 4;

            ushort bufPos1;
            bool mode;
            if ((src.ToUInt16() == 0x1234) && src.ToUInt16(2) == 0x5678)
            {
                System.Diagnostics.Debug.Assert(srcSize >= 4);

                src.Offset += 4;
                srcSize -= 4;

                bufPos1 = 273;
                mode = true; // 123Ch (cmp al, 12h)
            }
            else
            {
                bufPos1 = 4078;
                mode = false; // 275h (jnz +2)
            }

            var buf = new byte[4370];
            buf.Set(0, 32, bufPos1);

            byte chunkCount = 1;
            byte chunkBitField = 0;

            while (frameLength > 0)
            {
                chunkCount--;

                if (chunkCount == 0)
                {
                    chunkCount = 8;
                    chunkBitField = src.Value;
                    src.Offset++;
                }

                if (chunkBitField % 2 != 0)
                {
                    System.Diagnostics.Debug.Assert(srcSize >= 1);

                    chunkBitField >>= 1;
                    buf[bufPos1] = src.Value;
                    dest.Value = src.Value;
                    src.Offset++;
                    dest.Offset++;
                    bufPos1 = (ushort) ((bufPos1 + 1) % 4096);
                    frameLength--;
                    srcSize--;
                    continue;
                }
                chunkBitField >>= 1;

                System.Diagnostics.Debug.Assert(srcSize >= 2);

                var tmp = src.ToUInt16();
                var chunkLength = (ushort) (((tmp & 0xF00) >> 8) + 3);

                src.Offset += 2;
                srcSize -= 2;

                if ((mode && ((chunkLength & 0xFF) == 0x12)) ||
                    (!mode && (chunkLength == 0)))
                {
                    System.Diagnostics.Debug.Assert(srcSize >= 1);

                    chunkLength = (ushort) (src.Value + 0x12);
                    src.Offset++;
                    srcSize--;
                }

                var bufPos2 = (ushort) ((tmp & 0xFF) + ((tmp >> 4) & 0x0F00));
                if ((tmp + chunkLength >= 4096) ||
                    (chunkLength + bufPos1 >= 4096))
                {
                    for (var i = 0; i < chunkLength; i++, dest.Offset++)
                    {
                        dest.Value = buf[bufPos2];
                        buf[bufPos1] = buf[bufPos2];
                        bufPos1 = (ushort) ((bufPos1 + 1) % 4096);
                        bufPos2 = (ushort) ((bufPos2 + 1) % 4096);
                    }
                }
                else if ((tmp + chunkLength < bufPos1) ||
                         (chunkLength + bufPos1 < bufPos2))
                {
                    Array.Copy(buf, bufPos2, dest.Data, dest.Offset, chunkLength);
                    Array.Copy(buf, bufPos2, dest.Data, dest.Offset + bufPos1, chunkLength);

                    dest.Offset += chunkLength;
                    bufPos1 += chunkLength;
                    bufPos2 += chunkLength;
                }
                else
                {
                    for (var i = 0; i < chunkLength; i++, dest.Offset++, bufPos1++, bufPos2++)
                    {
                        dest.Value = buf[bufPos2];
                        buf[bufPos1] = buf[bufPos2];
                    }
                }
                frameLength -= chunkLength;
            }

            return realSize;
        }

        protected void DeRLE(ref BytePtr destPtr, ref BytePtr srcPtr, short destLen, short srcLen)
        {
            srcPtr.Offset++;

            if ((srcLen & 1) != 0)
            {
                var data = srcPtr.Value;
                srcPtr.Offset++;

                if (destLen > 0)
                {
                    destPtr.Value = data;
                    destPtr.Offset++;
                    destLen--;
                }
            }

            srcLen >>= 1;

            while (srcLen > 0)
            {
                var tmp = srcPtr.Value;
                srcPtr.Offset++;
                if ((tmp & 0x80) != 0)
                {
                    // Verbatim copy
                    tmp &= 0x7F;

                    var copyCount = (short) Math.Max(0, Math.Min(destLen, tmp * 2));
                    Array.Copy(srcPtr.Data, srcPtr.Offset, destPtr.Data, destPtr.Offset, copyCount);

                    srcPtr.Offset += tmp * 2;
                    destPtr.Offset += copyCount;
                    destLen -= copyCount;
                }
                else
                {
                    // 2 bytes tmp times
                    for (var i = 0; (i < tmp) && (destLen > 0); i++)
                    {
                        for (var j = 0; j < 2; j++)
                        {
                            if (destLen <= 0)
                                break;

                            destPtr.Value = srcPtr[j];
                            destPtr.Offset++;
                            destLen--;
                        }
                    }
                    srcPtr.Offset += 2;
                }
                srcLen -= tmp;
            }
        }

        // Sound helper functions
        protected void UnsignedToSigned(byte[] buffer, int length)
        {
            var buf = new BytePtr(buffer);
            while (length-- > 0)
            {
                buf.Value ^= 0x80;
                buf.Offset++;
            }
        }
    }
}