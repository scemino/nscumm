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

#if ENABLE_SCI32

using System;
using System.Collections.Generic;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using NScumm.Sci.Engine;
using NScumm.Sci.Sound.Decoders;
using NScumm.Sci.Video;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound
{
    /// <summary>
    /// An audio channel used by the software SCI mixer.
    /// </summary>
    internal class AudioChannel
    {
        /**
		 * The maximum channel volume.
		 */
        private const int MaxVolume = 127;

        /**
         * The ID of the resource loaded into this channel.
         */
        public ResourceId id;

        /**
         * The resource loaded into this channel.
         */
        public ResourceManager.ResourceSource.Resource resource;

        /**
         * Data stream containing the raw audio for the channel.
         */
        public Stream resourceStream;

        /**
         * The audio stream loaded into this channel.
         * `SeekableAudioStream` is used here instead of
         * `RewindableAudioStream` because
         * `RewindableAudioStream` does not include the
         * `getLength` function, which is needed to tell the
         * game engine the duration of audio streams.
         */
        public IAudioStream stream;

        /**
         * The converter used to transform and merge the input
         * stream into the mixer's output buffer.
         */
        public IRateConverter converter;

        /**
         * Duration of the channel, in ticks.
         */
        public uint duration;

        /**
         * The tick when the channel was started.
         */
        public uint startedAtTick;

        /**
         * The tick when the channel was paused.
         */
        public uint pausedAtTick;

        /**
         * Whether or not the audio in this channel should loop
         * infinitely.
         */
        public bool loop;

        /**
         * The time, in ticks, that the channel fade began.
         * If 0, the channel is not being faded.
         */
        public int fadeStartTick;

        /**
         * The start volume of a fade.
         */
        public int fadeStartVolume;

        /**
         * The total length of the fade, in ticks.
         */
        public uint fadeDuration;

        /**
         * The end volume of a fade.
         */
        public int fadeTargetVolume;

        /**
         * Whether or not the channel should be stopped and
         * freed when the fade is complete.
         */
        public bool stopChannelOnFade;

        /**
         * Whether or not this channel contains a Robot
         * audio block.
         */
        public bool robot;

        /**
         * Whether or not this channel contains a VMD audio
         * track.
         */
        public bool vmd;

        /**
         * For digital sound effects, the related VM
         * Sound::nodePtr object for the sound.
         */
        public Register soundNode;

        /**
         * The playback volume, from 1 to 127 inclusive.
         */
        public int volume;

        /**
         * The amount to pan to the right, from 0 to 100.
         * 50 is centered, -1 is not panned.
         */
        public int pan;
    }

    /**
     * Special audio channel indexes used to select a channel
     * for digital audio playback.
     */

    internal enum AudioChannelIndex
    {
        RobotChannel = -3,
        NoExistingChannel = -2,
        AllChannels = -1
    }

    /**
     * Audio32 acts as a permanent audio stream into the system
     * mixer and provides digital audio services for the SCI32
     * engine, since the system mixer does not support all the
     * features of SCI.
     */

    internal class Audio32 : IAudioStream
    {
        /// <summary>
        /// The maximum channel volume.
        /// </summary>
        private const int MaxVolume = 127;

        private ResourceManager _resMan;
        private IMixer _mixer;
        private SoundHandle _handle;
        private object _mutex = new object();

        /// <summary>
        /// If true, audio will be mixed by reducing the target
        /// buffer by half every time a new channel is mixed in.
        /// The final channel is not attenuated.
        /// </summary>
        private bool _attenuatedMixing;

        public bool StopRobotAudio()
        {
            lock (_mutex)
            {
                var channelIndex = FindRobotChannel();
                if (channelIndex == AudioChannelIndex.NoExistingChannel)
                {
                    return false;
                }

                Stop(channelIndex);
                return true;
            }
        }

        private AudioChannelIndex FindRobotChannel()
        {
            lock (_mutex)
            {
                for (var i = 0; i < _numActiveChannels; ++i)
                {
                    if (_channels[i].robot)
                    {
                        return (AudioChannelIndex)i;
                    }
                }
            }

            return AudioChannelIndex.NoExistingChannel;
        }


        /// <summary>
        /// When true, a modified attenuation algorithm is used
        /// (`A/4 + B`) instead of standard linear attenuation
        /// (`A/2 + B/2`).
        /// </summary>
        private bool _useModifiedAttenuation;

        /// <summary>
        /// The audio channels.
        /// </summary>
        private List<AudioChannel> _channels;

        /// <summary>
        /// The number of active audio channels in the mixer.
        /// Being active is not the same as playing; active
        /// channels may be paused.
        /// </summary>
        private byte _numActiveChannels;

       /**
         * Whether or not we are in the audio thread.
         *
         * This flag is used instead of passing a parameter to
         * `freeUnusedChannels` because a parameter would
         * require forwarding through the public method `stop`,
         * and there is not currently any reason for this
         * implementation detail to be exposed.
         */
        private bool _inAudioThread;

        /**
         * The list of resources from freed channels that need
         * to be unlocked from the main thread.
         */
        private List<ResourceManager.ResourceSource.Resource> _resourcesToUnlock;

        /**
         * The hardware DAC sample rate. Stored only for script
         * compatibility.
         */
        private ushort _globalSampleRate;

        /**
         * The maximum allowed sample rate of the system mixer.
         * Stored only for script compatibility.
         */
        private ushort _maxAllowedSampleRate;

        /**
         * The hardware DAC bit depth. Stored only for script
         * compatibility.
         */
        private byte _globalBitDepth;

        /**
         * The maximum allowed bit depth of the system mixer.
         * Stored only for script compatibility.
         */
        private byte _maxAllowedBitDepth;

        /**
         * The hardware DAC output (speaker) channel
         * configuration. Stored only for script compatibility.
         */
        private byte _globalNumOutputChannels;

        /**
         * The maximum allowed number of output (speaker)
         * channels of the system mixer. Stored only for script
         * compatibility.
         */
        private byte _maxAllowedOutputChannels;

        /**
         * The number of audio channels that should have their
         * data preloaded into memory instead of streaming from
         * disk.
         * 1 = all channels, 2 = 2nd active channel and above,
         * etc.
         * Stored only for script compatibility.
         */
        private byte _preload;

        /**
         * The index of the channel being monitored for signal,
         * or -1 if no channel is monitored. When a channel is
         * monitored, it also causes the engine to play only the
         * monitored channel.
         */
        private short _monitoredChannelIndex;

        /**
         * The data buffer holding decompressed audio data for
         * the channel that will be monitored for an audio
         * signal.
         */
        private short[] _monitoredBuffer;

        /**
         * The size of the buffer, in bytes.
         */
        private int _monitoredBufferSize;

        /**
         * The number of valid audio samples in the signal
         * monitoring buffer.
         */
        private int _numMonitoredSamples;

        /**
         * The tick when audio was globally paused.
         */
        private int _pausedAtTick;

        /**
         * The tick when audio was globally started.
         */
        private int _startedAtTick;

        /**
         * When true, channels marked as robot audio will not be
         * played.
         */
        private bool _robotAudioPaused;

        private readonly List<ResourceManager.ResourceSource> _sources;

        public Audio32(ResourceManager resMan)
        {
            _sources = new List<ResourceManager.ResourceSource>();
            _resMan = resMan;
            _mixer = SciEngine.Instance.Mixer;
            _globalSampleRate = 44100;
            _maxAllowedSampleRate = 44100;
            _globalBitDepth = 16;
            _maxAllowedBitDepth = 16;
            _globalNumOutputChannels = 2;
            _maxAllowedOutputChannels = 2;
            _attenuatedMixing = true;
            _monitoredChannelIndex = -1;

            if (ResourceManager.GetSciVersion() < SciVersion.V3)
            {
                _channels = new List<AudioChannel>(5);
            }
            else
            {
                _channels = new List<AudioChannel>(8);
            }

            _useModifiedAttenuation = false;
            if (ResourceManager.GetSciVersion() == SciVersion.V2_1_MIDDLE)
            {
                switch (SciEngine.Instance.GameId)
                {
                    case SciGameId.MOTHERGOOSEHIRES:
                    case SciGameId.PQ4:
                    case SciGameId.QFG4:
                    case SciGameId.SQ6:
                        _useModifiedAttenuation = true;
                        break;
                }
            }
            else if (ResourceManager.GetSciVersion() == SciVersion.V2_1_EARLY && SciEngine.Instance.GameId == SciGameId.KQ7)
            {
                // KQ7 1.51 uses the non-standard attenuation, but 2.00b
                // does not, which is strange.
                _useModifiedAttenuation = true;
            }

            _handle = _mixer.PlayStream(SoundType.SFX, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        public void Dispose()
        {
            Stop(AudioChannelIndex.AllChannels);
            _mixer.StopHandle(_handle);
            _monitoredBuffer = null;
        }

        public bool FinishRobotAudio()
        {
            lock (_mutex)
            {
                var channelIndex = FindRobotChannel();
                if (channelIndex == AudioChannelIndex.NoExistingChannel)
                {
                    return false;
                }

                ((RobotAudioStream)GetChannel((int)channelIndex).stream).Finish();
                return true;
            }
        }

        /// <summary>
        /// Gets the number of currently active channels.
        /// </summary>
        /// <returns></returns>
        public byte GetNumActiveChannels()
        {
            lock (_mutex)
            {
                return _numActiveChannels;
            }
        }

        /// <summary>
        /// Gets the (fake) sample rate of the hardware DAC.
        /// For script compatibility only.
        /// </summary>
        public ushort SampleRate
        {
            get { return _globalSampleRate; }
            set
            {
                if (value > _maxAllowedSampleRate)
                {
                    value = _maxAllowedSampleRate;
                }

                _globalSampleRate = value;
            }
        }

        public short GetVolume(short channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= _numActiveChannels)
            {
                return (short)(_mixer.GetChannelVolume(_handle) * MaxVolume / Mixer.MaxChannelVolume);
            }

            lock (_mutex)
            {
                return (short)GetChannel(channelIndex).volume;
            }
        }

        public void SetVolume(short channelIndex, short volume)
        {
            volume = Math.Min((short)MaxVolume, volume);
            if (channelIndex == (int)AudioChannelIndex.AllChannels)
            {
                ConfigManager.Instance.Set<int>("sfx_volume", volume * Mixer.MaxChannelVolume / MaxVolume);
                ConfigManager.Instance.Set<int>("speech_volume", volume * Mixer.MaxChannelVolume / MaxVolume);
                _mixer.SetChannelVolume(_handle, volume * Mixer.MaxChannelVolume / MaxVolume);
                SciEngine.Instance.SyncSoundSettings();
            }
            else if (channelIndex != (int)AudioChannelIndex.NoExistingChannel)
            {
                lock (_mutex)
                {
                    GetChannel(channelIndex).volume = volume;
                }
            }
        }

        public short GetPosition(short channelIndex)
        {
            lock (_mutex)
            {
                if (channelIndex == (int)AudioChannelIndex.NoExistingChannel || _numActiveChannels == 0)
                {
                    return -1;
                }

                // NOTE: SSCI treats this as an unsigned short except for
                // when the value is 65535, then it treats it as signed
                int position = -1;
                uint now = SciEngine.Instance.TickCount;

                // NOTE: The original engine also queried the audio driver to see whether
                // it thought that there was audio playback occurring via driver opcode 9
                if (channelIndex == (int)AudioChannelIndex.AllChannels)
                {
                    if (_pausedAtTick != 0)
                    {
                        position = _pausedAtTick - _startedAtTick;
                    }
                    else
                    {
                        position = (int)(now - _startedAtTick);
                    }
                }
                else
                {
                    AudioChannel channel = GetChannel(channelIndex);

                    if (channel.pausedAtTick != 0)
                    {
                        position = (int)(channel.pausedAtTick - channel.startedAtTick);
                    }
                    else if (_pausedAtTick != 0)
                    {
                        position = (int)(_pausedAtTick - channel.startedAtTick);
                    }
                    else
                    {
                        position = (int)(now - channel.startedAtTick);
                    }
                }

                return (short)Math.Min(position, 65534);
            }
        }

        private ushort Play(short channelIndex, ResourceId resourceId, bool autoPlay, bool loop, short volume,
            Register soundNode, bool monitor)
        {
            lock (_mutex)
            {
                FreeUnusedChannels();

                AudioChannel channel;
                if (channelIndex != (int)AudioChannelIndex.NoExistingChannel)
                {
                    channel = GetChannel(channelIndex);

                    if (channel.pausedAtTick != 0)
                    {
                        Resume(channelIndex);
                        return (ushort)Math.Min(65534, 1 + ((ISeekableAudioStream)channel.stream).Length.Milliseconds * 60 / 1000);
                    }

                    Warning("Tried to resume channel {0} that was not paused", channel.id);
                    return (ushort)Math.Min(65534, 1 + ((ISeekableAudioStream)channel.stream).Length.Milliseconds * 60 / 1000);
                }

                if (_numActiveChannels == _channels.Count)
                {
                    Warning("Audio mixer is full when trying to play %s", resourceId);
                    return 0;
                }

                // NOTE: SCI engine itself normally searches in this order:
                //
                // For Audio36:
                //
                // 1. First, request a FD using Audio36 name and use it as the
                //    source FD for reading the audio resource data.
                // 2a. If the returned FD is -1, or equals the audio map, or
                //     equals the audio bundle, try to get the offset of the
                //     data from the audio map, using the Audio36 name.
                //
                //     If the returned offset is -1, this is not a valid resource;
                //     return 0. Otherwise, set the read offset for the FD to the
                //     returned offset.
                // 2b. Otherwise, use the FD as-is (it is a patch file), with zero
                //     offset, and record it separately so it can be closed later.
                //
                // For plain audio:
                //
                // 1. First, request an Audio resource from the resource cache. If
                //    one does not exist, make the same request for a Wave resource.
                // 2a. If an audio resource was discovered, record its memory ID
                //     and clear the streaming FD
                // 2b. Otherwise, request an Audio FD. If one does not exist, make
                //     the same request for a Wave FD. If neither exist, this is not
                //     a valid resource; return 0. Otherwise, use the returned FD as
                //     the streaming ID and set the memory ID to null.
                //
                // Once these steps are complete, the audio engine either has a file
                // descriptor + offset that it can use to read streamed audio, or it
                // has a memory ID that it can use to read cached audio.
                //
                // Here in ScummVM we just ask the resource manager to give us the
                // resource and we get a seekable stream.

                // TODO: This should be fixed to use streaming, which means
                // fixing the resource manager to allow streaming, which means
                // probably rewriting a bunch of the resource manager.
                ResourceManager.ResourceSource.Resource resource = _resMan.FindResource(resourceId, true);
                if (resource == null)
                {
                    return 0;
                }

                channelIndex = _numActiveChannels++;

                channel = GetChannel(channelIndex);
                channel.id = resourceId;
                channel.resource = resource;
                channel.loop = loop;
                channel.robot = false;
                channel.vmd = false;
                channel.fadeStartTick = 0;
                channel.soundNode = soundNode;
                channel.volume = volume < 0 || volume > MaxVolume ? MaxVolume : volume;
                // TODO: SCI3 introduces stereo audio
                channel.pan = -1;

                if (monitor)
                {
                    _monitoredChannelIndex = channelIndex;
                }

                var headerStream = new MemoryStream(resource._header, 0, resource._headerSize);
                var dataStream = channel.resourceStream = resource.MakeStream();

                if (DetectSolAudio(headerStream))
                {
                    channel.stream = Sol.MakeSOLStream(headerStream, dataStream, false);
                }
                else if (DetectWaveAudio(dataStream))
                {
                    channel.stream = Wave.MakeWAVStream(dataStream, false);
                }
                else
                {
                    AudioFlags flags = AudioFlags.LittleEndian;
                    if (_globalBitDepth == 16)
                    {
                        flags |= AudioFlags.Is16Bits;
                    }
                    else
                    {
                        flags |= AudioFlags.Unsigned;
                    }

                    if (_globalNumOutputChannels == 2)
                    {
                        flags |= AudioFlags.Stereo;
                    }

                    channel.stream = new RawStream(flags, _globalSampleRate, false, dataStream);
                }

                channel.converter = RateHelper.MakeRateConverter(channel.stream.Rate, Rate, channel.stream.IsStereo,
                    false);

                // NOTE: SCI engine sets up a decompression buffer here for the audio
                // stream, plus writes information about the sample to the channel to
                // convert to the correct hardware output format, and allocates the
                // monitoring buffer to match the bitrate/samplerate/channels of the
                // original stream. We do not need to do any of these things since we
                // use audio streams, and allocate and fill the monitoring buffer
                // when reading audio data from the stream.

                channel.duration = /* round up */ (uint)(1 + ((ISeekableAudioStream)channel.stream).Length.Milliseconds * 60 / 1000);

                uint now = SciEngine.Instance.TickCount;
                channel.pausedAtTick = autoPlay ? 0 : now;
                channel.startedAtTick = now;

                if (_numActiveChannels == 1)
                {
                    _startedAtTick = (int)now;
                }

                return (ushort)channel.duration;
            }
        }

        public bool Resume(short channelIndex)
        {
            if (channelIndex == (int)AudioChannelIndex.NoExistingChannel)
            {
                return false;
            }

            lock (_mutex)
            {
                uint now = SciEngine.Instance.TickCount;

                if (channelIndex == (int)AudioChannelIndex.AllChannels)
                {
                    // Global pause in SSCI is an extra layer over
                    // individual channel pauses, so only unpause channels
                    // if there was not a global pause in place
                    if (_pausedAtTick == 0)
                    {
                        return false;
                    }

                    for (int i = 0; i < _numActiveChannels; ++i)
                    {
                        AudioChannel channel = GetChannel(i);
                        if (channel.pausedAtTick == 0)
                        {
                            channel.startedAtTick = (uint)(channel.startedAtTick + now - _pausedAtTick);
                        }
                    }

                    _startedAtTick = (int)(_startedAtTick + now - _pausedAtTick);
                    _pausedAtTick = 0;
                    return true;
                }
                else if (channelIndex == (int)AudioChannelIndex.RobotChannel)
                {
                    for (int i = 0; i < _numActiveChannels; ++i)
                    {
                        AudioChannel channel = GetChannel(i);
                        if (channel.robot)
                        {
                            channel.startedAtTick += now - channel.pausedAtTick;
                            channel.pausedAtTick = 0;
                            // TODO: Robot
                            // StartRobot();
                            return true;
                        }
                    }
                }
                else
                {
                    AudioChannel channel = GetChannel(channelIndex);
                    if (channel.pausedAtTick != 0)
                    {
                        channel.startedAtTick += now - channel.pausedAtTick;
                        channel.pausedAtTick = 0;
                        return true;
                    }
                }

                return false;
            }
        }

        private bool DetectSolAudio(Stream stream)
        {
            var initialPosition = stream.Position;

            // TODO: Resource manager for audio resources reads past the
            // header so even though this is the detection algorithm
            // in SSCI, ScummVM can't use it
#if Undefined
	byte header[6];
	if (stream.read(header, sizeof(header)) != sizeof(header)) {
		stream.seek(initialPosition);
		return false;
	}

	stream.seek(initialPosition);

	if (header[0] != 0x8d || READ_BE_UINT32(header + 2) != MKTAG('S', 'O', 'L', 0)) {
		return false;
	}

	return true;
#else
            byte[] header = new byte[4];
            if (stream.Read(header, 0, 4) != 4)
            {
                stream.Seek(initialPosition, SeekOrigin.Begin);
                return false;
            }

            stream.Seek(initialPosition, SeekOrigin.Begin);

            return header.ToUInt32BigEndian() == ScummHelper.MakeTag('S', 'O', 'L', '\0');

#endif
        }

        private static bool DetectWaveAudio(Stream stream)
        {
            var initialPosition = stream.Position;

            var blockHeader = new byte[8];
            if (stream.Read(blockHeader, 0, 8) != 8)
            {
                stream.Seek(initialPosition, SeekOrigin.Begin);
                return false;
            }

            stream.Seek(initialPosition, SeekOrigin.Begin);
            var headerType = blockHeader.ToUInt32BigEndian();

            return headerType == ScummHelper.MakeTag('R', 'I', 'F', 'F');
        }

        public short Stop(AudioChannelIndex channelIndex)
        {
            lock (_mutex)
            {
                short oldNumChannels = _numActiveChannels;

                if (channelIndex == AudioChannelIndex.NoExistingChannel || oldNumChannels == 0)
                {
                    return 0;
                }

                if (channelIndex == AudioChannelIndex.AllChannels)
                {
                    for (int i = 0; i < oldNumChannels; ++i)
                    {
                        FreeChannel(i);
                    }
                    _numActiveChannels = 0;
                }
                else
                {
                    FreeChannel((int)channelIndex);
                    --_numActiveChannels;
                    for (var i = (int)channelIndex; i < oldNumChannels - 1; ++i)
                    {
                        _channels[i] = _channels[i + 1];
                        if (i + 1 == _monitoredChannelIndex)
                        {
                            _monitoredChannelIndex = (short)i;
                        }
                    }
                }

                // NOTE: SSCI stops the DSP interrupt and frees the
                // global decompression buffer here if there are no
                // more active channels

                return oldNumChannels;
            }
        }

        public bool Pause(AudioChannelIndex channelIndex)
        {
            if (channelIndex == AudioChannelIndex.NoExistingChannel)
            {
                return false;
            }

            lock (_mutex)
            {
                uint now = SciEngine.Instance.TickCount;
                bool didPause = false;

                if (channelIndex == AudioChannelIndex.AllChannels)
                {
                    if (_pausedAtTick == 0)
                    {
                        _pausedAtTick = (int)now;
                        didPause = true;
                    }
                }
                else if (channelIndex == AudioChannelIndex.RobotChannel)
                {
                    _robotAudioPaused = true;
                    for (var i = 0; i < _numActiveChannels; ++i)
                    {
                        AudioChannel channel = GetChannel(i);
                        if (channel.robot)
                        {
                            channel.pausedAtTick = now;
                        }
                    }

                    // NOTE: The actual engine returns false here regardless of whether
                    // or not channels were paused
                }
                else
                {
                    AudioChannel channel = GetChannel((int)channelIndex);

                    if (channel.pausedAtTick == 0)
                    {
                        channel.pausedAtTick = now;
                        didPause = true;
                    }
                }

                return didPause;
            }
        }

        private void FreeChannel(int channelIndex)
        {
            // The original engine did this:
            // 1. Unlock memory-cached resource, if one existed
            // 2. Close patched audio file descriptor, if one existed
            // 3. Free decompression memory buffer, if one existed
            // 4. Clear monitored memory buffer, if one existed
            lock (_mutex)
            {
                AudioChannel channel = GetChannel(channelIndex);

                // We cannot unlock resources from the audio thread
                // because ResourceManager is not thread-safe; instead,
                // we just record that the resource needs unlocking and
                // unlock it whenever we are on the main thread again
                if (_inAudioThread)
                {
                    _resourcesToUnlock.Add(channel.resource);
                }
                else
                {
                    _resMan.UnlockResource(channel.resource);
                }

                channel.resource = null;
                channel.stream.Dispose();
                channel.stream = null;
                channel.resourceStream.Dispose();
                channel.resourceStream = null;
                channel.converter = null;

                if (_monitoredChannelIndex == channelIndex)
                {
                    _monitoredChannelIndex = -1;
                }
            }
        }

        private AudioChannel GetChannel(int channelIndex)
        {
            lock (_mutex)
            {
                System.Diagnostics.Debug.Assert(channelIndex >= 0 && channelIndex < _numActiveChannels);
                return _channels[channelIndex];
            }
        }

        public int ReadBuffer(short[] buffer, int numSamples)
        {
            lock (_mutex)
            {
                if (_pausedAtTick != 0 || _numActiveChannels == 0)
                {
                    return 0;
                }

                // ResourceManager is not thread-safe so we need to
                // avoid calling into it from the audio thread, but at
                // the same time we need to be able to clear out any
                // finished channels on a regular basis
                _inAudioThread = true;

                FreeUnusedChannels();

                // The caller of `readBuffer` is a rate converter,
                // which reuses (without clearing) an intermediate
                // buffer, so we need to zero the intermediate buffer
                // to prevent mixing into audio data from the last
                // callback.
                Array.Clear(buffer, 0, numSamples);

                // This emulates the attenuated mixing mode of SSCI
                // engine, which reduces the volume of the target
                // buffer when each new channel is mixed in.
                // Instead of manipulating the content of the target
                // buffer when mixing (which would either require
                // modification of RateConverter or an expensive second
                // pass against the entire target buffer), we just
                // scale the volume for each channel in advance, with
                // the earliest (lowest) channel having the highest
                // amount of attenuation (lowest volume).
                byte attenuationAmount;
                byte attenuationStepAmount;
                if (_useModifiedAttenuation)
                {
                    // channel | divisor
                    //       0 | 0  (>> 0)
                    //       1 | 4  (>> 2)
                    //       2 | 8...
                    attenuationAmount = (byte)(_numActiveChannels * 2);
                    attenuationStepAmount = 2;
                }
                else
                {
                    // channel | divisor
                    //       0 | 2  (>> 1)
                    //       1 | 4  (>> 2)
                    //       2 | 6...
                    if (_monitoredChannelIndex == -1 && _numActiveChannels > 1)
                    {
                        attenuationAmount = (byte)(_numActiveChannels + 1);
                        attenuationStepAmount = 1;
                    }
                    else
                    {
                        attenuationAmount = 0;
                        attenuationStepAmount = 0;
                    }
                }

                int maxSamplesWritten = 0;

                for (short channelIndex = 0; channelIndex < _numActiveChannels; ++channelIndex)
                {
                    attenuationAmount -= attenuationStepAmount;

                    AudioChannel channel = GetChannel(channelIndex);

                    if (channel.pausedAtTick != 0 || (channel.robot && _robotAudioPaused))
                    {
                        continue;
                    }

                    // Channel finished fading and had the
                    // stopChannelOnFade flag set, so no longer exists
                    if (channel.fadeStartTick != 0 && ProcessFade(channelIndex))
                    {
                        --channelIndex;
                        continue;
                    }

                    if (channel.robot)
                    {
                        // TODO: Robot audio into output buffer
                        continue;
                    }

                    if (channel.vmd)
                    {
                        // TODO: VMD audio into output buffer
                        continue;
                    }

                    int leftVolume, rightVolume;

                    if (channel.pan == -1 || !IsStereo)
                    {
                        leftVolume = rightVolume = channel.volume * Mixer.MaxChannelVolume / MaxVolume;
                    }
                    else
                    {
                        // TODO: This should match the SCI3 algorithm,
                        // which seems to halve the volume of each
                        // channel when centered; is this intended?
                        leftVolume = channel.volume * (100 - channel.pan) / 100 * Mixer.MaxChannelVolume / MaxVolume;
                        rightVolume = channel.volume * channel.pan / 100 * Mixer.MaxChannelVolume / MaxVolume;
                    }

                    if (_monitoredChannelIndex == -1 && _attenuatedMixing)
                    {
                        leftVolume >>= attenuationAmount;
                        rightVolume >>= attenuationAmount;
                    }

                    if (channelIndex == _monitoredChannelIndex)
                    {
                        int bufferSize = numSamples;
                        if (_monitoredBufferSize < bufferSize)
                        {
                            Array.Resize(ref _monitoredBuffer, numSamples);
                            _monitoredBufferSize = bufferSize;
                        }

                        Array.Clear(_monitoredBuffer, 0, _monitoredBufferSize);

                        _numMonitoredSamples = WriteAudioInternal(channel.stream, channel.converter, _monitoredBuffer,
                            numSamples, leftVolume, rightVolume, channel.loop);

                        Ptr<short> sourceBuffer = _monitoredBuffer;
                        Ptr<short> targetBuffer = buffer;
                        while (sourceBuffer.Offset != _numMonitoredSamples)
                        {
                            RateHelper.ClampedAdd(ref targetBuffer, sourceBuffer.Value);
                            targetBuffer.Offset++;
                            sourceBuffer.Offset++;
                        }

                        if (_numMonitoredSamples > maxSamplesWritten)
                        {
                            maxSamplesWritten = _numMonitoredSamples;
                        }
                    }
                    else if (!channel.stream.IsEndOfStream || channel.loop)
                    {
                        if (_monitoredChannelIndex != -1)
                        {
                            // Audio that is not on the monitored channel is silent
                            // when the monitored channel is active, but the stream still
                            // needs to be read in order to ensure that sound effects sync
                            // up once the monitored channel is turned off. The easiest
                            // way to guarantee this is to just do the normal channel read,
                            // but set the channel volume to zero so nothing is mixed in
                            leftVolume = rightVolume = 0;
                        }

                        int channelSamplesWritten = WriteAudioInternal(channel.stream, channel.converter, buffer,
                            numSamples, leftVolume,
                            rightVolume, channel.loop);
                        if (channelSamplesWritten > maxSamplesWritten)
                        {
                            maxSamplesWritten = channelSamplesWritten;
                        }
                    }
                }

                _inAudioThread = false;

                return maxSamplesWritten;
            }
        }

        public short FindChannelByArgs(int argc, StackPtr argv, int startIndex, Register soundNode)
        {
            // NOTE: argc/argv are already reduced by one in our engine because
            // this call is always made from a subop, so no reduction for the
            // subop is made in this function. SSCI takes extra steps to skip
            // the subop argument.

            argc -= startIndex;
            if (argc <= 0)
            {
                return (short)AudioChannelIndex.AllChannels;
            }

            lock (_mutex)
            {
                if (_numActiveChannels == 0)
                {
                    return (short)AudioChannelIndex.NoExistingChannel;
                }

                ResourceId searchId;

                if (argc < 5)
                {
                    searchId = new ResourceId(ResourceType.Audio, argv[startIndex].ToUInt16());
                }
                else
                {
                    searchId = new ResourceId(
                        ResourceType.Audio36,
                        argv[startIndex].ToUInt16(),
                        (byte)argv[startIndex + 1].ToUInt16(),
                        (byte)argv[startIndex + 2].ToUInt16(),
                        (byte)argv[startIndex + 3].ToUInt16(),
                        (byte)argv[startIndex + 4].ToUInt16()
                    );
                }

                return FindChannelById(searchId, soundNode);
            }
        }

        private short FindChannelById(ResourceId resourceId, Register soundNode)
        {
            lock (_mutex)
            {
                if (_numActiveChannels == 0)
                {
                    return (short)AudioChannelIndex.NoExistingChannel;
                }

                if (resourceId.Type == ResourceType.Audio)
                {
                    for (var i = 0; i < _numActiveChannels; ++i)
                    {
                        AudioChannel channel = _channels[i];
                        if (channel.id == resourceId &&
                            (soundNode.IsNull || soundNode == channel.soundNode)
                        )
                        {
                            return (short)i;
                        }
                    }
                }
                else if (resourceId.Type == ResourceType.Audio36)
                {
                    for (var i = 0; i < _numActiveChannels; ++i)
                    {
                        AudioChannel candidate = GetChannel(i);
                        if (!candidate.robot && candidate.id == resourceId)
                        {
                            return (short)i;
                        }
                    }
                }
                else
                {
                    Error("Audio32::findChannelById: Unknown resource type {0}", resourceId.Type);
                }

                return (short)AudioChannelIndex.NoExistingChannel;
            }
        }

        private int WriteAudioInternal(IAudioStream sourceStream, IRateConverter converter,
            Ptr<short> targetBuffer, int numSamples, int leftVolume, int rightVolume, bool loop)
        {
            int samplesToRead = numSamples;

            // The parent rate converter will request N * 2
            // samples from this `readBuffer` call, because
            // we tell it that we send stereo output, but
            // the source stream we're mixing in may be
            // mono, in which case we need to request half
            // as many samples from the mono stream and let
            // the converter double them for stereo output
            if (!sourceStream.IsStereo)
            {
                samplesToRead >>= 1;
            }

            int samplesWritten = 0;

            do
            {
                if (loop && sourceStream.IsEndOfStream)
                {
                    ((ISeekableAudioStream)sourceStream).Rewind();
                }

                int loopSamplesWritten =
                    converter.Flow(sourceStream, targetBuffer, samplesToRead, leftVolume, rightVolume);

                if (loopSamplesWritten == 0)
                {
                    break;
                }

                samplesToRead -= loopSamplesWritten;
                samplesWritten += loopSamplesWritten;
                targetBuffer.Offset += loopSamplesWritten << 1;
            } while (loop && samplesToRead > 0);

            if (!sourceStream.IsStereo)
            {
                samplesWritten <<= 1;
            }

            return samplesWritten;
        }

        /**
         * Processes an audio fade for the given channel.
         *
         * @returns true if the fade was completed and the
         * channel was stopped.
         */

        private bool ProcessFade(short channelIndex)
        {
            lock (_mutex)
            {
                AudioChannel channel = GetChannel(channelIndex);

                if (channel.fadeStartTick != 0)
                {
                    uint fadeElapsed = (uint)(SciEngine.Instance.TickCount - channel.fadeStartTick);
                    if (fadeElapsed > channel.fadeDuration)
                    {
                        channel.fadeStartTick = 0;
                        if (channel.stopChannelOnFade)
                        {
                            Stop((AudioChannelIndex)channelIndex);
                            return true;
                        }
                        SetVolume((AudioChannelIndex)channelIndex, channel.fadeTargetVolume);
                        return false;
                    }

                    int volume;
                    if (channel.fadeStartVolume > channel.fadeTargetVolume)
                    {
                        volume = (int)(channel.fadeStartVolume -
                                        fadeElapsed * (channel.fadeStartVolume - channel.fadeTargetVolume) /
                                        channel.fadeDuration);
                    }
                    else
                    {
                        volume = (int)(channel.fadeStartVolume +
                                        fadeElapsed * (channel.fadeTargetVolume - channel.fadeStartVolume) /
                                        channel.fadeDuration);
                    }

                    SetVolume((AudioChannelIndex)channelIndex, volume);
                    return false;
                }

                return false;
            }
        }

        public void SetVolume(AudioChannelIndex channelIndex, int volume)
        {
            volume = Math.Min(MaxVolume, volume);
            if (channelIndex == AudioChannelIndex.AllChannels)
            {
                ConfigManager.Instance.Set<int>("sfx_volume", volume * Mixer.MaxChannelVolume / MaxVolume);
                ConfigManager.Instance.Set<int>("speech_volume", volume * Mixer.MaxChannelVolume / MaxVolume);
                _mixer.SetChannelVolume(_handle, volume * Mixer.MaxChannelVolume / MaxVolume);
                SciEngine.Instance.SyncSoundSettings();
            }
            else if (channelIndex != AudioChannelIndex.NoExistingChannel)
            {
                lock (_mutex)
                {
                    GetChannel((int)channelIndex).volume = volume;
                }
            }
        }

        private void FreeUnusedChannels()
        {
            lock (_mutex)
            {
                for (int channelIndex = 0; channelIndex < _numActiveChannels; ++channelIndex)
                {
                    AudioChannel channel = GetChannel(channelIndex);
                    if (channel.stream.IsEndOfStream)
                    {
                        if (channel.loop)
                        {
                            ((ISeekableAudioStream)channel.stream).Rewind();
                        }
                        else
                        {
                            Stop((AudioChannelIndex)channelIndex--);
                        }
                    }
                }

                if (!_inAudioThread)
                {
                    UnlockResources();
                }
            }
        }

        private void UnlockResources()
        {
            lock (_mutex)
            {
                System.Diagnostics.Debug.Assert(!_inAudioThread);

                foreach (var it in _resourcesToUnlock)
                {
                    _resMan.UnlockResource(it);
                }
                _resourcesToUnlock.Clear();
            }
        }

        public bool IsStereo => true;
        public int Rate => _mixer.OutputRate;
        public bool IsEndOfData => _numActiveChannels == 0;
        public bool IsEndOfStream => false;

        public Register KernelPlay(bool autoPlay, int argc, StackPtr argv)
        {
            if (argc == 0)
            {
                return Register.Make(0, _numActiveChannels);
            }

            short channelIndex = FindChannelByArgs(argc, argv, 0, Register.NULL_REG);
            ResourceId resourceId;
            bool loop;
            short volume;
            bool monitor = false;
            Register soundNode = Register.NULL_REG;

            if (argc >= 5)
            {
                resourceId = new ResourceId(ResourceType.Audio36, argv[0].ToUInt16(), (byte)argv[1].ToUInt16(),
                    (byte)argv[2].ToUInt16(), (byte)argv[3].ToUInt16(), (byte)argv[4].ToUInt16());

                if (argc < 6 || argv[5].ToInt16() == 1)
                {
                    loop = false;
                }
                else
                {
                    // NOTE: Uses -1 for infinite loop. Presumably the
                    // engine was supposed to allow counter loops at one
                    // point, but ended up only using loop as a boolean.
                    loop = argv[5].ToInt16() != 0;
                }

                if (argc < 7 || argv[6].ToInt16() < 0 || argv[6].ToInt16() > Audio32.MaxVolume)
                {
                    volume = Audio32.MaxVolume;

                    if (argc >= 7)
                    {
                        monitor = true;
                    }
                }
                else
                {
                    volume = argv[6].ToInt16();
                }
            }
            else
            {
                resourceId = new ResourceId(ResourceType.Audio, argv[0].ToUInt16());

                if (argc < 2 || argv[1].ToInt16() == 1)
                {
                    loop = false;
                }
                else
                {
                    loop = argv[1].ToInt16() != 0;
                }

                // TODO: SCI3 uses the 0x80 bit as a flag to
                // indicate "priority channel", but the volume is clamped
                // in this call to 0x7F so that flag never makes it into
                // the audio subsystem
                if (argc < 3 || argv[2].ToInt16() < 0 || argv[2].ToInt16() > Audio32.MaxVolume)
                {
                    volume = MaxVolume;

                    if (argc >= 3)
                    {
                        monitor = true;
                    }
                }
                else
                {
                    volume = argv[2].ToInt16();
                }

                soundNode = argc == 4 ? argv[3] : Register.NULL_REG;
            }

            return Register.Make(0, Play(channelIndex, resourceId, autoPlay, loop, volume, soundNode, monitor));
        }

        public void SetSampleRate(ushort rate)
        {
            if (rate > _maxAllowedSampleRate)
            {
                rate = _maxAllowedSampleRate;
            }

            _globalSampleRate = rate;
        }

        public void SetBitDepth(byte depth)
        {
            if (depth > _maxAllowedBitDepth)
            {
                depth = _maxAllowedBitDepth;
            }

            _globalBitDepth = depth;
        }

        /**
         * Gets the (fake) bit depth of the hardware DAC.
         * For script compatibility only.
         */

        public byte GetBitDepth()
        {
            return _globalBitDepth;
        }

        /// <summary>
        /// Gets whether attenuated mixing mode is active.
        /// </summary>
        /// <returns></returns>
        public bool GetAttenuatedMixing()
        {
            return _attenuatedMixing;
        }

        /// <summary>
        /// Sets the attenuated mixing mode.
        /// </summary>
        /// <param name="attenuated"></param>
        public void SetAttenuatedMixing(bool attenuated)
        {
            lock (_mutex)
            {
                _attenuatedMixing = attenuated;
            }
        }

        public void SetNumOutputChannels(short numChannels)
        {
            if (numChannels > _maxAllowedOutputChannels)
            {
                numChannels = _maxAllowedOutputChannels;
            }

            _globalNumOutputChannels = (byte)numChannels;
        }

        /**
         * Gets the (fake) number of output (speaker) channels
         * of the hardware DAC. For script compatibility only.
         */
        public byte GetNumOutputChannels()
        {
            return _globalNumOutputChannels;
        }

        /**
	 * Gets the (fake) number of preloaded channels.
	 * For script compatibility only.
	 */
        public byte GetPreload()
        {
            return _preload;
        }

        /**
         * Sets the (fake) number of preloaded channels.
         * For script compatibility only.
         */
        public void SetPreload(byte preload)
        {
            _preload = preload;
        }

        public bool FadeChannel(short channelIndex, short targetVolume, short speed, short steps, bool stopAfterFade)
        {
            lock (_mutex)
            {

                if (channelIndex < 0 || channelIndex >= _numActiveChannels)
                {
                    return false;
                }

                AudioChannel channel = GetChannel(channelIndex);

                if (channel.id.Type != ResourceType.Audio || channel.volume == targetVolume)
                {
                    return false;
                }

                if (steps != 0 && speed != 0)
                {
                    channel.fadeStartTick = (int)SciEngine.Instance.TickCount;
                    channel.fadeStartVolume = channel.volume;
                    channel.fadeTargetVolume = targetVolume;
                    channel.fadeDuration = (uint)(speed * steps);
                    channel.stopChannelOnFade = stopAfterFade;
                }
                else
                {
                    SetVolume(channelIndex, targetVolume);
                }

                return true;
            }
        }

        public bool HasSignal()
        {
            lock (_mutex)
            {

                if (_monitoredChannelIndex == -1)
                {
                    return false;
                }

                var buffer = new Ptr<short>(_monitoredBuffer);
                var end = new Ptr<short>(_monitoredBuffer, _numMonitoredSamples);

                while (buffer != end)
                {
                    var sample = buffer.Value; buffer.Offset++;
                    if (sample > 1280 || sample < -1280)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void SetLoop(short channelIndex, bool loop)
        {
            lock (_mutex)
            {

                if (channelIndex < 0 || channelIndex >= _numActiveChannels)
                {
                    return;
                }

                AudioChannel channel = GetChannel(channelIndex);
                channel.loop = loop;
            }
        }

        public bool PlayRobotAudio(RobotAudioStream.RobotAudioPacket packet)
        {
            // Stop immediately
            if (packet.dataSize == 0)
            {
                Warning("Stopping robot stream by zero-length packet");
                return StopRobotAudio();
            }

            // Flush and then stop
            if (packet.dataSize == -1)
            {
                Warning("Stopping robot stream by negative-length packet");
                return FinishRobotAudio();
            }

            lock (_mutex)
            {
                var channelIndex = FindRobotChannel();

                bool isNewChannel = false;
                if (channelIndex == AudioChannelIndex.NoExistingChannel)
                {
                    if (_numActiveChannels == _channels.Count)
                    {
                        return false;
                    }

                    channelIndex = (AudioChannelIndex)_numActiveChannels++;
                    isNewChannel = true;
                }

                AudioChannel channel = GetChannel((int)channelIndex);

                if (isNewChannel)
                {
                    channel.id = new ResourceId();
                    channel.resource = null;
                    channel.loop = false;
                    channel.robot = true;
                    channel.fadeStartTick = 0;
                    channel.pausedAtTick = 0;
                    channel.soundNode = Register.NULL_REG;
                    channel.volume = MaxVolume;
                    // TODO: SCI3 introduces stereo audio
                    channel.pan = -1;
                    channel.converter = RateHelper.MakeRateConverter(RobotAudioStream.kRobotSampleRate, Rate, false);
                    // The RobotAudioStream buffer size is
                    // ((bytesPerSample * channels * sampleRate * 2000ms) / 1000ms) & ~3
                    // where bytesPerSample = 2, channels = 1, and sampleRate = 22050
                    channel.stream = new RobotAudioStream(88200);
                    _robotAudioPaused = false;

                    if (_numActiveChannels == 1)
                    {
                        _startedAtTick = (int)SciEngine.Instance.TickCount;
                    }
                }

                return ((RobotAudioStream)channel.stream).AddPacket(packet);
            }
        }

        public bool QueryRobotAudio(out RobotAudioStream.StreamState status)
        {
            lock (_mutex)
            {
                var channelIndex = FindRobotChannel();
                if (channelIndex == AudioChannelIndex.NoExistingChannel)
                {
                    status = new RobotAudioStream.StreamState();
                    return false;
                }

                status = ((RobotAudioStream)GetChannel((int)channelIndex).stream).GetStatus();
                return true;
            }
        }
    }
}

#endif
