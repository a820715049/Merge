/**
 * @Author: handong.liu
 * @Date: 2021-05-10 17:32:59
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public class ItemFrozenOverrideComponent : ItemComponentBase
    {
        public int unfrozenPrice => mUnfrozenPrice;
        private int mUnfrozenPrice;
    }
}