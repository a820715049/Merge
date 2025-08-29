/*
 * @Author: tang.yan
 * @Description: 1+1礼包界面 
 * @Date: 2023-12-27 20:12:11
 */
using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class UIOnePlusOneMinePack : UIBase
    {
        [Serializable]
        private class UIPackGroup
        {
            [SerializeField] public VerticalLayoutGroup layout;
            [SerializeField] public List<MBRewardIcon> rewardList;
            [SerializeField] public Button buyBtn;
            [SerializeField] public GameObject receiveGo;
            [SerializeField] public GameObject freeGo;
            [SerializeField] public Animation lockAnim;
            [SerializeField] public GameObject iapGo;
            [SerializeField] public UITextState iapPriceState;
            [SerializeField] public GameObject maskGo;
        }
        
        [SerializeField] private TextProOnACircle titleText;
        [SerializeField] private TMP_Text titleTextNormal;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text cdText;
        [SerializeField] private UIPackGroup payRewardGroup;
        [SerializeField] private UIPackGroup freeRewardGroup;

        private Action WhenInit;
        private Action<ActivityLike, bool> WhenEnd;
        internal Action<ActivityLike> WhenRefresh;
        private Action<IList<RewardCommitData>> WhenPurchase;
        private Action WhenTick;
        private PackOnePlusOneMine pack;
        
        private bool _isDelayRefresh = false; //是否延迟刷新界面 在随机宝箱开启时会延迟界面表现到随机宝箱领完后执行
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/Root/BtnClose", base.Close).FixPivot();
            payRewardGroup.buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnPayBtnClick);
            freeRewardGroup.buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnFreeBtnClick);
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackOnePlusOneMine)items[0];
        }

        protected override void OnPreOpen()
        {
            _RefreshTheme();
            _RefreshPack();
            _RefreshPrice();
            _RefreshGroup(true);
            _RefreshCD();
        }

        protected override void OnAddListener()
        {
            WhenInit ??= _RefreshPrice;
            WhenEnd ??= _RefreshEnd;
            WhenTick ??= _RefreshCD;
            WhenRefresh ??= p_ => { if (p_ == pack) _RefreshPack(); };
            WhenPurchase ??= r_ => _PurchaseComplete(r_);
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(_OnRandomBoxFinish);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(_OnRandomBoxFinish);
        }

        protected override void OnPostClose()
        {
            _isDelayRefresh = false;
        }
        
        private void _RefreshTheme() {
            var visual = pack.Visual;
            if (titleText != null) visual.Refresh(titleText, "mainTitle");
            if (titleTextNormal != null) visual.Refresh(titleTextNormal, "mainTitle");
            visual.Refresh(infoText, "subTitle");
        }

        private void _RefreshPack() {
            //付费奖励
            var payGoods = pack.Goods.reward;
            int payGoodsCount = payGoods.Count;
            payRewardGroup.layout.spacing = payGoodsCount <= 4 ? 60f : 40f;
            for (int i = 0; i < payRewardGroup.rewardList.Count; i++)
            {
                var reward = payRewardGroup.rewardList[i];
                if (i < payGoodsCount)
                {
                    reward.gameObject.SetActive(true);
                    reward.Refresh(payGoods[i]);
                }
                else
                {
                    reward.gameObject.SetActive(false);
                }
            }
            //免费奖励
            var freeGoods = pack.freeGoods;
            int freeGoodsCount = freeGoods.Count;
            freeRewardGroup.layout.spacing = freeGoodsCount <= 4 ? 60f : 40f;
            for (int i = 0; i < freeRewardGroup.rewardList.Count; i++)
            {
                var reward = freeRewardGroup.rewardList[i];
                if (i < freeGoodsCount)
                {
                    reward.gameObject.SetActive(true);
                    reward.Refresh(freeGoods[i]);
                }
                else
                {
                    reward.gameObject.SetActive(false);
                }
            }
        }

        private void _RefreshGroup(bool isInit = false)
        {
            int buyCount = pack.BuyCount;
            //付费组底部状态
            payRewardGroup.buyBtn.gameObject.SetActive(buyCount == 0);
            payRewardGroup.receiveGo.gameObject.SetActive(buyCount > 0);
            payRewardGroup.freeGo.gameObject.SetActive(false);
            payRewardGroup.iapGo.gameObject.SetActive(buyCount == 0);
            payRewardGroup.maskGo.gameObject.SetActive(buyCount >= 1);
            //免费组底部状态
            freeRewardGroup.buyBtn.gameObject.SetActive(buyCount < 2);
            freeRewardGroup.receiveGo.gameObject.SetActive(buyCount >= 2);
            freeRewardGroup.freeGo.gameObject.SetActive(buyCount < 2);
            freeRewardGroup.iapGo.gameObject.SetActive(false);
            freeRewardGroup.maskGo.gameObject.SetActive(buyCount >= 2);
            if (isInit)
            {
                freeRewardGroup.lockAnim.gameObject.SetActive(buyCount == 0);
            }
            freeRewardGroup.lockAnim.Play(buyCount == 0 ? "eff_lock_close" : "eff_lock_open");
        }

        private void _RefreshPrice() {
            if (pack.BuyCount == 0)
            {
                var valid = Game.Manager.iap.Initialized;
                payRewardGroup.iapPriceState.Enabled(valid, pack.Price);
                if (valid)
                {
                    payRewardGroup.buyBtn.interactable = true;
                    GameUIUtility.SetDefaultShader(payRewardGroup.buyBtn.image);
                }
                else
                {
                    payRewardGroup.buyBtn.interactable = false;
                    GameUIUtility.SetGrayShader(payRewardGroup.buyBtn.image);
                }
            }
        }

        private void _RefreshCD() {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, pack.endTS - t);
            UIUtility.CountDownFormat(cdText, diff);
        }

        private void _RefreshEnd(ActivityLike pack_, bool expire_) {
            if (pack_ != pack) return;
            Close();
        }

        private void _PurchaseComplete(IList<RewardCommitData> list_) {
            pack.ResetPopup();
            for (var n = 0; n < list_.Count; ++n) {
                var pos = payRewardGroup.rewardList[n].transform.position;
                UIFlyUtility.FlyReward(list_[n], pos);
            }
            if (!Game.Manager.specialRewardMan.CheckCanClaimSpecialReward())
            {
                _RefreshGroup();
            }
            else
            {
                _isDelayRefresh = true;
            }
            //打点
            DataTracker.oneplusone_mine_reward.Track(pack, false);
        }
        
        private void _OnRandomBoxFinish()
        {
            if (_isDelayRefresh)
            {
                _isDelayRefresh = false;
                _RefreshGroup();
            }
        }

        private void _OnPayBtnClick()
        {
            if (pack.BuyCount == 0)
            {
                Game.Manager.activity.giftpack.Purchase(pack, WhenPurchase);
            }
        }

        private void _OnFreeBtnClick()
        {
            if (pack.BuyCount == 1)
            {
                using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
                {
                    if (pack.CollectFreeReward(rewards))
                    {
                        for (var n = 0; n < rewards.Count; ++n) {
                            var pos = freeRewardGroup.rewardList[n].transform.position;
                            UIFlyUtility.FlyReward(rewards[n], pos);
                        }
                        pack.ResetPopup();
                        _RefreshGroup();
                    }
                }
            }
        }
    }
}