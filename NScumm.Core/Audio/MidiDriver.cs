//
//  MidiDriver.cs
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
using System.Linq;
using NScumm.Core.Audio.Midi;
using NScumm.Core.Common;

namespace NScumm.Core.Audio
{
    enum DeviceStringType
    {
        DriverName,
        DriverId,
        DeviceName,
        DeviceId
    }

    /// <summary>
    /// Music types that music drivers can implement and engines can rely on.
    /// </summary>
    public enum MusicType
    {
        // Invalid output
        Invalid = -1,
        // Auto
        Auto = 0,
        // Null
        Null,
        // PC Speaker
        PCSpeaker,
        // PCjr
        PCjr,
        // CMS
        CMS,
        // AdLib
        AdLib,
        // C64
        C64,
        // Amiga
        Amiga,
        // Apple IIGS
        AppleIIGS,
        // FM-TOWNS
        FMTowns,
        // PC98
        PC98,
        // General MIDI
        GeneralMidi,
        // MT-32
        MT32,
        // Roland GS
        RolandGS
    }

    [Flags]
    public enum MusicDriverTypes
    {
        None = 0,
        PCSpeaker = 1 << 0,
        // PC Speaker: Maps to MT_PCSPK and MT_PCJR
        CMS = 1 << 1,
        // Creative Music System / Gameblaster: Maps to MT_CMS
        PCjr = 1 << 2,
        // Tandy/PC Junior driver
        AdLib = 1 << 3,
        // AdLib: Maps to MT_ADLIB
        C64 = 1 << 4,
        Amiga = 1 << 5,
        AppleIIGS = 1 << 6,
        FMTowns = 1 << 7,
        // FM-TOWNS: Maps to MT_TOWNS
        PC98 = 1 << 8,
        // FM-TOWNS: Maps to MT_PC98
        Midi = 1 << 9,
        // Real MIDI
    }

    public abstract class MidiDriverBase : IMidiDriver
    {
        /// <summary>
        /// Output a packed midi command to the midi stream.
        /// The 'lowest' byte (i.e. b & 0xFF) is the status
        /// code, then come (if used) the first and second
        /// opcode.
        /// </summary>
        /// <param name="b">The blue component.</param>
        public abstract void Send(int b);

        /// <summary>
        /// Output a midi command to the midi stream. Convenience wrapper
        /// around the usual 'packed' send method.
        ///
        /// Do NOT use this for sysEx transmission; instead, use the sysEx()
        /// method below.
        /// </summary>
        /// <param name="status">Status.</param>
        /// <param name="firstOp">First op.</param>
        /// <param name="secondOp">Second op.</param>
        public virtual void Send(byte status, byte firstOp, byte secondOp)
        {
            Send(status | (firstOp << 8) | (secondOp << 16));
        }

        /// <summary>
        /// Transmit a sysEx to the midi device.
        ///
        /// The given msg MUST NOT contain the usual SysEx frame, i.e.
        /// do NOT include the leading 0xF0 and the trailing 0xF7.
        ///
        /// Furthermore, the maximal supported length of a SysEx
        /// is 264 bytes. Passing longer buffers can lead to
        /// undefined behavior (most likely, a crash).
        /// </summary>
        public virtual void SysEx(ByteAccess msg, ushort length)
        {
        }

        // TODO: Document this.
        public virtual void MetaEvent(byte type, ByteAccess data, ushort length)
        {
        }
    }

    public enum MidiDriverError
    {
        None = 0,
        CannotConnect = 1,
        //      MERR_STREAMING_NOT_AVAILABLE = 2,
        DeviceNotAvailable = 3,
        AlreadyOpen = 4
    }

    public abstract class MidiDriver : MidiDriverBase, IDisposable
    {
        /// <summary>
        /// Create music driver matching the given device handle, or NULL if there is no match.
        /// </summary>
        /// <returns>The midi.</returns>
        /// <param name = "mixer"></param>
        /// <param name="handle">Handle.</param>
        public static IMidiDriver CreateMidi(IMixer mixer, DeviceHandle handle)
        {
            IMidiDriver driver = null;
            var plugins = MusicManager.GetPlugins();
            foreach (var m in plugins)
            {
                if (GetDeviceString(handle, DeviceStringType.DriverId) == m.Id)
                    driver = m.CreateInstance(mixer, handle);
            }

            return driver;
        }

