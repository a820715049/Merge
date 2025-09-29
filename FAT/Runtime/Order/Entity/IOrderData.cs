/*
 * @Author: qun.chao
 * @Date: 2023-10-26 18:39:51
 */
using System;
using System.Collections.Generic;
using Config;
using fat.gamekitdata;
using fat.rawdata;

namespace FAT
{
    public enum OrderType
    {
        Normal = 0,
        Flash = 1,      // 限时订单
        Counting = 2,   // 次数订单
        Step = 3,       // 阶梯订单
        Challenge = 4,  // 连续限时订单活动 零度挑战
        MagicHour = 5,  // 星想事成活动 随机订单
        OrderDash = 6,
        Streak = 7,      // 连续订单活动
        LimitMergeOrder = 8, // 限时合成订单
        DiffChoice = 9, //自选限时订单
    }

    // 只增不删 避免错误解析用户存档
    public enum OrderParamType
    {
        EventId = 1,        // 独占活动的id
        StartTimeSec,       // 订单开始时间
        DurationSec,        // 订单持续时间
        OrderCountFrom,
        OrderCountRequire,
        OrderCountTotal,
        ScoreEventId,       // 积分活动id
        Score,              // 积分值
        ExtraBonusEventId,      // 额外奖励 活动id
        ExtraBonusRewardId,     // 额外奖励 奖励id
        ExtraBonusRewardNum,    // 额外奖励 奖励数量
        PayDifficulty,          // 订单生成时的付出难度
        ActDifficulty,          // 订单生成时的实际难度
        EventParam,
        ExtraBonusEventParam,   // 额外奖励 活动配置id
        ApiOrderStatus,         // 订单API OrderApiStatus
        ExtraBonusEventId_Mini,      // 额外奖励 右上角 活动id
        ExtraBonusEventParam_Mini,   // 额外奖励 右上角 活动配置id
        ExtraBonusRewardId_Mini,     // 额外奖励 右上角 奖励id
        ExtraBonusRewardNum_Mini,    // 额外奖励 右上角 奖励数量
        ScoreEventIdBR,       // 订单右下角积分活动id
        ScoreBR,              // 订单右下角积分值
        ExtraSlot_TL_EventId,        // 额外奖励槽 左上角 活动id | 当前用于好评订单
        ExtraSlot_TL_EventParam,     // 额外奖励槽 左上角 活动配置id
        ExtraSlot_TL_RewardId,       // 额外奖励槽 左上角 奖励id
        ExtraSlot_TL_RewardNum,      // 额外奖励槽 左上角 奖励数量
        ExtraSlot_TR_EventId,        // 额外奖励槽 右上角 活动id | 有右上角额外奖励冲突
        ExtraSlot_TR_EventParam,     // 额外奖励槽 右上角 活动配置id
        ExtraSlot_TR_RewardId,       // 额外奖励槽 右上角 奖励id
        ExtraSlot_TR_RewardNum,      // 额外奖励槽 右上角 奖励数量
        ScoreRewardBR,               // 订单右下角奖励id
        TAG_MASK,                   // 订单标签 | 订单可能同时具备多个标签
    }

    [Flags]
    public enum OrderTag
    {
        None = 0,
        // 抓宝订单
        ClawOrder = 1 << 0,
    }

    public enum OrderState
    {
        NotStart,
        PreShow,
        OnGoing,
        Finished,
        Rewarded,
        Expired,
    }

    public enum OrderApiStatus
    {
        None,       // 不涉及api
        Requesting, // 正在请求api
        Timeout,    // api太慢
        UseApi,     // 使用api
    }

    public class ItemCountInfo
    {
        public int Id;
        public int CurCount;
        public int TargetCount;
    }

