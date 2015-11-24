using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    internal class ControlButton
    {
        public ControlButton(ushort x, ushort y, uint resId, ButtonIds id, byte flag, ResMan pResMan, byte[] screenBuf,
            ISystem system)
        {
            _x = x;
            _y = y;
            _id = id;
            _flag = flag;
            _resId = resId;
            _resMan = pResMan;
            _frameIdx = 0;
            _resMan.ResOpen(_resId);
            FrameHeader tmp = new FrameHeader(_resMan.FetchFrame(_resMan.FetchRes(_resId), 0));
            _width = _resMan.ReadUInt16(tmp.width);
            _width = (ushort)((_width > Screen.SCREEN_WIDTH) ? Screen.SCREEN_WIDTH : _width);
            _height = _resMan.ReadUInt16(tmp.height);
            if ((x == 0) && (y == 0))
            { // center the frame (used for panels);
                _x = (ushort)((((640 - _width) / 2) < 0) ? 0 : ((640 - _width) / 2));
                _y = (ushort)((((480 - _height) / 2) < 0) ? 0 : ((480 - _height) / 2));
            }
            _dstBuf = new ByteAccess(screenBuf, _y * Screen.SCREEN_WIDTH + _x);
            _system = system;
        }

        public void Draw()
        {
            FrameHeader fHead = new FrameHeader(_resMan.FetchFrame(_resMan.FetchRes(_resId), (uint)_frameIdx));
            ByteAccess src = new ByteAccess(fHead.Data.Data, fHead.Data.Offset + FrameHeader.Size);
            var dst = new ByteAccess(_dstBuf);

            if (SystemVars.Platform == Platform.PSX && _resId != 0)
            {
                var HIFbuf = new byte[_resMan.ReadUInt16(fHead.height) * _resMan.ReadUInt16(fHead.width)];
                Screen.DecompressHIF(src.Data, src.Offset, HIFbuf);
                src = new ByteAccess(HIFbuf);

                if (_resMan.ReadUInt16(fHead.width) < 300)
                    for (var cnt = 0; cnt < _resMan.ReadUInt16(fHead.height); cnt++)
                    {
                        for (var cntx = 0; cntx < _resMan.ReadUInt16(fHead.width); cntx++)
                            if (src[cntx] != 0)
                                dst[cntx] = src[cntx];

                        dst.Offset += Screen.SCREEN_WIDTH;
                        for (var cntx = 0; cntx < _resMan.ReadUInt16(fHead.width); cntx++)
                            if (src[cntx] != 0)
                                dst[cntx] = src[cntx];

                        dst.Offset += Screen.SCREEN_WIDTH;
                        src.Offset += _resMan.ReadUInt16(fHead.width);
                    }
                else if (_resId == Sword1Res.SR_DEATHPANEL)
                { // Check for death panel psx version (which is 1/3 of original width)
                    for (var cnt = 0; cnt < _resMan.ReadUInt16(fHead.height) / 2; cnt++)
                    {
                        //Stretched panel is bigger than 640px, check we don't draw outside screen
                        for (var cntx = 0; (cntx < (_resMan.ReadUInt16(fHead.width)) / 3) && (cntx < (Screen.SCREEN_WIDTH - 3)); cntx++)
                            if (src[cntx] != 0)
                            {
                                dst[cntx * 3] = src[cntx];
                                dst[cntx * 3 + 1] = src[cntx];
                                dst[cntx * 3 + 2] = src[cntx];
                            }
                        dst.Offset += Screen.SCREEN_WIDTH;

                        for (var cntx = 0; cntx < (_resMan.ReadUInt16(fHead.width)) / 3; cntx++)
                            if (src[cntx] != 0)
                            {
                                dst[cntx * 3] = src[cntx];
                                dst[cntx * 3 + 1] = src[cntx];
                                dst[cntx * 3 + 2] = src[cntx];
                            }
                        dst.Offset += Screen.SCREEN_WIDTH;
                        src.Offset += _resMan.ReadUInt16(fHead.width) / 3;
                    }
                }
                else
                { //save slots needs to be multiplied by 2 in height
                    for (var cnt = 0; cnt < _resMan.ReadUInt16(fHead.height); cnt++)
                    {
                        for (var cntx = 0; cntx < _resMan.ReadUInt16(fHead.width) / 2; cntx++)
                            if (src[cntx] != 0)
                            {
                                dst[cntx * 2] = src[cntx];
                                dst[cntx * 2 + 1] = src[cntx];
                            }

                        dst.Offset += Screen.SCREEN_WIDTH;
                        for (var cntx = 0; cntx < _resMan.ReadUInt16(fHead.width) / 2; cntx++)
                            if (src[cntx] != 0)
                            {
                                dst[cntx * 2] = src[cntx];
                                dst[cntx * 2 + 1] = src[cntx];
                            }

                        dst.Offset += Screen.SCREEN_WIDTH;
                        src.Offset += _resMan.ReadUInt16(fHead.width) / 2;
                    }
                }
            }
            else
                for (var cnt = 0; cnt < _resMan.ReadUInt16(fHead.height); cnt++)
                {
                    for (var cntx = 0; cntx < _resMan.ReadUInt16(fHead.width); cntx++)
                        if (src[cntx] != 0)
                            dst[cntx] = src[cntx];

                    dst.Offset += Screen.SCREEN_WIDTH;
                    src.Offset += _resMan.ReadUInt16(fHead.width);
                }

            _system.GraphicsManager.CopyRectToScreen(_dstBuf.Data, _dstBuf.Offset, Screen.SCREEN_WIDTH, _x, _y, _width, _height);
        }

        public bool WasClicked(ushort mouseX, ushort mouseY)
        {
            if ((_x <= mouseX) && (_y <= mouseY) && (_x + _width >= mouseX) && (_y + _height >= mouseY))
                return true;
            else
                return false;
        }

        public void SetSelected(byte selected)
        {
            _frameIdx = selected;
            Draw();
        }

        public bool IsSaveslot()
        {
            return ((_resId >= Sword1Res.SR_SLAB1) && (_resId <= Sword1Res.SR_SLAB4));
        }

        public ButtonIds _id;
        public byte _flag;

        private int _frameIdx;
        private ushort _x, _y;
        private ushort _width, _height;
        private uint _resId;
        private ResMan _resMan;
        private ByteAccess _dstBuf;
        private ISystem _system;
    }
}