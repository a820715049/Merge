/**
 * @Author: handong.liu
 * @Date: 2021-02-26 18:46:28
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemAutoSourceComponent : ItemSourceComponentBase, IEffectReceiver
    {
        public int outputMilli => mJustCreate?Mathf.Max(0, mOutputCounter - (mConfig.OutputTime - mConfig.FirstOutputTime) * 1000):mOutputCounter;
        public int itemCount => mItemCount;
        public bool isOutputing => mItemCount < mConfig.Limit;
        public long outputWholeMilli => (mJustCreate?mConfig.FirstOutputTime:mConfig.OutputTime) * 1000;
        public ComMergeAutoSource config => mConfig;
        public bool isDead => config.AutoVanishTime > 0 && mTotalOutput >= config.AutoVanishTime;
        private ComMergeAutoSource mConfig = null;
        private bool mJustCreate = false;
        private int mItemCount = 0;
        private int mOutputCounter = 0;
        private int mTotalOutput = 0;
        private bool mAllowCharging => !mConfig.IsFillClear || mConfig.IsFillClear && mItemCount == 0;

        bool IEffectReceiver.WillReceiveEffect(SpeedEffect effect)
        {
            return mConfig.OutputTime > 0 && effect is SpeedEffect;
        }

        //注意：和时间相关的量，需要保存两个：counter和starttime，当parent == null时不在棋盘上，此时服务器保存counter,当parent!= null时在棋盘上，此时服务器保存starttime, 如此可减少数据改变频率
        //return whether changed
        public static bool SerializeDelta(MergeItem newData, MergeItem oldData)
        {
            if(oldData.ComAutoSource != null && oldData.ComAutoSource.Equals(newData.ComAutoSource))
            {
                newData.ComAutoSource = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool Validate(ItemComConfig config)
        {
            return config?.autoSourceConfig != null;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            long milli = item.world.lastTickMilli;
            long outputStart = 0;
            int outputCounter = 0;
            // if(isOutputing && item.parent != null)
            // {
            //     outputStart = (milli - mOutputCounter) / 1000;
            // }
            // else
            // {
            outputCounter = mOutputCounter;
            // }
            itemData.ComAutoSource = new ComAutoSource();
            itemData.ComAutoSource.Start = outputStart;
            itemData.ComAutoSource.ItemCount = mItemCount;
            itemData.ComAutoSource.OutputCounter = outputCounter;
            itemData.ComAutoSource.TotalOutput = mTotalOutput;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if(itemData.ComAutoSource != null)
            {
                mItemCount = itemData.ComAutoSource.ItemCount;

                // if(itemData.ComAutoSource.Start > 0)
                // {
                //     mOutputCounter = Mathf.Max(0, (int)(item.world.lastActiveTime * 1000 - itemData.ComAutoSource.Start * 1000));
                // }
                // else
                // {
                mOutputCounter = itemData.ComAutoSource.OutputCounter;
                // }
                mTotalOutput = itemData.ComAutoSource.TotalOutput;
            }
            mJustCreate = false;
        }

        public int ConsumeNextItem()
        {
            if(!IsNextItemReady())
            {
                return 0;
            }
            mItemCount--;
            mTotalOutput++;
            return ConsumeNextOutput(out _);
        }

        public int CalculateSpeedOutputCost()
        {
            var outputTotalMilli = mConfig.OutputTime * 1000;
            return EL.MathUtility.LerpInteger(0, mConfig.SpeedCost, outputTotalMilli - mOutputCounter, outputTotalMilli);
        }

        public bool SpeedOutput()
        {
            if(isOutputing)
            {
                var outputTotalMilli = mConfig.OutputTime * 1000;
                Update(Mathf.Max(0, outputTotalMilli - mOutputCounter));
                item.world.TriggerItemEvent(item, ItemEventType.ItemEventSpeedUp);
                return true;
            }
            return false;
        }

        public bool IsNextItemReady()
        {
            return item.isActive && mItemCount > 0;
        }

        public Toast GetToastTypeForItem(int itemTid)
        {
            config.OutputsToast.TryGetValue(itemTid, out var toastType);
            return (Toast)toastType;
        }

        public void StartInstantOutput(int count)
        {
            mItemCount += count;
            DebugEx.FormatInfo("ItemAutoSourceComponent::StartInstantOutput ----> {0},{1}", count, mItemCount);
        }

        protected override void OnPostMerge(Item src, Item dst)
        {
            var inheritedCountA = 0;
            var inheritedCountB = 0;
            if (!src.isFrozen && src.TryGetItemComponent<ItemAutoSourceComponent>(out var c, true))
            {
                inheritedCountA += c.itemCount;
                // mItemCount += c.mItemCount;
                // DebugEx.FormatInfo("ItemAutoSourceComponent::OnPostMerge ----> {0} add {1} from merge", item, c.mItemCount);
            }
            if(!dst.isFrozen && dst.TryGetItemComponent<ItemAutoSourceComponent>(out c, true))
            {
                inheritedCountB += c.itemCount;
                // mItemCount += c.mItemCount;
                // DebugEx.FormatInfo("ItemAutoSourceComponent::OnPostMerge ----> {0} add {1} from merge", item, c.mItemCount);
            }

            if (inheritedCountA + inheritedCountB > mItemCount)
            {
                mItemCount = inheritedCountA + inheritedCountB;
                DebugEx.Info($"ItemAutoSourceComponent::OnPostMerge ----> {inheritedCountA}@{src} {inheritedCountB}@{dst}");
            }
            base.OnPostMerge(src, dst);
        }

        protected override void OnPostSpawn(ItemSpawnContext sp)
        {
            base.OnPostSpawn(sp);
            if(sp != null)
            {
                if(sp.type == ItemSpawnContext.SpawnType.Upgrade && sp.from1 != null && sp.from1.TryGetItemComponent<ItemAutoSourceComponent>(out var autoSource))
                {
                    mItemCount += autoSource.mItemCount;
                    DebugEx.FormatInfo("ItemAutoSourceComponent::OnPostSpawn ----> {0} add {1} from upgrade", item, autoSource.mItemCount);
                }
            }
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).autoSourceConfig;
            mItemCount = 0;
            mOutputCounter = Mathf.Max(0, mConfig.OutputTime - mConfig.FirstOutputTime) * 1000;
            mJustCreate = true;
        }

        protected override void OnInitOutputSet(Dictionary<int, int> container)
        {
            ItemUtility.GetAutoSourceOutputs(item.tid, container);
        }

        protected override void OnInitRandomList(List<ItemOutputRandomList.OutputConstraitFixCount> container)
        {
            var cfg = Env.Instance.GetItemComConfig(item.tid)?.autoSourceConfig;
            if (cfg != null)
            {
                for (var i = 0; i < cfg.OutputsFixed.Count; i++)
                {
                    container.Add(new()
                    {
                        id = cfg.OutputsFixed[i],
                        totalCount = cfg.OutputsFixedTime[2 * i],
                        targetCount = cfg.OutputsFixedTime[2 * i + 1],
                    });
                }
            }
        }

        protected override void OnUpdateInactive(int dt)
        {
            base.OnUpdateInactive(dt);
            _UpdateRecharge(dt, false);
        }

        protected override void OnUpdate(int dt)
        {
            if(isGridNotMatch)
            {
                return;
            }
            base.OnUpdate(dt);
            dt = EffectUtility.CalculateMilliBySpeedEffect(this, dt);
            _UpdateRecharge(dt, true);
        }

        private void _UpdateRecharge(int dt, bool allowSpawn)
        {
            if (mItemCount < mConfig.Limit && mAllowCharging)
            {
                mOutputCounter += dt;
                var outputTime = mConfig.OutputTime * 1000;
                if (mOutputCounter >= outputTime)
                {
                    mItemCount = mItemCount + (mOutputCounter / outputTime) * mConfig.OutputCount;
                    if (mItemCount > mConfig.Limit)
                        mItemCount = mConfig.Limit;
                    mOutputCounter = mOutputCounter % outputTime;
                    DebugEx.FormatInfo("Merge.ItemAutoSourceComponent ----> item {0} add to {1}", item.id, mItemCount);
                }
            }
            if (item.parent != null && mItemCount > 0)
            {
                mJustCreate = false;
                var count = mItemCount;
                if (allowSpawn)
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (item.isDead)
                            break;
                        item.parent.UseAutoItemSource(item, out var st);
                        if (st != ItemUseState.Success)
                            break;
                    }
                }
                if (mItemCount >= mConfig.Limit)
                {
                    mItemCount = mConfig.Limit;
                    mOutputCounter = 0;
                }
            }
        }
    }
}