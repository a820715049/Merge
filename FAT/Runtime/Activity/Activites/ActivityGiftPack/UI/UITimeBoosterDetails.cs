/*
 *@Author:chaoran.zhang
 *@Desc:
 *@Created Time:2024.02.29 星期四 13:00:09
 */

using EL;
using TMPro;

namespace FAT
{
    public class UITimeBoosterDetails : UITipsBase
    {
        public TextMeshProUGUI Desc;
        
        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 3)
            {
                _SetCurTipsWidth(868);
                //设置tips位置参数
                _SetTipsPosInfo(items);
                //设置时长文本
                Desc.text = I18N.FormatText("#SysComDesc294", (int)items[2]/60000);
            }
        }

        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos(18);
        }
    }
}