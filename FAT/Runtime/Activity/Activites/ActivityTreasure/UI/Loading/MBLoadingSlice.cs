/*
 * @Author: qun.chao
 * @Date: 2022-10-19 10:19:52
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBLoadingSlice : BaseMeshEffect
    {
        [SerializeField] private int lineNum;
        [SerializeField] private float rot;
        [SerializeField] private Texture2D bg;
        [SerializeField] private float _loadingProgress;
        public float loadingProgress
        {
            get { return _loadingProgress; }
            set
            {
                _loadingProgress = value;
                graphic.SetVerticesDirty();
            }
        }

        #region bg
        private (float min, float max) bgCoordRangeW;
        private (float min, float max) bgCoordRangeH;
        #endregion

        private (float width, float length) boundingRectSize;
        private float lineHeight;
        private float fitCoe;
        private bool isInverted;

        public void SetInvert(bool b)
        {
            isInverted = b;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _RefreshParams();

            graphic.SetVerticesDirty();
        }
#endif

        public void Refresh()
        {
            _RefreshParams();
        }

        private void _RefreshParams()
        {
            var rect = (transform.parent as RectTransform).rect;
            // Debug.LogFormat("width {0}, height {1}", rect.width, rect.height);

            var ran = Mathf.Deg2Rad * rot;
            var sin = Mathf.Sin(ran);
            var cos = Mathf.Cos(ran);
            boundingRectSize = (rect.width * cos + rect.height * sin, rect.width * sin + rect.height * cos);
            // 上下边界各显示半个
            lineHeight = boundingRectSize.length / (lineNum + 1);

            // 计算bg参数
            bgCoordRangeW = (-rect.width / 2, rect.width / 2);
            bgCoordRangeH = (-rect.height / 2, rect.height / 2);

            // fullfit适配
            var aspectTex = bg.width * 1.0f / bg.height;
            var aspectArea = rect.width / rect.height;
            fitCoe = aspectTex / aspectArea;

            transform.localEulerAngles = new Vector3(0f, 0f, rot);

            graphic.SetVerticesDirty();
        }

        private void _DrawLine(VertexHelper vh, int lineIdx, float progress)
        {
            var line_h = lineHeight;
            var head_w = line_h * 0.5f;
            var body_w = boundingRectSize.width;
            var total_w = head_w + body_w;

            // line从左侧屏幕外的左下角作为顶点0
            var orig_x = -boundingRectSize.width * 0.5f - total_w;
            var orig_y = (lineIdx - (lineNum + 2) * 0.5f) * line_h;

            // 叠加当前offset
            orig_x += total_w * progress;

            // 左右交替
            int dir = 1;
            if (0 != lineIdx % 2)
            {
                dir = -1;
            }
            if (isInverted)
                dir *= -1;

            // 3 ------ 2
            // |        |
            // 0 ------ 1
            // body
            int count = vh.currentVertCount;
            _AddVert(vh, new Vector2(orig_x * dir, orig_y), Vector2.one * 0.5f);
            _AddVert(vh, new Vector2((orig_x + body_w) * dir, orig_y), Vector2.one * 0.5f);
            _AddVert(vh, new Vector2((orig_x + body_w) * dir, orig_y + line_h), Vector2.one * 0.5f);
            _AddVert(vh, new Vector2(orig_x * dir, orig_y + line_h), Vector2.one * 0.5f);
            _AddRectTriangle(vh, count, dir);
            // head
            count = vh.currentVertCount;
            _AddVert(vh, new Vector2((orig_x + body_w) * dir, orig_y), Vector2.right * 0.5f);
            _AddVert(vh, new Vector2((orig_x + body_w + head_w) * dir, orig_y), Vector2.zero);
            _AddVert(vh, new Vector2((orig_x + body_w + head_w) * dir, orig_y + line_h), Vector2.up);
            _AddVert(vh, new Vector2((orig_x + body_w) * dir, orig_y + line_h), new Vector2(0.5f, 1f));
            _AddRectTriangle(vh, count, dir);
        }

        private void _AddVert(VertexHelper vh, Vector2 pos, Vector2 uv)
        {
            // 裁减区域uv记录到color | 取值范围0~1
            // bg的uv记录到uv0 | 取值范围需要超过1
            var bgCoordPos = Quaternion.Euler(0f, 0f, rot) * pos;
            var t_x = (bgCoordPos.x - bgCoordRangeW.min) / (bgCoordRangeW.max - bgCoordRangeW.min);
            var t_y = (bgCoordPos.y - bgCoordRangeH.min) / (bgCoordRangeH.max - bgCoordRangeH.min);
            // if (fitCoe > 1f)
            // {
            //     vh.AddVert(pos, new Color(uv.x, uv.y, 0f), new Vector2((t_x - 0.5f) / fitCoe + 0.5f, t_y));
            // }
            // else
            // {
            //     vh.AddVert(pos, new Color(uv.x, uv.y, 0f), new Vector2(t_x, (t_y - 0.5f) * fitCoe + 0.5f));
            // }
            vh.AddVert(pos, new Color(uv.x, uv.y, 0f), new Vector2((t_x - 0.5f) + 0.5f, t_y));
        }

        private void _AddRectTriangle(VertexHelper vh, int vertIdx, int dir)
        {
            if (dir < 0)
            {
                vh.AddTriangle(vertIdx, vertIdx + 1, vertIdx + 2);
                vh.AddTriangle(vertIdx, vertIdx + 2, vertIdx + 3);
            }
            else
            {
                vh.AddTriangle(vertIdx, vertIdx + 2, vertIdx + 1);
                vh.AddTriangle(vertIdx, vertIdx + 3, vertIdx + 2);
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
            {
                return;
            }

            vh.Clear();
            for (int i = 0; i < lineNum + 2; ++i)
            {
                _DrawLine(vh, i, loadingProgress);
            }
        }
    }
}