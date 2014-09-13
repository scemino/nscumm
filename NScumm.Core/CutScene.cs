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
using System;
using System.Collections.Generic;
using NScumm.Core.IO;

namespace NScumm.Core
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

        CutSceneData cutSceneOverride = new CutSceneData();

        public Stack<CutSceneData> Data { get; private set; }

        public CutSceneData Override
        {
            get
            {
                if (Data.Count > 0)
                {
                    cutSceneOverride = Data.Peek();
       
                }
                return cutSceneOverride;
            }
        }

        public int CutSceneScriptIndex { get; set; }

        public CutScene()
        {
            Data = new Stack<CutSceneData>(MaxCutsceneNum);
        }

        public void SaveOrLoad(Serializer serializer)
        {
            var entries = new[]
            {
                LoadAndSaveEntry.Create(reader =>
                    {
                        var num = reader.ReadByte();
                        var cutScenePtr = reader.ReadUInt32s(5);
                        var cutSceneScript = reader.ReadBytes(5);
                        var cutSceneData = Array.ConvertAll(reader.ReadInt16s(5), n => (int)n);

                        // load Cut Scene Data
                        Data.Clear();
                        for (int i = 0; i < num; i++)
                        {
                            var data = new CutSceneData
                            {
                                Pointer = (int)cutScenePtr[i],
                                Script = cutSceneScript[i],
                                Data = cutSceneData[i]
                            };
                            Data.Push(data);
                        }
                        CutSceneScriptIndex = reader.ReadInt16();
                    }, writer =>
                    {
                        var cutScenePtr = new uint[5];
                        var cutSceneScript = new byte[5];
                        var cutSceneData = new short[5];
                        var cutSceneStack = Data.ToArray();
                        for (int i = 0; i < cutSceneStack.Length; i++)
                        {
                            cutScenePtr[i] = (uint)cutSceneStack[i].Pointer;   
                            cutSceneScript[i] = cutSceneStack[i].Script;   
                            cutSceneData[i] = (short)cutSceneStack[i].Data;   
                        }
                        writer.WriteByte(Data.Count);
                        writer.WriteUInt32s(cutScenePtr, 5);
                        writer.WriteBytes(cutSceneScript, 5);
                        writer.WriteInt16s(cutSceneData, 5);
                        writer.WriteInt16(CutSceneScriptIndex);
                    }, 8)
            };
            Array.ForEach(entries, e => e.Execute(serializer));
        }
    }
}

