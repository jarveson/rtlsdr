using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP {
    public static class Utils {
        public class FIRFilter {
            float[] coefs;
            int offset;
            int center;
            float[] currentSamples;

            public FIRFilter(ref float[] a_coefs) {
                coefs = a_coefs;
                offset = coefs.Length - 1;
                center = coefs.Length / 2;
                currentSamples = new float[offset];
            }

            public void LoadSamples(ref float[] samples) {
                float[] newSamples = new float[samples.Length + offset];
                Array.Copy(currentSamples, newSamples, currentSamples.Length - offset);
                Array.Copy(samples, 0, newSamples, offset, samples.Length);
                currentSamples = newSamples;
            }

            public float Get(int index) {
                float rtn = 0f;
                for (int i = 0; i < coefs.Length; ++i) {
                    rtn += coefs[i] * currentSamples[index + i];
                }
                return rtn;
            }

            public float GetDelayed(int index) {
                return currentSamples[index + center];
            }
        }

        public class ExpAverage {
            float avg = 0f;
            public float Std { get; private set; }
            float Weight;
            bool OptStd;
            public ExpAverage(int weight, bool optStd = false) {
                Weight = weight;
                OptStd = optStd;
                Std = 0f;
            }

            public float Add(float value) {
                avg = (Weight * avg + value) / (Weight + 1);
                if (OptStd) {
                    Std = (Weight * Std + (value - avg) * (value - avg)) / (Weight + 1);
                }
                return avg;
            }
        }

        public class StereoSeperator {
            public struct SeperatedStream {
                public bool found;
                public float[] diff;
            }

            int AVG_COEF = 9999;
            int STD_THRES = 400;
            float[] SIN = new float[8001];
            float[] COS = new float[8001];

            float sin = 0f;
            float cos = 1f;
            ExpAverage iavg = new ExpAverage(9999);
            ExpAverage qavg = new ExpAverage(9999);
            ExpAverage cavg = new ExpAverage(49999, true);

            public StereoSeperator(uint sampleRate, uint pilotFreq) {
                for (int i = 0; i < 8001; ++i) {
                    float freq = (float)((pilotFreq + i / 100 - 40) * 2 * Math.PI / sampleRate);
                    SIN[i] = (float)(Math.Sin(freq));
                    COS[i] = (float)(Math.Cos(freq));
                }
            }

            public SeperatedStream Seperate(ref float[] samples) {
                float[] rtn = new float[samples.Length];
                Array.Copy(samples, rtn, samples.Length);
                for (int i = 0; i < rtn.Length; ++i) {
                    float hdev = iavg.Add(rtn[i] * sin);
                    float vdev = qavg.Add(rtn[i] * cos);
                    rtn[i] *= sin * cos * 2;
                    float corr;
                    if (hdev > 0)
                        corr = Math.Max(-4, Math.Min(4, vdev / hdev));
                    else
                        corr = vdev == 0f ? 0f : (vdev > 0f ? 4f : -4f);

                    int idx = (int)Math.Round((corr + 4) * 1000);

                    float newSin = sin * COS[idx] + cos * SIN[idx];
                    cos = cos * COS[idx] - sin * SIN[idx];
                    sin = newSin;
                    cavg.Add(corr * 10);
                }
                return new SeperatedStream { diff = rtn, found = (cavg.Std < STD_THRES) };
            }
        }

        public class Deemphasizer {
            float val = 0f;
            float alpha = 0f;
            public Deemphasizer(uint sampleRate, uint timeConstant) {
                alpha = (float)(1 / (1 + sampleRate * timeConstant / 1e6));
            }

            public void InPlace(ref float[] samples) {
                for (int i = 0; i < samples.Length; ++i) {
                    val = val + alpha * (samples[i] - val);
                    samples[i] = val;
                }
            }
        }

        public class DownSampler {
            FIRFilter filter;
            int rateMul;

            public DownSampler(uint inRate, uint outRate, ref float[] coefs) {
                filter = new FIRFilter(ref coefs);
                rateMul = (int)(inRate / outRate);
            }

            public float[] DownSamples(ref float[] samples) {
                filter.LoadSamples(ref samples);
                float[] outArr = new float[samples.Length / rateMul];
                for (int i = 0, readFrom = 0; i < outArr.Length; ++i, readFrom += rateMul) {
                    outArr[i] = filter.Get(readFrom);
                }
                return outArr;
            }
        }

        public static float[] GetLowPassFIRCoeffs(uint sampleRate, float halfAmplFreq, uint length) {
            length += (length + 1) % 2;
            float  freq = halfAmplFreq / sampleRate;
            float[] coefs = new float[length];
            uint center = (uint)Math.Floor((double)(length / 2));

            double sum = 0;
            for (int i = 0; i < length; ++i) {
                double val;
                if (i == center)
                    val = 2 * Math.PI * freq;
                else {
                    val = Math.Sin(2 * Math.PI * freq * (i - center)) / (i - center);
                    val *= 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (length - 1));
                }
                sum += val;
                coefs[i] = (float)val;
            }
            for (int i = 0; i < length; ++i) {
                coefs[i] /= (float)sum;
            }
            return coefs;
        }
    }
}
