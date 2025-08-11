/**
 * @Author: handong.liu
 * @Date: 2020-11-02 17:15:09
 */
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;

public class AppSettingEditor : EditorWindow
{
    private string variantName;
    private int curSelected = -1;
    private Vector2 scrollPos;
    void OnGUI()
    {
        GUILayout.Label("配置Variant", EditorStyles.boldLabel);
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        string[] vars = VariantEditorUtility.GetVariantList();
        int currentVar = vars.IndexOf(VariantEditorUtility.GetCurrentVariant());
        if(currentVar >= 0)
        {
            vars[currentVar] += "*";
        }
        curSelected = EditorGUILayout.Popup(curSelected < 0?0:curSelected, vars);
        var selectedVariant = vars[curSelected];
        if(curSelected == currentVar)
        {
            selectedVariant = selectedVariant.Substring(0, selectedVariant.Length - 1);
        }
        var setting = VariantEditorUtility.LoadVariantConfig(selectedVariant);
        
        if(setting != null)
        {
            var editor = Editor.CreateEditor(setting);
            editor.DrawHeader();
            editor.DrawDefaultInspector();
            if(curSelected != currentVar && GUILayout.Button("切换"))
            {
                VariantEditorUtility.SwitchVariant(vars[curSelected]);
            }
            else if(curSelected == currentVar && GUILayout.Button("刷新"))
            {
                VariantEditorUtility.SwitchVariant(selectedVariant);
            }
            if (GUILayout.Button("Switch Editor Env"))
            {
                if (curSelected != currentVar)
                {
                    BuildWrapper.Utility.SwitchVariant_ForEitorRun(vars[curSelected]);
                    DreamMerge.GameEditorUtility.ClearVersionCache();
                }
                else if (curSelected == currentVar)
                {
                    BuildWrapper.Utility.SwitchVariant_ForEitorRun(selectedVariant);
                    DreamMerge.GameEditorUtility.ClearVersionCache();
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.Space();
            OnVariantEditorGUI(selectedVariant, setting);
        }
        EditorGUILayout.EndScrollView();
    }

    protected virtual void OnVariantEditorGUI(string selectedVariant, AppSettings setting)
    {
        
    }
}