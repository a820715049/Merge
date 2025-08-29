/*
 *@Author:chaoran.zhang
 *@Desc:bingo活动类
 *@Created Time:2025.02.24 星期一 14:37:26
 */

using System;
using fat.gamekitdata;
using System.Collections.Generic;
using static FAT.RecordStateHelper;
using static FAT.ListActivity;
using FAT.Merge;
using System.Linq;
using EL;
using fat.rawdata;
using UnityEngine;
using Cysharp.Text;

namespace FAT
{
    using static ListActivity;
    /// <summary>
    /// bingo活动实例类
    /// 约定：phase字段用来记录“当前轮数”,从零开始，打点时默认从1开始，需要手动+1。
    /// </summary>
    public class ActivityBingo : ActivityLike, IBoardEntry, IMergeItemIndicatorHandler, IActivityOrderHandler
    {
        public class EntryWrapper : IEntrySetup
        {
            public Entry Entry => e;
            private readonly Entry e;
            private readonly ActivityBingo bingo;

            public EntryWrapper(Entry e_, ActivityBingo bingo_)
            {
                (e, bingo) = (e_, bingo_);
                e_.flag.SetImage(bingo_.BadgeAsset);
                RefreshFlag();
                MessageCenter.Get<MSG.BINGO_ITEM_COMPLETE_DIRTY>().AddListener(RefreshFlag);
                MessageCenter.Get<MSG.BINGO_PROGRESS_UPDATE>().AddListener(RefreshFlag);
            }

            public override void Clear(Entry e_)
            {
                MessageCenter.Get<MSG.BINGO_ITEM_COMPLETE_DIRTY>().RemoveListener(RefreshFlag);
                MessageCenter.Get<MSG.BINGO_PROGRESS_UPDATE>().RemoveListener(RefreshFlag);
            }

            public void RefreshFlag()
            {
                e.flag.gameObject.SetActive(bingo.CheckBingoComplete());
            }
        }

        #region 存档字段
        public int BingoGroupID; //bingo活动组id
        public int BingoGroupPhase; //bingo活动组当前轮数
        public int BingoGroupPhaseTotal; //bingo活动组总轮数,打点用
        public int BingoTotal; //当前关卡bingo活动次数,打点用
        public int BingoBoardTotal; //累计完成了几个关卡
        public bool IsMain; //新手引导使用一次
        #endregion
        #region 活动数据字段
        /// <summary>
        /// bingo活动的所有bingo项
        /// </summary>
        public readonly List<BingoItem> BingoItems = new();
        public bool HasPop;
        public int BoardRowNum;
        public int BoardColNum;
        public Dictionary<int, int> BingoItemMap = new();
        #endregion
        #region 配置数据

        /// <summary>
        /// 包含活动换皮信息
        /// </summary>
        public int ConfBingoID;

        /// <summary>
        /// 包含共有多少关卡和生成器链条预览信息
        /// </summary>
        public int ConfBoardID;


        #endregion

        #region UI资源

        // public ActivityVisual StartVisual = new();
        // public PopupActivity StartPopup = new();
        // public UIResAlt StartRes = new(UIConfig.UIBingoStart);

        // public ActivityVisual RestartVisual = new();
        // public PopupActivity RestartPopup = new();
        // public UIResAlt RestartRes = new(UIConfig.UIBingoRestart);

        public ActivityVisual MainVisual = new();
        public PopupActivity MainPopup = new();
        public UIResAlt MainRes = new(UIConfig.UIBingoMain);

        public ActivityVisual BingoVisual = new();
        public UIResAlt BingoRes = new(UIConfig.UIBingoHelp);

        public ActivityVisual ItemVisual = new();
        public UIResAlt ItemRes = new(UIConfig.UIBingoItem);

        public ActivityVisual EndVisual = new();
        public UIResAlt EndRes = new(UIConfig.UIBingoEnd);
        public PopupActivity EndPopup = new();

