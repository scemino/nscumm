/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using NScumm.Core.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace NScumm.Core.IO
{
    public struct Resource
    {
        public byte RoomNum;
        public long Offset;
    }

    public abstract class ResourceIndex
    {
        #region Properties

        public string Directory
        {
            get;
            protected set;
        }

        public IDictionary<byte,string> RoomNames
        {
            get;
            protected set;
        }

        public ReadOnlyCollection<Resource> RoomResources
        {
            get;
            protected set;
        }

        public ReadOnlyCollection<Resource> ScriptResources
        {
            get;
            protected set;
        }

        public ReadOnlyCollection<Resource> SoundResources
        {
            get;
            protected set;
        }

        public ReadOnlyCollection<Resource> CostumeResources
        {
            get;
            protected set;
        }

        public byte[] ObjectOwnerTable { get; protected set; }

        public byte[] ObjectStateTable { get; protected set; }

        public uint[] ClassData { get; protected set; }

        #endregion

        #region Public Methods

        public static ResourceIndex Load(GameInfo game)
        {
            ResourceIndex index;
            switch (game.Version)
            {
                case 3:
                    if (game.IsOldBundle)
                    {
                        index = new ResourceIndex3_16();
                    }
                    else
                    {
                        index = new ResourceIndex3();
                    }
                    break;
                case 4:
                    index = new ResourceIndex4();
                    break;
                case 5:
                    index = new ResourceIndex5();
                    break;
                case 6:
                    index = new ResourceIndex6();
                    break;
                default:
                    throw new NotSupportedException("The SCUMM version {0} is not supported.");
            }

            index.LoadIndex(game);
            return index;
        }

        protected abstract void LoadIndex(GameInfo game);

        #endregion
    }
}
