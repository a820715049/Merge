/*
 * @Author: tang.yan
 * @Description: 随机宝箱特殊Tips界面 空界面 用于换 
 * @Date: 2025-01-17 14:01:23
 */

namespace FAT
{
    public class UIRandomBoxSpecialTips : UITipsBase
    {
        protected override void OnCreate()
        {
            
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 2)
            {
                //设置tips位置参数
                _SetTipsPosInfo(items);
            }
        }

        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos(18);
        }

        protected override void OnPostClose()
        {
            
        }
    }
}