        public override ActivityVisual Visual => MainVisual;

        // 角标
        public string BadgeAsset => MainVisual.Theme.AssetInfo.TryGetValue("badge", out var res) ? res : null;

        #endregion

        #region 通用接口

        public string BoardEntryAsset()
        {
            MainVisual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        public void CollectDetectorExcludeItemMap(List<IDictionary<int, int>> container)
        {
            container.Add(BingoItemMap);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            BingoGroupID = ReadInt(i++, any);
            BingoGroupPhase = ReadInt(i++, any);
            BingoGroupPhaseTotal = ReadInt(i++, any);
            BingoTotal = ReadInt(i++, any);
            if (BingoGroupID > 0) BingoItems.AddRange(ItemBingoUtility.CreateBingoItemList(BingoGroupID, BingoGroupPhase));
            if (BingoItems.Count > 0) ItemBingoUtility.LoadBingoItemData(BingoItems, data_, i);
            RefreshBingoItemMap();
            InitConf();
            RefreshTheme();
        }


        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            any.Add(ToRecord(i++, BingoGroupID));
            any.Add(ToRecord(i++, BingoGroupPhase));
            any.Add(ToRecord(i++, BingoGroupPhaseTotal));
            any.Add(ToRecord(i++, BingoTotal));
            BingoItems.ForEach(item => any.Add(ToRecord(i++, item.IsClaimed)));
        }

        public override void Open()
        {
            MainRes.ActiveR.Open(this, false);
        }

        public override void SetupFresh()
        {
            InitConf();
            RefreshTheme();
            var dic = GetOptionalBingoGroup();
            if (dic.Count == 1)
            {
                ChooseGroup(dic.First().Key);
            }
            Game.Manager.screenPopup.TryQueue(MainPopup, PopupType.Login, true);
            HasPop = true;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (!HasPop)
            {
                popup_.TryQueue(MainPopup, state_, true);
                HasPop = true;
            }
        }


        public ActivityBingo(ActivityLite lite)
        {
            Lite = lite;
        }

        public override void WhenEnd()
        {
            DataTracker.event_bingo_end.Track(this, phase, BingoBoardTotal);
            Game.Manager.screenPopup.TryQueue(EndPopup, PopupType.Login);
        }

        public event Action Invalidate;

        public ItemIndType CheckIndicator(int itemId, out string asset)
        {
            asset = BadgeAsset;
            return BingoItemMap.ContainsKey(itemId) ? ItemIndType.Bingo : ItemIndType.None;
        }

        #endregion

        #region UI显示接口

        /// <summary>
        /// 获取当前已经完成的bingo item个数
        /// </summary>
        /// <returns></returns>
        public int GetBingoCount()
        {
            return BingoItems.Count(item => item.IsClaimed);
        }

        /// <summary>
        /// 获取当前bingo item的总个数
        /// </summary>
        /// <returns></returns>
        public int GetBingoTotalNum()
        {
            return BingoItems.Count;
        }

        /// <summary>
        /// 获取当前bingo活动处于第几关，从1开始，用于主界面的UI展示
        /// </summary>
        /// <returns></returns>
        public int GetBingoBoardIndex()
        {
            return BingoGroupPhase + 1;
        }
        #endregion

        #region UI交互接口
        /// <summary>
        /// 检测当前是否已经选择了关卡组，用于主界面打开时的UI展示
        /// </summary>
        /// <returns>true表示已经选择，进入到关卡界面；false表示没有选择，则进入到关卡组选择界面</returns>
        public bool CheckGroupStart()
        {
            return BingoGroupID != 0;
        }

        /// <summary>
        /// 获取当前所有可以选择的关卡组的信息
        /// </summary>
        /// <returns>key值为bingogroup的ID，value为生成器链条List</returns>
        public Dictionary<int, List<int>> GetOptionalBingoGroup()
        {
            var confRound = ItemBingoUtility.GetEventItemBingoRound(Param);
            return ItemBingoUtility.GetBingoGroup(confRound?.IncludeItemBingoId.FirstOrDefault() ?? 0);
        }

