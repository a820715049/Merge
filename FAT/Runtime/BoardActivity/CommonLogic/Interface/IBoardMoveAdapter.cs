/*
 * @Author: tang.yan
 * @Description: 棋盘移动接口类
 * @Date: 2025-08-05 16:08:42
 */

using FAT.Merge;

namespace FAT
{
    public interface IBoardMoveAdapter
    {
        //返回棋盘移动需要存在的连续空行数  棋盘由云层解锁控制移动时就返回0
        int GetMoveNeedRowCount(int detailId);
        //根据具体的行配置返回在棋盘移动时指定移动的行数 棋盘不由云层解锁控制移动时就返回0
        int GetMoveCountByRowId(int rowId);
        //获取当前棋盘
        Board GetBoard();
        //同步更新当前深度值
        void OnDepthIndexUpdate(int newDepth);
    }
}