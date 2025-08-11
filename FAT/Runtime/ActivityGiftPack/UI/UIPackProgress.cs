/*
 * @Author: pengjian.zhang
 * @Description: 进阶礼包UI
 * @Date: 2024-07-24 11:36:21
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.rawdata;
using TMPro;

namespace FAT {
    public class UIPackProgress : PackUI {
        [SerializeField] public VerticalLayoutGroup layout1;
        [SerializeField] public VerticalLayoutGroup layout2;
        [SerializeField] public VerticalLayoutGroup layout3;
        [SerializeField] public List<MBRewardIcon> rewardList1;
        [SerializeField] public List<MBRewardIcon> rewardList2;
        [SerializeField] public List<MBRewardIcon> rewardList3;
        [SerializeField] public UIImageRes bg1;
        [SerializeField] public TextProOnACircle title;
        [SerializeField] public TMP_Text desc;
        [SerializeField] public Button confirm1;
        [SerializeField] public Button confirm2;
        [SerializeField] public Button confirm3;
        [SerializeField] public UITextState price1;
        [SerializeField] public UITextState price2;
        [SerializeField] public UITextState price3;
        [SerializeField] public Animator right1;
        [SerializeField] public Animator right2;
        [SerializeField] public Animator right3;
        [SerializeField] public Transform label1;
        [SerializeField] public Transform label2;
        [SerializeField] public Transform label3;
        [SerializeField] public Animation btnLock2;
        [SerializeField] public Animation btnLock3;
        private Action WhenInit;
        private PackProgress pack;
        internal override ActivityLike Pack => pack;
        private MBRewardLayout active;
        private bool isDelayRefresh = false; //是否延迟刷新界面 在随机宝箱开启时会延迟界面表现到随机宝箱领完后执行
        private List<List<MBRewardIcon>> rewardList = new();
        private UIIAPLabel iapLabel = new();
        
        protected override void OnCreate() {
            base.OnCreate();
            transform.FindEx<MapButton>("Content/close").WithClickScale().FixPivot().WhenClick = Close;
            confirm1.WithClickScale().FixPivot().onClick.AddListener(() => ConfirmClick(pack.Content1));
            confirm2.WithClickScale().FixPivot().onClick.AddListener(() => ConfirmClick(pack.Content2));
            confirm3.WithClickScale().FixPivot().onClick.AddListener(() => ConfirmClick(pack.Content3));
            rewardList.Add(rewardList1);
            rewardList.Add(rewardList2);
            rewardList.Add(rewardList3);
        }

        protected override void OnParse(params object[] items) {
            pack = (PackProgress)items[0];
        }

        protected override void OnPreOpen() {
            RefreshTheme();
            RefreshPack();
            RefreshPrice();
            RefreshGroup(true);
            RefreshCD();
            RefreshLabel();
            WhenInit ??= RefreshPrice;
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            WhenRefresh ??= p_ => { if (p_ == pack) RefreshPack(); };
            WhenPurchase ??= r_ => PurchaseComplete(r_);
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(OnRandomBoxFinish);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnRandomBoxFinish);
        }
        
        protected override void OnPostClose()
        {
            isDelayRefresh = false;
            iapLabel.Clear();
        }

        public override void RefreshTheme() 
        {
            var visual = pack.Visual;
            visual.Refresh(bg1, "titleBg");
            visual.Refresh(title, "mainTitle");
            visual.Refresh(desc, "subTitle");
        }       
        
        private void RefreshLabel() 
        {
            iapLabel.Setup(label1, pack.confD.Sale1, pack.PackId1);
            iapLabel.Setup(label2, pack.confD.Sale2, pack.PackId2);
            iapLabel.Setup(label3, pack.confD.Sale3, pack.PackId3);
        }

        public void RefreshPack() {
            //付费奖励
            layout1.spacing = pack.Goods1.reward.Count > 2 ? 20f : 40f;
            layout2.spacing = pack.Goods2.reward.Count > 3 ? 20f : 40f;
            RefreshRewardList(rewardList1, pack.Goods1);
            RefreshRewardList(rewardList2, pack.Goods2);
            RefreshRewardList(rewardList3, pack.Goods3);
        }

        private void RefreshRewardList(List<MBRewardIcon> l, BonusReward r)
        {
            for (int i = 0; i < l.Count; i++)
            {
                var reward = l[i];
                if (i < r.reward.Count)
                {
                    reward.gameObject.SetActive(true);
                    reward.Refresh(r.reward[i]);
                }
                else
                {
                    reward.gameObject.SetActive(false);
                }
            }
        }

        public void RefreshPrice()
        {
            var valid = Game.Manager.iap.Initialized;
            price1.Enabled(valid, pack.Price1);
            price2.Enabled(valid, pack.Price2);
            price3.Enabled(valid, pack.Price3);
            if (!valid)
            {
                confirm1.interactable = false;
                confirm2.interactable = false;
                confirm3.interactable = false;
                GameUIUtility.SetGrayShader(confirm1.image);
                GameUIUtility.SetGrayShader(confirm2.image);
                GameUIUtility.SetGrayShader(confirm3.image);
            }
        }

        internal override void PurchaseComplete(IList<RewardCommitData> list_)
        {
            
            Pack.ResetPopup();
            var buyCount = pack.BuyCount;
            for (var n = 0; n < list_.Count; ++n) {
                var pos = rewardList[buyCount - 1][n].transform.position;
                UIFlyUtility.FlyReward(list_[n], pos);
            }
            if (!Game.Manager.specialRewardMan.CheckCanClaimSpecialReward())
            {
                RefreshGroup();
            }
            else
            {
                isDelayRefresh = true;
            }
            IEnumerator DelayClose() {
                yield return new WaitForSeconds(closeDelay);
                Close();
            }
            if (buyCount > 2)
            {
                //打点
                pack.ReportPurchase();
                StartCoroutine(DelayClose());
            }
        }

        internal void ConfirmClick(IAPPack content_) {
            Game.Manager.activity.giftpack.Purchase(pack, content_, WhenPurchase);
        }

        internal override void ConfirmClick() {
            Game.Manager.activity.giftpack.Purchase(pack, pack.Content, WhenPurchase);
        }
        
        private void RefreshGroup(bool isInit = false)
        {
            var buyCount = pack.BuyCount;
            confirm1.gameObject.SetActive(buyCount <= 0);
            confirm2.gameObject.SetActive(buyCount <= 1);
            confirm3.gameObject.SetActive(buyCount <= 2);
            right1.gameObject.SetActive(buyCount > 0);
            right2.gameObject.SetActive(buyCount > 1);
            right3.gameObject.SetActive(buyCount > 2);
            if (!isInit)
            {
                if (buyCount == 1)
                {
                    right1.SetTrigger("Punch");
                }
                if (buyCount == 2)
                {
                    btnLock2.gameObject.SetActive(false);
                    right2.SetTrigger("Punch");
                }
                if (buyCount == 3)
                {
                    right3.SetTrigger("Punch");
                    btnLock3.gameObject.SetActive(false);
                }
                btnLock2.Play(buyCount > 0 ? "eff_lock_open" : "eff_lock_close");
                btnLock3.Play(buyCount > 1 ? "eff_lock_open" : "eff_lock_close"); 
            }
            else
            {
                btnLock2.gameObject.SetActive(buyCount < 1);
                btnLock3.gameObject.SetActive(buyCount < 2);
                btnLock2.Play("eff_lock_close");
                btnLock3.Play("eff_lock_close");
            }
            confirm2.interactable= buyCount > 0;
            confirm3.interactable = buyCount > 1;
            if (buyCount > 0)
            {
                GameUIUtility.SetDefaultShader(confirm2.image);
                price2.Select(0);
            }
            else
            {
                GameUIUtility.SetGrayShader(confirm2.image);
                price2.Select(1);
            }

            if (buyCount > 1)
            {
                GameUIUtility.SetDefaultShader(confirm3.image);
                price3.Select(0);
            }
            else
            {
                GameUIUtility.SetGrayShader(confirm3.image);
                price3.Select(1);
            }
        }
        
        private void OnRandomBoxFinish()
        {
            if (isDelayRefresh)
            {
                isDelayRefresh = false;
                RefreshGroup();
            }
        }
    }
}