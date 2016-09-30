//
//  ScummEngine_SaveLoad.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    public enum ResType
    {
        Invalid = 0,
        First = 1,
        Room = 1,
        Script = 2,
        Costume = 3,
        Sound = 4,
        Inventory = 5,
        Charset = 6,
        String = 7,
        Verb = 8,
        ActorName = 9,
        Buffer = 10,
        ScaleTable = 11,
        Temp = 12,
        FlObject = 13,
        Matrix = 14,
        Box = 15,
        ObjectName = 16,
        RoomScripts = 17,
        RoomImage = 18,
        Image = 19,
        Talkie = 20,
        SpoolBuffer = 21,
        VerbImage = 22,
        Last = 22
    }

    partial class ScummEngine
    {
        const byte TumbnailVersion = 1;
        const uint InfoSectionVersion = 2;
        const uint SaveInfoSectionSize = (4 + 4 + 4 + 4 + 4 + 4 + 2);
        const uint SaveCurrentVersion = 94;
        protected string _savegame;
        protected internal int _saveLoadFlag;
        protected int _saveLoadSlot;
        protected bool _saveTemporaryState;

        public void Load(string savegame)
        {
            _saveLoadFlag = 2;
            _savegame = savegame;
            _saveTemporaryState = false;
        }

        void SaveLoad()
        {
            if (_saveLoadFlag != 0)
            {
                if (_savegame == null)
                {
                    var dir = ServiceLocator.FileStorage.GetDirectoryName(Game.Path);
                    _savegame = ServiceLocator.FileStorage.Combine(dir, string.Format("{0}_{1}{2}.sav", Game.Id, _saveTemporaryState ? 'c' : 's', (_saveLoadSlot + 1)));
                }
                if (_saveLoadFlag == 2)
                {
                    if (ServiceLocator.FileStorage.FileExists(_savegame))
                    {
                        LoadState(_savegame);
                        if (_saveTemporaryState && Game.Version <= 7)
                        {
                            _variables[VariableGameLoaded.Value] = (_game.Version == 8) ? 1 : 203;
                        }
                    }
                }
                else if (_saveLoadFlag == 1)
                {
                    SaveState(_savegame, ServiceLocator.FileStorage.GetFileNameWithoutExtension(_savegame));
                    if (_saveTemporaryState)
                    {
                        _variables[VariableGameLoaded.Value] = 201;
                    }
                }

                // update IQ points after loading
                if (_saveLoadFlag == 2)
                {
                    if (_game.GameId == Scumm.IO.GameId.Indy4)
                        RunScript(145, false, false, new int[0]);
                }

                _saveLoadFlag = 0;
            }
        }

        protected void SaveState(string path, string name)
        {
            using (var file = ServiceLocator.FileStorage.OpenFileWrite(path))
            {
                var bw = new BinaryWriter(file);
                SaveHeader(name, bw);

                SaveInfos(bw);

                var serializer = Serializer.CreateWriter(bw, CurrentVersion);
                SaveOrLoad(serializer);
            }
        }

        protected bool LoadState(int slot, bool compat)
        {
            var filename = ServiceLocator.FileStorage.Combine(ServiceLocator.FileStorage.GetDirectoryName(Game.Path), MakeSavegameName(slot, compat));
            return LoadState(filename);
        }

        protected bool LoadState(string path)
        {
            using (var file = ServiceLocator.FileStorage.OpenFileRead(path))
            {
                var br = new BinaryReader(file);
                var hdr = LoadSaveGameHeader(br);
                var serializer = Serializer.CreateReader(br, hdr.Version);

                // Since version 56 we save additional information about the creation of
                // the save game and the save time.
                if (hdr.Version >= 56)
                {
                    var infos = LoadInfos(br);
                    if (infos == null)
                    {
                        //warning("Info section could not be found");
                        //delete in;
                        return false;
                    }

                    //SetTotalPlayTime(infos.playtime * 1000);
                }
                //else
                //{
                // start time counting
                //setTotalPlayTime();
                //}

                // Due to a bug in scummvm up to and including 0.3.0, save games could be saved
                // in the V8/V9 format but were tagged with a V7 mark. Ouch. So we just pretend V7 == V8 here
                if (hdr.Version == 7)
                    hdr.Version = 8;

                //_saveLoadDescription = hdr.name;

                // Unless specifically requested with _saveSound, we do not save the iMUSE
                // state for temporary state saves - such as certain cutscenes in DOTT,
                // FOA, Sam and Max, etc.
                //
                // Thus, we should probably not stop music when restoring from one of
                // these saves. This change stops the Mole Man theme from going quiet in
                // Sam & Max when Doug tells you about the Ball of Twine, as mentioned in
                // patch #886058.
                //
                // If we don't have iMUSE at all we may as well stop the sounds. The previous
                // default behavior here was to stopAllSounds on all state restores.

                if (IMuse == null || _saveSound || !_saveTemporaryState)
                    Sound.StopAllSounds();

                //            Sound->stopCD();

                Sound.PauseSounds(true);

                //closeRoom();

                _inventory = new ushort[_inventory.Length];
                _invData = new ObjectData[_invData.Length];
                _newNames.Clear();

                // Because old savegames won't fill the entire gfxUsageBits[] array,
                // clear it here just to be sure it won't hold any unforseen garbage.
                Gdi.ClearGfxUsageBits();

                // Nuke all resources
                //for (ResType type = rtFirst; type <= rtLast; type = ResType(type + 1))
                //    if (type != rtTemp && type != rtBuffer && (type != rtSound || _saveSound || !compat))
                //        for (ResId idx = 0; idx < _res->_types[type].size(); idx++)
                //        {
                //            _res->nukeResource(type, idx);
                //        }
                Array.Clear(_strings, 0, _strings.Length);

                ResetScummVars();

                //if (_game.features & GF_OLD_BUNDLE)
                //    loadCharset(0); // FIXME - HACK ?

                //
                // Now do the actual loading
                //
                SaveOrLoad(serializer);

                // Update volume settings
//                SyncSoundSettings();

                if (Game.Version < 7)
                {
                    Camera.LastPosition.X = Camera.CurrentPosition.X;
                }

                var sb = _screenB;
                var sh = _screenH;

                // Restore the virtual screens and force a fade to black.
                InitScreens(0, ScreenHeight);

                Gdi.Fill(MainVirtScreen.Surfaces[0].Pixels, MainVirtScreen.Pitch, 0, MainVirtScreen.Width, MainVirtScreen.Height);
                MainVirtScreen.SetDirtyRange(0, MainVirtScreen.Height);
                UpdateDirtyScreen(MainVirtScreen);
                //UpdatePalette();
                _gfxManager.SetPalette(_currentPalette.Colors);
                InitScreens(sb, sh);

                _completeScreenRedraw = true;

                // Reset charset mask
                _charset.HasMask = false;
                ClearTextSurface();
                ClearDrawObjectQueue();
                _verbMouseOver = 0;

                CameraMoved();

                Gdi.NumZBuffer = GetNumZBuffers();
                Gdi.SetMaskHeight(roomData.Header.Height);

                if (VariableRoomFlag.HasValue)
                {
                    Variables[VariableRoomFlag.Value] = 1;
                }

                // Sync with current config setting
                if (VariableVoiceMode.HasValue)
                {
                    Variables[VariableVoiceMode.Value] = (int)VoiceMode.VoiceAndText;
                }

                Sound.PauseSounds(false);
            }

            return true;
        }

        protected virtual void SaveOrLoad(Serializer serializer)
        {
            uint ENCD_offs = 0;
            uint EXCD_offs = 0;
            uint IM00_offs = 0;
            uint CLUT_offs = 0;
            uint EPAL_offs = 0;
            uint PALS_offs = 0;
            byte numObjectsInRoom = (byte)_objs.Length;

            #region MainEntries

            var mainEntries = new[]
            {
                LoadAndSaveEntry.Create(reader => _gameMD5 = reader.ReadBytes(16), writer => writer.Write(_gameMD5), 39),
                LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.Write(roomData.Header.Width), 8, 50),
                LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.Write(roomData.Header.Height), 8, 50),
                LoadAndSaveEntry.Create(reader => ENCD_offs = reader.ReadUInt32(), writer => writer.Write(ENCD_offs), 8, 50),
                LoadAndSaveEntry.Create(reader => EXCD_offs = reader.ReadUInt32(), writer => writer.Write(EXCD_offs), 8, 50),
                LoadAndSaveEntry.Create(reader => IM00_offs = reader.ReadUInt32(), writer => writer.Write(IM00_offs), 8, 50),
                LoadAndSaveEntry.Create(reader => CLUT_offs = reader.ReadUInt32(), writer => writer.Write(CLUT_offs), 8, 50),
                LoadAndSaveEntry.Create(reader => EPAL_offs = reader.ReadUInt32(), writer => writer.Write(EPAL_offs), 8, 9),
                LoadAndSaveEntry.Create(reader => PALS_offs = reader.ReadUInt32(), writer => writer.Write(PALS_offs), 8, 50),
                LoadAndSaveEntry.Create(reader => _curPalIndex = reader.ReadByte(), writer => writer.WriteByte(_curPalIndex), 8),
                LoadAndSaveEntry.Create(reader => _currentRoom = reader.ReadByte(), writer => writer.Write(_currentRoom), 8),
                LoadAndSaveEntry.Create(reader => _roomResource = reader.ReadByte(), writer => writer.Write(_roomResource), 8),
                LoadAndSaveEntry.Create(reader => numObjectsInRoom = reader.ReadByte(), writer => writer.Write(numObjectsInRoom), 8),
                LoadAndSaveEntry.Create(reader => CurrentScript = reader.ReadByte(), writer => writer.Write(CurrentScript), 8),
                LoadAndSaveEntry.Create(reader => reader.ReadUInt32s(NumLocalScripts), writer => writer.Write(new uint[NumLocalScripts], NumLocalScripts), 8, 50),
                // vm.localvar grew from 25 to 40 script entries and then from
                // 16 to 32 bit variables (but that wasn't reflect here)... and
                // THEN from 16 to 25 variables.
                LoadAndSaveEntry.Create(reader =>
                    {
                        for (int i = 0; i < 25; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadUInt16s(17));
                        }
                    }, writer =>
                    {
                        for (int i = 0; i < 25; i++)
                        {
                            writer.WriteUInt16s(_slots[i].LocalVariables.Cast<ushort>().ToArray(), 17);
                        }
                    }, 8, 8),
                LoadAndSaveEntry.Create(reader =>
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadUInt16s(17));
                        }
                    }, writer =>
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            writer.WriteUInt16s(_slots[i].LocalVariables.Cast<ushort>().ToArray(), 17);
                        }
                    }, 9, 14),
                // We used to save 25 * 40 = 1000 blocks; but actually, each 'row consisted of 26 entry,
                // i.e. 26 * 40 = 1040. Thus the last 40 blocks of localvar where not saved at all. To be
                // able to load this screwed format, we use a trick: We load 26 * 38 = 988 blocks.
                // Then, we mark the followin 12 blocks (24 bytes) as obsolete.
                LoadAndSaveEntry.Create(reader =>
                    {
                        for (int i = 0; i < 38; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadUInt16s(26));
                        }
                    }, writer =>
                    {
                        for (int i = 0; i < 38; i++)
                        {
                            writer.WriteUInt16s(_slots[i].LocalVariables.Cast<ushort>().ToArray(), 26);
                        }
                    }, 15, 17),
                // TODO
                //MK_OBSOLETE_ARRAY(ScummEngine, vm.localvar[39][0], sleUint16, 12, VER(15), VER(17)),
                // This was the first proper multi dimensional version of the localvars, with 32 bit values
                LoadAndSaveEntry.Create(reader =>
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadInt32s(26));
                        }
                    }, writer =>
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            writer.WriteInt32s(_slots[i].LocalVariables, 26);
                        }
                    }, 18, 19),

                // Then we doubled the script slots again, from 40 to 80
                LoadAndSaveEntry.Create(reader =>
                    {
                        for (int i = 0; i < NumScriptSlot; i++)
                        {
                            _slots[i].InitializeLocals(reader.ReadInt32s(26));
                        }
                    }, writer =>
                    {
                        for (int i = 0; i < NumScriptSlot; i++)
                        {
                            writer.WriteInt32s(_slots[i].LocalVariables, 26);
                        }
                    }, 20),

                LoadAndSaveEntry.Create(reader => _resourceMapper = reader.ReadBytes(128), writer => writer.Write(_resourceMapper), 8),
                LoadAndSaveEntry.Create(reader => CharsetColorMap = reader.ReadBytes(16), writer => writer.Write(CharsetColorMap), 8),

                // _charsetData grew from 10*16, to 15*16, to 23*16 bytes
                LoadAndSaveEntry.Create(reader => reader.ReadMatrixBytes(10, 16), writer => writer.WriteMatrixBytes(new byte[16, 10], 10, 16), 8, 9),
                LoadAndSaveEntry.Create(reader => reader.ReadMatrixBytes(15, 16), writer => writer.WriteMatrixBytes(new byte[16, 15], 15, 16), 10, 66),
                LoadAndSaveEntry.Create(reader => reader.ReadMatrixBytes(23, 16), writer => writer.WriteMatrixBytes(new byte[16, 23], 23, 16), 67),

                LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.WriteUInt16(0), 8, 62),

                LoadAndSaveEntry.Create(reader => _camera.DestinationPosition.X = reader.ReadInt16(), writer => writer.WriteInt16(_camera.DestinationPosition.X), 8),
                LoadAndSaveEntry.Create(reader => _camera.DestinationPosition.Y = reader.ReadInt16(), writer => writer.WriteInt16(_camera.DestinationPosition.Y), 8),
                LoadAndSaveEntry.Create(reader => _camera.CurrentPosition.X = reader.ReadInt16(), writer => writer.WriteInt16(_camera.CurrentPosition.X), 8),
                LoadAndSaveEntry.Create(reader => _camera.CurrentPosition.Y = reader.ReadInt16(), writer => writer.WriteInt16(_camera.CurrentPosition.Y), 8),
                LoadAndSaveEntry.Create(reader => _camera.LastPosition.X = reader.ReadInt16(), writer => writer.WriteInt16(_camera.LastPosition.X), 8),
                LoadAndSaveEntry.Create(reader => _camera.LastPosition.Y = reader.ReadInt16(), writer => writer.WriteInt16(_camera.LastPosition.Y), 8),
                LoadAndSaveEntry.Create(reader => _camera.Accel.X = reader.ReadInt16(), writer => writer.WriteInt16(_camera.Accel.X), 8),
                LoadAndSaveEntry.Create(reader => _camera.Accel.Y = reader.ReadInt16(), writer => writer.WriteInt16(_camera.Accel.Y), 8),
                LoadAndSaveEntry.Create(reader => _screenStartStrip = reader.ReadInt16(), writer => writer.WriteInt16(_screenStartStrip), 8),
                LoadAndSaveEntry.Create(reader => _screenEndStrip = reader.ReadInt16(), writer => writer.WriteInt16(_screenEndStrip), 8),
                LoadAndSaveEntry.Create(reader => _camera.Mode = (CameraMode)reader.ReadByte(), writer => writer.Write((byte)_camera.Mode), 8),
                LoadAndSaveEntry.Create(reader => _camera.ActorToFollow = reader.ReadByte(), writer => writer.Write(_camera.ActorToFollow), 8),
                LoadAndSaveEntry.Create(reader => _camera.LeftTrigger = reader.ReadInt16(), writer => writer.WriteInt16(_camera.LeftTrigger), 8),
                LoadAndSaveEntry.Create(reader => _camera.RightTrigger = reader.ReadInt16(), writer => writer.WriteInt16(_camera.RightTrigger), 8),
                LoadAndSaveEntry.Create(reader => _camera.MovingToActor = reader.ReadUInt16() != 0, writer => writer.WriteUInt16(_camera.MovingToActor), 8),

                LoadAndSaveEntry.Create(reader => _actorToPrintStrFor = reader.ReadByte(), writer => writer.WriteByte(_actorToPrintStrFor), 8),
                LoadAndSaveEntry.Create(reader => _charsetColor = reader.ReadByte(), writer => writer.WriteByte(_charsetColor), 8),

                // _charsetBufPos was changed from byte to int
                LoadAndSaveEntry.Create(reader => _charsetBufPos = reader.ReadByte(), writer => writer.WriteByte(_charsetBufPos), 8, 9),
                LoadAndSaveEntry.Create(reader => _charsetBufPos = reader.ReadInt16(), writer => writer.WriteInt16(_charsetBufPos), 10),

                LoadAndSaveEntry.Create(reader => _haveMsg = reader.ReadByte(), writer => writer.WriteByte(_haveMsg), 8),
                LoadAndSaveEntry.Create(reader => _haveActorSpeechMsg = reader.ReadByte() != 0, writer => writer.WriteByte(_haveActorSpeechMsg), 61),
                LoadAndSaveEntry.Create(reader => _useTalkAnims = reader.ReadByte() != 0, writer => writer.WriteByte(_useTalkAnims), 8),

                LoadAndSaveEntry.Create(reader => _talkDelay = reader.ReadInt16(), writer => writer.WriteInt16(_talkDelay), 8),
                LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 8),
                LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 8, 27),
                LoadAndSaveEntry.Create(reader => SentenceNum = reader.ReadByte(), writer => writer.WriteByte(SentenceNum), 8),

                LoadAndSaveEntry.Create(reader => cutScene.SaveOrLoad(serializer), writer => cutScene.SaveOrLoad(serializer), 8),

                LoadAndSaveEntry.Create(reader => _numNestedScripts = reader.ReadByte(), writer => writer.WriteByte(_numNestedScripts), 8),
                LoadAndSaveEntry.Create(reader => _userPut = (sbyte)reader.ReadByte(), writer => writer.WriteByte(_userPut), 8),
                LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.WriteUInt16(0), 17),
                LoadAndSaveEntry.Create(reader => _cursor.State = (sbyte)reader.ReadByte(), writer => writer.WriteByte(_cursor.State), 8),
                LoadAndSaveEntry.Create(reader => reader.ReadByte(), writer => writer.WriteByte(0), 8, 20),
                LoadAndSaveEntry.Create(reader => _currentCursor = reader.ReadByte(), writer => writer.WriteByte(_currentCursor), 8),
                LoadAndSaveEntry.Create(reader => _cursorData = reader.ReadBytes(8192), writer =>
                    {
                        var data = new byte[8192];
                        if (_cursorData != null)
                        {
                            Array.Copy(_cursorData, data, _cursorData.Length);
                        }
                        writer.Write(data);
                    }, 20),
                LoadAndSaveEntry.Create(reader => _cursor.Width = reader.ReadInt16(), writer => writer.WriteInt16(_cursor.Width), 20),
                LoadAndSaveEntry.Create(reader => _cursor.Height = reader.ReadInt16(), writer => writer.WriteInt16(_cursor.Height), 20),
                LoadAndSaveEntry.Create(reader => _cursor.Hotspot = new Point(reader.ReadInt16(), reader.ReadInt16()), writer =>
                    {
                        writer.WriteInt16(_cursor.Hotspot.X);
                        writer.WriteInt16(_cursor.Hotspot.Y);
                    }, 20),
                LoadAndSaveEntry.Create(reader => _cursor.Animate = reader.ReadByte() != 0, writer => writer.WriteByte(_cursor.Animate), 20),
                LoadAndSaveEntry.Create(reader => _cursor.AnimateIndex = reader.ReadByte(), writer => writer.WriteByte(_cursor.AnimateIndex), 20),
                LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 20),
                LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 20),

                LoadAndSaveEntry.Create(reader => reader.ReadBytes(256), writer => writer.Write(new byte[256]), 60),
                LoadAndSaveEntry.Create(reader => _doEffect = reader.ReadByte() != 0, writer => writer.WriteByte(_doEffect), 8),
                LoadAndSaveEntry.Create(reader => _switchRoomEffect = reader.ReadByte(), writer => writer.WriteByte(_switchRoomEffect), 8),
                LoadAndSaveEntry.Create(reader => _newEffect = reader.ReadByte(), writer => writer.WriteByte(_newEffect), 8),
                LoadAndSaveEntry.Create(reader => _switchRoomEffect2 = reader.ReadByte(), writer => writer.WriteByte(_switchRoomEffect2), 8),
                LoadAndSaveEntry.Create(reader => _bgNeedsRedraw = reader.ReadByte() != 0, writer => writer.WriteByte(_bgNeedsRedraw), 8),

                // The state of palManipulate is stored only since V10
                LoadAndSaveEntry.Create(reader => _palManipStart = reader.ReadByte(), writer => writer.WriteByte(_palManipStart), 10),
                LoadAndSaveEntry.Create(reader => _palManipEnd = reader.ReadByte(), writer => writer.WriteByte(_palManipEnd), 10),
                LoadAndSaveEntry.Create(reader => _palManipCounter = reader.ReadUInt16(), writer => writer.WriteUInt16(_palManipCounter), 10),

                // gfxUsageBits grew from 200 to 410 entries. Then 3 * 410 entries:
                LoadAndSaveEntry.Create(reader => Gdi.SaveOrLoad(serializer), writer => Gdi.SaveOrLoad(serializer), 0),

                LoadAndSaveEntry.Create(reader => Gdi.TransparentColor = reader.ReadByte(), writer => writer.WriteByte(Gdi.TransparentColor), 8, 50),
                LoadAndSaveEntry.Create(reader =>
                    {
                        for (int i = 0; i < 256; i++)
                        {
                            _currentPalette.Colors[i] = Color.FromRgb(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                        }
                    }, writer =>
                    {
                        for (int i = 0; i < 256; i++)
                        {
                            var l_color = _currentPalette.Colors[i];
                            writer.WriteByte(l_color.R);
                            writer.WriteByte(l_color.G);
                            writer.WriteByte(l_color.B);
                        }
                    }, 8),
                LoadAndSaveEntry.Create(reader => reader.ReadBytes(768), writer => writer.Write(new byte[768]), 53),

                // Sam & Max specific palette replaced by _shadowPalette now.
                LoadAndSaveEntry.Create(reader => reader.ReadBytes(256), writer => writer.Write(new byte[256]), 8, 33),

                LoadAndSaveEntry.Create(reader => _charsetBuffer = reader.ReadBytes(256), writer => writer.WriteBytes(_charsetBuffer, 256), 8),

                LoadAndSaveEntry.Create(reader => EgoPositioned = reader.ReadByte() != 0, writer => writer.WriteByte(EgoPositioned), 8),

                // _gdi->_imgBufOffs grew from 4 to 5 entries. Then one day we realized
                // that we don't have to store it since initBGBuffers() recomputes it.
                LoadAndSaveEntry.Create(reader => reader.ReadUInt16s(4), writer => writer.WriteUInt16s(new ushort[4], 4), 8, 9),
                LoadAndSaveEntry.Create(reader => reader.ReadUInt16s(5), writer => writer.WriteUInt16s(new ushort[5], 5), 10, 26),

                // See _imgBufOffs: _numZBuffer is recomputed by initBGBuffers().
                LoadAndSaveEntry.Create(reader => Gdi.NumZBuffer = reader.ReadByte(), writer => writer.WriteByte(Gdi.NumZBuffer), 8, 26),

                LoadAndSaveEntry.Create(reader => _screenEffectFlag = reader.ReadByte() != 0, writer => writer.WriteByte(_screenEffectFlag), 8),

                LoadAndSaveEntry.Create(reader => reader.ReadByte(), writer => writer.WriteByte(0), 8, 9),
                LoadAndSaveEntry.Create(reader => reader.ReadByte(), writer => writer.WriteByte(0), 8, 9),

                // Converted _shakeEnabled to boolean and added a _shakeFrame field.
                LoadAndSaveEntry.Create(reader => _shakeEnabled = reader.ReadInt16() == 1, writer => writer.WriteInt16(_shakeEnabled ? 1 : 0), 8, 9),
                LoadAndSaveEntry.Create(reader => _shakeEnabled = reader.ReadBoolean(), writer => writer.WriteByte(_shakeEnabled), 10),
                LoadAndSaveEntry.Create(reader => _shakeFrame = (int)reader.ReadUInt32(), writer => writer.WriteUInt32((uint)_shakeFrame), 10),

                LoadAndSaveEntry.Create(reader => _keepText = reader.ReadByte() != 0, writer => writer.WriteByte(_keepText), 8),

                LoadAndSaveEntry.Create(reader => _screenB = reader.ReadUInt16(), writer => writer.WriteUInt16(_screenB), 8),
                LoadAndSaveEntry.Create(reader => _screenH = reader.ReadUInt16(), writer => writer.WriteUInt16(_screenH), 8),

                LoadAndSaveEntry.Create(reader => reader.ReadUInt16(), writer => writer.WriteUInt16(0), 47),

                LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 9, 9),
                LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 9, 9),
                LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 9, 9),
                LoadAndSaveEntry.Create(reader => reader.ReadInt16(), writer => writer.WriteInt16(0), 9, 9)
            };

            #endregion MainEntries

            var md5Backup = new byte[16];
            Array.Copy(_gameMD5, md5Backup, 16);

            for (int i = 0; i < mainEntries.Length; i++)
            {
                mainEntries[i].Execute(serializer);
            }

            if (serializer.IsLoading)
            {
                roomData = _resManager.GetRoom(_roomResource);
            }
            //if (!Array.Equals(md5Backup, _gameMD5))
            //{
            //    //warning("Game was saved with different gamedata - you may encounter problems");
            //    //debug(1, "You have %s and save is %s.", md5str2, md5str1);
            //    return false;
            //}

            // Starting V14, we extended the usage bits, to be able to cope with games
            // that have more than 30 actors (up to 94 are supported now, in theory).
            // Since the format of the usage bits was changed by this, we have to
            // convert them when loading an older savegame.
