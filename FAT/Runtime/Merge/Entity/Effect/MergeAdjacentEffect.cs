/*
 * @Author: qun.chao
 * @Date: 2022-06-13 11:22:46
 */
using System;
using UnityEngine;

namespace FAT.Merge
{
    public class MergeAdjacentEffect
    {
        protected int mCols;
        protected int mRows;

        /*
            1 2 3
            8 0 4
            7 6 5
        */
        protected Vector2Int[] mAdjacentDir =
        {
            Vector2Int.zero,
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
        };

        public void Init(int col, int row)
        {
            mCols = col;
            mRows = row;
        }

        protected int _ReverseDir(int dir)
        {
            return dir >= 5 ? dir - 4 : dir + 4;
        }

        protected void _RoundTraverse(Action<int, int> imp, Vector2Int coord)
        {
            int _idx;
            for (int i = 1; i < mAdjacentDir.Length; ++i)
            {
                _idx = _CalculateIdxByCoord(coord.x + mAdjacentDir[i].x, coord.y + mAdjacentDir[i].y);
                // 目标格子 index / dir
                imp?.Invoke(_idx, _ReverseDir(i));
            }
        }

        protected void _SetAdjacentFlag(int[] flag, int idx, int dir)
        {
            flag[idx] |= 1 << (dir - 1);
        }

        protected void _ClearAdjacentFlag(int[] flag, int idx, int dir)
        {
            flag[idx] &= ~(1 << (dir - 1));
        }

        protected int _CalculateIdxByCoord(int col, int row)
        {
            if (col < 0 || col >= mCols || row < 0 || row >= mRows)
            {
                return -1;
            }

            return row * mCols + col;
        }

        protected bool _CalculateCoordByIdx(int idx, out int col, out int row)
        {
            row = idx / mCols;
            col = idx % mCols;
            return true;
        }

    }
}