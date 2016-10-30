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

using System;
using System.IO;
using NScumm.Core;

namespace NScumm.Sci
{
    /// <summary>
    /// LZW-like decompressor for SCI01/SCI1.
    /// </summary>
    /// <remarks>TODO: Needs clean-up of post-processing fncs</remarks>
    internal class DecompressorLzw : Decompressor
    {
        private const int PAL_SIZE = 1284;
        private const int EXTRA_MAGIC_SIZE = 15;
        private const int VIEW_HEADER_COLORS_8BIT = 0x80;

        private ushort _numbits;
        private ushort _curtoken, _endtoken;
        private ResourceCompression _compression;

        // decompressor data
        private class Tokenlist
        {
            public byte data;
            public ushort next;
        }

        public DecompressorLzw(ResourceCompression nCompression)
        {
            _compression = nCompression;
        }

        public override ResourceErrorCodes Unpack(Stream src, BytePtr dest, int nPacked, int nUnpacked)
        {
            byte[] buffer = null;

            switch (_compression)
            {
                case ResourceCompression.LZW:  // SCI0 LZW compression
                    return UnpackLZW(src, dest, nPacked, nUnpacked);
                case ResourceCompression.LZW1: // SCI01/1 LZW compression
                    return UnpackLZW1(src, dest, nPacked, nUnpacked);
                case ResourceCompression.LZW1View:
                    buffer = new byte[nUnpacked];
                    UnpackLZW1(src, buffer, nPacked, nUnpacked);
                    ReorderView(buffer, dest);
                    break;
                case ResourceCompression.LZW1Pic:
                    buffer = new byte[nUnpacked];
                    UnpackLZW1(src, buffer, nPacked, nUnpacked);
                    ReorderPic(buffer, dest, nUnpacked);
                    break;
            }

            return ResourceErrorCodes.NONE;
        }

        private void ReorderPic(byte[] src, BytePtr dest, int dsize)
        {
            ushort view_size, view_start, cdata_size;
            int i;
            var seeker = new ByteAccess(src);
            var writer = new ByteAccess(dest);

            writer.Value = PicOpOpx; writer.Offset++;
            writer.Value = PicOpxSetPalette; writer.Offset++;

            for (i = 0; i < 256; i++) /* Palette translation map */
            {
                writer.Value = (byte)i;
                writer.Offset++;
            }

            writer.WriteUInt32(0, 0); /* Palette stamp */
            writer.Offset += 4;

            view_size = seeker.ToUInt16();
            seeker.Offset += 2;
            view_start = seeker.ToUInt16();
            seeker.Offset += 2;
            cdata_size = seeker.ToUInt16();
            seeker.Offset += 2;

            var viewdata = new byte[7];
            seeker.CopyTo(viewdata, 0, viewdata.Length);
            seeker.Offset += viewdata.Length;

            seeker.CopyTo(writer, 4 * 256);/* Palette */
            seeker.Offset += 4 * 256;
            writer.Offset += 4 * 256;

            if (view_start != PAL_SIZE + 2)
            { /* +2 for the opcode */
                seeker.CopyTo(writer, view_start - PAL_SIZE - 2);
                seeker.Offset += view_start - PAL_SIZE - 2;
                writer.Offset += view_start - PAL_SIZE - 2;
            }

            if (dsize != view_start + EXTRA_MAGIC_SIZE + view_size)
            {
                seeker.CopyTo(dest, view_size + view_start + EXTRA_MAGIC_SIZE, dsize - view_size - view_start - EXTRA_MAGIC_SIZE);
                seeker.Offset += dsize - view_size - view_start - EXTRA_MAGIC_SIZE;
            }

            byte[] cdata = new byte[cdata_size];
            seeker.CopyTo(cdata, 0, cdata_size);
            seeker.Offset += cdata_size;

            writer = new ByteAccess(dest, view_start);
            writer.Value = PicOpOpx; writer.Offset++;
            writer.Value = PicOpxEmbeddedView; writer.Offset++;
            writer.Value = 0; writer.Offset++;
            writer.Value = 0; writer.Offset++;
            writer.Value = 0; writer.Offset++;
            writer.WriteUInt16(0, (ushort)(view_size + 8));
            writer.Offset += 2;

            writer.CopyFrom(viewdata, 0, viewdata.Length);
            writer.Offset += viewdata.Length;

            writer.Value = 0; writer.Offset++;

            DecodeRLE(seeker, new ByteAccess(cdata), writer, view_size);
        }