    public class OrderData : IOrderData
    {
        public OrderState State { get; set; }
        public int Id { get; set; }
        public int OrderType { get; set; }
        public int ProviderType { get; set; }
        public int RoleId { get; set; }
        public int UnlockLevel { get; set; }
        public List<ItemCountInfo> Requires { get; set; }
        public List<RewardConfig> Rewards { get; set; }
        public bool Displayed { get => Record.Displayed; set => Record.Displayed = value; }
        public int DffyStrategy { get => Record.DffyStrategy; set => Record.DffyStrategy = value; }
        public int GetValue(OrderParamType paramKey) { return RecordStateHelper.ReadInt((int)paramKey, Record.Extra); }

        #region conf
        public OrderRandomer ConfRandomer { get; set; }
        #endregion

        #region 星想事成
        public int FallbackItemId { get; set; }
        public (int min, int max) RewardDffyRange { get; set; }
        public int MagicHourTimeLifeMilli { get; set; }
        public int MagicHourTimeDurationMilli { get; set; }
        #endregion

        #region OrderBonus
        public int BonusID { get; set; }
        public int BonusEndTime { get; set; }
        public int BonusPhase { get; set; }
        public int BonusEventID { get; set; }
        public bool needBonusAnim { get; set; }
        #endregion

        #region 订单滚屏需求
        public bool HasScrollRequest { get; set; }
        #endregion

        public bool ShouldNotChange { get; set; }
        public Func<IOrderData, bool> RemoteOrderResolver { get; set; }

        public AnyState GetState(int id)
        {
            foreach (var state in Record.Extra)
            {
                if (state.Id == id)
                    return state;
            }
            return null;
        }

        public void AddTag(OrderTag flag)
        {
            var mask = GetValue(OrderParamType.TAG_MASK);
            RecordStateHelper.UpdateRecord((int)OrderParamType.TAG_MASK, mask | (int)flag, Record.Extra);
        }

        public void RemoveTag(OrderTag flag)
        {
            var mask = GetValue(OrderParamType.TAG_MASK);
            RecordStateHelper.UpdateRecord((int)OrderParamType.TAG_MASK, mask & ~(int)flag, Record.Extra);
        }

        public OrderRecord Record { get; set; }
    }


    public interface IOrderData
    {
        OrderState State { get; }
        int Id { get; }
        int OrderType { get; }
        int ProviderType { get; }
        int RoleId { get; }
        int UnlockLevel { get; }
        List<ItemCountInfo> Requires { get; }
        List<RewardConfig> Rewards { get; }
        bool Displayed { get; set; }
        int DffyStrategy { get; set; }
        int GetValue(OrderParamType paramKey);

        bool ShouldNotChange { get; set; }
        Func<IOrderData, bool> RemoteOrderResolver { get; set; }

        #region tag
        bool HasTag(OrderTag flag) => (GetValue(OrderParamType.TAG_MASK) & (int)flag) != 0;
        #endregion

        #region pay/act difficulty
        int PayDifficulty => GetValue(OrderParamType.PayDifficulty);
        int ActDifficulty => GetValue(OrderParamType.ActDifficulty);
        #endregion

        #region api order
        bool IsApiOrder => ApiStatus == OrderApiStatus.UseApi;
        OrderApiStatus ApiStatus => (OrderApiStatus)GetValue(OrderParamType.ApiOrderStatus);
        #endregion

        #region commit count
        // 计数类订单
        bool IsCounting => OrderType == (int)FAT.OrderType.Counting;
        int OrderCountFrom => GetValue(OrderParamType.OrderCountFrom);
        int OrderCountRequire => GetValue(OrderParamType.OrderCountRequire);
        int OrderCountTotal => GetValue(OrderParamType.OrderCountTotal);
        #endregion

        #region expire
        bool IsExpired => Duration > 0 && Countdown <= 0 || IsMagicHourExpired;
        int Duration => GetValue(OrderParamType.DurationSec);
        long Countdown
        {
            get
            {
                var start = GetValue(OrderParamType.StartTimeSec);
                var duration = GetValue(OrderParamType.DurationSec);
                return start + duration - Game.Instance.GetTimestampSeconds();
            }
        }
        #endregion

