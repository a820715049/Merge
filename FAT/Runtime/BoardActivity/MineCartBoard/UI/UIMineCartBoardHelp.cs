/**
 * @Author: lizhenpeng
 * @Date: 2025/8/1 19:08
 * @LastEditors: lizhenpeng
 * @LastEditTime: 2025/8/1 19:08
 * @Description: 矿车活动帮助界面 —— 根据 EventMineCartDetail 的 dropId/orderItem 配置，
 *              判断产出类型（耗体 or 订单），动态设置多段说明文本及图标。
 */

using EL;
using FAT;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIMineCartBoardHelp : UIActivityHelp
    {
        [SerializeField]
        private TMP_Text text1;
        [SerializeField]
        private TMP_Text entryinfo;
        [SerializeField]
        private TMP_Text text2;
        [SerializeField]
        private TMP_Text text3;
        [SerializeField]
        private TMP_Text text4;

        private MineCartActivity activity;

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnParse(params object[] items)
        {
            activity = items[0] as MineCartActivity;
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();

            if (activity == null)
                return;

            var conf = activity.ConfD;
            var detail = activity.GetCurDetailConfig();
            if (detail == null)
            {
                Debug.LogError($"[UICartBoardHelp] 未找到 detail 配置, detail: id={conf.Detail}");
                return;
            }

            var tokenId = conf.SpriteItemId;
            var tokenStr = UIUtility.FormatTMPString(tokenId);

            // 判断产出类型
            bool isOrder = detail.DropId.Count == 0;
            bool isDrop = detail.OrderItem.Count == 0;

            if (isOrder == isDrop)
            {
                Debug.LogWarning($"[UICartBoardHelp] 配置非法,DropId 和 OrderItem 同时为空或同时非空，无法判断产出方式");
                return;
            }

            string descKey = isOrder ? "#SysComDesc904" : "#SysComDesc298"; // 订单 or 耗体

            text1.SetText(I18N.FormatText(descKey, tokenStr));
            entryinfo.SetText(I18N.FormatText("#SysComDesc1537"));
            text2.SetText(I18N.FormatText("#SysComDesc86", tokenStr));
            text3.SetText(I18N.FormatText("#SysComDesc1492", tokenStr));
            text4.SetText(I18N.FormatText("#SysComDesc1492", tokenStr));
        }
    }
}
