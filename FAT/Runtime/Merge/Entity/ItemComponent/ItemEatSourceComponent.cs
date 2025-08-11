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
    public class ItemEatSourceComponent : ItemSourceComponentBase
    {
        private class EatGroup
        {
            public Dictionary<int, int> itemNeeded = new Dictionary<int, int>();
            public int weight;
            public static EatGroup Parse(string str)
            {
                EatGroup ret = new EatGroup();
                string[] items = str.Split(';');
                foreach(var item in items)
                {
                    string[] kv = item.Split(':');
                    if(kv.Length == 2 && int.TryParse(kv[0], out var id) && int.TryParse(kv[1], out var count))
                    {
                        ret.itemNeeded[id] = count;
                    }
                }
                return ret;
            }
        }
        public enum Status
        {
            Empty,
            Eating,
            Output
        }
        public Status state {
            get {
                if(countLeft > 0)
                {
                    return Status.Output;
                }
                else if(mEatingTimeLeft > 0)
                {
                    return Status.Eating;
                }
                else
                {
                    return Status.Empty;
                }
            }
        }
        public int energyCost => mConfig.EnergyCost;
        public Dictionary<int, int> eatItemNeeded => mEatGroup.itemNeeded;
        public int eatMilli => Mathf.Max(0, mEatingTotalTime - mEatingTimeLeft);
        public int eatLeftMilli => mEatingTimeLeft;
        public int eatTotalMilli => mEatingTotalTime;
        public int countLeft => mCountLeft;
        public bool canUse =>  countLeft > 0;
        public ComMergeEatSource config => mConfig;
        private List<EatGroup> mEatGroups = new List<EatGroup>();
        private List<EatGroup> mFixEatGroups = new List<EatGroup>();
        private EatGroup mEatGroup = null;
        private Dictionary<int, int> mItemsWithin = new Dictionary<int, int>();
        private ComMergeEatSource mConfig;
        private int mEatingTimeLeft;
        private int mEatingTotalTime;
        private int mCountLeft;

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
            return config?.eatSourceConfig != null;
        }

        public override void OnStart()
        {
            if(isNew)
            {
                _SetEatGroup(-1);
            }
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComEatingSource = new ComEatingSource();
            itemData.ComEatingSource.CountLeft = mCountLeft;
            foreach(var entry in mItemsWithin)
            {
                itemData.ComEatingSource.ItemsWithin.Add(entry.Key, entry.Value);
            }
            itemData.ComEatingSource.RandomNextIdx = randomOutputNextIdx;
            itemData.ComEatingSource.RandomSeed = randomOutputSeed;
            itemData.ComEatingSource.EatingTimeLeft = mEatingTimeLeft;
            itemData.ComEatingSource.EatGroup = mEatGroups.IndexOf(mEatGroup);
            if(itemData.ComEatingSource.EatGroup < 0)
            {
                itemData.ComEatingSource.EatGroup = mFixEatGroups.IndexOf(mEatGroup) + 1000;
            }
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if(itemData.ComEatingSource != null)
            {
                mEatingTimeLeft = itemData.ComEatingSource.EatingTimeLeft;
                mCountLeft = itemData.ComEatingSource.CountLeft;
                _SetEatGroup(itemData.ComEatingSource.EatGroup);
                foreach(var entry in itemData.ComEatingSource.ItemsWithin)
                {
                    mItemsWithin.Add(entry.Key, entry.Value);
                }
                SetRandomOutputParam(itemData.ComEatingSource.RandomSeed, itemData.ComEatingSource.RandomNextIdx);
            }
        }

        public int GetItemCountInStomach(int tid)
        {
            return mItemsWithin.GetDefault(tid, 0);
        }

        public void GetProgress(out int total, out int current)
        {
            total = 0;
            current = 0;
            foreach(var entry in mEatGroup.itemNeeded)
            {
                total += entry.Value;
                current += mItemsWithin.GetDefault(entry.Key, 0);
            }
        }

        public void SetFTEEatTime(int eatTime)
        {
            if(mEatingTimeLeft > 0)
            {
                DebugEx.FormatInfo("ItemEatSourceComponent::SetFTEEatTime ----> {0}", eatTime);
                mEatingTimeLeft = eatTime * 1000;
                mEatingTotalTime = eatTime * 1000;
            }
        }
        
        protected override void OnPostAttach()
        {
            mConfig = Env.Instance.GetItemComConfig(item.tid).eatSourceConfig;
            base.OnPostAttach();
            mEatGroups.Clear();
            for(int i = 0; i < mConfig.Eat.Count; i++)
            {
                mEatGroups.Add(EatGroup.Parse(mConfig.Eat[i]));
                mEatGroups[i].weight = Mathf.Max(1, mConfig.Weight.GetElementEx(i, ArrayExt.OverflowBehaviour.Default));
            }
            mFixEatGroups.Clear();
            for(int i = 0; i < mConfig.FixedEat.Count; i++)
            {
                mFixEatGroups.Add(EatGroup.Parse(mConfig.FixedEat[i]));
                mFixEatGroups[i].weight = 1;
            }
            mCountLeft = 0;
            mEatingTotalTime = mConfig.EatTime * 1000;
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

        protected override void OnInitOutputSet(Dictionary<int, int> container)
        {
            ItemUtility.GetEatSourceOutputs(item.tid, container);
        }

        public int CalculateSpeedEatCost()
        {
            return EL.MathUtility.LerpInteger(0, mConfig.SpeedCost, eatLeftMilli, mConfig.EatTime * 1000);
        }

        public bool EatItem(Item food)
        {
            if(state != Status.Empty)
            {
                return false;
            }
            return _EatItem(food);
        }

        public bool SpeedEat()
        {
            if(state == Status.Eating)
            {
                if(_FinishEat())
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

        protected override void OnUpdate(int dt)
        {
            base.OnUpdate(dt);
            if(mEatingTimeLeft > 0 || (mEatingTotalTime <= 0 && mItemsWithin.Count > 0))
            {
                mEatingTimeLeft -= dt;
                if(mEatingTimeLeft <= 0)
                {
                    mEatingTimeLeft = 0;
                    _FinishEat();
                }
            }
        }

        public int ConsumeNextItem()
        {
            if(state == Status.Output)
            {
                if(mCountLeft > 0)
                {
                    mCountLeft--;
                    if(mCountLeft == 0)
                    {
                        item.parent.TriggerItemStatusChange(item);
                    }
                    return ConsumeNextOutput(out _);
                }
            }
            return 0;
        }

        private bool _CanEatItem(Item item)
        {
            foreach(var entry in mEatGroup.itemNeeded)
            {
                if(entry.Key == item.tid && entry.Value > mItemsWithin.GetDefault(item.tid, 0))
                {
                    return true;
                }
            }
            return false;
        }

        private void _SetEatGroup(int idx)
        {
            if((idx < 0 || idx >= mEatGroups.Count) &&
                (idx < 1000 || idx - 1000 >= mFixEatGroups.Count))
            {
                int old = idx;
                idx = -1;
                if(mFixEatGroups.Count > 0)
                {
                    var cate = Env.Instance.GetCategoryByItem(item.tid);
                    if(cate != null)
                    {
                        var globalData = Env.Instance.GetGlobalData();
                        int nextFix = globalData.FixedEatId.GetDefault(cate.Id, 0);
                        if(nextFix < mFixEatGroups.Count)
                        {
                            DebugEx.FormatInfo("ItemEatSourceComponent::_SetEatGroup ----> {0} idx {1} illegal, use fix eat idx {2}", item, old, nextFix);
                            idx = 1000 + nextFix;
                            globalData.FixedEatId[cate.Id] = nextFix + 1;
                        }
                    }
                }
                if(idx < 0)
                {                
                    var r = mEatGroups.RandomChooseByWeight((e)=>e.weight);
                    idx = mEatGroups.IndexOf(r);
                    DebugEx.FormatInfo("ItemEatSourceComponent::_SetEatGroup ----> {0} idx {1} illegal, random {2}", item, old, idx);
                }
            }
            if(idx < 1000)
            {
                mEatGroup = mEatGroups[idx];
            }
            else
            {
                mEatGroup = mFixEatGroups[idx - 1000];
            }
        }

        private bool _StartEat()
        {
            foreach(var entry in mEatGroup.itemNeeded)
            {
                if(entry.Value > mItemsWithin.GetDefault(entry.Key, 0))
                {
                    DebugEx.FormatInfo("ItemEatSourceComponent::_StartEat ----> {0}: {1} item needed", item, entry.Key);
                    return false;
                }
            }
            DebugEx.FormatInfo("ItemEatSourceComponent::_StartEat ----> {0} start eat", item);
            mEatingTimeLeft = mConfig.EatTime * 1000;
            mEatingTotalTime = mConfig.EatTime * 1000;
            item.parent.TriggerItemStatusChange(item);
            if(mEatingTotalTime <= 0)
            {
                _FinishEat();
            }
            return true;
        }

        private bool _FinishEat()
        {
            mEatingTimeLeft = 0;
            mCountLeft = mConfig.LimitCount;
            DebugEx.FormatInfo("ItemEatSourceComponent::_FinishEat ----> {0}: {1} item generated", item, mCountLeft);
            item.parent.TriggerItemStatusChange(item);
            mItemsWithin.Clear();
            _SetEatGroup(-1);
            return true;
        }

        private bool _EatItem(Item food)
        {
            if(food.isActive && _CanEatItem(food))
            {
                DebugEx.FormatInfo("ItemEatSourceComponent::_EatItem ----> {0} eat {1}", item, food);
                mItemsWithin[food.tid] = mItemsWithin.GetDefault(food.tid, 0) + 1;
                _StartEat();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}