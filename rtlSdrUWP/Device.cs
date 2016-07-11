using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using rtlSdrUWP.enums;
using Windows.Devices.Enumeration;

namespace rtlSdrUWP {

    public class Device : IDisposable {
        UsbWrapper usbWrapper;

        // tuner context
        tuners.ITuner tuner;

        // rtl demod
        uint rtlXtal;

        uint tunXtal;

        int[] fir = FirDefault;

        uint offsFreq;
        uint bandwidth;

        const uint DEF_RTL_XTAL_FREQ = 28800000;
        const uint MIN_RTL_XTAL_FREQ = DEF_RTL_XTAL_FREQ - 1000;
        const uint MAX_RTL_XTAL_FREQ = DEF_RTL_XTAL_FREQ + 1000;
        const ushort EEPROM_ADDR = 0xa0;

        const uint DEFAULT_BUF_NUM = 15;
        const uint DEFAULT_BUF_LENGTH = (16 * 32 * 512);

        // device specific vars

        tuners.R82XX.R82XX_Config r82xxConfig;
        tuners.E4k.E4KState e4kState;

        enum Blocks {
            DEMODB = 0,
            USBB,
            SYSB,
            TUNB,
            ROMB,
            IRB,
            IICB
        };

        enum UsbReg {
            USB_SYSCTL  = 0x2000,
            USB_CTRL    = 0x2010,
            USB_STAT    = 0x2014,
            USB_EPA_CFG = 0x2144,
            USB_EPA_CTL = 0x2148,
            USB_EPA_MAXPKT = 0x2158,
            USB_EPA_MAXPKT_2 = 0x215a,
            USB_EPA_FIFO_CFG = 0x2160,

            SYS_DEMOD_CTL = 0x3000,
            SYS_GPO = 0x3001,
            SYS_GPI = 0x3002,
            SYS_GPOE = 0x3003,
            SYS_GPD = 0x3004,
            SYS_SYSINTE = 0x3005,
            SYS_SYSINTS = 0x3006,
            SYS_GP_CFG0 = 0x3007,
            SYS_GP_CFG1 = 0x3008,
            SYS_SYSINTE_1 = 0x3009,
            SYS_SYSINTS_1 = 0x300a,
            SYS_DEMOD_CTL_1 = 0x300b,
            SYS_IR_SUSPEND = 0x300c
        }

        static int[] FirDefault = {
            -54, -36, -41, -40, -32, -14, 14, 53,
            101, 156, 215, 273, 327, 372, 404, 421
        };

        #region public properties

        public tuners.Tuner TunerType { get; private set; }

        public int Gain { get; private set; }

        public uint Frequency { get; private set; }

        public int FreqCorrection { get; private set; }

        public uint SampleRate { get; private set; }

        public bool DirectSampling { get; private set; }

        public bool OffsetTuning { get { return offsFreq > 0; } }

        #endregion

        #region public methods
        public Device() {
            DirectSampling = false;
            TunerType = tuners.Tuner.UNKNOWN;
            usbWrapper = new UsbWrapper();
        }
        public void Dispose() {
            usbWrapper?.Dispose();
        }
        public async Task<bool> OpenDevice(string id) {
            bool connectedSuccessfully = false;
            connectedSuccessfully = await usbWrapper.ConnectToDevice(id);
            if (!connectedSuccessfully) {
                return false;
            }

            rtlXtal = DEF_RTL_XTAL_FREQ;

            fir = FirDefault;

            // preform dummy write
            int testWrite = await WriteReg(Blocks.USBB, UsbReg.USB_SYSCTL, 0x09, 1);
            if (testWrite < 0) {
                //todo: reset deivce?
                return false;
            }

            await InitBaseband();

            await SetI2cRepeater(true);

            byte reg;
            reg = await I2cReadReg(tuners.R82XX.R820T_I2C_ADDR, tuners.R82XX.R82XX_CHECK_ADDR);
            if (reg == tuners.R82XX.R82XX_CHECK_VAL) {
                // found it
                TunerType = tuners.Tuner.R820T;
                tuner = new tuners.R82XX();
                await FinalizeDevice();
                return true;
            }

            // todo: other tuners
            else {
                throw new NotImplementedException();
            }

            return false;
        }

