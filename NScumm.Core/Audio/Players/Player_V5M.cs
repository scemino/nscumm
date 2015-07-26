//
//  Player_V5M.cs
//
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

using System.Diagnostics;

namespace NScumm.Core.Audio
{

    class Player_V5M : Player_Mac
    {
        public Player_V5M(ScummEngine scumm, IMixer mixer)
            : base(scumm, mixer, 3, 0x07, false)
        {
        }

        protected override bool CheckMusicAvailable()
        {
            var resource = new MacResManager(ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path));

            for (int i = 0; i < monkeyIslandFileNames.Length; i++)
            {
                if (resource.Exists(monkeyIslandFileNames[i]))
                {
                    return true;
                }
            }

//            GUI::MessageDialog dialog(_(
//                "Could not find the 'Monkey Island' Macintosh executable to read the\n"
//                "instruments from. Music will be disabled."), _("OK"));
//            dialog.runModal();
            return false;
        }

        protected override bool LoadMusic(byte[] ptr)
        {
            var offset = 0;
            var resource = new MacResManager(ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path));
            bool found = false;
            uint i;

            for (i = 0; i < monkeyIslandFileNames.Length; i++)
            {
                if (resource.Open(monkeyIslandFileNames[i]))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            offset += 8;
            // TODO: Decipher the unknown bytes in the header. For now, skip 'em
            offset += 28;

            var idArray = resource.GetResIDArray(RES_SND);

            // Load the three channels and their instruments
            for (i = 0; i < 3; i++)
            {
                Debug.Assert(ptr.ToUInt32BigEndian(offset) == System.Text.Encoding.UTF8.GetBytes("Chan").ToUInt32BigEndian());
                var len = ptr.ToUInt32BigEndian(offset + 4);
                var instrument = ptr.ToUInt32BigEndian(offset + 8);

                _channel[i]._length = len - 20;
                _channel[i]._data = ptr;
                _channel[i]._dataOffset = offset + 12;
                _channel[i]._looped = (ptr.ToUInt32BigEndian((int)(offset + len - 8)) == System.Text.Encoding.UTF8.GetBytes("Loop").ToUInt32BigEndian());
                _channel[i]._pos = 0;
                _channel[i]._pitchModifier = 0;
                _channel[i]._velocity = 0;
                _channel[i]._remaining = 0;
                _channel[i]._notesLeft = true;

                for (uint j = 0; j < idArray.Length; j++)
                {
                    var name = resource.GetResName(RES_SND, idArray[j]);
                    if (instrument == System.Text.Encoding.UTF8.GetBytes(name).ToUInt32BigEndian())
                    {
                        Debug.WriteLine("Player_V5M::loadMusic: Channel {0}: Loading instrument '{1}'", i, name);
                        var stream = resource.GetResource(RES_SND, idArray[j]);

                        if (!_channel[i].LoadInstrument(stream))
                        {
                            resource.Close();
                            return false;
                        }

                        break;
                    }
                }

                offset += (int)len;
            }

            resource.Close();

            // The last note of each channel is just zeroes. We will adjust this
            // note so that all the channels end at the same time.

            uint[] samples = new uint[3];
            uint maxSamples = 0;
            for (i = 0; i < 3; i++)
            {
                samples[i] = 0;
                for (uint j = 0; j < _channel[i]._length; j += 4)
                {
                    samples[i] += DurationToSamples(_channel[i]._data.ToUInt16BigEndian((int)(_channel[i]._dataOffset + j)));
                }
                if (samples[i] > maxSamples)
                {
                    maxSamples = samples[i];
                }
            }

            for (i = 0; i < 3; i++)
            {
                _lastNoteSamples[i] = maxSamples - samples[i];
            }

            return true;
        }

        protected override bool GetNextNote(int ch, out uint samples, out int pitchModifier, out byte velocity)
        {
            if (_channel[ch]._pos >= _channel[ch]._length)
            {
                if (!_channel[ch]._looped)
                {
                    _channel[ch]._notesLeft = false;
                    samples = 0;
                    pitchModifier = 0;
                    velocity = 0;
                    return false;
                }
                // FIXME: Jamieson630: The jump seems to be happening
                // too quickly! There should maybe be a pause after
                // the last Note Off? But I couldn't find one in the
                // MI1 Lookout music, where I was hearing problems.
                _channel[ch]._pos = 0;
            }
            ushort duration = _channel[ch]._data.ToUInt16BigEndian((int)(_channel[ch]._dataOffset + _channel[ch]._pos));
            byte note = _channel[ch]._data[_channel[ch]._dataOffset + _channel[ch]._pos + 2];
            samples = DurationToSamples(duration);

            if (note != 1)
            {
                _channel[ch]._instrument.NewNote();
            }

            if (note > 1)
            {
                pitchModifier = NoteToPitchModifier(note, _channel[ch]._instrument);
                velocity = _channel[ch]._data[_channel[ch]._dataOffset + _channel[ch]._pos + 3];
            }
            else if (note == 1)
            {
                // This is guesswork, but Monkey Island uses two different
                // "special" note values: 0, which is clearly a rest, and 1
                // which is... I thought at first it was a "soft" key off, to
                // fade out the note, but listening to the music in a Mac
                // emulator (which unfortunately doesn't work all that well),
                // I hear no trace of fading out.
                //
                // It could mean "change the volume on the current note", but
                // I can't hear that either, and it always seems to use the
                // exact same velocity on this note.
                //
                // So it appears it really just is a "hold the current note",
                // but why? Couldn't they just have made the original note
                // longer?

                pitchModifier = _channel[ch]._pitchModifier;
                velocity = _channel[ch]._velocity;
            }
            else
            {
                pitchModifier = 0;
                velocity = 0;
            }

            _channel[ch]._pos += 4;

            if (_channel[ch]._pos >= _channel[ch]._length)
            {
                samples = _lastNoteSamples[ch];
            }
            return true;
        }

        readonly uint[] _lastNoteSamples = new uint[3];

        // Try both with and without underscore in the filename, because hfsutils may
        // turn the space into an underscore. At least, it did for me.

        static readonly string[] monkeyIslandFileNames =
            {
                "Monkey Island",
                "Monkey_Island"
            };
    }
}
