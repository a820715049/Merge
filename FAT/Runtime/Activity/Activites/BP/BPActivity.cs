/*
 * @Author: tang.yan
 * @Description: BP活动数据类 - FAT通行证.excel
 * @Doc: https://centurygames.feishu.cn/wiki/FDeUw753Yi6epgk7lyPcEGgBnNh
 * @Date: 2025-06-18 14:06:14
 */

using System;
using System.Collections.Generic;
using Config;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using EL.Resource;
using FAT.Merge;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using UnityEngine;

namespace FAT
{
    public class BPActivity : GiftPack, IBoardEntry, IBoardActivityTips
    {
        public override bool Valid => ConfD != null;
        public EventBp ConfD { get; private set; }

        #region 活动基础
        
        //用户分层 对应BpDetail.id
        private int _detailId;
        
        //外部调用需判空
        public BpDetail GetCurDetailConfig()
        {
            return Game.Manager.configMan.GetBpDetailConfig(_detailId);
        }
        
        public BPActivity(ActivityLite lite_)
        {
            Lite = lite_;
            ConfD = Game.Manager.configMan.GetEventBpConfig(lite_.Param);
            _InitTaskProgressValue();
            AddListener();
        }

        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            _detailId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            //刷新购买项信息
            RefreshContent();
            //初始化里程碑奖励领取状态List
            _InitRewardClaimStateDict();
            //初始化任务数据List
            _InitTaskDataList();
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新循环奖励随机池
            _RefreshCycleRandomPool();
            //活动首次开启时 尝试弹脸
            _TryPopFirst();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            var startIndex = 3;
            any.Add(ToRecord(startIndex++, _detailId));
            any.Add(ToRecord(startIndex++, _purchaseState));
            any.Add(ToRecord(startIndex++, _hasJoinBp));
            any.Add(ToRecord(startIndex++, _milestoneLevel));
            any.Add(ToRecord(startIndex++, _milestoneNum));
            //存储里程碑奖励领取情况
            foreach (var info in RewardClaimStateDict)
            {
                any.Add(ToRecord(startIndex++, info.Key));    //id
                any.Add(ToRecord(startIndex++, info.Value.Item1));    //免费奖励是否已领取
                any.Add(ToRecord(startIndex++, info.Value.Item2));    //付费奖励是否已领取
                any.Add(ToRecord(startIndex++, info.Value.Item3));    //当前id是否已弹出购买弹窗
            }
            //存储循环奖励的可领取次数
            any.Add(ToRecord(startIndex++, CycleAvailableCount));
            //存储循环奖励的已领取次数
            any.Add(ToRecord(startIndex++, CycleReceivedCount));
            //存储循环任务累计完成次数
            any.Add(ToRecord(startIndex++, _cycleFinishCount));
            //存储循环任务累计领取奖励的次数
            any.Add(ToRecord(startIndex++, _cycleClaimCount));
            //存储日刷任务下次刷新时间
            any.Add(ToRecord(startIndex++, _nextRefreshTs, TSOffset()));
            //存储任务相关数据
            foreach (var taskData in _bpTaskDataList)
            {
                any.Add(ToRecord(startIndex++, taskData.Id));
                any.Add(ToRecord(startIndex++, taskData.RequireCount));
                any.Add(ToRecord(startIndex++, taskData.ArchiveState));
            }
            //最后处理_progressValue 以应对后续加类型
            foreach (var value in _progressValue)
            {
                any.Add(ToRecord(startIndex++, value.Item1));
                any.Add(ToRecord(startIndex++, value.Item2));
            }
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            base.LoadSetup(data_);
            var any = data_.AnyState;
            var startIndex = 3;
            if (any.Count > startIndex)
            {
                _detailId = ReadInt(startIndex++, any);
                _purchaseState = ReadInt(startIndex++, any);
                _hasJoinBp = ReadBool(startIndex++, any);
                _milestoneLevel = ReadInt(startIndex++, any);
                _milestoneNum = ReadInt(startIndex++, any);
                RewardClaimStateDict.Clear();
                var detailConf = GetCurDetailConfig();
                if (detailConf == null)
                    return;
                //默认最后一个的循环奖励单独存
                var milestoneCount = detailConf.MileStones.Count - 1;
                for (var i = 0; i < milestoneCount; i++)
                {
                    var id = ReadInt(startIndex++, any);
                    var freeState = ReadBool(startIndex++, any);
                    var buyState = ReadBool(startIndex++, any);
                    var hasPopBuy = ReadBool(startIndex++, any);
                    RewardClaimStateDict.Add(id, (freeState, buyState, hasPopBuy));
                }
                //读取循环奖励的可领取次数
                CycleAvailableCount = ReadInt(startIndex++, any);
                //读取循环奖励的已领取次数
                CycleReceivedCount = ReadInt(startIndex++, any);
                //读取循环任务累计完成次数
                _cycleFinishCount = ReadInt(startIndex++, any);
                //读取循环任务累计领取奖励的次数
                _cycleClaimCount = ReadInt(startIndex++, any);
                //读取日刷任务下次刷新时间
                _nextRefreshTs = ReadTS(startIndex++, TSOffset(), any);
                //读取任务相关数据
                //因为存储的数据是已经排序后的，所以和配置顺序可能不一样，且还含有循环任务 所以采用下面的读取方式
                var taskCount = detailConf.DailyRefreshTask.Count + 1;  //+1是为了顺便读取循环任务
                var cycleTaskId = detailConf.CircleTask;
                for (var i = 0; i < taskCount; i++)
                {
                    var id = ReadInt(startIndex++, any);
                    var requireCount = ReadInt(startIndex++, any);
                    var state = ReadInt(startIndex++, any);
                    var taskData = new BPTaskData();
                    taskData.InitById(id, requireCount, state, Id, id == cycleTaskId);
                    _bpTaskDataList.Add(taskData);
                }
                //读取各个任务类型的进度值
                for (var i = 0; i < _progressValue.Count; i++)
                {
                    var value1 = ReadInt(startIndex++, any);
                    var value2 = ReadInt(startIndex++, any);
                    _progressValue[i] = (value1, value2);
                }
                //按任务状态排序
                _SortTaskDataList();
                //初始时刷新任务目前最新的进度值
                _UpdateTaskProgressValue();
            }
            //刷新购买项信息
            RefreshContent();
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新循环奖励随机池
            _RefreshCycleRandomPool();
            //上线加载存档时 检查任务跨天刷新
            _CheckTaskRefresh();
        }
        
