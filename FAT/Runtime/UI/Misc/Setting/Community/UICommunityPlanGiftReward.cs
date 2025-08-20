/*
 * @Author: yanfuxing
 * @Date: 2025-07-03 10:20:05
 */
using System;
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using CDKeyReward = fat.gamekitdata.Reward;

namespace FAT
{
    public class UICommunityPlanGiftReward : UIBase
    {
        public RectTransform root;
        public TextMeshProUGUI title;
        public TextMeshProUGUI desc;
        public TextMeshProUGUI claimText;
        public MapButton confirm;
        public GameObject GiftRewardItem;
        public GameObject GiftRewardRoot;
        public GameObject LinkBtnItem;
        public Transform LinkBtnRoot;
        private string _giftCode;
        private LinkType _linkType;
        private List<GameObject> _linkCellList = new();
        private List<GameObject> _rewardCellList = new();
        private CommunityPopupType _communityPopupType;
        private CommunityLinkRewardData _communityLinkRewardData;
        private List<RewardCommitData> _commitRewardList = new();
        private List<CDKeyReward> _CDKeyRewardList = new();
        private PoolItemType _mLinkItemType = PoolItemType.SETTING_COMMUNITY_TYPE_ITEM;
        private PoolItemType _mRewardItemType = PoolItemType.COMMUNITY_PLAN_GIFT_REWARD_ITEM;

        protected override void OnCreate()
        {
            Action CloseRef = ConfirmClick;
            confirm.WithClickScale().WhenClick = CloseRef;
            GameObjectPoolManager.Instance.PreparePool(_mLinkItemType, LinkBtnItem);
            GameObjectPoolManager.Instance.PreparePool(_mRewardItemType, GiftRewardItem);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                _communityLinkRewardData = (CommunityLinkRewardData)items[0];
                _giftCode = _communityLinkRewardData.GiftCode;
                _communityPopupType = _communityLinkRewardData.CommunityPopupType;
                _commitRewardList = _communityLinkRewardData.commitRewardList;
                _CDKeyRewardList = _communityLinkRewardData.CDKeyRewardList;
                _linkType = _communityLinkRewardData.LinkType;
            }
        }

        protected override void OnPreOpen()
        {
            InitPanel();
        }

        protected override void OnPreClose()
        {
            Clear();
        }

        private void InitPanel()
        {
            var communityLinkMan = Game.Manager.communityLinkMan;
            desc.text = communityLinkMan.SetDescByType(_communityPopupType);
            title.text = communityLinkMan.SetTitleByType(_communityPopupType);
            claimText.text = communityLinkMan.SetClaimTextByType(_communityPopupType);
            GiftRewardRoot.gameObject.SetActive(communityLinkMan.IsShowReward(_communityPopupType));
            LinkBtnRoot.gameObject.SetActive(communityLinkMan.IsShowAllLinkBtn(_communityPopupType));
            if (communityLinkMan.IsShowReward(_communityPopupType))
            {
                DebugEx.Info($"UICommunityPlanGiftReward _communityPopupType: {_communityPopupType}");
                if (_communityPopupType == CommunityPopupType.LinkHadGetReward)
                {
                    RefreshHadGetReward();
                }
                else
                {
                    RefreshAllRewardBtn();
                }
            }
            if (communityLinkMan.IsShowAllLinkBtn(_communityPopupType))
            {
                RefreshAllLinkBtn();
            }
            if (_linkType == LinkType.CommunityGiftLink)
            {
                DataTracker.gift_link.Track(_giftCode, communityLinkMan.IsNeedClaimReward(_communityPopupType));
            }
        }

        /// <summary>
        /// 刷新链接按钮
        /// </summary>
        private void RefreshAllLinkBtn()
        {
            var communityLinkMan = Game.Manager.communityLinkMan;
            var settingList = communityLinkMan.GetCommunityList();
            foreach (var settingData in settingList)
            {
                var cell = GameObjectPoolManager.Instance.CreateObject(_mLinkItemType, LinkBtnRoot.transform);
                cell.SetActive(true);
                var btnItem = cell.GetComponent<UIBtnLinkItem>();
                btnItem.UpdateContent(settingData, LinkType.CommunityGiftLink);
                btnItem.SetLabelIsShow(false);
                _linkCellList.Add(cell);
            }
        }

        /// <summary>
        /// 刷新奖励按钮
        /// </summary>
        private void RefreshAllRewardBtn()
        {
            for (int i = 0; i < _commitRewardList.Count; i++)
            {
                var rewardItem = _commitRewardList[i];
                CreateRewardCell(rewardItem.rewardId, rewardItem.rewardCount);
            }
        }

        /// <summary>
        /// 领取按钮点击
        /// </summary>y
        private void ConfirmClick()
        {
            Close();
            foreach (var item in _commitRewardList)
            {
                UIFlyUtility.FlyReward(item, root.transform.position);
            }
        }

        /// <summary>
        /// 清除
        /// </summary>
        private void Clear()
        {
            foreach (var item in _linkCellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(_mLinkItemType, item);
            }
            _linkCellList.Clear();

            foreach (var item in _rewardCellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(_mRewardItemType, item);
            }
            _rewardCellList.Clear();
        }

        /// <summary>
        /// 刷新已领取奖励
        /// </summary>
        private void RefreshHadGetReward()
        {
            DebugEx.Info($"UICommunityPlanGiftReward _CDKeyRewardList: {_CDKeyRewardList.Count}");
            for (int i = 0; i < _CDKeyRewardList.Count; i++)
            {
                var rewardItem = _CDKeyRewardList[i];
                CreateRewardCell(rewardItem.Id, rewardItem.Count);
            }
        }

        /// <summary>
        /// 创建外链奖励cell
        /// </summary>
        /// <param name="rewardId"></param>
        /// <param name="rewardCount"></param>
        private void CreateRewardCell(int rewardId, int rewardCount)
        {
            var cell = GameObjectPoolManager.Instance.CreateObject(_mRewardItemType, GiftRewardRoot.transform);
            cell.SetActive(true);
            var item = cell.GetComponent<MBRewardIcon>();
            item.Refresh(rewardId, rewardCount);
            item.transform.Find("finish").gameObject.SetActive(_communityPopupType == CommunityPopupType.LinkHadGetReward);
            item.transform.Find("count").gameObject.SetActive(_communityPopupType != CommunityPopupType.LinkHadGetReward);
            _rewardCellList.Add(cell);
        }
    }
}