
using System;
using System.Collections.Generic;

namespace FAT
{
    #region bingo结果返回值
    [Flags]
    public enum BingoResult
    {
        None = 0,
        Completed = 1 << 0,
        RowBingo = 1 << 1,
        ColumnBingo = 1 << 2,
        MainDiagonalBingo = 1 << 3,
        AntiDiagonalBingo = 1 << 4,
    }
    #endregion

    #region bingo单元结构
    public enum BingoState
    {
        UnFinished = 0,
        ToBeCompleted = 1,
        Completed = 2,
        Bingo = 3,
        Special = 4,//特殊状态，如上锁等
    }

    /// <summary>
    /// bingo单元基类
    /// 需要注意：bingo单元并不关心自身序号
    /// </summary>
    public class BingoCellBase
    {
        /// <summary>
        /// 当前bingo状态
        /// </summary>
        public BingoState state;

        /// <summary>
        /// 当前积分进度
        /// </summary>
        public int score;

        /// <summary>
        /// 目标积分进度
        /// </summary>
        public int target;

        /// <summary>
        /// 目标类型(int类型可以与enum互相转换，同时也可以用来记录棋子id等，因此选用int来记录目标类型)
        /// </summary>
        public int targetType;

        /// <summary>
        /// 更新积分进度
        /// 更新后自动检测标记是否可以置为ToBeCompleted状态
        /// </summary>
        /// <param name="add">需要更新到积分数量</param>
        public virtual void TryUpdateProgress(int type, int add)
        {
            if (type != targetType) { return; }
            if (state == BingoState.ToBeCompleted || state == BingoState.Completed) { return; }
            score += add;
            TryToBeCompleted();
        }

        /// <summary>
        /// 尝试标记为ToBeCompleted状态
        /// </summary>
        public virtual void TryToBeCompleted()
        {
            if (score >= target && state == BingoState.UnFinished) { ChangeState(BingoState.ToBeCompleted); }
        }

        /// <summary>
        /// 尝试标记为Completed状态
        /// </summary>
        public virtual void TryCompleted()
        {
            if (state != BingoState.ToBeCompleted) { return; }
            ChangeState(BingoState.Completed);
        }

        /// <summary>
        /// 尝试标记为Bingo状态;
        /// </summary>
        public virtual void TryBingo()
        {
            if (state != BingoState.Completed) { return; }
            ChangeState(BingoState.Bingo);
        }

        /// <summary>
        /// 修改当前bingo状态
        /// </summary>
        /// <param name="newState">需要修改到到状态</param>
        public void ChangeState(BingoState newState) { state = newState; }
    }
    #endregion

    #region bingo数据管理
    /// <summary>
    /// BingoCell管理类，存储所有BingoCell，并匹配序号，提供基本的检测、查找功能
    /// 需要注意：BingoCell本身并不关心自己的编号以及位置，这些信息是BingoMap管理
    /// </summary>
    public class BingoMapBase
    {
        /// <summary>
        /// 完成一次BingoCell后的结果
        /// </summary>
        public BingoResult result;

        /// <summary>
        /// 每行BingoCell数量
        /// </summary>
        public int cellCountEachRow;

        /// <summary>
        /// BingoCell字典
        /// </summary>
        public Dictionary<int, BingoCellBase> bingoDic = new();

        /// <summary>
        /// 刷新所有cell进度
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="score">增减的值/param>
        public virtual void UpdateCellScoreAll(int type, int score)
        {
            foreach (var cell in bingoDic) { cell.Value.TryUpdateProgress(type, score); }
        }

        /// <summary>
        /// 尝试完成某一个BingoCell
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        public virtual void TryCompleteBingoCell(int index)
        {
            result = BingoResult.None;
            if (!bingoDic.TryGetValue(index, out var cell)) { return; }
            cell.TryCompleted();
            if (cell.state == BingoState.Completed) { AfterComplete(index); }
        }

        /// <summary>
        /// 完成一个BingoCell后调用
        /// </summary>
        /// <param name="index"></param>
        protected virtual void AfterComplete(int index)
        {
            result |= BingoResult.Completed;
            if (CheckRowBingo(index)) { RowBingo(index); }
            if (CheckColumnBingo(index)) { ColumnBingo(index); }
            if (IsOnMainDiagonal(index) && CheckMainDiagonal()) { MainDiagonalBingo(); }
            if (IsOnAntiDiagonal(index) && CheckAntiDiagonal()) { AntiDiagonalBingo(); }
        }

        /// <summary>
        /// 获取跟传入的cell同一行的所有cell的序号
        /// </summary>
        /// <param name="index">c传入的cell序号/param>
        /// <param name="list">序号list</param>
        public virtual void FillSameRowCellList(int index, List<int> list)
        {
            var row = index / cellCountEachRow;
            for (var i = row * cellCountEachRow; i < row * cellCountEachRow + cellCountEachRow; i++) { list.Add(i); }
        }

        /// <summary>
        /// 获取跟传入的cell同一l列所有cell的序号
        /// </summary>
        /// <param name="index">c传入的cell序号/param>
        /// <param name="list">序号list</param>
        public virtual void FillSameColumnCellList(int index, List<int> list)
        {
            var column = index % cellCountEachRow;
            for (var i = column; i < cellCountEachRow * cellCountEachRow; i += cellCountEachRow) { list.Add(i); }
        }

