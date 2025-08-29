/*
 * @Author: tang.yan
 * @Description: 棋盘移动处理者
 * @Date: 2025-08-05 16:08:52
 */
using System;
using UnityEngine;
using FAT.Merge;
using System.Collections.Generic;
using EL;

namespace FAT
{
    public class BoardMoveHandler
    {
        public enum BoardMoveType
        {
            None = 0,
            CycleUp = 1,        //带循环的棋盘 整个棋盘向上移动 如矿车棋盘
            CloudDown = 2,      //带云层的棋盘 整个棋盘向下移动 如农场棋盘、许愿棋盘
            CycleMineUp = 3,    //带循环以及含有ComTrigAutoSource类型的棋子 整个棋盘向上移动 如挖矿棋盘
        }
        
        //各个活动继承的接口
        private readonly IBoardMoveAdapter _adapter;
        private readonly IBoardActivityRowConf _confAdapter;
        //棋盘移动的类型，不同类型决定了不同的处理逻辑
        private BoardMoveType _type = BoardMoveType.None;

        public BoardMoveHandler(ActivityLike activity, BoardMoveType type, int depthIndex)
        {
            _adapter = (IBoardMoveAdapter)activity;
            _confAdapter = (IBoardActivityRowConf)activity;
            if (_adapter == null || _confAdapter == null)
            {
                DebugEx.Error($"BoardMoveHandler error! activity = {activity?.Type}");
            }
            _type = type;
            _curDepthIndex = depthIndex;
        }
        
        //需在活动的OnBoardItemChange方法中调用
        public void OnBoardItemChange()
        {
            _TryExecuteMoveBoard();
        }

        //需在活动的OnActivityUpdate方法中调用
        public void OnActivityUpdate(float deltaTime)
        {
            //在下一帧调用棋盘上升回调 处理相关状态
            _ProcessMoveAction();
        }

        //棋盘是否正处于移动/待移动状态
        public bool IsBoardMoving()
        {
            return _isBoardMoving || _isReadyToMove;
        }

        //业务逻辑如果需要在棋盘移动前做一些额外表现 可以在做完表现后主动调用此方法
        //todo 这里的相关逻辑有待在实际应用中调整
        public void StartMoveBoard()
        {
            _ProcessMoveAction();
        }

        private void _ProcessMoveAction()
        {
            if (_isReadyToMove && _readyFrameCount != -1 && _readyFrameCount != Time.frameCount)
            {
                _isReadyToMove = false;
                _isBoardMoving = true;
                _moveAction?.Invoke();
                _moveAction = null;
                _isBoardMoving = false;
                _readyFrameCount = -1;
            }
        }

        #region 内部逻辑

        //记录当前深度值，初始为棋盘总行数，后续每次棋盘移动，都会加上移动的行数
        private int _curDepthIndex;
        //目前是否已检测到棋盘可以移动
        private bool _isReadyToMove = false;
        //上次检测到棋盘可以移动时的游戏帧数 避免同一帧内处理后续逻辑
        private int _readyFrameCount = -1;
        //目前数据层是否正在处理棋盘移动逻辑 加此标记位是为了避免处理过程中，因棋子移动、生成等行为，会多次触发_OnBoardItemChange回调导致意料之外的情况发生
        private bool _isBoardMoving = false;
        //缓存棋盘移动回调
        private Action _moveAction = null;

        private void _TryExecuteMoveBoard()
        {
            if (_isBoardMoving) 
                return;
            // reset
            _isReadyToMove = false;
            _moveAction = null;
            _readyFrameCount = -1;
            //获取棋盘
            var board = _adapter.GetBoard();
            if (board == null)
                return;
            var detailParam = Game.Manager.mergeBoardMan.GetBoardConfig(board.boardId)?.DetailParam ?? 0;
            if (detailParam <= 0)
                return;
            //检测是否可以移动
            var canMove = _CheckCanMoveByType(board, detailParam, out var realMoveCount);
            if (!canMove)
                return;
            //可以移动 记一下状态 等一帧后执行
            _readyFrameCount = Time.frameCount;
            _isReadyToMove = true;
            _moveAction = () => _ExecuteMoveBoard(board, realMoveCount, detailParam);
        }

