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
using NScumm.Core.Graphics;
using NScumm.Core;
using NScumm.Core.Common;

namespace NScumm.Sci.Graphics
{
    class CelInfo
    {
        public short width, height;
        public short scriptWidth, scriptHeight;
        public short displaceX;
        public short displaceY;
        public byte clearKey;
        public ushort offsetEGA;
        public uint offsetRLE;
        public uint offsetLiteral;
        public byte[] rawBitmap;
    }

    class LoopInfo
    {
        public bool mirrorFlag;
        public ushort celCount;
        public CelInfo[] cel;
    }

    enum Sci32ViewNativeResolution
    {
        NONE = -1,
        R320x200 = 0,
        R640x480 = 1,
        R640x400 = 2
    }

    /// <summary>
    /// View class, handles loading of view resources and drawing contained cels to screen
    ///  every view resource has its own instance of this class
    /// </summary>
    internal class GfxView
    {
        private const int SCI_VIEW_EGAMAPPING_SIZE = 16;
        private const int SCI_VIEW_EGAMAPPING_COUNT = 8;

        private GfxPalette _palette;
        private ResourceManager _resMan;
        private GfxScreen _screen;
        // this is not set for some views in laura bow 2 floppy and signals that the view shall never get scaled
        //  even if scaleX/Y are set (inside kAnimate)
        private bool _isScaleable;
        // specifies scaling resolution for SCI2 views (see gk1/windows, Wolfgang in room 720)
        private Sci32ViewNativeResolution _sci2ScaleRes;
        private ushort _loopCount;
        private LoopInfo[] _loop;
        // this is set for sci0early to adjust for the getCelRect() change
        private short _adjustForSci0Early;
        private int _resourceId;
        private GfxCoordAdjuster _coordAdjuster;
        private ResourceManager.ResourceSource.Resource _resource;
        private byte[] _resourceData;
        private int _resourceSize;
        private bool _embeddedPal;
        private Palette _viewPalette;
        private ByteAccess _EGAmapping;

        static readonly byte[] EGAmappingStraight = {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
        };

        static readonly byte[] ViewInject_LauraBow2_Dual = {
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x37,0x37,0x37,0x37,0x37,0x00,0x00,0x37,0x37,0x00,0x00,0x37,0x37,0x00,0x00,0x00,0x37,0x37,0x37,0x00,0x00,0x37,0x37,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x37,0x08,0x08,0x08,0x08,0x37,0x00,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x00,0x37,0x08,0x08,0x08,0x37,0x00,0x37,0x08,0x32,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x37,0x08,0x33,0x32,0x37,0x08,0x00,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x32,0x32,0x33,0x08,0x32,0x37,0x08,0x32,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x33,0x37,0x37,0x08,0x32,0x37,0x08,0x32,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x08,0x08,0x08,0x08,0x32,0x37,0x08,0x32,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x32,0x32,0x33,0x08,0x32,0x37,0x08,0x32,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x37,0x08,0x33,0x37,0x37,0x08,0x32,0x37,0x08,0x33,0x37,0x37,0x08,0x32,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x33,0x37,0x37,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x37,0x08,0x08,0x08,0x08,0x32,0x00,0x00,0x37,0x08,0x08,0x08,0x32,0x00,0x37,0x08,0x32,0x00,0x37,0x08,0x32,0x37,0x08,0x08,0x08,0x08,0x32,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x32,0x32,0x32,0x32,0x00,0x00,0x00,0x00,0x32,0x32,0x32,0x00,0x00,0x00,0x32,0x32,0x00,0x00,0x32,0x32,0x00,0x32,0x32,0x32,0x32,0x32,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00
        };

        static readonly byte[] ViewInject_KingsQuest6_Dual1 = {
            0x17,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x13,
            0x17,0x17,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x16,0x13,0x11,
            0x16,0x17,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x17,0x16,0x16,0x16,0x16,0x13,0x13,0x17,0x16,0x13,0x13,0x17,0x16,0x13,0x13,0x13,0x17,0x16,0x16,0x13,0x13,0x17,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x16,0x10,0x10,0x10,0x10,0x16,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x10,0x10,0x16,0x13,0x16,0x10,0x11,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x16,0x10,0x11,0x11,0x16,0x10,0x11,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x10,0x11,0x11,0x13,0x10,0x11,0x16,0x10,0x11,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x10,0x13,0x16,0x16,0x10,0x11,0x16,0x10,0x11,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x10,0x10,0x10,0x10,0x10,0x11,0x16,0x10,0x11,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x10,0x11,0x11,0x13,0x10,0x11,0x16,0x10,0x11,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x16,0x10,0x13,0x16,0x16,0x10,0x11,0x16,0x10,0x13,0x16,0x16,0x10,0x11,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x10,0x13,0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x16,0x10,0x10,0x10,0x10,0x11,0x11,0x13,0x16,0x10,0x10,0x10,0x11,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x13,0x10,0x10,0x10,0x10,0x11,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x13,0x13,0x13,0x11,0x11,0x11,0x13,0x13,0x13,0x11,0x11,0x13,0x13,0x11,0x11,0x13,0x11,0x11,0x11,0x11,0x11,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x11,
            0x16,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,
            0x13,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x11
        };

