//
//  ROMImage.cs
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

using System.IO;

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    enum RomInfoType { PCM, Control, Reverb }
    enum PairType { Full, FirstHalf, SecondHalf, Mux0, Mux1 }

    // Defines vital info about ROM file to be used by synth and applications
    class ROMInfo
    {
        public int FileSize;
        public string Sha1Digest;
        public RomInfoType Type;
        public string ShortName;
        public string Description;
        public PairType PairType;
        public ROMInfo PairROMInfo;
        public ControlROMFeatureSet ControlROMFeatures;

        ROMInfo(int fileSize, string sha1Digest, RomInfoType type, string shortName
                , string description, PairType pairType, ROMInfo pairROMInfo, ControlROMFeatureSet controlROMFeatures)
        {
            FileSize = fileSize;
            Sha1Digest = sha1Digest;
            Type = type;
            ShortName = shortName;
            Description = description;
            PairType = pairType;
            PairROMInfo = pairROMInfo;
            ControlROMFeatures = controlROMFeatures;
        }

        private static readonly ControlROMFeatureSet MT32_COMPATIBLE = new ControlROMFeatureSet(true, true);
        private static readonly ControlROMFeatureSet CM32L_COMPATIBLE = new ControlROMFeatureSet(false, false);

        // Known ROMs
        static readonly ROMInfo CTRL_MT32_V1_04 = new ROMInfo(65536, "5a5cb5a77d7d55ee69657c2f870416daed52dea7", RomInfoType.Control, "ctrl_mt32_1_04", "MT-32 Control v1.04", PairType.Full, null, MT32_COMPATIBLE);
        static readonly ROMInfo CTRL_MT32_V1_05 = new ROMInfo(65536, "e17a3a6d265bf1fa150312061134293d2b58288c", RomInfoType.Control, "ctrl_mt32_1_05", "MT-32 Control v1.05", PairType.Full, null, MT32_COMPATIBLE);
        static readonly ROMInfo CTRL_MT32_V1_06 = new ROMInfo(65536, "a553481f4e2794c10cfe597fef154eef0d8257de", RomInfoType.Control, "ctrl_mt32_1_06", "MT-32 Control v1.06", PairType.Full, null, MT32_COMPATIBLE);
        static readonly ROMInfo CTRL_MT32_V1_07 = new ROMInfo(65536, "b083518fffb7f66b03c23b7eb4f868e62dc5a987", RomInfoType.Control, "ctrl_mt32_1_07", "MT-32 Control v1.07", PairType.Full, null, MT32_COMPATIBLE);
        static readonly ROMInfo CTRL_MT32_BLUER = new ROMInfo(65536, "7b8c2a5ddb42fd0732e2f22b3340dcf5360edf92", RomInfoType.Control, "ctrl_mt32_bluer", "MT-32 Control BlueRidge", PairType.Full, null, MT32_COMPATIBLE);

        static readonly ROMInfo CTRL_CM32L_V1_00 = new ROMInfo(65536, "73683d585cd6948cc19547942ca0e14a0319456d", RomInfoType.Control, "ctrl_cm32l_1_00", "CM-32L/LAPC-I Control v1.00", PairType.Full, null, CM32L_COMPATIBLE);
        static readonly ROMInfo CTRL_CM32L_V1_02 = new ROMInfo(65536, "a439fbb390da38cada95a7cbb1d6ca199cd66ef8", RomInfoType.Control, "ctrl_cm32l_1_02", "CM-32L/LAPC-I Control v1.02", PairType.Full, null, CM32L_COMPATIBLE);

        static readonly ROMInfo PCM_MT32 = new ROMInfo(524288, "f6b1eebc4b2d200ec6d3d21d51325d5b48c60252", RomInfoType.PCM, "pcm_mt32", "MT-32 PCM ROM", PairType.Full, null, null);
        static readonly ROMInfo PCM_CM32L = new ROMInfo(1048576, "289cc298ad532b702461bfc738009d9ebe8025ea", RomInfoType.PCM, "pcm_cm32l", "CM-32L/CM-64/LAPC-I PCM ROM", PairType.Full, null, null);

        static readonly ROMInfo[] KnownROMInfos = { CTRL_MT32_V1_04, CTRL_MT32_V1_05, CTRL_MT32_V1_06, CTRL_MT32_V1_07, CTRL_MT32_BLUER, CTRL_CM32L_V1_00, CTRL_CM32L_V1_02, PCM_MT32, PCM_CM32L };

        // Returns a ROMInfo struct by inspecting the size and the SHA1 hash
        public static ROMInfo GetROMInfo(Stream stream)
        {
            var fileSize = stream.Length;
            string fileName = ServiceLocator.FileStorage.GetPath(stream);
            fileName = fileName.ToUpperInvariant();
            bool isCM32LROM = fileName.StartsWith("CM32L_");
            // We haven't added the SHA1 checksum code in ScummVM, as the file size
            // and ROM name suffices for our needs for now.
            //const char *fileDigest = file.getSHA1();
            foreach (ROMInfo romInfo in KnownROMInfos)
            {
                if (fileSize == romInfo.FileSize /*&& !strcmp(fileDigest, romInfo.sha1Digest)*/)
                {
                    if (fileSize == 65536)
                    {
                        // If we are looking for a CM-32L ROM, make sure we return the first matching
                        // CM-32L ROM from the list, instead of the first matching MT-32 ROM
                        if (isCM32LROM && romInfo.ControlROMFeatures.IsDefaultReverbMT32Compatible)
                            continue;
                    }
                    return romInfo;
                }
            }
            return null;
        }

        // Currently no-op
        public static void FreeROMInfo(ROMInfo romInfo)
        {
        }

        // Allows retrieving a NULL-terminated list of ROMInfos for a range of types and pairTypes
        // (specified by bitmasks)
        // Useful for GUI/console app to output information on what ROMs it supports
        //static ROMInfo GetROMInfoList(uint types, uint pairTypes);

        // Frees the list of ROMInfos given
        //static void FreeROMInfoList(ROMInfo romInfos);
    }

    class ControlROMFeatureSet
    {
        public ControlROMFeatureSet(bool defaultReverbMT32Compatible, bool oldMT32AnalogLPF)
        {
            IsDefaultReverbMT32Compatible = defaultReverbMT32Compatible;
            IsOldMT32AnalogLPF = oldMT32AnalogLPF;
        }

        public bool IsDefaultReverbMT32Compatible { get; }
        public bool IsOldMT32AnalogLPF { get; }
    }


    class ROMImage
    {
        private Stream file;
        private ROMInfo romInfo;

        public Stream File
        {
            get { return file; }
        }

        public ROMInfo ROMInfo
        {
            get { return romInfo; }
        }

        public static ROMImage MakeROMImage(Stream stream)
        {
            var romImage = new ROMImage();
            romImage.file = stream;
            romImage.romInfo = ROMInfo.GetROMInfo(romImage.file);
            return romImage;
        }

    }
}
