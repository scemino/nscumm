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
using System.Collections.Generic;

namespace NScumm.MonoGame
{
	public class OpenALDriver : IAudioDriver, IDisposable
	{
		readonly AudioContext audioContext;
		readonly List<int> bufferIds;
		readonly List<int> sourceIds;
		const int NumBuffers = 10;

		public OpenALDriver ()
		{
			audioContext = new AudioContext ();
			bufferIds = new List<int> ();
			sourceIds = new List<int> ();
		}

		public void Play (IAudioStream stream)
		{
			var bufferIds = AL.GenBuffers (NumBuffers);
			this.bufferIds.AddRange (bufferIds);
			var sourceId = AL.GenSource ();
			sourceIds.Add (sourceId);

			if (AL.GetError () != ALError.NoError) {
				Console.Error.WriteLine ("Error generating :(");
				return;
			}

			ALError error;
			foreach (var bufferId in bufferIds) {
				var data = stream.Read ();
				AL.BufferData (bufferId, ALFormat.Stereo16, data, data.Length, stream.Frequency);

				error = AL.GetError ();
				if (error != ALError.NoError) {
					Console.Error.WriteLine ("Error loading :( {0}", AL.GetErrorString (error));
					return;
				}
			}

			AL.SourceQueueBuffers (sourceId, bufferIds.Length, bufferIds);
			AL.SourcePlay (sourceId);
			error = AL.GetError ();
			if (error != ALError.NoError) {
				Console.Error.WriteLine ("Error Starting :( {0}", AL.GetErrorString (error));
				return;
			}
		}

		public void Update (IAudioStream stream)
		{
			int val;

			if (sourceIds.Count > 0) {
				var sourceId = sourceIds [0];
				AL.GetSource (sourceId, ALGetSourcei.BuffersProcessed, out val);
				if (val <= 0)
					return;
				while (val-- != 0) {
					var buffer = stream.Read ();

					var bufId = AL.SourceUnqueueBuffer (sourceId);
					AL.BufferData (bufId, ALFormat.Stereo16, buffer, buffer.Length, stream.Frequency);
					AL.SourceQueueBuffer (sourceId, bufId);
					if (AL.GetError () != ALError.NoError) {
						Console.Error.WriteLine ("Error buffering :(");
						return;
					}
				}

				var state = AL.GetSourceState (sourceId);
				if (state != ALSourceState.Playing)
					AL.SourcePlay (sourceId);
			}
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			if (bufferIds.Count > 0) {
				AL.DeleteBuffers (bufferIds.ToArray ());
			}
			if (sourceIds.Count > 0) {
				AL.DeleteSources (sourceIds.ToArray ());
			}
			audioContext.Dispose ();
		}

		#endregion

	}
	
}
