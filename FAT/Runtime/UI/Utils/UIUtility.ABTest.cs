/*
 * @Author: qun.chao
 * @Date: 2024-09-20 18:24:30
 */
using UnityEngine;

namespace FAT
{
    public static partial class UIUtility
    {
        public static void ABTest_BoardItemSize(RectTransform content, RectTransform cover)
        {
            var groupId = Game.Manager.playerGroupMan.GetPlayerGroup(9);
            // if (groupId != 1)
            // 20240428 9.0调整，即使是1组，也要按照配置调整尺寸
            {
                // 3类 非1组
                // 设置特殊item尺寸
                // 以下字段理论上会在棋子显示前优先赋值

                var origSize = BoardUtility.cellSize;
                if (origSize <= 0f)
                {
                    origSize = 136f;
                }
                // 百分比
                if (Game.Manager.configMan.globalConfig.ItemScaleTestB.TryGetValue(groupId, out var scaleInt))
                {
                    var scale = scaleInt * 0.01f;
                    content.sizeDelta = origSize * scale * Vector2.one;
                }
            }
        }

        // 64.4 / 64.5 为测试组需要有新增表现效果 | 现在到global中做ab测试
        public static bool ABTest_OrderItemChecker()
        {
            return Game.Manager.configMan.globalConfig.IsNewOrderView;
        }
    }
}