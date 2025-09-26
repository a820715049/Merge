using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using Config;
using EL;
using System.Linq;
using FAT.Merge;
using System.Collections;
using UnityEngine;
using EL.Resource;

namespace FAT {
    using static PoolMapping;

    public class ActivityStep : ActivityLike, IActivityOrderHandler, IActivityOrderGenerator {
        public struct Task {
            public EventStepTask conf;
            public MBRewardLayout.TupleList item;
            public List<RewardConfig> reward;
        }

        public EventStep confD;
        public EventStepDetail confG;
        public override bool Valid => confD != null;
        public override ActivityVisual Visual => VisualMain.visual;
        public VisualPopup VisualMain { get; } = new(UIConfig.UIActivityStep);
        public VisualPopup VisualEnd { get; } = new(UIConfig.UIActivityStepEnd);
        public VisualRes VisualComplete { get; } = new(UIConfig.UIActivityStepComplete);
        public List<Task> list = new();
        public AssetConfig rewardIcon;
        public readonly List<RewardConfig> rewardM = new();
        public int TaskIndex { get; set; }
        public int VisualIndex { get; set; }
        public bool Complete => TaskIndex >= list.Count;
        public bool Claimed { get; set; }
        public int DecorateScore => confG.DecorateScore;
        private readonly OutputSpawnBonusHandler bonusHandler = new();

        public ActivityStep(ActivityLite lite_) {
            Lite = lite_;
            confD = GetEventStep(lite_.Param);
            if (confD == null) return;
            VisualMain.Setup(confD.EventTheme, this);
            VisualEnd.Setup(confD.RecycleTheme, this, active_:false);
            VisualComplete.Setup(confD.CompleteTheme);
            var map = VisualMain.visual.AssetMap;
            if (map.TryGetValue("bg", out var s)) {
                rewardIcon = s.ConvertToAssetConfig();
            }
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(bonusHandler);
        }

        public override void SaveSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            any.Add(ToRecord(1, TaskIndex));
            if (confG != null) any.Add(ToRecord(2, confG.Id));
            bonusHandler.Serialize(any, 1000);
        }

