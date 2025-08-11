/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏拖拽组件 
 * @Date: 2024-09-26 10:09:25
 */

using System;
using UnityEngine.EventSystems;
using UnityEngine;

namespace MiniGame
{
    public class UIBeadsDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private bool _isInit;
        private bool _isDragging;
        private Vector3 _originCoordPos;    //记录坐标原点的世界坐标 位于拖拽区域左上角
        private float _cellWidth; //坐标系中单位距离的宽度
        private float _cellHeight; //坐标系中单位距离的高度
        private Vector2 _dragOffset = Vector2.zero;        //当前拖拽的位置和实际移动的珠子的位置的差值
        private Vector2 _beginPos = Vector2.zero;

        private Action<int, int, Vector2> _onDragBegin;
        private Action<Vector2> _onDrag;
        private Action<int, int> _onDragEnd;
        
        public void Init(Vector3 originPos, float width, float height)
        {
            _isInit = true;
            _isDragging = false;
            _originCoordPos = originPos;
            _cellWidth = width;
            _cellHeight = height;
        }
        
        public void RegisterListener(Action<int, int, Vector2> onDragBegin, Action<Vector2> onDrag, Action<int, int> onDragEnd)
        {
            _onDragBegin = onDragBegin;
            _onDrag = onDrag;
            _onDragEnd = onDragEnd;
        }

        public void Clear()
        {
            _isInit = false;
            _isDragging = false;
            _onDragBegin = null;
            _onDrag = null;
            _onDragEnd = null;
            _cellWidth = 0;
            _cellHeight = 0;
            _beginPos = Vector2.zero;
        }

        public bool IsDragging()
        {
            return _isDragging;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInit) return;
            if (eventData.pointerId > 0) return;    //屏蔽双指操作
            _isDragging = true;
            //开始拖拽时通过计算是否命中珠子
            var pressPos = eventData.pressPosition;
            _CalCoordIndex(pressPos, out var x, out var y);
            _beginPos = eventData.position;
            _onDragBegin?.Invoke(x, y, _beginPos);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isInit) return;
            if (eventData.pointerId > 0) return;
            var delta = eventData.position - _beginPos;
            _onDrag?.Invoke(delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isInit) return;
            if (eventData.pointerId > 0) return;
            _isDragging = false;
            var realDragPos = eventData.position - _dragOffset;
            _CalCoordIndex(realDragPos, out var x, out var y);
            _onDragEnd?.Invoke(x, y);
        }

        public void SetDragOffset(Vector2 offset)
        {
            _dragOffset = offset;
        }

        private void _CalCoordIndex(Vector2 pos, out int x, out int y)
        {
            x = Mathf.FloorToInt((pos.x - _originCoordPos.x) / _cellWidth);
            y = -Mathf.CeilToInt((pos.y - _originCoordPos.y) / _cellHeight);
        }
    }
}
