using System;
using System.IO;
using System.Linq.Expressions;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    internal enum CowMode
    {
        CowWave = 0,
        CowFLAC,
        CowVorbis,
        CowMP3,
        CowDemo,
        CowPSX
    }

    internal struct RoomVol
    {
        public int roomNo, leftVol, rightVol;
    }

    internal struct SampleId
    {
        public byte cluster;
        public byte idStd;
        public byte idWinDemo;
    }

    internal class FxDef
    {
        public SampleId sampleId;
        public uint type, delay;
        public RoomVol[] roomVolList = new RoomVol[Sound.MAX_ROOMS_PER_FX];

        public FxDef(byte[] sampleId, uint type, uint delay, int[][] roomVols)
        {
            this.sampleId.cluster = sampleId[0];
            this.sampleId.idStd = sampleId[1];
            this.sampleId.idWinDemo = sampleId[2];
            this.type = type;
            this.delay = delay;
            for (int i = 0; i < roomVols.GetLength(0); i++)
            {
                roomVolList[i].roomNo = roomVols[i][0];
                roomVolList[i].leftVol = roomVols[i][1];
                roomVolList[i].rightVol = roomVols[i][2];
            }
        }
    }

    internal struct QueueElement
    {
        public uint id, delay;
        public SoundHandle handle;
    }

    internal partial class Sound
    {
        private const int TOTAL_FX_PER_ROOM = 7; // total loop & random fx per room (see fx_list.c)
        private const int TOTAL_ROOMS = 100; //total number of rooms
        public const int MAX_ROOMS_PER_FX = 7; // max no. of rooms in the fx's room,vol list
        private const int MAX_FXQ_LENGTH = 32;      // max length of sound queue - ie. max number of fx that can be stored up/playing together

        private BinaryReader _cowFile;
        private UIntAccess _cowHeader;
        private uint _cowHeaderSize;
        private byte _currentCowFile;
        private CowMode _cowMode;
        private SoundHandle _speechHandle;
        private SoundHandle _fxHandle;

        private readonly QueueElement[] _fxQueue = new QueueElement[MAX_FXQ_LENGTH];
        private byte _endOfQueue;
        private readonly Random _rnd = new Random(Environment.TickCount);

        public Sound(GameSettings settings, Mixer mixer, ResMan resMan)
        {
            _mixer = mixer;
            _resMan = resMan;
            _settings = settings;

            _speechVolL = _speechVolR = _sfxVolL = _sfxVolR = 192;
        }

        public void NewScreen(uint screen)
        {
            if (_currentCowFile != SystemVars.CurrentCd)
            {
                if (_cowFile != null)
                    CloseCowSystem();
                InitCowSystem();
            }

            // Start the room's looping sounds.
            for (ushort cnt = 0; cnt < TOTAL_FX_PER_ROOM; cnt++)
            {
                ushort fxNo = _roomsFixedFx[screen][cnt];
                if (fxNo != 0)
                {
                    if (_fxList[fxNo].type == FX_LOOP)
                        AddToQueue(fxNo);
                }
                else
                    break;
            }
        }

        public void GiveSpeechVol(out byte volL, out byte volR)
        {
            volL = _speechVolL;
            volR = _speechVolR;
        }

        public void GiveSfxVol(out byte volL, out byte volR)
        {
            volL = _sfxVolL;
            volR = _sfxVolR;
        }

        public void SetSpeechVol(byte volL, byte volR)
        {
            _speechVolL = volL; _speechVolR = volR;
        }

        public void SetSfxVol(byte volL, byte volR)
        {
            _sfxVolL = volL; _sfxVolR = volR;
        }

        public void Engine()
        {
            // first of all, add any random sfx to the queue...
            for (ushort cnt = 0; cnt < TOTAL_FX_PER_ROOM; cnt++)
            {
                ushort fxNo = _roomsFixedFx[Logic.ScriptVars[(int)ScriptVariableNames.SCREEN]][cnt];
                if (fxNo != 0)
                {
                    if (_fxList[fxNo].type == FX_RANDOM)
                    {
                        if (_rnd.Next((int)_fxList[fxNo].delay) == 0)
                            AddToQueue(fxNo);
                    }
                }
                else
                    break;
            }
            // now process the queue
            for (var cnt2 = 0; cnt2 < _endOfQueue; cnt2++)
            {
                if (_fxQueue[cnt2].delay > 0)
                {
                    _fxQueue[cnt2].delay--;
                    if (_fxQueue[cnt2].delay == 0)
                        PlaySample(_fxQueue[cnt2]);
                }
                else
                {
                    if (!_mixer.IsSoundHandleActive(_fxQueue[cnt2].handle))
                    { // sound finished
                        _resMan.ResClose(GetSampleId((int)_fxQueue[cnt2].id));
                        if (cnt2 != _endOfQueue - 1)
                            _fxQueue[cnt2] = _fxQueue[_endOfQueue - 1];
                        _endOfQueue--;
                    }
                }
            }
        }

        public void QuitScreen()
        {
            // TODO:
        }

        public void CloseCowSystem()
        {
            // TODO:
        }

        public void CheckSpeechFileEndianness()
        {
            // TODO:
        }

        public bool StartSpeech(int i, int i1)
        {
            // TODO:
            return false;
        }

        public uint AddToQueue(int fxNo)
        {
            // TODO:
            return 0;
            bool alreadyInQueue = false;
            for (var cnt = 0; (cnt < _endOfQueue) && (!alreadyInQueue); cnt++)
                if (_fxQueue[cnt].id == (uint)fxNo)
                    alreadyInQueue = true;
            if (!alreadyInQueue)
            {
                if (_endOfQueue == MAX_FXQ_LENGTH)
                {
                    // TODO: warning("Sound queue overflow");
                    return 0;
                }
                uint sampleId = GetSampleId(fxNo);
                if ((sampleId & 0xFF) != 0xFF)
                {
                    _resMan.ResOpen(sampleId);
                    _fxQueue[_endOfQueue].id = (uint) fxNo;
                    if (_fxList[fxNo].type == FX_SPOT)
                        _fxQueue[_endOfQueue].delay = _fxList[fxNo].delay + 1;
                    else
                        _fxQueue[_endOfQueue].delay = 1;
                    _endOfQueue++;
                    return 1;
                }
                return 0;
            }
            return 0;
        }

        public void FnStopFx(int fxNo)
        {
            // TODO:
        }

        public bool SpeechFinished()
        {
            // TODO:
            return true;
        }

        public void StopSpeech()
        {
            // TODO:
        }

        public int AmISpeaking()
        {
            return 0;
        }


        private uint GetSampleId(int fxNo)
        {
            byte cluster = _fxList[fxNo].sampleId.cluster;
            byte id;
            if (SystemVars.IsDemo && SystemVars.Platform == Platform.Windows)
            {
                id = _fxList[fxNo].sampleId.idWinDemo;
            }
            else
            {
                id = _fxList[fxNo].sampleId.idStd;
            }
            return (uint)((cluster << 24) | id);
        }

        private void PlaySample(QueueElement elem)
        {
            var sampleData = _resMan.FetchRes(GetSampleId((int)elem.id));
            for (var cnt = 0; cnt < MAX_ROOMS_PER_FX; cnt++)
            {
                if (_fxList[elem.id].roomVolList[cnt].roomNo != 0)
                {
                    if ((_fxList[elem.id].roomVolList[cnt].roomNo == (int)Logic.ScriptVars[(int)ScriptVariableNames.SCREEN]) ||
                            (_fxList[elem.id].roomVolList[cnt].roomNo == -1))
                    {

                        byte volL = (byte)((_fxList[elem.id].roomVolList[cnt].leftVol * 10 * _sfxVolL) / 255);
                        byte volR = (byte)((_fxList[elem.id].roomVolList[cnt].rightVol * 10 * _sfxVolR) / 255);
                        sbyte pan = (sbyte)((volR - volL) / 2);
                        byte volume = (byte)((volR + volL) / 2);

                        if (SystemVars.Platform == Platform.PSX)
                        {
                            // TODO: PSX
                            throw new NotImplementedException();
                            //uint size = sampleData.ToUInt32(0);
                            //var audStream = new LoopingAudioStream(new XAStream(new MemoryStream(sampleData, 4, size - 4), 11025), (_fxList[elem.id].type == FX_LOOP) ? 0 : 1);
                            //elem.handle = _mixer.PlayStream(SoundType.SFX, audStream, (int) elem.id, volume, pan);
                        }
                        else
                        {
                            uint size = sampleData.ToUInt32(0x28);
                            AudioFlags flags;
                            if (sampleData.ToUInt16(0x22) == 16)
                                flags = AudioFlags.Is16Bits | AudioFlags.LittleEndian;
                            else
                                flags = AudioFlags.Unsigned;
                            if (sampleData.ToUInt16(0x16) == 2)
                                flags |= AudioFlags.Stereo;
                            var stream = new LoopingAudioStream(new RawStream(flags, 11025, false, new MemoryStream(sampleData, 0x2C, (int)size)),
                                                             (_fxList[elem.id].type == FX_LOOP) ? 0 : 1);
                            elem.handle = _mixer.PlayStream(SoundType.SFX, stream, (int)elem.id, volume, pan);
                        }
                    }
                }
                else
                    break;
            }
        }

        private BinaryReader TryToOpen(string filename)
        {
            var directory = ServiceLocator.FileStorage.GetDirectoryName(_settings.Game.Path);
            var path = ScummHelper.LocatePath(directory, filename);
            if (path != null)
            {
                return new BinaryReader(ServiceLocator.FileStorage.OpenFileRead(path));
            }
            return null;
        }

        private void InitCowSystem()
        {
            if (SystemVars.CurrentCd == 0)
                return;
            if (_cowFile == null)
            {
                var cowName = $"SPEECH{SystemVars.CurrentCd}.CLU";
                _cowFile = TryToOpen(cowName);
                //if (!_cowFile.isOpen())
                //{
                //    _cowFile.open("speech.clu");
                //}
                // TODO: debug(1, "Using uncompressed Speech Cluster");
                _cowMode = CowMode.CowWave;
            }

            if (Sword1.SystemVars.Platform == Platform.PSX)
            {
                // There's only one file on the PSX, so set it to the current disc.
                _currentCowFile = (byte)SystemVars.CurrentCd;
                if (_cowFile == null)
                {
                    _cowFile = TryToOpen("speech.dat");
                    _cowMode = CowMode.CowPSX;
                }
            }

            if (_cowFile == null)
                _cowFile = TryToOpen("speech.clu");

            if (_cowFile == null)
            {
                _cowFile = TryToOpen("cows.mad");
                _cowMode = CowMode.CowDemo;
            }

            if (_cowFile != null)
            {
                if (SystemVars.Platform == Platform.PSX)
                {
                    // Get data from the external table file
                    using (var tableFile = TryToOpen("speech.tab"))
                    {
                        _cowHeaderSize = (uint)tableFile.BaseStream.Length;
                        _cowHeader = new UIntAccess(new byte[_cowHeaderSize], 0);
                        if ((_cowHeaderSize & 3) != 0)
                            throw new InvalidOperationException($"Unexpected cow header size {_cowHeaderSize}");
                        for (var cnt = 0; cnt < _cowHeaderSize / 4; cnt++)
                            _cowHeader[cnt] = tableFile.ReadUInt32();
                    }
                }
                else
                {
                    _cowHeaderSize = _cowFile.ReadUInt32();
                    _cowHeader = new UIntAccess(new byte[_cowHeaderSize], 0);
                    if ((_cowHeaderSize & 3) != 0)
                        throw new InvalidOperationException("Unexpected cow header size {_cowHeaderSize}");
                    for (var cnt = 0; cnt < (_cowHeaderSize / 4) - 1; cnt++)
                        _cowHeader[cnt] = _cowFile.ReadUInt32();
                    _currentCowFile = (byte)SystemVars.CurrentCd;
                }
            }
            else
            {
                // TODO: warning($"Sound::initCowSystem: Can't open SPEECH{SystemVars.CurrentCd}.CLU");
            }
        }
        //--------------------------------------------------------------------------------------
        // Continuous & random background sound effects for each location

        // NB. There must be a list for each room number, even if location doesn't exist in game

        private static readonly ushort[][] _roomsFixedFx = new ushort[TOTAL_ROOMS][]
        {
            new ushort[] {0}, // 0

            // PARIS 1
            new ushort[] {2, 3, 4, 5, 0}, // 1
            new ushort[] {2, 0}, // 2
            new ushort[] {2, 3, 4, 5, 32, 0}, // 3
            new ushort[] {2, 3, 4, 5, 0}, // 4
            new ushort[] {2, 3, 4, 5, 0}, // 5
            new ushort[] {9, 11, 12, 13, 44, 45, 47}, // 6
            new ushort[] {9, 11, 12, 13, 44, 45, 47}, // 7
            new ushort[] {2, 3, 4, 5, 0}, // 8

            // PARIS 2
            new ushort[] {54, 63, 0}, // 9
            new ushort[] {51, 52, 53, 54, 63, 0}, // 10
            new ushort[] {70, 0}, // 11
            new ushort[] {51, 52, 70, 0}, // 12
            new ushort[] {0}, // 13
            new ushort[] {238, 0}, // 14
            new ushort[] {82, 0}, // 15
            new ushort[] {70, 81, 82, 0}, // 16
            new ushort[] {82, 0}, // 17
            new ushort[] {3, 4, 5, 70, 0}, // 18

            // IRELAND
            new ushort[] {120, 121, 122, 243, 0}, // 19
            new ushort[] {0}, // 20 Violin makes the ambience..
            new ushort[] {120, 121, 122, 243, 0}, // 21
            new ushort[] {120, 121, 122, 0}, // 22
            new ushort[] {120, 121, 122, 124, 0}, // 23
            new ushort[] {120, 121, 122, 0}, // 24
            new ushort[] {0}, // 25
            new ushort[] {123, 243, 0}, // 26

            // PARIS 3
            new ushort[] {135, 0}, // 27
            new ushort[] {202, 0}, // 28
            new ushort[] {202, 0}, // 29
            new ushort[] {0}, // 30
            new ushort[] {187, 0}, // 31
            new ushort[] {143, 145, 0}, // 32
            new ushort[] {143, 0}, // 33
            new ushort[] {143, 0}, // 34
            new ushort[] {0}, // 35

            // PARIS 4
            new ushort[] {198, 0}, // 36
            new ushort[] {225, 0}, // 37
            new ushort[] {160, 0}, // 38
            new ushort[] {0}, // 39
            new ushort[] {198, 0}, // 40
            new ushort[] {279, 0}, // 41
            new ushort[] {0}, // 42
            new ushort[] {279, 0}, // 43
            new ushort[] {0}, // 44 Doesn't exist

            // SYRIA
            new ushort[] {153, 0}, // 45
            new ushort[] {70, 81, 0}, // 46 - PARIS 2
            new ushort[] {153, 0}, // 47
            new ushort[] {160, 0}, // 48 - PARIS 4
            new ushort[] {0}, // 49
            new ushort[] {153, 0}, // 50
            new ushort[] {0}, // 51
            new ushort[] {0}, // 52
            new ushort[] {0}, // 53
            new ushort[] {130, 138, 0}, // 54
            new ushort[] {0}, // 55

            // SPAIN
            new ushort[] {204, 0}, // 56
            new ushort[] {181, 182, 184, 0}, // 57
            new ushort[] {181, 182, 184, 0}, // 58
            new ushort[] {0}, // 59
            new ushort[] {184, 0}, // 60
            new ushort[] {185, 0}, // 61
            new ushort[] {0}, // 62 Just music

            // NIGHT TRAIN
            new ushort[] {207, 0, 0}, // 63
            new ushort[] {0}, // 64 Doesn't exist
            new ushort[] {207, 0}, // 65
            new ushort[] {207, 0}, // 66
            new ushort[] {207, 0}, // 67
            new ushort[] {0}, // 68 Disnae exist
            new ushort[] {0}, // 69

            // SCOTLAND + FINALE
            new ushort[] {0}, // 70 Disnae exist
            new ushort[] {199, 200, 201, 242, 0}, // 71
            new ushort[] {199, 200, 201, 242, 0}, // 72
            new ushort[] {0}, // 73
            new ushort[] {284, 0}, // 74
            new ushort[] {284, 0}, // 75
            new ushort[] {284, 0}, // 76
            new ushort[] {284, 0}, // 77
            new ushort[] {284, 0}, // 78
            new ushort[] {284, 0}, // 79
            new ushort[] {0}, // 80
            new ushort[] {0}, // 81
            new ushort[] {0}, // 82
            new ushort[] {0}, // 83
            new ushort[] {0}, // 84
            new ushort[] {0}, // 85
            new ushort[] {0}, // 86
            new ushort[] {0}, // 87
            new ushort[] {0}, // 88
            new ushort[] {0}, // 89
            new ushort[] {0}, // 90
            new ushort[] {0}, // 91
            new ushort[] {0}, // 92
            new ushort[] {0}, // 93
            new ushort[] {0}, // 94
            new ushort[] {0}, // 95
            new ushort[] {0}, // 96
            new ushort[] {0}, // 97
            new ushort[] {0}, // 98
            new ushort[] {0}, // 99
        };

        private readonly GameSettings _settings;
        private readonly Mixer _mixer;
        private readonly ResMan _resMan;
        private byte _sfxVolL;
        private byte _sfxVolR;
        private byte _speechVolL;
        private byte _speechVolR;
    }
}