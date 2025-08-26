/*
 * @Author: tang.yan
 * @Description: 印章活动数据
 * @Doc: https://centurygames.feishu.cn/wiki/U4iKwKCdKihZdLkrZUbc3d8bnFe
 * @Date: 2025-01-17 16:01:33
 */
using System.Collections.Generic;
using Cysharp.Text;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using static FAT.RecordStateHelper;
using EL.Resource;

namespace FAT {
    //印章活动活动数据管理类
    public class ActivityStamp : ActivityLike
    {
        public EventStamp ConfD { get; private set; }
        public override bool Valid => Lite.Valid && ConfD != null;
        public override ActivityVisual Visual => PanelTheme;
        public ActivityVisual PanelTheme = new();       //界面theme
        public ActivityVisual NextRoundTheme = new();   //下一轮界面theme
        public PopupActivity StartPopup = new();        //第一次开启弹脸
        public PopupActivity NextRoundPopup = new();    //新一轮开启弹脸
        public UIResAlt PanelResAlt = new UIResAlt(UIConfig.UICardStamp); //界面UI
        
        private int _curRoundIndex = 0;   //当前正处于的轮次序号,默认从0开始,需要使用此序号去EventStamp.includeRoundId中去索引
        private int _curFinishIndex = 0;  //当前轮次中已经完成到的序号,默认从0开始,代表一个也没完成,之后每完成一个+1
        private bool _ignorePopup;

        public ActivityStamp() { }


        public ActivityStamp(ActivityLite lite_)
        {
            Lite = lite_;
            ConfD = Game.Manager.configMan.GetEventStampConfig(lite_.Param);
        }

        //获取当前轮次中已经完成到的序号
        public int GetCurFinishIndex()
        {
            return _curFinishIndex;
        }

        //外部调用需判空
        public EventStampRound GetCurRoundConfig()
        {
            return _GetTargetIndexRoundConfig(_curRoundIndex);
        }

        //外部传入objBasic.id，进行盖章Cost逻辑处理，返回是否处理成功
        //costCount默认1 代表要一次性cost几次
        public void TryExecuteCost(int objBasicId, int costCount = 1)
        {
            if (!Valid) return;
            var curRoundConf = GetCurRoundConfig();
            if (curRoundConf == null) return;
            if (!curRoundConf.Cost.Contains(objBasicId)) return;
            //完成的层数序号list
            var finishIndexList = new List<int>();
            //完成的层数和对应获得的奖励dict  key:层数序号 value:该层数对应的所有奖励list
            var finishRewardDict = new Dictionary<int, List<RewardCommitData>>();
            //最终序号为最大可盖章层数(达到此层数时直接发大奖大奖)
            var finalIndex = curRoundConf.Level;    
            var rewardMan = Game.Manager.rewardMan;
            //根据costCount递进当前已完成的序号，递进过程中如果有奖励则直接begin(目前逻辑上认为大奖位置不会配小奖)，当序号在配置范围内时，序号会递进
            for (var i = 0; i < costCount; i++)
            {
                var targetFinishIndex = _curFinishIndex + 1;
                var isBigReward = false;
                var itemInfoStr = "";
                if (targetFinishIndex < finalIndex) //发小奖
                {
                    foreach (var str in curRoundConf.GiftRewards)
                    {
                        var info = str.ConvertToInt3();
                        var giftIndex = info.Item3;
                        if (giftIndex == targetFinishIndex)
                        {
                            if (!finishRewardDict.ContainsKey(giftIndex))
                            {
                                finishRewardDict.Add(giftIndex, new List<RewardCommitData>());
                            }
                            finishRewardDict[giftIndex].Add(rewardMan.BeginReward(info.Item1, info.Item2, ReasonString.stamp));
                            if (itemInfoStr == "")
                                itemInfoStr = ZString.Concat(itemInfoStr, info.Item1, ":", info.Item2);
                            else
                                itemInfoStr = ZString.Concat(itemInfoStr, ",", info.Item1, ":", info.Item2);
                        }
                    }
                    finishIndexList.Add(targetFinishIndex);
                    _curFinishIndex++;
                }
                else if (targetFinishIndex == finalIndex)   //发大奖
                {
                    if (!finishRewardDict.ContainsKey(finalIndex))
                    {
                        finishRewardDict.Add(finalIndex, new List<RewardCommitData>());
                    }
                    foreach (var str in curRoundConf.LevelRewards)
                    {
                        var r = str.ConvertToRewardConfig();
                        if (r != null)
                        {
                            finishRewardDict[finalIndex].Add(rewardMan.BeginReward(r.Id, r.Count, ReasonString.stamp));
                            if (itemInfoStr == "")
                                itemInfoStr = ZString.Concat(itemInfoStr, r.Id, ":", r.Count);
                            else
                                itemInfoStr = ZString.Concat(itemInfoStr, ",", r.Id, ":", r.Count);
                        }
                    }
                    finishIndexList.Add(finalIndex);
                    _curFinishIndex++;
                    isBigReward = true;
                }
                else
                {
                    break;
                }
                if (!string.IsNullOrEmpty(itemInfoStr))
                {
                    //发奖时打点
                    DataTracker.event_stamp_reward.Track(this, isBigReward ? 1 : 0, itemInfoStr);
                }
                //每次盖章成功时打点
                DataTracker.event_stamp_collect.Track(this, _curFinishIndex, _curRoundIndex + 1, finalIndex, isBigReward);
            }
            var needNextPopup = false;
            var oldRoundIndex = _curRoundIndex;
            //已完成的序号递进完毕后，检查当前是否还有下一轮，有的话进入下一轮，没有的话活动直接结束
            if (_curFinishIndex == finalIndex)
            {
                if (_CheckHasNextRound())
                {
                    _curRoundIndex++;
                    _curFinishIndex = 0;
                    needNextPopup = true;
                }
                else
                {
                    Game.Manager.activity.EndImmediate(this, false);
                }
            }
            //每次cost成功后都会注册idle action 打开活动界面
            UIManager.Instance.RegisterIdleAction("ui_idle_stamp_cost_finish", 501, 
                () => _OpenUIStamp(this, finishIndexList, finishRewardDict, needNextPopup, oldRoundIndex));
        }
        
