using System;

namespace FAT
{
    [Flags]
    public enum ItemBingoState
    {
        None = 0,   //只完成当前一个item
        ItemCompleted = 1 << 0, //完成一个item
        RowCompleted = 1 << 1, //完成一行   
        ColumnCompleted = 1 << 2, //完成一列
        MainDiagonalCompleted = 1 << 3, //完成左上角都右下角的对角线
        AntiDiagonalCompleted = 1 << 4, //完成左下角到右上角的斜线
        FullHouse = 1 << 5, //完成全部
    }

    public static class BingoStateExtensions
    {
        public static void SetFlag(this ref ItemBingoState current, ItemBingoState flag)
        {
            current = current | flag;
        }

        public static ItemBingoState UnsetFlag(this ref ItemBingoState current, ItemBingoState flag)
        {
            return current & (~flag);
        }

        public static string ToBinaryString(this ItemBingoState state)
        {
            return Convert.ToString((int)state, 2).PadLeft(5, '0');
        }
    }
}
