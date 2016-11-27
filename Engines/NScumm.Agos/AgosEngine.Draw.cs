//
//  AgosEngine.Draw.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AGOSEngine
    {
        private BytePtr BackBuf => _backBuf.Pixels;

        protected BytePtr BackGround => _backGroundBuf.Pixels;

        private void AnimateSprites()
        {
            if (_copyScnFlag != 0)
            {
                _copyScnFlag--;
                _vgaSpriteChanged++;
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
                byte var = (byte) (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ? 293 : 71);
                if (_wallOn != 0 && _variableArray[var] == 0)
                {
                    _wallOn--;

                    var state = new VC10_state();
                    state.srcPtr = BackGround + 3 * _backGroundBuf.Pitch + 3 * 16;
                    state.height = state.draw_height = 127;
                    state.width = state.draw_width = 14;
                    state.y = 0;
                    state.x = 0;
                    state.palette = 0;
                    state.paletteMod = 0;
                    state.flags = DrawFlags.kDFNonTrans;

                    _windowNum = 4;

                    _backFlag = true;
                    DrawImage(state);
                    _backFlag = false;

                    _vgaSpriteChanged++;
                }
            }

            if (_scrollFlag == 0 && _vgaSpriteChanged == 0)
            {
                return;
            }

            _vgaSpriteChanged = 0;

            if (_paletteFlag == 2)
                _paletteFlag = 1;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 && _scrollFlag != 0)
            {
                ScrollScreen();
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                DirtyClips();
            }

            RestoreBackGround();

            var vsp = new Ptr<VgaSprite>(_vgaSprites);
            for (; vsp.Value.id != 0; vsp.Offset++)
            {
                if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                    (vsp.Value.windowNum & 0x8000) == 0)
                {
                    continue;
                }

                vsp.Value.windowNum = (ushort) (vsp.Value.windowNum & ~0x8000);

                var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, vsp.Value.zoneNum);
                _curVgaFile1 = vpe.Value.vgaFile1;
                _curVgaFile2 = vpe.Value.vgaFile2;
                _curSfxFile = vpe.Value.sfxFile;
                _windowNum = vsp.Value.windowNum;
                _vgaCurSpriteId = vsp.Value.id;

                SaveBackGround(vsp);

                DrawImageInit(vsp.Value.image, vsp.Value.palette, vsp.Value.x, vsp.Value.y, vsp.Value.flags);
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && _variableArray[293] != 0)
            {
                // Used by the Fire Wall and Ice Wall spells
                Debug(0, "Using special wall");

                byte color, h, len;
                var dst = _window4BackScn.Pixels;

                color = (byte) ((_variableArray[293] & 1) != 0 ? 13 : 15);
                _wallOn = 2;

                h = 127;
                while (h != 0)
                {
                    len = 112;
                    while (len-- != 0)
                    {
                        dst.Value = color;
                        dst.Offset += 2;
                    }

                    h--;
                    if (h == 0)
                        break;

                    len = 112;
                    while (len-- != 0)
                    {
                        dst.Offset++;
                        dst.Value = color;
                        dst.Offset++;
                    }
                    h--;
                }

                _window4Flag = 1;
                SetMoveRect(0, 0, 224, 127);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 &&
                     (_variableArray[71] & 2) != 0)
            {
                // Used by the Unholy Barrier spell
                byte color, h;
                var dst = _window4BackScn.Pixels;

                color = 1;
                _wallOn = 2;

                h = (byte) 43;
                while (h != 0)
                {
                    var len = (byte) 56;
                    while (len-- != 0)
                    {
                        dst.Value = color;
                        dst += 4;
                    }

                    h--;
                    if (h == 0)
                        break;

                    dst += 448;

                    len = 56;
                    while (len-- != 0)
                    {
                        dst += 2;
                        dst.Value = color;
                        dst += 2;
                    }
                    dst += 448;
                    h--;
                }

                _window4Flag = 1;
                SetMoveRect(0, 0, 224, 127);
            }

            if (_window6Flag == 1)
                _window6Flag++;

            if (_window4Flag == 1)
                _window4Flag++;

            _displayFlag++;
        }

        private void DirtyClips()
        {
            short x, y, w, h;
            restart:
            _newDirtyClip = false;

            Ptr<VgaSprite> vsp = _vgaSprites;
            while (vsp.Value.id != 0)
            {
                if ((vsp.Value.windowNum & 0x8000) != 0)
                {
                    x = vsp.Value.x;
                    y = vsp.Value.y;
                    w = 1;
                    h = 1;

                    if (vsp.Value.image != 0)
                    {
                        var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, vsp.Value.zoneNum);
                        BytePtr ptr = vpe.Value.vgaFile2 + vsp.Value.image * 8;
                        w = (short) (ptr.ToUInt16BigEndian(6) / 8);
                        h = ptr[5];
                    }

                    DirtyClipCheck(x, y, w, h);
                }
                vsp.Offset++;
            }

            Ptr<AnimTable> animTable = _screenAnim1;
            while (animTable.Value.srcPtr != BytePtr.Null)
            {
                if ((animTable.Value.windowNum & 0x8000) != 0)
                {
                    x = (short) (animTable.Value.x + _scrollX);
                    y = animTable.Value.y;
                    w = (short) (animTable.Value.width * 2);
                    h = (short) animTable.Value.height;

                    DirtyClipCheck(x, y, w, h);
                }
                animTable.Offset++;
            }

            if (_newDirtyClip)
                goto restart;
        }

        private void DirtyClipCheck(short x, short y, short w, short h)
        {
            short width, height, tmp;

            Ptr<VgaSprite> vsp = _vgaSprites;
            for (; vsp.Value.id != 0; vsp.Offset++)
            {
                if ((vsp.Value.windowNum & 0x8000) != 0)
                    continue;

                if (vsp.Value.image == 0)
                    continue;

                var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, vsp.Value.zoneNum);
                BytePtr ptr = vpe.Value.vgaFile2 + vsp.Value.image * 8;
                width = (short) (ptr.ToUInt32BigEndian(6) / 8);
                height = ptr[5];

                tmp = vsp.Value.x;
                if (tmp >= x)
                {
                    tmp -= w;
                    if (tmp >= x)
                        continue;
                }
                else
                {
                    tmp += width;
                    if (tmp < x)
                        continue;
                }

                tmp = vsp.Value.y;
                if (tmp >= y)
                {
                    tmp -= h;
                    if (tmp >= y)
                        continue;
                }
                else
                {
                    tmp += height;
                    if (tmp < y)
                        continue;
                }

                vsp.Value.windowNum |= 0x8000;
                _newDirtyClip = true;
            }

            Ptr<AnimTable> animTable = _screenAnim1;
            for (; animTable.Value.srcPtr != BytePtr.Null; animTable.Offset++)
            {
                if ((animTable.Value.windowNum & 0x8000) != 0)
                    continue;

                width = (short) (animTable.Value.width * 2);
                height = (short) animTable.Value.height;

                tmp = (short) (animTable.Value.x + _scrollX);
                if (tmp >= x)
                {
                    tmp -= w;
                    if (tmp >= x)
                        continue;
                }
                else
                {
                    tmp += width;
                    if (tmp < x)
                        continue;
                }

                tmp = animTable.Value.y;
                if (tmp >= y)
                {
                    tmp -= h;
                    if (tmp >= y)
                        continue;
                }
                else
                {
                    tmp += height;
                    if (tmp < y)
                        continue;
                }

                animTable.Value.windowNum |= 0x8000;
                _newDirtyClip = true;
            }
        }

        private void RestoreBackGround()
        {
            int images = 0;

            Ptr<AnimTable> animTable = _screenAnim1;
            while (animTable.Value.srcPtr != BytePtr.Null)
            {
                animTable.Offset++;
                images++;
            }

            while (images-- != 0)
            {
                animTable.Offset--;

                if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                    (animTable.Value.windowNum & 0x8000) == 0)
                {
                    continue;
                }

                _windowNum = (ushort) (animTable.Value.windowNum & ~0x8000);

                VC10_state state = new VC10_state();
                state.srcPtr = animTable.Value.srcPtr;
                state.height = state.draw_height = animTable.Value.height;
                state.width = state.draw_width = animTable.Value.width;
                state.y = animTable.Value.y;
                state.x = animTable.Value.x;
                state.palette = 0;
                state.paletteMod = 0;
                state.flags = DrawFlags.kDFNonTrans;

                _backFlag = true;
                DrawImage(state);

                if (_gd.ADGameDescription.gameType != SIMONGameType.GType_SIMON1 &&
                    _gd.ADGameDescription.gameType != SIMONGameType.GType_SIMON2)
                {
                    animTable.Value.srcPtr = BytePtr.Null;
                }
            }
            _backFlag = false;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                Ptr<AnimTable> animTableTmp;

                animTable = animTableTmp = _screenAnim1;
                while (animTable.Value.srcPtr != BytePtr.Null)
                {
                    if ((animTable.Value.windowNum & 0x8000) == 0)
                    {
                        animTableTmp[0] = new AnimTable(animTable[0]);
                        animTableTmp.Offset++;
                    }
                    animTable.Offset++;
                }
                animTableTmp.Value.srcPtr = BytePtr.Null;
            }
        }

        private void SaveBackGround(Ptr<VgaSprite> vsp)
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && _gd.Platform == Platform.AtariST &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
            {
                return;
            }

            if (((vsp.Value.flags & (ushort) DrawFlags.kDFSkipStoreBG) != 0) || vsp.Value.image == 0)
                return;

            Ptr<AnimTable> animTable = _screenAnim1;

            while (animTable.Value.srcPtr != BytePtr.Null)
                animTable.Offset++;

            var ptr = _curVgaFile2 + vsp.Value.image * 8;
            short x = (short) (vsp.Value.x - _scrollX);
            short y = (short) (vsp.Value.y - _scrollY);

            if (_window3Flag == 1)
            {
                animTable.Value.srcPtr = _window4BackScn.Pixels;
            }
            else
            {
                int xoffs = (_videoWindows[vsp.Value.windowNum * 4 + 0] * 2 + x) * 8;
                int yoffs = (_videoWindows[vsp.Value.windowNum * 4 + 1] + y);
                animTable.Value.srcPtr = BackGround + yoffs * _backGroundBuf.Pitch + xoffs;
            }

            animTable.Value.x = x;
            animTable.Value.y = y;

            animTable.Value.width = (ushort) (ptr.ToUInt16BigEndian(6) / 16);
            if ((vsp.Value.flags & 0x40) != 0)
            {
                animTable.Value.width++;
            }

            animTable.Value.height = ptr[5];
            animTable.Value.windowNum = vsp.Value.windowNum;
            animTable.Value.id = vsp.Value.id;
            animTable.Value.zoneNum = vsp.Value.zoneNum;

            animTable.Offset++;
            animTable.Value.srcPtr = BytePtr.Null;
        }

        private void DisplayBoxStars()
        {
            throw new NotImplementedException();
        }

        private void ScrollScreen()
        {
            throw new NotImplementedException();
        }

        private void ClearSurfaces()
        {
            OSystem.GraphicsManager.FillScreen(0);

            if (_backBuf != null)
            {
                BackBuf.Data.Set(BackBuf.Offset, 0, _backBuf.Height * _backBuf.Pitch);
            }
        }

        private void FillBackFromBackGround(int screenHeight, int screenWidth)
        {
            throw new NotImplementedException();
        }

        private void FillBackGroundFromBack()
        {
            var src = BackBuf;
            var dst = BackGround;
            for (int i = 0; i < _screenHeight; i++)
            {
                Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, _screenWidth);
                src.Offset += _backBuf.Pitch;
                dst.Offset += _backGroundBuf.Pitch;
            }
        }

        protected void SetMoveRect(ushort x, ushort y, ushort width, ushort height)
        {
            if (x < _moveXMin)
                _moveXMin = x;

            if (y < _moveYMin)
                _moveYMin = y;

            if (width > _moveXMax)
                _moveXMax = width;

            if (height > _moveYMax)
                _moveYMax = height;
        }

        private void DisplayScreen()
        {
            if (_fastFadeInFlag == 0 && _paletteFlag == 1)
            {
                _paletteFlag = 0;
                if (!ScummHelper.ArrayEquals(_displayPalette, 0, _currentPalette, 0, _displayPalette.Length))
                {
                    Array.Copy(_displayPalette, _currentPalette, _displayPalette.Length);
                    OSystem.GraphicsManager.SetPalette(_displayPalette, 0, 256);
                }
            }

            LocksScreen(screen =>
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                {
                    BytePtr src = BackBuf;
                    var dst = screen.Pixels;
                    for (int i = 0; i < _screenHeight; i++)
                    {
                        Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, _screenWidth);
                        src += _backBuf.Pitch;
                        dst += screen.Pitch;
                    }
                    if (_gd.ADGameDescription.gameId != GameIds.GID_DIMP)
                        FillBackFromBackGround(_screenHeight, _screenWidth);
                }
                else
                {
                    if (_window4Flag == 2)
                    {
                        _window4Flag = 0;

                        ushort srcWidth, width, height;
                        var dst = screen.Pixels;

                        var src = _window4BackScn.Pixels;
                        if (_window3Flag == 1)
                        {
                            src = BackGround;
                        }

                        dst += (_moveYMin + _videoWindows[17]) * screen.Pitch;
                        dst += (_videoWindows[16] * 16) + _moveXMin;

                        src += (_videoWindows[18] * 16 * _moveYMin);
                        src += _moveXMin;

                        srcWidth = (ushort) (_videoWindows[18] * 16);

                        width = (ushort) (_moveXMax - _moveXMin);
                        height = (ushort) (_moveYMax - _moveYMin);

                        for (; height > 0; height--)
                        {
                            Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, width);
                            dst += screen.Pitch;
                            src += srcWidth;
                        }

                        _moveXMin = 0xFFFF;
                        _moveYMin = 0xFFFF;
                        _moveXMax = 0;
                        _moveYMax = 0;
                    }

                    if (_window6Flag == 2)
                    {
                        _window6Flag = 0;

                        var src = _window6BackScn.Pixels;
                        var dst = screen.GetBasePtr(0, 51);
                        for (int i = 0; i < 80; i++)
                        {
                            Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, _window6BackScn.Width);
                            dst += screen.Pitch;
                            src += _window6BackScn.Pitch;
                        }
                    }
                }
            });

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF && _scrollFlag != 0)
            {
                ScrollScreen();
            }

            if (_fastFadeInFlag != 0)
            {
                FastFadeIn();
            }
        }

        private void FastFadeIn()
        {
            if ((_fastFadeInFlag & 0x8000) != 0)
            {
                SlowFadeIn();
            }
            else
            {
                _paletteFlag = 0;
                Array.Copy(_displayPalette, _currentPalette, _displayPalette.Length);
                OSystem.GraphicsManager.SetPalette(_displayPalette, 0, _fastFadeInFlag);
                _fastFadeInFlag = 0;
            }
        }

        private void SlowFadeIn()
        {
            _fastFadeInFlag = (ushort) ((_fastFadeInFlag & ~0x8000) / 3);
            _paletteFlag = 0;

            Array.Clear(_currentPalette, 0, _currentPalette.Length);

            for (var c = 255; c >= 0; c -= 4)
            {
                Ptr<Color> src = _displayPalette;

                for (var p = 0; p < _fastFadeInFlag; p++)
                {
                    if (src.Value.R >= c)
                        _currentPalette[p].R += 4;
                    if (src.Value.G >= c)
                        _currentPalette[p].G += 4;
                    if (src.Value.B >= c)
                        _currentPalette[p].B += 4;
                    src.Offset++;
                }
                OSystem.GraphicsManager.SetPalette(_currentPalette, 0, _fastFadeCount);
                Delay(5);
            }
            _fastFadeInFlag = 0;
        }
    }
}