//
//  KVideo.cs
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
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Core.Video;
using NScumm.Sci.Graphics;
using static NScumm.Core.DebugHelper;
using NScumm.Sci.Video;

namespace NScumm.Sci.Engine
{
    internal partial class Kernel
    {
        private static Register kShowMovie(EngineState s, int argc, StackPtr argv)
        {
            // Hide the cursor if it's showing and then show it again if it was
            // previously visible.
            bool reshowCursor = SciEngine.Instance._gfxCursor.IsVisible;
            if (reshowCursor)
                SciEngine.Instance._gfxCursor.KernelHide();

            var screenWidth = SciEngine.Instance.Settings.Game.Width;
            var screenHeight = SciEngine.Instance.Settings.Game.Height;

            VideoDecoder videoDecoder = null;

            if (argv[0].Segment != 0)
            {
                var filename = s._segMan.GetString(argv[0]);

                if (SciEngine.Instance.Platform == Platform.Macintosh)
                {
                    // Mac QuickTime
                    // The only argument is the string for the video

                    // HACK: Switch to 16bpp graphics for Cinepak.
                    // TODO: InitGraphics(screenWidth, screenHeight, screenWidth > 320, null);

                    if (SciEngine.Instance.System.GraphicsManager.PixelFormat.GetBytesPerPixel() == 1)
                    {
                        Warning(
                            "This video requires >8bpp color to be displayed, but could not switch to RGB color mode");
                        return Register.NULL_REG;
                    }

                    Warning("QuickTimeDecoder not implemented.");
                    // TODO:                    videoDecoder = new QuickTimeDecoder();
                    //                    if (!videoDecoder.loadFile(filename))
                    //                        Error("Could not open '{0}'", filename);
                }
                else
                {
                    // DOS SEQ
                    // SEQ's are called with no subops, just the string and delay
                    // Time is specified as ticks
                    Warning("SEQDecoder not implemented.");
                    // TODO:                   videoDecoder = new SEQDecoder(argv[1].toUint16());
                    //
                    //                    if (!videoDecoder.loadFile(filename))
                    //                    {
                    //                        warning("Failed to open movie file %s", filename.c_str());
                    //                        delete videoDecoder;
                    //                        videoDecoder = 0;
                    //                    }
                }
            }
            else
            {
                // Windows AVI
                // TODO: This appears to be some sort of subop. case 0 contains the string
                // for the video, so we'll just play it from there for now.

#if ENABLE_SCI32
                if (ResourceManager.GetSciVersion() >= SciVersion.V2_1_EARLY)
                {
                    // SCI2.1 always has argv[0] as 1, the rest of the arguments seem to
                    // follow SCI1.1/2.
                    if (argv[0].ToUInt16() != 1)
                        Error("SCI2.1 kShowMovie argv[0] not 1");
                    argv++;
                    argc--;
                }
#endif
                switch (argv[0].ToUInt16())
                {
                    case 0:
                        {
                            var filename = s._segMan.GetString(argv[1]);
                            Warning("AVIDecoder not implemented.");
                            //                        TODO: videoDecoder = new AVIDecoder();
                            //
                            //                        if (filename.equalsIgnoreCase("gk2a.avi"))
                            //                        {
                            //                            // HACK: Switch to 16bpp graphics for Indeo3.
                            //                            // The only known movie to do use this codec is the GK2 demo trailer
                            //                            // If another video turns up that uses Indeo, we may have to add a better
                            //                            // check.
                            //                            InitGraphics(screenWidth, screenHeight, screenWidth > 320, null);
                            //
                            //                            if (SciEngine.Instance.System.GraphicsManager.PixelFormat.GetBytesPerPixel() == 1)
                            //                            {
                            //                                Warning(
                            //                                    "This video requires >8bpp color to be displayed, but could not switch to RGB color mode");
                            //                                return Register.NULL_REG;
                            //                            }
                            //                        }
                            //
                            //                        if (!videoDecoder.LoadFile(filename))
                            //                        {
                            //                            Warning("Failed to open movie file {0}", filename);
                            //                            videoDecoder = null;
                            //                        }
                            //                        else
                            //                        {
                            //                            s._videoState.fileName = filename;
                            //                        }
                            break;
                        }
                    default:
                        Warning("Unhandled SCI kShowMovie subop {0}", argv[0].ToUInt16());
                        break;
                }
            }

            //if (videoDecoder!=null)
            //{
            //    PlayVideo(videoDecoder, s._videoState);

            //    // HACK: Switch back to 8bpp if we played a true color video.
            //    // We also won't be copying the screen to the SCI screen...
            //    if (SciEngine.Instance.System.GraphicsManager.PixelFormat.GetPytesPerPixel() != 1)
            //        InitGraphics(screenWidth, screenHeight, screenWidth > 320);
            //    else
            //    {
            //        SciEngine.Instance._gfxScreen.KernelSyncWithFramebuffer();
            //        SciEngine.Instance._gfxPalette16.KernelSyncScreenPalette();
            //    }
            //}

            if (reshowCursor)
                SciEngine.Instance._gfxCursor.KernelShow();

            return s.r_acc;
        }

#if ENABLE_SCI32
        private static Register kPlayVMD(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kPlayVMDOpen(EngineState s, int argc, StackPtr argv)
        {
            string fileName = s._segMan.GetString(argv[0]);
            // argv[1] is an optional cache size argument which we do not use
            // const uint16 cacheSize = argc > 1 ? CLIP<int16>(argv[1].toSint16(), 16, 1024) : 0;
            var flags = argc > 2 ? (VMDPlayer.OpenFlags)argv[2].ToUInt16() : VMDPlayer.OpenFlags.None;

            return Register.Make(0, (ushort)SciEngine.Instance._video32.VMDPlayer.Open(fileName, flags));
        }

        private static Register kPlayVMDInit(EngineState s, int argc, StackPtr argv)
        {
            short x = argv[0].ToInt16();
            short y = argv[1].ToInt16();
            VMDPlayer.PlayFlags flags = argc > 2 ? (VMDPlayer.PlayFlags)argv[2].ToUInt16() : VMDPlayer.PlayFlags.None;
            short boostPercent;
            short boostStartColor;
            short boostEndColor;
            if (argc > 5 && (flags & VMDPlayer.PlayFlags.Boost) != 0)
            {
                boostPercent = argv[3].ToInt16();
                boostStartColor = argv[4].ToInt16();
                boostEndColor = argv[5].ToInt16();
            }
            else
            {
                boostPercent = 0;
                boostStartColor = -1;
                boostEndColor = -1;
            }

            SciEngine.Instance._video32.VMDPlayer.Init(x, y, flags, boostPercent, boostStartColor, boostEndColor);

            return Register.Make(0, 0);
        }

        private static Register kPlayVMDClose(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, (ushort)SciEngine.Instance._video32.VMDPlayer.Close());
        }

