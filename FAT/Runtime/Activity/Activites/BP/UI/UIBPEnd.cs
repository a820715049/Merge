// ==================================================
// // File: UIBPEnd.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动结束界面
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPEnd : UIBase
    {
        [SerializeField] private GameObject scrollView;
        [SerializeField] private GameObject line;
        public MBBPCommonItem endItem;


        private RectTransform _content;
        private List<MBBPCommonItem> _endItemList = new(); // key: id
        private string endItemKey = "bp_end_item";
        private TextMeshProUGUI _btnTxt;
        private TextMeshProUGUI _desc1;
        private TextMeshProUGUI _desc2;
        private TextMeshProUGUI _subTitle;
        private UIVisualGroup visualGroup;


        // 活动实例 
        private BPActivity _activity;
        public bool giveUpBuy = false;

        private PoolMapping.Ref<List<RewardCommitData>> _claimContainer;
        private PoolMapping.Ref<List<(int, int)>> _previewAll;
        private PoolMapping.Ref<List<(int, int)>> _previewFree;

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("", out visualGroup);
            transform.Access("Content/bg/rewards/Scroll View/Viewport/Content", out _content);
            transform.Access("Content/bg/claimBtn/btnText", out _btnTxt);
            transform.Access("Content/bg/rewards/root/desc1", out _desc1);
            transform.Access("Content/bg/rewards/desc2", out _desc2);
            transform.Access("Content/bg/rewards/root/subTitle", out _subTitle);
        }

        private void AddButton()
        {
            transform.AddButton("Content/bg/title/close", OnClickClose).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/claimBtn", OnClickClaimBtn).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (BPActivity)items[0];
            var customParams = (object[])items[1];

            _claimContainer = (PoolMapping.Ref<List<RewardCommitData>>)customParams[0];
            _previewAll = (PoolMapping.Ref<List<(int, int)>>)customParams[1];
            _previewFree = (PoolMapping.Ref<List<(int, int)>>)customParams[2];
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            PreparePool();

            CheckUIState();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().AddListener(OnBpBuySuccess);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().RemoveListener(OnBpBuySuccess);
        }

        protected override void OnPostOpen()
        {
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
            for (int i = _endItemList.Count - 1; i >= 0; i--)
            {
                var item = _endItemList[i];
                GameObjectPoolManager.Instance.ReleaseObject(endItemKey, item.gameObject);
            }

            _endItemList.Clear();
            giveUpBuy = false;

            _previewAll.Free();
            _previewFree.Free();
        }

        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            var visual = _activity.EndPopup;
            visual.Refresh(visualGroup);
        }

        public void CheckUIState()
        {
            line.SetActive(true);

            // 没有购买付费
            if (!giveUpBuy && _activity.PurchaseState == BPActivity.BPPurchaseState.Free)
            {
                SwitchBuyState();
            }
            else
            {
                if (_claimContainer.obj.Count > 0)
                {
                    // 存在未领取的奖励 （里程碑 或 循环奖励）
                    SwitchClaimState();
                }
                else
                {
                    if (giveUpBuy)
                    {
                        Close();
                    }
                    else
                    {
                        SwitchEndState();
                    }
                }
            }
        }


        // 购买挽留状态
        private void SwitchBuyState()
        {
            for (int i = _endItemList.Count - 1; i >= 0; i--)
            {
                var item = _endItemList[i];
                GameObjectPoolManager.Instance.ReleaseObject(endItemKey, item.gameObject);
            }

            _endItemList.Clear();

            _btnTxt.SetText($"{_activity.GetIAPPriceByType(BPActivity.BPPurchaseType.End)}");
            _activity.EndPopup.visual.Refresh(_desc1, "desc1"); // 现在购买黄金通行证，你将获得

            // 根据传入的逻辑层的数据 初始化生成
            // 显示未领取的奖励 付费可领取的奖励 循环奖励
            foreach (var reward in _previewAll.obj)
            {
                // 创建Item
                GameObjectPoolManager.Instance.CreateObject(endItemKey, _content, obj =>
                {
                    obj.SetActive(true);
                    var item = obj.GetComponent<MBBPCommonItem>();
                    item.Setup(_activity, 152, 152);
                    item.item.Refresh(reward.Item1, reward.Item2);

                    _endItemList.Add(item);
                });
            }
        }

        // 奖励未领取状态
        private void SwitchClaimState()
        {
            _btnTxt.SetText(I18N.Text("#SysComBtn7"));
            _desc1.SetText(I18N.Text("#SysComDesc1372")); // 你有一些奖励忘了领取

            for (int i = _endItemList.Count - 1; i >= 0; i--)
            {
                var item = _endItemList[i];
                GameObjectPoolManager.Instance.ReleaseObject(endItemKey, item.gameObject);
            }

            _endItemList.Clear();

            var unClaimContainer =
                _activity.PurchaseState == BPActivity.BPPurchaseState.Free ? _previewFree : _previewAll;

            // 显示未领取的奖励 循环奖励
            foreach (var reward in unClaimContainer.obj)
            {
                // 创建Item
                GameObjectPoolManager.Instance.CreateObject(endItemKey, _content, obj =>
                {
                    obj.SetActive(true);
                    var item = obj.GetComponent<MBBPCommonItem>();
                    item.Setup(_activity, 152, 152);
                    item.item.Refresh(reward.Item1, reward.Item2);

                    _endItemList.Add(item);
                });
            }
        }

        // 活动结束状态
        private void SwitchEndState()
        {
            _btnTxt.SetText(I18N.Text("#SysComBtn3"));
            line.SetActive(false);
            scrollView.SetActive(false);
            _desc1.gameObject.SetActive(false);
            _desc2.gameObject.SetActive(true);
            _desc2.SetText(I18N.Text("#SysComDesc1373"));
        }

        private void PreparePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(endItemKey))
            {
                return;
            }

            GameObjectPoolManager.Instance.PreparePool(endItemKey, endItem.gameObject);
        }


        private void RefreshCd()
        {
        }


        private void OnClickClose()
        {
            if (_activity.PurchaseState == BPActivity.BPPurchaseState.Free && !giveUpBuy)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPDoubleCheck, _activity);
            }
            else
            {
                OnClickClaimBtn();
            }
        }

        private void OnClickClaimBtn()
        {
            if (_activity.PurchaseState == BPActivity.BPPurchaseState.Free && !giveUpBuy)
            {
                _activity.TryPurchase(BPActivity.BPPurchaseType.End);
                return;
            }
            else if (_claimContainer.obj.Count > 0) // 存在未领取的奖励 （里程碑 或 循环奖励）
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPReward, _activity, _claimContainer);
            }


            Close();
        }

        private void OnBpBuySuccess(BPActivity.BPPurchaseType type, PoolMapping.Ref<List<RewardCommitData>> container,
            bool late_)
        {
            if (type == BPActivity.BPPurchaseType.End)
            {
                // container 包含了付费奖励和循环奖励 
                // 需要合并显示 container + _claimContainer
                foreach (var commit in _claimContainer.obj)
                {
                    container.obj.Add(commit);
                }

                UIManager.Instance.OpenWindow(UIConfig.UIBPReward, _activity, container);
                _claimContainer.Free();
            }

            Close();
        }
    }
}