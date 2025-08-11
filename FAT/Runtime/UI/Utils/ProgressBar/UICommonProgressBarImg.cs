/*
 * @Author: tang.yan
 * @Description: 使用图片自身fillAmount实现的进度条效果
 * @Date: 2025-02-14 14:02:18
 */
using System;
using UnityEngine;
using UnityEngine.UI;

public class UICommonProgressBarImg : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMPro.TMP_Text tmpInd;
    [Range(0.1f, 10f)] [SerializeField] private float animTime = 0.25f;

    public bool IsWaitNotify => mShouldNotify;
    private Action mOnAnimFinishedHandler;
    private Func<long, long, string> mFormatter = FormatterASlashB;
    private bool mShouldNotify = false;
    private long mMin;
    private long mMax;
    private long mTar;
    private float mCur;
    private float mSpd;
    private long mLastShowedCur = long.MinValue;
    private long mLastShowedMax = long.MaxValue;

    private void Update()
    {
        if (mCur < mTar)
        {
            mCur += mSpd * Time.deltaTime;
            if (mCur >= mTar)
                mCur = mTar;
        }
        else if (mCur > mTar)
        {
            mCur -= mSpd * Time.deltaTime;
            if (mCur <= mTar)
                mCur = mTar;
        }
        else
        {
            _Notify();
        }

        _Refresh(mCur);
    }

    public void RegisterNotify(Action cb)
    {
        mOnAnimFinishedHandler += cb;
    }

    public void UnregisterNotify(Action cb)
    {
        mOnAnimFinishedHandler -= cb;
    }

    public void Setup(long min, long max, long cur)
    {
        mMin = min;
        mMax = max;
        if (cur < min) cur = min;
        if (cur > max) cur = max;
        mCur = cur;
        mTar = cur;
        mShouldNotify = false;
    }

    public void ForceSetup(long min, long max, long cur)
    {
        Setup(min, max, cur);
        _Refresh(mCur);
    }

    public void SetProgress(long tar)
    {
        if (tar < mMin) tar = mMin;
        if (tar > mMax) tar = mMax;
        mTar = tar;
        mSpd = Mathf.Abs(tar - mCur) / animTime;
        mShouldNotify = true;
    }

    public void SetFormatter(Func<long, long, string> fmt)
    {
        if (fmt != null)
            mFormatter = fmt;
        else
            mFormatter = FormatterASlashB;
    }

    public void SetInd(string info)
    {
        if (tmpInd != null)
            tmpInd.text = info;
    }

    public static string FormatterASlashB(long cur, long max)
    {
        return $"{cur}/{max}";
    }

    public static string FormatterPercent(long cur, long max)
    {
        return $"{cur * 100.0f / max:F0}%";
    }

    public static string FormatterPercentFloorToInt(long cur, long max)
    {
        return $"{Mathf.FloorToInt(cur * 100.0f / max)}%";
    }

    private void _Notify()
    {
        if (!mShouldNotify)
            return;
        mShouldNotify = false;
        mOnAnimFinishedHandler?.Invoke();
    }

    private void _Refresh(float cur)
    {
        var _cur = Mathf.CeilToInt(cur);
        if (_cur == mLastShowedCur && mMax == mLastShowedMax)
            return;
        // 显示文字
        _RefreshInd(_cur, mMax);
        // 显示进度条
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.InverseLerp(mMin, mMax, _cur);
        }
    }

    private void _RefreshInd(long cur, long max)
    {
        mLastShowedCur = cur;
        mLastShowedMax = max;
        if (tmpInd != null)
            tmpInd.text = mFormatter(cur, max);
    }
}