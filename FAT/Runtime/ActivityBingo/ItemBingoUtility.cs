using System.Collections.Generic;
using static FAT.RecordStateHelper;
using System.Linq;
using Cysharp.Threading.Tasks;
using fat.gamekitdata;
using fat.rawdata;
using Config;
using FAT.Merge;

namespace FAT
{
    /// <summary>
    /// bingo活动静态方法类，不存储任何数据，只提供数据流转方法
    /// </summary>
    public static class ItemBingoUtility
    {
        #region 配置获取方法
        public static EventItemBingoRound GetEventItemBingoRound(int id)
        {
            return id != 0 ? Game.Manager.configMan.GetEventItemBingoRoundConfig(id) : null;
        }

        public static EventItemBingo GetEventItemBingoConfig(int id)
        {
            return id != 0 ? Game.Manager.configMan.GetEventItemBingoConfig(id) : null;
        }

        public static GroupDetail GetGroupDetail(int id)
        {
            return id != 0 ? Game.Manager.configMan.GetGroupDetailConfig(id) : null;
        }

        public static ItemBingoBoard GetBoardConfig(int id)
        {
            return id != 0 ? Game.Manager.configMan.GetItemBingoBoardConfig(id) : null;
        }

        /// <summary>
        /// 获取棋盘奖励信息
        /// </summary>
        /// <param name="confBoardID">关卡ID</param>
        /// <returns>直线, 对角, 最终大奖</returns>
        public static (RewardConfig straight, RewardConfig slash, RewardConfig all) GetBoardRewardInfo(int confBoardID)
        {
            var cfg = GetBoardConfig(confBoardID);
            var straight = cfg.RewardStraight[0].ConvertToRewardConfig();
            var slash = cfg.RewardSlash[0].ConvertToRewardConfig();
            var all = cfg.RewardAll[0].ConvertToRewardConfig();
            return (straight, slash, all);
        }

        /// <summary>
        /// 根据categoryId获取当前拥有的最高等级item
        /// </summary>
        /// <param name="categoryIds">categoryId列表</param>
        /// <param name="results">结果列表 没有则填0补齐</param>
        public static void FillHighestLeveItemByCategory(IList<int> categoryIds, List<int> results)
        {
            results.Clear();
            foreach (var cid in categoryIds)
            {
                results.Add(GetHighestLevelItemIdInCategory(cid));
            }
        }

        /// <summary>
        /// 根据categoryId获取当前拥有的最高等级item
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns>生成器ID</returns>
        public static int GetHighestLevelItemIdInCategory(int categoryId)
        {
            var cat = Game.Manager.mergeItemMan.GetCategoryConfig(categoryId);
            var itemId = 0;
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
        #endregion
        #region bingo数据处理

        /// <summary>
        /// 加载bingo item数据
        /// </summary>
        /// <param name="bingoItems">要加载的itemList</param>
        /// <param name="data_">数据</param>
        /// <param name="index">数据索引</param>
        public static void LoadBingoItemData(List<BingoItem> bingoItems, ActivityInstance data_, int index)
        {
            var any = data_.AnyState;
            bingoItems.ForEach(item => item.IsClaimed = ReadBool(index++, any));
            RefreshIsBingo(bingoItems);
        }
        /// <summary>
        /// 根据bingo item列表刷新bingo状态
        /// </summary>
        /// <param name="bingoItems"></param>
        public static void RefreshIsBingo(List<BingoItem> bingoItems)
        {
            foreach (var group in bingoItems.GroupBy(item => item.CoordX).Where(g => g.All(item => item.IsClaimed)))
            {
                foreach (var item in group)
                {
                    item.HasBingo = true;
                }
            }
            foreach (var group in bingoItems.GroupBy(item => item.CoordY).Where(g => g.All(item => item.IsClaimed)))
            {
                foreach (var item in group)
                {
                    item.HasBingo = true;
                }
            }
            if (bingoItems.Where(item => item.IsDiagonal1).All(item => item.IsClaimed))
            {
                foreach (var item in bingoItems.Where(item => item.IsDiagonal1))
                {
                    item.HasBingo = true;
                }
            }
            if (bingoItems.Where(item => item.IsDiagonal2).All(item => item.IsClaimed))
            {
                foreach (var item in bingoItems.Where(item => item.IsDiagonal2))
                {
                    item.HasBingo = true;
                }
            }
        }

        /// <summary>
        /// 根据配置创建bingo item列表
        /// </summary>
        /// <param name="activityBingo"></param>
        public static List<BingoItem> CreateBingoItemList(int groupId, int phase)
        {
            var GroupConf = GetGroupDetail(groupId);
            var BoardConf = GetBoardConfig(GroupConf.IncludeBoard.FirstOrDefault(id => GroupConf.IncludeBoard.IndexOf(id) == phase));
            var size = BoardConf == null ? 0 : BoardConf.BoardColNum;
            return BoardConf == null ? new List<BingoItem>() : BoardConf.ItemInfo.Select(info => new BingoItem(info, size)).ToList();
        }

        /// <summary>
        /// 从仓库中取出生成器
        /// </summary>
        /// <param name="id">生成器链条id</param>
        /// <returns>true为成功</returns>
        public static bool TryTakeOutItem(int id)
        {
            if (Game.Manager.mainMergeMan.world.activeBoard.emptyGridCount < 1)
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BoardFullUi);
                Game.Manager.audioMan.TriggerSound("BoardFull");
                return false;
            }

