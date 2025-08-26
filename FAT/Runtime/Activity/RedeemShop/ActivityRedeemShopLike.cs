/*
 * @Author: yanfuxing
 * @Date: 2025-05-08 10:59:00
 */
using System.Collections.Generic;
using Config;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using FAT.MSG;
using UnityEngine;
using static EL.PoolMapping;
using static FAT.RecordStateHelper;
namespace FAT
{
    public class ActivityRedeemShopLike : ActivityLike, IBoardEntry
    {
        // 兑换商店主界面
        public VisualRes VisualUIRedeemShopMain { get; } = new(UIConfig.UIRedeemShopMain);
        // 兑换商店宣传界面
        public VisualPopup VisualUINoticeRedeemShop { get; } = new(UIConfig.UINoticeRedeemShop);
        // 兑换商店帮助界面
        public VisualRes VisualUIRedeemShopHelp { get; } = new(UIConfig.UIRedeemShopHelp);
        // 兑换商店阶段奖励界面
        public VisualPopup VisualUIRedeemShopStageReward { get; } = new(UIConfig.UIRedeemShopStageReward);
        // 兑换商店结算界面
        public VisualPopup VisualUIRedeemShopSettlement { get; } = new(UIConfig.UIRedeemShopSettlement);

        public override bool EntryVisible => !IsCompleteEndActivity();
        public bool BoardEntryVisible => !IsCompleteEndActivity();



        #region 存档
        //记录当前里程碑积分
        public int _recordMilestoneScore = 0;
        //当前里程碑轮次数
        private int _curMilistoneRoundNum = 0;
        //当前里程碑轮次索引(每个轮次有多个阶段)
        private int _curMilistoneRoundNumIndex = 0;
        //当前里程碑阶段索引(当前小阶段索引)
        private int _curMilistoneStageIndex = 0;
        //当前兑换商店轮次数
        private int _curRedeemShopRoundNum = 0;
        //当前兑换商店轮次索引
        private int _curRedeemShopRoundNumIndex = 0;
        //grp id 用户分层
        private int grpIndexMappingId;
        //当前兑换币数量
        public int CurRedeemCoinNum;
        //奖池Id
        private int _rewardPool1Id;
        //当前可兑换的奖励索引
        private int _curPool1CanRedeemIndex;
        //当前可兑换剩余次数
        public int _curPool1CanRedeemLeftNum;
        public int _pool1PlayAnimStateId;
        private int _rewardPool2Id;
        //当前可兑换的奖励索引
        private int _curPool2CanRedeemIndex;
        //当前可兑换剩余次数
        private int _curPool2CanRedeemLeftNum;
        public int _pool2PlayAnimStateId;
        private int _rewardPool3Id;
        //当前可兑换的奖励索引
        private int _curPool3CanRedeemIndex;
        //当前可兑换剩余次数
        private int _curPool3CanRedeemLeftNum;
        public int _pool3PlayAnimStateId;
        public bool IsLookRedPoint;
        //本轮第几个奖励，自增（每轮重置）
        public int Reward_num; 
        #endregion

        //当前阶段里程碑总阶段数(使用轮次总数)
        public int CurMileStoneStateAllNum
        {
            get
            {
                if (_eventRedeemDetailConfig == null || _eventRedeemDetailConfig.RedeemGrp == null || _curMilistoneRoundNum >= _eventRedeemDetailConfig.MilestoneGrp.Count)
                {
                    return 0;
                }
                return _eventRedeemDetailConfig.MilestoneGrp[_curMilistoneRoundNum].ConvertToRoundsArrayItem().RoundsArray.Length;
            }
        }
        //当前里程碑阶段索引（当前轮次阶段）
        public int CurMileStoneStateNum => _curShowMilestoneConfig != null ? _curShowMilestoneConfig.Sort : 0;

        // 活动配置
        private EventRedeem _eventRedeemConfig;
        public EventRedeem EventRedeemConfig => _eventRedeemConfig;
        //活动详情配置
        private EventRedeemDetail _eventRedeemDetailConfig;
        public EventRedeemDetail EventRedeemDetailConfig => _eventRedeemDetailConfig;
        //活动数据是否有效
        public override bool Valid => _eventRedeemConfig != null;
        //是否解锁
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureRedeem);
        public override ActivityVisual Visual => VisualUINoticeRedeemShop.visual;
        //里程碑节点List
        public List<MliestoneNodeItem> MilestoneNodeList = new();

        private Dictionary<int, List<RedeemShopNodeItem>> _redeemShopStageDic = new();
        public Dictionary<int, List<RedeemShopNodeItem>> RedeemShopStageDic => _redeemShopStageDic;
        private List<RedeemShopNodeItem> _redeemRewardPool1List = new();
        private List<RedeemShopNodeItem> _redeemRewardPool2List = new();
        private List<RedeemShopNodeItem> _redeemRewardPool3List = new();
        private List<RewardConfig> _redeemRewardList = new List<RewardConfig>();
        private List<RewardCommitData> _StateRewardCommitRewardList = new();  //阶段里程碑奖励提交List
        private EventRedeemMilestone _curShowMilestoneConfig;
        private RedeemShopSpawnBonusHandler _spawnBonusHandler;
        public List<MliestoneNodeItem> ClonMilestoneNodeList = new();
        public ActivityRedeemShopLike() { }

        public ActivityRedeemShopLike(ActivityLite lite)
        {
            Lite = lite;
            _eventRedeemConfig = Game.Manager.configMan.GetEventRedeemConfig(lite.Param);
            SetupBonusHandler();
            InitTheme();
        }

        public override void Open()
        {
            Open(VisualUIRedeemShopMain);
        }

        public void OpenHelp()
        {
            VisualUIRedeemShopHelp.Open(this);
        }

        public override void SetupFresh()
        {
            _recordMilestoneScore = 0;
            CurRedeemCoinNum = 0;
            //通过分层表获得逻辑id 此活动id含义为兑换模板id
            var redeemShopDetailId = Game.Manager.userGradeMan.GetTargetConfigDataId(_eventRedeemConfig.Detail);
            if (redeemShopDetailId == 0)
            {
                DebugEx.Info("gradeIndexMappingId not found _eventRedeemConfig.Detail: " + _eventRedeemConfig.Detail);
                return;
            }
            grpIndexMappingId = redeemShopDetailId;

            isFinishMax = false;


            SetMilestoneData(0, 0);
            SetRedeemShopData();
            RefreshDataRecordByRewardPool();
            VisualUINoticeRedeemShop.popup.OpenPopup();
        }

