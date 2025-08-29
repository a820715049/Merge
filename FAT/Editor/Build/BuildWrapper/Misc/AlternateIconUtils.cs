/*
 * @Author: qun.chao
 * @Date: 2025-08-27 12:17:23
 */
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using AppIconChanger.Editor; // AlternateIcon lives here

namespace BuildWrapper
{
    // ref: https://github.com/kyubuns/AppIconChangerUnity
    public static class AlternateIconUtils
    {
        private class IconEntry
        {
            public string propertyName;
            public string fileName;
            public int size;

            public IconEntry(string propertyName, int size)
            {
                this.propertyName = propertyName;
                this.fileName = propertyName + ".png";
                this.size = size;
            }
        }

        private static readonly IconEntry[] IconEntries = new[]
        {
            new IconEntry("iPhoneNotification40px", 40),
            new IconEntry("iPhoneNotification60px", 60),
            new IconEntry("iPhoneSettings58px", 58),
            new IconEntry("iPhoneSettings87px", 87),
            new IconEntry("iPhoneSpotlight80px", 80),
            new IconEntry("iPhoneSpotlight120px", 120),
            new IconEntry("iPhoneApp120px", 120),
            new IconEntry("iPhoneApp180px", 180),
            new IconEntry("iPadNotifications20px", 20),
            new IconEntry("iPadNotifications40px", 40),
            new IconEntry("iPadSettings29px", 29),
            new IconEntry("iPadSettings58px", 58),
            new IconEntry("iPadSpotlight40px", 40),
            new IconEntry("iPadSpotlight80px", 80),
            new IconEntry("iPadApp76px", 76),
            new IconEntry("iPadApp152px", 152),
            new IconEntry("iPadProApp167px", 167),
            new IconEntry("appStore1024px", 1024),
        };

        [MenuItem("Assets/AppIcon/Generate & Assign (Manual) From 1024 Base...", priority = 2000)]
        private static void GenerateAndAssignManualFrom1024()
        {
            var selected = GetSelectedAlternateIcon();
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Generate Icons", "Please select one AlternateIcon asset under Assets/Icon/configs.", "OK");
                return;
            }

            var baseImagePath = EditorUtility.OpenFilePanel("Choose 1024x1024 base image (PNG/JPG)", Application.dataPath, "png,jpg,jpeg");
            if (string.IsNullOrEmpty(baseImagePath))
            {
                return;
            }

