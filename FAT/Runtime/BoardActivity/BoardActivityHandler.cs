/*
 * @Author: tang.yan
 * @Description: 棋盘类活动处理器 用于控制活动的创建及结束等逻辑
 * @Date: 2025-03-17 10:03:35
 */
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.rawdata.FeatureEntry;

namespace FAT
{

    //棋盘类活动处理器 用于控制活动的创建及结束等逻辑
    //作为中间桥梁 解藕活动逻辑和棋盘核心逻辑
    public class BoardActivityHandler : ActivityGroup, IUserDataHolder
    {
        //活动涉及到的棋盘数据 | 仅读档流程中用到一次
        private IDictionary<int, BoardActivityData> archivedBoards;

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            archivedBoards = archive.ClientData.PlayerGameData.BoardActivity?.BoardActivityDataMap;
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.BoardActivity ??= new BoardActivity();
            var map = Game.Manager.activity.map;
            foreach (var (_, pack) in map)
            {
                if (pack is IBoardArchive boardArchive)
                {
                    var merge = new fat.gamekitdata.Merge();
                    boardArchive.FillBoardData(merge);
                    data.BoardActivity.BoardActivityDataMap[(int)boardArchive.Feature] = new BoardActivityData()
                    {
                        BindActivityId = pack.Id,
                        Board = merge,
                    };
                }
            }
        }

        //尝试添加棋盘活动数据 可通过时间或trigger两种形式触发
        public override (bool, string) TryAdd(Activity activity_, (int, int) id_, EventType type_, ActivityInstance data_, in Option option_)
        {
            //若活动正在开启中 说明已经初始化过数据了 直接返回true
            if (activity_.IsActive(id_)) return (true, "active");
            //活动数据非法或者已经过期(开过一次)则直接返回false
            if (activity_.IsInvalid(id_, out var rsi) || activity_.IsExpire(id_)) return (false, rsi ?? "expire");
            //如果没有配置 返回false
            var (r, rs) = ActivityLite.ReadyToCreate(id_, option_, out var lite);
            if (!r) return activity_.Invalid(id_, $"{id_} not available reason:{rs}");
            //创建活动数据
            (r, rs) = _TryCreateAct(lite, type_, out var boardActivity);
            if (r)
            {
                if (!boardActivity.Valid) return activity_.Invalid(id_, $"failed to create instance for {id_}");
                boardActivity.LoadData(data_);
                _TryCreateBoardData(type_, boardActivity, data_ == null);  //先创建棋盘数据
                option_.Apply(boardActivity);
                activity_.AddActive(id_, boardActivity, new_: data_ == null);    //再添加活动数据
                return (true, null);
            }
            return (false, rs);
        }

        private (bool, string) _TryCreateAct(LiteInfo lite_, EventType type_, out ActivityLike boardActivity)
        {
            boardActivity = null;
            if (!_CheckIsUnlock(type_))
                return (false, $"BoardActivity: {type_} not unlock");
            var (r, rs) = ActivityLite.TryCreate(lite_, type_, out var lite);
            if (!r)
                return (false, rs);
            if (!_TryCreate(lite_, type_, lite, out boardActivity))
                return (false, $"BoardActivity: {type_} create fail");
            return (true, null);
        }

        private void _TryCreateBoardData(EventType type_, ActivityLike activity, bool isNew)
        {
            //通知manager 持有当前开启的棋盘活动 并初始化数据
            switch (type_)
            {
                case EventType.Mine:
                    Game.Manager.mineBoardMan.TryStart(activity, isNew);
                    break;
                default:
                    //棋盘活动的数据合法且不是第一次创建时才会读棋盘数据存档
                    if (!isNew && activity is IBoardArchive boardArchive)
                    {
                        if (archivedBoards.TryGetValue((int)boardArchive.Feature, out var data))
                        {
                            boardArchive.SetBoardData(data.Board);
                        }
                    }
                    break;
            }
        }

        public override void End(Activity activity_, ActivityLike acti_, bool expire_)
        {
            //通知manager  棋盘活动结束了  清空活动
            var type = acti_?.Type ?? EventType.Default;
            switch (type)
            {
                case EventType.Mine:
                    Game.Manager.mineBoardMan.TryEnd(acti_);
                    break;
                default:
                    break;
            }
        }

        private bool _CheckIsUnlock(EventType type_)
        {
            var feature = Game.Manager.featureUnlockMan;
            return type_ switch
            {
                EventType.Fish => feature.IsFeatureEntryUnlocked(FeatureFish),
                EventType.Mine => Game.Manager.mineBoardMan.IsUnlock,
                EventType.FarmBoard => feature.IsFeatureEntryUnlocked(FeatureFarmBoard),
                EventType.Fight => feature.IsFeatureEntryUnlocked(FeatureFight),
                EventType.WishBoard => feature.IsFeatureEntryUnlocked(FeatureWishBoard),
                _ => false
            };
        }

        private bool _TryCreate(LiteInfo lite_, EventType type_, ActivityLite lite, out ActivityLike boardActivity)
        {
            boardActivity = null;
            switch (type_)
            {
                case EventType.Fish: boardActivity = new ActivityFishing(lite); return true;
                case EventType.FarmBoard: boardActivity = new FarmBoardActivity(lite); return true;
                case EventType.Fight: boardActivity = new FightBoardActivity(lite); return true;
                case EventType.Mine:
                    var config = Game.Manager.configMan.GetEventMineConfig(lite_.param);
                    if (config != null)
                    {
                        boardActivity = new MineBoardActivity();
                        var act = (MineBoardActivity)boardActivity;
                        act.Setup(lite, config);
                        return true;
                    }
                    break;
                case EventType.WishBoard: boardActivity = new WishBoardActivity(lite); return true;
                default:
                    break;
            }
            return false;
        }
    }
}