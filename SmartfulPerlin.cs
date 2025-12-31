using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FTDMapgen_WinForms
{
    public class PerlinNoise
    {
        public int Octaves { get; }
        public int Frequency { get; }
        public int Seed { get; }
        public float Max { get; private set; }
        public float Min { get; private set; }
        public PerlinNoise(int frequency, float amplitude, int octaves, int seed)
        {
            this.Seed = seed;
            this.Octaves = octaves;
            this.Frequency = frequency;
            this.Max = -10f;
            this.Min = 10f;
            this.Octaves = Mathf_Limited.Min(this.Octaves, 6);
            this._noiseFunctions = new PerlinNoise2D[this.Octaves];
            for (int i = 0; i < this.Octaves; i++)
            {
                this._noiseFunctions[i] = new PerlinNoise2D(frequency, amplitude, seed);
                frequency *= 2;
                amplitude /= 2f;
            }
        }

        // Token: 0x0600431E RID: 17182 RVA: 0x0012C884 File Offset: 0x0012AA84
        public void Run2D(float[,] H)
        {
            float num = (float)H.GetLength(0);
            float num2 = (float)H.GetLength(1);
            for (int i = 0; i < this.Octaves; i++)
            {
                double num3 = (double)(num / (float)this._noiseFunctions[i].Frequency);
                double num4 = (double)(num2 / (float)this._noiseFunctions[i].Frequency);
                int num5 = 0;
                while ((float)num5 < num)
                {
                    int num6 = 0;
                    while ((float)num6 < num2)
                    {
                        int num7 = (int)((double)num5 / num3);
                        int xb = num7 + 1;
                        int num8 = (int)((double)num6 / num4);
                        int yb = num8 + 1;
                        double interpolatedPoint = this._noiseFunctions[i].getInterpolatedPoint(num7, xb, num8, yb, (double)num5 / num3 - (double)num7, (double)num6 / num4 - (double)num8);
                        bool flag = i == 0;
                        if (flag)
                        {
                            H[num5, num6] = (float)(interpolatedPoint * (double)this._noiseFunctions[i].Amplitude);
                        }
                        else
                        {
                            H[num5, num6] += (float)(interpolatedPoint * (double)this._noiseFunctions[i].Amplitude);
                        }
                        bool flag2 = i == this.Octaves - 1;
                        if (flag2)
                        {
                            this.Max = Mathf_Limited.Max(this.Max, H[num5, num6]);
                            this.Min = Mathf_Limited.Min(this.Min, H[num5, num6]);
                        }
                        num6++;
                    }
                    num5++;
                }
            }
        }

        public float GetValue(int x, int y, int width, int height)
        {
            float num = 0f;
            for (int i = 0; i < this.Octaves; i++)
            {
                double num2 = (double)((float)width / (float)this._noiseFunctions[i].Frequency);
                double num3 = (double)((float)height / (float)this._noiseFunctions[i].Frequency);
                int num4 = (int)((double)x / num2);
                int xb = num4 + 1;
                int num5 = (int)((double)y / num3);
                int yb = num5 + 1;
                double interpolatedPoint = this._noiseFunctions[i].getInterpolatedPoint(num4, xb, num5, yb, (double)x / num2 - (double)num4, (double)y / num3 - (double)num5);
                num += (float)(interpolatedPoint * (double)this._noiseFunctions[i].Amplitude);
            }
            return num;
        }

        // Token: 0x06004320 RID: 17184 RVA: 0x0012CAC8 File Offset: 0x0012ACC8
        /*public void LogResults()
        {
            AdvLogger.LogInfo("Min:" + this.Min.ToString() + ". Max: " + this.Max.ToString(), LogOptions.OnlyInDeveloperLog);
        }*/

        // Token: 0x06004321 RID: 17185 RVA: 0x0012CB0C File Offset: 0x0012AD0C
        public ulong MemorySize()
        {
            ulong num = 0UL;
            foreach (PerlinNoise2D perlinNoise2D in this._noiseFunctions)
            {
                num += (ulong)((long)(perlinNoise2D.Frequency * perlinNoise2D.Frequency * 4));
            }
            return num;
        }

        // Token: 0x04001E5D RID: 7773
        private readonly PerlinNoise2D[] _noiseFunctions;
    }

    public class PerlinNoise2D
    {
        public float Amplitude { get; }
        public int Frequency { get; }
        public PerlinNoise2D(int freq, float amp, int seed)
        {
            freq = Mathf_Limited.Min(freq, 512);
            Random random = new Random(seed);
            this._noiseValues = new double[freq, freq];
            this.Amplitude = amp;
            this.Frequency = freq;
            for (int i = 0; i < freq; i++)
            {
                for (int j = 0; j < freq; j++)
                {
                    this._noiseValues[i, j] = random.NextDouble();
                }
            }
        }

        public double getInterpolatedPoint(int _xa, int _xb, int _ya, int _yb, double Px, double Py)
        {
            double pa = this.interpolate(this._noiseValues[_xa % this.Frequency, _ya % this.Frequency], this._noiseValues[_xb % this.Frequency, _ya % this.Frequency], Px);
            double pb = this.interpolate(this._noiseValues[_xa % this.Frequency, _yb % this.Frequency], this._noiseValues[_xb % this.Frequency, _yb % this.Frequency], Px);
            return this.interpolate(pa, pb, Py);
        }

        public double interpolate(double Pa, double Pb, double Px)
        {
            double num = Px * 3.1415927410125732;
            double num2 = (double)((1f - Mathf_Limited.Cos((float)num)) * 0.5f);
            return Pa * (1.0 - num2) + Pb * num2;
        }

        private readonly double[,] _noiseValues;
    }

    public static class Mathf_Limited
    {
        public static float Cos(float f)
        {
            return (float)Math.Cos((double)f);
        }

        public static int Min(int a, int b)
        {
            return (a < b) ? a : b;
        }
        public static float Min(float a, float b)
        {
            return (a < b) ? a : b;
        }

        public static float Max(float a, float b)
        {
            return (a > b) ? a : b;
        }
        public static float Round(float f)
        {
            return (float)Math.Round((double)f);
        }
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Mathf_Limited.Clamp01(t);
        }
        public static float Clamp01(float value)
        {
            bool flag = value < 0f;
            float result;
            if (flag)
            {
                result = 0f;
            }
            else
            {
                bool flag2 = value > 1f;
                if (flag2)
                {
                    result = 1f;
                }
                else
                {
                    result = value;
                }
            }
            return result;
        }
    }
}
