/*
 * @Author: qun.chao
 * @Date: 2022-06-13 16:29:03
 */
using System.Collections.Generic;
using UnityEngine;
using fat.rawdata;

namespace FAT.Merge
{
    // 1. 时间缩放效果只对环绕格子生效 不包括中心
    // 2. 不叠加 取效果最大的
    public class MergeAdjacentEffectTimeScale : MergeAdjacentEffect
    {
        // 记录周围8格是否存在IimeScale道具
        private int[] mFlagTable;
        // 记录当前格上次查询时的tid
        private int[] mTidTable;
        // 记录当前格上次查询得到的scale
        private int[] mScaleTable;

        // 棋盘上所有的TimeScale道具
        private Dictionary<int, Vector2Int> mTimeScaleItemDict = new Dictionary<int, Vector2Int>();
        private bool mRegistered = false;
        private Board mParent;

        public void Reset(int col, int row, Board parent)
        {
            base.Init(col, row);
            mParent = parent;
            mFlagTable = new int[col * row];
            mTidTable = new int[col * row];
            mScaleTable = new int[col * row];
            mTimeScaleItemDict.Clear();

            if (!mRegistered)
            {
                parent.onItemEnter += _OnItemEnter;
                parent.onItemLeave += _OnItemLeave;
                parent.onItemMove += _OnItemMove;
                mRegistered = true;
            }
        }

        public void Deserialize(Item item)
        {
            _TryApplyTimeScaleEffect(item);
        }

        public void TriggerUseTimeScaleSource(Item item)
        {
            _TryApplyTimeScaleEffect(item);
        }

        public int CalculateTimeScale(Item item)
        {
            // 场上没有道具
            if (mTimeScaleItemDict.Count < 1)
                return 1;

            // 技能物品不受影响
            if (item.HasComponent(ItemComponentType.Skill))
                return 1;

            // 当前位置
            var _idx = _CalculateIdxByCoord(item.coord.x, item.coord.y);

            // 没有受影响
            var _flag = mFlagTable[_idx];
            if (_flag == 0)
                return 1;

            // 没有发生改变
            if (mTidTable[_idx] == item.tid && mScaleTable[_idx] > 0)
                return mScaleTable[_idx];

            // 受时间缩放影响 => 选一个最大的
            Item skillItem;
            int scale = -1;
            for (int i = 1; i < mAdjacentDir.Length; ++i)
            {
                if ((_flag & (1 << (i - 1))) != 0)
                {
                    skillItem = mParent.GetItemByCoord(item.coord.x + mAdjacentDir[i].x, item.coord.y + mAdjacentDir[i].y);
                    if (skillItem != null)
                    {
                        if (skillItem.TryGetItemComponent(out ItemSkillComponent skill) && skill.type == SkillType.Tesla)
                        {
                            // 通用 or 指定的链条
                            if (skill.param2.Count < 1 || skill.param2.Contains(Env.Instance.GetCategoryByItem(item.tid).Id))
                            {
                                if (scale < skill.param3[0])
                                {
                                    scale = skill.param3[0];
                                }
                            }
                        }
                    }
                }
            }

            if (scale < 0)
                scale = 1;

            mTidTable[_idx] = item.tid;
            mScaleTable[_idx] = scale;

            return scale;
        }

        public int GetNextTimeScaleItemLifeMilli()
        {
            var life = int.MaxValue;
            foreach (var kv in mTimeScaleItemDict)
            {
                var item = mParent.GetItemByCoord(kv.Value.x, kv.Value.y);
                if (item != null && item.TryGetItemComponent(out ItemSkillComponent skill))
                {
                    if (skill.teslaLeftMilli < life)
                    {
                        life = skill.teslaLeftMilli;
                    }
                }
            }
            return life;
        }

        private bool _TryApplyTimeScaleEffect(Item item)
        {
            if (mTimeScaleItemDict.ContainsKey(item.id))
                return false;
            if (!_IsTimeScaleItem(item))
                return false;
            mTimeScaleItemDict.Add(item.id, item.coord);
            _SetFlag(item.coord);
            mTimeScaleItemDict[item.id] = item.coord;
            return true;
        }

        private bool _IsTimeScaleItem(Item item)
        {
            if (item.TryGetItemComponent(out ItemSkillComponent skill)
                && skill.type == SkillType.Tesla
                && skill.teslaActive)
                return true;
            return false;
        }

        private void _SetFlag(Vector2Int coord)
        {
            void _Imp(int coordIdx, int dir)
            {
                if (coordIdx < 0) return;
                _SetAdjacentFlag(mFlagTable, coordIdx, dir);
                // 标记dirty
                mScaleTable[coordIdx] = -1;
            }
            _RoundTraverse(_Imp, coord);
        }

        private void _ClearFlag(Vector2Int coord)
        {
            void _Imp(int coordIdx, int dir)
            {
                if (coordIdx < 0) return;
                _ClearAdjacentFlag(mFlagTable, coordIdx, dir);
                // 标记dirty
                mScaleTable[coordIdx] = -1;
            }
            _RoundTraverse(_Imp, coord);
        }

        private void _OnItemEnter(Item item)
        {
            if (_TryApplyTimeScaleEffect(item))
            {
                Debug.LogFormat("tesla effect {0} enter at {1}", item.id, item.coord);
            }
        }

        private void _OnItemLeave(Item item)
        {
            if (!mTimeScaleItemDict.ContainsKey(item.id))
                return;
            Debug.LogFormat("tesla effect {0} leave at {1}", item.id, item.coord);
            var coord = mTimeScaleItemDict[item.id];
            _SetFlag(coord);
            mTimeScaleItemDict.Remove(item.id);
        }

        private void _OnItemMove(Item item)
        {
            if (!mTimeScaleItemDict.ContainsKey(item.id))
                return;
            var preCoord = mTimeScaleItemDict[item.id];
            _ClearFlag(preCoord);
            _SetFlag(item.coord);
            mTimeScaleItemDict[item.id] = item.coord;
            Debug.LogFormat("tesla effect move {0} from {1} to {2}", item.id, preCoord, item.coord);
        }
    }
}