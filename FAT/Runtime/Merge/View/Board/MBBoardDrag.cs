/*
 * @Author: qun.chao
 * @Date: 2021-02-19 11:00:53
 */

using EL;

namespace FAT
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using FAT.Merge;

    public class MBBoardDrag : MonoBehaviour, IMergeBoard, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool IsDraging => mIsDrag;

        private int width;
        private int height;
        private float mScale => BoardUtility.screenToCanvasCoe;
        private Vector2 mOrigin => BoardUtility.originPosInScreenSpace;
        private Vector2Int mTempPos;
        private Vector2 mBeginDragPos;
        private bool mIsDrag = false;

        void IMergeBoard.Init()
        { }

        void IMergeBoard.Setup(int w, int h)
        {
            width = w;
            height = h;
        }

        void IMergeBoard.Cleanup()
        { }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.pointerId > 0) return;
            BoardViewManager.Instance.OnUserActive();
            mIsDrag = true;
            mBeginDragPos = eventData.position;

            _CalcAnchoredCoord(eventData.pressPosition, ref mTempPos);
            BoardViewManager.Instance.OnBeginDragAtTile(mTempPos.x, mTempPos.y, eventData.position);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId > 0) return;
            MessageCenter.Get<MSG.GAME_BOARD_TOUCH>().Dispatch();
            BoardViewManager.Instance.OnDrag(mScale * (eventData.position - mBeginDragPos), eventData.position);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId > 0) return;
            if (!IsDraging) return;
            EndDrag(eventData.position);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerId > 0) return;
            MessageCenter.Get<MSG.GAME_BOARD_TOUCH>().Dispatch();
            // drag生效后 不再响应click
            if (IsDraging) return;
            BoardViewManager.Instance.OnUserActive();
            _CalcAnchoredCoord(eventData.pressPosition, ref mTempPos);
            BoardViewManager.Instance.OnClickAtTile(mTempPos.x, mTempPos.y);
        }

        private void EndDrag(Vector2 pos)
        {
            mIsDrag = false;
            BoardViewManager.Instance.OnUserActive();
            BoardViewManager.Instance.OnEndDrag(pos);
        }

        private void Update()
        {
            if (IsDraging)
            {
                if (Input.touchCount < 1 &&
                    !Input.GetMouseButton(0) &&
                    !Input.GetMouseButton(1) &&
                    !Input.GetMouseButton(2))
                {
                    Debug.LogError("unexpected EndDrag");
                    EndDrag(mBeginDragPos);
                }
            }
        }

        private void _CalcAnchoredCoord(Vector2 pos, ref Vector2Int result)
        {
            result.x = Mathf.FloorToInt(mScale * (pos.x - mOrigin.x) / BoardUtility.cellSize);
            result.y = -Mathf.CeilToInt(mScale * (pos.y - mOrigin.y) / BoardUtility.cellSize);
        }
    }
}