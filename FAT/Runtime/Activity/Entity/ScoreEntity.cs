/*
 * @Author: pengjian.zhang
 * @Description: 额外积分功能（积分来源与积分计算）
 * @Date: 2024-07-02 17:36:28
 */

using System;
using System.Collections.Generic;
using EL;
using FAT.Merge;
using fat.rawdata;
using UnityEngine;
using static FAT.RecordStateHelper;
using Cysharp.Text;

namespace FAT
{
    public class ScoreEntity
    {
        public enum ScoreType
        {
            Shop,
            OrderLeft,
            OrderRight,
            Bubble,
            Merge,
            Joker,
        }
        
        public class ScoreFlyRewardData
        {
            public int rewardId;
            public int rewardCount;
        }
        
        private int RequireCoinId;
        private int BoardId;
        private int EventExtraScoreId;
        private ActivityLike Activity;
        private ReasonString ReasonString;
        private string MergeScorePrefab; //棋子合并后往上飘的积分prefab
        private bool NeedFlyCenter; //订单完成时是否需要积分飞向屏幕中心的表现
        private int Score;
        private int PrevScore;
        private ScoreMergeBonusHandler mergeHandler;
        private ScoreFlyRewardData orderScoreReward;
        private ScoreFlyRewardData orderScoreRewardBR;
        private List<RoundScore> roundScoreList = new();
        private int roundScoreVisitor(int idx) { return roundScoreList[idx].From; }
        private bool willCheckComplete = false;

        // 注意：这里注册的活动积分，需要明确：
        // 1.是否仅包含ScoreEntity逻辑范畴内的来源，如完成订单、合成、购买商店棋子等行为
        // 2.如果除了以上行为外还有其他来源，如直接购买获得或其他行为直接发放，这种情况需要活动内部进一步甄别
        public void Setup(int score, ActivityLike activity, int requireCoinId, int eventExtraScoreId, ReasonString r, string mergeScorePrefab, int boardId = 0, bool needFlyCenter = true)
        {
            Score = score;
            Activity = activity;
            RequireCoinId = requireCoinId;
            BoardId = boardId;
            EventExtraScoreId = eventExtraScoreId;
            ReasonString = r;
            MergeScorePrefab = mergeScorePrefab;
            NeedFlyCenter = needFlyCenter;
            MessageCenter.Get<MSG.ON_USE_JOKER_ITEM_UPGRADE>().AddListener(OnUseJokerItemTryAddScore);
            MessageCenter.Get<MSG.ON_USE_SPEED_UP_ITEM>().AddListener(OnUseSpeedUpItemTryAddScore);
            MessageCenter.Get<MSG.ON_BUY_SHOP_ITEM>().AddListener(OnBuyShopItemTryAddScore);
            MessageCenter.Get<MSG.ON_COMMIT_ORDER>().AddListener(OnCommitOrderTryAddScore);
            MessageCenter.Get<MSG.ON_COMMIT_ORDER_BR>().AddListener(OnCommitOrderTryAddScoreBR);
            MessageCenter.Get<MSG.ON_MERGE_HAS_SCORE_ITEM>().AddListener(OnMergeItemTryAddScore);
            MessageCenter.Get<MSG.ORDER_SCORE_ANIM_COMPLETE>().AddListener(OnCommitOrderAnimComplete);
            SetupBonusHandler();
            //填充积分取整配置
            roundScoreList.AddRange(Game.Manager.configMan.GetRoundScoreConfig());
        }

        public void Clear()
        {
            RequireCoinId = 0;
            BoardId = 0;
            MessageCenter.Get<MSG.ON_USE_JOKER_ITEM_UPGRADE>().RemoveListener(OnUseJokerItemTryAddScore);
            MessageCenter.Get<MSG.ON_USE_SPEED_UP_ITEM>().RemoveListener(OnUseSpeedUpItemTryAddScore);
            MessageCenter.Get<MSG.ON_BUY_SHOP_ITEM>().RemoveListener(OnBuyShopItemTryAddScore);
            MessageCenter.Get<MSG.ON_COMMIT_ORDER>().RemoveListener(OnCommitOrderTryAddScore);
            MessageCenter.Get<MSG.ON_COMMIT_ORDER_BR>().RemoveListener(OnCommitOrderTryAddScoreBR);
            MessageCenter.Get<MSG.ON_MERGE_HAS_SCORE_ITEM>().RemoveListener(OnMergeItemTryAddScore);
            MessageCenter.Get<MSG.ORDER_SCORE_ANIM_COMPLETE>().RemoveListener(OnCommitOrderAnimComplete);
            ClearBonusHandler();
            roundScoreList.Clear();
        }

