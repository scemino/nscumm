//
//  ImageData.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Scumm.Graphics
{
    public class ImageData1: ImageData
    {
        public byte[] Colors = new byte[4];
        public byte[] CharMap = new byte[2048];
        public byte[] ObjectMap = new byte[2048];
        public byte[] PicMap = new byte[4096];
        public byte[] ColorMap = new byte[4096];
        public byte[] MaskMap = new byte[4096];
        public byte[] MaskChar = new byte[4096];

        public override ImageData Clone()
        {
            var data = new ImageData1 { IsBomp = IsBomp };
            data.Data = new byte[Data.Length];
            Array.Copy(Data, data.Data, Data.Length);
            foreach (var zplane in ZPlanes)
            {
                data.ZPlanes.Add(zplane.Clone());
            }
            Array.Copy(Colors, data.Colors, Colors.Length);
            Array.Copy(CharMap, data.CharMap, CharMap.Length);
            Array.Copy(ObjectMap, data.ObjectMap, ObjectMap.Length);
            Array.Copy(PicMap, data.PicMap, PicMap.Length);
            Array.Copy(ColorMap, data.ColorMap, ColorMap.Length);
            Array.Copy(MaskMap, data.MaskMap, MaskMap.Length);
            Array.Copy(MaskChar, data.MaskChar, MaskChar.Length);
            return data;
        }
    }

    public class ImageData
    {
        public List<ZPlane>  ZPlanes { get; private set; }

        public byte[] Data { get; set; }

        public bool IsBomp
        {
            get;
            set;
        }

        public ImageData()
        {
            ZPlanes = new List<ZPlane>();
            Data = new byte[0];
        }

        public virtual ImageData Clone()
        {
            var data = new ImageData{ IsBomp = IsBomp };
            data.Data = new byte[Data.Length];
            Array.Copy(Data, data.Data, Data.Length);
            foreach (var zplane in ZPlanes)
            {
                data.ZPlanes.Add(zplane.Clone());
            }
            return data;
        }
    }
}

