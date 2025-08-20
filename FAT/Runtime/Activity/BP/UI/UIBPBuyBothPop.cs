// ==================================================
// // File: UIBPBuyBothPop.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动 购买界面强制弹窗
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPBuyBothPop : UIBase
    {
        public GameObject popItem;

        private RectTransform _normalContent;
        private RectTransform _luxuryContent;

        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _normalPrice;
        private TextMeshProUGUI _luxuryPrice;
        private UIVisualGroup _visualGroup;

        private TextMeshProUGUI _subTitle;

        // 活动实例 
        private BPActivity _activity;
        private PoolMapping.Ref<List<(int, int)>> _container;
        private string popItemKey = "bp_pop_item";
        private List<UICommonItem> _popItemList = new();

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("", out _visualGroup);
            transform.Access("Content/bg/Rewards/normal/claimBtn/price", out _normalPrice);
            transform.Access("Content/bg/Rewards/luxury/claimBtn/price", out _luxuryPrice);

            transform.Access("Content/bg/title/subTitle_under", out _subTitle);
            transform.Access("Content/bg/title/desc", out _desc);

            transform.Access("Content/bg/Rewards/normal/content/Scroll View/Viewport/Content", out _normalContent);
            transform.Access("Content/bg/Rewards/luxury/content/Scroll View/Viewport/Content", out _luxuryContent);
        }

        private void AddButton()
        {
            transform.AddButton("Content/bg/title/close", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/Rewards/normal/claimBtn", OnClickNormalClaim).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/Rewards/luxury/claimBtn", OnClickLuxuryClaim).WithClickScale().FixPivot();
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
            RefreshTheme();

            var curLvIndex = _activity.GetCurMilestoneLevel();
            var mileStones = _activity.GetCurDetailConfig().MileStones;
            _subTitle.gameObject.SetActive(curLvIndex >= mileStones.Count - 2);

            _activity.SetCurLevelPopBuy();

            _normalPrice.SetText(_activity.GetIAPPriceByType(BPActivity.BPPurchaseType.Normal));
            _luxuryPrice.SetText(_activity.GetIAPPriceByType(BPActivity.BPPurchaseType.Luxury));

            // 显示所有奖励（normal + luxury）
            PreparePool();
            _container = PoolMapping.PoolMappingAccess.Take(out List<(int, int)> _);
            _activity.CollectAllCanClaimReward(BPActivity.RewardCollectType.Pay, _container);

            foreach (var data in _container.obj)
            {
                var id = data.Item1;
                var count = data.Item2;
                GameObjectPoolManager.Instance.CreateObject(popItemKey, _normalContent, obj =>
                {
                    obj.SetActive(true);
                    var commonItem = obj.GetComponent<UICommonItem>();
                    commonItem.Refresh(id, count);
                    _popItemList.Add(commonItem);
                });

                GameObjectPoolManager.Instance.CreateObject(popItemKey, _luxuryContent, obj =>
                {
                    obj.SetActive(true);
                    var commonItem = obj.GetComponent<UICommonItem>();
                    commonItem.Refresh(id, count);
                    _popItemList.Add(commonItem);
                });
            }
        }

        private void PreparePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(popItemKey))
            {
                return;
            }

            GameObjectPoolManager.Instance.PreparePool(popItemKey, popItem.gameObject);
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
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
            foreach (var item in _popItemList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(popItemKey, item.gameObject);
            }

            _popItemList.Clear();

            _container.Free();
        }

        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            var visual = _activity.VisualBuyBothPop;
            visual.Refresh(_visualGroup);
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