        private void SetupBonusHandler()
        {
            var eventScoreConfig = Game.Manager.configMan.GetEventExtraScoreConfig(EventExtraScoreId);
            if (eventScoreConfig != null && eventScoreConfig.MergeFactor)
            {
                return;
            }
            if (mergeHandler == null)
            {
                mergeHandler = new ScoreMergeBonusHandler();
                Game.Manager.mergeBoardMan.RegisterGlobalMergeBonusHandler(mergeHandler);
            }
        }

        private void ClearBonusHandler()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalMergeBonusHandler(mergeHandler);
        }
        
        private void OnUseJokerItemTryAddScore(Item item, int score)
        {
            if (Activity is IActivityComplete c && !c.IsActive) return;
            var r = AddScore(ScoreType.Joker, score);
            if (r != null)
                MessageCenter.Get<MSG.BOARD_FLY_SCORE>().Dispatch((item, r, MergeScorePrefab));
        }
        
        private void OnUseSpeedUpItemTryAddScore(Item item, int score)
        {
            if (Activity is IActivityComplete c && !c.IsActive) return;
            var r = AddScore(ScoreType.Bubble, score);
            if (r != null)
                MessageCenter.Get<MSG.BOARD_FLY_SCORE>().Dispatch((item, r, MergeScorePrefab));
        }       
        
        private void OnMergeItemTryAddScore(Item item, int score)
        {
            if (Activity is IActivityComplete c && !c.IsActive) return;
            var r = AddScore(ScoreType.Merge, score);
            if (r != null)
                MessageCenter.Get<MSG.BOARD_FLY_SCORE>().Dispatch((item, r, MergeScorePrefab));
        }
        
        private void OnBuyShopItemTryAddScore(int price, int boardId)
        {
            if (Activity is IActivityComplete c && !c.IsActive) return;
            if (boardId != BoardId) return;
            var r = AddScore(ScoreType.Shop, price);
            if (r != null)
                MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_SHOP>().Dispatch(PrevScore, Score);
        }

        private void OnCommitOrderTryAddScore(int score)
        {
            if (!NeedFlyCenter) return;
            if (Activity is IActivityComplete c && !c.IsActive) return;
            orderScoreReward = AddScore(ScoreType.OrderLeft, score);
        }

        private void OnCommitOrderTryAddScoreBR(int score)
        {
            if (NeedFlyCenter) return;
            if (Activity is IActivityComplete c && !c.IsActive) return;
            orderScoreRewardBR = AddScore(ScoreType.OrderRight, score);
        }
        
        private void OnCommitOrderAnimComplete(Vector3 from, bool isBottomLeft)
        {
            if (!isBottomLeft && orderScoreRewardBR != null)
            {
                TryFlyOrderScore(orderScoreRewardBR, from);
                orderScoreRewardBR = null;
            }
            if (isBottomLeft && orderScoreReward != null)
            {
                TryFlyOrderScore(orderScoreReward, from);
                orderScoreReward = null;
            }
        }

        private void TryFlyOrderScore(ScoreFlyRewardData r, Vector3 from)
        {
            if (r != null)
            {
                var rewardMgr = Game.Manager.rewardMan;
                var commitData = rewardMgr.BeginReward(r.rewardId, r.rewardCount, ReasonString);
                //需要积分飞向屏幕中心的表现
                if (NeedFlyCenter)
                {
                    rewardMgr.CommitReward(commitData);
                    MessageCenter.Get<MSG.SCORE_FLY_REWARD_CENTER>().Dispatch((from, commitData, Activity));
                }
                //不需要飞向中心时 直接飞
                else
                {
                    UIFlyUtility.FlyReward(commitData, from);
                }
            }
        }
        