        static readonly byte[] ViewInject_KingsQuest6_Dual2 = {
            0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,
            0x10,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x10,
            0x10,0x13,0x16,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x13,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x16,0x13,0x13,0x13,0x13,0x11,0x11,0x16,0x13,0x11,0x11,0x16,0x13,0x11,0x11,0x11,0x16,0x13,0x13,0x11,0x11,0x16,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x13,0x16,0x16,0x16,0x16,0x13,0x11,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x13,0x16,0x16,0x16,0x13,0x11,0x13,0x16,0x10,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x13,0x16,0x10,0x10,0x13,0x16,0x10,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x13,0x16,0x10,0x10,0x11,0x16,0x10,0x13,0x16,0x10,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x13,0x16,0x11,0x13,0x13,0x16,0x10,0x13,0x16,0x10,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x13,0x16,0x16,0x16,0x16,0x16,0x10,0x13,0x16,0x10,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x13,0x16,0x10,0x10,0x11,0x16,0x10,0x13,0x16,0x10,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x13,0x16,0x11,0x13,0x13,0x16,0x10,0x13,0x16,0x11,0x13,0x13,0x16,0x10,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x13,0x16,0x11,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x13,0x16,0x16,0x16,0x16,0x10,0x10,0x11,0x13,0x16,0x16,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x13,0x16,0x10,0x11,0x16,0x16,0x16,0x16,0x10,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,0x10,0x10,0x10,0x11,0x11,0x11,0x10,0x10,0x10,0x11,0x11,0x11,0x10,0x10,0x11,0x11,0x10,0x10,0x11,0x10,0x10,0x10,0x10,0x10,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x13,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x11,0x10,0x10,
            0x10,0x11,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,
            0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10,0x10
        };

        internal void DrawScaled(Rect celRect, Rect clipRect, Rect clipRectTranslated, short loopNo, short celNo, byte priority, ushort scaleX, ushort scaleY)
        {
            throw new NotImplementedException();
        }

        internal void Draw(Rect rect, Rect clipRect, Rect clipRectTranslated, short loopNo, short celNo, byte priority, ushort EGAmappingNr, bool upscaledHires)
        {
            Palette palette = _embeddedPal ? _viewPalette : _palette._sysPalette;
            CelInfo celInfo = GetCelInfo(loopNo, celNo);
            var bitmap = new ByteAccess(GetBitmap(loopNo, celNo));
            short celHeight = celInfo.height;
            short celWidth = celInfo.width;
            byte clearKey = celInfo.clearKey;
            GfxScreenMasks drawMask = priority > 15 ? GfxScreenMasks.VISUAL : GfxScreenMasks.VISUAL | GfxScreenMasks.PRIORITY;
            int x, y;

            if (_embeddedPal)
                // Merge view palette in...
                _palette.Set(_viewPalette, false);

            short width = (short)Math.Min(clipRect.Width, celWidth);
            short height = (short)Math.Min(clipRect.Height, celHeight);

            bitmap.Offset += (clipRect.Top - rect.Top) * celWidth + (clipRect.Left - rect.Left);

            // WORKAROUND: EcoQuest French and German draw the fish and anemone sprites
            // with priority 15 in scene 440. Afterwards, a dialog is shown on top of
            // these sprites with priority 15 as well. This is undefined behavior
            // actually, as the sprites and dialog share the same priority, so in our
            // implementation the sprites get drawn incorrectly on top of the dialog.
            // Perhaps this worked by mistake in SSCI because of subtle differences in
            // how sprites are drawn. We compensate for this by resetting the priority
            // of all sprites that have a priority of 15 in scene 440 to priority 14,
            // so that the speech bubble can be drawn correctly on top of them. Fixes
            // bug #3040625.
            if (SciEngine.Instance.GameId == SciGameId.ECOQUEST && SciEngine.Instance.EngineState.CurrentRoomNumber == 440 && priority == 15)
                priority = 14;

            if (_EGAmapping == null)
            {
                for (y = 0; y < height; y++, bitmap.Offset += celWidth)
                {
                    for (x = 0; x < width; x++)
                    {
                        byte color = bitmap[x];
                        if (color != clearKey)
                        {
                            int x2 = clipRectTranslated.Left + x;
                            int y2 = clipRectTranslated.Top + y;
                            if (!upscaledHires)
                            {
                                if (priority >= _screen.GetPriority((short)x2, (short)y2))
                                {
                                    if (!_palette.IsRemapped(palette.mapping[color]))
                                    {
                                        _screen.PutPixel((short)x2, (short)y2, drawMask, palette.mapping[color], priority, 0);
                                    }
                                    else {
                                        byte remappedColor = _palette.RemapColor(palette.mapping[color], _screen.GetVisual((short)x2, (short)y2));
                                        _screen.PutPixel((short)x2, (short)y2, drawMask, remappedColor, priority, 0);
                                    }
                                }
                            }
                            else {
                                // UpscaledHires means view is hires and is supposed to
                                // get drawn onto lowres screen.
                                // FIXME(?): we can't read priority directly with the
                                // hires coordinates. May not be needed at all in kq6
                                // FIXME: Handle proper aspect ratio. Some GK1 hires images
                                // are in 640x400 instead of 640x480
                                _screen.PutPixelOnDisplay(x2, y2, palette.mapping[color]);
                            }
                        }
                    }
                }
            }
            else {
                ByteAccess EGAmapping = new ByteAccess(_EGAmapping, (EGAmappingNr * SCI_VIEW_EGAMAPPING_SIZE));
                for (y = 0; y < height; y++, bitmap.Offset += celWidth)
                {
                    for (x = 0; x < width; x++)
                    {
                        byte color = EGAmapping[bitmap[x]];
                        int x2 = clipRectTranslated.Left + x;
                        int y2 = clipRectTranslated.Top + y;
                        if (color != clearKey && priority >= _screen.GetPriority((short)x2, (short)y2))
                            _screen.PutPixel((short)x2, (short)y2, drawMask, color, priority, 0);
                    }
                }
            }
        }

