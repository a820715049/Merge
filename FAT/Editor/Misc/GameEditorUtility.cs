/**
 * @Author: handong.liu
 * @Date: 2022-04-01 11:17:21
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEditor;
using static CommonEditorUtility;

namespace DreamMerge
{
    public static class GameEditorUtility
    {
        [MenuItem("Tools/Clear Version Cache", priority = 1001)]
        public static void ClearVersionCache() => AppBuilderEditorUtility.ClearVersionCache(true, VariantEditorUtility.LoadCurrentAppSetting().version);

        [MenuItem("Tools/Recompile", priority = 1002)]
        public static void Recompile()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
    }
}