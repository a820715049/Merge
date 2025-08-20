using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using DG.Tweening;
using EL;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class MBSpinLayout : MonoBehaviour
    {
        public float animLength;
        public int minLaps;
        public GameObject cell;
        public Transform poolNode;
        private PackSpin _pack;
        private (int, int) _size;
        private int _curIndex;//当前动画展示的cell
        private List<MBSpineReward> _cellList = new();

        public void RefreshLayout(PackSpin pack)
        {
            _pack = pack;
            _CheckCellNum();
            _RefreshCellSize();
            _RefreshCellPos();
            _RefreshCellData();
        }

        public void PlayRewardAnim(Action callback)
        {
            _curIndex = 0;
            _lastTrigger = -1;
            DOTween.To(() => _curIndex, x => _triggerAnim(x), minLaps * _cellList.Count - 1 + _pack.GetAnimIndex(), animLength).SetEase(Ease.OutQuad).onComplete += () =>
            {
                _cellList[_pack.GetAnimIndex()].PlayWinTrigger();
                foreach (var cell in _cellList)
                {
                    if (_cellList.IndexOf(cell) != _pack.GetAnimIndex()) { cell.PlayPunchTrigger(); }
                }
                callback.Invoke();
            };
        }

        private int _lastTrigger;
        private void _triggerAnim(int index)
        {
            var needTrigger = index % _cellList.Count;
            if (needTrigger == _lastTrigger) { return; }
            _cellList[needTrigger].PlayStartTrigger();
            _lastTrigger = needTrigger;
        }

        public void PlayEndAnim()
        {
            _cellList[_pack.GetAnimIndex()].PlayEndTrigger();
        }

        /// <summary>
        /// 检查Cell数量是够和活动奖励数量匹配
        /// </summary>
        private void _CheckCellNum()
        {
            var diff = _pack.ShowIndexList.Count - _cellList.Count;
            if (diff == 0) { return; }
            if (diff > 0) { _AddCell(diff); }
            else { _RemoveCell(diff); }
        }

        private void _AddCell(int diff)
        {
            for (var i = 0; i < diff; i++) { _cellList.Add(Instantiate(cell, transform).GetComponent<MBSpineReward>()); }
        }

        private void _RemoveCell(int diff)
        {
            for (var i = 0; i < diff; i++)
            {
                if (_cellList.Count == 0) { break; }
                _cellList[0].gameObject.transform.SetParent(poolNode);
                _cellList.RemoveAt(0);
            }
        }

        /// <summary>
        /// 刷新Cell大小
        /// </summary>
        private void _RefreshCellSize()
        {
            _size.Item1 = _cellList.Count == 10 ? 332 : 252;
            _size.Item2 = 214;
            foreach (var cell in _cellList) { (cell.transform as RectTransform).sizeDelta = new Vector2(_size.Item1, _size.Item2); }
        }

        /// <summary>
        /// 分局数量确定每一个cell位置
        /// </summary>
        private void _RefreshCellPos()
        {
            for (var i = 0; i < _cellList.Count; i++) { _cellList[i].transform.localPosition = new Vector3(_GetCellPosX(i), _GetCellPosY(i), 0); }
        }

        private float _GetCellPosX(int index)
        {
            var offset = _cellList.Count == 10 ? 34f : 24f;
            var maxCount = _cellList.Count == 10 ? 3 : 4;//每行最多有几个
            if (index < maxCount) { return offset + _size.Item1 * (index + 0.5f) + index * 8f; }
            else if (index < maxCount + 3) { return offset + _size.Item1 * (maxCount - 0.5f) + 16f; }
            else if (index < maxCount * 2 + 2) { return offset + _size.Item1 * (maxCount * 2 + 1 - index + 0.5f) + (maxCount * 2 + 1 - index) * 8f; }
            else { return offset + _size.Item1 * 0.5f; }
        }
        private float _GetCellPosY(int index)
        {
            var maxCount = _cellList.Count == 10 ? 3 : 4;//每行最多有几个
            if (index < maxCount) { return -_size.Item2 * 0.5f; }
            else if (index < maxCount + 3) { return -_size.Item2 * (index - maxCount + 1.5f) - (index - maxCount + 1) * 8f; }
            else if (index < maxCount * 2 + 2) { return -_size.Item2 * 3.5f - 24f; }
            else { return -(_cellList.Count - index + 0.5f) * _size.Item2 - (_cellList.Count - index) * 8f; }
        }

        private void _RefreshCellData()
        {
            for (var i = 0; i < _pack.ShowIndexList.Count; i++)
            {
                var id = _pack.ShowIndexList[i];
                _cellList[i].RefreshCellData(id, _pack.CheckHasBought(id), _pack.RewardIDList.Count == 10 ? 128 : 104);
                _cellList[i].SetGroup(_pack.RewardIDList.Count == 10 ? 1 : 0);
            }
        }
    }
}