/*
 * @Author: tang.yan
 * @Description: 弹珠游戏-UI层面小球(弹珠)相关数据类  
 * @Date: 2024-12-10 16:12:14
 */
using Box2DSharp.Common;
using UnityEngine;

namespace FAT
{
    public class PachinkoBallData : PachinkoEntityData
    {
        //缓存球的位置和角度v3 避免每次使用都新创建
        private Vector3 _cacheBallPos = new Vector3();
        private Vector3 _cacheBallAngle = new Vector3();
        //缓存根节点的转换矩阵 用于坐标转换
        private Matrix4x4 _cacheRootMatrix;
        //小球初始位置
        private Vector3 _originBallPos = new Vector3();
        
        protected override void AfterBindRoot()
        {
            base.AfterBindRoot();
            //初始时记录小球位置
            _originBallPos = _root.localPosition;
        }

        protected override void AfterBindBody()
        {
            _root.gameObject.SetActive(true);
        }

        //刷新缓存的父节点矩阵
        public void RefreshCacheMatrix(Matrix4x4 parentMatrix)
        {
            _cacheRootMatrix = parentMatrix;
        }

        //每帧同步UI层小球的位置和角度信息
        public void FixedUpdateBallTransInfo(FP basePixelsPerUnit)
        {
            if (_body == null) return;
            //设置位置
            var box2dPos = _body.GetPosition() * basePixelsPerUnit;
            _cacheBallPos.Set(box2dPos.X.AsFloat, box2dPos.Y.AsFloat, FP.Zero.AsFloat);
            //基于parent root 将_ballBody的(局部)坐标转换成世界坐标并赋值给ball
            //这里缓存并直接使用根节点的转换矩阵，减少运算
            _root.position = _cacheRootMatrix.MultiplyPoint3x4(_cacheBallPos);
            //设置角度
            var box2dAngle = _body.GetAngle() * FP.Rad2Deg;
            _cacheBallAngle.Set(0, 0, box2dAngle.AsFloat);
            _iconTrans.eulerAngles = _cacheBallAngle;
        }

        protected override void OnReset()
        {
            _root.localPosition = _originBallPos;
            _root.gameObject.SetActive(false);
        }
    }
}