/*
 * @Author: qun.chao
 * @Date: 2025-07-25 12:48:33
 */
using UnityEngine;

namespace FAT
{
    public class UIClawOrderTips : UITipsBase
    {
        [SerializeField] private float offsetY = 18;
        [SerializeField] private float width = 314;

        protected override void OnParse(params object[] items)
        {
            _SetCurTipsWidth(width);
            // 设置tips位置参数
            _SetTipsPosInfo(items);
        }

        protected override void OnPreOpen()
        {
            // 刷新tips位置
            _RefreshTipsPos(offsetY);
        }
    }
}