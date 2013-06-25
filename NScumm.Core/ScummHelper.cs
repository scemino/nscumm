using System;
using System.IO;

namespace NScumm.Core
{
    static class ScummHelper
    {
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
            return (0x80 >> (x));
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public static void AssertRange(int min, int value, int max, string desc)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException("value", string.Format("{0} {1} is out of bounds ({2},{3})", desc, value, min, max));
            }
        }

        public static int OldDirToNewDir(int dir)
        {
            if (dir < 0 && dir > 3) throw new ArgumentOutOfRangeException("dir", dir, "Invalid direction");
            int[] new_dir_table = new int[4] { 270, 90, 180, 0 };
            return new_dir_table[dir];
        }

        public static uint[] ReadUInt32s(this BinaryReader reader, int count)
        {
            uint[] values = new uint[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = reader.ReadUInt32();
            }
            return values;
        }

        public static int[] ReadInt32s(this BinaryReader reader, int count)
        {
            int[] values = new int[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = reader.ReadInt32();
            }
            return values;
        }

        public static short[] ReadInt16s(this BinaryReader reader, int count)
        {
            short[] values = new short[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = reader.ReadInt16();
            }
            return values;
        }

        public static ushort[] ReadUInt16s(this BinaryReader reader, int count)
        {
            ushort[] values = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = reader.ReadUInt16();
            }
            return values;
        }

        public static int[][] ReadMatrixUInt16(this BinaryReader reader, int count1, int count2)
        {
            int[][] values = new int[count2][];

            for (int i = 0; i < count2; i++)
            {
                values[i] = new int[count1];
                for (int j = 0; j < count1; j++)
                {
                    values[i][j] = reader.ReadUInt16();
                }
            }
            return values;
        }

        public static int[][] ReadMatrixInt32(this BinaryReader reader, int count1, int count2)
        {
            int[][] values = new int[count2][];

            for (int i = 0; i < count2; i++)
            {
                values[i] = new int[count1];
                for (int j = 0; j < count1; j++)
                {
                    values[i][j] = reader.ReadInt32();
                }
            }
            return values;
        }

        public static byte[][] ReadMatrixBytes(this BinaryReader reader, int count1, int count2)
        {
            byte[][] values = new byte[count2][];

            for (int i = 0; i < count2; i++)
            {
                values[i] = new byte[count1];
                for (int j = 0; j < count1; j++)
                {
                    values[i][j] = reader.ReadByte();
                }
            }
            return values;
        }

        public static void Write(this BinaryWriter writer, uint[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteMatrixUInt16(this BinaryWriter writer, int[][] values, int count1, int count2)
        {
            for (int i = 0; i < count2; i++)
            {
                for (int j = 0; j < count1; j++)
                {
                    writer.Write((ushort)values[i][j]);
                }
            }
        }

        public static void WriteMatrixInt32(this BinaryWriter writer, int[][] values, int count1, int count2)
        {
            for (int i = 0; i < count2; i++)
            {
                for (int j = 0; j < count1; j++)
                {
                    writer.Write(values[i][j]);
                }
            }
        }

        public static void WriteMatrixBytes(this BinaryWriter writer, byte[][] values, int count1, int count2)
        {
            for (int i = 0; i < count2; i++)
            {
                for (int j = 0; j < count1; j++)
                {
                    writer.Write(values[i][j]);
                }
            }
        }

        public static void WriteMatrixBytes(this BinaryWriter writer, byte[,] values, int count1, int count2)
        {
            for (int i = 0; i < count2; i++)
            {
                for (int j = 0; j < count1; j++)
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
            ushort value16 = value ? (ushort)1 : (ushort)0;
            writer.Write(value16);
        }

        public static void WriteUInt16(this BinaryWriter writer, int value)
        {
            ushort value16 = (ushort)value;
            writer.Write(value16);
        }

        public static void WriteUInt16(this BinaryWriter writer, uint value)
        {
            ushort value16 = (ushort)value;
            writer.Write(value16);
        }

        public static void WriteByte(this BinaryWriter writer, bool value)
        {
            byte value8 = value ? (byte)1 : (byte)0;
            writer.Write(value8);
        }

        public static void WriteByte(this BinaryWriter writer, int value)
        {
            byte value8 = (byte)value;
            writer.Write(value8);
        }

        public static void WriteBytes(this BinaryWriter writer, byte[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteBytes(this BinaryWriter writer, ushort[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                writer.Write((byte)values[i]);
            }
        }

        public static void WriteUInt16s(this BinaryWriter writer, ushort[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteUInt32s(this BinaryWriter writer, uint[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteInt16s(this BinaryWriter writer, short[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteInt16s(this BinaryWriter writer, int[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                writer.Write((short)values[i]);
            }
        }

        public static void WriteInt32s(this BinaryWriter writer, int[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                writer.Write(values[i]);
            }
        }

        public static void WriteUInt32BigEndian(this BinaryWriter writer, uint value)
        {
            writer.Write(SwapBytes(value));
        }

        public static ushort SwapBytes(ushort value)
        {
            return (ushort)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        public static uint SwapBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 | (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        public static uint MakeTag(char a0, char a1, char a2, char a3)
        {
            return ((uint)((a3) | ((a2) << 8) | ((a1) << 16) | ((a0) << 24)));
        }

    }
}
