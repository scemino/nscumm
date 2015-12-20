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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Sky
{
    internal class CompactEntry
    {
        public object Data;
        public string Name;
        public CptTypeId Type;

        public int Size
        {
            get
            {
                if (Type == CptTypeId.Compact)
                {
                    return ((Compact)Data).Size;
                }
                var destBuf = (byte[])Data;
                return destBuf.Length;
            }
        }

        public byte[] Bytes
        {
            get
            {
                if (Type == CptTypeId.Compact)
                {
                    return ((Compact)Data).Bytes;
                }
                var destBuf = (byte[])Data;
                return destBuf;
            }
        }

        public int Id { get; private set; }

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

        public override string ToString()
        {
            if (Type == CptTypeId.Compact)
            {
                var c = (Compact)Data;
                StringBuilder sb = new StringBuilder();
                var noYes = new[] { "no", "yes" };
                sb.AppendFormat("Compact {0}", Name).AppendLine();
                sb.AppendFormat("logic      : {0:X4}: {1}", c.Core.logic, c.Core.logic <= 16 ? LogicTypes[c.Core.logic] : "unknown").AppendLine();
                sb.AppendFormat("status     : {0:X4}", c.Core.status).AppendLine();
                sb.AppendFormat("           : background  : {0}", noYes[(c.Core.status & Logic.ST_BACKGROUND) >> 0]).AppendLine();
                sb.AppendFormat("           : foreground  : {0}", noYes[(c.Core.status & Logic.ST_FOREGROUND) >> 1]).AppendLine();
                sb.AppendFormat("           : sort list   : {0}", noYes[(c.Core.status & Logic.ST_SORT) >> 2]).AppendLine();
                sb.AppendFormat("           : recreate    : {0}", noYes[(c.Core.status & Logic.ST_RECREATE) >> 3]).AppendLine();
                sb.AppendFormat("           : mouse       : {0}", noYes[(c.Core.status & Logic.ST_MOUSE) >> 4]).AppendLine();
                sb.AppendFormat("           : collision   : {0}", noYes[(c.Core.status & Logic.ST_COLLISION) >> 5]).AppendLine();
                sb.AppendFormat("           : logic       : {0}", noYes[(c.Core.status & Logic.ST_LOGIC) >> 6]).AppendLine();
                sb.AppendFormat("           : on grid     : {0}", noYes[(c.Core.status & Logic.ST_GRID_PLOT) >> 7]).AppendLine();
                sb.AppendFormat("           : ar priority : {0}", noYes[(c.Core.status & Logic.ST_AR_PRIORITY) >> 8]).AppendLine();
                sb.AppendFormat("sync       : {0:X4}", c.Core.sync).AppendLine();
                sb.AppendFormat("screen     : {0}", c.Core.screen).AppendLine();
                //_skyCompact->fetchCptInfo(c.Core.place, null, null, name);
                //sb.AppendFormat("place      : {0:X4}: {1}\n", c.Core.place, name).AppendLine(); 
                //_skyCompact->fetchCptInfo(c.Core.getToTableId, null, null, name);
                //sb.AppendFormat("get to tab : %04X: %s\n", c.Core.getToTableId, name).AppendLine();
                //sb.AppendFormat("x/y        : %d/%d\n", c.Core.xcood, c.Core.ycood).AppendLine();
                return sb.ToString();
            }
            return string.Format("({0}) {1}", Type, Name);
        }

        static readonly string[] LogicTypes = {
            "(none)", "SCRIPT", "AUTOROUTE", "AR_ANIM", "AR_TURNING", "ALT", "MOD_ANIM", "TURNING", "CURSOR", "TALK", "LISTEN",
            "STOPPED", "CHOOSE", "FRAMES", "PAUSE", "WAIT_SYNC", "SIMPLE MOD"
        };

        public CompactEntry(int id)
        {
            Id = id;
        }
    }

    internal enum CptIds : ushort
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

    internal enum CptTypeId : ushort
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
    internal struct MegaSet
    {
        [FieldOffset(0)]
        public ushort gridWidth; //  0
        [FieldOffset(2)]
        public ushort colOffset; //  1
        [FieldOffset(4)]
        public ushort colWidth; //  2
        [FieldOffset(6)]
        public ushort lastChr; //  3

        [FieldOffset(8)]
        public ushort animUpId; //  4
        [FieldOffset(10)]
        public ushort animDownId; //  5
        [FieldOffset(12)]
        public ushort animLeftId; //  6
        [FieldOffset(14)]
        public ushort animRightId; //  7

        [FieldOffset(16)]
        public ushort standUpId; //  8
        [FieldOffset(18)]
        public ushort standDownId; //  9
        [FieldOffset(20)]
        public ushort standLeftId; // 10
        [FieldOffset(22)]
        public ushort standRightId; // 11
        [FieldOffset(24)]
        public ushort standTalkId; // 12
        [FieldOffset(26)]
        public ushort turnTableId; // 13
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct CompactCore
    {
        [FieldOffset(0)]
        public ushort logic; //  0: Entry in logic table to run (byte as <256entries in logic table
        [FieldOffset(2)]
        public ushort status; //  1
        [FieldOffset(4)]
        public ushort sync; //  2: flag sent to compacts by other things

        [FieldOffset(6)]
        public ushort screen; //  3: current screen
        [FieldOffset(8)]
        public ushort place; //  4: so's this one
        [FieldOffset(10)]
        public ushort getToTableId; //  5: Address of how to get to things table

        [FieldOffset(12)]
        public ushort xcood; //  6
        [FieldOffset(14)]
        public ushort ycood; //  7

        [FieldOffset(16)]
        public ushort frame; //  8

        [FieldOffset(18)]
        public ushort cursorText; //  9
        [FieldOffset(20)]
        public ushort mouseOn; // 10
        [FieldOffset(22)]
        public ushort mouseOff; // 11
        [FieldOffset(24)]
        public ushort mouseClick; // 12

        [FieldOffset(26)]
        public short mouseRelX; // 13
        [FieldOffset(28)]
        public short mouseRelY; // 14
        [FieldOffset(30)]
        public ushort mouseSizeX; // 15
        [FieldOffset(32)]
        public ushort mouseSizeY; // 16

        [FieldOffset(34)]
        public ushort actionScript; // 17

        [FieldOffset(36)]
        public ushort upFlag; // 18: usually holds the Action Mode
        [FieldOffset(38)]
        public ushort downFlag; // 19: used for passing back

        [FieldOffset(40)]
        public ushort getToFlag;
        // 20: used by action script for get to attempts, also frame store (hence word)

        [FieldOffset(42)]
        public ushort flag; // 21: a use any time flag

        [FieldOffset(44)]
        public ushort mood; // 22: high level - stood or not

        [FieldOffset(46)]
        public ushort grafixProgId; // 23
        [FieldOffset(48)]
        public ushort grafixProgPos; // 24

        [FieldOffset(50)]
        public ushort offset; // 25

        [FieldOffset(52)]
        public ushort mode; // 26: which mcode block

        [FieldOffset(54)]
        public ushort baseSub; // 27: 1st mcode block relative to start of compact
        [FieldOffset(56)]
        public ushort baseSub_off; // 28
        [FieldOffset(58)]
        public ushort actionSub; // 29
        [FieldOffset(60)]
        public ushort actionSub_off; // 30
        [FieldOffset(62)]
        public ushort getToSub; // 31
        [FieldOffset(64)]
        public ushort getToSub_off; // 32
        [FieldOffset(66)]
        public ushort extraSub; // 33
        [FieldOffset(68)]
        public ushort extraSub_off; // 34

        [FieldOffset(70)]
        public ushort dir; // 35

        [FieldOffset(72)]
        public ushort stopScript; // 36
        [FieldOffset(74)]
        public ushort miniBump; // 37
        [FieldOffset(76)]
        public ushort leaving; // 38
        [FieldOffset(78)]
        public ushort atWatch; // 39: pointer to script variable
        [FieldOffset(80)]
        public ushort atWas; // 40: pointer to script variable
        [FieldOffset(82)]
        public ushort alt; // 41: alternate script
        [FieldOffset(84)]
        public ushort request; // 42

        [FieldOffset(86)]
        public ushort spWidth_xx; // 43
        [FieldOffset(88)]
        public ushort spColor; // 44
        [FieldOffset(90)]
        public ushort spTextId; // 45
        [FieldOffset(92)]
        public ushort spTime; // 46

        [FieldOffset(94)]
        public ushort arAnimIndex; // 47
        [FieldOffset(96)]
        public ushort turnProgId; // 48
        [FieldOffset(98)]
        public ushort turnProgPos; // 49

        [FieldOffset(100)]
        public ushort waitingFor; // 50

        [FieldOffset(102)]
        public ushort arTargetX; // 51
        [FieldOffset(104)]
        public ushort arTargetY; // 52

        [FieldOffset(106)]
        public ushort animScratchId; // 53: data area for AR

        [FieldOffset(108)]
        public ushort megaSet; // 54

        [FieldOffset(110 + 28 * 0)]
        public MegaSet megaSet0; // 55
        [FieldOffset(110 + 28 * 1)]
        public MegaSet megaSet1; //
        [FieldOffset(110 + 28 * 2)]
        public MegaSet megaSet2; //
        [FieldOffset(110 + 28 * 3)]
        public MegaSet megaSet3; //
    }

    internal class Compact
    {
        private byte[] _data;
        public CompactCore Core;

        public Compact(byte[] data)
        {
            _data = data;
            Core = ServiceLocator.Platform.ToStructure<CompactCore>(_data, 0);
        }

        public int Size
        {
            get { return _data.Length; }
        }

        public byte[] Bytes
        {
            get
            {
                var data = ServiceLocator.Platform.FromStructure(Core);
                Array.Copy(data, 0, _data, 0, _data.Length);
                return _data;
            }
        }

        public void Patch(byte[] data, int offset, int destOffset, int length)
        {
            _data = Bytes;
            Array.Copy(data, offset, _data, destOffset, length);
            Core = ServiceLocator.Platform.ToStructure<CompactCore>(_data, 0);
        }
    }

    class SkyCompact
    {
        private const int SkyCptSize = 419427;
        private const int CGridWidth = 114;
        private const int NextMegaSet = 258 - CGridWidth;

        private static readonly Func<Compact, ushort> NoSupported = c => { throw new NotSupportedException(); };

        private static readonly Func<Compact, ushort>[] GetCompactField =
        {
            c => c.Core.logic, NoSupported,
            c => c.Core.status, NoSupported,
            c => c.Core.sync, NoSupported,
            c => c.Core.screen, NoSupported,
            c => c.Core.place, NoSupported,
            c => c.Core.getToTableId, NoSupported, NoSupported, NoSupported,
            c => c.Core.xcood, NoSupported,
            c => c.Core.ycood, NoSupported,
            c => c.Core.frame, NoSupported,
            c => c.Core.cursorText, NoSupported,
            c => c.Core.mouseOn, NoSupported,
            c => c.Core.mouseOff, NoSupported,
            c => c.Core.mouseClick, NoSupported,
            c => (ushort) c.Core.mouseRelX, NoSupported,
            c => (ushort) c.Core.mouseRelY, NoSupported,
            c => c.Core.mouseSizeX, NoSupported,
            c => c.Core.mouseSizeY, NoSupported,
            c => c.Core.actionScript, NoSupported,
            c => c.Core.upFlag, NoSupported,
            c => c.Core.downFlag, NoSupported,
            c => c.Core.getToFlag, NoSupported,
            c => c.Core.flag, NoSupported,
            c => c.Core.mood, NoSupported,
            c => c.Core.grafixProgId, NoSupported, NoSupported, NoSupported,
            c => c.Core.offset, NoSupported,
            c => c.Core.mode, NoSupported,
            c => c.Core.baseSub, NoSupported,
            c => c.Core.baseSub_off, NoSupported,
            c => c.Core.actionSub, NoSupported,
            c => c.Core.actionSub_off, NoSupported,
            c => c.Core.getToSub, NoSupported,
            c => c.Core.getToSub_off, NoSupported,
            c => c.Core.extraSub, NoSupported,
            c => c.Core.extraSub_off, NoSupported,
            c => c.Core.dir, NoSupported,
            c => c.Core.stopScript, NoSupported,
            c => c.Core.miniBump, NoSupported,
            c => c.Core.leaving, NoSupported,
            c => c.Core.atWatch, NoSupported,
            c => c.Core.atWas, NoSupported,
            c => c.Core.alt, NoSupported,
            c => c.Core.request, NoSupported,
            c => c.Core.spWidth_xx, NoSupported,
            c => c.Core.spColor, NoSupported,
            c => c.Core.spTextId, NoSupported,
            c => c.Core.spTime, NoSupported,
            c => c.Core.arAnimIndex, NoSupported,
            c => c.Core.turnProgId, NoSupported, NoSupported, NoSupported,
            c => c.Core.waitingFor, NoSupported,
            c => c.Core.arTargetX, NoSupported,
            c => c.Core.arTargetY, NoSupported,
            c => c.Core.animScratchId, NoSupported, NoSupported, NoSupported,
            c => c.Core.megaSet, NoSupported
        };

        private static readonly Action<Compact, ushort> SetNoSupported =
            (c, v) => { throw new NotSupportedException(); };

        private static readonly Action<Compact, ushort>[] SetCompactField =
        {
            (c, v) => c.Core.logic = v, SetNoSupported,
            (c, v) => c.Core.status = v, SetNoSupported,
            (c, v) => c.Core.sync = v, SetNoSupported,
            (c, v) => c.Core.screen = v, SetNoSupported,
            (c, v) => c.Core.place = v, SetNoSupported,
            (c, v) => c.Core.getToTableId = v, SetNoSupported, SetNoSupported, SetNoSupported,
            (c, v) => c.Core.xcood = v, SetNoSupported,
            (c, v) => c.Core.ycood = v, SetNoSupported,
            (c, v) => c.Core.frame = v, SetNoSupported,
            (c, v) => c.Core.cursorText = v, SetNoSupported,
            (c, v) => c.Core.mouseOn = v, SetNoSupported,
            (c, v) => c.Core.mouseOff = v, SetNoSupported,
            (c, v) => c.Core.mouseClick = v, SetNoSupported,
            (c, v) => c.Core.mouseRelX = (short) v, SetNoSupported,
            (c, v) => c.Core.mouseRelY = (short) v, SetNoSupported,
            (c, v) => c.Core.mouseSizeX = v, SetNoSupported,
            (c, v) => c.Core.mouseSizeY = v, SetNoSupported,
            (c, v) => c.Core.actionScript = v, SetNoSupported,
            (c, v) => c.Core.upFlag = v, SetNoSupported,
            (c, v) => c.Core.downFlag = v, SetNoSupported,
            (c, v) => c.Core.getToFlag = v, SetNoSupported,
            (c, v) => c.Core.flag = v, SetNoSupported,
            (c, v) => c.Core.mood = v, SetNoSupported,
            (c, v) => c.Core.grafixProgId = v, SetNoSupported, SetNoSupported, SetNoSupported,
            (c, v) => c.Core.offset = v, SetNoSupported,
            (c, v) => c.Core.mode = v, SetNoSupported,
            (c, v) => c.Core.baseSub = v, SetNoSupported,
            (c, v) => c.Core.baseSub_off = v, SetNoSupported,
            (c, v) => c.Core.actionSub = v, SetNoSupported,
            (c, v) => c.Core.actionSub_off = v, SetNoSupported,
            (c, v) => c.Core.getToSub = v, SetNoSupported,
            (c, v) => c.Core.getToSub_off = v, SetNoSupported,
            (c, v) => c.Core.extraSub = v, SetNoSupported,
            (c, v) => c.Core.extraSub_off = v, SetNoSupported,
            (c, v) => c.Core.dir = v, SetNoSupported,
            (c, v) => c.Core.stopScript = v, SetNoSupported,
            (c, v) => c.Core.miniBump = v, SetNoSupported,
            (c, v) => c.Core.leaving = v, SetNoSupported,
            (c, v) => c.Core.atWatch = v, SetNoSupported,
            (c, v) => c.Core.atWas = v, SetNoSupported,
            (c, v) => c.Core.alt = v, SetNoSupported,
            (c, v) => c.Core.request = v, SetNoSupported,
            (c, v) => c.Core.spWidth_xx = v, SetNoSupported,
            (c, v) => c.Core.spColor = v, SetNoSupported,
            (c, v) => c.Core.spTextId = v, SetNoSupported,
            (c, v) => c.Core.spTime = v, SetNoSupported,
            (c, v) => c.Core.arAnimIndex = v, SetNoSupported,
            (c, v) => c.Core.turnProgId = v, SetNoSupported, SetNoSupported, SetNoSupported,
            (c, v) => c.Core.waitingFor = v, SetNoSupported,
            (c, v) => c.Core.arTargetX = v, SetNoSupported,
            (c, v) => c.Core.arTargetY = v, SetNoSupported,
            (c, v) => c.Core.animScratchId = v, SetNoSupported, SetNoSupported, SetNoSupported,
            (c, v) => c.Core.megaSet = v, SetNoSupported
        };

        private static readonly Func<MegaSet, ushort> GetMegaSetNotSupported =
            c => { throw new NotSupportedException(); };

        private static readonly Func<MegaSet, ushort>[] GetMegaSetField =
        {
            m => m.gridWidth, GetMegaSetNotSupported,
            m => m.colOffset, GetMegaSetNotSupported,
            m => m.colWidth, GetMegaSetNotSupported,
            m => m.lastChr, GetMegaSetNotSupported,
            m => m.animUpId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported,
            m => m.animDownId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported,
            m => m.animLeftId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported,
            m => m.animRightId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported,
            m => m.standUpId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported,
            m => m.standDownId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported,
            m => m.standLeftId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported,
            m => m.standRightId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported,
            m => m.standTalkId, GetMegaSetNotSupported, GetMegaSetNotSupported, GetMegaSetNotSupported
        };

        private static readonly Action<MegaSet, ushort> SetMegaSetNotSupported =
            (c, v) => { throw new NotSupportedException(); };

        private static readonly Action<MegaSet, ushort>[] SetMegaSetField =
        {
            (m, v) => m.gridWidth = v, SetMegaSetNotSupported,
            (m, v) => m.colOffset = v, SetMegaSetNotSupported,
            (m, v) => m.colWidth = v, SetMegaSetNotSupported,
            (m, v) => m.lastChr = v, SetMegaSetNotSupported,
            (m, v) => m.animUpId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported,
            (m, v) => m.animDownId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported,
            (m, v) => m.animLeftId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported,
            (m, v) => m.animRightId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported,
            (m, v) => m.standUpId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported,
            (m, v) => m.standDownId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported,
            (m, v) => m.standLeftId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported,
            (m, v) => m.standRightId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported,
            (m, v) => m.standTalkId = v, SetMegaSetNotSupported, SetMegaSetNotSupported, SetMegaSetNotSupported
        };

        private readonly byte[] _asciiBuf;
        private readonly CompactEntry[][] _compacts;
        private readonly long _resetDataPos;

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
                    throw new NotSupportedException(string.Format("Incorrect sky.cpt size ({0}, expected: {1})",
                        cptFile.BaseStream.Length, SkyCptSize));
                }

                // set the necessary data structs up...
                var numDataLists = cptFile.ReadUInt16();
                var dataListLen = cptFile.ReadUInt16s(numDataLists);
                var rawLen = cptFile.ReadUInt32();
                var srcLen = cptFile.ReadInt32();
                var srcBuf = cptFile.ReadBytes(srcLen * 2);
                var asciiLen = cptFile.ReadInt32();
                _asciiBuf = cptFile.ReadBytes(asciiLen);
                _compacts = new CompactEntry[numDataLists][];
                var srcPos = 0;
                var asciiPos = 0;

                // and fill them with the compact data
                for (var lcnt = 0; lcnt < numDataLists; lcnt++)
                {
                    _compacts[lcnt] = new CompactEntry[dataListLen[lcnt]];
                    for (var ecnt = 0; ecnt < dataListLen[lcnt]; ecnt++)
                    {
                        var size = srcBuf.ToUInt16(srcPos);
                        srcPos += 2;
                        if (size == 0) continue;

                        var type = (CptTypeId)srcBuf.ToUInt16(srcPos);
                        srcPos += 2;
                        var name = ReadName(ref asciiPos);
                        var raw = new byte[size * 2];
                        Array.Copy(srcBuf, srcPos, raw, 0, size * 2);
                        _compacts[lcnt][ecnt] = new CompactEntry((lcnt << 12) | ecnt)
                        {
                            Type = type,
                            Name = name,
                            Data = type == CptTypeId.Compact ? (object)new Compact(raw) : raw
                        };
                        srcPos += size * 2;
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
                    _compacts[dlincId >> 12][dlincId & 0xFFF] = new CompactEntry(dlincId)
                    {
                        Name = name,
                        Data = cDest != null ? cDest.Data : null
                    };
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
                        var cptId = diffBuf.ToUInt16(diffPos);
                        diffPos += 2;
                        var offset = diffBuf.ToUInt16(diffPos);
                        diffPos += 2;
                        var len = diffBuf.ToUInt16(diffPos);
                        diffPos += 2;

                        var cpt = FetchCptEntry(cptId);
                        cpt.Patch(diffBuf, diffPos, offset * 2, len * 2);
                        diffPos += len * 2;
                    }
                    System.Diagnostics.Debug.Assert(diffPos == diffSize);
                }

                // these are the IDs that have to be saved into savegame files.
                var numSaveIds = cptFile.ReadUInt16();
                SaveIds = cptFile.ReadUInt16s(numSaveIds);
                _resetDataPos = cptFile.BaseStream.Position;
            }
        }

        public ushort[] SaveIds { get; }

        public byte[] CreateResetData(ushort gameVersion)
        {
            using (var stream = OpenCompactStream())
            {
                var cptFile = new BinaryReader(stream);
                cptFile.BaseStream.Seek(_resetDataPos, SeekOrigin.Begin);
                var dataSize = cptFile.ReadUInt16() * 2;
                var resetBuf = cptFile.ReadBytes(dataSize);
                var numDiffs = cptFile.ReadUInt16();
                for (var cnt = 0; cnt < numDiffs; cnt++)
                {
                    var version = cptFile.ReadUInt16();
                    var diffFields = cptFile.ReadUInt16();
                    if (version == gameVersion)
                    {
                        for (ushort diffCnt = 0; diffCnt < diffFields; diffCnt++)
                        {
                            var pos = cptFile.ReadUInt16();
                            resetBuf.WriteUInt16(pos * 2, cptFile.ReadUInt16());
                        }
                        return resetBuf;
                    }
                    cptFile.BaseStream.Seek(diffFields * 2 * 2, SeekOrigin.Current);
                }
                throw new InvalidOperationException(
                    string.Format("Unable to find reset data for Beneath a Steel Sky Version 0.0{0,3}", gameVersion));
            }
        }

        public static FieldAccess<ushort> GetSub(Compact cpt, int mode)
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
            var entry = FetchCptEntry(cptId);
            return entry?.Bytes;
        }

        public ushort GetId(Compact cpt)
        {
            var len = _compacts.GetLength(0);
            for (int i = 0; i < len; i++)
            {
                var len2 = _compacts[i].Length;
                for (int j = 0; j < len2; j++)
                {
                    if (Equals(_compacts[i][j]?.Data, cpt))
                    {
                        return (ushort)((i << 12) | j);
                    }
                }
            }
            return 0;
        }

        /// <summary>
        ///     Gets the n'th mega set specified by a megaSet from a Compact object.
        /// </summary>
        /// <param name="cpt">Compact object.</param>
        /// <returns>the n'th mega set specified by a megaSet from a Compact object</returns>
        public static MegaSet GetMegaSet(Compact cpt)
        {
            switch (cpt.Core.megaSet)
            {
                case 0:
                    return cpt.Core.megaSet0;
                case NextMegaSet:
                    return cpt.Core.megaSet1;
                case NextMegaSet * 2:
                    return cpt.Core.megaSet2;
                case NextMegaSet * 3:
                    return cpt.Core.megaSet3;
                default:
                    throw new InvalidOperationException(string.Format("Invalid MegaSet ({0})", cpt.Core.megaSet));
            }
        }

        public FieldAccess<ushort> GetCompactElem(Compact cpt, ushort off)
        {
            ushort compactSize = (ushort)GetCompactField.Length;
            ushort megasetSize = (ushort)GetMegaSetField.Length;
            ushort turntableSize = 100;

            if (off < compactSize)
                return new FieldAccess<ushort>(() => GetCompactField[off](cpt), v => SetCompactField[off](cpt, v));
            off -= compactSize;

            if (off < megasetSize)
                return new FieldAccess<ushort>(() => GetMegaSetField[off](cpt.Core.megaSet0),
                    v => SetMegaSetField[off](cpt.Core.megaSet0, v));

            off -= megasetSize;
            if (off < turntableSize)
                return new FieldAccess<ushort>(() => FetchCptRaw(cpt.Core.megaSet0.turnTableId).ToUInt16(off / 2),
                    v => FetchCptRaw(cpt.Core.megaSet0.turnTableId).WriteUInt16(off / 2, v));

            off -= turntableSize;
            if (off < megasetSize)
                return new FieldAccess<ushort>(() => GetMegaSetField[off](cpt.Core.megaSet1),
                    v => SetMegaSetField[off](cpt.Core.megaSet1, v));

            off -= megasetSize;
            if (off < turntableSize)
                return new FieldAccess<ushort>(() => FetchCptRaw(cpt.Core.megaSet1.turnTableId).ToUInt16(off / 2),
                    v => FetchCptRaw(cpt.Core.megaSet1.turnTableId).WriteUInt16(off / 2, v));

            off -= turntableSize;
            if (off < megasetSize)
                return new FieldAccess<ushort>(() => GetMegaSetField[off](cpt.Core.megaSet2),
                    v => SetMegaSetField[off](cpt.Core.megaSet2, v));

            off -= megasetSize;
            if (off < turntableSize)
                return new FieldAccess<ushort>(() => FetchCptRaw(cpt.Core.megaSet2.turnTableId).ToUInt16(off / 2),
                    v => FetchCptRaw(cpt.Core.megaSet2.turnTableId).WriteUInt16(off / 2, v));

            off -= turntableSize;
            if (off < megasetSize)
                return new FieldAccess<ushort>(() => GetMegaSetField[off](cpt.Core.megaSet3),
                    v => SetMegaSetField[off](cpt.Core.megaSet3, v));

            off -= megasetSize;
            if (off < turntableSize)
                return new FieldAccess<ushort>(() => FetchCptRaw(cpt.Core.megaSet3.turnTableId).ToUInt16(off / 2),
                    v => FetchCptRaw(cpt.Core.megaSet3.turnTableId).WriteUInt16(off / 2, v));
            off -= turntableSize;

            throw new NotSupportedException(string.Format("Offset {0:X2} out of bounds of compact",
                off + compactSize + 4 * megasetSize + 4 * 100));
        }

        public UShortAccess GetGrafixPtr(Compact cpt)
        {
            var gfxBase = FetchCptRaw(cpt.Core.grafixProgId);
            if (gfxBase == null)
                return null;

            return new UShortAccess(gfxBase, cpt.Core.grafixProgPos * 2);
        }

        public UShortAccess GetTurnTable(Compact cpt, ushort dir)
        {
            if (dir > 4) throw new ArgumentOutOfRangeException("dir", string.Format("No TurnTable ({0}) in MegaSet ({1})", dir, cpt.Core.megaSet));

            var m = GetMegaSet(cpt);
            var turnTable = FetchCptRaw(m.turnTableId);
            return new UShortAccess(turnTable, 10 * dir);
        }

        /// <summary>
        /// Needed for some workaround where the engine has to check if it's currently processing joey, for example
        /// </summary>
        /// <param name="cpt"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool CptIsId(Compact cpt, ushort id)
        {
            return cpt == FetchCpt(id);
        }

        private static Stream OpenCompactStream()
        {
            var stream = ServiceLocator.FileStorage.OpenContent("sky.cpt");
            return stream;
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
    }
}