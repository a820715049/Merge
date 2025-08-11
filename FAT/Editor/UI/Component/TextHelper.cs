/*
 * @Author: qun.chao
 * @Date: 2021-06-09 18:55:11
 */
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Config;

public static class TextHelper
{
    [MenuItem("Tools/Text/ChangeToTargetFont")]
    /// <summary>
    /// 所有merge项目的font替换为默认
    /// </summary>
    public static void SetToTargetFont()
    {
        var goodFont = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
        if(goodFont == null) {
            return;
        }
        if (EditorUtility.DisplayDialog("长时间操作警告", $"修改所有字体为{goodFont.name}?", "好", "算了"))
        {
            System.Action<GameObject, List<Text>> processor = (go, list) =>
            {
                _TryFix(list, go, goodFont, false);
            };
            _ProcessForAllFont(processor);
        }
    }

    [MenuItem("Tools/Text/FixFont")]
    /// <summary>
    /// 所有merge项目的font替换为默认
    /// </summary>
    public static void SetToDefault()
    {
        var goodFont = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        System.Action<GameObject, List<Text>> processor = (go, list) =>
        {
            _TryFix(list, go, goodFont, false);
        };
        _ProcessForAllFont(processor);
    }

    public static void SetTextTo(Font goodFont)
    {
        System.Action<GameObject, List<Text>> processor = (go, list) =>
        {
            _TryFix(list, go, goodFont, false);
        };
        _ProcessForAllFont(processor);
    }

