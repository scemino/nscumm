using System;

namespace NScumm.Sword1
{
    class MemMan
    {
        public const int MEM_FREED = 0;
        public const int MEM_CAN_FREE = 1;
        public const int MEM_DONT_FREE = 2;

        const int MAX_ALLOC = 6 * 1024 * 1024; // max amount of mem we want to alloc().


        uint _alloced;  //currently allocated memory
        MemHandle _memListFree;
        MemHandle _memListFreeEnd;

        public void InitHandle(MemHandle bsMem)
        {
            // TODO:
        }

        public void Alloc(MemHandle bsMem, uint pSize, ushort pCond = MEM_DONT_FREE)
        {
            _alloced += pSize;
            bsMem.data = new byte[pSize];
            bsMem.cond = pCond;
            bsMem.size = pSize;
            if (pCond == MEM_CAN_FREE)
            {
                // TODO: warning("%d Bytes alloced as FREEABLE.", pSize); // why should one want to alloc mem if it can be freed?
                AddToFreeList(bsMem);
            }
            else if (bsMem.next != null || bsMem.prev != null) // it's in our _freeAble list, remove it from there
                RemoveFromFreeList(bsMem);
            CheckMemoryUsage();
        }

        public void SetCondition(MemHandle bsMem, ushort pCond)
        {
            if ((pCond == MEM_FREED) || (pCond > MEM_DONT_FREE))
                throw new InvalidOperationException("MemMan::setCondition: program tried to set illegal memory condition");
            if (bsMem.cond != pCond)
            {
                bsMem.cond = pCond;
                if (pCond == MEM_DONT_FREE)
                    RemoveFromFreeList(bsMem);
                else if (pCond == MEM_CAN_FREE)
                    AddToFreeList(bsMem);
            }
        }

        void AddToFreeList(MemHandle bsMem)
        {
            if (bsMem.next != null || bsMem.prev != null)
            {
                // TODO: warning("addToFreeList: mem block is already in freeList");
                return;
            }
            bsMem.prev = null;
            bsMem.next = _memListFree;
            if (bsMem.next != null)
                bsMem.next.prev = bsMem;
            _memListFree = bsMem;
            if (_memListFreeEnd == null)
                _memListFreeEnd = _memListFree;
        }

        void RemoveFromFreeList(MemHandle bsMem)
        {
            if (_memListFree == bsMem)
                _memListFree = bsMem.next;
            if (_memListFreeEnd == bsMem)
                _memListFreeEnd = bsMem.prev;

            if (bsMem.next != null)
                bsMem.next.prev = bsMem.prev;
            if (bsMem.prev != null)
                bsMem.prev.next = bsMem.next;
            bsMem.next = bsMem.prev = null;
        }

        void CheckMemoryUsage()
        {
            while ((_alloced > MAX_ALLOC) && _memListFree != null)
            {
                _memListFreeEnd.data = null;
                _memListFreeEnd.cond = MEM_FREED;
                _alloced -= _memListFreeEnd.size;
                RemoveFromFreeList(_memListFreeEnd);
            }
        }
    }
}