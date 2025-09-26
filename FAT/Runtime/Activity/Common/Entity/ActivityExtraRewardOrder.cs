/*
 * @Author: pengjian.zhang
 * @Description: 订单额外奖励活动
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/kc7e5p7c7gti54gg?inner=JybC5
 * @Date: 2024-03-19 16:19:28
 */

using EL;
using fat.gamekitdata;
using fat.rawdata;
using fat.conf;
using static FAT.RecordStateHelper;
using System;
using System.Collections.Generic;
using Config;
using FAT.Merge;
using EventType = fat.rawdata.EventType;
using EL.Resource;
using System.Linq;

namespace FAT
{
    using static PoolMapping;

    public class ActivityExtraRewardOrder : ActivityLike, IActivityOrderHandler, IBoardEntry
    {
        enum ParamKey
        {
            ActStartTime,
        }

        public override ActivityVisual Visual => VisualEntry;
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureOrderExtra);
        public UIResAlt Res { get; } = new(UIConfig.UIOrderExtra);
        public PopupActivity Popup { get; internal set; }
        public bool IsShowEntry => ConfD.EntryThemeId != 0;
        public int boardId => ConfD.BoardId;
        public EventOrderExtra ConfD;
        public ActivityVisual VisualEntry { get; } = new();
        public ActivityVisual VisualPanel { get; } = new();