        /// <summary>
        /// Find the music driver matching the given driver name/description.
        /// </summary>
        /// <returns>The device handle.</returns>
        /// <param name="identifier">Identifier.</param>
        public static DeviceHandle GetDeviceHandle(string identifier)
        {
            var p = MusicManager.GetPlugins();

            if (p.Count == 0)
                throw new NotSupportedException("MidiDriver.GetDeviceHandle: Music plugins must be loaded prior to calling this method");

            foreach (var m in p)
            {
                var i = m.GetDevices();
                foreach (var d in i)
                {
                    // The music driver id isn't unique, but it will match
                    // driver's first device. This is useful when selecting
                    // the driver from the command line.
                    if (identifier.Equals(d.MusicDriverId) || identifier.Equals(d.CompleteId) || identifier.Equals(d.CompleteName))
                    {
                        return d.Handle;
                    }
                }
            }

            return DeviceHandle.Invalid;
        }

        /// <summary>
        /// Returns the device handle based on the present devices and the flags parameter.
        /// </summary>
        /// <returns>The device handle based on the present devices and the flags parameter.</returns>
        /// <param name="flags">Flags.</param>
        /// <param name = "selectedDevice">The selected device</param>
        public static DeviceHandle DetectDevice(MusicDriverTypes flags, string selectedDevice)
        {
            var result = new DeviceHandle();
            var handle = GetDeviceHandle(selectedDevice);
            var musicType = GetMusicType(handle);
            switch (musicType)
            {
                case MusicType.PCSpeaker:
                    if (flags.HasFlag(MusicDriverTypes.PCSpeaker))
                    {
                        result = handle;
                    }
                    break;
                case MusicType.PCjr:
                    if (flags.HasFlag(MusicDriverTypes.PCjr))
                    {
                        result = handle;
                    }
                    break;
                case MusicType.AdLib:
                    if (flags.HasFlag(MusicDriverTypes.AdLib))
                    {
                        result = handle;
                    }
                    break;
                case MusicType.CMS:
                    if (flags.HasFlag(MusicDriverTypes.CMS))
                    {
                        result = handle;
                    }
                    break;
                case MusicType.FMTowns:
                    if (flags.HasFlag(MusicDriverTypes.FMTowns))
                    {
                        result = handle;
                    }
                    break;
                case MusicType.Null:
                    result = handle;
                    break;
            }

            if (!result.IsValid)
            {
                MusicType mt = MusicType.Null;
                if (flags.HasFlag(MusicDriverTypes.FMTowns))
                {
                    mt = MusicType.FMTowns;
                }
                else if (flags.HasFlag(MusicDriverTypes.AdLib))
                {
                    mt = MusicType.AdLib;
                }
                else if (flags.HasFlag(MusicDriverTypes.PCjr))
                {
                    mt = MusicType.PCjr;
                }
                else if (flags.HasFlag(MusicDriverTypes.PCSpeaker))
                {
                    mt = MusicType.PCSpeaker;
                }
                var device = MusicManager.GetPlugins().SelectMany(p => p.GetDevices()).FirstOrDefault(d => d.MusicType == mt);
                if (device != null)
                {
                    result = device.Handle;
                }
            }

            return result;
        }

        public static MusicType GetMusicType(DeviceHandle handle)
        {
            var musicType = MusicType.Invalid;
            var device = MusicManager.GetPlugins().SelectMany(p => p.GetDevices()).FirstOrDefault(d => Equals(d.Handle, handle));
            if (device != null)
            {
                musicType = device.MusicType;
            }
            return musicType;
        }

