/*
 * @Author: tang.yan
 * @Description: 场景特效Helper
 * @Date: 2025-04-25 16:04:54
 */

using System;
using UnityEngine;

namespace FAT
{
    public class MapEffectHelper : MonoBehaviour
    {
        private Material _gaussianBlurMat;  //高斯模糊
        private Material _alphaMaskMat;     //透明度Mask
        private int _iterations = 2;    //重复次数
        private int _downgrade = 4;     //降分辨率到多少倍
        
        private bool _canPost = false;  //是否可以进行后处理
        private Action _PostCallBack;   //后处理结果回调

        public void StartPostProcess(Action renderCb)
        {
            _canPost = true;
            _PostCallBack = renderCb;
        }

        public void StopPostProcess()
        {
            _canPost = false;
        }
        
        public void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (!_canPost)
            {
                //不做高斯模糊时也执行一次默认输出 避免报Waring
                Graphics.Blit(src, dst);
                return;
            }
            // 1) 申请两个降采样 RT
            RenderTexture rt1, rt2;
            int width = src.width / _downgrade;
            int height = src.height / _downgrade;
            rt1 = RenderTexture.GetTemporary(width, height);
            rt2 = RenderTexture.GetTemporary(width, height);
            // 2) 首次下采样或 quarter-pass
            if(_downgrade < 4)
            {
                Graphics.Blit(src, rt1);
            }
            else
            {
                Graphics.Blit(src, rt1, _GetGaussianBlurMat(), 0);
            }
            // 3) 横/竖向模糊循环
            for(int i = 0; i < _iterations; i++)
            {
                Graphics.Blit(rt1, rt2, _GetGaussianBlurMat(), 1);
                Graphics.Blit(rt2, rt1, _GetGaussianBlurMat(), 2);
            }
            
            //输出高斯模糊的最终结果
            Graphics.Blit(rt2, dst);
            
            RenderTexture.ReleaseTemporary(rt1);
            RenderTexture.ReleaseTemporary(rt2);
            
            //执行回调 之后清空 保证后续OnRenderImage时不再执行
            _PostCallBack?.Invoke();
            _PostCallBack = null;
        }

        private Material _GetGaussianBlurMat()
        {
            if (_gaussianBlurMat == null)
            {
                _gaussianBlurMat = new Material(CommonRes.Instance.gaussianBlurMat);
            }
            return _gaussianBlurMat;
        }
    }
}