/**
 * @Author: zhangpengjian
 * @Date: 2024/8/19 10:19:48
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/19 10:19:48
 * Description: https://centurygames.yuque.com/ywqzgn/ne0fhm/xz9edf49w7fyd932
 */

using EL;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using System.Collections.Generic;
using Config;
using Random = UnityEngine.Random;
using Google.Protobuf.Collections;
using FAT.Merge;
using EL.Resource;
using UnityEngine;

namespace FAT
{
    using static PoolMapping;

    public class ActivityDigging : ActivityLike, IBoardEntry
    {
        public enum DiggingCellState
        {
            None,           //默认状态
            KeyNotEnough,   //钥匙不足
            HasDug,         //已经挖过
            Fail,           //没挖到
            Get,            //挖到一整个海马
            GetAll,         //挖到关卡全部海马
            Bomb,           //挖到炸弹
            GetRandom,      //挖到随机奖励
            BombAndGet,     //炸弹并挖到海马
            BombAndGetAll,  //炸弹并完成关卡
        }

        public enum BombType
        {
            None = 0,
            Horizontal = 1,    // 横向爆炸
            Vertical = 2      // 竖向爆炸
        }

        public struct DiggingItem
        {
            public int x;
            public int y;
            public EventDiggingItem item;
            public BombType bombType;
        }

        public struct Node
        {
            public RewardConfig reward;
            public int value;
        }

        public struct DiggingResult
        {
            public List<DiggingItem> bombItems;  // 炸弹物品列表，按触发顺序排列
            public List<DiggingItem> obtainedItems;  // 获得的所有物品列表
            public List<int> explodedCells;  // 被炸的格子索引列表，按触发顺序排列
        }

        public override bool Valid => Lite.Valid;
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureDigging);
        private EventDiggingRound roundConfig;
        public EventDigging diggingConfig;
        public EventDiggingDetail detailConfig;
        public GiftPackLike pack;
        public bool PackValid => pack != null && pack.Stock > 0;
        private int boardId => diggingConfig.BoardId;
        #region 存储
        private int boardCellIndex1; //记录棋盘格下标是否被挖过 用两个int 按位运算来记录数据
        private int boardCellIndex2;
        private int keyNum;
        private int currentLevelIndex; //当前关卡下标
        private int currentRoundIndex; //当前轮数
        private int grpIndexMappingId; //grp id 用户分层
        private int score;
        private int diggingBoardId; //棋盘id 随机得到
        private int seed; //棋盘格子样式随机种子
        private int useKeyNum;
        private int getKeyNum;
        #endregion
        private List<DiggingItem> diggingItems = new();
        private bool hasPop;

        public readonly List<Node> ListM = new();
        public RewardConfig NormalScoreReward;
        private List<RewardCommitData> scoreCommitRewardList = new(); //积分兑换钥匙循环中：所有积分奖励对应的CommitData 待提交的积分奖励


        private int curShowScore;
        private int curTargetScore;
        private int scoreValueMax;
        private int curMileStoneIndex;
        private List<RewardConfig> rewardConfigList = new(); //寻宝积分奖励
        private int cycleScoreShowCount;
        private int normalScoreRewardDone;
        public UIResAlt Res { get; } = new(UIConfig.UIDiggingMain);
        public UIResAlt LoadingRes { get; } = new(UIConfig.UIDiggingLoading);
        public UIResAlt EndRes { get; } = new(UIConfig.UIDiggingEnd);
        public UIResAlt BeginRes { get; } = new(UIConfig.UIDiggingBegin);
        public UIResAlt BuyRes { get; } = new(UIConfig.UIDiggingGift);
        public UIResAlt HelpRes { get; } = new(UIConfig.UIDiggingHelp);
        public UIResAlt NewRoundRes { get; } = new(UIConfig.UIDiggingNewRound);
        public PopupActivity PopupStart = new();
        public PopupActivity PopupEnd = new();
        public PopupActivity PopupGift = new();
        public PopupActivity PopupHelp = new();
        public PopupActivity PopupNewRound = new();
        public ActivityVisual VisualStart = new(); //活动开启弹窗
        public ActivityVisual VisualEnd { get; } = new(); //结算
        public ActivityVisual VisualHelp { get; } = new();  //帮助
        public ActivityVisual VisualGift { get; } = new();  //买铲子
        public ActivityVisual VisualNewRound { get; } = new();  //新一轮
        public ActivityVisual VisualPanel { get; } = new();  //主界面
        public ActivityVisual VisualLoading { get; } = new();
        public override ActivityVisual Visual => VisualPanel;
        private DiggingSpawnBonusHandler spawnBonusHandler;

