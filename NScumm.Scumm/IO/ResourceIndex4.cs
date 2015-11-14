/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */


namespace NScumm.Scumm.IO
{
    class ResourceIndex4: ResourceIndex3
    {
        protected override byte GetEncodingByte(GameInfo game)
        {
            byte encByte = 0;
            if (!game.Features.HasFlag(GameFeatures.Old256))
            {
                encByte = 0;
            }
            return encByte;
        }
    }
}
