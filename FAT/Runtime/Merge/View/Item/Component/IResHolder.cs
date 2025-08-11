/*
 * @Author: qun.chao
 * @Date: 2022-11-01 11:49:20
 */
using System;
using FAT.Merge;

namespace FAT
{
    public interface IResHolder
    {
        //出生在棋盘上
        void SetBoardState();
        //从奖励箱发到棋盘上
        void SetBornState();
        //出现在奖励箱顶部
        void SetRewardState();
    }
}