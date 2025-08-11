/**
 * @Author: handong.liu
 * @Date: 2021-07-01 11:18:30
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace EL
{
    public class Bitmap64
    {
        private const int kBitSize = sizeof(ulong) * 8;
        public List<ulong> data => mData;
        private List<ulong> mData = new List<ulong>();
        private int mNumberBase = 0;

        public Bitmap64(int baseId)
        {
            Init(baseId);
        }

        public Bitmap64()
        {
            Init(0);
        }

        public void Clear()
        {
            mData.Clear();
        }
        public void Reset(IEnumerable<ulong> _data)
        {
            mData.Clear();
            mData.AddRange(_data);
        }
        // public void Serialize32(IList<int> _data)
        // {
        //     _data.Clear();
        //     var container = ObjectPool<List<int>>.GlobalPool.Alloc();
        //     for(int i = 0; i < mData.Count; i++)
        //     {
        //         ulong elem = mData[i];
        //         CommonUtility.SetBitmap(elem, container);
        //         for(int j = 0; j < container.Count; j++)
        //         {
        //             _data.Add(container[j]);
        //         }
        //     }
        // }
        // public void Deseiralize32(IList<int> _data)
        // {
        //     mData.Clear();
        //     var container = ObjectPool<List<int>>.GlobalPool.Alloc();
        //     for(int i = 0; i < _data.Count; i+=2)
        //     {
        //         container.Clear();
        //         container.Add(_data[i]);
        //         if(i + 1 < _data.Count)
        //         {
        //             container.Add(_data[i+1]);
        //         }
        //         mData.Add(CommonUtility.GetBitmap(container));
        //     }
        // }
        public void Init(int numberBase)
        {
            mNumberBase = numberBase;
        }
        //returns: is state changed
        public bool AddId(int id)
        {
            int innerId = id - mNumberBase;
            if(innerId < 0 || innerId > 65535)
            {
                EL.DebugEx.FormatError("BitmapUtility::EncodeIdToBitmap -----> very large id get, error! {0}, {1}", id, mNumberBase);
                return false;
            }
            int idx = innerId / kBitSize;
            ulong mask = (ulong)1 << (innerId % kBitSize);
            while(mData.Count <= idx)
            {
                mData.Add(0);
            }
            var previous = (mData[idx] & mask) == mask;
            mData[idx] |= mask;
            return !previous;
        }

        //returns: is state changed
        public bool RemoveId(int id)
        {
            int innerId = id - mNumberBase;
            if(innerId < 0 || innerId > 65535)
            {
                EL.DebugEx.FormatError("BitmapUtility::EncodeIdToBitmap -----> very large id get, error! {0}, {1}", id, mNumberBase);
                return false;
            }
            int idx = innerId / kBitSize;
            ulong mask = (ulong)1 << (innerId % kBitSize);
            if(mData.Count > idx)
            {
                var previous = (mData[idx] & mask) == mask;
                mData[idx] &= ~mask;
                return previous;
            }
            else
            {
                return false;
            }
        }

        public bool ContainsId(int id)
        {
            int innerId = id - mNumberBase;
            int idx = innerId / kBitSize;
            ulong mask = (ulong)1 << (innerId % kBitSize);
            return mData.Count > idx && (mData[idx] & mask) != 0;
        }

        public bool IsEmpty()
        {
            for(int i = 0; i < mData.Count; i++)
            {
                if(mData[i] > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public int ExtractIds(ICollection<int> ids) => ExtractIdsImpl(mData, mNumberBase, ids);
        public static int ExtractIdsImpl(IList<ulong> mData, int mNumberBase, ICollection<int> ids)
        {
            int ret = 0;
            for(int i = 0; i < mData.Count; i++)
            {
                ulong bits = mData[i];
                int id = kBitSize * i;
                ulong mask = 1;
                for(int j = 0; j < kBitSize; j++, mask <<= 1, id++)
                {
                    if((bits & mask) != 0)
                    {
                        ids?.Add(id + mNumberBase);
                        ret++;
                    }
                }
            }
            return ret;
        }
    }
}