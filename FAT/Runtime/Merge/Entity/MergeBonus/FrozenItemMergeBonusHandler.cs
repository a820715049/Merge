/*
 * @Author: tang.yan
 * @Description: 触发合成行为时，尝试结合目前情况产生冰冻棋子，成功产出后相关条件参数清0
 * @Date: 2025-08-14 18:08:59
 */

using EL;
using fat.rawdata;
using UnityEngine;
using System.Collections.Generic;

namespace FAT.Merge
{
    //触发合成行为时，尝试结合目前情况产生冰冻棋子，成功产出后相关条件参数清0
    public class FrozenItemMergeBonusHandler : IMergeBonusHandler
    {
        int IMergeBonusHandler.priority => 101; //冰冻棋子优先于泡泡，后于合成bonus
        
        private ActivityFrozenItem _actInst;
        private bool _isValid => _actInst != null && _actInst.Active;
        
        public FrozenItemMergeBonusHandler(ActivityFrozenItem act)
        {
            _actInst = act;
        }
        
        void IMergeBonusHandler.Process(MergeBonusContext context)
        {
            //活动实例非法时返回
            if (!_isValid)
                return;
            //如果冰冻棋子功能没开启 则return
            if (!Env.Instance.IsFeatureEnable(MergeFeatureType.FrozenItem))
            {
                DebugEx.FormatInfo("FrozenItemMergeBonusHandler::_TrySpawnFrozenItem ----> FrozenItem feature not open!");
                return;
            }
            //只认主棋盘
            var boardId = context.world?.activeBoard?.boardId;
            if (boardId != Constant.MainBoardId)
                return;
            if (!_actInst.MustSpawnFrozenItem)
            {
                _TrySpawnFrozenItem(context.result);
            }
            else
            {
                var srcItem = context.result;
                srcItem?.parent.TrySpawnFrozenItem(srcItem, srcItem.tid, 60000);
            }
        }

        void IMergeBonusHandler.OnRegister()
        {
            
        }

        void IMergeBonusHandler.OnUnRegister()
        {
            
        }
        
        private Item _TrySpawnFrozenItem(Item srcItem)
        {
            if (srcItem == null)
                return null;
            //检查配置上是否允许是冰冻棋子 不是则不走后续逻辑
            var cfg = Env.Instance.GetItemMergeConfig(srcItem.tid);
            if (cfg == null || !cfg.IsFrozenItem)
                return null;
            //活动配置
            var detail = _actInst.GetCurGroupConfig();
            if (detail == null)
                return null;
            // 名额限制：棋盘上冰冻棋子数量 < MaxFrozenItem
            var curFrozen = BoardViewManager.Instance.GetBubbleFrozenItemCount();      // 当前冰冻棋子数
            var limit = _actInst.Conf.MaxFrozenItem;                                   // 名额上限
            if (curFrozen >= limit)
                return null;
            // 未达“≥ ergConsumeTimes”的门槛，不触发
            if (!_actInst.ThresholdReached(detail))
                return null;
            // 掉落概率（命中保底时强制100%）
            var prob = _actInst.CalcDropProb(detail);
            var pass = _actInst.Guarantee || (UnityEngine.Random.Range(0f, 100f) < prob);
            if (!pass)
                return null;
            
            // —— 概率已通过：开始构建候选集并择优 —— //
            // 前提：假设玩家在合成A+A时，生成棋子B和冰冻棋子C
            // 目标：从【random订单（排除MagicHour）】中，挑出与 srcItem(棋子B) 同链、等级 ∈ [棋子B的等级+lvDiff, 订单需求等级-1]，
            // 且付出难度 ∈ 当前E1（累计消耗体力）所属难度段 的候选；优先选择“付出难度最高”的一个。
            // 若候选为空：点亮保底（下次合成100%），并返回 null。
            
            int targetId;
            long lifeTime;  //棋子存活时间 单位毫秒
            if (!TryPickCandidateAndLife(srcItem, detail, out targetId, out lifeTime, out var payDiff))
            {
                // 概率通过但候选为空 -> 点亮保底
                _actInst.SetGuarantee(true);
                return null;
            }
            // 真正产出
            var spawned = srcItem.parent.TrySpawnFrozenItem(srcItem, targetId, lifeTime);
            if (spawned != null)
            {
                // 产出成功 -> 清空累计并关闭保底
                _actInst.ResetAfterSpawn();
                DebugEx.FormatInfo("FrozenItemMergeBonusHandler::_TrySpawnFrozenItem Success ----> FrozenItem {0} created, form base {1}, lifeTime={2}ms", targetId, srcItem.tid, lifeTime);
                DataTracker.event_frozen_item_get.Track(_actInst, detail.Diff, payDiff, spawned.id, spawned.tid, ItemUtility.GetItemLevel(spawned.tid));
            }
            else
            {
                // 概率通过,订单上也有候选，但是因为棋盘满了导致想生成却实际没有生成冰冻棋子，此时触发保底，下次符合条件时必出冰冻棋子
                _actInst.SetGuarantee(true);
                DebugEx.FormatInfo("FrozenItemMergeBonusHandler::_TrySpawnFrozenItem Fail ----> FrozenItem {0} created, form base {1}, lifeTime={2}ms", targetId, srcItem.tid, lifeTime);
            }
            return spawned;
        }
        
