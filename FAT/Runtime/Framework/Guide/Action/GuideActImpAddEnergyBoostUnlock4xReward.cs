/*
 * @Author: qun.chao
 * @Date: 2024-12-24 15:31:26
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpAddEnergyBoostUnlock4xReward : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            // 理论上应该只有int参数
            // 但是guide表经常被错误转换生成不精确的小数 这里用float读取兼容一下
            float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var objID);
            float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var count);
            var data = Game.Manager.rewardMan.BeginReward((int)objID, (int)count, ReasonString.guide_energy4x);
            UIFlyUtility.FlyReward(data, UIEnergyBoostUnlock4X.rewardFromPos);
            mIsWaiting = false;
        }
    }
}