/**
 * @Author: handong.liu
 * @Date: 2023-02-21 15:32:25
 */

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public interface IInventoryBagMetaInfo
    {

    }
    public class InventoryBag
    {
        public int id => mId;
        public int capacity => mContent.Length;
        public int priority;
        public int MaxShowGirdNum { get; set; } //界面中最大可显示的格子数
        public BagMan.BagType bagType;

        // [IFix.Interpret] version30
        public bool MetaInfoValid => mId == 0 || mMetaInfo != null;
        private Item[] mContent = new Item[0];
        private int mId = 0;
        private IInventoryBagMetaInfo mMetaInfo;
        private List<int> RedPointItem = new();


        public T GetMetaInfo<T>() where T : class, IInventoryBagMetaInfo
        {
            return mMetaInfo as T;
        }

        public void SetMetaInfo(IInventoryBagMetaInfo info)
        {
            mMetaInfo = info;
        }

        public InventoryBag(int id)
        {
            mId = id;
            priority = id;
            if (Enum.IsDefined(typeof(BagMan.BagType), id))
            {
                bagType = (BagMan.BagType)id;
            }
            else
            {
                bagType = BagMan.BagType.None;
            }
        }

        public Item[] Clear()
        {
            var ret = mContent;
            mContent = new Item[mContent.Length];
            return ret;
        }

        public Item PeekItem(int idx)
        {
            return mContent.GetElementEx(idx, ArrayExt.OverflowBehaviour.Default);
        }

        public Item RemoveItem(int idx)
        {
            var item = mContent.GetElementEx(idx, ArrayExt.OverflowBehaviour.Default);
            if (item != null)
            {
                mContent[idx] = null;
                _Shrink();
            }
            return item;
        }

        public bool DisposeItem(Item i)
        {
            int idx = mContent.IndexOf(i);
            if (idx < 0)
            {
                return false;
            }
            mContent[idx] = null;
            _Shrink();
            return true;
        }
        //return negative means put failed
        public int PutItem(Item item)
        {
            if (mContent.Length <= 0 || mContent[mContent.Length - 1] != null) //full
            {
                return -1;
            }
            int idx = -1;
            for (int i = 0; i < mContent.Length && item != null; i++)
            {
                if (mContent[i] == null || mContent[i].tid > item.tid || idx >= 0)           //if idx >= 0, means we should push all element after
                {
                    Item temp = item;
                    item = mContent[i];
                    mContent[i] = temp;
                    if (idx < 0)
                    {
                        idx = i;
                    }
                }
            }
            return idx;
        }

        //将物品放到指定index的格子中去
        public int PutItemWithIndex(Item item, int putIndex)
        {
            //不在范围内
            if (mContent.Length <= 0 || mContent.Length < putIndex)
            {
                return -1;
            }
            var targetItem = PeekItem(putIndex);
            //对应index的格子上已有其他棋子
            if (targetItem != null)
                return -1;
            mContent[putIndex] = item;
            return putIndex;
        }

        //如果capacity设小，将丢弃的部分被返回
        //然而整个返回数组保持之前的大小，从而能够和槽位一一对应
        public Item[] SetCapacity(int capacity)
        {
            Item[] ret = new Item[mContent.Length];
            for (int i = capacity; i < mContent.Length; i++)
            {
                ret[i] = mContent[i];
            }
            System.Array.Resize(ref mContent, capacity);
            return ret;
        }

        public int GetItemIndexByTid(int tid)
        {
            for (int i = 0; i < mContent.Length; i++)
            {
                if (mContent[i] != null && mContent[i].tid == tid)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Update(int dt)
        {
            foreach (var i in mContent)
            {
                if (i != null)
                {
                    i.UpdateInactive(dt);
                }
            }
        }

        public void Serialize(fat.gamekitdata.MergeBag data)
        {
            data.Id = id;
            data.InvItems.Clear();
            //生成器背包允许有空位 存档时空位按0处理记录
            if (bagType == BagMan.BagType.Producer)
            {
                foreach (var i in mContent)
                {
                    data.InvItems.Add(i?.id ?? 0);
                }
            }
            //其他背包不允许有空位 逻辑上会自动前移补0 所以存档中也不能记录
            else
            {
                foreach (var i in mContent)
                {
                    if (i != null)
                    {
                        data.InvItems.Add(i.id);
                    }
                }
            }
            data.InvCapacity = mContent.Length;
            data.RedPointItems?.AddRange(RedPointItem);
        }

        //put item in, and remove item from items container
        public void Deserialize(fat.gamekitdata.MergeBag data, Dictionary<int, Item> items)
        {
            mId = data.Id;
            mContent = new Item[data.InvCapacity];
            for (int i = 0; i < data.InvItems.Count; i++)
            {
                if (i >= mContent.Length)
                {
                    DebugEx.FormatError(
                        "Merge.Inventory ----> data has more item than capaciy, capacity {0}@{1}, item ignored {2}",
                        mContent.Length, mId, data.InvItems[i]);
                }
                else
                {
                    var item = items.GetDefault(data.InvItems[i], null);
                    if (item == null)
                    {
                        //生成器背包允许有空位 存档时空位按0处理记录
                        if (bagType == BagMan.BagType.Producer)
                        {
                            mContent[i] = null;
                        }
                        else
                        {
                            DebugEx.FormatError("Merge.Inventory ----> item {0}@{1} is missing. ", mId,
                                data.InvItems[i]);
                        }
                    }
                    else
                    {
                        mContent[i] = item;
                    }

                    items.Remove(data.InvItems[i]);
                }
            }

            RedPointItem.AddRange(data.RedPointItems);
        }

        //算法的目的是将数组压缩 补齐中间为空的元素
        private void _Shrink()
        {
            //生成器背包各个格子之间允许不放物品
            if (bagType == BagMan.BagType.Producer)
                return;
            for (int i = 0, step = 0; i < mContent.Length; i++)
            {
                if (mContent[i] != null)
                {
                    var c = mContent[i];
                    mContent[i] = null;
                    mContent[i - step] = c;
                }
                else
                {
                    step++;
                }
            }
        }

        public void TryAddRedPointItem(int id)
        {
            if (!RedPointItem.Contains(id))
            {
                RedPointItem.Add(id);
                MessageCenter.Get<MSG.UI_INVENTORY_REFRESH_RED_POINT>().Dispatch();
            }
        }

        public void TryRemoveRedPointItem(int id)
        {
            if (RedPointItem.Contains(id))
            {
                RedPointItem.Remove(id);
                MessageCenter.Get<MSG.UI_INVENTORY_REFRESH_RED_POINT>().Dispatch();
            }
        }

        public void TryClearRedPoint()
        {
            RedPointItem.Clear();
            MessageCenter.Get<MSG.UI_INVENTORY_REFRESH_RED_POINT>().Dispatch();
        }

        public bool NeedRedPoint()
        {
            return RedPointItem.Count > 0;
        }
    }
}