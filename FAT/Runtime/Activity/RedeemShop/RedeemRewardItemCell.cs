/*
 * @Author: yanfuxing
 * @Date: 2025-05-19 10:20:05
 */
using System;
using System.Collections.Generic;
using Config;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class RedeemRewardItemCell : MonoBehaviour
    {
        [SerializeField] private UIImageRes _rewardIcon;
        [SerializeField] private TextMeshProUGUI _rewardIconText;
        [SerializeField] private UIImageState _itemBgState;
        [SerializeField] private UIImageState _itemBtnState;
        [SerializeField] private Transform _rewardTrans;
        [SerializeField] private Transform _lockTrans;
        [SerializeField] private Transform _DoneTrans;
        [SerializeField] private TextMeshProUGUI _leftCountText;
        [SerializeField] private UIImageRes _btnIcon;
        [SerializeField] private TextMeshProUGUI _btnIconNumText;
        [SerializeField] private Transform _freeTrans;
        [SerializeField] private Button _BtnClick;
        [SerializeField] private UIImageState _arrowImageState;
        private RedeemShopNodeItem _nodeItem;

        private ActivityRedeemShopLike _activityRedeemShopLike;

        void OnEnable()
        {
            _activityRedeemShopLike = (ActivityRedeemShopLike)Game.Manager.activity.LookupAny(fat.rawdata.EventType.Redeem);
            if (_activityRedeemShopLike == null)
            {
                DebugEx.Error("RedeemRewardItemCell: ActivityRedeemShopLike not found!");
            }
        }
        
        void Awake()
        {
            _BtnClick.onClick.AddListener(OnClick);

        }



        public void SetData(string redeemCoinImageStr, int rewardCount, RedeemShopNodeItem nodeItem)
        {
            _nodeItem = nodeItem;
            _rewardIcon.SetImage(redeemCoinImageStr);
            if (_rewardIconText != null)
            {
                _rewardIconText.text = rewardCount.ToString();
            }
            _leftCountText.text = I18N.FormatText("#SysComDesc1153", nodeItem.LeftRedeemCount);
            _btnIconNumText.text = nodeItem.needRedeemScore.ToString();
            if (_DoneTrans != null)
            {
                var isForbidClick = nodeItem.ItemState == RedeemShopItemState.Done;
                _BtnClick.interactable = !isForbidClick;

            }
            _arrowImageState.Select(nodeItem.IsCur || nodeItem.ItemState == RedeemShopItemState.Done ? 0 : 1);
            _arrowImageState.gameObject.SetActive(nodeItem.ItemIndex != 0);
        }

        public void SetBtnState(bool isUnLock)
        {
            if (_itemBtnState != null)
            {
                _itemBtnState.Select(isUnLock ? 1 : 0);
            }
        }

        public void SetBtnBgState(bool isUnLock)
        {
            if (_itemBgState != null)
            {
                _itemBgState.Select(isUnLock ? 0 : 1);
            }
        }

        public void SetBtnFreeState(bool isUnLock)
        {
            if (_rewardTrans != null)
            {
                if (this._nodeItem.needRedeemScore <= 0)
                {
                    _rewardTrans.gameObject.SetActive(!isUnLock);
                    _freeTrans.gameObject.SetActive(isUnLock);
                }
            }
        }

        public void SetBtnImageClick(bool isUnLock)
        {
            if (_itemBtnState != null)
            {
                var btn = _itemBtnState.GetComponent<Image>();
                if (btn != null)
                {
                    btn.raycastTarget = isUnLock;
                }
            }
        }

        private List<RewardCommitData> _commitRewardList = new(); //里程碑奖励数据列表
        private void OnClick()
        {
            if (_nodeItem.ItemState == RedeemShopItemState.CanRedeem && _nodeItem.needRedeemScore > 0)
            {
                if (_activityRedeemShopLike.CurRedeemCoinNum < _nodeItem.needRedeemScore)
                {
                    //提示兑换币不足
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.RedeemNoToken);
                    return;
                }
            }

            if (_nodeItem.ItemState == RedeemShopItemState.Lock)
            {
                //提示未解锁
                Game.Manager.commonTipsMan.ShowPopTips(Toast.RedeemLocked);
                return;
            }

            _commitRewardList.Clear();
            foreach (var rewardItem in _nodeItem.RedeemRewardList)
            {
                var commitData = Game.Manager.rewardMan.BeginReward(rewardItem.Id, rewardItem.Count, ReasonString.redeem_reward);
                _commitRewardList.Add(commitData);
            }


            foreach (var item in _commitRewardList)
            {
                UIFlyUtility.FlyReward(item, this.transform.position);
            }

            //进行数据层刷新
            this._nodeItem.LeftRedeemCount -= 1;
            if (_nodeItem.LeftRedeemCount <= 0)
            {
                _BtnClick.interactable = false;
            }

            if (_nodeItem.LeftRedeemCount > 0)
            {
                _activityRedeemShopLike.Track_RedeemReward(this._nodeItem);
            }
            
            MessageCenter.Get<MSG.REDEEMSHOP_BUY_REFRESH>().Dispatch((int)_nodeItem.RewardPoolType, _nodeItem.ItemIndex, _nodeItem.LeftRedeemCount);

            _activityRedeemShopLike.BuyShopDataChange(_nodeItem.needRedeemScore, _nodeItem.RewardId);
            MessageCenter.Get<MSG.REDEEMSHOP_DATA_CHANGE>().Dispatch();

        }
    }
}

