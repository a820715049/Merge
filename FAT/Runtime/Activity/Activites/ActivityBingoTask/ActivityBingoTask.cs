using System.Collections.Generic;
using System.Linq;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using FAT.MSG;

namespace FAT
{
    public class ActivityBingoTask : ActivityLike, IBoardEntry
    {
        #region 存档字段
        private int _detailID;
        private int _score;
        private int _rewardIndex = -1;
        #endregion

        #region 运行时字段
        private readonly int _bingoStateStartIndex = 100;
        public EventBingoTask conf;
        public EventBingoTaskDetail detail;
        private readonly BingoTaskMap _taskMap = new(4);
        private readonly List<RewardCommitData> _waitCommit = new();
        public bool hasPop;
        public int rewardIndex => _rewardIndex;
        #endregion

        #region UI
        public override ActivityVisual Visual => MainPopUp.visual;

        public VisualRes VisualHelp { get; } = new(UIConfig.UIBingoTaskHelp); // 帮助界面

        // 弹脸
        public VisualPopup EndPopup { get; } = new(UIConfig.UIBingoTaskEnd); // 补领
        public VisualPopup MainPopUp { get; } = new(UIConfig.UIBingoTaskMain); // 主UI
        #endregion

        #region ActivityLike
        public ActivityBingoTask(ActivityLite lite_)
        {
            Lite = lite_;
            conf = fat.conf.Data.GetEventBingoTask(Lite.Param);
            _AddListener();
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            _detailID = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _score = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _rewardIndex = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _InitData();
            _LoadAllBingoData(data_);
        }

        private void _LoadAllBingoData(ActivityInstance data_)
        {
            var index = _bingoStateStartIndex;
            for (var i = 0; i <= _taskMap.bingoDic.Count; i++)
            {
                if (_taskMap.TryGetBingoTaskCell(i, out var cell)) { _LoadBingoTaskCell(data_, cell, ref index); }
            }
        }

        private void _LoadBingoTaskCell(ActivityInstance data_, BingoTaskCell cell, ref int index)
        {
            cell.state = (BingoState)RecordStateHelper.ReadInt(index++, data_.AnyState);
            cell.score = RecordStateHelper.ReadInt(index++, data_.AnyState);
            cell.target = RecordStateHelper.ReadInt(index++, data_.AnyState);
        }

