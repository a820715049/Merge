/*

 * @Author: qun.chao
 * @Date: 2025-07-14 12:24:13
 */
using FAT.Merge;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using static fat.conf.Data;
using EL;
using UnityEngine;

namespace FAT
{
    public class ActivityClawOrder : ActivityLike, IBoardEntry, IActivityOrderHandler
    {
        // public struct RewardParam : IParamProvider
        // {
        //     public int orderId;
        //     public int payDffy;
        // }

        public override bool Valid => Lite.Valid && _conf != null;

        public EventClawOrderGroup ConfDetail => _confDetail;

        public int DisplayToken => curToken - _flyingToken;
        public int CurToken => curToken;
        public int MaxToken => _maxToken;
        public int SyncedToken => _lastSyncTokenNum;
        public int DrawAttemptCount => drawAttemptCount;
        public int SelectedOrderId => _selectedOrderId;

        #region UI
        public ActivityVisual mainVisual = new();
        public ActivityVisual endVisual = new();
        public PopupActivity popupStart = new();
        public PopupClawOrder popupMain = new();
        public PopupClawOrder popupEnd = new();
        public UIResAlt mainRes = new(UIConfig.UIClawOrderPanel);
        public UIResAlt endRes = new(UIConfig.UIClawOrderEnd);
        #endregion

        // 已完成全部订单
        private bool IsAllClear => curCompletedOrderCount >= _confDetail.TokenMilestone.Count;

        #region 存档字段
        // 当前数值模版id
        private int curTemplateId { get; set; }
        // 当前已完成订单数
        private int curCompletedOrderCount { get; set; }
        // 当前累积分数
        private int curToken { get; set; }
        // 当前已抽奖次数
        private int drawAttemptCount { get; set; }
        // 上次设置订单积分的时间
        private int lastSetOrderTokenTime { get; set; }
        // 上次领取订单积分的时间
        private int lastRewardOrderTokenTime { get; set; }
        #endregion

        // 当前选中的订单id
        private int _selectedOrderId = 0;
        // 上次发起尝试的帧数
        private int _lastTryFrame = 0;
        // 当前帧内应当处理的订单数
        private int _expectedOrderNum = 0;
        // 当前最大可领取的积分
        private int _maxToken = 0;
        // 正在飞奖励的积分
        private int _flyingToken = 0;
        // 上次同步的积分数量 | 用于UI开启后进行动画状态同步
        private int _lastSyncTokenNum = 0;

        private EventClawOrder _conf;
        private EventClawOrderGroup _confDetail;

        // 活动关注的全部订单slot
        private Dictionary<int, OrderRandomer> _validSlotMap = new();
        // 当前可工作的slot
        private Dictionary<int, OrderRandomer> _activeSlotMap = new();
        // 一次处理流程中需要考虑的所有订单实例
        private Dictionary<int, OrderData> _recordedOrderMap = new();


        public ActivityClawOrder(ActivityLite lite_)
        {
            Lite = lite_;
        }

        public override void WhenReset()
        {
            Cleanup();
        }

        public override void WhenEnd()
        {
            EndConvert();
            Cleanup();
        }

        /// <summary>
        /// 活动结束结算
        /// </summary>
        private void EndConvert()
        {
            var detail = _confDetail;
            if (detail != null)
            {
                var totalDrawCount = CalcTotalDrawChanceCountByToken(curToken);
                RewardCommitData reward = null;
                if (totalDrawCount > drawAttemptCount)
                {
                    // 需要转换
                    var left = totalDrawCount - drawAttemptCount;
                    using var _1 = PoolMapping.PoolMappingAccess.Borrow(out List<RewardCommitData> list);
                    using var _2 = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<int, int> map);
                    // 约定配0
                    map.Add(0, left);
                    ActivityExpire.ConvertToReward(detail.ExpireItem, list, ReasonString.order_claw, token_:map);
                    if (list.Count > 0)
                    {
                        reward = list[0];
                        list.RemoveAt(0);
                    }
                    if (list.Count> 0)
                    {
                        var pos = UIUtility.GetScreenCenterWorldPosForUICanvas();
                        UIFlyUtility.FlyRewardList(list, pos);
                    }
                    // track
                    if (reward != null)
                    {
                        DataTracker.event_claworder_end_reward.Track(this,
                            left,
                            $"{reward.rewardId}:{reward.rewardCount}");
                    }
                }
                Info($"EndConvert: {totalDrawCount} {drawAttemptCount} | {reward}");
                Game.Manager.screenPopup.TryQueue(popupEnd, (PopupType)(-1), reward);
            }
        }

