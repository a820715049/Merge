/**
 * @Author: handong.liu
 * @Date: 2021-09-06 18:02:05
 */
using fat.rawdata;

namespace FAT.Merge
{
    public class ItemFeatureComponent : ItemComponentBase
    {
        public FeatureEntry feature => mConfig.Feature;
        public int intParam => mConfig.Param;
        private ComMergeFeature mConfig;

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).featureConfig;
        }
    }
}