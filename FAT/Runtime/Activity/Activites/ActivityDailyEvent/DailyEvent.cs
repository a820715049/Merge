using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using UnityEngine;
using EL;
using System;
using DataDE = fat.gamekitdata.DailyEvent;
using static DataTracker;
using static FAT.RecordStateHelper;

namespace FAT
{
    using static PoolMapping;

    public class DailyEvent : IGameModule, IUserDataHolder, IUpdate
    {
        public class Task
        {
            public DailyEventTask conf;
            public Config.RewardConfig reward;
            public Config.RewardConfig rewardM;
            public int Id => conf.Id;
            public int Priority => conf.Sort;
            public int ValueIndex => conf.RequireType;
            public string Name => I18N.FormatText(conf.Desc, $"{require}{Icon}");
            public string Icon => conf.RequireType switch
            {
                1 => TextSprite.Diamond,
                //2 => merge count
                3 => TextSprite.Coin,
                _ => string.Empty
            };
            public int require;
            public int value;
            public bool complete;
            public int group;
        }

        public static int TokenId => 7;
        public readonly Dictionary<int, int> refValue = new();
        public DailyEventList active;
        public DEGroup groupRef;
        public DailyEventGroup group;
        public int groupIndex;
        public Config.RewardConfig groupReward;
        public Config.RewardConfig iconReward1;
        public Config.RewardConfig iconReward2;
        public readonly List<Task> list = new();
        public readonly List<Task> listN = new();
        public readonly Dictionary<int, Task> mapN = new();
        public DailyEventMilestone milestone;
        public DEMInfo nodeRef;
        public readonly List<RewardBar.NodeInfo> listM = new();
        public int valueMax;
        public int valueM;
        public long refreshTSLegacyD;
        public long refreshTSLegacyM;

