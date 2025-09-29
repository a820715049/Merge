/*
 * @Author: tang.yan
 * @Description: 自选限时订单活动 
 * @Doc: https://centurygames.feishu.cn/wiki/HEEew3SI4ir0FZkZOqKcq8Oun4c?fromScene=spaceOverview
 * @Date: 2025-09-26 11:09:16
 */

using EL;
using fat.gamekitdata;
using fat.rawdata;
using fat.conf;
using static FAT.RecordStateHelper;
using System;
using FAT.Merge;

namespace FAT
{
    public class ActivityOrderDiffChoice : ActivityLike, IActivityOrderHandler, IActivityOrderGenerator
    {
        public override bool Valid => Lite.Valid && Conf != null;
        public EventOrderDiffChoice Conf { get; private set; }
        //活动主界面可以展示出来的最晚时间 单位秒， 超过这个时间如果活动窗口还开着则需要关闭
        public long endShowTS => Valid ? endTS - Conf.Deadline : endTS;

        public ActivityOrderDiffChoice(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = Game.Manager.configMan.GetEventOrderDiffChoiceConfig(lite_.Param);
        }

        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            //刷新弹脸信息
            _RefreshPopupInfo();
            //活动首次开启时 尝试弹脸
            _TryPopFirst();
        }
        
        public static string GetOrderThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var cfg = Data.GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == EventType.OrderExtra);
                paramId = cfg?.EventParam ?? 0;
            }

            if (paramId == 0)
            {
                DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }

            var cfgDetail = Game.Manager.configMan.GetEventOrderDiffChoiceConfig(paramId);
            return cfgDetail?.OrderTheme;
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            _choiceType = ReadInt(0, any);
            _hasGenerateOrder = ReadBool(1, any);
            _isWin = ReadBool(2, any);
            //刷新弹脸信息
            _RefreshPopupInfo();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, _choiceType));
            any.Add(ToRecord(1, _hasGenerateOrder));
            any.Add(ToRecord(2, _isWin));
        }

        public override void WhenEnd()
        {
            //活动结束时打点
        }

        #region 界面 入口 换皮 弹脸

        public override ActivityVisual Visual => MainPopup.visual;
        public VisualPopup MainPopup { get; } = new(UIConfig.UIOrderDiffchoice); // 主界面

        public override void Open() => Open(MainPopup);

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            MainPopup.Setup(Conf.StartTheme, this);
        }
        
        //避免首次开启时SetupFresh和TryPopup都会走导致重复弹窗
        private bool _hasPopFirst = false;
        //目前是否可以弹窗(玩家没选择难度且时间在合适范围内)
        private bool _canPopup => !_hasJoin && Game.Instance.GetTimestampSeconds() <= endShowTS;
        
        private void _TryPopFirst()
        {
            if (!_canPopup)
                return;
            Game.Manager.screenPopup.TryQueue(MainPopup.popup, PopupType.Login);
            _hasPopFirst = true;
        }
        
        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (_hasPopFirst)
                return;
            if (!_canPopup)
                return;
            //玩家如果一直没选难度(没参与活动)，则每次登录都尝试弹窗
            MainPopup.Popup(popup_, state_);
        }

        #endregion

        #region 难度选择相关逻辑

        public enum DiffChoiceType
        {
            None = 0,   //未做选择
            Easy = 1,   //简单
            Normal = 2, //普通
            Hard = 3    //困难
        }
        
        public DiffChoiceType ChoiceType => (DiffChoiceType)_choiceType;
        private int _choiceType = (int)DiffChoiceType.None;
        private bool _hasJoin => (ChoiceType != DiffChoiceType.None);   //不为None时表示已加入活动

        //外部调用 尝试设置订单难度
        public bool TryChoiceDiff(DiffChoiceType type)
        {
            if (type == DiffChoiceType.None)
                return false;
            _choiceType = (int)type;
            return true;
        }

        #endregion

        #region 订单相关逻辑

        private bool _hasGenerateOrder; //记录是否已经生成订单
        private bool _isWin;            //记录订单是否成功完成
        
        private bool _CheckSlotReady(int slotId)
        {
            if (!Valid)
                return false;
            //玩家未选择难度
            if (!_hasJoin)
                return false;
            //已生成订单
            if (_hasGenerateOrder)
                return false;
            // 结束前一段时间内不能再刷新订单
            if (Game.Instance.GetTimestampSeconds() > endShowTS)
                return false;
            //检查当前选择的难度和slotId是否一致
            var orderSlotId = _GetOrderConfInfo()?.IntRandomer ?? -1;
            return orderSlotId == slotId;
        }

        private OrderDiffChoiceInfo _GetOrderConfInfo()
        {
            if (!Valid)
                return null;
            var infoId = ChoiceType switch
            {
                DiffChoiceType.Easy => Conf.EasyOrder,
                DiffChoiceType.Normal => Conf.MidOrder,
                DiffChoiceType.Hard => Conf.HardOrder,
                _ => 0
            };
            if (infoId <= 0)
                return null;
            return Game.Manager.configMan.GetOrderDiffChoiceInfoConfig(infoId);
        }

        bool IActivityOrderGenerator.TryGeneratePassiveOrder(OrderRandomer cfg, IOrderHelper helper, MergeWorldTracer tracer, Func<OrderRandomer, OrderData> builder, out OrderData order)
        {
            order = null;
            if (!_CheckSlotReady(cfg.Id))
                return false;
            order = builder?.Invoke(cfg);
            if (order == null)
                return false;
            order.OrderType = (int)OrderType.DiffChoice;
            order.Record.OrderType = order.OrderType;
            // 开始下一个限时订单
            var ret = _StartNextOrder((order as IOrderData).CalcRealDifficulty());
            var any = order.Record.Extra;
            any.Add(ToRecord((int)OrderParamType.EventId, ret.eventId));
            any.Add(ToRecord((int)OrderParamType.EventParam, ret.paramId));
            any.Add(ToRecord((int)OrderParamType.StartTimeSec, ret.startTime));
            any.Add(ToRecord((int)OrderParamType.DurationSec, ret.duration));
            DebugEx.Info($"ActivityOrderDiffChoice::TryGeneratePassiveOrder : {ret.eventId} {order.Id} {ret.duration}");
            return true;
        }

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (!(order as IOrderData).IsFlash)
                return false;
            if (order.GetValue(OrderParamType.EventId) != Id)
                return false;
            if (order.State == OrderState.Rewarded)
            {
                DebugEx.Info($"OrderProviderRandom::_RefreshOrderListImp ----> remove completed order {order.Id}");
                _OnOrderPassed(true);
            }
            else if (order.State == OrderState.Expired || (order as IOrderData).IsExpired)
            {
                _OnOrderPassed(false);
            }
            // 不改变order 始终返回false
            return false;
        }
        
        private (int eventId, int paramId, int startTime, int duration) _StartNextOrder(int realDifficulty)
        {
            var confInfo = _GetOrderConfInfo();
            var (life, method, _) = confInfo.LifeTime.ConvertToInt3();
            var orderStartTime = (int)Game.Instance.GetTimestampSeconds();
            var lifeTime = Game.Manager.rewardMan.CalcDynamicOrderLifeTime(method, life, realDifficulty);
            //取 根据难度计算出来的时间 和 当前活动剩余时间 二者的最小值作为订单的持续时间
            var duration = (int)Math.Min(lifeTime, Countdown);
            return (Id, Param, orderStartTime, duration);
        }

        private void _OnOrderPassed(bool isWin)
        {
            _isWin = isWin;
            //活动立即结束
            Game.Manager.activity.EndImmediate(this, false);
        }

        #endregion
    }
}