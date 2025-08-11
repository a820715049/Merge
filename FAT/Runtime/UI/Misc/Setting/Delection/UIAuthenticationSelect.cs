/*
 * @Author: tang.yan
 * @Description: 删除账号-认证方式选择界面 
 * @Date: 2023-12-18 10:12:00
 */
using System;
using UnityEngine;
using EL;

namespace FAT
{
    public class UIAuthenticationSelect : UIBase
    {
        [SerializeField] private GameObject goItem;
        [SerializeField] private Transform itemRoot;
        private PoolItemType mItemType = PoolItemType.AUTHENTICATION_TYPE_ITEM;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Root/BtnClose", base.Close).FixPivot();
            GameObjectPoolManager.Instance.PreparePool(mItemType, goItem);
        }

        protected override void OnPreOpen()
        {
            var list = AccountDelectionUtility.GetAuthenticationTypeList();
            UIUtility.CreateGenericPooItem(itemRoot, mItemType, list);
        }

        protected override void OnPostClose()
        {
            UIUtility.ReleaseClearableItem(itemRoot, mItemType);
        }
    }
}