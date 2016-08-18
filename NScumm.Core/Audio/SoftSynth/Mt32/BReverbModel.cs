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

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class BReverbSettings
    {
        readonly uint numberOfAllpasses;
        readonly uint[] allpassSizes;
        readonly uint numberOfCombs;
        readonly uint[] combSizes;
        readonly uint[] outLPositions;
        readonly uint[] outRPositions;
        readonly uint[] filterFactors;
        readonly uint[] feedbackFactors;
        readonly uint[] dryAmps;
        readonly uint[] wetLevels;
        readonly uint lpfAmp;

        public BReverbSettings(uint numberOfAllpasses, uint[] allpassSizes, uint numberOfCombs, uint[] combSizes,
                       uint[] outLPositions, uint[] outRPositions, uint[] filterFactors, uint[] feedbackFactors,
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
        public const uint PROCESS_DELAY = 1;

        public const uint MODE_3_ADDITIONAL_DELAY = 1;
        public const uint MODE_3_FEEDBACK_DELAY = 1;

        // MT32EMU_MEMADDR() converts from sysex-padded, MT32EMU_SYSEXMEMADDR converts to it
        // Roland provides documentation using the sysex-padded addresses, so we tend to use that in code and output
        public static int MT32EMU_MEMADDR(int x) { return ((((x) & 0x7f0000) >> 2) | (((x) & 0x7f00) >> 1) | ((x) & 0x7f));}
    }

    class BReverbModel
    {
        static class Mt32Settings
        {
            static readonly uint MODE_0_NUMBER_OF_ALLPASSES = 3;
            static readonly uint[] MODE_0_ALLPASSES = { 994, 729, 78 };
            static readonly uint MODE_0_NUMBER_OF_COMBS = 4; // Same as above in the new model implementation
            static readonly uint[] MODE_0_COMBS = { 575 + Mt32Emu.PROCESS_DELAY, 2040, 2752, 3629 };
            static readonly uint[] MODE_0_OUTL = { 2040, 687, 1814 };
            static readonly uint[] MODE_0_OUTR = { 1019, 2072, 1 };
            static readonly uint[] MODE_0_COMB_FACTOR = { 0xB0, 0x60, 0x60, 0x60 };
            static readonly uint[] MODE_0_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x28, 0x48, 0x60, 0x70, 0x78, 0x80, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98};
            static readonly uint[] MODE_0_DRY_AMP = { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 };
            static readonly uint[] MODE_0_WET_AMP = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x70, 0xA0, 0xE0 };
            static readonly uint MODE_0_LPF_AMP = 0x80;

            static readonly uint MODE_1_NUMBER_OF_ALLPASSES = 3;
            static readonly uint[] MODE_1_ALLPASSES = { 1324, 809, 176 };
            static readonly uint MODE_1_NUMBER_OF_COMBS = 4; // Same as above in the new model implementation
            static readonly uint[] MODE_1_COMBS = { 961 + Mt32Emu.PROCESS_DELAY, 2619, 3545, 4519 };
            static readonly uint[] MODE_1_OUTL = { 2618, 1760, 4518 };
            static readonly uint[] MODE_1_OUTR = { 1300, 3532, 2274 };
            static readonly uint[] MODE_1_COMB_FACTOR = { 0x90, 0x60, 0x60, 0x60 };
            static readonly uint[] MODE_1_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x28, 0x48, 0x60, 0x70, 0x78, 0x80, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98};
            static readonly uint[] MODE_1_DRY_AMP = { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 };
            static readonly uint[] MODE_1_WET_AMP = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x70, 0xA0, 0xE0 };
            static readonly uint MODE_1_LPF_AMP = 0x80;

            static readonly uint MODE_2_NUMBER_OF_ALLPASSES = 3;
            static readonly uint[] MODE_2_ALLPASSES = { 969, 644, 157 };
            static readonly uint MODE_2_NUMBER_OF_COMBS = 4; // Same as above in the new model implementation
            static readonly uint[] MODE_2_COMBS = { 116 + Mt32Emu.PROCESS_DELAY, 2259, 2839, 3539 };
            static readonly uint[] MODE_2_OUTL = { 2259, 718, 1769 };
            static readonly uint[] MODE_2_OUTR = { 1136, 2128, 1 };
            static readonly uint[] MODE_2_COMB_FACTOR = { 0, 0x60, 0x60, 0x60 };
            static readonly uint[] MODE_2_COMB_FEEDBACK = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                  0x28, 0x48, 0x60, 0x70, 0x78, 0x80, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98,
                                                  0x28, 0x48, 0x60, 0x78, 0x80, 0x88, 0x90, 0x98};
            static readonly uint[] MODE_2_DRY_AMP = { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 };
            static readonly uint[] MODE_2_WET_AMP = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x70, 0xA0, 0xE0 };
            static readonly uint MODE_2_LPF_AMP = 0x80;

            static readonly uint MODE_3_NUMBER_OF_ALLPASSES = 0;
            static readonly uint MODE_3_NUMBER_OF_COMBS = 1;
            static readonly uint[] MODE_3_DELAY = { 16000 + Mt32Emu.MODE_3_FEEDBACK_DELAY + Mt32Emu.PROCESS_DELAY + Mt32Emu.MODE_3_ADDITIONAL_DELAY };
            static readonly uint[] MODE_3_OUTL = { 400, 624, 960, 1488, 2256, 3472, 5280, 8000 };
            static readonly uint[] MODE_3_OUTR = { 800, 1248, 1920, 2976, 4512, 6944, 10560, 16000 };
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

        //    AllpassFilter** allpasses;
        //    CombFilter** combs;

        BReverbSettings currentSettings;
        //    readonly bool tapDelayMode;
        //    uint dryAmp;
        //    uint wetLevel;

        //    static readonly BReverbSettings &getCM32L_LAPCSettings(readonly ReverbMode mode);
        public static BReverbSettings GetMT32Settings(ReverbMode mode)
        {
            return Mt32Settings.REVERB_SETTINGS[(int)mode];
        }

        //    public BReverbModel(readonly ReverbMode mode, readonly bool mt32CompatibleModel = false);
        //    ~BReverbModel();
        //    // After readonlyruction or a close(), open() must be called at least once before any other call (with the exception of close()).
        //    void open();
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
    }


}
