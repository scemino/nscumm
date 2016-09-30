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

using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Core.Video;
using static NScumm.Core.DebugHelper;

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
    }
}

