/*
 * @Author: qun.chao
 * @Date: 2023-10-24 16:18:43
 */
using System.Collections.Generic;

namespace FAT
{
    public class MainOrderHelper : IOrderHelper
    {
        Dictionary<int, int> IOrderHelper.NpcInUseCountDict { get; } = new();
        Dictionary<int, int> IOrderHelper.RequireItemStateCache { get; } = new();
        Dictionary<int, int> IOrderHelper.RequireItemTargetCountDict { get; } = new ();
        int IOrderHelper.ActiveOrderCount { get; set; }
        OrderGroupProxy IOrderHelper.proxy { get; set; }
        public List<int> ImmediateSlotRequests { get; } = new();

        bool IOrderHelper.CheckCond_Level(int level)
        {
            return Game.Manager.mergeLevelMan.level >= level;
        }

        long IOrderHelper.GetTotalFinished()
        {
            return Game.Manager.mainOrderMan.totalFinished;
        }

        int IOrderHelper.GetBoardLevel()
        {
            return Game.Manager.mergeLevelMan.level;
        }

        bool IOrderHelper.IsOrderCompleted(int id)
        {
            return Game.Manager.mainOrderMan.IsOrderCompleted(id);
        }
    }
}