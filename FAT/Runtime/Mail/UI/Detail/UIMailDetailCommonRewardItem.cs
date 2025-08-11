/*
 * @Author: qun.chao
 * @Date: 2020-12-24 11:56:29
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.gamekitdata;

namespace FAT
{
    public class UIMailDetailCommonRewardItem : UIGenericItemBase<Reward>
    {
        private UIImageRes mImgRes;
        private Transform mTransName;
        private Text mTextCount;

        protected override void InitComponents()
        {
            transform.FindEx("Icon", out mImgRes);
            mTransName = transform.Find("Name");
            transform.FindEx("Num", out mTextCount);
        }

        protected override void UpdateOnDataChange()
        {
            var cfg = Game.Manager.objectMan.GetBasicConfig(mData.Id);

            mImgRes.SetImage(cfg.Icon.ConvertToAssetConfig().Group, cfg.Icon.ConvertToAssetConfig().Asset);
            MBI18NText.SetKey(mTransName.gameObject, cfg.Name);
            mTextCount.text = Game.Manager.rewardMan.GetRewardCountString(mData.Id, mData.Count);
        }

        protected override void UpdateOnDataClear()
        {
            mImgRes.Clear();
        }
    }
}