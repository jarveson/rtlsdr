using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace rtlSdrUWP.rds {
    class KaiserWindow {

        Complex[] hf;
        Complex[] w; // internal buffer?
        uint w_mask;
        uint w_index;
        uint w_len;
        float scale;

        static uint[] liquid_c_leading_zeros = {
            8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        private static uint MsbIndex(uint x) {
            uint bits;
            uint i, b;
            bits = 8 * sizeof(uint);
            for (i= 8 * sizeof(uint); i > 0; i -=8) {
                b = (uint)(((int)x >> (int)(i - 8)) & 0xff);
                if (b > 0) {
                    return bits - liquid_c_leading_zeros[b];
                }
                else
                    bits -= 8;
            }
            return 0;
        }

        private static float LnGammaF(float z) {
            float g;
            if (z < 0)
                throw new Exception();
            else if (z < 10f) {
                return (float)(LnGammaF(z + 1f) - Math.Log(z));
            }
            else {
                g = (float)(0.5f * (Math.Log(2 * Math.PI) - Math.Log(z)));
                g += (float)(z * (Math.Log(z + (1 /(12f*z-0.1f/z)))-1));
            }
            return g;
        }

        private float Besseli0f(float z) {
            if (z == 0f)
                return 1f;

            uint k;
            float t, y = 0f;
            for (k = 0; k < 32; ++k) {
                t =(float)(k * Math.Log(0.5f * z) - LnGammaF(k + 1f));
                y += (float)(Math.Exp(2 * t));
            }
            return y;
        }

        private static float sincf(float x) {
            if (Math.Abs(x) < 0.01f)
                return (float)(Math.Cos(Math.PI * x / 2f) * Math.Cos(Math.PI * x / 4f) * Math.Cos(Math.PI * x / 8f));
            return (float)(Math.Sin(Math.PI * x) / (Math.PI * x));
        }

        private float Kaiser(uint n, uint N, float beta, float mu) {
            float t = n - (float)(N - 1) / 2 + mu;
            float r = 2f * t / N;
            float a = Besseli0f((float)(beta * Math.Sqrt(1 - r * r)));
            float b = Besseli0f(beta);
            return a / b;
        }

        private float betaAs(float _As) {
            _As = Math.Abs(_As);
            float beta;
            if (_As > 50f)
                beta = 0.1102f * (_As - 8.7f);
            else if (_As > 21.0f)
                beta = (float)(0.5842 * Math.Pow(_As - 21, 0.4f) + 0.07886f * (_As - 21));
            else
                beta = 0.0f;

            return beta;
        }

        private float[] FirDesKaiser(uint n, float fc, float As, float mu) {
            float beta = betaAs(As);
            float[] _h = new float[n];

            float t, h1, h2;
            for (uint i = 0; i < n; ++i) {
                t = i - (float)(n - 1) / 2 + mu;
                h1 = sincf(2.0f * fc * t);

                // kaiser window
                h2 = Kaiser(i, n, beta, mu);

                //printf("t = %f, h1 = %f, h2 = %f\n", t, h1, h2);

                // composite
                _h[i] = h1 * h2;
            }
            return _h;
        }

        public KaiserWindow(uint n, float fc, float As = 60f, float mu = 0f) {
            float[] temp = FirDesKaiser(n, fc, As, mu);
            w_len = (uint)(1 << (int)MsbIndex(n));
            w_mask = w_len - 1;
            w = new Complex[w_len + n + 1];
            w_index = 0;

            uint i;
            hf = new Complex[n];
            for (i = n; i > 0; i--)
                hf[i - 1] = temp[n - i];

            scale = 2f * fc;

        }

        public void Push(Complex x) {
            w_index++;
            w_index &= w_mask;

            if (w_index == 0) 
                Array.Copy(w, (int)w_len, w, 0, hf.Length);

            // append value to end of buffer
            w[w_index + hf.Length - 1] = x;
        }

        private static Complex DotProd4(ref Complex[] h, ref Complex[] x, int n) {
            Complex r = 0;
            uint t = (uint)((n >> 2) << 2);

            uint i;
            for (i = 0; i < t; i += 4) {
                r += h[i] * x[i];
                r += h[i + 1] * x[i + 1];
                r += h[i + 2] * x[i + 2];
                r += h[i + 3] * x[i + 3];
            }

            // clean up remaining 
            for (; i <n; ++i) {
                r += h[i] * x[i];
            }

            return r;
        }

        public Complex Execute() {
            // read buffer
            Complex[] temp = new Complex[w.Length - w_index];
            Array.Copy(w, (int)w_index, temp, 0, (int)(w.Length - w_index));

            // execute dot product
            Complex rtn = DotProd4(ref hf, ref temp, hf.Length);
            return rtn;
        }
    }
}
