using System;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 简易确定性 Value Noise + fBm（分形噪声）。
    /// 纯 C#，不依赖 Unity；同一 seed 永远生成同一张图，不同 seed 生成不同图。
    /// </summary>
    public static class Noise
    {
        public static float Value(int x, int y, int seed)
        {
            long n = (long)x * 374761393L + (long)y * 668265263L + (long)seed * 974634583L;
            n = (n ^ (n >> 13)) * 1274126177L;
            n = n ^ (n >> 16);
            return (n & 0x7fffffff) / (float)0x7fffffff;
        }

        public static float Smooth(float x, float y, int seed)
        {
            var x0 = (int)Math.Floor(x);
            var y0 = (int)Math.Floor(y);
            var tx = x - x0;
            var ty = y - y0;

            // smoothstep 插值，让相邻格子的值连续变化
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            var v00 = Value(x0, y0, seed);
            var v10 = Value(x0 + 1, y0, seed);
            var v01 = Value(x0, y0 + 1, seed);
            var v11 = Value(x0 + 1, y0 + 1, seed);
            return Lerp(Lerp(v00, v10, tx), Lerp(v01, v11, tx), ty);
        }

        public static float Fbm(float x, float y, int seed, int octaves)
        {
            var sum = 0f;
            var amplitude = 1f;
            var frequency = 1f;
            var normalization = 0f;

            for (var i = 0; i < octaves; i++)
            {
                sum += Smooth(x * frequency, y * frequency, seed + i * 101) * amplitude;
                normalization += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return sum / normalization;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
