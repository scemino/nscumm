using System;
using System.Linq;
using System.Text;
using NScumm.Core;

namespace NScumm.Sky
{
    internal partial class Text
    {
        private const int MainCharHeight = 12;

        private const int FirstTextSec = 77;
        private const int FirstTextBuffer = 274;
        private const int NoOfTextSections = 8; // 8 sections per language
        private const int CharSetFile = 60150;
        private const int CharSetHeader = 128;
        private const int MaxNoLines = 10;

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

        private readonly SkyCompact _skyCompact;

        private readonly Disk _skyDisk;
        private StringBuilder _textBuffer = new StringBuilder();
        private uint _mouseOfsX, _mouseOfsY;

        public Text(Disk disk, SkyCompact skyCompact)
        {
            _skyDisk = disk;
            _skyCompact = skyCompact;

            InitHuffTree();

            _mainCharacterSet = new CharSet
            {
                Addr = _skyDisk.LoadFile(CharSetFile),
                CharHeight = MainCharHeight,
                CharSpacing = 0
            };

            FnSetFont(0);

            if (!SystemVars.Instance.GameVersion.Type.HasFlag(SkyGameType.Demo))
            {
                _controlCharacterSet = new CharSet
                {
                    Addr = _skyDisk.LoadFile(60520),
                    CharHeight = 12,
                    CharSpacing = 0
                };

                _linkCharacterSet = new CharSet
                {
                    Addr = _skyDisk.LoadFile(60521),
                    CharHeight = 12,
                    CharSpacing = 1
                };
            }
        }

        public Logic Logic { get; internal set; }

        public uint CurrentCharSet
        {
            get { return _curCharSet; }
        }

        public uint NumLetters { get; private set; }

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
            NumLetters = 2;

            // work around bug #778105 (line width exceeded)
            text = text.Replace("MUND-BEATMUNG!", "MUND BEATMUNG!");

            // work around bug #1151924 (line width exceeded when talking to gardener using spanish text)
            // This text apparently only is broken in the floppy versions, the CD versions contain
            // the correct string "MANIFESTACION - ARTISTICA.", which doesn't break the algorithm/game.
            var textBytes = text.Replace("MANIFESTACION-ARTISTICA.", "MANIFESTACION ARTISTICA.").ToCharArray().Select(c => (byte)c).ToArray();

            var curPos = 0;
            var lastSpace = 0;
            var textChar = textBytes[curPos++];

            while (textChar >= 0x20 && curPos <= textBytes.Length)
            {
                if ((_curCharSet == 1) && (textChar >= 0x80))
                    textChar = 0x20;

                textChar -= 0x20;
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

                    textBytes[lastSpace - 1] = 10;
                    lineWidth = 0;
                    numLines++;
                    curPos = lastSpace; //go back for new count
                }

                textChar = textBytes.Length == curPos ? (byte)0 : textBytes[curPos++];
                NumLetters++;
            }

            uint dtLastWidth = lineWidth; //save width of last line
            centerTable[numLines] = lineWidth; //and update centering table
            numLines++;

            if (numLines > MaxNoLines)
                throw new InvalidOperationException("Maximum no. of lines exceeded");

            var dtLineSize = pixelWidth * _charHeight;
            var sizeOfDataFileHeader = ServiceLocator.Platform.SizeOf<DataFileHeader>();
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

                textChar = textBytes[curPos++];
                while (textChar >= 0x20)
                {
                    MakeGameCharacter((byte)(textChar - 0x20), _characterSet, dest, ref curDest, color, pixelWidth);
                    textChar = textBytes.Length == curPos ? (byte)0 : textBytes[curPos++];
                }

