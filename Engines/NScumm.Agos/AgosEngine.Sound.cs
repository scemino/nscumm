//
//  AGOSEngine.Sound.cs
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
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AGOSEngine
    {
        private void vc52_playSound()
        {
            bool ambient = false;

            ushort sound = (ushort) VcReadNextWord();
            if (sound >= 0x8000)
            {
                ambient = true;
                sound = (ushort) -sound;
            }

            if (GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
            {
                short pan = (short) VcReadNextWord();
                short vol = (short) VcReadNextWord();

                if (ambient)
                    LoadSound(sound, pan, vol, SoundTypeFlags.AMBIENT);
                else
                    LoadSound(sound, pan, vol, SoundTypeFlags.SFX);
            }
            else if (GameType == SIMONGameType.GType_SIMON2)
            {
                if (ambient)
                    _sound.PlayAmbient(sound);
                else
                    _sound.PlayEffects(sound);
            }
            else if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                _sound.PlayEffects(sound);
            }
            else if (GameId == GameIds.GID_SIMON1DOS)
            {
                PlaySting(sound);
            }
            else if (GameType == SIMONGameType.GType_WW)
            {
                // TODO: Sound effects in PC version only
            }
            else
            {
                LoadSound(sound, 0, 0);
            }
        }

        protected void PlaySting(ushort soundId)
        {
            // The sound effects in floppy disk version of
            // Simon the Sorcerer 1 are only meant for AdLib
            if (!_midi._adLibMusic || !_midi._enable_sfx)
                return;

            var filename = $"STINGS{_soundFileId}.MUS";
            var musFile = OpenFileRead(filename);
            if (musFile == null)
            {
                Error("playSting: Can't load sound effect from '{0}'", filename);
                return;
            }

            musFile.Seek(soundId * 2, SeekOrigin.Begin);
            var br = new BinaryReader(musFile);
            var musOffset = br.ReadUInt16();

            musFile.Seek(musOffset, SeekOrigin.Begin);
            _midi.LoadSMF(musFile, soundId, true);
            _midi.StartTrack(0);
        }

        private void LoadSound(ushort sound, ushort frq, SoundTypeFlags type)
        {
            throw new System.NotImplementedException();
        }

        private void LoadSound(ushort sound, short pan, short vol, SoundTypeFlags type)
        {
            BytePtr dstPtr;
            if (GameId == GameIds.GID_DIMP)
            {
                System.Diagnostics.Debug.Assert(sound >= 1 && sound <= 32);
                var filename = $"{dimpSoundList[sound - 1]}.wav";

                var @in = OpenFileRead(filename);
                if (@in == null)
                    Error("loadSound: Can't load {0}", filename);

                int dstSize = (int) @in.Length;
                var dst = new byte[dstSize];
                dstPtr = dst;
                if (@in.Read(dst, 0, dstSize) != dstSize)
                    Error("loadSound: Read failed");
            }
            else if (Features.HasFlag(GameFeatures.GF_ZLIBCOMP))
            {
                string filename;

                int file, offset, srcSize, dstSize;
                if (GamePlatform == Platform.Amiga)
                {
                    LoadOffsets("sfxindex.dat", _zoneNumber * 22 + sound, out file, out offset, out srcSize, out dstSize);
                }
                else
                {
                    LoadOffsets("effects.wav", _zoneNumber * 22 + sound, out file, out offset, out srcSize, out dstSize);
                }

                if (GamePlatform == Platform.Amiga)
                    filename = $"sfx{file}.wav";
                else
                    filename = "effects.wav";

                var dst = new byte[dstSize];
                dstPtr = dst;
                DecompressData(filename, dst, offset, srcSize, dstSize);
            }
            else
            {
                if (_curSfxFile == BytePtr.Null)
                    return;

                var dst = _curSfxFile + _curSfxFile.ToInt32(sound * 4);
                dstPtr = dst;
            }

            if (type == SoundTypeFlags.AMBIENT)
                _sound.PlayAmbientData(dstPtr, sound, pan, vol);
            else if (type == SoundTypeFlags.SFX)
                _sound.PlaySfxData(dstPtr, sound, pan, vol);
            else if (type == SoundTypeFlags.SFX5)
                _sound.PlaySfx5Data(dstPtr, sound, pan, vol);
        }

        private static readonly string[] dimpSoundList =
        {
            "Beep",
            "Birth",
            "Boiling",
            "Burp",
            "Cough",
            "Die1",
            "Die2",
            "Fart",
            "Inject",
            "Killchik",
            "Puke",
            "Lights",
            "Shock",
            "Snore",
            "Snotty",
            "Whip",
            "Whistle",
            "Work1",
            "Work2",
            "Yawn",
            "And0w",
            "And0x",
            "And0y",
            "And0z",
            "And10",
            "And11",
            "And12",
            "And13",
            "And14",
            "And15",
            "And16",
            "And17",
        };
    }
}