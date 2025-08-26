using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using EL;
using fat.conf;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using FAT.MSG;
using UnityEngine;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class ActivityOrderBonus : ActivityLike, IBoardEntry, IActivityOrderHandler
    {
        #region 运行时字段
        public EventOrderBonus eventOrderBonus;
        public EventOrderBonusGroup eventOrderBonusGroup;
        public EventOrderBonusDetail eventOrderBonusDetail;
        public override bool Valid => eventOrderBonus != null;
        public bool needRedPoint;
        public int waitUpdate;
        #endregion

        #region Theme
        public ActivityVisual eventVisual = new();
        public PopupActivity eventPop = new();
        public UIResAlt eventRes = new(UIConfig.UIOrderBonusPanel);
        #endregion

        #region 存档字段
        private int _groupID;
        private int _fail;
        private int _endTime;
        private int _orderID;
        private int _detailID;
        private int _start;
        #endregion

        #region ActivityLike
        public ActivityOrderBonus() { }

        public ActivityOrderBonus(ActivityLite activityLite)
        {
            Lite = activityLite;
            eventOrderBonus = Game.Manager.configMan.GetEventOrderBonus(activityLite.Param);
            eventVisual.Setup(eventOrderBonus.EventTheme);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListenerUnique(SecondUpdate);
            MessageCenter.Get<ORDER_FINISH_DATA>().AddListenerUnique(CheckStart);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            _groupID = ReadInt(i++, any);
            _fail = ReadInt(i++, any);
            _endTime = ReadInt(i++, any);
            _orderID = ReadInt(i++, any);
            _detailID = ReadInt(i++, any);
            LoadState();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Clear();
            var i = 0;
            any.Add(ToRecord(i++, _groupID));
            any.Add(ToRecord(i++, _fail));
            any.Add(ToRecord(i++, _endTime));
            any.Add(ToRecord(i++, _orderID));
            any.Add(ToRecord(i++, _detailID));
        }

        public override void SetupFresh()
        {
            InitState();
            EnterNextPhase();
        }

        public override void Open()
        {
            UIManager.Instance.OpenWindow(eventRes.ActiveR, this);
        }

        public override void WhenEnd()
        {
            MessageCenter.Get<CLEAR_BONUS>().Dispatch(Id);
            base.WhenEnd();
        }
        #endregion

        #region IBoardEntry
        public string BoardEntryAsset()
        {
            //todo
            eventVisual.AssetMap.TryGetValue("boardEntry", out var result);
            if (!string.IsNullOrEmpty(result)) return result;
            return "event_orderbonus_default:UIOrderBonusEntry.prefab";
        }
        #endregion

        #region IActivityOrderHandle

        private readonly List<(int, int)> _orders = new();
        private long _frame;

        public static string GetExtraRewardMiniThemeRes(int eventId, int paramId)
        {
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderBonus, out var acti_);
            if (acti_ is ActivityOrderBonus)
            {
                (acti_ as ActivityOrderBonus).eventVisual.AssetMap.TryGetValue("extraReward", out var result);
                if (!string.IsNullOrEmpty(result))
                {
                    var res = result.Split("#");
                    return ZString.Format("{0}:{1}", res[0], res[1]);
                }
            }
            return "event_orderbonus_default:OrderRewardItem_Bonus.prefab";
        }
        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (!Active) return false;
            if (waitUpdate > 0) return false;
            if (order.OrderType != (int)OrderType.Normal) return false;
            if (order.State == OrderState.Rewarded || order.State == OrderState.PreShow) return false;
            if (phase == 0) return false;
            if (_orderID != 0)
            {
                if (order.Id != _orderID) return false;
                else
                {
                    if (order.BonusID == 0)
                    {
                        if (_endTime - (int)Game.TimestampNow() <= 0) return false;
                        order.BonusID = eventOrderBonusDetail.Id;
                        order.BonusPhase = phase;
                        order.BonusEventID = Id;
                        order.BonusEndTime = _endTime;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (phase == 0) return false;
                if (_frame == 0)
                {
                    _frame = Time.frameCount;
                    var diffAbs = Math.Abs(eventOrderBonusDetail.PayDiff - order.GetValue(OrderParamType.ActDifficulty));
                    _orders.Add((order.Id, diffAbs));
                }
                else if (_frame == Time.frameCount)
                {
                    var diffAbs = Math.Abs(eventOrderBonusDetail.PayDiff - order.GetValue(OrderParamType.ActDifficulty));
                    _orders.Add((order.Id, diffAbs));
                    _orders.Sort((a, b) => a.Item2 - b.Item2);
                }
                else
                {
                    if (order.Id == _orders.First().Item1)
                    {
                        order.BonusID = eventOrderBonusDetail.Id;
                        order.BonusPhase = phase;
                        _orders.Clear();
                        _orderID = order.Id;
                        order.BonusEventID = Id;
                        order.needBonusAnim = true;
                        _frame = 0;
                        var str = eventOrderBonusDetail.LifeTime.Split(':');
                        _start = (int)Game.TimestampNow();
                        if (str.Count() == 1)
                        {
                            if (int.TryParse(str[0], out var lifetime))
                            {
                                _endTime = (int)Game.TimestampNow() + lifetime;
                                order.BonusEndTime = _endTime;
                            }
                        }
                        else if (str.Count() == 2)
                        {
                            if (int.TryParse(str[0], out var baseTime) && int.TryParse(str[1], out var styles))
                            {
                                var lifetime = Game.Manager.rewardMan.CalcDynamicOrderLifeTime(styles, baseTime, order.GetValue(OrderParamType.ActDifficulty));
                                _endTime = (int)Game.TimestampNow() + lifetime;
                                order.BonusEndTime = _endTime;
                            }
                        }
                        DataTracker.event_orderbonus_pick.Track(this, phase - 1 <= eventOrderBonusGroup.Milestone.Count ? phase - 1 : eventOrderBonusGroup.Milestone.Count, eventOrderBonusGroup.Milestone.Count
                            , eventOrderBonusGroup.Diff, _fail + 1, phase, order.GetValue(OrderParamType.ActDifficulty), order.Id, _detailID);

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

        bool IActivityOrderHandler.OnPostUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            var changed = false;
            if (phase == 0 || !Active)
            {
                if (order.BonusID != 0)
                {
                    if (order.BonusEventID == Id)
                    {
                        DataTracker.event_orderbonus_fail.Track(this, _failPhase <= eventOrderBonusGroup.Milestone.Count ? _failPhase : eventOrderBonusGroup.Milestone.Count, eventOrderBonusGroup.Milestone.Count
                            , eventOrderBonusGroup.Diff, _fail, phase, order.GetValue(OrderParamType.ActDifficulty), order.Id, _failDetail);
                        order.BonusID = 0;
                        order.BonusEndTime = 0;
                        order.BonusEventID = 0;
                        order.BonusPhase = 0;
                        changed = true;
                    }
                }
            }
            return changed;
        }

        #endregion

        #region 活动逻辑
        /// <summary>
        /// 初始化存档数据
        /// </summary>
        private void InitState()
        {
            needRedPoint = true;
            _groupID = Game.Manager.userGradeMan.GetTargetConfigDataId(eventOrderBonus?.GradeId ?? 0);
            eventOrderBonusGroup = Game.Manager.configMan.GetEventOrderBonusGroup(_groupID);
            UIManager.Instance.RegisterIdleAction("bonus_start", 101, () => UIManager.Instance.OpenWindow(UIConfig.UIOrderBonusPanel, this));
        }

        private void LoadState()
        {
            eventOrderBonusGroup = Game.Manager.configMan.GetEventOrderBonusGroup(_groupID);
            if (_detailID != 0) eventOrderBonusDetail = Game.Manager.configMan.GetEventOrderBonusDetail(_detailID);
            if (Game.TimestampNow() >= _endTime && _endTime != 0) Fail();
        }

        private void CheckStart(IOrderData order)
        {
            if (order.OrderType != (int)OrderType.Normal) return;
            if (phase == 0)
            {
                EnterNextPhase();
                DataTracker.event_orderbonus_trigger.Track(this, phase - 1 <= eventOrderBonusGroup.Milestone.Count ? phase - 1 : eventOrderBonusGroup.Milestone.Count, eventOrderBonusGroup.Milestone.Count
                    , eventOrderBonusGroup.Diff, order.GetValue(OrderParamType.ActDifficulty), _fail);
            }
        }

        private void EnterNextPhase()
        {
            _orderID = 0;
            phase++;
            waitUpdate = 3;
            if (phase <= eventOrderBonusGroup.Milestone.Count)
                eventOrderBonusDetail = Game.Manager.configMan.GetEventOrderBonusDetail(eventOrderBonusGroup.Milestone[phase - 1]);
            else if (phase <= eventOrderBonusGroup.Milestone.Count + eventOrderBonusGroup.MaxMilestone.Count)
                eventOrderBonusDetail = Game.Manager.configMan.GetEventOrderBonusDetail(eventOrderBonusGroup.MaxMilestone[phase - eventOrderBonusGroup.Milestone.Count - 1]);
            else
                eventOrderBonusDetail = Game.Manager.configMan.GetEventOrderBonusDetail(eventOrderBonusGroup.CycleMaxMilestone);
            _detailID = eventOrderBonusDetail.Id;
            MessageCenter.Get<MSG.ORDER_BONUS_PHASE_CHANGE>().Dispatch();
            Game.Instance.StartCoroutineGlobal(ShowAnim());

        }

        private IEnumerator ShowAnim()
        {
            yield return new WaitUntil(() => _orderID != 0);
            yield return new WaitForSeconds(0.5f);
            UIManager.Instance.RegisterIdleAction("order_bonus", 100, () => UIManager.Instance.OpenWindow(UIConfig.UIOrderBonusReward, this));
        }

        public RewardCommitData TryClaimReward(IOrderData order)
        {
            if (phase == 0) return null;
            var list = eventOrderBonusDetail.RandomReward.Select(x => Game.Manager.configMan.GetRandomRewardConfigById(x));
            var result = list.RandomChooseByWeight(e => e.Weight);
            var reward = result.Reward.First().ConvertToInt3();
            var id = _detailID;
            EnterNextPhase();
            var info = Game.Manager.rewardMan.BeginReward(reward.Item1, Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(reward.Item2, reward.Item3), ReasonString.order_bonus);
            DataTracker.event_orderbonus_success.Track(this, phase - 1 <= eventOrderBonusGroup.Milestone.Count ? phase - 1 : eventOrderBonusGroup.Milestone.Count, eventOrderBonusGroup.Milestone.Count
                , eventOrderBonusGroup.Diff, _fail + 1, phase - 1, order.GetValue(OrderParamType.ActDifficulty),
                (int)Game.TimestampNow() - _start, ZString.Format("{0}:{1}", info.rewardId, info.rewardCount), order.Id, id);
            return info;
        }

        private int _failPhase;
        private int _failDetail;
        private void Fail()
        {
            _fail++;
            _failPhase = phase;
            _failDetail = _detailID;
            phase = 0;
            _endTime = 0;
            _detailID = 0;
            _orderID = 0;
            needRedPoint = true;
            MessageCenter.Get<MSG.ORDER_BONUS_PHASE_CHANGE>().Dispatch();
        }

        private void SecondUpdate()
        {
            if (waitUpdate > 0)
                waitUpdate--;
            if (_endTime > 0)
                if (_endTime <= Game.TimestampNow() && _orderID != 0)
                    Fail();
        }

        #endregion

        #region 对外接口
        public static string GetOrderThemeRes(int id)
        {
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderBonus, out var acti_);
            if (acti_ is ActivityOrderBonus)
            {
                (acti_ as ActivityOrderBonus).eventVisual.AssetMap.TryGetValue("OrderItem", out var result);
                if (!string.IsNullOrEmpty(result))
                {
                    var res = result.Split("#");
                    return ZString.Format("{0}:{1}", res[0], res[1]);
                }
            }
            return "event_orderbonus_default:OrderItem_Bonus.prefab";
        }
        #endregion
    }
}