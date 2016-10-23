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
using NScumm.Core.Audio.Decoders;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound
{
    [Flags]
    internal enum SolFlags
    {
        Compressed = 1 << 0,
        Unknown = 1 << 1,
        Is16Bit = 1 << 2,
        IsSigned = 1 << 3
    }

    internal class AudioPlayer
    {
        public const int AudioVolumeMax = 127;

        private int _audioRate;
        private readonly IMixer _mixer;
        private readonly ResourceManager _resMan;
        private bool _wPlayFlag;
        /// <summary>
        /// Used by kDoSync for speech syncing in CD talkie games
        /// </summary>
        private ResourceManager.ResourceSource.Resource _syncResource;
        private int _syncOffset;
        private uint _audioCdStart;
        private SoundHandle _audioHandle;

        public AudioPlayer(ResourceManager resMan)
        {
            _resMan = resMan;
            _audioRate = 11025;

            _mixer = SciEngine.Instance.Mixer;
            _wPlayFlag = false;
        }

        public IRewindableAudioStream GetAudioStream(uint number, uint volume, out int sampleLen)
        {
            ISeekableAudioStream audioSeekStream = null;
            IRewindableAudioStream audioStream = null;
            int size = 0;
            byte[] data = null;
            AudioFlags flags = 0;
            ResourceManager.ResourceSource.Resource audioRes;

            sampleLen = 0;

            if (volume == 65535)
            {
                audioRes = _resMan.FindResource(new ResourceId(ResourceType.Audio, (ushort)number), false);
                if (audioRes == null)
                {
                    Warning("Failed to find audio entry %i", number);
                    return null;
                }
            }
            else
            {
                audioRes = _resMan.FindResource(new ResourceId(ResourceType.Audio36, (ushort)volume, number), false);
                if (audioRes == null)
                {
                    Warning("Failed to find audio entry (%i, %i, %i, %i, %i)", volume, (number >> 24) & 0xff,
                            (number >> 16) & 0xff, (number >> 8) & 0xff, number & 0xff);
                    return null;
                }
            }

            uint audioCompressionType = audioRes.AudioCompressionType;

            if (audioCompressionType != 0)
            {
                // Compressed audio made by our tool
                byte[] compressedData = new byte[audioRes.size];
                // We copy over the compressed data in our own buffer. We have to do
                // this, because ResourceManager may free the original data late. All
                // other compression types already decompress completely into an
                // additional buffer here. MP3/OGG/FLAC decompression works on-the-fly
                // instead.
                Array.Copy(audioRes.data, compressedData, audioRes.size);
                var compressedStream = new MemoryStream(compressedData, 0, audioRes.size);

                if (audioCompressionType == ScummHelper.MakeTag('M', 'P', '3', ' '))
                {
                    audioSeekStream = (ISeekableAudioStream)ServiceLocator.AudioManager.MakeMp3Stream(compressedStream);
                }
                else if (audioCompressionType == ScummHelper.MakeTag('O', 'G', 'G', ' '))
                {
                    audioSeekStream = (ISeekableAudioStream)ServiceLocator.AudioManager.MakeVorbisStream(compressedStream);
                }
                else if (audioCompressionType == ScummHelper.MakeTag('F', 'L', 'A', 'C'))
                {
                    audioSeekStream = (ISeekableAudioStream)ServiceLocator.AudioManager.MakeFlacStream(compressedStream);
                }
                else
                {
                    Error("Compressed audio file encountered, but no appropriate decoder is compiled in");
                }
            }
            else
            {
                // Original source file
                if (audioRes._headerSize > 0)
                {
                    // SCI1.1
                    using (var headerStream = new MemoryStream(audioRes._header, 0, audioRes._headerSize))
                    {
                        SolFlags audioFlags;
                        if (ReadSolHeader(headerStream, audioRes._headerSize, ref size, ref _audioRate, out audioFlags, audioRes.size))
                        {
                            using (var dataStream = new MemoryStream(audioRes.data, 0, audioRes.size))
                            {
                                data = ReadSolAudio(dataStream, ref size, audioFlags, ref flags);
                            }
                        }
                    }
                }
                else if (audioRes.size > 4 && audioRes.data.ToUInt32BigEndian() == ScummHelper.MakeTag('R', 'I', 'F', 'F'))
                {
                    // WAVE detected
                    var waveStream = new MemoryStream(audioRes.data, 0, audioRes.size);

                    // Calculate samplelen from WAVE header
                    int waveSize, waveRate;
                    AudioFlags waveFlags;
                    bool ret = Wave.LoadWAVFromStream(waveStream, out waveSize, out waveRate, out waveFlags);
                    if (!ret)
                        Error("Failed to load WAV from stream");

                    sampleLen = (waveFlags.HasFlag(AudioFlags.Is16Bits) ? waveSize >> 1 : waveSize) * 60 / waveRate;

                        waveStream.Seek(0, SeekOrigin.Begin);
                    audioStream = Wave.MakeWAVStream(waveStream, true);
                }
                else if (audioRes.size > 4 && audioRes.data.ToUInt32BigEndian() == ScummHelper.MakeTag('F', 'O', 'R', 'M'))
                {
                    throw new NotImplementedException("AIFF");
                //    // AIFF detected
                //    var waveStream = new MemoryStream(audioRes.data, 0, audioRes.size);
                //    var rewindStream = Audio::makeAIFFStream(waveStream);
                //    audioSeekStream = dynamic_cast<Audio::SeekableAudioStream*>(rewindStream);

                //    if (!audioSeekStream)
                //    {
                //        Warning("AIFF file is not seekable");
                //    }
                }
                else if (audioRes.size > 14 && audioRes.data.ToUInt16BigEndian() == 1 && audioRes.data.ToUInt16BigEndian(2) == 1
                         && audioRes.data.ToUInt16BigEndian(4) == 5 && audioRes.data.ToUInt32BigEndian(10) == 0x00018051)
                {
                    throw new NotImplementedException("Mac snd");
                //    // Mac snd detected
                //    var sndStream = new MemoryStream(audioRes.data, 0, audioRes.size);

                //    audioSeekStream = Audio::makeMacSndStream(sndStream, DisposeAfterUse::YES);
                //    if (!audioSeekStream)
                //        Error("Failed to load Mac sound stream");

                }
                else
                {
                    // SCI1 raw audio
                    size = audioRes.size;
                    data = new byte[size];
                    Array.Copy(audioRes.data, data, size);
                    flags = AudioFlags.Unsigned;
                    _audioRate = 11025;
                }

                if (data != null)
                    audioSeekStream = new RawStream(flags, _audioRate, true, new MemoryStream(data, 0, size));
            }

            if (audioSeekStream != null)
            {
                sampleLen = (audioSeekStream.Length.Milliseconds * 60) / 1000; // we translate msecs to ticks
                audioStream = audioSeekStream;
            }
            // We have to make sure that we don't depend on resource manager pointers
            // after this point, because the actual audio resource may get unloaded by
            // resource manager at any time.
            return audioStream;
        }

        private static byte[] ReadSolAudio(Stream audioStream, ref int size, SolFlags audioFlags, ref AudioFlags flags)
        {
            if (!Enum.IsDefined(typeof(AudioFlags), flags))
                throw new ArgumentOutOfRangeException(nameof(flags), "Value should be defined in the AudioFlags enum.");

            byte[] buffer;

            // Convert the SOL stream flags to our own format
            flags = 0;
            if (audioFlags.HasFlag(SolFlags.Is16Bit))
                flags |= AudioFlags.Is16Bits | AudioFlags.LittleEndian;

            if (!audioFlags.HasFlag(SolFlags.IsSigned))
                flags |= AudioFlags.Unsigned;

            if (audioFlags.HasFlag(SolFlags.Compressed))
            {
                buffer = new byte[size * 2];

                if (audioFlags.HasFlag(SolFlags.Is16Bit))
                    DeDpcm16(buffer, audioStream, size);
                else {
                    DeDpcm8(buffer, audioStream, size);
                }

                size *= 2;
            }
            else {
                // We assume that the sound data is raw PCM
                buffer = new byte[size];
                audioStream.Read(buffer, 0, size);
            }

            return buffer;
        }

        // Sierra SOL audio file reader
        // Check here for more info: http://wiki.multimedia.cx/index.php?title=Sierra_Audio
        private static bool ReadSolHeader(Stream audioStream, int headerSize, ref int size, ref int audioRate, out SolFlags audioFlags, int resSize)
        {
            if (headerSize != 7 && headerSize != 11 && headerSize != 12)
            {
                Warning("SOL audio header of size %i not supported", headerSize);
                audioFlags = 0;
                return false;
            }

            var br = new BinaryReader(audioStream);
            uint tag = br.ReadUInt32BigEndian();

            if (tag != ScummHelper.MakeTag('S', 'O', 'L', '\0'))
            {
                Warning("No 'SOL' FourCC found");
                audioFlags = 0;
                return false;
            }

            audioRate = br.ReadUInt16();
            audioFlags = (SolFlags)br.ReadByte();

            // For the QFG3 demo format, just use the resource size
            // Otherwise, load it from the header
            if (headerSize == 7)
                size = resSize;
            else
                size = br.ReadInt32();

            return true;
        }

        private static void DeDpcm8(byte[] soundBuf, Stream audioStream, int n)
        {
            int s = 0x80;
            var buf = new BytePtr(soundBuf);
            for (uint i = 0; i < n; i++)
            {
                byte b = (byte)audioStream.ReadByte();

                DeDpcm8Nibble(buf, ref s, (byte)(b >> 4)); buf.Offset++;
                DeDpcm8Nibble(buf, ref s, (byte)(b & 0xf)); buf.Offset++;
            }
        }

        private static void DeDpcm8Nibble(BytePtr soundBuf, ref int s, byte b)
        {
            if ((b & 8)!=0)
            {
                s -= TableDpcm8[7 - (b & 7)];
            }
            else
                s += TableDpcm8[b & 7];
            s = ScummHelper.Clip(s, 0, 255);
            soundBuf.Value = (byte)s;
        }

        private static void DeDpcm16(byte[] soundBuf, Stream audioStream, int n)
        {
            int s = 0;
            for (int i = 0; i < n; i++)
            {
                byte b = (byte)audioStream.ReadByte();
                if ((b & 0x80)!=0)
                    s -= TableDpcm16[b & 0x7f];
                else
                    s += TableDpcm16[b];

                s = ScummHelper.Clip(s, -32768, 32767);
                soundBuf.WriteInt16(i * 2, (short)s);
            }
        }

        public void StopAllAudio()
        {
            StopAudio();
            if (_audioCdStart > 0)
                AudioCdStop();
        }

        private void AudioCdStop()
        {
            throw new NotImplementedException("AudioCdStop");
            //_audioCdStart = 0;
            //g_system.getAudioCDManager().stop();
        }

        public int StartAudio(ushort module, uint number)
        {
            int sampleLen;
            var audioStream = GetAudioStream((ushort)number, module, out sampleLen);

            if (audioStream != null)
            {
                _wPlayFlag = false;
                var soundType = (module == 65535) ? SoundType.SFX : SoundType.Speech;
                _audioHandle = _mixer.PlayStream(soundType, audioStream);
                return sampleLen;
            }

            // Don't throw a warning in this case. getAudioStream() already has. Some games
            // do miss audio entries (perhaps because of a typo, or because they were simply
            // forgotten).
            return 0;
        }

        public void PauseAudio()
        {
            _mixer.PauseHandle(_audioHandle, true);
        }

        public int WPlayAudio(ushort module, uint tuple)
        {
            // Get the audio sample length and set the wPlay flag so we return 0 on
            // position. SSCI pre-loads the audio here, but it's much easier for us to
            // just get the sample length and return that. wPlayAudio should *not*
            // actually start the sample.

            int sampleLen;
            using (var audioStream = GetAudioStream((ushort)tuple, module, out sampleLen))
            {
                if (audioStream == null)
                    Warning($"wPlayAudio: unable to create stream for audio tuple {tuple}, module {module}");
            }
            _wPlayFlag = true;
            return sampleLen;
        }

        public void ResumeAudio()
        {
            _mixer.PauseHandle(_audioHandle, false);
        }

        public void SetAudioRate(ushort rate)
        {
            _audioRate = rate;
        }

        public int GetAudioPosition()
        {
            if (_mixer.IsSoundHandleActive(_audioHandle))
                return _mixer.GetSoundElapsedTime(_audioHandle) * 6 / 100; // return elapsed time in ticks
            if (_wPlayFlag)
                return 0; // Sound has "loaded" so return that it hasn't started
            return -1; // Sound finished
        }

        public void StopAudio()
        {
            _mixer.StopHandle(_audioHandle);
        }

        public void HandleFanmadeSciAudio(Register sendp, SegManager segMan)
        {
            throw new NotImplementedException("HandleFanmadeSciAudio");
        }

        private static readonly byte[] TableDpcm8 = { 0, 1, 2, 3, 6, 10, 15, 21 };

        // FIXME: Move this to sound/adpcm.cpp?
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

    }
}
