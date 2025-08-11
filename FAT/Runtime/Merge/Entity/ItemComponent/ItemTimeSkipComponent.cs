/**
 * @Author: handong.liu
 * @Date: 2021-03-16 18:28:16
 */
using fat.rawdata;

namespace FAT.Merge
{
    public class ItemTimeSkipCompoent: ItemComponentBase
    {
        public ComMergeTimeSkip config => mConfig;
        private ComMergeTimeSkip mConfig;

        public static bool Validate(ItemComConfig config)
        {
            return config?.timeSkipConfig != null;
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).timeSkipConfig;
        }
    }
}