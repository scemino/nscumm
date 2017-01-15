
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
    public enum AudioChannels
    {
        Mono,
        Stereo
    }

    public sealed class DynamicSoundEffectInstance : IDisposable
    {
        private const int Buffercount = 2;

        private readonly int _sampleRate;
        private readonly ALFormat _format;
        private int _sourceId;
        private int[] _bufferIds;
        private int[] _bufferIdsToFill;
        private int _currentBufferToFill;
        private bool _isDisposed;
        private bool _hasSourceId;
        private Thread _bufferFillerThread;
        private bool _done;

        // Events
        public event EventHandler<EventArgs> BufferNeeded;

        public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels)
        {
            _sampleRate = sampleRate;
            switch (channels)
            {
                case AudioChannels.Mono:
                    _format = ALFormat.Mono16;
                    break;
                case AudioChannels.Stereo:
                    _format = ALFormat.Stereo16;
                    break;
            }
        }

        public void Play()
        {
            if (!_hasSourceId)
            {
                _bufferIds = AL.GenBuffers(Buffercount);
                _sourceId = AL.GenSource();
                _hasSourceId = true;
            }
            if (_bufferFillerThread == null)
            {
                _bufferIdsToFill = _bufferIds;
                _currentBufferToFill = 0;
                OnBufferNeeded(EventArgs.Empty);
                _bufferFillerThread = new Thread(BufferFiller);
                _bufferFillerThread.Start();
            }

            AL.SourcePlay(_sourceId);
        }

        public void Pause()
        {
            if (_hasSourceId)
            {
                AL.SourcePause(_sourceId);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                AL.DeleteBuffers(_bufferIds);
                AL.DeleteSource(_sourceId);
                _bufferIdsToFill = null;
                _hasSourceId = false;
                _isDisposed = true;
            }
        }

        public void Stop()
        {
            if (_hasSourceId)
            {
                _done = true;
                AL.SourceStop(_sourceId);
                int pendingBuffers = PendingBufferCount;
                if (pendingBuffers > 0)
                    AL.SourceUnqueueBuffers(_sourceId, PendingBufferCount);
            }
        }

        public void SubmitBuffer(byte[] buffer, int count)
        {
            if (_bufferIdsToFill != null)
            {
                AL.BufferData(_bufferIdsToFill[_currentBufferToFill], _format, buffer, count, _sampleRate);
                AL.SourceQueueBuffer(_sourceId, _bufferIdsToFill[_currentBufferToFill]);
                _currentBufferToFill++;
                if (_currentBufferToFill >= _bufferIdsToFill.Length)
                    _bufferIdsToFill = null;
                else
                    OnBufferNeeded(EventArgs.Empty);
            }
            else {
                throw new Exception("Buffer already full.");
            }
        }

        private void OnBufferNeeded(EventArgs args)
        {
            BufferNeeded?.Invoke(this, args);
        }

        private void BufferFiller()
        {
            while (!_done)
            {
                var state = AL.GetSourceState(_sourceId);
                if (state == ALSourceState.Stopped || state == ALSourceState.Initial)
                    AL.SourcePlay(_sourceId);

                if (_bufferIdsToFill != null)
                    continue;

                int buffersProcessed;
                AL.GetSource(_sourceId, ALGetSourcei.BuffersProcessed, out buffersProcessed);

                if (buffersProcessed == 0)
                    continue;

                _bufferIdsToFill = AL.SourceUnqueueBuffers(_sourceId, buffersProcessed);
                _currentBufferToFill = 0;
                OnBufferNeeded(EventArgs.Empty);
            }
        }

        private int PendingBufferCount
        {
            get
            {
                if (!_hasSourceId) return 0;

                int buffersQueued;
                AL.GetSource(_sourceId, ALGetSourcei.BuffersQueued, out buffersQueued);
                return buffersQueued;
            }
        }
    }
}