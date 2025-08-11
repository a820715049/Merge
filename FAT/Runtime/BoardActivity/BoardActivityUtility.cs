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

        #region 许愿棋盘接口

        //供外部获取指定范围内的棋盘行配置 startRow从0开始
        public static bool FillWishBoardRowConfStr(int detailId, IList<string> container, int startRow, int needRowCount)
        {
            var configMan = Game.Manager.configMan;
            var detailConf = configMan.GetEventWishBoardDetailConfig(detailId);
            if (detailConf == null || container == null)
                return false;
            var totalCount = detailConf.BoardRowId.Count;
            if (startRow < 0 || totalCount <= 0 || needRowCount <= 0)
                return false;
            container.Clear();
            //需要的行配置范围不超过总行数时 直接按index取配置；大于总行数时 不做处理
            if (startRow + needRowCount <= totalCount)
            {
                for (int i = startRow; i < startRow + needRowCount; i++)
                {
                    var rowConf = configMan.GetEventWishRowConfig(detailConf.BoardRowId[i]);
                    if (rowConf != null)
                        container.Add(rowConf.DownMiniRow);
                }
                DebugEx.FormatInfo("FillBoardRowConfStr 1, startIndex = {0}, needCount = {1}", startRow + 1, needRowCount);
            }

#if UNITY_EDITOR
            foreach (var info in container)
            {
                DebugEx.FormatInfo("FillBoardRowConfStr 3, info = {0}", info);
            }
#endif
            return true;
        }

        #endregion
        #region 农场棋盘接口

        //供外部获取指定范围内的棋盘行配置 startRow从0开始
        public static bool FillFarmBoardRowConfStr(int detailId, IList<string> container, int startRow, int needRowCount)
        {
            var configMan = Game.Manager.configMan;
            var detailConf = configMan.GetEventFarmBoardDetailConfig(detailId);
            if (detailConf == null || container == null)
                return false;
            var totalCount = detailConf.BoardRowId.Count;
            if (startRow < 0 || totalCount <= 0 || needRowCount <= 0)
                return false;
            container.Clear();
            //需要的行配置范围不超过总行数时 直接按index取配置；大于总行数时 不做处理
            if (startRow + needRowCount <= totalCount)
            {
                for (int i = startRow; i < startRow + needRowCount; i++)
                {
                    var rowConf = configMan.GetEventFarmRowConfig(detailConf.BoardRowId[i]);
                    if (rowConf != null)
                        container.Add(rowConf.UpMiniRow);
                }
                DebugEx.FormatInfo("FillBoardRowConfStr 1, startIndex = {0}, needCount = {1}", startRow + 1, needRowCount);
            }

#if UNITY_EDITOR
            foreach (var info in container)
            {
                DebugEx.FormatInfo("FillBoardRowConfStr 3, info = {0}", info);
            }
#endif
            return true;
        }

        #endregion
    }
}