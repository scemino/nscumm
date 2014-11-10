﻿//
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

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        [OpCode(0x9b)]
        void ResourceRoutines()
        {
            var subOp = ReadByte();
            switch (subOp)
            {
                case 100:               // SO_LOAD_SCRIPT
                    {
                        var resid = Pop();
//                        if (Game.Version >= 7)
//                        if (resid >= _numGlobalScripts)
//                            break;
//                        ensureResourceLoaded(rtScript, resid);
                    }
                    break;
                case 101:               // SO_LOAD_SOUND
                    {
                        var resid = Pop();
//                        ensureResourceLoaded(rtSound, resid);
                    }
                    break;
                case 102:               // SO_LOAD_COSTUME
                    {

                        var resid = Pop();
//                        ensureResourceLoaded(rtCostume, resid);
                    }
                    break;
                case 103:               // SO_LOAD_ROOM
                    {
                        var resid = Pop();
//                        ensureResourceLoaded(rtRoom, resid);
                    }
                    break;
                case 104:               // SO_NUKE_SCRIPT
                    {
                        var resid = Pop();
//                        if (_game.version >= 7)
//                        if (resid >= _numGlobalScripts)
//                            break;
//                        _res->setResourceCounter(rtScript, resid, 0x7F);
                    }
                    break;
                case 105:               // SO_NUKE_SOUND
                    {
                        var resid = Pop();
//                        _res->setResourceCounter(rtSound, resid, 0x7F);
                    }
                    break;
                case 106:               // SO_NUKE_COSTUME
                    {
                        var resid = Pop();
//                        _res->setResourceCounter(rtCostume, resid, 0x7F);
                    }
                    break;
                case 107:               // SO_NUKE_ROOM
                    {
                        var resid = Pop();
//                        _res->setResourceCounter(rtRoom, resid, 0x7F);
                    }
                    break;
                case 108:               // SO_LOCK_SCRIPT
                    {
                        var resid = Pop();
//                    if (resid >= _numGlobalScripts)
//                        break;
//                    _res->lock(rtScript, resid);
                    }
                    break;
                case 109:               // SO_LOCK_SOUND
                    {
                        var resid = Pop();
//                    _res->lock(rtSound, resid);
                    }
                    break;
                case 110:               // SO_LOCK_COSTUME
                    {
                        var resid = Pop();
//                    _res->lock(rtCostume, resid);
                    }
                    break;
                case 111:               // SO_LOCK_ROOM
                    {
                        var resid = Pop();
//                    if (resid > 0x7F)
//                        resid = _resourceMapper[resid & 0x7F];
//                    _res->lock(rtRoom, resid);
                    }
                    break;
                case 112:               // SO_UNLOCK_SCRIPT
                    {
                        var resid = Pop();
//                    if (resid >= _numGlobalScripts)
//                        break;
//                    _res->unlock(rtScript, resid);
                    }
                    break;
                case 113:               // SO_UNLOCK_SOUND
                    {
                        var resid = Pop();
//                    _res->unlock(rtSound, resid);
                    }
                    break;
                case 114:               // SO_UNLOCK_COSTUME
                    {
                        var resid = Pop();
//                    _res->unlock(rtCostume, resid);
                    }
                    break;
                case 115:               // SO_UNLOCK_ROOM
                    {
                        var resid = Pop();
//                    if (resid > 0x7F)
//                        resid = _resourceMapper[resid & 0x7F];
//                    _res->unlock(rtRoom, resid);
                    }
                    break;
                case 116:               // SO_CLEAR_HEAP
                    /* this is actually a scumm message */
                    throw new NotSupportedException("clear heap not working yet");
                    break;
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

        void PopRoomAndObj(out int room, out int obj)
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

        int GetObjectRoom(int obj)
        {
            throw new NotImplementedException();
//            ScummHelper.AssertRange(0, obj, 200 - 1, "object");
//            return _objectRoomTable[obj];
        }

    }
}

