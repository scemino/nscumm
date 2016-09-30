//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci
{
    /// <summary>
    /// STACpack decompressor for SCI32
    /// </summary>
    internal class DecompressorLzs: Decompressor
    {
        //----------------------------------------------
        // STACpack/LZS decompressor for SCI32
        // Based on Andre Beck's code from http://micky.ibh.de/~beck/stuff/lzs4i4l/
        //----------------------------------------------
        public override ResourceErrorCodes Unpack(Stream src, byte[] dest, int nPacked, int nUnpacked)
        {
            Init(src, dest, nPacked, nUnpacked);
            return UnpackLzs();
        }

        private ResourceErrorCodes UnpackLzs()
        {
            ushort offs = 0;
            uint clen;

            while (!IsFinished)
            {
                if (GetBitsMsb(1)!=0)
                {
                    // Compressed bytes follow
                    if (GetBitsMsb(1)!=0)
                    {
                        // Seven bit offset follows
                        offs = (ushort) GetBitsMsb(7);
                        if (offs==0) // This is the end marker - a 7 bit offset of zero
                            break;
                        if ((clen = GetCompLen())==0)
                        {
                            Warning("lzsDecomp: length mismatch");
                            return ResourceErrorCodes.DECOMPRESSION_ERROR;
                        }
                        CopyComp(offs, clen);
                    }
                    else
                    {
                        // Eleven bit offset follows
                        offs = (ushort) GetBitsMsb(11);
                        if ((clen = GetCompLen())==0)
                        {
                            Warning("lzsDecomp: length mismatch");
                            return ResourceErrorCodes.DECOMPRESSION_ERROR;
                        }
                        CopyComp(offs, clen);
                    }
                }
                else // Literal byte follows
                    PutByte(GetByteMsb());
            } // end of while ()
            return DwWrote == SzUnpacked ? 0 : ResourceErrorCodes.DECOMPRESSION_ERROR;
        }

        private uint GetCompLen()
        {
            // The most probable cases are hardcoded
            switch (GetBitsMsb(2))
            {
                case 0:
                    return 2;
                case 1:
                    return 3;
                case 2:
                    return 4;
                default:
                    switch (GetBitsMsb(2))
                    {
                        case 0:
                            return 5;
                        case 1:
                            return 6;
                        case 2:
                            return 7;
                        default:
                            // Ok, no shortcuts anymore - just get nibbles and add up
                            uint clen = 8;
                            int nibble;
                            do
                            {
                                nibble = (int) GetBitsMsb(4);
                                clen = (uint) (clen+nibble);
                            } while (nibble == 0xf);
                            return clen;
                    }
            }
        }

        private void CopyComp(int offs, uint clen)
        {
            int hpos = DwWrote - offs;

            while ((clen--)!=0)
                PutByte(Dest[hpos++]);
        }

    }
}