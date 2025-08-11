/*
 * @Author: qun.chao
 * @Date: 2023-12-19 18:35:21
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemToolSourceComponent : ItemSourceComponentBase
    {
        public int outputCountToDead => mItemTotalOutputToDead;
        // 是否再点一下就会死，或者不用点就死了
        public bool willDead => mItemCount <= 1;
        public int energyCost => 0;
        public int totalItemCount => itemInRechargeCount + itemCount;
        public int itemInRechargeCount => mItemInRechargeCount;
        public int itemCount => mItemCount;
        public ComMergeToolSource config => mConfig;
        private ComMergeToolSource mConfig = null;
        private int mItemTotalOutputToDead = 0;
        private int mItemCount = 0;
        private int mItemInRechargeCount = 0;
        
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
            return config?.toolSource != null;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComToolSource = new ComToolSource
            {
                ItemCount = mItemCount,
                TotalOutput = mItemTotalOutputToDead
            };
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if(itemData.ComToolSource != null)
            {
                mItemCount = itemData.ComToolSource.ItemCount;
                mItemTotalOutputToDead = itemData.ComToolSource.TotalOutput;
            }
        }

        protected override void OnInitOutputSet(Dictionary<int, int> container)
        { }

        public int ConsumeNextItem()
        {
            if(!IsNextItemReady())
            {
                return 0;
            }
            mItemCount--;
            var targetId = ConsumeNextOutput(out _);
            if (targetId <= 0)
                return SpawnTool();
            return targetId;
        }

        // public int ConsumeNextItem()
        // {
        //     if(!IsNextItemReady())
        //     {
        //         return 0;
        //     }
        //     if(!isNoCD)
        //     {
        //         mItemCount--;
        //     }
        //     if(mConfig.StageCount > 0)
        //     {
        //         mItemTotalOutputToDead ++;
        //     }
        //     return ConsumeNextOutput();
        // }

        public bool IsNextItemReady()
        {
            return item.isActive && itemCount > 0;
        }

        // public Toast GetToastTypeForItem(int itemTid)
        // {
        //     config.OutputsToast.TryGetValue(itemTid, out var toastType);
        //     return (Toast)toastType;
        // }

        private int ChooseOutputItemLevelByWeight(IDictionary<int, int> dict)
        {
            return dict.RandomChooseByWeight(e => e.Value).Key;
        }

        private int SpawnTool()
        {
            if (ItemUtility.TrySpawnTool(item, out var toolId, out var lackScore))
            {
                var mgr = Game.Manager.mergeItemMan;
                var toolCfg = mgr.GetToolBasicConfig(toolId);
                var cat = mgr.GetCategoryConfig(toolCfg.RelatedCategory);
                var selectedLevel = 0;

                if (lackScore <= 0)
                {
                    selectedLevel = ChooseOutputItemLevelByWeight(config.OutputInfo);
                }
                else
                {
                    using (ObjectPool<Dictionary<int, int>>.GlobalPool.AllocStub(out var dict))
                    {
                        foreach (var kv in config.OutputInfo)
                        {
                            var toolItem = mgr.GetToolMergeConfig(cat.Progress[kv.Key - 1]);
                            if (toolItem != null && toolItem.ToolScore <= lackScore)
                            {
                                // 从所有权重里排除掉不符合的项目
                                // 仅允许 score <= lackScore 的项目产出
                                dict.Add(kv.Key, kv.Value);
                            }
                        }
                        // 没有符合要求的item
                        if (dict.Count < 1)
                        {
                            selectedLevel = ChooseOutputItemLevelByWeight(config.OutputInfo);
                        }
                        else
                        {
                            selectedLevel = ChooseOutputItemLevelByWeight(dict);
                        }
                    }
                }
                return cat.Progress[selectedLevel - 1];
            }
            return 0;
        }

        protected override void OnPostMerge(Item src, Item dst)
        {
            var inheritedCountA = 0;
            var inheritedCountB = 0;
            if (!src.isFrozen && src.TryGetItemComponent<ItemToolSourceComponent>(out var c, true))
            {
                inheritedCountA += c.itemCount;
            }
            if (!dst.isFrozen && dst.TryGetItemComponent<ItemToolSourceComponent>(out c, true))
            {
                inheritedCountB += c.itemCount;
            }
            if (inheritedCountA + inheritedCountB > mItemCount)
            {
                mItemCount = inheritedCountA + inheritedCountB;
                DebugEx.Info($"ItemToolSourceComponent::OnPostMerge ----> {inheritedCountA}@{src} {inheritedCountB}@{dst}");
            }
            base.OnPostMerge(src, dst);
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).toolSource;
            mItemCount = mConfig.Capacity;
        }
    }
}