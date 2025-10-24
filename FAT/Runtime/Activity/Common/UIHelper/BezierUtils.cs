// ===================================================
// Author: mengqc
// Date: 2025/09/29
// ===================================================

using System.Collections.Generic;
using System.Linq;
using BezierSolution;
using UnityEngine;

namespace FAT
{
    public static class BezierUtils
    {
        public static void Reshape(BezierSpline spline, Vector3 startPt, Vector3 endPt)
        {
            // 获取曲线的点位
            var points = new List<BezierPoint>();
            var bezierPoints = spline.GetComponentsInChildren<BezierPoint>();
            points.AddRange(bezierPoints);

            // 将点位转换为比率位置
            var posList = points.Select(point => point.position).ToList();
            var startPos = posList[0];
            var endPos = posList[posList.Count - 1];
            var xLen = endPos.x - startPos.x;
            var yLen = endPos.y - startPos.y;

            var ratioList = new List<Vector2>();
            var posLen = posList.Count;

            for (int i = 0; i < posList.Count; i++)
            {
                var pos = posList[i];
                if (i == 0)
                {
                    ratioList.Add(new Vector2(0, 0));
                }
                else if (i == posLen - 1)
                {
                    ratioList.Add(new Vector2(1, 1));
                }
                else
                {
                    float xRatio = 0;
                    if (xLen != 0)
                    {
                        xRatio = (pos.x - startPos.x) / xLen;
                    }

                    float yRatio = 0;
                    if (yLen != 0)
                    {
                        yRatio = (pos.y - startPos.y) / yLen;
                    }

                    ratioList.Add(new Vector2(xRatio, yRatio));
                }
            }

            // 按新的起点与终点重新计算各点位置
            points[0].position = startPt;
            points[^1].position = endPt;

            for (var i = 1; i < points.Count - 1; i++)
            {
                var ratio = ratioList[i];
                points[i].position = new Vector3(
                    Mathf.LerpUnclamped(startPt.x, endPt.x, ratio.x),
                    Mathf.LerpUnclamped(startPt.y, endPt.y, ratio.y),
                    0
                );
            }

            spline.AutoConstructSpline();
        }
    }
}