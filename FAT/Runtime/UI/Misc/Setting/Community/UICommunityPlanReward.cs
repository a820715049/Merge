/*
 * @Author: yanfuxing
 * @Date: 2025-06-30 10:20:05
 */
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UICommunityPlanReward : UIBase
    {
        public RectTransform root;
        public TextMeshProUGUI title;
        public TextMeshProUGUI desc;
        public TextMeshProUGUI claimText;
        public MapButton confirm;
        public GameObject LinkBtnItem;
        public Transform LinkBtnRoot;
        public MBRewardIcon RewardIcon;
        private int _linkId;
        public List<RewardCommitData> _rewardCommitDataList = new();
        private CommunityLinkRewardData _communityLinkRewardData;
        private List<GameObject> _cellList = new();
        private CommunityPopupType _communityPopupType;
        private PoolItemType _mItemType = PoolItemType.SETTING_COMMUNITY_TYPE_ITEM;

        protected override void OnCreate()
        {
            Action CloseRef = ConfirmClick;
            confirm.WithClickScale().WhenClick = CloseRef;
            GameObjectPoolManager.Instance.PreparePool(_mItemType, LinkBtnItem);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                _communityLinkRewardData = (CommunityLinkRewardData)items[0];
                _linkId = _communityLinkRewardData.LinkId;
                _communityPopupType = _communityLinkRewardData.CommunityPopupType;
            }
        }

        protected override void OnPreOpen()
        {
            InitPanel();
        }

        protected override void OnPreClose()
        {
            foreach (var item in _cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(_mItemType, item);
            }
            _cellList.Clear();
        }

        /// <summary>
        /// 初始化面板
        /// </summary>
        private void InitPanel()
        {
            var communityLinkMan = Game.Manager.communityLinkMan;
            desc.text = communityLinkMan.SetDescByType(_communityPopupType);
            title.text = communityLinkMan.SetTitleByType(_communityPopupType);
            claimText.text = communityLinkMan.SetClaimTextByType(_communityPopupType);
            RewardIcon.gameObject.SetActive(communityLinkMan.IsShowReward(_communityPopupType));
            LinkBtnRoot.gameObject.SetActive(communityLinkMan.IsShowAllLinkBtn(_communityPopupType));
            if (communityLinkMan.IsShowReward(_communityPopupType))
            {
                RefreshReward();
            }

            if (communityLinkMan.IsShowAllLinkBtn(_communityPopupType))
            {
                RefreshAllLinkBtn();
            }
            //打开界面时，设置奖励状态
            communityLinkMan.SetLinkRewardState(_linkId, CommunityLinkRewardState.GetHadReceivedReward);
        }

        /// <summary>
        /// 刷新奖励
        /// </summary>
        public void RefreshReward()
        {
            var reward = Game.Manager.communityLinkMan.GetCommunityLinkReward(_linkId);
            if (reward != null)
            {
                RewardIcon.Refresh(reward);
            }
        }

        /// <summary>
        /// 刷新链接按钮
        /// </summary>
        private void RefreshAllLinkBtn()
        {
            var settingList = Game.Manager.communityLinkMan.GetCommunityList();
            foreach (var settingData in settingList)
            {
                var cell = GameObjectPoolManager.Instance.CreateObject(_mItemType, LinkBtnRoot.transform);
                cell.SetActive(true);
                cell.GetComponent<UIBtnLinkItem>().UpdateContent(settingData, LinkType.CommunityLink);
                _cellList.Add(cell);
            }
        }

        /// <summary>
        /// 领取按钮点击
        /// </summary>
        private void ConfirmClick()
        {
            Close();
            var rewardCommitDataList = Game.Manager.communityLinkMan.RewardCommitDataList;
            foreach (var item in rewardCommitDataList)
            {
                UIFlyUtility.FlyReward(item, root.transform.position);
            }
        }
    }
}