            var itemId = GetHighestLevelItemIdInCategory(id);
            if (itemId == 0) return false;
            Game.Manager.mainMergeMan.world.inventory.GetItemIndexByTid(itemId, out var index, out var bagId);
            var inReward = HasItemInMainBoardRewardTrack(itemId);
            if (index == -1 && !inReward) return false;
            if (index != -1)
            {

                UIFlyFactory.GetFlyTarget(FlyType.Inventory, out var worldPos);
                BoardUtility.RegisterSpawnRequest(itemId, worldPos);
                if (!Game.Manager.mainMergeMan.world.activeBoard.GetItemFromInventory(index, bagId))
                {
                    return false;
                }
                else
                {
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.ItemBingoSpawnerSent);
                }
            }
            else
            {
                index = Game.Manager.mainMergeMan.world.FindRewardIndex(itemId);
                Game.Manager.mainMergeMan.world.activeBoard.SpawnRewardItemByIdx(index);
                Game.Manager.commonTipsMan.ShowPopTips(Toast.ItemBingoSpawnerSent);
            }
            return true;
        }

        #endregion
        #region bingo数据获取

        /// <summary>
        /// 获取所有符合条件的可选的bingo组
        /// </summary>
        /// <returns>key值为bingogroup的ID，value为生成器链条List</returns>
        public static Dictionary<int, List<int>> GetBingoGroup(int BingoID)
        {
            var ConfBingo = GetEventItemBingoConfig(BingoID);
            return ConfBingo == null
                ? new Dictionary<int, List<int>>()
                : Game.Manager.configMan
                    .GetEventItemBingoDetailConfig(Game.Manager.userGradeMan.GetTargetConfigDataId(ConfBingo.GradeId))
                    ?.IncludeLevelGroups
                    .Select(id => Game.Manager.configMan.GetLevelGroupConfig(id))
                    .FirstOrDefault(group => group.MaxLevel >= Game.Manager.mergeLevelMan.level && group.MinLevel <= Game.Manager.mergeLevelMan.level)?.OptionalBingoGroup.Select(id => Game.Manager.configMan.GetGroupDetailConfig(id)).ToDictionary(group => group.Id, group => group.IncludeSpawner.ToList());
        }

        /// <summary>
        /// 预览完成bingo的奖励
        /// </summary>
        /// <param name="CoordX">bingo项坐标X</param>
        /// <param name="CoordY">bingo项坐标Y</param>
        /// <param name="bingoItems">bingoList</param>
        /// <returns></returns>
        public static List<(int, int)> PreviewCompleteBingo(int CoordX, int CoordY, List<BingoItem> bingoItems, int boardID)
        {
            var ret = new List<(int, int)>();
            var item = bingoItems.FirstOrDefault(i => i.CoordX == CoordX && i.CoordY == CoordY);
            if (item == null) return ret;
            ret.Add((item.RewardId, item.RewardCount));
            ret.AddRange(PrevieStraightReward(bingoItems, CoordX, CoordY, boardID));
            ret.AddRange(PrevieSlashReward(bingoItems, CoordX, CoordY, boardID));
            ret.AddRange(PrevieAllReward(bingoItems, CoordX, CoordY, boardID));
            return ret;
        }

        /// <summary>
        /// 预览完成bingo时获得的行列奖励
        /// </summary>
        /// <param name="bingoItems"></param>
        /// <param name="CoordX"></param>
        /// <param name="CoordY"></param>
        /// <param name="confBoardID"></param>
        /// <returns></returns>
        public static List<(int, int)> PrevieStraightReward(List<BingoItem> bingoItems, int CoordX, int CoordY, int confBoardID)
        {
            var ret = new List<(int, int)>();
            var board = GetBoardConfig(confBoardID);
            var rewardStrs = board.RewardStraight;
            if (bingoItems.Where(item => item.CoordX == CoordX && item.CoordY != CoordY).All(item => item.IsClaimed))
                ret.AddRange(ParseRewardStrs(rewardStrs.ToList()));
            if (bingoItems.Where(item => item.CoordX != CoordX && item.CoordY == CoordY).All(item => item.IsClaimed))
                ret.AddRange(ParseRewardStrs(rewardStrs.ToList()));
            return ret;
        }
        /// <summary>
        /// 预览完成bingo时获得的斜线奖励
        /// </summary>
        /// <param name="bingoItems"></param>
        /// <param name="CoordX"></param>
        /// <param name="CoordY"></param>
        /// <param name="confBoardID"></param>
        /// <returns></returns>
        public static List<(int, int)> PrevieSlashReward(List<BingoItem> bingoItems, int CoordX, int CoordY, int confBoardID)
        {
            var ret = new List<(int, int)>();
            var board = GetBoardConfig(confBoardID);
            var size = board.BoardColNum;
            var rewardStrs = board.RewardSlash;
            if (CoordX == CoordY)
                if (bingoItems.Where(item => item.IsDiagonal1 && item.CoordX != CoordX).All(item => item.IsClaimed))
                    ret.AddRange(ParseRewardStrs(rewardStrs.ToList()));
            if (CoordX + CoordY == size + 1)
                if (bingoItems.Where(item => item.IsDiagonal2 && item.CoordX != CoordX).All(item => item.IsClaimed))
                    ret.AddRange(ParseRewardStrs(rewardStrs.ToList()));
            return ret;
        }
        /// <summary>
        /// 预览完成全部bingo时获得的全奖励
        /// </summary>
        /// <param name="bingoItems"></param>
        /// <param name="confBoardID"></param>
        /// <returns>当List中没有奖励时表示没有完成全部bingo</returns>
        public static List<(int, int)> PrevieAllReward(List<BingoItem> bingoItems, int CoordX, int CoordY, int confBoardID)
        {
            var ret = new List<(int, int)>();
            var board = GetBoardConfig(confBoardID);
            var rewardStrs = board.RewardAll;
            if (bingoItems.All(item => item.IsClaimed || (item.CoordX == CoordX && item.CoordY == CoordY)))
                ret.AddRange(ParseRewardStrs(rewardStrs.ToList()));
            return ret;
        }

        /// <summary>
        ///解析奖励字符串
        /// </summary>
        /// <param name="rewardStrs"></param>
        /// <returns></returns>
        private static IEnumerable<(int, int)> ParseRewardStrs(List<string> rewardStrs)
        {
            return rewardStrs.Select(item =>
            {
                var parts = item.Split(':');
                int.TryParse(parts[0], out var id);
                int.TryParse(parts[1], out var count);
                return (id, count);
            });
        }

        /// <summary>
        /// 发放奖励逻辑
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public static List<RewardCommitData> GetRewardCommitDatas(List<(int, int)> ret)
        {
            var list = new List<RewardCommitData>();
            foreach (var variable in ret)
                list.Add(Game.Manager.rewardMan.BeginReward(variable.Item1, variable.Item2, ReasonString.bingo_reward));
            return list;
        }

        public static ItemBingoState GetBingoState(List<BingoItem> bingoItems, BingoItem bingo)
        {
            var state = ItemBingoState.None;
            state.SetFlag(ItemBingoState.ItemCompleted);
            if (bingoItems.Where(item => item.CoordX == bingo.CoordX).All(item => item.IsClaimed))
                state.SetFlag(ItemBingoState.ColumnCompleted);
            if (bingoItems.Where(item => item.CoordY == bingo.CoordY).All(item => item.IsClaimed))
                state.SetFlag(ItemBingoState.RowCompleted);
            if (bingo.IsDiagonal1 && bingoItems.Where(item => item.IsDiagonal1).All(item => item.IsClaimed))
                state.SetFlag(ItemBingoState.MainDiagonalCompleted);
            if (bingo.IsDiagonal2 && bingoItems.Where(item => item.IsDiagonal2).All(item => item.IsClaimed))
                state.SetFlag(ItemBingoState.AntiDiagonalCompleted);
            if (bingoItems.All(item => item.IsClaimed))
                state.SetFlag(ItemBingoState.FullHouse);
            return state;
        }

        public static void UpdateBingoItemState(List<BingoItem> bingoItems, ItemBingoState state, BingoItem bingo)
        {
            bingo.IsClaimed = state.HasFlag(ItemBingoState.ItemCompleted);
            if (state.HasFlag(ItemBingoState.ColumnCompleted))
                bingoItems.Where(item => item.CoordX == bingo.CoordX).ToList().ForEach(item => item.HasBingo = true);
            if (state.HasFlag(ItemBingoState.RowCompleted))
                bingoItems.Where(item => item.CoordY == bingo.CoordY).ToList().ForEach(item => item.HasBingo = true);
            if (state.HasFlag(ItemBingoState.MainDiagonalCompleted))
                bingoItems.Where(item => item.IsDiagonal1).ToList().ForEach(item => item.HasBingo = true);
            if (state.HasFlag(ItemBingoState.AntiDiagonalCompleted))
                bingoItems.Where(item => item.IsDiagonal2).ToList().ForEach(item => item.HasBingo = true);
            if (state.HasFlag(ItemBingoState.FullHouse))
                bingo.HasBingo = true;
        }

        /// <summary>
        /// 判断主棋盘存在item
        /// </summary>
        public static bool HasActiveItemInMainBoard(int itemId)
        {
            return Game.Manager.mainMergeMan.worldTracer.GetCurrentActiveBoardItemCount().ContainsKey(itemId);
        }

        /// <summary>
        /// 判断主棋盘和背包存在item
        /// </summary>
        public static bool HasActiveItemInMainBoardAndInventory(int itemId)
        {
            return Game.Manager.mainMergeMan.worldTracer.GetCurrentActiveBoardAndInventoryItemCount().ContainsKey(itemId);
        }

        /// <summary>
        /// 判断主棋盘礼物队列存在item
        /// </summary>
        public static bool HasItemInMainBoardRewardTrack(int itemId)
        {
            var index = Game.Manager.mainMergeMan.world.FindRewardIndex(itemId);
            return index >= 0;
        }

        public static void DebugBingoItem()
        {
            var act = Game.Manager.activity.LookupAny(EventType.ItemBingo);
            if (act == null) return;
            var activity = (ActivityBingo)act;
            foreach (var item in activity.BingoItems)
            {
                if (item.IsClaimed) continue;

                Game.Manager.mainMergeMan.world.activeBoard.SpawnItemMustWithReason(item.ItemId,
                    ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Cheat), 0, 0, false,
                    false);
            }
        }
        #endregion
    }

    /// <summary>
    /// bingo项类，存储bingo项数据
    /// </summary>
    public class BingoItem
    {
        public int ItemId;
        public int RewardId;
        public int RewardCount;
        /// <summary>
        /// 是否已经提交过，用于UI显示
        /// </summary>
        public bool IsClaimed;
        /// <summary>
        /// 是否已经bingo过，用于UI显示
        /// </summary>
        public bool HasBingo;
        public int CoordX;
        public int CoordY;
        /// <summary>
        /// 是否是左上角到右下角的对角线
        /// </summary>
        public bool IsDiagonal1;
        /// <summary>
        /// 是否是左下角到右上角的对角线
        /// </summary>
        public bool IsDiagonal2;

        public BingoItem(string info, int size)
        {
            var infos = info.Split(':');
            CoordX = int.Parse(infos[0]);
            CoordY = int.Parse(infos[1]);
            ItemId = int.Parse(infos[2]);
            RewardId = int.Parse(infos[3]);
            RewardCount = int.Parse(infos[4]);
            IsDiagonal1 = CoordX == CoordY;
            IsDiagonal2 = CoordX + CoordY == size + 1;
            IsClaimed = false;
            HasBingo = false;
        }
    }
}