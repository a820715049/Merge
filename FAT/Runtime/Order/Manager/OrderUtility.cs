/*
 * @Author: qun.chao
 * @Date: 2023-10-26 20:58:38
 */
using System.Collections;
using System.Collections.Generic;
using FAT.Merge;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using System;

namespace FAT
{
    public static class OrderUtility
    {
        public static void SetDebug(bool d)
        {
            if (d != sDebug)
            {
                sDebug = d;
                // Game.Instance.activityMan.CheckTask();
                // Game.Instance.schoolMan.OnMergeLevelChange();
                // Game.Instance.dailyTaskMan.UpdateAllTask();
            }
        }
        public static bool isDebug => sDebug;
        private static bool sDebug = false;

        public static bool UpdateOrderStatus(OrderData order, MergeWorldTracer tracer, IOrderHelper helper)
        {
            if (order.State == OrderState.Rewarded || order.State == OrderState.Expired)
            {
                return false;
            }

            // 统计时是否包括背包里的item
            var activeItems = Env.Instance.CanInventoryItemUseForOrder ?
                            tracer.GetCurrentActiveBoardAndInventoryItemCount() : tracer.GetCurrentActiveBoardItemCount();
            var previousState = order.State;
            bool changed = false;

            if (!helper.CheckCond_Level(order.UnlockLevel))
            {
                // 未解锁
                if (order.State == OrderState.NotStart)
                    order.State = OrderState.PreShow;
            }
            else
            {
                // 已解锁
                if (order is IOrderData orderHandler && orderHandler.IsCounting)
                {
                    // 计数订单更新进度
                    var total = helper.GetTotalFinished();
                    foreach (var state in order.Record.Extra)
                    {
                        if (state.Id == (int)OrderParamType.OrderCountTotal)
                        {
                            changed = state.Value != (int)total;
                            state.Value = (int)total;
                            break;
                        }
                    }
                    if (total >= orderHandler.OrderCountFrom + orderHandler.OrderCountRequire)
                    {
                        order.State = OrderState.Finished;
                        if (previousState == OrderState.Finished)
                        {
                            // 之前已经是完成状态 不需要触发change
                            changed = false;
                        }
                    }
                    else
                    {
                        order.State = OrderState.OnGoing;
                    }
                }
                else
                {
                    bool completed = true;
                    foreach (var item in order.Requires)
                    {
                        var newCount = activeItems.GetDefault(item.Id, 0);
                        var oldCount = item.CurCount;
                        if (oldCount != newCount)
                        {
                            item.CurCount = newCount;
                            changed = true;
                        }
                        completed = completed && item.CurCount >= item.TargetCount;
                    }
                    if (completed)
                    {
                        order.State = OrderState.Finished;
                        if (previousState == OrderState.Finished)
                        {
                            // 之前已经是完成状态 不需要触发change
                            changed = false;
                        }
                    }
                    else
                    {
                        order.State = OrderState.OnGoing;
                    }
                }
            }

            // 状态改变 尝试统计npc和需求item的数量
            if (order.State != previousState)
            {
                if (previousState == OrderState.PreShow)
                {
                    // 首次解锁
                    helper.UpdateOrderStatisticInfo(order, 1, 0);
                }
                else if (previousState == OrderState.NotStart)
                {
                    // 首次生成 or 读取存档
                    if (order.State == OrderState.PreShow)
                    {
                        // 锁定状态 仅显示npc
                        helper.UpdateOrderStatisticInfo(order, 0, 1);
                    }
                    else
                    {
                        // 非锁定
                        helper.UpdateOrderStatisticInfo(order, 1, 1);
                    }
                }
            }

#if !STRIP_DEBUG_CODE
            if (sDebug)
            {
                changed = order.State != OrderState.Finished;
                order.State = OrderState.Finished;
                return changed;
            }
#endif

            // 订单已解锁
            if (order.State != OrderState.PreShow)
            {
                // 当前不是预览锁定状态 说明npc和订单需求已显示
                if (order is IOrderData orderHandler && (orderHandler.IsFlash || orderHandler.IsStep || orderHandler.IsMagicHour))
                {
                    // 限时订单退场检查
                    if (orderHandler.IsExpired)
                    {
                        order.State = OrderState.Expired;
                        // 订单过期 准备退场 更新统计数量
                        helper.UpdateOrderStatisticInfo(order, -1, -1);
                    }
                }
            }

            bool isChange = changed || order.State != previousState;
            // 如果订单已登场且变动为可完成状态 则播音效
            if (isChange && order.Displayed && order.State == OrderState.Finished)
            {
                Game.Manager.audioMan.TriggerSound("OrderReward");
            }
            
            return isChange;
        }

