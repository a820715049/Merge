using System;
using System.Collections.Generic;
using EL;
using FAT.Merge;
using fat.rawdata;
using static FAT.RecordStateHelper;
using static fat.conf.Data;
using fat.gamekitdata;
using static EL.MessageCenter;
using FAT.MSG;
using Config;
using UnityEngine;

namespace FAT
{
    using static PoolMapping;
    // 限时合成订单活动（LimitMergeOrder）
    // 关键逻辑说明：
    // - 生命周期：SetupTS 支持 LifeTime 裁剪活动窗口
    // - 订单生成：DurationSec = Countdown - 1，让订单比活动早 1 秒结束，避免残留
    // - 弹窗：采用 _hasShownStartPopup 做互斥，初始化只弹登录弹窗，首单不再弹“新订单”弹窗
    // - 结束：WhenEnd 只做奖励结算与解绑关联活动，不再强制改订单状态，避免与 Provider 刷新打架
    public class ActivityLimitMergeOrder : ActivityLike, IActivityOrderHandler, IActivityOrderGenerator
    {
        // 资源缓存：活动结束后 LookupAny 可能拿不到实例，使用静态缓存兜底
        private static string s_cachedOrderAttachmentRes;
        private static string s_cachedOrderThemeRes;
        public partial class UIConfig
        {
            public static UIResource UILimitMergeOrder = new("UILimitMergeOrder_s001.prefab", UILayer.AboveStatus, "event_limitmergeorder_s001");
            public static UIResource UILimitMergeOrderReplacement = new("UILimitMergeOrderReplacement_s001.prefab", UILayer.AboveStatus, "event_limitmergeorder_s001");
        }
        public override bool Valid => Lite.Valid;
        private EventLimitMerge _conf;
        private EventLimitMergeGroup _confDetail;
        // UI 资源/弹窗（占位，接入时替换为真实 UIConfig）
        public ActivityVisual mainVisual = new();
        public ActivityVisual endVisual = new();
        public PopupActivity popupMain = new();
        public PopupLimitMergeOrder popupEnd = new();
        public PopupLimitMergeOrder popupNewOrder = new();
        public UIResAlt mainRes = new(UIConfig.UILimitMergeOrder);
        public UIResAlt endRes = new(UIConfig.UILimitMergeOrderReplacement);

        // 弹窗互斥：
        // true 表示已在初始化阶段入队了登录弹窗；
        // TryGeneratePassiveOrder 首单不再入队“新订单”弹窗，随后恢复为 false
        private bool _hasShownStartPopup;
        #region 存档字段
        private int curRound;
        private int curTemplateId;
        // 是否“静音”（截止期内不生成订单、不弹启用弹窗）
        private bool _muted;
        // 活动实例的结束时间戳（秒），受 LifeTime 限制
        private int actEndTime;
        public bool needPopRewardTip;
        #endregion
        public bool IsFinished()
        {
            return _conf != null && curRound >= _conf.OrderNum;
        }
        // 获取当前 round 的奖励宝箱配置 Id（返回 EventLimitMergeOrder.Rewards）
        public int GetCurrentRoundRewardBoxId()
        {
            if (_confDetail == null)
            {
                return 0;
            }
            var idx = Math.Max(0, curRound);
            if (idx >= _confDetail.OrderInfo.Count)
            {
                return 0;
            }
            var lm = GetEventLimitMergeOrder(_confDetail.OrderInfo[idx]);
            return lm?.Rewards ?? 0;
        }
        // 获取当前 round 需求棋子的“最大棋子”id（把配置解析为真实棋子ID；若配置为链条/分类ID，则取该链条的最高等级棋子）
        public int GetCurrentRoundMaxRequiredItemId()
        {
            if (_confDetail == null)
            {
                return 0;
            }
            var idx = Math.Max(0, curRound);
            if (idx >= _confDetail.OrderInfo.Count)
            {
                return 0;
            }
            var lm = GetEventLimitMergeOrder(_confDetail.OrderInfo[idx]);
            if (lm == null)
            {
                return 0;
            }
            return _ResolveRequiredItemId(lm.OrderItem);
        }
        public ActivityLimitMergeOrder(ActivityLite lite)
        {
            Lite = lite;
            InitConf();
            InitTheme();
        }
        public override void LoadSetup(ActivityInstance data)
        {
            var any = data.AnyState;
            int i = 0;
            curRound = ReadInt(i++, any);
            curTemplateId = ReadInt(i++, any);
            _muted = ReadBool(i++, any);
            actEndTime = ReadInt(i++, any);
            needPopRewardTip = ReadBool(i++, any);
        }
        public override void SaveSetup(ActivityInstance data)
        {
            var any = data.AnyState;
            int i = 0;
            any.Add(ToRecord(i++, curRound));
            any.Add(ToRecord(i++, curTemplateId));
            any.Add(ToRecord(i++, _muted));
            any.Add(ToRecord(i++, actEndTime));
            any.Add(ToRecord(i++, needPopRewardTip));
        }
        public override void WhenReset()
        {
            // 清理运行时状态，避免下一次加载残留
            curRound = 0;
            _muted = false;
            actEndTime = 0;
            popupMain.Clear();
            _hasShownStartPopup = false;
        }
        public override void WhenEnd()
        {
            CollectItem();
            EndExtraOrder();
        }
        private void CollectItem()
        {
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);

