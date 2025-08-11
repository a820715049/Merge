/**
 * @Author: handong.liu
 * @Date: 2020-07-13 19:58:34
 */
using UnityEngine;

namespace EL
{
    public static class MathUtility
    {
        public static Quaternion CalculateConjugate(Quaternion q)
        {
            q.x = -q.x;
            q.y = -q.y;
            q.z = -q.z;
            return q;
        }

        public static Quaternion CalculateDifferent(Quaternion from, Quaternion to)
        {
            return CalculateConjugate(from) * to;
        }

        private static Vector3[] sCachedVector = new Vector3[2];
        //transform a AABB by transform
        public static void TransformAABBByMatrix(ref Bounds target, Matrix4x4 mat)
        {
            sCachedVector[0] = target.min;
            sCachedVector[1] = target.max;
            bool first = true;
            Vector3 max = default(Vector3);
            Vector3 min = default(Vector3);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        Vector3 v = mat * new Vector4(sCachedVector[i].x, sCachedVector[j].y, sCachedVector[k].z, 1);
                        if (first)
                        {
                            first = false;
                            max = v;
                            min = v;
                        }
                        else
                        {
                            for (int l = 0; l < 3; l++)
                            {
                                if (max[l] < v[l])
                                {
                                    max[l] = v[l];
                                }
                                if (min[l] > v[l])
                                {
                                    min[l] = v[l];
                                }
                            }
                        }
                    }
                }
            }
            target.SetMinMax(min, max);
        }

        public static int AbsMod(int val, int b)
        {
            var ret = val % b;
            if (ret < 0)
            {
                ret += b;
            }
            return ret;
        }

        public static bool Approximately(double a, double b)
        {
            var dec = a - b;
            if (dec < 0)
            {
                dec = -dec;
            }
            return dec < Mathf.Epsilon;
        }

        public static string FormatPercentage(int per)
        {
            float f = (float)per / 100;
            return string.Format("{0}%", f.ToString("0.##"));
        }

        public static string FormatPercentage(float per)
        {
            return string.Format("{0}%", per.ToString("0.##"));
        }

        public static long ApplyPercentage(long baseVal, int per)
        {
            return baseVal * (10000 + per) / 10000;
        }

        public static int ApplyPercentage(int baseVal, int per)
        {
            return baseVal * (10000 + per) / 10000;
        }

        public static double ApplyPercentage(double baseVal, int per)
        {
            return baseVal + baseVal * per / 10000f;
        }

        public static BigNumber ApplyPercentage(BigNumber baseVal, int per)
        {
            return baseVal + baseVal * per / 10000f;
        }

        public static long ApplyPercentage(long baseVal, float fper)
        {
            return ApplyPercentage(baseVal, Mathf.RoundToInt(fper * 100));
        }

        public static int ApplyPercentage(int baseVal, float fper)
        {
            return ApplyPercentage(baseVal, Mathf.RoundToInt(fper * 100));
        }

        public static BigNumber ApplyPercentage(BigNumber baseVal, float fper)
        {
            return ApplyPercentage(baseVal, Mathf.RoundToInt(fper * 100));
        }

        public static double ApplyPercentage(double baseVal, float fper)
        {
            return ApplyPercentage(baseVal, Mathf.RoundToInt(fper * 100));
        }

        public static int LerpInteger(int start, int end, int factor, int maxFactor)
        {
            return (int)((long)start + ((long)factor * (end - start) + maxFactor - 1) / maxFactor);
        }

        public static long LerpInteger(long start, long end, float factor)
        {
            return start + (long)((double)(end - start) * factor);
        }

        public static bool ThrowDiceProbPercent(int percent0to100)
        {
            return Random.Range(0, 100) < percent0to100;
        }

        public static Rect MinMaxRectEx(float x1, float y1, float x2, float y2)
        {
            return Rect.MinMaxRect(Mathf.Min(x1, x2), Mathf.Min(y1, y2), Mathf.Max(x1, x2), Mathf.Max(y1, y2));
        }

        public static int CalculateGCD(int a, int b)
        {
            if (a < b)
            {
                int temp = a;
                a = b;
                b = temp;
            }
            while (b != 0)
            {
                int r = a % b;
                a = b;
                b = r;
            }
            return a;
        }

        public static long Max(long a, long b)
        {
            return a>b?a:b;
        }

        public static long Min(long a, long b)
        {
            return a>b?b:a;
        }
    }
}