        private long lifeTime => ConfD.Lifetime;
        private int actEndTime;
        // 是否“静音”（不生成任务、不给订单挂额外奖励）
        private bool _muted;
        public ActivityExtraRewardOrder(ActivityLite lite_)
        {
            Lite = lite_;
            ConfD = Game.Manager.configMan.GetEventOrderExtraConfig(lite_.Param);

            //初始化弹脸
            if (ConfD != null && Visual.Setup(ConfD.EventTheme, Res))
            {
                Popup = new(this, Visual, Res, false);
            }

            VisualEntry.Setup(ConfD.EntryThemeId);
            VisualPanel.Setup(ConfD.EventTheme, Res);
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach (var v in Visual.ResEnumerate()) yield return v;
            foreach (var v in VisualEntry.ResEnumerate()) yield return v;
            foreach (var v in VisualPanel.ResEnumerate()) yield return v;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(Popup, state_);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            actEndTime = ReadInt((int)ParamKey.ActStartTime, any);
            _muted = ReadInt(1, any) != 0;
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord((int)ParamKey.ActStartTime, actEndTime));
            any.Add(ToRecord(1, _muted ? 1 : 0));
        }

        public override void SetupClear()
        {
            base.SetupClear();
            ConfD = null;
        }

        public override void Open()
        {
            UIManager.Instance.OpenWindow(Res.ActiveR, this);
        }

        public override (long, long) SetupTS(long sTS_, long eTS_)
        {
            // 如果lifetime > 0
            // 代表活动期间内只能参与1次，且这次持续这么久的时间
            // 但如果活动剩余时间 < lifetime，则活动只会持续到活动结束
            // 如果lifetime = 0
            // 代表活动期间内会持续享受到订单额外奖励

            if (actEndTime > 0)
                return (Lite.StartTS, actEndTime);
            if (lifeTime > 0)
            {
                if (Game.Instance.GetTimestampSeconds() + lifeTime >= Lite.EndTS)
                {
                    actEndTime = (int)Lite.EndTS;
                }
                else
                {
                    actEndTime = (int)(Game.Instance.GetTimestampSeconds() + lifeTime);
                }
            }
            else
            {
                actEndTime = (int)Lite.EndTS;
            }
            return (Lite.StartTS, actEndTime);
        }

        public override void WhenReset()
        {
            base.WhenReset();
            _muted = false;
        }

        public override void WhenActive(bool new_)
        {
            if (!new_)
            {
                return;
            }
            // DeadLine 期内：静音，不做奖励挂载
            var now = Game.TimestampNow();
            var deadline = ConfD.Deadline; // 直接使用配置字段（单位：秒）
            if (deadline <= 0)
            {
                _muted = false;
                return;
            }
            var preDeadlineOk = now < (endTS - deadline);
            _muted = !preDeadlineOk;
        }

        public List<RewardConfig> GetExtraReward(int orderRandomId)
        {
            //通过随机订单槽位获取用户分层表id
            if (ConfD.RewardGrpInfo.TryGetValue(orderRandomId, out var gradeIndexMappingId))
            {
                var listR = new List<RewardConfig>();
                //通过分层表获得逻辑id 此活动id含义为订单奖励
                var orderRewardId = Game.Manager.userGradeMan.GetTargetConfigDataId(gradeIndexMappingId);
                if (orderRewardId == 0)
                {
                    DebugEx.FormatError("[ActivityExtraRewardOrder.GetExtraReward]: gradeIndexMappingId not found gradeIndexMappingId = {0}", gradeIndexMappingId);
                    return null;
                }
                //获取订单奖励
                var reward = Game.Manager.mergeItemMan.GetOrderRewardConfig(orderRewardId).Reward;
                var levelRate = 0;
                if (BoardViewWrapper.IsMainBoard())
                {
                    levelRate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();
                }
                //构造奖励
                foreach (var r in reward)
                {
                    var (cfgID, cfgCount, param) = r.ConvertToInt3();
                    var (id, count) = Game.Manager.rewardMan.CalcDynamicReward(cfgID, cfgCount, levelRate, 0, param);
                    listR.Add(new Config.RewardConfig()
                    {
                        Id = id,
                        Count = count,
                    });
                }
                return listR;
            }
            //该随机订单槽位没有额外奖励
            return null;
        }

        #region IActivityOrderHandler

        public static string GetExtraRewardMiniThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var cfg = Data.GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == EventType.OrderExtra);
                paramId = cfg?.EventParam ?? 0;
            }
            if (paramId == 0)
            {
                DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }
            var cfgDetail = Data.GetEventOrderExtra(paramId);
            if (cfgDetail.Location)
            {
                DebugEx.Warning($"wrong location for {eventId} {paramId}");
                return string.Empty;
            }
            return cfgDetail.OrderBg;
        }

        public static string GetOrderThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var cfg = Data.GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == EventType.OrderExtra);
                paramId = cfg?.EventParam ?? 0;
            }
            if (paramId == 0)
            {
                DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }
            var cfgDetail = Data.GetEventOrderExtra(paramId);
            return cfgDetail.OrderTheme;
        }

        (int key_eventId, int key_eventParam, int key_rewardId, int key_rewardNum) GetOrderParamKey()
        {
            if (ConfD.Location)
            {
                // 常规
                return ((int)OrderParamType.ExtraBonusEventId,
                    (int)OrderParamType.ExtraBonusEventParam,
                    (int)OrderParamType.ExtraBonusRewardId,
                    (int)OrderParamType.ExtraBonusRewardNum);
            }
            else
            {
                // 额外
                return ((int)OrderParamType.ExtraBonusEventId_Mini,
                    (int)OrderParamType.ExtraBonusEventParam_Mini,
                    (int)OrderParamType.ExtraBonusRewardId_Mini,
                    (int)OrderParamType.ExtraBonusRewardNum_Mini);
            }
        }

        bool IActivityOrderHandler.IsValidForBoard(int boardId)
        {
            return ConfD.BoardId == boardId;
        }
        /// <summary>
        /// V39，限时合成订单用到了这里，但是两个活动开启等级不一样，所以需要单独处理
        /// 44对应位置是商业化总表 - EventOrderExtra id
        /// </summary>
        /// <returns></returns>
        bool IsBlockByOtherActivity()
        {
            if (ConfD.Id == 44)
            {
                return !Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureLimitMerge);
            }
            return false;
        }
        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (_muted)
                return false;
            if (IsBlockByOtherActivity())
            {
                return false;
            }
            if ((order as IOrderData).IsMagicHour)
                return false;
            if (ConfD == null)
                return false;
            var changed = false;
            var shouldAddExtraReward = false;
            var (key_eventId, key_eventParam, key_rewardId, key_rewardNum) = GetOrderParamKey();

            var state = order.GetState(key_eventId);
            if (state == null)
            {
                if (_IsRewardAvailableForSlot(order.Id))
                {
                    shouldAddExtraReward = true;
                }
            }
            else if (state.Value != Id)
            {
                // 不是同一期活动
                OrderUtility.RemoveOrderRewardByStateKey(order, key_rewardId, key_rewardNum);
                if (_IsRewardAvailableForSlot(order.Id))
                {
                    shouldAddExtraReward = true;
                }
            }
            if (shouldAddExtraReward)
            {
                // 尝试在头部添加一个奖励
                if (ConfD.RewardGrpInfo.TryGetValue(order.Id, out var rewardGrpId))
                {
                    changed = true;

                    var rewardConfId = Game.Manager.userGradeMan.GetTargetConfigDataId(rewardGrpId);
                    // 只有一个额外奖励
                    var reward = Game.Manager.mergeItemMan.GetOrderRewardConfig(rewardConfId).Reward[0];
                    var (_cfg_id, _cfg_count, _param) = reward.ConvertToInt3();
                    // 获取等级系数
                    Game.Manager.mergeLevelMan.TryGetLevelRate(helper.GetBoardLevel(), out var levelRate);
                    // 计算实际难度
                    var difficulty = OrderUtility.CalcRealDifficultyForRequires(order.Record.RequireIds);
                    // 计算实际奖励
                    var (_id, _count) = Game.Manager.rewardMan.CalcDynamicReward(_cfg_id, _cfg_count, levelRate, difficulty, _param);
                    DebugEx.Info($"ActivityExtraRewardOrder calc reward ---> [id {_cfg_id}]:[num {_cfg_count}]:[lv {levelRate}]:[dffy {difficulty}]:[type {_param}] => {_id}:{_count}");
                    // 修改data
                    order.Rewards.Insert(0, new Config.RewardConfig()
                    {
                        Id = _id,
                        Count = _count,
                    });
                    // 修改record
                    order.Record.RewardIds.Insert(0, _id);
                    order.Record.RewardNums.Insert(0, _count);

                    UpdateRecord(key_eventId, Id, order.Record.Extra);
                    UpdateRecord(key_eventParam, Param, order.Record.Extra);
                    UpdateRecord(key_rewardId, _id, order.Record.Extra);
                    UpdateRecord(key_rewardNum, _count, order.Record.Extra);
                }
            }
            return changed;
        }

        // 判断订单slot是否适配
        private bool _IsRewardAvailableForSlot(int slotId)
        {
            return ConfD.RewardGrpInfo.ContainsKey(slotId);
        }

        public string BoardEntryAsset()
        {
            if (VisualEntry.Theme != null)
            {
                VisualEntry.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
                return key;
            }
            return null;
        }

        public bool BoardEntryVisible => IsShowEntry;

        #endregion
    }
}
