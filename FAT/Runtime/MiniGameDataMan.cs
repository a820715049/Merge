using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using Google.Protobuf.Collections;

namespace FAT
{
    public class MiniGameInfo
    {
        public MiniGameType Type;
        public bool FinishGuide;
        public int CompleteIndex = -1;
        public readonly List<int> RedPoint = new();
    }

    public class MiniGameDataMan : IGameModule, IUserDataHolder
    {
        public readonly Dictionary<MiniGameType, MiniGameInfo> AllGameSheets = new();

        public void Reset()
        {
            AllGameSheets.Clear();
        }

        public void LoadConfig()
        {
        }

        public void Startup()
        {
        }

        public void FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.MiniGameData ??= new MiniGameData();
            data.MiniGameData.AllData.Clear();
            data.MiniGameData.AllData.AddRange(AllGameSheets.Select(kv => new MiniGameLevel
                { GameId = (int)kv.Key, FinishGuide = kv.Value.FinishGuide, UnlockLevel = kv.Value.CompleteIndex }));
            foreach (var kv in data.MiniGameData.AllData)
                kv.RedPoint.AddRange(AllGameSheets[(MiniGameType)kv.GameId].RedPoint);
        }

        public void SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.MiniGameData;
            if (data == null) return;
            foreach (var kv in data.AllData)
            {
                var type = (MiniGameType)kv.GameId;
                var info = new MiniGameInfo
                    { Type = type, FinishGuide = kv.FinishGuide, CompleteIndex = kv.UnlockLevel };
                info.RedPoint.AddRange(kv.RedPoint);
                AllGameSheets.Add(type, info);
            }

