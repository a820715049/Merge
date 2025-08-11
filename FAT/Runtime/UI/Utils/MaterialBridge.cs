using UnityEngine;
using UnityEngine.UI;
using System;
using EL;
using TMPro;

namespace UnityEngine.UI
{
    [ExecuteInEditMode]
    public class MaterialBridge : MonoBehaviour
    {
        public Graphic target;
        private Vector2 offsetMainTexRef;
        public Vector2 offsetMainTex;

        public void OnValidate() {
            TryGetComponent<Graphic>(out target);
        }

        public void Update() {
            if (target == null || offsetMainTex == offsetMainTexRef) return;
            offsetMainTexRef = offsetMainTex;
            target.material.SetTextureOffset("_MainTex", offsetMainTex);
        }
    }
}