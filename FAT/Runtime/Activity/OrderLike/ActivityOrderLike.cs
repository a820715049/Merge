/*
 * @Author: qun.chao
 * @Date: 2025-03-24 17:01:25
 */
using fat.gamekitdata;
using fat.rawdata;
using System.Collections.Generic;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using FAT.Merge;
using EL;
using Cysharp.Text;
using DG.Tweening;
using UnityEngine.UI.Extensions;
using Cysharp.Threading.Tasks;
namespace FAT
{
    public struct OrderLikeParamProvider : IParamProvider
    {
        public int orderId;
    }

    public class ActivityOrderLike : ActivityLike, IBoardEntry, IActivityOrderHandler, ISpawnEffectWithTrail
    {
        public int DisplayToken => _claimedTokenCount - _flyingToken;
        public int CurToken => _claimedTokenCount;
        public int MaxToken => _confDetail.Progress;
        public bool ReadyToClaim => CurToken >= MaxToken && MaxToken > 0;
        public int TokenId => _conf.TokenId;

        #region UI
        public UIResAlt MainRes = new(UIConfig.UIOrderLike);
        public UIResAlt RoundStartRes = new(UIConfig.UIOrderLikeStart);
        public UIResAlt HelpRes = new(UIConfig.UIOrderLikeHelp);
        public PopupOrderLike MainPopup;
        #endregion

        #region 存档字段
        // 回合数
        private int _roundCount = 0;
        // 本回合 记录订单打赏过的次数
        private Dictionary<int, int> _orderTippedDict = new();
        // 本回合 已领取的token数量
        private int _claimedTokenCount = 0;
        // 本回合 完成的订单数量
        private int _orderNum = 0;
        // 是否已触发过启动弹窗
        private bool _hasPop;
        #endregion
        private bool _hasPopupAdded;
        private int _flyingToken = 0;

        // 本回合 订单的token信息
        private Dictionary<int, (int tokenNum, int maxTimes)> _curRoundTokenDict = new();

        private EventOrderLike _conf;
        private EventOrderLikeDetail _confDetail;
        private EventTheme _mainTheme;

        public ActivityOrderLike(ActivityLite lite_)
        {
            Lite = lite_;
        }

        string IBoardEntry.BoardEntryAsset()
        {
            return _mainTheme.AssetInfo.TryGetValue("boardEntry", out var res) ? res : null;
        }

        public void ResolveFlyingToken(int amount)
        {
            if (_flyingToken >= amount)
            {
                _flyingToken -= amount;
            }
            if (_flyingToken <= 0 && ReadyToClaim)
            {
                Game.Manager.screenPopup.TryQueue(MainPopup, PopupType.Login, true);
            }
        }

        public bool TryClaimReward(IList<(RewardCommitData, float)> rewards)
        {
            if (!ReadyToClaim)
                return false;
            var delay = 0.2f;
            var diffyMin = _confDetail.RwdActDiffRange[0];
            var diffyMax = _confDetail.RwdActDiffRange[1];
            var board = Game.Manager.mergeBoardMan.activeWorld.activeBoard;

            var context = ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.OrderLike);
            context.spawnEffect = this;

            var totalDffy = 0;
            using var sb = ZString.CreateStringBuilder();

