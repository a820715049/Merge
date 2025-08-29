/*
 * @Author: tang.yan
 * @Description: 矿车棋盘活动数据类
 * @Doc: https://centurygames.feishu.cn/wiki/DI62wtyF1iYnHTk9xA1cXHgrn4g
 * @Date: 2025-07-18 11:07:23
 */

using System;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using EL.Resource;
using FAT.Merge;
using UnityEngine;
using static FAT.RecordStateHelper;
using DG.Tweening;
using UnityEngine.UI.Extensions;
using System.Collections;

namespace FAT
{
    public class MineCartActivity : ActivityLike, IBoardEntry, IBoardArchive, IActivityUpdate,
        IActivityOrderHandler, IExternalOutput, ISpawnEffectWithTrail,
        IBoardActivityHandbook, IBoardActivityRowConf, IBoardMoveAdapter, IBoardExtremeAdapter
    {
        public override bool Valid => Lite.Valid && ConfD != null;
        public MergeWorld World { get; private set; }   //世界实体
        public MergeWorldTracer WorldTracer { get; private set; }   //世界实体追踪器
        public EventMineCart ConfD { get; private set; }
        //用户分层 区别棋盘配置 对应EventMineCartDetail.id
        public int DetailId { get; private set; }
        //从0开始 表示当前合成链中已解锁的最大等级棋子 如：0表示什么都没解锁  3代表当前已解锁合成链中第3个棋子
        public int UnlockMaxLevel { get; private set; }
        //当前棋子产出类型
        public ItemOutputType OutputType { get; private set; } = ItemOutputType.None;

        #region 活动基础

        //外部调用需判空
        public EventMineCartDetail GetCurDetailConfig()
        {
            return Game.Manager.configMan.GetEventMineCartDetailConfig(DetailId);
        }

        public MineCartActivity(ActivityLite lite_)
        {
            Lite = lite_;
            ConfD = Game.Manager.configMan.GetEventMineCartConfig(lite_.Param);
        }

        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            DetailId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.Detail);
            //初始化棋盘 只在活动创建时走一次
            _InitBoardData();
            //添加初始免费棋子到奖励箱
            _InitStartItem();
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新棋子产出类型
            _RefreshItemOutputType();
            //刷新耗体产出棋子模块
            _RefreshSpawnBonusHandler();
            //创建棋盘移动处理模块
            _InitBoardMoveHandler();
            //创建棋盘卡死处理模块
            _InitBoardExtremeHandler();
            //刷新当前回合相关信息
            _RefreshRoundInfo();
            //活动首次开启时弹脸
            StartPopup.Popup();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, DetailId));
            any.Add(ToRecord(1, RoundIndex));
            any.Add(ToRecord(2, UnlockMaxLevel));
            any.Add(ToRecord(3, MilestoneNum));
            any.Add(ToRecord(4, _milestonePhase));
            any.Add(ToRecord(5, _curDepthIndex));
            any.Add(ToRecord(6, HasPlayedEnterAnimation));
            any.Add(ToRecord(7, NeedShowHandbookRedDot));
            any.Add(ToRecord(8, PlayedHandbookBanner));
            any.Add(ToRecord(9, BaseMilestoneNum));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            DetailId = ReadInt(0, any);
            RoundIndex = ReadInt(1, any);
            UnlockMaxLevel = ReadInt(2, any);
            MilestoneNum = ReadInt(3, any);
            _milestonePhase = ReadInt(4, any);
            _curDepthIndex = ReadInt(5, any);
            HasPlayedEnterAnimation = ReadBool(6, any);
            NeedShowHandbookRedDot = ReadBool(7, any);
            PlayedHandbookBanner = ReadBool(8, any);
            BaseMilestoneNum = ReadInt(9, any);
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新图鉴棋子信息
            _RefreshAllItemIdList();
            //刷新棋子产出类型
            _RefreshItemOutputType();
            //刷新当前回合相关信息
            _RefreshRoundInfo();
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach (var v in StartPopup.ResEnumerate()) yield return v;
            foreach (var v in Visual.ResEnumerate()) yield return v;
        }

        public override void WhenEnd()
        {
            var conf = GetCurDetailConfig();
            if (conf != null)
            {
                Game.Manager.screenPopup.TryQueue(EndPopup.popup, (PopupType)(-1));

                // 回收棋盘上未使用的奖励棋子
                var boardReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> boardRewardList);
                var itemsInfo = BoardActivityUtility.CollectAllBoardReward(boardRewardList, World);
                //打点
                DataTracker.event_minecart_end_reward.Track(this, itemsInfo);
                // 仅在有回收奖励时弹补领弹窗
                if (boardRewardList.Count > 0)
                    Game.Manager.screenPopup.TryQueue(ConvertPopup.popup, (PopupType)(-1), boardReward);
                else
                    boardReward.Free();
            }
            //清理棋盘相关数据
            _ClearBoardData();
        }

        #region 界面 入口 换皮 弹脸

        public override ActivityVisual Visual => StartPopup.visual;

        public VisualRes VisualBoard { get; } = new(UIConfig.UIMineCartBoardMain); //棋盘UI
        public VisualRes VisualLoading { get; } = new(UIConfig.UIMineCartLoading);
        public VisualRes VisualHelp { get; } = new(UIConfig.UIMineCartBoardHelp);
        public VisualRes VisualBanner { get; } = new(UIConfig.UIMineCartBoardBannerTip);
        public VisualRes VisualMilestoneReward { get; } = new(UIConfig.UIMineCartBoardMilestoneReward);
        public VisualRes VisualHandbook { get; } = new(UIConfig.UIMineCartHandbook);
        public VisualRes VisualRewardTips { get; } = new(UIConfig.UIMineCartRewardTips);
        // 弹脸
        public VisualPopup StartPopup { get; } = new(UIConfig.UIMineCartBoardStartNotice); //活动开启theme
        public VisualPopup EndPopup { get; } = new(UIConfig.UIMineCartBoardEndNotice); //活动结束
        public VisualPopup ConvertPopup { get; } = new(UIConfig.UIMineCartBoardReplacement); //补领

        public override void Open()
        {
            ActivityTransit.Enter(this, VisualLoading, VisualBoard.res);
        }

        public void Close()
        {
            ActivityTransit.Exit(this, VisualBoard.res.ActiveR);
        }

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            StartPopup.Setup(ConfD.EventTheme, this);
            EndPopup.Setup(ConfD.EndTheme, this, false, false);
            ConvertPopup.Setup(ConfD.EndRewardTheme, this, false, false);

            VisualBoard.Setup(ConfD.BoardTheme);
            VisualHelp.Setup(ConfD.HelpTheme);
            VisualLoading.Setup(ConfD.LoadingTheme);
            VisualHandbook.Setup(ConfD.BookTheme);
            VisualBanner.Setup(ConfD.BannerTheme);
        }

        string IBoardEntry.BoardEntryAsset()
        {
            StartPopup.visual.AssetMap.TryGetValue("boardEntry", out var key);
            return key;
        }
        //是否已播放过入场动画
        public bool HasPlayedEnterAnimation = false;
        public bool NeedShowHandbookRedDot = false;
        public bool PlayedHandbookBanner = false;
        //大奖领奖完成后可以播放回合结束引导，这个参数不用持久化
        public bool CanPlayFinishRoundGuide = false;

        #endregion

        #endregion

        #region 图鉴相关

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
        private int _GetCurUnlockItemMaxLevel()
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
        }

        //当棋盘中有新棋子解锁时执行相关表现(表现层)
        void IBoardActivityHandbook.OnNewItemShow(Merge.Item itemData)
        {
            if (!Valid) return;
            //只有在解锁的新棋子是棋盘棋子时才发事件并打点
            if (((IBoardActivityHandbook)this).CheckIsBoardItem(itemData.config.Id))
            {
                MessageCenter.Get<MSG.UI_MINECART_BOARD_UNLOCK_ITEM>().Dispatch(itemData);
                NeedShowHandbookRedDot = true;
                //打点
                var totalCount = _allItemIdList.Count;
                var isFinal = UnlockMaxLevel >= totalCount;
                DataTracker.event_minecart_gallery.Track(this, UnlockMaxLevel, totalCount, GetCurDetailConfig()?.Diff ?? 0, isFinal, World.activeBoard?.boardId ?? 0, _curDepthIndex, RoundIndex + 1);
            }
        }

        private List<int> _allItemIdList = new List<int>();    //当前活动主链条的所有棋子idList 按等级由小到大排序

        private void _RefreshAllItemIdList()
        {
            _allItemIdList.Clear();
            var idList = ConfD?.HandBook;
            if (idList == null)
                return;
            foreach (var id in idList)
            {
                _allItemIdList.AddIfAbsent(id);
            }
        }

        #endregion

        #region 棋盘相关逻辑

        #region 基础创建

        FeatureEntry IBoardArchive.Feature => FeatureEntry.FeatureMineCart;

        //此接口在活动第一次创建时不会走到，创建后每次都会走,且时机在LoadSetup之后
        void IBoardArchive.SetBoardData(fat.gamekitdata.Merge data)
        {
            if (data == null)
                return;
            _InitWorld(data.BoardId, false);
            World.Deserialize(data, null);
            //刷新耗体产出棋子模块
            _RefreshSpawnBonusHandler();
            //创建棋盘移动处理模块
            _InitBoardMoveHandler();
            //创建棋盘卡死处理模块
            _InitBoardExtremeHandler();
        }

        void IBoardArchive.FillBoardData(fat.gamekitdata.Merge data)
        {
            World?.Serialize(data);
        }

        //初始化棋盘 只在活动创建时走一次
        private void _InitBoardData()
        {
            _allItemIdList.Clear();
            var infoConfig = GetCurDetailConfig();
            if (infoConfig == null || ConfD == null)
                return;
            //初始化图鉴棋子list
            foreach (var id in ConfD.HandBook)
            {
                _allItemIdList.AddIfAbsent(id);
            }
            //每次活动创建时都清空一下图鉴系统中的解锁信息 以防活动异常结束导致图鉴没有锁定
            Game.Manager.handbookMan.LockHandbookItem(_allItemIdList);
            //第一次初始化棋盘
            _InitWorld(infoConfig.BoardId, true);
            var board = World.activeBoard;
            //初始化棋盘深度值
            _curDepthIndex = board.size.y;
        }

        private void _InitWorld(int boardId, bool isFirstCreate)
        {
            World = new MergeWorld();
            WorldTracer = new MergeWorldTracer(_OnBoardItemChange, null);
            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry()
            {
                world = World,
                type = MergeWorldEntry.EntryType.MineCartBoard,
            });
            WorldTracer.Bind(World);
            World.BindTracer(WorldTracer);
            //棋盘不需要背包 也没有订单 和底部信息栏
            // World.BindOrderHelper(Game.Manager.mainOrderMan.curOrderHelper);
            Game.Manager.mergeBoardMan.InitializeBoard(this, World, boardId, isFirstCreate);
            //注册棋盘活动代理 用于调动IExternalOutput
            World.RegisterActivityHandler(this);
        }

        private void _ClearBoardData()
        {
            //清理handler
            _ClearSpawnBonusHandler();
            //取消注册棋盘活动代理
            World?.UnregisterActivityHandler(this);
            //清空handler
            _boardMoveHandler = null;
            _boardExtremeHandler = null;
            //活动结束时将关联棋子的图鉴置为锁定状态
            Game.Manager.handbookMan.LockHandbookItem(_allItemIdList);
            //取消注册并清理当前world
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            World = null;
            WorldTracer = null;
        }

        #endregion

        #region 棋盘下降及卡死相关处理逻辑

        private BoardMoveHandler _boardMoveHandler;
        //创建棋盘移动处理模块
        private void _InitBoardMoveHandler()
        {
            //这里确保_curDepthIndex为最新
            _boardMoveHandler = new BoardMoveHandler(this, BoardMoveHandler.BoardMoveType.CycleUp, _curDepthIndex);
        }

        private BoardExtremeHandler _boardExtremeHandler;
        //创建棋盘卡死处理模块
        private void _InitBoardExtremeHandler()
        {
            _boardExtremeHandler = new BoardExtremeHandler(this, BoardExtremeHandler.BoardExtremeType.CycleUp);
        }

        #region IBoardActivityRowConf

        IList<int> IBoardActivityRowConf.GetRowConfIdList(int detailId)
        {
            var detailConf = Game.Manager.configMan.GetEventMineCartRowGrpConfig(detailId);
            return detailConf?.BoardRowId;
        }

        string IBoardActivityRowConf.GetRowConfStr(int rowId)
        {
            var rowConf = Game.Manager.configMan.GetEventMineCartRowConfig(rowId);
            return rowConf?.RowDetail ?? "";
        }

        int IBoardActivityRowConf.GetCycleStartRowId(int detailId)
        {
            var detailConf = Game.Manager.configMan.GetEventMineCartRowGrpConfig(detailId);
            return detailConf?.CycleRowStart ?? 0;
        }

        #endregion

        #region IBoardMoveAdapter

        int IBoardMoveAdapter.GetMoveNeedRowCount(int detailId)
        {
            var detailConf = Game.Manager.configMan.GetEventMineCartRowGrpConfig(detailId);
            return detailConf?.RowUpCount ?? 0;
        }

        int IBoardMoveAdapter.GetMoveCountByRowId(int rowId)
        {
            return 0;
        }

        Board IBoardMoveAdapter.GetBoard()
        {
            return Valid ? World?.activeBoard : null;
        }

        //记录当前深度值，初始为棋盘总行数，后续每次棋盘向下延伸，都会加上延伸的行数
        private int _curDepthIndex;
        void IBoardMoveAdapter.OnDepthIndexUpdate(int newDepth)
        {
            _curDepthIndex = newDepth;
        }

        #endregion

        #region IBoardExtremeAdapter

        Board IBoardExtremeAdapter.GetBoard()
        {
            return Valid ? World?.activeBoard : null;
        }

        bool IBoardExtremeAdapter.CanCheckExtreme()
        {
            return _boardMoveHandler == null || !_boardMoveHandler.IsBoardMoving();
        }

        #endregion

        //UI在做完棋盘移动表现后主动调用此方法，用于检查棋盘是否卡死
        public void CheckBoardExtremeCase()
        {
            _boardExtremeHandler?.CheckBoardExtremeCase();
        }

        private void _OnBoardItemChange()
        {
            _boardMoveHandler?.OnBoardItemChange();
            _boardExtremeHandler?.OnBoardItemChange();
        }

        void IActivityUpdate.ActivityUpdate(float deltaTime)
        {
            _boardMoveHandler?.OnActivityUpdate(deltaTime);
            _boardExtremeHandler?.OnActivityUpdate(deltaTime);
        }

        #endregion

        #endregion

        #region 活动棋子产出相关

        //活动棋子产出类型
        public enum ItemOutputType
        {
            None = 0,   //无法产出
            Energy = 1, //耗体产出
            Order = 2,  //完成订单产出
            All = 3,    //既可以耗体又可以完成订单产出
        }

        //判断目前是否是耗体产出类型
        public bool IsEnergyType()
        {
            return OutputType == ItemOutputType.All || OutputType == ItemOutputType.Energy;
        }

        //判断目前是订单产出类型
        public bool IsOrderType()
        {
            return OutputType == ItemOutputType.All || OutputType == ItemOutputType.Order;
        }

        private void _RefreshItemOutputType()
        {
            var conf = GetCurDetailConfig();
            if (conf == null || ConfD == null) return;
            //判断当前产出类型
            var isOrder = conf.OrderItem.Count > 0;
            var isEnergy = conf.DropId.Count > 0;
            if (isOrder && isEnergy)
                OutputType = ItemOutputType.All;
            else if (isEnergy)
                OutputType = ItemOutputType.Energy;
            else if (isOrder)
                OutputType = ItemOutputType.Order;
            else
                OutputType = ItemOutputType.None;
        }

        //根据配置添加初始免费棋子到奖励箱
        private void _InitStartItem()
        {
            var conf = GetCurDetailConfig();
            if (conf == null || ConfD == null) return;
            var rewardMan = Game.Manager.rewardMan;
            rewardMan.PushContext(new RewardContext() { targetWorld = World });
            foreach (var itemStr in conf.FreeItem)
            {
                var r = itemStr.ConvertToRewardConfig();
                if (r != null)
                {
                    rewardMan.CommitReward(rewardMan.BeginReward(r.Id, r.Count, ReasonString.mine_cart_start));
                }
            }
            rewardMan.PopContext();
        }

        #region 主棋盘订单右下角奖励

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (!IsOrderType())
                return false;
            //心想事成订单不给奖励
            if ((order as IOrderData).IsMagicHour)
                return false;
            var changed = false;
            var state = order.GetState((int)OrderParamType.ScoreEventIdBR);
            // 没有积分 or 不是同一期活动时给这个订单生成右下角积分
            if (state == null || state.Value != Id)
            {
                var orderPayDiff = order.GetValue(OrderParamType.PayDifficulty);
                if (GetOrderRewardBR(orderPayDiff, out var rewardId, out var rewardNum))
                {
                    changed = true;
                    OrderAttachmentUtility.slot_score_br.UpdateScoreDataBR(order, Id, rewardNum, rewardId);
                }
            }
            return changed;
        }

        //活动内部根据配置自行决定订单右下角的奖励
        private bool GetOrderRewardBR(int diff, out int rewardId, out int rewardNum)
        {
            rewardId = 0;
            rewardNum = 0;
            var groupConfig = GetCurDetailConfig();
            if (groupConfig == null || ConfD == null)
                return false;
            var configMan = Game.Manager.configMan;
            foreach (var id in groupConfig.OrderItem)
            {
                var conf = configMan.GetEventMineCartOrderItemConfig(id);
                if (conf == null || diff < conf.PayDifficult) continue;
                rewardId = conf.ItemId;
                rewardNum = diff / conf.Base;
                //奖励数量至少为1
                if (rewardNum <= 0)
                {
                    rewardNum = 1;
                }
            }
            return rewardId > 0 && rewardNum > 0;
        }

        //完成订单获得棋子奖励时打点
        public void TrackOrderGetItem(int rewardId, int rewardCount, int payDiff)
        {
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return;
            var curBoardId = Game.Manager.mergeBoardMan.activeWorld?.activeBoard?.boardId ?? 0;
            DataTracker.event_minecart_getitem_order.Track(this, detailConf.Diff, curBoardId, _curDepthIndex, RoundIndex + 1, rewardId, ItemUtility.GetItemLevel(rewardId), rewardCount, payDiff);
        }

        #endregion

        #region 点击耗体生成器产棋子逻辑

        private MineCartItemSpawnBonusHandler _spawnBonusHandler;

        private void _RefreshSpawnBonusHandler()
        {
            if (!IsEnergyType())
                return;
            _spawnBonusHandler ??= new MineCartItemSpawnBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(_spawnBonusHandler);
        }

        private void _ClearSpawnBonusHandler()
        {
            //活动结束时 根据类型决定是否取消注册handler
            if (IsEnergyType())
                Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(_spawnBonusHandler);
            _spawnBonusHandler = null;
        }

        //根据当前回合数刷新当前掉落概率
        private void _SetBonusHandlerDirty()
        {
            if (IsEnergyType())
                _spawnBonusHandler?.SetDirty();
        }

        public EventMineCartDrop GetCurDropConf()
        {
            var confId = _GetCurDropConfId();
            return Game.Manager.configMan.GetEventMineCartDropConfig(confId);
        }

        //根据当前累计进行的回合数，获取当前已解锁到的棋盘掉落信息id (EventMineCartDrop.id)
        private int _GetCurDropConfId()
        {
            var detailConfig = GetCurDetailConfig();
            if (detailConfig == null)
                return 0;
            var totalCount = detailConfig.DropId.Count;
            if (totalCount <= 0)
                return 0;
            //掉落信息与当前累计进行的回合数绑定 超过配置的掉落信息个数时，默认使用最后一个配置
            if (detailConfig.DropId.TryGetByIndex(RoundIndex, out var dropId))
            {
                return dropId;
            }
            else if (RoundIndex >= totalCount - 1)
            {
                return detailConfig.DropId[^1];
            }
            else
            {
                return detailConfig.DropId[0];
            }
        }

        //点击耗体生成器获得棋子奖励时打点
        public void TrackBonusGetItem(int rewardId, int rewardCount)
        {
            var detailConf = GetCurDetailConfig();
            if (detailConf == null)
                return;
            var curBoardId = Game.Manager.mergeBoardMan.activeWorld?.activeBoard?.boardId ?? 0;
            DataTracker.event_minecart_getitem_tap.Track(this, detailConf.Diff, curBoardId, _curDepthIndex, RoundIndex + 1, rewardId, ItemUtility.GetItemLevel(rewardId), rewardCount);
        }

        #endregion

        #endregion

        #region 回合及里程碑逻辑+对应的发奖逻辑

        //当前一共进行了多少个回合 从0开始 会根据这个值取到当前真正的回合id
        public int RoundIndex { get; private set; } = 0;
        //当前回合的里程碑进度值
        public int MilestoneNum { get; private set; } = 0;
        public int BaseMilestoneNum { get; private set; } = 0;
        //当前里程碑所处的阶段 从0开始 根据阶段值读配置获取当前的最大进度以及达成后可获得的奖励
        private int _milestonePhase = 0;

        //调用时判空  获取当前回合的配置信息
        public EventMineCartRound GetCurRoundConfig()
        {
            return Game.Manager.configMan.GetEventMineCartRoundConfig(GetCurRoundConfigId());
        }

        //获取当前回合的配置id
        public int GetCurRoundConfigId()
        {
            if (!Valid)
                return 0;
            var detailConfig = GetCurDetailConfig();
            if (detailConfig == null || RoundIndex < 0)
                return 0;
            var roundIdList = detailConfig.RoundId;
            var total = roundIdList.Count;
            //还没走完第一次
            if (RoundIndex < total)
                return roundIdList[RoundIndex];
            //超出第一次 则查看有无循环
            var loopStartIndex = roundIdList.IndexOf(detailConfig.CycleRound);
            if (loopStartIndex < 0)
            {
                //无循环时 默认返回列表中最后一个
                return roundIdList[^1];
            }
            else
            {
                //有循环时 计算
                var cycleLength = total - loopStartIndex;   //确定循环段的长度
                var offset = RoundIndex - loopStartIndex;   //计算从“第一次到达循环起点”之后，又走过了多少步
                var idxInCycle = offset % cycleLength;     //用取余实现循环
                return roundIdList[loopStartIndex + idxInCycle];    //用取余实现循环
            }
        }

        //活动开始/跨回合时 刷新当前回合的相关信息
        private void _RefreshRoundInfo(bool needAdd = false)
        {
            if (needAdd)
                RoundIndex++;
            _RefreshUseItemRewardPool();
            //根据当前回合数刷新当前掉落概率
            _SetBonusHandlerDirty();
        }

        #endregion

        #region 活动棋子使用及发奖逻辑

        #region IExternalOutput 活动棋子使用及产出

        bool IExternalOutput.CanUseItem(Item source)
        {
            if (!Valid)
                return false;
            foreach (var id in ConfD.SpecialItem)
            {
                if (id == source.tid)
                    return true;
            }
            return false;
        }

        //尝试使用活动进度棋子
        bool IExternalOutput.TrySpawnItem(Item source, out int outputId, out ItemSpawnContext context)
        {
            outputId = -1;
            context = null;
            var comp = source.GetItemComponent<ItemActiveSourceComponent>();
            //棋子使用一次就得死亡
            if (!comp.WillDead)
                return false;
            _OnUseItem(source, comp);
            return true;
        }

        #endregion

        //检查棋盘上目前是否有对应活动棋子
        public bool HasSpecialItem()
        {
            if (!Valid)
                return false;
            var viewManager = BoardViewManager.Instance;
            foreach (var id in ConfD.SpecialItem)
            {
                if (viewManager.HasActiveItem(id, 1))
                    return true;
            }
            return false;
        }

        //处理使用活动棋子时的一系列逻辑
        private void _OnUseItem(Item source, ItemActiveSourceComponent comp)
        {
            //1.直接原地飞棋子 飞到上方矿车处
            var to = UIFlyFactory.ResolveFlyTarget(FlyType.MineCartUseItem);
            UIFlyUtility.FlyCustom(source.tid, 1, BoardUtility.GetWorldPosByCoord(source.coord), to, FlyStyle.Common, FlyType.MineCartUseItem, size: 136f);

            //2.从矿车位置向棋盘上发棋子使用后得到的奖励 棋盘满了时直接发到奖励箱
            _BeginUseItemReward();

            //3.处理活动棋子自身对应的里程碑进度值 进而推动里程碑发奖 回合大奖 回合数递进等
            _TryAddMilestoneNum(comp.DropCount);
        }

        //使用棋子时打点 因为要呈现出里程碑进度 所以穿插到里程碑进度逻辑中打
        private void _TrackUseItem(int milestonePhase)
        {
            //打点
            var totalNum = GetCurRoundConfig()?.MilestoneScore.Count ?? 0;
            var diff = GetCurDetailConfig()?.Diff ?? 0;
            var boardId = World?.activeBoard?.boardId ?? 0;
            DataTracker.event_minecart_foward.Track(this, milestonePhase + 1, totalNum, diff, boardId, _curDepthIndex, RoundIndex + 1, MilestoneNum);
        }

        #region 2.从矿车位置向棋盘上发棋子使用后得到的奖励 棋盘满了时直接发到奖励箱

        //活动棋子奖池  会在使用活动棋子时从中随机 每次随机到1个棋子id 会随机_randomCount次
        private List<(int itemId, int weight)> _useItemRewardPool = new();
        private int _randomCount = 0;   //从活动棋子奖池中随机几次
        private List<RewardCommitData> _useItemRewards = new(); //一次发奖行为中因棋盘满了 导致需要发到奖励箱的奖励

        //刷新活动棋子使用时的奖池
        private void _RefreshUseItemRewardPool()
        {
            var curRoundConf = GetCurRoundConfig();
            if (curRoundConf == null)
                return;
            //刷新奖池
            _useItemRewardPool.Clear();
            foreach (var reward in curRoundConf.BaseReward)
            {
                var (id, weight, _) = reward.ConvertToInt3();
                _useItemRewardPool.Add((id, weight));
            }
            //刷新随机次数
            _randomCount = curRoundConf.BaseCount;
        }

        private static float _flyDelayTime = 1f;
        private static float _flyIntervalTime = 0.06f;
        //从矿车位置向棋盘上发棋子使用后得到的奖励 棋盘满了时直接发到奖励箱
        private void _BeginUseItemReward()
        {
            _useItemRewards.Clear();
            var board = Valid ? World?.activeBoard : null;
            if (board == null)
                return;
            var context = ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.MineCart);
            context.spawnEffect = this;
            var origin = UIFlyFactory.ResolveFlyTarget(FlyType.MineCartGetItemReward);
            for (var index = 0; index < _randomCount; index++)
            {
                var itemId = _useItemRewardPool.RandomChooseByWeight(e => e.weight).itemId;
                if (itemId <= 0)
                    continue;
                BoardUtility.RegisterSpawnRequest(itemId, origin, _flyDelayTime + _flyIntervalTime * index);
                var item = board.TrySpawnItem(itemId, ItemSpawnReason.ActiveSource, context);
                if (item == null)
                {
                    BoardUtility.PopSpawnRequest();
                    var re = Game.Manager.rewardMan.BeginReward(itemId, 1, ReasonString.mine_cart_use_item);
                    _useItemRewards.Add(re);
                }
                else
                {
                    var delay = _flyDelayTime + _flyIntervalTime * index;
                    Game.Instance.StartCoroutineGlobal(CoPlaySound(delay, "BoardReward"));
                }
            }

            if (_useItemRewards.Count > 0)
            {
                Game.Instance.StartCoroutineGlobal(CoDelayReward(_flyDelayTime, origin, _useItemRewards));
            }
        }

        private IEnumerator CoPlaySound(float delay, string soundName)
        {
            yield return new WaitForSeconds(delay);
            Game.Manager.audioMan.TriggerSound(soundName);
        }

        private IEnumerator CoDelayReward(float delay, Vector3 origin, List<RewardCommitData> rewardList)
        {
            yield return new WaitForSeconds(delay);
            UIFlyUtility.FlyRewardList(rewardList, origin);
        }

        #endregion

        #region 3.处理活动棋子自身对应的里程碑进度值 进而推动里程碑发奖 回合大奖 回合数递进等

        //增加当前里程碑进度值
        private void _TryAddMilestoneNum(int addNum)
        {
            //获取当前的回合信息
            var curRoundConf = GetCurRoundConfig();
            if (curRoundConf == null)
                return;
            // 1. 记录旧状态
            int oldPhase = _milestonePhase;
            int totalPhases = curRoundConf.MilestoneScore.Count;
            int oldProgress = MilestoneNum;
            // 2. 计算并更新新进度
            int newProgress = oldProgress + addNum;
            MilestoneNum = newProgress;
            // 3. 计算 newPhase
            int newPhase = 0;
            for (int i = 0; i < totalPhases; i++)
            {
                //有多少阈值 ≤ newProgress
                if (newProgress >= curRoundConf.MilestoneScore[i])
                    newPhase++;
                else
                    break;
            }
            //使用棋子时打点 因为要呈现出里程碑进度 所以穿插到里程碑进度逻辑中打
            var trackPhase = newPhase < totalPhases - 1 ? newPhase : totalPhases - 1;
            _TrackUseItem(trackPhase);
            // 4. 没越过任何阈值，只更新进度条
            if (newPhase == oldPhase)
            {
                MessageCenter.Get<MSG.GAME_MINECART_BOARD_PROG_CHANGE>().Dispatch(newProgress, -1, default);
                return;
            }
            // 5. 如果越过一个或多个里程碑，逐一发奖 目前从配置上来说，不会有一下达成多了里程碑的情况
            for (int phaseIndex = oldPhase; phaseIndex < newPhase; phaseIndex++)
            {
                // 普通阶段奖
                if (phaseIndex < curRoundConf.MilestoneReward.Count)
                {
                    //先通知表现层 然后等表现层调接口
                    MessageCenter.Get<MSG.GAME_MINECART_BOARD_PROG_CHANGE>().Dispatch(newProgress, _milestonePhase, default);
                }
                // 最后一个阶段的「大奖」
                else if (oldPhase < totalPhases && phaseIndex == totalPhases - 1)
                {
                    var listT = PoolMapping.PoolMappingAccess.Take<List<RewardCommitData>>(out var list);
                    foreach (var r in curRoundConf.RoundReward)
                    {
                        var reward = r.ConvertToRewardConfig();
                        if (reward != null)
                        {
                            var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.mine_cart_round_reward);
                            list.Add(commit);
                        }
                    }
                    //通知界面发大奖
                    MessageCenter.Get<MSG.GAME_MINECART_BOARD_PROG_CHANGE>().Dispatch(newProgress, -1, listT);
                    //获得回合大奖时打点
                    var detailConfig = GetCurDetailConfig();
                    if (detailConfig != null)
                    {
                        var roundId = curRoundConf.Id;
                        var index = detailConfig.RoundId.IndexOf(roundId) + 1;
                        var totalIndex = detailConfig.RoundId.Count;
                        var diff = detailConfig.Diff;
                        var isFinal = index == totalIndex;
                        var boardId = World?.activeBoard?.boardId ?? 0;
                        DataTracker.event_minecart_reward.Track(this, index, totalIndex, diff, isFinal, roundId, boardId, _curDepthIndex, RoundIndex + 1);
                    }
                    //当前回合结束，重置
                    _milestonePhase = 0;
                    BaseMilestoneNum += MilestoneNum;
                    MilestoneNum = 0;
                    //回合数递进 + 刷新数据
                    _RefreshRoundInfo(true);
                    //返回
                    return;
                }
            }
            // 6. 更新阶段索引到 newPhase
            _milestonePhase = newPhase;
        }

        //发里程碑阶段奖励时使用的奖池 发奖时从中随机 每次随机到1个棋子id 会随机rewardCount次
        private List<(int itemId, int weight)> _milestoneRewardPool = new();
        //表现层调用领取并播放发阶段奖励的过程
        //milestonePhase为通过事件GAME_MINECART_BOARD_PROG_CHANGE传给表现层的第二个参数
        //delayTime为延迟发棋子的时间 表现为矿车刚好装到奖励气泡
        public void TryClaimMilestoneReward(int milestonePhase, float delayTime)
        {
            if (milestonePhase < 0 || !Valid)
                return;
            var curRoundConf = GetCurRoundConfig();
            if (curRoundConf == null)
                return;
            var configMan = Game.Manager.configMan;
            //刷新奖池
            _milestoneRewardPool.Clear();
            if (!curRoundConf.MilestoneReward.TryGetByIndex(milestonePhase, out var rewardConfId))
                return;
            var conf = configMan.GetEventMineCartRewardConfig(rewardConfId);
            if (conf == null)
                return;
            foreach (var pool in conf.RewardPool)
            {
                var (id, weight, _) = pool.ConvertToInt3();
                _milestoneRewardPool.Add((id, weight));
            }
            //发奖 这里默认发的都是棋子 都会尝试往棋盘上发 棋盘格子不足时发到奖励箱
            _BeginMilestoneReward(conf.RewardCount, delayTime);
        }

        //一次发奖行为中因棋盘满了 导致需要发到奖励箱的奖励
        private List<RewardCommitData> _milestoneRewards = new();
        //发奖 这里默认发的都是棋子 都会尝试往棋盘上发 棋盘格子不足时发到奖励箱
        private void _BeginMilestoneReward(int rewardCount, float delayTime)
        {
            _milestoneRewards.Clear();
            var board = Valid ? World?.activeBoard : null;
            if (board == null)
                return;
            var context = ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.MineCart);
            context.spawnEffect = this;
            var origin = UIFlyFactory.ResolveFlyTarget(FlyType.MineCartRewardBubble);
            for (var index = 0; index < rewardCount; index++)
            {
                var itemId = _milestoneRewardPool.RandomChooseByWeight(e => e.weight).itemId;
                if (itemId <= 0)
                    continue;
                BoardUtility.RegisterSpawnRequest(itemId, origin, delayTime + _flyIntervalTime * index);
                var item = board.TrySpawnItem(itemId, ItemSpawnReason.ActiveSource, context);
                if (item == null)
                {
                    BoardUtility.PopSpawnRequest();
                    var re = Game.Manager.rewardMan.BeginReward(itemId, 1, ReasonString.mine_cart_milestone_reward);
                    _milestoneRewards.Add(re);
                }
                else
                {
                    var delay = delayTime + _flyIntervalTime * index;
                    Game.Instance.StartCoroutineGlobal(CoPlaySound(delay, "BoardReward"));
                }
            }

            if (_milestoneRewards.Count > 0)
            {
                Game.Instance.StartCoroutineGlobal(CoDelayReward(delayTime, origin, _milestoneRewards));
            }
        }

        #endregion

        #region ISpawnEffectWithTrail 棋子拖尾

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

        #endregion
    }

    public class MineCartEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => entry;
        private readonly ListActivity.Entry entry;
        private readonly MineCartActivity activity;

        public MineCartEntry(ListActivity.Entry _entry, MineCartActivity _activity)
        {
            (entry, this.activity) = (_entry, _activity);
            _entry.dot.SetActive(this.activity.World.rewardCount > 0);
            _entry.dotCount.gameObject.SetActive(this.activity.World.rewardCount > 0);
            _entry.dotCount.SetRedPoint(this.activity.World.rewardCount);
        }
        public void RefreshDot(MineCartActivity activity)
        {
            if (activity != this.activity) return;
            entry.dot.SetActive(this.activity.World.rewardCount > 0);
            entry.dotCount.gameObject.SetActive(this.activity.World.rewardCount > 0);
            entry.dotCount.SetRedPoint(this.activity.World.rewardCount);
        }
        public override void Clear(ListActivity.Entry e_) { }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }
    }
}
