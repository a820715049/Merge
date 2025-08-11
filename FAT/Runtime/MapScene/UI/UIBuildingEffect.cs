/*
 * @Author: tang.yan
 * @Description: 场景建筑物效果UI 
 * @Date: 2025-04-27 12:04:33
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using EL;

namespace FAT
{
    public class UIBuildingEffect : UIBase
    {
        [SerializeField] private RawImage rawImage;
        [SerializeField] private GameObject effect;
        
        [Header("高亮颜色变化 0 表示时间开始时用的颜色，1 表示结束时用的颜色")]
        public Gradient highlightColorGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new (Color.white, 0f), new (new Color(1f, 0.84f, 0f), 0.5f), new (Color.white, 1f)
            }
        };
        
        [Header("高亮强度 (0~1) 0 表示不高亮，1 表示全高亮 ")]
        public AnimationCurve highlightStrengthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
        [Header("发光强度 (0~1) 0 表示无额外发光，1 表示最大发光")]
        public AnimationCurve glowIntensityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.5f), new Keyframe(1f, 0f));
        
        [Header("希望要显示的特效数量")]
        public int effectCount = 20;
        [Header("特效要分成多少组播放")]
        public int effectGroupCount = 10;
        [Header("每组特效间隔播放的时间")]
        public float effectGroupInterval = 0.05f;
        
        [Header("决定某个点位要显示特效的alpha阈值")]
        public byte threshold = 180;

        //叠加图片使用的材质球
        private Material overlayMat;
        private List<Vector2> _uvPoints = new List<Vector2>();  //根据规则采样到的uv坐标
        private List<Vector3> _uiWorldPoints = new List<Vector3>();   // 转换后的世界坐标列表
        private List<GameObject> _effectList = new List<GameObject>();
        private float _duration = 0f;
        private Coroutine _coEffect;
        private Coroutine _coHighlight;

        protected override void OnCreate()
        {
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MAP_BUILDING_EFFECT, effect);
            var buildingEffect = Game.Manager.mapSceneMan.MapEffectForBuilding;
            if (buildingEffect == null) return;
            _CancelSafeEdge();
            //运行时new一个材质球
            overlayMat = new Material(CommonRes.Instance.buildingGlowMat);
            if (overlayMat != null)
            {
                //将材质球赋值到rawImage上,并绑定两张RT图
                rawImage.material = overlayMat;
                //每次开启界面都刷新一下两张RT图
                overlayMat.SetTexture("_MainTex", buildingEffect.GetCameraAllRT());
                overlayMat.SetTexture("_OverlayTex", buildingEffect.GetCameraBuildingRT());
                //刷新初始参数
                _ApplyMaterial(0);
            }
        }

        //取消UIManager中的安全区范围，使得界面RT图呈现出来的内容和主相机渲染出来的内容 保持一致
        private void _CancelSafeEdge()
        {
            var safeRoot = UIManager.Instance.SafeRoot;
            var trans = rawImage.rectTransform;
            //默认trans的offsetMin offsetMax都为prefab上的预设值(0,0)
            trans.offsetMin = -safeRoot.offsetMin;
            trans.offsetMax = -safeRoot.offsetMax;
        }

        protected override void OnParse(params object[] items)
        {
            
        }

        protected override void OnPreOpen()
        {
            var mapSceneMan = Game.Manager.mapSceneMan;
            var buildingEffect = mapSceneMan.MapEffectForBuilding;
            if (buildingEffect == null) return;
            _CancelSafeEdge();
            _duration = mapSceneMan.scene.info.decorate.buildingFinishDelay;
            rawImage.enabled = true;
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.MAP_EFFECT_RT_READ_BACK>().AddListener(_OnEffectRTReadBack);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.MAP_EFFECT_RT_READ_BACK>().RemoveListener(_OnEffectRTReadBack);
        }

        protected override void OnPostClose()
        {
            _ClearCoroutine();
            rawImage.enabled = false;
            foreach (var obj in _effectList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.MAP_BUILDING_EFFECT, obj);
            }
            _effectList.Clear();
        }
        
        private void _ClearCoroutine()
        {
            if (_coEffect != null)
            {
                StopCoroutine(_coEffect);
                _coEffect = null;
            }
            if (_coHighlight != null)
            {
                StopCoroutine(_coHighlight);
                _coHighlight = null;
            }
        }
        
        private void _OnEffectRTReadBack(NativeArray<Color32> pixels, int rtWidth, int rtHeight)
        {
            //采样uv坐标
            _GenerateUVSamplePoints(pixels, rtWidth, rtHeight);
            //将uv坐标转成世界坐标 供特效播放
            _ConvertUVToWorldPoints();
            //播高亮动画和特效
            _ClearCoroutine();
            _coEffect = StartCoroutine(_CoPlayEffect());
            _coHighlight = StartCoroutine(_CoPlayHighlight());
        }

        private IEnumerator _CoPlayEffect()
        {
            if (_uiWorldPoints.Count <= 0) 
                yield break;
            // 确保按 X 升序
            _uiWorldPoints.Sort((a, b) => a.x.CompareTo(b.x));
            // 轮询分配到 N 组
            var groups = new List<List<Vector3>>(effectGroupCount);
            for (int g = 0; g < effectGroupCount; g++)
                groups.Add(new List<Vector3>());

            for (int i = 0; i < _uiWorldPoints.Count; i++)
            {
                int g = i % effectGroupCount;
                groups[g].Add(_uiWorldPoints[i]);
            }

            // 每隔 delta 播一组
            foreach (var group in groups)
            {
                foreach (var point in group)
                {
                    var obj = GameObjectPoolManager.Instance
                        .CreateObject(PoolItemType.MAP_BUILDING_EFFECT, transform);
                    obj.transform.position = point;
                    obj.gameObject.SetActive(true);
                    _effectList.Add(obj);
                }
                yield return new WaitForSeconds(effectGroupInterval);
            }
        }

        private IEnumerator _CoPlayHighlight()
        {
            // 如果材质为空或 duration 不合法，就什么也不做
            if (overlayMat == null || _duration <= 0f)
                yield break;
            
            var elapsed = 0f;
            while (elapsed < _duration)
            {
                // 归一化 t 到 [0,1]
                var t = Mathf.Clamp01(elapsed / _duration);
                //设置材质球
                _ApplyMaterial(t);
                // 累加时间
                elapsed += Time.deltaTime;
                yield return null;
            }
            // 确保结束时 t = 1 对应的状态被赋值一次（防止最后一帧未完全触发）
            _ApplyMaterial(1);
        }

        private void _ApplyMaterial(float t)
        {
            if (overlayMat == null)
                return;
            // 从曲线和渐变里取出当前值
            Color curHighlightColor = highlightColorGradient.Evaluate(t);
            float curHighlightStrength = highlightStrengthCurve.Evaluate(t);
            float curGlowIntensity = glowIntensityCurve.Evaluate(t);

            // 更新材质参数
            overlayMat.SetColor("_HighlightColor", curHighlightColor);
            overlayMat.SetFloat("_HighlightStrength", curHighlightStrength);
            overlayMat.SetFloat("_GlowIntensity", curGlowIntensity);
        }
        
        /// <summary>
        /// 将二值化 Mask 划分成 gridX×gridY 个小格子，
        /// 每格随机找一个 alpha >= 阈值 的像素作为采样点，直到凑够 count。
        /// </summary>
        private void _GenerateUVSamplePoints(NativeArray<Color32> pixels, int w, int h)
        {
            _uvPoints.Clear();
            if (pixels.Length <= 0) return;
            MapBuildingMaskSampler.GenerateUniformUVPoints(_uvPoints, pixels, w, h, effectCount, threshold);
        }
        
        //将uv坐标转换成当前UI对应的世界坐标
        private void _ConvertUVToWorldPoints()
        {
            _uiWorldPoints.Clear();
            if (_uvPoints.Count <= 0) return;
            var rt = rawImage.rectTransform;
            var rect = rt.rect;
            foreach (var uv in _uvPoints)
            {
                // 1) 计算在 RawImage 本地坐标系下的点
                var localX = Mathf.Lerp(rect.xMin, rect.xMax, uv.x);
                var localY = Mathf.Lerp(rect.yMin, rect.yMax, uv.y);
                var localPos = new Vector3(localX, localY, 0);
                // 2) 转换到世界坐标（Canvas 为 Overlay 时其实是屏幕坐标）
                var worldPos = rt.TransformPoint(localPos);
                _uiWorldPoints.Add(worldPos);
            }
        }

        private void OnDestroy()
        {
            // 1. 断开 RawImage 对材质的引用
            rawImage.material = null;

            // 2. 销毁材质对象
            if (overlayMat != null)
            {
                overlayMat.SetTexture("_MainTex", null);
                overlayMat.SetTexture("_OverlayTex", null);
                UnityEngine.Object.Destroy(overlayMat);
                overlayMat = null;
            }
        }
    }
}
