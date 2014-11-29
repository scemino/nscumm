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

namespace NScumm.MonoGame
{
    public class OpenALDriver : Mixer, IDisposable
    {
        readonly AudioContext audioContext;
        readonly int[] bufferIds;
        readonly int sourceId;
        short[] buffer;
        const int NumBuffers = 2;
        public const int Frequency = 44100;
        public const int NumSamples = 44100;

        public OpenALDriver()
            : base(Frequency)
        {
            audioContext = new AudioContext();
            buffer = new short[NumSamples / NumBuffers];
            bufferIds = AL.GenBuffers(NumBuffers);
            sourceId = AL.GenSource();
            CheckError();

            foreach (var bufId in bufferIds)
            {
                var len = MixCallback(buffer);
                AL.BufferData(bufId, ALFormat.Stereo16, buffer, len * 2, Frequency);
                Array.Clear(buffer, 0, buffer.Length);
                CheckError();
                AL.SourceQueueBuffer(sourceId, bufId);
                CheckError();
            }

            if (AL.GetSourceState(sourceId) != ALSourceState.Playing)
            {
                AL.SourcePlay(sourceId);
                CheckError();
            }
        }

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
            int val;
            AL.GetSource(sourceId, ALGetSourcei.BuffersProcessed, out val);
            if (val <= 0)
                return;
            while (val-- != 0)
            {
                var len = MixCallback(buffer);
//                    Console.WriteLine("Mix: {0}", len);
                if (len > 0)
                {
                    var bufId = AL.SourceUnqueueBuffer(sourceId);
                    AL.BufferData(bufId, ALFormat.Stereo16, buffer, len * 4, Frequency);
                    Array.Clear(buffer, 0, buffer.Length);
                    CheckError();
                    AL.SourceQueueBuffer(sourceId, bufId);
                    CheckError();
                }
            }

            if (AL.GetSourceState(sourceId) != ALSourceState.Playing)
            {
                AL.SourcePlay(sourceId);
                CheckError();
            }
        }

        #region IDisposable implementation

        public void Dispose()
        {
            AL.DeleteBuffers(bufferIds);
            AL.DeleteSource(sourceId);
            audioContext.Dispose();
        }

        #endregion

    }
	
}
