namespace NScumm.Sword1
{
    struct MenuObject
    {
        public int textDesc;
        public uint bigIconRes;
        public uint bigIconFrame;
        public uint luggageIconRes;
        public uint useScript;
    }

    internal class Menu
    {
        const int TOTAL_pockets = 52;

        public static MenuObject[] _objectDefs = new MenuObject[TOTAL_pockets + 1];

        public Menu(Screen screen, Mouse mouse)
        {
            // TODO:
        }

        public const int MENU_TOP = 0;
        public const int MENU_BOT = 1;

        public void Refresh(object menuTop)
        {
            // TODO:
        }

        public void FnChooser(SwordObject cpt)
        {
            // TODO:
        }

        public void FnEndChooser()
        {
            // TODO:
        }

        public void FnStartMenu()
        {
            // TODO:
        }

        public void FnEndMenu()
        {
            // TODO:
        }

        public void CfnReleaseMenu()
        {
            // TODO:
        }

        public void FnAddSubject(int sub)
        {
            // TODO:
        }

        public void CheckTopMenu()
        {
            // TODO:
        }
    }
}