/**
 * @Author: handong.liu
 * @Date: 2021-04-06 20:54:53
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemBoxComponent : ItemComponentBase
    {
        public int countLeft => Mathf.Max(0, mOutput.Count - mUsedCount);
        public ComMergeBox config => mBoxConfig;
        public int energyCost => config.EnergyCost;
        private int mUsedCount;
        private List<int> mOutput = new List<int>();
        private ComMergeBox mBoxConfig;

        public static bool SerializeDelta(MergeItem newData, MergeItem oldData)
        {
            if(oldData.ComBox != null && oldData.ComBox.Equals(newData.ComBox))
            {
                newData.ComBox = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool Validate(ItemComConfig config)
        {
            return config?.boxConfig != null;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComBox = new ComBox();
            itemData.ComBox.UsedCount = mUsedCount;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if(itemData.ComBox != null)
            {
                mUsedCount = itemData.ComBox.UsedCount;
            }
        }
        
        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mBoxConfig = Env.Instance.GetItemComConfig(item.tid).boxConfig;
            mUsedCount = 0;
            ItemUtility.GetBoxOutputs(item.tid, mOutput);
        }

        public int ConsumeNextItem()
        {
            int ret = 0;
            if(mOutput.Count > mUsedCount)
            {
                ret = mOutput[mUsedCount];    
            }
            mUsedCount++;
            return ret;
        }
    }
}