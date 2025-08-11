/*
 * @Author: qun.chao
 * @Date: 2024-05-27 14:08:17
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

[CustomPreview(typeof(RectTransform))]
public class UIPrefabPreview : ObjectPreview
{
    private GameObject uiPrefab;
    private Texture textureCached;
    private Vector2 uiRefSize = new(1080f, 1920f);

    public override bool HasPreviewGUI()
    {
        if (target is RectTransform trans)
        {
            // 仅预览场景外的prefab
            uiPrefab = trans.gameObject;
            return !trans.gameObject.scene.IsValid();
        }
        return false;
    }

    public override GUIContent GetPreviewTitle()
    {
        var content = base.GetPreviewTitle();
        content.text = $"UIPreview > {target.name}";
        return content;
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        if (uiPrefab == null)
            return;
        if (textureCached == null)
        {
            textureCached = _GetUIPreview(uiPrefab);
        }
        if (textureCached != null)
        {
            GUI.DrawTexture(r, textureCached, ScaleMode.ScaleToFit);
        }
    }

    public override void OnPreviewSettings()
    {
        if (GUILayout.Button("Refresh"))
        {
            textureCached = _GetUIPreview(uiPrefab, true);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (textureCached != null)
        {
            Object.DestroyImmediate(textureCached);
            textureCached = null;
        }
    }


    private Texture _GeneratePreview(GameObject go, string filePath)
    {
        // setup
        var cam = new GameObject("_preview_cam_").AddComponent<Camera>();
        cam.hideFlags = HideFlags.HideAndDontSave;
        cam.clearFlags = CameraClearFlags.Color;
        cam.orthographic = true;
        cam.backgroundColor = Color.black;
        cam.cullingMask = LayerMask.GetMask("UI", "SceneUI");
        var canvas = new GameObject("_preview_canvas_").AddComponent<Canvas>();
        canvas.gameObject.layer = LayerMask.NameToLayer("UI");
        canvas.hideFlags = HideFlags.HideAndDontSave;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.matchWidthOrHeight = 1;
        scaler.referenceResolution = uiRefSize;
        var _obj = (GameObject)PrefabUtility.InstantiatePrefab(go, canvas.transform);
        _obj.transform.localPosition = Vector3.zero;

        // get rt
        float scale = 0.5f;
        var rt = RenderTexture.GetTemporary((int)(uiRefSize.x * scale), (int)(uiRefSize.y * scale), 0, RenderTextureFormat.ARGB32);

        // prefab => rt
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = null;

        // rt => tex2d
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex2d = new Texture2D(rt.width, rt.height);
        tex2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = previous;
        tex2d.Apply();

        // save to file
        var bytes = tex2d.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);

        // release rt
        RenderTexture.ReleaseTemporary(rt);

        // clean
        Object.DestroyImmediate(canvas.gameObject);
        Object.DestroyImmediate(cam.gameObject);

        return tex2d;
    }

    private Texture _GetUIPreview(GameObject go, bool forceUpdate = false)
    {
        Texture tex = null;
        var filePath = _GetPreviewPngPath(go);
        if (!string.IsNullOrEmpty(filePath))
        {
            if (File.Exists(filePath) && !forceUpdate)
            {
                var bytes = File.ReadAllBytes(filePath);
                var tex2d = new Texture2D(1,1);
                tex2d.LoadImage(bytes);
                tex = tex2d;
            }
            else
            {
                tex = _GeneratePreview(go, filePath);
            }
        }
        return tex;
    }

    private string _GetPreviewCachePath()
    {
        var path = Path.Combine(Application.dataPath, "..", "Temp", "UIThumbnail");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    private string _GetPreviewPngPath(GameObject go)
    {
        var path = AssetDatabase.GetAssetPath(go);
        var guid = AssetDatabase.AssetPathToGUID(path);
        return Path.Combine(_GetPreviewCachePath(), $"{guid}.png");
    }
}
