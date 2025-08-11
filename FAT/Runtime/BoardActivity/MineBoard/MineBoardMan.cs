/*
 * @Author: tang.yan
 * @Description: 挖矿棋盘 管理器 
 * @Doc: https://centurygames.feishu.cn/wiki/BinVwMixeidJ7WkNd1tcU2x5n2f?fromScene=spaceOverview
 * @Date: 2025-03-07 10:03:28
 */

using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using FAT.Merge;
using UnityEngine;
using static FAT.RecordStateHelper;
using static FAT.BoardActivityUtility;

namespace FAT
{
    public class MineBoardMan : IGameModule, IUserDataHolder, IUpdate
    {
        //挖矿棋盘是否解锁
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureMine);
        //挖矿棋盘相关数据是否有效
        public bool IsValid => _curActivity != null && World != null;

        public MergeWorld World { get; private set; }   //世界实体
        public MergeWorldTracer WorldTracer { get; private set; }   //世界实体追踪器

        //记录当前正在进行的活动 若不在活动时间内会置为null 调用时记得判空 默认同一时间内只会开一个挖矿棋盘 
        private MineBoardActivity _curActivity;

        #region 对外方法

        //debug面板重置当前棋盘 避免来回调时间导致棋盘没有重置
        public void DebugResetMineBoard()
        {
            _ClearMineBoardData();
        }

        // 区分挖矿棋盘是从哪里打开的 主棋盘或者meta场景
        private static bool _isEnterFromMain;
        //进入挖矿棋盘
        public void EnterMineBoard()
        {
            if (!IsValid) return;
            if (UIManager.Instance.IsOpen(_curActivity.BoardResAlt.ActiveR)) return;
            //进入loading前就需要锁定弹窗逻辑
            UIManager.Instance.ChangeIdleActionState(false);
            Game.Manager.screenPopup.Block(true, false);
            Game.Instance.StartCoroutineGlobal(_CoLoading(_MergeToActivity));
        }

