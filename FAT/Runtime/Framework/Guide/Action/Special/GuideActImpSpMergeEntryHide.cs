/*
 * @Author: qun.chao
 * @Date: 2022-12-20 13:26:11
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpSpMergeEntryHide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            // // 标记接下来由guide打开面板
            // UIZodiacEventUtility.GuideOpenZodiac();
            // // 通知隐藏入口
            // EL.MessageCenter.Get<MSG.UI_ZODIAC_MERGE_ENTRY_GUIDE>().Dispatch(false);
            mIsWaiting = false;
        }
    }
}