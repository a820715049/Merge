/*
 * @Author: tang.yan
 * @Description: 字体材质创建及配置工具 
 * @Date: 2024-11-05 10:11:25
 */
using UnityEditor;
using UnityEngine;
using TMPro;

public class FontMaterialEditorWindow : EditorWindow
{
    private const string MaterialPath = "Assets/Bundle/fat/bundle_font_style/";
    private const string FontAssetPath = "Assets/Bundle/fat/bundle_font/language/base/Font_Normal.asset";
    private const string FontMaterialResPath = "Assets/Bundle/fat/bundle_font_style/Font_MaterialRes.asset";
    private int materialID = -1;
    private MaterialSettings singleMaterialSettings = new MaterialSettings();
    private MaterialSettings materialSettingsA = new MaterialSettings();
    private MaterialSettings materialSettingsB = new MaterialSettings();
    private MaterialSettings materialSettingsC = new MaterialSettings();
    private bool isSingle = false;
    private bool createA = false, createB = false, createC = false;

    [MenuItem("Tools/Font Material Editor")]
    public static void ShowWindow()
    {
        GetWindow<FontMaterialEditorWindow>("Font Material Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Font Material Configuration", EditorStyles.boldLabel);

        // ID Input Field
        materialID = EditorGUILayout.IntField("Material ID", materialID);

        // Material Selection and Options
        isSingle = EditorGUILayout.Toggle("Single Material Only", isSingle);
        if (isSingle)
        {
            createA = createB = createC = false;
            DisplayMaterialSettings(singleMaterialSettings, "Single");
            if (GUILayout.Button("Create Single Material"))
            {
                CreateMaterial($"Font_Normal_{materialID}", singleMaterialSettings);
            }
        }
        else
        {
            GUILayout.Label("Choose which types of material to create (A/B/C):");
            createA = EditorGUILayout.Toggle("Create A Material", createA);
            if (createA)
            {
                DisplayMaterialSettings(materialSettingsA, "A");
                if (GUILayout.Button("Create A Material"))
                {
                    SetDefaultValues(materialSettingsA, "A");
                    CreateMaterial($"Font_Normal_{materialID}a", materialSettingsA);
                }
            }

            createB = EditorGUILayout.Toggle("Create B Material", createB);
            if (createB)
            {
                DisplayMaterialSettings(materialSettingsB, "B");
                if (GUILayout.Button("Create B Material"))
                {
                    SetDefaultValues(materialSettingsB, "B");
                    CreateMaterial($"Font_Normal_{materialID}b", materialSettingsB);
                }
            }

            createC = EditorGUILayout.Toggle("Create C Material", createC);
            if (createC)
            {
                DisplayMaterialSettings(materialSettingsC, "C");
                if (GUILayout.Button("Create C Material"))
                {
                    SetDefaultValues(materialSettingsC, "C");
                    CreateMaterial($"Font_Normal_{materialID}c", materialSettingsC);
                }
            }
        }
    }

    private void OnDisable()
    {
        ClearCachedData();
    }

    private void DisplayMaterialSettings(MaterialSettings settings, string label)
    {
        GUILayout.Label($"Settings for Material {label}", EditorStyles.boldLabel);
        settings.faceColor = EditorGUILayout.ColorField($"Face Color ({label})", settings.faceColor);
        settings.outlineColor = EditorGUILayout.ColorField($"Outline Color ({label})", settings.outlineColor);
        settings.underlayColor = EditorGUILayout.ColorField($"Underlay Color ({label})", settings.underlayColor);

        if (label == "Single")
        {
            settings.faceDilate = EditorGUILayout.Slider($"Face Dilate ({label})", settings.faceDilate, -1, 1);
            settings.outlineThickness = EditorGUILayout.Slider($"Outline Thickness ({label})", settings.outlineThickness, 0, 1);
            settings.underlayOffsetX = EditorGUILayout.Slider($"Underlay Offset X ({label})", settings.underlayOffsetX, -1, 1);
            settings.underlayOffsetY = EditorGUILayout.Slider($"Underlay Offset Y ({label})", settings.underlayOffsetY, -1, 1);
            settings.underlayDilate = EditorGUILayout.Slider($"Underlay Dilate ({label})", settings.underlayDilate, -1, 1);
            settings.underlaySoftness = EditorGUILayout.Slider($"Underlay Softness ({label})", settings.underlaySoftness, 0, 1);
        }
    }

