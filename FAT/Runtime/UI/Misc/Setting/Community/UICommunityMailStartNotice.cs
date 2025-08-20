/*
 * @Author: yanfuxing
 * @Date: 2025-07-07 10:20:05
 */
using System;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UICommunityMailStartNotice : UIBase
    {
        public TextProOnACircle Title;
        public TextMeshProUGUI LeftTime;
        public MBRewardIcon RewardIcon;
        public TextMeshProUGUI Desc;
        public Button ConfirmBtn;
        public Button CloseBtn;
        public Transform LabelTrans;
        private Action _whenTick;
        private List<RewardCommitData> _rewardCommitDataList = new();
        private CommunityMailActivity _communityMailActivity;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            _whenTick ??= RefreshCD;
        }

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items.Length > 0 && items[0] is CommunityMailActivity activity)
            {
                _communityMailActivity = activity;
            }
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            RefreshCD();
            RefreshReward();
            ConfirmBtn.onClick.AddListener(ClickConfirm);
            CloseBtn.onClick.AddListener(Close);
            _communityMailActivity?.AddPopupCount();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_whenTick);
            MessageCenter.Get<MSG.APP_ENTER_FOREGROUND_EVENT>().AddListener(RefreshCommunityLinkReward);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_whenTick);
            MessageCenter.Get<MSG.APP_ENTER_FOREGROUND_EVENT>().RemoveListener(RefreshCommunityLinkReward);
        }

        private void RefreshCD()
        {
            var v = _communityMailActivity?.Countdown ?? 0;
            UIUtility.CountDownFormat(LeftTime, v);
            if (v <= 0)
                Close();
        }

        private void RefreshReward()
        {
            var linkId = _communityMailActivity.CommunityLinkConfig.Id;
            var linkData = Game.Manager.communityLinkMan.GetCommunityLinkDataById(linkId);
            LabelTrans.gameObject.SetActive(!string.IsNullOrEmpty(linkData.Reward));
            if (linkData != null && !string.IsNullOrEmpty(linkData.Reward))
            {
                var reward = Game.Manager.communityLinkMan.GetCommunityLinkReward(linkId);
                if (reward != null)
                {
                    RewardIcon.Refresh(reward);
                }
            }
        }

        private void ClickConfirm()
        {
            //跳转外链 打开奖励
            var linkId = _communityMailActivity.CommunityLinkConfig.Id;
            UIBridgeUtility.OpenURL(_communityMailActivity.CommunityLinkConfig.Link);
            if (string.IsNullOrEmpty(_communityMailActivity.CommunityLinkConfig.Reward))
            {
                DataTracker.community_link.Track(linkId, (int)CommunityLinkClickType.Community, false);
                return;
            }
            DataTracker.community_link.Track(linkId, (int)CommunityLinkClickType.Community, true);
            var reward = _communityMailActivity.CommunityLinkConfig.Reward.ConvertToRewardConfig();
            var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.community_reward);
            _rewardCommitDataList.Clear();
            _rewardCommitDataList.Add(commit);
            Game.Manager.communityLinkMan.SetLinkRewardState(linkId, CommunityLinkRewardState.NotReceivedReward);
            Close();
        }

        private void RefreshCommunityLinkReward()
        {
            if (_rewardCommitDataList.Count == 0)
            {
                return;
            }
            var communityLinkRewardData = new CommunityLinkRewardData();
            communityLinkRewardData.CommunityPopupType = CommunityPopupType.CommunityLinkReward;
            communityLinkRewardData.commitRewardList = _rewardCommitDataList;
            communityLinkRewardData.LinkType = LinkType.CommunityMailLink;
            CommunityPlanRewardPop rewardPop = new CommunityPlanRewardPop(CommunityLinkMan.CommunityPlanRewardPopupId, new UIResAlt(UIConfig.UICommunityPlanGiftReward));
            Game.Manager.screenPopup.TryQueue(rewardPop, PopupType.Login, communityLinkRewardData);
        }
    }
}