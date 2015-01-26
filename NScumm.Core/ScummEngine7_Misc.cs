﻿//
//  ScummEngine7_Misc.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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
using System.Diagnostics;
using NScumm.Core.IO;
using NScumm.Core.Graphics;

namespace NScumm.Core
{
    partial class ScummEngine7
    {
        int _smushFrameRate;
        bool _disableFadeInEffect;
        byte[] _lastStringTag = new byte[12 + 1];
        int _subtitleQueuePos;
        SubtitleText[] _subtitleQueue = new SubtitleText[20];

        [OpCode(0xc9)]
        protected override void KernelSetFunctions()
        {
            var args = GetStackList(30);

            switch (args[0])
            {
                case 4:
                    GrabCursor(args[1], args[2], args[3], args[4]);
                    break;
                case 6:
                    {
                        // SMUSH movie playback
                        if (args[1] == 0 && !_skipVideo)
                        {
                            var videoname = System.Text.Encoding.ASCII.GetString(GetStringAddressVar(VariableVideoName));
                            // TODO: vs
                            // Correct incorrect smush filename in Macintosh FT demo
//                            if ((_game.id == GID_FT) && (_game.features & GF_DEMO) && (_game.platform == Common::kPlatformMacintosh) &&
//                                (!strcmp(videoname, "jumpgorge.san")))
//                                _splayer.play("jumpgorg.san", _smushFrameRate);
                            // WORKAROUND: A faster frame rate is required, to keep audio/video in sync in this video
//                            else 
                            if (Game.GameId == GameId.Dig && videoname == "sq3.san")
                                SmushPlayer.Play(videoname, 14);
                            else
                                SmushPlayer.Play(videoname, _smushFrameRate);

                            if (Game.GameId == GameId.Dig)
                            {
                                _disableFadeInEffect = true;
                            }
                        }
                        else if (Game.GameId == GameId.FullThrottle && !_skipVideo)
                        {
                            var insaneVarNum = (Game.Features.HasFlag(GameFeatures.Demo)/* && (Game.Platform == Common::kPlatformDOS)*/)
                                ? 232 : 233;

                            Insane.SetSmushParams(_smushFrameRate);
                            Insane.RunScene(insaneVarNum);
                        }
                    }
                    break;
                case 12:
                    SetCursorFromImg(args[1], -1, args[2]);
                    break;
                case 13:
                    Actors[args[1]].RemapActorPalette(args[2], args[3], args[4], -1);
                    break;
                case 14:
                    Actors[args[1]].RemapActorPalette(args[2], args[3], args[4], args[5]);
                    break;
                case 15:
                    _smushFrameRate = args[1];
                    break;
                case 16:
                case 17:
                    EnqueueText(GetStringAddressVar(VariableString2Draw), args[3], args[4], (byte)args[2], (byte)args[1], (args[0] == 16));
                    break;
                case 107:
                    Actors[args[1]].SetScale(args[2], -1);
                    break;
                case 108:
                    SetShadowPalette(args[1], args[2], args[3], args[4], args[5], args[6]);
                    break;
                case 109:
                    SetShadowPalette(0, args[1], args[2], args[3], args[4], args[5]);
                    break;
                case 117:
                    FreezeScripts(2);
                    break;
                case 118:
                    EnqueueObject(args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], 3);
                    break;
                case 119:
                    EnqueueObject(args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], 0);
                    break;
                case 124:
                    _saveSound = args[1] != 0;
                    break;
                default:
                    throw new NotSupportedException(string.Format("KernelSetFunctions: default case {0} (param count {1})", args[0], args.Length));
            }
        }

        protected internal override void HandleSound()
        {
            base.HandleSound();
//            if (_imuseDigital) {
//                _imuseDigital.flushTracks();
//                // In CoMI and the Dig the full (non-demo) version invoke IMuseDigital::refreshScripts
//                if ((_game.id == GID_DIG || _game.id == GID_CMI) && !(_game.features & GF_DEMO))
//                    _imuseDigital.refreshScripts();
//            }
//            if (_smixer) {
//                _smixer.flush();
//            }
        }

        protected internal override void ProcessInput()
        {
            var cutsceneExitKeyEnabled = (!VariableCutSceneExitKey.HasValue || Variables[VariableCutSceneExitKey.Value] != 0);

            // VAR_VERSION_KEY (usually ctrl-v) is used in COMI, Dig and FT to trigger
            // a version dialog, unless VAR_VERSION_KEY is set to 0. However, the COMI
            // version string is hard coded in the engine, hence we don't invoke
            // versionDialog for it. Dig/FT version strings are partly hard coded, too.
//            if (Game.Id != GID_CMI && 0 != Variables[VariableVersionKey] &&
//                _inputManager.IsKeyDown(KeyCode.V) && _inputManager.IsKeyDown(KeyCode.Control)) {
//                VersionDialog();
//
//            } else 
            if (cutsceneExitKeyEnabled && _inputManager.IsKeyDown(KeyCode.Escape))
            {
                // Skip cutscene (or active SMUSH video).
                if (SmushActive)
                {
                    if (Game.GameId == GameId.FullThrottle)
                    {
                        Insane.EscapeKeyHandler();
                    }
                    else
                        SmushVideoShouldFinish = true;
                    _skipVideo = true;
                }
                else
                {
                    AbortCutscene();
                }

                mouseAndKeyboardStat = KeyCode.Escape;
            }
            else
            {
                base.ProcessInput();
            }

            if (_skipVideo && !SmushActive)
            {
                AbortCutscene();
                mouseAndKeyboardStat = KeyCode.Escape;
                _skipVideo = false;
            }
        }

        protected override void ActorTalk(byte[] msg)
        {
            var stringWrap = false;

            ConvertMessageToString(msg, _charsetBuffer, 0);

            // Play associated speech, if any
            PlaySpeech(_lastStringTag);

            if (Game.GameId == GameId.Dig /*|| Game.GameId == GID_CMI*/)
            {
                if (Variables[VariableHaveMessage.Value] != 0)
                    StopTalk();
            }
            else
            {
                if (!_keepText)
                    StopTalk();
            }
            if (_actorToPrintStrFor == 0xFF)
            {
                TalkingActor = 0xFF;
                _charsetColor = (byte)String[0].Color;
            }
            else
            {
                var a = Actors[_actorToPrintStrFor];
                TalkingActor = a.Number;
                if (!String[0].NoTalkAnim)
                {
                    a.RunActorTalkScript(a.TalkStartFrame);
                }
                _charsetColor = a.TalkColor;
            }

            _charsetBufPos = 0;
            _talkDelay = 0;
            _haveMsg = 1;
            if (Game.GameId == GameId.FullThrottle)
                Variables[VariableHaveMessage.Value] = 0xFF;
            _haveActorSpeechMsg = (Game.GameId == GameId.FullThrottle) ? true : (!Sound.IsSoundRunning(Sound.TalkSoundID));
            if (Game.GameId == GameId.Dig /*|| Game.GameId == GameId.GID_CMI*/)
            {
                stringWrap = String[0].Wrapping;
                String[0].Wrapping = true;
            }
            Charset();
            if (Game.GameId == GameId.Dig /*|| Game.GameId == GameId.GID_CMI*/)
            {
                if (Game.Version == 8)
                    Variables[VariableHaveMessage.Value] = (String[0].NoTalkAnim) ? 2 : 1;
                else
                    Variables[VariableHaveMessage.Value] = 1;
                String[0].Wrapping = stringWrap;
            }
        }

        protected override byte[] TranslateText(byte[] text)
        {
            int i;
            _lastStringTag[0] = 0;

            if (text[0] == '/')
            {
                // Extract the string tag from the text: /..../
                for (i = 0; (i < 12) && (text[i + 1] != '/'); i++)
                    _lastStringTag[i] = (byte)char.ToUpper((char)text[i + 1]);
                _lastStringTag[i] = 0;
            }

            // TODO: vs translation
            return text;
        }

        protected override void SetCameraAt(Point pos)
        {
            Point old = Camera.CurrentPosition;

            Camera.CurrentPosition = pos;

            Camera.CurrentPosition = ClampCameraPos(Camera.CurrentPosition);

            Camera.DestinationPosition = Camera.CurrentPosition;
            Variables[VariableCameraDestX] = Camera.DestinationPosition.X;
            Variables[VariableCameraDestY] = Camera.DestinationPosition.Y;

            Debug.Assert(Camera.CurrentPosition.X >= (ScreenWidth / 2) && Camera.CurrentPosition.Y >= (ScreenHeight / 2));

            if (Camera.CurrentPosition.X != old.X || Camera.CurrentPosition.Y != old.Y)
            {
                if (Variables[VariableScrollScript.Value] != 0)
                {
                    Variables[VariableCameraPosX.Value] = Camera.CurrentPosition.X;
                    Variables[VariableCameraPosY.Value] = Camera.CurrentPosition.Y;
                    RunScript(Variables[VariableScrollScript.Value], false, false, new int[0]);
                }

                // Even though CameraMoved() is called automatically, we may
                // need to know at once that the camera has moved, or text may
                // be printed at the wrong coordinates. See bugs #795938 and
                // #929242
                CameraMoved();
            }
        }

        internal protected override void SetCameraFollows(Actor a, bool setCamera = false)
        {
            var oldfollow = Camera.ActorToFollow;

            Camera.ActorToFollow = a.Number;
            Variables[VariableCameraFollowedActor.Value] = a.Number;

            if (!a.IsInCurrentRoom)
            {
                StartScene(a.Room);
            }

            var ax = Math.Abs(a.Position.X - Camera.CurrentPosition.X);
            var ay = Math.Abs(a.Position.Y - Camera.CurrentPosition.Y);

            if (ax > Variables[VariableCameraThresholdX.Value] || ay > Variables[VariableCameraThresholdY.Value] || ax > (ScreenWidth / 2) || ay > (ScreenHeight / 2))
            {
                SetCameraAt(a.Position);
            }

            if (a.Number != oldfollow)
                RunInventoryScript(0);
        }

        protected override void MoveCamera()
        {
            Point old = Camera.CurrentPosition;
            Actor a = null;

            if (Camera.ActorToFollow != 0)
            {
                a = Actors[Camera.ActorToFollow];
                if (Math.Abs(Camera.CurrentPosition.X - a.Position.X) > Variables[VariableCameraThresholdX.Value] ||
                    Math.Abs(Camera.CurrentPosition.Y - a.Position.Y) > Variables[VariableCameraThresholdY.Value])
                {
                    Camera.MovingToActor = true;
                    if (Variables[VariableCameraThresholdX.Value] == 0)
                        Camera.CurrentPosition.X = a.Position.X;
                    if (Variables[VariableCameraThresholdY.Value] == 0)
                        Camera.CurrentPosition.Y = a.Position.Y;
                    Camera.CurrentPosition = ClampCameraPos(Camera.CurrentPosition);
                }
            }
            else
            {
                Camera.MovingToActor = false;
            }

            if (Camera.MovingToActor)
            {
                Variables[VariableCameraDestX] = Camera.DestinationPosition.X = a.Position.X;
                Variables[VariableCameraDestY] = Camera.DestinationPosition.Y = a.Position.Y;
            }

            Debug.Assert(Camera.CurrentPosition.X >= (ScreenWidth / 2) && Camera.CurrentPosition.Y >= (ScreenHeight / 2));

            Camera.DestinationPosition = ClampCameraPos(Camera.DestinationPosition);

            if (Camera.CurrentPosition.X < Camera.DestinationPosition.X)
            {
                Camera.CurrentPosition.X += (short)Variables[VariableCameraSpeedX];
                if (Camera.CurrentPosition.X > Camera.DestinationPosition.X)
                    Camera.CurrentPosition.X = Camera.DestinationPosition.X;
            }

            if (Camera.CurrentPosition.X > Camera.DestinationPosition.X)
            {
                Camera.CurrentPosition.X -= (short)Variables[VariableCameraSpeedX];
                if (Camera.CurrentPosition.X < Camera.DestinationPosition.X)
                    Camera.CurrentPosition.X = Camera.DestinationPosition.X;
            }

            if (Camera.CurrentPosition.Y < Camera.DestinationPosition.Y)
            {
                Camera.CurrentPosition.Y += (short)Variables[VariableCameraSpeedY];
                if (Camera.CurrentPosition.Y > Camera.DestinationPosition.Y)
                    Camera.CurrentPosition.Y = Camera.DestinationPosition.Y;
            }

            if (Camera.CurrentPosition.Y > Camera.DestinationPosition.Y)
            {
                Camera.CurrentPosition.Y -= (short)Variables[VariableCameraSpeedY];
                if (Camera.CurrentPosition.Y < Camera.DestinationPosition.Y)
                    Camera.CurrentPosition.Y = Camera.DestinationPosition.Y;
            }

            if (Camera.CurrentPosition.X == Camera.DestinationPosition.X && Camera.CurrentPosition.Y == Camera.DestinationPosition.Y)
            {

                Camera.MovingToActor = false;
                Camera.Accel.X = Camera.Accel.Y = 0;
                Variables[VariableCameraSpeedX] = Variables[VariableCameraSpeedY] = 0;
            }
            else
            {

                Camera.Accel.X += (short)Variables[VariableCameraAccelX.Value];
                Camera.Accel.Y += (short)Variables[VariableCameraAccelY.Value];

                Variables[VariableCameraSpeedX] += Camera.Accel.X / 100;
                Variables[VariableCameraSpeedY] += Camera.Accel.Y / 100;

                if (Variables[VariableCameraSpeedX] > 8)
                    Variables[VariableCameraSpeedX] = 8;

                if (Variables[VariableCameraSpeedY] > 8)
                    Variables[VariableCameraSpeedY] = 8;

            }

            CameraMoved();

            if (Camera.CurrentPosition.X != old.X || Camera.CurrentPosition.Y != old.Y)
            {
                Variables[VariableCameraPosX.Value] = Camera.CurrentPosition.X;
                Variables[VariableCameraPosY.Value] = Camera.CurrentPosition.Y;

                if (Variables[VariableScrollScript.Value] != 0)
                    RunScript(Variables[VariableScrollScript.Value], false, false, new int[0]);
            }
        }

        protected override void PanCameraToCore(Point pos)
        {
            Variables[VariableCameraFollowedActor.Value] = Camera.ActorToFollow = 0;
            Camera.DestinationPosition = pos;
            Variables[VariableCameraDestX] = pos.X;
            Variables[VariableCameraDestY] = pos.Y;
        }

        protected override void HandleDrawing()
        {
            base.HandleDrawing();

            // Full Throttle always redraws verbs and draws verbs before actors
            RedrawVerbs();
        }

        internal protected override void ProcessActors()
        {
            base.ProcessActors();
            akos_processQueue();
        }

        protected override void DrawVerb(int verb, int mode)
        {
            if (verb == 0)
                return;

            var vs = Verbs[verb];

            if (vs.SaveId == 0 && vs.CurMode != 0 && vs.VerbId != 0)
            {
                if (vs.Type == VerbType.Image)
                {
                    DrawVerbBitmap(verb, vs.CurRect.Left, vs.CurRect.Top);
                    return;
                }

                var color = vs.Color;
                if (vs.CurMode == 2)
                    color = vs.DimColor;
                else if (mode != 0 && vs.HiColor != 0)
                    color = vs.HiColor;

                var msg = Verbs[verb].Text;
                if (msg == null)
                    return;

                // Convert the message, and skip a few remaining 0xFF codes (they
                // occur in FT; subtype 10, which is used for the speech associated
                // with the string).
                byte[] buf = new byte[384];
                ConvertMessageToString(msg, buf, 0);
                msg = buf;
                var msgPos = 0;
                while (msg[msgPos] == 0xFF)
                    msgPos += 4;

                // Set the specified charset id
                int oldID = _charset.GetCurId();
                _charset.SetCurID(vs.CharsetNr);

                // Compute the text rect
                vs.CurRect.Right = 0;
                vs.CurRect.Bottom = 0;
                var msgPos2 = msgPos;
                while (msg[msgPos2] != 0)
                {
                    var charWidth = _charset.GetCharWidth(msg[msgPos2]);
                    var charHeight = _charset.GetCharHeight(msg[msgPos2]);
                    vs.CurRect.Right += charWidth;
                    if (vs.CurRect.Bottom < charHeight)
                        vs.CurRect.Bottom = charHeight;
                    msgPos2++;
                }
                vs.CurRect.Right += vs.CurRect.Left;
                vs.CurRect.Bottom += vs.CurRect.Top;
                vs.OldRect = vs.CurRect;

                var maxWidth = ScreenWidth - vs.CurRect.Left;
//                if (_charset.GetStringWidth(0, buf) > maxWidth && Game.Version == 8)
//                {
//                    byte[] tmpBuf = new byte[384];
//                    memcpy(tmpBuf, msg, 384);
//
//                    int len = resStrLen(tmpBuf) - 1;
//                    while (len >= 0)
//                    {
//                        if (tmpBuf[len] == ' ')
//                        {
//                            tmpBuf[len] = 0;
//                            if (_charset.getStringWidth(0, tmpBuf) <= maxWidth)
//                            {
//                                break;
//                            }
//                        }
//                        --len;
//                    }
//                    enqueueText(tmpBuf, vs.CurRect.left, vs.CurRect.top, color, vs.charset_nr, vs.center);
//                    if (len >= 0)
//                    {
//                        enqueueText(&msg[len + 1], vs.CurRect.left, vs.CurRect.top + _verbLineSpacing, color, vs.charset_nr, vs.center);
//                        vs.CurRect.bottom += _verbLineSpacing;
//                    }
//                }
//                else
//                {
                EnqueueText(msg, vs.CurRect.Left, vs.CurRect.Top, color, vs.CharsetNr, vs.Center);
//                }
                _charset.SetCurID(oldID);
            }
        }

        void akos_processQueue()
        {
            byte cmd;
            int actor, param_1, param_2;

            while (_akosQueuePos != 0)
            {
                cmd = (byte)_akosQueue[_akosQueuePos].cmd;
                actor = _akosQueue[_akosQueuePos].actor;
                param_1 = _akosQueue[_akosQueuePos].param1;
                param_2 = _akosQueue[_akosQueuePos].param2;
                _akosQueuePos--;

                var a = Actors[actor];

                switch (cmd)
                {
                    case 1:
                        a.PutActor(new Point(), 0);
                        break;
                    case 3:
                        if (param_1 != 0)
                        {
                            // TODO: vs
//                            if (_imuseDigital)
//                            {
//                                _imuseDigital.startSfx(param_1, 63);
//                            }
                        }
                        break;
                    case 4:
                        a.StartAnimActor(param_1);
                        break;
                    case 5:
                        a.ForceClip = (byte)param_1;
                        break;
//                    case 6:
//                        a._heOffsX = param_1;
//                        a._heOffsY = param_2;
//                        break;
                    case 7:
                        if (param_1 != 0)
                        {
                            // TODO: vs
//                            if (_imuseDigital)
//                            {
//                                _imuseDigital.setVolume(param_1, param_2);
//                            }
                        }
                        break;
                    case 8:
                        if (param_1 != 0)
                        {
                            // TODO: vs
//                            if (_imuseDigital)
//                            {
//                                _imuseDigital.setPan(param_1, param_2);
//                            }
                        }
                        break;
                    case 9:
                        if (param_1 != 0)
                        {
                            // TODO: vs
//                            if (_imuseDigital)
//                            {
//                                _imuseDigital.setPriority(param_1, param_2);
//                            }
                        }
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("akos_queCommand({0},{1},{2},{3})", cmd, a.Number, param_1, param_2));
                }
            }
        }

        internal void ProcessSubtitleQueue()
        {
            for (int i = 0; i < _subtitleQueuePos; ++i)
            {
                var st = _subtitleQueue[i];
                if (/*!st.ActorSpeechMsg  && (!ConfMan.getBool("subtitles") ||*/ Variables[VariableVoiceMode.Value] == 0)
                    // no subtitles and there's a speech variant of the message, don't display the text
                    continue;
                EnqueueText(st.Text, st.X, st.Y, st.Color, st.Charset, false);
            }
        }

        internal void AddSubtitleToQueue(byte[] text, Point pos, byte color, byte charset)
        {
            if (text[0] != 0 && System.Text.Encoding.ASCII.GetString(text) != " ")
            {
                Debug.Assert(_subtitleQueuePos < _subtitleQueue.Length);
                var st = _subtitleQueue[_subtitleQueuePos];
                int i = 0;
                while (true)
                {
                    st.Text[i] = text[i];
                    if (text[i] == 0)
                        break;
                    ++i;
                }
                st.X = pos.X;
                st.Y = pos.Y;
                st.Color = color;
                st.Charset = charset;
                st.ActorSpeechMsg = _haveActorSpeechMsg;
                ++_subtitleQueuePos;
            }
        }

        internal void ClearSubtitleQueue()
        {
            for (int i = 0; i < _subtitleQueuePos; i++)
            {
                _subtitleQueue[i] = new SubtitleText();
            }
            _subtitleQueuePos = 0;
        }

        Point ClampCameraPos(Point pt)
        {
            short x = pt.X, y = pt.Y;
            if (pt.X < Variables[VariableCameraMinX.Value])
                x = (short)Variables[VariableCameraMinX.Value];

            if (pt.X > Variables[VariableCameraMaxX.Value])
                x = (short)Variables[VariableCameraMaxX.Value];

            if (pt.Y < Variables[VariableCameraMinY.Value])
                y = (short)Variables[VariableCameraMinY.Value];

            if (pt.Y > Variables[VariableCameraMaxY.Value])
                y = (short)Variables[VariableCameraMaxY.Value];

            return new Point(x, y);
        }

        void PlaySpeech(byte[] ptr)
        {
            if (Game.GameId == GameId.Dig && /*(ConfMan.getBool("speech_mute") ||*/ Variables[VariableVoiceMode.Value] == 2)
                return;

            if ((Game.GameId == GameId.Dig /*|| Game.GameId == GID_CMI*/) && ptr[0] != 0)
            {
                var pointer = System.Text.Encoding.ASCII.GetString(ptr);

                // Play speech
                if (!(Game.Features.HasFlag(GameFeatures.Demo) /*&& Game.GameId == GameId.GID_CMI*/)) // CMI demo does not have .IMX for voice
                    pointer += ".IMX";

                Sound.StopTalkSound();
                // TODO: vs _imuseDigital
//                _imuseDigital.StopSound(Sound.TalkSoundID);
//                _imuseDigital.StartVoice(Sound.TalkSoundID, pointer);
                Sound.TalkSound(0, 0, 2);
            }
        }

        void SetShadowPalette(int slot, int redScale, int greenScale, int blueScale, int startColor, int endColor)
        {
            if (slot < 0 || slot >= NumShadowPalette)
                throw new ArgumentException(string.Format("setShadowPalette: invalid slot {0}", slot), "slot");

            if (startColor < 0 || startColor > 255 || endColor < 0 || endColor > 255 || endColor < startColor)
                throw new ArgumentException(string.Format("setShadowPalette: invalid range from {0} to {1}", startColor, endColor), "startColor");

            var offs = slot * 256;
            for (var i = 0; i < 256; i++)
                _shadowPalette[offs + i] = (byte)i;

            offs += startColor;
            for (var i = startColor; i <= endColor; i++)
            {
                var curColor = CurrentPalette.Colors[i];
                _shadowPalette[offs + i] = (byte)RemapPaletteColor(
                    (curColor.R * redScale) >> 8, 
                    (curColor.G * greenScale) >> 8,
                    (curColor.B * blueScale) >> 8, -1);
            }
        }

        public byte[] GetStringAddressVar(int i)
        {
            return GetStringAddress(Variables[i]);
        }

        byte[] GetStringAddress(int i)
        {
            byte[] addr = _strings[i];
            if (addr == null)
                return null;
            // Skip over the ArrayHeader
            var tmp = new byte[addr.Length - 6];
            Array.Copy(addr, 6, tmp, 0, tmp.Length);
            return tmp;
        }
    }
}
