/**
 * @Author: handong.liu
 * @Date: 2020-08-11 18:52:35
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
public struct ColorHSV
{
    public const int kMaxH = 359;
    public const int kMaxS = 255;
    public const int kMaxV = 255;
    public static readonly ColorHSV kZero = new ColorHSV(0, 0, 0);
    public int H;
    public int S;
    public int V;
    public ColorHSV CalculateDelta(ColorHSV baseColor)
    {
        var ret = new ColorHSV();
        ret.H = (H + 720 - baseColor.H) % (kMaxH + 1);
        ret.S = S - baseColor.S;
        ret.V = V - baseColor.V;
        return ret;
    }
    public ColorHSV(int h, int s, int v)
    {
        H = h;
        S = s;
        V = v;
    }
}