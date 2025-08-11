
using UnityEngine;
using UnityEditor;
using System;

using static AssetBundleArranger;
using Spine.Unity;

public class AssetImport : AssetPostprocessor
{
    internal string AssetPath => assetPath.Contains('\\') ? assetPath.Replace('\\', '/') : assetPath;

    internal static bool Contains(ReadOnlySpan<char> s_, string t_) => s_.Contains(t_, StringComparison.OrdinalIgnoreCase);
    internal static int IndexOf(ReadOnlySpan<char> s_, string t_) => s_.IndexOf(t_, StringComparison.OrdinalIgnoreCase);

    private static TextureImporterSettings _settings = new();

    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains(kAssetBundleRootName)) return;
        var path = AssetPath.AsSpan();
        var target = (TextureImporter)assetImporter;
        if (Contains(path, "/sprite/")
            || Contains(path, "/sp_"))
        {
            target.textureType = TextureImporterType.Sprite;
            target.textureCompression = TextureImporterCompression.Compressed;
        }
        if (Contains(path, "/spine/"))
        {
            target.textureCompression = TextureImporterCompression.Compressed;
            var tickAlpha = !Contains(path, "s004") && !Contains(path, "s005");
            target.alphaIsTransparency = tickAlpha;
            var sExt = path.LastIndexOf('.');
            var pAtlas = $"{path[..sExt].ToString()}_Atlas.asset";
            var atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(pAtlas);
            //note: won't work during initial import
            if (atlas != null)
            {
                var v = tickAlpha ? 1 : 0;
                foreach (var mat in atlas.materials)
                {
                    mat.SetInt("_StraightAlphaInput", v);
                }
            }
        }
        if (Contains(path, "/card/card_"))
        {
            var s = target.GetPlatformTextureSettings("Android");
            s.format = TextureImporterFormat.ASTC_4x4;
            target.SetPlatformTextureSettings(s);
        }

        // 以下路径纯互斥
        if (Contains(path, "/bundle_item"))
        {
            target.textureType = TextureImporterType.Sprite;
            target.textureCompression = TextureImporterCompression.Compressed;
            target.maxTextureSize = 256;// 棋盘item尺寸卡到256
        }
        else if (Contains(path, "/bundle_card"))
        {
            target.textureType = TextureImporterType.Sprite;
            target.textureCompression = TextureImporterCompression.Compressed;
        }
        else if (Contains(path, "/bundle_building"))
        {
            // 建筑图片设置为FullRect 减少面数
            if (Contains(path, "sp_map_building"))
            {
                target.ReadTextureSettings(_settings);
                _settings.spriteMeshType = SpriteMeshType.FullRect;
                target.SetTextureSettings(_settings);
            }
        }
        else if (Contains(path, "/bundle_map"))
        {
            // 地图图片设置为FullRect 减少面数
            if (Contains(path, "sp_map"))
            {
                target.ReadTextureSettings(_settings);
                _settings.spriteMeshType = SpriteMeshType.FullRect;
                target.SetTextureSettings(_settings);
            }
        }
    }
}