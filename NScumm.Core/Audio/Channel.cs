//
//  Channel.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System.Diagnostics;

namespace NScumm.Core.Audio
{
    class Channel
    {
        const int MaxChannelVolume = 255;

        Mixer _mixer;
        IAudioStream _stream;
        bool _permanent;
        int _volume;
        int _pauseLevel;
        int _pauseStartTime;
        int _balance;
        int _volL;
        int _volR;
        int _samplesConsumed;
        int _samplesDecoded;
        int _pauseTime;
        int _mixerTimeStamp;
        IRateConverter _converter;

        public Channel(Mixer mixer, SoundType type, IAudioStream stream, bool autofreeStream, bool reverseStereo, int id, bool permanent)
        {
            _mixer = mixer;
            Type = type;
            _stream = stream;
            Id = id;
            _permanent = permanent;
            _volume = MaxChannelVolume;
            _converter = MakeRateConverter(_stream.Rate, _mixer.OutputRate, _stream.IsStereo, reverseStereo);
        }

        /**
         * Mixes the channel's samples into the given buffer.
         *
         * @param data buffer where to mix the data
         * @param len  number of sample *pairs*. So a value of
         *             10 means that the buffer contains twice 10 sample, each
         *             16 bits, for a total of 40 bytes.
         * @return number of sample pairs processed (which can still be silence!)
         */
        public int Mix(short[] data, int count)
        {
            Debug.Assert(_stream != null);

            int res = 0;
            if (_stream.IsEndOfData)
            {
                // TODO: call drain method
            }
            else
            {
                Debug.Assert(_converter != null);
                _samplesConsumed = _samplesDecoded;
                _mixerTimeStamp = Environment.TickCount;
                _pauseTime = 0;
                res = _converter.Flow(_stream, data, count, _volL, _volR);
                _samplesDecoded += res;
            }

            return res;
        }

        /**
         * Queries whether the channel is still playing or not.
         */
        public bool IsFinished{ get { return _stream.IsEndOfStream; } }

        /**
         * Queries whether the channel is a permanent channel.
         * A permanent channel is not affected by a Mixer::stopAll
         * call.
         */
        public bool IsPermanent { get { return _permanent; } }

        /**
         * Returns the id of the channel.
         */
        public int Id { get; private set; }

        public SoundType Type { get; private set; }

        /**
         * Pauses or unpaused the channel in a recursive fashion.
         *
         * @param paused true, when the channel should be paused.
         *               false when it should be unpaused.
         */
        public void Pause(bool paused)
        {
            if (paused) {
                _pauseLevel++;

                if (_pauseLevel == 1)
                    _pauseStartTime = Environment.TickCount;
            } else if (_pauseLevel > 0) {
                _pauseLevel--;

                if (_pauseLevel==0) {
                    _pauseTime = (Environment.TickCount - _pauseStartTime);
                    _pauseStartTime = 0;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the channel is currently paused.
        /// </summary>
        /// <value><c>true</c> if the channel is currently paused; otherwise, <c>false</c>.</value>
        public bool IsPaused { get { return (_pauseLevel != 0); } }

        public int Volume
        {
            get{ return _volume; }
            set
            {
                _volume = value;
                UpdateChannelVolumes();
            }
        }

        public int Balance
        {
            get{ return _balance; }
            set
            {
                _balance = value;
                UpdateChannelVolumes();
            }
        }

        public SoundHandle Handle{ get; set; }

        public Timestamp GetElapsedTime() {
            int rate = _mixer.OutputRate;
            int delta = 0;

            var ts=new Timestamp(0, rate);

            if (_mixerTimeStamp == 0)
                return ts;

            if (IsPaused)
                delta = _pauseStartTime - _mixerTimeStamp;
            else
                delta = Environment.TickCount - _mixerTimeStamp - _pauseTime;

            // Convert the number of samples into a time duration.

            ts = ts.AddFrames(_samplesConsumed);
            ts = ts.AddMsecs(delta);

            // In theory it would seem like a good idea to limit the approximation
            // so that it never exceeds the theoretical upper bound set by
            // _samplesDecoded. Meanwhile, back in the real world, doing so makes
            // the Broken Sword cutscenes noticeably jerkier. I guess the mixer
            // isn't invoked at the regular intervals that I first imagined.

            return ts;
        }

        void UpdateChannelVolumes()
        {
            // From the channel balance/volume and the global volume, we compute
            // the effective volume for the left and right channel. Note the
            // slightly odd divisor: the 255 reflects the fact that the maximal
            // value for _volume is 255, while the 127 is there because the
            // balance value ranges from -127 to 127.  The mixer (music/sound)
            // volume is in the range 0 - kMaxMixerVolume.
            // Hence, the vol_l/vol_r values will be in that range, too

            if (!_mixer.IsSoundTypeMuted(Type))
            {
                int vol = _mixer.GetVolumeForSoundType(Type) * _volume;

                if (_balance == 0)
                {
                    _volL = vol / Mixer.MaxChannelVolume;
                    _volR = vol / Mixer.MaxChannelVolume;
                }
                else if (_balance < 0)
                {
                    _volL = vol / Mixer.MaxChannelVolume;
                    _volR = ((127 + _balance) * vol) / (Mixer.MaxChannelVolume * 127);
                }
                else
                {
                    _volL = ((127 - _balance) * vol) / (Mixer.MaxChannelVolume * 127);
                    _volR = vol / Mixer.MaxChannelVolume;
                }
            }
            else
            {
                _volL = _volR = 0;
            }
        }

        static IRateConverter MakeRateConverter(int inrate, int outrate, bool stereo, bool reverseStereo)
        {
            if (inrate != outrate)
            {
                if ((inrate % outrate) == 0)
                {
                    return new SimpleRateConverter(inrate, outrate, stereo, reverseStereo);
                }
                else
                {
                    return new LinearRateConverter(inrate, outrate, stereo, reverseStereo);
                }
            }
            else
            {
                return new CopyRateConverter(stereo, reverseStereo);
            }
        }
    }
    
}