        public override void LoadSetup(ActivityInstance data)
        {
            var any = data.AnyState;
            _recordMilestoneScore = ReadInt(1, any);
            _curMilistoneRoundNum = ReadInt(2, any);
            _curMilistoneRoundNumIndex = ReadInt(3, any);
            _curMilistoneStageIndex = ReadInt(4, any);
            grpIndexMappingId = ReadInt(5, any);

            _curRedeemShopRoundNum = ReadInt(6, any);
            _curRedeemShopRoundNumIndex = ReadInt(7, any);
            CurRedeemCoinNum = ReadInt(8, any);

            _rewardPool1Id = ReadInt(9, any);
            _curPool1CanRedeemIndex = ReadInt(10, any);
            _curPool1CanRedeemLeftNum = ReadInt(11, any);

            _rewardPool2Id = ReadInt(12, any);
            _curPool2CanRedeemIndex = ReadInt(13, any);
            _curPool2CanRedeemLeftNum = ReadInt(14, any);

            _rewardPool3Id = ReadInt(15, any);
            _curPool3CanRedeemIndex = ReadInt(16, any);
            _curPool3CanRedeemLeftNum = ReadInt(17, any);

            _pool1PlayAnimStateId = ReadInt(18, any);
            _pool2PlayAnimStateId = ReadInt(19, any);
            _pool3PlayAnimStateId = ReadInt(20, any);

            IsLookRedPoint = ReadBool(21, any);
            Reward_num = ReadInt(22, any);

            SetMilestoneData(_curMilistoneRoundNum, _curMilistoneRoundNumIndex);  //当前里程碑记录轮次和当前轮次索引Id

            SetRedeemShopData();
        }

        public override void SaveSetup(ActivityInstance data)
        {
            var any = data.AnyState;
            any.Add(ToRecord(1, _recordMilestoneScore));
            any.Add(ToRecord(2, _curMilistoneRoundNum));
            any.Add(ToRecord(3, _curMilistoneRoundNumIndex));
            any.Add(ToRecord(4, _curMilistoneStageIndex));
            any.Add(ToRecord(5, grpIndexMappingId));

            any.Add(ToRecord(6, _curRedeemShopRoundNum));
            any.Add(ToRecord(7, _curRedeemShopRoundNumIndex));
            any.Add(ToRecord(8, CurRedeemCoinNum));

            any.Add(ToRecord(9, _rewardPool1Id));
            any.Add(ToRecord(10, _curPool1CanRedeemIndex));
            any.Add(ToRecord(11, _curPool1CanRedeemLeftNum));

            any.Add(ToRecord(12, _rewardPool2Id));
            any.Add(ToRecord(13, _curPool2CanRedeemIndex));
            any.Add(ToRecord(14, _curPool2CanRedeemLeftNum));

            any.Add(ToRecord(15, _rewardPool3Id));
            any.Add(ToRecord(16, _curPool3CanRedeemIndex));
            any.Add(ToRecord(17, _curPool3CanRedeemLeftNum));

            any.Add(ToRecord(18, _pool1PlayAnimStateId));
            any.Add(ToRecord(19, _pool2PlayAnimStateId));
            any.Add(ToRecord(20, _pool3PlayAnimStateId));

            any.Add(ToRecord(21, IsLookRedPoint));
            any.Add(ToRecord(22, Reward_num));
        }



        public override void WhenActive(bool new_)
        {
            base.WhenActive(new_);
        }

        public override void WhenEnd()
        {
            base.WhenEnd();
            if (!Valid) return;
            CheckSettlement();
        }
        public override void SetupClear()
        {
            _StateRewardCommitRewardList.Clear();
        }

        #region 初始化主题

        private void InitTheme()
        {
            VisualUIRedeemShopMain.Setup(_eventRedeemConfig.EventMain);
            VisualUIRedeemShopStageReward.Setup(_eventRedeemConfig.EventStage, this, active_: false);
            VisualUIRedeemShopHelp.Setup(_eventRedeemConfig.EventHelp);
            VisualUINoticeRedeemShop.Setup(_eventRedeemConfig.EventTheme, this, active_: false);
            VisualUIRedeemShopSettlement.Setup(_eventRedeemConfig.EventSettle, this, active_: false);
        }

