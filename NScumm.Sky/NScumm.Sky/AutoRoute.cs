using System;
using NScumm.Core;

namespace NScumm.Sky
{
    internal class AutoRoute
    {
        private const int RouteGridWidth = Screen.GameScreenWidth / 8 + 2;
        private const int RouteGridHeight = Screen.GameScreenHeight / 8 + 2;
        private const int RouteGridSize = RouteGridWidth * RouteGridHeight * 2;
        private const int WalkJump = 8; // walk in blocks of 8

        private static readonly short[] RouteDirections = { -1, 1, -RouteGridWidth, RouteGridWidth };
        private static readonly ushort[] LogicCommands = { Logic.RIGHTY, Logic.LEFTY, Logic.DOWNY, Logic.UPY };

        private readonly Grid _grid;
        private readonly byte[] _routeBuf;
        private readonly byte[] _routeGrid;
        private readonly SkyCompact _skyCompact;

        public AutoRoute(Grid skyGrid, SkyCompact skyCompact)
        {
            _grid = skyGrid;
            _skyCompact = skyCompact;
            _routeGrid = new byte[RouteGridSize];
            _routeBuf = new byte[Logic.ROUTE_SPACE];
        }

        public ushort DoAutoRoute(Compact cpt)
        {
            var cptScreen = (byte)cpt.Core.screen;
            var cptWidth = (byte)SkyCompact.GetMegaSet(cpt).gridWidth;
            InitWalkGrid(cptScreen, cptWidth);

            byte startX, startY, destX, destY;
            short initStaX, initStaY, initDestX, initDestY;

            ClipCoordX(cpt.Core.xcood, out startX, out initStaX);
            ClipCoordY(cpt.Core.ycood, out startY, out initStaY);
            ClipCoordX(cpt.Core.arTargetX, out destX, out initDestX);
            ClipCoordY(cpt.Core.arTargetY, out destY, out initDestY);

            var raw = _skyCompact.FetchCptRaw(cpt.Core.animScratchId);
            Array.Clear(raw, 0, 64);
            var routeDest = new UShortAccess(raw, 0);
            if ((startX == destX) && (startY == destY))
                return 2;

            var routeGrid = new UShortAccess(_routeGrid, 0);
            if (routeGrid[(destY + 1) * RouteGridWidth + destX + 1] != 0)
            {
                //if ((cpt == &Sky::SkyCompact::foster) && (cptScreen == 12) && (destX == 2) && (destY == 14)) {
                if (_skyCompact.CptIsId(cpt, (ushort)CptIds.Foster) && (cptScreen == 12) && (destX == 2) &&
                    (destY == 14))
                {
                    /* workaround for Scriptbug #1043047
                       In screen 12 (the pipe factory) Joey can block Foster's target
                       coordinates (2/14). This is normally not too tragic, but in the
                       scene when foster gets thrown out by Lamb (first time you enter
                       the pipe factory), the game would enter an infinite loop. */
                    routeGrid[(destY + 1) * RouteGridWidth + destX + 1] = 0;
                    // hide this part joey from the grid
                }
                else
                    return 1; // AR destination is an unaccessible block
            }

            if (!CalcWalkGrid(startX, startY, destX, destY))
                return 1; // can't find route to block

            var routeData = MakeRouteData(destX, destY);
            // the route is done.
            // if there was an initial x movement (due to clipping) tag it onto the start
            routeData = CheckInitMove(routeData, initStaX);

            byte cnt = 0;
            do
            {
                routeDest[cnt] = routeData[cnt];
                routeDest[cnt + 1] = routeData[cnt + 1];
                cnt += 2;
            } while (routeData[cnt - 2] != 0);
            return 0;
        }

        private static ushort CheckBlock(byte[] block, int blockPos)
        {
            var b = new UShortAccess(block, blockPos);
            ushort retVal = 0xFFFF;

            for (byte cnt = 0; cnt < 4; cnt++)
            {
                var fieldVal = b[RouteDirections[cnt]];
                if (fieldVal != 0 && (fieldVal < retVal))
                    retVal = fieldVal;
            }
            return retVal;
        }

        private static void ClipCoordX(ushort x, out byte blkX, out short initX)
        {
            if (x < Logic.TOP_LEFT_X)
            {
                blkX = 0;
                initX = (short)(x - Logic.TOP_LEFT_X);
            }
            else if (x >= Logic.TOP_LEFT_X + Screen.GameScreenWidth)
            {
                blkX = (Screen.GameScreenWidth - 1) >> 3;
                initX = (short)(x - (Logic.TOP_LEFT_X + Screen.GameScreenWidth - 1));
            }
            else
            {
                blkX = (byte)((x - Logic.TOP_LEFT_X) >> 3);
                initX = 0;
            }
        }

        private static void ClipCoordY(ushort y, out byte blkY, out short initY)
        {
            if (y < Logic.TOP_LEFT_Y)
            {
                blkY = 0;
                initY = (short)(y - Logic.TOP_LEFT_Y);
            }
            else if (y >= Logic.TOP_LEFT_Y + Screen.GameScreenHeight)
            {
                blkY = (Screen.GameScreenHeight - 1) >> 3;
                initY = (short)(y - (Logic.TOP_LEFT_Y + Screen.GameScreenHeight));
            }
            else
            {
                blkY = (byte)((y - Logic.TOP_LEFT_Y) >> 3);
                initY = 0;
            }
        }

