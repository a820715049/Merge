/**
 * @Author: zhangpengjian
 * @Date: 2025/2/19 16:31:13
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/2/19 16:31:13
 * Description: 寻宝结算
 */

namespace FAT
{
    public class UITreasureHuntEnd : UIActivityConvert
    {
        public ActivityTreasure activity;
        public override ActivityVisual Visual => activity.VisualEnd;
        public override bool Complete => true;

        protected override void OnParse(params object[] items)
        {
            activity = (ActivityTreasure)items[0];
            base.OnParse(items);
        }
    }
}