        public string BoardEntryAsset()
        {
            VisualUIRedeemShopMain.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        #endregion

        #region 里程碑相关

        #region 设置里程碑数据
        public void SetMilestoneData(int milistoneRoundNum, int milistoneRoundNumIndex)
        {
            //根据轮次获取当前里程碑
            _curMilistoneRoundNum = milistoneRoundNum;
            _curMilistoneRoundNumIndex = milistoneRoundNumIndex; //当前里程碑轮次索引

            _eventRedeemDetailConfig = Game.Manager.configMan.GetEventRedeemDetailConfig(grpIndexMappingId);
            var redeemShopMilestonGrp = _eventRedeemDetailConfig.MilestoneGrp;

            if (_curMilistoneRoundNum >= _eventRedeemDetailConfig.MilestoneGrp.Count)
            {
                //超过配置所有大轮次个数
                return;
            }
            //根据当前里程碑轮次获取当前里程碑组，也就是第几轮数据（1:2:3:4）
            var milestoneGrpArray = redeemShopMilestonGrp[milistoneRoundNum];
            if (milestoneGrpArray.Length <= 0)
            {
                DebugEx.Info("not found milestoneGrpArray Data: " + milistoneRoundNum);
                return;
            }
            //根据记录里程碑 轮次索引 获取当前里程碑阶段Id
            var groupArray = milestoneGrpArray.ConvertToRoundsArrayItem();
            if (milistoneRoundNumIndex < 0 || milistoneRoundNumIndex >= groupArray.RoundsArray.Length)
            {
                DebugEx.Info($"Index out of range: milistoneRoundNumIndex = {milistoneRoundNumIndex}, Length = {groupArray.RoundsArray.Length}");
                return;
            }
            var milestoneStageId = groupArray.RoundsArray[milistoneRoundNumIndex].ToString();
            int.TryParse(milestoneStageId, out var milestoneStageIdInt);
            var milestoneConfig = Game.Manager.configMan.GetEventRedeemMilestone(milestoneStageIdInt);
            if (milestoneConfig == null)
            {
                DebugEx.Info("not found milestoneStageConfig Data: " + milestoneStageId);
                return;
            }
            _curShowMilestoneConfig = milestoneConfig;
            MilestoneNodeList.Clear();
            //每个阶段策划又进行了细分，分为n个小阶段
            var milestoneScoreArray = milestoneConfig.MilestoneScore;
            var milestoneTokenRewardArray = milestoneConfig.TokenReward;
            int stageNum = milestoneConfig.Sort;
            for (int i = 0; i < milestoneScoreArray.Count; i++)
            {
                //当前阶段分数
                var milestoneScore = milestoneScoreArray[i];
                MilestoneNodeList.Add(new MliestoneNodeItem()
                {
                    milestoneId = milestoneStageIdInt,
                    milestoneScore = milestoneScore,
                    milestoneRoundNum = milestoneConfig.Sort,
                    milestoneRewardId = milestoneTokenRewardArray[i].ConvertToRewardConfig().Id,
                    milestoneRewardCount = milestoneTokenRewardArray[i].ConvertToRewardConfig().Count,
                    IsDonePro = CurMilestoneStateIsDone(_recordMilestoneScore, milestoneScore),
                    IsLast = i == milestoneScoreArray.Count - 1,
                    StageNum = stageNum,
                    isConfigMaxNode = i == milestoneScoreArray.Count - 1 && _curMilistoneRoundNumIndex >= _eventRedeemDetailConfig.MilestoneGrp.Count - 1
                });
            }
        }

        private bool CurMilestoneStateIsDone(int playerScore, int configScore)
        {
            return playerScore >= configScore;
        }

        #endregion

        #region 检查刷新里程碑数据
        public void CheckScore()
        {
            // 如果当前阶段索引超出范围，说明已经完成所有阶段
            if (IsFinishAllMaxConfigRoundMilestone())
            {
                return;
            }

            // 获取当前阶段的目标分数
            var currentStageScore = _curShowMilestoneConfig.MilestoneScore[_curMilistoneStageIndex];

            // 如果当前累积分数达到或超过当前阶段的目标分数
            while (_recordMilestoneScore >= currentStageScore)
            {
                // 发放当前阶段的奖励
                var reward = _curShowMilestoneConfig.TokenReward[_curMilistoneStageIndex].ConvertToRewardConfig();
                var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.redeem_Coin_change);
                _StateRewardCommitRewardList.Add(commit);

                var nodeItem = new MliestoneNodeItem()
                {
                    milestoneScore = currentStageScore,
                    milestoneRewardId = reward.Id,
                    milestoneRewardCount = reward.Count,
                    IsLast = _curMilistoneStageIndex == _curShowMilestoneConfig.MilestoneScore.Count - 1,
                    StageNum = _curShowMilestoneConfig.Sort,
                };

                // 进入下一个阶段
                _curMilistoneStageIndex++;

                if (!ClonMilestoneNodeList.Contains(nodeItem))
                {
                    ClonMilestoneNodeList.Add(nodeItem);
                    TrackGetMelistoneReward();
                }


                // 检查是否完成所有阶段
                if (_curMilistoneStageIndex >= _curShowMilestoneConfig.MilestoneScore.Count)
                {
                    var isFinishAllRound = IsFinishAllMaxConfigRoundMilestone();

                    // 完成一个小轮次
                    var curbigRoundCount = _eventRedeemDetailConfig.MilestoneGrp[_curMilistoneRoundNum].ConvertToRoundsArrayItem().RoundsArray.Length;
                    if (_curMilistoneRoundNumIndex >= curbigRoundCount - 1)
                    {
                        if (!isFinishAllRound)
                        {
                            // 当前轮次已完成，重置相关数据
                            _recordMilestoneScore = 0; // 重置记录的里程碑分数
                            _curMilistoneRoundNumIndex = 0;
                            _curMilistoneRoundNum++;
                            _curMilistoneStageIndex = 0;
                            UpdateLookRedPointFlag(false); // 重置红点标志
                        }
                        else
                        {
                            // 达到所有最大轮次，保持当前轮次索引不变
                            //isFinishCurRoundLastNode = true;
                            //isLastRoundCurNode = true;
                            //_curMilistoneStageIndex = _curShowMilestoneConfig.MilestoneScore.Count - 1;
                            //SetMilestoneData(_curMilistoneRoundNum, _curMilistoneRoundNumIndex);
                            //return;
                        }
                    }

                    if (isFinislast() && !isFinishAllRound)
                    {
                        _recordMilestoneScore -= currentStageScore;
                        _curMilistoneStageIndex = 0;
                        _curMilistoneRoundNumIndex++;

                    }
                }

                SetMilestoneData(_curMilistoneRoundNum, _curMilistoneRoundNumIndex);

                if (IsFinishAllMaxConfigRoundMilestone())
                {

                    //完成所有轮次进行检查是否结束活动
                    if (IsCompleteEndActivity())
                    {
                        DebugEx.Info("RedeemShopLike: All milestones completed, checking for end activity.");
                        Game.Manager.activity.EndImmediate(this, false);
                    }
                    //_curMilistoneStageIndex = _curShowMilestoneConfig.MilestoneScore.Count - 1; // 设置为最后一个阶段索引
                    break;
                }
                else
                {
                    // 更新当前阶段的目标分数
                    currentStageScore = _curShowMilestoneConfig.MilestoneScore[_curMilistoneStageIndex];
                }
               
            }
        }

        public bool isFinislast()
        {
            var milestoneScoreArray = _curShowMilestoneConfig.MilestoneScore;
            var milestoneMaxScore = milestoneScoreArray[milestoneScoreArray.Count - 1];
            return _recordMilestoneScore >= milestoneMaxScore;
        }


        #endregion

        #region 里程碑兑换币及数据更新
        public void UpdateRedeemCoinNum(int rewardId, int rewardCount)
        {
            //更新当前兑换币数量
            CurRedeemCoinNum += rewardCount;

            DataTracker.token_change.Track(rewardId, rewardCount, CurRedeemCoinNum, ReasonString.redeem_Coin_change);

        }

        #endregion

        #region 设置耗体生成器
        private void SetupBonusHandler()
        {
            if (_spawnBonusHandler == null)
            {
                _spawnBonusHandler = new RedeemShopSpawnBonusHandler(this);
                Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(_spawnBonusHandler);
            }
        }

        public bool isFinishMax = false;
        public void UpdateMilestoneScore(int score)
        {

            if (IsFinishAllMaxConfigRoundMilestone())
            {
                return;
            }

            var prevScore = _recordMilestoneScore;
            _recordMilestoneScore += score;


            DebugEx.Info($"redeemShopScore: _recordMilestoneScore = {_recordMilestoneScore}, score = {score}");

            MessageCenter.Get<REDEEMSHOP_SCORE_UPDATE>().Dispatch(prevScore, _recordMilestoneScore);

            if (IsFinishAllMaxConfigRoundMilestone())
            {
                return;
            }

            CheckScore();

        }
        #endregion

        private bool IsCompleteEndActivity()
        {
            bool isAllMilestone = IsFinishAllMaxConfigRoundMilestone();

            bool isAllRedeemShop = IsCompleteAllRedeemShop();

            return isAllMilestone && isAllRedeemShop;
        }

        private bool IsCompleteAllRedeemShop()
        {
            return IsFinishCurAllRewardPool() && IsFinishAllRound();
        }

        #region 获取当前里程碑阶段分数跟最大分数
        public (int, int) GetCurMelistonStageScoreProgress()
        {
            if (_curShowMilestoneConfig == null)
            {
                DebugEx.Info("GetCurStageScoreProgress _curShowMilestoneConfig is null");
                return (0, 0);
            }
            var milestoneMaxScore = _curShowMilestoneConfig.MilestoneScore[_curShowMilestoneConfig.MilestoneScore.Count -1];
            return (_recordMilestoneScore, milestoneMaxScore);
        }

