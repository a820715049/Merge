/*
 * @Author: tang.yan
 * @Description: 活动棋盘获取棋盘行配置接口类 - 活动中有棋盘上升/下降逻辑时需继承此接口 
 * @Date: 2025-07-24 18:07:06
 */

using System.Collections.Generic;

namespace FAT
{
    //活动棋盘获取棋盘行配置接口类 - 活动中有棋盘上升/下降逻辑时需继承此接口 
    public interface IBoardActivityRowConf
    {
        IList<int> GetRowConfIdList(int detailId);  //返回棋盘所有的行配置id List
        string GetRowConfStr(int rowId);     //返回指定行配置string
        int GetCycleStartRowId(int detailId);   //返回棋盘循环的起始行id  棋盘不支持循环时就返回0
    }
}