/*
 * @Author: yanfuxing
 * @Date: 2025-05-27 10:20:05
 */
using EL;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBtnLinkItem : MonoBehaviour
    {
        [SerializeField] private UIImageRes image;
        [SerializeField] private Button btn;
        [SerializeField] private Transform LabelTrans;
        [SerializeField] private MBRewardIcon LabelReward;
        private SettingsCommunity _data;
        private LinkType _linkType;

        private void Start()
        {
            btn.WithClickScale().FixPivot().onClick.AddListener(OnBtnClick);
        }

        private void OnBtnClick()
        {
            if (_data != null)
            {
                var linkId = _data.Link;
                var linkData = fat.conf.Data.GetCommunityLink(linkId);
                if (linkData != null)
                {
                    var communityLinkMan = Game.Manager.communityLinkMan;
                    UIBridgeUtility.OpenURL(linkData.Link);
                    if (_linkType == LinkType.CommunityLink)
                    {
                        communityLinkMan.RecordClickLinkId = linkId;
                        if (!communityLinkMan.IsHasReward(linkId) || string.IsNullOrEmpty(linkData.Reward))
                        {
                            DataTracker.community_link.Track(linkId, (int)CommunityLinkClickType.Setting, false);
                            return;
                        }
                        DataTracker.community_link.Track(linkId, (int)CommunityLinkClickType.Setting, true);
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

        public void UpdateContent(SettingsCommunity data, LinkType linkType)
        {
            _data = data;
            _linkType = linkType;
            image.SetImage(data.Image);
            var communityLinkMan = Game.Manager.communityLinkMan;
            var linkData = communityLinkMan.GetCommunityLinkDataById(data.Link);
            if (linkData != null)
            {
                SetLabelIsShow(data.ShowLable && communityLinkMan.IsHasReward(data.Link) && !string.IsNullOrEmpty(linkData.Reward));
                if (!string.IsNullOrEmpty(linkData.Reward))
                {
                    var reward = linkData.Reward.ConvertToRewardConfig();
                    LabelReward.Refresh(reward);
                }
            }
        }

        public void SetLabelIsShow(bool isShow)
        {
            LabelTrans.gameObject.SetActive(isShow);
        }
    }
}