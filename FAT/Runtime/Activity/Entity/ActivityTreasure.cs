/*
 * @Author: pengjian.zhang
 * @Description: 寻宝活动
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/qeheb5gzw5vglxey
 * @Date: 2024-04-15 17:39:28
 */

using System;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using System.Collections.Generic;
using Config;
using FAT.Merge;
using Google.Protobuf.Collections;
using Random = UnityEngine.Random;
using EL.Resource;
using System.Linq;

namespace FAT
{
    using static PoolMapping;

    public class ActivityTreasure : ActivityLike, IBoardEntry
    {
        public enum TreasureBoxState
        {
            None,
            KeyNotEnough,
            HasOpen,
            Empty,
            NormalReward,
            Treasure,
        }

        private enum TreasureParamKey
        {
            KeyNum,
            Score,
            IsSent,
            Appear,
            NoAppear,
            OpenCount,
            Group,
            Level,
            BoxIdx,
            CycleScore,
            NormalRewardDone,
            IsEnterHuntGuided,
            PackId,
            PackBuyCount,
            IsLeaveHuntGuided,
            RewardIdx,
            GrpId,
            BonusTokenPhase,
            BonusToken,
            BonusTokenRound,
            RewardSplit,
        }

        public struct Node
        {
            public RewardConfig reward;
            public int value;
        }

        public override bool Valid => Lite.Valid;

        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureTreasure);
        public UIResAlt Res { get; } = new(UIConfig.UITreasureHuntMain);
        public UIResAlt HelpRes { get; } = new(UIConfig.UITreasureHuntHelpPopup);
        public UIResAlt HelpTabRes { get; } = new(UIConfig.UITreasureHuntHelp);
        public UIResAlt BeginRes { get; } = new(UIConfig.UITreasureHuntStartNotice);
        public UIResAlt EndRes { get; } = new(UIConfig.UITreasureHuntEnd);
        public UIResAlt LevelRewardRes { get; } = new(UIConfig.UITreasureHuntLevelReward);
        public UIResAlt ProgressRewardRes { get; } = new(UIConfig.UITreasureHuntProgressReward);
        public UIResAlt LoadingRes { get; } = new(UIConfig.UITreasureHuntLoading);
        public PopupActivity Popup { get; internal set; }
        public PopupActivity PopupHelp { get; internal set; }
        public PopupActivity PopupEnd { get; internal set; }
        public PopupActivity PopupGift { get; internal set; }
        public ActivityVisual VisualEnd { get; } = new();
        public ActivityVisual VisualGift { get; } = new();
        public ActivityVisual VisualHelp { get; } = new();
        public ActivityVisual VisualHelpTab { get; } = new();
        public ActivityVisual VisualStart { get; } = new();
        public ActivityVisual VisualLevelReward { get; } = new();
        public ActivityVisual VisualProgressReward { get; } = new();
        public ActivityVisual VisualLoading { get; } = new();
        public EventTreasure ConfD;
        public readonly List<Node> ListM = new();
        public RewardConfig NormalScoreReward;
        public bool PackValid => pack != null && pack.Stock > 0;

        public int BagItemNum => tempRewardBag?.Count ?? 0; // 背包内物品数量
        public bool CanClaimBagReward => BagItemNum > 0; //临时背包是否有奖励可领
        public GiftPackLike pack;

        private int boardId => ConfD.BoardId;
        private int curShowScore;
        private int curTargetScore;
        private int keyNum;
        private int score;
        private int isSent; //是否是新的一次活动，是否赠送了初始钥匙
        private int scoreValueMax;
        private int curMileStoneIndex;
        private List<RewardConfig> rewardConfigList = new(); //寻宝积分奖励
        private TreasureSpawnBonusHandler spawnBonusHandler;
        private int currentLevelGroupIndex; //当前关卡组下标（有可能关卡组重复利用）
        private int currentLevelIndex; //当前关卡下标
        private int currentLevelNoAppear; //前几次必不出
        private int currentLevelAppear; //第几次必出
        private int currentLevelOpenCount; //当前关卡已经开了多少次宝箱
        private int boxIdx; //用位来记录宝箱是否开启过
        private int boxRewardIdx; //用位来记录奖励是否被领取过
        private int grpIndexMappingId; //grp id 用户分层
        private int bonusTokenPhase; //空箱token阶段
        private int bonusTokenRound;
        private int bonusToken; //空箱token数量
        public RewardConfig bonusTokenReward;
        private EventTreasureGroupDetail grpMappingConfig;
        private int cycleScoreCount;
        private int cycleScoreShowCount;
        private int normalScoreRewardDone;
        private int isEnterHuntGuided; //首次开启活动 进入寻宝界面弹窗
        private int isLeaveHuntGuided; //首次开启活动 进入寻宝界面后回到主棋盘弹窗
        private int addTempNum;
        private List<RewardConfig> tempRewardBag = new(); //临时背包
        private List<EventTreasureReward> currentLevelRewards = new(); //当前关卡里所有宝箱奖励配置
        private List<RewardCommitData> scoreCommitRewardList = new(); //积分兑换钥匙循环中：所有积分奖励对应的CommitData 待提交的积分奖励
        private List<RewardCommitData> tempRewardList = new();

