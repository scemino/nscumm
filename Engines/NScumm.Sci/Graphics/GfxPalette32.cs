//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    internal enum PalCyclerDirection
    {
        PalCycleBackward = 0,
        PalCycleForward = 1
    }

    internal class PalCycler
    {
        /// <summary>
        /// The color index of the palette cycler. This value is effectively used as the ID for the
        /// cycler.
        /// </summary>
        public byte fromColor;

        /// <summary>
        /// The number of palette slots which are cycled by the palette cycler.
        /// </summary>
        public ushort numColorsToCycle;

        /// <summary>
        /// The position of the cursor in its cycle.
        /// </summary>
        public byte currentCycle;

        /// <summary>
        /// The direction of the cycler.
        /// </summary>
        public PalCyclerDirection direction;

        /// <summary>
        /// The cycle tick at the last time the cycler’s currentCycle was updated.
        /// 795 days of game time ought to be enough for everyone? :)
        /// </summary>
        public uint lastUpdateTick;

        /// <summary>
        /// The amount of time in ticks each cycle should take to complete. In other words,
        /// the higher the delay, the slower the cycle animation. If delay is 0, the cycler
        /// does not automatically cycle and needs to be pumped manually with DoCycle.
        /// </summary>
        public short delay;

        /// <summary>
        /// The number of times this cycler has been paused.
        /// </summary>
        public ushort numTimesPaused;
    }

    internal class GfxPalette32
    {
        private readonly ResourceManager _resMan;
        /**
         * The palette revision version. Increments once per game
         * loop that changes the source palette.
         */
        private uint _version;

        /**
         * Whether or not the hardware palette needs updating.
         */
        private bool _needsUpdate;

        /**
         * The currently displayed palette.
         */
        private Palette _currentPalette;

        /**
         * The unmodified source palette loaded by kPalette. Additional
         * palette entries may be mixed into the source palette by
         * CelObj objects, which contain their own palettes.
         */
        private Palette _sourcePalette = new Palette();

        /**
         * The palette to be used when the hardware is next updated.
         * On update, _nextPalette is transferred to _currentPalette.
         */
        private Palette _nextPalette;

        /**
         * The fade table records the expected intensity level of each pixel
         * in the palette that will be displayed on the next frame.
         */
        private readonly ushort[] _fadeTable = new ushort[256];

        /**
         * An optional palette used to describe the source colors used
         * in a palette vary operation. If this palette is not specified,
         * sourcePalette is used instead.
         */
        private Palette _varyStartPalette;

        /**
         * An optional palette used to describe the target colors used
         * in a palette vary operation.
         */
        private Palette _varyTargetPalette;

        /**
         * The minimum palette index that has been varied from the
         * source palette. 0–255
         */
        private byte _varyFromColor;

        /**
         * The maximum palette index that is has been varied from the
         * source palette. 0-255
         */
        private byte _varyToColor;

        /**
         * The tick at the last time the palette vary was updated.
         */
        private uint _varyLastTick;

        /**
         * The amount of time to elapse, in ticks, between each cycle
         * of a palette vary animation.
         */
        private int _varyTime;

        /**
         * The direction of change: -1, 0, or 1.
         */
        private short _varyDirection;

        /**
         * The amount, in percent, that the vary color is currently
         * blended into the source color.
         */
        private short _varyPercent;

        /**
         * The target amount that a vary color will be blended into
         * the source color.
         */
        private short _varyTargetPercent;

        /**
         * The number of time palette varying has been paused.
         */
        private ushort _varyNumTimesPaused;

        /**
         * The cycle map is used to detect overlapping cyclers.
         * According to SCI engine code, when two cyclers overlap,
         * a fatal error has occurred and the engine will display
         * an error and then exit.
         *
         * The cycle map is also by the color remapping system to
         * avoid attempting to remap to palette entries that are
         * cycling (so won't be the expected color once the cycler
         * runs again).
         */
        private bool[] _cycleMap = new bool[256];
        // SQ6 defines 10 cyclers
        private PalCycler[] _cyclers = new PalCycler[10];

        public Palette CurrentPalette => _currentPalette;
        public Palette NextPalette => _nextPalette;
        public bool[] CycleMap => _cycleMap;

        public GfxPalette32(ResourceManager resMan)
        {
            _resMan = resMan;
            _version = 1;
            _varyToColor = 255;
            for (int i = 0, len = _fadeTable.Length; i < len; ++i)
            {
                _fadeTable[i] = 100;
            }

            LoadPalette(999);
        }

        private bool LoadPalette(int resourceId)
        {
            var palResource = _resMan.FindResource(new ResourceId(ResourceType.Palette, (ushort) resourceId), false);

            if (palResource == null)
            {
                return false;
            }

            var palette = new HunkPalette(palResource.data);
            Submit(palette);
            return true;
        }

        public void Submit(Palette palette)
        {
            Palette oldSourcePalette = new Palette(_sourcePalette);
            MergePaletteInternal(_sourcePalette, palette);

            if (!_needsUpdate && _sourcePalette != oldSourcePalette)
            {
                ++_version;
                _needsUpdate = true;
            }
        }

        public void Submit(HunkPalette hunkPalette)
        {
            if (hunkPalette.GetVersion() == _version)
            {
                return;
            }

            var oldSourcePalette = new Palette(_sourcePalette);
            var palette = hunkPalette.ToPalette();
            MergePaletteInternal(_sourcePalette, palette);

            if (!_needsUpdate && oldSourcePalette != _sourcePalette)
            {
                ++_version;
                _needsUpdate = true;
            }

            hunkPalette.SetVersion(_version);
        }

        private void MergePaletteInternal(Palette to, Palette from)
        {
            // The last color is always white, so it is not copied.
            // (Some palettes try to set the last color, which causes
            // churning in the palettes when they are merged)
            for (int i = 0, len = to.colors.Length - 1; i < len; ++i)
            {
                if (from.colors[i].used != 0)
                {
                    to.colors[i] = from.colors[i];
                }
            }
        }

        internal void SaveLoadWithSerializer(Serializer s)
        {
            throw new NotImplementedException();
        }

        public void KernelPalVarySet(int paletteId, short percent, int time, short fromColor, short toColor)
        {
            var palette = GetPaletteFromResourceInternal(paletteId);
            SetVary(palette, percent, time, fromColor, toColor);
        }

        private Palette GetPaletteFromResourceInternal(int resourceId)
        {
            var palResource = _resMan.FindResource(new ResourceId(ResourceType.Palette, (ushort) resourceId), false);

            if (palResource == null)
            {
                Error("Could not load vary palette {0}", resourceId);
            }

            var rawPalette = new HunkPalette(palResource.data);
            return rawPalette.ToPalette();
        }

        private void SetVary(Palette target, short percent, int time, short fromColor, short toColor)
        {
            SetTarget(target);
            SetVaryTimeInternal(percent, time);

            if (fromColor > -1)
            {
                _varyFromColor = (byte) fromColor;
            }
            if (toColor > -1)
            {
                System.Diagnostics.Debug.Assert(toColor < 256);
                _varyToColor = (byte) toColor;
            }
        }

        private void SetTarget(Palette palette)
        {
            _varyTargetPalette = new Palette(palette);
        }

        private void SetVaryTimeInternal(short percent, int time)
        {
            _varyLastTick = SciEngine.Instance.TickCount;
            if (time == 0 || _varyPercent == percent)
            {
                _varyDirection = 0;
                _varyTargetPercent = _varyPercent = percent;
            }
            else
            {
                _varyTime = time / (percent - _varyPercent);
                _varyTargetPercent = percent;

                if (_varyTime > 0)
                {
                    _varyDirection = 1;
                }
                else if (_varyTime < 0)
                {
                    _varyDirection = -1;
                    _varyTime = -_varyTime;
                }
                else
                {
                    _varyDirection = 0;
                    _varyTargetPercent = _varyPercent = percent;
                }
            }
        }

        public void SetVaryPercent(short percent, int time, short fromColor, short fromColorAlternate)
        {
            if (_varyTargetPalette != null)
            {
                SetVaryTimeInternal(percent, time);
            }

            // This looks like a mistake in the actual SCI engine (both SQ6 and Lighthouse);
            // the values are always hardcoded to -1 in kPalVary, so this code can never
            // actually be executed
            if (fromColor > -1)
            {
                _varyFromColor = (byte) fromColor;
            }
            if (fromColorAlternate > -1)
            {
                _varyFromColor = (byte) fromColorAlternate;
            }
        }

        public short GetVaryPercent()
        {
            return Math.Abs(_varyPercent);
        }

        public void VaryOff()
        {
            _varyNumTimesPaused = 0;
            _varyPercent = _varyTargetPercent = 0;
            _varyFromColor = 0;
            _varyToColor = 255;
            _varyDirection = 0;

            _varyTargetPalette = null;
            _varyStartPalette = null;
        }

        public void KernelPalVaryMergeTarget(int paletteId)
        {
            var palette = GetPaletteFromResourceInternal(paletteId);
            MergeTarget(palette);
        }

        private void MergeTarget(Palette palette)
        {
            if (_varyTargetPalette != null)
            {
                MergePaletteInternal(_varyTargetPalette, palette);
            }
            else
            {
                _varyTargetPalette = new Palette(palette);
            }
        }

        public void SetVaryTime(int time)
        {
            if (_varyTargetPalette == null)
            {
                return;
            }
            SetVaryTimeInternal(_varyTargetPercent, time);
        }

        public void KernelPalVarySetTarget(int paletteId)
        {
            var palette = GetPaletteFromResourceInternal(paletteId);
            SetTarget(palette);
        }

        public void KernelPalVarySetStart(int paletteId)
        {
            var palette = GetPaletteFromResourceInternal(paletteId);
            SetStart(palette);
        }

        private void SetStart(Palette palette)
        {
            _varyStartPalette = new Palette(palette);
        }

        public void KernelPalVaryMergeStart(int paletteId)
        {
            var palette = GetPaletteFromResourceInternal(paletteId);
            MergeStart(palette);
        }

        private void MergeStart(Palette palette)
        {
            if (_varyStartPalette != null)
            {
                MergePaletteInternal(_varyStartPalette, palette);
            }
            else
            {
                _varyStartPalette = new Palette(palette);
            }
        }

        // NOTE: There are some game scripts (like SQ6 Sierra logo and main menu) that call
        // setFade with numColorsToFade set to 256, but other parts of the engine like
        // processShowStyleNone use 255 instead of 256. It is not clear if this is because
        // the last palette entry is intentionally left unmodified, or if this is a bug
        // in the engine. It certainly seems confused because all other places that accept
        // color ranges typically receive values in the range of 0–255.
        public void SetFade(ushort percent, byte fromColor, ushort numColorsToFade)
        {
            if (fromColor > numColorsToFade)
            {
                return;
            }

            System.Diagnostics.Debug.Assert(numColorsToFade <= _fadeTable.Length);

            for (int i = fromColor; i < numColorsToFade; i++)
                _fadeTable[i] = percent;
        }

        public bool UpdateForFrame()
        {
            ApplyAll();
            _needsUpdate = false;
            return SciEngine.Instance._gfxRemap32.RemapAllTables(_nextPalette != _currentPalette);
        }

        public void UpdateFFrame()
        {
            for (int i = 0; i < _nextPalette.colors.Length; ++i)
            {
                _nextPalette.colors[i] = _sourcePalette.colors[i];
            }
            _needsUpdate = false;
            SciEngine.Instance._gfxRemap32.RemapAllTables(_nextPalette != _currentPalette);
        }

        public void UpdateHardware(bool updateScreen = true)
        {
            if (_currentPalette == _nextPalette)
            {
                return;
            }

            var bpal = new Core.Graphics.Color[256];

            for (int i = 0; i < _currentPalette.colors.Length - 1; ++i)
            {
                _currentPalette.colors[i] = _nextPalette.colors[i];

                // NOTE: If the brightness option in the user configuration file is set,
                // SCI engine adjusts palette brightnesses here by mapping RGB values to values
                // in some hard-coded brightness tables. There is no reason on modern hardware
                // to implement this, unless it is discovered that some game uses a non-standard
                // brightness setting by default

                // All color entries MUST be copied, not just "used" entries, otherwise
                // uninitialised memory from bpal makes its way into the system palette.
                // This would not normally be a problem, except that games sometimes use
                // unused palette entries. e.g. Phant1 title screen references palette
                // entries outside its own palette, so will render garbage colors where
                // the game expects them to be black
                bpal[i] = Core.Graphics.Color.FromRgb(_currentPalette.colors[i].R,
                    _currentPalette.colors[i].G, _currentPalette.colors[i].B);
            }

            // The last color must always be white
            bpal[255].R = 255;
            bpal[255].G = 255;
            bpal[255].B = 255;

            SciEngine.Instance.System.GraphicsManager.SetPalette(bpal, 0, 256);
            if (updateScreen)
            {
                SciEngine.Instance.EventManager.UpdateScreen();
            }
        }

        private void ApplyAll()
        {
            ApplyVary();
            ApplyCycles();
            ApplyFade();
        }

        private void ApplyVary()
        {
            while (SciEngine.Instance.TickCount - _varyLastTick > (uint) _varyTime && _varyDirection != 0)
            {
                _varyLastTick = (uint) (_varyLastTick + _varyTime);

                if (_varyPercent == _varyTargetPercent)
                {
                    _varyDirection = 0;
                }

                _varyPercent += _varyDirection;
            }

            if (_varyPercent == 0 || _varyTargetPalette == null)
            {
                for (int i = 0, len = _nextPalette.colors.Length; i < len; ++i)
                {
                    if (_varyStartPalette != null && i >= _varyFromColor && i <= _varyToColor)
                    {
                        _nextPalette.colors[i] = _varyStartPalette.colors[i];
                    }
                    else
                    {
                        _nextPalette.colors[i] = _sourcePalette.colors[i];
                    }
                }
            }
            else
            {
                for (int i = 0, len = _nextPalette.colors.Length; i < len; ++i)
                {
                    if (i >= _varyFromColor && i <= _varyToColor)
                    {
                        Color targetColor = _varyTargetPalette.colors[i];
                        Color sourceColor;

                        if (_varyStartPalette != null)
                        {
                            sourceColor = _varyStartPalette.colors[i];
                        }
                        else
                        {
                            sourceColor = _sourcePalette.colors[i];
                        }

                        Color computedColor;

                        int color;
                        color = targetColor.R - sourceColor.R;
                        computedColor.R = (byte) ((color * _varyPercent / 100) + sourceColor.R);
                        color = targetColor.G - sourceColor.G;
                        computedColor.G = (byte) (((color * _varyPercent) / 100) + sourceColor.G);
                        color = targetColor.B - sourceColor.B;
                        computedColor.B = (byte) (((color * _varyPercent) / 100) + sourceColor.B);
                        computedColor.used = sourceColor.used;

                        _nextPalette.colors[i] = computedColor;
                    }
                    else
                    {
                        _nextPalette.colors[i] = _sourcePalette.colors[i];
                    }
                }
            }
        }

        private void ApplyCycles()
        {
            Color[] paletteCopy = new Color[256];
            Array.Copy(_nextPalette.colors, paletteCopy, 256);

            for (int i = 0, len = _cyclers.Length; i < len; ++i)
            {
                PalCycler cycler = _cyclers[i];
                if (cycler == null)
                {
                    continue;
                }

                if (cycler.delay != 0 && cycler.numTimesPaused == 0)
                {
                    while (cycler.delay + cycler.lastUpdateTick < SciEngine.Instance.TickCount)
                    {
                        DoCycleInternal(cycler, 1);
                        cycler.lastUpdateTick = (uint) (cycler.lastUpdateTick + cycler.delay);
                    }
                }

                for (int j = 0; j < cycler.numColorsToCycle; j++)
                {
                    _nextPalette.colors[cycler.fromColor + j] =
                        paletteCopy[cycler.fromColor + (cycler.currentCycle + j) % cycler.numColorsToCycle];
                }
            }
        }

        private void ApplyFade()
        {
            for (int i = 0; i < _fadeTable.Length; ++i)
            {
                if (_fadeTable[i] == 100)
                    continue;

                Color color = _nextPalette.colors[i];

                color.R = (byte) Math.Min(255, color.R * _fadeTable[i] / 100);
                color.G = (byte) Math.Min(255, color.G * _fadeTable[i] / 100);
                color.B = (byte) Math.Min(255, color.B * _fadeTable[i] / 100);
            }
        }

        private void DoCycleInternal(PalCycler cycler, short speed)
        {
            short currentCycle = cycler.currentCycle;
            ushort numColorsToCycle = cycler.numColorsToCycle;

            if (cycler.direction == 0)
            {
                currentCycle = (short) ((currentCycle - (speed % numColorsToCycle)) + numColorsToCycle);
            }
            else
            {
                currentCycle = (short) (currentCycle + speed);
            }

            cycler.currentCycle = (byte) (currentCycle % numColorsToCycle);
        }

        public void SetCycle(ushort fromColor, ushort toColor, short direction, ushort delay)
        {
            System.Diagnostics.Debug.Assert(fromColor < toColor);

            int cyclerIndex;
            int numCyclers = _cyclers.Length;

            PalCycler cycler = GetCycler(fromColor);

            if (cycler != null)
            {
                ClearCycleMap(fromColor, cycler.numColorsToCycle);
            }
            else
            {
                for (cyclerIndex = 0; cyclerIndex < numCyclers; ++cyclerIndex)
                {
                    if (_cyclers[cyclerIndex] == null)
                    {
                        cycler = new PalCycler();
                        _cyclers[cyclerIndex] = cycler;
                        break;
                    }
                }
            }

            // SCI engine overrides the first oldest cycler that it finds where
            // “oldest” is determined by the difference between the tick and now
            if (cycler == null)
            {
                uint now = SciEngine.Instance.TickCount;
                uint minUpdateDelta = 0xFFFFFFFF;

                for (cyclerIndex = 0; cyclerIndex < numCyclers; ++cyclerIndex)
                {
                    PalCycler candidate = _cyclers[cyclerIndex];

                    uint updateDelta = now - candidate.lastUpdateTick;
                    if (updateDelta < minUpdateDelta)
                    {
                        minUpdateDelta = updateDelta;
                        cycler = candidate;
                    }
                }

                ClearCycleMap(cycler.fromColor, cycler.numColorsToCycle);
            }

            ushort numColorsToCycle = (ushort) (1 + ((byte) toColor) - fromColor);
            cycler.fromColor = (byte) fromColor;
            cycler.numColorsToCycle = (byte) numColorsToCycle;
            cycler.currentCycle = (byte) fromColor;
            cycler.direction = direction < 0 ? PalCyclerDirection.PalCycleBackward : PalCyclerDirection.PalCycleForward;
            cycler.delay = (short) delay;
            cycler.lastUpdateTick = SciEngine.Instance.TickCount;
            cycler.numTimesPaused = 0;

            SetCycleMap(fromColor, numColorsToCycle);
        }

        private void SetCycleMap(ushort fromColor, ushort numColorsToSet)
        {
            Ptr<bool> mapEntry = new Ptr<bool>(_cycleMap, fromColor);
            Ptr<bool> lastEntry = new Ptr<bool>(_cycleMap, numColorsToSet);
            while (mapEntry.Offset < lastEntry.Offset)
            {
                if (mapEntry[0])
                {
                    Error("Cycles intersect");
                }
                mapEntry[0] = true;
                mapEntry.Offset++;
            }
        }

        private PalCycler GetCycler(ushort fromColor)
        {
            int numCyclers = _cyclers.Length;

            for (int cyclerIndex = 0; cyclerIndex < numCyclers; ++cyclerIndex)
            {
                PalCycler cycler = _cyclers[cyclerIndex];
                if (cycler != null && cycler.fromColor == fromColor)
                {
                    return cycler;
                }
            }

            return null;
        }

        private void ClearCycleMap(ushort fromColor, ushort numColorsToClear)
        {
            var mapEntry = new Ptr<bool>(_cycleMap, fromColor);
            var lastEntry = new Ptr<bool>(_cycleMap, numColorsToClear);
            while (mapEntry.Offset < lastEntry.Offset)
            {
                mapEntry[0] = false;
                mapEntry.Offset++;
            }
        }

        public void DoCycle(byte fromColor, short speed)
        {
            PalCycler cycler = GetCycler(fromColor);
            if (cycler != null)
            {
                cycler.lastUpdateTick = SciEngine.Instance.TickCount;
                DoCycleInternal(cycler, speed);
            }
        }

        public void CycleAllPause()
        {
            // NOTE: The original engine did not check for null pointers in the
            // palette cyclers pointer array.
            for (int i = 0, len = _cyclers.Length; i < len; ++i)
            {
                PalCycler cycler = _cyclers[i];
                if (cycler != null)
                {
                    // This seems odd, because currentCycle is 0..numColorsPerCycle,
                    // but fromColor is 0..255. When applyAllCycles runs, the values
                    // end up back in range
                    cycler.currentCycle = cycler.fromColor;
                }
            }

            ApplyAllCycles();

            for (int i = 0, len = _cyclers.Length; i < len; ++i)
            {
                PalCycler cycler = _cyclers[i];
                if (cycler != null)
                {
                    ++cycler.numTimesPaused;
                }
            }
        }

        public void CyclePause(byte fromColor)
        {
            PalCycler cycler = GetCycler(fromColor);
            if (cycler != null)
            {
                ++cycler.numTimesPaused;
            }
        }

        public void CycleAllOn()
        {
            for (int i = 0, len = _cyclers.Length; i < len; ++i)
            {
                PalCycler cycler = _cyclers[i];
                if (cycler != null && cycler.numTimesPaused > 0)
                {
                    --cycler.numTimesPaused;
                }
            }
        }

        public void CycleOff(byte fromColor)
        {
            for (int i = 0, len = _cyclers.Length; i < len; ++i)
            {
                PalCycler cycler = _cyclers[i];
                if (cycler != null && cycler.fromColor == fromColor)
                {
                    ClearCycleMap(fromColor, cycler.numColorsToCycle);
                    _cyclers[i] = null;
                    break;
                }
            }
        }

        public void CycleAllOff()
        {
            for (int i = 0, len = _cyclers.Length; i < len; ++i)
            {
                PalCycler cycler = _cyclers[i];
                if (cycler != null)
                {
                    ClearCycleMap(cycler.fromColor, cycler.numColorsToCycle);
                    _cyclers[i] = null;
                }
            }
        }

        public void CycleOn(byte fromColor)
        {
            PalCycler cycler = GetCycler(fromColor);
            if (cycler != null && cycler.numTimesPaused > 0)
            {
                --cycler.numTimesPaused;
            }
        }

        private void ApplyAllCycles()
        {
            Color[] paletteCopy = new Color[256];
            Array.Copy(_nextPalette.colors, paletteCopy, 256);

            for (int cyclerIndex = 0, numCyclers = _cyclers.Length; cyclerIndex < numCyclers; ++cyclerIndex)
            {
                PalCycler cycler = _cyclers[cyclerIndex];
                if (cycler != null)
                {
                    cycler.currentCycle = (byte) ((cycler.currentCycle + 1) % cycler.numColorsToCycle);
                    // Disassembly was not fully evaluated to verify this is exactly the same
                    // as the code from applyCycles, but it appeared to be at a glance
                    for (int j = 0; j < cycler.numColorsToCycle; j++)
                    {
                        _nextPalette.colors[cycler.fromColor + j] =
                            paletteCopy[cycler.fromColor + (cycler.currentCycle + j) % cycler.numColorsToCycle];
                    }
                }
            }
        }
    }
}