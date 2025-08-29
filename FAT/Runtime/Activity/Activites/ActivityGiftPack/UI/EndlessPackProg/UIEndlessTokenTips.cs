/*
 * @Author: tang.yan
 * @Description: 无限礼包进度条版token tips 
 * @Date: 2025-02-14 17:02:48
 */

using EL;
using TMPro;

namespace FAT
{
    public class UIEndlessTokenTips : UITipsBase
    {
        public TextMeshProUGUI desc;
        public TextMeshProUGUI descBg;
        
        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 3)
            {
                _SetCurTipsWidth(934);
                //比实际高度少100 因为底层做高度适配时默认减了100避免挡住顶部资源栏，这个tips没有这个需求 就在这里补上100
                _SetCurTipsHeight(238);
                //设置tips位置参数
                _SetTipsPosInfo(items);
                //设置时长文本
                var tokenId = (int)items[2];
                var str = I18N.FormatText("#SysComDesc835", UIUtility.FormatTMPString(tokenId));
                desc.text = str;
                descBg.text = str;
            }
        }

        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos(10, false);
        }
    }
}