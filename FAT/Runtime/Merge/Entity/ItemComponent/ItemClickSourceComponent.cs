/**
 * @Author: handong.liu
 * @Date: 2021-02-19 15:28:52
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;
using Cysharp.Text;

namespace FAT.Merge
{
    public class ItemClickSourceComponent : ItemSourceComponentBase, IEffectReceiver
    {
        public bool isNoCD => mNoCDCountDown > 0;
        public int noCDMilliLeft => mNoCDCountDown;
        public int maxNoCDMilli => mMaxNoCDCountDown;
        public int outputCountToDead => mItemTotalOutputToDead;
        //是否再点一下就会死，或者不用点就死了
        public bool willDead => mConfig.StageCount > 0 && mConfig.StageCount <= outputCountToDead + 1;
        // 已死
        public bool isDead => mConfig.StageCount > 0 && mConfig.StageCount <= outputCountToDead;
        public int energyCost => _CheckEnergyCost();
        public MergeTapCost firstCost => mFirstCost;
        public IList<int> costConfig => mConfig.CostId;

        public int outputMilli => mOutputCounter;
        public bool isOutputing => mItemInRechargeCount > 0;
        public bool isBoostItem => mBoostItemCount > 0 && mConfig.IsBoostItem;
        public bool wasBoostItem;
        public bool isReviving => reviveTotalMilli > 0 && totalItemCount < mConfig.LimitCount;
        public int totalItemCount => itemInRechargeCount + itemCount;
        public int reviveMilli => mReviveCounter;
        public int reviveTotalMilli {
            get {
                int reviveSec = mConfig.ReviveTime;
                if(mFirstRevive && mConfig.FirstOutputTime > 0)
                {
                    reviveSec = mConfig.FirstOutputTime;
                }
                return reviveSec * 1000;
            }
        }
        public int itemInRechargeCount => mItemInRechargeCount;
        public int itemCount => isNoCD?100:mItemCount;
        public ComMergeTapSource config => mConfig;
        private ComMergeTapSource mConfig = null;
        private int mItemTotalOutputToDead = 0;
        private int mItemCount = 0;
        private int mItemInRechargeCount = 0;
        private int mOutputCounter = 0;
        private int mReviveCounter = 0;
        private int mNoCDCountDown = 0;
        private int mMaxNoCDCountDown = 0;
        private bool mFirstRevive = false;
        private int mBoostItemCount = 0;
        private MergeTapCost mFirstCost;
        private bool mAllowCharging => !mConfig.IsFillClear || mConfig.IsFillClear && mItemCount == 0;

        bool IEffectReceiver.WillReceiveEffect(SpeedEffect effect)
        {
            return (mConfig.ReviveTime > 0 || mConfig.OutputTime > 0) && effect is SpeedEffect;
        }

        public static bool SerializeDelta(MergeItem newData, MergeItem oldData)
        {
            if(oldData.ComClickSource != null && oldData.ComClickSource.Equals(newData.ComClickSource))
            {
                newData.ComClickSource = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool Validate(ItemComConfig config)
        {
            return config?.clickSourceConfig != null;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            long milli = item.world.lastTickMilli;
            long outputStart = 0;
            int outputCounter = 0;
            long reviveStart = 0;
            int reviveCounter = 0;
            // if(isOutputing && item.parent != null)
            // {
            //     outputStart = (milli - mOutputCounter) / 1000;
            // }
            // else
            // {
            outputCounter = mOutputCounter;
            // }
            // if(isReviving && item.parent != null)
            // {
            //     reviveStart = (milli - mReviveCounter) / 1000;
            // }
            // else
            // {
            reviveCounter = mReviveCounter;
            // }
            itemData.ComClickSource = new ComClickSource();
            itemData.ComClickSource.Item = mItemCount;
            itemData.ComClickSource.OutputCount = mItemTotalOutputToDead;
            itemData.ComClickSource.ItemInRecharge = mItemInRechargeCount;
            itemData.ComClickSource.OutputCounter = outputCounter;
            itemData.ComClickSource.OutputStart = outputStart;
            itemData.ComClickSource.ReviveStart = reviveStart;
            itemData.ComClickSource.ReviveCounter = reviveCounter;
            itemData.ComClickSource.RandomNextIdx = randomOutputNextIdx + 1;
            itemData.ComClickSource.RandomSeed = randomOutputSeed;
            itemData.ComClickSource.NoCDCounter = mNoCDCountDown / 1000;
            itemData.ComClickSource.IsFirstRevive = mFirstRevive;
            itemData.ComClickSource.BoostItemCount = mBoostItemCount;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if(itemData.ComClickSource != null)
            {
                mItemCount = itemData.ComClickSource.Item;
                mItemInRechargeCount = itemData.ComClickSource.ItemInRecharge;
                mItemTotalOutputToDead = itemData.ComClickSource.OutputCount;
                mNoCDCountDown = itemData.ComClickSource.NoCDCounter * 1000;
                mMaxNoCDCountDown = mNoCDCountDown;
                SetRandomOutputParam(itemData.ComClickSource.RandomSeed, itemData.ComClickSource.RandomNextIdx - 1);


                // if(itemData.ComClickSource.OutputStart > 0)
                // {
                //     mOutputCounter = Mathf.Max(0, (int)(item.world.lastActiveTime * 1000 - itemData.ComClickSource.OutputStart));
                // }
                // else
                // {
                mOutputCounter = itemData.ComClickSource.OutputCounter;
                // }

                // if(itemData.ComClickSource.ReviveStart > 0)
                // {
                //     mReviveCounter = Mathf.Max(0, (int)(item.world.lastActiveTime * 1000 - itemData.ComClickSource.ReviveStart * 1000));
                // }
                // else
                // {
                mReviveCounter = itemData.ComClickSource.ReviveCounter;
                // }
                mFirstRevive = itemData.ComClickSource.IsFirstRevive;
                mBoostItemCount = itemData.ComClickSource.BoostItemCount;
            }
        }

        public bool CanConsumeItem(Item consumeTarget)
        {
            if (energyCost > 0)
            {
                return false;
            }
            foreach (var cid in config.CostId)
            {
                var cfg = Env.Instance.GetMergeTapCostConfig(cid);
                if (cfg != null && cfg.Cost == consumeTarget.tid)
                {
                    return true;
                }
            }
            return false;
        }

        public void SimulateOutput(int count, int round)
        {
#if UNITY_EDITOR
            DebugEx.Info($"ItemClickSourceComponent::SimulateOutput begin ----> {item.id} count:{count} round:{round}");

            using var _1 = PoolMapping.PoolMappingAccess.Borrow<Dictionary<int, int>>(out var itemCountDict);
            using var _2 = PoolMapping.PoolMappingAccess.Borrow<List<(int, int)>>(out var itemCountList);
            var sb = ZString.CreateStringBuilder();

            for (var roundIdx = 0; roundIdx < round; roundIdx++)
            {
                sb.AppendLine();
                sb.Append($"Round {roundIdx + 1}:");
                for (var i = 0; i < count; i++)
                {
                    var itemId = ConsumeNextOutput(out var _);
                    itemCountDict[itemId] = itemCountDict.GetValueOrDefault(itemId, 0) + 1;
                }
                itemCountList.Clear();
                foreach (var (itemId, num) in itemCountDict)
                {
                    itemCountList.Add((itemId, num));
                }
                itemCountList.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                foreach (var (itemId, num) in itemCountList)
                {
                    sb.Append($"{itemId}:{num},");
                }
                itemCountDict.Clear();
            }
            DebugEx.Info($"ItemClickSourceComponent::SimulateOutput {sb}");
            DebugEx.Info($"ItemClickSourceComponent::SimulateOutput end ----> {item.id} count:{count} round:{round}");
#endif
        }

        public void StartBoostItem(int count)
        {
            mBoostItemCount += count;
            wasBoostItem = true;
        }
        
        public bool WasBoostItem()
        {
            var r = wasBoostItem;
            wasBoostItem = false;
            return r;
        }

        public int ConsumeNextItem(out int oldItemId)
        {
            oldItemId = 0;
            if(!IsNextItemReady())
            {
                return 0;
            }
            if(!isNoCD)
            {
                mItemCount--;
            }
            if(mConfig.StageCount > 0)
            {
                mItemTotalOutputToDead ++;
            }
            var itemId = ConsumeNextOutput(out var sourceType);
            // 如果产出物品有受限配置 需要检查是否进行受限转换
            if (ItemUtility.IsDropLimitItem(itemId, out var convertId))
            {
                itemId = convertId;
            }
            oldItemId = itemId;
            //若当前生成器有能量加倍功能 且 目前已开启能量加倍状态 则产出的棋子等级+1
            //具体做法为先按之前的逻辑得出目标棋子 然后寻找目标棋子所在合成链中比他高1级的棋子作为结果棋子返回
            if (itemId > 0 && mConfig.IsBoostable && Env.Instance.IsInEnergyBoost())
            {
                //逻辑上也区分出具有能量加倍功能的棋子
                if (sourceType is ItemSourceType.RandomFixed or ItemSourceType.RandomWeight)
                {
                    var level_add = EnergyBoostUtility.GetBoostLevel((int)Env.Instance.GetEnergyBoostState());
                    if (level_add > 0)
                    {
                        itemId = Env.Instance.GetNextLevelItemId(itemId, level_add);
                    }
                }
            }
            if (itemId > 0 && mConfig.IsBoostItem && mBoostItemCount > 0)
            {
                var level_add = 1;
                item.TryGetItemComponent<ItemSkillComponent>(out var skill);
                if(skill != null)
                {
                    level_add = skill.param[0];
                }
                itemId = Env.Instance.GetNextLevelItemId(itemId, level_add);
                mBoostItemCount--;
            }
            return itemId;
        }

        public void StartNoCD(int seconds)
        {
            mNoCDCountDown = seconds * 1000;
            mMaxNoCDCountDown = mNoCDCountDown;
            item.world.TriggerItemEvent(item, ItemEventType.ItemEventNoCDStateChange);
        }

        public bool IsNextItemReady()
        {
            return item.isActive && itemCount > 0;
        }

        /// <summary>
        /// 判断应该使用的toast
        /// </summary>
        /// <param name="itemTid">预计产出id</param>
        /// <param name="realTid">实际产出id</param>
        public Toast GetToastTypeForItem(int itemTid, int realTid)
        {
            if (config.IsBoostable && EnergyBoostUtility.Is4X() && config.MaxToast.TryGetValue(realTid, out var toast))
            {
                return (Toast)toast;
            }
            config.OutputsToast.TryGetValue(itemTid, out var toastType);
            return (Toast)toastType;
        }

        public int CalculateSpeedOutputCost()
        {
            if(isOutputing)
            {
                return 1;       //TODO: check this logic
            }
            else
            {
                return 0;
            }
        }

        public void StartInstantOutput(int count)
        {
            mItemCount += count;
            DebugEx.FormatInfo("ItemClickSourceComponent::StartInstantOutput ----> {0}, {1}, {2}", count, mItemCount, totalItemCount);
        }

        public bool SpeedOutput()
        {
            if(isOutputing)
            {
                var outputTotalMilli = mConfig.OutputTime * 1000;
                Update(Mathf.Max(0, outputTotalMilli - mOutputCounter));
                return true;
            }
            return false;
        }

        public int CalculateSpeedReviveCost()
        {
            if(item.tid == Constant.kSpeedupGuideObjId && !Env.Instance.IsSpeedupGuidePassed())             //guide return 0
            {
                return 0;
            }
            if(isReviving)
            {
                int baseTime = mConfig.ReviveTime;
                if (mFirstRevive && mConfig.FirstOutputTime > 0) {
                    baseTime = mConfig.FirstOutputTime;
                }
                var factor = Mathf.Max(0, baseTime * 1000 - mReviveCounter);
                var max = baseTime * 1000;
                if (config.Id == Constant.kBingoGeneratorId && CalculateReviveCostBingo(factor, max, out var cost)) {
                    return cost;
                }
                return EL.MathUtility.LerpInteger(0, mConfig.SpeedCost, factor, max);
            }
            else
            {
                return 0;
            }
        }

        public bool CalculateReviveCostBingo(int factor, int max, out int cost_) {
            cost_ = 0;
            // FAT_TODO
            // var bingo = Game.Instance.activityMan.BingoEvent;
            // if (bingo == null || !bingo.EventActive) return false;
            // var id = bingo.NextToFill(false);
            // var conf = Game.Instance.objectMan.GetMergeItemConfig(id);
            // if (conf == null) return false;
            // var price = conf.BingoPrice;
            // var c = price * (factor / (43200f * 1000f));
            // cost_ = Mathf.CeilToInt(c);
            return true;
        }

        public bool SpeedRevive()
        {
            if(isReviving)
            {
                Update(Mathf.Max(0, reviveTotalMilli - mReviveCounter));
                item.world.TriggerItemEvent(item, ItemEventType.ItemEventSpeedUp);
                //_TickRecharge(86400000);                //end all recharge at once
                return true;
            }
            return false;
        }

        protected override void OnInitOutputSet(Dictionary<int, int> container)
        {
            ItemUtility.GetClickSourceOutputs(item.tid, container);
        }

        protected override void OnInitRandomList(List<ItemOutputRandomList.OutputConstraitFixCount> container)
        {
            var comConfig = Env.Instance.GetItemComConfig(item.tid);
            if(comConfig != null && comConfig.clickSourceConfig != null)
            {
                for(int i = 0; i < comConfig.clickSourceConfig.OutputsFixed.Count; i++)
                {
                    container.Add(new ItemOutputRandomList.OutputConstraitFixCount(){
                        id = comConfig.clickSourceConfig.OutputsFixed[i],
                        totalCount = comConfig.clickSourceConfig.OutputsFixedTime[2 * i],
                        targetCount = comConfig.clickSourceConfig.OutputsFixedTime[2 * i + 1]
                    });
                }
            }
        }

        protected override void OnPostMerge(Item src, Item dst)
        {
            if(mConfig.FirstOutputTime <= 0)
            {
                var inheritedCountA = 0;
                var inheritedCountB = 0;
                if (!src.isFrozen && src.TryGetItemComponent<ItemClickSourceComponent>(out var c, true))
                {
                    inheritedCountA += c.itemCount;
                    inheritedCountA += c.mItemInRechargeCount;

                    // mItemCount += c.mItemCount;
                    // mItemCount += c.mItemInRechargeCount;
                    // DebugEx.FormatInfo("ItemClickSourceComponent::OnPostMerge ----> {0} add {1} item from merge", item, c.mItemCount);
                    // DebugEx.FormatInfo("ItemClickSourceComponent::OnPostMerge ----> {0} add {1} recharge from merge", item, c.mItemInRechargeCount);
                }
                if(!dst.isFrozen && dst.TryGetItemComponent<ItemClickSourceComponent>(out c, true))
                {
                    inheritedCountB += c.itemCount;
                    inheritedCountB += c.mItemInRechargeCount;

                    // mItemCount += c.mItemCount;
                    // mItemCount += c.mItemInRechargeCount;
                    // DebugEx.FormatInfo("ItemClickSourceComponent::OnPostMerge ----> {0} add {1} item from merge", item, c.mItemCount);
                    // DebugEx.FormatInfo("ItemClickSourceComponent::OnPostMerge ----> {0} add {1} recharge from merge", item, c.mItemInRechargeCount);
                }
                if (inheritedCountA + inheritedCountB > mItemCount)
                {
                    mItemCount = inheritedCountA + inheritedCountB;
                    DebugEx.Info($"ItemClickSourceComponent::OnPostMerge ----> {inheritedCountA}@{src} {inheritedCountB}@{dst}");
                }
                TryAddBoostCount(src, dst);
            }

            base.OnPostMerge(src, dst);
        }

        private void TryAddBoostCount(Item src, Item dst)
        {
            if (src.TryGetItemComponent<ItemClickSourceComponent>(out var boostSrc) && 
                dst.TryGetItemComponent<ItemClickSourceComponent>(out var boostDst))
            {
                if (boostSrc != null && boostSrc.isBoostItem)
                {
                    mBoostItemCount += boostSrc.mBoostItemCount;
                }
                if (boostDst != null && boostDst.isBoostItem)
                {
                    mBoostItemCount += boostDst.mBoostItemCount;
                }
                DebugEx.Info($"ItemClickSourceComponent::TryAddBoostCount ----> {mBoostItemCount}");
            }
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            var cfg = Env.Instance.GetItemComConfig(item.tid).clickSourceConfig;
            mConfig = cfg;
            if (cfg.CostId.Count >= 1)
            {
                mFirstCost = Env.Instance.GetMergeTapCostConfig(cfg.CostId[0]);
            }

            if(cfg.FirstOutputTime > 0)
            {
                mItemCount = 0;
            }
            else
            {
                mItemCount = cfg.ReviveCount;
            }
            //if(mConfig.reviveTime <= 0)
            {
                mItemInRechargeCount = 0;
            }
            mReviveCounter = 0;
            mFirstRevive = true;
        }

        protected override void OnUpdateInactive(int dt)
        {
            base.OnUpdateInactive(dt);
            _UpdateNoCD(dt);
            // 背包中正常充能
            _UpdateRecharge(dt);
        }

        protected override void OnUpdate(int dt)
        {
            if(isGridNotMatch)
            {
                return;
            }
            base.OnUpdate(dt);

            int milliToConsume = EffectUtility.CalculateMilliBySpeedEffect(this, dt);
            _UpdateNoCD(milliToConsume);
            _UpdateJumpCD(milliToConsume);
            _UpdateRecharge(milliToConsume);
        }

        private void _UpdateRecharge(int milli)
        {
            if(isReviving && mAllowCharging)
            {
                while (milli >= reviveTotalMilli - mReviveCounter && totalItemCount < mConfig.LimitCount)
                {
                    //one revive
                    int milliConsumed = reviveTotalMilli - mReviveCounter;
                    _TickRecharge(milliConsumed);
                    mReviveCounter = 0;
                    int oldCount = mItemInRechargeCount;
                    var addCount = Mathf.Min(mConfig.LimitCount - totalItemCount, mConfig.ReviveCount);
                    var directGetCount = Mathf.Min(addCount, config.OutputCount);                       //direct get the capacity of one output
                    addCount -= directGetCount;
                    mItemCount += directGetCount;
                    mItemInRechargeCount = mItemInRechargeCount + addCount;
                    DebugEx.FormatInfo("Merge.ItemSourceComponent ----> item {0} revive add {1} to {2}, itemCount add {3} to {4}", item.id, addCount, mItemInRechargeCount, directGetCount, mItemCount);
                    milli -= milliConsumed;

                    mFirstRevive = false;
                }
                if(isReviving)
                {
                    mReviveCounter += milli;
                }
            }
            if(milli > 0)
            {
                _TickRecharge(milli);
            }
        }

        // 跳过cd功能 物品在跳过cd期间数量回满 cd清零
        private void _UpdateJumpCD(int milli)
        {
            if (!mConfig.IsJumpable || !item.parent.world.jumpCD.hasActiveJumpCD)
                return;
            mReviveCounter = 0;
            mItemInRechargeCount = 0;
            if (mItemCount < mConfig.LimitCount)
                mItemCount = mConfig.LimitCount;
        }

        private void _UpdateNoCD(int milli)
        {
            if(isNoCD)
            {
                mNoCDCountDown -= milli;
                if(mNoCDCountDown <= 0)
                {
                    DebugEx.FormatInfo("Merge::ItemTapSourceComponent ----> no cd time end");
                    mNoCDCountDown = 0;
                    item.world.TriggerItemEvent(item, ItemEventType.ItemEventNoCDStateChange);
                }
            }
        }

        private void _TickRecharge(int deltaMilli)
        {
            if(isOutputing)
            {
                if(mConfig.OutputTime > 0)
                {
                    int outputTime = mConfig.OutputTime * 1000;
                    mOutputCounter += deltaMilli;
                    if(mOutputCounter >= outputTime)
                    {
                        int newItemCount = mOutputCounter / outputTime;
                        newItemCount *= mConfig.OutputCount;
                        mOutputCounter = mOutputCounter % outputTime;
                        if(newItemCount >= mItemInRechargeCount)
                        {
                            newItemCount = mItemInRechargeCount;
                            mOutputCounter = 0;
                        }
                        mItemInRechargeCount -= newItemCount;
                        mItemCount += newItemCount;
                        DebugEx.FormatInfo("Merge.ItemSourceComponent ----> item {0} charge {1} -> {2}", item.id, newItemCount, mItemCount);
                    }
                }
                else
                {
                    //direct get all item
                    int newItemCount = mItemInRechargeCount;
                    mItemInRechargeCount -= newItemCount;
                    mItemCount += newItemCount;
                    DebugEx.FormatInfo("Merge.ItemSourceComponent ----> item {0} directly get {1} -> {2}", item.id, newItemCount, mItemCount);
                }
            }
        }

        private int _CheckEnergyCost()
        {
            if (mFirstCost == null || mFirstCost.Cost != Constant.kMergeEnergyObjId)
                return 0;
            var baseCost = Constant.kMergeEnergyCostNum;
            if (!mConfig.IsBoostable)
                return baseCost;
            var state = Env.Instance.GetEnergyBoostState();
            var rate = EnergyBoostUtility.GetEnergyRate((int)state);
            return baseCost * rate;
        }
    }
}