/*
 * @Author: lizhenpeng
 * @Date: 2025-09-02 17:45:05
 * @Description: 七日活动文本气泡
 */

using TMPro;
using UnityEngine;

namespace FAT
{
    public class UISevenDayTaskTips: UITipsBase
    {
        [SerializeField] private float edgePadding = 24f;

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            _SetTipsPosInfo(items);
            _SetCurExtraWidth(edgePadding);

            if (items.Length > 2 && items[2] is string s)
            {
                var tf = transform.Find("root/Desc");
                var tmp = tf ? tf.GetComponent<TMP_Text>() : null;
                if (tmp) tmp.SetText(string.IsNullOrEmpty(s) ? " " : s);
            }
        }

        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos(18, true);
        }
    }
}


