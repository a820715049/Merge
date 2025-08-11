/*
 * @Author: tang.yan
 * @Description: 挖矿棋盘-活动逻辑相关  
 * @Date: 2025-03-07 10:03:18
 */

using System.Collections.Generic;
using Cysharp.Text;
using fat.rawdata;
using fat.gamekitdata;
using static FAT.RecordStateHelper;
using EL.Resource;
using static fat.conf.Data;
using EL;
using FAT.Merge;

namespace FAT
{
    //挖矿棋盘活动数据管理类
    public class MineBoardActivity : ActivityLike, IBoardEntry, IActivityOrderHandler
    {
        public EventMine ConfD { get; private set; }
        public int GroupId { get; private set; }  //用户分层 区别棋盘配置 对应EventMineGroup.id
        public int UnlockMaxLevel; //从0开始 当前合成链中已解锁的最大等级棋子
        public override ActivityVisual Visual => StartTheme;
        private bool _hasPop;

        private int _progressPhase; //当前进度条所处阶段 从0开始 根据阶段值读配置获取当前的最大进度以及达成后可获得的奖励
        private int _progressNum;   //当前进度条的进度值
        private int _tokenNum;      //当前代币数量

        #region EventTheme

        public ActivityVisual StartTheme = new(); //活动开启theme
        public ActivityVisual BoardTheme = new(); //棋盘theme
        public ActivityVisual EndTheme = new(); //活动结束theme
        public ActivityVisual RewardTheme = new(); //补领奖励theme
        public ActivityVisual HelpTheme = new(); //帮助界面theme
        public ActivityVisual HandBookTheme = new(); //图鉴界面theme
        public ActivityVisual LoadingTheme = new(); //loading界面theme
        public ActivityVisual BannerTheme = new();  //庆祝横幅界面theme
        public ActivityVisual MilestoneTheme = new();  //阶段奖励界面theme

        public PopupActivity StartPopup = new();
        public PopupActivity EndPopup = new();
        public PopupActivity RewardPopup = new();

        public UIResAlt StartResAlt = new UIResAlt(UIConfig.UIMineBoardStartNotice); //活动开启theme
        public UIResAlt EndResAlt = new UIResAlt(UIConfig.UIMineBoardEndNotice); //活动结束theme
        public UIResAlt BoardResAlt = new UIResAlt(UIConfig.UIMineBoardMain); //棋盘UI
        public UIResAlt RewardResAlt = new UIResAlt(UIConfig.UIMineBoardReplacement); //补领奖励
        public UIResAlt HelpResAlt = new UIResAlt(UIConfig.UIMineBoardHelp); //帮助界面
        public UIResAlt HandBookResAlt = new UIResAlt(UIConfig.UIMineHandbook);
        public UIResAlt MilestoneResAlt = new UIResAlt(UIConfig.UIMineBoardMilestone);
        public UIResAlt LoadingResAlt = new UIResAlt(UIConfig.UIMineLoading);
        public UIResAlt BannerResAlt = new UIResAlt(UIConfig.UIMineBoardMilestoneTips);

        #endregion

        //外部调用需判空
        public EventMineGroup GetCurGroupConfig()
        {
            return Game.Manager.configMan.GetEventMineGroupConfig(GroupId);
        }

        public void Setup(ActivityLite lite_, EventMine confD_)
        {
            Lite = lite_;
            ConfD = confD_;
        }

