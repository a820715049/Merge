/*
 * @Author: tang.yan
 * @Description: 体力列表礼包数据类
 * @Doc: https://centurygames.feishu.cn/wiki/DtcIwcN8HiRylyk2eVtc1Eg3nAf
 * @Date: 2025-04-07 16:04:25
 */

using System;
using System.Collections.Generic;
using Config;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using UnityEngine;
using static fat.conf.Data;
using static FAT.RecordStateHelper;

namespace FAT {
    public class PackErgList : GiftPack {
        
        //配置数据
        private ErgListPack _packConf;
        //用户分层 对应ErgListDetail.id 进存档
        private int _detailId;
        //目前是否已购买礼包权限
        public bool HasBuy => BuyCount > 0;
        //是否可以在首次完成任务时弹窗 每个活动生命周期内只弹一次 进存档
        private int _canPop = 0;
        //红点
        public bool HasRP => _hasFinishTaskRP || _hasRewardRP;
        private bool _hasFinishTaskRP = false;   //未购买礼包时 每次有新完成任务时入口提醒红点，打开界面后红点消失 进存档
        private bool _hasRewardRP = false;  //购买礼包后 存在可领奖励时入口提示红点，领取后消失
        
        public override UIResAlt Res { get; } = new(UIConfig.UIErgListPack);
        public override int PackId { get; set; }
        public override int ThemeId => _packConf.EventTheme;
        public override int StockTotal => -1;    //限购次数默认设成-1次 表示购买成功后不会立即结束
        
        public ActivityVisual RewardTheme = new(); //补领奖励theme
        public PopupActivity RewardPopup = new();
        public UIResAlt RewardResAlt = new UIResAlt(UIConfig.UIErgListPackEnd); //补领奖励

        //外部调用需判空
        public ErgListDetail GetCurDetailConfig()
        {
            return GetErgListDetail(_detailId);
        }
        
        public PackErgList(ActivityLite lite_)
        {
            Lite = lite_;
            _packConf = GetErgListPack(lite_.Param);
            _InitTaskProgressValue();
            RefreshTheme(popupCheck_:true);
            AddListener();
        }

        public override void SetupFresh()
        {
            _InitInfoWithUserGrade();
            _InitTaskDataList();
            //刷新弹脸信息
            _RefreshPopupInfo();
        }
        
        public override void SaveSetup(ActivityInstance data_) {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            var startIndex = 3;
            any.Add(ToRecord(startIndex++, _detailId));
            any.Add(ToRecord(startIndex++, _canPop));
            any.Add(ToRecord(startIndex++, _hasFinishTaskRP));
            foreach (var taskData in _ergTaskDataList)
            {
                any.Add(ToRecord(startIndex++, taskData.Id));
                any.Add(ToRecord(startIndex++, taskData.RequireCount));
                any.Add(ToRecord(startIndex++, taskData.State));
            }
            //最后处理_progressValue 以应对后续加类型
            foreach (var value in _progressValue)
            {
                any.Add(ToRecord(startIndex++, value));
            }
        }

        public override void LoadSetup(ActivityInstance data_) {
            base.LoadSetup(data_);
            var any = data_.AnyState;
            var startIndex = 3;
            if (any.Count > startIndex)
            {
                _detailId = ReadInt(startIndex++, any);
                _canPop = ReadInt(startIndex++, any);
                _hasFinishTaskRP = ReadBool(startIndex++, any);
                var detailConf = GetCurDetailConfig();
                if (detailConf == null)
                    return;
                var taskCount = detailConf.TaskInfo.Count;
                for (var i = 0; i < taskCount; i++)
                {
                    var id = ReadInt(startIndex++, any);
                    var requireCount = ReadInt(startIndex++, any);
                    var state = ReadInt(startIndex++, any);
                    var taskData = new ErgTaskData();
                    taskData.InitById(id, requireCount, state, Id);
                    _ergTaskDataList.Add(taskData);
                }
                for (var i = 0; i < _progressValue.Count; i++)
                {
                    var value = ReadInt(startIndex++, any);
                    _progressValue[i] = value;
                }
                _SortTaskDataList();
                _CalcTotalEnergyNum();
                _UpdateTaskProgressValue();
                _UpdateTaskBuyState();
                _RefreshRewardRP();
            }
            //刷新弹脸信息
            _RefreshPopupInfo();
        }
        
        private void _RefreshPopupInfo()
        {
            if (_packConf == null)
                return;
            if (RewardTheme.Setup(_packConf.EndTheme, RewardResAlt))
                RewardPopup.Setup(this, RewardTheme, RewardResAlt, false, false);
        }

        private void _InitInfoWithUserGrade()
        {
            if (_packConf == null)
                return;
            _detailId = Game.Manager.userGradeMan.GetTargetConfigDataId(_packConf.Detail);
            PackId = GetCurDetailConfig()?.PackId ?? 0;
            RefreshContent();
        }

