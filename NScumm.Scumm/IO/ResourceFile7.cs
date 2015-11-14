//
//  ResourceFile7.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm.IO
{
    class ResourceFile7: ResourceFile6
    {
        public ResourceFile7(Stream stream)
            : base(stream)
        {
        }

        public override byte[] ReadSound(MusicDriverTypes music, long offset)
        {
            GotoResourceHeader(offset);
            var tag = ToTag(_reader.ReadBytes(4));
            if (tag != "SOUN")
                throw new NotSupportedException("Expected SO block.");
            var size = _reader.ReadUInt32BigEndian();
            return _reader.ReadBytes((int)size);
        }

        protected override RoomHeader ReadRMHD()
        {
            var version = _reader.ReadUInt32();
            var header = new RoomHeader
                {
                    Width = _reader.ReadUInt16(),
                    Height = _reader.ReadUInt16(),
                    NumObjects = _reader.ReadUInt16()
                };
            return header;
        }

        protected override ObjectData ReadImageHeader()
        {
            // image header
            var version = _reader.ReadUInt32();
            var od = new ObjectData(_reader.ReadUInt16());
            var numImnn = _reader.ReadUInt16();
            od.Position = new Point(_reader.ReadInt16(), _reader.ReadInt16());
            od.Width = _reader.ReadUInt16();
            od.Height = _reader.ReadUInt16();
            var unknown1 = _reader.ReadBytes(3);
            od.ActorDir = _reader.ReadByte();
            var numHotspots = _reader.ReadUInt16();
            for (int i = 0; i < numHotspots; i++)
            {
                od.Hotspots.Add(new Point(_reader.ReadInt16(), _reader.ReadInt16()));
            }
            return od;
        }

        protected override ObjectData ReadCDHD()
        {
            var version = _reader.ReadUInt32();
            var obj = new ObjectData(_reader.ReadUInt16())
                {
                    Parent = _reader.ReadByte(),
                    ParentState = _reader.ReadByte()
                };
            obj.Flags = DrawBitmaps.AllowMaskOr;
            return obj;
        }

        protected override ObjectData Merge(ObjectData objImg, ObjectData objCode)
        {
            base.Merge(objImg, objCode);
            objCode.Width = objImg.Width;
            objCode.Height = objImg.Height;
            objCode.Position = objImg.Position;
            objCode.ActorDir = objImg.ActorDir;
            return objCode;
        }

        protected override int ReadScriptIndex()
        {
            return _reader.ReadUInt16();
        }

        protected override int GetNumGlobalScripts()
        {
            return 2000;
        }

        public override byte[] ReadCostume(long offset)
        {
            GotoResourceHeader(offset);
            var chunk = ChunkIterator5.ReadChunk(_reader);
            if (chunk.Tag != "AKOS")
                throw new NotSupportedException("Expected AKOS block.");
            return _reader.ReadBytes((int)chunk.Size - 8);
        }

        public static byte[] ReadData(byte[] input, string tag)
        {
            using (var ms = new MemoryStream(input))
            {
                var reader = new BinaryReader(ms);
                var it = new ChunkIterator5(reader, input.Length);
                while (it.MoveNext())
                {
                    if (it.Current.Tag == tag)
                    {
                        return reader.ReadBytes((int)(it.Current.Size - 8));
                    }
                }
            }
            return new byte[0];
        }

        public static long FindOffset(byte[] input, string tag)
        {
            using (var ms = new MemoryStream(input))
            {
                var reader = new BinaryReader(ms);
                var it = new ChunkIterator5(reader, input.Length);
                while (it.MoveNext())
                {
                    if (it.Current.Tag == tag)
                    {
                        return reader.BaseStream.Position;
                    }
                }
            }
            return -1;
        }

        public static T ReadData<T>(byte[] input, string tag)
        {
            return (T)ReadData(input, tag, typeof(T));
        }

        public static object ReadData(byte[] input, string tag, Type type)
        {
            var data = ReadData(input, tag);
            if (data == null)
                return null;

            return ServiceLocator.Platform.ToStructure(data, 0, type);
        }

        public static T ToStructure<T>(byte[] data, int offset)
        {
            return (T)ServiceLocator.Platform.ToStructure(data, offset, typeof(T));
        }
    }
}