    private void SetDefaultValues(MaterialSettings settings, string label)
    {
        if (label == "A")
        {
            settings.faceDilate = 0.1f;
            settings.underlayOffsetY = -0.2f;
            settings.underlayDilate = 0.8f;
        }
        else if (label == "B")
        {
            settings.underlayOffsetY = -0.2f;
            settings.underlayDilate = 0.6f;
        }
        else if (label == "C")
        {
            settings.underlayOffsetY = -0.2f;
            settings.underlayDilate = 0.4f;
        }
    }

    private void CreateMaterial(string materialName, MaterialSettings settings)
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Debug.LogError($"Font asset not found at {FontAssetPath}. Please provide a valid font asset.");
            return;
        }

        Material referenceMaterial = fontAsset.material;
        if (referenceMaterial == null)
        {
            Debug.LogError("The font asset does not have an associated material.");
            return;
        }
        
        Material newMaterial = new Material(referenceMaterial)
        {
            name = materialName
        };

        newMaterial.SetColor("_FaceColor", settings.faceColor);
        newMaterial.SetColor("_OutlineColor", settings.outlineColor);
        newMaterial.SetColor("_UnderlayColor", settings.underlayColor);
        newMaterial.SetFloat("_FaceDilate", settings.faceDilate);
        newMaterial.SetFloat("_OutlineThickness", settings.outlineThickness);
        newMaterial.SetFloat("_UnderlayOffsetX", settings.underlayOffsetX);
        newMaterial.SetFloat("_UnderlayOffsetY", settings.underlayOffsetY);
        newMaterial.SetFloat("_UnderlayDilate", settings.underlayDilate);
        newMaterial.SetFloat("_UnderlaySoftness", settings.underlaySoftness);
        newMaterial.EnableKeyword("UNDERLAY_ON");  // Ensure Underlay is enabled

        string materialPath = $"{MaterialPath}{materialName}.mat";
        AssetDatabase.CreateAsset(newMaterial, materialPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Material {materialName} created at {materialPath}");

        UpdateFontMaterialRes(materialName, newMaterial, settings);
    }

    private void UpdateFontMaterialRes(string materialName, Material newMaterial, MaterialSettings settings)
    {
        FontMaterialRes fontMaterialRes = AssetDatabase.LoadAssetAtPath<FontMaterialRes>(FontMaterialResPath);

        if (fontMaterialRes == null)
        {
            Debug.LogError($"FontMaterialRes.asset not found at {FontMaterialResPath}. Please ensure the asset exists.");
            return;
        }

        FontMatResConfig existingConfig = fontMaterialRes.GetFontMatResConf(materialID);
        if (existingConfig == null)
        {
            existingConfig = new FontMatResConfig();
            ArrayUtility.Add(ref fontMaterialRes.fontMatRes, existingConfig);
        }

        existingConfig.SetIndex(materialID);
        existingConfig.SetColor(settings.faceColor);
        existingConfig.SetIsSingle(isSingle);
        if (isSingle)
        {
            existingConfig.SetDefaultMat(newMaterial);
        }
        else
        {
            if (materialName.EndsWith("a")) existingConfig.SetMatA(newMaterial);
            else if (materialName.EndsWith("b")) existingConfig.SetMatB(newMaterial);
            else if (materialName.EndsWith("c")) existingConfig.SetMatC(newMaterial);
        }

        EditorUtility.SetDirty(fontMaterialRes);
        AssetDatabase.SaveAssets();
    }

    private void ClearCachedData()
    {
        materialID = -1;
        singleMaterialSettings.Clear();
        materialSettingsA.Clear();
        materialSettingsB.Clear();
        materialSettingsC.Clear();
        isSingle = false;
        createA = createB = createC = false;
    }
}

[System.Serializable]
public class MaterialSettings
{
    public Color faceColor = Color.white;
    public float faceDilate = 0f;
    public Color outlineColor = Color.black;
    public float outlineThickness = 0f;
    public Color underlayColor = Color.black;
    public float underlayOffsetX = 0f;
    public float underlayOffsetY = 0f;
    public float underlayDilate = 0f;
    public float underlaySoftness = 0f;

    public void Clear()
    {
        faceColor = Color.white;
        faceDilate = 0f;
        outlineColor = Color.black;
        outlineThickness = 0f;
        underlayColor = Color.black;
        underlayOffsetX = 0f;
        underlayOffsetY = 0f;
        underlayDilate = 0f;
        underlaySoftness = 0f;
    }
}
