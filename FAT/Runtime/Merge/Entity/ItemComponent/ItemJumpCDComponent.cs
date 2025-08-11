/*
 * @Author: qun.chao
 * @Date: 2024-03-01 16:39:44
 */
using fat.rawdata;

namespace FAT.Merge
{
    public class ItemJumpCDComponent : ItemComponentBase
    {
        public bool isCounting => item.world.jumpCD.activeJumpCDId == item.id;
        public int countdown => item.world.jumpCD.countdown;
        public ComMergeJumpCD config => mConfig;
        private ComMergeJumpCD mConfig = null;

        public static bool Validate(ItemComConfig config)
        {
            return config?.jumpCDConfig != null;
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).jumpCDConfig;
        }
    }
}