        #region flash
        // 限时订单 | 共用了倒计时的逻辑
        bool IsFlash => OrderType == (int)FAT.OrderType.Flash || OrderType == (int)FAT.OrderType.DiffChoice || OrderType == (int)FAT.OrderType.Challenge || OrderType == (int)FAT.OrderType.OrderDash || OrderType == (int)FAT.OrderType.Streak || OrderType == (int)FAT.OrderType.LimitMergeOrder;
        #endregion

        #region score
        int Score => GetValue(OrderParamType.Score);
        int ScoreBR => GetValue(OrderParamType.ScoreBR);
        int ScoreRewardBR => GetValue(OrderParamType.ScoreRewardBR);
        #endregion

        #region orderlike
        // 好评订单
        int LikeId => GetValue(OrderParamType.ExtraSlot_TL_RewardId);
        int LikeNum => GetValue(OrderParamType.ExtraSlot_TL_RewardNum);
        #endregion

        #region orderrate
        int RateId => GetValue(OrderParamType.ExtraSlot_TR_RewardId);
        int RateNum => GetValue(OrderParamType.ExtraSlot_TR_RewardNum);
        #endregion
        #region step
        // 阶梯订单
        bool IsStep => OrderType == (int)FAT.OrderType.Step;
        #endregion

        #region magic hour
        // 星想事成 **数据不存档**
        int FallbackItemId { get; set; }
        (int min, int max) RewardDffyRange { get; set; }
        int MagicHourTimeLifeMilli { get; set; }
        int MagicHourTimeDurationMilli { get; set; }
        bool IsMagicHour => OrderType == (int)FAT.OrderType.MagicHour;
        bool IsMagicHourExpired => IsMagicHour && MagicHourTimeDurationMilli > 0 && MagicHourTimeLifeMilli >= MagicHourTimeDurationMilli;
        #endregion

        #region extra reward
        // 额外奖励
        bool HasExtraReward => GetValue(OrderParamType.ExtraBonusEventId) > 0;
        bool HasExtraRewardMini => GetValue(OrderParamType.ExtraBonusEventId_Mini) > 0;
        (int id, int num) ExtraRewardMini => (GetValue(OrderParamType.ExtraBonusRewardId_Mini), GetValue(OrderParamType.ExtraBonusRewardNum_Mini));
        #endregion

        #region claw order
        bool IsClawOrder => HasTag(OrderTag.ClawOrder);
        #endregion

        #region OrderBonus
        int BonusEventID { get; set; }
        int BonusID { get; set; }
        int BonusEndTime { get; set; }
        int BonusPhase { get; set; }
        bool needBonusAnim { get; set; }
        bool HasBonus => BonusEventID != 0;
        #endregion

        #region 订单滚屏需求
        bool HasScrollRequest { get; set; }
        #endregion

        int CalcRealDifficulty()
        {
            int realDifficulty = 0;
            foreach (var req in Requires)
            {
                Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(req.Id, out _, out var real);
                realDifficulty += real;
            }
            return realDifficulty;
        }

        bool ShouldOverrideOrderRes()
        {
            return IsFlash || IsStep || IsMagicHour || HasExtraReward || IsClawOrder || (HasBonus && !needBonusAnim);
        }

