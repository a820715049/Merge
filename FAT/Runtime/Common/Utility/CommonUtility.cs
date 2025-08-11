/**
 * @Author: handong.liu
 * @Date: 2020-07-15 18:48:15
 */
using System.IO;
using System.Text;
using System;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using FAT;

public static class CommonUtility
{
    public static bool IsTouchOverUI(int fingerId)
    {
        if(Input.touchSupported)
        {
            return EventSystem.current.IsPointerOverGameObject(fingerId);
        }
        else
        {
            return EventSystem.current.IsPointerOverGameObject() || EventSystem.current.IsPointerOverGameObject(0);
        }
    }

    public static ulong DigestStringToUlong(string str)
    {
        ulong ret = 1;
        
        for(int i = 0; i < str.Length; i++)
        {
            ret = ret ^ ((ulong)str[i] << (i % 4) * 2);
        }
        return ret;
    }

    public static string GetUUID()
    {
        return System.Guid.NewGuid().ToString();
    }

    private static StringBuilder sbCache = new();
    private static MD5 md5 = new MD5CryptoServiceProvider();
    /// <summary>
    /// 获取文件MD5值
    /// </summary>
    /// <param name="fileName">文件绝对路径</param>
    /// <returns>MD5值</returns>
    public static string GetMD5HashFromFile(string fileName)
    {
        try
        {
            using (FileStream file = new FileStream(fileName, FileMode.Open))
            {
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                sbCache.Clear();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sbCache.Append(retVal[i].ToString("x2"));
                }
            }
            return sbCache.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"GetMD5HashFromFile() failed {ex.Message}@{fileName}");
            return string.Empty;
        }
    }

    public static void SafeWalk<T>(IEnumerable<T> target, System.Action<T> cb)
    {
        var list = EL.ObjectPool<List<T>>.GlobalPool.Alloc();
        list.Clear();
        list.AddRange(target);
        for(int i = list.Count - 1; i >= 0; i--)
        {
            cb(list[i]);
        }
        EL.ObjectPool<List<T>>.GlobalPool.Free(list);
    } 

    // unsafe public static ulong GetBitmap<T>(IList<T> bitmap) where T: unmanaged
    // {
    //     int l = (sizeof(ulong) + sizeof(T) - 1) / sizeof(T);
    //     ulong ret = 0;
    //     byte* dst = (byte*)&ret;
    //     byte* endDst = dst + sizeof(ulong);
    //     for(int i = 0; i < l && i < bitmap.Count; i++)
    //     {
    //         T elem = bitmap[i];
    //         byte* src = (byte*)&elem;
    //         for(int j = 0; j < sizeof(T) && dst < endDst; j++, src++, dst++)
    //         {
    //             *dst = *src;
    //         }
    //     }
    //     return ret;
    // }
    // unsafe public static void SetBitmap<T>(ulong map, IList<T> bitmap) where T: unmanaged
    // {
    //     int l = (sizeof(ulong) + sizeof(T) - 1) / sizeof(T);
    //     byte* src = (byte*)&map;
    //     byte* endSrc = src + sizeof(ulong);
    //     bitmap.Clear();
    //     for(int i = 0; i < l; i++)
    //     {
    //         T elem = default(T);
    //         byte* dst = (byte*)&elem;
    //         for(int j = 0; j < sizeof(T) && src < endSrc; j++, src++, dst++)
    //         {
    //             *dst = *src;
    //         }
    //         bitmap.Add(elem);
    //     }
    // }

    private static bool sDpiInited = false;
    private static float sDPI = 0;
    public static float GetScreenDPI()
    {
        if(!sDpiInited)
        {
            sDpiInited = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            /* old way
            AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
        
            AndroidJavaObject metrics = new AndroidJavaObject("android.util.DisplayMetrics");
            activity.Call<AndroidJavaObject>("getWindowManager").Call<AndroidJavaObject>("getDefaultDisplay").Call("getMetrics", metrics);
        
            sDPI = (metrics.Get<float>("xdpi") + metrics.Get<float>("ydpi")) * 0.5f;
            */
            sDPI = Screen.dpi;
#else
            sDPI = Screen.dpi;
#endif  
            EL.DebugEx.FormatInfo("CommonUtility ----> init dpi to {0}", sDPI);
        }
        return sDPI;
    }

    public static float ConvertDotDistanceToDpi170(float distance)
    {
        var dpi = GetScreenDPI();
        if(dpi > 1)
        {
            return (float)distance * 170f / dpi;
        }
        else
        {
            return distance;
        }
    }

    public static Vector3 GetBestCameraPos(Bounds worldBounds, Camera camera)
    {
        Transform cameraTransform = camera.transform;
        Vector3 viewDir = camera.transform.forward;
        Vector3 targetPt = worldBounds.min;
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        Vector3 referencePt = center + viewDir * 999;
        Bounds cameraSpaceBounds = default(Bounds);
        bool firstIter = true;
        for(int dx = -1; dx <= 1; dx += 2)
        {
            for(int dy = -1; dy <= 1; dy += 2)
            {
                for(int dz = -1; dz <= 1; dz += 2)
                {
                    Vector3 p = new Vector3(center.x + dx * extents.x, center.y + dy * extents.y, center.z + dz * extents.z);
                    p = cameraTransform.InverseTransformPoint(p);
                    if(firstIter)
                    {
                        firstIter = false;
                        cameraSpaceBounds = new Bounds(p, Vector3.zero);
                    }
                    else
                    {
                        cameraSpaceBounds.Encapsulate(p);
                    }
                }
            }
        }
        var requiredFrustumHeight = Mathf.Max(cameraSpaceBounds.max.y - cameraSpaceBounds.min.y, (cameraSpaceBounds.max.x - cameraSpaceBounds.min.x) / camera.aspect);
        var distance = requiredFrustumHeight * 0.5f / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var deltaPos = new Vector3(-cameraSpaceBounds.center.x, -cameraSpaceBounds.center.y, distance - cameraSpaceBounds.min.z);
        return camera.transform.position - cameraTransform.TransformVector(deltaPos);
    }

    public static bool FakeClickItem(GameObject go)
    {
        var inters = go.GetComponents<UnityEngine.EventSystems.IPointerClickHandler>();
        if(inters != null && EventSystem.current != null)
        {
            var ev = new PointerEventData(EventSystem.current);
            ev.button = PointerEventData.InputButton.Left;
            foreach(var inter in inters)
            {
                inter.OnPointerClick(ev);
            }
            return inters.Length > 0;
        }
        else
        {
            return false;
        }
    }

    public static bool RestartApp()
    {
        #if MINIGAME_WX
        WeChatWASM.WX.RestartMiniProgram();
        return true;
        #else
        return false;
        #endif
    }

    public static void QuitApp()
    {
        #if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            Game.Instance.mainEntry.StopAllCoroutines();
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Game.Instance.mainEntry.StopAllCoroutines();
            Application.Quit();
        #endif
    }

    public static void CheckReuseOrCreateCipher(ref System.Security.Cryptography.ICryptoTransform encoder, ref System.Security.Cryptography.ICryptoTransform decoder)
    {
        if(encoder == null || !encoder.CanReuseTransform ||
            decoder == null || !decoder.CanReuseTransform)
        {
            var m = new System.Security.Cryptography.AesManaged();
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(Constant.kArchiveKey1 + Constant.kArchiveKey2);
            var keyBytes = new byte[256/8];
            for(int i = 0; i < keyBytes.Length; i++)
            {
                keyBytes[i] = bytes[i % bytes.Length];
            }
            m.Key = keyBytes;
            m.Mode = System.Security.Cryptography.CipherMode.ECB;
            m.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            encoder = m.CreateEncryptor();
            decoder = m.CreateDecryptor();
        }
    }


    //只在编辑器用
    static public void DrawTextInEditor(string text, Vector3 worldPosition, Color textColor, Vector2 anchor, float textSize = 15f)
    {
#if UNITY_EDITOR
        var view = UnityEditor.SceneView.currentDrawingSceneView;
        if (!view)
            return;
        Vector3 screenPosition = view.camera.WorldToScreenPoint(worldPosition);
        if (screenPosition.y < 0 || screenPosition.y > view.camera.pixelHeight || screenPosition.x < 0 || screenPosition.x > view.camera.pixelWidth || screenPosition.z < 0)
            return;
        var pixelRatio = UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.right).x - UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.zero).x;
        UnityEditor.Handles.BeginGUI();
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)textSize,
            normal = new GUIStyleState() { textColor = textColor }
        };
        Vector2 size = style.CalcSize(new GUIContent(text)) * pixelRatio;
        var alignedPosition =
            ((Vector2)screenPosition +
            size * ((anchor + Vector2.left + Vector2.up) / 2f)) * (Vector2.right + Vector2.down) +
            Vector2.up * view.camera.pixelHeight;
        GUI.Label(new Rect(alignedPosition / pixelRatio, size / pixelRatio), text, style);
        UnityEditor.Handles.EndGUI();
#endif
    }
}