// ==================================================
// // File: UIBPBuyBoth.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动购买界面
// // ==================================================

using System.Collections.Generic;
using Config;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPBuyBoth : UIBase
    {
        private UIVisualGroup visualGroup;
        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _normalPrice;
        private TextMeshProUGUI _luxuryPrice;
        private RectTransform _tip;
        private RectTransform _best;
        private UICommonItem _normalReward;
        private UICommonItem _luxuryReward;

        // 活动实例 
        private BPActivity _activity;
        private readonly UIIAPLabel iapLabel = new();

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("", out visualGroup);
            transform.Access("Content/bg/Rewards/normal/claimBtn/price", out _normalPrice);
            transform.Access("Content/bg/Rewards/luxury/claimBtn/price", out _luxuryPrice);
            transform.Access("Content/bg/title/Tips", out _tip);
            transform.Access("Content/bg/Rewards/normal/content/rewardItem", out _normalReward);
            transform.Access("Content/bg/Rewards/luxury/content/rewardItem", out _luxuryReward);
            transform.Access("Content/bg/Rewards/luxury/content/best", out _best);
        }

        private void AddButton()
        {
            transform.AddButton("Content/bg/title/close", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/Rewards/normal/claimBtn", OnClickNormalClaim).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/Rewards/luxury/claimBtn", OnClickLuxuryClaim).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/title/Tips", OnClickTips).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/title/titleBg", OnClickTips);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (BPActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            _normalPrice.SetText(_activity.GetIAPPriceByType(BPActivity.BPPurchaseType.Normal));
            _luxuryPrice.SetText(_activity.GetIAPPriceByType(BPActivity.BPPurchaseType.Luxury));

            var config = _activity.GetMilestoneInfo(0);
            var info = config.RewardPay;
            _normalReward.Refresh(info[0].ConvertToRewardConfig());
            _luxuryReward.Refresh(info[0].ConvertToRewardConfig());

            iapLabel.Clear();
            var label = _activity.GetCurDetailConfig().Label;
            var packId = _activity.GetBpPackInfoByType(BPActivity.BPPurchaseType.Luxury).PackId;
            iapLabel.Setup(_best, label, packId);

            RefreshTheme();
        }


        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().AddListener(OnBpBuySuccess);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().RemoveListener(OnBpBuySuccess);
        }

        protected override void OnPostOpen()
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIBPMain);
            if (ui != null && ui is UIBPMain main)
            {
                // 里程碑跳转
                main.JumpToCurLvMilestone();
            }

            OnClickTips();
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
        }

        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            var visual = _activity.VisualBuyBoth;
            visual.Refresh(visualGroup);
        }

        private void RefreshCd()
        {
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is BPActivity)
            {
                Close();
            }
        }

        private void OnClickTips()
        {
            var rewardList = new List<RewardConfig>();

            var mileStones = _activity.GetCurDetailConfig().MileStones;
            foreach (var id in mileStones)
            {
                var config = Game.Manager.configMan.GetBpMilestoneConfig(id);
                if (config == null || !config.InGrandList)
                {
                    continue;
                }

                foreach (var reward in config.RewardPay)
                {
                    var info = reward.ConvertToInt3();

                    if (rewardList.Exists(x => x.Id == info.Item1))
                    {
                        rewardList.Find(x => x.Id == info.Item1).Count += info.Item2;
                    }
                    else
                    {
                        rewardList.Add(new RewardConfig()
                        {
                            Id = info.Item1,
                            Count = info.Item2
                        });
                    }
                }
            }

            UIManager.Instance.OpenWindow(UIConfig.UIBPRewardTip, _tip.position, 0f, _activity, rewardList, false);
        }

        private void OnClickNormalClaim()
        {
            _activity.TryPurchase(BPActivity.BPPurchaseType.Normal);
        }

        private void OnClickLuxuryClaim()
        {
            _activity.TryPurchase(BPActivity.BPPurchaseType.Luxury);
        }

        private void OnBpBuySuccess(BPActivity.BPPurchaseType type, PoolMapping.Ref<List<RewardCommitData>> container,
            bool late_)
        {
            if (type == BPActivity.BPPurchaseType.Normal)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPBuyOneSuccess, _activity);
            }
            else if (type == BPActivity.BPPurchaseType.Luxury)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPBuyTwoSuccess, _activity, container);
            }

            Close();
        }
    }
}