        public (int, int) GetCurEntryStageScoreProgress()
        {
            if (IsFinishAllMaxConfigRoundMilestone())
            {
                return (_recordMilestoneScore, _curShowMilestoneConfig.MilestoneScore[_curShowMilestoneConfig.MilestoneScore.Count - 1]);
            }
            if (_curShowMilestoneConfig == null)
            {
                DebugEx.Info("GetCurStageScoreProgress _curShowMilestoneConfig is null");
                return (0, 0);
            }
            var milestoneMaxScore = _curShowMilestoneConfig.MilestoneScore[_curMilistoneStageIndex];
            int preScore = 0;
            if (_curMilistoneStageIndex >= 1)
            {
                preScore = _curShowMilestoneConfig.MilestoneScore[_curMilistoneStageIndex - 1];
            }
            return (_recordMilestoneScore - preScore, milestoneMaxScore - preScore);
            //return (_recordMilestoneScore, milestoneMaxScore);
        }

        #endregion

        #region 获取当前奖励提交数据
        public RewardConfig GetTargetRewardConfig()
        {
            if (_curShowMilestoneConfig == null)
            {
                DebugEx.Info("GetRewardsById _curShowMilestoneConfig is null");
                return null;
            }
            if (_curMilistoneStageIndex >= _curShowMilestoneConfig.TokenReward.Count)
            {
                DebugEx.Info($"GetTargetRewardConfig _curMilistoneStageIndex out of range: {_curMilistoneStageIndex}");
                return null;
            }
            return _curShowMilestoneConfig.TokenReward[_curMilistoneStageIndex].ConvertToRewardConfig();
        }

        #endregion

        #region 阶段奖励弹窗
        public void ShowPopup(int stage)
        {
            VisualUIRedeemShopStageReward.Popup(custom_: stage);
        }

        #region 是否完成当前小阶段里程碑
        private bool IsFinishLastStageMilestone()
        {
            if (_curShowMilestoneConfig == null)
            {
                DebugEx.Info("IsLastStageMilestone _curShowMilestoneConfig is null");
                return false;
            }
            var milestoneScoreArray = _curShowMilestoneConfig.MilestoneScore;
            var milestoneMaxScore = milestoneScoreArray[milestoneScoreArray.Count - 1];
            return _recordMilestoneScore >= milestoneMaxScore;
        }
        #endregion

        #endregion

        #region 判断是否完成所有配置轮次
        public bool IsFinishAllMaxConfigRoundMilestone()
        {
            if (_eventRedeemDetailConfig == null || _eventRedeemDetailConfig.MilestoneGrp == null)
            {
                DebugEx.Info("IsFinishLastMilestone _eventRedeemDetailConfig is null");
                return false;
            }
            var maxBigRoundCount = _eventRedeemDetailConfig.MilestoneGrp[_eventRedeemDetailConfig.MilestoneGrp.Count - 1].ConvertToRoundsArrayItem().RoundsArray.Length;

            return _curMilistoneRoundNum >= _eventRedeemDetailConfig.MilestoneGrp.Count - 1
            && _curMilistoneRoundNumIndex >= maxBigRoundCount - 1
            && _curMilistoneStageIndex >= _curShowMilestoneConfig.MilestoneScore.Count;
        }
        #endregion

        #endregion

        #region 兑换商店相关

        #region 设置兑换商店数据
        private void SetRedeemShopData()
        {
            if (_eventRedeemDetailConfig == null)
            {
                DebugEx.Info("SetRedeemShopData _eventRedeemDetailConfig is null");
                return;
            }
            var redeemShopGrp = _eventRedeemDetailConfig.RedeemGrp;
            if (_curRedeemShopRoundNum >= redeemShopGrp.Count)
            {
                DebugEx.Info("RedeemShop RoundNum is Max: " + _curRedeemShopRoundNum);
                _curRedeemShopRoundNum = _curRedeemShopRoundNum - 1;
                //return;

            }
            //根据当前兑换商店轮次获取当前商店组，也就是第几轮数据（1:2:3:4）
            var curredeemShopGrp = redeemShopGrp[_curRedeemShopRoundNum];
            if (curredeemShopGrp.Length <= 0)
            {
                DebugEx.Info("milestoneGrpArray Data is null: " + _curRedeemShopRoundNum);
                return;
            }
            RewardPoolClear();
            var redeemShopGrpArray = curredeemShopGrp.ConvertToRoundsArrayItem().RoundsArray;
            for (int i = 0; i < redeemShopGrpArray.Length; i++)
            {
                //根据兑换商店组ID获取当前兑换商店组配置（奖池id-奖励）
                var redeemShopRewardPoolId = i + 1;
                int shopGrpId = redeemShopGrpArray[i];
                if (_redeemShopStageDic.ContainsKey(redeemShopRewardPoolId))
                {
                    DebugEx.Info("_redeemShopStageDic already has this key: " + redeemShopRewardPoolId);
                    continue;
                }
                SetRedeemShoRewardPoolNodeItemList(shopGrpId, redeemShopRewardPoolId);
                //刷新商店状态
                RefreshShopItemState(redeemShopRewardPoolId);
            }
        }
        #endregion

        #region 刷新奖池Item状态
        private void RefreshShopItemState(int poolType)
        {
            if (_redeemShopStageDic == null)
            {
                DebugEx.Info("RefreshShopItemState _redeemShopStageDic is null");
                return;
            }
            foreach (var item in _redeemShopStageDic[poolType])
            {
                if (poolType == _rewardPool1Id)
                {
                    RefreshShopItemState(item, poolType, _curPool1CanRedeemIndex);
                }
                else if (poolType == _rewardPool2Id)
                {
                    RefreshShopItemState(item, poolType, _curPool2CanRedeemIndex);
                }
                else if (poolType == _rewardPool3Id)
                {
                    RefreshShopItemState(item, poolType, _curPool3CanRedeemIndex);
                }
            }
        }
        #endregion