        private void ReorderView(byte[] src, BytePtr dest)
        {
            var seeker = new ByteAccess(src);
            var writer = new ByteAccess(dest);
            int l, lb, c, celindex, lh_last = -1;
            int chptr;
            int w;

            /* Parse the main header */
            var cellengths = new ByteAccess(src, seeker.ToUInt16() + 2);
            seeker.Offset += 2;
            int loopheaders = seeker.Increment();
            int lh_present = seeker.Increment();
            int lh_mask = seeker.ToUInt16();
            seeker.Offset += 2;
            int unknown = seeker.ToUInt16();
            seeker.Offset += 2;
            int pal_offset = seeker.ToUInt16();
            seeker.Offset += 2;
            int cel_total = seeker.ToUInt16();
            seeker.Offset += 2;

            var cc_pos = new ByteAccess[cel_total];
            var cc_lengths = new int[cel_total];

            for (c = 0; c < cel_total; c++)
                cc_lengths[c] = cellengths.ToUInt16(2 * c);

            writer.Value = (byte)loopheaders; writer.Offset++;
            writer.Value = VIEW_HEADER_COLORS_8BIT; writer.Offset++;
            writer.WriteUInt16(0, (ushort)lh_mask);
            writer.Offset += 2;
            writer.WriteUInt16(0, (ushort)unknown);
            writer.Offset += 2;
            writer.WriteUInt16(0, (ushort)pal_offset);
            writer.Offset += 2;

            var lh_ptr = new ByteAccess(writer);
            writer.Offset += 2 * loopheaders; /* Make room for the loop offset table */

            var pix_ptr = new ByteAccess(writer);

            byte[] celcounts = new byte[100];
            Array.Copy(seeker.Data, seeker.Offset, celcounts, 0, lh_present);
            seeker.Offset += lh_present;

            lb = 1;
            celindex = 0;

            pix_ptr = new ByteAccess(cellengths, (2 * cel_total));
            var rle_ptr = new ByteAccess(pix_ptr);
            w = 0;

            for (l = 0; l < loopheaders; l++)
            {
                if ((lh_mask & lb) != 0)
                { /* The loop is _not_ present */
                    if (lh_last == -1)
                    {
                        DebugHelper.Warning("Error: While reordering view: Loop not present, but can't re-use last loop");
                        lh_last = 0;
                    }
                    lh_ptr.WriteUInt16(0, (ushort)lh_last);
                    lh_ptr.Offset += 2;
                }
                else
                {
                    lh_last = writer.Offset;
                    lh_ptr.WriteUInt16(0, (ushort)lh_last);
                    lh_ptr.Offset += 2;
                    writer.WriteUInt16(0, celcounts[w]);
                    writer.Offset += 2;
                    writer.WriteUInt16(0, 0);
                    writer.Offset += 2;

                    /* Now, build the cel offset table */
                    chptr = writer.Offset + (2 * celcounts[w]);

                    for (c = 0; c < celcounts[w]; c++)
                    {
                        writer.WriteUInt16(0, (ushort)chptr);
                        writer.Offset += 2;
                        cc_pos[celindex + c] = new ByteAccess(dest, chptr);
                        chptr += 8 + cellengths.ToUInt16(2 * (celindex + c));
                    }

                    BuildCelHeaders(seeker, writer, celindex, cc_lengths, celcounts[w]);

                    celindex += celcounts[w];
                    w++;
                }

                lb = lb << 1;
            }

            if (celindex < cel_total)
            {
                DebugHelper.Warning("View decompression generated too few (%d / %d) headers", celindex, cel_total);
                return;
            }

            /* Figure out where the pixel data begins. */
            for (c = 0; c < cel_total; c++)
                pix_ptr.Offset += GetRLEsize(pix_ptr.Data, pix_ptr.Offset, cc_lengths[c]);

            rle_ptr = new ByteAccess(cellengths, (2 * cel_total));
            for (c = 0; c < cel_total; c++)
                DecodeRLE(rle_ptr, pix_ptr, new ByteAccess(cc_pos[c], 8), cc_lengths[c]);

            if (pal_offset != 0)
            {
                writer.Value = (byte)'P'; writer.Offset++;
                writer.Value = (byte)'A'; writer.Offset++;
                writer.Value = (byte)'L'; writer.Offset++;

                for (c = 0; c < 256; c++)
                {
                    writer.Value = (byte)c;
                    writer.Offset++;
                }

                seeker.Offset -= 4; /* The missing four. Don't ask why. */
                Array.Copy(seeker.Data, seeker.Offset, writer.Data, writer.Offset, 4 * 256 + 4);
            }
        }

