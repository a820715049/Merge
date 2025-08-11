/**
 * @Author: handong.liu
 * @Date: 2021-06-11 17:45:44
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FAT;
using EL;

namespace Hotfix
{
    [IFix.CustomBridge]
    public static class AdditionalBridge 
    {
        static List<System.Type> bridge = new List<System.Type>()
        {
            typeof(IEnumerator),
            typeof(IEnumerator<System.Object>),
            typeof(IMergeBoard),
            typeof(IGameModule),
            typeof(IUserDataHolder),
            typeof(IPostSetUserDataListener),
            typeof(IDeltaUserDataModifier),
            typeof(IUserDataInitializer),
            // typeof(IShopProvider),
            // typeof(IShopGoods),
            // typeof(IShopGoodsItem)
        };
    }

}