/*
 * @Author: qun.chao
 * @Date: 2025-02-20 16:45:30
 */
using System;

namespace FAT.Merge
{
    public enum ItemIndType
    {
        None,
        Bingo,
        TrainMission,
    }

    public interface IMergeItemIndicatorHandler
    {
        // 需要刷新时触发
        event Action Invalidate;
        // 检查Ind类型
        ItemIndType CheckIndicator(int itemId, out string asset);
    }
}