        private bool _CheckCanMoveByType(Board board, int detailParam, out int realMoveCount)
        {
            realMoveCount = 0;
            if (_type == BoardMoveType.CycleUp)
            {
                var moveNeedRowCount = _adapter.GetMoveNeedRowCount(detailParam);
                //1.计算屏幕内上至下连续的空行数。空行指的是无不可移动的棋子,本棋盘中只有箱子或者蜘蛛网状态
                var emptyRowCount = 0;
                var totalRow = board.size.y;
                for (var row = 0; row < totalRow; row++)
                {
                    if (board.CheckIsEmptyRow(row))
                        emptyRowCount++;
                    else
                        break;
                }
                //2.如果屏幕内上至下连续的空行数小于moveNeedRowCount时，不处理上升逻辑
                if (emptyRowCount < moveNeedRowCount)
                {
                    return false;
                }
                //3.计算实际上升行数  realMoveCount = 4 - (6 - emptyRowCount)
                realMoveCount = moveNeedRowCount - (totalRow - emptyRowCount);
                //上升行数大于0时才会移动
                return realMoveCount > 0;
            }
            else if (_type == BoardMoveType.CloudDown)
            {
                var hasLockCloud = board.CheckHasLockCloud();
                //当前棋盘中显示的云层区域有未解锁的 则return
                if (hasLockCloud)
                    return false;
                var boardRowConfIdList = _confAdapter.GetRowConfIdList(detailParam);
                if (boardRowConfIdList == null || boardRowConfIdList.Count < 0)
                    return false;
                //根据当前记录的深度值查找到当前最顶端的棋盘行配置
                if (!boardRowConfIdList.TryGetByIndex(_curDepthIndex - 1, out var rowInfoId))
                    return false;
                //读取配置上当前要下降的行数
                realMoveCount = _adapter.GetMoveCountByRowId(rowInfoId);
                //下降行数大于0时才会移动
                return realMoveCount > 0;
            }
            else if (_type == BoardMoveType.CycleMineUp)
            {
                var moveNeedRowCount = _adapter.GetMoveNeedRowCount(detailParam);
                //1.计算屏幕内上至下连续的空行数。空行指的是无不可移动的棋子,本棋盘中只有箱子或者蜘蛛网状态
                var emptyRowCount = 0;
                var totalRow = board.size.y;
                for (var row = 0; row < totalRow; row++)
                {
                    if (board.CheckIsEmptyRow(row))
                        emptyRowCount++;
                    else
                        break;
                }
                //2.如果屏幕内上至下连续的空行数小于moveNeedRowCount时，再检查一下是否是因为玩家操作不当导致棋盘无法上升，若是这种情况则帮一下玩家
                if (emptyRowCount < moveNeedRowCount)
                {
                    var dragEmptyRowCount = 0;
                    for (var row = 0; row < totalRow; row++)
                    {
                        if (board.CheckIsEmptyRow(row, true))
                            dragEmptyRowCount++;
                        else
                            break;
                    }
                    //若小于moveNeedRowCount 则认为是正常情况
                    if (dragEmptyRowCount < moveNeedRowCount)
                        return false;
                    //正常情况下二者值应该相等 此时说明玩家没有操作不当
                    if (dragEmptyRowCount <= emptyRowCount)
                        return false;
                    //一旦前者大于后者 则需要进一步检查
                    if (dragEmptyRowCount > emptyRowCount)
                    {
                        //检查目前棋盘上是否有合成可能
                        var checker = BoardViewManager.Instance.checker;
                        checker.FindMatch(true);
                        //如果还有 说明还没卡死 让玩家继续操作
                        if (checker.HasMatchPair())
                            return false;
                        //如果没有合成的可能，说明卡死了，帮玩家，直接上升棋盘，忽略无法合成的蜘蛛网棋子，若该棋子一直不被合成，则收到奖励箱后直接刷成解锁状态
                        emptyRowCount = dragEmptyRowCount;
                    }
                }
                //3.计算实际上升行数  realMoveCount = 4 - (8 - emptyRowCount)
                realMoveCount = moveNeedRowCount - (totalRow - emptyRowCount);
                //上升行数大于0时才会移动
                return realMoveCount > 0;
            }
            return false;
        }
        
