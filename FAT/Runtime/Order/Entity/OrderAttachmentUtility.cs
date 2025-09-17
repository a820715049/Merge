/*
 * @Author: qun.chao
 * @Date: 2025-03-25 12:16:30
 */
using static FAT.RecordStateHelper;
using fat.rawdata;

namespace FAT
{
    public static class OrderAttachmentUtility
    {
        // 订单附加数据槽
        public class Slot
        {
            public int keyEventId;
            public int keyEventParam;
            public int keyRewardId;
            public int keyRewardNum;

            public static Slot MakeSlot_Score()
            {
                var slot = new Slot
                {
                    keyEventId = (int)OrderParamType.ScoreEventId,
                    keyRewardNum = (int)OrderParamType.Score,
                };
                return slot;
            }

            public static Slot MakeSlot_Score_BR()
            {
                var slot = new Slot
                {
                    keyEventId = (int)OrderParamType.ScoreEventIdBR,
                    keyRewardNum = (int)OrderParamType.ScoreBR,
                    keyRewardId = (int)OrderParamType.ScoreRewardBR,
                };
                return slot;
            }

            public static Slot MakeSlot_Extra()
            {
                var slot = new Slot
                {
                    keyEventId = (int)OrderParamType.ExtraBonusEventId,
                    keyEventParam = (int)OrderParamType.ExtraBonusEventParam,
                    keyRewardId = (int)OrderParamType.ExtraBonusRewardId,
                    keyRewardNum = (int)OrderParamType.ExtraBonusRewardNum,
                };
                return slot;
            }

            public static Slot MakeSlot_ExtraMini()
            {
                var slot = new Slot
                {
                    keyEventId = (int)OrderParamType.ExtraBonusEventId_Mini,
                    keyEventParam = (int)OrderParamType.ExtraBonusEventParam_Mini,
                    keyRewardId = (int)OrderParamType.ExtraBonusRewardId_Mini,
                    keyRewardNum = (int)OrderParamType.ExtraBonusRewardNum_Mini,
                };
                return slot;
            }

            public static Slot MakeSlot_ExtraTL()
            {
                var slot = new Slot
                {
                    keyEventId = (int)OrderParamType.ExtraSlot_TL_EventId,
                    keyEventParam = (int)OrderParamType.ExtraSlot_TL_EventParam,
                    keyRewardId = (int)OrderParamType.ExtraSlot_TL_RewardId,
                    keyRewardNum = (int)OrderParamType.ExtraSlot_TL_RewardNum,
                };
                return slot;
            }

            public static Slot MakeSlot_ExtraTR()
            {
                var slot = new Slot
                {
                    keyEventId = (int)OrderParamType.ExtraSlot_TR_EventId,
                    keyEventParam = (int)OrderParamType.ExtraSlot_TR_EventParam,
                    keyRewardId = (int)OrderParamType.ExtraSlot_TR_RewardId,
                    keyRewardNum = (int)OrderParamType.ExtraSlot_TR_RewardNum,
                };
                return slot;
            }

            public bool HasData(IOrderData order)
            {
                return GetEventId(order) > 0;
            }

            public int GetEventId(IOrderData order)
            {
                return order.GetValue((OrderParamType)keyEventId);
            }

            public int GetParam(IOrderData order)
            {
                return order.GetValue((OrderParamType)keyEventParam);
            }

            public bool IsMatchEventId(IOrderData order, int eventId)
            {
                return order.GetValue((OrderParamType)keyEventId) == eventId;
            }

            public (int id, int num) GetReward(IOrderData order)
            {
                return (order.GetValue((OrderParamType)keyRewardId), order.GetValue((OrderParamType)keyRewardNum));
            }

            //订单左下角积分数据更新
            public void UpdateScoreData(OrderData order, int eventId, int rewardNum)
            {
                var extra = order.Record.Extra;
                UpdateRecord(keyEventId, eventId, extra);
                UpdateRecord(keyRewardNum, rewardNum, extra);
            }

            //订单右下角奖励数据更新  rewardId=0时认为奖励id是什么由对应活动的配置决定
            public void UpdateScoreDataBR(OrderData order, int eventId, int rewardNum, int rewardId = 0)
            {
                var extra = order.Record.Extra;
                UpdateRecord(keyEventId, eventId, extra);
                UpdateRecord(keyRewardNum, rewardNum, extra);
                UpdateRecord(keyRewardId, rewardId, extra);
            }

            public void UpdateEventData(OrderData order, int eventId, int eventParam, int rewardId, int rewardNum)
            {
                var extra = order.Record.Extra;
                UpdateRecord(keyEventId, eventId, extra);
                UpdateRecord(keyEventParam, eventParam, extra);
                UpdateRecord(keyRewardId, rewardId, extra);
                UpdateRecord(keyRewardNum, rewardNum, extra);
            }

            public void ClearData(OrderData order)
            {
                RemoveByKey(order, keyEventId);
                RemoveByKey(order, keyEventParam);
                RemoveByKey(order, keyRewardId);
                RemoveByKey(order, keyRewardNum);
            }

