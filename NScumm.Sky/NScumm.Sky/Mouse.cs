using System;

namespace NScumm.Sky
{
    internal class Mouse
    {
        private SkyCompact _skyCompact;
        private Disk _skyDisk;
        private SkySystem _system;
        private ushort _mouseX; //actual mouse coordinates
        private ushort _mouseY;

        public Mouse(SkySystem system, Disk skyDisk, SkyCompact skyCompact)
        {
            _system = system;
            _skyDisk = skyDisk;
            _skyCompact = skyCompact;
        }

        public Logic Logic { get; internal set; }

        public ushort MouseX
        {
            get { return _mouseX; }
        }

        public ushort MouseY
        {
            get { return _mouseY; }
        }

        public void SpriteMouse(ushort _savedMouse, int v1, int v2)
        {
            // TODO: SpriteMouse
        }

        public void ReplaceMouseCursors(int v)
        {
            // TODO: ReplaceMouseCursors
        }

        public void MouseEngine()
        {
            // TODO: MouseEngine
        }

        public void FnOpenCloseHand(bool open)
        {
            throw new NotImplementedException();
            //if (!open && _skyLogic.ScriptVariables[OBJECT_HELD]==0)
            //{
            //    SpriteMouse(1, 0, 0);
            //    return;
            //}
            //ushort cursor = FindMouseCursor(_skyLogic.ScriptVariables[OBJECT_HELD]) << 1;
            //if (open)
            //    cursor++;

            //uint size = ((DataFileHeader*)_objectMouseData)->s_sp_size;

            //var srcData = size * cursor + ServiceLocator.Platform.SizeOf<DataFileHeader>();
            //var destData = ServiceLocator.Platform.SizeOf<DataFileHeader>();
            //Array.Copy(_objectMouseData,srcData, _miceData,destData,size);
            //SpriteMouse(0, 5, 5);
        }

        public bool FnAddHuman()
        {
            //reintroduce the mouse so that the human can control the player
            //could still be switched out at high-level

            if (Logic.ScriptVariables[Logic.MOUSE_STOP] == 0)
            {
                Logic.ScriptVariables[Logic.MOUSE_STATUS] |= 6; //cursor & mouse

                if (_mouseY < 2) //stop mouse activating top line
                    _mouseY = 2;

                // TODO: _system.WarpMouse(_mouseX, _mouseY);

                //force the pointer engine into running a get-off
                //even if it's over nothing

                //KWIK-FIX
                //get off may contain script to remove mouse pointer text
                //surely this script should be run just in case
                //I am going to try it anyway
                if (Logic.ScriptVariables[Logic.GET_OFF] != 0)
                    Logic.Script((ushort) Logic.ScriptVariables[Logic.GET_OFF],
                        (ushort) (Logic.ScriptVariables[Logic.GET_OFF] >> 16));

                Logic.ScriptVariables[Logic.SPECIAL_ITEM] = 0xFFFFFFFF;
                Logic.ScriptVariables[Logic.GET_OFF] = Logic.RESET_MOUSE;
            }

            return true;
        }

        public void FnSaveCoods()
        {
            throw new NotImplementedException();
        }

        public void LockMouse()
        {
            throw new NotImplementedException();
        }

        public void WaitMouseNotPressed(int v)
        {
            throw new NotImplementedException();
        }

        public void UnlockMouse()
        {
            throw new NotImplementedException();
        }
    }
}