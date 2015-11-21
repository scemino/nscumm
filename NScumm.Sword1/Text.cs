using System;
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    struct LineInfo
    {
        public ushort width;  // width of line in pixels
        public ushort length; // length of line in characters
    }

    internal class Text
    {
        const int MAX_TEXT_OBS = 3;
        const int OVERLAP = 3;
        const int NO_COL = 0;   // sprite background - 0 for transparency
        const int MAX_LINES = 30;

        const int BORDER_COL = 200;
        const int BORDER_COL_PSX = 199;
        const int LETTER_COL = 193;

        private ObjectMan _objMan;
        private ResMan _resMan;
        private byte _textCount;
        ushort _charHeight, _joinWidth;
        FrameHeader[] _textBlocks = new FrameHeader[MAX_TEXT_OBS];
        uint _fontId;
        private byte[] _font;

        public Text(ObjectMan objMan, ResMan resMan, bool czechVersion)
        {
            _objMan = objMan;
            _resMan = resMan;
            _textCount = 0;
            _fontId = (uint)(czechVersion ? Sword1Res.CZECH_GAME_FONT : Sword1Res.GAME_FONT);
            _font = _resMan.OpenFetchRes(_fontId);

            _joinWidth = (ushort)(CharWidth((byte)' ') - 2 * OVERLAP);
            _charHeight = _resMan.ReadUInt16(new FrameHeader(_resMan.FetchFrame(_font, 0)).height); // all chars have the same height
        }

        private int CharWidth(byte ch)
        {
            if (ch < ' ')
                ch = 64;
            return _resMan.ReadUInt16(new FrameHeader(_resMan.FetchFrame(_font, (uint)(ch - ' '))).width);
        }

        public ByteAccess GiveSpriteData(uint textTarget)
        {
            // textTarget is the resource ID of the Compact linking the textdata.
            // that's 0x950000 for slot 0 and 0x950001 for slot 1. easy, huh? :)
            textTarget &= ObjectMan.ITM_ID;
            Debug.Assert(textTarget < MAX_TEXT_OBS);

            return _textBlocks[textTarget].Data;
        }

        public uint LowTextManager(ByteAccess ascii, int width, byte pen)
        {
            _textCount++;
            if (_textCount > MAX_TEXT_OBS)
                throw new InvalidOperationException("Text::lowTextManager: MAX_TEXT_OBS exceeded");
            uint textObjId = (ObjectMan.TEXT_sect * ObjectMan.ITM_PER_SEC) - 1;
            do
            {
                textObjId++;
            } while (_objMan.FetchObject(textObjId).status != 0);
            // okay, found a free text object

            _objMan.FetchObject(textObjId).status = Logic.STAT_FORE;
            MakeTextSprite((byte)textObjId, ascii, (ushort)width, pen);

            return textObjId;
        }

        private void MakeTextSprite(byte slot, ByteAccess text, ushort maxWidth, byte pen)
        {
            LineInfo[] lines = new LineInfo[MAX_LINES];
            ushort numLines = AnalyzeSentence(text, maxWidth, lines);

            ushort sprWidth = 0;
            ushort lineCnt;
            for (lineCnt = 0; lineCnt < numLines; lineCnt++)
                if (lines[lineCnt].width > sprWidth)
                    sprWidth = lines[lineCnt].width;

            ushort sprHeight = (ushort)(_charHeight * numLines);
            uint sprSize = (uint)(sprWidth * sprHeight);
            Debug.Assert(_textBlocks[slot] == null); // if this triggers, the speechDriver failed to call Text::releaseText.
            _textBlocks[slot] = new FrameHeader(new byte[sprSize + FrameHeader.Size]);

            Array.Copy(new[] { (byte)'N', (byte)'u', (byte)' ', (byte)' ' }, 0, _textBlocks[slot].runTimeComp.Data, _textBlocks[slot].runTimeComp.Offset, 4);
            _textBlocks[slot].compSize = 0;
            _textBlocks[slot].width = _resMan.ReadUInt16(sprWidth);
            _textBlocks[slot].height = _resMan.ReadUInt16(sprHeight);
            _textBlocks[slot].offsetX = 0;
            _textBlocks[slot].offsetY = 0;

            var linePtr = _textBlocks[slot];
            var linePtrOff = FrameHeader.Size;
            linePtr.Data.Data.Set(linePtr.Data.Offset + linePtrOff, (byte)NO_COL, (int)sprSize);
            for (lineCnt = 0; lineCnt < numLines; lineCnt++)
            {
                var sprPtr = (sprWidth - lines[lineCnt].width) / 2; // center the text
                for (ushort pos = 0; pos < lines[lineCnt].length; pos++)
                {
                    sprPtr += CopyChar(text[0], new ByteAccess(linePtr.Data.Data, linePtr.Data.Offset + sprPtr), sprWidth, pen) - OVERLAP;
                    text.Offset++;
                }
                text.Offset++; // skip space at the end of the line
                if (SystemVars.Platform == Platform.PSX) //Chars are half height in psx version
                    linePtrOff += (_charHeight / 2) * sprWidth;
                else
                    linePtrOff += _charHeight * sprWidth;
            }
        }

        private ushort AnalyzeSentence(ByteAccess text, ushort maxWidth, LineInfo[] line)
        {
            ushort lineNo = 0;

            bool firstWord = true;
            while (text[0] != 0)
            {
                ushort wordWidth = 0;
                ushort wordLength = 0;

                while ((text[0] != ' ') && text[0] != 0)
                {
                    wordWidth = (ushort)(wordWidth + CharWidth(text[0]) - OVERLAP);
                    wordLength++;
                    text.Offset++;
                }
                if (text[0] == ' ')
                    text.Offset++;

                wordWidth += OVERLAP; // no overlap on final letter of word!
                if (firstWord)
                { // first word on first line, so no separating SPACE needed
                    line[0].width = wordWidth;
                    line[0].length = wordLength;
                    firstWord = false;
                }
                else
                {
                    // see how much extra space this word will need to fit on current line
                    // (with a separating space character - also overlapped)
                    ushort spaceNeeded = (ushort)(_joinWidth + wordWidth);

                    if (line[lineNo].width + spaceNeeded <= maxWidth)
                    {
                        line[lineNo].width += spaceNeeded;
                        line[lineNo].length = (ushort)(line[lineNo].length + 1 + wordLength); // NB. space+word characters
                    }
                    else
                    {    // put word (without separating SPACE) at start of next line
                        lineNo++;
                        Debug.Assert(lineNo < MAX_LINES);
                        line[lineNo].width = wordWidth;
                        line[lineNo].length = wordLength;
                    }
                }
            }
            return (ushort)(lineNo + 1);  // return no of lines
        }

        ushort CopyChar(byte ch, ByteAccess sprPtr, ushort sprWidth, byte pen)
        {
            if (ch < ' ')
                ch = 64;
            FrameHeader chFrame = new FrameHeader(_resMan.FetchFrame(_font, (uint)(ch - ' ')));
            var chData = FrameHeader.Size;
            var dest = sprPtr;
            byte[] decBuf = null;
            ByteAccess decChr;
            ushort frameHeight = 0;

            if (Sword1.SystemVars.Platform == Platform.PSX)
            {
                frameHeight = (ushort)(_resMan.ReadUInt16(chFrame.height) / 2);
                if (_fontId == Sword1Res.CZECH_GAME_FONT)
                { //Czech game fonts are compressed
                    decBuf = new byte[(_resMan.ReadUInt16(chFrame.width)) * (_resMan.ReadUInt16(chFrame.height) / 2)];
                    Screen.DecompressHIF(chFrame.Data.Data, chFrame.Data.Offset + chData, decBuf);
                    decChr = new ByteAccess(decBuf, 0);
                }
                else //Normal game fonts are not compressed
                    decChr = new ByteAccess(chFrame.Data.Data, chFrame.Data.Offset + chData);
            }
            else
            {
                frameHeight = _resMan.ReadUInt16(chFrame.height);
                decChr = new ByteAccess(chFrame.Data.Data, chFrame.Data.Offset + chData);
            }

            for (ushort cnty = 0; cnty < frameHeight; cnty++)
            {
                for (ushort cntx = 0; cntx < _resMan.ReadUInt16(chFrame.width); cntx++)
                {
                    if (decChr[0] == LETTER_COL)
                        dest[cntx] = pen;
                    else if (((decChr[0] == BORDER_COL) || (decChr[0] == BORDER_COL_PSX)) && (dest[cntx] == 0)) // don't do a border if there's already a color underneath (chars can overlap)
                        dest[cntx] = BORDER_COL;
                    decChr.Offset++;
                }
                dest.Offset += sprWidth;
            }

            return _resMan.ReadUInt16(chFrame.width);
        }

        public void ReleaseText(int textId)
        {
            // TODO:
        }
    }
}