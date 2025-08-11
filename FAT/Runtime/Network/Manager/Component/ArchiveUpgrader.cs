/*
 * @Author: qun.chao
 * @Date: 2025-02-21 18:24:28
 */
using EL;
using fat.gamekitdata;
using confData = fat.conf.Data;

namespace FAT
{
    // 统一升级比各个接口里单独升级更容易维护
    public class ArchiveUpgrader
    {
        // https://centurygames.feishu.cn/wiki/Uf58wF19siph3ck5i3McPQ1mnQf
        private const int version_level_fix = 11;

        public void OnDataVersionUpgrade(LocalSaveData oldData, LocalSaveData nowData, int oldVersion, int nowVersion)
        {
            if (oldVersion >= nowVersion)
                return;

            if (oldVersion < version_level_fix)
            {
                Upgrade_FixLevel(oldData, nowData);
            }
        }

        private void Upgrade_FixLevel(LocalSaveData oldData, LocalSaveData nowData)
        {
            var buildingMap = confData.GetBuildingBaseMap();
            var buildingLevelMap = confData.GetBuildingLevelMap();
            var buildingCostMap = confData.GetBuildingCostMap();

            int CollectCostRewardExp(int costId)
            {
                var totalExp = 0;
                if (buildingCostMap.TryGetValue(costId, out var cfg))
                {
                    foreach (var item in cfg.CostReward)
                    {
                        var reward = item.ConvertToRewardConfig();
                        if (reward.Id == Constant.kMergeExpObjId)
                        {
                            totalExp += reward.Count;
                        }
                    }
                }
                return totalExp;
            }

            int CalcBuildingExpReward(int id, int nowLevel, int phase)
            {
                var totalExp = 0;
                if (!buildingMap.TryGetValue(id, out var cfg))
                    return totalExp;
                for (var levelIdx = 0; levelIdx < cfg.LevelInfo.Count; levelIdx++)
                {
                    if (levelIdx > nowLevel)
                        break;
                    if (!buildingLevelMap.TryGetValue(cfg.LevelInfo[levelIdx], out var levelCfg))
                        continue;
                    var costCount = levelCfg.CostInfo.Count;
                    if (levelIdx == nowLevel && phase > 0)
                    {
                        // 建筑正处于两个等级之间 只计算到当前phase
                        costCount = phase;
                    }
                    for (var costIdx = 0; costIdx < costCount; costIdx++)
                    {
                        totalExp += CollectCostRewardExp(levelCfg.CostInfo[costIdx]);
                    }
                }
                return totalExp;
            }

            // 用建筑等级推断level
            var srcData = oldData.ClientData.PlayerGameData;
            var dstData = nowData.ClientData;
            if (srcData == null || dstData == null)
                return;

            var beforeLevel = oldData.PlayerBaseData.Level;
            var beforeExp = oldData.PlayerBaseData.Exp;

            var totalExp = 0;
            var buildings = srcData.MapScene.Building;
            foreach (var item in buildings)
            {
                var id = item.Key;
                var level = item.Value.Level;
                var phase = item.Value.Phase;
                if (level == 0 && phase == 0)
                    continue;
                totalExp += CalcBuildingExpReward(id, level, phase);
            }

            // 如果修正后的等级会降低, 需要计算'如果不降级'额外需要的exp总量
            var expNeedForKeepLevel = 0;
            var afterLevel = 0;
            var afterExp = totalExp;
            var levelInfo = confData.GetMergeLevelSlice().GetEnumerator();
            while (levelInfo.MoveNext())
            {
                var item = levelInfo.Current;

                if (expNeedForKeepLevel <= 0)
                {
                    if (afterExp >= item.Exp)
                    {
                        afterExp -= item.Exp;
                        afterLevel = item.Level;
                    }
                    else
                    {
                        if (item.Level <= beforeLevel)
                        {
                            // 当前等级升级需要的经验
                            expNeedForKeepLevel = item.Exp - afterExp;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (item.Level <= beforeLevel)
                    {
                        // 当前等级需要的经验
                        expNeedForKeepLevel += item.Exp;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            var finalLevel = 0;
            var finalExp = 0;
            if (beforeLevel <= afterLevel)
            {
                finalLevel = afterLevel;
                finalExp = afterExp;
                expNeedForKeepLevel = 0;
            }
            else
            {
                // 理论上会发生降级
                // 为避免用户降级 需要记录用户提前预支的exp
                finalLevel = beforeLevel;
                finalExp = 0;
            }

            var levelStat = beforeLevel < finalLevel ? "up" :
                            beforeLevel > finalLevel ? "down" :
                            "remain";
            var expStat = beforeExp < finalExp ? "up" :
                          beforeExp > finalExp ? "down" :
                          "remain";

            using var _ = PoolMapping.PoolMappingAccess.Borrow<System.Text.StringBuilder>(out var sb);
            sb.Append($"[archiveupgrade] fix level =>");
            sb.Append($"before lv.{beforeLevel} exp.{beforeExp} | ");
            sb.Append($"after lv.{afterLevel} exp.{afterExp} | ");
            sb.Append($"final lv.{finalLevel} exp.{finalExp} debt.{expNeedForKeepLevel} | ");
            sb.Append($"lv {levelStat} | exp {expStat} | debt {expNeedForKeepLevel > 0}".ToLower());
            DataTracker.TrackLogInfo($"{sb}");

            nowData.PlayerBaseData.Level = finalLevel;
            nowData.PlayerBaseData.Exp = finalExp;
            nowData.PlayerBaseData.ExpDebt = expNeedForKeepLevel;
        }
    }
}