        public override void SetupFresh()
        {
            GroupId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            //添加初始代币
            _InitStartToken();
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新积分信息
            _RefreshScoreEntity();
            StartPopup.option = new IScreenPopup.Option() { ignoreLimit = true };
            Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login);
            _hasPop = true;
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, GroupId));
            any.Add(ToRecord(1, UnlockMaxLevel));
            any.Add(ToRecord(2, _progressPhase));
            any.Add(ToRecord(3, _progressNum));
            any.Add(ToRecord(4, _tokenNum));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            GroupId = ReadInt(0, any);
            UnlockMaxLevel = ReadInt(1, any);
            _progressPhase = ReadInt(2, any);
            _progressNum = ReadInt(3, any);
            _tokenNum = ReadInt(4, any);
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新积分信息
            _RefreshScoreEntity();
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach (var v in StartTheme.ResEnumerate()) yield return v;
            foreach (var v in BoardTheme.ResEnumerate()) yield return v;
            foreach (var v in EndTheme.ResEnumerate()) yield return v;
            foreach (var v in RewardTheme.ResEnumerate()) yield return v;
            foreach (var v in HelpTheme.ResEnumerate()) yield return v;
            foreach (var v in HandBookTheme.ResEnumerate()) yield return v;
            foreach (var v in LoadingTheme.ResEnumerate()) yield return v;
            foreach (var v in BannerTheme.ResEnumerate()) yield return v;
            foreach (var v in MilestoneTheme.ResEnumerate()) yield return v;
            foreach (var v in Visual.ResEnumerate()) yield return v;
        }

        public override void WhenEnd()
        {
            var conf = GetCurGroupConfig();
            if (conf != null)
            {
                //回收剩余代币
                using var _ = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<int, int> map);
                map[ConfD.TokenId] = _tokenNum;
                var tokenReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> tokenRewardList);
                ActivityExpire.ConvertToReward(conf.ExpireItem, tokenRewardList, ReasonString.mine_end_token_energy, map);
                Game.Manager.screenPopup.TryQueue(EndPopup, PopupType.Login, tokenReward);
                //回收棋盘上未使用的奖励棋子
                var boardReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> boardRewardList);
                if (Game.Manager.mineBoardMan.CollectAllBoardReward(boardRewardList) && boardRewardList.Count > 0)
                    Game.Manager.screenPopup.TryQueue(RewardPopup, PopupType.Login, boardReward);
                else
                    boardReward.Free();
            }
            _scoreEntity.Clear();
        }

        public override void WhenReset()
        {
            _scoreEntity.Clear();
        }

        public override void SetupClear()
        {
            base.SetupClear();
            ConfD = null;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            //if(!_hasPop)
            //popup_.TryQueue(StartPopup, state_);
        }

        public override void Open()
        {
            //打开活动面板
            Game.Manager.mineBoardMan.EnterMineBoard();
        }

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            if (StartTheme.Setup(ConfD.EventTheme, StartResAlt))
                StartPopup.Setup(this, StartTheme, StartResAlt);
            if (EndTheme.Setup(ConfD.EndTheme, EndResAlt))
                EndPopup.Setup(this, EndTheme, EndResAlt, false, false);
            if (RewardTheme.Setup(ConfD.EndRewardTheme, RewardResAlt))
                RewardPopup.Setup(this, RewardTheme, RewardResAlt, false, false);
            BoardTheme.Setup(ConfD.BoardTheme, BoardResAlt);
            HelpTheme.Setup(ConfD.HelpTheme, HelpResAlt);
            HandBookTheme.Setup(ConfD.HandbookTheme, HandBookResAlt);
            LoadingTheme.Setup(ConfD.LoadingTheme, LoadingResAlt);
            BannerTheme.Setup(ConfD.BannerTheme, BannerResAlt);
            MilestoneTheme.Setup(ConfD.MilestoneTheme, MilestoneResAlt);
        }

        public string BoardEntryAsset()
        {
            BoardTheme.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        public bool BoardEntryVisible => Game.Manager.mineBoardMan.IsValid;

        #region 棋子使用token、进度值token

        public bool IsMileStoneToken(int id)
        {
            if (ConfD == null)
                return false;
            foreach (var tokenId in ConfD.TokenMilestoneId)
            {
                if (tokenId == id)
                    return true;
            }
            return false;
        }

        public void TryAddToken(int id, int num, ReasonString reason)
        {
            if (ConfD == null || id <= 0 || num <= 0)
                return;
            if (ConfD.TokenId == id)
            {
                if (ChangeItemToken(true, num) && reason != ReasonString.mine_order_gettoken)
                {
                    //非完成订单获得货币时 单独向ScoreEntity中同步最新分数  并单独打点token_change
                    _scoreEntity.UpdateScore(_tokenNum);
                    DataTracker.token_change.Track(id, num, _tokenNum, reason);
                }
            }
            else if (IsMileStoneToken(id))
            {
                TryAddProgressNum(id, num, reason);
            }
        }

        public bool TryUseToken(int id, int num, ReasonString reason)
        {
            if (ConfD == null || id <= 0 || num <= 0)
                return false;
            if (ConfD.TokenId == id)
            {
                if (ChangeItemToken(false, num))
                {
                    DataTracker.token_change.Track(id, -num, _tokenNum, reason);
                    return true;
                }
            }
            return false;
        }

        private bool ChangeItemToken(bool isAdd, int changNum)
        {
            if (isAdd && changNum > 0)
            {
                _tokenNum += changNum;
                MessageCenter.Get<MSG.GAME_MINE_BOARD_TOKEN_CHANGE>().Dispatch(changNum, ConfD.TokenId);
                return true;
            }
            if (!isAdd)
            {
                if (_tokenNum >= changNum)
                {
                    _tokenNum -= changNum;
                    MessageCenter.Get<MSG.GAME_MINE_BOARD_TOKEN_CHANGE>().Dispatch(-changNum, ConfD.TokenId);
                    return true;
                }
                else
                {
                    var tokenIcon = UIUtility.FormatTMPString(ConfD.TokenId);
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.MineNoToken, tokenIcon);
                    return false;
                }
            }
            return false;
        }

        public int GetTokenNum()
        {
            return _tokenNum;
        }

        public int GetCurProgressPhase()
        {
            return _progressPhase;
        }

        public int GetCurProgressNum()
        {
            return _progressNum;
        }

        //获取当前进度条信息
        public EventMineReward GetProgressInfo(int progressPhase)
        {
            var allProgressInfo = GetCurGroupConfig()?.MilestoneRewardId;
            //所有进度值都完成
            if (allProgressInfo == null || progressPhase >= allProgressInfo.Count || progressPhase < 0)
                return null;
            return GetEventMineReward(allProgressInfo[progressPhase]);
        }

        //检查所有进度条是否都完成
        public bool CheckProgressFinish()
        {
            var allProgressInfo = GetCurGroupConfig()?.MilestoneRewardId;
            //所有进度值都完成
            return allProgressInfo == null || _progressPhase >= allProgressInfo.Count;
        }

        //外部调用增加当前进度值
        private void TryAddProgressNum(int tokenId, int tokenNum, ReasonString reason)
        {
            //获取当前阶段对应的进度信息
            var curProgressInfo = GetProgressInfo(_progressPhase);
            if (curProgressInfo == null)
                return;
            //增加进度值
            var finalProgressNum = _progressNum + tokenNum;
            DataTracker.token_change.Track(tokenId, tokenNum, finalProgressNum, reason);
            //检测是否达到本阶段最大值
            var curProgressMax = curProgressInfo.Milestone;
            //未达到最大值 只播放进度条增长动画
            if (finalProgressNum < curProgressMax)
            {
                _progressNum = finalProgressNum;
                MessageCenter.Get<MSG.GAME_MINE_BOARD_PROG_CHANGE>().Dispatch(finalProgressNum, default, -1);
                return;
            }
            //若达到则发阶段奖励且阶段值+1
            //这里默认单次加的进度值不会一下完成多段进度
            finalProgressNum -= curProgressMax; //此时finalProgressNum代表多余的进度值
            var listT = PoolMapping.PoolMappingAccess.Take<List<RewardCommitData>>(out var list);
            var rewardStr = "";
            foreach (var r in curProgressInfo.MilestoneReward)
            {
                if (rewardStr == "")
                    rewardStr = ZString.Concat(rewardStr, r);
                else
                    rewardStr = ZString.Concat(rewardStr, ",", r);
                var reward = r.ConvertToRewardConfig();
                if (reward != null)
                {
                    var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.mine_milestone_reward);
                    list.Add(commit);
                }
            }
            //进度条动画  进度条满时发奖流程  发完奖后若还有多余的进度值，剩余的进度值也要有动画
            MessageCenter.Get<MSG.GAME_MINE_BOARD_PROG_CHANGE>().Dispatch(finalProgressNum, listT, curProgressMax);
            //领取进度条奖励时打点
            var curGroupConf = GetCurGroupConfig();
            var allProgressInfo = curGroupConf.MilestoneRewardId;
            var isFinal = _progressPhase == allProgressInfo.Count - 1;
            Game.Manager.mineBoardMan.TrackMineMilestone(this, _progressPhase + 1, allProgressInfo.Count, curGroupConf.Diff, isFinal, rewardStr);
            //递进
            _progressPhase++;
            _progressNum = finalProgressNum;
        }

        //根据配置设置初始代币数量
        private void _InitStartToken()
        {
            var conf = GetCurGroupConfig();
            if (conf == null || ConfD == null) return;
            TryAddToken(ConfD.TokenId, conf.TokenNum, ReasonString.mine_start);
        }

        #endregion

        #region 主棋盘订单右下角积分

        private ScoreEntity _scoreEntity = new();

        private void _RefreshScoreEntity()
        {
            if (ConfD == null)
                return;
            var conf = GetCurGroupConfig();
            if (conf != null)
            {
                //无需监听_scoreEntity内部的SCORE_ENTITY_ADD_COMPLETE消息
                //因为所有积分发放最后都会走BeginReward, 而本活动内部处理了积分变化的逻辑 
                _scoreEntity.Setup(_tokenNum, this, ConfD.TokenId, conf.ExtraScore,
                    ReasonString.mine_order_gettoken, "", Constant.MainBoardId, false);
            }
        }

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            //积分类活动在计算订单积分时，都排除心想事成订单
            if ((order as IOrderData).IsMagicHour)
                return false;
            var changed = false;
            var state = order.GetState((int)OrderParamType.ScoreEventIdBR);
            // 没有积分 or 不是同一期活动时给这个订单生成右下角积分
            if (state == null || state.Value != Id)
            {
                changed = true;
                _scoreEntity.CalcOrderScoreBR(order, tracer);
            }
            return changed;
        }

        #endregion
    }

    public class MineBoardEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => e;
        private readonly ListActivity.Entry e;
        private readonly MineBoardActivity p;
        public MineBoardEntry(ListActivity.Entry e_, MineBoardActivity p_)
        {
            (e, p) = (e_, p_);
            e.dot.SetActive(p.GetTokenNum() > 0);
            e.dotCount.gameObject.SetActive(p.GetTokenNum() > 0);
            e.dotCount.SetText(p.GetTokenNum().ToString());
        }

        public void RefreshDot(MineBoardActivity activity)
        {
            if (activity != p) return;
            e.dot.SetActive(p.GetTokenNum() > 0);
            e.dotCount.gameObject.SetActive(p.GetTokenNum() > 0);
            e.dotCount.SetText(p.GetTokenNum().ToString());
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }
    }
}