            for (var i = 0; i < _confDetail.RwdLimitCount; i++)
            {
                var itemId = Game.Manager.mergeItemDifficultyMan.CalcSpecialBoxOutput(diffyMin, diffyMax);
                if (itemId <= 0)
                {
                    // 无法生成奖励
                    DebugEx.Error("[ActivityOrderLike] specialbox output failed");
                    continue;
                }
                // 记录id
                sb.Append(itemId);
                if (i < _confDetail.RwdLimitCount - 1)
                {
                    sb.Append(",");
                }
                // 记录难度
                if (Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(itemId, out var avg, out var real))
                {
                    totalDffy += real;
                }
                UniTask.Void(async () =>
                {
                    await UniTask.WaitForSeconds(delay * i);
                    // 播放音效
                    Game.Manager.audioMan.TriggerSound("OrderLikeRwd");
                });
                UIFlyFactory.GetFlyTarget(FlyType.OrderLikeToken, out var pos);
                // 注册出生位置
                BoardUtility.RegisterSpawnRequest(itemId, pos, delay * i);
                var item = board.TrySpawnItem(itemId, ItemSpawnReason.OrderLike, context);
                if (item != null)
                {
                    // 已发往棋盘
                    continue;
                }
                else
                {
                    // 不再需要发放到棋盘 取消预填充的SpawnRequest
                    BoardUtility.PopSpawnRequest();
                    // 需要放入奖励队列 走飞图标机制
                    var reward = Game.Manager.rewardMan.BeginReward(itemId, 1, ReasonString.order_like);
                    rewards?.Add((reward, delay * i));
                }
            }
            TrackReward(sb.ToString(), totalDffy);
            MoveToNextRound();
            return true;
        }

        private void MoveToNextRound()
        {
            _roundCount++;
            _claimedTokenCount = 0;
            _orderNum = 0;
            _orderTippedDict.Clear();
            RefreshRoundConf();
            MessageCenter.Get<MSG.ORDERLIKE_ROUND_CHANGE>().Dispatch();
            TrackRoundStart();
        }

        // 本回合 已投放的token数量
        private int GetTippedTokens()
        {
            var count = 0;
            foreach (var kv in _orderTippedDict)
            {
                if (_curRoundTokenDict.TryGetValue(kv.Key, out var info))
                {
                    count += info.tokenNum * kv.Value;
                }
            }
            return count;
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            _roundCount = ReadInt(i++, any);
            _claimedTokenCount = ReadInt(i++, any);
            _orderNum = ReadInt(i++, any);
            _hasPop = ReadBool(i++, any);
            foreach (var item in any)
            {
                if (item.Id < 0)
                {
                    // id为负数时，表示订单id
                    _orderTippedDict[UnityEngine.Mathf.Abs(-item.Id)] = item.Value;
                }
            }
            InitConf();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            any.Add(ToRecord(i++, _roundCount));
            any.Add(ToRecord(i++, _claimedTokenCount));
            any.Add(ToRecord(i++, _orderNum));
            any.Add(ToRecord(i++, _hasPop));
            foreach (var kv in _orderTippedDict)
            {
                // 用负数表示订单id
                any.Add(ToRecord(-kv.Key, kv.Value));
            }
        }

        public override void Open()
        {
            MainRes.ActiveR.Open(this);
        }

        public void OpenRoundStart()
        {
            RoundStartRes.ActiveR.Open(this);
        }

        public void OpenHelp()
        {
            HelpRes.ActiveR.Open(this);
        }

        public void MarkPopupDone()
        {
            _hasPop = true;
        }

        public override void SetupFresh()
        {
            InitConf();
            TrackRoundStart();
        }

        public void AddToken(int id, int count, int orderId)
        {
            if (id != _conf.TokenId)
                return;
            _flyingToken += count;
            var tokenAfter = _claimedTokenCount + count;
            if (tokenAfter > MaxToken)
            {
                tokenAfter = MaxToken;
            }
            _claimedTokenCount = tokenAfter;
            // 订单完成次数增加
            ++_orderNum;
            TrackMilestone(orderId);
            DataTracker.token_change.Track(id, count, CurToken, ReasonString.order_like);
        }

        // 当前轮开始
        private void TrackRoundStart()
        {
            DataTracker.event_orderlike_start.Track(this, _roundCount);
        }

        private void TrackMilestone(int orderId)
        {
            DataTracker.event_orderlike_milestone.Track(this, _roundCount, orderId, _orderNum, CurToken >= MaxToken, CurToken, MaxToken);
        }

        private void TrackReward(string rewardStr, int difficulty)
        {
            DataTracker.event_orderlike_rwd.Track(this, _roundCount, difficulty, rewardStr);
        }

