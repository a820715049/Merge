/*
 * @Author: qun.chao
 * @Date: 2022-12-20 13:28:41
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpSpMergeEntryShow : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            // 通知显示入口
            EL.MessageCenter.Get<MSG.UI_ZODIAC_MERGE_ENTRY_GUIDE>().Dispatch(true);
            mIsWaiting = false;
        }
    }
}