        public void UpdateScore(int score)
        {
            Score = score;
        }

        /// <summary>
        /// 添加积分
        /// </summary>
        /// <param name="type">来源</param>
        /// <param name="param">初始分数</param>
        /// <returns></returns>
        public ScoreFlyRewardData AddScore(ScoreType type, int param)
        {
            var activeWorld = Game.Manager.mergeBoardMan.activeWorld;
            //商店棋盘有自己的归属棋盘逻辑
            if (type != ScoreType.Shop && activeWorld != null && BoardId != 0)
            {
                //说明在棋盘内
                var curBoardId = activeWorld.activeBoard.boardId;
                if (curBoardId != BoardId)
                {
                    DebugEx.FormatError("[ScoreEntity.AddScore]: ActiveBoardId != ScoreConfigId But TryAddScore, activeBoardId = {0}, eventScoreConfigBoardId = {1}", curBoardId, BoardId);
                    return null;
                }
            }

            var score = CalculateScoreByType(type, param);
            //经过公式计算 要加的分 可能为0
            if (score <= 0)
            {
                return null;
            }

            ScoreFlyRewardData data = new();
            data.rewardId = RequireCoinId;
            data.rewardCount = score;
            
            PrevScore = Score;
            Score += score;
            MessageCenter.Get<MSG.SCORE_ENTITY_ADD_COMPLETE>().Dispatch((PrevScore, Score, RequireCoinId));
            //策划要求 万能棋子得分归于 Merge type
            if (type == ScoreType.Joker) type = ScoreType.Merge;
            var r = ZString.Concat(ReasonString, "_", type.ToString().ToLower());
            var reason = new ReasonString(r);
            DataTracker.token_change.Track(RequireCoinId, score, Score, reason);
            
            //活动已通关
            if (willCheckComplete)
            {
                return null;
            }
            if (Activity is IActivityComplete c && c.HasComplete())
            {
                willCheckComplete = true;
            }
            return data;
        }
        
        private int CalculateScoreByType(ScoreType type, int score)
        {
            //策划不配代表不支持此积分来源
            var eventScoreConfig = Game.Manager.configMan.GetEventExtraScoreConfig(EventExtraScoreId);
            if (eventScoreConfig == null) return score;
            var finalScore = 1.0f * score;
            if (type == ScoreType.Bubble)
            {
                if (eventScoreConfig.BuyBubbleFactor == 0)
                    return 0;
                //bubble积分数=泡泡钻数/{系数}*10，除不尽则向上取整
                finalScore = finalScore / eventScoreConfig.BuyBubbleFactor * 10;
            }
            else if (type == ScoreType.Shop)
            {
                if (eventScoreConfig.BuyMarketFactor == 0)
                    return 0;
                //商店积分数=通过priceRate计算后的数值/{系数}*10，除不尽则向上取整
                finalScore = finalScore / eventScoreConfig.BuyMarketFactor * 10;
            }
            else if (type == ScoreType.Merge || type == ScoreType.Joker)
            {
                //不支持合成获得积分时 返回0
                if (eventScoreConfig.MergeFactor)
                    return 0;
                //活动期间，在主棋盘内每次合成一次棋子，用户自动获得对应棋子配置的合成积分值
                //活动期间，在主棋盘内使用一次万能卡升级棋子，用户自动获得升级后对应棋子配置的合成积分值
                finalScore = score;
            }
            return Mathf.CeilToInt(finalScore);
        }

        //计算订单左下角的积分
        public void CalcOrderScore(OrderData order, MergeWorldTracer tracer)
        {
            var scoreTotal = 0;
            var eventScoreConfig = Game.Manager.configMan.GetEventExtraScoreConfig(EventExtraScoreId);
            if (eventScoreConfig.MinDiffRate != 0 && eventScoreConfig.OrderScoreFactor != 0)
            {
                foreach (var item in order.Requires)
                {
                    var (realDffy, accDffy) = OrderUtility.CalcDifficultyForItem(item.Id, tracer);
                    var payDffy = realDffy - accDffy;
                    var score = CalculateOrderScore(realDffy, (int)(payDffy * 100.0f / realDffy));
                    DebugEx.Info($"calc score {score} x {item.TargetCount} | {realDffy} {accDffy} {payDffy}");
                    scoreTotal += score * item.TargetCount;
                }
            }
            UpdateRecord((int)OrderParamType.ScoreEventId, Activity.Id, order.Record.Extra);
            UpdateRecord((int)OrderParamType.Score, scoreTotal, order.Record.Extra);
        }
        