        private enum BoardMoveDirection
        {
            None = 0,
            Up = 1,     //向上 如矿车棋盘
            Down = 2,   //向下 如农场棋盘 许愿棋盘
        }

        private BoardMoveDirection _CheckDirection()
        {
            switch (_type)
            {
                case BoardMoveType.CloudDown:
                    return BoardMoveDirection.Down;
                case BoardMoveType.CycleUp:
                case BoardMoveType.CycleMineUp:
                    return BoardMoveDirection.Up;
                default:
                    return BoardMoveDirection.None;
            }
        }

        private List<Item> collectItemList = new List<Item>();
        private void _ExecuteMoveBoard(Board board, int realMoveCount, int detailParam)
        {
            var direction = _CheckDirection();
            if (direction == BoardMoveDirection.None)
                return;
            var mergeBoardMan = Game.Manager.mergeBoardMan;
            //1.通知界面 按行传递要飞行的棋子
            for (var i = 0; i < realMoveCount; i++)    //这里为了配合表现层，改成循环收集的方式，每行会对应发一个事件
            {
                collectItemList.Clear();
                //从指定方向开始，把realMoveCount行数范围内所有的棋子，移到奖励箱
                //collectItemList记录收集的棋子，用于界面表现, 这里并没有清除棋子上原来的坐标信息，表现层可能会用得上
                mergeBoardMan.CollectBoardItemByRow(board, i + 1, collectItemList, direction == BoardMoveDirection.Up);
                //通知界面做飞棋子表现
                MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_MOVE_COLLECT>().Dispatch(collectItemList);
            }
            
            //2.仅数据层 根据棋盘移动方向的不同，将指定范围内的棋子坐标整体平移，空出来的位置根据配置创建新棋子
            if (direction == BoardMoveDirection.Up)
            {
                //将从(1+realMoveCount)行开始到最后一行为止范围内的所有棋子，整体向上平移到棋盘顶部
                mergeBoardMan.MoveUpBoardItem(board, 1 + realMoveCount);
                //在从(1+realMoveCount)行开始到最后一行为止范围内根据配置创建新的棋子
                using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
                {
                    if (BoardActivityUtility.FillBoardRowConfStr(_confAdapter, detailParam, rowItems, _curDepthIndex, realMoveCount))
                    {
                        var totalRow = board.size.y;
                        var startCreateRow = totalRow - realMoveCount;
                        mergeBoardMan.CreateNewBoardItemByRow(board, rowItems, startCreateRow);
                    }
                }
            }
            else if (direction == BoardMoveDirection.Down)
            {
                //将从第0行开始到倒数第(1+realMoveCount)行为止范围内的所有棋子，整体向下平移到棋盘底部
                mergeBoardMan.MoveDownBoardItem(board, 1 + realMoveCount);
                //在从(1+realMoveCount)行开始到最后一行为止范围内根据配置创建新的棋子
                using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
                {
                    if (BoardActivityUtility.FillBoardRowConfStr(_confAdapter, detailParam, rowItems, _curDepthIndex, realMoveCount))
                    {
                        mergeBoardMan.CreateNewBoardItemFromRowToTop(board, rowItems, realMoveCount);
                    }
                }
            }
            
            //3.更新当前深度值
            _curDepthIndex += realMoveCount;
            //调用adapter回调 使的活动实例保存_curDepthIndex 同时也方便其做一些数据层的额外操作(如类似农场棋盘刷新云层信息等)
            _adapter.OnDepthIndexUpdate(_curDepthIndex);
            //立即存档
            Game.Manager.archiveMan.SendImmediately(true);
            
            //4.整个流程结束 通知界面做棋盘上升表现
            MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_MOVE_START>().Dispatch();
        }

        #endregion
    }
}