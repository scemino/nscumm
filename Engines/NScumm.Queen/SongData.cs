//
//  SongData.cs
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

namespace NScumm.Queen
{
    public class SongData
    {
        public short[] tuneList=new short[5];
        public short volume;
        public short tempo;
        public short reverb;
        public short overrideCmd;
        public short ignore;

        public SongData(short[] tuneList, short volume,short tempo,short reverb,short overrideCmd, short ignore)
        {
            this.tuneList = tuneList;
            this.volume = volume;
            this.tempo = tempo;
            this.reverb = reverb;
            this.overrideCmd = overrideCmd;
            this.ignore = ignore;
        }
    }
    
}
