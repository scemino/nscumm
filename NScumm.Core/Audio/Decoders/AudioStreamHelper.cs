//
//  AudioStreamHelper.cs
//
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

namespace NScumm.Core.Audio.Decoders
{
    public static class AudioStreamHelper
    {
        public static Timestamp ConvertTimeToStreamPos(Timestamp where, int rate, bool isStereo)
        {
            var result = new Timestamp(where.ConvertToFramerate(rate * (isStereo ? 2 : 1)));

            // When the Stream is a stereo stream, we have to assure
            // that the sample position is an even number.
            if (isStereo && (result.TotalNumberOfFrames & 1) != 0)
                result = result.AddFrames(-1); // We cut off one sample here.

            // Since Timestamp allows sub-frame-precision it might lead to odd behaviors
            // when we would just return result.
            //
            // An example is when converting the timestamp 500ms to a 11025 Hz based
            // stream. It would have an internal frame counter of 5512.5. Now when
            // doing calculations at frame precision, this might lead to unexpected
            // results: The frame difference between a timestamp 1000ms and the above
            // mentioned timestamp (both with 11025 as framerate) would be 5512,
            // instead of 5513, which is what a frame-precision based code would expect.
            //
            // By creating a new Timestamp with the given parameters, we create a
            // Timestamp with frame-precision, which just drops a sub-frame-precision
            // information (i.e. rounds down).
            return new Timestamp(result.Seconds, result.NumberOfFrames, result.Framerate);
        }
    }
}
