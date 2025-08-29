/**
 * @Author: zhangpengjian
 * @Date: 2025/4/21 18:21:54
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/21 18:21:54
 * Description: 周任务
 */

using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using UnityEngine;
using static FAT.RecordStateHelper;

namespace FAT
{
    using static PoolMapping;
    public class ActivityWeeklyTask : ActivityLike, IBoardEntry
    {
        public class WeeklyTask
        {
            public EventWeeklyTaskInfo conf;
            public Config.RewardConfig reward;
            public int require;
            public int value;
            public bool complete;
        }

        public EventWeeklyTask Conf;
        public EventWeeklyTaskDetail DetailConf;
        private const int PHASE_COUNT = 4;
        #region Data
        private int grpId;
        private int levelRate;
        public int finalGet;
        public int[] phaseStates = new int[PHASE_COUNT];
        public int[] phasePop = new int[PHASE_COUNT];
        #endregion

        public List<List<WeeklyTask>> phaseTasks = new List<List<WeeklyTask>>();
        public VisualPopup VisualEnd { get; } = new(UIConfig.UIActivityWeeklyTaskEnd);
        public VisualPopup VisualMain { get; } = new(UIConfig.UIActivityWeeklyTaskMain);
        public override ActivityVisual Visual => VisualMain.visual;
        private List<int> phaseCompleteList = new List<int>();

        public ActivityWeeklyTask(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = fat.conf.Data.GetEventWeeklyTask(lite_.Param);
            MessageCenter.Get<MSG.GAME_COIN_USE>().AddListener(WhenCoinUse);
            MessageCenter.Get<MSG.GAME_COIN_ADD>().AddListener(WhenCoinAdd);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_MERGE>().AddListener(WhenBoardMerge);
            MessageCenter.Get<MSG.ORDER_FINISH>().AddListener(WhenOrderFinish);
            MessageCenter.Get<MSG.GAME_CARD_DRAW_FINISH>().AddListener(WhenCardDrawFinish);
            MessageCenter.Get<MSG.GAME_MERGE_PRE_BEGIN_REWARD>().AddListener(WhenTokenGet);
            MessageCenter.Get<MSG.GAME_MINE_BOARD_TOKEN_CHANGE>().AddListener(WhenTokenChange);
            MessageCenter.Get<MSG.ON_USE_SPEED_UP_ITEM_SUCCESS>().AddListener(WhenUnleashBubble);
        }

        public override void WhenReset()
        {
            RemoveListeners();
        }

        private void RemoveListeners()
        {
            MessageCenter.Get<MSG.GAME_COIN_USE>().RemoveListener(WhenCoinUse);
            MessageCenter.Get<MSG.GAME_COIN_ADD>().RemoveListener(WhenCoinAdd);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_MERGE>().RemoveListener(WhenBoardMerge);
            MessageCenter.Get<MSG.ORDER_FINISH>().RemoveListener(WhenOrderFinish);
            MessageCenter.Get<MSG.GAME_CARD_DRAW_FINISH>().RemoveListener(WhenCardDrawFinish);
            MessageCenter.Get<MSG.GAME_MERGE_PRE_BEGIN_REWARD>().RemoveListener(WhenTokenGet);
            MessageCenter.Get<MSG.GAME_MINE_BOARD_TOKEN_CHANGE>().RemoveListener(WhenTokenChange);
            MessageCenter.Get<MSG.ON_USE_SPEED_UP_ITEM_SUCCESS>().RemoveListener(WhenUnleashBubble);
        }

        public override void WhenActive(bool new_)
        {
            if (!new_)
            {
                return;
            }
            VisualMain.Popup();
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (IsComplete())
            {
                return;
            }
            VisualMain.Popup(popup_, state_, limit_: 1);
        }

        public override void WhenEnd()
        {
            RemoveListeners();
            TryClaimReward();
        }