        public void TryPurchasePack()
        {
            Game.Manager.activity.giftpack.Purchase(this, null, pack =>
            {
                //失败时关闭界面
                UIManager.Instance.CloseWindow(Res.ActiveR);
            });
        }

        //正常购买成功或补单成功后的回调
        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            base.PurchaseSuccess(packId_, rewards_, late_);
            //底层会记录BuyCount次数  本活动认为若补单造成重复购买多次 也单纯认为玩家拥有了一个权限 并不会因为买了多次就会多发体力 若有玩家找 则通过客服和打点推测补偿
            _UpdateTaskBuyState();
            //刷新红点
            _RefreshRewardRP();
            //发事件通知界面刷新
            MessageCenter.Get<MSG.GAME_ERG_LIST_PACK_BUY_SUCC>().Dispatch();
            //打点
            var conf = GetCurDetailConfig();
            DataTracker.event_erglist_purchase.Track(this, conf?.Diff ?? -1);
        }

        public override void WhenReset()
        {
            RemoveListener();
        }

        //当活动结束时 如果有未领取的免费奖励 则自动领取
        public override void WhenEnd()
        {
            //活动结束回收未被领取的奖励
            if (HasBuy)
            {
                var taskInfo = GetCurDetailConfig()?.TaskInfo;
                //检查目前已领取的任务奖励
                var claimedCount = 0;
                var totalCount = _ergTaskDataList.Count;
                foreach (var task in _ergTaskDataList)
                {
                    if (task.State == 2)
                    {
                        claimedCount++;
                    }
                }
                //暂时只帮助回收能量
                var totalEnergy = 0;
                foreach (var taskData in _ergTaskDataList)
                {
                    if (taskData.State != 1)
                        continue;
                    taskData.UpdateTaskState(2);
                    var r = taskData.Reward;
                    if (r != null && r.Id == Constant.kMergeEnergyObjId)
                    {
                        totalEnergy += r.Count;
                    }
                    claimedCount++;
                    //打点
                    var index = (taskInfo?.IndexOf(taskData.Id) ?? -1) + 1;
                    DataTracker.event_erglist_rwd.Track(this, index, claimedCount == totalCount);
                }
                if (totalEnergy > 0)
                {
                    var r = Game.Manager.rewardMan.BeginReward(Constant.kMergeEnergyObjId, totalEnergy, ReasonString.purchase);
                    if (r != null)
                    {
                        Game.Manager.screenPopup.TryQueue(RewardPopup, PopupType.Login, r);
                    }
                }
            }
            RemoveListener();
        }

        #region 任务相关

        //当前所有任务数据List
        private List<ErgTaskData> _ergTaskDataList = new List<ErgTaskData>();
        //当前所有任务奖励加起来的总和 默认只算能量
        public int TotalEnergyNum = 0;

        //获取所有任务数据
        public List<ErgTaskData> GetTaskDataList()
        {
            return _ergTaskDataList;
        }

        //检查目前所有任务是否已完成
        public bool CheckIsAllTaskFinish()
        {
            var isAllFinish = true;
            foreach (var taskData in _ergTaskDataList)
            {
                if (taskData.State <= 0)
                {
                    isAllFinish = false;
                    break;
                }
            }
            return isAllFinish;
        }
        