        private void _MergeToActivity(SimpleAsyncTask task, MineBoardActivity act)
        {
            _isEnterFromMain = UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain);
            if (_isEnterFromMain)
            {
                UIManager.Instance.CloseWindow(UIConfig.UIMergeBoardMain);
            }
            else
            {
                Game.Manager.mapSceneMan.Exit();
            }
            UIManager.Instance.OpenWindow(_curActivity.BoardResAlt.ActiveR, _curActivity);
            task.ResolveTaskSuccess();
        }

        //离开挖矿棋盘
        public void ExitMineBoard(MineBoardActivity act)
        {
            if (act == null)
                return;
            //关闭挖矿棋盘界面
            Game.Instance.StartCoroutineGlobal(_CoLoading(_ActivityToMerge, null, act));
        }

        private void _ActivityToMerge(SimpleAsyncTask task, MineBoardActivity act)
        {
            UIManager.Instance.CloseWindow(act.BoardResAlt.ActiveR);
            UIManager.Instance.CloseWindow(act.HandBookResAlt.ActiveR);
            UIManager.Instance.CloseWindow(act.MilestoneResAlt.ActiveR);

            //返回来源处
            if (_isEnterFromMain)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIMergeBoardMain);
            }
            else
            {
                Game.Manager.mapSceneMan.Enter(null);
            }
            task.ResolveTaskSuccess();
            //loading结束后才打开弹窗逻辑
            UIManager.Instance.ChangeIdleActionState(true);
            Game.Manager.screenPopup.Block(false, false);
        }
        private bool _isLoading;

        private System.Collections.IEnumerator _CoLoading(Action<SimpleAsyncTask, MineBoardActivity> afterFadeIn = null, Action afterFadeOut = null, MineBoardActivity act = null)
        {
            _isLoading = true;

            var waitFadeInEnd = new SimpleAsyncTask();
            var waitFadeOutEnd = new SimpleAsyncTask();
            var waitLoadingJobFinish = new SimpleAsyncTask();
            //复用寻宝loading音效
            Game.Manager.audioMan.TriggerSound("UnderseaTreasure");

            UIManager.Instance.OpenWindow(_curActivity.LoadingResAlt.ActiveR, waitLoadingJobFinish, waitFadeInEnd,
                waitFadeOutEnd);

            yield return waitFadeInEnd;

            afterFadeIn?.Invoke(waitLoadingJobFinish, act);

            yield return waitFadeOutEnd;

            afterFadeOut?.Invoke();

            _isLoading = false;
        }

        //图鉴棋子是否解锁
        public bool IsItemUnlock(int itemId)
        {
            return Game.Manager.handbookMan.IsItemUnlocked(itemId);
        }

        public int GetCurDepthIndex()
        {
            return _curDepthIndex;
        }

        //获取当前棋盘显示深度 单位米，默认一行对应100米
        public int GetCurDepth()
        {
            return _curDepthIndex * 100;
        }

        public List<int> GetAllItemIdList()
        {
            return _allItemIdList;
        }

        //获取当前已解锁到的挖矿棋盘棋子最大等级 等级从0开始 这里的等级涵盖了整条合成链的所有棋子
        public int GetCurUnlockItemMaxLevel()
        {
            if (!IsValid)
                return 0;
            var maxLevel = 0;
            for (var i = 0; i < _allItemIdList.Count; i++)
            {
                var itemId = _allItemIdList[i];
                if (IsItemUnlock(itemId) && maxLevel < i)
                    maxLevel = i;
            }
            return maxLevel;
        }

        //检查是否是挖矿棋盘专属棋子
        public bool CheckIsMineBoardItem(int itemId)
        {
            if (!IsValid || itemId <= 0) return false;
            return _allItemIdList.Contains(itemId);
        }

        //当挖矿棋盘中有新棋子解锁时刷新相关数据(数据层)
        public void OnNewItemUnlock()
        {
            if (!IsValid) return;
            _curActivity.UnlockMaxLevel = GetCurUnlockItemMaxLevel();
        }

        //当挖矿棋盘中有新棋子解锁时执行相关表现(表现层)
        public void OnNewItemShow(Merge.Item itemData)
        {
            if (!IsValid) return;
            //只有在解锁的新棋子是挖矿棋盘棋子时才发事件并打点
            if (CheckIsMineBoardItem(itemData.config.Id))
            {
                MessageCenter.Get<MSG.UI_MINE_BOARD_UNLOCK_ITEM>().Dispatch(itemData);
                var groupConfig = _curActivity?.GetCurGroupConfig();
                var maxLevel = 0;
                var totalItemNum = _allItemIdList.Count;
                var diff = 0;
                if (IsValid && groupConfig != null)
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
                DataTracker.event_mine_gallery_milestone.Track(_curActivity, maxLevel + 1, totalItemNum, diff, isFinal, World.activeBoard?.boardId ?? 0, _curDepthIndex);
            }
        }

        public void TryAddToken(int id, int num, ReasonString reason)
        {
            if (!IsValid)
                return;
            _curActivity.TryAddToken(id, num, reason);
        }

        public bool TryUseToken(int id, int num, ReasonString reason)
        {
            if (!IsValid)
                return false;
            return _curActivity.TryUseToken(id, num, reason);
        }

        #endregion

        #region 内部数据构造相关逻辑

        private int _curDepthIndex; //记录当前深度值，初始为棋盘总行数，后续每次棋盘向下延伸，都会加上延伸的行数
        private List<int> _allItemIdList = new List<int>();    //当前活动主链条的所有棋子idList 按等级由小到大排序

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.BoardActivity?.BoardActivityDataMap;
            //根据当前FeatureEntry获取对应棋盘存档数据，没有时返回
            if (data == null || !data.TryGetValue((int)FeatureEntry.FeatureMine, out var boardActivityData))
                return;
            var boardData = boardActivityData.Board;
            if (boardData != null)
            {
                //基于AnyState读取业务数据
                var any = boardActivityData.AnyState;
                _curDepthIndex = ReadInt(0, any);
                //读取并初始化棋盘数据
                _InitWorld(boardData.BoardId, false);
                World.Deserialize(boardData, null);
                WorldTracer.Invalidate();
            }
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.BoardActivity ??= new fat.gamekitdata.BoardActivity();
            var dataMap = data.BoardActivity.BoardActivityDataMap;
            if (IsValid)
            {
                var boardActivityData = new fat.gamekitdata.BoardActivityData();
                //存储当前绑定的活动id
                boardActivityData.BindActivityId = _curActivity.Id;
                //存储业务数据字段
                var any = boardActivityData.AnyState;
                any.Add(ToRecord(0, _curDepthIndex));
                //存储棋盘数据
                boardActivityData.Board = new fat.gamekitdata.Merge();
                World.Serialize(boardActivityData.Board);
                dataMap[(int)FeatureEntry.FeatureMine] = boardActivityData;
            }
        }

        public void TryStart(ActivityLike activity, bool isNew)
        {
            var act = (MineBoardActivity)activity;
            _SetCurActivity(act);
            _InitMineBoardData(isNew);
        }

        public void TryEnd(ActivityLike activity)
        {
            if (IsValid && _curActivity.Id == activity.Id)
            {
                _ClearMineBoardData();
                _SetCurActivity(null);
            }
        }

        private void _SetCurActivity(MineBoardActivity activity)
        {
            _curActivity = activity;
            _RefreshAllItemIdList();
        }

        private void _InitMineBoardData(bool isNew)
        {
            //不是第一次创建活动时 return
            if (!isNew)
                return;
            //第一次创建活动时 先清理一下之前可能存在的棋盘数据和图鉴数据
            _ClearMineBoardData();
            var infoConfig = _curActivity?.GetCurGroupConfig();
            if (infoConfig == null)
                return;
            _InitWorld(infoConfig.BoardId, true);
            _curDepthIndex = World.activeBoard?.size.y ?? 0;
            WorldTracer.Invalidate();
        }

        private void _ClearMineBoardData()
        {
            //活动结束时将关联棋子的图鉴置为锁定状态
            Game.Manager.handbookMan.LockHandbookItem(_allItemIdList);
            //取消注册并清理当前world
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            World = null;
            WorldTracer = null;
        }

        private void _InitWorld(int boardId, bool isFirstOpen)
        {
            World = new MergeWorld();
            WorldTracer = new MergeWorldTracer(_OnBoardItemChange, null);
            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry()
            {
                world = World,
                type = MergeWorldEntry.EntryType.MineBoard,
            });
            WorldTracer.Bind(World);
            World.BindTracer(WorldTracer);
            //挖矿棋盘不需要背包 也没有订单 和底部信息栏
            // World.BindOrderHelper(Game.Manager.mainOrderMan.curOrderHelper);
            Game.Manager.mergeBoardMan.InitializeBoard(World, boardId, isFirstOpen);
        }

        //供外部获取指定范围内的棋盘行配置 startRow从0开始
        public bool FillBoardRowConfStr(int detailId, IList<string> container, int startRow, int needRowCount)
        {
            var configMan = Game.Manager.configMan;
            var detailConf = configMan.GetEventMineBoardDetail(detailId);
            if (detailConf == null || container == null)
                return false;
            var totalCount = detailConf.BoardRowId.Count;
            if (startRow < 0 || totalCount <= 0 || needRowCount <= 0)
                return false;
            container.Clear();
            //需要的行配置范围不超过总行数时 直接按index取配置
            if (startRow + needRowCount <= totalCount)
            {
                for (int i = startRow; i < startRow + needRowCount; i++)
                {
                    var rowConf = configMan.GetEventMineBoardRow(detailConf.BoardRowId[i]);
                    if (rowConf != null)
                        container.Add(rowConf.MineRow);
                }
                DebugEx.FormatInfo("FillBoardRowConfStr 1, startIndex = {0}, needCount = {1}", startRow + 1, needRowCount);
            }
            //大于总行数时 先取到没超过的部分，然后超过的部分考虑循环，从startCycleIndex开始继续取剩余的部分
            else
            {
                //配置上可以开始循环的起始点
                var startCycleIndex = detailConf.BoardRowId.IndexOf(detailConf.CycleStart);
                //实际进行循环的起始点
                var realCycleIndex = startCycleIndex;
                //单位循环长度
                var cycleLength = totalCount - startCycleIndex;
                //剩余需要获取的行数
                var remainingRows = 0;
                // 先获取从startRow到末尾的部分
                if (startRow < totalCount)
                {
                    for (int i = startRow; i < totalCount; i++)
                    {
                        var rowConf = configMan.GetEventMineBoardRow(detailConf.BoardRowId[i]);
                        if (rowConf != null)
                            container.Add(rowConf.MineRow);
                    }
                    remainingRows = needRowCount - (totalCount - startRow);
                }
                else
                {
                    remainingRows = needRowCount;
                    var tempIndex = cycleLength != 0 ? (startRow - startCycleIndex) % cycleLength : 0;
                    realCycleIndex += tempIndex;
                }
                // 从循环起始点开始获取剩余的行
                for (int i = 0; i < remainingRows; i++)
                {
                    var findIndex = 0;
                    var curIndex = realCycleIndex + i;
                    if (curIndex < totalCount)
                    {
                        findIndex = curIndex;
                    }
                    else
                    {
                        findIndex = startCycleIndex + (curIndex - startCycleIndex) % cycleLength;
                    }
                    var rowConf = configMan.GetEventMineBoardRow(detailConf.BoardRowId[findIndex]);
                    if (rowConf != null)
                        container.Add(rowConf.MineRow);
                }
                DebugEx.FormatInfo("FillBoardRowConfStr 2, startIndex = {0}, needCount = {1}", realCycleIndex + 1, needRowCount);
            }

#if UNITY_EDITOR
            foreach (var info in container)
            {
                DebugEx.FormatInfo("FillBoardRowConfStr 3, info = {0}", info);
            }
#endif
            return true;
        }

        private void _RefreshAllItemIdList()
        {
            _allItemIdList.Clear();
            var idList = _curActivity?.GetCurGroupConfig()?.Handbook;
            if (idList == null)
                return;
            foreach (var id in idList)
            {
                _allItemIdList.AddIfAbsent(id);
            }
        }

        #region 棋盘上升相关逻辑

        //目前是否已检测到棋盘可以上升
        private bool _isReadyToMove = false;
        //上次检测到棋盘可以上升时的游戏帧数 避免同一帧内处理后续逻辑
        private int _readyFrameCount = -1;
        //目前数据层是否正在处理棋盘上升逻辑 加此标记位是为了避免处理过程中，因棋子移动、生成等行为，会多次触发_OnBoardItemChange回调导致意料之外的情况发生
        private bool _isBoardMoving = false;
        //缓存棋盘上升回调
        private Action _moveUpAction = null;

        //棋盘棋子发生变化时的回调
        private void _OnBoardItemChange()
        {
            if (_isBoardMoving) return;
            _isReadyToMove = false;
            _moveUpAction = null;
            _readyFrameCount = -1;
            //回调中检查当前是否满足屏幕内上至下数空行>=N行的情况。 空行指的是无不可移动的棋子
            var board = IsValid ? World.activeBoard : null;
            if (board == null)
                return;
            var detailParam = Game.Manager.mergeBoardMan.GetBoardConfig(board.boardId)?.DetailParam ?? 0;
            var rowUpCount = Game.Manager.configMan.GetEventMineBoardDetail(detailParam)?.RowUpCount ?? 0;
            //默认棋盘尺寸不变保持为8行。屏幕内上至下数连续空行>=rowUpCount时触发上升，上升行数=4-(8-屏幕内上至下数空行)，上升行数大于0时才会处理后续逻辑
            //这里先从配置层面判断，rowUpCount<=4时不做处理
            if (rowUpCount <= 4)
                return;
            //计算屏幕内上至下连续的空行数
            var emptyRowCount = 0;
            var totalRow = board.size.y;
            for (var row = 0; row < totalRow; row++)
            {
                if (board.CheckIsEmptyRow(row))
                    emptyRowCount++;
                else
                    break;
            }
            //如果屏幕内上至下连续的空行数小于rowUpCount时，再检查一下是否是因为玩家操作不当导致棋盘无法上升，若是这种情况则帮一下玩家
            if (emptyRowCount < rowUpCount)
            {
                var dragEmptyRowCount = 0;
                for (var row = 0; row < totalRow; row++)
                {
                    if (board.CheckIsEmptyRow(row, true))
                        dragEmptyRowCount++;
                    else
                        break;
                }
                //若小于rowUpCount 则认为是正常情况
                if (dragEmptyRowCount < rowUpCount)
                    return;
                //正常情况下二者值应该相等 此时说明玩家没有操作不当
                if (dragEmptyRowCount <= emptyRowCount)
                    return;
                //一旦前者大于后者 则需要进一步检查
                if (dragEmptyRowCount > emptyRowCount)
                {
                    //检查目前棋盘上是否有合成可能
                    var checker = BoardViewManager.Instance.checker;
                    checker.FindMatch(true);
                    //如果还有 说明还没卡死 让玩家继续操作
                    if (checker.HasMatchPair())
                        return;
                    //如果没有合成的可能，说明卡死了，帮玩家，直接上升棋盘，忽略无法合成的蜘蛛网棋子，若该棋子一直不被合成，则收到奖励箱后直接刷成解锁状态
                    emptyRowCount = dragEmptyRowCount;
                }
            }
            //计算上升行数
            var upRowCount = 4 - (8 - emptyRowCount);
            //上升行数大于0时才会处理后续逻辑
            if (upRowCount > 0)
            {
                _readyFrameCount = Time.frameCount;
                _isReadyToMove = true;
                _moveUpAction = () =>
                {
                    _MoveUpBoard(board, upRowCount, detailParam, totalRow - upRowCount);
                };
            }
        }

        void IUpdate.Update(float dt)
        {
            if (_readyFrameCount != -1 && _readyFrameCount != Time.frameCount)
            {
                MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_READY>().Dispatch();
                _readyFrameCount = -1;
            }
        }

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

        //棋盘上升一系列流程 先数据层后表现层
        private void _MoveUpBoard(Board board, int upRowCount, int detailParam, int startCreateRow)
        {
            var mergeBoardMan = Game.Manager.mergeBoardMan;
            var collectItemList = new List<Item>();
            //收集从上到下upRowCount行数范围内所有的棋子将其移到奖励箱
            //collectItemList记录收集的棋子，用于界面表现, 这里并没有清除棋子上原来的坐标信息，表现层可能会用得上
            mergeBoardMan.CollectBoardItemByRow(board, upRowCount, collectItemList);
            //通知界面做飞棋子表现
            MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_COLLECT>().Dispatch(collectItemList, upRowCount);
            //仅数据层 将从(1+upRowCount)行开始到最后一行为止范围内的所有棋子，整体向上平移到棋盘顶部
            mergeBoardMan.MoveUpBoardItem(board, 1 + upRowCount);
            //在从(1+upRowCount)行开始到最后一行为止范围内根据配置创建新的棋子
            using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
            {
                if (Game.Manager.mineBoardMan.FillBoardRowConfStr(detailParam, rowItems, _curDepthIndex, upRowCount))
                {
                    mergeBoardMan.CreateNewBoardItemByRow(board, rowItems, startCreateRow);
                }
            }
            //更新当前深度值
            _curDepthIndex += upRowCount;
            //立即存档
            Game.Manager.archiveMan.SendImmediately(true);
            //整个流程结束 通知界面做棋盘上升表现
            MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_FINISH>().Dispatch(upRowCount);
        }

        #endregion

        #region 活动结束回收bonus奖励

        //活动结束时回收棋盘中可以领取但未领取的各种奖励 这个方法会直接beginReward 需要在界面中合适时机自行commit
        //(MergeBoardMan中有个ClearBoard的清理方法 但是和当前需求可能不太相符 因此单独起一个干净的逻辑)
        public bool CollectAllBoardReward(List<RewardCommitData> rewards)
        {
            if (!IsValid || rewards == null)
                return false;
            var itemIdMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //棋子id整合结果(用于打点)
            var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //奖励整合结果
            World.WalkAllItem((item) =>
            {
                //遍历整个棋盘 找出所有可以回收的棋子 并整合
                _TryCollectReward(item, itemIdMap, rewardMap);
            }, MergeWorld.WalkItemMask.NoInventory);    //挖矿棋盘没有背包
            //发奖 相当于帮玩家把棋盘上没有使用的棋子直接用了 所以from用use_item
            foreach (var reward in rewardMap)
            {
                rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Key, reward.Value, ReasonString.use_item));
            }
            //打点
            DataTracker.event_mine_end_reward.Track(_curActivity, ConvertDictToString(itemIdMap));
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(itemIdMap);
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
            return true;
        }

        private void _TryCollectReward(Merge.Item item, Dictionary<int, int> itemIdMap, Dictionary<int, int> rewardMap)
        {
            if (item.TryGetItemComponent<ItemBonusCompoent>(out var bonusComp) && bonusComp.funcType == FuncType.Reward)
            {
                //收集棋子id
                Collect(itemIdMap, item.tid, 1);
                //收集奖励信息
                Collect(rewardMap, bonusComp.bonusId, bonusComp.bonusCount);
            }
            else if (item.TryGetItemComponent<ItemTapBonusComponent>(out var tapBonusComp) && tapBonusComp.funcType == FuncType.Collect)
            {
                //收集棋子id
                Collect(itemIdMap, item.tid, 1);
                //收集奖励信息
                Collect(rewardMap, tapBonusComp.bonusId, tapBonusComp.bonusCount);
            }
        }

        #endregion

        #region 打点

        public void TrackMineMilestone(ActivityLike act, int index, int milestoneNum, int diff, bool isFinal, string reward)
        {
            DataTracker.event_mine_milestone.Track(act, index, milestoneNum, diff, isFinal, World.activeBoard?.boardId ?? 0, _curDepthIndex, reward);
        }

        #endregion

        public void Reset()
        {
            _isReadyToMove = false;
            _isBoardMoving = false;
            _readyFrameCount = -1;
            _moveUpAction = null;
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            World = null;
            WorldTracer = null;
            _curActivity = null;
        }
        public void LoadConfig() { }
        public void Startup() { }

        #endregion
    }
}