        #region 刷新商店奖池Item状态
        private void RefreshShopItemState(RedeemShopNodeItem item, int poolType, int _curPoolCanRedeemIndex)
        {
            if (item.ItemIndex < _curPoolCanRedeemIndex)
            {
                item.ItemState = RedeemShopItemState.Done;
                item.IsCur = false;
            }
            else if (item.ItemIndex == _curPoolCanRedeemIndex)
            {
                if (poolType == _rewardPool1Id || poolType == _rewardPool2Id || poolType == _rewardPool3Id)
                {
                    if (poolType == _rewardPool1Id)
                    {
                        //这里根据_curPool1CanRedeemIndex的值来获取当前Item的剩余次数
                        if (_curPool1CanRedeemLeftNum == 0)
                        {
                            var redeemRewardConfig = Game.Manager.configMan.GetEventRedeemReward(item.RewardId);
                            _curPool1CanRedeemLeftNum = redeemRewardConfig.RedeemTimes;
                        }
                        item.LeftRedeemCount = _curPool1CanRedeemLeftNum;
                        //_curPool1CanRedeemLeftNum = item.LeftRedeemCount;

                        item.ItemState = RefreshRedeemShopItemState(item.needRedeemScore, _curPool1CanRedeemLeftNum, CurRedeemCoinNum, item.preNum, item.RewardPoolType);
                    }
                    else if (poolType == _rewardPool2Id)
                    {
                        if (_curPool2CanRedeemLeftNum == 0)
                        {
                            var redeemRewardConfig = Game.Manager.configMan.GetEventRedeemReward(item.RewardId);
                            _curPool2CanRedeemLeftNum = redeemRewardConfig.RedeemTimes;
                        }
                        item.LeftRedeemCount = _curPool2CanRedeemLeftNum;

                        //_curPool2CanRedeemLeftNum = item.LeftRedeemCount;
                        //item.LeftRedeemCount = _curPool2CanRedeemLeftNum;

                        item.ItemState = RefreshRedeemShopItemState(item.needRedeemScore, _curPool2CanRedeemLeftNum, CurRedeemCoinNum, item.preNum, item.RewardPoolType);
                    }
                    else if (poolType == _rewardPool3Id)
                    {
                        if (_curPool3CanRedeemLeftNum == 0)
                        {
                            var redeemRewardConfig = Game.Manager.configMan.GetEventRedeemReward(item.RewardId);
                            _curPool3CanRedeemLeftNum = redeemRewardConfig.RedeemTimes;
                        }
                        item.LeftRedeemCount = _curPool3CanRedeemLeftNum;

                        //_curPool3CanRedeemLeftNum = item.LeftRedeemCount;
                        //item.LeftRedeemCount = _curPool3CanRedeemLeftNum;

                        item.ItemState = RefreshRedeemShopItemState(item.needRedeemScore, _curPool3CanRedeemLeftNum, CurRedeemCoinNum, item.preNum, item.RewardPoolType);
                    }
                }
                else
                {
                    item.ItemState = RefreshRedeemShopItemState(item.needRedeemScore, item.LeftRedeemCount, CurRedeemCoinNum, item.preNum, item.RewardPoolType);
                }
            }
            else
            {
                item.ItemState = RefreshRedeemShopItemState(item.needRedeemScore, item.LeftRedeemCount, CurRedeemCoinNum, item.preNum, item.RewardPoolType);
            }
            item.IsCur = item.ItemState == RedeemShopItemState.CanRedeem;
        }
        #endregion

        #region 刷新指定Cell
        public void RefreshItemByIndex(int index, int poolType, int leftRedeemCount)
        {
            if (_redeemShopStageDic == null)
            {
                DebugEx.Info("RefreshItemByIndex _redeemShopStageDic is null");
                return;
            }
            var nodeItem = _redeemShopStageDic[poolType][index];

            nodeItem.ItemState = RefreshRedeemShopItemState(nodeItem.needRedeemScore, leftRedeemCount, CurRedeemCoinNum, nodeItem.preNum, (ShopRewardPoolType)poolType);

            if (nodeItem.ItemState == RedeemShopItemState.CanRedeem)
            {
                //这里应该分奖池赋值
                if (poolType == (int)ShopRewardPoolType.Pool1)
                {
                    nodeItem.LeftRedeemCount = leftRedeemCount;
                    _curPool1CanRedeemLeftNum = leftRedeemCount;
                }
                else if (poolType == (int)ShopRewardPoolType.Pool2)
                {
                    nodeItem.LeftRedeemCount = leftRedeemCount;
                    _curPool2CanRedeemLeftNum = leftRedeemCount;
                }
                else if (poolType == (int)ShopRewardPoolType.Pool3)
                {
                    nodeItem.LeftRedeemCount = leftRedeemCount;
                    _curPool3CanRedeemLeftNum = leftRedeemCount;
                }

            }

            //更新存储 开启下一个
            if (nodeItem.ItemState == RedeemShopItemState.Done)
            {
                nodeItem.IsCur = false;
                Track_RedeemReward(nodeItem);
                RefreshSaveData((int)nodeItem.RewardPoolType);
                if (!ShopIsHasNextStage((int)nodeItem.RewardPoolType))
                {
                    //没有下一阶段了 开始刷新奖池
                    bool isFinishAllShop = IsFinishCurAllRewardPool();
                    if (isFinishAllShop)
                    {
                        //所有奖池都完成了
                        _curRedeemShopRoundNum++;
                        _curRedeemShopRoundNumIndex++;

                        if (IsFinishAllRound())
                        {
                            //完成当前商店所有轮次,检查活动是否全部完成
                            bool isFinishAllActivity = IsCompleteEndActivity();
                            if (isFinishAllActivity)
                            {
                                Game.Manager.activity.EndImmediate(this, false);
                            }
                            return;
                        }
                        _curPool1CanRedeemIndex = 0;
                        _curPool2CanRedeemIndex = 0;
                        _curPool3CanRedeemIndex = 0;

                        _pool1PlayAnimStateId = 0;
                        _pool2PlayAnimStateId = 0;
                        _pool3PlayAnimStateId = 0;

                        Reward_num = 0;

                        SetRedeemShopData();
                        RefreshDataRecordByRewardPool();
                        MessageCenter.Get<MSG.REDEEMSHOP_PANEL_REFRESH>().Dispatch(true);
                    }
                    return;
                }
                var nodeItemNext = GetNextNodeItem(poolType);
                nodeItemNext.ItemState = RefreshRedeemShopItemState(nodeItemNext.needRedeemScore, nodeItemNext.LeftRedeemCount, CurRedeemCoinNum, nodeItemNext.preNum, (ShopRewardPoolType)poolType);

                if (nodeItemNext.ItemState == RedeemShopItemState.CanRedeem)
                {
                    nodeItemNext.IsCur = true;
                    var canRedeemPoolIndex = GetCanRedeemIndexByPoolType((int)nodeItem.RewardPoolType);

                    //_curPool1CanRedeemLeftNum = leftRedeemCount;
                    UpdateLookRedPointFlag(true);

                    //刷新下一个Item
                    MessageCenter.Get<MSG.REDEEMSHOP_BUY_REFRESH>().Dispatch((int)nodeItemNext.RewardPoolType, canRedeemPoolIndex, nodeItemNext.LeftRedeemCount);
                    SetCurPlayAnimStateByPoolType((int)nodeItem.RewardPoolType);
                }
            }
        }
        #endregion

        #region 获取下一个Item
        private RedeemShopNodeItem GetNextNodeItem(int poolType)
        {
            if (_redeemShopStageDic[poolType].Count > 0)
            {
                int canRedeemIndex = GetCanRedeemIndexByPoolType(poolType);
                return _redeemShopStageDic[poolType][canRedeemIndex];
            }
            return null;
        }
        #endregion

