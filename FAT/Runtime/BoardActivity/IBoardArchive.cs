/*
 * @Author: qun.chao
 * @Date: 2025-04-03 14:48:48
 */
using fat.gamekitdata;
using fat.rawdata;

namespace FAT
{
    // 棋盘存档接口
    public interface IBoardArchive
    {
        FeatureEntry Feature { get; }
        void SetBoardData(fat.gamekitdata.Merge data);
        void FillBoardData(fat.gamekitdata.Merge data);
    }
}