        public ActivityTreasure(ActivityLite lite_)
        {
            Lite = lite_;
            ConfD = Game.Manager.configMan.GetEventTreasureConfig(lite_.Param);
            if (ConfD == null) return;
            SetupBonusHandler();
            //初始化弹脸
            if (Visual.Setup(ConfD.TreasureTheme, Res))
            {
                Popup = new(this, Visual, Res, false);
            }
            VisualHelpTab.Setup(ConfD.HelpPlayTheme, HelpTabRes);
            VisualLevelReward.Setup(ConfD.LevelRewardTheme, LevelRewardRes);
            VisualProgressReward.Setup(ConfD.ProgressRewardTheme, ProgressRewardRes);
            VisualLoading.Setup(ConfD.LoadingTheme, LoadingRes);
            VisualHelp.Setup(ConfD.HelpKeyTheme, HelpRes);

            if (VisualEnd.Setup(ConfD.RecontinueTheme, EndRes))
            {
                PopupEnd = new(this, VisualEnd, EndRes, false, active_: false);
                var map = new VisualMap(VisualEnd.Theme.TextInfo);
                Visual.Theme.TextInfo.TryGetValue("mainTitle", out string maintitle);
                map.TryReplace("mainTitle", maintitle);
                map.TryReplace("subTitle1", "#SysComDesc306");
                var c = Game.Manager.objectMan.GetTokenConfig(ConfD.RequireCoinId);
                map.TryReplace("subTitle3", I18N.FormatText("#SysComDesc766", c.SpriteName));
                map.TryReplace("desc1", "#SysComDesc273");
            }
            var ui = new UIResAlt(UIConfig.UITreasureHuntGift);
            if (ConfD.BuyTheme != 0)
            {
                if (VisualGift.Setup(ConfD.BuyTheme, ui))
                {
                    PopupGift = new(this, VisualGift, ui);
                }
            }
            VisualStart.Setup(ConfD.EventTheme);
            //首次开启活动 送初始钥匙
            if (keyNum == 0 && isSent == 0)
            {
                keyNum = ConfD.KeyNum;
                isSent = 1;
            }
        }

        public override bool EntryVisible
        {
            get
            {
                var hasLevel = HasNextLevelGroup();
                return hasLevel;
            }
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach(var v in VisualEnd.ResEnumerate()) yield return v;
            foreach(var v in VisualGift.ResEnumerate()) yield return v;
            foreach(var v in VisualHelp.ResEnumerate()) yield return v;
            foreach(var v in VisualStart.ResEnumerate()) yield return v;
            foreach(var v in VisualHelpTab.ResEnumerate()) yield return v;
            foreach(var v in VisualLevelReward.ResEnumerate()) yield return v;
            foreach(var v in VisualProgressReward.ResEnumerate()) yield return v;
            foreach(var v in VisualLoading.ResEnumerate()) yield return v;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (!HasNextLevelGroup())
                return;
            popup_.TryQueue(Popup, state_);
            popup_.TryQueue(PopupHelp, state_);
            if (CanClaimBagReward)
            {
                UIManager.Instance.RegisterIdleAction("ui_idle_treasure_reward", 103, () => { TryClaimTempBagReward(); });
            }
        }

        public void TryPopupGift(ScreenPopup popup_, PopupType state_)
        {
            UIManager.Instance.OpenWindow(PopupGift.PopupRes, this);
        }

