/*
 * @Author: zhangchaoran
 * @Date: 2025-06-16 10:59:00
 */
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using Config;
using Cysharp.Text;
using DG.Tweening;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using FAT.MSG;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class WishBoardActivity : ActivityLike, IBoardEntry, IBoardArchive, IActivityUpdate, IActivityOrderHandler, ISpawnEffectWithTrail, IExternalOutput, IBoardActivityHandbook, IBoardActivityRowConf
    {
        public override bool Valid => Lite.Valid && ConfD != null;
        #region Visual
        public VisualRes VisualUIBoardMain { get; } = new(UIConfig.UIWishBoardMain); //棋盘主界面
        public VisualRes VisualUIHelp { get; } = new(UIConfig.UIWishBoardHelp); //帮助界面
        public VisualRes VisualUILoading { get; } = new(UIConfig.UIWishBoardLoading); //loading界面
        public VisualRes VisualUIHandbookTips { get; } = new(UIConfig.UIWishBoardHandbookTips); //庆祝横幅
        public VisualRes VisualUIHandbook { get; } = new(UIConfig.UIWishBoardHandbook); //图鉴界面
        public VisualRes VisualUIMilestone { get; } = new(UIConfig.UIWishBoardMilestone);//里程碑
        public VisualRes VisualUITip { get; } = new(UIConfig.UIWishBoardTip);

        // 弹脸
        public VisualPopup VisualStartNoticePopup { get; } = new(UIConfig.UIWishBoardStartNotice); //活动开启
        public VisualPopup VisualEndNoticePopup { get; } = new(UIConfig.UIWishBoardEndNotice); //活动结束
        public VisualPopup VisualConvertPopup { get; } = new(UIConfig.UIWishBoardConvert); //补领
        #endregion

        public MergeWorld World { get; private set; }
        public MergeWorldTracer WorldTracer { get; private set; }
        public int UnlockMaxLevel { get; private set; }
        public EventWishBoard ConfD { get; private set; }
        public int GroupId { get; private set; }  //用户分层 区别棋盘配置 对应EventWishBoardGroup.id
        private int _progressPhase; //当前进度条所处阶段 从0开始 根据阶段值读配置获取当前的最大进度以及达成后可获得的奖励
        private int _progressNum; //当前进度积分

        FeatureEntry IBoardArchive.Feature => FeatureEntry.FeatureWishBoard;
        public TokenOutputType OutputType { get; private set; } = TokenOutputType.None;
        public override ActivityVisual Visual => VisualStartNoticePopup.visual;

        public WishBoardActivity(ActivityLite lite)
        {
            Lite = lite;
            ConfD = Game.Manager.configMan.GetEventWishBoardConfig(lite.Param);
            InitTheme();
        }

        private void InitTheme()
        {
            VisualUIBoardMain.Setup(ConfD.BoardTheme);
            VisualUIHelp.Setup(ConfD.HelpTheme);
            VisualUILoading.Setup(ConfD.LoadingTheme);
            VisualUIHandbookTips.Setup(ConfD.CongratulateTheme);
            VisualUIHandbook.Setup(ConfD.GalleyTheme);
            VisualUIMilestone.Setup(ConfD.StepRewardTheme);
            VisualStartNoticePopup.Setup(ConfD.EventTheme, this, active_: false);
            VisualEndNoticePopup.Setup(ConfD.EndTheme, this, active_: false);
            VisualConvertPopup.Setup(ConfD.EndRewardTheme, this, active_: false);
            VisualUITip.Setup(ConfD.TipTheme);
        }
        #region ActivityLike
        public override void SetupFresh()
        {
            GroupId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            //初始化棋盘 只在活动创建时走一次
            _InitWishBoardData();
            //添加初始棋子
            _InitStartToken();
            //刷新弹脸信息
            InitTheme();
            //刷新代币产出类
            _RefreshTokenOutputType();
            //刷新耗体产出模块
            _RefreshSpawnBonusHandler();
            //活动首次开启时弹
            Game.Manager.screenPopup.TryQueue(VisualStartNoticePopup.popup, PopupType.Login);
        }

        private List<int> _allItemIdList = new List<int>();    //当前活动主链条的所有棋子idList 按等级由小到大排序
        //初始化棋盘 只在活动创建时走一次
        private void _InitWishBoardData()
        {
            _allItemIdList.Clear();
            var infoConfig = GetCurGroupConfig();
            if (infoConfig == null)
                return;
            //初始化图鉴棋子list
            foreach (var id in infoConfig.MilestoneId)
            {
                var mile = Game.Manager.configMan.GetEventWishMilestone(id);
                _allItemIdList.AddIfAbsent(mile.MilestoneItem);
            }
            //每次活动创建时都清空一下图鉴系统中的解锁信息 以防活动异常结束导致图鉴没有锁定
            Game.Manager.handbookMan.LockHandbookItem(_allItemIdList);
            //第一次初始化棋盘
            _InitWorld(infoConfig.BoardId, true);
            var board = World.activeBoard;
            _curDepthIndex = board.size.y;
            board.RefreshCloudInfo(_curDepthIndex, UnlockMaxLevel);
        }

        public void _InitStartToken()
        {
            var conf = GetCurGroupConfig();
            if (conf == null || ConfD == null) return;
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            GroupId = RecordStateHelper.ReadInt(i++, any);
            UnlockMaxLevel = RecordStateHelper.ReadInt(i++, any);
            _curDepthIndex = RecordStateHelper.ReadInt(i++, any);
            _progressPhase = RecordStateHelper.ReadInt(i++, any);
            _progressNum = RecordStateHelper.ReadInt(i++, any);
            //刷新图鉴棋子信息
            _RefreshAllItemIdList();
            //刷新代币产出类型
            _RefreshTokenOutputType();
            //刷新订单产出代币模块
            _RefreshSpawnBonusHandler();
        }

        public override void Open()
        {
            ActivityTransit.Enter(this, VisualUILoading, VisualUIBoardMain.res);
        }

        public override void SaveSetup(ActivityInstance data_)
        {

            var any = data_.AnyState;
            var i = 0;
            any.Add(RecordStateHelper.ToRecord(i++, GroupId));
            any.Add(RecordStateHelper.ToRecord(i++, UnlockMaxLevel));
            any.Add(RecordStateHelper.ToRecord(i++, _curDepthIndex));
            any.Add(RecordStateHelper.ToRecord(i++, _progressPhase));
            any.Add(RecordStateHelper.ToRecord(i++, _progressNum));
        }

        public override void WhenEnd()
        {
            base.WhenEnd();
            //活动结束和检查是否需要补领
            CheckActivityEndAndSettlement();
            //清理scoreEntity
            //_ClearScoreEntity();
            //清理棋盘相关数据
            _ClearWishBoardData();
        }

        private void _ClearWishBoardData()
        {
            //清理handler
            _ClearSpawnBonusHandler();
            World?.UnregisterActivityHandler(this);
            //活动结束时将关联棋子的图鉴置为锁定状态
            Game.Manager.handbookMan.LockHandbookItem(_allItemIdList);
            //取消注册并清理当前world
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            World = null;
            WorldTracer = null;
        }

        public void EnterWishBoard()
        {
            if (UIManager.Instance.IsOpen(VisualUIBoardMain.res.ActiveR)) return;
            ActivityTransit.Enter(this, VisualUILoading.res.ActiveR, VisualUIBoardMain.res);
        }

        #endregion
        public string BoardEntryAsset()
        {
            VisualStartNoticePopup.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }
        #region IActivityOrderHandler
        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            var changed = false;
            if (!IsOrderType()) { return changed; }
            if (order == null || order.ConfRandomer == null || !order.ConfRandomer.IsExtraScore) { return changed; }
            var state = order.GetState((int)OrderParamType.ScoreEventIdBR);
            // 没有奖励 or 不是同一期活动时给这个订单生成右下角积分
            if (state == null || state.Value != Id)
            {
                (var scoreTotal, var scoreId) = GetScoreReward(order.GetValue(OrderParamType.PayDifficulty));
                if (scoreId == 0 || scoreTotal == 0) { return changed; }
                changed = true;
                OrderAttachmentUtility.slot_score_br.UpdateScoreDataBR(order, Id, scoreTotal, scoreId);
            }
            return changed;
        }

        private (int, int) GetScoreReward(int diff)
        {
            var group = GetCurGroupConfig();
            var num = 0;
            var reward = 0;
            foreach (var id in group.OrderItemId)
            {
                var data = fat.conf.Data.GetEventWishOrderItem(id);
                if (diff >= data.PayDifficulty)
                {
                    reward = data.ItemId;
                    num = diff / data.Base;
                    if (num == 0) { num = 1; }
                }
            }
            return (num, reward);
        }
        #endregion

        #region IExternalOutput
        public bool CanUseItem(Item source)
        {
            foreach (var id in ConfD.BonusItemMax)
            {
                if (source.tid == id)
                {
                    if (!CheckProgressFinish()) { return true; }
                    else
                    {
                        var img1 = Game.Manager.objectMan.GetBasicConfig(source.tid).Icon.ConvertToAssetConfig().Asset.Split(".")[0];
                        var img2 = Game.Manager.objectMan.GetBasicConfig(GetCurGroupConfig().Cyclebox).Icon.ConvertToAssetConfig().Asset.Split(".")[0];
                        var str1 = ZString.Format("<sprite name=\"{0}\">", img1);
                        var str2 = ZString.Format("<sprite name=\"{0}\">", img2);
                        Game.Manager.commonTipsMan.ShowMessageTips(I18N.FormatText("#SysComDesc1259", str1, str2), I18N.Text("#SysComDesc1258"), null, null, true);
                        return false;
                    }
                }
            }
            if (source.tid == GetCurMilestone().ItemId) { return true; }
            return false;
        }
        public bool TrySpawnItem(Item source, out int outputId, out ItemSpawnContext context)
        {
            outputId = -1;
            context = null;
            var com = source.GetItemComponent<ItemActiveSourceComponent>();
            if (com.WillDead)
            {
                if (source.tid == GetCurMilestone().ItemId)
                {
                    var to = UIFlyFactory.ResolveFlyTarget(FlyType.WishBoardMilestone);
                    if (!World.activeBoard.DisposeItem(source, ItemDeadType.WishBoard))
                        return false;
                    UIFlyUtility.FlyCustom(source.tid, 1, BoardUtility.GetWorldPosByCoord(source.coord), to, FlyStyle.Common, FlyType.WishBoardMilestone, size: 136f);
                    BeginDragReward(source, 1.15f);
                }
                else if (!CheckProgressFinish())
                {
                    AddMilestoneScore(com.DropCount);
                    var to = UIFlyFactory.ResolveFlyTarget(FlyType.WishBoardScore);
                    UIFlyUtility.FlyCustom(Game.Manager.mergeItemMan.GetCategoryConfigByItemId(source.tid).Progress[0], com.Config.Drop, BoardUtility.GetWorldPosByCoord(source.coord), to, FlyStyle.Reward, FlyType.WishBoardScore, split: 3);
                }
            }
            return true;
        }
        #endregion

        #region 进度里程碑
        //获取当前进度条信息
        public EventWishBarReward GetProgressInfo(int progressPhase)
        {
            var allProgressInfo = GetCurGroupConfig()?.BarRewardId;
            //所有进度值都完成
            if (allProgressInfo == null || progressPhase >= allProgressInfo.Count || progressPhase < 0)
                return null;
            return fat.conf.Data.GetEventWishBarReward(allProgressInfo[progressPhase]);
        }

        //检查所有进度条是否都完成
        public bool CheckProgressFinish()
        {
            var allProgressInfo = GetCurGroupConfig()?.BarRewardId;
            //所有进度值都完成
            return allProgressInfo == null || _progressPhase >= allProgressInfo.Count;
        }

        public void AddMilestoneScore(int num)
        {
            var curMile = fat.conf.Data.GetEventWishBarReward(GetCurGroupConfig().BarRewardId[_progressPhase]);
            _progressNum += num;
            if (_progressNum >= curMile.BarNum)
            {
                _BeginMilestoneReward();
                _EnterNextMile();
            }
            else
            {
                MessageCenter.Get<UI_WISH_PROGRESS_CHANGE>().Dispatch(null, null, 0);
            }
        }

        private void _BeginMilestoneReward()
        {
            var curMile = fat.conf.Data.GetEventWishBarReward(GetCurGroupConfig().BarRewardId[_progressPhase]);
            var list = new List<RewardCommitData>();
            foreach (var info in curMile.BarReward)
            {
                var data = info.ConvertToInt3();
                list.Add(Game.Manager.rewardMan.BeginReward(data.Item1, data.Item2, ReasonString.wish_bar_reward));
            }
            MessageCenter.Get<UI_WISH_PROGRESS_CHANGE>().Dispatch(list, curMile.RewardIcon1, curMile.BarNum);
        }

        private void _EnterNextMile()
        {
            var curMile = fat.conf.Data.GetEventWishBarReward(GetCurGroupConfig().BarRewardId[_progressPhase]);
            _progressNum -= curMile.BarNum;
            TrackMineMilestone();
            _progressPhase++;
        }
        #endregion

        #region Board
        public void SetBoardData(fat.gamekitdata.Merge data)
        {
            if (data == null)
                return;
            _InitWorld(data.BoardId, false);
            World.Deserialize(data, null);
            World.activeBoard.RefreshCloudInfo(_curDepthIndex, UnlockMaxLevel);
            //刷新耗体产出代币模块
            _RefreshSpawnBonusHandler();
        }

        public void FillBoardData(fat.gamekitdata.Merge data)
        {
            World?.Serialize(data);
        }
        private void _InitWorld(int boardId, bool isFirstCreate)
        {
            World = new MergeWorld();
            WorldTracer = new MergeWorldTracer(_OnBoardItemChange, null);
            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry()
            {
                world = World,
                type = MergeWorldEntry.EntryType.WishBoard,
            });
            WorldTracer.Bind(World);
            World.BindTracer(WorldTracer);
            Game.Manager.mergeBoardMan.InitializeBoard(this, World, boardId, isFirstCreate);
            World.RegisterActivityHandler(this);
            if (isFirstCreate)
            {
                var ConfDetail = GetCurGroupConfig();
                var reward = ConfDetail.Startitem.ConvertToInt3();
                Game.Manager.rewardMan.PushContext(new RewardContext() { targetWorld = World });
                Game.Manager.rewardMan.CommitReward(Game.Manager.rewardMan.BeginReward(reward.Item1, reward.Item2, ReasonString.wish_start));
                Game.Manager.rewardMan.PopContext();
            }
        }
        #endregion

        #region Activity
        public int _lastKey;
        public string GetRandomKey()
        {
            var group = GetCurGroupConfig();
            var key = UnityEngine.Random.Range(0, group.KeyId.Count);
            while (_lastKey == group.KeyId[key])
            {
                key = UnityEngine.Random.Range(0, group.KeyId.Count);
            }
            _lastKey = group.KeyId[key];
            return I18N.Text(fat.conf.Data.GetEventWishKey(_lastKey).Key);
        }
        public EventWishBoardGroup GetCurGroupConfig()
        {
            return Game.Manager.configMan.GetEventWishBoardGroup(GroupId);
        }
        public int GetCurProgressPhase()
        {
            return _progressPhase;
        }

        public int GetCurProgressNum()
        {
            return _progressNum;
        }

        /// <summary>
        /// 获取当前里程碑最后一个阶段的奖励
        /// </summary>
        /// <returns></returns>
        public List<RewardConfig> GetMileStoneLastStageReward()
        {
            var listMilestone = GetCurGroupConfig().BarRewardId;
            var rewardId = listMilestone[listMilestone.Count - 1];
            var rewardConfig = Game.Manager.configMan.GetCurWishBarRewardById(rewardId);
            var reward = new RewardConfig[rewardConfig.BarReward.Count];
            for (int i = 0; i < rewardConfig.BarReward.Count; i++)
            {
                reward[i] = rewardConfig.BarReward[i].ConvertToRewardConfig();
            }
            return reward.ToList();
        }

        /// <summary>
        /// 获取当前代币数量
        /// </summary>
        /// <returns></returns>
        public int GetTokenNum()
        {
            return World.rewardCount;
        }

        private void CheckActivityEndAndSettlement()
        {
            var conf = GetCurGroupConfig();
            if (conf != null)
            {
                //活动结束
                var data = GetCurMilestone();
                if (data != null)
                {
                    Game.Manager.screenPopup.TryQueue(VisualEndNoticePopup.popup, PopupType.Login, data);
                }
                //回收棋盘上未使用的奖励棋子
                var boardReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> boardRewardList);
                var itemsInfo = BoardActivityUtility.CollectAllBoardReward(boardRewardList, World);
                //打点
                DataTracker.event_wish_end_reward.Track(this, itemsInfo);
                //只在有回收奖励时弹脸
                if (boardRewardList.Count > 0)
                    Game.Manager.screenPopup.TryQueue(VisualConvertPopup.popup, PopupType.Login, boardReward);
                else
                    boardReward.Free();
            }
        }

        public bool CheckMilestoneItemCanUse(int id)
        {
            return GetCurMilestone().ItemId == id;
        }
        private List<RewardCommitData> _milestoneRewad = new();
        public void BeginDragReward(Item use, float delay = 0f)
        {
            if (use == null)
                return;
            var board = Valid ? World?.activeBoard : null;
            if (board == null)
                return;
            //要消耗的棋子直接死亡
            if (!board.DisposeItem(use, ItemDeadType.WishBoard))
                return;
            var context = ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.WishBoard);
            context.spawnEffect = this;
            var origin = UIFlyFactory.ResolveFlyTarget(FlyType.WishBoardMilestone);
            var index = 0;
            foreach (var id in GetCurMilestone().DropItem)
            {
                var _delay = delay == 0 ? 0.4f : 1.15f;
                BoardUtility.RegisterSpawnRequest(id, origin, _delay + 0.06f * index);
                var item = World.activeBoard.TrySpawnItem(id, ItemSpawnReason.ActiveSource, context);
                if (item == null)
                {
                    BoardUtility.PopSpawnRequest();
                    var re = Game.Manager.rewardMan.BeginReward(id, 1, ReasonString.wish_bar_reward);
                    _milestoneRewad.Add(re);
                }
                else
                {
                    var __delay = 0.42f + 0.06f * index;
                    Game.Instance.StartCoroutineGlobal(CoPlaySound(__delay));
                }
                if (_milestoneRewad.Count > 0)
                {
                    Game.Instance.StartCoroutineGlobal(CoDelayReward(origin));
                }
                index++;
            }
        }
        private IEnumerator CoPlaySound(float delay)
        {
            yield return new WaitForSeconds(delay);
            Game.Manager.audioMan.TriggerSound("BoardReward");
        }

        private IEnumerator CoDelayReward(Vector3 origin)
        {
            yield return new WaitForSeconds(0.42f);
            UIFlyUtility.FlyRewardList(_milestoneRewad, origin);
            _milestoneRewad.Clear();
        }

        public string order_trail_key = "fat_guide:fx_common_trail.prefab";
        void ISpawnEffectWithTrail.AddTrail(MBItemView view, Tween tween)
        {
            var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.MiddleStatus);
            GameObjectPoolManager.Instance.CreateObject(order_trail_key, effRoot, trail =>
            {
                trail.SetActive(false);
                trail.transform.position = view.transform.position;
                var script = trail.GetOrAddComponent<MBAutoRelease>();
                script.Setup(order_trail_key, 4f);
                trail.transform.Find("particle/eff/glow03").gameObject.SetActive(true);
                if (tween.IsPlaying())
                {
                    var act = tween.onUpdate;
                    tween.OnUpdate(() =>
                    {
                        act?.Invoke();
                        if (!trail.activeSelf) { trail.SetActive(true); }
                        trail.transform.position = view.transform.position;
                    });
                    var act_complete = tween.onComplete;
                    tween.OnComplete(() =>
                    {
                        act_complete?.Invoke();
                        if (trail != null)
                        {
                            // 隐藏head粒子
                            trail.transform.Find("particle/eff/glow03").gameObject.SetActive(false);
                        }
                    });
                }
            });
        }
        #endregion

        #region 图鉴相关
        public EventWishMilestone GetCurMilestone()
        {
            var index = _GetCurUnlockItemMaxLevel() - 1;
            if (index < 0) index = 0;
            return fat.conf.Data.GetEventWishMilestone(GetCurGroupConfig().MilestoneId[index]);
        }

        //图鉴棋子是否解锁
        public bool IsItemUnlock(int itemId)
        {
            return Game.Manager.handbookMan.IsItemUnlocked(itemId);
        }

        public List<int> GetAllItemIdList()
        {
            return _allItemIdList;
        }

        //获取当前已解锁到的棋盘棋子最大等级 等级从0开始 这里的等级涵盖了整条合成链的所有棋子
        public int _GetCurUnlockItemMaxLevel()
        {
            if (!Valid)
                return 0;
            var maxLevel = 0;
            for (var i = 0; i < _allItemIdList.Count; i++)
            {
                var itemId = _allItemIdList[i];
                if (IsItemUnlock(itemId) && maxLevel < i + 1)
                    maxLevel = i + 1;
            }
            return maxLevel;
        }

        //检查是否是棋盘专属棋子
        bool IBoardActivityHandbook.CheckIsBoardItem(int itemId)
        {
            if (!Valid || itemId <= 0) return false;
            return _allItemIdList.Contains(itemId);
        }

        //当棋盘中有新棋子解锁时刷新相关数据(数据层)
        void IBoardActivityHandbook.OnNewItemUnlock()
        {
            if (!Valid) return;
            UnlockMaxLevel = _GetCurUnlockItemMaxLevel();
            //刷新云层信息
            World?.activeBoard?.RefreshCloudInfo(_curDepthIndex, UnlockMaxLevel);
            //检测当前棋盘内是否还有云层遮挡的区域 没有的话会触发下降
            _CheckCanMoveBoard();
            //有新棋子解锁时刷新当前掉落概率
            if (IsEnergyType())
                _spawnBonusHandler.SetDirty();
        }

        //当棋盘中有新棋子解锁时执行相关表现(表现层)
        void IBoardActivityHandbook.OnNewItemShow(Merge.Item itemData)
        {
            if (!Valid) return;
            //只有在解锁的新棋子是棋盘棋子时才发事件并打点
            if (((IBoardActivityHandbook)this).CheckIsBoardItem(itemData.config.Id))
            {
                MessageCenter.Get<MSG.UI_WISH_BOARD_UNLOCK_ITEM>().Dispatch(itemData);

                var groupConfig = GetCurGroupConfig();
                var maxLevel = 0;
                var totalItemNum = _allItemIdList.Count;
                var diff = 0;
                if (Valid && groupConfig != null)
                {
                    for (var i = 0; i < totalItemNum; i++)
                    {
                        var itemId = _allItemIdList[i];
                        if (IsItemUnlock(itemId) && maxLevel < i)
                            maxLevel = i;
                    }
                    diff = groupConfig.Diff;
                }
                var isFinal = maxLevel + 1 == totalItemNum;
                DataTracker.event_wish_gallery_milestone.Track(this, maxLevel + 1, totalItemNum, diff, isFinal, World.activeBoard?.boardId ?? 0, _curDepthIndex);
            }
        }

        private void _RefreshAllItemIdList()
        {
            _allItemIdList.Clear();
            var idList = GetCurGroupConfig()?.MilestoneId;
            if (idList == null)
                return;
            foreach (var id in idList)
            {
                _allItemIdList.AddIfAbsent(fat.conf.Data.GetEventWishMilestone(id).MilestoneItem);
            }
        }

        #endregion
        #region 棋盘下降逻辑

        #region IBoardActivityRowConf

        IList<int> IBoardActivityRowConf.GetRowConfIdList(int detailId)
        {
            var detailConf = Game.Manager.configMan.GetEventWishBoardDetailConfig(detailId);
            return detailConf?.BoardRowId;
        }

        string IBoardActivityRowConf.GetRowConfStr(int rowId)
        {
            var rowConf = Game.Manager.configMan.GetEventWishRowConfig(rowId);
            return rowConf?.DownMiniRow ?? "";
        }

        int IBoardActivityRowConf.GetCycleStartRowId(int detailId)
        {
            return 0;
        }

        #endregion

        //记录当前深度值，初始为棋盘总行数，后续每次棋盘向下延伸，都会加上延伸的行数
        private int _curDepthIndex;
        public int CurDepthIndex => _curDepthIndex;
        //目前是否已检测到棋盘可以下降
        public bool IsReadyToMove => _isReadyToMove;
        private bool _isReadyToMove = false;
        //上次检测到棋盘可以下降时的游戏帧数 避免同一帧内处理后续逻辑
        private int _readyFrameCount = -1;
        //目前数据层是否正在处理棋盘下降逻辑 加此标记位是为了避免处理过程中，因棋子移动、生成等行为，会多次触发_OnBoardItemChange回调导致意料之外的情况发生
        private bool _isBoardMoving = false;
        //缓存棋盘下降回调
        private Action _moveUpAction = null;

        private void _OnBoardItemChange()
        {
            //必须要有  在棋盘棋子状态发生变化时 也要检查一下是否可以移动棋盘  防止错过了新棋子解锁触发棋盘下降的这个时机
            _CheckCanMoveBoard();
            //棋盘棋子发生变化时 检测一下是否有卡死可能性
            CheckBoardExtremeCase();
        }

        //检测到有极限情况时，统一在1秒后处理
        private float _extremeCaseTime = 0;
        //是否正在等待处理卡死
        private bool _isWaitExtremeCase = false;
        //检查当前棋盘是否发生了极限情况
        public void CheckBoardExtremeCase()
        {
            //棋盘上升时和等待处理极限情况时不检查
            if (_isBoardMoving || _isReadyToMove || _isWaitExtremeCase)
                return;
            //检查棋盘是否卡死
            if (!_CheckHasExtremeCase())
                return;
            //如果卡死了 在1s后处理
            _extremeCaseTime = 0;
            _isWaitExtremeCase = true;
        }

        //棋盘下降条件：当前棋盘中显示的云层区域都已解锁
        private void _CheckCanMoveBoard()
        {
            if (_isBoardMoving) return;
            _isReadyToMove = false;
            _moveUpAction = null;
            _readyFrameCount = -1;
            var board = Valid ? World?.activeBoard : null;
            if (board == null)
                return;
            var hasLockCloud = board.CheckHasLockCloud();
            //当前棋盘中显示的云层区域有未解锁的 则return
            if (hasLockCloud)
                return;
            var configMan = Game.Manager.configMan;
            var detailParam = Game.Manager.mergeBoardMan.GetBoardConfig(board.boardId)?.DetailParam ?? 0;
            var boardRowConfIdList = configMan.GetEventWishBoardDetailConfig(detailParam)?.BoardRowId;
            if (boardRowConfIdList == null || boardRowConfIdList.Count < 0)
                return;
            //根据当前记录的深度值查找到当前最顶端的棋盘行配置
            if (boardRowConfIdList.TryGetByIndex(_curDepthIndex - 1, out var rowInfoId))
            {
                var rowConf = configMan.GetEventWishRowConfig(rowInfoId);
                if (rowConf == null)
                    return;
                //读取配置上当前要下降的行数
                var downRowCount = rowConf.DownNum;
                //如果要下降的行数小于等于0 不再下降
                if (downRowCount <= 0)
                    return;
                _readyFrameCount = Time.frameCount;
                _isReadyToMove = true;
                _moveUpAction = () =>
                {
                    _MoveDownBoard(board, downRowCount, detailParam);
                };
            }
        }

        void IActivityUpdate.ActivityUpdate(float deltaTime)
        {
            //棋盘上升相关
            if (_readyFrameCount != -1 && _readyFrameCount != Time.frameCount)
            {
                MessageCenter.Get<MSG.UI_WISH_BOARD_MOVE_UP_READY>().Dispatch();
                _readyFrameCount = -1;
            }
            //棋盘卡死相关
            if (_isWaitExtremeCase)
            {
                _extremeCaseTime += deltaTime;
                if (_extremeCaseTime > 1)
                {
                    _ExecuteExtremeCase();
                    _isWaitExtremeCase = false;
                }
            }
        }

        //外部界面准备好后调用
        public void StartMoveUpBoard()
        {
            if (_isReadyToMove)
            {
                _isReadyToMove = false;
                _isBoardMoving = true;
                _moveUpAction?.Invoke();
                _moveUpAction = null;
                _isBoardMoving = false;
            }
        }

        //棋盘下降一系列流程 先数据层后表现层
        private void _MoveDownBoard(Board board, int downRowCount, int detailParam)
        {
            var mergeBoardMan = Game.Manager.mergeBoardMan;
            var collectItemList = new List<Item>();
            //收集从下往上downRowCount行数范围内所有的棋子将其移到奖励箱
            //collectItemList记录收集的棋子，用于界面表现, 这里并没有清除棋子上原来的坐标信息，表现层可能会用得上
            mergeBoardMan.CollectBoardItemByRow(board, downRowCount, collectItemList, false, true);
            //仅数据层 将从第0行开始到倒数第(1+downRowCount)行为止范围内的所有棋子，整体向下平移到棋盘底部
            mergeBoardMan.MoveDownBoardItem(board, 1 + downRowCount);
            //在从(1+downRowCount)行开始到最后一行为止范围内根据配置创建新的棋子
            using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
            {
                if (BoardActivityUtility.FillBoardRowConfStr(this, detailParam, rowItems, _curDepthIndex, downRowCount))
                {
                    mergeBoardMan.CreateNewBoardItemFromRowToTop(board, rowItems, downRowCount);
                }
            }
            //更新当前深度值
            _curDepthIndex += downRowCount;
            //刷新云层信息
            board.RefreshCloudInfo(_curDepthIndex, UnlockMaxLevel);
            //立即存档
            Game.Manager.archiveMan.SendImmediately(true);
            //整个流程结束 通知界面做棋盘下降表现
            MessageCenter.Get<MSG.UI_WISH_BOARD_MOVE_UP_FINISH>().Dispatch(downRowCount);
        }

        private bool _CheckHasExtremeCase()
        {
            var board = Valid ? World?.activeBoard : null;
            if (board == null)
                return false;
            //棋盘还有空格子时return
            var hasEmptyGrid = board.CheckHasEmptyIdx();
            if (hasEmptyGrid)
                return false;
            //检测棋盘上是否有活跃的bonus棋子 有的话 不算卡死
            var hasBonus = board.CheckHasBonusItem();
            if (hasBonus)
                return false;
            //检查目前棋盘上是否有合成可能
            var checker = BoardViewManager.Instance.checker;
            checker.FindMatch(true);
            //如果还有 说明还没卡死 让玩家继续操作
            if (checker.HasMatchPair())
                return false;
            return true;
        }

        //执行棋盘卡死流程
        private void _ExecuteExtremeCase()
        {
            var board = Valid ? World?.activeBoard : null;
            if (board == null)
                return;
            //再次确认棋盘是否卡死
            if (!_CheckHasExtremeCase())
                return;
            //通知对应棋盘界面开启一段时间的block
            MessageCenter.Get<MSG.UI_WISH_EXTREME_CASE_BLOCK>().Dispatch();
            //确认卡死后弹提示并执行两件事
            Game.Manager.commonTipsMan.ShowPopTips(Toast.NoMerge);
            //1、将场上所有活跃的棋子收到奖励箱
            board.WalkAllItem((item) =>
            {
                if (item.isActive)
                {
                    board.MoveItemToRewardBox(item, true);
                }
            });
            //2、将棋盘最底下两行的所有棋子收进奖励箱
            var boardSize = board.size;
            var cols = boardSize.x;
            var rows = boardSize.y;
            for (int i = 0; i < 2; i++)
            {
                // 根据方向，计算要处理的行
                int row = rows - 1 - i;
                for (int col = 0; col < cols; col++)
                {
                    var item = board.GetItemByCoord(col, row);
                    if (item != null)
                    {
                        board.MoveItemToRewardBox(item, true);
                    }
                }
            }
        }

        #endregion


        #region 产棋子逻辑
        //代币产出类型
        public enum TokenOutputType
        {
            None = 0,   //无法产出
            Energy = 1, //耗体产出
            Order = 2,  //完成订单产出
            All = 3,    //既可以耗体又可以完成订单产出
        }

        //判断目前是否是耗体产出类型
        public bool IsEnergyType()
        {
            return OutputType == TokenOutputType.All || OutputType == TokenOutputType.Energy;
        }

        //判断目前是订单产出类型
        public bool IsOrderType()
        {
            return OutputType == TokenOutputType.All || OutputType == TokenOutputType.Order;
        }

        private void _RefreshTokenOutputType()
        {
            var conf = GetCurGroupConfig();
            if (conf == null || ConfD == null) return;
            //判断当前产出类型
            var isOrder = conf.OrderItemId.Count > 0;
            var isEnergy = conf.DropId.Count > 0;
            if (isOrder && isEnergy)
                OutputType = TokenOutputType.All;
            else if (isEnergy)
                OutputType = TokenOutputType.Energy;
            else if (isOrder)
                OutputType = TokenOutputType.Order;
            else
                OutputType = TokenOutputType.None;
        }

        private WishBoardItemSpawnBonusHandler _spawnBonusHandler;
        private void _RefreshSpawnBonusHandler()
        {
            if (!IsEnergyType())
                return;
            _spawnBonusHandler ??= new WishBoardItemSpawnBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(_spawnBonusHandler);
        }

        private void _ClearSpawnBonusHandler()
        {
            //活动结束时 根据类型决定是否取消注册handler
            if (IsEnergyType())
                Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(_spawnBonusHandler);
            _spawnBonusHandler = null;
        }

        public EventWishDrop GetCurDropConf()
        {
            var confId = _GetCurDropConfId();
            return Game.Manager.configMan.GetEventWishDropConfig(confId);
        }

        private int _GetCurDropConfId()
        {
            var detailConfig = GetCurGroupConfig();
            if (detailConfig == null)
                return 0;
            detailConfig.DropId.TryGetByIndex(UnlockMaxLevel, out var dropId);
            return dropId;
        }
        #endregion

        #region 打点
        public void TrackMineMilestone()
        {
            var curGroupConf = GetCurGroupConfig();
            var allProgressInfo = curGroupConf.BarRewardId;
            var isFinal = _progressPhase == allProgressInfo.Count - 1;
            DataTracker.event_wish_bar.Track(this, _progressPhase + 1, allProgressInfo.Count, curGroupConf.Diff, isFinal, World.activeBoard?.boardId ?? 0, _curDepthIndex);
        }
        #endregion
    }

    public class WishBoardEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => e;
        private readonly ListActivity.Entry e;
        private readonly WishBoardActivity p;
        public WishBoardEntry(ListActivity.Entry e_, WishBoardActivity p_)
        {
            (e, p) = (e_, p_);
            RefreshRedDot();
            MessageCenter.Get<FLY_ICON_FEED_BACK>().AddListener(RefreshRedDot);
        }

        private void RefreshRedDot(FlyableItemSlice slice)
        {
            if (slice.FlyType != FlyType.MergeItemFlyTarget) return;
            RefreshRedDot();
        }

        public void RefreshRedDot()
        {
            e.dot.SetActive(p.GetTokenNum() > 0);
            e.dotCount.gameObject.SetActive(p.GetTokenNum() > 0);
            e.dotCount.SetRedPoint(p.GetTokenNum());
        }

        public override void Clear(ListActivity.Entry e_)
        {
            MessageCenter.Get<FLY_ICON_FEED_BACK>().RemoveListener(RefreshRedDot);
        }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }

    }
}
