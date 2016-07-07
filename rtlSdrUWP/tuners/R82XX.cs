using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP.tuners {
    public class R82XX : ITuner {
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

        private struct Config {
            public byte i2cAddr;
            public uint xtal;
            public CHIP rafaelChip;
            public uint maxI2cMsgLen;
            public int usePredetect;
        }

        private struct PrivData {
            public Config cfg;

            public byte[] regs;
            public byte[] buf;
            public XTAL_CAP_VALUE xtal_cap_sel;
            public ushort pll;
            public uint intFreq;
            public byte filCalCode;
            public byte input;
            public int initDone;
            public int disableDither;
            public int regCache;
            public int regBatch, regLow, regHigh;

            // current mode
            public uint delsys;
            public TUNER_TYPE type;
            public uint bw;
            public Device dev;
        };

        private PrivData priv;

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

        // static data


        private static byte[] R82XX_INIT_ARRAY = {
            0x83, 0x32, 0x75,
            0xc0, 0x40, 0xd6, 0x6c,
            0xf5, 0x63, 0x75, 0x68,
            0x6c, 0x83, 0x80, 0x00,
            0x0f, 0x00, 0xc0, 0x30,
            0x48, 0xcc, 0x60, 0x00,
            0x54, 0xae, 0x4a, 0xc0
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

        private void ShadowStore(byte reg, byte[] val, int len) {
            int r = reg - REG_SHADOW_START;
            if (r < 0) {
                len += r;
                r = 0;
            }
            if (len <= 0)
                return;
            if (len > NUM_REGS - r)
                len = NUM_REGS - r;

            Array.Copy(val, 0, priv.regs, r, len);
        }

        public int Write(byte reg, byte[] val, int len) {
            int size;
            int pos=0;
            ShadowStore(reg, val, len);
            do {
                if (len > priv.cfg.maxI2cMsgLen - 1)
                    size = (int)priv.cfg.maxI2cMsgLen - 1;
                else
                    size = len;

                priv.buf[0] = reg;
                Array.Copy(val, pos, priv.buf, 1, size);

                // todo: rest of this

            } while (len > 0);
            return 0;
        }

        public int Init() {
            priv.xtal_cap_sel = XTAL_CAP_VALUE.HIGH_CAP_0P;

            return 0;
        }

        public int Exit() {
            return 0;
        }

        public int SetFreq(uint freq) { return 0; }
        public int SetBW(uint bw) {
            return 0;
        }

        public int SetGain(int gain) {
            return 0;
        }

        public int SetIfGain(int stage, int gain) {
            return 0;
        }

        public int SetGainMode(int manual) {
            return 0;
        }
    }
}
