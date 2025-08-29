/*
 * @Author: tang.yan
 * @Description: 棋盘卡死接口类 
 * @Date: 2025-08-06 18:08:41
 */
using FAT.Merge;

namespace FAT
{
    public interface IBoardExtremeAdapter
    {
        //获取当前棋盘
        Board GetBoard();
        //目前是否可以检测卡死情况
        bool CanCheckExtreme();
    }
}