/*
 * @Author: tang.yan
 * @Description: 迷你棋盘-活动逻辑相关
 * @Date: 2024-08-06 15:08:45
 */

using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using FAT.Merge;
using static FAT.RecordStateHelper;
using EL.Resource;

namespace FAT {
    
    //迷你棋盘活动处理器 用于控制活动的创建及结束等逻辑
    //作为中间桥梁 解藕活动逻辑(MiniBoardActivity)和迷你棋盘核心逻辑(MiniBoardMan)
    public class MiniBoardActivityHandler : ActivityGroup 
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
                _TryCreateMiniBoardData(miniBoardAct, data_ == null);  //先创建棋盘数据
                option_.Apply(miniBoardAct);
                activity_.AddActive(id_, miniBoardAct, new_: data_ == null);    //再添加活动数据
                return (true, null);
            }
            return (false, rs);
        }

        private (bool, string) _TryCreateAct(LiteInfo lite_, EventType type_, out MiniBoardActivity miniBoardAct)
        {
            miniBoardAct = null;
            var miniBoardMan = Game.Manager.miniBoardMan;
            if (!miniBoardMan.IsUnlock)
                return (false, "not unlock");
            var miniBoardConfig = Game.Manager.configMan.GetEventMiniBoardConfig(lite_.param);
            if (miniBoardConfig == null)
                return (false, "miniBoard Config failed");
            var (r, rs) = ActivityLite.TryCreate(lite_, type_, out var lite);
            if (!r) return (r, rs);
            miniBoardAct = new MiniBoardActivity();
            miniBoardAct.Setup(lite, miniBoardConfig);
            return (true, null);
        }

        private void _TryCreateMiniBoardData(MiniBoardActivity activity, bool isNew)
        {
            var miniBoardMan = Game.Manager.miniBoardMan;
            //通知man 持有当前开启的活动 并初始化数据
            miniBoardMan.SetCurActivity(activity);
            miniBoardMan.InitMiniBoardData(isNew);
        }

        public override void End(Activity activity_, ActivityLike acti_, bool expire_)
        {
            //通知miniBoardMan  acti_ 结束了  清空活动 
            var miniBoardMan = Game.Manager.miniBoardMan;
            if (miniBoardMan.CurActivity.Id == acti_.Id)
            {
                miniBoardMan.ClearMiniBoardData();
                miniBoardMan.SetCurActivity(null);
            }
        }
    }
    
    //迷你棋盘活动数据管理类
    public class MiniBoardActivity : ActivityLike, IBoardEntry
    {
        public EventMiniBoard ConfD { get; private set; }
        public int DetailId { get; private set; }  //用户分层 区别棋盘配置 对应EventMiniBoardDetail.id
        public MiniBoardItemSpawnBonusHandler SpawnHandler { get; } = new();  //迷你棋盘棋子生成器
        public bool UIOpenState { get; set; }   //用于判断棋盘UI幕布动画状态
        public int UnlockMaxLevel; //当前解锁的最大等级棋子
        public override ActivityVisual Visual => BoardTheme;
        private bool _hasPop;

        #region EventTheme

        public ActivityVisual StartTheme = new(); //活动开启theme
        public ActivityVisual BoardTheme = new(); //棋盘theme
        public ActivityVisual EndTheme = new(); //活动结束theme
        public ActivityVisual RewardTheme = new(); //补领奖励theme
        public PopupMiniBoard StartPopup = new();
        public PopupMiniBoard EndPopup = new();
        public PopupActivity RewardPopup = new();
        public UIResAlt BoardResAlt = new UIResAlt(UIConfig.UIMiniBoard); //棋盘UI
        public UIResAlt RewardResAlt = new UIResAlt(UIConfig.UIMiniBoardReward); //补领奖励

        #endregion

        public void Setup(ActivityLite lite_, EventMiniBoard confD_) {
            Lite = lite_;
            ConfD = confD_;
            //注册迷你棋盘棋子生成器
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(SpawnHandler);
        }

        public override void SetupFresh() {
            DetailId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            //刷新弹脸信息
            _RefreshPopupInfo();
            StartPopup.option = new IScreenPopup.Option() { ignoreLimit = true };
            Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login);
            _hasPop = true;
        }
        
        public override void SaveSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            any.Add(ToRecord(0, DetailId));
            any.Add(ToRecord(1, UIOpenState));
            any.Add(ToRecord(2,UnlockMaxLevel));
        }

        public override void LoadSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            DetailId = ReadInt(0, any);
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
            foreach(var v in Visual.ResEnumerate()) yield return v;
        }

        public override void WhenEnd()
        {
            //取消注册迷你棋盘棋子生成器
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(SpawnHandler);
            if (!UIManager.Instance.IsOpen(BoardResAlt.ActiveR))
                Game.Manager.screenPopup.TryQueue(EndPopup, PopupType.Login);
            var reward = new List<RewardCommitData>();
            if (Game.Manager.miniBoardMan.CollectAllBoardReward(reward) && reward.Count > 0)
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
            Game.Manager.miniBoardMan.EnterMiniBoard();
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
        }

        public string BoardEntryAsset()
        {
            BoardTheme.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        public bool BoardEntryVisible => Game.Manager.miniBoardMan.IsValid;
    }
}