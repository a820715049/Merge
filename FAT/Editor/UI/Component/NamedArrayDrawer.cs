// https://forum.unity.com/threads/how-to-change-the-name-of-list-elements-in-the-inspector.448910/
using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEditor;

[CustomPropertyDrawer(typeof(NamedArrayAttribute))]
public class NamedArrayDrawer : PropertyDrawer
{
    private List<string> overrideNames;
    private List<string> tooltipNames;

    // "an attribute argument must be a constant expression"
    // 在运行中动态cache
    private NamedArrayAttribute self;
    private void _EnsureSelf()
    {
        if (self == null)
        {
            self = (NamedArrayAttribute)attribute;
        }
        if (self.et.IsEnum && overrideNames == null)
        {
            overrideNames = new();
            tooltipNames = new();
            var names = System.Enum.GetValues(self.et);
            foreach (var n in names)
            {
                var memInfo =  self.et.GetMember(n.ToString());
                var atts = memInfo[0].GetCustomAttributes(typeof(TooltipAttribute), false);
                if (atts != null && atts.Length > 0)
                    tooltipNames.Add((atts[0] as TooltipAttribute).tooltip);
                else
                    tooltipNames.Add(string.Empty);
                overrideNames.Add(n.ToString());
            }
        }
    }

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        try
        {
            _EnsureSelf();
            int pos = int.Parse(property.propertyPath.Split('[', ']')[1]);
            if (overrideNames != null && pos < overrideNames.Count)
            {
                EditorGUI.PropertyField(rect, property, new GUIContent(overrideNames[pos], tooltipNames[pos]));
            }
            else if (self.names != null && pos < self.names.Length)
            {
                EditorGUI.PropertyField(rect, property, new GUIContent(self.names[pos]));
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label);
            }
        }
        catch
        {
            EditorGUI.PropertyField(rect, property, label);
        }
    }

}