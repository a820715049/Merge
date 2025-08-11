/*
 * @Author: qun.chao
 * @Date: 2022-03-18 10:13:13
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro.EditorUtilities;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(UITextExt), true)]
    // [CanEditMultipleObjects]
    public class UITextExtEditor : TextEditor
    {
        static readonly GUIContent k_TitleLabel_1 = new GUIContent("标题1级");
        static readonly GUIContent k_TitleLabel_2 = new GUIContent("标题2级");
        static readonly GUIContent k_TitleLabel_3 = new GUIContent("标题3级");
        static readonly GUIContent k_DescLabel_1 = new GUIContent("描述1级");
        static readonly GUIContent k_DescLabel_2 = new GUIContent("描述2级");
        static readonly GUIContent k_DescLabel_3 = new GUIContent("描述3级");
        static readonly GUIContent k_ButtonLabel_1 = new GUIContent("按钮1");
        static readonly GUIContent k_ButtonLabel_2 = new GUIContent("按钮2");
        static readonly GUIContent k_ButtonLabel_3 = new GUIContent("按钮3");

        protected SerializedProperty m_TextStyleProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_TextStyleProp = serializedObject.FindProperty("m_TextStyle");
        }

        public override void OnInspectorGUI()
        {
            // if (GUILayout.Button("设置字体为Asap-Regular"))
            // {
            //     _FixFont();
            // }

            EditorGUILayout.LabelField("设置预制样式");

            EditorGUI.BeginChangeCheck();

            int styleValue = m_TextStyleProp.intValue;
            int _selected = 0;

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);
            rect.width = rect.width / 4f;
            EditorGUILayout.BeginHorizontal();
            _AddTextStyle(ref rect, k_TitleLabel_1, 1, ref _selected);
            _AddTextStyle(ref rect, k_TitleLabel_2, 2, ref _selected);
            _AddTextStyle(ref rect, k_TitleLabel_3, 3, ref _selected);
            EditorGUILayout.EndHorizontal();

            rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);
            rect.width = rect.width / 4f;
            EditorGUILayout.BeginHorizontal();
            _AddTextStyle(ref rect, k_DescLabel_1, 4, ref _selected);
            _AddTextStyle(ref rect, k_DescLabel_2, 5, ref _selected);
            _AddTextStyle(ref rect, k_DescLabel_3, 6, ref _selected);
            EditorGUILayout.EndHorizontal();

            rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);
            rect.width = rect.width / 4f;
            EditorGUILayout.BeginHorizontal();
            _AddTextStyle(ref rect, k_ButtonLabel_1, 7, ref _selected);
            _AddTextStyle(ref rect, k_ButtonLabel_2, 8, ref _selected);
            _AddTextStyle(ref rect, k_ButtonLabel_3, 9, ref _selected);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                if (_selected != 0 && styleValue != _selected)
                {
                    // apply
                    var _textExt = serializedObject.targetObject as UITextExt;
                    _textExt.TextStyle = _selected;
                    // dirty
                    EditorUtility.SetDirty(_textExt);
                }
            }

            base.OnInspectorGUI();
        }

        private void _AddTextStyle(ref Rect rect, GUIContent gui, int type, ref int selected)
        {
            selected = TMP_EditorUtility.EditorToggle(rect, _IsStyle(type), gui, TMP_UIStyleManager.alignmentButtonLeft) && !_IsStyle(type) ? type : selected;
            rect.x += rect.width;
        }

        private bool _IsStyle(int v)
        {
            return m_TextStyleProp.intValue == v;
        }

        private void _FixFont()
        {
            TextHelper.FixFont(serializedObject.targetObject as UITextExt);
        }
    }
}