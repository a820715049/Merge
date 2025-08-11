/**
 * @Author: handong.liu
 * @Date: 2021-02-22 10:35:18
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemChestComponent : ItemSourceComponentBase, IEffectReceiver
    {
        public bool isWaiting => isNeedWait && item.world.currentWaitChest == item.id;
        public int energyCost => mChestConfig.EnergyCost;
        public int openWaitLeftMilli => isWaiting?config.WaitTime * 1000 - item.world.currentWaitChestTime:0;
        public int openWaitMilli => isWaiting?item.world.currentWaitChestTime:0;
        public bool isNeedWait => mChestConfig.WaitTime > 0;
        public int countLeft => mChestConfig.Capacity - mUsedCount;
        public bool canUse =>  countLeft > 0 && mOpened;
        public bool isOpenAndUsed => mUsedCount > 0;
        public ComMergeChest config => mChestConfig;
        private int mUsedCount;
        private bool mOpened;
        private ComMergeChest mChestConfig;

        bool IEffectReceiver.WillReceiveEffect(SpeedEffect effect)
        {
            return isWaiting && effect is SpeedEffect;
        }

        public static bool SerializeDelta(MergeItem newData, MergeItem oldData)
        {
            if(oldData.ComChest != null && oldData.ComChest.Equals(newData.ComChest))
            {
                newData.ComChest = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool Validate(ItemComConfig config)
        {
            return config?.chestConfig != null;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComChest = new ComChest();
            itemData.ComChest.Opened = mOpened;
            itemData.ComChest.UsedCount = mUsedCount;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if(itemData.ComChest != null)
            {
                mUsedCount = itemData.ComChest.UsedCount;
                if(mUsedCount >= mChestConfig.Capacity && mChestConfig.Capacity > 0)
                {
                    mUsedCount = mChestConfig.Capacity - 1;
                }
                mOpened = itemData.ComChest.Opened;
                SetOrderedOutputParam(mUsedCount);
            }
        }
        
        protected override void OnPostAttach()
        {
            mChestConfig = Env.Instance.GetItemComConfig(item.tid).chestConfig;
            base.OnPostAttach();
            mOpened = mChestConfig.WaitTime <= 0;
            mUsedCount = 0;
        }

        protected override void OnPostMerge(Item src, Item dst)
        {
            base.OnPostMerge(src, dst);
            var c1 = src.GetItemComponent<ItemChestComponent>();
            var c2 = dst.GetItemComponent<ItemChestComponent>();
            var parentLeft = c1.countLeft + c2.countLeft;
            var inheritedCap = Mathf.Min(parentLeft, mChestConfig.Capacity);
            mUsedCount = mChestConfig.Capacity - inheritedCap;
            DebugEx.Info($"ItemChestComponent::OnPostMerge ----> {parentLeft} => {mUsedCount}/{mChestConfig.Capacity}");
        }

        protected override void OnInitOrderedOutput(List<int> container)
        {
            if(mChestConfig != null && mChestConfig.OutputsWeight.Count == 0)
            {
                container.AddRange(mChestConfig.Outputs);
            }
        }

        protected override void OnInitOutputSet(Dictionary<int, int> container)
        {
            if(mChestConfig != null && mChestConfig.OutputsWeight.Count > 0)
            {
                for(int i = 0; i < mChestConfig.Outputs.Count; i++)
                {
                    container.Add(mChestConfig.Outputs[i], mChestConfig.OutputsWeight.GetElementEx(i));
                }
            }
            if(mChestConfig.OutputsSelectOne.Count > 0)
            {
                int targetId = mChestConfig.OutputsSelectOne[0];
                using(ObjectPool<DeterministicRandom>.GlobalPool.AllocStub(out var random))
                {
                    using(ObjectPool<List<int>>.GlobalPool.AllocStub(out var weightList))
                    {
                        random.ResetWithSeed(item.id);
                        for(int i = 0; i < mChestConfig.OutputsSelectOne.Count; i++)
                        {
                            int idxFound = mChestConfig.Outputs.IndexOf(mChestConfig.OutputsSelectOne[i]);
                            if(idxFound >= 0)
                            {
                                weightList.Add(mChestConfig.OutputsWeight[idxFound]);
                            }
                            else
                            {
                                weightList.Add(0);
                            }
                        }
                        targetId = mChestConfig.OutputsSelectOne.RandomChooseByWeight((e)=>weightList[mChestConfig.OutputsSelectOne.IndexOf(e)], ()=>random.Next);
                    }
                }
                foreach(var id in mChestConfig.Outputs)
                {
                    if(id == targetId)
                    {
                        continue;
                    }
                    int idx = mChestConfig.OutputsSelectOne.IndexOf(id);
                    if(idx >= 0)
                    {
                        container[targetId] = container.GetDefault(targetId, 0) + container.GetDefault(id, 0);
                        container.Remove(id);
                    }
                }
                DebugEx.FormatTrace("ItemChestComponent::OnInitOutputSet ----> choose {0} to output for item {1}:{2}", targetId, item, container);
            }
        }

        public bool StartWait()
        {
            if(config.WaitTime <= 0)            //no need to wait
            {
                return false;
            }
            var world = item.world;
            var env = Env.Instance;
            if(world.currentWaitChest <= 0)
            {
                world.SetWaitChest(item);
                item.SetEffectDirty();
                return true;
            }
            else
            {
                DebugEx.FormatWarning("ItemChestComponent.StartWait ----> chest {0} is already in wait, me({1}) cann't enter wait", world.currentWaitChest, item.id);
                return false;
            }
        }

        public bool SetOpen()
        {
            if(!mOpened)
            {
                mOpened = true;
                if(item.world.currentWaitChest == item.id)
                {
                    item.world.SetCurrentChestOpen();
                }
                item.SetEffectDirty();
            }
            return true;
        }

        public int ConsumeNextItem()
        {
            mUsedCount++;
            return ConsumeNextOutput(out _);
        }

        public int CalculateSpeedOpenCost()
        {
            if(mChestConfig.WaitTime > 0)
            {
                return EL.MathUtility.LerpInteger(0, mChestConfig.SpeedCost, openWaitLeftMilli, mChestConfig.WaitTime * 1000);
            }
            else
            {
                return 0;
            }
        }

        public bool SpeedOpen()
        {
            if(!mOpened)
            {
                if(SetOpen())
                {
                    item.world.TriggerItemEvent(item, ItemEventType.ItemEventSpeedUp);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}