/*
 * @Author: Assistant
 * @Date: 2024-05-17
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using fat.rawdata;
using UnityEditor;
using UnityEngine;
using EventType = fat.rawdata.EventType;


namespace FAT
{
    public static class FishResourceLogger
    {
        public static readonly IReadOnlyDictionary<EventType, Type> Map = new Dictionary<EventType, Type>
        {
            { EventType.Fish, typeof(ActivityFishing) },
            { EventType.FarmBoard, typeof(FarmBoardActivity) },
            { EventType.Fight, typeof(FightBoardActivity) },
            { EventType.Mine, typeof(MineBoardActivity) },
            { EventType.WishBoard, typeof(WishBoardActivity) },
            { EventType.MarketIapgift, typeof(ActivityMIG) },
            { EventType.FlashOrder, typeof(ActivityFlashOrder) },
            { EventType.Score, typeof(ActivityScore) },
            { EventType.Step, typeof(ActivityStep) },
            { EventType.OrderExtra, typeof(ActivityExtraRewardOrder) },
            { EventType.LoginGift, typeof(LoginGiftActivity) },
            { EventType.Treasure, typeof(ActivityTreasure) },
            { EventType.Survey, typeof(ActivitySurvey) },
            { EventType.Race, typeof(ActivityRace) },
            { EventType.Digging, typeof(ActivityDigging) },
            { EventType.Invite, typeof(ActivityInvite) },
            { EventType.Rank, typeof(ActivityRanking) },
            { EventType.ZeroQuest, typeof(ActivityOrderChallenge) },
            { EventType.Stamp, typeof(ActivityStamp) },
            { EventType.Wishing, typeof(ActivityMagicHour) },
            { EventType.Guess, typeof(ActivityGuess) },
            { EventType.OrderDash, typeof(ActivityOrderDash) },
            { EventType.OrderStreak, typeof(ActivityOrderStreak) },
            { EventType.ItemBingo, typeof(ActivityBingo) },
            { EventType.OrderLike, typeof(ActivityOrderLike) },
            { EventType.ScoreDuel, typeof(ActivityDuel) },
            { EventType.WeeklyTask, typeof(ActivityWeeklyTask) },
            { EventType.CastleMilestone, typeof(ActivityCastle) },
            { EventType.OrderRate, typeof(ActivityOrderRate) },
            { EventType.OrderBonus, typeof(ActivityOrderBonus) },
            { EventType.ClawOrder, typeof(ActivityClawOrder) },
            { EventType.Redeem, typeof(ActivityRedeemShopLike) },
            { EventType.WeeklyRaffle, typeof(ActivityWeeklyRaffle) },
            { EventType.ThreeSign, typeof(ActivityThreeSign) },
            { EventType.Community, typeof(CommunityMailActivity) },
            { EventType.BingoTask, typeof(ActivityBingoTask) },
            { EventType.Energy, typeof(PackEnergy) },
            { EventType.DailyPop, typeof(PackDaily) },
            { EventType.NewSession, typeof(PackNewSession) },
            { EventType.OnePlusOne, typeof(PackOnePlusOne) },
            { EventType.MineOnePlusOne, typeof(PackOnePlusOneMine) },
            { EventType.OnePlusTwo, typeof(PackOnePlusTwo) },
            { EventType.EndlessPack, typeof(PackEndless) },
            { EventType.EndlessThreePack, typeof(PackEndlessThree) },
            { EventType.GemEndlessThree, typeof(PackGemEndlessThree) },
            { EventType.FarmEndlessPack, typeof(PackEndlessFarm) },
            { EventType.ThreeForOnePack, typeof(Pack1in3) },
            { EventType.ProgressPack, typeof(PackProgress) },
            { EventType.RetentionPack, typeof(PackRetention) },
            { EventType.MarketSlidePack, typeof(PackMarketSlide) },
            { EventType.GemThreeForOne, typeof(PackGemThreeForOne) },
            { EventType.EnergyMultiPack, typeof(PackEnergyMultiPack) },
            { EventType.ShinnyGuarPack, typeof(PackShinnyGuar) },
            { EventType.DiscountPack, typeof(PackDiscount) },
            { EventType.ErgListPack, typeof(PackErgList) },
            { EventType.FightOnePlusOne, typeof(PackOnePlusOneFight) },
            { EventType.WishEndlessPack, typeof(PackEndlessWishBoard) },
            { EventType.SpinPack, typeof(PackSpin) },
            { EventType.Bp, typeof(BPActivity) },
            { EventType.NewUser, typeof(PackNU) },
            { EventType.ToolExchange, typeof(ExchangeTool) },
            { EventType.De, typeof(ActivityDE) },
            { EventType.Dem, typeof(ActivityDEM) },
            { EventType.CardAlbum, typeof(CardActivity) },
            { EventType.Decorate, typeof(DecorateActivity) },
            { EventType.MiniBoard, typeof(MiniBoardActivity) },
            { EventType.Pachinko, typeof(ActivityPachinko) },
            { EventType.MiniBoardMulti, typeof(MiniBoardMultiActivity) },
        };
        
        private static readonly HashSet<string> _ArtExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tga", ".psd", ".gif", ".bmp", ".tiff",
        };

        // 期望的源路径前缀（用于裁剪）
        private const string SrcPrefix = "Assets/Bundle/event/";
        // 目标根目录（项目根相对路径）
        private const string DstRoot = "SpriteData/";
        // 清单文件名
        private const string ManifestName = "文件列表.txt";
        
        [MenuItem("Tools/Log Activity Resources")]
        public static void LogActivityFishResources()
        {
            foreach (var activeKey in Map)
            {
                var groups = GetUIResourceNames(activeKey.Value);
                Debug.Log($"{activeKey.Key}: {string.Join(",", groups.ToArray())}");
                // List<string> paths = new();
                // foreach (var group in groups)
                // {
                //     Debug.Log($"Group: {group}");
                //     foreach (var asset in _GetAssetsByGroup(group))
                //     {
                //         var bundle = AssetDatabase.GetImplicitAssetBundleName(asset);
                //         paths.Add(asset);
                //         // Debug.Log($"  {bundle}: {asset}");
                //     }
                // }
                //
                // CopyAll(paths, activeKey.Key);
            }
        }
        
        // <summary>
        /// 通过活动类型查找其 VisualRes、UIResAlt、VisualPopup 成员所引用的 UIConfig.UIResource 常量名称。
        /// </summary>
        /// <param name="activityType">继承自 ActivityLike 的类型</param>
        /// <returns>UIConfig 常量名列表</returns>
        public static List<string> GetUIResourceNames(Type activityType)
        {
            var result = new List<string>();
            if (activityType == null || !typeof(ActivityLike).IsAssignableFrom(activityType)) return result;

            ActivityLike instance = (ActivityLike)Activator.CreateInstance(activityType, true);

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var field in activityType.GetFields(flags))
            {
                var value = field.GetValue(instance);
                if (TryGetUIConfigName(field.FieldType, value, out var name) && !result.Contains(name))
                {
                    result.Add(name);
                }
            }

            foreach (var prop in activityType.GetProperties(flags))
            {
                if (!prop.CanRead) continue;
                object value = null;
                try
                {
                    value = prop.GetValue(instance);
                }
                catch
                {
                    continue;
                }

                if (TryGetUIConfigName(prop.PropertyType, value, out var name) && !result.Contains(name))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        private static bool TryGetUIConfigName(Type memberType, object value, out string uiConfigName)
        {
            uiConfigName = null;
            if (value == null) return false;

            UIResource res = null;

            if (memberType == typeof(UIResAlt))
            {
                var ui = (UIResAlt)value;
                res = ui.RefR ?? ui.ActiveR;
            }
            else if (memberType == typeof(VisualRes))
            {
                var vr = (VisualRes)value;
                res = vr.res.RefR ?? vr.res.ActiveR;
            }
            else if (memberType == typeof(VisualPopup))
            {
                var vp = (VisualPopup)value;
                res = vp.res.RefR ?? vp.res.ActiveR;
            }

            if (res == null) return false;

            uiConfigName = FindUIConfigFieldName(res);
            return uiConfigName != null;
        }

        private static string FindUIConfigFieldName(UIResource res)
        {
            var fields = typeof(UIConfig).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (ReferenceEquals(field.GetValue(null), res))
                {
                    return field.Name;
                }
            }
            return null;
        }

        private static HashSet<string> _CollectFishGroups(string key)
        {
            var result = new HashSet<string>();
            var fields = typeof(UIConfig).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.Name.StartsWith(key, StringComparison.Ordinal))
                {
                    if (field.GetValue(null) is UIResource res && !string.IsNullOrEmpty(res.prefabGroup))
                    {
                        result.Add(res.prefabGroup);
                    }
                }
            }
            return result;
        }

        private static IEnumerable<string> _GetAssetsByGroup(string group)
        {
            var dir = _FindGroupPath(group);
            if (string.IsNullOrEmpty(dir))
            {
                yield break;
            }
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }
                var ext = Path.GetExtension(file);
                if (!_ArtExtensions.Contains(ext))
                {
                    continue;
                }

                var path = file.Replace('\\', '/');
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                yield return path;
            }
        }

        private static string _FindGroupPath(string group)
        {
            var idx = group.IndexOf('_');
            if (idx < 0)
            {
                var bundlePath = Path.Combine("Assets/Bundle", $"bundle_{group}");
                if (Directory.Exists(bundlePath))
                {
                    return bundlePath;
                }
            }
            else
            {
                while (idx >= 0)
                {
                    var parentPath = Path.Combine("Assets/Bundle", group[..idx]);
                    if (Directory.Exists(parentPath))
                    {
                        var bundleFolder = $"bundle_{group[(idx + 1)..]}";
                        var dirs = Directory.GetDirectories(parentPath, bundleFolder, SearchOption.AllDirectories);
                        if (dirs.Length > 0)
                        {
                            return dirs[0];
                        }
                    }
                    idx = group.IndexOf('_', idx + 1);
                }
            }
            return null;
        }
        
        public static void CopyAll(List<string> paths, string name)
        {
            try
            {
                // 项目根绝对路径（去掉末尾的 /Assets）
                var projectRoot = Path.GetDirectoryName(Application.dataPath).Replace("\\", "/");
                var dstRootAbs = Path.Combine(projectRoot, $"{DstRoot}{name}").Replace("\\", "/");

                if (!Directory.Exists(dstRootAbs))
                {
                    Directory.CreateDirectory(dstRootAbs);
                }

                // 去重 + 只保留以 "Assets/" 开头的有效项
                var items = paths
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Replace("\\", "/").Trim())
                    .Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .ToList();

                if (items.Count == 0)
                {
                    Debug.LogWarning("[CopyUIActivityFish] 列表为空，未执行复制。");
                    return;
                }

                var success = 0;
                var missing = 0;
                var written = new StringBuilder();

                foreach (var assetPath in items)
                {
                    // 1) 计算源文件绝对路径
                    var srcAbs = Path.Combine(projectRoot, assetPath).Replace("\\", "/");

                    if (!File.Exists(srcAbs))
                    {
                        missing++;
                        Debug.LogWarning($"[CopyUIActivityFish] 源文件不存在：{assetPath}");
                        continue;
                    }

                    // 2) 裁剪前缀，得到相对路径
                    string tail;
                    if (assetPath.StartsWith(SrcPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        tail = assetPath.Substring(SrcPrefix.Length); // 期望：bundle_fish_default/...
                    }
                    else
                    {
                        missing++;
                        Debug.LogWarning($"[CopyUIActivityFish] 非法路径（不以 Assets/ 开头）：{assetPath}");
                        continue;
                    }

                    // 3) 目标绝对路径（SpriteData/UIActivityFish/ + tail）
                    var dstAbs = Path.Combine(dstRootAbs, tail).Replace("\\", "/");
                    var dstDir = Path.GetDirectoryName(dstAbs);
                    Directory.CreateDirectory(dstDir);

                    // 4) 复制（覆盖同名文件）
                    File.Copy(srcAbs, dstAbs, overwrite: true);
                    success++;

                    // 5) 记录到清单（相对于 SpriteData/UIActivityFish）
                    var relForManifest = tail.Replace("\\", "/");
                    written.AppendLine(relForManifest);
                }

                // 写清单
                var manifestAbs = Path.Combine(dstRootAbs, ManifestName).Replace("\\", "/");
                File.WriteAllText(manifestAbs, written.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                AssetDatabase.Refresh();

                Debug.Log($"[CopyUIActivityFish] 完成。复制成功 {success} 个；缺失 {missing} 个。\n" +
                          $"目标目录：{dstRootAbs}\n清单：{manifestAbs}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CopyUIActivityFish] 执行失败：\n" + ex);
            }
        }
    }
}
