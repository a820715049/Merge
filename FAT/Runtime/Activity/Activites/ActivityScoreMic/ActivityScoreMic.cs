/*
 * @Author: tang.yan
 * @Description: 积分活动-麦克风版(积分活动的变种，把积分直接挂在棋子上，让玩家直观地看到积分来源)
 * @Doc: https://centurygames.feishu.cn/wiki/FCr6wUVEZiwH77kZn6pcjmTxn1g
 * @Date: 2025-09-02 14:09:55
 */

using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using EL;
using FAT.Merge;
using System.Collections.Generic;
using Config;

namespace FAT
{
    public class ActivityScoreMic : ActivityLike, IBoardEntry, IActivityOrderHandler
    {
        public override bool Valid => Lite.Valid && Conf != null;
        public MicMilestone Conf { get; private set; }

        #region 活动基础

        //用户分层 对应MicMilestoneDetail.id
        private int _detailId;

        //外部调用需判空
        public MicMilestoneDetail GetCurDetailConfig()
        {
            return Game.Manager.configMan.GetMicMilestoneDetailConfig(_detailId);
        }

        public ActivityScoreMic(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = Game.Manager.configMan.GetMicMilestoneConfig(lite_.Param);
        }

        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            _detailId = Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.EventGroup);
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新订单产出积分模块
            _RefreshScoreEntity();
            //刷新泡泡棋子和商店棋子的积分范围转换信息
            _RefreshAllRangeInfo();
            //刷新产棋子回调模块
            _RefreshSpawnBonusHandler();
            //刷新棋子销毁回调模块
            _RefreshDisposeBonusHandler();
            // 活动弹板
            Game.Manager.screenPopup.TryQueue(MainPopup.popup, (PopupType)(-1), false);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, _detailId));
            any.Add(ToRecord(1, TotalScore));
            any.Add(ToRecord(2, CurMilestoneLevel));
            any.Add(ToRecord(3, CurMilestoneNum));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            _detailId = ReadInt(0, any);
            TotalScore = ReadInt(1, any);
            CurMilestoneLevel = ReadInt(2, any);
            CurMilestoneNum = ReadInt(3, any);
            
            LastMilestoneLevel = CurMilestoneLevel;
            LastMilestoneNum = CurMilestoneNum;
            
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新订单产出积分模块
            _RefreshScoreEntity();
            //刷新泡泡棋子和商店棋子的积分范围转换信息
            _RefreshAllRangeInfo();
            //刷新产棋子回调模块
            _RefreshSpawnBonusHandler();
            //刷新棋子销毁回调模块
            _RefreshDisposeBonusHandler();
        }
        
        public override void WhenReset()
        {
            //清理scoreEntity
            _ClearScoreEntity();
            _hasLevelUp = false;
            _commitRewardList.Clear();
        }

        public override void WhenEnd()
        {
            //清理产棋子回调模块
            _ClearSpawnBonusHandler();
            //清理棋子销毁回调模块
            _ClearDisposeBonusHandler();
            //遍历主棋盘+所有类主棋盘，将其中所有的挂载了ItemActivityTokenComponent的棋子的左下角积分清0，并与本活动解绑
            Game.Manager.mergeBoardMan.WalkAllWorldItem((item) =>
            {
                if (item != null && item.TryGetItemComponent<ItemActivityTokenComponent>(out var comp))
                {
                    comp.ClearActivityInfo_BL();
                }
            }, true, MergeWorld.WalkItemMask.NoRewardList);
            //回收逻辑
            if (Conf != null)
            {
                //回收主棋盘上可能存在的积分双倍buff棋子
                var convertReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> convertRewardList);
                ActivityExpire.ConvertToReward(Conf.ExpireItem, convertRewardList, ReasonString.score_mic_convert);
                //弹活动结算界面，convertReward中可能会没有奖励
                Game.Manager.screenPopup.TryQueue(SettlePopup.popup, PopupType.Login, convertReward);
            }
            //清理scoreEntity
            _ClearScoreEntity();
            //保底逻辑 commit
            _TryCommitReward();
        }

        #region 界面 入口 换皮 弹脸

        public override ActivityVisual Visual => MainPopup.visual;
        public VisualPopup MainPopup { get; } = new(UIConfig.UIScore_Mic); // 主界面
        public VisualPopup SettlePopup { get; } = new(UIConfig.UIScoreConvert_Mic); //活动结算界面

        public override void Open() => Open(MainPopup);

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            MainPopup.Setup(Conf.EventMainTheme, this);
            //此弹窗可以在处于和主棋盘等价的活动棋盘上时，无视delay限制，直接弹出
            MainPopup.popup.option = new IScreenPopup.Option() { ignoreDelay = true };
            SettlePopup.Setup(Conf.EventSettleTheme, this, false, false);
        }

        string IBoardEntry.BoardEntryAsset()
        {
            MainPopup.visual.AssetMap.TryGetValue("boardEntry", out var key);
            return key;
        }

        private static string scoreKey = "score";
        private static string scoreMultiKey = "scoreMulti";
        public string GetScoreTextStyleKey(bool isMulti)
        {
            return isMulti ? scoreMultiKey : scoreKey;
        }

        #endregion

        #endregion

        #region 积分相关逻辑
        
        public int TotalScore { get; private set; } = 0;    //当前总分数
        public int CurMilestoneLevel { get; private set; } = 0; //当前里程碑所处等级 从0开始 根据阶段值读配置获取当前的最大进度以及达成后可获得的奖励
        public int CurMilestoneNum { get; private set; } = 0;   //当前里程碑的进度值
        public int LastMilestoneLevel { get; private set; } = 0;    //上次界面展示的里程碑等级
        public int LastMilestoneNum { get; private set; } = 0;  //上次界面展示的里程碑进度值
        
        //界面打开时才调用获取升级奖励list，保证拿到最新的奖励列表，拿到后列表clear
        public PoolMapping.Ref<List<RewardCommitData>> PopCommitDataList()
        {
            var listT = PoolMapping.PoolMappingAccess.Take<List<RewardCommitData>>(out var list);
            list.AddRange(_commitRewardList);
            _commitRewardList.Clear();
            return listT;
        }
        
        //主界面关闭时调用，帮界面记录一下上次打开时显示的里程碑信息
        public void OnMainUIClose()
        {
            LastMilestoneLevel = CurMilestoneLevel;
            LastMilestoneNum = CurMilestoneNum;
        }

        //进度条动画播完，或者切换到其他场景导致入口隐藏时，主动调用
        public void TryPopupLevelUp()
        {
            //活动结束时return
            if (!Active)
                return;
            //检查是否有升级 如果有则弹窗，弹窗打开时，自行获取升级获得的奖励，默认每次升级最多只会有一个RewardCommitData
            if (_hasLevelUp)
            {
                Game.Manager.screenPopup.TryQueue(MainPopup.popup, (PopupType)(-1));
            }
            _hasLevelUp = false;
        }
        
        //主界面关闭时调用，检查一下里程碑是否满级 若满级则活动直接结束
        public void CheckCanEnd()
        {
            //活动结束时return
            if (!Active)
                return;
            if (IsComplete())
            {
                Game.Manager.activity.EndImmediate(this, false);
            }
        }

        //保底逻辑 避免因为主界面弹窗因为种种原因（如想弹的时候发现活动结束了）没弹出来，导致没有commit
        private void _TryCommitReward()
        {
            if (_commitRewardList.Count > 0)
            {
                foreach (var commitData in _commitRewardList)
                {
                    Game.Manager.rewardMan.CommitReward(commitData);
                }
                _commitRewardList.Clear();
            }
        }

        #region 积分增加逻辑

        public void TryAddToken(int id, int num, ReasonString reason)
        {
            if (Conf == null || id <= 0 || num <= 0)
                return;
            //积分不符合时return
            var rate = 1;
            if (Conf.Token != id && !Conf.ExtraToken.TryGetValue(id, out rate)) 
                return;
            //积分转换 注意这里不需要再考虑翻倍了，外部发积分时就已经考虑完了，num就是最终值
            var transNum = rate * num;
            if (!_TryAddScore(transNum)) 
                return;
            if (reason != ReasonString.score_mic_order)
            {
                //非完成订单获得货币时 单独向ScoreEntity中同步最新分数  并单独打点token_change
                _scoreEntity.UpdateScore(TotalScore);
                DataTracker.token_change.Track(id, num, TotalScore, reason);
            }
            //打点
            var isMulti = CheckTokenMultiRate(id, out _);
            DataTracker.event_mic_token.Track(this, Conf.Token, reason, true, transNum, isMulti);
            //更新完分数后，再更新里程碑信息
            _UpdateMilestoneInfo(transNum);
        }

        private bool _TryAddScore(int addNum)
        {
            if (addNum > 0)
            {
                var oldScore = TotalScore;
                TotalScore += addNum;
                MessageCenter.Get<MSG.SCORE_MIC_NUM_ADD>().Dispatch(oldScore, TotalScore);
                return true;
            }
            return false;
        }

        #endregion

        #region 主棋盘订单左下角积分

        private ScoreEntity _scoreEntity;

        private void _RefreshScoreEntity()
        {
            if (Conf == null)
                return;
            var conf = GetCurDetailConfig();
            if (conf != null)
            {
                //需要时才new
                _scoreEntity ??= new ScoreEntity();
                //无需监听_scoreEntity内部的SCORE_ENTITY_ADD_COMPLETE消息
                //因为所有积分发放最后都会走BeginReward, 而本活动内部处理了积分变化的逻辑 
                _scoreEntity.Setup(TotalScore, this, Conf.Token, Conf.ExtraScore,
                    ReasonString.score_mic_order, "", Constant.MainBoardId, true, ScoreEntity.ScoreType.OnlyOrder);
            }
        }

        private void _ClearScoreEntity()
        {
            //活动结束时清理_scoreEntity
            _scoreEntity?.Clear();
            _scoreEntity = null;
        }

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            //积分类活动在计算订单积分时，排除非随机订单，排除IsExtraScore为false的订单
            if (order == null || order.ConfRandomer == null || !order.ConfRandomer.IsExtraScore)
            {
                return false;
            }
            var changed = false;
            var state = order.GetState((int)OrderParamType.ScoreEventId);
            // 没有积分 or 不是同一期活动时给这个订单生成左下角积分
            if (state == null || state.Value != Id)
            {
                changed = true;
                _scoreEntity.CalcOrderScore(order, tracer);
            }

            return changed;
        }

        #endregion

        #region 里程碑逻辑
        
        //获取指定等级的里程碑信息，milestoneLevel从0开始
        //若传入等级<0或>最大等级，会返回null，因此外部调用需判空
        public MicMilestoneGroup GetMilestoneInfo(int milestoneLevel)
        {
            var allMilestoneInfo = GetCurDetailConfig()?.MilestoneGroup;
            if (allMilestoneInfo == null)
                return null;
            if (allMilestoneInfo.TryGetByIndex(milestoneLevel, out var infoId))
            {
                return Game.Manager.configMan.GetMicMilestoneGroupConfig(infoId);
            }
            return null;
        }

        //获取当前等级对应的里程碑信息
        public MicMilestoneGroup GetCurMilestoneInfo()
        {
            return GetMilestoneInfo(CurMilestoneLevel);
        }
        
        public int GetCurMilestoneNumMax(int curMilestoneLevel)
        {
            var ids = GetCurDetailConfig().MilestoneGroup;
            if (ids.Count <= curMilestoneLevel) return 0;
            var id = ids[curMilestoneLevel];
            return Game.Manager.configMan.GetMicMilestoneGroupConfig(id).MilestoneScore;
        }

        public bool IsComplete()
        {
            var curGroupConf = GetCurDetailConfig();
            var allMilestoneInfo = curGroupConf.MilestoneGroup;
            var totalCount = allMilestoneInfo.Count;
            return CurMilestoneLevel >= totalCount;
        }

        //用于支持UI表现
        private bool _hasLevelUp;
        private List<RewardCommitData> _commitRewardList = new();

        //每次加分结束后，更新里程碑信息
        private void _UpdateMilestoneInfo(int scoreNum)
        {
            //获取当前阶段对应的进度信息 拿不到时认为数据非法或者已经满级
            var curMilestoneInfo = GetMilestoneInfo(CurMilestoneLevel);
            if (curMilestoneInfo == null)
                return;
            //增加进度值
            var finalMilestoneNum = CurMilestoneNum + scoreNum;
            //检测是否达到本阶段最大值
            var curMilestoneMax = curMilestoneInfo.MilestoneScore;
            //未达到最大值 return
            if (finalMilestoneNum < curMilestoneMax)
            {
                CurMilestoneNum = finalMilestoneNum;
                return;
            }
            //达到最大值时就处理里程碑升级逻辑，可能会一下升很多级
            var curGroupConf = GetCurDetailConfig();
            var allMilestoneInfo = curGroupConf.MilestoneGroup;
            var totalCount = allMilestoneInfo.Count;
            //构造发奖列表 最后传给界面
            do
            {
                //每次循环都拿到当前对应的里程碑配置信息 若拿不到配置 则认为满级 break
                var tempMilestoneInfo = GetMilestoneInfo(CurMilestoneLevel);
                if (tempMilestoneInfo == null)
                    break;
                var tempMilestoneMax = tempMilestoneInfo.MilestoneScore;
                if (finalMilestoneNum < tempMilestoneMax)
                    break;
                finalMilestoneNum -= tempMilestoneMax;   //此时finalMilestoneNum代表多余的进度值
                //递进 里程碑升级
                CurMilestoneLevel++;
                CurMilestoneNum = finalMilestoneNum;
                _hasLevelUp = true;
                //发奖
                var reward = tempMilestoneInfo.MilestoneReward.ConvertToRewardConfig();
                if (reward != null)
                {
                    var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.score_mic);
                    _commitRewardList.Add(commit);
                }
                //里程碑升级时打点
                DataTracker.event_mic_milestone.Track(this, CurMilestoneLevel, totalCount, curGroupConf.Diff, CurMilestoneLevel == totalCount);
            }
            while(true);
            //处理一下里程碑升级时表现层无法做表现，从而无法调起TryPopupLevelUp的情况
            if (_hasLevelUp)
            {
                var mapActive = Game.Manager.mapSceneMan.scene.Active;
                var curWorld  = Game.Manager.mergeBoardMan.activeWorld;
                var board     = curWorld?.activeBoard;
                // 规则：在 meta 场景下直接弹；不在 meta 时，且存在棋盘，且该棋盘不是主棋盘，且其世界等价于主世界 -> 弹
                bool canPopup = mapActive || (board != null 
                                              && board.boardId != Constant.MainBoardId 
                                              && curWorld.isEquivalentToMain);
                if (canPopup)
                    TryPopupLevelUp();
            }
        }

        #endregion

        #endregion

        #region 棋盘相关逻辑

        #region ScoreMicSpawnBonusHandler

        private ScoreMicSpawnBonusHandler spawnBonusHandler;

        private void _RefreshSpawnBonusHandler()
        {
            spawnBonusHandler ??= new ScoreMicSpawnBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(spawnBonusHandler);
        }
        
        private void _ClearSpawnBonusHandler()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(spawnBonusHandler);
            spawnBonusHandler = null;
        }

        #endregion
        
        #region ScoreMicDisposeBonusHandler

        private ScoreMicDisposeBonusHandler disposeBonusHandler;

        private void _RefreshDisposeBonusHandler()
        {
            disposeBonusHandler ??= new ScoreMicDisposeBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalDisposeBonusHandler(disposeBonusHandler);
        }
        
        private void _ClearDisposeBonusHandler()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalDisposeBonusHandler(disposeBonusHandler);
            disposeBonusHandler = null;
        }

        #endregion

        //检查传入的tokenId在目前World下是否可被翻倍，同时返回对应倍率
        public bool CheckTokenMultiRate(int tokenId, out int rate)
        {
            rate = 1;
            var curWorld = BoardViewManager.Instance.world;
            if (curWorld == null)
                return false;
            var hasActiveTokenMulti = curWorld.tokenMulti.hasActiveTokenMulti;
            if (hasActiveTokenMulti)
            {
                var activeItem = curWorld.activeBoard.FindItemById(curWorld.tokenMulti.activeTokenMultiId);
                var tokenMultiComp = activeItem?.GetItemComponent<ItemTokenMultiComponent>();
                rate = tokenMultiComp?.config?.TokenMultiplier ?? 1;
                return ItemUtility.CheckTokenCanMulti(tokenMultiComp, tokenId);
            }
            return false;
        }
        
        //检查主棋盘中的token翻倍情况
        //目前仅用于商店棋子使用
        public bool CheckMainBoardTokenMultiRate(int tokenId, out int rate)
        {
            rate = 1;
            var curWorld = Game.Manager.mainMergeMan.world;
            if (curWorld == null)
                return false;
            var hasActiveTokenMulti = curWorld.tokenMulti.hasActiveTokenMulti;
            if (hasActiveTokenMulti)
            {
                var activeItem = curWorld.activeBoard.FindItemById(curWorld.tokenMulti.activeTokenMultiId);
                var tokenMultiComp = activeItem?.GetItemComponent<ItemTokenMultiComponent>();
                rate = tokenMultiComp?.config?.TokenMultiplier ?? 1;
                return ItemUtility.CheckTokenCanMulti(tokenMultiComp, tokenId);
            }
            return false;
        }
        
        #endregion

        #region 泡泡棋子|商店棋子 相关逻辑
        
        //获取指定泡泡棋子
        public int GetTokenIdForBubbleItem(Item item)
        {
            if (!Active || _scoreEntity == null || item == null)
                return 0;
            var originScore = item.config?.BubblePrice ?? 0;
            var finalScore = _scoreEntity.GetCalcScoreByType(ScoreEntity.ScoreType.Bubble, originScore);
            foreach (var rangeInfo in _bubbleScoreRange)
            {
                if (finalScore >= rangeInfo.Value.Min && finalScore <= rangeInfo.Value.Max)
                    return rangeInfo.Key;
            }
            return 0;
        }
        
        //获取指定商店售卖棋子根据其原始分数算出来的要挂载的TokenId
        public int GetTokenIdForShopItem(ShopChessOrderData shopChessOrderData)
        {
            if (!Active || _scoreEntity == null || shopChessOrderData == null)
                return 0;
            var originScore = shopChessOrderData.CurSellGoodsPrice;
            var finalScore = _scoreEntity.GetCalcScoreByType(ScoreEntity.ScoreType.Bubble, originScore);
            foreach (var rangeInfo in _shopScoreRange)
            {
                if (finalScore >= rangeInfo.Value.Min && finalScore <= rangeInfo.Value.Max)
                    return rangeInfo.Key;
            }
            return 0;
        }
        
        //尝试在泡泡上挂积分时，先计算概率，通过后再挂
        public bool CheckCanSpawnOnBubble()
        {
            if (!Active || _scoreEntity == null)
                return false;
            var bubbleRate = GetCurDetailConfig()?.BubbleRate ?? 0;
            var rate = UnityEngine.Random.Range(0f, 100f);
            var isBingo = rate < bubbleRate;
            DebugEx.FormatInfo("ActivityScoreMic.CheckCanSpawnOnBubble : bubbleRate = {0}, realRate = {1}, isSpawn = {2}", bubbleRate, rate, isBingo);
            return isBingo;
        }
        
        private Dictionary<int, IntRangeConfig> _bubbleScoreRange = new Dictionary<int, IntRangeConfig>();
        private Dictionary<int, IntRangeConfig> _shopScoreRange = new Dictionary<int, IntRangeConfig>();

        //刷新泡泡棋子和商店棋子的积分范围转换信息
        private void _RefreshAllRangeInfo()
        {
            _bubbleScoreRange.Clear();
            _shopScoreRange.Clear();
            if (!Valid)
                return;
            var curDetailConfig = GetCurDetailConfig();
            if (curDetailConfig == null)
                return;
            _RefreshRangeInfo(_bubbleScoreRange, curDetailConfig.BubbleScore);
            _RefreshRangeInfo(_shopScoreRange, curDetailConfig.StoreScore);
        }

        private void _RefreshRangeInfo(IDictionary<int, IntRangeConfig> dict, IEnumerable<string> conf)
        {
            foreach (var info in conf)
            {
                var r = info.ConvertToInt3();
                if (r.Item3 > 0)
                {
                    var rangeInfo = new IntRangeConfig();
                    rangeInfo.Min = r.Item1;
                    rangeInfo.Max = r.Item2 < 0 ? int.MaxValue : r.Item2;
                    dict.Add(r.Item3, rangeInfo);
                }
            }
        }

        #endregion
    }
}