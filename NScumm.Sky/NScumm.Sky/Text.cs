using System;
using System.Text;
using NScumm.Core;

namespace NScumm.Sky
{
    internal partial class Text
    {
        private const int MAIN_CHAR_HEIGHT = 12;

        private const int FIRST_TEXT_SEC = 77;
        private const int FIRST_TEXT_BUFFER = 274;
        private const int LAST_TEXT_BUFFER = 284;
        private const int NO_OF_TEXT_SECTIONS = 8; // 8 sections per language
        private const int CHAR_SET_FILE = 60150;
        private const int MAX_SPEECH_SECTION = 7;
        private const int CHAR_SET_HEADER = 128;
        private const int MAX_NO_LINES = 10;

        private const int HufftreeLeftChild = 0;
        private const int HufftreeRightChild = 1;
        private const int HufftreeValue = 2;

        private static readonly PatchMessage[] PatchedMessages =
        {
            new PatchMessage(28724, "Testo e Parlato"), // - italian
            new PatchMessage(28707, "Solo Testo"),
            new PatchMessage(28693, "Solo Parlato"),
            new PatchMessage(28724, "Text och tal"), // - swedish
            new PatchMessage(28707, "Endast text"),
            new PatchMessage(28693, "Endast tal"),
            new PatchMessage(28686, "Musikvolym"),
            new PatchMessage(4336, "Wir befinden uns EINHUNDERTZWANZIG METER #ber dem ERBODEN!"), // - german
            new PatchMessage(28686, "Volume de musique") // - french
        };


        private static readonly ushort[] PatchLangIdx =
        {
            0xFFFF, // SKY_ENGLISH
            7, // SKY_GERMAN
            8, // SKY_FRENCH
            0xFFFF, // SKY_USA
            3, // SKY_SWEDISH
            0, // SKY_ITALIAN
            0xFFFF, // SKY_PORTUGUESE
            0xFFFF // SKY_SPANISH
        };

        private static readonly ushort[] PatchLangNum =
        {
            0, // SKY_ENGLISH
            1, // SKY_GERMAN
            1, // SKY_FRENCH
            0, // SKY_USA
            4, // SKY_SWEDISH
            3, // SKY_ITALIAN
            0, // SKY_PORTUGUESE
            0 // SKY_SPANISH
        };

        private byte[] _characterSet;
        private byte _charHeight;

        private uint _curCharSet;

        private uint _dtCharSpacing; //character separation adjustment

        private byte[,] _huffTree;
        private readonly CharSet _mainCharacterSet;
        private readonly CharSet _linkCharacterSet;
        private readonly CharSet _controlCharacterSet;

        private uint _numLetters; //no of chars in message
        private SkyCompact _skyCompact;

        private readonly Disk _skyDisk;
        private StringBuilder _textBuffer = new StringBuilder();
        private uint _mouseOfsX, _mouseOfsY;

        public Text(Disk disk, SkyCompact skyCompact)
        {
            _skyDisk = disk;
            _skyCompact = skyCompact;

            InitHuffTree();

            _mainCharacterSet.addr = _skyDisk.LoadFile(CHAR_SET_FILE);
            _mainCharacterSet.charHeight = MAIN_CHAR_HEIGHT;
            _mainCharacterSet.charSpacing = 0;

            FnSetFont(0);

            if (!SystemVars.Instance.GameVersion.Type.HasFlag(SkyGameType.Demo))
            {
                _controlCharacterSet.addr = _skyDisk.LoadFile(60520);
                _controlCharacterSet.charHeight = 12;
                _controlCharacterSet.charSpacing = 0;

                _linkCharacterSet.addr = _skyDisk.LoadFile(60521);
                _linkCharacterSet.charHeight = 12;
                _linkCharacterSet.charSpacing = 1;
            }
            else
            {
                _controlCharacterSet.addr = null;
                _linkCharacterSet.addr = null;
            }
        }

        public Logic Logic { get; internal set; }

        public DisplayedText DisplayText(ushort textNum, byte[] dest, bool center, ushort pixelWidth, byte color)
        {
            //Render text into buffer *dest
            GetText(textNum);
            return DisplayText(_textBuffer.ToString(), dest, center, pixelWidth, color);
        }