        private void DecodeRLE(ByteAccess rledata, ByteAccess pixeldata, ByteAccess outbuffer, int size)
        {
            int pos = 0;
            byte nextbyte;
            var rd = rledata;
            var ob = new ByteAccess(outbuffer);
            var pd = pixeldata;

            while (pos < size)
            {
                nextbyte = rd.Increment();
                ob.Value = nextbyte; ob.Offset++;
                pos++;
                switch (nextbyte & 0xC0)
                {
                    case 0x40:
                    case 0x00:
                        pd.CopyTo(ob, nextbyte);
                        pd.Offset += nextbyte;
                        ob.Offset += nextbyte;
                        pos += nextbyte;
                        break;
                    case 0xC0:
                        break;
                    case 0x80:
                        nextbyte = pd.Increment();
                        ob.Value = nextbyte; ob.Offset++;
                        pos++;
                        break;
                }
            }
        }

        private void BuildCelHeaders(ByteAccess seeker, ByteAccess writer, int celindex, int[] cc_lengths, int max)
        {
            for (int c = 0; c < max; c++)
            {
                Array.Copy(seeker.Data, seeker.Offset, writer.Data, writer.Offset, 6);
                seeker.Offset += 6;
                writer.Offset += 6;
                seeker.Offset++;
                ushort w = seeker.Value;
                writer.WriteUInt16(0, w); /* Zero extension */
                writer.Offset += 2;

                writer.Offset += cc_lengths[celindex];
                celindex++;
            }
        }

        /// <summary>
        /// Does the same this as decodeRLE, only to determine the length of the
        /// compressed source data.
        /// </summary>
        /// <returns>The RLE size.</returns>
        /// <param name="rledata">Rledata.</param>
        /// <param name="dsize">Dsize.</param>
        private int GetRLEsize(byte[] rledata, int offset, int dsize)
        {
            int pos = 0;
            byte nextbyte;
            int size = 0;

            while (pos < dsize)
            {
                nextbyte = rledata[offset++];
                pos++;
                size++;

                switch (nextbyte & 0xC0)
                {
                    case 0x40:
                    case 0x00:
                        pos += nextbyte;
                        break;
                    case 0xC0:
                        break;
                    case 0x80:
                        pos++;
                        break;
                }
            }

            return size;
        }

