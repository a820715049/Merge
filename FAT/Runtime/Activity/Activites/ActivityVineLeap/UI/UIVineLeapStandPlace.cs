// ===================================================
// Author: mengqc
// Date: 2025/09/11
// ===================================================

using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class UIVineLeapStandPlace : MonoBehaviour
    {
        public RectTransform standPos;
        private List<Vector3> _standPosList = new();

        public Vector3 GetStandPos(RectTransform targetContent, int index = 0)
        {
            if (_standPosList.Count <= 0)
            {
                RandomStandPos();
            }

            var offset = _standPosList[index];
            var gPos = standPos.TransformPoint(offset);
            return targetContent ? targetContent.InverseTransformPoint(gPos) : gPos;
        }

        public void RandomStandPos()
        {
            UIVineLeapUtil.RandomPos(standPos.rect, 15, _standPosList);
        }
    }
}