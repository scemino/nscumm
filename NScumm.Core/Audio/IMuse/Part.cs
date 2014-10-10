//
//  Part.cs
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
using NScumm.Core.Audio.Midi;

namespace NScumm.Core
{
    class Part
    {
        int _slot;
        MidiChannel _mc;
        IPlayer _player;
        short _pitchbend;
        byte _pitchbend_factor;
        int _transpose, _transpose_eff;
        int _vol, _vol_eff;
        int _detune, _detune_eff;
        int _pan, _pan_eff;
        bool _on;
        byte _modwheel;
        bool _pedal;
        int priority;
        int priorityEffective;

        public int Channel{ get; private set; }

        int _effect_level;
        byte _chorus;
        bool _percussion;
        byte _bank;

        // New abstract instrument definition
        //Instrument _instrument;
        bool _unassigned_instrument;
        // For diagnostic reporting purposes only

        // MidiChannel interface
        // (We don't currently derive from MidiChannel,
        //  but if we ever do, this will make it easy.)
        //        void noteOff(byte note);
        //
        //        void noteOn(byte note, byte velocity);
        //
        //        void programChange(byte value);
        //
        //        void pitchBend(short value);
        //
        //        void modulationWheel(byte value);
        //
        //        void volume(byte value);
        //
        //        void pitchBendFactor(byte value);
        //
        //        void sustain(bool value);
        //
        //        void effectLevel(byte value);
        //
        //        void chorusLevel(byte value);
        //
        //        void allNotesOff();

        //        void set_param(byte param, int value)
        //        {
        //        }

        //        void init();
        //
        //        void setup(Player*player);
        //
        //        void uninit();
        //
        //        void off();
        //
        //        void set_instrument(uint b);
        //
        //        void set_instrument(byte*data);
        //
        //        void set_instrument_pcspk(byte*data);
        //
        //        void load_global_instrument(byte b);
        //
        //        void set_transpose(sbyte transpose);
        //
        public int Detune
        {
            get{ return _detune; }
            set
            {
                // Sam&Max does not have detune, so we just ignore this here. We still get
                // this called, since Sam&Max uses the same controller for a different
                // purpose.
//                if (_se->_game_id == GID_SAMNMAX) {
//                        #if 0
//                        if (_mc) {
//                        _mc->controlChange(17, detune + 0x40);
//                        }
//                        #endif
//                } else {
                _detune_eff = Clamp((_detune = value) + _player.Detune, -128, 127);
                SendPitchBend();
//                    }
            }
        }

        public int Priority
        {
            get { return priority; }
            set
            {
                priorityEffective = Clamp((priority = value) + _player.Priority, 0, 255);
                // TODO:
//                if (_mc != null)
//                    _mc.Priority = priorityEffective;
            }
        }
        //
        public int Pan
        {
            get{ return _pan; }
            set
            {
                _pan_eff = Clamp((_pan = value) + _player.Pan, -64, 63);
                SendPanPosition(_pan_eff + 0x40);
            }
        }

        void SendPanPosition(int value)
        {
            if (_mc == null)
                return;

            // As described in bug report #1088045 "MI2: Minor problems in native MT-32 mode"
            // the original iMuse MT-32 driver did revert the panning. So we do the same
            // here in our code to have correctly panned sound output.
            if (_player.IsNativeMT32)
                value = 127 - value;

            // TODO:
//            _mc.PanPosition = value;
        }

        //
        //        void set_onoff(bool on);
        //
        //        void fix_after_load();
        //
        //        void sendAll();
        //
        //        bool clearToTransmit();

        public Part(IPlayer player, int priority, int channel)
        {
            _player = player;

            Channel = channel;
            Priority = priority;
            _percussion = (player.IsMidi && Channel == 9); // true;
            _on = true;
            priorityEffective = player.Priority;
            _vol = 127;
            _vol_eff = player.EffectiveVolume;
            _pan = Clamp(player.Pan, -64, 63);
            _transpose_eff = player.Transpose;
            _transpose = 0;
            _detune = 0;
            _detune_eff = player.Detune;
            _pitchbend_factor = 2;
            _pitchbend = 0;
            _effect_level = player.IsNativeMT32 ? 127 : 64;
            _effect_level = 64;
            // TODO:
//            _instrument.clear();
            _unassigned_instrument = true;
            _chorus = 0;
            _modwheel = 0;
            _bank = 0;
            _pedal = false;
        }

        static int Clamp(int val, int min, int max)
        {
            if (val < min)
                return min;
            if (val > max)
                return max;
            return val;
        }


        void SendPitchBend()
        {
            if (_mc == null)
                return;

            int bend = _pitchbend;
            // RPN-based pitchbend range doesn't work for the MT32,
            // so we'll do the scaling ourselves.
            if (_player.IsNativeMT32)
                bend = bend * _pitchbend_factor / 12;
            // TODO:
//            _mc.PitchBend(Clamp(bend + (_detune_eff * 64 / 12) + (_transpose_eff * 8192 / 12), -8192, 8191));
        }
        //
        //        void sendPanPosition(byte value);
        //
        //        void sendEffectLevel(byte value);
    };
}

