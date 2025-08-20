/*
 * @Author: qun.chao
 * @Date: 2021-02-24 17:49:25
 */

using EL;
using FAT.MSG;
using fat.rawdata;

namespace FAT
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Merge;
    using DG.Tweening;

    public class MatchChecker
    {
        private Item mMatchHintItemA;
        private Item mMatchHintItemB;
        private Item mTapTarget;
        private int mSelectedTid;
        private bool mHasMatchItem; //判断棋盘上是否有可以匹配的棋子(能够合成或者被对方吃掉)

        private List<Item> mCache = new List<Item>();

        private Coroutine mCoFindTapSource;
        private float mResolveTime;
        private Sequence mMatchSeq;

        public void Setup()
        { }

        public void Cleanup()
        {
            mSelectedTid = 0;

            mBoardGridIgnoreMap = 0;

            mHasMatchItem = false;
            mMatchHintItemA = null;
            mMatchHintItemB = null;

            mCache.Clear();

            StopMatchHintAnim();
            _StopFindTapTarget();
        }

        #region 格子标记

        // 棋盘尺寸 7x9
        // 可以用64位整数对格子进行标记 | 目前服务于特效表现
        // 现在用于忽略某些格子参与匹配逻辑
        private ulong mBoardGridIgnoreMap;
        private const int default_board_width = 7;

        public void MarkCoord(int coord_x, int coord_y)
        {
            mBoardGridIgnoreMap |= 1UL << _CoordToBoardIdx(coord_x, coord_y);
        }

        public void UnmarkCoord(int coord_x, int coord_y)
        {
            mBoardGridIgnoreMap &= ~(1UL << _CoordToBoardIdx(coord_x, coord_y));
        }

        public bool IsCoordMarked(Vector2Int coord)
        {
            var idx = _CoordToBoardIdx(coord.x, coord.y);
            return (mBoardGridIgnoreMap & (1UL << idx)) != 0;
        }

        private int _CoordToBoardIdx(int coord_x, int coord_y)
        {
            return default_board_width * coord_y + coord_x;
        }

        #endregion

        public bool HasMatchPair()
        {
            if (BoardUtility.isBoardCheckerPaused)
                return false;
            return mHasMatchItem;
        }

        private bool IsMatchHintItemReady()
        {
            var itemA = mMatchHintItemA;
            var itemB = mMatchHintItemB;

            if (itemA == null || itemB == null)
                return false;
            if (itemA.isDead || itemB.isDead)
                return false;
            if (itemA.parent == null || itemB.parent == null)
                return false;

            // 额外的匹配动画
            var mgr = BoardViewManager.Instance;
            var v1 = mgr.boardView.boardHolder.FindItemView(itemA.id);
            var v2 = mgr.boardView.boardHolder.FindItemView(itemB.id);
            if (v1 == null || v2 == null)
            {
                // item有可能还未落地 / 比如从背包延迟发出
                return false;
            }
            if (!v1.IsViewIdle() || !v2.IsViewIdle())
                return false;            

            return true;
        }

        public bool ShouldPlayMatchHintAnim()
        {
            return HasMatchPair() && IsMatchHintItemReady() && mResolveTime > 0 && (Time.time - mResolveTime) > 2f;
        }

        public void PlayMatchHintAnim()
        {
            mResolveTime = -1f;
            var mgr = BoardViewManager.Instance;
            var itemA = mMatchHintItemA;
            var itemB = mMatchHintItemB;

            DataTracker.TrackMergeActionHint(itemA, itemB);

            // 额外的匹配动画
            var v1 = mgr.boardView.boardHolder.FindItemView(itemA.id);
            var v2 = mgr.boardView.boardHolder.FindItemView(itemB.id);

            if (v1 == null || v2 == null)
            {
                // item有可能还未落地 / 比如从背包延迟发出
                return;
            }

            var transA = v1.transform;
            var transB = v2.transform;
            mgr.ReAnchorItemForPair(v1.transform);
            mgr.ReAnchorItemForPair(v2.transform);

            var isMatchForConsume = ItemUtility.CanConsume(itemA, itemB);
            if (isMatchForConsume)
            {
                // 如果A可以被B吃 则A本体与B角标进行match动画播放
                transB = v2.tapCostComp;
            }

            // 两个物品向连线方向靠近
            float trans_duration = 1.0f;            // 单程时间
            // 避免动画过程移动太远 用local 40换算出world坐标下的距离作为移动上限
            var maxOffset = transA.parent.TransformVector(40f, 0, 0).magnitude;
            var dist = transB.position - transA.position;
            var offset = dist.magnitude * 0.1f;
            if (offset > maxOffset) offset = maxOffset;
            var dir = dist.normalized;
            var p1_from = transA.position;
            var p2_from = transB.position;
            var p1_to = transA.position + dir * offset;
            var p2_to = transB.position - dir * offset;

            mMatchSeq = _BuildMatchAnim(transA, transB, trans_duration, p1_from, p1_to, p2_from, p2_to);
            MessageCenter.Get<MSG.GAME_REMIND_MERGE_START>().Dispatch();
        }

        private Sequence _BuildMatchAnim(Transform src, Transform dst, float duration_move, Vector3 src_from, Vector3 src_to, Vector3 dst_from, Vector3 dst_to)
        {
            Vector3 scaleFrom = Vector3.one;         // 原始尺寸
            Vector3 scaleTo = Vector3.one * 1.2f;   // 放大后尺寸

            var seq = DOTween.Sequence();
            // 位移
            seq.Append(src.DOMove(src_to, duration_move).From(src_from, true).SetEase(Ease.InOutCubic));
            seq.Join(dst.DOMove(dst_to, duration_move).From(dst_from, true).SetEase(Ease.InOutCubic));
            // 缩放
            seq.Join(src.DOScale(scaleTo, duration_move).From(scaleFrom, true).SetEase(Ease.InOutCubic));
            seq.Join(dst.DOScale(scaleTo, duration_move).From(scaleFrom, true).SetEase(Ease.InOutCubic));

            seq.AppendInterval(0.6f);

            // 位移 归位
            seq.Append(src.DOMove(src_from, duration_move).SetEase(Ease.InOutCubic));
            seq.Join(dst.DOMove(dst_from, duration_move).SetEase(Ease.InOutCubic));
            // 缩放 还原
            seq.Join(src.DOScale(scaleFrom, duration_move).SetEase(Ease.InOutCubic));
            seq.Join(dst.DOScale(scaleFrom, duration_move).SetEase(Ease.InOutCubic));

            seq.AppendInterval(0.3f);
            seq.SetLoops(-1, LoopType.Restart);
            seq.Play();
            seq.OnKill(() =>
            {
                if (src != null)
                {
                    src.localScale = scaleFrom;
                    src.position = src_from;
                }
                if (dst != null)
                {
                    dst.localScale = scaleFrom;
                    dst.position = dst_from;
                }
            });
            return seq;
        }

        // 仅停止动画
        public void StopMatchHintAnim()
        {
            mMatchSeq?.Kill();
            mMatchSeq = null;

            mResolveTime = Time.time;
            var mgr = BoardViewManager.Instance;
            var itemA = mMatchHintItemA;
            var itemB = mMatchHintItemB;
            if (itemA != null)
            {
                // mgr.RemoveMatchHint(itemA.id);
                mgr.HoldItemIfNotInMoveLayer(itemA);
            }
            if (itemB != null)
            {
                // mgr.RemoveMatchHint(itemB.id);
                mgr.HoldItemIfNotInMoveLayer(itemB);
            }
            MessageCenter.Get<GAME_REMIND_MERGE_END>().Dispatch();
        }

        public (Vector2Int, Vector2Int) GetMatchPairCoords()
        {
            return (mMatchHintItemA.coord, mMatchHintItemB.coord);
        }

        public bool HasTapTarget()
        {
            if (BoardUtility.isBoardCheckerPaused)
                return false;
            return mTapTarget != null && (Time.time - mResolveTime) > (float)Game.Manager.configMan.globalConfig.MergeRemindTriggerTime / 1000;
        }

        public Vector2Int GetTapTargetCoord()
        {
            return mTapTarget.coord;
        }

        public void SetMatchTid(int tid)
        {
            mSelectedTid = tid;

            _FindMatch();
        }

        public void FindMatch(bool ignoreConsume = false)
        {
            _FindMatch(ignoreConsume);
        }

        public int GetMatchItem()
        {
            if (mMatchHintItemA != null && mMatchHintItemB != null)
                return mMatchHintItemA.tid;
            else
                return 0;
        }

        public void CheckHint(bool forceCheck = false)
        {
            var orderRequireCache = BoardViewWrapper.GetBoardOrderRequireItemStateCache();
            // 优先匹配生成器 如果已经是生成器 则不用重新查找
            var itemA = mMatchHintItemA;
            var itemB = mMatchHintItemB;
            if (!forceCheck &&
                itemA != null && itemB != null &&
                !itemA.isDead && !itemB.isDead &&
                itemA.parent != null && itemB.parent != null &&
                !IsCoordMarked(itemA.coord) && !IsCoordMarked(itemB.coord) && orderRequireCache != null &&                                             // item所在格子不能被标记
                !orderRequireCache.TryGetValue(itemA.tid, out _) && !orderRequireCache.TryGetValue(itemB.tid, out _) &&     // 不能是订单正在要的物品
                (itemA.HasComponent(ItemComponentType.ClickSouce) || itemA.HasComponent(ItemComponentType.AutoSouce) || itemA.HasComponent(ItemComponentType.Chest)))
            {
                // 上次的match有效
                return;
            }

            _FindMatch();
        }

        private void _TryFindTapTarget()
        {
            // mCoFindTapSource = Game.Instance.mainEntry.StartCoroutine(_CoTryFindOrderItemTapTarget());
        }

        private void _StopFindTapTarget()
        {
            if (mCoFindTapSource != null)
            {
                Game.Instance.mainEntry.StopCoroutine(mCoFindTapSource);
                mCoFindTapSource = null;
            }
            mTapTarget = null;
        }

        private void _FindMatch(bool ignoreConsume = false)
        {
            _StopFindTapTarget();

            var mgr = BoardViewManager.Instance;
            var board = mgr.board;
            mHasMatchItem = false;

            // remove old hint
            StopMatchHintAnim();

            mMatchHintItemA = null;
            mMatchHintItemB = null;
            mTapTarget = null;

            mCache.Clear();
            mgr.board.WalkAllItem(_FillItem);

            mCache.Sort(_Sort);

            // 需要优先展示可解锁的item 所以找到匹配先记录为候选
            Item _candidateItemA = null;
            Item _candidateItemB = null;
            Item _item = null;

            var orderRequireCache = BoardViewWrapper.GetBoardOrderRequireItemStateCache();

            for (int i = 0; i < mCache.Count; i++)
            {
                _item = mCache[i];
                if (_item.isActive)
                {
                    for (int j = i + 1; j < mCache.Count; j++)
                    {
                        //ignoreConsume 忽略检测消耗棋子的行为
                        if (!ignoreConsume && ItemUtility.CanConsumeAny(_item, mCache[j], out var consume, out var dst))
                        {
                            _SetMatch(consume, dst);
                            return;
                        }

                        if (board.CanMerge(_item, mCache[j]))
                        {
                            // 优先提示生成器
                            // 优先提示非蜘蛛网
                            if (mCache[j].isFrozen)
                            {
                                _candidateItemA = _item;
                                _candidateItemB = mCache[j];
                            }
                            else
                            {
                                if (_item.HasComponent(ItemComponentType.ClickSouce) ||
                                    _item.HasComponent(ItemComponentType.AutoSouce) ||
                                    _item.HasComponent(ItemComponentType.Chest))
                                {
                                    // 找到合适的匹配项目
                                    _SetMatch(_item, mCache[j]);
                                    return;
                                }
                                else
                                {
                                    if (_candidateItemA == null || _candidateItemB == null)
                                    {
                                        _candidateItemA = _item;
                                        _candidateItemB = mCache[j];
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (_candidateItemA != null && _candidateItemB != null)
            {
                _SetMatch(_candidateItemA, _candidateItemB);
            }
            else
            {
                // 没有有效的match项 尝试提示tap
                _TryFindTapTarget();
            }
        }

        private void _SetMatch(Item a, Item b)
        {
            mResolveTime = Time.time;

            mMatchHintItemA = a;
            mMatchHintItemB = b;

            mHasMatchItem = true;
        }

        private int _Sort(Item a, Item b)
        {
            if (a.isActive && b.isActive)
                return a.id - b.id;
            if (a.isActive)
                return -1;
            if (b.isActive)
                return 1;
            return a.id - b.id;
        }

        private void _FillItem(Item item)
        {
            if (IsCoordMarked(item.coord))
                return;

            // 可用于任务的item不提示匹配效果
            if (ItemUtility.CanUseInOrder(item))
            {
                if (BoardViewWrapper.IsNeededByTopBarOrder(item.tid))
                    return;
            }

            // 没有特殊格的需求了
            // if (item.grid != null && BoardUtility.FiilMatchItemList(item.coord) > 0)
            // {
            //     // 在特殊格的物品不提示
            //     return;
            // }

            if (mSelectedTid <= 0 || mSelectedTid == item.tid)
                mCache.Add(item);
        }
    }
}