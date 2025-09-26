/*
 * @Author: chaoran.zhang
 * @Date: 2025-08-25 11:46:28
 * @LastEditors: chaoran.zhang
 * @LastEditTime: 2025-09-19 16:13:08
 */
using System;
using System.Collections.Generic;
using System.Linq;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using FAT.MSG;
using static EL.MessageCenter;

namespace FAT
{
    public class TaskManager : IGameModule, IUserDataHolder
    {
        private string _lastSigninDate = "";
        private List<TaskGroup> _taskGroups = new();
        public void FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.TaskData ??= new TaskData();
            data.TaskData.TaskGroups.AddRange(_taskGroups);
            data.TaskData.LastOnline = _lastSigninDate;
        }

        public void SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.TaskData;
            if (data == null) { return; }
            _taskGroups.Clear();
            _taskGroups.AddRange(data.TaskGroups);
            _lastSigninDate = data.LastOnline;
            if (CheckSignInEnable())
            {
                _UpdateTask(TaskType.TaskLogIn, 1);
                _lastSigninDate = GetUtcOffsetTime().ToString("yyyy-MM-dd");
            }
        }

        public void LoadConfig()
        {
        }

        public void Reset()
        {
            Get<GAME_COIN_USE>().RemoveListener(_WhenCoinUse);
            Get<GAME_COIN_ADD>().RemoveListener(_WhenCoinAdd);
            Get<ORDER_FINISH_DATA>().RemoveListener(_WhenFinishOrder);
            Get<GAME_CARD_DRAW_FINISH>().RemoveListener(_WhenCardDraw);
            Get<GAME_BOARD_ITEM_MERGE>().RemoveListener(_WhenMerge);
            Get<GAME_MERGE_PRE_BEGIN_REWARD>().RemoveListener(_WhenTokenGet);
            Get<ON_USE_SPEED_UP_ITEM_SUCCESS>().RemoveListener(_WhenUnleashBubble);
            Get<GAME_CARD_ADD>().RemoveListener(_WhenCardAdd);
            Get<GAME_BOARD_ITEM_SKILL>().RemoveListener(_WhenBoardSkill);
            Get<TASK_PAY_SUCCESS>().RemoveListener(_WhenPaySuccess);
            Get<GAME_MERGE_ENERGY_CHANGE>().RemoveListener(_WhenEnergyCost);
            Get<TASK_BUILD_UPGRADE>().RemoveListener(_WhenBuildUpgrade);
            Get<TASK_COMPLETE_DAILY_TASK>().RemoveListener(_WhenDailyTaskComplete);
            Get<TASK_RACE_WIN>().RemoveListener(_WhenRaceWin);
            Get<TASK_SCORE_DUEL_WIN>().RemoveListener(_WhenScoreDuelWin);
            Get<TASK_ACTIVITY_TOKEN_USE>().RemoveListener(_WhenTokenUse);
        }


        public void Startup()
        {
            Get<GAME_COIN_USE>().AddListenerUnique(_WhenCoinUse);
            Get<GAME_COIN_ADD>().AddListenerUnique(_WhenCoinAdd);
            Get<ORDER_FINISH_DATA>().AddListenerUnique(_WhenFinishOrder);
            Get<GAME_CARD_DRAW_FINISH>().AddListenerUnique(_WhenCardDraw);
            Get<GAME_BOARD_ITEM_MERGE>().AddListenerUnique(_WhenMerge);
            Get<GAME_MERGE_PRE_BEGIN_REWARD>().AddListenerUnique(_WhenTokenGet);
            Get<ON_USE_SPEED_UP_ITEM_SUCCESS>().AddListenerUnique(_WhenUnleashBubble);
            Get<GAME_CARD_ADD>().AddListenerUnique(_WhenCardAdd);
            Get<GAME_BOARD_ITEM_SKILL>().AddListenerUnique(_WhenBoardSkill);
            Get<TASK_PAY_SUCCESS>().AddListenerUnique(_WhenPaySuccess);
            Get<GAME_MERGE_ENERGY_CHANGE>().AddListenerUnique(_WhenEnergyCost);
            Get<TASK_BUILD_UPGRADE>().AddListenerUnique(_WhenBuildUpgrade);
            Get<TASK_COMPLETE_DAILY_TASK>().AddListenerUnique(_WhenDailyTaskComplete);
            Get<TASK_RACE_WIN>().AddListenerUnique(_WhenRaceWin);
            Get<TASK_SCORE_DUEL_WIN>().AddListenerUnique(_WhenScoreDuelWin);
            Get<TASK_ACTIVITY_TOKEN_USE>().AddListenerUnique(_WhenTokenUse);
        }

        private void _UpdateTask(TaskType type, int num)
        {
            foreach (var group in _taskGroups)
            {
                if (!group.TaskProgress.TryGetByIndex((int)type, out var ret)) { continue; }
                group.TaskProgress[(int)type] += num;
            }
            Get<TASK_UPDATE>().Dispatch(type, num);
        }

        public void DebugUpdateTask(TaskType type, int num) => _UpdateTask(type, num);



        private void _WhenCoinUse(CoinChange change)
        {
            if (change.type == CoinType.MergeCoin && change.reason == ReasonString.undo_sell_item) { _UpdateTask(TaskType.Coin, -change.amount); }
            else if (change.type == CoinType.Gem) { _UpdateTask(TaskType.TaskDiamond, change.amount); }
        }

        private void _WhenCoinAdd(CoinChange change_)
        {
            if (change_.type == CoinType.MergeCoin) { _UpdateTask(TaskType.Coin, change_.amount); }
        }

        private void _WhenFinishOrder(IOrderData data)
        {
            if (data.OrderType != (int)OrderType.MagicHour) { _UpdateTask(TaskType.Order, 1); }
        }

        private void _WhenCardDraw()
        {
            _UpdateTask(TaskType.CardPack, 1);
        }

        private void _WhenMerge(Item item)
        {
            var world = Game.Manager.mergeBoardMan.activeWorld;
            if (world.activeBoard.boardId == Constant.MainBoardId || world.isEquivalentToMain) { _UpdateTask(TaskType.Merge, 1); }
        }

        private void _WhenBoardSkill(Item item, SkillType type)
        {
            if (type != SkillType.Upgrade) { return; }
            var world = Game.Manager.mergeBoardMan.activeWorld;
            if (world.activeBoard.boardId == Constant.MainBoardId) { _UpdateTask(TaskType.Merge, 1); }
        }

        private void _WhenUnleashBubble()
        {
            _UpdateTask(TaskType.Bubble, 1);
        }

        private void _WhenTokenGet(RewardCommitData data)
        {
            if (data.rewardType != ObjConfigType.ActivityToken) { return; }
            var tokenConf = Game.Manager.objectMan.GetTokenConfig(data.rewardId);
            switch (tokenConf.Feature)
            {
                case FeatureEntry.FeatureDecorate:
                    {
                        _UpdateTask(TaskType.TaskDecorationTokenGet, data.rewardCount);
                        break;
                    }
                case FeatureEntry.FeatureOrderLike:
                    {
                        _UpdateTask(TaskType.TaskOrderLike, data.rewardCount);
                        break;
                    }
            }
        }

        private void _WhenTokenUse(int id, int num)
        {
            var tokenConf = Game.Manager.objectMan.GetTokenConfig(id);
            switch (tokenConf.Feature)
            {
                case FeatureEntry.FeatureMine:
                    {
                        _UpdateTask(TaskType.TaskMineTokenCost, num);
                        break;
                    }
                case FeatureEntry.FeatureFarmBoard:
                    {
                        _UpdateTask(TaskType.TaskFarmTokenCost, num);
                        break;
                    }
            }
        }

        private void _WhenCardAdd(CardData data)
        {
            _UpdateTask(TaskType.TaskCardNum, 1);
            switch (data.GetConfig().Star)
            {
                case 2:
                    {
                        _UpdateTask(TaskType.Task2StarCard, 1);
                        break;
                    }
                case 3:
                    {
                        _UpdateTask(TaskType.Task3StarCard, 1);
                        break;
                    }
                case 4:
                    {
                        _UpdateTask(TaskType.Task4StarCard, 1);
                        break;
                    }
                case 5:
                    {
                        _UpdateTask(TaskType.Task5StarCard, 1);
                        break;
                    }
            }
        }

        private void _WhenPaySuccess()
        {
            _UpdateTask(TaskType.TaskPay, 1);
        }

        private void _WhenEnergyCost(int num)
        {
            if (num > 0) { return; }
            _UpdateTask(TaskType.TaskEnergy, -num);
        }

        private void _WhenBuildUpgrade()
        {
            _UpdateTask(TaskType.TaskBuild, 1);
        }

        private void _WhenDailyTaskComplete()
        {
            _UpdateTask(TaskType.TaskDaily, 1);
        }

        private void _WhenRaceWin()
        {
            _UpdateTask(TaskType.TaskHotAirBalloon, 1);
        }

        private void _WhenScoreDuelWin()
        {
            _UpdateTask(TaskType.TaskUfo, 1);
        }

        /// <summary>
        /// 获取当前UTC十点为新的一天的依据，判断当前日期
        /// </summary>
        /// <returns></returns>
        private DateTime GetUtcOffsetTime()
        {
            var utcTime = DateTime.UtcNow.AddSeconds(Game.Manager.networkMan.networkBias);
            var utcTime_offset = new DateTime(utcTime.Year, utcTime.Month, utcTime.Day, Game.Manager.configMan.globalConfig.PopupRefresh, 0, 0, DateTimeKind.Utc);
            if (utcTime < utcTime_offset)
                utcTime_offset = utcTime_offset.AddDays(-1);
            return utcTime_offset;
        }
        /// <summary>
        /// 检查是否可以签到
        /// </summary>
        /// <returns></returns>
        private bool CheckSignInEnable()
        {
            if (string.IsNullOrEmpty(_lastSigninDate))
                return true;
            DateTime lastSignin;
            if (!DateTime.TryParseExact(_lastSigninDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out lastSignin))
                return true;
            return lastSignin.Date != GetUtcOffsetTime().Date;
        }

        #region 接口 

        /// <summary>
        /// 注册任务数据组
        /// </summary>
        /// <param name="activityLike">绑定的活动</param>
        public void RegisterTaskGroup(ActivityLike activityLike)
        {
            var group = new TaskGroup();
            group.Activity = activityLike.Id;
            for (var i = 0; i < Enum.GetValues(typeof(TaskType)).Length; i++) { group.TaskProgress.Add(0); }
            _taskGroups.Add(group);
            group.TaskProgress[(int)TaskType.TaskLogIn] += 1;
        }

        public void UnRegisterTaskGroup(ActivityLike activityLike)
        {
            var group = _taskGroups.FirstOrDefault(it => it.Activity == activityLike.Id);
            while (group != null)
            {
                _taskGroups.Remove(group);
                group = _taskGroups.FirstOrDefault(it => it.Activity == activityLike.Id);
            }
        }

        /// <summary>
        /// 获取传入类型的任务累计进度
        /// </summary>
        /// <param name="activityLike"> 绑定的活动</param>
        /// <param name="type"> 类型</param>
        /// <returns></returns>
        public int FillTaskProgress(ActivityLike activityLike, TaskType type)
        {
            var group = _taskGroups.FirstOrDefault(it => it.Activity == activityLike.Id);
            if (group == null) { return 0; }
            group.TaskProgress.TryGetByIndex((int)type, out var ret);
            return ret;
        }

        #endregion
    }
}