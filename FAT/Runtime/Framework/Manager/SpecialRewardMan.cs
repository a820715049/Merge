/*
 * @Author: tang.yan
 * @Description: 特殊弹脸奖励队列管理器 (如随机宝箱 万能卡) 
 * @Date: 2024-03-28 11:03:42
 */

using System.Collections.Generic;
using EL;

namespace FAT
{
    public partial class SpecialRewardMan : IGameModule
    {
        //特殊奖励数据
        private class RewardDisplayData
        {
            public ObjConfigType Type;      //物品类型
            public int Id;                  //物品id
            public SpecialRewardState State;//当前所处状态
        }
        
        private enum SpecialRewardState
        {
            Begin,  //开始领取 (BeginReward)
            Ready,  //准备领取 (CommitReward)
            Finish  //完成领取
        }

        #region 内部方法
        
        //维护现在要依次执行表现的奖励队列
        //Item1 物品类型 Item2 物品id Item3 物品是否已执行commit
        private List<RewardDisplayData> _rewardDisplayList = new List<RewardDisplayData>();

        //begin reward
        private bool _TryBeginSpecialReward(ObjConfigType type, int rewardId, int rewardNum, ReasonString reason = null)
        {
            //用之前清理
            _ClearFinishRewardDisplayData();
            var isSuccess = true;
            switch (type)
            {
                case ObjConfigType.RandomBox:
                    _GenRewardDisplayData(type, rewardId, rewardNum);
                    isSuccess = Game.Manager.randomBoxMan.TryBeginRandomBox(rewardId, rewardNum, reason);
                    break;
                case ObjConfigType.CardJoker:
                    _GenRewardDisplayData(type, rewardId, rewardNum);
                    isSuccess= Game.Manager.cardMan.OnGetCardJoker(rewardId, rewardNum);
                    break;
                case ObjConfigType.CardPack:
                    _GenRewardDisplayData(type, rewardId, rewardNum);
                    var fromPos = UIFlyFactory.ResolveFlyTarget(FlyType.MergeItemFlyTarget);
                    isSuccess = Game.Manager.cardMan.TryOpenCardPack(rewardId, fromPos, false);
                    break;
                default:
                    DebugEx.FormatError("SpecialRewardMan.TryBeginSpecialReward : error reward type! {0}", type);
                    isSuccess = false;
                    break;
            }
            return isSuccess;
        }
        
        //commit reward
        private void _TryCommitSpecialReward(ObjConfigType type, int rewardId, int rewardNum)
        {
            switch (type)
            {
                case ObjConfigType.RandomBox:
                    Game.Manager.randomBoxMan.TryReadyRandomBox(rewardId, rewardNum);
                    break;
                case ObjConfigType.CardJoker:
                    //走通用逻辑 无需其他处理
                    break;
                case ObjConfigType.CardPack:
                    //走通用逻辑 无需其他处理
                    break;
                default:
                    DebugEx.FormatError("SpecialRewardMan.TryCommitSpecialReward : error reward type! {0}", type);
                    return;
            }
            //刷新数据状态
            _ReadyRewardDisplayData(type, rewardId, rewardNum);
        }

        //展示当前队列中第一个奖励对应的弹脸界面 会在commit中主动调用
        private void _TryDisplaySpecialReward()
        {
            var data = _GetFirstCanClaimSpecialReward();
            if (data != null)
            {
                switch (data.Type)
                {
                    case ObjConfigType.RandomBox:
                        Game.Manager.randomBoxMan.TryClaimRandomBox();
                        break;
                    case ObjConfigType.CardJoker:
                        Game.Manager.cardMan.TryOpenJokerGet();
                        break;
                    case ObjConfigType.CardPack:
                        Game.Manager.cardMan.TryOpenPackDisplay();
                        break;
                }
            }
            Game.Manager.autoGuide.TryInterruptGuide();
        }
        
