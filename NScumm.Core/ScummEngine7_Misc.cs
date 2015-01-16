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

namespace NScumm.Core
{
    partial class ScummEngine7
    {
        int _smushFrameRate;
        bool _skipVideo;
        bool _disableFadeInEffect;

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
//                                _splayer->play("jumpgorg.san", _smushFrameRate);
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
                            // TODO: vs
//                            const int insaneVarNum = ((Game.Features.HasFlag(GameFeatures.Demo) && (Game.Platform == Common::kPlatformDOS))
//                                ? 232 : 233;

//                            _insane.SetSmushParams(_smushFrameRate);
//                            _insane.RunScene(insaneVarNum);
                        }
                    }
                    break;
                case 12:
                    SetCursorFromImg(args[1], -1, args[2]);
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
//                _imuseDigital->flushTracks();
//                // In CoMI and the Dig the full (non-demo) version invoke IMuseDigital::refreshScripts
//                if ((_game.id == GID_DIG || _game.id == GID_CMI) && !(_game.features & GF_DEMO))
//                    _imuseDigital->refreshScripts();
//            }
//            if (_smixer) {
//                _smixer->flush();
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
            if (cutsceneExitKeyEnabled && _inputManager.IsKeyDown(KeyCode.Escape)) {
                // Skip cutscene (or active SMUSH video).
                if (SmushActive) {
                    if (Game.GameId == GameId.FullThrottle)
                    {
                        // TODO: vs insane
//                        _insane.EscapeKeyHandler();
                    }
                    else
                        SmushVideoShouldFinish = true;
                    _skipVideo = true;
                } else {
                    AbortCutscene();
                }

                mouseAndKeyboardStat = KeyCode.Escape;

            }

            if (_skipVideo && !SmushActive) {
                AbortCutscene();
                mouseAndKeyboardStat = KeyCode.Escape;
                _skipVideo = false;
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

        byte[] GetStringAddressVar(int i)
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