        public DisplayedText DisplayText(string text, byte[] dest, bool center, ushort pixelWidth, byte color)
        {
            //Render text pointed to by *textPtr in buffer *dest
            var centerTable = new uint[10];
            ushort lineWidth = 0;

            uint numLines = 0;
            _numLetters = 2;

            // work around bug #778105 (line width exceeded)
            text = text.Replace("MUND-BEATMUNG!", "MUND BEATMUNG!");

            // work around bug #1151924 (line width exceeded when talking to gardener using spanish text)
            // This text apparently only is broken in the floppy versions, the CD versions contain
            // the correct string "MANIFESTACION - ARTISTICA.", which doesn't break the algorithm/game.
            var textBytes = text.Replace("MANIFESTACION-ARTISTICA.", "MANIFESTACION ARTISTICA.").ToCharArray();

            var curPos = 0;
            var lastSpace = 0;
            var textChar = textBytes[curPos++];

            while (textChar >= 0x20 && curPos != textBytes.Length)
            {
                if ((_curCharSet == 1) && (textChar >= 0x80))
                    textChar = (char)0x20;

                textChar -= (char)0x20;
                if (textChar == 0)
                {
                    lastSpace = curPos; //keep track of last space
                    centerTable[numLines] = lineWidth;
                }

                lineWidth += _characterSet[textChar]; //add character width
                lineWidth += (ushort)_dtCharSpacing; //include character spacing

                if (pixelWidth <= lineWidth)
                {
                    if (textBytes[lastSpace - 1] == 10)
                        throw new InvalidOperationException("line width exceeded");

                    textBytes[lastSpace - 1] = (char)10;
                    lineWidth = 0;
                    numLines++;
                    curPos = lastSpace; //go back for new count
                }

                textChar = textBytes[curPos++];
                _numLetters++;
            }

            uint dtLastWidth = lineWidth; //save width of last line
            centerTable[numLines] = lineWidth; //and update centering table
            numLines++;

            if (numLines > MAX_NO_LINES)
                throw new InvalidOperationException("Maximum no. of lines exceeded");

            var dtLineSize = pixelWidth * _charHeight;
            var sizeOfDataFileHeader = 22;
            var numBytes = (int)(dtLineSize * numLines + sizeOfDataFileHeader + 4);

            if (dest == null)
                dest = new byte[numBytes];

            // clear text sprite buffer
            Array.Clear(dest, sizeOfDataFileHeader, numBytes - sizeOfDataFileHeader);

            //make the header
            using (var header = ServiceLocator.Platform.WriteStructure<DataFileHeader>(dest, 0))
            {
                header.Object.s_width = pixelWidth;
                header.Object.s_height = (ushort)(_charHeight * numLines);
                header.Object.s_sp_size = (ushort)(pixelWidth * _charHeight * numLines);
                header.Object.s_offset_x = 0;
                header.Object.s_offset_y = 0;
            }

            //reset position
            curPos = 0;

            var curDest = sizeOfDataFileHeader; //point to where pixels start
            var prevDest = curDest;
            var centerTblPtr = 0;

            do
            {
                if (center)
                {
                    var width = (int)(pixelWidth - centerTable[centerTblPtr]) >> 1;
                    centerTblPtr++;
                    curDest += width;
                }

                textChar = text[curPos++];
                while (textChar >= 0x20 && curPos != textBytes.Length)
                {
                    MakeGameCharacter((byte)(textChar - 0x20), _characterSet, dest, ref curDest, color, pixelWidth);
                    textChar = text[curPos++];
                }

                prevDest = curDest = prevDest + dtLineSize; //start of last line + start of next
            } while (textChar >= 10 && curPos != textBytes.Length);

            var ret = new DisplayedText();
            ret.textData = dest;
            ret.textWidth = dtLastWidth;
            return ret;
        }

        public void FnSetFont(uint fontNr)
        {
            CharSet newCharSet;

            switch (fontNr)
            {
                case 0:
                    newCharSet = _mainCharacterSet;
                    break;
                case 1:
                    newCharSet = _linkCharacterSet;
                    break;
                case 2:
                    newCharSet = _controlCharacterSet;
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Tried to set invalid font ({0})", fontNr));
            }

            _curCharSet = fontNr;
            _characterSet = newCharSet.addr;
            _charHeight = (byte)newCharSet.charHeight;
            _dtCharSpacing = newCharSet.charSpacing;
        }

        public void ChangeTextSpriteColor(byte[] sprData, byte newCol)
        {
            var header = ServiceLocator.Platform.ToStructure<DataFileHeader>(sprData, 0);
            var offset = ServiceLocator.Platform.SizeOf<DataFileHeader>();
            for (ushort cnt = 0; cnt < header.s_sp_size; cnt++)
                if (sprData[offset + cnt] >= 241)
                    sprData[offset + cnt] = newCol;
        }

