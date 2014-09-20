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
using NScumm.Core.Audio.OPL;

namespace NScumm.Core.Audio.Midi
{
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

    public partial class MidiPlayer: IMusicPlayer
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

        readonly byte[,] myinsbank = new byte[128, 16];
        byte[,] smyinsbank = new byte[128, 16];

        IOpl opl;

        public MidiPlayer(IOpl opl)
        {   
            this.opl = opl;
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

        public void Rewind(int subsong)
        {
            pos = 0;
            tins = 0;
            adlib_style = AdlibStyles.Midi | AdlibStyles.Cmf;
            adlib_mode = AdlibMode.Melodic;
            for (var i = 0; i < 128; i++)
                for (var j = 0; j < 14; j++)
                    myinsbank[i, j] = midiFmInstruments[i, j];

            for (var i = 0; i < 16; i++)
            {
                ch[i] = new MidiChannel();
                ch[i].inum = 0;
                for (var j = 0; j < 11; j++)
                    ch[i].ins[j] = myinsbank[ch[i].inum, j];
                ch[i].vol = 127;
                ch[i].nshift = -25;
                ch[i].on = 1;
            }

            /* General init */
            for (var i = 0; i < 9; i++)
            {
                chp[i, 0] = -1;
                chp[i, 2] = 0;
            }

            deltas = 250;  // just a number,  not a standard
            msqtr = 500000;
            fwait = 123; // gotta be a small thing.. sorta like nothing
            iwait = 0;

            subsongs = 1;

            for (var i = 0; i < 16; i++)
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
            getnext(1);
            switch (type)
            {
                case FileType.Lucas:
                case FileType.Midi:
                    InitMidi();
                    break;
                case FileType.Cmf:
                    InitCmf();
                    break;
                case FileType.OldLucas:
                    InitOldLucas();
                    break;
                case FileType.AdvancedSierra:
                    InitAdvancedSierra(subsong);
                    break;
                case FileType.Sierra:
                    InitSierra();
                    break;
            }


            /*        sprintf(info,"%s\r\nTicks/Quarter Note: %ld\r\n",info,deltas);
        sprintf(info,"%sms/Quarter Note: %ld",info,msqtr); */

            for (var i = 0; i < 16; i++)
                if (track[i].on != 0)
                {
                    track[i].pos = track[i].spos;
                    track[i].pv = 0;
                    track[i].iwait = 0;
                }

            doing = 1;
            midi_fm_reset();
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
                                                //                              Console.Write ("(pb:{0}: {1} {2})", c, ctrl, vel);
                                        ch[c].vol = (int)vel;
                                                //                              Console.Write ("vol");
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
                                            //                                  Console.WriteLine (" AM+VIB depth change - AM {0}, VIB {1}",
                                            //                                      (adlib_data [0xbd] & 0x80) != 0 ? "on" : "off",
                                            //                                      (adlib_data [0xbd] & 0x40) != 0 ? "on" : "off"
                                            //                                  );
                                        }
                                        break;
                                    case 0x67:
                                                //                              Console.WriteLine ("Rhythm mode: {0}", vel);
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
                                                //                              Console.WriteLine ("{0}", l);

                                        if (datalook(pos) == 0x7d &&
                                            datalook(pos + 1) == 0x10 &&
                                            datalook(pos + 2) < 16)
                                        {
                                            adlib_style = AdlibStyles.Lucas | AdlibStyles.Midi;
                                            for (i = 0; i < l; i++)
                                            {
                                                //                                      Console.Write ("%x ", datalook (pos + i));
                                                //                                      if ((i - 3) % 10 == 0)
                                                //                                          Console.WriteLine ();
                                            }
                                            //                                  Console.WriteLine ();
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

                                            //                                  Console.Write ("\n{0}: ", c);
                                            //                                  for (i = 0; i < 11; i++)
                                            //                                      Console.Write ("{0:X2} ", ch [c].ins [i]);
                                            getnext(l - 26);
                                        }
                                        else
                                        {
                                            //                                  Console.WriteLine ();
                                            //                                  for (j = 0; j < l; j++)
                                            //                                      Console.Write ("{0:X2} ", getnext (1));
                                        }

                                                //                              Console.WriteLine ();
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
                                            //                                  Console.WriteLine ("endmark: {0} -- {1:X}", pos, pos);
                                        }
                                        break;
                                    case 0xfe:
                                        break;
                                    case 0xfd:
                                        break;
                                    case 0xff:
                                        v = getnext(1);
                                        l = getval();
                                                //                              Console.WriteLine ();
                                                //                              Console.Write ("[{0:X}_{1:X}]", v, l);
                                        if (v == 0x51)
                                        {
                                            lnum = getnext(l);
                                            msqtr = lnum; /*set tempo*/
                                            //                                  Console.Write ("(qtr={0})", msqtr);
                                        }
                                        else
                                        {
                                            //                                  for (i = 0; i < l; i++)
                                            //                                      Console.Write ("{0:X2} ", getnext (1));
                                        }
                                        break;
                                }
                                break;
                            default:
                                        //                          Console.Write ("!", v); /* if we get down here, a error occurred */
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

            //          Console.WriteLine ();
            //          for (i = 0; i < 16; i++)
            //              if (track [i].on != 0) {
            //                  if (track [i].pos < track [i].tend)
            //                      Console.Write ("<{0}>", track [i].iwait);
            //                  else
            //                      Console.Write ("stop");
            //              }

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

        void InitMidi()
        {
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
        }

        void InitCmf()
        {
            getnext(3);  // ctmf
            getnexti(2); //version
            var n = getnexti(2); // instrument offset
            var m = getnexti(2); // music offset
            deltas = getnexti(2); //ticks/qtr note
            msqtr = 1000000 / getnexti(2) * deltas;
            //the stuff in the cmf is click ticks per second..

            var i = getnexti(2);
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
            for (var j = 0; j < i; j++)
            {
                //                        Console.Write("\n{0}: ", j);
                for (var l = 0; l < 16; l++)
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
        }

        void InitOldLucas()
        {
            byte[] ins = new byte[16];

            msqtr = 250000;
            pos = 9;
            deltas = getnext(1);

            var i = 8;
            pos = 0x19;  // jump to instruments
            tins = (int)i;
            for (var j = 0; j < i; j++)
            {
                //                        Console.Write("\n{0}: ", j);
                for (var l = 0; l < 16; l++)
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
                    for (var j = 0; j < 11; j++)
                        ch[i].ins[j] = myinsbank[ch[i].inum, j];
                }
            }

            adlib_style = AdlibStyles.Lucas | AdlibStyles.Midi;

            curtrack = 0;
            track[curtrack].on = 1;
            track[curtrack].tend = data.Length;  // music until the end of the file
            track[curtrack].spos = 0x98;  //jump to midi music
        }

        void InitSierra()
        {
            memcpy(myinsbank, smyinsbank, 128, 16);
            getnext(2);
            deltas = 0x20;

            curtrack = 0;
            track[curtrack].on = 1;
            track[curtrack].tend = data.Length;  // music until the end of the file

            for (var i = 0; i < 16; i++)
            {
                ch[i].nshift = -13;
                ch[i].on = (int)getnext(1);
                ch[i].inum = (int)getnext(1);
                for (var j = 0; j < 11; j++)
                    ch[i].ins[j] = myinsbank[ch[i].inum, j];
            }

            track[curtrack].spos = pos;
            adlib_style = AdlibStyles.Sierra | AdlibStyles.Midi;
        }

        void InitAdvancedSierra(int subsong)
        {
            memcpy(myinsbank, smyinsbank, 128, 16);
            deltas = 0x20;
            getnext(11); //worthless empty space and "stuff" :)

            var o_sierra_pos = sierra_pos = pos;
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
            var i = 0;
            while (i != subsong)
            {
                sierra_next_section();
                i++;
            }

            adlib_style = AdlibStyles.Sierra | AdlibStyles.Midi;  //advanced sierra tunes use volume
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
    }
}

