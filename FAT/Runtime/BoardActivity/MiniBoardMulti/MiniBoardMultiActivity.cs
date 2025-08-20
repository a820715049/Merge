/*
 * @Author: tang.yan
 * @Description: 多轮迷你棋盘-活动逻辑相关 
 * @Date: 2025-01-02 15:01:09
 */

using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;
using FAT.Merge;
using static FAT.RecordStateHelper;
using EL.Resource;
using FAT.MSG;

namespace FAT {
    
    //多轮迷你棋盘活动处理器 用于控制活动的创建及结束等逻辑
    //作为中间桥梁 解藕活动逻辑(MiniBoardMultiActivity)和迷你棋盘核心逻辑(MiniBoardMultiMan)
    public class MiniBoardMultiActivityHandler : ActivityGroup 
    {
        //尝试添加迷你棋盘活动数据 可通过时间或trigger两种形式触发
        public override (bool, string) TryAdd(Activity activity_, (int, int) id_, EventType type_, ActivityInstance data_, in Option option_) {
            //若活动正在开启中 说明已经初始化过数据了 直接返回true
            if (activity_.IsActive(id_)) return (true, "active");
            //活动数据非法或者已经过期(开过一次)则直接返回false
            if (activity_.IsInvalid(id_, out var rsi) || activity_.IsExpire(id_)) return (false, rsi ?? "expire");
            //如果没有配置 返回false
            var (r, rs) = ActivityLite.ReadyToCreate(id_, option_, out var lite);
            if (!r) return activity_.Invalid(id_, $"{id_} not available reason:{rs}");
            //创建活动数据
            (r, rs) = _TryCreateAct(lite, type_, out var miniBoardAct);
            if (r)
            {
                if (!miniBoardAct.Valid) return activity_.Invalid(id_, $"failed to create instance for {id_}");
                miniBoardAct.LoadData(data_);
                _TryCreateMiniBoardMultiData(miniBoardAct, data_ == null);  //先创建棋盘数据
                option_.Apply(miniBoardAct);
                activity_.AddActive(id_, miniBoardAct, new_: data_ == null);    //再添加活动数据
                return (true, null);
            }
            return (false, rs);
        }

        private (bool, string) _TryCreateAct(LiteInfo lite_, EventType type_, out MiniBoardMultiActivity miniBoardAct)
        {
            miniBoardAct = null;
            var miniBoardMultiMan = Game.Manager.miniBoardMultiMan;
            if (!miniBoardMultiMan.IsUnlock)
                return (false, "miniBoardMulti not unlock");
            var miniBoardConfig = Game.Manager.configMan.GetEventMiniBoardMultiConfig(lite_.param);
            if (miniBoardConfig == null)
                return (false, "miniBoardMulti Config failed");
            var (r, rs) = ActivityLite.TryCreate(lite_, type_, out var lite);
            if (!r) return (r, rs);
            miniBoardAct = new MiniBoardMultiActivity();
            miniBoardAct.Setup(lite, miniBoardConfig);
            return (true, null);
        }

        private void _TryCreateMiniBoardMultiData(MiniBoardMultiActivity activity, bool isNew)
        {
            var miniBoardMultiMan = Game.Manager.miniBoardMultiMan;
            //通知man 持有当前开启的活动 并初始化数据
            miniBoardMultiMan.SetCurActivity(activity);
            miniBoardMultiMan.InitMiniBoardData(isNew);
        }

        public override void End(Activity activity_, ActivityLike acti_, bool expire_)
        {
            //通知miniBoardMultiMan  acti_ 结束了  清空活动 
            var miniBoardMultiMan = Game.Manager.miniBoardMultiMan;
            if (miniBoardMultiMan.CurActivity.Id == acti_.Id)
            {
                miniBoardMultiMan.ClearMiniBoardData();
                miniBoardMultiMan.SetCurActivity(null);
            }
        }
    }
    
    //迷你棋盘活动数据管理类
    public class MiniBoardMultiActivity : ActivityLike, IBoardEntry
    {
        public EventMiniBoardMulti ConfD { get; private set; }
        public int GroupId { get; private set; }  //用户分层 区别棋盘配置 对应EventMiniBoardMultiGroup.id
        public MiniBoardMultiBonusHandler SpawnHandler { get; } = new();  //迷你棋盘棋子生成器
        public bool UIOpenState { get; set; }   //用于判断棋盘UI幕布动画状态
        public int UnlockMaxLevel; //从0开始 当前合成链中已解锁的最大等级棋子，可能会超过目前可显示的棋子等级，判断时别直接用等于判断
        public override ActivityVisual Visual => BoardTheme;
        private bool _hasPop;

        #region EventTheme

        public ActivityVisual StartTheme = new(); //活动开启theme
        public ActivityVisual BoardTheme = new(); //棋盘theme
        public ActivityVisual EndTheme = new(); //活动结束theme
        public ActivityVisual RewardTheme = new(); //补领奖励theme
        public ActivityVisual NextTheme = new(); //下一轮弹窗theme
        public ActivityVisual HelpTheme = new(); //帮助界面theme
        public ActivityVisual GetKeyTheme = new();
        public PopupMiniBoardMulti StartPopup = new();
        public PopupMiniBoardMulti EndPopup = new();
        public PopupActivity RewardPopup = new();
        public UIResAlt BoardResAlt = new UIResAlt(UIConfig.UIMiniBoardMulti); //棋盘UI
        public UIResAlt RewardResAlt = new UIResAlt(UIConfig.UIMiniBoardMultiReward); //补领奖励
        public UIResAlt NextRoundResAlt = new UIResAlt(UIConfig.UIMiniBoardMultiNextRound); //下一轮弹窗
        public UIResAlt HelpResAlt = new UIResAlt(UIConfig.UIMiniBoardMultiHelp); //帮助界面
        public UIResAlt GetKeyResAlt = new UIResAlt(UIConfig.UIMiniBoardMultiFly);