        public Rect GetCelRect(short loopNo, short celNo, short x, short y, short z)
        {
            CelInfo celInfo = GetCelInfo(loopNo, celNo);
            var outRect = new Rect();
            outRect.Left = x + celInfo.displaceX - (celInfo.width >> 1);
            outRect.Right = outRect.Left + celInfo.width;
            outRect.Bottom = y + celInfo.displaceY - z + 1 + _adjustForSci0Early;
            outRect.Top = outRect.Bottom - celInfo.height;
            return outRect;
        }

        internal void GetCelSpecialHoyle4Rect(short loopNo, short celNo, short x, short y, short z, Rect celRect)
        {
            throw new NotImplementedException();
        }

        internal void GetCelScaledRect(short loopNo, short celNo, short x, short y, short z, short scaleX, short scaleY, Rect celRect)
        {
            throw new NotImplementedException();
        }

        public ushort GetCelCount(int loopNo)
        {
            loopNo = ScummHelper.Clip(loopNo, 0, _loopCount - 1);
            return _loop[loopNo].celCount;
        }

        public bool IsScaleable { get { return _isScaleable; } }

        public bool IsSci2Hires { get { return _sci2ScaleRes > Sci32ViewNativeResolution.R320x200; } }

        public ushort LoopCount { get { return _loopCount; } }

        public GfxView()
        {
        }

        public GfxView(ResourceManager resMan, GfxScreen screen, GfxPalette palette, int resourceId)
        {
            _resMan = resMan;
            _screen = screen;
            _palette = palette;
            _resourceId = resourceId;
            _coordAdjuster = SciEngine.Instance._gfxCoordAdjuster;

            InitData(resourceId);
        }