            ActivityExpire.ConvertToReward(_conf.ExpirePopup, list, ReasonString.limitmergeorder_reward);

            if (list.Count > 0)
            {
                // 延迟1秒弹窗
                Game.Instance.StartCoroutineGlobal(_CoPopupEndAfterDelay(listT));
            }
            else
            {
                listT.Free();
            }
        }
        private void EndExtraOrder()
        {
            // 结束本活动绑定的 ExtraRewardOrder（订单额外奖励活动）
            if (_confDetail != null && _confDetail.OrderExtraId > 0 &&
                Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderExtra, _confDetail.OrderExtraId, out var extraAct) && extraAct != null)
            {
                Game.Manager.activity.EndImmediate(extraAct, false);
            }
        }
        public override void Open()
        {
            popupMain.OpenPopup();
        }
        // 生命周期：若配置了 LifeTime，则本活动只在可参与窗口内持续 LifeTime 秒；否则持续到活动时间结束
        public override (long, long) SetupTS(long sTS_, long eTS_)
        {
            if (actEndTime > 0)
            {
                return (Lite.StartTS, actEndTime);
            }
            var lifeTime = _conf != null ? _conf.Lifetime : 0;
            if (lifeTime > 0)
            {
                var now = Game.Instance.GetTimestampSeconds();
                if (now + lifeTime >= Lite.EndTS)
                {
                    actEndTime = (int)Lite.EndTS;
                }
                else
                {
                    actEndTime = (int)(now + lifeTime);
                }
            }
            else
            {
                actEndTime = (int)Lite.EndTS;
            }
            return (Lite.StartTS, actEndTime);
        }

        public override void WhenActive(bool new_)
        {
            if (!new_)
            {
                return;
            }
            var now = Game.TimestampNow();
            var deadline = _conf.Deadline;
            if (deadline <= 0)
            {
                _muted = false;
            }
            else
            {
                var preDeadlineOk = now < (endTS - deadline);
                _muted = !preDeadlineOk;
            }
            if (!_muted)
            {
                // 处于活动可参与窗口内：遵循 Login 弹窗阶段
                _hasShownStartPopup = true;
                Game.Manager.screenPopup.TryQueue(popupMain, PopupType.Login);
            }
        }

        #region IActivityOrderHandler
        // 刷新前：监听我方限时合成订单的通过/过期，做回合统计与状态复位
        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            // 先处理订单状态，再判断活动是否已完成
            var od = order as IOrderData;
            if (od == null)
                return false;
            if (!od.IsFlash)
                return false;
            if (order.GetValue(OrderParamType.EventId) != Id)
            {
                return false;
            }
            if (order.State == OrderState.Rewarded)
            {
                OnOrderPassed(true);
            }
            if (order.State == OrderState.Expired || od.IsExpired)
            {
                OnOrderPassed(false);
            }
            return false;
        }
        #endregion

        #region 内部逻辑
        private void InitConf()
        {
            // 基础配置
            _conf = GetEventLimitMerge(Lite.Param);
            if (curTemplateId <= 0)
            {
                // 分层数据进存档 只有首次参加活动才会设置
                curTemplateId = Game.Manager.userGradeMan.GetTargetConfigDataId(_conf.InfoGrp);
            }
            // 模版详情
            _confDetail = GetEventLimitMergeGroup(curTemplateId);
        }
        private void InitTheme()
        {
            mainVisual.Setup(_conf.StartTheme, mainRes);
            endVisual.Setup(_conf.EndTheme, endRes);
            popupMain.Setup(this, mainVisual, mainRes);
            popupNewOrder.Setup(this, mainVisual, mainRes);
            popupEnd.Setup(this, endVisual, endRes);

            // 缓存关键资源
            if (mainVisual.AssetMap.TryGetValue("orderSlot", out var slotRes))
            {
                s_cachedOrderAttachmentRes = slotRes;
            }
            if (mainVisual.AssetMap.TryGetValue("orderItem", out var itemRes))
            {
                s_cachedOrderThemeRes = itemRes;
            }
        }

        // 解析配置的需求字段为“实际棋子ID”
        // - 如果传入的是链条/分类ID，则返回该链条的最高等级棋子
        // - 如果传入的本来就是棋子ID，则原样返回
        private static int _ResolveRequiredItemId(int itemOrCategoryId)
        {
            var catCfg = Game.Manager.mergeItemMan.GetCategoryConfig(itemOrCategoryId);
            if (catCfg != null && catCfg.Progress != null && catCfg.Progress.Count > 0)
            {
                return catCfg.Progress[catCfg.Progress.Count - 1];
            }
            return itemOrCategoryId;
        }
        public void OnOrderPassed(bool isWin)
        {
            if (isWin)
            {
                // 提交限时合成订单时埋点
                var currentIdx = curRound; // 0-based
                var total = _conf.OrderNum;
                var isFinal = currentIdx >= total - 1;
                var eventDiff = _confDetail.Diff;
                DataTracker.EventLimitMergeOrderEnd(this, eventDiff, currentIdx + 1, total, isFinal);
                curRound += 1;
                if (curRound >= total)
                {
                    Get<MSG.ACTIVITY_SUCCESS>().Dispatch(this);
                    //延迟1帧结束
                    Game.Instance.StartCoroutineGlobal(_CoEndAfterDelay());
                }
            }
            else
            {
                _muted = true;
                WhenEnd();
            }
        }
        #endregion
        bool IActivityOrderHandler.IsValidForBoard(int boardId)
        {
            return _conf.BoardId == boardId;
        }
        #region IActivityOrderGenerator
        // 由活动在被动slot上生成“限时合成订单”
        bool IActivityOrderGenerator.TryGeneratePassiveOrder(OrderRandomer cfg, IOrderHelper helper, MergeWorldTracer tracer, Func<OrderRandomer, OrderData> builder, out OrderData order)
        {
            if (_muted)
            {
                order = null;
                return false;
            }
            if (IsFinished())
            {
                order = null;
                return false;
            }
            order = null;
            if (_confDetail == null)
            {
                return false;
            }

            // 匹配本活动模板中的 IntRandomer（与 Step 的随机器匹配一致）
            if (curRound >= _confDetail.OrderInfo.Count)
            {
                return false;
            }
            int lmId = _confDetail.OrderInfo[curRound];
            var lm = GetEventLimitMergeOrder(lmId);
            if (lm == null || lm.IntRandomer != cfg.Id)
            {
                return false;
            }
            var requiredItemId = _ResolveRequiredItemId(lm.OrderItem);
            var requireItems = new List<int> { requiredItemId };
            var realDifficulty = OrderUtility.CalcRealDifficultyForRequires(requireItems);
            var rewards = Game.Manager.mergeItemMan.GetOrderRewardConfig(lm.Rewards).Reward;
            order = OrderUtility.MakeOrderByConfig(helper, OrderProviderType.Random, cfg.Id, cfg.RoleId, /*unlock*/0, realDifficulty, requireItems, rewards);
            order.OrderType = (int)OrderType.LimitMergeOrder;
            order.Record.OrderType = order.OrderType;
            order.ProviderType = (int)OrderProviderType.Random;
            var any = order.Record.Extra;
            any.Add(ToRecord((int)OrderParamType.EventId, Id));
            any.Add(ToRecord((int)OrderParamType.EventParam, curTemplateId));
            // 订单持续时间：比活动早 1 秒结束
            var start = (int)Game.TimestampNow();
            var duration = (int)Math.Max(0, Countdown - 1);
            any.Add(ToRecord((int)OrderParamType.StartTimeSec, start));
            any.Add(ToRecord((int)OrderParamType.DurationSec, duration));
            // 弹窗互斥：若初始化已弹过登录弹窗，则首单不再弹“新订单”弹窗
            if (!_hasShownStartPopup)
            {
                Game.Manager.screenPopup.TryQueue(popupNewOrder, (PopupType)(-1));
            }
            _hasShownStartPopup = false;
            needPopRewardTip = true;
            return true;
        }
        #endregion
        #region res
        public static string GetOrderAttachmentRes() => GetRes("orderSlot");
        public static string GetOrderThemeRes() => GetRes("orderItem");
        private static string GetRes(string key)
        {
            if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.LimitMerge, out var act))
            {
                if (act is ActivityLimitMergeOrder inst)
                {
                    if (inst.mainVisual.AssetMap.TryGetValue(key, out var result))
                    {
                        return result;
                    }
                }
            }
            // 活动已结束或实例不可用时，返回缓存
            if (key == "orderSlot")
            {
                return s_cachedOrderAttachmentRes;
            }
            if (key == "orderItem")
            {
                return s_cachedOrderThemeRes;
            }
            return null;
        }
        #endregion

        // 延迟结算弹窗协程
        private System.Collections.IEnumerator _CoPopupEndAfterDelay(object listToken)
        {
            yield return new WaitForSeconds(1f);
            Game.Manager.screenPopup.TryQueue(popupEnd, (PopupType)(-1), listToken);
        }

        private System.Collections.IEnumerator _CoEndAfterDelay()
        {
            yield return null;
            Game.Manager.activity.EndImmediate(this, false);
        }
    }
}

public partial class DataTracker
{
    public static void EventLimitMergeOrderEnd(FAT.ActivityLimitMergeOrder acti_, int event_diff, int milestone_queue, int milestone_num, bool is_final)
    {
        var data = BorrowTrackObject();
        FillActivity(data, acti_);
        // 显式补充关键字段
        data["event_id"] = acti_.Id;
        data["event_from"] = acti_.From;
        data["event_param"] = acti_.Param;
        data["event_diff"] = event_diff;
        data["milestone_queue"] = milestone_queue;
        data["milestone_num"] = milestone_num;
        data["is_final"] = is_final;
        TrackObject(data, "event_limitmerge_order_end");
    }
}