        #region 根据奖池类型获取当前可以兑换的索引
        private int GetCanRedeemIndexByPoolType(int poolType)
        {
            if (poolType == (int)ShopRewardPoolType.Pool1)
            {
                return _curPool1CanRedeemIndex;
            }
            else if (poolType == (int)ShopRewardPoolType.Pool2)
            {
                return _curPool2CanRedeemIndex;
            }
            else if (poolType == (int)ShopRewardPoolType.Pool3)
            {
                return _curPool3CanRedeemIndex;
            }
            return -1;
        }


        private void SetCurPlayAnimStateByPoolType(int poolType)
        {
            if (poolType == (int)ShopRewardPoolType.Pool1)
            {
                _pool1PlayAnimStateId = 1;
            }
            else if (poolType == (int)ShopRewardPoolType.Pool2)
            {
                _pool2PlayAnimStateId = 1;
            }
            else if (poolType == (int)ShopRewardPoolType.Pool3)
            {
                _pool3PlayAnimStateId = 1;
            }
        }
        #endregion

        #region 刷新ItemSaveData
        private void RefreshSaveData(int redeemShopRewardPoolId)
        {
            if (redeemShopRewardPoolId == (int)ShopRewardPoolType.Pool1)
            {
                _rewardPool1Id = redeemShopRewardPoolId;
                _curPool1CanRedeemLeftNum--;
                _curPool1CanRedeemIndex++;
                _pool1PlayAnimStateId = 0;
            }
            else if (redeemShopRewardPoolId == (int)ShopRewardPoolType.Pool2)
            {
                _rewardPool2Id = redeemShopRewardPoolId;
                _curPool2CanRedeemLeftNum--;
                _curPool2CanRedeemIndex++;
                _pool2PlayAnimStateId = 0;
            }
            else if (redeemShopRewardPoolId == (int)ShopRewardPoolType.Pool3)
            {
                _rewardPool3Id = redeemShopRewardPoolId;
                _curPool3CanRedeemLeftNum--;
                _curPool3CanRedeemIndex++;
                _pool3PlayAnimStateId = 0;
            }
        }
        #endregion

        #region 商店是否存在下一个阶段(Item)
        private bool ShopIsHasNextStage(int poolType)
        {
            if (_redeemShopStageDic[poolType].Count > 0)
            {
                if (poolType == (int)ShopRewardPoolType.Pool1)
                {
                    if (_curPool1CanRedeemIndex < _redeemShopStageDic[poolType].Count)
                    {
                        return true;
                    }
                }
                else if (poolType == (int)ShopRewardPoolType.Pool2)
                {
                    if (_curPool2CanRedeemIndex < _redeemShopStageDic[poolType].Count)
                    {
                        return true;
                    }
                }
                else if (poolType == (int)ShopRewardPoolType.Pool3)
                {
                    if (_curPool3CanRedeemIndex < _redeemShopStageDic[poolType].Count)
                    {
                        return true;
                    }
                }

            }
            return false;
        }
        #endregion

        #region 是否完成当前所有奖励池
        private bool IsFinishCurAllRewardPool()
        {
            bool isFinishPool1 = false;
            bool isFinishPool2 = false;
            bool isFinishPool3 = false;
            //分别判断三个奖池
            foreach (var item in _redeemShopStageDic)
            {
                var poolType = item.Key;
                if (poolType == (int)ShopRewardPoolType.Pool1)
                {
                    isFinishPool1 = _curPool1CanRedeemIndex >= _redeemShopStageDic[poolType].Count;
                }
                else if (poolType == (int)ShopRewardPoolType.Pool2)
                {
                    isFinishPool2 = _curPool2CanRedeemIndex >= _redeemShopStageDic[poolType].Count;
                }
                else if (poolType == (int)ShopRewardPoolType.Pool3)
                {
                    isFinishPool3 = _curPool3CanRedeemIndex >= _redeemShopStageDic[poolType].Count;
                }
            }
            return isFinishPool1 && isFinishPool2 && isFinishPool3;
        }
        #endregion

