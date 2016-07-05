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

    public interface ITuner { 
        int Init();
        int Exit();
        int SetFreq(uint freq);
        int SetBW(uint bw);
        int SetGain(int gain);
        int SetIfGain(int stage, int gain);
        int SetGainMode(int manual);
    }
}
