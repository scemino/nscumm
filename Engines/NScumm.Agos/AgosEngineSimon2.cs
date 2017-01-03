﻿//
//  AgosEngineSimon2.cs
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
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    internal class AgosEngineSimon2 : AgosEngineSimon1
    {
        private Dictionary<int, Action> _opcodesSimon2;
        private int _marks;

        private static readonly GameSpecificSettings Simon2Settings =
            new GameSpecificSettings
            {
                base_filename = string.Empty, // base_filename
                restore_filename = string.Empty, // restore_filename
                tbl_filename = string.Empty, // tbl_filename
                effects_filename = string.Empty, // effects_filename
                speech_filename = "SIMON2" // speech_filename
            };

        private static readonly byte[] convertVerbID =
        {
            0, 1, 5, 11, 8, 7, 10, 3, 2
        };

        private static readonly string[] russian_verb_prep_names =
        {
            "", "", "", "",
            "", "", "", "s yfn?",
            "", "", "", "_onu ?"
        };

        private static readonly string[] hebrew_verb_prep_names =
        {
            "", "", "", "",
            "", "", "", "RM ND ?",
            "", "", "", "LNI ?"
        };

        private static readonly string[] spanish_verb_prep_names =
        {
            "", "", "", "",
            "", "", "", "^con qu/?",
            "", "", "", "^a qui/n?"
        };

        private static readonly string[] italian_verb_prep_names =
        {
            "", "", "", "",
            "", "", "", "con cosa ?",
            "", "", "", "a chi ?"
        };

        private static readonly string[] french_verb_prep_names =
        {
            "", "", "", "",
            "", "", "", "avec quoi ?",
            "", "", "", "; qui ?"
        };

        private static readonly string[] german_verb_prep_names =
        {
            "", "", "", "",
            "", "", "", "mit was ?",
            "", "", "", "zu wem ?"
        };

        private static readonly string[] english_verb_prep_names =
        {
            "", "", "", "",
            "", "", "", "with what ?",
            "", "", "", "to whom ?"
        };

        private static readonly string[] czech_verb_prep_names =
        {
            "", "", "", "",
            "", "", "", "s cim ?",
            "", "", "", "komu ?"
        };

        private static string[] russian_verb_names =
        {
            "Ietj _",
            "Qnotrft< pa",
            "Nt_r[t<",
            "Ecjdat<",
            "Q=fst<",
            "C^]t<",
            "Ha_r[t<",
            "Isqom<^ocat<",
            "Docorjt<",
            "Qp]t<",
            "Neft<",
            "Eat<"
        };

        private static string[] hebrew_verb_names =
        {
            "LJ @L",
            "DQZKL RL",
            "TZG",
            "DFF",
            "@KEL",
            "DXM",
            "QBEX",
            "DYZNY",
            "CAX @L",
            "DQX",
            "LAY",
            "ZO"
        };

        private static string[] spanish_verb_names =
        {
            "Caminar",
            "Mirar",
            "Abrir",
            "Mover",
            "Consumir",
            "Coger",
            "Cerrar",
            "Usar",
            "Hablar",
            "Quitar",
            "Llevar",
            "Dar"
        };

        private static string[] italian_verb_names =
        {
            "Vai verso",
            "Osserva",
            "Apri",
            "Sposta",
            "Mangia",
            "Raccogli",
            "Chiudi",
            "Usa",
            "Parla a",
            "Togli",
            "Indossa",
            "Dai"
        };

        private static string[] french_verb_names =
        {
            "Aller vers",
            "Regarder",
            "Ouvrir",
            "D/placer",
            "Consommer",
            "Prendre",
            "Fermer",
            "Utiliser",
            "Parler ;",
            "Enlever",
            "Mettre",
            "Donner"
        };

        private static string[] german_verb_names =
        {
            "Gehe zu",
            "Schau an",
            ";ffne",
            "Bewege",
            "Verzehre",
            "Nimm",
            "Schlie+e",
            "Benutze",
            "Rede mit",
            "Entferne",
            "Trage",
            "Gib"
        };

        private static string[] english_verb_names =
        {
            "Walk to",
            "Look at",
            "Open",
            "Move",
            "Consume",
            "Pick up",
            "Close",
            "Use",
            "Talk to",
            "Remove",
            "Wear",
            "Give"
        };

        private static string[] czech_verb_names =
        {
            "Jit",
            "Podivat se",
            "Otevrit",
            "Pohnout s",
            "Snist",
            "Sebrat",
            "Zavrit",
            "Pouzit",
            "Mluvit s",
            "Odstranit",
            "Oblect",
            "Dat"
        };

        public AgosEngineSimon2(ISystem system, GameSettings settings, AgosGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected override void SetupOpcodes()
        {
            _opcodesSimon2 = new Dictionary<int, Action>
            {
                {1, o_at},
                {2, o_notAt},
                {5, o_carried},
                {6, o_notCarried},
                {7, o_isAt},
                {11, o_zero},
                {12, o_notZero},
                {13, o_eq},
                {14, o_notEq},
                {15, o_gt},
                {16, o_lt},
                {17, o_eqf},
                {18, o_notEqf},
                {19, o_ltf},
                {20, o_gtf},
                {23, o_chance},
                {25, o_isRoom},
                {26, o_isObject},
                {27, o_state},
                {28, o_oflag},
                {31, o_destroy},
                {33, o_place},
                {36, o_copyff},
                {41, o_clear},
                {42, o_let},
                {43, o_add},
                {44, o_sub},
                {45, o_addf},
                {46, o_subf},
                {47, o_mul},
                {48, o_div},
                {49, o_mulf},
                {50, o_divf},
                {51, o_mod},
                {52, o_modf},
                {53, o_random},
                {55, o_goto},
                {56, o_oset},
                {57, o_oclear},
                {58, o_putBy},
                {59, o_inc},
                {60, o_dec},
                {61, o_setState},
                {62, o_print},
                {63, o_message},
                {64, o_msg},
                {65, oww_addTextBox},
                {66, oww_setShortText},
                {67, oww_setLongText},
                {68, o_end},
                {69, o_done},
                {70, os2_printLongText},
                {71, o_process},
                {76, o_when},
                {77, o_if1},
                {78, o_if2},
                {79, o_isCalled},
                {80, o_is},
                {82, o_debug},
                {83, os2_rescan},
                {87, o_comment},
                {88, o_haltAnimation},
                {89, o_restartAnimation},
                {90, o_getParent},
                {91, o_getNext},
                {92, o_getChildren},
                {96, o_picture},
                {97, o_loadZone},
                {98, os2_animate},
                {99, os2_stopAnimate},
                {100, o_killAnimate},
                {101, o_defWindow},
                {102, o_window},
                {103, o_cls},
                {104, o_closeWindow},
                {107, o_addBox},
                {108, o_delBox},
                {109, o_enableBox},
                {110, o_disableBox},
                {111, o_moveBox},
                {114, o_doIcons},
                {115, o_isClass},
                {116, o_setClass},
                {117, o_unsetClass},
                {119, o_waitSync},
                {120, o_sync},
                {121, o_defObj},
                {125, o_here},
                {126, o_doClassIcons},
                {127, os2_playTune},
                {130, o_setAdjNoun},
                {132, o_saveUserGame},
                {133, o_loadUserGame},
                {135, os1_pauseGame},
                {136, o_copysf},
                {137, o_restoreIcons},
                {138, o_freezeZones},
                {139, o_placeNoIcons},
                {140, o_clearTimers},
                {141, o_setDollar},
                {142, o_isBox},
                {143, oe2_doTable},
                {151, oe2_storeItem},
                {152, oe2_getItem},
                {153, oe2_bSet},
                {154, oe2_bClear},
                {155, oe2_bZero},
                {156, oe2_bNotZero},
                {157, oe2_getOValue},
                {158, oe2_setOValue},
                {160, oe2_ink},
                {161, os1_screenTextBox},
                {162, os1_screenTextMsg},
                {163, os1_playEffect},
                {164, oe2_getDollar2},
                {165, oe2_isAdjNoun},
                {166, oe2_b2Set},
                {167, oe2_b2Clear},
                {168, oe2_b2Zero},
                {169, oe2_b2NotZero},
                {175, oww_lockZones},
                {176, oww_unlockZones},
                {177, os2_screenTextPObj},
                {178, os1_getPathPosn},
                {179, os1_scnTxtLongText},
                {180, os2_mouseOn},
                {181, os2_mouseOff},
                {184, os1_unloadZone},
                {186, os1_unfreezeZones},
                {188, os2_isShortText},
                {189, os2_clearMarks},
                {190, os2_waitMark},
            };
            _numOpcodes = 191;
        }

        protected override void ExecuteOpcode(int opcode)
        {
            _opcodesSimon2[opcode]();
        }

        protected override void SetupVideoOpcodes(Action[] op)
        {
            base.SetupVideoOpcodes(op);

            op[56] = vc56_delayLong;
            op[58] = vc58_changePriority;
            op[59] = vc59_stopAnimations;
            op[64] = vc64_ifSpeech;
            op[65] = vc65_slowFadeIn;
            op[66] = vc66_ifEqual;
            op[67] = vc67_ifLE;
            op[68] = vc68_ifGE;
            op[69] = vc69_playSeq;
            op[70] = vc70_joinSeq;
            op[71] = vc71_ifSeqWaiting;
            op[72] = vc72_segue;
            op[73] = vc73_setMark;
            op[74] = vc74_clearMark;
        }

        protected override void SetupGame()
        {
            gss = Simon2Settings;
            _tableIndexBase = 1580 / 4;
            _textIndexBase = 1500 / 4;
            _numVideoOpcodes = 75;
#if __DS__
	_vgaMemSize = 1300000;
#else
            _vgaMemSize = 2000000;
#endif
            _itemMemSize = 20000;
            _tableMemSize = 100000;
            // Check whether to use MT-32 MIDI tracks in Simon the Sorcerer 2
            if (GameType == SIMONGameType.GType_SIMON2 && _midi.HasNativeMt32)
                _musicIndexBase = (1128 + 612) / 4;
            else
                _musicIndexBase = 1128 / 4;
            _soundIndexBase = 1660 / 4;
            _frameCount = 1;
            _vgaBaseDelay = 1;
            _vgaPeriod = 45;
            _numBitArray1 = 16;
            _numBitArray2 = 16;
            _numItemStore = 10;
            _numTextBoxes = 20;
            _numVars = 255;

            _numMusic = 93;
            _numSFX = 222;
            _numSpeech = 11997;
            _numZone = 140;

            SetupGameCore();
        }

        protected override void DrawIcon(WindowBlock window, int icon, int x, int y)
        {
            _videoLockOut |= 0x8000;

            LockScreen(screen =>
            {
                var dst = screen.Pixels;

                dst += 110;
                dst += x;
                dst += (y + window.y) * screen.Pitch;

                BytePtr src = _iconFilePtr;
                src.Offset += src.ToUInt16(icon * 4 + 0);
                DecompressIcon(dst, src, 20, 10, 224, screen.Pitch);

                src = _iconFilePtr;
                src.Offset += src.ToUInt16(icon * 4 + 2);
                DecompressIcon(dst, src, 20, 10, 208, screen.Pitch);
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected override int SetupIconHitArea(WindowBlock window, uint num, int x, int y, Item itemPtr)
        {
            var h = FindEmptyHitArea();
            var ha = h.Value;
            ha.x = (ushort) (x + 110);
            ha.y = (ushort) (window.y + y);
            ha.itemPtr = itemPtr;
            ha.width = 20;
            ha.height = 20;
            ha.flags = BoxFlags.kBFDragBox | BoxFlags.kBFBoxInUse | BoxFlags.kBFBoxItem;
            ha.id = 0x7FFD;
            ha.priority = 100;
            ha.verb = 208;

            return h.Offset;
        }

        protected override void AddArrows(WindowBlock window, byte num)
        {
            var h = FindEmptyHitArea();
            _scrollUpHitArea = (ushort) h.Offset;

            var ha = h.Value;
            ha.x = 81;
            ha.y = 158;
            ha.width = 12;
            ha.height = 26;
            ha.flags = BoxFlags.kBFBoxInUse | BoxFlags.kBFNoTouchName;
            ha.id = 0x7FFB;
            ha.priority = 100;
            ha.window = window;
            ha.verb = 1;

            h = FindEmptyHitArea();
            _scrollDownHitArea = (ushort) h.Offset;

            ha = h.Value;
            ha.x = 227;
            ha.y = 162;
            ha.width = 12;
            ha.height = 26;
            ha.flags = BoxFlags.kBFBoxInUse | BoxFlags.kBFNoTouchName;
            ha.id = 0x7FFC;
            ha.priority = 100;
            ha.window = window;
            ha.verb = 1;
        }

        protected override string GenSaveName(int slot)
        {
            return $"simon2.{slot:D3}";
        }

        protected override void ClearName()
        {
            if (GetBitFlag(79))
            {
                SendSync(202);
                _lastNameOn = null;
                return;
            }

            if (_currentVerbBox == _lastVerbOn)
                return;

            ResetNameWindow();
            _lastVerbOn = _currentVerbBox;

            if (_currentVerbBox != null && !(_currentVerbBox.flags.HasFlag(BoxFlags.kBFBoxDead)))
                PrintVerbOf(_currentVerbBox.id);
        }

        protected override void PlaySpeech(ushort speechId, ushort vgaSpriteId)
        {
            if (speechId == 0xFFFF)
            {
                if (_subtitles)
                    return;
                if (!GetBitFlag(14) && !GetBitFlag(28))
                {
                    SetBitFlag(14, true);
                    _variableArray[100] = 5;
                    Animate(4, 1, 30, 0, 0, 0);
                    WaitForSync(130);
                }
                _skipVgaWait = true;
            }
            else
            {
                if (GameType == SIMONGameType.GType_SIMON2 && _subtitles && _language != Language.HE_ISR)
                {
                    LoadVoice(speechId);
                    return;
                }

                if (_subtitles && _scriptVar2)
                {
                    Animate(4, 2, 5, 0, 0, 0);
                    WaitForSync(205);
                    StopAnimateSimon2(2, 5);
                }

                StopAnimateSimon2(2, (ushort) (vgaSpriteId + 2));
                LoadVoice(speechId);
                Animate(4, 2, (ushort) (vgaSpriteId + 2), 0, 0, 0);
            }
        }

        private void PrintVerbOf(int hitarea_id)
        {
            string txt;

            hitarea_id -= 101;
            if (GameType == SIMONGameType.GType_SIMON2)
                hitarea_id = convertVerbID[hitarea_id];

            if (_showPreposition)
            {
                string[] verb_prep_names;
                switch (_language)
                {
                    case Language.RU_RUS:
                        verb_prep_names = russian_verb_prep_names;
                        break;
                    case Language.HE_ISR:
                        verb_prep_names = hebrew_verb_prep_names;
                        break;
                    case Language.ES_ESP:
                        verb_prep_names = spanish_verb_prep_names;
                        break;
                    case Language.IT_ITA:
                        verb_prep_names = italian_verb_prep_names;
                        break;
                    case Language.FR_FRA:
                        verb_prep_names = french_verb_prep_names;
                        break;
                    case Language.DE_DEU:
                        verb_prep_names = german_verb_prep_names;
                        break;
                    case Language.CZ_CZE:
                        verb_prep_names = czech_verb_prep_names;
                        break;
                    default:
                        verb_prep_names = english_verb_prep_names;
                        break;
                }
                txt = verb_prep_names[hitarea_id];
            }
            else
            {
                string[] verb_names;
                switch (_language)
                {
                    case Language.RU_RUS:
                        verb_names = russian_verb_names;
                        break;
                    case Language.HE_ISR:
                        verb_names = hebrew_verb_names;
                        break;
                    case Language.ES_ESP:
                        verb_names = spanish_verb_names;
                        break;
                    case Language.IT_ITA:
                        verb_names = italian_verb_names;
                        break;
                    case Language.FR_FRA:
                        verb_names = french_verb_names;
                        break;
                    case Language.DE_DEU:
                        verb_names = german_verb_names;
                        break;
                    case Language.CZ_CZE:
                        verb_names = czech_verb_names;
                        break;
                    default:
                        verb_names = english_verb_names;
                        break;
                }
                txt = verb_names[hitarea_id];
            }
            ShowActionString(txt);
        }

        private void vc56_delayLong()
        {
            ushort num = (ushort) (VcReadVarOrWord() * _frameCount);

            AddVgaEvent((ushort) (num + _vgaBaseDelay), EventType.ANIMATE_EVENT, _vcPtr, _vgaCurSpriteId, _vgaCurZoneNum);
            _vcPtr = _vcGetOutOfCode;
        }

        private void vc58_changePriority()
        {
            ushort sprite = _vgaCurSpriteId;
            ushort file = _vgaCurZoneNum;

            _vgaCurZoneNum = (ushort) VcReadNextWord();
            _vgaCurSpriteId = (ushort) VcReadNextWord();

            ushort tmp = To16Wrapper(VcReadNextWord());

            var vcPtrOrg = _vcPtr;
            _vcPtr = BitConverter.GetBytes(tmp);
            vc23_setPriority();

            _vcPtr = vcPtrOrg;
            _vgaCurSpriteId = sprite;
            _vgaCurZoneNum = file;
        }

        private void vc59_stopAnimations()
        {
            ushort file = (ushort) VcReadNextWord();
            ushort start = (ushort) VcReadNextWord();
            ushort end = (ushort) (VcReadNextWord() + 1);

            do
            {
                VcStopAnimation(file, start);
            } while (++start != end);
        }

        private void vc64_ifSpeech()
        {
            if ((GameType == SIMONGameType.GType_SIMON2 && _subtitles && _language != Language.HE_ISR) ||
                !_sound.IsVoiceActive)
            {
                VcSkipNextInstruction();
            }
        }

        private void vc65_slowFadeIn()
        {
            _fastFadeInFlag = 624;
            _fastFadeCount = 208;
            if (_windowNum != 4)
            {
                _fastFadeInFlag = 768;
                _fastFadeCount = 256;
            }
            _fastFadeInFlag |= 0x8000;
            _fastFadeOutFlag = false;
        }

        private void vc66_ifEqual()
        {
            ushort a = (ushort) VcReadNextWord();
            ushort b = (ushort) VcReadNextWord();

            if (VcReadVar(a) != VcReadVar(b))
                VcSkipNextInstruction();
        }

        private void vc67_ifLE()
        {
            ushort a = (ushort) VcReadNextWord();
            ushort b = (ushort) VcReadNextWord();

            if (VcReadVar(a) >= VcReadVar(b))
                VcSkipNextInstruction();
        }

        private void vc68_ifGE()
        {
            ushort a = (ushort) VcReadNextWord();
            ushort b = (ushort) VcReadNextWord();

            if (VcReadVar(a) <= VcReadVar(b))
                VcSkipNextInstruction();
        }

        private void vc69_playSeq()
        {
            short track = (short) VcReadNextWord();
            short loop = (short) VcReadNextWord();

            // Jamieson630:
            // This is a "play track". The original
            // design stored the track to play if one was
            // already in progress, so that the next time a
            // "fill MIDI stream" event occurred, the MIDI
            // player would find the change and switch
            // tracks. We use a different architecture that
            // allows for an immediate response here, but
            // we'll simulate the variable changes so other
            // scripts don't get thrown off.
            // NOTE: This opcode looks very similar in function
            // to vc72(), except that vc72() may allow for
            // specifying a non-valid track number (999 or -1)
            // as a means of stopping what music is currently
            // playing.
            _midi.SetLoop(loop != 0);
            _midi.StartTrack(track);
        }

        private void vc70_joinSeq()
        {
            // Simon2
            ushort track = (ushort) VcReadNextWord();
            ushort loop = (ushort) VcReadNextWord();

            // Jamieson630:
            // This sets the "on end of track" action.
            // It specifies whether to loop the current
            // track and, if not, whether to switch to
            // a different track upon completion.
            if (track != 0xFFFF && track != 999)
                _midi.QueueTrack(track, loop != 0);
            else
                _midi.SetLoop(loop != 0);
        }

        private void vc71_ifSeqWaiting()
        {
            // Jamieson630:
            // This command skips the next instruction
            // unless (1) there is a track playing, AND
            // (2) there is a track queued to play after it.
            if (!_midi.IsPlaying(true))
                VcSkipNextInstruction();
        }

        private void vc72_segue()
        {
            // Jamieson630:
            // This is a "play or stop track". Note that
            // this opcode looks very similar in function
            // to vc69(), except that this opcode may allow
            // for specifying a track of 999 or -1 in order to
            // stop the music. We'll code it that way for now.

            // NOTE: It's possible that when "stopping" a track,
            // we're supposed to just go on to the next queued
            // track, if any. Must find out if there is ANY
            // case where this is used to stop a track in the
            // first place.

            short track = (short) VcReadNextWord();
            short loop = (short) VcReadNextWord();

            if (track == -1 || track == 999)
            {
                StopMusic();
            }
            else
            {
                _midi.SetLoop(loop != 0);
                _midi.StartTrack(track);
            }
        }

        private void vc73_setMark()
        {
            _marks |= (1 << (int) VcReadNextWord());
        }

        private void vc74_clearMark()
        {
            _marks &= ~(1 << (int) VcReadNextWord());
        }

        protected override void ClearVideoWindow(ushort num, ushort color)
        {
            var vlut = new Ptr<ushort>(VideoWindows, num * 4);

            ushort xoffs = (ushort) (vlut[0] * 16);
            ushort yoffs = vlut[1];
            ushort dstWidth = (ushort) (VideoWindows[18] * 16);
            // TODO: Is there any known connection between dstWidth and the pitch
            // of the _window4BackScn Surface? If so, we might be able to pass
            // yoffs as proper y parameter to getBasePtr.
            var dst = _window4BackScn.GetBasePtr(xoffs, 0) + yoffs * dstWidth;

            SetMoveRect(0, 0, (ushort) (vlut[2] * 16), vlut[3]);

            for (var h = 0; h < vlut[3]; h++)
            {
                dst.Data.Set(dst.Offset, (byte) color, vlut[2] * 16);
                dst += dstWidth;
            }

            _window4Flag = 1;
        }

        private void WaitForMark(int i)
        {
            _exitCutscene = false;
            while ((_marks & (1 << i)) == 0)
            {
                if (_exitCutscene)
                {
                    if (GameType == SIMONGameType.GType_PP)
                    {
                        if (_picture8600)
                            break;
                    }
                    else
                    {
                        if (GetBitFlag(9))
                        {
                            EndCutscene();
                            break;
                        }
                    }
                }
                else
                {
                    ProcessSpecialKeys();
                }

                Delay(10);
            }
        }

        protected void os2_waitMark()
        {
            // 190
            int i = (int) GetVarOrByte();
            if ((_marks & (1 << i)) == 0)
                WaitForMark(i);
        }

        protected void os2_clearMarks()
        {
            // 189: clear_op189_flag
            _marks = 0;
        }

        protected void os2_isShortText()
        {
            // 188: string2 is
            uint i = GetVarOrByte();
            uint str = GetNextStringID();
            SetScriptCondition(str < _numTextBoxes && _shortText[i] == str);
        }

        private void os2_mouseOff()
        {
            // 181: force mouseOff
            ScriptMouseOff();
            ChangeWindow(1);
            ShowMessageFormat("\xC");
        }

        private void os2_mouseOn()
        {
            // 180: force mouseOn
            if (GameType == SIMONGameType.GType_SIMON2 && GetBitFlag(79))
            {
                _mouseCursor = 0;
            }
            _mouseHideCount = 0;
        }

        private void os2_screenTextPObj()
        {
            // 177: inventory descriptions
            uint vgaSpriteId = GetVarOrByte();
            uint color = GetVarOrByte();

            var subObject = (SubObject) FindChildOfType(GetNextItemPtr(), ChildType.kObjectType);
            if (Features.HasFlag(GameFeatures.GF_TALKIE))
            {
                if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFVoice))
                {
                    uint speechId =
                        (uint)
                        subObject.objectFlagValue[GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFVoice)];

                    if (subObject.objectFlags.HasFlag(SubObjectFlags.kOFNumber))
                    {
                        uint speechIdOffs =
                            (uint)
                            subObject.objectFlagValue[GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFNumber)];

                        if (speechId == 116)
                            speechId = speechIdOffs + 115;
                        if (speechId == 92)
                            speechId = speechIdOffs + 98;
                        if (speechId == 99)
                            speechId = 9;
                        if (speechId == 97)
                        {
                            switch (speechIdOffs)
                            {
                                case 12:
                                    speechId = 109;
                                    break;
                                case 14:
                                    speechId = 108;
                                    break;
                                case 18:
                                    speechId = 107;
                                    break;
                                case 20:
                                    speechId = 106;
                                    break;
                                case 22:
                                    speechId = 105;
                                    break;
                                case 28:
                                    speechId = 104;
                                    break;
                                case 90:
                                    speechId = 103;
                                    break;
                                case 92:
                                    speechId = 102;
                                    break;
                                case 100:
                                    speechId = 51;
                                    break;
                                default:
                                    Error("os2_screenTextPObj: invalid case {0}", speechIdOffs);
                                    break;
                            }
                        }
                    }

                    if (_speech)
                        PlaySpeech((ushort) speechId, (ushort) vgaSpriteId);
                }
            }

            if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFText) && _subtitles)
            {
                var stringPtr = GetStringPtrById((ushort) subObject.objectFlagValue[0]);
                var tl = GetTextLocation(vgaSpriteId);
                string buf;
                int j, k;

                if (subObject.objectFlags.HasFlag(SubObjectFlags.kOFNumber))
                {
                    if (_language == Language.HE_ISR)
                    {
                        j = subObject.objectFlagValue[GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFNumber)];
                        k = (j % 10) * 10;
                        k += j / 10;
                        if ((j % 10) == 0)
                            buf = $"0{k}{stringPtr}";
                        else
                            buf = $"{k}{stringPtr}";
                    }
                    else
                    {
                        buf = string.Format("{0}{1}",
                            subObject.objectFlagValue[GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFNumber)],
                            stringPtr);
                    }
                    stringPtr = buf;
                }
                if (stringPtr != null)
                    PrintScreenText(vgaSpriteId, color, stringPtr, tl.x, tl.y, tl.width);
            }
        }

        protected void os2_stopAnimate()
        {
            // 99: kill sprite
            ushort a = (ushort) GetVarOrWord();
            ushort b = (ushort) GetVarOrWord();
            StopAnimateSimon2(a, b);
        }

        protected void os2_animate()
        {
            // 98: start vga
            ushort zoneNum = (ushort) GetVarOrWord();
            ushort vgaSpriteId = (ushort) GetVarOrWord();
            ushort windowNum = (ushort) GetVarOrByte();
            short x = (short) GetVarOrWord();
            short y = (short) GetVarOrWord();
            ushort palette = (ushort) (GetVarOrWord() & 15);

            _videoLockOut |= 0x40;
            Animate(windowNum, zoneNum, vgaSpriteId, x, y, palette);
            _videoLockOut = (ushort) (_videoLockOut & ~0x40);
        }

        protected void os2_rescan()
        {
            // 83: restart subroutine
            if (_exitCutscene)
            {
                if (GetBitFlag(9))
                {
                    EndCutscene();
                }
            }
            else
            {
                ProcessSpecialKeys();
            }

            SetScriptReturn(-10);
        }

        private void os2_printLongText()
        {
            // 70: show string from array
            var str = GetStringPtrById(_longText[GetVarOrByte()]);
            WriteVariable(51, (ushort) (str.Length / 53 * 8 + 8));
            ShowMessageFormat("{0}\n", str);
        }

        private void os2_playTune()
        {
            // 127: deals with music
            int music = (int) GetVarOrWord();
            int track = (int) GetVarOrWord();
            int loop = (int) GetVarOrByte();

            // Jamieson630:
            // This appears to be a "load or play music" command.
            // The music resource is specified, and optionally
            // a track as well. Normally we see two calls being
            // made, one to load the resource and another to
            // actually start a track (so the resource is
            // effectively preloaded so there's no latency when
            // starting playback).

            _midi.SetLoop(loop != 0);
            if (_lastMusicPlayed != music)
                _nextMusicToPlay = (short) music;
            else
                _midi.StartTrack(track);
        }
    }
}