        /// <summary>
        /// 确认关卡组
        /// </summary>
        /// <param name="groupId">选择的关卡组ID</param>
        public void ChooseGroup(int groupId)
        {
            var group = ItemBingoUtility.GetGroupDetail(groupId);
            if (group == null) return;
            BingoGroupID = groupId;
            BingoGroupPhase = 0;
            ConfBoardID = group.IncludeBoard[BingoGroupPhase];
            BingoItems.Clear();
            BingoItems.AddRange(ItemBingoUtility.CreateBingoItemList(groupId, BingoGroupPhase));
            BingoItemMap.Clear();
            RefreshBingoItemMap();
            MessageCenter.Get<MSG.BINGO_PROGRESS_UPDATE>().Dispatch();
            MessageCenter.Get<MSG.BINGO_ITEM_COMPLETE_DIRTY>().Dispatch();
            Invalidate?.Invoke();
            DataTracker.event_bingo_restart.Track(this, phase + 1);
            BoardColNum = ItemBingoUtility.GetBoardConfig(ConfBoardID)?.BoardColNum ?? 0;
            BoardRowNum = ItemBingoUtility.GetBoardConfig(ConfBoardID)?.BoardRowNum ?? 0;
        }

        /// <summary>
        /// 获取当前bingo item列表
        /// </summary>
        /// <returns></returns>
        public List<BingoItem> GetBingoItemList()
        {
            return BingoItems;
        }

        public void RefreshBingoItemMap()
        {
            foreach (var item in BingoItems)
            {
                if (item.IsClaimed) continue;
                if (BingoItemMap.ContainsKey(item.ItemId))
                {
                    BingoItemMap[item.ItemId]++;
                }
                else
                {
                    BingoItemMap.Add(item.ItemId, 1);
                }
            }
        }

        public void RefreshBingoItemMap(BingoItem item)
        {
            if (!item.IsClaimed || !BingoItemMap.ContainsKey(item.ItemId)) return;
            BingoItemMap[item.ItemId]--;
            if (BingoItemMap[item.ItemId] <= 0) BingoItemMap.Remove(item.ItemId);
            MessageCenter.Get<MSG.BINGO_ITEM_MAP_UPDATE>().Dispatch();
        }

