//
//  CutScene.cs
//
//  Author:
//       Valéry Sablonnière <scemino74@gmail.com>
//
//  Copyright (c) 2013 
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

using NScumm.Core;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    class CutSceneData
    {
        public int Pointer { get; set; }

        public byte Script { get; set; }

        public int Data { get; set; }
    }

    class CutScene
    {
        const int MaxCutsceneNum = 5;

        public CutSceneData[] Data { get; private set; }

        public int ScriptIndex { get; set; }

        public byte StackPointer { get; set; }

        public CutScene()
        {
            Data = new CutSceneData[MaxCutsceneNum];
            ResetData();
        }

        void ResetData()
        {
            for (int i = 0; i < MaxCutsceneNum; i++)
            {
                Data[i] = new CutSceneData();
            }
        }

        public void SaveOrLoad(Serializer serializer)
        {
            var entries = new LoadAndSaveEntry[]
            {
                LoadAndSaveEntry.Create(reader =>
                    {
                        StackPointer = reader.ReadByte();
                        var cutScenePtr = reader.ReadInt32s(MaxCutsceneNum);
                        var cutSceneScript = reader.ReadBytes(MaxCutsceneNum);
                        var cutSceneData = reader.ReadInt16s(MaxCutsceneNum);

                        // load Cut Scene Data
                        for (var i = 0; i < MaxCutsceneNum; i++)
                        {
                            var data = new CutSceneData
                            {
                                Pointer = cutScenePtr[i],
                                Script = cutSceneScript[i],
                                Data = cutSceneData[i]
                            };
                            Data[i] = data;
                        }
                        ScriptIndex = reader.ReadInt16();
                    }, writer =>
                    {
                        var cutScenePtr = new int[MaxCutsceneNum];
                        var cutSceneScript = new byte[MaxCutsceneNum];
                        var cutSceneData = new short[MaxCutsceneNum];
                        var cutSceneStack = Data;
                        for (var i = 0; i < cutSceneStack.Length; i++)
                        {
                            cutScenePtr[i] = cutSceneStack[i].Pointer;   
                            cutSceneScript[i] = cutSceneStack[i].Script;   
                            cutSceneData[i] = (short)cutSceneStack[i].Data;   
                        }
                        writer.WriteByte(StackPointer);
                        writer.WriteInt32s(cutScenePtr, MaxCutsceneNum);
                        writer.WriteBytes(cutSceneScript, MaxCutsceneNum);
                        writer.WriteInt16s(cutSceneData, MaxCutsceneNum);
                        writer.WriteInt16(ScriptIndex);
                    }, 8)
            };
            entries.ForEach(e => e.Execute(serializer));
        }
    }
}