        private void InitConf()
        {
            var _cfg = GetEventOrderLike(Lite.Param);
            if (_cfg != null)
            {
                _conf = _cfg;
                _mainTheme = GetEventTheme(_cfg.EventTheme);
                Visual.Setup(_cfg.EventTheme, MainRes);
                MainPopup = new PopupOrderLike(this, Visual, MainRes);
                RefreshRoundConf();
            }
        }

        public override void WhenActive(bool new_)
        {
            if (!new_) return;
            if (!_hasPop && !_hasPopupAdded)
            {
                Game.Manager.screenPopup.TryQueue(MainPopup, PopupType.Login, true);
                _hasPopupAdded = true;
            }
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (!_hasPop && !_hasPopupAdded)
            {
                popup_.TryQueue(MainPopup, state_, true);
                _hasPopupAdded = true;
            }
        }

        private void RefreshRoundConf()
        {
            var templateId = _roundCount < _conf.QueueDetail.Count ? _conf.QueueDetail[_roundCount] : _conf.CycleDetail;
            _confDetail = GetEventOrderLikeDetail(templateId);
            _curRoundTokenDict.Clear();
            foreach (var info in _confDetail.NumInfo)
            {
                var (id, tokenNum, maxTimes) = info.ConvertToInt3();
                _curRoundTokenDict[id] = (tokenNum, maxTimes);
            }
        }

        #region IActivityOrderHandler
        public static string GetExtraRewardMiniThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var _cfg = GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == fat.rawdata.EventType.OrderLike);
                paramId = _cfg?.EventParam ?? 0;
            }
            if (paramId == 0)
            {
                DebugEx.Warning($"failed to find event {eventId} {paramId}");
                return string.Empty;
            }
            var cfg = GetEventOrderLike(paramId);
            var theme = GetEventTheme(cfg.EventTheme);
            theme.AssetInfo.TryGetValue("orderLike", out var res);
            return res;
        }

        bool IActivityOrderHandler.IsValidForBoard(int boardId) => boardId == _conf.BoardId;

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
            var tokenLeft = _confDetail.Progress - GetTippedTokens();
            if (tokenLeft <= 0)
            {
                // 已投放足够的token
                return changed;
            }
            // 判断是否有剩余次数
            if (_orderTippedDict.TryGetValue(order.Id, out var usedNum))
            {
                if (usedNum >= info.maxTimes)
                {
                    return changed;
                }
                else
                {
                    _orderTippedDict[order.Id] = usedNum + 1;
                }
            }
            else
            {
                _orderTippedDict[order.Id] = 1;
            }

            var num = tokenLeft < info.tokenNum ? tokenLeft : info.tokenNum;
            OrderAttachmentUtility.slot_extra_tl.UpdateEventData(order, Id, Param, _conf.TokenId, num);
            changed = true;
            return changed;
        }

        #endregion

        #region effect
        public string trail_res_key => _mainTheme.AssetInfo.TryGetValue("likeTail", out var res) ? res : null;

        void ISpawnEffectWithTrail.AddTrail(MBItemView view, Tween tween)
        {
            var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
            GameObjectPoolManager.Instance.CreateObject(trail_res_key, effRoot, trail =>
            {
                trail.SetActive(false);
                trail.transform.position = view.transform.position;
                trail.SetActive(true);
                var script = trail.GetOrAddComponent<MBAutoRelease>();
                script.Setup(trail_res_key, 3f);
                // 显示head粒子
                trail.transform.Find("particle").gameObject.SetActive(true);

                if (tween.IsPlaying())
                {
                    var act = tween.onUpdate;
                    tween.OnUpdate(() =>
                    {
                        act?.Invoke();
                        trail.transform.position = view.transform.position;
                    });
                    var act_complete = tween.onComplete;
                    tween.OnComplete(() =>
                    {
                        act_complete?.Invoke();
                        if (trail != null)
                        {
                            // 隐藏head粒子
                            trail.transform.Find("particle").gameObject.SetActive(false);
                        }
                    });
                }
            });
        }

        #endregion
    }
}