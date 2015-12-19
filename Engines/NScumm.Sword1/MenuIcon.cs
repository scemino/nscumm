namespace NScumm.Sword1
{
    internal class MenuIcon
    {
        public MenuIcon(byte menuType, byte menuPos, uint resId, uint frame, Screen screen)
        {
            _menuType = menuType;
            _menuPos = menuPos;
            _resId = resId;
            _frame = frame;
            _screen = screen;
            _selected = false;
        }

        public bool WasClicked(ushort mouseX, ushort mouseY)
        {
            if (((_menuType == Menu.MENU_TOP) && (mouseY >= 40)) || ((_menuType == Menu.MENU_BOT) && (mouseY < 440)))
                return false;
            if ((mouseX >= _menuPos * 40) && (mouseX < (_menuPos + 1) * 40))
                return true;
            else
                return false;
        }

        public void SetSelect(bool pSel)
        {
            _selected = pSel;
        }

        public void Draw(byte[] fadeMask = null, sbyte fadeStatus = 0)
        {
            ushort x = (ushort) (_menuPos * 40);
            ushort y = (ushort) ((_menuType == Menu.MENU_TOP) ? (0) : (440));
            _screen.ShowFrame(x, y, _resId, (uint) (_frame + (_selected ? 1 : 0)), fadeMask, fadeStatus);
        }

        private byte _menuType, _menuPos;
        private uint _resId, _frame;
        private bool _selected;
        private Screen _screen;
    }
}