//            if (serializer.IsLoading && serializer.Version < 14)
//                Gdi.UpgradeGfxUsageBits();

            // When loading, move the mouse to the saved mouse position.
            //if (serializer.Version >= 20)
            //{
            //    UpdateCursor();
            //    _system->warpMouse(_mouse.x, _mouse.y);
            //}

            // Before V61, we re-used the _haveMsg flag to handle "alternative" speech
            // sound files (see charset code 10).
            if (serializer.IsLoading && serializer.Version < 61)
            {
                if (_haveMsg == 0xFE)
                {
                    _haveActorSpeechMsg = false;
                    _haveMsg = 0xFF;
                }
                else
                {
                    _haveActorSpeechMsg = true;
                }
            }

            //
            // Save/load actors
            //
            for (int i = 0; i < Actors.Length; i++)
            {
                Actors[i].SaveOrLoad(serializer);
            }

            //
            // Save/load sound data
            //
            Sound.SaveOrLoad(serializer);

            //
            // Save/load script data
            //
            if (serializer.Version < 9)
            {
                for (int i = 0; i < 25; i++)
                {
                    _slots[i].SaveOrLoad(serializer, roomData.LocalScripts, ResourceManager.NumGlobalScripts);
                }
            }
            else if (serializer.Version < 20)
            {
                for (int i = 0; i < 40; i++)
                {
                    _slots[i].SaveOrLoad(serializer, roomData.LocalScripts, ResourceManager.NumGlobalScripts);
                }
            }
            else
            {
                for (int i = 0; i < NumScriptSlot; i++)
                {
                    _slots[i].SaveOrLoad(serializer, roomData.LocalScripts, ResourceManager.NumGlobalScripts);
                }
            }
            if (serializer.IsLoading)
            {
                _slots.ForEach(slot =>
                    {
                        if (slot.Where == WhereIsObject.Global)
                        {
                            slot.Offset -= 6;
                        }
                        else if (slot.Where == WhereIsObject.Local && slot.Number >= ResourceManager.NumGlobalScripts && roomData.LocalScripts[slot.Number - ResourceManager.NumGlobalScripts] != null)
                        {
                            slot.Offset = (uint)(slot.Offset - roomData.LocalScripts[slot.Number - ResourceManager.NumGlobalScripts].Offset);
                        }
                    });

                ResetRoomObjects();
            }

            //
            // Save/load local objects
            //
            for (int i = 0; i < _objs.Length; i++)
            {
                _objs[i].SaveOrLoad(serializer);
            }

            //
            // Save/load misc stuff
            //
            for (int i = 0; i < Verbs.Length; i++)
            {
                Verbs[i].SaveOrLoad(serializer);
            }
            for (int i = 0; i < 16; i++)
            {
                _nest[i].SaveOrLoad(serializer);
            }
            for (int i = 0; i < 6; i++)
            {
                _sentence[i].SaveOrLoad(serializer);
            }
            for (int i = 0; i < 6; i++)
            {
                _string[i].SaveOrLoad(serializer);
            }
            for (int i = 0; i < 16; i++)
            {
                _colorCycle[i].SaveOrLoad(serializer);
            }
            if (serializer.Version >= 13)
            {
                for (int i = 0; i < 20; i++)
                {
                    if (serializer.IsLoading)
                    {
                        _scaleSlots[i] = new ScaleSlot();
                    }
                    if (_scaleSlots[i] != null)
                    {
                        _scaleSlots[i].SaveOrLoad(serializer);
                    }
                }
            }

            //
            // Save/load resources
            //
            SaveOrLoadResources(serializer);

            //
            // Save/load global object state
            //
            var objStatesEntries = new[]
            {
                LoadAndSaveEntry.Create(reader =>
                    {
                        var objectOwnerTable = reader.ReadBytes(_resManager.ObjectOwnerTable.Length);
                        Array.Copy(objectOwnerTable, _resManager.ObjectOwnerTable, _resManager.ObjectOwnerTable.Length);
                    },
                    writer => writer.WriteBytes(_resManager.ObjectOwnerTable, _resManager.ObjectOwnerTable.Length)),
                LoadAndSaveEntry.Create(reader =>
                    {
                        var objectStateTable = reader.ReadBytes(_resManager.ObjectStateTable.Length);
                        Array.Copy(objectStateTable, _resManager.ObjectStateTable, _resManager.ObjectStateTable.Length);
                    },
                    writer => writer.WriteBytes(_resManager.ObjectStateTable, _resManager.ObjectStateTable.Length))
            };
            objStatesEntries.ForEach(e => e.Execute(serializer));

            //if (_objectRoomTable)
            //    s->saveLoadArrayOf(_objectRoomTable, _numGlobalObjects, sizeof(_objectRoomTable[0]), sleByte);

            //
            // Save/load palette data
            // Don't save 16 bit palette in FM-Towns and PCE games, since it gets regenerated afterwards anyway.
            //if (_16BitPalette && !(_game.platform == Common::kPlatformFMTowns && s->getVersion() < VER(82)) && !((_game.platform == Common::kPlatformFMTowns || _game.platform == Common::kPlatformPCEngine) && s->getVersion() > VER(87))) {
            //    s->saveLoadArrayOf(_16BitPalette, 512, sizeof(_16BitPalette[0]), sleUint16);
            //}

            var paletteEntries = new[]
            {
                LoadAndSaveEntry.Create(
                    reader => _shadowPalette = reader.ReadBytes(_shadowPalette.Length),
                    writer => writer.WriteBytes(_shadowPalette, _shadowPalette.Length)),
                // _roomPalette didn't show up until V21 save games
                // Note that we also save the room palette for Indy4 Amiga, since it
                // is used as palette map there too, but we do so slightly a bit
                // further down to group it with the other special palettes needed.
                LoadAndSaveEntry.Create(
                    reader => Gdi.RoomPalette = reader.ReadBytes(256),
                    writer => writer.WriteBytes(Gdi.RoomPalette, 256)
                        , 21),

                // PalManip data was not saved before V10 save games
                LoadAndSaveEntry.Create(reader =>
                    {
                        if (_palManipCounter != 0)
                        {
                            var colors = reader.ReadBytes(0x300);
                            for (int i = 0; i < 0x100; i++)
                            {
                                _palManipPalette.Colors[i] = Color.FromRgb(colors[i * 3], colors[i * 3 + 1], colors[i * 3 + 2]);    
                            }
                            var colors2 = reader.ReadUInt16s(0x300);
                            for (int i = 0; i < 0x100; i++)
                            {
                                _palManipIntermediatePal.Colors[i] = Color.FromRgb(colors2[i * 3], colors2[i * 3 + 1], colors2[i * 3 + 2]);    
                            }
                        }
                    },
                    writer =>
                    {
                        if (_palManipCounter != 0)
                        {
                            for (int i = 0; i < 0x100; i++)
                            {
                                writer.WriteByte(_palManipPalette.Colors[i].R);
                                writer.WriteByte(_palManipPalette.Colors[i].G);
                                writer.WriteByte(_palManipPalette.Colors[i].B);
                            }
                            for (int i = 0; i < 0x100; i++)
                            {
                                writer.WriteUInt16(_palManipIntermediatePal.Colors[i].R);
                                writer.WriteUInt16(_palManipIntermediatePal.Colors[i].G);
                                writer.WriteUInt16(_palManipIntermediatePal.Colors[i].B);
                            }
                        }
                    }, 10),


                // darkenPalette was not saved before V53
                LoadAndSaveEntry.Create(reader =>
                    {
                        // TODO?
                        //Array.Copy(currentPalette, darkenPalette, 768);
                    }, 0, 53),
                
            };
            paletteEntries.ForEach(entry => entry.Execute(serializer));

            // _colorUsedByCycle was not saved before V60
            if (serializer.IsLoading)
            {
                if (serializer.Version < 60)
                {
                    //Array.Clear(_colorUsedByCycle, 0, _colorUsedByCycle.Length);
                }
            }

            // Indy4 Amiga specific palette tables were not saved before V85
            //if (_game.platform == Common::kPlatformAmiga && _game.id == GID_INDY4) {
            //    if (s->getVersion() >= 85) {
            //        s->saveLoadArrayOf(_roomPalette, 256, 1, sleByte);
            //        s->saveLoadArrayOf(_verbPalette, 256, 1, sleByte);
            //        s->saveLoadArrayOf(_amigaPalette, 3 * 64, 1, sleByte);

            //        // Starting from version 86 we also save the first used color in
            //        // the palette beyond the verb palette. For old versions we just
            //        // look for it again, which hopefully won't cause any troubles.
            //        if (s->getVersion() >= 86) {
            //            s->saveLoadArrayOf(&_amigaFirstUsedColor, 1, 2, sleUint16);
            //        } else {
            //            amigaPaletteFindFirstUsedColor();
            //        }
            //    } else {
            //        warning("Save with old Indiana Jones 4 Amiga palette handling detected");
            //        // We need to restore the internal state of the Amiga palette for Indy4
            //        // Amiga. This might lead to graphics glitches!
            //        setAmigaPaletteFromPtr(_currentPalette);
            //    }
            //}

            //
            // Save/load more global object state
            //
            var globalObjStatesEntries = new[]
            {
                LoadAndSaveEntry.Create(
                    reader => Array.Copy(reader.ReadUInt32s(_resManager.ClassData.Length), _resManager.ClassData, _resManager.ClassData.Length),
                    writer => writer.WriteUInt32s(_resManager.ClassData, _resManager.ClassData.Length))
            };
            globalObjStatesEntries.ForEach(entry => entry.Execute(serializer));

            //
            // Save/load script variables
            //
            var var120Backup = _variables[120];
            var var98Backup = _variables[98];

            //if (serializer.Version > 37)
            //{
            //    s->saveLoadArrayOf(_roomVars, _numRoomVariables, sizeof(_roomVars[0]), sleInt32);
            //}

            // The variables grew from 16 to 32 bit.
            var variablesEntries = new[]
            {
                LoadAndSaveEntry.Create(
                    reader => _variables = reader.ReadInt16s(_variables.Length).ConvertAll(s => (int)s),
                    writer => writer.WriteInt16s(_variables, _variables.Length)
                        , 0, 15),
                LoadAndSaveEntry.Create(
                    reader => _variables = reader.ReadInt32s(_variables.Length),
                    writer => writer.WriteInt32s(_variables, _variables.Length), 15),
                LoadAndSaveEntry.Create(
                    reader => _bitVars = new BitArray(reader.ReadBytes(_bitVars.Length / 8)),
                    writer => writer.Write(_bitVars.ToByteArray())
                ),
            };
            variablesEntries.ForEach(entry => entry.Execute(serializer));

            if (_game.GameId == GameId.Tentacle) // Maybe misplaced, but that's the main idea
            {
                _variables[120] = var120Backup;
            }
            if (_game.GameId == GameId.Indy4)
            {
                _variables[98] = var98Backup;
            }

            //
            // Save/load a list of the locked objects
            //
            var lockedObjEntries = new[]
            {
                LoadAndSaveEntry.Create(reader =>
                    {
                        ResType tmp;
                        while ((tmp = (ResType)reader.ReadByte()) != (ResType)0xFF)
                        {
                            var index = reader.ReadUInt16();
                            if (tmp == ResType.FlObject)
                            {
                                _objs[index].IsLocked = true;
                            }
                        }
                    },
                    writer =>
                    {
                        for (int i = 0; i < _objs.Length; i++)
                        {
                            if (_objs[i].IsLocked)
                            {
                                writer.WriteByte((byte)ResType.FlObject);
                                writer.WriteUInt16(i);
                            }
                        }
                        writer.Write((byte)0xFF);
                    }
                )
            };
            lockedObjEntries.ForEach(entry => entry.Execute(serializer));

            //
            // Save/load the Audio CD status
            //
            //if (serializer.Version >= 24)
            //{
            //    AudioCDManager::Status info;
            //    if (s->isSaving())
            //        info = _system->getAudioCDManager()->getStatus();
            //    s->saveLoadArrayOf(&info, 1, sizeof(info), audioCDEntries);
            //     If we are loading, and the music being loaded was supposed to loop
            //     forever, then resume playing it. This helps a lot when the audio CD
            //     is used to provide ambient music (see bug #788195).
            //    if (s->isLoading() && info.playing && info.numLoops < 0)
            //      _system->getAudioCDManager()->play(info.track, info.numLoops, info.start, info.duration);
            //}

            //
            // Save/load the iMuse status
            //
            if (IMuse != null && (_saveSound || !_saveTemporaryState))
            {
                IMuse.SaveOrLoad(serializer);
            }

            //
            // Save/load music engine status
            //
            if (MusicEngine != null)
            {
                MusicEngine.SaveOrLoad(serializer);
            }

            //
            // Save/load the charset renderer state
            //
            //if (s->getVersion() >= VER(73))
            //{
            //    _charset->saveLoadWithSerializer(s);
            //}
            //else if (s->isLoading())
            //{
            //    if (s->getVersion() == VER(72))
            //    {
            //        _charset->setCurID(s->loadByte());
            //    }
            //    else
            //    {
            //        // Before V72, the charset id wasn't saved. This used to cause issues such
            //        // as the one described in the bug report #1722153. For these savegames,
            //        // we reinitialize the id using a, hopefully, sane value.
            //        _charset->setCurID(_string[0]._default.charset);
            //    }
            //}
        }

        void SaveOrLoadResources(Serializer serializer)
        {
            var entry = LoadAndSaveEntry.Create(
                            reader =>
                {
                    ResType type;
                    ushort idx;
                    while ((type = (ResType)reader.ReadUInt16()) != (ResType)0xFFFF)
                    {
                        while ((idx = reader.ReadUInt16()) != 0xFFFF)
                        {
                            LoadResource(reader, type, idx);
                        }
                    }
                },
                            writer =>
                {
                    // inventory
                    writer.WriteUInt16((ushort)ResType.Inventory);
                    for (int i = 0; i < _invData.Length; i++)
                    {
                        var data = _invData[i];
                        if (data == null)
                            break;
                        // write index
                        writer.WriteUInt16(i);
                        var verbTableLength = Game.Version == 8 ? 8 * data.ScriptOffsets.Count + 4 : 3 * data.ScriptOffsets.Count + 1;
                        var nameOffset = 18 + 1 + verbTableLength;
                        // write size
                        writer.WriteInt32(nameOffset + data.Name.Length + 1 + data.Script.Data.Length);
                        // write image offset
                        writer.WriteBytes(new byte[18], 18);
                        // write name offset
                        writer.WriteByte(nameOffset);
                        // write verb table
                        if (Game.Version == 8)
                        {
                            foreach (var scriptOffset in data.ScriptOffsets)
                            {
                                writer.WriteInt32(scriptOffset.Key);
                                writer.WriteInt32(scriptOffset.Value);
                            }
                            // write end of table
                            writer.WriteUInt32(0);
                        }
                        else
                        {
                            foreach (var scriptOffset in data.ScriptOffsets)
                            {
                                writer.WriteByte(scriptOffset.Key);
                                writer.WriteUInt16(scriptOffset.Value);
                            }
                            // write end of table
                            writer.WriteByte(0);
                        }
                        var name = EncodeName(data.Name);
                        // write name
                        for (int c = 0; c < name.Length; c++)
                        {
                            writer.WriteByte(name[c]);
                        }
                        writer.WriteByte(0);
                        // write script
                        writer.Write(data.Script.Data);
                        // write index
                        writer.WriteUInt16(_inventory[i]);
                    }
                    writer.WriteUInt16(0xFFFF);

                    // actors name
                    writer.WriteUInt16((ushort)ResType.ActorName);
                    for (int i = 0; i < Actors.Length; i++)
                    {
                        var actor = Actors[i];
                        if (actor.Name != null)
                        {
                            // write index
                            writer.WriteUInt16(i);
                            // write name
                            writer.WriteInt32(actor.Name.Length);
                            writer.WriteBytes(actor.Name, actor.Name.Length);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // objects name
                    writer.WriteUInt16((ushort)ResType.ObjectName);
                    var objs = _invData.Where(obj => obj != null && obj.Number != 0).ToArray();
                    for (var i = 0; i < objs.Length; i++)
                    {
                        var obj = objs[i];
                        var name = GetObjectOrActorName(obj.Number);
                        if (name.Length > 0)
                        {
                            // write index
                            writer.WriteUInt16(i);
                            // write name
                            writer.WriteInt32(name.Length);
                            writer.WriteBytes(name, name.Length);
                            // writer obj number
                            writer.WriteUInt16(obj.Number);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // matrix
                    writer.WriteUInt16((ushort)ResType.Matrix);
                    // write BoxMatrix
                    writer.WriteUInt16(1);
                    writer.WriteInt32(_boxMatrix.Count);
                    writer.WriteBytes(_boxMatrix.ToArray(), _boxMatrix.Count);

                    // write boxes
                    writer.WriteUInt16(2);
                    writer.WriteInt32((Game.Version == 8 ? 21 : 20) * _boxes.Length + 1);
                    writer.WriteByte(_boxes.Length);
                    for (int i = 0; i < _boxes.Length; i++)
                    {
                        Box box = _boxes[i];
                        writer.WriteInt16(box.Ulx);
                        writer.WriteInt16(box.Uly);
                        writer.WriteInt16(box.Urx);
                        writer.WriteInt16(box.Ury);
                        writer.WriteInt16(box.Lrx);
                        writer.WriteInt16(box.Lry);
                        writer.WriteInt16(box.Llx);
                        writer.WriteInt16(box.Lly);
                        writer.WriteByte(box.Mask);
                        writer.WriteByte((byte)box.Flags);
                        writer.WriteUInt16(box.Scale);
                        if (Game.Version == 8)
                        {
                            writer.WriteByte(box.ScaleSlot);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // verbs
                    writer.WriteUInt16((ushort)ResType.Verb);
                    for (int i = 0; i < Verbs.Length; i++)
                    {
                        var verb = Verbs[i];
                        if (verb.Text != null)
                        {
                            // write index
                            writer.WriteUInt16(i);
                            // write text
                            writer.WriteInt32(verb.Text.Length);
                            writer.WriteBytes(verb.Text, verb.Text.Length);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // verb images
                    writer.WriteUInt16((ushort)ResType.VerbImage);
                    for (int i = 0; i < Verbs.Length; i++)
                    {
                        var verb = Verbs[i];
                        if (verb.ImageData != null)
                        {
                            // write index
                            writer.WriteUInt16(i);
                            // write size
                            writer.WriteInt32(4 + verb.ImageData.Data.Length);
                            // wrie width and height
                            writer.WriteUInt16(verb.ImageWidth);
                            writer.WriteUInt16(verb.ImageHeight);
                            // write
                            writer.WriteBytes(verb.ImageData.Data, verb.ImageData.Data.Length);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // strings
                    writer.WriteUInt16((ushort)ResType.String);
                    for (int i = 0; i < _strings.Length; i++)
                    {
                        var str = _strings[i];
                        if (str != null)
                        {
                            // write index
                            writer.WriteUInt16(i);
                            // write text
                            writer.WriteInt32(str.Length);
                            writer.WriteBytes(str, str.Length);
                        }
                    }
                    writer.WriteUInt16(0xFFFF);

                    // write end of resources
                    writer.WriteUInt16(0xFFFF);

                });
            entry.Execute(serializer);
        }

        static byte[] EncodeName(byte[] name)
        {
            var encodedName = new List<byte>();
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == 255 && name[i + 1] == 4)
                {
                    encodedName.AddRange(new byte[] { 35, 35, 35, 35 });
                    i += 3;
                }
                else
                {
                    encodedName.Add(name[i]);
                }
            }
            return encodedName.ToArray();
        }

        void LoadResource(BinaryReader reader, ResType type, ushort idx)
        {
            bool dynamic = false;
            switch (type)
            {
                case ResType.Inventory:
                case ResType.String:
                case ResType.Verb:
                case ResType.VerbImage:
                case ResType.ActorName:
                case ResType.ScaleTable:
                case ResType.Temp:
                case ResType.FlObject:
                case ResType.Matrix:
                case ResType.ObjectName:
                    dynamic = true;
                    break;
            }

            if (dynamic)
            {
                int size = reader.ReadInt32();
                var ptr = reader.ReadBytes(size);

                //Console.WriteLine("Type: {0}, Index: {1}, Data: {2}", type, idx, size);

                switch (type)
                {
                    case ResType.Inventory:
                        {
                            var index = reader.ReadUInt16();
                            _inventory[idx] = index;
                            _invData[idx] = new ObjectData(index);
                            var br = new BinaryReader(new MemoryStream(ptr));
                            br.BaseStream.Seek(18, SeekOrigin.Begin);
                            var offset = br.ReadByte();
                            br.BaseStream.Seek(offset, SeekOrigin.Begin);
                            // read name
                            var name = new List<byte>();
                            var c = br.ReadByte();
                            while (c != 0)
                            {
                                name.Add(c);
                                c = br.ReadByte();
                            }
                            _invData[idx].Name = name.ToArray();
                            // read script
                            _invData[idx].Script.Data = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
                            // read verb table
                            br.BaseStream.Seek(19, SeekOrigin.Begin);
                            if (Game.Version == 8)
                            {
                                while (true)
                                {
                                    var id = br.ReadInt32();
                                    var off = br.ReadInt32();
                                    if (id == 0)
                                        break;
                                    _invData[idx].ScriptOffsets.Add(id, off);
                                }
                                _invData[idx].Script.Offset = _invData[idx].ScriptOffsets.Count * 8 + 4;
                            }
                            else
                            {
                                while (true)
                                {
                                    var id = br.ReadByte();
                                    var off = br.ReadUInt16();
                                    if (id == 0)
                                        break;
                                    _invData[idx].ScriptOffsets.Add(id, off);
                                }
                                _invData[idx].Script.Offset = _invData[idx].ScriptOffsets.Count * 3 + 1 + 8;
                            }
                        }
                        break;

                    case ResType.ActorName:
                        {
                            Actors[idx].Name = ptr;
                        }
                        break;

                    case ResType.ObjectName:
                        {
                            var index = reader.ReadUInt16();
                            _newNames[index] = ptr;
                        }
                        break;

                    case ResType.Matrix:
                        {
                            if (idx == 1)
                            {
                                // BOXM
                                _boxMatrix.Clear();
                                _boxMatrix.AddRange(ptr);
                            }
                            else if (idx == 2)
                            {
                                // BOXD
                                var br = new BinaryReader(new MemoryStream(ptr));

                                var numBoxes = br.ReadByte();
                                _boxes = new Box[numBoxes];
                                for (int i = 0; i < numBoxes; i++)
                                {
                                    var box = new Box();
                                    box.Ulx = br.ReadInt16();
                                    box.Uly = br.ReadInt16();
                                    box.Urx = br.ReadInt16();
                                    box.Ury = br.ReadInt16();
                                    box.Lrx = br.ReadInt16();
                                    box.Lry = br.ReadInt16();
                                    box.Llx = br.ReadInt16();
                                    box.Lly = br.ReadInt16();
                                    box.Mask = br.ReadByte();
                                    box.Flags = (BoxFlags)br.ReadByte();
                                    box.Scale = br.ReadUInt16();
                                    if (Game.Version == 8)
                                    {
                                        box.ScaleSlot = br.ReadByte();
                                    }
                                    _boxes[i] = box;
                                }
                            }
                        }
                        break;

                    case ResType.Verb:
                        {
                            Verbs[idx].Text = ptr;
                        }
                        break;

                    case ResType.VerbImage:
                        {
                            var br = new BinaryReader(new MemoryStream(ptr));
                            Verbs[idx].ImageWidth = br.ReadUInt16();
                            Verbs[idx].ImageHeight = br.ReadUInt16();
                            var imgSize = (int)(br.BaseStream.Length - 4);
                            Verbs[idx].ImageData = new ImageData{ Data = br.ReadBytes(imgSize) };
                        }
                        break;

                    case ResType.String:
                        {
                            _strings[idx] = ptr;
                        }
                        break;
                }
            }
//            else
//            {
//                Console.WriteLine("Type: {0}", type);
//            }
        }

        public void Save(string filename)
        {
            _saveLoadFlag = 1;
            _savegame = filename;
            _saveTemporaryState = false;
        }

        static bool SkipThumbnail(BinaryReader reader)
        {
            var position = reader.BaseStream.Position;
            var header = LoadHeader(reader);

            if (header == null)
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                return false;
            }

            reader.BaseStream.Seek(header.Size - (reader.BaseStream.Position - position), SeekOrigin.Current);
            return true;
        }

        static bool CheckThumbnailHeader(BinaryReader reader)
        {
            var position = reader.BaseStream.Position;
            var header = LoadHeader(reader);

            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return header != null;
        }

        static void SaveHeader(string name, BinaryWriter bw)
        {
            var hdr = new SaveGameHeader();
            hdr.Type = ScummHelper.MakeTag('S', 'C', 'V', 'M');
            hdr.Size = 0;
            hdr.Version = SaveCurrentVersion;

            bw.WriteUInt32BigEndian(hdr.Type);
            bw.Write(hdr.Size);
            bw.Write(hdr.Version);

            var data = Encoding.UTF8.GetBytes(name);
            var data2 = new byte[32];
            int length = Math.Min(data.Length, 31);
            Array.Copy(data, data2, Math.Min(data.Length, 31));
            data2[length] = 0;
            bw.Write(data2);
        }

        static SaveStateMetaInfos LoadInfos(BinaryReader reader)
        {
            var stuff = new SaveStateMetaInfos();
            var section = new SaveInfoSection();
            section.Type = ScummHelper.SwapBytes(reader.ReadUInt32());
            if (section.Type != ScummHelper.MakeTag('I', 'N', 'F', 'O'))
            {
                return null;
            }

            section.Version = ScummHelper.SwapBytes(reader.ReadUInt32());
            section.Size = ScummHelper.SwapBytes(reader.ReadUInt32());

            // If we ever extend this we should add a table containing the sizes corresponding to each
            // version, so that we are able to properly verify their correctness.
            if (section.Version == InfoSectionVersion && section.Size != SaveInfoSectionSize)
            {
                //warning("Info section is corrupt");
                reader.BaseStream.Seek(section.Size, SeekOrigin.Current);
                return null;
            }

            section.TimeTValue = ScummHelper.SwapBytes(reader.ReadUInt32());
            section.PlayTime = ScummHelper.SwapBytes(reader.ReadUInt32());

            // For header version 1, we load the data in with our old method
            if (section.Version == 1)
            {
                //time_t tmp = section.timeTValue;
                //tm *curTime = localtime(&tmp);
                //stuff->date = (curTime->tm_mday & 0xFF) << 24 | ((curTime->tm_mon + 1) & 0xFF) << 16 | (curTime->tm_year + 1900) & 0xFFFF;
                //stuff->time = (curTime->tm_hour & 0xFF) << 8 | (curTime->tm_min) & 0xFF;
                stuff.Date = 0;
                stuff.Time = 0;
            }

            if (section.Version >= 2)
            {
                section.Date = ScummHelper.SwapBytes(reader.ReadUInt32());
                section.Time = ScummHelper.SwapBytes(reader.ReadUInt16());

                stuff.Date = section.Date;
                stuff.Time = section.Time;
            }

            stuff.PlayTime = section.PlayTime;

            // Skip over the remaining (unsupported) data
            if (section.Size > SaveInfoSectionSize)
                reader.BaseStream.Seek(section.Size - SaveInfoSectionSize, SeekOrigin.Current);

            return stuff;
        }

        static void SaveInfos(BinaryWriter writer)
        {
            var section = new SaveInfoSection();
            section.Type = ScummHelper.MakeTag('I', 'N', 'F', 'O');
            section.Version = InfoSectionVersion;
            section.Size = SaveInfoSectionSize;

            // TODO: still save old format for older versions
            section.TimeTValue = 0;
            section.PlayTime = 0;

            //TimeDate curTime;
            //_system->getTimeAndDate(curTime);

            //section.date = ((curTime.tm_mday & 0xFF) << 24) | (((curTime.tm_mon + 1) & 0xFF) << 16) | ((curTime.tm_year + 1900) & 0xFFFF);
            //section.time = ((curTime.tm_hour & 0xFF) << 8) | ((curTime.tm_min) & 0xFF);

            writer.WriteUInt32BigEndian(section.Type);
            writer.WriteUInt32BigEndian(section.Version);
            writer.WriteUInt32BigEndian(section.Size);
            writer.WriteUInt32BigEndian(section.TimeTValue);
            writer.WriteUInt32BigEndian(section.PlayTime);
            writer.WriteUInt32BigEndian(section.Date);
            writer.WriteUInt16(section.Time);
        }

        static SaveGameHeader LoadSaveGameHeader(BinaryReader reader)
        {
            var hdr = new SaveGameHeader();
            hdr.Type = ScummHelper.SwapBytes(reader.ReadUInt32());
            if (hdr.Type != ScummHelper.MakeTag('S', 'C', 'V', 'M'))
                throw new NotSupportedException("Invalid savegame");
            hdr.Size = reader.ReadUInt32();
            hdr.Version = reader.ReadUInt32();
            // In older versions of ScummVM, the header version was not endian safe.
            // We account for that by retrying once with swapped byte order in case
            // we see a version that is higher than anything we'd expect...
            if (hdr.Version > 0xFFFFFF)
                hdr.Version = ScummHelper.SwapBytes(hdr.Version);

            // Reject save games which are too old or too new. Note that
            // We do not really support V7 games, but still accept them here
            // to work around a bug from the stone age (see below for more
            // information).
            if (hdr.Version < 7 || hdr.Version > CurrentVersion)
            {
                throw new NotSupportedException("Invalid version");
            }

			hdr.Name = reader.ReadBytes(32).GetText();

            // Since version 52 a thumbnail is saved directly after the header.
            if (hdr.Version >= 52)
            {
                // Prior to version 75 we always required an thumbnail to be present
                if (hdr.Version <= 74)
                {
                    if (!CheckThumbnailHeader(reader))
                    {
                        throw new NotSupportedException("Cannot load thumbnail");
                    }
                }
                SkipThumbnail(reader);
            }

            return hdr;
        }

        static ThumbnailHeader LoadHeader(BinaryReader reader)
        {
            var header = new ThumbnailHeader();
            header.Type = ScummHelper.SwapBytes(reader.ReadUInt32());
            // We also accept the bad 'BMHT' header here, for the sake of compatibility
            // with some older savegames which were written incorrectly due to a bug in
            // ScummVM which wrote the thumb header type incorrectly on LE systems.
            if (header.Type != ScummHelper.MakeTag('T', 'H', 'M', 'B') && header.Type != ScummHelper.MakeTag('B', 'M', 'H', 'T'))
            {
                //if (outputWarnings)
                //    warning("couldn't find thumbnail header type");
                return null;
            }

            header.Size = ScummHelper.SwapBytes(reader.ReadUInt32());
            header.Version = reader.ReadByte();

            if (header.Version > TumbnailVersion)
            {
                //if (outputWarnings)
                //    warning("trying to load a newer thumbnail version: %d instead of %d", header.version, THMB_VERSION);
                return null;
            }

            header.Width = ScummHelper.SwapBytes(reader.ReadUInt16());
            header.Height = ScummHelper.SwapBytes(reader.ReadUInt16());
            header.Bpp = reader.ReadByte();

            return header;
        }

        protected bool SavePreparedSavegame(int slot, string desc)
        {
            var filename = MakeSavegameName(slot, false);
            var directory = ServiceLocator.FileStorage.GetDirectoryName(Game.Path);
            SaveState(ServiceLocator.FileStorage.Combine(directory, filename), desc);
            return true;
        }

        protected bool[] ListSavegames(int num)
        {
            var marks = new bool[num];
            var prefix = new StringBuilder(MakeSavegameName(99, false));
            prefix[prefix.Length - 2] = '*';
            prefix.Remove(prefix.Length - 1, 1);
            var directory = ServiceLocator.FileStorage.GetDirectoryName(Game.Path);
            var files = ServiceLocator.FileStorage.EnumerateFiles(directory, prefix.ToString());
            foreach (var file in files)
            {
                var ext = ServiceLocator.FileStorage.GetExtension(file).Remove(0, 2);
                var slotNum = int.Parse(ext);
                if (slotNum >= 0 && slotNum < num)
                {
                    marks[slotNum] = true;
                }
            }
            return marks;
        }

        protected string MakeSavegameName(int slot, bool temporary)
        {
            return string.Format("{0}.{1}{2:D2}", Game.Id, temporary ? 'c' : 's', slot);
        }

        protected bool GetSavegameName(int slot, out string desc)
        {
            bool result;
            var filename = MakeSavegameName(slot, false);
            var directory = ServiceLocator.FileStorage.GetDirectoryName(Game.Path);
            using (var file = ServiceLocator.FileStorage.OpenFileRead(ServiceLocator.FileStorage.Combine(directory, filename)))
            {
                result = GetSavegameName(file, out desc);
            }
            return result;
        }

        static bool GetSavegameName(Stream stream, out string desc)
        {
            SaveGameHeader hdr;
        
            var br = new BinaryReader(stream);
            if ((hdr = LoadSaveGameHeader(br)) == null)
            {
                desc = "Invalid savegame";
                return false;
            }
        
            if (hdr.Version < 7 || hdr.Version > CurrentVersion)
            {
                desc = "Invalid version";
                return false;
            }
        
            // We (deliberately) broke HE savegame compatibility at some point.
            if (hdr.Version < 57 /* && heversion >= 60*/)
            {
                desc = "Unsupported version";
                return false;
            }
        
            desc = hdr.Name;
            return true;
        }
    }
}

