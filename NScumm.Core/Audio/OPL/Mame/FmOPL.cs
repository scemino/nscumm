//
//  FmOPL.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Core
{
    class OPL_SLOT
    {
        public int TL;
        /* total level     :TL << 8             */
        public int TLL;
        /* adjusted now TL                      */
        public byte KSR;
        /* key scale rate  :(shift down bit)    */
        public Func<int,int> AR;
        /* attack rate     :&AR_TABLE[AR<<2]    */
        public Func<int,int> DR;
        /* decay rate      :&DR_TABLE[DR<<2]    */
        public int SL;
        /* sustain level   :SL_TABLE[SL]        */
        public Func<int,int> RR;
        /* release rate    :&DR_TABLE[RR<<2]    */
        public byte ksl;
        /* keyscale level  :(shift down bits)   */
        public byte ksr;
        /* key scale rate  :kcode>>KSR          */
        public uint mul;
        /* multiple        :ML_TABLE[ML]        */
        public uint Cnt;
        /* frequency count                      */
        public uint Incr;
        /* frequency step                       */

        /* envelope generator state */
        public byte eg_typ;
        /* envelope type flag                  */
        public byte evm;
        /* envelope phase                       */
        public int evc;
        /* envelope counter                     */
        public int eve;
        /* envelope counter end point           */
        public int evs;
        /* envelope counter step                */
        public int evsa;
        /* envelope step for AR :AR[ksr]        */
        public int evsd;
        /* envelope step for DR :DR[ksr]        */
        public int evsr;
        /* envelope step for RR :RR[ksr]        */

        /* LFO */
        public byte ams;
        /* ams flag                            */
        public byte vib;
        /* vibrate flag                        */
        /* wave selector */
        public Func<int,int> wavetable;
    }

    class OPL_CH
    {
        public OPL_SLOT[] SLOT = new OPL_SLOT[2];
        public byte CON;
        /* connection type                  */
        public byte FB;
        /* feed back       :(shift down bit)*/
        public Func<int,int> connect1;
        /* slot1 output pointer             */
        public Func<int,int> connect2;
        /* slot2 output pointer             */
        public int[] op1_out = new int[2];
        /* slot1 output for selfeedback     */

        /* phase generator state */
        public uint block_fnum;
        /* block+fnum                       */
        public byte kcode;
        /* key code        : KeyScaleCode   */
        public uint fc;
        /* Freq. Increment base             */
        public uint ksl_base;
        /* KeyScaleLevel Base step          */
        public byte keyon;
        /* key on/off flag                  */
    }

    class FmOPL
    {
        const int FMOPL_ENV_BITS_HQ = 16;
        const int FMOPL_ENV_BITS_MQ = 8;
        const int FMOPL_ENV_BITS_LQ = 8;
        const int FMOPL_EG_ENT_HQ = 4096;
        const int FMOPL_EG_ENT_MQ = 1024;
        const int FMOPL_EG_ENT_LQ = 12;

        const int OPL_TYPE_WAVESEL = 0x01;
        /* waveform select    */

        delegate void OPL_IRQHANDLER(int param,int irq);

        delegate void OPL_TIMERHANDLER(int channel,double interval_Sec);

        delegate void OPL_UPDATEHANDLER(int param,int min_interval_us);

        byte type;
        /* chip type                         */
        int clock;
        /* master clock  (Hz)                */
        int rate;
        /* sampling rate (Hz)                */
        double freqbase;
        /* frequency base                    */
        double TimerBase;
        /* Timer base time (==sampling time) */
        byte address;
        /* address register                  */
        byte status;
        /* status flag                       */
        byte statusmask;
        /* status mask                       */
        uint mode;
        /* Reg.08 : CSM , notesel,etc.       */

        /* Timer */
        int[] T = new int[2];
        /* timer counter                     */
        byte[] st = new byte[2];
        /* timer enable                      */

        /* FM channel slots */
        OPL_CH[] P_CH;
        /* pointer of CH                     */
        int max_ch;
        /* maximum channel                   */

        /* Rythm sention */
        byte rythm;
        /* Rythm mode , key flag */

        /* time tables */
        int[] AR_TABLE = new int[76];
        /* atttack rate tables              */
        int[] DR_TABLE = new int[76];
        /* decay rate tables                */
        uint[] FN_TABLE = new uint[1024];
        /* fnumber . increment counter     */

        /* LFO */
        Func<int,int> ams_table;
        Func<int,int> vib_table;
        int amsCnt;
        int amsIncr;
        int vibCnt;
        int vibIncr;

        /* wave selector enable flag */
        byte wavesel;

        /* external event callback handler */
        OPL_TIMERHANDLER TimerHandler;
        /* TIMER handler   */
        int TimerParam;
        /* TIMER parameter */
        OPL_IRQHANDLER IRQHandler;
        /* IRQ handler    */
        int IRQParam;
        /* IRQ parameter  */
        OPL_UPDATEHANDLER UpdateHandler;
        /* stream update handler   */
        int UpdateParam;
        /* stream update parameter */

        Random rnd;

        public FmOPL(int type, int clock, int rate)
        {
            // We need to emulate one YM3812 chip
            int env_bits = FMOPL_ENV_BITS_HQ;
            int eg_ent = FMOPL_EG_ENT_HQ;

            OPLBuildTables(env_bits, eg_ent);

            int max_ch = 9; /* normaly 9 channels */

            if (!OPL_LockTable())
                throw new InvalidOperationException();

            /* clear */
            P_CH = new OPL_CH[max_ch];

            /* set channel state pointer */
            this.type = (byte)type;
            this.clock = clock;
            this.rate = rate;
            this.max_ch = max_ch;

            // Init the random source. Note: We use a fixed name for it here.
            // So if multiple FM_OPL objects exist in parallel, then their
            // random sources will have an equal name. At least in the
            // current EventRecorder implementation, this causes no problems;
            // but this is probably not guaranteed.
            // Alas, it does not seem worthwhile to bother much with this
            // at the time, so I am leaving it as it is.
            rnd = new Random();

            /* init grobal tables */
            OPL_initalize();

            /* reset chip */
            OPLResetChip();
        }

        void OPL_initalize()
        {
            int fn;

            /* frequency base */
            freqbase = (rate != 0) ? ((double)clock / rate) / 72 : 0;
            /* Timer base time */
            TimerBase = 1.0 / ((double)clock / 72.0);
            /* make time tables */
            Init_timetables(OPL_ARRATE, OPL_DRRATE);
            /* make fnumber . increment counter table */
            for (fn = 0; fn < 1024; fn++)
            {
                FN_TABLE[fn] = (uint)(freqbase * fn * FREQ_RATE * (1 << 7) / 2);
            }
            /* LFO freq.table */
            amsIncr = (int)(rate != 0 ? (double)AMS_ENT * (1 << AMS_SHIFT) / rate * 3.7 * ((double)clock / 3600000) : 0);
            vibIncr = (int)(rate != 0 ? (double)VIB_ENT * (1 << VIB_SHIFT) / rate * 6.4 * ((double)clock / 3600000) : 0);
        }

        void Init_timetables(int ARRATE, int DRRATE)
        {
            int i;
            double rate;

            /* make attack rate & decay rate tables */
            for (i = 0; i < 4; i++)
                AR_TABLE[i] = DR_TABLE[i] = 0;
            for (i = 4; i <= 60; i++)
            {
                rate = freqbase;                       /* frequency rate */
                if (i < 60)
                    rate *= 1.0 + (i & 3) * 0.25;       /* b0-1 : x1 , x1.25 , x1.5 , x1.75 */
                rate *= 1 << ((i >> 2) - 1);                        /* b2-5 : shift bit */
                rate *= (double)(EG_ENT << ENV_BITS);
                AR_TABLE[i] = (int)(rate / ARRATE);
                DR_TABLE[i] = (int)(rate / DRRATE);
            }
            for (i = 60; i < 76; i++)
            {
                AR_TABLE[i] = EG_AED - 1;
                DR_TABLE[i] = DR_TABLE[60];
            }
        }

        public void OPLResetChip()
        {
            /* reset chip */
            mode = 0;  /* normal mode */
            OPL_STATUS_RESET(0x7f);
            /* reset with register write */
            OPLWriteReg(0x01, 0); /* wabesel disable */
            OPLWriteReg(0x02, 0); /* Timer1 */
            OPLWriteReg(0x03, 0); /* Timer2 */
            OPLWriteReg(0x04, 0); /* IRQ mask clear */
            for (var i = 0xff; i >= 0x20; i--)
                OPLWriteReg(i, 0);
            /* reset OPerator parameter */
            for (var c = 0; c < max_ch; c++)
            {
                var CH = P_CH[c];
                /* P_CH[c].PAN = OPN_CENTER; */
                for (var s = 0; s < 2; s++)
                {
                    /* wave table */
                    CH.SLOT[s].wavetable = SIN_TABLE[0];
                    /* CH.SLOT[s].evm = ENV_MOD_RR; */
                    CH.SLOT[s].evc = EG_OFF;
                    CH.SLOT[s].eve = EG_OFF + 1;
                    CH.SLOT[s].evs = 0;
                }
            }
        }

        void OPL_STATUS_RESET(int flag)
        {
            /* reset status flag */
            status &= (byte)(~flag);
            if ((status & 0x80) != 0)
            {
                if ((status & statusmask) == 0)
                {
                    status &= 0x7f;
                    /* callback user interrupt handler (IRQ is ON to OFF) */
                    if (IRQHandler != null)
                        IRQHandler(IRQParam, 0);
                }
            }
        }

        /* multiple table */
        static uint ML(double a)
        {
            return (uint)(a * 2);
        }

        static readonly uint[] MUL_TABLE =
            {
                /* 1/2, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15 */
                ML(0.50), ML(1.00), ML(2.00),  ML(3.00), ML(4.00), ML(5.00), ML(6.00), ML(7.00),
                ML(8.00), ML(9.00), ML(10.00), ML(10.00), ML(12.00), ML(12.00), ML(15.00), ML(15.00)
            };

        /* set multi,am,vib,EG-TYP,KSR,mul */
        void set_mul(int slot, int v)
        {
            OPL_CH CH = P_CH[slot >> 1];
            OPL_SLOT SLOT = CH.SLOT[slot & 1];

            SLOT.mul = MUL_TABLE[v & 0x0f];
            SLOT.KSR = (v & 0x10) != 0 ? (byte)0 : (byte)2;
            SLOT.eg_typ = (byte)((v & 0x20) >> 5);
            SLOT.vib = (byte)(v & 0x40);
            SLOT.ams = (byte)(v & 0x80);
            CALC_FCSLOT(CH, SLOT);
        }

        /* ---------- frequency counter for operater update ---------- */
        void CALC_FCSLOT(OPL_CH CH, OPL_SLOT SLOT)
        {
            int ksr;

            /* frequency step counter */
            SLOT.Incr = CH.fc * SLOT.mul;
            ksr = CH.kcode >> SLOT.KSR;

            if (SLOT.ksr != ksr)
            {
                SLOT.ksr = (byte)ksr;
                /* attack , decay rate recalcration */
                SLOT.evsa = SLOT.AR(ksr);
                SLOT.evsd = SLOT.DR(ksr);
                SLOT.evsr = SLOT.RR(ksr);
            }
            SLOT.TLL = (int)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
        }

        /* register number to channel number , slot offset */
        const int SLOT1 = 0;
        const int SLOT2 = 1;

        /* envelope phase */
        const int ENV_MOD_RR = 0x00;
        const int ENV_MOD_DR = 0x01;
        const int ENV_MOD_AR = 0x02;

        /* ---------- write a OPL registers ---------- */
        public void OPLWriteReg(int r, int v)
        {
            OPL_CH CH;
            int slot;
            uint block_fnum;

            switch (r & 0xe0)
            {
                case 0x00: /* 00-1f:controll */
                    switch (r & 0x1f)
                    {
                        case 0x01:
                            /* wave selector enable */
                            if ((type & OPL_TYPE_WAVESEL) != 0)
                            {
                                wavesel = (byte)(v & 0x20);
                                if (wavesel == 0)
                                {
                                    /* preset compatible mode */
                                    for (var c = 0; c < max_ch; c++)
                                    {
                                        P_CH[c].SLOT[SLOT1].wavetable = SIN_TABLE[0];
                                        P_CH[c].SLOT[SLOT2].wavetable = SIN_TABLE[0];
                                    }
                                }
                            }
                            return;
                        case 0x02:  /* Timer 1 */
                            T[0] = (256 - v) * 4;
                            break;
                        case 0x03:  /* Timer 2 */
                            T[1] = (256 - v) * 16;
                            return;
                        case 0x04:  /* IRQ clear / mask and Timer enable */
                            if ((v & 0x80) != 0)
                            { /* IRQ flag clear */
                                OPL_STATUS_RESET(0x7f);
                            }
                            else
                            {    /* set IRQ mask ,timer enable*/
                                byte st1 = (byte)(v & 1);
                                byte st2 = (byte)((v >> 1) & 1);
                                /* IRQRST,T1MSK,t2MSK,EOSMSK,BRMSK,x,ST2,ST1 */
                                OPL_STATUS_RESET(v & 0x78);
                                OPL_STATUSMASK_SET(((~v) & 0x78) | 0x01);
                                /* timer 2 */
                                if (st[1] != st2)
                                {
                                    double interval = st2 != 0 ? (double)T[1] * TimerBase : 0.0;
                                    st[1] = st2;
                                    if (TimerHandler != null)
                                        TimerHandler(TimerParam + 1, interval);
                                }
                                /* timer 1 */
                                if (st[0] != st1)
                                {
                                    double interval = st1 != 0 ? (double)T[0] * TimerBase : 0.0;
                                    st[0] = st1;
                                    if (TimerHandler != null)
                                        TimerHandler(TimerParam + 0, interval);
                                }
                            }
                            return;
                    }
                    break;
                case 0x20:  /* am,vib,ksr,eg type,mul */
                    slot = slot_array[r & 0x1f];
                    if (slot == -1)
                        return;
                    set_mul(slot, v);
                    return;
                case 0x40:
                    slot = slot_array[r & 0x1f];
                    if (slot == -1)
                        return;
                    set_ksl_tl(slot, v);
                    return;
                case 0x60:
                    slot = slot_array[r & 0x1f];
                    if (slot == -1)
                        return;
                    set_ar_dr(slot, v);
                    return;
                case 0x80:
                    slot = slot_array[r & 0x1f];
                    if (slot == -1)
                        return;
                    set_sl_rr(slot, v);
                    return;
                case 0xa0:
                    switch (r)
                    {
                        case 0xbd:
                            /* amsep,vibdep,r,bd,sd,tom,tc,hh */
                            {
                                byte rkey = (byte)(rythm ^ v);
                                ams_table = new Func<int, int>(index => AMS_TABLE[(v & 0x80) != 0 ? index + AMS_ENT : index + 0]);
                                vib_table = new Func<int, int>(index => VIB_TABLE[(v & 0x40) != 0 ? index + VIB_ENT : index + 0]);
                                rythm = (byte)(v & 0x3f);
                                if ((rythm & 0x20) != 0)
                                {
                                    /* BD key on/off */
                                    if ((rkey & 0x10) != 0)
                                    {
                                        if ((v & 0x10) != 0)
                                        {
                                            P_CH[6].op1_out[0] = P_CH[6].op1_out[1] = 0;
                                            OPL_KEYON(P_CH[6].SLOT[SLOT1]);
                                            OPL_KEYON(P_CH[6].SLOT[SLOT2]);
                                        }
                                        else
                                        {
                                            OPL_KEYOFF(P_CH[6].SLOT[SLOT1]);
                                            OPL_KEYOFF(P_CH[6].SLOT[SLOT2]);
                                        }
                                    }
                                    /* SD key on/off */
                                    if ((rkey & 0x08) != 0)
                                    {
                                        if ((v & 0x08) != 0)
                                            OPL_KEYON(P_CH[7].SLOT[SLOT2]);
                                        else
                                            OPL_KEYOFF(P_CH[7].SLOT[SLOT2]);
                                    }/* TAM key on/off */
                                    if ((rkey & 0x04) != 0)
                                    {
                                        if ((v & 0x04) != 0)
                                            OPL_KEYON(P_CH[8].SLOT[SLOT1]);
                                        else
                                            OPL_KEYOFF(P_CH[8].SLOT[SLOT1]);
                                    }
                                    /* TOP-CY key on/off */
                                    if ((rkey & 0x02) != 0)
                                    {
                                        if ((v & 0x02) != 0)
                                            OPL_KEYON(P_CH[8].SLOT[SLOT2]);
                                        else
                                            OPL_KEYOFF(P_CH[8].SLOT[SLOT2]);
                                    }
                                    /* HH key on/off */
                                    if ((rkey & 0x01) != 0)
                                    {
                                        if ((v & 0x01) != 0)
                                            OPL_KEYON(P_CH[7].SLOT[SLOT1]);
                                        else
                                            OPL_KEYOFF(P_CH[7].SLOT[SLOT1]);
                                    }
                                }
                            }
                            return;

                        default:
                            break;
                    }
                    /* keyon,block,fnum */
                    if ((r & 0x0f) > 8)
                        return;
                    CH = P_CH[r & 0x0f];
                    if ((r & 0x10) == 0)
                    {    /* a0-a8 */
                        block_fnum = (uint)((CH.block_fnum & 0x1f00) | v);
                    }
                    else
                    {    /* b0-b8 */
                        int keyon = (v >> 5) & 1;
                        block_fnum = (uint)(((v & 0x1f) << 8) | (CH.block_fnum & 0xff));
                        if (CH.keyon != keyon)
                        {
                            if ((CH.keyon = (byte)keyon) != 0)
                            {
                                CH.op1_out[0] = CH.op1_out[1] = 0;
                                OPL_KEYON(CH.SLOT[SLOT1]);
                                OPL_KEYON(CH.SLOT[SLOT2]);
                            }
                            else
                            {
                                OPL_KEYOFF(CH.SLOT[SLOT1]);
                                OPL_KEYOFF(CH.SLOT[SLOT2]);
                            }
                        }
                    }
                    /* update */
                    if (CH.block_fnum != block_fnum)
                    {
                        int blockRv = (int)(7 - (block_fnum >> 10));
                        int fnum = (int)(block_fnum & 0x3ff);
                        CH.block_fnum = block_fnum;
                        CH.ksl_base = KSL_TABLE[block_fnum >> 6];
                        CH.fc = FN_TABLE[fnum] >> blockRv;
                        CH.kcode = (byte)(CH.block_fnum >> 9);
                        if (((mode & 0x40) != 0) && ((CH.block_fnum & 0x100) != 0))
                            CH.kcode |= 1;
                        CALC_FCSLOT(CH, CH.SLOT[SLOT1]);
                        CALC_FCSLOT(CH, CH.SLOT[SLOT2]);
                    }
                    return;
                case 0xc0:
                    /* FB,C */
                    if ((r & 0x0f) > 8)
                        return;
                    CH = P_CH[r & 0x0f];
                    {
                        int feedback = (v >> 1) & 7;
                        CH.FB = feedback != 0 ? (byte)((8 + 1) - feedback) : (byte)0;
                        CH.CON = (byte)(v & 1);
                        set_algorythm(CH);
                    }
                    return;
                case 0xe0: /* wave type */
                    slot = slot_array[r & 0x1f];
                    if (slot == -1)
                        return;
                    CH = P_CH[slot >> 1];
                    if (wavesel != 0)
                    {
                        CH.SLOT[slot & 1].wavetable = SIN_TABLE[(v & 0x03) * SIN_ENT];
                    }
                    return;
            }
        }

        /* ---------- YM3812 I/O interface ---------- */
        public int OPLWrite(int a, int v)
        {
            if ((a & 1) == 0)
            { /* address port */
                address = (byte)(v & 0xff);
            }
            else
            {    /* data port */
                if (UpdateHandler != null)
                    UpdateHandler(UpdateParam, 0);
                OPLWriteReg(address, v);
            }
            return status >> 7;
        }

        public byte OPLRead(int a)
        {
            if ((a & 1) == 0)
            { /* status port */
                return (byte)(status & (statusmask | 0x80));
            }
            /* data port */
            switch (address)
            {
                case 0x05: /* KeyBoard IN */
//                    Console.Error.WriteLine("OPL:read unmapped KEYBOARD port");
                    return 0;
                case 0x19: /* I/O DATA    */
//                    Console.Error.WriteLine("OPL:read unmapped I/O port");
                    return 0;
                case 0x1a: /* PCM-DATA    */
                    return 0;
            }
            return 0;
        }

        /*******************************************************************************/
        /*      YM3812 local section                                                   */
        /*******************************************************************************/

        /* ---------- update one of chip ----------- */
        public void YM3812UpdateOne(short[] buffer, int length)
        {
            int i;
            int data;
            short[] buf = buffer;
            int amsCnt = this.amsCnt;
            int vibCnt = this.vibCnt;
            byte rythm = (byte)(rythm & 0x20);
            OPL_CH CH, R_CH;


            if (this != cur_chip)
            {
                cur_chip = this;
                /* channel pointers */
                S_CH = P_CH;
                E_CH = S_CH[9];
                /* rythm slot */
                SLOT7_1 = S_CH[7].SLOT[SLOT1];
                SLOT7_2 = S_CH[7].SLOT[SLOT2];
                SLOT8_1 = S_CH[8].SLOT[SLOT1];
                SLOT8_2 = S_CH[8].SLOT[SLOT2];
                /* LFO state */
                amsIncr = this.amsIncr;
                vibIncr = this.vibIncr;
                ams_table = this.ams_table;
                vib_table = this.vib_table;
            }
            R_CH = rythm != 0 ? S_CH[6] : E_CH;
            for (i = 0; i < length; i++)
            {
                /*            channel A         channel B         channel C      */
                /* LFO */
                ams = ams_table((amsCnt += amsIncr) >> AMS_SHIFT);
                vib = vib_table((vibCnt += vibIncr) >> VIB_SHIFT);
                outd[0] = 0;
                /* FM part */
                for (CH = S_CH; CH < R_CH; CH++)
                    OPL_CALC_CH(CH);
                /* Rythn part */
                if (rythm)
                    OPL_CALC_RH(OPL, S_CH);
                /* limit check */
                data = CLIP(outd[0], OPL_MINOUT, OPL_MAXOUT);
                /* store to sound buffer */
                buf[i] = data >> OPL_OUTSB;
            }

            amsCnt = amsCnt;
            vibCnt = vibCnt;
        }

        /* ---------- calcrate one of channel ---------- */
        void OPL_CALC_CH(OPL_CH CH)
        {
            uint env_out;
            OPL_SLOT* SLOT;

            feedback2 = 0;
            /* SLOT 1 */
            SLOT = &CH.SLOT[SLOT1];
            env_out = OPL_CALC_SLOT(SLOT);
            if (env_out < (uint)(EG_ENT - 1))
            {
                /* PG */
                if (SLOT.vib)
                    SLOT.Cnt += (SLOT.Incr * vib) >> VIB_RATE_SHIFT;
                else
                    SLOT.Cnt += SLOT.Incr;
                /* connection */
                if (CH.FB)
                {
                    int feedback1 = (CH.op1_out[0] + CH.op1_out[1]) >> CH.FB;
                    CH.op1_out[1] = CH.op1_out[0];
                    *CH.connect1 += CH.op1_out[0] = OP_OUT(SLOT, env_out, feedback1);
                }
                else
                {
                    *CH.connect1 += OP_OUT(SLOT, env_out, 0);
                }
            }
            else
            {
                CH.op1_out[1] = CH.op1_out[0];
                CH.op1_out[0] = 0;
            }
            /* SLOT 2 */
            SLOT = &CH.SLOT[SLOT2];
            env_out = OPL_CALC_SLOT(SLOT);
            if (env_out < (uint)(EG_ENT - 1))
            {
                /* PG */
                if (SLOT.vib)
                    SLOT.Cnt += (SLOT.Incr * vib) >> VIB_RATE_SHIFT;
                else
                    SLOT.Cnt += SLOT.Incr;
                /* connection */
                outd[0] += OP_OUT(SLOT, env_out, feedback2);
            }
        }

        /* ---------- calcrate rythm block ---------- */
        const double WHITE_NOISE_db = 6.0;

        void OPL_CALC_RH(OPL_CH CH)
        {
            uint env_tam, env_sd, env_top, env_hh;
            // This code used to do int(rnd.getRandomBit() * (WHITE_NOISE_db / EG_STEP)),
            // but EG_STEP = 96.0/EG_ENT, and WHITE_NOISE_db=6.0. So, that's equivalent to
            // int(rnd.getRandomBit() * EG_ENT/16). We know that EG_ENT is 4096, or 1024,
            // or 128, so we can safely avoid any FP ops.
            int whitenoise = rnd.getRandomBit() * (EG_ENT >> 4);

            int tone8;

            OPL_SLOT* SLOT;
            int env_out;

            /* BD : same as FM serial mode and output level is large */
            feedback2 = 0;
            /* SLOT 1 */
            SLOT = &CH[6].SLOT[SLOT1];
            env_out = OPL_CALC_SLOT(SLOT);
            if (env_out < EG_ENT - 1)
            {
                /* PG */
                if (SLOT.vib)
                    SLOT.Cnt += (SLOT.Incr * vib) >> VIB_RATE_SHIFT;
                else
                    SLOT.Cnt += SLOT.Incr;
                /* connection */
                if (CH[6].FB)
                {
                    int feedback1 = (CH[6].op1_out[0] + CH[6].op1_out[1]) >> CH[6].FB;
                    CH[6].op1_out[1] = CH[6].op1_out[0];
                    feedback2 = CH[6].op1_out[0] = OP_OUT(SLOT, env_out, feedback1);
                }
                else
                {
                    feedback2 = OP_OUT(SLOT, env_out, 0);
                }
            }
            else
            {
                feedback2 = 0;
                CH[6].op1_out[1] = CH[6].op1_out[0];
                CH[6].op1_out[0] = 0;
            }
            /* SLOT 2 */
            SLOT = &CH[6].SLOT[SLOT2];
            env_out = OPL_CALC_SLOT(SLOT);
            if (env_out < EG_ENT - 1)
            {
                /* PG */
                if (SLOT.vib)
                    SLOT.Cnt += (SLOT.Incr * vib) >> VIB_RATE_SHIFT;
                else
                    SLOT.Cnt += SLOT.Incr;
                /* connection */
                outd[0] += OP_OUT(SLOT, env_out, feedback2) * 2;
            }

            // SD  (17) = mul14[fnum7] + white noise
            // TAM (15) = mul15[fnum8]
            // TOP (18) = fnum6(mul18[fnum8]+whitenoise)
            // HH  (14) = fnum7(mul18[fnum8]+whitenoise) + white noise
            env_sd = OPL_CALC_SLOT(SLOT7_2) + whitenoise;
            env_tam = OPL_CALC_SLOT(SLOT8_1);
            env_top = OPL_CALC_SLOT(SLOT8_2);
            env_hh = OPL_CALC_SLOT(SLOT7_1) + whitenoise;

            /* PG */
            if (SLOT7_1.vib)
                SLOT7_1.Cnt += (SLOT7_1.Incr * vib) >> (VIB_RATE_SHIFT - 1);
            else
                SLOT7_1.Cnt += 2 * SLOT7_1.Incr;
            if (SLOT7_2.vib)
                SLOT7_2.Cnt += (CH[7].fc * vib) >> (VIB_RATE_SHIFT - 3);
            else
                SLOT7_2.Cnt += (CH[7].fc * 8);
            if (SLOT8_1.vib)
                SLOT8_1.Cnt += (SLOT8_1.Incr * vib) >> VIB_RATE_SHIFT;
            else
                SLOT8_1.Cnt += SLOT8_1.Incr;
            if (SLOT8_2.vib)
                SLOT8_2.Cnt += ((CH[8].fc * 3) * vib) >> (VIB_RATE_SHIFT - 4);
            else
                SLOT8_2.Cnt += (CH[8].fc * 48);

            tone8 = OP_OUT(SLOT8_2, whitenoise, 0);

            /* SD */
            if (env_sd < (uint)(EG_ENT - 1))
                outd[0] += OP_OUT(SLOT7_1, env_sd, 0) * 8;
            /* TAM */
            if (env_tam < (uint)(EG_ENT - 1))
                outd[0] += OP_OUT(SLOT8_1, env_tam, 0) * 2;
            /* TOP-CY */
            if (env_top < (uint)(EG_ENT - 1))
                outd[0] += OP_OUT(SLOT7_2, env_top, tone8) * 2;
            /* HH */
            if (env_hh < (uint)(EG_ENT - 1))
                outd[0] += OP_OUT(SLOT7_2, env_hh, tone8) * 2;
        }

        /* return : envelope output */
        uint OPL_CALC_SLOT(OPL_SLOT SLOT)
        {
            /* calcrate envelope generator */
            if ((SLOT.evc += SLOT.evs) >= SLOT.eve)
            {
                switch (SLOT.evm)
                {
                    case ENV_MOD_AR: /* ATTACK . DECAY1 */
                            /* next DR */
                        SLOT.evm = ENV_MOD_DR;
                        SLOT.evc = EG_DST;
                        SLOT.eve = SLOT.SL;
                        SLOT.evs = SLOT.evsd;
                        break;
                    case ENV_MOD_DR: /* DECAY . SL or RR */
                        SLOT.evc = SLOT.SL;
                        SLOT.eve = EG_DED;
                        if (SLOT.eg_typ)
                        {
                            SLOT.evs = 0;
                        }
                        else
                        {
                            SLOT.evm = ENV_MOD_RR;
                            SLOT.evs = SLOT.evsr;
                        }
                        break;
                    case ENV_MOD_RR: /* RR . OFF */
                        SLOT.evc = EG_OFF;
                        SLOT.eve = EG_OFF + 1;
                        SLOT.evs = 0;
                        break;
                }
            }
            /* calcrate envelope */
            return SLOT.TLL + ENV_CURVE[SLOT.evc >> ENV_BITS] + (SLOT.ams ? ams : 0);
        }

        static int[] outd = new int[1];
        static int ams;
        static int vib;

        static int feedback2;
        /* connect for SLOT 2 */

        /* set algorythm connection */
        static void set_algorythm(OPL_CH CH)
        {
            var carrier = new Func<int,int>(index => outd[index]);
            CH.connect1 = CH.CON != 0 ? carrier : new Func<int,int>(index => feedback2 + index);
            CH.connect2 = carrier;
        }


        /* ----- key on  ----- */
        void OPL_KEYON(OPL_SLOT SLOT)
        {
            /* sin wave restart */
            SLOT.Cnt = 0;
            /* set attack */
            SLOT.evm = ENV_MOD_AR;
            SLOT.evs = SLOT.evsa;
            SLOT.evc = EG_AST;
            SLOT.eve = EG_AED;
        }

        /* ----- key off ----- */
        void OPL_KEYOFF(OPL_SLOT SLOT)
        {
            if (SLOT.evm > ENV_MOD_RR)
            {
                /* set envelope counter from envleope output */

                // WORKAROUND: The Kyra engine does something very strange when
                // starting a new song. For each channel:
                //
                // * The release rate is set to "fastest".
                // * Any note is keyed off.
                // * A very low-frequency note is keyed on.
                //
                // Usually, what happens next is that the real notes is keyed
                // on immediately, in which case there's no problem.
                //
                // However, if the note is again keyed off (because the channel
                // begins on a rest rather than a note), the envelope counter
                // was moved from the very lowest point on the attack curve to
                // the very highest point on the release curve.
                //
                // Again, this might not be a problem, if the release rate is
                // still set to "fastest". But in many cases, it had already
                // been increased. And, possibly because of inaccuracies in the
                // envelope generator, that would cause the note to "fade out"
                // for quite a long time.
                //
                // What we really need is a way to find the correct starting
                // point for the envelope counter, and that may be what the
                // commented-out line below is meant to do. For now, simply
                // handle the pathological case.

                if (SLOT.evm == ENV_MOD_AR && SLOT.evc == EG_AST)
                    SLOT.evc = EG_DED;
                else if ((SLOT.evc & EG_DST) == 0)
                        //SLOT.evc = (ENV_CURVE[SLOT.evc>>ENV_BITS]<<ENV_BITS) + EG_DST;
                        SLOT.evc = EG_DST;
                SLOT.eve = EG_DED;
                SLOT.evs = SLOT.evsr;
                SLOT.evm = ENV_MOD_RR;
            }
        }


        /* set ksl & tl */
        void set_ksl_tl(int slot, int v)
        {
            OPL_CH CH = P_CH[slot >> 1];
            OPL_SLOT SLOT = CH.SLOT[slot & 1];
            int ksl = v >> 6; /* 0 / 1.5 / 3 / 6 db/OCT */

            SLOT.ksl = ksl != 0 ? (byte)(3 - ksl) : (byte)31;
            SLOT.TL = (int)((v & 0x3f) * (0.75 / EG_STEP())); /* 0.75db step */

            if ((mode & 0x80) == 0)
            {  /* not CSM latch total level */
                SLOT.TLL = (int)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
            }
        }

        int RATE_0(int index)
        {
            return 0;
        }

        /* set attack rate & decay rate  */
        void set_ar_dr(int slot, int v)
        {
            OPL_CH CH = P_CH[slot >> 1];
            OPL_SLOT SLOT = CH.SLOT[slot & 1];
            int ar = v >> 4;
            int dr = v & 0x0f;

            SLOT.AR = ar != 0 ? new Func<int, int>(index => AR_TABLE[index + ar << 2]) : RATE_0;
            SLOT.evsa = SLOT.AR(SLOT.ksr);
            if (SLOT.evm == ENV_MOD_AR)
                SLOT.evs = SLOT.evsa;

            SLOT.DR = dr != 0 ? new Func<int, int>(index => DR_TABLE[index + dr << 2]) : RATE_0;
            SLOT.evsd = SLOT.DR(SLOT.ksr);
            if (SLOT.evm == ENV_MOD_DR)
                SLOT.evs = SLOT.evsd;
        }

        /* set sustain level & release rate */
        void set_sl_rr(int slot, int v)
        {
            OPL_CH CH = P_CH[slot >> 1];
            OPL_SLOT SLOT = CH.SLOT[slot & 1];
            int sl = v >> 4;
            int rr = v & 0x0f;

            SLOT.SL = SL_TABLE[sl];
            if (SLOT.evm == ENV_MOD_DR)
                SLOT.eve = SLOT.SL;
            SLOT.RR = new Func<int, int>(index => DR_TABLE[index + rr << 2]);
            SLOT.evsr = SLOT.RR(SLOT.ksr);
            if (SLOT.evm == ENV_MOD_RR)
                SLOT.evs = SLOT.evsr;
        }

        void OPL_STATUS_SET(int flag)
        {
            /* set status flag */
            status |= (byte)flag;
            if ((status & 0x80) == 0)
            {
                if ((status & statusmask) != 0)
                {    /* IRQ on */
                    status |= 0x80;
                    /* callback user interrupt handler (IRQ is OFF to ON) */
                    if (IRQHandler != null)
                        IRQHandler(IRQParam, 1);
                }
            }
        }

        void OPL_STATUSMASK_SET(int flag)
        {
            statusmask = (byte)flag;
            /* IRQ handling check */
            OPL_STATUS_SET(0);
            OPL_STATUS_RESET(0);
        }

        /* ---------- Generic interface section ---------- */
        const int OPL_TYPE_YM3526 = 0;
        const int OPL_TYPE_YM3812 = OPL_TYPE_WAVESEL;

        public static FmOPL MakeAdLibOPL(uint rate)
        {
            return new FmOPL(OPL_TYPE_YM3812, 3579545, (int)rate);
        }

        /* lock/unlock for common table */
        bool OPL_LockTable()
        {
            num_lock++;
            if (num_lock > 1)
                return true;
            /* first time */
            cur_chip = null;
            /* allocate total level table (128kb space) */
            if (OPLOpenTable() == 0)
            {
                num_lock--;
                return false;
            }
            return true;
        }

        int TL_MAX()
        {
            return (EG_ENT * 2);
        }
        /* limit(tl + ksr + envelope) + sinwave */

        /* TotalLevel : 48 24 12  6  3 1.5 0.75 (dB) */
        /* TL_TABLE[ 0      to TL_MAX          ] : plus  section */
        /* TL_TABLE[ TL_MAX to TL_MAX+TL_MAX-1 ] : minus section */
        static int[] TL_TABLE;

        /* pointers to TL_TABLE with sinwave output offset */
        static Func<int,int>[] SIN_TABLE;

        /* LFO table */
        static int[] AMS_TABLE;
        static int[] VIB_TABLE;

        /* envelope output curve table */
        /* attack + decay + OFF */
        //static int ENV_CURVE[2*EG_ENT+1];
        //static int ENV_CURVE[2 * 4096 + 1];   // to keep it static ...
        static int[] ENV_CURVE;

        /* ---------- generic table initialize ---------- */
        int OPLOpenTable()
        {
            int s, t;
            double rate;
            int i, j;
            double pom;


            /* allocate dynamic tables */
            TL_TABLE = new int[TL_MAX() * 2];
            SIN_TABLE = new Func<int,int>[SIN_ENT * 4];
            AMS_TABLE = new int[AMS_ENT * 2];
            VIB_TABLE = new int[VIB_ENT * 2];

            /* make total level table */
            for (t = 0; t < EG_ENT - 1; t++)
            {
                rate = ((1 << TL_BITS) - 1) / Math.Pow(10.0, EG_STEP() * t / 20);  /* dB . voltage */
                TL_TABLE[t] = (int)rate;
                TL_TABLE[TL_MAX() + t] = -TL_TABLE[t];
            }
            /* fill volume off area */
            for (t = EG_ENT - 1; t < TL_MAX(); t++)
            {
                TL_TABLE[t] = TL_TABLE[TL_MAX() + t] = 0;
            }

            /* make sinwave table (total level offet) */
            /* degree 0 = degree 180                   = off */
            SIN_TABLE[0] = SIN_TABLE[SIN_ENT / 2] = new Func<int, int>(index => TL_TABLE[index + EG_ENT - 1]);
            for (s = 1; s <= SIN_ENT / 4; s++)
            {
                pom = Math.Sin(2 * Math.PI * s / SIN_ENT); /* sin     */
                pom = 20 * Math.Log10(1 / pom);     /* decibel */
                j = (int)(pom / EG_STEP());         /* TL_TABLE steps */

                /* degree 0   -  90    , degree 180 -  90 : plus section */
                SIN_TABLE[s] = SIN_TABLE[SIN_ENT / 2 - s] = new Func<int, int>(index => TL_TABLE[index + j]);
                /* degree 180 - 270    , degree 360 - 270 : minus section */
                SIN_TABLE[SIN_ENT / 2 + s] = SIN_TABLE[SIN_ENT - s] = new Func<int, int>(index => TL_TABLE[index + TL_MAX() + j]);
            }
            for (s = 0; s < SIN_ENT; s++)
            {
                SIN_TABLE[SIN_ENT * 1 + s] = s < (SIN_ENT / 2) ? SIN_TABLE[s] : new Func<int, int>(index => TL_TABLE[index + EG_ENT]);
                SIN_TABLE[SIN_ENT * 2 + s] = SIN_TABLE[s % (SIN_ENT / 2)];
                SIN_TABLE[SIN_ENT * 3 + s] = (((s / (SIN_ENT / 4)) & 1) != 0) ? new Func<int, int>(index => TL_TABLE[index + EG_ENT]) : SIN_TABLE[SIN_ENT * 2 + s];
            }


            ENV_CURVE = new int[2 * EG_ENT + 1];

            /* envelope counter . envelope output table */
            for (i = 0; i < EG_ENT; i++)
            {
                /* ATTACK curve */
                pom = Math.Pow(((double)(EG_ENT - 1 - i) / EG_ENT), 8) * EG_ENT;
                /* if (pom >= EG_ENT) pom = EG_ENT-1; */
                ENV_CURVE[i] = (int)pom;
                /* DECAY ,RELEASE curve */
                ENV_CURVE[(EG_DST >> ENV_BITS) + i] = i;
            }
            /* off */
            ENV_CURVE[EG_OFF >> ENV_BITS] = EG_ENT - 1;
            /* make LFO ams table */
            for (i = 0; i < AMS_ENT; i++)
            {
                pom = (1.0 + Math.Sin(2 * Math.PI * i / AMS_ENT)) / 2; /* sin */
                AMS_TABLE[i] = (int)((1.0 / EG_STEP()) * pom); /* 1dB   */
                AMS_TABLE[AMS_ENT + i] = (int)((4.8 / EG_STEP()) * pom); /* 4.8dB */
            }
            /* make LFO vibrate table */
            for (i = 0; i < VIB_ENT; i++)
            {
                /* 100cent = 1seminote = 6% ?? */
                pom = (double)VIB_RATE * 0.06 * Math.Sin(2 * Math.PI * i / VIB_ENT); /* +-100sect step */
                VIB_TABLE[i] = (int)(VIB_RATE + (pom * 0.07)); /* +- 7cent */
                VIB_TABLE[VIB_ENT + i] = (int)(VIB_RATE + (pom * 0.14)); /* +-14cent */
            }
            return 1;
        }

        /* -------------------- preliminary define section --------------------- */
        /* attack/decay rate time rate */
        const int OPL_ARRATE = 141280;
        /* RATE 4 =  2826.24ms @ 3.6MHz */
        const int OPL_DRRATE = 1956000;
        /* RATE 4 = 39280.64ms @ 3.6MHz */

        const int FREQ_BITS = 24;
        /* frequency turn          */

        /* counter bits = 20 , octerve 7 */
        const int FREQ_RATE = (1 << (FREQ_BITS - 20));
        const int TL_BITS = (FREQ_BITS + 2);

        /* final output shift , limit minimum and maximum */
        const int OPL_OUTSB = (TL_BITS + 3 - 16);
        /* OPL output final shift 16bit */
        const int OPL_MAXOUT = (0x7fff << OPL_OUTSB);
        const int OPL_MINOUT = (-0x8000 << OPL_OUTSB);

        /* -------------------- quality selection --------------------- */

        /* sinwave entries */
        /* used static memory = SIN_ENT * 4 (byte) */
        const int SIN_ENT_SHIFT = 11;
        const int SIN_ENT = (1 << SIN_ENT_SHIFT);

        /* -------------------- static state --------------------- */

        /* lock level of common table */
        static int num_lock = 0;

        /* work table */
        static FmOPL cur_chip = null;
        /* current chip point */
        /* currenct chip state */
        /* static OPLSAMPLE  *bufL,*bufR; */
        static OPL_CH[] S_CH;
        static OPL_CH E_CH;
        OPL_SLOT SLOT7_1, SLOT7_2, SLOT8_1, SLOT8_2;

        /* output level entries (envelope,sinwave) */
        /* envelope counter lower bits */
        int ENV_BITS;
        /* envelope output entries */
        int EG_ENT;

        /* used dynamic memory = EG_ENT*4*4(byte)or EG_ENT*6*4(byte) */
        /* used static  memory = EG_ENT*4 (byte)                     */
        int EG_OFF;
        /* OFF */
        int EG_DED;
        int EG_DST;
        /* DECAY START */
        int EG_AED;

        /* ATTACK START */
        const int EG_AST = 0;
        /* OPL is 0.1875 dB step  */
        double EG_STEP()
        {
            return (96.0 / EG_ENT);
        }

        /* LFO table entries */
        const int VIB_ENT = 512;
        const int VIB_SHIFT = (32 - 9);
        const int AMS_ENT = 512;
        const int AMS_SHIFT = (32 - 9);

        const int VIB_RATE_SHIFT = 8;
        const int VIB_RATE = (1 << VIB_RATE_SHIFT);

        static readonly double[] KSL_TABLE_SEED =
            {
                /* OCT 0 */
                0.000, 0.000, 0.000, 0.000,
                0.000, 0.000, 0.000, 0.000,
                0.000, 0.000, 0.000, 0.000,
                0.000, 0.000, 0.000, 0.000,
                /* OCT 1 */
                0.000, 0.000, 0.000, 0.000,
                0.000, 0.000, 0.000, 0.000,
                0.000, 0.750, 1.125, 1.500,
                1.875, 2.250, 2.625, 3.000,
                /* OCT 2 */
                0.000, 0.000, 0.000, 0.000,
                0.000, 1.125, 1.875, 2.625,
                3.000, 3.750, 4.125, 4.500,
                4.875, 5.250, 5.625, 6.000,
                /* OCT 3 */
                0.000, 0.000, 0.000, 1.875,
                3.000, 4.125, 4.875, 5.625,
                6.000, 6.750, 7.125, 7.500,
                7.875, 8.250, 8.625, 9.000,
                /* OCT 4 */
                0.000, 0.000, 3.000, 4.875,
                6.000, 7.125, 7.875, 8.625,
                9.000, 9.750, 10.125, 10.500,
                10.875, 11.250, 11.625, 12.000,
                /* OCT 5 */
                0.000, 3.000, 6.000, 7.875,
                9.000, 10.125, 10.875, 11.625,
                12.000, 12.750, 13.125, 13.500,
                13.875, 14.250, 14.625, 15.000,
                /* OCT 6 */
                0.000, 6.000, 9.000, 10.875,
                12.000, 13.125, 13.875, 14.625,
                15.000, 15.750, 16.125, 16.500,
                16.875, 17.250, 17.625, 18.000,
                /* OCT 7 */
                0.000, 9.000, 12.000, 13.875,
                15.000, 16.125, 16.875, 17.625,
                18.000, 18.750, 19.125, 19.500,
                19.875, 20.250, 20.625, 21.000
            };

        /* -------------------- tables --------------------- */
        static readonly int[] slot_array =
            {
                0, 2, 4, 1, 3, 5, -1, -1,
                6, 8, 10, 7, 9, 11, -1, -1,
                12, 14, 16, 13, 15, 17, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1
            };

        static uint[] KSL_TABLE = new uint[8 * 16];

        /* sustain level table (3db per step) */
        /* 0 - 15: 0, 3, 6, 9,12,15,18,21,24,27,30,33,36,39,42,93 (dB)*/

        static int[] SL_TABLE = new int[16];

        static readonly uint[] SL_TABLE_SEED =
            {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 31
            };

        uint SC_KSL(double mydb)
        {
            return ((uint)(mydb / (EG_STEP() / 2)));
        }

        int SC_SL(uint db)
        {
            return (int)(db * ((3 / EG_STEP()) * (1 << ENV_BITS))) + EG_DST;
        }

        void OPLBuildTables(int ENV_BITS_PARAM, int EG_ENT_PARAM)
        {
            ENV_BITS = ENV_BITS_PARAM;
            EG_ENT = EG_ENT_PARAM;
            EG_OFF = ((2 * EG_ENT) << ENV_BITS);  /* OFF          */
            EG_DED = EG_OFF;
            EG_DST = (EG_ENT << ENV_BITS);     /* DECAY  START */
            EG_AED = EG_DST;
            //EG_STEP = (96.0/EG_ENT);

            for (var i = 0; i < KSL_TABLE_SEED.Length; i++)
                KSL_TABLE[i] = SC_KSL(KSL_TABLE_SEED[i]);

            for (var i = 0; i < SL_TABLE_SEED.Length; i++)
                SL_TABLE[i] = SC_SL(SL_TABLE_SEED[i]);
        }
    }
}

