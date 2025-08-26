/*
 * @Author: qun.chao
 * @Date: 2025-04-03 14:28:15
 */
using System;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using FAT.Merge;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using Cysharp.Text;

namespace FAT
{
    public class ActivityFishing : ActivityLike, IBoardEntry, IBoardArchive, IExternalOutput
    {
        public struct FishCaughtInfo
        {
            public int fishId;     // 鱼id
            public int curWeight;  // 本次重量
            public int maxWeight;  // 历史最重(不含本次)
            public int preCount;   // 之前捕获次数
            public int nowCount;   // 当前捕获次数
            public int preStar;    // 之前星星数
            public int nowStar;    // 当前星星数
        }

        public EventFish Conf => _conf;
        public EventFishDetail ConfDetail => _confDetail;
        public EventFishMilestone ConfMilestoneCur => _confMilestoneCur;
        public FishRarity RarityInfo => _confRarity;
        public MergeWorld World => _world;
        public IList<FishInfo> FishInfoList => _fishInfoList;
        public int TokenId => Conf.TokenId;
        public int MilestoneIdx => _curMilestoneIdx;

        public int CurToken => _curToken;
        public int MaxToken => ConfMilestoneCur.Star;
        public bool IsFinalMilestone => _curMilestoneIdx >= ConfDetail.Milestones.Count - 1;

        // 额外捕获次数
        private int ExtraCatchCount => Conf.FishRepeatNum;

        #region UI资源
        public override ActivityVisual Visual => VisualBoard.visual;
        public ActivityVisual EndTheme = new();
        public UIResAlt EndResAlt = new UIResAlt(UIConfig.UIActivityFishEnd); //活动结束theme
        public VisualRes VisualBoard { get; } = new(UIConfig.UIActivityFishMain); //棋盘UI
        public VisualRes VisualHelp { get; } = new(UIConfig.UIActivityFishHelp);
        public VisualRes VisualCollect { get; } = new(UIConfig.UIActivityFishCollect); //集齐鱼
        public VisualRes VisualGet { get; } = new(UIConfig.UIActivityFishGet); //获得鱼
        public VisualRes VisualTip { get; } = new(UIConfig.UIActivityFishTips); //鱼图鉴Tip
        public VisualRes VisualLoading { get; } = new(UIConfig.UIActivityFishLoading);
        public VisualRes VisualEnd { get; } = new(UIConfig.UIActivityFishEnd);
        public VisualRes VisualConvert { get; } = new(UIConfig.UIActivityFishConvert);

        // 弹脸
        public VisualPopup StartPopup { get; } = new(UIConfig.UIActivityFishBegin); //活动开启theme
        public PopupActivity EndPopup = new();
        public PopupActivity RewardPopup = new();
        #endregion

        #region 存档字段
        // 当前里程碑索引
        private int _curMilestoneIdx;
        // 当前分数
        private int _curToken;
        // 当前数值模版id
        private int _curTemplateId = -1;
        // 已经捕获的鱼列表 (id, 数量, 历史最大重量)
        private List<(int fishId, int count, int maxWeight)> _fishCaughtList = new();

        // 已捕获的鱼的信息在存档里是一个列表
        // 存储时使用id+mod 避免和常规存档字段冲突
        private int fish_id_mod = 10000;
        #endregion

        #region 棋盘
        private MergeWorld _world;
        private MergeWorldTracer _tracer;
        private FishingBoardItemSpawnBonusHandler _spawnBonusHandler;
        #endregion

        // 配置
        private EventFish _conf;
        // 当前里程碑
        private EventFishMilestone _confMilestoneCur;
        // 当前数值模版
        private EventFishDetail _confDetail;
        // 稀有度表
        private FishRarity _confRarity;
        // 所有鱼信息
        private readonly List<FishInfo> _fishInfoList = new();
        // 里程碑奖励
        private readonly List<RewardCommitData> _rewardsMilestone = new();
        // 转换奖励
        private readonly List<RewardCommitData> _rewardsConvert = new();
        // 当前里程碑的棋子掉落信息
        private readonly List<(int itemId, int weight)> _itemOutputs = new();
        // 当前里程碑的鱼掉落表 (id, 权重, 最小重量, 最大重量)
        private readonly List<(int fishId, int weight, int minWeight, int maxWeight)> _fishOutputs = new();
        // 已解锁的鱼
        private readonly HashSet<int> _fishUnlockSet = new();

        public ActivityFishing() { }