        /// <summary>
        /// 预览完成bingo的奖励
        /// </summary>
        public void PreviewCompleteBingo(BingoItem item, out List<(int, int)> reward)
        {
            reward = ItemBingoUtility.PreviewCompleteBingo(item.CoordX, item.CoordY, BingoItems, ConfBoardID);
        }
        /// <summary>
        /// 提交bingo棋子时调用
        /// <param name="item"></param>
        /// <param name="rewardCommitDatas">奖励List</param>
        /// <param name="enterNextBoard">是否进入下一关</param>
        /// <param name="enterNextRound">是否进入下一轮</param>
        /// <returns>bingo完成的状态</returns>
        public ItemBingoState CompleteBingo(BingoItem item, out List<RewardCommitData> rewardCommitDatas, out bool enterNextBoard, out bool enterNextRound)
        {
            rewardCommitDatas = null;
            enterNextBoard = false;
            enterNextRound = false;
            //已经提交过的奖励不能重复提交
            if (item.IsClaimed) return ItemBingoState.None;
            using (ObjectPool<List<ItemConsumeRequest>>.GlobalPool.AllocStub(out var toConsume))
            {
                toConsume.Add(new ItemConsumeRequest()
                {
                    itemId = item.ItemId,
                    itemCount = 1
                });
                var consumeSuccess = Game.Manager.mainMergeMan.world.TryConsumeOrderItem(toConsume, null, false);
                if (!consumeSuccess) return ItemBingoState.None;
                item.IsClaimed = true;
                var completedLines = ItemBingoUtility.PreviewCompleteBingo(item.CoordX, item.CoordY, BingoItems, ConfBoardID);
                rewardCommitDatas = ItemBingoUtility.GetRewardCommitDatas(completedLines);
                var state = ItemBingoUtility.GetBingoState(BingoItems, item);
                ItemBingoUtility.UpdateBingoItemState(BingoItems, state, item);
                var rewardstring = string.Join(",", rewardCommitDatas.Select(r => ZString.Format("{0}:{1}", r.rewardId, r.rewardCount)));
                DataTracker.event_bingo_submit.Track(this, ZString.Format("{0}:{1}:{2}", item.ItemId, item.CoordX, item.CoordY), rewardstring,
                    state.HasFlag(ItemBingoState.RowCompleted) || state.HasFlag(ItemBingoState.ColumnCompleted) || state.HasFlag(ItemBingoState.MainDiagonalCompleted) || state.HasFlag(ItemBingoState.AntiDiagonalCompleted),
                    state.HasFlag(ItemBingoState.FullHouse), BingoItems.Count(), BingoItems.Count(item => item.IsClaimed), BingoGroupPhaseTotal + 1, phase + 1);
                if (state.HasFlag(ItemBingoState.ColumnCompleted) || state.HasFlag(ItemBingoState.RowCompleted) || state.HasFlag(ItemBingoState.MainDiagonalCompleted) || state.HasFlag(ItemBingoState.AntiDiagonalCompleted))
                    DataTracker.event_bingo_complete.Track(this, state.HasFlag(ItemBingoState.RowCompleted) || state.HasFlag(ItemBingoState.ColumnCompleted),
                        state.HasFlag(ItemBingoState.MainDiagonalCompleted) || state.HasFlag(ItemBingoState.AntiDiagonalCompleted), state.HasFlag(ItemBingoState.FullHouse),
                        BingoGroupPhase + 1, ++BingoTotal, phase + 1, state.HasFlag(ItemBingoState.FullHouse));
                if (state.HasFlag(ItemBingoState.ItemCompleted)) RefreshBingoItemMap(item);
                if (state.HasFlag(ItemBingoState.ItemCompleted)) MessageCenter.Get<MSG.BINGO_PROGRESS_UPDATE>().Dispatch();
                enterNextBoard = state.HasFlag(ItemBingoState.FullHouse) && TryEnterNextBoard();
                if (!enterNextBoard) enterNextRound = state.HasFlag(ItemBingoState.FullHouse) && TryEnterNextRound();
                Invalidate?.Invoke();
                return state;
            }
        }

        /// <summary>
        /// 从仓库中取出生成器
        /// </summary>
        /// <param name="ID">生成器链条id</param>
        /// <returns>true为取出成功，需要修改UI显示状态</returns>
        public bool TryTakeOutItem(int ID)
        {
            return ItemBingoUtility.TryTakeOutItem(ID);
        }
        /// <summary>
        /// 检测当前是否有bingo item可以提交
        /// </summary>
        /// <returns></returns>
        public bool CheckBingoComplete()
        {
            var dic = Game.Manager.mainMergeMan.worldTracer.GetCurrentActiveBoardAndInventoryItemCount();
            var hasIntersection = dic.Keys.Any(k => BingoItemMap.ContainsKey(k));
            return hasIntersection;
        }

        public int FindFirstSubmitItem()
        {
            var index = 0;
            foreach (var item in BingoItems)
            {
                if (ItemBingoUtility.HasActiveItemInMainBoardAndInventory(item.ItemId) || ItemBingoUtility.HasItemInMainBoardRewardTrack(item.ItemId))
                    return index;
                index++;
            }
            return -1;
        }
        #endregion
        #region 内部逻辑

