/*
 * @Author: tang.yan
 * @Description: 三天签到活动数据类
 * @Doc: https://centurygames.feishu.cn/wiki/D2dfwjgZ8io9hJkT27XcqGptn9f
 * @Date: 2025-06-10 11:06:54
 */

using System;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using UnityEngine;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class ActivityThreeSign : ActivityLike
    {
        public override bool Valid => Lite.Valid && Conf != null;
        public EventThreeSign Conf { get; private set; }

        //目前累计签到天数 会进存档
        //需注意：本活动没有入口 只会在登录时以弹脸的形式打开一次，所以数据层在上线时就已经完成签到并发奖了
        public int TotalSignDay { get; private set; }
        //检查是否完成所有签到
        public bool IsSignAll => Valid && TotalSignDay >= Conf.Rewards.Count;
        //下次可签到的时间 用于检查当前时间点是否可以签到 默认-1代表无法签到 0代表还没签到 会进存档
        private long _nextSignTs = 0;

        //界面弹窗 因为不需要换皮 所以只用到popup
        private ThreeSignPopup _popup = new ThreeSignPopup();
        //界面Res
        private UIResAlt _res = new UIResAlt(UIConfig.UIThreeSign);

        //当前是否可以弹脸
        private bool canPopup = false;
        //记录弹脸时要传入的奖励数据
        private PoolMapping.Ref<List<RewardCommitData>> signReward;
        
        public ActivityThreeSign() { }

        
        public ActivityThreeSign(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = Game.Manager.configMan.GetEventThreeSignConfig(lite_.Param);
        }

        //执行时机：活动第一次触发时
        public override void SetupFresh()
        {
            //刷新弹脸信息
            _RefreshPopupInfo();
            //处理签到逻辑
            _ExecuteSign();
        }

        //执行时机：游戏中每隔5秒自动保存存档并发给服务器时
        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var tsO = TSOffset();
            any.Add(ToRecord(0, TotalSignDay));
            any.Add(ToRecord(1, _nextSignTs, tsO));
        }

        //执行时机：每次登录进游戏 加载游戏存档时
        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var tsO = TSOffset();
            TotalSignDay = ReadInt(0, any);
            _nextSignTs = ReadTS(1, tsO, any);
        }

        private long TSOffset() => Game.Timestamp(new DateTime(2024, 1, 1));

        public override (long, long) SetupTS(long sTS_, long eTS_)
        {
            if (sTS_ > 0) return (sTS_, eTS_);
            //活动第一次创建时 根据配置决定活动的最晚结束时间
            var sts = Lite.StartTS;
            var ets = Lite.StartTS + Conf.Lifetime;
            return (sts, ets);
        }

        public override void Open()
        {
            //实际上本活动没有入口 只会在登录时以弹脸的形式打开一次
        }

        public override void WhenActive(bool new_)
        {
            //第一次触发时走SetupFresh逻辑
            if (new_)
                return;
            //刷新弹脸信息
            _RefreshPopupInfo();
            //处理签到逻辑
            _ExecuteSign();
        }
        
        public override void TryPopup(ScreenPopup popup_, PopupType state_) 
        {
            if (state_ != PopupType.Login)
                return;
            _TryPopup();
        }

        public override void WhenEnd()
        {
            //活动结束时打点
            DataTracker.threesign_finish.Track(this, TotalSignDay, !IsSignAll);
        }

        public override void WhenReset()
        {
            canPopup = false;
        }

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            var popupId = Conf.PopupId;
            _popup.Setup(this, Game.Manager.configMan.GetPopupConfig(popupId), _res.ActiveR, popupId);
        }

        //处理签到逻辑
        private void _ExecuteSign()
        {
            if (!Valid)
                return;
            //如果已经全签完了 返回
            if (IsSignAll)
                return;
            //如果当前还没到下次可以签到的时间点或活动已经结束 则返回
            var curTs = Game.Instance.GetTimestampSeconds();
            if (curTs < _nextSignTs || curTs > endTS)
                return;
            //签到次数+1
            TotalSignDay++;
            //根据当前时间计算下次可以签到的时间
            var offsetHour = Game.Manager.configMan.globalConfig.UserRecordRefreshUtc;
            _nextSignTs = ((curTs - offsetHour * 3600) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + offsetHour * 3600;
            //领取当前的签到奖励，并弹窗
            if (Conf.Rewards.TryGetByIndex(TotalSignDay - 1, out var curRewardPoolId))
            {
                var rewardConf = Game.Manager.configMan.GetEventThreeSignPoolConfig(curRewardPoolId);
                if (rewardConf != null)
                {
                    signReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> signRewardList);
                    foreach (var reward in rewardConf.Pool)
                    {
                        var r = reward.ConvertToRewardConfig();
                        if (r != null)
                        {
                            var rewardData = Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.three_sign);
                            signRewardList.Add(rewardData);
                        }
                    }
                    canPopup = true;
                }
            }
            //签到时打点
            DataTracker.threesign_rwd.Track(this, TotalSignDay, IsSignAll);
            //检查是否完成了所有签到 是的话 直接主动结束活动
            if (IsSignAll)
            {
                //将活动结束时间设为当前+5 等下次CheckRefresh时自动结束
                //这么做的目的也是不得已。因为活动结束时也要弹窗，但目前流程上，弹窗时会过滤掉已经结束的活动，目前没有太好的时机，只能时延迟一下结束
                endTS = curTs + 5;
            }
        }

        private void _TryPopup()
        {
            if (!canPopup)
                return;
            if (signReward.Valid && signReward.obj.Count > 0)
            {
                //传入参数signReward 用于界面领奖表现 使用完后需自行回收进池
                Game.Manager.screenPopup.TryQueue(_popup, _popup.PopupState, signReward);
            }
            canPopup = false;
        }
    }

    public class ThreeSignPopup : IScreenPopup
    {
        private ActivityLike _activity;

        public void Setup(ActivityLike activity, Popup popupConf, UIResource uiResource, int popupId)
        {
            _activity = activity;
            PopupConf = popupConf;
            PopupRes = uiResource;
            PopupId = popupId;
        }

        public override bool OpenPopup()
        {
            UIManager.Instance.OpenWindow(PopupRes, _activity, Custom);
            Custom = null;
            DataTracker.event_popup.Track(_activity);
            return true;
        }
    }
}
