//
//  AkosRenderer.cs
//
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
using System.Diagnostics;
using NScumm.Core.IO;

namespace NScumm.Core.Graphics
{
    class AkosRenderer: ICostumeRenderer
    {
        public AkosRenderer(ScummEngine vm)
        {
            this.vm = vm;
        }

        #region ICostumeRenderer implementation

        public void SetPalette(ushort[] new_palette)
        {
            int size, i;

            size = akpl.Length;
            if (size == 0)
                return;

            if (size > 256)
                throw new InvalidOperationException(string.Format("akos_setPalette: {0} is too many colors", size));

//            if (vm.Game.Features.HasFlag(GameFeatures.16BITCOLOR) {
//                if (_paletteNum) {
//                    for (i = 0; i < size; i++)
//                        _palette[i] = READ_LE_UINT16(_vm->_hePalettes + _paletteNum * _vm->_hePaletteSlot + 768 + akpl[i] * 2);
//                } else if (rgbs) {
//                    for (i = 0; i < size; i++) {
//                        if (new_palette[i] == 0xFF) {
//                            uint8 col = akpl[i];
//                            _palette[i] = _vm->get16BitColor(rgbs[col * 3 + 0], rgbs[col * 3 + 1], rgbs[col * 3 + 2]);
//                        } else {
//                            _palette[i] = new_palette[i];
//                        }
//                    }
//                }
//            } else if (_vm->_game.heversion >= 99 && _paletteNum) {
//                for (i = 0; i < size; i++)
//                    _palette[i] = (byte)_vm->_hePalettes[_paletteNum * _vm->_hePaletteSlot + 768 + akpl[i]];
//            } else {
                for (i = 0; i < size; i++) {
                    _palette[i] = new_palette[i] != 0xFF ? new_palette[i] : akpl[i];
                }
//            }

//            if (_vm->_game.heversion == 70) {
//                for (i = 0; i < size; i++)
//                    _palette[i] = _vm->_HEV7ActorPalette[_palette[i]];
//            }

            if (size == 256) {
                var color = new_palette[0];
                if (color == 255) {
                    _palette[0] = color;
                } else {
                    _useBompPalette = true;
                }
            }
        }

        public void SetFacing(Actor a)
        {
            mirror = (ScummHelper.NewDirToOldDir(a.Facing) != 0) || ((akhd.flags & 1)!=0);
        }

        public void SetCostume(int costume, int shadow)
        {
            var akos = vm.ResourceManager.GetCostumeData(costume);
            Debug.Assert(akos!=null);

            akhd = ResourceFile7.ReadData<AkosHeader>(akos,"AKHD");
            akof = ResourceFile7.ReadData<AkosOffset>(akos,"AKOF");
            akci = ResourceFile7.ReadData(akos,"AKCI");
            aksq = ResourceFile7.ReadData(akos,"AKSQ");
            akcd = ResourceFile7.ReadData(akos,"AKCD");
            akpl = ResourceFile7.ReadData(akos,"AKPL");
            codec = akhd.codec;
//            akct = ResourceFile7.ReadData(akos,"AKCT");
//            rgbs = ResourceFile7.ReadData(akos,"RGBS");

//            xmap = 0;
//            if (shadow) {
//                const uint8 *xmapPtr = _vm->getResourceAddress(rtImage, shadow);
//                assert(xmapPtr);
//                xmap = ResourceFile7.ReadData(akos,'X','M','A','P'), xmapPtr);
//                assert(xmap);
//            }
        }

        public int DrawCostume(VirtScreen vs, int numStrips, Actor actor, bool drawToBackBuf)
        {
            throw new NotImplementedException();
        }

        public int DrawTop
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int DrawBottom
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public byte ActorID
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public byte ShadowMode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public byte[] ShadowTable
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int ActorX
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int ActorY
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public byte ZBuffer
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public byte ScaleX
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public byte ScaleY
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }


        #endregion

        ScummEngine vm;

        bool mirror;

        bool _useBompPalette;

        ushort codec;
        AkosHeader akhd; // header
        byte[] akpl;     // palette data
        byte[] akci;     // CostumeInfo table
        byte[] aksq;     // command sequence
        AkosOffset akof; // offsets into ci and cd table
        byte[] akcd;     // costume data (contains the data for the codecs)

        // actor _palette
        ushort[] _palette=new ushort[256];
    }
}

