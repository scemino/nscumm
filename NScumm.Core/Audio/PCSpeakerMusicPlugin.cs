//
//  PCSpeakerMusicPlugin.cs
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

using System.Collections.Generic;
using NScumm.Core.Audio.Midi;

namespace NScumm.Core.Audio.SoftSynth
{
    class PCSpeakerMusicPlugin : MusicPluginObject, IMusicPluginObject
    {
        public override string Id
        {
            get{ return "pcspk"; }
        }

        public override string Name
        {
            get { return "PC Speaker Emulator"; }
        }

        public override IList<MusicDevice> GetDevices()
        {
            return new []{ new MusicDevice(this, string.Empty, MusicType.PCSpeaker) };
        }

        public override IMidiDriver CreateInstance(IMixer mixer, DeviceHandle handle)
        {
            return new PCSpeakerDriver(mixer);
        }
    }
}
