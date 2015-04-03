//
//  MusicDevice.cs
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
    /// <summary>
    /// Description of a Music device. Used to list the devices a Music driver
    /// can manage and their capabilities.
    /// A device with an empty name means the default device.
    /// </summary>
    public class MusicDevice
    {
        public MusicDevice(IMusicPluginObject musicPlugin, string name, MusicType mt)
        {
            _musicDriverName = musicPlugin.Name;
            _musicDriverId = musicPlugin.Id;
            _name = name;
            _type = mt;
            Handle = new DeviceHandle(CompleteId.GetHashCode());
        }

        public string Name { get { return _name; } }

        public string MusicDriverName { get { return _musicDriverName; } }

        public string MusicDriverId { get { return _musicDriverId; } }

        public MusicType MusicType { get { return _type; } }

        /// <summary>
        /// Gets a user readable string that contains the name of the current
        /// device name (if it isn't the default one) and the name of the driver.
        /// </summary>
        /// <value>The name of the device name and the name of the driver.</value>
        public string CompleteName
        {
            get
            {
                string name;
                if (string.IsNullOrEmpty(_name))
                {
                    // Default device, just show the driver name
                    name = _musicDriverName;
                }
                else
                {
                    // Show both device and driver names
                    name = string.Format("{0} [{1}]", _name, _musicDriverName);
                }
                return name;
            }
        }

        /// <summary>
        /// Gets a user readable string that contains the name of the current
        /// device name (if it isn't the default one) and the id of the driver.
        /// </summary>
        /// <value>The complete identifier.</value>
        public string CompleteId
        {
            get
            {
                string id = _musicDriverId;
                if (!string.IsNullOrEmpty(id))
                {
                    id += "_";
                    id += _name;
                }
                return id;
            }
        }

        public DeviceHandle Handle { get; private set; }

        readonly string _name;
        readonly string _musicDriverId;
        readonly string _musicDriverName;
        readonly MusicType _type;
    }
}