        private int TryGetAnyStateValue(RepeatedField<AnyState> any, int index, int rewardIdx)
        {

            if (index < any.Count && index < rewardIdx)
            {
                return any[index].Value;
            }
            return 0;
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var rewardIdx = any.Count;
            for (var i = 0; i < any.Count; i++)
            {
                if (any[i].Value == -1)
                {
                    rewardIdx = i;
                    break;
                }
            }
            keyNum = TryGetAnyStateValue(any, (int)TreasureParamKey.KeyNum, rewardIdx);
            score = TryGetAnyStateValue(any, (int)TreasureParamKey.Score, rewardIdx);
            isSent = TryGetAnyStateValue(any, (int)TreasureParamKey.IsSent, rewardIdx);
            currentLevelAppear = TryGetAnyStateValue(any, (int)TreasureParamKey.Appear, rewardIdx);
            currentLevelNoAppear = TryGetAnyStateValue(any, (int)TreasureParamKey.NoAppear, rewardIdx);
            currentLevelOpenCount = TryGetAnyStateValue(any, (int)TreasureParamKey.OpenCount, rewardIdx);
            currentLevelGroupIndex = TryGetAnyStateValue(any, (int)TreasureParamKey.Group, rewardIdx);
            currentLevelIndex = TryGetAnyStateValue(any, (int)TreasureParamKey.Level, rewardIdx);
            boxIdx = TryGetAnyStateValue(any, (int)TreasureParamKey.BoxIdx, rewardIdx);
            cycleScoreCount = TryGetAnyStateValue(any, (int)TreasureParamKey.CycleScore, rewardIdx);
            cycleScoreShowCount = cycleScoreCount;
            normalScoreRewardDone = TryGetAnyStateValue(any, (int)TreasureParamKey.NormalRewardDone, rewardIdx);
            isEnterHuntGuided = TryGetAnyStateValue(any, (int)TreasureParamKey.IsEnterHuntGuided, rewardIdx);
            var packId = TryGetAnyStateValue(any, (int)TreasureParamKey.PackId, rewardIdx);
            var packBuyCount = TryGetAnyStateValue(any, (int)TreasureParamKey.PackBuyCount, rewardIdx);
            isLeaveHuntGuided = TryGetAnyStateValue(any, (int)TreasureParamKey.IsLeaveHuntGuided, rewardIdx);
            boxRewardIdx = TryGetAnyStateValue(any, (int)TreasureParamKey.RewardIdx, rewardIdx);
            grpIndexMappingId = TryGetAnyStateValue(any, (int)TreasureParamKey.GrpId, rewardIdx);
            bonusTokenPhase = TryGetAnyStateValue(any, (int)TreasureParamKey.BonusTokenPhase, rewardIdx);
            bonusToken = TryGetAnyStateValue(any, (int)TreasureParamKey.BonusToken, rewardIdx);
            bonusTokenRound = TryGetAnyStateValue(any, (int)TreasureParamKey.BonusTokenRound, rewardIdx);
            if (grpIndexMappingId != 0)
                grpMappingConfig = Game.Manager.configMan.GetEventTreasureGroupDetailConfig(grpIndexMappingId);
            else
            {
                var gradeId = ConfD.Id switch
                {
                    1 => 1,
                    2 => 2,
                    _ => 1
                };
                grpIndexMappingId = gradeId;
                grpMappingConfig = Game.Manager.configMan.GetEventTreasureGroupDetailConfig(grpIndexMappingId);
            }
            if (HasNextLevelGroup())
            {
                SetupScoreReward();
                SetupLevel();
            }
            SetupPack(packId, packBuyCount);

            if (rewardIdx != any.Count)
            {
                for (var i = rewardIdx + 1; i < any.Count; i++)
                {
                    var r = new RewardConfig
                    {
                        Id = any[i].Id,
                        Count = any[i].Value
                    };
                    tempRewardBag.Add(r);
                }
            }
            if (isLeaveHuntGuided > 0)
            {
                PopupHelp = new(this, VisualHelp, HelpRes, true);
            }
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Insert((int)TreasureParamKey.KeyNum, ToRecord(0, keyNum));
            any.Insert((int)TreasureParamKey.Score, ToRecord(0, score));
            any.Insert((int)TreasureParamKey.IsSent, ToRecord(0, isSent));
            any.Insert((int)TreasureParamKey.Appear, ToRecord(0, currentLevelAppear));
            any.Insert((int)TreasureParamKey.NoAppear, ToRecord(0, currentLevelNoAppear));
            any.Insert((int)TreasureParamKey.OpenCount, ToRecord(0, currentLevelOpenCount));
            any.Insert((int)TreasureParamKey.Group, ToRecord(0, currentLevelGroupIndex));
            any.Insert((int)TreasureParamKey.Level, ToRecord(0, currentLevelIndex));
            any.Insert((int)TreasureParamKey.BoxIdx, ToRecord(0, boxIdx));
            any.Insert((int)TreasureParamKey.CycleScore, ToRecord(0, cycleScoreCount));
            any.Insert((int)TreasureParamKey.NormalRewardDone, ToRecord(0, normalScoreRewardDone));
            any.Insert((int)TreasureParamKey.IsEnterHuntGuided, ToRecord(0, isEnterHuntGuided));
            any.Insert((int)TreasureParamKey.PackId, ToRecord(0, pack?.PackId ?? 0));
            any.Insert((int)TreasureParamKey.PackBuyCount, ToRecord(0, pack?.BuyCount ?? 0));
            any.Insert((int)TreasureParamKey.IsLeaveHuntGuided, ToRecord(0, isLeaveHuntGuided));
            any.Insert((int)TreasureParamKey.RewardIdx, ToRecord(0, boxRewardIdx));
            any.Insert((int)TreasureParamKey.GrpId, ToRecord(0, grpIndexMappingId));
            any.Insert((int)TreasureParamKey.BonusTokenPhase, ToRecord(0, bonusTokenPhase));
            any.Insert((int)TreasureParamKey.BonusToken, ToRecord(0, bonusToken));
            any.Insert((int)TreasureParamKey.BonusTokenRound, ToRecord(0, bonusTokenRound));
            any.Insert((int)TreasureParamKey.RewardSplit, ToRecord(0, -1));
            foreach (var reward in tempRewardBag)
            {
                any.Insert(any.Count, ToRecord(reward.Id, reward.Count));
            }
        }