        // 选中目标棋子并计算寿命；失败返回 false（用于置保底）
        private bool TryPickCandidateAndLife(Item srcItem, FrozenItemDetail d, out int targetId, out long lifeSeconds, out int payDiff)
        {
            targetId = 0;
            lifeSeconds = 0;
            payDiff = 0;    //最终计算的付出难度

            // 1) 本次合成链与等级（B）
            var srcTid = srcItem.tid;
            var srcCate = Env.Instance.GetCategoryByItem(srcTid);       // MergeItemCategory
            var cateProgress = srcCate?.Progress;
            if (cateProgress == null)
                return false;
            var bLevel = cateProgress.IndexOf(srcTid);  //等级从0开始

            // 2) 汇总 random 订单（排除 MagicHour），找与 srcCate 同链的“目标等级最大值” Lmax
            int maxTargetLevel = -1;
            using (var _ = PoolMapping.PoolMappingAccess.Borrow<List<IOrderData>>(out var orders))
            {
                Game.Manager.mainOrderMan.FillActiveOrders(orders, (int)OrderProviderTypeMask.Random);
                foreach (var od in orders)
                {
                    if (od == null || od.IsMagicHour) continue; // 排除心想事成
                    var reqs = od.Requires;
                    if (reqs == null) continue;
                    for (int i = 0; i < reqs.Count; i++)
                    {
                        var tid = reqs[i].Id;
                        if (tid <= 0) continue;
                        //如果srcCate的链条中包含tid，则说明是同链
                        if (!cateProgress.Contains(tid)) 
                            continue;
                        var lv = cateProgress.IndexOf(tid);  //等级从0开始
                        if (lv > maxTargetLevel) maxTargetLevel = lv;
                    }
                }
            }
            if (maxTargetLevel < 0)
            {
#if UNITY_EDITOR
                DebugEx.FormatInfo("FrozenItem::Pick fail - no random orders on same chain, baseTid={0}", srcTid);
#endif
                return false;
            }

            // 3) 等级窗口：[B + lvDiff, Lmax - 1]
            int minLevel = bLevel + d.LvDiff;
            int maxLevel = maxTargetLevel - 1;
            if (maxLevel < minLevel) return false;

            // 修正到链长度边界
            int maxIndex = cateProgress.Count - 1;
            if (minLevel > maxIndex) return false;
            if (maxLevel > maxIndex) maxLevel = maxIndex;

            // 4) 根据 E1 命中 energyRange 段，再用索引取 itemDiffRange 段（闭区间）
            int matchIdx = FindEnergyRangeIndex(d, _actInst.EnergyConsumed);
            // 未命中任何能量段，视为无候选
            if (matchIdx < 0)
            {
#if UNITY_EDITOR
                DebugEx.FormatInfo("FrozenItem::Pick fail - energyRange miss, EnergyConsumed={0}", _actInst.EnergyConsumed);
#endif
                return false;
            }

            (int dMin, int dMax) = ParseDiffRangeAt(d, matchIdx);

            // 5) 全局难度范围（用于寿命分段）
            (int gMin, int gMax) = GetGlobalDiffRange(d);
            if (gMax < gMin) return false;

            // 6) 从高到低扫描等级窗口，筛 payDffy ∈ [dMin, dMax]；选最大 payDffy（并可用等级高作次级tie-breaker）
            var tracer = Game.Manager.mainMergeMan.worldTracer;
            int bestTid = 0;
            int bestPay = int.MinValue;
            int bestLevel = -1;

            var env = Env.Instance;
            for (int lv = maxLevel; lv >= minLevel; lv--)
            {
                int candTid = GetTidAtLevel(srcCate, lv);
                if (candTid <= 0) continue;
                //检查配置上是否允许是冰冻棋子 不是则跳过
                var cfg = env.GetItemMergeConfig(candTid);
                if (cfg == null || !cfg.IsFrozenItem)
                    continue;

                var (real, acc) = OrderUtility.CalcDifficultyForItem(candTid, tracer);
                int pay = real - acc;
#if UNITY_EDITOR
                DebugEx.FormatInfo("FrozenItem::Picking, candTid={0}, level={1}, realDiff={2}, accDiff={3}, payDiff={4}",
                    candTid, lv, real, acc, pay);
#endif
                // payDffy 需命中当前段
                if (pay < dMin || pay > dMax)
                    continue;

                // pay 也必须在全局 [gMin, gMax] 内，否则本次跳过（不触发产出）
                if (pay < gMin || pay > gMax)
                    continue;

                // 选最大付出难度；若相等可以选择更高等级
                if (pay > bestPay || (pay == bestPay && lv > bestLevel))
                {
                    bestPay = pay;
                    bestTid = candTid;
                    bestLevel = lv;
                }
            }
            if (bestTid <= 0)
            {
#if UNITY_EDITOR
                DebugEx.FormatInfo("FrozenItem::Pick fail - no candidate in diff range, baseTid={0}, EnergyConsumed={1}, eIdx={2}, dRange=[{3},{4}]",
                    srcTid, _actInst.EnergyConsumed, matchIdx, dMin, dMax);
#endif
                return false;
            }
            //计算寿命
            var life = CalcLifeSeconds(d, bestPay, gMin, gMax);
            if (life <= 0)
            {
#if UNITY_EDITOR
                DebugEx.FormatInfo("FrozenItem::Pick fail - life<=0, targetTid={0}, pay={1}", bestTid, bestPay);
#endif
                return false;
            }
            targetId = bestTid;
            lifeSeconds = life * 1000; // 最后乘1000 把单位转成毫秒
            payDiff = bestPay;
#if UNITY_EDITOR
            //成功产出时打日志
            DebugEx.FormatInfo("FrozenItem::Pick Success! - baseTid={0}, baseLevel={1}, lvDiff={2}, Lmax={3}, EnergyConsumed={4}, matchDiffIdx={5}, dRange=[{6},{7}], pickTid={8}, payDiff={9}, life={10}s",
                srcTid, bLevel, d.LvDiff, maxTargetLevel, _actInst.EnergyConsumed, matchIdx, dMin, dMax, targetId, payDiff, life);
#endif
            return true;
        }

