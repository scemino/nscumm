using NScumm.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NScumm.Sky
{
    class CompactEntry
    {
        public CptTypeId Type;
        public string Name;
        public object Data;

        public void Patch(byte[] data)
        {
            Patch(data, 0, 0, data.Length);
        }

        public void Patch(byte[] data, int offset, int destOffset, int length)
        {
            if (Type == CptTypeId.Compact)
            {
                ((Compact)Data).Patch(data, offset, destOffset, length);
            }
            else
            {
                var destBuf = (byte[])Data;
                Array.Copy(data, offset, destBuf, destOffset, length);
            }
        }

        public int Size
        {
            get
            {
                if (Type == CptTypeId.Compact)
                {
                    return ((Compact)Data).Size;
                }
                else
                {
                    var destBuf = (byte[])Data;
                    return destBuf.Length;
                }
            }
        }
    }

    enum CptIds : ushort
    {
        Joey = 1,
        Foster = 3,
        Text1 = 0x17,
        Text11 = 0x21,
        MenuBar = 0x2E,
        ReichDoor20 = 0x30AB,
        MoveList = 0xBD,
        TalkTableList = 0xBC
    }

    enum CptTypeId : ushort
    {
        Null = 0,
        Compact,
        TurnTab,
        AnimSeq,
        MiscBin,
        GetToTab,
        RouteBuf,
        MainList,
        NumCptTypes
    }

    [StructLayout(LayoutKind.Explicit)]
    struct MegaSet
    {
        [FieldOffset(0)]
        public ushort gridWidth;    //  0
        [FieldOffset(2)]
        public ushort colOffset;    //  1
        [FieldOffset(4)]
        public ushort colWidth;     //  2
        [FieldOffset(6)]
        public ushort lastChr;      //  3

        [FieldOffset(8)]
        public ushort animUpId;     //  4
        [FieldOffset(10)]
        public ushort animDownId;   //  5
        [FieldOffset(12)]
        public ushort animLeftId;   //  6
        [FieldOffset(14)]
        public ushort animRightId;  //  7

        [FieldOffset(16)]
        public ushort standUpId;    //  8
        [FieldOffset(18)]
        public ushort standDownId;  //  9
        [FieldOffset(20)]
        public ushort standLeftId;  // 10
        [FieldOffset(22)]
        public ushort standRightId; // 11
        [FieldOffset(24)]
        public ushort standTalkId;  // 12
        [FieldOffset(26)]
        public ushort turnTableId;  // 13
    }

    [StructLayout(LayoutKind.Explicit)]
    struct CompactCore
    {
        [FieldOffset(0)]
        public ushort logic;        //  0: Entry in logic table to run (byte as <256entries in logic table
        [FieldOffset(2)]
        public ushort status;       //  1
        [FieldOffset(4)]
        public ushort sync;         //  2: flag sent to compacts by other things

        [FieldOffset(6)]
        public ushort screen;       //  3: current screen
        [FieldOffset(8)]
        public ushort place;        //  4: so's this one
        [FieldOffset(10)]
        public ushort getToTableId; //  5: Address of how to get to things table

        [FieldOffset(12)]
        public ushort xcood;        //  6
        [FieldOffset(14)]
        public ushort ycood;        //  7

        [FieldOffset(16)]
        public ushort frame;        //  8

        [FieldOffset(18)]
        public ushort cursorText;   //  9
        [FieldOffset(20)]
        public ushort mouseOn;      // 10
        [FieldOffset(22)]
        public ushort mouseOff;     // 11
        [FieldOffset(24)]
        public ushort mouseClick;   // 12

        [FieldOffset(26)]
        public short mouseRelX;     // 13
        [FieldOffset(28)]
        public short mouseRelY;     // 14
        [FieldOffset(30)]
        public ushort mouseSizeX;   // 15
        [FieldOffset(32)]
        public ushort mouseSizeY;   // 16

        [FieldOffset(34)]
        public ushort actionScript; // 17

        [FieldOffset(36)]
        public ushort upFlag;       // 18: usually holds the Action Mode
        [FieldOffset(38)]
        public ushort downFlag;     // 19: used for passing back
        [FieldOffset(40)]
        public ushort getToFlag;    // 20: used by action script for get to attempts, also frame store (hence word)
        [FieldOffset(42)]
        public ushort flag;         // 21: a use any time flag

        [FieldOffset(44)]
        public ushort mood;         // 22: high level - stood or not

        [FieldOffset(46)]
        public ushort grafixProgId; // 23
        [FieldOffset(48)]
        public ushort grafixProgPos;// 24

        [FieldOffset(50)]
        public ushort offset;       // 25

        [FieldOffset(52)]
        public ushort mode;         // 26: which mcode block

        [FieldOffset(54)]
        public ushort baseSub;      // 27: 1st mcode block relative to start of compact
        [FieldOffset(56)]
        public ushort baseSub_off;  // 28
        [FieldOffset(58)]
        public ushort actionSub;    // 29
        [FieldOffset(60)]
        public ushort actionSub_off;// 30
        [FieldOffset(62)]
        public ushort getToSub;     // 31
        [FieldOffset(64)]
        public ushort getToSub_off; // 32
        [FieldOffset(66)]
        public ushort extraSub;     // 33
        [FieldOffset(68)]
        public ushort extraSub_off; // 34

        [FieldOffset(70)]
        public ushort dir;          // 35

        [FieldOffset(72)]
        public ushort stopScript;   // 36
        [FieldOffset(74)]
        public ushort miniBump;     // 37
        [FieldOffset(76)]
        public ushort leaving;      // 38
        [FieldOffset(78)]
        public ushort atWatch;      // 39: pointer to script variable
        [FieldOffset(80)]
        public ushort atWas;        // 40: pointer to script variable
        [FieldOffset(82)]
        public ushort alt;          // 41: alternate script
        [FieldOffset(84)]
        public ushort request;      // 42

        [FieldOffset(86)]
        public ushort spWidth_xx;   // 43
        [FieldOffset(88)]
        public ushort spColor;  // 44
        [FieldOffset(90)]
        public ushort spTextId;     // 45
        [FieldOffset(92)]
        public ushort spTime;       // 46

        [FieldOffset(94)]
        public ushort arAnimIndex;  // 47
        [FieldOffset(96)]
        public ushort turnProgId;   // 48
        [FieldOffset(98)]
        public ushort turnProgPos;  // 49

        [FieldOffset(100)]
        public ushort waitingFor;   // 50

        [FieldOffset(102)]
        public ushort arTargetX;    // 51
        [FieldOffset(104)]
        public ushort arTargetY;    // 52

        [FieldOffset(106)]
        public ushort animScratchId;// 53: data area for AR

        [FieldOffset(108)]
        public ushort megaSet;      // 54

        [FieldOffset(110 + 28 * 1)]
        public MegaSet megaSet0;    // 55
        [FieldOffset(110 + 28 * 2)]
        public MegaSet megaSet1;    //
        [FieldOffset(110 + 28 * 3)]
        public MegaSet megaSet2;    //
        [FieldOffset(110 + 28 * 4)]
        public MegaSet megaSet3;    //
    }

    class Compact
    {
        public CompactCore Core;
        private byte[] _data;

        public Compact(byte[] data)
        {
            _data = data;
            Core = ServiceLocator.Platform.ToStructure<CompactCore>(_data, 0);
        }

        public int Size { get { return _data.Length; } }

        public void Patch(byte[] data, int offset, int destOffset, int length)
        {
            Array.Copy(data, offset, _data, destOffset, length);
            Core = ServiceLocator.Platform.ToStructure<CompactCore>(_data, 0);
        }
    }

    class SkyCompact
    {
        const int SkyCptSize = 419427;
        const int C_GRID_WIDTH = 114;
        const int NEXT_MEGA_SET = (258 - C_GRID_WIDTH);

        private byte[] _asciiBuf;
        private ushort[] _dataListLen;
        private ushort _numDataLists;
        private CompactEntry[][] _compacts;
        private ushort[] _saveIds;
        private long _resetDataPos;

        public ushort[] SaveIds { get { return _saveIds; } }

        public SkyCompact()
        {
            using (var stream = OpenCompactStream())
            {
                var cptFile = new BinaryReader(stream);

                var fileVersion = cptFile.ReadUInt16();
                if (fileVersion != 0)
                    throw new NotSupportedException(string.Format("unknown \"sky.cpt\" version {0}", fileVersion));

                if (SkyCptSize != cptFile.BaseStream.Length)
                {
                    // TODO: GUI
                    //GUI::MessageDialog dialog(_("The \"sky.cpt\" file has an incorrect size.\nPlease (re)download it from www.scummvm.org"), _("OK"), NULL);
                    //dialog.runModal();
                    throw new NotSupportedException(string.Format("Incorrect sky.cpt size ({0}, expected: {1})", cptFile.BaseStream.Length, SkyCptSize));
                }

                // set the necessary data structs up...
                _numDataLists = cptFile.ReadUInt16();
                _dataListLen = cptFile.ReadUInt16s(_numDataLists);
                var rawLen = cptFile.ReadUInt32();
                var srcLen = cptFile.ReadInt32();
                var srcBuf = cptFile.ReadBytes(srcLen * 2);
                var asciiLen = cptFile.ReadInt32();
                _asciiBuf = cptFile.ReadBytes(asciiLen);
                _compacts = new CompactEntry[_numDataLists][];
                var srcPos = 0;
                var asciiPos = 0;

                // and fill them with the compact data
                for (var lcnt = 0; lcnt < _numDataLists; lcnt++)
                {
                    _compacts[lcnt] = new CompactEntry[_dataListLen[lcnt]];
                    for (var ecnt = 0; ecnt < _dataListLen[lcnt]; ecnt++)
                    {
                        var size = srcBuf.ToUInt16(srcPos); srcPos += 2;
                        if (size != 0)
                        {
                            var type = (CptTypeId)srcBuf.ToUInt16(srcPos); srcPos += 2;
                            var name = ReadName(ref asciiPos);
                            var raw = new byte[size * 2];
                            Array.Copy(srcBuf, srcPos, raw, 0, size * 2);
                            _compacts[lcnt][ecnt] = new CompactEntry { Type = type, Name = name, Data = type == CptTypeId.Compact ? (object)new Compact(raw) : raw };
                            srcPos += (size * 2);
                        }
                    }
                }

                // these compacts don't actually exist but only point to other ones...
                var numDlincs = cptFile.ReadUInt16();
                var dlincBuf = cptFile.ReadUInt16s(numDlincs * 2);
                for (var i = 0; i < numDlincs; i++)
                {
                    var dlincId = dlincBuf[i * 2];
                    var destId = dlincBuf[i * 2 + 1];
                    var name = ReadName(ref asciiPos);
                    var cDest = _compacts[destId >> 12][destId & 0xFFF];
                    _compacts[dlincId >> 12][dlincId & 0xFFF] = new CompactEntry { Name = name, Data = cDest != null ? cDest.Data : null };
                }

                // if this is v0.0288, parse this diff data
                var numDiffs = cptFile.ReadUInt16();
                var diffSize = cptFile.ReadUInt16() * 2;
                var diffBuf = cptFile.ReadBytes(diffSize);
                if (SystemVars.Instance.GameVersion.Version.Minor == 288)
                {
                    var diffPos = 0;
                    for (var cnt = 0; cnt < numDiffs; cnt++)
                    {
                        var cptId = diffBuf.ToUInt16(diffPos); diffPos += 2;
                        var offset = diffBuf.ToUInt16(diffPos); diffPos += 2;
                        var len = diffBuf.ToUInt16(diffPos); diffPos += 2;

                        var cpt = FetchCptEntry(cptId);
                        cpt.Patch(diffBuf, diffPos, offset * 2, len * 2);
                        diffPos += (len * 2);
                    }
                    Debug.Assert(diffPos == diffSize);
                }

                // these are the IDs that have to be saved into savegame files.
                var numSaveIds = cptFile.ReadUInt16();
                _saveIds = cptFile.ReadUInt16s(numSaveIds);
                _resetDataPos = cptFile.BaseStream.Position;
            }
        }

        public byte[] CreateResetData(ushort gameVersion)
        {
            using (var stream = OpenCompactStream())
            {
                var cptFile = new BinaryReader(stream);
                cptFile.BaseStream.Seek(_resetDataPos, SeekOrigin.Begin);
                var dataSize = cptFile.ReadUInt16() * 2;
                var resetBuf = cptFile.ReadBytes(dataSize);
                ushort numDiffs = cptFile.ReadUInt16();
                for (var cnt = 0; cnt < numDiffs; cnt++)
                {
                    ushort version = cptFile.ReadUInt16();
                    ushort diffFields = cptFile.ReadUInt16();
                    if (version == gameVersion)
                    {
                        for (ushort diffCnt = 0; diffCnt < diffFields; diffCnt++)
                        {
                            ushort pos = cptFile.ReadUInt16();
                            resetBuf.WriteUInt16(pos * 2, cptFile.ReadUInt16());
                        }
                        return resetBuf;
                    }
                    else
                    {
                        cptFile.BaseStream.Seek(diffFields * 2 * 2, SeekOrigin.Current);
                    }
                }
                throw new InvalidOperationException(string.Format("Unable to find reset data for Beneath a Steel Sky Version 0.0{0,3}", gameVersion));
            }
        }

        public static FieldAccess<ushort> GetSub(Compact cpt, ushort mode)
        {
            switch (mode)
            {
                case 0:
                    return new FieldAccess<ushort>(() => cpt.Core.baseSub, v => cpt.Core.baseSub = v);
                case 2:
                    return new FieldAccess<ushort>(() => cpt.Core.baseSub_off, v => cpt.Core.baseSub_off = v);
                case 4:
                    return new FieldAccess<ushort>(() => cpt.Core.actionSub, v => cpt.Core.actionSub = v);
                case 6:
                    return new FieldAccess<ushort>(() => cpt.Core.actionSub_off, v => cpt.Core.actionSub_off = v);
                case 8:
                    return new FieldAccess<ushort>(() => cpt.Core.getToSub, v => cpt.Core.getToSub = v);
                case 10:
                    return new FieldAccess<ushort>(() => cpt.Core.getToSub_off, v => cpt.Core.getToSub_off = v);
                case 12:
                    return new FieldAccess<ushort>(() => cpt.Core.extraSub, v => cpt.Core.extraSub = v);
                case 14:
                    return new FieldAccess<ushort>(() => cpt.Core.extraSub_off, v => cpt.Core.extraSub_off = v);
                default:
                    throw new InvalidOperationException(string.Format("Invalid Mode ({0})", mode));
            }
        }

        public CompactEntry FetchCptEntry(ushort cptId)
        {
            if (cptId == 0xFFFF) // is this really still necessary?
                return null;

            // TODO: debug
            //debug(8, "Loading Compact %s [%s] (%04X=%d,%d)", _cptNames[cptId >> 12][cptId & 0xFFF], nameForType(_cptTypes[cptId >> 12][cptId & 0xFFF]), cptId, cptId >> 12, cptId & 0xFFF);

            return _compacts[cptId >> 12][cptId & 0xFFF];
        }

        public Compact FetchCpt(ushort cptId)
        {
            return (Compact)FetchCptEntry(cptId).Data;
        }

        public byte[] FetchCptRaw(ushort cptId)
        {
            return (byte[])FetchCptEntry(cptId).Data;
        }

        private static Stream OpenCompactStream()
        {
            return typeof(SkyCompact).Assembly.GetManifestResourceStream(typeof(SkyCompact), "sky.cpt");
        }

        private string ReadName(ref int asciiPos)
        {
            var name = new List<byte>();
            byte c;
            while ((c = _asciiBuf[asciiPos++]) != 0)
            {
                name.Add(c);
            }
            return Encoding.UTF8.GetString(name.ToArray());
        }

        /// <summary>
        /// Gets the n'th mega set specified by a megaSet from a Compact object.
        /// </summary>
        /// <param name="cpt">Compact object.</param>
        /// <returns>the n'th mega set specified by a megaSet from a Compact object</returns>
        public static MegaSet GetMegaSet(Compact cpt)
        {
            switch (cpt.Core.megaSet)
            {
                case 0:
                    return cpt.Core.megaSet0;
                case NEXT_MEGA_SET:
                    return cpt.Core.megaSet1;
                case NEXT_MEGA_SET * 2:
                    return cpt.Core.megaSet2;
                case NEXT_MEGA_SET * 3:
                    return cpt.Core.megaSet3;
                default:
                    throw new InvalidOperationException(string.Format("Invalid MegaSet ({0})", cpt.Core.megaSet));
            }
        }

        public FieldAccess<ushort> GetCompactElem(Compact cpt, ushort off)
        {
            if (off < _getCompactField.Length)
                return new FieldAccess<ushort>(() => _getCompactField[off](cpt), v => _setCompactField[off](cpt, v));

            off -= (ushort)_getCompactField.Length;
            if (off < _getMegaSetField.Length)
                return new FieldAccess<ushort>(() => _getMegaSetField[off](cpt.Core.megaSet0), v => _setMegaSetField[off](cpt.Core.megaSet0, v));

            off -= (ushort)_getMegaSetField.Length;
            if (off < 25)
                return new FieldAccess<ushort>(() => FetchCptRaw(cpt.Core.megaSet0.turnTableId).ToUInt16(off), v => FetchCptRaw(cpt.Core.megaSet0.turnTableId).WriteUInt16(off, v));

            off -= 25;
            if (off < _getMegaSetField.Length)
                return new FieldAccess<ushort>(() => _getMegaSetField[off](cpt.Core.megaSet1), v => _setMegaSetField[off](cpt.Core.megaSet1, v));

            off -= (ushort)_getMegaSetField.Length;
            if (off < 25)
                return new FieldAccess<ushort>(() => FetchCptRaw(cpt.Core.megaSet1.turnTableId).ToUInt16(off), v => FetchCptRaw(cpt.Core.megaSet1.turnTableId).WriteUInt16(off, v));

            off -= 25;
            if (off < _getMegaSetField.Length)
                return new FieldAccess<ushort>(() => _getMegaSetField[off](cpt.Core.megaSet2), v => _setMegaSetField[off](cpt.Core.megaSet2, v));

            off -= (ushort)_getMegaSetField.Length;
            if (off < 25)
                return new FieldAccess<ushort>(() => FetchCptRaw(cpt.Core.megaSet2.turnTableId).ToUInt16(off), v => FetchCptRaw(cpt.Core.megaSet2.turnTableId).WriteUInt16(off, v));

            off -= 25;
            if (off < _getMegaSetField.Length)
                return new FieldAccess<ushort>(() => _getMegaSetField[off](cpt.Core.megaSet3), v => _setMegaSetField[off](cpt.Core.megaSet3, v));

            off -= (ushort)_getMegaSetField.Length;
            if (off < 25)
                return new FieldAccess<ushort>(() => FetchCptRaw(cpt.Core.megaSet3.turnTableId).ToUInt16(off), v => FetchCptRaw(cpt.Core.megaSet3.turnTableId).WriteUInt16(off, v));
            off -= 25;

            throw new NotSupportedException(string.Format("Offset {0:X2} out of bounds of compact", (int)(off + _getCompactField.Length + 4 * _getMegaSetField.Length + 4 * 25)));
        }

        public UShortAccess GetGrafixPtr(Compact cpt)
        {
            var gfxBaseEntry = FetchCptEntry(cpt.Core.grafixProgId);
            if (gfxBaseEntry == null)
                return null;

            return new UShortAccess((byte[])gfxBaseEntry.Data, cpt.Core.grafixProgPos * 2);
        }

        public UShortAccess GetTurnTable(Compact cpt, ushort dir)
        {
            throw new NotImplementedException();
            //MegaSet m = GetMegaSet(cpt);
            //TurnTable turnTable = (TurnTable*)FetchCpt(m.turnTableId);
            //switch (dir)
            //{
            //    case 0:
            //        return turnTable->turnTableUp;
            //    case 1:
            //        return turnTable->turnTableDown;
            //    case 2:
            //        return turnTable->turnTableLeft;
            //    case 3:
            //        return turnTable->turnTableRight;
            //    case 4:
            //        return turnTable->turnTableTalk;
            //    default:
            //        error("No TurnTable (%d) in MegaSet (%d)", dir, cpt->megaSet);
            //}
        }

        static Func<Compact, ushort> _noSupported = new Func<Compact, ushort>(c => { throw new NotSupportedException(); });
        static Func<Compact, ushort>[] _getCompactField = new Func<Compact, ushort>[]
            {
                c => c.Core.logic, _noSupported,
                c => c.Core.status, _noSupported,
                c => c.Core.sync, _noSupported,
                c => c.Core.screen, _noSupported,
                c => c.Core.place, _noSupported,
                c => c.Core.getToTableId, _noSupported, _noSupported, _noSupported,
                c => c.Core.xcood, _noSupported,
                c => c.Core.ycood, _noSupported,
                c => c.Core.frame, _noSupported,
                c => c.Core.cursorText, _noSupported,
                c => c.Core.mouseOn, _noSupported,
                c => c.Core.mouseOff, _noSupported,
                c => c.Core.mouseClick, _noSupported,
                c => (ushort)c.Core.mouseRelX, _noSupported,
                c => (ushort)c.Core.mouseRelY, _noSupported,
                c => c.Core.mouseSizeX, _noSupported,
                c => c.Core.mouseSizeY, _noSupported,
                c => c.Core.actionScript, _noSupported,
                c => c.Core.upFlag, _noSupported,
                c => c.Core.downFlag, _noSupported,
                c => c.Core.getToFlag, _noSupported,
                c => c.Core.flag, _noSupported,
                c => c.Core.mood, _noSupported,
                c => c.Core.grafixProgId, _noSupported, _noSupported, _noSupported,
                c => c.Core.offset, _noSupported,
                c => c.Core.mode, _noSupported,
                c => c.Core.flag, _noSupported,
                c => c.Core.mode, _noSupported,
                c => c.Core.baseSub, _noSupported,
                c => c.Core.baseSub_off, _noSupported,
                c => c.Core.actionSub, _noSupported,
                c => c.Core.actionSub_off, _noSupported,
                c => c.Core.getToSub, _noSupported,
                c => c.Core.getToSub_off, _noSupported,
                c => c.Core.extraSub, _noSupported,
                c => c.Core.extraSub_off, _noSupported,
                c => c.Core.dir, _noSupported,
                c => c.Core.stopScript, _noSupported,
                c => c.Core.miniBump, _noSupported,
                c => c.Core.leaving, _noSupported,
                c => c.Core.atWatch, _noSupported,
                c => c.Core.atWas, _noSupported,
                c => c.Core.alt, _noSupported,
                c => c.Core.request, _noSupported,
                c => c.Core.spWidth_xx, _noSupported,
                c => c.Core.spColor, _noSupported,
                c => c.Core.spTextId, _noSupported,
                c => c.Core.spTime, _noSupported,
                c => c.Core.arAnimIndex, _noSupported,
                c => c.Core.turnProgId, _noSupported, _noSupported, _noSupported,
                c => c.Core.waitingFor, _noSupported,
                c => c.Core.arTargetX, _noSupported,
                c => c.Core.arTargetY, _noSupported,
                c => c.Core.animScratchId, _noSupported, _noSupported, _noSupported,
                c => c.Core.megaSet, _noSupported
            };
        static Action<Compact, ushort> _setNoSupported = new Action<Compact, ushort>((c, v) => { throw new NotSupportedException(); });
        static Action<Compact, ushort>[] _setCompactField = new Action<Compact, ushort>[]
            {
                (c,v) => c.Core.logic=v, _setNoSupported,
                (c,v) => c.Core.status=v, _setNoSupported,
                (c,v) => c.Core.sync=v, _setNoSupported,
                (c,v) => c.Core.screen=v, _setNoSupported,
                (c,v) => c.Core.place=v, _setNoSupported,
                (c,v) => c.Core.getToTableId=v, _setNoSupported, _setNoSupported, _setNoSupported,
                (c,v) => c.Core.xcood=v, _setNoSupported,
                (c,v) => c.Core.ycood=v, _setNoSupported,
                (c,v) => c.Core.frame=v, _setNoSupported,
                (c,v) => c.Core.cursorText=v, _setNoSupported,
                (c,v) => c.Core.mouseOn=v, _setNoSupported,
                (c,v) => c.Core.mouseOff=v, _setNoSupported,
                (c,v) => c.Core.mouseClick=v, _setNoSupported,
                (c,v) => c.Core.mouseRelX=(short)v, _setNoSupported,
                (c,v) => c.Core.mouseRelY=(short)v, _setNoSupported,
                (c,v) => c.Core.mouseSizeX=v, _setNoSupported,
                (c,v) => c.Core.mouseSizeY=v, _setNoSupported,
                (c,v) => c.Core.actionScript=v, _setNoSupported,
                (c,v) => c.Core.upFlag=v, _setNoSupported,
                (c,v) => c.Core.downFlag=v, _setNoSupported,
                (c,v) => c.Core.getToFlag=v, _setNoSupported,
                (c,v) => c.Core.flag=v, _setNoSupported,
                (c,v) => c.Core.mood=v, _setNoSupported,
                (c,v) => c.Core.grafixProgId=v, _setNoSupported, _setNoSupported, _setNoSupported,
                (c,v) => c.Core.offset=v, _setNoSupported,
                (c,v) => c.Core.mode=v, _setNoSupported,
                (c,v) => c.Core.flag=v, _setNoSupported,
                (c,v) => c.Core.mode=v, _setNoSupported,
                (c,v) => c.Core.baseSub=v, _setNoSupported,
                (c,v) => c.Core.baseSub_off=v, _setNoSupported,
                (c,v) => c.Core.actionSub=v, _setNoSupported,
                (c,v) => c.Core.actionSub_off=v, _setNoSupported,
                (c,v) => c.Core.getToSub=v, _setNoSupported,
                (c,v) => c.Core.getToSub_off=v, _setNoSupported,
                (c,v) => c.Core.extraSub=v, _setNoSupported,
                (c,v) => c.Core.extraSub_off=v, _setNoSupported,
                (c,v) => c.Core.dir=v, _setNoSupported,
                (c,v) => c.Core.stopScript=v, _setNoSupported,
                (c,v) => c.Core.miniBump=v, _setNoSupported,
                (c,v) => c.Core.leaving=v, _setNoSupported,
                (c,v) => c.Core.atWatch=v, _setNoSupported,
                (c,v) => c.Core.atWas=v, _setNoSupported,
                (c,v) => c.Core.alt=v, _setNoSupported,
                (c,v) => c.Core.request=v, _setNoSupported,
                (c,v) => c.Core.spWidth_xx=v, _setNoSupported,
                (c,v) => c.Core.spColor=v, _setNoSupported,
                (c,v) => c.Core.spTextId=v, _setNoSupported,
                (c,v) => c.Core.spTime=v, _setNoSupported,
                (c,v) => c.Core.arAnimIndex=v, _setNoSupported,
                (c,v) => c.Core.turnProgId=v, _setNoSupported, _setNoSupported, _setNoSupported,
                (c,v) => c.Core.waitingFor=v, _setNoSupported,
                (c,v) => c.Core.arTargetX=v, _setNoSupported,
                (c,v) => c.Core.arTargetY=v, _setNoSupported,
                (c,v) => c.Core.animScratchId=v, _setNoSupported, _setNoSupported, _setNoSupported,
                (c,v) => c.Core.megaSet=v, _setNoSupported
            };

        static Func<MegaSet, ushort> _getMegaSetNotSupported = new Func<MegaSet, ushort>(c => { throw new NotSupportedException(); });
        static Func<MegaSet, ushort>[] _getMegaSetField = new Func<MegaSet, ushort>[]{
            m => m.gridWidth, _getMegaSetNotSupported,
            m => m.colOffset, _getMegaSetNotSupported,
            m => m.colWidth, _getMegaSetNotSupported,
            m => m.lastChr, _getMegaSetNotSupported,
            m => m.animUpId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported,
            m => m.animDownId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported,
            m => m.animLeftId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported,
            m => m.animRightId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported,
            m => m.standUpId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported,
            m => m.standDownId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported,
            m => m.standLeftId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported,
            m => m.standRightId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported,
            m => m.standTalkId, _getMegaSetNotSupported, _getMegaSetNotSupported, _getMegaSetNotSupported
        };

        static Action<MegaSet, ushort> _setMegaSetNotSupported = new Action<MegaSet, ushort>((c, v) => { throw new NotSupportedException(); });
        static Action<MegaSet, ushort>[] _setMegaSetField = new Action<MegaSet, ushort>[]{
            (m,v) => m.gridWidth=v, _setMegaSetNotSupported,
            (m,v) => m.colOffset=v, _setMegaSetNotSupported,
            (m,v) => m.colWidth=v, _setMegaSetNotSupported,
            (m,v) => m.lastChr=v, _setMegaSetNotSupported,
            (m,v) => m.animUpId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported,
            (m,v) => m.animDownId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported,
            (m,v) => m.animLeftId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported,
            (m,v) => m.animRightId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported,
            (m,v) => m.standUpId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported,
            (m,v) => m.standDownId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported,
            (m,v) => m.standLeftId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported,
            (m,v) => m.standRightId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported,
            (m,v) => m.standTalkId=v, _setMegaSetNotSupported, _setMegaSetNotSupported, _setMegaSetNotSupported
        };
    }
}