        public async Task CloseDevice() {
            await DeInitBaseband();
            usbWrapper?.Dispose();
        }

        public static async Task<DeviceInformationCollection> GetDevices() {
            // todo: filter these, grr
            var devices = await UsbWrapper.FindAllDevices();
            return devices;
        }

        public async Task ResetBuffer() {
            await WriteReg(Blocks.USBB, UsbReg.USB_EPA_CTL, 0x1002, 2);
            await WriteReg(Blocks.USBB, UsbReg.USB_EPA_CTL, 0x0000, 2);
        }

        public async Task<int> SetXtalFreq(uint rtlFreq, uint tunerFreq) {
            int r = 0;
            if (rtlFreq < MIN_RTL_XTAL_FREQ || rtlFreq > MAX_RTL_XTAL_FREQ)
                return -2;
            if (rtlFreq != rtlXtal) {
                rtlXtal = rtlFreq;
                if (SampleRate > 0)
                    r |= await SetSampleRate(SampleRate);
            }

            if (tunXtal != tunerFreq) {
                if (tunerFreq == 0)
                    tunXtal = rtlXtal;
                else
                    tunXtal = rtlXtal;

                // read correct clock into e4k and r82xx struct
                // todo:
                uint tmp;
                GetXtalFreq(out tmp, out r82xxConfig.xtal);
                GetXtalFreq(out tmp, out e4kState.vco.fosc);

                if (Frequency > 0)
                    await SetCenterFreq(Frequency);
            }

            return r;
        }

        public async Task<int> SetSampleRate(uint sampRate) {
            ushort tmp;
            uint rsampRatio, realRsampRatio;
            double realRate;

            if ((sampRate <= 225000) || (sampRate > 3200000)
                || ((sampRate > 300000) && (sampRate <= 900000))) {
                throw new NotImplementedException();
                return -1;
            }

            rsampRatio = (uint)((rtlXtal * PowTwo(22)) / sampRate);
            rsampRatio &= 0x0ffffffc;

            realRsampRatio = rsampRatio | ((rsampRatio & 0x08000000) << 1);
            realRate = (rtlXtal * PowTwo(22)) / realRsampRatio;

            if (sampRate != realRate) {
                System.Diagnostics.Debug.WriteLine("Exact sample rate is %f hz", realRate);
            }
            SampleRate = (uint)realRate;

            if (tuner != null) {
                await SetI2cRepeater(true);
                tuner.SetBW(bandwidth > 0 ? bandwidth : SampleRate);
                await SetI2cRepeater(false);
            }

            tmp = (ushort)(rsampRatio >> 16);
            int r = await DemodWriteReg(1, 0x9f, tmp, 2);
            tmp = (ushort)(rsampRatio & 0xffff);
            r |= await DemodWriteReg(1, 0xa1, tmp, 2);

            r |= await SetSampleFreqCorrection(FreqCorrection);

            // reset demod
            r |= await DemodWriteReg(1, 0x1, 0x14, 1);
            r |= await DemodWriteReg(1, 0x1, 0x10, 1);

            // recalc offset if tuning enabled
            if (offsFreq != 0)
                await SetOffsetTuning(true);

            return r;
                
        }

        public async Task<int> SetOffsetTuning(bool on) {
            int r = 0;
            uint bw;

            if (TunerType == tuners.Tuner.R820T 
                || TunerType == tuners.Tuner.R828D) {
                return -2;
            }

            if (DirectSampling) {
                return -3;
            }

            // based on keenerds 1/f noise measurements?
            offsFreq = on ? ((SampleRate / 2) * 170 / 100) : 0;

            r |= await SetIFFreq(offsFreq);

            if (tuner != null) {
                await SetI2cRepeater(true);
                if (on)
                    bw = (2 * offsFreq);
                else if (bandwidth > 0)
                    bw = bandwidth;
                else
                    bw = SampleRate;
                tuner.SetBW(bw);
                await SetI2cRepeater(false);
            }

            if (Frequency > offsFreq)
                r |= await SetCenterFreq(Frequency);

            return r;
        }

