using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FontMatResConfig))]
public class FontMatResConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 获取字段
        SerializedProperty index = property.FindPropertyRelative("index");
        SerializedProperty color = property.FindPropertyRelative("color");
        SerializedProperty isSingle = property.FindPropertyRelative("isSingle");
        SerializedProperty defaultMat = property.FindPropertyRelative("defaultMat");
        SerializedProperty matA = property.FindPropertyRelative("matA");
        SerializedProperty matB = property.FindPropertyRelative("matB");
        SerializedProperty matC = property.FindPropertyRelative("matC");
        SerializedProperty isGradient = property.FindPropertyRelative("isGradient");
        SerializedProperty gradient = property.FindPropertyRelative("gradient");

        // 计算每个字段的矩形区域
        Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(rect, index);
        
        rect.y += EditorGUIUtility.singleLineHeight + 2;
        EditorGUI.PropertyField(rect, color);

        rect.y += EditorGUIUtility.singleLineHeight + 2;
        EditorGUI.PropertyField(rect, isSingle);

        if (isSingle.boolValue)
        {
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.PropertyField(rect, defaultMat);
        }
        else
        {
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.PropertyField(rect, matA);
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.PropertyField(rect, matB);
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.PropertyField(rect, matC);
        }

        rect.y += EditorGUIUtility.singleLineHeight + 2;
        EditorGUI.PropertyField(rect, isGradient);

        if (isGradient.boolValue)
        {
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.PropertyField(rect, gradient);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty isSingle = property.FindPropertyRelative("isSingle");
        SerializedProperty isGradient = property.FindPropertyRelative("isGradient");

        int lineCount = 3; // index, color, isSingle

        if (isSingle.boolValue)
        {
            lineCount += 1; // defaultMat
        }
        else
        {
            lineCount += 3; // matA, matB, matC
        }

        lineCount += 1; // isGradient

        if (isGradient.boolValue)
        {
            lineCount += 1; // gradient
        }

        return lineCount * (EditorGUIUtility.singleLineHeight + 2);
    }
}
