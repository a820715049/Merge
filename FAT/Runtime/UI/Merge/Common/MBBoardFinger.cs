/*
 * @Author: qun.chao
 * @Date: 2022-02-24 20:31:10
 */
namespace FAT
{
    using UnityEngine;
    using DG.Tweening;
    using Merge;
    using EL;

    public class MBBoardFinger : MonoBehaviour
    {
        enum AnimType
        {
            Match,      // 正常匹配
            Tap,        // 点选单位
            Drag,       // 拖拽单位 -> merge
            DragPos, // 拖拽单位到位置
            None,
        }

        private readonly float matchWaitDuration = 60f;
        private Transform mIndicator;
        private Tween mTweenAnim;
        private float mLastActTime;
        private float mAnimFixedIntervalTime = 0.5f;
        private float mMatchWaitTime = 0f;
        private float mTapWaitTime = 0f;
        private float mLastWaitTime = 0f;

        private bool mShowTapAnim = false;
        private AnimType mAnimType = AnimType.None;
        private Item mTapItem = null;
        private Item mDragFromItem = null;
        private Item mDragToItem = null;
        private Vector2 mDragPos;

        // 为简化guide相关配置 在guide中默认不再显示finger | 除非guide要求显示
        private bool mForceShowInGuide = false;

        public void Setup()
        {
            mIndicator = transform.GetChild(0);
        }

        public void InitOnPreOpen()
        {
            mLastWaitTime = 0f;
            mLastActTime = Time.time;

            _RefreshMatchWaitTime();
            _ClearAnim();

            mIndicator.gameObject.SetActive(false);
            MessageCenter.Get<MSG.GUIDE_QUIT>().AddListener(_OnMessageGuideQuit);
            MessageCenter.Get<MSG.GUIDE_CLOSE>().AddListener(_OnMessageGuideClose);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_Hide>().AddListener(_OnMessageFingerHide);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_TAP>().AddListener(_OnMessageGuideFingerTap);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG>().AddListener(_OnMessageGuideFingerDrag);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG_POS>().AddListener(_OnMessageGuideFingerDragPos);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_MATCH>().AddListener(_OnMessageGuideFingerMatch);
        }

        public void CleanupOnPostClose()
        {
            mForceShowInGuide = false;
            _OnCancelAnim();
            _ClearAnim();
            MessageCenter.Get<MSG.GUIDE_QUIT>().RemoveListener(_OnMessageGuideQuit);
            MessageCenter.Get<MSG.GUIDE_CLOSE>().RemoveListener(_OnMessageGuideClose);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_Hide>().RemoveListener(_OnMessageFingerHide);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_TAP>().RemoveListener(_OnMessageGuideFingerTap);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG>().RemoveListener(_OnMessageGuideFingerDrag);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG_POS>().RemoveListener(_OnMessageGuideFingerDragPos);
            MessageCenter.Get<MSG.UI_GUIDE_FINGER_MATCH>().RemoveListener(_OnMessageGuideFingerMatch);
        }

        public void _GuideForceShowFinger()
        {
            mForceShowInGuide = true;
        }

        private void _ClearAnim()
        {
            mAnimType = AnimType.None;
            mTapItem = null;
            mDragFromItem = null;
            mDragToItem = null;
        }

        private void Update()
        {
            if (Input.touchSupported)
            {
                if (Input.touchCount > 0)
                {
                    mLastActTime = Time.time;
                    _OnCancelAnim();
                    return;
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    mLastActTime = Time.time;
                    _OnCancelAnim();
                    return;
                }
            }

            // 拖拽中不能发起
            if (BoardViewManager.Instance.IsDragItem())
            {
                mLastActTime = Time.time;
                return;
            }

            _TryPlayAnim();
        }

        private void _RefreshMatchWaitTime()
        {
            // if (GuideMergeManager.Instance.IsDelayShowBoardMatchFinger())
            // {
            //     mMatchWaitTime = matchWaitDuration;
            // }
            // else
            {
                mMatchWaitTime = 1.5f;
            }
        }

        private void _OnCancelAnim()
        {
            if (mTweenAnim != null)
            {
                mTweenAnim.Kill();
                mTweenAnim = null;
            }

            if (mIndicator.gameObject.activeSelf)
            {
                mIndicator.gameObject.SetActive(false);
            }

            mLastWaitTime = 0f;
        }

        private void _OnTweenFinished()
        {
            _RefreshMatchWaitTime();

            mTweenAnim = null;

            // 动画已经发起 无需再等待启动时间 于是减去上次的等待时间
            mLastActTime = Time.time - mLastWaitTime;

            mIndicator.gameObject.SetActive(false);
        }