        private void InitData(int resourceId)
        {
            _resource = _resMan.FindResource(new ResourceId(ResourceType.View, (ushort)resourceId), true);
            if (_resource == null)
            {
                throw new InvalidOperationException("view resource {resourceId} not found");
            }
            _resourceData = _resource.data;
            _resourceSize = _resource.size;

            ByteAccess celData;
            ByteAccess loopData;
            ushort celOffset;
            CelInfo cel;
            ushort celCount = 0;
            ushort mirrorBits = 0;
            ushort palOffset = 0;
            ushort headerSize = 0;
            ushort loopSize = 0, celSize = 0;
            int loopNo, celNo, EGAmapNr;
            byte seekEntry;
            bool isEGA = false;
            bool isCompressed = true;
            ViewType curViewType = _resMan.ViewType;

            _loopCount = 0;
            _embeddedPal = false;
            _EGAmapping = null;
            _sci2ScaleRes = Sci32ViewNativeResolution.NONE;
            _isScaleable = true;

            // we adjust inside getCelRect for SCI0EARLY (that version didn't have the +1 when calculating bottom)
            _adjustForSci0Early = ResourceManager.GetSciVersion() == SciVersion.V0_EARLY ? (short)-1 : (short)0;

            // If we find an SCI1/SCI1.1 view (not amiga), we switch to that type for
            // EGA. This could get used to make view patches for EGA games, where the
            // new views include more colors. Users could manually adjust old views to
            // make them look better (like removing dithered colors that aren't caught
            // by our undithering or even improve the graphics overall).
            if (curViewType == ViewType.Ega)
            {
                if (_resourceData[1] == 0x80)
                {
                    curViewType = ViewType.Vga;
                }
                else {
                    if (_resourceData.ToUInt16(4) == 1)
                        curViewType = ViewType.Vga11;
                }
            }

            switch (curViewType)
            {
                case ViewType.Ega: // SCI0 (and Amiga 16 colors)
                case ViewType.Amiga: // Amiga ECS (32 colors)
                case ViewType.Amiga64: // Amiga AGA (64 colors)
                case ViewType.Vga: // View-format SCI1
                                   // LoopCount:WORD MirrorMask:WORD Version:WORD PaletteOffset:WORD LoopOffset0:WORD LoopOffset1:WORD...
                    isEGA = curViewType == ViewType.Ega;
                    _loopCount = _resourceData[0];
                    // bit 0x8000 of _resourceData[1] means palette is set
                    if ((_resourceData[1] & 0x40) != 0)
                        isCompressed = false;
                    mirrorBits = _resourceData.ToUInt16(2);
                    palOffset = _resourceData.ToUInt16(6);

                    if (palOffset != 0 && palOffset != 0x100)
                    {
                        // Some SCI0/SCI01 games also have an offset set. It seems that it
                        // points to a 16-byte mapping table but on those games using that
                        // mapping will actually screw things up. On the other side: VGA
                        // SCI1 games have this pointing to a VGA palette and EGA SCI1 games
                        // have this pointing to a 8x16 byte mapping table that needs to get
                        // applied then.
                        if (!isEGA)
                        {
                            _viewPalette = _palette.CreateFromData(new Core.Common.ByteAccess(_resourceData, palOffset), _resourceSize - palOffset);
                            _embeddedPal = true;
                        }
                        else {
                            // Only use the EGA-mapping, when being SCI1 EGA
                            //  SCI1 VGA conversion games (which will get detected as SCI1EARLY/MIDDLE/LATE) have some views
                            //  with broken mapping tables. I guess those games won't use the mapping, so I rather disable it
                            //  for them
                            if (ResourceManager.GetSciVersion() == SciVersion.V1_EGA_ONLY)
                            {
                                _EGAmapping = new ByteAccess(_resourceData, palOffset);
                                for (EGAmapNr = 0; EGAmapNr < SCI_VIEW_EGAMAPPING_COUNT; EGAmapNr++)
                                {
                                    if (!ScummHelper.ArrayEquals(_EGAmapping.Data, _EGAmapping.Offset, EGAmappingStraight, 0, SCI_VIEW_EGAMAPPING_SIZE))
                                        break;

                                    _EGAmapping.Offset += SCI_VIEW_EGAMAPPING_SIZE;
                                }
                                // If all mappings are "straight", then we actually ignore the mapping
                                if (EGAmapNr == SCI_VIEW_EGAMAPPING_COUNT)
                                    _EGAmapping = null;
                                else
                                    _EGAmapping = new ByteAccess(_resourceData, palOffset);
                            }
                        }
                    }

                    _loop = new LoopInfo[_loopCount];
                    for (loopNo = 0; loopNo < _loopCount; loopNo++)
                    {
                        loopData = new ByteAccess(_resourceData, _resourceData.ToUInt16(8 + loopNo * 2));
                        // CelCount:WORD Unknown:WORD CelOffset0:WORD CelOffset1:WORD...

                        celCount = loopData.ReadUInt16();
                        _loop[loopNo] = new LoopInfo();
                        _loop[loopNo].celCount = celCount;
                        _loop[loopNo].mirrorFlag = (mirrorBits & 1) != 0;
                        mirrorBits >>= 1;

                        // read cel info
                        _loop[loopNo].cel = new CelInfo[celCount];
                        for (celNo = 0; celNo < celCount; celNo++)
                        {
                            celOffset = loopData.ReadUInt16(4 + celNo * 2);
                            celData = new ByteAccess(_resourceData, celOffset);

                            // For VGA
                            // Width:WORD Height:WORD DisplaceX:BYTE DisplaceY:BYTE ClearKey:BYTE Unknown:BYTE RLEData starts now directly
                            // For EGA
                            // Width:WORD Height:WORD DisplaceX:BYTE DisplaceY:BYTE ClearKey:BYTE EGAData starts now directly
                            cel = _loop[loopNo].cel[celNo] = new CelInfo();
                            cel.scriptWidth = cel.width = celData.ReadInt16();
                            cel.scriptHeight = cel.height = celData.ReadInt16(2);
                            cel.displaceX = (sbyte)celData[4];
                            cel.displaceY = celData[5];
                            cel.clearKey = celData[6];

                            // HACK: Fix Ego's odd displacement in the QFG3 demo, scene 740.
                            // For some reason, ego jumps above the rope, so we fix his rope
                            // hanging view by displacing it down by 40 pixels. Fixes bug
                            // #3035693.
                            // FIXME: Remove this once we figure out why Ego jumps so high.
                            // Likely culprits include kInitBresen, kDoBresen and kCantBeHere.
                            // The scripts have the y offset that hero reaches (11) hardcoded,
                            // so it might be collision detection. However, since this requires
                            // extensive work to fix properly for very little gain, this hack
                            // here will suffice until the actual issue is found.
                            if (SciEngine.Instance.GameId == SciGameId.QFG3 && SciEngine.Instance.IsDemo && resourceId == 39)
                                cel.displaceY = 98;

                            if (isEGA)
                            {
                                cel.offsetEGA = (ushort)(celOffset + 7);
                                cel.offsetRLE = 0;
                                cel.offsetLiteral = 0;
                            }
                            else {
                                cel.offsetEGA = 0;
                                if (isCompressed)
                                {
                                    cel.offsetRLE = (uint)(celOffset + 8);
                                    cel.offsetLiteral = 0;
                                }
                                else {
                                    cel.offsetRLE = 0;
                                    cel.offsetLiteral = (uint)(celOffset + 8);
                                }
                            }
                            cel.rawBitmap = null;
                            if (_loop[loopNo].mirrorFlag)
                                cel.displaceX = (short)-cel.displaceX;
                        }
                    }
                    break;

                case ViewType.Vga11: // View-format SCI1.1+
                                     // HeaderSize:WORD LoopCount:BYTE Flags:BYTE Version:WORD Unknown:WORD PaletteOffset:WORD
                    headerSize = (ushort)(_resourceData.ReadSci11EndianUInt16(0) + 2); // headerSize is not part of the header, so it's added
                    //assert(headerSize >= 16);
                    _loopCount = _resourceData[2];

                    //assert(_loopCount);
                    palOffset = (ushort)_resourceData.ReadSci11EndianUInt32(8);

                    // For SCI32, this is a scale flag
                    if (ResourceManager.GetSciVersion() >= SciVersion.V2)
                    {
                        _sci2ScaleRes = (Sci32ViewNativeResolution)_resourceData[5];
                        if (_screen.UpscaledHires == GfxScreenUpscaledMode.DISABLED)
                            _sci2ScaleRes = Sci32ViewNativeResolution.NONE;
                    }

                    // flags is actually a bit-mask
                    //  it seems it was only used for some early sci1.1 games (or even just laura bow 2)
                    //  later interpreters dont support it at all anymore
                    // we assume that if flags is 0h the view does not support flags and default to scaleable
                    // if it's 1h then we assume that the view is not to be scaled
                    // if it's 40h then we assume that the view is scaleable
                    switch (_resourceData[3])
                    {
                        case 1:
                            _isScaleable = false;
                            break;
                        case 0x40:
                        case 0x4F:  // LSL6 Polish, seems to be garbage - bug #6718
                        case 0:
                            break; // don't do anything, we already have _isScaleable set
                        default:

                            throw new InvalidOperationException($"unsupported flags byte ({_resourceData[3]}) inside sci1.1 view");
                    }

                    loopData = new ByteAccess(_resourceData, headerSize);
                    loopSize = _resourceData[12];

                    //assert(loopSize >= 16);
                    celSize = _resourceData[13];

                    //assert(celSize >= 32);

                    if (palOffset != 0)
                    {
                        _viewPalette = _palette.CreateFromData(new ByteAccess(_resourceData, palOffset), _resourceSize - palOffset);
                        _embeddedPal = true;
                    }

                    _loop = new LoopInfo[_loopCount];
                    for (loopNo = 0; loopNo < _loopCount; loopNo++)
                    {
                        loopData = new ByteAccess(_resourceData, headerSize + (loopNo * loopSize));

                        seekEntry = loopData[0];
                        if (seekEntry != 255)
                        {
                            if (seekEntry >= _loopCount)
                                throw new InvalidOperationException("Bad loop-pointer in sci 1.1 view");
                            _loop[loopNo].mirrorFlag = true;
                            loopData = new ByteAccess(_resourceData, headerSize + (seekEntry * loopSize));
                        }
                        else {
                            _loop[loopNo].mirrorFlag = false;
                        }

                        celCount = loopData[2];
                        _loop[loopNo].celCount = celCount;

                        celData = new ByteAccess(_resourceData, (int)loopData.Data.ReadSci11EndianUInt32(loopData.Offset + 12));

                        // read cel info
                        _loop[loopNo].cel = new CelInfo[celCount];
                        for (celNo = 0; celNo < celCount; celNo++)
                        {
                            cel = _loop[loopNo].cel[celNo];
                            cel.scriptWidth = cel.width = (short)celData.Data.ReadSci11EndianUInt16(celData.Offset);
                            cel.scriptHeight = cel.height = (short)celData.Data.ReadSci11EndianUInt16(celData.Offset + 2);
                            cel.displaceX = (short)celData.Data.ReadSci11EndianUInt16(celData.Offset + 4);
                            cel.displaceY = (short)celData.Data.ReadSci11EndianUInt16(celData.Offset + 6);
                            if (cel.displaceY < 0)
                                cel.displaceY += 255; // sierra did this adjust in their sci1.1 getCelRect() - not sure about sci32

                            //assert(cel.width && cel.height);

                            cel.clearKey = celData[8];
                            cel.offsetEGA = 0;
                            cel.offsetRLE = celData.Data.ReadSci11EndianUInt32(celData.Offset + 24);
                            cel.offsetLiteral = celData.Data.ReadSci11EndianUInt32(celData.Offset + 28);

                            // GK1-hires content is actually uncompressed, we need to swap both so that we process it as such
                            if ((cel.offsetRLE != 0) && (cel.offsetLiteral == 0))
                                ScummHelper.Swap(ref cel.offsetRLE, ref cel.offsetLiteral);

                            cel.rawBitmap = null;
                            if (_loop[loopNo].mirrorFlag)
                                cel.displaceX = (short)-cel.displaceX;

                            celData.Offset += celSize;
                        }
                    }
#if ENABLE_SCI32
		// adjust width/height returned to scripts
		if (_sci2ScaleRes != SCI_VIEW_NATIVERES_NONE) {
			for (loopNo = 0; loopNo<_loopCount; loopNo++)
				for (celNo = 0; celNo<_loop[loopNo].celCount; celNo++)
					_screen.adjustBackUpscaledCoordinates(_loop[loopNo].cel[celNo].scriptWidth, _loop[loopNo].cel[celNo].scriptHeight, _sci2ScaleRes);
		} else if (ResourceManager.GetSciVersion() == SCI_VERSION_2_1) {
			for (loopNo = 0; loopNo<_loopCount; loopNo++)
				for (celNo = 0; celNo<_loop[loopNo].celCount; celNo++)
					_coordAdjuster.fromDisplayToScript(_loop[loopNo].cel[celNo].scriptHeight, _loop[loopNo].cel[celNo].scriptWidth);
		}
#endif
                    break;

                default:
                    throw new InvalidOperationException("ViewType was not detected, can't continue");
            }

            // Inject our own views
            //  Currently only used for Dual mode (speech + text) for games, that do not have a "dual" icon already
            //  Which is Laura Bow 2 + King's Quest 6
            switch (SciEngine.Instance.GameId)
            {
                case SciGameId.LAURABOW2:
                    // View 995, Loop 13, Cel 0 = "TEXT"
                    // View 995, Loop 13, Cel 1 = "SPEECH"
                    // View 995, Loop 13, Cel 2 = "DUAL" (<- our injected view)
                    if ((SciEngine.Instance.IsCD) && (resourceId == 995))
                    {
                        // security checks
                        if (_loopCount >= 14)
                        {
                            if ((_loop[13].celCount == 2) && (_loop[13].cel[0].width == 46) && (_loop[13].cel[0].height == 11))
                            {
                                // copy current cels over
                                CelInfo[] newCels = new CelInfo[3];

                                Array.Copy(_loop[13].cel, newCels, 2);
                                _loop[13].cel = null;

                                _loop[13].celCount++;
                                _loop[13].cel = newCels;
                                // Duplicate cel 0 to cel 2
                                _loop[13].cel[2] = _loop[13].cel[0];
                                // copy over our data (which is uncompressed bitmap data)
                                _loop[13].cel[2].rawBitmap = new byte[ViewInject_LauraBow2_Dual.Length];

                                Array.Copy(ViewInject_LauraBow2_Dual, _loop[13].cel[2].rawBitmap, ViewInject_LauraBow2_Dual.Length);
                            }
                        }
                    }
                    break;
                case SciGameId.KQ6:
                    // View 947, Loop 8, Cel 0 = "SPEECH" (not pressed)
                    // View 947, Loop 8, Cel 1 = "SPEECH" (pressed)
                    // View 947, Loop 9, Cel 0 = "TEXT" (not pressed)
                    // View 947, Loop 9, Cel 1 = "TEXT" (pressed)
                    // View 947, Loop 12, Cel 0 = "DUAL" (not pressed) (<- our injected view)
                    // View 947, Loop 12, Cel 1 = "DUAL" (pressed) (<- our injected view)
                    if ((SciEngine.Instance.IsCD) && (resourceId == 947))
                    {
                        // security checks
                        if (_loopCount == 12)
                        {
                            if ((_loop[8].celCount == 2) && (_loop[8].cel[0].width == 50) && (_loop[8].cel[0].height == 15))
                            {
                                // add another loop
                                LoopInfo[] newLoops = new LoopInfo[_loopCount + 1];

                                Array.Copy(_loop, newLoops, _loopCount);
                                _loop = newLoops;
                                _loopCount++;
                                // copy loop 8 to loop 12
                                _loop[12] = _loop[8];
                                _loop[12].cel = new CelInfo[2];
                                // duplicate all cels of loop 8 and into loop 12
                                Array.Copy(_loop[8].cel, _loop[12].cel, _loop[8].celCount);
                                // copy over our data (which is uncompressed bitmap data)
                                _loop[12].cel[0].rawBitmap = new byte[ViewInject_KingsQuest6_Dual1.Length];
                                Array.Copy(ViewInject_KingsQuest6_Dual1, _loop[12].cel[0].rawBitmap, ViewInject_KingsQuest6_Dual1.Length);

                                _loop[12].cel[1].rawBitmap = new byte[ViewInject_KingsQuest6_Dual2.Length];
                                Array.Copy(ViewInject_KingsQuest6_Dual1, _loop[12].cel[1].rawBitmap, ViewInject_KingsQuest6_Dual2.Length);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public short GetWidth(short loopNo, short celNo)
        {
            return (short)(_loopCount != 0 ? GetCelInfo(loopNo, celNo).width : 0);
        }

        public short GetHeight(short loopNo, short celNo)
        {
            return (short)(_loopCount != 0 ? GetCelInfo(loopNo, celNo).height : 0);
        }

        public byte[] GetBitmap(short loopNo, short celNo)
        {
            loopNo = (short)ScummHelper.Clip(loopNo, 0, _loopCount - 1);
            celNo = (short)ScummHelper.Clip(celNo, 0, _loop[loopNo].celCount - 1);
            if (_loop[loopNo].cel[celNo].rawBitmap != null)
                return _loop[loopNo].cel[celNo].rawBitmap;

            ushort width = (ushort)_loop[loopNo].cel[celNo].width;
            ushort height = (ushort)_loop[loopNo].cel[celNo].height;
            // allocating memory to store cel's bitmap
            int pixelCount = width * height;
            _loop[loopNo].cel[celNo].rawBitmap = new byte[pixelCount];
            var pBitmap = _loop[loopNo].cel[celNo].rawBitmap;

            // unpack the actual cel bitmap data
            UnpackCel(loopNo, celNo, pBitmap, pixelCount);

            if (_resMan.ViewType == ViewType.Ega)
                UnditherBitmap(pBitmap, (short)width, (short)height, _loop[loopNo].cel[celNo].clearKey);

            // mirroring the cel if needed
            if (_loop[loopNo].mirrorFlag)
            {
                int off = 0;
                for (int i = 0; i < height; i++, off += width)
                    for (int j = 0; j < width / 2; j++)
                        ScummHelper.Swap(ref pBitmap[off + j], ref pBitmap[off + width - j - 1]);
            }
            return _loop[loopNo].cel[celNo].rawBitmap;
        }

        /// <summary>
        /// Called after unpacking an EGA cel, this will try to undither (parts) of the
        /// cel if the dithering in here matches dithering used by the current picture.
        /// </summary>
        private void UnditherBitmap(byte[] bitmapPtr, short width, short height, byte clearKey)
        {
            var ditheredPicColors = _screen.UnditherGetDitheredBgColors;

            // It makes no sense to go further, if there isn't any dithered color data
            // available for the current picture
            if (ditheredPicColors == null)
                return;

            // We need at least a 4x2 bitmap for this algorithm to work
            if (width < 4 || height < 2)
                return;

            // If EGA mapping is used for this view, dont do undithering as well
            if (_EGAmapping != null)
                return;

            // Walk through the bitmap and remember all combinations of colors
            short[] ditheredBitmapColors = new short[GfxScreen.DITHERED_BG_COLORS_SIZE];
            byte color1, color2;
            byte nextColor1, nextColor2;
            short y, x;

            // Count all seemingly dithered pixel-combinations as soon as at least 4
            // pixels are adjacent and check pixels in the following line as well to
            // be the reverse pixel combination
            short checkHeight = (short)(height - 1);
            var curPtr = new ByteAccess(bitmapPtr);
            var nextPtr = new ByteAccess(curPtr, width);
            for (y = 0; y < checkHeight; y++)
            {
                color1 = curPtr[0]; color2 = (byte)((curPtr[1] << 4) | curPtr[2]);
                nextColor1 = (byte)(nextPtr[0] << 4); nextColor2 = (byte)((nextPtr[2] << 4) | nextPtr[1]);
                curPtr.Offset += 3;
                nextPtr.Offset += 3;
                for (x = 3; x < width; x++)
                {
                    color1 = (byte)((color1 << 4) | (color2 >> 4));
                    color2 = (byte)((color2 << 4) | curPtr.Increment());
                    nextColor1 = (byte)((nextColor1 >> 4) | (nextColor2 << 4));
                    nextColor2 = (byte)((nextColor2 >> 4) | nextPtr.Increment() << 4);
                    if ((color1 == color2) && (color1 == nextColor1) && (color1 == nextColor2))
                        ditheredBitmapColors[color1]++;
                }
            }

            // Now compare both dither color tables to find out matching dithered color
            // combinations
            bool[] unditherTable = new bool[GfxScreen.DITHERED_BG_COLORS_SIZE];
            byte color, unditherCount = 0;
            for (color = 0; color < 255; color++)
            {
                if ((ditheredBitmapColors[color] > 5) && (ditheredPicColors[color] > 200))
                {
                    // match found, check if colorKey is contained . if so, we ignore
                    // of course
                    color1 = (byte)(color & 0x0F); color2 = (byte)(color >> 4);
                    if ((color1 != clearKey) && (color2 != clearKey) && (color1 != color2))
                    {
                        // so set this and the reversed color-combination for undithering
                        unditherTable[color] = true;
                        unditherTable[(color1 << 4) | color2] = true;
                        unditherCount++;
                    }
                }
            }

            // Nothing found to undither . exit straight away
            if (unditherCount == 0)
                return;

            // We now need to replace color-combinations
            curPtr = new ByteAccess(bitmapPtr);
            for (y = 0; y < height; y++)
            {
                color = curPtr.Value;
                for (x = 1; x < width; x++)
                {
                    color = (byte)((color << 4) | curPtr[1]);
                    if (unditherTable[color])
                    {
                        // Some color with black? Turn colors around, otherwise it won't
                        // be the right color at all.
                        byte unditheredColor = color;
                        if ((color & 0xF0) == 0)
                            unditheredColor = (byte)((color << 4) | (color >> 4));
                        curPtr[0] = unditheredColor; curPtr[1] = unditheredColor;
                    }
                    curPtr.Offset++;
                }
                curPtr.Offset++;
            }
        }

        private void UnpackCel(short loopNo, short celNo, byte[] outPtr, int pixelCount)
        {
            CelInfo celInfo = GetCelInfo(loopNo, celNo);

            if (celInfo.offsetEGA != 0)
            {
                // decompression for EGA views
                UnpackCelData(_resourceData, outPtr, 0, pixelCount, celInfo.offsetEGA, 0, _resMan.ViewType, (ushort)celInfo.width, false);
            }
            else {
                // We fill the buffer with transparent pixels, so that we can later skip
                //  over pixels to automatically have them transparent
                // Also some RLE compressed cels are possibly ending with the last
                // non-transparent pixel (is this even possible with the current code?)
                byte clearColor = _loop[loopNo].cel[celNo].clearKey;

                // Since Mac OS required palette index 0 to be white and 0xff to be black, the
                // Mac SCI devs decided that rather than change scripts and various pieces of
                // code, that they would just put a little snippet of code to swap these colors
                // in various places around the SCI codebase. We figured that it would be less
                // hacky to swap pixels instead and run the Mac games with a PC palette.
                if (SciEngine.Instance.Platform == Core.IO.Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                {
                    // clearColor is based on PC palette, but the literal data is not.
                    // We flip clearColor here to make it match the literal data. All
                    // these pixels will be flipped back again below.
                    if (clearColor == 0)
                        clearColor = 0xff;
                    else if (clearColor == 0xff)
                        clearColor = 0;
                }

                bool isMacSci11ViewData = SciEngine.Instance.Platform == Core.IO.Platform.Macintosh && ResourceManager.GetSciVersion() == SciVersion.V1_1;
                UnpackCelData(_resourceData, outPtr, clearColor, pixelCount, (int)celInfo.offsetRLE, (int)celInfo.offsetLiteral, _resMan.ViewType, (ushort)celInfo.width, isMacSci11ViewData);

                // Swap 0 and 0xff pixels for Mac SCI1.1+ games (see above)
                if (SciEngine.Instance.Platform == Core.IO.Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                {
                    for (var i = 0; i < pixelCount; i++)
                    {
                        if (outPtr[i] == 0)
                            outPtr[i] = 0xff;
                        else if (outPtr[i] == 0xff)
                            outPtr[i] = 0;
                    }
                }
            }
        }

        private void UnpackCelData(byte[] inBuffer, byte[] celBitmap, byte clearColor, int pixelCount, int rlePos, int literalPos, ViewType viewType, ushort width, bool isMacSci11ViewData)
        {
            var outPtr = new ByteAccess(celBitmap);
            byte curByte, runLength;
            var rlePtr = new ByteAccess(inBuffer, rlePos);
            // The existence of a literal position pointer signifies data with two
            // separate streams, most likely a SCI1.1 view
            var literalPtr = new ByteAccess(inBuffer, literalPos);
            int pixelNr = 0;

            celBitmap.Set(0, clearColor, pixelCount);

            // View unpacking:
            //
            // EGA:
            // Each byte is like XXXXYYYY (XXXX: 0 - 15, YYYY: 0 - 15)
            // Set the next XXXX pixels to YYYY
            //
            // Amiga:
            // Each byte is like XXXXXYYY (XXXXX: 0 - 31, YYY: 0 - 7)
            // - Case A: YYY != 0
            //   Set the next YYY pixels to XXXXX
            // - Case B: YYY == 0
            //   Skip the next XXXXX pixels (i.e. transparency)
            //
            // Amiga 64:
            // Each byte is like XXYYYYYY (XX: 0 - 3, YYYYYY: 0 - 63)
            // - Case A: XX != 0
            //   Set the next XX pixels to YYYYYY
            // - Case B: XX == 0
            //   Skip the next YYYYYY pixels (i.e. transparency)
            //
            // VGA:
            // Each byte is like XXYYYYYY (YYYYY: 0 - 63)
            // - Case A: XX == 00 (binary)
            //   Copy next YYYYYY bytes as-is
            // - Case B: XX == 01 (binary)
            //   Same as above, copy YYYYYY + 64 bytes as-is
            // - Case C: XX == 10 (binary)
            //   Set the next YYYYY pixels to the next byte value
            // - Case D: XX == 11 (binary)
            //   Skip the next YYYYY pixels (i.e. transparency)

            if (literalPos != 0 && isMacSci11ViewData)
            {
                // KQ6/Freddy Pharkas/Slater use byte lengths, all others use uint16
                // The SCI devs must have realized that a max of 255 pixels wide
                // was not very good for 320 or 640 width games.
                bool hasByteLengths = (SciEngine.Instance.GameId == SciGameId.KQ6 || SciEngine.Instance.GameId == SciGameId.FREDDYPHARKAS
                        || SciEngine.Instance.GameId == SciGameId.SLATER);

                // compression for SCI1.1+ Mac
                while (pixelNr < pixelCount)
                {
                    var pixelLine = pixelNr;

                    if (hasByteLengths)
                    {
                        pixelNr += rlePtr.Increment();
                        runLength = rlePtr.Increment();
                    }
                    else {
                        pixelNr += rlePtr.ReadUInt16BigEndian();
                        runLength = (byte)rlePtr.ReadUInt16BigEndian(2);
                        rlePtr.Offset += 4;
                    }

                    while (runLength-- != 0 && pixelNr < pixelCount)
                        outPtr[pixelNr++] = literalPtr.Increment();

                    pixelNr = pixelLine + width;
                }
                return;
            }

            switch (viewType)
            {
                case ViewType.Ega:
                    while (pixelNr < pixelCount)
                    {
                        curByte = rlePtr.Increment();
                        runLength = (byte)(curByte >> 4);
                        outPtr.Data.Set(outPtr.Offset + pixelNr, (byte)(curByte & 0x0F), Math.Min(runLength, pixelCount - pixelNr));
                        pixelNr += runLength;
                    }
                    break;
                case ViewType.Amiga:
                    while (pixelNr < pixelCount)
                    {
                        curByte = rlePtr.Increment();
                        if ((curByte & 0x07) != 0)
                        { // fill with color
                            runLength = (byte)(curByte & 0x07);
                            curByte = (byte)(curByte >> 3);
                            outPtr.Data.Set(outPtr.Offset + pixelNr, curByte, Math.Min(runLength, pixelCount - pixelNr));
                        }
                        else { // skip the next pixels (transparency)
                            runLength = (byte)(curByte >> 3);
                        }
                        pixelNr += runLength;
                    }
                    break;
                case ViewType.Amiga64:
                    while (pixelNr < pixelCount)
                    {
                        curByte = rlePtr.Increment();
                        if ((curByte & 0xC0) != 0)
                        { // fill with color
                            runLength = (byte)(curByte >> 6);
                            curByte = (byte)(curByte & 0x3F);
                            outPtr.Data.Set(outPtr.Offset + pixelNr, curByte, Math.Min(runLength, pixelCount - pixelNr));
                        }
                        else { // skip the next pixels (transparency)
                            runLength = (byte)(curByte & 0x3F);
                        }
                        pixelNr += runLength;
                    }
                    break;
                case ViewType.Vga:
                case ViewType.Vga11:
                    // If we have no RLE data, the image is just uncompressed
                    if (rlePos == 0)
                    {
                        Array.Copy(literalPtr.Data, literalPtr.Offset, outPtr.Data, outPtr.Offset, pixelCount);
                        break;
                    }

                    while (pixelNr < pixelCount)
                    {
                        curByte = rlePtr.Increment();
                        runLength = (byte)(curByte & 0x3F);

                        switch (curByte & 0xC0)
                        {
                            case 0x40: // copy bytes as is (In copy case, runLength can go up to 127 i.e. pixel & 0x40). Fixes bug #3135872.
                            case 0x00: // copy bytes as-is
                                if ((curByte & 0xC0) == 0x40)
                                {
                                    runLength += 64;
                                }
                                if (literalPos == 0)
                                {
                                    Array.Copy(rlePtr.Data, rlePtr.Offset, outPtr.Data, outPtr.Offset + pixelNr, Math.Min(runLength, pixelCount - pixelNr));
                                    rlePtr.Offset += runLength;
                                }
                                else {
                                    Array.Copy(literalPtr.Data, literalPtr.Offset, outPtr.Data, outPtr.Offset + pixelNr, Math.Min(runLength, pixelCount - pixelNr));
                                    literalPtr.Offset += runLength;
                                }
                                break;
                            case 0x80: // fill with color
                                if (literalPos == 0)
                                {
                                    outPtr.Data.Set(outPtr.Offset + pixelNr, rlePtr.Increment(), Math.Min(runLength, pixelCount - pixelNr));
                                }
                                else {
                                    outPtr.Data.Set(outPtr.Offset + pixelNr, literalPtr.Increment(), Math.Min(runLength, pixelCount - pixelNr));
                                }
                                break;
                            case 0xC0: // skip the next pixels (transparency)
                                break;
                        }

                        pixelNr += runLength;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unsupported picture viewtype");
            }
        }

        public CelInfo GetCelInfo(short loopNo, short celNo)
        {
            //assert(_loopCount);
            loopNo = (short)ScummHelper.Clip(loopNo, 0, _loopCount - 1);
            celNo = (short)ScummHelper.Clip(celNo, 0, _loop[loopNo].celCount - 1);
            return _loop[loopNo].cel[celNo];
        }

        internal void AdjustToUpscaledCoordinates(short y, short x)
        {
            throw new NotImplementedException();
        }

        internal void AdjustBackUpscaledCoordinates(int top, int left)
        {
            throw new NotImplementedException();
        }
    }
}
