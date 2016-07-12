using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP {
    class FMDemodulator {
        double AMPL_CONV;
        float[] coefs;

        float lI = 0f;
        float lQ = 0f;

        public float RelSignalPower { get; private set; }

        Utils.DownSampler downsamplerI;
        Utils.DownSampler downsamplerQ;


        public FMDemodulator(uint inRate, uint outRate, uint maxFreq, float filterFreq, uint kernelLen) {
            RelSignalPower = 0f;
            AMPL_CONV = outRate / (2 * Math.PI * maxFreq);
            coefs = Utils.GetLowPassFIRCoeffs(inRate, filterFreq, kernelLen);
            downsamplerI = new Utils.DownSampler(inRate, outRate, ref coefs);
            downsamplerQ = new Utils.DownSampler(inRate, outRate, ref coefs);
        }

        public float[] DemodulateTuned(ref float[] samplesI, ref float[] samplesQ) {
            float[] I = downsamplerI.DownSamples(ref samplesI);
            float[] Q = downsamplerQ.DownSamples(ref samplesQ);
            float[] rtn = new float[I.Length];

            float prev = 0f;
            float difSqrSum = 0f;

            for (int i = 0; i < rtn.Length; ++i) {
                float real = lI * I[i] + lQ * Q[i];
                float imag = lI * Q[i] - I[i] * lQ;
                int sgn = 1;
                float circ = 0f;
                float ang = 0;
                float div = 0;

                if (real < 0f) {
                    sgn = -sgn;
                    real = -real;
                    circ = (float)Math.PI;
                }

                if (imag < 0) {
                    sgn = -sgn;
                    imag = -imag;
                    circ = -circ;
                }

                if (real > imag) {
                    div = imag / real;
                }
                else if (real != imag) {
                    ang = (float)(-Math.PI / 2);
                    div = real / imag;
                    sgn = -sgn;
                }
                rtn[i] = (float)(circ + sgn *
                            (ang + div
                            / (0.98419158358617365
                             + div * (0.093485702629671305
                                   + div * 0.19556307900617517))) * AMPL_CONV);
                lI = I[i];
                lQ = Q[i];
                float dif = prev - rtn[i];
                difSqrSum += dif * dif;
                prev = rtn[i];
            }
            RelSignalPower = (float)(1 - Math.Sqrt(difSqrSum / rtn.Length));

            return rtn;
        }
    }
}
