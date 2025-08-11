/*
 * @Author: yanfuxing
 * @Date: 2025-05-19 10:25:05
 */
using System.Collections.Generic;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIRedeemRewardBigItemCell : MonoBehaviour
    {
        [SerializeField] private UIImageRes _reward1Icon;
        [SerializeField] private TextMeshProUGUI _reward1IconText;
        [SerializeField] private UIImageRes _reward2Icon;
        [SerializeField] private TextMeshProUGUI _reward2IconText;
        [SerializeField] private UIImageState _itemBgState;
        [SerializeField] private UIImageState _itemBtnState;
        [SerializeField] private Transform _rewardTrans;
        [SerializeField] private Transform _lockTrans;
        [SerializeField] private Transform _DoneTrans;
        [SerializeField] private TextMeshProUGUI _leftCountText;
        [SerializeField] private Transform _freeTrans;
        [SerializeField] private Button _BtnClick;
        [SerializeField] private UIImageState _arrowImageState;
        [SerializeField] private TextMeshProUGUI _btnIconNumText;
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

        public void SetData(string reward1ImageStr, int count1, string reward2ImageStr, int count2, RedeemShopNodeItem nodeItem)
        {
            this._nodeItem = nodeItem;
            _reward1Icon.SetImage(reward1ImageStr);
            _reward2Icon.SetImage(reward2ImageStr);
            if (_leftCountText != null)
            {
                _leftCountText.text = I18N.FormatText("#SysComDesc1153", nodeItem.LeftRedeemCount);
            }
            _btnIconNumText.text = nodeItem.needRedeemScore.ToString();

            if (_reward1IconText != null)
            {
                _reward1IconText.text = count1.ToString();
            }
            if (_reward2IconText != null)
            {
                _reward2IconText.text = count2.ToString();
            }
            if (_DoneTrans != null)
            {
                var isForbidClick = nodeItem.ItemState == RedeemShopItemState.Done;
                _BtnClick.interactable = !isForbidClick;
            }
            // if (nodeItem.IsCur)
            // {
            //     _arrowImageState.Select(nodeItem.IsCur ? 0 : 1);
            // }
            // _arrowImageState.gameObject.SetActive(nodeItem.ItemIndex != 0);

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


        //里程碑奖励数据列表
        private List<RewardCommitData> _commitRewardList = new();
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

