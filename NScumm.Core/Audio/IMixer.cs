//
//  Mixer.cs
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

namespace NScumm.Core.Audio
{
    public enum SoundType
    {
        Plain = 0,
        Music = 1,
        SFX = 2,
        Speech = 3
    }

    public interface IMixer
    {
        int OutputRate { get; }

        bool IsReady { get; }

        /// <summary>
        /// Start playing the given audio stream.
        /// </summary>
        /// <param name="type">the type (voice/sfx/music) of the stream.</param>
        /// <param name="stream">the actual AudioStream to be played.</param>
        /// <param name="id">a unique id assigned to this stream.</param>
        /// <param name="volume">the volume with which to play the sound, ranging from 0 to 255.</param>
        /// <param name="balance">the balance with which to play the sound, ranging from -127 to 127 (full left to full right), 0 is balanced, -128 is invalid.</param>
        /// <param name="autofreeStream">a flag indicating whether the stream should be freed after playback finished.</param>
        /// <param name="permanent">a flag indicating whether a plain stopAll call should not stop this particular stream.</param>
        /// <param name="reverseStereo">a flag indicating whether left and right channels shall be swapped.</param>
        /// <description>Note that the sound id assigned below is unique. At most one stream
        /// with a given id can play at any given time. Trying to play a sound
        /// with an id that is already in use causes the new sound to be not played.
        /// </description>
        /// <returns>a SoundHandle which can be used to reference and control 
        /// the stream via suitable mixer methods</returns>
        SoundHandle PlayStream(
            SoundType type,
            IAudioStream stream,
            int id = -1,
            int volume = 255,
            int balance = 0,
            bool autofreeStream = true,
            bool permanent = false,
            bool reverseStereo = false);

        void StopID(int id);

        void StopAll();

        void StopHandle(SoundHandle handle);

        bool IsSoundIdActive(int id);

        bool IsSoundHandleActive(SoundHandle handle);

        bool HasActiveChannelOfType(SoundType type);

        /// <summary>
        /// Pause/unpause all sounds, including all regular and permanent
        /// channels.
        /// </summary>
        /// <param name="pause">If set to <c>true</c> pause everything, false to unpause.</param>
        void PauseAll(bool pause);

        /// <summary>
        /// Get approximation of for how long the channel has been playing.
        /// </summary>
        /// <returns>The sound elapsed time.</returns>
        /// <param name="handle">Compressed file sound handle.</param>
        int GetSoundElapsedTime(SoundHandle handle);

        /// <summary>
        /// Set the channel volume for the given handle.
        /// </summary>
        /// <param name="handle">The sound to affect.</param>
        /// <param name="volume">The new channel volume (0 - kMaxChannelVolume).</param>
        void SetChannelVolume(SoundHandle handle, int volume);

        /// <summary>
        /// Get the channel volume for the given handle.
        /// </summary>
        /// <returns>The channel volume.</returns>
        /// <param name="handle">The sound to affect.</param>
        int GetChannelVolume(SoundHandle handle);

        /// <summary>
        /// Set the channel balance for the given handle.
        /// </summary>
        /// <param name="handle">the sound to affect</param>
        /// <param name="balance">the new channel balance.</param>
        /// <remarks>(-127 ... 0 ... 127) corresponds to (left ... center ... right)</remarks>
        void SetChannelBalance(SoundHandle handle, int balance);

        /// <summary>
        /// Get the channel balance for the given handle.
        /// </summary>
        /// <returns>The channel balance.</returns>
        /// <param name="handle">The sound to affect.</param>
        int GetChannelBalance(SoundHandle handle);

        /// <summary>
        /// Query the global volume.
        /// </summary>
        /// <returns>The volume for sound type.</returns>
        /// <param name="type">The global music volume, 0 - MaxMixerVolume.</param>
        int GetVolumeForSoundType(SoundType type);
    }
}

