using UnityEngine;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FAT {
    public class SceneViewCapture : MonoBehaviour {
#if UNITY_EDITOR
        public int size;
        public string fileName;

        private void OnValidate() {
            size = Mathf.Max(512, size);
            fileName = string.IsNullOrEmpty(fileName) ? "scene_capture" : fileName;
        }
#endif
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(SceneViewCapture))]
    public class SceneViewCaptureEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            using (var h = new GUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Capture", GUILayout.Width(200), GUILayout.Height(50))) {
                    Capture();
                }
                GUILayout.FlexibleSpace();
            }
        }

        public void ClearRenderTexture(RenderTexture rt, Color c) {
            RenderTexture rta = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, c);
            RenderTexture.active = rta;
        }

        public void Capture() {
            var info = (SceneViewCapture)target;
            var x = info.size;
            var cam = SceneView.lastActiveSceneView.camera;
            var canvas = info.GetComponentInParent<Canvas>();
            var renderMode = RenderMode.ScreenSpaceOverlay;
            if (canvas != null) {
                renderMode = canvas.renderMode;
                canvas.renderMode = RenderMode.WorldSpace;
            }
            var y = Mathf.RoundToInt(x / cam.aspect);
            var rt = RenderTexture.GetTemporary(x, y, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
            ClearRenderTexture(rt, Color.clear);
            var rtActive = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = rtActive;
            rtActive = RenderTexture.active;
            RenderTexture.active = rt;
            if (canvas != null) {
                canvas.renderMode = renderMode;
            }
            var ct = new Texture2D(x, y, TextureFormat.RGBA32, false);
            ct.ReadPixels(new Rect(0, 0, x, y), 0, 0);
            RenderTexture.active = rtActive;
            RenderTexture.ReleaseTemporary(rt);
            var c = ct.EncodeToPNG();
            File.WriteAllBytes($"{info.fileName}.png", c);
        }
    }

#endif
}