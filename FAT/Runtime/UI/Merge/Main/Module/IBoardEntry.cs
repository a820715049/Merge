/**
 * @Author: zhangpengjian
 * @Date: 2024/10/22 19:08:42
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/22 19:08:42
 * Description: 棋盘活动入口接口
 */

namespace FAT
{
    interface IBoardEntry
    {
        string BoardEntryAsset();
        bool BoardEntryVisible => true;
    }

    interface IActivityBoardEntry
    {
        void RefreshEntry(ActivityLike activity);
    }
}