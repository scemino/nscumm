/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using NScumm.Core.Audio;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using NScumm.Core;

namespace NScumm.MonoGame
{
    public class OpenALDriver : Mixer, IDisposable
    {
        readonly AudioContext audioContext;
        readonly int bufferId;
        readonly int sourceId;
        short[] buffer;
        const int Frequency = 44100;

        public OpenALDriver()
            : base(Frequency)
        {
            audioContext = new AudioContext();
            buffer = new short[Frequency * 2];
            bufferId = AL.GenBuffer();
            sourceId = AL.GenSource();
            CheckError();
        }

        bool check;

        static void CheckError()
        {
            var error = AL.GetError();
            if (error != ALError.NoError)
            {
                var err = AL.GetErrorString(error);
                Console.Error.WriteLine("AL Error: {0}", err);
            }
        }

        public void Update()
        {
            int val = 0;
            if (check)
            {
                AL.GetSource(sourceId, ALGetSourcei.BuffersProcessed, out val);
            }

            if (!check || val > 0)
            {
                Array.Clear(buffer, 0, buffer.Length);
                var len = MixCallback(buffer);
                if (len > 0)
                {
                    AL.SourceUnqueueBuffer(sourceId);
                    AL.BufferData(bufferId, ALFormat.Stereo16, buffer, len * 4, Frequency);
                    CheckError();
                    AL.SourceQueueBuffer(sourceId, bufferId);
                    CheckError();
                    if (AL.GetSourceState(sourceId) != ALSourceState.Playing)
                    {
                        AL.SourcePlay(sourceId);
                        CheckError();
                    }
                    check = true;
                }
            }
        }

        #region IDisposable implementation

        public void Dispose()
        {
            AL.DeleteBuffer(bufferId);
            AL.DeleteSource(sourceId);
            audioContext.Dispose();
        }

        #endregion

    }
	
}
