/**
 * @Author: lizhenpeng
 * @Date: 2025/8/1 19:08
 * @LastEditors: lizhenpeng
 * @LastEditTime: 2025/8/1 19:08
 * @Description: 1加1礼包UI控制
 */

using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Config;

namespace FAT
{
    public class UIOnePlusOneMineCartPack : UIBase
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
        private PackOnePlusOneMineCart pack;
        
        private bool _isDelayRefresh = false; //是否延迟刷新界面 在随机宝箱开启时会延迟界面表现到随机宝箱领完后执行
        
        protected override void OnCreate()
        {
            // 空引用检查：确保按钮查找和事件绑定安全
            var maskBtn = transform.AddButton("Mask", base.Close);
            var closeBtn = transform.AddButton("Content/Root/BtnClose", base.Close);
            closeBtn?.FixPivot();  // 安全访问FixPivot

            if (payRewardGroup?.buyBtn != null)  // 双重空检查
            {
                payRewardGroup.buyBtn.WithClickScale().FixPivot()
                    .onClick.AddListener(_OnPayBtnClick);
            }
            else
            {
                Debug.LogError("payRewardGroup或其buyBtn未赋值");  // 错误提示
            }

            if (freeRewardGroup?.buyBtn != null)  // 双重空检查
            {
                freeRewardGroup.buyBtn.WithClickScale().FixPivot()
                    .onClick.AddListener(_OnFreeBtnClick);
            }
            else
            {
                Debug.LogError("freeRewardGroup或其buyBtn未赋值");  // 错误提示
            }
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackOnePlusOneMineCart)items[0];
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

        private void _RefreshRewardGroup(UIPackGroup group, IList<RewardConfig> rewards)
        {
            int count = rewards.Count;
            group.layout.spacing = count <= 4 ? 60f : 40f;

            for (int i = 0; i < group.rewardList.Count; i++)
            {
                var rewardIcon = group.rewardList[i];
                if (i < count)
                {
                    rewardIcon.gameObject.SetActive(true);
                    rewardIcon.Refresh(rewards[i]);
                }
                else
                {
                    rewardIcon.gameObject.SetActive(false);
                }
            }
        }

        private void _RefreshPack()
        {
            //付费奖励
            _RefreshRewardGroup(payRewardGroup, pack.Goods?.reward);

            //免费奖励
            _RefreshRewardGroup(freeRewardGroup, pack.freeGoods);
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
            if (freeRewardGroup.lockAnim != null)
            {
                if (isInit)
                {
                    freeRewardGroup.lockAnim.gameObject.SetActive(buyCount == 0);
                }

                string targetAnim = buyCount == 0 ? "eff_lock_close" : "eff_lock_open";

                // 检查当前是否正在播放目标动画
                if (freeRewardGroup.lockAnim.isPlaying &&
                    freeRewardGroup.lockAnim.clip != null &&
                    freeRewardGroup.lockAnim.clip.name == targetAnim)
                {
                    return; // 已在播放目标动画，无需重复播放
                }

                freeRewardGroup.lockAnim.Play(targetAnim);
            }
            else
            {
                Debug.LogWarning("freeRewardGroup.lockAnim未赋值");
            }
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
            for (var n = 0; n < list_.Count && n < payRewardGroup.rewardList.Count; ++n) {
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
            DataTracker.oneplusone_minecart_reward.Track(pack, false);
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
                        for (var n = 0; n < rewards.Count && n < freeRewardGroup.rewardList.Count; ++n) {
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