            Texture2D baseTexture;
            try
            {
                baseTexture = LoadTextureFromFile(baseImagePath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Generate Icons", "Failed to load image: " + e.Message, "OK");
                return;
            }

            if (baseTexture == null)
            {
                EditorUtility.DisplayDialog("Generate Icons", "Failed to load image.", "OK");
                return;
            }

            if (baseTexture.width != 1024 || baseTexture.height != 1024)
            {
                var cont = EditorUtility.DisplayDialog("Image Size Mismatch",
                    $"Selected image is {baseTexture.width}x{baseTexture.height}. Expected 1024x1024. Continue and resample?",
                    "Continue", "Cancel");
                if (!cont) return;
            }

            var iconAssetPath = AssetDatabase.GetAssetPath(selected);
            var iconDir = Path.GetDirectoryName(iconAssetPath)?.Replace("\\", "/") ?? "Assets";
            var iconName = Path.GetFileNameWithoutExtension(iconAssetPath);
            var outputDirAssetPath = iconDir + "/" + iconName + "_generate";

            EnsureDirectory(outputDirAssetPath);

            try
            {
                EditorUtility.DisplayProgressBar("Generating Icons", "Preparing...", 0f);

                var generatedAssetPaths = new Dictionary<string, string>(); // propertyName -> assetPath

                for (int i = 0; i < IconEntries.Length; i++)
                {
                    var entry = IconEntries[i];
                    var progress = (float)i / IconEntries.Length;
                    EditorUtility.DisplayProgressBar("Generating Icons", $"{entry.propertyName} ({entry.size}x{entry.size})", progress);

                    var scaled = ImageResampler.ResizeArea(baseTexture, entry.size, entry.size);
                    var png = scaled.EncodeToPNG();

                    var saveAssetPath = outputDirAssetPath + "/" + entry.fileName;
                    var saveAbsolutePath = AssetPathToAbsolutePath(saveAssetPath);
                    File.WriteAllBytes(saveAbsolutePath, png);

                    generatedAssetPaths[entry.propertyName] = saveAssetPath;
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                // Ensure correct importer settings (disable mipmaps, NPOT = None) and reimport
                foreach (var kv in generatedAssetPaths)
                {
                    var importer = AssetImporter.GetAtPath(kv.Value) as TextureImporter;
                    if (importer != null)
                    {
                        importer.mipmapEnabled = false;
                        importer.npotScale = TextureImporterNPOTScale.None;
                        importer.SaveAndReimport();
                    }
                }

                // Assign textures to the AlternateIcon asset fields
                var so = new SerializedObject(selected);
                foreach (var entry in IconEntries)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(generatedAssetPaths[entry.propertyName]);
                    so.FindProperty(entry.propertyName).objectReferenceValue = tex;
                }
                // Switch to Manual so PostProcessor uses manual textures
                so.FindProperty("type").enumValueIndex = (int)AlternateIconType.Manual;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(selected);
                AssetDatabase.SaveAssets();

                EditorUtility.RevealInFinder(AssetPathToAbsolutePath(outputDirAssetPath));
                EditorUtility.DisplayDialog("Generate Icons", "Icons generated and assigned successfully.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Assets/AppIcon/Generate & Assign (Manual) From 1024 Base...", true)]
        private static bool ValidateGenerateAndAssignManualFrom1024()
        {
            var selected = GetSelectedAlternateIcon();
            return selected != null;
        }

        private static AlternateIcon GetSelectedAlternateIcon()
        {
            if (Selection.objects == null || Selection.objects.Length != 1) return null;
            var obj = Selection.activeObject;
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return null;
            var icon = AssetDatabase.LoadAssetAtPath<AlternateIcon>(path);
            return icon;
        }

        private static void EnsureDirectory(string assetPath)
        {
            var fullPath = AssetPathToAbsolutePath(assetPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            assetPath = assetPath.Replace("\\", "/");
            if (!assetPath.StartsWith("Assets/"))
            {
                return assetPath;
            }
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static Texture2D LoadTextureFromFile(string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear
            };
            if (!tex.LoadImage(data, markNonReadable: false))
            {
                throw new Exception("Unsupported or corrupted image file.");
            }
            return tex;
        }

        private static class ImageResampler
        {
            public static Texture2D ResizeArea(Texture2D src, int dstW, int dstH)
            {
                if (src.width == dstW && src.height == dstH) return src;

                var dst = new Texture2D(dstW, dstH, TextureFormat.RGBA32, false);
                float scaleX = (float)src.width / dstW;
                float scaleY = (float)src.height / dstH;

                for (int y = 0; y < dstH; y++)
                {
                    float srcY0 = y * scaleY;
                    float srcY1 = (y + 1) * scaleY;
                    int yStart = Mathf.FloorToInt(srcY0);
                    int yEnd = Mathf.Min(src.height - 1, Mathf.CeilToInt(srcY1) - 1);
                    for (int x = 0; x < dstW; x++)
                    {
                        float srcX0 = x * scaleX;
                        float srcX1 = (x + 1) * scaleX;
                        int xStart = Mathf.FloorToInt(srcX0);
                        int xEnd = Mathf.Min(src.width - 1, Mathf.CeilToInt(srcX1) - 1);

                        Vector4 sum = Vector4.zero;
                        float areaSum = 0f;

                        for (int sy = yStart; sy <= yEnd; sy++)
                        {
                            float y0 = Mathf.Max(srcY0, sy);
                            float y1 = Mathf.Min(srcY1, sy + 1);
                            float wy = Mathf.Max(0f, y1 - y0);
                            for (int sx = xStart; sx <= xEnd; sx++)
                            {
                                float x0 = Mathf.Max(srcX0, sx);
                                float x1 = Mathf.Min(srcX1, sx + 1);
                                float wx = Mathf.Max(0f, x1 - x0);
                                float w = wx * wy;
                                Color c = src.GetPixel(sx, sy);
                                sum.x += c.r * w;
                                sum.y += c.g * w;
                                sum.z += c.b * w;
                                sum.w += c.a * w;
                                areaSum += w;
                            }
                        }

                        if (areaSum > 0f)
                        {
                            sum /= areaSum;
                        }
                        dst.SetPixel(x, y, new Color(sum.x, sum.y, sum.z, sum.w));
                    }
                }

                dst.Apply(false, false);
                return dst;
            }
        }
    }
}