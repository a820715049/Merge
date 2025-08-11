/*
 * @Author: qun.chao
 * @Date: 2024-01-04 14:29:24
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemOrderBoxComponent : ItemComponentBase
    {
        public ComMergeOrderBox config => mConfig;
        private ComMergeOrderBox mConfig = null;

        public static bool Validate(ItemComConfig config)
        {
            return config?.orderBoxConfig != null;
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).orderBoxConfig;
        }
    }
}