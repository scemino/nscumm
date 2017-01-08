
// this code is from: http://stackoverflow.com/questions/15959903/xna-monogame-dynamicsoundeffectinstance-buffer-already-full-exception

using System;

#if MONOMAC
using MonoMac.OpenAL;
#else
using OpenTK.Audio.OpenAL;
#endif
using System.Threading;

namespace Microsoft.Xna.Framework.Audio
{
    public sealed class DynamicSoundEffectInstance : IDisposable
    {
        private const int BUFFERCOUNT = 2;

        private SoundState soundState = SoundState.Stopped;
        private AudioChannels channels;
        private int sampleRate;
        private ALFormat format;
        private float volume = 1.0f;
        private int sourceId;
        private int[] bufferIds;
        private int[] bufferIdsToFill;
        private int currentBufferToFill;
        private bool isDisposed;
        private bool hasSourceId;
        private Thread bufferFillerThread;
        private bool _done;

        // Events
        public event EventHandler<EventArgs> BufferNeeded;

        internal void OnBufferNeeded(EventArgs args)
        {
            if (BufferNeeded != null)
            {
                BufferNeeded(this, args);
            }
        }

        public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels)
        {
            this.sampleRate = sampleRate;
            this.channels = channels;
            switch (channels)
            {
                case AudioChannels.Mono:
                    this.format = ALFormat.Mono16;
                    break;
                case AudioChannels.Stereo:
                    this.format = ALFormat.Stereo16;
                    break;
            }
        }

		public bool IsDisposed => isDisposed;

        public float Volume
        {
            get
            {
                return volume;
            }

            set
            {
                volume = value;
                if (hasSourceId)
                {
                    // Volume
                    AL.Source(sourceId, ALSourcef.Gain, volume * SoundEffect.MasterVolume);
                }

            }
        }

        public SoundState State => soundState;

        public void Play()
        {
            if (!hasSourceId)
            {
                bufferIds = AL.GenBuffers(BUFFERCOUNT);
                sourceId = AL.GenSource();
                hasSourceId = true;
            }
            soundState = SoundState.Playing;

            if (bufferFillerThread == null)
            {
                bufferIdsToFill = bufferIds;
                currentBufferToFill = 0;
                OnBufferNeeded(EventArgs.Empty);
                bufferFillerThread = new Thread(new ThreadStart(BufferFiller));
                bufferFillerThread.Start();
            }

            AL.SourcePlay(sourceId);
        }

        public void Pause()
        {
            if (hasSourceId)
            {
                AL.SourcePause(sourceId);
                soundState = SoundState.Paused;
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                Stop();
                AL.DeleteBuffers(bufferIds);
                AL.DeleteSource(sourceId);
                bufferIdsToFill = null;
                hasSourceId = false;
                isDisposed = true;
            }
        }

        public void Stop()
        {
            if (hasSourceId)
            {
                _done = true;
                AL.SourceStop(sourceId);
                int pendingBuffers = PendingBufferCount;
                if (pendingBuffers > 0)
                    AL.SourceUnqueueBuffers(sourceId, PendingBufferCount);
            }
            soundState = SoundState.Stopped;
        }

        public int GetSampleSizeInBytes(TimeSpan duration)
        {
            int size = (int)(duration.TotalMilliseconds * ((float)sampleRate / 1000.0f));
            return (size + (size & 1)) * 16;
        }

        public void SubmitBuffer(byte[] buffer, int count)
        {
            this.SubmitBuffer(buffer, 0, count);
        }

        public void SubmitBuffer(byte[] buffer, int offset, int count)
        {
            if (bufferIdsToFill != null)
            {
                AL.BufferData(bufferIdsToFill[currentBufferToFill], format, buffer, count, sampleRate);
                AL.SourceQueueBuffer(sourceId, bufferIdsToFill[currentBufferToFill]);
                currentBufferToFill++;
                if (currentBufferToFill >= bufferIdsToFill.Length)
                    bufferIdsToFill = null;
                else
                    OnBufferNeeded(EventArgs.Empty);
            }
            else {
                throw new Exception("Buffer already full.");
            }
        }

        private void BufferFiller()
        {
            while (!_done)
            {
                var state = AL.GetSourceState(sourceId);
                if (state == ALSourceState.Stopped || state == ALSourceState.Initial)
                    AL.SourcePlay(sourceId);

                if (bufferIdsToFill != null)
                    continue;

                int buffersProcessed;
                AL.GetSource(sourceId, ALGetSourcei.BuffersProcessed, out buffersProcessed);

                if (buffersProcessed == 0)
                    continue;

                bufferIdsToFill = AL.SourceUnqueueBuffers(sourceId, buffersProcessed);
                currentBufferToFill = 0;
                OnBufferNeeded(EventArgs.Empty);
            }
        }

        public int PendingBufferCount
        {
            get
            {
                if (hasSourceId)
                {
                    int buffersQueued;
                    AL.GetSource(sourceId, ALGetSourcei.BuffersQueued, out buffersQueued);
                    return buffersQueued;
                }
                return 0;
            }
        }
    }
}