        public bool Valid => ActivityD.Valid && groupRef != null;
        public bool Unlocked => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureDe);
        public bool MilestoneValid => ActivityM.Valid && nodeRef != null;
        public bool MilestoneUnlocked => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureDem);
        public bool NextGroupValid => groupIndex < groupRef.IncludeGroupId.Count - 1;
        public bool GroupValid => group != null;
        public bool GroupComplete => TaskComplete >= TaskCount;
        public int TaskCount => group != null ? group.IncludeTaskId.Count : 0;
        public int TaskCountDiff => list.Count - TaskCount;
        public int TaskCompleteTotal { get; private set; }
        public int TaskComplete => TaskCompleteTotal - TaskCountDiff;
        public int taskCountN;
        public int taskCountG;
        public long MilestoneCompleteTS { get; internal set; }
        internal long TSOffset = 17_0403_8400;//24/1/1 00:00:00
        private bool checkTask;
        public GroupDE ActivityGroup { get; } = new();
        public ActivityDE ActivityD { get; } = new();
        public ActivityDEM ActivityM { get; } = new();
        internal ActivityTrigger Trigger => Game.Manager.activityTrigger;
        internal List<RewardCommitData> MilestoneReward { get; } = new();

        public void LoadConfig()
        {

        }

        public void Reset()
        {
            ActivityD.SetupClear();
            ActivityM.SetupClear();
            active = null;
            SetupListener();
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var game = archive.ClientData.PlayerGameData;
            var data = game.DailyEvent ??= new();
            var any = data.AnyState;
            void FillActive(DataDE data_)
            {
                if (!Valid) return;
                data_.ActiveId = active.Id;
                data_.GroupIndex = groupIndex;
                foreach (var (k, v) in refValue)
                {
                    data_.RefValue[k] = v;
                }
                foreach (var task in list)
                {
                    var id = task.Id;
                    data_.Task[id] = new()
                    {
                        Id = id,
                        State = task.complete ? 1 : 0,
                        Require = task.require,
                        Value = task.value,
                    };
                }
                any.Add(ToRecord(1, ActivityD.Id));
                any.Add(ToRecord(3, groupRef.Id));
            }
            void FillMilestone(DataDE data_)
            {
                if (!MilestoneValid) return;
                data_.MilestoneId = milestone.Id;
                data_.MilestoneValue = valueM;
                var list = listM;
                for (var i = 0; i < list.Count; ++i)
                {
                    if (list[i].complete) data_.MilestoneRecord[i] = 1;
                }
                any.Add(ToRecord(2, ActivityM.Id));
                any.Add(ToRecord(4, nodeRef.Id));
            }
            FillActive(data);
            FillMilestone(data);
            any.Add(ToRecord(5, (int)(MilestoneCompleteTS - TSOffset)));
        }

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            static int RefId(int record_, int conf_)
            {
                if (record_ > 0) return record_;
                return Game.Manager.userGradeMan.GetTargetConfigDataId(conf_);
            }
            var data = archive.ClientData.PlayerGameData?.DailyEvent;
            if (data == null) return;
            var any = data.AnyState;
            var id = data.ActiveId;
            SetupRefreshTimeLegacy(data.RefreshTS);
            var conf = GetDailyEventList(id);
            if (conf != null)
            {
                var actId = ReadInt(1, any);
                var (valid, r) = ActivityLite.ReadyToCreate((actId, 0), default, out var lite);
                if (!valid) DebugEx.Error($"DE load failed, reason:{r}");
                else
                {
                    var gRefId = RefId(ReadInt(3, any), conf.IncludeGrpId);
                    var gRef = GetDEGroup(gRefId);
                    SetupActivityD(lite, conf, gRef, data.GroupIndex, data);
                }
            }
            var idM = data.MilestoneId;
            var confM = GetDailyEventMilestone(idM);
            if (confM != null)
            {
                var actId = ReadInt(2, any);
                var (valid, r) = ActivityLite.ReadyToCreate((actId, 0), default, out var lite);
                if (!valid) DebugEx.Error($"DEM load failed, reason:{r}");
                else
                {
                    var nRefId = RefId(ReadInt(4, any), confM.MilestoneGrpId);
                    var nRef = GetDEMInfo(nRefId);
                    SetupActivityM(lite, confM, nRef, data.MilestoneValue, data);
                }
            }
            MilestoneCompleteTS = ReadInt(5, any) + TSOffset;
        }

        public void SetupRefreshTimeLegacy(long ts_)
        {
            if (ts_ <= 0) return;
            var t = DateTime.UnixEpoch.AddSeconds(ts_);
            var g = Game.Manager.configMan.globalConfig;
            var rH = g.DeRefreshUtc;
            var rT = Game.NextTimeOfDay(t, rH, offset_: 0);
            var refreshT = rT;
            refreshTSLegacyD = Game.Timestamp(refreshT);
            var rD = g.DemRefreshWeekday;
            var wD = DayOfWeek(rT, -rH);
            var day = 7;
            var diffD = (rD + day - wD) % day;
            var refreshTM = rT.AddDays(diffD);
            refreshTSLegacyM = Game.Timestamp(refreshTM);
            Debug.Log($"{nameof(DailyEvent)} legacy refresh d:{Game.TimeOf(ts_)} t:{refreshT} tm:{refreshTM}");
        }

        public static int DayOfWeek(DateTime t_, int offset_)
        {
            var tt = t_.AddHours(offset_);
            var day = (int)tt.DayOfWeek;
            //map sun-sat = 0-6
            //to mon-sun = 1-7
            return day switch
            {
                0 => 7,
                _ => day,
            };
        }

        public void Startup()
        {

        }

        public (ActivityLike, bool, bool, string) SetupActivityD(LiteInfo lite_, bool check_)
        {
            var mgr = Game.Manager.activity;
            var a = ActivityD;
            if (!Unlocked || (check_ && a.SetupValid && mgr.LookupAny(a.Type) != null)) goto fail;
            var gts = Game.TimestampNow();
            var old = gts < refreshTSLegacyD && a.KeepLegacy();
            var confD = GetDailyEventList(lite_.param);
            if (confD == null) return (null, false, false, $"daily event list {lite_.param} not found");
            if (!check_) goto skip_check;
            if (!Trigger.Evaluate(confD.ActiveRequire)) goto fail;
            skip_check:
            if (a.Match(lite_, confD)) return (a, true, true, null);
            var keep = old || a.Id == lite_.id;
            var groupId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.IncludeGrpId);
            var groupRef = GetDEGroup(groupId);
            if (groupRef == null) return (null, false, false, $"daily event group {groupId}|{confD.IncludeGrpId} not found");
            return (SetupActivityD(lite_, confD, groupRef, keep_: keep), true, false, null);
        fail:
            return (a, false, false, null);
        }
        public ActivityDE SetupActivityD(LiteInfo lite_, DailyEventList confD_, DEGroup groupRef_, int group_ = 0, DataDE data_ = null, bool keep_ = false)
        {
            var a = ActivityD;
            if (a.Match(lite_, confD_)) return a;
            CleanupActive();
            a.Setup(lite_, confD_);
            SetupActive(confD_, groupRef_, group_, data_, keep_);
            return a;
        }

        public (ActivityLike, bool, bool, string) SetupActivityM(LiteInfo lite_, bool check_)
        {
            var mgr = Game.Manager.activity;
            var a = ActivityM;
            if (!MilestoneUnlocked || (check_ && a.SetupValid && mgr.LookupAny(a.Type) != null)) goto fail;
            var gts = Game.TimestampNow();
            var old = gts < refreshTSLegacyM && a.KeepLegacy();
            var confD = GetDailyEventMilestone(lite_.param);
            if (confD == null) return (null, false, false, $"daily event milestone {lite_.param} not found");
            if (!check_) goto skip_check;
            if (old && confD != a.confD) goto fail;
            skip_check:
            if (a.Match(lite_, confD)) return (a, true, true, null);
            var keep = (old || a.Id == lite_.id) && confD == milestone;
            var nodeId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.MilestoneGrpId);
            var nodeConf = GetDEMInfo(nodeId);
            if (nodeConf == null) return (null, false, false, $"milestone info {nodeId}|{confD.MilestoneGrpId} not found");
            return (SetupActivityM(lite_, confD, nodeConf, keep_: keep), true, false, null);
        fail:
            return (a, false, false, null);
        }
        public ActivityDEM SetupActivityM(LiteInfo lite_, DailyEventMilestone confD_, DEMInfo node_, int value_ = 0, DataDE data_ = null, bool keep_ = false)
        {
            var a = ActivityM;
            if (a.Match(lite_, confD_)) return a;
            a.Setup(lite_, confD_);
            SetupMilestone(confD_, node_, value_, data_, keep_);
            return a;
        }

        public void Update(float _)
        {
            if (checkTask)
            {
                checkTask = false;
                CheckTask();
            }
        }

        public void DebugReset()
        {
            refValue.Clear();
            var validD = Valid;
            var validM = MilestoneValid;
            var infoD = ActivityD.Lite.ToInfo();
            var infoM = ActivityM.Lite.ToInfo();
            ActivityD.SetupClear();
            ActivityM.SetupClear();
            if (validD) SetupActivityD(infoD, active, groupRef);
            if (validM) SetupActivityM(infoM, milestone, nodeRef);
            var popup = Game.Manager.screenPopup;
            var state = PopupType.Login;
            popup.Query(state);
        }

        public void DebugClaimNext()
        {
            for (var n = 0; n < list.Count; ++n)
            {
                var task = list[n];
                if (task.complete) continue;
                ClaimTask(task);
                break;
            }
            CheckGroup();
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_UPDATE_ANY>().Dispatch();
        }

        public void DebugAlmostCompleteNext()
        {
            for (var n = 0; n < list.Count; ++n)
            {
                var task = list[n];
                if (task.complete) continue;
                refValue[task.ValueIndex] = task.require - 1;
                break;
            }
            CheckTask();
        }

        private void SetupListener()
        {
            void WhenCoinUse(CoinChange change_)
            {
                if (change_.type == CoinType.Gem) UpdateRefValue(1, change_.amount);
                if (change_.type == CoinType.MergeCoin && change_.reason == ReasonString.undo_sell_item) UpdateRefValue(3, -change_.amount);
            }
            void WhenCoinAdd(CoinChange change_)
            {
                if (change_.type == CoinType.MergeCoin) UpdateRefValue(3, change_.amount);
            }
            void WhenBoardMerge(Merge.Item t_)
            {
                var b = t_?.world?.activeBoard;
                if (b == null || b.boardId != Constant.MainBoardId) return;
                UpdateRefValue(2, 1);
            }
            MessageCenter.Get<MSG.GAME_COIN_USE>().AddListenerUnique(WhenCoinUse);
            MessageCenter.Get<MSG.GAME_COIN_ADD>().AddListenerUnique(WhenCoinAdd);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_MERGE>().AddListenerUnique(WhenBoardMerge);
            MessageCenter.Get<MSG.ACTIVITY_STATE>().AddListenerUnique(WhenActivityState);
        }

        public void UpdateRefValue(int id, int value)
        {
            if (!Valid) return;
            refValue.TryGetValue(id, out var v);
            refValue[id] = v + value;
            checkTask = true;
        }

        public void UpdateMilestone(int id_, int v_)
        {
            var valueO = valueM;
            valueM += v_;
            token_change.Track(id_, v_, valueM, ReasonString.daily_event);
            if (valueO < valueMax)
            {
                for (var n = 0; n < listM.Count; ++n)
                {
                    var node = listM[n];
                    var ready = valueM >= node.value;
                    if (!ready) break;
                    if (!node.complete)
                    {
                        var data = ClaimMilestone(n);
                        MilestoneReward.Add(data);
                    }
                }
                MessageCenter.Get<MSG.DAILY_EVENT_MILESTONE_PROGRESS>().Dispatch(valueO, valueM);
            }
        }

        public int MilestoneNext(int v_) => RewardBar.Next(listM, v_);
        public bool MilestoneComplete(int v_) => RewardBar.Complete(listM, v_);

        public void SetupActive(DailyEventList conf_, DEGroup groupRef_, int group_ = 0, DataDE data_ = null, bool keep_ = false)
        {
            static int CountG(int id_) => GetDailyEventGroup(id_)?.IncludeTaskId.Count ?? 0;
            static (int, int) CountD(DEGroup g_)
            {
                var list = g_.IncludeGroupId;
                var n = CountG(list[0]);
                var g = 0;
                for (var k = 1; k < list.Count; ++k)
                {
                    g += CountG(list[k]);
                }
                return (n, g);
            }
            Debug.Log($"{nameof(DailyEvent)} select list:{conf_.Id} keep:{keep_}");
            if (!keep_) refValue.Clear();
            if (data_ != null)
            {
                foreach (var (k, v) in data_.RefValue)
                {
                    refValue[k] = v;
                }
            }
            active = conf_;
            groupRef = groupRef_;
            (taskCountN, taskCountG) = CountD(groupRef_);
            list.Clear();
            listN.Clear();
            SetupGroup(group_, data_, keep_: keep_);
        }

        public void SetupGroup(int index_, DataDE data_ = null, bool keep_ = false)
        {
            groupIndex = index_;
            group = null;
            groupReward = null;
            iconReward1 = null;
            iconReward2 = null;
            static Config.RewardConfig ListReward(IList<string> r_, int i_)
            {
                return i_ >= 0 && i_ < r_.Count ? r_[i_].ConvertToRewardConfigIfValid() : null;
            }
            static void TryAddTask(int id_, List<Task> list_, TaskDE data_)
            {
                var tConf = GetDailyEventTask(id_);
                if (tConf == null)
                {
                    if (data_ == null) Debug.LogError($"failed to find config for task id={id_}");
                    return;
                }
                var task = CreateTask(tConf, data_: data_);
                list_.Add(task);
            }
            static void TryMatchTask(int id_, List<Task> list_, Dictionary<int, Task> map_, TaskDE data_)
            {
                if (!map_.TryGetValue(id_, out var t))
                {
                    if (map_.Count > 0) DebugEx.Warning($"task to be added {id_} not found in map");
                    TryAddTask(id_, list_, null);
                }
                else
                {
                    list_.Add(t);
                }
            }
            var gList = groupRef.IncludeGroupId;
            var gId = 0;
            var last = gList.Count - 1;
            if (data_ != null)
            {
                foreach (var (id, state) in data_.Task)
                {
                    TryAddTask(id, list, state);
                }
            }
            if (groupIndex <= last)
            {
                gId = gList[groupIndex];
                group = GetDailyEventGroup(gId);
                groupReward = group.GroupReward.ConvertToRewardConfigIfValid();
                iconReward1 = ListReward(group.IconShow, 0);
                iconReward2 = ListReward(group.IconShow, 1);
                mapN.Clear();
                foreach (var t in listN)
                {
                    mapN[t.Id] = t;
                }
                listN.Clear();
                foreach (var tId in group.IncludeTaskId)
                {
                    if (data_ != null && data_.Task.ContainsKey(tId)) continue;
                    TryMatchTask(tId, list, mapN, null);
                }
                for (var k = index_ + 1; k <= last; ++k)
                {
                    var preview = GetDailyEventGroup(gList[k]);
                    foreach (var tId in preview.IncludeTaskId)
                    {
                        TryMatchTask(tId, listN, mapN, null);
                    }
                }
                mapN.Clear();
            }
            Debug.Log($"{nameof(DailyEvent)} select group:{gId} task:{list.Count}");
            CheckTask(changed_: true, keep_: keep_);
        }

        public static Config.RewardConfig RewardOf(DailyEventTask conf_)
        {
            var reward = conf_.TaskReward.ConvertToRewardConfig();
            var discountValid = Game.Manager.activity.LookupAny(fat.rawdata.EventType.DiscountPack, out _);
            var altValid = !string.IsNullOrEmpty(conf_.EventExchange);
            if (!discountValid && altValid)
            {
                reward = conf_.EventExchange.ConvertToRewardConfig();
            }
            return reward;
        }

        private static Task CreateTask(DailyEventTask conf_, TaskDE data_ = null)
        {
            static int CalculateRequire(string param_)
            {
                var t = param_.ConvertToRewardConfig();
                return Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(t.Id, t.Count);
            }
            var task = new Task()
            {
                conf = conf_,
                reward = RewardOf(conf_),
                rewardM = conf_.MilestoneReward.ConvertToRewardConfigIfValid(),
            };

            if (data_ != null)
            {
                task.require = data_.Require;
                task.value = data_.Value;
                task.complete = data_.State > 0;
            }
            else
            {
                task.require = CalculateRequire(conf_.RequireParam);
            }
            return task;
        }

        public void SetupMilestone(DailyEventMilestone conf_, DEMInfo node_, int value_ = 0, DataDE data_ = null, bool keep_ = false)
        {
            Debug.Log($"{nameof(DailyEvent)} select milestone:{conf_.Id} keep:{keep_}");
            milestone = conf_;
            nodeRef = node_;
            var confR = nodeRef.MilestoneReward;
            var confH = nodeRef.MilestoneHighlight;
            var confS = nodeRef.MilestoneScore;
            listM.Clear();
            var score = 0;
            for (var n = 0; n < confR.Count; ++n)
            {
                var v = 0;
                data_?.MilestoneRecord.TryGetValue(n, out v);
                score += confS[n];
                listM.Add(new()
                {
                    reward = confR[n].ConvertToRewardConfig(),
                    value = score,
                    pos = n + 1,
                    complete = v > 0 || (keep_ && valueM >= score),
                    effect = confH[n],
                });
            }
            valueMax = score;
            if (!keep_) valueM = value_;
            MessageCenter.Get<MSG.DAILY_EVENT_MILESTONE_UPDATE>().Dispatch();
        }

        public void CleanupActive()
        {
            if (active == null) return;
            var c = ActivityExpire.ConvertExpire(active.ExpireItem, Game.Manager.mainMergeMan.world);
            Debug.Log($"{nameof(DailyEvent)} convert {c} expire items");
        }

        public void CheckTask(bool changed_ = false, bool keep_ = false)
        {
            TaskCompleteTotal = 0;
            for (var n = 0; n < list.Count; ++n)
            {
                var task = list[n];
                if (task.complete)
                {
                    ++TaskCompleteTotal;
                    continue;
                }
                var oV = task.value;
                var ready = oV >= task.require;
                refValue.TryGetValue(task.ValueIndex, out var nV);
                if (oV == nV) goto check;
                task.value = nV;
                MessageCenter.Get<MSG.DAILY_EVENT_TASK_UPDATE>().Dispatch(task);
            check:
                var ready1 = nV >= task.require;
                if (ready != ready1) changed_ = true;
                if (ready1) ClaimTask(task, keep_);
            }
            if (changed_)
            {
                CheckGroup();
                MessageCenter.Get<MSG.DAILY_EVENT_TASK_UPDATE_ANY>().Dispatch();
            }
            for (var n = 0; n < listN.Count; ++n)
            {
                var task = listN[n];
                refValue.TryGetValue(task.ValueIndex, out var nV);
                task.value = nV;
            }
        }

        public void CheckGroup()
        {
            if (GroupComplete && groupReward == null)
            {
                ClaimGroup();
            }
        }

        public static RewardCommitData ClaimReward(Config.RewardConfig reward_)
        {
            if (reward_ == null) return null;
            var rewardMan = Game.Manager.rewardMan;
            var context = new RewardContext() { targetWorld = Game.Manager.mainMergeMan.world };
            return rewardMan.BeginReward(reward_.Id, reward_.Count, ReasonString.daily_event, context_: context);
        }
        public static void ClaimReward(List<RewardCommitData> list_, Config.RewardConfig reward_)
        {
            if (reward_ == null) return;
            var r = ClaimReward(reward_);
            list_?.Add(r);
        }

        public void ClaimTask(Task task_, bool keep_ = false)
        {
            task_.complete = true;
            ++TaskCompleteTotal;
            Debug.Log($"{nameof(DailyEvent)} task complete {task_.conf.Id} {task_.conf.TaskReward} {task_.conf.MilestoneReward}");
            daily_task.Track(ActivityD, task_);
            if (keep_) return;
            var token = PoolMappingAccess.Take<List<RewardCommitData>>(out var list);
            var tokenM = PoolMappingAccess.Take<List<RewardCommitData>>(out var listM);
            ClaimReward(list, task_.reward);
            ClaimReward(listM, task_.rewardM);
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_COMPLETE>().Dispatch((task_, token, tokenM));
            MessageCenter.Get<MSG.TASK_COMPLETE_DAILY_TASK>().Dispatch();
            if (GroupValid && GroupComplete)
            {
                MessageCenter.Get<MSG.ACTIVITY_SUCCESS>().Dispatch(ActivityD);
            }
        }

        public RewardCommitData ClaimGroup()
        {
            if (!GroupValid || !GroupComplete) return null;
            Debug.Log($"{nameof(DailyEvent)} group complete {group.Id} #{groupIndex} {group.GroupReward}");
            daily_task_group.Track();
            var data = ClaimReward(groupReward);
            SetupGroup(groupIndex + 1);
            checkTask = true;
            return data;
        }

        public RewardCommitData ClaimMilestone(int index_)
        {
            if (index_ < 0 || index_ >= listM.Count) return null;
            var node = listM[index_];
            if (node.complete) return null;
            Debug.Log($"{nameof(DailyEvent)} milestone complete {milestone.Id} #{index_} {node.reward}");
            daily_task_milestone.Track(ActivityM, index_, listM.Count, nodeRef.Diff);
            node.complete = true;
            listM[index_] = node;
            if (index_ == listM.Count - 1)
            {
                MilestoneCompleteTS = Game.TimestampNow();
                MessageCenter.Get<MSG.ACTIVITY_SUCCESS>().Dispatch(ActivityM);
            }
            return ClaimReward(node.reward);
        }

        public UIResource OpenTask()
        {
            if (ActivityD == null)
            {
                var r = UIConfig.UIDailyEvent;
                UIManager.Instance.OpenWindow(r);
                return r;
            }
            var rr = ActivityD.TaskRes.ActiveR;
            ActivityLike.OpenRes(rr, ActivityD);
            return rr;
        }

        public void WhenActivityState(ActivityLike acti_)
        {
            if (acti_.Type != fat.rawdata.EventType.DiscountPack) return;
            foreach (var task in list)
            {
                task.reward = RewardOf(task.conf);
            }
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_UPDATE_ANY>().Dispatch();
        }
    }
}