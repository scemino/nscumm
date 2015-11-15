//
//  TownsAudioInterface.cs
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
using System;

namespace NScumm.Core.Audio.SoftSynth
{
    public class TownsAudioInterface: IDisposable
    {
        public TownsAudioInterface(IMixer mixer, ITownsAudioInterfacePluginDriver driver, bool externalMutexHandling = false)
        {
            _intf = TownsAudioInterfaceInternal.AddNewRef(mixer, this, driver, externalMutexHandling);
        }

        public void Dispose()
        {
            TownsAudioInterfaceInternal.ReleaseRef(this);
            _intf = null;
        }

        public void SetMusicVolume(int volume)
        {
            _intf.SetMusicVolume(volume);
        }

        public bool Init()
        {
            return _intf.Init();
        }

        public void SetSoundEffectVolume(int volume)
        {
            _intf.SetSoundEffectVolume(volume);
        }

        public bool Callback(int command, params object[] args)
        {
            int res = _intf.ProcessCommand(command, args);
            return res != 0;
        }

        public void SetSoundEffectChanMask(int mask)
        {
            _intf.SetSoundEffectChanMask(mask);
        }

        TownsAudioInterfaceInternal _intf;
    }
}