        private bool TryEnterNextBoard()
        {
            BingoBoardTotal++;
            var confGroup = ItemBingoUtility.GetGroupDetail(BingoGroupID);
            DataTracker.event_bingo_level_complete.Track(this, ConfBoardID, BingoGroupPhase + 1, phase + 1);
            if (confGroup.IncludeBoard.Count > BingoGroupPhase + 1)
            {
                BingoTotal = 0;
                BingoGroupPhase++;
                BingoItems.Clear();
                BingoItemMap.Clear();
                ConfBoardID = confGroup.IncludeBoard[BingoGroupPhase];
                Debug.Log($"Enter next board: {ConfBoardID}");
                BingoItems.AddRange(ItemBingoUtility.CreateBingoItemList(BingoGroupID, BingoGroupPhase));
                RefreshBingoItemMap();
                MessageCenter.Get<MSG.BINGO_ENTER_NEXT_ROUND>().Dispatch();

                BoardColNum = ItemBingoUtility.GetBoardConfig(ConfBoardID)?.BoardColNum ?? 0;
                BoardRowNum = ItemBingoUtility.GetBoardConfig(ConfBoardID)?.BoardRowNum ?? 0;
                return true;
            }
            return false;
        }

        private bool TryEnterNextRound()
        {
            var confRound = ItemBingoUtility.GetEventItemBingoRound(Param);
            if (phase + 1 < confRound.IncludeItemBingoId.Count)
            {
                BingoTotal = 0;
                BingoGroupPhaseTotal = 0;
                phase++;
                BingoGroupPhase = 0;
                ConfBingoID = confRound.IncludeItemBingoId[phase];
                Debug.Log($"Enter next round: {ConfBingoID}");
                BingoItems.Clear();
                var confGroup = ItemBingoUtility.GetGroupDetail(BingoGroupID);
                ConfBoardID = confGroup.IncludeBoard[BingoGroupPhase];
                Debug.Log($"Enter next board: {ConfBoardID}");
                BingoItemMap.Clear();
                BingoItems.AddRange(ItemBingoUtility.CreateBingoItemList(BingoGroupID, BingoGroupPhase));
                RefreshBingoItemMap();
                MessageCenter.Get<MSG.BINGO_ENTER_NEXT_ROUND>().Dispatch();
                DataTracker.event_bingo_restart.Track(this, phase + 1);
                BoardColNum = ItemBingoUtility.GetBoardConfig(ConfBoardID)?.BoardColNum ?? 0;
                BoardRowNum = ItemBingoUtility.GetBoardConfig(ConfBoardID)?.BoardRowNum ?? 0;
                return true;
            }
            phase++;
            Game.Manager.activity.EndImmediate(this, false);
            return false;
        }

        private void InitConf()
        {
            var confRound = ItemBingoUtility.GetEventItemBingoRound(Param);
            ConfBingoID = confRound.IncludeItemBingoId[phase];
            var confGroup = ItemBingoUtility.GetGroupDetail(BingoGroupID);
            var boardConf = ItemBingoUtility.GetBoardConfig(confGroup?.IncludeBoard[BingoGroupPhase] ?? 0);
            BoardColNum = boardConf?.BoardColNum ?? 0;
            BoardRowNum = boardConf?.BoardRowNum ?? 0;
            ConfBoardID = boardConf?.Id ?? 0;
        }

        private void RefreshTheme()
        {
            var BingoConf = ItemBingoUtility.GetEventItemBingoConfig(ConfBingoID);
            MainVisual.Setup(BingoConf.MainTheme, MainRes);
            BingoVisual.Setup(BingoConf.BingoTheme, BingoRes);
            ItemVisual.Setup(BingoConf.ItemTheme, ItemRes);
            MainPopup.Setup(this, MainVisual, MainRes);
            EndVisual.Setup(BingoConf.EndTheme, EndRes);
            EndPopup.Setup(this, EndVisual, EndRes, false, false);
        }
        #endregion
    }

}
