/**
 * @Author: handong.liu
 * @Date: 2020-11-12 20:20:05
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public static class UnityColorUtility
{
    public static Color ColorFromByte(byte r, byte g, byte b, byte a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    public static ColorHSV RgbToHsv(Color rgb)
    {
        float min, max, tmp, H, S, V;
        float R = rgb.r, G = rgb.g, B = rgb.b;
        tmp = Mathf.Min(R, G);
        min = Mathf.Min(tmp, B);
        tmp = Mathf.Max(R, G);
        max = Mathf.Max(tmp, B);
        // H
        H = 0;
        if (max == min)
        {
            H = 0;
        }
        else if (max == R && G > B)
        {
            H = 60 * (G - B) * 1.0f / (max - min) + 0;
        }
        else if (max == R && G < B)
        {
            H = 60 * (G - B) * 1.0f / (max - min) + 360;
        }
        else if (max == G)
        {
            H = H = 60 * (B - R) * 1.0f / (max - min) + 120;
        }
        else if (max == B)
        {
            H = H = 60 * (R - G) * 1.0f / (max - min) + 240;
        }
        // S
        if (max == 0)
        {
            S = 0;
        }
        else
        {
            S = (max - min) * 1.0f / max;
        }
        // V
        V = max;
        return new ColorHSV((int)H, (int)(S * 255), (int)(V * 255));
    }

    /// <summary>
    /// HSV转换RGB
    /// </summary>
    /// <param name="hsv"></param>
    /// <returns></returns>
    public static Color HsvToRgb(ColorHSV hsv)
    {
        if (hsv.H == 360) hsv.H = 359; // 360为全黑，原因不明
        float R = 0f, G = 0f, B = 0f;
        if (hsv.S == 0)
        {
            return new Color((float)hsv.V / 255f, (float)hsv.V/255f, (float)hsv.V/255f);
        }
        float S = hsv.S * 1.0f / 255, V = hsv.V * 1.0f / 255;
        int H1 = (int)(hsv.H * 1.0f / 60), H = hsv.H;
        float F = H * 1.0f / 60 - H1;
        float P = V * (1.0f - S);
        float Q = V * (1.0f - F * S);
        float T = V * (1.0f - (1.0f - F) * S);
        switch (H1)
        {
            case 0: R = V; G = T; B = P; break;
            case 1: R = Q; G = V; B = P; break;
            case 2: R = P; G = V; B = T; break;
            case 3: R = P; G = Q; B = V; break;
            case 4: R = T; G = P; B = V; break;
            case 5: R = V; G = P; B = Q; break;
        }
        R = R * 255;
        G = G * 255;
        B = B * 255;
        while (R > 255) R -= 255;
        while (R < 0) R += 255;
        while (G > 255) G -= 255;
        while (G < 0) G += 255;
        while (B > 255) B -= 255;
        while (B < 0) B += 255;
        return new Color((float)R / 255f, (float)G/255f, (float)B/255f);
    }
}