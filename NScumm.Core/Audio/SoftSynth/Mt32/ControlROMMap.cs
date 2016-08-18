//
//  ControlROMMap.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    public class ControlROMMap
    {
        public ushort idPos;
        public ushort idLen;
        public string idBytes;
        public ushort pcmTable; // 4 * pcmCount bytes
        public ushort pcmCount;
        public ushort timbreAMap; // 128 bytes
        public ushort timbreAOffset;
        public bool timbreACompressed;
        public ushort timbreBMap; // 128 bytes
        public ushort timbreBOffset;
        public bool timbreBCompressed;
        public ushort timbreRMap; // 2 * timbreRCount bytes
        public ushort timbreRCount;
        public ushort rhythmSettings; // 4 * rhythmSettingsCount bytes
        public ushort rhythmSettingsCount;
        public ushort reserveSettings; // 9 bytes
        public ushort panSettings; // 8 bytes
        public ushort programSettings; // 8 bytes
        public ushort rhythmMaxTable; // 4 bytes
        public ushort patchMaxTable; // 16 bytes
        public ushort systemMaxTable; // 23 bytes
        public ushort timbreMaxTable; // 72 bytes

        public ControlROMMap(ushort idPos, ushort idLen, string idBytes, ushort pcmTable, ushort pcmCount, ushort timbreAMap, ushort timbreAOffset,
                             bool timbreACompressed, ushort timbreBMap, ushort timbreBOffset, bool timbreBCompressed, ushort timbreRMap, ushort timbreRCount,
                             ushort rhythmSettings, ushort rhythmSettingsCount, ushort reserveSettings, ushort panSettings, ushort programSettings,
                             ushort rhythmMaxTable, ushort patchMaxTable, ushort systemMaxTable, ushort timbreMaxTable)
        {
            this.idPos = idPos;
            this.idLen = idLen;
            this.idBytes = idBytes;
            this.pcmTable = pcmTable;
            this.pcmCount = pcmCount;
            this.timbreAMap = timbreAMap;
            this.timbreAOffset = timbreAOffset;
            this.timbreACompressed = timbreACompressed;
            this.timbreBMap = timbreBMap;
            this.timbreBOffset = timbreBOffset;
            this.timbreBCompressed = timbreBCompressed;
            this.timbreRMap = timbreRMap;
            this.timbreRCount = timbreRCount;
            this.rhythmSettings = rhythmSettings;
            this.rhythmSettingsCount = rhythmSettingsCount;
            this.reserveSettings = reserveSettings;
            this.panSettings = panSettings;
            this.programSettings = programSettings;
            this.rhythmMaxTable = rhythmMaxTable;
            this.patchMaxTable = patchMaxTable;
            this.systemMaxTable = systemMaxTable;
            this.timbreMaxTable = timbreMaxTable;
        }
    }
}