        /// <summary>
        /// Gets the device description string matching the given device handle and the given type.
        /// </summary>
        /// <returns>The device string.</returns>
        /// <param name="handle">Handle.</param>
        /// <param name="type">Type.</param>
        static string GetDeviceString(DeviceHandle handle, DeviceStringType type)
        {
            if (handle.IsValid)
            {
                var p = MusicManager.GetPlugins();
                foreach (var m in p)
                {
                    var devices = m.GetDevices();
                    foreach (var d in devices)
                    {
                        if (Equals(handle, d.Handle))
                        {
                            if (type == DeviceStringType.DriverName)
                                return d.MusicDriverName;
                            else if (type == DeviceStringType.DriverId)
                                return d.MusicDriverId;
                            else if (type == DeviceStringType.DeviceName)
                                return d.CompleteName;
                            else if (type == DeviceStringType.DeviceId)
                                return d.CompleteId;
                            else
                                return "auto";
                        }
                    }
                }
            }

            return "auto";
        }

        /// <summary>
        /// Open the midi driver.
        /// </summary>
        /// <returns>0 if successful, otherwise an error code.</returns>
        public abstract MidiDriverError Open();

        /// <summary>
        /// Get or set a property.
        /// </summary>
        /// <param name="prop">Property.</param>
        /// <param name="param">Parameter.</param>
        public abstract int Property(int prop, int param);

        /// <summary>
        /// Gets a text representation of an error code.
        /// </summary>
        /// <returns>The error name.</returns>
        /// <param name="errorCode">Error code.</param>
        public static string GetErrorName(MidiDriverError errorCode)
        {
            return errorCode.ToString();
        }

        public virtual void SysExCustomInstrument(byte channel, uint type, byte[] instr)
        {
        }

        public delegate void TimerProc(object param);

        // Timing functions - MidiDriver now operates timers
        public abstract void SetTimerCallback(object timerParam, TimerProc timerProc);

        /// <summary>
        /// Gets the time in microseconds between invocations of the timer callback.
        /// </summary>
        /// <value>The time in microseconds between invocations of the timer callback.</value>
        public abstract uint BaseTempo { get; }

        public abstract MidiChannel AllocateChannel();

        public abstract MidiChannel GetPercussionChannel();

        public void SendMt32Reset()
        {
            byte[] resetSysEx = { 0x41, 0x10, 0x16, 0x12, 0x7F, 0x00, 0x00, 0x01, 0x00 };
            SysEx(new ByteAccess(resetSysEx), (ushort)resetSysEx.Length);
            ServiceLocator.Platform.Sleep(100);
        }

        public void SendGmReset()
        {
            byte[] resetSysEx = { 0x7E, 0x7F, 0x09, 0x01 };
            SysEx(new ByteAccess(resetSysEx), (ushort)resetSysEx.Length);
            ServiceLocator.Platform.Sleep(100);
        }

        public virtual void Dispose() { }

        public static readonly byte[] Mt32ToGm = {
        //	  0    1    2    3    4    5    6    7    8    9    A    B    C    D    E    F
	            0,   1,   0,   2,   4,   4,   5,   3,  16,  17,  18,  16,  16,  19,  20,  21, // 0x
	            6,   6,   6,   7,   7,   7,   8, 112,  62,  62,  63,  63,  38,  38,  39,  39, // 1x
	            88,  95,  52,  98,  97,  99,  14,  54, 102,  96,  53, 102,  81, 100,  14,  80, // 2x
	            48,  48,  49,  45,  41,  40,  42,  42,  43,  46,  45,  24,  25,  28,  27, 104, // 3x
	            32,  32,  34,  33,  36,  37,  35,  35,  79,  73,  72,  72,  74,  75,  64,  65, // 4x
	            66,  67,  71,  71,  68,  69,  70,  22,  56,  59,  57,  57,  60,  60,  58,  61, // 5x
	            61,  11,  11,  98,  14,   9,  14,  13,  12, 107, 107,  77,  78,  78,  76,  76, // 6x
	            47, 117, 127, 118, 118, 116, 115, 119, 115, 112,  55, 124, 123,   0,  14, 117  // 7x
        };
    }
}

