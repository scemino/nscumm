//
//  BReverbModel.cs
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

#if MT32EMU_USE_FLOAT_SAMPLES
using Sample = System.Single;
using SampleEx = System.Single;
#else
using Sample = System.Int16;
using SampleEx = System.Int32;
#endif

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class BReverbSettings
    {
        public readonly int numberOfAllpasses;
        public readonly int[] allpassSizes;
        public readonly int numberOfCombs;
        public readonly int[] combSizes;
        public readonly int[] outLPositions;
        public readonly int[] outRPositions;
        public readonly uint[] filterFactors;
        public readonly uint[] feedbackFactors;
        public readonly uint[] dryAmps;
        public readonly uint[] wetLevels;
        public readonly uint lpfAmp;

        public BReverbSettings(int numberOfAllpasses, int[] allpassSizes, int numberOfCombs, int[] combSizes,
                       int[] outLPositions, int[] outRPositions, uint[] filterFactors, uint[] feedbackFactors,
                       uint[] dryAmps, uint[] wetLevels, uint lpfAmp)
        {
            this.numberOfAllpasses = numberOfAllpasses;
            this.allpassSizes = allpassSizes;
            this.numberOfCombs = numberOfCombs;
            this.combSizes = combSizes;
            this.outLPositions = outLPositions;
            this.outRPositions = outRPositions;
            this.filterFactors = filterFactors;
            this.feedbackFactors = feedbackFactors;
            this.dryAmps = dryAmps;
            this.wetLevels = wetLevels;
            this.lpfAmp = lpfAmp;
        }
    }

    static class Mt32Emu
    {
        // Sample rate to use in mixing. With the progress of development, we've found way too many thing dependent.
        // In order to achieve further advance in emulation accuracy, sample rate made fixed throughout the emulator,
        // except the emulation of analogue path.
        // The output from the synth is supposed to be resampled externally in order to convert to the desired sample rate.
        public const int SAMPLE_RATE = 32000;

        // The default value for the maximum number of partials playing simultaneously.
        public const int DEFAULT_MAX_PARTIALS = 32;

        // The higher this number, the more memory will be used, but the more samples can be processed in one run -
        // various parts of sample generation can be processed more efficiently in a single run.
        // A run's maximum length is that given to Synth::render(), so giving a value here higher than render() is ever
        // called with will give no gain (but simply waste the memory).
        // Note that this value does *not* in any way impose limitations on the length given to render(), and has no effect
        // on the generated audio.
        // This value must be >= 1.
        public const int MAX_SAMPLES_PER_RUN = 4096;

        // The default size of the internal MIDI event queue.
        // It holds the incoming MIDI events before the rendering engine actually processes them.
        // The main goal is to fairly emulate the real hardware behaviour which obviously
        // uses an internal MIDI event queue to gather incoming data as well as the delays
        // introduced by transferring data via the MIDI interface.
        // This also facilitates building of an external rendering loop
        // as the queue stores timestamped MIDI events.
        public const int DEFAULT_MIDI_EVENT_QUEUE_SIZE = 1024;

        // Because LA-32 chip makes it's output available to process by the Boss chip with a significant delay,
        // the Boss chip puts to the buffer the LA32 dry output when it is ready and performs processing of the _previously_ latched data.
        // Of course, the right way would be to use a dedicated variable for this, but our reverb model is way higher level,
        // so we can simply increase the input buffer size.
        public const int PROCESS_DELAY = 1;

        public const int MODE_3_ADDITIONAL_DELAY = 1;
        public const int MODE_3_FEEDBACK_DELAY = 1;

        // MIDI interface data transfer rate in samples. Used to simulate the transfer delay.
        public const double MIDI_DATA_TRANSFER_RATE = SAMPLE_RATE / 31250.0 * 8.0;

        // MT32EMU_MEMADDR() converts from sysex-padded, MT32EMU_SYSEXMEMADDR converts to it
        // Roland provides documentation using the sysex-padded addresses, so we tend to use that in code and output
        public static int MT32EMU_MEMADDR(int x) { return ((((x) & 0x7f0000) >> 2) | (((x) & 0x7f00) >> 1) | ((x) & 0x7f)); }
        public static int MT32EMU_SYSEXMEMADDR(int x) { return ((((x) & 0x1FC000) << 2) | (((x) & 0x3F80) << 1) | ((x) & 0x7f)); }
    }

    class BReverbModel
    {
        static class Mt32Settings
        {
            static readonly int MODE_0_NUMBER_OF_ALLPASSES = 3;
            static readonly int[] MODE_0_ALLPASSES = { 994, 729, 78 };
            static readonly int MODE_0_NUMBER_OF_COMBS = 4; // Same as above in the new model implementation
            static readonly int[] MODE_0_COMBS = { 575 + Mt32Emu.PROCESS_DELAY, 2040, 2752, 3629 };
            static readonly int[] MODE_0_OUTL = { 2040, 687, 1814 };
            static readonly int[] MODE_0_OUTR = { 1019, 2072, 1 };
            static readonly uint[] MODE_0_COMB_FACTOR = { 0xB0, 0x60, 0x60, 0x60 };
            static readonly uint[] MODE_0_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x28, 0x48, 0x60, 0x70, 0x78, 0x80, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98};
            static readonly uint[] MODE_0_DRY_AMP = { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 };
            static readonly uint[] MODE_0_WET_AMP = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x70, 0xA0, 0xE0 };
            static readonly uint MODE_0_LPF_AMP = 0x80;

            static readonly int MODE_1_NUMBER_OF_ALLPASSES = 3;
            static readonly int[] MODE_1_ALLPASSES = { 1324, 809, 176 };
            static readonly int MODE_1_NUMBER_OF_COMBS = 4; // Same as above in the new model implementation
            static readonly int[] MODE_1_COMBS = { 961 + Mt32Emu.PROCESS_DELAY, 2619, 3545, 4519 };
            static readonly int[] MODE_1_OUTL = { 2618, 1760, 4518 };
            static readonly int[] MODE_1_OUTR = { 1300, 3532, 2274 };
            static readonly uint[] MODE_1_COMB_FACTOR = { 0x90, 0x60, 0x60, 0x60 };
            static readonly uint[] MODE_1_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x28, 0x48, 0x60, 0x70, 0x78, 0x80, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98};
            static readonly uint[] MODE_1_DRY_AMP = { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 };
            static readonly uint[] MODE_1_WET_AMP = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x70, 0xA0, 0xE0 };
            static readonly uint MODE_1_LPF_AMP = 0x80;

            static readonly int MODE_2_NUMBER_OF_ALLPASSES = 3;
            static readonly int[] MODE_2_ALLPASSES = { 969, 644, 157 };
            static readonly int MODE_2_NUMBER_OF_COMBS = 4; // Same as above in the new model implementation
            static readonly int[] MODE_2_COMBS = { 116 + Mt32Emu.PROCESS_DELAY, 2259, 2839, 3539 };
            static readonly int[] MODE_2_OUTL = { 2259, 718, 1769 };
            static readonly int[] MODE_2_OUTR = { 1136, 2128, 1 };
            static readonly uint[] MODE_2_COMB_FACTOR = { 0, 0x60, 0x60, 0x60 };
            static readonly uint[] MODE_2_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x28, 0x48, 0x60, 0x70, 0x78, 0x80, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98};
            static readonly uint[] MODE_2_DRY_AMP = { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 };
            static readonly uint[] MODE_2_WET_AMP = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x70, 0xA0, 0xE0 };
            static readonly uint MODE_2_LPF_AMP = 0x80;

            static readonly int MODE_3_NUMBER_OF_ALLPASSES = 0;
            static readonly int MODE_3_NUMBER_OF_COMBS = 1;
            static readonly int[] MODE_3_DELAY = { 16000 + Mt32Emu.MODE_3_FEEDBACK_DELAY + Mt32Emu.PROCESS_DELAY + Mt32Emu.MODE_3_ADDITIONAL_DELAY };
            static readonly int[] MODE_3_OUTL = { 400, 624, 960, 1488, 2256, 3472, 5280, 8000 };
            static readonly int[] MODE_3_OUTR = { 800, 1248, 1920, 2976, 4512, 6944, 10560, 16000 };
            static readonly uint[] MODE_3_COMB_FACTOR = { 0x68 };
            static readonly uint[] MODE_3_COMB_FEEDBACK = { 0x68, 0x60 };
            static readonly uint[] MODE_3_DRY_AMP = {0x10, 0x10, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20,
                                            0x10, 0x20, 0x20, 0x10, 0x20, 0x10, 0x20, 0x10};
            static readonly uint[] MODE_3_WET_AMP = { 0x08, 0x18, 0x28, 0x40, 0x60, 0x80, 0xA8, 0xF8 };

            static readonly BReverbSettings REVERB_MODE_0_SETTINGS = new BReverbSettings(MODE_0_NUMBER_OF_ALLPASSES, MODE_0_ALLPASSES, MODE_0_NUMBER_OF_COMBS, MODE_0_COMBS, MODE_0_OUTL, MODE_0_OUTR, MODE_0_COMB_FACTOR, MODE_0_COMB_FEEDBACK, MODE_0_DRY_AMP, MODE_0_WET_AMP, MODE_0_LPF_AMP);
            static readonly BReverbSettings REVERB_MODE_1_SETTINGS = new BReverbSettings(MODE_1_NUMBER_OF_ALLPASSES, MODE_1_ALLPASSES, MODE_1_NUMBER_OF_COMBS, MODE_1_COMBS, MODE_1_OUTL, MODE_1_OUTR, MODE_1_COMB_FACTOR, MODE_1_COMB_FEEDBACK, MODE_1_DRY_AMP, MODE_1_WET_AMP, MODE_1_LPF_AMP);
            static readonly BReverbSettings REVERB_MODE_2_SETTINGS = new BReverbSettings(MODE_2_NUMBER_OF_ALLPASSES, MODE_2_ALLPASSES, MODE_2_NUMBER_OF_COMBS, MODE_2_COMBS, MODE_2_OUTL, MODE_2_OUTR, MODE_2_COMB_FACTOR, MODE_2_COMB_FEEDBACK, MODE_2_DRY_AMP, MODE_2_WET_AMP, MODE_2_LPF_AMP);
            static readonly BReverbSettings REVERB_MODE_3_SETTINGS = new BReverbSettings(MODE_3_NUMBER_OF_ALLPASSES, null, MODE_3_NUMBER_OF_COMBS, MODE_3_DELAY, MODE_3_OUTL, MODE_3_OUTR, MODE_3_COMB_FACTOR, MODE_3_COMB_FEEDBACK, MODE_3_DRY_AMP, MODE_3_WET_AMP, 0);

            public static readonly BReverbSettings[] REVERB_SETTINGS = { REVERB_MODE_0_SETTINGS, REVERB_MODE_1_SETTINGS, REVERB_MODE_2_SETTINGS, REVERB_MODE_3_SETTINGS };
        }

        static class Cm32lLapcSettings
        {
            static readonly int MODE_0_NUMBER_OF_ALLPASSES = 3;
            static readonly int[] MODE_0_ALLPASSES = { 994, 729, 78 };
            static readonly int MODE_0_NUMBER_OF_COMBS = 4; // Well, actually there are 3 comb filters, but the entrance LPF + delay can be processed via a hacked comb.
            static readonly int[] MODE_0_COMBS = { 705 + Mt32Emu.PROCESS_DELAY, 2349, 2839, 3632 };
            static readonly int[] MODE_0_OUTL = { 2349, 141, 1960 };
            static readonly int[] MODE_0_OUTR = { 1174, 1570, 145 };
            static readonly uint[] MODE_0_COMB_FACTOR = { 0xA0, 0x60, 0x60, 0x60 };
            static readonly uint[] MODE_0_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98};
            static readonly uint[] MODE_0_DRY_AMP = { 0xA0, 0xA0, 0xA0, 0xA0, 0xB0, 0xB0, 0xB0, 0xD0 };
            static readonly uint[] MODE_0_WET_AMP = { 0x10, 0x30, 0x50, 0x70, 0x90, 0xC0, 0xF0, 0xF0 };
            static readonly uint MODE_0_LPF_AMP = 0x60;

            static readonly int MODE_1_NUMBER_OF_ALLPASSES = 3;
            static readonly int[] MODE_1_ALLPASSES = { 1324, 809, 176 };
            static readonly int MODE_1_NUMBER_OF_COMBS = 4; // Same as for mode 0 above
            static readonly int[] MODE_1_COMBS = { 961 + Mt32Emu.PROCESS_DELAY, 2619, 3545, 4519 };
            static readonly int[] MODE_1_OUTL = { 2618, 1760, 4518 };
            static readonly int[] MODE_1_OUTR = { 1300, 3532, 2274 };
            static readonly uint[] MODE_1_COMB_FACTOR = { 0x80, 0x60, 0x60, 0x60 };
            static readonly uint[] MODE_1_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x28, 0x48, 0x60, 0x70, 0x78, 0x80, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98};
            static readonly uint[] MODE_1_DRY_AMP = { 0xA0, 0xA0, 0xB0, 0xB0, 0xB0, 0xB0, 0xB0, 0xE0 };
            static readonly uint[] MODE_1_WET_AMP = { 0x10, 0x30, 0x50, 0x70, 0x90, 0xC0, 0xF0, 0xF0 };
            static readonly uint MODE_1_LPF_AMP = 0x60;

            static readonly int MODE_2_NUMBER_OF_ALLPASSES = 3;
            static readonly int[] MODE_2_ALLPASSES = { 969, 644, 157 };
            static readonly int MODE_2_NUMBER_OF_COMBS = 4; // Same as for mode 0 above
            static readonly int[] MODE_2_COMBS = { 116 + Mt32Emu.PROCESS_DELAY, 2259, 2839, 3539 };
            static readonly int[] MODE_2_OUTL = { 2259, 718, 1769 };
            static readonly int[] MODE_2_OUTR = { 1136, 2128, 1 };
            static readonly uint[] MODE_2_COMB_FACTOR = { 0, 0x20, 0x20, 0x20 };
            static readonly uint[] MODE_2_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x30, 0x58, 0x78, 0x88, 0xA0, 0xB8, 0xC0, 0xD0,
                                                  0x30, 0x58, 0x78, 0x88, 0xA0, 0xB8, 0xC0, 0xD0,
                                                  0x30, 0x58, 0x78, 0x88, 0xA0, 0xB8, 0xC0, 0xD0};
            static readonly uint[] MODE_2_DRY_AMP = { 0xA0, 0xA0, 0xB0, 0xB0, 0xB0, 0xB0, 0xC0, 0xE0 };
            static readonly uint[] MODE_2_WET_AMP = { 0x10, 0x30, 0x50, 0x70, 0x90, 0xC0, 0xF0, 0xF0 };
            static readonly uint MODE_2_LPF_AMP = 0x80;

            static readonly int MODE_3_NUMBER_OF_ALLPASSES = 0;
            static readonly int MODE_3_NUMBER_OF_COMBS = 1;
            static readonly int[] MODE_3_DELAY = { 16000 + Mt32Emu.MODE_3_FEEDBACK_DELAY + Mt32Emu.PROCESS_DELAY + Mt32Emu.MODE_3_ADDITIONAL_DELAY };
            static readonly int[] MODE_3_OUTL = { 400, 624, 960, 1488, 2256, 3472, 5280, 8000 };
            static readonly int[] MODE_3_OUTR = { 800, 1248, 1920, 2976, 4512, 6944, 10560, 16000 };
            static readonly uint[] MODE_3_COMB_FACTOR = { 0x68 };
            static readonly uint[] MODE_3_COMB_FEEDBACK = { 0x68, 0x60 };
            static readonly uint[] MODE_3_DRY_AMP = {0x20, 0x50, 0x50, 0x50, 0x50, 0x50, 0x50, 0x50,
                                            0x20, 0x50, 0x50, 0x50, 0x50, 0x50, 0x50, 0x50};
            static readonly uint[] MODE_3_WET_AMP = { 0x18, 0x18, 0x28, 0x40, 0x60, 0x80, 0xA8, 0xF8 };

            static readonly BReverbSettings REVERB_MODE_0_SETTINGS = new BReverbSettings(MODE_0_NUMBER_OF_ALLPASSES, MODE_0_ALLPASSES, MODE_0_NUMBER_OF_COMBS, MODE_0_COMBS, MODE_0_OUTL, MODE_0_OUTR, MODE_0_COMB_FACTOR, MODE_0_COMB_FEEDBACK, MODE_0_DRY_AMP, MODE_0_WET_AMP, MODE_0_LPF_AMP);
            static readonly BReverbSettings REVERB_MODE_1_SETTINGS = new BReverbSettings(MODE_1_NUMBER_OF_ALLPASSES, MODE_1_ALLPASSES, MODE_1_NUMBER_OF_COMBS, MODE_1_COMBS, MODE_1_OUTL, MODE_1_OUTR, MODE_1_COMB_FACTOR, MODE_1_COMB_FEEDBACK, MODE_1_DRY_AMP, MODE_1_WET_AMP, MODE_1_LPF_AMP);
            static readonly BReverbSettings REVERB_MODE_2_SETTINGS = new BReverbSettings(MODE_2_NUMBER_OF_ALLPASSES, MODE_2_ALLPASSES, MODE_2_NUMBER_OF_COMBS, MODE_2_COMBS, MODE_2_OUTL, MODE_2_OUTR, MODE_2_COMB_FACTOR, MODE_2_COMB_FEEDBACK, MODE_2_DRY_AMP, MODE_2_WET_AMP, MODE_2_LPF_AMP);
            static readonly BReverbSettings REVERB_MODE_3_SETTINGS = new BReverbSettings(MODE_3_NUMBER_OF_ALLPASSES, null, MODE_3_NUMBER_OF_COMBS, MODE_3_DELAY, MODE_3_OUTL, MODE_3_OUTR, MODE_3_COMB_FACTOR, MODE_3_COMB_FEEDBACK, MODE_3_DRY_AMP, MODE_3_WET_AMP, 0);

            public static readonly BReverbSettings[] REVERB_SETTINGS = { REVERB_MODE_0_SETTINGS, REVERB_MODE_1_SETTINGS, REVERB_MODE_2_SETTINGS, REVERB_MODE_3_SETTINGS };
        }

        AllpassFilter[] allpasses;
        CombFilter[] combs;

        BReverbSettings currentSettings;
        readonly bool tapDelayMode;
        uint dryAmp;
        uint wetLevel;

        public static BReverbSettings GetCM32L_LAPCSettings(ReverbMode mode)
        {
            return Cm32lLapcSettings.REVERB_SETTINGS[(int)mode];
        }

        public static BReverbSettings GetMT32Settings(ReverbMode mode)
        {
            return Mt32Settings.REVERB_SETTINGS[(int)mode];
        }

        //    public BReverbModel(readonly ReverbMode mode, readonly bool mt32CompatibleModel = false);
        //    ~BReverbModel();
        //    // May be called multiple times without an open() in between.
        //    void close();
        //    void mute();
        //    void setParameters(Bit8u time, Bit8u level);
        //    void process(readonly Sample* inLeft, readonly Sample* inRight, Sample *outLeft, Sample* outRight, unsigned long numSamples);
        //    bool isActive() readonly;
        public bool IsMT32Compatible(ReverbMode mode)
        {
            return currentSettings == GetMT32Settings(mode);
        }

        public BReverbModel(ReverbMode mode, bool mt32CompatibleModel)
        {
            currentSettings = mt32CompatibleModel ? GetMT32Settings(mode) : GetCM32L_LAPCSettings(mode);
            tapDelayMode = mode == ReverbMode.TAP_DELAY;
        }

        // After construction or a close(), open() must be called at least once before any other call (with the exception of close()).
        public void Open()
        {
            if (currentSettings.numberOfAllpasses > 0)
            {
                allpasses = new AllpassFilter[currentSettings.numberOfAllpasses];
                for (var i = 0; i < currentSettings.numberOfAllpasses; i++)
                {
                    allpasses[i] = new AllpassFilter(currentSettings.allpassSizes[i]);
                }
            }
            combs = new CombFilter[currentSettings.numberOfCombs];
            if (tapDelayMode)
            {
                combs[0] = new TapDelayCombFilter(currentSettings.combSizes[0], currentSettings.filterFactors[0]);
            }
            else {
                combs[0] = new DelayWithLowPassFilter(currentSettings.combSizes[0], currentSettings.filterFactors[0], currentSettings.lpfAmp);
                for (var i = 1; i < currentSettings.numberOfCombs; i++)
                {
                    combs[i] = new CombFilter(currentSettings.combSizes[i], currentSettings.filterFactors[i]);
                }
            }
            Mute();
        }

        public void SetParameters(byte time, byte level)
        {
            if (combs == null) return;
            level &= 7;
            time &= 7;
            if (tapDelayMode)
            {
                TapDelayCombFilter comb = (TapDelayCombFilter)combs[0];
                comb.SetOutputPositions(currentSettings.outLPositions[time], currentSettings.outRPositions[time & 7]);
                comb.SetFeedbackFactor(currentSettings.feedbackFactors[((level < 3) || (time < 6)) ? 0 : 1]);
            }
            else {
                for (var i = 0; i < currentSettings.numberOfCombs; i++)
                {
                    combs[i].SetFeedbackFactor(currentSettings.feedbackFactors[(i << 3) + time]);
                }
            }
            if (time == 0 && level == 0)
            {
                dryAmp = wetLevel = 0;
            }
            else {
                if (tapDelayMode && ((time == 0) || (time == 1 && level == 1)))
                {
                    // Looks like MT-32 implementation has some minor quirks in this mode:
                    // for odd level values, the output level changes sometimes depending on the time value which doesn't seem right.
                    dryAmp = currentSettings.dryAmps[level + 8];
                }
                else {
                    dryAmp = currentSettings.dryAmps[level];
                }
                wetLevel = currentSettings.wetLevels[level];
            }
        }

        public void Mute()
        {
            if (allpasses != null)
            {
                for (var i = 0; i < currentSettings.numberOfAllpasses; i++)
                {
                    allpasses[i].Mute();
                }
            }
            if (combs != null)
            {
                for (var i = 0; i < currentSettings.numberOfCombs; i++)
                {
                    combs[i].Mute();
                }
            }
        }

        public void Process(Ptr<Sample> inLeft, Ptr<Sample> inRight, Ptr<Sample> outLeft, Ptr<Sample> outRight, int numSamples)
        {
            if (combs == null)
            {
                Synth.MuteSampleBuffer(outLeft.Data, outLeft.Offset, numSamples);
                Synth.MuteSampleBuffer(outRight.Data, outRight.Offset, numSamples);
                return;
            }

            Sample dry;

            var il = 0; var ol = 0;
            var ir = 0; var or = 0;
            while ((numSamples--) > 0)
            {
                if (tapDelayMode)
                {
#if MT32EMU_USE_FLOAT_SAMPLES
                    dry = (*(inLeft++) * 0.5f) + (*(inRight++) * 0.5f);
#else
                    dry = (Sample)((inLeft[il++] >> 1) + (inRight[ir++] >> 1));
#endif
                }
                else {
#if MT32EMU_USE_FLOAT_SAMPLES
                    dry = (*(inLeft++) * 0.25f) + (*(inRight++) * 0.25f);
#elif MT32EMU_BOSS_REVERB_PRECISE_MODE
                    dry = (*(inLeft++) >> 1) / 2 + (*(inRight++) >> 1) / 2;
#else
                    dry = (Sample)((inLeft[il++] >> 2) + (inRight[ir++] >> 2));
#endif
                }

                // Looks like dryAmp doesn't change in MT-32 but it does in CM-32L / LAPC-I
                dry = CombFilter.WeirdMul(dry, (byte)dryAmp, 0xFF);

                if (tapDelayMode)
                {
                    TapDelayCombFilter comb = (TapDelayCombFilter)(combs[0]);
                    comb.Process(dry);
                    if (outLeft != null)
                    {
                        outLeft[ol++] = CombFilter.WeirdMul(comb.GetLeftOutput(), (byte)wetLevel, 0xFF);
                    }
                    if (outRight != null)
                    {
                        outRight[or++] = CombFilter.WeirdMul(comb.GetRightOutput(), (byte)wetLevel, 0xFF);
                    }
                }
                else {
                    // If the output position is equal to the comb size, get it now in order not to loose it
                    Sample link = combs[0].GetOutputAt(currentSettings.combSizes[0] - 1);

                    // Entrance LPF. Note, comb.process() differs a bit here.
                    combs[0].Process(dry);

#if !MT32EMU_USE_FLOAT_SAMPLES
                    // This introduces reverb noise which actually makes output from the real Boss chip nondeterministic
                    link = (Sample)(link - 1);
#endif
                    link = allpasses[0].Process(link);
                    link = allpasses[1].Process(link);
                    link = allpasses[2].Process(link);

                    // If the output position is equal to the comb size, get it now in order not to loose it
                    Sample outL1 = combs[1].GetOutputAt(currentSettings.outLPositions[0] - 1);

                    combs[1].Process(link);
                    combs[2].Process(link);
                    combs[3].Process(link);

                    if (outLeft != null)
                    {
                        Sample outL2 = combs[2].GetOutputAt(currentSettings.outLPositions[1]);
                        Sample outL3 = combs[3].GetOutputAt(currentSettings.outLPositions[2]);
#if MT32EMU_USE_FLOAT_SAMPLES
                        Sample outSample = 1.5f * (outL1 + outL2) + outL3;
#elif MT32EMU_BOSS_REVERB_PRECISE_MODE
                        /* NOTE:
                         *   Thanks to Mok for discovering, the adder in BOSS reverb chip is found to perform addition with saturation to avoid integer overflow.
                         *   Analysing of the algorithm suggests that the overflow is most probable when the combs output is added below.
                         *   So, despite this isn't actually accurate, we only add the check here for performance reasons.
                         */
                        Sample outSample = Synth.ClipSampleEx(Synth.ClipSampleEx(Synth.ClipSampleEx(Synth.ClipSampleEx((SampleEx)outL1 + SampleEx(outL1 >> 1)) + (SampleEx)outL2) + SampleEx(outL2 >> 1)) + (SampleEx)outL3);
#else
                        Sample outSample = Synth.ClipSampleEx((SampleEx)outL1 + (SampleEx)(outL1 >> 1) + (SampleEx)outL2 + (SampleEx)(outL2 >> 1) + (SampleEx)outL3);
#endif
                        outLeft[ol++] = CombFilter.WeirdMul(outSample, (byte)wetLevel, 0xFF);
                    }
                    if (outRight != null)
                    {
                        Sample outR1 = combs[1].GetOutputAt(currentSettings.outRPositions[0]);
                        Sample outR2 = combs[2].GetOutputAt(currentSettings.outRPositions[1]);
                        Sample outR3 = combs[3].GetOutputAt(currentSettings.outRPositions[2]);
#if MT32EMU_USE_FLOAT_SAMPLES
                        Sample outSample = 1.5f * (outR1 + outR2) + outR3;
#elif MT32EMU_BOSS_REVERB_PRECISE_MODE
                        // See the note above for the left channel output.
                        Sample outSample = Synth::clipSampleEx(Synth.ClipSampleEx(Synth.ClipSampleEx(Synth.ClipSampleEx((SampleEx)outR1 + SampleEx(outR1 >> 1)) + (SampleEx)outR2) + SampleEx(outR2 >> 1)) + (SampleEx)outR3);
#else
                        Sample outSample = Synth.ClipSampleEx((SampleEx)outR1 + (SampleEx)(outR1 >> 1) + (SampleEx)outR2 + (SampleEx)(outR2 >> 1) + (SampleEx)outR3);
#endif
                        outRight[or++] = CombFilter.WeirdMul(outSample, (byte)wetLevel, 0xFF);
                    }
                }
            }
        }
    }


}
