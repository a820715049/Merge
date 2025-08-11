/*
 * @Author: qun.chao
 * @Date: 2022-03-18 10:00:14
 */
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI
{

    // https://docs.google.com/spreadsheets/d/1KJH9n0QqrPkgTLemH4dNVbd_6Zk55aPbjCPbWZb9gBg/edit?usp=sharing
    public static class TextUtility
    {
        // public enum TextStyleStr
        // {
        //     Title1,
        //     Title2,
        //     Title3,
        //     Desc1,
        //     Desc2,
        //     Desc3,
        //     Button1,
        //     Button2,
        //     Button3,
        // }

        public static bool ConvertOldStyle(int _style, UITextExt _text)
        {
            if (_style >= 3 && _style <= 7)
            {
                _text.TextStyle = _style + 1;
                return true;
            }
            return false;
        }

        public static bool RefreshTextStyle(int _style, Text _text)
        {
            switch (_style)
            {
                case 1: TextSetting_Title_1(_text); break;
                case 2: TextSetting_Title_2(_text); break;
                case 3: TextSetting_Title_3(_text); break;
                case 4: TextSetting_Desc_1(_text); break;
                case 5: TextSetting_Desc_2(_text); break;
                case 6: TextSetting_Desc_3(_text); break;
                case 7: TextSetting_Button_1(_text); break;
                case 8: TextSetting_Button_2(_text); break;
                case 9: TextSetting_Button_3(_text); break;
                default: return false;
            }
            return true;
        }

        public static void TextSetting_Title_1(Text _text)
        {
            _text.fontSize = 48;
            _text.fontStyle = FontStyle.Bold;
            _text.color = _MakeColor("#ffffff");
            _RemoveShadowAndOutline(_text);
            _MakeOutline(_text, _MakeColor("#7f493d"), 2);
        }

        public static void TextSetting_Title_2(Text _text)
        {
            _text.fontSize = 42;
            _text.fontStyle = FontStyle.Bold;
            _text.color = _MakeColor("#ffffff");
            _RemoveShadowAndOutline(_text);
            _MakeOutline(_text, _MakeColor("#7f493d"), 2);
        }

        public static void TextSetting_Title_3(Text _text)
        {
            _text.fontSize = 42;
            _text.fontStyle = FontStyle.Bold;
            _RemoveShadowAndOutline(_text);
            _SetOutlineAndShadow(_text, "#000000");
        }

        public static void TextSetting_Desc_1(Text _text)
        {
            _text.fontSize = 36;
            _text.fontStyle = FontStyle.Bold;
            _text.color = _MakeColor("#7f493d");
            _RemoveShadowAndOutline(_text);
        }

        public static void TextSetting_Desc_2(Text _text)
        {
            _text.fontSize = 30;
            _text.fontStyle = FontStyle.Bold;
            _text.color = _MakeColor("#7f493d");
            _RemoveShadowAndOutline(_text);
        }

        public static void TextSetting_Desc_3(Text _text)
        {
            _text.fontSize = 24;
            _text.fontStyle = FontStyle.Bold;
            _text.color = _MakeColor("#7f493d");
            _RemoveShadowAndOutline(_text);
        }

        public static void TextSetting_Button_1(Text _text)
        {
            _text.fontSize = 46;
            _text.fontStyle = FontStyle.Bold;
            _text.color = Color.white;
            _RemoveShadowAndOutline(_text);
            _SetOutlineAndShadow(_text, "#007c51");
        }

        public static void TextSetting_Button_2(Text _text)
        {
            _text.fontSize = 40;
            _text.fontStyle = FontStyle.Bold;
            _text.color = Color.white;
            _RemoveShadowAndOutline(_text);
            _SetOutlineAndShadow(_text, "#ad5102");
        }

        public static void TextSetting_Button_3(Text _text)
        {
            _text.fontSize = 30;
            _text.fontStyle = FontStyle.Bold;
            _text.color = Color.white;
            _RemoveShadowAndOutline(_text);
            _SetOutlineAndShadow(_text, "#007c51");
        }

        static void _SetOutlineAndShadow(Text _text, string str)
        {
            var col = _MakeColor(str);
            _MakeOutline(_text, col, 2);
            _MakeShadow(_text, col, 2);
        }

        static Color _MakeColor(string str)
        {
            if (!ColorUtility.TryParseHtmlString(str, out var col))
                return Color.white;
            return col;
        }

        static void _MakeOutline(Text text, Color col, int offset)
        {
            var _outline = text.GetComponent<Outline>();
            if (_outline == null)
                _outline = text.gameObject.AddComponent<Outline>();
            _outline.effectColor = col;
            _outline.effectDistance = new Vector2(offset, -offset);
        }

        static void _MakeShadow(Text text, Color col, int offset)
        {
            var _shadow = text.GetComponent<Shadow>();
            if (_shadow == null || _shadow is Outline)
                _shadow = text.gameObject.AddComponent<Shadow>();
            _shadow.effectColor = col;
            _shadow.effectDistance = new Vector2(0, -offset);
        }

        static void _RemoveShadowAndOutline(Text text)
        {
            var _comps = text.GetComponents<UnityEngine.UI.Shadow>();
            foreach (var item in _comps)
            {
                Component.DestroyImmediate(item, true);
            }
        }
    }

    public class UITextExt : Text
    {
        [SerializeField] private int m_TextStyle;

        public int TextStyle
        {
            get { return m_TextStyle; }
            set
            {
                if (TextUtility.RefreshTextStyle(value, this))
                    m_TextStyle = value;
            }
        }
    }
}