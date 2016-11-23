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

using System.IO;

namespace NScumm.Core.Audio.Decoders
{
    public class XAStream : IRewindableAudioStream
    {
        private BinaryReader _stream;
        private bool _disposeAfterUse;
        private byte _predictor;
        private double[] _samples = new double[28];
        private byte _samplesRemaining;
        private int _rate;
        private double _s1, _s2;
        private uint _loopPoint;
        private bool _endOfData;

        static readonly double[,] s_xaDataTable = {
            {  0.0, 0.0 },
            {  60.0 / 64.0,  0.0 },
            {  115.0 / 64.0, -52.0 / 64.0 },
            {  98.0 / 64.0, -55.0 / 64.0 },
            {  122.0 / 64.0, -60.0 / 64.0 }
        };

        public XAStream(Stream stream, int rate, bool disposeAfterUse = true)
        {
            _stream = new BinaryReader(stream);
            _disposeAfterUse = disposeAfterUse;
            _samplesRemaining = 0;
            _predictor = 0;
            _s1 = _s2 = 0.0;
            _rate = rate;
            _loopPoint = 0;
            _endOfData = false;
        }

        public bool IsEndOfData
        {
            get { return _endOfData && _samplesRemaining == 0; }
        }

        public bool IsEndOfStream
        {
            get { return IsEndOfData; }
        }

        public bool IsStereo
        {
            get { return false; }
        }

        public int Rate
        {
            get { return _rate; }
        }


        public void Dispose()
        {
            if (_disposeAfterUse)
            {
                _stream.Dispose();
            }
        }

        public int ReadBuffer(Ptr<short> buffer, int numSamples)
        {
            int samplesDecoded = 0;

            for (int i = 28 - _samplesRemaining; i < 28 && samplesDecoded < numSamples; i++)
            {
                _samples[i] = _samples[i] + _s1 * s_xaDataTable[_predictor, 0] + _s2 * s_xaDataTable[_predictor, 1];
                _s2 = _s1;
                _s1 = _samples[i];
                short d = (short)(_samples[i] + 0.5);
                buffer[samplesDecoded] = d;
                samplesDecoded++;
                _samplesRemaining--;
            }

            if (IsEndOfData)
                return samplesDecoded;

            while (!_endOfData && samplesDecoded < numSamples)
            {
                byte i = 0;

                _predictor = _stream.ReadByte();
                byte shift = (byte)(_predictor & 0xf);
                _predictor >>= 4;

                byte flags = _stream.ReadByte();
                if (flags == 3)
                {
                    // Loop
                    SeekToPos(_loopPoint);
                    continue;
                }
                else if (flags == 6)
                {
                    // Set loop point
                    _loopPoint = (uint)(_stream.BaseStream.Position - 2);
                }
                else if (flags == 7)
                {
                    // End of stream
                    _endOfData = true;
                    return samplesDecoded;
                }

                for (i = 0; i < 28; i += 2)
                {
                    byte d = _stream.ReadByte();
                    int s = (d & 0xf) << 12;
                    if ((s & 0x8000) != 0)
                        s = (int)(s | 0xffff0000);
                    _samples[i] = (s >> shift);
                    s = (d & 0xf0) << 8;
                    if ((s & 0x8000) != 0)
                        s = (int)(s | 0xffff0000);
                    _samples[i + 1] = (s >> shift);
                }

                for (i = 0; i < 28 && samplesDecoded < numSamples; i++)
                {
                    _samples[i] = _samples[i] + _s1 * s_xaDataTable[_predictor, 0] + _s2 * s_xaDataTable[_predictor, 1];
                    _s2 = _s1;
                    _s1 = _samples[i];
                    short d = (short)(_samples[i] + 0.5);
                    buffer[samplesDecoded] = d;
                    samplesDecoded++;
                }

                if (i != 28)
                    _samplesRemaining = (byte)(28 - i);

                if (_stream.BaseStream.Position >= _stream.BaseStream.Length)
                    _endOfData = true;
            }

            return samplesDecoded;
        }

        public bool Rewind()
        {
            SeekToPos(0);
            return true;
        }

        private void SeekToPos(uint pos)
        {
            _stream.BaseStream.Seek(pos, SeekOrigin.Begin);
            _samplesRemaining = 0;
            _predictor = 0;
            _s1 = _s2 = 0.0;
            _endOfData = false;
        }
    }
}
