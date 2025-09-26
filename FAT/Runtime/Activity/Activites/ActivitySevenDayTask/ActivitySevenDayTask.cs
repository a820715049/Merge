using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;
using com.fpnn.proto;
using Cysharp.Text;
using EL;
using fat.conf;
using fat.gamekitdata;
using fat.rawdata;
using FAT.MSG;

namespace FAT
{
    using static PoolMapping;
    public class ActivitySevenDayTask : ActivityLike, IBoardEntry
    {
        #region 存档
        private int _detailID;
        private int _tokenNum;
        private int _taskCount;
        private Dictionary<int, int> _taskInfos = new();
        private List<int> _completeTask = new();
        private List<int> _hasUnlockDay = new();
        private List<int> _waitComplete = new();
        #endregion

        #region 运行时
        public EventSevenDayTask eventConfig;
        public SevenDayTaskDetail detailConfig;
        public List<RewardCommitData> milestoneRewardCommitData = new();
        public List<RewardCommitData> taskRewardCommitDatas = new();
        public DateTime startDay => Game.TimeOf(startTS);
        public Dictionary<int, int> taskInfos => _taskInfos;
        public int TokenNum => _tokenNum;
        public bool hasFinishReward;
        private bool _hasPopup;
        private bool _hasEnd;
        private bool _waitPop;
        public override bool Valid => !_hasEnd;
        #endregion

        #region UI
        public VisualPopup mainPopup = new(UIConfig.UISevenDayTaskPanel);
        public VisualPopup visualEndPanel = new(UIConfig.UISevenDayTaskEnd);
        #endregion

        public override void Open()
        {
            UIManager.Instance.OpenWindow(mainPopup.res.ActiveR, this);
        }

        public ActivitySevenDayTask(ActivityLite lite)
        {
            Lite = lite;
            eventConfig = EventSevenDayTaskVisitor.Get(Lite.Param);
            mainPopup.Setup(eventConfig.EventTheme, this);
            visualEndPanel.Setup(eventConfig.ResultTheme, this, active_: false);
            MessageCenter.Get<TASK_UPDATE>().AddListener(WhenUpdate);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WaitPop);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (!_hasPopup)
            {
                popup_.TryQueue(mainPopup.popup, state_);
            }
        }

        public override void SetupFresh()
        {
            Game.Manager.taskMan.RegisterTaskGroup(this);
            _InitDetail();
            _InitTaskInfos();
            _hasPopup = true;
            _waitPop = true;
        }

        public void WaitPop()
        {
            if (!_waitPop) return;
            if (Game.Manager.mergeBoardMan.activeWorld == null) return;
            if (Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId != Constant.MainBoardId) return;
            mainPopup.Popup();
            _waitPop = false;
        }

