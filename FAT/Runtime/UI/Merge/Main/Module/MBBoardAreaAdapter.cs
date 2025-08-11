/*
 * @Author: qun.chao
 * @Date: 2023-10-25 12:18:12
 */

using EL;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FAT
{
    public class MBBoardAreaAdapter : UIBehaviour
    {
        /*
        美术要求棋盘区域按规则适配

        棋盘主要分为上下两区

        ----- 安全区边界(top)
        xxxxx 状态栏
        xxxxx 订单区
        ----- 分界bar上边界 => 安全区内80%处
        xxxxx 分界bar
        ----- 分界bar下边界
        xxxxx 棋盘区
        xxxxx 底部信息区
        ----- 安全区边界(bottom)

        bg在宽度不足时横向拉伸

        订单区 y方向锚点0.5比例
        棋盘区 y方向锚点0.5比例 向上偏移 剩余空白区域高度的60%尺寸
        */

        // 上下区分界比例
        [SerializeField] private float topBottomAnchorRef = 0.8f;
        // 状态栏尺寸
        [SerializeField] private float sizeStatusBar = 100f;
        // board整体居中 区域有空闲时向上偏移到60%比例处
        [SerializeField] private float boardOffsetPercent = 0.6f;
        [SerializeField] private float boardEdgeSpace = 20f;
        [SerializeField] private float rectOrderBottomExt = 20;
        [SerializeField] private float rectBoardTopExt = 80;
        [SerializeField][Range(1f, 10f)] private float maxScaleLimitForOrder = 2f;
        [SerializeField][Range(1f, 10f)] private float maxScaleLimitForBoard = 2f;
        [SerializeField] private RectTransform bgRoot;
        [SerializeField] private RectTransform bgTop;
        [SerializeField] private RectTransform bgBottom;
        [SerializeField] private RectTransform rectOrder;
        [SerializeField] private RectTransform rectBoard;
        [SerializeField] private RectTransform rectBottom;
        [SerializeField] private RectTransform rectMoveRoot;

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            _RefreshLayout();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _RefreshLayout();
        }

        public void ForceRefresh()
        {
            _RefreshLayout();
        }

        private void _RefreshLayout()
        {
            var fullRect = (transform as RectTransform).rect;
            var fullWidth = fullRect.width;
            var fullHeight = fullRect.height;
            if (fullWidth <= 0 || fullHeight <= 0)
                return;

            // 安全区是像素坐标
            // 与Screen尺寸计算得到上下边界的比例
            var safeArea = Screen.safeArea;
            var safe_area_bottom_size = fullHeight * (safeArea.yMin / Screen.height);
            var safe_area_top_size = fullHeight * (Screen.height - safeArea.yMax) / Screen.height;

            // 计算得到 排除安全区以后 80%位置在全尺寸的anchor系数
            var topBottonSplitLineCoe = ((fullHeight - safe_area_bottom_size - safe_area_top_size) * topBottomAnchorRef + safe_area_bottom_size) / fullHeight;
            bgBottom.anchorMax = new Vector2(1f, topBottonSplitLineCoe);
            bgTop.anchorMin = new Vector2(0f, topBottonSplitLineCoe);

            // bg仅在必要时横向拉伸
            var bgWidth = bgRoot.rect.width;
            bgRoot.localScale = fullWidth > bgWidth ? new Vector3(fullWidth / bgWidth, 1f, 1f) : Vector3.one;

            var topBottomSepPos = fullHeight * topBottonSplitLineCoe;
            var orderAreaMin = topBottomSepPos - rectOrderBottomExt;
            var orderAreaMax = fullHeight - safe_area_top_size - sizeStatusBar;

            // order
            var rectOrderScale = (orderAreaMax - orderAreaMin) / rectOrder.rect.height;
            _ClampScale_Order(ref rectOrderScale);
            rectOrder.localScale = Vector3.one * rectOrderScale;
            rectOrder.anchoredPosition = new Vector2(0f, orderAreaMin + rectOrder.rect.height * rectOrderScale * rectOrder.pivot.y);
            // 调整订单区实际size 避免拖拽边界不准确
            var orderParent = rectOrder.parent as RectTransform;
            var realWidth = orderParent.rect.width * rectOrderScale;
            rectOrder.sizeDelta = new Vector2((orderParent.rect.width - realWidth) / rectOrderScale, rectOrder.rect.height);

            // bottom
            var bottomAreaMin = safe_area_bottom_size;
            var bottomAreaMax = bottomAreaMin + rectBottom.rect.height;
            rectBottom.anchoredPosition = new Vector2(0f, (bottomAreaMin + bottomAreaMax) * 0.5f);

            // board
            var boardAreaMin = bottomAreaMax + boardEdgeSpace;
            var boardAreaMax = topBottomSepPos - rectBoardTopExt - boardEdgeSpace;
            var boardMaxHeight = boardAreaMax - boardAreaMin;
            var boardMaxWidth = fullWidth - boardEdgeSpace * 2;

            var rectBoardScaleX = boardMaxWidth / rectBoard.rect.width;
            var rectBoardScaleY = boardMaxHeight / rectBoard.rect.height;
            var rectBoardScale = Mathf.Min(rectBoardScaleX, rectBoardScaleY);
            _ClampScale_Board(ref rectBoardScale);
            rectBoard.localScale = Vector3.one * rectBoardScale;
            // 空闲区域6/4分 棋盘偏上一点
            var freeArea = Mathf.Max(0, boardMaxHeight - rectBoard.rect.height * rectBoardScale);
            rectBoard.anchoredPosition = new Vector2(0f, (boardAreaMin + boardAreaMax) * 0.5f + freeArea * (boardOffsetPercent - 0.5f));

            // move节点的缩放与board保持一致
            rectMoveRoot.localScale = rectBoard.localScale;

            // 在极端尺寸下bg纵向拉伸
            _AdjustBgHeightScale(bgTop);
            _AdjustBgHeightScale(bgBottom);

            MessageCenter.Get<MSG.BOARD_AREA_ADAPTER_COMPLETE>().Dispatch(rectOrderScale);
        }

        private void _ClampScale_Order(ref float scale)
        {
            if (scale > maxScaleLimitForOrder)
                scale = maxScaleLimitForOrder;
        }

        private void _ClampScale_Board(ref float scale)
        {
            if (scale > maxScaleLimitForBoard)
                scale = maxScaleLimitForBoard;
            Game.Manager.mainMergeMan.mainBoardScale = scale;
        }

        private void _AdjustBgHeightScale(RectTransform root)
        {
            var fill = root.Find("Fill") as RectTransform;
            if (fill.rect.height < root.rect.height)
            {
                fill.localScale = new Vector3(1f, root.rect.height / fill.rect.height);
            }
        }

        public Vector3 JumpCardAlbumIconPos()
        {
            var target = rectBottom.Find("MapSceneEntry").position;
            return target;
        }
    }
}