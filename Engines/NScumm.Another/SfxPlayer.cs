//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal class SfxPlayer
    {
        public Ptr<short> MarkVar;

        private readonly object _mutex = new object();
        private readonly AnotherMixer _mixer;
        private readonly Resource _res;
        private readonly IAnotherSystem _sys;
        private readonly SfxModule _sfxMod;

        private object _timerId;
        private ushort _delay;
        private ushort _resNum;

        public SfxPlayer(AnotherMixer mixer, Resource res, IAnotherSystem sys)
        {
            _mixer = mixer;
            _res = res;
            _sys = sys;

            _sfxMod = new SfxModule();
        }

        public void LoadSfxModule(ushort resNum, ushort delay, byte pos)
        {
            Debug(DebugLevels.DbgSnd, "SfxPlayer::loadSfxModule(0x{0:X}, {1}, {2})", resNum, delay, pos);
            lock (_mutex)
            {
                var me = _res.MemList[resNum];

                if (me.State == MemEntry.Loaded && me.Type == ResType.RtMusic)
                {
                    _resNum = resNum;
                    _sfxMod.Reset();
                    _sfxMod.CurOrder = pos;
                    _sfxMod.NumOrder = (byte) me.BufPtr.ToUInt16BigEndian(0x3E);
                    Debug(DebugLevels.DbgSnd, "SfxPlayer::loadSfxModule() curOrder = 0x{0:X} numOrder = 0x{1:X}",
                        _sfxMod.CurOrder,
                        _sfxMod.NumOrder);
                    for (var i = 0; i < 0x80; ++i)
                    {
                        _sfxMod.OrderTable[i] = me.BufPtr[0x40 + i];
                    }
                    _delay = delay == 0 ? me.BufPtr.ToUInt16BigEndian() : delay;
                    _delay = (ushort) (_delay * 60 / 7050);
                    _sfxMod.Data = me.BufPtr + 0xC0;
                    Debug(DebugLevels.DbgSnd, "SfxPlayer::loadSfxModule() eventDelay = {0} ms", _delay);
                    PrepareInstruments(me.BufPtr + 2);
                }
                else
                {
                    Warning("SfxPlayer::loadSfxModule() ec=0x{0:X}", 0xF8);
                }
            }
        }

        public void SetEventsDelay(ushort delay)
        {
            Debug(DebugLevels.DbgSnd, "SfxPlayer::setEventsDelay({0})", delay);
            lock (_mutex)
            {
                _delay = (ushort) (delay * 60 / 7050);
            }
        }

        public void Start()
        {
            Debug(DebugLevels.DbgSnd, "SfxPlayer::start()");
            lock (_mutex)
            {
                _sfxMod.CurPos = 0;
                _timerId = _sys.AddTimer(_delay, EventsCallback, this);
            }
        }

        public void Stop()
        {
            Debug(DebugLevels.DbgSnd, "SfxPlayer::stop()");
            lock (_mutex)
            {
                if (_resNum != 0)
                {
                    _resNum = 0;
                    _sys.RemoveTimer(_timerId);
                }
            }
        }

        private void HandleEvents()
        {
            lock (_mutex)
            {
                var order = _sfxMod.OrderTable[_sfxMod.CurOrder];
                var patternData = _sfxMod.Data + _sfxMod.CurPos + order * 1024;
                for (var ch = 0; ch < 4; ++ch)
                {
                    HandlePattern((byte) ch, patternData);
                    patternData += 4;
                }
                _sfxMod.CurPos += 4 * 4;
                Debug(DebugLevels.DbgSnd, "SfxPlayer::handleEvents() order = 0x{0:X} curPos = 0x{1:X}", order,
                    _sfxMod.CurPos);
                if (_sfxMod.CurPos >= 1024)
                {
                    _sfxMod.CurPos = 0;
                    order = (byte) (_sfxMod.CurOrder + 1);
                    if (order == _sfxMod.NumOrder)
                    {
                        _resNum = 0;
                        _sys.RemoveTimer(_timerId);
                        _mixer.StopAll();
                    }
                    _sfxMod.CurOrder = order;
                }
            }
        }

        private void HandlePattern(byte channel, BytePtr data)
        {
            var pat = new SfxPattern
            {
                Note1 = data.ToUInt16BigEndian(),
                Note2 = data.ToUInt16BigEndian(2)
            };
            if (pat.Note1 != 0xFFFD)
            {
                var sample = (ushort) ((pat.Note2 & 0xF000) >> 12);
                if (sample != 0)
                {
                    var ptr = _sfxMod.Samples[sample - 1].Data;
                    if (ptr != BytePtr.Null)
                    {
                        Debug(DebugLevels.DbgSnd, "SfxPlayer::handlePattern() preparing sample {0}", sample);
                        pat.SampleVolume = _sfxMod.Samples[sample - 1].Volume;
                        pat.SampleStart = 8;
                        pat.SampleBuffer = ptr;
                        pat.SampleLen = (ushort) (ptr.ToUInt16BigEndian() * 2);
                        var loopLen = (ushort) (ptr.ToUInt16BigEndian(2) * 2);
                        if (loopLen != 0)
                        {
                            pat.LoopPos = pat.SampleLen;
                            pat.LoopData = ptr;
                            pat.LoopLen = loopLen;
                        }
                        else
                        {
                            pat.LoopPos = 0;
                            pat.LoopData = BytePtr.Null;
                            pat.LoopLen = 0;
                        }
                        var m = (short) pat.SampleVolume;
                        var effect = (byte) ((pat.Note2 & 0x0F00) >> 8);
                        if (effect == 5)
                        {
                            // volume up
                            var volume = (byte) (pat.Note2 & 0xFF);
                            m += volume;
                            if (m > 0x3F)
                            {
                                m = 0x3F;
                            }
                        }
                        else if (effect == 6)
                        {
                            // volume down
                            var volume = (byte) (pat.Note2 & 0xFF);
                            m -= volume;
                            if (m < 0)
                            {
                                m = 0;
                            }
                        }
                        _mixer.SetChannelVolume(channel, (byte) m);
                        pat.SampleVolume = (ushort) m;
                    }
                }
            }
            if (pat.Note1 == 0xFFFD)
            {
                Debug(DebugLevels.DbgSnd, "SfxPlayer::handlePattern() _scriptVars[0xF4] = 0x{0:X}", pat.Note2);
                MarkVar.Value = (short) pat.Note2;
            }
            else if (pat.Note1 != 0)
            {
                if (pat.Note1 == 0xFFFE)
                {
                    _mixer.StopChannel(channel);
                }
                else if (pat.SampleBuffer != BytePtr.Null)
                {
                    var mc = new MixerChunk
                    {
                        Data = pat.SampleBuffer + pat.SampleStart,
                        Len = pat.SampleLen,
                        LoopPos = pat.LoopPos,
                        LoopLen = pat.LoopLen
                    };
                    //assert(pat.note_1 >= 0x37 && pat.note_1 < 0x1000);
                    // convert amiga period value to hz
                    var freq = (ushort) (7159092 / (pat.Note1 * 2));
                    Debug(DebugLevels.DbgSnd, "SfxPlayer::handlePattern() adding sample freq = 0x{0:X}", freq);
                    _mixer.PlayChannel(channel, mc, freq, (byte) pat.SampleVolume);
                }
            }
        }

        private static uint EventsCallback(int interval, object param)
        {
            var p = (SfxPlayer) param;
            p.HandleEvents();
            return p._delay;
        }

        private void PrepareInstruments(BytePtr p)
        {
            _sfxMod.ResetSamples();

            for (var i = 0; i < 15; ++i)
            {
                var ins = _sfxMod.Samples[i];
                var resNum = p.ToUInt16BigEndian();
                p += 2;
                if (resNum != 0)
                {
                    ins.Volume = p.ToUInt16BigEndian();
                    var me = _res.MemList[resNum];
                    if (me.State == MemEntry.Loaded && me.Type == ResType.RtSound)
                    {
                        ins.Data = me.BufPtr;
                        Array.Clear(ins.Data.Data, ins.Data.Offset + 8, 4);
                        Debug(DebugLevels.DbgSnd, "Loaded instrument 0x{0:X} n={1} volume={2}", resNum, i, ins.Volume);
                    }
                    else
                    {
                        Error("Error loading instrument 0x{0:X}", resNum);
                    }
                }
                p += 2; // skip volume
            }
        }

        public void SaveOrLoad(Serializer ser)
        {
            lock (_mutex)
            {
                Entry[] entries =
                {
                    Entry.Create(this, o=>o._delay, 2),
                    Entry.Create(this, o=>o._resNum, 2),
                    Entry.Create(_sfxMod, o=>o.CurPos, 2),
                    Entry.Create(_sfxMod, o=>o.CurOrder, 2),
                };
                ser.SaveOrLoadEntries(entries);
            }
            if (ser.Mode == Mode.SmLoad && _resNum != 0)
            {
                var delay = _delay;
                LoadSfxModule(_resNum, 0, _sfxMod.CurOrder);
                _delay = delay;
                _timerId = _sys.AddTimer(_delay, EventsCallback, this);
            }
        }
    }
}