/*
 * @Author: qun.chao
 * @Date: 2021-05-25 20:01:05
 */
using FAT.Merge;

namespace FAT
{
    public class GuideActImpMergeCtrl : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (bool.TryParse(param[0], out bool b))
            {
                // 传入true表示disable
                BoardViewManager.Instance.world.DisableComponent(ItemComponentType.Merge, !b);
            }
            else
            {
                BoardViewManager.Instance.world.DisableComponent(ItemComponentType.Merge, false);
            }
            mIsWaiting = false;
        }
    }
}