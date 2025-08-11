/**
 * @Author: handong.liu
 * @Date: 2020-07-27 17:26:22
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
public struct BigNumber
{
    private double mNum1;

    public static BigNumber operator- (BigNumber n1)
    {
        BigNumber ret = new BigNumber();
        ret.mNum1 = -n1.mNum1;
        return ret;
    }
    public static BigNumber operator+ (BigNumber n1, BigNumber n2)
    {
        BigNumber ret = new BigNumber();
        ret.mNum1 = n1.mNum1 + n2.mNum1;
        return ret;
    }
    public static BigNumber operator- (BigNumber n1, BigNumber n2)
    {
        BigNumber ret = new BigNumber();
        ret.mNum1 = n1.mNum1 - n2.mNum1;
        return ret;
    }
    public static BigNumber operator* (BigNumber n1, BigNumber n2)
    {
        BigNumber ret = new BigNumber();
        ret.mNum1 = n1.mNum1 * n2.mNum1;
        return ret;
    }
    public static BigNumber operator/ (BigNumber n1, BigNumber n2)
    {
        BigNumber ret = new BigNumber();
        ret.mNum1 = n1.mNum1 / n2.mNum1;
        return ret;
    }
    public static bool operator<= (BigNumber n1, BigNumber n2)
    {
        return n1.mNum1 <= n2.mNum1;
    }
    public static bool operator>=(BigNumber n1, BigNumber n2)
    {
        return n1.mNum1 >= n2.mNum1;
    }
    public static implicit operator BigNumber(long val)
    {
        BigNumber ret = new BigNumber();
        ret.mNum1 = val;
        return ret;
    }
    public static implicit operator int(BigNumber val)
    {
        return Mathf.CeilToInt((float)val.mNum1);
    }
    public static implicit operator BigNumber(double val)
    {
        BigNumber ret = new BigNumber();
        ret.mNum1 = val;
        return ret;
    }
    public static implicit operator BigNumber(string val)
    {
        BigNumber ret = new BigNumber();
        double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ret.mNum1);
        return ret;
    }
    public static implicit operator double(BigNumber val)
    {
        return val.mNum1;
    }
    public static implicit operator float(BigNumber val)
    {
        return (float)val.mNum1;
    }

    public override string ToString()
    {
        if(mNum1 >= 100)
        {
            // return mNum1.ToString("N0");
            return mNum1.ToString();
        }
        else
        {
            return mNum1.ToString("0.##");
        }
    }

    public string ToSerializedString()
    {
        return mNum1.ToString();
    }

    public string ToString(string format)
    {
        return mNum1.ToString(format);
    }
}