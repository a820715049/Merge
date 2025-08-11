/*
 * @Author: qun.chao
 * @Date: 2025-04-25 18:07:34
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.SlideMerge
{
    public class MBSlideMergeStage : MonoBehaviour
    {
        [SerializeField] private RectTransform ballRoot;
        [SerializeField] private RectTransform checkLine;
        [SerializeField] private float viewHeight;
        [SerializeField] private float topWidth;
        [SerializeField] private float bottomWidth;
        [SerializeField] private float realHeight;
        public Transform BallRoot => ballRoot;
        public PoolTable Table => _table;
        public float attractSpeed { get; set; } = 100f;
        public float mergeDiscount { get; set; } = 0.5f;
        public float RealCheckLineY => _realCheckLineY;

        private float _realCheckLineY { get; set; }
        private PoolTable _table = new();
        private Dictionary<Ball, MBSlideMergeItem> _ballViewDict = new();
        private List<(Ball, Ball)> _attractList = new();
        private List<Ball> _dyingList = new();
        private List<Transform> _cache = new();
        private UISlideMergeMain _main;
        private Vector2[] _corners = new Vector2[4];

        public void Setup(UISlideMergeMain main_)
        {
            _main = main_;
        }

        public void InitOnPreOpen()
        {
            _corners[0] = new Vector2(0, 0);
            _corners[1] = new Vector2(bottomWidth, 0);
            _corners[2] = new Vector2((bottomWidth + topWidth) * 0.5f, realHeight);
            _corners[3] = new Vector2((bottomWidth - topWidth) * 0.5f, realHeight);
            _table.SetupTrapezoid(_corners[0], _corners[1], _corners[2], _corners[3]);

            var lineCoe = 1f - _main.checkLine / 100f;
            _realCheckLineY = realHeight * lineCoe;
            var lineViewY = viewHeight * lineCoe;
            checkLine.anchoredPosition = new Vector2(0, lineViewY);
            var checkLineWidth = Mathf.Lerp(bottomWidth, topWidth, lineCoe);
            checkLine.sizeDelta = new Vector2(checkLineWidth - 20f, checkLine.sizeDelta.y);

            InitLevel();
        }

        public void CleanupOnPostClose()
        {
            _attractList.Clear();
            foreach (var ball in _dyingList)
            {
                _main.ReleaseBall(_ballViewDict[ball].gameObject, ball);
            }
            _dyingList.Clear();
            foreach (var ball in _table.Balls)
            {
                _main.ReleaseBall(_ballViewDict[ball].gameObject, ball);
            }
            _table.Clear();
            _ballViewDict.Clear();
        }

        public void SpawnBall(GameObject go, Ball data)
        {
            go.transform.SetParent(ballRoot, true);
            data.Pos = go.transform.localPosition;
            _table.AddBall(data);
            _ballViewDict[data] = go.GetComponent<MBSlideMergeItem>();
        }

        public void Update()
        {
            CheckAttract();
            CheckMerge();
            CheckDie();
            _table.Simulate(Time.deltaTime);
        }

        public void LateUpdate()
        {
            SyncBallView();
            SortByDistance();
        }

        private void InitLevel()
        {
            var initItemList = _main.initItemList;
            var initItemPosList = _main.initItemPosList;
            for (var i = 0; i < initItemList.Count; i++)
            {
                var itemId = initItemList[i];
                var (x, y, _) = initItemPosList[i].ConvertToInt3();
                var (view, data) = _main.SpawnInitBall(itemId);
                data.Pos = new Vector2(x, y);
                data.Vel = Vector2.zero;
                _table.AddBall(data);
                _ballViewDict[data] = view.GetComponent<MBSlideMergeItem>();
            }
        }

        private void CheckAttract()
        {
            var balls = _table.Balls;
            var count = balls.Count;
            var hasAttract = false;
            for (var i = count - 1; i >= 0 && !hasAttract; i--)
            {
                var a = balls[i];
                if (a.IsAttracting || a.Id >= _main.maxLevelItemId)
                    continue;
                for (var j = i - 1; j >= 0 && !hasAttract; j--)
                {
                    var b = balls[j];
                    if (b.IsAttracting || a.Id != b.Id)
                        continue;
                    var diff = b.Pos - a.Pos;
                    var sqrDistance = diff.sqrMagnitude;
                    var minDist = a.Radius + b.Radius + _main.mergeDist;
                    if (sqrDistance < minDist * minDist)
                    {
                        // 可以吸引
                        hasAttract = true;

                        a.IsAttracting = true;
                        b.IsAttracting = true;
                        _attractList.Add((a, b));
                        break;
                    }
                }
            }
        }

        private void CheckMerge()
        {
            var mergeDist = 1f;
            for (var i = _attractList.Count - 1; i >= 0; i--)
            {
                var (a, b) = _attractList[i];
                var diff = b.Pos - a.Pos;
                var sqrDistance = diff.sqrMagnitude;
                var minDist = a.Radius + b.Radius + mergeDist;
                if (sqrDistance < minDist * minDist)
                {
                    // 播放合成音效
                    var item = MiniGameConfig.Instance.GetMiniGameSlideMergeItem(a.Id);
                    var soundAsset = item.Asset.ConvertToAssetConfig();
                    FAT.Game.Manager.audioMan.PlaySound(soundAsset.Group, soundAsset.Asset);

                    // 合成新球
                    var (view, data) = _main.SpawnMergedBall(a.Id);
                    data.Pos = (a.Pos + b.Pos) / 2;
                    data.Vel = (b.Vel + a.Vel) * mergeDiscount / 2;
                    ReleaseMergedBall(a);
                    ReleaseMergedBall(b);
                    view.transform.localPosition = data.Pos;
                    _ballViewDict[data] = view.GetComponent<MBSlideMergeItem>();
                    _table.AddBall(data);
                    _attractList.RemoveAt(i);
                }
                else
                {
                    // 更新速度
                    var v1 = diff.normalized * attractSpeed * Time.deltaTime;
                    var v2 = -v1;
                    // 使反向的球速度较小
                    if (v1.y > 0f)
                    {
                        v2 *= 0.8f;
                    }
                    else if (v1.y < 0f)
                    {
                        v1 *= 0.8f;
                    }
                    a.Vel += v1;
                    b.Vel += v2;
                }
            }
        }

        private void CheckDie()
        {
            var dt = Time.deltaTime;
            var count = _dyingList.Count;
            for (var i = count - 1; i >= 0; i--)
            {
                var ball = _dyingList[i];
                if (ball.RadiusScale > 0f)
                {
                    ball.RadiusScale -= dt * 8f;
                    if (ball.RadiusScale < float.Epsilon)
                    {
                        var view = _ballViewDict[ball];
                        _ballViewDict.Remove(ball);
                        _main.ReleaseBall(view.gameObject, ball);
                        _dyingList.RemoveAt(i);
                    }
                }
            }
        }

        private void ReleaseMergedBall(Ball ball)
        {
            // 立即不参与物理
            var data = _table.RemoveBall(ball);
            _dyingList.Add(data);
            data.IsDead = true;
        }

        private void SyncBallView()
        {
            foreach (var (ball, view) in _ballViewDict)
            {
                var pos = ball.Pos;
                pos.y = SimulatePerspectiveZ(pos.y);
                view.transform.localPosition = pos;

                if (ball.IsNewBorn || ball.IsDead)
                {
                    if (ball.RadiusScale >= 1f)
                    {
                        if (ball.IsNewBorn)
                        {
                            ball.IsNewBorn = false;
                            view.Bg.gameObject.SetActive(false);
                        }
                        view.Icon.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        view.Icon.transform.localScale = Vector3.one * ball.RadiusScale;
                    }
                }
            }
        }

        private void SortByDistance()
        {
            static int Sort(Transform a, Transform b) => a.localPosition.y.CompareTo(b.localPosition.y);

            _cache.Clear();
            foreach (var kv in _ballViewDict)
            {
                _cache.Add(kv.Value.transform);
            }
            _cache.Sort(Sort);
            foreach (var view in _cache)
            {
                view.SetAsFirstSibling();
            }
            _cache.Clear();
        }

        private float SimulatePerspectiveZ(float z)
        {
            var coe = (realHeight - z) / realHeight;
            return viewHeight * (1f - coe);
            // return viewHeight * (1f - coe * coe);
        }

#if UNITY_EDITOR
        private Vector3[] _worldCorners = new Vector3[4];
        private void OnDrawGizmos()
        {
            // 获取梯形的四个角的世界坐标
            var min = (transform as RectTransform).rect.min;
            for (var i = 0; i < 4; i++)
            {
                _worldCorners[i] = transform.TransformPoint(_corners[i] + min);
            }

            // bg
            var fillColor = new Color(0.2f, 0.8f, 0.2f, 0.2f); // 半透明绿色填充
            UnityEditor.Handles.color = fillColor;
            UnityEditor.Handles.DrawAAConvexPolygon(_worldCorners);

            // ball
            UnityEditor.Handles.color = Color.red;
            var lossyScale = ballRoot.lossyScale.x;
            foreach (var ball in _table.Balls)
            {
                var radius = ball.Radius * lossyScale;
                var worldPos = ballRoot.TransformPoint(ball.Pos);
                UnityEditor.Handles.DrawWireDisc(worldPos, Vector3.forward, radius);
            }
        }
#endif
    }
}