        //计算订单右下角的积分
        public void CalcOrderScoreBR(OrderData order, MergeWorldTracer tracer)
        {
            var scoreTotal = 0;
            var eventScoreConfig = Game.Manager.configMan.GetEventExtraScoreConfig(EventExtraScoreId);
            if (eventScoreConfig.MinDiffRate != 0 && eventScoreConfig.OrderLowerRightScore != 0)
            {
                foreach (var item in order.Requires)
                {
                    var (realDffy, accDffy) = OrderUtility.CalcDifficultyForItem(item.Id, tracer);
                    var payDffy = realDffy - accDffy;
                    var score = CalculateOrderScoreBR(realDffy, (int)(payDffy * 100.0f / realDffy));
                    DebugEx.Info($"calc score {score} x {item.TargetCount} | {realDffy} {accDffy} {payDffy}");
                    scoreTotal += score * item.TargetCount;
                }
            }
            UpdateRecord((int)OrderParamType.ScoreEventIdBR, Activity.Id, order.Record.Extra);
            UpdateRecord((int)OrderParamType.ScoreBR, scoreTotal, order.Record.Extra);
        }
        
        private int CalculateOrderScore(int diff, int rate)
        {
            var eventScoreConfig = Game.Manager.configMan.GetEventExtraScoreConfig(EventExtraScoreId);
            rate = rate < eventScoreConfig.MinDiffRate ? eventScoreConfig.MinDiffRate : rate;
            var factor = eventScoreConfig.OrderScoreFactor;
            //单棋子积分公式=订单积分系数/100*(实际难度/100)*付出与实际难度比%（RoundScore向上取）；
            var orderScore = 1.0f * factor / 100 * (1.0f * diff / 100) * 1.0f * rate / 100;
            var finalOrderScore = RoundScore(orderScore);
            return finalOrderScore;
        }
        
        private int CalculateOrderScoreBR(int diff, int rate)
        {
            var eventScoreConfig = Game.Manager.configMan.GetEventExtraScoreConfig(EventExtraScoreId);
            rate = rate < eventScoreConfig.MinDiffRate ? eventScoreConfig.MinDiffRate : rate;
            var factor = eventScoreConfig.OrderLowerRightScore;
            //单棋子积分公式=订单积分系数/100*(实际难度/100)*付出与实际难度比%
            var orderScore = 1.0f * factor / 100 * (1.0f * diff / 100) * 1.0f * rate / 100;
            //向下取整 小于1时取1
            var finalOrderScore = Mathf.FloorToInt(orderScore);
            if (finalOrderScore < 1)
                finalOrderScore = 1;
            return finalOrderScore;
        }
        
        private int RoundScore(float score)
        {
            var idx = FindBoundIndex(score, roundScoreList.Count, roundScoreVisitor);
            if (idx < 0)
            {
                DebugEx.Error($"ScoreEntity::RoundScore fail to round by {score}");
                return 1;
            }
            var roundBy = roundScoreList[idx].RoundBy;
            var ret = Mathf.RoundToInt(score / roundBy) * roundBy;
            if (ret < 1)
                ret = 1;
            return ret;
        }
        
        // 区间有序 / 二分查找
        private int FindBoundIndex(float target, int count, Func<int, int> visitor)
        {
            int left = 0;
            int right = count - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (visitor(mid) < target)
                {
                    left = mid + 1;
                }
                else if (visitor(mid) > target)
                {
                    right = mid - 1;
                }
                else if (visitor(mid) == target)
                {
                    right = mid;
                    break;
                }
            }
            return right;
        }
    }
}