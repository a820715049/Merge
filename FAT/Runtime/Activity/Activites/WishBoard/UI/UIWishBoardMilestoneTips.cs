/*
 * @Author: yanfuxing
 * @Date: 2025-06-13 16:32:18
 */
namespace FAT
{
    public class UIWishBoardMilestoneTips : UITipsBase
    {
        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            _SetTipsPosInfo(items);
        }

        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos(18, false);
        }
    }
}