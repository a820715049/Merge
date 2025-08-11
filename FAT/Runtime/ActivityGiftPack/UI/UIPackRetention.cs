/**
 * @Author: zhangpengjian
 * @Date: 2024/8/8 17:56:24
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/9/18 11:02:18
 * Description: 付费留存礼包ui
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.rawdata;
using TMPro;
using Config;

namespace FAT
{
    public class UIPackRetention : PackUI
    {
        [SerializeField] public HorizontalLayoutGroup layout1;
        [SerializeField] public HorizontalLayoutGroup layout2;
        [SerializeField] public HorizontalLayoutGroup layout3;
        [SerializeField] public List<UICommonItem> rewardList1;
        [SerializeField] public List<UICommonItem> rewardList2;
        [SerializeField] public List<UICommonItem> rewardList3;
        [SerializeField] public UIImageRes bg;
        [SerializeField] public TMP_Text title;
        [SerializeField] public TMP_Text desc;
        [SerializeField] public Button confirmBtn;
        [SerializeField] public UITextState price;
        [SerializeField] public UITextState time;
        [SerializeField] public Animator mask1;
        [SerializeField] public Animator mask2;
        [SerializeField] public Animator mask3;
        [SerializeField] public Animator btnConfirmAnim;
        [SerializeField] public GameObject btnConfirmEfx;
        [SerializeField] public Transform cdTrans;
        [SerializeField] public UIImageRes bg2;
        [SerializeField] public UIImageRes bg3;
        [SerializeField] public UITextState desc2;
        [SerializeField] public UITextState desc3;
        private Action WhenInit;
        private PackRetention pack;
        private bool expire = false;
        internal override ActivityLike Pack => pack;
        private bool isDelayRefresh = false; //是否延迟刷新界面 在随机宝箱开启时会延迟界面表现到随机宝箱领完后执行

        protected override void OnCreate()
        {
            base.OnCreate();
            transform.FindEx<Button>("Content/BtnClose").WithClickScale().FixPivot().onClick.AddListener(Close);
            confirmBtn.WithClickScale().FixPivot().onClick.AddListener(() => ConfirmClick(pack.Content));
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackRetention)items[0];
            expire = (bool)items[1];
        }

        protected override void OnAddListener()
        {
            WhenInit ??= RefreshGroup;
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCollectCD;
            WhenRefresh ??= p_ => { if (p_ == pack) RefreshPack(); };
            WhenPurchase ??= r_ => PurchaseComplete(r_);
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(OnRandomBoxFinish);
            MessageCenter.Get<MSG.IAP_DATA_READY>().AddListener(OnIAPReissue);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnRandomBoxFinish);
            MessageCenter.Get<MSG.IAP_DATA_READY>().RemoveListener(OnIAPReissue);
        }

        protected override void OnPreOpen()
        {
            if (!Game.Manager.iap.DataReady)
            {
                confirmBtn.interactable = false;
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshTheme();
            RefreshPack();
            RefreshGroup();
            RefreshCollectCD();
            if (expire)
            {
                confirmBtn.interactable = false;
                GameUIUtility.SetGrayShader(confirmBtn.image);
                cdTrans.gameObject.SetActive(false);
                TryPurchaseOrGetFreeReward();
            }
        }

        private void RefreshCollectCD()
        {
            var buyCount = pack.BuyCount;
            if (buyCount < 1)
            {
                cdTrans.gameObject.SetActive(true);
                RefreshCD();
                return;
            }
            cdTrans.gameObject.SetActive(false);
            var collectTs = pack.GetNextCollectTs();
            var collectCd = (long)Mathf.Max(0, collectTs - Game.Instance.GetTimestampSeconds());
            time.text.text = I18N.FormatText("#SysComDesc526", UIUtility.CountDownFormat(collectCd));
            if (collectCd <= 0)
            {
                price.gameObject.SetActive(true);
                time.gameObject.SetActive(false);
                confirmBtn.interactable = true;
                btnConfirmAnim.enabled = true;
                btnConfirmAnim.SetBool("CanClick", true);
                btnConfirmEfx.SetActive(true);
                GameUIUtility.SetDefaultShader(confirmBtn.image);
            }
            else
            {
                btnConfirmEfx.SetActive(false);
                btnConfirmAnim.SetBool("CanClick", false);
            }

            if ((buyCount == 1 && Game.TimestampNow() >= pack.GetNextCollectTs()) || buyCount > 1)
            {
                pack.Visual.Refresh(bg2, "bg2");
                desc2.Select(0);
                RefreshItemText(rewardList2, 1, pack.freeGoods1.Count);
            }
            else
            {
                RefreshItemText(rewardList2, 0, pack.freeGoods1.Count);
                desc2.Select(1);
                pack.Visual.Refresh(bg2, "bg1");
            }
            if ((buyCount == 2 && Game.TimestampNow() >= pack.GetNextCollectTs()) || buyCount > 2)
            {
                RefreshItemText(rewardList3, 1, pack.freeGoods2.Count);
                desc2.Select(1);
                desc3.Select(0);
                pack.Visual.Refresh(bg3, "bg2");
            }
            else
            {
                RefreshItemText(rewardList3, 0, pack.freeGoods2.Count);
                desc3.Select(1);
                pack.Visual.Refresh(bg3, "bg1");
            }
        }

        private void OnIAPReissue()
        {
            confirmBtn.interactable = true;
            GameUIUtility.SetDefaultShader(confirmBtn.image);
            pack = (PackRetention)Game.Manager.activity.LookupAny(fat.rawdata.EventType.RetentionPack);
            RefreshAll();
        }

        protected override void OnPostClose()
        {
            isDelayRefresh = false;
        }

        public override void RefreshTheme()
        {
            var visual = pack.Visual;
            visual.Refresh(bg, "titleBg");
            visual.Refresh(title, "mainTitle");
            visual.Refresh(desc, "subTitle");
        }

        public void RefreshPack()
        {
            layout1.spacing = pack.Goods.reward.Count > 2 ? 72f : 110f;
            layout2.spacing = pack.freeGoods1.Count > 2 ? 72f : 110f;
            layout3.spacing = pack.freeGoods2.Count > 2 ? 72f : 110f;
            RefreshRewardList(rewardList1, pack.Goods);
            RefreshFreeList(rewardList2, pack.freeGoods1);
            RefreshFreeList(rewardList3, pack.freeGoods2);
        }

        private void RefreshRewardList(List<UICommonItem> l, BonusReward r)
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

        private void RefreshFreeList(List<UICommonItem> l, List<RewardConfig> r)
        {
            for (int i = 0; i < l.Count; i++)
            {
                var reward = l[i];
                if (i < r.Count)
                {
                    reward.gameObject.SetActive(true);
                    reward.Refresh(r[i]);
                }
                else
                {
                    reward.gameObject.SetActive(false);
                }
            }
        }

        public void RefreshGroup()
        {
            var valid = Game.Manager.iap.Initialized;
            var buyCount = pack.BuyCount;
            mask1.gameObject.SetActive(buyCount > 0);
            mask2.gameObject.SetActive(buyCount > 1);
            mask3.gameObject.SetActive(buyCount > 2);
            price.gameObject.SetActive(true);
            time.gameObject.SetActive(false);
            if (buyCount == 0)
            {
                price.Enabled(valid, pack.Price);
                btnConfirmEfx.SetActive(valid);
                confirmBtn.interactable = valid;
                if (valid)
                {
                    GameUIUtility.SetDefaultShader(confirmBtn.image);
                }
                else
                {
                    GameUIUtility.SetGrayShader(confirmBtn.image);
                }
            }
            else
            {
                price.text.text = I18N.Text("#SysComDesc525");
                var collectTs = pack.GetNextCollectTs();
                var cd = (long)Mathf.Max(collectTs - Game.Instance.GetTimestampSeconds());
                if (cd > 0)
                {
                    price.gameObject.SetActive(false);
                    time.gameObject.SetActive(true);
                    GameUIUtility.SetGrayShader(confirmBtn.image);
                    time.text.text = I18N.FormatText("#SysComDesc526", UIUtility.CountDownFormat(cd));
                    confirmBtn.interactable = false;
                    btnConfirmAnim.SetBool("CanClick", false);
                    btnConfirmEfx.SetActive(false);
                }
                else
                {
                    btnConfirmAnim.enabled = true;
                    btnConfirmAnim.SetBool("CanClick", true);
                    btnConfirmEfx.SetActive(true);
                    confirmBtn.interactable = true;
                    GameUIUtility.SetDefaultShader(confirmBtn.image);
                }
            }
            if ((buyCount == 1 && Game.TimestampNow() >= pack.GetNextCollectTs()) || buyCount > 1)
            {
                pack.Visual.Refresh(bg2, "bg2");
                desc2.Select(0);
                RefreshItemText(rewardList2, 1, pack.freeGoods1.Count);
            }
            else
            {
                RefreshItemText(rewardList2, 0, pack.freeGoods1.Count);
                desc2.Select(1);
                pack.Visual.Refresh(bg2, "bg1");
            }
            if ((buyCount == 2 && Game.TimestampNow() >= pack.GetNextCollectTs()) || buyCount > 2)
            {
                RefreshItemText(rewardList3, 1, pack.freeGoods2.Count);
                desc2.Select(1);
                desc3.Select(0);
                pack.Visual.Refresh(bg3, "bg2");
            }
            else
            {
                RefreshItemText(rewardList3, 0, pack.freeGoods2.Count);
                desc3.Select(1);
                pack.Visual.Refresh(bg3, "bg1");
            }
        }

        private void RefreshItemText(List<UICommonItem> l, int idx, int count)
        {
            for (int i = 0; i < l.Count; i++)
            {
                if (i < count)
                {
                    var text = l[i].transform.GetChild(2).GetComponent<UITextState>();
                    text.Select(idx);
                }
            }
        }

        internal override void PurchaseComplete(IList<RewardCommitData> list_)
        {
            Pack.ResetPopup();
            var buyCount = pack.BuyCount;
            for (var n = 0; n < list_.Count; ++n)
            {
                var pos = rewardList1[n].transform.position;
                UIFlyUtility.FlyReward(list_[n], pos);
            }
            if (!Game.Manager.specialRewardMan.CheckCanClaimSpecialReward())
            {
                RefreshGroup();
                mask1.SetTrigger("Punch");
            }
            else
            {
                isDelayRefresh = true;
            }
        }

        private IEnumerator DelayClose()
        {
            yield return new WaitForSeconds(closeDelay);
            Close();
        }

        internal void ConfirmClick(IAPPack content_)
        {
            var valid = Game.Manager.iap.Initialized;
            if (valid)
            {
                TryPurchaseOrGetFreeReward(content_);
            }
        }

        internal override void ConfirmClick()
        {
            TryPurchaseOrGetFreeReward();
            var buyCount = pack.BuyCount;
            Game.Manager.activity.giftpack.Purchase(pack, pack.Content, WhenPurchase);
        }

        private void TryPurchaseOrGetFreeReward(IAPPack content_ = null)
        {
            var buyCount = pack.BuyCount;
            if (buyCount == 0)
            {
                btnConfirmAnim.enabled = false;
                content_ = content_ == null ? pack.Content : content_;
                Game.Manager.activity.giftpack.Purchase(pack, content_, WhenPurchase);
            }
            else
            {
                using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
                {
                    var list = buyCount == 1 ? rewardList2 : rewardList3;
                    if (pack.CollectFreeReward(rewards))
                    {

                        for (var n = 0; n < rewards.Count; ++n)
                        {
                            var pos = list[n].transform.position;
                            UIFlyUtility.FlyReward(rewards[n], pos);
                        }
                        pack.ResetPopup();
                        RefreshGroup();
                        if (pack.BuyCount > 2 || expire)
                        {
                            mask3.SetTrigger("Punch");

                            StartCoroutine(DelayClose());
                        }
                        else
                        {
                            mask2.SetTrigger("Punch");
                        }
                    }
                }
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