/*
 * @Author: qun.chao
 * @Date: 2021-07-20 17:40:37
 */
using UnityEngine;
using EL;
using TMPro;

namespace FAT
{
    public class UIMailDetailCompRewardItem : UIGenericItemBase<RewardValue>
    {
        private UIImageRes mImgRes;
        private Transform mTransName;
        private TextMeshProUGUI mTextCount;
        private Transform mFinishIcon;

        protected override void InitComponents()
        {
            transform.FindEx("RewardIcon", out mImgRes);
            transform.FindEx("RewardCount", out mTextCount);
            transform.FindEx("Finish", out mFinishIcon);
            MessageCenter.Get<MSG.MAIL_ITEM_REFRESH>().AddListener(OnMailItemRefresh);
        }

        protected override void UpdateOnDataChange()
        {
            var cfg = Game.Manager.objectMan.GetBasicConfig(mData.id);
            mImgRes.SetImage(cfg.Icon.ConvertToAssetConfig().Group, cfg.Icon.ConvertToAssetConfig().Asset);
            mTextCount.text = Game.Manager.rewardMan.GetRewardCountString(mData.id, mData.count);
            IsShowFinishIcon(mData.isClaimed);
        }

        protected override void UpdateOnDataClear()
        {
            mImgRes.Clear();
        }

        public void IsShowFinishIcon(bool isShow)
        {
            if (mFinishIcon != null)
            {
                mFinishIcon.gameObject.SetActive(isShow);
                mTextCount.gameObject.SetActive(!isShow);
            }
        }

        private void OnMailItemRefresh()
        {
            IsShowFinishIcon(true);
        }

        void OnDestroy()
        {
            MessageCenter.Get<MSG.MAIL_ITEM_REFRESH>().RemoveListener(OnMailItemRefresh);
        }
    }
}