/**
 * @Author: zhangpengjian
 * @Date: 2025/2/18 17:36:30
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/2/18 17:36:30
 * Description: 砍价礼包
 */

using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT.MSG;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIPackDiscount : PackUI
    {

        [SerializeField]
        private Button info;

        [SerializeField]
        private MBRewardProgress progress;

        [SerializeField]
        private MBRewardLayout reward;

        [SerializeField]
        private TextMeshProUGUI discountMilestone;

        [SerializeField]
        private TextMeshProUGUI discountMax;

        [SerializeField]
        private Transform confirmA;

        [SerializeField]
        private Transform confirmB;
        [SerializeField]
        private UITextState priceCurr;
        [SerializeField]
        private UITextState pricePrev;
        [SerializeField]
        private UITextState price2;
        [SerializeField]
        private UITextState price3;
        [SerializeField]
        private SkeletonGraphic skeletonAnimationA;
        [SerializeField]
        private SkeletonGraphic skeletonAnimationB;
        [SerializeField]
        private Animator animatorSpine;
        [SerializeField]
        private Animator animatorText;
        [SerializeField]
        private Animator animatorConfirm;
        [SerializeField]
        private Transform block;
        private Action WhenInit;

        internal override ActivityLike Pack => pack;
        private PackDiscount pack;
        private bool passiveOpen;
        private float duration = 0.75f;

        protected override void OnParse(params object[] items)
        {
            passiveOpen = false;
            if (items.Length > 0)
            {
                pack = (PackDiscount)items[0];
                if (items.Length > 1)
                {
                    passiveOpen = (bool)items[1];
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            info.WithClickScale().FixPivot().onClick.AddListener(() => InfoClick());
        }

        protected override void OnPreOpen()
        {
            block.gameObject.SetActive(false);
            RefreshTheme();
            RefreshCD();
            RefreshPrice();
            RefreshPack();
            RefreshAnimation();
            RefreshProgress();
            WhenInit ??= RefreshPrice;
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            WhenRefresh ??= p_ => { if (p_ == pack) RefreshPack(); };
            WhenPurchase ??= r_ => _PurchaseComplete(r_);
            MessageCenter.Get<IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        private void RefreshProgress()
        {
            var (token, max, show) = pack.GetTokenProgress();
            if (passiveOpen)
            {
                StartCoroutine(RefreshPackCoroutine());
            }
            else
            {
                if (pack.TokenPrev != token)
                {
                    progress.Refresh(pack.TokenPrev, max);
                    progress.Refresh(token, max, duration);
                }
                else
                {
                    progress.Refresh(token, max);
                }
                discountMilestone.text = I18N.FormatText("#SysComDesc463", show);
                pack.SetPrevToken(token);
            }
        }

        private void RefreshAnimation()
        {
            skeletonAnimationA.AnimationState.SetAnimation(0, "idle", true);
            skeletonAnimationB.AnimationState.SetAnimation(0, "idle", true);
        }

        private void RefreshPrice()
        {
            var valid = Game.Manager.iap.Initialized;
            var p = pack.GetTokenPhase();
            var (_, priceLast, priceLast2) = pack.GetPrice();
            if (passiveOpen)
            {
                //被动打开 购买按钮上的价格应该是上次的
                //原价时 为上次
                //砍价时 为上次和上上次
                if (p > 1)
                {
                    animatorConfirm.SetTrigger("Two");
                    priceCurr.Enabled(valid, priceLast);
                    pricePrev.Enabled(valid, priceLast2);
                    price2.Enabled(valid, pack.Price);
                    price3.Enabled(valid, priceLast);
                }
                else
                {
                    animatorConfirm.SetTrigger("One");
                    confirm.State(valid, priceLast);
                    priceCurr.Enabled(valid, pack.Price);
                    pricePrev.Enabled(valid, priceLast);
                }
            }
            else
            {
                animatorConfirm.SetTrigger(p > 0 ? "Two" : "One");
                confirm.State(valid, pack.Price);
                priceCurr.Enabled(valid, pack.Price);
                pricePrev.Enabled(valid, priceLast);
                price2.Enabled(valid, pack.Price);
                price3.Enabled(valid, priceLast);
            }

        }

        private void RefreshPack()
        {
            discountMax.text = I18N.FormatText("#SysComDesc913", pack.ConfProgress.DiscountShow);
            reward.Refresh(pack.Goods.reward);
        }

        private IEnumerator RefreshPackCoroutine()
        {
            block.gameObject.SetActive(true);
            var p = pack.GetTokenPhase();
            var lastMax = pack.ConfProgress.ProgressDetail[p - 1].ConvertToRewardConfig().Count;
            var lastDiscountShow = pack.ConfProgress.ProgressDetail[p - 1].ConvertToRewardConfig().Id;
            discountMilestone.text = I18N.FormatText("#SysComDesc463", lastDiscountShow);
            progress.Refresh(pack.TokenPrev, lastMax);
            progress.Refresh(lastMax, lastMax, duration);
            yield return new WaitForSeconds(duration);
            Game.Manager.audioMan.TriggerSound("DiscountProgress");
            animatorText.SetTrigger("Punch");
            animatorSpine.SetTrigger("Punch");
            if (p == 1)
            {
                animatorConfirm.SetTrigger("Punch1");
            }
            else
            {
                animatorConfirm.SetTrigger("Punch2");
            }
            Game.Manager.audioMan.TriggerSound("DiscountBtnChange");
            skeletonAnimationA.AnimationState.ClearTrack(0);
            skeletonAnimationB.AnimationState.ClearTrack(0);
            // 添加一帧延迟后再播放动画
            skeletonAnimationA.AnimationState.SetEmptyAnimation(0, 0);
            skeletonAnimationB.AnimationState.SetEmptyAnimation(0, 0);
            Game.Manager.audioMan.TriggerSound("DiscountCut");
            skeletonAnimationA.AnimationState.SetAnimation(0, "cut", false).Complete += delegate (TrackEntry entry)
            {
                skeletonAnimationA.AnimationState.SetAnimation(0, "idle", true);
            };
            skeletonAnimationB.AnimationState.SetAnimation(0, "cut", false).Complete += delegate (TrackEntry entry)
            {
                block.gameObject.SetActive(false);
                skeletonAnimationB.AnimationState.SetAnimation(0, "idle", true);
            };
            var (token, max, show) = pack.GetTokenProgress();
            discountMilestone.text = I18N.FormatText("#SysComDesc463", show);

            pack.SetPrevToken(token);
            if (!pack.IsMax())
            {
                progress.Refresh(0, max);
                progress.Refresh(token, max, duration);
            }
        }

        private void InfoClick()
        {
            pack.HelpRes.ActiveR.Open(pack);
        }

        internal override void ConfirmClick()
        {
            Game.Manager.activity.giftpack.Purchase(pack, pack.Content, WhenPurchase);
        }

        private void _PurchaseComplete(IList<RewardCommitData> list_)
        {
            if (transform.gameObject.activeSelf)
            {
                base.PurchaseComplete(list_);
                DataTracker.event_discountpack_reward.Track(Pack, pack.GetTokenPhase());
            }
        }
    }
}