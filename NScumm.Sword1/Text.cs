using System;
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    internal struct LineInfo
    {
        public ushort Width;  // width of line in pixels
        public ushort Length; // length of line in characters
    }

    internal class Text
    {
        private const int MaxTextObs = 3;
        private const int Overlap = 3;
        private const int NoCol = 0;   // sprite background - 0 for transparency
        private const int MaxLines = 30;

        private const int BorderCol = 200;
        private const int BorderColPsx = 199;
        private const int LetterCol = 193;

        private readonly ObjectMan _objMan;
        private readonly ResMan _resMan;
        private byte _textCount;
        private readonly ushort _charHeight;
        private readonly ushort _joinWidth;
        private readonly FrameHeader[] _textBlocks = new FrameHeader[MaxTextObs];
        private readonly uint _fontId;
        private readonly byte[] _font;

        public Text(ObjectMan objMan, ResMan resMan, bool czechVersion)
        {
            _objMan = objMan;
            _resMan = resMan;
            _textCount = 0;
            _fontId = (uint)(czechVersion ? Sword1Res.CZECH_GAME_FONT : Sword1Res.GAME_FONT);
            _font = _resMan.OpenFetchRes(_fontId);

            _joinWidth = (ushort)(CharWidth((byte)' ') - 2 * Overlap);
            _charHeight = _resMan.ReadUInt16(new FrameHeader(_resMan.FetchFrame(_font, 0)).height); // all chars have the same height
        }

        public ByteAccess GiveSpriteData(uint textTarget)
        {
            // textTarget is the resource ID of the Compact linking the textdata.
            // that's 0x950000 for slot 0 and 0x950001 for slot 1. easy, huh? :)
            textTarget &= ObjectMan.ITM_ID;
            Debug.Assert(textTarget < MaxTextObs);

            return _textBlocks[textTarget].Data;
        }

        public uint LowTextManager(ByteAccess ascii, int width, byte pen)
        {
            _textCount++;
            if (_textCount > MaxTextObs)
                throw new InvalidOperationException("Text::lowTextManager: MAX_TEXT_OBS exceeded");
            uint textObjId = ObjectMan.TEXT_sect * ObjectMan.ITM_PER_SEC - 1;
            do
            {
                textObjId++;
            } while (_objMan.FetchObject(textObjId).status != 0);
            // okay, found a free text object

            _objMan.FetchObject(textObjId).status = Logic.STAT_FORE;
            MakeTextSprite((byte)textObjId, ascii, (ushort)width, pen);

            return textObjId;
        }

        public void ReleaseText(int id, bool updateCount = true)
        {
            id &= ObjectMan.ITM_ID;
            Debug.Assert(id < MaxTextObs);
            if (_textBlocks[id] != null)
            {
                _textBlocks[id] = null;
                if (updateCount)
                    _textCount--;
            }
        }

        private int CharWidth(byte ch)
        {
            if (ch < ' ')
                ch = 64;
            return _resMan.ReadUInt16(new FrameHeader(_resMan.FetchFrame(_font, (uint)(ch - ' '))).width);
        }

        private void MakeTextSprite(byte slot, ByteAccess text, ushort maxWidth, byte pen)
        {
            var lines = new LineInfo[MaxLines];
            var numLines = AnalyzeSentence(text, maxWidth, lines);

            ushort sprWidth = 0;
            ushort lineCnt;
            for (lineCnt = 0; lineCnt < numLines; lineCnt++)
                if (lines[lineCnt].Width > sprWidth)
                    sprWidth = lines[lineCnt].Width;

            var sprHeight = (ushort)(_charHeight * numLines);
            var sprSize = (uint)(sprWidth * sprHeight);
            Debug.Assert(_textBlocks[slot] == null); // if this triggers, the speechDriver failed to call Text::releaseText.
            _textBlocks[slot] = new FrameHeader(new byte[sprSize + FrameHeader.Size]);

            Array.Copy(new[] { (byte)'N', (byte)'u', (byte)' ', (byte)' ' }, 0, _textBlocks[slot].runTimeComp.Data, _textBlocks[slot].runTimeComp.Offset, 4);
            _textBlocks[slot].compSize = 0;
            _textBlocks[slot].width = _resMan.ReadUInt16(sprWidth);
            _textBlocks[slot].height = _resMan.ReadUInt16(sprHeight);
            _textBlocks[slot].offsetX = 0;
            _textBlocks[slot].offsetY = 0;

            var linePtr = new ByteAccess(_textBlocks[slot].Data.Data, _textBlocks[slot].Data.Offset + FrameHeader.Size);
            linePtr.Data.Set(linePtr.Offset, NoCol, (int)sprSize);
            for (lineCnt = 0; lineCnt < numLines; lineCnt++)
            {
                var sprPtr = (sprWidth - lines[lineCnt].Width) / 2; // center the text
                for (ushort pos = 0; pos < lines[lineCnt].Length; pos++)
                {
                    sprPtr += CopyChar(text[0], new ByteAccess(linePtr.Data, linePtr.Offset + sprPtr), sprWidth, pen) - Overlap;
                    text.Offset++;
                }
                text.Offset++; // skip space at the end of the line
                if (SystemVars.Platform == Platform.PSX) //Chars are half height in psx version
                    linePtr.Offset += _charHeight / 2 * sprWidth;
                else
                    linePtr.Offset += _charHeight * sprWidth;
            }
        }

        private ushort AnalyzeSentence(ByteAccess textSrc, ushort maxWidth, LineInfo[] line)
        {
            ushort lineNo = 0;
            var text = new ByteAccess(textSrc.Data, textSrc.Offset);
            var firstWord = true;
            while (text[0] != 0)
            {
                ushort wordWidth = 0;
                ushort wordLength = 0;

                while ((text[0] != ' ') && text[0] != 0)
                {
                    wordWidth = (ushort)(wordWidth + CharWidth(text[0]) - Overlap);
                    wordLength++;
                    text.Offset++;
                }
                if (text[0] == ' ')
                    text.Offset++;

                wordWidth += Overlap; // no overlap on final letter of word!
                if (firstWord)
                { // first word on first line, so no separating SPACE needed
                    line[0].Width = wordWidth;
                    line[0].Length = wordLength;
                    firstWord = false;
                }
                else
                {
                    // see how much extra space this word will need to fit on current line
                    // (with a separating space character - also overlapped)
                    var spaceNeeded = (ushort)(_joinWidth + wordWidth);

                    if (line[lineNo].Width + spaceNeeded <= maxWidth)
                    {
                        line[lineNo].Width += spaceNeeded;
                        line[lineNo].Length = (ushort)(line[lineNo].Length + 1 + wordLength); // NB. space+word characters
                    }
                    else
                    {    // put word (without separating SPACE) at start of next line
                        lineNo++;
                        Debug.Assert(lineNo < MaxLines);
                        line[lineNo].Width = wordWidth;
                        line[lineNo].Length = wordLength;
                    }
                }
            }
            return (ushort)(lineNo + 1);  // return no of lines
        }

        private ushort CopyChar(byte ch, ByteAccess sprPtr, ushort sprWidth, byte pen)
        {
            if (ch < ' ')
                ch = 64;
            var chFrame = new FrameHeader(_resMan.FetchFrame(_font, (uint)(ch - ' ')));
            var chData = FrameHeader.Size;
            var dest = sprPtr;
            ByteAccess decChr;
            ushort frameHeight;

            if (SystemVars.Platform == Platform.PSX)
            {
                frameHeight = (ushort)(_resMan.ReadUInt16(chFrame.height) / 2);
                if (_fontId == Sword1Res.CZECH_GAME_FONT)
                { //Czech game fonts are compressed
                    var decBuf = new byte[_resMan.ReadUInt16(chFrame.width) * (_resMan.ReadUInt16(chFrame.height) / 2)];
                    Screen.DecompressHIF(chFrame.Data.Data, chFrame.Data.Offset + chData, decBuf);
                    decChr = new ByteAccess(decBuf);
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
                    if (decChr[0] == LetterCol)
                        dest[cntx] = pen;
                    else if (((decChr[0] == BorderCol) || (decChr[0] == BorderColPsx)) && (dest[cntx] == 0)) // don't do a border if there's already a color underneath (chars can overlap)
                        dest[cntx] = BorderCol;
                    decChr.Offset++;
                }
                dest.Offset += sprWidth;
            }

            return _resMan.ReadUInt16(chFrame.width);
        }
    }
}