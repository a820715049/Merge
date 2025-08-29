/**
 * @Author: zhangpengjian
 * @Date: 2025/6/30 14:14:02
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/6/30 14:14:02
 * Description: 连续订单活动结束界面
 */

using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityOrderStreakConvert : UIBase
    {
        [SerializeField] private Button btnClose;

        protected override void OnCreate()
        {
            base.OnCreate();
            btnClose.onClick.AddListener(OnClickClose);
        }

        private void OnClickClose()
        {
            Close();
        }
    }
}
