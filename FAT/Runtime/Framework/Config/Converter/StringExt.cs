/**
 * @Author: handong.liu
 * @Date: 2021-03-25 20:08:13
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using Config;
using Config.Converter;

public static class StringConverterExt
{
    private static AssetConverter mConverter = new AssetConverter();
    public static AssetConfig ConvertToAssetConfig(this string text)
    {
        return mConverter.Get(text);
    }

    private static StyleAddConfigConverter mStyleAddConfigConverter = new StyleAddConfigConverter();
    public static StyleAddConfig ConvertToStyleAddCconfig(this string text)
    {
        return mStyleAddConfigConverter.Get(text);
    }

    private static ColorConfigConverter mColorConfigConverter = new ColorConfigConverter();
    public static ColorConfig ConvertToColorConfig(this string text)
    {
        return mColorConfigConverter.Get(text);
    }

    private static RewardConfigConverter mRewardConfigConverter = new RewardConfigConverter();
    public static RewardConfig ConvertToRewardConfig(this string text)
    {
        return mRewardConfigConverter.Get(text);
    }
    public static RewardConfig ConvertToRewardConfigIfValid(this string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var ret = mRewardConfigConverter.Get(text);
        return ret.Id > 0 ? ret : null;
    }

    private static MergeGridItemConverter mMergeGridItemConverter = new MergeGridItemConverter();
    public static MergeGridItem ConvertToMergeGridItem(this string text)
    {
        return mMergeGridItemConverter.Get(text);
    }

    private static GuideMergeRequireConverter mGuideMergeRequireConverter = new GuideMergeRequireConverter();
    public static GuideMergeRequire ConvertToGuideMergeRequire(this string text)
    {
        return mGuideMergeRequireConverter.Get(text);
    }

    private static RandomBoxShowRewardConverter mRandomBoxShowRewardConverter = new RandomBoxShowRewardConverter();
    public static RandomBoxShowReward ConvertToRandomBoxShowReward(this string text)
    {
        return mRandomBoxShowRewardConverter.Get(text);
    }

    private static CommonInt3Converter mCommonInt3Converter = new();
    public static (int, int, int) ConvertToInt3(this string text)
    {
        return mCommonInt3Converter.Get(text);
    }

    public static int ConvertToInt(this string text)
    {
        if (!string.IsNullOrEmpty(text) && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var r))
        {
            return (int)r;
        }
        else
        {
            return 0;
        }
    }


    public static fat.rawdata.GuideMergeRequireType ConvertToEnumGuideMergeRequireType(this string text)
    {
        if (!string.IsNullOrEmpty(text) && System.Enum.TryParse<fat.rawdata.GuideMergeRequireType>(text, out var r))
        {
            return r;
        }
        else
        {
            return fat.rawdata.GuideMergeRequireType.BoardItem;          //that should never happen
        }
    }
    
    //坐标类配置转换 为策划方便 先解析行 再解析列 如：(3:5) 拆分成 col = 5, row = 3  
    private static CoordConverter mCoordConverter = new ();
    public static CoordConfig ConvertToCoord(this string text)
    {
        return mCoordConverter.Get(text);
    }

    private static RoundsArrayConfigConverter _roundsItemConverter = new RoundsArrayConfigConverter();
    public static RoundsArrayConfig ConvertToRoundsArrayItem(this string text)
    {
        return _roundsItemConverter.Get(text);
    }
    
    private static IntRangeConverter mIntRangeConverter = new ();
    public static IntRangeConfig ConvertToIntRange(this string text)
    {
        return mIntRangeConverter.Get(text);
    }
}