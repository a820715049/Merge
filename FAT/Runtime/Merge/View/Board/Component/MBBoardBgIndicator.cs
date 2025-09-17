/*
 * @Author: qun.chao
 * @Date: 2024-12-19 12:31:42
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace FAT
{
    public class MBBoardBgIndicator : BaseMeshEffect
    {
        private readonly List<UIVertex> _vertices = new();
        private readonly List<int> _indices = new();
        private readonly Color colFinished = new Color32(0xd2, 0xfb, 0x85, 255);
        private readonly Color colOnGoing = new Color32(0x9b, 0xbb, 0x55, 255);
        private int width;
        private int height;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
                return;
            vh.Clear();
            if (BoardViewManager.Instance.world == null)
                return;
            _Clear();
            BuildBoard();
            vh.AddUIVertexStream(_vertices, _indices);
            _Clear();
        }

        public void Setup(int w, int h)
        {
            width = w;
            height = h;
            _SetDirty();
        }

        public void Cleanup()
        { }

        public void Refresh()
        {
            _SetDirty();
        }

        private void _SetDirty()
        {
            graphic.SetVerticesDirty();
        }

        private void _Clear()
        {
            _vertices.Clear();
            _indices.Clear();
        }

        /// <summary>
        /// 与棋子索引顺序保持一致 从左到右 从上到下
        /// </summary>
        private void BuildBoard()
        {
            var cache = BoardViewWrapper.GetBoardOrderRequireItemStateCache();
            // if (cache == null)
            //     return;
            for (var row = 0; row < height; ++row)
            {
                for (var col = 0; col < width; ++col)
                {
                    AddQuad(col, row, cache);
                }
            }
        }

        private void AddQuad(int col, int row, Dictionary<int, int> orderStateDict)
        {
            var cell_size = BoardUtility.cellSize;
            var item = BoardViewManager.Instance.board.GetItemByCoord(col, row);
            if (item == null)
                return;
            var valid = false;
            var hasFlag = false;
            var v = BoardViewManager.Instance.GetItemView(item.id);
            if (v != null)
            {
                v.SetOrderTipDirty();
                // item处于idle时才显示bg
                valid = !v.IsDragging();
                hasFlag = v.hasFlag;
            }
            var state = 0;
            
            if (valid &&
                item.isActive &&
                !item.HasComponent(Merge.ItemComponentType.Bubble) &&
                (hasFlag || (orderStateDict?.TryGetValue(item.tid, out  state) ?? false)))
            {
                // 需要显示格子
                var start_x = cell_size * col;
                var start_y = -cell_size * row;

                var v0 = new UIVertex() { position = new Vector3(start_x, start_y) };
                var v1 = new UIVertex() { position = new Vector3(start_x + cell_size, start_y) };
                var v2 = new UIVertex() { position = new Vector3(start_x + cell_size, start_y - cell_size) };
                var v3 = new UIVertex() { position = new Vector3(start_x, start_y - cell_size) };

                if (state == 1 || hasFlag)
                {
                    v0.color = v1.color = v2.color = v3.color = colFinished;
                }
                else
                {
                    v0.color = v1.color = v2.color = v3.color = colOnGoing;
                }

                var idx = _vertices.Count;
                _vertices.Add(v0);
                _vertices.Add(v1);
                _vertices.Add(v2);
                _vertices.Add(v3);
                _indices.Add(idx);
                _indices.Add(idx + 1);
                _indices.Add(idx + 2);
                _indices.Add(idx + 2);
                _indices.Add(idx + 3);
                _indices.Add(idx);
            }
        }
    }
}