    /// <summary>
    /// 找到所有使用Asap-Bold字体的text组件 替换成使用Asap-Reqular
    /// </summary>
    public static void SetToAsap()
    {
        var goodFontGuids = AssetDatabase.FindAssets("Asap-Regular" + " t:font");
        if (goodFontGuids.Length != 1)
        {
            Debug.LogFormat("Asap-Regular not found or duplicated");
            return;
        }
        var goodFont = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(goodFontGuids[0]), typeof(Font)) as Font;
        System.Action<GameObject, List<Text>> processor = (go, list) =>
        {
            _TryFix(list, go, goodFont, true);
        };
        _ProcessForAllFont(processor);
    }

    private static void _ProcessForAllFont(System.Action<GameObject, List<Text>> processor)
    {
        // var uiDirs = new[] { "Assets/Bundle/ui/bundle_dreammerge/Prefab/Hud" };
        // var allGuids = AssetDatabase.FindAssets("t:prefab", uiDirs);

        var uiDirs = new[] { "Assets/Bundle/ui" };
        var allGuids = AssetDatabase.FindAssets("t:prefab", uiDirs);
        int count = allGuids.Length;

        GameObject go = null;
        var cache = new List<Text>();
        var sb = new StringBuilder();

        Debug.LogFormat("[TextHelper] ============== begin ==============");

        for (int i = 0; i < count; i++)
        {
            sb.Clear();
            sb.Append(AssetDatabase.GUIDToAssetPath(allGuids[i]));
            var cancel = EditorUtility.DisplayCancelableProgressBar("Fix Font", sb.ToString(), (float)i / count);
            if (cancel)
            {
                break;
            }

            go = AssetDatabase.LoadAssetAtPath(sb.ToString(), typeof(GameObject)) as GameObject;
            if (go != null)
            {
                cache.Clear();
                go.GetComponentsInChildren<Text>(true, cache);
                processor?.Invoke(go, cache);
            }
        }
        EditorUtility.ClearProgressBar();
        Debug.LogFormat("[TextHelper] ============== end ==============");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.LogFormat("[TextHelper] saved");
    }

    private static void _TryFix(List<Text> list, GameObject go, Font good, bool forceBold)
    {
        bool found = false;
        foreach (var t in list)
        {
            if (t.font != good)
            {
                t.font = good;
                if (forceBold)
                    t.fontStyle = FontStyle.Bold;
                found = true;
            }
        }
        if (found)
        {
            EditorUtility.SetDirty(go);
            Debug.LogFormat("[TextHelper] applied to {0}", go.name);
        }
    }

    public static void FixFont(Text _text)
    {
        var goodFontGuids = AssetDatabase.FindAssets("Asap-Regular" + " t:font");
        if (goodFontGuids.Length != 1)
        {
            Debug.LogFormat("Asap-Regular not found or duplicated");
            return;
        }
        var goodFont = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(goodFontGuids[0]), typeof(Font)) as Font;
        if (_text.font != goodFont)
        {
            _text.font = goodFont;
            EditorUtility.SetDirty(_text);
        }
    }

    [MenuItem("Tools/Text/Refresh TextStyle")]
    public static void Refresh()
    {
        var uiDirs = new[] { "Assets/Bundle/ui" };
        var allGuids = AssetDatabase.FindAssets("t:prefab", uiDirs);
        int count = allGuids.Length;

        GameObject go = null;
        var cache = new List<UITextExt>();
        var sb = new StringBuilder();

        Debug.LogFormat("[TextHelper] ============== begin ==============");

        for (int i = 0; i < count; i++)
        {
            sb.Clear();
            sb.Append(AssetDatabase.GUIDToAssetPath(allGuids[i]));
            var cancel = EditorUtility.DisplayCancelableProgressBar("refresh text btyle", sb.ToString(), (float)i / count);
            if (cancel)
            {
                break;
            }

            go = AssetDatabase.LoadAssetAtPath(sb.ToString(), typeof(GameObject)) as GameObject;
            if (go != null)
            {
                cache.Clear();
                go.GetComponentsInChildren<UITextExt>(true, cache);
                _TryRefresh(cache, go);
            }
        }
        EditorUtility.ClearProgressBar();
        Debug.LogFormat("[TextHelper] ============== end ==============");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.LogFormat("[TextHelper] saved");
    }

    private static void _TryRefresh(List<UITextExt> list, GameObject go)
    {
        bool found = false;
        foreach (var t in list)
        {
            if (TextUtility.RefreshTextStyle(t.TextStyle, t))
                found = true;
        }
        if (found)
        {
            EditorUtility.SetDirty(go);
            Debug.LogFormat("[TextHelper] applied to {0}", go.name);
        }
    }


    [MenuItem("GameObject/UI/Text - Ext", false)]
    public static void AddAccordionGroup(MenuCommand menuCommand)
    {
        // Create a custom game object
        GameObject go = new GameObject("Text");
        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        var _text = go.AddComponent<UITextExt>();
        _text.TextStyle = 1;
        // FixFont(_text);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }

    [MenuItem("Tools/Text/Convert Text To TextExt")]
    public static void UseTextExt()
    {
        var uiDirs = new[] { "Assets/Bundle/ui" };
        // var uiDirs = new[] { "Assets/Bundle/ui/bundle_dreammerge/Prefab/Hud" };
        var allGuids = AssetDatabase.FindAssets("t:prefab", uiDirs);
        int count = allGuids.Length;

        GameObject go = null;
        var cache = new List<Text>();
        var sb = new StringBuilder();

        Debug.LogFormat("[TextHelper] ============== begin ==============");

        for (int i = 0; i < count; i++)
        {
            sb.Clear();
            sb.Append(AssetDatabase.GUIDToAssetPath(allGuids[i]));
            var cancel = EditorUtility.DisplayCancelableProgressBar("Convert Text to TextExt", sb.ToString(), (float)i / count);
            if (cancel)
            {
                break;
            }
            go = AssetDatabase.LoadAssetAtPath(sb.ToString(), typeof(GameObject)) as GameObject;
            if (go != null)
            {
                cache.Clear();
                go.GetComponentsInChildren<Text>(true, cache);
                _TryConvert(cache, go);
            }
        }
        EditorUtility.ClearProgressBar();
        Debug.LogFormat("[TextHelper] ============== end ==============");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.LogFormat("[TextHelper] saved");
    }

    private static void _TryConvert(List<Text> list, GameObject go)
    {
        bool found = false;
        foreach (var t in list)
        {
            if (CanConvertTo<UITextExt>(t))
            {
                ConvertTo<UITextExt>(t);
                found = true;
            }
        }
        if (found)
        {
            EditorUtility.SetDirty(go);
            Debug.LogFormat("[FontChecker] applied to {0}", go.name);
        }
    }


    // 替换脚本摘自 https://www.jianshu.com/p/baf1a0eb0298
    [MenuItem("CONTEXT/Text/Convert To UITextExt", true)]
    static bool _ConvertToUITextExt(MenuCommand command)
    {
        return CanConvertTo<UITextExt>(command.context);
    }
    [MenuItem("CONTEXT/Text/Convert To UITextExt", false)]
    static void ConvertToUITextExt(MenuCommand command)
    {
        ConvertTo<UITextExt>(command.context);
    }
    // [MenuItem("CONTEXT/Text/Convert To UILinkText", true)]
    // static bool _ConvertToUILinkTextExt(MenuCommand command)
    // {
    //     return CanConvertTo<UILinkText>(command.context);
    // }
    // [MenuItem("CONTEXT/Text/Convert To UILinkText", false)]
    // static void ConvertToUILinkTextExt(MenuCommand command)
    // {
    //     ConvertTo<UILinkText>(command.context);
    // }
    public static bool CanConvertTo<T>(Object context)
        where T : MonoBehaviour
    {
        return context && context.GetType() != typeof(T);
    }
    public static void ConvertTo<T>(Object context) where T : MonoBehaviour
    {
        var target = context as MonoBehaviour;
        var so = new SerializedObject(target);
        so.Update();
        bool oldEnable = target.enabled;
        target.enabled = false;
        foreach (var script in Resources.FindObjectsOfTypeAll<MonoScript>())
        {
            if (script.GetClass() != typeof(T)) continue;
            so.FindProperty("m_Script").objectReferenceValue = script;
            so.ApplyModifiedProperties();
            break;
        }
        (so.targetObject as MonoBehaviour).enabled = oldEnable;
    }

    #region text setting

    // static bool _GetActiveText(out Text text)
    // {
    //     text = Selection.activeGameObject?.GetComponent<Text>();
    //     if (text == null)
    //         UnityEngine.Debug.LogWarning("text component not find");
    //     return text != null;
    // }

    // // https://docs.google.com/spreadsheets/d/1KJH9n0QqrPkgTLemH4dNVbd_6Zk55aPbjCPbWZb9gBg/edit?usp=sharing
    // [MenuItem("Tools/Text/1. 标题1级 %#_F1")]
    // static void TextSetting_Title_1()
    // {
    //     if (!_GetActiveText(out var _text)) return;
    //     TextUtility.TextSetting_Title_1(_text);
    //     EditorUtility.SetDirty(_text.gameObject);
    // }

    // [MenuItem("Tools/Text/2. 标题2级 %#_F2")]
    // static void TextSetting_Title_2()
    // {
    //     if (!_GetActiveText(out var _text)) return;
    //     TextUtility.TextSetting_Title_2(_text);
    //     EditorUtility.SetDirty(_text.gameObject);
    // }

    // [MenuItem("Tools/Text/3. 描述1级 %#_F3")]
    // static void TextSetting_Desc_1()
    // {
    //     if (!_GetActiveText(out var _text)) return;
    //     TextUtility.TextSetting_Desc_1(_text);
    //     EditorUtility.SetDirty(_text.gameObject);
    // }

    // [MenuItem("Tools/Text/4. 描述2级 %#_F4")]
    // static void TextSetting_Desc_2()
    // {
    //     if (!_GetActiveText(out var _text)) return;
    //     TextUtility.TextSetting_Desc_2(_text);
    //     EditorUtility.SetDirty(_text.gameObject);
    // }

    // [MenuItem("Tools/Text/5. 描述3级 %#_F5")]
    // static void TextSetting_Desc_3()
    // {
    //     if (!_GetActiveText(out var _text)) return;
    //     TextUtility.TextSetting_Desc_3(_text);
    //     EditorUtility.SetDirty(_text.gameObject);
    // }

    // [MenuItem("Tools/Text/6. 按钮1级 %#_F6")]
    // static void TextSetting_Button_1()
    // {
    //     if (!_GetActiveText(out var _text)) return;
    //     TextUtility.TextSetting_Button_1(_text);
    //     EditorUtility.SetDirty(_text.gameObject);
    // }

    // [MenuItem("Tools/Text/7. 按钮2级 %#_F7")]
    // static void TextSetting_Button_2()
    // {
    //     if (!_GetActiveText(out var _text)) return;
    //     TextUtility.TextSetting_Button_2(_text);
    //     EditorUtility.SetDirty(_text.gameObject);
    // }

    #endregion
}