        public ActivityFishing(ActivityLite lite_)
        {
            Lite = lite_;
        }

        #region UI
        public override void Open()
        {
            ActivityTransit.Enter(this, this.VisualLoading, this.VisualBoard.res);
        }
        public void Close()
        {
            ActivityTransit.Exit(this, this.VisualLoading.res.ActiveR);
        }
        #endregion

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            _curMilestoneIdx = ReadInt(i++, any);
            _curToken = ReadInt(i++, any);
            _curTemplateId = ReadInt(i++, any);
            InitConf();
            InitTheme();
            for (; i < any.Count; i++)
            {
                var item = any[i];
                if (item.Id > fish_id_mod)
                {
                    // 是已捕获的鱼记录
                    var fishId = item.Id % fish_id_mod;
                    var count_and_weight = item.Value;
                    // 高16位是数量 低16位是最大重量
                    var count = count_and_weight >> 16;
                    var maxWeight = count_and_weight & 0xFFFF;
                    AddNewFish(fishId, count, maxWeight);
                }
            }
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            any.Add(ToRecord(i++, _curMilestoneIdx));
            any.Add(ToRecord(i++, _curToken));
            any.Add(ToRecord(i++, _curTemplateId));
            foreach (var fish in _fishCaughtList)
            {
                var count_and_weight = (fish.count << 16) | fish.maxWeight;
                any.Add(ToRecord(fish.fishId + fish_id_mod, count_and_weight));
            }
        }

        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            InitConf();
            InitTheme();
            InitWorld(null);

