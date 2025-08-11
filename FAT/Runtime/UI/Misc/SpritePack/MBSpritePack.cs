/*
 * @Author: qun.chao
 * @Date: 2024-03-14 17:35:44
 */
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEngine.TextCore;

// 1. 用格子组织sprite
// 2. 手动生成位置完全确定的简单图集 避免修改单个图片导致图集变化
// 3. 超过cellSize尺寸的图片自动适配到cellSize
// 4. 以cell中心为pivot 方便统一调整参数
// 5. 允许小图向上适配

// 新增 SpriteInfo 结构体
[System.Serializable]
public struct SpriteInfo
{
    public Sprite sprite;
    public string name;
}

public class MBSpritePack : MonoBehaviour
{
    public List<SpriteInfo> sprites;
    public Vector2Int cellSize;
    public Vector2Int dimensionSize;
    public bool expandToFitCell = true;
    public Texture2D copyTarget;
    public TMP_SpriteAsset targetSpriteAsset;

    public void Build()
    {
        _CheckSprite();
        _CheckSize();
#if UNITY_EDITOR
        _Generate();
#endif
    }

    private bool _CheckSprite()
    {
        foreach (var sp in sprites)
        {
            if (sp.sprite == null)
                continue;
            if (sp.sprite.texture.width > cellSize.x || sp.sprite.texture.height > cellSize.y)
            {
                Debug.LogError($"texture {sp.name} biggger than {cellSize}");
                return false;
            }
        }
        return true;
    }

    private void _CheckSize()
    {
        if (dimensionSize.x <= 0 || dimensionSize.x >= 20 ||
            dimensionSize.y <= 0 || dimensionSize.y >= 20)
        {
            throw new System.Exception("dimension too big or negative");
        }

        if (cellSize.x <= 0 || cellSize.x >= 500 ||
            cellSize.y <= 0 || cellSize.y >= 500)
        {
            throw new System.Exception("cellsize too big or negative");
        }
    }

#if UNITY_EDITOR
    private string _GetOutputFilePath()
    {
        var path = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
        var f = new FileInfo(path);
        var target_png = Path.Combine(f.Directory.FullName, "output_pack.png");
        return target_png;
    }

    private void _Generate()
    {
        var target_png = _GetOutputFilePath();

        var width = cellSize.x * dimensionSize.x;
        var height = cellSize.y * dimensionSize.y;

        var tex = new Texture2D(width, height);
        var cols = Enumerable.Repeat(Color.clear, width * height).ToArray();
        tex.SetPixels(cols);
        tex.alphaIsTransparency = true;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < sprites.Count; i++)
        {
            var sp = sprites[i].sprite;
            if (sp == null) continue;
            var x = i % dimensionSize.x;
            var y = (dimensionSize.y - 1) - i / dimensionSize.x;
            _DumpSprite(tex, sp, x * cellSize.x, y * cellSize.y);
        }
        tex.Apply();

        _SaveToPNG(tex, target_png);