        public static void DebugReset()
        {
            if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.ClawOrder, out var act))
            {
                (act as ActivityClawOrder).DebugClear();
            }
        }

        private void DebugClear()
        {
            // 丢弃存档
            curCompletedOrderCount = 0;
            curToken = 0;
            drawAttemptCount = 0;
            lastSetOrderTokenTime = 0;
            lastRewardOrderTokenTime = 0;

            // 重置状态
            _selectedOrderId = 0;
            _lastTryFrame = 0;
            _expectedOrderNum = 0;
            _flyingToken = 0;
            _lastSyncTokenNum = 0;

            MessageCenter.Get<MSG.CLAWORDER_TOKEN_COMMIT>().Dispatch();
        }

        private void Cleanup()
        {
            UnregisterEvent();
            _flyingToken = 0;
        }

        public void SyncToken()
        {
            _lastSyncTokenNum = curToken;
            MessageCenter.Get<MSG.CLAWORDER_CHANGE>().Dispatch();
        }

        /// <summary>
        /// 回收订单上的积分
        /// </summary>
        public bool TryClaimOrderToken(IOrderData order, int rewardId, int rewardNum, out RewardCommitData commitData)
        {
            commitData = Game.Manager.rewardMan.BeginReward(rewardId, rewardNum, ReasonString.order_claw);
            // 更新已完成的活动订单次数
            ++curCompletedOrderCount;
            // 当前没有选中
            _selectedOrderId = 0;
            // 更新上次领取积分的时间
            lastRewardOrderTokenTime = (int)Game.TimestampNow();

            // track
            var t = curToken;
            DataTracker.event_claworder_submit.Track(this,
                ConfDetail.Diff,
                curCompletedOrderCount,
                ConfDetail.TokenMilestone.Count,
                t,
                CalcDrawQueue(t),
                ConfDetail.DrawMilestone.Count,
                CalcTotalDrawChanceCountByToken(t),
                order.PayDifficulty,
                lastRewardOrderTokenTime - lastSetOrderTokenTime,
                order.Id,
                IsAllClear,
                $"{rewardId}:{rewardNum}");

            return true;
        }

        /// <summary>
        /// 由奖励系统发放积分
        /// </summary>
        public void AddToken(int id, int count)
        {
            if (id != _conf.TokenId)
                return;
            _flyingToken += count;
            var tokenAfter = curToken + count;
            if (tokenAfter > MaxToken)
            {
                tokenAfter = MaxToken;
            }
            curToken = tokenAfter;
            // Info($"AddToken: {count} {curToken}");
        }

        /// <summary>
        /// 结算飞奖励的积分
        /// </summary>
        public void ResolveFlyingToken(int amount)
        {
            // Info($"ResolveFlyingToken: {amount}");
            if (_flyingToken >= amount)
            {
                _flyingToken -= amount;
            }
            if (_flyingToken <= 0)
            {
                if (SyncedToken < curToken)
                {
                    if (CalcTotalDrawChanceCountByToken(SyncedToken) <
                        CalcTotalDrawChanceCountByToken(curToken))
                    {
                        // 进度奖励有变化 应该弹窗展示
                        Game.Manager.screenPopup.TryQueue(popupMain, (PopupType)(-1), true);
                    }
                }
            }
            MessageCenter.Get<MSG.CLAWORDER_TOKEN_COMMIT>().Dispatch();
        }

        /// <summary>
        /// 计算当前的抽奖获取 所处里程碑(从0开始)
        /// </summary>
        private int CalcDrawQueue(int token)
        {
            var idx = 0;
            var total = 0;
            var drawRewards = _confDetail.DrawMilestone;
            for (var i = 0; i < drawRewards.Count; i++)
            {
                var cfg = GetEventClawOrderDraw(drawRewards[i]);
                total += cfg.TokenCount;
                if (token >= total)
                {
                    idx = i + 1;
                }
                else
                {
                    break;
                }
            }
            return idx;
        }

        /// <summary>
        /// 计算当前的抽奖次数 所处里程碑(从0开始)
        /// </summary>
        private int CalcUseDrawQueue()
        {
            var idx = 0;
            var triedCount = drawAttemptCount;
            var rewardDiffs = _confDetail.RewardDiffMilestone;
            for (var i = 0; i < rewardDiffs.Count; i++)
            {
                var cfg = GetEventClawOrderReDiff(rewardDiffs[i]);
                if (triedCount >= cfg.DrawIndex)
                {
                    idx = i;
                }
                else
                {
                    break;
                }
            }
            return idx;
        }

        /// <summary>
        /// 是否是最后一次抽奖
        /// </summary>
        private bool IsFinalDraw()
        {
            var totalDrawCount = 0;
            foreach (var diff in _confDetail.DrawMilestone)
            {
                var cfg = GetEventClawOrderDraw(diff);
                totalDrawCount += cfg.DrawCount;
            }
            return drawAttemptCount >= totalDrawCount;
        }

        /// <summary>
        /// 根据积分计算对应的总抽奖次数
        /// </summary>
        public int CalcTotalDrawChanceCountByToken(int token)
        {
            var drawCount = 0;
            var tokenCount = 0;
            foreach (var draw in _confDetail.DrawMilestone)
            {
                var cfg = GetEventClawOrderDraw(draw);
                tokenCount += cfg.TokenCount;
                if (token >= tokenCount)
                {
                    drawCount += cfg.DrawCount;
                }
                else
                {
                    break;
                }
            }
            return drawCount;
        }

        /// <summary>
        /// 根据奖励序号计算对应要求的积分 和 获得的抽奖总数 和 资源id
        /// </summary>
        public (int token, int drawCount, int resId) CalcTokenByRewardIdx(int idx)
        {
            var drawRewards = _confDetail.DrawMilestone;
            if (idx < 0 || idx >= drawRewards.Count)
                return (0, 0, 0);
            var token = 0;
            var drawCount = 0;
            var resId = 0;
            for (var i = 0; i <= idx; ++i)
            {
                var cfg = GetEventClawOrderDraw(drawRewards[i]);
                token += cfg.TokenCount;
                drawCount += cfg.DrawCount;
                resId = cfg.RewardIconId;
            }
            // Info($"CalcTokenByRewardIdx: {idx} {token} {drawCount} {resId}");
            return (token, drawCount, resId);
        }

        /// <summary>
        /// 根据积分计算当前显示的资源id
        /// </summary>
        public int CalcClawResByToken(int token)
        {
            // 没够到阶段1时特殊显示为剪影
            var drawRewards = _confDetail.DrawMilestone;
            if (token < GetEventClawOrderDraw(drawRewards[0]).TokenCount)
            {
                return 0;
            }

            var resId = 0;
            var tokenRequire = 0;
            var attempts = drawAttemptCount;
            for (var i = 0; i < drawRewards.Count; i++)
            {
                var cfg = GetEventClawOrderDraw(drawRewards[i]);
                resId = cfg.RewardIconId;
                tokenRequire += cfg.TokenCount;
                if (tokenRequire <= token)
                {
                    // token够
                    if (cfg.DrawCount > attempts)
                    {
                        // 获得次数大于消费次数 | 当前阶段没抽完 显示当前res
                        break;
                    }
                    else
                    {
                        // 先消费完当前阶段的次数 | 进入下一阶段
                        attempts -= cfg.DrawCount;
                    }
                }
                else
                {
                    // 没达成时也要显示当前目标阶段的res
                    break;
                }
            }
            return resId;
        }

        /// <summary>
        /// 根据当前积分计算下一个里程碑的积分
        /// </summary>
        public int FindNextTokenMilestone(int nowToken)
        {
            var drawRewards = _confDetail.DrawMilestone;
            var nextToken = 0;
            for (var i = 0; i < drawRewards.Count; i++)
            {
                var cfg = GetEventClawOrderDraw(drawRewards[i]);
                nextToken += cfg.TokenCount;
                if (nowToken < nextToken)
                {
                    break;
                }
            }
            return nextToken;
        }

        /// <summary>
        /// 根据抽奖id查询对应的资源
        /// </summary>
        public EventClawOrderDraw GetConfigDrawInfo(int drawId)
        {
            return GetEventClawOrderDraw(drawId);
        }

        /// <summary>
        /// 根据资源id获取资源配置
        /// </summary>
        public EventClawOrderResource GetConfigDrawRes(int resId)
        {
            return GetEventClawOrderResource(resId);
        }

        /// <summary>
        /// 发起抽奖
        /// </summary>
        public bool TryDraw(out RewardCommitData reward)
        {
            reward = null;
            if (CalcTotalDrawChanceCountByToken(curToken) <= drawAttemptCount)
            {
                return false;
            }
            // 奖励难度区间
            var (diff_left, diff_right) = (0, 0);
            // 轮到第几次抽奖
            var nextDrawCount = drawAttemptCount + 1;
            var rewardDiffs = _confDetail.RewardDiffMilestone;
            for (var i = 0; i < rewardDiffs.Count; i++)
            {
                var cfg = GetEventClawOrderReDiff(rewardDiffs[i]);
                if (cfg.DrawIndex > nextDrawCount)
                {
                    break;
                }
                (diff_left, diff_right) = (cfg.RewardDiffRange[0], cfg.RewardDiffRange[1]);
            }
            var (itemId, fallback) = Game.Manager.mergeItemDifficultyMan.CalcSpecialBoxOutputWithFallbackState(diff_left, diff_right);
            if (itemId <= 0)
            {
                Error($"TryDraw: failed to calc itemId");
                return false;
            }

            ++drawAttemptCount;
            MessageCenter.Get<MSG.CLAWORDER_CHANGE>().Dispatch();

            reward = Game.Manager.rewardMan.BeginReward(itemId, 1, ReasonString.order_claw);
            Info($"TryDraw: {drawAttemptCount} => [{diff_left},{diff_right}] => {itemId}");

            // track
            Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(itemId, out _, out var diff);
            var isFinal = IsFinalDraw();
            var t = curToken;
            DataTracker.event_claworder_drawitem_spawn.Track(this,
                ConfDetail.Diff,
                curCompletedOrderCount,
                ConfDetail.TokenMilestone.Count,
                t,
                CalcDrawQueue(t),
                ConfDetail.DrawMilestone.Count,
                CalcTotalDrawChanceCountByToken(t),
                CalcUseDrawQueue() + 1,
                ConfDetail.RewardDiffMilestone.Count,
                drawAttemptCount,
                isFinal,
                $"[{diff_left},{diff_right}]",
                itemId,
                diff,
                ItemUtility.GetItemLevel(itemId),
                fallback);

            if (isFinal)
            {
                Game.Manager.activity.EndImmediate(this, false);
            }

            return true;
        }

        #region save / load / init
        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            any.Add(ToRecord(i++, curTemplateId));
            any.Add(ToRecord(i++, curCompletedOrderCount));
            any.Add(ToRecord(i++, curToken));
            any.Add(ToRecord(i++, drawAttemptCount));
            any.Add(ToRecord(i++, lastSetOrderTokenTime));
            any.Add(ToRecord(i++, lastRewardOrderTokenTime));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            curTemplateId = ReadInt(i++, any);
            curCompletedOrderCount = ReadInt(i++, any);
            curToken = ReadInt(i++, any);
            drawAttemptCount = ReadInt(i++, any);
            lastSetOrderTokenTime = ReadInt(i++, any);
            lastRewardOrderTokenTime = ReadInt(i++, any);
            InitConf();
            InitTheme();
            RegisterEvent();
            SyncToken();
        }

        public override void SetupFresh()
        {
            InitConf();
            InitTheme();
            RegisterEvent();
            Game.Manager.screenPopup.TryQueue(popupStart, PopupType.Login);
        }

        public override void Open()
        {
            popupMain.OpenPopup();
        }

        private void RegisterEvent() { }

        private void UnregisterEvent() { }

        private void InitConf()
        {
            // 基础配置
            _conf = GetEventClawOrder(Lite.Param);
            if (curTemplateId <= 0)
            {
                // 分层数据进存档 只有首次参加活动才会设置
                curTemplateId = Game.Manager.userGradeMan.GetTargetConfigDataId(_conf.GradeId);
            }
            // 模版详情
            _confDetail = GetEventClawOrderGroup(curTemplateId);
            foreach (var token in _confDetail.TokenMilestone)
            {
                _maxToken += GetEventClawOrderToken(token).TokenCollectCount;
            }

            // 提前记录关心的订单id
            _validSlotMap.Clear();
            var orders = GetOrderRandomerMap();
            foreach (var order in orders.Values)
            {
                if (order.IsOrderClaw)
                {
                    _validSlotMap.Add(order.Id, order);
                }
            }
        }

        private void InitTheme()
        {
            var cfg = _conf;
            mainVisual.Setup(cfg.EventTheme, mainRes);
            endVisual.Setup(cfg.EndTheme, endRes);

            popupStart.Setup(this, mainVisual, mainRes);
            popupMain.Setup(this, mainVisual, mainRes);
            popupEnd.Setup(this, endVisual, endRes);
        }

        #endregion

        #region IActivityOrderHandler

        // 挂接积分不影响订单的完成状态 允许返回false
        // 此处应该在订单被改变后通知UI进行刷新
        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            // 订单已结束
            if (order.State == OrderState.Rewarded)
            {
                return false;
            }
            // 订单已发完
            if (IsAllClear)
            {
                return false;
            }
            // 当前已有选中的订单 无需处理
            if (_selectedOrderId != 0)
            {
                return false;
            }
            // 读档时发现当前订单就是活动目标订单
            if ((order as IOrderData).IsClawOrder)
            {
                _selectedOrderId = order.Id;
                _recordedOrderMap.Clear();
                Info($"OnPreUpdate: found order {_selectedOrderId}");
                return false;
            }
            // 首次进入当前frame 进行必要的初始化
            if (_lastTryFrame != UnityEngine.Time.frameCount)
            {
                _lastTryFrame = UnityEngine.Time.frameCount;
                RefreshActiveSlots(helper);
            }
            OnPreUpdateOrder(order);
            return false;
        }

        #endregion

        private void RefreshActiveSlots(IOrderHelper helper)
        {
            // 首次开始统计
            using var _1 = PoolMapping.PoolMappingAccess.Borrow<List<IOrderData>>(out var activeOrders);
            using var _2 = PoolMapping.PoolMappingAccess.Borrow<HashSet<int>>(out var idSet);
            Game.Manager.mainOrderMan.FillActiveOrders(activeOrders, (int)OrderProviderTypeMask.Random);
            foreach (var od in activeOrders)
            {
                idSet.Add(od.Id);
            }
            // 更新可用的slot集合
            _activeSlotMap.Clear();
            foreach (var slot in _validSlotMap)
            {
                if (CheckSlotReady(slot.Value, idSet, helper))
                {
                    _activeSlotMap.Add(slot.Key, slot.Value);
                }
            }
            // 应处理的订单数量
            _expectedOrderNum = _activeSlotMap.Count;
            // 已记录的订单
            _recordedOrderMap.Clear();
#if UNITY_EDITOR
            Info($"RefreshActiveSlots: {_activeSlotMap.Count}");
#endif
        }

        private bool CheckSlotReady(OrderRandomer cfg, HashSet<int> idSet_, IOrderHelper helper)
        {
            foreach (var id in cfg.ShutdownRandId)
            {
                if (idSet_.Contains(id))
                {
                    return false;
                }
            }
            var ready = helper.CheckStateByConditionGroup(cfg.ActiveLevel, cfg.ShutdownLevel, cfg.ActiveOrderId, cfg.ShutdownOrderId, cfg.ActiveItemId, cfg.ShutdownItemId);
            return ready;
        }

        private void OnPreUpdateOrder(OrderData curOrder)
        {
            if (!_activeSlotMap.ContainsKey(curOrder.Id))
                return;
            _recordedOrderMap.TryAdd(curOrder.Id, curOrder);
            if (_recordedOrderMap.Count >= _expectedOrderNum)
            {
                // 要求的slot都已开始工作 可以开始挂接积分
                if (TryAddTokenToOrder(out var targetOrder))
                {
                    // 星想事成存在 不应滚动订单列表
                    if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.Wishing, out _))
                        return;
                    targetOrder.HasScrollRequest = true;
                }
            }
        }

        private bool TryAddTokenToOrder(out OrderData targetOrder)
        {
            // 排序 1. 距离区间中点近 2. 需要的物品数量多
            static int Sort((int dist, int requireCount, OrderData od) a, (int dist, int requireCount, OrderData od) b)
            {
                if (a.dist != b.dist)
                    return a.dist - b.dist;
                return -(a.requireCount - b.requireCount);
            }

            targetOrder = null;
            var tokenDetail = GetEventClawOrderToken(_confDetail.TokenMilestone[curCompletedOrderCount]);
            var (pay_min, pay_max) = (tokenDetail.OrderPayDiff[0], tokenDetail.OrderPayDiff[1]);
            var pay_mid = (pay_min + pay_max) / 2;
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<(int dist, int requireCount, OrderData od)>>(out var candidateOrders);

            // 检查所有已记录的订单
            foreach (var order in _recordedOrderMap.Values)
            {
                // 忽略可完成和已完成的订单
                if (order.State == OrderState.Finished || order.State == OrderState.Rewarded)
                    continue;
                // 统计 总付出难度 / 物品需求数量
                var totalPayDffy = (order as IOrderData).PayDifficulty;
                if (totalPayDffy >= pay_min && totalPayDffy <= pay_max)
                {
                    var totalItemCount = 0;
                    foreach (var item in order.Requires)
                    {
                        totalItemCount += item.TargetCount;
                    }
                    var diff = UnityEngine.Mathf.Abs(totalPayDffy - pay_mid);
                    candidateOrders.Add((diff, totalItemCount, order));
                }
            }
            if (candidateOrders.Count < 1)
            {
                return false;
            }
            candidateOrders.Sort(Sort);
            targetOrder = candidateOrders[0].od;
            AddTokenToOrder(targetOrder, tokenDetail.TokenCollectCount);
            return true;
        }

        private void AddTokenToOrder(OrderData order, int amount)
        {
            order.AddTag(OrderTag.ClawOrder);
            OrderAttachmentUtility.slot_extra_tr.UpdateEventData(order, Id, Lite.Param, _conf.TokenId, amount);
            MessageCenter.Get<MSG.GAME_ORDER_REFRESH>().Dispatch(order);
            lastSetOrderTokenTime = (int)Game.TimestampNow();
            Info($"AddTokenToOrder: {order.Id} {amount}");

            // track
            var waitTime = 0;
            if (lastRewardOrderTokenTime > 0)
            {
                waitTime = lastSetOrderTokenTime - lastRewardOrderTokenTime;
            }
            var t = curToken;
            DataTracker.event_claworder_pick_success.Track(this,
                ConfDetail.Diff,
                curCompletedOrderCount + 1,
                ConfDetail.TokenMilestone.Count,
                t,
                CalcDrawQueue(t),
                ConfDetail.DrawMilestone.Count,
                CalcTotalDrawChanceCountByToken(t),
                (order as IOrderData).PayDifficulty,
                waitTime,
                order.Id);
        }

        public string GetEndConvertDescParam()
        {
            if (endVisual.AssetMap.TryGetValue("endConvert", out var result))
                return result;
            return null;
        }

        #region IBoardEntry
        string IBoardEntry.BoardEntryAsset() => GetRes("boardEntry");
        #endregion

        #region res
        public static string GetOrderAttachmentRes() => GetRes("orderSlot");
        public static string GetOrderThemeRes() => GetRes("orderItem");

        private static string GetRes(string key)
        {
            if (!Game.Manager.activity.LookupAny(fat.rawdata.EventType.ClawOrder, out var act))
                return null;
            if (act is not ActivityClawOrder inst)
                return null;
            if (inst.mainVisual.AssetMap.TryGetValue(key, out var result))
                return result;
            return null;
        }
        #endregion

        private string logTag => $"[{nameof(ActivityClawOrder)}]";

        private void Info(string msg)
        {
            DebugEx.Info($"{logTag} {msg}");
        }

        private void Error(string msg)
        {
            DebugEx.Error($"{logTag} {msg}");
        }
    }
}