            public void ClearOrderReward(OrderData order)
            {
                OrderUtility.RemoveOrderRewardByStateKey(order, keyRewardId, keyRewardNum);
            }

            private void RemoveByKey(OrderData order, int keyId)
            {
                if (keyId > 0)
                    RemoveRecord(keyId, order.Record.Extra);
            }
        }

        // 左下角slot | 积分
        public static readonly Slot slot_score = Slot.MakeSlot_Score();
        // 右下角slot | 积分
        public static readonly Slot slot_score_br = Slot.MakeSlot_Score_BR();
        // 额外奖励 | 涉及到rewards
        public static readonly Slot slot_extra = Slot.MakeSlot_Extra();
        // 额外奖励 mini | 涉及到rewards
        public static readonly Slot slot_extra_mini = Slot.MakeSlot_ExtraMini();
        // 左上角slot | 类似积分
        public static readonly Slot slot_extra_tl = Slot.MakeSlot_ExtraTL();
        // 右上角slot | 类似积分
        public static readonly Slot slot_extra_tr = Slot.MakeSlot_ExtraTR();

        // TODO: 挂件的清理逻辑现在都应该使用 TryRemove_ExtraTR 的做法(判断该id的活动不存在 则清理)
        // 因为现在的很多活动已经不再是对所有订单都挂接, 而是对特定订单挂接
        // 当活动过期后, 简单判断有下列模版一致的活动存在, 并不能认为订单上挂接的数据一定有效
        // 现在这期活动可能需要关心的是别的订单
        public static bool TryRemoveInvalidEventData(OrderData order)
        {
            var changed = false;
            changed = TryRemove_Score(order) || changed;
            changed = TryRemove_Score_BR(order) || changed;
            changed = TryRemove_Extra(order) || changed;
            changed = TryRemove_ExtraMini(order) || changed;
            changed = TryRemove_ExtraTL(order) || changed;
            changed = TryRemove_ExtraTR(order) || changed;
            return changed;
        }

        private static bool TryRemove_Score(OrderData order)
        {
            if (!slot_score.HasData(order))
                return false;
            if (Game.Manager.activity.LookupAny(EventType.Score) != null)
                return false;
            if (Game.Manager.activity.LookupAny(EventType.Race) != null)
                return false;
            if (Game.Manager.activity.LookupAny(EventType.ScoreDuel) != null)
                return false;
            if (Game.Manager.activity.LookupAny(EventType.MultiplierRanking) != null)
                return false;
            if (Game.Manager.activity.LookupAny(EventType.MicMilestone) != null)
                return false;
            var changed = true;
            slot_score.ClearData(order);
            return changed;
        }

        private static bool TryRemove_Score_BR(OrderData order)
        {
            var eventId = slot_score_br.GetEventId(order);
            if (eventId <= 0)
                return false;
            if (Game.Manager.activity.Lookup(eventId, out var acti) && acti.Active)
                return false;
            slot_score_br.ClearData(order);
            return true;
        }

        private static bool TryRemove_Extra(OrderData order)
        {
            var eventParam = slot_extra.GetParam(order);
            if (eventParam <= 0)
                return false;
            if (Game.Manager.activity.LookupAny(EventType.OrderExtra, eventParam, out _))
                return false;
            slot_extra.ClearOrderReward(order);
            slot_extra.ClearData(order);
            return true;
        }

        private static bool TryRemove_ExtraMini(OrderData order)
        {
            var eventParam = slot_extra_mini.GetParam(order);
            if (eventParam <= 0)
                return false;
            if (Game.Manager.activity.LookupAny(EventType.OrderExtra, eventParam, out _))
                return false;
            slot_extra_mini.ClearOrderReward(order);
            slot_extra_mini.ClearData(order);
            return true;
        }

        private static bool TryRemove_ExtraTL(OrderData order)
        {
            var eventParam = slot_extra_tl.GetParam(order);
            if (eventParam <= 0)
                return false;
            // 好评订单
            if (Game.Manager.activity.LookupAny(EventType.OrderLike, eventParam, out _))
                return false;
            // 拼图活动
            if (Game.Manager.activity.LookupAny(EventType.Puzzle, eventParam, out _))
                return false;
            slot_extra_tl.ClearData(order);
            return true;
        }

        private static bool TryRemove_ExtraTR(OrderData order)
        {
            var eventId = slot_extra_tr.GetEventId(order);
            if (eventId <= 0)
                return false;
            if (Game.Manager.activity.Lookup(eventId, out var acti) && acti.Active)
                return false;
            slot_extra_tr.ClearData(order);
            TryRemove_Tag(order, eventId);
            return true;
        }

        private static bool TryRemove_Tag(OrderData order, int eventId)
        {
            var eventType = Game.Manager.activity.LookupConf(eventId, out var conf) ? conf.EventType : EventType.Default;
            var tag = eventType switch
            {
                EventType.ClawOrder => OrderTag.ClawOrder,
                _ => OrderTag.None,
            };
            if (tag != OrderTag.None)
            {
                order.RemoveTag(tag);
                return true;
            }
            return false;
        }
    }
}