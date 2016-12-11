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

using System;
using System.Collections.Generic;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private SoundHandle _modHandle;

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

        private void LoadSound(ushort sound, ushort freq, SoundTypeFlags flags)
        {
            int offs, size = 0;
            int rate = 8000;

            if (_curSfxFile == BytePtr.Null)
                return;

            var dst = _curSfxFile;
            if (GameType == SIMONGameType.GType_WW)
            {
                ushort tmp = sound;

                while (tmp-- != 0)
                {
                    size += dst.ToUInt16() + 4;
                    dst += dst.ToUInt16() + 4;

                    if (size > _curSfxFileSize)
                        Error("loadSound: Reading beyond EOF ({0}, {1})", size, _curSfxFileSize);
                }

                size = dst.ToUInt16();
                offs = 4;
            }
            else if (GameType == SIMONGameType.GType_ELVIRA2)
            {
                while (dst.ToInt32BigEndian(4) != sound)
                {
                    size += 12;
                    dst += 12;

                    if (size > _curSfxFileSize)
                        Error("loadSound: Reading beyond EOF ({0}, {1})", size, _curSfxFileSize);
                }

                size = dst.ToInt32BigEndian();
                offs = dst.ToInt32BigEndian(8);
            }
            else
            {
                while (dst.ToUInt16(6) != sound)
                {
                    size += 12;
                    dst += 12;

                    if (size > _curSfxFileSize)
                        Error("loadSound: Reading beyond EOF ({0}, {1})", size, _curSfxFileSize);
                }

                size = dst.ToUInt16(2);
                offs = dst.ToInt32BigEndian(8);
            }

            if (GameType == SIMONGameType.GType_PN)
            {
                if (freq == 0)
                {
                    rate = 4600;
                }
                else if (freq == 1)
                {
                    rate = 7400;
                }
                else
                {
                    rate = 9400;
                }
            }

            // TODO: Handle other sound flags in Amiga/AtariST versions
            if (flags == SoundTypeFlags.SFX && _sound.IsSfxActive)
            {
                _sound.QueueSound(dst + offs, sound, size, (ushort) rate);
            }
            else
            {
                if (flags == 0)
                    _sound.StopSfx();
                _sound.PlayRawData(dst + offs, sound, size, rate);
            }
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
                _sound.PlaySfxData(dstPtr, sound, pan, (uint) vol);
            else if (type == SoundTypeFlags.SFX5)
                _sound.PlaySfx5Data(dstPtr, sound, pan, vol);
        }

        protected virtual void PlayMusic(ushort music, ushort track)
        {
            StopMusic();

            if (GamePlatform == Platform.Amiga)
            {
                PlayModule(music);
            }
            else if (GamePlatform == Platform.AtariST)
            {
                // TODO: Add support for music formats used
            }
            else
            {
                _midi.SetLoop(true); // Must do this BEFORE loading music.

                var filename = $"MOD{music}.MUS";
                var f = OpenFileRead(filename);
                if (f == null)
                    Error("playMusic: Can't load music from '%s'", filename);

                _midi.LoadS1D(f);
                _midi.StartTrack(0);
                _midi.StartTrack(track);
            }
        }

        protected void PlayModule(ushort music)
        {
            int offs = 0;

            if (GamePlatform == Platform.Amiga && GameType == SIMONGameType.GType_WW)
            {
                // Multiple tunes are stored in music files for main locations
                for (var i = 0; i < 20; i++)
                {
                    if (amigaWaxworksOffs[i].tune == music)
                    {
                        music = amigaWaxworksOffs[i].fileNum;
                        offs = amigaWaxworksOffs[i].offs;
                    }
                }
            }

            string filename;
            if (GameType == SIMONGameType.GType_ELVIRA1 && Features.HasFlag(GameFeatures.GF_DEMO))
                filename = "elvira2";
            else if (GamePlatform == Platform.Acorn)
                filename = $"{music}tune.DAT";
            else
                filename = $"{music}tune";

            var f = OpenFileRead(filename);
            if (f == null)
            {
                Error("playModule: Can't load module from '{0}'", filename);
            }

            IAudioStream audioStream;
            if (!(GameType == SIMONGameType.GType_ELVIRA1 && Features.HasFlag(GameFeatures.GF_DEMO)) &&
                Features.HasFlag(GameFeatures.GF_CRUNCHED))
            {
                int srcSize = (int) f.Length;
                var srcBuf = new byte[srcSize];
                if (f.Read(srcBuf, 0, srcSize) != srcSize)
                    Error("playModule: Read failed");

                int dstSize = srcBuf.ToInt32BigEndian(srcSize - 4);
                var dstBuf = new byte[dstSize];
                var b = DecrunchFile(srcBuf, dstBuf, srcSize);

                var stream = new MemoryStream(dstBuf, 0, dstSize);
                audioStream = new ProtrackerStream(stream, offs);
            }
            else
            {
                audioStream = new ProtrackerStream(f);
            }

            _modHandle = Mixer.PlayStream(SoundType.Music, audioStream);
            Mixer.PauseHandle(_modHandle, _musicPaused);
        }

        protected void StopMusic()
        {
            if (_midiEnabled)
            {
                _midi.Stop();
            }
            Mixer.StopHandle(_modHandle);
        }

        private bool LoadVGASoundFile(ushort id, byte type)
        {
            string filename;
            BytePtr dst;
            int srcSize, dstSize;

            if (_gd.Platform == Platform.Amiga || _gd.Platform == Platform.AtariST)
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                    _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO) &&
                    _gd.Platform == Platform.Amiga)
                {
                    filename = $"{(char) 48 + id}{type}.out";
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                         _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                {
                    filename = $"{id:D2}{type}.out";
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                {
                    filename = "{(char)id + 48}{type}.in";
                }
                else
                {
                    filename = $"{id:D3}{type}.out";
                }
            }
            else
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                {
                    if (elvira1_soundTable[id] == 0)
                        return false;

                    filename = $"{elvira1_soundTable[id]:D2}.SND";
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                         _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                {
                    filename = $"{id:D2}{type}.VGA";
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                {
                    filename = $"{(char) 48 + id}{type}.out";
                }
                else
                {
                    filename = $"{id:D3}{type}.VGA";
                }
            }

            var @in = OpenFileRead(filename);
            if (@in == null || @in.Length == 0)
            {
                return false;
            }

            dstSize = srcSize = (int) @in.Length;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_CRUNCHED))
            {
                var data = new Stack<uint>();
                BytePtr dataOut;
                int dataOutSize = 0;

                var br = new BinaryReader(@in);
                for (var i = 0; i < srcSize / 4; ++i)
                    data.Push(br.ReadUInt32BigEndian());

                DecompressPN(data, out dataOut, ref dataOutSize);
                dst = AllocBlock(dataOutSize);
                Array.Copy(dataOut.Data, dataOut.Offset, dst.Data, dst.Offset, dataOutSize);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                     _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
            {
                var srcBuffer = new byte[srcSize];
                if (@in.Read(srcBuffer, 0, srcSize) != srcSize)
                    Error("loadVGASoundFile: Read failed");

                dstSize = srcBuffer.ToInt32BigEndian(srcSize - 4);
                dst = AllocBlock(dstSize);
                DecrunchFile(srcBuffer, dst, srcSize);
            }
            else
            {
                dst = AllocBlock(dstSize);
                if (@in.Read(dst.Data, dst.Offset, dstSize) != dstSize)
                    Error("loadVGASoundFile: Read failed");
            }
            @in.Dispose();

            return true;
        }

        private void LoadMusic(ushort music)
        {
            byte[] buf = new byte[4];

            StopMusic();

            _gameFile.Seek(_gameOffsetsPtr[_musicIndexBase + music - 1], SeekOrigin.Begin);
            _gameFile.Read(buf, 0, 4);
            if (buf.GetRawText() == "FORM")
            {
                _gameFile.Seek(_gameOffsetsPtr[_musicIndexBase + music - 1], SeekOrigin.Begin);
                _midi.LoadXMIDI(_gameFile);
            }
            else
            {
                _gameFile.Seek(_gameOffsetsPtr[_musicIndexBase + music - 1], SeekOrigin.Begin);
                _midi.LoadMultipleSMF(_gameFile);
            }

            _lastMusicPlayed = (short) music;
            _nextMusicToPlay = -1;
        }

        protected void LoadVoice(uint speechId)
        {
            if (GameType == SIMONGameType.GType_PP && speechId == 99)
            {
                _sound.StopVoice();
                return;
            }

            if (Features.HasFlag(GameFeatures.GF_ZLIBCOMP))
            {
                string filename;

                int file, offset, srcSize, dstSize;
                if (GamePlatform == Platform.Amiga)
                {
                    LoadOffsets("spindex.dat", (int) speechId, out file, out offset, out srcSize, out dstSize);
                }
                else
                {
                    LoadOffsets("speech.wav", (int) speechId, out file, out offset, out srcSize, out dstSize);
                }

                // Voice segment doesn't exist
                if (offset == 0xFFFFFFFF && srcSize == 0xFFFFFFFF && dstSize == 0xFFFFFFFF)
                {
                    Debug(0, "loadVoice: speechId %d removed", speechId);
                    return;
                }

                if (GamePlatform == Platform.Amiga)
                    filename = $"sp{file}.wav";
                else
                    filename = "speech.wav";

                var dst = new byte[dstSize];
                DecompressData(filename, dst, offset, srcSize, dstSize);
                _sound.PlayVoiceData(dst, speechId);
            }
            else
            {
                _sound.PlayVoice(speechId);
            }
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

        struct ModuleOffs
        {
            public byte tune;
            public byte fileNum;
            public int offs;

            public ModuleOffs(byte tune, byte fileNum, int offs)
            {
                this.tune = tune;
                this.fileNum = fileNum;
                this.offs = offs;
            }
        }

        private static readonly ModuleOffs[] amigaWaxworksOffs =
        {
            // Pyramid
            new ModuleOffs(2, 2, 0),
            new ModuleOffs(3, 2, 50980),
            new ModuleOffs(4, 2, 56160),
            new ModuleOffs(5, 2, 62364),
            new ModuleOffs(6, 2, 73688),

            // Zombie
            new ModuleOffs(8, 8, 0),
            new ModuleOffs(11, 8, 51156),
            new ModuleOffs(12, 8, 56336),
            new ModuleOffs(13, 8, 65612),
            new ModuleOffs(14, 8, 68744),

            // Mine
            new ModuleOffs(9, 9, 0),
            new ModuleOffs(15, 9, 47244),
            new ModuleOffs(16, 9, 52424),
            new ModuleOffs(17, 9, 59652),
            new ModuleOffs(18, 9, 62784),

            // Jack
            new ModuleOffs(10, 10, 0),
            new ModuleOffs(19, 10, 42054),
            new ModuleOffs(20, 10, 47234),
            new ModuleOffs(21, 10, 49342),
            new ModuleOffs(22, 10, 51450),
        };
    }
}