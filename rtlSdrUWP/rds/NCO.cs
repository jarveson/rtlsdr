using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP.rds {
    class NCO {
        float theta = 0;
        float d_theta = 0;
        float[] sintab = new float[256];
        uint index = 0;
        float sine = 0;
        float cosine = 1;
        float alpha;
        float beta;


        public NCO(float freq) {
            // init sine table
            for (int i = 0; i < 256; ++i) {
                sintab[i] = (float)(Math.Sin(2f * Math.PI * i / 256f));
            }

            alpha = 0.1f;
            beta = (float)(Math.Sqrt(alpha));
            d_theta = freq;
        }

        public Complex MixDown(Complex s) {
            sine = (float)(Math.Sin(theta));
            cosine = (float)(Math.Cos(theta));

            return s * (cosine - Complex.ImaginaryOne * (sine));
        }

        public void Step() {
            theta += d_theta;
            if (theta > Math.PI)
                theta -= (float)(2 * Math.PI);
            else if (theta < -Math.PI)
                theta += (float)(2 * Math.PI);
        }
    }
}