        private void _TryPlayAnim()
        {
            if (mTweenAnim != null)
                return;

            if (!mForceShowInGuide)
            {
                // 非强制显示时 guide进行中不再显示finger
                if (UIManager.Instance.IsOpen(UIConfig.UIGuide))
                    return;
            }

            if (mForceShowInGuide)
            {
                // 无需等待afk时间
                if (Time.time - mLastActTime < mAnimFixedIntervalTime)
                {
                    return;
                }
                else
                {
                    mLastWaitTime = 0f;

                    // 按标记来提示
                    switch (mAnimType)
                    {
                        case AnimType.Tap:
                            _TapAnim();
                            break;
                        case AnimType.Drag:
                            _DragAnim();
                            break;
                        case AnimType.DragPos:
                            _DragPosAnim();
                            break;
                        case AnimType.Match:
                        default:
                            _MergeAnim();
                            break;
                    }
                }
            }
            else
            {
                // // 非guide 可以根据设置显示tip
                // if (GuideManager.Instance.IsShowFinger())
                // {
                //     // 按能力提示
                //     if (BoardViewManager.Instance.checker.HasMatchPair())
                //     {
                //         if (Time.time - mLastActTime > mAnimFixedIntervalTime + mMatchWaitTime)
                //         {
                //             mLastWaitTime = mMatchWaitTime;
                //             _MergeAnim();
                //         }
                //         return;
                //     }
                //     else if (BoardViewManager.Instance.checker.HasTapTarget())
                //     {
                //         if (Time.time - mLastActTime > mAnimFixedIntervalTime + mTapWaitTime)
                //         {
                //             mLastWaitTime = mTapWaitTime;
                //             _ClickSourceAnim();
                //         }
                //         return;
                //     }
                // }

                // 1. 未显示其他tip，可以尝试ABTest
                // 2. 常规tip发起后，ABTest自动被覆盖
                _PerformAB();
            }
        }

        #region anim

        private void _ClickSourceAnim()
        {
            if (BoardViewManager.Instance.checker.HasTapTarget())
            {
                _PlayTap(BoardViewManager.Instance.checker.GetTapTargetCoord());
            }
        }

        private void _TapAnim()
        {
            if (mTapItem != null && mTapItem.parent != null && !mTapItem.isDead)
            {
                _PlayTap(mTapItem.coord);
            }
        }

        private void _MergeAnim()
        {
            if (BoardViewManager.Instance.checker.HasMatchPair())
            {
                var pairs = BoardViewManager.Instance.checker.GetMatchPairCoords();
                _PlayMatch(_AdjustCoordToDragPos(pairs.Item1), _AdjustCoordToDragPos(pairs.Item2));
            }
        }

        private void _DragAnim()
        {
            if (mDragFromItem != null && mDragToItem != null)
            {
                _PlayMatch(_AdjustCoordToDragPos(mDragFromItem.coord), _AdjustCoordToDragPos(mDragToItem.coord));
            }
        }

        private void _DragPosAnim()
        {
            if (mDragFromItem != null)
            {
                _PlayMatch(_AdjustCoordToDragPos(mDragFromItem.coord), mDragPos);
            }
        }

        private Vector2 _AdjustCoordToDragPos(Vector2 coord)
        {
            return coord + Vector2.one * 0.5f;
        }

        private void _PlayMatch(Vector2 from, Vector2 to)
        {
            var origin = BoardUtility.originPosInScreenSpace;
            var cellSize = BoardUtility.cellSize;
            var scale = BoardUtility.canvasToScreenCoe;

            var screen_p1 = origin + new Vector2(cellSize * from.x, -cellSize * from.y) * scale;
            var screen_p2 = origin + new Vector2(cellSize * to.x, -cellSize * to.y) * scale;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, screen_p1, null, out var p1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, screen_p2, null, out var p2);

            mIndicator.gameObject.SetActive(true);
            mIndicator.localScale = Vector3.one;
            mTweenAnim = mIndicator.DOLocalMove(p2, 1f).From(p1, true).SetEase(Ease.InOutCirc).OnComplete(_OnTweenFinished);
        }

        private void _PlayTap(Vector2Int coord)
        {
            var origin = BoardUtility.originPosInScreenSpace;
            var cellSize = BoardUtility.cellSize;
            var scale = BoardUtility.canvasToScreenCoe;

            var screen_p1 = origin + new Vector2(cellSize * coord.x + cellSize * 0.5f, -cellSize * coord.y - cellSize * 0.5f) * scale;
            Vector2 p1;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, screen_p1, null, out p1);

            mIndicator.gameObject.SetActive(true);
            mIndicator.transform.localPosition = p1;

