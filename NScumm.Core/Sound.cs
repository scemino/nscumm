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
using System;
using System.Collections.Generic;
using System.Timers;

namespace NScumm.Core
{
	class Sound
	{
		ScummEngine vm;
		Timer timer;
		Stack<int> soundQueue;
		const int BufferSize = 4096;
		readonly OPL3 opl;
		MidiPlayer player;
		long minicnt;
		bool playing;
		IAudioDriver driver;
		IAudioStream stream;

		class AudioStream: IAudioStream
		{
			readonly Sound sound;

			#region IAudioStream implementation

			public short[] Read ()
			{
				return sound.Read ();
			}

			public int Frequency {
				get {
					return 49700;
				}
			}

			#endregion

			public AudioStream (Sound sound)
			{
				this.sound = sound;
			}
		}

		public Sound (ScummEngine vm, IAudioDriver driver)
		{
			this.vm = vm;
			this.driver = driver;
			soundQueue = new Stack<int> ();
			timer = new Timer (100.7);
			timer.Elapsed += OnCDTimer;

			// initialize output & player
			opl = new OPL3 ();
			player = new MidiPlayer (opl);
			stream = new AudioStream (this);
		}

		public void PlayCDTrack (int track, int numLoops, int startFrame, int duration)
		{
			// Reset the music timer variable at the start of a new track
			vm.Variables [ScummEngine.VariableMusicTimer] = 0;

			// Play it
			//if (!_soundsPaused)
			//    g_system->getAudioCDManager()->play(track, numLoops, startFrame, duration);

			// Start the timer after starting the track. Starting an MP3 track is
			// almost instantaneous, but a CD player may take some time. Hopefully
			// playCD() will block during that delay.
			StartCDTimer ();
		}

		public void StartCDTimer ()
		{
			timer.Start ();
		}

		public void StopCD ()
		{
			timer.Stop ();
		}

		void OnCDTimer (object sender, EventArgs e)
		{
			// FIXME: Turn off the timer when it's no longer needed. In theory, it
			// should be possible to check with pollCD(), but since CD sound isn't
			// properly restarted when reloading a saved game, I don't dare to.

			vm.Variables [ScummEngine.VariableMusicTimer] += 6;
		}

		public void AddSoundToQueue (int sound)
		{
			soundQueue.Push (sound);
		}

		public void ProcessSoundQueue ()
		{
			while (soundQueue.Count > 0) {
				int sound = soundQueue.Pop ();
				if (sound != 0)
					PlaySound (sound);
			}
		}

		void PlaySound (int sound)
		{
			var data = vm.ResourceManager.GetSound (sound);
			player.LoadFromOldLucas (data);
			minicnt = 0;

			driver.Play (stream);
		}

        public void StopAllSounds()
        {
            soundQueue.Clear();
        }

		public int IsSoundRunning (int snd)
		{
			return 0;
		}

		public void Update ()
		{
			if (playing) {
				driver.Update (stream);
			}
		}

		short[] Read ()
		{
			long i, towrite = BufferSize;
			var buffer = new short[towrite];
			long pos = 0;

			// Prepare audiobuf with emulator output
			while (towrite > 0) {
				while (minicnt < 0) {
					minicnt += stream.Frequency * 4;
					playing = player.Update ();
				}
				i = Math.Min (towrite, (long)(minicnt / player.GetRefresh () + 4) & ~3);
				var n = Update (buffer, pos, i);
				pos += n;
				towrite -= i;
				minicnt -= (long)(player.GetRefresh () * i);
			}

			return buffer;
		}

		long Update (short[] buf, long pos, long samples)
		{
			for (int i = 0; i < samples; i += 4) {
				var data = opl.Read ();
				Array.Copy (data, 0, buf, pos, 2);
				pos += 2;
			}
			return samples / 2;
		}

        public void SoundKludge(int[] items)
        {
            if (items[0] == -1)
            {
                ProcessSoundQueue();
            }
            else
            {
                soundQueue.Push(items.Length);
                foreach (var item in items)
                {
                    soundQueue.Push(item);
                }
            }
        }

	}
}