        public ActivityDigging(ActivityLite lite_)
        {
            Lite = lite_;
            roundConfig = Game.Manager.configMan.GetEventDiggingRoundConfig(lite_.Param);
            SetupBonusHandler();
            debugIdx = -1;
            isDebug = false;
        }

        public override bool EntryVisible
        {
            get
            {
                return HasNextRound();
            }
        }

        private void SetupPopup()
        {
            VisualPanel.Setup(diggingConfig.DiggingTheme, Res);
            VisualLoading.Setup(diggingConfig.LoadingTheme, LoadingRes);
            VisualNewRound.Setup(diggingConfig.RestartTheme, NewRoundRes);
            VisualHelp.Setup(diggingConfig.HelpPlayTheme, HelpRes);
            if (diggingConfig.BuyTheme != 0)
            {
                if (VisualGift.Setup(diggingConfig.BuyTheme, BuyRes))
                {
                    PopupGift.Setup(this, VisualGift, BuyRes);
                }
            }
            if (VisualStart.Setup(diggingConfig.StartTheme, BeginRes))
            {
                PopupStart.Setup(this, VisualStart, BeginRes);
            }
            if (VisualEnd.Setup(diggingConfig.RecontinueTheme, EndRes))
            {
                PopupEnd.Setup(this, VisualEnd, EndRes, false, false);
            }
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (!hasPop)
            {
                popup_.TryQueue(PopupStart, state_);
            }
        }

        public void TryPopupGift(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(PopupGift, state_);
        }

        public override void SetupFresh()
        {
            //新开的活动 默认挖沙第一轮
            SetupNewRound(0);
            SetupScoreReward();
            PopupStart.option = new() { ignoreLimit = true };
            Game.Manager.screenPopup.Queue(PopupStart);
            hasPop = true;
        }

        private void SetupNewRound(int roundIdx)
        {
            score = 0;
            var diggingId = roundConfig.IncludeDiggingId[roundIdx];
            var confMgr = Game.Manager.configMan;
            diggingConfig = confMgr.GetEventDiggingConfig(diggingId);

            grpIndexMappingId = Game.Manager.userGradeMan.GetTargetConfigDataId(diggingConfig.GradeId);
            detailConfig = confMgr.GetEventDiggingDetail(grpIndexMappingId);
            if (roundIdx == 0)
            {
                keyNum = diggingConfig.FreeTokenNum;
                DataTracker.token_change.Track(diggingConfig.TokenId, diggingConfig.FreeTokenNum, keyNum, ReasonString.digging_level);
                if (diggingConfig.PackGrpId > 0)
                {
                    var packId = Game.Manager.userGradeMan.GetTargetConfigDataId(diggingConfig.PackGrpId);
                    SetupPack(packId, 0);
                }
                SetupPopup();
            }
            diggingBoardId = RandomBoardId(GetCurrentLevel().IncludeBoardId);
            // UIManager.Instance.RegisterIdleAction("ui_idle_treasure_begin", 101, () => UIManager.Instance.OpenWindow(BeginRes));
        }

