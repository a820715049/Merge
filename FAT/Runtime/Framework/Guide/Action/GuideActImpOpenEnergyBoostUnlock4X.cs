/*
 * @Author: qun.chao
 * @Date: 2024-12-24 15:27:50
 */
using System;

namespace FAT
{
    public class GuideActImpOpenEnergyBoostUnlock4X : GuideActImpBase
    {

        public override void Play(string[] param)
        {
            mIsWaiting = true;
            Action callback = () => { mIsWaiting = false; };
            UIManager.Instance.OpenWindowAndCallback(UIConfig.UIEnergyBoostUnlock4X, callback);
        }
    }
}