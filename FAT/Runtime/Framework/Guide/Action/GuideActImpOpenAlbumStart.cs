/*
*@Author:chaoran.zhang
*@Desc:打开集卡活动宣传UI
*@Created Time:2024.02.05 星期一 18:21:56
*/
using System;

namespace FAT
{
    public class GuideActImpOpenAlbumStart:GuideActImpBase
    {
        public override void Play(string[] param)
        {
            UIManager.Instance.OpenWindow(UIConfig.UICardActivityStartNotice);
            mIsWaiting = false;
        }
    }
}