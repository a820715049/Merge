/*
 * @Author: qun.chao
 * @Date: 2024-11-11 17:39:00
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using FAT.MSG;

namespace FAT
{
    public class UIPackGemThreeForOne : PackUI
    {
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtSubTitle;
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private UIImageRes bgTitle;
        [SerializeField] private UIImageRes bgCD;
        [SerializeField] private Button btnClose;
        [SerializeField] private Transform goodsRoot;
        [SerializeField] private GameObject goBlock;

        private Action WhenInit;
        internal override ActivityLike Pack => pack;
        private PackGemThreeForOne pack;
        internal override void ConfirmClick() { }

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(Close);
            transform.Access<Button>("Mask").onClick.AddListener(Close);
        }

        protected override void OnParse(params object[] items)
        {
            pack = items[0] as PackGemThreeForOne;
        }

        protected override void OnPreOpen()
        {
            SetBlock(false);

            pack.TryRefreshPackEndTIme();
            ForEachItem(item => item.InitOnPreOpen(pack));
            RefreshTheme();
            RefreshPack();
            RefreshPrice();
            RefreshCD();
            WhenInit ??= RefreshPrice;
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            WhenRefresh ??= p_ => { if (p_ == pack) RefreshPack(); };
            MessageCenter.Get<IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<ACTIVITY_UPDATE>().AddListener(OnMessageActivityUpdate);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<ACTIVITY_UPDATE>().RemoveListener(OnMessageActivityUpdate);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        protected override void OnPostClose()
        { }

        public override void RefreshTheme()
        {
            var visual = pack.Visual;
            visual.Refresh(bgTitle, "titleBg");
            visual.Refresh(bgCD, "cdBg");
            visual.Refresh(txtTitle, "mainTitle");
            visual.Refresh(txtSubTitle, "subTitle");
            visual.Refresh(txtCD, "cdText");
            ForEachItem(item => item.RefreshTheme());
        }

        private void SetBlock(bool b)
        {
            goBlock.SetActive(b);
        }

        private void RefreshPack()
        {
            ForEachItem(item => item.RefreshPack());
        }

        private new void RefreshCD()
        {
            var diff_ = pack.PackEndTIme - Game.Instance.GetTimestampSeconds();
            UIUtility.CountDownFormat(txtCD, diff_);
        }

        public void RefreshEnd() {}

        private void RefreshPrice()
        {
            ForEachItem(item => item.RefreshPrice());
        }

        private void ForEachItem(Action<UIPackGemThreeForOneItem> act)
        {
            var root = goodsRoot;
            var count = root.childCount;
            for (var i = 0; i < count ; ++i)
            {
                var item = root.GetChild(i).GetComponent<UIPackGemThreeForOneItem>();
                act?.Invoke(item);
            }
        }

        private void OnMessageActivityUpdate()
        {
            if (base.IsOpen() && pack != null)
            {
                if (pack.PackEndTIme <= 0)
                {
                    IEnumerator DelayClose()
                    {
                        SetBlock(true);
                        yield return new WaitForSeconds(closeDelay);
                        Close();
                        SetBlock(false);
                    }
                    StartCoroutine(DelayClose());
                }
            }
        }
    }
}