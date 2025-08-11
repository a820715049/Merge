/*
 * @Author: qun.chao
 * @Date: 2024-12-03 11:25:05
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
    public class UIPackEnergyMultiPack : PackUI
    {
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtSubTitle;
        [SerializeField] private TextMeshProUGUI txtTip;
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private UIImageRes bgTitle;
        [SerializeField] private UIImageRes bgTitle2;
        [SerializeField] private UIImageRes bgCD;
        [SerializeField] private Button btnClose;
        [SerializeField] private Transform goodsRoot;
        [SerializeField] private GameObject goBlock;
        [SerializeField] private Transform labelRoot;

        private Action WhenInit;
        internal override ActivityLike Pack => pack;
        private PackEnergyMultiPack pack;
        internal override void ConfirmClick() { }
        private UIIAPLabel iapLabel;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(Close);
            transform.Access<Button>("Mask").onClick.AddListener(Close);
        }

        protected override void OnParse(params object[] items)
        {
            pack = items[0] as PackEnergyMultiPack;
        }

        protected override void OnPreOpen()
        {
            SetBlock(false);

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
        {
            ClearLabel();
        }

        private void RefreshLabel()
        {
            ClearLabel();
            var lastPack = pack.LastPackId;
            var labelId = pack.LabelId;
            if (labelId > 0 && lastPack > 0)
            {
                iapLabel ??= new();
                iapLabel.Setup(labelRoot, labelId, lastPack);
            }
        }

        private void ClearLabel()
        {
            iapLabel?.Clear();
        }

        private new void RefreshCD()
        {
            var diff_ = pack.Countdown;
            UIUtility.CountDownFormat(txtCD, diff_);
        }

        public override void RefreshTheme()
        {
            var visual = pack.Visual;
            visual.Refresh(bgTitle, "titleBg");
            visual.Refresh(bgTitle2, "titleBg2");
            visual.Refresh(bgCD, "cdBg");
            visual.Refresh(txtTitle, "mainTitle");
            visual.Refresh(txtSubTitle, "subTitle");
            visual.Refresh(txtTip, "tipText");
            visual.Refresh(txtCD, "cdText");
            ForEachItem(item => item.RefreshTheme());
        }

        private void SetBlock(bool b)
        {
            goBlock.SetActive(b);
        }

        private void RefreshPack()
        {
            RefreshLabel();
            ForEachItem(item => item.RefreshPack());
        }

        public void RefreshEnd() {}

        private void RefreshPrice()
        {
            ForEachItem(item => item.RefreshPrice());
        }

        private void ForEachItem(Action<UIPackEnergyMultiPackItem> act)
        {
            var root = goodsRoot;
            var count = root.childCount;
            for (var i = 0; i < count ; ++i)
            {
                // 存在某些用来挂接标签的节点不是item
                if (root.GetChild(i).TryGetComponent<UIPackEnergyMultiPackItem>(out var item))
                    act?.Invoke(item);
            }
        }

        private void OnMessageActivityUpdate()
        {
            if (base.IsOpen() && pack != null)
            {
                if (pack.Countdown <= 0 || pack.Stock <= 0)
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