        //尝试领取任务奖励
        public void TryClaimTaskReward(ErgTaskData taskData, Vector3 flyPos)
        {
            if (taskData == null || taskData.State != 1)
                return;
            //已购买礼包 则直接发奖
            if (HasBuy)
            {
                //处理活动状态
                taskData.UpdateTaskState(2);
                //排序
                _SortTaskDataList();
                //发奖
                var reward = taskData.Reward;
                var r = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase);
                if (r != null)
                {
                    UIFlyUtility.FlyReward(r, flyPos);
                }
                //发事件通知界面刷新
                MessageCenter.Get<MSG.GAME_ERG_LIST_PACK_CLAIM_SUCC>().Dispatch(taskData);
                //检查所有任务奖励是否都已领取
                var isAllClaimed = true;
                foreach (var task in _ergTaskDataList)
                {
                    if (task.State != 2)
                    {
                        isAllClaimed = false;
                        break;
                    }
                }
                //打点
                var index = (GetCurDetailConfig()?.TaskInfo.IndexOf(taskData.Id) ?? -1) + 1;
                DataTracker.event_erglist_rwd.Track(this, index, isAllClaimed);
                //检查活动是否可以结束
                _CheckActivityEnd(isAllClaimed);
                //刷新红点
                _RefreshRewardRP();
            }
            //未购买礼包 弹出购买提示界面
            else
            {
                UIManager.Instance.OpenWindow(UIConfig.UIErgListPackBuyTips, this);
            }
        }
        
        //体力任务数据
        public class ErgTaskData
        {
            public int Id;                  //任务配置Id
            public ErgListTask Conf;        //任务配置
            public RewardConfig Reward;     //任务完成后发的奖励
            public int Priority;            //排序优先级
            public int TaskType;            //任务类型
            //进存档
            public int RequireCount;    //该任务完成所需要的总数值 
            public int State;           //该任务目前状态 默认0表示未完成 1表示已完成未领奖 2表示已完成已领奖
            //界面使用
            public int BelongActId;         //任务所属活动Id
            public int ProgressValue;       //当前任务进度值 存一下用于界面显示
            public bool HasBuy;             //礼包是否已购买 

            //第一次创建
            public void InitByConf(ErgListTask conf, int belongActId)
            {
                Conf = conf;
                Id = Conf.Id;
                Reward = Conf.TaskReward.ConvertToRewardConfig();
                Priority = Conf.Sort;
                TaskType = Conf.TaskType;
                RequireCount = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(Conf.RequireParam);
                State = 0;
                BelongActId = belongActId;
            }
            
            //读存档时创建
            public void InitById(int id, int requireCount, int state, int belongActId)
            {
                Id = id;
                Conf = GetErgListTask(Id);
                if (Conf != null)
                {
                    Reward = Conf.TaskReward.ConvertToRewardConfig();
                    Priority = Conf.Sort;
                    TaskType = Conf.TaskType;
                }
                RequireCount = requireCount;
                State = state;
                BelongActId = belongActId;
            }

            public void UpdateTaskState(int state)
            {
                State = state;
            }

            public void UpdateTaskValue(int value)
            {
                ProgressValue = value;
            }

            public void UpdateHasBuy(bool hasBuy)
            {
                HasBuy = hasBuy;
            }
            
            public override string ToString()
            {
                return $"ErgTaskData(Id={Id}, RequireCount={RequireCount}, State={State}, BelongActId = {BelongActId}, ProgressValue = {ProgressValue}, HasBuy = {HasBuy}, Conf={Conf})";
            }
        }
        
        //任务类型
        private enum ErgListTaskType
        {
            CollectCoin = 1,    //收集金币
        }

        //记录各个任务类型在活动期间的进度值。
        private List<int> _progressValue;
        private void _InitTaskProgressValue()
        {
            //初始化 key为[type-1] 使用这种方式是为了便于存档读取
            var count = Enum.GetValues(typeof(ErgListTaskType)).Length;
            _progressValue = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                _progressValue.Add(0);
            }
        }
        
        private void _InitTaskDataList()
        {
            if (_packConf == null)
                return;
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return;
            foreach (var taskId in detailConf.TaskInfo)
            {
                var taskConf = GetErgListTask(taskId);
                if (taskConf == null) continue;
                var taskData = new ErgTaskData();
                taskData.InitByConf(taskConf, Id);
                _ergTaskDataList.Add(taskData);
            }
            _SortTaskDataList();
            _CalcTotalEnergyNum();
            _UpdateTaskProgressValue();
            _UpdateTaskBuyState();
            _RefreshRewardRP();
        }
        
        //排序
        private void _SortTaskDataList()
        {
            _ergTaskDataList.Sort(_Compare);
        }
        
        private static int _Compare(ErgTaskData a, ErgTaskData b)
        {
            int stateCompare = _GetStateWeight(a.State).CompareTo(_GetStateWeight(b.State));
            if (stateCompare != 0)
                return stateCompare;
            return a.Priority.CompareTo(b.Priority);
        }
        
        // 状态权重排序：1（未领奖） > 0（未完成） > 2（已领奖）
        private static int _GetStateWeight(int state)
        {
            return state switch
            {
                1 => 0,
                0 => 1,
                2 => 2,
                _ => 3,
            };
        }

        private void _CalcTotalEnergyNum()
        {
            TotalEnergyNum = 0;
            foreach (var data in _ergTaskDataList)
            {
                var r = data.Reward;
                if (r != null && r.Id == Constant.kMergeEnergyObjId)
                {
                    TotalEnergyNum += r.Count;
                }
            }
        }
        
        private void AddListener() 
        {
            MessageCenter.Get<MSG.GAME_COIN_USE>().AddListener(_WhenCoinUse);
            MessageCenter.Get<MSG.GAME_COIN_ADD>().AddListener(_WhenCoinAdd);
        }

        private void RemoveListener()
        {
            MessageCenter.Get<MSG.GAME_COIN_USE>().RemoveListener(_WhenCoinUse);
            MessageCenter.Get<MSG.GAME_COIN_ADD>().RemoveListener(_WhenCoinAdd);
        }
        
        private void _WhenCoinUse(CoinChange change_) 
        {
            if (change_.type == CoinType.MergeCoin && change_.reason == ReasonString.undo_sell_item) 
                _UpdateProgressValue(ErgListTaskType.CollectCoin, -change_.amount);
        }
        
        private void _WhenCoinAdd(CoinChange change_) 
        {
            if (change_.type == CoinType.MergeCoin)
                _UpdateProgressValue(ErgListTaskType.CollectCoin, change_.amount);
        }
        
        private void _UpdateProgressValue(ErgListTaskType type, int value) 
        {
            if (!Valid || _packConf == null || _progressValue == null) return;
            var key = (int)type - 1;
            if (_progressValue.TryGetByIndex(key, out var oldValue))
            {
                var finalValue = oldValue + value;
                _progressValue[key] = finalValue;
                _CheckTaskState(type, finalValue);
            }
        }

        //检查各个任务是否已完成 已经是完成状态的则跳过
        //新完成的任务不会立即帮领，需要玩家手动领取，活动结束后如果付费了但是没领完 则会帮领
        private void _CheckTaskState(ErgListTaskType type, int checkValue)
        {
            if (checkValue <= 0)
                return;
            var checkType = (int)type;
            var hasFinish = false;
            foreach (var taskData in _ergTaskDataList)
            {
                //已经是完成状态的则跳过
                if (taskData.State > 0 || checkType != taskData.TaskType)
                    continue;
                //若检查到任务符合完成条件 则置为已完成未领奖状态
                if (checkValue >= taskData.RequireCount)
                {
                    taskData.UpdateTaskState(1);
                    hasFinish = true;
                    var detailConf = GetCurDetailConfig();
                    if (detailConf != null)
                    {
                        var index = detailConf.TaskInfo.IndexOf(taskData.Id) + 1;
                        var total = _ergTaskDataList.Count;
                        var isFinal = index == total;
                        DataTracker.event_erglist_complete.Track(this, index, total, detailConf.Diff, HasBuy, isFinal);
                    }
                }
                taskData.UpdateTaskValue(checkValue);
            }
            if (hasFinish)
            {
                _TryPopupOnTaskFinish();
                _RefreshRewardRP();
                _RefreshFinishTaskRP();
            }
        }

        private void _UpdateTaskProgressValue()
        {
            foreach (var taskData in _ergTaskDataList)
            {
                if (_progressValue.TryGetByIndex(taskData.TaskType - 1, out var value))
                {
                    taskData.UpdateTaskValue(value);
                }
            }
        }

        private void _UpdateTaskBuyState()
        {
            foreach (var taskData in _ergTaskDataList)
            {
                taskData.UpdateHasBuy(HasBuy);
            }
        }

        private void _CheckActivityEnd(bool isAllClaimed)
        {
            //如果所有任务都结束了 则立即结束活动
            if (isAllClaimed)
            {
                Game.Manager.activity.EndImmediate(this,false);
            }
        }

        //第一次完成任务时弹窗
        private void _TryPopupOnTaskFinish()
        {
            if (_canPop > 0)
                return;
            _canPop = 1;
            UIManager.Instance.RegisterIdleAction("pack_erg_list_finish_task", 701,
                () =>
                {
                    if (Active)
                    {
                        Res?.ActiveR?.Open(this);
                    }
                });
        }
        
        //刷新可领取奖励红点
        private void _RefreshRewardRP()
        {
            var old = _hasRewardRP;
            _hasRewardRP = false;
            if (!HasBuy)
                return;
            foreach (var taskData in _ergTaskDataList)
            {
                if (taskData.State == 1)
                {
                    _hasRewardRP = true;
                    break;
                }
            }
            //状态发生变化时触发入口刷新红点
            if (old != _hasRewardRP)
                MessageCenter.Get<MSG.ACTIVITY_UPDATE>().Dispatch();
        }

        public void OnOpenPackUI()
        {
            _hasFinishTaskRP = false;
            //打开界面时触发入口刷新红点
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().Dispatch();
        }
        
        //刷新有新完成任务时红点
        private void _RefreshFinishTaskRP()
        {
            var old = _hasFinishTaskRP;
            _hasFinishTaskRP = false;
            if (HasBuy)
                return;
            foreach (var taskData in _ergTaskDataList)
            {
                if (taskData.State == 1)
                {
                    _hasFinishTaskRP = true;
                    break;
                }
            }
            //状态发生变化时触发入口刷新红点
            if (old != _hasFinishTaskRP)
                MessageCenter.Get<MSG.ACTIVITY_UPDATE>().Dispatch();
        }
        
        public ListActivity.IEntrySetup SetupEntry(ListActivity.Entry e_)
        {
            e_?.dot.SetActive(HasRP);
            return null;
        }
        
        #endregion
    }
}