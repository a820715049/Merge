// ===================================================
// Author: mengqc
// Date: 2025/09/10
// ===================================================

using System;
using System.Collections.Generic;
using System.Linq;
using BezierSolution;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FAT
{
    public static class UIVineLeapUtil
    {
        public static void RandomPos(Rect rect, int num, List<Vector3> posList)
        {
            // 随机生成位置，0默认为底部中点，其余位置随机分布在两侧，y的位置从底部向上随机，最终按y进行从小到大的排序
            posList.Clear();

            if (num <= 0) return;

            // 第一个位置固定在底部中点
            posList.Add(new Vector3(0, rect.yMin));

            if (num <= 1) return;

            var listX = new List<float>();
            var listY = new List<float>();

            // 生成剩余位置的随机坐标
            for (var i = 1; i < num; i++)
            {
                // X坐标：在区域宽度范围内随机
                var x = Random.Range(0, rect.width / 2);
                listX.Add(x);

                // Y坐标：在高度范围内随机
                var y = Random.Range(rect.yMin, rect.yMax);
                listY.Add(y);
            }

            // Y坐标按从小到大排序
            listY.Sort((a, b) => a.CompareTo(b));

            // 交替分布在左右两侧
            bool isLeft = false;
            for (var i = 0; i < listX.Count; i++)
            {
                var finalX = listX[i] * (isLeft ? -1 : 1);
                var finalY = listY[i];
                posList.Add(new Vector3(finalX, finalY));
                isLeft = !isLeft;
            }
        }

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