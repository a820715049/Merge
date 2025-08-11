/*
 * @Author: qun.chao
 * @Date: 2025-01-07 11:21:52
 */
using UnityEngine;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using EL;

namespace FAT.Merge
{
    public class ItemMixSourceComponent : ItemSourceComponentBase
    {
        public int outputCountToDead => mItemTotalOutputToDead;
        // 已死
        public bool isDead => mConfig.StageCount > 0 && mConfig.StageCount <= outputCountToDead;
        public int outputMilli => mOutputCounter;
        public bool isOutputing => mItemInRechargeCount > 0;
        public bool isReviving => reviveTotalMilli > 0 && totalItemCount < mConfig.LimitCount;
        public int totalItemCount => itemInRechargeCount + itemCount;
        public int reviveMilli => mReviveCounter;
        public int reviveTotalMilli
        {
            get
            {
                int reviveSec = mConfig.ReviveTime;
                if (mFirstRevive && mConfig.FirstOutputTime > 0)
                {
                    reviveSec = mConfig.FirstOutputTime;
                }
                return reviveSec * 1000;
            }
        }
        public int itemInRechargeCount => mItemInRechargeCount;
        public int itemCount => mItemCount;
        public int totalMixRequire { get; set; }
        public int mixedCount => mixList.Count;
        public IList<int> mixedItems => mixList;
        public ComMergeMixSource config => mConfig;
        private ComMergeMixSource mConfig = null;
        private int mItemTotalOutputToDead = 0;
        private int mItemCount = 0;
        // 记录复活后获得的全部产出潜能
        // 实际产出受到OutputTime的限制
        private int mItemInRechargeCount = 0;
        private int mOutputCounter = 0;
        private int mReviveCounter = 0;
        private bool mFirstRevive = false;
        private bool mAllowCharging => !mConfig.IsFillClear || mConfig.IsFillClear && mItemCount == 0;
        private List<int> mixList = new();

