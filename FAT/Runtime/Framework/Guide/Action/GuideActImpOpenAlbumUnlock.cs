/*
*@Author:chaoran.zhang
*@Desc:打卡卡册功能解锁UI
*@Created Time:2024.02.05 星期一 14:02:09
*/
using System;

namespace FAT
{
    public class GuideActImpOpenAlbumUnlock:GuideActImpBase
    {
        public override void Play(string[] param)
        {
            mIsWaiting = true;

            Action callback = () => { mIsWaiting = false;};
            
            UIManager.Instance.OpenWindow(UIConfig.UIGuideCardUnlock, callback);
        }
    }
}