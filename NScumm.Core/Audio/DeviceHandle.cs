//
//  DeviceHandle.cs
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


namespace NScumm.Core.Audio
{
    public struct DeviceHandle
    {
        readonly int handle;
        static readonly DeviceHandle _invalid = new DeviceHandle(0);

        /// <summary>
        /// Initializes a new instance of the <see cref="NScumm.Core.DeviceHandle"/> struct.
        /// </summary>
        /// <param name="handle">Handle.</param>
        /// <description>
        /// The value 0 is reserved for an invalid device for now.
        /// TODO: Maybe we should use -1 (i.e. 0xFFFFFFFF) as invalid device?
        /// </description>
        public DeviceHandle(int handle)
            : this()
        {
            this.handle = handle;
        }

        public bool IsValid { get { return handle != 0; } }

        public static DeviceHandle Invalid
        {
            get{ return _invalid; }
        }
    }
    
}
