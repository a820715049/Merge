/*
 * @Author: qun.chao
 * @Date: 2021-09-29 17:31:59
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FAT.Merge;
using EL;

namespace FAT
{
    public static class BoardViewWrapper
    {
        public enum ParamType
        {
            CompItemInfo,
            CompRewardTrack,
        }

        // 目前的交互流程 同一环境只涉及一个棋盘
        private static MergeWorld world;
        private static int activityId;
        private static Dictionary<ParamType, object> globalParamHolder = new Dictionary<ParamType, object>();

        public static void PushWorld(MergeWorld w, int actId = -1)
        {
            if (world != null)
            {
                Debug.LogWarning("push world -> stack overflow");
            }
            world = w;
            activityId = actId;
            modalRewardList.Clear();
        }

        public static void PopWorld()
        {
            if (world == null)
            {
                Debug.LogWarning("pop world -> empty stack");
            }
            world = null;
            activityId = -1;
            globalParamHolder.Clear();
        }

        public static MergeWorld GetCurrentWorld()
        {
            return world;
        }

        public static void SetParam(ParamType pt, object obj)
        {
            globalParamHolder[pt] = obj;
        }

        public static object GetParam(ParamType pt)
        {
            if (globalParamHolder.TryGetValue(pt, out var obj))
            {
                return obj;
            }
            return null;
        }

        public static string GetBoardName()
        {
            if (world != null)
                return world.dataTrackName;
            return Game.Manager.mainMergeMan.world.dataTrackName;
        }

        public static bool IsMainBoard()
        {
            return _IsMain();
        }

        private static bool _IsMain()
        {
            return world == null || world == Game.Manager.mainMergeMan.world;
        }

        #region order

        public static bool IsNeededByTopBarOrder(int tid)
        {
            return GetBoardOrderRequireItemStateCache()?.TryGetValue(tid, out _) ?? false;
        }

        public static bool TryFinishOrder(IOrderData order, ICollection<RewardCommitData> orderRewards, ICollection<RewardCommitData> boxRewards, bool needConfirm)
        {
            if (_IsMain())
            {
                // 如果允许订单消耗背包物品 则需要弹窗确认
                if (needConfirm && Env.Instance.CanInventoryItemUseForOrder)
                {
                    var temp = PoolMapping.PoolMappingAccess.Take<List<Item>>(out var confirmList);
                    if (OrderUtility.IsConsumeNeedConfirmation(order as OrderData, world, confirmList) &&
                        confirmList.Count > 0)
                    {
                        // 使用正式的弹窗 | UI关闭时释放temp
                        UIManager.Instance.OpenWindow(UIConfig.UICompleteOrderBag,confirmList, new Action(() =>
                        {
                            temp.Free();
                            MessageCenter.Get<MSG.GAME_ORDER_TRY_FINISH_FROM_UI>().Dispatch(order, false);
                        }));
                        return false;
                    }
                    else
                    {
                        return TryFinishOrderInner(order, orderRewards, boxRewards);
                    }
                }
                else
                {
                    return TryFinishOrderInner(order, orderRewards, boxRewards);
                }
            }
            return false;
        }

        // 订单完成时 订单附加的礼盒可以顺便完成
        private static bool TryFinishOrderInner(IOrderData order, ICollection<RewardCommitData> orderRewards, ICollection<RewardCommitData> boxRewards)
        {
            bool ret = false;
            int levelRate = 0;
            if (_IsMain())
            {
                ret = Game.Manager.mainOrderMan.TryFinishOrder(order, orderRewards);
                levelRate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();
            }

            if (ret && BoardViewWrapper.TryGetOrderBoxDetail(order.Id, out _, out _, out var detail))
            {
                // 成功完成订单 则顺便结算礼盒
                using (ObjectPool<Dictionary<int, int>>.GlobalPool.AllocStub(out var dict))
                {
                    foreach (var randId in detail.RandomReward)
                    {
                        var randReward = Game.Manager.configMan.GetRandomRewardConfigById(randId);
                        dict.Add(randId, randReward.Weight);
                    }
                    var rewardMan = Game.Manager.rewardMan;
                    // 按权重随机
                    var resultId = dict.RandomChooseByWeight(e => e.Value).Key;
                    var resultCfg = Game.Manager.configMan.GetRandomRewardConfigById(resultId);
                    foreach (var reward in resultCfg.Reward)
                    {
                        //随机宝箱发奖时支持按参数类型计算
                        var (_cfg_id, _cfg_count, _param) = reward.ConvertToInt3();
                        var (_id, _count) = rewardMan.CalcDynamicReward(_cfg_id, _cfg_count, levelRate, 0, _param);
                        boxRewards?.Add(rewardMan.BeginReward(_id, _count, ReasonString.order_box));
                    }
                }
            }

            if (ret && order.IsMagicHour)
            {
                // 成功完成订单 结算星想事成
                var result = world.activeBoard.TryResolveMagicHourOutput(order, out var targetOrder);
                if (result != null)
                {
                    MessageCenter.Get<MSG.GAME_ORDER_MAGICHOUR_REWARD_BEGIN>().Dispatch((order, targetOrder, result));
                }
            }

            return ret;
        }

        public static bool TryGetOrderBoxDetail(int randomOrderId, out int totalMilli, out int countMilli, out fat.rawdata.OrderBoxDetail detail)
        {
            totalMilli = 0;
            countMilli = 0;
            detail = null;
            bool ret = false;
            if (world != null && world.orderBox.hasActiveOrderBox)
            {
                totalMilli = world.orderBox.orderBoxDurationMilli;
                countMilli = world.orderBox.orderBoxLifeCountMilli;
                detail = world.orderBox.GetOrderBoxDetailByRandomerId(randomOrderId);
                ret = detail != null;
            }
            return ret;
        }

        #endregion

        #region modal rewards

        // 需要弹窗表现的奖励列表
        private static List<RewardCommitData> modalRewardList = new List<RewardCommitData>();

        public static void ShowModalReward(RewardCommitData reward)
        {
            modalRewardList.Add(reward);
        }

        #endregion

        public static void FillBoardOrderExcept(List<IOrderData> container, int exceptMask)
        {
            FillBoardOrder(container, (int)OrderProviderTypeMask.All - exceptMask);
        }

        public static void FillBoardOrder(List<IOrderData> container, int mask = (int)OrderProviderTypeMask.All)
        {
            if (_IsMain())
            {
                Game.Manager.mainOrderMan.FillActiveOrders(container, mask);
            }
        }

        public static void ValidateOrderDisplayCache()
        {
            if (_IsMain())
            {
                Game.Manager.mainOrderMan.ValidateOrderDisplayCache();
            }
        }

        public static Dictionary<int, int> GetBoardOrderRequireItemStateCache()
        {
            if (_IsMain())
            {
                return Game.Manager.mainOrderMan.GetOrderRequireItemStateCache();
            }
            return null;
        }

        // 0: 订单需求部分满足
        // 1: 订单需求完全满足
        public static int GetItemRequireState(Item item)
        {
            if (!item.isActive || item.HasComponent(ItemComponentType.Bubble))
                return -1;
            var cache = GetBoardOrderRequireItemStateCache();
            if (cache.TryGetValue(item.tid, out var state))
            {
                return state;
            }
            return -1;
        }

        public static bool TryFinishOrderByItem(Item item)
        {
            var cache = GetBoardOrderRequireItemStateCache();
            if (cache == null)
                return false;
            var canFinishOrder = false;
            if (cache.TryGetValue(item.tid, out var state))
            {
                if (state >= 1)
                {
                    // 可以完成订单
                    using (EL.ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var orders))
                    {
                        FillBoardOrder(orders);
                        foreach (var order in orders)
                        {
                            if (order.State == OrderState.Finished)
                            {
                                var idx = order.Requires.FindIndex(x => x.Id == item.tid);
                                if (idx >= 0)
                                {
                                    world.AddPriorityConsumeItem(item);
                                    canFinishOrder = true;
                                    EL.MessageCenter.Get<MSG.GAME_ORDER_TRY_FINISH_FROM_UI>().Dispatch(order, true);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return canFinishOrder;
        }
    }
}