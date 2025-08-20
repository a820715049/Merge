// ================================================
// File: UIBPMain.cs
// Author: yueran.li
// Date: 2025/06/18 12:14:16 星期三
// Desc: BP 主界面
// ================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIBPMain : UIBase, INavBack
    {
        public UIVisualGroup visualGroup;
        [SerializeField] private UIBPMileStoneScrollRect fancyScroll;
        public UIBPMileStoneScrollRect FancyScroll => fancyScroll;

        // 任务
        private MBBPTask _task;
        public MBBPTask Task => _task;

        // cd
        private TextMeshProUGUI _cd;

        // 任务
        private RectTransform _taskBg;

        // 进度条
        private MBBPProgress _progress;
        public MBBPProgress Progress => _progress;
        private Animator _expAnimator;

        // 里程碑
        private RectTransform _mileStoneRoot;

        // 升级按钮
        private TextMeshProUGUI _packUpTxt;

        // block
        private NonDrawingGraphic _block;

        // 活动实例 
        private BPActivity _activity;
        public BPActivity Activity => _activity;
        private bool fromMerge = false;

        #region UI基础
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();

            fancyScroll.InitLayout();
        }

        private void RegisterComp()
        {
            transform.Access("block", out _block);
            transform.Access("Root/TaskBg", out _taskBg);
            transform.Access("Root/TaskBg/Task", out _task);
            transform.Access("Root/TaskBg/Task/cdBg/_cd/text", out _cd);
            transform.Access("Root/MileStoneBg/MileStone/", out _mileStoneRoot);
            transform.Access("Content/BottomBg/bg/packUpBtn/packUpTxt", out _packUpTxt);
            transform.Access("Root/TaskBg/Task/ProgressBg", out _progress);
            transform.Access("Root/TaskBg/Task/ProgressBg/ExpNode", out _expAnimator);
        }

        private void AddButton()
        {
            transform.AddButton("Content/titleBg/close", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/BottomBg/bg/packUpBtn", OnClickPackUp).WithClickScale().FixPivot();
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
            Game.Manager.screenPopup.Block(delay_: true);

            // 打开活动主界面时 参与活动
            _activity.SetJoinBp();

            // 刷新主题 换皮
            RefreshTheme();

            // 任务初始化
            Task.Initialize(_activity);

            // 进度条初始化
            _progress.Init(_activity);

            // 里程碑
            MileStoneInit();

            // 里程碑跳转
            var hasTaskComplete = _activity.CheckHasUnClaimTask();
            if (hasTaskComplete)
            {
                JumpToCurLvMilestone();
            }
            else
            {
                JumpToUncClaimMileStone();
            }
            RefreshCd();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().AddListener(FlyFeedBack);
            MessageCenter.Get<UI_BP_TASK_COMPLETE>().AddListener(Task.Sort);
            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().AddListener(OnBpBuySuccess);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().RemoveListener(FlyFeedBack);
            MessageCenter.Get<UI_BP_TASK_COMPLETE>().RemoveListener(Task.Sort);
            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().RemoveListener(OnBpBuySuccess);
        }

        protected override void OnPostOpen()
        {
            // 打开任务完成界面
            var hasTaskComplete = _activity.CheckHasUnClaimTask();
            if (hasTaskComplete) // 是否有完成的任务
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPTaskComplete, _activity, false); // false 表示不是任务刷新
            }

            // 是否需要打开购买强弹界面
            if (!hasTaskComplete && _activity.CheckCanPopBuy())
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPBuyBothPop, _activity);
            }

            fromMerge = UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain);
        }

        public void JumpToUncClaimMileStone()
        {
            var claim = TryGetMinUnClaimMileStoneID(out var uncClaimMileStoneID);
            if (claim)
            {
                var config = Game.Manager.configMan.GetBpMilestoneConfig(uncClaimMileStoneID);
                var jumpIndex = 0;

                if (config.ShowNum == 0)
                {
                    // 循环奖励 理论上不会进到这个条件 因为RewardClaimStateDict 中没有循环奖励
                    var mileStones = _activity.GetCurDetailConfig().MileStones;
                    jumpIndex = mileStones.Count + 1;
                }
                else if (config.ShowNum == 1)
                {
                    // 等级为1时 跳转到icon
                    jumpIndex = 0;
                }
                else
                {
                    jumpIndex = config.ShowNum;
                }

                fancyScroll.JumpToWithSort(jumpIndex);
            }
            else
            {
                JumpToCurLvMilestone();
            }
        }

        private bool TryGetMinUnClaimMileStoneID(out int mileStoneID)
        {
            mileStoneID = -1;
            if (_activity == null)
            {
                return false;
            }

            //Value: item1:免费奖励是否已领取  item2:付费奖励是否已领取  item3:当前id(等级)是否弹过购买弹窗
            var keyList = _activity.RewardClaimStateDict.Keys.ToList();
            keyList.Sort();
            foreach (var id in keyList)
            {
                // 免费奖励未领取
                if (!_activity.RewardClaimStateDict[id].Item1)
                {
                    if (_activity.CheckCanClaimReward(id, true))
                    {
                        mileStoneID = id;
                        return true;
                    }
                }

                // 付费奖励未领取
                if (!_activity.RewardClaimStateDict[id].Item2)
                {
                    if (_activity.CheckCanClaimReward(id, false))
                    {
                        mileStoneID = id;
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
            Task.Release();
            _progress.Release();

            if (IsBlock)
            {
                SetBlock(false);
            }

            Game.Manager.screenPopup.Block(false, false);
        }

        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            switch (_activity.PurchaseState)
            {
                case BPActivity.BPPurchaseState.Free:
                    _packUpTxt.text = I18N.Text("#SysComDesc53"); // 解锁
                    break;
                case BPActivity.BPPurchaseState.Normal:
                    _packUpTxt.text = I18N.Text("#SysComDesc19"); // 升级
                    break;
                case BPActivity.BPPurchaseState.Luxury:
                    _packUpTxt.text = I18N.Text("#SysComDesc609"); // 开始
                    break;
            }

            var visual = _activity.VisualMain;
            visual.Refresh(visualGroup);
        }
        #endregion

        #region 事件
        private void RefreshCd()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            _cd.SetCountDown(diff);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is BPActivity)
            {
                Close();
            }
        }

        private void FlyFeedBack(FlyableItemSlice slice)
        {
            if (slice.FlyType == FlyType.BPExp)
            {
                _expAnimator.SetTrigger("Punch");
                Game.Manager.audioMan.TriggerSound("AddEnergy");
            }
        }

        // 购买成功
        private void OnBpBuySuccess(BPActivity.BPPurchaseType type, PoolMapping.Ref<List<RewardCommitData>> container,
            bool late_)
        {
            if (_activity == null)
            {
                return;
            }

            switch (_activity.PurchaseState)
            {
                case BPActivity.BPPurchaseState.Free:
                    _packUpTxt.text = I18N.Text("#SysComDesc53"); // 解锁
                    break;
                case BPActivity.BPPurchaseState.Normal:
                    _packUpTxt.text = I18N.Text("#SysComDesc19"); // 升级
                    break;
                case BPActivity.BPPurchaseState.Luxury:
                    _packUpTxt.text = I18N.Text("#SysComDesc609"); // 开始
                    break;
            }
        }
        #endregion

        #region 里程碑
        List<BPMileStoneCellViewData> milestones = new(25);

        // 里程碑初始化
        private void MileStoneInit()
        {
            var milestoneConfig = _activity.GetCurDetailConfig()?.MileStones;
            if (milestoneConfig == null)
            {
                return;
            }

            var curLvIndex = _activity.GetCurMilestoneLevel();

            milestones.Clear();
            milestones.Add(new BPMileStoneCellViewData() { IsIcon = true, Activity = _activity, });

            // 添加里程碑 最后一个是循环奖励 减去一个
            for (var i = 0; i < milestoneConfig.Count - 1; i++)
            {
                var config = _activity.GetMilestoneInfo(i);
                var (normalClaim, luxuryClaim, _) = _activity.RewardClaimStateDict[config.Id];
                var lv = config.ShowNum;

                using (ObjectPool<BPMileStoneCellViewData>.GlobalPool.AllocStub(out var viewData))
                {
                    viewData = new()
                    {
                        ShowNum = lv,
                        Activity = _activity,
                        Config = config,
                        ProgressViewLv = curLvIndex + 1,
                    };

                    if (viewData.ShowNum > curLvIndex + 1)
                    {
                        viewData.FreeCellViewState = BPMileStoneCellViewData.UIBpCellViewState.UnAchieve;
                        viewData.LuxuryCellViewState = BPMileStoneCellViewData.UIBpCellViewState.UnAchieve;
                    }
                    else
                    {
                        viewData.FreeCellViewState = normalClaim
                            ? BPMileStoneCellViewData.UIBpCellViewState.Claimed
                            : BPMileStoneCellViewData.UIBpCellViewState.Achieved;

                        viewData.LuxuryCellViewState = luxuryClaim
                            ? BPMileStoneCellViewData.UIBpCellViewState.Claimed
                            : BPMileStoneCellViewData.UIBpCellViewState.Achieved;
                    }

                    milestones.Add(viewData);
                }
            }

            milestones.Add(new BPMileStoneCellViewData() { IsCycle = true, Activity = _activity });
            milestones.Add(new BPMileStoneCellViewData() { IsEmpty = true });
            RefreshMileStone(milestones);
        }

        private void RefreshMileStone(List<BPMileStoneCellViewData> milestones)
        {
            fancyScroll.UpdateData(milestones);
        }

        public void JumpToCurLvMilestone()
        {
            var curLvIndex = _activity.GetCurMilestoneLevel();

            // 等级1的时候 跳转到icon
            if (curLvIndex == 0)
            {
                curLvIndex = -1;
            }
            else if (_activity.CheckMilestoneCycle())
            {
                fancyScroll.JumpToBottom();
                return;
            }

            fancyScroll.JumpToWithSort(curLvIndex + 1);
        }
        #endregion

        #region 升级按钮
        // 点击升级按钮
        private void OnClickPackUp()
        {
            var curState = _activity.PurchaseState;
            // 没有付费
            if (curState == BPActivity.BPPurchaseState.Free)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPBuyBoth, _activity); // 购买界面
            }
            // 购买了付费一
            else if (curState == BPActivity.BPPurchaseState.Normal)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPBuyUpgrade, _activity); // 升级界面
            }
            // 购买了付费二
            else if (curState == BPActivity.BPPurchaseState.Luxury)
            {
                // 关闭界面
                Close();

                // meta界面打开时 关闭跳转主棋盘
                if (!fromMerge)
                {
                    GameProcedure.SceneToMerge();
                }
            }
        }
        #endregion

        public bool IsBlock => _block.raycastTarget;

        public void SetBlock(bool value)
        {
            _block.raycastTarget = value;
        }

        public void OnNavBack()
        {
            if (IsBlock)
            {
                return;
            }

            Close();
        }
    }
}