        public static bool Validate(ItemComConfig config)
        {
            return config?.mixSourceConfig != null;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            var data = new ComMixSource
            {
                ReviveCounter = mReviveCounter,
                OutputCounter = mOutputCounter,
                ItemInRecharge = mItemInRechargeCount,
                ItemCount = mItemCount,
                OutputCount = mItemTotalOutputToDead,
                IsFirstRevive = mFirstRevive
            };
            data.MixedItems.AddRange(mixList);

            itemData.ComMixSource = data;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            var data = itemData.ComMixSource;
            if (data != null)
            {
                mItemCount = data.ItemCount;
                mItemInRechargeCount = data.ItemInRecharge;
                mItemTotalOutputToDead = data.OutputCount;
                mOutputCounter = data.OutputCounter;
                mReviveCounter = data.ReviveCounter;
                mFirstRevive = data.IsFirstRevive;
                mixList.Clear();
                mixList.AddRange(data.MixedItems);
            }
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

        public (bool, int) CheckMixState()
        {
            foreach (var groupId in config.MixId)
            {
                var cfg = Env.Instance.GetMergeMixCostConfig(groupId);
                if (cfg != null)
                {
                    var status = cfg.MixInfo.CompareAsSet(mixList);
                    if (status == ListExt.SetCompareStatus.Equal)
                    {
                        // 已消耗足够
                        return (true, groupId);
                    }
                }
            }
            return (false, 0);
        }

        public bool CanMixItem(Item target)
        {
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<int>>(out var list);
            list.AddRange(mixList);
            list.Add(target.tid);
            foreach (var groupId in config.MixId)
            {
                var cfg = Env.Instance.GetMergeMixCostConfig(groupId);
                if (cfg != null)
                {
                    var status = cfg.MixInfo.CompareAsSet(list);
                    if (status == ListExt.SetCompareStatus.Equal || status == ListExt.SetCompareStatus.Superset)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryMixItem(Item target)
        {
            if (IsNextItemReady() && CanMixItem(target))
            {
                mixList.Add(target.tid);
                return true;
            }
            return false;
        }

        public bool TryExtract(int itemId)
        {
            var idx = mixList.IndexOf(itemId);
            if (idx >= 0)
            {
                mixList.RemoveAt(idx);
                return true;
            }
            return false;
        }

        public int ConsumeNextItem(int mixId, out int oldItemId)
        {
            oldItemId = 0;
            var cfg = Env.Instance.GetMergeMixCostConfig(mixId);

            if (!IsNextItemReady())
            {
                return 0;
            }
            if (cfg == null)
            {
                return 0;
            }
            mItemCount--;
            mixList.Clear();
            if (mConfig.StageCount > 0)
            {
                mItemTotalOutputToDead++;
            }

            ResetOutputs(cfg.Outputs);
            var itemId = ConsumeNextOutput(out var sourceType);

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
            return itemId;
        }

        public bool SpeedOutput()
        {
            if (isOutputing)
            {
                var outputTotalMilli = mConfig.OutputTime * 1000;
                Update(Mathf.Max(0, outputTotalMilli - mOutputCounter));
                return true;
            }
            return false;
        }

        public bool SpeedRevive()
        {
            if (isReviving)
            {
                Update(Mathf.Max(0, reviveTotalMilli - mReviveCounter));
                item.world.TriggerItemEvent(item, ItemEventType.ItemEventSpeedUp);
                return true;
            }
            return false;
        }

        protected override void OnInitOutputSet(Dictionary<int, int> container)
        {
            // 不填充产出物 当产出组合确定后再根据该组合进行产出
        }

        protected override void OnPostMerge(Item src, Item dst)
        {
            if (mConfig.FirstOutputTime <= 0)
            {
                var inheritedCountA = 0;
                var inheritedCountB = 0;
                if (!src.isFrozen && src.TryGetItemComponent<ItemMixSourceComponent>(out var c, true))
                {
                    inheritedCountA += c.itemCount;
                    inheritedCountA += c.mItemInRechargeCount;
                }
                if (!dst.isFrozen && dst.TryGetItemComponent<ItemMixSourceComponent>(out c, true))
                {
                    inheritedCountB += c.itemCount;
                    inheritedCountB += c.mItemInRechargeCount;
                }
                if (inheritedCountA + inheritedCountB > mItemCount)
                {
                    mItemCount = inheritedCountA + inheritedCountB;
                    DebugEx.Info($"ItemMixSourceComponent::OnPostMerge ----> {inheritedCountA}@{src} {inheritedCountB}@{dst}");
                }
            }
            base.OnPostMerge(src, dst);
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            var cfg = Env.Instance.GetItemComConfig(item.tid).mixSourceConfig;
            mConfig = cfg;
            mItemCount = cfg.FirstOutputTime > 0 ? 0 : cfg.ReviveCount;
            mItemInRechargeCount = 0;
            mReviveCounter = 0;
            mFirstRevive = true;

            if (cfg.MixId.Count > 0)
            {
                var cost = Env.Instance.GetMergeMixCostConfig(cfg.MixId[0]);
                totalMixRequire = cost?.MixInfo.Count ?? 0;
            }
            else
            {
                totalMixRequire = 0;
            }
        }

        protected override void OnUpdateInactive(int dt)
        {
            base.OnUpdateInactive(dt);
            // 背包中正常充能
            _UpdateRecharge(dt);
        }

        protected override void OnUpdate(int dt)
        {
            if (isGridNotMatch)
            {
                return;
            }
            base.OnUpdate(dt);

            int milliToConsume = EffectUtility.CalculateMilliBySpeedEffect(this, dt);
            _UpdateJumpCD(milliToConsume);
            _UpdateRecharge(milliToConsume);
        }

        private void _UpdateRecharge(int milli)
        {
            if (isReviving && mAllowCharging)
            {
                while (milli >= reviveTotalMilli - mReviveCounter && totalItemCount < mConfig.LimitCount)
                {
                    //one revive
                    int milliConsumed = reviveTotalMilli - mReviveCounter;
                    _TickRecharge(milliConsumed);
                    mReviveCounter = 0;
                    var addCount = Mathf.Min(mConfig.LimitCount - totalItemCount, mConfig.ReviveCount);
                    var directGetCount = Mathf.Min(addCount, config.OutputCount);                       //direct get the capacity of one output
                    addCount -= directGetCount;
                    mItemCount += directGetCount;
                    mItemInRechargeCount += addCount;
                    DebugEx.FormatInfo("Merge.ItemSourceComponent ----> item {0} revive add {1} to {2}, itemCount add {3} to {4}", item.id, addCount, mItemInRechargeCount, directGetCount, mItemCount);
                    milli -= milliConsumed;

                    mFirstRevive = false;
                }
                if (isReviving)
                {
                    mReviveCounter += milli;
                }
            }
            if (milli > 0)
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

        private void _TickRecharge(int deltaMilli)
        {
            if (isOutputing)
            {
                if (mConfig.OutputTime > 0)
                {
                    int outputTime = mConfig.OutputTime * 1000;
                    mOutputCounter += deltaMilli;
                    if (mOutputCounter >= outputTime)
                    {
                        int newItemCount = mOutputCounter / outputTime;
                        newItemCount *= mConfig.OutputCount;
                        mOutputCounter = mOutputCounter % outputTime;
                        if (newItemCount >= mItemInRechargeCount)
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
    }
}