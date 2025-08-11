/*
 * @Author: qun.chao
 * @Date: 2024-03-14 17:47:53
 */
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MBSpritePack))]
public class MBSpritePackEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var mb = target as MBSpritePack;
        if (GUILayout.Button("Build"))
        {
            mb.Build();
        }
        if (GUILayout.Button("CopyToTarget"))
        {
            mb.CopyToTarget();
        }
        if (GUILayout.Button("Create New Sprite Pack"))
        {
            mb.CreateNewSpritePack();
        }
    }
}