/*
 * @Author: qun.chao
 * @Date: 2021-07-20 17:40:37
 */
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT
{
    public class UIMailDetailCompRewardItem : UIGenericItemBase<RewardValue>
    {
        private UIImageRes mImgRes;
        private Transform mTransName;
        private TextMeshProUGUI mTextCount;

        protected override void InitComponents()
        {
            transform.FindEx("RewardIcon", out mImgRes);
            transform.FindEx("RewardCount", out mTextCount);
        }

        protected override void UpdateOnDataChange()
        {
            var cfg = Game.Manager.objectMan.GetBasicConfig(mData.id);
            mImgRes.SetImage(cfg.Icon.ConvertToAssetConfig().Group, cfg.Icon.ConvertToAssetConfig().Asset);
            mTextCount.text = Game.Manager.rewardMan.GetRewardCountString(mData.id, mData.count);
        }

        protected override void UpdateOnDataClear()
        {
            mImgRes.Clear();
        }
    }
}