        //检查当前是否还有下一轮
        private bool _CheckHasNextRound()
        {
            var roundIdList = ConfD?.IncludeRoundId;
            if (roundIdList == null) 
                return false;
            return _curRoundIndex + 1 < roundIdList.Count;
        }

        //获取指定序号的轮次配置信息
        private EventStampRound _GetTargetIndexRoundConfig(int targetIndex)
        {
            var roundIdList = ConfD?.IncludeRoundId;
            if (roundIdList == null) return null;
            return roundIdList.TryGetByIndex(targetIndex, out var roundId) 
                ? Game.Manager.configMan.GetEventStampRoundConfig(roundId) 
                : null;
        }

        private void _OpenUIStamp(ActivityStamp act, List<int> finishIndexList, Dictionary<int, List<RewardCommitData>> finishRewardDict, bool needNextPopup, int showRoundIndex)
        {
            //活动结束后 不再打开界面
            if (act == null || !act.Active)
            {
                //奖励不会被commit 可能会出现负体力 出现这种情况时打个log点位 方便排查问题
                DataTracker.TrackLogInfo($"ActivityStamp want open UI, but end. Activity = {act?.Info3}");
                return;
            }
            var showRoundConfig = _GetTargetIndexRoundConfig(showRoundIndex);
            UIManager.Instance.OpenWindow(act.PanelResAlt.ActiveR, act, finishIndexList, finishRewardDict, showRoundConfig);
            if (needNextPopup)
            {
                //下一轮界面弹脸
                Game.Manager.screenPopup.TryQueue(act.NextRoundPopup, PopupType.Login);
            }
        }

        public override void SetupFresh() {
            //刷新弹脸信息
            _RefreshPopupInfo();
            //第一次开启时主动调用弹脸
            Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login);
            _ignorePopup = true;
        }
        
        public override void SaveSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            any.Add(ToRecord(0, _curRoundIndex));
            any.Add(ToRecord(1, _curFinishIndex));
        }

        public override void LoadSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            _curRoundIndex = ReadInt(0, any);
            _curFinishIndex = ReadInt(1, any);
            //刷新弹脸信息
            _RefreshPopupInfo();
        }
        
        public override IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (!Valid) yield break;
            foreach(var v in PanelTheme.ResEnumerate()) yield return v;
            foreach(var v in NextRoundTheme.ResEnumerate()) yield return v;
            foreach(var v in Visual.ResEnumerate()) yield return v;
        }

        public override void WhenEnd()
        {
            DataTracker.event_stamp_end.Track(this, _curFinishIndex, _curRoundIndex + 1);
        }
        
        public override void SetupClear() { }
        
        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            if (!_ignorePopup)
            {
                popup_.TryQueue(StartPopup, state_);
            }
        }

        public override void Open()
        {
            UIManager.Instance.OpenWindow(PanelResAlt.ActiveR, this);
        }

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            if (PanelTheme.Setup(ConfD.StampTheme, PanelResAlt))
                StartPopup.Setup(this, PanelTheme, PanelResAlt);
            if (NextRoundTheme.Setup(ConfD.NextRoundTheme, PanelResAlt))
                NextRoundPopup.Setup(this, NextRoundTheme, PanelResAlt);
        }
    }
}