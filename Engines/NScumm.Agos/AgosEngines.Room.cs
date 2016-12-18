namespace NScumm.Agos
{
    partial class AgosEngine
    {
        protected ushort GetExitOf(Item item, ushort d)
        {
            ushort y = 0;

            var subRoom = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
            if (subRoom == null)
                return 0;
            ushort x = d;
            while (x > y)
            {
                if (GetDoorState(item, y) == 0)
                    d--;
                y++;
            }
            return subRoom.roomExit[d];
        }

        protected ushort GetDoorState(Item item, ushort d)
        {
            ushort mask = 3;

            var subRoom = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
            if (subRoom == null)
                return 0;

            d <<= 1;
            mask <<= d;
            var n = (ushort) (subRoom.roomExitStates & mask);
            n >>= d;

            return n;
        }

        protected void SetDoorState(Item i, ushort d, ushort n)
        {
            ushort y = 0;

            var r = (SubRoom) FindChildOfType(i, ChildType.kRoomType);
            if (r == null)
                return;
            var d1 = d;
            while (d > y)
            {
                if (GetDoorState(i, y) == 0)
                    d1--;
                y++;
            }
            ChangeDoorState(r, d, n);

            var j = DerefItem(r.roomExit[d1]);
            if (j == null)
                return;
            var r1 = (SubRoom) FindChildOfType(j, ChildType.kRoomType);
            if (r1 == null)
                return;
            d = GetBackExit(d);
            d1 = d;
            y = 0;
            while (d > y)
            {
                if (GetDoorState(j, y) == 0)
                    d1--;
                y++;
            }
            /* Check are a complete exit pair */
            if (DerefItem(r1.roomExit[d1]) != i)
                return;
            /* Change state of exit coming back */
            ChangeDoorState(r1, d, n);
        }

        private void ChangeDoorState(SubRoom r, ushort d, ushort n)
        {
            ushort mask = 3;
            d <<= 1;
            mask <<= d;
            n <<= d;
            r.roomExitStates = (ushort) (r.roomExitStates & ~mask);
            r.roomExitStates |= n;
        }

        private ushort GetBackExit(int n) {
            switch (n) {
                case 0:
                    return 2;
                case 1:
                    return 3;
                case 2:
                    return 0;
                case 3:
                    return 1;
                case 4:
                    return 5;
                case 5:
                    return 4;
            }

            return 0;
        }
    }
}