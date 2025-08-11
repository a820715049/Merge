/*
 * @Author: qun.chao
 * @Date: 2022-11-15 20:40:23
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UITrail : MaskableGraphic
    {
        struct Segment
        {
            // 位置
            public Vector2 pos;
            // 连接两个seg的方向
            public Vector2 dir;
            // 宽度方向的向量
            public Vector2 normal;

            // 添加时间
            public float addTime;
            // 距离跟随seg的长度
            public float segLenOrig;
            // 距离跟随seg的实际长度 | 受时间影响会变化
            public float segLenReal;
            // 与更早一个seg相比 normal属性之间的夹角
            public float normalAngleDiffOrig;
        }

        #region param
        public Transform refTrans;
        public Gradient gradient = new Gradient();
        public AnimationCurve widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        public float lifetime = 1f;
        public float minStep = 40f;
        public float halfWidth = 20f;
        #endregion

        private const int maxSegNum = 200;
        private Segment headSeg = new Segment();
        private UIVertex v1 = new UIVertex { color = Color.white };
        private UIVertex v2 = new UIVertex { color = Color.white };
        private List<Segment> segList = new List<Segment>();

        private Vector3 lastSegPos;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _EnsureRefTrans();
        }
#endif

        public override Texture mainTexture
        {
            get
            {
                if (material != null)
                {
                    return material.mainTexture;
                }
                return base.mainTexture;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _EnsureRefTrans();
            lastSegPos = refTrans.localPosition;
            segList.Clear();
            _MarkDirty();
        }

        private void Update()
        {
            if (segList.Count > 0 || _IsMoving())
            {
                _MarkDirty();
            }
        }

        private void _MarkDirty()
        {
            base.SetVerticesDirty();
        }

        private bool _IsMoving()
        {
            return !Vector3.Equals(refTrans.localPosition, lastSegPos);
        }

        private void _EnsureRefTrans()
        {
            if (refTrans == null)
                refTrans = transform;
        }

        private bool _IsActivated()
        {
            if (segList.Count > 0)
                return true;
            if (_IsMoving())
            {
                // 位置发生变化 启动绘制
                _AddSegment(lastSegPos);
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            vh.Clear();

            if (!_IsActivated())
                return;

            _SyncSegment();

            _UpdateLeadingSegment();

            var totalLength = _UpdateTrailLength();

            if (segList.Count < 1 || Mathf.Approximately(totalLength, 0f))
                return;

            _BuildTrail(vh, totalLength);
        }

        private void _AddVertex(VertexHelper vh, Segment seg, Vector2 offset, float uv_x)
        {
            var col = gradient.Evaluate(uv_x) * base.color;
            v1.color = col;
            v2.color = col;

            var r = halfWidth * widthCurve.Evaluate(uv_x);
            v1.position = seg.pos - seg.normal * r - offset;
            v2.position = seg.pos + seg.normal * r - offset;

            v1.uv0 = new Vector2(uv_x, 1f);
            v2.uv0 = new Vector2(uv_x, 0f);

            vh.AddVert(v1);
            vh.AddVert(v2);
        }

        private void _BuildTrail(VertexHelper vh, float totalLength)
        {
            Vector2 localPos = refTrans.localPosition;
            float curLength = 0f;
            for (int i = 0; i < segList.Count; ++i)
            {
                _AddVertex(vh, segList[i], localPos, 1f - curLength / totalLength);
                curLength += segList[i].segLenReal;
            }

            _AddVertex(vh, headSeg, localPos, 1f - curLength / totalLength);

            var vertCount = vh.currentVertCount;
            for (int i = 0; i + 3 < vertCount; i += 2)
            {
                vh.AddTriangle(i + 1, i, i + 3);
                vh.AddTriangle(i, i + 2, i + 3);
            }
        }

        private void _AddSegment(Vector3 pos)
        {
            lastSegPos = pos;
            if (segList.Count > 0)
            {
                _UpdateLatestSeg(pos);
            }
            var cur = new Segment
            {
                pos = pos,
                addTime = Time.time
            };
            segList.Add(cur);
        }

        private void _SyncSegment()
        {
            var segLen = minStep;
            var diff = refTrans.localPosition - lastSegPos;
            diff.z = 0f;
            var dist = diff.magnitude;
            if (dist > segLen)
            {
                var num = (int)(dist / segLen);
                _AddSegment(lastSegPos + diff * segLen * num / dist);
            }

            // 太长
            if (segList.Count > maxSegNum)
            {
                segList.RemoveRange(0, segList.Count - maxSegNum);
            }
        }

        private void _UpdateLeadingSegment()
        {
            var curPos = refTrans.localPosition;
            _UpdateLatestSeg(curPos);

            var prev = segList[segList.Count - 1];
            headSeg.pos = curPos;
            headSeg.dir = prev.dir;
            headSeg.normal = _CalcRightNormal(headSeg.dir);
        }

        /// <summary>
        /// 计算尾段 / 记录总长度
        /// </summary>
        private float _UpdateTrailLength()
        {
            // 下一段丢弃时间
            float followingDropTime = 0f;
            float totalLength = 0f;
            for (int i = segList.Count - 1; i >= 0; --i)
            {
                var seg = segList[i];
                var curDropTime = seg.addTime + lifetime;
                if (Time.time > curDropTime)
                {
                    if (followingDropTime > 0f)
                    {
                        // 时间作为渐变依据
                        var t = (followingDropTime - Time.time) / (followingDropTime - curDropTime);

                        // 更新长度
                        seg.segLenReal = seg.segLenOrig * t;

                        var followingSeg = segList[i + 1];
                        // 更新尾巴位置
                        seg.pos = followingSeg.pos - seg.dir * seg.segLenReal;

                        // 更新normal方向
                        var angle = followingSeg.normalAngleDiffOrig * t;
                        seg.normal = Quaternion.Euler(0f, 0f, angle) * followingSeg.normal;

                        // 回写
                        segList[i] = seg;

                        // 累积长度
                        totalLength += seg.segLenReal;

                        // 尾巴后面全舍弃
                        segList.RemoveRange(0, i);
                        break;
                    }
                    else
                    {
                        // 全部超时
                        segList.Clear();
                        lastSegPos = refTrans.localPosition;
                        break;
                    }
                }
                else
                {
                    followingDropTime = curDropTime;
                    totalLength += seg.segLenReal;
                }
            }
            return totalLength;
        }

        /// <summary>
        /// 根据最新坐标决定最后一段seg的展开方向
        /// </summary>
        private void _UpdateLatestSeg(Vector2 nextPos)
        {
            var count = segList.Count;
            var last = segList[count - 1];
            var diff = nextPos - last.pos;
            last.segLenOrig = diff.magnitude;
            last.segLenReal = last.segLenOrig;

            if (Mathf.Approximately(last.segLenOrig, 0f))
            {
                // 长度近似0 无法计算dir
                if (count > 1)
                    last.dir = segList[count - 2].dir;
                else
                    last.dir = Vector2.zero;
            }
            else
                last.dir = diff / last.segLenReal;

            if (count > 1)
            {
                var before_last = segList[count - 2];
                var mid_dir = last.dir - before_last.dir;
                if (mid_dir == Vector2.zero)
                {
                    last.normal = _CalcRightNormal(last.dir);
                }
                else
                {
                    last.normal = mid_dir.normalized;
                    if (_IsMovingLeft(last.dir, last.normal))
                    {
                        last.normal = -last.normal;
                    }
                }
                // 当前normal 与 前一个normal 的夹角
                last.normalAngleDiffOrig = Vector2.SignedAngle(last.normal, before_last.normal);
            }
            else
            {
                last.normal = _CalcRightNormal(last.dir);
            }
            segList[segList.Count - 1] = last;
        }

        private float _CrossTest(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private bool _IsMovingLeft(Vector2 dir, Vector2 vec)
        {
            return _CrossTest(dir, vec) > 0f;
        }

        private Vector2 _CalcRightNormal(Vector2 dir)
        {
            return new Vector2(dir.y, -dir.x);
        }
    }
}