        #region 是否完成配置所有轮次
        private bool IsFinishAllRound()
        {
            var redeemShopGrp = _eventRedeemDetailConfig.RedeemGrp;
            //根据当前兑换商店轮次获取当前商店组，也就是第几轮数据（1:2:3:4）
            if (_curRedeemShopRoundNum >= redeemShopGrp.Count)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region 轮次刷新根据奖池设置数据
        private void RefreshDataRecordByRewardPool()
        {
            foreach (var item in _redeemShopStageDic)
            {
                foreach (var itemData in item.Value)
                {
                    if (itemData.RewardPoolType == ShopRewardPoolType.Pool1)
                    {
                        if (itemData.ItemIndex == _curPool1CanRedeemIndex)
                        {
                            //当前可以触发的奖励
                            _rewardPool1Id = (int)itemData.RewardPoolType;
                            _curPool1CanRedeemIndex = itemData.ItemIndex;
                            _curPool1CanRedeemLeftNum = itemData.LeftRedeemCount;
                        }
                    }
                    else if (itemData.RewardPoolType == ShopRewardPoolType.Pool2)
                    {
                        if (itemData.ItemIndex == _curPool2CanRedeemIndex)
                        {
                            _rewardPool2Id = (int)itemData.RewardPoolType;
                            _curPool2CanRedeemIndex = itemData.ItemIndex;
                            _curPool2CanRedeemLeftNum = itemData.LeftRedeemCount;
                        }
                    }
                    else if (itemData.RewardPoolType == ShopRewardPoolType.Pool3)
                    {
                        if (itemData.ItemIndex == _curPool3CanRedeemIndex)
                        {
                            _rewardPool3Id = (int)itemData.RewardPoolType;
                            _curPool3CanRedeemIndex = itemData.ItemIndex;
                            _curPool3CanRedeemLeftNum = itemData.LeftRedeemCount;
                        }
                    }
                }
            }
        }
        #endregion

        #region 设置兑换商店奖池数据
        private void SetRedeemShoRewardPoolNodeItemList(int shopGrpId, int redeemShopRewardPoolId)
        {
            var redeemGrpConfig = Game.Manager.configMan.GetEventRedeemGrp(shopGrpId);
            if (redeemGrpConfig == null)
            {
                DebugEx.Info("EventRedeemGrp Config is null");
                return;
            }
            var tempList = new List<RedeemShopNodeItem>();
            for (int i = 0; i < redeemGrpConfig.RewardDetail.Count; i++)
            {
                var rewardId = redeemGrpConfig.RewardDetail[i];
                var redeemRewardConfig = Game.Manager.configMan.GetEventRedeemReward(rewardId);
                if (redeemRewardConfig == null)
                {
                    DebugEx.Info("EventRedeemReward Config is null");
                    continue;
                }
                var redeemShopNodeItem = new RedeemShopNodeItem()
                {
                    preNum = i - 1,
                    RoundNum = _curRedeemShopRoundNum,
                    BoardType = (ShopBoardType)redeemRewardConfig.BoardType,
                    needRedeemScore = redeemRewardConfig.RedeemScore,
                    RedeemRewardList = GetRewardsById(redeemRewardConfig.Id),
                    LeftRedeemCount = redeemRewardConfig.RedeemTimes,
                    RewardPoolType = (ShopRewardPoolType)redeemShopRewardPoolId,
                    ItemIndex = i,
                    IsPlayAnim = false,
                    ShopGrpId = shopGrpId,
                    RewardId = redeemRewardConfig.Id,
                    IsLast = i == redeemGrpConfig.RewardDetail.Count - 1
                };

                tempList.Add(redeemShopNodeItem);
            }

            if (redeemShopRewardPoolId == (int)ShopRewardPoolType.Pool1)
            {
                _rewardPool1Id = redeemShopRewardPoolId;
                GetPoolListByPoolType(redeemShopRewardPoolId, tempList);
            }
            else if (redeemShopRewardPoolId == (int)ShopRewardPoolType.Pool2)
            {
                _rewardPool2Id = redeemShopRewardPoolId;
                GetPoolListByPoolType(redeemShopRewardPoolId, tempList);
            }
            else if (redeemShopRewardPoolId == (int)ShopRewardPoolType.Pool3)
            {
                _rewardPool3Id = redeemShopRewardPoolId;
                GetPoolListByPoolType(redeemShopRewardPoolId, tempList);
            }
            else
            {
                DebugEx.Info("not found redeemShopRewardPoolId: " + redeemShopRewardPoolId);
            }
        }

        private void GetPoolListByPoolType(int poolType, List<RedeemShopNodeItem> list)
        {
            if (!_redeemShopStageDic.ContainsKey(poolType))
            {
                _redeemShopStageDic.Add(poolType, list);
            }
        }

        #endregion

        #region 获取兑换奖励列表
        private List<RewardConfig> GetRewardsById(int id)
        {
            var rewardList = new List<RewardConfig>();

            var redeemRewardConfig = Game.Manager.configMan.GetEventRedeemReward(id);
            if (redeemRewardConfig == null)
            {
                DebugEx.Info("GetRewardsById redeemRewardConfig is null");
                return null;
            }
            _redeemRewardList.Clear();
            for (int i = 0; i < redeemRewardConfig.Reward.Count; i++)
            {
                var reward = redeemRewardConfig.Reward[i].ConvertToRewardConfig();
                rewardList.Add(reward);
            }
            return rewardList;
        }

        #endregion

        #region 兑换商店状态刷新
        private RedeemShopItemState RefreshRedeemShopItemState(int needRedeemScore, int leftRedeemCount, int redeemCoinNum, int preNum, ShopRewardPoolType poolType)
        {
            if (IsFinishPreState(preNum, poolType) && leftRedeemCount > 0)
            {
                if (needRedeemScore <= 0)
                {
                    //免费
                    //return RedeemShopItemState.Free;
                }
                //前置条件完成
                return RedeemShopItemState.CanRedeem;
            }

            if (redeemCoinNum >= needRedeemScore && leftRedeemCount <= 0)
            {
                return RedeemShopItemState.Done;
            }
            else if (needRedeemScore == 0 && leftRedeemCount > 0 && IsFinishPreState(preNum, poolType))
            {
                //需要兑换币数量为0 兑换次数大于0
                return RedeemShopItemState.Free;
            }
            else if (!IsFinishPreState(preNum, poolType))
            {
                //前置的条件没有完成 那就是锁定状态
                return RedeemShopItemState.Lock;
            }
            return RedeemShopItemState.None;
        }
        #endregion

        #region 是否完成前置条件判断
        private bool IsFinishPreState(int preNum, ShopRewardPoolType poolType)
        {
            if (preNum < 0)
            {
                return true;
            }
            var preNodeItem = _redeemShopStageDic[(int)poolType][preNum];
            if (preNodeItem.ItemState == RedeemShopItemState.Done)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region 奖励池清除
        private void RewardPoolClear()
        {
            _redeemShopStageDic.Clear();
            _redeemRewardPool1List.Clear();
            _redeemRewardPool2List.Clear();
            _redeemRewardPool3List.Clear();
        }
        #endregion

        #region 获取当前展示里程碑最大跟最小值
        public (int, int) GetScoreShowNum()
        {
            if (_curShowMilestoneConfig == null)
            {
                DebugEx.Info("GetCurStageScoreProgress _curShowMilestoneConfig is null");
                return (0, 0);
            }
            var milestoneMaxScore = _curShowMilestoneConfig.MilestoneScore[_curShowMilestoneConfig.MilestoneScore.Count -1];
            return (_recordMilestoneScore, milestoneMaxScore);
        }



        public RewardConfig GetScoreShowReward()
        {
            if (IsFinishAllMaxConfigRoundMilestone())
            {
                var reward = _curShowMilestoneConfig.TokenReward[_curShowMilestoneConfig.TokenReward.Count - 1];
                return reward.ConvertToRewardConfig();
            }
            var str =  _curShowMilestoneConfig.TokenReward[_curMilistoneStageIndex];
            return str.ConvertToRewardConfig();
        }

        public (int, int) GetNextScore()
        {
            if (IsFinishAllMaxConfigRoundMilestone())
            {
                var maxScore = _curShowMilestoneConfig.MilestoneScore[_curShowMilestoneConfig.MilestoneScore.Count - 1];
                return (_recordMilestoneScore, maxScore);
            }
            var curMinStageMaxScore = _curShowMilestoneConfig.MilestoneScore[_curMilistoneStageIndex];
            int preScore = 0;
            if (_curMilistoneStageIndex >= 1)
            {
                preScore = _curShowMilestoneConfig.MilestoneScore[_curMilistoneStageIndex - 1];
            }
            return (_recordMilestoneScore - preScore, curMinStageMaxScore - preScore);
        }


        #endregion

        #region 获取入口奖励进度相关
        public RewardCommitData TryGetCommitReward(RewardConfig reward)
        {
            RewardCommitData rewardCommitData = null;
            foreach (var commitData in _StateRewardCommitRewardList)
            {
                if (commitData.rewardId == reward.Id && commitData.rewardCount == reward.Count)
                {
                    rewardCommitData = commitData;
                    break;
                }
            }
            return rewardCommitData;
        }

        #endregion

        #region 奖励播放状态Id
        public void SetRewardPoolPlayStateId(ShopRewardPoolType poolType, int id)
        {
            if (poolType == ShopRewardPoolType.Pool1)
            {
                _pool1PlayAnimStateId = id;
            }
            else if (poolType == ShopRewardPoolType.Pool2)
            {
                _pool2PlayAnimStateId = id;
            }
            else if (poolType == ShopRewardPoolType.Pool3)
            {
                _pool3PlayAnimStateId = id;
            }
        }
        #endregion

        #region 是否存在可领取奖励

        #region 是否存在可领取奖励
        public bool IsHasCanRedeemReward()
        {
            if (_redeemShopStageDic == null || _redeemShopStageDic.Count <= 0)
            {
                return false;
            }
            foreach (var item in _redeemShopStageDic)
            {
                foreach (var nodeItem in item.Value)
                {
                    if (IsCanRedeemReward(nodeItem))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsCanRedeemReward(RedeemShopNodeItem nodeItem)
        {
            if (nodeItem == null)
            {
                return false;
            }
            if (CurRedeemCoinNum >= nodeItem.needRedeemScore && nodeItem.IsCur)
            {
                //满足兑换条件
                if (nodeItem.ItemState == RedeemShopItemState.CanRedeem)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateLookRedPointFlag(bool isLook)
        {
            IsLookRedPoint = isLook;

            if (isLook)
            {
                MessageCenter.Get<REDEEMSHOP_REDPOINT_REFRESH>().Dispatch(isLook);
            }
        }

        #endregion

        #region 兑换商品数据
        public void BuyShopDataChange(int costCount, int rewardId)
        {
            CurRedeemCoinNum -= costCount;

            DataTracker.token_change.Track(rewardId, costCount, CurRedeemCoinNum, ReasonString.redeem_Coin_change);

        }

        #endregion

        #region 结算
        public void CheckSettlement()
        {
            if (_eventRedeemConfig == null)
            {
                return;
            }
            using var _ = PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            map[_eventRedeemConfig.TokenId] = CurRedeemCoinNum;
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            ActivityExpire.ConvertToReward(_eventRedeemConfig.ExpirePopup, list, ReasonString.redeem_token_energy, map);
            if (list == null || list.Count <= 0)
            {
                return;
            }
            Game.Manager.screenPopup.Queue(VisualUIRedeemShopSettlement.popup, listT);
        }
        #endregion

        #region 打点相关

        #region 里程碑领奖打点

        private void TrackGetMelistoneReward()
        {
            int milestone_stage = _curMilistoneRoundNumIndex + 1;
            int milestone_queue = _curMilistoneStageIndex;
            int milestone_num = _curShowMilestoneConfig.MilestoneScore.Count;
            int milestone_difficulty = _eventRedeemDetailConfig.Diff;
            var is_Final = IsFinishLastStageMilestone();
            int round_Num = _curMilistoneRoundNum + 1;
            Track_GetMelistoneReward(milestone_stage, milestone_queue, milestone_num, milestone_difficulty, is_Final, round_Num);
        }


        private void Track_GetMelistoneReward(int milestoneStage, int milestoneQueue, int milestoneNum, int milestoneDifficulty, bool isFinal, int roundNum)
        {
            DataTracker.event_redeem_complete.Track(this, milestoneStage, milestoneQueue, milestoneNum, milestoneDifficulty, isFinal, roundNum);
        }
        #endregion

        #region 兑换奖励打点
        public void Track_RedeemReward(RedeemShopNodeItem nodeItem)
        {
            var isFinal = IsRedeemShopFinal(nodeItem);
            DataTracker.event_redeem_reward.Track(this, nodeItem.ShopGrpId, nodeItem.ItemIndex + 1, _eventRedeemDetailConfig.Diff, _curRedeemShopRoundNum + 1, isFinal, ++Reward_num);
        }

        private bool IsRedeemShopFinal(RedeemShopNodeItem nodeItem)
        {
            // 如果没有池子，直接返回false
            if (_redeemShopStageDic.Count == 0)
                return false;
                
            foreach (var item in _redeemShopStageDic)
            {
                var rewardPoolList = item.Value;

                // 如果池子为空，返回false
                if (rewardPoolList == null || rewardPoolList.Count == 0)
                    return false;

                // 找到池子中的最后一个节点
                var lastNode = rewardPoolList.Find(node => node.IsLast);
                if (lastNode == null)
                    return false;

                if (lastNode.ItemState != RedeemShopItemState.Done)
                    return false;
            }

            // 所有池子的最后一个节点都完成了
            return true;
        }



        #endregion

        #endregion

        #endregion

        #endregion

        #region 音效
        public static void PlaySound(AudioEffect se)
        {
            Game.Manager.audioMan.TriggerSound(se.ToString());
        }

        #endregion
    }

    public class RedeemShopEntry : ListActivity.IEntrySetup
    {
        private readonly ListActivity.Entry e;
        private readonly ActivityRedeemShopLike activity;
        public RedeemShopEntry(ListActivity.Entry e, ActivityRedeemShopLike activity)
        {
            this.activity = activity;
            this.e = e;
            var hasReward = activity.IsHasCanRedeemReward() && !activity.IsLookRedPoint;
            e.dot.SetActive(hasReward);
            MessageCenter.Get<REDEEMSHOP_REDPOINT_REFRESH>().AddListener(LookRedPointRefresh);
        }

        public override void Clear(ListActivity.Entry e_)
        {
            MessageCenter.Get<REDEEMSHOP_REDPOINT_REFRESH>().RemoveListener(LookRedPointRefresh);
        }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }

        private void LookRedPointRefresh(bool obj)
        {
            if (!activity.Valid) return;
            var isHasReward = activity.IsHasCanRedeemReward();
            e.dot.SetActive(isHasReward && !activity.IsLookRedPoint);
        }
    }

    public class MliestoneNodeItem
    {
        public int milestoneId;
        public int milestoneScore;
        public int milestoneRoundNum;
        public int milestoneRewardId;
        public int milestoneRewardCount;
        public bool IsDonePro;
        public bool IsLast;
        public int StageNum;
        public bool isConfigMaxNode;  //所有轮次最后一个节点
    }

    public class RedeemShopNodeItem
    {
        public int RoundNum;
        public ShopBoardType BoardType;
        public int needRedeemScore;
        public int LeftRedeemCount;
        public List<RewardConfig> RedeemRewardList;
        public RedeemShopItemState ItemState;
        public ShopRewardPoolType RewardPoolType;
        public int preNum;
        public bool IsCur;
        public int ItemIndex;
        public bool IsPlayAnim;
        public int ShopGrpId;
        public int RewardId;
        public bool IsLast;
    }

    public class ShopRewardItemCell
    {
        public GameObject cell;
        public UICommonItem item;
        public RewardCommitData data;

        public void Init(GameObject cell_)
        {
            this.cell = cell_;
            item = cell.GetComponent<UICommonItem>();
        }

        public void SetData(RewardCommitData data_)
        {
            data = data_;
        }

    }

    public enum ShopBoardType
    {
        Small = 0,
        Big = 1,
    }

    public enum RedeemShopItemState
    {
        None = 0,
        Free = 1,
        Lock = 2,
        CanRedeem = 3,
        Done = 4,
    }

    public enum ShopRewardPoolType
    {
        Pool1 = 1,
        Pool2 = 2,
        Pool3 = 3,
    }

    public enum AudioEffect
    {
        RedeemComplete, //兑换阶段完成
        RedeemUnlock, //商品解锁音效f
        RedeemSwitch, //单个商品兑换完成打钩
        RedeemAccept, //入口接收动画时播放完成
    }


}