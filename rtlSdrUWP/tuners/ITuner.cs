using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP.tuners {
    public enum Tuner {
        UNKNOWN,
        E4000,
        FC0012,
        FC0013,
        FC2580,
        R820T,
        R828D
    };

    public delegate Task<int> I2cWriteFunc(byte i2c_addr, byte[] buf, byte len);

    public interface ITuner { 
        Task<int> Init(object config);
        int Exit();
        Task<int> Write(byte reg, byte[] val, int len);
        Task<int> SetFreq(uint freq);
        Task<int> SetBW(uint bw);
        Task<int> SetGain(int gain);
        Task<int> SetIfGain(int stage, int gain);
        Task<int> SetGainMode(int manual);
    }
}