        private static Register kPlayVMDPlayUntilEvent(EngineState s, int argc, StackPtr argv)
        {
            VMDPlayer.EventFlags flags = (VMDPlayer.EventFlags)argv[0].ToUInt16();
            short lastFrameNo = (short)(argc > 1 ? argv[1].ToInt16() : -1);
            short yieldInterval = (short)(argc > 2 ? argv[2].ToInt16() : -1);
            return Register.Make(0,
                (ushort)SciEngine.Instance._video32.VMDPlayer.KernelPlayUntilEvent(flags, lastFrameNo, yieldInterval));
        }

        private static Register kPlayVMDShowCursor(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._video32.VMDPlayer.ShowCursor = argv[0].ToUInt16() != 0;
            return s.r_acc;
        }

        private static Register kPlayVMDSetBlackoutArea(EngineState s, int argc, StackPtr argv)
        {
            short scriptWidth = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            Rect blackoutArea = new Rect();
            blackoutArea.Left = Math.Max((short)0, argv[0].ToInt16());
            blackoutArea.Top = Math.Max((short)0, argv[1].ToInt16());
            blackoutArea.Right = Math.Min(scriptWidth, (short)(argv[2].ToInt16() + 1));
            blackoutArea.Bottom = Math.Min(scriptHeight, (short)(argv[3].ToInt16() + 1));
            SciEngine.Instance._video32.VMDPlayer.SetBlackoutArea(blackoutArea);
            return s.r_acc;
        }

        private static Register kPlayVMDRestrictPalette(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._video32.VMDPlayer.RestrictPalette((byte)argv[0].ToUInt16(), (byte)argv[1].ToUInt16());
            return s.r_acc;
        }

