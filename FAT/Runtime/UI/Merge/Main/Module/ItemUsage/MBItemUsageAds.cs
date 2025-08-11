/*
 * @Author: qun.chao
 * @Date: 2023-10-26 14:35:55
 */
using UnityEngine;

namespace FAT.Merge
{
    public class MBItemUsageAds : MBItemUsageBase
    {
        protected override void OnBtnClick()
        {
            if (!BoardUtility.CanWatchBubbleAds())
                return;
            base.OnBtnClick();

            if (mItem.TryGetItemComponent(out ItemBubbleComponent bubble))
            {
                bubble.OnWatchBubbleAd_Prepare();
                var adid = Game.Manager.configMan.globalConfig.BubbleAdId;
                DataTracker.TrackAdIconClick(adid);
                var item = mItem;
                Game.Manager.adsMan.TryPlayAdsVideo(adid, (_, suc) =>
                {
                    DataTracker.TrackAdReward(adid, 1, item?.tid ?? 0);
                    bubble.OnWatchBubbleAd_Finish(suc);
                });
            }
        }
        
        public override void Refresh()
        {
            base.Refresh();
            DataTracker.TrackAdIconShow(Game.Manager.configMan.globalConfig.BubbleAdId);
        }
    }
}