        private void InitWalkGrid(byte screen, byte width)
        {
            byte stretch = 0;
            var screenGrid = _grid.GiveGrid(screen);
            var screenGridPos = Logic.GRID_SIZE;
            var wGridPos = new UShortAccess(_routeGrid, ((RouteGridSize >> 1) - RouteGridWidth - 2) * 2);

            Array.Clear(_routeGrid, 0, _routeGrid.Length);
            byte bitsLeft = 0;
            uint gridData = 0;
            for (byte gridCntY = 0; gridCntY < RouteGridHeight - 2; gridCntY++)
            {
                for (byte gridCntX = 0; gridCntX < RouteGridWidth - 2; gridCntX++)
                {
                    if (bitsLeft == 0)
                    {
                        screenGridPos -= 4;
                        gridData = screenGrid.ToUInt32(screenGridPos);
                        bitsLeft = 32;
                    }
                    if ((gridData & 1) != 0)
                    {
                        wGridPos[0] = 0xFFFF; // block is not accessible
                        stretch = width;
                    }
                    else if (stretch != 0)
                    {
                        wGridPos[0] = 0xFFFF;
                        stretch--;
                    }
                    wGridPos.Offset -= 2;
                    bitsLeft--;
                    gridData >>= 1;
                }
                wGridPos.Offset -= 4;
                stretch = 0;
            }
        }

        private bool CalcWalkGrid(byte startX, byte startY, byte destX, byte destY)
        {
            short directionX, directionY;
            byte roiX, roiY; // Rectangle Of Interest in the walk grid
            if (startY > destY)
            {
                directionY = -RouteGridWidth;
                roiY = startY;
            }
            else
            {
                directionY = RouteGridWidth;
                roiY = (byte)(RouteGridHeight - 1 - startY);
            }
            if (startX > destX)
            {
                directionX = -1;
                roiX = (byte)(startX + 2);
            }
            else
            {
                directionX = 1;
                roiX = (byte)(RouteGridWidth - 1 - startX);
            }

            var walkDest = (destY + 1) * RouteGridWidth + destX + 1;
            var walkStart = (startY + 1) * RouteGridWidth + startX + 1;

            var routeGrid = new UShortAccess(_routeGrid, 0);
            routeGrid[walkStart] = 1;

            // if we are on the edge, move diagonally from start
            if (roiY < RouteGridHeight - 3)
                walkStart -= directionY;

            if (roiX < RouteGridWidth - 2)
                walkStart -= directionX;

            var gridChanged = true;
            var foundRoute = false;

            while (!foundRoute && gridChanged)
            {
                gridChanged = false;
                var yWalkCalc = walkStart;
                for (byte cnty = 0; cnty < roiY; cnty++)
                {
                    var xWalkCalc = yWalkCalc;
                    for (byte cntx = 0; cntx < roiX; cntx++)
                    {
                        if (routeGrid[xWalkCalc] == 0)
                        {
                            // block wasn't done, yet
                            var blockRet = CheckBlock(_routeGrid, routeGrid.Offset + xWalkCalc * 2);
                            if (blockRet < 0xFFFF)
                            {
                                routeGrid[xWalkCalc] = (ushort)(blockRet + 1);
                                gridChanged = true;
                            }
                        }
                        xWalkCalc += directionX;
                    }
                    yWalkCalc += directionY;
                }
                if (routeGrid[walkDest] != 0)
                {
                    // okay, finished
                    foundRoute = true;
                }
                else
                {
                    // we couldn't find the route, let's extend the ROI
                    if (roiY < RouteGridHeight - 4)
                    {
                        walkStart -= directionY;
                        roiY++;
                    }
                    if (roiX < RouteGridWidth - 4)
                    {
                        walkStart -= directionX;
                        roiX++;
                    }
                }
            }
            return foundRoute;
        }

        private UShortAccess MakeRouteData(byte destX, byte destY)
        {
            Array.Clear(_routeBuf, 0, _routeBuf.Length);
            var routeBuf = new UShortAccess(_routeBuf, 0);
            var routeGrid = new UShortAccess(_routeGrid, 0);

            var routePos = (destY + 1) * RouteGridWidth + destX + 1;
            var dataTrg = (Logic.ROUTE_SPACE >> 1) - 2;

            var lastVal = (ushort)(routeGrid[routePos] - 1);
            while (lastVal != 0)
            {
                // lastVal == 0 means route is done.
                dataTrg -= 2;

                short walkDirection = 0;
                for (byte cnt = 0; cnt < 4; cnt++)
                    if (lastVal == routeGrid[routePos + RouteDirections[cnt]])
                    {
                        routeBuf[dataTrg + 1] = LogicCommands[cnt];
                        walkDirection = RouteDirections[cnt];
                        break;
                    }

                if (walkDirection == 0)
                    throw new InvalidOperationException(
                        string.Format("makeRouteData:: can't find way through walkGrid (pos {0})", lastVal));

                while (lastVal != 0 && (lastVal == routeGrid[routePos + walkDirection]))
                {
                    routeBuf[dataTrg] += WalkJump;
                    lastVal--;
                    routePos += walkDirection;
                }
            }
            return new UShortAccess(routeBuf.Data, routeBuf.Offset + dataTrg * 2);
        }

        private static UShortAccess CheckInitMove(UShortAccess data, short initStaX)
        {
            var index = 0;
            if (initStaX < 0)
            {
                index -= 2;
                data[index + 1] = Logic.RIGHTY;
                data[index] = (ushort)((-initStaX + 7) & 0xFFF8);
            }
            else if (initStaX > 0)
            {
                index -= 2;
                data[index] = (ushort)((initStaX + 7) & 0xFFF8);
                data[index] = (ushort)((initStaX + 7) & 0xFFF8);
            }
            return new UShortAccess(data.Data, data.Offset + index * 2);
        }
    }
}