        //在即将关闭奖励展示界面时，主动调用finish 便于后续清理不再使用的数据
        private void _TryFinishSpecialReward(ObjConfigType type, int rewardId)
        {
            _FinishRewardDisplayData(type, rewardId);
        }
        
        #endregion

        //在一个奖励展示完后 检查是否有后续特殊奖励 没有的话就Dispatch事件 用于通知其他系统
        public void CheckSpecialRewardFinish()
        {
            if (!CheckCanClaimSpecialReward())
            {
                DebugEx.FormatInfo("[SpecialRewardMan.CheckSpecialRewardFinish] : Special Reward Finish!");
                MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().Dispatch();
            }
        }

        //从数据层和表现层同时判断目前是否忙碌
        public bool IsBusy()
        {
            foreach (var displayData in _rewardDisplayList)
            {
                if (displayData.State < SpecialRewardState.Finish)
                {
                    return true;
                }
            }
            return false;
        }
        
        //仅从表现层判断目前是否忙碌
        public bool IsBusyShow()
        {
            foreach (var displayData in _rewardDisplayList)
            {
                if (displayData.State == SpecialRewardState.Ready)
                {
                    return true;
                }
            }
            return false;
        }

        public bool CheckCanClaimSpecialReward()
        {
            bool canClaim = false;
            foreach (var displayData in _rewardDisplayList)
            {
                if (displayData.State == SpecialRewardState.Ready)
                {
                    canClaim = true;
                    break;
                }
            }
            return canClaim;
        }
        
        //界面中调用  在当前list中尝试获取指定type的可以展示的奖励id
        public int TryGetCanClaimId(ObjConfigType type)
        {
            foreach (var displayData in _rewardDisplayList)
            {
                if (type == displayData.Type && displayData.State == SpecialRewardState.Ready)
                {
                    return displayData.Id;
                }
            }
            return 0;
        }
        
        public void Reset()
        {
            //reset时清理所有特殊奖励 无论其是否commit
            _rewardDisplayList.Clear();
            _priorityQueue.Clear();
            _isPumping = false;
        }

        public void LoadConfig() { }

        public void Startup() { }
        
        private void _GenRewardDisplayData(ObjConfigType type, int rewardId, int rewardNum)
        {
            for (int i = 0; i < rewardNum; i++)
            {
                var displayData = new RewardDisplayData()
                {
                    Type = type,
                    Id = rewardId,
                    State = SpecialRewardState.Begin
                };
                _rewardDisplayList.Add(displayData);
            }
        }

        private void _ReadyRewardDisplayData(ObjConfigType type, int rewardId, int rewardNum)
        {
            int count = 0;
            foreach (var displayData in _rewardDisplayList)
            {
                if (count < rewardNum)
                {
                    if (displayData.Type == type && displayData.Id == rewardId && displayData.State == SpecialRewardState.Begin)
                    {
                        displayData.State = SpecialRewardState.Ready;
                        count++;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void _FinishRewardDisplayData(ObjConfigType type, int rewardId)
        {
            foreach (var displayData in _rewardDisplayList)
            {
                if (type == displayData.Type && rewardId == displayData.Id && displayData.State == SpecialRewardState.Ready)
                {
                    displayData.State = SpecialRewardState.Finish;
                    break;
                }
            }
        }
        
        private void _ClearFinishRewardDisplayData()
        {
            for (int i = _rewardDisplayList.Count - 1; i >= 0; i--)
            {
                var displayData = _rewardDisplayList[i];
                if (displayData.State == SpecialRewardState.Finish)
                {
                    _rewardDisplayList.RemoveAt(i);
                }
            }
        }
        
        private RewardDisplayData _GetFirstCanClaimSpecialReward()
        {
            foreach (var displayData in _rewardDisplayList)
            {
                if (displayData.State == SpecialRewardState.Ready)
                {
                    return displayData;
                }
            }
            return null;
        }
    }
}
