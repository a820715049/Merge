/*
 * @Author: tang.yan
 * @Description: 1+2礼包界面 
 * @Date: 2024-07-03 10:07:55
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
    public class UIOnePlusTwoPack : UIBase
    {
        [Serializable]
        private class UIPackGroup
        {
            [SerializeField] public VerticalLayoutGroup layout;
            [SerializeField] public List<UICommonItem> rewardList;
            [SerializeField] public Button buyBtn;
            [SerializeField] public GameObject receiveGo;
            [SerializeField] public GameObject freeGo;
            [SerializeField] public Animation lockAnim;
            [SerializeField] public GameObject iapGo;
            [SerializeField] public UITextState iapPriceState;
            [SerializeField] public GameObject maskGo;
        }
        
        [SerializeField] private UIImageRes bg;
        [SerializeField] private UIImageRes titleBg;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text cdText;
        [SerializeField] private UIImageRes cdBg;
        [SerializeField] private UIPackGroup payRewardGroup;
        [SerializeField] private UIPackGroup freeRewardGroup1;
        [SerializeField] private UIPackGroup freeRewardGroup2;

        private Action WhenInit;
        private Action<ActivityLike, bool> WhenEnd;
        private Action<ActivityLike> WhenRefresh;
        private Action<IList<RewardCommitData>> WhenPurchase;
        private Action WhenTick;
        private PackOnePlusTwo pack;
        
        private bool _isDelayRefresh = false; //是否延迟刷新界面 在随机宝箱开启时会延迟界面表现到随机宝箱领完后执行
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/Root/BtnClose", base.Close).FixPivot();
            payRewardGroup.buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnPayBtnClick);
            freeRewardGroup1.buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnFreeBtnClick1);
            freeRewardGroup2.buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnFreeBtnClick2);
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackOnePlusTwo)items[0];
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
            visual.Refresh(bg, "bgImage");
            visual.Refresh(titleBg, "titleBg");
            visual.Refresh(infoText, "subTitle");
            visual.Refresh(cdText, "time");
            visual.Refresh(cdBg, "time");
        }

        private void _RefreshPack() {
            //付费奖励
            var payGoods = pack.Goods.reward;
            var payGoodsCount = payGoods.Count;
            payRewardGroup.layout.spacing = _GetLayoutSpace(payGoodsCount);
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
            //免费奖励1
            var freeGoods1 = pack.freeGoods1;
            var freeGoodsCount1 = freeGoods1.Count;
            freeRewardGroup1.layout.spacing = _GetLayoutSpace(freeGoodsCount1);
            for (var i = 0; i < freeRewardGroup1.rewardList.Count; i++)
            {
                var reward = freeRewardGroup1.rewardList[i];
                if (i < freeGoodsCount1)
                {
                    reward.gameObject.SetActive(true);
                    reward.Refresh(freeGoods1[i]);
                }
                else
                {
                    reward.gameObject.SetActive(false);
                }
            }
            //免费奖励2
            var freeGoods2 = pack.freeGoods2;
            var freeGoodsCount2 = freeGoods2.Count;
            freeRewardGroup2.layout.spacing = _GetLayoutSpace(freeGoodsCount2);
            for (var i = 0; i < freeRewardGroup2.rewardList.Count; i++)
            {
                var reward = freeRewardGroup2.rewardList[i];
                if (i < freeGoodsCount2)
                {
                    reward.gameObject.SetActive(true);
                    reward.Refresh(freeGoods2[i]);
                }
                else
                {
                    reward.gameObject.SetActive(false);
                }
            }
        }

        private float _GetLayoutSpace(int goodsCount)
        {
            var space = goodsCount <= 3 ? 86f : (goodsCount <= 4 ? 46f : 38f);
            return space;
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
            //buyCount =2说明只领了第一个免费奖励 =3说明只领了第二个免费奖励 =4说明免费奖励都领了
            //第一列免费组底部状态 
            var isCollect1 = buyCount == 2 || buyCount == 4;
            freeRewardGroup1.buyBtn.gameObject.SetActive(!isCollect1);
            freeRewardGroup1.receiveGo.gameObject.SetActive(isCollect1);
            freeRewardGroup1.freeGo.gameObject.SetActive(!isCollect1);
            freeRewardGroup1.iapGo.gameObject.SetActive(false);
            freeRewardGroup1.maskGo.gameObject.SetActive(isCollect1);
            if (isInit)
            {
                freeRewardGroup1.lockAnim.gameObject.SetActive(buyCount == 0);
            }
            if (buyCount == 0)
                freeRewardGroup1.lockAnim.Play("eff_lock_close");
            else if (buyCount == 1)
                freeRewardGroup1.lockAnim.Play("eff_lock_open");
            //第二列免费组底部状态
            var isCollect2 = buyCount == 3 || buyCount == 4;
            freeRewardGroup2.buyBtn.gameObject.SetActive(!isCollect2);
            freeRewardGroup2.receiveGo.gameObject.SetActive(isCollect2);
            freeRewardGroup2.freeGo.gameObject.SetActive(!isCollect2);
            freeRewardGroup2.iapGo.gameObject.SetActive(false);
            freeRewardGroup2.maskGo.gameObject.SetActive(isCollect2);
            if (isInit)
            {
                freeRewardGroup2.lockAnim.gameObject.SetActive(buyCount == 0);
            }
            if (buyCount == 0)
                freeRewardGroup2.lockAnim.Play("eff_lock_close");
            else if (buyCount == 1)
                freeRewardGroup2.lockAnim.Play("eff_lock_open");
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
            DataTracker.oneplustwo_reward.Track(pack, 1, false);
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

        private void _OnFreeBtnClick1()
        {
            //未购买礼包 或者已购买已领取第一个免费奖励
            if (pack.BuyCount < 1 || pack.BuyCount == 2 || pack.BuyCount == 4)
                return;
            using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
            {
                if (pack.CollectFreeReward(1, rewards))
                {
                    for (var n = 0; n < rewards.Count; ++n) {
                        var pos = freeRewardGroup1.rewardList[n].transform.position;
                        UIFlyUtility.FlyReward(rewards[n], pos);
                    }
                    pack.ResetPopup();
                    _RefreshGroup();
                }
            }
            
        }
        
        private void _OnFreeBtnClick2()
        {
            //未购买礼包 或者已购买已领取第二个免费奖励
            if (pack.BuyCount < 1 || pack.BuyCount == 3 || pack.BuyCount == 4)
                return;
            using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
            {
                if (pack.CollectFreeReward(2, rewards))
                {
                    for (var n = 0; n < rewards.Count; ++n) {
                        var pos = freeRewardGroup2.rewardList[n].transform.position;
                        UIFlyUtility.FlyReward(rewards[n], pos);
                    }
                    pack.ResetPopup();
                    _RefreshGroup();
                }
            }
        }
    }
}