/*
 * @Author: tang.yan
 * @Description: 活动棋盘相关方法工具类 
 * @Date: 2025-03-17 11:03:54
 */

using System.Collections.Generic;
using UnityEngine;
using Cysharp.Text;
using FAT.Merge;
using EL;
using fat.rawdata;

namespace FAT
{
    public static class BoardActivityUtility
    {
        #region 棋盘奖励回收公共接口

        //活动结束时回收棋盘中可以领取但未领取的各种奖励 这个方法会直接beginReward 需要在界面中合适时机自行commit
        public static string CollectAllBoardReward(List<RewardCommitData> rewards, MergeWorld world)
        {
            if (rewards == null || world == null)
                return "";
            var itemIdMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //棋子id整合结果(用于打点)
            var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //奖励整合结果
            world.WalkAllItem((item) =>
            {
                //遍历整个棋盘 找出所有可以回收的棋子 并整合
                _TryCollectReward(item, itemIdMap, rewardMap);
            }, MergeWorld.WalkItemMask.NoInventory);    //默认棋盘没有背包
            //发奖 相当于帮玩家把棋盘上没有使用的棋子直接用了 所以from用use_item
            var rewardMan = Game.Manager.rewardMan;
            foreach (var reward in rewardMap)
            {
                rewards.Add(rewardMan.BeginReward(reward.Key, reward.Value, ReasonString.use_item));
            }
            //对外传出棋子回收的整合信息 便于打点
            var itemsInfo = ConvertDictToString(itemIdMap);
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(itemIdMap);
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
            return itemsInfo;
        }

        private static void _TryCollectReward(Item item, Dictionary<int, int> itemIdMap, Dictionary<int, int> rewardMap)
        {
            if (item.TryGetItemComponent<ItemBonusCompoent>(out var bonusComp) && bonusComp.funcType == FuncType.Reward)
            {
                //收集棋子id
                Collect(itemIdMap, item.tid, 1);
                //收集奖励信息
                Collect(rewardMap, bonusComp.bonusId, bonusComp.bonusCount);
            }
            else if (item.TryGetItemComponent<ItemTapBonusComponent>(out var tapBonusComp) && tapBonusComp.funcType == FuncType.Collect)
            {
                //收集棋子id
                Collect(itemIdMap, item.tid, 1);
                //收集奖励信息
                Collect(rewardMap, tapBonusComp.bonusId, tapBonusComp.bonusCount);
            }
        }

        public static void Collect(Dictionary<int, int> map, int id, int count, int maxCount = -1)
        {
            //maxCount为-1表示没有最大数量限制
            if (maxCount == -1)
            {
                if (map.ContainsKey(id)) map[id] += count;
                else map.Add(id, count);
            }
            else if (maxCount > 0)
            {
                if (map.TryGetValue(id, out var curCount))
                {
                    var checkCount = curCount + count;
                    map[id] = Mathf.Min(checkCount, maxCount);
                }
                else
                {
                    map.Add(id, Mathf.Min(count, maxCount));
                }
            }
        }

        public static string ConvertDictToString(Dictionary<int, int> dict)
        {
            var sb = ZString.CreateStringBuilder();
            foreach (var info in dict)
            {
                if (sb.Length > 0) sb.Append(",");  // 只有在不是第一个元素时才加逗号
                //id:数量:棋子等级  逗号隔开
                sb.Append(info.Key.ToString());
                sb.Append(":");
                sb.Append(info.Value.ToString());
                sb.Append(":");
                sb.Append(ItemUtility.GetItemLevel(info.Key).ToString());
            }
            return sb.ToString();
        }

        #endregion

