/*
 * @Author: qun.chao
 * @Date: 2023-11-24 11:18:31
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FAT.Merge;
using EL;

namespace FAT
{
    /*
        棋子 or transform 组成的多个目标共同决定mask区域
    */
    [RequireComponent(typeof(RawImage))]
    public class UIGuideMaskForBoardItem : BaseMeshEffect
    {
        private HashSet<int> targetItemIdTable = new();
        private List<Vector2> coordCacheList = new();
        private List<Transform> transformList = new();
        // color -> x,y,radius,fade
        private Color x_y_r_f = Color.white;
        private float radiusOffsetCoe = 0f;

        public void Setup()
        {
        }

        public void InitOnPreOpen()
        {
            Hide();
            MessageCenter.Get<MSG.UI_BOARD_DIRTY>().AddListener(_OnMessageBoardDirty);
        }

        public void CleanupOnPostClose()
        {
            Hide();
            MessageCenter.Get<MSG.UI_BOARD_DIRTY>().RemoveListener(_OnMessageBoardDirty);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            targetItemIdTable.Clear();
            coordCacheList.Clear();
            transformList.Clear();
        }

        // 允许指定transform和itemId对mask区域进行限定
        public void ShowMask(List<int> items, List<Transform> transforms, float offsetCoe)
        {
            // 在适配黑边后 mask区域不再拥有全屏尺寸
            // 简单修复 => 让mask区域铺满全屏 仍然用以前的遮照方案即可
            UIUtility.SetRectFullScreenSize(transform as RectTransform);

            // item
            targetItemIdTable.Clear();
            if (items != null)
            {
                foreach (var item in items)
                {
                    targetItemIdTable.Add(item);
                }
            }

            // transform
            transformList.Clear();
            if (transforms != null)
            {
                transformList.AddRange(transforms);
            }

            // offset
            radiusOffsetCoe = offsetCoe;

            _RefreshTargetItemCoordCache();
            _Show();
            gameObject.SetActive(true);
        }

        private void _RefreshTargetItemCoordCache()
        {
            coordCacheList.Clear();
            foreach (var trans in transformList)
            {
                var sp = GuideUtility.CalcRectTransformScreenPos(null, trans);
                var coord = BoardUtility.GetRealCoordByScreenPos(sp);
                coordCacheList.Add(coord);
            }

            // 没有要关心的item
            if (targetItemIdTable.Count < 1)
                return;

            var board = BoardViewManager.Instance.board;
            var width = board.size.x;
            var height = board.size.y;

            Item item;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; j++)
                {
                    item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        if (targetItemIdTable.Contains(item.tid))
                        {
                            // 蜘蛛网内物品和常规物品都可以计入  不计入泡泡棋子 但计入冰冻棋子
                            if (!item.isLocked && item.isFrozen || item.isActive && !ItemUtility.IsBubbleItem(item))
                            {
                                coordCacheList.Add(item.coord);
                            }
                        }
                    }
                }
            }
        }

        private void _OnMessageBoardDirty()
        {
            _RefreshTargetItemCoordCache();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
            {
                return;
            }

            _Update(vh, 0);
            _Update(vh, 1);
            _Update(vh, 2);
            _Update(vh, 3);
        }

        private void _Update(VertexHelper vh, int index)
        {
            UIVertex vert = new UIVertex();
            vh.PopulateUIVertex(ref vert, index);
            vert.color = x_y_r_f;
            vh.SetUIVertex(vert, index);
        }

        private void Update()
        {
            if (coordCacheList.Count < 1)
                return;
            _Show();
        }

        private void _Show()
        {
            Vector2 center = Vector2.zero;
            foreach (var coord in coordCacheList)
            {
                center += coord;
            }
            center /= coordCacheList.Count;
            var distSquare = 0f;
            foreach (var coord in coordCacheList)
            {
                var sqrMag = (coord - center).sqrMagnitude;
                if (distSquare < sqrMag)
                {
                    distSquare = sqrMag;
                }
            }

            var sp = BoardUtility.GetScreenPosByCoord(center.x, center.y);
            // 加半格 尽量让icon完全包含在内
            var screen_radius = (Mathf.Sqrt(distSquare) + 0.5f) * BoardUtility.screenCellSize / Screen.width;

            var canvasRoot = UIManager.Instance.CanvasRoot;
            var canvasRect = canvasRoot.rect;

            // 简单起见 把渐变区域的宽度当成基准 允许策划配置系数*基准作为半径偏移量
            // 1080尺度下 30作为渐变区域
            var fadeRange = 30f / canvasRect.width;

            // calc param
            x_y_r_f.r = sp.x / canvasRect.width / canvasRoot.localScale.x;
            x_y_r_f.g = sp.y / canvasRect.height / canvasRoot.localScale.y;
            x_y_r_f.b = screen_radius + fadeRange * radiusOffsetCoe;
            x_y_r_f.a = fadeRange;

            // dirty
            base.graphic.SetVerticesDirty();
        }
    }
}