        // 命中 energyRange 的段索引（左闭右闭；-1 表示无上界）
        private static int FindEnergyRangeIndex(FrozenItemDetail d, int energy)
        {
            var ranges = d.EnergyRange; // RepeatedField<string> "L:R"
            if (ranges == null || ranges.Count == 0) return -1;

            for (int i = 0; i < ranges.Count; i++)
            {
                var r = ranges[i].ConvertToIntRange();
                int min = r.Min;
                int max = r.Max < 0 ? int.MaxValue : r.Max;
                if (energy >= min && energy <= max)
                    return i;
            }
            return -1;
        }

        // 取指定索引的难度闭区间
        private static (int, int) ParseDiffRangeAt(FrozenItemDetail d, int idx)
        {
            var diffs = d.ItemDiffRange; // RepeatedField<string> "L:R"
            if (diffs == null || idx < 0 || idx >= diffs.Count) return (int.MaxValue, int.MinValue);
            var r = diffs[idx].ConvertToIntRange();
            return (r.Min, r.Max);
        }

        // 全局难度范围 [Dmin, Dmax]（用于寿命映射）
        private static (int, int) GetGlobalDiffRange(FrozenItemDetail d)
        {
            int gMin = int.MaxValue;
            int gMax = int.MinValue;
            var diffs = d.ItemDiffRange;
            if (diffs == null) return (gMin, gMax);
            for (int i = 0; i < diffs.Count; i++)
            {
                var r = diffs[i].ConvertToIntRange();
                if (r.Min < gMin) gMin = r.Min;
                if (r.Max > gMax) gMax = r.Max;
            }
            return (gMin, gMax);
        }

        // 寿命映射：把 [gMin, gMax] 等分到 itemDeadTime 数组（左闭右开，最后一段右闭）
        private static long CalcLifeSeconds(FrozenItemDetail d, int pay, int gMin, int gMax)
        {
            var times = d.ItemDeadTime; // RepeatedField<int>（单位：秒；升序；可为单值）
            if (times == null || times.Count == 0)
                return 0;

            if (times.Count == 1)
                return times[0];

            // pay 不在全局范围内，由调用处已过滤；此处假定 gMax >= gMin
            int N = times.Count;
            int span = gMax - gMin;
            if (span <= 0)
                return times[0];

            // 计算所在分段索引
            // 段宽 = span / N（浮点），分界 T_i = floor(gMin + i * span / N)
            // 区间定义：[T_{i-1}, T_i)，最后一段 [T_{N-1}, gMax]
            int seg;
            if (pay >= gMax)
            {
                seg = N - 1;
            }
            else
            {
                float f = (pay - gMin) / (float)span;
                seg = Mathf.FloorToInt(f * N);
                seg = Mathf.Clamp(seg, 0, N - 1);
            }

            return times[seg];
        }

        // 取链上指定等级对应的棋子tid（Progress 约定为 level->tid）
        private static int GetTidAtLevel(MergeItemCategory cate, int level)
        {
            return cate.Progress[level];
        }
    }
}