        private void TryClaimReward()
        {
            var rewardMap = new Dictionary<int, RewardCommitData>(); // rewardId -> RewardCommitData

            // 检查每个阶段的任务完成情况
            for (int phase = 0; phase < PHASE_COUNT; phase++)
            {
                bool phaseCompleted = true;
                foreach (var task in phaseTasks[phase])
                {
                    if (!task.complete)
                    {
                        phaseCompleted = false;
                        break;
                    }
                }
                // 如果该阶段所有任务都完成且还未领取奖励
                if (phaseCompleted && phaseStates[phase] == 0)
                {
                    // 标记该阶段已领取
                    phaseStates[phase] = 1;
                    // 获取该阶段的奖励配置
                    var taskGroup = fat.conf.Data.GetEventWeeklyTaskGrp(DetailConf.TaskGroup[phase]);
                    for (int i = 0; i < taskGroup.GrpReward.Count; i++)
                    {
                        var phaseReward = taskGroup.GrpReward[i].ConvertToRewardConfig();
                        var reward = Game.Manager.rewardMan.BeginReward(phaseReward.Id, phaseReward.Count, ReasonString.weekly_task);

                        // 合并奖励
                        if (rewardMap.TryGetValue(reward.rewardId, out var existingReward))
                        {
                            existingReward.rewardCount += reward.rewardCount;
                        }
                        else
                        {
                            rewardMap[reward.rewardId] = reward;
                        }
                    }


                    DataTracker.event_weeklytask_stage.Track(this, phase + 1, DetailConf.TaskGroup.Count, DetailConf.Diff, 1, phase == DetailConf.TaskGroup.Count - 1);
                }
            }
            var isAllComplete = true;
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                if (!IsPhaseComplete(i))
                {
                    isAllComplete = false;
                    break;
                }
            }
            if (isAllComplete && finalGet == 0)
            {
                for (int i = 0; i < DetailConf.FinalReward.Count; i++)
                {
                    var finalReward = DetailConf.FinalReward[i].ConvertToRewardConfig();
                    var reward = Game.Manager.rewardMan.BeginReward(finalReward.Id, finalReward.Count, ReasonString.weekly_task);
                    // 合并最终奖励
                    if (rewardMap.TryGetValue(reward.rewardId, out var existingReward))
                    {
                        existingReward.rewardCount += reward.rewardCount;
                    }
                    else
                    {
                        rewardMap[reward.rewardId] = reward;
                    }
                }

                DataTracker.event_weeklytask_final.Track(this, DetailConf.TaskGroup.Count, DetailConf.Diff, 1);
            }
            var r = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);

            // 将合并后的奖励添加到列表
            rewardList.AddRange(rewardMap.Values);