                prevDest = curDest = prevDest + dtLineSize; //start of last line + start of next
            } while (textChar >= 10);

            var ret = new DisplayedText
            {
                TextData = dest,
                TextWidth = dtLastWidth
            };
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
            _characterSet = newCharSet.Addr;
            _charHeight = (byte)newCharSet.CharHeight;
            _dtCharSpacing = newCharSet.CharSpacing;
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
            Logic.ScriptVariables[Logic.CURSOR_ID] = text.CompactNum;
            if (Logic.ScriptVariables[Logic.MENU] != 0)
            {
                _mouseOfsY = Logic.TOP_LEFT_Y - 2;
                if (mouseX < 150)
                    _mouseOfsX = Logic.TOP_LEFT_X + 24;
                else
                    _mouseOfsX = Logic.TOP_LEFT_X - 8 - text.TextWidth;
            }
            else
            {
                _mouseOfsY = Logic.TOP_LEFT_Y - 10;
                if (mouseX < 150)
                    _mouseOfsX = Logic.TOP_LEFT_X + 13;
                else
                    _mouseOfsX = Logic.TOP_LEFT_X - 8 - text.TextWidth;
            }
            Compact textCompact = _skyCompact.FetchCpt(text.CompactNum);
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

            cpt.Core.flag = (ushort)((ushort)(compactNum - Logic.FIRST_TEXT_COMPACT) + FirstTextBuffer);

            SkyEngine.ItemList[cpt.Core.flag] = textInfo.TextData;

            cpt.Core.logic = logicNum;
            cpt.Core.status = Logic.ST_LOGIC | Logic.ST_FOREGROUND | Logic.ST_RECREATE;
            cpt.Core.screen = (ushort)Logic.ScriptVariables[Logic.SCREEN];

            textInfo.CompactNum = (ushort)compactNum;
            return textInfo;
        }

        public void LogicCursor(Compact textCompact, ushort mouseX, ushort mouseY)
        {
            textCompact.Core.xcood = (ushort)(mouseX + _mouseOfsX);
            textCompact.Core.ycood = (ushort)(mouseY + _mouseOfsY);
            if (textCompact.Core.ycood < Logic.TOP_LEFT_Y)
                textCompact.Core.ycood = Logic.TOP_LEFT_Y;
        }

        public void FnTextModule(uint textInfoId, uint textNo)
        {
            FnSetFont(1);
            var msgData = new UShortAccess(_skyCompact.FetchCptRaw((ushort)textInfoId), 0);
            var textId = LowTextManager(textNo, msgData[1], msgData[2], 209, false);
            Logic.ScriptVariables[Logic.RESULT] = textId.CompactNum;
            var textCompact = _skyCompact.FetchCpt(textId.CompactNum);
            textCompact.Core.xcood = msgData[3];
            textCompact.Core.ycood = msgData[4];
            FnSetFont(0);
        }

        private void MakeGameCharacter(byte textChar, byte[] charSetPtr, byte[] dest, ref int dstPtr, byte color,
            ushort bufPitch)
        {
            var charWidth = (byte)(charSetPtr[textChar] + 1 - _dtCharSpacing);
            var charSpritePtr = CharSetHeader + (_charHeight << 2) * textChar;
            var curPos = 0;

            for (var i = 0; i < _charHeight; i++)
            {
                var prevPos = curPos;

                var data = charSetPtr.ToUInt16BigEndian(charSpritePtr);
                var mask = charSetPtr.ToUInt16BigEndian(charSpritePtr + 2);
                charSpritePtr += 4;

                for (var j = 0; j < charWidth; j++)
                {
                    var maskBit = (mask & 0x8000) != 0;
                    mask <<= 1;
                    var dataBit = (data & 0x8000) != 0;
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
                    _huffTree = HuffTree00109;
                    break;
                case 272: // FIXME: Extract data
                case 267:
                    _huffTree = HuffTree00267;
                    break;
                case 288:
                    _huffTree = HuffTree00288;
                    break;
                case 303:
                    _huffTree = HuffTree00303;
                    break;
                case 331:
                    _huffTree = HuffTree00331;
                    break;
                case 348:
                    _huffTree = HuffTree00348;
                    break;
                case 365:
                    _huffTree = HuffTree00365;
                    break;
                case 368:
                    _huffTree = HuffTree00368;
                    break;
                case 372:
                    _huffTree = HuffTree00372;
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

            if (SkyEngine.ItemList[FirstTextSec + sectionNo] == null)
            {
                //check if already loaded
                //debug(5, "Loading Text item(s) for Section %d", (sectionNo >> 2));

                var fileNo = sectionNo + (uint)(SystemVars.Instance.Language * NoOfTextSections + 60600);
                SkyEngine.ItemList[FirstTextSec + sectionNo] = _skyDisk.LoadFile((ushort)fileNo);
            }
            var textData = SkyEngine.ItemList[FirstTextSec + sectionNo];
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

        private class CharSet
        {
            public byte[] Addr;
            public uint CharHeight;
            public uint CharSpacing;
        }
    }

    internal class DisplayedText
    {
        public byte[] TextData;
        public uint TextWidth;
        public ushort CompactNum;
    }
}