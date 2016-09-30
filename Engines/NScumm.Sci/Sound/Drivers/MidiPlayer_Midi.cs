//
//  MidiPlayer_Midi.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System.Collections.Generic;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound.Drivers
{
    internal class MidiPlayer_Midi : MidiPlayer
    {
        private const int MIDI_RHYTHM_CHANNEL = 9;

        private const int Voices = 32;
        private const int ReverbConfigNr = 11;
        private const int MaxSysExSize = 264;

        // Patch not mapped
        private const int MIDI_UNMAPPED = 0xff;
        // Patch mapped to rhythm key
        private const int MIDI_MAPPED_TO_RHYTHM = 0xfe;

        public override bool HasRhythmChannel
        {
            get
            {
                return true;
            }
        }

        public override int Polyphony
        {
            get
            {
                if (SciEngine.Instance != null && SciEngine.Instance._features.UseAltWinGMSound)
                    return 16;
                return Voices;
            }
        }

        public override byte PlayId
        {
            get
            {
                switch (_version)
                {
                    case SciVersion.V0_EARLY:
                    case SciVersion.V0_LATE:
                        return 0x01;
                    default:
                        if (_isMt32)
                            return 0x0c;
                        else
                            return (byte)(_useMT32Track ? 0x0c : 0x07);
                }
            }
        }

        // We return 1 for mt32, because if we remap channels to 0 for mt32, those won't get played at all
        // NOTE: SSCI uses channels 1 through 8 for General MIDI as well, in the drivers I checked
        public override int FirstChannel
        {
            get
            {
                if (_isMt32)
                    return 1;
                return 0;
            }
        }

        public override int LastChannel
        {
            get
            {
                if (_isMt32)
                    return 8;
                return 15;
            }
        }

        public override byte Volume
        {
            get
            {
                return (byte)_masterVolume;
            }
            set
            {
                _masterVolume = value;

                if (!_playSwitch)
                    return;

                for (int i = 1; i < 10; i++)
                {
                    if (_channels[i].volume != 0xff)
                        ControlChange(i, 0x07, _channels[i].volume & 0x7f);
                }
            }
        }

        public override sbyte Reverb
        {
            get { return _reverb; }
            set
            {
                System.Diagnostics.Debug.Assert(value < ReverbConfigNr);

                if (_hasReverb && (_reverb != value))
                    SendMt32SysEx(0x100001, _reverbConfig[value], 3, true);

                _reverb = value;
            }
        }

        private class Channel
        {
            public byte mappedPatch;
            public byte patch;
            public int velocityMapIdx;
            public bool playing;
            public sbyte keyShift;
            public sbyte volAdjust;
            public byte pan;
            public byte hold;
            public byte volume;

            public Channel()
            {
                mappedPatch = MIDI_UNMAPPED;
                patch = MIDI_UNMAPPED;
                pan = 0x40;
                volume = 0x7f;
            }
        }

        private bool _isMt32;
        private bool _useMT32Track;
        private bool _hasReverb;
        private bool _playSwitch;
        private int _masterVolume;

        private byte[][] _reverbConfig = new byte[ReverbConfigNr][];
        private Channel[] _channels = new Channel[16];
        private byte[] _percussionMap = new byte[128];
        private byte[] _keyShift = new byte[128];
        private byte[] _volAdjust = new byte[128];
        private byte[] _patchMap = new byte[128];
        private byte[] _velocityMapIdx = new byte[128];
        private byte[][] _velocityMap = new byte[4][];

        // These are extensions used for our own MT-32 to GM mapping
        private byte[] _pitchBendRange = new byte[128];
        private byte[] _percussionVelocityScale = new byte[128];

        private byte[] _goodbyeMsg = new byte[20];
        private byte[] _sysExBuf = new byte[MaxSysExSize];
        private List<Mt32ToGmMap> Mt32dynamicMappings = new List<Mt32ToGmMap>();

        public MidiPlayer_Midi(SciVersion version)
            : base(version)
        {
            _playSwitch = true;
            _masterVolume = 15;
            _useMT32Track = true;
            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = new Channel();
            }
            for (int i = 0; i < 4; i++)
            {
                _velocityMap[i] = new byte[128];
            }

            DeviceHandle dev = MidiDriver.DetectDevice(MusicDriverTypes.Midi, SciEngine.Instance.Settings.AudioDevice);
            _driver = (MidiDriver)MidiDriver.CreateMidi(SciEngine.Instance.Mixer, dev);

            if (MidiDriver.GetMusicType(dev) == MusicType.MT32 || ConfigManager.Instance.Get<bool>("native_mt32"))
                _isMt32 = true;

            _sysExBuf[0] = 0x41;
            _sysExBuf[1] = 0x10;
            _sysExBuf[2] = 0x16;
            _sysExBuf[3] = 0x12;

            for (int i = 0; i < ReverbConfigNr; i++)
            {
                _reverbConfig[i] = new byte[3];
            }

            // TODO: Mt32dynamicMappings = new Mt32ToGmMapList();
        }

        public override void Send(int b)
        {
            byte command = (byte)(b & 0xf0);
            byte channel = (byte)(b & 0xf);
            byte op1 = (byte)((b >> 8) & 0x7f);
            byte op2 = (byte)((b >> 16) & 0x7f);

            // In early SCI0, we may also get events for AdLib rhythm channels.
            // While an MT-32 would ignore those with the default channel mapping,
            // we filter these out for the benefit of other MIDI devices.
            if (_version == SciVersion.V0_EARLY)
            {
                if (channel < 1 || channel > 9)
                    return;
            }

            switch (command)
            {
                case 0x80:
                    NoteOn(channel, op1, 0);
                    break;
                case 0x90:
                    NoteOn(channel, op1, op2);
                    break;
                case 0xb0:
                    ControlChange(channel, op1, op2);
                    break;
                case 0xc0:
                    SetPatch(channel, op1);
                    break;
                // The original MIDI driver from sierra ignores aftertouch completely, so should we
                case 0xa0: // Polyphonic key pressure (aftertouch)
                case 0xd0: // Channel pressure (aftertouch)
                    break;
                case 0xe0:
                    _driver.Send(b);
                    break;
                default:
                    Warning("Ignoring MIDI event {0:X2}", command);
                    break;
            }
        }

        public override MidiDriverError Open(ResourceManager resMan)
        {
            System.Diagnostics.Debug.Assert(resMan != null);

            var retval = _driver.Open();
            if (retval != 0)
            {
                Warning("Failed to open MIDI driver");
                return retval;
            }

            // By default use no mapping
            for (int i = 0; i < 128; i++)
            {
                _percussionMap[i] = (byte)i;
                _patchMap[i] = (byte)i;
                _velocityMap[0][i] = (byte)i;
                _velocityMap[1][i] = (byte)i;
                _velocityMap[2][i] = (byte)i;
                _velocityMap[3][i] = (byte)i;
                _keyShift[i] = 0;
                _volAdjust[i] = 0;
                _velocityMapIdx[i] = 0;
                _pitchBendRange[i] = MIDI_UNMAPPED;
                _percussionVelocityScale[i] = 127;
            }

            ResourceManager.ResourceSource.Resource res = null;

            if (SciEngine.Instance != null && SciEngine.Instance._features.UseAltWinGMSound)
            {
                res = resMan.FindResource(new ResourceId(ResourceType.Patch, 4), false);
                if (!(res != null && IsMt32GmPatch(res.data, res.size)))
                {
                    // Don't do any mapping when a Windows alternative track is selected
                    // and no MIDI patch is available
                    _useMT32Track = false;
                    return 0;
                }
            }

            if (_isMt32)
            {
                // MT-32
                ResetMt32();

                res = resMan.FindResource(new ResourceId(ResourceType.Patch, 1), false);

                if (res != null)
                {
                    if (IsMt32GmPatch(res.data, res.size))
                    {
                        ReadMt32GmPatch(res.data, res.size);
                        // Note that _goodbyeMsg is not zero-terminated
                        _goodbyeMsg = ScummHelper.GetBytes("      ScummVM       ");
                    }
                    else {
                        ReadMt32Patch(res.data, res.size);
                    }
                }
                else {
                    // Early SCI0 games have the sound bank embedded in the MT-32 driver
                    ReadMt32DrvData();
                }
            }
            else {
                // General MIDI
                res = resMan.FindResource(new ResourceId(ResourceType.Patch, 4), false);

                if (res != null && IsMt32GmPatch(res.data, res.size))
                {
                    // There is a GM patch
                    ReadMt32GmPatch(res.data, res.size);

                    if (SciEngine.Instance != null && SciEngine.Instance._features.UseAltWinGMSound)
                    {
                        // Always use the GM track if an alternative GM Windows soundtrack is selected
                        _useMT32Track = false;
                    }
                    else {
                        // Detect the format of patch 1, so that we know what play mask to use
                        res = resMan.FindResource(new ResourceId(ResourceType.Patch, 1), false);
                        if (res == null)
                            _useMT32Track = false;
                        else
                            _useMT32Track = !IsMt32GmPatch(res.data, res.size);

                        // Check if the songs themselves have a GM track
                        if (!_useMT32Track)
                        {
                            if (!resMan.IsGMTrackIncluded())
                                _useMT32Track = true;
                        }
                    }
                }
                else {
                    // No GM patch found, map instruments using MT-32 patch

                    Warning("Game has no native support for General MIDI, applying auto-mapping");

                    // TODO: The MT-32 <. GM mapping hasn't been worked on for SCI1 games. Throw
                    // a warning to the user
                    if (ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY)
                        Warning("The automatic mapping for General MIDI hasn't been worked on for " +
                                "SCI1 games. Music might sound wrong or broken. Please choose another " +
                                "music driver for this game (e.g. AdLib or MT-32) if you are " +
                                "experiencing issues with music");

                    // Modify velocity map to make low velocity notes a little louder
                    for (int i = 1; i < 0x40; i++)
                    {
                        _velocityMap[0][i] = (byte)(0x20 + (i - 1) / 2);
                        _velocityMap[1][i] = (byte)(0x20 + (i - 1) / 2);
                        _velocityMap[2][i] = (byte)(0x20 + (i - 1) / 2);
                        _velocityMap[3][i] = (byte)(0x20 + (i - 1) / 2);
                    }

                    res = resMan.FindResource(new ResourceId(ResourceType.Patch, 1), false);

                    if (res != null)
                    {
                        if (!IsMt32GmPatch(res.data, res.size))
                        {
                            MapMt32ToGm(res.data, res.size);
                        }
                        else {
                            if (ResourceManager.GetSciVersion() < SciVersion.V3)
                            {
                                Error("MT-32 patch has wrong type");
                            }
                            else {
                                // Happens in the SCI3 interactive demo of Lighthouse
                                Warning("TODO: Ignoring new SCI3 type of MT-32 patch for now (size = %d)", res.size);
                            }
                        }
                    }
                    else {
                        // Early SCI0 games have the sound bank embedded in the MT-32 driver

                        // No MT-32 patch present, try to read from MT32.DRV
                        var f = Core.Engine.OpenFileRead("MT32.DRV");
                        if (f != null)
                        {
                            int size = (int)f.Length;

                            System.Diagnostics.Debug.Assert(size >= 70);

                            f.Seek(0x29, SeekOrigin.Begin);

                            // Read AdLib.MT-32 patch map
                            for (int i = 0; i < 48; i++)
                                _patchMap[i] = GetGmInstrument(Mt32PresetTimbreMaps[f.ReadByte() & 0x7f]);
                        }
                    }
                }
            }

            return 0;
        }

        public override void PlaySwitch(bool play)
        {
            _playSwitch = play;
            if (play)
                Volume = (byte)_masterVolume;
            else {
                for (int i = 1; i < 10; i++)
                    _driver.Send((byte)(0xb0 | i), 7, 0);
            }
        }

        public override void Close()
        {
            if (_isMt32)
            {
                // Send goodbye message
                SendMt32SysEx(0x200000, _goodbyeMsg, 20);
            }

            _driver.Dispose();
        }

        public override void SysEx(BytePtr msg, ushort length)
        {
            _driver.SysEx(msg, length);

            // Wait the time it takes to send the SysEx data
            int delay = (length + 2) * 1000 / 3125;

            // Plus an additional delay for the MT-32 rev00
            if (_isMt32)
                delay += 40;

            ServiceLocator.Platform.Sleep(delay);
            SciEngine.Instance.System.GraphicsManager.UpdateScreen();
        }

        private void ReadMt32DrvData()
        {
            Stream f = Core.Engine.OpenFileRead("MT32.DRV");

            if (f != null)
            {
                using (var br = new BinaryReader(f))
                {
                    var size = f.Length;

                    // Skip before-SysEx text
                    if (size == 1773 || size == 1759 || size == 1747)   // XMAS88 / KQ4 early (0.000.253 / 0.000.274)
                        f.Seek(0x59, SeekOrigin.Begin);
                    else if (size == 2771)              // LSL2 early
                        f.Seek(0x29, SeekOrigin.Begin);
                    else
                        Error("Unknown MT32.DRV size ({0})", size);

                    // Skip 2 extra 0 bytes in some drivers
                    if (br.ReadUInt16() != 0)
                        f.Seek(-2, SeekOrigin.Current);

                    // Send before-SysEx text
                    SendMt32SysEx(0x200000, f, 20);

                    if (size != 2271)
                    {
                        // Send after-SysEx text (SSCI sends this before every song).
                        // There aren't any SysEx calls in old drivers, so this can
                        // be sent right after the before-SysEx text.
                        SendMt32SysEx(0x200000, f, 20);
                    }
                    else {
                        // Skip the after-SysEx text in the newer patch version, we'll send
                        // it after the SysEx messages are sent.
                        f.Seek(20, SeekOrigin.Current);
                    }

                    // Save goodbye message. This isn't a C string, so it may not be
                    // nul-terminated.
                    f.Read(_goodbyeMsg, 0, 20);

                    // Set volume
                    byte volume = (byte)ScummHelper.Clip(br.ReadUInt16(), 0, 100);
                    SetMt32Volume(volume);

                    if (size == 2771)
                    {
                        // MT32.DRV in LSL2 early contains more data, like a normal patch
                        byte reverb = br.ReadByte();

                        _hasReverb = true;

                        // Skip reverb SysEx message
                        f.Seek(11, SeekOrigin.Current);

                        // Read reverb data (stored vertically - patch #3117434)
                        for (int j = 0; j < 3; ++j)
                        {
                            for (int i = 0; i < ReverbConfigNr; i++)
                            {
                                _reverbConfig[i][j] = br.ReadByte();
                            }
                        }

                        f.Seek(2235, SeekOrigin.Current);   // skip driver code

                        // Patches 1-48
                        SendMt32SysEx(0x50000, f, 256);
                        SendMt32SysEx(0x50200, f, 128);

                        Reverb = (sbyte)reverb;

                        // Send the after-SysEx text
                        f.Seek(0x3d, SeekOrigin.Begin);
                        SendMt32SysEx(0x200000, f, 20);
                    }
                    else {
                        byte[] reverbSysEx = new byte[13];
                        // This old driver should have a full reverb SysEx
                        if ((f.Read(reverbSysEx, 0, 13) != 13) || (reverbSysEx[0] != 0xf0) || (reverbSysEx[12] != 0xf7))
                            Error("Error reading MT32.DRV");

                        // Send reverb SysEx
                        SysEx(new BytePtr(reverbSysEx, 1), 11);
                        _hasReverb = false;

                        f.Seek(0x29, SeekOrigin.Begin);

                        // Read AdLib.MT-32 patch map
                        for (int i = 0; i < 48; i++)
                        {
                            _patchMap[i] = br.ReadByte();
                        }
                    }

                }
            }
            else {
                Error("Failed to open MT32.DRV");
            }
        }

        private void ReadMt32Patch(byte[] data, int size)
        {
            // MT-32 patch contents:
            // - 20 bytes unkown
            // - 20 bytes before-SysEx message
            // - 20 bytes goodbye SysEx message
            // - 2 bytes volume
            // - 1 byte reverb
            // - 11 bytes reverb Sysex message
            // - 3 * 11 reverb data
            // - 256 + 128 bytes patches 1-48
            // -. total: 491 bytes
            // - 1 byte number of timbres (64 max)
            // - 246 * timbres timbre data
            // - 2 bytes flag (0xabcd)
            // - 256 + 128 bytes patches 49-96
            // - 2 bytes flag (0xdcba)
            // - 256 bytes rhythm key map
            // - 9 bytes partial reserve

            var str = new MemoryStream(data, 0, size);
            var br = new BinaryReader(str);

            // Send before-SysEx text
            str.Seek(20, SeekOrigin.Begin);
            SendMt32SysEx(0x200000, str, 20);

            // Save goodbye message
            str.Read(_goodbyeMsg, 0, 20);

            byte volume = (byte)ScummHelper.Clip(br.ReadUInt16(), 0, 100);
            SetMt32Volume(volume);

            // Reverb default only used in (roughly) SCI0/SCI01
            byte reverb = br.ReadByte();

            _hasReverb = true;

            // Skip reverb SysEx message
            str.Seek(11, SeekOrigin.Current);

            // Read reverb data (stored vertically - patch #3117434)
            for (int j = 0; j < 3; ++j)
            {
                for (int i = 0; i < ReverbConfigNr; i++)
                {
                    _reverbConfig[i][j] = br.ReadByte();
                }
            }

            // Patches 1-48
            SendMt32SysEx(0x50000, str, 256);
            SendMt32SysEx(0x50200, str, 128);

            // Timbres
            byte timbresNr = br.ReadByte();
            for (int i = 0; i < timbresNr; i++)
                SendMt32SysEx((uint)(0x80000 + (i << 9)), str, 246);

            ushort flag = br.ReadUInt16BigEndian();

            if (str.Position < str.Length && (flag == 0xabcd))
            {
                // Patches 49-96
                SendMt32SysEx(0x50300, str, 256);
                SendMt32SysEx(0x50500, str, 128);
                flag = br.ReadUInt16BigEndian();
            }

            if (str.Position < str.Length && (flag == 0xdcba))
            {
                // Rhythm key map
                SendMt32SysEx(0x30110, str, 256);
                // Partial reserve
                SendMt32SysEx(0x100004, str, 9);
            }

            // Reverb for SCI0
            if (_version <= SciVersion.V0_LATE)
                Reverb = (sbyte)reverb;

            // Send after-SysEx text
            str.Seek(0, SeekOrigin.Begin);
            SendMt32SysEx(0x200000, str, 20);

            // Send the mystery SysEx
            SendMt32SysEx(0x52000a, new byte[] { 16, 16, 16, 16, 16, 16 }, 6);
        }

        private void MapMt32ToGm(byte[] data, int size)
        {
            // FIXME: Clean this up
            int memtimbres, patches;
            byte group, number, keyshift, /*finetune,*/ bender_range;
            BytePtr patchpointer;
            int pos;
            int i;

            for (i = 0; i < 128; i++)
            {
                _patchMap[i] = GetGmInstrument(Mt32PresetTimbreMaps[i]);
                _pitchBendRange[i] = 12;
            }

            for (i = 0; i < 128; i++)
                _percussionMap[i] = Mt32PresetRhythmKeymap[i];

            memtimbres = data[0x1eb];
            pos = 0x1ec + memtimbres * 0xf6;

            if (size > pos && ((0x100 * data[pos] + data[pos + 1]) == 0xabcd))
            {
                patches = 96;
                pos += 2 + 8 * 48;
            }
            else {
                patches = 48;
            }

            DebugC(DebugLevels.Sound, "[MT32-to-GM] %d MT-32 Patches detected", patches);
            DebugC(DebugLevels.Sound, "[MT32-to-GM] %d MT-32 Memory Timbres", memtimbres);

            DebugC(DebugLevels.Sound, "\n[MT32-to-GM] Mapping patches..");

            for (i = 0; i < patches; i++)
            {
                byte[] name = new byte[11];

                if (i < 48)
                    patchpointer = new BytePtr(data, 0x6b + 8 * i);
                else
                    patchpointer = new BytePtr(data, 0x1ec + 8 * (i - 48) + memtimbres * 0xf6 + 2);

                group = patchpointer[0];
                number = patchpointer[1];
                keyshift = patchpointer[2];
                //finetune = *(patchpointer + 3);
                bender_range = patchpointer[4];

                //debugCN(kDebugLevelSound, "  [%03d] ", i);

                switch (group)
                {
                    case 1:
                    // Fall through
                    case 0:
                        if (group == 1)
                        {
                            number += 64;
                        }
                        _patchMap[i] = GetGmInstrument(Mt32PresetTimbreMaps[number]);
                        //debugCN(kDebugLevelSound, "%s . ", Mt32PresetTimbreMaps[number].name);
                        break;
                    case 2:
                        if (number < memtimbres)
                        {
                            Array.Copy(data, 0x1ec + number * 0xf6, name, 0, 10);
                            _patchMap[i] = LookupGmInstrument(name);
                            //debugCN(kDebugLevelSound, "%s . ", name);
                        }
                        else {
                            _patchMap[i] = 0xff;
                            //debugCN(kDebugLevelSound, "[Invalid]  . ");
                        }
                        break;
                    case 3:
                        _patchMap[i] = GetGmInstrument(Mt32RhythmTimbreMaps[number]);
                        //debugCN(kDebugLevelSound, "%s . ", Mt32RhythmTimbreMaps[number].name);
                        break;
                }

                if (_patchMap[i] == MIDI_UNMAPPED)
                {
                    DebugC(DebugLevels.Sound, "[Unmapped]");
                }
                else {
#if !REDUCE_MEMORY_USAGE
                    if (_patchMap[i] >= 128)
                    {
                        DebugC(DebugLevels.Sound, "{0} [Rhythm]", GmPercussionNames[_patchMap[i] - 128]);
                    }
                    else {
                        DebugC(DebugLevels.Sound, "{0}", GmInstrumentNames[_patchMap[i]]);
                    }
#endif
                }

                _keyShift[i] = (byte)(ScummHelper.Clip(keyshift, 0, 48) - 24);
                _pitchBendRange[i] = (byte)ScummHelper.Clip(bender_range, 0, 24);
            }

            if (size > pos && ((0x100 * data[pos] + data[pos + 1]) == 0xdcba))
            {
                DebugC(DebugLevels.Sound, "\n[MT32-to-GM] Mapping percussion..");

                for (i = 0; i < 64; i++)
                {
                    number = data[pos + 4 * i + 2];
                    byte ins = (byte)(i + 24);

                    //debugCN(kDebugLevelSound, "  [%03d] ", ins);

                    if (number < 64)
                    {
                        byte[] name = new byte[11];
                        Array.Copy(data, 0x1ec + number * 0xf6, name, 0, 10);
                        //debugCN(kDebugLevelSound, "%s . ", name);
                        _percussionMap[ins] = LookupGmRhythmKey(name);
                    }
                    else {
                        if (number < 94)
                        {
                            //debugCN(kDebugLevelSound, "%s . ", Mt32RhythmTimbreMaps[number - 64].name);
                            _percussionMap[ins] = Mt32RhythmTimbreMaps[number - 64].gmRhythmKey;
                        }
                        else {
                            //debugCN(kDebugLevelSound, "[Key  %03i] . ", number);
                            _percussionMap[ins] = MIDI_UNMAPPED;
                        }
                    }

#if !REDUCE_MEMORY_USAGE
                    if (_percussionMap[ins] == MIDI_UNMAPPED)
                        DebugC(DebugLevels.Sound, "[Unmapped]");
                    else
                        DebugC(DebugLevels.Sound, "{0}", GmPercussionNames[_percussionMap[ins]]);
#endif

                    _percussionVelocityScale[ins] = (byte)(data[+pos + 4 * i + 3] * 127 / 100);
                }
            }

        }

        private byte LookupGmInstrument(BytePtr iname)
        {
            int i = 0;
            var name = iname.GetRawText(0, 10);

            if (Mt32dynamicMappings != null)
            {
                foreach (var it in Mt32dynamicMappings)
                {
                    if (string.Equals(name, it.name, StringComparison.OrdinalIgnoreCase))
                        return GetGmInstrument(it);
                }
            }

            while (Mt32MemoryTimbreMaps[i].name != null)
            {
                if (string.Equals(name, Mt32MemoryTimbreMaps[i].name, StringComparison.OrdinalIgnoreCase))
                    return GetGmInstrument(Mt32MemoryTimbreMaps[i]);
                i++;
            }

            return MIDI_UNMAPPED;
        }

        private byte LookupGmRhythmKey(BytePtr iname)
        {
            int i = 0;
            var name = iname.GetRawText(0, 10);
            if (Mt32dynamicMappings != null)
            {
                foreach (var it in Mt32dynamicMappings)
                {
                    if (string.Equals(name, it.name, StringComparison.OrdinalIgnoreCase))
                        return it.gmRhythmKey;
                }
            }

            while (Mt32MemoryTimbreMaps[i].name != null)
            {
                if (string.Equals(name, Mt32MemoryTimbreMaps[i].name, StringComparison.OrdinalIgnoreCase))
                    return Mt32MemoryTimbreMaps[i].gmRhythmKey;
                i++;
            }

            return MIDI_UNMAPPED;
        }

        private byte GetGmInstrument(Mt32ToGmMap Mt32Ins)
        {
            if (Mt32Ins.gmInstr == MIDI_MAPPED_TO_RHYTHM)
                return (byte)(Mt32Ins.gmRhythmKey + 0x80);
            return Mt32Ins.gmInstr;
        }

        private void ReadMt32GmPatch(byte[] data, int size)
        {
            // GM patch contents:
            // - 128 bytes patch map
            // - 128 bytes key shift
            // - 128 bytes volume adjustment
            // - 128 bytes percussion map
            // - 1 byte volume adjust for the rhythm channel
            // - 128 bytes velocity map IDs
            // - 512 bytes velocity map
            // -. total: 1153 bytes

            Array.Copy(data, _patchMap, 128);
            Array.Copy(data, 128, _keyShift, 0, 128);
            Array.Copy(data, 256, _volAdjust, 0, 128);
            Array.Copy(data, 384, _percussionMap, 0, 128);
            _channels[MIDI_RHYTHM_CHANNEL].volAdjust = (sbyte)data[512];
            Array.Copy(data, 513, _velocityMapIdx, 0, 128);
            for (int i = 0; i < 4; i++)
            {
                Array.Copy(data, 641 + 128 * i, _velocityMap[i], 0, 128);
            }

            ushort midiSize = data.ToUInt16(1153);

            if (midiSize > 0)
            {
                if (size < midiSize + 1155)
                    Error("Failed to read MIDI data");

                int midi = 1155;
                byte command = 0;
                int i = 0;

                while (i < midiSize)
                {
                    byte op1, op2;

                    if ((data[midi + i] & 0x80) != 0)
                        command = data[midi + i++];

                    switch (command & 0xf0)
                    {
                        case 0xf0:
                            {
                                var sysExEndIndex = Array.IndexOf(data, (byte)0xf7, midi + i, midiSize - i);

                                if (sysExEndIndex == -1)
                                    Error("Failed to find end of sysEx");

                                int len = sysExEndIndex - (midi + i);
                                SysEx(new BytePtr(data, midi + i), (ushort)len);

                                i += len + 1; // One more for the 0x7f
                                break;
                            }
                        case 0x80:
                        case 0x90:
                        case 0xa0:
                        case 0xb0:
                        case 0xe0:
                            if (i + 1 >= midiSize)
                                Error("MIDI command exceeds data size");

                            op1 = data[midi + i++];
                            op2 = data[midi + i++];
                            _driver.Send(command, op1, op2);
                            break;
                        case 0xc0:
                        case 0xd0:
                            if (i >= midiSize)
                                Error("MIDI command exceeds data size");

                            op1 = data[midi + i++];
                            _driver.Send(command, op1, 0);
                            break;
                        default:
                            Error("Failed to find MIDI command byte");
                            break;
                    }
                }
            }
        }

        private void ResetMt32()
        {
            SendMt32SysEx(0x7f0000, new byte[] { 1, 0 }, 2, true);

            // This seems to require a longer delay than usual
            ServiceLocator.Platform.Sleep(150);
        }

        private void SetMt32Volume(byte volume)
        {
            SendMt32SysEx(0x100016, new byte[] { volume }, 1);
        }

        private bool IsMt32GmPatch(byte[] data, int size)
        {
            // WORKAROUND: Some Mac games (e.g. LSL5) may have an extra byte at the
            // end, so compensate for that here - bug #6725.
            if (size == 16890)
                size--;

            // Need at least 1153 + 2 bytes for a GM patch. Check readMt32GmPatch()
            // below for more info.
            if (size < 1153 + 2)
                return false;
            // The maximum number of bytes for an MT-32 patch is 16889. The maximum
            // number of timbres is 64, which leads us to:
            // 491 + 1 + 64 * 246 + 653 = 16889
            if (size > 16889)
                return true;

            bool isMt32 = false;
            bool isMt32Gm = false;

            // First, check for a GM patch. The presence of MIDI data after the
            // initial 1153 + 2 bytes indicates a GM patch
            if (data.ToUInt16(1153) + 1155 == size)
                isMt32Gm = true;

            // Now check for a regular MT-32 patch. Check readMt32Patch() below for
            // more info.
            // 491 = 20 + 20 + 20 + 2 + 1 + 11 + 3 * 11 + 256 + 128
            byte timbresNr = data[491];
            int pos = 492 + 246 * timbresNr;

            // Patches 49-96
            if ((size >= (pos + 386)) && (data.ToUInt16BigEndian(pos) == 0xabcd))
                pos += 386; // 256 + 128 + 2

            // Rhythm key map + partial reserve
            if ((size >= (pos + 267)) && (data.ToUInt16BigEndian(pos) == 0xdcba))
                pos += 267; // 256 + 9 + 2

            if (size == pos)
                isMt32 = true;

            if (isMt32 == isMt32Gm)
                Error("Failed to detect MT-32 patch format");

            return isMt32Gm;
        }

        private void NoteOn(int channel, int note, int velocity)
        {
            byte patch = _channels[channel].mappedPatch;

            System.Diagnostics.Debug.Assert(channel <= 15);
            System.Diagnostics.Debug.Assert(note <= 127);
            System.Diagnostics.Debug.Assert(velocity <= 127);

            if (channel == MIDI_RHYTHM_CHANNEL)
            {
                if (_percussionMap[note] == MIDI_UNMAPPED)
                {
                    DebugC(DebugLevels.Sound, "[Midi] Percussion instrument {0} is unmapped", note);
                    return;
                }

                note = _percussionMap[note];
                // Scale velocity;
                velocity = velocity * _percussionVelocityScale[note] / 127;
            }
            else if (patch >= 128)
            {
                if (patch == MIDI_UNMAPPED)
                    return;

                // Map to rhythm
                channel = MIDI_RHYTHM_CHANNEL;
                note = patch - 128;

                // Scale velocity;
                velocity = velocity * _percussionVelocityScale[note] / 127;
            }
            else {
                sbyte keyshift = _channels[channel].keyShift;

                int shiftNote = note + keyshift;

                if (keyshift > 0)
                {
                    while (shiftNote > 127)
                        shiftNote -= 12;
                }
                else {
                    while (shiftNote < 0)
                        shiftNote += 12;
                }

                note = shiftNote;

                // We assume that velocity 0 maps to 0 (for note off)
                int mapIndex = _channels[channel].velocityMapIdx;
                System.Diagnostics.Debug.Assert(velocity <= 127);
                velocity = _velocityMap[mapIndex][velocity];
            }

            _channels[channel].playing = true;
            _driver.Send((byte)(0x90 | channel), (byte)note, (byte)velocity);
        }

        private void ControlChange(int channel, int control, int value)
        {
            System.Diagnostics.Debug.Assert(channel <= 15);

            switch (control)
            {
                case 0x07:
                    _channels[channel].volume = (byte)value;

                    if (!_playSwitch)
                        return;

                    value += _channels[channel].volAdjust;

                    if (value > 0x7f)
                        value = 0x7f;

                    if (value < 0)
                        value = 1;

                    value *= _masterVolume;

                    if (value != 0)
                    {
                        value /= 15;

                        if (value == 0)
                            value = 1;
                    }
                    break;
                case 0x0a:
                    _channels[channel].pan = (byte)value;
                    break;
                case 0x40:
                    _channels[channel].hold = (byte)value;
                    break;
                case 0x4b:  // voice mapping
                    break;
                case 0x4e:  // velocity
                    break;
                case 0x7b:
                    _channels[channel].playing = false;
                    break;
            }

            _driver.Send((byte)(0xb0 | channel), (byte)control, (byte)value);
        }

        private void SetPatch(int channel, int patch)
        {
            bool resetVol = false;

            System.Diagnostics.Debug.Assert(channel <= 15);

            if ((channel == MIDI_RHYTHM_CHANNEL) || (_channels[channel].patch == patch))
                return;

            _channels[channel].patch = (byte)patch;
            _channels[channel].velocityMapIdx = _velocityMapIdx[patch];

            if (_channels[channel].mappedPatch == MIDI_UNMAPPED)
                resetVol = true;

            _channels[channel].mappedPatch = _patchMap[patch];

            if (_patchMap[patch] == MIDI_UNMAPPED)
            {
                DebugC(DebugLevels.Sound, "[Midi] Channel {0} set to unmapped patch {1}", channel, patch);
                _driver.Send((byte)(0xb0 | channel), 0x7b, 0);
                _driver.Send((byte)(0xb0 | channel), 0x40, 0);
                return;
            }

            if (_patchMap[patch] >= 128)
            {
                // Mapped to rhythm, don't send channel commands
                return;
            }

            if (_channels[channel].keyShift != _keyShift[patch])
            {
                _channels[channel].keyShift = (sbyte)_keyShift[patch];
                _driver.Send((byte)(0xb0 | channel), 0x7b, 0);
                _driver.Send((byte)(0xb0 | channel), 0x40, 0);
                resetVol = true;
            }

            if (resetVol || (_channels[channel].volAdjust != _volAdjust[patch]))
            {
                _channels[channel].volAdjust = (sbyte)_volAdjust[patch];
                ControlChange(channel, 0x07, _channels[channel].volume);
            }

            byte bendRange = _pitchBendRange[patch];
            if (bendRange != MIDI_UNMAPPED)
                _driver.SetPitchBendRange((byte)channel, bendRange);

            _driver.Send((byte)(0xc0 | channel), _patchMap[patch], 0);

            // Send a pointless command to work around a firmware bug in common
            // USB-MIDI cables. If the first MIDI command in a USB packet is a
            // Cx or Dx command, the second command in the packet is dropped
            // somewhere.
            // FIXME: consider putting a workaround in the MIDI backend drivers
            // instead.
            // Known to be affected: alsa, coremidi
            // Known *not* to be affected: windows (only seems to send one MIDI
            // command per USB packet even if the device allows larger packets).
            _driver.Send((byte)(0xb0 | channel), 0x0a, _channels[channel].pan);
        }

        private void SendMt32SysEx(uint addr, byte[] buf, int len, bool noDelay = false)
        {
            using (var str = new MemoryStream(buf, 0, len))
            {
                SendMt32SysEx(addr, str, len, noDelay);
            }
        }

        private void SendMt32SysEx(uint addr, Stream str, int len, bool noDelay = false)
        {
            if (len + 8 > MaxSysExSize)
            {
                Warning("SysEx message exceed maximum size; ignoring");
                return;
            }

            ushort chk = 0;

            _sysExBuf[4] = (byte)((addr >> 16) & 0xff);
            _sysExBuf[5] = (byte)((addr >> 8) & 0xff);
            _sysExBuf[6] = (byte)(addr & 0xff);

            for (int i = 0; i < len; i++)
                _sysExBuf[7 + i] = (byte)str.ReadByte();

            for (int i = 4; i < 7 + len; i++)
                chk -= _sysExBuf[i];

            _sysExBuf[7 + len] = (byte)(chk & 0x7f);

            if (noDelay)
                _driver.SysEx(_sysExBuf, (ushort)(len + 8));
            else
                SysEx(_sysExBuf, (ushort)(len + 8));
        }

        private struct Mt32ToGmMap
        {
            public string name;
            public byte gmInstr;
            public byte gmRhythmKey;

            public Mt32ToGmMap(string name, byte gmInstr, byte gmRhythmKey)
            {
                this.name = name;
                this.gmInstr = gmInstr;
                this.gmRhythmKey = gmRhythmKey;
            }
        }

        private static readonly Mt32ToGmMap[] Mt32PresetTimbreMaps = {
            /*000*/ new Mt32ToGmMap("AcouPiano1", 0, MIDI_UNMAPPED),
            /*001*/  new Mt32ToGmMap("AcouPiano2", 1, MIDI_UNMAPPED),
            /*002*/  new Mt32ToGmMap("AcouPiano3", 0, MIDI_UNMAPPED),
            /*003*/  new Mt32ToGmMap("ElecPiano1", 4, MIDI_UNMAPPED),
            /*004*/  new Mt32ToGmMap("ElecPiano2", 5, MIDI_UNMAPPED),
            /*005*/  new Mt32ToGmMap("ElecPiano3", 4, MIDI_UNMAPPED),
            /*006*/  new Mt32ToGmMap("ElecPiano4", 5, MIDI_UNMAPPED),
            /*007*/  new Mt32ToGmMap("Honkytonk ", 3, MIDI_UNMAPPED),
            /*008*/  new Mt32ToGmMap("Elec Org 1", 16, MIDI_UNMAPPED),
            /*009*/  new Mt32ToGmMap("Elec Org 2", 17, MIDI_UNMAPPED),
            /*010*/  new Mt32ToGmMap("Elec Org 3", 18, MIDI_UNMAPPED),
            /*011*/  new Mt32ToGmMap("Elec Org 4", 18, MIDI_UNMAPPED),
            /*012*/  new Mt32ToGmMap("Pipe Org 1", 19, MIDI_UNMAPPED),
            /*013*/  new Mt32ToGmMap("Pipe Org 2", 19, MIDI_UNMAPPED),
            /*014*/  new Mt32ToGmMap("Pipe Org 3", 20, MIDI_UNMAPPED),
            /*015*/  new Mt32ToGmMap("Accordion ", 21, MIDI_UNMAPPED),
            /*016*/  new Mt32ToGmMap("Harpsi 1  ", 6, MIDI_UNMAPPED),
            /*017*/  new Mt32ToGmMap("Harpsi 2  ", 6, MIDI_UNMAPPED),
            /*018*/  new Mt32ToGmMap("Harpsi 3  ", 6, MIDI_UNMAPPED),
            /*019*/  new Mt32ToGmMap("Clavi 1   ", 7, MIDI_UNMAPPED),
            /*020*/  new Mt32ToGmMap("Clavi 2   ", 7, MIDI_UNMAPPED),
            /*021*/  new Mt32ToGmMap("Clavi 3   ", 7, MIDI_UNMAPPED),
            /*022*/  new Mt32ToGmMap("Celesta 1 ", 8, MIDI_UNMAPPED),
            /*023*/  new Mt32ToGmMap("Celesta 2 ", 8, MIDI_UNMAPPED),
            /*024*/  new Mt32ToGmMap("Syn Brass1", 62, MIDI_UNMAPPED),
            /*025*/  new Mt32ToGmMap("Syn Brass2", 63, MIDI_UNMAPPED),
            /*026*/  new Mt32ToGmMap("Syn Brass3", 62, MIDI_UNMAPPED),
            /*027*/  new Mt32ToGmMap("Syn Brass4", 63, MIDI_UNMAPPED),
            /*028*/  new Mt32ToGmMap("Syn Bass 1", 38, MIDI_UNMAPPED),
            /*029*/  new Mt32ToGmMap("Syn Bass 2", 39, MIDI_UNMAPPED),
            /*030*/  new Mt32ToGmMap("Syn Bass 3", 38, MIDI_UNMAPPED),
            /*031*/  new Mt32ToGmMap("Syn Bass 4", 39, MIDI_UNMAPPED),
            /*032*/  new Mt32ToGmMap("Fantasy   ", 88, MIDI_UNMAPPED),
            /*033*/  new Mt32ToGmMap("Harmo Pan ", 89, MIDI_UNMAPPED),
            /*034*/  new Mt32ToGmMap("Chorale   ", 52, MIDI_UNMAPPED),
            /*035*/  new Mt32ToGmMap("Glasses   ", 98, MIDI_UNMAPPED),
            /*036*/  new Mt32ToGmMap("Soundtrack", 97, MIDI_UNMAPPED),
            /*037*/  new Mt32ToGmMap("Atmosphere", 99, MIDI_UNMAPPED),
            /*038*/  new Mt32ToGmMap("Warm Bell ", 89, MIDI_UNMAPPED),
            /*039*/  new Mt32ToGmMap("Funny Vox ", 85, MIDI_UNMAPPED),
            /*040*/  new Mt32ToGmMap("Echo Bell ", 39, MIDI_UNMAPPED),
            /*041*/  new Mt32ToGmMap("Ice Rain  ", 101, MIDI_UNMAPPED),
            /*042*/  new Mt32ToGmMap("Oboe 2001 ", 68, MIDI_UNMAPPED),
            /*043*/  new Mt32ToGmMap("Echo Pan  ", 87, MIDI_UNMAPPED),
            /*044*/  new Mt32ToGmMap("DoctorSolo", 86, MIDI_UNMAPPED),
            /*045*/  new Mt32ToGmMap("Schooldaze", 103, MIDI_UNMAPPED),
            /*046*/  new Mt32ToGmMap("BellSinger", 88, MIDI_UNMAPPED),
            /*047*/  new Mt32ToGmMap("SquareWave", 80, MIDI_UNMAPPED),
            /*048*/  new Mt32ToGmMap("Str Sect 1", 48, MIDI_UNMAPPED),
            /*049*/  new Mt32ToGmMap("Str Sect 2", 48, MIDI_UNMAPPED),
            /*050*/  new Mt32ToGmMap("Str Sect 3", 49, MIDI_UNMAPPED),
            /*051*/  new Mt32ToGmMap("Pizzicato ", 45, MIDI_UNMAPPED),
            /*052*/  new Mt32ToGmMap("Violin 1  ", 40, MIDI_UNMAPPED),
            /*053*/  new Mt32ToGmMap("Violin 2  ", 40, MIDI_UNMAPPED),
            /*054*/  new Mt32ToGmMap("Cello 1   ", 42, MIDI_UNMAPPED),
            /*055*/  new Mt32ToGmMap("Cello 2   ", 42, MIDI_UNMAPPED),
            /*056*/  new Mt32ToGmMap("Contrabass", 43, MIDI_UNMAPPED),
            /*057*/  new Mt32ToGmMap("Harp 1    ", 46, MIDI_UNMAPPED),
            /*058*/  new Mt32ToGmMap("Harp 2    ", 46, MIDI_UNMAPPED),
            /*059*/  new Mt32ToGmMap("Guitar 1  ", 24, MIDI_UNMAPPED),
            /*060*/  new Mt32ToGmMap("Guitar 2  ", 25, MIDI_UNMAPPED),
            /*061*/  new Mt32ToGmMap("Elec Gtr 1", 26, MIDI_UNMAPPED),
            /*062*/  new Mt32ToGmMap("Elec Gtr 2", 27, MIDI_UNMAPPED),
            /*063*/  new Mt32ToGmMap("Sitar     ", 104, MIDI_UNMAPPED),
            /*064*/  new Mt32ToGmMap("Acou Bass1", 32, MIDI_UNMAPPED),
            /*065*/  new Mt32ToGmMap("Acou Bass2", 33, MIDI_UNMAPPED),
            /*066*/  new Mt32ToGmMap("Elec Bass1", 34, MIDI_UNMAPPED),
            /*067*/  new Mt32ToGmMap("Elec Bass2", 39, MIDI_UNMAPPED),
            /*068*/  new Mt32ToGmMap("Slap Bass1", 36, MIDI_UNMAPPED),
            /*069*/  new Mt32ToGmMap("Slap Bass2", 37, MIDI_UNMAPPED),
            /*070*/  new Mt32ToGmMap("Fretless 1", 35, MIDI_UNMAPPED),
            /*071*/  new Mt32ToGmMap("Fretless 2", 35, MIDI_UNMAPPED),
            /*072*/  new Mt32ToGmMap("Flute 1   ", 73, MIDI_UNMAPPED),
            /*073*/  new Mt32ToGmMap("Flute 2   ", 73, MIDI_UNMAPPED),
            /*074*/  new Mt32ToGmMap("Piccolo 1 ", 72, MIDI_UNMAPPED),
            /*075*/  new Mt32ToGmMap("Piccolo 2 ", 72, MIDI_UNMAPPED),
            /*076*/  new Mt32ToGmMap("Recorder  ", 74, MIDI_UNMAPPED),
            /*077*/  new Mt32ToGmMap("Panpipes  ", 75, MIDI_UNMAPPED),
            /*078*/  new Mt32ToGmMap("Sax 1     ", 64, MIDI_UNMAPPED),
            /*079*/  new Mt32ToGmMap("Sax 2     ", 65, MIDI_UNMAPPED),
            /*080*/  new Mt32ToGmMap("Sax 3     ", 66, MIDI_UNMAPPED),
            /*081*/  new Mt32ToGmMap("Sax 4     ", 67, MIDI_UNMAPPED),
            /*082*/  new Mt32ToGmMap("Clarinet 1", 71, MIDI_UNMAPPED),
            /*083*/  new Mt32ToGmMap("Clarinet 2", 71, MIDI_UNMAPPED),
            /*084*/  new Mt32ToGmMap("Oboe      ", 68, MIDI_UNMAPPED),
            /*085*/  new Mt32ToGmMap("Engl Horn ", 69, MIDI_UNMAPPED),
            /*086*/  new Mt32ToGmMap("Bassoon   ", 70, MIDI_UNMAPPED),
            /*087*/  new Mt32ToGmMap("Harmonica ", 22, MIDI_UNMAPPED),
            /*088*/  new Mt32ToGmMap("Trumpet 1 ", 56, MIDI_UNMAPPED),
            /*089*/  new Mt32ToGmMap("Trumpet 2 ", 56, MIDI_UNMAPPED),
            /*090*/  new Mt32ToGmMap("Trombone 1", 57, MIDI_UNMAPPED),
            /*091*/  new Mt32ToGmMap("Trombone 2", 57, MIDI_UNMAPPED),
            /*092*/  new Mt32ToGmMap("Fr Horn 1 ", 60, MIDI_UNMAPPED),
            /*093*/  new Mt32ToGmMap("Fr Horn 2 ", 60, MIDI_UNMAPPED),
            /*094*/  new Mt32ToGmMap("Tuba      ", 58, MIDI_UNMAPPED),
            /*095*/  new Mt32ToGmMap("Brs Sect 1", 61, MIDI_UNMAPPED),
            /*096*/  new Mt32ToGmMap("Brs Sect 2", 61, MIDI_UNMAPPED),
            /*097*/  new Mt32ToGmMap("Vibe 1    ", 11, MIDI_UNMAPPED),
            /*098*/  new Mt32ToGmMap("Vibe 2    ", 11, MIDI_UNMAPPED),
            /*099*/  new Mt32ToGmMap("Syn Mallet", 15, MIDI_UNMAPPED),
            /*100*/  new Mt32ToGmMap("Wind Bell ", 88, MIDI_UNMAPPED),
            /*101*/  new Mt32ToGmMap("Glock     ", 9, MIDI_UNMAPPED),
            /*102*/  new Mt32ToGmMap("Tube Bell ", 14, MIDI_UNMAPPED),
            /*103*/  new Mt32ToGmMap("Xylophone ", 13, MIDI_UNMAPPED),
            /*104*/  new Mt32ToGmMap("Marimba   ", 12, MIDI_UNMAPPED),
            /*105*/  new Mt32ToGmMap("Koto      ", 107, MIDI_UNMAPPED),
            /*106*/  new Mt32ToGmMap("Sho       ", 111, MIDI_UNMAPPED),
            /*107*/  new Mt32ToGmMap("Shakuhachi", 77, MIDI_UNMAPPED),
            /*108*/  new Mt32ToGmMap("Whistle 1 ", 78, MIDI_UNMAPPED),
            /*109*/  new Mt32ToGmMap("Whistle 2 ", 78, MIDI_UNMAPPED),
            /*110*/  new Mt32ToGmMap("BottleBlow", 76, MIDI_UNMAPPED),
            /*111*/  new Mt32ToGmMap("BreathPipe", 121, MIDI_UNMAPPED),
            /*112*/  new Mt32ToGmMap("Timpani   ", 47, MIDI_UNMAPPED),
            /*113*/  new Mt32ToGmMap("MelodicTom", 117, MIDI_UNMAPPED),
            /*114*/  new Mt32ToGmMap("Deep Snare", MIDI_MAPPED_TO_RHYTHM, 38),
            /*115*/  new Mt32ToGmMap("Elec Perc1", 115, MIDI_UNMAPPED), // ?
            /*116*/  new Mt32ToGmMap("Elec Perc2", 118, MIDI_UNMAPPED), // ?
            /*117*/  new Mt32ToGmMap("Taiko     ", 116, MIDI_UNMAPPED),
            /*118*/  new Mt32ToGmMap("Taiko Rim ", 118, MIDI_UNMAPPED),
            /*119*/  new Mt32ToGmMap("Cymbal    ", MIDI_MAPPED_TO_RHYTHM, 51),
            /*120*/  new Mt32ToGmMap("Castanets ", MIDI_MAPPED_TO_RHYTHM, 75), // approximation
            /*121*/  new Mt32ToGmMap("Triangle  ", 112, MIDI_UNMAPPED),
            /*122*/  new Mt32ToGmMap("Orche Hit ", 55, MIDI_UNMAPPED),
            /*123*/  new Mt32ToGmMap("Telephone ", 124, MIDI_UNMAPPED),
            /*124*/  new Mt32ToGmMap("Bird Tweet", 123, MIDI_UNMAPPED),
            /*125*/  new Mt32ToGmMap("OneNoteJam", 8, MIDI_UNMAPPED), // approximation
            /*126*/  new Mt32ToGmMap("WaterBells", 98, MIDI_UNMAPPED),
            /*127*/  new Mt32ToGmMap("JungleTune", 75, MIDI_UNMAPPED) // approximation
        };

        private static readonly Mt32ToGmMap[] Mt32RhythmTimbreMaps = {
            /*00*/  new Mt32ToGmMap("Acou BD   ", MIDI_MAPPED_TO_RHYTHM, 35),
            /*01*/  new Mt32ToGmMap("Acou SD   ", MIDI_MAPPED_TO_RHYTHM, 38),
            /*02*/  new Mt32ToGmMap("Acou HiTom", 117, 50),
            /*03*/  new Mt32ToGmMap("AcouMidTom", 117, 47),
            /*04*/  new Mt32ToGmMap("AcouLowTom", 117, 41),
            /*05*/  new Mt32ToGmMap("Elec SD   ", MIDI_MAPPED_TO_RHYTHM, 40),
            /*06*/  new Mt32ToGmMap("Clsd HiHat", MIDI_MAPPED_TO_RHYTHM, 42),
            /*07*/  new Mt32ToGmMap("OpenHiHat1", MIDI_MAPPED_TO_RHYTHM, 46),
            /*08*/  new Mt32ToGmMap("Crash Cym ", MIDI_MAPPED_TO_RHYTHM, 49),
            /*09*/  new Mt32ToGmMap("Ride Cym  ", MIDI_MAPPED_TO_RHYTHM, 51),
            /*10*/  new Mt32ToGmMap("Rim Shot  ", MIDI_MAPPED_TO_RHYTHM, 37),
            /*11*/  new Mt32ToGmMap("Hand Clap ", MIDI_MAPPED_TO_RHYTHM, 39),
            /*12*/  new Mt32ToGmMap("Cowbell   ", MIDI_MAPPED_TO_RHYTHM, 56),
            /*13*/  new Mt32ToGmMap("Mt HiConga", MIDI_MAPPED_TO_RHYTHM, 62),
            /*14*/  new Mt32ToGmMap("High Conga", MIDI_MAPPED_TO_RHYTHM, 63),
            /*15*/  new Mt32ToGmMap("Low Conga ", MIDI_MAPPED_TO_RHYTHM, 64),
            /*16*/  new Mt32ToGmMap("Hi Timbale", MIDI_MAPPED_TO_RHYTHM, 65),
            /*17*/  new Mt32ToGmMap("LowTimbale", MIDI_MAPPED_TO_RHYTHM, 66),
            /*18*/  new Mt32ToGmMap("High Bongo", MIDI_MAPPED_TO_RHYTHM, 60),
            /*19*/  new Mt32ToGmMap("Low Bongo ", MIDI_MAPPED_TO_RHYTHM, 61),
            /*20*/  new Mt32ToGmMap("High Agogo", 113, 67),
            /*21*/  new Mt32ToGmMap("Low Agogo ", 113, 68),
            /*22*/  new Mt32ToGmMap("Tambourine", MIDI_MAPPED_TO_RHYTHM, 54),
            /*23*/  new Mt32ToGmMap("Claves    ", MIDI_MAPPED_TO_RHYTHM, 75),
            /*24*/  new Mt32ToGmMap("Maracas   ", MIDI_MAPPED_TO_RHYTHM, 70),
            /*25*/  new Mt32ToGmMap("SmbaWhis L", 78, 72),
            /*26*/  new Mt32ToGmMap("SmbaWhis S", 78, 71),
            /*27*/  new Mt32ToGmMap("Cabasa    ", MIDI_MAPPED_TO_RHYTHM, 69),
            /*28*/  new Mt32ToGmMap("Quijada   ", MIDI_MAPPED_TO_RHYTHM, 73),
            /*29*/  new Mt32ToGmMap("OpenHiHat2", MIDI_MAPPED_TO_RHYTHM, 44)
        };

        private static readonly byte[] Mt32PresetRhythmKeymap = {
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, 35, 36, 37, 38, 39,
            40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
            50, 51, MIDI_UNMAPPED, MIDI_UNMAPPED, 54, MIDI_UNMAPPED, 56, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
            70, 71, 72, 73, MIDI_UNMAPPED, 75, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED,
            MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED, MIDI_UNMAPPED
        };

        /* +++ - Don't change unless you've got a good reason
   ++  - Looks good, sounds ok
   +   - Not too bad, but is it right?
   ?   - Where do I map this one?
   ??  - Any good ideas?
   ??? - I'm clueless?
   R   - Rhythm...
*/

        private static readonly Mt32ToGmMap[] Mt32MemoryTimbreMaps = {
            new Mt32ToGmMap("AccPnoKA2 ", 1, MIDI_UNMAPPED),     // ++ (KQ1)
            new Mt32ToGmMap("Acou BD   ", MIDI_MAPPED_TO_RHYTHM, 35),   // R (PQ2)
            new Mt32ToGmMap("Acou SD   ", MIDI_MAPPED_TO_RHYTHM, 38),   // R (PQ2)
            new Mt32ToGmMap("AcouPnoKA ", 0, MIDI_UNMAPPED),     // ++ (KQ1)
            new Mt32ToGmMap("BASS      ", 32, MIDI_UNMAPPED),    // + (LSL3)
            new Mt32ToGmMap("BASSOONPCM", 70, MIDI_UNMAPPED),    // + (LB1)
            new Mt32ToGmMap("BEACH WAVE", 122, MIDI_UNMAPPED),   // + (LSL3)
            new Mt32ToGmMap("BagPipes  ", 109, MIDI_UNMAPPED),
            new Mt32ToGmMap("BassPizzMS", 45, MIDI_UNMAPPED),    // ++ (QFG1)
            new Mt32ToGmMap("BassoonKA ", 70, MIDI_UNMAPPED),    // ++ (KQ1)
            new Mt32ToGmMap("Bell    MS", 112, MIDI_UNMAPPED),   // ++ (Iceman)
            new Mt32ToGmMap("Bells   MS", 112, MIDI_UNMAPPED),   // + (QFG1)
            new Mt32ToGmMap("Big Bell  ", 14, MIDI_UNMAPPED),    // + (LB1)
            new Mt32ToGmMap("Bird Tweet", 123, MIDI_UNMAPPED),
            new Mt32ToGmMap("BrsSect MS", 61, MIDI_UNMAPPED),    // +++ (Iceman)
            new Mt32ToGmMap("CLAPPING  ", 126, MIDI_UNMAPPED),   // ++ (LSL3)
            new Mt32ToGmMap("Cabasa    ", MIDI_MAPPED_TO_RHYTHM, 69),   // R (Hoyle)
            new Mt32ToGmMap("Calliope  ", 82, MIDI_UNMAPPED),    // +++ (QFG1)
            new Mt32ToGmMap("CelticHarp", 46, MIDI_UNMAPPED),    // ++ (Camelot)
            new Mt32ToGmMap("Chicago MS", 1, MIDI_UNMAPPED),     // ++ (Iceman)
            new Mt32ToGmMap("Chop      ", 117, MIDI_UNMAPPED),
            new Mt32ToGmMap("Chorale MS", 52, MIDI_UNMAPPED),    // + (Camelot)
            new Mt32ToGmMap("ClarinetMS", 71, MIDI_UNMAPPED),
            new Mt32ToGmMap("Claves    ", MIDI_MAPPED_TO_RHYTHM, 75),   // R (PQ2)
            new Mt32ToGmMap("Claw    MS", 118, MIDI_UNMAPPED),    // + (QFG1)
            new Mt32ToGmMap("ClockBell ", 14, MIDI_UNMAPPED),    // + (LB1)
            new Mt32ToGmMap("ConcertCym", MIDI_MAPPED_TO_RHYTHM, 55),   // R ? (KQ1)
            new Mt32ToGmMap("Conga   MS", MIDI_MAPPED_TO_RHYTHM, 64),   // R (QFG1)
            new Mt32ToGmMap("CoolPhone ", 124, MIDI_UNMAPPED),   // ++ (LSL3)
            new Mt32ToGmMap("CracklesMS", 115, MIDI_UNMAPPED), // ? (Camelot, QFG1)
            new Mt32ToGmMap("CreakyD MS", MIDI_UNMAPPED, MIDI_UNMAPPED), // ??? (KQ1)
            new Mt32ToGmMap("Cricket   ", 120, MIDI_UNMAPPED), // ? (LB1)
            new Mt32ToGmMap("CrshCymbMS", MIDI_MAPPED_TO_RHYTHM, 57),   // R +++ (Iceman)
            new Mt32ToGmMap("CstlGateMS", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (QFG1)
            new Mt32ToGmMap("CymSwellMS", MIDI_MAPPED_TO_RHYTHM, 55),   // R ? (Camelot, QFG1)
            new Mt32ToGmMap("CymbRollKA", MIDI_MAPPED_TO_RHYTHM, 57),   // R ? (KQ1)
            new Mt32ToGmMap("Cymbal Lo ", MIDI_UNMAPPED, MIDI_UNMAPPED), // R ? (LSL3)
            new Mt32ToGmMap("card      ", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (Hoyle)
            new Mt32ToGmMap("DirtGtr MS", 30, MIDI_UNMAPPED),    // + (Iceman)
            new Mt32ToGmMap("DirtGtr2MS", 29, MIDI_UNMAPPED),    // + (Iceman)
            new Mt32ToGmMap("E Bass  MS", 33, MIDI_UNMAPPED),    // + (SQ3)
            new Mt32ToGmMap("ElecBassMS", 33, MIDI_UNMAPPED),
            new Mt32ToGmMap("ElecGtr MS", 27, MIDI_UNMAPPED),    // ++ (Iceman)
            new Mt32ToGmMap("EnglHornMS", 69, MIDI_UNMAPPED),
            new Mt32ToGmMap("FantasiaKA", 88, MIDI_UNMAPPED),
            new Mt32ToGmMap("Fantasy   ", 99, MIDI_UNMAPPED),    // + (PQ2)
            new Mt32ToGmMap("Fantasy2MS", 99, MIDI_UNMAPPED),    // ++ (Camelot, QFG1)
            new Mt32ToGmMap("Filter  MS", 95, MIDI_UNMAPPED),    // +++ (Iceman)
            new Mt32ToGmMap("Filter2 MS", 95, MIDI_UNMAPPED),    // ++ (Iceman)
            new Mt32ToGmMap("Flame2  MS", 121, MIDI_UNMAPPED),   // ? (QFG1)
            new Mt32ToGmMap("Flames  MS", 121, MIDI_UNMAPPED),   // ? (QFG1)
            new Mt32ToGmMap("Flute   MS", 73, MIDI_UNMAPPED),    // +++ (QFG1)
            new Mt32ToGmMap("FogHorn MS", 58, MIDI_UNMAPPED),
            new Mt32ToGmMap("FrHorn1 MS", 60, MIDI_UNMAPPED),    // +++ (QFG1)
            new Mt32ToGmMap("FunnyTrmp ", 56, MIDI_UNMAPPED),    // ++ (LB1)
            new Mt32ToGmMap("GameSnd MS", 80, MIDI_UNMAPPED),
            new Mt32ToGmMap("Glock   MS", 9, MIDI_UNMAPPED),     // +++ (QFG1)
            new Mt32ToGmMap("Gunshot   ", 127, MIDI_UNMAPPED),   // +++ (LB1)
            new Mt32ToGmMap("Hammer  MS", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (QFG1)
            new Mt32ToGmMap("Harmonica2", 22, MIDI_UNMAPPED),    // +++ (LB1)
            new Mt32ToGmMap("Harpsi 1  ", 6, MIDI_UNMAPPED),     // + (Hoyle)
            new Mt32ToGmMap("Harpsi 2  ", 6, MIDI_UNMAPPED),     // +++ (LB1)
            new Mt32ToGmMap("Heart   MS", 116, MIDI_UNMAPPED),   // ? (Iceman)
            new Mt32ToGmMap("Horse1  MS", 115, MIDI_UNMAPPED),   // ? (Camelot, QFG1)
            new Mt32ToGmMap("Horse2  MS", 115, MIDI_UNMAPPED),   // ? (Camelot, QFG1)
            new Mt32ToGmMap("InHale  MS", 121, MIDI_UNMAPPED),   // ++ (Iceman)
            new Mt32ToGmMap("KNIFE     ", 120, MIDI_UNMAPPED),   // ? (LSL3)
            new Mt32ToGmMap("KenBanjo  ", 105, MIDI_UNMAPPED),   // +++ (LB1)
            new Mt32ToGmMap("Kiss    MS", 25, MIDI_UNMAPPED),    // ++ (QFG1)
            new Mt32ToGmMap("KongHit   ", MIDI_UNMAPPED, MIDI_UNMAPPED), // ??? (KQ1)
            new Mt32ToGmMap("Koto      ", 107, MIDI_UNMAPPED),   // +++ (PQ2)
            new Mt32ToGmMap("Laser   MS", 81, MIDI_UNMAPPED),    // ?? (QFG1)
            new Mt32ToGmMap("Meeps   MS", 62, MIDI_UNMAPPED),    // ? (QFG1)
            new Mt32ToGmMap("MTrak   MS", 62, MIDI_UNMAPPED),    // ?? (Iceman)
            new Mt32ToGmMap("MachGun MS", 127, MIDI_UNMAPPED),   // ? (Iceman)
            new Mt32ToGmMap("OCEANSOUND", 122, MIDI_UNMAPPED),   // + (LSL3)
            new Mt32ToGmMap("Oboe 2001 ", 68, MIDI_UNMAPPED),    // + (PQ2)
            new Mt32ToGmMap("Ocean   MS", 122, MIDI_UNMAPPED),   // + (Iceman)
            new Mt32ToGmMap("PPG 2.3 MS", 75, MIDI_UNMAPPED),    // ? (Iceman)
            new Mt32ToGmMap("PianoCrank", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (LB1)
            new Mt32ToGmMap("PicSnareMS", MIDI_MAPPED_TO_RHYTHM, 40),   // R ? (Iceman)
            new Mt32ToGmMap("PiccoloKA ", 72, MIDI_UNMAPPED),    // +++ (KQ1)
            new Mt32ToGmMap("PinkBassMS", 39, MIDI_UNMAPPED),
            new Mt32ToGmMap("Pizz2     ", 45, MIDI_UNMAPPED),    // ++ (LB1)
            new Mt32ToGmMap("Portcullis", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (KQ1)
            new Mt32ToGmMap("Raspbry MS", 81, MIDI_UNMAPPED),    // ? (QFG1)
            new Mt32ToGmMap("RatSqueek ", 72, MIDI_UNMAPPED),    // ? (LauraBow1, Camelot)
            new Mt32ToGmMap("Record78  ", MIDI_UNMAPPED, MIDI_UNMAPPED), // +++ (LB1)
            new Mt32ToGmMap("RecorderMS", 74, MIDI_UNMAPPED),    // +++ (Camelot)
            new Mt32ToGmMap("Red Baron ", 125, MIDI_UNMAPPED),   // ? (LB1)
            new Mt32ToGmMap("ReedPipMS ", 20, MIDI_UNMAPPED),    // +++ (Camelot)
            new Mt32ToGmMap("RevCymb MS", 119, MIDI_UNMAPPED),
            new Mt32ToGmMap("RifleShot ", 127, MIDI_UNMAPPED),   // + (LB1)
            new Mt32ToGmMap("RimShot MS", MIDI_MAPPED_TO_RHYTHM, 37),   // R
            new Mt32ToGmMap("SHOWER    ", 52, MIDI_UNMAPPED),    // ? (LSL3)
            new Mt32ToGmMap("SQ Bass MS", 32, MIDI_UNMAPPED),    // + (SQ3)
            new Mt32ToGmMap("ShakuVibMS", 79, MIDI_UNMAPPED),    // + (Iceman)
            new Mt32ToGmMap("SlapBassMS", 36, MIDI_UNMAPPED),    // +++ (Iceman)
            new Mt32ToGmMap("Snare   MS", MIDI_MAPPED_TO_RHYTHM, 38),   // R (QFG1)
            new Mt32ToGmMap("Some Birds", 123, MIDI_UNMAPPED),   // + (LB1)
            new Mt32ToGmMap("Sonar   MS", 78, MIDI_UNMAPPED),    // ? (Iceman)
            new Mt32ToGmMap("Soundtrk2 ", 97, MIDI_UNMAPPED),    // +++ (LB1)
            new Mt32ToGmMap("Soundtrack", 97, MIDI_UNMAPPED),    // ++ (Camelot)
            new Mt32ToGmMap("SqurWaveMS", 80, MIDI_UNMAPPED),
            new Mt32ToGmMap("StabBassMS", 34, MIDI_UNMAPPED),    // + (Iceman)
            new Mt32ToGmMap("SteelDrmMS", 114, MIDI_UNMAPPED),   // +++ (Iceman)
            new Mt32ToGmMap("StrSect1MS", 48, MIDI_UNMAPPED),    // ++ (QFG1)
            new Mt32ToGmMap("String  MS", 45, MIDI_UNMAPPED),    // + (Camelot)
            new Mt32ToGmMap("Syn-Choir ", 91, MIDI_UNMAPPED),
            new Mt32ToGmMap("Syn Brass4", 63, MIDI_UNMAPPED),    // ++ (PQ2)
            new Mt32ToGmMap("SynBass MS", 38, MIDI_UNMAPPED),
            new Mt32ToGmMap("SwmpBackgr", 120, MIDI_UNMAPPED),    // ?? (LB1, QFG1)
            new Mt32ToGmMap("T-Bone2 MS", 57, MIDI_UNMAPPED),    // +++ (QFG1)
            new Mt32ToGmMap("Taiko     ", 116, 35),      // +++ (Camelot)
            new Mt32ToGmMap("Taiko Rim ", 118, 37),      // +++ (LSL3)
            new Mt32ToGmMap("Timpani1  ", 47, MIDI_UNMAPPED),    // +++ (LB1)
            new Mt32ToGmMap("Tom     MS", 117, 48),      // +++ (Iceman)
            new Mt32ToGmMap("Toms    MS", 117, 48),      // +++ (Camelot, QFG1)
            new Mt32ToGmMap("Tpt1prtl  ", 56, MIDI_UNMAPPED),    // +++ (KQ1)
            new Mt32ToGmMap("TriangleMS", 112, 81),      // R (Camelot)
            new Mt32ToGmMap("Trumpet 1 ", 56, MIDI_UNMAPPED),    // +++ (Camelot)
            new Mt32ToGmMap("Type    MS", MIDI_MAPPED_TO_RHYTHM, 39),   // + (Iceman)
            new Mt32ToGmMap("Warm Pad"  , 89, MIDI_UNMAPPED),  // ++ (PQ3)
            new Mt32ToGmMap("WaterBells", 98, MIDI_UNMAPPED),    // + (PQ2)
            new Mt32ToGmMap("WaterFallK", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (KQ1)
            new Mt32ToGmMap("Whiporill ", 123, MIDI_UNMAPPED),   // + (LB1)
            new Mt32ToGmMap("Wind      ", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (LB1)
            new Mt32ToGmMap("Wind    MS", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (QFG1, Iceman)
            new Mt32ToGmMap("Wind2   MS", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (Camelot)
            new Mt32ToGmMap("Woodpecker", 115, MIDI_UNMAPPED),   // ? (LB1)
            new Mt32ToGmMap("WtrFall MS", MIDI_UNMAPPED, MIDI_UNMAPPED), // ? (Camelot, QFG1, Iceman)
            new Mt32ToGmMap(null, 0, 0)
        };

        private static string[] GmInstrumentNames = {
    /*000*/  "Acoustic Grand Piano",
    /*001*/  "Bright Acoustic Piano",
    /*002*/  "Electric Grand Piano",
    /*003*/  "Honky-tonk Piano",
    /*004*/  "Electric Piano 1",
    /*005*/  "Electric Piano 2",
    /*006*/  "Harpsichord",
    /*007*/  "Clavinet",
    /*008*/  "Celesta",
    /*009*/  "Glockenspiel",
    /*010*/  "Music Box",
    /*011*/  "Vibraphone",
    /*012*/  "Marimba",
    /*013*/  "Xylophone",
    /*014*/  "Tubular Bells",
    /*015*/  "Dulcimer",
    /*016*/  "Drawbar Organ",
    /*017*/  "Percussive Organ",
    /*018*/  "Rock Organ",
    /*019*/  "Church Organ",
    /*020*/  "Reed Organ",
    /*021*/  "Accordion",
    /*022*/  "Harmonica",
    /*023*/  "Tango Accordion",
    /*024*/  "Acoustic Guitar (nylon)",
    /*025*/  "Acoustic Guitar (steel)",
    /*026*/  "Electric Guitar (jazz)",
    /*027*/  "Electric Guitar (clean)",
    /*028*/  "Electric Guitar (muted)",
    /*029*/  "Overdriven Guitar",
    /*030*/  "Distortion Guitar",
    /*031*/  "Guitar Harmonics",
    /*032*/  "Acoustic Bass",
    /*033*/  "Electric Bass (finger)",
    /*034*/  "Electric Bass (pick)",
    /*035*/  "Fretless Bass",
    /*036*/  "Slap Bass 1",
    /*037*/  "Slap Bass 2",
    /*038*/  "Synth Bass 1",
    /*039*/  "Synth Bass 2",
    /*040*/  "Violin",
    /*041*/  "Viola",
    /*042*/  "Cello",
    /*043*/  "Contrabass",
    /*044*/  "Tremolo Strings",
    /*045*/  "Pizzicato Strings",
    /*046*/  "Orchestral Harp",
    /*047*/  "Timpani",
    /*048*/  "String Ensemble 1",
    /*049*/  "String Ensemble 2",
    /*050*/  "SynthStrings 1",
    /*051*/  "SynthStrings 2",
    /*052*/  "Choir Aahs",
    /*053*/  "Voice Oohs",
    /*054*/  "Synth Voice",
    /*055*/  "Orchestra Hit",
    /*056*/  "Trumpet",
    /*057*/  "Trombone",
    /*058*/  "Tuba",
    /*059*/  "Muted Trumpet",
    /*060*/  "French Horn",
    /*061*/  "Brass Section",
    /*062*/  "SynthBrass 1",
    /*063*/  "SynthBrass 2",
    /*064*/  "Soprano Sax",
    /*065*/  "Alto Sax",
    /*066*/  "Tenor Sax",
    /*067*/  "Baritone Sax",
    /*068*/  "Oboe",
    /*069*/  "English Horn",
    /*070*/  "Bassoon",
    /*071*/  "Clarinet",
    /*072*/  "Piccolo",
    /*073*/  "Flute",
    /*074*/  "Recorder",
    /*075*/  "Pan Flute",
    /*076*/  "Blown Bottle",
    /*077*/  "Shakuhachi",
    /*078*/  "Whistle",
    /*079*/  "Ocarina",
    /*080*/  "Lead 1 (square)",
    /*081*/  "Lead 2 (sawtooth)",
    /*082*/  "Lead 3 (calliope)",
    /*083*/  "Lead 4 (chiff)",
    /*084*/  "Lead 5 (charang)",
    /*085*/  "Lead 6 (voice)",
    /*086*/  "Lead 7 (fifths)",
    /*087*/  "Lead 8 (bass+lead)",
    /*088*/  "Pad 1 (new age)",
    /*089*/  "Pad 2 (warm)",
    /*090*/  "Pad 3 (polysynth)",
    /*091*/  "Pad 4 (choir)",
    /*092*/  "Pad 5 (bowed)",
    /*093*/  "Pad 6 (metallic)",
    /*094*/  "Pad 7 (halo)",
    /*095*/  "Pad 8 (sweep)",
    /*096*/  "FX 1 (rain)",
    /*097*/  "FX 2 (soundtrack)",
    /*098*/  "FX 3 (crystal)",
    /*099*/  "FX 4 (atmosphere)",
    /*100*/  "FX 5 (brightness)",
    /*101*/  "FX 6 (goblins)",
    /*102*/  "FX 7 (echoes)",
    /*103*/  "FX 8 (sci-fi)",
    /*104*/  "Sitar",
    /*105*/  "Banjo",
    /*106*/  "Shamisen",
    /*107*/  "Koto",
    /*108*/  "Kalimba",
    /*109*/  "Bag pipe",
    /*110*/  "Fiddle",
    /*111*/  "Shannai",
    /*112*/  "Tinkle Bell",
    /*113*/  "Agogo",
    /*114*/  "Steel Drums",
    /*115*/  "Woodblock",
    /*116*/  "Taiko Drum",
    /*117*/  "Melodic Tom",
    /*118*/  "Synth Drum",
    /*119*/  "Reverse Cymbal",
    /*120*/  "Guitar Fret Noise",
    /*121*/  "Breath Noise",
    /*122*/  "Seashore",
    /*123*/  "Bird Tweet",
    /*124*/  "Telephone Ring",
    /*125*/  "Helicopter",
    /*126*/  "Applause",
    /*127*/  "Gunshot"
};

        // The GM Percussion map is downwards compatible to the MT32 map, which is used in SCI
        private static readonly string[] GmPercussionNames = {
    /*00*/  string.Empty,
    /*10*/  string.Empty,
    /*20*/  string.Empty,
    /*30*/  string.Empty,
    // The preceeding percussions are not covered by the GM standard
    /*35*/  "Acoustic Bass Drum",
    /*36*/  "Bass Drum 1",
    /*37*/  "Side Stick",
    /*38*/  "Acoustic Snare",
    /*39*/  "Hand Clap",
    /*40*/  "Electric Snare",
    /*41*/  "Low Floor Tom",
    /*42*/  "Closed Hi-Hat",
    /*43*/  "High Floor Tom",
    /*44*/  "Pedal Hi-Hat",
    /*45*/  "Low Tom",
    /*46*/  "Open Hi-Hat",
    /*47*/  "Low-Mid Tom",
    /*48*/  "Hi-Mid Tom",
    /*49*/  "Crash Cymbal 1",
    /*50*/  "High Tom",
    /*51*/  "Ride Cymbal 1",
    /*52*/  "Chinese Cymbal",
    /*53*/  "Ride Bell",
    /*54*/  "Tambourine",
    /*55*/  "Splash Cymbal",
    /*56*/  "Cowbell",
    /*57*/  "Crash Cymbal 2",
    /*58*/  "Vibraslap",
    /*59*/  "Ride Cymbal 2",
    /*60*/  "Hi Bongo",
    /*61*/  "Low Bongo",
    /*62*/  "Mute Hi Conga",
    /*63*/  "Open Hi Conga",
    /*64*/  "Low Conga",
    /*65*/  "High Timbale",
    /*66*/  "Low Timbale",
    /*67*/  "High Agogo",
    /*68*/  "Low Agogo",
    /*69*/  "Cabasa",
    /*70*/  "Maracas",
    /*71*/  "Short Whistle",
    /*72*/  "Long Whistle",
    /*73*/  "Short Guiro",
    /*74*/  "Long Guiro",
    /*75*/  "Claves",
    /*76*/  "Hi Wood Block",
    /*77*/  "Low Wood Block",
    /*78*/  "Mute Cuica",
    /*79*/  "Open Cuica",
    /*80*/  "Mute Triangle",
    /*81*/  "Open Triangle"
};

    }
}
