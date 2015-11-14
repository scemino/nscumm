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

using System;
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse
{
    class Part
    {
        public IMuseInternal Se { get; set; }

        public int Slot { get; set; }

        public MidiChannel MidiChannel { get; set; }

        public Player Player { get; private set; }

        short _pitchbend;
        byte _pitchbend_factor;
        int _transpose_eff;
        int _vol, _vol_eff;
        int _detune, _detune_eff;
        int _pan, _pan_eff;

        public Part Previous { get; set; }

        public Part Next { get; set; }

        public bool On { get; private set; }

        byte _modwheel;
        int priority;

        public bool Pedal { get; private set; }

        public int Transpose { get; private set; }

        public int PriorityEffective { get; set; }

        public int Channel { get; set; }

        int _effect_level;
        byte _chorus;

        public bool Percussion { get; set; }

        byte _bank;

        // New abstract instrument definition
        public Instrument Instrument { get; private set; }

        bool _unassignedInstrument;
        // For diagnostic reporting purposes only

        public Part()
        {
            Instrument = new Instrument();
        }

        // MidiChannel interface
        // (We don't currently derive from MidiChannel,
        //  but if we ever do, this will make it easy.)
        public void NoteOff(byte note)
        {
            if (!On)
                return;

            MidiChannel mc = MidiChannel;
            if (mc != null)
            {
                mc.NoteOff(note);
            }
            else if (Percussion)
            {
                mc = Player.MidiDriver.GetPercussionChannel();
                if (mc != null)
                    mc.NoteOff(note);
            }
        }

        static byte prev_vol_eff;

        internal void NoteOn(byte note, byte velocity)
        {
            if (!On)
                return;

            MidiChannel mc = MidiChannel;

            // DEBUG
            if (_unassignedInstrument && !Percussion)
            {
                _unassignedInstrument = false;
                if (!Instrument.IsValid)
                {
                    Debug.WriteLine("[{0}] No instrument specified", Channel);
                    return;
                }
            }

            if (mc != null && Instrument.IsValid)
            {
                mc.NoteOn(note, velocity);
            }
            else if (Percussion)
            {
                mc = Player.MidiDriver.GetPercussionChannel();
                if (mc == null)
                    return;

                // FIXME: The following is evil, EVIL!!! Either prev_vol_eff is
                // actually meant to be a member of the Part class (i.e. each
                // instance of Part keeps a separate copy of it); or it really
                // is supposed to be shared by all Part instances -- but then it
                // should be implemented as a class static var. As it is, using
                // a function level static var in most cases is arcane and evil.
                prev_vol_eff = 128;
                if (_vol_eff != prev_vol_eff)
                {
                    mc.Volume((byte)_vol_eff);
                    prev_vol_eff = (byte)_vol_eff;
                }
                if ((note < 35) && (!Player._se.IsNativeMT32))
                    note = Instrument._gmRhythmMap[note];

                mc.NoteOn(note, velocity);
            }
        }

        public void ProgramChange(byte value)
        {
            _bank = 0;
            Instrument.Program(value, Player.IsMT32);
            if (ClearToTransmit())
                Instrument.Send(MidiChannel);
        }

        public void PitchBend(short value)
        {
            _pitchbend = value;
            SendPitchBend();
        }

        public void ModulationWheel(byte value)
        {
            _modwheel = value;
            if (MidiChannel != null)
                MidiChannel.ModulationWheel(value);
        }

        public int Volume
        {
            get { return _vol; }
            set
            {
                _vol_eff = ((_vol = value) + 1) * Player.GetEffectiveVolume() >> 7;
                if (MidiChannel != null)
                    MidiChannel.Volume((byte)_vol_eff);
            }
        }

        public void PitchBendFactor(byte value)
        {
            if (value > 12)
                return;
            PitchBend(0);
            _pitchbend_factor = value;
            if (MidiChannel != null)
                MidiChannel.PitchBendFactor(value);
        }

        public void Sustain(bool value)
        {
            Pedal = value;
            if (MidiChannel != null)
                MidiChannel.Sustain(value);
        }

        public void EffectLevel(byte value)
        {
            _effect_level = value;
            SendEffectLevel(value);
        }

        public void ChorusLevel(byte value)
        {
            _chorus = value;
            if (MidiChannel != null)
                MidiChannel.ChorusLevel(value);
        }

        public void AllNotesOff()
        {
            if (MidiChannel == null)
                return;
            MidiChannel.AllNotesOff();
        }

        public void SetParam(byte param, int value)
        {
        }

        public void Init()
        {
            Player = null;
            Next = null;
            Previous = null;
            MidiChannel = null;
        }

        public void Uninit()
        {
            if (Player == null)
                return;
            Off();
            Player.RemovePart(this);
            Player = null;
        }

        public void Off()
        {
            if (MidiChannel != null)
            {
                MidiChannel.AllNotesOff();
                MidiChannel.Release();
                MidiChannel = null;
            }
        }

        public void SetInstrument(uint b)
        {
            _bank = (byte)(b >> 8);
//            if (_bank != 0)
//                Console.Error.WriteLine("Non-zero instrument bank selection. Please report this");
            // HACK: Horrible hack to allow tracing of program change source.
            // The Mac m68k versions of MI2 and Indy4 use a different program "bank"
            // when it gets program change events through the iMuse SysEx handler.
            // We emulate this by introducing a special instrument, which sets
            // the instrument via sysEx_customInstrument. This seems to be
            // exclusively used for special sound effects like the "spit" sound.
            // TODO: part
//            if (ScummEngine.IsMacM68kIMuse())
//            {
//                Instrument.macSfx(b);
//            }
//            else
            {
                Instrument.Program((byte)b, Player.IsMT32);
            }
            if (ClearToTransmit())
                Instrument.Send(MidiChannel);
        }

        public void SetInstrument(byte[] data)
        {
            if (Se.PcSpeaker)
                Instrument.PcSpk(data);
            else
                Instrument.Adlib(data);

            if (ClearToTransmit())
                Instrument.Send(MidiChannel);
        }

        public void LoadGlobalInstrument(byte slot)
        {
            Player._se.CopyGlobalInstrument(slot, Instrument);
            if (ClearToTransmit())
                Instrument.Send(MidiChannel);
        }

        public void SetTranspose(sbyte transpose)
        {
            Transpose = transpose;
            _transpose_eff = (Transpose == -128) ? 0 : Player.TransposeClamp(Transpose + Player.GetTranspose(), -24, 24);
            SendPitchBend();
        }

        public int Detune
        {
            get{ return _detune; }
            set
            {
                // Sam&Max does not have detune, so we just ignore this here. We still get
                // this called, since Sam&Max uses the same controller for a different
                // purpose.
                if (Se.GameId == Scumm.IO.GameId.SamNMax)
                {
                    #if false
                                        if (MidiChannel) {
                                        MidiChannel.controlChange(17, detune + 0x40);
                        }
                    #endif
                }
                else
                {
                    _detune_eff = Clamp((_detune = value) + Player.Detune, -128, 127);
                    SendPitchBend();
                }
            }
        }

        public int Priority
        {
            get { return priority; }
            set
            {
                PriorityEffective = Clamp((priority = value) + Player.Priority, 0, 255);
                if (MidiChannel != null)
                    MidiChannel.Priority((byte)PriorityEffective);
            }
        }

        public int Pan
        {
            get{ return _pan; }
            set
            {
                _pan_eff = Clamp((_pan = value) + Player.Pan, -64, 63);
                SendPanPosition(_pan_eff + 0x40);
            }
        }

        void SendPanPosition(int value)
        {
            if (MidiChannel == null)
                return;

            // As described in bug report #1088045 "MI2: Minor problems in native MT-32 mode"
            // the original iMuse MT-32 driver did revert the panning. So we do the same
            // here in our code to have correctly panned sound output.
            // TODO: part
//            if (Player.IsNativeMT32)
//                value = 127 - value;

            MidiChannel.PanPosition((byte)value);
        }

        public void SetOnOff(bool on)
        {
            if (On != on)
            {
                On = on;
                if (!on)
                    Off();
                if (!Percussion)
                    Player._se.ReallocateMidiChannels(Player.MidiDriver);
            }
        }

        public void FixAfterLoad()
        {
            SetTranspose((sbyte)Transpose);
            Volume = _vol;
            Detune = _detune;
            Priority = Priority;
            Pan = Pan;
            SendAll();
        }

        public void SendAll()
        {
            if (!ClearToTransmit())
                return;

            MidiChannel.PitchBendFactor(_pitchbend_factor);
            SendPitchBend();
            MidiChannel.Volume((byte)_vol_eff);
            MidiChannel.Sustain(Pedal);
            MidiChannel.ModulationWheel(_modwheel);
            SendPanPosition(_pan_eff + 0x40);

            if (Instrument.IsValid)
                Instrument.Send(MidiChannel);

            // We need to send the effect level after setting up the instrument
            // otherwise the reverb setting for MT-32 will be overwritten.
            SendEffectLevel((byte)_effect_level);

            MidiChannel.ChorusLevel(_chorus);
            MidiChannel.Priority((byte)PriorityEffective);
        }

        public bool ClearToTransmit()
        {
            if (MidiChannel != null)
                return true;
            if (Instrument.IsValid)
                Player._se.ReallocateMidiChannels(Player.MidiDriver);
            return false;
        }

        public void Setup(Player player)
        {
            Player = player;

            Percussion = (player.IsMIDI && Channel == 9); // true;
            On = true;
            PriorityEffective = player.Priority;
            Priority = 0;
            _vol = 127;
            _vol_eff = player.GetEffectiveVolume();
            _pan = Clamp(player.Pan, -64, 63);
            _transpose_eff = player.GetTranspose();
            Transpose = 0;
            _detune = 0;
            _detune_eff = player.Detune;
            _pitchbend_factor = 2;
            _pitchbend = 0;
            _effect_level = player._se.IsNativeMT32 ? 127 : 64;
            Instrument.Clear();
            _unassignedInstrument = true;
            _chorus = 0;
            _modwheel = 0;
            _bank = 0;
            Pedal = false;
            MidiChannel = null;
        }

        public void SaveOrLoad(Serializer ser)
        {
            var partEntries = new []
            {
                LoadAndSaveEntry.Create(r => _pitchbend = r.ReadInt16(), w => w.WriteInt16(_pitchbend), 8),
                LoadAndSaveEntry.Create(r => _pitchbend_factor = r.ReadByte(), w => w.WriteByte(_pitchbend_factor), 8),
                LoadAndSaveEntry.Create(r => Transpose = r.ReadSByte(), w => w.Write((sbyte)Transpose), 8),
                LoadAndSaveEntry.Create(r => _vol = r.ReadByte(), w => w.WriteByte(_vol), 8),
                LoadAndSaveEntry.Create(r => _detune = r.ReadSByte(), w => w.Write((sbyte)_detune), 8),
                LoadAndSaveEntry.Create(r => _pan = r.ReadSByte(), w => w.Write((sbyte)_pan), 8),
                LoadAndSaveEntry.Create(r => On = r.ReadBoolean(), w => w.Write(On), 8),
                LoadAndSaveEntry.Create(r => _modwheel = r.ReadByte(), w => w.WriteByte(_modwheel), 8),
                LoadAndSaveEntry.Create(r => Pedal = r.ReadBoolean(), w => w.Write(Pedal), 8),
                LoadAndSaveEntry.Create(r => r.ReadByte(), w => w.WriteByte(0), 8, 16),
                LoadAndSaveEntry.Create(r => priority = r.ReadByte(), w => w.WriteByte(priority), 8),
                LoadAndSaveEntry.Create(r => Channel = r.ReadByte(), w => w.WriteByte(Channel), 8),
                LoadAndSaveEntry.Create(r => _effect_level = r.ReadByte(), w => w.WriteByte(_effect_level), 8),
                LoadAndSaveEntry.Create(r => _chorus = r.ReadByte(), w => w.WriteByte(_chorus), 8),
                LoadAndSaveEntry.Create(r => Percussion = r.ReadBoolean(), w => w.Write(Percussion), 8),
                LoadAndSaveEntry.Create(r => _bank = r.ReadByte(), w => w.WriteByte(_bank), 8)
            };

            int num;
            if (!ser.IsLoading)
            {
                num = Next != null ? Array.IndexOf(Se._parts, Next) + 1 : 0;
                ser.Writer.WriteUInt16(num);

                num = Previous != null ? Array.IndexOf(Se._parts, Previous) + 1 : 0;
                ser.Writer.WriteUInt16(num);

                num = Player != null ? Array.IndexOf(Se._players, Player) + 1 : 0;
                ser.Writer.WriteUInt16(num);
            }
            else
            {
                num = ser.Reader.ReadUInt16();
                Next = num != 0 ? Se._parts[num - 1] : null;

                num = ser.Reader.ReadUInt16();
                Previous = num != 0 ? Se._parts[num - 1] : null;

                num = ser.Reader.ReadUInt16();
                Player = num != 0 ? Se._players[num - 1] : null;
            }
            partEntries.ForEach(e => e.Execute(ser));
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
            if (MidiChannel == null)
                return;

            var bend = _pitchbend;
            // RPN-based pitchbend range doesn't work for the MT32,
            // so we'll do the scaling ourselves.
            if (Player._se.IsNativeMT32)
                bend = (short)(bend * _pitchbend_factor / 12);
            MidiChannel.PitchBend((short)Clamp(bend + (_detune_eff * 64 / 12) + (_transpose_eff * 8192 / 12), -8192, 8191));
        }

        void SendEffectLevel(byte value)
        {
            if (MidiChannel == null)
                return;

            // As described in bug report #1088045 "MI2: Minor problems in native MT-32 mode"
            // for the MT-32 one has to use a sysEx event to change the effect level (rather
            // the reverb setting).
            if (Player._se.IsNativeMT32)
            {
                if (value != 127 && value != 0)
                {
//                    Console.Error.WriteLine("Trying to use unsupported effect level value {0} in native MT-32 mode.", value);

                    if (value >= 64)
                        value = 127;
                    else
                        value = 0;
                }

                var message = new byte[]{ 0x41, 0x00, 0x16, 0x12, 0x00, 0x00, 0x06, 0x00, 0x00 };
                message[1] = MidiChannel.Number;
                message[7] = (byte)((value == 127) ? 1 : 0);
                message[8] = (byte)(128 - (6 + message[7]));
                Player.MidiDriver.SysEx(message, 9);
            }
            else
            {
                MidiChannel.EffectLevel(value);
            }
        }
    }
}