        private ResourceErrorCodes UnpackLZW1(Stream src, BytePtr dest, int nPacked, int nUnpacked)
        {
            Init(src, dest, nPacked, nUnpacked);

            byte[] stak = new byte[0x1014];
            var tokens = new Tokenlist[0x10004];

            byte lastchar = 0;
            ushort stakptr = 0, lastbits = 0;

            byte decryptstart = 0;
            ushort bitstring;
            ushort token;
            bool bExit = false;

            while (!IsFinished && !bExit)
            {
                switch (decryptstart)
                {
                    case 0:
                        bitstring = (ushort)GetBitsMsb(_numbits);
                        if (bitstring == 0x101)
                        {// found end-of-data signal
                            bExit = true;
                            continue;
                        }
                        PutByte((byte)bitstring);
                        lastbits = bitstring;
                        lastchar = (byte)(bitstring & 0xff);
                        decryptstart = 1;
                        break;

                    case 1:
                        bitstring = (ushort)GetBitsMsb(_numbits);
                        if (bitstring == 0x101)
                        { // found end-of-data signal
                            bExit = true;
                            continue;
                        }
                        if (bitstring == 0x100)
                        { // start-over signal
                            _numbits = 9;
                            _curtoken = 0x102;
                            _endtoken = 0x1ff;
                            decryptstart = 0;
                            continue;
                        }

                        token = bitstring;
                        if (token >= _curtoken)
                        { // index past current point
                            token = lastbits;
                            stak[stakptr++] = lastchar;
                        }
                        while ((token > 0xff) && (token < 0x1004))
                        { // follow links back in data
                            stak[stakptr++] = tokens[token].data;
                            token = tokens[token].next;
                        }
                        lastchar = stak[stakptr++] = (byte)(token & 0xff);
                        // put stack in buffer
                        while (stakptr > 0)
                        {
                            PutByte(stak[--stakptr]);
                            if (DwWrote == SzUnpacked)
                            {
                                bExit = true;
                                continue;
                            }
                        }
                        // put token into record
                        if (_curtoken <= _endtoken)
                        {
                            tokens[_curtoken] = new Tokenlist();
                            tokens[_curtoken].data = lastchar;
                            tokens[_curtoken].next = lastbits;
                            _curtoken++;
                            if (_curtoken == _endtoken && _numbits < 12)
                            {
                                _numbits++;
                                _endtoken = (ushort)((_endtoken << 1) + 1);
                            }
                        }
                        lastbits = bitstring;
                        break;
                }
            }

            return DwWrote == SzUnpacked ? 0 : ResourceErrorCodes.DECOMPRESSION_ERROR;
        }

        private ResourceErrorCodes UnpackLZW(Stream src, BytePtr dest, int nPacked, int nUnpacked)
        {
            Init(src, dest, nPacked, nUnpacked);

            ushort token; // The last received value
            ushort tokenlastlength = 0;

            var tokenlist = new ushort[4096]; // pointers to dest[]
            var tokenlengthlist = new ushort[4096]; // char length of each token

            while (!IsFinished)
            {
                token = (ushort)GetBitsLsb(_numbits);

                if (token == 0x101)
                {
                    return 0; // terminator
                }

                if (token == 0x100)
                { // reset command
                    _numbits = 9;
                    _endtoken = 0x1FF;
                    _curtoken = 0x0102;
                }
                else
                {
                    if (token > 0xff)
                    {
                        if (token >= _curtoken)
                        {
                            DebugHelper.Warning($"unpackLZW: Bad token {token:X}");
                            return ResourceErrorCodes.DECOMPRESSION_ERROR;
                        }
                        tokenlastlength = (ushort)(tokenlengthlist[token] + 1);
                        if (DwWrote + tokenlastlength > SzUnpacked)
                        {
                            // For me this seems a normal situation, It's necessary to handle it
                            DebugHelper.Warning($"unpackLZW: Trying to write beyond the end of array(len={SzUnpacked}, destctr={DwWrote}, tok_len={tokenlastlength})");
                            for (int i = 0; DwWrote < SzUnpacked; i++)
                                PutByte(dest[tokenlist[token] + i]);
                        }
                        else
                            for (int i = 0; i < tokenlastlength; i++)
                                PutByte(dest[tokenlist[token] + i]);
                    }
                    else
                    {
                        tokenlastlength = 1;
                        if (DwWrote >= SzUnpacked)
                        {
                            DebugHelper.Warning("unpackLZW: Try to write single byte beyond end of array");
                        }
                        else
                            PutByte((byte)token);
                    }
                    if (_curtoken > _endtoken && _numbits < 12)
                    {
                        _numbits++;
                        _endtoken = (ushort)((_endtoken << 1) + 1);
                    }
                    if (_curtoken <= _endtoken)
                    {
                        tokenlist[_curtoken] = (ushort)(DwWrote - tokenlastlength);
                        tokenlengthlist[_curtoken] = tokenlastlength;
                        _curtoken++;
                    }

                }
            }

            return DwWrote == SzUnpacked ? 0 : ResourceErrorCodes.DECOMPRESSION_ERROR;
        }

        protected override void Init(Stream src, BytePtr dest, int nPacked, int nUnpacked)
        {
            base.Init(src, dest, nPacked, nUnpacked);

            _numbits = 9;
            _curtoken = 0x102;
            _endtoken = 0x1ff;
        }
    }
}