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
using System.Collections.Generic;
using NScumm.Core.Audio.Midi;

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
    enum MusicType
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

    abstract class MidiDriverBase : IMidiDriver
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
        public virtual void SysEx(byte[] msg, ushort length)
        {
        }

        // TODO: Document this.
        public virtual void MetaEvent(byte type, byte[] data, ushort length)
        {
        }
    }

    enum MidiDriverError
    {
        None = 0,
        CannotConnect = 1,
        //      MERR_STREAMING_NOT_AVAILABLE = 2,
        DeviceNotAvailable = 3,
        AlreadyOpen = 4
    }

    abstract class MidiDriver: MidiDriverBase
    {
        /// <summary>
        /// Create music driver matching the given device handle, or NULL if there is no match.
        /// </summary>
        /// <returns>The midi.</returns>
        /// <param name = "mixer"></param>
        /// <param name="handle">Handle.</param>
        public static MidiDriver CreateMidi(IMixer mixer, DeviceHandle handle)
        {
            MidiDriver driver = null;
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
        public static DeviceHandle DetectDevice(int flags)
        {
            // TODO:
            return GetDeviceHandle("adlib");
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
    }
}