        public override void Open()
        {
            UIManager.Instance.OpenWindow(MainPopUp.res.ActiveR, this);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _detailID));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _score));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _rewardIndex));
            _SaveAllBingoData(data_);
        }

        private void _SaveAllBingoData(ActivityInstance data_)
        {
            var index = _bingoStateStartIndex;
            for (var i = 0; i <= _taskMap.bingoDic.Count; i++)
            {
                if (_taskMap.TryGetBingoTaskCell(i, out var cell)) { _SaveBingoTaskCell(data_, cell, ref index); }
            }
        }

        private void _SaveBingoTaskCell(ActivityInstance data_, BingoTaskCell cell, ref int index)
        {
            data_.AnyState.Add(RecordStateHelper.ToRecord(index++, (int)cell.state));
            data_.AnyState.Add(RecordStateHelper.ToRecord(index++, cell.score));
            data_.AnyState.Add(RecordStateHelper.ToRecord(index++, cell.target));
        }

        public override void SetupFresh()
        {
            _InitData();
            _InitBingoTaskCell();
        }

        public override void WhenReset()
        {
            _RemoveListener();
        }

        public override void WhenEnd()
        {
            Game.Manager.screenPopup.TryQueue(EndPopup.popup, PopupType.Login, this);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (hasPop) { return; }
            popup_.TryQueue(MainPopUp.popup, state_);
            hasPop = true;
        }
        #endregion

        #region 接口

        /// <summary>
        /// 活动里程碑i进度
        /// </summary>
        public int score => _score;

        /// <summary>
        /// bingo数据
        /// </summary>
        public BingoTaskMap bingoTaskMap => _taskMap;

        /// <summary>
        /// 尝试完成某一个BingoTaskCell
        /// </summary>
        /// <param name="index">cell序号(0-15)</param>
        public BingoResult TryCompleteBingoCell(int index)
        {
            _taskMap.TryCompleteBingoCell(index);
            _RefreshScore(_taskMap.result, index);
            if (_taskMap.bingoDic.All(pair => pair.Value.state == BingoState.Bingo)) { Game.Manager.activity.EndImmediate(this, false); }
            DataTracker.event_bingotask_complete.Track(this, _taskMap.bingoDic.Count(pair => pair.Value.state == BingoState.Bingo || pair.Value.state == BingoState.Completed),
                index, _taskMap.bingoDic.Count, detail.Diff, _taskMap.result.HasFlag(BingoResult.RowBingo) || _taskMap.result.HasFlag(BingoResult.ColumnBingo) || _taskMap.result.HasFlag(BingoResult.AntiDiagonalBingo) || _taskMap.result.HasFlag(BingoResult.MainDiagonalBingo),
                _rewardIndex == detail.MilestoneReward.Count - 1, 1);
            return _taskMap.result;
        }

        /// <summary>
        ///  获取待提交的阶段奖励信息
        /// </summary>
        /// <returns></returns>
        public List<RewardCommitData> GetWaitCommit()
        {
            var list = new List<RewardCommitData>();
            list.AddRange(_waitCommit);
            _waitCommit.Clear();
            return list;
        }

        /// <summary>
        /// 是否需要显示红点
        /// </summary>
        /// <returns></returns>
        public bool WhetherShowRemind(out int redNum)
        {
            redNum = _taskMap.bingoDic.Values.Count(e => e.state == BingoState.ToBeCompleted);
            return redNum > 0;
        }

        /// <summary>
        /// 是否全部完成
        /// </summary>
        /// <returns></returns>
        public bool WhetherAllBingo() => _taskMap.bingoDic.Values.All(e => e.state == BingoState.Bingo);

        /// <summary>
        /// 填充同一列BingoCell的Index，内部逻辑不会清理list，需要清理的话在调用前手动清理
        /// </summary>
        /// <param name="index">要查找的BingoCell的Index/param>
        /// <param name="list">容器/param>
        public void FillSameColumnCellList(int index, List<int> list) => _taskMap.FillSameColumnCellList(index, list);

        /// <summary>
        /// 填充同一行BingoCell的Index，内部逻辑不会清理list，需要清理的话在调用前手动清理
        /// </summary>
        /// <param name="index">要查找的BingoCell的Index/param>
        /// <param name="list">容器/param>
        public void FillSameRowCellList(int index, List<int> list) => _taskMap.FillSameRowCellList(index, list);

        /// <summary>
        /// 填充主对角线ingoCell的Index，内部逻辑不会清理list，需要清理的话在调用前手动清理
        /// </summary>
        /// <param name="index">要查找的BingoCell的Index/param>
        /// <param name="list">容器/param>
        public void FillMainDiagonalCellList(int index, List<int> list) => _taskMap.FillMainDiagonalCellList(index, list);

        /// <summary>
        /// 填充副对角线BingoCell的Index，内部逻辑不会清理list，需要清理的话在调用前手动清理
        /// </summary>
        /// <param name="index">要查找的BingoCell的Index/param>
        /// <param name="list">容器/param>
        public void FillAntiDiagonalCellList(int index, List<int> list) => _taskMap.FillAntiDiagonalCellList(index, list);


        /// <summary>
        /// 填充BingoCell的相邻格子的index，内部逻辑不会清理list，需要清理的话在调用前手动清理
        /// </summary>
        /// <param name="index">要查找的BingoCell的Index/param>
        /// <param name="list">容器/param>
        public void FillAdjacentCellList(int index, List<int> list) => _taskMap.FillAdjacentCellList(index, list);

        #endregion

        #region 业务逻辑

        private void _AddListener()
        {
            MessageCenter.Get<GAME_COIN_USE>().AddListener(_WhenCoinUse);
            MessageCenter.Get<GAME_COIN_ADD>().AddListener(_WhenCoinChange);
            MessageCenter.Get<GAME_BOARD_ITEM_MERGE>().AddListener(_WhenMerge);
            MessageCenter.Get<ORDER_FINISH_DATA>().AddListener(_WhenFinishOrder);
            MessageCenter.Get<GAME_CARD_DRAW_FINISH>().AddListener(_WhenCardDraw);
            MessageCenter.Get<GAME_MERGE_PRE_BEGIN_REWARD>().AddListener(_WhenTokenGet);
            MessageCenter.Get<GAME_MINE_BOARD_TOKEN_CHANGE>().AddListener(_WhenTokenCost);
            MessageCenter.Get<ON_USE_SPEED_UP_ITEM_SUCCESS>().AddListener(_WhendUnleashBubble);
            MessageCenter.Get<GAME_CARD_ADD>().AddListener(_WhenCardAdd);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_SKILL>().AddListener(_WhenBoardSkill);
        }

        private void _RemoveListener()
        {
            MessageCenter.Get<GAME_COIN_USE>().RemoveListener(_WhenCoinUse);
            MessageCenter.Get<GAME_COIN_ADD>().RemoveListener(_WhenCoinChange);
            MessageCenter.Get<GAME_BOARD_ITEM_MERGE>().RemoveListener(_WhenMerge);
            MessageCenter.Get<ORDER_FINISH_DATA>().RemoveListener(_WhenFinishOrder);
            MessageCenter.Get<GAME_CARD_DRAW_FINISH>().RemoveListener(_WhenCardDraw);
            MessageCenter.Get<GAME_MERGE_PRE_BEGIN_REWARD>().RemoveListener(_WhenTokenGet);
            MessageCenter.Get<GAME_MINE_BOARD_TOKEN_CHANGE>().RemoveListener(_WhenTokenCost);
            MessageCenter.Get<ON_USE_SPEED_UP_ITEM_SUCCESS>().RemoveListener(_WhendUnleashBubble);
            MessageCenter.Get<GAME_CARD_ADD>().RemoveListener(_WhenCardAdd);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_SKILL>().RemoveListener(_WhenBoardSkill);
        }

        /// <summary>
        /// 初始化活动数据
        /// </summary>
        private void _InitData()
        {
            _InitDetail();
            _InitTheme();
            _CreateBingoTaskCell();
        }
        /// <summary>
        /// 初始化detail信息(_detailID、detail)
        /// </summary>
        private void _InitDetail()
        {
            if (_detailID == 0) { _detailID = Game.Manager.userGradeMan.GetTargetConfigDataId(conf.Detail); }
            detail = fat.conf.Data.GetEventBingoTaskDetail(_detailID);
        }

        /// <summary>
        /// 初始化EventTheme信息（弹窗等）
        /// </summary>
        private void _InitTheme()
        {
            MainPopUp.Setup(conf.EventTheme, this);
            EndPopup.Setup(conf.EndTheme, this, false, false);
            VisualHelp.Setup(conf.HelpTheme);
        }

        /// <summary>
        /// 根据配置创建BingoTaskCell类,不包含数据初始化
        /// </summary>
        private void _CreateBingoTaskCell()
        {
            foreach (var id in detail.TaskList)
            {
                var info = fat.conf.Data.GetEventBingoTaskInfo(id);
                _taskMap.bingoDic.TryAdd(info.Index, new BingoTaskCell(info));
            }
        }

        /// <summary>
        /// 根据配置初始化BingoTaskCell，只在SetupFresh调用一次
        /// </summary>
        private void _InitBingoTaskCell()
        {
            for (var i = 0; i <= _taskMap.bingoDic.Count; i++)
            {
                if (!_taskMap.TryGetBingoTaskCell(i, out var cell)) { continue; }
                cell.target = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(cell.taskInfo.RequireParam);
            }
        }

        /// <summary>
        /// 刷新活动里程碑
        /// </summary>
        private void _RefreshScore(BingoResult result, int index)
        {
            if (result.HasFlag(BingoResult.RowBingo)) { _score++; TrackBingo(index / _taskMap.cellCountEachRow + 7); }
            if (result.HasFlag(BingoResult.ColumnBingo)) { _score++; TrackBingo(index % _taskMap.cellCountEachRow + 3); }
            if (result.HasFlag(BingoResult.MainDiagonalBingo)) { _score++; TrackBingo(1); }
            if (result.HasFlag(BingoResult.AntiDiagonalBingo)) { _score++; TrackBingo(2); }
            _TryClaimMilestoneReward();
        }

        private void TrackBingo(int line)
        {
            DataTracker.event_bingotask_bingo.Track(this, _score, detail.Diff, line, _taskMap.bingoDic.All(e => e.Value.state == BingoState.Bingo), 1);
        }

        /// <summary>
        /// 尝试领取阶段奖励
        /// </summary>
        private void _TryClaimMilestoneReward()
        {
            for (var i = 0; i < detail.MilstoneScore.Count; i++)
            {
                if (_score < detail.MilstoneScore[i]) { return; }
                if (_rewardIndex >= i) { continue; }
                _ClaimMilestoneReward(i);
            }
        }

        /// <summary>
        /// 发奖，更新已领取奖励index
        /// </summary>
        /// <param name="index">要领取的奖励序号</param>
        private void _ClaimMilestoneReward(int index)
        {
            var reward = detail.MilestoneReward[index].ConvertToRewardConfig();
            _waitCommit.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.bingo_task_milestone));
            _rewardIndex = index;
            DataTracker.event_bingotask_reward.Track(this, index + 1, detail.MilestoneReward.Count, detail.Diff, index == detail.MilestoneReward.Count - 1, 1);
        }

        #endregion

        #region 事件

        private void _WhenCoinUse(CoinChange change_)
        {
            if (change_.type == CoinType.MergeCoin && change_.reason == ReasonString.undo_sell_item) { _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCoin, -change_.amount); }
        }

        private void _WhenCoinChange(CoinChange change_)
        {
            if (change_.type == CoinType.MergeCoin) { _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCoin, change_.amount); }
        }

        private void _WhenMerge(Item item)
        {
            if (item?.world?.activeBoard?.boardId != Constant.MainBoardId) { return; }
            _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskMerge, 1);
        }
        private void _WhenTokenGet(RewardCommitData reward)
        {
            if (reward.rewardType != ObjConfigType.ActivityToken) { return; }
            var tokenConf = Game.Manager.objectMan.GetTokenConfig(reward.rewardId);
            if (tokenConf.Feature == FeatureEntry.FeatureOrderLike) { _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskOrderLike, reward.rewardCount); }
            else { _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskTokenGet, reward.rewardCount); }
        }

        private void _WhenBoardSkill(Item item, SkillType skillType)
        {
            var board = item?.world?.activeBoard;
            if (board == null || board.boardId != Constant.MainBoardId) { return; }
            if (skillType != SkillType.Upgrade) { return; }
            _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskMerge, 1);
        }

        private void _WhenFinishOrder(IOrderData data) => _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskOrder, data.OrderType != (int)OrderType.MagicHour ? 1 : 0);
        private void _WhenCardDraw() => _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCardPack, 1);
        private void _WhendUnleashBubble() => _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskBubble, 1);
        private void _WhenTokenCost(int num, int id) => _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskTokenCost, num < 0 ? -num : 0);
        private void _WhenCardAdd() => _taskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCardNum, 1);

        #endregion

        public string BoardEntryAsset()
        {
            MainPopUp.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }
    }

    // meta 入口显示
    public class BingoTaskEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => entry;
        private readonly ListActivity.Entry entry;
        private readonly ActivityBingoTask activity;

        public BingoTaskEntry(ListActivity.Entry ent, ActivityBingoTask act)
        {
            (entry, activity) = (ent, act);
            var showRedPoint = activity.WhetherShowRemind(out var redNum);
            ent.dot.SetActive(showRedPoint);
            ent.dotCount.gameObject.SetActive(showRedPoint);
            ent.dotCount.SetText(redNum.ToString());
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            var showRedPoint = activity.WhetherShowRemind(out var redNum);
            entry.dot.SetActive(showRedPoint);
            if (showRedPoint)
            {
                entry.dotCount.SetText(redNum.ToString());
            }

            return UIUtility.CountDownFormat(diff_);
        }
    }

    #region BingiCell

    /// <summary>
    /// BingoTask活动使用的BingoCell数据单元
    /// </summary>
    public class BingoTaskCell : BingoCellBase
    {
        public EventBingoTaskInfo taskInfo;
        public BingoTaskCell(EventBingoTaskInfo info)
        {
            taskInfo = info;
            state = info.IsDefaultUnlock ? BingoState.UnFinished : BingoState.Special;
            targetType = info.TaskType;
        }

        public override void TryToBeCompleted()
        {
            if (state == BingoState.Special) { return; }
            base.TryToBeCompleted();
        }

        public void QuitSpecialState()
        {
            if (state != BingoState.Special) { return; }
            state = BingoState.UnFinished;
            TryToBeCompleted();
            MessageCenter.Get<BINGO_TASK_QUIT_SPECIAL>().Dispatch(taskInfo.Index);
        }
    }
    #endregion

    #region BinggoMap
    /// <summary>
    /// BingoTask活动使用的bingo数据管理类
    /// </summary>
    public class BingoTaskMap : BingoMapBase
    {
        public BingoTaskMap(int cellCountEachRow)
        {
            this.cellCountEachRow = cellCountEachRow;
        }
        /// <summary>
        /// 获取对应序号的bingoTaskCell,因为基类中存的是BingoCellBase，所以声明该函数方便使用
        /// </summary>
        /// <param name="index">bingoCell序号</param>
        /// <returns></returns>
        public bool TryGetBingoTaskCell(int index, out BingoTaskCell outCell)
        {
            outCell = null;
            if (!bingoDic.TryGetValue(index, out var cell) || cell is not BingoTaskCell taskCell) { return false; }
            outCell = taskCell;
            return true;
        }

        override public void TryCompleteBingoCell(int index)
        {
            base.TryCompleteBingoCell(index);
            if (result.HasFlag(BingoResult.Completed)) _UnlockBingoTaskCell(index);
        }

        /// <summary>
        /// 解锁刚刚完成的BingoTaskCell附近的BingoTaskCell
        /// </summary>
        /// <param name="index">刚刚完成的BingoCell序号</param>
        private void _UnlockBingoTaskCell(int index)
        {
            var list = new List<int>();
            FillAdjacentCellList(index, list);
            foreach (var id in list)
            {
                if (TryGetBingoTaskCell(id, out var cell)) { cell.QuitSpecialState(); }
            }
        }

    }
    #endregion
}
