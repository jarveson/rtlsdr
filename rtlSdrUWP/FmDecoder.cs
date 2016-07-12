using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP {
    class FmDecoder {
        
        private static uint IN_RATE = 1024000;
        private static uint OUT_RATE = 48000;

        private float[] I;
        private float[] Q;

        private float cosine = 1f;
        private float sine = 0f;

        DemodulateWBFM demodulator;

        public FmDecoder() {
            demodulator = new DemodulateWBFM(IN_RATE, OUT_RATE);
        }

        private void ProcessIQSamples(ref byte[] buf) {
            int len = buf.Length / 2;
            I = new float[len];
            Q = new float[len];

            for (int i=0; i < len; ++i) {
                I[i] = (float)(buf[2 * i] / 128 - 0.995);
                Q[i] = (float)(buf[2 * i + 1] / 128 - 0.995);
            }
        }

        private void ShiftFreq(uint freq) {
            double dCos = Math.Cos(2 * Math.PI * freq / IN_RATE);
            double dSin = Math.Sin(2 * Math.PI * freq / IN_RATE);

            float[] oI = new float[I.Length];
            float[] oQ = new float[Q.Length];

            for (int i = 0; i < I.Length; ++i) {
                oI[i] = I[i] * cosine - Q[i] * sine;
                oQ[i] = I[i] * sine + Q[i] * cosine;
                float newSine = (float)(cosine * dSin + sine * dCos);
                cosine = (float)(cosine * dCos - sine * dSin);
                sine = newSine;
            }
            I = oI;
            Q = oQ;
        }

        public DemodulateWBFM.DemodulatedSignal Process(ref byte[] buf, uint freqOffset) {
            ProcessIQSamples(ref buf);
            ShiftFreq(freqOffset);

            return demodulator.Demodulate(ref I, ref Q, true);
        }
    }
}
