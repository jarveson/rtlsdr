using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP {
    class DemodulateWBFM {
        public struct DemodulatedSignal {
            public float[] left;
            public float[] right;
            public bool stereo;
            public float signalLevel;
        }

        uint INTER_RATE = 336000;
        uint MAX_F = 75000;
        float FILTER = 75000f * 0.8f;
        uint PILOT_FREQ = 19000;
        uint DEEMPH_TC = 50;

        FMDemodulator demodulator;
        float[] filterCoefs;
        Utils.DownSampler monosampler;
        Utils.DownSampler stereoSampler;
        Utils.StereoSeperator stereoSeperator;
        Utils.Deemphasizer leftDeemph;
        Utils.Deemphasizer rightDeemph;

        public DemodulateWBFM (uint inRate, uint outRate) {
            demodulator = new FMDemodulator(inRate, INTER_RATE, MAX_F, FILTER, 51);
            filterCoefs = Utils.GetLowPassFIRCoeffs(INTER_RATE, 10000, 41);
            monosampler = new Utils.DownSampler(INTER_RATE, outRate, ref filterCoefs);
            stereoSampler = new Utils.DownSampler(INTER_RATE, outRate, ref filterCoefs);
            stereoSeperator = new Utils.StereoSeperator(INTER_RATE, PILOT_FREQ);
            leftDeemph = new Utils.Deemphasizer(outRate, DEEMPH_TC);
            rightDeemph = new Utils.Deemphasizer(outRate, DEEMPH_TC);
        }

        public DemodulatedSignal Demodulate(ref float[] samplesI, ref float[] samplesQ, bool inStereo = true) {
            float[] demodulated = demodulator.DemodulateTuned(ref samplesI, ref samplesQ);
            float[] leftAudio = monosampler.DownSamples(ref demodulated);
            float[] rightAudio = leftAudio;
            bool stereoOut = false;
            if (inStereo) {
                Utils.StereoSeperator.SeperatedStream stereo = stereoSeperator.Seperate(ref demodulated);
                if (stereo.found) {
                    stereoOut = true;
                    float[] diffAudio = stereoSampler.DownSamples(ref stereo.diff);
                    for (int i = 0; i < diffAudio.Length; ++i) {
                        rightAudio[i] -= diffAudio[i];
                        leftAudio[i] += diffAudio[i];
                    }
                }
            }

            leftDeemph.InPlace(ref leftAudio);
            rightDeemph.InPlace(ref rightAudio);

            return new DemodulatedSignal { left = leftAudio, right = rightAudio,
                stereo = stereoOut, signalLevel = demodulator.RelSignalPower };
        }
    }
}
