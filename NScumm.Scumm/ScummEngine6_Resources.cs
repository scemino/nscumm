//
//  ScummEngine6_Resources.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        [OpCode(0x9b)]
        protected virtual void ResourceRoutines()
        {
            var subOp = ReadByte();
            switch (subOp)
            {
                case 100:               // SO_LOAD_SCRIPT
                    {
                        var resid = Pop();
                        if (Game.Version >= 7)
                            if (resid >= ResourceManager.NumGlobalScripts)
                                break;
                        ResourceManager.LoadScript(resid);
                    }
                    break;
                case 101:               // SO_LOADSound
                    {
                        var resid = Pop();
                        ResourceManager.LoadSound(Sound.MusicType, resid);
                    }
                    break;
                case 102:               // SO_LOAD_COSTUME
                    {
                        var resid = Pop();
                        ResourceManager.LoadCostume(resid);
                    }
                    break;
                case 103:               // SO_LOAD_ROOM
                    {
                        var resid = Pop();
                        ResourceManager.LoadRoom(resid);
                    }
                    break;
                case 104:               // SO_NUKE_SCRIPT
                    {
                        var resid = Pop();
                        if (Game.Version >= 7)
                            if (resid >= ResourceManager.NumGlobalScripts)
                                break;
                        ResourceManager.SetScriptCounter(resid, 0x7F);
                    }
                    break;
                case 105:               // SO_NUKESound
                    {
                        var resid = Pop();
                        ResourceManager.SetSoundCounter(resid, 0x7F);
                    }
                    break;
                case 106:               // SO_NUKE_COSTUME
                    {
                        var resid = Pop();
                        ResourceManager.SetCostumeCounter(resid, 0x7F);
                    }
                    break;
                case 107:               // SO_NUKE_ROOM
                    {
                        var resid = Pop();
                        ResourceManager.SetRoomCounter(resid, 0x7F);
                    }
                    break;
                case 108:               // SO_LOCK_SCRIPT
                    {
                        var resid = Pop();
                        if (resid >= ResourceManager.NumGlobalScripts)
                            break;
                        ResourceManager.LockScript(resid);
                    }
                    break;
                case 109:               // SO_LOCKSound
                    {
                        var resid = Pop();
                        ResourceManager.LockSound(resid);
                    }
                    break;
                case 110:               // SO_LOCK_COSTUME
                    {
                        var resid = Pop();
                        ResourceManager.LockCostume(resid);
                    }
                    break;
                case 111:               // SO_LOCK_ROOM
                    {
                        var resid = Pop();
                        if (resid > 0x7F)
                            resid = _resourceMapper[resid & 0x7F];
                        ResourceManager.LockRoom(resid);
                    }
                    break;
                case 112:               // SO_UNLOCK_SCRIPT
                    {
                        var resid = Pop();
                        if (resid >= ResourceManager.NumGlobalScripts)
                            break;
                        ResourceManager.UnlockScript(resid);
                    }
                    break;
                case 113:               // SO_UNLOCKSound
                    {
                        var resid = Pop();
                        ResourceManager.UnlockSound(resid);
                    }
                    break;
                case 114:               // SO_UNLOCK_COSTUME
                    {
                        var resid = Pop();
                        ResourceManager.UnlockCostume(resid);
                    }
                    break;
                case 115:               // SO_UNLOCK_ROOM
                    {
                        var resid = Pop();
                        if (resid > 0x7F)
                            resid = _resourceMapper[resid & 0x7F];
                        ResourceManager.UnlockRoom(resid);
                    }
                    break;
                case 116:               // SO_CLEAR_HEAP
                    /* this is actually a scumm message */
                    throw new NotSupportedException("clear heap not working yet");
                case 117:               // SO_LOAD_CHARSET
                    {
                        var resid = Pop();
                        LoadCharset(resid);
                    }
                    break;
                case 118:               // SO_NUKE_CHARSET
                    {
                        var resid = Pop();
                        //                    nukeCharset(resid);
                    }
                    break;
                case 119:               // SO_LOAD_OBJECT
                    {
                        int room, obj;
                        PopRoomAndObj(out room, out obj);
                        LoadFlObject(obj, room);
                        break;
                    }
                default:
                    throw new NotSupportedException(string.Format("ResourceRoutines: default case {0}", subOp));
            }

        }

        protected void PopRoomAndObj(out int room, out int obj)
        {
            if (Game.Version >= 7)
            {
                obj = Pop();
                room = GetObjectRoom(obj);
            }
            else
            {
                room = Pop();
                obj = Pop();
            }

        }

        protected int GetObjectRoom(int obj)
        {
            return _resManager.ObjectRoomTable[obj];
        }
    }
}