            // 发放奖励
            if (rewardList.Count > 0)
            {
                Game.Manager.screenPopup.TryQueue(VisualEnd.popup, PopupType.Login, r);
            }
            else
            {
                r.Free();
            }
        }

        public void ClaimFinalReward(Vector3 pos)
        {
            if (IsComplete()) return;
            finalGet = 1;
            var rewards = new List<RewardCommitData>();
            for (int i = 0; i < DetailConf.FinalReward.Count; i++)
            {
                var finalReward = DetailConf.FinalReward[i].ConvertToRewardConfig();
                var reward = Game.Manager.rewardMan.BeginReward(finalReward.Id, finalReward.Count, ReasonString.weekly_task);
                rewards.Add(reward);
            }
            UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyTaskReward, pos, rewards, DetailConf.FinalRwdImg, this, true);
            DataTracker.event_weeklytask_final.Track(this, DetailConf.TaskGroup.Count, DetailConf.Diff, 1);
        }

        public void ClaimStageReward(int phase_, Vector3 pos)
        {
            bool phaseCompleted = true;
            foreach (var task in phaseTasks[phase_])
            {
                if (!task.complete)
                {
                    phaseCompleted = false;
                    break;
                }
            }

            // 如果该阶段所有任务都完成且还未领取奖励
            if (!phaseCompleted || phaseStates[phase_] == 1)
            {
                return;
            }
            phaseStates[phase_] = 1;
            var taskGroup = fat.conf.Data.GetEventWeeklyTaskGrp(DetailConf.TaskGroup[phase_]);
            var rewards = new List<RewardCommitData>();
            for (int i = 0; i < taskGroup.GrpReward.Count; i++)
            {
                var phaseReward = taskGroup.GrpReward[i].ConvertToRewardConfig();
                //随机宝箱
                var reward = Game.Manager.rewardMan.BeginReward(phaseReward.Id, phaseReward.Count, ReasonString.weekly_task);
                rewards.Add(reward);
            }
            UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyTaskReward, pos, rewards, taskGroup.GrpRwdImg, this, false);
            DataTracker.event_weeklytask_stage.Track(this, phase_ + 1, DetailConf.TaskGroup.Count, DetailConf.Diff, 1, phase_ == DetailConf.TaskGroup.Count - 1);
        }

        private void WhenUnleashBubble()
        {
            UpdateValue(WeeklyTaskType.TaskBubble, 1);
        }

        private void WhenTokenChange(int tokenNum, int tokenId)
        {
            if (tokenNum > 0)
            {
                return;
            }
            UpdateValueByToken(Mathf.Abs(tokenNum), tokenId);
        }

        private void WhenTokenGet(RewardCommitData data_)
        {
            // 检查是否有任务类型为TaskTokenGet的任务
            bool hasTokenGetTask = false;
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                for (int j = 0; j < phaseTasks[i].Count; j++)
                {
                    if (phaseTasks[i][j].conf.TaskType == (int)WeeklyTaskType.TaskTokenGet && data_.rewardId == phaseTasks[i][j].conf.TokenId)
                    {
                        hasTokenGetTask = true;
                        break;
                    }
                }
                if (hasTokenGetTask) break;
            }

            if (hasTokenGetTask)
            {
                UpdateValueByToken(data_.rewardCount, data_.rewardId);
            }
        }

        private void UpdateValueByToken(int tokenNum, int tokenId)
        {
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                for (int j = 0; j < phaseTasks[i].Count; j++)
                {
                    if (phaseTasks[i][j].conf.TokenId == tokenId && !phaseTasks[i][j].complete)
                    {
                        var pre = phaseTasks[i][j].value;
                        phaseTasks[i][j].value += tokenNum;
                        CheckComplete(i, j, pre);
                    }
                }
            }
        }

        private void WhenOrderFinish()
        {
            UpdateValue(WeeklyTaskType.TaskOrder, 1);
        }

        private void WhenCardDrawFinish()
        {
            UpdateValue(WeeklyTaskType.TaskCardPack, 1);
        }

        private void WhenCoinUse(CoinChange change_)
        {
            if (change_.type == CoinType.MergeCoin && change_.reason == ReasonString.undo_sell_item)
            {
                UpdateValue(WeeklyTaskType.TaskCoin, -change_.amount);
            }
        }

        private void WhenCoinAdd(CoinChange change_)
        {
            if (change_.type == CoinType.MergeCoin) UpdateValue(WeeklyTaskType.TaskCoin, change_.amount);
        }

        private void WhenBoardMerge(Merge.Item t_)
        {
            var b = t_?.world?.activeBoard;
            if (b == null || b.boardId != Constant.MainBoardId) return;
            UpdateValue(WeeklyTaskType.TaskMerge, 1);
        }

        private void UpdateValue(WeeklyTaskType type_, int value_)
        {
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                for (int j = 0; j < phaseTasks[i].Count; j++)
                {
                    if (phaseTasks[i][j].conf.TaskType == (int)type_ && !phaseTasks[i][j].complete)
                    {
                        var pre = phaseTasks[i][j].value;
                        phaseTasks[i][j].value += value_;
                        CheckComplete(i, j, pre);
                    }
                }
            }
        }

        public void DebugCompleteTaskById(int id_)
        {
            // 遍历所有阶段和任务，找到匹配ID的任务
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                for (int j = 0; j < phaseTasks[i].Count; j++)
                {
                    if (phaseTasks[i][j].conf.Id == id_ && !phaseTasks[i][j].complete)
                    {
                        // 设置任务完成所需的值
                        phaseTasks[i][j].value = phaseTasks[i][j].require;
                        // 检查任务完成状态
                        CheckComplete(i, j, 0);
                        return;
                    }
                }
            }
        }

        public void DebugCompleteTask()
        {
            // 遍历所有阶段和任务，找到第一个未完成的任务
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                for (int j = 0; j < phaseTasks[i].Count; j++)
                {
                    if (!phaseTasks[i][j].complete)
                    {
                        // 设置任务完成所需的值
                        phaseTasks[i][j].value = phaseTasks[i][j].require;
                        // 检查任务完成状态
                        CheckComplete(i, j, 0);
                        return;
                    }
                }
            }
        }

        public (int, int) GetPhaseProgress(int phase_)
        {
            int progress = 0;
            var total = fat.conf.Data.GetEventWeeklyTaskGrp(DetailConf.TaskGroup[phase_]).TaskList.Count;
            foreach (var task in phaseTasks[phase_])
            {
                if (task.complete)
                {
                    progress += 1;
                }
            }
            return (progress, total);
        }

        private void CheckComplete(int phase_, int index_, int pre)
        {
            if (phaseTasks[phase_][index_].value >= phaseTasks[phase_][index_].require && !phaseTasks[phase_][index_].complete)
            {
                //不溢出
                phaseTasks[phase_][index_].value = phaseTasks[phase_][index_].require;
                phaseTasks[phase_][index_].complete = true;
                if (UIManager.Instance.IsIdleIn(UIConfig.UIMergeBoardMain))
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyTaskNotice, phaseTasks[phase_][index_], UIManager.Instance.IsIdleIn(UIConfig.UIMiniBoardMulti), pre);
                }
                var total = fat.conf.Data.GetEventWeeklyTaskGrp(DetailConf.TaskGroup[phase_]).TaskList.Count;
                var (progress, _) = GetPhaseProgress(phase_);
                phaseCompleteList.Clear();
                var phaseCompleted = true;
                var isAllFinished = true;
                for (int phase = 0; phase < PHASE_COUNT; phase++)
                {
                    phaseCompleted = true;
                    foreach (var task in phaseTasks[phase])
                    {
                        if (!task.complete)
                        {
                            phaseCompleted = false;
                            isAllFinished = false;
                            break;
                        }
                    }
                    if (phaseCompleted && phaseStates[phase] == 0)
                    {
                        phaseCompleteList.Add(phase);
                    }
                }
                if (phaseCompleteList.Count > 0 && phaseCompleteList.Contains(phase_) && phasePop[phase_] == 0)
                {
                    DataTracker.event_weeklytask_task.Track(this, phase_ + 1, progress, phaseTasks[phase_][index_].conf.Sort, total, DetailConf.Diff, true, isAllFinished);
                    UIManager.Instance.RegisterIdleAction("ui_idle_weeklytask_main", 90, () => { UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyTaskMain, this, phaseCompleteList); });
                    phasePop[phase_] = 1;
                }
                else
                {
                    DataTracker.event_weeklytask_task.Track(this, phase_ + 1, progress, phaseTasks[phase_][index_].conf.Sort, total, DetailConf.Diff, false, isAllFinished);
                }
            }
        }

        private void SetupTheme()
        {
            VisualEnd.Setup(Conf.ResultTheme, this, active_: false);
            VisualMain.Setup(Conf.EventTheme, this);
        }

        public override void SetupFresh()
        {
            grpId = Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.Detail);
            DetailConf = fat.conf.Data.GetEventWeeklyTaskDetail(grpId);
            levelRate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                phaseTasks.Add(new List<WeeklyTask>());
            }
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                var taskGroup = fat.conf.Data.GetEventWeeklyTaskGrp(DetailConf.TaskGroup[i]);
                for (int j = 0; j < taskGroup.TaskList.Count; j++)
                {
                    var c = fat.conf.Data.GetEventWeeklyTaskInfo(taskGroup.TaskList[j]);
                    phaseTasks[i].Add(new WeeklyTask()
                    {
                        conf = c,
                        reward = c.TaskReward.ConvertToRewardConfig(),
                        require = CalculateRequire(c.RequireParam),
                        value = 0,
                        complete = false
                    });
                }
            }
            SetupTheme();
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            grpId = ReadInt(0, any);
            levelRate = ReadInt(1, any);
            finalGet = ReadInt(2, any);
            DetailConf = fat.conf.Data.GetEventWeeklyTaskDetail(grpId);

            // 读取阶段状态
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                phaseStates[i] = ReadInt(i + 3, any);
            }

            for (int i = 0; i < PHASE_COUNT; i++)
            {
                phasePop[i] = ReadInt(i + 3 + PHASE_COUNT, any);
            }

            // 读取任务值
            int valueIndex = PHASE_COUNT * 2 + 3;

            for (int i = 0; i < PHASE_COUNT; i++)
            {
                phaseTasks.Add(new List<WeeklyTask>());
                var taskGroup = fat.conf.Data.GetEventWeeklyTaskGrp(DetailConf.TaskGroup[i]);
                for (int j = 0; j < taskGroup.TaskList.Count; j++)
                {
                    var c = fat.conf.Data.GetEventWeeklyTaskInfo(taskGroup.TaskList[j]);
                    var value = ReadInt(valueIndex++, any);
                    var require = CalculateRequire(c.RequireParam);
                    phaseTasks[i].Add(new WeeklyTask()
                    {
                        conf = c,
                        reward = c.TaskReward.ConvertToRewardConfig(),
                        require = require,
                        value = value,
                        complete = value >= require,
                    });
                }
            }
            SetupTheme();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, grpId));
            any.Add(ToRecord(1, levelRate));
            any.Add(ToRecord(2, finalGet));

            // 保存阶段状态
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                any.Add(ToRecord(i + 3, phaseStates[i]));
            }

            for (int i = 0; i < PHASE_COUNT; i++)
            {
                any.Add(ToRecord(i + 3 + PHASE_COUNT, phasePop[i]));
            }

            // 保存任务值
            int valueIndex = PHASE_COUNT * 2 + 3;
            for (int phase = 0; phase < PHASE_COUNT; phase++)
            {
                foreach (var task in phaseTasks[phase])
                {
                    any.Add(ToRecord(valueIndex++, task.value));
                }
            }
        }

        private int CalculateRequire(string param_)
        {
            var t = param_.ConvertToRewardConfig();
            return Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(t.Id, t.Count, levelRate);
        }

        public override void Open()
        {
            phaseCompleteList.Clear();
            bool phaseCompleted;
            for (int phase = 0; phase < PHASE_COUNT; phase++)
            {
                phaseCompleted = true;
                foreach (var task in phaseTasks[phase])
                {
                    if (!task.complete)
                    {
                        phaseCompleted = false;
                        break;
                    }
                }
                if (phaseCompleted && phaseStates[phase] == 0)
                {
                    phaseCompleteList.Add(phase);
                }
            }
            VisualMain.res.ActiveR.Open(this, phaseCompleteList);
        }

        public string BoardEntryAsset()
        {
            Visual.Theme.AssetInfo.TryGetValue("boardEntry", out var asset);
            return asset;
        }

        public bool BoardEntryVisible => !IsComplete();

        public override bool EntryVisible
        {
            get
            {
                return !IsComplete();
            }
        }


        public bool IsComplete()
        {
            for (int i = 0; i < PHASE_COUNT; i++)
            {
                if (phaseStates[i] == 0) return false;
            }
            if (finalGet == 0) return false;
            return true;
        }

        public bool IsPhaseGot(int phase_)
        {
            if (phaseStates[phase_] == 1) return true;
            return false;
        }

        public bool IsPhaseComplete(int phase_)
        {
            if (IsPhaseGot(phase_)) return true;
            if (IsPhaseCanGet(phase_)) return true;
            return false;
        }

        public bool IsPhaseCanGet(int phase_)
        {
            bool phaseCompleted = true;
            foreach (var task in phaseTasks[phase_])
            {
                if (!task.complete)
                {
                    phaseCompleted = false;
                    break;
                }
            }
            if (phaseCompleted && phaseStates[phase_] == 0)
            {
                return true;
            }
            return false;
        }

        public bool CheckIsShowRedPoint()
        {
            for (int phase = 0; phase < PHASE_COUNT; phase++)
            {
                bool phaseCompleted = true;
                foreach (var task in phaseTasks[phase])
                {
                    if (!task.complete)
                    {
                        phaseCompleted = false;
                        break;
                    }
                }
                if (phaseCompleted && phaseStates[phase] == 0)
                {
                    return true;
                }
            }
            var canGetFinalReward = true;
            for (int i = 0; i < phaseStates.Length; i++)
            {
                if (phaseStates[i] == 0)
                {
                    canGetFinalReward = false;
                    break;
                }
            }
            if (canGetFinalReward && finalGet == 0) return true;
            return false;
        }
    }

    public class WeeklyTaskEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => e;
        private readonly ListActivity.Entry e;
        private readonly ActivityWeeklyTask p;

        public WeeklyTaskEntry(ListActivity.Entry e_, ActivityWeeklyTask p_)
        {
            (e, p) = (e_, p_);
            e_.dot.SetActive(p.CheckIsShowRedPoint());
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            if (p.IsComplete())
            {
                e.obj.gameObject.SetActive(false);
            }
            return UIUtility.CountDownFormat(diff_);
        }
    }
}