            StartPopup.Popup();
        }

        public override void WhenReset()
        {
            Cleanup();
        }

        public override void WhenEnd()
        {
            EndConvert();
            Cleanup();
        }

        private void InitConf()
        {
            _conf = GetEventFish(Lite.Param);
            if (_curTemplateId <= 0)
            {
                _curTemplateId = Game.Manager.userGradeMan.GetTargetConfigDataId(_conf.GradeId);
            }
            _confDetail = GetEventFishDetail(_curTemplateId);
            InitFish();
            RefreshMilestone();
        }

        private void InitTheme()
        {
            var cfg = _conf;

            VisualBoard.Setup(cfg.BoardTheme);
            VisualCollect.Setup(cfg.FishCollectedTheme);
            VisualGet.Setup(cfg.NewFishTheme);
            VisualTip.Setup(cfg.FishTipTheme);
            VisualLoading.Setup(cfg.LoadingTheme);
            VisualEnd.Setup(cfg.EndTheme);
            StartPopup.Setup(cfg.StartTheme, this);
            if (EndTheme.Setup(cfg.EndTheme, EndResAlt))
                EndPopup.Setup(this, EndTheme, EndResAlt, false, false);
            VisualConvert.Setup(cfg.ExpirePopup);
            RewardPopup.Setup(this, VisualConvert.visual, VisualConvert.res, false, false);
        }

        private void InitFish()
        {
            _fishInfoList.Clear();
            var allFish = GetFishInfoMap();
            foreach (var fish in allFish) _fishInfoList.Add(fish.Value);
            _fishInfoList.Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        private void InitWorld(fat.gamekitdata.Merge data)
        {
            var world_ = new MergeWorld();
            var tracer_ = new MergeWorldTracer(null, null);
            tracer_.Bind(world_);
            world_.BindTracer(tracer_);
            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry()
            {
                world = world_,
                type = MergeWorldEntry.EntryType.FishingBoard,
            });
            var isFirstInit = data == null;
            Game.Manager.mergeBoardMan.InitializeBoard(world_, _confDetail.BoardId, isFirstInit);
            if (!isFirstInit)
            {
                // 读档
                world_.Deserialize(data, null);
            }
            else
            {
                // 首次创建 需要填充初始奖励道具
                var rewardMan = Game.Manager.rewardMan;
                foreach (var item in ConfDetail.FreeItem)
                {
                    rewardMan.PushContext(new RewardContext() { targetWorld = World });
                    rewardMan.CommitReward(rewardMan.BeginReward(item, 1, ReasonString.fish_start));
                    rewardMan.PopContext();
                }
            }
            // 注册全局handler
            var handler = new FishingBoardItemSpawnBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(handler);
            // 注册当前world的handler
            world_.RegisterActivityHandler(this);

            _world = world_;
            _tracer = tracer_;
            _spawnBonusHandler = handler;
        }

        // 填充里程碑奖励
        public int FillMilestoneRewards(List<RewardCommitData> container)
        {
            var count = _rewardsMilestone.Count;
            container.AddRange(_rewardsMilestone);
            _rewardsMilestone.Clear();
            return count;
        }

        // 填充转换奖励
        public int FillConvertRewards(List<RewardCommitData> container)
        {
            var count = _rewardsConvert.Count;
            container.AddRange(_rewardsConvert);
            _rewardsConvert.Clear();
            return count;
        }

        private void Cleanup()
        {
            // 取消注册
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(_spawnBonusHandler);
            World?.UnregisterActivityHandler(this);
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);

            _world = null;
            _tracer = null;
        }

        private void EndConvert()
        {
            // 存档不能再变更, 结算UI里需要用到存档数据
            Game.Manager.screenPopup.TryQueue(EndPopup, PopupType.Login);
            var reward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);
            if (CollectAllBoardReward(rewardList) && rewardList.Count > 0)
            {
                Game.Manager.screenPopup.TryQueue(RewardPopup, PopupType.Login, reward);
            }
            else
            {
                reward.Free();
            }
        }

        private void RefreshMilestone()
        {
            var milestones = _confDetail.Milestones;
            var milestoneId = _curMilestoneIdx < milestones.Count ? milestones[_curMilestoneIdx] : milestones[^1];
            _confMilestoneCur = GetEventFishMilestone(milestoneId);
            _spawnBonusHandler?.SetDirty();
            // 构建棋子掉落表
            _itemOutputs.Clear();
            foreach (var item in _confMilestoneCur.ChestOutputs)
            {
                var (id, weight, _) = item.ConvertToInt3();
                _itemOutputs.Add((id, weight));
            }
            // 构建鱼掉落表
            _fishOutputs.Clear();
            foreach (var fishId in _confMilestoneCur.RandomFish)
            {
                var fish = _fishInfoList.Find(f => f.Id == fishId);
                _fishOutputs.Add((fishId, fish.RandomWeight, fish.Weight[0], fish.Weight[1]));
            }
        }

        public bool CheckIsShowRedPoint(out int rpNum)
        {
            rpNum = 0;
            if (!Active || World == null) return false;
            rpNum = World.rewardCount;
            return true;
        }

        public void AddToken(int id, int count)
        {
            if (id != TokenId)
                return;
            if (CurToken >= MaxToken && IsFinalMilestone)
            {
                _Info($"skip add, cur: {CurToken}, max: {MaxToken}");
                return;
            }
            var rewardChange = false;
            var tokenAfter = CurToken + count;
            while (tokenAfter >= MaxToken)
            {
                rewardChange = true;
                AddMilestoneReward();
                if (IsFinalMilestone)
                {
                    tokenAfter = MaxToken;
                    break;
                }
                else
                {
                    _curMilestoneIdx++;
                    RefreshMilestone();
                }
            }
            _curToken = tokenAfter;
            DataTracker.token_change.Track(id, count, tokenAfter, ReasonString.fish_milestone);
            MessageCenter.Get<MSG.FISHING_MILESTONE_TOKEN_CHANGE>().Dispatch();
            if (rewardChange)
                MessageCenter.Get<MSG.FISHING_MILESTONE_REWARD_CHANGE>().Dispatch();
        }

        private void AddMilestoneReward()
        {
            var rewardMan = Game.Manager.rewardMan;
            // 奖励需要发到主棋盘
            rewardMan.PushContext(new RewardContext() { targetWorld = Game.Manager.mainMergeMan.world });
            var milestone = _confMilestoneCur;
            foreach (var item in milestone.Reward)
            {
                var reward = item.ConvertToRewardConfig();
                _rewardsMilestone.Add(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.fish_milestone));
            }
            rewardMan.PopContext();
            // 打点
            DataTracker.event_fish_milestone.Track(this, MilestoneIdx, ConfDetail.Milestones.Count, ConfDetail.Diff);
        }

        #region IBoardEntry
        string IBoardEntry.BoardEntryAsset()
        {
            VisualBoard.visual.AssetMap.TryGetValue("boardEntry", out var key);
            return key;
        }
        #endregion

        #region IBoardArchive
        FeatureEntry IBoardArchive.Feature => FeatureEntry.FeatureFish;

        void IBoardArchive.SetBoardData(fat.gamekitdata.Merge data)
        {
            if (World != null)
            {
                // 棋盘由初始化流程创建
                _Error("world not null");
                return;
            }

            InitWorld(data);
        }

        void IBoardArchive.FillBoardData(fat.gamekitdata.Merge data)
        {
            World?.Serialize(data);
        }
        #endregion

        #region 棋子产出逻辑
        bool IExternalOutput.CanUseItem(Item source)
        {
            return source.tid == Conf.FishRod;
        }

        bool IExternalOutput.TrySpawnItem(Item source, out int outputId, out ItemSpawnContext context)
        {
            outputId = -1;
            context = null;
            var com = source.GetItemComponent<ItemActiveSourceComponent>();
            if (com.WillDead)
            {
                // 此时应该生成鱼
                GenerateFish();
            }
            else
            {
                context = ItemSpawnContext.CreateWithSource(null, ItemSpawnContext.SpawnType.Fishing);
                // 生成棋子
                var item = _itemOutputs.RandomChooseByWeight(e => e.weight);
                outputId = item.itemId;
                // 注册出生位置
                var pos = UIFlyFactory.ResolveFlyTarget(FlyType.FishSpawnPoint);
                BoardUtility.RegisterSpawnRequest(outputId, pos);
                MessageCenter.Get<MSG.FISHING_FISH_BOARD_SPAWN_ITEM>().Dispatch();
            }
            return true;
        }

        // 鱼是否解锁
        public bool IsFishUnlocked(int fishId)
        {
            return _fishUnlockSet.Contains(fishId);
        }

        // 获取鱼捕获次数
        public int GetFishCaughtCount(int fishId)
        {
            var caughtCount = 0;
            var caughtIdx = _fishCaughtList.FindIndex(e => e.fishId == fishId);
            if (caughtIdx >= 0)
            {
                var (_, _count, _) = _fishCaughtList[caughtIdx];
                caughtCount += _count;
            }
            return caughtCount;
        }

        // 获取鱼最大星级要求捕获次数
        public int GetFishMaxStarRequireCount(int fishId)
        {
            return CalcFishStarRequireCount(fishId);
        }

        // 计算星级要求的捕获次数
        public int CalcFishStarRequireCount(int fishId, int targetStar = int.MaxValue)
        {
            var count = 0;
            var fishInfo = FishInfoList.FindEx(f => f.Id == fishId);
            foreach (var star in fishInfo.Star)
            {
                var (starNum, fishNum, _) = star.ConvertToInt3();
                if (starNum > targetStar)
                    break;
                count += fishNum;
            }
            return count;
        }

        public int GetFishMaxStar(int fishId)
        {
            return CalcFishStarByCount(fishId, int.MaxValue);
        }

        // 通过捕获数量计算可获得的星星数
        public int CalcFishStarByCount(int fishId, int count)
        {
            var maxStar = 0;
            var fishInfo = FishInfoList.FindEx(f => f.Id == fishId);
            foreach (var star in fishInfo.Star)
            {
                var (starNum, fishNum, _) = star.ConvertToInt3();
                if (count >= fishNum)
                {
                    maxStar = starNum;
                    count -= fishNum;
                }
            }
            return maxStar;
        }

        // 冗余的渔获转换为奖励
        private void ConvertFish(int fishId)
        {
            var fishInfo = FishInfoList.FindEx(f => f.Id == fishId);
            foreach (var item in fishInfo.RepeatConvert)
            {
                var reward = item.ConvertToRewardConfig();
                _rewardsConvert.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.fish_convert));
            }
            MessageCenter.Get<MSG.FISHING_FISH_CONVERT>().Dispatch();
        }

        private void UpdateFishCaughtInfo(int fishId, int count, int maxWeight)
        {
            var idx = _fishCaughtList.FindIndex(e => e.fishId == fishId);
            if (idx >= 0)
            {
                _fishCaughtList[idx] = (fishId, count, maxWeight);
            }
            else
            {
                AddNewFish(fishId, count, maxWeight);
            }
        }

        private void AddNewFish(int fishId, int count, int maxWeight)
        {
            _fishUnlockSet.Add(fishId);
            _fishCaughtList.Add((fishId, count, maxWeight));
        }

        private void GenerateFish()
        {
            var allStar = CurToken >= MaxToken && IsFinalMilestone;
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<(int id, int weight, int min, int max)>>(out var outputList);
            foreach (var dropInfo in _fishOutputs)
            {
                if (allStar)
                {
                    // 已全收集 掉落池不做调整
                    outputList.Add((dropInfo.fishId, dropInfo.weight, dropInfo.minWeight, dropInfo.maxWeight));
                    continue;
                }
                // 未全收集 过滤掉捕获次数达到 maxcount + extra 的鱼
                var caughtCount = GetFishCaughtCount(dropInfo.fishId);
                var maxCount = GetFishMaxStarRequireCount(dropInfo.fishId);
                if (caughtCount >= maxCount + ExtraCatchCount)
                {
                    continue;
                }
                outputList.Add((dropInfo.fishId, dropInfo.weight, dropInfo.minWeight, dropInfo.maxWeight));
            }

            if (outputList.Count == 0)
            {
                _Error("no fish output");
                return;
            }

            // 掉落
            var fish = outputList.RandomChooseByWeight(e => e.weight);
            // 本次重量
            var curWeight = UnityEngine.Random.Range(fish.min, fish.max);
            // 历史最重 
            var maxWeight = 0;
            // 捕获次数
            var count = 0;

            var caughtList = _fishCaughtList;
            var idx = caughtList.FindIndex(e => e.fishId == fish.id);
            if (idx >= 0)
            {
                var (_, _count, _maxWeight) = caughtList[idx];
                count = _count;
                maxWeight = _maxWeight;
            }
            var shouldConvert = false;
            var preStar = CalcFishStarByCount(fish.id, count);
            ++count;
            var curStar = CalcFishStarByCount(fish.id, count);
            if (curStar > preStar)
            {
                // 奖励星星
                AddToken(TokenId, curStar - preStar);
                // 打点
                DataTracker.event_fish_star.Track(this, fish.id, curStar);
            }
            else
            {
                var maxStar = GetFishMaxStar(fish.id);
                if (preStar == maxStar)
                {
                    // 之前已达到最大星星 此时鱼不贡献星星 需要转换为奖励
                    shouldConvert = true;
                    ConvertFish(fish.id);
                }
            }
            // 更新捕获信息
            UpdateFishCaughtInfo(fish.id, count, Math.Max(curWeight, maxWeight));
            var fishCaughtInfo = new FishCaughtInfo()
            {
                fishId = fish.id,
                curWeight = curWeight,
                maxWeight = maxWeight,
                preCount = count - 1,
                nowCount = count,
                preStar = preStar,
                nowStar = curStar,
            };
            // 捕获通知
            MessageCenter.Get<MSG.FISHING_FISH_CAUGHT>().Dispatch(fishCaughtInfo);
            // 打点
            var totalCaughtCount = 0;
            foreach (var item in caughtList) totalCaughtCount += item.count;
            DataTracker.event_fish_get_fish.Track(this, fish.id, count, totalCaughtCount, shouldConvert);
        }
        #endregion

        #region 结束时回收奖励
        private bool CollectAllBoardReward(List<RewardCommitData> rewards)
        {
            if (World == null || rewards == null)
                return false;
            using var _1 = PoolMapping.PoolMappingAccess.Borrow<Dictionary<int, int>>(out var itemIdMap);
            using var _2 = PoolMapping.PoolMappingAccess.Borrow<Dictionary<int, int>>(out var rewardMap);
            //遍历整个棋盘 找出所有可以回收的棋子 并整合
            World.WalkAllItem((item) =>
            {
                ItemUtility.CollectRewardItem(item, itemIdMap, rewardMap);
            }, MergeWorld.WalkItemMask.NoInventory);
            //发奖 相当于帮玩家把棋盘上没有使用的棋子直接用了 所以from用use_item
            foreach (var reward in rewardMap)
            {
                rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Key, reward.Value, ReasonString.use_item));
            }
            //打点
            DataTracker.event_fish_end_collect.Track(this, ItemUtility.ConvertItemDictToString_Id_Num_Level(itemIdMap));
            return true;
        }
        #endregion

        #region log
        private void _Info(string msg)
        {
            DebugEx.Info($"[{nameof(ActivityFishing)}] {msg}");
        }
        private void _Error(string msg)
        {
            DebugEx.Error($"[{nameof(ActivityFishing)}] {msg}");
        }
        #endregion
    }

    public class FishingEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => e;
        private readonly ListActivity.Entry e;
        private readonly ActivityFishing p;

        public FishingEntry(ListActivity.Entry e_, ActivityFishing p_)
        {
            (e, p) = (e_, p_);
            var showRedPoint = p.CheckIsShowRedPoint(out var rpNum);
            e_.dot.SetActive(showRedPoint && rpNum > 0);
            e_.dotCount.gameObject.SetActive(showRedPoint && rpNum > 0);
            e_.dotCount.SetRedPoint(rpNum);
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