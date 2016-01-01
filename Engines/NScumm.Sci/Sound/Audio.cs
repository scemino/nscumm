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

using NScumm.Core.Audio;

namespace NScumm.Sci.Sound
{
    internal class AudioPlayer
    {
        private int _audioRate;
        private IMixer _mixer;
        private ResourceManager _resMan;
        private bool _wPlayFlag;

        public AudioPlayer(ResourceManager resMan)
        {
            _resMan = resMan;
            _audioRate = 11025;

            // TODO: _mixer = g_system->getMixer();
            _wPlayFlag = false;
        }
    }
}
