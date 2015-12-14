using System;
using System.Diagnostics;
using System.IO;
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

    internal class QueueElement
    {
        public uint id, delay;
        public SoundHandle handle;
    }

    internal partial class Sound
    {
        public const int MAX_ROOMS_PER_FX = 7; // max no. of rooms in the fx's room,vol list
        private const int TOTAL_FX_PER_ROOM = 7; // total loop & random fx per room (see fx_list.c)
        private const int TOTAL_ROOMS = 100; //total number of rooms
        private const int MAX_FXQ_LENGTH = 32;      // max length of sound queue - ie. max number of fx that can be stored up/playing together
        private const int SOUND_SPEECH_ID = 1;
        private const int WAVE_VOL_TAB_LENGTH = 480;
        private const int WAVE_VOL_THRESHOLD = 190000; //120000;
        private const AudioFlags SPEECH_FLAGS = AudioFlags.Is16Bits | AudioFlags.LittleEndian;

        private BinaryReader _cowFile;
        private UIntAccess _cowHeader;
        private uint _cowHeaderSize;
        private byte _currentCowFile;
        private CowMode _cowMode;
        private SoundHandle _speechHandle;

        private readonly QueueElement[] _fxQueue;
        private byte _endOfQueue;
        private readonly Random _rnd = new Random(Environment.TickCount);
        private ushort _waveVolPos;
        private bool[] _waveVolume = new bool[WAVE_VOL_TAB_LENGTH];

        public Sound(GameSettings settings, Mixer mixer, ResMan resMan)
        {
            _mixer = mixer;
            _resMan = resMan;
            _settings = settings;

            _speechVolL = _speechVolR = _sfxVolL = _sfxVolR = 192;
            _fxQueue = new QueueElement[MAX_FXQ_LENGTH];
            for (int i = 0; i < _fxQueue.Length; i++)
            {
                _fxQueue[i] = new QueueElement();
            }
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
            // stop all running SFX
            while (_endOfQueue != 0)
                FnStopFx((int)_fxQueue[0].id);
        }

        public void CloseCowSystem()
        {
            _cowFile.Dispose();
            _cowHeader = null;
            _currentCowFile = 0;
        }

        public void CheckSpeechFileEndianness()
        {
            // Some mac versions (not all of them) use big endian wav, although
            // the wav header doesn't indicate it.
            // Use heuristic to determine endianness of speech.
            // The heuristic consist in computing the sum of the absolute difference for
            // every two consecutive samples. This is done both with a big endian and a
            // little endian assumption. The one with the smallest sum should be the
            // correct one (the sound wave is supposed to be relatively smooth).
            // It needs at least 1000 samples to get stable result (the code below is
            // using the first 2000 samples of the wav sound).

            // Init speech file if not already done.
            if (_currentCowFile == 0)
            {
                // Open one of the speech files. It uses SwordEngine::_systemVars.currentCD
                // to decide which file to open, therefore if it is currently set to zero
                // we have to set it to either 1 or 2 (I decided to set it to 1 as this is
                // more likely to be the first file that will be needed).
                bool no_current_cd = false;
                if (SystemVars.CurrentCd == 0)
                {
                    SystemVars.CurrentCd = 1;
                    no_current_cd = true;
                }
                InitCowSystem();
                if (no_current_cd)
                {
                    // In case it fails with CD1 retry with CD2
                    if (_currentCowFile == 0)
                    {
                        SystemVars.CurrentCd = 2;
                        InitCowSystem();
                    }
                    // Reset currentCD flag
                    SystemVars.CurrentCd = 0;
                }
            }

            // Testing for endianness makes sense only if using the uncompressed files.
            if (_cowHeader == null || (_cowMode != CowMode.CowWave && _cowMode != CowMode.CowDemo))
                return;

            // I picked the sample to use randomly (I just made sure it is long enough so that there is
            // a fair chance of the heuristic to have a stable result and work for every language).
            int roomNo = _currentCowFile == 1 ? 1 : 129;
            int localNo = _currentCowFile == 1 ? 2 : 933;
            // Get the speech data and apply the heuristic
            uint locIndex = _cowHeader[roomNo] >> 2;
            var sampleSize = _cowHeader[(int)(locIndex + (localNo * 2))];
            var index = _cowHeader[(int)(locIndex + (localNo * 2) - 1)];
            if (sampleSize != 0)
            {
                uint size;
                // Compute average of difference between two consecutive samples for both BE and LE
                _bigEndianSpeech = false;
                var data = UncompressSpeech(index + _cowHeaderSize, sampleSize, out size);
                uint maxSamples = size > 2000 ? 2000 : size;
                double le_diff = EndiannessHeuristicValue(data, size, ref maxSamples);

                _bigEndianSpeech = true;
                data = UncompressSpeech(index + _cowHeaderSize, sampleSize, out size);
                double be_diff = EndiannessHeuristicValue(data, size, ref maxSamples);

                // Set the big endian flag
                _bigEndianSpeech = (be_diff < le_diff);
                if (_bigEndianSpeech)
                {
                    // TODO: debug(6, "Mac version: using big endian speech file");
                }
                else
                {
                    // TODO: debug(6, "Mac version: using little endian speech file");
                }
                // TODO: debug(8, "Speech endianness heuristic: average = %f for BE and %f for LE (%d samples)", be_diff, le_diff, maxSamples);
            }
        }

        public bool StartSpeech(ushort roomNo, ushort localNo)
        {
            if (_cowHeader == null || ServiceLocator.AudioManager == null)
            {
                // TODO: warning("Sound::startSpeech: COW file isn't open");
                return false;
            }

            uint locIndex = 0xFFFFFFFF;
            int sampleSize = 0;
            uint index = 0;

            //            if (_cowMode == CowPSX)
            //            {
            //                Common::File file;
            //                uint16 i;

            //                if (!file.open("speech.lis"))
            //                {
            //                    warning("Could not open speech.lis");
            //                    return false;
            //                }

            //                for (i = 0; !file.eos() && !file.err(); i++)
            //                    if (file.readUint16LE() == roomNo)
            //                    {
            //                        locIndex = i;
            //                        break;
            //                    }
            //                file.close();

            //                if (locIndex == 0xFFFFFFFF)
            //                {
            //                    warning("Could not find room %d in speech.lis", roomNo);
            //                    return false;
            //                }

            //                if (!file.open("speech.inf"))
            //                {
            //                    warning("Could not open speech.inf");
            //                    return false;
            //                }

            //                uint16 numRooms = file.readUint16LE(); // Read number of rooms referenced in this file

            //                file.seek(locIndex * 4 + 2); // 4 bytes per room, skip first 2 bytes

            //                uint16 numLines = file.readUint16LE();
            //                uint16 roomOffset = file.readUint16LE();

            //                file.seek(2 + numRooms * 4 + roomOffset * 2); // The offset is in terms of uint16's, so multiply by 2. Skip the room indexes too.

            //                locIndex = 0xFFFFFFFF;

            //                for (i = 0; i < numLines; i++)
            //                    if (file.readUint16LE() == localNo)
            //                    {
            //                        locIndex = i;
            //                        break;
            //                    }

            //                if (locIndex == 0xFFFFFFFF)
            //                {
            //                    warning("Could not find local number %d in room %d in speech.inf", roomNo, localNo);
            //                    return false;
            //                }

            //                file.close();

            //                index = _cowHeader[(roomOffset + locIndex) * 2];
            //                sampleSize = _cowHeader[(roomOffset + locIndex) * 2 + 1];
            //            }
            //            else {
            locIndex = _cowHeader[roomNo] >> 2;
            sampleSize = (int)_cowHeader[(int)(locIndex + (localNo * 2))];
            index = _cowHeader[(int)(locIndex + (localNo * 2) - 1)];
            //            }

            //            debug(6, "startSpeech(%d, %d): locIndex %d, sampleSize %d, index %d", roomNo, localNo, locIndex, sampleSize, index);

            IAudioStream stream = null;

            if (sampleSize != 0)
            {
                byte speechVol = (byte)((_speechVolR + _speechVolL) / 2);
                sbyte speechPan = (sbyte)((_speechVolR - _speechVolL) / 2);
                if ((_cowMode == CowMode.CowWave) || (_cowMode == CowMode.CowDemo))
                {
                    uint size;
                    var data = UncompressSpeech(index + _cowHeaderSize, (uint)sampleSize, out size);
                    if (data != null)
                    {
                        stream = new RawStream(SPEECH_FLAGS, 11025, true, new MemoryStream(data.Data, data.Offset, (int)size));
                        _speechHandle = _mixer.PlayStream(SoundType.Speech, stream, SOUND_SPEECH_ID, speechVol, speechPan);
                    }
                }
                //                else if (_cowMode == CowPSX && sampleSize != 0xffffffff)
                //                {
                //                    _cowFile.seek(index * 2048);
                //                    Common::SeekableReadStream* tmp = _cowFile.readStream(sampleSize);
                //                    assert(tmp);
                //                    stream = Audio::makeXAStream(tmp, 11025);
                //                    _mixer->playStream(Audio::Mixer::kSpeechSoundType, &_speechHandle, stream, SOUND_SPEECH_ID, speechVol, speechPan);
                //                    // with compressed audio, we can't calculate the wave volume.
                //                    // so default to talking.
                //                    for (int cnt = 0; cnt < 480; cnt++)
                //                        _waveVolume[cnt] = true;
                //                    _waveVolPos = 0;
                //                }
                else if (_cowMode == CowMode.CowFLAC)
                {
                    _cowFile.BaseStream.Seek(index, SeekOrigin.Begin);
                    var tmp = _cowFile.ReadBytes(sampleSize);
                    stream = ServiceLocator.AudioManager.MakeFlacStream(new MemoryStream(tmp));
                    if (stream != null)
                    {
                        _speechHandle = _mixer.PlayStream(SoundType.Speech, stream, SOUND_SPEECH_ID, speechVol, speechPan);
                        // with compressed audio, we can't calculate the wave volume.
                        // so default to talking.
                        for (int cnt = 0; cnt < 480; cnt++)
                            _waveVolume[cnt] = true;
                        _waveVolPos = 0;
                    }
                }
                else if (_cowMode == CowMode.CowVorbis)
                {
                    _cowFile.BaseStream.Seek(index, SeekOrigin.Begin);
                    var tmp = _cowFile.ReadBytes(sampleSize);
                    stream = ServiceLocator.AudioManager.MakeVorbisStream(new MemoryStream(tmp));
                    if (stream != null)
                    {
                        _speechHandle = _mixer.PlayStream(SoundType.Speech, stream, SOUND_SPEECH_ID, speechVol, speechPan);
                        // with compressed audio, we can't calculate the wave volume.
                        // so default to talking.
                        for (int cnt = 0; cnt < 480; cnt++)
                            _waveVolume[cnt] = true;
                        _waveVolPos = 0;
                    }
                }
                else if (_cowMode == CowMode.CowMP3)
                {
                    _cowFile.BaseStream.Seek(index, SeekOrigin.Begin);
                    var tmp = _cowFile.ReadBytes(sampleSize);
                    stream = ServiceLocator.AudioManager.MakeMp3Stream(new MemoryStream(tmp));
                    if (stream != null)
                    {
                        _speechHandle = _mixer.PlayStream(SoundType.Speech, stream, SOUND_SPEECH_ID, speechVol, speechPan);
                        // with compressed audio, we can't calculate the wave volume.
                        // so default to talking.
                        for (int cnt = 0; cnt < 480; cnt++)
                            _waveVolume[cnt] = true;
                        _waveVolPos = 0;
                    }
                }
                return true;
            }
            else
                return false;
        }

        public uint AddToQueue(int fxNo)
        {
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
                    _fxQueue[_endOfQueue].id = (uint)fxNo;
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
            _mixer.StopID(fxNo);
            for (var cnt = 0; cnt < _endOfQueue; cnt++)
                if (_fxQueue[cnt].id == (uint)fxNo)
                {
                    if (_fxQueue[cnt].delay == 0) // sound was started
                        _resMan.ResClose(GetSampleId((int)_fxQueue[cnt].id));
                    if (cnt != _endOfQueue - 1)
                        _fxQueue[cnt] = _fxQueue[_endOfQueue - 1];
                    _endOfQueue--;
                    return;
                }
            // TODO: debug(8, "fnStopFx: id not found in queue");
        }

        public bool SpeechFinished()
        {
            return !_mixer.IsSoundHandleActive(_speechHandle);
        }

        public void StopSpeech()
        {
            _mixer.StopID(SOUND_SPEECH_ID);
        }

        public bool AmISpeaking()
        {
            _waveVolPos++;
            return _waveVolume[_waveVolPos - 1];
        }


        private double EndiannessHeuristicValue(UShortAccess data, uint dataSize, ref uint maxSamples)
        {
            if (data == null)
                return 50000; // the heuristic value for the wrong endianess is about 21000 (1/3rd of the 16 bits range)

            double diff_sum = 0;
            uint cpt = 0;
            short prev_value = (short)data[0];
            for (int i = 1; i < dataSize && cpt < maxSamples; ++i)
            {
                short value = (short)data[i];
                if (value != prev_value)
                {
                    diff_sum += Math.Abs((double)(value - prev_value));
                    ++cpt;
                    prev_value = value;
                }
            }
            if (cpt == 0)
                return 50000;
            maxSamples = cpt;
            return diff_sum / cpt;
        }

        private UShortAccess UncompressSpeech(uint index, uint cSize, out uint size)
        {
            _cowFile.BaseStream.Seek(index, SeekOrigin.Begin);
            var fBuf = _cowFile.ReadBytes((int)cSize);
            uint headerPos = 0;

            while ((fBuf.ToUInt32BigEndian((int)headerPos) != ScummHelper.MakeTag('d', 'a', 't', 'a')) && (headerPos < 100))
                headerPos++;

            UShortAccess srcData;
            if (headerPos < 100)
            {
                int resSize;
                uint srcPos;
                short length;
                cSize /= 2;
                headerPos += 4; // skip 'data' tag
                if (_cowMode != CowMode.CowDemo)
                {
                    resSize = (int)(fBuf.ToUInt32((int)headerPos) >> 1);
                    headerPos += 4;
                }
                else
                {
                    // the demo speech files have the uncompressed size
                    // embedded in the compressed stream *sigh*
                    //
                    // But not always, apparently. See bug #2182450. Is
                    // there any way to figure out the size other than
                    // decoding the sound in that case?

                    if (fBuf[headerPos + 1] == 0)
                    {
                        if (fBuf.ToInt16((int)headerPos) == 1)
                        {
                            resSize = fBuf.ToInt16((int)(headerPos + 2));
                            resSize |= fBuf.ToInt16((int)(headerPos + 6)) << 16;
                        }
                        else
                            resSize = fBuf.ToInt32((int)(headerPos + 2));
                        resSize >>= 1;
                    }
                    else
                    {
                        resSize = 0;
                        srcData = new UShortAccess(fBuf);
                        srcPos = headerPos >> 1;
                        while (srcPos < cSize)
                        {
                            length = (short)srcData[(int)srcPos];
                            srcPos++;
                            if (length < 0)
                            {
                                resSize -= length;
                                srcPos++;
                            }
                            else
                            {
                                resSize += length;
                                srcPos = (uint)(srcPos + length);
                            }
                        }
                    }
                }
                Debug.Assert((headerPos & 1) == 0);
                srcData = new UShortAccess(fBuf);
                srcPos = headerPos >> 1;
                uint dstPos = 0;
                var dstData = new UShortAccess(new byte[resSize * 2]);
                int samplesLeft = resSize;
                while (srcPos < cSize && samplesLeft > 0)
                {
                    length = (short)(_bigEndianSpeech ? ScummHelper.SwapBytes(srcData[(int)srcPos]) : srcData[(int)srcPos]);
                    srcPos++;
                    if (length < 0)
                    {
                        length = (short)-length;
                        if (length > samplesLeft)
                            length = (short)samplesLeft;
                        short value;
                        if (_bigEndianSpeech)
                        {
                            value = (short)ScummHelper.SwapBytes(srcData[(int)srcPos]);
                        }
                        else
                        {
                            value = (short)srcData[(int)srcPos];
                        }
                        for (ushort cnt = 0; cnt < (ushort)length; cnt++)
                            dstData[(int)dstPos++] = (ushort)value;
                        srcPos++;
                    }
                    else
                    {
                        if (length > samplesLeft)
                            length = (short)samplesLeft;
                        if (_bigEndianSpeech)
                        {
                            for (ushort cnt = 0; cnt < length; cnt++)
                                dstData[(int)dstPos++] = ScummHelper.SwapBytes(srcData[(int)srcPos++]);
                        }
                        else
                        {
                            Array.Copy(srcData.Data, (int)(srcData.Offset + srcPos * 2), dstData.Data, (int)(dstData.Offset + dstPos * 2), length * 2);
                            dstPos = (uint)(dstPos + length);
                            srcPos = (uint)(srcPos + length);
                        }
                    }
                    samplesLeft -= length;
                }
                if (samplesLeft > 0)
                {
                    dstData.Data.Set((int)(dstData.Offset + dstPos), 0, samplesLeft * 2);
                }
                if (_cowMode == CowMode.CowDemo) // demo has wave output size embedded in the compressed data
                {
                    dstData.Data.WriteUInt32(dstData.Offset, 0);
                }
                size = (uint)(resSize * 2);
                CalcWaveVolume(dstData, resSize);
                return dstData;
            }
            else
            {
                // TODO: warning("Sound::uncompressSpeech(): DATA tag not found in wave header");
                size = 0;
                return null;
            }
        }

        private void CalcWaveVolume(UShortAccess data, int length)
        {
            var blkPos = new UShortAccess(data.Data, data.Offset + 918 * 2);
            uint cnt;
            for (cnt = 0; cnt < WAVE_VOL_TAB_LENGTH; cnt++)
                _waveVolume[cnt] = false;
            _waveVolPos = 0;
            for (uint blkCnt = 1; blkCnt < length / 918; blkCnt++)
            {
                if (blkCnt >= WAVE_VOL_TAB_LENGTH)
                {
                    // TODO: warning("Wave vol tab too small");
                    return;
                }
                int average = 0;
                for (cnt = 0; cnt < 918; cnt++)
                    average += blkPos[(int)cnt];
                average /= 918;
                uint diff = 0;
                for (cnt = 0; cnt < 918; cnt++)
                {
                    short smpDiff = (short)(blkPos[0] - average);
                    diff += (uint)Math.Abs(smpDiff);
                    blkPos.Offset += 2;
                }
                if (diff > WAVE_VOL_THRESHOLD)
                    _waveVolume[blkCnt - 1] = true;
            }
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
                var cowName = $"SPEECH{SystemVars.CurrentCd}.CLF";
                _cowFile = TryToOpen(cowName);
                //if (_cowFile.isOpen())
                //{
                //    debug(1, "Using FLAC compressed Speech Cluster");
                //}
                _cowMode = CowMode.CowFLAC;
            }

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

            if (SystemVars.Platform == Platform.PSX)
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
        private bool _bigEndianSpeech;
    }
}