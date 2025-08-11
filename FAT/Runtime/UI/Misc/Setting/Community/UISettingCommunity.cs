/*
 * @Author: pengjian.zhang
 * @Description: 社区跳转（JoinUs）
 * @Date: 2024-07-09 17:46:23
 */

using System.Collections.Generic;
using UnityEngine;
using EL;
using fat.rawdata;

namespace FAT
{
    public class UISettingCommunity : UIBase
    {
        [SerializeField] private GameObject goItem;
        [SerializeField] private Transform itemRoot;
        private PoolItemType mItemType = PoolItemType.SETTING_COMMUNITY_TYPE_ITEM;
        private Transform emptyText;
        
        protected override void OnCreate()
        {
            transform.AddButton("Content/Root/BtnClose", base.Close).FixPivot();
            emptyText = transform.Find("Content/Empty");
            GameObjectPoolManager.Instance.PreparePool(mItemType, goItem);
        }

        protected override void OnPreOpen()
        {
            var list = Game.Manager.configMan.GetSettingsCommunity();
            var configs = new List<SettingsCommunity>();
            foreach (var config in list)
            {
                var unlock = Game.Manager.activityTrigger.Evaluate(config.UnlockReguire);
                if (unlock)
                    configs.Add(config);
            }
            
            configs.Sort((a, b) => a.Sequence - b.Sequence);
            if (configs.Count > 0)
                UIUtility.CreateGenericPooItem(itemRoot, mItemType, configs);
            emptyText.gameObject.SetActive(configs.Count <= 0);
        }

        protected override void OnPostClose()
        {
            UIUtility.ReleaseClearableItem(itemRoot, mItemType);
        }
    }
}