        public void GetXtalFreq(out uint rtlFreq, out uint tunerFreq) {
            rtlFreq = (uint)((rtlXtal) * (1.0 + (FreqCorrection) / 1e6));
            tunerFreq = (uint)((tunXtal) * (1.0 + (FreqCorrection) / 1e6));
        }

        public async Task<int> WriteEeprom(byte[] data, byte offset, ushort len) {
            int r = 0;
            byte[] cmd = new byte[2];

            if ((len + offset) > 256)
                return -2;

            for (int i = 0; i < len; ++i) {
                cmd[0] = (byte)(i + offset);
                r = await WriteArray(Blocks.IICB, EEPROM_ADDR, cmd, 1);
                var retBuffer = await ReadArray(Blocks.IICB, EEPROM_ADDR, 1);
                cmd[1] = retBuffer[0];

                if (cmd[1] == data[i])
                    continue;

                cmd[1] = data[i];
                r = await WriteArray(Blocks.IICB, EEPROM_ADDR, cmd, 2);
                if (r != 2)
                    return -3;

                // add delay to prevent fail
                await Task.Delay(5000);
            }
            return 0;
        }

        public async Task<byte[]> ReadEeprom(byte offset, ushort len) {
            int r = 0;
            if ((len + offset) > 256)
                return new byte[] { };

            r = await WriteArray(Blocks.IICB, EEPROM_ADDR, new byte[] { offset }, 1);
            if (r < 0)
                return new byte[] { };

            List<byte> retBuffer = new List<byte>();
            for (int i = 0; i < len; ++i) {
                var tmp = await ReadArray(Blocks.IICB, EEPROM_ADDR, 1);
                if (tmp.Length == 0)
                    return new byte[] { };
                retBuffer.Add(tmp[0]);
            }

            return retBuffer.ToArray();
        }

        public async Task<int> SetCenterFreq(uint freq) {
            int r = -1;
            if (DirectSampling) {
                r = await SetIFFreq(freq);
            }
            else if (tuner != null) {
                await SetI2cRepeater(true);
                r = tuner.SetFreq(freq - offsFreq);
                await SetI2cRepeater(false);
            }

            if (r != 0) {
                Frequency = freq;
            }
            else {
                Frequency = 0;
            }

            return r;
        }

        public async Task<int> SetTunerBandwidth(uint bw) {
            int r = 0;

            if (tuner != null) {
                await SetI2cRepeater(true);
                r = tuner.SetBW(bw > 0 ? bw : SampleRate);
                await SetI2cRepeater(false);
                if (r > 0)
                    return r;
                bandwidth = bw;
            }
            return r;
        }

        public int[] GetTunerGains() {
            int[] e4kGains = { -10, 15, 40, 65, 90, 115, 140, 165, 190, 215, 240, 290, 340, 420 };
            int[] fc0012Gains = { -99, -40, 71, 179, 192 };
            int[] fc0013Gains = { -99, -73, -65, -63, -60, -58, -54, 58, 61, 63, 65, 67, 68, 70, 71, 179, 181, 182,
                       184, 186, 188, 191, 197 };
            int[] fc2580Gains = { 0 /* no gain values */ };
            int[] r82xxGains = { 0, 9, 14, 27, 37, 77, 87, 125, 144, 157, 166, 197, 207, 229, 254, 280, 297, 328,
                     338, 364, 372, 386, 402, 421, 434, 439, 445, 480, 496 };
            int[] unknownGains = { 0 /* no gain values */ };

            switch (TunerType) {
                case tuners.Tuner.E4000:
                    return e4kGains;
                case tuners.Tuner.FC0012:
                    return fc0012Gains;
                case tuners.Tuner.FC0013:
                    return fc0013Gains;
                case tuners.Tuner.FC2580:
                    return fc2580Gains;
                case tuners.Tuner.R820T:
                    return r82xxGains;
                case tuners.Tuner.R828D:
                    return r82xxGains;
                default:
                    return unknownGains; 
            }
        }

