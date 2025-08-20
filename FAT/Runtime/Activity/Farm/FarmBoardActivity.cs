/*
 * @Author: tang.yan
 * @Description: 农场棋盘活动数据类 
 * @Doc: https://centurygames.feishu.cn/wiki/VbtUw5VNhibluVkq2NncdPPjnIb
 * @Date: 2025-05-08 18:05:15
 */

using System;
using System.Collections.Generic;
using Config;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using EL.Resource;
using FAT.Merge;
using UnityEngine;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class FarmBoardActivity : ActivityLike, IBoardEntry, IBoardArchive, IActivityUpdate, IActivityOrderHandler
    {
        public override bool Valid => Lite.Valid && ConfD != null;
        public MergeWorld World { get; private set; }   //世界实体
        public MergeWorldTracer WorldTracer { get; private set; }   //世界实体追踪器
        public EventFarmBoard ConfD { get; private set; }
        //用户分层 区别棋盘配置 对应EventFarmBoardGroup.id
        public int GroupId { get; private set; }  
        //从0开始 表示当前合成链中已解锁的最大等级棋子 如：0表示什么都没解锁  3代表当前已解锁合成链中第3个棋子
        public int UnlockMaxLevel { get; private set; } 
        //当前已解锁的农田数量
        public int UnlockFarmlandNum { get; private set; } 
        //当前代币产出类型
        public TokenOutputType OutputType { get; private set; } = TokenOutputType.None;
        //当前代币数量
        public int TokenNum { get; private set; } = 0;

        #region 活动基础

        //外部调用需判空
        public EventFarmBoardGroup GetCurGroupConfig()
        {
            return Game.Manager.configMan.GetEventFarmBoardGroupConfig(GroupId);
        }
        
        //debug面板重置当前棋盘 避免来回调时间导致棋盘没有重置
        public void DebugResetFarmBoard()
        {
            _ClearFarmBoardData();
        }
        
        public FarmBoardActivity(ActivityLite lite_)
        {
            Lite = lite_;
            ConfD = Game.Manager.configMan.GetEventFarmBoardConfig(lite_.Param);
        }
        
        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            GroupId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            //初始化农场棋盘 只在活动创建时走一次
            _InitFarmBoardData();
            //添加初始代币
            _InitStartToken();
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新农田信息
            _RefreshFarmlandInfo();
            //刷新动物产出信息
            _RefreshAnimalOutputInfo();
            //刷新代币产出类型
            _RefreshTokenOutputType();
            //刷新订单产出代币模块
            _RefreshScoreEntity();
            //刷新耗体产出代币模块
            _RefreshSpawnBonusHandler();
            //活动首次开启时弹脸
            StartPopup.Popup();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, GroupId));
            any.Add(ToRecord(1, UnlockMaxLevel));
            any.Add(ToRecord(2, TokenNum));
            any.Add(ToRecord(3, _curDepthIndex));
            any.Add(ToRecord(4, _curAnimalOutputIndex));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            GroupId = ReadInt(0, any);
            UnlockMaxLevel = ReadInt(1, any);
            TokenNum = ReadInt(2, any);
            _curDepthIndex = ReadInt(3, any);
            _curAnimalOutputIndex = ReadInt(4, any);
            //刷新弹脸信息
            _RefreshPopupInfo();
            //刷新图鉴棋子信息
            _RefreshAllItemIdList();
            //刷新农田信息
            _RefreshFarmlandInfo();
            //刷新动物产出信息
            _RefreshAnimalOutputInfo();
            //刷新代币产出类型
            _RefreshTokenOutputType();
            //刷新订单产出代币模块
            _RefreshScoreEntity();
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach (var v in StartPopup.ResEnumerate()) yield return v;
            foreach (var v in Visual.ResEnumerate()) yield return v;
        }

        public override void WhenReset()
        {
            //清理scoreEntity
            _ClearScoreEntity();
        }
        
        public override void WhenEnd()
        {
            var conf = GetCurGroupConfig();
            if (conf != null)
            {
                //回收剩余代币
                using var _ = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<int, int> map);
                map[ConfD.TokenId] = TokenNum;
                var tokenReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> tokenRewardList);
                ActivityExpire.ConvertToReward(conf.ExpireItem, tokenRewardList, ReasonString.farm_end_token_energy, map);
                Game.Manager.screenPopup.TryQueue(EndPopup.popup, PopupType.Login, tokenReward);
                //回收棋盘上未使用的奖励棋子
                var boardReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> boardRewardList);
                var itemsInfo = BoardActivityUtility.CollectAllBoardReward(boardRewardList, World);
                //打点
                DataTracker.event_farm_end_reward.Track(this, itemsInfo);
                //只在有回收奖励时弹脸
                if (boardRewardList.Count > 0)
                    Game.Manager.screenPopup.TryQueue(ConvertPopup.popup, PopupType.Login, boardReward);
                else
                    boardReward.Free();
            }
            //清理scoreEntity
            _ClearScoreEntity();
            //清理棋盘相关数据
            _ClearFarmBoardData();
        }
        
        #region 界面 入口 换皮 弹脸
        
        public override ActivityVisual Visual => StartPopup.visual;

        public VisualRes VisualBoard { get; } = new(UIConfig.UIFarmBoardMain); //棋盘UI
        public VisualRes VisualHelp { get; } = new(UIConfig.UIFarmBoardHelp);
        public VisualRes VisualTokenTip { get; } = new(UIConfig.UIFarmBoardTokenTips); //Tip
        public VisualRes VisualAnimalTip { get; } = new(UIConfig.UIFarmBoardAnimalTips); //Tip
        public VisualRes VisualLoading { get; } = new(UIConfig.UIFarmBoardLoading);
        public VisualRes VisualComplete { get; } = new(UIConfig.UIFarmBoardComplete);
        public VisualRes VisualGetSeed { get; } = new(UIConfig.UIFarmBoardGetSeed);

        // 弹脸
        public VisualPopup StartPopup { get; } = new(UIConfig.UIFarmBoardBegin); //活动开启theme
        public VisualPopup EndPopup { get; } = new(UIConfig.UIFarmBoardEnd); //活动结束
        public VisualPopup ConvertPopup { get; } = new(UIConfig.UIFarmBoardConvert); //补领
        
        public override void Open()
        {
            ActivityTransit.Enter(this, this.VisualLoading, this.VisualBoard.res);
        }

        public void Close()
        {
            ActivityTransit.Exit(this, this.VisualBoard.res.ActiveR);
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
            VisualComplete.Setup(ConfD.FinishTheme);
        }
        
        string IBoardEntry.BoardEntryAsset()
        {
            StartPopup.visual.AssetMap.TryGetValue("boardEntry", out var key);
            return key;
        }
        
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

        //获取当前已解锁到的农场棋盘棋子最大等级 等级从0开始 这里的等级涵盖了整条合成链的所有棋子
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

        //检查是否是农场棋盘专属棋子
        public bool CheckIsFarmBoardItem(int itemId)
        {
            if (!Valid || itemId <= 0) return false;
            return _allItemIdList.Contains(itemId);
        }
        
        //当农场棋盘中有新棋子解锁时刷新相关数据(数据层)
        public void OnNewItemUnlock()
        {
            if (!Valid) return;
            UnlockMaxLevel = _GetCurUnlockItemMaxLevel();
            //刷新云层信息
            World?.activeBoard?.RefreshCloudInfo(_curDepthIndex, UnlockMaxLevel);
            //检测当前棋盘内是否还有云层遮挡的区域 没有的话会触发下降
            _CheckCanMoveBoard();
            //刷新当前农田信息
            _RefreshFarmlandInfo();
            //有新棋子解锁时刷新当前掉落概率
            if (IsEnergyType())
                _spawnBonusHandler.SetDirty();
        }
        
        //当农场棋盘中有新棋子解锁时执行相关表现(表现层)
        public void OnNewItemShow(Merge.Item itemData)
        {
            if (!Valid) return;
            //只有在解锁的新棋子是农场棋盘棋子时才发事件并打点
            if (CheckIsFarmBoardItem(itemData.config.Id))
            {
                MessageCenter.Get<MSG.UI_FARM_BOARD_UNLOCK_ITEM>().Dispatch(itemData);
                var totalCount = _allItemIdList.Count;
                var isFinal = UnlockMaxLevel >= totalCount;
                DataTracker.event_farm_milestone.Track(this, UnlockMaxLevel, totalCount, 
                    GetCurGroupConfig()?.Diff ?? 0, World.activeBoard?.boardId ?? 0, _curDepthIndex, UnlockFarmlandNum, isFinal);
            }
        }
        
        private List<int> _allItemIdList = new List<int>();    //当前活动主链条的所有棋子idList 按等级由小到大排序
        
        private void _RefreshAllItemIdList()
        {
            _allItemIdList.Clear();
            var idList = GetCurGroupConfig()?.MilestoneId;
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
        
        FeatureEntry IBoardArchive.Feature => FeatureEntry.FeatureFarmBoard;

        //此接口在活动第一次创建时不会走到，创建后每次都会走,且时机在LoadSetup之后
        void IBoardArchive.SetBoardData(fat.gamekitdata.Merge data)
        {
            if (data == null)
                return;
            _InitWorld(data.BoardId, false);
            World.Deserialize(data, null);
            World.activeBoard.RefreshCloudInfo(_curDepthIndex, UnlockMaxLevel);
            //刷新耗体产出代币模块
            _RefreshSpawnBonusHandler();
        }

        void IBoardArchive.FillBoardData(fat.gamekitdata.Merge data)
        {
            World?.Serialize(data);
        }
        
        //初始化农场棋盘 只在活动创建时走一次
        private void _InitFarmBoardData()
        {
            _allItemIdList.Clear();
            var infoConfig = GetCurGroupConfig();
            if (infoConfig == null)
                return;
            //初始化图鉴棋子list
            foreach (var id in infoConfig.MilestoneId)
            {
                _allItemIdList.AddIfAbsent(id);
            }
            //每次活动创建时都清空一下图鉴系统中的解锁信息 以防活动异常结束导致图鉴没有锁定
            Game.Manager.handbookMan.LockHandbookItem(_allItemIdList);
            //第一次初始化棋盘
            _InitWorld(infoConfig.BoardId, true);
            var board = World.activeBoard;
            _curDepthIndex = board.size.y;
            board.RefreshCloudInfo(_curDepthIndex, UnlockMaxLevel);
        }

        private void _InitWorld(int boardId, bool isFirstCreate)
        {
            World = new MergeWorld();
            WorldTracer = new MergeWorldTracer(_OnBoardItemChange, null);
            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry()
            {
                world = World,
                type = MergeWorldEntry.EntryType.FarmBoard,
            });
            WorldTracer.Bind(World);
            World.BindTracer(WorldTracer);
            //棋盘不需要背包 也没有订单 和底部信息栏
            // World.BindOrderHelper(Game.Manager.mainOrderMan.curOrderHelper);
            Game.Manager.mergeBoardMan.InitializeBoard(World, boardId, isFirstCreate);
        }
        
        private void _ClearFarmBoardData()
        {
            //清理handler
            _ClearSpawnBonusHandler();
            //活动结束时将关联棋子的图鉴置为锁定状态
            Game.Manager.handbookMan.LockHandbookItem(_allItemIdList);
            //取消注册并清理当前world
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            World = null;
            WorldTracer = null;
        }
        
        #endregion

        #region 棋盘下降逻辑
        
        //记录当前深度值，初始为棋盘总行数，后续每次棋盘向下延伸，都会加上延伸的行数
        private int _curDepthIndex;
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

        //这里数据层不需要关注云层是否解锁，因为云层解锁本质上是新棋子解锁驱动的，目前已经有UI_FARM_BOARD_UNLOCK_ITEM事件驱动了
        //基于此的话 数据层只需要在合适时机检测棋盘是否可以下降就可以
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
            var boardRowConfIdList = configMan.GetEventFarmBoardDetailConfig(detailParam)?.BoardRowId;
            if (boardRowConfIdList == null || boardRowConfIdList.Count < 0)
                return;
            //根据当前记录的深度值查找到当前最顶端的棋盘行配置
            if (boardRowConfIdList.TryGetByIndex(_curDepthIndex - 1, out var rowInfoId))
            {
                var rowConf = configMan.GetEventFarmRowConfig(rowInfoId);
                if (rowConf == null)
                    return;
                //读取配置上当前要下降的行数
                var downRowCount = rowConf.UpNum;
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
                MessageCenter.Get<MSG.UI_FARM_BOARD_MOVE_UP_READY>().Dispatch();
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
            mergeBoardMan.CollectBoardItemByRow(board, downRowCount, collectItemList, false);
            //通知界面做飞棋子表现
            MessageCenter.Get<MSG.UI_FARM_BOARD_MOVE_UP_COLLECT>().Dispatch(collectItemList, downRowCount);
            //仅数据层 将从第0行开始到倒数第(1+downRowCount)行为止范围内的所有棋子，整体向下平移到棋盘底部
            mergeBoardMan.MoveDownBoardItem(board, 1 + downRowCount);
            //在从(1+downRowCount)行开始到最后一行为止范围内根据配置创建新的棋子
            using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
            {
                if (BoardActivityUtility.FillFarmBoardRowConfStr(detailParam, rowItems, _curDepthIndex, downRowCount))
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
            MessageCenter.Get<MSG.UI_FARM_BOARD_MOVE_UP_FINISH>().Dispatch(downRowCount);
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
            MessageCenter.Get<MSG.UI_FARM_EXTREME_CASE_BLOCK>().Dispatch();
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

        #region 农田逻辑

        //当前的农田产出信息
        private RewardConfig farmlandOutputInfo;
        private string farmlandOutputInfoStr = "";
        
        //传入解锁等级，获取该等级是否有对应的可解锁农田
        public bool CheckHasFarmland(int unlockLevel)
        {
            var idList = GetCurGroupConfig()?.AreaId;
            if (idList == null)
                return false;
            var configMan = Game.Manager.configMan;
            foreach (var id in idList)
            {
                var farmlandConf = configMan.GetEventFarmBoardFarmConfig(id);
                if (farmlandConf != null && unlockLevel == farmlandConf.UnlockLevel)
                {
                    return true;
                }
            }
            return false;
        }

        //消耗代币并收集农田棋子 
        //会根据处理结果有不同返回值
        //0 数据错误 默认无对应表现
        //1 棋盘空间不足
        //2 奖励箱顶部棋子发往棋盘(配置保证农田产的棋子一定会在奖励箱顶部)
        //3 代币不足
        //4 成功发棋子
        public int CollectFarmland(Vector3 farmlandPos)
        {
            if (farmlandOutputInfo == null)
                return 0;
            var board = Valid ? World?.activeBoard : null;
            if (board == null)
                return 0;
            //检查棋盘空间是否有空间
            var hasEmptyGrid = board.CheckHasEmptyIdx();
            if (!hasEmptyGrid)
                return 1;
            //检查奖励箱顶部棋子是否是农田产的棋子 是的话就直接发到棋盘上，此时不播农田收割动画，不消耗代币
            var nextItem = World.nextRewardItem;
            if (nextItem != null && nextItem.tid == farmlandOutputInfo.Id)
            {
                BoardUtility.ClearSpawnRequest();
                BoardUtility.RegisterSpawnRequest(nextItem.tid, _rewardBoxPos);
                var item = board.SpawnRewardItemByIdx(0);
                if (item == null)
                {
                    // clear
                    BoardUtility.ClearSpawnRequest();
                    // 弹取出失败toast
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.BoardFullUi);
                    //播音效
                    Game.Manager.audioMan.TriggerSound("BoardFull");
                }
                else
                {
                    // track
                    DataTracker.gift_box_change.Track(item.tid, false);
                    // 取出棋子播放音效
                    Game.Manager.audioMan.TriggerSound("BoardReward");
                }
                return 2;
            }
            //尝试消耗代币
            if (!TryUseToken(ConfD.TokenId, ConfD.TokenUse, ReasonString.farm_use_token))
                return 3;
            //尝试往棋盘上发棋子
            BoardUtility.ClearSpawnRequest();
            var bonusId = farmlandOutputInfo.Id;
            for (int i = 0; i < farmlandOutputInfo.Count; i++)
            {
                //从农田飞往棋盘对应坐标位置
                BoardUtility.RegisterSpawnRequest(bonusId, farmlandPos);
                var bonusItem = board.SpawnItemMustWithReason(bonusId, ItemSpawnContext.CreateWithSource(null, ItemSpawnContext.SpawnType.Farm), 0, 0, false, false);
                //如果棋盘满了发不上 则发到奖励箱
                if (bonusItem == null)
                {
                    BoardUtility.ClearSpawnRequest();
                    var d = Game.Manager.rewardMan.BeginReward(bonusId, 1, ReasonString.farm_use_token);
                    UIFlyUtility.FlyReward(d, farmlandPos);
                }
            }
            return 4;
        }
        
        private void _RefreshFarmlandInfo()
        {
            var idList = GetCurGroupConfig()?.AreaId;
            if (idList == null)
                return;
            var count = 0;
            var outputStr = "";
            var configMan = Game.Manager.configMan;
            foreach (var id in idList)
            {
                var farmlandConf = configMan.GetEventFarmBoardFarmConfig(id);
                if (farmlandConf != null && UnlockMaxLevel >= farmlandConf.UnlockLevel)
                {
                    count++;
                    outputStr = farmlandConf.ItemDetail;
                }
            }
            UnlockFarmlandNum = count;
            farmlandOutputInfoStr = outputStr;
            farmlandOutputInfo = outputStr.ConvertToRewardConfigIfValid();
        }

        //表现层设置棋盘奖励箱位置，便于特殊情境下，数据层推动表现时传入起始位置
        private Vector3 _rewardBoxPos = Vector3.zero;
        public void SetRewardBoxPos(Vector3 rewardBoxPos)
        {
            _rewardBoxPos.Set(rewardBoxPos.x, rewardBoxPos.y, rewardBoxPos.z);
        }

        #endregion

        #region 动物逻辑

        //记录当前动物的产出情况
        private List<int> _animalOutputInfo = new List<int>();
        private int _curAnimalOutputIndex = -1;    //存档记录目前的产出序号，方便之后判断当前该产出什么棋子，默认-1表示当前不是产出状态

        //获取当前动物可以吃的棋子id 目前默认只能吃一种棋子
        public int GetConsumeItemId()
        {
            return _GetAnimalConfig()?.ItemId ?? 0;
        }
        
        //在合适时机自行检查棋盘上是否存在目标棋子，如棋子合成时、第一次进界面时
        public bool CheckHasConsumeItem()
        {
            var consumeItemId = GetConsumeItemId();
            if (consumeItemId <= 0)
                return false;
            var item = World?.activeBoard?.FindAnyItemByConfigId(consumeItemId);
            return item != null;
        }

        //检查动物目前是否正在产出当中 外部可根据此改变动物状态
        public bool CheckIsInOutput()
        {
            return _curAnimalOutputIndex >= 0;
        }

        //尝试消耗指定的棋子
        public bool TryConsumeItem(Item item)
        {
            if (item == null)
                return false;
            var conf = _GetAnimalConfig();
            if (conf == null || conf.ItemId != item.tid)
                return false;
            var board = Valid ? World?.activeBoard : null; 
            if (board == null) 
                return false;
            //要消耗的棋子直接死亡
            if (!board.DisposeItem(item, ItemDeadType.FarmAnimal))
                return false;
            _animalOutputInfo.Clear();
            foreach (var outputId in conf.Output)
            {
                _animalOutputInfo.Add(outputId);
            }
            _curAnimalOutputIndex = 0;
            return true;
        }

        //尝试产出棋子
        //会根据处理结果有不同返回值
        //0 数据错误 默认无对应表现
        //1 产出成功
        //2 棋盘空间不足 产出失败
        public int TryOutputItem(Vector3 animalPos)
        {
            if (!CheckIsInOutput())
                return 0;
            if (!_TryGetNextOutputItem(out var outputId))
                return 0;
            var board = Valid ? World?.activeBoard : null; 
            if (board == null) 
                return 0;
            
            //从动物飞往棋盘对应坐标位置
            BoardUtility.ClearSpawnRequest();
            BoardUtility.RegisterSpawnRequest(outputId, animalPos);
            var bonusItem = board.SpawnItemMustWithReason(outputId, ItemSpawnContext.CreateWithSource(null, ItemSpawnContext.SpawnType.Farm), 0, 0, false, false);
            if (bonusItem != null)
            {
                _curAnimalOutputIndex++;
                //产出次数用完 重置为-1
                if (_curAnimalOutputIndex >= _animalOutputInfo.Count)
                {
                    _curAnimalOutputIndex = -1;
                }
                return 1;
            }
            //棋盘满了 产出失败
            BoardUtility.ClearSpawnRequest();
            return 2;
        }
        
        //尝试获取下一个要产出的棋子id
        private bool _TryGetNextOutputItem(out int itemId)
        {
            return _animalOutputInfo.TryGetByIndex(_curAnimalOutputIndex, out itemId);
        }

        private void _RefreshAnimalOutputInfo()
        {
            if (!CheckIsInOutput())
                return;
            var conf = _GetAnimalConfig();
            if (conf == null)
                return;
            _animalOutputInfo.Clear();
            foreach (var outputId in conf.Output)
            {
                _animalOutputInfo.Add(outputId);
            }
        }
        
        //获取当前棋盘对应的动物配置
        private EventFarmBoardAnimal _GetAnimalConfig()
        {
            var eatId = GetCurGroupConfig()?.EatId ?? 0;
            return Game.Manager.configMan.GetEventFarmBoardAnimalConfig(eatId);
        }

        #endregion

        #endregion
        
        #region 代币相关
        
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
            var isOrder = conf.ExtraScore > 0;
            var isEnergy = conf.LevelId.Count > 0;
            if (isOrder && isEnergy)
                OutputType = TokenOutputType.All;
            else if (isEnergy)
                OutputType = TokenOutputType.Energy;
            else if (isOrder)
                OutputType = TokenOutputType.Order;
            else
                OutputType = TokenOutputType.None;
        }

        #region 代币增加/消耗逻辑
        
        public void TryAddToken(int id, int num, ReasonString reason)
        {
            if (ConfD == null || id <= 0 || num <= 0)
                return;
            if (ConfD.TokenId == id)
            {
                if (ChangeItemToken(true, num) && reason != ReasonString.farm_order)
                {
                    //非完成订单获得货币时 单独向ScoreEntity中同步最新分数  并单独打点token_change
                    if (IsOrderType()) 
                        _scoreEntity.UpdateScore(TokenNum);
                    DataTracker.token_change.Track(id, num, TokenNum, reason);
                }
            }
        }

        public bool TryUseToken(int id, int num, ReasonString reason)
        {
            if (ConfD == null || id <= 0 || num <= 0)
                return false;
            if (ConfD.TokenId == id)
            {
                if (ChangeItemToken(false, num))
                {
                    DataTracker.token_change.Track(id, -num, TokenNum, reason);
                    //消耗代币时额外打个点，便于查看当时状态
                    DataTracker.event_farm_use_token.Track(this, UnlockMaxLevel, _allItemIdList.Count, GetCurGroupConfig()?.Diff ?? 0, World.activeBoard?.boardId ?? 0, _curDepthIndex, UnlockFarmlandNum, farmlandOutputInfoStr);
                    return true;
                }
            }
            return false;
        }

        private bool ChangeItemToken(bool isAdd, int changNum)
        {
            if (isAdd && changNum > 0)
            {
                TokenNum += changNum;
                MessageCenter.Get<MSG.FARM_BOARD_TOKEN_CHANGE>().Dispatch();
                return true;
            }
            if (!isAdd)
            {
                if (TokenNum >= changNum)
                {
                    TokenNum -= changNum;
                    MessageCenter.Get<MSG.FARM_BOARD_TOKEN_CHANGE>().Dispatch();
                    return true;
                }
                else
                {
                    var tokenIcon = UIUtility.FormatTMPString(ConfD.TokenId);
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.FarmNoToken, tokenIcon);
                    return false;
                }
            }
            return false;
        }
        
        //根据配置设置初始代币数量
        private void _InitStartToken()
        {
            var conf = GetCurGroupConfig();
            if (conf == null || ConfD == null) return;
            TryAddToken(ConfD.TokenId, conf.TokenNum, ReasonString.farm_start);
        }

        #endregion
        
        #region 主棋盘订单右下角积分

        private ScoreEntity _scoreEntity;

        private void _RefreshScoreEntity()
        {
            if (!IsOrderType())
                return;
            if (ConfD == null)
                return;
            var conf = GetCurGroupConfig();
            if (conf != null)
            {
                //需要时才new
                _scoreEntity ??= new ScoreEntity();
                //无需监听_scoreEntity内部的SCORE_ENTITY_ADD_COMPLETE消息
                //因为所有积分发放最后都会走BeginReward, 而本活动内部处理了积分变化的逻辑 
                _scoreEntity.Setup(TokenNum, this, ConfD.TokenId, conf.ExtraScore,
                    ReasonString.farm_order, "", Constant.MainBoardId, false);
            }
        }
        
        private void _ClearScoreEntity()
        {
            //活动结束时 根据类型决定是否清理_scoreEntity
            if (IsOrderType())
                _scoreEntity?.Clear();
            _scoreEntity = null;
        }

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (!IsOrderType())
                return false;
            //积分类活动在计算订单积分时，都排除心想事成订单
            if ((order as IOrderData).IsMagicHour)
                return false;
            var changed = false;
            var state = order.GetState((int)OrderParamType.ScoreEventIdBR);
            // 没有积分 or 不是同一期活动时给这个订单生成右下角积分
            if (state == null || state.Value != Id)
            {
                changed = true;
                _scoreEntity.CalcOrderScoreBR(order, tracer);
            }
            return changed;
        }

        #endregion

        #region 点击耗体生成器产代币逻辑
        
        private FarmBoardItemSpawnBonusHandler _spawnBonusHandler;

        private void _RefreshSpawnBonusHandler()
        {
            if (!IsEnergyType())
                return;
            _spawnBonusHandler ??= new FarmBoardItemSpawnBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(_spawnBonusHandler);
        }
        
        private void _ClearSpawnBonusHandler()
        {
            //活动结束时 根据类型决定是否取消注册handler
            if (IsEnergyType())
                Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(_spawnBonusHandler);
            _spawnBonusHandler = null;
        }
        
        public EventFarmDrop GetCurDropConf()
        {
            var confId = _GetCurDropConfId();
            return Game.Manager.configMan.GetEventFarmDropConfig(confId);
        }

        //获取当前已解锁到的农场棋盘掉落信息id (EventFarmDrop.id) 
        private int _GetCurDropConfId()
        {
            var detailConfig = GetCurGroupConfig();
            if (detailConfig == null)
                return 0;
            detailConfig.LevelId.TryGetByIndex(UnlockMaxLevel, out var dropId);
            return dropId;
        }

        #endregion
        
        #endregion
    }

    public class FarmBoardEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => entry;
        private readonly ListActivity.Entry entry;
        private readonly FarmBoardActivity activity;

        public FarmBoardEntry(ListActivity.Entry _entry, FarmBoardActivity _activity)
        {
            (entry, this.activity) = (_entry, _activity);
            entry.dot.SetActive(activity.TokenNum > 0);
            entry.dotCount.gameObject.SetActive(activity.TokenNum > 0);
            entry.dotCount.SetRedPoint(activity.TokenNum);
            MessageCenter.Get<MSG.FARM_BOARD_TOKEN_CHANGE>().AddListener(RefreshEntry);
        }

        public override void Clear(ListActivity.Entry e_)
        {
            MessageCenter.Get<MSG.FARM_BOARD_TOKEN_CHANGE>().RemoveListener(RefreshEntry);
        }
        
        private void RefreshEntry()
        {
            entry.dotCount.SetRedPoint(activity.TokenNum);
        }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }
    }
}