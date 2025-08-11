/**
 * @Author: handong.liu
 * @Date: 2021-09-30 14:31:59
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class EatGroup
    {
        public Dictionary<int, int> itemNeeded = new Dictionary<int, int>();
        public int changeId;
        //当返回bool时，说明idx已经超越了边界，数据不合法
        public bool Parse(ComMergeEat conf, int idx)
        {
            changeId = 0;
            itemNeeded.Clear();
            if(idx < 0 || idx >= conf.Eat.Count)
            {
                return false;
            }
            var str = conf.Eat[idx];
            changeId = conf.Changeid.GetElementEx(idx, ArrayExt.OverflowBehaviour.Clamp);
            _ParseEatItem(str, itemNeeded);
            return true;
        }
        private static void _ParseEatItem(string str, Dictionary<int, int> container)
        {
            string[] items = str.Split(';');
            foreach(var item in items)
            {
                string[] kv = item.Split(':');
                if(kv.Length == 2 && int.TryParse(kv[0], out var id) && int.TryParse(kv[1], out var count))
                {
                    container[id] = count;
                }
            }
        }
    }
    public class ItemEatComponent : ItemComponentBase
    {
        public ComMergeEat config => mConfig;
        public int eatGroupCount => mEatGroups.Count;
        private List<EatGroup> mEatGroups = new List<EatGroup>();
        private int mEatGroupId = 0;
        private Dictionary<int, int> mItemsWithin = new Dictionary<int, int>();
        private ComMergeEat mConfig;
        private InterlaceOutputMethod mInterlaceRandom;

        public static bool SerializeDelta(MergeItem newData, MergeItem oldData)
        {
            if(oldData.ComEatingSource != null && oldData.ComEatingSource.Equals(newData.ComEatingSource))
            {
                newData.ComEatingSource = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool Validate(ItemComConfig config)
        {
            return config?.eatConfig != null;
        }

        public override void OnStart()
        {
            if(isNew)
            {
                _InitEatGroup(-1);
            }
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComEatingSource = new ComEatingSource();
            foreach(var entry in mItemsWithin)
            {
                itemData.ComEatingSource.ItemsWithin.Add(entry.Key, entry.Value);
            }
            itemData.ComEatingSource.EatGroup = mEatGroupId;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if(itemData.ComEatingSource != null)
            {
                _InitEatGroup(itemData.ComEatingSource.EatGroup);
                foreach(var entry in itemData.ComEatingSource.ItemsWithin)
                {
                    mItemsWithin.Add(entry.Key, entry.Value);
                }
            }
        }

        public int GetItemCountInStomach(int tid)
        {
            return mItemsWithin.GetDefault(tid, 0);
        }

        //idx: eatGroup的序号0开始
        public Dictionary<int, int> GetEatItemNeeded(int idx)
        {
            return mEatGroups.GetElementEx(idx, ArrayExt.OverflowBehaviour.Default)?.itemNeeded;
        }

        public void GetMaxEatProgress(out int idx, out int total, out int current)
        {
            idx = 0;
            GetEatProgress(idx, out total, out current);
            for(int i = 1; i < eatGroupCount; i++)
            {
                GetEatProgress(i, out var total2, out var current2);
                if(current2 * 10000 / total2 > current * 10000 / total)
                {
                    idx = i;
                    total = total2;
                    current = current2;
                }
            }
        }

        //idx: eatGroup的序号0开始
        public void GetEatProgress(int idx, out int total, out int current)
        {
            total = 0;
            current = 0;
            var itemNeeded = GetEatItemNeeded(idx);
            if(itemNeeded != null)
            {
                foreach(var entry in itemNeeded)
                {
                    total += entry.Value;
                    current += mItemsWithin.GetDefault(entry.Key, 0);
                }
            }
        }
        
        protected override void OnPostAttach()
        {
            mConfig = Env.Instance.GetItemComConfig(item.tid).eatConfig;
            _CreateRandomMethod();
            base.OnPostAttach();
        }

        private void _CreateRandomMethod()
        {
            switch(mConfig.RandomMethod)
            {
                case RandomMethodType.Interlace:
                {
                    mInterlaceRandom = item.world.GetInterlaceRandomForId(item.tid);
                    DebugEx.FormatTrace("ItemEatComponent::_CreateRandomMethod ----> create as interlace {0}", mInterlaceRandom);
                }
                break;
            }
        }

        public bool EatItem(Item food)
        {
            return _EatItem(food);
        }

        public bool CanEatItemId(int tid)
        {
            return _CanEatItem(tid);
        }

        private bool _CanEatItem(int tid)
        {
            foreach(var group in mEatGroups)
            {
                foreach(var entry in group.itemNeeded)
                {
                    if(entry.Key == tid && entry.Value > mItemsWithin.GetDefault(tid, 0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void _InitEatGroup(int idx)
        {
            mEatGroups.Clear();
            if(mConfig.Weight.Count == 0)
            {
                mEatGroupId = 0;
                for(var i = 0; i < mConfig.Eat.Count; i++)
                {
                    var f = new EatGroup();
                    f.Parse(mConfig, i);
                    mEatGroups.Add(f);
                }
            }
            else
            {
                if(idx < 0 || idx >= mConfig.Eat.Count)
                {
                    int old = idx;
                    idx = -1;
                    if(mConfig.FixedEat.Count > 0)
                    {
                        var cate = Env.Instance.GetCategoryByItem(item.tid);
                        if(cate != null)
                        {
                            var globalData = Env.Instance.GetGlobalData();
                            int nextFix = globalData.FixedEatId.GetDefault(cate.Id, 0);
                            if(nextFix < mConfig.FixedEat.Count)
                            {
                                DebugEx.FormatInfo("ItemEatComponent::_SetEatGroup ----> {0} idx {1} illegal, use fix eat idx {2}", item, old, nextFix);
                                idx = mConfig.FixedEat[nextFix];
                                globalData.FixedEatId[cate.Id] = nextFix + 1;
                            }
                        }
                    }
                    if(idx < 0)
                    {                
                        if(mInterlaceRandom != null)
                        {
                            idx = mInterlaceRandom.UseNextItem();
                            DebugEx.FormatInfo("ItemEatComponent::_SetEatGroup ----> use interlace {0}", idx);
                        }
                        if(idx < 0)
                        {
                            using(ObjectPool<List<int>>.GlobalPool.AllocStub(out var containers))
                            {
                                for(var i = 0 ; i < mConfig.Eat.Count; i ++)
                                {
                                    int weight = mConfig.Weight.GetElementEx(i, ArrayExt.OverflowBehaviour.Default);
                                    if(weight > 0)
                                    {
                                        containers.Add(i);
                                    }
                                }
                                idx = containers.RandomChooseByWeight((e) => mConfig.Weight[e]);
                            }
                        }
                        DebugEx.FormatInfo("ItemEatComponent::_SetEatGroup ----> {0} idx {1} illegal, random {2}", item, old, idx);
                    }
                }
                mEatGroupId = idx;
                var f = new EatGroup();
                f.Parse(mConfig, idx);
                mEatGroups.Add(f);
            }
        }

        private bool _TryFinishEat()
        {
            EatGroup finishGroup = null;
            foreach(var group in mEatGroups)
            {
                bool notReady = false;
                foreach(var entry in group.itemNeeded)
                {
                    if(GetItemCountInStomach(entry.Key) < entry.Value)
                    {
                        notReady = true;
                        break;
                    }
                }
                if(!notReady)
                {
                    finishGroup = group;
                    break;
                }
            }
            if(finishGroup == null)
            {
                return false;
            }
            var targetId = finishGroup.changeId;
            DebugEx.FormatInfo("ItemEatComponent::_FinishEat ----> {0} transform to {1}", item, targetId);
            item.parent.ChangeItem(item, targetId, ItemDeadType.Eat, ItemSpawnContext.SpawnType.Eat);
            return true;
        }

        private bool _EatItem(Item food)
        {
            if(food.isActive && _CanEatItem(food.tid))
            {
                DebugEx.FormatInfo("ItemEatComponent::_EatItem ----> {0} eat {1}", item, food);
                mItemsWithin[food.tid] = mItemsWithin.GetDefault(food.tid, 0) + 1;
                item.parent.TriggerItemStatusChange(item);
                _TryFinishEat();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}