        public async Task<int> SetFreqCorrection(int ppm) {
            int r = 0;
            if (FreqCorrection == ppm)
                return -2;

            FreqCorrection = ppm;

            r |= await SetSampleFreqCorrection(ppm);

            uint tmp;
            GetXtalFreq(out tmp, out e4kState.vco.fosc);
            GetXtalFreq(out tmp, out r82xxConfig.xtal);

            if (Frequency > 0)
                r |= await SetCenterFreq(Frequency);

            return r;
        }

        public async Task<int> SetTunerGain(int gain) {
            int r = 0;
            if (tuner == null)
                return -1;

            await SetI2cRepeater(true);
            r = tuner.SetGain(Gain);
            await SetI2cRepeater(false);

            if (r == 0)
                Gain = gain;
            else
                Gain = 0;

            return r;
        }

        public async Task<int> SetTunerIFGain(int stage, int gain) {
            if (tuner == null)
                return -1;
            await SetI2cRepeater(true);
            int r = tuner.SetIfGain(stage, gain);
            await SetI2cRepeater(false);

            return r;
        }

        public async Task<int> SetTunerGainMode(int mode) {
            if (tuner == null)
                return -1;

            await SetI2cRepeater(true);
            int r = tuner.SetGainMode(mode);
            await SetI2cRepeater(false);

            return r;
        }

        public async Task SetTestMode(bool on) {
            await DemodWriteReg(0, 0x19, on ? (ushort)0x03 : (ushort)0x05, 1);
        }

        public async Task SetAgcMode(bool on) {
            await DemodWriteReg(0, 0x19, on ? (ushort)0x25 : (ushort)0x05, 1);
        }

        private async Task<int> SetDirectSampling(bool on) {
            int r = 0;
            if (on) {
                //disable zeroif mode
                await DemodWriteReg(1, 0xb1, 0x1a, 1);

                // disable spec inver
                await DemodWriteReg(1, 0x15, 0x00, 1);

                // only enable in phase adc input
                await DemodWriteReg(0, 0x08, 0x4d, 1);

                // swap i and q adc
                await DemodWriteReg(0, 0x06, 0x80, 1);
                DirectSampling = true;
            }
            else {
                if (TunerType == tuners.Tuner.R820T || TunerType == tuners.Tuner.R828D) {
                    await SetIFFreq(tuners.R82XX.R82XX_IF_FREQ);
                    await DemodWriteReg(1, 0x15, 0x01, 1);
                }
                else {
                    await SetIFFreq(0);
                    await DemodWriteReg(0, 0x08, 0xcd, 1);
                    await DemodWriteReg(1, 0xb1, 0x1b, 1);
                }

                await DemodWriteReg(0, 0x06, 0x80, 1);
                DirectSampling = false;
            }

            r |= await SetCenterFreq(Frequency);
            return r;
        }

        public async Task<byte[]> ReadAsync() {
            // hardcoding default for now
            uint readSize = (16 * 32 * 512);
            return await usbWrapper.BulkReadDefault(readSize);
        }

        #endregion

        #region private methods Tuner
        protected async Task<int> I2cWrite(byte i2c_addr, byte[] buf, byte len) {
            ushort addr = i2c_addr;
            return await WriteArray(Blocks.IICB, addr, buf, len);
        }

        protected async Task<byte[]> I2cRead(byte i2c_addr, byte[] buf, int len) {
            ushort addr = i2c_addr;
            return await ReadArray(Blocks.IICB, addr, (byte)len);
        }

        protected async Task<int> SetIFFreq(uint freq) {
            uint m_rtlXtal;
            uint m_tunFreq;
            GetXtalFreq(out m_rtlXtal, out m_tunFreq);
            int r = 0;

            int ifFreq = (int)(((freq * PowTwo(22)) / m_rtlXtal) * (-1));

            byte tmp = (byte)((ifFreq >> 16) & 0x3f);
            r = await DemodWriteReg(1, 0x19, tmp, 1);
            tmp = (byte)((ifFreq >> 8) & 0xff);
            r |= await DemodWriteReg(1, 0x1a, tmp, 1);
            tmp = (byte)(ifFreq & 0xff);
            r |= await DemodWriteReg(1, 0x1b, tmp, 1);

            return r;
        }

        #endregion

        #region Private methods
        private double PowTwo(int x) => ((1<<(x)));