        /// <summary>
        /// 获取对角线上的cell序号，如果传入的cell不在对角线上，则不会填充list
        /// </summary>
        /// <param name="index">c传入的cell序号/param>
        /// <param name="list">序号list</param>
        public virtual void FillMainDiagonalCellList(int index, List<int> list)
        {
            if (!IsOnMainDiagonal(index)) { return; }
            for (var i = 0; i < cellCountEachRow; i++) { list.Add(i * cellCountEachRow + i); }
        }

        /// <summary>
        /// 获取对角线上的cell序号，如果传入的cell不在对角线上，则不会填充list
        /// </summary>
        /// <param name="index">查找cell序号/param>
        /// <param name="list">序号list</param>
        public virtual void FillAntiDiagonalCellList(int index, List<int> list)
        {
            if (!IsOnAntiDiagonal(index)) { return; }
            for (var i = 0; i < cellCountEachRow; i++) { list.Add(i * cellCountEachRow + cellCountEachRow - i - 1); }
        }

        /// <summary>
        /// 获取上下左右相邻的cell序号
        /// </summary>
        /// <param name="index">查找的cell序号/param>
        /// <param name="list">序号list/param>
        public virtual void FillAdjacentCellList(int index, List<int> list)
        {
            if ((index - 1) / cellCountEachRow == index / cellCountEachRow) list.Add(index - 1);
            if ((index + 1) / cellCountEachRow == index / cellCountEachRow) list.Add(index + 1);
            if ((index - cellCountEachRow) >= 0) list.Add(index - cellCountEachRow);
            if ((index + cellCountEachRow) <= cellCountEachRow * cellCountEachRow) list.Add(index + cellCountEachRow);
        }

        /// <summary>
        /// 检查BingoCell所在行是否可以bingo
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        protected virtual bool CheckRowBingo(int index)
        {
            var row = index / cellCountEachRow;
            for (var i = row * cellCountEachRow; i < row * cellCountEachRow + cellCountEachRow; i++)
            {
                if (!CheckCellBingo(i)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 完成BingoCell所在行的Bingo
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        protected void RowBingo(int index)
        {
            var row = index / cellCountEachRow;
            for (var i = row * cellCountEachRow; i < row * cellCountEachRow + cellCountEachRow; i++) { bingoDic[i].TryBingo(); }
            AfterRowBingo(index);
        }

        /// <summary>
        /// 完成BingoCell所在行的Bingo后执行的逻辑
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        protected virtual void AfterRowBingo(int index) { result |= BingoResult.RowBingo; }

        /// <summary>
        /// 检查BingoCell所在列是否可以bingo
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        protected virtual bool CheckColumnBingo(int index)
        {
            var column = index % cellCountEachRow;
            for (var i = column; i < cellCountEachRow * cellCountEachRow; i += cellCountEachRow)
            {
                if (!CheckCellBingo(i)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 完成BingoCell所在列的bingo
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        protected void ColumnBingo(int index)
        {
            var column = index % cellCountEachRow;
            for (var i = column; i < cellCountEachRow * cellCountEachRow; i += cellCountEachRow) { bingoDic[i].TryBingo(); }
            AfterColumnBingo(index);
        }

        /// <summary>
        /// 完成BingoCell所在列的bingo后执行的逻辑
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        protected virtual void AfterColumnBingo(int index) { result |= BingoResult.ColumnBingo; }

        /// <summary>
        /// 检查BingoCell是否在主对角线上
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        protected bool IsOnMainDiagonal(int index)
        {
            var row = index / cellCountEachRow;
            var column = index % cellCountEachRow;
            return row == column;
        }

        /// <summary>
        /// 检查主对角线是否可以bingo
        /// </summary>
        protected virtual bool CheckMainDiagonal()
        {
            for (var i = 0; i < cellCountEachRow; i++)
            {
                if (!CheckCellBingo(i * cellCountEachRow + i)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 完成主对角线的bingo
        /// </summary>
        protected void MainDiagonalBingo()
        {
            for (var i = 0; i < cellCountEachRow; i++) { bingoDic[i * cellCountEachRow + i].TryBingo(); }
            AfterMainDiagonalBingo();
        }

        /// <summary>
        /// 完成主对角线的bingo后执行的逻辑
        /// </summary>
        protected virtual void AfterMainDiagonalBingo() { result |= BingoResult.MainDiagonalBingo; }

        /// <summary>
        /// 检查BingoCell是否在副对角线上
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        protected bool IsOnAntiDiagonal(int index)
        {
            var row = index / cellCountEachRow;
            var column = index % cellCountEachRow;
            return row + column == cellCountEachRow - 1;
        }

        /// <summary>
        /// 检查副对角线是否可以bingo
        /// </summary>
        protected bool CheckAntiDiagonal()
        {
            for (var i = 0; i < cellCountEachRow; i++)
            {
                if (!CheckCellBingo(i * cellCountEachRow + cellCountEachRow - i - 1)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 完成副对角线的bingo
        /// </summary>
        protected void AntiDiagonalBingo()
        {
            for (var i = 0; i < cellCountEachRow; i++) { bingoDic[i * cellCountEachRow + cellCountEachRow - i - 1].TryBingo(); }
            AfterAntiDiagonalBingo();
        }

        /// <summary>
        /// 完成副对角线的bingo后执行的逻辑
        /// </summary>
        protected virtual void AfterAntiDiagonalBingo() { result |= BingoResult.AntiDiagonalBingo; }

        /// <summary>
        /// 检查BingoCell是否满足bingo条件
        /// </summary>
        /// <param name="index">BingoCell序号</param>
        public virtual bool CheckCellBingo(int index)
        {
            return bingoDic.TryGetValue(index, out var cell) && (cell.state == BingoState.Completed || cell.state == BingoState.Bingo);
        }
    }
    #endregion
}