        public void FnPointerText(uint pointedId, ushort mouseX, ushort mouseY)
        {
            var ptrComp = _skyCompact.FetchCpt((ushort)pointedId);
            var text = LowTextManager(ptrComp.Core.cursorText, Logic.TEXT_MOUSE_WIDTH, Logic.L_CURSOR, 242, false);
            Logic.ScriptVariables[Logic.CURSOR_ID] = text.compactNum;
            if (Logic.ScriptVariables[Logic.MENU] != 0)
            {
                _mouseOfsY = Logic.TOP_LEFT_Y - 2;
                if (mouseX < 150)
                    _mouseOfsX = Logic.TOP_LEFT_X + 24;
                else
                    _mouseOfsX = Logic.TOP_LEFT_X - 8 - text.textWidth;
            }
            else
            {
                _mouseOfsY = Logic.TOP_LEFT_Y - 10;
                if (mouseX < 150)
                    _mouseOfsX = Logic.TOP_LEFT_X + 13;
                else
                    _mouseOfsX = Logic.TOP_LEFT_X - 8 - text.textWidth;
            }
            Compact textCompact = _skyCompact.FetchCpt(text.compactNum);
            LogicCursor(textCompact, mouseX, mouseY);
        }

        public DisplayedText LowTextManager(uint textNum, ushort width, ushort logicNum, byte color, bool center)
        {
            GetText(textNum);
            DisplayedText textInfo = DisplayText(_textBuffer.ToString(), null, center, width, color);

            uint compactNum = Logic.FIRST_TEXT_COMPACT;
            Compact cpt = _skyCompact.FetchCpt((ushort)compactNum);
            while (cpt.Core.status != 0)
            {
                compactNum++;
                cpt = _skyCompact.FetchCpt((ushort)compactNum);
            }

            cpt.Core.flag = (ushort)((ushort)(compactNum - Logic.FIRST_TEXT_COMPACT) + FIRST_TEXT_BUFFER);

            SkyEngine.ItemList[cpt.Core.flag] = textInfo.textData;

            cpt.Core.logic = logicNum;
            cpt.Core.status = Logic.ST_LOGIC | Logic.ST_FOREGROUND | Logic.ST_RECREATE;
            cpt.Core.screen = (ushort)Logic.ScriptVariables[Logic.SCREEN];

            textInfo.compactNum = (ushort)compactNum;
            return textInfo;
        }

        public void LogicCursor(Compact textCompact, ushort mouseX, ushort mouseY)
        {
            textCompact.Core.xcood = (ushort)(mouseX + _mouseOfsX);
            textCompact.Core.ycood = (ushort)(mouseY + _mouseOfsY);
            if (textCompact.Core.ycood < Logic.TOP_LEFT_Y)
                textCompact.Core.ycood = Logic.TOP_LEFT_Y;
        }

        public void FnTextModule(uint a, uint b)
        {
            throw new NotImplementedException();
        }

        private void MakeGameCharacter(byte textChar, byte[] charSetPtr, byte[] dest, ref int dstPtr, byte color,
            ushort bufPitch)
        {
            bool maskBit, dataBit;
            var charWidth = (byte)(charSetPtr[textChar] + 1 - _dtCharSpacing);
            ushort data, mask;
            var charSpritePtr = CHAR_SET_HEADER + (_charHeight << 2) * textChar;
            var curPos = 0;

            for (var i = 0; i < _charHeight; i++)
            {
                var prevPos = curPos;

                data = charSetPtr.ToUInt16BigEndian(charSpritePtr);
                mask = charSetPtr.ToUInt16BigEndian(charSpritePtr + 2);
                charSpritePtr += 4;

                for (var j = 0; j < charWidth; j++)
                {
                    maskBit = (mask & 0x8000) != 0; //check mask
                    mask <<= 1;
                    dataBit = (data & 0x8000) != 0; //check data
                    data <<= 1;

                    if (maskBit)
                    {
                        dest[dstPtr + curPos] = dataBit ? color : (byte)240; //black edge
                    }
                    curPos++;
                }
                //advance a line
                curPos = prevPos + bufPitch;
            }
            //update position
            dstPtr += (int)(charWidth + _dtCharSpacing * 2 - 1);
        }