        private async Task<int> SetSampleFreqCorrection(int ppm) {
            int r = 0;
            byte tmp;
            short offs = (short)(ppm * (-1) * PowTwo(24) / 1000000);

            tmp = (byte)(offs & 0xff);
            r |= await DemodWriteReg(1, 0x3f, tmp, 1);
            tmp = (byte)((offs >> 8) & 0x3f);
            r |= await DemodWriteReg(1, 0x3e, tmp, 1);

            return r;
        }

        private async Task DeInitBaseband() {
            if (tuner != null) {
                await SetI2cRepeater(true);
                tuner.Exit();
                await SetI2cRepeater(false);
            }

            await WriteReg(Blocks.SYSB, UsbReg.SYS_DEMOD_CTL, 0x20, 1);
        }

        private async Task FinalizeDevice() {
            tunXtal = rtlXtal;
            object config = null;
            if (TunerType == tuners.Tuner.R828D || TunerType == tuners.Tuner.R820T) {
                // disable zero-if mode
                await DemodWriteReg(1, 0xb1, 0x1a, 1);

                // only enable in phase adc input
                await DemodWriteReg(0, 0x08, 0x4d, 1);

                // r82xx use 3.57 IF for dvb-t 6 mhz mode and 4.57 for 8mhz mode
                await SetIFFreq(tuners.R82XX.R82XX_IF_FREQ);

                // enable spectrum inversion
                await DemodWriteReg(1, 0x15, 0x01, 1);

                if (TunerType == tuners.Tuner.R828D) {
                    r82xxConfig.i2cAddr = tuners.R82XX.R828D_I2C_ADDR;
                    r82xxConfig.rafaelChip = tuners.R82XX.CHIP.R828D;
                }
                else {
                    r82xxConfig.i2cAddr = tuners.R82XX.R820T_I2C_ADDR;
                    r82xxConfig.rafaelChip = tuners.R82XX.CHIP.R820T;
                }

                uint ignore = 0;
                GetXtalFreq(out ignore, out r82xxConfig.xtal);

                r82xxConfig.maxI2cMsgLen = 8;
                r82xxConfig.usePreDetect = 0;
                config = r82xxConfig as object;
            }
            else if (TunerType == tuners.Tuner.UNKNOWN) {
                await SetDirectSampling(true);
                throw new NotImplementedException();
            }
            
            tuner?.Init(config);

            await SetI2cRepeater(false);
        }

        private async Task<byte[]> ReadArray(Blocks block, ushort addr, byte len) {
            ushort index = (ushort)((byte)block << 8);

            return await usbWrapper.ControlTransferIn(0, addr, index, len);
        }

        private async Task<byte> I2cReadReg(byte i2cAddr, byte reg) {
            ushort addr = (byte)i2cAddr;

            await WriteArray(Blocks.IICB, addr, new byte[] { reg }, 1);
            var data = await ReadArray(Blocks.IICB, addr, 1);
            return data[0];
        }

        private async Task SetI2cRepeater(bool on) {
            await DemodWriteReg(1, 0x01, on ? (ushort)0x18 : (ushort)0x10, 1);
        }

