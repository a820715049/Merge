/*
 * @Author: chaoran.zhang
 * @Date: 2025-08-04 16:21:01
 * @LastEditors: chaoran.zhang
 * @LastEditTime: 2025-09-23 11:17:38
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Text;
using EL;
using fat.conf;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;

namespace FAT
{
    public class TrainMissionActivity : ActivityLike, IBoardEntry, IBoardArchive, IMergeItemIndicatorHandler
    {
        #region 存档字段
        private int _missionDetailID; //EventTrainMissionDetail ID
        private int _groupDetailID; //TrainGroupDetail ID
        private int _challengeIndex;//TrainChallenge
        private int _challengeComplete; //挑战完成情况
        private int _topOrderID; //上方订单
        private int _bottomOrderID; //下方订单
        private int _topOrderComplete; //上方订单完成情况
        private int _bottomOrderComplete;  //下方订单完成情况
        private bool _waitEnterNextChallenge; //等待进入下一轮
        private bool _waitRecycle; //等待结束回收
        private bool _needPlayEnterAnim; //需要播放火车进入动画
        private int _topOrderStartTime;
        private int _bottomOrderStartTime;
        private int _limitCommitCount;
        private int _trainQueue;
        private int _milestoneQueue;
        private int _challengeQueue = 1;
        #endregion

        #region 配置
        public EventTrainMissionRound missionRound;
        public EventTrainMission mission;
        public TrainChallenge trainChallenge; //当前挑战

        #endregion

        #region 运行字段
        public MergeWorld World { get; private set; } // 世界实体
        public MergeWorldTracer WorldTracer { get; private set; } // 世界实体追踪器
        public override ActivityVisual Visual => VisualMain.visual;
        public VisualRes VisualMain { get; } = new(UIConfig.UITrainMissionMain); // 主界面
        public VisualRes VisualHelp { get; } = new(UIConfig.UITrainMissionHelp); // 帮助界面
        public VisualRes VisualChooseGroup { get; } = new(UIConfig.UITrainMissionChooseGroup); // 选组界面
        public VisualRes VisualComplete { get; } = new(UIConfig.UITrainMissionComplete); // 本轮达成界面
        public VisualRes VisualLoading { get; } = new(UIConfig.UITrainMissionLoading); // 加载界面
        public VisualRes VisualPreview { get; } = new(UIConfig.UITrainMissionPreview); // 预览界面
        public VisualRes VisualReward { get; } = new(UIConfig.UITrainMissionReward); // 里程碑发奖界面
        public VisualRes VisualItemInfo { get; } = new(UIConfig.UITrainMissionItemInfo); // 任务信息界面
        public VisualPopup StartPopup { get; } = new(UIConfig.UITrainMissionBegin); // 活动开启
        public VisualPopup EndPopup { get; } = new(UIConfig.UITrainMissionEnd); // 活动结束
        public TrainMissionOrder topOrder = new();
        public TrainMissionOrder bottomOrder = new();
        public bool waitEnterNextChallenge => _waitEnterNextChallenge;
        public bool waitRecycle => _waitRecycle;
        public bool NeedPlayEnterAnim => _needPlayEnterAnim;
        public int challengeIndex => _challengeIndex;
        public int groupDetailID => _groupDetailID;
        private List<RewardCommitData> _orderRewardWaitCommit = new();
        private RewardCommitData _trainMilestoneRewardWaitCommit;
        private RewardCommitData _recycleReward;
        public RewardCommitData recycleReward => _recycleReward;
        private bool _hasConvert;

        public event Action Invalidate;
        #endregion

        #region ActivityLike
        public TrainMissionActivity(ActivityLite lite)
        {
            Lite = lite;
            missionRound = EventTrainMissionRoundVisitor.Get(Lite.Param);
        }

        // 角标
        public string BadgeAsset => Visual.Theme.AssetInfo.TryGetValue("badge", out var res) ? res : null;

        public FeatureEntry Feature => FeatureEntry.FeatureTrainMission;

        public override void SetupFresh()
        {
            _needPlayEnterAnim = true;
            _groupDetailID = 0;
            _RefreshMission();
            _RefreshDetailID();
            _InitWorld();
            _RefreshTheme();
            if (_GetCanChooseGroupDetailID().Count == 1)
            {
                ChooseGroup(_GetCanChooseGroupDetailID()[0]);
            }
            Game.Manager.screenPopup.Queue(StartPopup.popup);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(StartPopup.popup, state_, this);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            SaveNormalData(data_);
        }

        public void SaveNormalData(ActivityInstance data_)
        {
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _missionDetailID));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _groupDetailID));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _challengeIndex));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _challengeComplete));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, topOrder.orderID));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, topOrder.complete));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, bottomOrder.orderID));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, bottomOrder.complete));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _waitEnterNextChallenge));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _waitRecycle));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _needPlayEnterAnim));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, topOrder.startTime));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, bottomOrder.startTime));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _limitCommitCount));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _trainQueue));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _milestoneQueue));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _challengeQueue));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            LoadNormalData(data_);
            _RefreshMission();
            _RefreshTheme();
            if (!NeedChooseGroup()) { _UpdateChallenge(); }
            topOrder.SetData(_topOrderID, _topOrderComplete, _topOrderStartTime);
            bottomOrder.SetData(_bottomOrderID, _bottomOrderComplete, _bottomOrderStartTime);
        }

        public void LoadNormalData(ActivityInstance data_)
        {
            _missionDetailID = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _groupDetailID = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _challengeIndex = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _challengeComplete = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _topOrderID = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _topOrderComplete = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _bottomOrderID = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _bottomOrderComplete = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _waitEnterNextChallenge = RecordStateHelper.ReadBool(dataIndex++, data_.AnyState);
            _waitRecycle = RecordStateHelper.ReadBool(dataIndex++, data_.AnyState);
            _needPlayEnterAnim = RecordStateHelper.ReadBool(dataIndex++, data_.AnyState);
            _topOrderStartTime = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _bottomOrderStartTime = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _limitCommitCount = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _trainQueue = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _milestoneQueue = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _challengeQueue = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
        }

        public override void Open()
        {
            // 是否需要选择生成器组
            if (NeedChooseGroup())
            {
                UIManager.Instance.OpenWindow(VisualChooseGroup.res.ActiveR, this);
                return;
            }


            var item1 = BoardActivityUtility.GetHighestLevelItemIdInCategory(trainChallenge.ConnectSpawner[0], 1);
            var item2 = BoardActivityUtility.GetHighestLevelItemIdInCategory(trainChallenge.ConnectSpawner[1], 1);
            var cat1 = Game.Manager.mergeItemMan.GetCategoryConfig(trainChallenge.ConnectSpawner[0]);
            var cat2 = Game.Manager.mergeItemMan.GetCategoryConfig(trainChallenge.ConnectSpawner[1]);
            World.activeBoard.WalkAllItem((Item item) =>
            {
                if (cat1.Progress.Contains(item.tid))
                {
                    if (item.tid != item1)
                    {
                        var pos = item.coord;
                        World.activeBoard.DisposeItem(item);
                        World.activeBoard.SpawnItem(item1, pos.x, pos.y, false, false);
                    }
                }
                else if (cat2.Progress.Contains(item.tid))
                {
                    if (item.tid != item2)
                    {
                        var pos = item.coord;
                        World.activeBoard.DisposeItem(item);
                        World.activeBoard.SpawnItem(item2, pos.x, pos.y, false, false);
                    }
                }
            });
            TrainMissionUtility.EnterActivity();
        }

        public override void WhenEnd()
        {
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            if (NeedChooseGroup()) return;
            if (_hasConvert)
            {
                EndPopup.Popup(0, _recycleReward);
                return;
            }

            var totalDiff = 0;
            var cat1 = Game.Manager.mergeItemMan.GetCategoryConfig(trainChallenge.ConnectSpawner[0]);
            var cat2 = Game.Manager.mergeItemMan.GetCategoryConfig(trainChallenge.ConnectSpawner[1]);
            var list = new List<Item>();
            World.activeBoard.WalkAllItem((Item item) =>
            {
                if (!cat1.Progress.Contains(item.tid) && !cat2.Progress.Contains(item.tid))
                {
                    Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(item.tid, out var avg, out var real);
                    totalDiff += real;
                    list.Add(item);
                }
            });
            World.inventory.WalkAllItem((Item item) =>
            {
                Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(item.tid, out var avg, out var real);
                totalDiff += real;
                list.Add(item);
            });
            if (totalDiff > 0)
            {
                for (var i = 0; i < mission.EndRecycle.Count; i++)
                {
                    var config = mission.EndRecycle[i].ConvertToInt3();
                    if (totalDiff > config.Item1) { continue; }
                    _recycleReward = Game.Manager.rewardMan.BeginReward(config.Item2, config.Item3, ReasonString.train_end_reward);
                    _hasConvert = true;
                    break;
                }
            }
            EndPopup.Popup(0, _recycleReward);
            DataTracker.event_train_end.Track(this, groupDetailID, trainChallenge.Id, _challengeQueue + 1, ZString.Concat(list.Select(it => it.tid)), totalDiff,
                phase + 1, _recycleReward == null ? "" : ZString.Format("{0}:{1}", _recycleReward.rewardId, _recycleReward.rewardCount));
        }
        #endregion

        #region 接口
        public bool CheckCanShowRP()
        {
            for (var i = 0; i < topOrder.ItemInfos.Count; i++) { if (CheckMissionState(topOrder, i) == 2) { return true; } }
            for (var i = 0; i < bottomOrder.ItemInfos.Count; i++) { if (CheckMissionState(bottomOrder, i) == 2) { return true; } }
            return false;
        }

        public string BoardEntryAsset()
        {
            VisualMain.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        /// <summary>
        /// 玩家是够需要进入选择生成器链条界面
        /// </summary>
        /// <returns></returns>
        public bool NeedChooseGroup() => _groupDetailID == 0;


        /// <summary>
        /// 获取可选择的生成器链条组
        /// </summary>
        /// <param name="pairs">传入后内部会自动执行清理逻辑</param>
        public void GetGroupInfo(Dictionary<int, List<int>> pairs)
        {
            pairs.Clear();
            var groupDetailList = _GetCanChooseGroupDetailID();
            foreach (var detail in groupDetailList) { pairs.Add(detail, Enumerable.ToList(TrainGroupDetailVisitor.Get(detail).IncludeSpawner)); }
        }

        /// <summary>
        /// 确认生成器组
        /// </summary>
        /// <param name="id"></param>
        public void ChooseGroup(int id)
        {
            _groupDetailID = id;
            DataTracker.event_train_choose_group.Track(this, _groupDetailID);
            _ResetChallenge();
            _StartNewChallenge();
            _FillBoardItemByChallenge();
        }

        /// <summary>
        /// 获取当前里程碑进度
        /// </summary>
        /// <returns></returns>
        public int GetCurMilestoneProgress()
        {
            var progress = 0;
            for (var i = 0; i < trainChallenge.IncludeTrainMission.Count; i++) { if ((_challengeComplete & 1 << i) > 0) { progress++; } }
            return progress;
        }

        /// <summary>
        /// 获取当前挑战里程碑的最大值
        /// </summary>
        /// <returns></returns>
        public int GetCurMilestoneTotal() => trainChallenge.IncludeTrainMission.Count();

        /// <summary>
        /// 完成订单上的一节车厢任务
        /// </summary>
        /// <param name="order">订单</param>
        /// <param name="index">车厢序号</param>
        /// <returns></returns>
        public List<RewardCommitData> CompleteTrainMission(TrainMissionOrder order, int index)
        {
            var list = new List<RewardCommitData>();
            order.complete |= 1 << index;
            var info = order.ItemInfos[index];
            using (ObjectPool<List<ItemConsumeRequest>>.GlobalPool.AllocStub(out var toConsume))
            {
                toConsume.Add(new ItemConsumeRequest()
                {
                    itemId = info.itemID,
                    itemCount = 1
                });
                World.TryConsumeOrderItem(toConsume, null, false);
                if (_FinishOrder(order))
                {
                    _BeginMilestoneReward();
                    _FinishChallenge();
                    DataTracker.event_train_complete_train.Track(this, groupDetailID, trainChallenge.Id, _challengeIndex + 1,
                        order.orderID, ZString.Format("{0}/{1}", _orderRewardWaitCommit[0].rewardId, _orderRewardWaitCommit[0].rewardCount), _trainQueue, phase + 1);
                }
            }
            list.Add(Game.Manager.rewardMan.BeginReward(info.rewardID, info.rewardCount, ReasonString.train_item_reward));
            if (order.TryGetSpecialMission(index, out var endTime, out _))
            {
                if (Game.Instance.GetTimestampSeconds() < endTime)
                {
                    var special = order.specialMissionInfos.FirstOrDefault(e => e.orderIndex - 1 == index);
                    list.Add(Game.Manager.rewardMan.BeginReward(special.rewardID, special.rewardCount, ReasonString.train_limit_item_reward));
                    _limitCommitCount++;
                }
            }
            DataTracker.event_train_complete_item.Track(this, _groupDetailID, trainChallenge.Id, _challengeIndex + 1, order.orderID, order.ItemInfos[index].itemID,
                ZString.Concat(order.ItemInfos[index].rewardID, ':', order.ItemInfos[index].rewardCount), list.Count > 1,
                list.Count > 1 ? ZString.Format("{0}:{1}", list[1].rewardId, list[1].rewardCount) : "", list.Count > 1 ? _limitCommitCount : -1, phase + 1);
            return list;
        }

        /// <summary>
        /// 检测订单状态
        /// </summary>
        /// <param name="order"></param>
        /// <param name="index"></param>
        /// <returns> 1:未完成 2:可以完成 3:已完成 </returns>
        public int CheckMissionState(TrainMissionOrder order, int index)
        {
            if ((order.complete & 1 << index) > 0) return 3;
            var dic = WorldTracer.GetCurrentActiveBoardAndInventoryItemCount();
            var info = order.ItemInfos[index];
            return dic.Keys.Any(k => k == info.itemID) ? 2 : 1;
        }

        /// <summary>
        ///  进入下一个challenge
        /// </summary>
        public void EnterNextChallenge()
        {
            var list = new List<Item>();
            var cat1 = Game.Manager.mergeItemMan.GetCategoryConfig(trainChallenge.ConnectSpawner[0]);
            var cat2 = Game.Manager.mergeItemMan.GetCategoryConfig(trainChallenge.ConnectSpawner[1]);
            World.activeBoard.WalkAllItem((Item item) =>
            {
                if (cat1.Progress.Contains(item.tid) || cat2.Progress.Contains(item.tid)) { list.Add(item); }
            });
            _StartNewChallenge();
            _RefreshNextChallengeItem(list);
            _waitEnterNextChallenge = false;
            _needPlayEnterAnim = true;
        }

        /// <summary>
        /// 切换动画状态
        /// </summary>
        /// <param name="state"></param>
        public void ChangeAnimState(bool state) => _needPlayEnterAnim = state;

        /// <summary>
        /// 获取当前挑战的里程碑配置
        /// </summary>
        /// <param name="list"></param>
        public void GetTrainMilestones(List<TrainMilestone> list)
        {
            list.Clear();
            foreach (var id in trainChallenge.IncludeMilestone) { list.Add(TrainMilestoneVisitor.Get(id)); }
        }

        /// <summary>
        /// 获取待提交的订单完成奖励
        /// </summary>
        /// <returns></returns>
        public List<RewardCommitData> GetOrderWaitCommitReward()
        {
            var list = new List<RewardCommitData>();
            list.AddRange(_orderRewardWaitCommit);
            _orderRewardWaitCommit.Clear();
            return list;
        }

        /// <summary>
        /// 获取待提交的里程碑奖励
        /// </summary>
        /// <returns> 不为空，但是Count可能为0，Count为0表示没有触发里程碑奖励</returns>
        public RewardCommitData GetMilestoneWaitCommitReward()
        {
            var ret = _trainMilestoneRewardWaitCommit;
            _trainMilestoneRewardWaitCommit = null;
            return ret;
        }

        public RewardCommitData GetRecycleReward()
        {
            var ret = _recycleReward;
            _recycleReward = null;
            return ret;
        }

        /// <summary>
        /// 尝试往背包中放入棋子
        /// </summary>
        /// <param name="item"> item </param>
        /// <returns></returns>
        public bool PutItem(Item item)
        {
            if (item.TryGetItemComponent<ItemBonusCompoent>(out var bonus) && bonus.inventoryAutoUse)
            {
                item.parent.UseBonusItem(item);
                return true;
            }
            if (!ItemUtility.CanItemInInventory(item))
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BagIllegal);
                return false;
            }
            foreach (var id in trainChallenge.ConnectSpawner) { if (Game.Manager.mergeItemMan.GetCategoryConfigByItemId(item.tid).Id == id) { return false; } }
            if (!World.activeBoard.PutItemInInventory(item))
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BagFull);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从背包中取出棋子
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="bagId"></param>
        /// <returns></returns>
        public bool PeekItem(int idx, int bagId)
        {
            return World.activeBoard.GetItemFromInventory(idx, bagId);
        }

        public void FinishRoundDebug()
        {
            _waitRecycle = true;
        }

        /// <summary>
        /// 结束活动并回收棋子
        /// </summary>
        public List<Item> FinishRound()
        {
            var list = new List<Item>();
            var LogList = new List<Item>();
            var totalDiff = 0;
            var cat1 = Game.Manager.mergeItemMan.GetCategoryConfig(trainChallenge.ConnectSpawner[0]);
            var cat2 = Game.Manager.mergeItemMan.GetCategoryConfig(trainChallenge.ConnectSpawner[1]);
            World.activeBoard.WalkAllItem((Item item) =>
            {
                if (!cat1.Progress.Contains(item.tid) && !cat2.Progress.Contains(item.tid))
                {
                    list.Add(item);
                    LogList.Add(item);
                    Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(item.tid, out var avg, out var real);
                    totalDiff += real;
                }
            });
            World.inventory.WalkAllItem((Item item) =>
            {
                Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(item.tid, out var avg, out var real);
                totalDiff += real;
                LogList.Add(item);
            });
            if (totalDiff > 0)
            {
                for (var i = 0; i < mission.EndRecycle.Count; i++)
                {
                    var config = mission.EndRecycle[i].ConvertToInt3();
                    if (totalDiff > config.Item1) { continue; }
                    _recycleReward = Game.Manager.rewardMan.BeginReward(config.Item2, config.Item3, ReasonString.train_end_reward);
                    _hasConvert = true;
                    break;
                }
            }
            phase++;
            if (phase >= missionRound.IncludeTrainId.Count)
            {
                Game.Manager.activity.EndImmediate(this, false);
                DataTracker.event_train_end.Track(this, groupDetailID, trainChallenge.Id, _challengeQueue,
                ZString.Join(',', LogList.Select(it => it.tid)), totalDiff, phase, _recycleReward == null ? "" : ZString.Format("{0}:{1}", _recycleReward.rewardId, _recycleReward.rewardCount));
            }
            else { _EnterNextRound(); }
            return list;
        }

        public List<RewardConfig> GetTotalRewardPreview()
        {
            var dic = new Dictionary<int, int>();
            foreach (var id in TrainGroupDetailVisitor.Get(_groupDetailID).IncludeChallenge)
            {
                var challenge = TrainChallengeVisitor.Get(id);
                foreach (var orderId in challenge.IncludeTrainMission)
                {
                    var configs = TrainMissionVisitor.Get(orderId).Reward.Select(str => str.ConvertToRewardConfig());
                    foreach (var reward in configs)
                    {
                        if (dic.ContainsKey(reward.Id)) { dic[reward.Id] += reward.Count; }
                        else dic.Add(reward.Id, reward.Count);
                    }
                }

                foreach (var milestoneId in challenge.IncludeMilestone)
                {
                    var reward = TrainMilestoneVisitor.Get(milestoneId).Reward.ConvertToRewardConfig();
                    if (dic.ContainsKey(reward.Id)) { dic[reward.Id] += reward.Count; }
                    else dic.Add(reward.Id, reward.Count);
                }
            }
            var list = new List<RewardConfig>();
            foreach (var pair in dic)
            {
                list.Add(new RewardConfig() { Id = pair.Key, Count = pair.Value });
            }
            return list;
        }

        #endregion

        #region 棋盘逻辑
        /// <summary>
        /// 初始化棋盘，仅在活动第一次开启以及进入下一个轮次时调用一次
        /// </summary>
        private void _InitWorld()
        {
            _CreateWorld();
            _CreateWorldTracer();
            _Bind();
            _CreateInventory();
            Game.Manager.mergeBoardMan.InitializeBoard(World, mission.BoardId, true);
        }

        public void SetBoardData(fat.gamekitdata.Merge data)
        {
            if (data == null) { return; }
            _CreateWorld();
            _CreateWorldTracer();
            _Bind();
            Game.Manager.mergeBoardMan.InitializeBoard(World, mission.BoardId, false);
            World.Deserialize(data, null);
        }

        private void _CreateWorld()
        {
            World = new MergeWorld();
            if (!mission.IsOnBonus) { World.UnRegisterConfigMergeBonusHandler(); }
            if (!mission.IsOnBubble) { World.UnRegisterBubbleMergeBonusHandler(); }
            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry() { world = World, type = MergeWorldEntry.EntryType.TrainMission, });
        }

        private void _CreateWorldTracer()
        {
            WorldTracer = new MergeWorldTracer(_OnBoardItemChange, null);
        }

        private void _Bind()
        {
            WorldTracer.Bind(World);
            World.BindTracer(WorldTracer);
        }

        private void _CreateInventory()
        {
            World.inventory.AddBag(1);
            World.inventory.SetCapacity(mission.Storage, 1);
        }

        private void _OnBoardItemChange()
        {
            Invalidate?.Invoke();
        }


        public void FillBoardData(fat.gamekitdata.Merge data)
        {
            World?.Serialize(data);
        }

        /// <summary>
        /// 替换下一轮生成器
        /// </summary>
        private void _RefreshNextChallengeItem(List<Item> list)
        {
            var results = new List<int>();
            BoardActivityUtility.FillHighestLeveItemByCategory(trainChallenge.ConnectSpawner, results, 1);
            for (int i = 0; i < list.Count; i++)
            {
                World.activeBoard.ConvertItem(list[i], results[i]);
            }
        }
        #endregion

        #region 业务逻辑处理
        /// <summary>
        /// 刷新当前的EventTrainMission配置
        /// </summary>
        private void _RefreshMission() => mission = EventTrainMissionVisitor.Get(missionRound.IncludeTrainId[phase]);

        /// <summary>
        /// 刷新_detailID
        /// </summary>
        private void _RefreshDetailID() => _missionDetailID = Game.Manager.userGradeMan.GetTargetConfigDataId(mission.GradeId);


        /// <summary>
        /// 刷新EventTheme
        /// </summary>
        private void _RefreshTheme()
        {
            VisualMain.Setup(mission.MainTheme);
            VisualHelp.Setup(mission.HelpTheme);
            VisualChooseGroup.Setup(mission.ChooseTheme);
            VisualLoading.Setup(mission.LoadingTheme);
            VisualPreview.Setup(mission.PreviewTheme);
            VisualItemInfo.Setup(mission.ItemTheme);
            StartPopup.Setup(mission.StartTheme, this);
            EndPopup.Setup(mission.EndTheme, this, active_: false);
        }

        /// <summary>
        /// 获取所有可选生成器组的id
        /// </summary>
        /// <returns></returns>
        private List<int> _GetCanChooseGroupDetailID()
        {
            var list = new List<int>();
            foreach (var id in EventTrainMissionDetailVisitor.Get(_missionDetailID).IncludeLevelGroups)
            {
                if (!_CheckGroupLevelRequire(id)) { continue; }
                _FillGroupDetailID(list, id);
                list.Sort();
                break;
            }
            return list;
        }

        /// <summary>
        /// 检测当前等级是否满足对应id的TrainLevelGroup配置的要求
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool _CheckGroupLevelRequire(int id)
        {
            var levelGroup = TrainLevelGroupVisitor.Get(id);
            return levelGroup == null ? false : levelGroup.MinLevel <= Game.Manager.mergeLevelMan.level && levelGroup.MaxLevel >= Game.Manager.mergeLevelMan.level;
        }

        /// <summary>
        /// 填充GroupDetailID
        /// </summary>
        /// <param name="ids">容器</param>
        /// <param name="GroupId">LevekGroupID</param>
        private void _FillGroupDetailID(List<int> ids, int GroupId)
        {
            ids.Clear();
            var levelGroup = TrainLevelGroupVisitor.Get(GroupId);
            if (levelGroup.OptionalGroup.Count <= 3) { ids.AddRange(levelGroup.OptionalGroup); }
            else { ids.AddRange(levelGroup.OptionalGroup.OrderBy(x => Guid.NewGuid()).Take(3)); }//打乱后取出前三个
        }

        /// <summary>
        /// 重置挑战关卡
        /// </summary>
        private void _ResetChallenge()
        {
            _challengeIndex = -1;
            _challengeComplete = 0;
        }

        /// <summary>
        /// 开启新一轮挑战
        /// </summary>
        private void _StartNewChallenge()
        {
            _challengeIndex++;
            _challengeComplete = 0;
            _UpdateChallenge();
            _UpdateOrder();
        }

        /// <summary>
        /// 刷新当前挑战关卡的配置信息
        /// </summary>
        private void _UpdateChallenge()
        {
            var challengeID = TrainGroupDetailVisitor.Get(_groupDetailID).IncludeChallenge[_challengeIndex];
            trainChallenge = TrainChallengeVisitor.Get(challengeID);
        }

        /// <summary>
        /// 根据玩家当前选择的生成器组，填充活动棋盘
        /// </summary>
        private void _FillBoardItemByChallenge()
        {
            var item1 = BoardActivityUtility.GetHighestLevelItemIdInCategory(trainChallenge.ConnectSpawner[0], 1);
            var item2 = BoardActivityUtility.GetHighestLevelItemIdInCategory(trainChallenge.ConnectSpawner[1], 1);
            World.activeBoard.SpawnItem(item1, 0, 0, false, false);
            World.activeBoard.SpawnItem(item2, 0, World.activeBoard.size.x - 1, false, false);
        }

        /// <summary>
        /// 刷新订单信息
        /// </summary>
        private void _UpdateOrder()
        {
            for (var i = 0; i < trainChallenge.IncludeTrainMission.Count; i++)
            {
                if ((1 << i & _challengeComplete) > 0) { continue; }
                if (topOrder.orderID == trainChallenge.IncludeTrainMission[i] || bottomOrder.orderID == trainChallenge.IncludeTrainMission[i]) { continue; }
                if (topOrder.orderID == 0) { topOrder.SetData(trainChallenge.IncludeTrainMission[i], 0, (int)Game.Instance.GetTimestampSeconds()); }
                else if (bottomOrder.orderID == 0) { bottomOrder.SetData(trainChallenge.IncludeTrainMission[i], 0, (int)Game.Instance.GetTimestampSeconds()); }
            }
        }

        /// <summary>
        /// 完成火车订单
        /// </summary>
        /// <param name="order"></param>
        private bool _FinishOrder(TrainMissionOrder order)
        {
            if (!order.CheckAllFinish()) { return false; }
            _challengeComplete |= 1 << trainChallenge.IncludeTrainMission.IndexOf(order.orderID);
            foreach (var config in order.rewardConfigs) { _orderRewardWaitCommit.Add(Game.Manager.rewardMan.BeginReward(config.Id, config.Count, ReasonString.train_reward)); }
            if (topOrder.orderID == order.orderID) { topOrder = new TrainMissionOrder(); }
            else { bottomOrder = new TrainMissionOrder(); }
            _UpdateOrder();
            _trainQueue++;
            return true;
        }

        /// <summary>
        /// 完成当前挑战
        /// </summary>
        private void _FinishChallenge()
        {
            if (topOrder.orderID != 0 || bottomOrder.orderID != 0) { return; }
            if (_challengeIndex + 1 >= TrainGroupDetailVisitor.Get(_groupDetailID).IncludeChallenge.Count) { _waitRecycle = true; }
            else { _waitEnterNextChallenge = true; }
            DataTracker.event_train_challenge_complete.Track(this, groupDetailID, trainChallenge.Id, _challengeIndex + 1, _challengeIndex + 1 >= TrainGroupDetailVisitor.Get(groupDetailID).IncludeChallenge.Count, phase + 1);
            _challengeQueue++;
        }

        private void _BeginMilestoneReward()
        {
            var list = new List<TrainMilestone>();
            GetTrainMilestones(list);
            foreach (var item in list)
            {
                if (GetCurMilestoneProgress() == item.MissionNum)
                {
                    _BeginMilestoneReward(item);
                    _milestoneQueue++;
                    DataTracker.event_train_complete_milestone.Track(this, groupDetailID, trainChallenge.Id, item.Id, _milestoneQueue,
                        ZString.Format("{0}/{1}", _trainMilestoneRewardWaitCommit.rewardId, _trainMilestoneRewardWaitCommit.rewardCount), item.MissionNum == 6, phase + 1);
                }
            }
        }

        private void _BeginMilestoneReward(TrainMilestone trainMilestone)
        {
            var reward = trainMilestone.Reward.ConvertToRewardConfig();
            _trainMilestoneRewardWaitCommit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.train_milestone_reward);
        }

        private void _EnterNextRound()
        {
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            SetupFresh();
            _waitRecycle = false;
            DataTracker.event_train_restart.Track(this, phase + 1);
        }

        public ItemIndType CheckIndicator(int itemId, out string asset)
        {
            asset = null;
            if (!UIManager.Instance.IsOpen(VisualMain.res.ActiveR))
            {
                return ItemIndType.None;
            }

            if (topOrder.ItemInfos.Any(e => e.itemID == itemId && (topOrder.complete & 1 << topOrder.ItemInfos.IndexOf(e)) == 0)
            || bottomOrder.ItemInfos.Any(e => e.itemID == itemId && (bottomOrder.complete & 1 << bottomOrder.ItemInfos.IndexOf(e)) == 0)) { return ItemIndType.TrainMission; }
            else { return ItemIndType.None; }
        }

        #endregion

    }

    public class TrainMissionEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => entry;
        private readonly ListActivity.Entry entry;
        private readonly TrainMissionActivity activity;

        public TrainMissionEntry(ListActivity.Entry _entry, TrainMissionActivity _activity)
        {
            (entry, this.activity) = (_entry, _activity);
            entry.flag.SetImage(activity.BadgeAsset); // event_common#ci_s_green_right.png
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }


        public override string TextCD(long diff_)
        {
            entry.flag.gameObject.SetActive(activity.CheckCanShowRP());
            return UIUtility.CountDownFormat(diff_);
        }
    }
}