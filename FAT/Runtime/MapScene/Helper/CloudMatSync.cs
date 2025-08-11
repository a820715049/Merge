using System.Collections;
using System;
using UnityEngine;
using EL.Resource;
using Config;
using Spine.Unity;
using System.Collections.Generic;
using DG.Tweening;

namespace FAT
{
    [ExecuteInEditMode]
    public class CloudMatSync : MonoBehaviour
    {
        public Renderer target;
        public Material mat;
        public float value;
        public float soft = 0.25f;
        public float alpha;
        public Vector4 offset = new(1, 1, 0, 0);
        public bool useBlock;
        private Vector4 st;
        private Vector4 fade;
        private Color color;
        private int pST;
        private int pFade;
        private int pColor;
        private static MaterialPropertyBlock block;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            if (target == null || mat == null)
            {
                if (!TryGetComponent(out target)) return;
                mat = target?.sharedMaterial;
            }
        }
#endif

        public void Update()
        {
            if (pST == 0 || pFade == 0)
            {
                pST = Shader.PropertyToID("_FadeTex_ST");
                pFade = Shader.PropertyToID("_Fade");
                pColor = Shader.PropertyToID("_Color");
                color = Color.white;
            }

            block ??= new MaterialPropertyBlock();
            if (fade.x != value || fade.y != soft
                                || color.a != alpha
                                || offset != st)
            {
                fade.x = value;
                fade.y = soft;
                color.a = alpha;
                st = offset;
                Refresh();
            }
        }

        public void Refresh()
        {
            if (useBlock)
            {
                target.GetPropertyBlock(block);
                block.SetVector(pFade, fade);
                block.SetVector(pST, st);
                block.SetColor(pColor, color);
                target.SetPropertyBlock(block);
            }
            else
            {
                mat.SetVector(pFade, fade);
                mat.SetVector(pST, st);
                mat.SetColor(pColor, color);
            }
        }
    }
}