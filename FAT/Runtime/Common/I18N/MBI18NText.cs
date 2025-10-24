/**
 * @Author: handong.liu
 * @Date: 2020-08-31 17:17:41
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using EL;
using TMPro;

[ExecuteInEditMode]
public class MBI18NText : MonoBehaviour
{
    public delegate string StringGetter();
    [SerializeField]
    private string mKey = "";
    private bool mEmptyWhenNoText = false;
    private string mPlainText = null;
    private object[] mParams = null;
    private StringGetter mOperator = null;
    private Text mCachedText;
    private TextMesh mCached3DText;
    private TMPro.TMP_Text mCachedTMPText;
    private TextProOnACurve mCachedCurveText;
    public string Key => mKey;

    public static MBI18NText SetEmptyWhenNoText(GameObject gameObject, bool emptyWhenNoText)
    {
        MBI18NText mb = gameObject.GetComponent<MBI18NText>();
        if (mb == null)
        {
            mb = gameObject.AddComponent<MBI18NText>();
        }
        mb.mEmptyWhenNoText = emptyWhenNoText;
        mb._Refresh();
        return mb;
    }

    public static MBI18NText SetPlainText(GameObject gameObject, string plain)
    {
        MBI18NText mb = gameObject.GetComponent<MBI18NText>();
        if (mb == null)
        {
            mb = gameObject.AddComponent<MBI18NText>();
        }
        mb.mPlainText = plain;
        mb.mKey = null;
        mb.mParams = null;
        mb.mOperator = null;
        mb._Refresh();
        return mb;
    }

    public static MBI18NText SetKey(GameObject gameObject, string key)
    {
        MBI18NText mb = gameObject.GetComponent<MBI18NText>();
        if (mb == null)
        {
            mb = gameObject.AddComponent<MBI18NText>();
        }
        mb.mPlainText = null;
        mb.mKey = key;
        mb.mParams = null;
        mb.mOperator = null;
        mb._Refresh();
        return mb;
    }
    public static MBI18NText SetKey(Component com, string key)
    {
        MBI18NText mb = com.GetComponent<MBI18NText>();
        if (mb == null)
        {
            mb = com.gameObject.AddComponent<MBI18NText>();
        }
        mb.mPlainText = null;
        mb.mKey = key;
        mb.mParams = null;
        mb.mOperator = null;
        mb._Refresh();
        return mb;
    }
    public static MBI18NText SetFormatKey(GameObject gameObject, string key, params object[] param)
    {
        MBI18NText mb = gameObject.GetComponent<MBI18NText>();
        if (mb == null)
        {
            mb = gameObject.AddComponent<MBI18NText>();
        }
        mb.mPlainText = null;
        mb.mKey = key;
        mb.mParams = param;
        mb.mOperator = null;
        mb._Refresh();
        return mb;
    }
    public static MBI18NText SetDynamicText(GameObject gameObject, StringGetter oper)
    {
        MBI18NText mb = gameObject.GetComponent<MBI18NText>();
        if (mb == null)
        {
            mb = gameObject.AddComponent<MBI18NText>();
        }
        mb.mPlainText = null;
        mb.mKey = null;
        mb.mParams = null;
        mb.mOperator = oper;
        mb._Refresh();
        return mb;
    }

    public void Awake()
    {
        mCachedText = GetComponent<Text>();
        mCached3DText = GetComponent<TextMesh>();
        mCachedTMPText = GetComponent<TMPro.TMP_Text>();
        mCachedCurveText = GetComponent<TextProOnACurve>();
    }

    public void OnEnable()
    {
        I18N.onLanguageChange += _Refresh;
        _Refresh();
    }

    public void OnDisable()
    {
        I18N.onLanguageChange -= _Refresh;
    }

    private void _Refresh()
    {
#if UNITY_EDITOR
        if (!Preview && !Application.isPlaying) return;
#endif
        if (null != mCachedText || null != mCached3DText || null != mCachedTMPText)
        {
            if (mOperator != null)
            {
                _SetText(mOperator());
            }
            else if (mParams != null)
            {
                _SetText(I18N.FormatText(mKey, mParams));
            }
            else if (mKey != null)
            {
                if (mEmptyWhenNoText)
                {
                    _SetText(I18N.TextNoPlaceholder(mKey));
                }
                else
                {
                    _SetText(I18N.Text(mKey));
                }
            }
            else
            {
                _SetText(mPlainText);
            }
        }
    }

    private void _SetText(string text)
    {
        if (mCachedText != null)
        {
            mCachedText.text = text;
        }
        if (mCached3DText != null)
        {
            mCached3DText.text = text;
        }
        if (mCachedCurveText != null)
        {
            mCachedCurveText.SetText(text);
        }
        else if (mCachedTMPText != null)
        {
            mCachedTMPText.text = text;
        }
    }

#if UNITY_EDITOR
    //editor环境下刷新inspector
    public void OnValidate()
    {
        if (!Preview) return;
        _Refresh();
    }

    private const string MenuPath = "Tools/I18N Preview";
    public static bool Preview
    {
        get => UnityEditor.EditorPrefs.GetBool(MenuPath, false);
        set => UnityEditor.EditorPrefs.SetBool(MenuPath, value);
    }

    [UnityEditor.MenuItem(MenuPath, priority = 11000)]
    private static void TPreview()
    {
        Preview = !Preview;
        UnityEditor.Menu.SetChecked(MenuPath, Preview);
    }
#endif
}