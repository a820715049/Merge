/*
 * @Author: qun.chao
 * @Date: 2022-03-07 20:43:50
 */
using UnityEngine;
using EL;

namespace FAT
{
    public class UIGuideBoardTap : MonoBehaviour
    {
        private RectTransform clickArea;
        private bool isUse = false;
        private int coord_x;
        private int coord_y;

        public void Setup()
        {
            clickArea = transform.Find("ClickArea") as RectTransform;
            clickArea.AddButton(null, _OnBtnClick);
        }

        public void InitOnPreOpen()
        {
            _Hide();
        }

        public void CleanupOnPostClose()
        {
            _Hide();
        }

        public void Hide()
        {
            _Hide();
        }

        private void _Hide()
        {
            clickArea.gameObject.SetActive(false);
        }

        /// <summary>
        /// 只显示mask 无操作
        /// </summary>
        public void ShowMask(int x, int y)
        {
            _SetPointer(x, y);
            _SetMask();
            _Hide();
        }

        public void ShowSelectTarget(int x, int y, bool setMask)
        {
            isUse = false;
            _SetPointer(x, y);
            if (setMask) _SetMask();
            _SetArrow();
        }

        public void ShowUseTarget(int x, int y, bool setMask)
        {
            isUse = true;
            _SetPointer(x, y);
            if (setMask) _SetMask();
            _SetArrow();
        }

        private void _SetMask()
        {
            _GetContext().ShowMask(clickArea);
        }

        private void _SetArrow()
        {
            _GetContext().ShowForcePointer(clickArea, null);
        }

        private void _SetPointer(int x, int y)
        {
            coord_x = x;
            coord_y = y;
            clickArea.gameObject.SetActive(true);

            Vector2 screenPos = BoardUtility.GetScreenPosByCoord(x, y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(clickArea.parent as RectTransform, screenPos, null, out Vector2 localPos);
            clickArea.anchoredPosition = localPos;

            var size = BoardUtility.cellSize;
            clickArea.sizeDelta = new Vector2(size, size) * 1.5f;
        }

        private void _OnBtnClick()
        {
            // 等效于select目标
            if (isUse)
            {
                BoardViewManager.Instance.RefreshInfo(coord_x, coord_y);
                BoardViewManager.Instance.OnClickAtTile(coord_x, coord_y);
            }
            else
            {
                BoardViewManager.Instance.RefreshInfo(coord_x, coord_y);
            }

            _Hide();

            // resolve
            var _context = _GetContext();
            _context.UserTapBoardTarget();

            _context.HidePointer();
            // auto hide mask
            _context.HideMask();
        }

        private UIGuideContext _GetContext()
        {
            return Game.Manager.guideMan.ActiveGuideContext;
        }
    }
}