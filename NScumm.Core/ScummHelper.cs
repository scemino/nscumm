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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace NScumm.Core
{
    public static class ScummHelper
    {
        public static string GetString(this System.Text.Encoding encoding, byte[] bytes)
        {
            if (bytes == null)
                return null;
            return encoding.GetString(bytes, 0, bytes.Length);
        }

        public static string ToWin1252String(this byte[] bytes)
        {
            if (bytes == null)
                return null;
            var chars = bytes.Select(c => (char)c).ToArray();
            return new string(chars);
        }

        public static void ForEach<T>(this T[] array, Action<T> action)
        {
            foreach (var item in array)
            {
                action(item);
            }
        }

        public static TOut[] ConvertAll<TIn, TOut>(this TIn[] array, Func<TIn, TOut> action)
        {
            var results = new TOut[array.Length];
            for (var i = 0; i < array.Length; i++)
            {
                var item = array[i];
                results[i] = action(item);
            }
            return results;
        }

        public static int Clip(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static T Clip<T>(T value, T min, T max) where T : IComparable
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        public static string NormalizePath(string path)
        {
            var dir = ServiceLocator.FileStorage.GetDirectoryName(path);
            return (from file in ServiceLocator.FileStorage.EnumerateFiles(dir)
                where string.Equals(file, path, StringComparison.OrdinalIgnoreCase)
                select file).FirstOrDefault();
        }

        public static string LocatePath(string directory, string filename)
        {
            return (from file in ServiceLocator.FileStorage.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    let fn = ServiceLocator.FileStorage.GetFileName(file)
                    where string.Equals(fn, filename, StringComparison.OrdinalIgnoreCase)
                    select file).FirstOrDefault();
        }

        public static byte[] ToByteArray(this BitArray bits)
        {
            var numBytes = bits.Length / 8;
            if (bits.Length % 8 != 0)
                numBytes++;

            var bytes = new byte[numBytes];
            int byteIndex = 0, bitIndex = 0;

            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                    bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }

            return bytes;
        }

        public static int ToTicks(TimeSpan time)
        {
            return (int)(time.TotalSeconds * 60);
        }

        public static TimeSpan ToTimeSpan(int ticks)
        {
            var t = (double)ticks;
            return TimeSpan.FromSeconds(t / 60.0);
        }

        public static int NewDirToOldDir(int dir)
        {
            if (dir >= 71 && dir <= 109)
                return 1;
            if (dir >= 109 && dir <= 251)
                return 2;
            if (dir >= 251 && dir <= 289)
                return 0;
            return 3;
        }

        public static int RevBitMask(int x)
        {
            return 0x80 >> x;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        public static void AssertRange(long min, long value, long max, string desc)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"{desc} {value} is out of bounds ({min},{max})");
            }
        }

        /// <summary>
        /// Convert a simple direction to an angle.
        /// </summary>
        /// <returns>The simple dir.</returns>
        /// <param name="dirType">Dir type.</param>
        /// <param name="dir">Dir.</param>
        public static int FromSimpleDir(int dirType, int dir)
        {
            if (dirType != 0)
                return dir * 45;
            return dir * 90;
        }

        public static int OldDirToNewDir(int dir)
        {
            if (dir < 0 && dir > 3)
                throw new ArgumentOutOfRangeException(nameof(dir), "Invalid direction");
            int[] newDirTable = { 270, 90, 180, 0 };
            return newDirTable[dir];
        }

        public static uint[] ReadUInt32s(this BinaryReader reader, int count)
        {
            var values = new uint[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = reader.ReadUInt32();
            }
            return values;
        }

        public static ushort ReadUInt16BigEndian(this BinaryReader reader)
        {
            return SwapBytes(reader.ReadUInt16());
        }

        public static short ReadInt16BigEndian(this BinaryReader reader)
        {
            return SwapBytes(reader.ReadInt16());
        }

        public static uint ReadUInt32BigEndian(this BinaryReader reader)
        {
            return SwapBytes(reader.ReadUInt32());
        }

        public static int ReadInt32BigEndian(this BinaryReader reader)
        {
            return (int)reader.ReadUInt32BigEndian();
        }

        public static int[] ReadInt32s(this BinaryReader reader, int count)
        {
            var values = new int[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = reader.ReadInt32();
            }
            return values;
        }

        public static sbyte[] ReadSBytes(this BinaryReader reader, int count)
        {
            var values = new sbyte[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = reader.ReadSByte();
            }
            return values;
        }

        public static short[] ReadInt16s(this BinaryReader reader, int count)
        {
            var values = new short[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = reader.ReadInt16();
            }
            return values;
        }

        public static ushort[] ReadUInt16s(this BinaryReader reader, int count)
        {
            var values = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = reader.ReadUInt16();
            }
            return values;
        }

        public static int[][] ReadMatrixUInt16(this BinaryReader reader, int count1, int count2)
        {
            var values = new int[count2][];

            for (var i = 0; i < count2; i++)
            {
                values[i] = new int[count1];
                for (var j = 0; j < count1; j++)
                {
                    values[i][j] = reader.ReadUInt16();
                }
            }
            return values;
        }

        public static int[][] ReadMatrixInt32(this BinaryReader reader, int count1, int count2)
        {
            var values = new int[count2][];

            for (var i = 0; i < count2; i++)
            {
                values[i] = new int[count1];
                for (var j = 0; j < count1; j++)
                {
                    values[i][j] = reader.ReadInt32();
                }
            }
            return values;
        }

        public static byte[][] ReadMatrixBytes(this BinaryReader reader, int count1, int count2)
        {
            var values = new byte[count2][];

            for (var i = 0; i < count2; i++)
            {
                values[i] = new byte[count1];
                for (var j = 0; j < count1; j++)
                {
                    values[i][j] = reader.ReadByte();
                }
            }
            return values;
        }

        public static void Write(this BinaryWriter writer, uint[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteMatrixUInt16(this BinaryWriter writer, int[][] values, int count1, int count2)
        {
            for (var i = 0; i < count2; i++)
            {
                for (var j = 0; j < count1; j++)
                {
                    writer.Write((ushort)values[i][j]);
                }
            }
        }

        public static void WriteMatrixInt32(this BinaryWriter writer, int[][] values, int count1, int count2)
        {
            for (var i = 0; i < count2; i++)
            {
                for (var j = 0; j < count1; j++)
                {
                    writer.Write(values[i][j]);
                }
            }
        }

        public static void WriteMatrixBytes(this BinaryWriter writer, byte[][] values, int count1, int count2)
        {
            for (var i = 0; i < count2; i++)
            {
                for (var j = 0; j < count1; j++)
                {
                    writer.Write(values[i][j]);
                }
            }
        }

        public static void WriteMatrixBytes(this BinaryWriter writer, byte[,] values, int count1, int count2)
        {
            for (var i = 0; i < count2; i++)
            {
                for (var j = 0; j < count1; j++)
                {
                    writer.Write(values[i, j]);
                }
            }
        }

        public static void WriteInt32(this BinaryWriter writer, int value)
        {
            writer.Write(value);
        }

        public static void WriteUInt32(this BinaryWriter writer, uint value)
        {
            writer.Write(value);
        }

        public static void WriteInt16(this BinaryWriter writer, int value)
        {
            writer.Write((short)value);
        }

        public static void WriteUInt16(this BinaryWriter writer, bool value)
        {
            var value16 = value ? (ushort)1 : (ushort)0;
            writer.Write(value16);
        }

        public static void WriteUInt16(this BinaryWriter writer, int value)
        {
            var value16 = (ushort)value;
            writer.Write(value16);
        }

        public static void WriteUInt16(this BinaryWriter writer, uint value)
        {
            var value16 = (ushort)value;
            writer.Write(value16);
        }

        public static void WriteByte(this BinaryWriter writer, bool value)
        {
            var value8 = value ? (byte)1 : (byte)0;
            writer.Write(value8);
        }

        public static void WriteByte(this BinaryWriter writer, int value)
        {
            var value8 = (byte)value;
            writer.Write(value8);
        }

        public static void WriteBytes(this BinaryWriter writer, byte[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteBytes(this BinaryWriter writer, ushort[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write((byte)values[i]);
            }
        }

        public static void WriteSBytes(this BinaryWriter writer, sbyte[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteUInt16s(this BinaryWriter writer, ushort[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteUInt32s(this BinaryWriter writer, uint[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteInt16s(this BinaryWriter writer, short[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteInt16s(this BinaryWriter writer, int[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write((short)values[i]);
            }
        }

        public static void WriteInt32s(this BinaryWriter writer, int[] values, int count)
        {
            for (var i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteUInt16BigEndian(this BinaryWriter writer, ushort value)
        {
            writer.Write(SwapBytes(value));
        }

        public static void WriteUInt32BigEndian(this BinaryWriter writer, uint value)
        {
            writer.Write(SwapBytes(value));
        }

        public static byte[] GetBytesBigEndian(uint value)
        {
            return BitConverter.GetBytes(SwapBytes(value));
        }

        public static ushort SwapBytes(ushort value)
        {
            return (ushort)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        public static short SwapBytes(short value)
        {
            return (short)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        public static uint SwapBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 | (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        public static uint MakeTag(char a0, char a1, char a2, char a3)
        {
            return (uint)(a3 | (a2 << 8) | (a1 << 16) | (a0 << 24));
        }

        public static ushort ToUInt16(this byte[] value, int startIndex = 0)
        {
            return BitConverter.ToUInt16(value, startIndex);
        }

        public static short ToInt16(this byte[] value, int startIndex = 0)
        {
            return BitConverter.ToInt16(value, startIndex);
        }

        public static short ToInt16BigEndian(this byte[] value, int startIndex = 0)
        {
            return (short)ToUInt16BigEndian(value, startIndex);
        }

        public static ushort ToUInt16BigEndian(this byte[] value, int startIndex = 0)
        {
            return SwapBytes(ToUInt16(value, startIndex));
        }

        public static uint ToUInt24(this byte[] value, int startIndex = 0)
        {
            return (uint)((value[startIndex + 2] << 16) | (value[startIndex + 1] << 8) | value[startIndex]);
        }

        public static void WriteUInt16(this byte[] array, int startIndex, ushort value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Copy(data, 0, array, startIndex, 2);
        }

        public static void WriteInt16(this byte[] array, int startIndex, short value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Copy(data, 0, array, startIndex, 2);
        }

        public static void WriteInt16BigEndian(this byte[] array, int startIndex, short value)
        {
            var data = BitConverter.GetBytes(SwapBytes(value));
            Array.Copy(data, 0, array, startIndex, 2);
        }

        public static void WriteUInt16BigEndian(this byte[] array, int startIndex, ushort value)
        {
            var data = BitConverter.GetBytes(SwapBytes(value));
            Array.Copy(data, 0, array, startIndex, 2);
        }

        public static uint ToUInt32(this byte[] value, int startIndex = 0)
        {
            return BitConverter.ToUInt32(value, startIndex);
        }

        public static int ToInt32(this byte[] value, int startIndex = 0)
        {
            return BitConverter.ToInt32(value, startIndex);
        }

        public static uint ToUInt32BigEndian(this byte[] value, int startIndex = 0)
        {
            return SwapBytes(ToUInt32(value, startIndex));
        }

        public static void Set(this byte[] array, int startIndex, byte value, int length)
        {
            for (var i = startIndex; i < startIndex + length; i++)
            {
                array[i] = value;
            }
        }

        public static void Set<T>(this T[] array, int startIndex, T value, int length)
        {
            for (var i = startIndex; i < startIndex + length; i++)
            {
                array[i] = value;
            }
        }

        public static void WriteUInt32(this byte[] array, int startIndex, uint value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Copy(data, 0, array, startIndex, 4);
        }

        public static void WriteInt32(this byte[] array, int startIndex, int value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Copy(data, 0, array, startIndex, 4);
        }

        public static void WriteUInt32BigEndian(this byte[] array, int startIndex, uint value)
        {
            var data = BitConverter.GetBytes(SwapBytes(value));
            Array.Copy(data, 0, array, startIndex, 4);
        }

        public static int ToInt32BigEndian(this byte[] value, int startIndex = 0)
        {
            return (int)SwapBytes(ToUInt32(value, startIndex));
        }

        public static string ToText(this byte[] value, int startIndex = 0, int count = 4)
        {
            return System.Text.Encoding.UTF8.GetString(value, startIndex, count);
        }

        public static string ReadTag(this BinaryReader reader)
        {
            return System.Text.Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4);
        }

        public static string GetText(this BytePtr value, int startIndex = 0, int count = -1)
        {
            return GetText(value.Data, value.Offset + startIndex, count);
        }

        public static string GetText(this byte[] value, int startIndex = 0, int count = -1)
        {
            var data = new List<byte>();
            count = count < 0 ? value.Length : count;
            for (var i = startIndex; i < startIndex + count && value[i] != 0; i++)
            {
                data.Add(value[i]);
            }
            return System.Text.Encoding.UTF8.GetString(data.ToArray());
        }

        public static string GetRawText(this BytePtr value, int startIndex = 0, int count = -1)
        {
            return GetRawText(value.Data, value.Offset + startIndex, count);
        }

        public static string GetRawText(this byte[] value, int startIndex = 0, int count = -1)
        {
            var data = new List<char>();
            count = count < 0 ? value.Length : count;
            for (var i = startIndex; i < startIndex + count && value[i] != 0; i++)
            {
                data.Add((char)value[i]);
            }
            return new string(data.ToArray());
        }

        public static byte[] GetBytes(this string value)
        {
            return value.ToCharArray().Select(c => (byte)c).ToArray();
        }

        public static int GetTextLength(this byte[] value, int startIndex = 0)
        {
            var length = 0;
            for (var i = startIndex; i < value.Length && value[i] != 0; i++)
            {
                length++;
            }
            return length;
        }

        public static int GetStringLength(byte[] data, int gameVersion)
        {
            var length = 0;
            for (var i = 0; i < data.Length; i++)
            {
                var character = data[i];
                length++;
                if (character == 0xFF)
                {
                    character = data[i];
                    length++;
                    if (character != 1 && character != 2 && character != 3 && character != 8)
                    {
                        var count = gameVersion == 8 ? 4 : 2;
                        i += count;
                        length += count;
                    }
                }
                length++;
            }
            return length;
        }

        public static void ArraySet<T>(T[] array, T value, int index)
        {
            for (var i = index; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        public static bool ArrayEquals(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (src[srcOffset + i] != dst[dstOffset + i])
                    return false;
            }
            return true;
        }
    }
}