        public static bool IsConsumeNeedConfirmation(OrderData order, MergeWorld world, List<Item> confirmList)
        {
            if (order.Requires.Count < 1)
                return false;
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<ItemConsumeRequest>>(out var reqList);
            foreach (var item in order.Requires)
            {
                reqList.Add(new ItemConsumeRequest() { itemId = item.Id, itemCount = item.TargetCount });
            }
            var canCommit = world.TryConsumeOrderItem(reqList, confirmList, true);
            return canCommit && confirmList.Count > 0;
        }

        public static bool TryFinishOrder(OrderData order, MergeWorldTracer tracer, IOrderHelper helper, ReasonString reason, ICollection<RewardCommitData> rewards, RewardFlags flags = RewardFlags.None)
        {
            UpdateOrderStatus(order, tracer, helper);
            int id = order.Id;
            if (order.State != OrderState.Finished)
            {
                DebugEx.FormatWarning("OrderUtility::FinishTask ----> not finish {0}:{1}", reason, order);
                return false;
            }
            var world = tracer.world;
            bool consumeSuccess = true;
            if (order.Requires.Count > 0)
            {
                using (ObjectPool<List<ItemConsumeRequest>>.GlobalPool.AllocStub(out var toConsume))
                {
                    foreach (var item in order.Requires)
                    {
                        toConsume.Add(new ItemConsumeRequest()
                        {
                            itemId = item.Id,
                            itemCount = item.TargetCount
                        });
                    }
                    consumeSuccess = world.TryConsumeOrderItem(toConsume, null, false);
                }
            }
            if (!consumeSuccess
#if !STRIP_DEBUG_CODE
                && !sDebug
#endif
                )
            {
                DebugEx.FormatWarning("OrderUtility::FinishTask ----> item not enough {0}: {1}", reason, id);
                return false;
            }
            DebugEx.FormatInfo("OrderUtility::FinishTask ----> task rewarded {0}:{1}", reason, id);
            if (rewards != null)
            {
                foreach (var reward in order.Rewards)
                {
                    rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.order, flags));
                }
            }
            var prevState = order.State;
            order.State = OrderState.Rewarded;
            // 订单完结 更新需求数量
            helper.UpdateOrderStatisticInfo(order, -1, -1);
            return true;
        }

        //计算订单的平均难度和实际难度
        //结果值用做公式计算时 需要除100
        public static void CalOrderDifficulty(IOrderData order, out int totalAvg, out int totalReal)
        {
            totalAvg = 0;
            totalReal = 0;
            foreach (var info in order.Requires)
            {
                if (Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(info.Id, out int avg, out int real))
                {
                    totalAvg += avg;
                    totalReal += real;
                }
            }
        }

        #region data

        // ref: https://centurygames.yuque.com/ywqzgn/ne0fhm/ux1astmzw3sars5l#gS20N
        // 找出备选角色中 使用次数最少的
        public static int DecideOrderRole(int roleId, IOrderHelper helper)
        {
            if (roleId > 0)
                return roleId;
            var pool = Game.Manager.npcMan.OrderNpcPool;
            var npcInUse = helper.NpcInUseCountDict;
            using (ObjectPool<List<NpcConfig>>.GlobalPool.AllocStub(out var list))
            {
                int minUseCount = 100000;
                foreach (var kv in pool)
                {
                    npcInUse.TryGetValue(kv.Key, out var count);
                    if (count < minUseCount)
                    {
                        minUseCount = count;
                        list.Clear();
                        list.Add(kv.Value);
                    }
                    else if (count == minUseCount)
                    {
                        list.Add(kv.Value);
                    }
                }
                var rollIdx = UnityEngine.Random.Range(0, list.Count);
                if (rollIdx >= 0)
                {
                    roleId = list[rollIdx].Id;
                }
                DebugEx.Info($"OrderUtility::DecideOrderRole {rollIdx}/{list.Count} {roleId}");
            }
            return roleId;
        }

        public static int CalcRealDifficultyForRequires(IList<int> requireItemId)
        {
            // 需求物品的总难度
            int realDifficulty = 0;
            foreach (var item in requireItemId)
            {
                Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(item, out _, out var real);
                realDifficulty += real;
                DebugEx.Info($"OrderUtility require ---> item {item}, dffy {real}");
            }
            return realDifficulty;
        }

        public static (int realDffy, int accDffy) CalcDifficultyForItem(int itemId, MergeWorldTracer tracer)
        {
            var mgr = Game.Manager.mergeItemDifficultyMan;
            var itemCount = tracer.GetCurrentActiveBoardItemCount();
            mgr.TryGetItemDifficulty(itemId, out _, out var realDffy);
            int accDffy = 0;
            var cat = Env.Instance.GetCategoryByItem(itemId);
            foreach (var item in cat.Progress)
            {
                if (itemCount.TryGetValue(item, out var _num))
                {
                    mgr.TryGetItemDifficulty(item, out _, out var _realDffy);
                    accDffy += _num * _realDffy;
                }
            }
            return (realDffy, accDffy);
        }

        public static int CalcRealDffyRound(int payDffy, int realDffy, int minDiffRate)
        {
            var pay_to_real_ratio = payDffy * 1f / realDffy;
            var ratio = MathF.Max(pay_to_real_ratio, minDiffRate / 100f);
            var realDffyRound = UnityEngine.Mathf.RoundToInt(realDffy * ratio);
            return realDffyRound;
        }

        public static OrderData MakeOrder_Init(IOrderHelper helper, OrderProviderType providerType, int id, int roleId, int unlockLevel)
        {
            var data = new OrderData()
            {
                State = OrderState.NotStart,
                Id = id,
                OrderType = 0,      // 常规 / 订单数量
                ProviderType = (int)providerType,   // common / random / ...
                RoleId = helper == null ? 0 : DecideOrderRole(roleId, helper),
                UnlockLevel = unlockLevel,
                Requires = new List<ItemCountInfo>(),
                Rewards = new List<Config.RewardConfig>(),
            };
            if (data.ProviderType == (int)OrderProviderType.Random)
            {
                data.ConfRandomer = helper.proxy.GetRandomerSlotConf(data.Id);
            }
            return data;
        }

        public static void MakeOrder_Require(OrderData data, IList<int> requireItemId)
        {
            // 需求物品只配置了id 如果要多个相同物品 则重复配置id
            ItemCountInfo info;
            foreach (var item in requireItemId)
            {
                info = null;
                foreach (var require in data.Requires)
                {
                    if (require.Id == item)
                    {
                        info = require;
                    }
                }
                if (info == null)
                {
                    info = new ItemCountInfo();
                    data.Requires.Add(info);
                }
                info.Id = item;
                info.TargetCount += 1;
            }
        }

        public static void MakeOrder_Reward(OrderData data, IOrderHelper helper, int realDifficulty, IList<string> reward)
        {
            Game.Manager.mergeLevelMan.TryGetLevelRate(helper.GetBoardLevel(), out var levelRate);
            foreach (var r in reward)
            {
                var (_cfg_id, _cfg_count, _param) = r.ConvertToInt3();
                var (_id, _count) = Game.Manager.rewardMan.CalcDynamicReward(_cfg_id, _cfg_count, levelRate, realDifficulty, _param);
                DebugEx.Info($"OrderUtility calc reward ---> [id {_cfg_id}]:[num {_cfg_count}]:[lv {levelRate}]:[dffy {realDifficulty}]:[type {_param}] => {_id}:{_count}");
                data.Rewards.Add(new Config.RewardConfig()
                {
                    Id = _id,
                    Count = _count,
                });
            }
        }

        public static void MakeOrder_Record(OrderData data)
        {
            var record = new OrderRecord()
            {
                Id = data.Id,
                OrderType = data.OrderType,
                ProviderType = data.ProviderType,
                RoleId = data.RoleId,
                UnlockLevel = data.UnlockLevel,
                CreatedAt = Env.Instance.GetTimestamp(),
            };
            for (int i = 0; i < data.Requires.Count; i++)
            {
                record.RequireIds.Add(data.Requires[i].Id);
                record.RequireNums.Add(data.Requires[i].TargetCount);
            }
            for (int i = 0; i < data.Rewards.Count; i++)
            {
                record.RewardIds.Add(data.Rewards[i].Id);
                record.RewardNums.Add(data.Rewards[i].Count);
            }
            data.Record = record;
        }

        public static OrderData MakeOrderByConfig(IOrderHelper helper, OrderProviderType providerType, int id, int roleId, int unlockLevel, int realDifficulty, IList<int> requireItemId, IList<string> reward)
        {
            var data = MakeOrder_Init(helper, providerType, id, roleId, unlockLevel);
            MakeOrder_Require(data, requireItemId);
            MakeOrder_Reward(data, helper, realDifficulty, reward);
            MakeOrder_Record(data);
            return data;
        }

        public static OrderData MakeOrderByRecord(OrderRecord orderRecord, IOrderHelper helper)
        {
            var data = new OrderData()
            {
                State = OrderState.NotStart,
                Id = orderRecord.Id,
                OrderType = orderRecord.OrderType,      // 常规 / 订单数量
                ProviderType = orderRecord.ProviderType,   // common / random / ...
                RoleId = orderRecord.RoleId,
                UnlockLevel = orderRecord.UnlockLevel,
                Requires = new List<ItemCountInfo>(),
                Rewards = new List<Config.RewardConfig>(),
                Record = orderRecord,
            };
            if (data.ProviderType == (int)OrderProviderType.Random)
            {
                data.ConfRandomer = helper.proxy.GetRandomerSlotConf(data.Id);
            }
            for (var i = 0; i < orderRecord.RequireIds.Count; i++)
            {
                var info = new ItemCountInfo();
                data.Requires.Add(info);
                info.Id = orderRecord.RequireIds[i];
                info.TargetCount = orderRecord.RequireNums[i];
            }
            for (var i = 0; i < orderRecord.RewardIds.Count; i++)
            {
                var r = new Config.RewardConfig();
                data.Rewards.Add(r);
                r.Id = orderRecord.RewardIds[i];
                r.Count = orderRecord.RewardNums[i];
            }
            return data;
        }

        // status: 0 => no api, 1 => wait api, 2 => use aip
        public static void SetOrderApiStatus(IOrderData order, OrderApiStatus status)
        {
            RecordStateHelper.UpdateRecord((int)OrderParamType.ApiOrderStatus, (int)status, (order as OrderData).Record.Extra);
        }

        public static void TryTrackOrderShow(IOrderData order)
        {
            if (!order.Displayed)
            {
                order.Displayed = true;
                DataTracker.order_show.Track(order);
                MessageCenter.Get<MSG.GAME_ORDER_DISPLAY_CHANGE>().Dispatch();
            }
        }

        #endregion

        #region activity

        // 对api订单 清除所有活动奖励
        public static void ClearActivityRewarsForApiOrder(OrderData order)
        {
            ClearActivity_ExtraBonus(order);
            ClearActivity_ExtraBonus_Mini(order);
        }

        public static void ClearActivity_ExtraBonus(OrderData order)
        {
            RemoveOrderRewardByStateKey(order, (int)OrderParamType.ExtraBonusRewardId, (int)OrderParamType.ExtraBonusRewardNum);
            RecordStateHelper.RemoveRecord((int)OrderParamType.ExtraBonusEventId, order.Record.Extra);
            RecordStateHelper.RemoveRecord((int)OrderParamType.ExtraBonusEventParam, order.Record.Extra);
        }

        public static void ClearActivity_ExtraBonus_Mini(OrderData order)
        {
            RemoveOrderRewardByStateKey(order, (int)OrderParamType.ExtraBonusRewardId_Mini, (int)OrderParamType.ExtraBonusRewardNum_Mini);
            RecordStateHelper.RemoveRecord((int)OrderParamType.ExtraBonusEventId_Mini, order.Record.Extra);
            RecordStateHelper.RemoveRecord((int)OrderParamType.ExtraBonusEventParam_Mini, order.Record.Extra);
        }

        // 移除存储key 以及key代表的订单奖励
        public static bool RemoveOrderRewardByStateKey(OrderData order, int keyId, int keyNum)
        {
            var changed = false;
            var reward_id = RecordStateHelper.ReadInt(keyId, order.Record.Extra); 
            var reward_num = RecordStateHelper.ReadInt(keyNum, order.Record.Extra);
            var record_idx = -1;
            for (var i = 0; i < order.Rewards.Count; i++)
            {
                var r = order.Rewards[i];
                if (r.Id == reward_id && r.Count == reward_num)
                {
                    record_idx = i;
                    break;
                }
            }
            if (record_idx >= 0)
            {
                changed = true;
                order.Rewards.RemoveAt(record_idx);
                order.Record.RewardIds.RemoveAt(record_idx);
                order.Record.RewardNums.RemoveAt(record_idx);
            }
            changed = RecordStateHelper.RemoveRecord(keyId, order.Record.Extra) || changed;
            changed = RecordStateHelper.RemoveRecord(keyNum, order.Record.Extra) || changed;
            return changed;
        }

        #endregion
    }
}