        public override void WhenEnd()
        {
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            using var _ = PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            for (var i = 0; i < detailConfig.TaskGroup.Count; i++)
            {
                var conf = SevenDayTaskGroupVisitor.Get(detailConfig.TaskGroup[i]);
                foreach (var id in conf.TaskInfo)
                {
                    if (CheckTaskCanComplete(id))
                    {
                        var config = SevenDayTaskInfoVisitor.Get(id);
                        foreach (var str in config.TaskReward)
                        {
                            var reward = str.ConvertToRewardConfig();
                            if (map.ContainsKey(reward.Id)) { map[reward.Id] += reward.Count; }
                            else { map.Add(reward.Id, reward.Count); }
                        }
                        _completeTask.Add(id);
                        DataTracker.event_7daytask_claim.Track(this, detailConfig.TaskGroup.IndexOf(conf.Id) + 1, conf.TaskInfo.IndexOf(id) + 1, _tokenNum,
                        detailConfig.Diff, config.TaskReward[1].ConvertToInt3().Item1, _completeTask.Count == detailConfig.TaskGroup.Count * conf.TaskInfo.Count);
                    }
                }
            }
            foreach (var info in map)
            {
                if (info.Key == eventConfig.TokenId) { _tokenNum += info.Value; }
            }
            map.Remove(eventConfig.TokenId);
            for (var i = phase; i < detailConfig.PointsRwd.Count; i++)
            {
                var mile = SevenDayTaskRwdVisitor.Get(detailConfig.PointsRwd[i]);
                if (_tokenNum >= mile.Points)
                {
                    foreach (var str in mile.Reward)
                    {
                        var reward = str.ConvertToRewardConfig();
                        if (map.ContainsKey(reward.Id)) { map[reward.Id] += reward.Count; }
                        else { map.Add(reward.Id, reward.Count); }
                    }
                    phase++;
                    DataTracker.event_7daytask_milestone.Track(this, phase, detailConfig.PointsRwd.Count, _tokenNum, detailConfig.Diff, mile.Id, phase == detailConfig.PointsRwd.Count);
                }
            }
            foreach (var info in map)
            {
                list.Add(Game.Manager.rewardMan.BeginReward(info.Key, info.Value, ReasonString.seven_day_task));
            }
            if (list.Count > 0) { Game.Manager.screenPopup.Queue(visualEndPanel.popup, listT); }
            Game.Manager.taskMan.UnRegisterTaskGroup(this);
            MessageCenter.Get<TASK_UPDATE>().RemoveListener(WhenUpdate);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WaitPop);
            _hasEnd = true;
        }

        public override void WhenReset()
        {
            MessageCenter.Get<TASK_UPDATE>().RemoveListener(WhenUpdate);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WaitPop);
        }

        private void WhenUpdate(TaskType type, int count)
        {
            foreach (var groupID in detailConfig.TaskGroup)
            {
                var group = SevenDayTaskGroupVisitor.Get(groupID);
                foreach (var id in group.TaskInfo)
                {
                    if (CheckTaskCanComplete(id))
                    {
                        if (_waitComplete.Contains(id)) continue;
                        DataTracker.event_7daytask_task.Track(this, detailConfig.TaskGroup.IndexOf(group.Id) + 1, group.TaskInfo.IndexOf(id) + 1, id, _tokenNum, detailConfig.Diff, _completeTask.Count == detailConfig.TaskGroup.Count * group.TaskInfo.Count);
                        _waitComplete.Add(id);
                    }
                }
            }
            MessageCenter.Get<SEVEN_DAY_TASK_UPDATE>().Dispatch();
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            _LoadNormal(data_);
            _LoadTask(data_);
            _LoadComplete(data_);
            _LoadUnlockDay(data_);
            _LoadWaitComplete(data_);
            _InitDetail();
        }

        private void _LoadNormal(ActivityInstance data_)
        {
            _detailID = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _tokenNum = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _taskCount = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
        }

        private void _LoadTask(ActivityInstance data_)
        {
            dataIndex = 100;
            for (var i = 0; i < _taskCount; i++)
            {
                var id = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
                var target = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
                _taskInfos.Add(id, target);
            }
        }

        private void _LoadComplete(ActivityInstance data_)
        {
            dataIndex = 500;
            while (RecordStateHelper.ReadInt(dataIndex, data_.AnyState) != 0) { _completeTask.Add(RecordStateHelper.ReadInt(dataIndex++, data_.AnyState)); }
        }

        private void _LoadUnlockDay(ActivityInstance data_)
        {
            dataIndex = 1000;
            while (RecordStateHelper.ReadInt(dataIndex, data_.AnyState) != 0) { _hasUnlockDay.Add(RecordStateHelper.ReadInt(dataIndex++, data_.AnyState)); }
        }

        private void _LoadWaitComplete(ActivityInstance data_)
        {
            dataIndex = 1500;
            while (RecordStateHelper.ReadInt(dataIndex, data_.AnyState) != 0) { _waitComplete.Add(RecordStateHelper.ReadInt(dataIndex++, data_.AnyState)); }
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            _SaveNormal(data_);
            _SaveTask(data_);
            _SaveComplete(data_);
            _SaveUnlockDay(data_);
            _SaveWaitComplete(data_);
        }

        private void _SaveNormal(ActivityInstance data_)
        {
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _detailID));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _tokenNum));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _taskCount));
        }
        private void _SaveTask(ActivityInstance data_)
        {
            dataIndex = 100;
            foreach (var info in _taskInfos)
            {
                data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, info.Key));
                data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, info.Value));
            }
        }

        private void _SaveComplete(ActivityInstance data_)
        {
            dataIndex = 500;
            foreach (var id in _completeTask) { data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, id)); }
        }

        private void _SaveUnlockDay(ActivityInstance data_)
        {
            dataIndex = 1000;
            foreach (var id in _hasUnlockDay) { data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, id)); }
        }

        private void _SaveWaitComplete(ActivityInstance data_)
        {
            dataIndex = 1500;
            foreach (var id in _waitComplete) { data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, id)); }
        }

        private void _InitDetail()
        {
            if (_detailID == 0) { _detailID = Game.Manager.userGradeMan.GetTargetConfigDataId(eventConfig?.Detail ?? 0); }
            detailConfig = SevenDayTaskDetailVisitor.Get(_detailID);
        }

        private void _InitTaskInfos()
        {
            foreach (var id in detailConfig.TaskGroup)
            {
                var group = SevenDayTaskGroupVisitor.Get(id);
                if (group != null)
                {
                    foreach (var task in group.TaskInfo)
                    {
                        var info = SevenDayTaskInfoVisitor.Get(task);
                        if (info != null)
                        {
                            var require = info.RequireParam.ConvertToInt3();
                            _taskInfos.TryAdd(task, Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(require.Item1, require.Item2));
                        }
                    }
                }
            }
            _taskCount = _taskInfos.Count;
        }

        public string BoardEntryAsset()
        {
            mainPopup.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return string.IsNullOrEmpty(key) ? "event_sevendaytask_default#SevenDayTaskEntry.prefab" : key;
        }

        #region 接口
        public void AddToken(int id, int amount)
        {
            if (id != eventConfig.TokenId || amount < 0) { return; }
            _tokenNum += amount;
            if (_CheckMilestoneReward()) { _ClaimMilestoneReward(); }
        }

        public bool TryCompleteTask(int id, SevenDayTaskGroup group)
        {
            var config = SevenDayTaskInfoVisitor.Get(id);
            if (config == null || _completeTask.Contains(id)) { return false; }
            foreach (var str in config.TaskReward)
            {
                var reward = str.ConvertToRewardConfig();
                taskRewardCommitDatas.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.seven_day_task));
            }
            _completeTask.Add(id);
            DataTracker.event_7daytask_claim.Track(this, detailConfig.TaskGroup.IndexOf(group.Id) + 1, group.TaskInfo.IndexOf(id) + 1, _tokenNum, detailConfig.Diff, taskRewardCommitDatas[1].rewardId, _completeTask.Count == detailConfig.TaskGroup.Count * group.TaskInfo.Count);
            MessageCenter.Get<SEVEN_DAY_TASK_UPDATE>().Dispatch();
            var allfinish = true;
            foreach (var groupID in detailConfig.TaskGroup)
            {
                if (SevenDayTaskGroupVisitor.Get(groupID).TaskInfo.All(x => _completeTask.Contains(x))) { continue; }
                allfinish = false;
            }
            if (allfinish) Game.Manager.activity.EndImmediate(this, false);
            return true;
        }

        public bool CheckTaskCanComplete(int id)
        {
            var config = SevenDayTaskInfoVisitor.Get(id);
            if (config == null || !_taskInfos.ContainsKey(id)) { return false; }
            if (_completeTask.Contains(id)) { return false; }
            return _taskInfos[id] <= Game.Manager.taskMan.FillTaskProgress(this, config.TaskType);
        }

        public bool CheckTaskHasComplete(int id)
        {
            return _completeTask.Contains(id);
        }

        public bool WhetherPlayUnlock(int day)
        {
            if (_hasUnlockDay.Contains(day + 1)) { return false; }
            return startDay.AddDays(day) <= DateTime.UtcNow.AddSeconds(Game.Manager.networkMan.networkBias);
        }

        public bool WhetherUnlock(int day)
        {
            return startDay.AddDays(day) < DateTime.UtcNow.AddSeconds(Game.Manager.networkMan.networkBias);
        }

        public bool WhetherComplete(int day)
        {
            var conf = SevenDayTaskGroupVisitor.Get(detailConfig.TaskGroup[day]);
            foreach (var id in conf.TaskInfo)
            {
                if (!CheckTaskHasComplete(id)) { return false; }
            }
            return true;
        }

        public int GetCanCompleteNum(int day)
        {
            var count = 0;
            var conf = SevenDayTaskGroupVisitor.Get(detailConfig.TaskGroup[day]);
            foreach (var id in conf.TaskInfo)
            {
                if (CheckTaskCanComplete(id)) { count++; }
            }
            return count;
        }

        public void UnlockDay(int day)
        {
            for (var i = 0; i <= day; i++)
            {
                _hasUnlockDay.AddIfAbsent(i + 1);

            }
        }

        public int GetCanCompleteTaskCount()
        {
            var count = 0;
            foreach (var group in detailConfig.TaskGroup)
            {
                if (!WhetherUnlock(detailConfig.TaskGroup.IndexOf(group))) { continue; }
                foreach (var id in SevenDayTaskGroupVisitor.Get(group).TaskInfo)
                {
                    if (CheckTaskCanComplete(id)) { count++; }
                }
            }
            return count;
        }

        #endregion

        #region 内部业务
        private bool _CheckMilestoneReward()
        {
            if (phase >= detailConfig.PointsRwd.Count) { return false; }
            return _tokenNum >= SevenDayTaskRwdVisitor.Get(detailConfig.PointsRwd[phase]).Points;
        }

        private void _ClaimMilestoneReward()
        {
            var config = SevenDayTaskRwdVisitor.Get(detailConfig.PointsRwd[phase]);
            milestoneRewardCommitData.Clear();
            foreach (var str in config.Reward)
            {
                var reward = str.ConvertToRewardConfig();
                milestoneRewardCommitData.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.seven_day_milestone));
            }
            phase++;
            DataTracker.event_7daytask_milestone.Track(this, phase, detailConfig.PointsRwd.Count, _tokenNum, detailConfig.Diff, config.Id, phase == detailConfig.PointsRwd.Count);
        }
        #endregion
    }
}