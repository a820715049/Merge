// /**
//  * @Author: handong.liu
//  * @Date: 2022-06-21 19:07:33
//  */
// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEditor;

// [System.Serializable]
// public class ResVariant
// {
//     public string variantName;
//     public BuildTarget target;
//     public TextureImporterFormat textureFormat;
// }

// public class ResVariantConfig : ScriptableObject
// {
//     public string currentVariant;
//     public ResVariant[] variants;
//     public const string kVariantAssetsPath = "Assets/ResVariant.asset";
//     private static ResVariantConfig sCached = null;


//     public static ResVariantConfig Instance {
//         get {
//             if(sCached != null)
//             {
//                 return sCached;
//             }
//             sCached = AssetDatabase.LoadAssetAtPath<ResVariantConfig>(kVariantAssetsPath);
//             if(sCached == null)
//             {
//                 sCached = ScriptableObject.CreateInstance<ResVariantConfig>();
//                 AssetDatabase.CreateAsset(sCached, kVariantAssetsPath);
//             }
//             return sCached;
//         }
//     }

//     public ResVariant GetByTarget(BuildTarget target)
//     {
//         if(string.IsNullOrEmpty(currentVariant))
//         {
//             return null;
//         }
//         foreach(var v in variants)
//         {
//             if(v.target == target && v.variantName == currentVariant)
//             {
//                 return v;
//             }
//         }
//         return null;
//     }

//     public void Save()
//     {
//         AssetDatabase.CreateAsset(this, kVariantAssetsPath);
//         AssetDatabase.SaveAssets();
//     }
// }

