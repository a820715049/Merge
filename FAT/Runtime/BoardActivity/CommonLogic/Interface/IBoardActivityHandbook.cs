/*
 * @Author: tang.yan
 * @Description: 活动棋盘棋子图鉴接口类 - 活动中有图鉴系统时需继承此接口 
 * @Date: 2025-07-21 11:07:26
 */

namespace FAT
{
    //活动棋盘棋子图鉴接口类 - 活动中有图鉴系统时需继承此接口 
    public interface IBoardActivityHandbook
    {
        //是否是棋盘专属棋子
        bool CheckIsBoardItem(int itemId);
        //当棋盘中有新棋子解锁时刷新相关数据(数据层)
        void OnNewItemUnlock();
        //当棋盘中有新棋子解锁时执行相关表现(表现层)
        void OnNewItemShow(Merge.Item itemData);
    }
}