        public ListActivity.IEntrySetup SetupEntry(ListActivity.Entry e_)
        {
            e_.dot.SetActive(GetKeyNum() > 0);
            e_.dotCount.gameObject.SetActive(GetKeyNum() > 0);
            e_.dotCount.SetText(GetKeyNum().ToString());
            return null;
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach(var v in VisualEnd.ResEnumerate()) yield return v;
            foreach(var v in VisualGift.ResEnumerate()) yield return v;
            foreach(var v in VisualHelp.ResEnumerate()) yield return v;
            foreach(var v in VisualStart.ResEnumerate()) yield return v;
            foreach(var v in VisualPanel.ResEnumerate()) yield return v;
            foreach(var v in VisualNewRound.ResEnumerate()) yield return v;
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            boardCellIndex1 = ReadInt(1, any);
            boardCellIndex2 = ReadInt(2, any);
            keyNum = ReadInt(3, any);
            currentLevelIndex = ReadInt(4, any);
            currentRoundIndex = ReadInt(5, any);
            score = ReadInt(6, any);
            grpIndexMappingId = ReadInt(7, any);
            var packId = ReadInt(8, any);
            var packBuyCount = ReadInt(9, any);
            diggingBoardId = ReadInt(10, any);
            seed = ReadInt(11, any);
            useKeyNum = ReadInt(12, any);
            getKeyNum = ReadInt(13, any);
            if (currentRoundIndex > roundConfig.IncludeDiggingId.Count - 1)
            {
                return;
            }
            diggingConfig = Game.Manager.configMan.GetEventDiggingConfig(roundConfig.IncludeDiggingId[currentRoundIndex]);
            if (grpIndexMappingId != 0)
                detailConfig = Game.Manager.configMan.GetEventDiggingDetail(grpIndexMappingId);
            SetupPack(packId, packBuyCount);
            SetupPopup();
            SetupScoreReward();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(1, boardCellIndex1));
            any.Add(ToRecord(2, boardCellIndex2));
            any.Add(ToRecord(3, keyNum));
            any.Add(ToRecord(4, currentLevelIndex));
            any.Add(ToRecord(5, currentRoundIndex));
            any.Add(ToRecord(6, score));
            any.Add(ToRecord(7, grpIndexMappingId));
            any.Add(ToRecord(8, pack?.PackId ?? 0));
            any.Add(ToRecord(9, pack?.BuyCount ?? 0));
            any.Add(ToRecord(10, diggingBoardId));
            any.Add(ToRecord(11, seed));
            any.Add(ToRecord(12, useKeyNum));
            any.Add(ToRecord(13, getKeyNum));
        }

        internal void SetupPack(int packId_, int buy_)
        {
            if (packId_ <= 0) return;
            pack ??= new();
            pack.Setup(buy_, diggingConfig.PackTimes);
            pack.Refresh(Id, From, packId_);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().Dispatch(this);
        }

        public override void SetupClear()
        {
            scoreCommitRewardList.Clear();
        }

        public override void WhenReset()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(spawnBonusHandler);
        }

        public override void Open()
        {
            UIDiggingUtility.EnterActivity();
        }

        public override void WhenEnd()
        {
            if (!HasNextRound())
            {
                return;
            }
            TryExchangeExpireKey();
        }

        public RewardCommitData AddScore(int addScore)
        {
            var activeWorld = Game.Manager.mergeBoardMan.activeWorld;
            if (activeWorld != null)
            {
                //说明在棋盘内
                var curBoardId = activeWorld.activeBoard.boardId;
                if (curBoardId != boardId)
                {
                    DebugEx.FormatError(
                        "[ActivityDigging.AddScore]: ActiveBoardId != DiggingConfigId But TryAddScore, activeBoardId = {0}, eventDiggingConfigBoardId = {1}",
                        curBoardId, boardId);
                    return null;
                }
            }

            var reward = Game.Manager.rewardMan.BeginReward(diggingConfig.RequireScoreId, addScore, ReasonString.digging_level);
            return reward;
        }

        public void UpdateScoreOrKey(int rewardId, int addNum)
        {
            if (rewardId == diggingConfig.RequireScoreId)
            {
                var prev = score;
                score += addNum;
                CheckScore(prev);
                DebugEx.Info($"[ActivityDigging.UpdateScore] change = {addNum} afterScore = {score}");
                MessageCenter.Get<MSG.DIGGING_SCORE_UPDATE>().Dispatch(prev, score);
                DataTracker.token_change.Track(rewardId, addNum, score, ReasonString.digging_level);
            }
            else if (rewardId == diggingConfig.TokenId)
            {
                if (addNum > 0)
                {
                    getKeyNum += addNum;
                }
                else
                {
                    useKeyNum += 1;
                }
                keyNum += addNum;
                MessageCenter.Get<MSG.DIGGING_KEY_UPDATE>().Dispatch(addNum);
                DataTracker.token_change.Track(rewardId, addNum, keyNum, ReasonString.digging_level);
                DebugEx.Info($"[ActivityDigging.UpdateDiggingKey] change = {addNum} afterDiggingKey = {keyNum}");
            }
        }

        private void CheckScore(int prevScore)
        {
            //通过总积分 算出显示数据 当前里程碑目标积分 当前展示积分
            var goalScore = detailConfig.CycleLevelScore;
            var mileStone = detailConfig.LevelScore;
            //累计积分已经达到普通里程的最大值
            if (score >= scoreValueMax)
            {
                if (score - scoreValueMax >= goalScore + GetScoreCycleCount(prevScore) * goalScore)
                {
                    var finalReward = detailConfig.CycleLevelToken.ConvertToRewardConfig();
                    //发奖
                    var reward = Game.Manager.rewardMan.BeginReward(finalReward.Id, finalReward.Count,
                        ReasonString.digging_level);
                    scoreCommitRewardList.Add(reward);
                    //超过普通里程碑最大值且完成了一次循环里程碑
                    curShowScore = (score - scoreValueMax) % goalScore;
                }
                else
                {
                    if (normalScoreRewardDone == 0)
                    {
                        //只发一次
                        var reward = Game.Manager.rewardMan.BeginReward(rewardConfigList[rewardConfigList.Count - 1].Id,
                            rewardConfigList[
                                rewardConfigList.Count - 1].Count, ReasonString.digging_level);
                        scoreCommitRewardList.Add(reward);
                    }
                    //刚超过里程碑最大值 但还没有达到循环目标分值 发放最后一个里程碑奖励
                    curShowScore = score - scoreValueMax;
                }

                normalScoreRewardDone = 1;
                curTargetScore = goalScore;
            }
            else
            {
                //两种边界情况
                //1.当前分数小于第一里程碑要求分数
                if (score < mileStone[0])
                {
                    curShowScore = score;
                    curTargetScore = mileStone[0];
                    curMileStoneIndex = 0;
                    NormalScoreReward = rewardConfigList[0];
                }
                else
                {
                    var mileStoneIndex = 0;
                    for (var i = 0; i < ListM.Count; i++)
                    {
                        if (score >= ListM[i].value && score < ListM[i + 1].value)
                        {
                            curTargetScore = mileStone[i + 1];
                            curShowScore = score - ListM[i].value;
                            mileStoneIndex = i + 1;
                            break;
                        }
                    }

                    if (mileStoneIndex != curMileStoneIndex)
                    {
                        //如果一次性获得大额积分 需要发n次奖
                        for (var i = 0; i < mileStoneIndex - curMileStoneIndex; i++)
                        {
                            var reward = Game.Manager.rewardMan.BeginReward(
                                rewardConfigList[curMileStoneIndex + i].Id,
                                rewardConfigList[curMileStoneIndex + i].Count, ReasonString.digging_level);
                            scoreCommitRewardList.Add(reward);
                        }
                        MessageCenter.Get<MSG.BOARD_ORDER_SCROLL_RESET>().Dispatch();
                    }
                    curMileStoneIndex = mileStoneIndex;
                    NormalScoreReward = rewardConfigList[mileStoneIndex];
                }
            }
        }

        private void SetupBonusHandler()
        {
            if (spawnBonusHandler == null)
            {
                spawnBonusHandler = new DiggingSpawnBonusHandler(this);
                Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(spawnBonusHandler);
            }
        }

        /// <summary>
        /// 尝试挖格子
        /// </summary>
        public bool TryDiggingCell(int cellIndex, List<RewardCommitData> rewards, out DiggingCellState state, out DiggingResult result, bool isChainReaction = false)
        {
            result = new DiggingResult 
            { 
                bombItems = new List<DiggingItem>(),
                obtainedItems = new List<DiggingItem>(),
                explodedCells = new List<int>()
            };
            rewards.Clear();
            var rewardMgr = Game.Manager.rewardMan;
            state = DiggingCellState.None;
            
            //钥匙不够且不是连锁反应
            if (keyNum <= 0 && !isChainReaction)
            {
                state = DiggingCellState.KeyNotEnough;
                return false;
            }
            //已经挖过
            if (HasDug(cellIndex))
            {
                state = DiggingCellState.HasDug;
                return false;
            }
            SaveCellHasDug(cellIndex);
            //不是连锁反应才扣钥匙
            if (!isChainReaction)
            {
                //扣钥匙
                UpdateScoreOrKey(diggingConfig.TokenId, -1);
            }
            
            //是否挖到东西
            if (IsDiggingSuccess(cellIndex, out DiggingItem diggingItem))
            {
                bool isBomb = diggingItem.item.BoomType > 0;
                bool hasGotItem = false;
                bool hasGotAll = false;

                if (isBomb)
                {
                    result.bombItems.Add(diggingItem);  // 添加第一个炸弹
                    // 处理炸弹连锁反应
                    var (row, col) = GetBoardSize();
                    var currentRow = cellIndex / col;
                    var currentCol = cellIndex % col;
                    
                    List<int> cellsToExplode = new List<int>();
                    var validCount = 0;
                    // 根据炸弹类型确定需要爆炸的格子
                    if (diggingItem.item.BoomType == 1) // 横向炸弹
                    {
                        // 横向爆炸，收集同一行未挖的格子
                        for (int c = 0; c < col; c++)
                        {
                            var idx = currentRow * col + c;
                            if (!HasDug(idx) && idx != cellIndex)
                            {
                                cellsToExplode.Add(idx);
                                if (IsDiggingSuccess(idx, out _))
                                {
                                    validCount++;
                                }
                            }
                        }
                    }
                    else // 纵向炸弹
                    {
                        // 纵向爆炸，收集同一列未挖的格子
                        for (int r = 0; r < row; r++)
                        {
                            var idx = r * col + currentCol;
                            if (!HasDug(idx) && idx != cellIndex)
                            {
                                cellsToExplode.Add(idx);
                                if (IsDiggingSuccess(idx, out _))
                                {
                                    validCount++;
                                }
                            }
                        }
                    }
                    
                    // 记录被炸的格子
                    result.explodedCells.AddRange(cellsToExplode);
                    DataTracker.digging_boom.Track(this, GetCurrentLevel().ShowNum, diggingBoardId, validCount);
                    // 处理连锁爆炸
                    foreach (var idx in cellsToExplode)
                    {
                        var chainResult = new DiggingResult 
                        { 
                            bombItems = new List<DiggingItem>(),
                            obtainedItems = new List<DiggingItem>(),
                            explodedCells = new List<int>()
                        };
                        var chainState = DiggingCellState.None;
                        List<RewardCommitData> chainRewards = new List<RewardCommitData>();
                        
                        TryDiggingCell(idx, chainRewards, out chainState, out chainResult, true);
                        
                        if (chainRewards.Count > 0)
                        {
                            rewards.AddRange(chainRewards);
                        }
                        
                        // 收集连锁反应中的炸弹和被炸格子
                        if (chainResult.bombItems.Count > 0)
                        {
                            result.bombItems.AddRange(chainResult.bombItems);
                            result.explodedCells.AddRange(chainResult.explodedCells);
                        }
                        
                        if (chainState == DiggingCellState.Get || chainState == DiggingCellState.BombAndGet)
                        {
                            hasGotItem = true;
                            result.obtainedItems.AddRange(chainResult.obtainedItems);
                        }
                        
                        if (chainState == DiggingCellState.GetAll || chainState == DiggingCellState.BombAndGetAll)
                        {
                            hasGotAll = true;
                            result.obtainedItems.AddRange(chainResult.obtainedItems);
                        }
                    }
                }
                
                if (HasGot(diggingItem.x, diggingItem.y, diggingItem.item.ColSize, diggingItem.item.RowSize))
                {
                    hasGotItem = true;
                    if (!isBomb)  // 如果不是炸弹，才添加到获得物品列表
                    {
                        result.obtainedItems.Add(diggingItem);
                    }
                }
                
                if (HasGotAll())
                {
                    hasGotAll = true;
                    //发奖
                    var curLevel = GetCurrentLevel();
                    foreach (var item in curLevel.LevelReward)
                    {
                        var r = item.ConvertToRewardConfig();
                        rewards.Add(rewardMgr.BeginReward(r.Id, r.Count, ReasonString.digging_milestone));
                    }
                    var (row, col) = GetBoardSize();
                    var digNum = 0;
                    for (int i = 0; i < row * col; i++)
                    {
                        if (HasDug(i))
                        {
                            digNum += 1;
                        }
                    }
                    currentLevelIndex += 1;
                    boardCellIndex1 = 0;
                    boardCellIndex2 = 0;
                    if (currentLevelIndex > detailConfig.Includelevel.Count - 1)
                    {
                        DataTracker.digging_level_complete.Track(this, currentRoundIndex + 1, curLevel.ShowNum, diggingBoardId, digNum, detailConfig.Includelevel.Count, detailConfig.Includelevel.Count, detailConfig.Diff, true);
                        currentLevelIndex = 0;
                        //尝试开始新一轮
                        currentRoundIndex += 1;
                        if (currentRoundIndex > roundConfig.IncludeDiggingId.Count - 1)
                        {
                            //活动结束
                        }
                        else
                        {
                            DataTracker.digging_restart.Track(this, currentRoundIndex);
                            SetupNewRound(currentRoundIndex);
                        }
                    }
                    else
                    {
                        DataTracker.digging_level_complete.Track(this, currentRoundIndex + 1, curLevel.ShowNum, diggingBoardId, digNum, currentLevelIndex, detailConfig.Includelevel.Count, detailConfig.Diff, false);
                        var s = GetCurrentLevel().IncludeBoardId;
                        diggingBoardId = RandomBoardId(s);
                        GenerateNewSeed();
                    }
                }

                // 根据组合状态设置最终状态
                if (isBomb)
                {
                    if (hasGotAll)
                    {
                        state = DiggingCellState.BombAndGetAll;
                    }
                    else if (hasGotItem)
                    {
                        state = DiggingCellState.BombAndGet;
                    }
                    else
                    {
                        state = DiggingCellState.Bomb;
                    }
                }
                else
                {
                    if (hasGotAll)
                    {
                        state = DiggingCellState.GetAll;
                    }
                    else if (hasGotItem)
                    {
                        state = DiggingCellState.Get;
                    }
                }
            }
            else
            {
                state = DiggingCellState.Fail;
                // 没挖到东西时，有概率获得随机奖励
                var curLevel = GetCurrentLevel();
                if (curLevel != null && curLevel.RandomRewardInfo.Count > 0)
                {
                    // 随机奖励概率 = 配置值/100
                    var probability = curLevel.RandomRewardProbability / 100f;
                    if (Random.value < probability)
                    {
                        // 根据权重随机选择一个奖励
                        var totalWeight = 0;
                        using (ObjectPool<List<(int index, int weight)>>.GlobalPool.AllocStub(out var list))
                        {
                            for (int i = 0; i < curLevel.RandomRewardInfo.Count; i++)
                            {
                                var rewardInfo = curLevel.RandomRewardInfo[i];
                                var (id, count, weight) = rewardInfo.ConvertToInt3();
                                totalWeight += weight;
                                list.Add((i, weight));
                            }
                            
                            var roll = Random.Range(1, totalWeight + 1);
                            var weightSum = 0;
                            int selectedIndex = 0;
                            
                            foreach (var item in list)
                            {
                                weightSum += item.weight;
                                if (weightSum >= roll)
                                {
                                    selectedIndex = item.index;
                                    break;
                                }
                            }
                            
                            var selectedRewardInfo = curLevel.RandomRewardInfo[selectedIndex];
                            var rewardConfig = selectedRewardInfo.ConvertToRewardConfig();
                            var r = rewardMgr.BeginReward(rewardConfig.Id, rewardConfig.Count, ReasonString.digging_random);
                            rewards.Add(r);
                            state = DiggingCellState.GetRandom;
                            DataTracker.digging_random_reward.Track(this, GetCurrentLevel().ShowNum, diggingBoardId, rewardConfig.Id, rewardConfig.Count);
                        }
                    }
                }
            }
            Game.Manager.archiveMan.SendImmediately(true);
            return true;
        }

        /// <summary>
        /// 是否挖到了此关卡内所有海马
        /// </summary>
        /// <returns></returns>
        private bool HasGotAll()
        {
            if (diggingBoardId <= 0) return false;
            var boardConfig = Game.Manager.configMan.GetEventDiggingBoard(diggingBoardId);
            foreach (var item in boardConfig.ItemInfo)
            {
                var (itemId, x, y) = item.ConvertToInt3();
                var itemConfig = Game.Manager.configMan.GetEventDiggingItem(itemId);
                if (itemConfig.BoomType > 0)
                {
                    continue;
                }
                if (!HasGot(x, y, itemConfig.ColSize, itemConfig.RowSize))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// n个海马 是否挖到其中一个海马的其中一个格子
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool IsDiggingSuccess(int index, out DiggingItem diggingItem)
        {
            diggingItem = new DiggingItem();
            if (diggingBoardId <= 0) return false;
            var boardConfig = Game.Manager.configMan.GetEventDiggingBoard(diggingBoardId);
            
            foreach (var item in boardConfig.ItemInfo)
            {
                var (itemId, x, y) = item.ConvertToInt3();
                var itemConfig = Game.Manager.configMan.GetEventDiggingItem(itemId);
                if (IsDiggingItem(x, y, itemConfig.ColSize, itemConfig.RowSize, index))
                {
                    diggingItem.x = x;
                    diggingItem.y = y;
                    diggingItem.item = itemConfig;
                    // 检查是否是炸弹
                    if (itemConfig.BoomType >= 0)
                    {
                        diggingItem.bombType = (BombType)itemConfig.BoomType;
                    }
                    else
                    {
                        diggingItem.bombType = BombType.None;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取当前关卡所有海马（待挖掘的物品）
        /// </summary>
        public List<DiggingItem> GetCurrentLevelAllItems()
        {
            if (diggingBoardId <= 0) return null;
            diggingItems.Clear();
            var boardConfig = Game.Manager.configMan.GetEventDiggingBoard(diggingBoardId);
            foreach (var item in boardConfig.ItemInfo)
            {
                var (itemId, x, y) = item.ConvertToInt3();
                var itemConfig = Game.Manager.configMan.GetEventDiggingItem(itemId);
                if (itemConfig.BoomType > 0)
                {
                    continue;
                }
                diggingItems.Add(new DiggingItem()
                {
                    x = x,
                    y = y,
                    item = itemConfig,
                });
            }
            diggingItems.Sort((a, b) =>
            {
                var aSize = a.item.ColSize * a.item.RowSize;
                var bSize = b.item.ColSize * b.item.RowSize;
                if (aSize != bSize)
                {
                    return aSize - bSize;
                }
                return a.item.Id - b.item.Id;
            });
            return diggingItems;
        }

        /// <summary>
        /// 一个海马 占区域2X2 此函数表示 是否挖到2X2其中一格
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="right"></param>
        /// <param name="down"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool IsDiggingItem(int x, int y, int right, int down, int index)
        {
            x -= 1;
            y -= 1;
            var (row, col) = GetBoardSize();
            for (var i = x; i < x + right; i++)
            {
                for (var j = y; j < y + down; j++)
                {
                    if (i >= 0 && i < col && j >= 0 && j < row)  // 确保没有超过配置规定的格子范围
                    {
                        var idx = j * row + i;
                        if (idx == index)
                            return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public (int, int) GetBoardSize()
        {
            if (diggingBoardId <= 0) return (0, 0);
            var boardConfig = Game.Manager.configMan.GetEventDiggingBoard(diggingBoardId);
            return (boardConfig.RowNum, boardConfig.ColNum);
        }

        public RepeatedField<string> GetLevelRewards(int levelId)
        {
            var levelC = Game.Manager.configMan.GetEventDiggingLevel(levelId);
            return levelC.LevelReward;
        }

        public EventDiggingLevel GetLevelConfig(int levelId)
        {
            var levelC = Game.Manager.configMan.GetEventDiggingLevel(levelId);
            if (levelC != null)
                return levelC;
            return null;
        }

        /// <summary>
        /// 是否已经获得了某个海马
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="right"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public bool HasGot(int x, int y, int right, int down)
        {
            x -= 1;
            y -= 1;
            var (row, col) = GetBoardSize();
            for (var i = x; i < x + right; i++)
            {
                for (var j = y; j < y + down; j++)
                {
                    if (i >= 0 && i < col && j >= 0 && j < row)  // 确保没有超过配置规定的格子范围
                    {
                        var idx = j * row + i;
                        if (!HasDug(idx))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 该格子是否挖掘过
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool HasDug(int idx)
        {
            var (row, col) = GetBoardSize();
            if (idx < 0 || idx >= row * col)
            {
                return false;
            }
            if (idx < 32)
            {
                return (boardCellIndex1 & (1 << idx)) != 0;
            }
            else
            {
                idx -= 32;
                return (boardCellIndex2 & (1 << idx)) != 0;
            }
        }

        private void SaveCellHasDug(int idx)
        {
            if (idx < 32)
            {
                boardCellIndex1 |= 1 << idx;
            }
            else
            {
                idx -= 32;
                boardCellIndex2 |= 1 << idx;
            }
        }

        public void TryExchangeExpireKey()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(spawnBonusHandler);
            if (diggingConfig == null)
            {
                return;
            }
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            using var _ = PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            map[diggingConfig.TokenId] = keyNum;
            ActivityExpire.ConvertToReward(diggingConfig.ExpirePopup, list, ReasonString.digging_end, token_:map);
            var roundNum = HasNextRound() ? currentRoundIndex : roundConfig.IncludeDiggingId.Count;
            DataTracker.digging_end.Track(this, roundNum, getKeyNum, useKeyNum, keyNum);
            keyNum = 0;
            if (UIManager.Instance.IsShow(Res.ActiveR))
            {
                UIConfig.UIDiggingEnd.Open(this, listT);
            }
            else
            {
                Game.Manager.screenPopup.Queue(PopupEnd, listT);
            }
        }

        public int GetKeyNum()
        {
            return keyNum;
        }

        public int GetScore()
        {
            return score;
        }

        public int GetScoreMax()
        {
            return scoreValueMax;
        }

        public bool HasNextRound()
        {
            if (roundConfig != null)
                return currentRoundIndex < roundConfig.IncludeDiggingId.Count;
            return false;
        }

        public bool HasNextLevel()
        {
            return currentLevelIndex < detailConfig.Includelevel.Count;
        }

        public EventDiggingLevel GetCurrentLevel()
        {
            if (currentLevelIndex < 0 || currentLevelIndex > detailConfig.Includelevel.Count - 1)
                return null;
            var levelId = detailConfig.Includelevel[currentLevelIndex];
            var levelConfig = Game.Manager.configMan.GetEventDiggingLevel(levelId);
            return levelConfig;
        }

        public EventDiggingLevel GetLastLevel()
        {
            if (currentLevelIndex < 0 || currentLevelIndex > detailConfig.Includelevel.Count - 1)
                return null;
            var levelId = detailConfig.Includelevel[detailConfig.Includelevel.Count - 1];
            var levelConfig = Game.Manager.configMan.GetEventDiggingLevel(levelId);
            return levelConfig;
        }

        public (int, int) GetLevelInfo()
        {
            var levelCount = detailConfig.Includelevel.Count;
            return (currentLevelIndex, levelCount);
        }


        public int GetShowLevelNum()
        {
            return GetCurrentLevel().ShowNum;
        }

        /// <summary>
        /// 随机一个棋盘内容 避免每个玩家挖沙策略一致
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private int RandomBoardId(RepeatedField<string> s)
        {
            var totalWeight = 0;
            using (ObjectPool<List<(int id, int weight)>>.GlobalPool.AllocStub(out var list))
            {
                foreach (var board in s)
                {
                    var (id, w, _) = board.ConvertToInt3();
                    totalWeight += w;
                    list.Add((id, w));
                }
                if (isDebug)
                {
                    return list[debugIdx].id;
                }
                var roll = Random.Range(1, totalWeight);
                var weightSum = 0;
                foreach (var board in list)
                {
                    weightSum += board.weight;
                    if (weightSum >= roll)
                    {
                        DataTracker.digging_level_random.Track(this, GetCurrentLevel().ShowNum, board.id);
                        return board.id;
                    }
                }
            }

            return 0;
        }

        #region 棋盘入口相关
        /// <summary>
        /// 入口展示分数
        /// </summary>
        /// <returns></returns>
        public (int, int) GetScoreShowNum()
        {
            //初始化 积分进度和积分奖励
            var goalScore = detailConfig.CycleLevelScore;
            var mileStone = detailConfig.LevelScore;
            //累计积分已经达到普通里程的最大值
            if (score >= scoreValueMax)
            {
                if (score - scoreValueMax >= goalScore)
                {
                    //超过普通里程碑最大值且完成了一次循环里程碑
                    curShowScore = (score - scoreValueMax) % goalScore;
                }
                else
                {
                    //刚超过里程碑最大值 但还没有达到循环目标分值
                    curShowScore = score - scoreValueMax;
                }

                curTargetScore = goalScore;
            }
            else
            {
                //两种边界情况
                //1.当前分数小于第一里程碑要求分数
                if (score < mileStone[0])
                {
                    curShowScore = score;
                    curTargetScore = mileStone[0];
                    curMileStoneIndex = 0;
                    NormalScoreReward = rewardConfigList[0];
                }
                else
                {
                    var mileStoneIndex = 0;
                    for (var i = 0; i < ListM.Count; i++)
                    {
                        if (score >= ListM[i].value && score < ListM[i + 1].value)
                        {
                            curTargetScore = mileStone[i + 1];
                            curShowScore = score - ListM[i].value;
                            mileStoneIndex = i + 1;
                            break;
                        }
                    }

                    curMileStoneIndex = mileStoneIndex;
                    NormalScoreReward = rewardConfigList[mileStoneIndex];
                }
            }

            return (curShowScore, curTargetScore);
        }

        public RewardConfig GetScoreShowReward()
        {
            if (score >= scoreValueMax)
                return detailConfig.CycleLevelToken.ConvertToRewardConfig();
            else
                return NormalScoreReward;
        }

        public int ScoreRewardNext(int v_)
        {
            if (v_ >= scoreValueMax)
            {
                return -1;
            }
            var ret = ListM.Count;
            for (var n = 0; n < ListM.Count; ++n)
            {
                var node = ListM[n];
                var ready = v_ >= node.value;
                if (!ready)
                {
                    ret = n;
                    break;
                }
            }
            return ret;
        }

        public int GetScoreCycleCount(int prevScore = 0)
        {
            var s = prevScore > 0 ? prevScore : score;
            return (s - scoreValueMax) / detailConfig.CycleLevelScore;
        }

        public RewardCommitData TryGetCommitReward(RewardConfig reward)
        {
            RewardCommitData rewardCommitData = null;
            foreach (var commitData in scoreCommitRewardList)
            {
                if (commitData.rewardId == reward.Id && commitData.rewardCount == reward.Count)
                {
                    rewardCommitData = commitData;
                    break;
                }
            }
            return rewardCommitData;
        }

        /// <summary>
        /// 初始化分数奖励
        /// </summary>
        private void SetupScoreReward()
        {
            var confR = detailConfig.LevelToken;
            var confS = detailConfig.LevelScore;
            ListM.Clear();
            var s = 0;
            for (var n = 0; n < confR.Count; ++n)
            {
                var v = 0;
                s += confS[n];
                ListM.Add(new()
                {
                    reward = confR[n].ConvertToRewardConfig(),
                    value = s,
                });
            }

            scoreValueMax = s;

            foreach (var reward in detailConfig.LevelToken)
            {
                rewardConfigList.Add(reward.ConvertToRewardConfig());
            }
        }
        #endregion

        public int GetSeed()
        {
            return seed;
        }

        private void GenerateNewSeed()
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
        }

        #region Debug
        private int debugIdx = -1;
        private bool isDebug;
        internal void DebugLevelMap()
        {
            isDebug = true;
            if (debugIdx >= 3)
            {
                debugIdx = 3;
            }
            else
            {
                debugIdx += 1;
            }
            UnityEngine.Debug.Log($"ActivityDigging Debug Number = {debugIdx + 1}");
        }

        public string BoardEntryAsset()
        {
            if (VisualPanel.Theme != null)
            {
                VisualPanel.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
                return key;
            }
            return null;
        }

        public bool BoardEntryVisible => HasNextRound();

        #endregion
    }
}