        private void InitHuffTree()
        {
            switch (SystemVars.Instance.GameVersion.Version.Minor)
            {
                case 109:
                    _huffTree = _huffTree_00109;
                    break;
                case 272: // FIXME: Extract data
                case 267:
                    _huffTree = _huffTree_00267;
                    break;
                case 288:
                    _huffTree = _huffTree_00288;
                    break;
                case 303:
                    _huffTree = _huffTree_00303;
                    break;
                case 331:
                    _huffTree = _huffTree_00331;
                    break;
                case 348:
                    _huffTree = _huffTree_00348;
                    break;
                case 365:
                    _huffTree = _huffTree_00365;
                    break;
                case 368:
                    _huffTree = _huffTree_00368;
                    break;
                case 372:
                    _huffTree = _huffTree_00372;
                    break;
                default:
                    throw new NotSupportedException(string.Format("Unknown game version {0}",
                        SystemVars.Instance.GameVersion.Version.Minor));
            }
        }

        private void GetText(uint textNr)
        {
            //load text #"textNr" into textBuffer
            if (DoPatchMessage(textNr))
                return;

            var sectionNo = (textNr & 0x0F000) >> 12;

            if (SkyEngine.ItemList[FIRST_TEXT_SEC + sectionNo] == null)
            {
                //check if already loaded
                //debug(5, "Loading Text item(s) for Section %d", (sectionNo >> 2));

                var fileNo = sectionNo + (uint)(SystemVars.Instance.Language * NO_OF_TEXT_SECTIONS + 60600);
                SkyEngine.ItemList[FIRST_TEXT_SEC + sectionNo] = _skyDisk.LoadFile((ushort)fileNo);
            }
            var textData = SkyEngine.ItemList[FIRST_TEXT_SEC + sectionNo];
            var textDataPtr = 0;

            var offset = 0;

            var blockNr = textNr & 0xFE0;
            textNr &= 0x1F;

            if (blockNr != 0)
            {
                var blockPtr = 4;
                var nr32MsgBlocks = blockNr >> 5;

                do
                {
                    offset += textData.ToUInt16(textDataPtr + blockPtr);
                    blockPtr += 2;
                } while (--nr32MsgBlocks != 0);
            }

            if (textNr != 0)
            {
                var blockPtr = blockNr + textData.ToUInt16(textDataPtr);
                do
                {
                    ushort skipBytes = textData[textDataPtr + blockPtr++];
                    if ((skipBytes & 0x80) != 0)
                    {
                        skipBytes &= 0x7F;
                        skipBytes <<= 3;
                    }
                    offset += skipBytes;
                } while (--textNr != 0);
            }

            var bitPos = offset & 3;
            offset >>= 2;
            offset += textData.ToUInt16(textDataPtr + 2);

            textDataPtr += offset;

            //bit pointer: 0.8, 1.6, 2.4 ...
            bitPos ^= 3;
            bitPos++;
            bitPos <<= 1;

            char textChar;

            _textBuffer.Clear();
            do
            {
                textChar = GetTextChar(textData, ref textDataPtr, ref bitPos);
                if (textChar != 0)
                {
                    _textBuffer.Append(textChar);
                }
            } while (textChar != 0);
        }

        private char GetTextChar(byte[] data, ref int dataPtr, ref int bitPos)
        {
            var pos = 0;
            while (true)
            {
                if (GetTextBit(data, ref dataPtr, ref bitPos))
                    pos = _huffTree[pos, HufftreeRightChild];
                else
                    pos = _huffTree[pos, HufftreeLeftChild];

                if (_huffTree[pos, HufftreeLeftChild] == 0 && _huffTree[pos, HufftreeRightChild] == 0)
                {
                    return (char)_huffTree[pos, HufftreeValue];
                }
            }
        }

        private bool GetTextBit(byte[] data, ref int dataPtr, ref int bitPos)
        {
            if (bitPos != 0)
            {
                bitPos--;
            }
            else
            {
                dataPtr++;
                bitPos = 7;
            }

            return ((data[dataPtr] >> bitPos) & 1) != 0;
        }

        private bool DoPatchMessage(uint textNum)
        {
            var patchIdx = PatchLangIdx[SystemVars.Instance.Language];
            var patchNum = PatchLangNum[SystemVars.Instance.Language];
            for (ushort cnt = 0; cnt < patchNum; cnt++)
            {
                if (PatchedMessages[cnt + patchIdx].TextNr == textNum)
                {
                    _textBuffer = new StringBuilder(PatchedMessages[cnt + patchIdx].Text);
                    return true;
                }
            }
            return false;
        }

        private struct PatchMessage
        {
            public PatchMessage(uint textNr, string text)
            {
                TextNr = textNr;
                Text = text;
            }

            public readonly uint TextNr;
            public readonly string Text;
        }

        private struct CharSet
        {
            public byte[] addr;
            public uint charHeight;
            public uint charSpacing;
        }
    }

    internal struct DisplayedText
    {
        public byte[] textData;
        public uint textWidth;
        public ushort compactNum;
    }
}