            mTweenAnim = mIndicator.DOScale(new Vector3(1.1f, 1.1f, 1.1f), 0.5f).From(Vector3.one, true).SetLoops(2, LoopType.Yoyo).OnComplete(_OnTweenFinished);
        }

        private void _PlayPointer(Transform target)
        {
            var screen_p = RectTransformUtility.WorldToScreenPoint(null, target.position);
            Vector2 p1;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, screen_p, null, out p1);

            mIndicator.gameObject.SetActive(true);
            mIndicator.transform.localPosition = p1;

            mTweenAnim = mIndicator.DOScale(new Vector3(1.1f, 1.1f, 1.1f), 0.5f).From(Vector3.one, true).SetLoops(2, LoopType.Yoyo).OnComplete(_OnTweenFinished);
        }

        #endregion

        #region message

        private void _OnMessageGuideQuit()
        {
            mForceShowInGuide = false;
        }

        private void _OnMessageGuideClose()
        {
            mForceShowInGuide = false;
        }

        private void _OnMessageFingerHide()
        {
            mForceShowInGuide = false;
        }

        private void _OnMessageGuideFingerTap(Item item)
        {
            _ClearAnim();
            mTapItem = item;
            mAnimType = AnimType.Tap;
            _GuideForceShowFinger();
        }

        private void _OnMessageGuideFingerDrag(Item from, Item to)
        {
            _ClearAnim();
            mDragFromItem = from;
            mDragToItem = to;
            mAnimType = AnimType.Drag;
            _GuideForceShowFinger();
        }

        private void _OnMessageGuideFingerDragCoord(Item from, Vector2 pos)
        {
            _ClearAnim();
            mDragFromItem = from;
            mDragPos = pos;
            mAnimType = AnimType.DragPos;
            _GuideForceShowFinger();
        }

        private void _OnMessageGuideFingerDragPos(Item from, Vector2 pos)
        {
            _ClearAnim();
            mDragFromItem = from;
            mDragPos = pos;
            mAnimType = AnimType.DragPos;
            _GuideForceShowFinger();
        }

        private void _OnMessageGuideFingerMatch()
        {
            _ClearAnim();
            mAnimType = AnimType.Match;
            _GuideForceShowFinger();
        }

        #endregion

        #region A/B TEST
        private bool _PerformAB()
        {
            // if (UIUtility.IsFreeSpeedupTestGroupB())
            // {
            //     _ChestSpeedUpGuide();
            //     return true;
            // }
            return false;
        }

        #region 宝箱free加速

        private float mLastCheckTime = -1f;

        private void _TipForFirstReward()
        {
            var obj = BoardViewWrapper.GetParam(BoardViewWrapper.ParamType.CompRewardTrack);
            if (obj != null)
            {
                if (obj is MBMergeCompReward reward)
                {
                    var tar = reward.FirstRewardTrans();
                    _PlayPointer(tar);
                }
            }
        }

        private void _TipForSelect(Vector2Int coord)
        {
            _PlayTap(coord);
        }

        // private void _TipForOpen()
        // {
        //     var tar = _FindChestOpenButton();
        //     if (tar != null)
        //     {
        //         _PlayPointer(tar);
        //     }
        // }

        // private void _TipForSpeedup()
        // {
        //     var tar = _FindChestSpeedUpButton();
        //     if (tar != null)
        //     {
        //         _PlayPointer(tar);
        //     }
        // }

        private Item _FindBoardItemForFreeSpeedup(Board board)
        {
            var w = board.size.x;
            var h = board.size.y;
            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; y++)
                {
                    var item = board.GetItemByCoord(x, y);
                    if (item != null && _IsFreeSpeedupChest(item))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        // private Transform _FindChestOpenButton()
        // {
        //     var obj = BoardViewWrapper.GetParam(BoardViewWrapper.ParamType.CompItemInfo);
        //     if (obj != null)
        //     {
        //         if (obj is MBBoardItemInfo info)
        //         {
        //             return info.GetChestOpenButton();
        //         }
        //     }
        //     return null;
        // }

        // private Transform _FindChestSpeedUpButton()
        // {
        //     var obj = BoardViewWrapper.GetParam(BoardViewWrapper.ParamType.CompItemInfo);
        //     if (obj != null)
        //     {
        //         if (obj is MBBoardItemInfo info)
        //         {
        //             return info.GetChestSpeedupButton();
        //         }
        //     }
        //     return null;
        // }

        private Item _GetSelectedBoardItem()
        {
            return BoardViewManager.Instance.GetCurrentBoardInfoItem();
        }

        private bool _IsBoardItemSelected(Item item)
        {
            var _sel = _GetSelectedBoardItem();
            if (item == null || _sel == null)
                return false;
            return item.id == _sel.id;
        }

        private bool _IsFreeSpeedupChest(Item item)
        {
            if (item.TryGetItemComponent(out ItemChestComponent chest))
            {
                if (!chest.canUse && ItemUtility.CanUseGlobalFreeSpeedup(item.tid) && ItemUtility.IsSupportSpeedup(item.tid))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #endregion
    }
}