        public override void SetupFresh()
        {
            grpIndexMappingId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            grpMappingConfig = Game.Manager.configMan.GetEventTreasureGroupDetailConfig(grpIndexMappingId);
            SetupScoreReward();
            SetupLevel();
            if (ConfD.PackGrpId > 0)
            {
                var packId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.PackGrpId);
                SetupPack(packId, 0);
            }
            if (Active)
            {
                DataTracker.token_change.Track(ConfD.RequireCoinId, ConfD.KeyNum, keyNum, ReasonString.treasure_reward);
                
                UIManager.Instance.RegisterIdleAction("ui_idle_treasure_begin", 101, () => 
                {
                    if (Active)
                    {
                        BeginRes.ActiveR.Open(this);
                    }
                });
            }
        }

        public override void SetupClear()
        {
            scoreCommitRewardList.Clear();
        }

        internal void SetupPack(int packId_, int buy_)
        {
            if (packId_ <= 0) return;
            pack ??= new();
            pack.Setup(buy_, ConfD.PackTimes);
            pack.Refresh(Id, From, packId_);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().Dispatch(this);
        }

        public override void Open()
        {
            UITreasureHuntUtility.EnterActivity();
        }

        public override void WhenReset()
        {
        }

        public override void WhenEnd()
        {
            if (!HasNextLevelGroup())
            {
                return;
            }
            if (UIManager.Instance.IsIdleIn(Res.ActiveR))
                return;
            TryExchangeExpireTreasureKey();
            if (CanClaimBagReward)
            {
                UIManager.Instance.RegisterIdleAction("ui_idle_treasure_reward", 103, () => { TryClaimTempBagReward(); });
            }
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
                        "[ActivityTreasure.AddScore]: ActiveBoardId != TreasureConfigId But TryAddScore, activeBoardId = {0}, eventTreasureConfigBoardId = {1}",
                        curBoardId, boardId);
                    return null;
                }
            }

            var reward = Game.Manager.rewardMan.BeginReward(ConfD.RequireScoreId, addScore, ReasonString.treasure);
            return reward;
        }

        public void UpdateScoreOrTreasureKey(int rewardId, int addNum)
        {
            if (rewardId == ConfD.RequireScoreId)
            {
                var canAddScore = ((addTempNum + addNum) / 2) >= 1;
                if ((addTempNum + addNum) % 2 == 0 || canAddScore)
                {
                    var prev = score;
                    var resultScoreAdd = ((addTempNum + addNum) / 2) * 2;
                    score += resultScoreAdd;
                    if ((addTempNum + addNum) % 2 == 0)
                        addTempNum = 0;
                    CheckScore();
                    MessageCenter.Get<MSG.TREASURE_SCORE_UPDATE>().Dispatch(prev, score);
                    DataTracker.token_change.Track(rewardId, addNum, score, ReasonString.treasure);
                }
                else
                {
                    addTempNum = (addTempNum + addNum) % 2;
                }
            }
            else if (rewardId == ConfD.RequireCoinId)
            {
                keyNum += addNum;
                MessageCenter.Get<MSG.TREASURE_KEY_UPDATE>().Dispatch(addNum);
            }
            else if (rewardId == ConfD.BonusToken)
            {
                bonusToken += addNum;
                CheckBonusToken();
            }
        }

        private void CheckBonusToken() 
        {
            var max = grpMappingConfig.BonusPoints[bonusTokenPhase];
            if (bonusToken < max) return;
            var reward = grpMappingConfig.BonusReward[bonusTokenPhase].ConvertToRewardConfig();
            tempRewardBag.Add(reward);
            DataTracker.event_treasure_bonus.Track(Id, Param, From, bonusTokenPhase + 1, grpMappingConfig.BonusPoints.Count, grpMappingConfig.Diff, bonusTokenPhase == grpMappingConfig.BonusPoints.Count - 1, bonusTokenRound + 1);
            SetBonusTokenReward(reward);
            bonusTokenPhase++;
            if (bonusTokenPhase >= grpMappingConfig.BonusPoints.Count)
            {
                bonusTokenRound++;
                bonusTokenPhase = 0;
            }
            bonusToken -= max;
        }

        public (int, int) GetBonusTokenShowNum()
        {
            var max = grpMappingConfig.BonusPoints[bonusTokenPhase];
            return (bonusToken, max);
        }

        public void SetBonusTokenReward(RewardConfig reward)
        {
            bonusTokenReward = reward;
        }

        public RewardConfig GetBonusTokenShowReward()
        {
            return grpMappingConfig.BonusReward[bonusTokenPhase].ConvertToRewardConfig();
        }

        private void CheckScore()
        {
            //通过总积分 算出显示数据 当前里程碑目标积分 当前展示积分
            var goalScore = grpMappingConfig.CycleLevelScore;
            var mileStone = grpMappingConfig.LevelScore;
            //累计积分已经达到普通里程的最大值
            if (score >= scoreValueMax)
            {
                if (score - scoreValueMax >= goalScore + cycleScoreCount * goalScore)
                {
                    cycleScoreCount += 1;
                    var finalReward = grpMappingConfig.CycleLevelReward.ConvertToRewardConfig();
                    //发奖
                    var reward = Game.Manager.rewardMan.BeginReward(finalReward.Id, finalReward.Count,
                        ReasonString.treasure);
                    //发奖发到开宝箱钥匙奖励时 ReasonString不同 所以散落到各处打点
                    if (reward.rewardId == ConfD.RequireCoinId)
                        DataTracker.token_change.Track(reward.rewardId, reward.rewardCount, keyNum, ReasonString.treasure_reward);
                    scoreCommitRewardList.Add(reward);
                    DataTracker.event_treasure_score.Track(Id, Param, mileStone.Count + cycleScoreCount, From, currentLevelGroupIndex + 1);
                    //超过普通里程碑最大值且完成了一次循环里程碑
                    curShowScore = (score - scoreValueMax) % goalScore;
                }
                else
                {
                    if (normalScoreRewardDone == 0)
                    {
                        //只发一次
                        var reward = Game.Manager.rewardMan.BeginReward(rewardConfigList[rewardConfigList.Count - 1].Id,
                            rewardConfigList[rewardConfigList.Count - 1].Count, ReasonString.treasure);
                        if (reward.rewardId == ConfD.RequireCoinId)
                            DataTracker.token_change.Track(reward.rewardId, reward.rewardCount, keyNum, ReasonString.treasure_reward);
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
                                rewardConfigList[curMileStoneIndex + i].Count, ReasonString.treasure);
                            scoreCommitRewardList.Add(reward);
                            if (reward.rewardId == ConfD.RequireCoinId)
                                DataTracker.token_change.Track(reward.rewardId, reward.rewardCount, keyNum, ReasonString.treasure_reward);
                            DataTracker.event_treasure_score.Track(Id, Param, mileStoneIndex, From, currentLevelGroupIndex + 1);
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
                spawnBonusHandler = new TreasureSpawnBonusHandler(this);
                Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(spawnBonusHandler);
            }
        }

        /// <summary>
        /// 获取当前关卡组配置
        /// </summary>
        /// <returns></returns>
        public EventTreasureGroup GetCurrentTreasureGroup()
        {
            var groupId = grpMappingConfig.IncludeGrpId[currentLevelGroupIndex];
            var grpConfig = Game.Manager.configMan.GetEventTreasureGroupConfig(groupId);
            return grpConfig;
        }

        /// <summary>
        /// 获取当前关卡配置
        /// </summary>
        /// <returns></returns>
        public EventTreasureLevel GetCurrentTreasureLevel()
        {
            var grpConfig = GetCurrentTreasureGroup();
            var levelId = grpConfig.IncludeLvId[currentLevelIndex];
            var levelConfig = Game.Manager.configMan.GetEventTreasureLevelConfig(levelId);
            return levelConfig;
        }

        /// <summary>
        /// 获取寻宝里程碑奖励
        /// </summary>
        /// <returns>RewardData</returns>
        public void GetMileStoneReward(List<RewardConfig> container)
        {
            var grpConfig = GetCurrentTreasureGroup();
            foreach (var r in grpConfig.MilestoneReward)
            {
                var reward = r.ConvertToRewardConfig();
                container.Add(reward);
            }
        }

        /// <summary>
        /// 获取当前关卡宝藏奖励
        /// </summary>
        /// <returns></returns>
        public void GetCurrentLevelTreasureRewardConfig(List<RewardConfig> container)
        {
            EventTreasureReward treasure = null;
            var level = GetCurrentTreasureLevel();
            var boxes = GetCurrentLevelBoxes();
            foreach (var re in boxes)
            {
                if (re.IsTreasure)
                {
                    treasure = re;
                    break;
                }
            }
            if (treasure == null)
                DebugEx.Info(
                    $"[ActivityTreasure.GetCurrentLevelTreasureRewardConfig] no treasure, Treasure = {level.Id}");
            CalcReward(treasure, container, true);
        }

        /// <summary>
        /// 初始化关卡信息
        /// </summary>
        private void SetupLevel()
        {
            RandomAppearProbability();
        }

        public List<EventTreasureReward> GetCurrentLevelBoxes()
        {
            currentLevelRewards.Clear();
            var level = GetCurrentTreasureLevel();
            foreach (var rId in level.RewardInfo)
            {
                currentLevelRewards.Add(Game.Manager.configMan.GetEventTreasureRewardConfig(rId));
            }

            return currentLevelRewards;
        }

        /// <summary>
        /// 尝试打开宝箱
        /// </summary>
        public bool TryOpenBox(int boxIndex, List<RewardConfig> treasureRewards, List<RewardConfig> mileStoneRewards, out TreasureBoxState state)
        {
            state = TreasureBoxState.None;
            //钥匙不够
            if (keyNum <= 0)
            {
                state = TreasureBoxState.KeyNotEnough;
                return false;
            }
            //已经开过
            if (HasOpen(boxIndex))
            {
                state = TreasureBoxState.HasOpen;
                return false;
            }
            UpdateScoreOrTreasureKey(ConfD.RequireCoinId, -1);
            var boxes = GetCurrentLevelBoxes();
            if (currentLevelOpenCount < currentLevelNoAppear)
            {
                using (ObjectPool<List<EventTreasureReward>>.GlobalPool.AllocStub(out var listR))
                {

                    for (var i = 0; i < boxes.Count; i++)
                    {
                        if (!boxes[i].IsTreasure && !HasGetReward(i))
                            listR.Add(boxes[i]);
                    }
                    var rId = RandomBoxRewardId(listR);
                    var group = GetCurrentTreasureGroup();
                    var rewardIndex = 0;
                    for (int i = 0; i < boxes.Count; i++)
                    {
                        if (rId == boxes[i].Id)
                        {
                            rewardIndex = i;
                            break;
                        }
                    }
                    SaveBoxIndexOpened(boxIndex);
                    SaveRewardIndexOpened(rewardIndex);
                    var rConfig = Game.Manager.configMan.GetEventTreasureRewardConfig(rId);
                    CalcReward(rConfig, treasureRewards);
                    state = treasureRewards.Count > 0 ? TreasureBoxState.NormalReward : TreasureBoxState.Empty;
                    currentLevelOpenCount += 1;
                    DataTracker.event_treasure.Track(Id, Param, grpMappingConfig.IncludeGrpId[currentLevelGroupIndex], group.IncludeLvId[currentLevelIndex], rId, 0, From, currentLevelGroupIndex + 1, currentLevelIndex + 1, currentLevelOpenCount);
                }
            }
            else if (currentLevelOpenCount >= currentLevelAppear - 1)
            {
                //必出宝藏
                EventTreasureReward treasure = null;
                foreach (var re in boxes)
                {
                    if (re.IsTreasure)
                    {
                        treasure = re;
                        break;
                    }
                }
                if (treasure != null)
                {
                    var group = GetCurrentTreasureGroup();
                    currentLevelOpenCount += 1;
                    DataTracker.event_treasure.Track(Id, Param, grpMappingConfig.IncludeGrpId[currentLevelGroupIndex], group.IncludeLvId[currentLevelIndex], treasure.Id, 1, From, currentLevelGroupIndex + 1, currentLevelIndex + 1, currentLevelOpenCount);
                    CalcReward(treasure, treasureRewards);
                    state = TreasureBoxState.Treasure;
                    OnChangeLevel(mileStoneRewards);
                }
            }
            else
            {
                using (ObjectPool<List<EventTreasureReward>>.GlobalPool.AllocStub(out var listR))
                {
                    for (var i = 0; i < boxes.Count; i++)
                    {
                        if (!HasGetReward(i))
                            listR.Add(boxes[i]);
                    }
                    var rId = RandomBoxRewardId(listR);
                    var group = GetCurrentTreasureGroup();
                    var rewardIndex = 0;
                    for (var i = 0; i < boxes.Count; i++)
                    {
                        if (rId == boxes[i].Id)
                        {
                            rewardIndex = i;
                            break;
                        }
                    }
                    SaveBoxIndexOpened(boxIndex);
                    SaveRewardIndexOpened(rewardIndex);
                    var rConfig = Game.Manager.configMan.GetEventTreasureRewardConfig(rId);
                    CalcReward(rConfig, treasureRewards);
                    currentLevelOpenCount += 1;
                    DataTracker.event_treasure.Track(Id, Param, grpMappingConfig.IncludeGrpId[currentLevelGroupIndex], group.IncludeLvId[currentLevelIndex], rId, rConfig.IsTreasure ? 1 : 0, From, currentLevelGroupIndex + 1, currentLevelIndex + 1, currentLevelOpenCount);
                    if (rConfig.IsTreasure)
                    {
                        state = TreasureBoxState.Treasure;
                        //如果不配几次必出与必不出时 开出宝藏 完成本关卡
                        OnChangeLevel(mileStoneRewards);
                    }
                    else if (treasureRewards.Count > 0)
                    {
                        state = TreasureBoxState.NormalReward;
                    }
                    else
                    {
                        state = TreasureBoxState.Empty;
                    }
                }
            }
            MessageCenter.Get<MSG.TREASURE_OPENBOX_UPDATE>().Dispatch();
            return true;
        }

        private void CalcReward(EventTreasureReward rConfig, List<RewardConfig> container, bool isShow = false)
        {
            var levelRate = 0;
            if (BoardViewWrapper.IsMainBoard())
            {
                levelRate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();
            }
            var rewardMan = Game.Manager.rewardMan;
            foreach (var r in rConfig.Reward)
            {
                var (cfgID, cfgCount, param) = r.ConvertToInt3();
                var (id, count) = rewardMan.CalcDynamicReward(cfgID, cfgCount, levelRate, 0, param);
                var reward = new RewardConfig()
                {
                    Id = id,
                    Count = count
                };
                if (reward.Id != 0)
                {
                    //如果宝藏里开出钥匙，需要实时增加，发奖到玩家，其余奖励进临时背包
                    if (reward.Id == ConfD.RequireCoinId)
                    {
                        if (!isShow)
                        {
                            var rewardCommit = rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.treasure_key_by_open_box);
                            rewardMan.CommitReward(rewardCommit);
                            if (rewardCommit.rewardId == ConfD.RequireCoinId)
                                DataTracker.token_change.Track(rewardCommit.rewardId, rewardCommit.rewardCount, keyNum, ReasonString.treasure_chest);
                        }
                    }
                    else if (reward.Id == ConfD.BonusToken && !isShow && ConfD.BonusToken != 0)
                    {
                        Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.treasure);
                        DataTracker.token_change.Track(reward.Id, reward.Count, keyNum, ReasonString.treasure_chest);
                    }
                    else
                    {
                        if (!isShow)
                        {
                            tempRewardBag.Add(reward);
                            DataTracker.treasure_bag_change.Track(1, reward.Id, reward.Count);
                        }
                    }
                    container.Add(reward);
                }
            }
            Game.Manager.archiveMan.SendImmediately(true);
        }

        /// <summary>
        /// 该宝箱是否开启过
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool HasOpen(int idx)
        {
            return (boxIdx & (1 << idx)) != 0;
        }

        public bool HasGetReward(int idx)
        {
            return (boxRewardIdx & (1 << idx)) != 0;
        }

        private void SaveRewardIndexOpened(int idx)
        {
            boxRewardIdx |= 1 << idx;
        }

        private void SaveBoxIndexOpened(int idx)
        {
            boxIdx |= 1 << idx;
        }

        /// <summary>
        /// 回到主棋盘领取临时背包里的奖励
        /// </summary>
        public void TryClaimTempBagReward()
        {
            tempRewardList.Clear();
            if (CanClaimBagReward)
            {
                foreach (var item in tempRewardBag)
                {
                    var data = Game.Manager.rewardMan.BeginReward(item.Id, item.Count, ReasonString.total_reward);
                    tempRewardList.Add(data);
                }
                tempRewardBag.Clear();
                UIManager.Instance.OpenWindow(UIConfig.UITotalRewardPanel, tempRewardList);
            }
        }

        public void TryExchangeExpireTreasureKey()
        {
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            using var _ = PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            map[ConfD.RequireCoinId] = keyNum;
            ActivityExpire.ConvertToReward(ConfD.ExpirePopup, list, ReasonString.treasure, token_:map);
            keyNum = 0;
            var ui = UIManager.Instance;
            if (ui.IsOpen(Res.ActiveR))
            {
                ui.RegisterIdleAction("ui_idle_treasure_exchange", 102, () => { ui.OpenWindow(EndRes.ActiveR, this, listT); });
            }
            else
            {
                Game.Manager.screenPopup.Queue(PopupEnd, listT);
            }
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(spawnBonusHandler);
        }

        public void RemoveRewardFromTempBag(int count)
        {
            tempRewardBag.RemoveRange(0, count);
            Game.Manager.archiveMan.SendImmediately(true);
        }

        /// <summary>
        /// 获取临时背包奖励
        /// </summary>
        /// <returns>奖励内容</returns>
        public List<RewardConfig> GetTempBagReward()
        {
            return tempRewardBag;
        }

        /// <summary>
        /// 获取当前钥匙数
        /// </summary>
        /// <returns>钥匙</returns>
        public int GetKeyNum()
        {
            return keyNum;
        }

        /// <summary>
        /// 获取积分
        /// </summary>
        /// <returns></returns>
        public int GetScore()
        {
            return score;
        }

        /// <summary>
        /// 根据权重随机一个宝箱id
        /// </summary>
        /// <param name="listR"></param>
        /// <returns></returns>
        private int RandomBoxRewardId(List<EventTreasureReward> listR)
        {
            var totalWeight = 0;
            using (ObjectPool<List<(int id, int weight)>>.GlobalPool.AllocStub(out var list))
            {
                foreach (var reward in listR)
                {
                    var w = reward.Weight;
                    totalWeight += w;
                    list.Add((reward.Id, w));
                }

                var roll = Random.Range(1, totalWeight);
                var weightSum = 0;
                foreach (var box in list)
                {
                    weightSum += box.weight;
                    if (weightSum >= roll)
                    {
                        return box.id;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// 每个关卡找到宝藏后数据刷新
        /// </summary>
        private void OnChangeLevel(List<RewardConfig> mileStoneRewards)
        {
            var groupConfig = GetCurrentTreasureGroup();
            currentLevelOpenCount = 0;
            currentLevelNoAppear = 0;
            currentLevelAppear = 0;
            currentLevelIndex += 1;
            boxIdx = 0;
            boxRewardIdx = 0;
            if (currentLevelIndex > groupConfig.IncludeLvId.Count - 1)
            {
                //已经完成了本关卡组内的所有关卡
                //发里程碑奖励
                var grpConfig = GetCurrentTreasureGroup();
                foreach (var r in grpConfig.MilestoneReward)
                {
                    var reward = r.ConvertToRewardConfig();
                    tempRewardBag.Add(reward);
                    DataTracker.treasure_bag_change.Track(1, reward.Id, reward.Count);
                    if (reward.Id == ConfD.RequireCoinId)
                    {
                        var rewardCommit =
                            Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.treasure);
                        DataTracker.token_change.Track(rewardCommit.rewardId, rewardCommit.rewardCount, keyNum, ReasonString.treasure_milestone);
                    }
                    else
                    {
                        mileStoneRewards.Add(reward);
                    }
                }
                DataTracker.event_treasure_milestone.Track(Id, Param, grpMappingConfig.IncludeGrpId[currentLevelGroupIndex], From, currentLevelGroupIndex + 1, groupConfig.IncludeLvId.Count, groupConfig.IncludeLvId.Count, grpMappingConfig.Diff, true);
                currentLevelIndex = 0;
                currentLevelGroupIndex += 1;
                Game.Manager.archiveMan.SendImmediately(true);
                if (currentLevelGroupIndex > grpMappingConfig.IncludeGrpId.Count - 1)
                {
                    //已经完成了所有关卡组 结束活动
                    //领取临时背包里的所有奖励 
                    MessageCenter.Get<MSG.ACTIVITY_SUCCESS>().Dispatch(this);
                }
            }
            else
            {
                DataTracker.event_treasure_milestone.Track(Id, Param, grpMappingConfig.IncludeGrpId[currentLevelGroupIndex], From, currentLevelGroupIndex + 1, groupConfig.IncludeLvId.Count, currentLevelIndex, grpMappingConfig.Diff, false);
            }

            if (HasNextLevelGroup())
                SetupLevel();
            MessageCenter.Get<MSG.TREASURE_LEVEL_UPDATE>().Dispatch();
        }

        /// <summary>
        /// 随机第几次出与不出次数
        /// </summary>
        private void RandomAppearProbability()
        {
            var level = GetCurrentTreasureLevel();
            if (currentLevelNoAppear == 0 && level.NoAppear.Count > 0)
                currentLevelNoAppear = Random.Range((int)level.NoAppear[0], (int)level.NoAppear[1]);
            if (currentLevelAppear == 0 && level.Appear.Count > 0)
                currentLevelAppear = Random.Range((int)level.Appear[0], (int)level.Appear[1]);
        }

        /// <summary>
        /// 初始化分数奖励
        /// </summary>
        private void SetupScoreReward()
        {
            var confR = grpMappingConfig.LevelReward;
            var confS = grpMappingConfig.LevelScore;
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

            foreach (var reward in grpMappingConfig.LevelReward)
            {
                rewardConfigList.Add(reward.ConvertToRewardConfig());
            }
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

        public RewardConfig GetScoreShowReward()
        {
            if (score >= scoreValueMax)
                return grpMappingConfig.CycleLevelReward.ConvertToRewardConfig();
            else
                return NormalScoreReward;
        }

        /// <summary>
        /// 寻宝入口展示分数
        /// </summary>
        /// <returns></returns>
        public (int, int) GetScoreShowNum()
        {
            //初始化 积分进度和积分奖励
            var goalScore = grpMappingConfig.CycleLevelScore;
            var mileStone = grpMappingConfig.LevelScore;
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

        #region Debug

        public int GetAppear()
        {
            return currentLevelAppear;
        }
        public int GetNoAppear()
        {
            return currentLevelNoAppear;
        }
        public int GetOpenCount()
        {
            return currentLevelOpenCount;
        }
        public int GetGroupIndex()
        {
            return currentLevelGroupIndex;
        }
        public int GetLevelIndex()
        {
            return currentLevelIndex;
        }
        public int GetScoreCycleCount()
        {
            return cycleScoreShowCount;
        }

        #endregion

        public int GetScoreMax()
        {
            return scoreValueMax;
        }

        /// <summary>
        /// 获取展示数据 当前处于第几关以及当前关卡组总关卡数
        /// </summary>
        /// <returns></returns>
        public (int, int) GetLevelInfo()
        {
            var levelCount = GetCurrentTreasureGroup().IncludeLvId.Count;
            return (currentLevelIndex, levelCount);
        }

        public void SetEnterGuideHasPopup()
        {
            isEnterHuntGuided = 1;
        }

        public void SetLeaveGuideHasPopup()
        {
            isLeaveHuntGuided = 1;
        }

        /// <summary>
        /// 进入寻宝界面是否弹出过引导弹窗
        /// </summary>
        /// <returns></returns>
        public bool IsEnterHuntGuided()
        {
            return isEnterHuntGuided > 0;
        }

        /// <summary>
        /// 回到主棋盘是否弹出过引导弹窗
        /// </summary>
        /// <returns></returns>
        public bool IsLeaveHuntGuided()
        {
            return isLeaveHuntGuided > 0;
        }

        /// <summary>
        /// 是否还有下一关卡组
        /// </summary>
        /// <returns></returns>
        public bool HasNextLevelGroup()
        {
            return currentLevelGroupIndex <= grpMappingConfig.IncludeGrpId.Count - 1;
        }

        public void SetCycleScoreCount()
        {
            cycleScoreShowCount += 1;
        }

        public ListActivity.IEntrySetup SetupEntry(ListActivity.Entry e_)
        {
            e_.dot.SetActive(GetKeyNum() > 0);
            e_.dotCount.gameObject.SetActive(GetKeyNum() > 0);
            e_.dotCount.SetRedPoint(GetKeyNum());
            return null;
        }

        public RewardCommitData TryGetCommitReward(RewardConfig reward)
        {
            RewardCommitData rewardCommitData = null;
            foreach (var commitData in scoreCommitRewardList)
            {
                if (commitData.rewardId == reward.Id && commitData.rewardCount == reward.Count)
                {
                    rewardCommitData = commitData;
                    scoreCommitRewardList.Remove(commitData);
                    break;
                }
            }
            return rewardCommitData;
        }

        public RewardConfig GetCycleReward()
        {
            return grpMappingConfig.CycleLevelReward.ConvertToRewardConfig();
        }

        public int GetCycleScore()
        {
            return grpMappingConfig.CycleLevelScore;
        }

        public string BoardEntryAsset()
        {
            Visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        public bool BoardEntryVisible => HasNextLevelGroup();
    }
}