        private async Task InitBaseband() {

            // init usb
            await WriteReg(Blocks.USBB, UsbReg.USB_SYSCTL, 0x09, 1);
            await WriteReg(Blocks.USBB, UsbReg.USB_EPA_MAXPKT, 0x002, 2);
            await WriteReg(Blocks.USBB, UsbReg.USB_EPA_CTL, 0x1002, 2);

            // poweron demod
            await WriteReg(Blocks.SYSB, UsbReg.SYS_DEMOD_CTL_1, 0x22, 1);
            await WriteReg(Blocks.SYSB, UsbReg.SYS_DEMOD_CTL, 0xe8, 1);

            // reset demod
            await DemodWriteReg(1, 0x01, 0x14, 1);
            await DemodWriteReg(1, 0x01, 0x10, 1);

            // disable spectrum inversion and adjacent channel rejection
            await DemodWriteReg(1, 0x15, 0x00, 1);
            await DemodWriteReg(1, 0x16, 0x0000, 2);

            // clear both ddc shift and IF freq registers
            for (int i = 0; i < 6; ++i) {
                await DemodWriteReg(1, (ushort)(0x16 + i), 0x00, 1);
            }

            await SetFir();

            // enable sdr mode, disable dagc
            await DemodWriteReg(0, 0x19, 0x05, 1);

            // init fsm state holding register
            await DemodWriteReg(1, 0x93, 0xf0, 1);
            await DemodWriteReg(1, 0x94, 0x0f, 1);

            // disable agc 
            await DemodWriteReg(1, 0x11, 0x00, 1);

            // disable rf and if agc loop
            await DemodWriteReg(1, 0x04, 0x00, 1);

            // disable pid filter
            await DemodWriteReg(0, 0x61, 0x60, 1);

            // defaulst adc_i/adc_q datapath
            await DemodWriteReg(0, 0x06, 0x80, 1);

            // enable zero-if mode, dc cancellation, iq estimation comp
            await DemodWriteReg(1, 0xb1, 0x1b, 1);

            // disable 4.096 mhz clock output on tp_ck0
            await DemodWriteReg(0, 0x0d, 0x83, 1);
        }

        private async Task<int> SetFir() {
            byte[] firByte = new byte[20];

            for (int i = 0; i < 8; ++i) {
                int val = fir[i];
                if (val < -128 || val > 127)
                    return -1;
                firByte[i] = (byte)val;
            }

            for (int i = 0; i < 8; i += 2) {
                int val0 = fir[8 + i];
                int val1 = fir[8 + i + 1];

                if (val0 < -2048 || val0 > 2047 || val1 < -2048 || val1 > 2047)
                    return -1;
                firByte[8 + i * 3 / 2] = (byte)(val0 >> 4);
                firByte[8 + i * 3 / 2 + 1] = (byte)((byte)(val0 << 4) | (byte)(((val1 >> 8) & 0x0f)));
                firByte[8 + i * 3 / 2 + 2] = (byte)val1;
            }

            for (int i =0; i < firByte.Length; ++i) {
                if (await DemodWriteReg(1, (ushort)(0x1c + i), firByte[i], 1) > 0)
                    return -1;
            }
            return 0;
        }

        private async Task<int> WriteReg(Blocks block, UsbReg addr, ushort val, byte len) {
            int r;
            byte[] data = new byte[2];
            ushort index = (ushort)((byte)block << 8 | 0x10);

            if (len == 1)
                data[0] = (byte)(val & 0xff);
            else
                data[0] = (byte)(val >> 8);

            data[1] = (byte)(val & 0xff);

            r = await usbWrapper.ControlTransferOut(0, (ushort)addr, index, data, len);

            if (r < 0)
                throw new NotImplementedException();

            return r;
        }

        private async Task<ushort> DemodReadReg(byte page, ushort addr, byte len) {

            ushort index = page;
            ushort reg;
            addr = (ushort)((addr << 8) | 0x20);

            var data = await usbWrapper.ControlTransferIn(0, addr, index, len);

            if (data.Length == 0)
                throw new NotImplementedException();
            if (data.Length == 1)
                return data[0];
            reg = (ushort)((data[1] << 8) | data[0]);
            return reg;
        }

        private async Task<int> DemodWriteReg(byte page, ushort addr, ushort val, byte len) {
            int r;
            byte[] data = new byte[2];
            ushort index = (ushort)(0x10 | page);
            addr = (ushort)((addr << 8) | 0x20);

            if (len == 1)
                data[0] = (byte)(val & 0xff);
            else
                data[0] = (byte)(val >> 8);

            data[1] = (byte)(val & 0xff);

            r = await usbWrapper.ControlTransferOut(0, addr, index, data, len);

            if (r < 0) {
                //error
                throw new NotImplementedException();
            }

            await DemodReadReg(0x0a, 0x01, 1);

            return (r == len) ? 0 : -1;
        }

        private async Task<int> WriteArray(Blocks block, ushort addr, byte[] array, byte len){
            int r;
            ushort index = (ushort)(((byte)block << 8) | 0x10);
            r = await usbWrapper.ControlTransferOut(0, addr, index, array, len);
            return r;
        }
    }

    #endregion
}
