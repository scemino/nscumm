//
//  AGOSEngine.String.cs
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

using System;
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private void UncompressText(BytePtr ptr)
        {
            while (true)
            {
                byte a;
                if (_awaitTwoByteToken != 0)
                {
                    a = (byte) _awaitTwoByteToken;
                }
                else
                {
                    a = ptr.Value;
                    ptr.Offset++;
                }
                if (a == 0)
                    return;
                ptr = UncompressToken(a, ptr);
                if (ptr.Value == 0)
                    return;
            }
        }

        private BytePtr UncompressToken(byte a, BytePtr ptr)
        {
            BytePtr ptr1;
            BytePtr ptr2 = BytePtr.Null;
            int count1 = 0;

            if (a == 0xFF || a == 0xFE || a == 0xFD)
            {
                if (a == 0xFF)
                    ptr2 = _twoByteTokenStrings;
                if (a == 0xFE)
                    ptr2 = _secondTwoByteTokenStrings;
                if (a == 0xFD)
                    ptr2 = _thirdTwoByteTokenStrings;
                _awaitTwoByteToken = a;
                var b = a;
                a = ptr.Value;
                ptr.Offset++;
                if (a == 0) /* Need to return such that next byte   */
                    return BytePtr.Null; /* is used as two byte token		*/

                _awaitTwoByteToken = 0;
                ptr1 = _twoByteTokens;
                while (ptr1.Value != a)
                {
                    ptr1.Offset++;
                    count1++;
                    if (ptr1.Value == 0)
                    {
                        /* If was not a two byte token  */
                        count1 = 0; /* then was a byte token.	*/
                        ptr1 = _byteTokens;
                        while (ptr1.Value != a)
                        {
                            ptr1.Offset++;
                            count1++;
                        }
                        ptr1 = _byteTokenStrings; /* Find it */
                        while (count1-- != 0)
                        {
                            while (ptr1.Value != 0)
                            {
                                ptr1.Offset++;
                            }
                        }
                        ptr1 = UncompressToken(b, ptr1); /* Try this one as a two byte token */
                        UncompressText(ptr1); /* Uncompress rest of this token    */
                        return ptr;
                    }
                }
                while (count1-- != 0)
                {
                    while (ptr2.Value != 0)
                    {
                        ptr2.Offset++;
                    }
                }
                UncompressText(ptr2);
            }
            else
            {
                ptr1 = _byteTokens;
                while (ptr1.Value != a)
                {
                    ptr1.Offset++;
                    count1++;
                    if (ptr1.Value == 0)
                    {
                        _textBuffer[_textCount++] = a; /* Not a byte token */
                        return ptr; /* must be real character */
                    }
                }
                ptr1 = _byteTokenStrings;
                while (count1-- != 0)
                {
                    /* Is a byte token so count */
                    while (ptr1.Value != 0)
                    {
                        ptr1.Offset++;
                    } /* to start of token */
                }
                UncompressText(ptr1); /* and do it */
            }
            return ptr;
        }

        protected string GetStringPtrById(ushort stringId, bool upperCase = false)
        {
            _freeStringSlot ^= 1;
            var dst = _stringReturnBuffer[_freeStringSlot];

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                _gd.Platform == Platform.AtariST)
            {
                var ptr = _stringTabPtr[stringId];
                _textCount = 0;
                _awaitTwoByteToken = 0;
                UncompressText(ptr);
                _textBuffer[_textCount] = 0;
                Array.Copy(_textBuffer, 0, dst, 0, 180);
            }
            else
            {
                BytePtr stringPtr;
                if (stringId < 0x8000)
                {
                    stringPtr = _stringTabPtr[stringId];
                }
                else
                {
                    stringPtr = GetLocalStringById(stringId);
                }
                var data = stringPtr.GetRawText(0, 180);
                var len = Math.Min(data.Length, 180);
                Array.Copy(data.GetBytes(), 0, dst, 0, len);
                dst[len] = 0;
            }

            // WORKAROUND bug #1538873: The French version of Simon 1 and the
            // Polish version of Simon 2 used excess spaces, at the end of many
            // messages, so we strip off those excess spaces.
            if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                 _language == Language.FR_FRA) ||
                (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                 _language == Language.PL_POL))
            {
                ushort len = (ushort) (dst.GetTextLength() - 1);

                while (len != 0 && dst[len] == 32)
                {
                    dst[len] = 0;
                    len--;
                }
            }

            if (upperCase && dst[0] != 0)
            {
                if (char.IsLower((char) dst[0]))
                    dst[0] = (byte) char.ToUpper((char) dst[0]);
            }

            return dst.GetRawText();
        }

        private BytePtr GetLocalStringById(ushort stringId)
        {
            if (stringId < _stringIdLocalMin || stringId >= _stringIdLocalMax)
            {
                LoadTextIntoMem(stringId);
            }
            return _localStringtable[stringId - _stringIdLocalMin];
        }

        protected void RenderString(int vgaSpriteId, uint color, ushort width, ushort height, byte[] txt)
        {
            var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, 2);
            int textHeight = _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                             _gd.ADGameDescription.gameType == SIMONGameType.GType_PP
                ? 15
                : 10;
            int count = 0;

            if (vgaSpriteId >= 100)
            {
                vgaSpriteId -= 100;
                vpe.Offset++;
            }

            BytePtr dst;
            var src = dst = vpe.Value.vgaFile2;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                if (vgaSpriteId == 1)
                    count = 45000;
            }
            else
            {
                count = 4000;
                if (vgaSpriteId == 1)
                    count *= 2;
            }

            var p = new BytePtr(dst, vgaSpriteId * 8);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                if (vgaSpriteId != 1)
                    p.WriteInt32(0, p.ToInt32(-8) + p.ToUInt16(-4) * p.ToUInt16(-2));

                p.WriteUInt16(4, height);
                p.WriteUInt16(6, width);
            }
            else
            {
                p.WriteUInt16BigEndian(4, height);
                p.WriteUInt16BigEndian(6, width);
            }
            dst += (int) ReadUint32Wrapper(p);

            if (count != 0)
                dst.Data.Set(dst.Offset, 0, count);

            if (_language == Language.HE_ISR)
                dst += width - 1; // For Hebrew, start at the right edge, not the left.

            var dstOrg = dst;
            foreach (byte t in txt)
            {
                if (t == 0) break;
                var chr = t;
                if (chr == 10)
                {
                    dstOrg += width * textHeight;
                    dst = dstOrg;
                }
                else if ((chr -= (byte) ' ') == 0)
                {
                    dst += _language == Language.HE_ISR ? -6 : 6; // Hebrew moves to the left, all others to the right
                }
                else
                {
                    BytePtr imgHdr, img;
                    int imgWidth, imgHeight;

                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                        _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                    {
                        imgHdr = src + 96 + chr * 8;
                        imgHeight = imgHdr.ToUInt16(4);
                        imgWidth = imgHdr.ToUInt16(6);
                        img = src + imgHdr.ToInt32();
                    }
                    else
                    {
                        imgHdr = src + 48 + chr * 4;
                        imgHeight = imgHdr[2];
                        imgWidth = imgHdr[3];
                        img = src + imgHdr.ToUInt16();
                    }

                    if (_language == Language.HE_ISR)
                        dst -= imgWidth - 1; // For Hebrew, move from right edge to left edge of image.
                    BytePtr curDst = dst;

                    // Occurs in Amiga and Macintosh ports of The Feeble Files, when
                    // special characters are used by French/German/Spanish versions.
                    // Due to the English image data, been used by all languages.
                    if (imgWidth == 0 || imgHeight == 0)
                        continue;

                    System.Diagnostics.Debug.Assert(imgWidth < 50 && imgHeight < 50);

                    do
                    {
                        for (var j = 0; j != imgWidth; j++)
                        {
                            chr = img.Value;
                            img.Offset++;
                            if (chr != 0)
                            {
                                if (chr == 0xF)
                                    chr = 207;
                                else
                                    chr = (byte) (chr + color);
                                curDst[j] = chr;
                            }
                        }
                        curDst.Offset += width;
                    } while (--imgHeight != 0);

                    if (_language != Language.HE_ISR) // Hebrew character movement is done higher up
                        dst.Offset += imgWidth - 1;
                }
            }
        }

        private void RenderStringAmiga(uint vgaSpriteId, uint color, uint width, uint height, string text)
        {
            var txt = text.GetBytes();
            var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, 2);
            int count;

            if (vgaSpriteId >= 100)
            {
                vgaSpriteId -= 100;
                vpe.Offset++;
            }

            var dst = vpe.Value.vgaFile2;

            count = 2000;
            if (vgaSpriteId == 1)
                count *= 2;

            var p = new BytePtr(dst, (int) (vgaSpriteId * 8));
            p.WriteUInt16BigEndian(4, (ushort) height);
            p.WriteUInt16BigEndian(6, (ushort) width);
            dst += p.ToInt32BigEndian();

            width /= 8; // convert width from pixels to bytes

            byte[] imgSrc = null;
            switch (_language)
            {
                case Language.IT_ITA:
                    imgSrc = CharsetFontData.italian_simon1AGAFontData;
                    break;
                case Language.FR_FRA:
                    imgSrc = CharsetFontData.french_simon1AGAFontData;
                    break;
                case Language.DE_DEU:
                    imgSrc = CharsetFontData.german_simon1AGAFontData;
                    break;
                case Language.EN_ANY:
                    imgSrc = CharsetFontData.english_simon1AGAFontData;
                    break;
                default:
                    Error("renderStringAmiga: Unknown language {0}", _language);
                    break;
            }


            int charsize = (int) (width * height);
            Array.Clear(dst.Data, dst.Offset, count);
            var dstOrg = dst;
            int delta = 0;
            var t = 0;
            while (t < txt.Length)
            {
                var chr = txt[t++];
                int imgWidth = 1;
                if (chr == 10)
                {
                    dst.Offset += (int) (width * 10);
                    dstOrg = dst;
                    delta = 0;
                }
                else if ((sbyte)(chr -= (byte)'!') < 0)
                {
                    imgWidth = 7;
                }
                else
                {
                    BytePtr img = new BytePtr(imgSrc, chr * 41);
                    imgWidth = img[40];
                    BytePtr curDst = dstOrg;
                    for (int row = 0; row < 10; row++)
                    {
                        int col = (int) color;
                        for (int plane = 0; plane < 3; plane++)
                        {
                            chr = (byte) (img[plane] >> delta);
                            if (chr != 0)
                            {
                                if ((col & 1) != 0) curDst[charsize * 0] = (byte) (curDst[charsize * 0] | chr);
                                if ((col & 2) != 0) curDst[charsize * 1] = (byte) (curDst[charsize * 1] | chr);
                                if ((col & 4) != 0) curDst[charsize * 2] = (byte) (curDst[charsize * 2] | chr);
                                if ((col & 8) != 0) curDst[charsize * 3] = (byte) (curDst[charsize * 3] | chr);
                            }
                            chr = (byte) (img[plane] << (8 - delta));
                            if (((8 - delta) < imgWidth) && (chr != 0))
                            {
                                if ((col & 1) != 0)
                                    curDst[charsize * 0 + 1] = (byte) (curDst[charsize * 0 + 1] | chr);
                                if ((col & 2) != 0)
                                    curDst[charsize * 1 + 1] = (byte) (curDst[charsize * 1 + 1] | chr);
                                if ((col & 4) != 0)
                                    curDst[charsize * 2 + 1] = (byte) (curDst[charsize * 2 + 1] | chr);
                                if ((col & 8) != 0)
                                    curDst[charsize * 3 + 1] = (byte) (curDst[charsize * 3 + 1] | chr);
                            }
                            col++;
                        }
                        chr = (byte) (img[3] >> delta);
                        if (chr != 0)
                        {
                            curDst[charsize * 0] = (byte) (curDst[charsize * 0] | chr);
                            curDst[charsize * 1] = (byte) (curDst[charsize * 1] | chr);
                            curDst[charsize * 2] = (byte) (curDst[charsize * 2] | chr);
                            curDst[charsize * 3] = (byte) (curDst[charsize * 3] | chr);
                        }
                        chr = (byte) (img[3] << (8 - delta));
                        if ((8 - delta < imgWidth) && (chr != 0))
                        {
                            curDst[charsize * 0 + 1] = (byte) (curDst[charsize * 0 + 1] | chr);
                            curDst[charsize * 1 + 1] = (byte) (curDst[charsize * 1 + 1] | chr);
                            curDst[charsize * 2 + 1] = (byte) (curDst[charsize * 2 + 1] | chr);
                            curDst[charsize * 3 + 1] = (byte) (curDst[charsize * 3 + 1] | chr);
                        }
                        curDst.Offset += (int) width;
                        img += 4;
                    }
                }
                delta += imgWidth - 1;
                if (delta >= 8)
                {
                    delta -= 8;
                    dstOrg.Offset++;
                }
            }
        }

        protected TextLocation GetTextLocation(uint a)
        {
            switch (a)
            {
                case 1:
                    return TextLocation1;
                case 2:
                    return TextLocation2;
                case 101:
                    return TextLocation3;
                case 102:
                    return TextLocation4;
                default:
                    Error("getTextLocation: Invalid text location {0}", a);
                    break;
            }
            return null; // for compilers that don't support NORETURN
        }

        private void AllocateStringTable(int num)
        {
            _stringTabPtr = new BytePtr[num];
            _stringTabPos = 0;
            _stringTabSize = num;
        }

        private void SetupStringTable(BytePtr mem, int num)
        {
            int i = 0;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                _gd.Platform == Platform.AtariST)
            {
                int ct1;

                _twoByteTokens = mem;
                while (mem.Value != 0)
                {
                    mem.Offset++;
                    i++;
                }
                _twoByteTokenStrings = mem;
                ct1 = i;
                while (mem.Value != 0)
                {
                    mem.Offset++;
                    while (mem.Value != 0)
                    {
                        mem.Offset++;
                    }
                    i--;
                    if ((i == 0) && (ct1 != 0))
                    {
                        _secondTwoByteTokenStrings = mem;
                        i = ct1;
                        ct1 = 0;
                    }
                    if (i == 0)
                        _thirdTwoByteTokenStrings = mem;
                }
                _byteTokens = mem;
                while (mem.Value != 0)
                {
                    mem.Offset++;
                }
                _byteTokenStrings = mem;
                while (mem.Value != 0)
                {
                    mem.Offset++;
                    while (mem.Value != 0)
                    {
                        mem.Offset++;
                    }
                }
                i = 0;
                l1:
                _stringTabPtr[i++] = mem;
                num--;
                if (num == 0)
                {
                    _stringTabPos = i;
                    return;
                }
                while (mem.Value != 0)
                {
                    mem.Offset++;
                }
                goto l1;
            }
            for (i = 0; i < num; i++)
            {
                _stringTabPtr[i] = mem;
                var tmp = mem.GetRawText();
                Debug($"{i} {tmp}");
                while (mem.Value != 0)
                {
                    mem.Offset++;
                }
                mem.Offset++;
            }

            _stringTabPos = i;
        }

        private void SetupLocalStringTable(BytePtr mem, int num)
        {
            for (var i = 0; i < num; i++)
            {
                _localStringtable[i] = mem;
                while (mem.Value != 0)
                    mem.Offset++;
                mem.Offset++;
            }
        }

        private int LoadTextFile(string filename, BytePtr dst)
        {
            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_OLD_BUNDLE))
                return LoadTextFile_simon1(filename, dst);
            return LoadTextFile_gme(filename, dst);
        }

        private int LoadTextFile_simon1(string filename, BytePtr dst)
        {
            var fo = OpenFileRead(filename);
            int size;

            if (fo == null)
                Error("loadTextFile: Can't open '{0}'", filename);

            size = (int) fo.Length;

            if (fo.Read(dst.Data, dst.Offset, size) != size)
                Error("loadTextFile: fread failed");
            fo.Dispose();

            return size;
        }

        private int LoadTextFile_gme(string filename, BytePtr dst)
        {
            int res = int.Parse(filename.Substring(4)) + _textIndexBase - 1;
            int offs = (int) _gameOffsetsPtr[res];
            int size = (int) (_gameOffsetsPtr[res + 1] - offs);

            ReadGameFile(dst, offs, size);

            return size;
        }

        private void LoadTextIntoMem(ushort stringId)
        {
            ushort baseMin = 0x8000;

            _tablesHeapPtr = _tablesheapPtrNew;
            _tablesHeapCurPos = _tablesHeapCurPosNew;

            var p = new BytePtr(_strippedTxtMem);

            // get filename
            while (p.Value != 0)
            {
                string filename = string.Empty;
                while (p.Value != 0)
                {
                    filename += (char) p.Value;
                    p.Offset++;
                }
                p.Offset++;

                if (_gd.Platform == Platform.Acorn)
                {
                    filename += ".DAT";
                }

                var baseMax = (ushort) ((p[0] * 256) | p[1]);
                p += 2;

                if (stringId < baseMax)
                {
                    _stringIdLocalMin = baseMin;
                    _stringIdLocalMax = baseMax;

                    _localStringtable = new BytePtr[baseMax - baseMin + 1];

                    var size = (ushort) ((baseMax - baseMin + 1) * 4);
                    _tablesHeapPtr += size;
                    _tablesHeapCurPos += size;

                    size = (ushort) LoadTextFile(filename, _tablesHeapPtr);

                    SetupLocalStringTable(_tablesHeapPtr, baseMax - baseMin + 1);

                    _tablesHeapPtr += size;
                    _tablesHeapCurPos += size;

                    if (_tablesHeapCurPos > _tablesHeapSize)
                    {
                        Error("loadTextIntoMem: Out of table memory");
                    }
                    return;
                }

                baseMin = baseMax;
            }

            Error("loadTextIntoMem: didn't find {0}", stringId);
        }

        protected string GetPixelLength(string @string, ushort maxWidth, out ushort pixels)
        {
            var s = 0;
            pixels = 0;

            foreach(var chr in @string)
            {
                s++;
                byte len = _language == Language.PL_POL ? PolishCharWidth[chr] : CharWidth[chr];
                if (pixels + len > maxWidth)
                    break;
                pixels += len;
            }

            return @string.Substring(s);
        }

        private bool PrintTextOf(uint a, uint x, uint y)
        {
            if (GameType == SIMONGameType.GType_SIMON2)
            {
                if (GetBitFlag(79))
                {
                    _variableArray[84] = (short) a;
                    var sub = GetSubroutineByID(5003);
                    if (sub != null)
                        StartSubroutineEx(sub);
                    return true;
                }
            }

            if (a >= _numTextBoxes)
                return false;


            var stringPtr = GetStringPtrById(_shortText[a]);
            if (GameType == SIMONGameType.GType_FF)
            {
                ushort pixels;
                GetPixelLength(stringPtr, 400, out pixels);
                var w = (ushort) (pixels + 1);
                x = (uint) (x - w / 2);
                PrintScreenText(6, 0, stringPtr, (short) x, (short) y, (short) w);
            }
            else
            {
                ShowActionString(stringPtr);
            }

            return true;
        }

        private bool PrintNameOf(Item item, uint x, uint y)
        {
            if (item == null || item == DummyItem2 || item == DummyItem3)
                return false;

            var subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            if (subObject == null)
                return false;

            var stringPtr = GetStringPtrById(subObject.objectName);
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
            {
                ushort pixels;
                GetPixelLength(stringPtr, 400, out pixels);
                short w = (short) (pixels + 1);
                x = (uint) (x - w / 2);
                PrintScreenText(6, 0, stringPtr, (short) x, (short) y, w);
            }
            else
            {
                ShowActionString(stringPtr);
            }

            return true;
        }

        protected virtual void PrintScreenText(uint vgaSpriteId, uint color, string str, short x, short y, short width)
        {
            var @string = new BytePtr(str.GetBytes());
            byte[] convertedString = new byte[320];
            BytePtr convertedString2 = convertedString;
            short height, talkDelay;
            int stringLength = str.Length;
            int lettersPerRow, lettersPerRowJustified;
            const int textHeight = 10;

            height = textHeight;
            lettersPerRow = width / 6;
            lettersPerRowJustified = stringLength / (stringLength / lettersPerRow + 1) + 1;

            talkDelay = (short) ((stringLength + 3) / 3);
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                if (_variableArray[141] == 0)
                    _variableArray[141] = 9;
                _variableArray[85] = (short) (_variableArray[141] * talkDelay);

                if (_language == Language.HE_ISR)
                    _variableArray[85] += (short) (talkDelay * 2);
            }
            else
            {
                if (_variableArray[86] == 0)
                    talkDelay /= 2;
                if (_variableArray[86] == 2)
                    talkDelay *= 2;
                _variableArray[85] = (short) (talkDelay * 5);
            }

            System.Diagnostics.Debug.Assert(stringLength > 0);

            while (stringLength > 0)
            {
                int pos = 0;
                if (stringLength > lettersPerRow)
                {
                    int removeLastWord = 0;
                    if (lettersPerRow > lettersPerRowJustified)
                    {
                        pos = lettersPerRowJustified;
                        while ((pos + @string.Offset < str.Length) && @string[pos] != ' ')
                            pos++;
                        if (pos > lettersPerRow)
                            removeLastWord = 1;
                    }
                    if (lettersPerRow <= lettersPerRowJustified || removeLastWord != 0)
                    {
                        pos = lettersPerRow;
                        while (@string[pos] != ' ' && pos > 0)
                            pos--;
                    }
                    height += textHeight;
                    y -= textHeight;
                }
                else
                    pos = stringLength;
                var padding = (lettersPerRow - pos) % 2 != 0 ? (lettersPerRow - pos) / 2 + 1 : (lettersPerRow - pos) / 2;
                while (padding-- != 0)
                {
                    convertedString2.Value = (byte) ' ';
                    convertedString2.Offset++;
                }
                stringLength -= pos;
                while (pos-- != 0)
                {
                    convertedString2.Value = @string.Value;
                    convertedString2.Offset++;
                    @string.Offset++;
                }
                convertedString2.Value = (byte) '\n';
                convertedString2.Offset++;
                @string.Offset++; // skip space
                stringLength--; // skip space
            }
            convertedString2[-1] = 0;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                StopAnimate((ushort) (vgaSpriteId + 199));
            else
                StopAnimateSimon2(2, (ushort) vgaSpriteId);

            if (_gd.Platform == Platform.Amiga)
            {
                color = color * 3 + 1;
                RenderStringAmiga(vgaSpriteId, color, (uint) width, (uint) height, convertedString.GetRawText());
            }
            else
            {
                color = color * 3 + 192;
                RenderString((int) vgaSpriteId, color, (ushort) width, (ushort) height, convertedString);
            }

            ushort windowNum = (ushort) (!GetBitFlag(133) ? 3 : 4);
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
                windowNum = 4;

            x /= 8;
            if (y < 2)
                y = 2;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
            {
                ushort id = (ushort) (199 + vgaSpriteId);
                Animate(windowNum, (ushort) (id / 100), id, x, y, 12);
            }
            else
            {
                Animate(windowNum, 2, (ushort) vgaSpriteId, x, y, 12);
            }
        }

        protected uint GetNextStringID()
        {
            return (ushort) GetNextWord();
        }

        protected void SetScriptCondition(bool cond)
        {
            _runScriptCondition[_recursionDepth] = cond;
        }

        protected static readonly byte[] PolishCharWidth =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 6, 2, 8, 7, 6, 10, 8, 2,
            4, 4, 7, 6, 3, 4, 2, 3, 6, 4,
            6, 6, 7, 6, 6, 6, 6, 6, 2, 8,
            6, 9, 7, 6, 6, 8, 7, 8, 8, 7,
            6, 9, 8, 2, 6, 7, 6, 10, 8, 9,
            7, 9, 7, 7, 8, 8, 8, 12, 8, 8,
            7, 6, 7, 6, 4, 7, 7, 7, 7, 6,
            7, 7, 4, 7, 6, 2, 3, 6, 2, 10,
            6, 7, 7, 7, 5, 6, 4, 6, 6, 10,
            6, 6, 6, 0, 0, 0, 0, 0, 8, 6,
            7, 7, 7, 7, 7, 6, 7, 7, 7, 4,
            4, 3, 8, 8, 7, 0, 0, 7, 7, 7,
            6, 6, 6, 9, 8, 0, 0, 0, 0, 0,
            7, 3, 7, 6, 6, 8, 0, 0, 6, 0,
            0, 0, 0, 2, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 7
        };


        protected static readonly byte[] CharWidth =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 6, 2, 4, 8, 6, 10, 9, 2,
            4, 4, 6, 6, 3, 4, 2, 3, 6, 4,
            6, 6, 7, 6, 6, 6, 6, 6, 2, 3,
            7, 7, 7, 6, 11, 8, 7, 8, 8, 7,
            6, 9, 8, 2, 6, 7, 6, 10, 8, 9,
            7, 9, 7, 7, 8, 8, 8, 12, 8, 8,
            7, 3, 3, 3, 6, 8, 3, 7, 7, 6,
            7, 7, 4, 7, 6, 2, 3, 6, 2, 10,
            6, 7, 7, 7, 5, 6, 4, 7, 7, 10,
            6, 6, 6, 0, 0, 0, 0, 0, 8, 6,
            7, 7, 7, 7, 7, 6, 7, 7, 7, 4,
            4, 3, 8, 8, 7, 0, 0, 7, 7, 7,
            6, 6, 6, 9, 8, 0, 0, 0, 0, 0,
            7, 3, 7, 6, 6, 8, 0, 6, 0, 0,
            0, 0, 0, 2, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 7
        };
    }
}