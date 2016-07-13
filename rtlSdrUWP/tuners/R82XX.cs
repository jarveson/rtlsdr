using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP.tuners {
    public class R82XX : Device, ITuner {
#if false
        public struct R82XX_Config {
            public byte i2cAddr;
            public uint xtal;
            public CHIP rafaelChip;
            public uint maxI2cMsgLen;
            public int usePreDetect;
        }

        // const data
        public const byte R820T_I2C_ADDR = 0x34;
        public const byte R828D_I2C_ADDR = 0x74;

        public const uint R828D_XTAL_FREQ = 16000000;

        public const byte R82XX_CHECK_ADDR = 0x0;
        public const byte R82XX_CHECK_VAL = 0x69;

        public const uint R82XX_IF_FREQ = 3570000;

        private const uint R82XX_DEFAULT_IF_FREQ = 6000000;
        private const uint R82XX_DEFAULT_IF_BW = 2000000;

        private const int REG_SHADOW_START = 5;
        private const int NUM_REGS = 30;
        private const uint NUM_IMR = 5;
        private const uint IMR_TRIAL = 9;

        private const uint VER_NUM = 49;

        public enum CHIP {
            R820T,
            R620D,
            R828D,
            R828,
            R828S,
            R820C
        }

        private enum TUNER_TYPE {
            RADIO = 1,
            ANALOG_TV,
            DIGITAL_TV
        }

        private enum XTAL_CAP_VALUE {
            LOW_CAP_30P,
            LOW_CAP_20P,
            LOW_CAP_10P,
            LOW_CAP_0P,
            HIGH_CAP_0P
        }

        private R82XX_Config config;
        private byte[] regs;
        private byte[] buf;
        private XTAL_CAP_VALUE xtal_cap_sel;
        private ushort pll;
        private uint intFreq;
        private byte filCalCode;
        private byte input;
        private int disableDither;
        private int regCache;
        private int regBatch, regLow, regHigh;

        // current mode
        private uint delSys;
        private uint bandwidth;

        private bool hasLock;
        

        private struct FreqRange {
            public uint freq;
            public byte openD;
            public byte rfMuxPloy;
            public byte tfC;
            public byte xtalCap20p;
            public byte xtalCap10p;
            public byte xtalCap0p;
        }

        private enum DELIVERY_SYSTEM {
            UNDEFINED,
            DVBT,
            DVBT2,
            ISDBT
        }

        private static byte[] R82XX_INIT_ARRAY = {
            0x83, 0x32, 0x75,
            0xc0, 0x40, 0xd6, 0x6c,
            0xf5, 0x63, 0x75, 0x68,
            0x6c, 0x83, 0x80, 0x00,
            0x0f, 0x00, 0xc0, 0x30,
            0x48, 0xcc, 0x60, 0x00,
            0x54, 0xae, 0x4a, 0xc0
        };

        private static int[] If_Low_Pass_Bw_Table = {
            1700000, 1600000, 1550000, 1450000, 1200000, 900000, 700000, 550000, 450000, 350000
        };

        private static int[] lna_gain_steps = {
            0, 9, 13, 40, 38, 13, 31, 22, 26, 31, 26, 14, 19, 5, 35, 13
        };

        private static int[] mixer_gain_steps = {
            0, 5, 10, 10, 19, 9, 10, 25, 17, 10, 8, 16, 13, 6, 3, -8
        };


        private static FreqRange[] freqRanges = {
            new FreqRange {freq = 0, openD = 0x08, rfMuxPloy = 0x02, tfC = 0xdf, xtalCap20p = 0x02, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 50, openD = 0x08, rfMuxPloy = 0x02, tfC = 0xbe, xtalCap20p = 0x02, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 55, openD = 0x08, rfMuxPloy = 0x02, tfC = 0x8b, xtalCap20p = 0x02, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 60, openD = 0x08, rfMuxPloy = 0x02, tfC = 0x7b, xtalCap20p = 0x02, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 65, openD = 0x08, rfMuxPloy = 0x02, tfC = 0x69, xtalCap20p = 0x02, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 70, openD = 0x08, rfMuxPloy = 0x02, tfC = 0x58, xtalCap20p = 0x02, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 75, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x44, xtalCap20p = 0x02, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 80, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x44, xtalCap20p = 0x02, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 90, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x34, xtalCap20p = 0x01, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 100, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x34, xtalCap20p = 0x01, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 110, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x24, xtalCap20p = 0x01, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 120, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x24, xtalCap20p = 0x01, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 140, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x14, xtalCap20p = 0x01, xtalCap10p = 0x01, xtalCap0p = 0x00 },
            new FreqRange {freq = 180, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x13, xtalCap20p = 0x00, xtalCap10p = 0x00, xtalCap0p = 0x00 },
            new FreqRange {freq = 220, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x13, xtalCap20p = 0x00, xtalCap10p = 0x00, xtalCap0p = 0x00 },
            new FreqRange {freq = 250, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x11, xtalCap20p = 0x00, xtalCap10p = 0x00, xtalCap0p = 0x00 },
            new FreqRange {freq = 280, openD = 0x00, rfMuxPloy = 0x02, tfC = 0x00, xtalCap20p = 0x00, xtalCap10p = 0x00, xtalCap0p = 0x00 },
            new FreqRange {freq = 310, openD = 0x00, rfMuxPloy = 0x41, tfC = 0x00, xtalCap20p = 0x00, xtalCap10p = 0x00, xtalCap0p = 0x00 },
            new FreqRange {freq = 450, openD = 0x00, rfMuxPloy = 0x41, tfC = 0x00, xtalCap20p = 0x00, xtalCap10p = 0x00, xtalCap0p = 0x00 },
            new FreqRange {freq = 588, openD = 0x00, rfMuxPloy = 0x40, tfC = 0x00, xtalCap20p = 0x00, xtalCap10p = 0x00, xtalCap0p = 0x00 },
            new FreqRange {freq = 650, openD = 0x00, rfMuxPloy = 0x40, tfC = 0x00, xtalCap20p = 0x00, xtalCap10p = 0x00, xtalCap0p = 0x00 },
        };

        private static List<KeyValuePair<byte, XTAL_CAP_VALUE>> xtalCapacitor = new List<KeyValuePair<byte, XTAL_CAP_VALUE>> {
            new KeyValuePair<byte, XTAL_CAP_VALUE>(0x0b, XTAL_CAP_VALUE.LOW_CAP_30P),
            new KeyValuePair<byte, XTAL_CAP_VALUE>(0x02, XTAL_CAP_VALUE.LOW_CAP_20P),
            new KeyValuePair<byte, XTAL_CAP_VALUE>(0x01, XTAL_CAP_VALUE.LOW_CAP_10P),
            new KeyValuePair<byte, XTAL_CAP_VALUE>(0x00, XTAL_CAP_VALUE.LOW_CAP_0P),
            new KeyValuePair<byte, XTAL_CAP_VALUE>(0x10, XTAL_CAP_VALUE.HIGH_CAP_0P),
        };

        private void ShadowStore(byte reg, ref byte[] val, int len) {
            int r = reg - REG_SHADOW_START;
            if (r < 0) {
                len += r;
                r = 0;
            }
            if (len <= 0)
                return;
            if (len > NUM_REGS - r)
                len = NUM_REGS - r;

            Array.Copy(val, 0, regs, r, len);
        }

        private int ReadCacheReg(int reg) {
            reg -= REG_SHADOW_START;

            if (reg >= 0 && reg < NUM_REGS)
                return regs[reg];
            else return -1;
        }

        private async Task<int> WriteRegMask(byte reg, byte val, byte bitMask) {
            int rc = ReadCacheReg(reg);

            if (rc < 0)
                return rc;
            val = (byte)((rc & ~bitMask) | (val & bitMask));

            return await Write(reg, new byte[] { val }, 1);
        }

        private async Task<int> WriteReg(byte reg, byte val) {
            return await Write(reg, new byte[] { val }, 1);
        }

        private async Task<int> SysFreqSel(uint freq, TUNER_TYPE type, uint delsys) {
            int rc;
            byte mixer_top, lna_top, cp_cur, div_buf_cur, lna_vth_l, mixer_vth_l;
            byte air_cable1_in, cable2_in, pre_dect, lna_discharge, filter_cur;

            switch (delsys) {
                case (uint)DELIVERY_SYSTEM.DVBT:
                    if ((freq == 506000000) || (freq == 666000000) ||
                       (freq == 818000000)) {
                        mixer_top = 0x14;   /* mixer top:14 , top-1, low-discharge */
                        lna_top = 0xe5;     /* detect bw 3, lna top:4, predet top:2 */
                        cp_cur = 0x28;      /* 101, 0.2 */
                        div_buf_cur = 0x20; /* 10, 200u */
                    }
                    else {
                        mixer_top = 0x24;   /* mixer top:13 , top-1, low-discharge */
                        lna_top = 0xe5;     /* detect bw 3, lna top:4, predet top:2 */
                        cp_cur = 0x38;      /* 111, auto */
                        div_buf_cur = 0x30; /* 11, 150u */
                    }
                    lna_vth_l = 0x53;       /* lna vth 0.84	,  vtl 0.64 */
                    mixer_vth_l = 0x75;     /* mixer vth 1.04, vtl 0.84 */
                    air_cable1_in = 0x00;
                    cable2_in = 0x00;
                    pre_dect = 0x40;
                    lna_discharge = 14;
                    filter_cur = 0x40;      /* 10, low */
                    break;
                case (uint)DELIVERY_SYSTEM.DVBT2:
                    mixer_top = 0x24;   /* mixer top:13 , top-1, low-discharge */
                    lna_top = 0xe5;     /* detect bw 3, lna top:4, predet top:2 */
                    lna_vth_l = 0x53;   /* lna vth 0.84	,  vtl 0.64 */
                    mixer_vth_l = 0x75; /* mixer vth 1.04, vtl 0.84 */
                    air_cable1_in = 0x00;
                    cable2_in = 0x00;
                    pre_dect = 0x40;
                    lna_discharge = 14;
                    cp_cur = 0x38;      /* 111, auto */
                    div_buf_cur = 0x30; /* 11, 150u */
                    filter_cur = 0x40;  /* 10, low */
                    break;
                case (uint)DELIVERY_SYSTEM.ISDBT:
                    mixer_top = 0x24;   /* mixer top:13 , top-1, low-discharge */
                    lna_top = 0xe5;     /* detect bw 3, lna top:4, predet top:2 */
                    lna_vth_l = 0x75;   /* lna vth 1.04	,  vtl 0.84 */
                    mixer_vth_l = 0x75; /* mixer vth 1.04, vtl 0.84 */
                    air_cable1_in = 0x00;
                    cable2_in = 0x00;
                    pre_dect = 0x40;
                    lna_discharge = 14;
                    cp_cur = 0x38;      /* 111, auto */
                    div_buf_cur = 0x30; /* 11, 150u */
                    filter_cur = 0x40;  /* 10, low */
                    break;
                default: /* DVB-T 8M */
                    mixer_top = 0x24;   /* mixer top:13 , top-1, low-discharge */
                    lna_top = 0xe5;     /* detect bw 3, lna top:4, predet top:2 */
                    lna_vth_l = 0x53;   /* lna vth 0.84	,  vtl 0.64 */
                    mixer_vth_l = 0x75; /* mixer vth 1.04, vtl 0.84 */
                    air_cable1_in = 0x00;
                    cable2_in = 0x00;
                    pre_dect = 0x40;
                    lna_discharge = 14;
                    cp_cur = 0x38;      /* 111, auto */
                    div_buf_cur = 0x30; /* 11, 150u */
                    filter_cur = 0x40;  /* 10, low */
                    break;
            }

            if (config.usePreDetect > 0) {
                rc = await WriteRegMask(0x06, pre_dect, 0x40);
            }

            await WriteRegMask(0x1d, lna_top, 0xc7);
            await WriteRegMask(0x1c, mixer_top, 0xf8);
            await WriteReg(0x0d, lna_vth_l);
            await WriteReg(0x0e, mixer_vth_l);

            input = air_cable1_in;

            // air in only for astrometa
            await WriteRegMask(0x05, air_cable1_in, 0x60);
            await WriteRegMask(0x06, cable2_in, 0x08);
            await WriteRegMask(0x11, cp_cur, 0x38);
            await WriteRegMask(0x17, div_buf_cur, 0x30);

            await WriteRegMask(0x0a, filter_cur, 0x60);

            // set lna

            if (type != TUNER_TYPE.ANALOG_TV) {
                // lna top: lowest
                await WriteRegMask(0x1d, 0, 0x38);
                // 0 : normal mode
                await WriteRegMask(0x1c, 0x0, 0x4);
                // 0 : predect off
                await WriteRegMask(0x06, 0, 0x40);
                // agc clk 250hz
                await WriteRegMask(0x1a, 0x30, 0x30);
                // write lna top =3;
                await WriteRegMask(0x1d, 0x18, 0x38);
                // write dischare mode
                await WriteRegMask(0x1c, mixer_top, 0x04);
                // lna dischare current
                await WriteRegMask(0x1e, lna_discharge, 0x1f);
                // agc clk 60hz
                await WriteRegMask(0x1a, 0x20, 0x30);
            }
            else {
                // predect off
                await WriteRegMask(0x06, 0, 0x40);
                // write lna top
                await WriteRegMask(0x1d, lna_top, 0x38);
                // writre dischare
                await WriteRegMask(0x1c, mixer_top, 0x04);
                // lna dischare current
                await WriteRegMask(0x1e, lna_discharge, 0x1f);
                // agc clk 1khz
                await WriteRegMask(0x1a, 0x00, 0x30);

                await WriteRegMask(0x10, 0x00, 0x04);
            }
            return 0;
        }

        private async Task<int> SetPll(uint freq) {
            int rc, i;
            ulong vcoFreq;
            uint vcoFra;
            uint vcoMin = 1770000;
            uint vcoMax = vcoMin * 2;
            uint freqKhz, pllRef, pllRefKhz;
            uint nSdm = 2;
            uint sdm = 0;
            byte mixDiv = 2;
            byte divBuf = 0;
            byte divNum = 0;
            byte vcoPowerRef = 2;
            byte refdiv2 = 0;
            byte ni, si, nint, vcoFineTune, val;
            byte[] data = new byte[5];

            // freq in khz
            freqKhz = (freq + 500) / 1000;
            pllRef = config.xtal;
            pllRefKhz = (config.xtal + 500) / 1000;

            rc = await WriteRegMask(0x10, refdiv2, 0x10);
            if (rc < 0)
                return rc;

            // set autotune 128khz
            rc = await WriteRegMask(0x1a, 0x00, 0x0c);
            if (rc < 0)
                return rc;

            // set vco current = 100
            rc = await WriteRegMask(0x12, 0x80, 0xe0);
            if (rc < 0)
                return rc;

            // calc divider
            while (mixDiv <= 64) {
                if (((freqKhz * mixDiv) >= vcoMin) && 
                    ((freqKhz * mixDiv) < vcoMax)) {
                    divBuf = mixDiv;
                    while (divBuf > 2) {
                        divBuf = (byte)(divBuf >> 1);
                        divNum++;
                    }
                    break;
                }
                mixDiv = (byte)(mixDiv << 1);
            }

            data = await Read(0x00, data, data.Length);

            if (config.rafaelChip == CHIP.R828D)
                vcoPowerRef = 1;

            vcoFineTune = (byte)((data[4] & 0x30) >> 4);

            if (vcoFineTune > vcoPowerRef)
                divNum -= 1;
            else if (vcoFineTune < vcoPowerRef)
                divNum += 1;

            rc = await WriteRegMask(0x10, (byte)(divNum << 5), 0xe0);
            if (rc < 0)
                return rc;

            vcoFreq = (ulong)(freq) * (ulong)(mixDiv);
            nint = (byte)(vcoFreq / (2 * pllRef));
            vcoFra = (uint)((vcoFreq - 2 * pllRef * nint) / 1000);

            if (nint > ((128 / vcoPowerRef) - 1)) {
                throw new Exception();
            }

            ni = (byte)((nint - 13) / 4);
            si = (byte)(nint - 4 * ni - 13);

            rc = await WriteReg(0x14, (byte)(ni + (byte)(si << 6)));
            if (rc < 0)
                return rc;
            // pw_sdm
            if (vcoFra == 0)
                val = 0x08;
            else val = 0x00;

            rc = await WriteRegMask(0x12, val, 0x08);
            if (rc < 0) {
                return rc;
            }

            // sdm calc
            while (vcoFra < 1) {
                if (vcoFra > (2 * pllRefKhz / nSdm)) {
                    sdm = sdm + 32768 / (nSdm / 2);
                    vcoFra = vcoFra - 2 * pllRefKhz / nSdm;
                    if (nSdm >= 0x8000)
                        break;
                }
                nSdm <<= 1;
            }

            rc = await WriteReg(0x16, (byte)(sdm >> 8));
            if (rc < 0)
                return rc;

            rc = await WriteReg(0x15, (byte)(sdm  & 0xff));
            if (rc < 0)
                return rc;

            for (i = 0; i < 2; ++i) {
                // check if pll has locked
                data = await Read(0x00, data, 3);
                if (data == null)
                    return rc;
                if ((data[2] & 0x40) != 0)
                    break;
                if (i == 0) {
                    // didnt lock, increase vco current
                    rc = await WriteRegMask(0x12, 0x60, 0xe0);
                    if (rc < 0)
                        return rc;
                }
            }

            if ((data[2] & 0x40) == 0) {
                hasLock = false;
                return 0;
            }
            hasLock = true;
            // pll autotune 8khz;
            rc = await WriteRegMask(0x1a, 0x08, 0x08);

            return rc;
        }

        private async Task<int> SetTvStandard(uint bw, TUNER_TYPE type, uint delsys) {
            int rc, i;
            uint ifKhz, filtCalLo;
            byte[] data = new byte[5];
            byte filtGain, imgR, filtQ, hpCor, extEnable, LoopThrough;
            byte ltAtt, fltExtWidest, polyfilCur;
            int needCalib;

            if (delsys == (uint)DELIVERY_SYSTEM.ISDBT) {
                ifKhz = 4063;
                filtCalLo = 59000;
                filtGain = 0x10;   /* +3db, 6mhz on */
                imgR = 0x00;       /* image negative */
                filtQ = 0x10;      /* r10[4]:low q(1'b1) */
                hpCor = 0x6a;      /* 1.7m disable, +2cap, 1.25mhz */
                extEnable = 0x40;  /* r30[6], ext enable; r30[5]:0 ext at lna max */
                LoopThrough = 0x00;    /* r5[7], lt on */
                ltAtt = 0x00;      /* r31[7], lt att enable */
                fltExtWidest = 0x00;  /* r15[7]: flt_ext_wide off */
                polyfilCur = 0x60; /* r25[6:5]:min */
            }
            else {
                if (bw <= 6) {
                    ifKhz = 3570;
                    filtCalLo= 56000;    /* 52000->56000 */
                    filtGain = 0x10;   /* +3db, 6mhz on */
                    imgR = 0x00;       /* image negative */
                    filtQ= 0x10;      /* r10[4]:low q(1'b1) */
                    hpCor = 0x6b;      /* 1.7m disable, +2cap, 1.0mhz */
                    extEnable= 0x60;  /* r30[6]=1 ext enable; r30[5]:1 ext at lna max-1 */
                    LoopThrough= 0x00;    /* r5[7], lt on */
                    ltAtt= 0x00;      /* r31[7], lt att enable */
                    fltExtWidest= 0x00;  /* r15[7]: flt_ext_wide off */
                    polyfilCur= 0x60; /* r25[6:5]:min */
                }
                else if (bw == 7) {
                    /* 7 MHz, second table */
                    ifKhz = 4570;
                    filtCalLo= 63000;
                    filtGain = 0x10;   /* +3db, 6mhz on */
                    imgR = 0x00;       /* image negative */
                    filtQ= 0x10;      /* r10[4]:low q(1'b1) */
                    hpCor= 0x2a;      /* 1.7m disable, +1cap, 1.25mhz */
                    extEnable= 0x60;  /* r30[6]=1 ext enable; r30[5]:1 ext at lna max-1 */
                    LoopThrough= 0x00;    /* r5[7], lt on */
                    ltAtt= 0x00;      /* r31[7], lt att enable */
                    fltExtWidest= 0x00;  /* r15[7]: flt_ext_wide off */
                    polyfilCur= 0x60; /* r25[6:5]:min */
                }
                else {
                    ifKhz = 4570;
                    filtCalLo= 68500;
                    filtGain = 0x10;   /* +3db, 6mhz on */
                    imgR= 0x00;       /* image negative */
                    filtQ= 0x10;      /* r10[4]:low q(1'b1) */
                    hpCor= 0x0b;      /* 1.7m disable, +0cap, 1.0mhz */
                    extEnable= 0x60;  /* r30[6]=1 ext enable; r30[5]:1 ext at lna max-1 */
                    LoopThrough= 0x00;    /* r5[7], lt on */
                    ltAtt= 0x00;      /* r31[7], lt att enable */
                    fltExtWidest= 0x00;  /* r15[7]: flt_ext_wide off */
                    polyfilCur= 0x60; /* r25[6:5]:min */
                }
            }

            // init shadow registers
            Array.Copy(R82XX_INIT_ARRAY, regs, R82XX_INIT_ARRAY.Length);

            // init flag & xtal check result)
            rc = await WriteRegMask(0x0c, 0x00, 0x0f);
            if (rc < 0)
                return rc;

            // version
            rc = await WriteRegMask(0x13, (byte)VER_NUM, 0x3f);

            // lt gain test
            if (type != TUNER_TYPE.ANALOG_TV) {
                rc = await WriteRegMask(0x1d, 0x00, 0x38);
                if (rc < 0)
                    return rc;
            }

            intFreq = ifKhz * 1000;

            // check if standard changed, if so filter calib is needed
            needCalib = 1;
            if (needCalib == 1) {
                for (i=0; i< 2; ++i) {
                    // filtcap
                    rc = await WriteRegMask(0x0b, hpCor, 0x60);
                    if (rc < 0)
                        return rc;
                    // cali clk on
                    rc = await WriteRegMask(0x0f, 0x04, 0x04);
                    if (rc < 0)
                        return rc;
                    // xtal cap
                    rc = await WriteRegMask(0x10, 0x00, 0x03);
                    if (rc < 0)
                        return rc;

                    rc = await SetPll(filtCalLo * 1000);
                    if (rc < 0 || !hasLock)
                        return rc;

                    // start trigger
                    rc = await WriteRegMask(0x0b, 0x10, 0x10);
                    if (rc < 0)
                        return rc;

                    // stop trigger
                    rc = await WriteRegMask(0x0b, 0x00, 0x10);
                    if (rc < 0)
                        return rc;

                    // set cali clk = off
                    rc = await WriteRegMask(0x0f, 0x00, 0x04);
                    if (rc < 0)
                        return rc;

                    // check if calib worked
                    data = await Read(0x00, data, data.Length);
                    if (data == null)
                        return rc;
                    filCalCode = (byte)(data[4] & 0x0f);

                    if (filCalCode > 0 && filCalCode != 0x0f)
                        break;
                }
                // narrowest 
                if (filCalCode == 0x0f)
                    filCalCode = 0;
            }

            rc = await WriteRegMask(0x0a, (byte)(filtQ | filCalCode), 0x1f);
            if (rc < 0)
                return rc;

            // set bw, filterGain and hp conrer
            rc = await WriteRegMask(0x0b, hpCor, 0xef);
            if (rc < 0)
                return rc;

            // set imgr
            rc = await WriteRegMask(0x07, imgR, 0x80);
            // set filt 3db v6mhz
            rc = await WriteRegMask(0x06, filtGain, 0x30);

            // channel filter extenstion
            rc = await WriteRegMask(0x1e, extEnable, 0x60);

            // lop through
            rc = await WriteRegMask(0x05, LoopThrough, 0x80);

            // loop through attenuation
            rc = await WriteRegMask(0x1f, ltAtt, 0x80);

            // filter extension widest
            rc = await WriteRegMask(0x0f, fltExtWidest, 0x80);

            // re poly filter current
            rc = await WriteRegMask(0x19, polyfilCur, 0x60);

            // store current standard, if it changes, recalibrate
            bandwidth = bw;
            delSys = delsys;
            return 0;
        }

        private byte BitRev(byte data) {
            byte[] lut = {
                0x0, 0x8, 0x4, 0xc, 0x2, 0xa, 0x6, 0xe,
                0x1, 0x9, 0x5, 0xd, 0x3, 0xb, 0x7, 0xf
            };

            return (byte)(lut[data & 0xf] << 4 | lut[data >> 4]);
        }

        private async Task<byte[]> Read(byte reg, byte[] val, int len) {
            int rc, i;
            byte[] p = new byte[buf.Length -1];
            Array.Copy(buf, 1, p, 0, buf.Length - 1);

            buf[0] = reg;
            rc = await I2cWrite(config.i2cAddr, buf, 1);
            if (rc < 1)
                throw new Exception();
                //return null;

            p = await I2cRead(config.i2cAddr, p, (byte)len);
            if (p.Length != len) {
                throw new Exception();
                //return null;
            }

            for (i = 0; i < len; ++i) {
                val[i] = BitRev(p[i]);
            }
            return val;
        }

        private async Task<int> SetMux( uint freq) {
            FreqRange range;
            int rc;
            uint i;
            byte val;

            // get proper freq range
            freq = freq / 1000000;

            for (i = 0; i < freqRanges.Length -1; ++i) {
                if ( freq < freqRanges[ i + i].freq) {
                    break;
                }
            }
            range = freqRanges[i];

            // open drain
            await WriteRegMask(0x17, range.openD, 0x08);

            // rf mux, polymux
            await WriteRegMask(0x1a, range.rfMuxPloy, 0xc3);

            // tf band
            await WriteReg(0x1b, range.tfC);


            // xtal cap and drive
            switch (xtal_cap_sel) {
                case XTAL_CAP_VALUE.LOW_CAP_30P:
                    val = (byte)(range.xtalCap20p | 0x08);
                    break;
                case XTAL_CAP_VALUE.LOW_CAP_20P:
                    val = (byte)(range.xtalCap20p | 0x08);
                    break;
                case XTAL_CAP_VALUE.LOW_CAP_10P:
                    val = (byte)(range.xtalCap10p | 0x08);
                    break;
                case XTAL_CAP_VALUE.LOW_CAP_0P:
                    val = (byte)(range.xtalCap0p | 0x08);
                    break;
                default:
                    val = (byte)(range.xtalCap0p | 0x08);
                    break;
            }
            await WriteRegMask(0x10, val, 0x0b);

            await WriteRegMask(0x08, 0x00, 0x3f);
            await WriteRegMask(0x09, 0x00, 0x3f);

            return 0;
        }

        public async Task<int> Write( byte reg, byte[] val, int len) {
            int size;
            int pos=0;
            ShadowStore(reg, ref val, len);
            do {
                if (len > config.maxI2cMsgLen - 1)
                    size = (int)config.maxI2cMsgLen - 1;
                else
                    size = len;

                buf[0] = reg;
                Array.Copy(val, pos, buf, 1, size);
                int rc = await I2cWrite(config.i2cAddr, buf, (byte)(size + 1));

                if (rc != size + 1) {
                    //throw new Exception();
                    if (rc < 0)
                        return rc;
                    return -1;
                }

                reg += (byte)size;
                len -= size;
                pos += size;

            } while (len > 0);
            return 0;
        }

        public async Task<int> Init(object a_config) {
            config = (R82XX_Config)a_config;
            xtal_cap_sel = XTAL_CAP_VALUE.HIGH_CAP_0P;

            int rc = await Write(0x05, R82XX_INIT_ARRAY, R82XX_INIT_ARRAY.Length);

            rc = await SetTvStandard(3, TUNER_TYPE.DIGITAL_TV, 0);

            if (rc < 0)
                return rc;

            rc = await SysFreqSel(0, TUNER_TYPE.DIGITAL_TV, (uint)DELIVERY_SYSTEM.DVBT);

            if (rc < 0)
                throw new Exception();

            return 0;
        }

        public int Exit() {
            return 0;
        }

        public async Task<int> SetFreq(uint freq) {
            int rc = -1;
            uint lo_freq = freq + intFreq;
            byte air_cable1_in;
            await SetMux(lo_freq);
            await SetPll(lo_freq);

            // switch stuff
            air_cable1_in = (freq > (345 * 1000 * 1000)) ? (byte)0x00 : (byte)0x60;

            if ((config.rafaelChip == CHIP.R828D) && 
                    (air_cable1_in != input)) {
                input = air_cable1_in;
                await WriteRegMask(0x05, air_cable1_in, 0x60);
            }
            return 0;

        }
        public async Task<int> SetBW(uint bw) {
            const uint FILT_HP_BW1 = 350000;
            const uint FILT_HP_BW2 = 380000;
            int rc;
            uint i;
            int real_bw = 0;
            byte reg_0a;
            byte reg_0b;

            if (bw > 7000000) {
                // BW: 8 MHz
                reg_0a = 0x10;
                reg_0b = 0x0b;
                intFreq = 4570000;
            }
            else if (bw > 6000000) {
                // BW: 7 MHz
                reg_0a = 0x10;
                reg_0b = 0x2a;
                intFreq = 4570000;
            }
            else if (bw > If_Low_Pass_Bw_Table[0] + FILT_HP_BW1 + FILT_HP_BW2) {
                // BW: 6 MHz
                reg_0a = 0x10;
                reg_0b = 0x6b;
                intFreq = 3570000;
            }
            else {
                reg_0a = 0x00;
                reg_0b = 0x80;
                intFreq = 2300000;

                if (bw > If_Low_Pass_Bw_Table[0] + FILT_HP_BW1) {
                    bw -= FILT_HP_BW2;
                    intFreq += FILT_HP_BW2;
                    real_bw += (int)FILT_HP_BW2;
                }
                else {
                    reg_0b |= 0x20;
                }

                if (bw > If_Low_Pass_Bw_Table[0]) {
                    bw -= FILT_HP_BW1;
                    intFreq += FILT_HP_BW1;
                    real_bw += (int)FILT_HP_BW1;
                }
                else {
                    reg_0b |= 0x40;
                }

                // find low-pass filter
                for (i = 0; i < If_Low_Pass_Bw_Table.Length; ++i) {
                    if (bw > If_Low_Pass_Bw_Table[i])
                        break;
                }
                --i;
                reg_0b |= (byte)(15 - i);
                real_bw += If_Low_Pass_Bw_Table[i];

                intFreq -= (uint)(real_bw / 2);
            }

            rc = await WriteRegMask(0x0a, reg_0a, 0x10);
            if (rc < 0)
                return rc;

            rc = await WriteRegMask(0x0b, reg_0b, 0xef);
            if (rc < 0)
                return rc;

            
            if (intFreq < 0) {
                throw new Exception();
            }

            await SetIFFreq(intFreq);

            return await SetCenterFreq(Frequency);

        }

        private async Task<int> SetGain( int setManualGain, int gain) {
            int rc;

            if (setManualGain > 0) {
                int i, total_gain = 0;
                byte mix_index = 0, lna_index = 0;
                byte[] data = new byte[4];

                /* LNA auto off */
                rc = await WriteRegMask(0x05, 0x10, 0x10);
                if (rc < 0)
                    return rc;

                /* Mixer auto off */
                rc = await WriteRegMask(0x07, 0, 0x10);
                if (rc < 0)
                    return rc;

                data = await Read(0x00, data, data.Length);
                if (data == null)
                    return rc;

                /* set fixed VGA gain for now (16.3 dB) */
                rc = await WriteRegMask(0x0c, 0x08, 0x9f);
                if (rc < 0)
                    return rc;

                for (i = 0; i < 15; i++) {
                    if (total_gain >= gain)
                        break;

                    total_gain += lna_gain_steps[++lna_index];

                    if (total_gain >= gain)
                        break;

                    total_gain += mixer_gain_steps[++mix_index];
                }

                /* set LNA gain */
                rc = await WriteRegMask(0x05, lna_index, 0x0f);
                if (rc < 0)
                    return rc;

                /* set Mixer gain */
                rc = await WriteRegMask(0x07, mix_index, 0x0f);
                if (rc < 0)
                    return rc;
            }
            else {
                /* LNA */
                rc = await WriteRegMask(0x05, 0, 0x10);
                if (rc < 0)
                    return rc;

                /* Mixer */
                rc = await WriteRegMask(0x07, 0x10, 0x10);
                if (rc < 0)
                    return rc;

                /* set fixed VGA gain for now (26.5 dB) */
                rc = await WriteRegMask(0x0c, 0x0b, 0x9f);
                if (rc < 0)
                    return rc;
            }

            return rc;
        }

        public async Task<int> SetGain(int gain) {
            return await SetGain(1, gain);
        }

        public async Task<int> SetIfGain(int stage, int gain) {
            return 0;
        }

        public async Task<int> SetGainMode(int manual) {
            return await SetGain(manual, 0);
        }
#endif
    }
}
