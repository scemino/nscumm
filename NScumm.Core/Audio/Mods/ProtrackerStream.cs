//
//  ProtrackerStream.cs
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
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio
{
    public class ProtrackerStream : Paula
    {
        private Module _module;

        int _tick;
        int _row;
        int _pos;

        int _speed;
        int _bpm;

        // For effect 0xB - Jump To Pattern;
        bool _hasJumpToPattern;
        int _jumpToPattern;

        // For effect 0xD - PatternBreak;
        bool _hasPatternBreak;
        int _skipRow;

        // For effect 0xE6 - Pattern Loop
        bool _hasPatternLoop;
        int _patternLoopCount;
        int _patternLoopRow;

        // For effect 0xEE - Pattern Delay
        byte _patternDelay;

        Track[] _track = new Track[4];

        class Track
        {
            public byte sample;
            public byte lastSample;
            public ushort period;
            public Offset offset;

            public byte vol;
            public byte finetune;

            // For effect 0x0 - Arpeggio
            public bool arpeggio;
            public byte[] arpeggioNotes = new byte[3];

            // For effect 0x3 - Porta to note
            public ushort portaToNote;
            public byte portaToNoteSpeed;

            // For effect 0x4 - Vibrato
            public int vibrato;
            public byte vibratoPos;
            public byte vibratoSpeed;
            public byte vibratoDepth;

            // For effect 0xED - Delay sample
            public byte delaySample;
            public byte delaySampleTick;
        }

        public ProtrackerStream(Stream stream, int offs = 0, int rate = 44100, bool stereo = true)
            : base(stereo, rate, (uint)(rate / 50))
        {
            _module = new Module();
            bool result = _module.Load(stream, offs);
            System.Diagnostics.Debug.Assert(result);

            _tick = _row = _pos = 0;

            _speed = 6;
            _bpm = 125;

            _hasJumpToPattern = false;
            _jumpToPattern = 0;

            _hasPatternBreak = false;
            _skipRow = 0;

            _hasPatternLoop = false;
            _patternLoopCount = 0;
            _patternLoopRow = 0;

            _patternDelay = 0;

            for (var i = 0; i < _track.Length; i++)
            {
                _track[i] = new Track();
            }

            StartPaula();
        }

        public Module Module
        {
            get
            {
                // Ordinarily, the Module is not meant to be seen outside of
                // this class, but occasionally, it's useful to be able to
                // manipulate it directly. The Hopkins engine uses this to
                // repair a broken song.
                return _module;
            }
        }

        protected override void Interrupt()
        {
            int track;

            for (track = 0; track < 4; track++)
            {
                _track[track].offset = GetChannelOffset(track);
                if (_tick == 0 && _track[track].arpeggio)
                {
                    _track[track].period = (ushort)Module.NoteToPeriod(_track[track].arpeggioNotes[0],
                            _track[track].finetune);
                }
            }

            if (_tick == 0)
            {
                if (_hasJumpToPattern)
                {
                    _hasJumpToPattern = false;
                    _pos = _jumpToPattern;
                    _row = 0;
                }
                else if (_hasPatternBreak)
                {
                    _hasPatternBreak = false;
                    _row = _skipRow;
                    _pos = (_pos + 1) % _module.songlen;
                    _patternLoopRow = 0;
                }
                else if (_hasPatternLoop)
                {
                    _hasPatternLoop = false;
                    _row = _patternLoopRow;
                }
                if (_row >= 64)
                {
                    _row = 0;
                    _pos = (_pos + 1) % _module.songlen;
                    _patternLoopRow = 0;
                }

                UpdateRow();
            }
            else
                UpdateEffects();

            _tick = (_tick + 1) % (_speed + _patternDelay * _speed);
            if (_tick == 0)
            {
                _row++;
                _patternDelay = 0;
            }

            for (track = 0; track < 4; track++)
            {
                SetChannelVolume(track, _track[track].vol);
                SetChannelPeriod(track, (short)(_track[track].period + _track[track].vibrato));
                if (_track[track].sample != 0)
                {
                    sample_t sample = _module.sample[_track[track].sample - 1];
                    SetChannelData(track,
                                   new ByteAccess(sample.data),
                                   sample.replen > 2 ? new ByteAccess(sample.data, sample.repeat) : null,
                                   sample.len,
                                   sample.replen);
                    SetChannelOffset(track, _track[track].offset);
                    _track[track].sample = 0;
                }
            }
        }

        private void DoPorta(int track)
        {
            if (_track[track].portaToNote != 0 && _track[track].portaToNoteSpeed != 0)
            {
                if (_track[track].period < _track[track].portaToNote)
                {
                    _track[track].period += _track[track].portaToNoteSpeed;
                    if (_track[track].period > _track[track].portaToNote)
                        _track[track].period = _track[track].portaToNote;
                }
                else if (_track[track].period > _track[track].portaToNote)
                {
                    _track[track].period -= _track[track].portaToNoteSpeed;
                    if (_track[track].period < _track[track].portaToNote)
                        _track[track].period = _track[track].portaToNote;
                }
            }
        }

        private void DoVibrato(int track)
        {
            _track[track].vibrato =
                    (_track[track].vibratoDepth * sinetable[_track[track].vibratoPos]) / 128;
            _track[track].vibratoPos += _track[track].vibratoSpeed;
            _track[track].vibratoPos %= 64;
        }

        private void DoVolSlide(int track, byte ex, byte ey)
        {
            int vol = _track[track].vol;
            if (ex == 0)
                vol -= ey;
            else if (ey == 0)
                vol += ex;

            if (vol < 0)
                vol = 0;
            else if (vol > 64)
                vol = 64;

            _track[track].vol = (byte)vol;
        }

        private void UpdateRow()
        {
            for (int track = 0; track < 4; track++)
            {
                _track[track].arpeggio = false;
                _track[track].vibrato = 0;
                _track[track].delaySampleTick = 0;
                note_t note = _module.pattern[_module.songpos[_pos], _row, track];

                int effect = note.effect >> 8;

                if (note.sample != 0)
                {
                    if (_track[track].sample != note.sample)
                    {
                        _track[track].vibratoPos = 0;
                    }
                    _track[track].sample = note.sample;
                    _track[track].lastSample = note.sample;
                    _track[track].finetune = _module.sample[note.sample - 1].finetune;
                    _track[track].vol = _module.sample[note.sample - 1].vol;
                }

                if (note.period != 0)
                {
                    if (effect != 3 && effect != 5)
                    {
                        if (_track[track].finetune != 0)
                            _track[track].period = (ushort)Module.NoteToPeriod(note.note, _track[track].finetune);
                        else
                            _track[track].period = note.period;

                        _track[track].offset = new Offset(0);
                        _track[track].sample = _track[track].lastSample;
                    }
                }

                var exy = (byte)(note.effect & 0xff);
                var ex = (byte)((note.effect >> 4) & 0xf);
                var ey = (byte)(note.effect & 0xf);

                int vol;
                switch (effect)
                {
                    case 0x0:
                        if (exy != 0)
                        {
                            _track[track].arpeggio = true;
                            if (note.period != 0)
                            {
                                _track[track].arpeggioNotes[0] = note.note;
                                _track[track].arpeggioNotes[1] = (byte)(note.note + ex);
                                _track[track].arpeggioNotes[2] = (byte)(note.note + ey);
                            }
                        }
                        break;
                    case 0x1:
                        break;
                    case 0x2:
                        break;
                    case 0x3:
                        if (note.period != 0)
                            _track[track].portaToNote = note.period;
                        if (exy != 0)
                            _track[track].portaToNoteSpeed = exy;
                        break;
                    case 0x4:
                        if (exy != 0)
                        {
                            _track[track].vibratoSpeed = ex;
                            _track[track].vibratoDepth = ey;
                        }
                        break;
                    case 0x5:
                        DoPorta(track);
                        DoVolSlide(track, ex, ey);
                        break;
                    case 0x6:
                        DoVibrato(track);
                        DoVolSlide(track, ex, ey);
                        break;
                    case 0x9: // Set sample offset
                        if (exy != 0)
                        {
                            _track[track].offset = new Offset(exy * 256);
                            SetChannelOffset(track, _track[track].offset);
                        }
                        break;
                    case 0xA:
                        break;
                    case 0xB:
                        _hasJumpToPattern = true;
                        _jumpToPattern = exy;
                        break;
                    case 0xC:
                        _track[track].vol = exy;
                        break;
                    case 0xD:
                        _hasPatternBreak = true;
                        _skipRow = ex * 10 + ey;
                        break;
                    case 0xE:
                        switch (ex)
                        {
                            case 0x0: // Switch filters off
                                break;
                            case 0x1: // Fine slide up
                                _track[track].period -= exy;
                                break;
                            case 0x2: // Fine slide down
                                _track[track].period += exy;
                                break;
                            case 0x5: // Set finetune
                                _track[track].finetune = ey;
                                _module.sample[_track[track].sample].finetune = ey;
                                if (note.period != 0)
                                {
                                    if (ey != 0)
                                        _track[track].period = (ushort)Module.NoteToPeriod(note.note, ey);
                                    else
                                        _track[track].period = note.period;
                                }
                                break;
                            case 0x6:
                                if (ey == 0)
                                {
                                    _patternLoopRow = _row;
                                }
                                else
                                {
                                    _patternLoopCount++;
                                    if (_patternLoopCount <= ey)
                                        _hasPatternLoop = true;
                                    else
                                        _patternLoopCount = 0;
                                }
                                break;
                            case 0x9:
                                break;  // Retrigger note
                            case 0xA: // Fine volume slide up
                                vol = _track[track].vol + ey;
                                if (vol > 64)
                                    vol = 64;
                                _track[track].vol = (byte)vol;
                                break;
                            case 0xB: // Fine volume slide down
                                vol = _track[track].vol - ey;
                                if (vol < 0)
                                    vol = 0;
                                _track[track].vol = (byte)vol;
                                break;
                            case 0xD: // Delay sample
                                _track[track].delaySampleTick = ey;
                                _track[track].delaySample = _track[track].sample;
                                _track[track].sample = 0;
                                _track[track].vol = 0;
                                break;
                            case 0xE: // Pattern delay
                                _patternDelay = ey;
                                break;
                            default:
                                Warning($"Unimplemented effect {note.effect:X}");
                                break;
                        }
                        break;

                    case 0xF:
                        if (exy < 0x20)
                        {
                            _speed = exy;
                        }
                        else
                        {
                            _bpm = exy;
                            InterruptFreq = (uint)(Rate / (_bpm * 0.4));
                        }
                        break;
                    default:
                        Warning($"Unimplemented effect {note.effect:X}");
                        break;
                }
            }
        }

        private void UpdateEffects()
        {
            for (int track = 0; track < 4; track++)
            {
                _track[track].vibrato = 0;

                note_t note =
                    _module.pattern[_module.songpos[_pos], _row, track];

                int effect = note.effect >> 8;

                int exy = note.effect & 0xff;
                int ex = (note.effect >> 4) & 0xf;
                int ey = (note.effect) & 0xf;

                switch (effect)
                {
                    case 0x0:
                        if (exy != 0)
                        {
                            int idx = (_tick == 1) ? 0 : (_tick % 3);
                            _track[track].period = (ushort)Module.NoteToPeriod(_track[track].arpeggioNotes[idx],
                                        _track[track].finetune);
                        }
                        break;
                    case 0x1:
                        _track[track].period = (ushort)(_track[track].period - exy);
                        break;
                    case 0x2:
                        _track[track].period = (ushort)(_track[track].period + exy);
                        break;
                    case 0x3:
                        DoPorta(track);
                        break;
                    case 0x4:
                        DoVibrato(track);
                        break;
                    case 0x5:
                        DoPorta(track);
                        DoVolSlide(track, (byte)ex, (byte)ey);
                        break;
                    case 0x6:
                        DoVibrato(track);
                        DoVolSlide(track, (byte)ex, (byte)ey);
                        break;
                    case 0xA:
                        DoVolSlide(track, (byte)ex, (byte)ey);
                        break;
                    case 0xE:
                        switch (ex)
                        {
                            case 0x6:
                                break;  // Pattern loop
                            case 0x9:   // Retrigger note
                                if (ey != 0 && (_tick % ey) == 0)
                                    _track[track].offset = new Offset(0);
                                break;
                            case 0xD: // Delay sample
                                if (_tick == _track[track].delaySampleTick)
                                {
                                    _track[track].sample = _track[track].delaySample;
                                    _track[track].offset = new Offset(0);
                                    if (_track[track].sample != 0)
                                        _track[track].vol = _module.sample[_track[track].sample - 1].vol;
                                }
                                break;
                        }
                        break;
                }
            }
        }

        private static readonly short[] sinetable = {
             0,   24,   49,   74,   97,  120,  141,  161,
            180,  197,  212,  224,  235,  244,  250,  253,
            255,  253,  250,  244,  235,  224,  212,  197,
            180,  161,  141,  120,   97,   74,   49,   24,
             0,  -24,  -49,  -74,  -97, -120, -141, -161,
            -180, -197, -212, -224, -235, -244, -250, -253,
            -255, -253, -250, -244, -235, -224, -212, -197,
            -180, -161, -141, -120,  -97,  -74,  -49,  -24
        };
    }
}