        public override void LoadSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            TaskIndex = ReadInt(1, any);
            var gId = ReadInt(2, any);
            bonusHandler.Deserialize(any, 1000);
            VisualIndex = TaskIndex;
            SetupDetail(gId);
        }

        public override void SetupFresh() {
            var gId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.GradeId);
            SetupDetail(gId);
        }

        public void SetupDetail(int gId) {
            if (gId == 0) {
                gId = 3;//HACK to fix legacy data before 14.0, to be deleted when 13.x is obselete
            }
            confG = GetEventStepDetail(gId);
            if (confG == null) {
                DebugEx.Error($"failed to find step detail {gId}");
                return;
            }
            foreach(var s in confG.MilestoneReward) {
                var r = s.ConvertToRewardConfig();
                rewardM.Add(r);
            }
            foreach(var id in confG.TaskId) {
                var confT = GetEventStepTask(id);
                if (confT == null) {
                    DebugEx.Warning($"failed to find step task {id}");
                    continue;
                }
                var itemList = confT.RequireItemId;
                var item = Enumerable.ToList(itemList.GroupBy(v => v).Select(g => (g.First(), g.Count())));
                var rewardList = confT.Reward;
                var t = new Task {
                    conf = confT,
                    item = new MBRewardLayout.TupleList { list = item },
                    reward = Enumerable.ToList(rewardList.Select(v => v.ConvertToRewardConfig())),
                };
                list.Add(t);
            }
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (!Valid) yield break;
            foreach(var v in Visual.ResEnumerate()) yield return v;
            foreach(var v in VisualEnd.ResEnumerate()) yield return v;
            foreach(var v in VisualComplete.ResEnumerate()) yield return v;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            VisualMain.Popup(popup_, state_);
        }

        public override void Open() {
            Open(VisualMain.res);
        }

        public override void WhenEnd() {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(bonusHandler);
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            ActivityExpire.ConvertToReward(confD.ExpirePopup, list, ReasonString.step);
            VisualEnd.Popup(custom_:listT);
        }

        public void DebugReset() {
            TaskIndex = 0;
            VisualIndex = 0;
        }

        public void DebugComplete() {
            CompleteTask(0);
        }

        public void CompleteTask(float delay_ = 0.5f) {
            ++TaskIndex;
            IEnumerator Delay() {
                yield return new WaitForSeconds(delay_);
                var wait = Game.Manager.specialRewardMan.IsBusy();
                if (wait) {
                    void WaitA() {
                        MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(WaitA);
                        Open();
                    }
                    MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(WaitA);
                    yield break;
                }
                Open();
            }
            Game.Instance.StartCoroutineGlobal(Delay());
        }

        public void TryComplete() {
            if (Claimed) return;
            DataTracker.event_step_milestone.Track(this, confG.Diff);
            MessageCenter.Get<MSG.ACTIVITY_SUCCESS>().Dispatch(this);
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            var rewardMan = Game.Manager.rewardMan;
            foreach(var r in rewardM) {
                var d = rewardMan.BeginReward(r.Id, r.Count, ReasonString.step);
                list.Add(d);
            }
            UIManager.Instance.OpenWindow(VisualComplete.res.ActiveR, this, listT);
        }

        #region order

        public static readonly string orderPrefabKey = "bgOrder";
        public static string GetOrderThemeRes(int eventId, int paramId)
        {
            if (paramId == 0) {
                var cfg = GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == fat.rawdata.EventType.Step);
                paramId = cfg?.EventParam ?? 0;
            }
            if (paramId == 0) {
                DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }
            var cfgDetail = GetEventStep(paramId);
            var theme = GetOneEventThemeByFilter(x => x.Id == cfgDetail.EventTheme);
            if (theme.AssetInfo.TryGetValue(orderPrefabKey, out var assetInfo)) {
                return assetInfo;
            }
            return cfgDetail?.OrderPrefab;
        }

        void IActivityOrderHandler.HandlerCollected() {
            if (TaskIndex < 0 || TaskIndex >= list.Count) return;
            var task = list[TaskIndex];
            var conf = task.conf;
            if (bonusHandler.id == conf.Id) return;
            bonusHandler.Init(conf.Id, conf.OutputsOne, conf.OutputsTwo, conf.OutputsFour, conf.OutputsFixedOne, conf.OutputsFixedTwo, conf.OutputsFixedFour, conf.WithoutputTime, null);
        }

        bool IActivityOrderHandler.IsValidForBoard(int boardId)
        {
            return confD.BoardId == boardId;
        }

        bool IActivityOrderGenerator.TryGeneratePassiveOrder(OrderRandomer cfg, IOrderHelper helper, MergeWorldTracer tracer, Func<OrderRandomer, OrderData> builder, out OrderData order)
        {
            order = null;
            if (cfg.Id != confD.RandomerId)
                return false;
            if (Complete)
                return false;
            var task = list[TaskIndex];
            var items = task.conf.RequireItemId;
            var difficulty = OrderUtility.CalcRealDifficultyForRequires(items);
            order = OrderUtility.MakeOrderByConfig(helper, OrderProviderType.Random, cfg.Id, task.conf.RoleId, 0, difficulty, items, task.conf.Reward);
            order.OrderType = (int)OrderType.Step;
            order.Record.OrderType = order.OrderType;
            var any = order.Record.Extra;
            any.Add(ToRecord((int)OrderParamType.EventId, Id));
            any.Add(ToRecord((int)OrderParamType.EventParam, Param));
            any.Add(ToRecord((int)OrderParamType.StartTimeSec, (int)Game.TimestampNow()));
            any.Add(ToRecord((int)OrderParamType.DurationSec, (int)Countdown));
            DebugEx.Info($"ActivityStep::TryGeneratePassiveOrder StepOrder {Id} {order.Id} {Countdown}");

            return true;
        }

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (order.Id != confD.RandomerId)
                return false;
            if (Complete)
                return false;
            if (!(order as IOrderData).IsStep)
                return false;
            if (order.GetValue(OrderParamType.EventId) != Id)
                return false;
            if (order.State == OrderState.Rewarded)
            {
                DebugEx.Info($"ActivityStep::OnPreUpdate order completed {order.Id}");
                CompleteTask(2);
            }
            else if (order.State == OrderState.Expired)
            {
                DebugEx.Info($"ActivityStep::OnPreUpdate order expired {order.Id}");
            }
            // 不改变order 始终返回false
            return false;
        }
        #endregion
    }
}