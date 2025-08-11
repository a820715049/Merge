/**
 * @Author: handong.liu
 * @Date: 2021-02-24 20:22:47
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public class Inventory
    {
        public const int kDefaultBag = 0;
        //总大小
        public int capacity {
            get {
                var cap = 0;
                foreach(var b in mBags.Values)
                {
                    cap += b.capacity;
                }
                return cap;
            }
        }
        private Dictionary<int, InventoryBag> mBags = new Dictionary<int, InventoryBag>();
        public event System.Action<int, Item[], bool> onCapacityChange;           //bagid, 变化的物品, isDelete
        // public int capacity => mContent.Length;
        // private Item[] mContent = new Item[0];
        private IMergeWorldPrivate mParent;

        private List<InventoryBag> mCachedBagList = new List<InventoryBag>();
        private List<InventoryBag> _GetBagListSorted(bool reversed = false)
        {
            mCachedBagList.Clear();
            mCachedBagList.AddRange(mBags.Values);
            if (reversed)
            {
                mCachedBagList.Sort(_SortBagReversed);
            }
            else
            {
                mCachedBagList.Sort(_SortBag);
            }
            return mCachedBagList;
        }
        private int _SortBag(InventoryBag a, InventoryBag b)
        {
            return a.priority - b.priority;
        }
        private int _SortBagReversed(InventoryBag a, InventoryBag b)
        {
            return a.priority - b.priority;
        }

        public int GetCapacity(int bagId)
        {
            return mBags.GetDefault(bagId, null)?.capacity ?? 0;
        }

        public int FillAllBagId(List<int> idContainer = null)
        {
            if (idContainer != null)
            {
                foreach (var b in _GetBagListSorted())
                {
                    if (!b.MetaInfoValid) {
                        DeleteBag(b.id);
                        continue;
                    }
                    idContainer.Add(b.id);
                }
            }
            return mBags.Count;
        }

        public T GetBagMetaInfo<T>(int bagId) where T : class, IInventoryBagMetaInfo
        {
            if(mBags.TryGetValue(bagId, out var bag))
            {
                return bag.GetMetaInfo<T>();
            }
            else
            {
                return default;
            }
        }

        public void SetBagMetaInfo(int bagId, IInventoryBagMetaInfo info)
        {
            if(mBags.TryGetValue(bagId, out var bag))
            {
                bag.SetMetaInfo(info);
            }
        }

        public bool AddBag(int bagId)
        {
            if (mBags.ContainsKey(bagId))
            {
                return false;
            }
            mBags.Add(bagId, new InventoryBag(bagId));
            return true;
        }

        //计算所有背包当前的总空余容量以及存放的棋子数量
        public void CalcInventoryMetric(out int itemNum, out int spaceNum)
        {
            itemNum = 0;
            spaceNum = 0;
            foreach(var bag in mBags.Values)
            {
                spaceNum += bag.capacity;
                for (int i = 0; i < bag.capacity; ++i)
                {
                    if (bag.PeekItem(i) != null)
                    {
                        ++itemNum;
                        --spaceNum;
                    }
                }
            }
        }

        public void WalkAllItem(System.Action<Item> func)
        {
            foreach(var bag in mBags.Values)
            {
                for(int i = 0; i < bag.capacity; i++)
                {
                    var item = bag.PeekItem(i);
                    if(item != null)
                    {
                        func(item);
                    }
                }
            }
        }

        //返回bag里的所有位置，里面null表示一个空位。每个item都放到了rewardlist中
        //如果返回null，说明bag不存在
        public Item[] DeleteBag(int bagId)
        {
            if (mBags.TryGetValue(bagId, out var bag))
            {
                var items = bag.Clear();
                foreach(var item in items)
                {
                    if(item != null)
                    {
                        mParent.world.AddReward(item, false);
                    }
                }
                onCapacityChange?.Invoke(bagId, items, true);
                mBags.Remove(bagId);
                // mParent.world.TriggerInventoryCapacityChange(bagId, items);
                return items;
            }
            else
            {
                return null;
            }
        }

        public void SetBagPriority(int bagId, int priority)
        {
            if (mBags.TryGetValue(bagId, out var bag))
            {
                bag.priority = priority;
            }
        }

        public Inventory(IMergeWorldPrivate p)
        {
            mParent = p;
        }
        
        public InventoryBag GetBagById(int bagId)
        {
            if (mBags.TryGetValue(bagId, out var bag))
            {
                return bag;
            }
            else
            {
                return null;
            }
        }
        
        public InventoryBag GetBagByType(BagMan.BagType type)
        {
            int bagId = (int) type;
            if (mBags.TryGetValue(bagId, out var bag))
            {
                return bag;
            }
            else
            {
                return null;
            }
        }

        public Item PeekItem(int idx, int bagId = 0)
        {
            if (mBags.TryGetValue(bagId, out var bag))
            {
                return bag.PeekItem(idx);
            }
            else
            {
                return null;
            }
        }

        public void Serialize(fat.gamekitdata.Merge data)
        {
            data.Inventory.Clear();
            foreach (var b in mBags.Values)
            {
                var d = new fat.gamekitdata.MergeBag();
                b.Serialize(d);
                data.Inventory.Add(d);
            }
        }

             
        //put item in, and remove item from items container
        public void Deserialize(fat.gamekitdata.Merge data, Dictionary<int, Item> items)
        {
            mBags.Clear();
            foreach (var b in data.Inventory)
            {
                var newBag = new InventoryBag(b.Id);
                newBag.Deserialize(b, items);
                mBags.Add(newBag.id, newBag);
            }
        }

        public void Update(int dt)
        {
            foreach (var bag in mBags.Values)
            {
                if (bag != null)
                {
                    bag.Update(dt);
                }
            }
        }

        public Item RemoveItem(int idx, int bagId = 0)
        {
            if (mBags.TryGetValue(bagId, out var bag))
            {
                return bag.RemoveItem(idx);
            }
            else
            {
                return null;
            }
        }

        public void DisposeItem(Item i)
        {
            foreach (var bag in _GetBagListSorted())
            {
                if (bag != null && bag.DisposeItem(i))
                {
                    return;
                }
            }
            DebugEx.FormatInfo("Inventory::DisposeItem ----> not in Inventory {0}:{1}", i.id, i.tid);
        }

        public bool GetItemIndexByTid(int tid, out int idx, out int bagId)
        {
            idx = -1;
            bagId = 0;
            foreach (var bag in _GetBagListSorted())
            {
                idx = bag.GetItemIndexByTid(tid);
                if (idx >= 0)
                {
                    bagId = bag.id;
                    return true;
                }
            }
            return false;
        }

        public bool PutItem(Item item, out int idx, out int bagId)
        {
            idx = -1;
            bagId = 0;
            //判断棋子是否可以进入生成器背包 是的话优先进生成器背包
            var girdData = Game.Manager.bagMan.CheckCanPutProducerBag(item);
            if (girdData != null)
            {
                var producerBag = GetBagByType(BagMan.BagType.Producer);
                idx = producerBag.PutItemWithIndex(item, girdData.GirdIndex);
                if (idx >= 0)
                {
                    bagId = producerBag.id;
                    GetBagByType(BagMan.BagType.Producer).TryRemoveRedPointItem(item.tid);
                }
            }
            else
            {
                //若不能进入生成器 则按正常棋子规则
                foreach (var bag in _GetBagListSorted())
                {
                    if (bag.bagType == BagMan.BagType.Item)
                    {
                        idx = bag.PutItem(item);
                        if (idx >= 0)
                        {
                            bagId = bag.id;
                            break;
                        }
                    }
                }
            }
            return idx >= 0;
        }

        // public void AddCapacity()
        // {
        //     System.Array.Resize(ref mContent, mContent.Length + 1);
        // }

        public Item[] SetCapacity(int capacity, int bagId = 0)
        {
            if (mBags.TryGetValue(bagId, out var bag))
            {
                var ret = bag.SetCapacity(capacity);
                if(ret != null)
                {
                    foreach(var r in ret)
                    {
                        if(r != null)
                        {
                            mParent.world.AddReward(r, false);
                        }
                    }
                    onCapacityChange?.Invoke(bagId, ret, false);
                }
                return ret;
            }
            else
            {
                DebugEx.FormatError("Inventory::SetCapacity ----> bag not exists {0}", bagId);
                return null;
            }
        }
    }
}