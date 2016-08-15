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
using NScumm.Core.Audio;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound
{
    enum AudioSyncCommands
    {
        Start = 0,
        Next = 1,
        Stop = 2
    }

    internal class AudioPlayer
    {
        public const int AUDIO_VOLUME_MAX = 127;

        private int _audioRate;
        private IMixer _mixer;
        private ResourceManager _resMan;
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
            throw new NotImplementedException("GetAudioStream");
            //IAudioStream audioSeekStream;
            //Stream audioStream;
            //int size = 0;
            //byte[] data = 0;
            //byte flags = 0;
            //ResourceManager.ResourceSource.Resource audioRes;

            //sampleLen = 0;

            //if (volume == 65535)
            //{
            //    audioRes = _resMan.FindResource(new ResourceId(ResourceType.Audio, (ushort)number), false);
            //    if (audioRes==null)
            //    {
            //        Warning("Failed to find audio entry %i", number);
            //        return null;
            //    }
            //}
            //else
            //{
            //    audioRes = _resMan.FindResource(new ResourceId(ResourceType.Audio36, (ushort)volume, number), false);
            //    if (audioRes == null)
            //    {
            //        Warning("Failed to find audio entry (%i, %i, %i, %i, %i)", volume, (number >> 24) & 0xff,
            //                (number >> 16) & 0xff, (number >> 8) & 0xff, number & 0xff);
            //        return null;
            //    }
            //}

            //byte audioFlags;
            //uint audioCompressionType = audioRes.getAudioCompressionType();

            //if (audioCompressionType!=0)
            //{
            //    // Compressed audio made by our tool
            //    byte[] compressedData = new byte[audioRes.size];
            //    // We copy over the compressed data in our own buffer. We have to do
            //    // this, because ResourceManager may free the original data late. All
            //    // other compression types already decompress completely into an
            //    // additional buffer here. MP3/OGG/FLAC decompression works on-the-fly
            //    // instead.
            //    memcpy(compressedData, audioRes.data, audioRes.size);
            //    var compressedStream = new MemoryStream(compressedData, 0, (int)audioRes.size);

            //    if (audioCompressionType == ScummHelper.MakeTag('M', 'P', '3', ' '))
            //    {
            //        audioSeekStream = ServiceLocator.AudioManager.MakeMp3Stream(compressedStream);
            //    }
            //    else if (audioCompressionType == ScummHelper.MakeTag('O', 'G', 'G', ' '){
            //        audioSeekStream = ServiceLocator.AudioManager.MakeVorbisStream(compressedStream);
            //    }
            //    else if (audioCompressionType == ScummHelper.MakeTag('F', 'L', 'A', 'C'){
            //        audioSeekStream = ServiceLocator.AudioManager.MakeFlacStream(compressedStream);
            //    }
            //    else
            //    {
            //        Error("Compressed audio file encountered, but no appropriate decoder is compiled in");
            //    }
            //}
            //else
            //{
            //    // Original source file
            //    if (audioRes._headerSize > 0)
            //    {
            //        // SCI1.1
            //        MemoryStream headerStream = new MemoryStream(audioRes._header, audioRes._headerSize);

            //        if (ReadSOLHeader(headerStream, audioRes._headerSize, size, _audioRate, audioFlags, audioRes.size))
            //        {
            //            MemoryStream dataStream = new MemoryStream(audioRes.data, audioRes.size);
            //            data = ReadSOLAudio(dataStream, size, audioFlags, flags);
            //        }
            //    }
            //    else if (audioRes.size > 4 && READ_BE_UINT32(audioRes.data) == MKTAG('R', 'I', 'F', 'F'))
            //    {
            //        // WAVE detected
            //        var waveStream = new MemoryStream(audioRes.data, audioRes.size);

            //        // Calculate samplelen from WAVE header
            //        int waveSize = 0, waveRate = 0;
            //        byte waveFlags = 0;
            //        bool ret = Audio::loadWAVFromStream(*waveStream, waveSize, waveRate, waveFlags);
            //        if (!ret)
            //            error("Failed to load WAV from stream");

            //        sampleLen = (waveFlags & Audio::FLAG_16BITS ? waveSize >> 1 : waveSize) * 60 / waveRate;

            //            waveStream.Seek(0, SeekOrigin.Begin);
            //        audioStream = Audio::makeWAVStream(waveStream, DisposeAfterUse::YES);
            //    }
            //    else if (audioRes.size > 4 && READ_BE_UINT32(audioRes.data) == MKTAG('F', 'O', 'R', 'M'))
            //    {
            //        // AIFF detected
            //        var waveStream = new MemoryStream(audioRes.data, 0, audioRes.size);
            //        var rewindStream = Audio::makeAIFFStream(waveStream);
            //        audioSeekStream = dynamic_cast<Audio::SeekableAudioStream*>(rewindStream);

            //        if (!audioSeekStream)
            //        {
            //            Warning("AIFF file is not seekable");
            //        }
            //    }
            //    else if (audioRes.size > 14 && READ_BE_UINT16(audioRes.data) == 1 && READ_BE_UINT16(audioRes.data + 2) == 1
            //          && READ_BE_UINT16(audioRes.data + 4) == 5 && READ_BE_UINT32(audioRes.data + 10) == 0x00018051)
            //    {
            //        // Mac snd detected
            //        var sndStream = new MemoryStream(audioRes.data, 0, audioRes.size);

            //        audioSeekStream = Audio::makeMacSndStream(sndStream, DisposeAfterUse::YES);
            //        if (!audioSeekStream)
            //            Error("Failed to load Mac sound stream");

            //    }
            //    else
            //    {
            //        // SCI1 raw audio
            //        size = audioRes.size;
            //        data = (byte*)malloc(size);
            //        assert(data);
            //        memcpy(data, audioRes.data, size);
            //        flags = Audio::FLAG_UNSIGNED;
            //        _audioRate = 11025;
            //    }

            //    if (data)
            //        audioSeekStream = Audio::makeRawStream(data, size, _audioRate, flags);
            //}

            //if (audioSeekStream)
            //{
            //    sampleLen = (audioSeekStream.getLength().msecs() * 60) / 1000; // we translate msecs to ticks
            //    audioStream = audioSeekStream;
            //}
            //// We have to make sure that we don't depend on resource manager pointers
            //// after this point, because the actual audio resource may get unloaded by
            //// resource manager at any time.
            //if (audioStream != null)
            //    return audioStream;

            //return null;
        }

        public void StopSoundSync()
        {
            if (_syncResource != null)
            {
                _resMan.UnlockResource(_syncResource);
                _syncResource = null;
            }
        }

        public void SetSoundSync(ResourceId id, Register syncObjAddr, SegManager segMan)
        {
            _syncResource = _resMan.FindResource(id, true);
            _syncOffset = 0;

            if (_syncResource != null)
            {
                SciEngine.WriteSelectorValue(segMan, syncObjAddr, o => o.syncCue, 0);
            }
            else
            {
                Warning($"setSoundSync: failed to find resource {id}");
                // Notify the scripts to stop sound sync
                SciEngine.WriteSelectorValue(segMan, syncObjAddr, o => o.syncCue, Register.SIGNAL_OFFSET);
            }
        }

        public void DoSoundSync(Register syncObjAddr, SegManager segMan)
        {
            if (_syncResource != null && (_syncOffset < _syncResource.size - 1))
            {
                short syncCue = -1;
                short syncTime = (short)_syncResource.data.ReadSci11EndianUInt16(_syncOffset);

                _syncOffset += 2;

                if ((syncTime != -1) && (_syncOffset < _syncResource.size - 1))
                {
                    syncCue = (short)_syncResource.data.ReadSci11EndianUInt16(_syncOffset);
                    _syncOffset += 2;
                }

                SciEngine.WriteSelectorValue(segMan, syncObjAddr, o => o.syncTime, (ushort)syncTime);
                SciEngine.WriteSelectorValue(segMan, syncObjAddr, o => o.syncCue, (ushort)syncCue);
            }
        }

        public void StopAllAudio()
        {
            StopSoundSync();
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

            int sampleLen = 0;
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

        public void HandleFanmadeSciAudio(Register sendp, SegManager _segMan)
        {
            throw new NotImplementedException("HandleFanmadeSciAudio");
        }
    }
}
