using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP.rds {
    class AGC {
        float bandwidth;
        float g;
        float alpha;
        float y2_prime;

        public AGC(float bw) {
            bandwidth = bw;
            alpha = bw;
        }

        public Complex Execute(Complex s) {
            Complex y = s * g;

            Complex temp = (y * Complex.Conjugate(y));
            float y2 = (float)(temp.Real);

            y2_prime = (1f - alpha) * y2_prime + alpha * y2;

            if (y2_prime > (Math.E - 6f)) {
                g *= (float)(Math.Exp(-0.5f * alpha * Math.Log(y2_prime)));
            }
            if (g > Math.E * 6f)
                g = (float)(Math.E * 6f);

            return y;
        }
    }
}
