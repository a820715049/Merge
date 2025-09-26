/**
 * @Author: zhangpengjian
 * @Date: 2025/8/4 18:16:43
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/8/4 18:16:43
 * Description: 拼图活动
 */

using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using static FAT.ListActivity;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class ActivityPuzzle : ActivityLike, IBoardEntry, IActivityOrderHandler
    {
        public EventPuzzle conf;
        public EventPuzzleRound confR;
        public EventPuzzleDetaile confD;
        public VisualPopup VisualMain { get; } = new(UIConfig.UIActivityPuzzleMain);
        public VisualPopup VisualConvert { get; } = new(UIConfig.UIActivityPuzzleConvert);
        private Dictionary<int, (int tokenNum, int maxTimes)> _curRoundTokenDict = new();
        private Dictionary<int, int> _orderPuzzleDict = new();
        public int MaxProgress => confR?.Progress ?? 0;
        public int PuzzleProgress => _puzzleValue;
        public int TokenId => conf?.TokenId ?? 0;
        public int TokenNum => _tokenNum;
        public int Round => _currentRound;
        public int MilestoneIndex => _lastClaimedMilestoneIndex;
        /// <summary>
        /// 判断当前回合是否是循环回来的（第2次及以上出现的回合）
        /// </summary>
        public bool IsCycleRound => conf.IsCycle && _currentRound >= conf.NormalRoundId.Count;
        public List<RewardCommitData> RewardCommitList = new();

        private int _tokenNum;
        private int _currentRound;
        private int _grpId;
        private int _puzzleIdx;
        private int _puzzleValue;
        private int _lastClaimedMilestoneIndex = -1;
        private int _guidePenaltyUsed = 0; // 引导期间已经减少的token数量

        public override ActivityVisual Visual => VisualMain.visual;

        public ActivityPuzzle(ActivityLite lite_)
        {
            Lite = lite_;
            conf = fat.conf.EventPuzzleVisitor.GetOneByFilter(x => x.Id == lite_.Param);
            VisualMain.Setup(conf.EventThemeId, this);
            VisualConvert.Setup(conf.RecontinueTheme, this, active_: false);
        }

        public override void Open()
        {
            VisualMain.Open(this);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (IsComplete()) return;
            popup_.TryQueue(VisualMain.popup, state_);
        }

        public override void LoadSetup(ActivityInstance instance)
        {
            var any = instance.AnyState;
            var i = 0;
            _currentRound = ReadInt(i++, any);
            _tokenNum = ReadInt(i++, any);
            _puzzleIdx = ReadInt(i++, any);
            _puzzleValue = ReadInt(i++, any);
            _grpId = ReadInt(i++, any);
            _lastClaimedMilestoneIndex = ReadInt(i++, any);
            _guidePenaltyUsed = ReadInt(i++, any);
            foreach (var item in any)
            {
                if (item.Id < 0)
                {
                    // id为负数时，表示订单id
                    _orderPuzzleDict[UnityEngine.Mathf.Abs(-item.Id)] = item.Value;
                }
            }

            RefreshRoundConf(_grpId);
        }

        public override void SaveSetup(ActivityInstance instance)
        {
            var any = instance.AnyState;
            var i = 0;
            any.Add(ToRecord(i++, _currentRound));
            any.Add(ToRecord(i++, _tokenNum));
            any.Add(ToRecord(i++, _puzzleIdx));
            any.Add(ToRecord(i++, _puzzleValue));
            any.Add(ToRecord(i++, _grpId));
            any.Add(ToRecord(i++, _lastClaimedMilestoneIndex));
            any.Add(ToRecord(i++, _guidePenaltyUsed));
            foreach (var kv in _orderPuzzleDict)
            {
                // 用负数表示订单id
                any.Add(ToRecord(-kv.Key, kv.Value));
            }
        }

        public override void WhenEnd()
        {
            using var _ = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            map[conf.TokenId] = _tokenNum;
            var tokenReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> tokenRewardList);
            ActivityExpire.ConvertToReward(conf.ExpirePopup, tokenRewardList, ReasonString.puzzle_end, map);
            if (tokenRewardList.Count > 0)
            {
                Game.Manager.screenPopup.TryQueue(VisualConvert.popup, PopupType.Login, tokenReward);
            }
            else
            {
                tokenReward.Free();
            }
        }

        public override void SetupFresh()
        {
            _tokenNum = conf.FreeTokenNum;
            RefreshRoundConf();
        }

        public void MoveToNextRound()
        {
            _currentRound++;
            _tokenNum = 0;
            _orderPuzzleDict.Clear();
            
            bool shouldContinue;
            if (conf.IsCycle)
            {
                // 循环模式：只要有轮次配置就永远继续
                shouldContinue = conf.NormalRoundId.Count > 0;
            }
            else
            {
                // 非循环模式：保持原有逻辑
                shouldContinue = _currentRound < conf.NormalRoundId.Count;
            }
            
            if (shouldContinue)
            {
                RefreshRoundConf();
                _puzzleIdx = 0;
                _puzzleValue = 0;
                // 重置里程碑奖励状态
                _lastClaimedMilestoneIndex = -1;
            }
        }

        private void RefreshRoundConf(int groupId = 0)
        {
            // 根据是否循环模式来获取模板ID
            int roundIndex;
            if (conf.IsCycle)
            {
                // 循环模式：使用模运算循环轮次
                roundIndex = _currentRound % conf.NormalRoundId.Count;
            }
            else
            {
                // 非循环模式：直接使用当前轮次
                roundIndex = _currentRound;
            }
            
            var templateId = conf.NormalRoundId[roundIndex];
            confR = fat.conf.EventPuzzleRoundVisitor.GetOneByFilter(x => x.Id == templateId);
            _grpId = groupId > 0 ? groupId : Game.Manager.userGradeMan.GetTargetConfigDataId(confR.GradeId);
            confD = fat.conf.EventPuzzleDetaileVisitor.GetOneByFilter(x => x.Id == _grpId);
            _curRoundTokenDict.Clear();
            foreach (var info in confD.NumInfo)
            {
                var (id, tokenNum, maxTimes) = info.ConvertToInt3();
                _curRoundTokenDict[id] = (tokenNum, maxTimes);
            }
        }

        public void AddToken(int id, int count)
        {
            if (id != TokenId)
                return;
            var tokenAfter = _tokenNum + count;
            if (tokenAfter > MaxProgress)
            {
                tokenAfter = MaxProgress;
            }
            _tokenNum = tokenAfter;
            DataTracker.token_change.Track(id, count, _tokenNum, ReasonString.puzzle_token);
        }

        public bool TryPutPuzzle(int id)
        {
            if (_tokenNum <= 0) return false;
            if (HasPutPuzzle(id)) return false;
            _tokenNum--;
            SavePuzzleIndexPut(id);
            _puzzleValue++;
            RewardCommitList.Clear();
            // 检查并发放里程碑奖励
            CheckMilestoneRewards();
            if (IsComplete())
            {
                Game.Manager.activity.EndImmediate(this, false);
            }
            return true;
        }

        public bool HasPutPuzzle(int idx)
        {
            return (_puzzleIdx & (1 << idx)) != 0;
        }

        private void SavePuzzleIndexPut(int idx)
        {
            _puzzleIdx |= 1 << idx;
        }

        private int GetPuzzleTokens()
        {
            var count = 0;
            foreach (var kv in _orderPuzzleDict)
            {
                if (_curRoundTokenDict.TryGetValue(kv.Key, out var info))
                {
                    count += info.tokenNum * kv.Value;
                }
            }
            return count;
        }

        // 检查并发放里程碑奖励
        private void CheckMilestoneRewards()
        {
            if (confD?.RewardCount == null || confD?.RewardId == null) return;

            var rewardCountList = confD.RewardCount;
            var rewardIdList = confD.RewardId;

            // 从上次发放的下一个里程碑开始检查
            for (int i = _lastClaimedMilestoneIndex + 1; i < rewardCountList.Count && i < rewardIdList.Count; i++)
            {
                var milestoneValue = rewardCountList[i];

                // 如果达到里程碑
                if (_puzzleValue >= milestoneValue)
                {
                    RewardCommitList.Clear();
                    var rewardConfig = fat.conf.EventPuzzleRewardsVisitor.GetOneByFilter(x => x.Id == rewardIdList[i].ConvertToInt());
                    foreach (var reward in rewardConfig.RewardId)
                    {
                        var rewardData = reward.ConvertToRewardConfig();
                        var r = Game.Manager.rewardMan.BeginReward(rewardData.Id, rewardData.Count, ReasonString.puzzle_milestone);
                        RewardCommitList.Add(r);
                    }
                    DataTracker.event_puzzle_rwd.Track(this, i + 1, confD.RewardCount.Count, confD.Id, _currentRound + 1, i == rewardCountList.Count - 1, IsCycleRound);
                    // 更新已发放的里程碑下标
                    _lastClaimedMilestoneIndex = i;
                }
                else
                {
                    // 如果还没达到这个里程碑，后面的也不用检查了
                    break;
                }
            }
        }

        public bool IsComplete()
        {
            // 循环模式下，不会因为轮次结束而完成
            if (conf.IsCycle)
            {
                return false;
            }
            else
            {
                // 非循环模式：保持原有逻辑
                return _currentRound >= conf.NormalRoundId.Count - 1 && _puzzleValue >= MaxProgress;
            }
        }

        #region IBoardEntry

        string IBoardEntry.BoardEntryAsset() => VisualMain.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var res) ? res : null;
        bool IBoardEntry.BoardEntryVisible => !IsComplete();

        #endregion

        #region IActivityOrderHandler

        public static string GetExtraRewardMiniThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var _cfg = fat.conf.EventTimeVisitor.GetOneByFilter(x => x.Id == eventId && x.EventType == fat.rawdata.EventType.Puzzle);
                paramId = _cfg?.EventParam ?? 0;
            }
            if (paramId == 0)
            {
                DebugEx.Warning($"failed to find event {eventId} {paramId}");
                return string.Empty;
            }
            var cfg = fat.conf.EventPuzzleVisitor.GetOneByFilter(x => x.Id == paramId);
            var theme = fat.conf.EventThemeVisitor.GetOneByFilter(x => x.Id == cfg.EventThemeId);
            theme.AssetInfo.TryGetValue("orderPuzzle", out var res);
            return res;
        }

        bool IActivityOrderHandler.IsValidForBoard(int boardId) => boardId == conf.BoardId;

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            var changed = false;
            if (OrderAttachmentUtility.slot_extra_tl.HasData(order))
            {
                if (OrderAttachmentUtility.slot_extra_tl.IsMatchEventId(order, Id))
                {
                    return changed;
                }
                else
                {
                    // 不是同一期 这里顺便删除
                    OrderAttachmentUtility.slot_extra_tl.ClearData(order);
                    changed = true;
                }
            }
            // 不支持的slot | 无需后续处理
            if (!_curRoundTokenDict.TryGetValue(order.Id, out var info))
            {
                return changed;
            }
            // 检查剩余token数量
            var tokenLeft = MaxProgress - GetPuzzleTokens();
            if (tokenLeft <= 0)
            {
                // 已投放足够的token
                return changed;
            }
            // 判断是否有剩余次数
            if (_orderPuzzleDict.TryGetValue(order.Id, out var usedNum))
            {
                if (usedNum >= info.maxTimes)
                {
                    return changed;
                }
                else
                {
                    _orderPuzzleDict[order.Id] = usedNum + 1;
                }
            }
            else
            {
                _orderPuzzleDict[order.Id] = 1;
            }

            var num = tokenLeft < info.tokenNum ? tokenLeft : info.tokenNum;
            // 引导没完成时总共少投放1个token
            var finished = Game.Manager.guideMan.IsGuideFinished(151);
            if (!finished && num > 0 && _guidePenaltyUsed < 1)
            {
                num = num - 1;
                _guidePenaltyUsed = 1; // 标记已经减少了1个token
            }
            OrderAttachmentUtility.slot_extra_tl.UpdateEventData(order, Id, Param, TokenId, num);
            changed = true;
            return changed;
        }
        #endregion
    }

    public class PuzzleEntry : IEntrySetup
    {
        public Entry Entry => e;
        private readonly Entry e;
        private readonly ActivityPuzzle p;

        public PuzzleEntry(Entry e_, ActivityPuzzle p_)
        {
            (e, p) = (e_, p_);
            e.dot.SetActive(p.TokenNum > 0);
        }

        public override void Clear(Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            var showRedPoint = p.TokenNum > 0;
            e.dot.SetActive(showRedPoint);
            return UIUtility.CountDownFormat(diff_);
        }
    }
}