        //通用接口 供外部获取指定范围内的棋盘行配置 startRow从0开始，
        //IBoardActivityRowConf为所有需要棋盘上升/下降的活动
        //needCycle 为true表示本棋盘为循环棋盘
        public static bool FillBoardRowConfStr(IBoardActivityRowConf IRowConf, int detailId, IList<string> container, int startRow, int needRowCount)
        {
            if (detailId <= 0 || container == null)
                return false;
            var cycleStartRowId = IRowConf.GetCycleStartRowId(detailId);
            var needCycle = cycleStartRowId > 0;
            var rowConfIdList = IRowConf.GetRowConfIdList(detailId);
            var totalCount = rowConfIdList.Count;
            if (startRow < 0 || totalCount <= 0 || needRowCount <= 0)
                return false;
            container.Clear();
            //需要的行配置范围不超过总行数时 直接按index取配置
            if (startRow + needRowCount <= totalCount)
            {
                for (int i = startRow; i < startRow + needRowCount; i++)
                {
                    var rowConfId = rowConfIdList[i];
                    var rowConfStr = IRowConf.GetRowConfStr(rowConfId);
                    container.Add(rowConfStr);
                }
                DebugEx.FormatInfo("FillBoardRowConfStr 1, startIndex = {0}, needCount = {1}", startRow + 1, needRowCount);
            }
            //大于总行数时 先取到没超过的部分，然后超过的部分考虑循环，从startCycleIndex开始继续取剩余的部分
            else if (needCycle)
            {
                //配置上可以开始循环的起始点
                var startCycleIndex = rowConfIdList.IndexOf(cycleStartRowId);
                //实际进行循环的起始点
                var realCycleIndex = startCycleIndex;
                //单位循环长度
                var cycleLength = totalCount - startCycleIndex;
                //剩余需要获取的行数
                var remainingRows = 0;
                // 先获取从startRow到末尾的部分
                if (startRow < totalCount)
                {
                    for (int i = startRow; i < totalCount; i++)
                    {
                        var rowConfId = rowConfIdList[i];
                        var rowConfStr = IRowConf.GetRowConfStr(rowConfId);
                        container.Add(rowConfStr);
                    }
                    remainingRows = needRowCount - (totalCount - startRow);
                }
                else
                {
                    remainingRows = needRowCount;
                    var tempIndex = cycleLength != 0 ? (startRow - startCycleIndex) % cycleLength : 0;
                    realCycleIndex += tempIndex;
                }
                // 从循环起始点开始获取剩余的行
                for (int i = 0; i < remainingRows; i++)
                {
                    var findIndex = 0;
                    var curIndex = realCycleIndex + i;
                    if (curIndex < totalCount)
                    {
                        findIndex = curIndex;
                    }
                    else
                    {
                        findIndex = startCycleIndex + (curIndex - startCycleIndex) % cycleLength;
                    }
                    var rowConfId = rowConfIdList[findIndex];
                    var rowConfStr = IRowConf.GetRowConfStr(rowConfId);
                    container.Add(rowConfStr);
                }
                DebugEx.FormatInfo("FillBoardRowConfStr 2, startIndex = {0}, needCount = {1}", realCycleIndex + 1, needRowCount);
            }

#if UNITY_EDITOR
            foreach (var info in container)
            {
                DebugEx.FormatInfo("FillBoardRowConfStr 3, info = {0}", info);
            }
#endif
            return true;
        }

        #region 棋盘查找逻辑

        /// <summary>
        /// 根据categoryId获取当前拥有的最高等级item
        /// </summary>
        /// <param name="categoryIds">categoryId列表</param>
        /// <param name="results">结果列表</param>
        /// <param name="defaultType">默认值类型  0:没有查到则填充0 1:没有查到则填充链条最高级</param>
        public static void FillHighestLeveItemByCategory(IList<int> categoryIds, List<int> results, int defaultType = 0)
        {
            results.Clear();
            foreach (var cid in categoryIds)
            {
                results.Add(GetHighestLevelItemIdInCategory(cid, defaultType));
            }
        }

        /// <summary>
        /// 根据categoryId获取当前拥有的最高等级item
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="defaultType">默认值类型  0:没有查到则填充0 1:没有查到则填充链条最高级</param>
        /// <returns>生成器ID</returns>
        public static int GetHighestLevelItemIdInCategory(int categoryId, int defaultType = 0)
        {
            var cat = Game.Manager.mergeItemMan.GetCategoryConfig(categoryId);
            if (cat == null || cat.Progress.Count <= 0) return 0;
            var itemId = defaultType switch
            {
                0 => 0,
                1 => cat.Progress[^1],
                _ => 0
            };
            for (var i = cat.Progress.Count - 1; i >= 0; i--)
            {
                var tid = cat.Progress[i];
                if (HasActiveItemInMainBoardAndInventory(tid) || HasItemInMainBoardRewardTrack(tid))
                {
                    itemId = tid;
                    break;
                }
            }
            return itemId;
        }

        /// <summary>
        /// 判断主棋盘存在item
        /// </summary>
        public static bool HasActiveItemInMainBoard(int itemId) => Game.Manager.mainMergeMan.worldTracer.GetCurrentActiveBoardItemCount().ContainsKey(itemId);


        /// <summary>
        /// 判断主棋盘和背包存在item
        /// </summary>
        public static bool HasActiveItemInMainBoardAndInventory(int itemId) => Game.Manager.mainMergeMan.worldTracer.GetCurrentActiveBoardAndInventoryItemCount().ContainsKey(itemId);


        /// <summary>
        /// 判断主棋盘礼物队列存在item
        /// </summary>
        public static bool HasItemInMainBoardRewardTrack(int itemId)
        {
            var index = Game.Manager.mainMergeMan.world.FindRewardIndex(itemId);
            return index >= 0;
        }

        #endregion
    }
}