//
//  DisabledChannel.cs
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

namespace NScumm.Core.Audio.OPL
{
    // There's just one instance of this class, that fills the eventual gaps in the Channel array;
    class DisabledChannel : Channel
    {
        internal DisabledChannel()
            : base(0)
        {
        }

        public override double[] getChannelOutput()
        {
            return getInFourChannels(0);
        }

        protected override void keyOn()
        {
        }

        protected override void keyOff()
        {
        }

        protected override void updateOperators()
        {
        }
    }
}

