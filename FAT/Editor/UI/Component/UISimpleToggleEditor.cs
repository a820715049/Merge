using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(UISimpleToggle), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the Toggle Component.
    ///   Extend this class to write a custom editor for an Toggle-derived component.
    /// </summary>
    public class UISimpleToggleEditor : SelectableEditor
    {
        SerializedProperty m_OnValueChangedProperty;
        SerializedProperty m_TransitionProperty;
        SerializedProperty m_GraphicProperty;
        SerializedProperty m_GroupProperty;
        SerializedProperty m_NormalRootProperty;
        SerializedProperty m_SelectedRootProperty;
        SerializedProperty m_ContentRootProperty;
        SerializedProperty m_IsOnProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_TransitionProperty = serializedObject.FindProperty("toggleTransition");
            m_GraphicProperty = serializedObject.FindProperty("graphic");
            m_GroupProperty = serializedObject.FindProperty("m_Group");
            m_IsOnProperty = serializedObject.FindProperty("m_IsOn");
            m_OnValueChangedProperty = serializedObject.FindProperty("onValueChanged");

            m_NormalRootProperty = serializedObject.FindProperty("m_NormalRoot");
            m_SelectedRootProperty = serializedObject.FindProperty("m_SelectedRoot");
            m_ContentRootProperty = serializedObject.FindProperty("m_ContentRoot");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_IsOnProperty);
            if (EditorGUI.EndChangeCheck())
            {
                UISimpleToggle toggle = serializedObject.targetObject as UISimpleToggle;
                UISimpleToggleGroup group = m_GroupProperty.objectReferenceValue as UISimpleToggleGroup;

                toggle.isOn = m_IsOnProperty.boolValue;

                if (group != null && toggle.IsActive())
                {
                    if (toggle.isOn || (!group.AnyTogglesOn() && !group.allowSwitchOff))
                    {
                        toggle.isOn = true;
                        group.NotifyToggleOn(toggle);
                        EditorUtility.SetDirty(toggle);
                    }
                }
            }

            // EditorGUILayout.PropertyField(m_TransitionProperty);
            // EditorGUILayout.PropertyField(m_GraphicProperty);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_GroupProperty);
            if (EditorGUI.EndChangeCheck())
            {
                UISimpleToggle toggle = serializedObject.targetObject as UISimpleToggle;
                UISimpleToggleGroup group = m_GroupProperty.objectReferenceValue as UISimpleToggleGroup;
                toggle.group = group;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_NormalRootProperty);
            EditorGUILayout.PropertyField(m_SelectedRootProperty);
            EditorGUILayout.PropertyField(m_ContentRootProperty);

            // Draw the event notification options
            EditorGUILayout.PropertyField(m_OnValueChangedProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
