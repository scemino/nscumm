//
//  Player_V3M.cs
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
    /**
 * Scumm V3 Macintosh music driver.
 */
    class Player_V3M : Player_Mac
    {
        public Player_V3M(ScummEngine scumm, IMixer mixer)
            : base(scumm, mixer, 5, 0x1E, true)
        {
            // Channel 0 seems to be what was played on low-end macs, that couldn't
            // handle multi-channel music and play the game at the same time. I'm
            // not sure if stream 4 is ever used, but let's use it just in case.

        }

        protected override bool CheckMusicAvailable()
        {
            var resource = new MacResManager(ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path));

            for (int i = 0; i < loomFileNames.Length; i++) {
                if (resource.Exists(loomFileNames[i])) {
                    return true;
                }
            }

//            Debug.WriteLine("Could not find the 'Loom' Macintosh executable to read the \n\ninstruments from. Music will be disabled.");
//            GUI::MessageDialog dialog(_(
//                "Could not find the 'Loom' Macintosh executable to read the\n"
//                "instruments from. Music will be disabled."), _("OK"));
//            dialog.runModal();
            return false;
        }

        protected override bool LoadMusic(byte[] ptr)
        {
            var resource = new MacResManager(ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path));
            bool found = false;

            for (int i = 0; i < loomFileNames.Length; i++)
            {
                if (resource.Open(loomFileNames[i]))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            if (ptr[4] != 's' || ptr[5] != 'o')
            {
                // Like the original we ignore all sound resources which do not have
                // a 'so' tag in them.
                // See bug #3602239 ("Mac Loom crashes using opening spell on
                // gravestone") for a case where this is required. Loom Mac tries to
                // play resource 11 here. This resource is no Mac sound resource
                // though, it is a PC Speaker resource. A test with the original
                // interpreter also has shown that no sound is played while the
                // screen is shaking.
                Debug.WriteLine("Player_V3M::loadMusic: Skipping unknown music type {0:X2}{1:X2}", ptr[4], ptr[5]);
                resource.Close();
                return false;
            }

            for (var i = 0; i < 5; i++)
            {
                var instrument = ptr.ToUInt16BigEndian(20 + 2 * i);
                int offset = ptr.ToUInt16BigEndian(30 + 2 * i);

                _channel[i]._looped = false;
                _channel[i]._length = (uint)(ptr.ToUInt16BigEndian(offset + 4) * 3);
                _channel[i]._data = ptr;
                _channel[i]._dataOffset = offset + 6;
                _channel[i]._pos = 0;
                _channel[i]._pitchModifier = 0;
                _channel[i]._velocity = 0;
                _channel[i]._remaining = 0;
                _channel[i]._notesLeft = true;

                var stream = resource.GetResource(RES_SND, instrument);
                if (_channel[i].LoadInstrument(stream))
                {
                    Debug.WriteLine("Player_V3M::loadMusic: Channel {0} - Loaded Instrument {1} ({2})", i, instrument, resource.GetResName(RES_SND, instrument));
                }
                else
                {
                    resource.Close();
                    return false;
                }
            }

            resource.Close();
            return true;
        }

        protected override bool GetNextNote(int ch, out uint samples, out int pitchModifier, out byte velocity)
        {
            _channel[ch]._instrument.NewNote();
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
                _channel[ch]._pos = 0;
            }
            ushort duration = _channel[ch]._data.ToUInt16BigEndian(_channel[ch]._dataOffset + (int)_channel[ch]._pos);
            byte note = _channel[ch]._data[_channel[ch]._dataOffset + _channel[ch]._pos + 2];
            samples = DurationToSamples(duration);
            if (note > 0)
            {
                pitchModifier = NoteToPitchModifier(note, _channel[ch]._instrument);
                velocity = 127;
            }
            else
            {
                pitchModifier = 0;
                velocity = 0;
            }
            _channel[ch]._pos += 3;
            return true;
        }

        static readonly string[] loomFileNames =
            {
                "Loom\xAA",
                "Loom\x99",
                "Loom\xE2\x84\xA2",
                "Loom"
            };
    }
}
