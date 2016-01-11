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
using NScumm.Core.Audio;

namespace NScumm.Sci.Sound.Drivers
{
    abstract class MidiPlayer : MidiDriverBase
    {
        protected MidiDriver _driver;
        protected sbyte _reverb;
        protected SciVersion _version;

        public virtual uint BaseTempo { get { return _driver.BaseTempo; } }

        public abstract bool HasRhythmChannel
        {
            get;
        }

        public abstract byte PlayId { get; }
        public abstract int Polyphony { get; }
        public virtual int FirstChannel { get { return 0; } }
        public virtual int LastChannel { get { return 15; } }
        public virtual byte Volume
        {
            get
            {
                return _driver != null ? (byte)_driver.Property(MidiDriver_AdLib.MIDI_PROP_MASTER_VOLUME, 0xffff) : (byte)0;
            }
            set
            {
                if (_driver != null)
                    _driver.Property(MidiDriver_AdLib.MIDI_PROP_MASTER_VOLUME, value);
            }
        }
        public sbyte Reverb
        {
            get { return _reverb; }
            set { _reverb = value; }
        }

        protected MidiPlayer(SciVersion version)
        {
            _reverb = -1;
            _version = version;
        }

        public MidiDriverError Open()
        {
            ResourceManager resMan = SciEngine.Instance.ResMan;	// HACK
            return Open(resMan);
        }

        public virtual MidiDriverError Open(ResourceManager resMan)
        {
            return _driver.Open();
        }

        public virtual void Close() { _driver.Dispose(); }

        public override void Send(int b)
        {
            _driver.Send(b);
        }

        public virtual void SetTimerCallback(object timer_param, MidiDriver.TimerProc timer_proc)
        {
            _driver.SetTimerCallback(timer_param, timer_proc);
        }

        public virtual void PlaySwitch(bool play)
        {
            if (!play)
            {
                // Send "All Sound Off" on all channels
                for (int i = 0; i < MidiDriver_AdLib.MIDI_CHANNELS; ++i)
                    _driver.Send((byte)(0xb0 + i), MidiDriver_AdLib.SCI_MIDI_CHANNEL_NOTES_OFF, 0);
            }
        }
    }
}
