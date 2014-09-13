/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;

namespace NScumm.Core.Audio
{
    public class MidiChannel
    {
        public int inum;
        public byte[] ins;
        public int vol;
        public int nshift;
        public int on;

        public MidiChannel()
        {
            ins = new byte[11];
        }
    }

    public struct MidiTrack
    {
        public long tend;
        public long spos;
        public long pos;
        public long iwait;
        public int on;
        public byte pv;
    }

    [Flags]
    public enum AdlibStyles
    {
        Lucas = 1,
        Cmf = 2,
        Midi = 4,
        Sierra = 8
    }

    public enum AdlibMode
    {
        Melodic = 0,
        Rythm = 1
    }

    public enum FileType
    {
        Lucas = 1,
        Midi = 2,
        Cmf = 3,
        Sierra = 4,
        AdvancedSierra = 5,
        OldLucas = 6
    }

    public class Player
    {
        public Player(IOpl opl)
        {
            this.opl = opl;
        }

        public virtual int getsubsong()
        { 
            return 0; 
        }

        protected IOpl opl;
    }

    public class MidiPlayer: Player
    {
        readonly MidiTrack[] track = new MidiTrack[16];
        readonly MidiChannel[] ch = new MidiChannel[16];
        readonly byte[] adlib_data = new byte[256];
        AdlibStyles adlib_style;
        AdlibMode adlib_mode;
        long pos;
        long sierra_pos;
        //sierras gotta be special.. :>
        FileType type;
        int tins;
        readonly int[,] chp = new int[18, 3];
        long deltas;
        long msqtr;
        float fwait;
        long iwait;
        int subsongs;
        uint curtrack;
        byte[] data;
        int doing;
        // Map CMF drum channels 11 - 15 to corresponding AdLib drum channels
        static readonly int[] percussion_map = { 6, 7, 8, 8, 7 };
        // AdLib standard operator table
        static readonly byte[] adlib_opadd =
            {
                0x00,
                0x01,
                0x02,
                0x08,
                0x09,
                0x0A,
                0x10,
                0x11,
                0x12
            };
        // map CMF drum channels 12 - 15 to corresponding AdLib drum operators
        // bass drum (channel 11) not mapped, cause it's handled like a normal instrument
        static readonly int[] map_chan = { 0x14, 0x12, 0x15, 0x11 };
        readonly byte[,] myinsbank = new byte[128, 16];
        byte[,] smyinsbank = new byte[128, 16];
        byte[,] midiFmInstruments =
            {
                { 0x21, 0x21, 0x8f, 0x0c, 0xf2, 0xf2, 0x45, 0x76, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Acoustic Grand */
                { 0x31, 0x21, 0x4b, 0x09, 0xf2, 0xf2, 0x54, 0x56, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Bright Acoustic */
                { 0x31, 0x21, 0x49, 0x09, 0xf2, 0xf2, 0x55, 0x76, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Electric Grand */
                { 0xb1, 0x61, 0x0e, 0x09, 0xf2, 0xf3, 0x3b, 0x0b, 0x00, 0x00, 0x06, 0, 0, 0 }, /* Honky-Tonk */
                { 0x01, 0x21, 0x57, 0x09, 0xf1, 0xf1, 0x38, 0x28, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Electric Piano 1 */
                { 0x01, 0x21, 0x93, 0x09, 0xf1, 0xf1, 0x38, 0x28, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Electric Piano 2 */
                { 0x21, 0x36, 0x80, 0x17, 0xa2, 0xf1, 0x01, 0xd5, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Harpsichord */
                { 0x01, 0x01, 0x92, 0x09, 0xc2, 0xc2, 0xa8, 0x58, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Clav */
                { 0x0c, 0x81, 0x5c, 0x09, 0xf6, 0xf3, 0x54, 0xb5, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Celesta */
                { 0x07, 0x11, 0x97, 0x89, 0xf6, 0xf5, 0x32, 0x11, 0x00, 0x00, 0x02, 0, 0, 0 }, /* Glockenspiel */
                { 0x17, 0x01, 0x21, 0x09, 0x56, 0xf6, 0x04, 0x04, 0x00, 0x00, 0x02, 0, 0, 0 }, /* Music Box */
                { 0x18, 0x81, 0x62, 0x09, 0xf3, 0xf2, 0xe6, 0xf6, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Vibraphone */
                { 0x18, 0x21, 0x23, 0x09, 0xf7, 0xe5, 0x55, 0xd8, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Marimba */
                { 0x15, 0x01, 0x91, 0x09, 0xf6, 0xf6, 0xa6, 0xe6, 0x00, 0x00, 0x04, 0, 0, 0 }, /* Xylophone */
                { 0x45, 0x81, 0x59, 0x89, 0xd3, 0xa3, 0x82, 0xe3, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* Tubular Bells */
                { 0x03, 0x81, 0x49, 0x89, 0x74, 0xb3, 0x55, 0x05, 0x01, 0x00, 0x04, 0, 0, 0 }, /* Dulcimer */
                { 0x71, 0x31, 0x92, 0x09, 0xf6, 0xf1, 0x14, 0x07, 0x00, 0x00, 0x02, 0, 0, 0 }, /* Drawbar Organ */
                { 0x72, 0x30, 0x14, 0x09, 0xc7, 0xc7, 0x58, 0x08, 0x00, 0x00, 0x02, 0, 0, 0 }, /* Percussive Organ */
                { 0x70, 0xb1, 0x44, 0x09, 0xaa, 0x8a, 0x18, 0x08, 0x00, 0x00, 0x04, 0, 0, 0 }, /* Rock Organ */
                { 0x23, 0xb1, 0x93, 0x09, 0x97, 0x55, 0x23, 0x14, 0x01, 0x00, 0x04, 0, 0, 0 }, /* Church Organ */
                { 0x61, 0xb1, 0x13, 0x89, 0x97, 0x55, 0x04, 0x04, 0x01, 0x00, 0x00, 0, 0, 0 }, /* Reed Organ */
                { 0x24, 0xb1, 0x48, 0x09, 0x98, 0x46, 0x2a, 0x1a, 0x01, 0x00, 0x0c, 0, 0, 0 }, /* Accoridan */
                { 0x61, 0x21, 0x13, 0x09, 0x91, 0x61, 0x06, 0x07, 0x01, 0x00, 0x0a, 0, 0, 0 }, /* Harmonica */
                { 0x21, 0xa1, 0x13, 0x92, 0x71, 0x61, 0x06, 0x07, 0x00, 0x00, 0x06, 0, 0, 0 }, /* Tango Accordian */
                { 0x02, 0x41, 0x9c, 0x89, 0xf3, 0xf3, 0x94, 0xc8, 0x01, 0x00, 0x0c, 0, 0, 0 }, /* Acoustic Guitar(nylon) */
                { 0x03, 0x11, 0x54, 0x09, 0xf3, 0xf1, 0x9a, 0xe7, 0x01, 0x00, 0x0c, 0, 0, 0 }, /* Acoustic Guitar(steel) */
                { 0x23, 0x21, 0x5f, 0x09, 0xf1, 0xf2, 0x3a, 0xf8, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Electric Guitar(jazz) */
                { 0x03, 0x21, 0x87, 0x89, 0xf6, 0xf3, 0x22, 0xf8, 0x01, 0x00, 0x06, 0, 0, 0 }, /* Electric Guitar(clean) */
                { 0x03, 0x21, 0x47, 0x09, 0xf9, 0xf6, 0x54, 0x3a, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Electric Guitar(muted) */
                { 0x23, 0x21, 0x4a, 0x0e, 0x91, 0x84, 0x41, 0x19, 0x01, 0x00, 0x08, 0, 0, 0 }, /* Overdriven Guitar */
                { 0x23, 0x21, 0x4a, 0x09, 0x95, 0x94, 0x19, 0x19, 0x01, 0x00, 0x08, 0, 0, 0 }, /* Distortion Guitar */
                { 0x09, 0x84, 0xa1, 0x89, 0x20, 0xd1, 0x4f, 0xf8, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Guitar Harmonics */
                { 0x21, 0xa2, 0x1e, 0x09, 0x94, 0xc3, 0x06, 0xa6, 0x00, 0x00, 0x02, 0, 0, 0 }, /* Acoustic Bass */
                { 0x31, 0x31, 0x12, 0x09, 0xf1, 0xf1, 0x28, 0x18, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Electric Bass(finger) */
                { 0x31, 0x31, 0x8d, 0x09, 0xf1, 0xf1, 0xe8, 0x78, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Electric Bass(pick) */
                { 0x31, 0x32, 0x5b, 0x09, 0x51, 0x71, 0x28, 0x48, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* Fretless Bass */
                { 0x01, 0x21, 0x8b, 0x49, 0xa1, 0xf2, 0x9a, 0xdf, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Slap Bass 1 */
                { 0x21, 0x21, 0x8b, 0x11, 0xa2, 0xa1, 0x16, 0xdf, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Slap Bass 2 */
                { 0x31, 0x31, 0x8b, 0x09, 0xf4, 0xf1, 0xe8, 0x78, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Synth Bass 1 */
                { 0x31, 0x31, 0x12, 0x09, 0xf1, 0xf1, 0x28, 0x18, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Synth Bass 2 */
                { 0x31, 0x21, 0x15, 0x09, 0xdd, 0x56, 0x13, 0x26, 0x01, 0x00, 0x08, 0, 0, 0 }, /* Violin */
                { 0x31, 0x21, 0x16, 0x09, 0xdd, 0x66, 0x13, 0x06, 0x01, 0x00, 0x08, 0, 0, 0 }, /* Viola */
                { 0x71, 0x31, 0x49, 0x09, 0xd1, 0x61, 0x1c, 0x0c, 0x01, 0x00, 0x08, 0, 0, 0 }, /* Cello */
                { 0x21, 0x23, 0x4d, 0x89, 0x71, 0x72, 0x12, 0x06, 0x01, 0x00, 0x02, 0, 0, 0 }, /* Contrabass */
                { 0xf1, 0xe1, 0x40, 0x09, 0xf1, 0x6f, 0x21, 0x16, 0x01, 0x00, 0x02, 0, 0, 0 }, /* Tremolo Strings */
                { 0x02, 0x01, 0x1a, 0x89, 0xf5, 0x85, 0x75, 0x35, 0x01, 0x00, 0x00, 0, 0, 0 }, /* Pizzicato Strings */
                { 0x02, 0x01, 0x1d, 0x89, 0xf5, 0xf3, 0x75, 0xf4, 0x01, 0x00, 0x00, 0, 0, 0 }, /* Orchestral Strings */
                { 0x10, 0x11, 0x41, 0x09, 0xf5, 0xf2, 0x05, 0xc3, 0x01, 0x00, 0x02, 0, 0, 0 }, /* Timpani */
                { 0x21, 0xa2, 0x9b, 0x0a, 0xb1, 0x72, 0x25, 0x08, 0x01, 0x00, 0x0e, 0, 0, 0 }, /* String Ensemble 1 */
                { 0xa1, 0x21, 0x98, 0x09, 0x7f, 0x3f, 0x03, 0x07, 0x01, 0x01, 0x00, 0, 0, 0 }, /* String Ensemble 2 */
                { 0xa1, 0x61, 0x93, 0x09, 0xc1, 0x4f, 0x12, 0x05, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* SynthStrings 1 */
                { 0x21, 0x61, 0x18, 0x09, 0xc1, 0x4f, 0x22, 0x05, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* SynthStrings 2 */
                { 0x31, 0x72, 0x5b, 0x8c, 0xf4, 0x8a, 0x15, 0x05, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Choir Aahs */
                { 0xa1, 0x61, 0x90, 0x09, 0x74, 0x71, 0x39, 0x67, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Voice Oohs */
                { 0x71, 0x72, 0x57, 0x09, 0x54, 0x7a, 0x05, 0x05, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* Synth Voice */
                { 0x90, 0x41, 0x00, 0x09, 0x54, 0xa5, 0x63, 0x45, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Orchestra Hit */
                { 0x21, 0x21, 0x92, 0x0a, 0x85, 0x8f, 0x17, 0x09, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* Trumpet */
                { 0x21, 0x21, 0x94, 0x0e, 0x75, 0x8f, 0x17, 0x09, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* Trombone */
                { 0x21, 0x61, 0x94, 0x09, 0x76, 0x82, 0x15, 0x37, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* Tuba */
                { 0x31, 0x21, 0x43, 0x09, 0x9e, 0x62, 0x17, 0x2c, 0x01, 0x01, 0x02, 0, 0, 0 }, /* Muted Trumpet */
                { 0x21, 0x21, 0x9b, 0x09, 0x61, 0x7f, 0x6a, 0x0a, 0x00, 0x00, 0x02, 0, 0, 0 }, /* French Horn */
                { 0x61, 0x22, 0x8a, 0x0f, 0x75, 0x74, 0x1f, 0x0f, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Brass Section */
                { 0xa1, 0x21, 0x86, 0x8c, 0x72, 0x71, 0x55, 0x18, 0x01, 0x00, 0x00, 0, 0, 0 }, /* SynthBrass 1 */
                { 0x21, 0x21, 0x4d, 0x09, 0x54, 0xa6, 0x3c, 0x1c, 0x00, 0x00, 0x08, 0, 0, 0 }, /* SynthBrass 2 */
                { 0x31, 0x61, 0x8f, 0x09, 0x93, 0x72, 0x02, 0x0b, 0x01, 0x00, 0x08, 0, 0, 0 }, /* Soprano Sax */
                { 0x31, 0x61, 0x8e, 0x09, 0x93, 0x72, 0x03, 0x09, 0x01, 0x00, 0x08, 0, 0, 0 }, /* Alto Sax */
                { 0x31, 0x61, 0x91, 0x09, 0x93, 0x82, 0x03, 0x09, 0x01, 0x00, 0x0a, 0, 0, 0 }, /* Tenor Sax */
                { 0x31, 0x61, 0x8e, 0x09, 0x93, 0x72, 0x0f, 0x0f, 0x01, 0x00, 0x0a, 0, 0, 0 }, /* Baritone Sax */
                { 0x21, 0x21, 0x4b, 0x09, 0xaa, 0x8f, 0x16, 0x0a, 0x01, 0x00, 0x08, 0, 0, 0 }, /* Oboe */
                { 0x31, 0x21, 0x90, 0x09, 0x7e, 0x8b, 0x17, 0x0c, 0x01, 0x01, 0x06, 0, 0, 0 }, /* English Horn */
                { 0x31, 0x32, 0x81, 0x09, 0x75, 0x61, 0x19, 0x19, 0x01, 0x00, 0x00, 0, 0, 0 }, /* Bassoon */
                { 0x32, 0x21, 0x90, 0x09, 0x9b, 0x72, 0x21, 0x17, 0x00, 0x00, 0x04, 0, 0, 0 }, /* Clarinet */
                { 0xe1, 0xe1, 0x1f, 0x09, 0x85, 0x65, 0x5f, 0x1a, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Piccolo */
                { 0xe1, 0xe1, 0x46, 0x09, 0x88, 0x65, 0x5f, 0x1a, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Flute */
                { 0xa1, 0x21, 0x9c, 0x09, 0x75, 0x75, 0x1f, 0x0a, 0x00, 0x00, 0x02, 0, 0, 0 }, /* Recorder */
                { 0x31, 0x21, 0x8b, 0x09, 0x84, 0x65, 0x58, 0x1a, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Pan Flute */
                { 0xe1, 0xa1, 0x4c, 0x09, 0x66, 0x65, 0x56, 0x26, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Blown Bottle */
                { 0x62, 0xa1, 0xcb, 0x09, 0x76, 0x55, 0x46, 0x36, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Skakuhachi */
                { 0x62, 0xa1, 0xa2, 0x09, 0x57, 0x56, 0x07, 0x07, 0x00, 0x00, 0x0b, 0, 0, 0 }, /* Whistle */
                { 0x62, 0xa1, 0x9c, 0x09, 0x77, 0x76, 0x07, 0x07, 0x00, 0x00, 0x0b, 0, 0, 0 }, /* Ocarina */
                { 0x22, 0x21, 0x59, 0x09, 0xff, 0xff, 0x03, 0x0f, 0x02, 0x00, 0x00, 0, 0, 0 }, /* Lead 1 (square) */
                { 0x21, 0x21, 0x0e, 0x09, 0xff, 0xff, 0x0f, 0x0f, 0x01, 0x01, 0x00, 0, 0, 0 }, /* Lead 2 (sawtooth) */
                { 0x22, 0x21, 0x46, 0x89, 0x86, 0x64, 0x55, 0x18, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Lead 3 (calliope) */
                { 0x21, 0xa1, 0x45, 0x09, 0x66, 0x96, 0x12, 0x0a, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Lead 4 (chiff) */
                { 0x21, 0x22, 0x8b, 0x09, 0x92, 0x91, 0x2a, 0x2a, 0x01, 0x00, 0x00, 0, 0, 0 }, /* Lead 5 (charang) */
                { 0xa2, 0x61, 0x9e, 0x49, 0xdf, 0x6f, 0x05, 0x07, 0x00, 0x00, 0x02, 0, 0, 0 }, /* Lead 6 (voice) */
                { 0x20, 0x60, 0x1a, 0x09, 0xef, 0x8f, 0x01, 0x06, 0x00, 0x02, 0x00, 0, 0, 0 }, /* Lead 7 (fifths) */
                { 0x21, 0x21, 0x8f, 0x86, 0xf1, 0xf4, 0x29, 0x09, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Lead 8 (bass+lead) */
                { 0x77, 0xa1, 0xa5, 0x09, 0x53, 0xa0, 0x94, 0x05, 0x00, 0x00, 0x02, 0, 0, 0 }, /* Pad 1 (new age) */
                { 0x61, 0xb1, 0x1f, 0x89, 0xa8, 0x25, 0x11, 0x03, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Pad 2 (warm) */
                { 0x61, 0x61, 0x17, 0x09, 0x91, 0x55, 0x34, 0x16, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* Pad 3 (polysynth) */
                { 0x71, 0x72, 0x5d, 0x09, 0x54, 0x6a, 0x01, 0x03, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Pad 4 (choir) */
                { 0x21, 0xa2, 0x97, 0x09, 0x21, 0x42, 0x43, 0x35, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Pad 5 (bowed) */
                { 0xa1, 0x21, 0x1c, 0x09, 0xa1, 0x31, 0x77, 0x47, 0x01, 0x01, 0x00, 0, 0, 0 }, /* Pad 6 (metallic) */
                { 0x21, 0x61, 0x89, 0x0c, 0x11, 0x42, 0x33, 0x25, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Pad 7 (halo) */
                { 0xa1, 0x21, 0x15, 0x09, 0x11, 0xcf, 0x47, 0x07, 0x01, 0x00, 0x00, 0, 0, 0 }, /* Pad 8 (sweep) */
                { 0x3a, 0x51, 0xce, 0x09, 0xf8, 0x86, 0xf6, 0x02, 0x00, 0x00, 0x02, 0, 0, 0 }, /* FX 1 (rain) */
                { 0x21, 0x21, 0x15, 0x09, 0x21, 0x41, 0x23, 0x13, 0x01, 0x00, 0x00, 0, 0, 0 }, /* FX 2 (soundtrack) */
                { 0x06, 0x01, 0x5b, 0x09, 0x74, 0xa5, 0x95, 0x72, 0x00, 0x00, 0x00, 0, 0, 0 }, /* FX 3 (crystal) */
                { 0x22, 0x61, 0x92, 0x8c, 0xb1, 0xf2, 0x81, 0x26, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* FX 4 (atmosphere) */
                { 0x41, 0x42, 0x4d, 0x09, 0xf1, 0xf2, 0x51, 0xf5, 0x01, 0x00, 0x00, 0, 0, 0 }, /* FX 5 (brightness) */
                { 0x61, 0xa3, 0x94, 0x89, 0x11, 0x11, 0x51, 0x13, 0x01, 0x00, 0x06, 0, 0, 0 }, /* FX 6 (goblins) */
                { 0x61, 0xa1, 0x8c, 0x89, 0x11, 0x1d, 0x31, 0x03, 0x00, 0x00, 0x06, 0, 0, 0 }, /* FX 7 (echoes) */
                { 0xa4, 0x61, 0x4c, 0x09, 0xf3, 0x81, 0x73, 0x23, 0x01, 0x00, 0x04, 0, 0, 0 }, /* FX 8 (sci-fi) */
                { 0x02, 0x07, 0x85, 0x0c, 0xd2, 0xf2, 0x53, 0xf6, 0x00, 0x01, 0x00, 0, 0, 0 }, /* Sitar */
                { 0x11, 0x13, 0x0c, 0x89, 0xa3, 0xa2, 0x11, 0xe5, 0x01, 0x00, 0x00, 0, 0, 0 }, /* Banjo */
                { 0x11, 0x11, 0x06, 0x09, 0xf6, 0xf2, 0x41, 0xe6, 0x01, 0x02, 0x04, 0, 0, 0 }, /* Shamisen */
                { 0x93, 0x91, 0x91, 0x09, 0xd4, 0xeb, 0x32, 0x11, 0x00, 0x01, 0x08, 0, 0, 0 }, /* Koto */
                { 0x04, 0x01, 0x4f, 0x09, 0xfa, 0xc2, 0x56, 0x05, 0x00, 0x00, 0x0c, 0, 0, 0 }, /* Kalimba */
                { 0x21, 0x22, 0x49, 0x09, 0x7c, 0x6f, 0x20, 0x0c, 0x00, 0x01, 0x06, 0, 0, 0 }, /* Bagpipe */
                { 0x31, 0x21, 0x85, 0x09, 0xdd, 0x56, 0x33, 0x16, 0x01, 0x00, 0x0a, 0, 0, 0 }, /* Fiddle */
                { 0x20, 0x21, 0x04, 0x8a, 0xda, 0x8f, 0x05, 0x0b, 0x02, 0x00, 0x06, 0, 0, 0 }, /* Shanai */
                { 0x05, 0x03, 0x6a, 0x89, 0xf1, 0xc3, 0xe5, 0xe5, 0x00, 0x00, 0x06, 0, 0, 0 }, /* Tinkle Bell */
                { 0x07, 0x02, 0x15, 0x09, 0xec, 0xf8, 0x26, 0x16, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Agogo */
                { 0x05, 0x01, 0x9d, 0x09, 0x67, 0xdf, 0x35, 0x05, 0x00, 0x00, 0x08, 0, 0, 0 }, /* Steel Drums */
                { 0x18, 0x12, 0x96, 0x09, 0xfa, 0xf8, 0x28, 0xe5, 0x00, 0x00, 0x0a, 0, 0, 0 }, /* Woodblock */
                { 0x10, 0x00, 0x86, 0x0c, 0xa8, 0xfa, 0x07, 0x03, 0x00, 0x00, 0x06, 0, 0, 0 }, /* Taiko Drum */
                { 0x11, 0x10, 0x41, 0x0c, 0xf8, 0xf3, 0x47, 0x03, 0x02, 0x00, 0x04, 0, 0, 0 }, /* Melodic Tom */
                { 0x01, 0x10, 0x8e, 0x09, 0xf1, 0xf3, 0x06, 0x02, 0x02, 0x00, 0x0e, 0, 0, 0 }, /* Synth Drum */
                { 0x0e, 0xc0, 0x00, 0x09, 0x1f, 0x1f, 0x00, 0xff, 0x00, 0x03, 0x0e, 0, 0, 0 }, /* Reverse Cymbal */
                { 0x06, 0x03, 0x80, 0x91, 0xf8, 0x56, 0x24, 0x84, 0x00, 0x02, 0x0e, 0, 0, 0 }, /* Guitar Fret Noise */
                { 0x0e, 0xd0, 0x00, 0x0e, 0xf8, 0x34, 0x00, 0x04, 0x00, 0x03, 0x0e, 0, 0, 0 }, /* Breath Noise */
                { 0x0e, 0xc0, 0x00, 0x09, 0xf6, 0x1f, 0x00, 0x02, 0x00, 0x03, 0x0e, 0, 0, 0 }, /* Seashore */
                { 0xd5, 0xda, 0x95, 0x49, 0x37, 0x56, 0xa3, 0x37, 0x00, 0x00, 0x00, 0, 0, 0 }, /* Bird Tweet */
                { 0x35, 0x14, 0x5c, 0x11, 0xb2, 0xf4, 0x61, 0x15, 0x02, 0x00, 0x0a, 0, 0, 0 }, /* Telephone ring */
                { 0x0e, 0xd0, 0x00, 0x09, 0xf6, 0x4f, 0x00, 0xf5, 0x00, 0x03, 0x0e, 0, 0, 0 }, /* Helicopter */
                { 0x26, 0xe4, 0x00, 0x09, 0xff, 0x12, 0x01, 0x16, 0x00, 0x01, 0x0e, 0, 0, 0 }, /* Applause */
                { 0x00, 0x00, 0x00, 0x09, 0xf3, 0xf6, 0xf0, 0xc9, 0x00, 0x02, 0x0e, 0, 0, 0 }  /* Gunshot */
            };
        /* logarithmic relationship between midi and FM volumes */
        static readonly int[] my_midi_fm_vol_table =
            {
                0, 11, 16, 19, 22, 25, 27, 29, 32, 33, 35, 37, 39, 40, 42, 43,
                45, 46, 48, 49, 50, 51, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62,
                64, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 75, 76, 77,
                78, 79, 80, 80, 81, 82, 83, 83, 84, 85, 86, 86, 87, 88, 89, 89,
                90, 91, 91, 92, 93, 93, 94, 95, 96, 96, 97, 97, 98, 99, 99, 100,
                101, 101, 102, 103, 103, 104, 104, 105, 106, 106, 107, 107, 108,
                109, 109, 110, 110, 111, 112, 112, 113, 113, 114, 114, 115, 115,
                116, 117, 117, 118, 118, 119, 119, 120, 120, 121, 121, 122, 122,
                123, 123, 124, 124, 125, 125, 126, 126, 127
            };
        // Standard AdLib frequency table
        static readonly int[] fnums =
            {
            0x16b,
            0x181,
            0x198,
            0x1b0,
            0x1ca,
            0x1e5,
            0x202,
            0x220,
            0x241,
            0x263,
            0x287,
            0x2ae
        };

        public MidiPlayer(IOpl opl)
            : base(opl)
        {
            
        }

        public void Load(string filename)
        {
            FileType good = 0;
            using (var stream = File.OpenRead(filename))
            {
                var br = new BinaryReader(stream);
                var sig = br.ReadBytes(6);
                if (sig[0] == 'A' && sig[1] == 'D' && sig[2] == 'L')
                {
                    good = FileType.Lucas;
                }
                else if (sig[4] == 'A' && sig[5] == 'D')
                {
                    good = FileType.OldLucas;
                }

                if (good != 0)
                {
                    br.BaseStream.Seek(0, SeekOrigin.Begin);
                    data = br.ReadBytes((int)br.BaseStream.Length);
                    type = good;
                    subsongs = 1;
                    Rewind(0);
                }
            }
        }

        public void LoadFrom(byte[] data)
        {
            FileType good = 0;
            var br = new BinaryReader(new MemoryStream(data));
            var sig = br.ReadBytes(6);
            if (sig[0] == 'A' && sig[1] == 'D' && sig[2] == 'L')
            {
                good = FileType.Lucas;
            }
            else if (sig[4] == 'A' && sig[5] == 'D')
            {
                good = FileType.OldLucas;
            }

            if (good != 0)
            {
                br.BaseStream.Seek(0, SeekOrigin.Begin);
                this.data = br.ReadBytes((int)br.BaseStream.Length);
                type = good;
                subsongs = 1;
                Rewind(0);
            }
        }

        public void LoadFromOldLucas(byte[] data)
        {
            this.data = data;
            type = FileType.OldLucas;
            subsongs = 1;
            Rewind(0);
        }

        public void Rewind(int subsong)
        {
            long i, j, n, m, l;
            long o_sierra_pos;
            byte[] ins = new byte[16];

            pos = 0;
            tins = 0;
            adlib_style = AdlibStyles.Midi | AdlibStyles.Cmf;
            adlib_mode = AdlibMode.Melodic;
            for (i = 0; i < 128; i++)
                for (j = 0; j < 14; j++)
                    myinsbank[i, j] = midiFmInstruments[i, j];
            for (i = 0; i < 16; i++)
            {
                ch[i] = new MidiChannel();
                ch[i].inum = 0;
                for (j = 0; j < 11; j++)
                    ch[i].ins[j] = myinsbank[ch[i].inum, j];
                ch[i].vol = 127;
                ch[i].nshift = -25;
                ch[i].on = 1;
            }

            /* General init */
            for (i = 0; i < 9; i++)
            {
                chp[i, 0] = -1;
                chp[i, 2] = 0;
            }

            deltas = 250;  // just a number,  not a standard
            msqtr = 500000;
            fwait = 123; // gotta be a small thing.. sorta like nothing
            iwait = 0;

            subsongs = 1;

            for (i = 0; i < 16; i++)
            {
                track[i].tend = 0;
                track[i].spos = 0;
                track[i].pos = 0;
                track[i].iwait = 0;
                track[i].on = 0;
                track[i].pv = 0;
            }
            curtrack = 0;

            /* specific to file-type init */

            pos = 0;
            i = getnext(1);
            switch (type)
            {
                case FileType.Lucas:
                case FileType.Midi:
                    if (type == FileType.Lucas)
                    {
                        getnext(24);  //skip junk and get to the midi.
                        adlib_style = AdlibStyles.Lucas | AdlibStyles.Midi;
                    }
            //note: no break, we go right into midi headers...
                    if (type != FileType.Lucas)
                        tins = 128;
                    getnext(11);  /*skip header*/
                    deltas = getnext(2);
//                    Console.WriteLine("deltas:{0}", deltas);
                    getnext(4);

                    curtrack = 0;
                    track[curtrack].on = 1;
                    track[curtrack].tend = getnext(4);
                    track[curtrack].spos = pos;
//                    Console.WriteLine("tracklen:{0}", track [curtrack].tend);
                    break;
                case FileType.Cmf:
                    getnext(3);  // ctmf
                    getnexti(2); //version
                    n = getnexti(2); // instrument offset
                    m = getnexti(2); // music offset
                    deltas = getnexti(2); //ticks/qtr note
                    msqtr = 1000000 / getnexti(2) * deltas;
                    //the stuff in the cmf is click ticks per second..

                    i = getnexti(2);
                    if (i != 0)
                    {
                        var title = ReadString(data, i);
                        Console.WriteLine("Title: {0}", title);
                    }
                    i = getnexti(2);
                    if (i != 0)
                    {
                        var author = ReadString(data, i);
                        Console.WriteLine("Author: {0}", author);
                    }
                    i = getnexti(2);
                    if (i != 0)
                    {
                        var remarks = ReadString(data, i);
                        Console.WriteLine("Remarks: {0}", remarks);
                    }

                    getnext(16); // channel in use table ..
                    i = getnexti(2); // num instr
                    if (i > 128)
                        i = 128; // to ward of bad numbers...
                    getnexti(2); //basic tempo

//                    Console.WriteLine("\nioff:{0}\nmoff{1}\ndeltas:{2}\nmsqtr:{3}\nnumi:{4}",
//                                      n, m, deltas, msqtr, i);
                    pos = n;  // jump to instruments
                    tins = (int)i;
                    for (j = 0; j < i; j++)
                    {
//                        Console.Write("\n{0}: ", j);
                        for (l = 0; l < 16; l++)
                        {
                            myinsbank[j, l] = (byte)getnext(1);
//                            Console.Write("{0:X2} ", myinsbank [j, l]);
                        }
                    }

                    for (i = 0; i < 16; i++)
                        ch[i].nshift = -13;

                    adlib_style = AdlibStyles.Cmf;

                    curtrack = 0;
                    track[curtrack].on = 1;
                    track[curtrack].tend = data.Length;  // music until the end of the file
                    track[curtrack].spos = m;  //jump to midi music
                    break;
                case FileType.OldLucas:
                    msqtr = 250000;
                    pos = 9;
                    deltas = getnext(1);

                    i = 8;
                    pos = 0x19;  // jump to instruments
                    tins = (int)i;
                    for (j = 0; j < i; j++)
                    {
//                        Console.Write("\n{0}: ", j);
                        for (l = 0; l < 16; l++)
                            ins[l] = (byte)getnext(1);

                        myinsbank[j, 10] = ins[2];
                        myinsbank[j, 0] = ins[3];
                        myinsbank[j, 2] = ins[4];
                        myinsbank[j, 4] = ins[5];
                        myinsbank[j, 6] = ins[6];
                        myinsbank[j, 8] = ins[7];
                        myinsbank[j, 1] = ins[8];
                        myinsbank[j, 3] = ins[9];
                        myinsbank[j, 5] = ins[10];
                        myinsbank[j, 7] = ins[11];
                        myinsbank[j, 9] = ins[12];

//                        for (l=0; l<11; l++)
//                            Console.WriteLine("{0:X2} ", myinsbank [j, l]);
                    }

                    for (i = 0; i < 16; i++)
                    {
                        if (i < tins)
                        {
                            ch[i].inum = (int)i;
                            for (j = 0; j < 11; j++)
                                ch[i].ins[j] = myinsbank[ch[i].inum, j];
                        }
                    }

                    adlib_style = AdlibStyles.Lucas | AdlibStyles.Midi;

                    curtrack = 0;
                    track[curtrack].on = 1;
                    track[curtrack].tend = data.Length;  // music until the end of the file
                    track[curtrack].spos = 0x98;  //jump to midi music
                    break;
                case FileType.AdvancedSierra:
                    memcpy(myinsbank, smyinsbank, 128, 16);
                    deltas = 0x20;
                    getnext(11); //worthless empty space and "stuff" :)

                    o_sierra_pos = sierra_pos = pos;
                    sierra_next_section();
                    while (datalook(sierra_pos - 2) != 0xff)
                    {
                        sierra_next_section();
                        subsongs++;
                    }

                    if (subsong < 0 || subsong >= subsongs)
                        subsong = 0;

                    sierra_pos = o_sierra_pos;
                    sierra_next_section();
                    i = 0;
                    while (i != subsong)
                    {
                        sierra_next_section();
                        i++;
                    }

                    adlib_style = AdlibStyles.Sierra | AdlibStyles.Midi;  //advanced sierra tunes use volume
                    break;
                case FileType.Sierra:
                    memcpy(myinsbank, smyinsbank, 128, 16);
                    getnext(2);
                    deltas = 0x20;

                    curtrack = 0;
                    track[curtrack].on = 1;
                    track[curtrack].tend = data.Length;  // music until the end of the file

                    for (i = 0; i < 16; i++)
                    {
                        ch[i].nshift = -13;
                        ch[i].on = (int)getnext(1);
                        ch[i].inum = (int)getnext(1);
                        for (j = 0; j < 11; j++)
                            ch[i].ins[j] = myinsbank[ch[i].inum, j];
                    }

                    track[curtrack].spos = pos;
                    adlib_style = AdlibStyles.Sierra | AdlibStyles.Midi;
                    break;
            }


            /*        sprintf(info,"%s\r\nTicks/Quarter Note: %ld\r\n",info,deltas);
        sprintf(info,"%sms/Quarter Note: %ld",info,msqtr); */

            for (i = 0; i < 16; i++)
                if (track[i].on != 0)
                {
                    track[i].pos = track[i].spos;
                    track[i].pv = 0;
                    track[i].iwait = 0;
                }

            doing = 1;
            midi_fm_reset();
        }

        string ReadString(byte[] data, long index)
        {
            var sb = new StringBuilder();
            while (data[index] != 0)
            {
                sb.Append((char)data[index]);
            }
            return sb.ToString();
        }

        byte datalook(long pos)
        {
            if (pos < 0 || pos >= data.Length)
                return(0);
            return(data[pos]);
        }

        long getnext(long num)
        {
            long v = 0;
            long i;

            for (i = 0; i < num; i++)
            {
                v <<= 8;
                v += datalook(pos);
                pos++;
            }
            return v;
        }

        long getnexti(long num)
        {
            long v = 0;
            int i;

            for (i = 0; i < num; i++)
            {
                v += (((long)datalook(pos)) << (8 * i));
                pos++;
            }
            return(v);
        }

        void memcpy(byte[,] src, byte[,] dst, int dim1, int dim2)
        {
            for (int i = 0; i < dim1; i++)
            {
                for (int j = 0; j < dim2; j++)
                {
                    dst[i, j] = src[i, j];
                }   
            }
        }

        void midi_fm_reset()
        {
            for (int i = 0; i < 256; i++)
                midi_write_adlib(i, 0);

            midi_write_adlib(0x01, 0x20);
            midi_write_adlib(0xBD, 0xc0);
        }

        void midi_write_adlib(int r, int v)
        {
            opl.Write(0, r, v);
            adlib_data[r] = (byte)v;
        }

        void sierra_next_section()
        {
            int i, j;

            for (i = 0; i < 16; i++)
                track[i].on = 0;

//            Console.WriteLine("\n\nnext adv sierra section");

            pos = sierra_pos;
            i = 0;
            j = 0;
            while (i != 0xff)
            {
                getnext(1);
                curtrack = (uint)j;
                j++;
                track[curtrack].on = 1;
                track[curtrack].spos = getnext(1);
                track[curtrack].spos += (getnext(1) << 8) + 4;  //4 best usually +3? not 0,1,2 or 5
                //       track[curtrack].spos=getnext(1)+(getnext(1)<<8)+4;     // dynamite!: doesn't optimize correctly!!
                track[curtrack].tend = data.Length; //0xFC will kill it
                track[curtrack].iwait = 0;
                track[curtrack].pv = 0;
//                Console.WriteLine("track {0} starts at {1:X}", curtrack, track [curtrack].spos);

                getnext(2);
                i = (int)getnext(1);
            }
            getnext(2);
            deltas = 0x20;
            sierra_pos = pos;
            //getch();

            fwait = 0;
            doing = 1;
        }

        int getval()
        {
            int v = 0;
            byte b;

            b = (byte)getnext(1);
            v = b & 0x7f;
            while ((b & 0x80) != 0)
            {
                b = (byte)getnext(1);
                v = (v << 7) + (b & 0x7F);
            }
            return(v);
        }

        void midi_fm_endnote(int voice)
        {
            //midi_fm_volume(voice,0);
            //midi_write_adlib(0xb0+voice,0);

            midi_write_adlib(0xb0 + voice, (byte)(adlib_data[0xb0 + voice] & (255 - 32)));
        }

        void midi_fm_instrument(int voice, byte[] inst)
        {
            if (adlib_style.HasFlag(AdlibStyles.Sierra))
                midi_write_adlib(0xbd, 0);  //just gotta make sure this happens..
            //'cause who knows when it'll be
            //reset otherwise.


            midi_write_adlib(0x20 + adlib_opadd[voice], inst[0]);
            midi_write_adlib(0x23 + adlib_opadd[voice], inst[1]);

            if (adlib_style.HasFlag(AdlibStyles.Lucas))
            {
                midi_write_adlib(0x43 + adlib_opadd[voice], 0x3f);
                if ((inst[10] & 1) == 0)
                    midi_write_adlib(0x40 + adlib_opadd[voice], inst[2]);
                else
                    midi_write_adlib(0x40 + adlib_opadd[voice], 0x3f);

            }
            else if ((adlib_style.HasFlag(AdlibStyles.Sierra)) || (adlib_style.HasFlag(AdlibStyles.Cmf)))
            {
                midi_write_adlib(0x40 + adlib_opadd[voice], inst[2]);
                midi_write_adlib(0x43 + adlib_opadd[voice], inst[3]);

            }
            else
            {
                midi_write_adlib(0x40 + adlib_opadd[voice], inst[2]);
                if ((inst[10] & 1) == 0)
                    midi_write_adlib(0x43 + adlib_opadd[voice], inst[3]);
                else
                    midi_write_adlib(0x43 + adlib_opadd[voice], 0);
            }

            midi_write_adlib(0x60 + adlib_opadd[voice], inst[4]);
            midi_write_adlib(0x63 + adlib_opadd[voice], inst[5]);
            midi_write_adlib(0x80 + adlib_opadd[voice], inst[6]);
            midi_write_adlib(0x83 + adlib_opadd[voice], inst[7]);
            midi_write_adlib(0xe0 + adlib_opadd[voice], inst[8]);
            midi_write_adlib(0xe3 + adlib_opadd[voice], inst[9]);

            midi_write_adlib(0xc0 + voice, inst[10]);
        }

        void midi_fm_percussion(int ch, byte[] inst)
        {
            int opadd = map_chan[ch - 12];

            midi_write_adlib(0x20 + opadd, inst[0]);
            midi_write_adlib(0x40 + opadd, inst[2]);
            midi_write_adlib(0x60 + opadd, inst[4]);
            midi_write_adlib(0x80 + opadd, inst[6]);
            midi_write_adlib(0xe0 + opadd, inst[8]);
            if (opadd < 0x13) // only output this for the modulator, not the carrier, as it affects the entire channel
                midi_write_adlib(0xc0 + percussion_map[ch - 11], inst[10]);
        }

        void midi_fm_playnote(int voice, int note, int volume)
        {
            int freq = fnums[note % 12];
            int oct = note / 12;
            int c;

            midi_fm_volume(voice, volume);
            midi_write_adlib(0xa0 + voice, (byte)(freq & 0xff));

            c = ((freq & 0x300) >> 8) + ((oct & 7) << 2) + (adlib_mode == AdlibMode.Melodic || voice < 6 ? (1 << 5) : 0);
            midi_write_adlib(0xb0 + voice, (byte)c);
        }

        void midi_fm_volume(int voice, int volume)
        {
            int vol;

            if (adlib_style.HasFlag(AdlibStyles.Sierra) == false)
            {  //sierra likes it loud!
                vol = volume >> 2;

                if (adlib_style.HasFlag(AdlibStyles.Lucas))
                {
                    if ((adlib_data[0xc0 + voice] & 1) == 1)
                        midi_write_adlib(0x40 + adlib_opadd[voice], (byte)((63 - vol) |
                            (adlib_data[0x40 + adlib_opadd[voice]] & 0xc0)));
                    midi_write_adlib(0x43 + adlib_opadd[voice], (byte)((63 - vol) |
                        (adlib_data[0x43 + adlib_opadd[voice]] & 0xc0)));
                }
                else
                {
                    if ((adlib_data[0xc0 + voice] & 1) == 1)
                        midi_write_adlib(0x40 + adlib_opadd[voice], (byte)((63 - vol) |
                            (adlib_data[0x40 + adlib_opadd[voice]] & 0xc0)));
                    midi_write_adlib(0x43 + adlib_opadd[voice], (byte)((63 - vol) |
                        (adlib_data[0x43 + adlib_opadd[voice]] & 0xc0)));
                }
            }
        }

        public float GetRefresh()
        {
            return (fwait > 0.01f ? fwait : 0.01f);
        }

        public bool Update()
        {
            long w, v, note, vel, ctrl, nv, x, l, lnum;
            int i = 0, j, c;
            int on, onl, numchan;
            int ret;

            if (doing == 1)
            {
                // just get the first wait and ignore it :>
                for (curtrack = 0; curtrack < 16; curtrack++)
                    if (track[curtrack].on != 0)
                    {
                        pos = track[curtrack].pos;
                        if (type != FileType.Sierra && type != FileType.AdvancedSierra)
                            track[curtrack].iwait += getval();
                        else
                            track[curtrack].iwait += getnext(1);
                        track[curtrack].pos = pos;
                    }
                doing = 0;
            }

            iwait = 0;
            ret = 1;

            while (iwait == 0 && ret == 1)
            {
                for (curtrack = 0; curtrack < 16; curtrack++)
                    if (track[curtrack].on != 0 && track[curtrack].iwait == 0 &&
                    track[curtrack].pos < track[curtrack].tend)
                    {
                        pos = track[curtrack].pos;

                        v = getnext(1);

                        //  This is to do implied MIDI events.
                        if (v < 0x80)
                        {
                            v = track[curtrack].pv;
                            pos--;
                        }
                        track[curtrack].pv = (byte)v;

                        c = (int)(v & 0x0f);
//                        Console.Write("[{0:X2}]", v);
                        switch (v & 0xf0)
                        {
                            case 0x80: /*note off*/
                                note = getnext(1);
                                vel = getnext(1);
                                for (i = 0; i < 9; i++)
                                    if (chp[i, 0] == c && chp[i, 1] == note)
                                    {
                                        midi_fm_endnote(i);
                                        chp[i, 0] = -1;
                                    }
                                break;
                            case 0x90: /*note on*/
                            //  doing=0;
                                note = getnext(1);
                                vel = getnext(1);

                                if (adlib_mode == AdlibMode.Rythm)
                                    numchan = 6;
                                else
                                    numchan = 9;

                                if (ch[c].on != 0)
                                {
                                    for (i = 0; i < 18; i++)
                                        chp[i, 2]++;

                                    if (c < 11 || adlib_mode == AdlibMode.Melodic)
                                    {
                                        j = 0;
                                        on = -1;
                                        onl = 0;
                                        for (i = 0; i < numchan; i++)
                                            if (chp[i, 0] == -1 && chp[i, 2] > onl)
                                            {
                                                onl = chp[i, 2];
                                                on = i;
                                                j = 1;
                                            }

                                        if (on == -1)
                                        {
                                            onl = 0;
                                            for (i = 0; i < numchan; i++)
                                                if (chp[i, 2] > onl)
                                                {
                                                    onl = chp[i, 2];
                                                    on = i;
                                                }
                                        }

                                        if (j == 0)
                                            midi_fm_endnote(on);
                                    }
                                    else
                                        on = percussion_map[c - 11];

                                    if (vel != 0 && ch[c].inum >= 0 && ch[c].inum < 128)
                                    {
                                        if (adlib_mode == AdlibMode.Melodic || c < 12) // 11 == bass drum, handled like a normal instrument, on == channel 6 thanks to percussion_map[] above
                                            midi_fm_instrument(on, ch[c].ins);
                                        else
                                            midi_fm_percussion(c, ch[c].ins);

                                        if (adlib_style.HasFlag(AdlibStyles.Midi))
                                        {
                                            nv = ((ch[c].vol * vel) / 128);
                                            if (adlib_style.HasFlag(AdlibStyles.Lucas))
                                                nv *= 2;
                                            if (nv > 127)
                                                nv = 127;
                                            nv = my_midi_fm_vol_table[nv];
                                            if (adlib_style.HasFlag(AdlibStyles.Lucas))
                                                nv = (int)((float)Math.Sqrt((float)nv) * 11);
                                        }
                                        else if (adlib_style.HasFlag(AdlibStyles.Cmf))
                                        {
                                            // CMF doesn't support note velocity (even though some files have them!)
                                            nv = 127;
                                        }
                                        else
                                        {
                                            nv = vel;
                                        }

                                        midi_fm_playnote(on, (int)note + ch[c].nshift, (int)nv * 2); // sets freq in rhythm mode
                                        chp[on, 0] = c;
                                        chp[on, 1] = (int)note;
                                        chp[on, 2] = 0;

                                        if (adlib_mode == AdlibMode.Rythm && c >= 11)
                                        {
                                            // Still need to turn off the perc instrument before playing it again,
                                            // as not all songs send a noteoff.
                                            midi_write_adlib(0xbd, (byte)adlib_data[0xbd] & ~(0x10 >> (c - 11)));
                                            // Play the perc instrument
                                            midi_write_adlib(0xbd, (byte)adlib_data[0xbd] | (0x10 >> (c - 11)));
                                        }

                                    }
                                    else
                                    {
                                        if (vel == 0)
                                        { //same code as end note
                                            if (adlib_mode == AdlibMode.Rythm && c >= 11)
                                            {
                                                // Turn off the percussion instrument
                                                midi_write_adlib(0xbd, adlib_data[0xbd] & ~(0x10 >> (c - 11)));
                                                //midi_fm_endnote(percussion_map[c]);
                                                chp[percussion_map[c - 11], 0] = -1;
                                            }
                                            else
                                            {
                                                for (i = 0; i < 9; i++)
                                                {
                                                    if (chp[i, 0] == c && chp[i, 1] == note)
                                                    {
                                                        // midi_fm_volume(i,0);  // really end the note
                                                        midi_fm_endnote(i);
                                                        chp[i, 0] = -1;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // i forget what this is for.
                                            chp[on, 0] = -1;
                                            chp[on, 2] = 0;
                                        }
                                    }
//                                    Console.WriteLine(" [{0}:{1}:{2}:{3}]", c, ch [c].inum, note, vel);
                                }
                                else
                                {
                                    Console.Write("off");
                                }
                                break;
                            case 0xa0: /*key after touch */
                                note = getnext(1);
                                vel = getnext(1);
                            /*  //this might all be good
                for (i=0; i<9; i++)
                    if (chp[i][0]==c & chp[i][1]==note)
                        
midi_fm_playnote(i,note+cnote[c],my_midi_fm_vol_table[(cvols[c]*vel)/128]*2);
                */
                                break;
                            case 0xb0: /*control change .. pitch bend? */
                                ctrl = getnext(1);
                                vel = getnext(1);

                                switch (ctrl)
                                {
                                    case 0x07:
//								Console.Write ("(pb:{0}: {1} {2})", c, ctrl, vel);
                                        ch[c].vol = (int)vel;
//								Console.Write ("vol");
                                        break;
                                    case 0x63:
                                        if (adlib_style.HasFlag(AdlibStyles.Cmf))
                                        {
                                            // Custom extension to allow CMF files to switch the
                                            // AM+VIB depth on and off (officially this is on,
                                            // and there's no way to switch it off.)  Controller
                                            // values:
                                            //   0 == AM+VIB off
                                            //   1 == VIB on
                                            //   2 == AM on
                                            //   3 == AM+VIB on
                                            midi_write_adlib(0xbd, (int)((adlib_data[0xbd] & ~0xC0) | (vel << 6)));
//									Console.WriteLine (" AM+VIB depth change - AM {0}, VIB {1}",
//										(adlib_data [0xbd] & 0x80) != 0 ? "on" : "off",
//										(adlib_data [0xbd] & 0x40) != 0 ? "on" : "off"
//									);
                                        }
                                        break;
                                    case 0x67:
//								Console.WriteLine ("Rhythm mode: {0}", vel);
                                        if (adlib_style.HasFlag(AdlibStyles.Cmf))
                                        {
                                            adlib_mode = (AdlibMode)vel;
                                            if (adlib_mode == AdlibMode.Rythm)
                                                midi_write_adlib(0xbd, adlib_data[0xbd] | (1 << 5));
                                            else
                                                midi_write_adlib(0xbd, adlib_data[0xbd] & ~(1 << 5));
                                        }
                                        break;
                                }
                                break;
                            case 0xc0: /*patch change*/
                                x = getnext(1);
                                ch[c].inum = (int)x;
                                for (j = 0; j < 11; j++)
                                    ch[c].ins[j] = myinsbank[ch[c].inum, j];
                                break;
                            case 0xd0: /*chanel touch*/
                                x = getnext(1);
                                break;
                            case 0xe0: /*pitch wheel*/
                                x = getnext(1);
                                x = getnext(1);
                                break;
                            case 0xf0:
                                switch (v)
                                {
                                    case 0xf0:
                                    case 0xf7: /*sysex*/
                                        l = getval();
                                        if (datalook(pos + l) == 0xf7)
                                            i = 1;
//								Console.WriteLine ("{0}", l);
                                        
                                        if (datalook(pos) == 0x7d &&
                                        datalook(pos + 1) == 0x10 &&
                                        datalook(pos + 2) < 16)
                                        {
                                            adlib_style = AdlibStyles.Lucas | AdlibStyles.Midi;
                                            for (i = 0; i < l; i++)
                                            {
//										Console.Write ("%x ", datalook (pos + i));
//										if ((i - 3) % 10 == 0)
//											Console.WriteLine ();
                                            }
//									Console.WriteLine ();
                                            getnext(1);
                                            getnext(1);
                                            c = (int)getnext(1);
                                            getnext(1);

                                            //  getnext(22); //temp
                                            ch[c].ins[0] = (byte)((getnext(1) << 4) + getnext(1));
                                            ch[c].ins[2] = (byte)(0xff - (((getnext(1) << 4) + getnext(1)) & 0x3f));
                                            ch[c].ins[4] = (byte)(0xff - ((getnext(1) << 4) + getnext(1)));
                                            ch[c].ins[6] = (byte)(0xff - ((getnext(1) << 4) + getnext(1)));
                                            ch[c].ins[8] = (byte)((getnext(1) << 4) + getnext(1));

                                            ch[c].ins[1] = (byte)((getnext(1) << 4) + getnext(1));
                                            ch[c].ins[3] = (byte)(0xff - (((getnext(1) << 4) + getnext(1)) & 0x3f));
                                            ch[c].ins[5] = (byte)(0xff - ((getnext(1) << 4) + getnext(1)));
                                            ch[c].ins[7] = (byte)(0xff - ((getnext(1) << 4) + getnext(1)));
                                            ch[c].ins[9] = (byte)((getnext(1) << 4) + getnext(1));

                                            i = (int)((getnext(1) << 4) + getnext(1));
                                            ch[c].ins[10] = (byte)i;

                                            //if ((i&1)==1) ch[c].ins[10]=1;

//									Console.Write ("\n{0}: ", c);
//									for (i = 0; i < 11; i++)
//										Console.Write ("{0:X2} ", ch [c].ins [i]);
                                            getnext(l - 26);
                                        }
                                        else
                                        {
//									Console.WriteLine ();
//									for (j = 0; j < l; j++)
//										Console.Write ("{0:X2} ", getnext (1));
                                        }

//								Console.WriteLine ();
                                        if (i == 1)
                                            getnext(1);
                                        break;
                                    case 0xf1:
                                        break;
                                    case 0xf2:
                                        getnext(2);
                                        break;
                                    case 0xf3:
                                        getnext(1);
                                        break;
                                    case 0xf4:
                                        break;
                                    case 0xf5:
                                        break;
                                    case 0xf6: /*something*/
                                    case 0xf8:
                                    case 0xfa:
                                    case 0xfb:
                                    case 0xfc:
                                    //this ends the track for sierra.
                                        if (type == FileType.Sierra ||
                                        type == FileType.AdvancedSierra)
                                        {
                                            track[curtrack].tend = pos;
//									Console.WriteLine ("endmark: {0} -- {1:X}", pos, pos);
                                        }
                                        break;
                                    case 0xfe:
                                        break;
                                    case 0xfd:
                                        break;
                                    case 0xff:
                                        v = getnext(1);
                                        l = getval();
//								Console.WriteLine ();
//								Console.Write ("[{0:X}_{1:X}]", v, l);
                                        if (v == 0x51)
                                        {
                                            lnum = getnext(l);
                                            msqtr = lnum; /*set tempo*/
//									Console.Write ("(qtr={0})", msqtr);
                                        }
                                        else
                                        {
//									for (i = 0; i < l; i++)
//										Console.Write ("{0:X2} ", getnext (1));
                                        }
                                        break;
                                }
                                break;
                            default:
//							Console.Write ("!", v); /* if we get down here, a error occurred */
                                break;
                        }

                        if (pos < track[curtrack].tend)
                        {
                            if (type != FileType.Sierra && type != FileType.AdvancedSierra)
                                w = getval();
                            else
                                w = getnext(1);
                            track[curtrack].iwait = w;
                            /*
            if (w!=0)
                {
                midiprintf("\n<%d>",w);
                f = 
((float)w/(float)deltas)*((float)msqtr/(float)1000000);
                if (doing==1) f=0; //not playing yet. don't wait yet
                }
                */
                        }
                        else
                            track[curtrack].iwait = 0;

                        track[curtrack].pos = pos;
                    }


                ret = 0; //end of song.
                iwait = 0;
                for (curtrack = 0; curtrack < 16; curtrack++)
                    if (track[curtrack].on == 1 &&
                    track[curtrack].pos < track[curtrack].tend)
                        ret = 1;  //not yet..

                if (ret == 1)
                {
                    iwait = 0xffffff;  // bigger than any wait can be!
                    for (curtrack = 0; curtrack < 16; curtrack++)
                        if (track[curtrack].on == 1 &&
                        track[curtrack].pos < track[curtrack].tend &&
                        track[curtrack].iwait < iwait)
                            iwait = track[curtrack].iwait;
                }
            }


            if (iwait != 0 && ret == 1)
            {
                for (curtrack = 0; curtrack < 16; curtrack++)
                    if (track[curtrack].on != 0)
                        track[curtrack].iwait -= iwait;


                fwait = 1.0f / (((float)iwait / (float)deltas) * ((float)msqtr / (float)1000000));
            }
            else
                fwait = 50;  // 1/50th of a second

//			Console.WriteLine ();
//			for (i = 0; i < 16; i++)
//				if (track [i].on != 0) {
//					if (track [i].pos < track [i].tend)
//						Console.Write ("<{0}>", track [i].iwait);
//					else
//						Console.Write ("stop");
//				}

            /*
    if (ret==0 && type==FILE_ADVSIERRA)
        if (datalook(sierra_pos-2)!=0xff)
            {
            midiprintf ("next sectoin!");
            sierra_next_section(p);
            fwait=50;
            ret=1;
            }
    */

            return (ret != 0);
        }
    }
}