        AssetDatabase.Refresh();
        Debug.Log("Build Success");
    }

    private void _DumpSprite(Texture2D tex, Sprite sp, int startX, int startY)
    {
        var w = sp.texture.width;
        var h = sp.texture.height;

        var (rt_w, rt_h) = _ResolveRenderTextureSize(w, h);
        var tmp = RenderTexture.GetTemporary(rt_w, rt_h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(sp.texture, tmp);
        var previous = RenderTexture.active;
        RenderTexture.active = tmp;

        var offset_x = (int)((cellSize.x - rt_w) * 0.5f);
        var offset_y = (int)((cellSize.y - rt_h) * 0.5f);

        tex.ReadPixels(new Rect(0, 0, rt_w, rt_h), startX + offset_x, startY + offset_y);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);
    }

    private (int, int) _ResolveRenderTextureSize(int w, int h)
    {
        if (!expandToFitCell)
        {
            if (w <= cellSize.x && h <= cellSize.y)
                return (w, h);
        }
        var cellRatio = cellSize.x * 1.0f / cellSize.y;
        var spriteRatio = w * 1.0f / h;
        if (spriteRatio > cellRatio)
        {
            return (cellSize.x, Mathf.RoundToInt(cellSize.x / spriteRatio));
        }
        else
        {
            return (Mathf.RoundToInt(cellSize.y * spriteRatio), cellSize.y);
        }
    }

    private void _SaveToPNG(Texture2D tex, string filePath)
    {
        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
    }

    public void CopyToTarget()
    {
        _CopyToTarget();
    }

    private void _CopyToTarget()
    {
        if (copyTarget == null)
            return;
        var targetPath = UnityEditor.AssetDatabase.GetAssetPath(copyTarget);
        var output_file = _GetOutputFilePath();
        if (File.Exists(output_file))
        {
            File.Copy(output_file, targetPath, true);
            Debug.Log($"CopyToTarget Success, from : {output_file} , to : {targetPath}");
            AssetDatabase.Refresh();
            // 更新 Sprite Asset
            _UpdateSpriteAsset();
        }
    }

    private void _UpdateSpriteAsset()
    {
        if (targetSpriteAsset == null)
        {
            Debug.LogError("Target Sprite Asset is not assigned.");
            return;
        }

        // 确保 spriteSheet 是最新的
        targetSpriteAsset.spriteSheet = copyTarget;

        // 增量更新 spriteCharacterTable 和 spriteGlyphTable
        for (int i = 0; i < sprites.Count; i++)
        {
            var spriteInfo = sprites[i];
            if (spriteInfo.sprite == null) continue;

            // 检查是否已经存在对应的条目
            var existingCharacter = targetSpriteAsset.spriteCharacterTable.FirstOrDefault(c => c.name == spriteInfo.name);
            if (existingCharacter == null)
            {
                // 计算图集中的位置
                int x = i % dimensionSize.x;  // 计算列
                int y = (dimensionSize.y - 1) - i / dimensionSize.x;  // 计算行，从下往上填充

                // 创建新的 TMP_SpriteGlyph
                var spriteGlyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    //默认bearingY为高度值的0.75
                    metrics = new GlyphMetrics(cellSize.x, cellSize.y, 0, cellSize.y * 0.75f, cellSize.x),
                    glyphRect = new GlyphRect(x * cellSize.x, y * cellSize.y, cellSize.x, cellSize.y),
                    //默认缩放比例为1.4
                    scale = 1.4f,
                    atlasIndex = 0
                };

                // 创建新的 TMP_SpriteCharacter
                var spriteCharacter = new TMP_SpriteCharacter
                {
                    glyph = spriteGlyph,
                    glyphIndex = spriteGlyph.index,  // 确保 glyphIndex 与 glyph 的 index 一致
                    unicode = 65534, // 使用默认的 Unicode 值
                    name = spriteInfo.name,
                    scale = 1f
                };

                // 添加到表中
                targetSpriteAsset.spriteGlyphTable.Add(spriteGlyph);
                targetSpriteAsset.spriteCharacterTable.Add(spriteCharacter);
            }
        }

        // 更新查找表并保存
        targetSpriteAsset.UpdateLookupTables();
        EditorUtility.SetDirty(targetSpriteAsset);
        AssetDatabase.SaveAssets();
    }

    public void CreateNewSpritePack()
    {
        if (copyTarget == null || targetSpriteAsset == null)
        {
            Debug.LogError("Copy Target or Target Sprite Asset is not assigned.");
            return;
        }

        // 获取 copy target 图片名字中的数字
        string copyTargetName = copyTarget.name;
        int lastUnderscoreIndex = copyTargetName.LastIndexOf('_');
        if (lastUnderscoreIndex == -1 || !int.TryParse(copyTargetName.Substring(lastUnderscoreIndex + 1), out int currentNumber))
        {
            Debug.LogError("Copy Target name does not follow the expected format 'sprite_pack_x'.");
            return;
        }

        // 重置当前基础字段
        sprites.Clear();
        sprites.Add(new SpriteInfo());
        cellSize = new Vector2Int(128, 128);
        dimensionSize = new Vector2Int(8, 8);
        expandToFitCell = true;

        // 新图片和 asset 的命名
        string newSpriteName = $"sprite_pack_{currentNumber + 1}";
        string newAssetName = $"sprite_pack_{currentNumber + 1}";

        // 目标路径
        string targetFolderPath = "Assets/Bundle/fat/bundle_font_sprite";

        // 检查路径是否存在，如果不存在则创建
        if (!Directory.Exists(targetFolderPath))
        {
            Directory.CreateDirectory(targetFolderPath);
            Debug.Log($"Created directory: {targetFolderPath}");
        }

        // 创建并保存新的Texture2D
        Texture2D newTexture = new Texture2D(cellSize.x * dimensionSize.x, cellSize.y * dimensionSize.y, TextureFormat.RGBA32, false);
        string newSpritePath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolderPath}/{newSpriteName}.png");
        byte[] textureData = newTexture.EncodeToPNG();
        File.WriteAllBytes(newSpritePath, textureData);

        // 触发 Unity 文件刷新，确保 Texture2D 被正确创建
        AssetDatabase.Refresh();

        // 加载新创建的 Texture2D
        Texture2D loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newSpritePath);
        if (loadedTexture == null)
        {
            Debug.LogError("Failed to load the newly created Texture2D.");
            return;
        }

        // 创建新的 TMP_SpriteAsset，并使用加载后的 Texture2D
        TMP_SpriteAsset newSpriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        newSpriteAsset.name = newAssetName;
        newSpriteAsset.spriteSheet = loadedTexture;
        newSpriteAsset.spriteInfoList = new List<TMP_Sprite>();
        
        // 保存新的 TMP_SpriteAsset
        string newAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolderPath}/{newAssetName}.asset");
        AssetDatabase.CreateAsset(newSpriteAsset, newAssetPath);
        // 重新导入 newSpriteAsset，确保其状态是最新的
        AssetDatabase.ImportAsset(newAssetPath, ImportAssetOptions.ForceUpdate);

        // 再次触发 Unity 文件刷新，确保 TMP_SpriteAsset 被正确创建
        AssetDatabase.Refresh();
        //在创建成功后才赋值材质，避免TMP_SpriteAsset.UpgradeSpriteAsset报错
        newSpriteAsset.material = GetDefaultSpriteMaterial(newSpriteAsset, loadedTexture);
        var a = newSpriteAsset.spriteCharacterTable.Count;//主动触发TMP_SpriteAsset.UpgradeSpriteAsset

        // 将 SpriteCurrency 设置为新 TMP_SpriteAsset 的 fallback
        string spriteCurrencyPath = $"{targetFolderPath}/SpriteCurrency.asset";
        TMP_SpriteAsset spriteCurrency = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(spriteCurrencyPath);
        if (spriteCurrency != null)
        {
            if (spriteCurrency.fallbackSpriteAssets == null)
            {
                spriteCurrency.fallbackSpriteAssets = new List<TMP_SpriteAsset>();
            }
            spriteCurrency.fallbackSpriteAssets.Add(newSpriteAsset);
            EditorUtility.SetDirty(spriteCurrency);
        }
        // 将新创建的 Texture2D 和 TMP_SpriteAsset 赋值给字段
        copyTarget = loadedTexture;
        targetSpriteAsset = newSpriteAsset;

        // 标记当前对象为"脏"状态，确保修改被保存
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log("CreateNewSpritePack Success");
    }
    
    private Material GetDefaultSpriteMaterial(TMP_SpriteAsset spriteAsset, Texture spriteSheet)
    {
        ShaderUtilities.GetShaderPropertyIDs();
        Material obj = new Material(Shader.Find("TextMeshPro/Sprite"));
        obj.SetTexture(ShaderUtilities.ID_MainTex, spriteSheet);
        obj.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(obj, spriteAsset);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset));
        return obj;
    }

#endif
}