        #endregion

        public void Setup(ActivityLite lite_, EventMiniBoardMulti confD_) {
            Lite = lite_;
            ConfD = confD_;
            //注册迷你棋盘棋子生成器
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(SpawnHandler);
        }

        public override void SetupFresh() {
            GroupId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            //刷新弹脸信息
            _RefreshPopupInfo();
            StartPopup.option = new IScreenPopup.Option() { ignoreLimit = true };
            Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login);
            _hasPop = true;
        }
        
        public override void SaveSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            any.Add(ToRecord(0, GroupId));
            any.Add(ToRecord(1, UIOpenState));
            any.Add(ToRecord(2,UnlockMaxLevel));
        }

        public override void LoadSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            GroupId = ReadInt(0, any);
            UIOpenState = ReadBool(1, any);
            UnlockMaxLevel = ReadInt(2, any);
            //刷新弹脸信息
            _RefreshPopupInfo();
        }
        
        public override IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (!Valid) yield break;
            foreach(var v in StartTheme.ResEnumerate()) yield return v;
            foreach(var v in BoardTheme.ResEnumerate()) yield return v;
            foreach(var v in EndTheme.ResEnumerate()) yield return v;
            foreach(var v in RewardTheme.ResEnumerate()) yield return v;
            foreach(var v in NextTheme.ResEnumerate()) yield return v;
            foreach(var v in HelpTheme.ResEnumerate()) yield return v;
            foreach(var v in Visual.ResEnumerate()) yield return v;
        }

        public override void WhenEnd()
        {
            //取消注册迷你棋盘棋子生成器
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(SpawnHandler);
            if (!UIManager.Instance.IsOpen(BoardResAlt.ActiveR))
                Game.Manager.screenPopup.TryQueue(EndPopup, PopupType.Login);
            var reward = new List<RewardCommitData>();
            if (Game.Manager.miniBoardMultiMan.CollectAllBoardReward(reward) && reward.Count > 0)
                Game.Manager.screenPopup.TryQueue(RewardPopup, PopupType.Login, reward);
        }
        
        public override void SetupClear() {
            base.SetupClear();
            ConfD = null;
        }
        
        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            if(!_hasPop)
                popup_.TryQueue(StartPopup, state_);
        }

        public override void Open()
        {
            //打开活动面板
            Game.Manager.miniBoardMultiMan.EnterMiniBoard();
        }

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            if (StartTheme.Setup(ConfD.EventTheme, BoardResAlt))
                StartPopup.Setup(this, StartTheme, BoardResAlt);
            if (EndTheme.Setup(ConfD.EndTheme, BoardResAlt))
                EndPopup.Setup(this, EndTheme, BoardResAlt, false, false);
            if (RewardTheme.Setup(ConfD.EndRewardTheme, RewardResAlt))
                RewardPopup.Setup(this, RewardTheme, RewardResAlt, false, false);
            BoardTheme.Setup(ConfD.BoardTheme, BoardResAlt);
            NextTheme.Setup(ConfD.BoardNextTheme, NextRoundResAlt);
            HelpTheme.Setup(ConfD.HelpTheme, HelpResAlt);
            GetKeyTheme.Setup(ConfD.GetKeyTheme, GetKeyResAlt);
        }

        public string BoardEntryAsset()
        {
            BoardTheme.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        public bool BoardEntryVisible => Game.Manager.miniBoardMultiMan.IsValid;
    }
    
    public class MiniBoardMultiEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => e;
        private readonly ListActivity.Entry e;
        private readonly MiniBoardMultiActivity p;
        public MiniBoardMultiEntry(ListActivity.Entry e_, MiniBoardMultiActivity p_)
        {
            (e, p) = (e_, p_);
            e.dot.SetActive(Game.Manager.miniBoardMultiMan.World?.rewardCount > 0);
            e.dotCount.gameObject.SetActive(Game.Manager.miniBoardMultiMan.World?.rewardCount > 0);
            var rewardCount = Game.Manager.miniBoardMultiMan.World?.rewardCount ?? 0;
            e.dotCount.SetRedPoint(rewardCount);
            MessageCenter.Get<CHECK_MINI_BOARD_MULTI_ENTRY_RED_POINT>().AddListenerUnique(RefreshDot);
        }

        public void RefreshDot(MiniBoardMultiActivity activity)
        {
            if(activity != p) return;
            e.dot.SetActive(Game.Manager.miniBoardMultiMan.World?.rewardCount > 0);
            e.dotCount.gameObject.SetActive(Game.Manager.miniBoardMultiMan.World?.rewardCount > 0);
            var rewardCount = Game.Manager.miniBoardMultiMan.World?.rewardCount ?? 0;
            e.dotCount.SetRedPoint(rewardCount);
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