        private long TSOffset() => Game.Timestamp(new DateTime(2024, 1, 1));

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach (var v in StartPopup.ResEnumerate()) yield return v;
            foreach (var v in Visual.ResEnumerate()) yield return v;
        }

        public override void WhenReset()
        {
            RemoveListener();
        }

        public override void WhenEnd()
        {
            RemoveListener();
            //活动结束时 自动领取所有未领取的任务奖励
            _AutoRefreshTask();
            //活动结束时 自动领取所有可领取的里程碑奖励和循环奖励
            _AutoClaimReward();
            //活动结束时打点
            DataTracker.bp_end_settle.Track(this, GetCurDetailConfig()?.Diff ?? 0, _purchaseState, _milestoneLevel + 1);
        }
        
        private void AddListener()
        {
            MessageCenter.Get<MSG.GAME_COIN_USE>().AddListener(_WhenCoinUse);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_MERGE>().AddListener(_WhenBoardMerge);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_SKILL>().AddListener(_WhenBoardSkill);
            MessageCenter.Get<MSG.ORDER_FINISH_DATA>().AddListener(_WhenOrderFinish);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondUpdate);
        }

        private void RemoveListener()
        {
            MessageCenter.Get<MSG.GAME_COIN_USE>().RemoveListener(_WhenCoinUse);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_MERGE>().RemoveListener(_WhenBoardMerge);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_SKILL>().RemoveListener(_WhenBoardSkill);
            MessageCenter.Get<MSG.ORDER_FINISH_DATA>().RemoveListener(_WhenOrderFinish);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondUpdate);
        }

        private void _OnSecondUpdate()
        {
            //在线期间 检查任务跨天刷新
            _CheckTaskRefresh();
        }
        
        #region 界面 入口 换皮 弹脸 红点
        
        public override ActivityVisual Visual => VisualMain.visual;

        public VisualRes VisualMain { get; } = new(UIConfig.UIBPMain); // 主界面

        public VisualRes VisualBuyBoth { get; } = new(UIConfig.UIBPBuyBoth); // 购买界面
        public VisualRes VisualBuyBothPop { get; } = new(UIConfig.UIBPBuyBothPop); // 购买强弹界面
        public VisualRes VisualUpgrade { get; } = new(UIConfig.UIBPBuyUpgrade); // 升级界面
        public VisualRes VisualBuyOneSuccess { get; } = new(UIConfig.UIBPBuyOneSuccess); // 付费一购买成功界面
        public VisualRes VisualBuyTwoSuccess { get; } = new(UIConfig.UIBPBuyTwoSuccess); // 付费二购买成功界面
        public VisualRes VisualDoubleCheck { get; } = new(UIConfig.UIBPDoubleCheck); // 二次确认界面
        public ActivityVisual VisualTaskItem { get; } = new(); // 任务Item

        // 弹脸
        public VisualPopup StartPopup { get; } = new(UIConfig.UIBPStart); // 活动开启
        public VisualPopup EndPopup { get; } = new(UIConfig.UIBPEnd); // 活动结束
        public VisualPopup TaskRefreshPopup { get; } = new(UIConfig.UIBPTaskComplete); // 任务刷新弹窗

        public override void Open() => Open(VisualMain.res);

        private void _RefreshPopupInfo()
        {
            if (!Valid)
            {
                return;
            }

            // 弹脸
            StartPopup.Setup(ConfD.StartTheme, this);
            TaskRefreshPopup.Setup(ConfD.TaskTheme, this);
            EndPopup.Setup(ConfD.EndTheme, this, false, false);

            // 界面
            VisualMain.Setup(ConfD.MainTheme);
            VisualBuyBoth.Setup(ConfD.BuyTheme);
            VisualBuyBothPop.Setup(ConfD.BuyPopTheme);
            VisualUpgrade.Setup(ConfD.BuyTheme2);
            VisualBuyOneSuccess.Setup(ConfD.BuySuccessTheme1);
            VisualBuyTwoSuccess.Setup(ConfD.BuySuccessTheme2);
            VisualDoubleCheck.Setup(ConfD.ConfirmTheme);
            
            // 任务Item
            VisualTaskItem.Setup(ConfD.TaskAssetTheme);
        }

        private bool _hasPopFirst = false;

        private void _TryPopFirst()
        {
            if (!_hasJoinBp)
            {
                Game.Manager.screenPopup.TryQueue(StartPopup.popup, PopupType.Login);
                _hasPopFirst = true;
            }
        }

        string IBoardEntry.BoardEntryAsset()
        {
            Visual.AssetMap.TryGetValue("boardEntry", out var key);
            return key;
        }

        string IBoardActivityTips.BoardActivityTipsAsset()
        {
            Visual.AssetMap.TryGetValue("taskTips", out var key);
            return key;
        }

        //玩家目前是否参与了bp活动  参与的意思是见到过bp的主界面
        private bool _hasJoinBp = false;

        //打开活动主界面时认为玩家参与了bp活动
        public void SetJoinBp()
        {
            _hasJoinBp = true;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (!_hasPopFirst)
            {
                if (!_hasJoinBp)
                    //玩家没参与活动时 弹开启弹窗 配置决定其一天内弹出次数
                    StartPopup.Popup(popup_, state_);
                else
                    //玩家参与了活动时 弹任务刷新界面 配置决定其一天内弹出次数  custom_传参数告知界面这次弹窗是要显示任务刷新
                    TaskRefreshPopup.Popup(popup_, state_, custom_: true);
            }
        }

        //是否可以显示红点 同时返回显示的数量 数量=待领奖的任务个数+里程碑可以领取的奖励个数+循环奖励次数
        public bool CheckCanShowRP(out int num)
        {
            num = 0;
            //待领奖的日刷任务个数
            foreach (var taskData in _bpTaskDataList)
            {
                if (!taskData.IsCycle)
                {
                    if (taskData.State == BPTaskState.UnClaim)
                        num++;
                }
            }
            //循环任务已完成未领取的次数
            num += UnClaimCycleTaskCount;
            //里程碑可以领取的奖励个数
            var isFree = PurchaseState == BPPurchaseState.Free;
            foreach (var state in RewardClaimStateDict)
            {
                if (isFree)
                    //找到没有领取免费奖励的id 检查目前等级下是否可以领取
                    if (state.Value.Item1) continue;
                else
                    //找到没有领取免费奖励或付费奖励的id 检查目前等级下是否可以领取
                    if (state.Value.Item1 && state.Value.Item2) continue;
                var id = state.Key;
                //获取对应配置信息
                var curMilestoneInfo = Game.Manager.configMan.GetBpMilestoneConfig(id);
                if (curMilestoneInfo == null) continue;
                //检查该里程碑是否达到领奖等级
                var level = curMilestoneInfo.ShowNum - 1;
                if (level > _milestoneLevel) continue;
                if (!state.Value.Item1)
                {
                    //统计免费奖励
                    num++;
                }
                if (!isFree && !state.Value.Item2)
                {
                    //统计付费奖励
                    num++;
                }
            }
            if (!isFree)
            {
                //付费后才统计循环奖励次数
                num += CycleAvailableCount;
            }
            return num > 0;
        }

        //检查日刷任务信息
        public (int, int) CheckDailyTaskCount()
        {
            var completeCount = 0;
            var totalCount = 0;
            foreach (var taskData in _bpTaskDataList)
            {
                if (!taskData.IsCycle)
                {
                    //记录已完成数量
                    if (taskData.State == BPTaskState.UnClaim || taskData.State == BPTaskState.Claimed)
                    {
                        completeCount++;
                    }
                    //记录日刷任务总数量
                    totalCount++;
                }
            }
            return (completeCount, totalCount);
        }
        
        //界面调用检查目前是否可以弹出购买弹窗
        //注意 如果配了3 5 7级能弹，若一下从1级升到10级，则只会弹一遍
        public bool CheckCanPopBuy()
        {
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return false;
            //必须是免费用户才检查是否弹出
            if (PurchaseState != BPPurchaseState.Free)
                return false;
            var configMan = Game.Manager.configMan;
            for (var i = 0; i < detailConf.MileStones.Count - 1; i++)
            {
                //查找所有<=当前等级的里程碑配置
                if (i > _milestoneLevel)
                    break;
                var id = detailConf.MileStones[i];
                //查找没弹过弹窗且配置上允许弹的id 找到了就认为可以弹
                if (RewardClaimStateDict.TryGetValue(id, out var info) && !info.Item3)
                {
                    var conf = configMan.GetBpMilestoneConfig(id);
                    if (conf != null && conf.IsPopLevel)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //设置当前等级及之前的所有等级为已弹出购买弹窗
        public void SetCurLevelPopBuy()
        {
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return;
            for (var i = 0; i < detailConf.MileStones.Count - 1; i++)
            {
                //查找所有<=当前等级的数据
                if (i > _milestoneLevel)
                    break;
                var id = detailConf.MileStones[i];
                if (RewardClaimStateDict.TryGetValue(id, out var info))
                {
                    RewardClaimStateDict[id] = (info.Item1, info.Item2, true);
                }
            }
        }

        #endregion

        #endregion
        
        #region 购买逻辑

        //BP的购买项类型
        public enum BPPurchaseType
        {
            None = 0,       //无效
            Normal = 1,     //付费一档
            Luxury = 2,     //付费二档
            Up = 3,         //一档升级到二档 补差价
            End = 4,        //活动结束时挽留
        }
        
        //BP的购买状态
        public enum BPPurchaseState
        {
            Free = 0,       //未付费
            Normal = 1,     //买了付费一档
            Luxury = 2,     //买了付费二档
        }

        //当前购买状态 默认初始为免费
        private int _purchaseState = (int)BPPurchaseState.Free;
        public BPPurchaseState PurchaseState => (BPPurchaseState)_purchaseState;
        
        //自行构造购买项相关信息 Value.Item1为IAPPack.id BonusReward目前应该是空的没有内容
        private Dictionary<BPPurchaseType, (int, IAPPack, BonusReward)> _purchaseInfoDict = new();

        public BpPackInfo GetBpPackInfoByType(BPPurchaseType type)
        {
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return null;
            var packInfoId = type switch
            {
                BPPurchaseType.Normal => detailConf.PackNormal,
                BPPurchaseType.Luxury => detailConf.PackLuxury,
                BPPurchaseType.Up => detailConf.PackUp,
                BPPurchaseType.End => detailConf.PackEnd,
                _ => 0
            };
            return packInfoId <= 0 ? null : Game.Manager.configMan.GetBpPackInfoConfig(packInfoId);
        }
        
        public IAPPack GetIAPPackByType(BPPurchaseType type)
        {
            return _purchaseInfoDict.TryGetValue(type, out var info) ? info.Item2 : null;
        }

        public string GetIAPPriceByType(BPPurchaseType type)
        {
            return Game.Manager.iap.PriceInfo(GetIAPPackByType(type)?.IapId ?? 0);
        }

        //外部调用 付费指定类型的购买项
        public void TryPurchase(BPPurchaseType type)
        {
            var iapPack = GetIAPPackByType(type);
            if (iapPack == null)
                return;
#if FAT_PIONEER
            //先锋服玩家可以跳过IAP购买流程 直接购买BP
            using var _ = ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var list);
            PurchaseSuccess(iapPack.Id, list, false);
#else
            Game.Manager.activity.giftpack.Purchase(this, iapPack);
#endif
        }
        
        //正常购买成功或补单成功后的回调
        //这里无论是正常购买、单次购买后补单还是重复购买后补单，都会依照packId_是多少来处理对应逻辑
        //且保证不会重复处理一样的packId_或状态，这样做的目的是避免重复购买时，如果有客诉，补偿时会比较纯粹
        //需注意的是只要活动结束了，任何的补单回调都不会走。
        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            base.PurchaseSuccess(packId_, rewards_, late_);
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return;
            //根据packId找出其购买类型
            var purchaseType = BPPurchaseType.None;
            foreach (var info in _purchaseInfoDict)
            {
                if (info.Value.Item1 == packId_)
                {
                    purchaseType = info.Key;
                    break;
                }
            }
            //非法购买类型时return
            if (purchaseType == BPPurchaseType.None)
                return;
            //创建奖励容器
            var container = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);
            if (!late_)
            {
                //非补单情况下 填充IAPPack本身的奖励 目前从配置上来讲会配空
                rewardList.AddRange(rewards_);  
            }
            //根据购买类型决定最终的购买状态
            if (purchaseType == BPPurchaseType.Normal)
            {
                //当前状态不是付费二档时 将状态置为付费一档  避免重复购买导致档位下降
                if (PurchaseState != BPPurchaseState.Luxury)
                {
                    _purchaseState = (int)BPPurchaseState.Normal;
                }
            }
            else if (purchaseType == BPPurchaseType.Luxury)
            {
                //只有当前状态不是付费二档时 才会发放一次性奖励 避免重复购买导致重复发
                if (PurchaseState != BPPurchaseState.Luxury)
                {
                    var packInfo = GetBpPackInfoByType(purchaseType);
                    if (packInfo != null)
                    {
                        var reward = packInfo.Reward.ConvertToRewardConfig();
                        if (reward != null)
                        {
                            //处理领奖
                            var rewardData = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase);
                            rewardList.Add(rewardData);
                        }
                    }
                }
                _purchaseState = (int)BPPurchaseState.Luxury;
            }
            else if (purchaseType == BPPurchaseType.Up)
            {
                //只有当前状态不是付费二档时 才会发放一次性奖励 避免重复购买导致重复发
                if (PurchaseState != BPPurchaseState.Luxury)
                {
                    var packInfo = GetBpPackInfoByType(purchaseType);
                    if (packInfo != null)
                    {
                        var reward = packInfo.Reward.ConvertToRewardConfig();
                        if (reward != null)
                        {
                            //处理领奖
                            var rewardData = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase);
                            rewardList.Add(rewardData);
                        }
                    }
                }
                _purchaseState = (int)BPPurchaseState.Luxury;
            }
            //购买挽留档位时 活动其实已经结束了 所以玩家可购买的窗口期只有那一次session
            //又因为活动已经结束，所以在此之前的所有相关补单的回调都不会走。
            else if (purchaseType == BPPurchaseType.End)
            {
                //只有当前状态是免费档时 才会发放一次性奖励 避免重复购买导致重复发
                if (PurchaseState == BPPurchaseState.Free)
                {
                    //已付费 则尝试领取所有付费奖励 + 循环奖励
                    var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //奖励整合结果
                    //收集奖励同时会打点和修改对应数据
                    _TryClaimAllMilestoneReward(false, rewardMap);
                    //统一领取整理好后的任务奖励
                    var rewardMan = Game.Manager.rewardMan;
                    foreach (var rewardInfo in rewardMap)
                    {
                        var reward = rewardMan.BeginReward(rewardInfo.Key, rewardInfo.Value, ReasonString.bp_lastpurchase);
                        rewardList.Add(reward);
                    }
                    //数据回收
                    ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
                }
                _purchaseState = (int)BPPurchaseState.Normal;
            }
            //购买成功时打点
            DataTracker.bp_purchase.Track(this, detailConf.Diff, (int)purchaseType, late_);
            //发事件通知界面做表现 late_会告知界面此次购买回调是否是补单，补单时的界面表现需要具体情况具体处理
            MessageCenter.Get<MSG.GAME_BP_BUY_SUCCESS>().Dispatch(purchaseType, container, late_);
        }
        
        public override BonusReward MatchPack(int packId_) 
        {
            foreach (var info in _purchaseInfoDict)
            {
                if (info.Value.Item1 == packId_)
                {
                    return info.Value.Item3;
                }
            }
            return null;
        }
        
        public override void RefreshContent()
        {
            _purchaseInfoDict.Clear();
            if (!Valid)
                return;
            _RefreshPackInfo(BPPurchaseType.Normal);
            _RefreshPackInfo(BPPurchaseType.Luxury);
            _RefreshPackInfo(BPPurchaseType.Up);
            _RefreshPackInfo(BPPurchaseType.End);
        }
        
        private void _RefreshPackInfo(BPPurchaseType type)
        {
            var packId = GetBpPackInfoByType(type)?.PackId ?? 0;
            if (packId <= 0)
                return;
            var iapPack = GetIAPPack(packId);
            if (iapPack == null)
                return;
            var goods = new BonusReward();
            goods.Refresh(packId, iapPack);
            _purchaseInfoDict.Add(type, (packId, iapPack, goods));
        }

        //本礼包活动不使用底层GiftPack字段/方法
        public override int PackId { get; set; }
        public override int ThemeId { get; }
        public override int StockTotal => -1;   //设成-1 避免此字段的影响
        public override void RefreshPack() { }
        
        #endregion

        #region 里程碑

        #region 里程碑进度值

        private int _milestoneLevel; //当前里程碑所处等级 从0开始 根据阶段值读配置获取当前的最大进度以及达成后可获得的奖励
        private int _milestoneNum;   //当前里程碑的进度值

        //获取指定等级的里程碑信息 默认最后一个为循环奖励  milestoneLevel从0开始
        //若传入等级<0或>最大等级，会返回null，因此外部调用需判空
        public BpMilestone GetMilestoneInfo(int milestoneLevel)
        {
            var allMilestoneInfo = GetCurDetailConfig()?.MileStones;
            if (allMilestoneInfo == null || milestoneLevel < 0)
                return null;
            var cycleIndex = allMilestoneInfo.Count - 1;    //默认最后一个为循环奖励
            var isCycle = milestoneLevel >= cycleIndex;
            var finalIndex = !isCycle ? milestoneLevel : cycleIndex;
            var conf = Game.Manager.configMan.GetBpMilestoneConfig(allMilestoneInfo[finalIndex]);
            //如果当前等级处于循环阶段 进一步判断是否达到循环上限(满级)
            if (isCycle && conf != null)
            {
                //若当前等级超过上限 则返回null
                if (milestoneLevel >= cycleIndex + conf.CircleLimit - 1)
                    return null;
            }
            return conf;
        }
        
        public int GetCurMilestoneLevel()
        {
            return _milestoneLevel;
        }

        public int GetCurMilestoneNum()
        {
            return _milestoneNum;
        }

        //外部调用增加当前进度值 支持一下发很多经验 升很多级
        public void TryAddMilestoneNum(int tokenId, int tokenNum, ReasonString reason)
        {
            if (ConfD == null || tokenId <= 0 || tokenNum <= 0 || tokenId != ConfD.ScoreId)
                return;
            //获取当前阶段对应的进度信息 拿不到时认为数据非法或者已经满级
            var curMilestoneInfo = GetMilestoneInfo(_milestoneLevel);
            if (curMilestoneInfo == null)
                return;
            //只在最终处理积分的时候 才考虑经验倍数加成 避免各个来源处自己考虑是否需要加倍
            //若有些来源不需要加倍（如直接购买付费二档时 不考虑倍数），则通过reason区别
            if (reason != ReasonString.purchase && reason != ReasonString.bp_lastpurchase && PurchaseState == BPPurchaseState.Luxury)
            {
                var bpPackInfo = GetBpPackInfoByType(BPPurchaseType.Luxury);
                var privilege = bpPackInfo?.PrivilegeInfo ?? 0;
                if (privilege > 0)
                {
                    tokenNum = tokenNum * privilege / 10;   //  配置上倍率会扩大10倍 所以实际计算时再除回去
                }
            }
            //增加进度值
            var finalMilestoneNum = _milestoneNum + tokenNum;
            DataTracker.token_change.Track(tokenId, tokenNum, finalMilestoneNum, reason);
            //检测是否达到本阶段最大值
            var curMilestoneMax = curMilestoneInfo.Score;
            //未达到最大值 只播放进度条增长动画
            if (finalMilestoneNum < curMilestoneMax)
            {
                _milestoneNum = finalMilestoneNum;
                MessageCenter.Get<MSG.UI_BP_MILESTONE_CHANGE>().Dispatch(finalMilestoneNum, -1);
                return;
            }
            //达到最大值时就处理里程碑升级逻辑，可能会一下升很多级
            var curGroupConf = GetCurDetailConfig();
            var allMilestoneInfo = curGroupConf.MileStones;
            var totalCount = allMilestoneInfo.Count;
            do
            {
                //每次循环都拿到当前对应的里程碑配置信息 若拿不到配置 则认为满级 break
                var tempMilestoneInfo = GetMilestoneInfo(_milestoneLevel);
                if (tempMilestoneInfo == null)
                    break;
                var tempMilestoneMax = tempMilestoneInfo.Score;
                if (finalMilestoneNum < tempMilestoneMax)
                    break;
                finalMilestoneNum -= tempMilestoneMax;   //此时finalMilestoneNum代表多余的进度值
                //先递进
                _milestoneLevel++;
                _milestoneNum = finalMilestoneNum;
                //检查下当前是否处于循环奖励阶段 是的话循环奖励的可领取次数+1
                var isCycle = _milestoneLevel >= totalCount - 1;
                if (isCycle)
                    CycleAvailableCount++;
                //打点逻辑  完成循环奖励的同时也会打里程碑升级
                if (isCycle)
                {
                    //循环奖励完成时打点
                    DataTracker.bp_cycle_complete.Track(this, curGroupConf.Diff, CycleAvailableCount, CycleReceivedCount, _milestoneLevel + 1);
                }
                var nextMilestoneConf = GetMilestoneInfo(_milestoneLevel);  //这里获得的配置已经是升级后的配置 因为_milestoneLevel在前面已经+1了
                //里程碑升级时打点
                var isFinal = _milestoneLevel >= totalCount - 2;
                var levelNum = nextMilestoneConf?.Id ?? tempMilestoneInfo.Id;   //nextMilestoneConf为空时说明满级 此时用当前等级的id打点（其实二者都是循环档位的id）
                DataTracker.bp_milestone_complete.Track(this, curGroupConf.Diff, _milestoneLevel + 1, totalCount - 1, isFinal, 
                    levelNum, _purchaseState, _milestoneLevel + 1);
            }
            while(true);
            
            //进度条动画  进度条满时发奖流程  发完奖后若还有多余的进度值，剩余的进度值也要有动画
            MessageCenter.Get<MSG.UI_BP_MILESTONE_CHANGE>().Dispatch(finalMilestoneNum, curMilestoneMax);
        }

        #endregion

        #region 里程碑奖励

        #region 进度奖励(免费/付费)

        //记录里程碑各等级的免费和付费奖励是否已领取 会进存档  这里面不会记录循环奖励相关信息 循环奖励单独拿出来存
        //Key: 里程碑奖励信息id 对应BpMilestone.id
        //Value: item1:免费奖励是否已领取  item2:付费奖励是否已领取  item3:当前id(等级)是否弹过购买弹窗
        public Dictionary<int, (bool, bool, bool)> RewardClaimStateDict { get; private set; } = new Dictionary<int, (bool, bool, bool)>();

        //检查指定id的奖励是否可领取  会检查bp等级和领取状态
        public bool CheckCanClaimReward(int id, bool isClaimFree)
        {
            //若是领取付费奖励 则要检查购买状态 免费状态时不允许领取付费奖励
            if (!isClaimFree && PurchaseState == BPPurchaseState.Free)
                return false;
            //根据id获取对应配置信息
            var curMilestoneInfo = Game.Manager.configMan.GetBpMilestoneConfig(id);
            if (curMilestoneInfo == null)
                return false;
            //检查传入id的里程碑是否可以领奖
            var level = curMilestoneInfo.ShowNum - 1;
            if (level > _milestoneLevel)
                return false;
            //获取对应领奖信息
            if (!RewardClaimStateDict.TryGetValue(id, out var claimStateInfo))
                return false;
            //是否已领奖
            var hasClaim = isClaimFree ? claimStateInfo.Item1 : claimStateInfo.Item2;
            return !hasClaim;
        }
        
        //UI上手动领取里程碑奖励 调用时自行维护container,方法内部不负责Clear或Free 
        public bool TryClaimMilestoneReward(int id, bool isClaimFree, PoolMapping.Ref<List<RewardCommitData>> container)
        {
            //若是领取付费奖励 则要检查购买状态 免费状态时不允许领取付费奖励
            if (!isClaimFree && PurchaseState == BPPurchaseState.Free)
                return false;
            if (id <= 0 || !container.Valid)
                return false;
            //根据id获取对应配置信息
            var curMilestoneInfo = Game.Manager.configMan.GetBpMilestoneConfig(id);
            if (curMilestoneInfo == null)
                return false;
            //检查传入id的里程碑是否可以领奖
            var level = curMilestoneInfo.ShowNum - 1;
            if (level > _milestoneLevel)
                return false;
            //获取对应领奖信息
            if (!RewardClaimStateDict.TryGetValue(id, out var claimStateInfo))
                return false;
            var hasClaim = isClaimFree ? claimStateInfo.Item1 : claimStateInfo.Item2;
            if (hasClaim)
                return false;
            //处理奖励配置
            var rewardConf = isClaimFree ? curMilestoneInfo.RewardFree : curMilestoneInfo.RewardPay;
            if (rewardConf == null || rewardConf.Count < 1)
                return false;
            var reasonFrom = isClaimFree ? ReasonString.bp_milestone : ReasonString.bp_milestone_purchase;
            //处理领奖 支持配置多个奖励
            foreach (var rewardStr in rewardConf)
            {
                var reward = rewardStr.ConvertToRewardConfig();
                if (reward != null)
                {
                    var rewardData = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, reasonFrom);
                    container.obj.Add(rewardData);
                }
            }
            //对应标记领取状态为true
            if (isClaimFree)
                RewardClaimStateDict[id] = (true, claimStateInfo.Item2, claimStateInfo.Item3);
            else
                RewardClaimStateDict[id] = (claimStateInfo.Item1, true, claimStateInfo.Item3);
            //领取里程碑奖励时打点
            var detailConf = GetCurDetailConfig();
            var totalCount = detailConf?.MileStones?.Count ?? 0;
            totalCount = totalCount > 0 ? totalCount - 1 : 0;
            DataTracker.bp_milestone_claim.Track(this, detailConf?.Diff ?? 0, level + 1, 
                totalCount, id, isClaimFree, _purchaseState);
            return true;
        }

        private void _InitRewardClaimStateDict()
        {
            RewardClaimStateDict.Clear();
            var allMilestoneInfo = GetCurDetailConfig()?.MileStones;
            if (allMilestoneInfo == null || allMilestoneInfo.Count < 1)
                return;
            for (var i = 0; i < allMilestoneInfo.Count - 1; i++)    ////默认最后一个为循环奖励
            {
                var id = allMilestoneInfo[i];
                RewardClaimStateDict.Add(id, (false, false, false));
            }
        }
        
        #endregion
        
        #region 循环奖励

        //循环奖励的可领取次数 会进存档
        public int CycleAvailableCount { get; private set; } = 0;
        //循环奖励的已领取次数 会进存档
        public int CycleReceivedCount { get; private set; } = 0;
        //缓存循环奖励的随机池
        private List<(int, int, int)> _cycleRandomPool = new List<(int, int, int)>();
        
        public List<(int, int, int)> GetCycleRandomPool()
        {
            return _cycleRandomPool;
        }

        //检查当前是否处于循环奖励阶段
        public bool CheckMilestoneCycle()
        {
            var allMilestoneInfo = GetCurDetailConfig()?.MileStones;
            return allMilestoneInfo != null && _milestoneLevel + 1 >= allMilestoneInfo.Count - 1;
        }
        
        //获取循环奖励配置信息id 对应BpMilestone.id
        public int GetCycleMilestoneId()
        {
            var allMilestoneInfo = GetCurDetailConfig()?.MileStones;
            if (allMilestoneInfo == null || allMilestoneInfo.Count < 1)
                return -1;
            //默认最后一个是循环奖励id
            return allMilestoneInfo[^1];
        }
        
        //UI上手动一次性领取全部的循环奖励 调用时自行维护container,方法内部不负责Clear或Free 
        public bool TryClaimAllCycleReward(PoolMapping.Ref<List<RewardCommitData>> container)
        {
            //免费状态时不允许领取循环奖励
            if (PurchaseState == BPPurchaseState.Free)
                return false;
            if (CycleAvailableCount < 1 || !container.Valid)
                return false;
            var curDetailConf = GetCurDetailConfig();
            var allMilestoneInfo = curDetailConf?.MileStones;
            if (allMilestoneInfo == null || allMilestoneInfo.Count < 1)
                return false;
            if (_cycleRandomPool.Count <= 0)
                return false;
            //借助dict收集整合任务奖励
            var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();
            //一次性领取全部的循环奖励
            for (var i = 0; i < CycleAvailableCount; i++)
            {
                var info = _cycleRandomPool.RandomChooseByWeight(e => e.Item3);
                //收集奖励信息
                CollectReward(rewardMap, info.Item1, info.Item2);
            }
            //统一领取整理好后的循环奖励
            var rewardMan = Game.Manager.rewardMan;
            foreach (var rewardInfo in rewardMap)
            {
                var reward = rewardMan.BeginReward(rewardInfo.Key, rewardInfo.Value, ReasonString.bp_cycle_reward);
                container.obj.Add(reward);
            }
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
            //领取循环奖励时打点
            DataTracker.bp_cycle_claim.Track(this, curDetailConf.Diff, CycleAvailableCount, allMilestoneInfo[^1]);
            //领取完后记录已领取次数 并清0
            CycleReceivedCount += CycleAvailableCount;
            CycleAvailableCount = 0;
            return true;
        }

        private void _RefreshCycleRandomPool()
        {
            if (_cycleRandomPool.Count > 0)
                return;
            var allMilestoneInfo = GetCurDetailConfig()?.MileStones;
            if (allMilestoneInfo == null)
                return;
            var cycleConf = Game.Manager.configMan.GetBpMilestoneConfig(allMilestoneInfo[^1]);
            if (cycleConf == null || cycleConf.CircleReward.Count <= 0)
                return;
            foreach (var r in cycleConf.CircleReward)
            {
                var info = r.ConvertToInt3();
                _cycleRandomPool.Add(info);
            }
        }

        #endregion

        #region 里程碑展示奖励收集逻辑

        //奖励收集合并展示的类型 不同类型收集的逻辑不同
        public enum RewardCollectType
        {
            Free,   //纯免费奖励 包括所有未领取的免费档里程碑奖励
            Pay,    //纯付费奖励 包括所有未领取的付费档里程碑奖励 + 所有的循环宝箱
            All,    //免费+付费
        }
        
        //收集且合并展示当前可获得的所有奖励 区分纯免费 付费一档 付费二档 挽留
        //container 存储奖励的id和数量
        public void CollectAllCanClaimReward(RewardCollectType collectType, PoolMapping.Ref<List<(int, int)>> container)
        {
            if (!Valid)
                return;
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return;
            var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //奖励整合结果
            if (collectType == RewardCollectType.Free)
            {
                _CollectFreeReward(rewardMap);
            }
            else if (collectType == RewardCollectType.Pay)
            {
                _CollectPayReward(rewardMap);
            }
            else if (collectType == RewardCollectType.All)
            {
                _CollectFreeReward(rewardMap);
                _CollectPayReward(rewardMap);
            }
            //将整合的数据导进container
            foreach (var rewardInfo in rewardMap)
            {
                container.obj.Add((rewardInfo.Key, rewardInfo.Value));
            }
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
        }

        //收集目前可以领取的所有免费奖励
        private void _CollectFreeReward(Dictionary<int, int> rewardMap)
        {
            foreach (var state in RewardClaimStateDict)
            {
                //找到没有领取免费奖励的id 检查目前等级下是否可以领取
                if (state.Value.Item1) 
                    continue;
                //获取对应配置信息
                var curMilestoneInfo = Game.Manager.configMan.GetBpMilestoneConfig(state.Key);
                if (curMilestoneInfo == null) 
                    continue;
                //检查该里程碑是否达到领奖等级
                var level = curMilestoneInfo.ShowNum - 1;
                if (level > _milestoneLevel)
                    continue;
                //收集免费奖励
                foreach (var rewardStr in curMilestoneInfo.RewardFree)
                {
                    var r = rewardStr.ConvertToRewardConfig();
                    if (r != null)
                    {
                        CollectReward(rewardMap, r.Id, r.Count);
                    }
                }
            }
        }
        
        //收集目前可以领取的所有付费奖励 = 里程碑奖励+循环奖励  循环奖励以宝箱icon的形式展示出来
        private void _CollectPayReward(Dictionary<int, int> rewardMap)
        {
            //收集里程碑奖励
            foreach (var state in RewardClaimStateDict)
            {
                //找到没有领取付费奖励的id 检查目前等级下是否可以领取
                if (state.Value.Item2) 
                    continue;
                //获取对应配置信息
                var curMilestoneInfo = Game.Manager.configMan.GetBpMilestoneConfig(state.Key);
                if (curMilestoneInfo == null) 
                    continue;
                //检查该里程碑是否达到领奖等级
                var level = curMilestoneInfo.ShowNum - 1;
                if (level > _milestoneLevel)
                    continue;
                //收集付费奖励
                foreach (var rewardStr in curMilestoneInfo.RewardPay)
                {
                    var r = rewardStr.ConvertToRewardConfig();
                    if (r != null)
                    {
                        CollectReward(rewardMap, r.Id, r.Count);
                    }
                }
            }
            //当前有可收集次数时 才收集循环奖励
            if (CycleAvailableCount > 0)
            {
                var cycleRewardId = ConfD?.CircleChestId ?? 0;
                if (cycleRewardId > 0)
                {
                    CollectReward(rewardMap, cycleRewardId, CycleAvailableCount);
                }
            }
        }

        #endregion

        #region 里程碑结算时奖励自动收集+领取逻辑

        private void _AutoClaimReward()
        {
            //实际领取奖励前 先把要预览显示的内容收集好；这里还需要注意：循环奖励宝箱在此时只是预览Icon和数量的状态 并未随机出实际要发的奖励
            var previewAll = PoolMapping.PoolMappingAccess.Take(out List<(int, int)> _);
            var previewFree = PoolMapping.PoolMappingAccess.Take(out List<(int, int)> _);
            CollectAllCanClaimReward(RewardCollectType.All, previewAll);
            CollectAllCanClaimReward(RewardCollectType.Free, previewFree);
            var rewardMan = Game.Manager.rewardMan;
            //实际领取
            var container = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);
            var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //奖励整合结果
            //收集奖励同时会打点和修改对应数据 区分已付费和未付费
            //先领取所有免费的
            _TryClaimAllMilestoneReward(true, rewardMap);
            //统一领取整理好后的免费奖励
            foreach (var rewardInfo in rewardMap)
            {
                var reward = rewardMan.BeginReward(rewardInfo.Key, rewardInfo.Value, ReasonString.bp_end);
                rewardList.Add(reward);
            }
            //再领取所有付费的
            if (PurchaseState != BPPurchaseState.Free)
            {
                rewardMap.Clear();
                _TryClaimAllMilestoneReward(false, rewardMap);
                //统一领取整理好后的付费奖励
                foreach (var rewardInfo in rewardMap)
                {
                    var reward = rewardMan.BeginReward(rewardInfo.Key, rewardInfo.Value, ReasonString.bp_end_purchase);
                    rewardList.Add(reward);
                }
            }
            //弹脸
            object[] customParams = 
            { 
                container,      //参数1: 活动结束时 直接自动帮领的所有奖励
                previewAll,     //参数2: 用于玩家未购买BP或者已购买时界面上的预览显示（数据层要提前构造好 因为实际领取后就拿不到了）
                previewFree     //参数3: 用于玩家未购买且放弃挽留购买项时界面上的预览显示（数据层要提前构造好 因为实际领取后就拿不到了）
            };
            Game.Manager.screenPopup.TryQueue(EndPopup.popup, PopupType.Login, customParams);
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
        }
        
        //仅在内部结算时使用 领取所有可领取的免费/付费里程碑+循环奖励  可以传参决定是否仅领取免费奖励
        private void _TryClaimAllMilestoneReward(bool isOnlyFree, Dictionary<int, int> rewardMap)
        {
            //打点需要的字段
            var detailConf = GetCurDetailConfig();
            var diff = detailConf?.Diff ?? 0;
            var totalCount = detailConf?.MileStones?.Count ?? 0;
            totalCount = totalCount > 0 ? totalCount - 1 : 0;
            //收集发生变化的id列表
            var changeIdList = ObjectPool<List<int>>.GlobalPool.Alloc();    
            //收集里程碑奖励
            foreach (var state in RewardClaimStateDict)
            {
                if (isOnlyFree)
                    //找到没有领取免费奖励的id 检查目前等级下是否可以领取
                    if (state.Value.Item1) continue;
                else
                    //找到没有领取免费奖励或付费奖励的id 检查目前等级下是否可以领取
                    if (state.Value.Item1 && state.Value.Item2) continue;
                
                var id = state.Key;
                //获取对应配置信息
                var curMilestoneInfo = Game.Manager.configMan.GetBpMilestoneConfig(id);
                if (curMilestoneInfo == null) continue;
                //检查该里程碑是否达到领奖等级
                var level = curMilestoneInfo.ShowNum - 1;
                if (level > _milestoneLevel) continue;

                if (!state.Value.Item1)
                {
                    //收集免费奖励 同时打点
                    foreach (var rewardStr in curMilestoneInfo.RewardFree)
                    {
                        var r = rewardStr.ConvertToRewardConfig();
                        if (r != null)
                        {
                            CollectReward(rewardMap, r.Id, r.Count);
                        }
                    }
                    //领取免费奖励时打点
                    DataTracker.bp_milestone_claim.Track(this, diff, level + 1, totalCount, id, true, _purchaseState);
                }

                if (!isOnlyFree && !state.Value.Item2)
                {
                    //收集付费奖励 同时打点
                    foreach (var rewardStr in curMilestoneInfo.RewardPay)
                    {
                        var r = rewardStr.ConvertToRewardConfig();
                        if (r != null)
                        {
                            CollectReward(rewardMap, r.Id, r.Count);
                        }
                    }
                    //领取付费奖励时打点
                    DataTracker.bp_milestone_claim.Track(this, diff, level + 1, totalCount, id, false, _purchaseState);
                }
                //收集发生变化的id
                changeIdList.Add(id);
            }
            //统一处理发生变化的id
            foreach (var id in changeIdList)
            {
                if (RewardClaimStateDict.TryGetValue(id, out var claimStateInfo))
                {
                    //对应标记领取状态为true
                    if (isOnlyFree)
                        RewardClaimStateDict[id] = (true, claimStateInfo.Item2, claimStateInfo.Item3);
                    else
                        RewardClaimStateDict[id] = (true, true, claimStateInfo.Item3);
                }
            }
            //数据回收
            ObjectPool<List<int>>.GlobalPool.Free(changeIdList);
            //收集循环奖励
            if (!isOnlyFree)
            {
                if (CycleAvailableCount > 0 && _cycleRandomPool.Count > 0)
                {
                    //一次性领取全部的循环奖励
                    for (var i = 0; i < CycleAvailableCount; i++)
                    {
                        var info = _cycleRandomPool.RandomChooseByWeight(e => e.Item3);
                        //收集奖励信息
                        CollectReward(rewardMap, info.Item1, info.Item2);
                    }
                    //领取循环奖励时打点
                    DataTracker.bp_cycle_claim.Track(this, diff, CycleAvailableCount, GetCycleMilestoneId());
                    //领取完后记录已领取次数 并清0
                    CycleReceivedCount += CycleAvailableCount;
                    CycleAvailableCount = 0;
                }
            }
        }

        private static void CollectReward(Dictionary<int, int> map, int id, int count, int maxCount = -1)
        {
            //maxCount为-1表示没有最大数量限制
            if (maxCount == -1)
            {
                if (map.ContainsKey(id)) map[id] += count;
                else map.Add(id, count);
            }
            else if (maxCount > 0)
            {
                if (map.TryGetValue(id, out var curCount))
                {
                    var checkCount = curCount + count;
                    map[id] = Mathf.Min(checkCount, maxCount);
                }
                else
                {
                    map.Add(id, Mathf.Min(count, maxCount));
                }
            }
        }
        
        #endregion

        #endregion

        #endregion
        
        #region 任务

        #region 对外参数/方法

        //日刷任务下次刷新时间
        public long TaskRefreshTs => _nextRefreshTs;
        //目前未领取奖励的循环任务数量
        public int UnClaimCycleTaskCount => _cycleFinishCount - _cycleClaimCount;
        
        //获取所有任务数据
        //注意这里拿到的数据始终是排序好后的顺序，且包含循环任务
        public List<BPTaskData> GetBPTaskDataList()
        {
            return _bpTaskDataList;
        }

        //检查是否有已完成未领奖的任务
        public bool CheckHasUnClaimTask()
        {
            var hasUnClaim = false;
            foreach (var taskData in _bpTaskDataList)
            {
                if (taskData.State == BPTaskState.UnClaim)
                {
                    hasUnClaim = true;
                    break;
                }
            }
            return hasUnClaim || UnClaimCycleTaskCount > 0;
        }
        
        //任务刷新界面中主动调用填充新解锁的任务
        public bool FillNewTaskList(PoolMapping.Ref<List<BPTaskData>> container)
        {
            if (!container.Valid)
                return false;
            //所有日刷任务都会被填充进container
            foreach (var taskData in _bpTaskDataList)
            {
                if (!taskData.IsCycle)
                    container.obj.Add(taskData);
            }
            return true;
        }
        
        //任务完成界面中主动调用填充目前处于已完成待领奖状态的任务 若循环任务有已完成待领奖次数，则也会加进去
        public bool FillUnClaimTaskList(PoolMapping.Ref<List<BPTaskData>> container)
        {
            if (!container.Valid)
                return false;
            foreach (var taskData in _bpTaskDataList)
            {
                if (!taskData.IsCycle)
                {
                    if (taskData.State == BPTaskState.UnClaim)
                        container.obj.Add(taskData);
                }
                else
                {
                    //完成了一次循环任务 就填充几次数据
                    for (var i = 0; i < UnClaimCycleTaskCount; i++)
                    {
                        container.obj.Add(taskData);
                    }
                }
            }
            return true;
        }

        //界面上玩家主动操作领取任务奖励
        public void ClaimAllTaskReward(PoolMapping.Ref<List<RewardCommitData>> container)
        {
            //读配置 获取二档时的倍数 用于打点
            var privilege = 0;
            if (PurchaseState == BPPurchaseState.Luxury)
            {
                var bpPackInfo = GetBpPackInfoByType(BPPurchaseType.Luxury);
                privilege = bpPackInfo?.PrivilegeInfo ?? 0;
            }
            //借助dict收集整合任务奖励
            var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();
            foreach (var taskData in _bpTaskDataList)
            {
                //日刷任务只领取一次奖励
                if (!taskData.IsCycle)
                {
                    if (taskData.State != BPTaskState.UnClaim)
                        continue;
                    //收集任务奖励信息
                    CollectReward(rewardMap, taskData.Reward.Id, taskData.Reward.Count);
                    //领完后活动状态置为已领取
                    taskData.UpdateTaskState(BPTaskState.Claimed);
                    //打点
                    taskData.TrackClaimReward(this, GetCurDetailConfig()?.Diff ?? 0, _purchaseState, 1, privilege);
                }
                //循环任务一次性领取所有未领取的奖励 且领完后保持当前状态不变
                else
                {
                    var unClaimCount = _cycleFinishCount - _cycleClaimCount;
                    for (var i = 0; i < unClaimCount; i++)
                    {
                        //收集任务奖励信息
                        CollectReward(rewardMap, taskData.Reward.Id, taskData.Reward.Count);
                        //打点
                        taskData.TrackClaimReward(this, GetCurDetailConfig()?.Diff ?? 0, _purchaseState, 1, privilege);
                    }
                    _cycleClaimCount = _cycleFinishCount;
                }
            }
            //统一领取整理好后的任务奖励
            var rewardMan = Game.Manager.rewardMan;
            foreach (var rewardInfo in rewardMap)
            {
                var reward = rewardMan.BeginReward(rewardInfo.Key, rewardInfo.Value, ReasonString.bp_task);
                container.obj.Add(reward);
            }
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
            //领奖后排序
            _SortTaskDataList();
            //领取完后立即存档
            Game.Manager.archiveMan.SendImmediately(true);
            //发事件通知界面刷新
            MessageCenter.Get<MSG.GAME_BP_TASK_STATE_CHANGE>().Dispatch();
        }

        #endregion
        
        #region 任务基本数据

        //当前所有任务数据List 包含日刷任务和循环任务 且始终为排序好后的顺序
        private List<BPTaskData> _bpTaskDataList = new List<BPTaskData>();
        
        //进存档字段
        //记录各个任务类型在活动期间的进度值。
        //Item1记录日刷任务进度值，会在跨天时清0；Item2记录循环任务进度值，跨天时不清0
        private List<(int, int)> _progressValue;
        
        //BP任务的状态类型
        public enum BPTaskState
        {
            UnFinish = 0,   //未完成
            UnClaim = 1,    //已完成未领奖
            Claimed = 2,    //已完成已领奖
        }
        
        public class BPTaskData
        {
            public int Id;                  //任务配置Id
            public BpTask Conf;             //任务配置
            public RewardConfig Reward;     //任务完成后发的奖励
            public int Priority;            //排序优先级
            public int TaskType;            //任务类型
            public bool IsCycle;            //是否是循环任务
            //进存档
            public int RequireCount;        //该任务完成所需要的总数值
            public BPTaskState State;       //该任务目前状态 
            public int ArchiveState;           //该任务目前状态 仅用于存档时使用 避免存档时频繁转换类型
            //界面使用
            public int BelongActId;         //任务所属活动Id
            public int ProgressValue;       //当前任务进度值 存一下用于界面显示

            //第一次创建
            public void InitByConf(BpTask conf, int belongActId, bool isCycle)
            {
                Conf = conf;
                Id = Conf.Id;
                Reward = Conf.Reward.ConvertToRewardConfig();
                Priority = Conf.Sort;
                TaskType = (int)Conf.TaskType;
                IsCycle = isCycle;
                RequireCount = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(Conf.Parameter);
                ArchiveState = 0;
                State = BPTaskState.UnFinish;
                BelongActId = belongActId;
            }
            
            //读存档时创建
            public void InitById(int id, int requireCount, int state, int belongActId, bool isCycle)
            {
                Id = id;
                Conf = Game.Manager.configMan.GetBpTaskConfig(Id);
                if (Conf != null)
                {
                    Reward = Conf.Reward.ConvertToRewardConfig();
                    Priority = Conf.Sort;
                    TaskType = (int)Conf.TaskType;
                }
                IsCycle = isCycle;
                RequireCount = requireCount;
                ArchiveState = state;
                State = (BPTaskState)state;
                BelongActId = belongActId;
            }

            public void UpdateTaskState(BPTaskState state)
            {
                State = state;
                ArchiveState = (int)state;
            }

            public void UpdateTaskValue(int value)
            {
                ProgressValue = value;
            }

            //任务领奖时打点（每个任务打1个点，循环任务每1个循环打1次点）
            public void TrackClaimReward(ActivityLike act, int diff, int typeId, int type, int privilege)
            {
                var rewardNum = Reward?.Count ?? 0;
                if (privilege > 0)
                    rewardNum = rewardNum * privilege / 10;     //配置上倍率会扩大10倍 所以实际计算时再除回去
                DataTracker.bp_task_complete.Track(act, diff, Id, typeId, type, rewardNum);
            }
            
            public override string ToString()
            {
                return $"ErgTaskData(Id={Id}, RequireCount={RequireCount}, State={State}, BelongActId = {BelongActId}, ProgressValue = {ProgressValue}, IsCycle = {IsCycle}, Conf={Conf})";
            }
        }

        private void _InitTaskProgressValue()
        {
            //使用这种方式初始化是为了便于存档读取
            var count = Enum.GetValues(typeof(BpTaskType)).Length;
            _progressValue = new List<(int, int)>(count);
            for (var i = 0; i < count; i++)
            {
                _progressValue.Add((0, 0));
            }
        }
        
        private void _InitTaskDataList()
        {
            if (!Valid)
                return;
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return;
            var configMan = Game.Manager.configMan;
            //创建日刷任务数据
            foreach (var taskId in detailConf.DailyRefreshTask)
            {
                var taskConf = configMan.GetBpTaskConfig(taskId);
                if (taskConf == null) continue;
                var taskData = new BPTaskData();
                taskData.InitByConf(taskConf, Id, false);
                _bpTaskDataList.Add(taskData);
            }
            //创建循环任务数据
            var cycleTaskConf = configMan.GetBpTaskConfig(detailConf.CircleTask);
            if (cycleTaskConf != null)
            {
                var taskData = new BPTaskData();
                taskData.InitByConf(cycleTaskConf, Id, true);
                _bpTaskDataList.Add(taskData);
            }
            //按任务状态排序
            _SortTaskDataList();
            //初始时刷新任务目前最新的进度值
            _UpdateTaskProgressValue();
        }

        //排序
        private void _SortTaskDataList()
        {
            _bpTaskDataList.Sort(_Compare);
        }
        
        //排序规则  日刷未完成＞循环＞日刷已完成  状态一致时用Priority从小到大排序
        private static int _Compare(BPTaskData a, BPTaskData b)
        {
            int stateCompare = _GetStateWeight(a.State).CompareTo(_GetStateWeight(b.State));
            if (stateCompare != 0)
                return stateCompare;
            int cycleCompare = _GetCycleTypeWeight(a.IsCycle).CompareTo(_GetCycleTypeWeight(b.IsCycle));
            if (cycleCompare != 0)
                return cycleCompare;
            return a.Priority.CompareTo(b.Priority);
        }
        
        // 状态权重排序：1（未领奖） > 0（未完成） > 2（已领奖）
        private static int _GetStateWeight(BPTaskState state)
        {
            return state switch
            {
                BPTaskState.UnClaim => 0,
                BPTaskState.UnFinish => 1,
                BPTaskState.Claimed => 2,
                _ => 3,
            };
        }
        
        // 是否为循环任务排序 0(日刷任务) > 1(循环任务)
        private static int _GetCycleTypeWeight(bool isCycle)
        {
            return isCycle ? 1 : 0;
        }
        
        //初始时刷新任务目前最新的进度值
        private void _UpdateTaskProgressValue()
        {
            foreach (var taskData in _bpTaskDataList)
            {
                if (_progressValue.TryGetByIndex(taskData.TaskType, out var value))
                {
                    //日刷任务使用Item1 循环任务使用Item2
                    var finalValue = !taskData.IsCycle ? value.Item1 : value.Item2 % taskData.RequireCount;
                    taskData.UpdateTaskValue(finalValue);
                }
            }
        }

        #endregion

        #region 任务进度变化

        private void _WhenCoinUse(CoinChange change)
        {
            if (change.type == CoinType.Gem) 
                _UpdateProgressValue(BpTaskType.Diamond, change.amount);
        }
        
        //只接受来自主棋盘的棋子合成
        private void _WhenBoardMerge(Item item)
        {
            var board = item?.world?.activeBoard;
            if (board == null || board.boardId != Constant.MainBoardId) 
                return;
            _UpdateProgressValue(BpTaskType.Merge, 1);
        }

        private void _WhenBoardSkill(Item item, SkillType skillType)
        {
            var board = item?.world?.activeBoard;
            //只考虑主棋盘
            if (board == null || board.boardId != Constant.MainBoardId) 
                return;
            //只在使用了万能棋子时才统计棋子合成次数
            if (skillType != SkillType.Upgrade)
                return;
            _UpdateProgressValue(BpTaskType.Merge, 1);
        }
        
        private void _WhenOrderFinish(IOrderData orderData)
        {
            //心想事成订单不计入任务进度
            if (orderData.IsMagicHour)
                return;
            _UpdateProgressValue(BpTaskType.Order, 1);
        }
        
        private void _UpdateProgressValue(BpTaskType type, int value) 
        {
            if (!Valid || _progressValue == null) return;
            var key = (int)type;
            if (_progressValue.TryGetByIndex(key, out var oldValue))
            {
                var finalValue1 = oldValue.Item1 + value;
                var finalValue2 = oldValue.Item2 + value;
                _progressValue[key] = (finalValue1, finalValue2);
                _CheckTaskState(type, finalValue1, finalValue2);
            }
        }

        #endregion

        #region 任务状态变化
        
        #region 任务进度/状态发生变化时 主棋盘任务Tips表现需要的内容

        // 任务变化类型
        public enum BPTaskChangeType
        {
            Progress,   //进度刷新
            Completed   //完成达成
        }

        // 单条任务更新信息
        public struct BPTaskUpdateInfo
        {
            public BpTaskType TaskType;
            public BPTaskData TaskData;
            public BPTaskChangeType ChangeType;

            public BPTaskUpdateInfo(BpTaskType type, BPTaskData data, BPTaskChangeType changeType)
            {
                TaskType = type;
                TaskData = data;
                ChangeType = changeType;
            }
        }

        private List<BPTaskUpdateInfo> _taskUpdateInfoList = new List<BPTaskUpdateInfo>();

        private void _ClearTaskUpdateInfo()
        {
            _taskUpdateInfoList.Clear();
        }

        private void _AddTaskUpdateInfo(BpTaskType type, BPTaskData data, BPTaskChangeType changeType)
        {
            _taskUpdateInfoList.Add(new BPTaskUpdateInfo(type, data, changeType));
        }

        private void _TryDispatchTaskUpdate()
        {
            if (_taskUpdateInfoList.Count > 0)
            {
                MessageCenter.Get<MSG.GAME_BP_TASK_UPDATED>().Dispatch(_taskUpdateInfoList);
            }
        }

        #endregion
        
        //目前认为循环任务只会有一个，所以起全局值来记录，避免存档中填充无效信息
        //循环任务累计完成次数
        private int _cycleFinishCount = 0;
        //循环任务累计领取奖励的次数
        private int _cycleClaimCount = 0;
        //日刷任务下次刷新时间
        private long _nextRefreshTs = 0;
        
        //检查各个任务是否已完成 已经是完成状态的则跳过
        //新完成的任务不会立即帮领，需要玩家手动领取，活动结束后如果付费了但是没领完 则会帮领
        //日刷任务使用checkValue   循环任务使用checkValueCycle
        private void _CheckTaskState(BpTaskType type, int checkValue, int checkValueCycle)
        {
            if (checkValue <= 0 || checkValueCycle <= 0)
                return;
            _ClearTaskUpdateInfo();
            var checkType = (int)type;
            var hasFinish = false;
            foreach (var taskData in _bpTaskDataList)
            {
                //已经是完成状态的则跳过
                if (taskData.State > BPTaskState.UnFinish || checkType != taskData.TaskType)
                    continue;
                //日刷任务和循环任务使用不同的值分开处理
                if (!taskData.IsCycle)
                {
                    //检查到任务符合完成条件，若是日刷任务则置为已完成未领奖状态
                    if (checkValue >= taskData.RequireCount)
                    {
                        hasFinish = true;
                        taskData.UpdateTaskState(BPTaskState.UnClaim);
                        //记录任务完成
                        _AddTaskUpdateInfo(type, taskData, BPTaskChangeType.Completed);
                    }
                    else
                    {
                        //记录任务进度刷新
                        _AddTaskUpdateInfo(type, taskData, BPTaskChangeType.Progress);
                    }
                    //刷新当前任务进度值
                    taskData.UpdateTaskValue(checkValue);
                }
                else
                {
                    var oldFinishCount = _cycleFinishCount;
                    //检查到任务符合完成条件，若是循环任务则仍置为未完成状态，会记录最新的循环任务完成次数
                    if (checkValueCycle >= taskData.RequireCount)
                    {
                        taskData.UpdateTaskState(BPTaskState.UnFinish);
                        //循环任务累积完成次数
                        _cycleFinishCount = checkValueCycle / taskData.RequireCount;
                        if (_cycleFinishCount > oldFinishCount)
                        {
                            hasFinish = true;
                            //记录任务完成
                            _AddTaskUpdateInfo(type, taskData, BPTaskChangeType.Completed);
                        }
                        else
                        {
                            //记录任务进度刷新
                            _AddTaskUpdateInfo(type, taskData, BPTaskChangeType.Progress);
                        }
                    }
                    else
                    {
                        //记录任务进度刷新
                        _AddTaskUpdateInfo(type, taskData, BPTaskChangeType.Progress);
                    }
                    //取余RequireCount 确定最终要刷新的值
                    var finalValue = checkValueCycle % taskData.RequireCount;
                    //刷新当前任务进度值
                    taskData.UpdateTaskValue(finalValue);
                }
            }
            if (hasFinish)
            {
                _SortTaskDataList();
                MessageCenter.Get<MSG.GAME_BP_TASK_STATE_CHANGE>().Dispatch();
            }
            //广播任务刷新事件
            _TryDispatchTaskUpdate();
        }
        
        //跨天循环任务刷新
        private void _CheckTaskRefresh()
        {
            //活动结束时不走刷新逻辑
            if (!Active)
                return;
            var now = Game.Instance.GetTimestampSeconds();
            if (now < _nextRefreshTs)
                return;
            //计算下次刷新的节点
            var offsetHour = Game.Manager.configMan.globalConfig.BpTaskRefresh;
            _nextRefreshTs = ((now - offsetHour * 3600) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + offsetHour * 3600;
            //跨天时 自动领取所有未领取的任务奖励 
            _AutoRefreshTask();
            //跨天时 日刷任务进度值清0 循环任务进度值不清0
            _ResetTaskValue();
        }

        //跨天或活动结束时：
        //1、自动领取未领取的任务奖励  此时奖励直接发放 没有获得表现
        //2、重置活动状态
        private void _AutoRefreshTask()
        {
            //读配置 获取二档时的倍数 用于打点
            var privilege = 0;
            if (PurchaseState == BPPurchaseState.Luxury)
            {
                var bpPackInfo = GetBpPackInfoByType(BPPurchaseType.Luxury);
                privilege = bpPackInfo?.PrivilegeInfo ?? 0;
            }
            //借助dict收集整合任务奖励
            var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();
            foreach (var taskData in _bpTaskDataList)
            {
                //日刷任务只领取一次奖励
                if (!taskData.IsCycle)
                {
                    //UnClaim状态的任务 直接领奖励 并重置为UnFinish
                    if (taskData.State == BPTaskState.UnClaim)
                    {
                        //收集任务奖励信息
                        CollectReward(rewardMap, taskData.Reward.Id, taskData.Reward.Count);
                        //刷新任务状态
                        taskData.UpdateTaskState(BPTaskState.UnFinish);
                        //打点
                        taskData.TrackClaimReward(this, GetCurDetailConfig()?.Diff ?? 0, _purchaseState, 2, privilege);
                    }
                    //Claimed状态的任务 只重置为UnFinish
                    else if (taskData.State == BPTaskState.Claimed)
                    {
                        taskData.UpdateTaskState(BPTaskState.UnFinish);
                    }
                }
                //循环任务一次性领取所有未领取的奖励
                else
                {
                    var unClaimCount = _cycleFinishCount - _cycleClaimCount;
                    for (var i = 0; i < unClaimCount; i++)
                    {
                        //收集任务奖励信息
                        CollectReward(rewardMap, taskData.Reward.Id, taskData.Reward.Count);
                        //打点
                        taskData.TrackClaimReward(this, GetCurDetailConfig()?.Diff ?? 0, _purchaseState, 2, privilege);
                    }
                    _cycleClaimCount = _cycleFinishCount;
                }
            }
            //统一领取整理好后的任务奖励
            var rewardMan = Game.Manager.rewardMan;
            foreach (var reward in rewardMap)
            {
                rewardMan.CommitReward(rewardMan.BeginReward(reward.Key, reward.Value, ReasonString.bp_task));
            }
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
            //领奖后排序
            _SortTaskDataList();
            //发事件通知界面刷新
            MessageCenter.Get<MSG.GAME_BP_TASK_STATE_CHANGE>().Dispatch();
        }

        private void _ResetTaskValue()
        {
            if (_progressValue == null)
                return;
            var count = Enum.GetValues(typeof(BpTaskType)).Length;
            for (var i = 0; i < count; i++)
            {
                //Item1记录日刷任务进度值，会在跨天时清0；Item2记录循环任务进度值，跨天时不清0
                var oldValue = _progressValue[i];
                var newValue = (0, oldValue.Item2);
                _progressValue[i] = newValue;
            }
            _UpdateTaskProgressValue();
        }
        
        #endregion

        #endregion
    }

    public class BPEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => entry;
        private readonly ListActivity.Entry entry;
        private readonly BPActivity activity;

        public BPEntry(ListActivity.Entry _entry, BPActivity _activity)
        {
            (entry, this.activity) = (_entry, _activity);
            var showRed = _activity.CheckCanShowRP(out var num);
            entry.dot.SetActive(showRed);
            entry.dotCount.gameObject.SetActive(showRed);
            entry.dotCount.SetText(num <= 99 ? num.ToString() : "99+");
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            // 借助倒计时每秒刷新 刷红点
            var showRed = activity.CheckCanShowRP(out var num);
            entry.dot.SetActive(showRed);
            entry.dotCount.gameObject.SetActive(showRed);
            entry.dotCount.SetText(num <= 99 ? num.ToString() : "99+");

            return UIUtility.CountDownFormat(diff_);
        }
    }
}