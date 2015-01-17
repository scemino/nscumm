//
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
        bool _skipVideo;
        bool _disableFadeInEffect;
        byte[] _lastStringTag = new byte[12 + 1];

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

                            // TODO: vs
//                            _insane.SetSmushParams(_smushFrameRate);
                            Insane.RunScene(insaneVarNum);
                        }
                    }
                    break;
                case 12:
                    SetCursorFromImg(args[1], -1, args[2]);
                    break;
                case 14:
                    Actors[args[1]].RemapActorPalette(args[2], args[3], args[4], args[5]);
                    break;
                case 15:
                    _smushFrameRate = args[1];
                    break;
                case 108:
                    SetShadowPalette(args[1], args[2], args[3], args[4], args[5], args[6]);
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
            base.ProcessInput();

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
            // TODO: vs fix pos x and y
            int pos = Camera.CurrentPosition.X;
            int t;
            Actor a = null;
            var snapToX = (/*_snapScroll ||*/ (VariableCameraFastX.HasValue && Variables[VariableCameraFastX.Value] != 0));

            Camera.CurrentPosition.X = (short)(Camera.CurrentPosition.X & 0xFFF8);

            if (VariableCameraMinX.HasValue && Camera.CurrentPosition.X < Variables[VariableCameraMinX.Value])
            {
                if (snapToX)
                    Camera.CurrentPosition.X = (short)Variables[VariableCameraMinX.Value];
                else
                    Camera.CurrentPosition.X += 8;
                CameraMoved();
                return;
            }

            if (VariableCameraMaxX.HasValue && Camera.CurrentPosition.X > Variables[VariableCameraMaxX.Value])
            {
                if (snapToX)
                    Camera.CurrentPosition.X = (short)Variables[VariableCameraMaxX.Value];
                else
                    Camera.CurrentPosition.X -= 8;
                CameraMoved();
                return;
            }

            if (Camera.Mode == CameraMode.FollowActor)
            {
                a = Actors[Camera.ActorToFollow];

                int actorx = a.Position.X;
                t = actorx / 8 - _screenStartStrip;

                if (t < Camera.LeftTrigger || t > Camera.RightTrigger)
                {
                    if (snapToX)
                    {
                        if (t > 40 - 5)
                            Camera.DestinationPosition.X = (short)(actorx + 80);
                        if (t < 5)
                            Camera.DestinationPosition.X = (short)(actorx - 80);
                    }
                    else
                        Camera.MovingToActor = true;
                }
            }

            if (Camera.MovingToActor)
            {
                a = Actors[Camera.ActorToFollow];
                Camera.DestinationPosition.X = a.Position.X;
            }

            if (VariableCameraMinX.HasValue && Camera.DestinationPosition.X < Variables[VariableCameraMinX.Value])
                Camera.DestinationPosition.X = (short)Variables[VariableCameraMinX.Value];

            if (VariableCameraMaxX.HasValue && Camera.DestinationPosition.X > Variables[VariableCameraMaxX.Value])
                Camera.DestinationPosition.X = (short)Variables[VariableCameraMaxX.Value];

            if (snapToX)
            {
                Camera.CurrentPosition.X = Camera.DestinationPosition.X;
            }
            else
            {
                if (Camera.CurrentPosition.X < Camera.DestinationPosition.X)
                    Camera.CurrentPosition.X += 8;
                if (Camera.CurrentPosition.X > Camera.DestinationPosition.X)
                    Camera.CurrentPosition.X -= 8;
            }

            /* Actor 'a' is set a bit above */
            if (Camera.MovingToActor && (Camera.CurrentPosition.X / 8) == (a.Position.X / 8))
            {
                Camera.MovingToActor = false;
            }

            CameraMoved();

            if (VariableScrollScript.HasValue && Variables[VariableScrollScript.Value] != 0 && pos != Camera.CurrentPosition.X)
            {
                Variables[VariableCameraPosX.Value] = Camera.CurrentPosition.X;
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

        Point ClampCameraPos(Point pt)
        {
            short x = pt.X, y = pt.Y;
            if (pt.X < Variables[VariableCameraMinX.Value])
                x = (short)Variables[VariableCameraMinX.Value];

            if (pt.X > Variables[VariableCameraMaxX.Value])
                x = (short)Variables[VariableCameraMaxX.Value];

            if (pt.Y < Variables[VariableCameraMinY])
                y = (short)Variables[VariableCameraMinY];

            if (pt.Y > Variables[VariableCameraMaxY])
                y = (short)Variables[VariableCameraMaxY];

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

