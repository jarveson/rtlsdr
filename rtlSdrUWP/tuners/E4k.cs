using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP.tuners {
    public class E4k {
        public struct PllParams {
            public uint fosc;
            public uint intended_flo;
            public uint flo;
            public ushort x;
            public byte z;
            public byte r;
            public byte r_idx;
            public byte threephase;
        };
        public struct E4KState {
            public byte i2cAddr;
            public PllParams vco;
        }
    }
}
