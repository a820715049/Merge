/*
 * @Author: qun.chao
 * @Date: 2021-03-03 20:20:32
 */
using FAT.Merge;

namespace FAT
{
    public class GuideActImpSourceCtrl : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (bool.TryParse(param[0], out bool b))
            {
                // 传入true表示disable
                BoardViewManager.Instance.world.DisableComponent(ItemComponentType.ClickSouce, !b);
            }
            else
            {
                BoardViewManager.Instance.world.DisableComponent(ItemComponentType.ClickSouce, false);
            }
            mIsWaiting = false;
        }
    }
}