        #region 订单prefab
        bool TryGetOverrideRes(out string res)
        {
            res = null;
            if (IsFlash)
            {
                if (OrderType == (int)FAT.OrderType.Challenge)
                {
                    res = ActivityOrderChallenge.GetOrderThemeRes(GetValue(OrderParamType.EventId), GetValue(OrderParamType.EventParam));
                }
                else if (OrderType == (int)FAT.OrderType.OrderDash)
                {
                    res = ActivityOrderDash.GetOrderThemeRes(GetValue(OrderParamType.EventId), GetValue(OrderParamType.EventParam));
                }
                else if (OrderType == (int)FAT.OrderType.Flash)
                {
                    res = ActivityFlashOrder.GetOrderThemeRes(GetValue(OrderParamType.EventId), GetValue(OrderParamType.EventParam));
                }
                else if (OrderType == (int)FAT.OrderType.DiffChoice)
                {
                    res = ActivityOrderDiffChoice.GetOrderThemeRes(GetValue(OrderParamType.EventId), GetValue(OrderParamType.EventParam));
                }
                else if (OrderType == (int)FAT.OrderType.Streak)
                {
                    res = ActivityOrderStreak.GetOrderThemeRes(GetValue(OrderParamType.EventId), GetValue(OrderParamType.EventParam));
                }
                else if (OrderType == (int)FAT.OrderType.LimitMergeOrder)
                {
                    res = ActivityLimitMergeOrder.GetOrderThemeRes();
                }
            }
            else if (HasBonus)
            {
                res = ActivityOrderBonus.GetOrderThemeRes(BonusID);
            }
            else if (IsStep)
            {
                res = ActivityStep.GetOrderThemeRes(GetValue(OrderParamType.EventId), GetValue(OrderParamType.EventParam));
            }
            else if (IsMagicHour)
            {
                res = ActivityMagicHour.GetOrderThemeRes(GetValue(OrderParamType.EventId), GetValue(OrderParamType.EventParam));
            }
            else if (IsClawOrder)
            {
                res = ActivityClawOrder.GetOrderThemeRes();
            }
            else if (HasExtraReward)
            {
                res = ActivityExtraRewardOrder.GetOrderThemeRes(GetValue(OrderParamType.ExtraBonusEventId), GetValue(OrderParamType.ExtraBonusEventParam));
            }
            return !string.IsNullOrEmpty(res);
        }
        #endregion

        #region 订单挂件
        bool TryGetExtraRewardMiniRes(out string res)
        {
            res = ActivityExtraRewardOrder.GetExtraRewardMiniThemeRes(GetValue(OrderParamType.ExtraBonusEventId_Mini), GetValue(OrderParamType.ExtraBonusEventParam_Mini));
            return !string.IsNullOrEmpty(res);
        }

        bool TryGetOrderLikeRes(out string res)
        {
            if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderLike, out var _orderlike))
            {
                res = ActivityOrderLike.GetExtraRewardMiniThemeRes(GetValue(OrderParamType.ExtraSlot_TL_EventId), GetValue(OrderParamType.ExtraSlot_TL_EventParam));
            }
            else if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.Puzzle, out var _puzzle))
            {
                res = ActivityPuzzle.GetExtraRewardMiniThemeRes(GetValue(OrderParamType.ExtraSlot_TL_EventId), GetValue(OrderParamType.ExtraSlot_TL_EventParam));
            }
            else
            {
                res = null;
            }
            return !string.IsNullOrEmpty(res);
        }

        // 进度礼盒
        bool TryGetOrderRateRes(out string res)
        {
            res = ActivityOrderRate.GetExtraRewardMiniThemeRes(GetValue(OrderParamType.ExtraSlot_TR_EventId), GetValue(OrderParamType.ExtraSlot_TR_EventParam));
            return !string.IsNullOrEmpty(res);
        }

        // 订单助力
        bool TryGetOrderBonusRes(out string res)
        {
            res = ActivityOrderBonus.GetExtraRewardMiniThemeRes(GetValue(OrderParamType.ExtraSlot_TR_EventId), GetValue(OrderParamType.ExtraSlot_TR_EventParam));
            return !string.IsNullOrEmpty(res);
        }

        // 抓宝订单
        bool TryGetClawOrderRes(out string res)
        {
            res = ActivityClawOrder.GetOrderAttachmentRes();
            return !string.IsNullOrEmpty(res);
        }
        bool TryGetLimitMergeOrderRes(out string res)
        {
            res = ActivityLimitMergeOrder.GetOrderAttachmentRes();
            return !string.IsNullOrEmpty(res);
        }
        #endregion
    }
}
