/*
 * @Author: tang.yan
 * @Description: 美术字体材质资源列表 
 * @Date: 2024-01-04 15:01:39
 */

using EL;
using UnityEngine;
using TMPro;
using FAT;

[System.Serializable]
public class FontMatResConfig
{
    //美术字体对应文档：https://docs.google.com/spreadsheets/d/1yvFqfdFvekQMkB1JcOhm-vOu68sRo4KSGyM4NXGacaw/edit#gid=0
    //不同档位字体形式说明：http://svn.ifunplus.cn/svn/merge-art/FAT/%e5%ad%97%e4%bd%93%e8%a7%84%e8%8c%83/
    
    //基本信息
    [Tooltip("与美术表对应的材质序号")] public int index;   //与美术表对应的材质序号
    [Tooltip("字体颜色")] public Color color;     //字体颜色
    //材质球信息
    [SerializeField] [Tooltip("是否只用单一材质球")] private bool isSingle;             //是否只用单一材质球
    [SerializeField] [Tooltip("默认使用的材质")] private Material defaultMat;          //默认使用的材质 
    [SerializeField] [Tooltip("A类材质球(对应字体大小0-50)")] private Material matA;    //A类材质球(对应字体大小0-50) 
    [SerializeField] [Tooltip("B类材质球(对应字体大小51-100)")] private Material matB;  //B类材质球(对应字体大小51-100) 
    [SerializeField] [Tooltip("C类材质球(对应字体大小101-max)")] private Material matC; //C类材质球(对应字体大小101-max) 
    //字体渐变信息
    [SerializeField] [Tooltip("是否使用字体渐变")] private bool isGradient; //是否使用字体渐变
    [Tooltip("字体渐变预设")] public TMP_ColorGradient gradient;  //字体渐变预设
    
    // 公共方法用于设置字段
    public void SetIndex(int newIndex) => index = newIndex;
    public void SetColor(Color newColor) => color = newColor;
    public void SetIsSingle(bool value) => isSingle = value;
    public void SetDefaultMat(Material mat) => defaultMat = mat;
    public void SetMatA(Material mat) => matA = mat;
    public void SetMatB(Material mat) => matB = mat;
    public void SetMatC(Material mat) => matC = mat;
    
    public void ApplyFontMatResConfig(TMP_Text tmpText)
    {
        if (tmpText == null)
            return;
        //设置材质球
        var mat = GetUsableMaterial(tmpText);
        if (mat != null)
            tmpText.fontSharedMaterial = mat;
        else
        {
            var trans = tmpText.transform;
            var parentName = UIManager.Instance.GetBelongUIPrefabName(trans);
            DebugEx.FormatError("[FontMatResConfig]: Empty Material! ConfigIndex = {0}, PrefabName = {1}, NodeName = {2}, FontSize = {3}", 
                index, parentName, trans.name, tmpText.fontSize);
        }
        //设置字体颜色
        tmpText.color = color;
        //设置字体渐变
        if (isGradient && gradient != null)
        {
            tmpText.enableVertexGradient = true;
            tmpText.colorGradientPreset = gradient;
        }
        else
        {
            tmpText.enableVertexGradient = false;
            tmpText.colorGradientPreset = null;
        }
    }
    
    private Material GetUsableMaterial(TMP_Text tmpText)
    {
        if (isSingle)
            return defaultMat;
        var fontSize = tmpText.fontSize;
        return fontSize switch
        {
            <= 50 => matA,
            > 50 and <= 100 => matB,
            > 100 => matC,
            _ => null
        };
    }
}

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/FontMaterialRes", order = 2)]
public class FontMaterialRes : ScriptableObject
{
    public static FontMaterialRes Instance;
    public TMP_FontAsset mainFontAsset;
    public FontMatResConfig[] fontMatRes;

    public FontMatResConfig GetFontMatResConf(int fontIndex)
    {
        if(fontMatRes != null)
        {
            foreach(var conf in fontMatRes)
            {
                if(conf.index == fontIndex)
                {
                    return conf;
                }
            }
        }
        return null;
    }
}