            CheckGameSheets();
        }

        /// <summary>
        /// 检测当前是否有处于active状态，但是存档中没有的小游戏信息，并及时创建
        /// </summary>
        public void CheckGameSheets()
        {
            var all = Game.Manager.configMan.GetMiniGameMap();
            var newGame = all.Where(kv => kv.IsActive && !AllGameSheets.ContainsKey(kv.MiniGameType)).Select(kv =>
                new MiniGameInfo() { Type = kv.MiniGameType, FinishGuide = false });
            foreach (var game in newGame) AllGameSheets.Add(game.Type, game);
        }

        /// <summary>
        /// 记录完成某一个关卡
        /// </summary>
        /// <param name="type">游戏类型</param>
        /// <param name="index">关卡序号</param>
        public void CompleteLevel(MiniGameType type, int index)
        {
            var all = Game.Manager.configMan.GetMiniGameMap();
            if (AllGameSheets.TryGetValue(type, out var info))
            {
                if (info.CompleteIndex < index)
                {
                    info.CompleteIndex = index;
                    RefreshRP(type, index);
                }
            }
            else
            {
                var gameInfo = new MiniGameInfo { Type = type, CompleteIndex = index, FinishGuide = true };
                AllGameSheets.Add(type, gameInfo);
                RefreshRP(type, index);
            }
        }

        public void RefreshRP(MiniGameType type, int index)
        {
            var info = AllGameSheets[type];
            var sheet = Game.Manager.configMan.GetMiniGameMap().FirstOrDefault(kv => kv.MiniGameType == type);
            if (sheet == null) return;
            if (index >= sheet.LevelId.Count - 1)
                return;
            if (type == MiniGameType.MiniGameBeads)
            {
                if (Game.Manager.configMan.GetBeadsLevelConf(sheet.LevelId[index + 1]).ActiveLv <=
                    Game.Manager.mergeLevelMan.level)
                    info.RedPoint.Add(index + 1);
            }
            else if (type == MiniGameType.MiniGameSlideMerge)
            {
                info.RedPoint.Add(index + 1);
            }
        }

        /// <summary>
        /// 记录完成新手引导
        /// </summary>
        /// <param name="type">游戏类型</param>
        public void CompleteGuide(MiniGameType type)
        {
            if (AllGameSheets.TryGetValue(type, out var info))
            {
                info.FinishGuide = true;
            }
            else
            {
                var gameInfo = new MiniGameInfo { Type = type, FinishGuide = true };
                AllGameSheets.Add(type, gameInfo);
            }
        }

        /// <summary>
        /// 迷你游戏系统是否解锁
        /// </summary>
        /// <returns></returns>
        public bool IsMiniGameUnlock()
        {
            return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureMiniGame) &&
                   AllGameSheets.Any() && AllGameSheets.Any(kv =>
                       Game.Manager.configMan.GetMiniGameMap()
                           .FirstOrDefault(sheet => sheet.MiniGameType == kv.Value.Type)?.IsActive ?? false);
        }

        /// <summary>
        /// 目标关卡是否完成
        /// </summary>
        /// <param name="type">游戏类型</param>
        /// <param name="index">关卡序号</param>
        /// <returns></returns>
        public bool IsLevelComplete(MiniGameType type, int index)
        {
            if (!AllGameSheets.TryGetValue(type, out var gameInfo)) return false;
            return gameInfo.CompleteIndex >= index;
        }

        /// <summary>
        /// 当玩家等级提升时 会解锁小游戏中某些关卡
        /// </summary>
        public void OnMergeLevelChange()
        {
            if (!AllGameSheets.Any())
                CheckGameSheets();
            foreach (var act in AllGameSheets.Values)
                switch (act.Type)
                {
                    case MiniGameType.MiniGameBeads:
                    {
                        var info = AllGameSheets[act.Type];
                        var levels = Game.Manager.configMan.GetMiniGameMap()
                            .First(kv => kv.MiniGameType == act.Type).LevelId;
                        var level = levels.FirstOrDefault(kv =>
                            Game.Manager.configMan.GetBeadsLevelConf(kv).ActiveLv == Game.Manager.mergeLevelMan.level);
                        if (level > 0 && (levels.IndexOf(level) - 1 == info.CompleteIndex ||
                                          levels.IndexOf(level) == 0))
                            info.RedPoint.Add(info.CompleteIndex + 1);
                        break;
                    }
                    case MiniGameType.MiniGameSlideMerge:
                    {
                        var info = AllGameSheets[act.Type];
                        var levels = Game.Manager.configMan.GetMiniGameMap()
                            .First(kv => kv.MiniGameType == act.Type).LevelId;
                        var level = levels.FirstOrDefault(kv =>
                            Game.Manager.configMan.GetSlideMergeLevel(kv).ActiveLv == Game.Manager.mergeLevelMan.level);
                        if (level > 0 && (levels.IndexOf(level) - 1 == info.CompleteIndex ||
                                          levels.IndexOf(level) == 0))
                            info.RedPoint.Add(info.CompleteIndex + 1);
                        break;
                    }
                    case MiniGameType.Default:
                    {
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
        }

        /// <summary>
        /// 检查迷你游戏入口是否有红点
        /// </summary>
        /// <returns></returns>
        public bool CheckHasRP()
        {
            return AllGameSheets.Any(kv => kv.Value.RedPoint.Any());
        }

        /// <summary>
        /// 检查目标关卡是否有红点
        /// </summary>
        /// <param name="type">游戏类型</param>
        /// <param name="level">关卡序号</param>
        /// <returns></returns>
        public bool CheckLevelHasRP(MiniGameType type, int level)
        {
            return AllGameSheets.TryGetValue(type, out var info) && info.RedPoint.Contains(level);
        }

        /// <summary>
        /// 清空红点数据
        /// </summary>
        /// <param name="type">游戏类型</param>
        public void ClearLevelRP(MiniGameType type)
        {
            if (!AllGameSheets.TryGetValue(type, out var info))
                return;
            info.RedPoint.Clear();
        }

        public void SetIndex(MiniGameType type, int index)
        {
            if (!AllGameSheets.TryGetValue(type, out var info))
                return;
            info.CompleteIndex = index;
        }
    }
}