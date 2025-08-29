// ==================================================
// // File: UIBPTaskComplete.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动任务完成界面
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPTaskComplete : UIBase, INavBack
    {
        [SerializeField] private GameObject scrollView;
        public MBBPTaskItem taskItem;
        public GameObject privilege;

        private RectTransform _content;
        private List<MBBPTaskItem> _taskItemList = new(); // key: id
        private string taskItemKey = "bp_task_item";

        private UIVisualGroup visualGroup;

        private TextMeshProUGUI _subTitle;
        private TextMeshProUGUI _privilegeText;
        private UIImageRes _bg;
        private UIImageRes _titleBg;

        // 活动实例 
        private BPActivity _activity;
        private PoolMapping.Ref<List<BPActivity.BPTaskData>> _container;
        private bool _isRefresh;

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("", out visualGroup);
            transform.Access("Content/bg/rewards/Scroll View/Viewport/Content", out _content);
            transform.Access("Content/bg/rewards/root/subTitle", out _subTitle);
            transform.Access("Content/bg/rewards/root/privilege/Text", out _privilegeText);
        }

        private void AddButton()
        {
            transform.AddButton("Content/bg/title/close", OnClickClaimBtn).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/claimBtn", OnClickClaimBtn).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (BPActivity)items[0];
            _isRefresh = (bool)items[1];
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnPreOpen()
        {
            _subTitle.SetText(I18N.Text(_isRefresh ? "#SysComDesc1375" : "#SysComDesc1358"));
            var privilegeInfo = _activity.GetBpPackInfoByType(BPActivity.BPPurchaseType.Luxury).PrivilegeInfo;
            var tokenIcon = UIUtility.FormatTMPString(_activity.ConfD.ScoreId);
            _privilegeText.SetText($"{tokenIcon}{privilegeInfo * 1f / 10f}");

            // 填充任务数据
            _container = PoolMapping.PoolMappingAccess.Take(out List<BPActivity.BPTaskData> _);
            if (_isRefresh)
            {
                _activity.FillNewTaskList(_container);
            }
            else
            {
                _activity.FillUnClaimTaskList(_container);
            }

            // 特权是否显示
            privilege.SetActive(_activity.PurchaseState == BPActivity.BPPurchaseState.Luxury);

            RefreshTheme();

            PreparePool();

            var taskDataList = _container.obj;
            // 根据传入的逻辑层的数据 初始化生成 taskItem
            foreach (var taskData in taskDataList)
            {
                // 创建Item
                GameObjectPoolManager.Instance.CreateObject(taskItemKey, _content, obj =>
                {
                    obj.SetActive(true);
                    var item = obj.GetComponent<MBBPTaskItem>();
                    item.Init(_activity, taskData);
                    item.RefreshView(true);

                    _taskItemList.Add(item);
                });
            }
        }

        private void PreparePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(taskItemKey))
            {
                return;
            }

            GameObjectPoolManager.Instance.PreparePool(taskItemKey, taskItem.gameObject);
        }


        protected override void OnPostOpen()
        {
            // 任务刷新动画
            if (_isRefresh)
            {
                foreach (var task in _taskItemList)
                {
                    task.PlayTaskRefreshAnim();
                }
            }
        }

        protected override void OnPreClose()
        {
            _container.Free();
        }

        protected override void OnPostClose()
        {
            for (int i = _taskItemList.Count - 1; i >= 0; i--)
            {
                var item = _taskItemList[i];
                GameObjectPoolManager.Instance.ReleaseObject(taskItemKey, item.gameObject);
            }

            _taskItemList.Clear();
        }

        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            var visual = _activity.TaskRefreshPopup;
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

        private void OnClickClaimBtn()
        {
            // 判断是任务完成 还是 任务刷新
            if (!_isRefresh)
            {
                var rewardContainer = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);
                _activity.ClaimAllTaskReward(rewardContainer);
                if (rewardList.Count > 0)
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIBPReward, _activity, rewardContainer);
                }

                foreach (var taskData in _container.obj)
                {
                    MessageCenter.Get<UI_BP_TASK_COMPLETE>().Dispatch(taskData.Id);
                }
            }
            else
            {
                // 任务刷新 点击打开BP主界面
                _activity.Open();
            }

            Close();
        }

        public void OnNavBack()
        {
            OnClickClaimBtn();
        }
    }
}