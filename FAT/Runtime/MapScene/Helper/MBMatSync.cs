using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using Config;
using Spine.Unity;

namespace FAT
{
    [ExecuteInEditMode]
    public class MBMatSync : MonoBehaviour
    {
        public Material mat;
        public float displacement;
        public float diffraction;
        private Vector4 control;

#if UNITY_EDITOR
        private void OnValidate() {
            if (UnityEditor.EditorApplication.isPlaying) return;
            if (mat == null) {
                var r = GetComponent<Graphic>();
                mat = r.material;
            }
        }
#endif

        public void Update() {
            if (displacement != control.x || diffraction != control.y) {
                control = new Vector4(displacement, diffraction, 0, 0);
                mat.SetVector("_Control", control);
            }
        }
    }
}


