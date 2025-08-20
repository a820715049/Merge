/*
 * @Author: yanfuxing
 * @Date: 2025-06-30 10:20:05
 */
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIShopLinkItemCell : MonoBehaviour
    {
        public UIImageRes TopTitleImage;
        public TMP_Text TopTitleText;
        public Button GoBtn;
        public MBRewardIcon RewardIcon;
        private ShopCommunity _data;
        
        void Awake()
        {
            GoBtn.WithClickScale().FixPivot().onClick.AddListener(OnBtnClick);
        }

        public void UpdateContent(ShopCommunity data)
        {
            _data = data;
            TopTitleImage.SetImage(data.Image);
            TopTitleText.text = I18N.Text(data.Title);
            var linkData = Game.Manager.communityLinkMan.GetCommunityLinkDataById(data.Link);
            if (linkData != null)
            {
                var reward = linkData.Reward.ConvertToRewardConfig();
                RewardIcon.Refresh(reward);
            }
        }

        private void OnBtnClick()
        {
            if (_data != null)
            {
                var linkId = _data.Link;
                var communityLinkMan = Game.Manager.communityLinkMan;
                var linkData = communityLinkMan.GetCommunityLinkDataById(linkId);
                if (linkData != null)
                {
                    UIBridgeUtility.OpenURL(linkData.Link);
                    communityLinkMan.RecordClickLinkId = linkId;
                    if (!communityLinkMan.IsHasReward(linkId))
                    {
                        DataTracker.community_link.Track(linkId, (int)CommunityLinkClickType.Shop, false);
                        return;
                    }
                    DataTracker.community_link.Track(linkId, (int)CommunityLinkClickType.Shop, true);
                    //发奖
                    var reward = linkData.Reward.ConvertToRewardConfig();
                    var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.community_reward);
                    communityLinkMan.RewardCommitDataList.Clear();
                    communityLinkMan.RewardCommitDataList.Add(commit);
                    //设置奖励状态
                    communityLinkMan.SetLinkRewardState(linkId, CommunityLinkRewardState.NotReceivedReward);
                    MessageCenter.Get<MSG.COMMUNITY_LINK_REFRESH_RED_DOT>().Dispatch();
                }
            }
        }
    }
}