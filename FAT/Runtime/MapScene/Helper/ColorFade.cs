using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace FAT {
    public class ColorFade : MonoBehaviour {
        
        [Serializable]
        public class Entry {
            public Renderer renderer;         // 渲染器
            public int      materialIndex;    // 第几号材质（sharedMaterials 数组下标）
            public Color    baseColor;        // 记录该材质的原始颜色
            [NonSerialized] public MaterialPropertyBlock block;
        }

        public bool includeChild;
        public Vector2 from = Vector2.one, to = Vector2.one * 0.75f;
        public float interval = 0.8f;

        [SerializeField] 
        private List<Entry> entries = new List<Entry>();
        private Tween tween;

    #if UNITY_EDITOR
        private void OnValidate() 
        {
            if (Application.isPlaying) return;
            CollectEntries();
        }
    #endif

        private void Awake() 
        {
            if (entries.Count == 0)
                CollectEntries();

            // 现在可以直接给引用类型字段赋值
            foreach (var e in entries) 
            {
                e.block = new MaterialPropertyBlock();
            }
        }

        private void CollectEntries() 
        {
            var set = new HashSet<Renderer>();
            entries.Clear();

            if (TryGetComponent<Renderer>(out var r))
                set.Add(r);
            if (includeChild)
                foreach (var rr in GetComponentsInChildren<Renderer>(true))
                    set.Add(rr);

            foreach (var rr in set) 
            {
                var mats = rr.sharedMaterials;
                if (mats == null) continue;
                for (int i = 0; i < mats.Length; i++) 
                {
                    var mat = mats[i];
                    entries.Add(new Entry {
                        renderer      = rr,
                        materialIndex = i,
                        baseColor     = mat.color,
                        block         = new MaterialPropertyBlock()
                    });
                }
            }
        }

        private void ApplyFade(Vector2 v)
        {
            float scale = v.x;
            float alpha = v.y;
            foreach (var e in entries) {
                var mpb = e.block;
                // 获取该 sub‐mesh 当前的 PropertyBlock
                e.renderer.GetPropertyBlock(mpb, e.materialIndex);

                // 计算新色值
                var c = e.baseColor;
                var col = new Color(c.r * scale, c.g * scale, c.b * scale, alpha);
                
                //这里因为不同shader要设置的字段名可能不一样，暂时先都处理一下
                //设置UI_Shader/Effect/distort_add、UI_Shader/Effect/distort_blend中的颜色
                e.block.SetColor("_TintColor", col);
                //设置Spine/Skeleton Tint中的颜色
                e.block.SetColor("_Color", col);

                // 提交到指定下标的材质
                e.renderer.SetPropertyBlock(mpb, e.materialIndex);
            }
        }

        public void Fade(Vector2 v) => ApplyFade(v);

        public void Preview(bool enable) {
            tween?.Kill();
            if (enable) {
                tween = DOVirtual.Float(0, 1, interval, t => {
                    ApplyFade(Vector2.Lerp(from, to, t));
                }).SetLoops(-1, LoopType.Yoyo);
            } else {
                ApplyFade(Vector2.one);
            }
        }
    }
}