        private static Register kRobot(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kRobotOpen(EngineState s, int argc, StackPtr argv)
        {
            int robotId = argv[0].ToUInt16();
            Register plane = argv[1];
            short priority = argv[2].ToInt16();
            short x = argv[3].ToInt16();
            short y = argv[4].ToInt16();
            short scale = argc > 5 ? argv[5].ToInt16() : (short)128;
            SciEngine.Instance._video32.RobotPlayer.Open(robotId, plane, priority, x, y, scale);
            return Register.Make(0, 0);
        }

        private static Register kRobotShowFrame(EngineState s, int argc, StackPtr argv)
        {
            ushort frameNo = argv[0].ToUInt16();
            ushort newX = argc > 1 ? argv[1].ToUInt16() : (ushort)RobotDecoder.kUnspecified;
            ushort newY = argc > 1 ? argv[2].ToUInt16() : (ushort)RobotDecoder.kUnspecified;
            SciEngine.Instance._video32.RobotPlayer.ShowFrame(frameNo, newX, newY, RobotDecoder.kUnspecified);
            return s.r_acc;
        }

        private static Register kRobotGetFrameSize(EngineState s, int argc, StackPtr argv)
        {
            Rect frameRect = new Rect();
            ushort numFramesTotal = SciEngine.Instance._video32.RobotPlayer.GetFrameSize(ref frameRect);

            SciArray outRect = s._segMan.LookupArray(argv[0]);
            Register[] values = new Register[4] {
                Register.Make(0, (ushort)frameRect.Left),
                Register.Make(0, (ushort)frameRect.Top),
                Register.Make(0, (ushort)(frameRect.Right - 1)),
                Register.Make(0, (ushort)(frameRect.Bottom - 1))
            };
            outRect.SetElements(0, 4, new StackPtr(values));

            return Register.Make(0, numFramesTotal);
        }

        private static Register kRobotPlay(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._video32.RobotPlayer.Resume();
            return s.r_acc;
        }

        private static Register kRobotGetIsFinished(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, SciEngine.Instance._video32.RobotPlayer.Status == RobotStatus.kRobotStatusEnd);
        }

        private static Register kRobotGetIsPlaying(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, SciEngine.Instance._video32.RobotPlayer.Status == RobotStatus.kRobotStatusPlaying);
        }

        private static Register kRobotClose(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._video32.RobotPlayer.Close();
            return s.r_acc;
        }

        private static Register kRobotGetCue(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.WriteSelectorValue(s._segMan, argv[0], o => o.signal, (ushort)SciEngine.Instance._video32.RobotPlayer.GetCue());
            return s.r_acc;
        }

        private static Register kRobotPause(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._video32.RobotPlayer.Pause();
            return s.r_acc;
        }

        private static Register kRobotGetFrameNo(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, (ushort)SciEngine.Instance._video32.RobotPlayer.FrameNo);
        }

        private static Register kRobotSetPriority(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._video32.RobotPlayer.SetPriority(argv[0].ToInt16());
            return s.r_acc;
        }

        private static Register kPlayDuck(EngineState s, int argc, StackPtr argv)
        {
            throw new NotImplementedException("kPlayDuck");
            //            ushort operation = argv[0].ToUInt16();
            //            bool reshowCursor = SciEngine.Instance._gfxCursor.IsVisible;
            //
            //            switch (operation)
            //            {
            //                case 1: // Play
            //                    // 6 params
            //                    s._videoState.Reset();
            //                    s._videoState.fileName = $"{argv[1].ToUInt16()}.duk";
            //
            //                    var videoDecoder = new AVIDecoder();
            //
            //                    if (!videoDecoder.LoadFile(s._videoState.fileName))
            //                    {
            //                        Warning("Could not open Duck {0}", s._videoState.fileName);
            //                        break;
            //                    }
            //
            //                    if (reshowCursor)
            //                        SciEngine.Instance._gfxCursor.KernelHide();
            //
            //                {
            //                    // Duck videos are 16bpp, so we need to change the active pixel format
            //                    int oldWidth = SciEngine.Instance.Settings.Game.Width;
            //                    int oldHeight = SciEngine.Instance.Settings.Game.Height;
            //                    var formats = new List<PixelFormat> {videoDecoder.PixelFormat};
            //                    InitGraphics(640, 480, true, formats);
            //
            //                    if (SciEngine.Instance.System.GraphicsManager.PixelFormat.GetBytesPerPixel() !=
            //                        videoDecoder.PixelFormat.BytesPerPixel)
            //                        Error("Could not switch screen format for the duck video");
            //
            //                    PlayVideo(videoDecoder, s._videoState);
            //
            //                    // Switch back to 8bpp
            //                    InitGraphics(oldWidth, oldHeight, oldWidth > 320);
            //                }
            //
            //                    if (reshowCursor)
            //                        SciEngine.Instance._gfxCursor.KernelShow();
            //                    break;
            //                default:
            //                    kStub(s, argc, argv);
            //                    break;
            //            }
            //
            //            return s.r_acc;
        }

        private static Register kShowMovie32(EngineState s, int argc, StackPtr argv)
        {
            string fileName = s._segMan.GetString(argv[0]);
            short numTicks = argv[1].ToInt16();
            short x = (short)(argc > 3 ? argv[2].ToInt16() : 0);
            short y = (short)(argc > 3 ? argv[3].ToInt16() : 0);

            throw new NotImplementedException();
            //TODO: vs
            // SciEngine.Instance._video32.SEQPlayer.Play(fileName, numTicks, x, y);

            return s.r_acc;
        }

        private static Register kShowMovieWin(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }
#endif
    }
}