/*
 * @Author: qun.chao
 * @Date: 2022-11-01 12:08:18
 */
using System;
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class MBResHolderBase : MonoBehaviour, IResHolder
    {
        public virtual void OnInit(Item item) { }

        public virtual void OnClear() { }
        
        public virtual void SetBoardState() { }

        public virtual void SetBornState() { }

        public virtual void SetRewardState() { }
    }
}