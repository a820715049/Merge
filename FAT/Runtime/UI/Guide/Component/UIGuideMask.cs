/*
 * @Author: qun.chao
 * @Date: 2022-03-07 18:44:25
 */

using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    [RequireComponent(typeof(RawImage))]
    public class UIGuideMask : BaseMeshEffect
    {
        // color -> x,y,radius,fade
        private Color x_y_r_f = Color.white;
        private float last_x;
        private float last_y;
        private Transform mTarget;
        private bool mIsSceneCanvas;
        private float offsetCoe = 0f;

        public void Setup()
        {
        }

        public void InitOnPreOpen()
        {
            Hide();
        }

        public void CleanupOnPostClose()
        {
            Hide();
        }

        public void Show(Transform target, float offset = 0f)
        {
            // 在适配黑边后 mask区域不再拥有全屏尺寸
            // 简单修复 => 让mask区域铺满全屏 仍然用以前的遮照方案即可
            UIUtility.SetRectFullScreenSize(transform as RectTransform);
            mTarget = target;
            if (GuideUtility.CheckIsAncestor(UIManager.Instance.CanvasRoot, target))
            {
                mIsSceneCanvas = false;
            }
            else
            {
                mIsSceneCanvas = true;
            }
            offsetCoe = offset;
            _Show();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            mTarget = null;
            mIsSceneCanvas = false;
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

        private void Update()
        {
            if (mTarget == null)
                return;
            // if (!Mathf.Approximately(last_x, mTarget.position.x) || !Mathf.Approximately(last_y, mTarget.position.y))
            // {
            //     _Show();
            // }
            _Show();
        }

        private void _Update(VertexHelper vh, int index)
        {
            UIVertex vert = new UIVertex();
            vh.PopulateUIVertex(ref vert, index);
            vert.color = x_y_r_f;
            vh.SetUIVertex(vert, index);
        }

        private void _Show()
        {
            if (mIsSceneCanvas)
            {
                _ShowInSceneUI();
            }
            else
            {
                _ShowInMainUI();
            }
        }

        private void _ShowInMainUI()
        {
            var tarTrans = mTarget as RectTransform;
            var (w, h) = GuideUtility.CalcRectTransformScreenSize(null, tarTrans);
            var sp = GuideUtility.CalcRectTransformScreenPos(null, tarTrans);
            _ShowCircle(w / Screen.width, h / Screen.width, sp);
        }

        private void _ShowInSceneUI()
        {
            var tarTrans = mTarget as RectTransform;
            var (w, h) = GuideUtility.CalcRectTransformScreenSize(Camera.main, tarTrans);
            var sp = GuideUtility.CalcRectTransformScreenPos(Camera.main, tarTrans);
            _ShowCircle(w / Screen.width, h / Screen.width, sp);
        }

        /// <summary>
        /// 拼接参数用于计算屏幕空间圆形
        /// </summary>
        /// <param name="width">宽度占画面宽的比例</param>
        /// <param name="height">高度占画面宽的比例 因为shader中是用的_ScreenParams.x来衡量圆范围 所以还是以画面宽为依据</param>
        /// <param name="sp"></param>屏幕坐标<summary>
        private void _ShowCircle(float width, float height, Vector2 sp)
        {
            var canvasRoot = UIManager.Instance.CanvasRoot;
            var canvasRect = canvasRoot.rect;

            // pos
            x_y_r_f.r = sp.x / canvasRect.width / canvasRoot.localScale.x;
            x_y_r_f.g = sp.y / canvasRect.height / canvasRoot.localScale.y;

            // 简单起见 把渐变区域的宽度当成基准 允许策划配置系数*基准作为半径偏移量
            // 1080尺度下 30作为渐变区域
            var fadeRange = 30f / canvasRect.width;

            // // 选一个长边作为圆的半径
            // var radius = width > height ? width : height;
            // 对角线作为半径
            var radius = Mathf.Sqrt(width * width + height * height);
            x_y_r_f.b = radius * 0.5f + fadeRange * offsetCoe;

            // 渐变 外展区域
            x_y_r_f.a = 30f / canvasRect.width;

            // refresh
            base.graphic.SetVerticesDirty();
        }
    }
}